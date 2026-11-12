"""Floor 2 (ice) copies of the foundry enemies' painted art, for EnemyArtSet.Frosted (visual only, docs/49).

The lava golems turn frosted: their rock becomes cold silver-grey stone a step lighter than the slate floor (the
dark outline kept), brass becomes frosted silver, and the lava cracks, eyes, flame and embers become violet - the
Silver Wisp's #8B6CE8 (docs/44 F2), rising to a lighter violet where the lava burned yellow-white, kept apart from
the region's quicksilver cyan. The Forge Warden's violet core burns brighter. Alpha is kept.

Every body, death, fall and own-death drawing of each set is copied to <set folder>/Ice/<name>.png with the
source's import settings (its .meta copied under a fresh guid; an existing output keeps its guid). The hit flashes
are white silhouettes and are shared, not copied. Each set gets <Set>Ice.asset beside the copies: the set's own
asset with every copied drawing's guid swapped, and the set's asset gains `_frost`, pointing at it.
Usage: python Tools/ArtReview/GenerateIceEnemyArt.py
"""
import os
import re
import uuid

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CHARACTERS = os.path.join(ROOT, "Assets", "_Project", "Art", "Characters")
VIOLET_DEEP = np.array([58, 38, 120], dtype=np.float32) / 255
VIOLET = np.array([139, 108, 232], dtype=np.float32) / 255  # #8B6CE8
VIOLET_LIGHT = np.array([196, 178, 255], dtype=np.float32) / 255
SILVER = np.array([169, 180, 191], dtype=np.float32) / 255  # docs/49 silver
COLD = np.array([0.93, 0.98, 1.07], dtype=np.float32)

# folder, set asset name, glow onset (the captain's red rock is warm too, so only its brighter lava glows), core
# boost, stone value (the Warden's steel starts lighter than the golems' rock)
SETS = [
    ("SlagBrute", "SlagBruteArtSet", 0.12, 0.0, 1.4),
    ("Runner", "RunnerArtSet", 0.12, 0.0, 1.4),
    ("Tank", "TankArtSet", 0.12, 0.0, 1.4),
    ("GruntCaptain", "GruntCaptainArtSet", 0.5, 0.0, 1.4),
    ("CinderMite", "CinderMiteArtSet", 0.12, 0.0, 1.4),
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


def frost(rgba, onset, boost, value):
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
    # White-hot reads warm by a hair only, so the core's warmth starts at neutral (the steel's highlights are cool).
    hot_white = smooth(rgb[..., 0] - rgb[..., 2], -0.01, 0.04)
    core = hot_white * smooth(mx, 0.82, 0.95) * np.maximum(dilate(orange, 5), 1 - smooth(sat, 0.4, 0.55))
    hot = np.maximum(hot, core) * (1 - brass)
    hot = np.clip(hot * (1 + boost), 0, 1)
    # Violet ramp by the lava's own brightness: deep in the dim edge, #8B6CE8 in the crack, light in the core.
    # Capped short of the lightest violet so a thin crack never reads white; a boost (the Warden) lifts the dim edge.
    # The Warden's core (a boost) stays nearer #8B6CE8 itself: saturated rather than pale, the stronger violet.
    t = np.clip(lum / 0.8 + boost, 0, 0.85 - boost * 0.5)[..., None]
    low = VIOLET_DEEP + (VIOLET - VIOLET_DEEP) * np.clip(t * 2, 0, 1)
    high = VIOLET + (VIOLET_LIGHT - VIOLET) * np.clip(t * 2 - 1, 0, 1)
    glow = np.where(t < 0.5, low, high)
    # Cold silver-grey stone: grey by luminance, lifted a step off the slate floor; the outline stays dark.
    stone_value = np.clip(lum * value + 0.015, 0, 1)
    stone = (stone_value[..., None] * 0.9 + rgb * 0.1) * COLD
    silver = SILVER * np.clip(lum * 1.6, 0, 1.2)[..., None]
    b = brass[..., None]
    h = hot[..., None]
    out = stone * (1 - h - b * (1 - h)) + silver * b * (1 - h) + glow * h
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
    for folder, set_name, onset, boost, value in SETS:
        set_path = os.path.join(CHARACTERS, folder, set_name + ".asset")
        with open(set_path, newline="") as f:
            text = f.read()
        ice_dir = os.path.join(CHARACTERS, folder, "Ice")
        os.makedirs(ice_dir, exist_ok=True)
        folder_meta(ice_dir)
        swaps = {}
        for line in text.splitlines():
            if "Flash:" in line or "m_Script" in line or "_frost" in line or "_gloam" in line:
                continue
            for guid in re.findall(r"guid: (\w+), type: 3", line):
                if guid in swaps:
                    continue
                source = guid_to_path[guid]
                image = Image.open(source)
                out = Image.fromarray(frost(np.array(image.convert("RGBA")), onset, boost, value), "RGBA")
                stem = os.path.splitext(os.path.basename(source))[0]
                target = os.path.join(ice_dir, stem + ".png")
                out.save(target, optimize=True)
                swaps[guid] = write_meta(source + ".meta", target + ".meta")
        ice_name = set_name.replace("ArtSet", "IceArtSet")
        ice_text = re.sub(r"guid: (\w+), type: 3", lambda m: "guid: %s, type: 3" % swaps.get(m.group(1), m.group(1)), text)
        ice_text = ice_text.replace("m_Name: " + set_name, "m_Name: " + ice_name)
        ice_text = re.sub(r"\n  _frost: [^\n]*", "", ice_text)
        # Nor the garden's copy (GenerateGloamEnemyArt.py): a frosted set has no themed copies of its own.
        ice_text = re.sub(r"\n  _gloam: [^\n]*", "", ice_text)
        newline = "\r\n" if "\r\n" in text else "\n"
        ice_text = ice_text.rstrip("\r\n") + newline + "  _frost: {fileID: 0}" + newline
        ice_asset = os.path.join(ice_dir, ice_name + ".asset")
        ice_guid = guid_of(ice_asset + ".meta") if os.path.exists(ice_asset + ".meta") else uuid.uuid4().hex
        with open(ice_asset, "w", newline="") as f:
            f.write(ice_text)
        with open(set_path + ".meta", newline="") as f:
            set_meta = f.read()
        with open(ice_asset + ".meta", "w", newline="") as f:
            f.write(re.sub(r"^guid: \w+", "guid: " + ice_guid, set_meta, count=1, flags=re.M))
        frost_line = "  _frost: {fileID: 11400000, guid: %s, type: 2}" % ice_guid
        if "\n  _frost:" in text:
            text = re.sub(r"\n  _frost: [^\r\n]*", "\n" + frost_line, text)
        else:
            text = text.rstrip("\r\n") + newline + frost_line + newline
        with open(set_path, "w", newline="") as f:
            f.write(text)
        print(folder, len(swaps), "copies ->", ice_name)


if __name__ == "__main__":
    main()
