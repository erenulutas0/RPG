"""Floor 3 (Gloam Gardens) variants of the foundry's glowing textures, for FoundryRegionView.ApplyTheme.

GenerateIceGlowTextures.py's SOURCES list and masks, with docs/51's colours: each source's hot, saturated orange
(fire windows, lava cracks, coal, flame) becomes gloamlight #C6EE6A scaled by its brightness (never amber); bronze,
brass and gold become verdigris #6FA38C; everything else is desaturated toward ash-loam olive. A cut-out sprite
(alpha) gets a moss cap where the ice pass had rime: the few rows under every top edge of its silhouette go toward
moss #3F5A32-#4F6B3A. Alpha is kept. No orchid (hue 280-330 is the enemies' alone, docs/51 7.3). Outputs go to
Assets/_Project/Resources/Gloam/<same name>.png, loaded by name only on floor 3, with the import settings of the
Resources/Ice twin (its .meta copied under a fresh guid; an existing output keeps its guid).
Usage: python -I Tools/ArtReview/GenerateGloamGlowTextures.py
"""
import os
import re
import uuid

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DST = os.path.join(ROOT, "Assets", "_Project", "Resources", "Gloam")
ICE = os.path.join(ROOT, "Assets", "_Project", "Resources", "Ice")
GLOAMLIGHT = np.array([198, 238, 106], dtype=np.float32) / 255
OLIVE = np.array([0.97, 1.0, 0.88], dtype=np.float32)
VERDIGRIS = np.array([111, 163, 140], dtype=np.float32) / 255
MOSS = np.array([63, 90, 50], dtype=np.float32) / 255
MOSS_LIT = np.array([79, 107, 58], dtype=np.float32) / 255

# source, glow scale (the root well darker than the lanterns), optional value scale of the base (stone and iron
# that were only tinted before land at GloamPavingV1's value instead of lighter). The ice pass's list, one for one.
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
    ("Assets/_Project/Art/Props/Foundry/FoundryPavingV8.png", 0.85, 0.7),
    ("Assets/_Project/Art/Props/Foundry/FoundryIronV2.png", 0.85, 0.8),
    ("Assets/_Project/Art/Props/Foundry/FoundryCastIronV2.png", 0.85, 0.8),
    # The boiler wrap's fire window to a dim gloamlight.
    ("Assets/_Project/Art/Props/Foundry/FoundryBoilerWrap.png", 0.6, 0.85),
    ("Assets/_Project/Art/Environment/Platform/ENV_Platform_Floor.png", 0.85, 0.9),
]


def gloamify(rgba, glow_scale, value=1.0):
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
    base = (rgb * 0.25 + lum[..., None] * 0.75) * OLIVE * value
    glow = GLOAMLIGHT * np.clip(mx * 1.1, 0, 1)[..., None] * glow_scale
    verdigris = VERDIGRIS * np.clip(lum * 1.7, 0, 1.15)[..., None]
    out = base * (1 - hot - metal)[..., None] + glow * hot[..., None] + verdigris * metal[..., None]
    alpha = rgba[..., 3].astype(np.float32) / 255
    if alpha.min() < 0.5:
        # Moss cap: a pixel of the body with open air within `reach` rows above it.
        reach = max(4, rgba.shape[0] // 64)
        solid = alpha > 0.5
        open_above = np.zeros_like(solid)
        for k in range(1, reach + 1):
            shifted = np.ones_like(solid)
            shifted[k:] = ~solid[:-k]
            open_above |= shifted
        cap = (solid & open_above).astype(np.float32)
        # Soften one step so the cap is not a hard line; moss is darker than rime, so it covers a little less.
        cap[1:] = np.maximum(cap[1:], cap[:-1] * 0.5)
        cap *= 0.75
        moss = MOSS + (MOSS_LIT - MOSS) * np.clip(lum * 1.5, 0, 1)[..., None]
        out = out * (1 - cap[..., None]) + moss * cap[..., None]
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
    for entry in SOURCES:
        source, scale = entry[0], entry[1]
        value = entry[2] if len(entry) > 2 else 1.0
        path = os.path.join(ROOT, source)
        image = Image.open(path)
        mode = image.mode
        rgba = np.array(image.convert("RGBA"))
        out = Image.fromarray(gloamify(rgba, scale, value), "RGBA")
        if mode == "RGB":
            out = out.convert("RGB")
        name = os.path.basename(source)
        target = os.path.join(DST, name)
        out.save(target)
        meta = target + ".meta"
        guid = None
        if os.path.exists(meta):
            with open(meta) as f:
                guid = re.search(r"^guid: (\w+)", f.read(), re.M).group(1)
        # The Resources/Ice twin's import settings (the same as the source's), under this output's own guid.
        with open(os.path.join(ICE, name + ".meta"), newline="") as f:
            text = f.read()
        text = re.sub(r"^guid: \w+", "guid: " + (guid or uuid.uuid4().hex), text, count=1, flags=re.M)
        with open(meta, "w", newline="") as f:
            f.write(text)
        print(target, out.size, os.path.getsize(target))


if __name__ == "__main__":
    main()
