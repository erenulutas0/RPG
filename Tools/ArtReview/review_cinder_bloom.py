"""Review sheets for the Cinder Bloom (PackCinderBloomSprites.py): exactly the PNG pixels Unity imports, placed by their
.meta pivots, on floor 3's ash loam (GloamPavingV1).

    python Tools/ArtReview/review_cinder_bloom.py [--out DIR]

Writes to DIR (default ArtDirection/2026-11-14/cinder-bloom-01, untracked):
    1-states.png      CLOSED against OPEN, both facings and their mirrors, 2x: the cycle's two silhouettes
    2-cycle.png       the cycle as the runtime would play it, each facing: closed idle, unfold-0, unfold-1, open,
                      unfold-1, unfold-0 (closing), closed idle - 2x
    3-motion.png      the walk (idle, step, pass), the maul's windup and strike, both facings, then its own death and
                      the two remains, 2x
    4-lineup.png      at the phone's scale (the Samsung S23 shows 120 pixels a world unit), with contact shadows: the
                      Vanguard, the floor-3 roster in its bark-and-orchid recolour (Grunt, Mite, Runner, Tank, Captain),
                      the Mirror Sentry (for the silhouette), and the Bloom closed and open, front and rear; below, a
                      Thorn Post moment - the hero between a closed and an open Bloom amid the Gloam Grunt and Mites
    4-lineup-value.png the same in luminance only: what reads first by value
    5-squint.png      the Thorn Post moment at half the phone's scale and blurred, the "one glance" test
    6-bark-chip.png   the bark chip's three frames on the closed Bloom, 3x
    checks.json       the readability numbers (docs/51 section 7, docs/52 section 2 rule 3): orchid share closed against
                      open, warm (red-orange) and chartreuse texels (both must be 0), luminance against the loam, the
                      figure's height and width against the Captain's, texture memory
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
OUT = Path(ARGS[ARGS.index('--out') + 1]) if '--out' in ARGS else ROOT / 'ArtDirection/2026-11-14/cinder-bloom-01'
OUT.mkdir(parents=True, exist_ok=True)
ART = ROOT / 'Assets/_Project/Art/Characters'
BLOOM = ART / 'CinderBloom'
PAVING = ROOT / 'Assets/_Project/Art/Environment/Gloam/GloamPavingV1.png'
PHONE = 120
PPU = 128
LOAM = (0x3C, 0x3F, 0x36, 255)


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


def bloom(name):
    return Sprite(BLOOM / ('CHR_CinderBloom_%s.png' % name))


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
    ImageDraw.Draw(layer).ellipse((at[0] - rx, at[1] - ry, at[0] + rx, at[1] + ry), fill=(10, 14, 12, 110))
    canvas.alpha_composite(layer.filter(ImageFilter.GaussianBlur(scale * 0.04)))


def paste(canvas, sprite, at, scale, smooth=True):
    k = scale / PPU
    size = (max(1, round(sprite.image.width * k)), max(1, round(sprite.image.height * k)))
    image = sprite.image.resize(size, Image.BILINEAR if smooth else Image.NEAREST)
    canvas.alpha_composite(image, (round(at[0] - sprite.pivot[0] * k), round(at[1] - sprite.pivot[1] * k)))


def label(canvas, text, at):
    d = ImageDraw.Draw(canvas)
    d.text((at[0] + 1, at[1] + 1), text, fill=(0, 0, 0, 255))
    d.text(at, text, fill=(236, 240, 228, 255))


def strip(items, scale, height, gap, title, cell=None, foot=60):
    """A row of sprites standing on one line, each in a cell wide enough for the widest."""
    cell = cell or max(max(s.image.width * scale / PPU for _, s in items), 60)
    width = int(len(items) * (cell + gap) + gap)
    canvas = floor((width, height))
    for k, (text, sprite) in enumerate(items):
        x = gap + k * (cell + gap)
        at = (x + cell / 2, height - foot)
        if sprite is not None:
            paste(canvas, sprite, at, scale, smooth=False)
        label(canvas, text, (x, height - foot + 18))
    label(canvas, title, (8, 6))
    return canvas


def stack(sheets, path):
    width = max(s.width for s in sheets)
    out = Image.new('RGBA', (width, sum(s.height for s in sheets)), LOAM)
    y = 0
    for s in sheets:
        out.alpha_composite(s, (0, y))
        y += s.height
    out.convert('RGB').save(path)
    return out


def main():
    # ---- 1. Closed against open ----
    rows = []
    for facing in ('front', 'rear'):
        rows.append(strip([('CLOSED %s (left)' % facing, bloom('%s-idle' % facing)),
                           ('CLOSED %s (right)' % facing, bloom('%s-idle' % facing).mirrored()),
                           ('OPEN %s (left)' % facing, bloom('%s-open' % facing)),
                           ('OPEN %s (right)' % facing, bloom('%s-open' % facing).mirrored())],
                          256, 560, 24, 'Cinder Bloom - closed (bark shut, maul on the shoulder, walks and swings) '
                          'against open (rooted, maul grounded, the bloom out: strike here) - %s, 2x' % facing))
    stack(rows, OUT / '1-states.png')
    # ---- 2. The cycle ----
    order = ['idle', 'unfold-0', 'unfold-1', 'open', 'unfold-1', 'unfold-0', 'idle']
    names = ['closed', 'unfold-0', 'unfold-1', 'OPEN (held 2.5 s)', 'unfold-1 (closing)', 'unfold-0 (closing)', 'closed']
    rows = [strip([(n, bloom('%s-%s' % (facing, o))) for n, o in zip(names, order)], 256, 540, 16,
                  'The cycle, %s: closed 3.5 s -> unfold -> open 2.5 s -> close -> closed (2x)' % facing)
            for facing in ('front', 'rear')]
    stack(rows, OUT / '2-cycle.png')
    # ---- 3. Motion and death ----
    rows = []
    for facing in ('front', 'rear'):
        rows.append(strip([(p, bloom('%s-%s' % (facing, p))) for p in
                           ('idle', 'step-a', 'pass-a', 'step-b', 'pass-b', 'windup', 'strike')], 256, 560, 16,
                          'Closed, %s: the walk (maul on the shoulder), the windup and the blow (2x)' % facing))
    for facing in ('front', 'rear'):
        rows.append(strip([('dying %s %d' % (facing, k), bloom('dying-%s-%d' % (facing, k))) for k in range(7)],
                          256, 440, 16, 'Own death, %s, 0.06 s a step: the shutters burst, the bloom wilts' % facing))
    rows.append(strip([('remains: collapse', bloom('death-collapse')), ('remains: cool', bloom('death-cool'))],
                      256, 400, 24, 'The two remains'))
    stack(rows, OUT / '3-motion.png')
    # ---- 4. The lineup at the phone's scale ----
    cast = [('Vanguard', Sprite(ART / 'Vanguard/CHR_Vanguard_Front_Idle.tga'), 0.78),
            ('Grunt (gloam)', Sprite(ART / 'SlagBrute/Gloam/CHR_Grunt_front-idle.png'), 0.78),
            ('Mite (gloam)', Sprite(ART / 'CinderMite/Gloam/CHR_Mite_front_idle.png'), 0.45),
            ('Runner (gloam)', Sprite(ART / 'Runner/Gloam/CHR_Runner_front-idle.png'), 0.6),
            ('Tank (gloam)', Sprite(ART / 'Tank/Gloam/CHR_Tank_front-idle.png'), 0.93),
            ('Captain (gloam)', Sprite(ART / 'GruntCaptain/Gloam/CHR_Captain_front-idle.png'), 0.88),
            ('Mirror Sentry', Sprite(ART / 'MirrorSentry/CHR_MirrorSentry_front-idle.png'), 0.86),
            ('Bloom closed', bloom('front-idle'), 0.86), ('Bloom open', bloom('front-open'), 0.86),
            ('closed rear', bloom('rear-idle'), 0.86), ('open rear', bloom('rear-open'), 0.86)]
    lineup = floor((1420, 560))
    for k, (name, sprite, width) in enumerate(cast):
        at = (70 + k * 128, 250)
        shadow(lineup, at, width, PHONE)
        paste(lineup, sprite, at, PHONE)
        label(lineup, name, (at[0] - 48, 262))
    label(lineup, "Floor 3 at the phone's scale (120 px a world unit) on GloamPavingV1", (8, 6))
    # Thorn Post: the hero between a closed Bloom (walking in) and an open one, the Gloam Grunt and two Mites.
    scene = [((330, 500), bloom('front-step-a'), 0.96), ((700, 470), Sprite(ART / 'Vanguard/CHR_Vanguard_Front_Idle.tga').mirrored(), 0.78),
             ((1040, 520), bloom('front-open').mirrored(), 0.96),
             ((520, 420), Sprite(ART / 'SlagBrute/Gloam/CHR_Grunt_front-idle.png'), 0.78),
             ((880, 400), Sprite(ART / 'CinderMite/Gloam/CHR_Mite_front_idle.png').mirrored(), 0.45),
             ((1220, 430), Sprite(ART / 'CinderMite/Gloam/CHR_Mite_front_idle.png'), 0.45)]
    for at, sprite, width in sorted(scene, key=lambda s: s[0][1]):
        shadow(lineup, at, width, PHONE)
        paste(lineup, sprite, at, PHONE)
    label(lineup, 'Thorn Post: a closed Bloom walking in (left), the hero, an open Bloom (right), a Grunt and Mites',
          (8, 300))
    lineup.convert('RGB').save(OUT / '4-lineup.png')
    lineup.convert('L').convert('RGB').save(OUT / '4-lineup-value.png')
    # ---- 5. The squint: the Thorn Post moment at half scale, blurred ----
    moment = lineup.crop((200, 310, 1420, 560))
    small = moment.resize((moment.width // 2, moment.height // 2), Image.LANCZOS)
    blurred = small.filter(ImageFilter.GaussianBlur(1.6))
    squint = Image.new('RGBA', (small.width * 2 + 30, small.height + 30), LOAM)
    squint.alpha_composite(small, (10, 24))
    squint.alpha_composite(blurred, (small.width + 20, 24))
    label(squint, 'half scale (a far, small phone view)', (10, 4))
    label(squint, 'half scale, blurred (the glance)', (small.width + 20, 4))
    squint.convert('RGB').save(OUT / '5-squint.png')
    # ---- 6. The bark chip ----
    chip = floor((760, 340))
    for k in range(3):
        at = (130 + k * 250, 300)
        paste(chip, bloom('front-idle'), at, 384 / 2, smooth=False)
        anchor = json.loads((OUT / 'sprites.json').read_text())['furnace_anchor_world_units']['front-idle']
        paste(chip, bloom('bark-chip-%d' % k), (at[0] + anchor[0] * 192, at[1] - anchor[1] * 192), 384 / 2,
              smooth=False)
        label(chip, 'bark chip %d' % k, (at[0] - 40, 310))
    label(chip, 'The bark chip on the struck closed Bloom (at the furnace anchor), 1.5x', (8, 6))
    chip.convert('RGB').save(OUT / '6-bark-chip.png')
    # ---- The numbers ----
    checks = {}

    def stats(sprite):
        px = np.asarray(sprite.image).astype(float) / 255
        a = px[..., 3] > 0.9
        rgb = px[..., :3][a]
        lum = rgb @ np.array([0.2126, 0.7152, 0.0722])
        hsv = np.array([colorsys.rgb_to_hsv(*c) for c in rgb])
        orchid = (hsv[:, 0] > 280 / 360) & (hsv[:, 0] < 330 / 360) & (hsv[:, 1] > 0.25) & (hsv[:, 2] > 0.3)
        warm = (hsv[:, 0] < 30 / 360) & (hsv[:, 1] > 0.5) & (hsv[:, 2] > 0.5)
        chartreuse = (hsv[:, 0] > 60 / 360) & (hsv[:, 0] < 100 / 360) & (hsv[:, 1] > 0.35) & (hsv[:, 2] > 0.45)
        bright = lum > 0.5
        return dict(texels=int(a.sum()), orchid_share=round(float(orchid.mean()), 4),
                    orchid_mean_luminance=round(float(lum[orchid].mean()), 3) if orchid.any() else None,
                    body_mean_luminance=round(float(lum[~orchid].mean()), 3),
                    bright_orchid_share_of_bright=round(float((orchid & bright).sum() / max(1, bright.sum())), 3),
                    warm_texels=int(warm.sum()), chartreuse_texels=int(chartreuse.sum()))

    for name in ('front-idle', 'front-step-a', 'front-windup', 'front-strike', 'rear-idle', 'front-unfold-0',
                 'front-unfold-1', 'front-open', 'rear-open', 'dying-front-0', 'death-cool'):
        checks[name] = stats(bloom(name))
    for name, path in (('grunt_gloam', 'SlagBrute/Gloam/CHR_Grunt_front-idle.png'),
                       ('captain_gloam', 'GruntCaptain/Gloam/CHR_Captain_front-idle.png')):
        checks[name] = stats(Sprite(ART / path))

    def figure(sprite):
        al = np.asarray(sprite.image)[..., 3] > 128
        ys, xs = np.nonzero(al)
        return dict(height=round((sprite.pivot[1] - ys.min()) / PPU, 3), width=round((xs.max() - xs.min() + 1) / PPU, 3))

    checks['figure_units'] = dict(bloom_closed=figure(bloom('front-idle')), bloom_open=figure(bloom('front-open')),
                                  captain=figure(Sprite(ART / 'GruntCaptain/Gloam/CHR_Captain_front-idle.png')),
                                  grunt=figure(Sprite(ART / 'SlagBrute/Gloam/CHR_Grunt_front-idle.png')),
                                  sentry=figure(Sprite(ART / 'MirrorSentry/CHR_MirrorSentry_front-idle.png')),
                                  vanguard=figure(Sprite(ART / 'Vanguard/CHR_Vanguard_Front_Idle.tga')))
    checks['phone_pixels_tall_closed'] = round(checks['figure_units']['bloom_closed']['height'] * PHONE)
    floor_px = np.asarray(floor((256, 256))).astype(float)[..., :3] / 255
    checks['floor_mean_luminance'] = round(float((floor_px @ np.array([0.2126, 0.7152, 0.0722])).mean()), 3)
    sprites = json.loads((OUT / 'sprites.json').read_text())
    checks['astc_4x4_bytes'] = sprites['astc_4x4_bytes']
    checks['astc_4x4_bytes_by_group'] = sprites['astc_4x4_bytes_by_group']
    checks['texture_count'] = sprites['texture_count']
    (OUT / 'checks.json').write_text(json.dumps(checks, indent=1))
    print(json.dumps(checks, indent=1))


if __name__ == '__main__':
    main()
