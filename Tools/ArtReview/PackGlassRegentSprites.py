"""Packs the Glass Regent's raw Blender frames (BuildBlenderGlassRegent.py) into the game's sprites, its EnemyArtSet and
its glass copies' EnemyArtSet, on the Forge Warden's contract (PackWardenSprites.py).

    python Tools/ArtReview/PackGlassRegentSprites.py [--render DIR]

Per pose: the 4x render is box-filtered down in linear light with premultiplied alpha; the emission pass's three
channels (red the heart, green the crown, the eyes, the rod's crystal and the collar's shards, blue the second phase's
cracks) are coloured and added with their own gains (EMISSION below, per phase: tune a group's value here, no render
needed); then the one-texel dark outline, then each group's soft bloom laid over it in light, so a glow never gets a dark
ring. The first phase's frame, the second's and the white flash share one rect, symmetric about the pivot, each side a
whole number of 4x4 ASTC blocks, exactly as the Warden's. The fall is trimmed tight and carries its contact shadow, as
the Warden's.

The copies (docs/50 section 6: "paler silver at about 70 % opacity, no heart, no crown"): rendered without the crown,
the heart, the eyes and the collar's shards, then paled here (COPY below: toward pale silver by their own value, the
body at 72 % opacity, the outline kept at 90 % so the shape still reads on the slate). Their strike is their idle (a
copy does not swing); their two remains are a burst of glass and the heap it leaves, trimmed tight with a contact
shadow.

Every .meta is the matching Forge Warden sprite's own .meta (the same importer: one sprite, 128 texels a unit,
bilinear, no mipmaps, not readable, ASTC 4x4 on Android) with a fresh GUID and this sprite's pivot. Writes, all
regenerated on every run:

    Assets/_Project/Art/Characters/GlassRegent/CHR_GlassRegent_<facing>_<pose>.png, _hot.png, _Flash.png, the fall's
        CHR_GlassRegent_<facing>_fall-<beat>.png, CHR_GlassRegent_fall-topple.png, CHR_GlassRegent_death_<k>.png
    Assets/_Project/Art/Characters/GlassRegent/GlassRegentArtSet.asset: laid out as ForgeWardenArtSet.asset, _frost empty
        (born on floor 2: docs/50 section 2 rule 6)
    Assets/_Project/Art/Characters/GlassRegent/Copy/CHR_GlassRegentCopy_*.png and GlassRegentCopyArtSet.asset
    ArtDirection/2026-11-12/glass-regent-01/frames.json: every sprite's size and pivot, and the memory figures

Existing GUIDs are kept, so re-running never breaks a reference; a new file gets a fresh uuid4 GUID.
"""
import hashlib
import json
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


RENDER = Path(arg('--render', str(ROOT / 'TestResults/glass-regent-render')))
CHARACTERS = ROOT / 'Assets/_Project/Art/Characters'
OUT = CHARACTERS / 'GlassRegent'
COPY_OUT = OUT / 'Copy'
WARDEN = CHARACTERS / 'ForgeWarden'
DOCS = ROOT / 'ArtDirection/2026-11-12/glass-regent-01'
SET_SCRIPT_GUID = 'ec76f54135d844f881ee6222cf455ad2'    # Scripts/Art/Unity/EnemyArtSet.cs.meta
FRAME_ORDER = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b']
COPY_FRAMES = ['idle', 'step-a', 'step-b', 'idle', 'pass-a', 'pass-b']      # a copy does not swing
FACINGS = ['front', 'rear']
OUTLINE = (0x12, 0x0D, 0x14)
MARGIN = 2
ANDROID_MAX = 512
# The bar hangs from the crown's top in the idle (the spikes within this half-width of the root) plus headroom.
VISUAL_TOP_HALF_WIDTH = 0.3
VISUAL_TOP_HEADROOM = 0.08
# The contact shadow under the robe's hem and the rod's butt; the Warden's is 1.12, the Regent is slimmer.
SHADOW_WIDTH = 1.0
COPY_SHADOW_WIDTH = 0.9
WINDUP_POSES = ['windup', 'windup-step-a', 'windup-pass-a', 'windup-pass-b']
WALK_QUARTERS = ['windup-step-a', 'windup-pass-a', 'windup', 'windup-pass-b']
FALLS = ['fall-struck', 'fall-knee', 'fall-topple', 'fall-down']
SHARED_FALLS = {'fall-topple', 'fall-down'}
REMAINS = {'fall-knee': 'CHR_GlassRegent_death_0', 'fall-down': 'CHR_GlassRegent_death_1'}
SHARDS = {'shards-burst': 'CHR_GlassRegentCopy_death_0', 'shards-heap': 'CHR_GlassRegentCopy_death_1'}

# The emission groups, per variant: (edge colour, core colour, gain, bloom gain, wide bloom gain). The mask (0..1,
# above 1 when the windup flares) runs the colour from edge to core and scales it. The heart is the brightest violet on
# the floor; the crown glows enough to read as lit glass, not as a lamp; the second phase brightens both and opens the
# cracks; the copies keep only a faint rod crystal.
EMISSION = {
    'cool': {'heart': ('#5B3FC8', '#C9B6FF', 1.0, 0.7, 0.28), 'crown': ('#6E55D6', '#D2C4FF', 0.42, 0.35, 0.12)},
    'hot': {'heart': ('#8B6CE8', '#F4EEFF', 1.5, 1.0, 0.45), 'crown': ('#8B6CE8', '#E4DAFF', 0.72, 0.6, 0.2),
            'crack': ('#B9A2FF', '#FFFFFF', 1.25, 0.8, 0.25)},
    'copy': {'crown': ('#8C97AE', '#E2E8F2', 0.25, 0.2, 0.05)},
}
CHANNEL = {'heart': 0, 'crown': 1, 'crack': 2}
BLOOM_SIGMA = (1.2, 3.8)       # the tight halo and the wide one, in texels
HALO_ALPHA = 0.85
# The copies: pale silver by their own value, the body at 72 % opacity, the outline at 90 %.
COPY = dict(tint='#D6DEEA', floor=0.36, slope=0.72, amount=0.68, body_alpha=0.72, outline_alpha=0.9)
SHADOW_TEXEL_COUNT = 32
SHADOW_STEPS = ((0.18, 132), (0.42, 96), (0.72, 58), (1.0, 24))
SHADOW_RGB = (9, 10, 20)
ROOT_SHADOW = {'fall-struck': 1.0, 'fall-knee': 0.8, 'fall-topple': 0.45, 'fall-down': 0.0,
               'shards-burst': 0.55, 'shards-heap': 0.0}
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


def pale(lin, alpha):
    """The copies' recolour on premultiplied linear light: toward pale silver by the body's own value."""
    straight = np.where(alpha > 1e-6, lin / np.maximum(alpha, 1e-6), 0)
    srgb = to_srgb(straight)
    lum = srgb @ np.array([0.299, 0.587, 0.114])
    target = hexrgb(COPY['tint'])[None, None, :] * np.clip(COPY['floor'] + COPY['slope'] * lum, 0, 1)[..., None]
    out = srgb * (1 - COPY['amount']) + target * COPY['amount']
    return to_linear(out) * alpha


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
    if r['variant'] == 'copy':
        lin = pale(lin, alpha) * COPY['body_alpha']
        alpha = alpha * COPY['body_alpha']
        ring = ring * COPY['outline_alpha']
    lin, alpha = lin + ink * ring, alpha + ring
    amount = np.clip(bloom_light.max(axis=-1, keepdims=True), 0, 1)
    lin, alpha = lin + bloom_light, alpha + amount * HALO_ALPHA * (1 - alpha)
    return finish(lin, alpha), body


def tight_rect(image, pivot_x, pivot_row):
    y, x = np.nonzero(image[..., 3])
    h, w = image.shape[:2]
    if x.min() < MARGIN or y.min() < MARGIN or x.max() >= w - MARGIN or y.max() >= h - MARGIN:
        sys.exit('A fall drawing runs off its render canvas: widen FALL_CANVAS_PX in BuildBlenderGlassRegent.py')
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
        y, x = np.nonzero(image[..., 3])
        xs.extend([x.min(), x.max() + 1])
        ys.extend([y.min(), y.max() + 1])
    half = max(pivot_x - min(xs), max(xs) - pivot_x) + MARGIN
    half += half % 2
    y0, y1 = min(min(ys), pivot_row) - MARGIN, max(max(ys), pivot_row) + MARGIN
    y0 -= (-(y1 - y0)) % 4
    h, w = images[0].shape[:2]
    if pivot_x - half < 0 or pivot_x + half > w or y0 < 0 or y1 > h:
        sys.exit('A frame runs off the render canvas: widen CANVAS_PX in BuildBlenderGlassRegent.py')
    return pivot_x - half, y0, pivot_x + half, y1


def existing_guid(meta):
    if meta.exists():
        m = re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(encoding='utf-8'), re.M)
        if m:
            return m.group(1)
    return uuid.uuid4().hex


def warden_meta(name):
    """The matching Forge Warden sprite's .meta: the importer this sprite copies."""
    path = WARDEN / (name + '.png.meta')
    if not path.exists():
        sys.exit('No Warden sprite to copy the import from: ' + path.name)
    return path.read_text(encoding='utf-8')


def write_sprite(folder, name, pixels, pivot_y, source, pivot_x=0.5):
    path = folder / (name + '.png')
    meta = Path(str(path) + '.meta')
    guid = existing_guid(meta)
    Image.fromarray(pixels, 'RGBA').save(path, optimize=True)
    text = warden_meta(source)
    text = re.sub(r'^guid: [0-9a-f]{32}', 'guid: ' + guid, text, count=1, flags=re.M)
    pivot = '{x: %s, y: %.8f}' % ('0.5' if pivot_x == 0.5 else '%.8f' % pivot_x, pivot_y)
    text = re.sub(r'^  spritePivot: \{[^}]*\}', '  spritePivot: ' + pivot, text, count=1, flags=re.M)
    meta.write_text(text, encoding='utf-8', newline='\n')
    h, w = pixels.shape[:2]
    return dict(file=str(path.relative_to(OUT)).replace('\\', '/'), guid=guid, size=[w, h],
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


def write_set(folder, name, lines):
    path = folder / (name + '.asset')
    guid = existing_guid(Path(str(path) + '.meta'))
    head = ['%YAML 1.1', '%TAG !u! tag:unity3d.com,2011:', '--- !u!114 &11400000', 'MonoBehaviour:',
            '  m_ObjectHideFlags: 0', '  m_CorrespondingSourceObject: {fileID: 0}', '  m_PrefabInstance: {fileID: 0}',
            '  m_PrefabAsset: {fileID: 0}', '  m_GameObject: {fileID: 0}', '  m_Enabled: 1', '  m_EditorHideFlags: 0',
            '  m_Script: {fileID: 11500000, guid: %s, type: 3}' % SET_SCRIPT_GUID, '  m_Name: ' + name,
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


meta_in = json.loads((RENDER / 'frames.json').read_text())
SS, CANVAS, PIVOT, GLOW_SCALE = meta_in['supersample'], meta_in['canvas'], meta_in['pivot'], meta_in['glow_scale']
PPU = meta_in['ppu']
assert PPU == 128, 'The Regent is drawn at the painted Grunt\'s 128 texels a unit, as the Warden.'
pivot_x, pivot_row = PIVOT[0], CANVAS[1] - PIVOT[1]
records = {(r['facing'], r['pose'], r['variant']): r for r in meta_in['frames']}


def need(facing, pose, variant):
    r = records.get((facing, pose, variant))
    if r is None:
        sys.exit('Missing render: %s %s %s (render every frame first)' % (facing, pose, variant))
    return r


for folder in (OUT, COPY_OUT):
    folder.mkdir(parents=True, exist_ok=True)
    folder_meta(folder)
DOCS.mkdir(parents=True, exist_ok=True)

# ---- The Regent ----
written = []
sprites = {}
visual_top = None
for facing in FACINGS:
    for pose in FRAME_ORDER + WINDUP_POSES:
        cool, crisp = processed(need(facing, pose, 'cool'))
        hot, _ = processed(need(facing, pose, 'hot'))
        flash = flash_of(crisp)
        x0, y0, x1, y1 = symmetric_rect([cool, hot, flash], pivot_x, pivot_row)
        pivot_y = (y1 - pivot_row) / (y1 - y0)
        stem = 'CHR_GlassRegent_%s_%s' % (facing, pose)
        source = 'CHR_Warden_%s_%s' % (facing, pose)
        for kind, pixels, name, src in (('cool', cool, stem, source), ('hot', hot, stem + '_hot', source + '_hot'),
                                        ('flash', flash, stem + '_Flash', source + '_Flash')):
            sprite = write_sprite(OUT, name, np.ascontiguousarray(pixels[y0:y1, x0:x1]), pivot_y, src)
            sprite.update(facing=facing, pose=pose, kind=kind)
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
        x0, y0, x1, y1 = tight_rect(pixels, fall_x, fall_row)
        name = (REMAINS.get(pose) if facing == 'front' else None) or (
            'CHR_GlassRegent_%s' % pose if pose in SHARED_FALLS else 'CHR_GlassRegent_%s_%s' % (facing, pose))
        source = name.replace('CHR_GlassRegent_', 'CHR_Warden_')
        sprite = write_sprite(OUT, name, np.ascontiguousarray(pixels[y0:y1, x0:x1]), (y1 - fall_row) / (y1 - y0),
                              source, (fall_x - x0) / (x1 - x0))
        sprite.update(facing='both' if pose in SHARED_FALLS else facing, pose=pose,
                      kind='remains' if name in REMAINS.values() else 'fall', seconds=r['seconds'])
        falls[(facing, pose)] = sprite
        written.append(sprite)
deaths = [falls[('front', pose)] for pose in REMAINS]
fall_seconds = [need('front', pose, 'hot')['seconds'] for pose in FALLS]

# ---- The copies ----
copies = {}
copy_written = []
copy_top = None
for facing in FACINGS:
    for pose in ['idle', 'step-a', 'step-b', 'pass-a', 'pass-b'] + WINDUP_POSES:
        body, crisp = processed(need(facing, pose, 'copy'))
        flash = flash_of(crisp)
        x0, y0, x1, y1 = symmetric_rect([body, flash], pivot_x, pivot_row)
        pivot_y = (y1 - pivot_row) / (y1 - y0)
        stem = 'CHR_GlassRegentCopy_%s_%s' % (facing, pose)
        source = 'CHR_Warden_%s_%s' % (facing, pose)
        for kind, pixels, name, src in (('copy', body, stem, source), ('flash', flash, stem + '_Flash', source + '_Flash')):
            sprite = write_sprite(COPY_OUT, name, np.ascontiguousarray(pixels[y0:y1, x0:x1]), pivot_y, src)
            sprite.update(facing=facing, pose=pose, kind=kind)
            copies[(facing, pose, kind)] = sprite
            copy_written.append(sprite)
        if facing == 'front' and pose == 'idle':
            copy_top = visual_top_of(crisp)
copy_deaths = []
for pose, name in SHARDS.items():
    r = need('front', pose, 'copy')
    canvas, pivot = r['canvas'], r['pivot']
    fall_x, fall_row = pivot[0], canvas[1] - pivot[1]
    pixels, _ = processed(r)
    pixels = over_shadow(pixels, contact_shadow(r, pixels.shape[0], pixels.shape[1], fall_x, fall_row,
                                                COPY_SHADOW_WIDTH))
    x0, y0, x1, y1 = tight_rect(pixels, fall_x, fall_row)
    source = 'CHR_Warden_death_%s' % name[-1]
    sprite = write_sprite(COPY_OUT, name, np.ascontiguousarray(pixels[y0:y1, x0:x1]), (y1 - fall_row) / (y1 - y0),
                          source, (fall_x - x0) / (x1 - x0))
    sprite.update(facing='front', pose=pose, kind='remains')
    copy_deaths.append(sprite)
    copy_written.append(sprite)

# Stale files from an earlier pack would stay imported; remove them.
for folder, prefix, keep in ((OUT, 'CHR_GlassRegent_*.png', {Path(s['file']).name for s in written}),
                             (COPY_OUT, 'CHR_GlassRegentCopy_*.png', {Path(s['file']).name for s in copy_written})):
    for stale in folder.glob(prefix):
        if stale.name not in keep:
            stale.unlink()
            Path(str(stale) + '.meta').unlink(missing_ok=True)

# ---- The sets: the Warden's layout, field for field ----
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
lines += ['  _frost: {fileID: 0}']
regent_set = write_set(OUT, 'GlassRegentArtSet', lines)

lines = []
for facing, field in (('front', '_front'), ('rear', '_rear')):
    lines += frames_yaml(field, [(copies[(facing, p, 'copy')], copies[(facing, p, 'flash')]) for p in COPY_FRAMES])
lines += ['  _death:'] + ['  - ' + ref(d) for d in copy_deaths]
lines += ['  _visualTop: %s' % copy_top, '  _shadowWidth: %s' % COPY_SHADOW_WIDTH,
          '  _walkCycleDistance: %s' % walk_cycle]
lines += frames_yaml('_windup', [(copies[(f, 'windup', 'copy')], copies[(f, 'windup', 'flash')]) for f in FACINGS])
lines += frames_yaml('_windupWalk', [(copies[(f, q, 'copy')], copies[(f, q, 'flash')]) for f in FACINGS
                                     for q in WALK_QUARTERS])
lines += ['  _frost: {fileID: 0}']
copy_set = write_set(COPY_OUT, 'GlassRegentCopyArtSet', lines)


def memory(items):
    texels = sum(s['size'][0] * s['size'][1] for s in items)
    return dict(textures=len(items), astc_4x4_bytes=texels, rgba32_bytes=texels * 4)


report = dict(art_set=regent_set, copy_set=copy_set, pixels_per_unit=PPU, canvas=CANVAS, pivot=PIVOT,
              visual_top=visual_top, copy_visual_top=copy_top, shadow_width=SHADOW_WIDTH,
              copy_shadow_width=COPY_SHADOW_WIDTH, walk_cycle_distance=walk_cycle, emission=EMISSION, copy_recipe=COPY,
              fall=dict(poses=FALLS, seconds=fall_seconds, total_seconds=round(sum(fall_seconds), 3)),
              memory=dict(regent=memory(written), copies=memory(copy_written)),
              sprites=written, copy_sprites=copy_written)
(DOCS / 'frames.json').write_text(json.dumps(report, indent=1), encoding='utf-8', newline='\n')
m = report['memory']
print('REGENT_PACKED sprites %d ASTC4x4 %.2f MB; copies %d ASTC4x4 %.2f MB; visualTop %s copyTop %s walk %s; set %s copy set %s'
      % (m['regent']['textures'], m['regent']['astc_4x4_bytes'] / 2 ** 20, m['copies']['textures'],
         m['copies']['astc_4x4_bytes'] / 2 ** 20, visual_top, copy_top, walk_cycle, regent_set['guid'], copy_set['guid']))
