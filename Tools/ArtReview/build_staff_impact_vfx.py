"""Painted Staff impact: the floor ring a Staff hit draws round its target, and the burst on the target.

The Staff's hit was the last core strike still drawn from code (StaffSplashArt: four thin violet arcs). This paints it
in the style of the painted strikes, skills and feedback (build_skill_vfx, build_combat_feedback_vfx), with the
procedural ring kept as the fallback:

  - Ring: a unit-radius floor ellipse, half as deep as it is wide, which CombatEffectsView scales to the shown part of
    the splash exactly as it scaled the procedural ring, so it covers the same floor at every Area. Over five frames
    (0.24 s): a flash on the floor at the target's feet, then a crisp white leading edge on a violet band that softens
    toward the target, over a thin dark rim (the rim is what reads on the Foundry's orange lava, the light edge what
    reads on its dark stone), arcane motes lifting off it, and the band tearing into four tapered arcs that swirl
    clockwise as they fade: the procedural ring's four arcs, painted.
  - Burst: the Staff's orb bursting on the struck body, upright over it as the other strikes are, over four frames
    (0.16 s): a white-hot heart in a lilac glow, then five comet wisps spiralling out clockwise, as the ring's arcs do,
    shedding motes. Curved, with no straight rays, so it never reads as the gold crit star or the Forge Burst's steel
    flash.

Colour: the Staff's own violet, sampled from its crystal (Art/Characters/Vanguard/WPN_Vanguard_Staff.tga), from its
deep shade to the pink-lilac of its highlight, which is the warm core. Never ember: ember-orange is Burn, the Crucible
Vent and the hit sparks, and steel blue is the Vanguard's Sword, Daggers and Forge Burst.

Both strips go on one sheet beside the painted slash and spark, Assets/_Project/Art/Effects/FoundryStrikes/
VFX_StaffBlast.png, and are written into FoundryStrikeArtSet.asset as the strike set's optional _staffRing and
_staffBurst (Cryptforge.Art.StrikeArtSet), every other line of the asset kept as it is. The painting primitives, packing
and export are build_skill_vfx's: frames are painted 4x supersampled in world units, box-filtered down in sRGB as the
project blends, and exported straight-alpha with colour bled into the clear texels. Deterministic: the same script
paints the same sheet. An existing .meta GUID is kept, so a repaint never breaks a reference.

Usage:
    python Tools/ArtReview/build_staff_impact_vfx.py               # the sheet, its meta and the strike set's strips
    python Tools/ArtReview/build_staff_impact_vfx.py --frames DIR  # also save every frame as a PNG, for review
"""
import argparse
import math
import os
import re

import numpy as np
from PIL import Image

import build_skill_vfx as skill
from build_skill_vfx import WHITE, Frame, Noise, blur, disc, hex_rgb, mix, smoothstep

ROOT = skill.ROOT
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "Effects", "FoundryStrikes")
ASSET_PATH = os.path.join(OUT_DIR, "FoundryStrikeArtSet.asset")
SHEET_NAME = "VFX_StaffBlast"

# The Staff's violet, from its crystal's deep shade to the pink-lilac of its highlight.
SOOT = hex_rgb("#160A2A")
DEEP = hex_rgb("#6623C2")
VIOLET = hex_rgb("#9140EA")
LILAC = hex_rgb("#C680FA")
PALE = hex_rgb("#EBB6FC")
HOT = hex_rgb("#F9DEFD")

# ----------------------------------------------------------------------------------------------------------------------
# The floor ring: a unit radius across (x), half as deep (y), as the procedural ring and the skills' rings are painted.

RING_TPU = 180.0  # the skills' floor rings' density
RING_FRAMES = 5
# Room round the unit contour for the dark rim and the motes that lift off the band (screen-up).
RING_EXTENT = (-1.1, -0.58, 1.1, 0.72)
ARCS = 4


def arc_profile(angle, offset, span):
    """How much of the band stands at each angle once it breaks into ARCS arcs centred offset + k * 90 degrees: each arc
    covers span of its quarter, its head (clockwise end) rising quickly and its tail tapering long, as the procedural
    ring's arcs do, so they swirl clockwise as they fade."""
    period = 2 * np.pi / ARCS
    head = offset - 0.5 * span * period
    p = np.mod(angle - head, period) / period
    return smoothstep(0.0, 0.16 * span, p) * smoothstep(span, 0.38 * span, p) * (p < span)


def paint_twinkle(frame, x, y, size, strength, color=HOT, glow=LILAC):
    """A small arcane mote: a soft lilac glow round a hot dot with a short upright and level glint through it."""
    frame.paint(glow, disc(frame, x, y, size * 2.2, soft=size * 2.0) * 0.45 * strength)
    dx, dy = np.abs(frame.X - x), np.abs(frame.Y - y)
    arm = size * 3.2
    level = np.clip((size * 0.42 * (1.0 - dx / arm) - dy) / frame.px + 0.5, 0.0, 1.0) * (dx < arm)
    upright = np.clip((size * 0.42 * (1.0 - dy / arm) - dx) / frame.px + 0.5, 0.0, 1.0) * (dy < arm)
    frame.paint(color, np.maximum(level, upright) * 0.85 * strength)
    frame.paint(color, disc(frame, x, y, size * 0.75, soft=size * 0.4) * strength)


def ring_frame(index, noise):
    """Frame index of the ring. The view grows it from 24/38 of its radius to all of it over its first 0.14 s and fades
    it over its last 0.11 s, so the frames carry the shape: the flash and the whole band while it grows, then the break
    into four arcs that swirl as it fades."""
    frame = Frame(*RING_EXTENT, RING_TPU)
    X, Y = frame.X, frame.Y
    d = np.sqrt(X ** 2 + (Y / 0.5) ** 2)
    angle = np.arctan2(Y / 0.5, X)
    ca, sa = np.cos(angle), np.sin(angle)
    # Inward from the contour, in units of the radius across; the edge wavers a little, as a painted line does.
    depth = (1.0 - d) + noise(ca * 0.7, sa * 0.7) * 0.005
    # By frame: band width, strength, the flash on the floor at the target, and the break into arcs (span of each arc's
    # quarter, None while whole) with the swirl so far (degrees clockwise).
    band, strength, flash, span, swirl = (
        (0.1, 1.0, 1.0, None, 0.0),
        (0.085, 1.0, 0.0, None, 0.0),
        (0.068, 0.95, 0.0, 0.88, 5.0),
        (0.052, 0.85, 0.0, 0.68, 11.0),
        (0.038, 0.7, 0.0, 0.46, 17.0))[index]
    band = band * (1.0 + 0.1 * noise(ca * 1.4 + 2.0, sa * 1.4))
    whole = np.ones_like(X)
    if span is not None:
        whole = arc_profile(angle, -math.radians(swirl), span)
        # Ragged where it tears rather than cut clean.
        whole = whole * smoothstep(-0.55, 0.1, noise(ca * 2.1 + 5.0, sa * 2.1) + whole * 0.8)
    whole = np.clip(whole, 0.0, 1.0)
    width = band * whole ** 0.6
    shown = (width > 1e-4).astype(np.float32)
    # Across the band: 0 at its leading edge, 1 at its inner side.
    across = np.clip(depth / np.maximum(width, 1e-4), 0.0, 4.0)

    if flash > 0:
        # The bolt's flash on the floor round the target's feet, small and gone in a frame.
        frame.paint(VIOLET, smoothstep(0.3, 0.0, d) ** 1.8 * 0.42 * flash)
        frame.paint(LILAC, smoothstep(0.17, 0.0, d) ** 1.4 * 0.7 * flash)
        frame.paint(HOT, smoothstep(0.075, 0.0, d) * 0.85 * flash)
        # A breath of light trailing the band toward the target, never a full disc: a wash muddies the lava.
        frame.paint(VIOLET, smoothstep(0.14, 0.0, depth - width) * (depth > width) * 0.12 * flash)
    # The dark rim just outside the leading edge, for the lava.
    frame.paint(SOOT, smoothstep(0.034, 0.0, -depth) * (depth <= 0.003) * 0.6 * strength * whole)
    # The band: crisp at its leading edge and soft toward the target, pale lilac deepening to violet as it trails.
    body = smoothstep(-0.004, 0.004, depth) * smoothstep(1.0, 0.4, across) * shown
    tone = mix(PALE, LILAC, smoothstep(0.0, 0.25, across))
    tone = mix(tone, VIOLET, smoothstep(0.2, 0.55, across))
    tone = mix(tone, DEEP, smoothstep(0.55, 1.0, across) * 0.8)
    # Light running round the band unevenly, as a brushed stroke catches it, and shifting frame to frame: never a flat
    # ring.
    sheen = noise(ca * 2.6 + 9.0, sa * 2.6 - 1.5 * index) * 0.5 + 0.5
    tone = mix(tone, PALE, smoothstep(0.55, 0.9, sheen) * smoothstep(0.75, 0.15, across) * 0.45)
    tone = mix(tone, DEEP, smoothstep(0.45, 0.1, sheen) * smoothstep(0.1, 0.6, across) * 0.35)
    frame.paint(LILAC, np.clip(blur(body, 2.5) * 0.5, 0.0, 0.24) * strength * (index < 3))
    frame.paint(tone, body * strength)
    # The leading edge: white as it flashes out, then the crystal's pale lilac.
    lead = smoothstep(0.02, 0.006, np.abs(depth - 0.009)) * np.clip(whole * 1.4, 0.0, 1.0) * shown
    frame.paint((WHITE, mix(WHITE, PALE, 0.55), PALE, PALE, PALE)[index], lead * strength)

    # Motes lifting off the band, rising as the ring fades.
    if index >= 1:
        rng = np.random.default_rng(7)
        for k in range(18):
            a = rng.uniform(0, 2 * np.pi) - math.radians(swirl)
            r = 1.0 - rng.uniform(0.0, 0.07)
            lift = (0.02 + 0.03 * rng.random()) * (index - 0.5)
            x, y = math.cos(a) * r, math.sin(a) * 0.5 * r + lift
            size = (0.009 + 0.006 * rng.random()) * (1.0 - 0.13 * (index - 1))
            fade = (1.0, 0.95, 0.8, 0.6)[index - 1] * (0.6 + 0.4 * rng.random())
            paint_twinkle(frame, x, y, size, fade)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# The burst: the Staff's orb detonating on the struck body, centred on it as the other strikes are.

BURST_TPU = 128.0  # the painted actors' density
BURST_FRAMES = 4
BURST_EXTENT = (-0.44, -0.44, 0.44, 0.44)
WISPS = 5
# How far a wisp turns clockwise as it flies out, in radians a world unit.
WISP_TWIST = 3.2


def wisp(frame, start, head, length, width):
    """Coverage and heat of a comet wisp flying out from the centre along a clockwise spiral: its head at radius head,
    its tail trailing length back toward the centre, thinning to a thread; heat 1 at the head and 0 at the tail."""
    body = np.zeros_like(frame.X)
    heat = np.zeros_like(frame.X)
    tail = max(0.015, head - length)
    for k in range(24):
        t = k / 23.0  # 0 at the tail, 1 at the head
        r = tail + (head - tail) * t
        a = start - WISP_TWIST * r
        x, y = math.cos(a) * r, math.sin(a) * r
        # Fullest just behind the head, which narrows to a point, and thinning back to a thread at the tail.
        radius = width * (0.15 + 0.85 * t ** 1.3) * min(1.0, (1.0 - t) / 0.3 + 0.35)
        d = np.sqrt((frame.X - x) ** 2 + (frame.Y - y) ** 2)
        cover = np.clip((radius - d) / frame.px + 0.5, 0.0, 1.0) * (0.35 + 0.65 * t)
        body = np.maximum(body, cover)
        heat = np.maximum(heat, cover * t)
    return body, heat


def burst_frame(index):
    """The Staff's orb bursting on the struck body: a white-hot heart in a lilac glow, then five comet wisps spiralling
    out clockwise, as the floor ring's arcs swirl, shedding motes as they burn out."""
    frame = Frame(*BURST_EXTENT, BURST_TPU)
    r = np.hypot(frame.X, frame.Y)
    # By frame: the heart's radius, the glow's, the wisps' head radius, length and width, and the strength of it all.
    heart, glow, head, length, width, strength = (
        (0.062, 0.27, 0.075, 0.045, 0.024, 1.0),
        (0.036, 0.24, 0.18, 0.13, 0.03, 1.0),
        (0.0, 0.18, 0.26, 0.14, 0.023, 0.85),
        (0.0, 0.12, 0.31, 0.11, 0.015, 0.6))[index]
    # The glow the heart leaves dies with it, so the last frame is the wisps alone.
    frame.paint(VIOLET, smoothstep(glow, 0.0, r) ** 1.6 * (0.5, 0.5, 0.36, 0.16)[index] * strength)
    body = np.zeros_like(r)
    heat = np.zeros_like(r)
    for k in range(WISPS):
        start = 2 * np.pi * (k + 0.3 * math.sin(k * 2.7)) / WISPS + 0.4
        reach = head * (0.88 + 0.24 * ((k * 0.618) % 1.0))
        b, h = wisp(frame, start, reach, length, width)
        body = np.maximum(body, b)
        heat = np.maximum(heat, h)
    frame.paint(SOOT, blur(body, 1.0) * 0.22 * strength)
    frame.paint(LILAC, np.clip(blur(body, 2.5) * 0.7, 0.0, 0.4) * strength)
    frame.paint(mix(DEEP, VIOLET, smoothstep(0.0, 0.3, heat)), blur(body, 0.3) * strength)
    frame.paint(LILAC, blur(smoothstep(0.25, 0.55, heat), 0.5) * strength)
    frame.paint(HOT, blur(smoothstep(0.6, 0.85, heat), 0.6) * strength)
    frame.paint(WHITE, blur(smoothstep(0.93, 0.99, heat), 0.5) * strength * (index < 2))
    if heart > 0:
        frame.paint(LILAC, disc(frame, 0.0, 0.0, heart * 2.0, soft=heart * 1.6) * 0.8 * strength)
        frame.paint(HOT, disc(frame, 0.0, 0.0, heart * 1.25, soft=heart * 0.8) * strength)
        frame.paint(WHITE, disc(frame, 0.0, 0.0, heart * 0.8, soft=heart * 0.4) * strength)
    if index >= 1:
        # Motes shed behind the wisps, drifting out.
        for k in range(WISPS):
            start = 2 * np.pi * (k + 0.3 * math.sin(k * 2.7)) / WISPS + 0.4
            at = max(0.03, head * (0.56 + 0.12 * ((k * 0.618) % 1.0)))
            a = start - WISP_TWIST * at + 0.35
            paint_twinkle(frame, math.cos(a) * at, math.sin(a) * at, (0.012, 0.01, 0.008)[index - 1],
                          (0.9, 0.75, 0.5)[index - 1])
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# The strike set: its staff strips are replaced, every other line kept as it is.


def write_asset(strips, guid, sheet):
    text = open(ASSET_PATH, encoding="utf-8", newline="").read()
    # Drop any staff strips an earlier run wrote: they are the last fields of the set.
    kept = re.split(r"(?m)^  _staffRing:\n", text)[0]
    if not kept.endswith("\n"):
        kept += "\n"
    named = {s.name: s for s in strips}
    lines = []
    for field, name in (("_staffRing", "staffRing"), ("_staffBurst", "staffBurst")):
        lines.append(f"  {field}:")
        lines.append(skill.strip_yaml(named[name], guid, sheet, 4))
    skill.write(ASSET_PATH, kept + "\n".join(lines) + "\n")


def build_strips():
    noise = Noise(41, period=1.0, feature=0.08)
    return [skill.Strip("staffRing", [ring_frame(i, noise) for i in range(RING_FRAMES)]),
            skill.Strip("staffBurst", [burst_frame(i) for i in range(BURST_FRAMES)])]


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--frames", help="also save every frame as a PNG in this directory")
    args = parser.parse_args()

    strips = build_strips()
    sheet = skill.bleed(skill.pack(strips))
    png = os.path.join(OUT_DIR, SHEET_NAME + ".png")
    Image.fromarray(sheet, "RGBA").save(png, optimize=True)
    meta = png + ".meta"
    guid = skill.existing_guid(meta)
    skill.write(meta, skill.TEXTURE_META.format(guid=guid, sprite_id=skill.existing_sprite_id(meta),
                                                tpu=skill.number(RING_TPU)))
    write_asset(strips, guid, sheet)
    size = skill.astc_bytes(sheet.shape[1], sheet.shape[0])
    print(f"{SHEET_NAME}.png {sheet.shape[1]}x{sheet.shape[0]}  ASTC 4x4 {size / 1024:.0f} KiB "
          f"({size / 1048576:.2f} MiB)")
    if args.frames:
        os.makedirs(args.frames, exist_ok=True)
        for s in strips:
            for i, img in enumerate(s.images):
                Image.fromarray(img, "RGBA").save(os.path.join(args.frames, f"{s.name}_{i}.png"))


if __name__ == "__main__":
    main()
