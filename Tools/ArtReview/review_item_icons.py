"""Review sheets for the gear's item icons (build_upgrade_icons.py, ITEMS).

    python -B Tools/ArtReview/review_item_icons.py --out DIR [--from-sheet]

Paints the nine item icons (or, with --from-sheet, reads them back from Art/UI/ItemIcons/UI_ItemIcons.png as the
project holds them) and lays each on the bag's four rarity grounds as AnvilView does: a bag place is the tier's frame
(Sliced at a pixels-per-unit multiplier of 2), its ground 16 units in from the place's edge and the icon 22 in, drawn
with bilinear filtering and no mipmaps as the phone draws them. The grounds and frames are read from the choice cards'
sheet (Art/UI/ChoiceCards): Common the green tier's, Rare the orange's (its ground blue), Epic the red's, Gold the gold's.

Writes:
  - item-icons-100.png: each item (a row) on the four grounds with its icon at 100, the bag's size, and beside them the
    upgrade card icon the bag borrowed for it until now, on the Common ground.
  - item-icons-236.png: the same at 236, to read their painting.
  - bag-mock.png: a full bag of thirty, the items in mixed rarities, at the bag's size on the Anvil's navy.
"""
import argparse
import os
import re
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_upgrade_icons as icons  # noqa: E402

ROOT = icons.ROOT
CARDS = os.path.join(ROOT, "Assets", "_Project", "Art", "UI", "ChoiceCards")
UPGRADES = os.path.join(ROOT, "Assets", "_Project", "Art", "UI", "UpgradeIcons")
ITEMS_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "UI", "ItemIcons")
FONT = "C:/Windows/Fonts/arialbd.ttf"
BACK = np.array([16, 21, 40], np.float32) / 255.0  # the Anvil's surface behind the places

TIERS = (("Common", "Green"), ("Rare", "Orange"), ("Epic", "Red"), ("Gold", "Gold"))
NAMES = {
    "item_foundry_mail": ("Foundry Mail", "upgrade_armor"), "item_ember_locket": ("Ember Locket", "upgrade_attack_speed"),
    "item_cinder_ring": ("Cinder Ring", "upgrade_crit"), "item_quicksilver_hauberk": ("Quicksilver Hauberk", "upgrade_armor"),
    "item_quicksilver_locket": ("Quicksilver Locket", "upgrade_attack_speed"), "item_mirror_ring": ("Mirror Ring", "upgrade_crit"),
    "item_bark_mail": ("Bark Mail", "upgrade_armor"), "item_spore_locket": ("Spore Locket", "upgrade_attack_speed"),
    "item_gloam_signet": ("Gloam Signet", "upgrade_crit"),
}


def sheet_sprites(png, meta_path):
    """name -> straight RGBA float array of every sprite a Multiple sheet's meta cuts, with its 9-slice border."""
    image = np.asarray(Image.open(png).convert("RGBA")).astype(np.float32) / 255.0
    meta = open(meta_path, encoding="utf-8").read()
    pattern = (r"      name: (\S+)\n      rect:\n        serializedVersion: 2\n        x: (\d+)\n        y: (\d+)\n"
               r"        width: (\d+)\n        height: (\d+)\n      alignment: \d+\n      pivot: \{x: [\d.]+, y: [\d.]+\}\n"
               r"      border: \{x: (\d+), y: (\d+), z: (\d+), w: (\d+)\}\n")
    out = {}
    for m in re.finditer(pattern, meta):
        name, x, y, w, h = m.group(1), *map(int, m.group(2, 3, 4, 5))
        top = image.shape[0] - y - h
        out[name] = (image[top:top + h, x:x + w].copy(), tuple(map(int, m.group(6, 7, 8, 9))))
    return out


def sample(rgba, w, h):
    """The sprite drawn into w x h as the GPU draws it: bilinear, no mipmaps (premultiplied while filtering)."""
    sh, sw = rgba.shape[:2]
    pre = rgba.copy()
    pre[..., :3] *= pre[..., 3:]
    ys = (np.arange(h) + 0.5) * sh / h - 0.5
    xs = (np.arange(w) + 0.5) * sw / w - 0.5
    yy, xx = np.meshgrid(ys, xs, indexing="ij")
    out = np.stack([ndimage.map_coordinates(pre[..., c], [yy, xx], order=1, mode="nearest") for c in range(4)], -1)
    return out


def sliced(rgba, border, w, h, multiplier=1.0):
    """A Sliced image at a pixels-per-unit multiplier: corners kept at their size over the multiplier, the rest stretched."""
    left, bottom, right, top = border
    sh, sw = rgba.shape[:2]
    out = np.zeros((h, w, 4), np.float32)
    dl, dr, dt, db = (int(round(v / multiplier)) for v in (left, right, top, bottom))
    xs = [(0, left, 0, dl), (left, sw - right, dl, w - dr), (sw - right, sw, w - dr, w)]
    ys = [(0, top, 0, dt), (top, sh - bottom, dt, h - db), (sh - bottom, sh, h - db, h)]
    for sy0, sy1, dy0, dy1 in ys:
        for sx0, sx1, dx0, dx1 in xs:
            if sx1 > sx0 and sy1 > sy0 and dx1 > dx0 and dy1 > dy0:
                out[dy0:dy1, dx0:dx1] = sample(rgba[sy0:sy1, sx0:sx1], dx1 - dx0, dy1 - dy0)
    return out


def over(canvas, pre, x, y):
    """Lays a premultiplied image on the canvas at (x, y)."""
    h, w = pre.shape[:2]
    region = canvas[y:y + h, x:x + w]
    region[:] = pre[..., :3] + region * (1 - pre[..., 3:])


def place(canvas, x, y, icon_size, tier, icon, cards):
    """A bag place (AnvilView.MakeTile and PaintTile) with its icon at icon_size: the place is the icon plus 22 a side."""
    size = icon_size + 44
    frame, border = cards[f"UI_ChoiceCards_{tier}Frame"]
    ground, _ = cards[f"UI_ChoiceCards_{tier}Ground"]
    over(canvas, sliced(frame, border, size, size, 2.0), x, y)
    over(canvas, sample(ground, size - 32, size - 32), x + 16, y + 16)
    if icon is not None:
        over(canvas, sample(icon, icon_size, icon_size), x + 22, y + 22)
    return size


def text(canvas, words, x, y, size, colour=(235, 228, 214)):
    img = Image.fromarray((np.clip(canvas, 0, 1) * 255 + 0.5).astype(np.uint8))
    ImageDraw.Draw(img).text((x, y), words, font=ImageFont.truetype(FONT, size), fill=colour)
    canvas[:] = np.asarray(img).astype(np.float32) / 255.0


def item_icons(from_sheet):
    if from_sheet:
        sprites = sheet_sprites(os.path.join(ITEMS_DIR, "UI_ItemIcons.png"), os.path.join(ITEMS_DIR, "UI_ItemIcons.png.meta"))
        return {k: sprites["UI_ItemIcons_" + k][0] for k in NAMES}
    return {k: v["card"] for k, v in icons.build_item_icons().items()}


def contact(items, before, cards, icon_size, with_before, path):
    label_w = 330 if icon_size < 200 else 400
    tile = icon_size + 44
    gap = 14
    cols = len(TIERS) + (1 if with_before else 0)
    width = label_w + cols * (tile + gap) + gap
    height = 70 + len(items) * (tile + gap) + gap
    canvas = np.zeros((height, width, 3), np.float32) + BACK
    head = 30 if icon_size < 200 else 40
    for c, (rarity, _) in enumerate(TIERS):
        text(canvas, rarity, label_w + c * (tile + gap) + 10, 18, head)
    if with_before:
        text(canvas, "Before", label_w + len(TIERS) * (tile + gap) + 10, 18, head, (150, 156, 176))
    for r, (key, icon) in enumerate(items.items()):
        y = 70 + r * (tile + gap)
        name, borrowed = NAMES[key]
        text(canvas, name, 16, y + tile // 2 - head // 2 - 4, head)
        for c, (_, tier) in enumerate(TIERS):
            place(canvas, label_w + c * (tile + gap), y, icon_size, tier, icon, cards)
        if with_before:
            place(canvas, label_w + len(TIERS) * (tile + gap), y, icon_size, "Green", before[borrowed], cards)
    Image.fromarray((np.clip(canvas, 0, 1) * 255 + 0.5).astype(np.uint8)).save(path)


def bag(items, cards, path):
    """Thirty places six across, as the bag lays them out (146 units a place, 10 apart), the items in mixed rarities."""
    keys = list(items)
    tile, gap = 146, 10
    width = 6 * tile + 5 * gap + 64
    height = 5 * tile + 4 * gap + 64
    canvas = np.zeros((height, width, 3), np.float32) + BACK
    for i in range(30):
        key = keys[(i * 4) % len(keys)]
        tier = TIERS[(i + i // 6) % 4][1]
        r, c = divmod(i, 6)
        place(canvas, 32 + c * (tile + gap), 32 + r * (tile + gap), tile - 44, tier, items[key], cards)
    Image.fromarray((np.clip(canvas, 0, 1) * 255 + 0.5).astype(np.uint8)).save(path)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--out", required=True)
    parser.add_argument("--from-sheet", action="store_true", help="read the icons back from the project's item sheet")
    args = parser.parse_args()
    os.makedirs(args.out, exist_ok=True)
    cards = sheet_sprites(os.path.join(CARDS, "UI_ChoiceCards.png"), os.path.join(CARDS, "UI_ChoiceCards.png.meta"))
    upgrades = sheet_sprites(os.path.join(UPGRADES, "UI_UpgradeIcons.png"), os.path.join(UPGRADES, "UI_UpgradeIcons.png.meta"))
    before = {k: upgrades["UI_UpgradeIcons_" + k][0] for k in ("upgrade_armor", "upgrade_attack_speed", "upgrade_crit")}
    items = item_icons(args.from_sheet)
    contact(items, before, cards, 100, True, os.path.join(args.out, "item-icons-100.png"))
    contact(items, before, cards, 236, False, os.path.join(args.out, "item-icons-236.png"))
    bag(items, cards, os.path.join(args.out, "bag-mock.png"))
    print("wrote", args.out)


if __name__ == "__main__":
    main()
