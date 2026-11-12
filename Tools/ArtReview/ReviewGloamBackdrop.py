"""Review images for the Gloam Gardens pass-2 assets (docs/51 sections 6-8), written to an untracked folder:
- plate.png: ENV_Gloam_Garden_v1 alone, as the game draws it (FoundryCavern.shader: renderer tint, saturation x1.08,
  10 % violet), full and as the phone's portrait crop (FrameCavern: cover the camera x1.08, centred);
- arena-overview.png / arena-bridge.png: floor-3 mocks - the floor-2 captures (ice-map-01/floor2) with their ice plate
  found by FrameCavern's framing and replaced by the garden plate, and their arena re-tinted to the docs/51 palette
  (slate to the ash-loam ramp, quicksilver glows to dim gloamlight; saturated actors, bars and telegraphs untouched);
  each beside its floor-2 original;
- growth-sheet.png: GloamGrowth.png on a checker and on mossed wall stone, and at play scale (120 px per world unit,
  the Gameplay camera's 9-unit width on a 1080 px phone) on a sidewall mock beside the Vanguard;
- checks.txt: the docs/51 section 7 numbers (plate contrast in the arena-overlap zone, red-orange and orchid pixel
  counts, the mock floor's luminance and saturation, the growth sheet's hues).
Usage: python -I Tools/ArtReview/ReviewGloamBackdrop.py [out_dir]
Default out: E:/MobileGame/ArtDirection/2026-11-12/gloam-backdrop-01
"""
import importlib.util
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ENV = os.path.join(ROOT, "Assets", "_Project", "Art", "Environment")
GLOAM_PLATE = os.path.join(ENV, "ENV_Gloam_Garden_v1.png")
ICE_PLATE = os.path.join(ENV, "ENV_Ice_Vault_v1.png")
GROWTH = os.path.join(ROOT, "Assets", "_Project", "Resources", "Gloam", "GloamGrowth.png")
VANGUARD = os.path.join(ROOT, "Assets", "_Project", "Art", "Characters", "Vanguard", "CHR_Vanguard_ContactA.tga")
CAPTURES = os.path.join("E:/MobileGame", "ArtDirection", "2026-11-12", "ice-map-01", "floor2")
DEFAULT_OUT = os.path.join("E:/MobileGame", "ArtDirection", "2026-11-12", "gloam-backdrop-01")

spec = importlib.util.spec_from_file_location("gloam_backdrop", os.path.join(os.path.dirname(__file__), "GenerateGloamBackdrop.py"))
gb = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gb)


def font(size):
    for f in ("segoeui.ttf", "arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(f, size)
        except OSError:
            pass
    return ImageFont.load_default()


def load(path):
    return np.asarray(Image.open(path).convert("RGB")).astype(np.float32) / 255.0


def hsv(img):
    mx, mn = img.max(-1), img.min(-1)
    s = np.where(mx > 1e-4, (mx - mn) / np.maximum(mx, 1e-4), 0)
    r, g, b = img[..., 0], img[..., 1], img[..., 2]
    d = np.maximum(mx - mn, 1e-6)
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60
    return h, s, mx


def lum(img):
    return img @ np.array([0.2126, 0.7152, 0.0722], np.float32)


def framed(plate, cw, ch, dx, dy):
    """The plate as FrameCavern draws it on a cw x ch camera: cover x1.08, centred, shifted by the parallax (dx, dy
    screen pixels, y down). Nearest-free bilinear sampling."""
    ph, pw = plate.shape[:2]
    scale = max(cw / pw, ch / ph) * 1.08
    ys, xs = np.mgrid[0:ch, 0:cw].astype(np.float32)
    px = (xs - cw / 2 - dx) / scale + pw / 2
    py = (ys - ch / 2 - dy) / scale + ph / 2
    x0 = np.clip(np.floor(px).astype(int), 0, pw - 2)
    y0 = np.clip(np.floor(py).astype(int), 0, ph - 2)
    fx = np.clip(px - x0, 0, 1)[..., None]
    fy = np.clip(py - y0, 0, 1)[..., None]
    a = plate[y0, x0] * (1 - fx) + plate[y0, x0 + 1] * fx
    b = plate[y0 + 1, x0] * (1 - fx) + plate[y0 + 1, x0 + 1] * fx
    return a * (1 - fy) + b * fy


def find_parallax(capture, plate):
    ch, cw = capture.shape[:2]
    small = capture[::4, ::4]
    best = None
    rx, ry = int(cw * 0.025) + 4, int(ch * 0.025) + 4
    for dy in range(-ry, ry + 1, 4):
        for dx in range(-rx, rx + 1, 4):
            f = framed(plate, cw // 4, ch // 4, dx / 4, dy / 4)
            err = np.abs(f - small).sum(-1)
            score = np.mean(np.sort(err.ravel())[: err.size // 3])  # the best-matching third is the visible plate
            if best is None or score < best[0]:
                best = (score, dx, dy)
    return best[1], best[2]


def background_mask(capture, plate_seen):
    """Where the capture is the ice plate: the per-pixel error averaged over a small window (the arena differs in
    texture even where its value is close to the plate's), then a light clean-up."""
    from PIL import ImageFilter
    err = np.abs(capture - plate_seen).sum(-1)
    err_im = Image.fromarray(np.clip(err * 255, 0, 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(4))
    window = np.asarray(err_im).astype(np.float32) / 255.0
    m = ((window < 0.085) & (err < 0.2)).astype(np.float32)
    # the plate's own bright features (frost-light windows, lit shard edges) resample badly: take them as plate
    # wherever the capture is bright there too
    bright = Image.fromarray(((lum(plate_seen) > 0.33) * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(9))
    m = np.maximum(m, (np.asarray(bright) > 0) & (lum(capture) > 0.28) & (window < 0.25))
    im = Image.fromarray((m * 255).astype(np.uint8))
    im = im.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(7)).filter(ImageFilter.MinFilter(5))
    im = im.filter(ImageFilter.GaussianBlur(1.0))
    return np.asarray(im).astype(np.float32) / 255.0


RAMP = [(0.00, "0C0E0B"), (0.10, "1D1F1A"), (0.24, "3C3F36"), (0.40, "63665A"), (0.65, "A7A99C"), (1.0, "E4E4DA")]


def ramp(t):
    t = np.clip(t, 0, 1)
    out = np.zeros(t.shape + (3,), np.float32)
    for (t0, c0), (t1, c1) in zip(RAMP[:-1], RAMP[1:]):
        k = np.clip((t - t0) / (t1 - t0), 0, 1)[..., None]
        seg = (t >= t0) & (t <= t1)
        out[seg] = (gb.c(c0) * (1 - k) + gb.c(c1) * k)[seg]
    return out


def gloam_arena(capture):
    """Floor 2 to a floor-3 mock: slate to the ash-loam ramp at the same luminance, quicksilver/ice glows to dim
    gloamlight, mirror-ice to verdigris; saturated non-cyan pixels (heroes, bars, telegraphs, pickups) untouched."""
    h, s, v = hsv(capture)
    L = lum(capture)
    out = ramp(L * 1.02)
    cyan = (h > 165) & (h < 215) & (s > 0.28)
    glow = cyan & (v > 0.6)
    ice = cyan & ~glow
    out[ice] = (gb.VERDIGRIS * np.clip(L[ice] * 1.6, 0.25, 1.1)[..., None]) * 0.85
    out[glow] = gb.GLOAM * np.clip(v[glow], 0, 1)[..., None] * 0.62
    keep = (s > 0.38) & ~cyan
    out[keep] = capture[keep]
    return np.clip(out, 0, 1)


def to_img(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "RGB")


def label(im, text, pos=(14, 10), size=26):
    d = ImageDraw.Draw(im)
    d.text((pos[0] + 2, pos[1] + 2), text, fill=(0, 0, 0), font=font(size))
    d.text(pos, text, fill=(235, 235, 225), font=font(size))


def mock(name, ice_plate, gloam_plate_seen_fn, out_dir, checks):
    cap = load(os.path.join(CAPTURES, name + ".png"))
    ch, cw = cap.shape[:2]
    ice_seen = gb.in_game(ice_plate)
    dx, dy = find_parallax(cap, ice_seen)
    seen = framed(ice_seen, cw, ch, dx, dy)
    bg = background_mask(cap, seen)
    arena = gloam_arena(cap)
    garden = framed(gloam_plate_seen_fn(), cw, ch, dx, dy)
    out = arena * (1 - bg[..., None]) + garden * bg[..., None]
    fl = bg < 0.05
    L, (h, s, v) = lum(arena)[fl], hsv(arena)
    checks.append("%s: parallax (%d, %d) px, plate visible on %.0f %% of the frame; mock arena mean luminance %.2f, "
                  "mean saturation %.2f (docs/51: 0.25-0.40 and below 0.2)" % (name, dx, dy, bg.mean() * 100, L.mean(), s[fl].mean()))
    left = to_img(cap)
    right = to_img(out)
    label(left, "floor 2 (capture)")
    label(right, "floor 3 mock: garden plate + docs/51 re-tint")
    gap = 16
    sheet = Image.new("RGB", (cw * 2 + gap, ch), (12, 12, 12))
    sheet.paste(left, (0, 0))
    sheet.paste(right, (cw + gap, 0))
    if ch > 1600:
        sheet = sheet.resize((sheet.width * 1400 // ch, 1400), Image.LANCZOS)
    sheet.save(os.path.join(out_dir, "arena-" + ("overview" if name == "overview" else "bridge") + ".png"))
    to_img(out).save(os.path.join(out_dir, "arena-" + ("overview" if name == "overview" else "bridge") + "-mock.png"))


def plate_checks(plate, seen, checks):
    H, W = plate.shape[:2]
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    u, v = xx / W, yy / H
    zone = np.exp(-(((u - 0.5) / 0.24) ** 2 + ((v - 0.66) / 0.24) ** 2)) > 0.5
    sun = np.sqrt((xx - gb.SUN_X) ** 2 + (yy - gb.SUN_Y) ** 2) < gb.SUN_R * 0.9
    L = lum(seen)
    checks.append("plate (in game): arena-overlap zone luminance mean %.3f, std %.3f, 5-95 %% range %.3f; whole plate std %.3f"
                  % (L[zone].mean(), L[zone].std(), np.percentile(L[zone], 95) - np.percentile(L[zone], 5), L.std()))
    checks.append("plate (in game): sun disc luminance %.3f; brightest 0.1 %% of the rest %.3f (the sun stays the focal point)"
                  % (L[sun].mean(), np.percentile(L[~sun], 99.9)))
    for tag, img in (("source", plate), ("in game", seen)):
        h, s, val = hsv(img)
        red = ((h < 30) | (h > 345)) & (s > 0.45) & (val > 0.35)
        orchid = (h >= 280) & (h <= 330) & (s > 0.2) & (val > 0.15)
        checks.append("plate (%s): red-orange pixels (hue <30 or >345, sat >0.45, value >0.35): %d; orchid pixels "
                      "(hue 280-330, sat >0.2, value >0.15): %d" % (tag, int(red.sum()), int(orchid.sum())))
    h, s, val = hsv(seen)
    bright = val > 0.6
    green = bright & (h > 60) & (h < 100) & (s > 0.3)
    centre = (u > 0.3) & (u < 0.7) & (v > 0.35) & (v < 0.88)
    checks.append("plate (in game): bright gloamlight pixels %d, of which in the middle (u .3-.7, v .35-.88) %d"
                  % (int(green.sum()), int((green & centre).sum())))


def plate_sheet(plate, seen, out_dir):
    H, W = plate.shape[:2]
    # the phone: 1080 x 2340, FrameCavern's cover x1.08 at zero parallax
    phone = framed(seen, 540, 1170, 0, 0)
    full = to_img(seen).resize((780, 1170), Image.LANCZOS)
    p = to_img(phone)
    label(full, "ENV_Gloam_Garden_v1 (as drawn: shader x1.08 sat, 10 % violet)", size=20)
    label(p, "phone portrait crop", size=20)
    sheet = Image.new("RGB", (780 + 16 + 540, 1170), (12, 12, 12))
    sheet.paste(full, (0, 0))
    sheet.paste(p, (796, 0))
    sheet.save(os.path.join(out_dir, "plate.png"))
    to_img(plate).save(os.path.join(out_dir, "plate-source.png"))


def growth_sheet(out_dir, checks):
    g = Image.open(GROWTH).convert("RGBA")
    n = g.width
    chk = Image.new("RGBA", g.size, (90, 90, 90, 255))
    d = ImageDraw.Draw(chk)
    for y in range(0, n, 32):
        for x in range(0, n, 32):
            if (x // 32 + y // 32) % 2:
                d.rectangle([x, y, x + 31, y + 31], fill=(118, 118, 118, 255))
    wall = Image.new("RGBA", g.size, (0x2C, 0x30, 0x29, 255))
    a = Image.alpha_composite(chk, g).resize((512, 512), Image.LANCZOS)
    b = Image.alpha_composite(wall, g).resize((512, 512), Image.LANCZOS)
    # play scale: 120 px per world unit; a rim edge ~1 unit takes a 256 px quarter of a moss strip (0.47x), a root
    # 0.4-1.25 units long, a cluster ~0.55 units tall.
    scene = Image.new("RGBA", (1080, 512), (0, 0, 0, 255))
    sd = ImageDraw.Draw(scene)
    # backdrop: the garden plate's middle, darkened as it sits behind the rim
    plate = Image.open(GLOAM_PLATE).convert("RGB").crop((0, 900, 1024, 1386)).resize((1080, 512))
    scene.paste(plate, (0, 0))
    deck_top, rim = 120, 210
    sd.rectangle([0, deck_top, 1080, rim], fill=(0x3C, 0x3F, 0x36, 255))  # the deck (ash loam)
    sd.rectangle([0, rim, 1080, rim + 150], fill=(0x2C, 0x30, 0x29, 255))  # the sidewall (wall stone, mossed)
    for x in range(0, 1080, 64):
        sd.line([x, rim, x, rim + 150], fill=(0x22, 0x26, 0x20, 255), width=2)
    sd.line([0, rim + 75, 1080, rim + 75], fill=(0x22, 0x26, 0x20, 255), width=2)
    k = 0
    for x in range(0, 1080, 120):  # moss strip, one quarter per 120 px edge
        q = g.crop(((k % 4) * 256, 768, (k % 4) * 256 + 256, 896)).resize((120, 60), Image.LANCZOS)
        scene.alpha_composite(q, (x, rim - 15))
        k += 1
    rng = np.random.default_rng(7)
    for x in range(30, 1080, 60):  # roots under the rim (every other edge carries three in the ice builder)
        if (x // 120) % 2:
            continue
        i = int(rng.integers(0, 8))
        length = rng.uniform(0.4, 1.25) * 120
        cell = g.crop((i * 128, 0, i * 128 + 128, 512)).resize((int(length / 4), int(length)), Image.LANCZOS)
        scene.alpha_composite(cell, (int(x - length / 8), rim + 6))
    for x, i in ((260, 0), (620, 2), (900, 3)):  # clusters on the sidewall (every fifth edge)
        cell = g.crop((i * 256, 512, i * 256 + 256, 768)).resize((66, 66), Image.LANCZOS)
        scene.alpha_composite(cell, (x - 33, rim + 90 - 63))
    if os.path.exists(VANGUARD):
        v = Image.open(VANGUARD).convert("RGBA")
        bb = v.getbbox()
        v = v.crop(bb)
        v = v.resize((int(v.width * 140 / v.height), 140), Image.LANCZOS)
        scene.alpha_composite(v, (480, deck_top + 60 - 140 + 20))
    s = scene.convert("RGB")
    label(s, "play scale (120 px / unit): moss fringe, roots, clusters on the sidewall; the Vanguard on the deck", size=20)
    sheet = Image.new("RGB", (1080, 512 + 16 + 512), (12, 12, 12))
    sheet.paste(a.convert("RGB"), (0, 0))
    sheet.paste(b.convert("RGB"), (568, 0))
    sheet.paste(s, (0, 528))
    label(sheet, "GloamGrowth.png (1024x1024): roots 8x(128x512) | clusters 4x(256x256) | moss strips 2x(1024x128)", size=20)
    sheet.save(os.path.join(out_dir, "growth-sheet.png"))
    arr = np.asarray(g).astype(np.float32) / 255
    rgb, al = arr[..., :3], arr[..., 3]
    h, s_, val = hsv(rgb)
    vis = al > 0.3
    checks.append("growth: orchid pixels (hue 280-330, sat >0.2) %d; red-orange (hue <30 or >345, sat >0.45, value >0.35) %d; "
                  "warm amber glow (hue 30-55, sat >0.5, value >0.6) %d"
                  % (int((vis & (h >= 280) & (h <= 330) & (s_ > 0.2)).sum()),
                     int((vis & ((h < 30) | (h > 345)) & (s_ > 0.45) & (val > 0.35)).sum()),
                     int((vis & (h > 30) & (h < 55) & (s_ > 0.5) & (val > 0.6)).sum())))


def main():
    out_dir = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_OUT
    os.makedirs(out_dir, exist_ok=True)
    plate = load(GLOAM_PLATE)
    seen = gb.in_game(plate)
    ice = load(ICE_PLATE)
    checks = []
    plate_checks(plate, seen, checks)
    plate_sheet(plate, seen, out_dir)
    for name in ("overview", "portrait_-31_-31"):
        mock(name, ice, lambda: seen, out_dir, checks)
    growth_sheet(out_dir, checks)
    with open(os.path.join(out_dir, "checks.txt"), "w") as f:
        f.write("\n".join(checks) + "\n")
    print("\n".join(checks))


if __name__ == "__main__":
    main()
