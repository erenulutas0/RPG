"""Review sheets for floor 3's horde creatures (PackGloamHordeSprites.py): exactly the PNG pixels Unity imports, placed
by their .meta pivots, on floor 3's ash loam (GloamPavingV1).

    python Tools/ArtReview/review_gloam_horde.py [--out DIR] [--suffix TEXT]

Writes to DIR (default E:/MobileGame/ArtDirection/2026-11-15/gloam-horde-01, the main checkout's, untracked):
    1-wraith.png        the Pollen Wraith at 3x: the five bodies in each facing and mirrored, its own death, the two
                        remains, the pollen motes and the pollen bloom
    2-thornling.png     the Thornling at 2x: the six bodies and the windup in each facing, its own death, the remains
    3-thornling-dive.png the dive, the eruption and the stuck pose in each facing at 2x, the ash puff, the thorn burst,
                        and the ring under a hero at the phone's scale - orchid filling, then its last second red-orange
    4-crowd.png         at the phone's scale (the Samsung S23 shows 120 pixels a world unit): a floor-3 horde of 32
                        Wraiths, four Thornlings (one stuck at a ring, one diving), three Grunts and the Cinder Bloom
                        round the Vanguard, and Ilya among Wraiths beside it; contact shadows as the game draws them
    4-crowd-value.png   the same in luminance only
    5-squint.png        the crowd at half the phone's scale, sharp and blurred (the glance)
    checks.json         orchid share, warm (red-orange) and chartreuse texels on every body (both 0), luminance against
                        the loam and Ilya's robe, heights against the Mite and the Runner, texture memory
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
OUT = Path(ARGS[ARGS.index('--out') + 1]) if '--out' in ARGS else Path('E:/MobileGame/ArtDirection/2026-11-15/gloam-horde-01')
SUFFIX = ARGS[ARGS.index('--suffix') + 1] if '--suffix' in ARGS else ''
OUT.mkdir(parents=True, exist_ok=True)
ART = ROOT / 'Assets/_Project/Art/Characters'
WRAITH = ART / 'PollenWraith'
THORN = ART / 'Thornling'
PAVING = ROOT / 'Assets/_Project/Art/Environment/Gloam/GloamPavingV1.png'
ILYA_FRAMES = Path('E:/MobileGame/ArtDirection/2026-10-27/arcanist-blender-01/frames.json')
PHONE = 120
PPU = 128
LOAM = (0x3C, 0x3F, 0x36, 255)


class Sprite:
    def __init__(self, path=None, image=None, pivot=None, ppu=PPU):
        self.ppu = ppu
        if path is not None:
            self.image = Image.open(path).convert('RGBA')
            meta = Path(str(path) + '.meta').read_text(encoding='utf-8')
            x, y = (float(v) for v in re.search(r'spritePivot: \{x: ([-\d.]+), y: ([-\d.]+)\}', meta).groups())
            self.pivot = (x * self.image.width, (1 - y) * self.image.height)
            m = re.search(r'spritePixelsToUnits: (\d+)', meta)
            self.ppu = float(m.group(1)) if m else ppu
        else:
            self.image, self.pivot = image, pivot

    def mirrored(self):
        return Sprite(image=self.image.transpose(Image.FLIP_LEFT_RIGHT),
                      pivot=(self.image.width - self.pivot[0], self.pivot[1]), ppu=self.ppu)

    def tinted(self, alpha):
        im = self.image.copy()
        a = np.asarray(im).copy()
        a[..., 3] = (a[..., 3] * alpha).astype(np.uint8)
        return Sprite(image=Image.fromarray(a), pivot=self.pivot, ppu=self.ppu)


def wraith(name):
    return Sprite(WRAITH / ('CHR_PollenWraith_%s.png' % name))


def thorn(name):
    return Sprite(THORN / ('CHR_Thornling_%s.png' % name))


def ilya(name='idle_south-west_0'):
    report = json.loads(ILYA_FRAMES.read_text())
    pages = [Image.open(ART / 'CinderArcanist' / a['file']).convert('RGBA') for a in report['atlases']]
    for f in report['frames']:
        if f['name'] == name:
            page = pages[f['atlas']]
            x, y, w, h = f['rect']
            top = page.height - y - h
            return Sprite(image=page.crop((x, top, x + w, top + h)), pivot=(f['pivot'][0] * w, (1 - f['pivot'][1]) * h),
                          ppu=report['pixels_per_unit'])
    for f in report['frames']:
        if f['name'].startswith('idle_south_'):
            return ilya(f['name'])
    raise KeyError(name)


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
    canvas.alpha_composite(layer.filter(ImageFilter.GaussianBlur(max(0.5, scale * 0.04))))


def paste(canvas, sprite, at, scale, smooth=True, size_scale=1.0):
    k = scale / sprite.ppu * size_scale
    size = (max(1, round(sprite.image.width * k)), max(1, round(sprite.image.height * k)))
    image = sprite.image.resize(size, Image.BILINEAR if smooth else Image.NEAREST)
    canvas.alpha_composite(image, (round(at[0] - sprite.pivot[0] * k), round(at[1] - sprite.pivot[1] * k)))


def label(canvas, text, at):
    d = ImageDraw.Draw(canvas)
    d.text((at[0] + 1, at[1] + 1), text, fill=(0, 0, 0, 255))
    d.text(at, text, fill=(236, 240, 228, 255))


def strip(items, scale, height, gap, title, foot=40, cell=None):
    cell = cell or max(max((s.image.width * scale / s.ppu) for _, s in items if s is not None), 60)
    width = int(len(items) * (cell + gap) + gap)
    canvas = floor((width, height))
    for k, (text, sprite) in enumerate(items):
        x = gap + k * (cell + gap)
        at = (x + cell / 2, height - foot)
        if sprite is not None:
            paste(canvas, sprite, at, scale, smooth=False)
        label(canvas, text, (x, height - foot + 14))
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


def ring(canvas, at, radius, fill, colouring, scale):
    """The ring under the hero as the runtime would lay it: the fill scaled from the centre, the edge at the radius."""
    edge = thorn('ring-edge-%s' % colouring)
    filler = thorn('ring-fill-%s' % colouring)
    if fill > 0:
        paste(canvas, filler, at, scale, size_scale=radius * fill)
    paste(canvas, edge, at, scale, size_scale=radius)


def main():
    # ---- 1. The Wraith ----
    rows = []
    for facing in ('front', 'rear'):
        poses = ['idle', 'step-a', 'step-b', 'windup', 'strike']
        items = [(p, wraith('%s-%s' % (facing, p))) for p in poses] + \
                [(p + ' (R)', wraith('%s-%s' % (facing, p)).mirrored()) for p in ('idle', 'strike')]
        rows.append(strip(items, 384, 300, 12, 'Pollen Wraith, %s: idle, the drift (step A, step B), the windup, the snap; '
                          'mirrored for the right (3x)' % facing))
    for facing in ('front', 'rear'):
        rows.append(strip([('dying %d' % k, wraith('dying-%s-%d' % (facing, k))) for k in range(7)], 384, 300, 12,
                          'Its own death, %s: struck, the puff bursts, the wisps fly, the husk falls and greys (3x)' % facing))
    rows.append(strip([('husk', wraith('death-husk')), ('dust', wraith('death-dust'))] +
                      [('mote %d' % k, wraith('pollen-mote-%d' % k)) for k in range(3)] +
                      [('bloom %d' % k, wraith('pollen-bloom-%d' % k)) for k in range(4)], 384, 360, 12,
                      'The two remains; the pollen motes at a still hero\'s feet; the pollen bloom at the hero\'s feet (3x)'))
    stack(rows, OUT / ('1-wraith%s.png' % SUFFIX))
    # ---- 2. The Thornling ----
    rows = []
    for facing in ('front', 'rear'):
        poses = ['idle', 'step-a', 'pass-a', 'step-b', 'pass-b', 'windup', 'strike']
        rows.append(strip([(p, thorn('%s-%s' % (facing, p))) for p in poses], 256, 340, 12,
                          'Thornling, %s: idle, the lope (step, pass), the rake drawn back and struck (2x)' % facing))
    for facing in ('front', 'rear'):
        rows.append(strip([('dying %d' % k, thorn('dying-%s-%d' % (facing, k))) for k in range(5)] +
                          ([('collapse', thorn('death-collapse')), ('cool', thorn('death-cool'))] if facing == 'front' else []),
                          256, 300, 12, 'Its own death, %s, and the two remains (2x)' % facing))
    stack(rows, OUT / ('2-thornling%s.png' % SUFFIX))
    # ---- 3. The dive, the eruption, stuck, and the ring ----
    rows = []
    for facing in ('front', 'rear'):
        poses = ['dive-0', 'dive-1', 'dive-2', 'erupt-0', 'erupt-1', 'erupt-2', 'stuck-0', 'stuck-1']
        rows.append(strip([(p, thorn('%s-%s' % (facing, p))) for p in poses], 256, 340, 12,
                          'Thornling, %s: dives head first, erupts, sticks - the ash ring painted where it meets the floor (2x)'
                          % facing))
    rows.append(strip([('ash puff %d' % k, thorn('ash-puff-%d' % k)) for k in range(3)] +
                      [('thorn burst %d' % k, thorn('thorn-burst-%d' % k)) for k in range(3)], 256, 300, 16,
                      'The dive\'s ash puff and the eruption\'s thorn burst (2x)'))
    # The ring at the phone's scale: the hero standing in it as it fills (orchid), its last second (red-orange),
    # the eruption under the hero, the Thornling stuck beside the hero after it.
    seq = floor((1300, 300))
    vanguard = Sprite(ART / 'Vanguard/CHR_Vanguard_Front_Idle.tga')
    steps = [('ring down 0.0 s', 0.08, 'orchid', None), ('0.3 s', 0.4, 'orchid', None), ('0.55 s', 0.72, 'orchid', None),
             ('last second: 0.7 s', 0.9, 'warning', None), ('erupts 0.8 s', 1.0, 'warning', 'erupt'),
             ('stuck 1.0 s', 0.0, None, 'stuck')]
    for k, (text, fill, colouring, event) in enumerate(steps):
        at = (110 + k * 210, 210)
        if colouring:
            ring(seq, at, 1.2, fill, colouring, PHONE)
        hero_at = (at[0] + (60 if event else 0), at[1] - (10 if event else 0))
        if event == 'erupt':
            paste(seq, thorn('thorn-burst-1'), at, PHONE)
            paste(seq, thorn('front-erupt-2'), at, PHONE)
        elif event == 'stuck':
            paste(seq, thorn('front-stuck-0'), (at[0] - 20, at[1]), PHONE)
            hero_at = (at[0] + 80, at[1] - 20)
        shadow(seq, hero_at, 0.78, PHONE)
        paste(seq, vanguard.mirrored(), hero_at, PHONE)
        label(seq, text, (at[0] - 70, 270))
    dive = (110 + 6 * 210 - 40, 210)
    label(seq, 'The ring (r 1.2) at the phone\'s scale: orchid while it fills, red-orange only in its last second', (8, 6))
    rows.append(seq)
    dives = floor((1300, 200))
    for k, name in enumerate(['front-idle', 'front-dive-0', 'front-dive-1', 'front-dive-2']):
        at = (90 + k * 150, 150)
        if k >= 1:
            paste(dives, thorn('ash-puff-%d' % (k - 1)), at, PHONE)
        else:
            shadow(dives, at, 0.5, PHONE)
        paste(dives, thorn(name), at, PHONE)
    for k, name in enumerate(['front-erupt-0', 'front-erupt-1', 'front-erupt-2', 'front-stuck-0', 'front-stuck-1']):
        at = (700 + k * 130, 150)
        if k < 3:
            paste(dives, thorn('thorn-burst-%d' % k), at, PHONE)
        paste(dives, thorn(name), at, PHONE)
    label(dives, 'At the phone\'s scale: walking, the dive with its ash puff; the eruption with its thorn burst, stuck', (8, 6))
    rows.append(dives)
    stack(rows, OUT / ('3-thornling-dive%s.png' % SUFFIX))
    # ---- 4. The crowd at the phone's scale ----
    W, H = 1400, 760
    crowd = floor((W, H))
    rng = np.random.default_rng(3)
    cast = []
    hero_at = (560, 430)
    cast.append((hero_at, vanguard.mirrored(), 0.78))
    poses = ['idle', 'step-a', 'step-b', 'strike', 'windup']
    placed = [hero_at]
    count = 0
    while count < 32:
        a = rng.uniform(0, 2 * np.pi)
        r = rng.uniform(0.9, 3.6) * PHONE
        at = (hero_at[0] + r * np.cos(a), hero_at[1] + 0.55 * r * np.sin(a))
        if not (40 < at[0] < 1000 and 120 < at[1] < H - 30):
            continue
        if any(np.hypot(at[0] - q[0], (at[1] - q[1]) * 1.6) < 44 for q in placed):
            continue
        placed.append(at)
        facing = 'front' if at[1] >= hero_at[1] - 30 else 'rear'
        pose = poses[int(rng.integers(0, 3))] if r > 1.3 * PHONE else poses[3 + int(rng.integers(0, 2))]
        s = wraith('%s-%s' % (facing, pose))
        if at[0] < hero_at[0]:
            s = s.mirrored()
        cast.append((at, s, 0.34))
        count += 1
    for at, name, mirror, width in [((330, 300), 'rear-step-a', True, 0.5), ((820, 560), 'front-pass-b', False, 0.5),
                                    ((240, 610), 'front-step-b', True, 0.5)]:
        s = thorn(name)
        cast.append((at, s.mirrored() if mirror else s, width))
    # A Thornling stuck beside a ring that just erupted (orchid gone), one diving out at the edge.
    ring_at = (700, 400)
    stuck_at = (690, 405)
    cast.append((stuck_at, thorn('front-stuck-1').mirrored(), None))
    cast.append(((900, 250), thorn('rear-dive-1'), None))
    for at in [(180, 420), (930, 420), (420, 650)]:
        cast.append((at, Sprite(ART / 'SlagBrute/Gloam/CHR_Grunt_front-idle.png').mirrored() if at[0] < 560 else
                     Sprite(ART / 'SlagBrute/Gloam/CHR_Grunt_front-idle.png'), 0.78))
    cast.append(((860, 690), Sprite(ART / 'CinderBloom/CHR_CinderBloom_front-step-a.png'), 0.86))
    paste(crowd, thorn('thorn-burst-2'), ring_at, PHONE)
    for at, sprite, width in sorted(cast, key=lambda c: c[0][1]):
        if width:
            shadow(crowd, at, width, PHONE)
        paste(crowd, sprite, at, PHONE)
    # Ilya among Wraiths at the right: pale robe, pale orchid motes.
    ilya_at = (1210, 430)
    ilya_cast = [(ilya_at, ilya(), 0.78)]
    for k, (dx, dy) in enumerate([(-80, -60), (70, -50), (-95, 20), (90, 30), (-40, 70), (50, 80), (0, -95),
                                  (-120, -10), (120, -5), (25, 45)]):
        facing = 'front' if dy > -10 else 'rear'
        s = wraith('%s-%s' % (facing, ('idle', 'step-a', 'step-b', 'strike', 'windup')[k % 5]))
        ilya_cast.append(((ilya_at[0] + dx, ilya_at[1] + dy), s.mirrored() if dx < 0 else s, 0.34))
    for at, sprite, width in sorted(ilya_cast, key=lambda c: c[0][1]):
        shadow(crowd, at, width, PHONE)
        paste(crowd, sprite, at, PHONE)
    label(crowd, 'Floor 3\'s horde at the phone\'s scale (120 px a world unit) on GloamPavingV1: 32 Pollen Wraiths, '
                 'Thornlings (one stuck after its eruption, one diving), Grunts, the Cinder Bloom, the Vanguard; Ilya among '
                 'Wraiths (right)', (8, 6))
    crowd.convert('RGB').save(OUT / ('4-crowd%s.png' % SUFFIX))
    crowd.convert('L').convert('RGB').save(OUT / ('4-crowd-value%s.png' % SUFFIX))
    # ---- 5. The squint ----
    small = crowd.resize((W // 2, H // 2), Image.LANCZOS)
    blurred = small.filter(ImageFilter.GaussianBlur(1.6))
    squint = Image.new('RGBA', (small.width * 2 + 30, small.height + 30), LOAM)
    squint.alpha_composite(small, (10, 24))
    squint.alpha_composite(blurred, (small.width + 20, 24))
    label(squint, 'half scale (a far, small phone view)', (10, 4))
    label(squint, 'half scale, blurred (the glance)', (small.width + 20, 4))
    squint.convert('RGB').save(OUT / ('5-squint%s.png' % SUFFIX))
    # ---- The numbers ----
    checks = {}

    def stats(sprite):
        px = np.asarray(sprite.image).astype(float) / 255
        a = px[..., 3] > 0.9
        rgb = px[..., :3][a]
        lum = rgb @ np.array([0.2126, 0.7152, 0.0722])
        hsv = np.array([colorsys.rgb_to_hsv(*c) for c in rgb])
        orchid = (hsv[:, 0] > 280 / 360) & (hsv[:, 0] < 330 / 360) & (hsv[:, 1] > 0.25) & (hsv[:, 2] > 0.3)
        warm = ((hsv[:, 0] < 30 / 360) | (hsv[:, 0] > 345 / 360)) & (hsv[:, 1] > 0.5) & (hsv[:, 2] > 0.5)
        chartreuse = (hsv[:, 0] > 60 / 360) & (hsv[:, 0] < 100 / 360) & (hsv[:, 1] > 0.35) & (hsv[:, 2] > 0.45)
        return dict(texels=int(a.sum()), orchid_share=round(float(orchid.mean()), 4),
                    mean_luminance=round(float(lum.mean()), 3),
                    mean_saturation=round(float(hsv[:, 1].mean()), 3),
                    warm_texels=int(warm.sum()), chartreuse_texels=int(chartreuse.sum()))

    names_w = [p.name[len('CHR_PollenWraith_'):-4] for p in sorted(WRAITH.glob('CHR_PollenWraith_*.png'))
               if '_Flash' not in p.name and 'pollen-' not in p.name]
    names_t = [p.name[len('CHR_Thornling_'):-4] for p in sorted(THORN.glob('CHR_Thornling_*.png'))
               if '_Flash' not in p.name and not p.name[len('CHR_Thornling_'):].startswith(('ring-', 'ash-', 'thorn-'))]
    checks['wraith'] = {n: stats(wraith(n)) for n in names_w}
    checks['thornling'] = {n: stats(thorn(n)) for n in names_t}
    checks['bodies_with_warm_or_chartreuse'] = [n for group in ('wraith', 'thornling') for n, s in checks[group].items()
                                                if s['warm_texels'] or s['chartreuse_texels']]
    checks['effects'] = {n: stats(wraith(n)) for n in ['pollen-mote-0', 'pollen-bloom-1']}
    checks['effects'].update({n: stats(thorn(n)) for n in ['ash-puff-1', 'thorn-burst-1', 'ring-edge-orchid',
                                                          'ring-edge-warning']})
    checks['ilya_idle'] = stats(ilya())
    checks['gloam_mite_idle'] = stats(Sprite(ART / 'CinderMite/Gloam/CHR_Mite_front_idle.png'))
    checks['gloam_runner_idle'] = stats(Sprite(ART / 'Runner/Gloam/CHR_Runner_front-idle.png'))

    def figure(sprite):
        al = np.asarray(sprite.image)[..., 3] > 128
        ys, xs = np.nonzero(al)
        return dict(top=round((sprite.pivot[1] - ys.min()) / sprite.ppu, 3), width=round((xs.max() - xs.min() + 1) / sprite.ppu, 3),
                    bottom=round((sprite.pivot[1] - ys.max()) / sprite.ppu, 3))

    checks['figure_units'] = dict(wraith=figure(wraith('front-idle')), thornling=figure(thorn('front-idle')),
                                  mite=figure(Sprite(ART / 'CinderMite/CHR_Mite_front_idle.tga')),
                                  runner=figure(Sprite(ART / 'Runner/CHR_Runner_front-idle.tga')),
                                  vanguard=figure(vanguard))
    floor_px = np.asarray(floor((256, 256))).astype(float)[..., :3] / 255
    checks['floor_mean_luminance'] = round(float((floor_px @ np.array([0.2126, 0.7152, 0.0722])).mean()), 3)
    for creature, folder in (('wraith', 'pollen-wraith'), ('thornling', 'thornling')):
        sprites = json.loads((OUT / folder / 'sprites.json').read_text())
        checks['%s_memory' % creature] = dict(textures=sprites['texture_count'], astc_4x4_bytes=sprites['astc_4x4_bytes'],
                                              by_group=sprites['astc_4x4_bytes_by_group'],
                                              set_textures=sprites['set_texture_count'],
                                              set_astc_4x4_bytes=sprites['set_astc_4x4_bytes'])
    (OUT / ('checks%s.json' % SUFFIX)).write_text(json.dumps(checks, indent=1))
    summary = {k: v for k, v in checks.items() if k not in ('wraith', 'thornling')}
    summary['wraith_idle'] = checks['wraith']['front-idle']
    summary['thornling_idle'] = checks['thornling']['front-idle']
    print(json.dumps(summary, indent=1))


if __name__ == '__main__':
    main()
