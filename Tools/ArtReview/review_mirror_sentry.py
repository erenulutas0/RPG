"""Review sheets for the Mirror Sentry (PackMirrorSentrySprites.py): exactly the PNG pixels Unity imports, placed by
their .meta pivots, on floor 2's slate ice (IcePavingV1).

    python Tools/ArtReview/review_mirror_sentry.py [--out DIR]

Writes to DIR (default ArtDirection/2026-11-12/mirror-sentry-01, untracked):
    facings.png      the four facings (front and rear, each mirrored for the right), zoomed 2x
    motion.png       the strike windup and the Mirror Bash, both facings, then its own death and the two remains, 2x
    lineup.png       at the phone's scale (the Samsung S23 shows 120 pixels a world unit), with contact shadows:
                     the Vanguard, the frosted Grunt, Mite, Tank and Captain, and the Sentry front and rear; below, the
                     Sentry facing the hero with its glint arc on the floor and a reflect glint toward the hero
    lineup-value.png the same in luminance only: what reads first by value
    effects.png      the glint arc (top-down, as drawn, and on the 2:1 floor at four facings) and the reflect glint's
                     three frames, 2x
    checks.json      the readability numbers (docs/49 section 7, docs/50 section 2): the mirror's luminance against
                     the plate's, the share of warm texels on the body, the Sentry's height against the Captain's
"""
import colorsys
import json
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
ARGS = sys.argv[1:]
OUT = Path(ARGS[ARGS.index('--out') + 1]) if '--out' in ARGS else ROOT / 'ArtDirection/2026-11-12/mirror-sentry-01'
OUT.mkdir(parents=True, exist_ok=True)
ART = ROOT / 'Assets/_Project/Art/Characters'
SENTRY = ART / 'MirrorSentry'
PAVING = ROOT / 'Assets/_Project/Art/Environment/Ice/IcePavingV1.png'
PHONE = 120
PPU = 128
DEPTH = 0.5                       # ArenaFloor.DepthScale
SLATE = (0x3B, 0x43, 0x50, 255)


class Sprite:
    def __init__(self, path):
        self.image = Image.open(path).convert('RGBA')
        meta = Path(str(path) + '.meta').read_text(encoding='utf-8')
        x, y = (float(v) for v in re.search(r'spritePivot: \{x: ([-\d.]+), y: ([-\d.]+)\}', meta).groups())
        self.pivot = (x * self.image.width, (1 - y) * self.image.height)   # from the top-left

    def mirrored(self):
        s = Sprite.__new__(Sprite)
        s.image = self.image.transpose(Image.FLIP_LEFT_RIGHT)
        s.pivot = (self.image.width - self.pivot[0], self.pivot[1])
        return s


def sentry(name):
    return Sprite(SENTRY / ('CHR_MirrorSentry_%s.png' % name))


def floor(size):
    tile = Image.open(PAVING).convert('RGBA')
    tile = tile.resize((tile.width // 2, tile.height // 2), Image.LANCZOS)
    out = Image.new('RGBA', size)
    for y in range(0, size[1], tile.height):
        for x in range(0, size[0], tile.width):
            out.paste(tile, (x, y))
    return out


def shadow(canvas, at, width, scale):
    layer = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
    rx, ry = width * scale / 2, width * scale / 4
    ImageDraw.Draw(layer).ellipse((at[0] - rx, at[1] - ry, at[0] + rx, at[1] + ry), fill=(8, 6, 12, 110))
    canvas.alpha_composite(layer.filter(ImageFilter.GaussianBlur(scale * 0.04)))


def paste(canvas, sprite, at, scale, smooth=True):
    k = scale / PPU
    size = (max(1, round(sprite.image.width * k)), max(1, round(sprite.image.height * k)))
    image = sprite.image.resize(size, Image.BILINEAR if smooth else Image.NEAREST)
    canvas.alpha_composite(image, (round(at[0] - sprite.pivot[0] * k), round(at[1] - sprite.pivot[1] * k)))


def label(canvas, text, at):
    d = ImageDraw.Draw(canvas)
    d.text((at[0] + 1, at[1] + 1), text, fill=(0, 0, 0, 255))
    d.text(at, text, fill=(236, 240, 248, 255))


def strip(items, scale, height, gap, title, background=None):
    width = sum(max(it[1].image.width * scale / PPU, 40) + gap for it in items) + gap
    canvas = (background or (lambda s: Image.new('RGBA', s, SLATE)))((int(width), height))
    x = gap
    for text, sprite in items:
        w = max(sprite.image.width * scale / PPU, 40)
        at = (x + sprite.pivot[0] * scale / PPU, height - 70)
        paste(canvas, sprite, at, scale, smooth=False)
        label(canvas, text, (x, height - 20))
        x += w + gap
    label(canvas, title, (8, 6))
    return canvas


def floor_glint(arc, degrees, scale):
    """The glint arc as the runtime should lay it: turned to the facing, then squashed onto the 2:1 floor."""
    big = arc.image.resize((round(arc.image.width * scale / PPU), round(arc.image.height * scale / PPU)), Image.BILINEAR)
    pad = max(big.size) * 2
    square = Image.new('RGBA', (pad, pad))
    px, py = arc.pivot[0] * scale / PPU, arc.pivot[1] * scale / PPU
    square.alpha_composite(big, (round(pad / 2 - px), round(pad / 2 - py)))
    square = square.rotate(degrees, resample=Image.BICUBIC)
    return square.resize((pad, round(pad * DEPTH)), Image.BICUBIC)


def main():
    # ---- The four facings ----
    items = [('front (left)', sentry('front-idle')), ('front (right)', sentry('front-idle').mirrored()),
             ('rear (left)', sentry('rear-idle')), ('rear (right)', sentry('rear-idle').mirrored())]
    strip(items, 256, 520, 30, 'Mirror Sentry - four facings (front, rear; the game mirrors them for the right) - 2x',
          floor).convert('RGB').save(OUT / 'facings.png')
    # ---- Strike and death ----
    rows = [[('windup front', sentry('front-windup')), ('strike front', sentry('front-strike')),
             ('windup rear', sentry('rear-windup')), ('strike rear', sentry('rear-strike')),
             ('remains: collapse', sentry('death-collapse')), ('remains: cool', sentry('death-cool'))],
            [('dying front %d' % k, sentry('dying-front-%d' % k)) for k in range(7)],
            [('dying rear %d' % k, sentry('dying-rear-%d' % k)) for k in range(7)]]
    sheets = [strip(row, 256, 500, 24, title, floor) for row, title in
              zip(rows, ['The Mirror Bash (windup, strike) and the two remains - 2x', 'Own death, front, 0.06 s a step',
                         'Own death, rear'])]
    width = max(s.width for s in sheets)
    motion = Image.new('RGBA', (width, sum(s.height for s in sheets)), SLATE)
    y = 0
    for s in sheets:
        motion.alpha_composite(s, (0, y))
        y += s.height
    motion.convert('RGB').save(OUT / 'motion.png')
    # ---- The lineup at the phone's scale ----
    cast = [('Vanguard', Sprite(ART / 'Vanguard/CHR_Vanguard_Front_Idle.tga'), 0.78),
            ('Grunt (frosted)', Sprite(ART / 'SlagBrute/Ice/CHR_Grunt_front-idle.png'), 0.78),
            ('Mite (frosted)', Sprite(ART / 'CinderMite/Ice/CHR_Mite_front_idle.png'), 0.45),
            ('Tank (frosted)', Sprite(ART / 'Tank/Ice/CHR_Tank_front-idle.png'), 0.93),
            ('Captain (frosted)', Sprite(ART / 'GruntCaptain/Ice/CHR_Captain_front-idle.png'), 0.88),
            ('Mirror Sentry', sentry('front-idle'), 0.86), ('Sentry rear', sentry('rear-idle'), 0.86)]
    lineup = floor((1000, 520))
    for k, (name, sprite, width) in enumerate(cast):
        at = (75 + k * 140, 230)
        shadow(lineup, at, width, PHONE)
        paste(lineup, sprite, at, PHONE)
        label(lineup, name, (at[0] - 45, 240))
    label(lineup, 'Floor 2 at the phone\'s scale (120 px a world unit) on IcePavingV1', (8, 6))
    # The Sentry facing the hero, its glint arc on the floor toward the hero, a reflect glint from its mirror.
    hero_at, sentry_at = (230, 470), (560, 410)
    arc = sentry('glint-arc')
    dx, dy = hero_at[0] - sentry_at[0], (hero_at[1] - sentry_at[1]) / DEPTH
    facing = np.degrees(np.arctan2(-dy, dx))
    ground = floor_glint(arc, facing, PHONE)
    shadow(lineup, sentry_at, 0.86, PHONE)
    lineup.alpha_composite(ground, (round(sentry_at[0] - ground.width / 2), round(sentry_at[1] - ground.height / 2)))
    shadow(lineup, hero_at, 0.78, PHONE)
    paste(lineup, Sprite(ART / 'Vanguard/CHR_Vanguard_Front_Idle.tga').mirrored(), hero_at, PHONE)
    paste(lineup, sentry('front-idle'), sentry_at, PHONE)
    anchor = json.loads((OUT / 'sprites.json').read_text())['mirror_anchor_world_units']['front-idle']
    mirror_at = (sentry_at[0] + anchor[0] * PHONE, sentry_at[1] - anchor[1] * PHONE)
    target = (hero_at[0], hero_at[1] - 0.55 * PHONE)
    streak = sentry('reflect-1')
    length = np.hypot(target[0] - mirror_at[0], target[1] - mirror_at[1])
    big = streak.image.resize((round(streak.image.width * length / PPU), round(streak.image.height * PHONE / PPU)),
                              Image.BILINEAR)
    pad = max(big.size) * 2 + 4
    square = Image.new('RGBA', (pad, pad))
    square.alpha_composite(big, (round(pad / 2 - streak.pivot[0] * length / PPU),
                                 round(pad / 2 - streak.pivot[1] * PHONE / PPU)))
    angle = np.degrees(np.arctan2(-(target[1] - mirror_at[1]), target[0] - mirror_at[0]))
    square = square.rotate(angle, resample=Image.BICUBIC)
    lineup.alpha_composite(square, (round(mirror_at[0] - pad / 2), round(mirror_at[1] - pad / 2)))
    label(lineup, 'the guard: glint arc toward the hero (rotated, then squashed 2:1), a reflect glint (frame 1)',
          (8, 300))
    lineup.convert('RGB').save(OUT / 'lineup.png')
    lineup.convert('L').convert('RGB').save(OUT / 'lineup-value.png')
    # ---- The effects ----
    fx = Image.new('RGBA', (1000, 580), SLATE)
    paste(fx, arc, (40, 220), 256, smooth=False)
    label(fx, 'glint arc, top-down as drawn (pivot = the root, left edge), 2x', (8, 6))
    for k, degrees in enumerate((0, 90, 150, 225)):
        g = floor_glint(arc, degrees, 256)
        fx.alpha_composite(g, (round(470 + (k % 2) * 330 - g.width / 2), round(130 + (k // 2) * 210 - g.height / 2)))
        label(fx, 'facing %d deg on the 2:1 floor' % degrees, (400 + (k % 2) * 330, 30 + (k // 2) * 210))
    for k in range(3):
        paste(fx, sentry('reflect-%d' % k), (40, 480 + k * 30), 256, smooth=False)
        label(fx, 'reflect %d (one world unit long, pivot at the mirror)' % k, (380, 472 + k * 30))
    fx.convert('RGB').save(OUT / 'effects.png')
    # ---- The numbers ----
    checks = {}
    for name in ('front-idle', 'front-step-a', 'front-strike', 'front-windup'):
        px = np.asarray(sentry(name).image).astype(float) / 255
        a = px[..., 3] > 0.9
        rgb = px[..., :3][a]
        lum = rgb @ np.array([0.2126, 0.7152, 0.0722])
        hsv = np.array([colorsys.rgb_to_hsv(*c) for c in rgb])
        mirrorish = (hsv[:, 0] > 0.68) & (hsv[:, 0] < 0.78) & (hsv[:, 1] > 0.08) & (lum > 0.55)
        warm = ((hsv[:, 0] < 0.11) | (hsv[:, 0] > 0.95)) & (hsv[:, 1] > 0.35) & (hsv[:, 2] > 0.35)
        rest = ~mirrorish
        checks[name] = dict(mirror_texels=int(mirrorish.sum()), mirror_mean_luminance=round(float(lum[mirrorish].mean()), 3),
                            body_mean_luminance=round(float(lum[rest].mean()), 3),
                            body_p95_luminance=round(float(np.percentile(lum[rest], 95)), 3),
                            warm_texels=int(warm.sum()), texels=int(a.sum()))
    def height(sprite):
        ys = np.nonzero(np.asarray(sprite.image)[..., 3] > 128)[0]
        return round((sprite.pivot[1] - ys.min()) / PPU, 3)
    checks['figure_height_units'] = dict(sentry=height(sentry('front-idle')),
                                         captain=height(Sprite(ART / 'GruntCaptain/Ice/CHR_Captain_front-idle.png')),
                                         grunt=height(Sprite(ART / 'SlagBrute/Ice/CHR_Grunt_front-idle.png')),
                                         vanguard=height(Sprite(ART / 'Vanguard/CHR_Vanguard_Front_Idle.tga')))
    floor_px = np.asarray(floor((256, 256))).astype(float)[..., :3] / 255
    checks['floor_mean_luminance'] = round(float((floor_px @ np.array([0.2126, 0.7152, 0.0722])).mean()), 3)
    (OUT / 'checks.json').write_text(json.dumps(checks, indent=1))
    print(json.dumps(checks, indent=1))


if __name__ == '__main__':
    main()
