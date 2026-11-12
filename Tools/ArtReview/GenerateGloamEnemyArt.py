"""Floor 3 (Gloam Gardens) copies of the foundry enemies' painted art, for EnemyArtSet.ForTheme(Gloam) (visual only,
docs/51).

GenerateIceEnemyArt.py's masks with GenerateGloamMoodboard.bloom's colours: the lava golems' rock becomes charred bark
(#4A3B33's warm brown, a step lighter than the ash-loam floor, the dark outline kept), brass becomes verdigris
#6FA38C, and the lava cracks, eyes, flame and embers become the orchid bloom - deep #5A1F66 in the dim edge, #D86BD0
in the crack, light #F6C2F0 in the core, capped short of the lightest so a thin crack never reads white. Orchid is
the enemies' colour on this floor alone (docs/51 7.3). The Forge Warden's core burns brighter (a boost). Per-set body
value: the Runner's drawing starts lighter than the golems' rock, and at the golems' value its bark lands near Ilya's
ash-white robe (the mood board), so it takes a lower one. Alpha is kept.

Every body, death, fall and own-death drawing of each set is copied to <set folder>/Gloam/<name>.png with the
source's import settings (its .meta copied under a fresh guid; an existing output keeps its guid). The hit flashes
are white silhouettes and are shared, not copied. Each set gets <Set>GloamArtSet.asset beside the copies: the set's
own asset with every copied drawing's guid swapped and no themed copies of its own, and the set's asset gains
`_gloam`, pointing at it.
Usage: python -I Tools/ArtReview/GenerateGloamEnemyArt.py
"""
import os
import re
import uuid

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CHARACTERS = os.path.join(ROOT, "Assets", "_Project", "Art", "Characters")
BLOOM_DEEP = np.array([90, 31, 102], dtype=np.float32) / 255  # #5A1F66
BLOOM = np.array([216, 107, 208], dtype=np.float32) / 255  # #D86BD0
BLOOM_LIGHT = np.array([246, 194, 240], dtype=np.float32) / 255  # #F6C2F0
VERDIGRIS = np.array([111, 163, 140], dtype=np.float32) / 255  # #6FA38C
BARK = np.array([1.10, 0.95, 0.84], dtype=np.float32)

# folder, set asset name, glow onset (the captain's red rock is warm too, so only its brighter lava glows), core
# boost, bark value (the Warden's steel starts lighter than the golems' rock; the Runner's drawing too)
SETS = [
    ("SlagBrute", "SlagBruteArtSet", 0.12, 0.0, 1.3),
    ("Runner", "RunnerArtSet", 0.12, 0.0, 1.0),
    ("Tank", "TankArtSet", 0.12, 0.0, 1.3),
    ("GruntCaptain", "GruntCaptainArtSet", 0.5, 0.0, 1.3),
    ("CinderMite", "CinderMiteArtSet", 0.12, 0.0, 1.3),
    ("ForgeWarden", "ForgeWardenArtSet", 0.12, 0.3, 1.1),
]


def smooth(x, lo, hi):
    t = np.clip((x - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)


def dilate(mask, steps):
    out = mask.copy()
    for _ in range(steps):
        grown = out.copy()
        grown[1:] |= out[:-1]
        grown[:-1] |= out[1:]
        grown[:, 1:] |= out[:, :-1]
        grown[:, :-1] |= out[:, 1:]
        out = grown
    return out


def bloom(rgba, onset, boost, value):
    rgb = rgba[..., :3].astype(np.float32) / 255
    mx = rgb.max(axis=-1)
    mn = rgb.min(axis=-1)
    lum = rgb @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    sat = np.where(mx > 1e-4, (mx - mn) / np.maximum(mx, 1e-4), 0)
    warm = smooth(rgb[..., 0] - rgb[..., 2], 0.03, 0.12)
    green = rgb[..., 1] / np.maximum(rgb[..., 0], 1e-4)
    # Lava is orange-red (little green); its yellow-white core sits in it. Brass keeps more green and lies apart.
    orange = (warm > 0.5) & (green < 0.6) & (sat > 0.55) & (mx > max(onset, 0.35))
    near_lava = dilate(orange, 3)
    brass = warm * smooth(green, 0.55, 0.62) * smooth(sat, 0.2, 0.3) * (1 - smooth(sat, 0.85, 0.95)) * ~near_lava
    hot = warm * smooth(sat, 0.3, 0.55) * smooth(mx, onset, onset + 0.25)
    # The core: bright warm pixels by the lava, or burning yellow-white anywhere (brass highlights stay saturated).
    hot_white = smooth(rgb[..., 0] - rgb[..., 2], -0.01, 0.04)
    core = hot_white * smooth(mx, 0.82, 0.95) * np.maximum(dilate(orange, 5), 1 - smooth(sat, 0.4, 0.55))
    hot = np.maximum(hot, core) * (1 - brass)
    hot = np.clip(hot * (1 + boost), 0, 1)
    # Orchid ramp by the lava's own brightness, capped short of the lightest; a boost (the Warden) lifts the dim edge.
    t = np.clip(lum / 0.8 + boost, 0, 0.85 - boost * 0.5)[..., None]
    low = BLOOM_DEEP + (BLOOM - BLOOM_DEEP) * np.clip(t * 2, 0, 1)
    high = BLOOM + (BLOOM_LIGHT - BLOOM) * np.clip(t * 2 - 1, 0, 1)
    glow = np.where(t < 0.5, low, high)
    # Charred bark: warm brown by luminance, lifted a step off the ash-loam floor; the outline stays dark.
    bark_value = np.clip(lum * value + 0.015, 0, 1)
    bark = (bark_value[..., None] * 0.9 + rgb * 0.1) * BARK
    verdigris = VERDIGRIS * np.clip(lum * 1.6, 0, 1.2)[..., None]
    b = brass[..., None]
    h = hot[..., None]
    out = bark * (1 - h - b * (1 - h)) + verdigris * b * (1 - h) + glow * h
    rgba = rgba.copy()
    rgba[..., :3] = np.clip(out * 255 + 0.5, 0, 255).astype(np.uint8)
    return rgba


def guid_of(meta_path):
    with open(meta_path) as f:
        return re.search(r"^guid: (\w+)", f.read(), re.M).group(1)


def write_meta(source_meta, target_meta):
    guid = guid_of(target_meta) if os.path.exists(target_meta) else uuid.uuid4().hex
    with open(source_meta, newline="") as f:
        text = f.read()
    text = re.sub(r"^guid: \w+", "guid: " + guid, text, count=1, flags=re.M)
    with open(target_meta, "w", newline="") as f:
        f.write(text)
    return guid


def folder_meta(path):
    meta = path + ".meta"
    if not os.path.exists(meta):
        with open(meta, "w", newline="\n") as f:
            f.write("fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
                    "  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % uuid.uuid4().hex)


def main():
    guid_to_path = {}
    for folder, _, files in os.walk(CHARACTERS):
        for name in files:
            if name.endswith(".meta"):
                path = os.path.join(folder, name)
                with open(path) as f:
                    m = re.search(r"^guid: (\w+)", f.read(300), re.M)
                if m:
                    guid_to_path[m.group(1)] = path[:-5]
    total = 0
    for folder, set_name, onset, boost, value in SETS:
        set_path = os.path.join(CHARACTERS, folder, set_name + ".asset")
        with open(set_path, newline="") as f:
            text = f.read()
        newline = "\r\n" if "\r\n" in text else "\n"
        gloam_dir = os.path.join(CHARACTERS, folder, "Gloam")
        os.makedirs(gloam_dir, exist_ok=True)
        folder_meta(gloam_dir)
        swaps = {}
        for line in text.splitlines():
            if "Flash:" in line or "m_Script" in line or "_frost" in line or "_gloam" in line:
                continue
            for guid in re.findall(r"guid: (\w+), type: 3", line):
                if guid in swaps:
                    continue
                source = guid_to_path[guid]
                image = Image.open(source)
                out = Image.fromarray(bloom(np.array(image.convert("RGBA")), onset, boost, value), "RGBA")
                stem = os.path.splitext(os.path.basename(source))[0]
                target = os.path.join(gloam_dir, stem + ".png")
                out.save(target, optimize=True)
                total += os.path.getsize(target)
                swaps[guid] = write_meta(source + ".meta", target + ".meta")
        gloam_name = set_name.replace("ArtSet", "GloamArtSet")
        gloam_text = re.sub(r"guid: (\w+), type: 3", lambda m: "guid: %s, type: 3" % swaps.get(m.group(1), m.group(1)), text)
        gloam_text = gloam_text.replace("m_Name: " + set_name, "m_Name: " + gloam_name)
        # The copy has no themed copies of its own.
        gloam_text = re.sub(r"\r?\n  _frost: [^\r\n]*", "", gloam_text)
        gloam_text = re.sub(r"\r?\n  _gloam: [^\r\n]*", "", gloam_text)
        gloam_text = (gloam_text.rstrip("\r\n") + newline + "  _frost: {fileID: 0}" + newline
                      + "  _gloam: {fileID: 0}" + newline)
        gloam_asset = os.path.join(gloam_dir, gloam_name + ".asset")
        gloam_guid = guid_of(gloam_asset + ".meta") if os.path.exists(gloam_asset + ".meta") else uuid.uuid4().hex
        with open(gloam_asset, "w", newline="") as f:
            f.write(gloam_text)
        with open(set_path + ".meta", newline="") as f:
            set_meta = f.read()
        with open(gloam_asset + ".meta", "w", newline="") as f:
            f.write(re.sub(r"^guid: \w+", "guid: " + gloam_guid, set_meta, count=1, flags=re.M))
        # The set's own `_gloam`, right after its `_frost` (the fields' order).
        gloam_line = "  _gloam: {fileID: 11400000, guid: %s, type: 2}" % gloam_guid
        if re.search(r"\n  _gloam:", text):
            text = re.sub(r"\n  _gloam: [^\r\n]*", "\n" + gloam_line, text)
        elif re.search(r"\n  _frost:", text):
            text = re.sub(r"(\n  _frost: [^\r\n]*)", lambda m: m.group(1) + newline + gloam_line, text, count=1)
        else:
            text = text.rstrip("\r\n") + newline + gloam_line + newline
        with open(set_path, "w", newline="") as f:
            f.write(text)
        print(folder, len(swaps), "copies ->", gloam_name)
    print("copies:", total, "bytes")


if __name__ == "__main__":
    main()
