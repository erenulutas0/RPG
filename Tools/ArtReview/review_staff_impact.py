"""Review images for the painted Staff impact (build_staff_impact_vfx.py), composited from the game's own sprites.

    python Tools/ArtReview/review_staff_impact.py --out DIR [--only stone,lava,area,vent,crowd,gif,frames]
                                                  [--capture IN_GAME_FRAME.png]

Draws what CombatEffectsView draws for a Staff hit at the phone's scale (the Samsung S23 shows 120 pixels a world
unit, 1080 across a 9-unit view), as the GPU does: bilinear sampling, straight alpha blended in sRGB (the project's
gamma colour space). Before is the procedural ring (StaffSplashArt, ported texel for texel); after is the painted ring
and burst read back from VFX_StaffBlast.png and FoundryStrikeArtSet.asset, exactly what Unity imports (before ASTC).
Both are timed and sized by the view's own rules: RingScale (the shown half of the splash, clamped to 0.75-1.5 of the
pixel ring), the growth from 24/38 of the radius over 0.14 s and the fade over the last 0.11 s. Ilya (her rendered
atlases), the painted Grunt and Cinder Mite, their contact shadows, the painted hit spark (ember on a burning enemy),
the painted burn flames and the painted numerals come from their own art sets. The floor is an in-game capture of the
Foundry court; the lava is a patch in the pit's sampled colours, as ReviewArcanistSprites.py paints it.

Writes: before-after-stone.png and before-after-lava.png (and x2 zooms), area-min-max.png, staff-hit.gif (real
timing, before and after side by side), crowd-before.png, crowd-after.png, crowd.gif, vent-and-staff.png and
painted-frames.png; with --capture, scale-check.png sets a real in-game frame of a crowd at the phone's framing over
the composited crowd.
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
OUT = Path(ARGS[ARGS.index("--out") + 1]) if "--out" in ARGS else ROOT / "TestResults/staff-impact-review"
OUT.mkdir(parents=True, exist_ok=True)

PHONE = 120.0  # S23 pixels per world unit
STRIKES = ROOT / "Assets/_Project/Art/Effects/FoundryStrikes"
SKILLS = ROOT / "Assets/_Project/Art/Effects/Skills"
FEEDBACK = ROOT / "Assets/_Project/Art/Effects/Feedback"
ARCANIST = ROOT / "Assets/_Project/Art/Characters/CinderArcanist"
GRUNT = ROOT / "Assets/_Project/Art/Characters/SlagBrute"
MITE = ROOT / "Assets/_Project/Art/Characters/CinderMite"
STONE_CAPTURE = ROOT / "ArtDirection/2026-10-23/foundry-rim-pipes-01/after/approach-0-0.png"

# CombatEffectsView's rules.
RING_FULL_RADIUS = 38  # EffectArt.RingFullRadius, pixel-art texels
PIXELS_PER_UNIT = 32.0  # PixelSpriteFactory.PixelsPerUnit
MIN_RING_SCALE, MAX_RING_SCALE = 0.75, 1.5
RING_SPLASH_FRACTION = 0.5  # the Gameplay scene's _ringSplashFraction
STAFF_SPLASH = 3.5  # Weapon_Staff.asset
STAFF_PULSE = 0.21  # the procedural ring's life
PAINTED_RING_FRAME = 0.048  # CombatEffectsView.PaintedStaffRingFrameDuration
PAINTED_BURST_FRAME = 0.04  # CombatEffectsView.PaintedStaffBurstFrameDuration
SPARK_FRAME = 0.06
BURN_TINT = (0.906, 0.447, 0.196, 1.0)
VENT_SPLASH = 1.2  # BurnRule.VentSplashRadius
NUMBER_RISE, NUMBER_DURATION, NUMBER_LIFT = 0.6, 0.7, 0.25


def ring_radius(splash, area=1.0):
    wanted = splash * area * RING_SPLASH_FRACTION * PIXELS_PER_UNIT
    return min(max(wanted / RING_FULL_RADIUS, MIN_RING_SCALE), MAX_RING_SCALE) * RING_FULL_RADIUS / PIXELS_PER_UNIT


def pulse_scale(radius, t):
    progress = min(max(t / 0.14, 0.0), 1.0)
    return radius * (24 / 38 + (1 - 24 / 38) * (1 - (1 - progress) ** 2))


def pulse_alpha(lifetime, t):
    return min(max((lifetime - t) / 0.11, 0.0), 1.0)


# ----------------------------------------------------------------------------------------------------------------------
# Sprites and drawing.


class Sprite:
    """Straight-alpha RGBA in [0, 1], image order, pinned at pivot (normalised, from the bottom-left as Unity's) at ppu
    texels a world unit."""

    def __init__(self, rgba, pivot, ppu, name=""):
        self.rgba = rgba.astype(np.float32) / 255.0 if rgba.dtype == np.uint8 else rgba.astype(np.float32)
        self.pivot = pivot
        self.ppu = ppu
        self.name = name

    @property
    def size(self):
        return self.rgba.shape[1], self.rgba.shape[0]


class View:
    def __init__(self, background, centre):
        self.canvas = background.astype(np.float32) / 255.0
        self.centre = centre  # world point at the middle of the canvas

    def to_screen(self, x, y):
        h, w = self.canvas.shape[:2]
        return w / 2 + (x - self.centre[0]) * PHONE, h / 2 - (y - self.centre[1]) * PHONE

    def draw(self, sprite, position, scale=1.0, tint=(1, 1, 1, 1), flip=False):
        sx = scale if isinstance(scale, (int, float)) else scale[0]
        sy = scale if isinstance(scale, (int, float)) else scale[1]
        tw, th = sprite.size
        world_w, world_h = tw / sprite.ppu * sx, th / sprite.ppu * sy
        left = position[0] - sprite.pivot[0] * world_w
        bottom = position[1] - sprite.pivot[1] * world_h
        x0, y1 = self.to_screen(left, bottom)
        x1, y0 = self.to_screen(left + world_w, bottom + world_h)
        h, w = self.canvas.shape[:2]
        ix0, ix1 = max(0, int(math.floor(x0))), min(w, int(math.ceil(x1)))
        iy0, iy1 = max(0, int(math.floor(y0))), min(h, int(math.ceil(y1)))
        if ix0 >= ix1 or iy0 >= iy1:
            return
        px = np.arange(ix0, ix1) + 0.5
        py = np.arange(iy0, iy1) + 0.5
        u = (px - x0) / (x1 - x0)
        v = (py - y0) / (y1 - y0)  # 0 at the top
        if flip:
            u = 1.0 - u
        inside_u = (u >= 0) & (u <= 1)
        inside_v = (v >= 0) & (v <= 1)
        tx = u * tw - 0.5
        ty = v * th - 0.5
        sample = bilinear(sprite.rgba, tx, ty)
        sample = sample * np.asarray(tint, np.float32)
        a = sample[..., 3:] * inside_v[:, None, None] * inside_u[None, :, None]
        region = self.canvas[iy0:iy1, ix0:ix1, :3]
        self.canvas[iy0:iy1, ix0:ix1, :3] = sample[..., :3] * a + region * (1.0 - a)

    def image(self):
        return Image.fromarray((np.clip(self.canvas[..., :3], 0, 1) * 255 + 0.5).astype(np.uint8), "RGB")


def bilinear(texture, tx, ty):
    """GPU bilinear sampling, clamped at the edges, at texel coordinates tx (columns) and ty (rows from the top)."""
    h, w = texture.shape[:2]
    x0 = np.floor(tx).astype(int)
    y0 = np.floor(ty).astype(int)
    fx = (tx - x0).astype(np.float32)
    fy = (ty - y0).astype(np.float32)
    xa, xb = np.clip(x0, 0, w - 1), np.clip(x0 + 1, 0, w - 1)
    ya, yb = np.clip(y0, 0, h - 1), np.clip(y0 + 1, 0, h - 1)
    top = texture[ya][:, xa] * (1 - fx)[None, :, None] + texture[ya][:, xb] * fx[None, :, None]
    low = texture[yb][:, xa] * (1 - fx)[None, :, None] + texture[yb][:, xb] * fx[None, :, None]
    return top * (1 - fy)[:, None, None] + low * fy[:, None, None]


def shadow(view, at, width):
    """ContactShadowView's soft ellipse under an actor, on the 2:1 floor."""
    rx, ry = width * 0.5, width * 0.25
    size = 64
    yy, xx = np.mgrid[0:size, 0:size]
    d = np.hypot((xx + 0.5 - size / 2) / (size / 2), (yy + 0.5 - size / 2) / (size / 2))
    alpha = np.clip(1.0 - d, 0, 1) ** 0.8 * (110 / 255)
    rgba = np.zeros((size, size, 4), np.float32)
    rgba[..., 0], rgba[..., 1], rgba[..., 2] = 8 / 255, 6 / 255, 12 / 255
    rgba[..., 3] = alpha
    view.draw(Sprite(rgba, (0.5, 0.5), size / (2 * rx)), at, (1.0, ry / rx))


# ----------------------------------------------------------------------------------------------------------------------
# The art.


def procedural_ring():
    """StaffSplashArt.Create, texel for texel."""
    width, height, radius = 512, 256, 240.0
    y, x = np.mgrid[0:height, 0:width].astype(np.float32)
    dx = x + 0.5 - width * 0.5
    dy = (y + 0.5 - height * 0.5) * 2
    length = np.sqrt(dx * dx + dy * dy)
    angle = np.arctan2(dy, dx)
    phase = np.mod(angle + np.pi / 4, np.pi / 2) / (np.pi / 2)
    taper = np.clip((phase - 0.10) / 0.12, 0, 1) * np.clip((0.90 - phase) / 0.25, 0, 1)
    gradient = np.sqrt(np.cos(angle) ** 2 + 4 * np.sin(angle) ** 2)
    distance = np.abs(length - radius) / gradient
    core = np.clip(2.3 * taper - distance, 0, 1)
    glow = 0.28 * np.clip((7 * taper - distance) / 5, 0, 1)
    alpha = np.maximum(core, glow)
    low, high = np.array([0.44, 0.23, 0.9]), np.array([0.91, 0.79, 1.0])
    color = low + (high - low) * core[..., None]
    rgba = np.concatenate([color, alpha[..., None]], -1)
    # Unity's texture rows run bottom-up; the formula is symmetric top to bottom except for the arcs' swirl.
    return Sprite(rgba[::-1], (0.5, 0.5), 240.0, "Shared Staff impact arcs")


def parse_strips(asset_path):
    """Every strip of an art set asset: name -> dict(guid, sheet size, tpu, pivot, cells)."""
    text = asset_path.read_text(encoding="utf-8")
    strips = {}
    for match in re.finditer(r"(?m)^  (_\w+):\n((?:    .*\n|  - .*\n)+)", text):
        name, body = match.group(1), match.group(2)
        sheets = re.findall(r"Sheet: \{fileID: 2800000, guid: (\w+)", body)
        if not sheets:
            continue
        # A list of strips (the flames) or one.
        blocks = re.split(r"(?m)^  - (?=Sheet:)", body) if body.startswith("  - Sheet") else [body]
        parsed = []
        for block in blocks:
            if "Sheet:" not in block:
                continue
            guid = re.search(r"guid: (\w+)", block).group(1)
            tpu = float(re.search(r"TexelsPerUnit: ([\d.]+)", block).group(1))
            px, py = (float(v) for v in re.search(r"Pivot: \{x: ([-\d.e]+), y: ([-\d.e]+)\}", block).groups())
            sw = int(re.search(r"SheetWidth: (\d+)", block).group(1))
            sh = int(re.search(r"SheetHeight: (\d+)", block).group(1))
            cells = [tuple(int(v) for v in c) for c in
                     re.findall(r"- X: (\d+)\n\s+Y: (\d+)\n\s+Width: (\d+)\n\s+Height: (\d+)", block)]
            parsed.append(dict(guid=guid, tpu=tpu, pivot=(px, py), size=(sw, sh), cells=cells))
        strips[name] = parsed if len(parsed) > 1 or body.startswith("  - Sheet") else parsed[0]
    return strips


def sheet_by_guid(folder, guid):
    for meta in folder.glob("*.png.meta"):
        if f"guid: {guid}" in meta.read_text(encoding="utf-8"):
            return np.asarray(Image.open(str(meta)[:-5]).convert("RGBA"))
    raise FileNotFoundError(guid)


def cut(strip, folder, prefix):
    sheet = sheet_by_guid(folder, strip["guid"])
    h = sheet.shape[0]
    sprites = []
    for i, (x, y, w, hh) in enumerate(strip["cells"]):
        top = h - y - hh
        sprites.append(Sprite(sheet[top:top + hh, x:x + w], strip["pivot"], strip["tpu"], f"{prefix}{i}"))
    return sprites


def tga(path, pivot, ppu=128.0):
    return Sprite(np.asarray(Image.open(path).convert("RGBA")), pivot, ppu, path.stem)


def load_art():
    art = {}
    art["ring_before"] = procedural_ring()
    strike = parse_strips(STRIKES / "FoundryStrikeArtSet.asset")
    art["ring"] = cut(strike["_staffRing"], STRIKES, "VFX Staff Ring ")
    art["burst"] = cut(strike["_staffBurst"], STRIKES, "VFX Staff Burst ")
    art["spark"] = [tga(STRIKES / f"VFX_spark_{i}.tga", (0.5, 0.5)) for i in range(4)]
    skills = parse_strips(SKILLS / "SkillEffectArtSet.asset")
    art["flames"] = [cut(s, SKILLS, f"VFX Flame {k + 1} ") for k, s in enumerate(skills["_flames"])]
    art["vent_column"] = cut(skills["_ventColumn"], SKILLS, "VFX Vent Column ")
    feedback = parse_strips(FEEDBACK / "CombatFeedbackArtSet.asset")
    art["digits"] = cut(feedback["_digits"], FEEDBACK, "VFX Digit ")
    art["digit_stride"] = 24 / 128.0
    report = json.loads((ROOT / "ArtDirection/2026-10-27/arcanist-blender-01/frames.json").read_text())
    pages = [np.asarray(Image.open(ARCANIST / a["file"]).convert("RGBA")) for a in report["atlases"]]
    ilya = {}
    for f in report["frames"]:
        page = pages[f["atlas"]]
        x, y, w, h = f["rect"]
        top = page.shape[0] - y - h
        ilya[f["name"]] = Sprite(page[top:top + h, x:x + w], tuple(f["pivot"]), report["pixels_per_unit"], f["name"])
    art["ilya"] = ilya
    art["grunt"] = tga(GRUNT / "CHR_Grunt_front-idle.tga", (0.5, 0.1))
    art["mite"] = tga(MITE / "CHR_Mite_front_idle.tga", (0.4810606, 0.035714287))
    return art


# ----------------------------------------------------------------------------------------------------------------------
# Floors.


_COURT = []


def stone(size, corner=None):
    """The Foundry court from an in-game capture at the phone's scale (its cobbles and a gold inlay, clear of the actors
    it caught): the part of it as large as asked for whose top-left corner is corner, or else its middle."""
    if not _COURT:
        _COURT.append(Image.open(STONE_CAPTURE).convert("RGB").crop((0, 40, 1080, 900)))
    court = _COURT[0]
    w, h = min(size[0], court.width), min(size[1], court.height)
    left, top = corner if corner is not None else ((court.width - w) // 2, (court.height - h) // 2)
    left, top = min(left, court.width - w), min(top, court.height - h)
    out = Image.new("RGB", size)
    out.paste(court.crop((left, top, left + w, top + h)), ((size[0] - w) // 2, (size[1] - h) // 2))
    return np.asarray(out)


def court(size):
    """The court clear of its gold inlay, for the close views."""
    return stone(size, (20, 300))


def save_gif(frames, path, milliseconds, keys, dither=True):
    """One palette for every frame, made from the key frames (where the effects show) and the first, so the effects
    keep their colour and the GIF never flickers or grows a palette a frame; dithered unless the file would grow too
    large (the crowd's flames change every frame)."""
    picked = [frames[0]] + [frames[k] for k in keys]
    sample = Image.new("RGB", (frames[0].width, frames[0].height * len(picked)))
    for k, frame in enumerate(picked):
        sample.paste(frame, (0, frames[0].height * k))
    palette = sample.quantize(255, method=Image.MEDIANCUT)
    indexed = [f.quantize(palette=palette, dither=Image.FLOYDSTEINBERG if dither else Image.NONE) for f in frames]
    indexed[0].save(path, save_all=True, append_images=indexed[1:], duration=milliseconds, loop=0, optimize=True)


def lava(size, seed=3):
    """Molten orange in the pit's sampled colours (pit-spill.png: 201,37,1 to 253,52,2, crests 255,167,15)."""
    rng = np.random.default_rng(seed)
    w, h = size
    field = np.zeros((h, w))
    for scale, weight in [(48, 0.55), (20, 0.3), (8, 0.15)]:
        coarse = rng.random((h // scale + 2, w // scale + 2))
        field += weight * np.asarray(Image.fromarray((coarse * 255).astype(np.uint8)).resize(
            (w + 2 * scale, h + 2 * scale), Image.BICUBIC))[scale:scale + h, scale:scale + w] / 255.0
    low, mid, high = np.array([170, 30, 2]), np.array([235, 62, 4]), np.array([255, 160, 24])
    t = np.clip((field - 0.25) / 0.5, 0, 1)[..., None]
    rgb = np.where(t < 0.6, low + (mid - low) * (t / 0.6), mid + (high - mid) * ((t - 0.6) / 0.4))
    return rgb.astype(np.uint8)


# ----------------------------------------------------------------------------------------------------------------------
# A scene: actors on the floor, and the hits that land on them at given times.

GRUNT_BODY = 0.5625  # the painted Grunt's sprite centre over its feet: where its sparks and strikes land
MITE_BODY = 0.2457


class Enemy:
    def __init__(self, kind, at, burning=0, flip=False):
        self.kind, self.at, self.burning, self.flip = kind, at, burning, flip

    @property
    def body(self):
        return self.at[1] + (GRUNT_BODY if self.kind == "grunt" else MITE_BODY)


def draw_shadows(view, hero, enemies):
    """Every actor's contact shadow, on the floor under everything else."""
    shadow(view, hero, 0.78)
    for e in enemies:
        shadow(view, e.at, 0.78 if e.kind == "grunt" else 0.48)


def draw_bodies(view, art, hero, enemies, hero_frame):
    actors = [(hero[1], "hero", None)] + [(e.at[1], "enemy", e) for e in enemies]
    for _, kind, e in sorted(actors, key=lambda a: -a[0]):
        if kind == "hero":
            view.draw(art["ilya"][hero_frame], hero)
        else:
            sprite = art["grunt"] if e.kind == "grunt" else art["mite"]
            view.draw(sprite, e.at, flip=e.flip)


def draw_flames(view, art, enemies, time):
    for k, e in enumerate(enemies):
        if not e.burning:
            continue
        frames = art["flames"][e.burning - 1]
        frame = frames[int(time / 0.07 + k * 3) % len(frames)]
        scale = 1.0 if e.kind == "grunt" else 0.56
        view.draw(frame, e.at, scale)


def staff_ring(view, art, centre, radius, t, painted, tint=(1, 1, 1, 1)):
    if painted:
        frames = art["ring"]
        life = len(frames) * PAINTED_RING_FRAME
        if t < 0 or t >= life:
            return
        sprite = frames[min(int(t / PAINTED_RING_FRAME), len(frames) - 1)]
    else:
        life = STAFF_PULSE
        if t < 0 or t >= life:
            return
        sprite = art["ring_before"]
    scale = pulse_scale(radius, t)
    a = pulse_alpha(life, t)
    view.draw(sprite, centre, scale, (tint[0], tint[1], tint[2], tint[3] * a))


def staff_burst(view, art, anchor, t):
    frames = art["burst"]
    if t < 0 or t >= len(frames) * PAINTED_BURST_FRAME:
        return
    view.draw(frames[int(t / PAINTED_BURST_FRAME)], anchor)


def spark(view, art, anchor, t, burning):
    if t < 0 or t >= 4 * SPARK_FRAME:
        return
    view.draw(art["spark"][int(t / SPARK_FRAME)], anchor, tint=BURN_TINT if burning else (1, 1, 1, 1))


def number(view, art, value, anchor, t, jitter=0.0):
    if t < 0 or t >= NUMBER_DURATION:
        return
    u = t / NUMBER_DURATION
    rise = 1 - (1 - u) ** 2
    alpha = 1.0 if u < 0.6 else 1 - (u - 0.6) / 0.4
    text = str(value)
    stride = art["digit_stride"]
    glyph = art["digits"][0].size[0] / 128.0
    width = (len(text) - 1) * stride + glyph
    x = anchor[0] + jitter - width / 2
    y = anchor[1] + NUMBER_LIFT + NUMBER_RISE * rise
    for i, ch in enumerate(text):
        view.draw(art["digits"][int(ch)], (x + i * stride, y), tint=(1, 1, 1, alpha))


def hero_strike_frame(t, facing="east"):
    """Ilya's strike clip from the hit (RenderedCharacterAnimator.OnAttacked), five frames at 50 ms, then idle."""
    if 0 <= t < 0.25:
        return f"strike_{facing}_{int(t / 0.05)}"
    return f"idle_{facing}_0"


# ----------------------------------------------------------------------------------------------------------------------
# Sheets.


def label(image, text, at=(8, 6)):
    draw = ImageDraw.Draw(image)
    x, y = at
    draw.rectangle((x - 4, y - 2, x + 7 * len(text) + 4, y + 13), fill=(14, 12, 18))
    draw.text((x, y), text, fill=(236, 228, 214))
    return image


def duel(art, floor, painted, t, area=1.0, size=(600, 380), hero=(-1.0, -0.15), target=(1.25, 0.1), others=()):
    """Ilya casting toward a Grunt, the moment t after the hit."""
    centre = (0.55, 0.35)
    view = View(floor(size), centre)
    enemies = [Enemy("grunt", target)] + list(others)
    radius = ring_radius(STAFF_SPLASH, area)
    draw_shadows(view, hero, enemies)
    staff_ring(view, art, target, radius, t, painted)
    draw_bodies(view, art, hero, enemies, hero_strike_frame(t))
    draw_flames(view, art, enemies, t)
    for e in enemies:
        spark(view, art, (e.at[0], e.body), t, e.burning)
    if painted:
        staff_burst(view, art, (target[0], target[1] + GRUNT_BODY), t)
    for k, e in enumerate(enemies):
        number(view, art, 11 if k == 0 else 8, (e.at[0], e.body), t, 0.1 * (k % 3 - 1))
    return view.image()


TIMES = (1 / 60, 0.05, 0.1, 0.15, 0.2, 0.23)


def before_after(art, floor, name):
    columns = []
    for painted in (False, True):
        row = []
        for t in TIMES:
            image = duel(art, floor, painted, t, others=(Enemy("mite", (2.2, 0.75)), Enemy("grunt", (0.35, 1.2))))
            label(image, f"{'after' if painted else 'before'}  t={t * 1000:.0f} ms")
            row.append(image)
        columns.append(row)
    w, h = columns[0][0].size
    sheet = Image.new("RGB", (w * len(TIMES), h * 2), (10, 8, 12))
    for r, row in enumerate(columns):
        for c, image in enumerate(row):
            sheet.paste(image, (c * w, r * h))
    sheet.save(OUT / f"before-after-{name}.png")
    # The two most telling moments at 2x, nearest neighbour, so the texels show as the phone shows them.
    zoom = Image.new("RGB", (w * 2 * 2, h * 2 * 2))
    for r, row in enumerate(columns):
        for c, t in enumerate((0.05, 0.15)):
            image = row[TIMES.index(t)]
            zoom.paste(image.resize((w * 2, h * 2), Image.NEAREST), (c * w * 2, r * h * 2))
    zoom.save(OUT / f"before-after-{name}-x2.png")


def reference_ellipse(image, view, centre, radius, color=(90, 230, 255)):
    """A thin dashed ellipse at exactly radius on the floor: where the ring's contour must lie."""
    draw = ImageDraw.Draw(image)
    cx, cy = view.to_screen(*centre)
    rx, ry = radius * PHONE, radius * PHONE * 0.5
    for k in range(0, 360, 6):
        a0, a1 = math.radians(k), math.radians(k + 3)
        draw.line((cx + rx * math.cos(a0), cy - ry * math.sin(a0), cx + rx * math.cos(a1), cy - ry * math.sin(a1)),
                  fill=color, width=1)


def area_extremes(art):
    """The ring at the smallest and largest scale RingScale allows (0.75 and 1.5 of the pixel ring, 0.89 and 1.78
    units), and the Staff's own at Area 1 (1.75 units), before and after, with the radius each must reach dashed."""
    cases = [("MinRingScale 0.75: r 0.89", MIN_RING_SCALE * RING_FULL_RADIUS / PIXELS_PER_UNIT),
             ("Staff at Area 1: r 1.75", ring_radius(STAFF_SPLASH, 1.0)),
             ("MaxRingScale 1.5 (Area >= 1.02): r 1.78", ring_radius(STAFF_SPLASH, 2.0))]
    size = (520, 300)
    rows = []
    for painted in (False, True):
        for t in (0.15, 0.06):
            row = []
            for title, radius in cases:
                view = View(court(size), (0, 0.2))
                enemies = [Enemy("grunt", (0, 0))]
                shadow(view, (0, 0), 0.78)
                staff_ring(view, art, (0, 0), radius, t, painted)
                draw_bodies(view, art, (-9, -9), enemies, "idle_east_0")
                if painted:
                    staff_burst(view, art, (0, GRUNT_BODY), t)
                image = view.image()
                if t == 0.15:
                    reference_ellipse(image, view, (0, 0), radius)
                label(image, f"{'after' if painted else 'before'} {t * 1000:.0f} ms  {title}")
                row.append(image)
            rows.append(row)
    w, h = size
    sheet = Image.new("RGB", (w * len(cases), h * len(rows)), (10, 8, 12))
    for r, row in enumerate(rows):
        for c, image in enumerate(row):
            sheet.paste(image, (c * w, r * h))
    sheet.save(OUT / "area-min-max.png")


def crowd_layout(seed=5):
    """A horde of 34 round Ilya, most of the near ones burning, as a Vent-and-Staff fight looks a minute in."""
    rng = np.random.default_rng(seed)
    enemies = []
    while len(enemies) < 34:
        x, y = rng.uniform(-4.2, 4.2), rng.uniform(-3.4, 4.6)
        if math.hypot(x, (y + 1.2) * 1.4) < 1.3:
            continue
        if any(math.hypot(x - e.at[0], (y - e.at[1]) * 1.6) < 0.62 for e in enemies):
            continue
        kind = "grunt" if rng.random() < 0.38 else "mite"
        burning = int(rng.integers(1, 6)) if rng.random() < 0.6 else 0
        enemies.append(Enemy(kind, (x, y), burning, flip=x < 0))
    return enemies


def crowd_frame(art, enemies, hits, time, painted, size, centre, hero=(0.0, -1.2)):
    """The crowd at time: hits is a list of (time, target index, area) Staff hits; every enemy within the splash of a
    hit sparks and shows its number, burning ones in ember."""
    view = View(stone(size), centre)
    draw_shadows(view, hero, enemies)
    for at, target, area in hits:
        e = enemies[target]
        staff_ring(view, art, e.at, ring_radius(STAFF_SPLASH, area), time - at, painted)
    last = max([at for at, _, _ in hits if at <= time], default=-9)
    draw_bodies(view, art, hero, enemies, hero_strike_frame(time - last, "north"))
    draw_flames(view, art, enemies, time)
    for at, target, area in hits:
        centre_enemy = enemies[target]
        reach = STAFF_SPLASH * area
        for k, e in enumerate(enemies):
            if math.hypot(e.at[0] - centre_enemy.at[0], e.at[1] - centre_enemy.at[1]) <= reach:
                spark(view, art, (e.at[0], e.body), time - at, e.burning)
    # Burn ticks, every half second on each burning enemy, staggered: ember sparks.
    for k, e in enumerate(enemies):
        if not e.burning:
            continue
        tick = (time + 0.137 * k) % 0.5
        spark(view, art, (e.at[0], e.body), tick, True)
    for at, target, area in hits:
        if painted:
            staff_burst(view, art, (enemies[target].at[0], enemies[target].body), time - at)
    for at, target, area in hits:
        centre_enemy = enemies[target]
        reach = STAFF_SPLASH * area
        shown = 0
        for k, e in enumerate(enemies):
            if math.hypot(e.at[0] - centre_enemy.at[0], e.at[1] - centre_enemy.at[1]) <= reach and shown < 6:
                number(view, art, 11 if e is centre_enemy else 8, (e.at[0], e.body), time - at, 0.1 * (k % 3 - 1))
                shown += 1
    return view.image()


def nearest(enemies, point):
    return min(range(len(enemies)), key=lambda k: math.hypot(enemies[k].at[0] - point[0], enemies[k].at[1] - point[1]))


def crowd(art):
    enemies = crowd_layout()
    # Two Staffs a little out of step, both widened by Area, each on the enemy nearest Ilya's aim.
    a, b = nearest(enemies, (-1.4, 0.4)), nearest(enemies, (1.6, 1.4))
    hits = [(0.0, a, 1.5), (0.11, b, 1.5)]
    size, centre = (1080, 860), (0.0, 0.9)
    for painted in (False, True):
        image = crowd_frame(art, enemies, hits, 0.16, painted, size, centre)
        label(image, f"{'after' if painted else 'before'}: two Staff hits 110 ms apart, 34 enemies, 20 burning")
        image.save(OUT / f"crowd-{'after' if painted else 'before'}.png")
    # In motion at real timing: hits every 0.55 s (two Staffs on the 1.1 s cadence, half a cooldown out of step).
    frames = []
    aims = ((-1.4, 0.4), (1.6, 1.4), (0.6, 2.4), (-2.2, 1.6), (1.2, -0.2), (-0.4, 3.0))
    targets = [nearest(enemies, p) for p in aims]
    hits = [(0.05 + 0.55 * n, targets[n], 1.5) for n in range(6)]
    gif_size, gif_centre = (600, 560), (0.0, 0.9)
    for k in range(0, 56):
        t = k * 0.04
        before = crowd_frame(art, enemies, hits, t, False, gif_size, gif_centre)
        after = crowd_frame(art, enemies, hits, t, True, gif_size, gif_centre)
        sheet = Image.new("RGB", (gif_size[0] * 2, gif_size[1]))
        sheet.paste(before, (0, 0))
        sheet.paste(after, (gif_size[0], 0))
        label(sheet, "before")
        label(sheet, "after", (gif_size[0] + 8, 6))
        frames.append(sheet)
    save_gif(frames, OUT / "crowd.gif", 40, [2, 4, 15, 17, 29, 31, 43], dither=False)


def staff_hit_gif(art):
    """Ilya casting at a Grunt twice, on the Staff's 1.1 s cadence, before and after side by side, at 50 frames a second
    (20 ms, the shortest delay browsers honour)."""
    frames = []
    others = (Enemy("mite", (2.2, 0.75)), Enemy("grunt", (0.35, 1.2)))
    for k in range(0, 120):
        t = k * 0.02
        local = (t - 0.1) % 1.1 if t >= 0.1 else -1
        row = []
        for painted in (False, True):
            image = duel(art, court, painted, local, others=others)
            label(image, "after" if painted else "before")
            row.append(image)
        sheet = Image.new("RGB", (row[0].width * 2, row[0].height))
        sheet.paste(row[0], (0, 0))
        sheet.paste(row[1], (row[0].width, 0))
        frames.append(sheet)
    save_gif(frames, OUT / "staff-hit.gif", 20, [6, 8, 10, 12, 14])


def vent_and_staff(art):
    """A Crucible Vent eruption (the procedural ring tinted ember, at the Vent's 1.2-unit splash, and the painted
    column) beside a painted Staff hit, at the same moments: the Vent stays ember, the Staff violet."""
    size = (760, 360)
    tiles = []
    for t in (0.05, 0.12):
        view = View(court(size), (0.4, 0.4))
        vent, staffed = Enemy("grunt", (-1.4, 0.2)), Enemy("grunt", (1.9, 0.3))
        for e in (vent, staffed):
            shadow(view, e.at, 0.78)
        staff_ring(view, art, vent.at, VENT_SPLASH, t, False, BURN_TINT)
        staff_ring(view, art, staffed.at, ring_radius(STAFF_SPLASH), t, True)
        draw_bodies(view, art, (-9, -9), [vent, staffed], "idle_east_0")
        column = art["vent_column"][min(int(t / 0.05), len(art["vent_column"]) - 1)]
        view.draw(column, vent.at, 0.62 + 0.1 * 3)
        staff_burst(view, art, (staffed.at[0], staffed.body), t)
        image = view.image()
        label(image, f"Vent eruption (left) beside a Staff hit (right), {t * 1000:.0f} ms")
        tiles.append(image)
    sheet = Image.new("RGB", (size[0], size[1] * len(tiles)))
    for k, image in enumerate(tiles):
        sheet.paste(image, (0, k * size[1]))
    sheet.save(OUT / "vent-and-staff.png")


def scale_check(art, capture):
    """A real in-game frame of Ilya in a crowd at the phone's framing (left) beside the composited crowd (right), both
    at 1:1, so the composites' scale and density can be judged against the game's own."""
    real = Image.open(capture).convert("RGB")
    real = real.crop((0, 760, 1080, 1480))
    enemies = crowd_layout()
    a = nearest(enemies, (1.6, 1.4))
    ours = crowd_frame(art, enemies, [(0.0, a, 1.5)], 0.05, True, (1080, 720), (0.2, 0.9))
    label(real, "in game (current procedural ring), 1:1")
    label(ours, "composite, painted ring at 50 ms, 1:1")
    sheet = Image.new("RGB", (1080, 1440))
    sheet.paste(real, (0, 0))
    sheet.paste(ours, (0, 720))
    sheet.save(OUT / "scale-check.png")


def painted_frames(art):
    """Every painted frame as cut, on a neutral grey: the ring's at their own size, the burst's at twice theirs."""
    rings, bursts = art["ring"], art["burst"]
    w, h = rings[0].size
    bw = max(b.size[0] for b in bursts)
    bh = max(b.size[1] for b in bursts)
    sheet = Image.new("RGBA", (w * 3 + 16, h * 2 + 8 + bh * 2 + 40), (58, 54, 64, 255))
    draw = ImageDraw.Draw(sheet)
    for i, ring in enumerate(rings):
        x, y = (i % 3) * (w + 8), (i // 3) * (h + 8)
        sheet.alpha_composite(Image.fromarray((ring.rgba * 255 + 0.5).astype(np.uint8), "RGBA"), (x, y))
        ms = PAINTED_RING_FRAME * 1000
        draw.text((x + 6, y + 4), f"ring {i}  {i * ms:.0f}-{(i + 1) * ms:.0f} ms", fill=(235, 225, 210, 255))
    top = 2 * (h + 8) + 20
    for i, burst in enumerate(bursts):
        image = Image.fromarray((burst.rgba * 255 + 0.5).astype(np.uint8), "RGBA")
        image = image.resize((image.width * 2, image.height * 2), Image.BILINEAR)
        x = i * (bw * 2 + 12)
        sheet.alpha_composite(image, (x, top))
        ms = PAINTED_BURST_FRAME * 1000
        draw.text((x + 6, top - 14), f"burst {i}  {i * ms:.0f}-{(i + 1) * ms:.0f} ms (2x)", fill=(235, 225, 210, 255))
    sheet.convert("RGB").save(OUT / "painted-frames.png")


def main():
    art = load_art()
    if "--capture" in ARGS:
        scale_check(art, ARGS[ARGS.index("--capture") + 1])
    only = ARGS[ARGS.index("--only") + 1].split(",") if "--only" in ARGS else None
    jobs = {"stone": lambda: before_after(art, court, "stone"), "lava": lambda: before_after(art, lava, "lava"),
            "area": lambda: area_extremes(art), "vent": lambda: vent_and_staff(art), "crowd": lambda: crowd(art),
            "gif": lambda: staff_hit_gif(art), "frames": lambda: painted_frames(art)}
    for name, job in jobs.items():
        if only is None or name in only:
            job()
    print("STAFF_REVIEW", OUT)


if __name__ == "__main__":
    main()
