"""Floor 2 (ice) variants of the foundry's glowing textures, for FoundryRegionView.ApplyTheme.

Each source's hot, saturated orange (fire windows, lava cracks, coal, flame) becomes docs/49's quicksilver glow
#7FE3FF scaled by its brightness; everything else is desaturated and cooled toward slate. Alpha is kept. Outputs go to
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

# source, glow scale (the pit darker than the fires)
SOURCES = [
    ("Assets/_Project/Art/Props/Foundry/FoundryEmbers.png", 0.85),
    ("Assets/_Project/Art/Props/Foundry/FoundryPitLining.png", 0.65),
    ("Assets/_Project/Resources/FoundryBoiler.png", 0.85),
    ("Assets/_Project/Resources/FoundryShortBoiler.png", 0.85),
]


def icify(rgba, glow_scale):
    rgb = rgba[..., :3].astype(np.float32) / 255
    mx = rgb.max(axis=-1)
    mn = rgb.min(axis=-1)
    lum = rgb @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    sat = np.where(mx > 1e-4, (mx - mn) / np.maximum(mx, 1e-4), 0)
    warm = (rgb[..., 0] > rgb[..., 2]).astype(np.float32)
    hot = np.clip((sat - 0.4) * 3, 0, 1) * np.clip((mx - 0.35) / 0.35, 0, 1) * warm
    base = (rgb * 0.25 + lum[..., None] * 0.75) * COOL
    glow = QUICKSILVER * np.clip(mx * 1.1, 0, 1)[..., None] * glow_scale
    out = base * (1 - hot[..., None]) + glow * hot[..., None]
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
