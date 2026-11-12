"""Gloam Gardens (floor 3) first textures: re-tint the foundry paving and masonry to the ash-loam ramp, gather
moss in the joints and in soft patches, and scatter a few pale ash flecks (no gloss, no glints).

The recipe is GenerateGloamMoodboard.gloam_surface (the mood board's preview), with its seeds and amounts.
Joints and stone shapes stay exactly where the foundry rasters put them, so the procedural layouts
(FoundryRegionCourtFlagstones etc.) line up unchanged. Output sizes equal the sources. Each output gets its
Ice twin's import settings (IcePavingV1.png / IceMasonryV1.png: the .meta copied under a fresh guid; an existing
output keeps its guid).
Usage: python -I Tools/ArtReview/GenerateGloamTextures.py
See docs/51_GLOAM_GARDENS_ART_DIRECTION.md.
"""
import os
import re
import uuid

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "Assets", "_Project", "Art", "Props", "Foundry")
DST = os.path.join(ROOT, "Assets", "_Project", "Art", "Environment", "Gloam")
ICE = os.path.join(ROOT, "Assets", "_Project", "Art", "Environment", "Ice")


def hexrgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


# Palette (docs/51): ash-loam ramp, moss, ash drift.
LOAM_DEEP = hexrgb("1D1F1A")
LOAM = hexrgb("3C3F36")
LOAM_LIGHT = hexrgb("63665A")
MOSS_DARK = hexrgb("2E4226")
MOSS = hexrgb("3F5A32")
MOSS_LIT = hexrgb("4F6B3A")
ASH = hexrgb("A7A99C")


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


def gloam(src_name, dst_name, twin_name, seed, moss_amount, joint_moss, contrast):
    im = np.asarray(Image.open(os.path.join(SRC, src_name)).convert("RGB")).astype(np.float32) / 255.0
    h, w, _ = im.shape
    rng = np.random.default_rng(seed)
    lum = im @ np.array([0.299, 0.587, 0.114], np.float32)
    lo, hi = np.percentile(lum, 2), np.percentile(lum, 98)
    t = 0.5 + (np.clip((lum - lo) / (hi - lo + 1e-6), 0, 1) - 0.5) * contrast
    out = ramp(t, [LOAM_DEEP, LOAM, LOAM_LIGHT])

    # Joints = locally dark pixels; moss gathers in them.
    joint = periodic_blur(np.clip((periodic_blur(lum, 9.0) - lum) * 9.0, 0, 1), 1.5)
    n_big = periodic_noise(h, w, 60.0, rng)
    n_mid = periodic_noise(h, w, 14.0, rng)
    n_fine = periodic_noise(h, w, 2.0, rng)
    patch = np.clip((n_big * 0.7 + n_mid * 0.3) * 0.5 + moss_amount - 0.5, 0, 1)
    moss = np.clip(patch * (0.5 + 0.5 * np.clip(n_fine * 0.5 + 0.5, 0, 1)), 0, 1) * 0.5
    moss = np.maximum(moss, joint * joint_moss * np.clip(0.55 + 0.45 * n_mid, 0, 1))
    moss_col = ramp(np.clip(n_fine * 0.25 + 0.5, 0, 1), [MOSS_DARK, MOSS, MOSS_LIT])
    out = out * (1 - moss[..., None]) + moss_col * moss[..., None]

    # Sparse pale ash flecks on the stone faces, never in the joints; dim so the floor stays mid-dark.
    fleck = (rng.random((h, w)) > 0.9982).astype(np.float32)
    fleck = np.clip(periodic_blur(fleck, 0.9) * 3.0, 0, 0.45) * (1 - joint)
    out = np.clip(out * (1 - fleck[..., None]) + ASH * fleck[..., None], 0, 1)

    path = os.path.join(DST, dst_name)
    Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGB").save(path, optimize=True)
    write_meta(os.path.join(ICE, twin_name + ".meta"), path + ".meta")
    mx, mn = out.max(-1), out.min(-1)
    print(dst_name, (w, h), os.path.getsize(path), "bytes; mean RGB", (out.mean(axis=(0, 1)) * 255).round(1),
          "mean luminance %.3f, mean saturation %.3f" % (
              float((out @ np.array([0.2126, 0.7152, 0.0722], np.float32)).mean()),
              float(((mx - mn) / np.maximum(mx, 1e-4)).mean())))


def folder_meta(path):
    meta = path + ".meta"
    if not os.path.exists(meta):
        with open(meta, "w", newline="\n") as f:
            f.write("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
                    "  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)


def write_meta(twin_meta, meta):
    guid = None
    if os.path.exists(meta):
        with open(meta) as f:
            guid = re.search(r"^guid: (\w+)", f.read(), re.M).group(1)
    with open(twin_meta, newline="") as f:
        text = f.read()
    text = re.sub(r"^guid: \w+", "guid: " + (guid or uuid.uuid4().hex), text, count=1, flags=re.M)
    with open(meta, "w", newline="") as f:
        f.write(text)


if __name__ == "__main__":
    os.makedirs(DST, exist_ok=True)
    folder_meta(DST)
    gloam("FoundryPavingV10.png", "GloamPavingV1.png", "IcePavingV1.png", 5101, 0.30, 0.80, 0.9)
    gloam("FoundryMasonryV4.png", "GloamMasonryV1.png", "IceMasonryV1.png", 5102, 0.36, 0.70, 1.0)
