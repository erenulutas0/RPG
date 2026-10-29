"""Review sheets for the painted HUD and health bars (build_hud_art.py).

    python Tools/ArtReview/review_hud_art.py --out DIR [--crowd CAPTURE.png] [--warden CAPTURE.png]

Reads the painted sprites back from Art/UI/Hud (the sheets, their metas' rects and 9-slice borders, and the sets' bar
layout) and lays them out as HudArtView and EnemyLookView do: the scene's rects on the 1080-wide canvas (a phone at
1080x2340 shows it at one canvas unit a pixel, its safe area the whole screen), Sliced images cut at their borders, a
Filled bar cropped at its fill amount, and the enemy bars at the camera's 120 pixels a world unit (9 units across).

Writes, for real in-game frames at the phone's framing (crowd and Warden fight captures):
  - phone-<name>-before-after.png: the capture, and the same frame with the painted HUD and bars laid over it. The
    old HUD's labels, coin and badges are lifted from the capture (they sit on the old panels' navy, which the painted
    face keeps) and the old enemy bars are found in it by their fill's red and replaced, at the same place and fill.
  - bars-on-stone-and-lava.png: every bar at full, half and low (the two that drain with the chunk a hit just took),
    over the foundry's stone and over lava, at phone size and enlarged.
  - enemy-bars-crowd.png: the enemy bars, full and compact, over a crowd from the capture, at phone size and enlarged.
  - sprites-and-borders.png: every painted sprite, enlarged, with its 9-slice borders marked.
"""
import math
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[2]
HUD = ROOT / "Assets/_Project/Art/UI/Hud"
UI = ROOT / "Assets/_Project/Art/UI"
ARGS = sys.argv[1:]


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


OUT = Path(arg("--out", ROOT / "TestResults/hud-art-review"))
OUT.mkdir(parents=True, exist_ok=True)
CAPTURES = {k: arg("--" + k) for k in ("crowd", "warden", "arcanist")}
# The ability icon each capture's hero shows in the well: the Vanguard's Forge Burst, the Arcanist's Crucible Vent.
ICONS = {"crowd": "UI_Icon_ForgeBurst_v1", "warden": "UI_Icon_ForgeBurst_v1", "arcanist": "UI_Icon_CrucibleVent_v1"}
PHONE = 120.0  # pixels per world unit at the camera's 9 units across 1080
W, H = 1080, 2340
FONT = "C:/Windows/Fonts/arial.ttf"


# ----------------------------------------------------------------------------------------------------------------------
# The painted sprites, read back from the sheets as Unity cuts them.


class Sprite:
    def __init__(self, name, rgba, border, pivot, ppu):
        self.name, self.rgba, self.border, self.pivot, self.ppu = name, rgba, border, pivot, ppu

    @property
    def size(self):
        return self.rgba.shape[1], self.rgba.shape[0]


def load_sheet(name):
    image = np.asarray(Image.open(HUD / f"{name}.png").convert("RGBA")).astype(np.float32) / 255.0
    meta = (HUD / f"{name}.png.meta").read_text(encoding="utf-8")
    ppu = float(re.search(r"spritePixelsToUnits: ([\d.]+)", meta).group(1))
    sprites, ids = {}, {}
    pattern = (r"      name: (\S+)\n      rect:\n        serializedVersion: 2\n        x: (\d+)\n        y: (\d+)\n"
               r"        width: (\d+)\n        height: (\d+)\n      alignment: \d+\n      pivot: \{x: ([\d.]+), y: ([\d.]+)\}\n"
               r"      border: \{x: (\d+), y: (\d+), z: (\d+), w: (\d+)\}\n(?:.*\n)*?      internalID: (-?\d+)")
    for m in re.finditer(pattern, meta):
        n, x, y, w, h = m.group(1), *map(int, m.group(2, 3, 4, 5))
        top = image.shape[0] - y - h
        rgba = image[top:top + h, x:x + w].copy()
        rgba[..., :3] *= rgba[..., 3:]  # premultiplied from here on
        key = n.split("_", 2)[-1]
        sprites[key] = Sprite(key, rgba, tuple(map(int, m.group(8, 9, 10, 11))), (float(m.group(6)), float(m.group(7))), ppu)
        ids[m.group(12)] = key
    return sprites, ids


HUD_SPRITES, HUD_IDS = load_sheet("UI_Hud")
BAR_SPRITES, _ = load_sheet("UI_EnemyBar")


def bar_layout():
    text = (HUD / "HudArtSet.asset").read_text(encoding="utf-8")
    bars = {}
    for field in ("_hero", "_experience", "_boss"):
        block = text.split(f"  {field}:\n")[1]
        def num(key):
            m = re.search(rf"    {key}: \{{x: (-?[\d.]+), y: (-?[\d.]+), z: (-?[\d.]+), w: (-?[\d.]+)\}}", block)
            return tuple(float(v) for v in m.groups())
        trail = re.search(r"    _trail: \{fileID: (-?\d+)", block).group(1)
        bars[field] = dict(window=num("_window"), outset=num("_frameOutset"),
                           glint=float(re.search(r"    _glintWidth: ([\d.]+)", block).group(1)),
                           trail=trail != "0")
    return bars


BARS = bar_layout()


# ----------------------------------------------------------------------------------------------------------------------
# A tiny uGUI: premultiplied RGBA images composited onto a float canvas.


def resize(rgba, w, h):
    """Bilinear, premultiplied, as the GPU samples a sprite drawn larger or smaller."""
    w, h = max(1, int(round(w))), max(1, int(round(h)))
    if (w, h) == (rgba.shape[1], rgba.shape[0]):
        return rgba
    channels = [np.asarray(Image.fromarray(rgba[..., k], "F").resize((w, h), Image.BILINEAR)) for k in range(4)]
    return np.stack(channels, -1)


def blit(canvas, rgba, x, y, tint=(1, 1, 1, 1), mask=None):
    """Source-over at integer position (x, y) of the top-left corner, clipped to the canvas."""
    x, y = int(round(x)), int(round(y))
    h, w = rgba.shape[:2]
    x0, y0, x1, y1 = max(0, x), max(0, y), min(canvas.shape[1], x + w), min(canvas.shape[0], y + h)
    if x0 >= x1 or y0 >= y1:
        return
    src = rgba[y0 - y:y1 - y, x0 - x:x1 - x] * np.asarray(tint, np.float32)
    if mask is not None:
        src = src * mask[y0 - y:y1 - y, x0 - x:x1 - x, None]
    dst = canvas[y0:y1, x0:x1]
    dst[..., :3] = src[..., :3] + dst[..., :3] * (1 - src[..., 3:])
    dst[..., 3:] = src[..., 3:] + dst[..., 3:] * (1 - src[..., 3:])


def sliced(sprite, w, h):
    """A Sliced image: corners kept, edges and centre stretched, as Unity cuts at the sprite's borders."""
    l, b, r, t = sprite.border
    sw, sh = sprite.size
    w, h = int(round(w)), int(round(h))
    out = np.zeros((h, w, 4), np.float32)
    xs = [(0, l, 0, l), (l, sw - r, l, w - r), (sw - r, sw, w - r, w)]
    ys = [(0, t, 0, t), (t, sh - b, t, h - b), (sh - b, sh, h - b, h)]
    for sy0, sy1, dy0, dy1 in ys:
        for sx0, sx1, dx0, dx1 in xs:
            if sx1 <= sx0 or sy1 <= sy0 or dx1 <= dx0 or dy1 <= dy0:
                continue
            part = resize(sprite.rgba[sy0:sy1, sx0:sx1], dx1 - dx0, dy1 - dy0)
            out[dy0:dy1, dx0:dx1] = part
    return out


def draw(canvas, sprite, rect, kind="simple", fill=1.0, tint=(1, 1, 1, 1)):
    x, y, w, h = rect
    image = sliced(sprite, w, h) if kind == "sliced" else resize(sprite.rgba, w, h)
    if kind == "filled":
        cut = int(round(image.shape[1] * fill))
        image = image.copy()
        image[:, cut:] = 0
    blit(canvas, image, x, y, tint)


def text(canvas, words, rect, size, colour, align="left", bold=False):
    font = ImageFont.truetype(FONT if not bold else "C:/Windows/Fonts/arialbd.ttf", size)
    x, y, w, h = rect
    layer = Image.new("L", (int(w) + 40, int(h) + 40))
    d = ImageDraw.Draw(layer)
    box = d.textbbox((0, 0), words, font=font)
    tw = box[2] - box[0]
    tx = {"left": 0, "centre": (w - tw) / 2, "right": w - tw}[align] - box[0]
    ty = (h - size * 1.15) / 2
    d.text((tx + 20, ty + 20), words, font=font, fill=255)
    a = np.asarray(layer, np.float32)[20:20 + int(h), 20:20 + int(w)] / 255.0
    rgba = np.zeros(a.shape + (4,), np.float32)
    rgba[..., :3] = np.asarray(colour, np.float32)[None, None] / 255.0 * a[..., None]
    rgba[..., 3] = a
    blit(canvas, rgba, x, y)


def to_canvas(rgb):
    out = np.ones(rgb.shape[:2] + (4,), np.float32)
    out[..., :3] = np.asarray(rgb, np.float32) / 255.0
    return out


def to_image(canvas):
    return Image.fromarray((np.clip(canvas[..., :3], 0, 1) * 255 + 0.5).astype(np.uint8), "RGB")


# ----------------------------------------------------------------------------------------------------------------------
# The Gameplay scene's HUD on a 1080x2340 phone (screen rects: left, top, width, height).

TOP = (24, 8, 1032, 244)
LOWER = (24, 2028, 1032, 252)
PAUSE = (936, 30, 104, 104)
FLOOR_LABEL = (52, 28, 610, 94)
GOLD_ICON = (688, 44, 62, 62)
GOLD_COUNT = (758, 44, 164, 62)
PIPS = [(52 + 96 * i, 133, 82, 8) for i in range(6)]
BADGES = [(52 + 96 * i, 158, 90, 72) for i in range(3)] + [(352 + 96 * i, 158, 90, 72) for i in range(3)]
BOSS_NAME = (80, 265, 920, 42)
BOSS_BAR = (72, 313, 936, 14)
HERO_LABEL = (62, 2055, 700, 48)
HERO_BAR = (64, 2108, 698, 22)
XP_LABEL = (62, 2182, 700, 32)
XP_BAR = (64, 2224, 698, 8)
ABILITY = (806, 2038, 234, 234)
WELL = (846, 2078, 154, 154)
INK = (238, 232, 217)
OLD_NAVY = np.array([10, 17, 32], np.float32)


def window_of(rect, window):
    x, y, w, h = rect
    l, b, r, t = window
    return (x - l, y - t, w + l + r, h + b + t)


def outset(rect, out):
    x, y, w, h = rect
    l, b, r, t = out
    return (x - l, y - t, w + l + r, h + b + t)


def painted_bar(canvas, field, prefix, rect, fill, trail=None):
    """HudArtView's bar: trough, trail, fill, glint and frame, in that order, the fill cropped at its amount."""
    layout = BARS[field]
    win = window_of(rect, layout["window"])
    s = HUD_SPRITES
    draw(canvas, s[prefix + "Trough"], win, "sliced")
    if trail is not None and layout["trail"] and trail > fill:
        draw(canvas, s[prefix + "Trail"], win, "filled", trail)
    if fill > 0:
        draw(canvas, s[prefix + "Fill"], win, "filled", fill)
    if 0 < fill < 1:
        gw = layout["glint"]
        gh = win[3] + 6
        draw(canvas, s[prefix + "Glint"], (win[0] + win[2] * fill - gw / 2, win[1] + win[3] / 2 - gh / 2, gw, gh))
    draw(canvas, s[prefix + "Frame"], outset(win, layout["outset"]), "sliced")


def lift(capture, rect, ink, background=None):
    """A label's (or an icon's) pixels lifted off the old HUD as straight colour over its background: anti-aliased text
    over a flat colour comes back with its coverage."""
    x, y, w, h = rect
    patch = capture[y:y + h, x:x + w].astype(np.float32)
    bg = np.median(np.concatenate([patch[0], patch[-1], patch[:, 0], patch[:, -1]]), axis=0) if background is None \
        else np.asarray(background, np.float32)
    fg = np.asarray(ink, np.float32)
    span = fg - bg
    a = np.clip(((patch - bg) * span).sum(-1) / max((span * span).sum(), 1.0), 0, 1)
    rgba = np.zeros((h, w, 4), np.float32)
    rgba[..., :3] = fg / 255.0 * a[..., None]
    rgba[..., 3] = a
    return rgba


def paint_hud(capture, boss, fills=None, icon_name="UI_Icon_ForgeBurst_v1"):
    """The capture with the painted HUD laid over the old one."""
    fills = fills or {}
    canvas = to_canvas(capture)
    s = HUD_SPRITES
    # What the old HUD showed, lifted before it is painted over: the labels, the coin, the badges' text.
    floor = capture[FLOOR_LABEL[1]:FLOOR_LABEL[1] + FLOOR_LABEL[3], FLOOR_LABEL[0]:FLOOR_LABEL[0] + FLOOR_LABEL[2]]
    keep = {name: capture[r[1]:r[1] + r[3], r[0]:r[0] + r[2]].copy() for name, r in
            (("floor", FLOOR_LABEL), ("coin", GOLD_ICON), ("gold", GOLD_COUNT), ("hero", HERO_LABEL), ("xp", XP_LABEL))}
    pause_ink = lift(capture, (PAUSE[0] + 22, PAUSE[1] + 22, 60, 60), INK)
    badge_ink = [lift(capture, (b[0] + 4, b[1] + 2, b[2] - 8, b[3] - 8), INK) for b in BADGES]
    boss_ink = lift(capture, BOSS_NAME, INK) if boss else None
    del floor
    # The panels: the forged frame, its face the old navy.
    for rect in (TOP, LOWER):
        draw(canvas, s["Panel"], rect, "sliced")
    for name, r in (("floor", FLOOR_LABEL), ("coin", GOLD_ICON), ("gold", GOLD_COUNT), ("hero", HERO_LABEL), ("xp", XP_LABEL)):
        patch = keep[name].astype(np.float32)
        # Only what differs from the old navy is lifted, so the frame under a label's rect shows through.
        diff = np.abs(patch - OLD_NAVY).max(-1)
        a = np.clip((diff - 4) / 10, 0, 1)
        rgba = np.concatenate([patch / 255.0 * a[..., None], a[..., None]], -1)
        blit(canvas, rgba.astype(np.float32), r[0], r[1])
    draw(canvas, s["Button"], PAUSE, "sliced")
    blit(canvas, pause_ink, PAUSE[0] + 22, PAUSE[1] + 22)
    for i, rect in enumerate(PIPS):
        tint = (218 / 255, 165 / 255, 84 / 255, 1) if i < 1 else (60 / 255, 60 / 255, 79 / 255, 1)
        if i < 1:  # one room on these floors
            draw(canvas, s["Pip"], rect, "sliced", tint=tint)
    for i, rect in enumerate(BADGES):
        weapon = i < 3
        filled = i == 0
        alpha = 1.0 if filled else 0.3
        draw(canvas, s["WeaponSlot" if weapon else "TomeSlot"], rect, "sliced", tint=(1, 1, 1, alpha))
        accent = (0.93, 0.62, 0.30) if weapon else (0.48, 0.68, 0.98)
        draw(canvas, s["SlotAccent"], (rect[0], rect[1] + rect[3] - 4, rect[2], 4), "sliced", tint=accent + (alpha,))
        if filled:
            blit(canvas, badge_ink[i], rect[0] + 4, rect[1] + 2)
    if boss:
        draw(canvas, s["BossNamePlate"], BOSS_NAME)
        blit(canvas, boss_ink, BOSS_NAME[0], BOSS_NAME[1])
        painted_bar(canvas, "_boss", "Boss", BOSS_BAR, fills.get("boss", 1.0), fills.get("boss_trail"))
    painted_bar(canvas, "_hero", "Hero", HERO_BAR, fills.get("hero", 1.0), fills.get("hero_trail"))
    painted_bar(canvas, "_experience", "Xp", XP_BAR, fills.get("xp", 0.0))
    # The ability: the icon in its well (the old ring is painted over), the ring round it.
    icon = np.asarray(Image.open(UI / f"{icon_name}.png").convert("RGBA").resize((154, 154), Image.LANCZOS),
                      np.float32) / 255.0
    icon[..., :3] *= icon[..., 3:]
    yy, xx = np.mgrid[0:154, 0:154]
    well = np.clip(77.0 - np.hypot(xx + 0.5 - 77, yy + 0.5 - 77) + 0.5, 0, 1).astype(np.float32)
    blit(canvas, icon, WELL[0], WELL[1], mask=well)
    draw(canvas, s["AbilityRing"], ABILITY)
    return canvas


# ----------------------------------------------------------------------------------------------------------------------
# The enemy bars in the world: found in the capture by the pixel bar's red, replaced by the painted bar.

FALLBACK_FILL = 24  # the pixel bar's fill texels


def find_bars(capture):
    """Each old bar's backing rect (left, top, width, height) and fill fraction, from its red fill: three texels of red
    between dark rows of the backing, a texel inside its left end."""
    c = capture.astype(np.int32)
    # The pixel fill's three rows (EnemyArt.HealthLight, PixelPalette.HealthRed, EnemyArt.HealthShade).
    red = np.zeros(c.shape[:2], bool)
    for colour in ((244, 116, 106), (229, 63, 50), (158, 42, 34)):
        red |= np.abs(c - np.array(colour)).max(-1) < 26
    red = ndimage.binary_closing(red, np.ones((3, 1), bool))
    red[:330] = False  # the world's band only: the HUD's own reds are not enemy bars
    red[2020:] = False
    dark = c.max(-1) < 48
    labels, _ = ndimage.label(red)
    bars = []
    for box in ndimage.find_objects(labels):
        ys, xs = box
        h, w = ys.stop - ys.start, xs.stop - xs.start
        if not (7 <= h <= 13 and w >= 2):
            continue
        # A bar's fill is lit on top and shaded at its foot (a fire's red is not), over most of its length even where
        # another bar overlaps it, in the backing's dark.
        top_row, foot_row = c[ys.start + 1, xs.start:xs.stop], c[ys.stop - 2, xs.start:xs.stop]
        if not (np.median(np.abs(top_row - np.array((244, 116, 106))).max(-1)) < 40 and
                np.median(np.abs(foot_row - np.array((158, 42, 34))).max(-1)) < 40):
            continue
        if not (dark[ys.start - 2, xs.start:xs.stop].mean() > 0.5 or dark[min(ys.stop + 1, c.shape[0] - 1), xs.start:xs.stop].mean() > 0.5):
            continue
        compact = h < 10.5
        sx, sy = (0.65, 0.8) if compact else (1.0, 1.0)
        texel_x, texel_y = PHONE / 32 * sx, PHONE / 32 * sy
        full, tall = 26 * texel_x, 5 * texel_y
        left, top = xs.start - texel_x, (ys.start + ys.stop) / 2 - tall / 2
        bars.append(dict(rect=(left, top, full, tall), fill=min(1.0, w / (24 * texel_x)), compact=compact))
    return bars


def erase(canvas, rect):
    """Fills an old bar's rect from what lies round it (nearest pixels, softened)."""
    x, y, w, h = rect
    x0, y0, x1, y1 = int(math.floor(x)) - 1, int(math.floor(y)) - 1, int(math.ceil(x + w)) + 1, int(math.ceil(y + h)) + 1
    pad = 6
    region = canvas[y0 - pad:y1 + pad, x0 - pad:x1 + pad, :3].copy()
    hole = np.zeros(region.shape[:2], bool)
    hole[pad:-pad, pad:-pad] = True
    _, (iy, ix) = ndimage.distance_transform_edt(hole, return_indices=True)
    filled = region[iy, ix]
    filled = ndimage.gaussian_filter(filled, (2, 2, 0))
    region[hole] = filled[hole]
    canvas[y0 - pad:y1 + pad, x0 - pad:x1 + pad, :3] = region


def enemy_bar(canvas, centre_x, centre_y, fill, compact, trail=None, scale=PHONE):
    """EnemyLookView's painted bar: backing, chunk, fill (whole texels of its width from its left edge), centred at the
    given screen point; compact bars squeezed to 0.65 by 0.8."""
    sx, sy = (0.65, 0.8) if compact else (1.0, 1.0)
    back = BAR_SPRITES["CompactBack" if compact else "Back"]
    fill_sprite = BAR_SPRITES["CompactFill" if compact else "Fill"]
    trail_sprite = BAR_SPRITES["CompactTrail" if compact else "Trail"]
    k = scale / back.ppu
    bw, bh = back.size[0] * k * sx, back.size[1] * k * sy
    image = resize(back.rgba, bw, bh)
    # Sub-pixel placement as the GPU would: resample onto the pixel grid by its fractional offset.
    def place(rgba, left, top):
        fx, fy = left - math.floor(left), top - math.floor(top)
        shifted = ndimage.shift(rgba, (fy, fx, 0), order=1, mode="constant")
        pad = np.zeros((rgba.shape[0] + 2, rgba.shape[1] + 2, 4), np.float32)
        pad[:rgba.shape[0], :rgba.shape[1]] = shifted
        blit(canvas, pad, math.floor(left), math.floor(top))
    left, top = centre_x - bw / 2, centre_y - bh / 2
    place(image, left, top)
    fw_texels = fill_sprite.size[0]
    fw, fh = fw_texels * k * sx, fill_sprite.size[1] * k * sy
    fl = centre_x - fw / 2
    ft = centre_y - fh / 2
    for sprite, amount in ((trail_sprite, trail), (fill_sprite, fill)):
        if amount is None or amount <= 0 or (sprite is trail_sprite and amount <= fill):
            continue
        shown = math.ceil(amount * fw_texels) / fw_texels
        img = resize(sprite.rgba, fw * shown, fh)
        place(img, fl, ft)


def replace_bars(canvas, capture):
    bars = find_bars(capture)
    for bar in bars:
        erase(canvas, bar["rect"])
    for bar in bars:
        x, y, w, h = bar["rect"]
        enemy_bar(canvas, x + w / 2, y + h / 2, bar["fill"], bar["compact"])
    return bars


# ----------------------------------------------------------------------------------------------------------------------
# The sheets.


def label(image, words, at, size=26, colour=(235, 228, 210)):
    d = ImageDraw.Draw(image)
    d.text(at, words, font=ImageFont.truetype(FONT, size), fill=colour)


def phone(name, path, boss):
    capture = np.asarray(Image.open(path).convert("RGB"))
    canvas = paint_hud(capture, boss, icon_name=ICONS[name])
    bars = replace_bars(canvas, capture)
    after = to_image(canvas)
    before = Image.fromarray(capture)
    sheet = Image.new("RGB", (W * 2 + 60, H + 90), (24, 22, 28))
    sheet.paste(before, (20, 70))
    sheet.paste(after, (W + 40, 70))
    label(sheet, "before (in-game capture, 1080x2340)", (20, 20), 34)
    label(sheet, f"after (painted HUD and {len(bars)} enemy bars laid over the same frame)", (W + 40, 20), 34)
    sheet.save(OUT / f"phone-{name}-before-after.png")
    after.save(OUT / f"phone-{name}-after.png")
    return capture, after


def stone(size, capture):
    """A patch of the court's stone from the capture, clear of the actors."""
    w, h = size
    return capture[340:340 + h, 0:w]


def lava(size, seed=3):
    """Molten orange in the pit's sampled colours (as review_staff_impact paints it)."""
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


def zoom(image, factor):
    return image.resize((image.width * factor, image.height * factor), Image.NEAREST)


STATES = (("full", 1.0, None), ("half, a hit's chunk trailing", 0.5, 0.68), ("low", 0.12, None))


def bars_sheet(capture):
    rows = []
    for ground_name, ground in (("stone", lambda s: stone(s, capture)), ("lava", lava)):
        for state, fill, trail in STATES:
            w, h = 1080, 120
            canvas = to_canvas(ground((w, h)))
            panel_rect = (24, 4, 1032, 112)
            # The hero's and the experience bar sit on the bottom panel, the boss bar over the floor itself.
            boss_rect = (72, 70, 936, 14)
            boss_canvas = to_canvas(ground((w, 110)))
            painted_bar(boss_canvas, "_boss", "Boss", boss_rect, fill, trail)
            draw(canvas, HUD_SPRITES["Panel"], panel_rect, "sliced")
            painted_bar(canvas, "_hero", "Hero", (64, 30, 698, 22), fill, trail)
            painted_bar(canvas, "_experience", "Xp", (64, 86, 698, 8), fill)
            text(canvas, f"{int(round(fill * 100))} / 100 HP", (780, 24, 260, 34), 28, INK)
            enemies = to_canvas(ground((w, 70)))
            for i, compact in enumerate((False, True, False, True)):
                enemy_bar(enemies, 120 + 230 * i, 35, fill, compact, trail)
            block = np.concatenate([boss_canvas, canvas, enemies], 0)
            rows.append((f"{state} on {ground_name}", to_image(block)))
    sheet_w = 1080 + 40
    height = sum(img.height + 50 for _, img in rows) + 20
    sheet = Image.new("RGB", (sheet_w, height), (24, 22, 28))
    y = 10
    for title, img in rows:
        label(sheet, title, (20, y + 6), 28)
        sheet.paste(img, (20, y + 44))
        y += img.height + 50
    sheet.save(OUT / "bars-on-stone-and-lava.png")
    # Enlarged: the left ends of the three HUD bars and the enemy bars, x3.
    crops = []
    for title, img in rows:
        crops.append(zoom(img.crop((30, 40, 380, 300)), 3))
    grid = Image.new("RGB", (crops[0].width * 2 + 30, (crops[0].height + 50) * 3 + 10), (24, 22, 28))
    for i, (crop, (title, _)) in enumerate(zip(crops, rows)):
        cx, cy = (i // 3) * (crop.width + 10) + 10, (i % 3) * (crop.height + 50) + 10
        label(grid, title + " (x3)", (cx, cy), 28)
        grid.paste(crop, (cx, cy + 40))
    grid.save(OUT / "bars-on-stone-and-lava-x3.png")


def crowd_sheet(after, capture):
    region = (250, 900, 830, 1300)
    before = Image.fromarray(capture).crop(region)
    painted = after.crop(region)
    sheet = Image.new("RGB", (before.width * 3 * 2 + 30, before.height * 3 + 60), (24, 22, 28))
    sheet.paste(zoom(before, 3), (10, 50))
    sheet.paste(zoom(painted, 3), (before.width * 3 + 20, 50))
    label(sheet, "before (x3)", (10, 10), 30)
    label(sheet, "after: painted enemy bars, full and the Mites' compact (x3)", (before.width * 3 + 20, 10), 30)
    sheet.save(OUT / "enemy-bars-crowd-x3.png")
    phone_size = Image.new("RGB", (before.width * 2 + 30, before.height + 60), (24, 22, 28))
    phone_size.paste(before, (10, 50))
    phone_size.paste(painted, (before.width + 20, 50))
    label(phone_size, "before (phone pixels)", (10, 10), 28)
    label(phone_size, "after (phone pixels)", (before.width + 20, 10), 28)
    phone_size.save(OUT / "enemy-bars-crowd.png")


def sprites_sheet():
    entries = [(k, s, 2 if s.size[0] > 200 else 4 if s.size[0] > 60 else 6) for k, s in HUD_SPRITES.items()] + \
              [("Enemy " + k, s, 6) for k, s in BAR_SPRITES.items()]
    # Long fills are shown as their left 220 texels.
    placed, x, y, row_h = [], 20, 20, 0
    width = 2200
    for name, sprite, factor in entries:
        rgba = sprite.rgba
        if rgba.shape[1] > 300:
            rgba = rgba[:, :220]
        img_w, img_h = rgba.shape[1] * factor, rgba.shape[0] * factor
        tag_w = ImageDraw.Draw(Image.new("L", (1, 1))).textlength(
            f"{name} {sprite.size[0]}x{sprite.size[1]} x{factor}  border {sprite.border}", font=ImageFont.truetype(FONT, 20))
        if x + max(img_w, tag_w) + 20 > width:
            x, y, row_h = 20, y + row_h + 60, 0
        placed.append((name, sprite, rgba, factor, x, y))
        tag_w = ImageDraw.Draw(Image.new("L", (1, 1))).textlength(
            f"{name} {sprite.size[0]}x{sprite.size[1]} x{factor}  border {sprite.border}", font=ImageFont.truetype(FONT, 20))
        x += int(max(img_w, tag_w)) + 30
        row_h = max(row_h, img_h)
    height = y + row_h + 80
    sheet = Image.new("RGB", (width, height), (24, 22, 28))
    checker = np.indices((height, width)).sum(0) // 12 % 2
    base = np.where(checker[..., None] == 1, np.array([70, 66, 78]), np.array([56, 52, 64])).astype(np.uint8)
    sheet = Image.fromarray(base)
    d = ImageDraw.Draw(sheet)
    font = ImageFont.truetype(FONT, 20)
    for name, sprite, rgba, factor, x, y in placed:
        canvas = to_canvas(np.asarray(sheet.crop((x, y + 30, x + rgba.shape[1] * factor, y + 30 + rgba.shape[0] * factor))))
        big = np.repeat(np.repeat(rgba, factor, 0), factor, 1)
        blit(canvas, big, 0, 0)
        sheet.paste(to_image(canvas), (x, y + 30))
        l, b, r, t = sprite.border
        w_, h_ = rgba.shape[1] * factor, rgba.shape[0] * factor
        for bx in ([l * factor] if l else []) + ([w_ - r * factor] if r and rgba.shape[1] == sprite.size[0] else []):
            d.line([(x + bx, y + 30), (x + bx, y + 30 + h_)], fill=(80, 255, 120), width=2)
        for by in ([t * factor] if t else []) + ([h_ - b * factor] if b else []):
            d.line([(x, y + 30 + by), (x + w_, y + 30 + by)], fill=(80, 255, 120), width=2)
        tag = f"{name} {sprite.size[0]}x{sprite.size[1]} x{factor}" + (f"  border {sprite.border}" if any(sprite.border) else "")
        d.text((x, y + 4), tag, font=font, fill=(235, 228, 210))
    sheet.save(OUT / "sprites-and-borders.png")


def main():
    capture = None
    for name, path in CAPTURES.items():
        if path:
            cap, after = phone(name, path, boss=(name == "warden"))
            if name == "crowd":
                crowd_sheet(after, cap)
                capture = cap
    if capture is None and CAPTURES["warden"]:
        capture = np.asarray(Image.open(CAPTURES["warden"]).convert("RGB"))
    if capture is not None:
        bars_sheet(capture)
    sprites_sheet()
    print("wrote", OUT)


if __name__ == "__main__":
    main()
