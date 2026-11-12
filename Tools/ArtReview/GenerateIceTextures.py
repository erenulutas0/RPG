"""Ice-map (floor 2) first textures: re-tint the foundry paving and masonry to a
cold, desaturated slate and lay a tiling frost overlay that gathers in the joints.

Joints and stone shapes stay exactly where the foundry rasters put them, so the
procedural layouts (FoundryRegionCourtFlagstones etc.) line up unchanged.
Output sizes equal the sources. Usage: python Tools/ArtReview/GenerateIceTextures.py
See docs/49_ICE_MAP_ART_DIRECTION.md.
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "Assets", "_Project", "Art", "Props", "Foundry")
DST = os.path.join(ROOT, "Assets", "_Project", "Art", "Environment", "Ice")


def hexrgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


# Palette (docs/49): floor slate ramp and frost.
SLATE_DEEP = hexrgb("1C2129")
SLATE_MID = hexrgb("3B4350")
SLATE_LIGHT = hexrgb("67717F")
FROST = hexrgb("B9CCDA")
RIME = hexrgb("DCE7EF")
GLAZE = hexrgb("8FC3D9")


def _gauss(h, w, sigma):
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    return np.exp(-(fx * fx + fy * fy) * (sigma * sigma) * 2.0 * np.pi ** 2)


def periodic_blur(a, sigma):
    """Gaussian blur that wraps, so a tiling texture stays tiling."""
    h, w = a.shape
    return np.real(np.fft.ifft2(np.fft.fft2(a) * _gauss(h, w, sigma))).astype(np.float32)


def periodic_noise(h, w, scale, rng):
    out = periodic_blur(rng.standard_normal((h, w)).astype(np.float32), scale)
    out -= out.mean()
    return out / (out.std() + 1e-6)


def ramp(t, stops):
    t = np.clip(t, 0, 1)
    n = len(stops) - 1
    seg = np.minimum((t * n).astype(int), n - 1)
    k = (t * n - seg)[..., None]
    a = np.stack(stops)[seg]
    b = np.stack(stops)[seg + 1]
    return a * (1 - k) + b * k


def icify(src_name, dst_name, seed, frost_amount, joint_frost, contrast):
    im = np.asarray(Image.open(os.path.join(SRC, src_name)).convert("RGB")).astype(np.float32) / 255.0
    h, w, _ = im.shape
    rng = np.random.default_rng(seed)
    lum = im @ np.array([0.299, 0.587, 0.114], np.float32)
    lo, hi = np.percentile(lum, 2), np.percentile(lum, 98)
    t = np.clip((lum - lo) / (hi - lo + 1e-6), 0, 1)
    t = 0.5 + (t - 0.5) * contrast
    base = ramp(t, [SLATE_DEEP, SLATE_MID, SLATE_LIGHT])

    # Joints = locally dark pixels; frost settles in them (rime in the crevices).
    local = periodic_blur(lum, 9.0)
    joint = np.clip((local - lum) * 9.0, 0, 1)
    joint = periodic_blur(joint, 1.5)

    n_big = periodic_noise(h, w, 60.0, rng)
    n_mid = periodic_noise(h, w, 14.0, rng)
    n_fine = periodic_noise(h, w, 2.0, rng)
    patch = np.clip((n_big * 0.7 + n_mid * 0.3) * 0.5 + frost_amount - 0.5, 0, 1)
    frost = np.clip(patch * (0.55 + 0.45 * np.clip(n_fine * 0.5 + 0.5, 0, 1)), 0, 1) * 0.55
    frost = np.maximum(frost, joint * joint_frost * np.clip(0.6 + 0.4 * n_mid, 0, 1))
    out = base * (1 - frost[..., None]) + FROST * frost[..., None]

    # Sparse rime glints, small and dim so the floor stays a mid-dark value.
    sp = (rng.random((h, w)) > 0.9985).astype(np.float32)
    glint = np.clip(periodic_blur(sp, 0.8) * 3.0 * np.clip(patch + joint, 0, 1), 0, 0.5)
    out = out * (1 - glint[..., None]) + RIME * glint[..., None]

    # Faint cold glaze across the stone faces (clear ice), not in the joints.
    sheen = np.clip(periodic_noise(h, w, 30.0, rng) * 0.5 + 0.5, 0, 1) * (1 - joint) * 0.05
    out = np.clip(out + sheen[..., None] * GLAZE, 0, 1)

    os.makedirs(DST, exist_ok=True)
    path = os.path.join(DST, dst_name)
    Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGB").save(path, optimize=True)
    print(dst_name, (w, h), os.path.getsize(path), "bytes; mean RGB", (out.mean(axis=(0, 1)) * 255).round(1))


if __name__ == "__main__":
    icify("FoundryPavingV10.png", "IcePavingV1.png", 4902, 0.38, 0.85, 0.9)
    icify("FoundryMasonryV4.png", "IceMasonryV1.png", 4903, 0.30, 0.75, 1.0)
