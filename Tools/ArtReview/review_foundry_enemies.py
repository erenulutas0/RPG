"""Review material for the Runner, the Tank and the Grunt Captain (build_foundry_enemies.py): exactly the TGA pixels
Unity imports, beside the painted Grunt, the Cinder Mite and the Vanguard.

    python Tools/ArtReview/review_foundry_enemies.py --out DIR

Writes to DIR, at the phone's scale (the Samsung S23 shows 120 pixels a world unit) and zoomed:
    beside-<enemy>-<floor>.png    the enemy (front idle, front contact, rear idle) beside the Grunt, Mite and Vanguard
    crowd-<floor>.png             a mixed crowd of the Foundry's six enemy kinds
    before-after-<floor>.png      each enemy's pixel look (EnemyArt, kept in pixel-before/) beside its new sprites
    <enemy>-walk.gif              four directions walking at the enemy's speed (the frames chosen by travelled
                                  distance, as EnemyLookView does), a strike, then the idle
    <enemy>-death.gif             the idle, the two remains frames fading and sinking as the pooled remains do
    approach-<enemy>.png          the comparison behind each choice of method
<floor> is the Foundry's dark stone (cropped from an in-game capture) or molten orange (a patch in the pit's colours).
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
ARGS = sys.argv[1:]
OUT = Path(ARGS[ARGS.index('--out') + 1]) if '--out' in ARGS else ROOT / 'TestResults/foundry-enemies-review'
OUT.mkdir(parents=True, exist_ok=True)
ART = ROOT / 'Assets/_Project/Art/Characters'
DOCS = ROOT / 'ArtDirection/2026-10-27/foundry-enemies-01'
CAPTURE = ROOT / 'ArtDirection/2026-10-24/foundry-pit-eruption-01/pit-spill.png'
RENDER = ROOT / 'TestResults/foundry-enemies-render'
PHONE = 120
PPU = 128
REPORT = {e['enemy']: e for e in json.loads((DOCS / 'sprites.json').read_text())['enemies']}
FOLDERS = {'runner': ('Runner', 'Runner'), 'tank': ('Tank', 'Tank'), 'captain': ('GruntCaptain', 'Captain')}
# Speeds from the EnemyDefinition assets (Assets/_Project/Data/Enemies), walk cycles from the art sets.
SPEED = {'grunt': 1.6, 'runner': 3.0, 'tank': 0.9, 'captain': 1.4, 'mite': None}
CYCLE = {'grunt': 0.7}
PIXEL = DOCS / 'pixel-before'


class Sprite:
    def __init__(self, image, pivot, ppu=PPU):
        self.image, self.pivot, self.ppu = image, pivot, ppu

    def mirrored(self):
        return Sprite(self.image.transpose(Image.FLIP_LEFT_RIGHT), (self.image.width - self.pivot[0], self.pivot[1]),
                      self.ppu)


def enemy_sprite(enemy, frame):
    folder, stem = FOLDERS[enemy]
    image = Image.open(ART / folder / ('CHR_%s_%s.tga' % (stem, frame))).convert('RGBA')
    px, py = REPORT[enemy]['pivot']
    return Sprite(image, (px, image.height - py))


def grunt_sprite(frame):
    image = Image.open(ART / 'SlagBrute' / ('CHR_Grunt_%s.tga' % frame)).convert('RGBA')
    return Sprite(image, (90, image.height - 18))


def mite_sprite(frame='front_idle'):
    image = Image.open(ART / 'CinderMite' / ('CHR_Mite_%s.tga' % frame)).convert('RGBA')
    return Sprite(image, (0.4810606 * image.width, image.height * (1 - 0.035714287)))


def vanguard_sprite():
    image = Image.open(ART / 'Vanguard/CHR_Vanguard_Front_Idle.tga').convert('RGBA')
    return Sprite(image, (0.45343137 * image.width, image.height * (1 - 0.013888889)))


def pixel_sprite(look, pose='IdleA'):
    """The pixel look as the game drew it: 32 texels a unit, the pivot at the bottom centre (row 0 is the floor)."""
    image = Image.open(PIXEL / ('%s_%s.png' % (look, pose))).convert('RGBA')
    return Sprite(image, (image.width / 2, image.height), 32)


SHADOW = {'grunt': .78, 'mite': .45, 'vanguard': .78}


def shadow_width(name):
    return REPORT[name]['shadow_width'] if name in REPORT else SHADOW.get(name, .6)


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


FLOORS = {'stone': stone, 'lava': lava}


def shadow(canvas, at, width):
    """The game's contact shadow (ContactShadowView: the set's width, on the 2:1 floor)."""
    layer = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
    rx, ry = width * PHONE / 2, width * PHONE / 4
    ImageDraw.Draw(layer).ellipse((at[0] - rx, at[1] - ry, at[0] + rx, at[1] + ry), fill=(8, 6, 12, 110))
    canvas.alpha_composite(layer.filter(ImageFilter.GaussianBlur(PHONE * 0.04)))


def paste(canvas, sprite, at, alpha=1.0, smooth=True):
    scale = PHONE / sprite.ppu
    size = (max(1, round(sprite.image.width * scale)), max(1, round(sprite.image.height * scale)))
    image = sprite.image.resize(size, Image.BILINEAR if smooth else Image.NEAREST)
    if alpha < 1:
        a = np.asarray(image).copy()
        a[..., 3] = (a[..., 3] * alpha).astype(np.uint8)
        image = Image.fromarray(a, 'RGBA')
    canvas.alpha_composite(image, (round(at[0] - sprite.pivot[0] * scale), round(at[1] - sprite.pivot[1] * scale)))


def label(canvas, text, at):
    d = ImageDraw.Draw(canvas)
    d.text((at[0] + 1, at[1] + 1), text, fill=(0, 0, 0, 255))
    d.text(at, text, fill=(240, 232, 220, 255))


def save_pair(canvas, name, zoom=3):
    canvas = canvas.convert('RGB')
    canvas.save(OUT / (name + '.png'))
    canvas.resize((canvas.width * zoom, canvas.height * zoom), Image.NEAREST).save(OUT / (name + '-x%d.png' % zoom))


# ---- Each enemy beside the painted Grunt, the Mite and the Vanguard ----
def beside(enemy):
    for floor, make in FLOORS.items():
        canvas = make((860, 250)).copy()
        cast = [('vanguard', vanguard_sprite()), ('grunt', grunt_sprite('front-idle')), ('mite', mite_sprite()),
                (enemy, enemy_sprite(enemy, 'front-idle')), (enemy, enemy_sprite(enemy, 'front-step-a')),
                (enemy, enemy_sprite(enemy, 'rear-idle')), ('grunt', grunt_sprite('rear-idle'))]
        for k, (who, sprite) in enumerate(cast):
            at = (70 + k * 120, 215)
            shadow(canvas, at, shadow_width(who))
            paste(canvas, sprite, at)
        label(canvas, '%s beside the Vanguard, the painted Grunt and the Cinder Mite - 120 px a unit' % enemy, (8, 6))
        save_pair(canvas, 'beside-%s-%s' % (enemy, floor))


# ---- A mixed crowd of all six kinds ----
def crowd():
    rng = np.random.default_rng(5)
    kinds = (['grunt'] * 5 + ['mite'] * 8 + ['runner'] * 4 + ['tank'] * 2 + ['captain'] * 2 + ['warden'])
    spots = []
    for kind in kinds:
        for _ in range(200):
            x, y = rng.uniform(60, 1020), rng.uniform(150, 560)
            if all((x - sx) ** 2 + ((y - sy) * 2) ** 2 > 75 ** 2 for sx, sy, _ in spots):
                break
        spots.append((x, y, kind))
    spots.sort(key=lambda s: s[1])
    for floor, make in FLOORS.items():
        canvas = make((1080, 600)).copy()
        for x, y, kind in spots:
            left = rng.random() < 0.5
            rear = rng.random() < 0.35
            facing = 'rear' if rear else 'front'
            if kind == 'grunt':
                sprite = grunt_sprite('%s-%s' % (facing, rng.choice(['idle', 'step-a', 'pass-b'])))
            elif kind == 'mite':
                sprite = mite_sprite('%s_%s' % (facing, rng.choice(['idle', 'step-a'])))
            elif kind == 'warden':
                sprite = pixel_sprite('Warden')
            else:
                sprite = enemy_sprite(kind, '%s-%s' % (facing, rng.choice(['idle', 'step-a', 'pass-a', 'step-b'])))
            if not left and kind != 'warden':
                sprite = sprite.mirrored()
            shadow(canvas, (x, y), shadow_width(kind) if kind != 'warden' else 1.25)
            paste(canvas, sprite, (x, y), smooth=kind != 'warden')
        label(canvas, 'The Foundry crowd: Grunt, Cinder Mite, Runner, Tank, Grunt Captain and the Forge Warden '
                      '(its pixel look: the boss is drawn separately) - 120 px a unit', (8, 6))
        save_pair(canvas, 'crowd-%s' % floor, zoom=2)


# ---- Before and after ----
def before_after():
    # Each pair spaced by its width, so the Tank's two never overlap.
    looks = [('runner', 'Runner', 90), ('tank', 'Tank', 175), ('captain', 'Captain', 150)]
    for floor, make in FLOORS.items():
        canvas = make((920, 230)).copy()
        x = 60
        for enemy, look, gap in looks:
            for j, (sprite, smooth) in enumerate(((pixel_sprite(look), False), (enemy_sprite(enemy, 'front-idle'), True))):
                at = (x + j * gap, 200)
                shadow(canvas, at, shadow_width(enemy))
                paste(canvas, sprite, at, smooth=smooth)
            label(canvas, '%s: pixel | painted' % enemy, (x - 40, 12))
            x += 2 * gap + 40
        save_pair(canvas, 'before-after-%s' % floor)


# ---- Walk and strike GIFs ----
def walking_frame(distance, cycle):
    """EnemyArtSet.WalkingFrame: contact A, passing A, contact B, passing B over one cycle of floor distance."""
    phase = (distance % cycle) / cycle
    quarter = min(3, int(phase * 4))
    return [1, 4, 2, 5][quarter]


ORDER = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b']


def frame_sprite(enemy, rear, index):
    facing = 'rear' if rear else 'front'
    if enemy == 'grunt':
        return grunt_sprite('%s-%s' % (facing, ORDER[index]))
    return enemy_sprite(enemy, '%s-%s' % (facing, ORDER[index]))


def walk_gif(enemy):
    cycle = REPORT[enemy]['walk_cycle_distance'] if enemy in REPORT else CYCLE[enemy]
    speed = SPEED[enemy]
    dt = 0.04
    lanes = [(False, False), (False, True), (True, False), (True, True)]   # (rear, mirrored = moving right)
    width, lane_h = 760, 190
    floor = stone((width, lane_h * len(lanes)))
    frames, delays = [], []
    walk_time = 2.4
    steps = int(walk_time / dt)
    script = [('walk', t) for t in range(steps)] + [('strike', t) for t in range(4)] + [('idle', t) for t in range(12)]
    for kind, t in script:
        canvas = floor.copy()
        for k, (rear, right) in enumerate(lanes):
            distance = speed * dt * (t if kind == 'walk' else steps)
            span = width - 160
            travelled = (distance * PHONE) % span
            x = 80 + (travelled if right else span - travelled)
            y = k * lane_h + lane_h - 28
            index = walking_frame(distance, cycle) if kind == 'walk' else 3 if kind == 'strike' else 0
            sprite = frame_sprite(enemy, rear, index)
            if right:
                sprite = sprite.mirrored()
            shadow(canvas, (x, y), shadow_width(enemy))
            paste(canvas, sprite, (x, y))
        label(canvas, '%s at %.1f units a second, %.2f-unit cycle: %s' % (enemy, speed, cycle, kind), (8, 6))
        frames.append(canvas.convert('RGB'))
        delays.append(40 if kind != 'strike' else 35)
    frames[0].save(OUT / ('%s-walk.gif' % enemy), save_all=True, append_images=frames[1:], duration=delays, loop=0,
                   disposal=1)


def death_gif(enemy):
    """The pooled remains (CombatEffectsView.OnEnemyDefeated): two frames over 0.4 s, 0.16 s each, fading and
    sinking, mirrored when the enemy faced right."""
    floor = stone((360, 200))
    frames, delays = [], []
    idle = enemy_sprite(enemy, 'front-idle') if enemy in REPORT else grunt_sprite('front-idle')
    deaths = [enemy_sprite(enemy, 'death-collapse'), enemy_sprite(enemy, 'death-cool')] if enemy in REPORT else \
        [grunt_sprite('death-collapse'), grunt_sprite('death-cool')]
    for t in range(14):
        canvas = floor.copy()
        shadow(canvas, (180, 170), shadow_width(enemy))
        paste(canvas, idle, (180, 170))
        frames.append(canvas.convert('RGB'))
        delays.append(40)
    for t in range(10):
        age = t * 0.04
        canvas = floor.copy()
        sprite = deaths[0] if age < 0.16 else deaths[1]
        fade = 1 - max(0.0, (age - 0.16) / 0.24)
        sink = 0.06 * age / 0.4 * PHONE
        paste(canvas, sprite, (180, 170 + sink), alpha=max(0.0, fade))
        frames.append(canvas.convert('RGB'))
        delays.append(40)
    for t in range(12):
        frames.append(floor.copy().convert('RGB'))
        delays.append(40)
    frames[0].save(OUT / ('%s-death.gif' % enemy), save_all=True, append_images=frames[1:], duration=delays, loop=0,
                   disposal=1)


# ---- The comparison behind each method ----
def approach():
    """Captain: the kitbash of the Grunt's masters against the rendered candidate; Runner and Tank: their renders
    against the painted Grunt squeezed and stretched to their pixel looks' proportions - the kitbash route."""
    candidate = RENDER / 'captain' / 'front-idle.png'
    for floor, make in FLOORS.items():
        canvas = make((700, 240)).copy()
        cast = [('painted Grunt', grunt_sprite('front-idle'), 'grunt'),
                ('Captain: kitbash (chosen)', enemy_sprite('captain', 'front-idle'), 'captain')]
        if candidate.exists():
            sys.path.insert(0, str(ROOT / 'Tools/ArtReview'))
            import build_foundry_enemies as pack
            meta = json.loads((RENDER / 'captain' / 'frames.json').read_text())
            px = pack.process_render(candidate, RENDER / 'captain' / 'mask' / 'front-idle.png', meta['supersample'])
            image = Image.fromarray(px, 'RGBA')
            cast.append(('Captain: rendered (rejected)', Sprite(image, (meta['pivot'][0], image.height - meta['pivot'][1])),
                         'captain'))
        for k, (text, sprite, who) in enumerate(cast):
            at = (110 + k * 230, 210)
            shadow(canvas, at, shadow_width(who))
            paste(canvas, sprite, at)
            label(canvas, text, (at[0] - 80, 12))
        save_pair(canvas, 'approach-captain-%s' % floor)
        canvas = make((900, 240)).copy()
        grunt = grunt_sprite('front-idle')
        cast = [('painted Grunt', grunt, 'grunt')]
        for enemy, (w, h) in (('runner', (0.52, 0.95)), ('tank', (1.3, 1.0))):
            box = grunt.image.getbbox()
            body = grunt.image.crop(box)
            size = (round(w * PPU), round(h * PPU))
            squeezed = Image.new('RGBA', (size[0] + 8, size[1] + 8))
            squeezed.alpha_composite(body.resize(size, Image.LANCZOS), (4, 4))
            cast.append(('%s: Grunt reshaped (rejected)' % enemy, Sprite(squeezed, (squeezed.width / 2, squeezed.height - 6)),
                         enemy))
            cast.append(('%s: rendered (chosen)' % enemy, enemy_sprite(enemy, 'front-idle'), enemy))
        for k, (text, sprite, who) in enumerate(cast):
            at = (90 + k * 180, 210)
            shadow(canvas, at, shadow_width(who))
            paste(canvas, sprite, at)
            label(canvas, text, (at[0] - 80, 12 + 14 * (k % 2)))
        save_pair(canvas, 'approach-runner-tank-%s' % floor)


for enemy in ('runner', 'tank', 'captain'):
    beside(enemy)
    walk_gif(enemy)
    death_gif(enemy)
walk_gif('grunt')
crowd()
before_after()
approach()
print('FOUNDRY_REVIEW', OUT)
