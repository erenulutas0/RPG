"""Packs Ilya Kest's raw Blender frames (BuildBlenderArcanist.py) into the game's sprite atlases.

    python Tools/ArtReview/PackArcanistSprites.py [--render DIR]

Per frame: the 4x render is box-filtered down in linear light with premultiplied alpha (area-exact anti-aliasing; the
floor pivot stays on a pixel corner), given a one-pixel dark outline and the crucible's soft ember halo (at the anchor
and flare frames.json records), and trimmed to its pixels plus a transparent margin (always keeping the pivot inside).
Every facing is packed, the west three included (they are rendered, not mirrored). The packing tries a few page shapes
(2048x1024, 2048x2048, 1024x1024), MaxRects rules and orders and keeps the layout with the fewest atlas texels; its
last page is then repacked into the smallest bin that holds it. Pages are cropped to whole 4x4 ASTC blocks. Writes, all
regenerated on every run (a rerun on the same renders is byte-identical):

    Assets/_Project/Art/Characters/CinderArcanist/CHR_CinderArcanist_<n>.png (+ .meta: sprite rects and pivots,
        bilinear, no mipmaps, ASTC 4x4 on Android - modelled on the Vanguard's character textures)
    Assets/_Project/Art/Characters/CinderArcanist/CinderArcanistArtSet.asset (+ .meta): the RenderedCharacterArtSet
    ArtDirection/2026-10-27/arcanist-blender-01/frames.json: every frame's atlas rect, pivot and crucible anchor

Existing GUIDs are kept, so re-running never breaks a reference; a page beyond the existing ones takes a GUID derived
from its name. Sprite IDs are derived from the frame names. A sprite referenced from outside the set (the hero's portrait,
Hero_CinderArcanist._portrait, names idle_south_0 by its texture's GUID) is kept on the texture it was on: the pages are
ordered so that every such frame lands on the page whose GUID the reference holds.
"""
import hashlib
import json
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
ARGS = sys.argv[1:]


def arg(name, default):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


RENDER = Path(arg('--render', str(ROOT / 'TestResults/arcanist-render')))
# Page shapes the packing may use (width, height); the largest an atlas gets is the platforms' 2048 import limit.
PAGE_SHAPES = [(2048, 1024), (2048, 2048), (1024, 1024)]
OUT = ROOT / 'Assets/_Project/Art/Characters/CinderArcanist'
DOCS = ROOT / 'ArtDirection/2026-10-27/arcanist-blender-01'
SET_SCRIPT_GUID = '9a023454e5b64792879bd5578ca2bdac'   # Scripts/Art/Unity/RenderedCharacterArtSet.cs.meta
FOLDER_GUID = '45e42a0cc5514f69a7aec6efd3515b68'
MOTIONS = ['idle', 'walk', 'walk-strike', 'strike', 'cast', 'hit', 'death']        # RenderedMotion order
FACINGS = ['south', 'south-east', 'east', 'north-east', 'north', 'north-west', 'west', 'south-west']  # RenderedFacing
OUTLINE = (0x16, 0x0F, 0x17)
# The crucible's halo: ember orange, centred a little above the cup's mouth (on the flame), about three texels wide at
# a resting flare and wider as it flares, strongest a little over half over the figure.
GLOW = (0xFF, 0x9A, 0x45)
GLOW_LIFT = 0.035
GLOW_SIGMA = 2.8
GLOW_GAIN = 0.26
GLOW_PEAK = 0.6
MARGIN = 1      # transparent texels kept round each frame, so bilinear filtering never reads a neighbour
PADDING = 0     # more between packed rects (none needed: each rect's own margin is already transparent)


def to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def process(path, ss, crucible=None, flare=0.0):
    """The final sprite pixels for one raw frame: straight-alpha sRGB uint8, the canvas at the game's resolution."""
    a = np.asarray(Image.open(path).convert('RGBA')).astype(np.float64) / 255.0
    alpha = a[..., 3:4]
    lin = to_linear(a[..., :3]) * alpha
    h, w = alpha.shape[0] // ss, alpha.shape[1] // ss
    lin = lin.reshape(h, ss, w, ss, 3).mean(axis=(1, 3))
    alpha = alpha.reshape(h, ss, w, ss, 1).mean(axis=(1, 3))
    # One texel of dark outline round the silhouette, soft at the corners, under the figure.
    pad = np.pad(alpha[..., 0], 1)
    grown = np.maximum.reduce([pad[1:-1, 1:-1], pad[:-2, 1:-1], pad[2:, 1:-1], pad[1:-1, :-2], pad[1:-1, 2:],
                               0.7 * pad[:-2, :-2], 0.7 * pad[2:, 2:], 0.7 * pad[:-2, 2:], 0.7 * pad[2:, :-2]])[..., None]
    ink = to_linear(np.array(OUTLINE) / 255.0)
    lin = lin + ink * grown * (1 - alpha)
    alpha = alpha + grown * (1 - alpha)
    # The crucible glows: a soft ember halo round the staff's head (frames.json records where it projects and how hard
    # it flares), added in light after the outline so it never gets a dark ring. Over the figure it brightens; beyond
    # it the halo is a translucent ember the floor shows through.
    if crucible is not None and flare > 0.05:
        cx = pivot_x + crucible[0] * PPU
        cy = pivot_top - (crucible[1] + GLOW_LIFT) * PPU
        yy, xx = np.mgrid[0:h, 0:w] + 0.5
        sigma = GLOW_SIGMA * (1 + 0.5 * max(0.0, flare - 1))
        amount = (min(GLOW_PEAK, GLOW_GAIN * flare) * np.exp(-((xx - cx) ** 2 + (yy - cy) ** 2) / (2 * sigma ** 2)))[..., None]
        lin = lin + to_linear(np.array(GLOW) / 255.0) * amount
        alpha = alpha + amount * 0.8 * (1 - alpha)
    straight = np.where(alpha > 1e-6, lin / np.maximum(alpha, 1e-6), 0)
    rgb = to_srgb(straight)
    out = np.concatenate([rgb, alpha], axis=-1)
    out = (out * 255 + 0.5).astype(np.uint8)
    out[out[..., 3] < 2] = 0
    return out


def trim(pixels, pivot_x, pivot_top):
    """(x0, y0, x1, y1) in canvas pixels from the top-left: the frame's pixels and margin, always holding the pivot."""
    ys, xs = np.nonzero(pixels[..., 3])
    x0, x1 = min(xs.min(), pivot_x) - MARGIN, max(xs.max() + 1, pivot_x) + MARGIN
    y0, y1 = min(ys.min(), pivot_top) - MARGIN, max(ys.max() + 1, pivot_top) + MARGIN
    h, w = pixels.shape[:2]
    return max(0, x0), max(0, y0), min(w, x1), min(h, y1)


class MaxRects:
    """A MaxRects bin (Jylanki 2010), top-left origin. Rules: best short side fit ('bssf'), best area fit ('baf'),
    top-left first ('tl', which keeps a page's used rows few so it crops short) and contact point ('cp': the most edge
    shared with the bin's sides and the rects already placed, which packs these similar figures tightest)."""

    def __init__(self, w, h, rule='bssf'):
        self.w, self.h, self.rule = w, h, rule
        self.free = [(0, 0, w, h)]
        self.used = []

    def _score(self, fx, fy, fw, fh, w, h):
        if self.rule == 'tl':
            return (fy + h, fx)
        if self.rule == 'baf':
            return (fw * fh - w * h, min(fw - w, fh - h), fy, fx)
        if self.rule == 'cp':
            return (-self._contact(fx, fy, w, h), fy, fx)
        return (min(fw - w, fh - h), max(fw - w, fh - h), fy, fx)

    def _contact(self, x, y, w, h):
        edge = (h if x == 0 or x + w == self.w else 0) + (w if y == 0 or y + h == self.h else 0)
        for ux, uy, uw, uh in self.used:
            if ux == x + w or ux + uw == x:
                edge += max(0, min(uy + uh, y + h) - max(uy, y))
            if uy == y + h or uy + uh == y:
                edge += max(0, min(ux + uw, x + w) - max(ux, x))
        return edge

    def insert(self, w, h):
        best = None
        for fx, fy, fw, fh in self.free:
            if w <= fw and h <= fh:
                score = self._score(fx, fy, fw, fh, w, h)
                if best is None or score < best[0]:
                    best = (score, (fx, fy))
        if best is None:
            return None
        x, y = best[1]
        placed = (x, y, w, h)
        self.used.append(placed)
        free = set()
        for f in self.free:
            free.update(self._split(f, placed))
        # Largest first, in a fixed order, dropping every free rect another one holds (a duplicate is kept once).
        kept = []
        for f in sorted(free, key=lambda r: (-r[2] * r[3], r)):
            if not any(self._contains(g, f) for g in kept):
                kept.append(f)
        self.free = kept
        return x, y

    @staticmethod
    def _split(f, r):
        fx, fy, fw, fh = f
        rx, ry, rw, rh = r
        if rx >= fx + fw or rx + rw <= fx or ry >= fy + fh or ry + rh <= fy:
            return [f]
        out = []
        if rx > fx:
            out.append((fx, fy, rx - fx, fh))
        if rx + rw < fx + fw:
            out.append((rx + rw, fy, fx + fw - rx - rw, fh))
        if ry > fy:
            out.append((fx, fy, fw, ry - fy))
        if ry + rh < fy + fh:
            out.append((fx, ry + rh, fw, fy + fh - ry - rh))
        return out

    @staticmethod
    def _contains(a, b):
        return b[0] >= a[0] and b[1] >= a[1] and b[0] + b[2] <= a[0] + a[2] and b[1] + b[3] <= a[1] + a[3]


def existing_guid(meta):
    """The GUID an asset already has or, for a new one, a GUID derived from its file name, so a fresh pack names it
    the same way every time."""
    if meta.exists():
        m = re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(encoding='utf-8'), re.M)
        if m:
            return m.group(1)
    return hashlib.md5(('Cryptforge guid ' + meta.name[:-len('.meta')]).encode()).hexdigest()


def internal_id(name):
    value = int.from_bytes(hashlib.sha256(name.encode()).digest()[:8], 'little', signed=True)
    return value if value not in (0, 21300000) else value + 1


def sprite_id(name):
    return hashlib.md5(('Cryptforge ' + name).encode()).hexdigest()


def texture_meta(guid, size, sprites):
    names = '\n'.join('  - first:\n      213: %d\n    second: %s' % (s['id'], s['name']) for s in sprites)
    entries = []
    for s in sprites:
        x, y, w, h = s['unity_rect']
        entries.append(f"""    - serializedVersion: 2
      name: {s['name']}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: {w}
        height: {h}
      alignment: 9
      pivot: {{x: {s['pivot'][0]:.8f}, y: {s['pivot'][1]:.8f}}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sprite_id(s['name'])}
      internalID: {s['id']}
      vertices: []
      indices:
      edges: []
      weights: []""")
    table = '\n'.join('      %s: %d' % (s['name'], s['id']) for s in sprites)
    # The Android override's size limit is the power of two that holds the page (the importer offers only those).
    platform = []
    for target, compression, fmt, overridden in [('DefaultTexturePlatform', 0, -1, 0), ('Standalone', 1, -1, 0),
                                                 ('Android', 1, 48, 1)]:
        platform.append(f"""  - serializedVersion: 4
    buildTarget: {target}
    maxTextureSize: {1 << (max(size) - 1).bit_length() if target == 'Android' else 2048}
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
  internalIDToNameTable:
{names}
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
  spriteMode: 2
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 9
  spritePivot: {{x: 0.5, y: 0}}
  spritePixelsToUnits: {PPU}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
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
{chr(10).join(platform)}
  spriteSheet:
    serializedVersion: 2
    sprites:
{chr(10).join(entries)}
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
{table}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def art_set(clips, timings, stride):
    lines = ['%YAML 1.1', '%TAG !u! tag:unity3d.com,2011:', '--- !u!114 &11400000', 'MonoBehaviour:',
             '  m_ObjectHideFlags: 0', '  m_CorrespondingSourceObject: {fileID: 0}', '  m_PrefabInstance: {fileID: 0}',
             '  m_PrefabAsset: {fileID: 0}', '  m_GameObject: {fileID: 0}', '  m_Enabled: 1', '  m_EditorHideFlags: 0',
             '  m_Script: {fileID: 11500000, guid: %s, type: 3}' % SET_SCRIPT_GUID, '  m_Name: CinderArcanistArtSet',
             '  m_EditorClassIdentifier: ', '  _strideLength: %s' % repr(float(stride)), '  _timings:']
    for motion, fps, loops in timings:
        lines += ['  - Motion: %d' % motion, '    FramesPerSecond: %s' % repr(float(fps)), '    Loops: %d' % int(loops)]
    lines.append('  _clips:')
    for motion, facing, refs in clips:
        lines += ['  - Motion: %d' % motion, '    Facing: %d' % facing, '    Frames:']
        lines += ['    - {fileID: %d, guid: %s, type: 3}' % ref for ref in refs]
    return '\n'.join(lines) + '\n'


meta_in = json.loads((RENDER / 'frames.json').read_text())
PPU, SS, CANVAS = meta_in['ppu'], meta_in['supersample'], meta_in['canvas']
size = (int(round(CANVAS * PPU)), int(round(meta_in.get('canvas_height', CANVAS) * PPU)))   # width, height
pivot_x, pivot_top = meta_in['pivot'][0], size[1] - meta_in['pivot'][1]
records = {(r['facing'], r['clip'], r['frame']): r for r in meta_in['frames']}
frames = []
for clip in MOTIONS:
    count = meta_in['clips'][clip]['frames']
    for facing in FACINGS:
        for k in range(count):
            r = records.get((facing, clip, k))
            if r is None:
                sys.exit('Missing render: %s %s %d (render every frame first)' % (facing, clip, k))
            pixels = process(RENDER / r['file'], SS, r.get('crucible'), r.get('flare', 0.0))
            x0, y0, x1, y1 = trim(pixels, pivot_x, pivot_top)
            frames.append(dict(name='%s_%s_%d' % (clip, facing, k), clip=clip, facing=facing, frame=k,
                               pixels=pixels[y0:y1, x0:x1], crop=(x0, y0, x1, y1), crucible=r.get('crucible'),
                               flare=r.get('flare'), source=r['file']))

# Pack onto as many pages of a shape as it takes; each page is cropped to the rows and columns it uses, rounded up to
# whole 4x4 ASTC blocks (the importer keeps non-power-of-two sizes: nPOTScale 0).
def pack(order, shape, rule):
    pages, placed = [], []
    for f in order:
        h, w = f['pixels'].shape[:2]
        for index, bin_ in enumerate(pages):
            at = bin_.insert(w + PADDING, h + PADDING)
            if at is not None:
                break
        else:
            pages.append(MaxRects(shape[0], shape[1], rule))
            index, at = len(pages) - 1, pages[-1].insert(w + PADDING, h + PADDING)
            if at is None:
                return None
        placed.append((f, index, at))
    sizes = []
    for index in range(len(pages)):
        used_w = max(at[0] + f['pixels'].shape[1] for f, i, at in placed if i == index)
        used_h = max(at[1] + f['pixels'].shape[0] for f, i, at in placed if i == index)
        sizes.append(((used_w + 3) // 4 * 4, (used_h + 3) // 4 * 4))
    return placed, sizes


def texels(result):
    return sum(w * h for w, h in result[1])


ORDERS = [lambda f: (-f['pixels'].shape[0], -f['pixels'].shape[1]),                  # tallest first
          lambda f: (-f['pixels'].shape[0], f['pixels'].shape[1]),                   # tallest, the narrow first
          lambda f: (-f['pixels'].shape[0] * f['pixels'].shape[1],),                 # largest first
          lambda f: (-max(f['pixels'].shape[:2]), -min(f['pixels'].shape[:2])),      # longest side first
          lambda f: (-f['pixels'].shape[1], -f['pixels'].shape[0])]                  # widest first
RULES = ('cp', 'bssf', 'baf', 'tl')


def ordered(group, key):
    # Ties fall by name, so an order is the same on every run.
    return sorted(group, key=lambda f: (key(f), f['name']))


candidates = []
for s, shape in enumerate(PAGE_SHAPES):
    for r, rule in enumerate(RULES):
        for k, key in enumerate(ORDERS):
            result = pack(ordered(frames, key), shape, rule)
            if result is not None:
                candidates.append(((texels(result), len(result[1]), s, r, k), result))
candidates.sort(key=lambda c: c[0])


def repack_last(result):
    """The layout with its last page repacked into the smallest bin of any width that holds it on one page."""
    placed, sizes = result
    last = len(sizes) - 1
    rest = [f for f, i, at in placed if i == last]
    best = None
    for width in range(128, max(s[0] for s in PAGE_SHAPES) + 1, 64):
        for rule in RULES:
            for key in ORDERS[:3]:
                one = pack(ordered(rest, key), (width, max(s[1] for s in PAGE_SHAPES)), rule)
                if one is not None and len(one[1]) == 1 and (best is None or texels(one) < texels(best)):
                    best = one
    if best is None or texels(best) >= sizes[last][0] * sizes[last][1]:
        return result
    return ([(f, i, at) for f, i, at in placed if i != last] + [(f, last, at) for f, _, at in best[0]],
            sizes[:last] + best[1])


# The few best layouts get their last page repacked; the fewest texels wins, then the fewest pages.
best = min((repack_last(result) for _, result in candidates[:4]), key=lambda r: (texels(r), len(r[1])))
for f, index, at in best[0]:
    f['page'], f['at'] = index, at
shapes = best[1]


def page_guid(index):
    return existing_guid(OUT / ('CHR_CinderArcanist_%d.png.meta' % index))


def references_from_outside(set_path):
    """{frame name: (texture GUID, referencing asset)} for every frame another asset references by sprite ID and
    texture GUID: the art set, rewritten below, aside."""
    ids = {internal_id(f['name']): f['name'] for f in frames}
    known = {page_guid(i) for i in range(max(len(shapes), len(list(OUT.glob('CHR_CinderArcanist_*.png.meta')))))}
    found = {}
    for path in sorted((ROOT / 'Assets').rglob('*')):
        if path.suffix not in ('.asset', '.prefab', '.unity', '.mat', '.anim', '.controller') or path == set_path:
            continue
        text = path.read_text(encoding='utf-8', errors='ignore')
        if not any(g in text for g in known):
            continue
        for file_id, guid in re.findall(r'fileID: (-?\d+), guid: ([0-9a-f]{32})', text):
            if guid in known and int(file_id) in ids:
                found[ids[int(file_id)]] = (guid, path.relative_to(ROOT).as_posix())
    return found


# Order the pages so that every frame referenced from outside the set stays on the texture (the GUID) it was on.
pins = references_from_outside(OUT / 'CinderArcanistArtSet.asset')
by_name = {f['name']: f for f in frames}
final = {}                       # packed page -> atlas index
for name, (guid, where) in sorted(pins.items()):
    target = next((i for i in range(len(shapes)) if page_guid(i) == guid), None)
    page = by_name[name]['page']
    if target is None or final.get(page, target) != target or any(t == target and p != page for p, t in final.items()):
        sys.exit('%s references %s on texture %s, where this packing cannot keep it' % (where, name, guid))
    final[page] = target
spare = [i for i in range(len(shapes)) if i not in final.values()]
for page in range(len(shapes)):
    if page not in final:
        final[page] = spare.pop(0)
for f in frames:
    f['page'] = final[f['page']]
shapes = [shapes[p] for p in sorted(range(len(shapes)), key=lambda p: final[p])]

OUT.mkdir(parents=True, exist_ok=True)
DOCS.mkdir(parents=True, exist_ok=True)
for stale in OUT.glob('CHR_CinderArcanist_*.png'):
    if int(stale.stem.rsplit('_', 1)[1]) >= len(shapes):
        stale.unlink()
        Path(str(stale) + '.meta').unlink(missing_ok=True)
folder_meta = ROOT / 'Assets/_Project/Art/Characters/CinderArcanist.meta'
if not folder_meta.exists():
    folder_meta.write_text('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                           '  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % FOLDER_GUID, encoding='utf-8', newline='\n')

atlases = []
for index, (page_w, page_h) in enumerate(shapes):
    name = 'CHR_CinderArcanist_%d' % index
    path = OUT / (name + '.png')
    guid = existing_guid(Path(str(path) + '.meta'))
    canvas = np.zeros((page_h, page_w, 4), np.uint8)
    sprites = []
    for f in frames:
        if f['page'] != index:
            continue
        x, y = f['at']
        h, w = f['pixels'].shape[:2]
        canvas[y:y + h, x:x + w] = f['pixels']
        x0, y0, x1, y1 = f['crop']
        f['pivot'] = ((pivot_x - x0) / w, (y1 - pivot_top) / h)
        f['unity_rect'] = (x, page_h - y - h, w, h)
        f['id'] = internal_id(f['name'])
        f['guid'] = guid
        sprites.append(f)
    Image.fromarray(canvas, 'RGBA').save(path, optimize=True)
    Path(str(path) + '.meta').write_text(texture_meta(guid, (page_w, page_h), sprites), encoding='utf-8', newline='\n')
    atlases.append(dict(file=path.name, guid=guid, size=[page_w, page_h], sprites=len(sprites),
                        sha256=hashlib.sha256(path.read_bytes()).hexdigest()))

by_key = {(f['clip'], f['facing'], f['frame']): f for f in frames}
clips = []
for m, clip in enumerate(MOTIONS):
    count = meta_in['clips'][clip]['frames']
    for fi, facing in enumerate(FACINGS):
        clips.append((m, fi, [(by_key[(clip, facing, k)]['id'], by_key[(clip, facing, k)]['guid']) for k in range(count)]))
timings = [(m, meta_in['clips'][c]['fps'], meta_in['clips'][c]['loops']) for m, c in enumerate(MOTIONS)]
set_path = OUT / 'CinderArcanistArtSet.asset'
set_guid = existing_guid(Path(str(set_path) + '.meta'))
set_path.write_text(art_set(clips, timings, meta_in['stride']), encoding='utf-8', newline='\n')
Path(str(set_path) + '.meta').write_text('fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n'
                                         '  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n'
                                         '  assetBundleVariant: \n' % set_guid, encoding='utf-8', newline='\n')

texels = sum(a['size'][0] * a['size'][1] for a in atlases)
used = sum(f['pixels'].shape[0] * f['pixels'].shape[1] for f in frames)
report = dict(art_set=dict(file=set_path.name, guid=set_guid), atlases=atlases, pixels_per_unit=PPU,
              canvas=list(size), pivot=meta_in['pivot'], stride=meta_in['stride'], facings=FACINGS,
              kept_on_their_texture={name: where for name, (guid, where) in sorted(pins.items())},
              memory=dict(astc_4x4_bytes=texels, rgba32_bytes=texels * 4, frame_texels=used,
                          fill=round(used / texels, 3)),
              frames=[dict(name=f['name'], atlas=f['page'], rect=list(f['unity_rect']),
                           pivot=[round(f['pivot'][0], 6), round(f['pivot'][1], 6)], crop=[int(v) for v in f['crop']],
                           crucible=f['crucible'], flare=f['flare'], source=f['source']) for f in frames])
(DOCS / 'frames.json').write_text(json.dumps(report, indent=1), encoding='utf-8', newline='\n')
print('ARCANIST_PACKED frames %d atlases %s ASTC4x4 %.2f MB (%d texels, fill %.0f%%) set %s'
      % (len(frames), [tuple(a['size']) for a in atlases], texels / 2 ** 20, texels, 100 * used / texels, set_guid))
