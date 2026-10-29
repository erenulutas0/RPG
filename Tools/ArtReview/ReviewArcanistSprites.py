"""Review material for Ilya Kest's packed sprites (PackArcanistSprites.py): exactly the atlas pixels Unity imports.

    python Tools/ArtReview/ReviewArcanistSprites.py --out DIR [--before DIR]

Writes to DIR: one animated GIF per motion with all eight game directions (drawn as RenderedCharacterAnimator draws
them: the west three as rendered when the set has them, else the east drawings mirrored), a contact sheet per motion,
a turnaround, Ilya beside the painted Vanguard at the phone's scale (the Samsung S23 shows 120 pixels a world unit:
1080 pixels across a 9-unit view) and zoomed, on the Foundry's dark stone (cropped from an in-game capture) and on
molten orange (a patch in the pit's sampled colours), an eight-direction walk at the phone's scale on both floors, and
a sheet of every west frame.

--before DIR names an earlier pack (its frames.json beside its atlases, as PackArcanistSprites.py wrote them) to
compare with: the turn from east to west before and now, and the strike and the Vent's cast to the west before and now.
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ARGS = sys.argv[1:]
OUT = Path(ARGS[ARGS.index('--out') + 1]) if '--out' in ARGS else ROOT / 'TestResults/arcanist-review'
BEFORE = Path(ARGS[ARGS.index('--before') + 1]) if '--before' in ARGS else None
OUT.mkdir(parents=True, exist_ok=True)
ART = ROOT / 'Assets/_Project/Art/Characters/CinderArcanist'
VANGUARD = ROOT / 'Assets/_Project/Art/Characters/Vanguard'
CAPTURE = ROOT / 'ArtDirection/2026-10-24/foundry-pit-eruption-01/pit-spill.png'
PHONE = 120          # S23 pixels per world unit
MOTIONS = ['idle', 'walk', 'walk-strike', 'strike', 'cast', 'hit', 'death']
# The game's eight directions round the compass from the camera (label, facing) and the east twin a set of only the
# five east facings mirrors for a west one (RenderedCharacterFrames.EastMirrorOf).
COMPASS = [('S', 'south'), ('SE', 'south-east'), ('E', 'east'), ('NE', 'north-east'), ('N', 'north'),
           ('NW', 'north-west'), ('W', 'west'), ('SW', 'south-west')]
EAST_TWIN = {'north-west': 'north-east', 'west': 'east', 'south-west': 'south-east'}


class Pack:
    """One packed set: its frames cut from its atlases, each with its pivot in the frame's pixels."""

    def __init__(self, report_path, atlas_dir):
        report = json.loads(Path(report_path).read_text())
        self.ppu = report['pixels_per_unit']
        pages = [Image.open(Path(atlas_dir) / a['file']).convert('RGBA') for a in report['atlases']]
        self.frames = {}
        for f in report['frames']:
            page = pages[f['atlas']]
            x, y, w, h = f['rect']
            top = page.height - y - h
            self.frames[f['name']] = (page.crop((x, top, x + w, top + h)), (f['pivot'][0] * w, (1 - f['pivot'][1]) * h))
        self.draws_west = all('%s_%s_0' % (m, f) in self.frames for m in MOTIONS for f in EAST_TWIN)

    def drawn(self, facing):
        """(facing drawn, mirrored) for a direction, as RenderedCharacterAnimator shows it."""
        if facing in EAST_TWIN and not self.draws_west:
            return EAST_TWIN[facing], True
        return facing, False

    def frame(self, motion, facing, k):
        drawn, mirrored = self.drawn(facing)
        image, (px, py) = self.frames['%s_%s_%d' % (motion, drawn, k)]
        if mirrored:
            image = image.transpose(Image.FLIP_LEFT_RIGHT)
            px = image.width - px
        return image, (px, py)

    def count(self, motion):
        return sum(1 for name in self.frames if name.startswith(motion + '_south_'))


NOW = Pack(ROOT / 'ArtDirection/2026-10-27/arcanist-blender-01/frames.json', ART)
PPU = NOW.ppu
OLD = Pack(BEFORE / 'frames.json', BEFORE) if BEFORE else None
DIRECTIONS = [(label, facing, NOW.drawn(facing)[1]) for label, facing in COMPASS]


def frame(motion, facing, k, mirrored=False):
    image, (px, py) = NOW.frames['%s_%s_%d' % (motion, facing, k)]
    if mirrored:
        image = image.transpose(Image.FLIP_LEFT_RIGHT)
        px = image.width - px
    return image, (px, py)


def count(motion):
    return NOW.count(motion)


def paste(canvas, image, pivot, at, scale=1.0, smooth=True):
    if scale != 1.0:
        size = (max(1, round(image.width * scale)), max(1, round(image.height * scale)))
        image = image.resize(size, Image.BILINEAR if smooth else Image.NEAREST)
        pivot = (pivot[0] * scale, pivot[1] * scale)
    canvas.alpha_composite(image, (round(at[0] - pivot[0]), round(at[1] - pivot[1])))


def shadow(canvas, at, ppu, width=0.78):
    """The game's contact shadow under a hero (ContactShadowView: 0.78 units wide, on the 2:1 floor)."""
    layer = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
    rx, ry = width * ppu / 2, width * ppu / 4
    ImageDraw.Draw(layer).ellipse((at[0] - rx, at[1] - ry, at[0] + rx, at[1] + ry), fill=(8, 6, 12, 110))
    canvas.alpha_composite(layer.filter(ImageFilter.GaussianBlur(ppu * 0.04)))


def stone(size):
    capture = Image.open(CAPTURE).convert('RGBA')
    tile = capture.crop((0, 1330, 560, 1700))
    out = Image.new('RGBA', size)
    for y in range(0, size[1], tile.height):
        for x in range(0, size[0], tile.width):
            out.paste(tile, (x, y))
    return out


def lava(size, seed=3):
    """Molten orange in the pit's sampled colours (pit-spill.png: 201,37,1 to 253,52,2, crests 255,167,15)."""
    rng = np.random.default_rng(seed)
    h, w = size[1], size[0]
    field = np.zeros((h, w))
    for scale, weight in [(48, 0.55), (20, 0.3), (8, 0.15)]:
        coarse = rng.random((h // scale + 2, w // scale + 2))
        field += weight * np.asarray(Image.fromarray((coarse * 255).astype(np.uint8)).resize(
            (w + 2 * scale, h + 2 * scale), Image.BICUBIC))[scale:scale + h, scale:scale + w] / 255.0
    low, mid, high = np.array([170, 30, 2]), np.array([235, 62, 4]), np.array([255, 160, 24])
    t = np.clip((field - 0.25) / 0.5, 0, 1)[..., None]
    rgb = np.where(t < 0.6, low + (mid - low) * (t / 0.6), mid + (high - mid) * ((t - 0.6) / 0.4))
    return Image.fromarray(np.concatenate([rgb, np.full((h, w, 1), 255)], -1).astype(np.uint8), 'RGBA')


def vanguard(front):
    name = 'CHR_Vanguard_Front_Idle.tga' if front else 'CHR_Vanguard_Idle.tga'
    image = Image.open(VANGUARD / name).convert('RGBA')
    pivot = (0.45343137 if front else 0.43382353) * image.width, image.height * (1 - (0.013888889 if front else 0))
    return image, pivot


def labelled(image, labels, cell_w, title=None, height=14):
    """The image under a strip naming its columns, with a title line above it when given."""
    font = ImageFont.load_default(size=height - 3)
    rows = 2 if title else 1
    strip = Image.new('RGBA', (image.width, height * rows), (20, 18, 24, 255))
    draw = ImageDraw.Draw(strip)
    if title:
        draw.text((6, 1), title, fill=(255, 190, 120, 255), font=font)
    for i, label in enumerate(labels):
        width = draw.textlength(label, font=font)
        draw.text((i * cell_w + (cell_w - width) / 2, height * (rows - 1) + 1), label, fill=(220, 210, 200, 255),
                  font=font)
    out = Image.new('RGBA', (image.width, image.height + strip.height))
    out.paste(strip, (0, 0))
    out.paste(image, (0, strip.height))
    return out


# ---- One GIF per motion: the eight directions side by side, 2x, on the stone with the contact shadow ----
CELL_W, CELL_H, PIVOT_Y = 220, 220, 192       # the whole canvas width: a thrust staff never leaves its cell
ZOOM = 2
floor = stone((CELL_W * len(DIRECTIONS), CELL_H))


def gif_frame(motion, k):
    canvas = floor.copy()
    for i, (_, facing, _) in enumerate(DIRECTIONS):
        at = (i * CELL_W + CELL_W / 2, PIVOT_Y)
        shadow(canvas, at, PPU)
        image, pivot = NOW.frame(motion, facing, k)
        paste(canvas, image, pivot, at)
    out = labelled(canvas, [label for label, _, _ in DIRECTIONS], CELL_W)
    return out.resize((out.width * ZOOM, out.height * ZOOM), Image.NEAREST).convert('RGB')


# Milliseconds a frame: the art set's rates (BuildBlenderArcanist.CLIPS); the walks at the foundry's 3.5 units a
# second over the 1.2-unit stride, about 23 frames a second.
timing = {'idle': 200, 'walk': 43, 'walk-strike': 43, 'strike': 50, 'cast': 71, 'hit': 50, 'death': 111}
for motion in MOTIONS:
    n = count(motion)
    images = [gif_frame(motion, k) for k in range(n)]
    delays = [timing[motion]] * n
    if motion in ('strike', 'cast', 'hit'):
        images.append(gif_frame('idle', 0))
        delays.append(500)
    if motion == 'death':
        delays[-1] = 1500
    loops = 3 if motion in ('walk', 'walk-strike') else 1
    images, delays = images * loops, delays * loops
    images[0].save(OUT / ('ilya-%s.gif' % motion), save_all=True, append_images=images[1:], duration=delays, loop=0,
                   disposal=1)

# ---- Contact sheets: every frame of a motion (columns) in every direction (rows), 1x, with the floor pivot marked ----
for motion in MOTIONS:
    n = count(motion)
    sheet = Image.new('RGBA', (CELL_W * n, CELL_H * len(DIRECTIONS)), (74, 72, 80, 255))
    draw = ImageDraw.Draw(sheet)
    for row, (label, facing, mirrored) in enumerate(DIRECTIONS):
        for k in range(n):
            at = (k * CELL_W + CELL_W / 2, row * CELL_H + PIVOT_Y)
            image, pivot = NOW.frame(motion, facing, k)
            paste(sheet, image, pivot, at)
            draw.line((at[0] - 4, at[1], at[0] + 4, at[1]), fill=(90, 200, 255, 255))
        draw.text((4, row * CELL_H + 4), label + (' (mirrored)' if mirrored else ''), fill=(230, 220, 210, 255))
    sheet.convert('RGB').save(OUT / ('ilya-%s-sheet.png' % motion))

# ---- Turnaround: the idle's first frame in the eight directions, 3x and at 1x ----
turn = Image.new('RGBA', (CELL_W * 8, CELL_H), (74, 72, 80, 255))
for i, (_, facing, _) in enumerate(DIRECTIONS):
    image, pivot = NOW.frame('idle', facing, 0)
    paste(turn, image, pivot, (i * CELL_W + CELL_W / 2, PIVOT_Y))
big = turn.resize((turn.width * 3, turn.height * 3), Image.NEAREST)
sheet = Image.new('RGBA', (big.width, big.height + turn.height), (20, 18, 24, 255))
sheet.paste(big, (0, 0))
sheet.paste(turn, (0, big.height))
sheet.convert('RGB').save(OUT / 'ilya-turnaround.png')


# ---- Beside the painted Vanguard, at the phone's scale and zoomed, on stone and on molten orange ----
def beside(background, title):
    width, height = 560, 260
    canvas = background((width, height)).copy()
    spots = [(70, 'v', True), (180, 'i', 'south-east'), (290, 'i', 'south'), (400, 'v', False), (500, 'i', 'north-west')]
    for x, who, which in spots:
        at = (x, 225)
        shadow(canvas, at, PHONE)
        if who == 'v':
            image, pivot = vanguard(which)
            if which:
                image, pivot = image.transpose(Image.FLIP_LEFT_RIGHT), (image.width - pivot[0], pivot[1])
            paste(canvas, image, pivot, at, PHONE / 128)
        else:
            image, pivot = NOW.frame('idle', which, 0)
            paste(canvas, image, pivot, at, PHONE / PPU)
    canvas = canvas.convert('RGB')
    canvas.save(OUT / ('beside-vanguard-%s-phone.png' % title))
    canvas.resize((width * 3, height * 3), Image.NEAREST).save(OUT / ('beside-vanguard-%s-phone-x3.png' % title))


beside(stone, 'stone')
beside(lava, 'lava')


# ---- An eight-direction walk at the phone's scale (120 pixels a unit), on stone and on lava, and the same at 3x ----
def phone_walk(background, title):
    cell_w, cell_h, pivot_y = 150, 260, 228
    base = background((cell_w * 8, cell_h)).copy()
    for i in range(8):
        shadow(base, (i * cell_w + cell_w / 2, pivot_y), PHONE)
    images = []
    for k in range(count('walk')):
        canvas = base.copy()
        for i, (_, facing, _) in enumerate(DIRECTIONS):
            image, pivot = NOW.frame('walk', facing, k)
            paste(canvas, image, pivot, (i * cell_w + cell_w / 2, pivot_y), PHONE / PPU)
        images.append(labelled(canvas, [label for label, _, _ in DIRECTIONS], cell_w).convert('RGB'))
    images = images * 3
    images[0].save(OUT / ('ilya-walk-8dir-phone-%s.gif' % title), save_all=True, append_images=images[1:],
                   duration=timing['walk'], loop=0, disposal=1)
    big = [im.resize((im.width * 3, im.height * 3), Image.NEAREST) for im in images[:count('walk')]] * 3
    big[0].save(OUT / ('ilya-walk-8dir-phone-%s-x3.gif' % title), save_all=True, append_images=big[1:],
                duration=timing['walk'], loop=0, disposal=1)


phone_walk(stone, 'stone')
phone_walk(lava, 'lava')

# ---- Every west frame: one row a facing and motion, 1x on neutral grey with the pivot marked ----
west = [facing for _, facing in COMPASS if facing in EAST_TWIN]
if NOW.draws_west:
    rows = [(m, f) for f in west for m in MOTIONS]
    width = CELL_W * max(count(m) for m in MOTIONS)
    sheet = Image.new('RGBA', (width, CELL_H * len(rows)), (74, 72, 80, 255))
    draw = ImageDraw.Draw(sheet)
    for row, (motion, facing) in enumerate(rows):
        for k in range(count(motion)):
            at = (k * CELL_W + CELL_W / 2, row * CELL_H + PIVOT_Y)
            image, pivot = NOW.frame(motion, facing, k)
            paste(sheet, image, pivot, at)
            draw.line((at[0] - 4, at[1], at[0] + 4, at[1]), fill=(90, 200, 255, 255))
        draw.text((4, row * CELL_H + 4), '%s %s' % (facing, motion), fill=(230, 220, 210, 255))
    sheet.convert('RGB').save(OUT / 'ilya-west-frames-sheet.png')


# ---- Before and now: the turn from east to west, and the strike and the Vent to the west ----
def compare_turn(motion, k, title):
    """Two rows - the earlier pack as the game drew it, then now - turning from east over the north to the west and
    on round the south, 3x, with the phone-scale strip on the stone under each."""
    order = ['east', 'north-east', 'north', 'north-west', 'west', 'south-west', 'south', 'south-east']
    labels = [dict((f, l) for l, f in COMPASS)[f] for f in order]
    blocks = []
    for name, pack in (('before', OLD), ('now', NOW)):
        row = Image.new('RGBA', (CELL_W * len(order), CELL_H), (74, 72, 80, 255))
        draw = ImageDraw.Draw(row)
        for i, facing in enumerate(order):
            image, pivot = pack.frame(motion, facing, k)
            paste(row, image, pivot, (i * CELL_W + CELL_W / 2, PIVOT_Y))
            drawn, mirrored = pack.drawn(facing)
            if mirrored:
                draw.text((i * CELL_W + 6, CELL_H - 16), 'mirrored ' + drawn, fill=(255, 150, 120, 255),
                          font=ImageFont.load_default(size=12))
        big = row.resize((row.width * 3, row.height * 3), Image.NEAREST)
        phone = stone((CELL_W * len(order), 250))
        for i, facing in enumerate(order):
            at = (i * CELL_W + CELL_W / 2, 225)
            shadow(phone, at, PHONE)
            image, pivot = pack.frame(motion, facing, k)
            paste(phone, image, pivot, at, PHONE / PPU)
        phone = phone.resize((phone.width * 3, phone.height * 3), Image.NEAREST)
        block = Image.new('RGBA', (big.width, big.height + phone.height))
        block.paste(big, (0, 0))
        block.paste(phone, (0, big.height))
        blocks.append(labelled(block, labels, CELL_W * 3, '%s: %s. 3x, then the phone\'s 120 pixels a unit (3x)' % (
            name, 'the west mirrored from the east - the light and the staff change sides' if not pack.draws_west else
            'all eight facings rendered - the light stays upper left, the staff in the right hand'), 34))
    out = Image.new('RGB', (blocks[0].width, sum(b.height for b in blocks)))
    y = 0
    for b in blocks:
        out.paste(b.convert('RGB'), (0, y))
        y += b.height
    out.save(OUT / ('ilya-turn-%s-before-now.png' % title))


def compare_action(motion, title):
    """The motion to the west (and north-west, south-west), before and now, 3x on the stone: a sheet and a GIF."""
    facings = ['north-west', 'west', 'south-west']
    n = count(motion)
    images, sheet_rows = [], []
    for k in list(range(n)) + [None]:
        canvas = stone((CELL_W * 6, CELL_H)).copy()
        for i, (pack, facing) in enumerate([(OLD, f) for f in facings] + [(NOW, f) for f in facings]):
            at = (i * CELL_W + CELL_W / 2, PIVOT_Y)
            shadow(canvas, at, PPU)
            image, pivot = pack.frame(motion if k is not None else 'idle', facing, k if k is not None else 0)
            paste(canvas, image, pivot, at)
        labels = ['before ' + dict((f, l) for l, f in COMPASS)[f] for f in facings] + \
                 ['now ' + dict((f, l) for l, f in COMPASS)[f] for f in facings]
        out = labelled(canvas, labels, CELL_W)
        out = out.resize((out.width * 3, out.height * 3), Image.NEAREST).convert('RGB')
        images.append(out)
        if k is not None:
            sheet_rows.append(out)
    delays = [timing[motion]] * n + [500]
    images[0].save(OUT / ('ilya-west-%s-before-now.gif' % title), save_all=True, append_images=images[1:],
                   duration=[d * 2 for d in delays], loop=0, disposal=1)
    sheet = Image.new('RGB', (sheet_rows[0].width, sum(r.height for r in sheet_rows)))
    for i, r in enumerate(sheet_rows):
        sheet.paste(r, (0, i * r.height))
    sheet.save(OUT / ('ilya-west-%s-before-now.png' % title))


if OLD is not None:
    compare_turn('idle', 0, 'idle')
    compare_turn('walk', 2, 'walk')
    compare_action('strike', 'strike')
    compare_action('cast', 'vent-cast')
print('ARCANIST_REVIEW', OUT)
