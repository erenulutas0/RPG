"""Before/after review of the Cinder Saw's upgrade icons recoloured to its painted blade (docs/63 13.10).

    python -B Tools/ArtReview/review_saw_icons.py --before OLD_SHEET.png --out DIR [--capture OFFER.png]

Reads the Saw's four card icons and its socket icon off two sheets: OLD_SHEET (the upgrade icons' sheet before the
recolour, e.g. from `git show <rev>:Assets/_Project/Art/UI/UpgradeIcons/UI_UpgradeIcons.png`) and the project's own
Art/UI/UpgradeIcons/UI_UpgradeIcons.png, both cut at the project's meta's rects (the recolour moved none). The cards are
laid out by review_choice_cards.py as RunChoiceView and ChoiceCardPainter draw them on a 1080x2340 phone, so the icons
are at card scale: about 120 canvas units in an offer's three stacked cards.

Writes:
  - wells.png and wells-x2.png: each icon (a pair of columns, before and after) in the well of the card frames it can
    show in - green's common card (a new weapon's, its well's ground stone grey), gold's, the green card shaded while
    it waits out its delay, the forged blank the panel falls back to without the card set, orange's and red's - cropped
    to the card's icon end.
  - phone-offer.png: a three-card offer of the Saw, its weapon on a green NEW WEAPON card, its damage line on gold and
    its attack speed line on red, before and after.
  - socket.png: the socket icon in a loadout badge (LoadoutBadgesView: the weapon plate, the icon 56 square at 4, 6),
    before and after, at the phone's size and at three times; and on the capture's HUD with --capture.
"""
import argparse
import os
import re
import shutil
import sys
import tempfile

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
SHEET = os.path.join(ROOT, "Assets", "_Project", "Art", "UI", "UpgradeIcons", "UI_UpgradeIcons.png")
FONT = "C:/Windows/Fonts/arial.ttf"
IDS = ("weapon_cinder_saw", "line_cinder_saw_damage", "line_cinder_saw_area", "line_cinder_saw_attack_speed")
NAMES = {"weapon_cinder_saw": "weapon", "line_cinder_saw_damage": "damage line", "line_cinder_saw_area": "area line",
         "line_cinder_saw_attack_speed": "attack speed line"}

parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
parser.add_argument("--before", required=True, help="the upgrade icons' sheet before the recolour")
parser.add_argument("--out", required=True, help="the review folder")
parser.add_argument("--capture", help="a 1080x2340 capture with the HUD's loadout row (its second badge empty)")
ARGS = parser.parse_args()
os.makedirs(ARGS.out, exist_ok=True)

# review_choice_cards reads its own arguments when imported (and review_hud_art makes a folder under its --out): it is
# given a scratch folder of its own.
SCRATCH = tempfile.mkdtemp()
sys.argv = [sys.argv[0], "--out", SCRATCH]
sys.path.insert(0, HERE)
import review_choice_cards as rc  # noqa: E402


def cut(png):
    """id -> PIL RGBA image of each Saw sprite (cards, and the weapon's socket as '<id>_slot'), at the meta's rects."""
    sheet = Image.open(png).convert("RGBA")
    meta = open(SHEET + ".meta", encoding="utf-8").read()
    found = {}
    pattern = (r"      name: UI_UpgradeIcons_(\S+)\n      rect:\n        serializedVersion: 2\n        x: (\d+)\n"
               r"        y: (\d+)\n        width: (\d+)\n        height: (\d+)\n")
    for m in re.finditer(pattern, meta):
        if "cinder_saw" not in m.group(1):
            continue
        x, y, w, h = map(int, m.group(2, 3, 4, 5))
        top = sheet.height - y - h
        found[m.group(1)] = sheet.crop((x, top, x + w, top + h))
    return found


ICONS = {"before": cut(ARGS.before), "after": cut(SHEET)}
for when, icons in ICONS.items():
    for key, image in icons.items():
        rc.SET_ICONS[f"{when}_{key}"] = image


def caption(image, text, at, size=22, fill=(235, 228, 210)):
    ImageDraw.Draw(image).text(at, text, font=ImageFont.truetype(FONT, size), fill=fill)


# The frames, (label, look, tier, state, kind); "Blank" is the forged blank card the panel shows without the set.
FRAMES = [("green, a new weapon", "Green", 0, "rest", "NEW WEAPON"),
          ("gold", "Gold", 3, "rest", "Lv 1 -> 2"),
          ("green, shaded while it waits", "Green", 0, "disabled", "NEW WEAPON"),
          ("forged blank (no card set)", "Blank", None, "rest", None),
          ("orange", "Orange", 1, "rest", "Lv 1 -> 2"),
          ("red", "Red", 2, "rest", "Lv 1 -> 2")]


def one_card(look, tier, state, kind, icon, name="Cinder Saw", desc="+20% damage", height=None):
    """A card at the three-card offer's height on the surface's navy, its canvas and its parts."""
    parts = rc.layout(3)[0]
    h = parts["card"][3] if height is None else height
    canvas = rc.to_canvas(np.zeros((int(h) + 24, 896 + 24, 3), np.float32) + np.array([12, 18, 36], np.float32))
    parts = dict(parts, card=(12.0, 12.0, 896.0, h))
    colour = rc.KIND_NEW if kind == "NEW WEAPON" else rc.KIND_LEVEL
    card = rc.Card(name, desc, look if look != "Blank" else "Green", tier, kind, colour, icon)
    if look == "Blank":
        rc.before_card(canvas, card, parts)
    else:
        rc.after_card(canvas, card, parts, state)
    return canvas


def wells():
    cell_w, cell_h = 236, 214
    left = 270
    sheet = Image.new("RGB", (left + cell_w * 2 * len(IDS) + 20, 90 + cell_h * len(FRAMES) + 20), (24, 22, 28))
    for i, key in enumerate(IDS):
        x = left + i * 2 * cell_w
        caption(sheet, NAMES[key], (x + 10, 16), 26)
        caption(sheet, "before", (x + 10, 52))
        caption(sheet, "after", (x + cell_w + 10, 52))
    for row, (label, look, tier, state, kind) in enumerate(FRAMES):
        y = 90 + row * cell_h
        caption(sheet, label, (16, y + cell_h // 2 - 14), 22)
        for i, key in enumerate(IDS):
            for j, when in enumerate(("before", "after")):
                canvas = one_card(look, tier, state, kind, f"set:{when}_{key}", name="", desc="")
                sheet.paste(rc.to_image(canvas).crop((0, 0, cell_w, cell_h)), (left + (i * 2 + j) * cell_w, y))
    sheet.save(os.path.join(ARGS.out, "wells.png"))
    sheet.resize((sheet.width * 2, sheet.height * 2), Image.LANCZOS).save(os.path.join(ARGS.out, "wells-x2.png"))


def phone_offer():
    field = np.zeros((rc.H, rc.W, 3), np.uint8) + np.array(rc.FIELD, np.uint8)
    pair = Image.new("RGB", (rc.W * 2 + 60, rc.H + 70), (24, 22, 28))
    for j, when in enumerate(("before", "after")):
        cards = [rc.Card("Cinder Saw", "8 damage every 0.2s to every enemy close around the hero", "Green", 0,
                         "NEW WEAPON", rc.KIND_NEW, f"set:{when}_weapon_cinder_saw"),
                 rc.Card("Cinder Saw", "+20% damage", "Gold", 3, "Lv 1 -> 2", rc.KIND_LEVEL,
                         f"set:{when}_line_cinder_saw_damage"),
                 rc.Card("Cinder Saw", "+15% attack speed", "Red", 2, "Lv 2 -> 3", rc.KIND_LEVEL,
                         f"set:{when}_line_cinder_saw_attack_speed")]
        pair.paste(rc.to_image(rc.panel(field, "upgrade", cards, painted=True)), (20 + j * (rc.W + 20), 60))
        caption(pair, when, (20 + j * (rc.W + 20), 14), 30)
    pair.save(os.path.join(ARGS.out, "phone-offer.png"))


def socket():
    """The loadout badge (90x72) on the HUD's dark, its weapon plate, the icon at (4, 6) 56 square."""
    plate = (0.16, 0.11, 0.08, 0.86)
    badges = []
    for when in ("before", "after"):
        canvas = rc.to_canvas(np.zeros((88, 106, 3), np.float32) + np.array([14, 16, 26], np.float32))
        box = np.zeros((72, 90, 4), np.float32)
        box[..., :3] = np.asarray(plate[:3], np.float32) * plate[3]
        box[..., 3] = plate[3]
        rc.blit(canvas, box, 8, 8)
        icon = np.asarray(ICONS[when]["weapon_cinder_saw_slot"], np.float32) / 255.0
        icon[..., :3] *= icon[..., 3:]
        rc.blit(canvas, icon, 8 + 4, 8 + 6)
        badges.append(rc.to_image(canvas))
    w, h = badges[0].size
    sheet = Image.new("RGB", (40 + (w + 20) * 2 + (w * 3 + 20) * 2, 60 + h * 3 + 20), (24, 22, 28))
    for j, when in enumerate(("before", "after")):
        sheet.paste(badges[j], (20 + j * (w + 20), 60))
        caption(sheet, when, (20 + j * (w + 20), 20))
        x = 40 + (w + 20) * 2 + j * (w * 3 + 20)
        sheet.paste(badges[j].resize((w * 3, h * 3), Image.NEAREST), (x, 60))
        caption(sheet, when + " x3", (x, 20))
    sheet.save(os.path.join(ARGS.out, "socket.png"))
    if ARGS.capture:
        shot = np.asarray(Image.open(ARGS.capture).convert("RGB"))
        crops = []
        for when in ("before", "after"):
            canvas = rc.to_canvas(shot)
            icon = np.asarray(ICONS[when]["weapon_cinder_saw_slot"], np.float32) / 255.0
            icon[..., :3] *= icon[..., 3:]
            # The second weapon badge (review_hud_art.BADGES[1]), empty in the capture.
            rc.blit(canvas, icon, 148 + 4, 158 + 6)
            crops.append(rc.to_image(canvas).crop((24, 8, 664, 252)))
        cw, ch = crops[0].size
        hud = Image.new("RGB", (cw * 2 + 60, ch * 2 + 90), (24, 22, 28))
        for j, when in enumerate(("before", "after")):
            hud.paste(crops[j].resize((cw * 2, ch * 2), Image.LANCZOS).crop((0, 0, cw, ch * 2)), (20 + j * (cw + 20), 60))
            caption(hud, when + " (x2, the second badge)", (20 + j * (cw + 20), 20))
        hud.save(os.path.join(ARGS.out, "socket-hud.png"))


wells()
phone_offer()
socket()
shutil.rmtree(SCRATCH, ignore_errors=True)
print("wrote", ", ".join(sorted(os.listdir(ARGS.out))))
