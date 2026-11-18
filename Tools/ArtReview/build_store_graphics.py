"""Google Play store graphics drafts, built from the game's own art (docs/59: the store listing before the closed test).

    python -B Tools/ArtReview/build_store_graphics.py [--out StoreListing/graphics] [--review DIR]

Sources (none of them ships; the outputs live outside Assets):
  - the Vanguard: its painted front three-quarter master, ArtDirection/2026-09-17/vanguard-directions-01/front-idle-v1.png
    (1176 x 1337, the master the in-game front sprites and puppet are cut from);
  - Ilya, the Cinder Arcanist: the game's own Blender model rendered at 4x by its build script, one raw frame a pose:
        blender --background --factory-startup --python Tools/ArtReview/BuildBlenderArcanist.py -- \
            --out TestResults/store-ilya --blend TestResults/store-ilya/ilya.blend \
            --facings south-west,south,south-east --clips cast,idle --preview --frame 2 --samples 32
    (880 x 1000 frames; this uses south-west/idle_2.png: the staff held upright, its crucible alight);
  - the Foundry: the runtime overview capture of 2026-11-12, ArtDirection/2026-11-12/foundry-trolley-crown-01/after/
    overview.png (1600 x 1200: the floating court, its pit, the chained towers and the lava falls).
ArtDirection is not in git: from an agent's worktree the main checkout's copy is found by walking up the folders.

Writes (each a Play requirement, see StoreListing/README.md):
  - icon-512.png: the hi-res app icon, 512 x 512, 32-bit PNG. The Vanguard's helmet and shoulders, lit from behind by
    the foundry's ember glow over the lava towers. No text. Full bleed: Play rounds the corners itself.
  - adaptive-foreground-432.png / adaptive-background-432.png: the Android adaptive launcher icon's two layers at
    432 x 432 (108 dp at xxxhdpi). The foreground is the same bust on transparency, kept inside the central 66 dp safe
    circle (264 px across, 61 % of the layer) so no launcher mask cuts it; the background is the glow and towers.
  - feature-graphic-1024x500.png: the feature graphic, 1024 x 500, 24-bit PNG (no alpha). The two heroes either side
    of the Foundry's court, the pit glowing between them where a video's play button would sit.
With --review: the icon under the launchers' masks (circle, squircle, rounded square, teardrop) at 192, 96 and 48, and
the feature graphic at its listing sizes. Deterministic: the same sources give the same bytes.
"""
import argparse
import math
import os

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
VANGUARD = os.path.join("ArtDirection", "2026-09-17", "vanguard-directions-01", "front-idle-v1.png")
OVERVIEW = os.path.join("ArtDirection", "2026-11-12", "foundry-trolley-crown-01", "after", "overview.png")
ILYA = os.path.join("TestResults", "store-ilya", "south-west", "idle_2.png")
FONT = "C:/Windows/Fonts/arialbd.ttf"



def find(rel):
    """`rel` under this checkout or, from an agent's worktree, the first folder above it that has it."""
    here = ROOT
    while True:
        path = os.path.join(here, rel)
        if os.path.exists(path):
            return path
        up = os.path.dirname(here)
        if up == here:
            raise SystemExit(f"missing source: {rel} (see this script's docstring)")
        here = up


# ----------------------------------------------------------------------------------------------------------------------
# Working in linear light, premultiplied: a layer is (rgb, a) with rgb already multiplied by a.


def to_lin(srgb):
    c = np.asarray(srgb, np.float32)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(lin):
    c = np.clip(lin, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)


# Linear-light colours (the working space): the foundry's ember, its gold-hot core, the dusk of its sky.
EMBER = to_lin(np.array([255, 104, 24], np.float32) / 255.0)
HOT = to_lin(np.array([255, 178, 70], np.float32) / 255.0)
DUSK = to_lin(np.array([30, 16, 44], np.float32) / 255.0)


def load_layer(path):
    img = np.asarray(Image.open(path).convert("RGBA")).astype(np.float32) / 255.0
    a = img[..., 3:4]
    return to_lin(img[..., :3]) * a, a


def load_rgb(path):
    return to_lin(np.asarray(Image.open(path).convert("RGB")).astype(np.float32) / 255.0)


def resize(arr, w, h):
    """Area filtering down, cubic up (premultiplied layers stay premultiplied)."""
    interp = cv2.INTER_AREA if w < arr.shape[1] else cv2.INTER_CUBIC
    out = cv2.resize(arr, (int(w), int(h)), interpolation=interp)
    return out if out.ndim == arr.ndim else out[..., None]


def place(canvas_rgb, layer, x, y):
    """Composite a premultiplied layer (rgb, a) over a linear canvas, its top-left at (x, y)."""
    rgb, a = layer
    H, W = canvas_rgb.shape[:2]
    h, w = a.shape[:2]
    x0, y0 = max(0, x), max(0, y)
    x1, y1 = min(W, x + w), min(H, y + h)
    if x0 >= x1 or y0 >= y1:
        return canvas_rgb
    sx, sy = x0 - x, y0 - y
    src_rgb = rgb[sy:sy + y1 - y0, sx:sx + x1 - x0]
    src_a = a[sy:sy + y1 - y0, sx:sx + x1 - x0]
    canvas_rgb[y0:y1, x0:x1] = src_rgb + canvas_rgb[y0:y1, x0:x1] * (1 - src_a)
    return canvas_rgb


def blur(arr, sigma):
    if sigma <= 0:
        return arr
    out = cv2.GaussianBlur(arr, (0, 0), sigma)
    return out[..., None] if out.ndim < arr.ndim else out


def radial(w, h, cx, cy, r, power=1.6):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.hypot(xx - cx, yy - cy) / r
    return (np.clip(1 - d, 0, 1) ** power)[..., None]


def rim_light(layer, colour, strength, toward=(0.0, -1.0), width=3.0):
    """A warm rim on the silhouette's edges that face `toward` (a unit vector, y down): the glow behind the figure
    catching its outline. Added only where the figure is."""
    rgb, a = layer
    al = a[..., 0]
    inner = blur(al, width)
    edge = np.clip(al - inner, 0, 1)
    gy, gx = np.gradient(blur(al, width * 1.5))
    facing = np.clip(-(gx * toward[0] + gy * toward[1]) * 6.0, 0, 1)  # the edge's outward normal is -gradient
    k = (edge * (0.35 + 0.65 * facing))[..., None] * strength
    return rgb + colour * k * a, a


def sparks(w, h, n, seed, area=None, size=(0.8, 2.2), colour=None):
    """A field of rising ember sparks: a small bright head and a fading trail below it, as light to add."""
    colour = HOT if colour is None else colour
    rng = np.random.RandomState(seed)
    out = np.zeros((h, w), np.float32)
    x0, y0, x1, y1 = area or (0, 0, w, h)
    for _ in range(n):
        x, y = rng.uniform(x0, x1), rng.uniform(y0, y1)
        r = rng.uniform(*size)
        b = rng.uniform(0.35, 1.0)
        drift = rng.uniform(-1.5, 1.5) * r
        for k in range(6):
            t = k / 6.0
            cv2.circle(out, (int((x + drift * t) * 16), int((y + r * 5 * t) * 16)),
                       max(1, int(r * (1 - t * 0.6) * 16)), b * (1 - t) * 0.7, -1, cv2.LINE_AA, shift=4)
        cv2.circle(out, (int(x * 16), int(y * 16)), int(r * 16), b, -1, cv2.LINE_AA, shift=4)
    halo = blur(out, 2.5) * 1.2
    return (out + halo)[..., None] * colour


def save_rgb(lin, path):
    img = (to_srgb(lin) * 255 + 0.5).astype(np.uint8)
    Image.fromarray(img, "RGB").save(path, optimize=True)


def save_rgba(rgb_premul, a, path):
    al = np.clip(a, 0, 1)
    straight = np.where(al > 1e-5, rgb_premul / np.maximum(al, 1e-5), 0)
    img = np.concatenate([(to_srgb(straight) * 255 + 0.5), (al * 255 + 0.5)], -1).astype(np.uint8)
    Image.fromarray(img, "RGBA").save(path, optimize=True)


# ----------------------------------------------------------------------------------------------------------------------
# The pieces.


def vanguard_bust(width):
    """The Vanguard's helmet, pauldrons and chest from its master, `width` pixels across (the crop is square)."""
    rgb, a = load_layer(find(VANGUARD))
    x0, y0, side = 318, 62, 640  # helmet crest at the top, the belt at the bottom, the pauldrons inside the sides
    rgb, a = rgb[y0:y0 + side, x0:x0 + side], a[y0:y0 + side, x0:x0 + side]
    return resize(rgb, width, width), resize(a, width, width)


def icon_background(size):
    """The lava towers of the overview, out of focus, under the foundry's ember glow and a dusk vignette."""
    ov = load_rgb(find(OVERVIEW))
    # The left towers and their lava falls (the overview's own upper left), a square.
    crop = ov[0:620, 0:620]
    bg = resize(crop, size, size)
    bg = blur(bg, size / 110.0)
    bg = bg * 0.45 + DUSK * 0.5
    glow = radial(size, size, size * 0.5, size * 0.40, size * 0.60, 1.5)
    hot = radial(size, size, size * 0.5, size * 0.36, size * 0.26, 2.0)
    bg = bg + EMBER * glow * 0.55 + HOT * hot * 0.50
    vignette = radial(size, size, size * 0.5, size * 0.42, size * 0.80, 0.7)
    bg = bg * (0.30 + 0.70 * vignette)
    bg = bg + sparks(size, size, int(size / 20), 11, (0, 0, size, size * 0.75), (size / 360, size / 170), HOT) * 0.6
    return bg


def icon_512():
    size = 512
    bg = icon_background(size)
    bust = vanguard_bust(560)
    bust = rim_light(bust, HOT, 1.6, toward=(0.0, -1.0), width=4.0)
    # A soft shadow under the bust's silhouette, so it lifts off the glow.
    sh = blur(bust[1], 10.0) * 0.55
    canvas = place(bg.copy(), (np.zeros_like(bust[0]), sh), -24 + 6, 40 + 12)
    canvas = place(canvas, bust, -24, 40)
    return canvas


def adaptive_layers():
    size = 432
    bg = icon_background(size)
    # The foreground: the bust inside the 66 dp safe circle (264 px across at 432), faded out at its edge.
    safe_r = 132.0
    bust_w = 300
    rgb, a = vanguard_bust(bust_w)
    layer_rgb = np.zeros((size, size, 3), np.float32)
    layer_a = np.zeros((size, size, 1), np.float32)
    ox, oy = (size - bust_w) // 2 - 4, 216 - 128 - 6
    layer_rgb = place(layer_rgb, rim_light((rgb, a), HOT, 1.6, (0.0, -1.0), 3.0), ox, oy)
    layer_a = place(layer_a, (a, a), ox, oy)
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    d = np.hypot(xx - size / 2, yy - size / 2)
    fade = np.clip((safe_r - d) / 14.0, 0, 1)[..., None]
    fade = fade * fade * (3 - 2 * fade)
    return bg, layer_rgb * fade, layer_a * fade


def ilya_figure(height):
    """Ilya cropped to its bounds and `height` tall, and where its crucible's glowing head is (the brightest warm
    cluster in the frame's top quarter: the flame in the staff's crucible head), in the result's pixels."""
    rgb, a = load_layer(find(ILYA))
    ys, xs = np.nonzero(a[..., 0] > 0.02)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    srgb = to_srgb(np.where(a > 1e-4, rgb / np.maximum(a, 1e-4), 0))
    hot = (srgb[..., 0] > 0.85) & (srgb[..., 0] - srgb[..., 2] > 0.30) & (srgb[..., 1] > 0.55) & (a[..., 0] > 0.5)
    hot[y0 + (y1 - y0) // 4:] = False  # the staff's head is in the top quarter
    hy, hx = np.nonzero(hot)
    orb = ((hx.mean() - x0), (hy.mean() - y0)) if len(hx) else None
    rgb, a = rgb[y0:y1, x0:x1], a[y0:y1, x0:x1]
    k = height / (y1 - y0)
    orb = (orb[0] * k, orb[1] * k) if orb else None
    return resize(rgb, (x1 - x0) * k, height), resize(a, (x1 - x0) * k, height), orb


def vanguard_figure(height):
    rgb, a = load_layer(find(VANGUARD))
    ys, xs = np.nonzero(a[..., 0] > 0.02)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    rgb, a = rgb[y0:y1, x0:x1], a[y0:y1, x0:x1]
    k = height / (y1 - y0)
    return resize(rgb, (x1 - x0) * k, height), resize(a, (x1 - x0) * k, height)


def blend_into(layer, scene, x, y, amount=0.10, bounce=0.12):
    """Sits a figure in the scene: its colours drawn a little toward the blurred scene behind it (the air between),
    and a warm bounce from the floor on its lower part."""
    rgb, a = layer
    h, w = a.shape[:2]
    pad = np.pad(blur(scene, 18.0), ((h, h), (w, w), (0, 0)), mode="edge")
    behind = pad[y + h:y + 2 * h, x + w:x + 2 * w]
    low = np.clip((np.linspace(0, 1, h, dtype=np.float32)[:, None, None] - 0.55) / 0.45, 0, 1)
    rgb = rgb * (1 - amount) + behind * a * amount + EMBER * a * low * bounce
    return rgb, a


def ground_shadow(w, h, cx, cy, rx, ry, strength):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.hypot((xx - cx) / rx, (yy - cy) / ry)
    return (np.clip(1 - d, 0, 1) ** 1.2 * strength)[..., None]


def feature_graphic():
    W, H = 1024, 500
    ov = load_rgb(find(OVERVIEW))
    # The court centred, the towers either side: a 2.048:1 band of the overview.
    band_h = int(round(1600 / (W / H)))
    top = 250
    bg = resize(ov[top:top + band_h, 0:1600], W, H)
    sharp = bg.copy()
    bg = blur(bg, 1.6)
    # Keep the court sharper than the edges, as a lens would: the heroes stand in front of it.
    focus = radial(W, H, W * 0.5, H * 0.52, W * 0.42, 1.0)
    bg = bg * (1 - focus) + sharp * focus
    bg = bg * 0.78
    # The foundry's warmth from the pit and below, the sky cooler and darker.
    bg = bg + EMBER * radial(W, H, W * 0.47, H * 0.62, W * 0.30, 1.6) * 0.10
    yy = np.linspace(0, 1, H, dtype=np.float32)[:, None, None]
    bg = bg * (0.72 + 0.28 * np.clip(1 - np.abs(yy - 0.5) * 1.6, 0, 1))
    # The foreground falls into shadow under the heroes' feet.
    floor = np.clip((yy - 0.70) / 0.30, 0, 1) ** 1.3
    bg = bg * (1 - floor * 0.75) + DUSK * floor * 0.25

    vg = vanguard_figure(408)
    il_rgb, il_a, orb = ilya_figure(366)
    il = (il_rgb, il_a)
    # Rim light from the court's glow between them: the Vanguard's right edges, Ilya's left.
    vg = rim_light(vg, HOT, 1.1, toward=(1.0, -0.3), width=3.0)
    il = rim_light(il, HOT, 0.45, toward=(-1.0, -0.3), width=2.0)
    feet = H - 22
    # Both well inside the edges, which some placements crop; the pit between them, where a video's play button sits.
    vcx, icx = 292, 742
    vx = vcx - vg[1].shape[1] // 2
    ix = icx - il[1].shape[1] // 2
    canvas = bg.copy()
    canvas = canvas * (1 - ground_shadow(W, H, vcx, feet - 4, 150, 22, 0.55))
    canvas = canvas * (1 - ground_shadow(W, H, icx - 6, feet - 4, 110, 18, 0.55))
    vg = blend_into(vg, canvas, vx, feet - vg[1].shape[0])
    il = blend_into(il, canvas, ix, feet - il[1].shape[0])
    canvas = place(canvas, vg, vx, feet - vg[1].shape[0])
    canvas = place(canvas, il, ix, feet - il[1].shape[0])
    if orb:
        # The crucible's flare, which the game's sprite packer lays round it: a white-hot core and an ember halo.
        ox, oy = ix + orb[0], feet - il[1].shape[0] + orb[1]
        canvas = canvas + EMBER * radial(W, H, ox, oy, 110, 2.2) * 0.8 + HOT * radial(W, H, ox, oy, 22, 1.6) * 0.7
    canvas = canvas + sparks(W, H, 46, 5, (0, 0, W, H * 0.85), (0.7, 1.9)) * 0.55
    return canvas


# ----------------------------------------------------------------------------------------------------------------------
# Review: the icon under launcher masks and at small sizes, the feature graphic at its listing sizes.


def masks(size):
    out = {}
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32) + 0.5
    c = size / 2
    out["circle"] = np.hypot(xx - c, yy - c) <= c
    n = 5.0  # squircle
    out["squircle"] = (np.abs((xx - c) / c) ** n + np.abs((yy - c) / c) ** n) <= 1
    r = size * 0.22
    dx = np.clip(np.abs(xx - c) - (c - r), 0, None)
    dy = np.clip(np.abs(yy - c) - (c - r), 0, None)
    out["rounded"] = np.hypot(dx, dy) <= r
    tear = np.hypot(xx - c, yy - c) <= c
    out["teardrop"] = tear | ((xx >= c) & (yy >= c))
    return out


def review(icon, fg, fa, bg432, feature, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    font = ImageFont.truetype(FONT, 16) if os.path.exists(FONT) else ImageFont.load_default()
    # The adaptive icon as a launcher draws it: the 108 dp layers masked to the central 72 dp (288 of 432).
    full = bg432 * (1 - fa) + fg
    view = full[72:360, 72:360]
    sheet = Image.new("RGB", (1500, 760), (28, 30, 38))
    d = ImageDraw.Draw(sheet)
    d.text((16, 10), "Adaptive icon (foreground over background, the launcher's 72 dp window) under four masks, at 192 /"
                     " 96 / 48; then the 512 Play icon with Play's rounded corners", font=font, fill=(220, 220, 220))
    x = 16
    for name, m in masks(192).items():
        for j, s in enumerate((192, 96, 48)):
            mm = masks(s)[name][..., None]
            img = resize(view, s, s)
            rgba = np.concatenate([to_srgb(img) * 255, mm * 255.0], -1).astype(np.uint8)
            sheet.paste(Image.fromarray(rgba, "RGBA"), (x + (0 if j == 0 else (j - 1) * 60), 40 if j == 0 else 250),
                        Image.fromarray(rgba, "RGBA"))
        d.text((x, 360), name, font=font, fill=(200, 200, 200))
        x += 230
    # The safe circle and the whole layers.
    lay = (to_srgb(full) * 255).astype(np.uint8)
    lay_img = Image.fromarray(lay, "RGB").resize((288, 288), Image.LANCZOS)
    dd = ImageDraw.Draw(lay_img)
    k = 288 / 432
    dd.ellipse([(216 - 132) * k, (216 - 132) * k, (216 + 132) * k, (216 + 132) * k], outline=(80, 255, 120))
    dd.rectangle([72 * k, 72 * k, 360 * k, 360 * k], outline=(255, 255, 80))
    sheet.paste(lay_img, (16 + 4 * 230, 40))
    d.text((16 + 4 * 230, 335), "the 108 dp layers: 72 dp window (yellow), 66 dp safe circle (green)", font=font,
           fill=(200, 200, 200))
    play = (to_srgb(icon) * 255).astype(np.uint8)
    m512 = masks(512)["rounded"]
    for j, s in enumerate((256, 96, 48)):
        img = Image.fromarray(play, "RGB").resize((s, s), Image.LANCZOS)
        mm = Image.fromarray((m512 * 255).astype(np.uint8), "L").resize((s, s), Image.LANCZOS)
        sheet.paste(img, (16 + j * 280 if j == 0 else 16 + 280 + (j - 1) * 120, 420), mm)
    d.text((16, 690), "Play icon 512 shown at 256 / 96 / 48 with rounded corners", font=font, fill=(200, 200, 200))
    sheet.save(os.path.join(out_dir, "icon-review.png"))
    f = Image.fromarray((to_srgb(feature) * 255).astype(np.uint8), "RGB")
    fs = Image.new("RGB", (1024 + 40, 500 + 300 + 80), (28, 30, 38))
    fs.paste(f, (20, 20))
    small = f.resize((512, 250), Image.LANCZOS)
    fs.paste(small, (20, 540))
    tiny = f.resize((320, 156), Image.LANCZOS)
    fs.paste(tiny, (560, 540))
    ImageDraw.Draw(fs).text((20, 800), "feature graphic at 1024, 512 and 320 wide", font=font, fill=(200, 200, 200))
    fs.save(os.path.join(out_dir, "feature-review.png"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default=os.path.join(ROOT, "StoreListing", "graphics"))
    ap.add_argument("--review", default=None)
    args = ap.parse_args()
    os.makedirs(args.out, exist_ok=True)
    icon = icon_512()
    save_rgb(icon, os.path.join(args.out, "icon-512.png"))
    bg432, fg, fa = adaptive_layers()
    save_rgb(bg432, os.path.join(args.out, "adaptive-background-432.png"))
    save_rgba(fg, fa, os.path.join(args.out, "adaptive-foreground-432.png"))
    feature = feature_graphic()
    save_rgb(feature, os.path.join(args.out, "feature-graphic-1024x500.png"))
    if args.review:
        review(icon, fg, fa, bg432, feature, args.review)
    for name in sorted(os.listdir(args.out)):
        if name.endswith(".png"):
            p = os.path.join(args.out, name)
            im = Image.open(p)
            print(f"{name}: {im.size[0]}x{im.size[1]} {im.mode} {os.path.getsize(p):,} bytes")


if __name__ == "__main__":
    main()
