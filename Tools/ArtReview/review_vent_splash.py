"""Review images for the Crucible Vent's painted eruption ring (build_skill_vfx.py), composited from the game's sprites.

    python Tools/ArtReview/review_vent_splash.py --out DIR [--only stone,lava,stacks,staff,crowd,gif,frames]
                                                 [--capture IN_GAME_FRAME.png]

Draws what CombatEffectsView draws for a Crucible Vent at the phone's scale (the Samsung S23 shows 120 pixels a world
unit, 1080 across a 9-unit view), as the GPU does: bilinear sampling, straight alpha blended in sRGB (the project's
gamma colour space). The drawing, the floors, the actors, the hit sparks, the numerals and the Staff's painted hit are
review_staff_impact's. Before is the procedural ring StaffSplashArt draws, tinted ember; after is the painted ring read
back from VFX_Skill_VentSplash.png and SkillEffectArtSet.asset, exactly what Unity imports (before ASTC). Both are
timed and sized by the view's own rules: BurnRule.VentSplashRadius, the growth from 24/38 of it over 0.14 s and the fade
over the last 0.11 s, the painted ring's 0.055 s frames, its heat by the stacks it spent and every other eruption
mirrored about its centre. Around them, as the view draws them: each eruption's painted column, scaled by its stacks,
and the embers rising off it; Ilya's rendered Vent cast and the ember cast ring at her feet; the ability's reach ring,
ready and then cooling; the burn flames until the Vent spends them; and the hit spark and number of every enemy each
eruption's splash reaches (eight a stack), as many as the view's pools hold.

Writes: before-after-stone.png and before-after-lava.png (and x2 zooms of their telling moments), stacks.png (one
stack beside a full burn, on stone and on lava), staff-and-vent.png (a painted Staff hit beside a painted eruption),
crowd-before.png and crowd-after.png (a Vent through eleven burning enemies round Ilya), vent.gif (the same at real
timing, 50 frames a second, before and after side by side) and painted-frames.png; with --capture, scale-check.png sets
a real in-game frame of Ilya in a crowd at the phone's framing over the composited crowd.
"""
import math
import sys

import numpy as np
from PIL import Image, ImageDraw

import review_staff_impact as rs  # reads --out from the same command line, so both write to one folder

OUT = rs.OUT
ARGS = sys.argv[1:]

# CombatEffectsView's rules for a Vent (OnAbilityUsed, OnVented and the effect update), and the ability's numbers.
VENT_SPLASH = 1.2  # BurnRule.VentSplashRadius
MAX_STACKS = 5  # BurnRule.MaxStacks
SPLASH_FRAME = 0.055  # PaintedVentSplashFrameDuration
SPLASH_COOL = (1.0, 0.7, 0.55, 0.75)  # VentSplashCool: one stack's tint; a full burn's is white
COLUMN_FRAME = 0.05  # PaintedVentColumnFrameDuration
COLUMN_SCALE, COLUMN_PER_STACK = 0.62, 0.1
EMBER_FRAME, EMBER_LIFE, EMBER_FADE = 0.15, 0.6, 0.16
EMBER_RISE, EMBER_RISE_PER_STACK = 0.7, 0.12
EMBER_SCALE, EMBER_SCALE_PER_STACK = 0.8, 0.08
EMBER_LIFT = 0.45
CAST_FRAME, CAST_LIFE = 0.07, 0.21  # VentCastFrameDuration and the ability pulse's life
VENT_REACH = 3.0  # Ability_CrucibleVent's radius
VENT_DAMAGE = 8  # Ability_CrucibleVent's damage a stack
CAST_FPS, CAST_FRAMES = 14.0, 5  # CinderArcanistArtSet's Vent cast
IDLE_FPS, IDLE_FRAMES = 5.0, 8
SHARED_SLOTS, NUMBER_SLOTS = 24, 12  # the view's shared effect pool and its numbers


def splash_tint(stacks):
    """Color.Lerp(VentSplashCool, white, heat): as painted on a full burn, cooler, redder and fainter with fewer stacks."""
    heat = (min(max(stacks, 1), MAX_STACKS) - 1) / (MAX_STACKS - 1)
    return tuple(c + (1.0 - c) * heat for c in SPLASH_COOL)


def hash01(a, b, seed):
    """A fixed value in [0, 1) for a pair of integers, standing in for PixelNoise.Value."""
    h = (a * 374761393 + b * 668265263 + seed * 2246822519) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((h ^ (h >> 16)) & 0xFFFFFF) / float(0x1000000)


def ability_ring():
    """AbilityRingArt.Create, texel for texel: the ability's reach marked round the hero, a unit radius at scale 1."""
    width, height = 512, 256
    y, x = np.mgrid[0:height, 0:width].astype(np.float32)
    dx, dy = x + 0.5 - width * 0.5, y + 0.5 - height * 0.5
    length = np.sqrt(dx * dx + 4 * dy * dy)
    angle = np.arctan2(dy * 2, dx)
    gradient = np.sqrt(np.cos(angle) ** 2 + 4 * np.sin(angle) ** 2)
    distance = np.abs(length - 240) / gradient
    phase = np.mod(angle + np.pi / 8, np.pi / 4)
    gap = np.abs(phase - np.pi / 8) < 0.027
    core = np.where(gap, 0.0, np.clip(2.1 - distance, 0, 1))
    shadow = np.where(gap, 0.0, 0.36 * np.clip(3.5 - distance, 0, 1))
    marker = np.zeros_like(core)
    for i in range(4):
        a = i * np.pi / 2
        mx, my = math.cos(a) * 231, math.sin(a) * 115.5
        edge = np.abs(np.abs(dx - mx) + np.abs(dy - my) * 1.5 - 4.5)
        marker = np.maximum(marker, np.clip(1.1 - edge, 0, 1))
    core = np.maximum(core, marker)
    alpha = np.maximum(core, shadow)
    low, high = np.array([0.055, 0.08, 0.16]), np.array([0.63, 0.84, 1.0])
    rgba = np.concatenate([low + (high - low) * core[..., None], alpha[..., None]], -1)
    return rs.Sprite(rgba[::-1], (0.5, 0.5), 240.0, "Shared ability arc seal")


def load_art():
    art = rs.load_art()
    skills = rs.parse_strips(rs.SKILLS / "SkillEffectArtSet.asset")
    art["splash"] = rs.cut(skills["_ventSplash"], rs.SKILLS, "VFX Vent Splash ")
    art["vent_embers"] = rs.cut(skills["_ventEmbers"], rs.SKILLS, "VFX Vent Embers ")
    art["vent_cast"] = rs.cut(skills["_ventRing"], rs.SKILLS, "VFX Vent Cast ")
    art["reach"] = ability_ring()
    return art


# ----------------------------------------------------------------------------------------------------------------------
# What the view draws for one Vent.


def mirrored(sprite):
    """The sprite as SpriteRenderer.flipX draws it: mirrored about its pivot, not about its middle."""
    return rs.Sprite(sprite.rgba[:, ::-1], (1.0 - sprite.pivot[0], sprite.pivot[1]), sprite.ppu, sprite.name)


def splash_ring(view, art, at, t, stacks, painted, flip=False):
    """An eruption's floor ring t seconds after the Vent, as OnVented arms it and the StaffPulse branch runs it."""
    if painted:
        frames = art["splash"]
        life = len(frames) * SPLASH_FRAME
        if t < 0 or t >= life:
            return
        sprite = frames[min(int(t / SPLASH_FRAME), len(frames) - 1)]
        sprite = mirrored(sprite) if flip else sprite
        tint = splash_tint(stacks)
    else:
        life = rs.STAFF_PULSE
        if t < 0 or t >= life:
            return
        sprite, tint = art["ring_before"], rs.BURN_TINT
    alpha = tint[3] * rs.pulse_alpha(life, t)
    view.draw(sprite, at, rs.pulse_scale(VENT_SPLASH, t), (tint[0], tint[1], tint[2], alpha))


def column(view, art, at, t, stacks):
    frames = art["vent_column"]
    if 0 <= t < len(frames) * COLUMN_FRAME:
        view.draw(frames[int(t / COLUMN_FRAME)], at, COLUMN_SCALE + COLUMN_PER_STACK * stacks)


def embers(view, art, at, t, stacks, count):
    """The embers an eruption throws up: one cluster, a second mirrored the other way from three stacks."""
    frames = art["vent_embers"]
    if t < 0 or t >= EMBER_LIFE:
        return
    scale = COLUMN_SCALE + COLUMN_PER_STACK * stacks
    alpha = min(1.0, (EMBER_LIFE - t) / EMBER_FADE)
    for i in range(2 if stacks >= 3 else 1):
        side = 1.0 if i == 0 else -1.0
        drift = side * (0.1 + 0.15 * hash01(i, count, 57))
        origin = (at[0] + drift * t, at[1] + EMBER_LIFT * scale + (EMBER_RISE + EMBER_RISE_PER_STACK * stacks) * t)
        sprite = frames[min(int(t / EMBER_FRAME), len(frames) - 1)]
        view.draw(mirrored(sprite) if i == 1 else sprite, origin, EMBER_SCALE + EMBER_SCALE_PER_STACK * stacks,
                  (1, 1, 1, alpha))


def cast_ring(view, art, at, t):
    """The painted cast ring at Ilya's feet, grown from 0.6 of the Vent's reach to all of it and faded (AbilityPulse)."""
    frames = art["vent_cast"]
    if t < 0 or t >= CAST_LIFE:
        return
    progress = min(max(t / 0.14, 0.0), 1.0)
    radius = VENT_REACH * (0.6 + 0.4 * (1 - (1 - progress) ** 2))
    view.draw(frames[min(int(t / CAST_FRAME), len(frames) - 1)], at, radius,
              (1, 1, 1, 0.85 * min(max((CAST_LIFE - t) / 0.11, 0.0), 1.0)))


def reach_ring(view, art, at, time, vent_at):
    """AbilityRingView: the reach ring breathes while the Vent is ready and dims while it cools."""
    ready = vent_at is None or time < vent_at
    alpha = 0.75 - 0.1 * (0.5 + 0.5 * math.sin(time * 3.0)) if ready else 0.22
    view.draw(art["reach"], at, VENT_REACH, (1, 1, 1, alpha))


def ilya_frame(time, vent_at, facing):
    if vent_at is not None and 0 <= time - vent_at < CAST_FRAMES / CAST_FPS:
        return f"cast_{facing}_{min(int((time - vent_at) * CAST_FPS), CAST_FRAMES - 1)}"
    return f"idle_{facing}_{int(time * IDLE_FPS) % IDLE_FRAMES}"


def floor_distance(a, b):
    return math.hypot(a[0] - b[0], (a[1] - b[1]) / 0.5)


def vent_events(hero, enemies):
    """What one Vent does (AbilityController.Vent): every burning enemy within reach of the hero (every one, with no
    hero in the scene) spends its stacks at once, nearest first; then each erupts for eight a stack on itself and every
    enemy within the splash. Returns the eruptions (enemy index, stacks) and the hits (enemy index, damage, burning when
    struck) in the order they land."""
    origin = hero if hero is not None else (0.0, 0.0)
    vented = sorted((k for k, e in enumerate(enemies)
                     if e.burning and (hero is None or floor_distance(e.at, hero) <= VENT_REACH)),
                    key=lambda k: floor_distance(enemies[k].at, origin))
    spent = {k: enemies[k].burning for k in vented}
    eruptions, hits = [], []
    for k in vented:
        eruptions.append((k, spent[k]))
        damage = spent[k] * VENT_DAMAGE
        hits.append((k, damage, False))
        near = sorted((j for j, e in enumerate(enemies) if j != k and
                       floor_distance(e.at, enemies[k].at) <= VENT_SPLASH),
                      key=lambda j: floor_distance(enemies[j].at, enemies[k].at))
        for j in near:
            hits.append((j, damage, bool(enemies[j].burning) and j not in spent))
    return eruptions, hits


def vent_scene(art, floor, size, centre, hero, enemies, time, vent_at, painted, facing="north", reach=True):
    """The fight at time: enemies burn until the Vent at vent_at (None: never) spends them, then each one spent erupts.
    With no hero (None), only the eruptions: no Ilya, no cast ring and no reach ring."""
    view = rs.View(floor(size), centre)
    drawn = hero if hero is not None else (-99.0, -99.0)
    rs.draw_shadows(view, drawn, enemies)
    after = vent_at is not None and time >= vent_at
    t = time - vent_at if after else -1.0
    eruptions, hits = vent_events(hero, enemies) if vent_at is not None else ([], [])
    spent = {k for k, _ in eruptions}
    if reach and hero is not None:
        reach_ring(view, art, hero, time, vent_at)
    if after:
        # The floor order: the cast ring at the hero, then each eruption's ring, every other one mirrored.
        if hero is not None:
            cast_ring(view, art, hero, t)
        for n, (k, stacks) in enumerate(eruptions):
            splash_ring(view, art, enemies[k].at, t, stacks, painted, flip=n % 2 == 1)
    rs.draw_bodies(view, art, drawn, enemies, ilya_frame(time, vent_at, facing))
    burning = [rs.Enemy(e.kind, e.at, 0 if (after and k in spent) else e.burning, e.flip)
               for k, e in enumerate(enemies)]
    rs.draw_flames(view, art, burning, time)
    if after:
        for n, (k, stacks) in enumerate(eruptions):
            column(view, art, enemies[k].at, t, stacks)
        # The shared pool keeps the last sparks a crowd's Vent arms, the number pool its last twelve numbers.
        for k, damage, ember in hits[-SHARED_SLOTS:]:
            rs.spark(view, art, (enemies[k].at[0], enemies[k].body), t, ember)
        for n, (k, stacks) in enumerate(eruptions):
            embers(view, art, enemies[k].at, t, stacks, n)
        for n, (k, damage, _) in enumerate(hits[-NUMBER_SLOTS:]):
            rs.number(view, art, damage, (enemies[k].at[0], enemies[k].body), t, 0.3 * (hash01(n, k, 41) - 0.5))
    return view.image()


# ----------------------------------------------------------------------------------------------------------------------
# Sheets.


TIMES = (1 / 60, 0.05, 0.1, 0.15, 0.2, 0.25, 0.3)


def duel(art, floor, painted, t, stacks=3, size=(600, 380)):
    """Ilya beside a burning Grunt and a Cinder Mite, the moment t after her Vent; the Grunt erupts."""
    hero, centre = (-1.05, -0.2), (0.55, 0.35)
    enemies = [rs.Enemy("grunt", (1.0, 0.15), stacks), rs.Enemy("mite", (2.0, 0.6)), rs.Enemy("grunt", (0.3, 1.25))]
    return vent_scene(art, floor, size, centre, hero, enemies, 0.5 + t, 0.5, painted, facing="east", reach=False)


def before_after(art, floor, name):
    rows = []
    for painted in (False, True):
        row = []
        for t in TIMES:
            image = duel(art, floor, painted, t)
            rs.label(image, f"{'after' if painted else 'before'}  t={t * 1000:.0f} ms")
            row.append(image)
        rows.append(row)
    w, h = rows[0][0].size
    sheet = Image.new("RGB", (w * len(TIMES), h * 2), (10, 8, 12))
    for r, row in enumerate(rows):
        for c, image in enumerate(row):
            sheet.paste(image, (c * w, r * h))
    sheet.save(OUT / f"before-after-{name}.png")
    # The two most telling moments at 2x, nearest neighbour, so the texels show as the phone shows them.
    zoom = Image.new("RGB", (w * 2 * 2, h * 2 * 2))
    for r, row in enumerate(rows):
        for c, t in enumerate((0.05, 0.15)):
            zoom.paste(row[TIMES.index(t)].resize((w * 2, h * 2), Image.NEAREST), (c * w * 2, r * h * 2))
    zoom.save(OUT / f"before-after-{name}-x2.png")


def reference_ellipse(image, view, centre, radius, color=(90, 230, 255)):
    rs.reference_ellipse(image, view, centre, radius, color)


def stacks(art):
    """One stack beside a full burn, on the court and on lava, as the ring grows and once it has: the ring as wide
    either way (the splash's radius dashed), the full burn's hotter, its column taller and its embers thrown higher."""
    size = (460, 330)
    rows = []
    for floor, name in ((rs.court, "stone"), (rs.lava, "lava")):
        row = []
        for count in (1, MAX_STACKS):
            for t in (0.06, 0.15):
                view = rs.View(floor(size), (0.0, 0.3))
                enemy = rs.Enemy("grunt", (0.0, 0.0), count)
                image = vent_scene(art, floor, size, (0.0, 0.3), None, [enemy], 0.5 + t, 0.5, True)
                if t == 0.15:
                    reference_ellipse(image, view, (0.0, 0.0), VENT_SPLASH)
                rs.label(image, f"{name}: {count} stack{'s' if count > 1 else ''}, {t * 1000:.0f} ms")
                row.append(image)
        rows.append(row)
    w, h = size
    sheet = Image.new("RGB", (w * 4, h * 2))
    for r, row in enumerate(rows):
        for c, image in enumerate(row):
            sheet.paste(image, (c * w, r * h))
    sheet.save(OUT / "stacks.png")


def staff_and_vent(art):
    """A painted Staff hit (left, its violet ring at the shown part of its splash) beside a painted eruption (right, its
    ember ring at the Vent's whole 1.2-unit splash), at the same moments."""
    size = (820, 360)
    tiles = []
    for t in (0.03, 0.08, 0.14, 0.2):
        view = rs.View(rs.court(size), (0.3, 0.4))
        staffed, vented = rs.Enemy("grunt", (-1.55, 0.25)), rs.Enemy("grunt", (1.75, 0.2), 3)
        for e in (staffed, vented):
            rs.shadow(view, e.at, 0.78)
        rs.staff_ring(view, art, staffed.at, rs.ring_radius(rs.STAFF_SPLASH), t, True)
        splash_ring(view, art, vented.at, t, 3, True)
        rs.draw_bodies(view, art, (-9, -9), [staffed, vented], "idle_east_0")
        column(view, art, vented.at, t, 3)
        rs.staff_burst(view, art, (staffed.at[0], staffed.body), t)
        embers(view, art, vented.at, t, 3, 0)
        image = view.image()
        rs.label(image, f"Staff hit (left) beside a Vent eruption at 3 stacks (right), {t * 1000:.0f} ms")
        tiles.append(image)
    sheet = Image.new("RGB", (size[0] * 2, size[1] * 2))
    for k, image in enumerate(tiles):
        sheet.paste(image, ((k % 2) * size[0], (k // 2) * size[1]))
    sheet.save(OUT / "staff-and-vent.png")


def crowd_layout(seed=11):
    """Ilya amid a Foundry crowd a minute in: eleven burning enemies within the Vent's reach at one to five stacks, a
    few unlit beside them, and two burning beyond it."""
    rng = np.random.default_rng(seed)
    hero = (0.0, 0.0)
    enemies = []
    while len(enemies) < 16:
        x, y = rng.uniform(-3.4, 3.4), rng.uniform(-1.5, 1.6)
        d = floor_distance((x, y), hero)
        if d < 0.9 or d > 3.4:
            continue
        if any(math.hypot(x - e.at[0], (y - e.at[1]) * 1.6) < 0.66 for e in enemies):
            continue
        kind = "grunt" if rng.random() < 0.42 else "mite"
        enemies.append(rs.Enemy(kind, (x, y), 0, flip=x < 0))
    inside = [k for k, e in enumerate(enemies) if floor_distance(e.at, hero) <= VENT_REACH]
    outside = [k for k in range(len(enemies)) if k not in inside]
    for n, k in enumerate(inside[:11]):
        enemies[k].burning = 1 + (n * 3) % MAX_STACKS
    for k in outside[:2]:
        enemies[k].burning = 2
    return hero, enemies


def crowd(art):
    hero, enemies = crowd_layout()
    eruptions, hits = vent_events(hero, enemies)
    size, centre = (1080, 820), (0.0, 0.35)
    for painted in (False, True):
        image = vent_scene(art, rs.stone, size, centre, hero, enemies, 0.6 + 0.09, 0.6, painted)
        rs.label(image, f"{'after' if painted else 'before'}: a Crucible Vent 90 ms in, {len(eruptions)} eruptions, "
                        f"{len(hits)} hits")
        image.save(OUT / f"crowd-{'after' if painted else 'before'}.png")


def vent_gif(art):
    """The crowd at real timing, 50 frames a second (20 ms, the shortest delay browsers honour): the burning crowd,
    Ilya's Vent at 0.4 s, its eruptions and their embers, before and after side by side."""
    hero, enemies = crowd_layout()
    size, centre = (640, 560), (0.0, 0.3)
    frames = []
    for k in range(80):
        time = k * 0.02
        row = []
        for painted in (False, True):
            image = vent_scene(art, rs.stone, size, centre, hero, enemies, time, 0.4, painted)
            rs.label(image, "after" if painted else "before")
            row.append(image)
        sheet = Image.new("RGB", (size[0] * 2, size[1]))
        sheet.paste(row[0], (0, 0))
        sheet.paste(row[1], (size[0], 0))
        frames.append(sheet)
    # The palette from the frames the eruptions show in (0.44 to 0.68 s), so their colours hold.
    rs.save_gif(frames, OUT / "vent.gif", 20, [22, 25, 28, 31, 34], dither=False)


def painted_frames(art):
    """Every painted frame as cut, on a neutral grey and on lava, at its own size, with the time it shows."""
    rings = art["splash"]
    w, h = rings[0].size
    grey = (58, 54, 64)
    sheet = Image.new("RGB", (w * 3 + 16, (h + 8) * 4), grey)
    lava = Image.fromarray(rs.lava((w * 3 + 16, (h + 8) * 2)))
    sheet.paste(lava, (0, (h + 8) * 2))
    draw = ImageDraw.Draw(sheet)
    for i, ring in enumerate(rings):
        tile = Image.fromarray((ring.rgba * 255 + 0.5).astype(np.uint8), "RGBA")
        for band in (0, 2):
            x, y = (i % 3) * (w + 8), ((i // 3) + band) * (h + 8)
            base = sheet.crop((x, y, x + w, y + h)).convert("RGBA")
            base.alpha_composite(tile)
            sheet.paste(base.convert("RGB"), (x, y))
            ms = SPLASH_FRAME * 1000
            draw.text((x + 6, y + 4), f"splash {i}  {i * ms:.0f}-{(i + 1) * ms:.0f} ms", fill=(235, 225, 210))
    sheet.save(OUT / "painted-frames.png")


def scale_check(art, capture):
    """A real in-game frame of Ilya in a crowd at the phone's framing (top) over the composited Vent at the same
    scale (bottom), both 1:1, so the composites' scale can be judged against the game's own."""
    real = Image.open(capture).convert("RGB").crop((0, 760, 1080, 1480))
    hero, enemies = crowd_layout()
    ours = vent_scene(art, rs.stone, (1080, 720), (0.0, 0.35), hero, enemies, 0.6 + 0.09, 0.6, True)
    rs.label(real, "in game (Ilya in a crowd, the reach ring round her), 1:1")
    rs.label(ours, "composite: the painted Vent 90 ms in, 1:1")
    sheet = Image.new("RGB", (1080, 1440))
    sheet.paste(real, (0, 0))
    sheet.paste(ours, (0, 720))
    sheet.save(OUT / "scale-check.png")


def main():
    art = load_art()
    if "--capture" in ARGS:
        scale_check(art, ARGS[ARGS.index("--capture") + 1])
    only = ARGS[ARGS.index("--only") + 1].split(",") if "--only" in ARGS else None
    jobs = {"stone": lambda: before_after(art, rs.court, "stone"), "lava": lambda: before_after(art, rs.lava, "lava"),
            "stacks": lambda: stacks(art), "staff": lambda: staff_and_vent(art), "crowd": lambda: crowd(art),
            "gif": lambda: vent_gif(art), "frames": lambda: painted_frames(art)}
    for name, job in jobs.items():
        if only is None or name in only:
            job()
    print("VENT_REVIEW", OUT)


if __name__ == "__main__":
    main()
