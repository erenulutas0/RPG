"""Gloam Gardens (floor 3) mood board: palette swatches, preview re-tints of the foundry paving and
masonry (ash-loam with moss in the joints), the enemies re-coloured as charred bark with orchid bloom
beside their foundry selves, the backdrop plate concept (GenerateGloamBackdrop.py), and a readability
strip with the real hero sprites (the Vanguard's blue, Ilya's ash-white robe and ember staff), a
re-coloured enemy, the red-orange telegraph, the XP crystal and a damage number on the floor values.

Concept only: the re-tints here are previews of the recipes docs/51 section 9 names for the production
scripts, never written into Assets.
Usage: python Tools/ArtReview/GenerateGloamMoodboard.py [out.png]
Default out: E:/MobileGame/ArtDirection/2026-11-12/gloam-gardens-01/concept/moodboard.png
See docs/51_GLOAM_GARDENS_ART_DIRECTION.md.
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ART = os.path.join(ROOT, "Assets", "_Project", "Art")
FOUNDRY = os.path.join(ART, "Props", "Foundry")
CHARS = os.path.join(ART, "Characters")
VAN = os.path.join(CHARS, "Vanguard", "CHR_Vanguard_ContactA.tga")
ILYA = os.path.join(CHARS, "CinderArcanist", "CHR_CinderArcanist_0.png")
XP = os.path.join(ART, "Props", "GroundRewards", "Reward_Experience.tga")
CONCEPT = os.path.join("E:/MobileGame", "ArtDirection", "2026-11-12", "gloam-gardens-01", "concept")
ENEMIES = [("SlagBrute", "CHR_Grunt_front-idle.tga", "Grunt"), ("Runner", "CHR_Runner_front-idle.tga", "Runner"),
           ("CinderMite", "CHR_Mite_front_idle.tga", "Cinder Mite"), ("ForgeWarden", "CHR_Warden_front_idle.png", "Warden")]

PALETTE = [
    ("Ash loam deep", "1D1F1A"), ("Ash loam", "3C3F36"), ("Ash loam light", "63665A"), ("Ash drift (off-lane)", "A7A99C"),
    ("Wall stone, mossed", "2C3029"), ("Wall cap, lichen", "7C8466"), ("Root bark (was iron)", "2A221E"), ("Heartwood", "5B4838"),
    ("Moss", "3F5A32"), ("Moss lit", "4F6B3A"), ("Lichen", "8FA35A"), ("Verdigris (was brass)", "6FA38C"),
    ("Gloamlight (was lava)", "C6EE6A"), ("Gloamlight core", "E2F7A0"), ("Moss shade (shadow)", "1F2E2A"), ("Ash haze", "767A68"),
    ("Sky gloam", "0B110F"), ("Sky dusk", "23302A"), ("Dimmed sun", "CDBE8E"), ("Telegraph (keep)", "FF5A3C"),
    ("Enemy bark body", "4A3B33"), ("Enemy bloom deep", "5A1F66"), ("Enemy bloom (orchid)", "D86BD0"), ("Enemy bloom light", "F6C2F0"),
]


def rgb(h):
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def f3(h):
    return np.array(rgb(h), np.float32) / 255.0


def font(size):
    for f in ("segoeui.ttf", "arial.ttf", "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(f, size)
        except OSError:
            pass
    return ImageFont.load_default()


def smooth(x, lo, hi):
    t = np.clip((x - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


def dilate(mask, steps):
    out = mask.copy()
    for _ in range(steps):
        g = out.copy()
        g[1:] |= out[:-1]
        g[:-1] |= out[1:]
        g[:, 1:] |= out[:, :-1]
        g[:, :-1] |= out[:, 1:]
        out = g
    return out


def periodic_blur(a, sigma):
    h, w = a.shape
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    g = np.exp(-(fx * fx + fy * fy) * (sigma * sigma) * 2.0 * np.pi ** 2)
    return np.real(np.fft.ifft2(np.fft.fft2(a) * g)).astype(np.float32)


def periodic_noise(h, w, scale, rng):
    out = periodic_blur(rng.standard_normal((h, w)).astype(np.float32), scale)
    out -= out.mean()
    return out / (out.std() + 1e-6)


def ramp(t, stops):
    t = np.clip(t, 0, 1)
    n = len(stops) - 1
    seg = np.minimum((t * n).astype(int), n - 1)
    k = (t * n - seg)[..., None]
    s = np.stack(stops)
    return s[seg] * (1 - k) + s[seg + 1] * k


def gloam_surface(src, seed, moss_amount, joint_moss, contrast):
    """Preview of the GenerateGloamTextures recipe: the foundry raster's values on the ash-loam ramp, moss
    gathered in the joints and in patches, a few pale ash flecks; joints and shapes stay where they were."""
    im = np.asarray(Image.open(os.path.join(FOUNDRY, src)).convert("RGB")).astype(np.float32) / 255.0
    h, w, _ = im.shape
    rng = np.random.default_rng(seed)
    lum = im @ np.array([0.299, 0.587, 0.114], np.float32)
    lo, hi = np.percentile(lum, 2), np.percentile(lum, 98)
    t = 0.5 + (np.clip((lum - lo) / (hi - lo + 1e-6), 0, 1) - 0.5) * contrast
    out = ramp(t, [f3("1D1F1A"), f3("3C3F36"), f3("63665A")])
    joint = periodic_blur(np.clip((periodic_blur(lum, 9.0) - lum) * 9.0, 0, 1), 1.5)
    n_big, n_mid = periodic_noise(h, w, 60.0, rng), periodic_noise(h, w, 14.0, rng)
    n_fine = periodic_noise(h, w, 2.0, rng)
    patch = np.clip((n_big * 0.7 + n_mid * 0.3) * 0.5 + moss_amount - 0.5, 0, 1)
    moss = np.clip(patch * (0.5 + 0.5 * np.clip(n_fine * 0.5 + 0.5, 0, 1)), 0, 1) * 0.5
    moss = np.maximum(moss, joint * joint_moss * np.clip(0.55 + 0.45 * n_mid, 0, 1))
    moss_col = ramp(np.clip(n_fine * 0.25 + 0.5, 0, 1), [f3("2E4226"), f3("3F5A32"), f3("4F6B3A")])
    out = out * (1 - moss[..., None]) + moss_col * moss[..., None]
    fleck = (rng.random((h, w)) > 0.9982).astype(np.float32)
    fleck = np.clip(periodic_blur(fleck, 0.9) * 3.0, 0, 0.45) * (1 - joint)
    out = out * (1 - fleck[..., None]) + f3("A7A99C") * fleck[..., None]
    return Image.fromarray(np.clip(out * 255 + 0.5, 0, 255).astype(np.uint8), "RGB")


BARK_TINT = np.array([1.10, 0.95, 0.84], np.float32)


def bloom(rgba, onset=0.12, boost=0.0, value=1.3):
    """Preview of the GenerateGloamEnemyArt recipe (GenerateIceEnemyArt's masks): rock to charred bark a step
    off the floor, brass to verdigris, lava/eyes/flame to the orchid bloom ramp; dark outline and alpha kept."""
    a = rgba.astype(np.float32)[..., :3] / 255
    mx, mn = a.max(-1), a.min(-1)
    lum = a @ np.array([0.299, 0.587, 0.114], np.float32)
    sat = np.where(mx > 1e-4, (mx - mn) / np.maximum(mx, 1e-4), 0)
    warm = smooth(a[..., 0] - a[..., 2], 0.03, 0.12)
    green = a[..., 1] / np.maximum(a[..., 0], 1e-4)
    orange = (warm > 0.5) & (green < 0.6) & (sat > 0.55) & (mx > max(onset, 0.35))
    near = dilate(orange, 3)
    brass = warm * smooth(green, 0.55, 0.62) * smooth(sat, 0.2, 0.3) * (1 - smooth(sat, 0.85, 0.95)) * ~near
    hot = warm * smooth(sat, 0.3, 0.55) * smooth(mx, onset, onset + 0.25)
    core = smooth(a[..., 0] - a[..., 2], -0.01, 0.04) * smooth(mx, 0.82, 0.95) * np.maximum(dilate(orange, 5), 1 - smooth(sat, 0.4, 0.55))
    hot = np.clip(np.maximum(hot, core) * (1 - brass) * (1 + boost), 0, 1)
    t = np.clip(lum / 0.8 + boost, 0, 0.85 - boost * 0.5)[..., None]
    deep, mid, light = f3("5A1F66"), f3("D86BD0"), f3("F6C2F0")
    glow = np.where(t < 0.5, deep + (mid - deep) * np.clip(t * 2, 0, 1), mid + (light - mid) * np.clip(t * 2 - 1, 0, 1))
    body = (np.clip(lum * value + 0.015, 0, 1)[..., None] * 0.9 + a * 0.1) * BARK_TINT
    verd = f3("6FA38C") * np.clip(lum * 1.6, 0, 1.2)[..., None]
    b, h = brass[..., None], hot[..., None]
    out = body * (1 - h - b * (1 - h)) + verd * b * (1 - h) + glow * h
    res = rgba.copy()
    res[..., :3] = np.clip(out * 255 + 0.5, 0, 255).astype(np.uint8)
    return res


def load_rgba(path):
    return np.asarray(Image.open(path).convert("RGBA"))


def ilya_cell():
    """The first pose of Ilya's sheet: the first run of non-empty columns in the top row band."""
    a = load_rgba(ILYA)
    band = a[: a.shape[0] // 6]
    cols = np.where(band[..., 3].max(0) > 30)[0]
    x0 = cols[0]
    x1 = x0
    while x1 + 1 < band.shape[1] and band[:, x1 + 1, 3].max() > 30:
        x1 += 1
    rows = np.where(band[:, x0:x1 + 1, 3].max(1) > 30)[0]
    return Image.fromarray(band[rows[0]:rows[-1] + 1, x0:x1 + 1])


def fit(im, h):
    return im.resize((max(1, int(im.width * h / im.height)), h), Image.LANCZOS)


def trim(im):
    bb = im.getbbox()
    return im.crop(bb) if bb else im


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(CONCEPT, "moodboard.png")
    W, H = 1800, 1130
    im = Image.new("RGB", (W, H), rgb("12150F"))
    dr = ImageDraw.Draw(im)
    dr.text((24, 14), "Cryptforge F3 - Gloam Gardens - mood board (docs/51)", fill=rgb("E6E8DC"), font=font(26))
    dr.text((24, 48), "Plants on ash, moving roots, trees fed by memory instead of light: the foundry overgrown at dusk",
            fill=rgb("9EA38E"), font=font(15))

    # Palette, 4 columns x 6 rows.
    sw, sh, x0, y0 = 140, 58, 24, 80
    for i, (name, hx) in enumerate(PALETTE):
        cx, cy = x0 + (i % 4) * (sw + 10), y0 + (i // 4) * (sh + 34)
        dr.rectangle([cx, cy, cx + sw, cy + sh], fill=rgb(hx), outline=rgb("33382C"))
        dr.text((cx, cy + sh + 2), name, fill=rgb("CBD0BE"), font=font(12))
        dr.text((cx, cy + sh + 16), "#" + hx, fill=rgb("8A8F7A"), font=font(12))

    # Surface previews.
    px = 640
    for j, (src, seed, ma, jm, ct, label) in enumerate([
            ("FoundryPavingV10.png", 5101, 0.30, 0.80, 0.9, "GloamPaving preview (FoundryPavingV10 re-tint + moss)"),
            ("FoundryMasonryV4.png", 5102, 0.36, 0.70, 1.0, "GloamMasonry preview (FoundryMasonryV4 re-tint + moss)")]):
        try:
            tex = gloam_surface(src, seed, ma, jm, ct).resize((340, 340), Image.LANCZOS)
            im.paste(tex, (px + j * 360, 80))
        except OSError:
            pass
        dr.text((px + j * 360, 424), label, fill=rgb("CBD0BE"), font=font(12))

    # Backdrop concept (right column).
    try:
        bd = Image.open(os.path.join(CONCEPT, "backdrop.png")).convert("RGB")
        bd = bd.resize((390, 585), Image.LANCZOS)
        im.paste(bd, (1386, 80))
    except OSError:
        dr.rectangle([1386, 80, 1776, 665], outline=rgb("33382C"))
    dr.text((1386, 670), "Backdrop plate concept (1024x1536): root-trunks,", fill=rgb("CBD0BE"), font=font(12))
    dr.text((1386, 686), "dimmed sun, overgrown towers, gloamlight fungi", fill=rgb("CBD0BE"), font=font(12))

    # Enemy recolours: foundry vs gloam on the ash floor.
    ey = 452
    floor_tile = gloam_surface("FoundryPavingV10.png", 5101, 0.30, 0.80, 0.9).resize((736, 214), Image.LANCZOS)
    im.paste(floor_tile, (640, ey))
    dr.text((640, ey + 218), "Enemies: foundry (left of each pair) and Gloam bloom (right): charred bark, verdigris, orchid bloom",
            fill=rgb("CBD0BE"), font=font(12))
    ex = 648
    for folder, fn, label in ENEMIES:
        try:
            src = load_rgba(os.path.join(CHARS, folder, fn))
        except OSError:
            continue
        boost = 0.3 if folder == "ForgeWarden" else 0.0
        value = 1.1 if folder == "ForgeWarden" else 1.3
        tall = {"CinderMite": 70, "Runner": 120}.get(folder, 140)
        sprites = [fit(trim(Image.fromarray(arr)), tall) for arr in (src, bloom(src, boost=boost, value=value))]
        cell = 182
        scale = min(1.0, (cell - 8) / float(sum(sp.width for sp in sprites)))
        sprites = [sp.resize((max(1, int(sp.width * scale)), max(1, int(sp.height * scale))), Image.LANCZOS) for sp in sprites]
        cx = ex
        for spr in sprites:
            im.paste(spr, (cx, ey + 180 - spr.height), spr)
            cx += spr.width
        dr.text((ex + 4, ey + 186), label, fill=rgb("E6E8DC"), font=font(13), stroke_width=2, stroke_fill=rgb("0B0D09"))
        ex += cell

    # Readability strip: the real heroes, a gloam enemy, telegraph, XP, damage number on the floor values.
    rx, ry = 24, 700
    dr.text((rx, ry), "Readability on the floor values: Vanguard, Ilya, Gloam Grunt, slam telegraph, XP crystal, damage number",
            fill=rgb("E6E8DC"), font=font(15))
    try:
        van = fit(trim(Image.open(VAN).convert("RGBA")), 120)
    except OSError:
        van = None
    try:
        ilya = fit(trim(ilya_cell()), 130)
    except (OSError, IndexError):
        ilya = None
    try:
        grunt = fit(trim(Image.fromarray(bloom(load_rgba(os.path.join(CHARS, "SlagBrute", "CHR_Grunt_front-idle.tga"))))), 90)
    except OSError:
        grunt = None
    try:
        xp = fit(trim(Image.open(XP).convert("RGBA")), 34)
    except OSError:
        xp = None
    floors = [("1D1F1A", "deep"), ("3C3F36", "ash loam"), ("63665A", "light"), ("A7A99C", "ash drift - off-lane only")]
    for k, (hx, nm) in enumerate(floors):
        bx, by = rx + k * 330, ry + 30
        dr.rectangle([bx, by, bx + 318, by + 350], fill=rgb(hx))
        if van:
            im.paste(van, (bx + 20, by + 20), van)
        if ilya:
            im.paste(ilya, (bx + 130, by + 10), ilya)
        if grunt:
            im.paste(grunt, (bx + 215, by + 50), grunt)
        dr.ellipse([bx + 30, by + 170, bx + 190, by + 240], outline=rgb("FF5A3C"), width=6)
        if xp:
            im.paste(xp, (bx + 225, by + 180), xp)
        dr.ellipse([bx + 270, by + 190, bx + 290, by + 210], fill=rgb("E8B23A"), outline=rgb("0B0D09"), width=2)
        dr.text((bx + 215, by + 240), "123", fill=rgb("FFFFFF"), font=font(30), stroke_width=3, stroke_fill=rgb("0B0D09"))
        dr.text((bx + 8, by + 322), "#%s %s" % (hx, nm), fill=rgb("0B0D09") if k == 3 else rgb("E6E8DC"), font=font(13))
    dr.text((rx, ry + 392), "Rules: floor 10-45% luminance, saturation < 0.2; orchid belongs to enemies only; gloamlight never in "
            "lanes; telegraphs are the only red-orange; ash drift kills Ilya's robe (#D8CFC2) - keep it off the lanes",
            fill=rgb("9EA38E"), font=font(13))

    # The fight-lane numbers docs/51 section 7 holds the paving to.
    pav = np.asarray(gloam_surface("FoundryPavingV10.png", 5101, 0.30, 0.80, 0.9)).astype(np.float32) / 255
    mx, mn = pav.max(-1), pav.min(-1)
    print("paving preview: mean luminance %.3f, mean saturation %.3f" % (
        float((pav @ np.array([0.2126, 0.7152, 0.0722], np.float32)).mean()), float(((mx - mn) / np.maximum(mx, 1e-4)).mean())))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    im.save(out, optimize=True)
    print(out, im.size, os.path.getsize(out), "bytes")


if __name__ == "__main__":
    main()
