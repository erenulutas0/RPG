"""Packs the Cinder Bloom's raw Blender frames (BuildBlenderCinderBloom.py) into the game's sprites and its EnemyArtSet,
its open state's drawings beside the set, and draws the bark chip (docs/52 section 5).

    python Tools/ArtReview/PackCinderBloomSprites.py [--render DIR] [--docs DIR]

(python -I hides the user site; run it as `python -I -c "import sys, runpy; sys.path.append(<user site-packages>);
sys.argv = ['PackCinderBloomSprites.py']; runpy.run_path('Tools/ArtReview/PackCinderBloomSprites.py',
run_name='__main__')"` when numpy lives there.)

Per frame: the 4x render is box-filtered down in linear light with premultiplied alpha (the floor pivot stays on a
texel corner). The bloom's family - the petals and heart, the furnace light behind the shutters' seam and the rear
grate, the eyes - is laid in from the aux pass: its coverage times an orchid ramp read at its tone (deep #5A1F66 at a
petal's shaded base, orchid #D86BD0, pale #F6C2F0 at a lit tip; docs/51's enemy palette), at BLOOM_VALUE times the
frame's own value (the dying dim), so its value is tuned here, never by rendering again. The glow blooms softly inside
the figure and, on the open drawings, a little past it (an orchid halo), and a one-texel dark outline closes the
silhouette. Nothing warm and nothing chartreuse is laid anywhere (docs/52 section 2 rule 3: red-orange is the
telegraphs', gloamlight the garden's).

The elite contract is the Grunt Captain's, frame for frame: six bodies a facing with their white flashes, the strike
windup (front, rear) with flashes, the two remains, and its own death of seven drawings a facing, listed eleven steps
long (the last held) at the Captain's 0.06 s - these are the CLOSED state. A facing's five walking bodies share one
canvas (whole ASTC blocks, the pivot where the feet stand - not centred: the maul on the shoulder reaches to one side);
the windup, the strike, the open drawings and the death are each cropped to what they show (whole blocks, the pivot
inside), so the maul's reach costs only the frames that reach.

The OPEN state is not in the set (EnemyArtSet has no slot for it): three drawings a facing - unfold-0 (the maul off
the shoulder, the shutters cracking on a bud), unfold-1 (half open) and open (rooted, wide open: held for the open
window); closing plays unfold-1, unfold-0 (docs/52 section 5) - each with its white flash, imported as the set's are and
listed in sprites.json for the runtime.

Writes, all regenerated on every run (existing GUIDs kept, new files get fresh uuid4 GUIDs):

    Assets/_Project/Art/Characters/CinderBloom/CHR_CinderBloom_<frame>.png (+ _Flash.png) with .meta files copied
        from the Grunt Captain's matching sprite (its import settings exactly; the GUID and the pivot are this sprite's)
    Assets/_Project/Art/Characters/CinderBloom/CHR_CinderBloom_<facing>-<unfold-0|unfold-1|open>.png (+ _Flash)
        the open state, imported as the Captain's idle and its flash are
    Assets/_Project/Art/Characters/CinderBloom/CHR_CinderBloom_bark-chip-<0..2>.png: the bark chip a struck closed Bloom
        throws (docs/52: in place of the full white flash), splinters and ash about a point, its pivot that point
    Assets/_Project/Art/Characters/CinderBloom/CinderBloomArtSet.asset (+ .meta): the EnemyArtSet, laid out line for
        line as GruntCaptainArtSet.asset, `_frost` and `_gloam` empty (born on floor 3, docs/52 section 2 rule 6)
    <docs>/sprites.json: every texture's size, pivot, GUID and ASTC 4x4 bytes, the set's values, the open state's
        drawings in play order and the furnace's anchor in each living frame (default
        ArtDirection/2026-11-14/cinder-bloom-01, untracked)
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


RENDER = Path(arg('--render', str(ROOT / 'TestResults/cinder-bloom-render')))
DOCS = Path(arg('--docs', str(ROOT / 'ArtDirection/2026-11-14/cinder-bloom-01')))
OUT = ROOT / 'Assets/_Project/Art/Characters/CinderBloom'
CAPTAIN = ROOT / 'Assets/_Project/Art/Characters/GruntCaptain'
STEM = 'CinderBloom'
PPU = 128
MARGIN = 2
OUTLINE = (0x12, 0x0F, 0x0E)
ANDROID_MAX = 256           # the Captain's import caps Android at 256: every texture must fit under it

# ---- The bloom: tune here (docs/52: the brightest orchid on the floor, #D86BD0 to #F6C2F0, never red-orange) ----
BLOOM_VALUE = 1.0
# The orchid ramp by tone (0 a shaded base deep in the furnace, 1 a lit tip / the heart), interpolated in linear light.
RAMP = [(0.0, '#3A1046'), (0.24, '#5A1F66'), (0.46, '#B850B8'), (0.6, '#D86BD0'), (0.78, '#F2A8EC'), (1.0, '#FDE6FA')]
ORCHID = '#D86BD0'
GLOW_INSIDE = ((1.2, 0.55), (3.0, 0.35))      # (sigma, gain) pairs, inside the figure only
GLOW_HALO = (3.5, 1.0, 0.55)                 # the open drawings' halo past the silhouette: sigma, gain, its alpha cap

FRAME_ORDER = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b']
WALKING = ['idle', 'step-a', 'step-b', 'pass-a', 'pass-b']
OPEN_ORDER = ['unfold-0', 'unfold-1', 'open']
FACINGS = ['front', 'rear']
DYING = 7                   # drawings of the own death a facing (the Captain's seven)


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
        sys.exit('%s touches the edge of its render canvas: widen it in BuildBlenderCinderBloom.py' % path)


def ramp(tone):
    """The orchid (linear) at each tone."""
    stops = np.array([s for s, _ in RAMP])
    cols = np.array([hexlin(h) for _, h in RAMP])
    t = np.clip(tone, 0, 1)
    out = np.zeros(t.shape + (3,))
    for c in range(3):
        out[..., c] = np.interp(t, stops, cols[:, c])
    return out


def load_premultiplied(path):
    px = np.asarray(Image.open(path).convert('RGBA')).astype(np.float64) / 255.0
    alpha = px[..., 3:4]
    return np.concatenate([to_linear(px[..., :3]) * alpha, alpha], axis=-1)


def process(record, ss):
    """One frame at the game's resolution: straight-alpha sRGB uint8."""
    beauty = load_premultiplied(RENDER / record['file'])
    edge_check(beauty[..., 3], record['file'])
    aux = downsample(load_premultiplied(RENDER / record['aux']), ss)
    beauty = downsample(beauty, ss)
    lin, alpha = beauty[..., :3], beauty[..., 3:4]
    m = np.clip(aux[..., 0], 0, 1)
    tone = np.where(m > 1e-4, aux[..., 1] / np.maximum(m, 1e-4), 0)
    value = BLOOM_VALUE * record['bloom']
    # The beauty drew the bloom black, so its colour adds by coverage and an edge texel is never counted twice.
    lin = lin + ramp(tone) * (m * value)[..., None]
    glow = aux[..., 2] * value
    light = sum(blur(glow, sigma) * gain for sigma, gain in GLOW_INSIDE)[..., None]
    lin = lin + hexlin(ORCHID) * np.clip(light, 0, 0.7) * alpha * (1 - 0.6 * m[..., None])
    # One texel of dark outline round the silhouette, soft at the corners.
    pad = np.pad(alpha[..., 0], 1)
    grown = np.maximum.reduce([pad[1:-1, 1:-1], pad[:-2, 1:-1], pad[2:, 1:-1], pad[1:-1, :-2], pad[1:-1, 2:],
                               0.7 * pad[:-2, :-2], 0.7 * pad[2:, 2:], 0.7 * pad[:-2, 2:], 0.7 * pad[2:, :-2]])[..., None]
    lin = lin + hexlin('#%02X%02X%02X' % OUTLINE) * grown * (1 - alpha)
    alpha = alpha + grown * (1 - alpha)
    if record['kind'] == 'open':
        # The open bloom's halo: orchid light a little past the silhouette, so "strike here" reads at the phone's scale.
        sigma, gain, cap = GLOW_HALO
        halo = np.clip(blur(glow, sigma) * gain, 0, cap)[..., None] * (1 - alpha)
        lin = lin + hexlin(ORCHID) * halo
        alpha = alpha + halo
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
    """One canvas for a group of frames: their texels and a margin about the pivot (the maul on the shoulder reaches
    further to one side than the other, so the canvas is not centred), whole ASTC blocks. pivot is (x, rows from
    the bottom)."""
    h = next(iter(frames.values())).shape[0]
    left = right = top = bottom = 0
    for px in frames.values():
        ys, xs = np.nonzero(px[..., 3] > 0)
        left = max(left, pivot[0] - xs.min())
        right = max(right, xs.max() + 1 - pivot[0])
        top = max(top, (h - pivot[1]) - ys.min())
        bottom = max(bottom, ys.max() + 1 - (h - pivot[1]))
    left, right = left + MARGIN, right + MARGIN
    width = -(-(left + right) // 4) * 4
    left = width - right
    up, down = top + MARGIN, bottom + MARGIN
    height = -(-(up + down) // 4) * 4
    up = height - down
    x0, y0 = pivot[0] - left, (h - pivot[1]) - up
    out = {}
    for name, px in frames.items():
        out[name] = px[y0:y0 + height, x0:x0 + width].copy()
    return out, (int(left), int(down))


def tight(px, pivot):
    """A drawing cropped to its texels and a margin, the pivot inside, each side a multiple of 4."""
    h, w = px.shape[:2]
    prow = h - pivot[1]
    y, x = np.nonzero(px[..., 3])
    x0, x1 = min(x.min(), pivot[0]) - MARGIN, max(x.max() + 1, pivot[0]) + MARGIN
    y0, y1 = min(y.min(), prow) - MARGIN, max(y.max() + 1, prow) + MARGIN
    x0 -= (-(x1 - x0)) % 4
    y0 -= (-(y1 - y0)) % 4
    if x0 < 0 or y0 < 0 or x1 > w or y1 > h:
        sys.exit('a drawing runs off its canvas')
    return px[y0:y1, x0:x1], (pivot[0] - x0, y1 - prow)


# ---------------------------------------------------------------------------------------------------------------------
# The bark chip, drawn at 4x and box-filtered down
# ---------------------------------------------------------------------------------------------------------------------
FX_SS = 4


def premult_layer(colour_lin, alpha):
    return np.concatenate([colour_lin * alpha[..., None], alpha[..., None]], axis=-1)


def over(dst, src):
    return src + dst * (1 - src[..., 3:4])


def bark_chip(k):
    """The bark chip, frame k (docs/52: a struck closed Bloom shows a dull bark chip, not the full white flash):
    splinters of charred bark thrown up and out from the struck point, a puff of ash behind them - a burst, a spread,
    a fall. Drawn as seen (the floor's 2:1 does not apply: it is in the air); its pivot the struck point."""
    w, h = 64, 56
    cx, cy = 32, 30
    rng = np.random.default_rng(7)
    angles = np.radians(np.array([200, 235, 262, 290, 318, 345, 20, 160]) + rng.uniform(-8, 8, 8))
    lengths = rng.uniform(0.6, 1.0, 8)
    ys, xs = np.mgrid[0:h * FX_SS, 0:w * FX_SS]
    x = (xs + 0.5) / FX_SS - cx
    y = cy - (ys + 0.5) / FX_SS
    img = np.zeros((h * FX_SS, w * FX_SS, 4))
    travel = (5.0, 11.0, 15.0)[k]
    drop = (0.0, 1.5, 5.0)[k]
    fade = (1.0, 0.9, 0.55)[k]
    # The ash puff: a dull grey-brown blot round the point, swelling and thinning.
    r = np.hypot(x, y * 1.2)
    puff_r = (5.0, 8.0, 10.0)[k]
    puff = np.exp(-(r / puff_r) ** 2) * (0.55, 0.4, 0.2)[k]
    img = over(img, premult_layer(hexlin('#6D665C'), np.clip(puff, 0, 1)))
    for i, (a, ln) in enumerate(zip(angles, lengths)):
        d = travel * ln
        px, py = d * np.cos(a), d * np.sin(a) - drop * ln
        size = (2.6, 2.2, 1.8)[k] * (0.7 + 0.5 * ln)
        spin = a + k * 0.9 + i
        u = (x - px) * np.cos(spin) + (y - py) * np.sin(spin)
        v = -(x - px) * np.sin(spin) + (y - py) * np.cos(spin)
        shard = smoothstep(1.0, 0.0, np.abs(u) / size + np.abs(v) / (size * 0.45))
        outline = smoothstep(1.0, 0.0, np.abs(u) / (size + 0.9) + np.abs(v) / (size * 0.45 + 0.9))
        img = over(img, premult_layer(hexlin('#120F0E'), np.clip(outline * fade, 0, 1)))
        lit = '#8F8174' if i % 3 else '#65574C'
        img = over(img, premult_layer(hexlin(lit), np.clip(shard * fade, 0, 1)))
    img = downsample(img, FX_SS)
    return finish(img[..., :3], img[..., 3:4]), (cx, h - cy)


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


def write_sprite(name, px, pivot, captain_name, textures, group):
    """The PNG and its .meta: the Captain's matching sprite's import settings, this sprite's GUID and pivot."""
    h, w = px.shape[:2]
    pivot = (int(pivot[0]), int(pivot[1]))
    if max(w, h) > ANDROID_MAX:
        sys.exit('%s is %dx%d: over the Android cap %d the Captain\'s import sets' % (name, w, h, ANDROID_MAX))
    if w % 4 or h % 4:
        sys.exit('%s is %dx%d: not whole ASTC blocks' % (name, w, h))
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
                         meta_from='CHR_Captain_%s.tga.meta' % captain_name, astc_4x4_bytes=astc_bytes(w, h),
                         group=group))
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
    pivots = {}           # sprite name -> pivot in texels (x, rows from the bottom)
    sizes = {}
    pivot = tuple(records[('front', 'idle')]['pivot'])

    def body(name, px, p, group, captain_name=None):
        captain_name = captain_name or name
        guids[name] = write_sprite(name, px, p, captain_name, textures, group)
        guids[name + '_Flash'] = write_sprite(name + '_Flash', flash_of(px), p, captain_name + '_Flash', textures,
                                              group)
        pivots[name], sizes[name] = p, px.shape[1::-1]

    # The walking bodies: one canvas a facing.
    walking, walk_pivots = {}, {}
    for facing in FACINGS:
        group = {'%s-%s' % (facing, pose): process(records[(facing, pose)], ss) for pose in WALKING}
        group, walk_pivots[facing] = fit(group, pivot)
        walking.update(group)
        for name, px in group.items():
            body(name, px, walk_pivots[facing], 'closed')
    # The blow (the windup and the strike): each cropped to what it shows.
    for facing in FACINGS:
        for pose in ('windup', 'strike'):
            r = records[(facing, pose)]
            px, p = tight(process(r, ss), tuple(r['pivot']))
            body('%s-%s' % (facing, pose), px, p, 'closed')
    # The open state, beside the set: each cropped, imported as the Captain's idle and its flash.
    for facing in FACINGS:
        for pose in OPEN_ORDER:
            r = records[(facing, pose)]
            px, p = tight(process(r, ss), tuple(r['pivot']))
            body('%s-%s' % (facing, pose), px, p, 'open', captain_name='%s-idle' % facing)
    # The own death, and the two remains.
    for facing in FACINGS:
        for k in range(DYING):
            r = records[(facing, 'dying-%d' % k)]
            px, p = tight(process(r, ss), tuple(r['pivot']))
            name = 'dying-%s-%d' % (facing, k)
            guids[name] = write_sprite(name, px, p, name, textures, 'death')
            pivots[name], sizes[name] = p, px.shape[1::-1]
    for name in ('death-collapse', 'death-cool'):
        r = records[('front', name)]
        px, p = tight(process(r, ss), tuple(r['pivot']))
        guids[name] = write_sprite(name, px, p, name, textures, 'death')
        pivots[name], sizes[name] = p, px.shape[1::-1]
    # The bark chip: not part of the set (the runtime draws it), imported as the Captain's idle is.
    effects = {}
    for k in range(3):
        px, p = bark_chip(k)
        effects['bark-chip-%d' % k] = write_sprite('bark-chip-%d' % k, px, p, 'front-idle', textures, 'effect')
    # The set's numbers from the art: the top of the walking poses (the branches included) and a little headroom, as
    # the Captain's - not the windup, whose maul rises over the bar for a beat; the shadow under the stumps and fists.
    top = 0
    for name, px in walking.items():
        ys, _ = np.nonzero(px[..., 3] > 128)
        top = max(top, (px.shape[0] - walk_pivots[name.split('-')[0]][1]) - ys.min())
    visual_top = round(top / PPU + 0.05, 2)
    shadow_width = 0.86
    # The art set: the Captain's own asset, line for line, each of its GUIDs swapped for this set's same-named sprite.
    captain_set = (CAPTAIN / 'GruntCaptainArtSet.asset').read_bytes().decode('utf-8')
    swap = {}
    for meta_path in CAPTAIN.glob('CHR_Captain_*.tga.meta'):
        name = meta_path.name[len('CHR_Captain_'):-len('.tga.meta')]
        swap[guid_of(meta_path)] = guids[name]
    text = re.sub(r'guid: ([0-9a-f]{32}), type: 3', lambda m: 'guid: %s, type: 3' % swap.get(m.group(1), m.group(1)),
                  captain_set)
    text = text.replace('m_Name: GruntCaptainArtSet', 'm_Name: %sArtSet' % STEM)
    text = re.sub(r'_visualTop: [\d.]+', '_visualTop: %s' % visual_top, text)
    text = re.sub(r'_shadowWidth: [\d.]+', '_shadowWidth: %s' % shadow_width, text)
    text = re.sub(r'_walkCycleDistance: [\d.]+', '_walkCycleDistance: %s' % round(meta['stride'], 2), text)
    text = re.sub(r'_frost: \{[^}]*\}', '_frost: {fileID: 0}', text)
    text = re.sub(r'_gloam: \{[^}]*\}', '_gloam: {fileID: 0}', text)
    sprite_refs = re.findall(r'fileID: 21300000, guid: ([0-9a-f]{32})', text)
    assert sprite_refs and all(g in guids.values() for g in sprite_refs) and 'GruntCaptain' not in text
    assert 'type: 2}' not in text, 'the set references no other set'
    set_path = OUT / ('%sArtSet.asset' % STEM)
    set_meta = Path(str(set_path) + '.meta')
    set_guid = existing_guid(set_meta)
    set_path.write_bytes(text.encode('utf-8'))
    captain_meta = (CAPTAIN / 'GruntCaptainArtSet.asset.meta').read_bytes().decode('utf-8')
    set_meta.write_bytes(re.sub(r'^guid: \w+', 'guid: ' + set_guid, captain_meta, count=1, flags=re.M).encode('utf-8'))
    by_group = {}
    for t in textures:
        by_group[t['group']] = by_group.get(t['group'], 0) + t['astc_4x4_bytes']
    total = sum(by_group.values())
    anchors = {'%s-%s' % (r['facing'], r['pose']): r['furnace_centre'] for r in meta['frames']
               if r['kind'] in ('body', 'open')}

    def entry(name):
        return dict(sprite='CHR_%s_%s.png' % (STEM, name), guid=guids[name],
                    flash='CHR_%s_%s_Flash.png' % (STEM, name), flash_guid=guids[name + '_Flash'],
                    size=list(sizes[name]), pivot=list(pivots[name]))

    open_state = dict(
        note='Not in CinderBloomArtSet: the runtime holds these itself (front and rear look left, mirrored for the '
             'right, as the set\'s). Play unfold-0, unfold-1 as the open window begins, hold open for the window, '
             'and play unfold-1 then unfold-0 as it ends, before the closed idle/walk resumes. The bloom is never '
             'red-orange: it does not hurt (docs/52 section 5).',
        unfold=['unfold-0', 'unfold-1'], hold='open', close=['unfold-1', 'unfold-0'],
        suggested_seconds=dict(transition_frame=0.08, close_lead=0.16),
        frames={facing: {pose: entry('%s-%s' % (facing, pose)) for pose in OPEN_ORDER} for facing in FACINGS})
    report = dict(art_set=dict(file=set_path.name, guid=set_guid), walk_body_size={f: list(walking['%s-idle' % f].shape[1::-1]) for f in FACINGS},
                  walk_body_pivot={f: list(walk_pivots[f]) for f in FACINGS}, visual_top=visual_top, shadow_width=shadow_width,
                  walk_cycle_distance=meta['stride'],
                  bloom=dict(value=BLOOM_VALUE, ramp=RAMP, orchid=ORCHID, glow_inside=GLOW_INSIDE, halo=GLOW_HALO),
                  open_state=open_state,
                  bark_chip=dict(frames=['CHR_%s_bark-chip-%d.png' % (STEM, k) for k in range(3)], guids=effects,
                                 note='Drawn as seen, pivot the struck point; tint the closed body\'s white flash '
                                      'toward bark (#65574C) instead of full white while it plays.'),
                  furnace_anchor_world_units=anchors,
                  textures=textures, texture_count=len(textures), astc_4x4_bytes=total, astc_4x4_bytes_by_group=by_group)
    DOCS.mkdir(parents=True, exist_ok=True)
    (DOCS / 'sprites.json').write_text(json.dumps(report, indent=1, default=lambda o: o.item()))
    print('BLOOM_PACKED walk %dx%d pivot %s visualTop %s textures %d ASTC4x4 %d bytes %s set %s'
          % (walking['front-idle'].shape[1], walking['front-idle'].shape[0], walk_pivots, visual_top, len(textures),
             total, by_group, set_guid))


if __name__ == '__main__':
    main()
