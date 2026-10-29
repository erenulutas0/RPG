"""Review sheets for the painted choice cards (build_choice_card_art.py).

    python -B Tools/ArtReview/review_choice_cards.py --out DIR --levelup CAPTURE.png --evolution CAPTURE.png
                                                     --checkpoint CAPTURE.png [--forge CAPTURE.png]

Reads the painted sprites back from Art/UI/ChoiceCards (the sheet, its meta's rects and 9-slice borders, and the set's
margins) and lays the choice panel out as RunChoiceView and ChoiceCardPainter do on a 1080x2340 phone (one canvas unit a
pixel, the safe area the whole screen): the Choice Surface anchored at 0.42 of the screen's height, 992x860, its title
322 above its centre, the scene's two 896x252 cards at 101 and -191, or three 190-unit cards stacked 28 apart as
LayOut stacks them, each card's name, description, icon and kind line where the scene and LayOut put them, Sliced
images cut at their borders, and the labels set in Arial at the largest size their best fit allows.

"Before" is the panel as it is without the set: the forged blank panel (UI_Panel_ForgedBlank_v2, imported at 1024 and
sliced at a seventh) for the surface and every card, the flat navy dim, the tier on the name only. Both are drawn over
the capture with the painted HUD laid over its HUD first (review_hud_art.paint_hud), as the run under the panel now is.

Writes:
  - phone-<name>-before-after.png and phone-<name>-after.png for each capture given: a level-up of mixed tiers, an
    offer holding an evolution, the checkpoint and (if given) the forge.
  - tiers-closeup.png: every treatment at rest with its well empty, with a square icon in its well, pressed and
    waiting (disabled), at phone size, and enlarged.
  - sprites-and-borders.png: every painted sprite, enlarged, with its 9-slice borders marked.
"""
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/_Project/Art/UI/ChoiceCards"
UI = ROOT / "Assets/_Project/Art/UI"
ARGS = sys.argv[1:]


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


OUT = Path(arg("--out", ROOT / "TestResults/choice-card-review"))
OUT.mkdir(parents=True, exist_ok=True)
CAPTURES = {k: arg("--" + k) for k in ("levelup", "evolution", "checkpoint", "forge")}
W, H = 1080, 2340
FONT = "C:/Windows/Fonts/arial.ttf"
FONT_BOLD = "C:/Windows/Fonts/arialbd.ttf"

# The painted HUD under the panel: review_hud_art lays it over a capture's old HUD. It reads its own arguments when
# imported, so it is given an output folder of its own under ours.
sys.argv = [sys.argv[0], "--out", str(OUT / "hud")]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import review_hud_art as hudreview  # noqa: E402

sys.argv = [sys.argv[0]] + ARGS


# ----------------------------------------------------------------------------------------------------------------------
# The painted sprites, as Unity cuts them, and the set's numbers.


class Sprite:
    def __init__(self, name, rgba, border, pivot=(0.5, 0.5)):
        self.name, self.rgba, self.border, self.pivot = name, rgba, border, pivot

    @property
    def size(self):
        return self.rgba.shape[1], self.rgba.shape[0]


def load_sheet():
    image = np.asarray(Image.open(ART / "UI_ChoiceCards.png").convert("RGBA")).astype(np.float32) / 255.0
    meta = (ART / "UI_ChoiceCards.png.meta").read_text(encoding="utf-8")
    sprites = {}
    pattern = (r"      name: (\S+)\n      rect:\n        serializedVersion: 2\n        x: (\d+)\n        y: (\d+)\n"
               r"        width: (\d+)\n        height: (\d+)\n      alignment: \d+\n      pivot: \{x: ([\d.]+), y: ([\d.]+)\}\n"
               r"      border: \{x: (\d+), y: (\d+), z: (\d+), w: (\d+)\}\n")
    for m in re.finditer(pattern, meta):
        n, x, y, w, h = m.group(1), *map(int, m.group(2, 3, 4, 5))
        top = image.shape[0] - y - h
        rgba = image[top:top + h, x:x + w].copy()
        rgba[..., :3] *= rgba[..., 3:]  # premultiplied from here on
        sprites[n.split("_", 2)[-1]] = Sprite(n, rgba, tuple(map(int, m.group(8, 9, 10, 11))),
                                              (float(m.group(6)), float(m.group(7))))
    return sprites


SPRITES = load_sheet()
SET = (ART / "ChoiceCardArtSet.asset").read_text(encoding="utf-8")


def set_vec(field):
    m = re.search(rf"  {field}: \{{x: (-?[\d.]+), y: (-?[\d.]+), z: (-?[\d.]+), w: (-?[\d.]+)\}}", SET)
    return tuple(float(v) for v in m.groups())


def set_num(field):
    return float(re.search(rf"  {field}: (-?[\d.]+)", SET).group(1))


def set_colours():
    """Each treatment's press colour, in the set's order, and the shade of a waiting card."""
    found = [tuple(float(v) for v in m.groups()) for m in
             re.finditer(r"_pressColour: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)\}", SET)]
    names = ("Green", "Orange", "Red", "Gold", "Evolution", "Neutral", "Extract", "Climb")
    disabled = re.search(r"_disabledColour: \{r: ([\d.]+), g: ([\d.]+), b: ([\d.]+), a: ([\d.]+)\}", SET)
    return dict(zip(names, found)), tuple(float(v) for v in disabled.groups())


NAME_MARGIN = set_vec("_namePlateMargin")
DESC_MARGIN = set_vec("_descriptionMargin")
TITLE_MARGIN = set_vec("_titlePlateMargin")
WELL_MARGIN = set_num("_wellMargin")
SIGIL_SCALE = set_num("_sigilScale")
TITLE_RULE = set_num("_titleRuleHeight")
def treatment_blocks():
    """Each treatment's lines in the set, by name."""
    tiers = SET.split("  _tiers:\n")[1].split("  _evolution:\n")[0].split("  - ")[1:]
    blocks = dict(zip(("Green", "Orange", "Red", "Gold"), tiers))
    rest = SET.split("  _evolution:\n")[1]
    for name, field, after in (("Evolution", None, "  _neutral:\n"), ("Neutral", "  _neutral:\n", "  _extract:\n"),
                               ("Extract", "  _extract:\n", "  _climb:\n"), ("Climb", "  _climb:\n", None)):
        part = rest if field is None else rest.split(field)[1]
        blocks[name] = part.split(after)[0] if after else part
    return blocks


AURA_OUTSET, CRESTED = {}, set()
for key, block in treatment_blocks().items():
    m = re.search(r"_auraOutset: \{x: ([\d.]+), y: ([\d.]+), z: ([\d.]+), w: ([\d.]+)\}", block)
    if m and any(float(v) for v in m.groups()):
        AURA_OUTSET[key] = tuple(float(v) for v in m.groups())
    if not re.search(r"_crest: \{fileID: 0\}", block):
        CRESTED.add(key)
PRESS, DISABLED = set_colours()


# ----------------------------------------------------------------------------------------------------------------------
# A tiny uGUI.

resize, blit, to_canvas, to_image = hudreview.resize, hudreview.blit, hudreview.to_canvas, hudreview.to_image


def sliced(rgba, border, w, h, scale=1.0):
    """A Sliced image: corners kept, edges and centre stretched, as Unity cuts at the sprite's borders; scale is canvas
    units a texel (a seventh of the imported texels for the forged blank at its pixels-per-unit multiplier of 7)."""
    l, b, r, t = border
    sh, sw = rgba.shape[:2]
    w, h = int(round(w)), int(round(h))
    out = np.zeros((h, w, 4), np.float32)
    dl, dr, dt, db = (int(round(v * scale)) for v in (l, r, t, b))
    xs = [(0, l, 0, dl), (l, sw - r, dl, w - dr), (sw - r, sw, w - dr, w)]
    ys = [(0, t, 0, dt), (t, sh - b, dt, h - db), (sh - b, sh, h - db, h)]
    for sy0, sy1, dy0, dy1 in ys:
        for sx0, sx1, dx0, dx1 in xs:
            if sx1 <= sx0 or sy1 <= sy0 or dx1 <= dx0 or dy1 <= dy0:
                continue
            out[dy0:dy1, dx0:dx1] = resize(rgba[int(sy0):int(sy1), int(sx0):int(sx1)], dx1 - dx0, dy1 - dy0)
    return out


def draw(canvas, sprite, rect, kind="sliced", tint=(1, 1, 1, 1)):
    x, y, w, h = rect
    if w <= 0 or h <= 0:
        return
    image = sliced(sprite.rgba, sprite.border, w, h) if kind == "sliced" else resize(sprite.rgba, w, h)
    blit(canvas, image, x, y, tint)


def wrap(words, font, width, draw_):
    lines, line = [], ""
    for word in words.split(" "):
        trial = (line + " " + word).strip()
        if draw_.textlength(trial, font=font) <= width or not line:
            line = trial
        else:
            lines.append(line)
            line = word
    if line:
        lines.append(line)
    return lines


def label(canvas, words, rect, size_max, size_min, colour, bold=False, align="left"):
    """A uGUI Text with best fit: the largest size from size_max down to size_min whose wrapped lines fit the rect, set
    in the vertical middle of the rect."""
    if not words:
        return
    x, y, w, h = rect
    scratch = ImageDraw.Draw(Image.new("L", (1, 1)))
    for size in range(size_max, size_min - 1, -1):
        font = ImageFont.truetype(FONT_BOLD if bold else FONT, size)
        lines = wrap(words, font, w, scratch)
        line_h = size * 1.15
        if len(lines) * line_h <= h + 0.5:
            break
    layer = Image.new("L", (int(w) + 40, int(h) + 40))
    d = ImageDraw.Draw(layer)
    top = (h - len(lines) * line_h) / 2
    ascent = font.getmetrics()[0]
    for i, line in enumerate(lines):
        tw = d.textlength(line, font=font)
        tx = {"left": 0, "centre": (w - tw) / 2, "right": w - tw}[align]
        # Unity sets a line's ascent from its top; a line is about 1.15 of the size.
        ty = top + i * line_h + (line_h - size * 1.12) / 2 + size * 0.905 - ascent
        d.text((tx + 20, ty + 20), line, font=font, fill=255)
    a = np.asarray(layer, np.float32)[20:20 + int(h), 20:20 + int(w)] / 255.0
    rgba = np.zeros(a.shape + (4,), np.float32)
    rgba[..., :3] = np.asarray(colour, np.float32)[None, None] / 255.0 * a[..., None]
    rgba[..., 3] = a
    blit(canvas, rgba, x, y)


def icon_image(name, size):
    img = Image.open(UI / f"{name}.png").convert("RGBA").resize((int(round(size)), int(round(size))), Image.LANCZOS)
    rgba = np.asarray(img, np.float32) / 255.0
    rgba[..., :3] *= rgba[..., 3:]
    return rgba


FORGED = np.asarray(Image.open(UI / "UI_Panel_ForgedBlank_v2.png").convert("RGBA").resize((1024, 341), Image.LANCZOS),
                    np.float32) / 255.0
FORGED_BORDER = (128 * 1024 / 2172,) * 4  # the 128-texel border, in the imported texture's texels
FORGED_SCALE = (7 * 1024 / 2172) ** -1  # canvas units a texel: the import's 1024 and the multiplier of 7


# ----------------------------------------------------------------------------------------------------------------------
# The choice panel on the phone (screen rects: left, top, width, height).

SURFACE = (44.0, H - 0.42 * H - 430.0, 992.0, 860.0)
SURFACE_CENTRE_Y = H - 0.42 * H
TITLE = (96.0, SURFACE_CENTRE_Y - 322.0 - 63.0, 888.0, 126.0)
INK = (238, 232, 217)
RARITY = [(0x4C, 0xC3, 0x6A), (0xF0, 0x8A, 0x24), (0xE0, 0x39, 0x3E), (0xF5, 0xC5, 0x42)]
KIND_NEW, KIND_LEVEL, KIND_EVOLUTION = (255, 219, 115), (199, 214, 235), (255, 140, 56)
TITLES = {"upgrade": "Level up! Choose one upgrade", "forge": "The forge: choose one",
          "checkpoint": "Checkpoint: extract or climb?"}


class Card:
    def __init__(self, name, description, look, tier=None, kind=None, kind_colour=None, icon=None):
        self.name, self.description, self.look, self.tier = name, description, look, tier
        self.kind, self.kind_colour, self.icon = kind, kind_colour, icon


def layout(count):
    """Each card's rect and its parts' (card-local) as the scene authors two and LayOut stacks three."""
    if count <= 2:
        tops = [SURFACE_CENTRE_Y - 101 - 126, SURFACE_CENTRE_Y + 191 - 126]
        height, name_h, desc_top, desc_h, icon = 252.0, 76.0, 116.0, 108.0, 160.0
    else:
        top0 = SURFACE_CENTRE_Y - 227.0
        bottom = SURFACE_CENTRE_Y + 430.0 - 31.0
        height = min(252.0, (bottom - top0 - 28.0 * (count - 1)) / count)
        scale = height / 252.0
        footer = 28.0 * scale
        block = (height - 36.0 - footer) / 188.0
        name_h, desc_top, desc_h, icon = 76.0 * block, 36.0 + 80.0 * block, 108.0 * block, 160.0 * scale
        tops = [top0 + i * (height + 28.0) for i in range(count)]
    cards = []
    for top in tops[:count]:
        card = (92.0, top, 896.0, height)
        cards.append(dict(card=card, name=(216.0, 36.0, 624.0, name_h), desc=(216.0, desc_top, 624.0, desc_h),
                          icon=(34.0, (height - icon) / 2, icon, icon), kind=(488.0, 8.0, 380.0, 28.0)))
    return cards


def at(card, part):
    return (card[0] + part[0], card[1] + part[1], part[2], part[3])


def grow(rect, margin):
    x, y, w, h = rect
    l, b, r, t = margin
    return (x - l, y - t, w + l + r, h + b + t)


def card_labels(canvas, c, parts):
    name_colour = RARITY[c.tier] if c.tier is not None else INK
    label(canvas, c.name, at(parts["card"], parts["name"]), 38, 30, name_colour, bold=True)
    label(canvas, c.description, at(parts["card"], parts["desc"]), 30, 26, INK)
    if c.kind:
        label(canvas, c.kind, at(parts["card"], parts["kind"]), 24, 18, c.kind_colour, bold=True, align="right")


def before_card(canvas, c, parts):
    x, y, w, h = parts["card"]
    blit(canvas, sliced(FORGED, FORGED_BORDER, w, h, FORGED_SCALE), x, y)
    if c.icon:
        icon = at(parts["card"], parts["icon"])
        blit(canvas, icon_image(c.icon, icon[2]), icon[0], icon[1])
    card_labels(canvas, c, parts)


def after_card(canvas, c, parts, state="rest"):
    s = SPRITES
    card = parts["card"]
    draw(canvas, s[c.look + "Frame"], card)
    if c.look in AURA_OUTSET:
        draw(canvas, s[c.look + "Aura"], grow(card, AURA_OUTSET[c.look]))
    if state == "pressed":
        draw(canvas, s["Press"], card, tint=PRESS[c.look])
    elif state == "disabled":
        draw(canvas, s["Press"], card, tint=DISABLED)
    name = at(card, parts["name"])
    desc = at(card, parts["desc"])
    icon = at(card, parts["icon"])
    draw(canvas, s[c.look + "NamePlate"], grow(name, NAME_MARGIN))
    draw(canvas, s["Description"], grow(desc, DESC_MARGIN))
    draw(canvas, s[c.look + "Well"], grow(icon, (WELL_MARGIN,) * 4))
    if c.icon:
        blit(canvas, icon_image(c.icon, icon[2]), icon[0], icon[1])
    else:
        size = icon[2] * SIGIL_SCALE
        draw(canvas, s[c.look + "Sigil"], (icon[0] + (icon[2] - size) / 2, icon[1] + (icon[3] - size) / 2, size, size),
             kind="simple")
    draw(canvas, s["WellLip"], icon)
    if c.look in CRESTED:
        crest = s[c.look + "Crest"]
        cw, ch = crest.size
        # Its pivot on the middle of the card's top edge.
        draw(canvas, crest, (card[0] + card[2] / 2 - cw * crest.pivot[0], card[1] - ch * (1 - crest.pivot[1]), cw, ch),
             kind="simple")
    card_labels(canvas, c, parts)


def panel(capture, kind, cards, painted, states=None):
    canvas = to_canvas(capture)
    if painted:
        draw(canvas, SPRITES["Backdrop"], (0, 0, W, H), kind="simple")
        draw(canvas, SPRITES["Surface"], SURFACE)
        plate = grow(TITLE, TITLE_MARGIN)
        draw(canvas, SPRITES["TitlePlate"], plate)
        crest = SPRITES["TitleCrest"]
        cw, ch = crest.size
        draw(canvas, crest, (plate[0] + plate[2] / 2 - cw / 2, plate[1] + plate[3] - TITLE_RULE - ch / 2, cw, ch),
             kind="simple")
    else:
        dim = np.zeros((H, W, 4), np.float32)
        dim[..., :3] = np.array([0.02745098, 0.03529412, 0.07058824]) * 0.8627451
        dim[..., 3] = 0.8627451
        blit(canvas, dim, 0, 0)
        blit(canvas, sliced(FORGED, FORGED_BORDER, SURFACE[2], SURFACE[3], FORGED_SCALE), SURFACE[0], SURFACE[1])
    label(canvas, TITLES[kind], TITLE, 42, 32, INK, bold=True, align="centre")
    for i, (c, parts) in enumerate(zip(cards, layout(len(cards)))):
        if painted:
            after_card(canvas, c, parts, (states or {}).get(i, "rest"))
        else:
            before_card(canvas, c, parts)
    return canvas


# ----------------------------------------------------------------------------------------------------------------------
# The prompts shown.

LEVELUP = [Card("Precision", "+3% crit chance to every weapon", "Green", 0, "NEW TOME", KIND_NEW),
           Card("Staff", "+12.5% area", "Orange", 1, "Lv 2 -> 3", KIND_LEVEL),
           Card("Force", "+20% damage to every weapon", "Gold", 3, "NEW TOME", KIND_NEW)]
EVOLUTION = [Card("Daggers", "+15% attack speed", "Red", 2, "Lv 3 -> 4", KIND_LEVEL),
             Card("Cinder Nova", "Kills burst for 75% damage nearby, and a harder world", "Evolution", 2, "EVOLUTION",
                  KIND_EVOLUTION),
             Card("Vitality", "+20 maximum health", "Green", 0, "NEW TOME", KIND_NEW)]
CHECKPOINT = [Card("Extract", "Bank all 116 gold and end the run", "Extract"),
              Card("Climb", "Secure 116 gold. Quicksilver Vaults: +20% enemy damage, +50% gold", "Climb")]
FORGE = [Card("Mend", "Restore 40% of your health", "Neutral"),
         Card("Temper", "Gain 1 extra upgrade choice", "Neutral")]
PROMPTS = {"levelup": ("upgrade", LEVELUP), "evolution": ("upgrade", EVOLUTION), "checkpoint": ("checkpoint", CHECKPOINT),
           "forge": ("forge", FORGE)}
ICONS = {"levelup": "UI_Icon_CrucibleVent_v1", "evolution": "UI_Icon_ForgeBurst_v1",
         "checkpoint": "UI_Icon_ForgeBurst_v1", "forge": "UI_Icon_ForgeBurst_v1"}


def caption(image, text, at_, size=30):
    ImageDraw.Draw(image).text(at_, text, font=ImageFont.truetype(FONT, size), fill=(235, 228, 210))


def phone(name, path):
    capture = np.asarray(Image.open(path).convert("RGB"))
    under = (hudreview.to_image(hudreview.paint_hud(capture, boss=False, icon_name=ICONS[name])))
    base = np.asarray(under)
    kind, cards = PROMPTS[name]
    before = to_image(panel(base, kind, cards, painted=False))
    after = to_image(panel(base, kind, cards, painted=True))
    after.save(OUT / f"phone-{name}-after.png")
    pair = Image.new("RGB", (W * 2 + 60, H + 70), (24, 22, 28))
    pair.paste(before, (20, 60))
    pair.paste(after, (W + 40, 60))
    caption(pair, "before (no set: the forged blank panel, the tier on the name only)", (20, 14))
    caption(pair, "after (the painted cards and panel)", (W + 40, 14))
    pair.save(OUT / f"phone-{name}-before-after.png")
    return capture


def closeups(capture):
    """Every treatment at rest (well empty), with an icon, pressed and waiting, on the surface's face."""
    looks = [("Green", 0, "NEW TOME", "Precision", "+3% crit chance to every weapon"),
             ("Orange", 1, "Lv 2 -> 3", "Staff", "+12.5% area"),
             ("Red", 2, "Lv 3 -> 4", "Daggers", "+15% attack speed"),
             ("Gold", 3, "NEW TOME", "Force", "+20% damage to every weapon"),
             ("Evolution", 2, "EVOLUTION", "Cinder Nova", "Kills burst for 75% damage nearby, and a harder world"),
             ("Neutral", None, None, "Mend", "Restore 40% of your health"),
             ("Extract", None, None, "Extract", "Bank all 116 gold and end the run"),
             ("Climb", None, None, "Climb", "Secure 116 gold. Quicksilver Vaults: +20% enemy damage, +50% gold")]
    kinds = {"NEW TOME": KIND_NEW, "EVOLUTION": KIND_EVOLUTION}
    cell_w, cell_h = 960, 300
    columns = [("at rest, its well empty", "rest", False), ("an icon in its well", "rest", True),
               ("pressed", "pressed", False), ("waiting out the delay", "disabled", False)]
    sheet = Image.new("RGB", (cell_w * len(columns) + 40, cell_h * len(looks) + 90), (24, 22, 28))
    navy = np.zeros((cell_h, cell_w, 3), np.float32) + np.array([12, 18, 36], np.float32)
    for col, (title, state, with_icon) in enumerate(columns):
        caption(sheet, title, (20 + col * cell_w + 20, 20), 32)
        for row, (look, tier, kind, name, desc) in enumerate(looks):
            canvas = to_canvas(navy)
            c = Card(name, desc, look, tier, kind, kinds.get(kind, KIND_LEVEL),
                     "UI_Icon_TemperedEdge_v1" if with_icon else None)
            # At the height the game shows it: an upgrade offer's three stacked cards, the forge's and the checkpoint's
            # two as the scene authors them.
            parts = layout(3 if tier is not None else 2)[0]
            x0, y0 = 32.0, (cell_h - parts["card"][3]) / 2
            parts = dict(parts, card=(x0, y0, 896.0, parts["card"][3]))
            after_card(canvas, c, parts, state)
            sheet.paste(to_image(canvas), (20 + col * cell_w, 70 + row * cell_h))
    sheet.save(OUT / "tiers-closeup.png")
    # Enlarged: the first two columns, twice the phone's size, as a phone held close shows them.
    big = sheet.crop((20, 70, 20 + cell_w * 2, 70 + cell_h * len(looks)))
    big = big.resize((big.width * 2, big.height * 2), Image.LANCZOS)
    for i in range(0, len(looks), 4):
        part = big.crop((0, i * cell_h * 2, big.width, min(big.height, (i + 4) * cell_h * 2)))
        part.save(OUT / f"tiers-closeup-x2-{i // 4 + 1}.png")


def sprites_sheet():
    entries = list(SPRITES.items())
    width = 2400
    font = ImageFont.truetype(FONT, 20)
    scratch = ImageDraw.Draw(Image.new("L", (1, 1)))
    placed, x, y, row_h = [], 20, 20, 0
    for key, sprite in entries:
        factor = 2 if sprite.size[0] > 150 else 4 if sprite.size[0] > 60 else 6
        img_w, img_h = sprite.size[0] * factor, sprite.size[1] * factor
        tag = f"{key} {sprite.size[0]}x{sprite.size[1]} x{factor}" + (f"  border {sprite.border}" if any(sprite.border) else "")
        tag_w = scratch.textlength(tag, font=font)
        if x + max(img_w, tag_w) + 20 > width:
            x, y, row_h = 20, y + row_h + 60, 0
        placed.append((tag, sprite, factor, x, y))
        x += int(max(img_w, tag_w)) + 30
        row_h = max(row_h, img_h)
    height = y + row_h + 80
    checker = np.indices((height, width)).sum(0) // 12 % 2
    base = np.where(checker[..., None] == 1, np.array([70, 66, 78]), np.array([56, 52, 64])).astype(np.uint8)
    sheet = Image.fromarray(base)
    d = ImageDraw.Draw(sheet)
    for tag, sprite, factor, x, y in placed:
        rgba = sprite.rgba
        w_, h_ = rgba.shape[1] * factor, rgba.shape[0] * factor
        canvas = to_canvas(np.asarray(sheet.crop((x, y + 30, x + w_, y + 30 + h_))))
        blit(canvas, np.repeat(np.repeat(rgba, factor, 0), factor, 1), 0, 0)
        sheet.paste(to_image(canvas), (x, y + 30))
        l, b, r, t = sprite.border
        for bx in ([l * factor] if l else []) + ([w_ - r * factor] if r else []):
            d.line([(x + bx, y + 30), (x + bx, y + 30 + h_)], fill=(80, 255, 120), width=2)
        for by in ([t * factor] if t else []) + ([h_ - b * factor] if b else []):
            d.line([(x, y + 30 + by), (x + w_, y + 30 + by)], fill=(80, 255, 120), width=2)
        d.text((x, y + 4), tag, font=font, fill=(235, 228, 210))
    sheet.save(OUT / "sprites-and-borders.png")


def main():
    capture = None
    for name, path in CAPTURES.items():
        if path:
            capture = phone(name, path)
    closeups(capture)
    sprites_sheet()
    print("wrote", OUT)


if __name__ == "__main__":
    main()
