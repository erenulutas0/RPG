"""Packs the Mirror Sentry's raw Blender frames (BuildBlenderMirrorSentry.py) into the game's sprites and its EnemyArtSet,
and draws its two effects (docs/50 section 5).

    python Tools/ArtReview/PackMirrorSentrySprites.py [--render DIR]

(python -I hides the user site; run it as `python -I -c "import sys, runpy; sys.path.append(<user site-packages>);
sys.argv = ['PackMirrorSentrySprites.py']; runpy.run_path('Tools/ArtReview/PackMirrorSentrySprites.py',
run_name='__main__')"` when numpy lives there.)

Per frame: the 4x render is box-filtered down in linear light with premultiplied alpha (the floor pivot stays on a
texel corner). The mirror is laid in from the aux pass - its coverage times a pale silver-violet with two diagonal
sheen bands, at MIRROR_VALUE times the frame's own value (the struck flash, the dying dim) - so its value is tuned here,
never by rendering again. The visor's violet light blooms softly inside the figure, the mirror lifts its rim a little,
and a one-texel dark outline closes the silhouette. No warm colour is laid anywhere (docs/50 rule 3).

The elite contract is the Grunt Captain's, frame for frame: six bodies a facing with their white flashes, the strike
windup (front, rear) with flashes, the two remains, and its own death of seven drawings a facing, listed eleven steps
long (the last held) at the Captain's 0.06 s. The bodies and windups share one canvas, symmetric about the pivot, each
side whole ASTC blocks; the death drawings are cropped to what they show (whole blocks, the pivot inside). The Sentry
is a head taller than the Captain, so its canvas is taller than the Captain's 168x176: the sizes follow the figure.

Writes, all regenerated on every run (existing GUIDs kept, new files get fresh uuid4 GUIDs):

    Assets/_Project/Art/Characters/MirrorSentry/CHR_MirrorSentry_<frame>.png (+ _Flash.png) with .meta files copied
        from the Grunt Captain's matching sprite (its import settings exactly; the GUID and the pivot are this sprite's)
    Assets/_Project/Art/Characters/MirrorSentry/CHR_MirrorSentry_glint-arc.png: the mirror glint on the floor, a violet
        arc 120 degrees wide drawn TOP-DOWN (not foreshortened), pointing +x, its pivot the arc's centre (the Sentry's
        root): the runtime turns it to the facing about z under a parent squashed by ArenaFloor.DepthScale on y
    Assets/_Project/Art/Characters/MirrorSentry/CHR_MirrorSentry_reflect-<0..2>.png: the reflect glint, a violet streak
        one world unit long along +x, its pivot the mirror's end: the runtime puts it on the mirror, turns it to the hero
        and scales x by the distance (forming, full, fading)
    Assets/_Project/Art/Characters/MirrorSentry/MirrorSentryArtSet.asset (+ .meta): the EnemyArtSet, laid out line for
        line as GruntCaptainArtSet.asset, `_frost` empty (born on floor 2, docs/50 rule 6)
    ArtDirection/2026-11-12/mirror-sentry-01/sprites.json: every texture's size, pivot, GUID and ASTC 4x4 bytes, the
        set's values and the mirror's anchor in each body frame
"""
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


RENDER = Path(arg('--render', str(ROOT / 'TestResults/mirror-sentry-render')))
OUT = ROOT / 'Assets/_Project/Art/Characters/MirrorSentry'
CAPTAIN = ROOT / 'Assets/_Project/Art/Characters/GruntCaptain'
DOCS = ROOT / 'ArtDirection/2026-11-12/mirror-sentry-01'
STEM = 'MirrorSentry'
PPU = 128
MARGIN = 2
OUTLINE = (0x12, 0x0F, 0x1A)
ANDROID_MAX = 256           # the Captain's import caps Android at 256: every texture must fit under it

# ---- The mirror: tune here (docs/50: the brightest shape on the body, a pale silver-violet slab) ----
MIRROR_VALUE = 1.0                         # the whole mirror's brightness
MIRROR_TOP = '#E7E2FF'                     # its upper left (sheen coordinate 0)
MIRROR_BOTTOM = '#AFA4E6'                  # its lower right (1): the floor's dark reflected
MIRROR_BANDS = [(0.33, 0.055, '#FFFFFF', 0.85), (0.46, 0.018, '#FFFFFF', 0.6)]   # centre, half-width, colour, strength
MIRROR_RIM_GLOW = 0.12                     # the mirror's light on the rim round it
# ---- The visor's violet light ----
VIOLET = '#8B6CE8'
VISOR_BLOOM = ((1.0, 0.7), (2.6, 0.3))     # (sigma, gain) pairs, inside the figure only

FRAME_ORDER = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b']
FACINGS = ['front', 'rear']
DYING = 7                   # drawings of the own death a facing (the Captain's seven)
DEATH_STEPS = 11            # steps listed (the Captain's: seven, the last held four more)


def to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def hexlin(h):
    h = h.lstrip('#')
    return to_linear(np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)]) / 255.0)


def blur(field, sigma):
    """A separable gaussian on an (h, w) field, zero beyond its edges."""
    radius = max(1, int(sigma * 3 + 0.5))
    x = np.arange(-radius, radius + 1)
    k = np.exp(-x * x / (2 * sigma * sigma))
    k /= k.sum()
    out = field
    for axis in (0, 1):
        pad = np.pad(out, [(radius, radius) if a == axis else (0, 0) for a in range(2)])
        acc = np.zeros_like(out)
        for i, w in enumerate(k):
            acc += w * (pad[i:i + out.shape[0]] if axis == 0 else pad[:, i:i + out.shape[1]])
        out = acc
    return out


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def downsample(px, ss):
    h, w = px.shape[0] // ss, px.shape[1] // ss
    return px.reshape(h, ss, w, ss, px.shape[2]).mean(axis=(1, 3))


def edge_check(alpha, path):
    if max(alpha[0].max(), alpha[-1].max(), alpha[:, 0].max(), alpha[:, -1].max()) > 0:
        sys.exit('%s touches the edge of its render canvas: widen it in BuildBlenderMirrorSentry.py' % path)


def mirror_colour(sheen):
    """The mirror's colour (linear) at a sheen coordinate: a diagonal gradient and two bright bands across it."""
    s = sheen[..., None]
    colour = hexlin(MIRROR_TOP) * (1 - s) + hexlin(MIRROR_BOTTOM) * s
    for centre, half, hexc, strength in MIRROR_BANDS:
        band = np.exp(-((sheen - centre) / half) ** 2)[..., None] * strength
        colour = colour * (1 - band) + hexlin(hexc) * band
    return colour * MIRROR_VALUE


def process(record, ss):
    """One frame at the game's resolution: straight-alpha sRGB uint8, and its alpha."""
    beauty = np.asarray(Image.open(RENDER / record['file']).convert('RGBA')).astype(np.float64) / 255.0
    edge_check(beauty[..., 3], record['file'])
    alpha = beauty[..., 3:4]
    lin = to_linear(beauty[..., :3]) * alpha
    aux = to_linear(np.asarray(Image.open(RENDER / record['aux']).convert('RGB')).astype(np.float64) / 255.0)
    lin = downsample(lin, ss)
    alpha = downsample(alpha, ss)
    aux = downsample(aux, ss)
    m = np.clip(aux[..., 0], 0, 1)
    sheen = np.where(m > 1e-4, aux[..., 1] / np.maximum(m, 1e-4), 0)
    # The beauty drew the mirror black, so its colour adds by coverage and an edge texel is never counted twice.
    lin = lin + mirror_colour(np.clip(sheen, 0, 1)) * (m * record['mirror'])[..., None]
    inside = alpha * (1 - m[..., None])
    rim = blur(m, 1.6)[..., None] * MIRROR_RIM_GLOW * record['mirror']
    lin = lin + hexlin('#D9D0FF') * rim * inside
    glow = aux[..., 2]
    light = sum(blur(glow, sigma) * gain for sigma, gain in VISOR_BLOOM)[..., None]
    lin = lin + hexlin(VIOLET) * np.clip(light, 0, 0.8) * alpha
    # One texel of dark outline round the silhouette, soft at the corners.
    pad = np.pad(alpha[..., 0], 1)
    grown = np.maximum.reduce([pad[1:-1, 1:-1], pad[:-2, 1:-1], pad[2:, 1:-1], pad[1:-1, :-2], pad[1:-1, 2:],
                               0.7 * pad[:-2, :-2], 0.7 * pad[2:, 2:], 0.7 * pad[:-2, 2:], 0.7 * pad[2:, :-2]])[..., None]
    lin = lin + hexlin('#%02X%02X%02X' % OUTLINE) * grown * (1 - alpha)
    alpha = alpha + grown * (1 - alpha)
    return finish(lin, alpha)


def finish(lin, alpha):
    straight = np.where(alpha > 1e-6, lin / np.maximum(alpha, 1e-6), 0)
    out = np.concatenate([to_srgb(straight), np.clip(alpha, 0, 1)], axis=-1)
    out = (out * 255 + 0.5).astype(np.uint8)
    out[out[..., 3] < 2] = 0
    return out


def flash_of(px):
    """The hit flash: the body's own alpha in white, white under the transparent texels too, as the Captain's."""
    out = np.full_like(px, 255)
    out[..., 3] = px[..., 3]
    return out


def fit(frames, pivot):
    """One canvas for every living frame: their texels and a margin, symmetric about the pivot (the body's bounds
    centre on its feet), each side whole ASTC blocks. pivot is (x, rows from the bottom)."""
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
    out = {}
    for name, px in frames.items():
        canvas = np.zeros((height, 2 * half, 4), np.uint8)
        canvas[:, :] = px[y0:y0 + height, x0:x0 + 2 * half]
        out[name] = canvas
    return out, (int(half), int(down))


def tight(px, pivot):
    """A death drawing cropped to its texels and a margin, the pivot inside, each side a multiple of 4."""
    h, w = px.shape[:2]
    prow = h - pivot[1]
    y, x = np.nonzero(px[..., 3])
    x0, x1 = min(x.min(), pivot[0]) - MARGIN, max(x.max() + 1, pivot[0]) + MARGIN
    y0, y1 = min(y.min(), prow) - MARGIN, max(y.max() + 1, prow) + MARGIN
    x0 -= (-(x1 - x0)) % 4
    y0 -= (-(y1 - y0)) % 4
    if x0 < 0 or y0 < 0 or x1 > w or y1 > h:
        sys.exit('a death drawing runs off its canvas')
    return px[y0:y1, x0:x1], (pivot[0] - x0, y1 - prow)


# ---------------------------------------------------------------------------------------------------------------------
# The effects, drawn at 4x and box-filtered down
# ---------------------------------------------------------------------------------------------------------------------
FX_SS = 4
ARC_RADIUS = 0.72           # world units on the floor from the root to the arc's middle line
ARC_HALF = 60.0             # degrees either side of the facing: the guard's 120-degree front arc


def premult_layer(colour_lin, alpha):
    return np.concatenate([colour_lin * alpha[..., None], alpha[..., None]], axis=-1)


def over(dst, src):
    return src + dst * (1 - src[..., 3:4])


def glint_arc():
    """The mirror glint (docs/50 section 5): a short violet arc on the floor as wide as the front arc, brightest at the
    facing and thinning to nothing at the arc's ends. Top-down; the pivot (the root) on the left edge's middle."""
    w, h = 108, 192
    xs = (np.arange(w * FX_SS) + 0.5) / (PPU * FX_SS)
    ys = ((h * FX_SS / 2) - np.arange(h * FX_SS) - 0.5) / (PPU * FX_SS)
    x, y = np.meshgrid(xs, ys)
    r = np.hypot(x, y)
    t = np.clip(np.abs(np.degrees(np.arctan2(y, x))) / ARC_HALF, 0, 1)
    taper = np.clip(1 - t * t, 0, 1)
    d = np.abs(r - ARC_RADIUS)
    core_w = 0.03 * taper ** 0.7 + 1e-4
    core = smoothstep(core_w + 0.012, core_w, d) * (taper > 0)
    glow = np.exp(-(d / (0.075 * taper ** 0.5 + 1e-4)) ** 2) * 0.5 * taper ** 0.6
    img = premult_layer(hexlin(VIOLET), np.clip(glow, 0, 1))
    img = over(img, premult_layer(hexlin('#C9BCFF'), np.clip(core, 0, 1)))
    centre = smoothstep(0.012, 0.004, d) * taper ** 3 * 0.8
    img = over(img, premult_layer(hexlin('#F1EDFF'), np.clip(centre, 0, 1)))
    img = downsample(img, FX_SS)
    return finish(img[..., :3], img[..., 3:4]), (0, h // 2)


def reflect_glint(k):
    """The reflect glint, frame k: a violet streak from the mirror (x 0) toward the hero (x 1 world unit) - forming
    from a flare at the mirror, full with a spark at the hero, fading in broken dashes."""
    w, h, ox = 156, 32, 12
    xs = (np.arange(w * FX_SS) + 0.5) / (PPU * FX_SS) - ox / PPU
    ys = ((h * FX_SS / 2) - np.arange(h * FX_SS) - 0.5) / (PPU * FX_SS)
    x, y = np.meshgrid(xs, ys)
    length, half, strength = [(0.55, 0.016, 1.0), (1.0, 0.024, 1.0), (1.0, 0.013, 0.6)][k]
    along = np.clip(x / length, 0, 1)
    within = (x >= 0) & (x <= length)
    width = half * (0.55 + 0.45 * np.sin(np.pi * along) ** 0.5) + 1e-4
    core = smoothstep(width + 0.006, width * 0.5, np.abs(y)) * within
    halo = np.exp(-(y / (width * 3.2)) ** 2) * within * 0.55
    if k == 0:
        fade = smoothstep(length, length * 0.6, x)
        core, halo = core * fade, halo * fade
    if k == 2:
        dashes = 0.35 + 0.65 * (np.cos(x * 2 * np.pi * 7) > -0.2)
        core, halo = core * dashes, halo * dashes
    img = premult_layer(hexlin(VIOLET), np.clip(halo * strength, 0, 1))
    img = over(img, premult_layer(hexlin('#D7CCFF'), np.clip(core * strength, 0, 1)))
    for cx, radius, amount in {0: [(0.0, 0.07, 1.0)], 1: [(0.0, 0.04, 0.7), (1.0, 0.05, 0.9)],
                               2: [(1.0, 0.035, 0.45)]}[k]:
        dist = np.hypot(x - cx, y)
        star = (np.exp(-(dist / radius) ** 2) + 0.6 * np.exp(-(np.abs(y) / 0.006) ** 2 - ((x - cx) / (radius * 1.8)) ** 2)
                + 0.6 * np.exp(-(np.abs(x - cx) / 0.006) ** 2 - (y / (radius * 1.2)) ** 2))
        img = over(img, premult_layer(hexlin('#F4F0FF'), np.clip(star * amount, 0, 1)))
    img = downsample(img, FX_SS)
    return finish(img[..., :3], img[..., 3:4]), (ox, h // 2)


# ---------------------------------------------------------------------------------------------------------------------
# Assets
# ---------------------------------------------------------------------------------------------------------------------
def existing_guid(meta):
    if meta.exists():
        m = re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(encoding='utf-8'), re.M)
        if m:
            return m.group(1)
    return uuid.uuid4().hex


def guid_of(meta):
    return re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(encoding='utf-8'), re.M).group(1)


def astc_bytes(w, h):
    return ((w + 3) // 4) * ((h + 3) // 4) * 16


def write_sprite(name, px, pivot, captain_name, textures):
    """The PNG and its .meta: the Captain's matching sprite's import settings, this sprite's GUID and pivot."""
    h, w = px.shape[:2]
    if max(w, h) > ANDROID_MAX:
        sys.exit('%s is %dx%d: over the Android cap %d the Captain\'s import sets' % (name, w, h, ANDROID_MAX))
    path = OUT / ('CHR_%s_%s.png' % (STEM, name))
    meta = Path(str(path) + '.meta')
    guid = existing_guid(meta)
    Image.fromarray(px, 'RGBA').save(path, optimize=True)
    template = (CAPTAIN / ('CHR_Captain_%s.tga.meta' % captain_name)).read_bytes().decode('utf-8')
    text = re.sub(r'^guid: \w+', 'guid: ' + guid, template, count=1, flags=re.M)
    pivot_norm = (pivot[0] / w, pivot[1] / h)
    text, n = re.subn(r'spritePivot: \{x: [^,]+, y: [^}]+\}',
                      'spritePivot: {x: %.8f, y: %.8f}' % pivot_norm, text, count=1)
    assert n == 1
    meta.write_bytes(text.encode('utf-8'))
    textures.append(dict(file=path.name, guid=guid, size=[w, h], pivot=list(pivot),
                         pivot_normalised=[round(pivot_norm[0], 8), round(pivot_norm[1], 8)],
                         meta_from='CHR_Captain_%s.tga.meta' % captain_name, astc_4x4_bytes=astc_bytes(w, h)))
    return guid


def main():
    meta = json.loads((RENDER / 'frames.json').read_text())
    ss = meta['supersample']
    records = {(r['facing'], r['pose']): r for r in meta['frames']}
    OUT.mkdir(parents=True, exist_ok=True)
    folder_meta = OUT.parent / (OUT.name + '.meta')
    if not folder_meta.exists():
        folder_meta.write_text('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                               '  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex,
                               encoding='utf-8', newline='\n')
    textures = []
    guids = {}            # sprite name -> guid (the Captain's naming, CHR_<stem>_<name>)
    # The living bodies and the windups: one canvas.
    living = {}
    for facing in FACINGS:
        for pose in FRAME_ORDER + ['windup']:
            living['%s-%s' % (facing, pose)] = process(records[(facing, pose)], ss)
    canvas = records[('front', 'idle')]['canvas']
    pivot = tuple(records[('front', 'idle')]['pivot'])
    living, body_pivot = fit(living, pivot)
    for name, px in living.items():
        guids[name] = write_sprite(name, px, body_pivot, name, textures)
        guids[name + '_Flash'] = write_sprite(name + '_Flash', flash_of(px), body_pivot, name + '_Flash', textures)
    # The own death, and the two remains.
    for facing in FACINGS:
        for k in range(DYING):
            r = records[(facing, 'dying-%d' % k)]
            px, p = tight(process(r, ss), tuple(r['pivot']))
            name = 'dying-%s-%d' % (facing, k)
            guids[name] = write_sprite(name, px, p, name, textures)
    for name in ('death-collapse', 'death-cool'):
        r = records[('front', name)]
        px, p = tight(process(r, ss), tuple(r['pivot']))
        guids[name] = write_sprite(name, px, p, name, textures)
    # The effects: not part of the set (the runtime draws them), imported as the Captain's idle is.
    arc, arc_pivot = glint_arc()
    effects = {'glint-arc': write_sprite('glint-arc', arc, arc_pivot, 'front-idle', textures)}
    for k in range(3):
        px, p = reflect_glint(k)
        effects['reflect-%d' % k] = write_sprite('reflect-%d' % k, px, p, 'front-idle', textures)
    # The set's numbers from the art: the top of every living pose (the crown included) and a little headroom, as the
    # Captain's; the shadow the Grunt's 0.78 of the idle's width without the shield (the legs and pauldrons).
    h = next(iter(living.values())).shape[0]
    top = 0
    for px in living.values():
        ys, _ = np.nonzero(px[..., 3] > 128)
        top = max(top, (h - body_pivot[1]) - ys.min())
    visual_top = round(top / PPU + 0.05, 2)
    shadow_width = 0.86
    # The art set: the Captain's own asset, line for line, each of its GUIDs swapped for this set's same-named sprite.
    captain_set = (CAPTAIN / 'GruntCaptainArtSet.asset').read_bytes().decode('utf-8')
    swap = {}
    for meta_path in CAPTAIN.glob('CHR_Captain_*.tga.meta'):
        name = meta_path.name[len('CHR_Captain_'):-len('.tga.meta')]
        swap[guid_of(meta_path)] = guids[name]
    text = re.sub(r'guid: ([0-9a-f]{32}), type: 3', lambda m: 'guid: %s, type: 3' % swap[m.group(1)], captain_set)
    text = text.replace('m_Name: GruntCaptainArtSet', 'm_Name: %sArtSet' % STEM)
    text = re.sub(r'_visualTop: [\d.]+', '_visualTop: %s' % visual_top, text)
    text = re.sub(r'_shadowWidth: [\d.]+', '_shadowWidth: %s' % shadow_width, text)
    text = re.sub(r'_frost: \{[^}]*\}', '_frost: {fileID: 0}', text)
    assert text.count('type: 3}') == captain_set.count('type: 3}') and 'GruntCaptain' not in text
    set_path = OUT / ('%sArtSet.asset' % STEM)
    set_meta = Path(str(set_path) + '.meta')
    set_guid = existing_guid(set_meta)
    set_path.write_bytes(text.encode('utf-8'))
    captain_meta = (CAPTAIN / 'GruntCaptainArtSet.asset.meta').read_bytes().decode('utf-8')
    set_meta.write_bytes(re.sub(r'^guid: \w+', 'guid: ' + set_guid, captain_meta, count=1, flags=re.M).encode('utf-8'))
    total = sum(t['astc_4x4_bytes'] for t in textures)
    in_set = sum(t['astc_4x4_bytes'] for t in textures if not t['file'].startswith(('CHR_%s_glint' % STEM,
                                                                                    'CHR_%s_reflect' % STEM)))
    anchors = {'%s-%s' % (r['facing'], r['pose']): r['mirror_centre'] for r in meta['frames'] if r['kind'] == 'body'}
    report = dict(art_set=dict(file=set_path.name, guid=set_guid), body_size=list(living['front-idle'].shape[1::-1]),
                  body_pivot=list(body_pivot), visual_top=visual_top, shadow_width=shadow_width,
                  walk_cycle_distance=meta['stride'], mirror=dict(value=MIRROR_VALUE, top=MIRROR_TOP,
                                                                   bottom=MIRROR_BOTTOM, bands=MIRROR_BANDS),
                  mirror_anchor_world_units=anchors, effects=effects,
                  glint_arc=dict(radius=ARC_RADIUS, half_angle=ARC_HALF, drawn='top-down, pointing +x'),
                  textures=textures, texture_count=len(textures), astc_4x4_bytes=total, astc_4x4_bytes_in_set=in_set)
    DOCS.mkdir(parents=True, exist_ok=True)
    (DOCS / 'sprites.json').write_text(json.dumps(report, indent=1))
    print('SENTRY_PACKED body %dx%d pivot %s visualTop %s textures %d ASTC4x4 %d bytes (set %d) set %s'
          % (living['front-idle'].shape[1], living['front-idle'].shape[0], body_pivot, visual_top, len(textures),
             total, in_set, set_guid))


if __name__ == '__main__':
    main()
