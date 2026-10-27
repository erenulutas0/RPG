"""Review images for the painted run feedback (build_run_feedback_vfx.py), composited from the game's own sprites.

    python Tools/ArtReview/review_run_feedback.py --out DIR [--only moments,picks,lava,gifs,film,crowd,frames]
                                                   [--capture IN_GAME_FRAME.png]

Draws what RunFeedbackView draws at the phone's scale (the Samsung S23 shows 120 pixels a world unit), as the GPU does:
bilinear sampling, straight alpha blended in sRGB (the project's gamma colour space). Before is the LineRenderers as the
phone draws them (no MSAA at the Android quality level): the 40-point loop at its width, each spark a line tapering to
nothing with a round head; after is the painted frames read back from VFX_RunFeedback.png and RunFeedbackArtSet.asset,
exactly what Unity imports (before ASTC). Both are armed, timed, sized, tinted and faded by the view's own rules, ported
below with its seeds, so every spark flies where the view flies it. The Vanguard (his painted front idle and its white
flash) and Ilya (her rendered atlases), the painted Grunt, Runner, Tank and Cinder Mite with their contact shadows and
health bars come from their own art; the floor is an in-game capture of the Foundry court, the lava a patch in the pit's
sampled colours (review_staff_impact's).

Writes: moments-before-after.png (each moment alone: the level-up, a card of each of the four tiers, the gold card's
wider ring among them, the evolution on Ilya, the weapon pop) and its x2 zoom, picks-before-after.png (what a pick after
a level-up shows, the level's burst spreading with the card's), tiers-at-a-glance.png (the four tiers side by side, alone
and after a level-up), lava-before-after.png, a GIF per moment at real timing (before and after side by side, 50 frames
a second: level-up, card-common, card-rare, card-epic, card-gold, evolution, weapon-pop, pick-rare-after-level-up),
film-*.png (every 60 Hz frame of a moment, painted), crowd-before.png, crowd-after.png and crowd.gif (a pick, and an
evolution, after a level-up in a horde of 37), painted-frames.png; with --capture, scale-check.png sets a real in-game
frame at the phone's framing over the composite.
"""
import json
import math
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
ARGS = sys.argv[1:]
OUT = Path(ARGS[ARGS.index("--out") + 1]) if "--out" in ARGS else ROOT / "TestResults/run-feedback-review"
OUT.mkdir(parents=True, exist_ok=True)
# review_staff_impact reads its own --out when imported; give it ours, so it makes no folder of its own.
sys.argv = [sys.argv[0], "--out", str(OUT)]
import review_staff_impact as staff  # noqa: E402  (its floors, sprites, sampling and GIF palette)

PHONE = staff.PHONE
FEEDBACK = ROOT / "Assets/_Project/Art/Effects/RunFeedback"
CHARACTERS = ROOT / "Assets/_Project/Art/Characters"

# ----------------------------------------------------------------------------------------------------------------------
# RunFeedbackView, ported: its constants, its arming and its drawing.

RING_SLOTS, SPARK_SLOTS, RING_SEGMENTS = 8, 32, 40
WAIST_HEIGHT = 0.35
SPARK_SEED = 53
LEVEL_UP_SECONDS, CARD_SECONDS, GOLD_SECONDS = 0.5, 0.42, 0.7
CORE_SECONDS, GOLD_CORE_SECONDS, HERO_FLASH_SECONDS = 0.16, 0.24, 0.3
LEVEL_GOLD = (1.0, 0.78, 0.3, 1.0)
LEVEL_SPARK = (1.0, 0.92, 0.6, 1.0)
EMBER = (1.0, 0.55, 0.22, 1.0)
WHITE = (1.0, 1.0, 1.0, 1.0)
# RunChoiceView.RarityColour: green, orange, red, gold.
TIERS = (("Common", (0x4C, 0xC3, 0x6A)), ("Rare", (0xF0, 0x8A, 0x24)), ("Epic", (0xE0, 0x39, 0x3E)),
         ("Gold", (0xF5, 0xC5, 0x42)))
RARITY = [tuple(c / 255.0 for c in rgb) + (1.0,) for _, rgb in TIERS]
GOLD_TIER = 3


def lerp(a, b, t):
    return tuple(x + (y - x) * t for x, y in zip(a, b))


def unity_smoothstep(x):
    """Mathf.SmoothStep(0, 1, x)."""
    x = min(max(x, 0.0), 1.0)
    return x * x * (3.0 - 2.0 * x)


def pixel_noise(x, y, seed):
    """PixelNoise.Value."""
    m = 0xFFFFFFFF
    h = (seed * 0x9E3779B1) & m
    h ^= (x * 0x85EBCA6B) & m
    h = ((h << 13) | (h >> 19)) & m
    h ^= (y * 0xC2B2AE35) & m
    h = (h * 0x27D4EB2F) & m
    h ^= h >> 15
    h = (h * 0x165667B1) & m
    h ^= h >> 13
    return (h & 0xFFFFFF) / 16777216.0


class Feedback:
    """The view's rings and sparks as armed at times (seconds), pooled as the view pools them: the oldest gives way."""

    def __init__(self):
        self.rings = [None] * RING_SLOTS
        self.sparks = [None] * SPARK_SLOTS
        self.next_ring = self.next_spark = self.bursts = 0
        self.flash_at = None

    def ring(self, at, look, delay, life, colour, r0, r1, w0, w1):
        self.rings[self.next_ring] = dict(at=at, look=look, delay=delay, life=life, colour=colour, r0=r0, r1=r1,
                                          w0=w0, w1=w1)
        self.next_ring = (self.next_ring + 1) % RING_SLOTS

    def spark(self, at, delay, life, start, velocity, gravity, length, width, colour, kind):
        self.sparks[self.next_spark] = dict(at=at, delay=delay, life=life, start=start, velocity=velocity,
                                            gravity=gravity, length=length, width=width, colour=colour, kind=kind)
        self.next_spark = (self.next_spark + 1) % SPARK_SLOTS

    # OnLevelChanged
    def level_up(self, at):
        self.ring(at, "level", 0.0, LEVEL_UP_SECONDS, LEVEL_GOLD, 0.2, 0.95, 0.1, 0.02)
        self.rise(at, 7, 0.32, 1.8, LEVEL_UP_SECONDS, LEVEL_SPARK, "level")

    # OnSelected
    def card(self, at, tier, evolution=False):
        colour = RARITY[tier]
        if evolution:
            self.evolution(at, colour)
            return
        core = lerp(colour, WHITE, 0.55)
        if tier == GOLD_TIER:
            self.ring(at, "core", 0.0, GOLD_CORE_SECONDS, core, 0.16, 0.42, 0.28, 0.08)
            self.ring(at, "card", 0.04, GOLD_SECONDS, colour, 0.2, 1.3, 0.12, 0.03)
            self.rise(at, 6, 0.4, 2.1, 0.6, lerp(colour, WHITE, 0.35), "card")
            return
        self.ring(at, "core", 0.0, CORE_SECONDS, core, 0.12, 0.3, 0.22, 0.06)
        self.ring(at, "card", 0.03, CARD_SECONDS, colour, 0.2, 0.85, 0.08, 0.02)

    # ShowEvolution
    def evolution(self, at, colour):
        self.ring(at, "core", 0.0, GOLD_CORE_SECONDS, lerp(colour, WHITE, 0.7), 0.14, 0.4, 0.3, 0.08)
        self.ring(at, "card", 0.04, GOLD_SECONDS, colour, 0.2, 1.25, 0.13, 0.03)
        self.ring(at, "ember", 0.18, 0.6, EMBER, 0.15, 0.95, 0.1, 0.025)
        self.rise(at, 8, 0.45, 2.4, 0.6, lerp(EMBER, WHITE, 0.3), "evolution")
        self.flash_at = at

    # OnWeaponAcquired
    def weapon(self, at):
        self.ring(at, "ember", 0.0, 0.3, EMBER, 0.1, 0.45, 0.06, 0.015)
        count = 6
        hot = lerp(EMBER, WHITE, 0.25)
        self.bursts += 1
        for i in range(count):
            a = math.radians(25.0 + (155.0 - 25.0) * i / (count - 1.0))
            n = pixel_noise(i, self.bursts, SPARK_SEED)
            velocity = (math.cos(a) * 1.6, 0.9 + math.sin(a) * 1.1 + n * 0.3)
            self.spark(at, 0.0, 0.38, (0.0, WAIST_HEIGHT), velocity, 6.5, 0.09, 0.045, hot, "weapon")

    # Rise
    def rise(self, at, count, radius, speed, life, colour, kind):
        self.bursts += 1
        for i in range(count):
            n = pixel_noise(i, self.bursts, SPARK_SEED)
            a = (i + 0.5 + (n - 0.5) * 0.6) * math.pi * 2.0 / count
            start = (math.cos(a) * radius, math.sin(a) * radius * 0.5 + 0.05)
            velocity = (math.cos(a) * 0.3, speed + n * 0.5)
            self.spark(at, (i % 3) * 0.04, life * (0.85 + 0.15 * n), start, velocity, speed * 1.3, 0.14, 0.05, colour,
                       kind)

    def shown_rings(self, time):
        """Every ring on show at time: (look, t, radius, width, colour with its fade)."""
        out = []
        for ring in self.rings:
            if ring is None:
                continue
            shown = time - ring["at"] - ring["delay"]
            if shown < 0 or shown >= ring["life"]:
                continue
            t = shown / ring["life"]
            ease = 1.0 - (1.0 - t) ** 3
            radius = ring["r0"] + (ring["r1"] - ring["r0"]) * ease
            width = ring["w0"] + (ring["w1"] - ring["w0"]) * t
            colour = list(ring["colour"])
            colour[3] *= 1.0 - unity_smoothstep((t - 0.3) / 0.7)
            out.append((ring["look"], t, radius, width, tuple(colour)))
        return out

    def shown_sparks(self, time):
        """Every spark on show at time: (t, head, heading, length, width, colour with its fade, its burst's kind)."""
        out = []
        for spark in self.sparks:
            if spark is None:
                continue
            shown = time - spark["at"] - spark["delay"]
            if shown < 0 or shown >= spark["life"]:
                continue
            t = shown / spark["life"]
            (sx, sy), (vx, vy), g = spark["start"], spark["velocity"], spark["gravity"]
            head = (sx + vx * shown, sy + vy * shown - 0.5 * g * shown * shown)
            hx, hy = vx, vy - g * shown
            norm = math.hypot(hx, hy)
            heading = (hx / norm, hy / norm) if norm * norm > 1e-4 else (0.0, 1.0)
            colour = list(spark["colour"])
            colour[3] *= 1.0 - unity_smoothstep((t - 0.35) / 0.65)
            out.append((t, head, heading, spark["length"], spark["width"], tuple(colour), spark["kind"]))
        return out

    def flash(self, time):
        """The hero's white over his silhouette: 0.9 of the remaining share squared."""
        if self.flash_at is None:
            return 0.0
        remaining = HERO_FLASH_SECONDS - (time - self.flash_at)
        if remaining <= 0 or time < self.flash_at:
            return 0.0
        return 0.9 * (remaining / HERO_FLASH_SECONDS) ** 2


# ----------------------------------------------------------------------------------------------------------------------
# Drawing.


class View(staff.View):
    def draw_affine(self, sprite, position, angle_degrees, sx, sy, tint):
        """A sprite rotated about its pivot and scaled along its own axes, bilinear, as a SpriteRenderer draws it."""
        tw, th = sprite.size
        c, s = math.cos(math.radians(angle_degrees)), math.sin(math.radians(angle_degrees))
        px, py = sprite.pivot[0] * tw, sprite.pivot[1] * th
        corners = []
        for u, v in ((0, 0), (tw, 0), (0, th), (tw, th)):
            lx, ly = (u - px) / sprite.ppu * sx, (v - py) / sprite.ppu * sy
            corners.append(self.to_screen(position[0] + lx * c - ly * s, position[1] + lx * s + ly * c))
        h, w = self.canvas.shape[:2]
        x0 = max(0, int(math.floor(min(p[0] for p in corners))))
        x1 = min(w, int(math.ceil(max(p[0] for p in corners))))
        y0 = max(0, int(math.floor(min(p[1] for p in corners))))
        y1 = min(h, int(math.ceil(max(p[1] for p in corners))))
        if x0 >= x1 or y0 >= y1:
            return
        gx, gy = np.meshgrid(np.arange(x0, x1) + 0.5, np.arange(y0, y1) + 0.5)
        cx, cy = self.to_screen(*position)
        wx, wy = (gx - cx) / PHONE, -(gy - cy) / PHONE
        lx, ly = wx * c + wy * s, -wx * s + wy * c
        u = lx / sx * sprite.ppu + px
        v = ly / sy * sprite.ppu + py  # texels from the bottom
        inside = (u >= 0) & (u <= tw) & (v >= 0) & (v <= th)
        sample = bilinear2d(sprite.rgba, u - 0.5, th - v - 0.5) * np.asarray(tint, np.float32)
        a = sample[..., 3:] * inside[..., None]
        region = self.canvas[y0:y1, x0:x1, :3]
        self.canvas[y0:y1, x0:x1, :3] = sample[..., :3] * a + region * (1.0 - a)

    def fill(self, coverage_fn, colour, bounds):
        """Hard-edged coverage (pixel centres, no MSAA) of a shape in world units, in one colour."""
        h, w = self.canvas.shape[:2]
        (wx0, wy0), (wx1, wy1) = bounds
        sx0, sy1 = self.to_screen(wx0, wy0)
        sx1, sy0 = self.to_screen(wx1, wy1)
        x0, x1 = max(0, int(math.floor(sx0)) - 1), min(w, int(math.ceil(sx1)) + 1)
        y0, y1 = max(0, int(math.floor(sy0)) - 1), min(h, int(math.ceil(sy1)) + 1)
        if x0 >= x1 or y0 >= y1:
            return
        gx, gy = np.meshgrid(np.arange(x0, x1) + 0.5, np.arange(y0, y1) + 0.5)
        wx = self.centre[0] + (gx - w / 2) / PHONE
        wy = self.centre[1] - (gy - h / 2) / PHONE
        cover, alpha = coverage_fn(wx, wy)
        a = (cover * alpha)[..., None] * colour[3]
        region = self.canvas[y0:y1, x0:x1, :3]
        self.canvas[y0:y1, x0:x1, :3] = np.asarray(colour[:3], np.float32) * a + region * (1.0 - a)


def bilinear2d(texture, tx, ty):
    """GPU bilinear sampling at texel coordinates of any shape, clamped at the edges."""
    h, w = texture.shape[:2]
    x0 = np.floor(tx).astype(int)
    y0 = np.floor(ty).astype(int)
    fx = (tx - x0)[..., None].astype(np.float32)
    fy = (ty - y0)[..., None].astype(np.float32)
    xa, xb = np.clip(x0, 0, w - 1), np.clip(x0 + 1, 0, w - 1)
    ya, yb = np.clip(y0, 0, h - 1), np.clip(y0 + 1, 0, h - 1)
    top = texture[ya, xa] * (1 - fx) + texture[ya, xb] * fx
    low = texture[yb, xa] * (1 - fx) + texture[yb, xb] * fx
    return top * (1 - fy) + low * fy


def line_ring(view, feet, radius, width, colour):
    """A LineRenderer loop of RING_SEGMENTS points on the floor ellipse at radius, width wide, hard-edged."""
    angles = np.arange(RING_SEGMENTS) * 2 * np.pi / RING_SEGMENTS
    pts = np.stack([feet[0] + np.cos(angles) * radius, feet[1] + np.sin(angles) * radius * 0.5], -1)
    a, b = pts, np.roll(pts, -1, axis=0)

    def coverage(wx, wy):
        best = np.full(wx.shape, np.inf, np.float32)
        for (ax, ay), (bx, by) in zip(a, b):
            dx, dy = bx - ax, by - ay
            u = np.clip(((wx - ax) * dx + (wy - ay) * dy) / (dx * dx + dy * dy), 0.0, 1.0)
            best = np.minimum(best, np.hypot(wx - ax - u * dx, wy - ay - u * dy))
        return (best <= width * 0.5).astype(np.float32), np.ones_like(best)

    margin = width
    view.fill(coverage, colour, ((feet[0] - radius - margin, feet[1] - radius * 0.5 - margin),
                                 (feet[0] + radius + margin, feet[1] + radius * 0.5 + margin)))


def line_spark(view, head, tail, width, colour):
    """A two-point LineRenderer: width at the head tapering to none at the tail, its alpha likewise, a round head."""
    (hx, hy), (tx, ty) = head, tail

    def coverage(wx, wy):
        dx, dy = tx - hx, ty - hy
        length2 = max(dx * dx + dy * dy, 1e-9)
        u = np.clip(((wx - hx) * dx + (wy - hy) * dy) / length2, 0.0, 1.0)
        d = np.hypot(wx - hx - u * dx, wy - hy - u * dy)
        half = width * 0.5 * (1.0 - u)
        return (d <= half).astype(np.float32), 1.0 - u

    pad = width
    view.fill(coverage, colour, ((min(hx, tx) - pad, min(hy, ty) - pad), (max(hx, tx) + pad, max(hy, ty) + pad)))


# ----------------------------------------------------------------------------------------------------------------------
# The art.


def load_art():
    art = {}
    strips = staff.parse_strips(FEEDBACK / "RunFeedbackArtSet.asset")
    for field, key in (("_levelRing", "level"), ("_cardRing", "card"), ("_cardCore", "core"), ("_emberRing", "ember"),
                       ("_goldSpark", "gold_spark"), ("_emberSpark", "ember_spark")):
        art[key] = staff.cut(strips[field], FEEDBACK, field)
    text = (FEEDBACK / "RunFeedbackArtSet.asset").read_text(encoding="utf-8")
    art["spark_width"] = float(re.search(r"_sparkWidth: ([\d.]+)", text).group(1)) / strips["_goldSpark"]["tpu"]
    vanguard = CHARACTERS / "Vanguard"
    art["vanguard"] = staff.tga(vanguard / "CHR_Vanguard_Front_Idle.tga", (0.45343137, 0.013888889))
    art["vanguard_flash"] = staff.tga(vanguard / "CHR_Vanguard_Front_Idle_Flash.tga", (0.45343137, 0.013888889))
    report = json.loads((ROOT / "ArtDirection/2026-10-27/arcanist-blender-01/frames.json").read_text())
    pages = [np.asarray(Image.open(CHARACTERS / "CinderArcanist" / a["file"]).convert("RGBA")) for a in report["atlases"]]
    for f in report["frames"]:
        if f["name"] != "idle_south_0":
            continue
        page = pages[f["atlas"]]
        x, y, w, h = f["rect"]
        top = page.shape[0] - y - h
        rgba = page[top:top + h, x:x + w]
        art["ilya"] = staff.Sprite(rgba, tuple(f["pivot"]), report["pixels_per_unit"], f["name"])
        white = rgba.copy()
        white[..., :3] = 255
        art["ilya_flash"] = staff.Sprite(white, tuple(f["pivot"]), report["pixels_per_unit"], f["name"] + " flash")
    art["grunt"] = staff.tga(CHARACTERS / "SlagBrute/CHR_Grunt_front-idle.tga", (0.5, 0.1))
    art["mite"] = staff.tga(CHARACTERS / "CinderMite/CHR_Mite_front_idle.tga", (0.4810606, 0.035714287))
    art["runner"] = staff.tga(CHARACTERS / "Runner/CHR_Runner_front-idle.tga", (0.5, 0.13157895))
    art["tank"] = staff.tga(CHARACTERS / "Tank/CHR_Tank_front-idle.tga", (0.5, 0.15853659))
    return art


# Where an enemy's health bar hangs (EnemyLookView: its painted art's visual top and 0.12 over it) and how wide it is
# (EnemyArt's 26x5 texel bar at 32 a unit; a Mite's is scaled 0.65 by 0.8).
ENEMIES = {"grunt": (1.05, 0.78), "mite": (0.5, 0.48), "runner": (0.95, 0.7), "tank": (1.15, 0.9)}


def health_bar(view, at, kind, fraction):
    top, _ = ENEMIES[kind]
    sx, sy = (0.65, 0.8) if kind == "mite" else (1.0, 1.0)
    cx, cy = at[0], at[1] + top + 0.12
    w, h = 26 / 32.0 * sx, 5 / 32.0 * sy

    def rect(x0, y0, x1, y1, colour):
        view.fill(lambda wx, wy: (((wx >= x0) & (wx < x1) & (wy >= y0) & (wy < y1)).astype(np.float32),
                                  np.ones_like(wx)), colour, ((x0, y0), (x1, y1)))

    texel_x, texel_y = 1 / 32.0 * sx, 1 / 32.0 * sy
    rect(cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2, (8 / 255, 8 / 255, 29 / 255, 1.0))
    rect(cx - w / 2 + texel_x, cy - h / 2 + texel_y, cx + w / 2 - texel_x, cy + h / 2 - texel_y,
         (0x15 / 255, 0x18 / 255, 0x20 / 255, 1.0))
    fill_w = (w - 2 * texel_x) * fraction
    left = cx - w / 2 + texel_x
    for row, colour in enumerate(((0x9E, 0x2A, 0x22), (0xE5, 0x3F, 0x32), (0xF4, 0x74, 0x6A))):
        y0 = cy - h / 2 + texel_y * (1 + row)
        rect(left, y0, left + fill_w, y0 + texel_y, tuple(c / 255 for c in colour) + (1.0,))


# ----------------------------------------------------------------------------------------------------------------------
# A scene: the hero on the floor, maybe a crowd round him, and what the view shows at a moment.


class Scene:
    def __init__(self, hero="vanguard", feet=(0.0, 0.0), enemies=(), bars=False):
        self.hero, self.feet, self.enemies, self.bars = hero, feet, list(enemies), bars


def frame_at(art, scene, feedback, time, painted, floor, size, centre):
    view = View(floor(size), centre)
    feet = scene.feet
    # The rings on the floor's order, under every actor: a contact shadow sorts inside its actor's group
    # (GroundedSorting), so even the hero's own shadow lies over them.
    for look, t, radius, width, colour in feedback.shown_rings(time):
        if painted:
            frames = art[look]
            sprite = frames[min(int(t * len(frames)), len(frames) - 1)]
            tint = colour if look in ("card", "core") else (1.0, 1.0, 1.0, colour[3])
            reach = radius + width * 0.5
            view.draw_affine(sprite, feet, 0.0, reach, reach, tint)
        else:
            line_ring(view, feet, radius, width, colour)
    # The actors, farther first, each over its own shadow; the hero lit white over his silhouette while an evolution's
    # flash lasts.
    actors = [(feet[1], "hero", None)] + [(at[1], kind, at) for kind, at, _ in scene.enemies]
    for _, kind, at in sorted(actors, key=lambda a: -a[0]):
        staff.shadow(view, feet if kind == "hero" else at,
                     {"hero": 0.78, "grunt": 0.78, "mite": 0.48, "runner": 0.66, "tank": 0.9}[kind])
        if kind == "hero":
            view.draw(art[scene.hero], feet)
            strength = feedback.flash(time)
            if strength > 0:
                view.draw(art[scene.hero + "_flash"], feet, tint=(1, 1, 1, strength))
        else:
            view.draw(art[kind], at, flip=at[0] < feet[0])
    # The sparks over the combatants.
    for t, head, heading, length, width, colour, kind in feedback.shown_sparks(time):
        hx, hy = feet[0] + head[0], feet[1] + head[1]
        if painted:
            # Gold for a level-up and a card, ember for an evolution and a weapon, in their own colours.
            frames = art["ember_spark" if kind in ("evolution", "weapon") else "gold_spark"]
            sprite = frames[min(int(t * len(frames)), len(frames) - 1)]
            size = width / art["spark_width"]
            view.draw_affine(sprite, (hx, hy), math.degrees(math.atan2(heading[1], heading[0])), size,
                             size * (1.0 - 0.5 * t), (1.0, 1.0, 1.0, colour[3]))
        else:
            line_spark(view, (hx, hy), (hx - heading[0] * length, hy - heading[1] * length), width * (1.0 - 0.5 * t),
                       colour)
    if scene.bars:
        for k, (kind, at, health) in enumerate(scene.enemies):
            health_bar(view, at, kind, health)
    return view.image()


def label(image, text, at=(8, 6)):
    return staff.label(image, text, at)


# ----------------------------------------------------------------------------------------------------------------------
# The moments.


def moment(name):
    """The feedback a moment arms at time 0, and how long it lasts."""
    fb = Feedback()
    if name == "level":
        fb.level_up(0.0)
        return fb, LEVEL_UP_SECONDS
    if name.startswith("card"):
        tier = int(name[-1])
        fb.card(0.0, tier)
        return fb, GOLD_SECONDS + 0.04 if tier == GOLD_TIER else CARD_SECONDS + 0.03
    if name == "evolution":
        fb.card(0.0, GOLD_TIER, evolution=True)
        return fb, 0.78
    if name == "weapon":
        fb.weapon(0.0)
        return fb, 0.38
    if name == "pick-weapon":
        fb.level_up(0.0)
        fb.card(0.0, 1)
        fb.weapon(0.0)
        return fb, LEVEL_UP_SECONDS
    if name == "pick-evolution":
        fb.level_up(0.0)
        fb.card(0.0, 2, evolution=True)
        return fb, 0.78
    if name.startswith("pick"):
        # A level-up opens its offer at once and holds its burst under the panel on scaled time; the card is taken and
        # the run moves on in the same frame, so both play out together from the pick.
        tier = int(name[-1])
        fb.level_up(0.0)
        fb.card(0.0, tier)
        return fb, GOLD_SECONDS + 0.04 if tier == GOLD_TIER else LEVEL_UP_SECONDS
    raise ValueError(name)


MOMENTS = (("level", "Level up"), ("card0", "Common card"), ("card1", "Rare card"), ("card2", "Epic card"),
           ("card3", "Gold card"), ("evolution", "Evolution (gold tier)"), ("weapon", "Weapon picked up"))
# The moments each sheet shows a moment at, in seconds: over its own life, the slow ones longer.
TIMES = (0.02, 0.06, 0.12, 0.2, 0.3, 0.42)
SLOW_TIMES = (0.02, 0.08, 0.16, 0.26, 0.4, 0.56)
QUICK_TIMES = (0.02, 0.05, 0.1, 0.16, 0.22, 0.3)
SIZE = (340, 300)


def times_for(key):
    if key in ("card3", "evolution", "pick3", "pick-evolution"):
        return SLOW_TIMES
    return QUICK_TIMES if key == "weapon" else TIMES


def hero_scene(hero="vanguard"):
    return Scene(hero, (0.0, 0.0))


def moments_sheet(art, name, floor, moments):
    """Each moment a row pair, before over after, at the times its life is shown at (times_for)."""
    w, h = SIZE
    out = Image.new("RGB", (w * len(TIMES), h * 2 * len(moments)), (10, 8, 12))
    for m, (key, caption) in enumerate(moments):
        for p, painted in enumerate((False, True)):
            for c, time in enumerate(times_for(key)):
                fb, _ = moment(key)
                image = frame_at(art, hero_scene("ilya" if key == "evolution" else "vanguard"), fb, time, painted,
                                 floor, SIZE, (0.0, 0.45))
                label(image, f"{caption} {'after' if painted else 'before'} {time * 1000:.0f} ms")
                out.paste(image, (c * w, (m * 2 + p) * h))
    out.save(OUT / f"{name}.png")
    return out


def zoom(image, box, name):
    crop = image.crop(box)
    crop.resize((crop.width * 2, crop.height * 2), Image.NEAREST).save(OUT / name)


def tiers_sheet(art):
    """The four tiers side by side, each a card taken alone and after a level-up, at three moments: the hue each reads."""
    w, h = SIZE
    times = (0.06, 0.14, 0.26)
    out = Image.new("RGB", (w * 4, h * len(times) * 2), (10, 8, 12))
    for r, (combined, time) in enumerate([(c, t) for c in (False, True) for t in times]):
        for tier in range(4):
            fb, _ = moment(("pick" if combined else "card") + str(tier))
            image = frame_at(art, hero_scene(), fb, time, True, staff.court, SIZE, (0.0, 0.45))
            label(image, f"{TIERS[tier][0]}{' after a level-up' if combined else ''} {time * 1000:.0f} ms")
            out.paste(image, (tier * w, r * h))
    out.save(OUT / "tiers-at-a-glance.png")


# The GIFs' names.
GIF_NAMES = {"level": "level-up", "card0": "card-common", "card1": "card-rare", "card2": "card-epic",
             "card3": "card-gold", "evolution": "evolution", "weapon": "weapon-pop", "pick1": "pick-rare-after-level-up"}


def moment_gif(art, key, caption, hero, floor=staff.court):
    fb, life = moment(key)
    frames = []
    hold = 0.25
    total = life + 2 * hold
    for k in range(int(total / 0.02) + 1):
        time = k * 0.02 - hold
        row = []
        for painted in (False, True):
            image = frame_at(art, hero_scene(hero), fb, time, painted, floor, SIZE, (0.0, 0.45))
            label(image, f"{caption} {'after' if painted else 'before'}")
            row.append(image)
        both = Image.new("RGB", (SIZE[0] * 2, SIZE[1]))
        both.paste(row[0], (0, 0))
        both.paste(row[1], (SIZE[0], 0))
        frames.append(both)
    keys = [int((hold + t) / 0.02) for t in (0.03, 0.1, 0.2, 0.3) if hold + t < total]
    staff.save_gif(frames, OUT / f"{GIF_NAMES[key]}.gif", 20, keys)


def filmstrip(art, key, count):
    """Every frame the phone shows at 60 a second, from the moment on, painted: how the band steps from one painted
    frame to the next while the ring spreads."""
    size, cols = (340, 220), 8
    rows = (count + cols - 1) // cols
    sheet = Image.new("RGB", (size[0] * cols, size[1] * rows), (10, 8, 12))
    for k in range(count):
        fb, _ = moment(key)
        image = frame_at(art, hero_scene("ilya" if key == "evolution" else "vanguard"), fb, k / 60.0, True, staff.court,
                         size, (0.0, 0.2))
        label(image, f"{k / 60.0 * 1000:.0f} ms")
        sheet.paste(image, ((k % cols) * size[0], (k // cols) * size[1]))
    sheet.save(OUT / f"film-{GIF_NAMES[key]}.png")


def crowd_layout(seed=11):
    """A horde of 34 round the hero, as a fight a minute in looks: Grunts, Mites, Runners and Tanks, some hurt."""
    rng = np.random.default_rng(seed)
    enemies = []
    while len(enemies) < 34:
        x, y = rng.uniform(-4.2, 4.2), rng.uniform(-3.0, 4.0)
        if math.hypot(x, y * 1.6) < 1.05:
            continue
        if any(math.hypot(x - e[1][0], (y - e[1][1]) * 1.6) < 0.66 for e in enemies):
            continue
        kind = ("grunt", "mite", "runner", "tank")[int(rng.choice(4, p=(0.4, 0.3, 0.2, 0.1)))]
        enemies.append((kind, (x, y), float(rng.uniform(0.25, 1.0))))
    # A few pressing in close, as they do once the panel lets the run go.
    enemies += [("grunt", (0.85, 0.35), 0.7), ("mite", (-0.75, -0.3), 0.5), ("runner", (-0.55, 0.62), 0.9)]
    return enemies


def crowd(art):
    enemies = crowd_layout()
    scene = Scene("vanguard", (0.0, 0.0), enemies, bars=True)
    size, centre = (1080, 900), (0.0, 0.6)
    for painted in (False, True):
        fb, _ = moment("pick2")
        image = frame_at(art, scene, fb, 0.12, painted, staff.stone, size, centre)
        label(image, f"{'after' if painted else 'before'}: an Epic card taken after a level-up, 120 ms, 37 enemies")
        image.save(OUT / f"crowd-{'after' if painted else 'before'}.png")
    frames = []
    fb, life = moment("pick-evolution")
    gif_size, gif_centre = (560, 520), (0.0, 0.5)
    for k in range(int((life + 0.3) / 0.02)):
        time = k * 0.02 - 0.1
        row = [frame_at(art, scene, fb, time, painted, staff.stone, gif_size, gif_centre) for painted in (False, True)]
        both = Image.new("RGB", (gif_size[0] * 2, gif_size[1]))
        both.paste(row[0], (0, 0))
        both.paste(row[1], (gif_size[0], 0))
        label(both, "before")
        label(both, "after: an evolution taken after a level-up, in the crowd", (gif_size[0] + 8, 6))
        frames.append(both)
    staff.save_gif(frames, OUT / "crowd.gif", 20, [7, 11, 16, 24], dither=False)


def picks(art):
    moments = (("pick0", "Common after a level"), ("pick1", "Rare after a level"), ("pick3", "Gold after a level"),
               ("pick-weapon", "Rare weapon after a level"), ("pick-evolution", "Evolution after a level"))
    moments_sheet(art, "picks-before-after", staff.court, moments)


def painted_frames(art):
    """Every painted frame as cut, on a neutral grey, the tinted ones in each tier's colour."""
    rows = []
    for key, tints in (("level", [WHITE]), ("card", RARITY), ("core", [lerp(c, WHITE, 0.55) for c in RARITY]),
                       ("ember", [WHITE]), ("gold_spark", [WHITE]), ("ember_spark", [WHITE])):
        for tint in tints:
            tiles = []
            for sprite in art[key]:
                rgba = sprite.rgba * np.asarray(tint, np.float32)
                base = np.full(rgba.shape[:2] + (3,), (58 / 255, 54 / 255, 64 / 255), np.float32)
                out = rgba[..., :3] * rgba[..., 3:] + base * (1 - rgba[..., 3:])
                im = Image.fromarray((np.clip(out, 0, 1) * 255 + 0.5).astype(np.uint8), "RGB")
                scale = 4 if key.endswith("spark") else 2 if key == "core" else 1
                tiles.append(im.resize((im.width * scale, im.height * scale), Image.NEAREST))
            width = sum(t.width + 6 for t in tiles)
            height = max(t.height for t in tiles) + 16
            row = Image.new("RGB", (width, height), (24, 22, 28))
            ImageDraw.Draw(row).text((4, 2), f"{key} x{len(tiles)}", fill=(235, 225, 210))
            x = 0
            for t in tiles:
                row.paste(t, (x, 16))
                x += t.width + 6
            rows.append(row)
    out = Image.new("RGB", (max(r.width for r in rows), sum(r.height for r in rows)), (24, 22, 28))
    y = 0
    for r in rows:
        out.paste(r, (0, y))
        y += r.height
    out.save(OUT / "painted-frames.png")


def scale_check(art, capture):
    """A real in-game frame of the crowd at the phone's framing beside the composite, both 1:1."""
    real = Image.open(capture).convert("RGB").crop((0, 820, 1080, 1540))
    enemies = crowd_layout()
    fb, _ = moment("pick2")
    ours = frame_at(art, Scene("vanguard", (0.0, 0.0), enemies, bars=True), fb, 0.12, True, staff.stone, (1080, 720),
                    (0.0, 0.6))
    label(real, "in game (no feedback showing), 1:1")
    label(ours, "composite, an Epic card after a level-up at 120 ms, 1:1")
    out = Image.new("RGB", (1080, 1440))
    out.paste(real, (0, 0))
    out.paste(ours, (0, 720))
    out.save(OUT / "scale-check.png")


def main():
    art = load_art()
    if "--capture" in ARGS:
        scale_check(art, ARGS[ARGS.index("--capture") + 1])
    only = ARGS[ARGS.index("--only") + 1].split(",") if "--only" in ARGS else None

    def run_moments():
        image = moments_sheet(art, "moments-before-after", staff.court, MOMENTS)
        w, h = SIZE
        zoom(image, (w, 0, w * 3, h * 14), "moments-before-after-x2.png")

    jobs = {"moments": run_moments,
            "picks": lambda: (picks(art), tiers_sheet(art)),
            "lava": lambda: moments_sheet(art, "lava-before-after", staff.lava, MOMENTS),
            "gifs": lambda: [moment_gif(art, key, caption, "ilya" if key == "evolution" else "vanguard")
                             for key, caption in MOMENTS + (("pick1", "Rare card after a level-up"),)],
            "film": lambda: [filmstrip(art, key, count) for key, count in (("level", 32), ("card2", 24), ("card3", 40),
                                                                           ("evolution", 48))],
            "crowd": lambda: crowd(art),
            "frames": lambda: painted_frames(art)}
    for name, job in jobs.items():
        if only is None or name in only:
            job()
    print("RUN_FEEDBACK_REVIEW", OUT)


if __name__ == "__main__":
    main()
