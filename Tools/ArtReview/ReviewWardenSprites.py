"""Review material for the Forge Warden's packed sprites (PackWardenSprites.py): exactly the pixels Unity imports, at
the phone's scale (the Samsung S23 shows 120 pixels a world unit: 1080 across a 9-unit view) and zoomed.

    python Tools/ArtReview/ReviewWardenSprites.py --out DIR [--pixel DIR]

Writes to DIR:
    warden-beside-roster-{stone,lava}.png (+ -x3): the Warden beside the painted Vanguard, Grunt and Cinder Mite, with
        the game's contact shadows and health bars, on the Foundry's dark stone (cropped from an in-game capture) and
        on molten orange (a patch in the pit's sampled colours)
    warden-walk.gif: the heavy walk in the four drawn directions at the Warden's fighting speed
    warden-telegraph.gif: the mark goes down, the hammer goes up for the telegraph, the blow lands - first phase, then
        the second
    warden-hit.gif, warden-death.gif: the white and the critical flash with the body's squash; the fall to the remains
    warden-phase2.png: each pose in the first phase and the second side by side
    warden-in-game.png: a mock of the fight at phone scale - bar, shadow, a Burn flame sized as BurnFlames sizes it,
        the slam mark round the hero
    warden-before-after.png: the pixel Warden (EnemyArt, from --pixel, the raw dumps of EnemyArt.Draw), what the prefab
        showed before this set (the painted Grunt's frames, inherited from Grunt.prefab), and the new Warden
    warden-sheet.png: every packed sprite with its pivot marked
"""
import json
import math
import re
import sys
import struct
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
ARGS = sys.argv[1:]
OUT = Path(ARGS[ARGS.index('--out') + 1]) if '--out' in ARGS else ROOT / 'TestResults/warden-review'
PIXEL = Path(ARGS[ARGS.index('--pixel') + 1]) if '--pixel' in ARGS else None
OUT.mkdir(parents=True, exist_ok=True)
ART = ROOT / 'Assets/_Project/Art/Characters'
CAPTURE = ROOT / 'ArtDirection/2026-10-24/foundry-pit-eruption-01/pit-spill.png'
SKILLS = ROOT / 'Assets/_Project/Art/Effects/Skills'
report = json.loads((ROOT / 'ArtDirection/2026-10-27/warden-blender-01/frames.json').read_text())
PHONE = 120                 # S23 pixels per world unit
SLAM_RADIUS = 2.2           # WardenRule.SlamRadius, floor units
TELEGRAPH = 1.1             # WardenRule.Telegraph
SLAM_HOLD = 0.35            # EnemyLookView.SlamFrameSeconds
BAR_CLEARANCE = 0.12        # EnemyLookView._barClearance
EMBER, HOT = (255, 82, 31), (255, 209, 115)      # WardenSlamView's mark colours


def pivot_of(meta_path, size):
    text = Path(meta_path).read_text(encoding='utf-8')
    m = re.search(r'spritePivot: \{x: ([-0-9.e]+), y: ([-0-9.e]+)\}', text)
    px, py = float(m.group(1)), float(m.group(2))
    return px * size[0], (1 - py) * size[1]          # from the top-left


def load(path):
    image = Image.open(path).convert('RGBA')
    return image, pivot_of(str(path) + '.meta', image.size)


def warden(facing, pose, kind='cool'):
    stem = 'CHR_Warden_%s_%s' % (facing, pose)
    name = stem + {'cool': '', 'hot': '_hot', 'flash': '_Flash'}[kind]
    return load(ART / 'ForgeWarden' / (name + '.png'))


def warden_death(k):
    return load(ART / 'ForgeWarden' / ('CHR_Warden_death_%d.png' % k))


def grunt(name):
    return load(ART / 'SlagBrute' / ('CHR_Grunt_%s.tga' % name))


def mite(name):
    return load(ART / 'CinderMite' / ('CHR_Mite_%s.tga' % name))


def vanguard(front=True):
    return load(ART / 'Vanguard' / ('CHR_Vanguard_Front_Idle.tga' if front else 'CHR_Vanguard_Idle.tga'))


def mirrored(sprite):
    image, (px, py) = sprite
    return image.transpose(Image.FLIP_LEFT_RIGHT), (image.width - px, py)


def paste(canvas, sprite, at, scale, alpha=1.0, squash=(1.0, 1.0)):
    image, (px, py) = sprite
    sx, sy = scale * squash[0], scale * squash[1]
    size = (max(1, round(image.width * sx)), max(1, round(image.height * sy)))
    image = image.resize(size, Image.BILINEAR)
    if alpha < 1:
        a = np.asarray(image).copy()
        a[..., 3] = (a[..., 3] * alpha).astype(np.uint8)
        image = Image.fromarray(a, 'RGBA')
    canvas.alpha_composite(image, (round(at[0] - px * sx), round(at[1] - py * sy)))


def shadow(canvas, at, ppu, width):
    """ContactShadowView: ContactShadowArt's stepped ellipse (alpha 132, 96, 58, 24), width units across."""
    w, h = width * ppu, width * ppu / 2
    layer = np.zeros((canvas.height, canvas.width, 4), np.uint8)
    ys, xs = np.mgrid[0:canvas.height, 0:canvas.width]
    nx = (xs + 0.5 - at[0]) / (w / 2 * 15 / 16)
    ny = (ys + 0.5 - at[1]) / (h / 2 * 7 / 8)
    r = nx * nx + ny * ny
    alpha = np.select([r < .18, r < .42, r < .72, r < 1], [132, 96, 58, 24], 0).astype(np.uint8)
    layer[..., 0], layer[..., 1], layer[..., 2], layer[..., 3] = 9, 10, 20, alpha
    image = Image.fromarray(layer, 'RGBA').filter(ImageFilter.GaussianBlur(ppu * 0.012))
    canvas.alpha_composite(image)


def bar(canvas, at, top, ppu, fraction=1.0, scale=(1.0, 1.0)):
    """EnemyLookView's bar: EnemyArt's 26x5 backing and 24x3 fill at 32 texels a unit, at top + clearance."""
    texel = ppu / 32
    cx, cy = at[0], at[1] - (top + BAR_CLEARANCE) * ppu
    draw = ImageDraw.Draw(canvas)
    w, h = 26 * texel * scale[0], 5 * texel * scale[1]
    draw.rectangle((cx - w / 2, cy - h / 2, cx + w / 2 - 1, cy + h / 2 - 1), fill=(8, 8, 29, 255))
    fw, fh = 24 * texel * scale[0] * fraction, 3 * texel * scale[1]
    left = cx - 12 * texel * scale[0]
    rows = [(244, 116, 106), (229, 63, 50), (158, 42, 34)]
    for k, colour in enumerate(rows):
        y0 = cy - fh / 2 + k * fh / 3
        draw.rectangle((left, y0, left + fw - 1, y0 + fh / 3 - 1), fill=colour + (255,))


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


VISUAL_TOP = report['visual_top']
SHADOW = report['shadow_width']
CYCLE = report['walk_cycle_distance']
GRUNT_TOP, GRUNT_SHADOW = 1.05, 0.78          # SlagBruteArtSet
MITE_TOP, MITE_SHADOW = None, None


def mite_numbers():
    """The Mite's set leaves _visualTop and _shadowWidth at their defaults: its idle sprite's top, and 0.48."""
    text = (ART / 'CinderMite' / 'CinderMiteArtSet.asset').read_text(encoding='utf-8')
    found = re.search(r'_visualTop: ([0-9.]+)', text)
    image, (_, py) = mite('front_idle')
    top = float(found.group(1)) if found and float(found.group(1)) > 0 else py / 128
    found = re.search(r'_shadowWidth: ([0-9.]+)', text)
    return top, float(found.group(1)) if found else 0.48


MITE_TOP, MITE_SHADOW = mite_numbers()


# ---- Beside the painted roster, at the phone's scale and zoomed, on stone and on molten orange ----
def beside(background, title):
    width, height = 1100, 330
    canvas = background((width, height)).copy()
    floor_y = 290
    k = PHONE / 128
    cast = [(70, 'v', vanguard(True), None, None, 0.78), (190, 'g', grunt('front-idle'), GRUNT_TOP, None, GRUNT_SHADOW),
            (290, 'm', mite('front_idle'), MITE_TOP, (.65, .8), MITE_SHADOW),
            (430, 'w', warden('front', 'idle'), VISUAL_TOP, None, SHADOW),
            (600, 'w', mirrored(warden('rear', 'idle')), VISUAL_TOP, None, SHADOW),
            (770, 'w', warden('front', 'windup'), VISUAL_TOP, None, SHADOW),
            (950, 'w', warden('front', 'strike', 'hot'), VISUAL_TOP, None, SHADOW)]
    for x, who, sprite, top, bar_scale, shadow_width in cast:
        at = (x, floor_y)
        shadow(canvas, at, PHONE, shadow_width)
        paste(canvas, sprite, at, k)
        if top is not None:
            bar(canvas, at, top, PHONE, 1.0 if who != 'w' else 0.8, bar_scale or (1.0, 1.0))
    canvas = canvas.convert('RGB')
    canvas.save(OUT / ('warden-beside-roster-%s.png' % title))
    canvas.crop((120, 0, 700, height)).resize((580 * 3, height * 3), Image.NEAREST).save(
        OUT / ('warden-beside-roster-%s-x3.png' % title))


beside(stone, 'stone')
beside(lava, 'lava')


# ---- GIFs: phone scale, doubled ----
ZOOM = 2


def gif(name, frames, delays):
    frames = [f.convert('RGB').resize((f.width * ZOOM, f.height * ZOOM), Image.NEAREST) for f in frames]
    frames[0].save(OUT / name, save_all=True, append_images=frames[1:], duration=delays, loop=0, disposal=1)


def cell(background, w, h):
    return background((w, h)).copy()


WALK_ORDER = [('step-a', 1), ('pass-a', 4), ('step-b', 2), ('pass-b', 5)]


def walk_gif():
    k = PHONE / 128
    w, h = 4 * 190, 290
    base = stone((w, h))
    images, delays = [], []
    seconds = CYCLE / 4 / 1.0                     # a quarter cycle at the boss's speed, 1 floor unit a second
    for _ in range(3):
        for pose, _index in WALK_ORDER:
            canvas = base.copy()
            for i, (facing, flip) in enumerate([('front', False), ('front', True), ('rear', False), ('rear', True)]):
                at = (95 + i * 190, 260)
                shadow(canvas, at, PHONE, SHADOW)
                sprite = warden(facing, pose)
                paste(canvas, mirrored(sprite) if flip else sprite, at, k)
            images.append(canvas)
            delays.append(int(seconds * 1000))
    gif('warden-walk.gif', images, delays)


walk_gif()


def mark_layer(size, centre, ppu, fill, flash, pulse):
    """WardenSlamView's mark: an edge ring SlamRadius wide on the 2:1 floor and a fill ring growing out to it."""
    layer = Image.new('RGBA', size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    rx, ry = SLAM_RADIUS * ppu, SLAM_RADIUS * ppu / 2
    if flash > 0:
        edge = tuple(int(EMBER[i] + (HOT[i] - EMBER[i]) * flash) for i in range(3)) + (int(255 * flash),)
    else:
        edge = EMBER + (int(255 * pulse),)
    draw.ellipse((centre[0] - rx, centre[1] - ry, centre[0] + rx, centre[1] + ry), outline=edge, width=max(2, round(.07 * ppu)))
    s = max(.05, fill)
    fa = int(255 * (flash if flash > 0 else .4 + .6 * fill))
    draw.ellipse((centre[0] - rx * s, centre[1] - ry * s, centre[0] + rx * s, centre[1] + ry * s), outline=HOT + (fa,),
                 width=max(2, round(.05 * ppu)))
    return layer


def telegraph_gif():
    """The Warden left of the hero; the mark goes down where the hero stands, the hammer goes up and holds for the
    telegraph while the fill grows to the edge, the blow lands on the strike pose as the mark flashes."""
    k = PHONE / 128
    w, h = 900, 520
    base = stone((w, h))
    warden_at, hero_at = (250, 330), (560, 400)
    images, delays = [], []
    step = 0.05

    def frame(sprite, fill=0.0, flash=0.0, marked=False, t=0.0):
        canvas = base.copy()
        if marked or flash > 0:
            pulse = .6 + .4 * math.sin(t * (9 + 15 * fill))
            canvas.alpha_composite(mark_layer((w, h), hero_at, PHONE, fill, flash, pulse))
        shadow(canvas, hero_at, PHONE, 0.78)
        paste(canvas, mirrored(vanguard(True)), hero_at, k)
        shadow(canvas, warden_at, PHONE, SHADOW)
        paste(canvas, sprite, warden_at, k)
        bar(canvas, warden_at, VISUAL_TOP, PHONE, 0.7)
        return canvas

    for kind in ('cool', 'hot'):
        # Facing the hero, on its right: the drawings mirrored.
        idle = mirrored(warden('front', 'idle', kind))
        for _ in range(8):
            images.append(frame(idle)); delays.append(int(step * 1000))
        n = int(round(TELEGRAPH / step))
        for i in range(n):
            images.append(frame(mirrored(warden('front', 'windup', kind)), fill=(i + 1) / n, marked=True, t=i * step))
            delays.append(int(step * 1000))
        m = int(round(SLAM_HOLD / step))
        for i in range(m):
            images.append(frame(mirrored(warden('front', 'strike', kind)), flash=1 - i / m))
            delays.append(int(step * 1000))
        for _ in range(12):
            images.append(frame(idle)); delays.append(int(step * 1000))
    gif('warden-telegraph.gif', images, delays)


telegraph_gif()


def hit_gif():
    """CombatantView's flash (the white silhouette, gold on a critical) for 0.14 s and EnemyLookView's 0.09 s squash."""
    k = PHONE / 128
    w, h = 2 * 260, 300
    base = stone((w, h))
    images, delays = [], []
    step = 1 / 60

    def draw_pair(t_hit, colour):
        canvas = base.copy()
        for i, facing in enumerate(('front', 'rear')):
            at = (130 + i * 260, 270)
            shadow(canvas, at, PHONE, SHADOW)
            body = warden(facing, 'idle')
            squash = 1.0
            if t_hit is not None and t_hit < .09:
                squash = 1 - t_hit / .09
            paste(canvas, body, at, k, squash=(1 + .06 * squash, 1 - .08 * squash))
            if t_hit is not None and t_hit < .14:
                flash = warden(facing, 'idle', 'flash')
                image, pivot = flash
                tinted = np.asarray(image).copy()
                tinted[..., :3] = colour
                paste(canvas, (Image.fromarray(tinted, 'RGBA'), pivot), at, k, squash=(1 + .06 * squash, 1 - .08 * squash))
        return canvas

    for colour in ((255, 255, 255), (255, 209, 51)):
        for _ in range(12):
            images.append(draw_pair(None, colour)); delays.append(17)
        for i in range(14):
            images.append(draw_pair(i * step, colour)); delays.append(17)
    gif('warden-hit.gif', images, delays)


hit_gif()


def death_gif():
    """The body hides and the pooled remains take its place: the first drawing for 0.16 s, the second to 0.4 s,
    fading over the last 0.16 s and sinking 0.05 units (CombatEffectsView.OnEnemyDefeated)."""
    k = PHONE / 128
    w, h = 2 * 300, 300
    base = stone((w, h))
    images, delays = [], []
    step = 1 / 30
    for _ in range(15):
        canvas = base.copy()
        for i, flip in enumerate((False, True)):
            at = (150 + i * 300, 260)
            shadow(canvas, at, PHONE, SHADOW)
            sprite = warden('front', 'idle', 'hot')
            paste(canvas, mirrored(sprite) if flip else sprite, at, k)
        images.append(canvas); delays.append(33)
    t = 0.0
    while t <= 0.4 + 1e-6:
        canvas = base.copy()
        left = min(1.0, max(0.0, (0.4 - t) / .16))
        for i, flip in enumerate((False, True)):
            at = (150 + i * 300, 260 + 0.05 * PHONE * (1 - left))
            sprite = warden_death(0 if t < .16 else 1)
            paste(canvas, mirrored(sprite) if flip else sprite, at, k, alpha=left)
        images.append(canvas); delays.append(33)
        t += step
    canvas = base.copy()
    images.append(canvas); delays.append(900)
    # And the two drawings held, to see them.
    for k_ in (0, 1):
        canvas = base.copy()
        for i, flip in enumerate((False, True)):
            at = (150 + i * 300, 260)
            sprite = warden_death(k_)
            paste(canvas, mirrored(sprite) if flip else sprite, at, k)
        images.append(canvas); delays.append(1000)
    gif('warden-death.gif', images, delays)


death_gif()


# ---- The two phases side by side, doubled ----
def phase2():
    k = PHONE / 128
    poses = [('front', 'idle'), ('front', 'windup'), ('front', 'strike'), ('rear', 'idle'), ('rear', 'windup')]
    w, h = len(poses) * 2 * 175, 380
    rows = []
    for background in (stone, lava):
        canvas = background((w, h)).copy()
        for i, (facing, pose) in enumerate(poses):
            for j, kind in enumerate(('cool', 'hot')):
                at = (88 + (2 * i + j) * 175, 350)
                shadow(canvas, at, PHONE, SHADOW)
                paste(canvas, warden(facing, pose, kind), at, k)
        rows.append(canvas)
    sheet = Image.new('RGB', (w, 2 * h))
    sheet.paste(rows[0], (0, 0))
    sheet.paste(rows[1], (0, h))
    sheet.resize((w * 2 // 2 * 1, 2 * h), Image.NEAREST).save(OUT / 'warden-phase2.png')


phase2()


# ---- A mock of the fight at phone scale: bar, shadow, a Burn flame as BurnFlames sizes it, the slam mark ----
def burn_frame():
    text = (SKILLS / 'SkillEffectArtSet.asset').read_text(encoding='utf-8')
    block = text.split('_flames:')[1]
    sheet_h = int(re.search(r'SheetHeight: (\d+)', block).group(1))
    tpu = float(re.search(r'TexelsPerUnit: ([0-9.]+)', block).group(1))
    px, py = (float(v) for v in re.search(r'Pivot: \{x: ([-0-9.e]+), y: ([-0-9.e]+)\}', block).groups())
    x, y, fw, fh = (int(v) for v in re.search(r'- X: (\d+)\s+Y: (\d+)\s+Width: (\d+)\s+Height: (\d+)', block).groups())
    sheet = Image.open(SKILLS / 'VFX_Skill_Burn.png').convert('RGBA')
    top = sheet_h - y - fh
    image = sheet.crop((x, top, x + fw, top + fh))
    return image, (px * fw, (1 - py) * fh), tpu


def in_game():
    w, h = 1080, 760
    canvas = stone((w, h)).copy()
    k = PHONE / 128
    flame, flame_pivot, flame_tpu = burn_frame()
    hero_at = (720, 560)
    canvas.alpha_composite(mark_layer((w, h), hero_at, PHONE, 0.65, 0.0, 0.9))
    grunt_at, warden_at, mite_at = (130, 600), (380, 470), (900, 360)
    for at, sprite, top, sw, bar_scale, burn in [(grunt_at, grunt('front-idle'), GRUNT_TOP, GRUNT_SHADOW, None, True),
                                                 (mite_at, mirrored(mite('front_idle')), MITE_TOP, MITE_SHADOW, (.65, .8), False),
                                                 (warden_at, mirrored(warden('front', 'windup', 'hot')), VISUAL_TOP, SHADOW, None, True)]:
        shadow(canvas, at, PHONE, sw)
        paste(canvas, sprite, at, k)
        if burn:
            # BurnFlames.PlacePainted: at the feet under the body's middle, scaled by VisualTop / 1.05 within .56-1.5.
            scale = min(1.5, max(.56, top / 1.05))
            paste(canvas, (flame, flame_pivot), at, PHONE / flame_tpu * scale, alpha=.95)
        bar(canvas, at, top, PHONE, 0.55 if sprite is not None else 1, bar_scale or (1.0, 1.0))
    shadow(canvas, hero_at, PHONE, 0.78)
    paste(canvas, vanguard(True), hero_at, k)
    canvas.convert('RGB').save(OUT / 'warden-in-game.png')


in_game()


# ---- Before and after ----
def raw_pixel(name):
    data = (PIXEL / (name + '.rgba')).read_bytes()
    w, h = struct.unpack('<ii', data[:8])
    return Image.frombytes('RGBA', (w, h), data[8:])


def before_after():
    w, h = 3 * 300, 360
    rows = []
    for background in (stone, lava):
        canvas = background((w, h)).copy()
        floor_y = 320
        if PIXEL is not None:
            # The pixel look: its canvas's bottom-centre is the pivot (EnemyArt), 32 texels a unit.
            image = raw_pixel('Warden_IdleA')
            at = (150, floor_y)
            shadow(canvas, at, PHONE, 49 / 32 * .82)
            paste(canvas, (image, (image.width / 2, image.height)), at, PHONE / 32)
            bar(canvas, at, 56 / 32, PHONE)
        # What the prefab showed before: Grunt.prefab's painted set, inherited through Tank.prefab.
        at = (450, floor_y)
        shadow(canvas, at, PHONE, GRUNT_SHADOW)
        paste(canvas, grunt('front-idle'), at, PHONE / 128)
        bar(canvas, at, GRUNT_TOP, PHONE)
        at = (750, floor_y)
        shadow(canvas, at, PHONE, SHADOW)
        paste(canvas, warden('front', 'idle'), at, PHONE / 128)
        bar(canvas, at, VISUAL_TOP, PHONE)
        rows.append(canvas)
    sheet = Image.new('RGB', (w, 2 * h + 24), (20, 18, 24))
    draw = ImageDraw.Draw(sheet)
    for i, label in enumerate(['pixel Warden (EnemyArt.cs)', 'inherited painted Grunt set', 'new painted Warden']):
        draw.text((i * 300 + 12, 6), label, fill=(230, 220, 210))
    sheet.paste(rows[0], (0, 24))
    sheet.paste(rows[1], (0, 24 + h))
    sheet.save(OUT / 'warden-before-after.png')
    sheet.resize((sheet.width * 2, sheet.height * 2), Image.NEAREST).save(OUT / 'warden-before-after-x2.png')


before_after()


# ---- Every packed sprite, pivot marked ----
def contact_sheet():
    sprites = report['sprites']
    cell_w, cell_h = 300, 420
    cols = 8
    rows = (len(sprites) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * cell_w, rows * cell_h), (74, 72, 80, 255))
    draw = ImageDraw.Draw(sheet)
    for i, s in enumerate(sprites):
        sprite = load(ART / 'ForgeWarden' / s['file'])
        at = ((i % cols) * cell_w + cell_w / 2, (i // cols) * cell_h + cell_h - 50)
        paste(sheet, sprite, at, 1.0)
        draw.line((at[0] - 5, at[1], at[0] + 5, at[1]), fill=(90, 200, 255, 255))
        draw.text(((i % cols) * cell_w + 4, (i // cols) * cell_h + 4), s['file'].replace('CHR_Warden_', '')[:-4],
                  fill=(230, 220, 210, 255))
    sheet.convert('RGB').save(OUT / 'warden-sheet.png')


contact_sheet()
print('WARDEN_REVIEW', OUT)
