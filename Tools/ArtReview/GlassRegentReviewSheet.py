"""The Glass Regent's review sheets (docs/50 section 6, docs/49's readability rules), from the packed sprites.

    python Tools/ArtReview/GlassRegentReviewSheet.py [--out DIR]

Writes into ArtDirection/2026-11-12/glass-regent-01/ (untracked, by convention):
    1-regent-frames.png   every body drawing, front and rear, the first phase over the second, at the sprites' own
                          128 texels a unit on floor 2's slate (#3B4350)
    2-copies-and-fall.png the copies' drawings and their two remains; the Regent's fall and its two remains
    3-lane-mark.png       the lane telegraph at progress .1 / .4 / .7 / 1 and the blow's flash, tinted as WardenSlamView
                          tints the Warden's mark, on IcePavingV1 at game scale; then phase 2's three lanes
    4-lineup.png          at game scale (120 px a unit: Gameplay.unity's 9 visible units across a 1080 px phone) on
                          IcePavingV1 and on the slate: a frosted Grunt, the hero, the Mirror Sentry, the frosted Forge
                          Warden, the Regent in each phase and a copy; then the same at 2x
    review.json           the measured values the self-review reads (heights, luminance, hue shares)

IcePavingV1 is laid flat at 3 units a tile, squashed 2:1 like the floor, unlit: the game's lighting and tint are not
reproduced, so it reads a little lighter than in the scene.
"""
import colorsys
import json
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ARGS = sys.argv[1:]
OUT = Path(ARGS[ARGS.index('--out') + 1]) if '--out' in ARGS else ROOT / 'ArtDirection/2026-11-12/glass-regent-01'
CH = ROOT / 'Assets/_Project/Art/Characters'
REGENT = CH / 'GlassRegent'
SLATE = (0x3B, 0x43, 0x50, 255)
INK = (235, 238, 245, 255)
GAME = 120                      # phone pixels a world unit at the game's framing
PPU = 128
EMBER = np.array([1.0, 0.32, 0.12])         # WardenSlamView.Ember
HOT = np.array([1.0, 0.82, 0.45])           # WardenSlamView.Hot
LANE_W, LANE_L = 1.2, 10.0                  # docs/50: RegentLaneWidth, RegentLaneLength
BODY_POSES = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b', 'windup', 'windup-step-a', 'windup-pass-a',
              'windup-pass-b']
COPY_POSES = ['idle', 'step-a', 'step-b', 'pass-a', 'pass-b', 'windup', 'windup-step-a', 'windup-pass-a',
              'windup-pass-b']


def font(size):
    try:
        return ImageFont.load_default(size=size)
    except TypeError:
        return ImageFont.load_default()


F = font(14)
FB = font(20)


def sprite(path):
    """(image, pivot in pixels from the top-left) of a sprite and its .meta's pivot."""
    im = Image.open(path).convert('RGBA')
    m = re.search(r'spritePivot: \{x: ([\d.e-]+), y: ([\d.e-]+)\}', Path(str(path) + '.meta').read_text())
    px, py = float(m.group(1)), float(m.group(2))
    return im, (px * im.width, (1 - py) * im.height)


def scaled(im, pivot, factor):
    if factor == 1:
        return im, pivot
    size = (max(1, round(im.width * factor)), max(1, round(im.height * factor)))
    return im.resize(size, Image.LANCZOS), (pivot[0] * factor, pivot[1] * factor)


def place(canvas, im, pivot, x, y, mirror=False):
    if mirror:
        im = im.transpose(Image.FLIP_LEFT_RIGHT)
        pivot = (im.width - pivot[0], pivot[1])
    canvas.alpha_composite(im, (int(round(x - pivot[0])), int(round(y - pivot[1]))))


def label(canvas, x, y, text, f=F, fill=INK):
    ImageDraw.Draw(canvas).text((x, y), text, font=f, fill=fill)


def paving(w, h, units_per_tile=3.0, scale=GAME):
    tex = Image.open(ROOT / 'Assets/_Project/Art/Environment/Ice/IcePavingV1.png').convert('RGBA')
    tw = int(units_per_tile * scale)
    tile = tex.resize((tw, tw // 2), Image.LANCZOS)
    bg = Image.new('RGBA', (w, h))
    for yy in range(0, h, tile.height):
        for xx in range(0, w, tile.width):
            bg.paste(tile, (xx, yy))
    return bg


# ---- 1. The Regent's frames ----
def regent_frames():
    rows = [(f, v) for f in ('front', 'rear') for v in ('', '_hot')]
    cells = [[sprite(REGENT / ('CHR_GlassRegent_%s_%s%s.png' % (f, p, v))) for p in BODY_POSES] for f, v in rows]
    cw = max(im.width for row in cells for im, _ in row) + 8
    above = max(pv[1] for row in cells for _, pv in row)
    below = max(im.height - pv[1] for row in cells for im, pv in row)
    ch = int(above + below) + 30
    sheet = Image.new('RGBA', (cw * len(BODY_POSES) + 150, ch * len(rows) + 40), SLATE)
    label(sheet, 8, 8, 'Glass Regent - every body drawing at 128 texels a unit, looking left (the game mirrors for the '
                       'right). Rows: front phase 1, front phase 2, rear phase 1, rear phase 2.', FB)
    for j, ((f, v), row) in enumerate(zip(rows, cells)):
        label(sheet, 8, 40 + j * ch + ch // 2, '%s %s' % (f, 'phase 2' if v else 'phase 1'))
        for i, (im, pv) in enumerate(row):
            x = 150 + i * cw + cw // 2
            place(sheet, im, pv, x, 40 + j * ch + 20 + above)
            if j == 0:
                label(sheet, 150 + i * cw + 4, 40 + 2, BODY_POSES[i])
    sheet.save(OUT / '1-regent-frames.png')


# ---- 2. The copies and the falls ----
def copies_and_fall():
    rows = [[sprite(REGENT / 'Copy' / ('CHR_GlassRegentCopy_%s_%s.png' % (f, p))) for p in COPY_POSES] for f in ('front', 'rear')]
    rows[0] += [sprite(REGENT / 'Copy' / 'CHR_GlassRegentCopy_death_0.png'), sprite(REGENT / 'Copy' / 'CHR_GlassRegentCopy_death_1.png')]
    falls = [sprite(REGENT / n) for n in ('CHR_GlassRegent_front_fall-struck.png', 'CHR_GlassRegent_death_0.png',
                                          'CHR_GlassRegent_fall-topple.png', 'CHR_GlassRegent_death_1.png',
                                          'CHR_GlassRegent_rear_fall-struck.png', 'CHR_GlassRegent_rear_fall-knee.png')]
    cw = max(im.width for row in rows for im, _ in row) + 8
    above = max(pv[1] for row in rows for _, pv in row)
    ch = int(above + max(im.height - pv[1] for row in rows for im, pv in row)) + 30
    fw = max(im.width for im, _ in falls) + 10
    fa = max(pv[1] for _, pv in falls)
    fh = int(fa + max(im.height - pv[1] for im, pv in falls)) + 30
    w = max(cw * 11 + 120, fw * 6 + 120)
    sheet = Image.new('RGBA', (w, 50 + ch * 2 + 40 + fh), SLATE)
    label(sheet, 8, 8, 'Glass copies (no crown, no heart, no eyes; pale silver at 72 % opacity, outline 90 %) and their two '
                       'remains; below, the Regent\'s fall (struck, kneel = remains 0, topple, down = remains 1; rear struck, '
                       'rear kneel).', FB)
    names = COPY_POSES + ['remains 0', 'remains 1']
    for j, row in enumerate(rows):
        label(sheet, 8, 50 + j * ch + ch // 2, ('front', 'rear')[j])
        for i, (im, pv) in enumerate(row):
            place(sheet, im, pv, 120 + i * cw + cw // 2, 50 + j * ch + 20 + above)
            if j == 0:
                label(sheet, 120 + i * cw + 4, 52, names[i])
    y0 = 50 + ch * 2 + 40
    for i, (im, pv) in enumerate(falls):
        place(sheet, im, pv, 120 + i * fw + fw // 2, y0 + 20 + fa)
        label(sheet, 120 + i * fw + 4, y0, ['struck', 'kneel (remains 0)', 'topple', 'down (remains 1)', 'rear struck',
                                            'rear kneel'][i])
    sheet.save(OUT / '2-copies-and-fall.png')


# ---- 3. The lane mark ----
def sliced(path, width, height, border=20):
    """A 9-slice sprite drawn at width x height texels, the borders kept (shrunk only when the size is smaller)."""
    src = np.asarray(Image.open(path).convert('RGBA')).astype(np.float64) / 255
    n = src.shape[0]
    bx, by = min(border, width / 2), min(border, height / 2)

    def axis(size, b):
        out = np.empty(size)
        t = np.arange(size) + 0.5
        lo, hi = t < b, t > size - b
        out[lo] = t[lo] / b * border
        out[hi] = n - (size - t[hi]) / b * border
        mid = ~lo & ~hi
        out[mid] = border + (t[mid] - b) / max(size - 2 * b, 1e-6) * (n - 2 * border)
        return np.clip(out - 0.5, 0, n - 1)

    xs, ys = axis(int(width), bx), axis(int(height), by)
    x0, y0 = np.floor(xs).astype(int), np.floor(ys).astype(int)
    x1, y1 = np.minimum(x0 + 1, n - 1), np.minimum(y0 + 1, n - 1)
    fx, fy = (xs - x0)[None, :, None], (ys - y0)[:, None, None]
    pm = src.copy()
    pm[..., :3] *= pm[..., 3:]
    top = pm[y0][:, x0] * (1 - fx) + pm[y0][:, x1] * fx
    bot = pm[y1][:, x0] * (1 - fx) + pm[y1][:, x1] * fx
    return top * (1 - fy) + bot * fy          # premultiplied


def lane_layer(canvas_size, origin, angle, progress, flash=False):
    """The lane at the game's scale: edge and fill composed in the lane's own space at 128 texels a unit, tinted as
    WardenSlamView tints the Warden's mark, then laid on the 2:1 floor from origin (screen px) along angle (floor
    radians)."""
    length, width = int(LANE_L * PPU), int(round(LANE_W * PPU))
    edge = sliced(REGENT / 'Lane/FX_RegentLane_Edge.png', length, width)
    fill_h = max(2, int(round(max(.05, progress) * width)))
    fill = sliced(REGENT / 'Lane/FX_RegentLane_Fill.png', length, fill_h)
    layer = np.zeros((width, length, 4))
    off = (width - fill_h) // 2
    edge_tint = np.append(HOT if flash else EMBER, 1.0 if flash else 0.8)      # 0.8: the pulse's middle
    fill_tint = np.array([1, 1, 1, 1.0 if flash else .4 + .6 * progress])
    layer[off:off + fill_h] += fill * fill_tint
    e = edge * edge_tint
    layer = e + layer * (1 - e[..., 3:])
    straight = np.where(layer[..., 3:] > 1e-6, layer[..., :3] / np.maximum(layer[..., 3:], 1e-6), 0)
    img = Image.fromarray((np.concatenate([straight, layer[..., 3:]], -1).clip(0, 1) * 255 + .5).astype(np.uint8), 'RGBA')
    c, s = np.cos(angle), np.sin(angle)
    ox, oy = origin
    k = 1 / GAME
    a = PPU * (c * k)
    b = PPU * (-2 * s * k)
    cc = PPU * (-c * ox * k + 2 * s * oy * k)
    d = -PPU * (-s * k)
    e_ = -PPU * (-2 * c * k)
    f = width / 2 - PPU * (s * ox * k + 2 * c * oy * k)
    return img.transform(canvas_size, Image.AFFINE, (a, b, cc, d, e_, f), resample=Image.BILINEAR)


def floor_to_screen(origin, fx, fy):
    return origin[0] + fx * GAME, origin[1] - fy * GAME * 0.5


def lane_mark():
    regent = scaled(*sprite(REGENT / 'CHR_GlassRegent_front_windup.png'), GAME / PPU)
    regent2 = scaled(*sprite(REGENT / 'CHR_GlassRegent_front_windup_hot.png'), GAME / PPU)
    copy = scaled(*sprite(REGENT / 'Copy/CHR_GlassRegentCopy_front_windup.png'), GAME / PPU)
    hero = scaled(*sprite(CH / 'Vanguard/CHR_Vanguard_Front_Idle.tga'), GAME / PPU)
    pw, ph = 560, 600
    panels = [(0.1, False), (0.4, False), (0.7, False), (1.0, False), (1.0, True)]
    sheet = Image.new('RGBA', (pw * 5, ph * 2 + 60), SLATE)
    label(sheet, 8, 8, 'The lane mark on IcePavingV1 at game scale: progress .1 / .4 / .7 / 1 and the blow\'s flash '
                       '(edge tinted Ember with its pulse, the stripe widening to the edges); below, phase 2\'s three lanes '
                       'at .7 - the Regent and two copies 120 degrees round the hero.', FB)
    angle = np.radians(200)            # toward the screen's lower left: the Regent stands up and to the right
    for i, (p, flash) in enumerate(panels):
        panel = paving(pw, ph)
        origin = (pw * 0.8, ph * 0.66)
        panel.alpha_composite(lane_layer(panel.size, origin, angle, p, flash))
        hx, hy = floor_to_screen(origin, 3.2 * np.cos(angle), 3.2 * np.sin(angle))
        place(panel, *hero, hx, hy)
        place(panel, *regent, *origin)
        label(panel, 8, ph - 24, 'flash' if flash else 'progress %.1f' % p, FB)
        sheet.paste(panel, (i * pw, 60))
    # Phase 2: three bodies round the hero, every lane through the hero's spot.
    panel = paving(pw * 5, ph)
    hero_at = (pw * 2.5, ph * 0.6)
    dist = 2.8
    bodies = []
    for k, look in enumerate((regent2, copy, copy)):
        a = np.radians(60 + 120 * k)
        fx, fy = dist * np.cos(a), dist * np.sin(a)
        bodies.append((floor_to_screen(hero_at, fx, fy), np.arctan2(-fy, -fx), look))
    for pos, ang, _ in bodies:
        panel.alpha_composite(lane_layer(panel.size, pos, ang, 0.7))
    for pos, ang, look in sorted(bodies + [(hero_at, 0, hero)], key=lambda b: b[0][1]):
        place(panel, *look, *pos, mirror=pos[0] < hero_at[0])
    label(panel, 8, ph - 24, 'phase 2, progress 0.7', FB)
    sheet.paste(panel, (0, 60 + ph))
    sheet.save(OUT / '3-lane-mark.png')


# ---- 4. The lineup ----
LINEUP = [
    ('frosted Grunt', CH / 'SlagBrute/Ice/CHR_Grunt_front-idle.png'),
    ('hero (Vanguard)', CH / 'Vanguard/CHR_Vanguard_Front_Idle.tga'),
    ('Mirror Sentry', CH / 'MirrorSentry/CHR_MirrorSentry_front-idle.png'),
    ('frosted Forge Warden', CH / 'ForgeWarden/Ice/CHR_Warden_front_idle.png'),
    ('Glass Regent', REGENT / 'CHR_GlassRegent_front_idle.png'),
    ('Regent phase 2', REGENT / 'CHR_GlassRegent_front_idle_hot.png'),
    ('glass copy', REGENT / 'Copy/CHR_GlassRegentCopy_front_idle.png'),
]


def lineup():
    gap = int(1.25 * GAME)
    w = gap * (len(LINEUP) + 1)
    strip = int(2.7 * GAME)
    sheet = Image.new('RGBA', (w, 60 + strip * 2 + 40 + strip * 2), SLATE)
    label(sheet, 8, 8, 'Lineup at game scale (120 px a unit = a 1080 px phone), on IcePavingV1 (unlit) and on the slate '
                       '#3B4350; then the same at 2x.', FB)
    heights = {}
    for row, (bg, zoom) in enumerate(((paving(w, strip), 1), (Image.new('RGBA', (w, strip), SLATE), 1))):
        for i, (name, path) in enumerate(LINEUP):
            im, pv = scaled(*sprite(path), GAME / PPU)
            place(bg, im, pv, gap * (i + 1), strip - 30)
            if row == 0:
                label(bg, gap * (i + 1) - 50, strip - 22, name)
                a = np.asarray(Image.open(path).convert('RGBA'))[..., 3]
                top = np.nonzero((a > 128).any(axis=1))[0].min()
                heights[name] = round((sprite(path)[1][1] - top) / PPU, 3)
        sheet.paste(bg, (0, 60 + row * strip))
    big = sheet.crop((0, 60, w, 60 + strip * 2)).resize((w * 2, strip * 4), Image.NEAREST)
    big = big.crop((gap * 5, 0, gap * 5 + w, strip * 4))
    sheet.paste(big, (0, 60 + strip * 2 + 40))
    label(sheet, 8, 60 + strip * 2 + 10, '2x, from the Mirror Sentry rightward (on the paving)', FB)
    sheet.save(OUT / '4-lineup.png')
    return heights


def measure():
    """Body luminance and hue shares of the Regent's idle (opaque texels), against the slate's."""
    out = {}
    for name, path in (('regent', REGENT / 'CHR_GlassRegent_front_idle.png'),
                       ('regent_hot', REGENT / 'CHR_GlassRegent_front_idle_hot.png'),
                       ('copy', REGENT / 'Copy/CHR_GlassRegentCopy_front_idle.png'),
                       ('mirror_sentry', CH / 'MirrorSentry/CHR_MirrorSentry_front-idle.png'),
                       ('frosted_warden', CH / 'ForgeWarden/Ice/CHR_Warden_front_idle.png')):
        a = np.asarray(Image.open(path).convert('RGBA')).astype(np.float64) / 255
        m = a[..., 3] > 0.5
        rgb = a[..., :3][m]
        lum = rgb @ np.array([0.2126, 0.7152, 0.0722])
        hsv = np.array([colorsys.rgb_to_hsv(*c) for c in rgb[::7]])
        warm = ((hsv[:, 0] < 0.11) | (hsv[:, 0] > 0.95)) & (hsv[:, 1] > 0.45) & (hsv[:, 2] > 0.4)
        violet = (hsv[:, 0] > 0.66) & (hsv[:, 0] < 0.8) & (hsv[:, 1] > 0.25)
        out[name] = dict(mean_luminance=round(float(lum.mean()), 3), bright_share=round(float((lum > 0.5).mean()), 3),
                         warm_share=round(float(warm.mean()), 4), violet_share=round(float(violet.mean()), 3),
                         opaque_alpha=round(float(a[..., 3][m].mean()), 3))
    out['slate_luminance'] = round(float(np.array([0x3B, 0x43, 0x50]) / 255 @ np.array([0.2126, 0.7152, 0.0722])), 3)
    return out


OUT.mkdir(parents=True, exist_ok=True)
regent_frames()
copies_and_fall()
lane_mark()
review = dict(heights_units=lineup(), colour=measure())
(OUT / 'review.json').write_text(json.dumps(review, indent=1))
print(json.dumps(review, indent=1))
