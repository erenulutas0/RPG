"""Review material for the Forge Warden's packed sprites (PackWardenSprites.py): exactly the pixels Unity imports, at
the phone's scale (the Samsung S23 shows 120 pixels a world unit: 1080 across a 9-unit view) and zoomed.

    python Tools/ArtReview/ReviewWardenSprites.py --out DIR [--pixel DIR] [--before DIR]

--before takes a folder holding the Warden's sprites before the walking windup and the fall (ForgeWarden/ as git
archive of an earlier commit unpacks it under Assets/_Project/Art/Characters), for the before/after GIFs.

Writes to DIR:
    warden-beside-roster-{stone,lava}.png (+ -x3): the Warden beside the painted Vanguard, Grunt and Cinder Mite, with
        the game's contact shadows and health bars, on the Foundry's dark stone (cropped from an in-game capture) and
        on molten orange (a patch in the pit's sampled colours)
    warden-walk.gif: the heavy walk in the four drawn directions at the Warden's fighting speed
    warden-windup-walk-{stone,lava}.gif: the raised hammer carried at a walk in the four directions, crossing the floor
        at the Warden's speed, its legs stepped by the distance travelled as EnemyLookView steps them
    warden-windup-walk-frames-{stone,lava}.png: its quarters held still, at the phone's scale and tripled
    warden-telegraph-walking-{stone,lava}.gif: the mark goes down and the Warden keeps coming through the telegraph,
        the blow lands at the mark - before (the standing windup gliding) and after (the walking windup), both phases
    warden-death-{stone,lava}.gif: the killing blow at real timing, front and rear - before (the two remains every
        painted enemy leaves, 0.4 s) and after (the boss's own fall, 1.32 s), the live shadow hiding as the game hides it
    warden-hit.gif: the white and the critical flash with the body's squash
    warden-phase2.png: each pose in the first phase and the second side by side
    warden-in-game.png: a mock of the fight at phone scale - bar, shadow, a Burn flame sized as BurnFlames sizes it,
        the slam mark round the hero
    warden-before-after.png: the pixel Warden (EnemyArt, from --pixel, the raw dumps of EnemyArt.Draw), what the prefab
        showed before this set (the painted Grunt's frames, inherited from Grunt.prefab), and the new Warden
    warden-sheet.png: every packed sprite with its pivot marked
    warden-new-sprites.png: the drawings the walking windup and the fall added or redrew, pivots marked
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
BEFORE = Path(ARGS[ARGS.index('--before') + 1]) if '--before' in ARGS else None
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
SPEED = 1.0                 # Enemy_ForgeWarden's move speed, floor units a second
REACH = 1.7                 # Weapon_WardenHammer's range: nearer than this the Warden stands and swings
FADE, SINK = 0.16, 0.05     # CombatEffectsView: the remains fade over their last 0.16 s, settling 0.05 into the floor
REMAINS = (0.16, 0.4)       # every painted enemy's two remains: the first for 0.16 s, the second to 0.4 s
GIF_STEP = 0.04             # 25 frames a second, which every GIF viewer keeps
# EnemyArtSet._windupWalk's quarters, as WalkingQuarter steps them.
WALK_QUARTERS = ['windup-step-a', 'windup-pass-a', 'windup', 'windup-pass-b']
EMBER, HOT = (255, 82, 31), (255, 209, 115)      # WardenSlamView's mark colours


def pivot_of(meta_path, size):
    text = Path(meta_path).read_text(encoding='utf-8')
    m = re.search(r'spritePivot: \{x: ([-0-9.e]+), y: ([-0-9.e]+)\}', text)
    px, py = float(m.group(1)), float(m.group(2))
    return px * size[0], (1 - py) * size[1]          # from the top-left


def load(path):
    image = Image.open(path).convert('RGBA')
    return image, pivot_of(str(path) + '.meta', image.size)


def warden(facing, pose, kind='cool', folder=None):
    stem = 'CHR_Warden_%s_%s' % (facing, pose)
    name = stem + {'cool': '', 'hot': '_hot', 'flash': '_Flash'}[kind]
    return load((folder or ART / 'ForgeWarden') / (name + '.png'))


def by_file(name, folder=None):
    return load((folder or ART / 'ForgeWarden') / name)


def before_folder():
    """The Warden's sprites before the walking windup and the fall, or None."""
    if BEFORE is None:
        return None
    nested = BEFORE / 'Assets/_Project/Art/Characters/ForgeWarden'
    return nested if nested.exists() else BEFORE


def fall_of(rear):
    """The boss's fall in a facing: [(sprite, seconds held)], as ForgeWardenArtSet lists it (frames.json)."""
    fall = report['fall']
    sprites = {(s['facing'], s['pose']): s for s in report['sprites'] if s['kind'] in ('fall', 'remains')}
    facing = 'rear' if rear else 'front'
    out = []
    for pose, seconds in zip(fall['poses'], fall['seconds']):
        s = sprites.get((facing, pose)) or sprites.get(('both', pose))
        out.append((by_file(s['file']), seconds))
    return out


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


def gif(name, frames, delays, zoom=ZOOM):
    frames = [f.convert('RGB').resize((f.width * zoom, f.height * zoom), Image.NEAREST) for f in frames]
    # One palette for every frame (from a spread of them), undithered, so the floor's photograph is the same in each and
    # only what moves is written again.
    picks = frames[::max(1, len(frames) // 6)][:6]
    mosaic = Image.new('RGB', (frames[0].width * len(picks), frames[0].height))
    for i, f in enumerate(picks):
        mosaic.paste(f, (i * f.width, 0))
    palette = mosaic.quantize(colors=255, method=Image.Quantize.MEDIANCUT)
    frames = [f.quantize(palette=palette, dither=Image.Dither.NONE) for f in frames]
    frames[0].save(OUT / name, save_all=True, append_images=frames[1:], duration=delays, loop=0, disposal=1,
                   optimize=False)


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


def label(canvas, text_, at):
    draw = ImageDraw.Draw(canvas)
    x, y = at
    draw.rectangle((x - 3, y - 2, x + 7 * len(text_) + 3, y + 12), fill=(14, 12, 18, 200))
    draw.text((x, y), text_, fill=(236, 226, 214, 255))


def raised(quarter, facing, kind, folder=None):
    """The raised hammer: standing (quarter None) or carried at a walk."""
    return warden(facing, 'windup' if quarter is None else WALK_QUARTERS[quarter], kind, folder)


def quarter_at(distance):
    return min(3, int((distance % CYCLE) / CYCLE * 4))


def walk_frame(distance):
    """EnemyArtSet.WalkingFrame's pose: contact A, passing A, contact B, passing B."""
    return ['step-a', 'pass-a', 'step-b', 'pass-b'][quarter_at(distance)]


def windup_walk_gif(background, title):
    """The raised hammer carried at a walk in the four drawn directions, each crossing its lane of floor at the
    Warden's speed and at the phone's own scale, one stride and round again: the drawing changes every quarter stride
    by the distance travelled, while the root glides between, as the game draws it; the shadow and bar ride the root."""
    k = PHONE / 128
    lane_w, lane_h = 330, 500
    lanes = [('front', False, (-1, -1)), ('front', True, (1, -1)), ('rear', False, (-1, 1)), ('rear', True, (1, 1))]
    base = background((lane_w * 4, lane_h)).copy()
    images, delays = [], []
    span = CYCLE
    steps = int(round(span / SPEED / GIF_STEP))
    for i in range(steps):
        distance = i * GIF_STEP * SPEED
        canvas = base.copy()
        for j, (facing, flip, (dx, dy)) in enumerate(lanes):
            # Along the floor's diagonal (the 2:1 floor halves its depth on screen), centred on the lane.
            n = math.sqrt(dx * dx + dy * dy)
            fx, fy = dx / n * (distance - span / 2), dy / n * (distance - span / 2)
            at = (j * lane_w + lane_w / 2 + fx * PHONE, lane_h - 70 - fy * 0.5 * PHONE)
            shadow(canvas, at, PHONE, SHADOW)
            sprite = raised(quarter_at(distance), facing, 'cool')
            paste(canvas, mirrored(sprite) if flip else sprite, at, k)
            bar(canvas, at, VISUAL_TOP, PHONE, 0.8)
            label(canvas, '%s %s' % (facing, 'right' if flip else 'left'), (j * lane_w + 10, 8))
        images.append(canvas)
        delays.append(int(GIF_STEP * 1000))
    gif('warden-windup-walk-%s.gif' % title, images, delays, zoom=1)


def windup_walk_frames(background, title):
    """The quarters held still, front then rear, at the phone's scale and tripled."""
    k = PHONE / 128
    cw, ch = 190, 400
    canvas = background((cw * 8, ch)).copy()
    for j, facing in enumerate(('front', 'rear')):
        for q in range(4):
            at = ((j * 4 + q) * cw + cw / 2, ch - 30)
            shadow(canvas, at, PHONE, SHADOW)
            paste(canvas, raised(q, facing, 'cool'), at, k)
            bar(canvas, at, VISUAL_TOP, PHONE, 0.8)
            label(canvas, '%s %s' % (facing, WALK_QUARTERS[q]), ((j * 4 + q) * cw + 6, 6))
    canvas = canvas.convert('RGB')
    canvas.save(OUT / ('warden-windup-walk-frames-%s.png' % title))
    canvas.crop((0, 0, cw * 4, ch)).resize((cw * 4 * 3, ch * 3), Image.NEAREST).save(
        OUT / ('warden-windup-walk-frames-%s-x3.png' % title))


def telegraph_walking_gif(background, title):
    """The Warden 3.6 units from the hero walks in at its speed; the mark goes down round the hero and it keeps coming
    through the 1.1 s telegraph, the hammer raised; the blow lands at the mark wherever the Warden has got to, on the
    strike pose held 0.35 s while it walks on. Left the Warden before this work (the standing windup, gliding), right
    after (the walking windup); the first phase, then the second."""
    k = PHONE / 128
    old = before_folder()
    variants = [('after', None)] if old is None else [('before', old), ('after', None)]
    pw, ph = 800, 560
    base = background((pw * len(variants), ph)).copy()
    hero_at = (pw - 270, 470)
    images, delays = [], []
    for kind in ('cool', 'hot'):
        lead, total = 0.6, 0.6 + TELEGRAPH + SLAM_HOLD + 0.5
        steps = int(round(total / GIF_STEP))
        for i in range(steps):
            t = i * GIF_STEP
            canvas = base.copy()
            for j, (name, folder) in enumerate(variants):
                ox = j * pw
                # The floor distance from the hero shrinks at the Warden's speed until it is within the hammer's reach.
                gap = max(REACH, 3.6 - SPEED * t)
                walked = 3.6 - gap
                at = (ox + hero_at[0] - gap * PHONE * 0.94, hero_at[1] - gap * 0.34 * 0.5 * PHONE)
                hero = (ox + hero_at[0], hero_at[1])
                marked = lead <= t < lead + TELEGRAPH
                after_blow = t - lead - TELEGRAPH
                if marked or 0 <= after_blow < SLAM_HOLD:
                    fill = min(1.0, (t - lead) / TELEGRAPH) if marked else 0.0
                    flash = 0.0 if marked else 1 - after_blow / SLAM_HOLD
                    pulse = .6 + .4 * math.sin(t * (9 + 15 * fill))
                    layer = mark_layer((pw, ph), (hero_at[0], hero_at[1]), PHONE, fill, flash, pulse)
                    canvas.alpha_composite(layer, (ox, 0))
                shadow(canvas, hero, PHONE, 0.78)
                paste(canvas, mirrored(vanguard(True)), hero, k)
                moving = gap > REACH
                if marked:
                    quarter = quarter_at(walked) if (moving and folder is None) else None
                    sprite = raised(quarter, 'front', kind, folder)
                elif 0 <= after_blow < SLAM_HOLD:
                    sprite = warden('front', 'strike', kind)
                else:
                    sprite = warden('front', walk_frame(walked) if moving else 'idle', kind)
                shadow(canvas, at, PHONE, SHADOW)
                paste(canvas, mirrored(sprite), at, k)
                bar(canvas, at, VISUAL_TOP, PHONE, 0.8 if kind == 'cool' else 0.4)
                label(canvas, '%s - %s phase' % (name, 'first' if kind == 'cool' else 'second'), (ox + 10, 8))
            images.append(canvas)
            delays.append(int(GIF_STEP * 1000))
    gif('warden-telegraph-walking-%s.gif' % title, images, delays, zoom=1)


def death_gif(background, title):
    """The killing blow at real timing, the boss in its second phase, front (looking left) and rear (looking right): the
    body and its live shadow and bar hide at once and the pooled remains take over where it stood, fading over the
    last 0.16 s and settling 0.05 into the floor. Left before this work (the two remains, the front's drawings for
    both facings), right after (the boss's own fall in the facing it fell in)."""
    k = PHONE / 128
    old = before_folder()
    columns = [('before', old), ('after', None)] if old is not None else [('after', None)]
    cw, ch = 460, 300
    base = background((cw * len(columns), ch * 2)).copy()
    images, delays = [], []
    alive, longest = 0.5, max(sum(s for _, s in fall_of(False)), REMAINS[1])
    steps = int(round((alive + longest + 0.6) / GIF_STEP))
    for i in range(steps):
        t = i * GIF_STEP
        canvas = base.copy()
        for row, (facing, flip) in enumerate((('front', False), ('rear', True))):
            for col, (name, folder) in enumerate(columns):
                # Room on the side the body falls toward: the drawings' left, mirrored with them.
                at = (col * cw + cw / 2 + (-60 if flip else 60), row * ch + ch - 70)
                if t < alive:
                    shadow(canvas, at, PHONE, SHADOW)
                    body = warden(facing, 'idle', 'hot')
                    paste(canvas, mirrored(body) if flip else body, at, k)
                    bar(canvas, at, VISUAL_TOP, PHONE, 0.06)
                else:
                    dt = t - alive
                    if folder is not None:
                        lifetime = REMAINS[1]
                        drawing = by_file('CHR_Warden_death_%d.png' % (0 if dt < REMAINS[0] else 1), folder)
                    else:
                        fall = fall_of(facing == 'rear')
                        lifetime = sum(s for _, s in fall)
                        drawing, held = fall[-1][0], 0.0
                        for sprite, seconds in fall:
                            if dt < held + seconds:
                                drawing = sprite
                                break
                            held += seconds
                    if dt < lifetime:
                        left = min(1.0, max(0.0, (lifetime - dt) / FADE))
                        sunk = (at[0], at[1] + SINK * PHONE * (1 - left))
                        paste(canvas, mirrored(drawing) if flip else drawing, sunk, k, alpha=left)
                if row == 0:
                    label(canvas, name, (col * cw + 10, 8))
            label(canvas, '%s %s' % (facing, 'right' if flip else 'left'), (10, row * ch + 26))
        images.append(canvas)
        delays.append(int(GIF_STEP * 1000))
    gif('warden-death-%s.gif' % title, images, delays, zoom=1)


for background, title in ((stone, 'stone'), (lava, 'lava')):
    windup_walk_gif(background, title)
    windup_walk_frames(background, title)
    telegraph_walking_gif(background, title)
    death_gif(background, title)


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
    cell_w, cell_h = 360, 420
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


def new_sprites_sheet():
    """The drawings the walking windup and the fall added or redrew, at 1x on mid grey, each pivot marked."""
    names = []
    for facing in ('front', 'rear'):
        for pose in WALK_QUARTERS:
            names += ['CHR_Warden_%s_%s%s.png' % (facing, pose, suffix) for suffix in ('', '_hot', '_Flash')]
    names += [s['file'] for s in report['sprites'] if s['kind'] in ('fall', 'remains')]
    cell_w, cell_h, cols = 360, 420, 6
    rows = (len(names) + cols - 1) // cols
    sheet = Image.new('RGBA', (cols * cell_w, rows * cell_h), (74, 72, 80, 255))
    draw = ImageDraw.Draw(sheet)
    for i, name in enumerate(names):
        sprite = by_file(name)
        image, (px, py) = sprite
        at = ((i % cols) * cell_w + cell_w / 2, (i // cols) * cell_h + cell_h - 60)
        paste(sheet, sprite, at, 1.0)
        draw.line((at[0] - 6, at[1], at[0] + 6, at[1]), fill=(90, 200, 255, 255))
        draw.line((at[0], at[1] - 6, at[0], at[1] + 6), fill=(90, 200, 255, 255))
        draw.text(((i % cols) * cell_w + 4, (i // cols) * cell_h + 4), '%s %dx%d' % (name.replace('CHR_Warden_', '')[:-4],
                  image.width, image.height), fill=(230, 220, 210, 255))
    sheet.convert('RGB').save(OUT / 'warden-new-sprites.png')


new_sprites_sheet()
print('WARDEN_REVIEW', OUT)
