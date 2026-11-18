"""Review sheets for the six hero-skill icons on the ability button (docs/57 5.2), as the phone draws them.

    python -B Tools/ArtReview/review_skill_icons.py --out DIR [--icons DIR] [--floor PNG]

The shipped two (Forge Burst, Crucible Vent) and the four new ones (build_skill_icons.py; read from --icons, else from
Assets/_Project/Art/UI) are laid in the button as the Gameplay scene builds it: a navy disc, the icon square in a round
well 154/234 of the button (a Mask on the Knob sprite), the bezel over it. The bezel is UI_Bezel_Ability_v1.png, the
scene's own sprite; at run time HudArtView swaps in the painted HUD's ring (UI_Hud.png, UI_Hud_AbilityRing), so both are
shown. The icon is imported at 256 on Android (its .meta) and drawn bilinear with no mipmaps, so it is reduced to 256
first and then sampled straight down to the button's size, without filtering, as the GPU does.

Writes:
  - skill-icons-sheet.png: each hero's three icons as painted, then on both bezels at 1x of the S23's 1080 canvas
    (skill 1 at 234, skills 2-3 at 176, as docs/57 5.2 lays them) and at phone size (the well at 96 px, a 720-wide
    phone's skills 2-3), the six side by side at 96 and 64 for distinctness, and in grey for their values.
  - skill-icons-phone.png: each hero's three-button arc at docs/57 5.2's anchors over a floor capture, 1:1 for the
    S23 and reduced for a 720-wide phone.
"""
import argparse
import os

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
UI = os.path.join(ROOT, "Assets", "_Project", "Art", "UI")
FONT = "C:/Windows/Fonts/arialbd.ttf"
NAVY = (16, 20, 35)  # the button's Knob tint (0.063, 0.078, 0.137)
BACK = (24, 26, 34)
FLOOR_CAPTURE = os.path.join("ArtDirection", "2026-11-12", "foundry-court-ways-02", "after", "approach-0-0.png")


def default_floor():
    """The Foundry court capture of 2026-11-12, in this checkout or (from an agent's worktree) the main one above it."""
    here = ROOT
    while True:
        path = os.path.join(here, FLOOR_CAPTURE)
        if os.path.exists(path):
            return path
        up = os.path.dirname(here)
        if up == here:
            return None
        here = up

HEROES = (
    ("The Vanguard", (("Forge Burst", "ForgeBurst", True), ("Iron Resolve", "IronResolve", False),
                      ("Shield Rush", "ShieldRush", False))),
    ("Ilya, the Cinder Arcanist", (("Crucible Vent", "CrucibleVent", True), ("Flame Sigil", "FlameSigil", False),
                                   ("Cinder Ward", "CinderWard", False))),
)


def font(size):
    try:
        return ImageFont.truetype(FONT, size)
    except OSError:
        return ImageFont.load_default()


def load_icons(icon_dir):
    out = {}
    for _, row in HEROES:
        for title, key, shipped in row:
            folder = UI if shipped or not icon_dir else icon_dir
            out[key] = np.asarray(Image.open(os.path.join(folder, f"UI_Icon_{key}_v1.png")).convert("RGB"))
    return out


def hud_ring():
    sheet = Image.open(os.path.join(UI, "Hud", "UI_Hud.png")).convert("RGBA")
    h = sheet.height
    return np.asarray(sheet.crop((0, h - 106 - 234, 234, h - 106)))  # UI_Hud_AbilityRing: x 0, y 106, 234 square


def imported(rgb, size=256):
    """The import: the 1254 square reduced to the Android override's 256."""
    return np.asarray(Image.fromarray(rgb).resize((size, size), Image.LANCZOS))


def gpu(img, size):
    """Bilinear with no mipmaps: sampled straight down, no prefilter."""
    return cv2.resize(img, (size, size), interpolation=cv2.INTER_LINEAR)


def disc(size, d, aa=1.0):
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32) + 0.5
    r = np.hypot(xx - size / 2, yy - size / 2)
    return np.clip((d / 2 - r) / aa + 0.5, 0, 1)


def button(icon_rgb, bezel_rgba, size):
    """The ability button at `size` canvas pixels, as straight RGBA."""
    out = np.zeros((size, size, 4), np.float32)
    base = disc(size, size * 0.97)
    out[..., :3] = np.array(NAVY, np.float32)
    out[..., 3] = base * 255
    well = int(round(size * 154 / 234))
    icon = gpu(imported(icon_rgb), well).astype(np.float32)
    m = disc(well, well)[..., None]
    o = (size - well) // 2
    region = out[o:o + well, o:o + well]
    region[..., :3] = region[..., :3] * (1 - m) + icon * m
    region[..., 3] = np.maximum(region[..., 3], m[..., 0] * 255)
    bz = gpu(bezel_rgba, size).astype(np.float32)
    a = bz[..., 3:4] / 255.0
    out[..., :3] = out[..., :3] * (1 - a) + bz[..., :3] * a
    out[..., 3] = np.maximum(out[..., 3], bz[..., 3])
    return Image.fromarray(out.clip(0, 255).astype(np.uint8), "RGBA")


def label(draw, xy, text, size=18, fill=(225, 222, 214), anchor="la"):
    draw.text(xy, text, font=font(size), fill=fill, anchor=anchor)


def sheet(icons, bezels, out_path):
    W = 1800
    big = 300
    rows_h = 34 + 26 + 2 * 262 + 10 + 190 + 30
    H = 60 + rows_h * 2 + 360
    img = Image.new("RGBA", (W, H), BACK + (255,))
    d = ImageDraw.Draw(img)
    label(d, (24, 18), "Hero skills - the six ability icons (shipped two first in each row), as the phone draws them",
          22)
    y = 60
    for hero, row in HEROES:
        label(d, (24, y), hero, 22, (250, 210, 140))
        y += 34
        # As painted: the 1254 square, reduced.
        for i, (title, key, shipped) in enumerate(row):
            x = 24 + i * (big + 24)
            img.paste(Image.fromarray(icons[key]).resize((big, big), Image.LANCZOS), (x, y))
            label(d, (x, y + big + 6), title + (" (shipped)" if shipped else " (new)"), 18)
        # 1x on the S23 canvas, both bezels.
        x0 = 24 + 3 * (big + 24) + 10
        label(d, (x0, y), "1x on the S23 (skill 1 at 234, skills 2-3 at 176)", 16, (170, 180, 200))
        for b, (bname, bz) in enumerate(bezels):
            for i, (title, key, _) in enumerate(row):
                s = 234 if i == 0 else 176
                bx = x0 + i * 250 + (234 - s) // 2
                by = y + 48 + b * 262 + (234 - s) // 2
                img.alpha_composite(button(icons[key], bz, s), (bx, by))
            label(d, (x0, y + 26 + b * 262), bname, 15, (170, 180, 200))
        y += 26 + 2 * 262 + 10
        # Phone size: the well at 96 (the button at 146), and the 720-wide phone's skill 1 (156 / 0.667 = 104 well).
        label(d, (24, y), "phone size: the icon well at 96 px (button 146) - v1 bezel, then the HUD ring", 16,
              (170, 180, 200))
        for b, (bname, bz) in enumerate(bezels):
            for i, (title, key, _) in enumerate(row):
                img.alpha_composite(button(icons[key], bz, 146), (24 + b * 520 + i * 160, y + 24))
        y += 190 + 30
    # The six side by side at 96 and 64 (well size), unmasked and masked, and in grey.
    label(d, (24, y), "all six at a glance: the well at 96 and 64 px, colour and grey (values)", 16, (170, 180, 200))
    y += 26
    keys = [k for _, row in HEROES for _, k, _ in row]
    for j, s in enumerate((96, 64)):
        for i, k in enumerate(keys):
            small = gpu(imported(icons[k]), s)
            m = disc(s, s)[..., None]
            rgba = np.concatenate([small, (m * 255).astype(np.uint8)], -1)
            img.alpha_composite(Image.fromarray(rgba, "RGBA"), (24 + i * (s + 16), y + j * 110))
            grey = cv2.cvtColor(small, cv2.COLOR_RGB2GRAY)
            grey = np.stack([grey] * 3, -1)
            rgba = np.concatenate([grey, (m * 255).astype(np.uint8)], -1)
            img.alpha_composite(Image.fromarray(rgba, "RGBA"), (24 + 6 * (s + 16) + 40 + i * (s + 16), y + j * 110))
    img.convert("RGB").save(out_path)
    return out_path


def cluster(icons, row, bz, floor, scale):
    """The three-button arc at docs/57 5.2's anchors (bottom-right, pivot (1, 0)), on the bottom 640 canvas rows of a
    1080-wide screen, reduced by `scale`."""
    W, Hc = 1080, 640
    if floor is not None:
        f = floor.resize((1080, int(floor.height * 1080 / floor.width)), Image.LANCZOS)
        canvas = f.crop((0, f.height - Hc, 1080, f.height)).convert("RGBA")
    else:
        canvas = Image.new("RGBA", (W, Hc), (70, 66, 78, 255))
    slots = ((234, -40, 68), (176, -90, 341), (176, -306, 160))
    for (title, key, _), (s, ax, ay) in zip(row, slots):
        x = W + ax - s
        y = Hc - ay - s
        canvas.alpha_composite(button(icons[key], bz, s), (x, y))
    if scale != 1.0:
        canvas = canvas.resize((int(W * scale), int(Hc * scale)), Image.LANCZOS)
    return canvas


def phone(icons, bz, floor, out_path):
    a = [cluster(icons, row, bz, floor, 1.0) for _, row in HEROES]
    b = [cluster(icons, row, bz, floor, 720 / 1080) for _, row in HEROES]
    W = 1080 * 2 + 60
    H = 40 + 640 + 40 + int(640 * 720 / 1080) + 20
    img = Image.new("RGBA", (W, H), BACK + (255,))
    d = ImageDraw.Draw(img)
    label(d, (20, 10), "S23 (1080 canvas, 1:1): the Vanguard | Ilya - the HUD ring as at run time", 18)
    for i, c in enumerate(a):
        img.alpha_composite(c, (20 + i * (1080 + 20), 40))
    label(d, (20, 40 + 640 + 10), "a 720-wide phone (the same canvas reduced 2/3)", 18)
    for i, c in enumerate(b):
        img.alpha_composite(c, (20 + i * (1080 + 20), 40 + 640 + 40))
    img.convert("RGB").save(out_path)
    return out_path


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", required=True)
    ap.add_argument("--icons", default=None, help="read the four new icons here instead of Assets")
    ap.add_argument("--floor", default=None, help="a 1080-wide floor capture to lay the clusters on")
    args = ap.parse_args()
    os.makedirs(args.out, exist_ok=True)
    icons = load_icons(args.icons)
    v1 = np.asarray(Image.open(os.path.join(UI, "UI_Bezel_Ability_v1.png")).convert("RGBA"))
    v1 = np.asarray(Image.fromarray(v1).resize((256, 256), Image.LANCZOS))
    ring = hud_ring()
    floor_path = args.floor or default_floor()
    floor = Image.open(floor_path).convert("RGB") if floor_path and os.path.exists(floor_path) else None
    print(sheet(icons, (("UI_Bezel_Ability_v1", v1), ("HUD ring (run time)", ring)),
                os.path.join(args.out, "skill-icons-sheet.png")))
    print(phone(icons, ring, floor, os.path.join(args.out, "skill-icons-phone.png")))


if __name__ == "__main__":
    main()
