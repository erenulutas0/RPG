"""The Runner, the Tank and the Grunt Captain as the game's sprites (docs/44, The Foundry roster).

    python Tools/ArtReview/build_foundry_enemies.py [--render DIR] [--only runner,tank,captain] [--preview DIR]

The Runner and the Tank come from their Blender renders (BuildBlenderFoundryEnemies.py): each 4x frame is box-filtered
down in linear light with premultiplied alpha (the floor pivot stays on a texel corner), its lava mask (the render's
'lava' AOV) blooms a soft ember light over the stone round every seam, as the painted Grunt's seams glow, and a
one-texel dark outline closes the silhouette.

The Grunt Captain is a kitbash of the painted Grunt's own masters (ArtDirection/2026-09-17, registered exactly as the
Grunt's sprites were): every frame scaled up, the basalt pushed toward a red heat with hotter, wider seams, and
painted brass added per frame - a crested captain's helm on the head and the warning bell on a chain across the chest
(docs/44: the Captain's Post, "Hold the gate until the bell").

Writes, all regenerated on every run (existing GUIDs are kept, new files get fresh uuid4 GUIDs):

    Assets/_Project/Art/Characters/<Folder>/CHR_<Name>_<frame>.tga and _Flash.tga (+ .meta, modelled on the Grunt's:
        a single sprite, 128 pixels a unit, bilinear, no mipmaps, ASTC 4x4 on Android, the pivot at the feet)
    Assets/_Project/Art/Characters/<Folder>/<Name>ArtSet.asset (+ .meta): the EnemyArtSet
    ArtDirection/2026-10-27/foundry-enemies-01/sprites.json: sizes, pivots, the set values and the texture memory
"""
import json
import math
import re
import sys
import uuid
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ARGS = sys.argv[1:]


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


RENDER = Path(arg('--render', str(ROOT / 'TestResults/foundry-enemies-render')))
ONLY = arg('--only', 'runner,tank,captain').split(',')
PREVIEW = arg('--preview')
DOCS = ROOT / 'ArtDirection/2026-10-27/foundry-enemies-01'
PPU = 128                       # texels a world unit, as the painted Grunt
OUTLINE = (0x16, 0x0F, 0x17)
# The seams' light over the stone: ember orange, about two texels wide, as on the painted Grunt.
BLOOM = (0xFF, 0x86, 0x2E)
BLOOM_SIGMA = 2.0
# The mask is 0.5 for a seam and 1 for the Runner's heart (BuildBlenderFoundryEnemies.lava_material).
BLOOM_GAIN = 1.5
BLOOM_CAP = 0.8
HEART = (0xFF, 0xB0, 0x48)
HEART_SIGMA = 4.5
HEART_GAIN = 2.2


def to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def blur(field, sigma):
    """A separable Gaussian blur (edges clamp), enough for a bloom without SciPy."""
    radius = max(1, int(3 * sigma))
    x = np.arange(-radius, radius + 1)
    k = np.exp(-x * x / (2 * sigma * sigma))
    k /= k.sum()
    pad = np.pad(field, ((radius, radius), (0, 0)), mode='edge')
    field = sum(k[i] * pad[i:i + field.shape[0]] for i in range(len(k)))
    pad = np.pad(field, ((0, 0), (radius, radius)), mode='edge')
    return sum(k[i] * pad[:, i:i + field.shape[1]] for i in range(len(k)))


def outline(lin, alpha, ink=OUTLINE):
    """One texel of dark outline round the silhouette, soft at the corners, under the figure (premultiplied)."""
    pad = np.pad(alpha[..., 0], 1)
    grown = np.maximum.reduce([pad[1:-1, 1:-1], pad[:-2, 1:-1], pad[2:, 1:-1], pad[1:-1, :-2], pad[1:-1, 2:],
                               0.7 * pad[:-2, :-2], 0.7 * pad[2:, 2:], 0.7 * pad[:-2, 2:], 0.7 * pad[2:, :-2]])[..., None]
    ink = to_linear(np.array(ink) / 255.0)
    return lin + ink * grown * (1 - alpha), alpha + grown * (1 - alpha)


def finish(lin, alpha):
    """Premultiplied linear to straight-alpha sRGB uint8; fully transparent texels carry black."""
    straight = np.where(alpha > 1e-6, lin / np.maximum(alpha, 1e-6), 0)
    out = np.concatenate([to_srgb(straight), alpha], axis=-1)
    out = (out * 255 + 0.5).astype(np.uint8)
    out[out[..., 3] < 2] = 0
    return out


def process_render(beauty_path, mask_path, ss):
    """One Blender frame at the game's resolution: straight-alpha sRGB uint8."""
    a = np.asarray(Image.open(beauty_path).convert('RGBA')).astype(np.float64) / 255.0
    alpha = a[..., 3:4]
    # A figure touching the render's edge was cut off by its canvas (SPECS in BuildBlenderFoundryEnemies.py).
    if max(alpha[0].max(), alpha[-1].max(), alpha[:, 0].max(), alpha[:, -1].max()) > 0:
        sys.exit('%s touches the edge of its render canvas: widen the canvas' % beauty_path)
    lin = to_linear(a[..., :3]) * alpha
    h, w = alpha.shape[0] // ss, alpha.shape[1] // ss
    lin = lin.reshape(h, ss, w, ss, 3).mean(axis=(1, 3))
    alpha = alpha.reshape(h, ss, w, ss, 1).mean(axis=(1, 3))
    m = np.asarray(Image.open(mask_path)).astype(np.float64)
    m = to_linear(m / (65535.0 if m.max() > 255 else 255.0))
    if m.ndim == 3:
        m = m[..., 0]
    m = m.reshape(h, ss, w, ss).mean(axis=(1, 3))
    # The seams light the stone round them: a soft ember bloom from the lava mask, kept inside the figure (the
    # painted Grunt casts no halo on the floor) and strongest where it is thin, so wide lava does not wash out.
    glow = blur(m, BLOOM_SIGMA)[..., None]
    lin = lin + to_linear(np.array(BLOOM) / 255.0) * np.clip(glow * BLOOM_GAIN, 0, BLOOM_CAP) * alpha
    # The Runner's heart (mask above a seam's 0.5) lights its whole chest: a second, wider bloom from it alone.
    heart = blur(np.clip(m - 0.5, 0, 0.5) * 2, HEART_SIGMA)[..., None]
    lin = lin + to_linear(np.array(HEART) / 255.0) * np.clip(heart * HEART_GAIN, 0, 0.7) * alpha
    lin, alpha = outline(lin, alpha)
    return finish(lin, alpha)


# ---------------------------------------------------------------------------------------------------------------------
# The Grunt Captain: the painted Grunt's own masters, registered exactly as its sprites were, a size up and red-hot
# ---------------------------------------------------------------------------------------------------------------------
GRUNT_SET = ROOT / 'ArtDirection/2026-09-17/grunt-polish-01'
REGISTRATION = json.loads((GRUNT_SET / 'registration.json').read_text())
CAPTAIN_SCALE = 1.15            # the Captain is the Grunt a size up (pixel looks 35x40 against 31x30)
CAPTAIN_CANVAS = 208            # final texels, square: the Grunt's 180 canvas at the Captain's scale, rounded to 4
CAPTAIN_PIVOT = (104, 21)       # the floor root's texel corner from the bottom left (the Grunt's 90, 18, scaled)
WORK = 4                        # dressed at 4x, then box-filtered down
WORK_SIZE = CAPTAIN_CANVAS * WORK
WORK_ROOT = (CAPTAIN_PIVOT[0] * WORK, WORK_SIZE - CAPTAIN_PIVOT[1] * WORK)
# The Grunt's 640-texel registered canvas spans its 180 sprite texels; the Captain's spans 180 * 1.15 * 4 work texels.
WORK_PER_CANVAS = 180 * CAPTAIN_SCALE * WORK / REGISTRATION['canvas']


def premultiplied(image):
    a = np.asarray(image.convert('RGBA')).astype(np.float64) / 255.0
    alpha = a[..., 3:4]
    return np.concatenate([to_linear(a[..., :3]) * alpha, alpha], axis=-1)


def resize_premultiplied(px, size):
    """Lanczos on each premultiplied linear channel, so edges never pick up a dark fringe."""
    out = [np.asarray(Image.fromarray(px[..., c].astype(np.float32), 'F').resize(size, Image.LANCZOS))
           for c in range(px.shape[2])]
    out = np.clip(np.stack(out, axis=-1), 0, None)
    out[..., 3] = np.clip(out[..., 3], 0, 1)
    out[..., :3] = np.minimum(out[..., :3], out[..., 3:4])
    return out


def registered(name):
    """One Grunt master cell on the Captain's work canvas (premultiplied linear RGBA): the cell isolated from its sheet
    (registration.json), scaled by its sheet's factor and the Captain's size, its floor root on the work root."""
    frame = next(f for f in REGISTRATION['frames'] if f['name'] == name)
    sheet = Image.open(GRUNT_SET / frame['file']).convert('RGBA')
    x0, y0, x1, y1 = frame['bounds']
    cell = premultiplied(sheet.crop((x0, y0, x1, y1)))
    k = frame['sourceScale'] * WORK_PER_CANVAS
    size = (round((x1 - x0) * k), round((y1 - y0) * k))
    cell = resize_premultiplied(cell, size)
    left = round((x0 - frame['root'][0]) * k + WORK_ROOT[0])
    top = round((y0 - frame['root'][1]) * k + WORK_ROOT[1])
    canvas = np.zeros((WORK_SIZE, WORK_SIZE, 4))
    cx0, cy0 = max(0, left), max(0, top)
    cx1, cy1 = min(WORK_SIZE, left + size[0]), min(WORK_SIZE, top + size[1])
    canvas[cy0:cy1, cx0:cx1] = cell[cy0 - top:cy1 - top, cx0 - left:cx1 - left]
    return canvas


# OKLab, for turning the basalt's hue while keeping every painted value step.
M1 = np.array([[0.4122214708, 0.5363325363, 0.0514459929], [0.2119034982, 0.6806995451, 0.1073969566],
               [0.0883024619, 0.2817188376, 0.6299787005]])
M2 = np.array([[0.2104542553, 0.7936177850, -0.0040720468], [1.9779984951, -2.4285922050, 0.4505937099],
               [0.0259040371, 0.7827717662, -0.8086757660]])


def to_oklab(lin):
    lms = np.cbrt(np.maximum(lin @ M1.T, 0))
    return lms @ M2.T


def from_oklab(lab):
    lms = lab @ np.linalg.inv(M2).T
    return np.maximum((lms ** 3) @ np.linalg.inv(M1).T, 0)


def red_hot(px, heat=1.0):
    """The Captain's heat: the basalt turned from violet to a dark ember red, a step deeper, every value step kept;
    the seams hotter and brighter; and the stone round each seam lit red from within. `heat` below 1 lets the
    remains cool: less of both, over the masters' own dimmed seams."""
    alpha = px[..., 3:4]
    straight = np.where(alpha > 1e-6, px[..., :3] / np.maximum(alpha, 1e-6), 0)
    srgb = to_srgb(straight)
    warm = srgb[..., 0] - srgb[..., 2]
    lava = np.clip((warm - 0.18) / 0.22, 0, 1)[..., None]           # the masters' molten seams, by their warmth
    lab = to_oklab(straight)
    L, a, b = lab[..., 0:1], lab[..., 1:2], lab[..., 2:3]
    hue = np.arctan2(b, a) + np.radians(HEAT_HUE_TURN)
    chroma = np.hypot(a, b) * HEAT_CHROMA + HEAT_CHROMA_ADD
    stone = from_oklab(np.concatenate([L * HEAT_DARKEN, chroma * np.cos(hue), chroma * np.sin(hue)], axis=-1))
    hot = straight * (1 - HEAT_SEAM * heat) + to_linear(np.array(HEAT_SEAM_COLOUR) / 255.0) * HEAT_SEAM * heat
    rgb = stone * (1 - lava) + hot * lava
    # Lit from within: the seams' light spreads over the stone round them, redder and wider than the Grunt's.
    glow = blur(lava[..., 0] * alpha[..., 0], HEAT_GLOW_SIGMA)[..., None]
    rgb = rgb + to_linear(np.array(HEAT_GLOW) / 255.0) * np.clip(glow * HEAT_GLOW_GAIN * heat, 0, 0.5) * (1 - lava)
    return np.concatenate([rgb * alpha, alpha], axis=-1)


HEAT_HUE_TURN = 90.0             # violet basalt (OKLab hue ~ 300) to a dark ember red (~ 30)
HEAT_CHROMA = 1.35
HEAT_CHROMA_ADD = 0.012
HEAT_DARKEN = 0.8
HEAT_SEAM = 0.35
HEAT_SEAM_COLOUR = (0xFF, 0xC8, 0x4A)
HEAT_GLOW = (0xFF, 0x4A, 0x18)
HEAT_GLOW_SIGMA = 9.0            # work texels: about two final texels
HEAT_GLOW_GAIN = 0.9


def downsample(px, factor=WORK):
    h, w = px.shape[0] // factor, px.shape[1] // factor
    return px.reshape(h, factor, w, factor, px.shape[2]).mean(axis=(1, 3))


CAPTAIN_FRAMES = ['front-idle', 'front-step-a', 'front-step-b', 'front-strike', 'front-pass-a', 'front-pass-b',
                  'rear-idle', 'rear-step-a', 'rear-step-b', 'rear-strike', 'rear-pass-a', 'rear-pass-b',
                  'death-collapse', 'death-cool']


def captain_work(out_dir):
    """The registered, recoloured work frames with a 20-texel grid: for placing the brass by eye."""
    from PIL import ImageDraw
    out_dir.mkdir(parents=True, exist_ok=True)
    for name in CAPTAIN_FRAMES:
        px = red_hot(registered(name))
        img = Image.fromarray(finish(px[..., :3], px[..., 3:4]), 'RGBA')
        bg = Image.new('RGBA', img.size, (40, 40, 52, 255))
        bg.alpha_composite(img)
        d = ImageDraw.Draw(bg)
        for v in range(0, WORK_SIZE, 40):
            d.line((v, 0, v, WORK_SIZE), fill=(90, 90, 110, 255) if v % 200 else (150, 150, 180, 255))
            d.line((0, v, WORK_SIZE, v), fill=(90, 90, 110, 255) if v % 200 else (150, 150, 180, 255))
        for v in range(0, WORK_SIZE, 200):
            d.text((v + 2, 2), str(v), fill=(255, 255, 255, 255))
            d.text((2, v + 2), str(v), fill=(255, 255, 255, 255))
        d.line((WORK_ROOT[0] - 10, WORK_ROOT[1], WORK_ROOT[0] + 10, WORK_ROOT[1]), fill=(80, 220, 255, 255), width=3)
        bg.save(out_dir / ('captain-work_%s.png' % name))
    print('CAPTAIN_WORK', out_dir)




# Where the Captain's brass sits on each frame, in work texels (the 4x canvas, from its top left), placed by eye on the
# registered frames (captain_work): the head rock's left and right edges and its top, the brim's height, a turn in
# degrees (clockwise) for a tipped head; the bell's hanging loop and the two ends of its chain at the neck (front);
# the chain's ends across the back of the neck (rear); the fallen helm and bell in the remains (death).
CAPTAIN_DRESS = {
    'front-idle': dict(head=(320, 440, 300), brim=343, bell=(386, 452), chain=((334, 420), (430, 410))),
    'front-step-a': dict(head=(315, 428, 315), brim=366, bell=(372, 474), chain=((326, 440), (420, 432))),
    'front-step-b': dict(head=(300, 430, 295), brim=346, bell=(366, 455), chain=((314, 425), (420, 415))),
    'front-strike': dict(head=(380, 500, 320), brim=366, bell=(436, 470), chain=((392, 440), (490, 432)), turn=-6),
    'front-pass-a': dict(head=(280, 410, 285), brim=335, bell=(342, 445), chain=((292, 414), (400, 404))),
    'front-pass-b': dict(head=(280, 410, 290), brim=342, bell=(346, 452), chain=((292, 420), (400, 410))),
    'rear-idle': dict(head=(270, 440, 215), brim=262, back=((292, 300), (424, 304))),
    'rear-step-a': dict(head=(275, 410, 235), brim=278, back=((292, 314), (398, 316))),
    'rear-step-b': dict(head=(275, 445, 230), brim=276, back=((296, 314), (428, 318))),
    'rear-strike': dict(head=(360, 530, 245), brim=292, back=((382, 334), (514, 338)), turn=8),
    'rear-pass-a': dict(head=(325, 475, 235), brim=278, back=((344, 312), (458, 314))),
    'rear-pass-b': dict(head=(315, 470, 238), brim=280, back=((334, 314), (452, 316))),
    'death-collapse': dict(helm=(318, 520, 1.0, 0), bell=(484, 706, 1.0, 0)),
    'death-cool': dict(helm=(316, 540, 1.0, 0), bell=(486, 716, 1.0, 0)),
}
CAPTAIN_HEAT = {'death-collapse': 0.8, 'death-cool': 0.35}
BRASS = RENDER / 'captain-brass'
BRASS_LIGHT, BRASS_MID, BRASS_DARK, BRASS_INK = (0xFF, 0xE2, 0x9A), (0xD8, 0xA0, 0x4C), (0x8A, 0x5A, 0x2A), (0x2A, 0x16, 0x0C)


def brass_prop(name):
    meta = json.loads((BRASS / 'frames.json').read_text())
    px = premultiplied(Image.open(BRASS / (name + '.png')))
    ss = meta['supersample']
    pivot = (meta['pivot'][0] * ss, px.shape[0] - meta['pivot'][1] * ss)
    return px, pivot


def over(canvas, layer):
    return layer + canvas * (1 - layer[..., 3:4])


def place_prop(canvas, prop, at, scale, turn=0.0):
    """Lays a rendered prop on the canvas: its anchor on `at`, scaled and turned (degrees, clockwise) about it."""
    px, pivot = prop
    c, s = math.cos(math.radians(turn)), math.sin(math.radians(turn))
    a, b, d, e = c / scale, s / scale, -s / scale, c / scale
    data = (a, b, pivot[0] - (a * at[0] + b * at[1]), d, e, pivot[1] - (d * at[0] + e * at[1]))
    size = (canvas.shape[1], canvas.shape[0])
    layer = np.stack([np.asarray(Image.fromarray(px[..., k].astype(np.float32), 'F').transform(
        size, Image.AFFINE, data, resample=Image.BICUBIC)) for k in range(4)], axis=-1)
    layer = np.clip(layer, 0, None)
    layer[..., 3] = np.clip(layer[..., 3], 0, 1)
    layer[..., :3] = np.minimum(layer[..., :3], layer[..., 3:4])
    return over(canvas, layer)


def chain(canvas, points, links=13):
    """A brass chain through the points (a sagging curve), drawn link by link at the work resolution: a dark edge,
    the brass and a lit fleck on every other link, so it reads as a chain and not as a cord."""
    from PIL import ImageDraw
    layer = Image.new('RGBA', (canvas.shape[1], canvas.shape[0]), (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    path = []
    for p0, p1, sag in points:
        for i in range(links + 1):
            t = i / links
            x = p0[0] + (p1[0] - p0[0]) * t
            y = p0[1] + (p1[1] - p0[1]) * t + sag * 4 * t * (1 - t)
            path.append((x, y))
    draw.line(path, fill=BRASS_INK + (255,), width=11, joint='curve')
    draw.line(path, fill=BRASS_DARK + (255,), width=7, joint='curve')
    for k, (x, y) in enumerate(path):
        r = 3.2 if k % 2 else 2.4
        draw.ellipse((x - r, y - r, x + r, y + r), fill=(BRASS_MID if k % 2 else BRASS_LIGHT) + (255,))
    return over(canvas, premultiplied(layer))


def captain_frame(name):
    """One Grunt Captain frame at the game's resolution: straight-alpha sRGB uint8, CAPTAIN_CANVAS square."""
    px = red_hot(registered(name), CAPTAIN_HEAT.get(name, 1.0))
    dress = CAPTAIN_DRESS[name]
    if 'head' in dress:
        x0, x1, _ = dress['head']
        front = name.startswith('front')
        helm = brass_prop('helm-front' if front else 'helm-rear')
        # The helm is a little wider than the head rock from the front; from the back the top rock reads wider than
        # the head, so the helm takes less of it.
        width = (x1 - x0) * (1.1 if front else 0.86)
        scale = width / (2 * 0.124 * PPU * WORK)
        if front:
            px = chain(px, [(dress['chain'][0], dress['bell'], 10), (dress['bell'], dress['chain'][1], 10)])
            px = place_prop(px, brass_prop('bell-front'), dress['bell'], CAPTAIN_SCALE)
        else:
            px = chain(px, [(dress['back'][0], dress['back'][1], 22)], links=16)
        px = place_prop(px, helm, ((x0 + x1) / 2, dress['brim']), scale, dress.get('turn', 0.0))
    else:
        hx, hy, hs, ht = dress['helm']
        bx, by, bs, bt = dress['bell']
        px = place_prop(px, brass_prop('bell-fallen'), (bx, by), CAPTAIN_SCALE * bs, bt)
        px = place_prop(px, brass_prop('helm-fallen'), (hx, hy), CAPTAIN_SCALE * hs, ht)
    small = downsample(px)
    return finish(small[..., :3], small[..., 3:4])


# ---------------------------------------------------------------------------------------------------------------------
# Assets: TGA frames and their flash silhouettes, metas modelled on the Grunt's, the EnemyArtSet
# ---------------------------------------------------------------------------------------------------------------------
ART = ROOT / 'Assets/_Project/Art/Characters'
SET_SCRIPT_GUID = 'ec76f54135d844f881ee6222cf455ad2'      # Scripts/Art/Unity/EnemyArtSet.cs.meta
FRAME_ORDER = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b']   # EnemyArtSet indices 0-5
ENEMIES = {
    # folder, file stem, set name; the set's walk cycle in floor units (distance-driven: EnemyLookView)
    'runner': dict(folder='Runner', stem='Runner', set='RunnerArtSet', cycle=1.0),
    'tank': dict(folder='Tank', stem='Tank', set='TankArtSet', cycle=0.6),
    'captain': dict(folder='GruntCaptain', stem='Captain', set='GruntCaptainArtSet', cycle=0.8),
}
# Transparent texels kept round the figures' union, so bilinear sampling never reads past an edge.
MARGIN = 2
TEXTURE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 9
  spritePivot: {{x: {px:.8f}, y: {py:.8f}}}
  spritePixelsToUnits: 128
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: {android}
    resizeAlgorithm: 0
    textureFormat: 48
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 1
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def existing_guid(meta):
    if meta.exists():
        m = re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(encoding='utf-8'), re.M)
        if m:
            return m.group(1)
    return uuid.uuid4().hex


def write_tga(path, pixels):
    """Uncompressed 32-bit TGA, bottom-left origin, as the Grunt's (BuildGruntAssets.WriteTga)."""
    h, w = pixels.shape[:2]
    header = bytes([0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, w & 255, w >> 8, h & 255, h >> 8, 32, 8])
    bgra = pixels[::-1][..., [2, 1, 0, 3]]
    path.write_bytes(header + bgra.astype(np.uint8).tobytes())


def flash_of(pixels):
    """The hit flash: the body's own alpha in white, white under the transparent texels too, as the Grunt's."""
    out = np.full_like(pixels, 255)
    out[..., 3] = pixels[..., 3]
    return out


def frames_of(enemy):
    """Every frame of one enemy at the game's resolution on its full render canvas, with the canvas pivot."""
    if enemy == 'captain':
        names = ['%s-%s' % (f, p) for f in ('front', 'rear') for p in FRAME_ORDER] + ['death-collapse', 'death-cool']
        return {n: captain_frame(n) for n in names}, (CAPTAIN_PIVOT[0], CAPTAIN_PIVOT[1])
    meta = json.loads((RENDER / enemy / 'frames.json').read_text())
    frames = {f['name']: process_render(RENDER / enemy / f['file'], RENDER / enemy / f['mask'], meta['supersample'])
              for f in meta['frames']}
    return frames, tuple(meta['pivot'])


def fit(frames, pivot):
    """One canvas for every frame: the union of their texels and a margin, symmetric about the pivot so the body's
    bounds centre on the feet (BurnFlames centres its flame on them), each side a whole number of ASTC blocks."""
    h = next(iter(frames.values())).shape[0]
    left = right = top = bottom = 0
    for px in frames.values():
        ys, xs = np.nonzero(px[..., 3] > 0)
        left = max(left, pivot[0] - xs.min())
        right = max(right, xs.max() + 1 - pivot[0])
        top = max(top, (h - pivot[1]) - ys.min())
        bottom = max(bottom, ys.max() + 1 - (h - pivot[1]))
    half = -(-(max(left, right) + MARGIN) // 2) * 2
    up, down = top + MARGIN, bottom + MARGIN
    height = -(-(up + down) // 4) * 4
    up = height - down
    x0, y0 = pivot[0] - half, (h - pivot[1]) - up
    cropped = {}
    for name, px in frames.items():
        canvas = np.zeros((height, 2 * half, 4), np.uint8)
        sx0, sy0 = max(0, x0), max(0, y0)
        sx1, sy1 = min(px.shape[1], x0 + 2 * half), min(px.shape[0], y0 + height)
        canvas[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = px[sy0:sy1, sx0:sx1]
        cropped[name] = canvas
    return cropped, (int(half), int(down))


def measures(frames, pivot):
    """The set's numbers, in world units, from the art itself: VisualTop clears every living pose's top by the Grunt's
    margin (its 1.05 over a 1.0 strike); the contact shadow is the Grunt's 0.78 of the idle body's width."""
    h = next(iter(frames.values())).shape[0]
    top = 0
    for name, px in frames.items():
        if name.startswith('death'):
            continue
        ys, _ = np.nonzero(px[..., 3] > 128)
        top = max(top, (h - pivot[1]) - ys.min())
    _, xs = np.nonzero(frames['front-idle'][..., 3] > 128)
    width = (xs.max() + 1 - xs.min()) / PPU
    return round(top / PPU + 0.05, 2), round(0.78 * width, 2)


def art_set(name, refs, deaths, visual_top, shadow, cycle):
    lines = ['%YAML 1.1', '%TAG !u! tag:unity3d.com,2011:', '--- !u!114 &11400000', 'MonoBehaviour:',
             '  m_ObjectHideFlags: 0', '  m_CorrespondingSourceObject: {fileID: 0}', '  m_PrefabInstance: {fileID: 0}',
             '  m_PrefabAsset: {fileID: 0}', '  m_GameObject: {fileID: 0}', '  m_Enabled: 1', '  m_EditorHideFlags: 0',
             '  m_Script: {fileID: 11500000, guid: %s, type: 3}' % SET_SCRIPT_GUID, '  m_Name: %s' % name,
             '  m_EditorClassIdentifier: ']
    for facing in ('front', 'rear'):
        lines.append('  _%s:' % facing)
        for pose in FRAME_ORDER:
            body, flash = refs['%s-%s' % (facing, pose)]
            lines.append('  - Body: {fileID: 21300000, guid: %s, type: 3}' % body)
            lines.append('    Flash: {fileID: 21300000, guid: %s, type: 3}' % flash)
    lines.append('  _death:')
    for guid in deaths:
        lines.append('  - {fileID: 21300000, guid: %s, type: 3}' % guid)
    lines += ['  _visualTop: %s' % visual_top, '  _shadowWidth: %s' % shadow, '  _walkCycleDistance: %s' % cycle]
    return '\n'.join(lines) + '\n'


def astc_bytes(w, h):
    return ((w + 3) // 4) * ((h + 3) // 4) * 16


def pack(enemy):
    spec = ENEMIES[enemy]
    frames, pivot = frames_of(enemy)
    frames, pivot = fit(frames, pivot)
    folder = ART / spec['folder']
    folder.mkdir(parents=True, exist_ok=True)
    folder_meta = ART / (spec['folder'] + '.meta')
    if not folder_meta.exists():
        folder_meta.write_text('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                               '  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex,
                               encoding='utf-8', newline='\n')
    h, w = next(iter(frames.values())).shape[:2]
    pivot_norm = (pivot[0] / w, pivot[1] / h)
    refs, deaths, textures = {}, [], []
    for name, px in frames.items():
        guids = []
        for flash in (False, True):
            path = folder / ('CHR_%s_%s%s.tga' % (spec['stem'], name, '_Flash' if flash else ''))
            meta = Path(str(path) + '.meta')
            guid = existing_guid(meta)
            write_tga(path, flash_of(px) if flash else px)
            meta.write_text(TEXTURE_META.format(guid=guid, px=pivot_norm[0], py=pivot_norm[1],
                                                android=256 if max(w, h) <= 256 else 512),
                            encoding='utf-8', newline='\n')
            textures.append(dict(file=path.name, guid=guid, size=[w, h], astc_4x4_bytes=astc_bytes(w, h)))
            guids.append(guid)
            if name.startswith('death'):
                break
        if name.startswith('death'):
            deaths.append(guids[0])
        else:
            refs[name] = guids
    visual_top, shadow = measures(frames, pivot)
    set_path = folder / (spec['set'] + '.asset')
    set_guid = existing_guid(Path(str(set_path) + '.meta'))
    set_path.write_text(art_set(spec['set'], refs, [deaths[0], deaths[1]] if len(deaths) == 2 else deaths,
                                visual_top, shadow, spec['cycle']), encoding='utf-8', newline='\n')
    Path(str(set_path) + '.meta').write_text('fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n'
                                             '  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n'
                                             '  assetBundleVariant: \n' % set_guid, encoding='utf-8', newline='\n')
    total = sum(t['astc_4x4_bytes'] for t in textures)
    print('FOUNDRY_PACKED %s %dx%d pivot %s visualTop %s shadow %s cycle %s textures %d ASTC4x4 %d bytes'
          % (enemy, w, h, pivot, visual_top, shadow, spec['cycle'], len(textures), total))
    return dict(enemy=enemy, art_set=dict(file=set_path.name, guid=set_guid), size=[w, h], pivot=list(pivot),
                pivot_normalised=[round(pivot_norm[0], 8), round(pivot_norm[1], 8)], visual_top=visual_top,
                shadow_width=shadow, walk_cycle_distance=spec['cycle'], textures=textures, astc_4x4_bytes=total)


def preview_renders(enemy, out_dir):
    meta = json.loads((RENDER / enemy / 'frames.json').read_text())
    out_dir.mkdir(parents=True, exist_ok=True)
    for f in meta['frames']:
        pixels = process_render(RENDER / enemy / f['file'], RENDER / enemy / f['mask'], meta['supersample'])
        Image.fromarray(pixels, 'RGBA').save(out_dir / ('%s_%s.png' % (enemy, f['name'])))
    print('FOUNDRY_PREVIEW', enemy, len(meta['frames']), out_dir)


def main():
    if '--captain-work' in ARGS:
        captain_work(Path(arg('--captain-work')))
        return
    if PREVIEW:
        for enemy in ONLY:
            if (RENDER / enemy / 'frames.json').exists():
                preview_renders(enemy, Path(PREVIEW))
        return

    report = [pack(enemy) for enemy in ONLY]
    DOCS.mkdir(parents=True, exist_ok=True)
    sprites = DOCS / 'sprites.json'
    known = {r['enemy']: r for r in json.loads(sprites.read_text()).get('enemies', [])} if sprites.exists() else {}
    known.update({r['enemy']: r for r in report})
    everything = [known[e] for e in ENEMIES if e in known]
    sprites.write_text(json.dumps(dict(pixels_per_unit=PPU, enemies=everything,
                                       total_astc_4x4_bytes=sum(r['astc_4x4_bytes'] for r in everything)), indent=1) + '\n',
                       encoding='utf-8', newline='\n')
    print('FOUNDRY_TOTAL ASTC4x4 %d bytes (%.2f MB) for %s' % (sum(r['astc_4x4_bytes'] for r in everything),
                                                               sum(r['astc_4x4_bytes'] for r in everything) / 2 ** 20,
                                                               ', '.join(r['enemy'] for r in everything)))


if __name__ == '__main__':
    main()
