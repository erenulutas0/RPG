"""Floor 2 (ice) variants of the foundry's glowing textures, for FoundryRegionView.ApplyTheme.

Each source's hot, saturated orange (fire windows, lava cracks, coal, flame) becomes docs/49's quicksilver glow
#7FE3FF scaled by its brightness; bronze, brass and gold become frosted silver #A9B4BF; everything else is desaturated
and cooled toward slate. A cut-out sprite (alpha) gets a rime cap: the few rows under every top edge of its silhouette
go toward snow #E4ECF2. Alpha is kept. Outputs go to
Assets/_Project/Resources/Ice/<same name>.png, loaded by name only on the ice floor, with the source's import settings
(its .meta copied under a fresh guid; an existing output keeps its guid).
Usage: python Tools/ArtReview/GenerateIceGlowTextures.py
"""
import os
import re
import uuid

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DST = os.path.join(ROOT, "Assets", "_Project", "Resources", "Ice")
QUICKSILVER = np.array([127, 227, 255], dtype=np.float32) / 255
COOL = np.array([0.90, 0.96, 1.06], dtype=np.float32)
SILVER = np.array([169, 180, 191], dtype=np.float32) / 255
SNOW = np.array([228, 236, 242], dtype=np.float32) / 255

# source, glow scale (the pit darker than the fires)
SOURCES = [
    ("Assets/_Project/Art/Props/Foundry/FoundryEmbers.png", 0.85),
    ("Assets/_Project/Art/Props/Foundry/FoundryPitLining.png", 0.65),
    ("Assets/_Project/Resources/FoundryBoiler.png", 0.85),
    ("Assets/_Project/Resources/FoundryShortBoiler.png", 0.85),
    ("Assets/_Project/Art/Props/Foundry/ForgeKeeper.png", 0.85),
    ("Assets/_Project/Resources/FoundryCrane.png", 0.85),
    ("Assets/_Project/Resources/FoundryStockHoist.png", 0.85),
    ("Assets/_Project/Resources/FoundryStockStation.png", 0.85),
    ("Assets/_Project/Resources/FoundryCastingBench.png", 0.85),
    ("Assets/_Project/Resources/FoundryReservoir.png", 0.85),
    ("Assets/_Project/Resources/FoundrySalvage.png", 0.85),
]


def icify(rgba, glow_scale):
    rgb = rgba[..., :3].astype(np.float32) / 255
    mx = rgb.max(axis=-1)
    mn = rgb.min(axis=-1)
    lum = rgb @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    sat = np.where(mx > 1e-4, (mx - mn) / np.maximum(mx, 1e-4), 0)
    warm = (rgb[..., 0] > rgb[..., 2]).astype(np.float32)
    green = rgb[..., 1] / np.maximum(rgb[..., 0], 1e-4)
    # Fire is orange-red (little green); bronze and gold keep more green.
    fire = np.clip((0.72 - green) * 6, 0, 1)
    hot = np.clip((sat - 0.4) * 3, 0, 1) * np.clip((mx - 0.35) / 0.35, 0, 1) * warm * fire
    metal = np.clip((sat - 0.18) * 4, 0, 1) * warm * (1 - hot)
    base = (rgb * 0.25 + lum[..., None] * 0.75) * COOL
    glow = QUICKSILVER * np.clip(mx * 1.1, 0, 1)[..., None] * glow_scale
    silver = SILVER * np.clip(lum * 1.7, 0, 1.15)[..., None]
    out = base * (1 - hot - metal)[..., None] + glow * hot[..., None] + silver * metal[..., None]
    alpha = rgba[..., 3].astype(np.float32) / 255
    if alpha.min() < 0.5:
        # Rime cap: a pixel of the body with open air within `reach` rows above it.
        reach = max(4, rgba.shape[0] // 64)
        solid = alpha > 0.5
        open_above = np.zeros_like(solid)
        for k in range(1, reach + 1):
            shifted = np.ones_like(solid)
            shifted[k:] = ~solid[:-k]
            open_above |= shifted
        rime = (solid & open_above).astype(np.float32)
        # Soften one step so the cap is not a hard line.
        rime[1:] = np.maximum(rime[1:], rime[:-1] * 0.5)
        rime *= 0.85
        snow = SNOW * (0.75 + 0.25 * lum)[..., None]
        out = out * (1 - rime[..., None]) + snow * rime[..., None]
    rgba = rgba.copy()
    rgba[..., :3] = np.clip(out * 255 + 0.5, 0, 255).astype(np.uint8)
    return rgba


def main():
    os.makedirs(DST, exist_ok=True)
    folder_meta = DST + ".meta"
    if not os.path.exists(folder_meta):
        with open(folder_meta, "w", newline="\n") as f:
            f.write("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
                    "  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)
    for source, scale in SOURCES:
        path = os.path.join(ROOT, source)
        image = Image.open(path)
        mode = image.mode
        rgba = np.array(image.convert("RGBA"))
        out = Image.fromarray(icify(rgba, scale), "RGBA")
        if mode == "RGB":
            out = out.convert("RGB")
        target = os.path.join(DST, os.path.basename(source))
        out.save(target)
        meta = target + ".meta"
        guid = None
        if os.path.exists(meta):
            guid = re.search(r"^guid: (\w+)", open(meta).read(), re.M).group(1)
        text = open(path + ".meta", newline="").read()
        text = re.sub(r"^guid: \w+", "guid: " + (guid or uuid.uuid4().hex), text, count=1, flags=re.M)
        with open(meta, "w", newline="") as f:
            f.write(text)
        print(target, out.size)


if __name__ == "__main__":
    main()
