"""Packs Rootmother Myrr's raw Blender frames (BuildBlenderRootmother.py) into the game's sprites and her EnemyArtSet, on
the Glass Regent's contract (PackGlassRegentSprites.py; the Forge Warden's, PackWardenSprites.py, before it).

    python Tools/ArtReview/PackRootmotherSprites.py [--render DIR] [--docs DIR]

--render: BuildBlenderRootmother.py's --out (default TestResults/rootmother-render); --docs: where frames.json goes
(default ArtDirection/2026-11-25/rootmother-02, untracked).

Per pose: the 4x render is box-filtered down in linear light with premultiplied alpha; the emission pass's three
channels (red the seed-pod heart, green the buds, the blooms and the eyes, blue the second phase's root veins) are
coloured orchid and added with their own gains (EMISSION below: tune a group's value here, no render needed); then the
one-texel dark outline in her own ink, then each group's soft bloom laid over it in light, so a glow never gets a dark
ring. The bloom's last fringe - texels fainter than RECT_ALPHA outside the figure, a halo nobody sees on the loam - is
cut, so it never widens a rect. The first phase's frame, the second's and the white flash share one rect, symmetric
about the pivot, each side a whole number of 4x4 ASTC blocks, exactly as the Regent's. The fall is trimmed tight and
carries its contact shadow, as the Regent's.

The rows (EnemyArtSet, the Regent's order): idle (her idle-a), contact A, contact B, the strike (strike-1: the hook
driven into the floor, the roots bursting round it), passing A, passing B; the standing windup (the telegraph's hold,
the walking windup's contact B); the walking windup's quarters; the second phase's drawings of them all; the fall
(struck, sink, slump, stump - from the slump the facings share their drawings), whose sink and stump are the two
remains every painted enemy leaves. Her other review clips (idle-b, the telegraph's gather and peak, strike-0 and -2,
the phase change) are not in the set: the game's tell, kick and phase burst are BossFeelView's.

Every .meta is the Glass Regent's sprite .meta (the same importer as the Warden's: one sprite, 128 texels a unit,
bilinear, no mipmaps, not readable, ASTC 4x4 on Android, at most 512) with a fresh GUID and this sprite's pivot; a white
flash, which carries no colour, takes ASTC 6x6 on Android (FLASH_ANDROID_FORMAT) to keep her memory near the Regent's.
Writes, all regenerated on every run:

    Assets/_Project/Art/Characters/RootmotherMyrr/CHR_Myrr_<facing>_<pose>.png, _hot.png, _Flash.png, the fall's
        CHR_Myrr_<facing>_fall-struck.png, CHR_Myrr_fall-slump.png, CHR_Myrr_death_<k>.png (+ .meta)
    Assets/_Project/Art/Characters/RootmotherMyrr/RootmotherMyrrArtSet.asset (+ .meta): laid out field for field as
        GlassRegentArtSet.asset, _frost and _gloam empty (born on floor 3 in the garden's palette: docs/52 section 2
        rule 6)
    <docs>/frames.json: every sprite's size and pivot, and the memory figures beside the Regent's

Existing GUIDs are kept, so re-running never breaks a reference; a new file gets a fresh uuid4 GUID.
"""
import hashlib
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


def arg(name, default):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


RENDER = Path(arg('--render', str(ROOT / 'TestResults/rootmother-render')))
DOCS = Path(arg('--docs', str(ROOT / 'ArtDirection/2026-11-25/rootmother-02')))
CHARACTERS = ROOT / 'Assets/_Project/Art/Characters'
OUT = CHARACTERS / 'RootmotherMyrr'
PREFIX = 'CHR_Myrr_'
SET_NAME = 'RootmotherMyrrArtSet'
# The importer every sprite copies: the Glass Regent's (itself the Forge Warden's).
TEMPLATE_META = CHARACTERS / 'GlassRegent/CHR_GlassRegent_front_idle.png.meta'
REGENT_FRAMES = CHARACTERS / 'GlassRegent'
SET_SCRIPT_GUID = 'ec76f54135d844f881ee6222cf455ad2'    # Scripts/Art/Unity/EnemyArtSet.cs.meta
# EnemyArtSet's frame order: idle, contact A, contact B, strike, then the appended passing A and B (HasPassingFrames).
FRAME_ORDER = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b']
# The rendered drawing each of the set's poses takes where its name differs.
RENDERED = {'idle': 'idle-a', 'strike': 'strike-1'}
FACINGS = ['front', 'rear']
OUTLINE = (0x12, 0x0D, 0x0B)       # her ink (BuildBlenderRootmother.py's OUTLINE)
MARGIN = 2
ANDROID_MAX = 512
# The bloom's last fringe: texels fainter than this (of 255) outside the figure are cut before the rect is drawn.
RECT_ALPHA = 12
# The bar hangs from the canopy's top over her root (the branches within this half-width of it) plus headroom.
VISUAL_TOP_HALF_WIDTH = 0.3
VISUAL_TOP_HEADROOM = 0.08
# The contact shadow under her root skirt; the Warden's is 1.12, the Regent's 1.0 - her skirt is wider than either.
SHADOW_WIDTH = 1.2
# The telegraph standing, then carried at a walk: _windupWalk's quarters are contact A, passing A, contact B - the
# standing windup's own drawing - and passing B.
WINDUP_POSES = ['windup', 'windup-step-a', 'windup-pass-a', 'windup-pass-b']
WALK_QUARTERS = ['windup-step-a', 'windup-pass-a', 'windup', 'windup-pass-b']
FALLS = ['fall-struck', 'fall-sink', 'fall-slump', 'fall-stump']
SHARED_FALLS = {'fall-slump', 'fall-stump'}
REMAINS = {'fall-sink': PREFIX + 'death_0', 'fall-stump': PREFIX + 'death_1'}
# Android's format by kind (TextureImporterFormat): ASTC 4x4 (48) for every drawing; the white flashes ASTC 6x6 (50).
BODY_ANDROID_FORMAT = 48
FLASH_ANDROID_FORMAT = 50

# The emission groups, per phase: (edge colour, core colour, gain, bloom gain, wide bloom gain). The mask (0..1, above 1
# when the telegraph flares and in the second phase, which the render already carries) runs the colour from edge to core
# and scales it. Orchid throughout (docs/51: the creature's colour), the cores toward the pale orchid of her blooms'
# hearts - never red-orange (the snare's) and never gloamlight (the garden's). Tuned to the look the owner approved
# (review_rootmother.py's lighting of pass 1.1): about the same share of near-white texels in the buds and the blooms,
# and the same pink in them; the second phase's masks run hotter, so its gains are lower.
EMISSION = {
    'cool': {'heart': ('#C45ABC', '#F6C2F0', 0.7, 0.5, 0.2), 'bloom': ('#D86BD0', '#F6C2F0', 0.8, 0.55, 0.2),
             'vein': ('#B04AA8', '#E89BE0', 0.55, 0.4, 0.12)},
    'hot': {'heart': ('#C45ABC', '#F6C2F0', 0.62, 0.45, 0.18), 'bloom': ('#D86BD0', '#F2B4EC', 0.66, 0.48, 0.18),
            'vein': ('#B04AA8', '#E89BE0', 0.45, 0.3, 0.1)},
}
CHANNEL = {'heart': 0, 'bloom': 1, 'vein': 2}
BLOOM_SIGMA = (1.2, 3.8)       # the tight halo and the wide one, in texels
HALO_ALPHA = 0.85
SHADOW_TEXEL_COUNT = 32
SHADOW_STEPS = ((0.18, 132), (0.42, 96), (0.72, 58), (1.0, 24))
SHADOW_RGB = (9, 10, 20)
# She sinks where she stood: the root's own shadow stays under every drawing of the fall.
ROOT_SHADOW = {'fall-struck': 1.0, 'fall-sink': 1.0, 'fall-slump': 1.0, 'fall-stump': 0.9}
CONTACT_GAP = 0.3
CONTACT_SPREAD = 0.08


def hexrgb(h):
    h = h.lstrip('#')
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)]) / 255.0


def to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def blur(image, sigma):
    radius = max(1, int(sigma * 3 + 0.5))
    x = np.arange(-radius, radius + 1)
    kernel = np.exp(-x * x / (2 * sigma * sigma))
    kernel /= kernel.sum()
    out = image
    for axis in (0, 1):
        padded = np.pad(out, [(radius, radius) if a == axis else (0, 0) for a in range(3)])
        acc = np.zeros_like(out)
        for k, weight in enumerate(kernel):
            acc += weight * (padded[k:k + out.shape[0]] if axis == 0 else padded[:, k:k + out.shape[1]])
        out = acc
    return out


def downsample(path, ss):
    a = np.asarray(Image.open(path).convert('RGBA')).astype(np.float64) / 255.0
    alpha = a[..., 3:4]
    lin = to_linear(a[..., :3]) * alpha
    h, w = alpha.shape[0] // ss, alpha.shape[1] // ss
    return (lin.reshape(h, ss, w, ss, 3).mean(axis=(1, 3)), alpha.reshape(h, ss, w, ss, 1).mean(axis=(1, 3)))


def masks_of(path, ss, scale):
    """The emission pass's three masks at the game's resolution (coverage-weighted, as the beauty is premultiplied)."""
    a = np.asarray(Image.open(path).convert('RGBA')).astype(np.float64) / 255.0
    m = a[..., :3] * a[..., 3:4] / scale
    h, w = m.shape[0] // ss, m.shape[1] // ss
    return m.reshape(h, ss, w, ss, 3).mean(axis=(1, 3))


def emission(masks, variant):
    """(crisp light added to the body, bloom light) in linear, from the masks and the variant's EMISSION table."""
    crisp = np.zeros(masks.shape)
    bloom = np.zeros(masks.shape)
    for group, (edge, core, gain, tight, wide) in EMISSION[variant].items():
        m = masks[..., CHANNEL[group]:CHANNEL[group] + 1]
        t = np.clip(m, 0, 1)
        colour = to_linear(hexrgb(edge)) * (1 - t) + to_linear(hexrgb(core)) * t
        light = colour * m * gain
        crisp += light
        bloom += blur(light, BLOOM_SIGMA[0]) * tight / max(gain, 1e-6) + blur(light, BLOOM_SIGMA[1]) * wide / max(gain, 1e-6)
    return crisp, bloom


def outline_ring(alpha):
    pad = np.pad(alpha[..., 0], 1)
    grown = np.maximum.reduce([pad[1:-1, 1:-1], pad[:-2, 1:-1], pad[2:, 1:-1], pad[1:-1, :-2], pad[1:-1, 2:],
                               0.7 * pad[:-2, :-2], 0.7 * pad[2:, 2:], 0.7 * pad[:-2, 2:], 0.7 * pad[2:, :-2]])[..., None]
    return grown * (1 - alpha)          # the ring's coverage outside the body


def finish(lin, alpha):
    straight = np.where(alpha > 1e-6, lin / np.maximum(alpha, 1e-6), 0)
    out = np.concatenate([to_srgb(straight), np.clip(alpha, 0, 1)], axis=-1)
    out = (out * 255 + 0.5).astype(np.uint8)
    out[out[..., 3] < 2] = 0
    return out


def flash_of(alpha):
    out = np.zeros(alpha.shape[:2] + (4,), np.uint8)
    out[..., :3] = 255
    out[..., 3] = (np.clip(alpha[..., 0], 0, 1) * 255 + 0.5).astype(np.uint8)
    out[out[..., 3] < 2] = 0
    return out


def processed(r):
    """(the finished frame, the crisp body alpha under the outline and the bloom) for one rendered frame."""
    lin, alpha = downsample(RENDER / r['file'], SS)
    crisp_light, bloom_light = emission(masks_of(RENDER / r['glow'], SS, GLOW_SCALE), r['variant'])
    lin = lin + crisp_light
    body = alpha.copy()
    ink = to_linear(np.array(OUTLINE) / 255.0)
    ring = outline_ring(alpha)
    lin, alpha = lin + ink * ring, alpha + ring
    amount = np.clip(bloom_light.max(axis=-1, keepdims=True), 0, 1)
    lin, alpha = lin + bloom_light, alpha + amount * HALO_ALPHA * (1 - alpha)
    return finish(lin, alpha), body


def visible(image):
    """The texels a rect must hold: everything but the bloom's last fringe."""
    return image[..., 3] >= RECT_ALPHA


def clear_outside(pixels, rect):
    """Cuts the fringe the rect leaves out, and keeps the rect's margin transparent."""
    x0, y0, x1, y1 = rect
    out = pixels.copy()
    keep = np.zeros(out.shape[:2], bool)
    keep[y0 + MARGIN:y1 - MARGIN, x0 + MARGIN:x1 - MARGIN] = True
    out[~keep] = 0
    return out


def tight_rect(image, pivot_x, pivot_row):
    y, x = np.nonzero(visible(image))
    h, w = image.shape[:2]
    if x.min() < MARGIN or y.min() < MARGIN or x.max() >= w - MARGIN or y.max() >= h - MARGIN:
        sys.exit('A fall drawing runs off its render canvas: widen FALL_CANVAS_PX in BuildBlenderRootmother.py')
    x0, x1 = min(x.min(), pivot_x) - MARGIN, max(x.max() + 1, pivot_x) + MARGIN
    y0, y1 = min(y.min(), pivot_row) - MARGIN, max(y.max() + 1, pivot_row) + MARGIN
    x0 -= (-(x1 - x0)) % 4
    y0 -= (-(y1 - y0)) % 4
    return max(0, x0), max(0, y0), x1, y1


def contact_shadow(record, height, width, pivot_x, pivot_row, shadow_width):
    texel = shadow_width / SHADOW_TEXEL_COUNT
    cx = (np.floor((np.arange(width) + 0.5 - pivot_x) / PPU / texel) + 0.5) * texel
    cy = (np.floor((pivot_row - np.arange(height) - 0.5) / PPU / texel) + 0.5) * texel
    x, y = np.meshgrid(cx, cy)
    reach = np.ones(x.shape)
    root = ROOT_SHADOW[record['pose']]
    if root > 0:
        e = (x / (15 * texel)) ** 2 + (y / (7 * texel)) ** 2
        reach = np.minimum(reach, 1 - root * (1 - np.minimum(e, 1)))
    for sx, sy, h, r in record['footprint']:
        weight = 1 - max(0.0, h - r) / CONTACT_GAP
        if weight <= 0:
            continue
        ax = r + CONTACT_SPREAD
        e = ((x - sx) / ax) ** 2 + ((y - sy) / (ax * 0.5)) ** 2
        reach = np.minimum(reach, 1 - weight * (1 - np.minimum(e, 1)))
    alpha = np.zeros(x.shape)
    for limit, value in reversed(SHADOW_STEPS):
        alpha = np.where(reach < limit, value / 255.0, alpha)
    return alpha


def over_shadow(pixels, shadow):
    a = pixels[..., 3:4] / 255.0
    s = shadow[..., None] * (1 - a)
    out_a = a + s
    rgb = (pixels[..., :3] / 255.0 * a + np.array(SHADOW_RGB) / 255.0 * s) / np.maximum(out_a, 1e-6)
    out = np.concatenate([rgb, out_a], axis=-1)
    out = (np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8)
    out[out[..., 3] < 2] = 0
    return out


def symmetric_rect(images, pivot_x, pivot_row):
    xs, ys = [], []
    for image in images:
        y, x = np.nonzero(visible(image))
        xs.extend([x.min(), x.max() + 1])
        ys.extend([y.min(), y.max() + 1])
    half = max(pivot_x - min(xs), max(xs) - pivot_x) + MARGIN
    half += half % 2
    y0, y1 = min(min(ys), pivot_row) - MARGIN, max(max(ys), pivot_row) + MARGIN
    y0 -= (-(y1 - y0)) % 4
    h, w = images[0].shape[:2]
    if pivot_x - half < 0 or pivot_x + half > w or y0 < 0 or y1 > h:
        sys.exit('A frame runs off the render canvas: widen CANVAS_PX in BuildBlenderRootmother.py')
    return pivot_x - half, y0, pivot_x + half, y1


def existing_guid(meta):
    if meta.exists():
        m = re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(encoding='utf-8'), re.M)
        if m:
            return m.group(1)
    return uuid.uuid4().hex


TEMPLATE = TEMPLATE_META.read_text(encoding='utf-8')
ANDROID_BLOCK = re.compile(r'(    buildTarget: Android\n(?:    [^\n]*\n)*?    textureFormat: )(\d+)')
assert ANDROID_BLOCK.search(TEMPLATE).group(2) == str(BODY_ANDROID_FORMAT), 'The Regent imports as ASTC 4x4 on Android.'


def write_sprite(name, pixels, pivot_y, android_format, pivot_x=0.5):
    path = OUT / (name + '.png')
    meta = Path(str(path) + '.meta')
    guid = existing_guid(meta)
    Image.fromarray(pixels, 'RGBA').save(path, optimize=True)
    text = re.sub(r'^guid: [0-9a-f]{32}', 'guid: ' + guid, TEMPLATE, count=1, flags=re.M)
    pivot = '{x: %s, y: %.8f}' % ('0.5' if pivot_x == 0.5 else '%.8f' % pivot_x, pivot_y)
    text = re.sub(r'^  spritePivot: \{[^}]*\}', '  spritePivot: ' + pivot, text, count=1, flags=re.M)
    text = ANDROID_BLOCK.sub(lambda m: m.group(1) + str(android_format), text, count=1)
    meta.write_text(text, encoding='utf-8', newline='\n')
    h, w = pixels.shape[:2]
    if max(w, h) > ANDROID_MAX:
        sys.exit('%s is %dx%d: Android would downscale it past %d' % (name, w, h, ANDROID_MAX))
    return dict(file=path.name, guid=guid, size=[w, h], android_format=android_format,
                pivot=[0.5 if pivot_x == 0.5 else round(pivot_x, 6), round(pivot_y, 6)],
                sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def ref(sprite):
    return '{fileID: 21300000, guid: %s, type: 3}' % sprite['guid']


def frames_yaml(field, frames):
    lines = ['  %s:' % field]
    for body, flash in frames:
        lines += ['  - Body: ' + ref(body), '    Flash: ' + ref(flash)]
    return lines


def folder_meta(folder):
    meta = Path(str(folder) + '.meta')
    if not meta.exists():
        meta.write_text('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                        '  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex,
                        encoding='utf-8', newline='\n')


def write_set(lines):
    path = OUT / (SET_NAME + '.asset')
    guid = existing_guid(Path(str(path) + '.meta'))
    head = ['%YAML 1.1', '%TAG !u! tag:unity3d.com,2011:', '--- !u!114 &11400000', 'MonoBehaviour:',
            '  m_ObjectHideFlags: 0', '  m_CorrespondingSourceObject: {fileID: 0}', '  m_PrefabInstance: {fileID: 0}',
            '  m_PrefabAsset: {fileID: 0}', '  m_GameObject: {fileID: 0}', '  m_Enabled: 1', '  m_EditorHideFlags: 0',
            '  m_Script: {fileID: 11500000, guid: %s, type: 3}' % SET_SCRIPT_GUID, '  m_Name: ' + SET_NAME,
            '  m_EditorClassIdentifier: ']
    path.write_text('\n'.join(head + lines) + '\n', encoding='utf-8', newline='\n')
    Path(str(path) + '.meta').write_text('fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n'
                                         '  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n'
                                         '  assetBundleVariant: \n' % guid, encoding='utf-8', newline='\n')
    return dict(file=str(path.relative_to(ROOT)).replace('\\', '/'), guid=guid)


def visual_top_of(crisp):
    body = crisp[:, int(pivot_x - VISUAL_TOP_HALF_WIDTH * PPU):int(pivot_x + VISUAL_TOP_HALF_WIDTH * PPU), 0]
    rows = np.nonzero((body > 0.5).any(axis=1))[0]
    return round((pivot_row - rows.min()) / PPU + VISUAL_TOP_HEADROOM, 3)


def astc_bytes(size, block):
    return math.ceil(size[0] / block) * math.ceil(size[1] / block) * 16


def memory(items):
    texels = sum(s['size'][0] * s['size'][1] for s in items)
    astc = sum(astc_bytes(s['size'], 6 if s.get('android_format') == 50 else 4) for s in items)
    return dict(textures=len(items), texels=texels, android_astc_bytes=astc, all_astc_4x4_bytes=texels,
                rgba32_bytes=texels * 4)


def regent_memory():
    """The Glass Regent's painted set beside hers: its textures in its own folder (not its copies), as ASTC 4x4."""
    items = []
    for png in sorted(REGENT_FRAMES.glob('CHR_GlassRegent_*.png')):
        with Image.open(png) as im:
            items.append(dict(size=list(im.size), android_format=BODY_ANDROID_FORMAT))
    return memory(items)


meta_in = json.loads((RENDER / 'frames.json').read_text())
SS, CANVAS, PIVOT, GLOW_SCALE = meta_in['supersample'], meta_in['canvas'], meta_in['pivot'], meta_in['glow_scale']
PPU = meta_in['ppu']
assert PPU == 128, 'Myrr is drawn at the painted Grunt\'s 128 texels a unit, as the Warden and the Regent.'
pivot_x, pivot_row = PIVOT[0], CANVAS[1] - PIVOT[1]
records = {(r['facing'], r['pose'], r['variant']): r for r in meta_in['frames']}


def need(facing, pose, variant):
    r = records.get((facing, pose, variant))
    if r is None:
        sys.exit('Missing render: %s %s %s (render every frame first)' % (facing, pose, variant))
    return r


OUT.mkdir(parents=True, exist_ok=True)
folder_meta(OUT)
DOCS.mkdir(parents=True, exist_ok=True)

written = []
sprites = {}
visual_top = None
for facing in FACINGS:
    for pose in FRAME_ORDER + WINDUP_POSES:
        source = RENDERED.get(pose, pose)
        cool, crisp = processed(need(facing, source, 'cool'))
        hot, _ = processed(need(facing, source, 'hot'))
        flash = flash_of(crisp)
        rect = symmetric_rect([cool, hot, flash], pivot_x, pivot_row)
        x0, y0, x1, y1 = rect
        pivot_y = (y1 - pivot_row) / (y1 - y0)
        stem = '%s%s_%s' % (PREFIX, facing, pose)
        for kind, pixels, name, fmt in (('cool', cool, stem, BODY_ANDROID_FORMAT),
                                        ('hot', hot, stem + '_hot', BODY_ANDROID_FORMAT),
                                        ('flash', flash, stem + '_Flash', FLASH_ANDROID_FORMAT)):
            cut = clear_outside(pixels, rect)
            sprite = write_sprite(name, np.ascontiguousarray(cut[y0:y1, x0:x1]), pivot_y, fmt)
            sprite.update(facing=facing, pose=pose, rendered=source, kind=kind)
            sprites[(facing, pose, kind)] = sprite
            written.append(sprite)
        if facing == 'front' and pose == 'idle':
            visual_top = visual_top_of(crisp)
falls = {}
for facing in FACINGS:
    for pose in FALLS:
        if pose in SHARED_FALLS and facing != 'front':
            falls[(facing, pose)] = falls[('front', pose)]
            continue
        r = need(facing, pose, 'hot')
        canvas, pivot = r['canvas'], r['pivot']
        fall_x, fall_row = pivot[0], canvas[1] - pivot[1]
        pixels, _ = processed(r)
        pixels = over_shadow(pixels, contact_shadow(r, pixels.shape[0], pixels.shape[1], fall_x, fall_row, SHADOW_WIDTH))
        rect = tight_rect(pixels, fall_x, fall_row)
        x0, y0, x1, y1 = rect
        name = (REMAINS.get(pose) if facing == 'front' else None) or (
            '%s%s' % (PREFIX, pose) if pose in SHARED_FALLS else '%s%s_%s' % (PREFIX, facing, pose))
        cut = clear_outside(pixels, rect)
        sprite = write_sprite(name, np.ascontiguousarray(cut[y0:y1, x0:x1]), (y1 - fall_row) / (y1 - y0),
                              BODY_ANDROID_FORMAT, (fall_x - x0) / (x1 - x0))
        sprite.update(facing='both' if pose in SHARED_FALLS else facing, pose=pose,
                      kind='remains' if name in REMAINS.values() else 'fall', seconds=r['seconds'])
        falls[(facing, pose)] = sprite
        written.append(sprite)
deaths = [falls[('front', pose)] for pose in REMAINS]
fall_seconds = [need('front', pose, 'hot')['seconds'] for pose in FALLS]

# Stale files from an earlier pack would stay imported; remove them.
keep = {s['file'] for s in written}
for stale in OUT.glob(PREFIX + '*.png'):
    if stale.name not in keep:
        stale.unlink()
        Path(str(stale) + '.meta').unlink(missing_ok=True)

# ---- The set: the Regent's layout, field for field ----
walk_cycle = meta_in['stride']
lines = []
for facing, field in (('front', '_front'), ('rear', '_rear')):
    lines += frames_yaml(field, [(sprites[(facing, p, 'cool')], sprites[(facing, p, 'flash')]) for p in FRAME_ORDER])
lines += ['  _death:'] + ['  - ' + ref(d) for d in deaths]
lines += ['  _visualTop: %s' % visual_top, '  _shadowWidth: %s' % SHADOW_WIDTH, '  _walkCycleDistance: %s' % walk_cycle]
lines += frames_yaml('_windup', [(sprites[(f, 'windup', 'cool')], sprites[(f, 'windup', 'flash')]) for f in FACINGS])
for facing, field in (('front', '_enragedFront'), ('rear', '_enragedRear')):
    lines += frames_yaml(field, [(sprites[(facing, p, 'hot')], sprites[(facing, p, 'flash')]) for p in FRAME_ORDER])
lines += frames_yaml('_enragedWindup', [(sprites[(f, 'windup', 'hot')], sprites[(f, 'windup', 'flash')]) for f in FACINGS])
lines += frames_yaml('_windupWalk', [(sprites[(f, q, 'cool')], sprites[(f, q, 'flash')]) for f in FACINGS
                                     for q in WALK_QUARTERS])
lines += frames_yaml('_enragedWindupWalk', [(sprites[(f, q, 'hot')], sprites[(f, q, 'flash')]) for f in FACINGS
                                            for q in WALK_QUARTERS])
for facing, field in (('front', '_fallFront'), ('rear', '_fallRear')):
    lines += ['  %s:' % field] + ['  - ' + ref(falls[(facing, pose)]) for pose in FALLS]
lines += ['  _fallSeconds:'] + ['  - %s' % s for s in fall_seconds]
lines += ['  _frost: {fileID: 0}', '  _gloam: {fileID: 0}']
art_set = write_set(lines)

mine, regent = memory(written), regent_memory()
report = dict(art_set=art_set, pixels_per_unit=PPU, canvas=CANVAS, pivot=PIVOT, rendered=RENDERED,
              visual_top=visual_top, shadow_width=SHADOW_WIDTH, walk_cycle_distance=walk_cycle, emission=EMISSION,
              rect_alpha=RECT_ALPHA, fall=dict(poses=FALLS, seconds=fall_seconds, total_seconds=round(sum(fall_seconds), 3)),
              memory=dict(myrr=mine, glass_regent=regent,
                          ratio=round(mine['android_astc_bytes'] / regent['android_astc_bytes'], 3)),
              sprites=written)
(DOCS / 'frames.json').write_text(json.dumps(report, indent=1), encoding='utf-8', newline='\n')
print('MYRR_PACKED sprites %d, Android %.2f MiB (all ASTC 4x4 %.2f MiB); the Regent %d, %.2f MiB; ratio %.2f; '
      'visualTop %s walk %s; largest %s; set %s'
      % (mine['textures'], mine['android_astc_bytes'] / 2 ** 20, mine['all_astc_4x4_bytes'] / 2 ** 20,
         regent['textures'], regent['android_astc_bytes'] / 2 ** 20, report['memory']['ratio'], visual_top, walk_cycle,
         max(max(s['size']) for s in written), art_set['guid']))
