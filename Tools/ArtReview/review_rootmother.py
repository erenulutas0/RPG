"""Rootmother Myrr's review sheets (docs/64 stage 4): the frames BuildBlenderRootmother.py rendered, lit and laid out for
a look, and the colour checks docs/51 and docs/52 ask of a floor-3 creature.

    python Tools/ArtReview/review_rootmother.py [--render DIR] [--out DIR]

--render: BuildBlenderRootmother.py's --out (default TestResults/rootmother-render); --out: the review folder (default
ArtDirection/2026-11-23/rootmother-01, untracked). Writes:
    sprites/<facing>/<pose>_<variant>.png   each frame at 128 texels a unit: the beauty, box-filtered from the 4x render,
                                            with its glow channels coloured orchid and added (a small halo round them)
    1-clips-front.png, 2-clips-rear.png     every clip's frames, both phases, on the floor's ash loam
    3-phone-scale.png                       idle, telegraph peak and strike at the phone's scale (4.6 world units across
                                            1080 px) beside the Forge Warden, the Glass Regent and the Vanguard, on the
                                            three loam values, and a squint (a quarter size, blurred)
    4-value-map.png                         luminance and the orchid mask of the idle, both phases
    5-clips-front.gif                       the clips played at their seconds (idle, telegraph, strike, phase, death)
    checks.json                             warning red-orange and gloamlight texels (both must be 0), the orchid share,
                                            the body's mean luminance against the loam's, the footprint
Nothing here is generated imagery: it composites the renders.
"""
import colorsys
import json
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ARGV = sys.argv[1:]


def option(name, default):
    return ARGV[ARGV.index(name) + 1] if name in ARGV else default


RENDER = Path(option('--render', str(ROOT / 'TestResults/rootmother-render')))
OUT = Path(option('--out', str(ROOT / 'ArtDirection/2026-11-23/rootmother-01')))

LOAM = {'deep': (0x1D, 0x1F, 0x1A), 'loam': (0x3C, 0x3F, 0x36), 'light': (0x63, 0x66, 0x5A)}
ORCHID = (0xD8, 0x6B, 0xD0)
ORCHID_LIGHT = (0xF6, 0xC2, 0xF0)
VEIN = (0xE8, 0x9B, 0xE0)
# Each glow channel's colour and gain (the pack's tuning, without rendering again).
GLOWS = {0: (ORCHID_LIGHT, 1.25), 1: (ORCHID, 1.35), 2: (VEIN, 0.9)}
HALO = (4, 0.45)              # radius in final texels and gain
PHONE_PX_PER_UNIT = 1080 / 4.6


def load_meta():
    return json.loads((RENDER / 'frames.json').read_text())


def lit(record, ss):
    """The frame at its final size: the beauty box-filtered, the glow channels coloured and added with a halo."""
    beauty = Image.open(RENDER / record['file']).convert('RGBA')
    glow = Image.open(RENDER / record['glow']).convert('RGBA')
    size = (beauty.width // ss, beauty.height // ss)
    beauty = beauty.reduce(ss)
    glow = glow.reduce(ss)
    r, g, b, a = beauty.split()
    channels = glow.split()
    add = Image.new('RGB', size, (0, 0, 0))
    halo_alpha = Image.new('L', size, 0)
    for index, (colour, gain) in GLOWS.items():
        mask = channels[index].point(lambda v, gain=gain: min(255, int(v * 2 * gain)))
        layer = Image.new('RGB', size, colour)
        add = ImageChops.add(add, ImageChops.multiply(layer, Image.merge('RGB', (mask, mask, mask))))
        halo = mask.filter(ImageFilter.GaussianBlur(HALO[0])).point(lambda v: int(v * HALO[1]))
        add = ImageChops.add(add, ImageChops.multiply(layer, Image.merge('RGB', (halo, halo, halo))))
        halo_alpha = ImageChops.lighter(halo_alpha, halo)
    rgb = ImageChops.add(Image.merge('RGB', (r, g, b)), add)
    alpha = ImageChops.lighter(a, halo_alpha.point(lambda v: min(255, int(v * 1.4))))
    out = Image.merge('RGBA', (*rgb.split(), alpha))
    return out


def on(colour, sprite, pad=0):
    bg = Image.new('RGBA', (sprite.width + 2 * pad, sprite.height + 2 * pad), colour + (255,))
    bg.alpha_composite(sprite, (pad, pad))
    return bg


def font(size):
    for name in ('arial.ttf', 'DejaVuSans.ttf'):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default()


def clips_sheet(sprites, facing, title):
    clips = [('idle', ['idle-a', 'idle-b']), ('telegraph', ['windup-0', 'windup-1', 'windup-2']),
             ('root strike', ['strike-0', 'strike-1', 'strike-2']), ('phase change', ['phase-0', 'phase-1', 'phase-2']),
             ('death', ['fall-struck', 'fall-sink', 'fall-slump', 'fall-stump'])]
    cell = (224, 256)
    cols = 8
    rows = []
    for name, poses in clips:
        row = []
        for variant in ('cool', 'hot'):
            for pose in poses:
                key = (facing, pose, variant)
                if key not in sprites and pose in ('fall-slump', 'fall-stump'):
                    key = ('front', pose, variant)
                if key in sprites:
                    row.append((pose + (' (front)' if key[0] != facing else ''), variant, sprites[key]))
        rows.append((name, row))
    width = 150 + cols * cell[0]
    sheet = Image.new('RGBA', (width, 60 + len(rows) * (cell[1] + 26)), (24, 26, 22, 255))
    draw = ImageDraw.Draw(sheet)
    draw.text((12, 14), title, fill=(230, 226, 214), font=font(26))
    for r, (name, row) in enumerate(rows):
        y = 60 + r * (cell[1] + 26)
        draw.text((12, y + cell[1] // 2), name, fill=(230, 226, 214), font=font(20))
        for c, (pose, variant, sprite) in enumerate(row[:cols]):
            x = 150 + c * cell[0]
            tile = on(LOAM['loam'], sprite.resize(cell, Image.LANCZOS))
            sheet.alpha_composite(tile, (x, y))
            draw.text((x + 6, y + cell[1] + 4), '%s %s' % (pose, variant), fill=(200, 196, 186), font=font(14))
    return sheet


def phone_sheet(sprites):
    scale = PHONE_PX_PER_UNIT / 128.0
    refs = []
    chars = ROOT / 'Assets/_Project/Art/Characters'
    for label, path in (('Forge Warden', chars / 'ForgeWarden/CHR_Warden_front_idle.png'),
                        ('Glass Regent', chars / 'GlassRegent/CHR_GlassRegent_front_idle.png'),
                        ('Vanguard', chars / 'Vanguard/CHR_Vanguard_Front_Idle.tga')):
        if path.exists():
            refs.append((label, Image.open(path).convert('RGBA')))
    shown = [('Myrr idle', sprites.get(('front', 'idle-a', 'cool'))), ('Myrr idle, phase 2', sprites.get(('front', 'idle-a', 'hot'))),
             ('telegraph peak', sprites.get(('front', 'windup-2', 'cool'))), ('root strike', sprites.get(('front', 'strike-1', 'cool')))]
    shown = [(l, s) for l, s in shown if s is not None]
    panels = []
    for tone in ('deep', 'loam', 'light'):
        items = [(l, s.crop(s.getbbox())) for l, s in shown] + refs
        scaled = [(l, s.resize((max(1, int(s.width * scale)), max(1, int(s.height * scale))), Image.LANCZOS)) for l, s in items]
        h = max(s.height for _, s in scaled) + 60
        w = sum(s.width + 30 for _, s in scaled) + 30
        panel = Image.new('RGBA', (w, h), LOAM[tone] + (255,))
        draw = ImageDraw.Draw(panel)
        x = 30
        for label, s in scaled:
            panel.alpha_composite(s, (x, h - 40 - s.height))
            draw.text((x, h - 34), label, fill=(235, 232, 222), font=font(18))
            x += s.width + 30
        panels.append(panel)
    width = max(p.width for p in panels)
    squint_src = panels[1]
    squint = squint_src.resize((squint_src.width // 4, squint_src.height // 4), Image.BOX).filter(ImageFilter.GaussianBlur(1.2))
    squint = squint.resize((squint.width * 2, squint.height * 2), Image.NEAREST)
    sheet = Image.new('RGBA', (max(width, squint.width), sum(p.height for p in panels) + squint.height + 80), (20, 22, 18, 255))
    y = 40
    draw = ImageDraw.Draw(sheet)
    draw.text((20, 8), 'Phone scale (S23: 4.6 world units across 1080 px), on deep / mid / light ash loam; squint below',
              fill=(230, 226, 214), font=font(20))
    for p in panels:
        sheet.alpha_composite(p, (0, y))
        y += p.height
    sheet.alpha_composite(squint, (0, y + 30))
    return sheet


def value_map(sprites):
    tiles = []
    for variant in ('cool', 'hot'):
        s = sprites.get(('front', 'idle-a', variant))
        if s is None:
            continue
        lum = s.convert('L')
        alpha = s.split()[3]
        grey = Image.merge('RGBA', (lum, lum, lum, alpha))
        orchid = Image.new('RGBA', s.size, (0, 0, 0, 0))
        px = s.load()
        out = orchid.load()
        for y in range(s.height):
            for x in range(s.width):
                r, g, b, a = px[x, y]
                if a < 128:
                    continue
                h, sat, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
                if 280 / 360 <= h <= 330 / 360 and sat > 0.25 and v > 0.35:
                    out[x, y] = (255, 255, 255, 255)
        tiles += [on(LOAM['loam'], grey), on((0, 0, 0), orchid)]
    sheet = Image.new('RGBA', (sum(t.width for t in tiles), max(t.height for t in tiles)), (0, 0, 0, 255))
    x = 0
    for t in tiles:
        sheet.alpha_composite(t, (x, 0))
        x += t.width
    return sheet


def checks(sprites, meta):
    warning = gloam = orchid = opaque = 0
    lum_sum = 0.0
    body = 0
    for key, s in sprites.items():
        px = s.load()
        for y in range(0, s.height):
            for x in range(0, s.width):
                r, g, b, a = px[x, y]
                if a < 128:
                    continue
                opaque += 1
                h, sat, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
                hue = h * 360
                if (hue <= 25 or hue >= 350) and sat > 0.45 and v > 0.35:
                    warning += 1
                if 65 <= hue <= 95 and sat > 0.35 and v > 0.5:
                    gloam += 1
                if 280 <= hue <= 330 and sat > 0.25 and v > 0.35:
                    orchid += 1
                if key[1] == 'idle-a' and key[2] == 'cool' and key[0] == 'front':
                    lum_sum += 0.2126 * r + 0.7152 * g + 0.0722 * b
                    body += 1
    idle = sprites.get(('front', 'idle-a', 'cool'))
    box = idle.getbbox() if idle else None
    loam = LOAM['loam']
    return dict(frames=len(sprites), warning_red_orange_texels=warning, gloamlight_texels=gloam,
                orchid_share=round(orchid / max(1, opaque), 4),
                idle_mean_luminance=round(lum_sum / max(1, body) / 255, 3),
                loam_luminance=round((0.2126 * loam[0] + 0.7152 * loam[1] + 0.0722 * loam[2]) / 255, 3),
                idle_bbox_px=box, idle_size_units=[round((box[2] - box[0]) / 128, 3), round((box[3] - box[1]) / 128, 3)] if box else None,
                pivot_px=meta['pivot'], canvas_px=meta['canvas'])


def gif(sprites, facing):
    order = [('idle-a', 'cool'), ('idle-b', 'cool')] * 2 + [('windup-0', 'cool'), ('windup-1', 'cool'), ('windup-2', 'cool'),
             ('strike-0', 'cool'), ('strike-1', 'cool'), ('strike-2', 'cool'), ('idle-a', 'cool'), ('phase-0', 'cool'),
             ('phase-1', 'hot'), ('phase-2', 'hot'), ('idle-a', 'hot'), ('idle-b', 'hot'), ('windup-2', 'hot'),
             ('strike-1', 'hot'), ('fall-struck', 'hot'), ('fall-sink', 'hot'), ('fall-slump', 'hot'), ('fall-stump', 'hot')]
    seconds = {'idle': 600, 'windup': 400, 'strike': 140, 'phase': 300, 'fall': 380}
    frames, durations = [], []
    for pose, variant in order:
        key = (facing, pose, variant)
        if key not in sprites:
            key = ('front', pose, variant)
        if key not in sprites:
            continue
        s = on(LOAM['loam'], sprites[key]).convert('RGB').resize((224, 256), Image.LANCZOS)
        frames.append(s.convert('P', palette=Image.ADAPTIVE, colors=128))
        durations.append(seconds[pose.split('-')[0]] if pose != 'fall-stump' else 1200)
    return frames, durations


def main():
    meta = load_meta()
    ss = meta['supersample']
    sprites = {}
    for record in meta['frames']:
        s = lit(record, ss)
        key = (record['facing'], record['pose'], record['variant'])
        sprites[key] = s
        path = OUT / 'sprites' / record['facing'] / ('%s_%s.png' % (record['pose'], record['variant']))
        path.parent.mkdir(parents=True, exist_ok=True)
        s.save(path)
    clips_sheet(sprites, 'front', 'Rootmother Myrr - front (cool: phase one, hot: phase two)').save(OUT / '1-clips-front.png')
    clips_sheet(sprites, 'rear', 'Rootmother Myrr - rear (slump and stump: the front\'s drawings)').save(OUT / '2-clips-rear.png')
    phone_sheet(sprites).save(OUT / '3-phone-scale.png')
    value_map(sprites).save(OUT / '4-value-map.png')
    frames, durations = gif(sprites, 'front')
    if frames:
        frames[0].save(OUT / '5-clips-front.gif', save_all=True, append_images=frames[1:], duration=durations, loop=0)
    result = checks(sprites, meta)
    (OUT / 'checks.json').write_text(json.dumps(result, indent=1))
    print('ROOTMOTHER_REVIEW', json.dumps(result))


if __name__ == '__main__':
    main()
