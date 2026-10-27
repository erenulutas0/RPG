"""Builds the Cinder Arcanist's Crucible Vent icon in the Forge Burst icon's style (docs/45 section 7, P6).

    python Tools/ArtReview/build_ability_icons.py [out_png] [--preview preview_png]

The Forge Burst icon (Assets/_Project/Art/UI/UI_Icon_ForgeBurst_v1.png) is a glowing chunky-pixel anvil with three
blades on a dark navy field, about 13 screen pixels to a cell. This draws a cracked crucible erupting three flame
tongues on the same grid, in the Arcanist's ember palette, over the Forge Burst icon's own field (its glyph painted
out), so the two ability buttons read as one family. Deterministic: same inputs, same bytes.
"""
import os
import sys

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
FORGE = os.path.join(ROOT, "Assets", "_Project", "Art", "UI", "UI_Icon_ForgeBurst_v1.png")
DEFAULT_OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "UI", "UI_Icon_CrucibleVent_v1.png")
SIZE = 1254
GRID = 96  # cells across; 1254 / 96 is the Forge Burst icon's ~13 px cell

# The ember palette (docs/45): from the darkest outline to the hottest highlight.
OUTLINE = (70, 6, 18)
RIM = (183, 62, 72)       # crimson #B73E48
DEEP = (214, 70, 26)
MID = (231, 114, 50)      # ember #E77232
LIGHT = (250, 176, 82)
HOT = (255, 226, 150)
WHITE = (255, 247, 228)
GLOW = np.array([231, 114, 50], dtype=np.float32)


def field():
    """The Forge Burst icon's navy field with its glyph and halo painted out."""
    forge = cv2.imread(FORGE, cv2.IMREAD_COLOR)
    hsv = cv2.cvtColor(forge, cv2.COLOR_BGR2HSV)
    # The field is dark navy; everything bright or saturated blue belongs to the glyph or its glow.
    mask = ((hsv[..., 2] > 70) | ((hsv[..., 1] > 150) & (hsv[..., 2] > 45))).astype(np.uint8) * 255
    mask = cv2.dilate(mask, np.ones((61, 61), np.uint8))
    clean = cv2.inpaint(forge, mask, 25, cv2.INPAINT_TELEA)
    clean = cv2.GaussianBlur(clean, (0, 0), 6)
    # Keep the field's own grain where it was never painted over.
    keep = cv2.GaussianBlur(255 - mask, (0, 0), 20).astype(np.float32)[..., None] / 255.0
    out = forge.astype(np.float32) * keep + clean.astype(np.float32) * (1 - keep)
    return cv2.cvtColor(out.clip(0, 255).astype(np.uint8), cv2.COLOR_BGR2RGB)


# The glyph is drawn in design cells and scaled about the icon's centre so it fills the frame as the anvil does (about
# 64 % of the width), not edge to edge.
SCALE = .86
CENTRE = (48, 50)


def T(x, y):
    return (round(CENTRE[0] + (x - CENTRE[0]) * SCALE), round(CENTRE[1] + (y - CENTRE[1]) * SCALE))


def glyph_cells():
    """The crucible and its flames on the cell grid: an RGBA image GRID x GRID."""
    img = Image.new("RGBA", (GRID, GRID), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx = GRID // 2

    def poly(points, fill):
        d.polygon([T(x, y) for x, y in points], fill=fill)

    def rect(x0, y0, x1, y1, fill):
        a, b = T(x0, y0)
        c, e = T(x1, y1)
        d.rectangle([a, b, c, e], fill=fill)

    # Three flame tongues, like the anvil's three blades: the middle one tallest, the sides leaning out.
    def tongue(base_x, base_y, tip_x, tip_y, half, fill):
        poly([(base_x - half, base_y), (base_x - half * .55, base_y - (base_y - tip_y) * .55),
              (tip_x, tip_y), (base_x + half * .55, base_y - (base_y - tip_y) * .55), (base_x + half, base_y)], fill)

    flames = Image.new("RGBA", (GRID, GRID), (0, 0, 0, 0))
    fd = ImageDraw.Draw(flames)

    def fpoly(points, fill):
        fd.polygon([T(x, y) for x, y in points], fill=fill)

    def ftongue(base_x, base_y, tip_x, tip_y, half, fill):
        fpoly([(base_x - half, base_y), (base_x - half * .6, base_y - (base_y - tip_y) * .5),
               (tip_x, tip_y), (base_x + half * .6, base_y - (base_y - tip_y) * .5), (base_x + half, base_y)], fill)

    mouth_y = 50
    for colour, shrink in ((MID, 0), (LIGHT, 2), (HOT, 4), (WHITE, 6)):
        ftongue(cx, mouth_y + 2, cx, 16 + shrink * 1.4, 8 - shrink, colour + (255,))
        ftongue(cx - 15, mouth_y + 2, cx - 24, 27 + shrink * 1.3, 6 - shrink * .8, colour + (255,))
        ftongue(cx + 15, mouth_y + 2, cx + 24, 27 + shrink * 1.3, 6 - shrink * .8, colour + (255,))

    # The crucible: a deep bowl narrowing to a foot, a thick lip, two lugs, a glowing crack.
    body = [(cx - 25, 52), (cx + 25, 52), (cx + 21, 70), (cx + 14, 79), (cx - 14, 79), (cx - 21, 70)]
    poly(body, MID + (255,))
    poly([(cx - 21, 54), (cx + 21, 54), (cx + 18, 68), (cx + 12, 76), (cx - 12, 76), (cx - 18, 68)], LIGHT + (255,))
    poly([(cx - 13, 56), (cx + 3, 56), (cx + 1, 70), (cx - 6, 74), (cx - 11, 68)], HOT + (255,))
    poly([(cx - 9, 57), (cx - 3, 57), (cx - 5, 67), (cx - 8, 66)], WHITE + (255,))
    # Lip and lugs; the mouth shows the molten metal: a dark band round a white-hot pool.
    rect(cx - 29, 46, cx + 29, 52, LIGHT + (255,))
    rect(cx - 29, 46, cx + 29, 47, HOT + (255,))
    rect(cx - 24, 48, cx + 24, 51, DEEP + (255,))
    rect(cx - 17, 49, cx + 17, 50, HOT + (255,))
    rect(cx - 9, 49, cx + 9, 50, WHITE + (255,))
    rect(cx - 34, 55, cx - 26, 60, MID + (255,))
    rect(cx + 26, 55, cx + 34, 60, MID + (255,))
    rect(cx - 33, 55, cx - 28, 56, LIGHT + (255,))
    rect(cx + 28, 55, cx + 33, 56, LIGHT + (255,))
    # Foot.
    rect(cx - 12, 79, cx + 12, 83, DEEP + (255,))
    rect(cx - 10, 79, cx + 10, 80, MID + (255,))
    # The crack, lit from inside: a zigzag of the hottest colour down the bowl's right side.
    crack = [T(cx + 10, 53), T(cx + 7, 59), T(cx + 12, 63), T(cx + 8, 70), T(cx + 10, 75)]
    d.line(crack, fill=WHITE + (255,), width=2)

    # Flames behind the lip: composite the crucible over them.
    combined = Image.alpha_composite(flames, img)
    return combined


def outline(cells):
    """A two-band outline around the glyph, like the anvil's: crimson inside, near-black outside."""
    a = np.array(cells)
    solid = a[..., 3] > 0
    k = np.ones((3, 3), np.uint8)
    ring1 = cv2.dilate(solid.astype(np.uint8), k) .astype(bool) & ~solid
    ring2 = cv2.dilate((solid | ring1).astype(np.uint8), k).astype(bool) & ~(solid | ring1)
    a[ring1] = RIM + (255,)
    a[ring2] = OUTLINE + (255,)
    return Image.fromarray(a)


def build(out_path, preview_path=None):
    base = field().astype(np.float32)
    cells = outline(glyph_cells())
    # Upscale the cells with hard edges, then soften by half a cell so the icon reads as the anvil's chunky pixels.
    glyph = cells.resize((SIZE, SIZE), Image.NEAREST)
    g = np.array(glyph).astype(np.float32)
    alpha = g[..., 3:4] / 255.0
    # Glow: the bright parts blurred wide and narrow, in ember, added under the glyph.
    bright = (g[..., :3].max(axis=2) > 200).astype(np.float32) * alpha[..., 0]
    wide = cv2.GaussianBlur(bright, (0, 0), 46)[..., None]
    near = cv2.GaussianBlur(bright, (0, 0), 14)[..., None]
    lit = base + GLOW * (wide * 1.15 + near * .55)
    lit = lit.clip(0, 255)
    out = lit * (1 - alpha) + g[..., :3] * alpha
    # The anvil's cells carry a faint inner bloom; a light blur on the glyph alone matches it.
    soft = cv2.GaussianBlur(out, (0, 0), 1.2)
    out = soft * alpha + out * (1 - alpha)
    image = Image.fromarray(out.clip(0, 255).astype(np.uint8), "RGB")
    image.save(out_path, optimize=True)
    if preview_path:
        forge = Image.open(FORGE).convert("RGB")
        pair = Image.new("RGB", (SIZE * 2 + 40, SIZE), (8, 8, 12))
        pair.paste(forge, (0, 0))
        pair.paste(image, (SIZE + 40, 0))
        small = pair.resize((pair.width // 6, pair.height // 6), Image.LANCZOS)
        big = pair.resize((pair.width // 3, pair.height // 3), Image.LANCZOS)
        sheet = Image.new("RGB", (big.width, big.height + small.height + 20), (8, 8, 12))
        sheet.paste(big, (0, 0))
        sheet.paste(small, (0, big.height + 20))
        sheet.save(preview_path)
    return out_path


if __name__ == "__main__":
    args = [a for a in sys.argv[1:]]
    preview = None
    if "--preview" in args:
        i = args.index("--preview")
        preview = args[i + 1]
        del args[i:i + 2]
    print(build(args[0] if args else DEFAULT_OUT, preview))
