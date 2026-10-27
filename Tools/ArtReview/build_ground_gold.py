"""Builds the gold-only ground reward from the painted experience-and-gold pile (Reward_ExperienceGold.tga).

    python Tools/ArtReview/build_ground_gold.py [out_tga] [--preview preview_png]

A drop of gold alone (an overrun kill pays no experience) used to lie on the floor as the pixel coin, beside the painted
crystal piles. Its coins are the pile's own two painted coins: cut out of the pile by colour (gold, and the dark outline
that rings it), the crystal behind them left out, an outline painted where a coin met the crystal, and pinned at the
coins' base. Deterministic: same input, same bytes.
"""
import os
import sys

import cv2
import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FOLDER = os.path.join(ROOT, "Assets", "_Project", "Art", "Props", "GroundRewards")
PILE = os.path.join(FOLDER, "Reward_ExperienceGold.tga")
DEFAULT_OUT = os.path.join(FOLDER, "Reward_Gold.tga")
# The pile's darkest outline, sampled from its rim.
OUTLINE = (34, 20, 22)


def coins():
    """The pile's coins on a clear canvas the pile's size, and where they touch the floor (x, y from the bottom)."""
    pile = np.array(Image.open(PILE).convert("RGBA"))
    rgb, alpha = pile[..., :3], pile[..., 3]
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV).astype(int)
    hue, sat, val = hsv[..., 0], hsv[..., 1], hsv[..., 2]
    solid = alpha > 0
    blue = (hue >= 85) & (hue <= 135) & (sat > 50) & solid
    gold = (hue >= 8) & (hue <= 35) & (sat > 70) & (val > 50) & solid
    # Close the emblems and the shading bands inside each coin, then take the dark rim round them, never the crystal.
    body = cv2.morphologyEx(gold.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8)).astype(bool) & solid
    rim = cv2.dilate(body.astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool) & solid & ~blue & (val < 110)
    keep = body | rim
    out = np.zeros_like(pile)
    out[keep] = pile[keep]
    # Where a coin sat on the crystal it had no outline of its own: paint one there.
    ring = cv2.dilate(keep.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool) & ~keep & blue
    out[ring, :3] = OUTLINE
    out[ring, 3] = 255
    ys, xs = np.nonzero(out[..., 3] > 0)
    base_x = (xs.min() + xs.max() + 1) / 2
    base_y = out.shape[0] - (ys.max() + 1) + 3  # a few texels up the coins' lowest edge, as the pile's pin sits
    return out, base_x, base_y


def build(out_path, preview_path=None):
    image, base_x, base_y = coins()
    Image.fromarray(image, "RGBA").save(out_path)
    if preview_path:
        pile = Image.open(PILE).convert("RGBA")
        sheet = Image.new("RGBA", (pile.width * 2 + 8, pile.height), (40, 36, 44, 255))
        sheet.alpha_composite(pile, (0, 0))
        sheet.alpha_composite(Image.fromarray(image, "RGBA"), (pile.width + 8, 0))
        sheet.convert("RGB").resize((sheet.width * 3, sheet.height * 3), Image.NEAREST).save(preview_path)
    return base_x / image.shape[1], base_y / image.shape[0]


if __name__ == "__main__":
    args = sys.argv[1:]
    preview = None
    if "--preview" in args:
        i = args.index("--preview")
        preview = args[i + 1]
        del args[i:i + 2]
    pivot = build(args[0] if args else DEFAULT_OUT, preview)
    print(f"pivot {pivot[0]:.6f} {pivot[1]:.6f}")
