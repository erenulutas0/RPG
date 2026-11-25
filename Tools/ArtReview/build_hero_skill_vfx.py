"""Painted effects for docs/57's four new skills, in the painted actors' smooth style (build_skill_vfx's):

  - Iron Resolve (the Vanguard's guard, 4 s of +100 armour): an iron bell closing over the hero. Its plates fly in as
    six staves meeting over its head and lock with a white clang (resolveClose); for the guard's seconds the bell holds
    as a blue steel sheen the hero shows through, plated, riveted and catching a glint that sweeps round it
    (resolveHold, a loop); as the guard ends its seams flash and the staves fall away in flakes (resolveEnd). Under it a
    ring of rivets stamps the floor at a unit's radius (resolveRivets, 0.3 s: docs/57 3.1's "a ring of rivets ... radius
    1, 0.3 s").
  - Shield Rush (the Vanguard's charge, 4 units in 0.3 s): a steel-blue wake behind the hero with a bow wave before its
    shield (rushStreak, facing +x with the head at the pivot, turned along the dash), and where the dash stops, the
    shield's impact: a white flash and a crescent of shocked air, steel sparks flung forward and the dust it skids up
    (rushImpact, facing +x likewise).
  - Flame Sigil (Ilya's glyph, radius 2.5 for 5 s): a unit floor ellipse the view grows to the glyph's radius - a burning
    double band of twelve furnace runes over a soot rim, an inner ring with six spokes like a furnace grate, a faint warm
    wash over the floor it holds and short licks off its far side; the runes' heat runs round the ring over its loop
    (sigilGlyph). Its ignition from Ilya's feet outward: the grate and then the band white-hot (sigilIgnite).
  - Cinder Ward (Ilya's ward, 20 % of her health for 2 s): ash-white flakes and cinders whirling round her in three
    tilted orbits inside a faint ash shell (wardShell, a loop that closes on itself); their rise from the floor into the
    shell (wardRaise); the shell blowing apart as the ward ends or is spent (wardBreak); and the cinders flung at a
    striker whose blow the ward absorbed (wardFlare: docs/57 3.2's "each absorbed blow flares ember at the striker").

Colour (docs/52 section 2.1 rule 3, docs/51): red-orange is what hurts on a timer - the Warden's mark is a red-orange
ring on the floor - and orchid is a creature, so no vivid texel of these strips lies at hue under 30 or from 345
degrees, nor at 280-330. The Vanguard's are its steel blue (hue 205-225) over cool iron; Ilya's are furnace gold (hue
34-47) and ash, never Burn's ember (#E77232, hue 21) - docs/57 named the Sigil "ember-orange", which as a hero's floor
ring of 2.5 would read as the Warden's mark. Their soot is a neutral dark, so no blend with it turns red. EditMode's
PaintedHeroSkillEffectTests reads every texel back; this script checks the same before it writes.

Each skill is a family on a sheet of its own in Assets/_Project/Art/Effects/Skills (VFX_Skill_Resolve, VFX_Skill_Rush,
VFX_Skill_Sigil, VFX_Skill_Ward), imported as the other skill sheets (build_skill_vfx.TEXTURE_META), and its strips are
written into SkillEffectArtSet.asset after _forgeFlash, every other line kept (build_skill_vfx.write_fields); the art set
holds them as optional, so a set painted before them is still whole. The primitives, packing and export are
build_skill_vfx's: frames painted 4x supersampled in world units, box-filtered down in sRGB as the project blends,
exported straight-alpha with colour bled into the clear texels. Deterministic. Existing .meta GUIDs are kept.

Usage:
    python Tools/ArtReview/build_hero_skill_vfx.py               # the sheets, their metas and the art set's strips
    python Tools/ArtReview/build_hero_skill_vfx.py --frames DIR  # also save every frame as a PNG, for review
"""
import argparse
import math
import os

import numpy as np
from PIL import Image

import build_skill_vfx as skill
from build_skill_vfx import (STEEL, STEEL_DEEP, STEEL_LIGHT, STEEL_PALE, STEEL_SOOT, WHITE, WHITE_HOT, Frame, Noise,
                             blur, grow, hex_rgb, mix, shrink, smoothstep)

OUT_DIR = skill.OUT_DIR
ASSET_PATH = skill.ASSET_PATH
DEPTH = 0.5  # ArenaFloor.DepthScale: a floor unit up the screen is half a unit

# The Vanguard's iron, cool and blue-grey beside build_skill_vfx's steel.
IRON_DARK = hex_rgb("#283246")
IRON = hex_rgb("#56688A")
IRON_LIGHT = hex_rgb("#A7BAD8")
DUST = hex_rgb("#8F887E")
DUST_DARK = hex_rgb("#4C4842")
# Ilya's furnace gold, hue 34-47 from its deep shade to white-hot, and the ash of her ward; a neutral soot.
NEUTRAL_SOOT = hex_rgb("#191817")
GOLD_DEEP = hex_rgb("#7A5212")
GOLD_DARK = hex_rgb("#A46E16")
AMBER = hex_rgb("#E29A22")
GOLD = hex_rgb("#FFC64D")
GOLD_PALE = hex_rgb("#FFE6A0")
ASH_WHITE = hex_rgb("#EFEAE1")
ASH = hex_rgb("#B5ADA2")
ASH_DARK = hex_rgb("#615B54")
GOLD_FIRE = (NEUTRAL_SOOT, GOLD_DARK, GOLD_DEEP, AMBER, GOLD_DARK, GOLD, GOLD_PALE, WHITE_HOT)


# ----------------------------------------------------------------------------------------------------------------------
# Shared helpers.


def segment_distance(X, Y, x0, y0, x1, y1):
    dx, dy = x1 - x0, y1 - y0
    length = dx * dx + dy * dy
    t = np.clip(((X - x0) * dx + (Y - y0) * dy) / max(length, 1e-12), 0.0, 1.0)
    return np.hypot(X - (x0 + t * dx), Y - (y0 + t * dy))


def stroke(frame, distance, width, soft=0.0):
    """Anti-aliased coverage of a stroke width wide (world units) at the given distance from its centre line."""
    return np.clip((width * 0.5 - distance) / max(frame.px, soft) + 0.5, 0.0, 1.0)


def spark(frame, x, y, angle, length, width, color, glow, alpha=1.0):
    """A streak from (x, y) back along angle's opposite (head at x, y): a soft glow, then the core tapering to its tail."""
    a = math.radians(angle)
    tx, ty = x - math.cos(a) * length, y - math.sin(a) * length
    d = segment_distance(frame.X, frame.Y, x, y, tx, ty)
    along = np.clip(((frame.X - tx) * math.cos(a) + (frame.Y - ty) * math.sin(a)) / max(length, 1e-6), 0.0, 1.0)
    frame.paint(glow, smoothstep(width * 3.0, 0.0, d) * 0.35 * along * alpha)
    frame.paint(color, stroke(frame, d, width * (0.35 + 0.65 * along)) * (0.3 + 0.7 * along) * alpha)


def cinder(frame, x, y, radius, hot, stretch=1.6, angle=90.0, alpha=1.0):
    """A cinder in Ilya's gold, streaking along its flight: a faint halo, a thin neutral soot edge (never the burn's
    red soot, which blends red), a core from deep gold through amber to white-hot as hot runs from 0 to 1."""
    c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
    u = ((frame.X - x) * c + (frame.Y - y) * s) / stretch
    v = -(frame.X - x) * s + (frame.Y - y) * c
    d = np.sqrt(u * u + v * v)

    def dot(r, soft):
        return np.clip((r - d) / max(frame.px, soft) + 0.5, 0.0, 1.0)

    frame.paint(mix(AMBER, GOLD, hot), dot(radius * 2.6, radius * 2.0) * 0.3 * (0.4 + 0.6 * hot) * alpha)
    frame.paint(NEUTRAL_SOOT, dot(radius * 1.45, radius * 0.4) * 0.5 * alpha)
    core = mix(mix(GOLD_DARK, AMBER, min(1.0, hot * 2.0)), mix(GOLD, WHITE_HOT, max(0.0, hot * 2.0 - 1.0)),
               min(1.0, hot * 1.5))
    frame.paint(core, dot(radius, radius * 0.3) * alpha)


def flake(frame, x, y, length, angle, light, alpha=1.0):
    """An ash flake: a pale chip turned along its flight, its shadow side a little darker."""
    c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
    u = ((frame.X - x) * c + (frame.Y - y) * s) / max(length, 1e-6)
    v = (-(frame.X - x) * s + (frame.Y - y) * c) / max(length * 0.42, 1e-6)
    d = np.sqrt(u * u + v * v)
    edge = frame.px / max(length * 0.42, 1e-6)
    cover = np.clip((1.0 - d) / edge + 0.5, 0.0, 1.0)
    frame.paint(NEUTRAL_SOOT, np.clip((1.35 - d) / edge + 0.5, 0.0, 1.0) * 0.35 * alpha)
    frame.paint(mix(ASH_DARK, ASH_WHITE, np.clip(light + 0.35 * v, 0.0, 1.0)), cover * alpha)


def dust_puff(frame, x, y, radius, alpha, seed):
    """A soft puff of floor dust, lit from above: a warm grey of no saturation to speak of."""
    rng = np.random.default_rng(seed)
    for k in range(4):
        ox, oy = rng.uniform(-0.5, 0.5) * radius, rng.uniform(-0.3, 0.4) * radius
        r = radius * rng.uniform(0.45, 0.75)
        d = np.hypot(frame.X - (x + ox), frame.Y - (y + oy))
        cover = smoothstep(r, r * 0.25, d)
        frame.paint(mix(DUST_DARK, DUST, np.clip(0.5 + (frame.Y - y - oy) / (2 * r), 0.0, 1.0)), cover * alpha * 0.7)


# ----------------------------------------------------------------------------------------------------------------------
# A bell round the hero: upright walls of radius R round the feet (a floor circle, half as deep as it is wide on
# screen) to the height wall, under an elliptic cap cap high. Seen as the game projects the floor - a point (x, z, h), z
# away from the camera, stands on screen at (x, h + z * DEPTH) - h falls along a view ray as z grows, so the surface
# seen is the one with the greatest h on the ray: the near face (z < 0) wherever it reaches, and over it the top of the
# cap seen from above.


class Bell:
    def __init__(self, R, wall, cap):
        self.R, self.wall, self.cap = R, wall, cap

    def radius(self, h):
        t = np.clip((h - self.wall) / self.cap, 0.0, 1.0)
        return self.R * np.sqrt(np.clip(1.0 - t * t, 0.0, 1.0))

    def solve(self, X, Y, iterations=18):
        """For screen points: whether the bell covers them, and the seen point's height h, angle theta round the axis
        (0 facing the camera, positive to the right, +-pi at the back), radius r there and surface normal."""
        R, wall, cap = self.R, self.wall, self.cap
        x2 = X * X
        across = np.clip(R * R - x2, 0.0, None)
        limb = wall + cap * np.sqrt(across) / R  # where the near face meets the back, in this column
        lo, hi = np.zeros_like(X), limb.copy()
        for _ in range(iterations):  # the near face: h - DEPTH * sqrt(r^2 - x^2) rises with h
            mid = (lo + hi) * 0.5
            f = mid - DEPTH * np.sqrt(np.clip(self.radius(mid) ** 2 - x2, 0.0, None))
            below = f < Y
            lo, hi = np.where(below, mid, lo), np.where(below, hi, mid)
        near_h = (lo + hi) * 0.5
        near = (x2 < R * R) & (Y >= -DEPTH * np.sqrt(across)) & (Y <= limb)
        # The cap seen from above: h + DEPTH * sqrt(r^2 - x^2) peaks at peak_h, then falls to the limb.
        peak_h = wall + 2 * cap * cap * DEPTH * np.sqrt(across) / (R * np.sqrt(R * R + 4 * cap * cap * DEPTH * DEPTH))
        top = peak_h + DEPTH * np.sqrt(np.clip(self.radius(peak_h) ** 2 - x2, 0.0, None))
        lo, hi = peak_h.copy(), limb.copy()
        for _ in range(iterations):
            mid = (lo + hi) * 0.5
            g = mid + DEPTH * np.sqrt(np.clip(self.radius(mid) ** 2 - x2, 0.0, None))
            above = g > Y
            lo, hi = np.where(above, mid, lo), np.where(above, hi, mid)
        back_h = (lo + hi) * 0.5
        back = (x2 < R * R) & (Y > limb) & (Y <= top)
        inside = near | back
        h = np.where(near, near_h, back_h)
        r = self.radius(h)
        z = np.where(near, -1.0, 1.0) * np.sqrt(np.clip(r * r - x2, 0.0, None))
        theta = np.arctan2(X, -z)
        n = np.stack([X / (R * R), z / (R * R), np.clip(h - wall, 0.0, None) / (cap * cap)], axis=-1)
        n /= np.maximum(np.linalg.norm(n, axis=-1, keepdims=True), 1e-9)
        return inside, h, theta, r, n


def angle_gap(a, b):
    return np.abs((a - b + np.pi) % (2 * np.pi) - np.pi)


def shifted(field, dx, dy, px):
    """A supersampled field moved by (dx, dy) world units, whole supersamples at a time, clear where it moved from."""
    ix, iy = int(round(dx / px)), int(round(-dy / px))
    out = np.zeros_like(field)
    h, w = field.shape
    src_y, dst_y = slice(max(0, -iy), min(h, h - iy)), slice(max(0, iy), min(h, h + iy))
    src_x, dst_x = slice(max(0, -ix), min(w, w - ix)), slice(max(0, ix), min(w, w + ix))
    out[dst_y, dst_x] = field[src_y, src_x]
    return out


# ----------------------------------------------------------------------------------------------------------------------
# Iron Resolve.

RESOLVE_TPU = 100.0  # the rendered heroes' density: the bell the guard holds for its 4 s
RESOLVE_FAST_TPU = 75.0  # its closing and its fall, a fifth of a second each, and the rivets' 0.3 s
RESOLVE_EXTENT = (-1.08, -0.62, 1.08, 1.95)
RESOLVE_BELL = Bell(0.64, 0.74, 0.8)
STAVES = 8  # round the whole bell: on its near half a stave faces the camera, its seams at +-22.5 and +-67.5 degrees
HOOPS = (0.36, 0.98)
RIVET_COUNT = 16
RESOLVE_LIGHT = np.array([-0.45, -0.55, 0.7]) / np.linalg.norm([-0.45, -0.55, 0.7])


def paint_bell(frame, bell, light, glint=None, scale=1.0, spread=0.0, drop=0.0, iron=0.0, flash=0.0, sheen=1.0,
               crack=0.0, seams=1.0):
    """The plated bell over the hero, scaled by scale about the feet. Its staves can stand apart: each moves out along
    its own bearing by spread and down by drop, so the bell can fly in and fall away. iron (0-1) turns the sheen into
    solid plate, flash whites it, crack widens its seams, seams fades them."""
    X, Y = frame.X, frame.Y
    inside, h, theta, r, n = bell.solve(X / scale, Y / scale)
    if not inside.any():
        return
    shade = np.clip(n @ light, 0.0, 1.0)
    # Seams: the staves' edges (arc length on the surface) and the hoops; a rivet where they cross.
    step = 2 * np.pi / STAVES
    nearest = np.round((theta - step * 0.5) / step) * step + step * 0.5
    edge = angle_gap(theta, nearest) * r * scale
    hoop = np.min(np.stack([np.abs(h - lat) for lat in HOOPS]), axis=0) * scale
    width = 0.014 + 0.03 * crack
    seam = np.maximum(smoothstep(width, width * 0.3, edge), smoothstep(0.012, 0.004, hoop))
    rivet = np.zeros_like(X)
    for lat in HOOPS:
        rivet = np.maximum(rivet, smoothstep(0.032, 0.018, np.hypot(edge, (h - lat) * scale)))
    spec = np.zeros_like(X)
    if glint is not None:
        half = (light + glint) / np.linalg.norm(light + glint)
        spec = np.clip(n @ half, 0.0, 1.0) ** 50
    fields = np.stack([inside.astype(np.float32), shade, seam, rivet, spec])
    if spread or drop:
        moved = np.zeros_like(fields)
        for k in range(STAVES):
            mid_a = -np.pi + k * step
            mine = inside & (angle_gap(theta, mid_a) <= step * 0.5 + 1e-6)
            if not mine.any():
                continue
            # Out along its bearing: sideways on screen, and down the floor's depth for the near ones.
            dx = math.sin(mid_a) * spread * scale
            dy = (-math.cos(mid_a) * spread * DEPTH - drop * (1.0 + 0.6 * abs(math.sin(mid_a)))) * scale
            landed = shifted(mine.astype(np.float32), dx, dy, frame.px) > 0
            for f in range(len(fields)):
                moved[f] = np.where(landed, shifted(fields[f] * mine, dx, dy, frame.px), moved[f])
        fields = moved
    cover, shade, seam, rivet, spec = fields
    sheen_fill = mix(STEEL_DEEP, STEEL_LIGHT, shade ** 1.4)
    iron_fill = mix(IRON_DARK, IRON_LIGHT, shade ** 1.2)
    body = mix(sheen_fill, iron_fill, iron)
    alpha = (0.1 + 0.1 * shade) * sheen * (1.0 - iron) + (0.55 + 0.35 * shade) * iron
    # The rim: the bell's outline, which is what reads round the hero against the floor, over a soot edge for the lava.
    rim = np.clip(cover - shrink(cover, 1.8), 0.0, 1.0)
    outer = np.clip(grow(cover, 1.3) - cover, 0.0, 1.0)
    frame.paint(STEEL_SOOT, blur(outer, 0.5) * (0.38 + 0.25 * iron) * max(sheen, iron))
    frame.paint(body, blur(cover, 0.3) * alpha)
    # A cool band of light up the bell's far edges, as plate catches the room.
    frame.paint(STEEL, blur(np.clip(cover - shrink(cover, 5.0), 0.0, 1.0), 1.2) * 0.22 * sheen * (1.0 - iron))
    seam_color = mix(mix(STEEL_PALE, IRON_DARK, iron * 0.7), WHITE, flash)
    frame.paint(seam_color, blur(seam * cover, 0.35) * min(1.0, (0.36 * sheen + 0.75 * iron + flash) * seams))
    frame.paint(mix(STEEL_LIGHT, WHITE, flash), blur(rim, 0.4) * min(1.0, 0.85 * max(sheen, iron) + flash))
    frame.paint(mix(STEEL_PALE, WHITE, 0.5), blur(rivet * cover, 0.3) * min(1.0, 0.85 * max(sheen, iron) + flash))
    if glint is not None:
        frame.paint(WHITE, blur(spec * cover, 0.8) * 0.8 * sheen)
    if flash > 0:
        frame.paint(STEEL_PALE, blur(cover, 0.6) * 0.45 * flash)


def resolve_close_frame(index):
    """The staves flying in as solid iron, the clang as they lock (white along every seam, sparks off the rivets), and
    the white settling into the sheen."""
    frame = Frame(*RESOLVE_EXTENT, RESOLVE_FAST_TPU)
    scale, spread, iron, flash, sheen = ((1.14, 0.2, 0.8, 0.0, 0.0), (1.0, 0.0, 0.35, 1.0, 0.6),
                                         (1.0, 0.0, 0.0, 0.4, 1.0))[index]
    if index == 0:
        # The air the staves push before them: pale streaks closing in along their flight.
        for k in range(1, STAVES // 2):
            for side in (-1, 1):
                a = side * k * 2 * np.pi / STAVES
                x0 = math.sin(a) * (RESOLVE_BELL.R + spread) * scale
                for j, h in enumerate((0.3, 0.7, 1.05)):
                    spark(frame, x0 - side * 0.02, h * scale, 180.0 if side > 0 else 0.0, 0.09 * (1.0 - 0.15 * j),
                          0.018, STEEL_PALE, STEEL_LIGHT, 0.6)
    paint_bell(frame, RESOLVE_BELL, RESOLVE_LIGHT, scale=scale, spread=spread, iron=iron, flash=flash, sheen=sheen)
    if index == 1:
        # The clang: sparks flung off the rivets where the staves met.
        rng = np.random.default_rng(41)
        for k in range(14):
            side = 1 if k % 2 else -1
            h = HOOPS[k % 2] + rng.uniform(-0.05, 0.05)
            x = side * rng.uniform(0.25, 0.6)
            fly = side * rng.uniform(20, 100)
            if side < 0:
                fly = 180 - fly
            spark(frame, x + 0.1 * math.cos(math.radians(fly)), h + 0.1 * math.sin(math.radians(fly)), fly,
                  rng.uniform(0.08, 0.14), 0.02, WHITE, STEEL_LIGHT)
    return frame


def resolve_hold_frame(index, count):
    """The guard holding: the bell a steel-blue sheen the hero shows through, its glint sweeping round it once a loop."""
    frame = Frame(*RESOLVE_EXTENT, RESOLVE_TPU)
    turn = -1.1 + 2.2 * index / count
    glint = np.array([math.sin(turn), -math.cos(turn) * 0.9, 0.55])
    glint /= np.linalg.norm(glint)
    paint_bell(frame, RESOLVE_BELL, RESOLVE_LIGHT, glint=glint, sheen=0.94 + 0.06 * math.cos(2 * np.pi * index / count))
    return frame


def resolve_end_frame(index):
    """The guard ending: its seams flash white and crack, the staves fall away from the hero, then only steel flakes."""
    frame = Frame(*RESOLVE_EXTENT, RESOLVE_FAST_TPU)
    spread, drop, iron, flash, sheen, crack = ((0.03, 0.0, 0.0, 0.8, 1.0, 0.5), (0.14, 0.1, 0.4, 0.1, 0.4, 1.0),
                                               (0.0, 0.0, 0.0, 0.0, 0.0, 0.0))[index]
    if index <= 1:
        fade = (1.0, 0.6)[index]
        paint_bell(frame, RESOLVE_BELL, RESOLVE_LIGHT, spread=spread, drop=drop, iron=iron * fade, flash=flash,
                   sheen=sheen * fade, crack=crack, seams=fade)
    if index >= 1:
        # Steel flakes shed as it breaks, falling and thinning.
        rng = np.random.default_rng(53)
        for k in range(20):
            a = rng.uniform(-np.pi, np.pi)
            h0 = rng.uniform(0.15, 1.35)
            out = 0.62 + 0.12 * index + rng.uniform(0, 0.08)
            x = math.sin(a) * out
            y = h0 - 0.14 * index * index * rng.uniform(0.6, 1.0) - math.cos(a) * 0.12
            size = rng.uniform(0.024, 0.04)
            alpha = (0.0, 0.95, 0.6)[index]
            d = np.hypot((frame.X - x) / 1.6, frame.Y - y)
            frame.paint(STEEL_SOOT, smoothstep(size * 1.5, size * 1.1, d) * 0.45 * alpha)
            frame.paint(mix(IRON_LIGHT, STEEL_LIGHT, rng.random()), smoothstep(size, size * 0.6, d) * alpha)
    return frame


def resolve_rivets_frame(index):
    """The ring of rivets the guard stamps on the floor round the feet, a unit's radius across and half as deep: struck
    with a flash and a puff of dust at each, then joined by a steel band, then dimming."""
    frame = Frame(-1.14, -0.62, 1.14, 0.62, RESOLVE_FAST_TPU)
    X, Y = frame.X, frame.Y
    d = np.sqrt(X ** 2 + (Y / DEPTH) ** 2)
    strength = (1.0, 0.9, 0.55)[index]
    if index == 0:
        frame.paint(STEEL_PALE, smoothstep(0.06, 0.0, np.abs(d - 0.98)) * 0.55)
        frame.paint(WHITE, smoothstep(0.02, 0.006, np.abs(d - 0.98)) * 0.9)
    if index == 1:
        frame.paint(STEEL_SOOT, smoothstep(0.05, 0.024, np.abs(d - 0.98)) * 0.45)
        frame.paint(STEEL, smoothstep(0.026, 0.012, np.abs(d - 0.98)) * 0.75)
        frame.paint(STEEL_LIGHT, smoothstep(0.011, 0.003, np.abs(d - 0.985)) * 0.6)
    for k in range(RIVET_COUNT):
        a = 2 * np.pi * k / RIVET_COUNT  # one at each quarter of the ring, so it stands square under the hero
        x, y = math.cos(a) * 0.98, math.sin(a) * 0.98 * DEPTH
        if index == 0:
            dust_puff(frame, x, y + 0.02, 0.09, 0.55, 100 + k)
        rr = np.hypot(X - x, (Y - y) / 0.62)
        frame.paint(STEEL_SOOT, smoothstep(0.07, 0.052, rr) * 0.7 * strength)
        frame.paint(mix(IRON, STEEL, 0.4), smoothstep(0.05, 0.038, rr) * strength)
        hr = np.hypot(X - (x - 0.014), (Y - (y + 0.009)) / 0.62)
        frame.paint(STEEL_PALE if index else WHITE, smoothstep(0.026, 0.01, hr) * strength)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# Shield Rush: facing +x, the head at the pivot (the hero's middle), the wake trailing behind it.

RUSH_TPU = 80.0
RUSH_FRAMES = 5
# Speed lines: across the wake (-1 to 1), how far back each reaches (a share of the wake) and how far behind the head it
# starts.
RUSH_LINES = ((-0.8, 0.75, 0.25), (-0.52, 1.0, 0.05), (-0.24, 0.62, 0.3), (0.02, 1.08, 0.0), (0.27, 0.8, 0.18),
              (0.53, 0.95, 0.08), (0.8, 0.6, 0.32))


def rush_streak_frame(index, noise):
    frame = Frame(-2.05, -0.56, 0.6, 0.56, RUSH_TPU)
    X, Y = frame.X, frame.Y
    length, strength, bow, ragged = ((0.6, 1.0, 1.0, 0.0), (1.3, 1.0, 0.85, 0.0), (1.75, 0.95, 0.55, 0.15),
                                     (1.65, 0.7, 0.25, 0.45), (1.3, 0.38, 0.0, 0.8))[index]
    t = np.clip((0.15 - X) / length, 0.0, 1.0)
    half = 0.36 * np.clip(1.0 - t, 0.0, 1.0) ** 0.6 * np.sqrt(np.clip((0.2 - X) / 0.2, 0.0, 1.0))
    wake = smoothstep(half + 0.01, half - 0.04, np.abs(Y)) * (X > 0.15 - length) * (half > 0.0)
    tear = smoothstep(0.35, 0.75, noise(X * 1.3 + 0.2 * index, Y * 2.0) * 0.5 + 0.5 + t * ragged)
    wake = wake * (1.0 - tear * ragged)
    frame.paint(STEEL_SOOT, blur(grow(wake, 1.2), 0.8) * 0.3 * strength)
    frame.paint(mix(STEEL_LIGHT, STEEL_DEEP, t ** 0.8), blur(wake, 0.7) * (0.62 * (1.0 - t) ** 1.1) * strength)
    for k, (across, reach, start) in enumerate(RUSH_LINES):
        x0 = 0.12 - 0.3 * start
        x1 = x0 - length * reach * (0.8 + 0.2 * math.sin(index * 1.3 + k))
        y0 = across * 0.3
        d = segment_distance(X, Y, x0, y0, x1, y0 * 0.75)
        along = np.clip((X - x1) / max(x0 - x1, 1e-6), 0.0, 1.0)
        frame.paint(mix(STEEL_LIGHT, WHITE, along), stroke(frame, d, 0.026 * (0.3 + 0.7 * along)) * along ** 0.8 * strength)
    if bow > 0:
        # The bow wave just before the shield: an arc of shocked air, white at its crest.
        ex, ey = (X - 0.02) / 0.26, Y / 0.4
        r = np.sqrt(ex * ex + ey * ey)
        front = smoothstep(0.15, 0.55, ex / np.maximum(r, 1e-6))
        frame.paint(STEEL_LIGHT, smoothstep(0.24, 0.0, np.abs(r - 1.0)) * front * 0.35 * bow)
        frame.paint(WHITE, smoothstep(0.09, 0.03, np.abs(r - 1.0)) * front * 0.9 * bow)
    return frame


def rush_impact_frame(index):
    """Where the dash stops: a white flash at the shield, a crescent of shocked air before it, steel sparks flung forward
    and floor dust round it, the crescent thinning as the dust settles."""
    frame = Frame(-0.75, -0.8, 0.85, 0.8, RUSH_TPU)
    X, Y = frame.X, frame.Y
    flash, arc, reach, dust = ((1.0, 1.0, 0.0, 0.55), (0.45, 0.85, 0.5, 0.75), (0.1, 0.45, 0.85, 0.7),
                               (0.0, 0.0, 1.0, 0.5), (0.0, 0.0, 1.0, 0.25))[index]
    spread = 1.0 + 0.22 * index
    for k in range(8):
        a = 2 * np.pi * (k + 0.4 * math.sin(k * 1.7)) / 8
        dust_puff(frame, -0.08 + math.cos(a) * 0.2 * spread, math.sin(a) * 0.26 * spread, 0.11 + 0.025 * index,
                  dust * (0.65 if math.cos(a) > 0.3 else 1.0), 200 + k)
    if flash > 0:
        d = np.hypot(X - 0.08, Y / 1.2)
        frame.paint(STEEL_LIGHT, smoothstep(0.4, 0.0, d) ** 1.5 * 0.6 * flash)
        frame.paint(WHITE, smoothstep(0.15, 0.0, d) * flash)
    if arc > 0:
        cx = 0.12 + 0.1 * index
        ex, ey = (X - cx) / (0.16 + 0.07 * index), Y / (0.36 + 0.1 * index)
        r = np.sqrt(ex * ex + ey * ey)
        front = smoothstep(0.0, 0.5, ex / np.maximum(r, 1e-6))
        frame.paint(STEEL_SOOT, smoothstep(0.24, 0.1, np.abs(r - 1.12)) * front * 0.3 * arc)
        frame.paint(STEEL_LIGHT, smoothstep(0.26, 0.0, np.abs(r - 1.0)) * front * 0.5 * arc)
        frame.paint(WHITE, smoothstep(0.1, 0.03, np.abs(r - 1.0)) * front * arc)
    if index <= 3:
        rng = np.random.default_rng(71)
        for k in range(7):
            angle = rng.uniform(-62, 62)
            speed = rng.uniform(0.55, 0.85)
            head = 0.12 + speed * (0.12 + reach * 0.45)
            hx, hy = math.cos(math.radians(angle)) * head + 0.08, math.sin(math.radians(angle)) * head * 0.8
            spark(frame, hx, hy, angle, 0.07 + 0.09 * (1 - reach), 0.02, WHITE if index < 2 else STEEL_PALE,
                  STEEL_LIGHT, 1.0 - 0.6 * reach)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# Flame Sigil: a unit floor ellipse, half as deep as it is wide, which the view grows to the glyph's radius.

SIGIL_TPU = 130.0
SIGIL_IGNITE_TPU = 100.0
SIGIL_FRAMES = 4
SIGIL_EXTENT = (-1.12, -0.6, 1.12, 0.74)
RUNES = 12
OUTER, INNER, GRATE = 0.975, 0.8, 0.5

# Furnace runes: strokes in a rune's own frame (u along the ring, v across it, each -1 to 1).
RUNE_SHAPES = (
    ((-0.5, -0.9, 0.0, 0.9), (0.0, 0.9, 0.5, -0.9), (-0.3, -0.1, 0.3, -0.1)),  # a peak and its bar: the anvil's horn
    ((0.0, -0.9, 0.0, 0.9), (-0.55, 0.3, 0.0, 0.9), (0.55, 0.3, 0.0, 0.9)),  # a flame's arrow
    ((-0.5, -0.8, 0.5, 0.8), (0.5, -0.8, -0.5, 0.8), (-0.5, 0.0, 0.5, 0.0)),  # the crossed tongs
    ((-0.45, -0.9, -0.45, 0.9), (0.45, -0.9, 0.45, 0.9), (-0.45, 0.35, 0.45, -0.35)),  # the bellows
    ((0.0, -0.9, 0.0, 0.2), (-0.55, 0.2, 0.55, 0.2), (-0.55, 0.2, 0.0, 0.9), (0.55, 0.2, 0.0, 0.9)),  # the hammer
)


def sigil_fields(frame, noise):
    X, Y = frame.X, frame.Y
    d = np.sqrt(X ** 2 + (Y / DEPTH) ** 2)
    angle = np.arctan2(Y / DEPTH, X)
    wobble = noise(np.cos(angle) * 0.7, np.sin(angle) * 0.7) * 0.006
    return d + wobble, angle


def rune_cover(frame, d, angle, heat_of):
    """Coverage of the twelve runes between the band's two lines, and the heat each burns at."""
    cover = np.zeros_like(d)
    heat = np.zeros_like(d)
    mid_r = (OUTER + INNER) * 0.5
    half_v = (OUTER - INNER) * 0.5 - 0.03
    for k in range(RUNES):
        a0 = 2 * np.pi * (k + 0.5) / RUNES
        u = ((angle - a0 + np.pi) % (2 * np.pi) - np.pi) * mid_r / 0.055  # rune units along the ring
        v = (d - mid_r) / half_v
        near = (np.abs(u) < 1.6) & (np.abs(v) < 1.6)
        if not near.any():
            continue
        c = np.zeros_like(d)
        for x0, y0, x1, y1 in RUNE_SHAPES[k % len(RUNE_SHAPES)]:
            dist = segment_distance(u, v, x0, y0, x1, y1) * 0.055  # back to world units along the floor
            c = np.maximum(c, np.clip((0.019 - dist) / (frame.px * 1.6) + 0.5, 0.0, 1.0))
        c = c * near
        cover = np.maximum(cover, c)
        heat = np.where(c > 0, np.maximum(heat, heat_of(k)), heat)
    return cover, heat


def paint_sigil(frame, noise, fire_noise, phase, ignite=None):
    """The glyph at loop angle phase; ignite (0, 1) paints its ignition instead: the grate, then the band, white-hot."""
    X, Y = frame.X, frame.Y
    d, angle = sigil_fields(frame, noise)
    floor_d = np.hypot(X, Y / DEPTH)
    band_on, runes_on, grate_heat, band_heat, licks = 1.0, 1.0, 0.0, 0.0, 1.0
    if ignite == 0:
        band_on, runes_on, grate_heat, licks = 0.45, 0.0, 1.0, 0.0
    elif ignite == 1:
        band_on, runes_on, grate_heat, band_heat, licks = 1.0, 1.0, 0.5, 1.0, 1.6
    # The faint wash over the floor it holds, strongest toward the band, and the soot rim outside it.
    wash = smoothstep(0.15, 0.85, d) * (d < OUTER)
    frame.paint(AMBER, wash * (0.1 + 0.04 * noise(X * 2 + 1.0, Y * 4 - 0.3 * phase)) * band_on)
    frame.paint(NEUTRAL_SOOT, smoothstep(0.05, 0.0, np.abs(d - OUTER - 0.03)) * (d > OUTER) * 0.5 * band_on)
    # The band: two lines, a glow between them.
    frame.paint(AMBER, smoothstep(0.1, 0.0, np.abs(d - (OUTER + INNER) * 0.5) - 0.06) * 0.22 * band_on)
    for radius, width in ((OUTER, 0.024), (INNER, 0.018)):
        frame.paint(GOLD_DARK, smoothstep(width * 2.2, width, np.abs(d - radius)) * 0.6 * band_on)
        frame.paint(mix(GOLD, WHITE_HOT, band_heat), smoothstep(width, width * 0.3, np.abs(d - radius)) * band_on)
        frame.paint(mix(GOLD_PALE, WHITE_HOT, band_heat), smoothstep(width * 0.35, 0.0, np.abs(d - radius)) * band_on)
    # The grate: an inner ring and six spokes out to the band, tapering as they go.
    grate = smoothstep(0.016, 0.005, np.abs(d - GRATE))
    along = np.clip((floor_d - GRATE) / (INNER - GRATE), 0.0, 1.0)
    for k in range(6):
        a = 2 * np.pi * k / 6 + np.pi / 6
        dist = segment_distance(X, Y / DEPTH, math.cos(a) * (GRATE + 0.03), math.sin(a) * (GRATE + 0.03),
                                math.cos(a) * (INNER - 0.04), math.sin(a) * (INNER - 0.04))
        grate = np.maximum(grate, np.clip((0.018 * (1.0 - 0.5 * along) - dist) / (frame.px * 1.5) + 0.5, 0.0, 1.0))
    frame.paint(GOLD_DARK, blur(grow(grate, 1.0), 0.5) * 0.5 * (0.55 + 0.45 * grate_heat))
    frame.paint(mix(GOLD, WHITE_HOT, grate_heat), grate * (0.8 + 0.2 * grate_heat))
    if grate_heat > 0:
        frame.paint(WHITE_HOT, smoothstep(0.35, 0.0, floor_d) * 0.6 * grate_heat)
    # The runes, their heat running round the ring over the loop.
    if runes_on > 0:
        cover, heat = rune_cover(frame, d, angle, lambda k: 0.5 + 0.5 * math.cos(2 * np.pi * k / RUNES * 2 - phase))
        heat = np.maximum(heat, band_heat)
        frame.paint(GOLD_DARK, blur(grow(cover, 0.9), 0.6) * 0.6 * runes_on)
        frame.paint(AMBER, blur(grow(cover, 2.5), 2.0) * 0.35 * heat * runes_on)
        frame.paint(mix(GOLD_DARK, GOLD, np.clip(heat * 1.6, 0.0, 1.0)), cover * runes_on)
        frame.paint(mix(GOLD_PALE, WHITE_HOT, band_heat), smoothstep(0.6, 0.95, heat) * blur(shrink(cover, 0.5), 0.3))
    # Licks off the band: tallest along its far side, low round its sides, none across the near side, which keeps the
    # floor clear in front of what stands in it.
    if licks > 0:
        tongues = []
        count = 28
        for k in range(count):
            a = 2 * np.pi * (k + 0.35 * math.sin(k * 2.1)) / count + 0.1
            back = smoothstep(-0.35, 0.8, math.sin(a))
            if back < 0.08:
                continue
            h = (0.045 + 0.05 * ((k * 0.618) % 1.0)) * back * licks
            x, y = math.cos(a) * (OUTER - 0.012), math.sin(a) * (OUTER - 0.012) * DEPTH
            tongues.append(dict(x=x, y=y - 0.012, h=h, w=0.026 + 0.008 * ((k * 0.37) % 1.0), lean=0.012 * math.cos(a),
                                wave=0.006, ph=float(k), sharp=1.3, flick=0.15, sway=0.008, foot=0.02))
        skill.paint_fire(frame, tongues, phase, 0.55 + 0.3 * (ignite == 1), fire_noise, glow=0.08, rim=0.5,
                         palette=GOLD_FIRE)


def sigil_glyph_frame(index, noise, fire_noise):
    frame = Frame(*SIGIL_EXTENT, SIGIL_TPU)
    paint_sigil(frame, noise, fire_noise, 2 * np.pi * index / SIGIL_FRAMES)
    return frame


def sigil_ignite_frame(index, noise, fire_noise):
    frame = Frame(*SIGIL_EXTENT, SIGIL_IGNITE_TPU)
    paint_sigil(frame, noise, fire_noise, 0.0, ignite=index)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# Cinder Ward: round Ilya's feet (the pivot), ash and cinders whirling in three tilted orbits inside a faint ash shell.

WARD_TPU = 100.0
WARD_FAST_TPU = 75.0  # its rise and its break, a fifth of a second each
WARD_FRAMES = 6
WARD_EXTENT = (-0.95, -0.5, 0.95, 1.75)
WARD_BELL = Bell(0.5, 0.62, 0.74)
# Each orbit: the height of its centre, its radius on the floor, its tilt (degrees, screen) and its direction. Three
# gusts ride each, evenly spaced, so the loop closes when each has moved on to the next one's place.
ORBITS = ((0.36, 0.56, 10.0, 1), (0.74, 0.52, -14.0, -1), (1.1, 0.42, 7.0, 1))
GUSTS = 3


def orbit_point(orbit, t, spread=1.0, lift=0.0):
    """Screen point of the orbit at angle t, and its depth (-1 near the camera, +1 far)."""
    cy, radius, tilt, _ = orbit
    x, z = math.cos(t) * radius * spread, math.sin(t) * radius * spread
    sx, sy = x, z * DEPTH * 0.62
    c, s = math.cos(math.radians(tilt)), math.sin(math.radians(tilt))
    return sx * c - sy * s, cy + lift + sx * s + sy * c, math.sin(t)


def paint_gust(frame, orbit, head, direction, span, alpha, spread=1.0, lift=0.0, fall=0.0, width=0.046, heat=1.0):
    """A gust of ash along an orbit: a ribbon tapering back from its head over span radians - a soft ash band round a
    bright core - with a cinder leading it and two flakes riding it, all dimmer where the orbit passes behind her."""
    beads = 40
    band = np.zeros_like(frame.X)
    core = np.zeros_like(frame.X)
    for j in range(beads):
        t = head - direction * span * j / (beads - 1)
        x, y, depth = orbit_point(orbit, t, spread, lift)
        y -= fall * (j / beads)
        near = 0.5 - 0.5 * depth
        taper = 1.0 - j / beads
        r = width * (0.25 + 0.75 * taper) * (0.7 + 0.3 * near)
        d = np.hypot(frame.X - x, frame.Y - y)
        a = (0.3 + 0.7 * near) * taper ** 0.8
        band = np.maximum(band, smoothstep(r * 1.4, r * 0.3, d) * a)
        core = np.maximum(core, smoothstep(r * 0.6, r * 0.15, d) * a)
    frame.paint(NEUTRAL_SOOT, blur(band, 0.8) * 0.22 * alpha)
    frame.paint(ASH, band * 0.6 * alpha)
    frame.paint(ASH_WHITE, core * 0.9 * alpha)
    x, y, depth = orbit_point(orbit, head, spread, lift)
    near = 0.5 - 0.5 * depth
    nx, ny, _ = orbit_point(orbit, head + direction * 0.05, spread, lift)
    cinder(frame, x, y, 0.022 * (0.75 + 0.35 * near), heat * (0.55 + 0.45 * near), 1.8,
           math.degrees(math.atan2(ny - y, nx - x)), alpha * (0.45 + 0.55 * near))
    for f, back in enumerate((0.35, 0.7)):
        t = head - direction * span * back
        fx, fy, fdepth = orbit_point(orbit, t, spread, lift)
        fy -= fall * back
        fnear = 0.5 - 0.5 * fdepth
        gx, gy, _ = orbit_point(orbit, t + direction * 0.05, spread, lift)
        flake(frame, fx, fy + 0.02 * (f - 0.5), 0.04 * (0.75 + 0.3 * fnear),
              math.degrees(math.atan2(gy - fy, gx - fx)) + 25 * (f - 0.5), 0.5 + 0.45 * fnear,
              alpha * (0.4 + 0.6 * fnear))


def paint_orbits(frame, phase, spread=1.0, lift=0.0, alpha=1.0, span=1.1, heat=1.0, fall=0.0):
    for o, orbit in enumerate(ORBITS):
        direction = orbit[3]
        for g in range(GUSTS):
            head = 2 * np.pi * g / GUSTS + direction * phase * 2 * np.pi / GUSTS + o * 0.9
            paint_gust(frame, orbit, head, direction, span, alpha, spread, lift, fall, heat=heat)


def paint_ward_shell(frame, strength, flash=0.0):
    """The faint ash shell round her: a haze inside the bell and its outline, ash-white."""
    inside, h, theta, r, n = WARD_BELL.solve(frame.X, frame.Y)
    cover = inside.astype(np.float32)
    rim = np.clip(cover - shrink(cover, 1.5), 0.0, 1.0)
    light = np.clip(n @ (np.array([-0.4, -0.6, 0.7]) / np.linalg.norm([-0.4, -0.6, 0.7])), 0.0, 1.0)
    frame.paint(NEUTRAL_SOOT, blur(np.clip(grow(cover, 1.2) - cover, 0.0, 1.0), 0.5) * 0.2 * strength)
    frame.paint(ASH, blur(cover, 0.6) * (0.06 + 0.07 * light) * strength)
    frame.paint(mix(ASH, ASH_WHITE, 0.6 + 0.4 * flash), blur(rim, 0.6) * (0.42 + 0.5 * flash) * strength)


def ward_shell_frame(index):
    frame = Frame(*WARD_EXTENT, WARD_TPU)
    paint_ward_shell(frame, 1.0)
    paint_orbits(frame, index / WARD_FRAMES)
    return frame


def ward_raise_frame(index):
    """Ash and cinders whirling up from the floor round her feet into the shell, which flashes as it closes."""
    frame = Frame(*WARD_EXTENT, WARD_FAST_TPU)
    if index == 2:
        paint_ward_shell(frame, 1.0, flash=1.0)
        paint_orbits(frame, 0.0, span=1.5)
        return frame
    rise = (0.2, 0.62)[index]
    for o, orbit in enumerate(ORBITS):
        low = (orbit[0] * rise, orbit[1] * (1.3 - 0.3 * rise), orbit[2] * rise, orbit[3])
        for g in range(GUSTS):
            head = 2 * np.pi * g / GUSTS + o * 0.9 - (1 - index) * 0.8 * orbit[3]
            paint_gust(frame, low, head, orbit[3], 0.8 + 0.4 * index, 0.9)
    if index == 1:
        paint_ward_shell(frame, 0.45)
    return frame


def ward_break_frame(index):
    """The ward ending or spent: the shell flashes and blows apart, its ash and cinders flung out and falling."""
    frame = Frame(-1.3, -0.62, 1.3, 1.9, WARD_FAST_TPU)
    flash, strength, spread, fall, alpha = ((1.0, 1.0, 1.1, 0.0, 1.0), (0.2, 0.4, 1.45, 0.1, 0.8),
                                            (0.0, 0.0, 1.8, 0.28, 0.45))[index]
    if strength > 0:
        paint_ward_shell(frame, strength, flash=flash)
    paint_orbits(frame, 0.2 * index, spread=spread, alpha=alpha, span=0.7 if index else 1.1, heat=1.0 - 0.3 * index,
                 fall=fall)
    return frame


def ward_flare_frame(index):
    """At a striker whose blow the ward absorbed: a white-hot flash in a gold glow, cinders flung out round it, and a
    puff of ash."""
    frame = Frame(-0.55, -0.55, 0.55, 0.55, WARD_TPU)
    X, Y = frame.X, frame.Y
    d = np.hypot(X, Y)
    flash = (1.0, 0.45, 0.0)[index]
    if index >= 1:
        frame.paint(ASH, smoothstep(0.18 + 0.07 * index, 0.04, d) * (0.4 if index == 1 else 0.22))
    frame.paint(GOLD, smoothstep(0.3, 0.0, d) ** 1.5 * 0.65 * max(flash, 0.15))
    frame.paint(WHITE_HOT, smoothstep(0.11 * (1 + 0.3 * index), 0.0, d) * flash)
    for k in range(8):
        a = 2 * np.pi * (k + 0.25 * math.sin(k * 3.1)) / 8
        out = 0.12 + 0.1 * index + 0.03 * ((k * 0.618) % 1.0)
        cinder(frame, math.cos(a) * out, math.sin(a) * out, 0.016 * (1 - 0.2 * index),
               1.0 - 0.35 * index, 2.0, math.degrees(a), 1.0)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# The colour rules, as PaintedHeroSkillEffectTests reads them: a vivid texel (saturation and value at least 0.35) is
# never red-orange (hue under 30 or from 345) nor orchid (280-330), and every bright, coloured one (alpha over 64, value
# over 0.7, saturation at least 0.2) is the hero's own: steel for the Vanguard, gold for Ilya.

HUES = {"Vanguard": (190.0, 240.0), "Ilya": (32.0, 60.0)}


def hsv(rgb):
    """Hue in degrees, saturation and value of straight colours in 0-1, as Unity's Color.RGBToHSV reads them."""
    r, g, b = rgb[:, 0], rgb[:, 1], rgb[:, 2]
    top, low = rgb.max(axis=1), rgb.min(axis=1)
    spread = top - low
    safe = np.where(spread > 0, spread, 1.0)
    hue = np.where(top == low, 0.0,
                   np.where((b > g) & (b > r), 4.0 + (r - g) / safe,
                            np.where(g > r, 2.0 + (b - r) / safe, (g - b) / safe)))
    hue = (hue / 6.0) % 1.0 * 360.0
    sat = np.where(top > 0, spread / np.where(top > 0, top, 1.0), 0.0)
    return hue, sat, top


def check_colours(strip, hero):
    rgba = np.concatenate([img.reshape(-1, 4) for img in strip.images]).astype(np.float32) / 255.0
    shown = rgba[rgba[:, 3] > 32 / 255.0]
    hue, sat, val = hsv(shown[:, :3])
    vivid = (sat >= 0.35) & (val >= 0.35)
    red = vivid & ((hue < 30.0) | (hue >= 345.0))
    orchid = vivid & (hue >= 280.0) & (hue <= 330.0)
    bright = (shown[:, 3] > 64 / 255.0) & (val > 0.7) & (sat >= 0.2)
    lo, hi = HUES[hero]
    stray = bright & ((hue < lo) | (hue > hi))
    problems = []
    for name, mask in (("red-orange", red), ("orchid", orchid), ("off its hue", stray)):
        if mask.any():
            sample = shown[np.argmax(mask)][:3] * 255
            problems.append(f"{strip.name}: {mask.sum()} texels {name}, e.g. {sample.astype(int)} hue {hue[mask][0]:.0f}")
    return problems, int(bright.sum())


# ----------------------------------------------------------------------------------------------------------------------


def build_families():
    noise = Noise(7, period=0.9, feature=0.05)
    ring_noise = Noise(11, period=1.0, feature=0.08)
    fine = Noise(29, period=1.0, feature=0.04)
    holds = 4
    resolve = [skill.Strip("resolveClose", [resolve_close_frame(i) for i in range(3)]),
               skill.Strip("resolveHold", [resolve_hold_frame(i, holds) for i in range(holds)]),
               skill.Strip("resolveEnd", [resolve_end_frame(i) for i in range(3)]),
               skill.Strip("resolveRivets", [resolve_rivets_frame(i) for i in range(3)])]
    rush = [skill.Strip("rushStreak", [rush_streak_frame(i, fine) for i in range(RUSH_FRAMES)]),
            skill.Strip("rushImpact", [rush_impact_frame(i) for i in range(5)])]
    sigil = [skill.Strip("sigilIgnite", [sigil_ignite_frame(i, ring_noise, noise) for i in range(2)]),
             skill.Strip("sigilGlyph", [sigil_glyph_frame(i, ring_noise, noise) for i in range(SIGIL_FRAMES)])]
    ward = [skill.Strip("wardRaise", [ward_raise_frame(i) for i in range(3)]),
            skill.Strip("wardShell", [ward_shell_frame(i) for i in range(WARD_FRAMES)]),
            skill.Strip("wardBreak", [ward_break_frame(i) for i in range(3)]),
            skill.Strip("wardFlare", [ward_flare_frame(i) for i in range(3)])]
    return {"VFX_Skill_Resolve": ("Vanguard", resolve), "VFX_Skill_Rush": ("Vanguard", rush),
            "VFX_Skill_Sigil": ("Ilya", sigil), "VFX_Skill_Ward": ("Ilya", ward)}


FIELDS = ("resolveClose", "resolveHold", "resolveEnd", "resolveRivets", "rushStreak", "rushImpact", "sigilIgnite",
          "sigilGlyph", "wardRaise", "wardShell", "wardBreak", "wardFlare")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--frames", help="also save every frame as a PNG in this directory")
    parser.add_argument("--dry", action="store_true", help="paint and check, but write nothing into Assets")
    args = parser.parse_args()

    families = build_families()
    problems = []
    for name, (hero, strips) in families.items():
        for strip in strips:
            found, bright = check_colours(strip, hero)
            problems += found
            if bright == 0:
                problems.append(f"{strip.name}: nothing bright in the hero's own colour")
    if problems:
        raise SystemExit("Colour rules broken:\n  " + "\n  ".join(problems))

    written = {}
    total = 0
    for name, (hero, strips) in families.items():
        sheet = skill.bleed(skill.pack(strips))
        size = skill.astc_bytes(sheet.shape[1], sheet.shape[0])
        total += size
        detail = "  ".join(f"{s.name} {len(s.images)}x{s.images[0].shape[1]}x{s.images[0].shape[0]}@{s.tpu:g}"
                           for s in strips)
        print(f"{name}.png {sheet.shape[1]}x{sheet.shape[0]}  ASTC 4x4 {size / 1024:.0f} KiB  {detail}")
        if args.frames:
            os.makedirs(args.frames, exist_ok=True)
            for s in strips:
                for i, img in enumerate(s.images):
                    Image.fromarray(img, "RGBA").save(os.path.join(args.frames, f"{s.name}_{i}.png"))
        if args.dry:
            continue
        png = os.path.join(OUT_DIR, name + ".png")
        Image.fromarray(sheet, "RGBA").save(png, optimize=True)
        meta = png + ".meta"
        guid = skill.existing_guid(meta)
        skill.write(meta, skill.TEXTURE_META.format(guid=guid, sprite_id=skill.existing_sprite_id(meta),
                                                    tpu=skill.number(strips[0].tpu)))
        for s in strips:
            written[s.name] = (s, guid, sheet)
    print(f"total ASTC 4x4 {total / 1024:.0f} KiB ({total / 1048576:.2f} MiB)")
    if not args.dry:
        skill.write_fields(ASSET_PATH, [(f"_{name}", f"  _{name}:\n" + skill.strip_yaml(*written[name], 4))
                                        for name in FIELDS], after="_forgeFlash")


if __name__ == "__main__":
    main()
