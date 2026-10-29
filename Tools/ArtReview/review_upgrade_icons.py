"""Review sheets for the upgrade card icons (build_upgrade_icons.py).

    python Tools/ArtReview/review_upgrade_icons.py --out DIR --capture CAPTURE.png [--hud HUD_AFTER.png]

Reads the painted sprites back from Art/UI/UpgradeIcons (the sheet and its meta's rects, and the set's ids) and lays them
out as the game does on a 1080x2340 phone (one canvas unit a pixel, the safe area the whole screen): the Choice Panel's
dimmed screen, its Choice Surface and cards in the scene's rects (Gameplay.unity), the cards' forged frame cut at its
9-slice border at the scene's pixels-per-unit multiplier, each icon sampled bilinearly without mipmaps as the GPU does,
and the labels in the scene's fonts, sizes and colours; and the loadout row's badges in HudArtSet's sockets.

Writes:
  - icons-sheet.png: every card icon with its id, at its painted size, and the socket icons beside them.
  - cards-category-phone.png: three category offers as the foundry stacks them (three 190-tall cards, 120 icons) over
    the capture, at phone size; cards-category-x2.png: one of them enlarged.
  - cards-hall-phone.png: the hall's two cards (160 icons): a painted legacy icon beside a set icon.
  - loadout-before-after.png: the top panel's loadout row before (the names) and after (the icons), at phone size and
    enlarged, over the painted HUD (--hud, the painted HUD's own review frame) or the capture.
"""
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ICONS = ROOT / "Assets/_Project/Art/UI/UpgradeIcons"
UI = ROOT / "Assets/_Project/Art/UI"
HUD = UI / "Hud"
ARGS = sys.argv[1:]


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


FONT = "C:/Windows/Fonts/arial.ttf"
BOLD = "C:/Windows/Fonts/arialbd.ttf"
W, H = 1080, 2340
INK = (238, 232, 217)  # the scene's label colour (0.933, 0.910, 0.851)
NAVY = (12, 17, 35)


# ----------------------------------------------------------------------------------------------------------------------
# Sprites, read back from their sheets as Unity cuts them (rects from the bottom left), premultiplied.


def load_sheet(png, meta):
    image = np.asarray(Image.open(png).convert("RGBA")).astype(np.float32) / 255.0
    text = Path(meta).read_text(encoding="utf-8")
    out = {}
    pattern = (r"      name: (\S+)\n      rect:\n        serializedVersion: 2\n        x: (\d+)\n        y: (\d+)\n"
               r"        width: (\d+)\n        height: (\d+)\n(?:.*\n)*?      border: \{x: (\d+), y: (\d+), z: (\d+), w: (\d+)\}")
    for m in re.finditer(pattern, text):
        name, x, y, w, h = m.group(1), *map(int, m.group(2, 3, 4, 5))
        top = image.shape[0] - y - h
        rgba = image[top:top + h, x:x + w].copy()
        rgba[..., :3] *= rgba[..., 3:]
        out[name] = (rgba, tuple(map(int, m.group(6, 7, 8, 9))))
    return out


def load_whole(png, border):
    rgba = np.asarray(Image.open(png).convert("RGBA")).astype(np.float32) / 255.0
    rgba[..., :3] *= rgba[..., 3:]
    return rgba, border


ICON_SPRITES = load_sheet(ICONS / "UI_UpgradeIcons.png", ICONS / "UI_UpgradeIcons.png.meta")
HUD_SPRITES = load_sheet(HUD / "UI_Hud.png", HUD / "UI_Hud.png.meta")
CARD_FRAME = load_whole(UI / "UI_Panel_ForgedBlank_v2.png", (128, 128, 128, 128))


def icon(id_, slot=False):
    return ICON_SPRITES[f"UI_UpgradeIcons_{id_}{'_slot' if slot else ''}"][0]


def legacy(name):
    rgba = np.asarray(Image.open(UI / f"UI_Icon_{name}_v1.png").convert("RGBA")).astype(np.float32) / 255.0
    rgba[..., :3] *= rgba[..., 3:]
    return rgba


# ----------------------------------------------------------------------------------------------------------------------
# A tiny uGUI over a premultiplied float canvas.


def gpu(rgba, w, h):
    """Sampled as the GPU samples an unmipped texture drawn at another size: bilinear at each pixel's centre."""
    w, h = int(round(w)), int(round(h))
    sh, sw = rgba.shape[:2]
    if (w, h) == (sw, sh):
        return rgba
    u = (np.arange(w) + 0.5) * sw / w - 0.5
    v = (np.arange(h) + 0.5) * sh / h - 0.5
    x0 = np.clip(np.floor(u).astype(int), 0, sw - 1)
    y0 = np.clip(np.floor(v).astype(int), 0, sh - 1)
    x1, y1 = np.clip(x0 + 1, 0, sw - 1), np.clip(y0 + 1, 0, sh - 1)
    fx = np.clip(u - np.floor(u), 0, 1)[None, :, None]
    fy = np.clip(v - np.floor(v), 0, 1)[:, None, None]
    a = rgba[y0][:, x0]
    b = rgba[y0][:, x1]
    c = rgba[y1][:, x0]
    d = rgba[y1][:, x1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def blit(canvas, rgba, x, y, tint=(1, 1, 1, 1)):
    x, y = int(round(x)), int(round(y))
    h, w = rgba.shape[:2]
    x0, y0, x1, y1 = max(0, x), max(0, y), min(canvas.shape[1], x + w), min(canvas.shape[0], y + h)
    if x0 >= x1 or y0 >= y1:
        return
    src = rgba[y0 - y:y1 - y, x0 - x:x1 - x] * np.asarray(tint, np.float32)
    dst = canvas[y0:y1, x0:x1]
    dst[..., :3] = src[..., :3] + dst[..., :3] * (1 - src[..., 3:])
    dst[..., 3:] = src[..., 3:] + dst[..., 3:] * (1 - src[..., 3:])


def sliced(sprite, w, h, scale=1.0):
    """A Sliced image: corners kept (at 1/scale for a pixels-per-unit multiplier), edges and centre stretched."""
    rgba, (l, b, r, t) = sprite
    sh, sw = rgba.shape[:2]
    w, h = int(round(w)), int(round(h))
    dl, db, dr, dt = (int(round(v / scale)) for v in (l, b, r, t))
    out = np.zeros((h, w, 4), np.float32)
    xs = [(0, l, 0, dl), (l, sw - r, dl, w - dr), (sw - r, sw, w - dr, w)]
    ys = [(0, t, 0, dt), (t, sh - b, dt, h - db), (sh - b, sh, h - db, h)]
    for sy0, sy1, dy0, dy1 in ys:
        for sx0, sx1, dx0, dx1 in xs:
            if sx1 > sx0 and sy1 > sy0 and dx1 > dx0 and dy1 > dy0:
                out[dy0:dy1, dx0:dx1] = gpu(rgba[sy0:sy1, sx0:sx1], dx1 - dx0, dy1 - dy0)
    return out


def wrapped(words, font, width):
    """The words broken into lines no wider than width, as uGUI's Text wraps them."""
    measure = ImageDraw.Draw(Image.new("L", (4, 4)))
    lines, line = [], ""
    for word in words.split(" "):
        trial = (line + " " + word).strip()
        if line and measure.textlength(trial, font=font) > width:
            lines.append(line)
            line = word
        else:
            line = trial
    return lines + [line]


def text(canvas, words, rect, size, colour, align="left", bold=False, minimum=None):
    """A label in its rect, wrapped to its width and vertically centred; with a minimum, shrunk (best fit) until its
    lines fit the rect's height."""
    x, y, w, h = rect
    size = int(size)
    while True:
        font = ImageFont.truetype(BOLD if bold else FONT, size)
        lines = wrapped(words, font, w)
        if minimum is None or size <= minimum or len(lines) * size * 1.15 <= h:
            break
        size -= 1
    layer = Image.new("L", (int(w) + 80, int(h) + 80))
    d = ImageDraw.Draw(layer)
    top = (h - len(lines) * size * 1.15) / 2
    for k, line in enumerate(lines):
        box = d.textbbox((0, 0), line, font=font)
        tw = box[2] - box[0]
        tx = {"left": 0, "centre": (w - tw) / 2, "right": w - tw}[align] - box[0]
        d.text((tx + 40, top + k * size * 1.15 + 40), line, font=font, fill=255)
    a = np.asarray(layer, np.float32)[40:40 + int(h), 40:40 + int(w)] / 255.0
    rgba = np.zeros(a.shape + (4,), np.float32)
    rgba[..., :3] = np.asarray(colour, np.float32)[None, None] / 255.0 * a[..., None]
    rgba[..., 3] = a
    blit(canvas, rgba, x, y)


def to_canvas(rgb):
    out = np.ones(rgb.shape[:2] + (4,), np.float32)
    out[..., :3] = np.asarray(rgb, np.float32)[..., :3] / 255.0
    return out


def to_image(canvas):
    return Image.fromarray((np.clip(canvas[..., :3], 0, 1) * 255 + 0.5).astype(np.uint8), "RGB")


def fill(canvas, rect, colour):
    x, y, w, h = rect
    rgba = np.zeros((h, w, 4), np.float32)
    rgba[..., :3] = np.asarray(colour[:3], np.float32) * colour[3]
    rgba[..., 3] = colour[3]
    blit(canvas, rgba, x, y)


# ----------------------------------------------------------------------------------------------------------------------
# The Choice Panel on the phone (Gameplay.unity): the surface 992x860 at 0.42 of the screen up, its title 322 above its
# centre, the two authored cards 896x252 at 101 and -191; a category offer stacks three 190 tall from the first card's
# top, 28 apart (RunChoiceView.LayOut), each part scaled with its card.

RARITY = [(0x4C, 0xC3, 0x6A), (0xF0, 0x8A, 0x24), (0xE0, 0x39, 0x3E), (0xF5, 0xC5, 0x42)]
KIND = {"new": (255, 219, 115), "level": (199, 214, 235), "evolution": (255, 140, 56)}
SURFACE = (W / 2 - 496, H - 0.42 * H - 430, 992, 860)


def panel(canvas, title):
    fill(canvas, (0, 0, W, H), (0.0275, 0.0353, 0.0706, 0.863))
    sx, sy, sw, sh = SURFACE
    blit(canvas, sliced(CARD_FRAME, sw, sh, 7.0), sx, sy)
    text(canvas, title, (sx + 52, sy + 430 - 322 - 63, 888, 126), 42, INK, "centre", True, 32)


def card(canvas, centre_y, height, art, name, rarity, description, kind=None, kind_colour=None, icon_size=None):
    """One card centred `centre_y` below the surface's centre (canvas units, y down)."""
    sx, sy, sw, sh = SURFACE
    cx, cy = sx + sw / 2, sy + sh / 2 + centre_y
    x0, y0 = cx - 448, cy - height / 2
    blit(canvas, sliced(CARD_FRAME, 896, height, 7.0), x0, y0)
    scale = height / 252.0
    size = icon_size or 160 * scale
    if art is not None:
        blit(canvas, gpu(art, size, size), x0 + 34, cy - size / 2)
    # Name and description: the top band keeps its 36; the rest shares what is left (LayOut).
    header, block_bottom = 36.0, 224.0
    footer = (252 - block_bottom) * scale
    k = (height - header - footer) / (block_bottom - header)
    name_y, name_h = header, 76 * k
    desc_y, desc_h = header + (116 - header) * k, 108 * k
    text(canvas, name, (x0 + 216, y0 + name_y, 624, name_h), 38, RARITY[rarity] if rarity is not None else INK,
         bold=True, minimum=30)
    text(canvas, description, (x0 + 216, y0 + desc_y, 624, desc_h), 30, INK, minimum=26)
    if kind:
        text(canvas, kind, (x0 + 896 - 28 - 380, y0 + 8, 380, 28), 24, kind_colour, "right", True, 18)


def stacked(canvas, offer):
    for i, c in enumerate(offer):
        card(canvas, -(227 - 95 - i * 218), 190, *c)


# ----------------------------------------------------------------------------------------------------------------------
# The loadout row (LoadoutBadgesView) on HudArtSet's sockets: six 90x72 badges from x 52, y 158.

BADGES = [(52 + 96 * i, 158, 90, 72) for i in range(3)] + [(352 + 96 * i, 158, 90, 72) for i in range(3)]
WEAPON_ACCENT, TOME_ACCENT, EVOLVED = (0.93, 0.62, 0.30), (0.48, 0.68, 0.98), (1.0, 0.80, 0.30)
BADGE_INK = (237, 232, 217)


def badge(canvas, rect, weapon, filled, short=None, level=None, evolved=False, art=None):
    s = HUD_SPRITES
    alpha = 1.0 if filled else 0.3
    plate = s["UI_Hud_WeaponSlot" if weapon else "UI_Hud_TomeSlot"]
    x, y, w, h = rect
    blit(canvas, sliced(plate, w, h), x, y, (1, 1, 1, alpha))
    accent = EVOLVED if evolved else (WEAPON_ACCENT if weapon else TOME_ACCENT)
    blit(canvas, sliced(s["UI_Hud_SlotAccent"], w, 4), x, y + h - 4, accent + (alpha,))
    if not filled:
        return
    ink = tuple(int(v * 255) for v in EVOLVED) if evolved else BADGE_INK
    if art is None:
        name = f"{short}+" if evolved else short
        text(canvas, name, (x + 4, y + 3, w - 8, 28), 24, ink, "centre", False, 16)
        text(canvas, str(level), (x + 4, y + h - 5 - 36, w - 8, 36), 34, ink, "centre", True, 24)
    else:
        blit(canvas, gpu(art, 56, 56), x + 4, y + 6)
        text(canvas, str(level), (x + 60, y + 6, 28, 56), 34, ink, "centre", True, 24)


LOADOUT = [  # (weapon?, id, short name, level, evolved)
    (True, "weapon_sword", "Sword", 4, False), (True, "weapon_staff", "Staff", 2, False),
    (True, "weapon_daggers", "Dag", 8, True),
    (False, "tome_force", "Force", 3, False), (False, "tome_reach", "Reach", 1, False),
    (False, "tome_vitality", "Vit", 5, False),
]
EVOLUTION = {"weapon_sword": "evolution_sword", "weapon_staff": "evolution_staff", "weapon_daggers": "evolution_daggers"}


def loadout(canvas, icons, entries=LOADOUT):
    for rect, entry in zip(BADGES, entries):
        if entry is None:
            badge(canvas, rect, rect[0] < 340, False)
            continue
        weapon, id_, short, level, evolved = entry
        art = icon(EVOLUTION[id_] if evolved else id_, slot=True) if icons else None
        badge(canvas, rect, weapon, True, short, level, evolved, art)


def clear_row(canvas, background):
    """The badge row's band painted back to the panel's face, so the old badges never show under the new."""
    x0, y0 = BADGES[0][0] - 2, BADGES[0][1] - 2
    x1 = BADGES[-1][0] + BADGES[-1][2] + 2
    y1 = BADGES[0][1] + BADGES[0][3] + 2
    fill(canvas, (x0, y0, x1 - x0, y1 - y0), tuple(v / 255.0 for v in background) + (1.0,))


# ----------------------------------------------------------------------------------------------------------------------
# The sheets.


def label(image, words, at, size=18, colour=(235, 228, 210)):
    ImageDraw.Draw(image).text(at, words, font=ImageFont.truetype(FONT, size), fill=colour)


def icons_sheet(out, ids):
    cols = 8
    cell_w, cell_h = 212, 196
    rows = (len(ids) + cols - 1) // cols
    canvas = to_canvas(np.zeros((rows * cell_h + 120, cols * cell_w, 3)) + NAVY)
    for i, id_ in enumerate(ids):
        r, c = divmod(i, cols)
        blit(canvas, icon(id_), c * cell_w + 34, r * cell_h + 8)
    slots = [k[len("UI_UpgradeIcons_"):-len("_slot")] for k in ICON_SPRITES if k.endswith("_slot")]
    y = rows * cell_h + 16
    for i, id_ in enumerate(slots):
        blit(canvas, icon(id_, True), 16 + i * 96, y)
    img = to_image(canvas)
    for i, id_ in enumerate(ids):
        r, c = divmod(i, cols)
        label(img, id_, (c * cell_w + 6, r * cell_h + 160), 13)
    label(img, "socket icons (56x56): " + ", ".join(slots), (16, y + 64), 14)
    img.save(out / "icons-sheet.png")
    img.resize((img.width * 2, img.height * 2), Image.NEAREST).save(out / "icons-sheet-x2.png")


def category_cards(out, capture):
    offers = [
        [(icon("weapon_staff"), "Staff", 0, "11 damage every 1.1s. Blasts every enemy near the target for 75%",
          "NEW WEAPON", KIND["new"]),
         (icon("line_sword_damage"), "Sword", 1, "+2.5 damage", "Lv 2 -> 3", KIND["level"]),
         (icon("tome_force"), "Force", 2, "+10% damage to every weapon", "NEW TOME", KIND["new"])],
        [(icon("evolution_staff"), "Cinder Nova", 0, "Kills burst for 40% damage nearby, and a harder world",
          "EVOLUTION", KIND["evolution"]),
         (icon("line_staff_area"), "Staff", 3, "+16% area", "Lv 5 -> 6", KIND["level"]),
         (icon("upgrade_health_evolved"), "Ironblood", 1, "+50 maximum health, and a harder world", None, None)],
        [(icon("line_daggers_crit"), "Daggers", 0, "+4% crit chance", "Lv 1 -> 2", KIND["level"]),
         (icon("tome_reach"), "Reach", 2, "+12% area to every weapon", "Lv 3 -> 4", KIND["level"]),
         (icon("upgrade_move_speed"), "Swift Boots", 3, "+16% movement speed", None, None)],
    ]
    phones = []
    for offer in offers:
        canvas = to_canvas(capture)
        panel(canvas, "Level up! Choose one upgrade")
        stacked(canvas, offer)
        phones.append(to_image(canvas))
    crop = (0, 880, W, 1840)
    strip = Image.new("RGB", (W * len(phones) + 20 * (len(phones) - 1), crop[3] - crop[1]), (30, 30, 30))
    for i, p in enumerate(phones):
        strip.paste(p.crop(crop), (i * (W + 20), 0))
    strip.save(out / "cards-category-phone.png")
    phones[0].save(out / "cards-category-full.png")
    zoom = phones[1].crop((40, 900, 1040, 1820))
    zoom.resize((zoom.width * 2, zoom.height * 2), Image.NEAREST).save(out / "cards-category-x2.png")


def hall_cards(out, capture):
    canvas = to_canvas(capture)
    panel(canvas, "Level up! Choose one upgrade")
    card(canvas, -101, 252, icon("upgrade_range"), "Long Reach", 1, "+0.3 reach")
    card(canvas, 191, 252, legacy("TemperedEdge"), "Tempered Edge", 0, "+50% damage per hit")
    a = to_image(canvas)
    canvas = to_canvas(capture)
    panel(canvas, "Level up! Choose one upgrade")
    card(canvas, -101, 252, icon("upgrade_crit"), "Keen Edge", 2, "+5% chance to crit")
    card(canvas, 191, 252, legacy("QuickenedGrip"), "Quickened Grip", 3, "+30% attack speed")
    b = to_image(canvas)
    crop = (0, 900, W, 1820)
    strip = Image.new("RGB", (W * 2 + 20, crop[3] - crop[1]), (30, 30, 30))
    strip.paste(a.crop(crop), (0, 0))
    strip.paste(b.crop(crop), (W + 20, 0))
    strip.save(out / "cards-hall-phone.png")


def loadout_sheet(out, hud):
    face = tuple(int(v) for v in np.median(hud[190:205, 700:720].reshape(-1, 3), axis=0))
    before, after = to_canvas(hud), to_canvas(hud)
    clear_row(before, face)
    clear_row(after, face)
    loadout(before, False)
    loadout(after, True)
    crop = (0, 0, W, 270)
    b, a = to_image(before).crop(crop), to_image(after).crop(crop)
    early = to_canvas(hud)
    clear_row(early, face)
    loadout(early, True, [(True, "weapon_sword", "Sword", 1, False), None, None, None, None, None])
    e = to_image(early).crop(crop)
    sheet = Image.new("RGB", (W, crop[3] * 3 + 3 * 40), (30, 30, 30))
    for i, (img, words) in enumerate(((b, "before: the names (painted sockets)"), (a, "after: the icons, the level beside each"),
                                      (e, "after, at the run's start: the forge's weapon, the rest free"))):
        sheet.paste(img, (0, i * (crop[3] + 40) + 40))
        label(sheet, words, (16, i * (crop[3] + 40) + 10), 22)
    sheet.save(out / "loadout-before-after.png")
    row = (40, 150, 660, 240)
    pair = Image.new("RGB", ((row[2] - row[0]) * 2, (row[3] - row[1]) * 4 + 30), (30, 30, 30))
    pair.paste(b.crop(row).resize(((row[2] - row[0]) * 2, (row[3] - row[1]) * 2), Image.NEAREST), (0, 0))
    pair.paste(a.crop(row).resize(((row[2] - row[0]) * 2, (row[3] - row[1]) * 2), Image.NEAREST), (0, (row[3] - row[1]) * 2 + 30))
    pair.save(out / "loadout-before-after-x2.png")


def main():
    out = Path(arg("--out", ROOT / "TestResults/upgrade-icon-review"))
    out.mkdir(parents=True, exist_ok=True)
    capture = np.asarray(Image.open(arg("--capture")).convert("RGB"))
    hud = np.asarray(Image.open(arg("--hud", arg("--capture"))).convert("RGB"))
    ids = re.findall(r"  - _id: (\S+)", (ICONS / "UpgradeIconSet.asset").read_text(encoding="utf-8"))
    icons_sheet(out, ids)
    category_cards(out, capture)
    hall_cards(out, capture)
    loadout_sheet(out, hud)
    print("wrote", ", ".join(sorted(p.name for p in out.glob("*.png"))))


if __name__ == "__main__":
    main()
