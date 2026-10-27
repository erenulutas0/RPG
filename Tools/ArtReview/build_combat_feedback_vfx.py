"""Painted combat feedback: the Daggers' double slash, the crit star, the death smoke, the gold coin and the numerals.

The heroes, enemies, sword slash, hit spark and every skill effect are smooth painted art (128 texels a unit, bilinear).
These five were still drawn from code as crisp pixel art (EffectArt) and clashed beside them. This paints them again in
the actors' style, with the pixel art kept as the fallback:

  - Double slash: the Daggers' two quick crossing cuts, steel blue with a white core and a soft glow like the painted
    sword slash, the first cut leading and the second crossing it in a flash.
  - Crit star: a sharp starburst in warm gold, a white-hot heart and long needle rays, then a ring and flung motes.
  - Smoke: soft soot billows lit from above and warmed from below, with an ember fleck or two, thinning into wisps:
    the death puff of an enemy without painted remains, and (tinted faint) a horde enemy's arrival.
  - Coin: a small gold coin spinning on its upright axis, a brass rim, an embossed face and a glint.
  - Numerals: bold, chunky, slightly tapered digits drawn from vector strokes (no font is rasterised), a soot outline and
    a soft shadow so they read on orange lava and on dark stone; white over pale warm for a hit, larger warm gold with a
    hot highlight for a crit. Each glyph is a fixed cell pinned at its bottom-left corner and set at a fixed stride, the
    same size and spacing as the pixel numerals, so CombatEffectsView lays a number out as before.

Every frame goes on one shared sheet, so everything this draws batches, the numbers on screen included, and
Assets/_Project/Art/Effects/Feedback/CombatFeedbackArtSet.asset is written with every strip's cells, pivot and density
(Cryptforge.Art.CombatFeedbackArtSet). The painting primitives, packing and export are build_skill_vfx's: frames are
painted 4x supersampled in world units, box-filtered down in sRGB as the project blends, and exported straight-alpha
with colour bled into the clear texels. Deterministic: the same script paints the same sheet. Existing .meta GUIDs are
kept, so a repaint never breaks a reference.

Usage:
    python Tools/ArtReview/build_combat_feedback_vfx.py               # paint the sheet, its meta and the art set
    python Tools/ArtReview/build_combat_feedback_vfx.py --frames DIR  # also save every frame as a PNG, for review
"""
import argparse
import math
import os

import numpy as np
from PIL import Image
from scipy import ndimage

import build_skill_vfx as skill
from build_skill_vfx import (CRIMSON, EMBER, STEEL, STEEL_LIGHT, STEEL_PALE, WHITE, WHITE_HOT, YELLOW, Frame, Noise,
                             blur, disc, grow, hex_rgb, mix, smoothstep, to_image)

ROOT = skill.ROOT
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "Effects", "Feedback")
SCRIPT_META = os.path.join(ROOT, "Assets", "_Project", "Scripts", "Art", "Unity", "CombatFeedbackArtSet.cs.meta")
ASSET_PATH = os.path.join(OUT_DIR, "CombatFeedbackArtSet.asset")
SHEET_NAME = "VFX_Feedback"

TPU = 128.0  # the painted actors' density

# The feedback palette beside the skills' (build_skill_vfx): soot for smoke and outlines, gold and brass for the coin and
# the crits.
SOOT = hex_rgb("#201B22")
SOOT_WARM = hex_rgb("#2A1416")
SMOKE_DEEP = hex_rgb("#2A2226")
SMOKE = hex_rgb("#463A3E")
SMOKE_LIT = hex_rgb("#77646A")
SMOKE_RIM = hex_rgb("#A08A8D")
GOLD_HOT = hex_rgb("#FFF4C8")
GOLD_LIGHT = hex_rgb("#FFDD7A")
GOLD = hex_rgb("#F7B637")
GOLD_DEEP = hex_rgb("#E0861F")
BRASS_LIGHT = hex_rgb("#F8BA50")
BRASS = hex_rgb("#AC7243")
BRASS_DARK = hex_rgb("#6B4224")
PALE_WARM = hex_rgb("#FFE6C4")


# ----------------------------------------------------------------------------------------------------------------------
# Strips with fixed cells. A numeral is cut at its frame as painted, never cropped to its ink, so every digit of a font
# is one cell size pinned at its bottom-left corner, as the layout sets them by stride.


class FixedStrip(skill.Strip):
    """Frames of one effect, each exported whole at the frame's own size, with the pivot the world origin of the frame."""

    def __init__(self, name, frames):
        self.name = name
        self.images = [to_image(f.resolve()) for f in frames]
        f = frames[0]
        if any((g.w, g.h, g.x0, g.y0) != (f.w, f.h, f.x0, f.y0) for g in frames):
            raise ValueError(f"{name}: every frame of a fixed strip is one cell")
        for img in self.images:
            if img[0, :, 3].any() or img[-1, :, 3].any() or img[:, 0, 3].any() or img[:, -1, 3].any():
                raise ValueError(f"{name}: a frame's paint reaches its edge; widen its cell")
        self.tpu = f.tpu
        self.pivot = (-f.x0 * f.tpu / f.w, -f.y0 * f.tpu / f.h)
        self.cells = []
        self.sheet = None


# ----------------------------------------------------------------------------------------------------------------------
# Numerals. Each digit is a few centreline strokes in its fill box (x 0-1 across the box's width, y 0-1.4 up it, the
# pixel numerals' 5:7), drawn as tapered capsules: a vertical run is the full stroke weight, a horizontal one lighter
# (a broad nib's contrast), and free ends taper a little, so the glyphs read chunky but drawn rather than typed.

BOX_ASPECT = 1.4
STROKE_V = 0.168  # half the stroke weight of a vertical run, in box widths
STROKE_H = 0.136  # and of a horizontal one
L, R = STROKE_V, 1.0 - STROKE_V  # centrelines whose stroke touches the box's sides
B, T = STROKE_H, BOX_ASPECT - STROKE_H  # and its foot and top


def _line(a, b, n=10):
    return [(a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t) for t in np.linspace(0.0, 1.0, n)]


def _arc(cx, cy, rx, ry, a0, a1, n=28, squash=2.0):
    """Points on an elliptical (squash 2) or squarer (squash > 2) arc from angle a0 to a1, in degrees anticlockwise."""
    e = 2.0 / squash
    out = []
    for t in np.linspace(0.0, 1.0, n):
        a = math.radians(a0 + (a1 - a0) * t)
        c, s = math.cos(a), math.sin(a)
        out.append((cx + rx * math.copysign(abs(c) ** e, c), cy + ry * math.copysign(abs(s) ** e, s)))
    return out


def _join(*runs):
    points = []
    for run in runs:
        points.extend(run if not points else run[1:] if np.allclose(run[0], points[-1]) else run)
    return points


def numeral_strokes(digit):
    """The strokes of a numeral: each a list of points and the taper of its first and last end (1 for none)."""
    if digit == 0:
        return [(_arc(0.5, 0.7, R - 0.5, 0.7 - B, 90, 450, n=72, squash=2.6), 1.0, 1.0)]
    if digit == 1:
        return [(_line((0.57, B), (0.57, T)), 1.0, 1.0),
                (_line((0.57, T), (0.2, T - 0.32)), 1.0, 0.78),
                (_line((0.18, B), (0.86, B)), 1.0, 1.0)]
    if digit == 2:
        top = _arc(0.5, T - 0.32, R - 0.5, 0.32, 168, -38)
        return [(_join(top, _line(top[-1], (L, B + 0.04)), _line((L, B), (R + 0.03, B))), 0.82, 1.0)]
    if digit == 3:
        upper = _arc(0.5, T - 0.28, R - 0.5, 0.28, 158, -90)
        lower = _arc(0.5, B + 0.3, R - 0.5, 0.3, 90, -158)
        waist = upper[-1][1] + 0.02
        return [(upper, 0.82, 1.0), (lower, 1.0, 0.82), (_line((0.33, waist), (0.52, waist)), 0.85, 1.0)]
    if digit == 4:
        stem_x = 0.69
        return [(_join(_line((stem_x, T), (L - 0.01, 0.5)), _line((L - 0.01, 0.47), (R + 0.04, 0.47))), 1.0, 1.0),
                (_line((stem_x, T), (stem_x, B)), 1.0, 1.0)]
    if digit == 5:
        bowl = _arc(0.47, 0.47, R - 0.47, 0.47 - B, 128, -152)
        flag = _join(_line((R, T), (0.22, T)), _line((0.22, T), (0.2, 0.76)), _line((0.2, 0.76), bowl[0]))
        return [(_join(flag, bowl), 1.0, 0.82)]
    if digit == 6:
        loop = _arc(0.5, 0.47, R - 0.5, 0.47 - B, 0, 360, n=56)
        hook = _arc(0.62, 0.62, 0.62 - L, T - 0.62, 180, 72)
        return [(loop, 1.0, 1.0), (_join(_line((L, 0.47), (L, 0.62)), hook), 1.0, 0.8)]
    if digit == 7:
        return [(_join(_line((L, T), (R, T)), _line((R, T), (0.4, B))), 1.0, 0.92)]
    if digit == 8:
        return [(_arc(0.5, T - 0.275, 0.5 - L - 0.035, 0.275, 90, 450, n=56), 1.0, 1.0),
                (_arc(0.5, B + 0.295, R - 0.5, 0.295, 90, 450, n=56), 1.0, 1.0)]
    if digit == 9:
        loop = _arc(0.5, T - 0.335, R - 0.5, 0.335, 0, 360, n=56)
        tail = _arc(0.38, 0.78, R - 0.38, 0.78 - B, 0, -108)
        return [(loop, 1.0, 1.0), (_join(_line((R, T - 0.335), (R, 0.78)), tail), 1.0, 0.8)]
    raise ValueError(digit)


def numeral_field(frame, digit, x0, y0, width):
    """Signed distance (world units, negative inside) to a numeral whose fill box has its bottom-left corner at (x0, y0)
    and is this wide."""
    X = (frame.X - x0) / width
    Y = (frame.Y - y0) / width
    field = np.full(frame.X.shape, np.inf, np.float32)
    for points, taper0, taper1 in numeral_strokes(digit):
        p = np.asarray(points, np.float32)
        seg = p[1:] - p[:-1]
        lengths = np.maximum(np.linalg.norm(seg, axis=1), 1e-6)
        upright = np.abs(seg[:, 1]) / lengths
        radius = STROKE_H + (STROKE_V - STROKE_H) * upright
        # Each point's weight is that of the runs either side of it, so a curve thickens smoothly into its sides.
        weights = np.concatenate([[radius[0]], (radius[1:] + radius[:-1]) * 0.5, [radius[-1]]])
        along = np.concatenate([[0.0], np.cumsum(lengths)]) / np.sum(lengths)
        weights = weights * np.interp(along, [0.0, 0.18, 0.82, 1.0], [taper0, 1.0, 1.0, taper1])
        for k in range(len(seg)):
            ax, ay = p[k]
            dx, dy = seg[k]
            t = np.clip(((X - ax) * dx + (Y - ay) * dy) / (dx * dx + dy * dy), 0.0, 1.0)
            d = np.hypot(X - ax - t * dx, Y - ay - t * dy) - (weights[k] + (weights[k + 1] - weights[k]) * t)
            np.minimum(field, d, out=field)
    return field * width


# A normal numeral's fill box, its outline and the cell round it, in texels, and its stride: the pixel numerals' 5x7
# glyph, one-texel outline and six-texel stride at 32 texels a unit, which is 20x28, 4 and 24 at 128. A crit is drawn
# at twice the size, its outline a little lighter than twice, as the pixel crit is the pixel numeral doubled.
DIGIT_FILL = (20, 28)
DIGIT_STRIDE = 24
DIGIT_OUTLINE = 3.3
# Inside a counter (the holes of 0, 4, 6, 8 and 9) the outline is thinner, so the floor shows through it and the
# numeral reads by its shape at phone size rather than as a blot with a dark heart.
DIGIT_INNER = 1.6
DIGIT_CELL = (30, 42)
CRIT_SCALE = 2
CRIT_OUTLINE = 5.6
CRIT_INNER = 2.8
CRIT_CELL = (56, 80)


def numeral_frame(digit, critical):
    scale = CRIT_SCALE if critical else 1
    cell_w, cell_h = CRIT_CELL if critical else DIGIT_CELL
    fill_w, fill_h = DIGIT_FILL[0] * scale / TPU, DIGIT_FILL[1] * scale / TPU
    outline = (CRIT_OUTLINE if critical else DIGIT_OUTLINE) / TPU
    inner = (CRIT_INNER if critical else DIGIT_INNER) / TPU
    frame = Frame(0.0, 0.0, cell_w / TPU, cell_h / TPU, TPU)
    # The fill box sits centred across the cell, so a row of cells centres its numerals; over the shadow's drop.
    x0 = (cell_w / TPU - fill_w) * 0.5
    drop = (2.4 if critical else 1.3) / TPU
    y0 = outline + drop + 3.6 * scale / TPU
    field = numeral_field(frame, digit, x0, y0, fill_w)
    counters = ndimage.binary_fill_holes(field < 0.0) & (field >= 0.0)
    px = frame.px

    def cover(f, inset=0.0):
        return np.clip(0.5 - (f - inset) / px, 0.0, 1.0)

    # A soft shadow under the outline, dropped a little, so the number stands off lava as well as stone.
    shifted = np.roll(field, int(round(drop / px)), axis=0)
    # Softened up and down only: across, it stays inside the outline, so it never darkens the next numeral's fill.
    shadow = ndimage.gaussian_filter1d(cover(shifted, outline), 0.8 * scale * skill.SS, axis=0)
    frame.paint(SOOT, shadow * ~counters * 0.55)
    edge = SOOT_WARM if critical else SOOT
    frame.paint(edge, cover(field, np.where(counters, inner, outline)))
    height = np.clip((frame.Y - y0) / fill_h, 0.0, 1.0)
    inside = cover(field)
    # Lit from above: the top of every stroke catches light, its underside turns warmer, as a raised glyph would.
    lit = cover(np.roll(field, -int(round(1.2 * scale / TPU / px)), axis=0))
    under = np.clip(inside - lit, 0.0, 1.0)
    if critical:
        body = mix(GOLD_DEEP, GOLD, smoothstep(0.0, 0.5, height))
        body = mix(body, GOLD_LIGHT, smoothstep(0.45, 0.8, height))
        body = mix(body, GOLD_HOT, smoothstep(0.72, 0.98, height))
        frame.paint(body, inside)
        frame.paint(hex_rgb("#C8601A"), under * 0.75)
        # The hot highlight: a white-hot glint across the upper strokes.
        frame.paint(WHITE, inside * smoothstep(0.78, 0.97, height) * 0.55)
    else:
        body = mix(PALE_WARM, WHITE, smoothstep(0.1, 0.75, height))
        frame.paint(body, inside)
        frame.paint(hex_rgb("#E9B98A"), under * 0.7)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# The Daggers' double slash, centred on the struck body like the pixel one: a first cut down from the upper left, a
# second up from the lower left that crosses it in a flash, both thinning into motes. Steel blue round a white core in
# a soft blue glow, the painted sword slash's tones (Art/Effects/FoundryStrikes).

SLASH_FRAMES = 5
# Each cut: from, to, and how far its centreline bows out (world units, to the left of its travel).
CUTS = (((-0.31, 0.26), (0.31, -0.24), -0.035), ((-0.3, -0.25), (0.3, 0.26), 0.035))


def cut_coords(frame, start, end, bow, n=48):
    """(along, across, length) of every sample against a gently bowed cut: along 0-1 from its start, across in world
    units."""
    (sx, sy), (ex, ey) = start, end
    length = math.hypot(ex - sx, ey - sy)
    nx, ny = -(ey - sy) / length, (ex - sx) / length
    t = np.linspace(0.0, 1.0, n)
    bulge = bow * np.sin(np.pi * t)
    px_, py_ = sx + (ex - sx) * t + nx * bulge, sy + (ey - sy) * t + ny * bulge
    best = np.full(frame.X.shape, np.inf, np.float32)
    along = np.zeros(frame.X.shape, np.float32)
    across = np.zeros(frame.X.shape, np.float32)
    for k in range(n - 1):
        ax, ay, dx, dy = px_[k], py_[k], px_[k + 1] - px_[k], py_[k + 1] - py_[k]
        u = np.clip(((frame.X - ax) * dx + (frame.Y - ay) * dy) / (dx * dx + dy * dy), 0.0, 1.0)
        qx, qy = frame.X - ax - u * dx, frame.Y - ay - u * dy
        d = np.hypot(qx, qy)
        closer = d < best
        best = np.where(closer, d, best)
        along = np.where(closer, (k + u) / (n - 1), along)
        across = np.where(closer, np.sign(qx * -dy + qy * dx) * d, across)
    return along, across, length


def paint_cut(frame, coords, reach, width, strength, core, noise=None, breakup=0.0):
    """One cut, drawn from its start to `reach` (0-1) along it: widest just behind its head, tapering to a point at
    both ends, a white core through its middle (core 0-1), steel round it, motion streaks beside it and a soft glow."""
    along, across, length = coords
    u = np.clip(along / max(reach, 1e-3), 0.0, 1.0)
    shown = along <= reach
    # The profile peaks 60% of the way to the head: a blade's swipe is fullest where it has just passed.
    rising = np.sin(np.pi * 0.5 * np.clip(u / 0.6, 0.0, 1.0)) ** 0.7
    falling = np.cos(np.pi * 0.5 * np.clip((u - 0.6) / 0.4, 0.0, 1.0)) ** 0.55
    profile = np.where(u < 0.6, rising, falling)
    half = width * profile * shown
    # Nothing where the cut has no width yet: a zero half-width must not leave a hairline along its path.
    body = np.clip((half - np.abs(across)) / frame.px + 0.5, 0.0, 1.0) * (half > 0)
    if breakup > 0 and noise is not None:
        body = body * smoothstep(breakup - 0.25, breakup + 0.15, noise(frame.X * 1.6 + 3.1, frame.Y * 1.6) * 0.5 + 0.5)
    frame.paint(STEEL, np.clip(blur(body, 3.5) * 1.4, 0.0, 0.42) * strength)
    for side in (-1.0, 1.0):
        # Streaks a little behind the blade either side, shorter than it, as a swipe leaves in the air.
        offset = side * width * 1.75
        lane_half = width * 0.28 * profile * (u > 0.12) * (u < 0.82) * shown
        lane = np.clip((lane_half - np.abs(across - offset)) / frame.px + 0.5, 0.0, 1.0) * (lane_half > 0)
        frame.paint(STEEL_LIGHT, blur(lane, 0.5) * 0.55 * strength)
    frame.paint(mix(STEEL_LIGHT, STEEL_PALE, 0.35), blur(body, 0.35) * strength)
    if core > 0:
        centre = np.clip((half * 0.42 - np.abs(across)) / frame.px + 0.5, 0.0, 1.0) * (half > 0)
        centre = centre * smoothstep(0.08, 0.3, u)
        frame.paint(WHITE, blur(centre, 0.45) * core * strength)


def paint_glint(frame, x, y, size, strength, color=WHITE, glow=STEEL_LIGHT, rays=4, angle=0.0):
    """A small bright glint: a soft glow, a hot centre and thin needle rays."""
    frame.paint(glow, disc(frame, x, y, size * 0.9, soft=size * 0.9) * 0.55 * strength)
    dx, dy = frame.X - x, frame.Y - y
    r = np.hypot(dx, dy)
    a = np.arctan2(dy, dx) - math.radians(angle)
    needle = np.clip(np.cos(a * rays / 2.0) ** 2, 0.0, 1.0) ** 40 * smoothstep(size * 2.2, 0.0, r)
    frame.paint(color, np.clip(needle * 1.4, 0.0, 1.0) * strength)
    frame.paint(color, disc(frame, x, y, size * 0.32, soft=size * 0.25) * strength)


def double_slash_frame(index, noise):
    frame = Frame(-0.46, -0.42, 0.46, 0.42, TPU)
    a, b = (cut_coords(frame, *cut) for cut in CUTS)
    # By frame: (reach, width, strength, core, breakup) of the first cut and of the second.
    first = ((0.62, 0.03, 1.0, 1.0, 0.0), (1.0, 0.034, 1.0, 1.0, 0.0), (1.0, 0.028, 0.85, 0.7, 0.0),
             (1.0, 0.018, 0.6, 0.0, 0.35), (1.0, 0.011, 0.32, 0.0, 0.55))[index]
    second = (None, (0.55, 0.03, 1.0, 1.0, 0.0), (1.0, 0.035, 1.0, 1.0, 0.0), (1.0, 0.024, 0.75, 0.35, 0.2),
              (1.0, 0.014, 0.45, 0.0, 0.5))[index]
    for coords, cut in ((a, first), (b, second)):
        if cut is None:
            continue
        reach, width, strength, core, breakup = cut
        paint_cut(frame, coords, reach, width, strength, core, noise, breakup)
        if reach < 1.0:
            # The blade's point, bright where it is now.
            (sx, sy), (ex, ey), _ = CUTS[0] if coords is a else CUTS[1]
            paint_glint(frame, sx + (ex - sx) * reach, sy + (ey - sy) * reach, 0.03, 0.9)
    if index in (2, 3):
        # Where the cuts cross, a white flash.
        paint_glint(frame, 0.0, 0.01, (0.085, 0.06)[index - 2], (1.0, 0.6)[index - 2], rays=4, angle=45.0)
    if index >= 3:
        # Motes shed along the cuts, drifting out.
        rng = np.random.default_rng(5)
        for k in range(10):
            cut = CUTS[k % 2]
            t = rng.uniform(0.15, 0.9)
            (sx, sy), (ex, ey), _ = cut
            drift = 0.03 * (index - 2) * (1 if rng.random() < 0.5 else -1)
            x = sx + (ex - sx) * t + drift * 0.7
            y = sy + (ey - sy) * t + drift
            frame.paint(STEEL_PALE, disc(frame, x, y, 0.007 * (1.4 - 0.3 * (index - 3)), soft=0.004)
                        * (0.8 if index == 3 else 0.45))
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# The crit star, over the strike: a white-hot heart with four long needle rays and four shorter ones in warm gold,
# bursting with a thin ring, then retracting into flung motes.

STAR_FRAMES = 4


def paint_rays(frame, count, length, half, angle, strength, rim=True):
    """Needle rays from the centre: count of them, starting at angle (degrees), each tapering from half (world units,
    half its base width) to a point at length."""
    X, Y = frame.X, frame.Y
    cover = np.zeros(X.shape, np.float32)
    hot = np.zeros(X.shape, np.float32)
    for k in range(count):
        a = math.radians(angle + 360.0 * k / count)
        c, s = math.cos(a), math.sin(a)
        u = X * c + Y * s
        v = -X * s + Y * c
        t = np.clip(u / length, 0.0, 1.0)
        w = half * (1.0 - t) ** 1.25
        # Only along the ray: a zero width must not leave a hairline behind the centre or past the tip.
        ray = np.clip((w - np.abs(v)) / frame.px + 0.5, 0.0, 1.0) * (u > 0) * (u < length)
        cover = np.maximum(cover, ray)
        hot = np.maximum(hot, ray * (1.0 - t) * np.clip(1.0 - np.abs(v) / np.maximum(w, 1e-6), 0.0, 1.0))
    if rim:
        frame.paint(SOOT_WARM, blur(grow(cover, 0.8), 0.5) * 0.45 * strength)
    frame.paint(GOLD, blur(cover, 0.3) * strength)
    frame.paint(GOLD_LIGHT, blur(smoothstep(0.15, 0.45, hot), 0.4) * strength)
    frame.paint(GOLD_HOT, blur(smoothstep(0.45, 0.8, hot), 0.4) * strength)


def crit_star_frame(index):
    frame = Frame(-0.44, -0.44, 0.44, 0.44, TPU)
    # By frame: long ray length and half width, short ray length and half width, glow radius, core radius, strength.
    long_len, long_half, short_len, short_half, glow, core, strength = (
        (0.27, 0.024, 0.14, 0.015, 0.11, 0.05, 1.0),
        (0.37, 0.026, 0.21, 0.017, 0.15, 0.058, 1.0),
        (0.31, 0.016, 0.15, 0.011, 0.14, 0.04, 0.85),
        (0.17, 0.009, 0.08, 0.007, 0.1, 0.026, 0.55))[index]
    # A tight warm glow: a wide one hazed the body it lit over.
    frame.paint(GOLD, disc(frame, 0.0, 0.0, glow, soft=glow) * 0.45 * strength)
    if index in (1, 2):
        # The burst's ring, racing out and thinning.
        r = (0.16, 0.25)[index - 1]
        ring = np.clip(((0.011, 0.007)[index - 1] - np.abs(np.hypot(frame.X, frame.Y) - r)) / frame.px + 0.5, 0.0, 1.0)
        frame.paint(GOLD_LIGHT, blur(ring, 0.5) * (0.85, 0.5)[index - 1])
    paint_rays(frame, 4, short_len, short_half, 45.0, strength)
    paint_rays(frame, 4, long_len, long_half, 90.0, strength)
    frame.paint(GOLD_HOT, disc(frame, 0.0, 0.0, core * 1.6, soft=core * 1.2) * strength)
    frame.paint(WHITE, disc(frame, 0.0, 0.0, core, soft=core * 0.5) * strength)
    if index >= 2:
        # Motes flung out between the rays.
        for k in range(8):
            a = math.radians(22.5 + 45.0 * k + 9.0 * math.sin(k * 2.1))
            r = (0.27, 0.35)[index - 2] + 0.02 * ((k * 0.618) % 1.0)
            size = (0.012, 0.008)[index - 2] * (0.8 + 0.4 * ((k * 0.37) % 1.0))
            x, y = r * math.cos(a), r * math.sin(a)
            frame.paint(GOLD, disc(frame, x, y, size * 1.8, soft=size * 1.5) * 0.4)
            frame.paint(GOLD_HOT, disc(frame, x, y, size, soft=size * 0.5) * (0.9, 0.6)[index - 2])
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# Smoke: the death puff of an enemy without painted remains, which the view rises and shows at 0.55 of its size, and a
# horde newcomer's arrival puff, the same frames faint and smaller. Painted in the pixel smoke's own frame - a unit
# square at scale 1 - at the density that lands about a texel to a pixel at the death puff's size.

SMOKE_FRAMES = 6
SMOKE_TPU = 72.0
# The billows of the puff at its full spread: x, y, radius, in the unit frame, as the pixel puff lays its clouds out;
# lower ones are painted last, in front.
BILLOWS = ((0.0, 0.11, 0.21), (-0.28, -0.04, 0.19), (0.28, -0.06, 0.185), (-0.12, -0.21, 0.21),
           (0.14, -0.22, 0.205), (0.0, -0.04, 0.24))


def smoke_frame(index, noise):
    frame = Frame(-0.7, -0.68, 0.7, 0.86, SMOKE_TPU)
    X, Y = frame.X, frame.Y
    # By frame: how far the billows have spread and risen, how thick the smoke is, how far it has broken into wisps,
    # and how hot its heart and underside still glow. It stays whole for most of its life, as the pixel puff does,
    # then thins out over its last frames, so the view's cut at its end is never a pop.
    spread, rise, thick, breakup, heat = ((0.7, 0.0, 1.0, 0.0, 1.0), (0.86, 0.03, 1.0, 0.0, 0.75),
                                          (0.98, 0.07, 1.0, 0.0, 0.45), (1.07, 0.11, 0.9, 0.18, 0.22),
                                          (1.14, 0.16, 0.6, 0.4, 0.08), (1.19, 0.2, 0.3, 0.6, 0.0))[index]
    light = np.array([-0.55, 0.83], np.float32)
    # Thinning in soft patches rather than cut through: a hard threshold left crescents of billow standing like horns.
    patches = noise(X * 0.9 + 0.7, Y * 0.9 - rise * 2.0) * 0.5 + 0.5
    wisps = np.clip(1.0 - breakup * (1.25 - patches), 0.0, 1.0)
    billows = []
    for k, (bx, by, br) in enumerate(BILLOWS):
        cx, cy = bx * spread, by * spread * 0.9 + rise
        r = br * (0.5 + 0.5 * spread)
        dx, dy = X - cx, Y - cy
        d = np.hypot(dx, dy)
        # Ragged rather than round: each billow's edge wanders, and a finer turbulence breaks its outline.
        wobble = 1.0 + 0.2 * noise(dx / r * 0.25 + k * 0.37, dy / r * 0.25 + rise)
        wobble = wobble + 0.08 * noise(dx / r * 0.8 + k * 1.3, dy / r * 0.8 - rise * 3.0)
        billows.append((k, by, r, dx, dy, d, r * wobble))
    union = np.zeros(X.shape, np.float32)
    for k, by, r, dx, dy, d, edge in billows:
        union = np.maximum(union, smoothstep(edge, edge - r * 0.3, d))
    union = union * wisps * thick
    # A soft soot edge round the whole cloud, so it parts from the dark stone as well as from the lava.
    frame.paint(SOOT, blur(grow(union, 1.0), 0.8) * 0.7)
    for k, by, r, dx, dy, d, edge in sorted(billows, key=lambda b: -b[1]):
        cover = smoothstep(edge, edge - r * 0.3, d) * wisps * thick
        n = np.stack([dx, dy], -1) / np.maximum(d, 1e-6)[..., None]
        # Lit from the upper left, strongest toward each billow's edge, as round smoke is.
        lambert = np.clip((n @ light) * np.clip(d / r, 0.0, 1.0) ** 0.7 * 0.6 + 0.5, 0.0, 1.0)
        tone = mix(SMOKE_DEEP, SMOKE, smoothstep(0.1, 0.45, lambert))
        tone = mix(tone, SMOKE_LIT, smoothstep(0.5, 0.8, lambert))
        tone = mix(tone, SMOKE_RIM, smoothstep(0.8, 1.0, lambert) * smoothstep(0.55, 0.95, d / edge))
        # Brushed through with a soft turbulence, so a billow is never a flat ball.
        tone = tone * (0.92 + 0.12 * noise(X * 3.1 + k, Y * 3.1 - rise * 4.0))[..., None]
        # Warmed from below by the embers it rose from.
        under = smoothstep(-0.1, -0.85, dy / r) * heat
        tone = mix(tone, mix(CRIMSON, EMBER, 0.45), under * 0.75)
        frame.paint(tone, cover)
    if heat > 0:
        # The hot heart showing through the billows as they part.
        cy = -0.06 * spread + rise
        frame.paint(EMBER, disc(frame, 0.0, cy, 0.16 * spread, soft=0.13 * spread) * heat * union * 0.7)
        frame.paint(YELLOW, disc(frame, 0.0, cy, 0.07 * spread, soft=0.06) * heat * union * 0.75)
    # An ember fleck or two thrown up out of it, cooling as they rise.
    for k, (x0, y0, vx, vy) in enumerate(((0.12, 0.1, 0.08, 0.095), (-0.18, 0.02, -0.07, 0.085))):
        if index == 0 or (k == 1 and index < 2):
            continue
        age = index - (1 if k == 0 else 2)
        paint_fleck(frame, x0 + vx * age, y0 + rise + vy * age, 0.02 - 0.0018 * age, max(0.0, 1.0 - 0.2 * age),
                    math.degrees(math.atan2(vy, vx)))
    return frame


def paint_fleck(frame, x, y, radius, hot, angle):
    """An ember fleck streaking up out of the smoke: a warm glow round a core cooling from yellow-white to ember. The
    skills' embers carry a soot ring for the lava; over smoke that ring read as an eye, so a fleck has none."""
    c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
    u = ((frame.X - x) * c + (frame.Y - y) * s) / 1.6
    v = -(frame.X - x) * s + (frame.Y - y) * c
    d = np.hypot(u, v)
    frame.paint(EMBER, np.clip((radius * 2.6 - d) / (radius * 2.0), 0.0, 1.0) * 0.4 * (0.5 + 0.5 * hot))
    core = mix(EMBER, mix(YELLOW, WHITE_HOT, max(0.0, hot * 2.0 - 1.0)), min(1.0, hot * 1.4))
    frame.paint(core, np.clip((radius - d) / max(frame.px, radius * 0.35) + 0.5, 0.0, 1.0))


# ----------------------------------------------------------------------------------------------------------------------
# The gold coin: a disc spinning on its upright axis, face, three-quarters, edge and round again, a brass rim, an
# embossed face lit from the upper left, a dark edge for the lava and a glint as the face turns to the light.

COIN_FRAMES = 8
COIN_RADIUS = 0.125
COIN_THICK = 0.05


def coin_frame(index):
    frame = Frame(-0.21, -0.21, 0.21, 0.21, TPU)
    X, Y = frame.X, frame.Y
    theta = 2.0 * math.pi * index / COIN_FRAMES
    c, s = math.cos(theta), math.sin(theta)
    R = COIN_RADIUS
    face_w = R * abs(c)
    shift = COIN_THICK * 0.5 * s
    px = frame.px

    def ellipse(cx, rx, ry, grow_by=0.0):
        # Anti-aliased by the implicit function over its gradient, which stays a distance however thin the ellipse;
        # an edge-on face, under half a texel wide, is the rim's side alone.
        rx, ry = rx + grow_by, ry + grow_by
        if rx < 0.5 / TPU:
            return np.zeros(X.shape, np.float32)
        ux, uy = (X - cx) / rx, Y / ry
        gradient = 2.0 * np.hypot(ux / rx, uy / ry) + 1e-9
        return np.clip(0.5 - (ux * ux + uy * uy - 1.0) / gradient / px, 0.0, 1.0)

    def band(grow_by=0.0):
        # The rim's side between the two faces: the coin's thickness seen as it turns.
        left, right = sorted((shift, -shift))
        inside = (X >= left - grow_by) & (X <= right + grow_by)
        return inside * np.clip((R + grow_by - np.abs(Y)) / px + 0.5, 0.0, 1.0)

    front = ellipse(shift, face_w, R)
    back = ellipse(-shift, face_w, R)
    silhouette = np.maximum(np.maximum(front, back), band())
    outline = np.maximum(np.maximum(ellipse(shift, face_w, R, 1.4 / TPU), ellipse(-shift, face_w, R, 1.4 / TPU)),
                         band(1.4 / TPU))
    frame.paint(SOOT_WARM, outline * 0.92)
    # The rim's side: brass, lit along its top, with fine milling.
    height = np.clip((Y + R) / (2 * R), 0.0, 1.0)
    side = mix(BRASS_DARK, GOLD_DEEP, smoothstep(0.0, 0.45, height))
    side = mix(side, GOLD_LIGHT, smoothstep(0.55, 0.92, height) * 0.85)
    milling = 0.85 + 0.15 * np.cos(Y / R * 26.0) ** 2
    frame.paint(side * milling[..., None], silhouette)
    # The face turned to the camera, the front or the back alike.
    if face_w > 0.2 / TPU:
        fx = shift if c >= 0 else -shift
        face = ellipse(fx, face_w, R)
        u = (X - fx) / max(face_w, 1e-4)
        v = Y / R
        r = np.hypot(u, v)
        # Lit from the upper left; the light slides across the face as it turns.
        lightness = np.clip(0.55 - 0.35 * (u * 0.7 - v * 0.7) * abs(c) - 0.2 * r, 0.0, 1.0)
        tone = mix(GOLD_DEEP, GOLD, smoothstep(0.1, 0.45, lightness))
        tone = mix(tone, GOLD_LIGHT, smoothstep(0.45, 0.75, lightness))
        frame.paint(tone, face)
        # A raised rim round the face, dark at its lower right and bright at its upper left.
        ring = smoothstep(0.7, 0.76, r) * smoothstep(0.98, 0.9, r) * face
        frame.paint(mix(BRASS, GOLD_LIGHT, np.clip(0.5 - 0.6 * (u - v) * 0.5, 0.0, 1.0)), ring * 0.9)
        # The embossed mark: a four-point star, its lit facets toward the upper left.
        star_r = np.abs(u) ** 0.6 + np.abs(v) ** 0.6
        mark = smoothstep(0.62, 0.52, star_r) * face
        frame.paint(mix(BRASS_DARK, BRASS_LIGHT, np.clip(0.45 - (u - v), 0.0, 1.0)), mark)
        frame.paint(GOLD_HOT, smoothstep(0.3, 0.15, star_r) * face * 0.7)
        # A soft sheen across the upper left.
        sheen = np.clip(1.0 - np.hypot(u + 0.35, v - 0.4) / 0.45, 0.0, 1.0) * face
        frame.paint(GOLD_HOT, sheen ** 1.5 * 0.55 * abs(c))
    # The glint, as the face meets the light.
    glint = (1.0, 0.45, 0.0, 0.0, 0.8, 0.35, 0.0, 0.0)[index]
    if glint > 0:
        gx = (shift if c >= 0 else -shift) - face_w * 0.42
        paint_glint(frame, gx, R * 0.42, 0.05, glint, color=WHITE, glow=GOLD_HOT, rays=4)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# The sheet.


def build_strips():
    noise = Noise(23, period=1.0, feature=0.09)
    smoke_noise = Noise(29, period=1.0, feature=0.12)
    return [skill.Strip("doubleSlash", [double_slash_frame(i, noise) for i in range(SLASH_FRAMES)]),
            skill.Strip("critStar", [crit_star_frame(i) for i in range(STAR_FRAMES)]),
            skill.Strip("smoke", [smoke_frame(i, smoke_noise) for i in range(SMOKE_FRAMES)]),
            skill.Strip("coin", [coin_frame(i) for i in range(COIN_FRAMES)]),
            FixedStrip("digits", [numeral_frame(d, False) for d in range(10)]),
            FixedStrip("critDigits", [numeral_frame(d, True) for d in range(10)])]


def write_asset(strips, guid, sheet, script_guid):
    body = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000", "MonoBehaviour:",
            "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
            f"  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}", "  m_Name: CombatFeedbackArtSet",
            "  m_EditorClassIdentifier: "]
    named = {s.name: s for s in strips}
    for field, name in (("_doubleSlash", "doubleSlash"), ("_critStar", "critStar"), ("_smoke", "smoke"),
                        ("_coin", "coin"), ("_digits", "digits"), ("_critDigits", "critDigits")):
        body.append(f"  {field}:")
        body.append(skill.strip_yaml(named[name], guid, sheet, 4))
    body.append(f"  _digitStride: {DIGIT_STRIDE}")
    body.append(f"  _critDigitStride: {DIGIT_STRIDE * CRIT_SCALE}")
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
                                                tpu=skill.number(TPU)))
    write_asset(strips, guid, sheet, skill.existing_guid(SCRIPT_META))
    asset_meta = ASSET_PATH + ".meta"
    skill.write(asset_meta, skill.ASSET_META.format(guid=skill.existing_guid(asset_meta)))
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
