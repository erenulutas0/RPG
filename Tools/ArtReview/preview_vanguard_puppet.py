#!/usr/bin/env python3
"""Animated previews of the Vanguard's cut-out puppet, for review without Unity (docs/45 section 4 B).

The motion is the game's own: PuppetPreviewDriver.cs is compiled with the pure puppet sources (PuppetPose, PuppetGait,
PuppetMotion) into a throwaway console app, which plays scripted scenes at 60 frames a second and prints every pose.
The parts are the ones Unity imports: VanguardPuppetArtSet.asset for the rig (parts, joints, bones, draw order, hands,
grips), each TGA with the pivot its .meta gives it, and the painted equipment. This script hangs the parts on their
joints as PuppetRig does, places the weapon on the grip as PuppetAnimator does, and composites the figure bilinearly at
the game's framing - 9 world units across a 1080-pixel phone, 120 pixels a unit - over a dark stone floor that scrolls
with the hero, with the contact shadow under it.

Usage:
  python Tools/ArtReview/preview_vanguard_puppet.py OUT_DIR [--scene NAME ...] [--zoom Z] [--frames]

Writes NAME.gif per scene at real time (50 frames a second, the fastest every GIF viewer plays, sampled from the 60 fps
motion), plus walk-compare.gif: the painted four-frame walk VanguardAnimator plays today beside the puppet's. --frames
also writes each scene's frames as a contact sheet PNG. --zoom scales the images (default 1, the phone's own pixels).
"""
from __future__ import annotations

import argparse
import json
import math
import re
import shutil
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / 'Assets/_Project/Art/Characters/Vanguard'
ASSET = ART / 'Puppet/VanguardPuppetArtSet.asset'
SOURCES = ['Assets/_Project/Scripts/Art/PuppetPose.cs', 'Assets/_Project/Scripts/Art/PuppetGait.cs',
           'Assets/_Project/Scripts/Art/PuppetMotion.cs', 'Tools/ArtReview/PuppetPreviewDriver.cs']
PAVING = ROOT / 'Assets/_Project/Art/Props/Foundry/FoundryPavingV8.png'

# The game's framing: ArenaCameraFollow shows 9 world units across the screen; a 1080-pixel phone draws 120 a unit.
PIXELS_PER_UNIT = 120.0
BONES = ['Fixed', 'Pelvis', 'Torso', 'Head', 'Cape', 'Skirt', 'ThighLeft', 'ShinLeft', 'ThighRight', 'ShinRight',
         'UpperArmLeft', 'ForearmLeft', 'HandLeft', 'UpperArmRight', 'ForearmRight', 'HandRight']
# CombatantView's hit feedback: a white silhouette for this long after a hit.
FLASH_SECONDS = 0.14
# The feet stand this far down the image, leaving room above the helmet for a raised sword.
FEET_ROW = 0.7
SCENES = ['idle', 'walk-rear', 'walk-front', 'walk-rear-left', 'walk-front-left', 'strike-walk', 'strike-walk-front',
          'strike-stand', 'cast', 'hit', 'death', 'staff-strike-walk', 'daggers-strike-walk']


# --------------------------------------------------------------------------------------------------- the driver

def build_driver(work):
    project = work / 'PuppetPreviewDriver.csproj'
    items = '\n'.join(f'    <Compile Include="{(ROOT / s).as_posix()}" />' for s in SOURCES)
    project.write_text(f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>9.0</LangVersion>
    <Nullable>disable</Nullable>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <NuGetAudit>false</NuGetAudit>
    <AssemblyName>PuppetPreviewDriver</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
{items}
  </ItemGroup>
</Project>
""", encoding='utf-8')
    out = work / 'bin'
    result = subprocess.run(['dotnet', 'build', str(project), '-c', 'Release', '-o', str(out), '-nologo', '-v', 'q'],
                            capture_output=True, text=True)
    if result.returncode != 0:
        raise SystemExit('driver build failed:\n' + result.stdout + result.stderr)
    return out / 'PuppetPreviewDriver.dll'


def run_driver(dll, scene, weapon, art):
    legs = [f'{v:.6f}' for name in ('Rear', 'Front') for v in art[name]['legs']]
    result = subprocess.run(['dotnet', str(dll), scene, weapon] + legs, capture_output=True, text=True)
    if result.returncode != 0:
        raise SystemExit(f'driver {scene} failed:\n' + result.stderr)
    return [json.loads(line) for line in result.stdout.splitlines() if line.strip()]


# ----------------------------------------------------------------------------------------------------- the art

def guid_files(folder):
    found = {}
    for meta in folder.rglob('*.meta'):
        match = re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(encoding='utf-8'), re.M)
        if match:
            found[match.group(1)] = meta.with_suffix('')
    return found


@dataclass
class Sprite:
    rgba: np.ndarray    # premultiplied, top row first
    pivot: tuple        # Unity pivot, 0..1 from the bottom-left
    ppu: float


def load_sprite(path, white=False):
    with Image.open(path) as image:
        rgba = np.asarray(image.convert('RGBA'), np.float32) / 255
    meta = path.with_suffix(path.suffix + '.meta').read_text(encoding='utf-8')
    px, py = re.search(r'spritePivot: \{x: ([^,]+), y: ([^}]+)\}', meta).groups()
    ppu = float(re.search(r'spritePixelsToUnits: ([0-9.]+)', meta).group(1))
    if white:
        rgba[..., :3] = 1.0
    premult = rgba.copy()
    premult[..., :3] *= rgba[..., 3:4]
    return Sprite(premult, (float(px), float(py)), ppu)


def parse_facing(block, files):
    parts = []
    for m in re.finditer(r'    - Name: (.*)\n      Sprite: \{fileID: 21300000, guid: (\w+), type: 3\}\n'
                         r'      Flash: \{fileID: 21300000, guid: (\w+), type: 3\}\n      Parent: (-?\d+)\n'
                         r'      Joint: \{x: ([^,]+), y: ([^}]+)\}\n      Bone: (\d+)\n      Sorting: (-?\d+)', block):
        name, sprite, flash, parent, jx, jy, bone, sorting = m.groups()
        parts.append(dict(name=name, sprite=load_sprite(files[sprite]), flash=load_sprite(files[flash], white=True),
                          parent=int(parent), joint=(float(jx), float(jy)), bone=BONES[int(bone)],
                          sorting=int(sorting)))

    def value(key):
        return re.search(rf'    {key}: (.*)', block).group(1)

    def vec(key):
        x, y = re.search(r'\{x: ([^,]+), y: ([^}]+)\}', value(key)).groups()
        return float(x), float(y)

    def vec4(key):
        return tuple(float(v) for v in re.search(r'\{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}',
                                                   value(key)).groups())

    facing = dict(parts=parts, right_hand=int(value('RightHand')), right_grip=vec('RightGrip'),
                  right_item=int(value('RightItemSorting')), left_hand=int(value('LeftHand')),
                  left_grip=vec('LeftGrip'), left_item=int(value('LeftItemSorting')),
                  faces_left=value('FacesLeft').strip() == '1')
    turn = -1.0 if facing['faces_left'] else 1.0
    facing['legs'] = (leg(parts, 'ThighRight', 'ShinRight', vec4('RightSole'), turn)
                      + leg(parts, 'ThighLeft', 'ShinLeft', vec4('LeftSole'), turn))
    return facing


def leg(parts, thigh_bone, shin_bone, sole, turn):
    """PuppetRig.Leg: the knee from the hip and the sole's heel and toe from the knee, for a figure facing right."""
    thigh = next((i for i, p in enumerate(parts) if p['bone'] == thigh_bone), -1)
    shin = next((i for i, p in enumerate(parts) if p['bone'] == shin_bone), -1)
    if thigh < 0 or shin < 0 or parts[shin]['parent'] != thigh:
        return (0.0,) * 6
    if all(v == 0 for v in sole):
        sprite = parts[shin]['sprite']
        h, w = sprite.rgba.shape[:2]
        px, py = sprite.pivot
        texel = 1 / sprite.ppu
        lo_x, hi_x, lo_y = -px * w * texel, (1 - px) * w * texel, -py * h * texel
        sole = (lo_x + texel, lo_y + texel, hi_x - texel, lo_y + texel)
    kx, ky = parts[shin]['joint']
    return (turn * kx, ky, turn * sole[0], sole[1], turn * sole[2], sole[3])


def load_art():
    files = guid_files(ART)
    text = ASSET.read_text(encoding='utf-8')
    rear = text[text.index('  _rear:'):text.index('  _front:')]
    front = text[text.index('  _front:'):text.index('  _sword:')]
    equipment = {}
    for key in ('sword', 'shield', 'staff', 'dagger'):
        guid = re.search(rf'  _{key}: \{{fileID: 21300000, guid: (\w+)', text).group(1)
        equipment[key] = load_sprite(files[guid])
    return dict(Rear=parse_facing(rear, files), Front=parse_facing(front, files), equipment=equipment)


# ------------------------------------------------------------------------------------------------------ the rig

def rotation(pose, bone):
    """PuppetPose.Rotation."""
    table = {'Torso': -pose['Lean'], 'Head': -pose['Head'], 'Cape': -pose['Cape'], 'Skirt': -pose['Skirt'],
             'ThighLeft': pose['HipLeft'], 'ShinLeft': -pose['KneeLeft'], 'ThighRight': pose['HipRight'],
             'ShinRight': -pose['KneeRight'], 'UpperArmLeft': pose['ShoulderLeft'], 'ForearmLeft': pose['ElbowLeft'],
             'HandLeft': pose['WristLeft'], 'UpperArmRight': pose['ShoulderRight'],
             'ForearmRight': pose['ElbowRight'], 'HandRight': pose['WristRight']}
    return table.get(bone, 0.0)


def rise(pose, bone):
    return {'Pelvis': pose['Lift'], 'Torso': pose['Breath']}.get(bone, 0.0)


def forward(pose, bone):
    return {'Pelvis': pose['Shift'], 'ThighRight': pose['HipTwist'], 'ThighLeft': -pose['HipTwist'],
            'UpperArmRight': pose['Twist'], 'UpperArmLeft': -pose['Twist']}.get(bone, 0.0)


def trs(tx, ty, degrees, sx=1.0, sy=1.0):
    r = math.radians(degrees)
    c, s = math.cos(r), math.sin(r)
    return np.array([[c * sx, -s * sy, tx], [s * sx, c * sy, ty], [0, 0, 1]], np.float64)


def pose_rig(facing, pose):
    """World matrices (figure space: feet at the origin, unmirrored, y up) of every part and both grips."""
    turn = -1.0 if facing['faces_left'] else 1.0
    root = trs(0, 0, pose['Fall'], -1.0 if facing['faces_left'] else 1.0, 1.0)
    world = []
    for part in facing['parts']:
        jx, jy = part['joint']
        local = trs(jx + turn * forward(pose, part['bone']), jy + rise(pose, part['bone']),
                    turn * rotation(pose, part['bone']))
        parent = world[part['parent']] if part['parent'] >= 0 else root
        world.append(parent @ local)
    grips = {}
    for side in ('right', 'left'):
        hand = facing[f'{side}_hand']
        bone = facing['parts'][hand]['bone']
        wrist = pose['WristRight'] if side == 'right' else pose['WristLeft']
        turns = bone not in ('HandRight', 'HandLeft')
        gx, gy = facing[f'{side}_grip']
        grips[side] = world[hand] @ trs(gx, gy, turn * wrist if turns else 0.0)
    return world, grips


def grip_pose(matrix):
    """PuppetAnimator.GripPose: the position and the turn read from the grip's up axis."""
    return matrix[0, 2], matrix[1, 2], math.degrees(math.atan2(-matrix[0, 1], matrix[1, 1]))


# ------------------------------------------------------------------------------------------------- compositing

class Canvas:
    def __init__(self, width, height, zoom):
        self.width, self.height, self.zoom = width, height, zoom
        self.rgba = np.zeros((height, width, 4), np.float32)

    def draw(self, sprite, matrix, origin, tint=None):
        """Draws a sprite through a figure-space matrix; origin is the figure's feet in canvas pixels."""
        h, w = sprite.rgba.shape[:2]
        px, py = sprite.pivot
        scale = PIXELS_PER_UNIT * self.zoom
        # Texel centre (c, r) -> sprite local world units -> figure space -> canvas pixels.
        to_local = np.array([[1 / sprite.ppu, 0, (0.5 - px * w) / sprite.ppu],
                             [0, -1 / sprite.ppu, (h - 0.5 - py * h) / sprite.ppu], [0, 0, 1]])
        to_canvas = np.array([[scale, 0, origin[0]], [0, -scale, origin[1]], [0, 0, 1]])
        m = (to_canvas @ matrix @ to_local)[:2]
        layer = cv2.warpAffine(sprite.rgba, m, (self.width, self.height), flags=cv2.INTER_LINEAR,
                               borderMode=cv2.BORDER_CONSTANT, borderValue=(0, 0, 0, 0))
        if tint is not None:
            layer[..., :3] = layer[..., 3:4] * np.array(tint, np.float32)
        a = layer[..., 3:4]
        self.rgba = layer + self.rgba * (1 - a)

    def over(self, other):
        a = other[..., 3:4]
        self.rgba = other + self.rgba * (1 - a)


def floor_image(width, height, camera_x, camera_y, zoom, paving):
    """A dark stone floor in world space: the foundry's paving, darkened, laid in flagstones a unit wide and half a
    unit deep on screen (the floor's depth scale), so the eye can read the hero's travel against it."""
    scale = PIXELS_PER_UNIT * zoom
    tile = paving
    th, tw = tile.shape[:2]
    # The paving texture spans two world units.
    span = 2.0 * scale
    ys, xs = np.mgrid[0:height, 0:width].astype(np.float32)
    wx = (xs - width / 2) / scale + camera_x
    wy = (height * FEET_ROW - ys) / scale + camera_y
    u = ((wx / 2.0) % 1.0) * (tw - 1)
    v = ((wy / 1.0) % 1.0) * (th - 1)
    stone = cv2.remap(tile, u.astype(np.float32), v.astype(np.float32), cv2.INTER_LINEAR)
    stone = stone * 0.32
    # Flagstones: one unit across, half a unit tall on screen, every other row offset by half.
    row = np.floor(wy / 0.5)
    fx = (wx + 0.5 * (row % 2)) % 1.0
    fy = (wy / 0.5) % 1.0
    joint = np.minimum(np.minimum(fx, 1 - fx) * scale, np.minimum(fy, 1 - fy) * scale * 0.5)
    stone = stone * (0.55 + 0.45 * np.clip(joint / 2.0, 0, 1))[..., None]
    out = np.concatenate([stone, np.ones((height, width, 1), np.float32)], axis=2)
    return out


def shadow_image(width, height, origin, zoom):
    """ContactShadowArt: a soft ellipse 0.73 units across under the feet."""
    scale = PIXELS_PER_UNIT * zoom
    ys, xs = np.mgrid[0:height, 0:width].astype(np.float32)
    nx = (xs - origin[0]) / (0.366 * scale)
    ny = (ys - origin[1]) / (0.171 * scale)
    r = nx * nx + ny * ny
    a = np.where(r < .18, 132, np.where(r < .42, 96, np.where(r < .72, 58, np.where(r < 1, 24, 0)))) / 255.0
    a = cv2.GaussianBlur(a.astype(np.float32), (0, 0), 0.8 * zoom)
    out = np.zeros((height, width, 4), np.float32)
    out[..., 0] = 9 / 255 * a
    out[..., 1] = 10 / 255 * a
    out[..., 2] = 20 / 255 * a
    out[..., 3] = a
    return out


# PuppetAnimator's carry for the front facing: the sword hangs point-down and back, the daggers tilt out from the
# body, so neither covers the chest; turned back toward their drawn angle as they are used (PuppetPose.Wield).
FRONT_SWORD_CARRY = -180.0
FRONT_DAGGER_CARRY = 35.0


def weapon_items(art, weapon, front):
    eq = art['equipment']
    if weapon == 'staff':
        return [('right', eq['staff'], 0.0)]
    if weapon == 'daggers':
        return [('right', eq['dagger'], FRONT_DAGGER_CARRY if front else 0.0),
                ('left', eq['dagger'], -FRONT_DAGGER_CARRY if front else 0.0)]
    return [('right', eq['sword'], FRONT_SWORD_CARRY if front else 0.0), ('left', eq['shield'], 0.0)]


def render_figure(art, frame, width, height, origin, zoom, flash=False):
    """The puppet alone (premultiplied RGBA), mirrored for leftward travel as the hero's presentation is."""
    facing = art['Front' if frame['front'] else 'Rear']
    world, grips = pose_rig(facing, frame)
    canvas = Canvas(width, height, zoom)
    draws = []
    for i, part in enumerate(facing['parts']):
        sprite = part['flash'] if flash else part['sprite']
        draws.append((part['sorting'], i, sprite, world[i], (1, 1, 1) if flash else None))
    # Weapons stay visible until the body, and the weapons under it, is hidden at death.
    if not frame['dying']:
        for k, (side, sprite, carry) in enumerate(weapon_items(art, frame['weapon'], frame['front'])):
            x, y, angle = grip_pose(grips[side])
            sorting = facing['right_item'] if side == 'right' else facing['left_item']
            draws.append((sorting, 100 + k, sprite, trs(x, y, angle + (1 - frame['Wield']) * carry), None))
    for _, _, sprite, matrix, tint in sorted(draws, key=lambda d: (d[0], d[1])):
        canvas.draw(sprite, matrix, origin, tint)
    figure = canvas.rgba
    if frame['mirrored']:
        # Mirror about the feet.
        m = np.float32([[-1, 0, 2 * origin[0]], [0, 1, 0]])
        figure = cv2.warpAffine(figure, m, (width, height), flags=cv2.INTER_NEAREST)
    return figure


def compose(art, frame, width, height, zoom, paving, flash):
    origin = (width / 2, height * FEET_ROW)
    image = floor_image(width, height, frame['x'], frame['y'], zoom, paving)
    shadow = shadow_image(width, height, origin, zoom)
    a = shadow[..., 3:4]
    image = shadow + image * (1 - a)
    figure = render_figure(art, frame, width, height, origin, zoom, flash)
    a = figure[..., 3:4]
    image = figure + image * (1 - a)
    return (np.clip(image[..., :3], 0, 1) * 255 + 0.5).astype(np.uint8)


# ----------------------------------------------------------------------------------------- the painted walk today

OLD_FRAMES = ['Idle', 'ContactA', 'PassingA', 'ContactB', 'PassingB']
# VanguardArtSet.asset's hand anchors for frames 0-4 (world units from the feet), rear and front.
OLD_RIGHT = {False: [(0.3112745, 0.51256675), (0.32352942, 0.51746), (0.32843137, 0.51746), (0.3259804, 0.51746),
                     (0.32352942, 0.51746)],
             True: [(-0.28063726, 0.53100586)] * 5}
OLD_LEFT = {False: [(-0.30514705, 0.754782), (-0.3112745, 0.7511121), (-0.3112745, 0.7511121),
                    (-0.31495097, 0.7511121), (-0.3112745, 0.7511121)],
            True: [(0.38112745, 0.657959)] * 5}


class OldWalk:
    """VanguardAnimator as it plays today: four painted poses a 0.8-unit stride and its bob, squash and lean."""

    def __init__(self, art):
        self.frames = {}
        for front in (False, True):
            prefix = 'CHR_Vanguard_Front_' if front else 'CHR_Vanguard_'
            self.frames[front] = [load_sprite(ART / f'{prefix}{n}.tga') for n in OLD_FRAMES]
        self.sword = art['equipment']['sword']
        self.shield = art['equipment']['shield']
        self.distance = self.gait = self.phase = self.lean = self.settle = self.settle_weight = self.breath = 0.0
        self.striding = False

    def step(self, frame, dt=1 / 60):
        dx, dy = frame['dx'], frame['dy'] / 0.5
        travel = math.hypot(dx, dy)
        moving = travel >= 1e-4
        if travel > 0:
            self.distance = (self.distance + travel) % 0.8
        index = 1 + min(3, int(self.distance / 0.8 * 4)) if moving else 0
        if not moving:
            self.distance = 0.0
        lean = 0.0
        if moving:
            self.settle = 0
            self.gait = min(1, self.gait + dt / 0.1)
            self.phase = self.distance / 0.8
            lean = (3.0 if frame['mirrored'] else -3.0) * abs(dx) / travel
        else:
            if self.striding:
                self.settle, self.settle_weight = 0.18, self.gait
            self.settle = max(0.0, self.settle - dt)
            self.gait = self.settle_weight * self.settle / 0.18
        self.striding = moving
        self.lean += max(-25 * dt, min(25 * dt, lean - self.lean))
        self.breath = 0.0 if self.gait >= 1 else (self.breath + dt) % 2.6
        step = self.gait * math.cos(4 * math.pi * (self.phase - 0.125)) if self.gait > 0 else 0.0
        land = 0.025 * self.settle_weight * math.sin(math.pi * (1 - self.settle / 0.18)) if self.settle > 0 else 0.0
        breath = 0.012 * (1 - self.gait) * 0.5 * (1 - math.cos(2 * math.pi * self.breath / 2.6))
        squash = 0.015 * step + land
        return index, trs(0, 0.03 * 0.5 * (self.gait - step), self.lean * (1 + 0.2 * step), 1 + squash * 0.5,
                          1 - squash + breath)

    def render(self, frame, width, height, zoom):
        index, weight = self.step(frame)
        front = frame['front']
        origin = (width / 2, height * FEET_ROW)
        canvas = Canvas(width, height, zoom)
        mirror = np.diag([-1.0 if frame['mirrored'] else 1.0, 1.0, 1.0])
        body = weight @ mirror
        rh = OLD_RIGHT[front][index]
        lh = OLD_LEFT[front][index]
        sword = body @ trs(rh[0], rh[1], -180 if front else 0)
        shield = body @ trs(lh[0], lh[1], 0)
        draws = [(0, self.frames[front][index], body), (2 if front else -1, self.sword, sword),
                 (3 if front else -1, self.shield, shield)]
        for _, sprite, matrix in sorted(draws, key=lambda d: d[0]):
            canvas.draw(sprite, matrix, origin)
        return canvas.rgba


# ----------------------------------------------------------------------------------------------------- output

def save_gif(path, images, fps=50):
    frames = [Image.fromarray(i) for i in images]
    palette = Image.fromarray(np.concatenate([np.asarray(f) for f in frames[::max(1, len(frames) // 12)]], axis=0))
    palette = palette.quantize(colors=255, method=Image.Quantize.MEDIANCUT)
    quantized = [f.quantize(palette=palette, dither=Image.Dither.NONE) for f in frames]
    quantized[0].save(path, save_all=True, append_images=quantized[1:], duration=int(round(1000 / fps)), loop=0,
                      optimize=False, disposal=1)


def real_time(frames, fps=50):
    """The 60 fps frames nearest each 50 fps instant, so the GIF plays at the game's speed."""
    count = int(len(frames) * fps / 60)
    return [frames[min(len(frames) - 1, int(round(k * 60 / fps)))] for k in range(count)]


def contact_sheet(path, images, every, columns=12, label=None):
    picked = images[::every]
    h, w = picked[0].shape[:2]
    rows = (len(picked) + columns - 1) // columns
    sheet = Image.new('RGB', (columns * w, rows * h), (20, 20, 24))
    draw = ImageDraw.Draw(sheet)
    for k, image in enumerate(picked):
        sheet.paste(Image.fromarray(image), ((k % columns) * w, (k // columns) * h))
        draw.text(((k % columns) * w + 3, (k // columns) * h + 2), str(k * every), fill=(255, 255, 0))
    sheet.save(path)


def scene_weapon(name):
    if name.startswith('staff-'):
        return name[len('staff-'):], 'staff'
    if name.startswith('daggers-'):
        return name[len('daggers-'):], 'daggers'
    return name, 'sword'


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split('\n\n')[0])
    parser.add_argument('out', type=Path)
    parser.add_argument('--scene', action='append', help='scene names (default: all)')
    parser.add_argument('--zoom', type=float, default=1.0)
    parser.add_argument('--frames', action='store_true', help='also write contact sheets')
    parser.add_argument('--every', type=int, default=3, help='contact sheet: every Nth 60 fps frame (default 3)')
    parser.add_argument('--no-compare', action='store_true')
    args = parser.parse_args(argv)
    args.out.mkdir(parents=True, exist_ok=True)
    art = load_art()
    with Image.open(PAVING) as image:
        paving = np.asarray(image.convert('RGB'), np.float32) / 255
    work = Path(tempfile.mkdtemp(prefix='puppet-preview-'))
    try:
        dll = build_driver(work)
        width, height = int(320 * args.zoom), int(330 * args.zoom)
        for name in args.scene or SCENES:
            scene, weapon = scene_weapon(name)
            frames = run_driver(dll, scene, weapon, art)
            images = []
            flash_left = 0.0
            for frame in frames:
                if frame['hit']:
                    flash_left = FLASH_SECONDS
                images.append(compose(art, frame, width, height, args.zoom, paving, flash_left > 0))
                flash_left -= 1 / 60
            save_gif(args.out / f'{name}.gif', real_time(images))
            if args.frames:
                contact_sheet(args.out / f'{name}-frames.png', images, args.every)
            print(f'{name}: {len(frames)} frames')
        if not args.no_compare:
            # Today's painted walk (left) and the puppet's (right) over the same travel, from behind then the front.
            frames = run_driver(dll, 'walk-rear', 'sword', art) + run_driver(dll, 'walk-front', 'sword', art)
            old = OldWalk(art)
            images = []
            origin = (width / 2, height * FEET_ROW)
            shadow = shadow_image(width, height, origin, args.zoom)
            for frame in frames:
                floor = floor_image(width, height, frame['x'], frame['y'], args.zoom, paving)
                ground = shadow + floor * (1 - shadow[..., 3:4])
                painted = old.render(frame, width, height, args.zoom)
                left = painted + ground * (1 - painted[..., 3:4])
                right = compose(art, frame, width, height, args.zoom, paving, False)
                pair = np.concatenate([(np.clip(left[..., :3], 0, 1) * 255 + 0.5).astype(np.uint8),
                                       np.full((height, 4, 3), 10, np.uint8), right], axis=1)
                images.append(pair)
            save_gif(args.out / 'walk-compare.gif', real_time(images))
            if args.frames:
                contact_sheet(args.out / 'walk-compare-frames.png', images, 3, columns=6)
            print(f'walk-compare: {len(frames)} frames')
    finally:
        shutil.rmtree(work, ignore_errors=True)
    return 0


if __name__ == '__main__':
    sys.exit(main())
