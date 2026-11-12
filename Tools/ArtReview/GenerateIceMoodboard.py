"""Ice-map (floor 2) mood board: palette swatches, the ice paving and masonry,
a backdrop silhouette (frozen spires + aurora) and a readability strip with the
Vanguard's blue (sampled from its sprite) on the floor values.
Usage: python Tools/ArtReview/GenerateIceMoodboard.py [out.png]
Default out: <main checkout>/ArtDirection/2026-11-12/ice-map-01/concept/moodboard.png
See docs/49_ICE_MAP_ART_DIRECTION.md.
"""
import os
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ICE = os.path.join(ROOT, "Assets", "_Project", "Art", "Environment", "Ice")
VAN = os.path.join(ROOT, "Assets", "_Project", "Art", "Characters", "Vanguard", "CHR_Vanguard_ContactA.tga")

PALETTE = [
    ("Floor slate deep", "1C2129"), ("Floor slate", "3B4350"), ("Floor slate light", "67717F"),
    ("Wall stone", "2A3140"), ("Wall cap / rime", "8C9AAB"), ("Black iron", "1E242C"),
    ("Mirror-ice deep", "1F4E66"), ("Mirror-ice", "5FA8C8"), ("Ice edge", "BFE6F2"),
    ("Frost", "B9CCDA"), ("Snow", "E4ECF2"), ("Silver (was brass)", "A9B4BF"),
    ("Quicksilver glow (was lava)", "7FE3FF"), ("Frost-light", "A6F0FF"), ("Violet shade", "4A3E6E"),
    ("Backdrop sky", "0D1424"), ("Aurora teal", "3FD9B0"), ("Aurora violet", "8B6CE8"),
    ("Haze", "6E8299"), ("Telegraph (keep)", "FF5A3C"),
]


def rgb(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def font(size):
    for f in ("segoeui.ttf", "arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(f, size)
        except OSError:
            pass
    return ImageFont.load_default()


def vanguard_blue():
    try:
        a = np.asarray(Image.open(VAN).convert("RGBA")).reshape(-1, 4).astype(np.float32)
        a = a[a[:, 3] > 200][:, :3]
        mx, mn = a.max(1), a.min(1)
        sat = (mx - mn) / (mx + 1e-6)
        blue = a[(sat > 0.45) & (a[:, 2] > a[:, 0] + 30) & (mx > 80)]
        if len(blue) > 50:
            return tuple(int(v) for v in np.median(blue, 0))
    except OSError:
        pass
    return (40, 110, 230)


def backdrop(w, h, rng):
    y = np.linspace(0, 1, h)[:, None]
    x = np.linspace(0, 1, w)[None, :]
    sky = np.array(rgb("0D1424"), np.float32) * (1 - y[..., None] * 0.2) + np.array(rgb("1C2A45"), np.float32) * (y[..., None] * 0.6)
    band = 0.32 + 0.08 * np.sin(x * 9 + 1.2) + 0.04 * np.sin(x * 23)
    d = np.exp(-((y - band) / 0.07) ** 2) + 0.5 * np.exp(-np.maximum(y - band, 0) / 0.12) * (y > band)
    curtain = (0.6 + 0.4 * np.sin(x * 38 + np.sin(x * 7) * 3)) * d
    col = np.array(rgb("3FD9B0"), np.float32) * (1 - x[..., None]) + np.array(rgb("8B6CE8"), np.float32) * x[..., None]
    img = sky + col * (curtain[..., None] * 0.75)
    img = np.clip(img, 0, 255).astype(np.uint8)
    im = Image.fromarray(img, "RGB")
    dr = ImageDraw.Draw(im)
    for layer, (c, base, hmax) in enumerate([("2A3850", 0.62, 0.45), ("111722", 0.80, 0.55)]):
        xs = 0
        while xs < w:
            sw = rng.integers(14, 46)
            sh = int(h * hmax * rng.uniform(0.3, 1.0))
            yb = int(h * base)
            dr.polygon([(xs, yb), (xs + sw // 2, yb - sh), (xs + sw, yb)], fill=rgb(c))
            xs += int(sw * rng.uniform(0.5, 1.1))
        dr.rectangle([0, int(h * base), w, h], fill=rgb(c))
    return im


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
        "E:/MobileGame", "ArtDirection", "2026-11-12", "ice-map-01", "concept", "moodboard.png")
    W, H = 1600, 1000
    rng = np.random.default_rng(49)
    im = Image.new("RGB", (W, H), rgb("10141B"))
    dr = ImageDraw.Draw(im)
    dr.text((24, 14), "Cryptforge F2 - Frozen Quicksilver Vaults - mood board v1 (docs/49)", fill=rgb("E4ECF2"), font=font(26))

    # Palette, 4 columns x 5 rows on the left.
    sw, sh, x0, y0 = 150, 74, 24, 60
    for i, (name, hx) in enumerate(PALETTE):
        cx, cy = x0 + (i % 4) * (sw + 10), y0 + (i // 4) * (sh + 34)
        dr.rectangle([cx, cy, cx + sw, cy + sh], fill=rgb(hx), outline=rgb("2E3644"))
        dr.text((cx, cy + sh + 2), name, fill=rgb("C8D2DC"), font=font(13))
        dr.text((cx, cy + sh + 16), "#" + hx, fill=rgb("8C9AAB"), font=font(13))

    # Ice paving and masonry crops (right column).
    px = 700
    for j, (fn, label) in enumerate([("IcePavingV1.png", "IcePavingV1 (re-tinted FoundryPavingV10 + frost)"),
                                     ("IceMasonryV1.png", "IceMasonryV1 (re-tinted FoundryMasonryV4 + frost)")]):
        try:
            tex = Image.open(os.path.join(ICE, fn)).convert("RGB").resize((420, 420), Image.LANCZOS)
            im.paste(tex, (px + j * 445, 60))
        except OSError:
            pass
        dr.text((px + j * 445, 484), label, fill=rgb("C8D2DC"), font=font(13))

    # Backdrop silhouette.
    bd = backdrop(890, 300, rng)
    im.paste(bd, (24, 600))
    dr.text((24, 906), "Backdrop: two layers of frozen spires under an aurora band (teal to violet)", fill=rgb("C8D2DC"), font=font(14))

    # Readability: the Vanguard's blue and enemy/telegraph swatches on the floor values.
    vb = vanguard_blue()
    rx, ry = 940, 530
    dr.text((rx, ry), "Readability: hero blue %s, enemies, telegraph on floor values" % ("#%02X%02X%02X" % vb), fill=rgb("E4ECF2"), font=font(15))
    floors = [("1C2129", "deep"), ("3B4350", "slate"), ("67717F", "light"), ("B9CCDA", "frost - avoid")]
    for k, (hx, nm) in enumerate(floors):
        bx, by = rx + k * 160, ry + 30
        dr.rectangle([bx, by, bx + 150, by + 330], fill=rgb(hx))
        dr.ellipse([bx + 45, by + 30, bx + 105, by + 120], fill=vb, outline=rgb("BFE6F2"), width=3)
        dr.ellipse([bx + 20, by + 150, bx + 60, by + 190], fill=rgb("C9D6E2"), outline=rgb("0A0D12"), width=3)
        dr.ellipse([bx + 90, by + 150, bx + 130, by + 190], fill=rgb("8B6CE8"), outline=rgb("0A0D12"), width=3)
        dr.ellipse([bx + 25, by + 215, bx + 125, by + 265], outline=rgb("FF5A3C"), width=5)
        dr.text((bx + 52, by + 280), "123", fill=rgb("FFFFFF"), font=font(22), stroke_width=2, stroke_fill=rgb("0A0D12"))
        dr.text((bx + 4, by + 312), "#%s %s" % (hx, nm), fill=rgb("0A0D12") if k == 3 else rgb("E4ECF2"), font=font(12))
    dr.text((rx, ry + 372), "hero (blue, ice-edge rim)  |  Mirror Sentry (silver) / Silver Wisp (violet)  |  slam ring (red-orange)  |  damage number",
            fill=rgb("8C9AAB"), font=font(12))

    os.makedirs(os.path.dirname(out), exist_ok=True)
    im.save(out, optimize=True)
    print(out, im.size, os.path.getsize(out), "bytes; vanguard blue", vb)


if __name__ == "__main__":
    main()
