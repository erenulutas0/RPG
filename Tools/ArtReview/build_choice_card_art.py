"""The run's choice cards painted (UI round): every level-up's offer, the forge's visits and the checkpoint's Extract or
climb, all shown by RunChoiceView as the same large touch cards.

The cards were the forged blank panel at a seventh of its size: a navy face in a thin gold line, the same for a green
card as for a gold one, the tier shown only on the card's name. This paints them in the in-run HUD's language
(build_hud_art.py: riveted dark iron, a gold inlay, ember corners), one treatment per tier, and the panel round them:

  - Frame (9-slice, the card's own image): an outline, a lit metal rim, an enamel inlay in the tier's colour between two
    grooves, a chamfer down to a dark face that the inlay lights from the rim inward, and at each corner a bracket
    wrapping both edges round a stone. The metals climb with the tier: green is iron with an emerald inlay, orange bronze
    and copper with amber and rivets, red blackened iron edged in gold with crimson enamel and pointed clasps, gold is
    gilded throughout, its white-gold inlay glowing, beaded brackets round sun-stones and a light falling from its top
    rim. The rim keeps to 12 units (the kind line stands 8 below the card's top), the brackets to 15 deep and 34 long
    (26 along the top right, where the kind line ends 28 in), clear of the icon's well (24 in) and of every label.
  - Aura (9-slice, laid past the card): gold's radiance with a star over each corner's stone; and the evolution's, which
    outranks gold: a furnace's heat round the whole card, horns of forged iron curling up out of its top corners with
    fire rising between them, spikes thrust from its foot, molten seams in its rim and furnace clamps at its corners.
  - Name plate (9-slice, behind the name): a glow of the tier's colour behind the name's start, a diamond of its stone
    and a rule of its metal along the name's foot, fading toward the far end: the line between name and description.
  - Description (9-slice, shared): a faint recess the description lies in, fading out toward the card's far end.
  - Well (9-slice, behind the icon): a recessed socket in the tier's metal round any square icon, its floor the navy the
    painted icons carry, and a lip laid over the icon's edge (shared) so a square icon sits in it rather than on it.
  - Sigil (what an empty well holds): a cut stone of the tier's colour in its prongs, the evolution's molten core, the
    forge's anvil, Extract's lit doorway with a coin on its threshold and Climb's three rising chevrons.
  - Press (9-slice, over the frame, painted light): the button's own graphic from now on, clear at rest; under a finger
    it lights the card from its rim in the treatment's colour, and it shades the card while it waits out its delay.
  - The panel: the surface's forged frame (the HUD's panel, heavier, riveted ember plates at its corners), a smoked plate
    between gold rules behind the title with an ember crest on it, and the backdrop's vignette.

The forge's cards are the HUD's own iron and gold on its navy (neutral, no tier); the checkpoint's Extract is cool steel
and moonstone, its Climb hot iron and embers.

One sheet, Assets/_Project/Art/UI/ChoiceCards/UI_ChoiceCards.png, at 100 texels a unit (the canvas's reference, so a
sprite is painted at its size on the 1080-wide canvas), imported as Multiple sprites with their rects, pivots and
9-slice borders in the .meta, bilinear, no mipmaps, ASTC 4x4 on Android, like the HUD's; and the set,
Assets/_Project/Art/UI/ChoiceCards/ChoiceCardArtSet.asset (Cryptforge.Art.ChoiceCardArtSet), with every sprite, the
press colours and the margins the parts keep round the labels and the icon. Painted with build_hud_art's height-field
canvas (4x supersampled, lit from the top left), packed and exported by build_skill_vfx. Deterministic: the same script
paints the same files byte for byte, and existing GUIDs and sprite ids are kept.

Usage:
    python -B Tools/ArtReview/build_choice_card_art.py                # paint the sheet, its meta and the set
    python -B Tools/ArtReview/build_choice_card_art.py --frames DIR   # also save every sprite as a PNG, for review
"""
import argparse
import math
import os
import uuid

import numpy as np
from PIL import Image

import build_hud_art as hud
import build_skill_vfx as skill
from build_hud_art import (GOLD, GOLD_DARK, GROOVE, IRON, IRON_DARK, OUTLINE, Canvas, Mat, band, dome, ember, inside,
                           periodic_noise, ramp, rivet, sd_box, sd_chamfer, sd_circle, sd_horn)
from build_skill_vfx import hex_rgb, smoothstep

ROOT = skill.ROOT
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "UI", "ChoiceCards")
SCRIPT_META = os.path.join(ROOT, "Assets", "_Project", "Scripts", "Art", "Unity", "ChoiceCardArtSet.cs.meta")
SHEET = "UI_ChoiceCards"
TPU = 100.0

NAVY = "#0C1224"  # the painted icons' background, and the HUD panels' face

# ----------------------------------------------------------------------------------------------------------------------
# The card's geometry on the canvas (RunChoiceView, the Gameplay scene): a 896-wide card, 252 tall as the scene has two
# or 190 when three are stacked. Its kind line spans x 488-868 and y 8-36 from its top, the name and description start
# 216 in, and the icon stands 34 in, 160 (or 120) square, centred on the card's height.

FRAME_B = 40  # the frame's corners and the depth its edges keep native
FRAME_S = 2 * FRAME_B + 8
RIM = 12.0  # the frame's depth into the card: the face starts here
STONE_AT = 11.5  # a corner stone's centre, in from both edges
BRACKET_DEPTH = 14.5
BRACKET_LONG = 34.0
BRACKET_TOP_RIGHT = 26.0  # along the top at the top right: the kind line's right end is 28 in


class Look:
    """One treatment: its metals, its enamel, its face and its stones."""

    def __init__(self, name, edge, rim, inlay, face, glow, glow_strength, stone, corner, press, inlay_glow=None,
                 inlay_heat=0.0, rule=None, plate_metal=None, top_light=None, bottom_light=None, well_metal=None,
                 bracket_edge=None):
        self.name = name
        self.edge, self.rim, self.inlay = edge, rim, inlay
        self.face, self.glow, self.glow_strength = face, glow, glow_strength
        self.stone = stone  # the stones' colour
        self.corner = corner  # the corner brackets' style
        self.press = press  # the press's light, (r, g, b, a)
        self.inlay_glow, self.inlay_heat = inlay_glow, inlay_heat
        self.rule = rule or inlay  # the name plate's rule
        self.plate_metal = plate_metal or rim  # the corner brackets' body
        self.bracket_edge = bracket_edge or edge  # and their lit edge
        self.top_light, self.bottom_light = top_light, bottom_light
        self.well_metal = well_metal or rim


def enamel(colour, spec=0.75, gloss=28):
    """A glassy enamel: its colour, and a specular of its own lightened colour."""
    c = hex_rgb(colour)
    light = np.clip(c * 0.5 + 0.55, 0, 1)
    return Mat(c, spec=spec, gloss=gloss, speccol="#%02X%02X%02X" % tuple(int(v * 255) for v in light))


STEEL_EDGE = Mat("#6A7080", spec=0.55, gloss=18, speccol="#D8E0F0")
BRONZE = Mat("#4C3828", spec=0.45, gloss=14, speccol="#F0C8A0")
COPPER = Mat("#B8682E", spec=0.85, gloss=20, speccol="#FFD0A0")
BLACK_IRON = Mat("#26222C", spec=0.35, gloss=12, speccol="#D8D0E0")
GILT = Mat("#C8902E", spec=1.0, gloss=24, speccol="#FFF0C0")
GILT_BRIGHT = Mat("#EAB94E", spec=1.0, gloss=26, speccol="#FFF6D8")
WHITE_GOLD = Mat("#F6D880", spec=1.0, gloss=30, speccol="#FFFFFF")
FORGE_IRON = Mat("#3A3236", spec=0.35, gloss=12, speccol="#F0C8B0")
SOOT_IRON = Mat("#1E1A1C", spec=0.25, gloss=10, speccol="#E0B0A0")
SILVER = Mat("#A0ACBE", spec=0.9, gloss=24, speccol="#F4F8FF")
COOL_IRON = Mat("#2E3442", spec=0.32, gloss=14, speccol="#C8D6F0")
WARM_IRON = Mat("#362E2C", spec=0.32, gloss=14, speccol="#F0D0C0")
MOLTEN = Mat("#E8641E", spec=0.2, gloss=10)
EMBER_ENAMEL = Mat("#C8461C", spec=0.6, gloss=24, speccol="#FFD8B0")
# The brackets' metals, a step lighter than the rims they stand on, so they read as pieces of their own.
STEEL = Mat("#575D6C", spec=0.5, gloss=16, speccol="#D8E0F0")
STEEL_LIGHT = Mat("#8E95A6", spec=0.6, gloss=18, speccol="#F0F4FF")
COPPER_DARK = Mat("#7C4622", spec=0.6, gloss=16, speccol="#FFD0A0")
COPPER_LIGHT = Mat("#D88A48", spec=0.9, gloss=20, speccol="#FFE0C0")
BLUE_STEEL = Mat("#46506A", spec=0.5, gloss=16, speccol="#D8E4FF")

RARITY = ("#4CC36A", "#F08A24", "#E0393E", "#F5C542")  # RunChoiceView.RarityColours, green to gold

LOOKS = {
    "Green": Look("Green", STEEL_EDGE, IRON, enamel("#22A04A"), "#09140F", "#1C6A38", 0.55, "#4CC36A", "plain",
                  (0.55, 1.0, 0.68, 0.85), inlay_glow="#2ED060", inlay_heat=0.08, plate_metal=STEEL,
                  bracket_edge=STEEL_LIGHT),
    "Orange": Look("Orange", COPPER, BRONZE, enamel("#E2741A"), "#130B05", "#80400E", 0.5, "#FF9A30", "studded",
                   (1.0, 0.74, 0.40, 0.85), inlay_glow="#FF8A2A", inlay_heat=0.10, plate_metal=COPPER_DARK,
                   bracket_edge=COPPER_LIGHT),
    "Red": Look("Red", GOLD_DARK, BLACK_IRON, enamel("#C21E2A"), "#12060A", "#801420", 0.55, "#FF4048", "clasped",
                (1.0, 0.50, 0.50, 0.85), inlay_glow="#FF3A40", inlay_heat=0.16, bracket_edge=GOLD),
    "Gold": Look("Gold", GILT_BRIGHT, GILT, WHITE_GOLD, "#130C04", "#8A5C12", 0.6, "#FFD860", "beaded",
                 (1.0, 0.90, 0.55, 0.9), inlay_glow="#FFE6A0", inlay_heat=0.25, rule=GILT_BRIGHT,
                 top_light="#6E4C12", bracket_edge=WHITE_GOLD),
    "Evolution": Look("Evolution", MOLTEN, SOOT_IRON, MOLTEN, "#120704", "#A8380C", 0.65, "#FF7A2A", "furnace",
                      (1.0, 0.60, 0.28, 0.9), inlay_glow="#FFB040", inlay_heat=1.0, rule=Mat("#F08A30", spec=0.5),
                      plate_metal=FORGE_IRON, bottom_light="#702808", well_metal=FORGE_IRON),
    "Neutral": Look("Neutral", GOLD, IRON, GOLD, NAVY, "#3A2C14", 0.25, "#E8561A", "ember", (1.0, 0.84, 0.50, 0.8)),
    "Extract": Look("Extract", SILVER, COOL_IRON, enamel("#5E92D8"), "#09111F", "#1E4A80", 0.5, "#A8DCFF", "moonstone",
                    (0.62, 0.82, 1.0, 0.85), inlay_glow="#8CC8FF", inlay_heat=0.12, rule=SILVER,
                    plate_metal=BLUE_STEEL),
    "Climb": Look("Climb", GOLD, WARM_IRON, EMBER_ENAMEL, "#130908", "#802C10", 0.55, "#FF6A22", "ember",
                  (1.0, 0.60, 0.32, 0.85), inlay_glow="#FF6A22", inlay_heat=0.5),
}
TIERS = ("Green", "Orange", "Red", "Gold")


def wash(cv, rgb, alpha):
    """Straight colour laid over what the canvas holds at an alpha (source-over), for the flat sprites. Canvas.lay mixes a
    layer's colour with what lies under it by its coverage, which is right for the lit metal's bands but darkens a first
    layer laid at a partial alpha (a glow, a veil) by that alpha a second time; a wash keeps its colour at any alpha."""
    a = np.clip(alpha, 0, 1).astype(np.float32)
    rgb = np.broadcast_to(np.asarray(rgb, np.float32), cv.albedo.shape)
    out = a + cv.alpha * (1 - a)
    cv.albedo = (rgb * a[..., None] + cv.albedo * (cv.alpha * (1 - a))[..., None]) / np.maximum(out, 1e-6)[..., None]
    cv.alpha = out


# ----------------------------------------------------------------------------------------------------------------------
# Stones.


def cabochon(cv, cx, cy, r, colour, base, heat=0.75):
    """A polished stone set in metal: its colour lit from inside, brightest below its middle, a glint at its top left."""
    sd = sd_circle(cv.X, cv.Y, cx, cy, r)
    cv.lay(inside(sd - 0.8) * (1 - inside(sd)), OUTLINE, height=np.full_like(sd, base))
    t = np.clip(-sd / r, 0, 1)
    c = hex_rgb(colour)
    cv.lay(inside(sd), Mat(c * 0.32, spec=1.0, gloss=46), height=base + r * 0.55 * np.sqrt(t * (2 - t)))
    dx, dy = (cv.X - cx) / r, (cv.Y - cy) / r
    core = np.clip(1 - np.hypot(dx + 0.1, dy - 0.28) * 1.05, 0, 1)
    cv.emit += c * (heat * 0.95 * core ** 1.1 * inside(sd))[..., None]
    cv.emit += np.clip(c * 0.4 + 0.6, 0, 1) * (heat * 0.35 * np.clip(1 - np.hypot(dx + 0.05, dy - 0.35) * 2.4, 0, 1) ** 2
                                                * inside(sd))[..., None]
    # The light it throws on the metal round it.
    d = np.hypot(cv.X - cx, cv.Y - cy) / r
    cv.emit += c * (heat * 0.16 * np.exp(-((d - 1) / 0.9) ** 2) * (1 - inside(sd)))[..., None]


def faceted_stone(cv, cx, cy, r, colour, base, heat=0.6, facets=8, seed=1):
    """A cut stone seen from above: a flat table, a ring of kite facets and a ring of star facets sloping to an
    octagonal girdle, each facet its own shade of the stone (light caught inside it), the facets' meeting lines lit,
    lit from the top left with a hard specular. Returns its coverage."""
    X, Y = cv.X - cx, cv.Y - cy
    c = hex_rgb(colour)
    planes = []
    for k in range(facets):  # the crown's main facets, from the girdle
        a = (k + 0.5) * 2 * math.pi / facets
        planes.append(1.2 * (r - (X * math.cos(a) + Y * math.sin(a))))
    for k in range(facets):  # the star facets between them, nearer the table
        a = k * 2 * math.pi / facets
        planes.append(0.58 * r + 0.62 * (0.80 * r - (X * math.cos(a) + Y * math.sin(a))))
    stack = np.stack(planes)
    table_h = 0.6 * r
    order = np.sort(stack, axis=0)
    lowest = np.minimum(order[0], table_h)
    second = np.minimum(np.where(order[0] < table_h, order[1], order[0]), table_h)
    which = np.where(order[0] < table_h, stack.argmin(0), -1)
    girdle_d = stack[:facets].min(0) / 1.2  # inward distance to the octagon's edge
    cover = inside(-girdle_d)
    cv.lay(inside(-(girdle_d + 1.3)) * (1 - cover), OUTLINE, height=np.full_like(X, base))
    rng = np.random.default_rng(seed)
    values = rng.uniform(0.22, 1.0, 2 * facets)
    value = np.where(which < 0, 0.82, values[np.clip(which, 0, None)])
    angle = np.arctan2(Y, X)
    toward = 0.5 + 0.5 * np.cos(angle - math.radians(225))  # the top left faces the light
    value = value * (0.45 + 0.7 * toward) * np.where(which < 0, 1.0, 1.0)
    albedo = c[None, None, :] * (0.18 + 0.85 * value)[..., None]
    cv.lay(cover, Mat(c * 0.5, spec=1.0, gloss=70), height=base + lowest * 0.85, albedo=albedo)
    rad = np.hypot(X, Y) / r
    cv.emit += c * (heat * 0.5 * np.clip(1 - rad, 0, 1) ** 0.7 * cover)[..., None]
    # Where two facets meet, a fine bright line.
    edge = np.exp(-((second - lowest) / 0.35) ** 2) * cover * (rad > 0.05)
    cv.emit += np.clip(c * 0.5 + 0.55, 0, 1) * (0.28 * edge)[..., None]
    return cover


def sparkle(cv, cx, cy, length, width=0.7, strength=1.0):
    """A four-pointed glint's coverage."""
    X, Y = np.abs(cv.X - cx), np.abs(cv.Y - cy)
    ray = np.exp(-(Y / width) ** 2) * np.clip(1 - X / length, 0, 1) ** 2 + \
        np.exp(-(X / width) ** 2) * np.clip(1 - Y / length, 0, 1) ** 2
    core = np.exp(-(np.hypot(X, Y) / (width * 1.8)) ** 2)
    return np.clip(strength * np.maximum(ray, core), 0, 1)


# ----------------------------------------------------------------------------------------------------------------------
# The card frame.


def corner_piece(cv, look, left, top):
    """A corner's bracket: two bars of the treatment's metal wrapping the corner along both edges, standing proud of the
    rim, and a boss at the corner with its stone; each style ends its bars its own way."""
    s = FRAME_S
    X, Y = cv.X, cv.Y
    style = look.corner

    def uv(x, y):
        return (x if left else s - x), (y if top else s - y)

    def at(u, v):
        return (u if left else s - u), (v if top else s - v)

    depth = 16.0 if style == "furnace" else BRACKET_DEPTH
    h_len = BRACKET_TOP_RIGHT if (top and not left) else BRACKET_LONG
    v_len = BRACKET_LONG
    if style == "furnace":
        h_len, v_len = min(h_len, 30.0), 30.0
    vc = (0.8 + depth) / 2

    def bars(x, y):
        u, v = uv(x, y)
        if style == "clasped":
            hb = np.maximum(sd_box(u, v, 0.8, 0.8, h_len, depth), ((u - h_len) + np.abs(v - vc)) / math.sqrt(2))
            vb = np.maximum(sd_box(u, v, 0.8, 0.8, depth, v_len), ((v - v_len) + np.abs(u - vc)) / math.sqrt(2))
        else:
            hb = sd_chamfer(u, v, 0.8, 0.8, h_len, depth, 4.0)
            vb = sd_chamfer(u, v, 0.8, 0.8, depth, v_len, 4.0)
        return np.minimum(hb, vb)

    def boss(x, y):
        u, v = uv(x, y)
        size = 24.0 if style == "furnace" else 22.0
        return sd_chamfer(u, v, 1.0, 1.0, size, size, 3.8)

    # The cast shadow, down and right onto the rim and the face.
    shadow = np.minimum(bars(X - 1.0, Y - 1.6), boss(X - 1.0, Y - 1.6))
    body = np.minimum(bars(X, Y), boss(X, Y))
    cv.darken(0.55 * np.clip(1 - (shadow + 0.5) / 2.4, 0, 1) * (1 - inside(body)))
    edge = look.bracket_edge
    bd = -bars(X, Y)
    cv.lay(band(bd, 0.0, 1.0), OUTLINE, height=np.full_like(bd, 0.9))
    cv.lay(band(bd, 1.0, 2.3), edge, height=1.1 + dome(bd, 1.0, 2.3, 0.7))
    cv.lay(band(bd, 2.3, 1e9), look.plate_metal, height=1.7 + dome(np.clip(bd, 0, 9.0), 2.3, 9.0, 0.9))
    if style == "furnace":
        cv.glow("#FF6A1E", 0.55 * band(bd, 1.0, 2.3))
    # The boss at the corner, a step higher.
    pd = -boss(X, Y)
    cv.lay(band(pd, 0.0, 1.0), OUTLINE, height=np.full_like(pd, 2.0))
    cv.lay(band(pd, 1.0, 2.8), edge, height=2.2 + dome(pd, 1.0, 2.8, 0.9))
    cv.lay(band(pd, 2.8, 1e9), look.plate_metal, height=np.full_like(pd, 3.0) + 0.3 * smoothstep(2.8, 8, pd))
    sx, sy = at(STONE_AT, STONE_AT)
    if style == "furnace":
        cx, cy = at(12.5, 12.5)
        # A grille onto the fire: three slots glowing through the clamp, and bolts at the bars' ends.
        for k in (-1, 0, 1):
            slot = sd_box(X, Y, cx + k * 4.2 - 1.1, cy - 5.0, cx + k * 4.2 + 1.1, cy + 5.0, 0.8)
            cv.lay(inside(slot + 0.7) * (1 - inside(slot)), OUTLINE, height=np.full_like(pd, 2.6))
            hot = inside(slot)
            cv.lay(hot, Mat("#3A0A04"), height=np.full_like(pd, 1.6))
            depth_ = np.clip((Y - (cy - 5.0)) / 10.0, 0, 1)
            cv.glow("#E8501A", 0.95 * hot)
            cv.glow("#FFC45A", 0.85 * hot * smoothstep(0.3, 1.0, depth_))
        cv.glow("#C8401A", 0.25 * np.exp(-(np.maximum(-pd, 0) / 2.2) ** 2) * (1 - inside(-pd)))
        for u, v in ((h_len - 5.0, vc), (vc, v_len - 5.0)):
            bx, by = at(u, v)
            rivet(cv, bx, by, 1.8, 2.2, mat=IRON)
        return
    if style == "ember":
        ember(cv, sx, sy, 4.2, 3.1, heat=1.25 if look.name == "Climb" else 1.0)
    elif style == "beaded":
        cabochon(cv, sx, sy, 5.2, look.stone, 3.1, heat=1.15)
    elif style == "clasped":
        faceted_stone(cv, sx, sy, 5.4, look.stone, 3.1, heat=0.9, seed=5)
    else:
        cabochon(cv, sx, sy, 4.4, look.stone, 3.1, heat=0.9 if style == "moonstone" else 0.85)
    if style == "beaded":
        # A white-gold line inlaid down each bar's spine, and a pearl where it ends.
        u, v = uv(X, Y)
        spine = np.clip(1 - np.abs(v - vc) / 0.75, 0, 1) * smoothstep(22.5, 24.0, u) * (1 - smoothstep(h_len - 7.5, h_len - 6.0, u))
        spine = np.maximum(spine, np.clip(1 - np.abs(u - vc) / 0.75, 0, 1) * smoothstep(22.5, 24.0, v) *
                           (1 - smoothstep(v_len - 7.5, v_len - 6.0, v)))
        cv.lay(spine, WHITE_GOLD, height=1.7 + 1.0 + 0.4 * spine)
        for u_, v_ in ((h_len - 4.2, vc), (vc, v_len - 4.2)):
            bx, by = at(u_, v_)
            rivet(cv, bx, by, 1.9, 2.4, mat=WHITE_GOLD, rise=1.3)
    elif style in ("studded", "ember", "moonstone"):
        metal = COPPER if style == "studded" else (SILVER if style == "moonstone" else GOLD)
        for u, v in ((h_len - 5.5, vc), (vc, v_len - 5.5)):
            bx, by = at(u, v)
            rivet(cv, bx, by, 1.6, 2.2, mat=metal)
    elif style == "clasped":
        # A gold line down each clasp's spine, toward its point.
        for horizontal in (True, False):
            u, v = uv(X, Y)
            if horizontal:
                spine = np.clip(1 - np.abs(v - vc) / 0.8, 0, 1) * (u > 24) * (u < h_len - 4)
            else:
                spine = np.clip(1 - np.abs(u - vc) / 0.8, 0, 1) * (v > 24) * (v < v_len - 4)
            cv.lay(spine, GOLD, height=1.7 + 1.2 + 0.4 * spine)


def card_frame(look):
    s, b = FRAME_S, FRAME_B
    cv = Canvas(s, s)
    X, Y = cv.X, cv.Y
    d = -sd_chamfer(X, Y, 0.25, 0.25, s - 0.25, s - 0.25, 4.0)
    cv.lay(band(d, 0.0, 1.1), OUTLINE, height=np.full_like(d, 0.2))
    cv.lay(band(d, 1.1, 2.6), look.edge, height=0.2 + dome(d, 1.1, 2.6, 0.8))
    cv.lay(band(d, 2.6, 6.4), look.rim, height=0.6 + dome(d, 2.6, 6.4, 1.8))
    cv.lay(band(d, 6.4, 7.0), GROOVE, height=np.full_like(d, 0.1))
    cv.lay(band(d, 7.0, 10.4), look.inlay, height=dome(d, 7.0, 10.4, 1.3))
    cv.lay(band(d, 10.4, 11.0), GROOVE, height=np.zeros_like(d))
    cv.lay(band(d, 11.0, RIM), IRON_DARK if look.rim is not GILT else GOLD_DARK, height=ramp(d, 11.0, RIM, 0.6, 0.0))
    # The face: dark, toward the tier's hue, lit by the inlay from the rim inward and flat in the middle, so it
    # stretches; gold also lit from its top rim, the evolution from its foot.
    centre = hex_rgb(look.face)
    face = np.broadcast_to(centre, X.shape + (3,)).copy()
    within = 1 - smoothstep(b - 12.0, b - 2.0, d)
    lit = look.glow_strength * np.exp(-np.maximum(d - RIM, 0) / 7.0) * within
    face += (hex_rgb(look.glow) - centre) * lit[..., None]
    if look.top_light:
        t = np.exp(-np.maximum(Y - RIM, 0) / 10.0) * (1 - smoothstep(b - 12.0, b - 2.0, Y))
        face += (hex_rgb(look.top_light) - centre) * (0.8 * t)[..., None]
    if look.bottom_light:
        t = np.exp(-np.maximum(s - Y - RIM, 0) / 10.0) * (1 - smoothstep(b - 12.0, b - 2.0, s - Y))
        face += (hex_rgb(look.bottom_light) - centre) * (0.8 * t)[..., None]
    cv.lay(band(d, RIM, 1e9), Mat(look.face), height=np.zeros_like(d), albedo=face)
    occlusion = 0.42 * (1 - smoothstep(RIM, RIM + 4.5, d)) * (d >= RIM - 0.5)
    cv.darken(occlusion)
    if look.inlay_glow:
        inlay = band(d, 7.0, 10.4)
        cv.glow(look.inlay_glow, look.inlay_heat * inlay * (0.55 + 0.45 * np.sin(np.pi * np.clip((d - 7) / 3.4, 0, 1))))
        cv.glow(look.inlay_glow, 0.35 * look.inlay_heat * np.exp(-((d - 8.7) / 3.0) ** 2) * (1 - inlay))
    if look.name == "Evolution":
        # Molten seams cracking the rim's iron, running along it, and the heat on the face beside the rim.
        seam = np.exp(-((d - 4.5) / 0.45) ** 2) * band(d, 2.6, 6.4)
        cv.glow("#FF7A2A", 0.9 * seam)
        cv.glow("#FFD070", 0.5 * seam ** 2)
        cv.glow("#E0501A", 0.2 * np.exp(-np.maximum(d - RIM, 0) / 3.0) * (d > RIM) * within)
    for left in (True, False):
        for top in (True, False):
            corner_piece(cv, look, left, top)
    return cv.shade().image(), (b, b, b, b), (0.5, 0.5)


# ----------------------------------------------------------------------------------------------------------------------
# Auras: past the card's rect, laid over its corners. An outset is (left, bottom, right, top) in canvas units: the stacked
# cards stand 28 apart, so an aura keeps to 12 above and below a card, and 48 lie between a card's sides and the
# surface's edge (30 to its frame's face), so the evolution's reaches 26 out at its sides.

GOLD_OUT = (14, 12, 14, 12)
EVO_OUT = (26, 12, 26, 12)
AURA_IN = 30  # how far an aura's corners reach into the card


class AuraCanvas:
    """A canvas round a card's corner zones: the card's rect inside it at the outset, the sprite's borders, and the
    distance outside the card's rect (negative inside) and to the sprite's own edge."""

    def __init__(self, outset):
        l, b, r, t = outset
        self.outset = outset
        self.w, self.h = l + r + 2 * AURA_IN + 8, t + b + 2 * AURA_IN + 8
        self.cv = Canvas(self.w, self.h)
        X, Y = self.cv.X, self.cv.Y
        self.x0, self.y0, self.x1, self.y1 = l, t, self.w - r, self.h - b  # the card, image order
        self.card = sd_chamfer(X, Y, self.x0, self.y0, self.x1, self.y1, 4.0)
        self.edge = np.minimum(np.minimum(X, Y), np.minimum(self.w - X, self.h - Y))
        self.border = (l + AURA_IN, b + AURA_IN, r + AURA_IN, t + AURA_IN)
        # 1 inside a corner's zone, 0 in the edges' middles that the slicing stretches: whatever a corner holds (a star's
        # rays, a glint, a flame) must fade out before it, or it is smeared along the whole card.
        bl, bb, br, bt = self.border
        across = np.maximum(1 - smoothstep(bl - 5.0, bl - 1.0, X), smoothstep(self.w - br + 1.0, self.w - br + 5.0, X))
        down = np.maximum(1 - smoothstep(bt - 5.0, bt - 1.0, Y), smoothstep(self.h - bb + 1.0, self.h - bb + 5.0, Y))
        self.corners = across * down

    def corner(self, left, top):
        return (self.x0 if left else self.x1), (self.y0 if top else self.y1)

    def at(self, left, top, u, v):
        """A point u in from the corner along x and v along y (negative: outside the card)."""
        cx, cy = self.corner(left, top)
        return (cx + u if left else cx - u), (cy + v if top else cy - v)

    def glow(self, near, far, near_alpha, far_alpha):
        """A glow beyond the card's edge: a hot band against it and a wide halo, gone before the sprite's edge."""
        outside = np.clip(self.card, 0, None)
        gone = smoothstep(0.5, 3.0, self.edge)
        start = np.clip(self.card + 1.0, 0, 1)
        return (near_alpha * np.exp(-outside / near) * start * gone, far_alpha * np.exp(-outside / far) * start * gone)


def sparkle_diag(cv, cx, cy, length, width):
    X, Y = cv.X - cx, cv.Y - cy
    u, v = np.abs(X + Y) / math.sqrt(2), np.abs(X - Y) / math.sqrt(2)
    return np.clip(np.exp(-(v / width) ** 2) * np.clip(1 - u / length, 0, 1) ** 2 +
                   np.exp(-(u / width) ** 2) * np.clip(1 - v / length, 0, 1) ** 2, 0, 1)


def gold_aura():
    a_ = AuraCanvas(GOLD_OUT)
    cv = a_.cv
    near, far = a_.glow(2.6, 6.5, 0.85, 0.45)
    rgb = np.broadcast_to(hex_rgb("#FFB838"), cv.X.shape + (3,)).copy()
    alpha = np.clip(far + near * (1 - far), 0, 1)
    rgb = rgb + (hex_rgb("#FFE28A") - rgb) * (near / np.maximum(alpha, 1e-4))[..., None]
    # A star over each corner's sun-stone, its long rays along the rims, and two small glints beside it.
    for left in (True, False):
        for top in (True, False):
            cx, cy = a_.at(left, top, STONE_AT, STONE_AT)
            a = sparkle(cv, cx, cy, 24.0, 1.0)
            a = np.maximum(a, 0.85 * sparkle_diag(cv, cx, cy, 10.0, 0.85))
            for (u, v), size in (((30.0, -6.0), 5.0), ((-7.0, 27.0), 4.0), ((-9.0, -5.0), 3.2)):
                gx, gy = a_.at(left, top, u, v)
                a = np.maximum(a, sparkle(cv, gx, gy, size, 0.55))
            a = a * smoothstep(0.5, 2.5, a_.edge) * a_.corners
            white = np.clip(a * 1.3, 0, 1)
            rgb = rgb * (1 - white[..., None]) + hex_rgb("#FFF8E0") * white[..., None]
            alpha = np.maximum(alpha, a)
    wash(cv, rgb, alpha)
    return cv.flat().image(), a_.border, (0.5, 0.5)


def flame_tongue(cv, x, base_y, height, width, lean):
    """A tongue of fire rising from base_y (image rows, up is smaller): its coverage, and how far up it each texel is."""
    X, Y = cv.X, cv.Y
    t = np.clip((base_y - Y) / height, 0, 1)  # 0 at its root, 1 at its tip
    centre = x + lean * t ** 1.6 * height
    half = width * (1 - t) ** 0.75 * (0.65 + 0.35 * np.sin(np.pi * np.clip(t * 1.3, 0, 1)))
    cover = np.clip(1 - np.abs(X - centre) / np.maximum(half, 1e-3), 0, 1) ** 0.6 * (t < 1)
    cover *= np.clip((base_y + 2.5 - Y) / 3.0, 0, 1)
    return cover, t


FIRE = [(0.0, (1.0, 0.97, 0.80)), (0.25, (1.0, 0.82, 0.38)), (0.55, (1.0, 0.52, 0.14)), (0.8, (0.86, 0.24, 0.08)),
        (1.0, (0.55, 0.10, 0.04))]


def fire_colour(t):
    ts = [k for k, _ in FIRE]
    return np.stack([np.interp(t, ts, [c[i] for _, c in FIRE]) for i in range(3)], -1).astype(np.float32)


def lay_fire(fire_rgb, fire_a, cover, t):
    a = cover * np.clip(1.15 - t * 0.6, 0, 1)
    return fire_rgb * (1 - a[..., None]) + fire_colour(t) * a[..., None], a + fire_a * (1 - a)


def evolution_aura():
    a_ = AuraCanvas(EVO_OUT)
    cv = a_.cv
    X, Y = cv.X, cv.Y
    # The furnace's heat round the card: a hot band against it and a wide red halo.
    near, far = a_.glow(2.4, 7.0, 0.95, 0.66)
    alpha = np.clip(far + near * (1 - far), 0, 1)
    rgb = np.broadcast_to(hex_rgb("#D83A12"), X.shape + (3,)).copy()
    rgb = rgb + (hex_rgb("#FFB040") - rgb) * (near / np.maximum(alpha, 1e-4))[..., None]
    fire_a = np.zeros_like(X)
    fire_rgb = np.zeros(X.shape + (3,), np.float32)
    for left in (True, False):
        side = 1 if left else -1  # +1 points into the card along x
        # Fire rising off the top rim at the corner, inside the horn's sweep.
        for u, hgt, wid, lean in ((13.0, 11.5, 3.6, -0.12), (20.5, 10.0, 3.2, 0.10), (27.5, 7.5, 2.6, 0.18),
                                  (6.5, 8.0, 2.8, -0.25)):
            x, y = a_.at(left, True, u, 1.5)
            cover, t = flame_tongue(cv, x, y, hgt, wid, lean * side)
            fire_rgb, fire_a = lay_fire(fire_rgb, fire_a, cover, t)
        # Embers above the fire.
        for u, v, r in ((17.0, -10.0, 0.9), (24.0, -11.0, 0.7), (10.0, -11.0, 0.65), (29.0, -8.0, 0.6),
                        (-4.0, -9.0, 0.7)):
            ex, ey = a_.at(left, True, u, v)
            e = np.clip(1 - np.hypot(X - ex, Y - ey) / r, 0, 1) ** 0.7
            fire_rgb = fire_rgb * (1 - e[..., None]) + hex_rgb("#FFD27A") * e[..., None]
            fire_a = e + fire_a * (1 - e)
    fire_a = fire_a * smoothstep(0.3, 1.5, a_.edge) * a_.corners
    rgb = rgb * (1 - fire_a[..., None]) + fire_rgb * fire_a[..., None]
    alpha = fire_a + alpha * (1 - fire_a)
    wash(cv, rgb, alpha)
    cv.flat()
    flat = cv.rgba.copy()
    # The horns and the spikes: forged iron sweeping out past the card's sides, lit, back-lit by the heat, on a canvas of
    # their own so they shade as metal.
    metal = Canvas(a_.w, a_.h)
    MX, MY = metal.X, metal.Y
    halo = np.zeros_like(MX)
    for left in (True, False):
        horn = [a_.at(left, True, 10.0, 2.0), a_.at(left, True, -1.0, -8.5), a_.at(left, True, -22.0, -10.0)]
        spike = [a_.at(left, False, 10.0, 2.0), a_.at(left, False, -1.0, -5.5), a_.at(left, False, -19.0, -8.5)]
        for path, r0 in ((horn, 5.2), (spike, 4.2)):
            shape = sd_horn(MX, MY, path, r0, 0.8)
            pd = -shape
            halo = np.maximum(halo, np.exp(-np.maximum(shape, 0) / 2.0) * (shape > 0))
            metal.lay(band(pd, 0, 1.0), OUTLINE, height=np.full_like(pd, 0.6))
            metal.lay(band(pd, 1.0, 1e9), FORGE_IRON, height=0.8 + 2.4 * smoothstep(1.0, r0, pd))
            reach = math.hypot(path[2][0] - path[0][0], path[2][1] - path[0][1])
            tip = np.clip((reach - np.hypot(MX - path[2][0], MY - path[2][1])) / reach, 0, 1)
            metal.glow("#FF7A2A", 1.0 * smoothstep(0.45, 0.92, tip) * band(pd, 1.0, 1e9))
            metal.glow("#FFD27A", 0.8 * smoothstep(0.75, 1.0, tip) * band(pd, 1.0, 1e9))
            # The heat lighting its edge.
            metal.glow("#E0501A", 0.4 * band(pd, 1.0, 2.2))
    metal.shade()
    m = metal.rgba
    halo = 0.75 * halo * smoothstep(0.3, 1.5, a_.edge)
    under = flat.copy()
    under[..., :3] = hex_rgb("#FF8A2A") * halo[..., None] + under[..., :3] * (1 - halo[..., None])
    under[..., 3] = halo + under[..., 3] * (1 - halo)
    cv.rgba = m + under * (1 - m[..., 3:])
    return cv.image(), a_.border, (0.5, 0.5)


# Crests: laid at native size on the middle of a card's top edge, the sprite's pivot on that edge. They keep 15 above it
# (the card above stands 28 off, the title 32) and within 18 below it, clear of the kind line (from 488 across, 8 down)
# and the name (from 36 down).

FIRE_CREST_W, FIRE_CREST_H, FIRE_CREST_UP = 500, 26, 16  # 16 above the card's top edge, 10 below it; x 198-698


def fire_crest():
    """The evolution's crest: a crown of fire burning along the middle of its top edge, tallest at the middle, embers
    above it and its heat on the rim below."""
    w, h, up = FIRE_CREST_W, FIRE_CREST_H, FIRE_CREST_UP
    cv = Canvas(w, h)
    X, Y = cv.X, cv.Y
    c = w / 2
    f = X / w
    ends = smoothstep(0.0, 0.17, np.minimum(f, 1 - f))
    bell = 0.32 + 0.68 * np.exp(-((f - 0.5) / 0.27) ** 2)
    # Tongues every 21 units or so, their spacing and heights wavering, standing on the rim 3 below the card's edge.
    wobble = periodic_noise(97, w, h, 16, 60)
    peaks = (0.5 + 0.5 * np.cos(2 * math.pi * X / 19.0 + 1.5 * wobble)) ** 3.2
    tall = 15.0 * bell * (0.3 + 0.7 * peaks) * (0.85 + 0.15 * periodic_noise(98, w, h, 9, 60)) * ends
    root = up + 3.0
    t = np.clip((root - Y) / np.maximum(tall, 0.5), 0, None)
    cover = np.clip((1.0 - t) / 0.2, 0, 1) * np.clip((root + 3.0 - Y) / 3.5, 0, 1) * smoothstep(0.6, 1.6, tall)
    # Licks torn off the tallest tongues, just above their tips.
    lick_x = (X % 38.0) / 38.0
    lick = np.clip(1 - np.hypot((lick_x - 0.5) * 38.0 / 2.2, (Y - (root - tall - 3.2)) / 1.6), 0, 1) * smoothstep(0.55, 0.9, peaks)
    cover = np.maximum(cover, 0.85 * lick * bell * ends)
    t = np.where(lick * bell * ends > cover * 0.99, 0.85, t)
    fire_a = cover * np.clip(1.1 - 0.45 * np.clip(t, 0, 1), 0, 1)
    fire_rgb = fire_colour(np.clip(t, 0, 1))
    # A dark red glow round the fire, so it parts from a dark floor and a dark card alike.
    halo = np.clip(skill.blur(cover, 2.2) * 0.75, 0, 1) * ends
    rgb = (fire_rgb * fire_a[..., None] + hex_rgb("#A8280C") * (halo * (1 - fire_a))[..., None]) / np.maximum(
        fire_a + halo * (1 - fire_a), 1e-4)[..., None]
    alpha = np.clip(fire_a + halo * (1 - fire_a), 0, 1)
    for x, y, r in ((c - 62, 2.6, 0.9), (c + 27, 1.6, 0.85), (c - 14, 1.2, 0.7), (c + 88, 4.6, 0.75),
                    (c - 112, 5.8, 0.65), (c + 132, 6.4, 0.65), (c + 58, 3.2, 0.6), (c - 88, 3.8, 0.6)):
        e = np.clip(1 - np.hypot(X - x, Y - y) / r, 0, 1) ** 0.7
        rgb = rgb * (1 - e[..., None]) + hex_rgb("#FFD27A") * e[..., None]
        alpha = e + alpha * (1 - e)
    alpha = alpha * smoothstep(0.3, 1.5, np.minimum(np.minimum(X, Y), np.minimum(w - X, h - Y)))
    wash(cv, rgb, alpha)
    return cv.flat().image(), (0, 0, 0, 0), (0.5, (h - up) / h)


GOLD_CREST_W, GOLD_CREST_H, GOLD_CREST_UP = 80, 30, 12  # 12 above the card's top edge, 18 below it


def gold_crest():
    """Gold's clasp on the middle of its top edge: a gilded lozenge round a cut sun-stone, scrolled wings along the edge
    ending in pearls."""
    w, h, up = GOLD_CREST_W, GOLD_CREST_H, GOLD_CREST_UP
    cv = Canvas(w, h)
    X, Y = cv.X, cv.Y
    c = w / 2
    cy = up + 3.0
    wings = np.full(X.shape, 1e9, np.float32)
    ends = []
    for side in (-1, 1):
        for k in range(34):
            t = k / 33.0
            px = c + side * (11 + 25 * t)
            py = up + 1.0 - 3.4 * math.sin(t * math.pi * 0.9)
            rr = 2.6 * (1 - t) + 0.7
            wings = np.minimum(wings, np.hypot(X - px, Y - py) - rr)
        ends.append((c + side * 36.5, up + 1.0 - 3.4 * math.sin(0.9 * math.pi)))
    cv.lay(inside(wings - 0.9) * (1 - inside(wings)), OUTLINE, height=np.zeros_like(X))
    cv.lay(inside(wings), GILT_BRIGHT, height=0.5 + 1.2 * np.clip(-wings, 0, 2.0))
    for ex, ey in ends:
        rivet(cv, ex, ey, 2.0, 1.0, mat=WHITE_GOLD)
    lozenge = (np.abs(X - c) / 15.0 + np.abs(Y - cy) / 13.0) * 13.0 - 13.0
    ld = -lozenge
    cv.lay(inside(lozenge - 1.1) * (1 - inside(lozenge)), OUTLINE, height=np.full_like(X, 0.8))
    cv.lay(band(ld, 0, 3.2), GILT_BRIGHT, height=1.0 + dome(ld, 0, 3.2, 1.4))
    cv.lay(band(ld, 3.2, 3.9), GROOVE, height=np.full_like(X, 0.8))
    cv.lay(band(ld, 3.9, 1e9), GILT, height=np.full_like(X, 1.2))
    faceted_stone(cv, c, cy, 7.2, "#FFD860", 1.6, heat=1.25, seed=21)
    cv.shade()
    glint = sparkle(cv, c - 2.5, cy - 3.0, 9.0, 0.7)
    cv.over((1.0, 1.0, 0.95), glint)
    return cv.image(), (0, 0, 0, 0), (0.5, (h - up) / h)


# ----------------------------------------------------------------------------------------------------------------------
# The name plate, the description's recess, the well, its lip and the press.

PLATE_W, PLATE_HT = 120, 56
PLATE_BORDER = (32, 8, 8, 0)  # left, bottom, right, top


def name_plate(look):
    w, h = PLATE_W, PLATE_HT
    cv = Canvas(w, h)
    X, Y = cv.X, cv.Y
    left, bottom, right, _ = PLATE_BORDER
    # Across the plate: 0 at its start, 1 at its end; the stretched middle carries the fades.
    u = np.clip((X - left) / (w - left - right), 0, 1)
    start = smoothstep(1.0, 14.0, X)
    rule_y = h - 4.2
    glow_colour = look.inlay_glow or "#FFC45A"
    glow = 0.3 * np.exp(-(u / 0.24) ** 2) * start * smoothstep(2.0, 30.0, Y) * (1 - smoothstep(rule_y - 1, rule_y + 1, Y))
    smoke = 0.3 * start * (1 - smoothstep(0.3, 1.0, u)) * smoothstep(4.0, 34.0, Y) * (1 - smoothstep(rule_y, rule_y + 2, Y))
    a = np.clip(glow + smoke * (1 - glow), 0, 1)
    rgb = (hex_rgb(glow_colour) * glow[..., None] + hex_rgb("#020306") * (smoke * (1 - glow))[..., None]) / \
        np.maximum(a, 1e-4)[..., None]
    wash(cv, rgb, a)
    cv.flat()
    base = cv.rgba.copy()
    # The rule along the foot, of the tier's metal, fading toward the end; a diamond of the stone at its start.
    metal = Canvas(w, h)
    MX, MY = metal.X, metal.Y
    fade = (1 - smoothstep(0.5, 1.0, u)) * smoothstep(13.0, 17.0, MX)
    rule = np.clip(1.25 - np.abs(MY - rule_y) / 1.0, 0, 1) * fade
    outline = np.clip(1.25 - np.abs(MY - rule_y) / 2.0, 0, 1) * fade
    metal.lay(outline, OUTLINE, height=np.zeros_like(MX))
    metal.lay(rule, look.rule, height=0.9 * np.clip(1.25 - np.abs(MY - rule_y) / 1.0, 0, 1))
    if look.inlay_glow:
        metal.glow(look.inlay_glow, look.inlay_heat * 0.8 * rule)
    diamond = (np.abs(MX - 9.0) + np.abs(MY - rule_y) * 1.15) - 5.6
    metal.lay(inside(diamond - 1.0) * (1 - inside(diamond)), OUTLINE, height=np.full_like(MX, 0.3))
    dd = np.clip(-diamond / 5.6, 0, 1)
    c = hex_rgb(look.stone)
    metal.lay(inside(diamond), Mat(c * 0.45, spec=1.0, gloss=40), height=0.3 + 2.6 * dd)
    metal.emit += c * (0.9 * np.clip(1 - np.hypot(MX - 9.3, MY - rule_y - 0.9) / 4.8, 0, 1) * inside(diamond))[..., None]
    metal.shade()
    m = metal.rgba
    cv.rgba = m + base * (1 - m[..., 3:])
    return cv.image(), PLATE_BORDER, (0.5, 0.5)


DESC_W, DESC_H = 96, 40
DESC_BORDER = (12, 12, 12, 12)


def description_area():
    """A faint recess the description lies in: darker, soft at its edges, its top in shadow, a hair of light along its
    foot; fading out toward its far end (the stretched middle carries the fade)."""
    w, h = DESC_W, DESC_H
    cv = Canvas(w, h)
    X, Y = cv.X, cv.Y
    u = np.clip((X - DESC_BORDER[0]) / (w - DESC_BORDER[0] - DESC_BORDER[2]), 0, 1)
    fade = 1 - smoothstep(0.45, 1.0, u)
    sd = sd_box(X, Y, 1.0, 1.0, w - 1.0, h - 1.0, 5.0)
    soft = np.clip(-sd / 5.0, 0, 1) ** 0.8
    a = 0.26 * soft + 0.16 * np.clip(-sd / 2.5, 0, 1) * np.exp(-np.maximum(Y - 1.0, 0) / 3.5)
    a = a * np.where(X < DESC_BORDER[0], 1.0, fade)
    wash(cv, hex_rgb("#020308"), a)
    cv.flat()
    foot = np.clip(1 - np.abs(Y - (h - 1.6)) / 0.7, 0, 1) * smoothstep(4.0, 12.0, X) * np.where(X < DESC_BORDER[0], 1.0, fade)
    cv.over((1.0, 0.92, 0.78), 0.09 * foot)
    return cv.image(), DESC_BORDER, (0.5, 0.5)


WELL_MARGIN = 10.0  # how far the well's rim reaches past the icon
WELL_S, WELL_B = 56, 24


def well(look):
    """The icon's socket: an outline, a lit rim of the treatment's metal, its inlay, a groove and a chamfer down to a navy
    floor exactly where the icon's edge is, the floor's top and left edges in the rim's shadow."""
    s, m = WELL_S, WELL_MARGIN
    cv = Canvas(s, s)
    X, Y = cv.X, cv.Y
    d = -sd_chamfer(X, Y, 0.25, 0.25, s - 0.25, s - 0.25, 5.0)
    cv.lay(band(d, 0.0, 1.2), OUTLINE, height=np.full_like(d, 1.0))
    cv.lay(band(d, 1.2, 2.4), look.edge, height=1.0 + dome(d, 1.2, 2.4, 0.6))
    cv.lay(band(d, 2.4, 5.2), look.well_metal, height=1.2 + dome(d, 2.4, 5.2, 1.3))
    cv.lay(band(d, 5.2, 5.8), GROOVE, height=np.full_like(d, 0.6))
    cv.lay(band(d, 5.8, 8.0), look.inlay, height=0.6 + dome(d, 5.8, 8.0, 0.9))
    cv.lay(band(d, 8.0, 8.6), GROOVE, height=np.full_like(d, 0.2))
    cv.lay(band(d, 8.6, m), IRON_DARK, height=ramp(d, 8.6, m, 0.3, -0.4))
    cv.lay(band(d, m, 1e9), Mat(NAVY), height=np.full_like(d, -0.4))
    if look.inlay_glow:
        cv.glow(look.inlay_glow, look.inlay_heat * 1.2 * band(d, 5.8, 8.0))
    cv.shade()
    # Unlit shading on the floor, so its middle stays the icons' navy: the rim's shadow from the top and left, deeper,
    # and a little from the bottom and right.
    floor = inside(-(sd_box(X, Y, m, m, s - m, s - m)))
    top_left = np.minimum(X - m, Y - m)
    bottom_right = np.minimum(s - m - X, s - m - Y)
    shade = np.maximum(0.62 * np.clip(1 - top_left / 7.0, 0, 1) ** 1.4, 0.3 * np.clip(1 - bottom_right / 4.0, 0, 1) ** 1.4)
    cv.over((0.0, 0.0, 0.0), shade * floor)
    return cv.image(), (WELL_B,) * 4, (0.5, 0.5)


LIP_S, LIP_B = 24, 8


def well_lip():
    """Laid over the icon's edge: a dark line at the edge and the rim's shadow falling a few units onto the icon from its
    top and left, a faint light on its bottom and right, so a square icon sits down in the well."""
    s = LIP_S
    cv = Canvas(s, s)
    X, Y = cv.X, cv.Y
    top_left = np.minimum(X, Y)
    bottom_right = np.minimum(s - X, s - Y)
    near = np.minimum(top_left, bottom_right)
    line = np.clip(1.4 - near, 0, 1) * 0.85
    shadow = 0.5 * np.clip(1 - top_left / 6.0, 0, 1) ** 1.5
    wash(cv, np.zeros(3, np.float32), np.maximum(line, shadow))
    cv.flat()
    light = 0.16 * np.clip(1 - (bottom_right - 1.3) / 1.6, 0, 1) * (bottom_right > 1.3) * smoothstep(3.0, 6.0, top_left)
    cv.over((1.0, 0.94, 0.85), light)
    return cv.image(), (LIP_B,) * 4, (0.5, 0.5)


def press():
    """The press, painted light: a wash over the rim, a light falling inward from it and a veil over the face. The
    button's colours tint it: clear at rest, the treatment's light under a finger, a shade while the card waits."""
    s, b = FRAME_S, FRAME_B
    cv = Canvas(s, s)
    d = -sd_chamfer(cv.X, cv.Y, 0.25, 0.25, s - 0.25, s - 0.25, 4.0)
    inward = np.exp(-np.maximum(d - RIM, 0) / 5.0) * (1 - smoothstep(b - 10.0, b - 1.0, d))
    a = np.where(d < RIM, 0.22, 0.1 + 0.9 * inward)
    wash(cv, np.ones(3, np.float32), a * np.clip(d * 2, 0, 1))
    return cv.flat().image(), (b, b, b, b), (0.5, 0.5)


# ----------------------------------------------------------------------------------------------------------------------
# Sigils: what an empty well holds.

SIGIL = 116


def stone_sigil(look, seed):
    s = SIGIL
    cv = Canvas(s, s)
    c = s / 2
    X, Y = cv.X, cv.Y
    shadow = sd_circle(X, Y, c + 2.5, c + 3.5, 41.0)
    cv.lay(0.5 * np.clip(1 - (shadow + 2) / 6, 0, 1), Mat("#000000"), height=np.zeros_like(shadow))
    # A thin bezel of the treatment's metal round the girdle.
    bezel = sd_circle(X, Y, c, c, 41.5)
    bd = -bezel
    cv.lay(band(bd, 0, 1.2), OUTLINE, height=np.full_like(bd, 0.4))
    cv.lay(band(bd, 1.2, 1e9), look.well_metal if look.name != "Green" else STEEL_EDGE,
           height=0.6 + dome(np.clip(bd, 0, 4.5), 1.2, 4.5, 1.2))
    faceted_stone(cv, c, c, 39.0, look.stone, 1.4, heat=0.75 if look.name != "Gold" else 0.9, seed=seed)
    # Four claws holding it, and the glint.
    metal = look.well_metal if look.name != "Green" else STEEL_EDGE
    for k in range(4):
        a = math.radians(45 + 90 * k)
        px, py = c + 37.5 * math.cos(a), c + 37.5 * math.sin(a)
        rivet(cv, px, py, 3.6, 3.2, mat=metal, rise=2.2)
    cv.shade()
    glint = sparkle(cv, c - 13.0, c - 15.0, 13.0, 0.9)
    cv.over((1.0, 1.0, 0.96), glint * 0.95)
    return cv.image(), (0, 0, 0, 0), (0.5, 0.5)


def molten_sigil():
    s = SIGIL
    cv = Canvas(s, s)
    c = s / 2
    X, Y = cv.X, cv.Y
    r = 39.0
    sd = sd_circle(X, Y, c, c, r)
    halo = np.clip(1 - np.maximum(sd, 0) / 15.0, 0, 1) ** 2 * (sd > 0)
    t = np.clip(-sd / r, 0, 1)
    cv.lay(inside(sd - 1.0) * (1 - inside(sd)), OUTLINE, height=np.zeros_like(sd))
    cv.lay(inside(sd), Mat("#2A0C06", spec=0.3, gloss=10), height=r * 0.5 * np.sqrt(t * (2 - t)))
    # Cracks: where smooth noise crosses zero, the crust parts on the fire under it.
    n1 = periodic_noise(81, s, s, 14, 14)
    n2 = periodic_noise(83, s, s, 7, 7)
    crack = np.clip(1 - np.abs(n1 + 0.35 * n2) / 0.075, 0, 1) ** 1.3
    rad = np.hypot(X - c, Y - c) / r
    heart = np.clip(1 - rad, 0, 1)
    within = inside(sd)
    cv.emit += hex_rgb("#E8501A") * ((0.95 * crack + 0.3 * heart) * within)[..., None]
    cv.emit += hex_rgb("#FFC45A") * ((0.85 * crack * smoothstep(0.15, 1.0, heart) + 0.4 * heart ** 2) * within)[..., None]
    cv.emit += hex_rgb("#FFF0C0") * ((0.55 * crack * smoothstep(0.5, 1.0, heart)) * within)[..., None]
    cv.shade()
    glow = 0.6 * halo
    cv.rgba[..., :3] = cv.rgba[..., :3] + hex_rgb("#E8501A") * glow[..., None] * (1 - cv.rgba[..., 3:])
    cv.rgba[..., 3] = cv.rgba[..., 3] + glow * (1 - cv.rgba[..., 3])
    return cv.image(), (0, 0, 0, 0), (0.5, 0.5)


def relief(cv, sd, metal, height=2.2, outline=1.2):
    """A raised shape: an outline, then metal rounded up from its edge."""
    d = -sd
    cv.lay(inside(sd - outline) * (1 - inside(sd)), OUTLINE, height=np.full_like(sd, 0.2))
    cv.lay(inside(sd), metal, height=0.2 + height * smoothstep(0.0, 3.2, d))


ANVIL_IRON = Mat("#585C6A", spec=0.45, gloss=16, speccol="#E0E8F8")


def anvil_sigil():
    """The forge: an anvil of lit iron filling the well, its striking face gilded, a bar glowing on it from the fire and
    the sparks of a blow above."""
    s = SIGIL
    cv = Canvas(s, s)
    X, Y = cv.X, cv.Y

    def body(x, y):
        face = sd_box(x, y, 27, 50, 105, 64, 1.8)
        horn = sd_horn(x, y, [(30, 56.5), (16, 58.0), (4, 51.5)], 6.6, 0.8)
        waist = sd_box(x, y, 48, 61, 86, 84, 2.0)
        waist = np.maximum(waist, -sd_circle(x, y, 40, 86, 11.0))
        waist = np.maximum(waist, -sd_circle(x, y, 94, 86, 11.0))
        foot = sd_chamfer(x, y, 31, 81, 103, 96, 3.4)
        return np.minimum(np.minimum(face, horn), np.minimum(waist, foot))

    shape = body(X, Y)
    shadow = body(X - 2.0, Y - 3.0)
    cv.lay(0.5 * np.clip(1 - (shadow + 1) / 4, 0, 1), Mat("#000000"), height=np.zeros_like(X))
    relief(cv, shape, ANVIL_IRON, height=3.0)
    cv.lay(inside(sd_box(X, Y, 28.5, 50.5, 103.5, 54.5, 1.0)) * inside(shape), GILT_BRIGHT, height=np.full_like(X, 3.4))
    # A bar from the fire lying on the face: white-hot along its middle, red at its ends, its glow on the iron.
    bar = sd_box(X, Y, 46, 40.5, 88, 50.5, 4.6)
    cv.lay(inside(bar - 0.9) * (1 - inside(bar)), OUTLINE, height=np.full_like(X, 3.4))
    cv.lay(inside(bar), Mat("#3A0C06", spec=0.2), height=3.6 + 1.4 * np.clip(-bar / 4.0, 0, 1))
    along = np.clip(1 - np.abs(X - 67) / 21.0, 0, 1)
    cv.glow("#E8501A", 0.95 * inside(bar))
    cv.glow("#FFC45A", 0.9 * inside(bar) * along ** 0.7)
    cv.glow("#FFF0C0", 0.6 * inside(bar) * along ** 2 * np.clip(-bar / 3.0, 0, 1))
    cv.glow("#C8401A", 0.4 * np.exp(-(np.maximum(bar, 0) / 4.0) ** 2) * (1 - inside(bar)) * inside(shape))
    cv.shade()
    # The sparks of a blow, flying up off the bar.
    a = np.maximum(sparkle(cv, 70, 27, 19.0, 1.2), 0.85 * sparkle_diag(cv, 70, 27, 9.0, 1.0))
    for ex, ey, r in ((52, 31, 1.5), (88, 30, 1.3), (80, 17, 1.0), (58, 18, 0.9), (95, 22, 0.8), (44, 23, 0.8)):
        a = np.maximum(a, np.clip(1 - np.hypot(X - ex, Y - ey) / r, 0, 1) ** 0.6)
    cv.over((1.0, 0.74, 0.32), np.clip(a, 0, 1))
    cv.over((1.0, 1.0, 0.92), np.clip(a * 1.6 - 0.6, 0, 1))
    return cv.image(), (0, 0, 0, 0), (0.5, 0.5)


def door_sigil():
    """Extract: an arched doorway of steel, cool light within it, and a gold coin on its threshold."""
    s = SIGIL
    cv = Canvas(s, s)
    X, Y = cv.X, cv.Y
    c = s / 2

    def arch(x0, y0, x1, y1, dx=0.0, dy=0.0):
        r = (x1 - x0) / 2
        cx = (x0 + x1) / 2
        rect = sd_box(X - dx, Y - dy, x0, y0 + r, x1, y1)
        top = sd_circle(X - dx, Y - dy, cx, y0 + r, r)
        return np.minimum(rect, np.where(Y - dy < y0 + r, top, 1e9))

    outer = arch(28, 10, 88, 98)
    opening = arch(39, 21, 77, 98)
    shadow = np.maximum(arch(28, 10, 88, 98, 2, 3), -arch(39, 21, 77, 98, 2, 3))
    cv.lay(0.5 * np.clip(1 - (shadow + 1) / 4, 0, 1), Mat("#000000"), height=np.zeros_like(X))
    frame = np.maximum(outer, -opening)
    coin = sd_circle(X, Y, c, 84, 11.0)
    coin_cover = inside(coin)
    # The light inside: brightest at the threshold, cool; masked where the coin stands.
    lit = inside(opening) * (1 - inside(frame))
    v = np.clip((Y - 21) / 77, 0, 1)
    light = np.stack([np.interp(v, [0, 0.6, 1], [0.16, 0.46, 0.80]), np.interp(v, [0, 0.6, 1], [0.30, 0.66, 0.92]),
                      np.interp(v, [0, 0.6, 1], [0.52, 0.90, 1.0])], -1).astype(np.float32)
    cv.lay(lit, Mat("#000000"), height=np.full_like(X, -0.5), albedo=np.zeros_like(light))
    cv.emit += light * (lit * (1 - coin_cover))[..., None]
    relief(cv, frame, SILVER, height=2.4)
    key = sd_chamfer(X, Y, 53, 7.5, 63, 20, 2.0)
    relief(cv, key, SILVER, height=3.0)
    step = sd_box(X, Y, 22, 98, 94, 104, 1.0)
    relief(cv, step, COOL_IRON, height=1.6)
    # The coin, in front of the light.
    shade_c = sd_circle(X - 1.5, Y - 2.0, c, 84, 11.0)
    cv.darken(0.5 * np.clip(1 - (shade_c + 0.5) / 2.5, 0, 1) * (1 - coin_cover))
    cd = -coin
    cv.lay(band(cd, 0, 1.2), OUTLINE, height=np.full_like(X, 1.0))
    cv.lay(band(cd, 1.2, 3.4), GILT_BRIGHT, height=1.2 + dome(cd, 1.2, 3.4, 1.2))
    cv.lay(band(cd, 3.4, 1e9), GILT, height=np.full_like(X, 1.6) + 0.6 * smoothstep(3.4, 9, cd))
    cv.shade()
    return cv.image(), (0, 0, 0, 0), (0.5, 0.5)


def chevron_sigil():
    """Climb: three chevrons rising, of hot iron, the highest hottest."""
    s = SIGIL
    cv = Canvas(s, s)
    X, Y = cv.X, cv.Y
    c = s / 2

    def chevron(y, half, thick, dx=0.0, dy=0.0):
        u = np.abs(X - dx - c)
        dist = (Y - dy - y) - u * 0.9
        return np.maximum(np.abs(dist - thick / 2) - thick / 2, u - half)

    for y, heat in ((78, 0.25), (56, 0.55), (34, 1.0)):
        ch = chevron(y, 40.0, 13.0)
        shadow = chevron(y, 40.0, 13.0, 2.0, 3.0)
        cv.lay(0.45 * np.clip(1 - (shadow + 1) / 4, 0, 1) * (1 - inside(ch)), Mat("#000000"), height=np.zeros_like(X))
        relief(cv, ch, Mat("#3A2420", spec=0.5, gloss=14, speccol="#FFD0A0"), height=2.4)
        edge = inside(ch) * np.exp(-np.maximum(-ch, 0) / 2.5)
        cv.glow("#FF6A22", heat * 0.7 * inside(ch))
        cv.glow("#FFC45A", heat * 0.55 * inside(ch) * (1 - edge))
    cv.shade()
    return cv.image(), (0, 0, 0, 0), (0.5, 0.5)


# ----------------------------------------------------------------------------------------------------------------------
# The panel: the surface's frame, the title's plate and crest, the backdrop.

SURFACE_S, SURFACE_B = 96, 40
SURFACE_FRAME = 18.0


def surface():
    """The choice panel's forged frame: the HUD panel's, heavier: an outline, a gilded edge, an iron rim, a groove, the
    gold inlay, a groove and a chamfer down to the navy face; riveted corner plates with an ember in each, keeping to
    27 of the corner (the last card stands 31 above the surface's foot and 48 in from its sides)."""
    s, b = SURFACE_S, SURFACE_B
    cv = Canvas(s, s)
    X, Y = cv.X, cv.Y
    d = -sd_chamfer(X, Y, 0.25, 0.25, s - 0.25, s - 0.25, 6.0)
    cv.lay(band(d, 0.0, 1.4), OUTLINE, height=np.full_like(d, 0.2))
    cv.lay(band(d, 1.4, 3.8), GOLD, height=0.2 + dome(d, 1.4, 3.8, 1.1))
    cv.lay(band(d, 3.8, 10.0), IRON, height=0.6 + dome(d, 3.8, 10.0, 2.6))
    cv.lay(band(d, 10.0, 10.8), GROOVE, height=np.full_like(d, 0.1))
    cv.lay(band(d, 10.8, 15.0), GOLD, height=dome(d, 10.8, 15.0, 1.7))
    cv.lay(band(d, 15.0, 16.0), GROOVE, height=np.zeros_like(d))
    cv.lay(band(d, 16.0, SURFACE_FRAME), IRON_DARK, height=ramp(d, 16.0, SURFACE_FRAME, 0.8, 0.0))
    cv.lay(band(d, SURFACE_FRAME, 1e9), Mat(NAVY), height=np.zeros_like(d))
    cv.darken(0.45 * (1 - smoothstep(SURFACE_FRAME, SURFACE_FRAME + 6.0, d)) * (d >= SURFACE_FRAME - 0.5))
    for cx, cy in ((14.0, 14.0), (s - 14.0, 14.0), (14.0, s - 14.0), (s - 14.0, s - 14.0)):
        h = 12.5
        plate = sd_chamfer(X, Y, cx - h, cy - h, cx + h, cy + h, 4.4)
        shadow = sd_chamfer(X, Y, cx - h + 0.6, cy - h + 1.4, cx + h + 1.6, cy + h + 2.4, 4.4)
        cv.darken(0.55 * np.clip(1 - (shadow + 0.5) / 2.6, 0, 1) * (1 - inside(plate)))
        pd = -plate
        cv.lay(band(pd, 0.0, 1.2), OUTLINE, height=np.full_like(pd, 1.0))
        cv.lay(band(pd, 1.2, 3.6), GOLD, height=1.4 + dome(pd, 1.2, 3.6, 1.1))
        cv.lay(band(pd, 3.6, 1e9), IRON, height=np.full_like(pd, 2.6) + 0.3 * smoothstep(3.6, 10, pd))
        for rx, ry in ((cx - 7.6, cy - 7.6), (cx + 7.6, cy - 7.6), (cx - 7.6, cy + 7.6), (cx + 7.6, cy + 7.6)):
            rivet(cv, rx, ry, 1.5, 2.8)
        ember(cv, cx, cy, 4.6, 2.8)
    return cv.shade().image(), (b, b, b, b), (0.5, 0.5)


TITLE_W, TITLE_H = 176, 92
TITLE_BORDER = (80, 14, 80, 14)
TITLE_RULE = 6.0  # the rules' height in from the plate's top and foot: the crest is centred on the lower one


def title_plate():
    """A smoked band behind the title between two gold rules, the rules and the smoke fading in from each end."""
    w, h = TITLE_W, TITLE_H
    cv = Canvas(w, h)
    X, Y = cv.X, cv.Y
    ends = smoothstep(2.0, 78.0, np.minimum(X, w - X))
    v = np.abs(Y - h / 2) / (h / 2 - 8)
    smoke = 0.6 * ends * (1 - smoothstep(0.7, 1.05, v))
    wash(cv, hex_rgb("#04060C"), smoke)
    cv.flat()
    base = cv.rgba.copy()
    metal = Canvas(w, h)
    MX, MY = metal.X, metal.Y
    fade = smoothstep(6.0, 70.0, np.minimum(MX, w - MX))
    for ry in (TITLE_RULE, h - TITLE_RULE):
        rule = np.clip(1 - np.abs(MY - ry) / 1.3, 0, 1) * fade
        outline = np.clip(1 - np.abs(MY - ry) / 2.3, 0, 1) * fade
        metal.lay(outline, OUTLINE, height=np.zeros_like(MX))
        metal.lay(rule, GOLD, height=1.0 * np.clip(1 - np.abs(MY - ry) / 1.3, 0, 1))
    metal.shade()
    m = metal.rgba
    cv.rgba = m + base * (1 - m[..., 3:])
    return cv.image(), TITLE_BORDER, (0.5, 0.5)


def title_crest():
    """On the title plate's lower rule, at its middle: an ember in a gold lozenge, short gold wings either side."""
    w, h = 96, 30
    cv = Canvas(w, h)
    X, Y = cv.X, cv.Y
    c = w / 2
    cy = h / 2
    wings = np.full(X.shape, 1e9, np.float32)
    for side in (-1, 1):
        for k in range(30):
            t = k / 29.0
            px = c + side * (12 + 30 * t)
            py = cy - 3.0 * math.sin(t * math.pi)
            rr = 2.0 * (1 - t) + 0.5
            wings = np.minimum(wings, np.hypot(X - px, Y - py) - rr)
    cv.lay(inside(wings - 0.8) * (1 - inside(wings)), OUTLINE, height=np.zeros_like(X))
    cv.lay(inside(wings), GOLD, height=0.4 + 1.0 * np.clip(-wings, 0, 1.5))
    lozenge = (np.abs(X - c) / 13.0 + np.abs(Y - cy) / 11.0) * 11.0 - 11.0
    ld = -lozenge
    cv.lay(inside(lozenge - 1.0) * (1 - inside(lozenge)), OUTLINE, height=np.full_like(X, 0.6))
    cv.lay(band(ld, 0, 2.6), GOLD, height=0.8 + dome(ld, 0, 2.6, 1.2))
    cv.lay(band(ld, 2.6, 3.2), GROOVE, height=np.full_like(X, 0.6))
    ember(cv, c, cy, 6.4, 0.9, heat=1.1)
    return cv.shade().image(), (0, 0, 0, 0), (0.5, 0.5)


BACKDROP_W, BACKDROP_H = 90, 196
EXTRUDE = 2  # texels of the backdrop's edges repeated round it on the sheet, so its stretched edges never fade


def backdrop():
    """The dim behind the panel: the old navy, deeper toward the screen's edges and corners, a faint ember warmth from
    below. Stretched over the whole screen: its edge texels are repeated past its rect on the sheet, so bilinear
    sampling at the screen's edges reads them, not the clear gutter."""
    w, h = BACKDROP_W, BACKDROP_H
    cv = Canvas(w, h)
    u = (cv.X / w - 0.5) * 2
    v = (cv.Y / h - 0.56) * 2
    r = np.hypot(u * 1.0, v * 0.82)
    a = 0.80 + 0.14 * smoothstep(0.35, 1.35, r)
    rgb = np.broadcast_to(hex_rgb("#070912"), cv.X.shape + (3,)).copy()
    warm = 0.5 * smoothstep(0.55, 1.0, cv.Y / h) * (1 - smoothstep(0.0, 1.0, np.abs(u)))
    rgb = rgb + (hex_rgb("#1E0C08") - rgb) * warm[..., None]
    wash(cv, rgb, a)
    image = np.pad(cv.flat().image(), ((EXTRUDE, EXTRUDE), (EXTRUDE, EXTRUDE), (0, 0)), mode="edge")
    return image, (0, 0, 0, 0), (0.5, 0.5)


# ----------------------------------------------------------------------------------------------------------------------
# The sheet and the set.

SIGILS = {"Green": lambda look: stone_sigil(look, 11), "Orange": lambda look: stone_sigil(look, 12),
          "Red": lambda look: stone_sigil(look, 13), "Gold": lambda look: stone_sigil(look, 14),
          "Evolution": lambda look: molten_sigil(), "Neutral": lambda look: anvil_sigil(),
          "Extract": lambda look: door_sigil(), "Climb": lambda look: chevron_sigil()}
AURAS = {"Gold": (gold_aura, GOLD_OUT), "Evolution": (evolution_aura, EVO_OUT)}
CRESTS = {"Gold": gold_crest, "Evolution": fire_crest}
TREATMENT_ORDER = ("Green", "Orange", "Red", "Gold", "Evolution", "Neutral", "Extract", "Climb")


def sprites():
    out = {
        "Backdrop": backdrop(),
        "Surface": surface(),
        "TitlePlate": title_plate(),
        "TitleCrest": title_crest(),
        "Description": description_area(),
        "WellLip": well_lip(),
        "Press": press(),
    }
    for key in TREATMENT_ORDER:
        look = LOOKS[key]
        out[key + "Frame"] = card_frame(look)
        if key in AURAS:
            out[key + "Aura"] = AURAS[key][0]()
        if key in CRESTS:
            out[key + "Crest"] = CRESTS[key]()
        out[key + "NamePlate"] = name_plate(look)
        out[key + "Well"] = well(look)
        out[key + "Sigil"] = SIGILS[key](look)
    return out


# The parts' places round the labels and the icon, written into the set: margins (left, bottom, right, top; positive
# outward) from the label's rect and the title plate's from the title's, the well's reach past the icon, the sigil's share
# of the icon's size, and the shade a waiting card wears. The name plate's rule lies along the name's foot, between the
# name and the description; the description's recess keeps inside the card's rim on the right.
NAME_PLATE_MARGIN = (8, 2, 0, 0)
DESCRIPTION_MARGIN = (8, -4, 8, -4)
TITLE_PLATE_MARGIN = (-20, -17, -20, -17)
SIGIL_SCALE = 0.84
DISABLED = (0.02, 0.03, 0.06, 0.8)
EXTRUDED = {"Backdrop": EXTRUDE}


def write_bytes(path, text):
    with open(path, "wb") as f:
        f.write(text.encode("utf-8"))


def build_sheet(painted, frames_dir):
    sprites_ = [hud.Packed(f"{SHEET}_{key}", *value) for key, value in painted.items()]
    sheet = skill.bleed(skill.pack(sprites_))
    for s in sprites_:
        e = EXTRUDED.get(s.name[len(SHEET) + 1:], 0)
        if e:
            x, y, w, h = s.cells[0]
            s.cells[0] = (x + e, y + e, w - 2 * e, h - 2 * e)
    png = os.path.join(OUT_DIR, SHEET + ".png")
    Image.fromarray(sheet, "RGBA").save(png, optimize=True)
    meta = png + ".meta"
    guid = skill.existing_guid(meta)
    ids = hud.existing_ids(meta)
    taken = {v[0] for v in ids.values()}
    for s in sprites_:
        if s.name not in ids:
            internal = hud.fresh_internal_id(taken)
            taken.add(internal)
            ids[s.name] = (internal, uuid.uuid4().hex)
    write_bytes(meta, hud.texture_meta(guid, TPU, sprites_, ids))
    if frames_dir:
        os.makedirs(frames_dir, exist_ok=True)
        for s in sprites_:
            Image.fromarray(s.images[0], "RGBA").save(os.path.join(frames_dir, s.name + ".png"))
    size = skill.astc_bytes(sheet.shape[1], sheet.shape[0])
    print(f"{SHEET}.png {sheet.shape[1]}x{sheet.shape[0]}  ASTC 4x4 {size / 1024:.1f} KiB")
    for s in sprites_:
        x, y, w, h = s.cells[0]
        print(f"  {s.name:34s} {w:4d}x{h:<4d} border {s.border}")
    return dict(guid=guid, ids=ids, sheet=sheet, sprites=sprites_, bytes=size)


def ref(built, key):
    return f"{{fileID: {built['ids'][f'{SHEET}_{key}'][0]}, guid: {built['guid']}, type: 3}}"


def colour(c):
    return "{r: %s, g: %s, b: %s, a: %s}" % tuple(skill.number(float(v)) for v in c)


def write_set(built):
    body = hud.asset_header(skill.existing_guid(SCRIPT_META), "ChoiceCardArtSet")
    for field, key in (("_backdrop", "Backdrop"), ("_surface", "Surface"), ("_titlePlate", "TitlePlate"),
                       ("_titleCrest", "TitleCrest"), ("_description", "Description"), ("_wellLip", "WellLip"),
                       ("_press", "Press")):
        body.append(f"  {field}: {ref(built, key)}")
    body.append(f"  _disabledColour: {colour(DISABLED)}")
    body.append(f"  _namePlateMargin: {hud.vec(NAME_PLATE_MARGIN)}")
    body.append(f"  _descriptionMargin: {hud.vec(DESCRIPTION_MARGIN)}")
    body.append(f"  _titlePlateMargin: {hud.vec(TITLE_PLATE_MARGIN)}")
    body.append(f"  _titleRuleHeight: {skill.number(TITLE_RULE)}")
    body.append(f"  _wellMargin: {skill.number(WELL_MARGIN)}")
    body.append(f"  _sigilScale: {skill.number(SIGIL_SCALE)}")

    def treatment(key, indent):
        pad = " " * indent
        aura = AURAS.get(key)
        lines = [f"_frame: {ref(built, key + 'Frame')}",
                 f"_aura: {ref(built, key + 'Aura') if aura else '{fileID: 0}'}",
                 f"_auraOutset: {hud.vec(aura[1] if aura else (0, 0, 0, 0))}",
                 f"_crest: {ref(built, key + 'Crest') if key in CRESTS else '{fileID: 0}'}",
                 f"_namePlate: {ref(built, key + 'NamePlate')}",
                 f"_well: {ref(built, key + 'Well')}",
                 f"_sigil: {ref(built, key + 'Sigil')}",
                 f"_pressColour: {colour(LOOKS[key].press)}"]
        return [pad + line for line in lines]

    body.append("  _tiers:")
    for key in TIERS:
        lines = treatment(key, 4)
        body.append("  - " + lines[0][4:])
        body.extend(lines[1:])
    for field, key in (("_evolution", "Evolution"), ("_neutral", "Neutral"), ("_extract", "Extract"), ("_climb", "Climb")):
        body.append(f"  {field}:")
        body.extend(treatment(key, 4))
    path = os.path.join(OUT_DIR, "ChoiceCardArtSet.asset")
    write_bytes(path, "\n".join(body) + "\n")
    write_bytes(path + ".meta", skill.ASSET_META.format(guid=skill.existing_guid(path + ".meta")))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--frames", help="also save every sprite as a PNG in this directory")
    args = parser.parse_args()
    if not os.path.exists(SCRIPT_META):
        raise SystemExit(f"{SCRIPT_META} is missing: the set needs its script's GUID.")
    os.makedirs(OUT_DIR, exist_ok=True)
    folder_meta = OUT_DIR + ".meta"
    write_bytes(folder_meta, skill.FOLDER_META.format(guid=skill.existing_guid(folder_meta)))
    built = build_sheet(sprites(), args.frames)
    write_set(built)
    print(f"total ASTC 4x4 {built['bytes'] / 1024:.1f} KiB ({built['bytes'] / 1048576:.3f} MiB)")


if __name__ == "__main__":
    main()
