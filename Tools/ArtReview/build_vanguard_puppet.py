#!/usr/bin/env python3
"""Cut the Vanguard's two painted masters into cut-out puppet parts (docs/45 section 4 B).

The Vanguard was painted twice, whole: once from behind (rear three-quarter, facing up-right) and once from the front
(front three-quarter, facing down-right). The puppet needs each facing as separate pieces - helmet, torso, cape, belt,
skirt, thighs, shins with boots, upper arms, the near pauldron and forearms with their gauntlets - that PuppetRig hangs
from one another and turns at their joints. A painting only holds what was visible, so every piece that can swing away
from what covered it is completed here with the classic cut-out tricks:

  * each piece is taken along the painted outlines: the dark, colourless lines between plates split the paint into
    regions, a point inside each plate names the piece it belongs to, rough polygons hand out the specks, and every
    outline goes to the piece in front of those it separates, so a piece that swings away takes its outline along;
  * every piece continues under whatever covers it - automatically a little way under each neighbour in front, and
    further where its hidden shape is drawn here (the torso under the arms and the cape, the cape behind the whole
    body, an elbow plate under the lame that overlaps it) - painted by smearing its own armour along the limb or by a
    smooth fill that settles into the plate's colour;
  * the joints that open when a limb bends - shoulders under the pauldrons, hips under the skirt, knees and elbows -
    are filled with the master's own painted chainmail, as real armour shows mail in its gaps, with round caps so a
    bent knee or elbow never opens;
  * a dark outline drawn round any silhouette edge that was invented rather than painted;
  * the far thigh seen from behind, which the cape hides entirely, is the near thigh cloned and darkened;
  * where the painting overlaps two pieces both ways, the draw order follows the painting: seen from the front the
    thigh's knee band lies over the knee plate, seen from behind the shin's band over the thigh's mail.

The pieces are reduced to the runtime scale of the painted Vanguard frames (816 source pixels a world unit, 128 texels
a unit, area-averaged in premultiplied alpha) on one grid per facing, so the standing puppet matches the painting
texel for texel, and written as RGBA TGA with a white hit-flash silhouette beside each. Every .meta copies the
importer settings of an existing Vanguard frame (128 PPU, bilinear, no mipmaps, clamp, full-rect, Android ASTC 4x4)
with the sprite pivot on the piece's joint; a file that already has a .meta keeps its GUID, so a rebuild never breaks
the scene's references. VanguardPuppetArtSet.asset (a PuppetArtSet) lists the parts, joints, bones, draw order,
hands, grips and each boot's sole (the heel and toe the walk's bob stands on), and borrows the existing painted
equipment.

Usage:
  python Tools/ArtReview/build_vanguard_puppet.py [--review DIR] [--dry-run]

  --review DIR  also writes review images: every cut over the master, each facing's finished parts at source size,
                and the standing puppet recomposed from its reduced parts beside the reduced master and their
                difference (the mean difference is printed; the standing puppet should be the painting).
  --dry-run     builds and reviews but writes nothing under Assets.

Then preview_vanguard_puppet.py animates the parts with the game's own motion.

Coordinates in the tables are source pixels of each master, origin top-left, y down. Joints are where a part turns
(its sprite pivot); a part's Joint in the asset is its joint relative to its parent's, in world units, y up.
"""
from __future__ import annotations

import argparse
import math
import re
import sys
import uuid
from dataclasses import dataclass, field
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = ROOT / 'Assets/_Project/Art/Characters/Vanguard/Puppet'
META_TEMPLATE = ROOT / 'Assets/_Project/Art/Characters/Vanguard/CHR_Vanguard_Idle.tga.meta'
ASSET_NAME = 'VanguardPuppetArtSet'
PUPPET_SCRIPT_META = ROOT / 'Assets/_Project/Scripts/Art/Unity/PuppetArtSet.cs.meta'
EQUIPMENT = {
    '_sword': ROOT / 'Assets/_Project/Art/Characters/Vanguard/WPN_Vanguard_Sword.tga.meta',
    '_shield': ROOT / 'Assets/_Project/Art/Characters/Vanguard/WPN_Vanguard_Shield.tga.meta',
    '_staff': ROOT / 'Assets/_Project/Art/Characters/Vanguard/WPN_Vanguard_Staff.tga.meta',
    '_dagger': ROOT / 'Assets/_Project/Art/Characters/Vanguard/WPN_Vanguard_Dagger.tga.meta',
}

# The painted Vanguard's scale (ArtDirection/2026-09-17/vanguard-runtime-01): an 816-pixel crop of either master is
# 128 texels, one world unit, at 128 pixels per unit.
SOURCE_PER_UNIT = 816.0
PPU = 128
SCALE = PPU / SOURCE_PER_UNIT
# Every reduction runs on a canvas whose size is a whole number of texels: 51 source pixels are exactly 8 texels.
GRID = 51
# The masters' painted interior sits at alpha 253; anything at or above this is solid paint.
SOLID_ALPHA = 0.985
# Below this coverage a source pixel is background.
CLEAR_ALPHA = 0.03
# A painted outline or crease between plates: darker than this luminance (0-255) with no channel above
# LINE_CHANNEL, which keeps the cape's deep navy and the armour's blue shadows out.
LINE_LUMINANCE = 30
LINE_CHANNEL = 62
# How far (source pixels) the part in front reaches to take an outline it shares with a part behind.
LINE_REACH = 5
# The painted outline, sampled from the masters' silhouettes: almost black with a little navy.
OUTLINE = np.array([5, 4, 11], np.float32) / 255
# How far (source pixels) every part continues under each neighbour drawn in front of it.
UNDERLAP = 14
# The width (source pixels) of an outline drawn round an invented edge: about a third of a texel at runtime, like
# the painted ones.
DRAWN_OUTLINE = 3.0

BONES = ['Fixed', 'Pelvis', 'Torso', 'Head', 'Cape', 'Skirt', 'ThighLeft', 'ShinLeft', 'ThighRight', 'ShinRight',
         'UpperArmLeft', 'ForearmLeft', 'HandLeft', 'UpperArmRight', 'ForearmRight', 'HandRight']


@dataclass
class Part:
    name: str
    bone: str
    parent: str | None
    joint: tuple[float, float]
    sorting: int
    # Points inside its painted plates: each claims the whole region of paint the outlines enclose around it.
    seeds: list
    # Rough polygons for the paint no seed claims (chainmail, specks): a region goes to the part whose polygon
    # covers most of it, front parts first.
    take: list = field(default_factory=list)
    # Where it continues out of sight, beyond what it took: polygons and circles, painted by the fill, and kept to what
    # the parts in front of it cover at rest.
    hidden: list = field(default_factory=list)
    # Where it continues as the mail under the plates - shoulders under pauldrons, hips under the skirt, the gaps a
    # bent knee or elbow opens - painted with the master's own chainmail, as real armour shows at its joints.
    mail: list = field(default_factory=list)
    # Paint another part took that this one also needs, copied as painted: a plate whose edge the neighbour in
    # front overlaps keeps that edge when it turns away.
    copy: list = field(default_factory=list)
    # The fill for hidden pixels: 'smear' copies the part's own paint along axis (both ways, the nearer wins),
    # 'smooth' blends it in from every side; fill_seeds limits the paint it starts from to those seeds' regions.
    fill: str = 'smooth'
    axis: tuple[float, float] = (0.0, 1.0)
    fill_seeds: list = field(default_factory=list)
    # For a smooth fill: the plain plate colour (0-255 RGB) it settles into away from the paint.
    fill_colour: tuple | None = None
    # A part painted from another: (source part, dx, dy, tone) copies that part's finished layer shifted by
    # (dx, dy) source pixels and scaled in tone, under whatever this part took itself.
    clone: tuple | None = None

    def file_stem(self):
        return re.sub(r'[^A-Za-z0-9]', '', self.name)


@dataclass
class Facing:
    name: str
    source: str
    # The source pixel on the floor line under the figure, which the rig's root stands on.
    floor: tuple[float, float]
    parts: list
    right_hand: str
    right_grip: tuple[float, float]
    right_item_sorting: int
    left_hand: str
    left_grip: tuple[float, float]
    left_item_sorting: int
    # A rectangle (x0, y0, x1, y1) of plain painted chainmail, tiled into every part's mail.
    mail_patch: tuple
    # Polylines drawn into the outlines where the painting runs two plates together without one.
    cuts: list = field(default_factory=list)


def circle(x, y, r):
    return ('circle', float(x), float(y), float(r))


# ------------------------------------------------------------------------------------------------- the two facings
#
# Draw order, back to front, the same idea in both facings: the far arm and the far leg behind the body; the torso;
# the near leg; the skirt over the thighs and the belt over the skirt and the torso's waist; the near arm over the
# torso, its pauldron covering the shoulder, the held weapon between the pauldron and the gauntlet, which closes over
# the grip. Seen from behind the cape covers the back and the helmet sits over the cape's hood; seen from the front
# the cape hangs behind everything. A limb's elbow or knee plate rides on the lower part so it turns with the joint,
# and whichever part the painting overlaps on top keeps its place: seen from the front the thigh's knee band lies
# over the knee plate, seen from behind the shin's knee band over the thigh's mail, and the far upper arm's lame over
# its elbow plate.

REAR = Facing(
    name='Rear',
    source='ArtDirection/2026-09-16/character-proof-01/vanguard-body-v2.png',
    floor=(594.0, 1213.0),
    right_hand='Forearm Right', right_grip=(848.0, 794.0), right_item_sorting=10,
    left_hand='Forearm Left', left_grip=(345.0, 596.0), left_item_sorting=-13,
    mail_patch=(596, 745, 634, 792),
    parts=[
        Part('Pelvis', 'Pelvis', None, (650, 628), 5,
             seeds=[(650, 625), (640, 643)],
             take=[[(590, 600), (706, 598), (710, 652), (590, 658)]],
             hidden=[[(560, 598), (725, 592), (728, 662), (560, 668)]]),
        Part('Torso', 'Torso', 'Pelvis', (650, 628), 0,
             seeds=[(414, 332), (627, 569)],
             take=[[(338, 250), (505, 252), (520, 300), (600, 345), (705, 340), (720, 380), (705, 520), (712, 612),
                    (590, 616), (590, 520), (520, 420), (440, 410), (338, 400)]],
             hidden=[[(345, 300), (420, 255), (505, 262), (525, 300), (565, 330), (650, 335), (708, 345), (742, 400),
                      (748, 520), (742, 600), (720, 640), (600, 645), (575, 600), (520, 520), (460, 430), (400, 412),
                      (345, 400)]],
             mail=[[(668, 520), (742, 515), (748, 600), (720, 640), (668, 620)]],
             fill_seeds=[(627, 569)], fill_colour=(34, 50, 104)),
        Part('Head', 'Head', 'Torso', (600, 330), 7,
             seeds=[(601, 217), (700, 254), (505, 219), (678, 258), (554, 336)],
             take=[[(480, 80), (735, 80), (735, 330), (690, 346), (600, 352), (520, 348), (480, 320)]]),
        Part('Cape', 'Cape', 'Torso', (570, 360), 6,
             seeds=[(548, 376), (480, 659)],
             take=[[(460, 300), (705, 300), (705, 360), (612, 420), (602, 600), (612, 760), (620, 890), (300, 810),
                    (380, 600), (392, 430), (440, 380)]],
             hidden=[[(485, 300), (705, 300), (705, 365), (485, 365)]],
             fill_seeds=[(548, 376)]),
        Part('Skirt', 'Skirt', 'Pelvis', (670, 650), 4,
             seeds=[(681, 674), (601, 683), (691, 742), (617, 759)],
             take=[[(588, 650), (720, 645), (765, 700), (770, 800), (740, 812), (650, 806), (590, 800)]],
             hidden=[[(565, 645), (600, 640), (600, 790), (565, 795)], [(600, 636), (735, 636), (735, 672), (600, 672)]],
             fill='smear', axis=(0.0, 1.0)),
        Part('Thigh Left', 'ThighLeft', 'Pelvis', (445, 745), -8,
             seeds=[(431, 793)],
             take=[[(412, 752), (448, 752), (448, 822), (412, 822)]],
             mail=[circle(408, 853, 36)],
             clone=('Thigh Right', -239, -20, 0.8)),
        Part('Shin Left', 'ShinLeft', 'Thigh Left', (408, 853), -7,
             seeds=[(400, 927), (409, 855), (392, 1046), (423, 1026)],
             take=[[(340, 832), (482, 832), (490, 1000), (484, 1105), (295, 1105), (300, 1000), (338, 880)]]),
        Part('Thigh Right', 'ThighRight', 'Pelvis', (684, 765), 2,
             seeds=[(661, 825)],
             take=[[(622, 800), (700, 800), (700, 848), (622, 848)]],
             mail=[[(632, 715), (744, 715), (752, 800), (738, 862), (690, 878), (640, 868), (626, 800)],
                   circle(680, 850, 42)]),
        Part('Shin Right', 'ShinRight', 'Thigh Right', (680, 850), 3,
             seeds=[(679, 847), (730, 884), (669, 996), (778, 1122), (726, 1121), (716, 1110), (658, 1160)],
             take=[[(612, 808), (740, 805), (780, 880), (790, 1060), (850, 1100), (850, 1220), (600, 1220),
                    (612, 1080), (612, 900)]]),
        Part('Upper Arm Left', 'UpperArmLeft', 'Torso', (392, 392), -11,
             seeds=[(367, 425), (395, 437), (369, 457)],
             take=[[(345, 395), (405, 395), (400, 470), (350, 470)]],
             mail=[[(352, 350), (440, 350), (432, 440), (400, 470), (360, 440)], circle(392, 392, 38)]),
        Part('Forearm Left', 'ForearmLeft', 'Upper Arm Left', (326, 462), -12,
             seeds=[(325, 466), (375, 486), (377, 518), (335, 529), (340, 555), (377, 576), (326, 586), (361, 608),
                    (340, 616)],
             take=[[(288, 405), (358, 405), (405, 470), (395, 560), (380, 635), (300, 635), (290, 520)]],
             hidden=[circle(326, 460, 50)],
             fill_seeds=[(325, 466)]),
        Part('Upper Arm Right', 'UpperArmRight', 'Torso', (728, 482), 8,
             seeds=[(743, 563), (672, 596)],
             take=[[(662, 540), (810, 512), (812, 600), (700, 615), (660, 610)]],
             mail=[[(686, 445), (782, 445), (806, 520), (676, 530)], circle(728, 482, 46),
                   [(690, 600), (804, 600), (782, 650), (732, 662), (690, 630)], circle(757, 640, 36)]),
        Part('Pauldron Right', 'Fixed', 'Torso', (713, 453), 9,
             seeds=[(713, 453), (613, 418)],
             take=[[(590, 352), (700, 342), (790, 360), (832, 420), (832, 500), (806, 522), (700, 548), (640, 540),
                    (600, 500), (588, 420)]]),
        Part('Forearm Right', 'ForearmRight', 'Upper Arm Right', (757, 640), 11,
             seeds=[(757, 639), (819, 699), (859, 777), (828, 813)],
             take=[[(704, 586), (822, 590), (870, 680), (900, 760), (900, 835), (805, 835), (752, 760), (704, 700)]]),
    ])

FRONT = Facing(
    name='Front',
    source='ArtDirection/2026-09-17/vanguard-directions-01/front-idle-v1.png',
    floor=(594.0, 1225.0),
    right_hand='Forearm Right', right_grip=(365.0, 790.0), right_item_sorting=10,
    left_hand='Forearm Left', left_grip=(905.0, 686.0), left_item_sorting=-10,
    mail_patch=(578, 700, 614, 778),
    cuts=[[(505, 590), (560, 581), (620, 583), (668, 588), (712, 584), (770, 572)]],
    parts=[
        Part('Pelvis', 'Pelvis', None, (632, 608), 5,
             seeds=[(612, 615), (547, 607), (730, 605), (525, 592), (685, 620)],
             take=[[(514, 572), (752, 570), (752, 640), (720, 652), (650, 652), (514, 632)]]),
        Part('Torso', 'Torso', 'Pelvis', (632, 608), 0,
             seeds=[(657, 507), (785, 373), (729, 350), (700, 382), (632, 389)],
             take=[[(490, 345), (600, 345), (705, 295), (850, 295), (850, 450), (780, 470), (765, 600), (765, 650),
                    (500, 650), (480, 520)]],
             hidden=[[(445, 365), (520, 338), (600, 345), (705, 300), (850, 300), (858, 450), (792, 480), (772, 560),
                      (770, 645), (720, 655), (530, 655), (470, 610), (440, 520)]],
             mail=[[(446, 480), (500, 470), (505, 610), (462, 610)]],
             fill_seeds=[(657, 507)], fill_colour=(70, 88, 150)),
        Part('Head', 'Head', 'Torso', (632, 378), 6,
             seeds=[(634, 240), (544, 259), (726, 211)],
             take=[[(510, 92), (732, 92), (732, 300), (690, 370), (630, 385), (560, 372), (510, 330)]]),
        Part('Cape', 'Cape', 'Torso', (632, 360), -20,
             seeds=[(817, 722), (425, 683), (403, 666), (466, 700), (431, 806), (320, 843), (601, 824)],
             take=[[(255, 430), (340, 420), (330, 520), (300, 620), (330, 740), (470, 760), (470, 900), (380, 940),
                    (250, 900)], [(754, 530), (880, 400), (900, 700), (860, 820), (760, 810)],
                   [(380, 580), (462, 580), (470, 860), (380, 860)], [(575, 798), (620, 798), (620, 850), (575, 850)]],
             hidden=[[(300, 440), (360, 360), (450, 330), (640, 325), (780, 320), (850, 380), (875, 600), (870, 810),
                      (780, 860), (640, 900), (480, 930), (330, 930), (280, 860)]]),
        Part('Skirt', 'Skirt', 'Pelvis', (640, 630), 4,
             seeds=[(538, 650), (751, 634), (768, 690), (688, 762), (630, 751), (527, 721), (597, 741), (751, 733)],
             take=[[(455, 615), (770, 612), (812, 750), (760, 805), (750, 872), (636, 872), (610, 790), (470, 785),
                    (450, 700)]],
             hidden=[[(470, 598), (760, 598), (765, 640), (470, 640)]],
             fill='smear', axis=(0.0, 1.0)),
        Part('Thigh Left', 'ThighLeft', 'Pelvis', (768, 745), -7,
             seeds=[(772, 773), (779, 793)],
             take=[[(745, 740), (810, 740), (812, 818), (745, 818)]],
             mail=[[(728, 700), (808, 700), (812, 770), (740, 775)]]),
        Part('Shin Left', 'ShinLeft', 'Thigh Left', (785, 845), -8,
             seeds=[(796, 846), (757, 845), (709, 871), (746, 950), (765, 1037), (801, 1029), (712, 1045), (844, 1063),
                    (773, 1067), (806, 1095)],
             take=[[(685, 800), (835, 785), (840, 900), (815, 1000), (912, 1050), (912, 1125), (684, 1125),
                    (684, 1000), (684, 880)]],
             hidden=[[(748, 768), (815, 768), (820, 815), (748, 815)]],
             fill='smear', axis=(0.0, 1.0)),
        Part('Thigh Right', 'ThighRight', 'Pelvis', (527, 750), 3,
             seeds=[(524, 815), (542, 798), (462, 814)],
             take=[[(450, 778), (590, 778), (590, 840), (450, 840)]],
             mail=[[(470, 700), (592, 700), (596, 790), (462, 790)]]),
        Part('Shin Right', 'ShinRight', 'Thigh Right', (505, 868), 2,
             seeds=[(509, 883), (451, 852), (448, 996), (420, 1101), (426, 1120), (381, 1123), (464, 1154), (376, 1161),
                    (465, 1201)],
             take=[[(430, 806), (575, 806), (575, 960), (545, 1090), (535, 1230), (360, 1230), (378, 1090),
                    (385, 900)]],
             hidden=[[(462, 788), (586, 788), (586, 836), (462, 836)]],
             fill='smear', axis=(0.0, 1.0)),
        Part('Upper Arm Left', 'UpperArmLeft', 'Torso', (795, 405), -11,
             seeds=[(825, 464), (791, 481), (806, 506), (821, 523)],
             take=[[(770, 432), (852, 432), (845, 545), (770, 545)]],
             mail=[[(772, 380), (858, 380), (862, 450), (772, 450)], circle(795, 405, 36)]),
        Part('Forearm Left', 'ForearmLeft', 'Upper Arm Left', (845, 525), -12,
             seeds=[(851, 521), (869, 582), (917, 647), (884, 652), (879, 684), (917, 691), (903, 708)],
             take=[[(775, 462), (900, 462), (945, 615), (948, 725), (858, 725), (800, 560)]],
             hidden=[circle(845, 522, 48)],
             fill_seeds=[(851, 521)]),
        Part('Upper Arm Right', 'UpperArmRight', 'Torso', (425, 445), 8,
             seeds=[(407, 529), (470, 547), (394, 572)],
             take=[[(345, 480), (488, 480), (488, 570), (430, 606), (350, 600)]],
             mail=[[(355, 420), (470, 420), (478, 490), (345, 490)], circle(425, 445, 44),
                   [(352, 585), (440, 585), (432, 615), (362, 612)], circle(382, 580, 34)]),
        Part('Pauldron Right', 'Fixed', 'Torso', (444, 430), 9,
             seeds=[(444, 430), (531, 391), (585, 438)],
             take=[[(335, 345), (450, 300), (588, 296), (608, 440), (560, 500), (470, 522), (338, 470)]]),
        Part('Forearm Right', 'ForearmRight', 'Upper Arm Right', (382, 580), 11,
             seeds=[(336, 563), (359, 640), (343, 757), (393, 749), (404, 779), (371, 800), (387, 807), (351, 809),
                    (332, 809)],
             take=[[(305, 550), (440, 552), (440, 735), (420, 822), (300, 822), (298, 700)]]),
    ])

FACINGS = [REAR, FRONT]


# ----------------------------------------------------------------------------------------------------- raster helpers

def load_master(path):
    with Image.open(ROOT / path) as image:
        rgba = np.asarray(image.convert('RGBA'), np.float32) / 255
    # The painted interior is stored a shade under opaque; solid paint is solid.
    rgba[..., 3] = np.clip(rgba[..., 3] / SOLID_ALPHA, 0, 1)
    return rgba


def shape_mask(shapes, size):
    """Polygons and circles rasterised (supersampled 4x) to a coverage map."""
    h, w = size
    s = 4
    image = Image.new('L', (w * s, h * s), 0)
    draw = ImageDraw.Draw(image)
    for shape in shapes:
        if isinstance(shape, tuple) and shape and shape[0] == 'circle':
            _, x, y, r = shape
            draw.ellipse([(x - r) * s, (y - r) * s, (x + r) * s, (y + r) * s], fill=255)
        elif shape:
            draw.polygon([(x * s, y * s) for x, y in shape], fill=255)
    big = np.asarray(image, np.float32) / 255
    return big.reshape(h, s, w, s).mean(axis=(1, 3))


def disk(radius):
    r = int(math.ceil(radius))
    y, x = np.mgrid[-r:r + 1, -r:r + 1]
    return x * x + y * y <= radius * radius


def luminance(rgb):
    return rgb @ np.array([0.299, 0.587, 0.114], np.float32) * 255


def smooth_fill(rgb, known):
    """Pull-push: every unknown pixel takes a weighted blend of the known ones around it, coarse to fine."""
    weight = known.astype(np.float32)
    levels = [(rgb * weight[..., None], weight)]
    # Down to a single cell, which holds every known pixel, so no level is left with an empty cell to fill black.
    while max(levels[-1][1].shape) > 1:
        c, w = levels[-1]
        h, wd = w.shape
        h2, w2 = (h + 1) // 2, (wd + 1) // 2
        c = np.pad(c, ((0, h2 * 2 - h), (0, w2 * 2 - wd), (0, 0)))
        w = np.pad(w, ((0, h2 * 2 - h), (0, w2 * 2 - wd)))
        c = c.reshape(h2, 2, w2, 2, 3).sum(axis=(1, 3))
        w = w.reshape(h2, 2, w2, 2).sum(axis=(1, 3))
        levels.append((c, w))
    c, w = levels[-1]
    filled = c / np.maximum(w, 1e-6)[..., None]
    for c, w in reversed(levels[:-1]):
        h, wd = w.shape
        h2, w2 = filled.shape[:2]
        up = cv2.resize(filled, (w2 * 2, h2 * 2), interpolation=cv2.INTER_LINEAR)[:h, :wd]
        alpha = np.clip(w, 0, 1)[..., None]
        filled = c / np.maximum(w, 1e-6)[..., None] * alpha + up * (1 - alpha)
    return filled


def smear_fill(rgb, known, todo, axis, reach=260):
    """Copies the part's own paint into todo along axis from whichever side reaches it first, keeping each limb's
    cross-section - its edge outlines and its shading - as the hidden length is invented. A light blur along the way
    hides the repetition; what nothing reaches stays unfilled."""
    ax = np.array(axis, np.float32)
    ax /= np.linalg.norm(ax)
    h, w = known.shape
    out = rgb.copy()
    done = known.copy()
    left = todo & ~known
    if not left.any():
        return out, done
    ys, xs = np.nonzero(left)
    best = np.full(len(ys), np.inf, np.float32)
    colour = np.zeros((len(ys), 3), np.float32)
    soft = cv2.GaussianBlur(rgb * known[..., None], (0, 0), 1.2)
    softw = cv2.GaussianBlur(known.astype(np.float32), (0, 0), 1.2)
    for sign in (1.0, -1.0):
        step = ax * sign
        found = np.zeros(len(ys), bool)
        for k in range(1, reach):
            px = xs + step[0] * k
            py = ys + step[1] * k
            xi = np.clip(np.round(px).astype(int), 0, w - 1)
            yi = np.clip(np.round(py).astype(int), 0, h - 1)
            hit = ~found & known[yi, xi] & (k < best)
            if hit.any():
                # Sample a texel or two inside the paint, past its antialiased rim.
                inner_x = np.clip(np.round(px + step[0] * 2).astype(int), 0, w - 1)
                inner_y = np.clip(np.round(py + step[1] * 2).astype(int), 0, h - 1)
                use_inner = known[inner_y, inner_x]
                sx = np.where(use_inner, inner_x, xi)
                sy = np.where(use_inner, inner_y, yi)
                wgt = np.maximum(softw[sy, sx], 1e-6)
                colour[hit] = (soft[sy, sx] / wgt[:, None])[hit]
                best[hit] = k
                found |= hit
            if found.all():
                break
    reached = np.isfinite(best)
    out[ys[reached], xs[reached]] = colour[reached]
    done[ys[reached], xs[reached]] = True
    return out, done


@dataclass
class Layer:
    part: Part
    rgb: np.ndarray        # H x W x 3, straight colour
    alpha: np.ndarray      # H x W coverage
    own: np.ndarray        # painted pixels it took
    invented: np.ndarray   # pixels it gained out of sight


def outlines(facing, rgb, solid):
    """The painted lines between plates: dark and nearly colourless. The cape's deepest navy and the armour's blue
    shadows are dark too, but saturated, so they stay inside their regions. The facing's cuts close the few places
    where the painting runs two plates together."""
    lum = luminance(rgb)
    line = solid & (lum < LINE_LUMINANCE) & (rgb.max(axis=2) * 255 < LINE_CHANNEL)
    if facing.cuts:
        h, w = solid.shape
        image = Image.new('L', (w, h), 0)
        draw = ImageDraw.Draw(image)
        for cut in facing.cuts:
            draw.line(cut, fill=255, width=3)
        line |= (np.asarray(image) > 0) & solid
    return line


def segment(facing, rgb, solid, h, w):
    """Which part every painted pixel belongs to. The outlines split the paint into regions, one plate each; a part's
    seeds claim the regions they sit in, the rough polygons hand out the rest, and each outline goes to the part in
    front of those it separates, so a part that swings away takes its outline with it. Islands a part ends up with
    away from its plates go to the part around them."""
    parts = facing.parts
    n = len(parts)
    sorting = np.array([p.sorting for p in parts])
    line = outlines(facing, rgb, solid)
    regions, count = ndimage.label(solid & ~line)
    owner = np.full(count + 1, -1, np.int32)
    seeded = np.zeros(count + 1, bool)
    for i, part in enumerate(parts):
        for x, y in part.seeds:
            k = regions[int(round(y)), int(round(x))]
            if k == 0:
                raise SystemExit(f'{facing.name} {part.name}: seed ({x}, {y}) is on an outline')
            if owner[k] not in (-1, i):
                raise SystemExit(f'{facing.name} {part.name}: seed ({x}, {y}) is in {parts[owner[k]].name}\'s region')
            owner[k] = i
            seeded[k] = True
    # The rough polygons, front parts over back ones, vote for every region no seed claimed.
    rough = np.full((h, w), -1, np.int32)
    for i in sorted(range(n), key=lambda k: sorting[k]):
        if parts[i].take:
            rough[shape_mask(parts[i].take, (h, w)) > 0.5] = i
    flat = regions.ravel()
    order = np.argsort(flat, kind='stable')
    bounds = np.searchsorted(flat[order], np.arange(count + 2))
    votes = rough.ravel()
    for k in np.nonzero(owner[1:] < 0)[0] + 1:
        members = votes[order[bounds[k]:bounds[k + 1]]]
        members = members[members >= 0]
        if len(members):
            owner[k] = np.bincount(members, minlength=n).argmax()
    label = owner[regions]
    label[~solid | line] = -1
    lost = solid & ~line & (label < 0)
    if lost.any():
        _, (iy, ix) = ndimage.distance_transform_edt(label < 0, return_indices=True)
        label[lost] = label[iy[lost], ix[lost]]
    # Each outline to the frontmost part beside it.
    pending = line.copy()
    for i in sorted(range(n), key=lambda k: -sorting[k]):
        near = ndimage.binary_dilation(label == i, structure=disk(LINE_REACH)) & pending
        label[near] = i
        pending &= ~near
    if pending.any():
        _, (iy, ix) = ndimage.distance_transform_edt(label < 0, return_indices=True)
        label[pending] = label[iy[pending], ix[pending]]
    label[~solid] = -1
    # Islands: every piece of a part not joined to a seeded plate goes to the part it borders most.
    seeded_px = seeded[regions]
    for i in range(n):
        mine = label == i
        pieces, count_i = ndimage.label(mine, structure=np.ones((3, 3), bool))
        if count_i <= 1:
            continue
        anchored = np.unique(pieces[mine & seeded_px])
        for k in range(1, count_i + 1):
            if k in anchored:
                continue
            piece = pieces == k
            ring = ndimage.binary_dilation(piece, structure=np.ones((3, 3), bool)) & ~piece & (label >= 0) & ~mine
            if ring.any():
                label[piece] = np.bincount(label[ring], minlength=n).argmax()
    return label, line


def mail_texture(rgb, patch, size):
    """The facing's patch of painted chainmail mirrored into a seamless tile and repeated over the whole image,
    a shade darker: mail seen in the gap between plates sits in their shadow."""
    x0, y0, x1, y1 = patch
    tile = rgb[y0:y1, x0:x1]
    tile = np.concatenate([tile, tile[:, ::-1]], axis=1)
    tile = np.concatenate([tile, tile[::-1]], axis=0)
    h, w = size
    reps = (h // tile.shape[0] + 1, w // tile.shape[1] + 1, 1)
    return np.tile(tile, reps)[:h, :w] * 0.85


def seeds_region(facing, part, rgb, solid, line):
    """The regions a part's fill_seeds sit in."""
    regions, _ = ndimage.label(solid & ~line)
    mask = np.zeros(solid.shape, bool)
    for x, y in part.fill_seeds:
        mask |= regions == regions[int(round(y)), int(round(x))]
    return mask


def cut_facing(facing):
    rgba = load_master(facing.source)
    h, w = rgba.shape[:2]
    rgb, alpha = rgba[..., :3], rgba[..., 3]
    solid = alpha > CLEAR_ALPHA
    parts = facing.parts
    n = len(parts)
    sorting = np.array([p.sorting for p in parts])
    label, line = segment(facing, rgb, solid, h, w)
    mail = mail_texture(rgb, facing.mail_patch, (h, w))

    layers = {}
    for i in sorted(range(n), key=lambda k: (parts[k].clone is not None, k)):
        part = parts[i]
        own = label == i
        if part.copy:
            own |= (shape_mask(part.copy, (h, w)) > 0.5) & solid
        shape = own.copy()
        # A part only invents paint where a part drawn in front of it covers that spot at rest: the standing puppet
        # is then exactly the painting, and the invented paint shows only as what covered it moves away.
        front = (label >= 0) & (sorting[np.maximum(label, 0)] > part.sorting)
        if part.hidden:
            shape |= (shape_mask(part.hidden, (h, w)) > 0.5) & front
        mail_mask = (shape_mask(part.mail, (h, w)) > 0.5) & front if part.mail else np.zeros((h, w), bool)
        shape |= mail_mask
        # A little way under every neighbour in front, so a small turn never shows a cut edge.
        if own.any():
            shape |= ndimage.binary_dilation(own, structure=disk(UNDERLAP)) & front
        colour = rgb.copy()
        known = own & (alpha > 0.5)
        colour[mail_mask] = mail[mail_mask]
        if part.clone is not None:
            src_name, dx, dy, tone = part.clone
            src = layers[src_name]
            m = np.float32([[1, 0, dx], [0, 1, dy]])
            moved_rgb = cv2.warpAffine(src.rgb * src.alpha[..., None], m, (w, h))
            moved_a = cv2.warpAffine(src.alpha, m, (w, h))
            moved_rgb = moved_rgb / np.maximum(moved_a, 1e-6)[..., None] * tone
            take_clone = (moved_a > 0.5) & front
            colour[take_clone] = moved_rgb[take_clone]
            known |= take_clone
            shape |= take_clone
        todo = shape & ~known & ~mail_mask
        # The fill starts from plate paint, not from the outlines round it, unless it smears a limb's whole
        # cross-section, outline included.
        sources = known & ~line if part.fill == 'smooth' else known.copy()
        if part.fill_seeds:
            sources &= seeds_region(facing, part, rgb, solid, line)
        if todo.any() and sources.any():
            if part.fill == 'smear':
                colour, reached = smear_fill(colour, sources, todo, part.axis)
                rest = todo & ~reached
                if rest.any():
                    smooth = smooth_fill(colour, reached | mail_mask)
                    colour[rest] = smooth[rest]
            else:
                smooth = smooth_fill(colour, sources)
                if part.fill_colour is not None:
                    # Blends from the paint beside it into one plain plate colour, so a large hidden area takes
                    # the plate's tone rather than the average of every rivet and crease around it.
                    far = np.clip(ndimage.distance_transform_edt(~sources) / 24, 0, 1)[..., None]
                    smooth = smooth * (1 - far) + np.array(part.fill_colour, np.float32) / 255 * far
                colour[todo] = smooth[todo]
        layer_alpha = np.where(own, alpha, 0).astype(np.float32)
        invented = shape & ~own
        layer_alpha[invented] = 1.0
        # A painted outline round every invented silhouette edge.
        if invented.any():
            inside = ndimage.distance_transform_edt(shape)
            rim = invented & (inside <= DRAWN_OUTLINE + 1)
            t = np.clip((DRAWN_OUTLINE + 1 - inside) / 1.5, 0, 1)[..., None]
            colour = np.where(rim[..., None], colour * (1 - t) + OUTLINE * t, colour)
        layers[part.name] = Layer(part, colour, layer_alpha, own, invented)
    return rgba, label, [layers[p.name] for p in parts]


# ------------------------------------------------------------------------------------------------ runtime reduction

@dataclass
class Sprite:
    part: Part
    rgba: np.ndarray            # runtime texels, top row first
    pivot: tuple[float, float]  # Unity pivot, 0..1 from the bottom-left
    joint_world: tuple[float, float]
    origin: tuple[int, int]     # its top-left texel on the facing's runtime grid


def sole(layer):
    """The two points of a shin's boot that meet the floor, in world units from the knee: the heel a leg thrown forward
    lands on and the toe one swung back rolls onto - the lowest points of the boot's outline turned twenty degrees
    forward and thirty back. The bob stands the figure on whichever is lower."""
    solid = layer.alpha > 0.5
    ys, xs = np.nonzero(solid)
    top, bottom = ys.min(), ys.max()
    band = ys >= bottom - 0.3 * (bottom - top)
    points = np.stack([xs[band], ys[band] + 1], axis=1).astype(np.float32)
    hull = cv2.convexHull(points).reshape(-1, 2)
    jx, jy = layer.part.joint
    wx = (hull[:, 0] - jx) / SOURCE_PER_UNIT
    wy = (jy - hull[:, 1]) / SOURCE_PER_UNIT
    ends = []
    for degrees in (20.0, -30.0):
        r = math.radians(degrees)
        k = int(np.argmin(wx * math.sin(r) + wy * math.cos(r)))
        ends += [float(wx[k]), float(wy[k])]
    return tuple(ends)


def reduce_facing(facing, layers, size):
    """Every layer on the same runtime grid, cropped to what it paints. The grid starts at a whole number of GRID
    blocks from the floor point, so each texel's corners are fixed relative to the floor."""
    h, w = size
    fx, fy = facing.floor
    # The canvas runs from a whole number of GRID blocks left of and above the floor point to past the image.
    kx = int(math.ceil(fx / GRID)) + 1
    ky = int(math.ceil(fy / GRID)) + 1
    x0 = fx - GRID * kx
    y0 = fy - GRID * ky
    cols = int(math.ceil((w - x0) / GRID)) + 1
    rows = int(math.ceil((h - y0) / GRID)) + 1
    cw, ch = cols * GRID, rows * GRID
    tw, th = cols * GRID * PPU // int(SOURCE_PER_UNIT), rows * GRID * PPU // int(SOURCE_PER_UNIT)
    assert abs(tw - cw * SCALE) < 1e-6 and abs(th - ch * SCALE) < 1e-6
    ox, oy = int(round(-x0)), int(round(-y0))
    sprites = []
    for layer in layers:
        canvas = np.zeros((ch, cw, 4), np.float32)
        canvas[oy:oy + h, ox:ox + w, :3] = layer.rgb * layer.alpha[..., None]
        canvas[oy:oy + h, ox:ox + w, 3] = layer.alpha
        small = cv2.resize(canvas, (tw, th), interpolation=cv2.INTER_AREA)
        a = small[..., 3]
        ys, xs = np.nonzero(a > 1 / 255)
        if len(ys) == 0:
            raise SystemExit(f'{facing.name} {layer.part.name}: nothing painted')
        y_lo, y_hi = max(ys.min() - 1, 0), min(ys.max() + 2, th)
        x_lo, x_hi = max(xs.min() - 1, 0), min(xs.max() + 2, tw)
        crop = small[y_lo:y_hi, x_lo:x_hi].copy()
        straight = crop.copy()
        straight[..., :3] = crop[..., :3] / np.maximum(crop[..., 3:4], 1e-6)
        straight[..., :3] = np.where(crop[..., 3:4] > 0, straight[..., :3], 0)
        jx, jy = layer.part.joint
        # The joint in texels from the crop's top-left corner.
        u = (jx - x0) * SCALE - x_lo
        v = (jy - y0) * SCALE - y_lo
        pivot = (u / (x_hi - x_lo), 1 - v / (y_hi - y_lo))
        sprites.append(Sprite(layer.part, np.clip(straight, 0, 1), pivot,
                              ((jx - fx) / SOURCE_PER_UNIT, (fy - jy) / SOURCE_PER_UNIT), (x_lo, y_lo)))
    return sprites, (ox, oy, cw, ch, tw, th)


# ------------------------------------------------------------------------------------------------------ the writing

def write_tga(path, rgba, flash=False):
    """RGBA32 uncompressed, bottom-left origin, as BuildVanguardAssets writes them."""
    data = (np.clip(rgba, 0, 1) * 255 + 0.5).astype(np.uint8)
    h, w = data.shape[:2]
    if flash:
        data = data.copy()
        data[..., :3] = 255
        data[data[..., 3] == 0, :3] = 0
    bgra = data[::-1][..., [2, 1, 0, 3]]
    header = bytes([0, 0, 2] + [0] * 9) + w.to_bytes(2, 'little') + h.to_bytes(2, 'little') + bytes([32, 8])
    path.write_bytes(header + bgra.tobytes())


def read_guid(meta_path):
    if not meta_path.exists():
        return None
    match = re.search(r'^guid: ([0-9a-f]{32})$', meta_path.read_text(encoding='utf-8'), re.M)
    return match.group(1) if match else None


def texture_meta(template, guid, pivot):
    text = re.sub(r'^guid: [0-9a-f]{32}$', f'guid: {guid}', template, count=1, flags=re.M)
    text = re.sub(r'spritePivot: \{x: [^,]+, y: [^}]+\}', f'spritePivot: {{x: {pivot[0]:.8g}, y: {pivot[1]:.8g}}}', text,
                  count=1)
    # A fresh single sprite: no names carried over from the frame the settings came from.
    text = re.sub(r'internalIDToNameTable:\n(?:  - .*\n(?:    .*\n|      .*\n)*)*', 'internalIDToNameTable: []\n', text)
    text = re.sub(r'(  spriteSheet:\n    serializedVersion: 2\n)    sprites:\n(?:    - .*\n(?:      .*\n)*)*',
                  r'\1    sprites: []\n', text)
    text = re.sub(r'    nameFileIdTable:\n(?:      .*\n)*', '    nameFileIdTable: {}\n', text)
    return text


def fmt(value):
    text = f'{value:.7g}'
    return '0' if text in ('-0', '0') else text


def write_assets(sprites_by_facing, dry_run):
    template = META_TEMPLATE.read_text(encoding='utf-8')
    if 'spritePixelsToUnits: 128' not in template or 'textureFormat: 48' not in template:
        raise SystemExit('The template meta no longer carries the Vanguard import settings.')
    script_guid = read_guid(PUPPET_SCRIPT_META)
    refs = {}
    texture_bytes = 0
    files = []
    for facing, sprites, _ in sprites_by_facing:
        for sprite in sprites:
            for flash in (False, True):
                stem = f'PUP_Vanguard_{facing.name}_{sprite.part.file_stem()}' + ('_Flash' if flash else '')
                path = OUT_DIR / f'{stem}.tga'
                meta = path.with_suffix('.tga.meta')
                guid = read_guid(meta) or uuid.uuid4().hex
                refs[(facing.name, sprite.part.name, flash)] = guid
                h, w = sprite.rgba.shape[:2]
                # ASTC 4x4: 16 bytes a block of 4 x 4 texels.
                texture_bytes += ((w + 3) // 4) * ((h + 3) // 4) * 16
                files.append(path)
                if not dry_run:
                    OUT_DIR.mkdir(parents=True, exist_ok=True)
                    write_tga(path, sprite.rgba, flash)
                    meta.write_text(texture_meta(template, guid, sprite.pivot), encoding='utf-8', newline='\n')
    equipment = {k: read_guid(v) for k, v in EQUIPMENT.items()}
    lines = ['%YAML 1.1', '%TAG !u! tag:unity3d.com,2011:', '--- !u!114 &11400000', 'MonoBehaviour:',
             '  m_ObjectHideFlags: 0', '  m_CorrespondingSourceObject: {fileID: 0}', '  m_PrefabInstance: {fileID: 0}',
             '  m_PrefabAsset: {fileID: 0}', '  m_GameObject: {fileID: 0}', '  m_Enabled: 1', '  m_EditorHideFlags: 0',
             f'  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}', f'  m_Name: {ASSET_NAME}',
             '  m_EditorClassIdentifier: ']
    for facing, sprites, soles in sprites_by_facing:
        key = '_rear' if facing.name == 'Rear' else '_front'
        index = {s.part.name: i for i, s in enumerate(sprites)}
        lines += [f'  {key}:', '    Parts:']
        for sprite in sprites:
            part = sprite.part
            if part.parent is None:
                joint = sprite.joint_world
                parent = -1
            else:
                parent = index[part.parent]
                pj = sprites[parent].part.joint
                joint = ((part.joint[0] - pj[0]) / SOURCE_PER_UNIT, (pj[1] - part.joint[1]) / SOURCE_PER_UNIT)
            lines += [f'    - Name: {part.name}',
                      f'      Sprite: {{fileID: 21300000, guid: {refs[(facing.name, part.name, False)]}, type: 3}}',
                      f'      Flash: {{fileID: 21300000, guid: {refs[(facing.name, part.name, True)]}, type: 3}}',
                      f'      Parent: {parent}',
                      f'      Joint: {{x: {fmt(joint[0])}, y: {fmt(joint[1])}}}',
                      f'      Bone: {BONES.index(part.bone)}',
                      f'      Sorting: {part.sorting}']
        for side in ('right', 'left'):
            hand = getattr(facing, f'{side}_hand')
            grip = getattr(facing, f'{side}_grip')
            hj = sprites[index[hand]].part.joint
            g = ((grip[0] - hj[0]) / SOURCE_PER_UNIT, (hj[1] - grip[1]) / SOURCE_PER_UNIT)
            cap = side.capitalize()
            lines += [f'    {cap}Hand: {index[hand]}', f'    {cap}Grip: {{x: {fmt(g[0])}, y: {fmt(g[1])}}}',
                      f'    {cap}ItemSorting: {getattr(facing, side + "_item_sorting")}']
        for side, bone in (('Right', 'ShinRight'), ('Left', 'ShinLeft')):
            x, y, z, w = soles[bone]
            lines.append(f'    {side}Sole: {{x: {fmt(x)}, y: {fmt(y)}, z: {fmt(z)}, w: {fmt(w)}}}')
        lines.append('    FacesLeft: 0')
    for key, guid in equipment.items():
        lines.append(f'  {key}: {{fileID: 21300000, guid: {guid}, type: 3}}')
    asset = OUT_DIR / f'{ASSET_NAME}.asset'
    asset_meta = asset.with_suffix('.asset.meta')
    asset_guid = read_guid(asset_meta) or uuid.uuid4().hex
    folder_meta = OUT_DIR.with_suffix('.meta')
    if not dry_run:
        asset.write_text('\n'.join(lines) + '\n', encoding='utf-8', newline='\n')
        asset_meta.write_text(f'fileFormatVersion: 2\nguid: {asset_guid}\nNativeFormatImporter:\n  externalObjects: {{}}\n'
                              f'  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',
                              encoding='utf-8', newline='\n')
        if not folder_meta.exists():
            folder_meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\n'
                                   f'DefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n'
                                   f'  assetBundleVariant: \n', encoding='utf-8', newline='\n')
        # Nothing stale: a part renamed or dropped takes its files with it.
        keep = {p.resolve() for p in files}
        for old in OUT_DIR.glob('PUP_Vanguard_*.tga'):
            if old.resolve() not in keep:
                old.unlink()
                old.with_suffix('.tga.meta').unlink(missing_ok=True)
    return asset_guid, texture_bytes


# ------------------------------------------------------------------------------------------------------ the review

PALETTE = [(255, 80, 80), (80, 200, 255), (255, 210, 0), (120, 255, 90), (255, 110, 220), (0, 230, 190),
           (255, 150, 40), (175, 135, 255), (230, 255, 60), (255, 255, 255), (70, 140, 255), (255, 95, 145),
           (160, 255, 200), (255, 190, 150), (200, 120, 80)]


def write_review(directory, facing, rgba, label, layers, sprites):
    directory.mkdir(parents=True, exist_ok=True)
    h, w = label.shape
    over = (rgba[..., :3] * 0.55 * rgba[..., 3:4] * 255).astype(np.float32)
    tint = np.zeros((h, w, 3), np.float32)
    for i, layer in enumerate(layers):
        tint[label == i] = PALETTE[i % len(PALETTE)]
    m = label >= 0
    over[m] = over[m] * 0.55 + tint[m] * 0.45
    edges = np.zeros((h, w), bool)
    edges[:-1] |= label[:-1] != label[1:]
    edges[:, :-1] |= label[:, :-1] != label[:, 1:]
    over[edges] = (255, 255, 255)
    image = Image.fromarray(over.clip(0, 255).astype(np.uint8))
    draw = ImageDraw.Draw(image)
    for i, layer in enumerate(layers):
        x, y = layer.part.joint
        draw.ellipse([x - 5, y - 5, x + 5, y + 5], outline=(0, 0, 0), fill=PALETTE[i % len(PALETTE)])
        draw.text((x + 7, y - 6), layer.part.name, fill=(255, 255, 255))
    image.save(directory / f'{facing.name.lower()}-cuts.png')

    # Each finished layer at source size on a grey board, invented pixels tinted.
    tiles = []
    for layer in layers:
        ys, xs = np.nonzero(layer.alpha > 0.02)
        y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        board = np.zeros((y1 - y0, x1 - x0, 3), np.float32) + 0.33
        a = layer.alpha[y0:y1, x0:x1, None]
        board = board * (1 - a) + layer.rgb[y0:y1, x0:x1] * a
        tiles.append((layer.part.name, board))
    th = max(t.shape[0] for _, t in tiles)
    sheet = np.zeros((th + 20, sum(t.shape[1] + 10 for _, t in tiles), 3), np.float32) + 0.2
    x = 0
    sheet_img = None
    for name, t in tiles:
        sheet[20:20 + t.shape[0], x:x + t.shape[1]] = t
        x += t.shape[1] + 10
    sheet_img = Image.fromarray((sheet * 255).astype(np.uint8))
    draw = ImageDraw.Draw(sheet_img)
    x = 0
    for name, t in tiles:
        draw.text((x + 2, 4), name, fill=(255, 255, 255))
        x += t.shape[1] + 10
    sheet_img.save(directory / f'{facing.name.lower()}-layers.png')


def rest_check(directory, facing, rgba, sprites, grid):
    """The standing puppet recomposed from its reduced parts, in draw order, against the master reduced on the same
    grid: they should be the same picture. Writes reduced master | recomposed | difference x4, returns the mean
    difference over the figure (0-255)."""
    ox, oy, cw, ch, tw, th = grid
    h, w = rgba.shape[:2]
    canvas = np.zeros((ch, cw, 4), np.float32)
    canvas[oy:oy + h, ox:ox + w, :3] = rgba[..., :3] * rgba[..., 3:4]
    canvas[oy:oy + h, ox:ox + w, 3] = rgba[..., 3]
    master = cv2.resize(canvas, (tw, th), interpolation=cv2.INTER_AREA)
    puppet = np.zeros((th, tw, 4), np.float32)
    for s in sorted(sprites, key=lambda s: s.part.sorting):
        x, y = s.origin
        sh, sw = s.rgba.shape[:2]
        layer = s.rgba.copy()
        layer[..., :3] *= layer[..., 3:4]
        region = puppet[y:y + sh, x:x + sw]
        region[...] = layer + region * (1 - layer[..., 3:4])
    figure = master[..., 3] > 0.5
    error = np.abs(master[..., :3] - puppet[..., :3])[figure].mean() * 255
    board = np.zeros((th, tw, 3), np.float32) + 0.25
    views = [board * (1 - m[..., 3:4]) + m[..., :3] for m in (master, puppet)]
    views.append(np.clip(np.abs(master[..., :3] - puppet[..., :3]) * 4, 0, 1))
    strip = np.concatenate(views, axis=1)
    Image.fromarray((strip * 255).astype(np.uint8)).resize((strip.shape[1] * 3, strip.shape[0] * 3),
                                                           Image.NEAREST).save(directory / f'{facing.name.lower()}-rest.png')
    return error


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    parser.add_argument('--review', type=Path, help='write review images and manifest.json here')
    parser.add_argument('--dry-run', action='store_true', help='write nothing under Assets')
    args = parser.parse_args(argv)
    built = []
    for facing in FACINGS:
        rgba, label, layers = cut_facing(facing)
        soles = {layer.part.bone: sole(layer) for layer in layers if layer.part.bone in ('ShinRight', 'ShinLeft')}
        sprites, grid = reduce_facing(facing, layers, rgba.shape[:2])
        built.append((facing, sprites, soles))
        if args.review:
            write_review(args.review, facing, rgba, label, layers, sprites)
            error = rest_check(args.review, facing, rgba, sprites, grid)
            print(f'{facing.name}: standing puppet against the reduced master, mean difference {error:.2f} of 255')
        print(f'{facing.name}: {len(sprites)} parts, ' + ', '.join(f'{s.part.name} {s.rgba.shape[1]}x{s.rgba.shape[0]}'
                                                                   for s in sprites))
    asset_guid, texture_bytes = write_assets(built, args.dry_run)
    print(f'ASTC 4x4 texture memory, both facings with flash silhouettes: {texture_bytes / 1024:.1f} KiB')
    return 0


if __name__ == '__main__':
    sys.exit(main())
