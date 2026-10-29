"""Review material for the horde's anticipation and deaths (build_horde_motion.py), from the imported TGAs and sets.

    python Tools/ArtReview/review_horde_motion.py --out DIR [--stone PHONE_CAPTURE.png]

At the phone's scale (the Samsung S23 shows 120 pixels a world unit) on the Foundry's stone (cropped from an in-game
capture) and on molten orange (the pit's colours), as review_foundry_enemies.py composites them. Timing is the game's:
the walk steps by distance at the enemy's speed, the windup holds its set's beat (at most half the blow's interval)
before the strike, the strike frame holds 0.14 s (EnemyLookView), a death steps its drawings every _deathStep seconds
and fades and sinks over its last 0.16 s (CombatEffectsView).

    <enemy>-strike-<floor>.gif   walk, stop in reach, windup, strike, recover - front and rear, left and right
    <enemy>-death-<floor>.gif    standing, then its own death - front and rear - beside the two remains it replaces
    crowd-<floor>.png/.gif       a crowd round the hero: walking, winding up, striking and dying at once
    sprites.png                  every new sprite on a checker, its pivot (the floor root) marked
"""
import json
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
ARGS = sys.argv[1:]
OUT = Path(ARGS[ARGS.index('--out') + 1]) if '--out' in ARGS else ROOT / 'TestResults/horde-motion-review'
OUT.mkdir(parents=True, exist_ok=True)
ART = ROOT / 'Assets/_Project/Art/Characters'
CAPTURE = ROOT / 'ArtDirection/2026-10-24/foundry-pit-eruption-01/pit-spill.png'
PHONE = 120
STRIKE_HOLD = 0.14
FADE = 0.16
SINK = 0.05
ENEMIES = {
    # folder, stem, separator, set, speed (EnemyDefinition), the weapon's interval (Weapon_*), the walk's frame order
    'grunt': ('SlagBrute', 'Grunt', '-', 'SlagBruteArtSet', 1.6, 1.0),
    'mite': ('CinderMite', 'Mite', '_', 'CinderMiteArtSet', 2.2, 1.5),
    'runner': ('Runner', 'Runner', '-', 'RunnerArtSet', 3.0, 0.4),
    'tank': ('Tank', 'Tank', '-', 'TankArtSet', 0.9, 2.5),
    'captain': ('GruntCaptain', 'Captain', '-', 'GruntCaptainArtSet', 1.4, 1.2),
}
GUIDS = {}
for meta in ART.rglob('*.tga.meta'):
    GUIDS[re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8'), re.M).group(1)] = Path(str(meta)[:-5])


class Sprite:
    def __init__(self, path):
        self.path = path
        self.image = Image.open(path).convert('RGBA')
        m = re.search(r'spritePivot: \{x: (-?[\d.]+), y: (-?[\d.]+)\}', Path(str(path) + '.meta').read_text())
        self.pivot = (float(m.group(1)) * self.image.width, self.image.height * (1 - float(m.group(2))))

    def mirrored(self):
        s = object.__new__(Sprite)
        s.path, s.image = self.path, self.image.transpose(Image.FLIP_LEFT_RIGHT)
        s.pivot = (self.image.width - self.pivot[0], self.pivot[1])
        return s


class Set:
    """An EnemyArtSet read from its YAML: its frames, its windup, its deaths and its numbers."""

    def __init__(self, enemy):
        folder, stem, sep, name, self.speed, self.interval = ENEMIES[enemy]
        text = (ART / folder / (name + '.asset')).read_text(encoding='utf-8')

        def block(field):
            m = re.search(r'\n  %s:\n((?:  [- ].*\n)+)' % field, text)
            return [GUIDS[g] for g in re.findall(r'guid: (\w+)', m.group(1))] if m else []

        def number(field, default):
            m = re.search(r'\n  %s: ([\d.]+)' % field, text)
            return float(m.group(1)) if m else default

        front, rear = block('_front'), block('_rear')
        self.front = [Sprite(p) for p in front[0::2]]
        self.rear = [Sprite(p) for p in rear[0::2]]
        windup = block('_strikeWindup')
        self.windup = [Sprite(windup[0]), Sprite(windup[2])]
        self.windup_seconds = number('_strikeWindupSeconds', 0)
        self.death = {False: [Sprite(p) for p in block('_deathFront')], True: [Sprite(p) for p in block('_deathRear')]}
        self.step = number('_deathStep', 0.06)
        self.remains = [Sprite(p) for p in block('_death')]
        self.cycle = number('_walkCycleDistance', 0.55)
        self.shadow = number('_shadowWidth', 0.5)

    def walking(self, distance):
        phase = (distance % self.cycle) / self.cycle
        if len(self.front) < 6:
            return 1 if phase < 0.5 else 2
        return [1, 4, 2, 5][min(3, int(phase * 4))]

    def beat(self):
        return min(self.windup_seconds, self.interval * 0.5)


def stone(size):
    """The Foundry's stone from a capture at the phone's own scale: --stone PATH (a 1080 x 2340 phone frame, its open
    floor between the HUD and the fight, y 250-930) or, without it, the repository's pit capture."""
    if '--stone' in ARGS:
        tile = Image.open(ARGS[ARGS.index('--stone') + 1]).convert('RGBA').crop((0, 250, 1080, 930))
    else:
        tile = Image.open(CAPTURE).convert('RGBA').crop((0, 1330, 560, 1700))
    out = Image.new('RGBA', size)
    for y in range(0, size[1], tile.height):
        for x in range(0, size[0], tile.width):
            out.paste(tile, (x, y))
    return out


def lava(size, seed=3):
    """Molten orange in the pit's sampled colours (review_foundry_enemies.lava)."""
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


FLOORS = {'stone': stone, 'lava': lava}


def shadow(canvas, at, width):
    layer = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
    rx, ry = width * PHONE / 2, width * PHONE / 4
    ImageDraw.Draw(layer).ellipse((at[0] - rx, at[1] - ry, at[0] + rx, at[1] + ry), fill=(8, 6, 12, 110))
    canvas.alpha_composite(layer.filter(ImageFilter.GaussianBlur(PHONE * 0.04)))


def paste(canvas, sprite, at, alpha=1.0, mirror=False):
    if mirror:
        sprite = sprite.mirrored()
    scale = PHONE / 128
    size = (max(1, round(sprite.image.width * scale)), max(1, round(sprite.image.height * scale)))
    image = sprite.image.resize(size, Image.BILINEAR)
    if alpha < 1:
        a = np.asarray(image).copy()
        a[..., 3] = (a[..., 3] * alpha).astype(np.uint8)
        image = Image.fromarray(a, 'RGBA')
    canvas.alpha_composite(image, (round(at[0] - sprite.pivot[0] * scale), round(at[1] - sprite.pivot[1] * scale)))


def label(canvas, text, at):
    d = ImageDraw.Draw(canvas)
    d.text((at[0] + 1, at[1] + 1), text, fill=(0, 0, 0, 255))
    d.text(at, text, fill=(240, 232, 220, 255))


def death_frame(s, rear, age):
    """The death on show `age` seconds in, its alpha and sink - CombatEffectsView's remains - or None once gone."""
    drawings = s.death[rear]
    life = len(drawings) * s.step
    if age >= life:
        return None
    frame = min(len(drawings) - 1, int(age / s.step))
    left = min(1.0, (life - age) / FADE)
    return drawings[frame], left, SINK * (1 - left)


def strike_timeline(s):
    """(seconds, state) of one strike cycle: walking in, standing in reach while the cooldown runs, the windup, the
    strike frame, then standing again until the next windup."""
    walk = 0.9
    stand = max(0.1, s.interval - s.beat() - STRIKE_HOLD) if s.interval > 0.6 else max(0.0, s.interval - s.beat() - STRIKE_HOLD)
    return [(walk, 'walk'), (min(stand, 0.5), 'idle'), (s.beat(), 'windup'), (STRIKE_HOLD, 'strike'), (0.3, 'idle')]


def state_at(s, t):
    for seconds, state in strike_timeline(s):
        if t < seconds:
            return state, t
        t -= seconds
    return 'idle', t


def strike_gif(enemy, floor):
    s = Set(enemy)
    lanes = [(False, False), (False, True), (True, False), (True, True)]   # (rear, mirrored)
    width, lane = 4 * 230, 230
    base = FLOORS[floor]((width, lane + 40))
    total = sum(x for x, _ in strike_timeline(s))
    frames, durations = [], []
    dt = 0.04
    t = 0.0
    while t < total:
        canvas = base.copy()
        state, local = state_at(s, t)
        for k, (rear, mirror) in enumerate(lanes):
            x0 = 115 + k * 230
            feet = (x0, lane - 10)
            walked = min(t, 0.9) * s.speed
            # Walks toward the target (left, or right mirrored) and stops in reach.
            offset = (0.9 * s.speed - walked) * PHONE * 0.35 * (1 if not mirror else -1)
            at = (feet[0] + offset * 0.4, feet[1])
            frames_of = s.rear if rear else s.front
            if state == 'walk':
                sprite = frames_of[s.walking(walked)]
            elif state == 'windup':
                sprite = s.windup[1 if rear else 0]
            elif state == 'strike':
                sprite = frames_of[3]
            else:
                sprite = frames_of[0]
            shadow(canvas, at, s.shadow)
            paste(canvas, sprite, at, mirror=mirror)
        label(canvas, '%s  %s  t=%.2fs  %s (windup %.2fs, interval %.1fs)' % (enemy, floor, t, state.upper(), s.beat(),
                                                                          s.interval), (8, 6))
        frames.append(canvas.convert('RGB'))
        durations.append(40)
        t += dt
    durations[-1] = 400
    frames[0].save(OUT / ('%s-strike-%s.gif' % (enemy, floor)), save_all=True, append_images=frames[1:],
                   duration=durations, loop=0)
    return frames


def death_gif(enemy, floor):
    s = Set(enemy)
    width, height = 3 * 260, 260
    base = FLOORS[floor]((width, height))
    frames, durations = [], []
    standing = 0.4
    life = len(s.death[False]) * s.step
    t = 0.0
    while t < standing + max(life, 0.4) + 0.3:
        canvas = base.copy()
        for k, rear in enumerate((False, True, None)):
            at = (130 + k * 260, height - 60)
            if t < standing:
                sprite = (s.rear if rear else s.front)[0]
                shadow(canvas, at, s.shadow)
                paste(canvas, sprite, at)
                continue
            age = t - standing
            if rear is None:
                # The two remains it replaces: 0.16 s each over 0.4 s, fading.
                if age < 0.4:
                    left = min(1.0, (0.4 - age) / FADE)
                    paste(canvas, s.remains[min(1, int(age / 0.16))], (at[0], at[1] + SINK * (1 - left) * PHONE),
                          alpha=left)
                continue
            shown = death_frame(s, rear, age)
            if shown:
                sprite, alpha, sink = shown
                paste(canvas, sprite, (at[0], at[1] + sink * PHONE), alpha=alpha)
        label(canvas, '%s  %s  t=%.2fs   front | rear | the two remains it replaces' % (enemy, floor, t), (8, 6))
        frames.append(canvas.convert('RGB'))
        durations.append(40)
        t += 0.04
    durations[-1] = 500
    frames[0].save(OUT / ('%s-death-%s.gif' % (enemy, floor)), save_all=True, append_images=frames[1:],
                   duration=durations, loop=0)


def crowd(floor):
    """Sixteen enemies round the Vanguard, each a moment into its own cycle - walking, winding up, striking, dying."""
    sets = {e: Set(e) for e in ENEMIES}
    width, height = 1080, 680
    base = FLOORS[floor]((width, height))
    hero_img = Image.open(ART / 'Vanguard/CHR_Vanguard_Front_Idle.tga').convert('RGBA')
    hero = object.__new__(Sprite)
    hero.path, hero.image = None, hero_img
    hero.pivot = (0.45343137 * hero_img.width, hero_img.height * (1 - 0.013888889))
    rng = np.random.default_rng(5)
    order = list(ENEMIES) * 4
    cast = []
    for i, enemy in enumerate(order[:18]):
        ang = 2 * np.pi * i / 18 + rng.uniform(-0.12, 0.12)
        r = rng.uniform(1.3, 3.4)
        x = width / 2 + np.cos(ang) * r * PHONE
        y = height / 2 + 40 + np.sin(ang) * r * PHONE * 0.5
        rear = np.sin(ang) > 0          # below the hero, it looks up at it
        mirror = np.cos(ang) < 0        # left of the hero, it looks right
        cast.append((enemy, x, y, rear, mirror, rng.uniform(0, 1.2), ['walk', 'windup', 'strike', 'death'][i % 4]))
    frames = []
    for f in range(30):
        t = f * 0.04
        canvas = base.copy()
        items = []
        for enemy, x, y, rear, mirror, phase, role in cast:
            s = sets[enemy]
            items.append((y, enemy, x, rear, mirror, phase, role, s))
        items.append((height / 2 + 40, 'hero', width / 2, False, False, 0, 'hero', None))
        for y, enemy, x, rear, mirror, phase, role, s in sorted(items, key=lambda it: it[0]):
            if role == 'hero':
                shadow(canvas, (x, y), 0.78)
                paste(canvas, hero, (x, y))
                continue
            frames_of = s.rear if rear else s.front
            if role == 'walk':
                shadow(canvas, (x, y), s.shadow)
                paste(canvas, frames_of[s.walking((t + phase) * s.speed)], (x, y), mirror=mirror)
            elif role == 'windup':
                shadow(canvas, (x, y), s.shadow)
                paste(canvas, s.windup[1 if rear else 0] if (t + phase) % 0.8 < 0.5 else frames_of[0], (x, y),
                      mirror=mirror)
            elif role == 'strike':
                shadow(canvas, (x, y), s.shadow)
                paste(canvas, frames_of[3] if (t + phase) % 0.8 < 0.3 else s.windup[1 if rear else 0], (x, y),
                      mirror=mirror)
            else:
                shown = death_frame(s, rear, (t + phase * 0.4) % 0.8)
                if shown:
                    sprite, alpha, sink = shown
                    paste(canvas, sprite, (x, y + sink * PHONE), alpha=alpha, mirror=mirror)
        label(canvas, 'A crowd at the phone\'s scale (%s): walking, winding up, striking and dying at once' % floor,
              (8, 6))
        frames.append(canvas.convert('RGB'))
    frames[12].save(OUT / ('crowd-%s.png' % floor))
    frames[0].save(OUT / ('crowd-%s.gif' % floor), save_all=True, append_images=frames[1:], duration=40, loop=0)


def sprite_sheet():
    """Every new sprite at 1:1 on a checker, its pivot (the floor root) marked with a cross, labelled."""
    tiles = []
    for enemy in ENEMIES:
        s = Set(enemy)
        row = [('windup front', s.windup[0]), ('windup rear', s.windup[1])]
        for rear in (False, True):
            seen = []
            for sprite in s.death[rear]:
                if sprite.path not in [p.path for p in seen]:
                    seen.append(sprite)
            row += [('%s %s %d' % ('rear' if rear else 'front', 'death', k), sp) for k, sp in enumerate(seen)]
        tiles.append((enemy, row))
    cell_h = max(sp.image.height for _, row in tiles for _, sp in row) + 18
    width = max(sum(sp.image.width + 6 for _, sp in row) for _, row in tiles) + 90
    sheet = Image.new('RGBA', (width, cell_h * len(tiles)), (40, 40, 48, 255))
    d = ImageDraw.Draw(sheet)
    for r, (enemy, row) in enumerate(tiles):
        y0 = r * cell_h
        d.text((4, y0 + 4), enemy, fill=(255, 255, 255, 255))
        x = 86
        for name, sp in row:
            w, h = sp.image.size
            checker = Image.new('RGBA', (w, h))
            cd = ImageDraw.Draw(checker)
            for cy in range(0, h, 8):
                for cx in range(0, w, 8):
                    cd.rectangle((cx, cy, cx + 7, cy + 7),
                                 fill=(110, 104, 112, 255) if (cx // 8 + cy // 8) % 2 else (88, 84, 92, 255))
            checker.alpha_composite(sp.image)
            sheet.alpha_composite(checker, (x, y0 + 14))
            px, py = sp.pivot
            cx, cy = x + px, y0 + 14 + py
            d.line((cx - 6, cy, cx + 6, cy), fill=(0, 255, 220, 255))
            d.line((cx, cy - 6, cx, cy + 6), fill=(0, 255, 220, 255))
            d.text((x, y0 + 2), '%s %dx%d' % (name, w, h), fill=(220, 220, 220, 255))
            x += w + 6
    sheet.save(OUT / 'sprites.png')


for enemy in ENEMIES:
    for floor in FLOORS:
        strike_gif(enemy, floor)
        death_gif(enemy, floor)
for floor in FLOORS:
    crowd(floor)
sprite_sheet()
print('HORDE_REVIEW', OUT)
