"""The Cinder Saw's painted blade and edge ring (docs/63 section 13), for the strike set's optional _sawBlade and _sawRing
(Cryptforge.Art.StrikeArtSet), with EffectArt's pixel blade and the Staff's procedural ring kept as the fallback.

  - Blade: the rendered blade (BuildBlenderCinderSaw.py) turning a fifth of a turn over BLADE_FRAMES frames, played in a
    loop while the Saw circles the hero, pinned at its axle. Each 4x render is box-filtered down in linear light with
    premultiplied alpha; the emission pass's heat is coloured from straw at its edge to white at its core and added to
    the iron; the renders of the turn just behind each frame are laid under it as a faint smear, their hot points
    trailing into short arcs; then the one-texel dark outline round the blade, and the heat's soft bloom over it all in
    light, so a hot point never gets a dark ring.
  - Ring: the faint ring the aura's edge shows once a second round the hero, a unit-radius floor ellipse half as deep as
    it is wide, as the Staff's painted ring, which CombatEffectsView scales to the radius it marks: a thin yellow-white
    line, brighter at the leading end of each of its RING_TEETH serrations so it reads as the saw's own cut, over a
    straw glow and a faint dark rim for the lava, fading and breaking up over its three frames (0.21 s, the procedural
    ring's time).

Colour: the blade's heat and the ring are yellow-white, hue 45-50 degrees, pale - never Burn's ember (#E77232, hue 21) nor
the red-orange docs/52 section 2.1 rule 3 keeps for what hurts on a timer (#FF5A3C, hue 9), nor the orchid docs/51 gives
the enemies; the iron is a cool dark grey. EditMode's PaintedCinderSawTests reads every texel back.

Both strips go on one sheet, Assets/_Project/Art/Effects/FoundryStrikes/VFX_CinderSaw.png, imported as the Staff's
painted sheet is (build_skill_vfx's TEXTURE_META: bilinear, no mipmaps, ASTC 4x4 on Android at the sheet's full size),
and are written into FoundryStrikeArtSet.asset after the staff strips, every other line of the asset kept as it is
(build_skill_vfx.write_fields). The packing and export are build_skill_vfx's; the ring is painted with its primitives,
4x supersampled. Deterministic: the same renders pack the same sheet. An existing .meta GUID is kept, so a repaint never
breaks a reference.

Usage:
    blender --background --factory-startup --python Tools/ArtReview/BuildBlenderCinderSaw.py      # the renders first
    python Tools/ArtReview/build_cinder_saw_vfx.py                 # the sheet, its meta and the strike set's strips
    python Tools/ArtReview/build_cinder_saw_vfx.py --render DIR    # renders from elsewhere (default the Blender default)
    python Tools/ArtReview/build_cinder_saw_vfx.py --frames DIR    # also save every frame as a PNG, for review
"""
import argparse
import json
import math
import os

import numpy as np
from PIL import Image

import build_skill_vfx as skill
from build_skill_vfx import Frame, hex_rgb, mix, smoothstep

ROOT = skill.ROOT
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "Effects", "FoundryStrikes")
ASSET_PATH = os.path.join(OUT_DIR, "FoundryStrikeArtSet.asset")
SHEET_NAME = "VFX_CinderSaw"
RENDER = os.path.join(ROOT, "TestResults", "cinder-saw-render")

# The heat, straw at its edge to white at its core (hue 47 degrees, the core nearly white), with its gain; the bloom's
# tight and wide halos (sigma in texels, gain); how much of the bloom's light becomes cover over the clear texels.
HEAT_EDGE = "#FFD95A"
HEAT_CORE = "#FFFBEC"
HEAT_GAIN = 1.25
BLOOM = ((1.1, 0.85), (3.4, 0.5))
HALO_ALPHA = 0.8
# The rendered roster's outline (BuildBlenderVanguard.py's OUTLINE).
OUTLINE = "#120F1D"
# The smear under each frame: the renders of the turn just behind it, weighted toward the newest, at this opacity; their
# hot points' light at this share of the blade's own.
SMEAR_BODY = 0.32
SMEAR_HEAT = 0.75

# The ring.
RING_TPU = 128.0
RING_FRAMES = 3
RING_EXTENT = (-1.08, -0.58, 1.08, 0.58)
RING_TEETH = 24
RING_SOOT = hex_rgb("#16141C")


def to_linear(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def linear_hex(value):
    return to_linear(hex_rgb(value).astype(np.float64))


def gaussian(image, sigma):
    radius = max(1, int(sigma * 3 + 0.5))
    x = np.arange(-radius, radius + 1)
    kernel = np.exp(-x * x / (2 * sigma * sigma))
    kernel /= kernel.sum()
    out = image
    for axis in (0, 1):
        padded = np.pad(out, [(radius, radius) if a == axis else (0, 0) for a in range(out.ndim)])
        acc = np.zeros_like(out)
        for k, weight in enumerate(kernel):
            acc += weight * (padded[k:k + out.shape[0]] if axis == 0 else padded[:, k:k + out.shape[1]])
        out = acc
    return out


# ----------------------------------------------------------------------------------------------------------------------
# The blade, from the renders.


def downsample(path, ss):
    """The render at the game's resolution: premultiplied linear colour and coverage."""
    a = np.asarray(Image.open(path).convert("RGBA")).astype(np.float64) / 255.0
    alpha = a[..., 3:4]
    lin = to_linear(a[..., :3]) * alpha
    h, w = alpha.shape[0] // ss, alpha.shape[1] // ss
    return lin.reshape(h, ss, w, ss, 3).mean(axis=(1, 3)), alpha.reshape(h, ss, w, ss, 1).mean(axis=(1, 3))


def heat_of(path, ss, scale):
    """The emission pass's heat (its red channel) at the game's resolution, coverage-weighted as the beauty is."""
    a = np.asarray(Image.open(path).convert("RGBA")).astype(np.float64) / 255.0
    m = a[..., 0:1] * a[..., 3:4] / scale
    h, w = m.shape[0] // ss, m.shape[1] // ss
    return m.reshape(h, ss, w, ss, 1).mean(axis=(1, 3))


def light_of(heat):
    """The heat's light, linear: straw at its edge to white at its core, scaled by the heat."""
    t = np.clip(heat, 0, 1)
    colour = linear_hex(HEAT_EDGE) * (1 - t) + linear_hex(HEAT_CORE) * t
    return colour * heat * HEAT_GAIN


def outline_cover(alpha):
    """The body's coverage grown by a texel (the diagonals a little less): the outline laid under it."""
    pad = np.pad(alpha[..., 0], 1)
    grown = np.maximum.reduce([pad[1:-1, 1:-1], pad[:-2, 1:-1], pad[2:, 1:-1], pad[1:-1, :-2], pad[1:-1, 2:],
                               0.7 * pad[:-2, :-2], 0.7 * pad[2:, 2:], 0.7 * pad[:-2, 2:], 0.7 * pad[2:, :-2]])
    return grown[..., None]


def over(top, bottom):
    """Premultiplied source-over of (colour, alpha) pairs."""
    (tc, ta), (bc, ba) = top, bottom
    return tc + bc * (1 - ta), ta + ba * (1 - ta)


def with_light(layer, light, cover):
    """Light added to a layer, the light's own brightness (scaled by cover) becoming coverage over clear texels."""
    colour, alpha = layer
    amount = np.clip(light.max(axis=-1, keepdims=True), 0, 1) * cover
    return colour + light, alpha + amount * (1 - alpha)


def blade_frame(record, render, meta):
    ss, scale = meta["supersample"], meta["glow_scale"]
    steps = record["steps"]
    body = downsample(os.path.join(render, steps[0]["file"]), ss)
    heat = heat_of(os.path.join(render, steps[0]["glow"]), ss, scale)
    # The turn just behind: weighted toward the newest, so the smear fades along its trail.
    weights = [len(steps) - j for j in range(1, len(steps))]
    total = float(sum(weights))
    smear_colour = np.zeros_like(body[0])
    smear_alpha = np.zeros_like(body[1])
    smear_heat = np.zeros_like(heat)
    for weight, step in zip(weights, steps[1:]):
        colour, alpha = downsample(os.path.join(render, step["file"]), ss)
        smear_colour += colour * weight / total
        smear_alpha += alpha * weight / total
        smear_heat += heat_of(os.path.join(render, step["glow"]), ss, scale) * weight / total
    smear = (smear_colour * SMEAR_BODY, smear_alpha * SMEAR_BODY)
    trail = light_of(smear_heat) * SMEAR_HEAT
    smear = with_light(smear, trail, 1.0)
    # The outline under the blade and over the smear, then the blade with its heat.
    ink = outline_cover(body[1])
    layer = over((linear_hex(OUTLINE) * ink, ink), smear)
    own = light_of(heat)
    layer = over((body[0] + own, body[1]), layer)
    # The bloom of every hot point, the blade's own and its trail's, over it all.
    light = own + trail
    bloom = sum(gaussian(light, sigma) * gain for sigma, gain in BLOOM)
    colour, alpha = with_light(layer, bloom, HALO_ALPHA)
    return finish(colour, alpha)


def finish(colour, alpha):
    """Straight-alpha sRGB bytes (image order, row 0 at the top); a last invisible tail (under 2/255) cleared."""
    alpha = np.clip(alpha, 0, 1)
    straight = np.where(alpha > 1e-6, colour / np.maximum(alpha, 1e-6), 0)
    out = np.concatenate([to_srgb(straight), alpha], axis=-1)
    out = (out * 255 + 0.5).astype(np.uint8)
    out[out[..., 3] < 2] = 0
    return out


class Rendered:
    """A strip of finished frames (image order) cropped alike, with the pivot every frame pins at: the shape
    build_skill_vfx's pack and strip_yaml take."""

    def __init__(self, name, images, tpu, pivot_x, pivot_y_from_top):
        x0, y0, x1, y1 = skill.crop_box(name, images)
        self.name = name
        self.images = [img[y0:y1, x0:x1] for img in images]
        self.tpu = tpu
        width, height = x1 - x0, y1 - y0
        self.pivot = ((pivot_x - x0) / width, (y1 - pivot_y_from_top) / height)
        self.cells = []


def build_blade(render):
    meta = json.loads(open(os.path.join(render, "frames.json"), encoding="utf-8").read())
    frames = sorted(meta["frames"], key=lambda r: r["frame"])
    images = [blade_frame(record, render, meta) for record in frames]
    px, py = meta["pivot"]
    return Rendered("sawBlade", images, float(meta["ppu"]), px, meta["canvas"] - py), meta


# ----------------------------------------------------------------------------------------------------------------------
# The ring, painted.


def ring_frame(index):
    """Frame index of the edge ring: a unit floor ellipse whose thin line is serrated like the blade's cut, brightest at
    the leading (counter-clockwise) end of each serration; whole and bright, then dimmer and turned on a little, then
    thin and breaking up."""
    frame = Frame(*RING_EXTENT, RING_TPU)
    X, Y = frame.X, frame.Y
    d = np.sqrt(X ** 2 + (Y / 0.5) ** 2)
    angle = np.arctan2(Y / 0.5, X)
    # Inward from the contour, in units of the radius across.
    depth = 1.0 - d
    strength, width, swirl, broken = ((1.0, 0.011, 0.0, 0.0), (0.78, 0.009, 0.25, 0.0), (0.5, 0.007, 0.5, 0.45))[index]
    # Each serration ramps from its trailing end to its bright leading end.
    phase = np.mod(angle / (2 * np.pi) * RING_TEETH - swirl, 1.0)
    serration = 0.45 + 0.55 * phase ** 1.6
    if broken > 0:
        # The last frame keeps the bright leading halves only.
        serration = serration * smoothstep(broken - 0.1, broken + 0.1, phase)
    # A faint dark rim just outside the line, so it reads on the foundry's lava.
    frame.paint(RING_SOOT, smoothstep(0.035, 0.0, -depth) * (depth < -0.004) * 0.32 * strength)
    # The straw glow round the line, then the line, straw at its edges and white-hot along its middle.
    glow = smoothstep(0.07, 0.0, np.abs(depth))
    frame.paint(hex_rgb(HEAT_EDGE), glow ** 1.6 * 0.42 * strength * serration)
    line = smoothstep(width, width * 0.35, np.abs(depth))
    tone = mix(hex_rgb(HEAT_EDGE), hex_rgb(HEAT_CORE), serration)
    frame.paint(tone, line * strength * (0.55 + 0.45 * serration))
    return frame


def build_ring():
    return skill.Strip("sawRing", [ring_frame(i) for i in range(RING_FRAMES)])


# ----------------------------------------------------------------------------------------------------------------------


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--render", default=RENDER, help="the Blender renders and frames.json")
    parser.add_argument("--frames", help="also save every frame as a PNG in this directory")
    args = parser.parse_args()

    blade, meta = build_blade(args.render)
    ring = build_ring()
    strips = [blade, ring]
    sheet = skill.bleed(skill.pack(strips))
    png = os.path.join(OUT_DIR, SHEET_NAME + ".png")
    Image.fromarray(sheet, "RGBA").save(png, optimize=True)
    meta_path = png + ".meta"
    guid = skill.existing_guid(meta_path)
    skill.write(meta_path, skill.TEXTURE_META.format(guid=guid, sprite_id=skill.existing_sprite_id(meta_path),
                                                     tpu=skill.number(blade.tpu)))
    skill.write_fields(ASSET_PATH, [(field, f"  {field}:\n" + skill.strip_yaml(strip, guid, sheet, 4))
                                    for field, strip in (("_sawBlade", blade), ("_sawRing", ring))],
                       after="_staffBurst")
    size = skill.astc_bytes(sheet.shape[1], sheet.shape[0])
    print(f"{SHEET_NAME}.png {sheet.shape[1]}x{sheet.shape[0]}  ASTC 4x4 {size / 1024:.0f} KiB ({size / 1048576:.2f} MiB)"
          f"  blade {len(blade.images)} frames {blade.images[0].shape[1]}x{blade.images[0].shape[0]} at {blade.tpu:g}"
          f"  ring {len(ring.images)} frames {ring.images[0].shape[1]}x{ring.images[0].shape[0]} at {ring.tpu:g}")
    if args.frames:
        os.makedirs(args.frames, exist_ok=True)
        for s in strips:
            for i, img in enumerate(s.images):
                Image.fromarray(img, "RGBA").save(os.path.join(args.frames, f"{s.name}_{i}.png"))


if __name__ == "__main__":
    main()
