"""Painted run feedback: the run's growth on the hero (docs/43 item 1, RunFeedbackView).

The level-up burst, the taken card's ring, the evolution and the weapon picked up were the last effects still drawn as
thin vector lines (LineRenderers on one Sprites/Default material). This paints them in the family of the painted strikes,
skills and feedback (build_skill_vfx, build_combat_feedback_vfx, build_staff_impact_vfx), with the lines kept as the
fallback:

  - Level ring: a ring of warm light spreading over the floor at the hero's feet - a white-gold line in a gold band and
    a soft amber glow, glints riding it, dissolving into them as it fades. Light rather than a tier's band, and a step
    outside the card's ring as the two spread together after the pick a level-up opens, so the tier still reads.
  - Card ring: a bold band in the card's tier colour, a lit line just inside its leading edge, over a thin dark rim (the
    rim is what reads on the Foundry's orange lava, the band what reads on its dark stone), softening toward the hero,
    thinning and fraying as it fades. Painted light and neutral: RunFeedbackView tints it in the tier's colour
    (RunChoiceView's four), so every tier keeps its exact hue. The gold card's ring and the evolution's are the same
    frames, wider.
  - Card core: the bright core a pick flashes first, a white-hot floor flash with short spokes of light opening into a
    ring that thins; neutral too, tinted in the tier's pale colour.
  - Ember ring: the evolution's second ring and the weapon's small one, a molten band in the forge's ember (white-hot
    edge, yellow to crimson as it cools) throwing spatter as it breaks into glowing beads: forge metal, not the Crucible
    Vent's wall of flame.
  - Sparks: the streaks the bursts send up, a hot head and a tail cooling behind it, facing +x with the head at the
    pivot: gold light with a glint for a level-up and a gold card, the forge's ember for an evolution and a weapon.

Every ring is a unit floor ellipse, half as deep as it is wide (ArenaFloor.DepthScale), whose leading edge lies a world
unit out at scale 1: the view scales it to the outer edge of the line it stands for, so it covers the floor the line
did, and shows the frame for the share of its life it has lived. Each ring's frames are painted for the radius that
line reaches over that share (RunFeedbackView's envelopes, below), so its band is about as wide on screen as the line
was, a little wider, and thins toward the far and near sides less than the floor does, so it never turns to a hair.

Everything goes on one sheet, Assets/_Project/Art/Effects/RunFeedback/VFX_RunFeedback.png, and RunFeedbackArtSet.asset
is written with every strip's cells, pivot and density (Cryptforge.Art.RunFeedbackArtSet). The painting primitives,
packing and export are build_skill_vfx's: frames are painted 4x supersampled in world units, box-filtered down in sRGB as
the project blends, and exported straight-alpha with colour bled into the clear texels. Deterministic: the same script
paints the same sheet. Existing .meta GUIDs are kept, so a repaint never breaks a reference.

Usage:
    python Tools/ArtReview/build_run_feedback_vfx.py               # the sheet, its meta and the art set
    python Tools/ArtReview/build_run_feedback_vfx.py --frames DIR  # also save every frame as a PNG, for review
"""
import argparse
import math
import os

import numpy as np
from PIL import Image

import build_skill_vfx as skill
from build_combat_feedback_vfx import paint_glint
from build_skill_vfx import (CRIMSON, CRIMSON_DEEP, EMBER, EMBER_DEEP, WHITE_HOT, YELLOW, Frame, Noise, disc, hex_rgb,
                             mix, smoothstep)

ROOT = skill.ROOT
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "Effects", "RunFeedback")
SCRIPT_META = os.path.join(ROOT, "Assets", "_Project", "Scripts", "Art", "Unity", "RunFeedbackArtSet.cs.meta")
ASSET_PATH = os.path.join(OUT_DIR, "RunFeedbackArtSet.asset")
SHEET_NAME = "VFX_RunFeedback"

DEPTH = 0.5  # ArenaFloor.DepthScale: the floor is seen at half its depth
# How far a band keeps its width toward the far and near sides of its ellipse: 0 would halve it there as the floor
# does, 1 keep it as the lines did.
FORESHORTEN = 0.6

# RunFeedbackView's lines, each as (from radius, to radius, from width, to width): the radius eases out cubically over
# the line's life and the width falls linearly. A painted ring's frames are painted for the outer edge of the first one
# that uses them; the others are the same frames at their own reach.
LEVEL = (0.2, 0.95, 0.1, 0.02)
CARD = (0.2, 0.85, 0.08, 0.02)  # and the gold card's (0.2, 1.3, 0.12, 0.03) and the evolution's (0.2, 1.25, 0.13, 0.03)
CORE = (0.12, 0.3, 0.22, 0.06)  # and the gold card's and the evolution's, a little larger
EMBER_RING = (0.15, 0.95, 0.1, 0.025)  # the evolution's; the weapon's (0.1, 0.45, 0.06, 0.015) is the same frames, small

# The level-up's light: white gold in a gold band over amber.
LIGHT = hex_rgb("#FFF9E6")
GOLD_PALE = hex_rgb("#FFE69E")
GOLD = hex_rgb("#FFC84A")
AMBER = hex_rgb("#EE9A2E")
RIM_WARM = hex_rgb("#22140C")
FORGE_SOOT = hex_rgb("#1E0E0C")
# A streak's heart, body, glow and tail's end: the level-up's gold light, and the forge's ember (build_skill_vfx's).
GOLD_SPARK = (LIGHT, GOLD_PALE, GOLD, AMBER)
EMBER_SPARK = (WHITE_HOT, YELLOW, EMBER, CRIMSON)


def grey(value):
    return np.array([value, value, value], np.float32)


def greys(values):
    return np.repeat(np.asarray(values, np.float32)[..., None], 3, axis=-1)


def ease(t):
    return 1.0 - (1.0 - t) ** 3


def design_share(index, frames):
    """The share of its life a frame is painted for: two thirds of the way through the stretch it shows over, so the band
    it starts at, small and spreading fast, gives way to the next frame's without a jump."""
    return (index + 0.65) / frames


def reach(line, t):
    """The outer edge of a RunFeedbackView line at share t of its life: its eased radius and half its width."""
    r0, r1, w0, w1 = line
    return r0 + (r1 - r0) * ease(t) + 0.5 * width(line, t)


def width(line, t):
    return line[2] + (line[3] - line[2]) * t


def ring_space(frame, R):
    """For a unit ring shown at reach R: every sample's distance inward from the leading edge in world units, as wide on
    screen across the floor and a little narrower up and down it (FORESHORTEN), its angle round the ring, and its
    distance out along the floor ellipse (1 on the contour)."""
    d = np.sqrt(frame.X ** 2 + (frame.Y / DEPTH) ** 2)
    a = np.arctan2(frame.Y / DEPTH, frame.X)
    gradient = np.sqrt(np.cos(a) ** 2 + (np.sin(a) / DEPTH) ** 2)
    return (1.0 - d) * R / gradient ** FORESHORTEN, a, d


# Nothing of a ring reaches further past its contour than this share of it, so a frame painted for a small reach keeps
# the cell as tight as the rest.
OUTSIDE = 0.055


def band(x, inner, outer, soft):
    """Coverage of the span [inner, outer] of x, its edges softened over soft either side."""
    return smoothstep(inner - soft, inner + soft, x) * smoothstep(outer + soft, outer - soft, x)


def breakup(noise, a, amount, seed):
    """1 where a ring still stands round its angle a and 0 where it has frayed away: none of it at amount 0, most of it
    at 1, in ragged runs rather than a clean dash. The noise repeats every world unit, so a seed is a fraction of one."""
    if amount <= 0.0:
        return np.ones_like(a)
    n = noise(np.cos(a) * 1.9 + seed, np.sin(a) * 1.9 - seed) * 0.5 + 0.5
    fine = noise(np.cos(a) * 5.3 - seed, np.sin(a) * 5.3 + 0.7 * seed) * 0.5 + 0.5
    field = 0.75 * n + 0.25 * fine
    return smoothstep(amount - 0.12, amount + 0.12, field)


def sheen(noise, a, index, seed):
    """Light running unevenly round a ring, shifting frame to frame, in [0, 1]."""
    return noise(np.cos(a) * 2.3 + seed, np.sin(a) * 2.3 - 0.9 * index) * 0.5 + 0.5


def scatter(count, seed):
    """Fixed, uneven places round a ring: (angle, size share, phase) for each of count."""
    rng = np.random.default_rng(seed)
    out = []
    for k in range(count):
        a = 2 * np.pi * (k + rng.uniform(-0.32, 0.32)) / count
        out.append((a, rng.uniform(0.7, 1.15), rng.uniform(0.0, 2 * np.pi)))
    return out


def on_ring(angle, inward, R):
    """The frame point at angle round the unit ring, inward world units in from its leading edge."""
    r = 1.0 - inward / R
    return math.cos(angle) * r, math.sin(angle) * DEPTH * r


def paint_streak(frame, x, y, angle, length, head, glow, palette, strength=1.0, cool=0.0):
    """A hot streak flying along angle (degrees) with its head at (x, y): a soft glow round a hot head and a tail
    trailing back along its flight, thinning and cooling to nothing. palette: its heart, body, glow and tail's end."""
    heart, body, halo, end = palette
    c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
    u = (frame.X - x) * c + (frame.Y - y) * s
    v = -(frame.X - x) * s + (frame.Y - y) * c
    along = np.clip(-u / length, 0.0, 1.0)
    half = head * 0.8 * (1.0 - along) ** 1.3
    tail = np.clip((half - np.abs(v)) / frame.px + 0.5, 0.0, 1.0) * (u <= 0.0) * (u > -length)
    frame.paint(halo, disc(frame, x, y, glow, soft=glow * 0.8) * 0.42 * strength)
    frame.paint(mix(body, end, np.clip(along * (1.0 + cool), 0.0, 1.0)), tail * (1.0 - along ** 1.6) * strength)
    frame.paint(mix(heart, body, cool), disc(frame, x, y, head, soft=head * 0.3) * strength)


# ----------------------------------------------------------------------------------------------------------------------
# The level-up: a ring of light. Its frames are painted for the level line's reach (LEVEL); the view shows frame k over
# the k-th fifth of the half second.

LEVEL_FRAMES = 5
LEVEL_TPU = 100.0
LEVEL_EXTENT = (-1.3, -0.75, 1.3, 0.9)


def level_ring_frame(index, noise):
    t = design_share(index, LEVEL_FRAMES)
    R = reach(LEVEL, t)
    frame = Frame(*LEVEL_EXTENT, LEVEL_TPU)
    dw, a, _ = ring_space(frame, R)
    soft = 0.5 * R / LEVEL_TPU  # about half a texel, in world units
    B = 1.2 * width(LEVEL, t)
    # By frame: how far the ring has frayed, how big its glints are and how far its motes have lifted (world units).
    fray, glint, lift = ((0.0, 0.5, 0.0), (0.0, 0.9, 0.01), (0.06, 1.0, 0.024), (0.3, 0.9, 0.038),
                         (0.58, 0.7, 0.05))[index]
    out = OUTSIDE * R
    standing = breakup(noise, a, fray, 0.31)
    light = sheen(noise, a, index, 0.62)
    # A soft amber glow either side, wider toward the hero, and a faint warm shadow beyond it so the light still parts
    # from the orange lava.
    frame.paint(RIM_WARM, band(dw, -min(0.05, out - 0.006), -min(0.022, 0.45 * out), 0.006) * 0.22 * standing)
    # Under the band and past it either side, so the light never dips between the band and its glow.
    halo = smoothstep(-min(0.03, out), 0.0, dw) * smoothstep(B + 0.07, 0.5 * B, dw)
    frame.paint(AMBER, halo ** 1.4 * 0.42 * standing)
    # The band: amber at its soft inner side, gold, pale gold and a white-gold line just inside its leading edge.
    across = np.clip(dw / B, 0.0, 1.0)
    body = band(dw, 0.0, B, soft) * smoothstep(1.0, 0.55, across) ** 0.8 * standing
    tone = mix(AMBER, GOLD, smoothstep(0.95, 0.5, across))
    tone = mix(tone, GOLD_PALE, smoothstep(0.5, 0.22, across) * (0.7 + 0.3 * light))
    frame.paint(tone, body)
    frame.paint(LIGHT, band(dw, 0.12 * B, 0.36 * B, soft) * standing * (0.8 + 0.2 * light))
    # Glints riding the band, twinkling frame to frame, lifting a little as the ring dissolves into them.
    for k, (angle, size, phase) in enumerate(scatter(10, 61)):
        twinkle = 0.5 + 0.5 * math.sin(phase + 2.2 * index)
        s = 0.046 * size * glint * twinkle
        if s < 0.012:
            continue
        x, y = on_ring(angle, 0.3 * B, R)
        y += lift * (0.5 + ((k * 0.618) % 1.0)) / R
        paint_glint(frame, x, y, s / R, min(1.0, 0.7 + 0.4 * twinkle), color=LIGHT, glow=GOLD, rays=4,
                    angle=45.0 if k % 3 == 0 else 0.0)
    # Motes of light drifting up off the ring once it has spread.
    if index >= 1:
        for k, (angle, size, phase) in enumerate(scatter(10, 67 + index)):
            x, y = on_ring(angle, 0.45 * B, R)
            y += (0.012 + 0.03 * size + lift) / R
            frame.paint(GOLD, disc(frame, x, y, 0.017 / R, soft=0.015 / R) * 0.5)
            frame.paint(LIGHT, disc(frame, x, y, 0.0075 / R, soft=0.004 / R) * 0.9)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# The taken card: its ring and the bright core before it, painted light and neutral for the view's tier tint.

CARD_FRAMES = 6
CARD_TPU = 100.0
CARD_EXTENT = (-1.3, -0.75, 1.3, 0.8)
CORE_FRAMES = 4
CORE_TPU = 64.0
CORE_EXTENT = (-1.3, -0.72, 1.3, 0.72)


def card_ring_frame(index, noise):
    t = design_share(index, CARD_FRAMES)
    R = reach(CARD, t)
    frame = Frame(*CARD_EXTENT, CARD_TPU)
    dw, a, _ = ring_space(frame, R)
    soft = 0.5 * R / CARD_TPU
    B = 1.3 * width(CARD, t)
    # By frame: how dark its rim is (none while the core still flashes over it) and how far the band has frayed.
    rim, fray = ((0.0, 0.0), (0.7, 0.0), (1.0, 0.0), (1.0, 0.1), (1.0, 0.34), (1.0, 0.6))[index]
    standing = breakup(noise, a, fray, 0.57)
    light = sheen(noise, a, index, 0.14)
    # The dark rim just outside the leading edge, for the lava.
    frame.paint(grey(0.06), band(dw, -0.014, 0.0, soft) * 0.46 * rim * standing)
    # A soft glow of the tier's light trailing the band toward the hero, from under it, so it never dips between them.
    frame.paint(grey(0.92), smoothstep(0.0, 0.004, dw) * smoothstep(B + 0.045, 0.5 * B, dw) * 0.22 * standing)
    # The band: the tier's full colour in a lit line just inside its leading edge, a shade deeper either side of it and
    # deeper still toward the hero, brushed by the sheen, soft at its inner side.
    across = np.clip(dw / B, 0.0, 1.0)
    lit = band(across, 0.12, 0.42, 0.08)
    value = 0.86 + 0.14 * lit - 0.14 * smoothstep(0.45, 1.0, across) - 0.1 * (1.0 - light) * (1.0 - lit)
    body = band(dw, 0.0, B, soft) * smoothstep(1.0, 0.6, across) * standing
    frame.paint(greys(value), body)
    # A few glints catch on the band as it settles, in the tier's own full colour.
    if 2 <= index <= 4:
        for k, (angle, size, phase) in enumerate(scatter(7, 71)):
            twinkle = 0.5 + 0.5 * math.sin(phase + 2.3 * index)
            s = 0.026 * size * twinkle
            if s < 0.01:
                continue
            x, y = on_ring(angle, 0.3 * B, R)
            paint_glint(frame, x, y, s / R, 0.95, color=grey(1.0), glow=grey(0.95), rays=4)
    return frame


def card_core_frame(index):
    """The pick's bright core: a floor flash with short spokes of light, opening into a thick ring that thins. Painted
    with its outer edge on the unit contour; the view scales it to the core line's outer edge."""
    frame = Frame(*CORE_EXTENT, CORE_TPU)
    d = np.sqrt(frame.X ** 2 + (frame.Y / DEPTH) ** 2)
    a = np.arctan2(frame.Y / DEPTH, frame.X)
    # By frame: where the ring's inner side has opened to (share of the contour, 0 for a whole disc), how bright the
    # middle still is, how far the spokes reach past the contour and how strong they are, and the whole flash's strength.
    opening, middle, spokes, spoke_strength, strength = ((0.0, 1.0, 0.08, 0.8, 1.0), (0.46, 0.3, 0.0, 0.0, 1.0),
                                                         (0.77, 0.05, 0.0, 0.0, 0.95), (0.87, 0.0, 0.0, 0.0, 0.85))[index]
    soft = 0.03
    # A soft light round the flash on the floor.
    frame.paint(grey(0.9), smoothstep(1.07, 1.0, d) * (d >= 1.0) * 0.4 * strength)
    if spokes > 0:
        rays = np.clip(np.cos(a * 6.0 + 0.4) ** 2, 0.0, 1.0) ** 12 * (0.7 + 0.3 * np.cos(a * 12.0 + 1.1))
        frame.paint(grey(1.0), rays * smoothstep(1.0 + spokes, 0.7, d) * smoothstep(0.35, 0.6, d) * spoke_strength)
    inside = smoothstep(1.0 + soft, 1.0 - soft, d)
    # The ring, brightest near its outer edge and soft toward its opening; the whole disc while it has none.
    across = np.clip((1.0 - d) / max(1.0 - opening, 0.05), 0.0, 1.0)
    ring = inside
    if opening > 0.0:
        ring = inside * smoothstep(opening - 0.12, opening + 0.08, d) * smoothstep(1.0, 0.45, across) ** 0.6
    # The middle, under the ring, so the two never part.
    frame.paint(grey(0.95), np.clip(inside - ring, 0.0, 1.0) * middle * strength)
    # White-hot at its heart while it flashes whole, then brightest at the ring's outer edge.
    frame.paint(greys(1.0 - 0.08 * d if opening <= 0.0 else 1.0 - 0.12 * across), ring * strength)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# The ember ring: the evolution's second ring (painted for its reach, EMBER_RING) and the weapon's small one.

EMBER_FRAMES = 5
EMBER_TPU = 100.0
EMBER_EXTENT = (-1.3, -0.75, 1.3, 0.95)


def ember_ring_frame(index, noise):
    t = design_share(index, EMBER_FRAMES)
    R = reach(EMBER_RING, t)
    frame = Frame(*EMBER_EXTENT, EMBER_TPU)
    dw, a, _ = ring_space(frame, R)
    soft = 0.5 * R / EMBER_TPU
    B = 1.25 * width(EMBER_RING, t)
    # By frame: how hot it still is (1 white-hot to 0 crimson), how far it has broken into beads and how far its
    # spatter has flown (world units).
    heat, fray, fling = ((1.0, 0.0, 0.0), (0.85, 0.0, 0.03), (0.65, 0.12, 0.05), (0.45, 0.36, 0.07),
                         (0.26, 0.6, 0.085))[index]
    out = OUTSIDE * R
    standing = breakup(noise, a, fray, 0.83)
    flicker = noise(np.cos(a) * 3.1 + 0.45, np.sin(a) * 3.1 + 1.3 * index) * 0.5 + 0.5
    # Soot just outside the band and a glow of heat round it.
    frame.paint(FORGE_SOOT, band(dw, -min(0.018, 0.6 * out), 0.0, soft) * (0.25 if index == 0 else 0.6) * standing)
    glow = smoothstep(-min(0.055, out), -min(0.018, 0.6 * out), dw) * smoothstep(B + 0.05, 0.5 * B, dw)
    frame.paint(mix(CRIMSON, EMBER, heat), glow * (0.22 + 0.2 * heat) * standing)
    across = np.clip(dw / B, 0.0, 1.0)
    body = band(dw, 0.0, B, soft) * smoothstep(1.0, 0.65, across) * standing
    # Hottest at the leading edge, cooling toward the hero; the whole of it cooling frame by frame.
    local = np.clip(heat * (1.12 - 0.7 * across) + 0.14 * (flicker - 0.5), 0.0, 1.0)
    tone = mix(CRIMSON_DEEP, CRIMSON, smoothstep(0.0, 0.25, local))
    tone = mix(tone, EMBER_DEEP, smoothstep(0.2, 0.42, local))
    tone = mix(tone, EMBER, smoothstep(0.38, 0.6, local))
    tone = mix(tone, YELLOW, smoothstep(0.6, 0.82, local))
    tone = mix(tone, WHITE_HOT, smoothstep(0.84, 0.98, local))
    frame.paint(tone, body)
    # Spatter flung up and out of the band, each drop at its own distance, cooling as it flies.
    if fling > 0:
        rng = np.random.default_rng(79)
        for k, (angle, size, phase) in enumerate(scatter(11, 73)):
            far = fling * (0.45 + 0.55 * rng.random())
            out = 1.0 + (0.004 + 0.35 * far) / R
            up = far * (0.5 + 0.6 * rng.random()) / R
            x, y = math.cos(angle) * out, math.sin(angle) * DEPTH * out + up
            flight = math.degrees(math.atan2(0.5 + up * R / max(far, 1e-3), math.cos(angle)))
            paint_streak(frame, x, y, flight, (0.026 + 0.3 * far) / R, 0.0075 * size / R, 0.016 * size / R, EMBER_SPARK,
                         cool=min(1.0, 1.0 - heat + 0.3 * ((k * 0.37) % 1.0)))
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# The sparks: streaks facing +x with their heads at the pivot, painted at the level-up's rising spark's size - gold
# light for a level-up and a gold card, ember for an evolution and a weapon picked up.

SPARK_FRAMES = 3
SPARK_TPU = 128.0
SPARK_EXTENT = (-0.22, -0.09, 0.09, 0.09)
SPARK_WIDTH = 0.05  # RunFeedbackView's rising spark line: the view scales a spark by its own line's width over this


def spark_frame(index, palette, glints):
    frame = Frame(*SPARK_EXTENT, SPARK_TPU)
    # By frame: the tail's length, the head's radius, the glow's, the glint's size (0 for none) and how far it has
    # cooled.
    length, head, glow, glint, cool = ((0.16, 0.019, 0.036, 0.024, 0.0), (0.14, 0.017, 0.031, 0.015, 0.25),
                                       (0.11, 0.014, 0.026, 0.0, 0.55))[index]
    paint_streak(frame, 0.0, 0.0, 0.0, length, head, glow, palette, cool=cool)
    if glints and glint > 0:
        paint_glint(frame, 0.0, 0.0, glint, 1.0, color=palette[0], glow=palette[1], rays=4, angle=45.0 * index)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# The sheet and the art set.


def build_strips():
    noise = Noise(83, period=1.0, feature=0.08)
    return [skill.Strip("levelRing", [level_ring_frame(i, noise) for i in range(LEVEL_FRAMES)]),
            skill.Strip("cardRing", [card_ring_frame(i, noise) for i in range(CARD_FRAMES)]),
            skill.Strip("cardCore", [card_core_frame(i) for i in range(CORE_FRAMES)]),
            skill.Strip("emberRing", [ember_ring_frame(i, noise) for i in range(EMBER_FRAMES)]),
            skill.Strip("goldSpark", [spark_frame(i, GOLD_SPARK, True) for i in range(SPARK_FRAMES)]),
            skill.Strip("emberSpark", [spark_frame(i, EMBER_SPARK, False) for i in range(SPARK_FRAMES)])]


def write_asset(strips, guid, sheet, script_guid):
    body = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000", "MonoBehaviour:",
            "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
            f"  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}", "  m_Name: RunFeedbackArtSet",
            "  m_EditorClassIdentifier: "]
    named = {s.name: s for s in strips}
    for field, name in (("_levelRing", "levelRing"), ("_cardRing", "cardRing"), ("_cardCore", "cardCore"),
                        ("_emberRing", "emberRing"), ("_goldSpark", "goldSpark"), ("_emberSpark", "emberSpark")):
        body.append(f"  {field}:")
        body.append(skill.strip_yaml(named[name], guid, sheet, 4))
    body.append(f"  _sparkWidth: {skill.number(SPARK_WIDTH * SPARK_TPU)}")
    skill.write(ASSET_PATH, "\n".join(body) + "\n")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--frames", help="also save every frame as a PNG in this directory")
    args = parser.parse_args()

    os.makedirs(OUT_DIR, exist_ok=True)
    folder_meta = OUT_DIR + ".meta"
    skill.write(folder_meta, skill.FOLDER_META.format(guid=skill.existing_guid(folder_meta)))
    if not os.path.exists(SCRIPT_META):
        raise SystemExit(f"{SCRIPT_META} is missing: the art set needs its script's GUID.")
    strips = build_strips()
    sheet = skill.bleed(skill.pack(strips))
    png = os.path.join(OUT_DIR, SHEET_NAME + ".png")
    Image.fromarray(sheet, "RGBA").save(png, optimize=True)
    meta = png + ".meta"
    guid = skill.existing_guid(meta)
    skill.write(meta, skill.TEXTURE_META.format(guid=guid, sprite_id=skill.existing_sprite_id(meta),
                                                tpu=skill.number(CARD_TPU)))
    write_asset(strips, guid, sheet, skill.existing_guid(SCRIPT_META))
    asset_meta = ASSET_PATH + ".meta"
    skill.write(asset_meta, skill.ASSET_META.format(guid=skill.existing_guid(asset_meta)))
    size = skill.astc_bytes(sheet.shape[1], sheet.shape[0])
    print(f"{SHEET_NAME}.png {sheet.shape[1]}x{sheet.shape[0]}  ASTC 4x4 {size / 1024:.0f} KiB "
          f"({size / 1048576:.2f} MiB)")
    for s in strips:
        print(f"  {s.name}: {len(s.images)} frames {s.images[0].shape[1]}x{s.images[0].shape[0]} at {s.tpu:g} a unit")
    if args.frames:
        os.makedirs(args.frames, exist_ok=True)
        for s in strips:
            for i, img in enumerate(s.images):
                Image.fromarray(img, "RGBA").save(os.path.join(args.frames, f"{s.name}_{i}.png"))


if __name__ == "__main__":
    main()
