"""Packs floor 3's two horde creatures (BuildBlenderGloamHorde.py) into the game's sprites and their EnemyArtSets, the
Thornling's dive, eruption and stuck drawings beside its set, and draws their pooled effects (docs/52 sections 3-4).

    python Tools/ArtReview/PackGloamHordeSprites.py [--creature wraith|thornling|both] [--render DIR] [--docs DIR]

(python -I hides the user site; run it as `python -I -c "import sys, runpy; sys.path.append(<user site-packages>);
sys.argv = ['PackGloamHordeSprites.py']; runpy.run_path('Tools/ArtReview/PackGloamHordeSprites.py',
run_name='__main__')"` when numpy lives there.)

Per frame, as PackCinderBloomSprites.py: the 4x render box-filtered down in linear light with premultiplied alpha (the
floor pivot on a texel corner); the aux family laid in by coverage (the Wraith's seed core on a plum ramp with a wet
glint, the Thornling's thorn tips and eyes on the orchid ramp) at the frame's own value (the dying dim); its glow
bloomed softly inside the figure; a one-texel dark outline. The Wraith wears a faint pale-orchid halo (a luminous mote,
read at a glance in a crowd); its dying drawings shed pollen motes and grey as the husk spends; the Thornling's
drawings under the ash get a ring of disturbed ash where the body meets the floor, its far lip behind the body and its
near lip over the cut. Nothing warm and nothing chartreuse is laid on either creature (docs/52 section 2 rule 3). The
only red-orange anywhere is the Thornling ring's warning colouring (#FF5A3C), an effect, never a body.

THE POLLEN WRAITH keeps the Cinder Mite's contract frame for frame: idle, step A, step B, strike a facing with their
white flashes, the strike windup (front, rear) with flashes, the two remains, its own death of seven drawings a facing
listed nine steps long at the Mite's 0.06 s. A facing's five bodies share one canvas (whole ASTC blocks, the pivot where
it hovers over the floor). CinderMiteArtSet.asset is the set's template, line for line, every GUID swapped for this
set's same-named sprite, `_frost` and `_gloam` empty (born on floor 3), its visual top, shadow and drift cycle written
from the art. Effects: three pollen motes (clinging to a still hero's feet) and a four-frame pollen bloom (the burst at
the hero's feet when pollen blooms).

THE THORNLING keeps the Runner's contract frame for frame (six bodies a facing, the strike windup, the two remains, its
own death of five drawings a facing listed ten steps long), RunnerArtSet.asset its template. Beside the set, each with
its flash: dive-0..2 (gathering, head first, nearly gone), erupt-0..2 (breaking the ash, half out, airborne) and
stuck-0..1 (claws caught, heaving) in each facing. Effects: the ash puff of a dive (3 frames), the thorn burst of an
eruption (3 frames) and the ring under the hero as a radial pair - its edge and its fill, each in orchid (neutral, the
ring's first part) and in warning red-orange (its last second) - drawn at a radius of one world unit (scale them by
the ring's radius: 1.2 for the Thornling, 0.8 for the pollen meter).

Writes, all regenerated on every run (existing GUIDs kept, new files get fresh uuid4 GUIDs):
    Assets/_Project/Art/Characters/PollenWraith/CHR_PollenWraith_<frame>.png (+ _Flash.png) and their .meta files
        (CHR_Mite_front_windup.tga.meta's import settings exactly; this sprite's GUID and pivot), PollenWraithArtSet.asset
    Assets/_Project/Art/Characters/Thornling/CHR_Thornling_<frame>.png (+ _Flash.png) and their .meta files
        (CHR_Runner_front-idle.tga.meta's import settings), ThornlingArtSet.asset
    <docs>/pollen-wraith/sprites.json and <docs>/thornling/sprites.json: every texture's size, pivot, GUID and ASTC 4x4
        bytes, the set's values, the extra drawings and effects in play order with suggested timings
        (default E:/MobileGame/ArtDirection/2026-11-15/gloam-horde-01, the main checkout's, untracked)
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


RENDER = Path(arg('--render', str(ROOT / 'TestResults/gloam-horde-render')))
DOCS = Path(arg('--docs', 'E:/MobileGame/ArtDirection/2026-11-15/gloam-horde-01'))
WHICH = arg('--creature', 'both')
CHARS = ROOT / 'Assets/_Project/Art/Characters'
PPU = 128
MARGIN = 2
ANDROID_MAX = 256
FX_SS = 4

# The orchid ramp (docs/51's enemy palette, the Bloom's): a thorn's shaded base deep plum, its lit tip pale orchid.
ORCHID_RAMP = [(0.0, '#3A1046'), (0.24, '#5A1F66'), (0.46, '#B850B8'), (0.6, '#D86BD0'), (0.78, '#F2A8EC'),
               (1.0, '#FDE6FA')]
# The seed core: a dark plum seed (docs/52 #5A1F66) with a wet pale glint.
CORE_RAMP = [(0.0, '#1E0726'), (0.22, '#3E1448'), (0.34, '#5A1F66'), (0.55, '#7E3088'), (0.78, '#E7A6E0'),
             (1.0, '#FFF0FC')]

CREATURES = {
    'wraith': dict(
        stem='PollenWraith', folder='PollenWraith', docs='pollen-wraith',
        template_dir='CinderMite', template_set='CinderMiteArtSet.asset', template_meta='CHR_Mite_front_windup.tga.meta',
        ramp=CORE_RAMP, tone_lift=0.0, value=1.0,
        glow_colour='#F6C2F0', glow_inside=((1.4, 0.55), (3.0, 0.3)), inside_cap=0.6,
        halo=(1.6, 0.9, 0.3), halo_colour='#E9A8E2',
        outline=(0x1C, 0x10, 0x20),
        walking=['idle', 'step-a', 'step-b', 'strike', 'windup'], tight=[],
        extras=[], dying=7, remains=['death-husk', 'death-dust'],
        shadow_width=0.34),
    'thornling': dict(
        stem='Thornling', folder='Thornling', docs='thornling',
        template_dir='Runner', template_set='RunnerArtSet.asset', template_meta='CHR_Runner_front-idle.tga.meta',
        ramp=ORCHID_RAMP, tone_lift=0.05, value=1.0,
        glow_colour='#D86BD0', glow_inside=((1.0, 0.45),), inside_cap=0.5,
        halo=None, halo_colour=None,
        outline=(0x12, 0x0F, 0x0E),
        walking=['idle', 'step-a', 'step-b', 'pass-a', 'pass-b'], tight=['strike', 'windup'],
        extras=['dive-0', 'dive-1', 'dive-2', 'erupt-0', 'erupt-1', 'erupt-2', 'stuck-0', 'stuck-1'],
        dying=5, remains=['death-collapse', 'death-cool'],
        shadow_width=0.5),
}
FACINGS = ['front', 'rear']
# The Thornling's drawings mostly under the ash, where it cannot be struck (docs/52 section 4): no flash.
UNDER_ASH = {'dive-1', 'dive-2', 'erupt-0'}


def to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def hexlin(h):
    h = h.lstrip('#')
    return to_linear(np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)]) / 255.0)


def blur(field, sigma):
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


def ramp(stops_hex, tone):
    stops = np.array([s for s, _ in stops_hex])
    cols = np.array([hexlin(h) for _, h in stops_hex])
    t = np.clip(tone, 0, 1)
    out = np.zeros(t.shape + (3,))
    for c in range(3):
        out[..., c] = np.interp(t, stops, cols[:, c])
    return out


def load_premultiplied(path):
    px = np.asarray(Image.open(path).convert('RGBA')).astype(np.float64) / 255.0
    alpha = px[..., 3:4]
    return np.concatenate([to_linear(px[..., :3]) * alpha, alpha], axis=-1)


def over(dst, src):
    return src + dst * (1 - src[..., 3:4])


def layer(colour_lin, alpha):
    return np.concatenate([colour_lin * alpha[..., None], alpha[..., None]], axis=-1)


def outline_of(alpha):
    pad = np.pad(alpha, 1)
    return np.maximum.reduce([pad[1:-1, 1:-1], pad[:-2, 1:-1], pad[2:, 1:-1], pad[1:-1, :-2], pad[1:-1, 2:],
                              0.7 * pad[:-2, :-2], 0.7 * pad[2:, 2:], 0.7 * pad[:-2, 2:], 0.7 * pad[2:, :-2]])


def finish(lin, alpha):
    straight = np.where(alpha > 1e-6, lin / np.maximum(alpha, 1e-6), 0)
    out = np.concatenate([to_srgb(straight), np.clip(alpha, 0, 1)], axis=-1)
    out = (out * 255 + 0.5).astype(np.uint8)
    out[out[..., 3] < 2] = 0
    return out


# ---------------------------------------------------------------------------------------------------------------------
# Painted additions: the Wraith's shed pollen, the Thornling's disturbed ash
# ---------------------------------------------------------------------------------------------------------------------
def motes(shape, centre_px, k, count, spread, rise, rng_seed, colour='#F2B6EC'):
    """Pollen motes shed by a bursting Wraith: soft pale-orchid dots thrown out round the body centre (texels), on a
    dark one-texel ring so they hold on light loam too."""
    h, w = shape
    ss = FX_SS
    ys, xs = np.mgrid[0:h * ss, 0:w * ss]
    x = (xs + 0.5) / ss
    y = (ys + 0.5) / ss
    rng = np.random.default_rng(rng_seed)
    img = np.zeros((h * ss, w * ss, 4))
    for i in range(count):
        a = rng.uniform(0, 2 * np.pi)
        r = spread * rng.uniform(0.45, 1.0)
        px = centre_px[0] + r * np.cos(a)
        py = centre_px[1] - r * 0.7 * np.sin(a) - rise * rng.uniform(0.3, 1.0)
        size = rng.uniform(1.0, 1.6)
        d = np.hypot(x - px, y - py)
        dark = smoothstep(size + 1.0, size + 0.2, d)
        img = over(img, layer(hexlin('#2A1230'), dark * 0.7))
        dot = smoothstep(size, size * 0.3, d)
        img = over(img, layer(hexlin(colour), dot))
    return downsample(img, ss)


def ash_layers(shape, points_px, k_kind):
    """A ring of disturbed ash round where the body meets the floor: (under, over) premultiplied layers - the hole and
    the far lip under the body, the near lip over its cut. points_px: the floor crossing in texels (x, y down)."""
    h, w = shape
    pts = np.array(points_px)
    cx, cy = pts[:, 0].mean(), pts[:, 1].mean()
    half_w = max(10.0, (pts[:, 0].max() - pts[:, 0].min()) / 2 + 6)
    half_h = max(half_w * 0.5, (pts[:, 1].max() - pts[:, 1].min()) / 2 + 4)
    half_w = max(half_w, half_h * 2 * 0.85)
    ss = FX_SS
    ys, xs = np.mgrid[0:h * ss, 0:w * ss]
    x = ((xs + 0.5) / ss - cx) / half_w
    y = ((ys + 0.5) / ss - cy) / half_h
    r = np.hypot(x, y)
    ang = np.arctan2(y, x)
    lump = 1 + 0.12 * np.sin(5 * ang + 1.3) + 0.07 * np.sin(11 * ang + 0.4)
    lip_in, lip_out = 0.88, 1.2 * lump
    hole = smoothstep(0.95, 0.7, r)
    lip = smoothstep(lip_in - 0.08, lip_in + 0.05, r) * smoothstep(lip_out + 0.05, lip_out - 0.12, r)
    rim = smoothstep(lip_out + 0.18, lip_out + 0.03, r) * (1 - smoothstep(lip_out - 0.05, lip_out + 0.03, r) * 0) - lip
    rim = np.clip(rim, 0, 1)
    # The lip lit from the upper left (the key), shaded low right; a few clods thrown out.
    shade = np.clip(0.55 - 0.45 * (x * -0.55 + y * -0.8) / np.maximum(r, 1e-3), 0, 1)
    lip_col = (1 - shade)[..., None] * hexlin('#77746A') + shade[..., None] * hexlin('#3B3934')
    under = np.zeros((h * ss, w * ss, 4))
    under = over(under, layer(hexlin('#1F1E1A'), hole * 0.92))
    far = (y < 0.05).astype(float)
    near = 1 - far
    lip_layer = np.concatenate([lip_col * lip[..., None], lip[..., None]], axis=-1)
    rim_layer = layer(hexlin('#141210'), rim * 0.9)
    under = over(under, rim_layer * far[..., None])
    under = over(under, lip_layer * far[..., None])
    over_l = np.zeros((h * ss, w * ss, 4))
    over_l = over(over_l, rim_layer * near[..., None])
    over_l = over(over_l, lip_layer * near[..., None])
    return downsample(under, ss), downsample(over_l, ss)


# ---------------------------------------------------------------------------------------------------------------------
# One frame
# ---------------------------------------------------------------------------------------------------------------------
def process(cfg, render, record, ss, creature):
    """One frame at the game's resolution: (straight-alpha sRGB uint8, the body's alpha for its flash)."""
    beauty = load_premultiplied(render / record['file'])
    if max(beauty[0, :, 3].max(), beauty[-1, :, 3].max(), beauty[:, 0, 3].max(), beauty[:, -1, 3].max()) > 0:
        sys.exit('%s touches the edge of its render canvas: widen it in BuildBlenderGloamHorde.py' % record['file'])
    aux = downsample(load_premultiplied(render / record['aux']), ss)
    beauty = downsample(beauty, ss)
    lin, alpha = beauty[..., :3], beauty[..., 3:4]
    m = np.clip(aux[..., 0], 0, 1)
    tone = np.where(m > 1e-4, aux[..., 1] / np.maximum(m, 1e-4), 0)
    value = cfg['value'] * record['glow']
    lin = lin + ramp(cfg['ramp'], tone + cfg['tone_lift'] * (m > 1e-4)) * (m * min(1.0, value))[..., None]
    glow = aux[..., 2] * value
    core_glow = glow * np.clip(m * 4, 0, 1)
    light = sum(blur(core_glow, sigma) * gain for sigma, gain in cfg['glow_inside'])[..., None]
    lin = lin + hexlin(cfg['glow_colour']) * np.clip(light, 0, cfg['inside_cap']) * alpha * (1 - m[..., None])
    if record.get('ash', 0) > 0:
        # A spent husk greys toward ash.
        lum = (lin @ np.array([0.2126, 0.7152, 0.0722]))[..., None]
        grey = lum * hexlin('#B4AEA6') / hexlin('#B4AEA6').mean()
        lin = lin + (grey * 0.85 - lin) * record['ash']
    h, w = alpha.shape[:2]
    pivot = record['pivot']
    if creature == 'wraith' and record['kind'] == 'dying':
        k = int(record['pose'].split('-')[1])
        if 1 <= k <= 5:
            bc = record['body_centre']
            centre = (pivot[0] + bc[0] * PPU, h - pivot[1] - bc[1] * PPU)
            shed = motes((h, w), centre, k, (6, 9, 9, 7, 4)[k - 1], (12, 20, 27, 32, 35)[k - 1],
                         (0, 4, 9, 14, 18)[k - 1], 31 + k)
            fade = (1.0, 0.95, 0.8, 0.6, 0.4)[k - 1]
            body = np.concatenate([lin, alpha], axis=-1)
            body = over(shed * fade, body)
            lin, alpha = body[..., :3], body[..., 3:4]
    grown = outline_of(alpha[..., 0])[..., None]
    lin = lin + hexlin('#%02X%02X%02X' % cfg['outline']) * grown * (1 - alpha)
    alpha = alpha + grown * (1 - alpha)
    body_alpha = alpha[..., 0].copy()
    if cfg['halo']:
        sigma, gain, cap = cfg['halo']
        field = blur(np.clip(alpha[..., 0] * (0.4 + 0.6 * np.clip(glow * 3, 0, 1)), 0, 1), sigma)
        halo = np.clip(field * gain, 0, cap)[..., None] * (1 - alpha) * min(1.0, record['glow'])
        lin = lin + hexlin(cfg['halo_colour']) * halo
        alpha = alpha + halo
    if record.get('sink') and record.get('floor_points'):
        pts = [(pivot[0] + x * PPU, h - pivot[1] - y * PPU) for x, y in record['floor_points']]
        under, over_l = ash_layers((h, w), pts, record['pose'])
        body = np.concatenate([lin, alpha], axis=-1)
        body = over(under, body)
        body = over(body, over_l)
        lin, alpha = body[..., :3], body[..., 3:4]
        body_alpha = np.maximum(body_alpha, 0)        # the flash covers the creature, not the ash
    return finish(lin, alpha), body_alpha


def flash_of(px, body_alpha):
    out = np.full_like(px, 255)
    out[..., 3] = np.clip(body_alpha * 255 + 0.5, 0, 255).astype(np.uint8)
    out[out[..., 3] < 2, 3] = 0
    return out


def fit(frames, pivot):
    """One canvas for a group of frames (each (px, body_alpha)): their texels and a margin about the pivot, whole ASTC
    blocks. pivot is (x, rows from the bottom)."""
    h = next(iter(frames.values()))[0].shape[0]
    left = right = top = bottom = 0
    for px, _ in frames.values():
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
    for name, (px, ba) in frames.items():
        out[name] = (px[y0:y0 + height, x0:x0 + width].copy(), ba[y0:y0 + height, x0:x0 + width].copy())
    return out, (int(left), int(down))


def tight(px, ba, pivot):
    # A few transparent texels round the canvas first, so the block rounding never runs off it.
    pad = 8
    px = np.pad(px, ((pad, pad), (pad, pad), (0, 0)))
    ba = np.pad(ba, pad)
    pivot = (pivot[0] + pad, pivot[1] + pad)
    h, w = px.shape[:2]
    prow = h - pivot[1]
    y, x = np.nonzero(px[..., 3])
    x0, x1 = min(x.min(), pivot[0]) - MARGIN, max(x.max() + 1, pivot[0]) + MARGIN
    y0, y1 = min(y.min(), prow) - MARGIN, max(y.max() + 1, prow) + MARGIN
    x0 -= (-(x1 - x0)) % 4
    y0 -= (-(y1 - y0)) % 4
    if x0 < 0 or y0 < 0 or x1 > w or y1 > h:
        sys.exit('a drawing runs off its canvas')
    return px[y0:y1, x0:x1], ba[y0:y1, x0:x1], (pivot[0] - x0, y1 - prow)


# ---------------------------------------------------------------------------------------------------------------------
# Effects, drawn at 4x and box-filtered down
# ---------------------------------------------------------------------------------------------------------------------
def canvas(w, h):
    ys, xs = np.mgrid[0:h * FX_SS, 0:w * FX_SS]
    return (xs + 0.5) / FX_SS, (ys + 0.5) / FX_SS, np.zeros((h * FX_SS, w * FX_SS, 4))


def fx_finish(img):
    img = downsample(img, FX_SS)
    return finish(img[..., :3], img[..., 3:4])


def shard(x, y, px, py, angle, length, width):
    """A lanceolate shard (a petal, a thorn): its base at (px, py), pointing along angle (screen, y down)."""
    u = (x - px) * np.cos(angle) + (y - py) * np.sin(angle)
    v = -(x - px) * np.sin(angle) + (y - py) * np.cos(angle)
    s = np.clip(u / max(length, 1e-3), 0, 1)
    half = width * np.sin(np.pi * np.clip(s, 0, 1) ** 0.7) * (1 - s * 0.2)
    inside = (u > 0) & (u < length)
    return np.where(inside, smoothstep(0.0, -0.6, np.abs(v) - half), 0.0), s


def pollen_mote(k):
    """A pollen mote clinging to a still hero's feet (pooled, scattered at the feet, rising and fading): one, two or
    three soft pale-orchid motes, each on a dark plum ring so it reads on light and dark loam alike."""
    w = h = 16
    x, y, img = canvas(w, h)
    spots = [[(8, 8, 2.2)], [(6, 9, 1.9), (10.5, 6.5, 1.5)], [(5.5, 9.5, 1.6), (10, 9, 1.8), (8, 5, 1.4)]][k]
    for px, py, r in spots:
        d = np.hypot(x - px, y - py)
        img = over(img, layer(hexlin('#2A1230'), smoothstep(r + 1.0, r + 0.25, d) * 0.75))
        img = over(img, layer(hexlin('#D886D2'), smoothstep(r, r * 0.45, d)))
        img = over(img, layer(hexlin('#FBE0F7'), smoothstep(r * 0.55, 0.0, d)))
    return fx_finish(img), (8, 8)


def pollen_bloom(k):
    """The bloom of pollen at the hero's feet (docs/52 section 3: a short orchid burst, then the usual hit flash): a ring
    of orchid petals bursting out over the floor (2:1) and pollen thrown up, four frames - burst, open, scatter, fade."""
    w, h = 128, 96
    cx, cy = 64, 70
    x, y, img = canvas(w, h)
    rng = np.random.default_rng(5)
    reach = (14, 30, 42, 48)[k]
    fade = (1.0, 1.0, 0.75, 0.4)[k]
    # A soft orchid light on the floor under the burst.
    d = np.hypot((x - cx) / 1.0, (y - cy) * 2.0)
    glow = np.exp(-(d / (reach * 0.9)) ** 2) * (0.55, 0.45, 0.25, 0.1)[k]
    img = over(img, layer(hexlin('#D86BD0'), np.clip(glow, 0, 1)))
    # Petals: twelve, thrown radially over the floor, tumbling.
    for i in range(12):
        a = 2 * np.pi * i / 12 + rng.uniform(-0.15, 0.15)
        r = reach * rng.uniform(0.7, 1.0)
        px, py = cx + r * np.cos(a), cy + 0.5 * r * np.sin(a) - (0, 4, 7, 6)[k] * rng.uniform(0.4, 1.0)
        ang = np.arctan2(0.5 * np.sin(a), np.cos(a)) + (0, 0.5, 1.2, 2.0)[k] * (1 if i % 2 else -1)
        length = (7, 9, 8, 6)[k] * rng.uniform(0.8, 1.15)
        sh, s = shard(x, y, px - length * 0.5 * np.cos(ang), py - length * 0.5 * np.sin(ang), ang, length, 2.6)
        outline, _ = shard(x, y, px - (length + 1.4) * 0.5 * np.cos(ang), py - (length + 1.4) * 0.5 * np.sin(ang), ang,
                           length + 1.4, 3.5)
        img = over(img, layer(hexlin('#1C1020'), outline * fade * 0.9))
        col = (1 - s)[..., None] * hexlin('#C25EBE') + s[..., None] * hexlin('#F6C2F0')
        img = over(img, np.concatenate([col * (sh * fade)[..., None], (sh * fade)[..., None]], axis=-1))
    # Pollen thrown up.
    for i in range(16):
        a = rng.uniform(0, 2 * np.pi)
        r = reach * rng.uniform(0.2, 0.9)
        px = cx + r * np.cos(a)
        py = cy + 0.5 * r * np.sin(a) - (6, 16, 28, 38)[k] * rng.uniform(0.3, 1.0)
        size = rng.uniform(1.1, 1.8)
        dd = np.hypot(x - px, y - py)
        img = over(img, layer(hexlin('#2A1230'), smoothstep(size + 0.9, size + 0.2, dd) * 0.6 * fade))
        img = over(img, layer(hexlin('#F8D2F2'), smoothstep(size, size * 0.3, dd) * fade))
    return fx_finish(img), (cx, h - cy)


def ash_puff(k):
    """The ash a diving Thornling throws (harmless: grey, not red-orange and not orchid-bright): a low cloud of ash
    blots swelling and thinning over three frames, a few clods kicked out."""
    w, h = 96, 64
    cx, cy = 48, 46
    x, y, img = canvas(w, h)
    rng = np.random.default_rng(11)
    spread = (14, 22, 28)[k]
    fade = (1.0, 0.85, 0.5)[k]
    for i in range(9):
        a = rng.uniform(0, 2 * np.pi)
        r = spread * rng.uniform(0.2, 0.9)
        px, py = cx + r * np.cos(a), cy + 0.45 * r * np.sin(a) - (3, 8, 12)[k] * rng.uniform(0.3, 1.0)
        size = (8, 11, 13)[k] * rng.uniform(0.8, 1.15)
        d = np.hypot(x - px, (y - py) * 1.25)
        blot = np.exp(-(d / size) ** 2.2) * fade * 1.15
        lit = np.clip((-(x - px) - (y - py)) / size * 0.5 + 0.5, 0, 1)
        col = lit[..., None] * hexlin('#9A978A') + (1 - lit)[..., None] * hexlin('#5E5B52')
        img = over(img, np.concatenate([col * np.clip(blot, 0, 0.85)[..., None], np.clip(blot, 0, 0.85)[..., None]], axis=-1))
    for i in range(6):
        a = rng.uniform(np.pi, 2 * np.pi)
        r = spread * (1.1, 1.4, 1.5)[k] * rng.uniform(0.7, 1.0)
        px, py = cx + r * np.cos(a), cy + 0.5 * r * np.sin(a) + (0, 3, 8)[k]
        size = rng.uniform(1.4, 2.2)
        d = np.hypot(x - px, y - py)
        img = over(img, layer(hexlin('#141210'), smoothstep(size + 0.9, size + 0.2, d) * fade))
        img = over(img, layer(hexlin('#6E6A5E'), smoothstep(size, size * 0.3, d) * fade))
    return fx_finish(img), (cx, h - cy)


def thorn_burst(k):
    """The eruption's burst (docs/52: a burst of dark thorns with orchid tips): a crown of thorns breaking up through
    the ash round a point on the floor, clods flung out - burst, full, breaking."""
    w, h = 128, 112
    cx, cy = 64, 86
    x, y, img = canvas(w, h)
    rng = np.random.default_rng(23)
    grow = (0.55, 1.0, 0.8)[k]
    fade = (1.0, 1.0, 0.7)[k]
    # The broken ash round the point.
    d = np.hypot(x - cx, (y - cy) * 2.0)
    ring = smoothstep(26 * (0.7 + 0.3 * grow), 18 * (0.7 + 0.3 * grow), d) * smoothstep(8, 16, d) * 0.85 * fade
    img = over(img, layer(hexlin('#5E5B52'), ring))
    img = over(img, layer(hexlin('#1F1E1A'), smoothstep(12, 6, d) * 0.85 * fade))
    thorns = []
    for i in range(9):
        a = np.pi + np.pi * (i + 0.5) / 9 + rng.uniform(-0.12, 0.12)       # fanned upward (screen y down)
        length = rng.uniform(26, 40) * grow * (0.75 if i in (0, 8) else 1.0)
        base_r = rng.uniform(4, 9)
        bx, by = cx + base_r * np.cos(a) * 1.4, cy + base_r * 0.5 * np.sin(a) * 0.5
        thorns.append((by, bx, a, length * 1.1, rng.uniform(2.6, 3.4)))
    for by, bx, a, length, width in sorted(thorns):
        ang = a + (0, 0, 0.25 if bx > cx else -0.25)[k]
        out_, _ = shard(x, y, bx - 1.0 * np.cos(ang), by - 1.0 * np.sin(ang), ang, length + 2.0, width + 1.4)
        img = over(img, layer(hexlin('#120F0E'), out_ * fade))
        sh, s = shard(x, y, bx, by, ang, length, width)
        tip = smoothstep(0.62, 0.8, s)
        lit = np.clip(0.5 + 0.5 * np.sin(ang + np.pi / 2), 0, 1)
        bark = hexlin('#3F322B') * (0.8 + 0.5 * lit)
        col = (1 - tip)[..., None] * bark + tip[..., None] * ramp(ORCHID_RAMP, 0.45 + 0.5 * s)
        img = over(img, np.concatenate([col * (sh * fade)[..., None], (sh * fade)[..., None]], axis=-1))
    for i in range(8):
        a = rng.uniform(np.pi * 1.05, np.pi * 1.95)
        r = (14, 30, 40)[k] * rng.uniform(0.6, 1.0)
        px, py = cx + r * np.cos(a) * 1.3, cy + r * np.sin(a) + (0, 0, 10)[k]
        size = rng.uniform(1.6, 2.6)
        dd = np.hypot(x - px, y - py)
        img = over(img, layer(hexlin('#141210'), smoothstep(size + 1.0, size + 0.2, dd) * fade))
        img = over(img, layer(hexlin('#77736A'), smoothstep(size, size * 0.3, dd) * fade))
    return fx_finish(img), (cx, h - cy)


def ring_edge(colouring):
    """The ring's edge at a radius of one world unit on the 2:1 floor (128 texels a unit): a bright band with a dark
    keyline outside and a soft glow inward. Orchid (#D86BD0, the ring's first part) or warning (#FF5A3C, its last
    second)."""
    w, h = 256, 128
    cx, cy = 128, 64
    x, y, img = canvas(w, h)
    r = np.hypot((x - cx) / 126.0, (y - cy) / 63.0)
    main, light = {'orchid': ('#D86BD0', '#F6C2F0'), 'warning': ('#FF5A3C', '#FFC2A8')}[colouring]
    inner_glow = np.clip(smoothstep(0.7, 0.985, r) * (r < 0.985), 0, 1) * 0.32
    img = over(img, layer(hexlin(main), inner_glow))
    band = smoothstep(0.935, 0.955, r) * smoothstep(1.0, 0.982, r)
    key = smoothstep(0.99, 0.996, r) * smoothstep(1.0, 0.998, r * 0.997)
    img = over(img, layer(hexlin('#1A0E10'), np.clip(key, 0, 1) * 0.8))
    img = over(img, layer(hexlin(main), band))
    core = smoothstep(0.952, 0.962, r) * smoothstep(0.978, 0.968, r)
    img = over(img, layer(hexlin(light), core * 0.85))
    return fx_finish(img), (cx, h - cy)


def ring_fill(colouring):
    """The ring's fill (scaled from its centre 0..1 as the telegraph runs, as the Warden mark's fill grows): a soft disc
    brightening to its rim, at 64 texels a unit (a soft gradient loses nothing at half the density)."""
    w, h = 128, 64
    cx, cy = 64, 32
    x, y, img = canvas(w, h)
    r = np.hypot((x - cx) / 62.5, (y - cy) / 31.25)
    main = {'orchid': '#B850B8', 'warning': '#FF5A3C'}[colouring]
    a = (0.28 + 0.45 * smoothstep(0.3, 0.98, r)) * smoothstep(1.0, 0.975, r)
    img = over(img, layer(hexlin(main), a))
    return fx_finish(img), (cx, h - cy)


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


class Writer:
    def __init__(self, cfg):
        self.cfg = cfg
        self.out = CHARS / cfg['folder']
        self.template = (CHARS / cfg['template_dir'] / cfg['template_meta']).read_bytes().decode('utf-8')
        self.textures = []
        self.guids = {}
        self.pivots = {}
        self.sizes = {}

    def sprite(self, name, px, pivot, group, ppu=PPU):
        h, w = px.shape[:2]
        pivot = (int(pivot[0]), int(pivot[1]))
        if max(w, h) > ANDROID_MAX:
            sys.exit('%s is %dx%d: over the Android cap %d' % (name, w, h, ANDROID_MAX))
        if w % 4 or h % 4:
            sys.exit('%s is %dx%d: not whole ASTC blocks' % (name, w, h))
        path = self.out / ('CHR_%s_%s.png' % (self.cfg['stem'], name))
        meta = Path(str(path) + '.meta')
        guid = existing_guid(meta)
        Image.fromarray(px, 'RGBA').save(path, optimize=True)
        text = re.sub(r'^guid: \w+', 'guid: ' + guid, self.template, count=1, flags=re.M)
        pivot_norm = (pivot[0] / w, pivot[1] / h)
        text, n = re.subn(r'spritePivot: \{x: [^,]+, y: [^}]+\}', 'spritePivot: {x: %.8f, y: %.8f}' % pivot_norm, text,
                          count=1)
        assert n == 1
        if ppu != PPU:
            text, n = re.subn(r'spritePixelsToUnits: \d+', 'spritePixelsToUnits: %d' % ppu, text, count=1)
            assert n == 1
        assert 'sprites: []' in text and 'internalIDToNameTable: []' in text, 'a clean single-sprite template'
        meta.write_bytes(text.encode('utf-8'))
        self.textures.append(dict(file=path.name, guid=guid, size=[w, h], pivot=list(pivot),
                                  pivot_normalised=[round(pivot_norm[0], 8), round(pivot_norm[1], 8)],
                                  pixels_per_unit=ppu, astc_4x4_bytes=astc_bytes(w, h), group=group))
        self.guids[name], self.pivots[name], self.sizes[name] = guid, pivot, [w, h]
        return guid

    def body(self, name, px, ba, pivot, group):
        self.sprite(name, px, pivot, group)
        self.sprite(name + '_Flash', flash_of(px, ba), pivot, group)

    def entry(self, name, flash=True):
        e = dict(sprite='CHR_%s_%s.png' % (self.cfg['stem'], name), guid=self.guids[name], size=self.sizes[name],
                 pivot=list(self.pivots[name]))
        if flash:
            e.update(flash='CHR_%s_%s_Flash.png' % (self.cfg['stem'], name), flash_guid=self.guids[name + '_Flash'])
        return e


def template_name_map(creature):
    """The template set's sprite names (its files' stems after CHR_<x>_) to this set's names."""
    if creature == 'wraith':
        out = {}
        for facing in FACINGS:
            for pose in ('idle', 'step-a', 'step-b', 'strike', 'windup'):
                out['%s_%s' % (facing, pose)] = '%s-%s' % (facing, pose)
                out['%s_%s_Flash' % (facing, pose)] = '%s-%s_Flash' % (facing, pose)
            for k in range(7):
                out['dying_%s_%d' % (facing, k)] = 'dying-%s-%d' % (facing, k)
        out['death_break'] = 'death-husk'
        out['death_embers'] = 'death-dust'
        return 'CHR_Mite_', '.tga.meta', out
    return 'CHR_Runner_', '.tga.meta', None


def floor_points(record):
    return record.get('floor_points')


def pack(creature):
    cfg = CREATURES[creature]
    render = RENDER / creature
    meta = json.loads((render / 'frames.json').read_text())
    ss = meta['supersample']
    records = {(r['facing'], r['pose']): r for r in meta['frames']}
    w = Writer(cfg)
    w.out.mkdir(parents=True, exist_ok=True)
    folder_meta = w.out.parent / (w.out.name + '.meta')
    if not folder_meta.exists():
        folder_meta.write_text('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                               '  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex,
                               encoding='utf-8', newline='\n')
    pivot = tuple(records[('front', 'idle')]['pivot'])
    walking, walk_pivots = {}, {}
    for facing in FACINGS:
        group = {'%s-%s' % (facing, pose): process(cfg, render, records[(facing, pose)], ss, creature)
                 for pose in cfg['walking']}
        group, walk_pivots[facing] = fit(group, pivot)
        walking.update(group)
        for name, (px, ba) in group.items():
            w.body(name, px, ba, walk_pivots[facing], 'body')
    for facing in FACINGS:
        for pose in cfg['tight']:
            r = records[(facing, pose)]
            px, ba = process(cfg, render, r, ss, creature)
            px, ba, p = tight(px, ba, tuple(r['pivot']))
            w.body('%s-%s' % (facing, pose), px, ba, p, 'body')
    for facing in FACINGS:
        for pose in cfg['extras']:
            r = records[(facing, pose)]
            px, ba = process(cfg, render, r, ss, creature)
            px, ba, p = tight(px, ba, tuple(r['pivot']))
            if pose in UNDER_ASH:
                w.sprite('%s-%s' % (facing, pose), px, p, 'extra')
            else:
                w.body('%s-%s' % (facing, pose), px, ba, p, 'extra')
    for facing in FACINGS:
        for k in range(cfg['dying']):
            r = records[(facing, 'dying-%d' % k)]
            px, ba = process(cfg, render, r, ss, creature)
            px, _, p = tight(px, ba, tuple(r['pivot']))
            w.sprite('dying-%s-%d' % (facing, k), px, p, 'death')
    for name in cfg['remains']:
        r = records[('front', name)]
        px, ba = process(cfg, render, r, ss, creature)
        px, _, p = tight(px, ba, tuple(r['pivot']))
        w.sprite(name, px, p, 'death')
    # Effects, outside the set (the runtime pools them).
    effects = {}
    if creature == 'wraith':
        draws = [('pollen-mote-%d' % k, pollen_mote, k, PPU) for k in range(3)] + \
                [('pollen-bloom-%d' % k, pollen_bloom, k, PPU) for k in range(4)]
    else:
        draws = [('ash-puff-%d' % k, ash_puff, k, PPU) for k in range(3)] + \
                [('thorn-burst-%d' % k, thorn_burst, k, PPU) for k in range(3)] + \
                [('ring-edge-%s' % c, ring_edge, c, PPU) for c in ('orchid', 'warning')] + \
                [('ring-fill-%s' % c, ring_fill, c, 64) for c in ('orchid', 'warning')]
    for name, fn, k, ppu in draws:
        px, p = fn(k)
        w.sprite(name, px, p, 'effect', ppu=ppu)
        effects[name] = w.entry(name, flash=False)
        effects[name]['pixels_per_unit'] = ppu
    # The set's numbers from the art: the top of the walking poses and a little headroom; the shadow; the cycle.
    top = 0
    for name, (px, _) in walking.items():
        ys, _ = np.nonzero(px[..., 3] > 128)
        top = max(top, (px.shape[0] - walk_pivots[name.split('-')[0]][1]) - ys.min())
    visual_top = round(top / PPU + 0.05, 2)
    # The set: the template's own asset, line for line, each GUID swapped for this set's same-named sprite.
    tdir = CHARS / cfg['template_dir']
    template_set = (tdir / cfg['template_set']).read_bytes().decode('utf-8')
    prefix, ext, name_map = template_name_map(creature)
    swap = {}
    for meta_path in tdir.glob(prefix + '*' + ext):
        tname = meta_path.name[len(prefix):-len(ext)]
        mine = name_map[tname] if name_map else tname
        swap[guid_of(meta_path)] = w.guids[mine]
    text = re.sub(r'guid: ([0-9a-f]{32}), type: 3', lambda m: 'guid: %s, type: 3' % swap.get(m.group(1), m.group(1)),
                  template_set)
    text = re.sub(r'm_Name: \w+ArtSet', 'm_Name: %sArtSet' % cfg['stem'], text)
    stride = round(meta['stride'], 2)
    if '_visualTop' in text:
        text = re.sub(r'_visualTop: [\d.]+', '_visualTop: %s' % visual_top, text)
        text = re.sub(r'_shadowWidth: [\d.]+', '_shadowWidth: %s' % cfg['shadow_width'], text)
        text = re.sub(r'_walkCycleDistance: [\d.]+', '_walkCycleDistance: %s' % stride, text)
    else:
        # The Mite's asset leaves them at their defaults; the Wraith writes its own after _death, in field order.
        text, n = re.subn(r'(\n  _strikeWindup:)', '\n  _visualTop: %s\n  _shadowWidth: %s\n  _walkCycleDistance: %s\\1'
                          % (visual_top, cfg['shadow_width'], stride), text, count=1)
        assert n == 1
    text = re.sub(r'_frost: \{[^}]*\}', '_frost: {fileID: 0}', text)
    text = re.sub(r'_gloam: \{[^}]*\}', '_gloam: {fileID: 0}', text)
    sprite_refs = re.findall(r'fileID: 21300000, guid: ([0-9a-f]{32})', text)
    assert sprite_refs and all(g in w.guids.values() for g in sprite_refs), 'every sprite is this set\'s'
    assert 'type: 2}' not in text, 'the set references no other set'
    set_path = w.out / ('%sArtSet.asset' % cfg['stem'])
    set_meta = Path(str(set_path) + '.meta')
    set_guid = existing_guid(set_meta)
    set_path.write_bytes(text.encode('utf-8'))
    tmeta = (tdir / (cfg['template_set'] + '.meta')).read_bytes().decode('utf-8')
    set_meta.write_bytes(re.sub(r'^guid: \w+', 'guid: ' + set_guid, tmeta, count=1, flags=re.M).encode('utf-8'))
    # Drawings an earlier run wrote that this one did not: gone, with their .meta files.
    written = {t['file'] for t in w.textures}
    for stale in w.out.glob('CHR_%s_*.png' % cfg['stem']):
        if stale.name not in written:
            stale.unlink()
            Path(str(stale) + '.meta').unlink(missing_ok=True)
    by_group = {}
    for t in w.textures:
        by_group[t['group']] = by_group.get(t['group'], 0) + t['astc_4x4_bytes']
    total = sum(by_group.values())
    report = dict(art_set=dict(file=set_path.name, guid=set_guid, template=cfg['template_set']),
                  walk_body_size={f: list(walking['%s-idle' % f][0].shape[1::-1]) for f in FACINGS},
                  walk_body_pivot={f: list(walk_pivots[f]) for f in FACINGS}, visual_top=visual_top,
                  shadow_width=cfg['shadow_width'], walk_cycle_distance=stride,
                  body_centre_world_units={'%s-%s' % (r['facing'], r['pose']): r['body_centre'] for r in meta['frames']},
                  effects=effects)
    if creature == 'wraith':
        report['effects_note'] = (
            'pollen-mote-0..2: pooled at a still hero\'s feet while pollen is under 0.5 (harmless yet), pivot the mote; '
            'scatter a few round the feet, let them rise a little and fade. pollen-bloom-0..3: the bloom at the hero\'s '
            'feet, pivot the feet, 0.06 s a frame, played once with the usual hit flash. From 0.5 pollen the meter '
            'under the hero is the Thornling folder\'s ring pair at radius 0.8: orchid, red-orange only in its last '
            'second (docs/52 section 11 decision 5).')
    else:
        report['extras'] = dict(
            note='Not in ThornlingArtSet: the runtime holds these itself (front and rear look left, mirrored for the '
                 'right, as the set\'s); each drawn above the ash has a white flash sharing its rect and pivot (the '
                 'three mostly under it - dive-1, dive-2, erupt-0 - have none: it cannot be struck there). Pivots are '
                 'the floor point '
                 'under the creature, as the set\'s. The ash ring round the body is painted into the drawings (the '
                 'flash covers the creature only).',
            dive=['dive-0', 'dive-1', 'dive-2'], erupt=['erupt-0', 'erupt-1', 'erupt-2'], stuck=['stuck-0', 'stuck-1'],
            suggested_seconds=dict(dive_frame=0.067, erupt_frame=0.06, stuck_frame=0.25,
                                   dive='0.2 s: dive-0, dive-1, dive-2 (then hidden under the ash)',
                                   erupt='erupt-0, erupt-1, erupt-2 at 0.06 s, then stuck',
                                   stuck='1.0 s: alternate stuck-0 and stuck-1 every 0.25 s'),
            under_ash_without_flash=sorted(UNDER_ASH),
            frames={facing: {pose: w.entry('%s-%s' % (facing, pose), flash=pose not in UNDER_ASH)
                             for pose in cfg['extras']} for facing in FACINGS})
        report['effects_note'] = (
            'ash-puff-0..2: where the Thornling dives (and under an eruption), pivot the floor point, 0.08 s a frame. '
            'thorn-burst-0..2: at the ring\'s centre as it erupts, pivot the floor point, 0.06 s a frame. '
            'ring-edge-* (128 texels a unit) and ring-fill-* (64 texels a unit) are drawn at a radius of 1.0 world unit '
            'on the 2:1 floor, pivot the centre: scale both by the ring\'s radius (1.2), grow the fill 0..1 with the '
            'telegraph as the Warden mark\'s fill grows; use the orchid pair until the last second, then the warning '
            '(#FF5A3C) pair. The pollen meter under the hero reuses them at 0.8.')
    report.update(textures=w.textures, texture_count=len(w.textures), astc_4x4_bytes=total,
                  astc_4x4_bytes_by_group=by_group,
                  set_texture_count=sum(1 for t in w.textures if t['group'] in ('body', 'death')),
                  set_astc_4x4_bytes=sum(t['astc_4x4_bytes'] for t in w.textures if t['group'] in ('body', 'death')))
    out = DOCS / cfg['docs']
    out.mkdir(parents=True, exist_ok=True)
    (out / 'sprites.json').write_text(json.dumps(report, indent=1, default=lambda o: o.item()))
    print('HORDE_PACKED', creature, 'walk', report['walk_body_size'], 'pivot', walk_pivots, 'visualTop', visual_top,
          'textures', len(w.textures), 'set', report['set_texture_count'], report['set_astc_4x4_bytes'],
          'all', total, by_group, 'guid', set_guid)


if __name__ == '__main__':
    for c in (('wraith', 'thornling') if WHICH == 'both' else (WHICH,)):
        pack(c)
