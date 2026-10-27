"""Review material for Ilya Kest's packed sprites (PackArcanistSprites.py): exactly the atlas pixels Unity imports.

    python Tools/ArtReview/ReviewArcanistSprites.py --out DIR

Writes to DIR: one animated GIF per motion with all eight game directions (the west three are the east drawings
mirrored, as RenderedCharacterAnimator draws them), a turnaround, and Ilya beside the painted Vanguard at the phone's
scale (the Samsung S23 shows 120 pixels a world unit: 1080 pixels across a 9-unit view) and zoomed, on the Foundry's
dark stone (cropped from an in-game capture) and on molten orange (a patch in the pit's sampled colours).
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
ARGS = sys.argv[1:]
OUT = Path(ARGS[ARGS.index('--out') + 1]) if '--out' in ARGS else ROOT / 'TestResults/arcanist-review'
OUT.mkdir(parents=True, exist_ok=True)
ART = ROOT / 'Assets/_Project/Art/Characters/CinderArcanist'
VANGUARD = ROOT / 'Assets/_Project/Art/Characters/Vanguard'
CAPTURE = ROOT / 'ArtDirection/2026-10-24/foundry-pit-eruption-01/pit-spill.png'
report = json.loads((ROOT / 'ArtDirection/2026-10-27/arcanist-blender-01/frames.json').read_text())
PPU = report['pixels_per_unit']
PHONE = 120          # S23 pixels per world unit
MOTIONS = ['idle', 'walk', 'walk-strike', 'strike', 'cast', 'hit', 'death']
# The game's eight directions, as drawn: (label, rendered facing, mirrored).
DIRECTIONS = [('S', 'south', False), ('SE', 'south-east', False), ('E', 'east', False), ('NE', 'north-east', False),
              ('N', 'north', False), ('NW', 'north-east', True), ('W', 'east', True), ('SW', 'south-east', True)]

pages = [Image.open(ART / a['file']).convert('RGBA') for a in report['atlases']]
FRAMES = {}
for f in report['frames']:
    page = pages[f['atlas']]
    x, y, w, h = f['rect']
    top = page.height - y - h
    image = page.crop((x, top, x + w, top + h))
    FRAMES[f['name']] = (image, (f['pivot'][0] * w, (1 - f['pivot'][1]) * h))


def frame(motion, facing, k, mirrored=False):
    image, (px, py) = FRAMES['%s_%s_%d' % (motion, facing, k)]
    if mirrored:
        image = image.transpose(Image.FLIP_LEFT_RIGHT)
        px = image.width - px
    return image, (px, py)


def count(motion):
    return sum(1 for name in FRAMES if name.startswith(motion + '_south_'))


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


# ---- One GIF per motion: the eight directions side by side, 2x, on the stone with the contact shadow ----
CELL_W, CELL_H, PIVOT_Y = 180, 200, 172
ZOOM = 2
floor = stone((CELL_W * len(DIRECTIONS), CELL_H))
label_strip = Image.new('RGBA', (CELL_W * len(DIRECTIONS), 14), (20, 18, 24, 255))
draw = ImageDraw.Draw(label_strip)
for i, (label, _, _) in enumerate(DIRECTIONS):
    draw.text((i * CELL_W + CELL_W // 2 - 6, 1), label, fill=(220, 210, 200, 255))


def gif_frame(motion, k):
    canvas = floor.copy()
    for i, (_, facing, mirrored) in enumerate(DIRECTIONS):
        at = (i * CELL_W + CELL_W / 2, PIVOT_Y)
        shadow(canvas, at, PPU)
        image, pivot = frame(motion, facing, k, mirrored)
        paste(canvas, image, pivot, at)
    out = Image.new('RGBA', (canvas.width, canvas.height + label_strip.height))
    out.paste(label_strip, (0, 0))
    out.paste(canvas, (0, label_strip.height))
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
            image, pivot = frame(motion, facing, k, mirrored)
            paste(sheet, image, pivot, at)
            draw.line((at[0] - 4, at[1], at[0] + 4, at[1]), fill=(90, 200, 255, 255))
        draw.text((4, row * CELL_H + 4), label, fill=(230, 220, 210, 255))
    sheet.convert('RGB').save(OUT / ('ilya-%s-sheet.png' % motion))

# ---- Turnaround: the idle's first frame in the eight directions, 3x and at 1x ----
turn = Image.new('RGBA', (CELL_W * 8, CELL_H), (74, 72, 80, 255))
for i, (_, facing, mirrored) in enumerate(DIRECTIONS):
    image, pivot = frame('idle', facing, 0, mirrored)
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
    spots = [(70, 'v', True), (180, 'i', 'south-east'), (290, 'i', 'south'), (400, 'v', False), (500, 'i', 'north-east')]
    for x, who, which in spots:
        at = (x, 225)
        shadow(canvas, at, PHONE)
        if who == 'v':
            image, pivot = vanguard(which)
            if which:
                image, pivot = image.transpose(Image.FLIP_LEFT_RIGHT), (image.width - pivot[0], pivot[1])
            paste(canvas, image, pivot, at, PHONE / 128)
        else:
            image, pivot = frame('idle', which, 0)
            paste(canvas, image, pivot, at, PHONE / PPU)
    canvas = canvas.convert('RGB')
    canvas.save(OUT / ('beside-vanguard-%s-phone.png' % title))
    canvas.resize((width * 3, height * 3), Image.NEAREST).save(OUT / ('beside-vanguard-%s-phone-x3.png' % title))


beside(stone, 'stone')
beside(lava, 'lava')
print('ARCANIST_REVIEW', OUT)
