"""Packs the Forge Warden's raw Blender frames (BuildBlenderWarden.py) into the game's sprites and its EnemyArtSet.

    python Tools/ArtReview/PackWardenSprites.py [--render DIR]

Per pose: the 4x render is box-filtered down in linear light with premultiplied alpha (area-exact anti-aliasing; the
floor pivot stays on a texel corner), given a one-texel dark outline, then the furnace's bloom from the glow pass is
laid over it - a soft ember halo round the visor, the seams and the cracks, stronger in the second phase - added in
light after the outline, so a glow never gets a dark ring. The first phase's frame, the second's and the white flash
share one rect: trimmed to their texels plus a margin, symmetric about the pivot (so a body's bounds centre is its
root, where the hit sparks and the burn flames stand), each side a whole number of 4x4 ASTC blocks. Writes, all
regenerated on every run:

    Assets/_Project/Art/Characters/ForgeWarden/CHR_Warden_<facing>_<pose>.png, _hot.png and _Flash.png (+ .meta: one
        sprite each, the pivot at the feet, 128 texels a unit, bilinear, no mipmaps, not readable, ASTC 4x4 on Android -
        the painted Grunt's import)
    Assets/_Project/Art/Characters/ForgeWarden/CHR_Warden_death_<k>.png (+ .meta)
    Assets/_Project/Art/Characters/ForgeWarden/ForgeWardenArtSet.asset (+ .meta): the EnemyArtSet
    ArtDirection/2026-10-27/warden-blender-01/frames.json: every sprite's size and pivot, and the memory figures

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


RENDER = Path(arg('--render', str(ROOT / 'TestResults/warden-render')))
OUT = ROOT / 'Assets/_Project/Art/Characters/ForgeWarden'
DOCS = ROOT / 'ArtDirection/2026-10-27/warden-blender-01'
SET_SCRIPT_GUID = 'ec76f54135d844f881ee6222cf455ad2'    # Scripts/Art/Unity/EnemyArtSet.cs.meta
# EnemyArtSet's frame order: idle, contact A, contact B, strike, then the appended passing A and B (HasPassingFrames).
FRAME_ORDER = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b']
FACINGS = ['front', 'rear']
OUTLINE = (0x12, 0x0D, 0x14)
MARGIN = 2          # transparent texels round each frame, so bilinear filtering never reads a neighbour
# The furnace's bloom: a tight halo and a wide one, as gains on the glow pass's emission, per phase.
BLOOM = {'cool': (1.2, 0.9, 3.6, 0.34), 'hot': (1.2, 1.25, 4.2, 0.55)}   # sigma, gain, wide sigma, wide gain
HALO_ALPHA = 0.85   # how much of the halo outside the figure covers the floor (the rest adds to it)
ANDROID_MAX = 512   # every frame fits under this, so Android never downscales one
# The bar and the burn flames hang from this (EnemyArtSet.VisualTop): the body's top in the idle, without the hammer,
# and a little headroom, so the bar floats clear of the pauldrons as the Grunt's does above its head.
VISUAL_TOP_HALF_WIDTH = 0.5
VISUAL_TOP_HEADROOM = 0.08
# The contact shadow under the feet and the hammer's head (EnemyArtSet.ShadowWidth); the painted Grunt's is 0.78.
SHADOW_WIDTH = 1.12


def to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def blur(image, sigma):
    """A separable gaussian on an (h, w, c) float image, zero beyond its edges."""
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


def glow_of(path, ss, scale):
    """The glow pass's emission at the game's resolution, in linear light (the render wrote it at scale)."""
    a = np.asarray(Image.open(path).convert('RGB')).astype(np.float64) / 255.0
    lin = to_linear(a) / scale
    h, w = lin.shape[0] // ss, lin.shape[1] // ss
    return lin.reshape(h, ss, w, ss, 3).mean(axis=(1, 3))


def outlined(lin, alpha):
    """One texel of dark outline round the silhouette, soft at the corners, under the figure."""
    pad = np.pad(alpha[..., 0], 1)
    grown = np.maximum.reduce([pad[1:-1, 1:-1], pad[:-2, 1:-1], pad[2:, 1:-1], pad[1:-1, :-2], pad[1:-1, 2:],
                               0.7 * pad[:-2, :-2], 0.7 * pad[2:, 2:], 0.7 * pad[:-2, 2:], 0.7 * pad[2:, :-2]])[..., None]
    ink = to_linear(np.array(OUTLINE) / 255.0)
    return lin + ink * grown * (1 - alpha), alpha + grown * (1 - alpha)


def bloomed(lin, alpha, glow, phase):
    sigma, gain, wide_sigma, wide_gain = BLOOM[phase]
    light = blur(glow, sigma) * gain + blur(glow, wide_sigma) * wide_gain
    amount = np.clip(light.max(axis=-1, keepdims=True), 0, 1)
    return lin + light, alpha + amount * HALO_ALPHA * (1 - alpha)


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


def symmetric_rect(images, pivot_x, pivot_row):
    """(x0, y0, x1, y1), top-left origin: every image's texels plus the margin, symmetric about the pivot column,
    holding the pivot row, each side a multiple of 4."""
    xs, ys = [], []
    for image in images:
        y, x = np.nonzero(image[..., 3])
        xs.extend([x.min(), x.max() + 1])
        ys.extend([y.min(), y.max() + 1])
    half = max(pivot_x - min(xs), max(xs) - pivot_x) + MARGIN
    half += half % 2                                   # an even half: the width is a multiple of 4
    y0, y1 = min(min(ys), pivot_row) - MARGIN, max(max(ys), pivot_row) + MARGIN
    y0 -= (-(y1 - y0)) % 4                             # extend upward to whole blocks
    h, w = images[0].shape[:2]
    if pivot_x - half < 0 or pivot_x + half > w or y0 < 0 or y1 > h:
        sys.exit('A frame runs off the render canvas: widen CANVAS_PX in BuildBlenderWarden.py')
    return pivot_x - half, y0, pivot_x + half, y1


def existing_guid(meta):
    if meta.exists():
        m = re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(encoding='utf-8'), re.M)
        if m:
            return m.group(1)
    return uuid.uuid4().hex


def texture_meta(guid, pivot_y):
    platforms = []
    for target, compression, fmt, overridden, size in [('DefaultTexturePlatform', 0, -1, 0, 2048),
                                                        ('Standalone', 1, -1, 0, 2048), ('Android', 1, 48, 1, ANDROID_MAX)]:
        platforms.append(f"""  - serializedVersion: 4
    buildTarget: {target}
    maxTextureSize: {size}
    resizeAlgorithm: 0
    textureFormat: {fmt}
    textureCompression: {compression}
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: {overridden}
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0""")
    return f"""fileFormatVersion: 2
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
  spritePivot: {{x: 0.5, y: {pivot_y:.8f}}}
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
{chr(10).join(platforms)}
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


def write_sprite(name, pixels, pivot_y):
    path = OUT / (name + '.png')
    meta = Path(str(path) + '.meta')
    guid = existing_guid(meta)
    Image.fromarray(pixels, 'RGBA').save(path, optimize=True)
    meta.write_text(texture_meta(guid, pivot_y), encoding='utf-8', newline='\n')
    h, w = pixels.shape[:2]
    return dict(file=path.name, guid=guid, size=[w, h], pivot=[0.5, round(pivot_y, 6)],
                sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def ref(sprite):
    return '{fileID: 21300000, guid: %s, type: 3}' % sprite['guid']


def frames_yaml(field, frames):
    lines = ['  %s:' % field]
    for body, flash in frames:
        lines += ['  - Body: ' + ref(body), '    Flash: ' + ref(flash)]
    return lines


meta_in = json.loads((RENDER / 'frames.json').read_text())
SS, CANVAS, PIVOT, GLOW_SCALE = meta_in['supersample'], meta_in['canvas'], meta_in['pivot'], meta_in['glow_scale']
PPU = meta_in['ppu']
assert PPU == 128, 'The Warden is drawn at the painted Grunt\'s 128 texels a unit.'
pivot_x, pivot_row = PIVOT[0], CANVAS[1] - PIVOT[1]
records = {(r['facing'], r['pose'], r['variant']): r for r in meta_in['frames']}


def need(facing, pose, variant):
    r = records.get((facing, pose, variant))
    if r is None:
        sys.exit('Missing render: %s %s %s (render every frame first)' % (facing, pose, variant))
    return r


def processed(r):
    """(first-pass pixels with bloom, the crisp alpha under the bloom) for one rendered frame."""
    lin, alpha = downsample(RENDER / r['file'], SS)
    lin, alpha = outlined(lin, alpha)
    crisp = alpha.copy()
    lin, alpha = bloomed(lin, alpha, glow_of(RENDER / r['glow'], SS, GLOW_SCALE), r['variant'])
    return finish(lin, alpha), crisp


OUT.mkdir(parents=True, exist_ok=True)
DOCS.mkdir(parents=True, exist_ok=True)
folder_meta = ROOT / 'Assets/_Project/Art/Characters/ForgeWarden.meta'
if not folder_meta.exists():
    folder_meta.write_text('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                           '  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex,
                           encoding='utf-8', newline='\n')

written = []
sprites = {}        # (facing, pose, 'cool'|'hot'|'flash') -> sprite record
visual_top = None
for facing in FACINGS:
    for pose in FRAME_ORDER + ['windup']:
        cool, crisp = processed(need(facing, pose, 'cool'))
        hot, _ = processed(need(facing, pose, 'hot'))
        flash = flash_of(crisp)
        x0, y0, x1, y1 = symmetric_rect([cool, hot, flash], pivot_x, pivot_row)
        pivot_y = (y1 - pivot_row) / (y1 - y0)
        stem = 'CHR_Warden_%s_%s' % (facing, pose)
        for kind, pixels, name in (('cool', cool, stem), ('hot', hot, stem + '_hot'), ('flash', flash, stem + '_Flash')):
            sprite = write_sprite(name, np.ascontiguousarray(pixels[y0:y1, x0:x1]), pivot_y)
            sprite.update(facing=facing, pose=pose, kind=kind)
            sprites[(facing, pose, kind)] = sprite
            written.append(sprite)
        if facing == 'front' and pose == 'idle':
            # The body's top: the highest solid texel within the body's width, the bar's anchor.
            body = crisp[:, int(pivot_x - VISUAL_TOP_HALF_WIDTH * PPU):int(pivot_x + VISUAL_TOP_HALF_WIDTH * PPU), 0]
            rows = np.nonzero((body > 0.5).any(axis=1))[0]
            visual_top = round((pivot_row - rows.min()) / PPU + VISUAL_TOP_HEADROOM, 3)
deaths = []
for k in range(2):
    pixels, _ = processed(need('front', 'death-%d' % k, 'cool'))
    x0, y0, x1, y1 = symmetric_rect([pixels], pivot_x, pivot_row)
    sprite = write_sprite('CHR_Warden_death_%d' % k, np.ascontiguousarray(pixels[y0:y1, x0:x1]), (y1 - pivot_row) / (y1 - y0))
    sprite.update(facing='front', pose='death-%d' % k, kind='death')
    deaths.append(sprite)
    written.append(sprite)

# Stale files from an earlier pack (a pose renamed or dropped) would stay imported; remove them.
keep = {s['file'] for s in written}
for stale in OUT.glob('CHR_Warden_*.png'):
    if stale.name not in keep:
        stale.unlink()
        Path(str(stale) + '.meta').unlink(missing_ok=True)

lines = ['%YAML 1.1', '%TAG !u! tag:unity3d.com,2011:', '--- !u!114 &11400000', 'MonoBehaviour:',
         '  m_ObjectHideFlags: 0', '  m_CorrespondingSourceObject: {fileID: 0}', '  m_PrefabInstance: {fileID: 0}',
         '  m_PrefabAsset: {fileID: 0}', '  m_GameObject: {fileID: 0}', '  m_Enabled: 1', '  m_EditorHideFlags: 0',
         '  m_Script: {fileID: 11500000, guid: %s, type: 3}' % SET_SCRIPT_GUID, '  m_Name: ForgeWardenArtSet',
         '  m_EditorClassIdentifier: ']
for facing, field in (('front', '_front'), ('rear', '_rear')):
    lines += frames_yaml(field, [(sprites[(facing, p, 'cool')], sprites[(facing, p, 'flash')]) for p in FRAME_ORDER])
lines += ['  _death:'] + ['  - ' + ref(d) for d in deaths]
walk_cycle = meta_in['stride']
lines += ['  _visualTop: %s' % visual_top, '  _shadowWidth: %s' % SHADOW_WIDTH, '  _walkCycleDistance: %s' % walk_cycle]
lines += frames_yaml('_windup', [(sprites[(f, 'windup', 'cool')], sprites[(f, 'windup', 'flash')]) for f in FACINGS])
# The second phase's bodies wear the first phase's flashes: the silhouette is the same, so no texture is spent twice.
for facing, field in (('front', '_enragedFront'), ('rear', '_enragedRear')):
    lines += frames_yaml(field, [(sprites[(facing, p, 'hot')], sprites[(facing, p, 'flash')]) for p in FRAME_ORDER])
lines += frames_yaml('_enragedWindup', [(sprites[(f, 'windup', 'hot')], sprites[(f, 'windup', 'flash')]) for f in FACINGS])
set_path = OUT / 'ForgeWardenArtSet.asset'
set_guid = existing_guid(Path(str(set_path) + '.meta'))
set_path.write_text('\n'.join(lines) + '\n', encoding='utf-8', newline='\n')
Path(str(set_path) + '.meta').write_text('fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n'
                                         '  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n'
                                         '  assetBundleVariant: \n' % set_guid, encoding='utf-8', newline='\n')

texels = sum(s['size'][0] * s['size'][1] for s in written)
report = dict(art_set=dict(file=set_path.name, guid=set_guid), pixels_per_unit=PPU, canvas=CANVAS, pivot=PIVOT,
              visual_top=visual_top, shadow_width=SHADOW_WIDTH, walk_cycle_distance=walk_cycle,
              memory=dict(textures=len(written), astc_4x4_bytes=texels, rgba32_bytes=texels * 4),
              sprites=written)
(DOCS / 'frames.json').write_text(json.dumps(report, indent=1), encoding='utf-8', newline='\n')
print('WARDEN_PACKED sprites %d ASTC4x4 %.2f MB (%d texels) RGBA32 %.2f MB visualTop %s walk %s set %s'
      % (len(written), texels / 2 ** 20, texels, texels * 4 / 2 ** 20, visual_top, walk_cycle, set_guid))
