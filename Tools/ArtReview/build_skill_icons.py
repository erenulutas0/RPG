"""Paints the four new hero-skill icons (docs/57 section 3) in the family of the two shipped ability icons.

    python Tools/ArtReview/build_skill_icons.py                    # the four PNGs (and a .meta for any that lacks one)
    python Tools/ArtReview/build_skill_icons.py --out DIR          # the four PNGs into DIR, the project untouched
    python Tools/ArtReview/build_skill_icons.py --cells DIR        # also each glyph's 96 cells, magnified, for review

The shipped two (Assets/_Project/Art/UI/UI_Icon_ForgeBurst_v1.png, UI_Icon_CrucibleVent_v1.png, the latter painted by
build_ability_icons.py) are a 1254-texel opaque square: a dark navy field and one glowing glyph of chunky pixel art on a
96-cell grid (about 13 texels a cell) filling some two thirds of the width, with a two-band outline (the family's rim
colour inside, a dark one outside) and the glyph's light spread into the field. AbilityButtonView shows the square in a
round well (a Mask on the Knob sprite) under the bezel, so everything that matters stays inside its inscribed circle.

Each new glyph is painted on the same grid with the gear icons' painting library (build_upgrade_icons.py: parts banded
onto a few picked tones, lit from the top left, outlined in their own darkest tone), then given the shipped icons' rim,
outline, glow and softening over the shipped icons' own field (build_ability_icons.field: the Forge Burst square with
its glyph painted out):

  - Iron Resolve (the Vanguard, docs/57 3.1): its breastplate face on - blue-lacquered steel plate with a brass gorget,
    rivets, chevron and belt, pauldrons on both shoulders, two lames below - tempered: a white glint and a blue sheen.
  - Shield Rush (the Vanguard): a brass heater shield in a steel rim, tilted into a charge to the right, three
    steel-blue speed streaks behind it and the strike's white flare at its leading edge.
  - Flame Sigil (Ilya, docs/57 3.2): a furnace glyph on the floor - an ember ring in perspective notched with white-hot
    runes round an inner ring, the floor inside it lit, and a curling flame rising from its centre with two small ones.
  - Cinder Ward (Ilya): Ilya hooded in her ash robe, a dark silhouette rimmed in ember, inside a ward of three ember
    strokes wheeling round her, each swelling to a white-hot cinder at its head, the space within lit. (Its first
    paint, a dome of ash hexagonal plates, read as a shell at 64 px; skill-icons-02 of 2026-11-19.)

The Vanguard's two glow the Forge Burst's blue and are rimmed in its blue; Ilya's two glow and are rimmed in the
Crucible Vent's ember and crimson. Deterministic: the same script paints the same bytes (the inpainted field, minutes to
compute, is kept under TestResults/skill-icons). A .meta is written only when a PNG has none: it copies
UI_Icon_CrucibleVent_v1.png.meta (Sprite, bilinear, no mipmaps, 256 max and ASTC 4x4 on Android) with a fresh random
GUID checked unique against every .meta under Assets, so a repaint never changes a GUID.

Review: Tools/ArtReview/review_skill_icons.py lays all six on the bezel as the phone draws them.
"""
import argparse
import math
import os
import re
import sys
import uuid

import cv2
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import build_ability_icons as ability  # noqa: E402
import build_upgrade_icons as lib  # noqa: E402
from build_upgrade_icons import (CX, CY, GOLD, N, Glyph, Ramp, T, blade, colours, edge_of, ellipse,  # noqa: E402
                                 mask_of, part, polygon, ring, skin_dome, skin_pillow)

ROOT = lib.ROOT
UI = os.path.join(ROOT, "Assets", "_Project", "Art", "UI")
SIZE = ability.SIZE  # 1254
TEMPLATE_META = os.path.join(UI, "UI_Icon_CrucibleVent_v1.png.meta")

# ----------------------------------------------------------------------------------------------------------------------
# Ramps, dark to light. PLATE is the Vanguard's blue-lacquered steel (its painted masters' armour); the gold is the
# library's, which is already the painted icons' brass. ASH is the Arcanist's robe and the cinder's grey.

PLATE = Ramp("#081650", "#1236A0", "#1E5ADA", "#3282FA", "#5AA8FF", "#9CD4FF", "#E6F6FF")
BRASS = GOLD
PALE = Ramp("#0A2A7A", "#1E5AC8", "#4C96F4", "#8CC8FF", "#C8E8FF", "#F2FAFF", "#FFFFFF")
ASH = Ramp("#1E171C", "#3A3036", "#5E5258", "#8A7E80", "#B4AAA6", "#DAD0C8", "#FAF4EA")
FLAME = Ramp("#5A0C10", "#A41E14", "#E2461A", "#FF7A24", "#FFAE40", "#FFDC80", "#FFF8DC")


class Family:
    def __init__(self, rim, dark, glow, halo):
        self.rim = np.array(rim, np.float32) / 255.0  # the inner outline band
        self.dark = np.array(dark, np.float32) / 255.0  # the outer outline band
        self.glow = np.array(glow, np.float32)  # added to the field, in 0..255
        self.halo = np.array(halo, np.float32)


# Sampled off the shipped icons: the Forge Burst's bright blue rim and deep navy line; the Crucible Vent's crimson rim
# and near-black red line (build_ability_icons.RIM, OUTLINE), each family's glow its own.
VANGUARD = Family(rim=(14, 78, 248), dark=(2, 12, 92), glow=(40, 120, 255), halo=(30, 90, 230))
ILYA = Family(rim=ability.RIM, dark=ability.OUTLINE, glow=tuple(ability.GLOW), halo=(230, 100, 40))


def rotated(points, angle, about):
    a = math.radians(angle)
    ca, sa = math.cos(a), math.sin(a)
    ox, oy = about
    return [(ox + (x - ox) * ca - (y - oy) * sa, oy + (x - ox) * sa + (y - oy) * ca) for x, y in points]


def mirror(half, cx=48.0):
    """A symmetric outline from its right half (top to bottom): the left half mirrored back up."""
    return list(half) + [(2 * cx - x, y) for x, y in reversed(half)]


def flame(g, base, height, half, lean=0.0, ramp=FLAME, emit=1.0, core=True):
    """A tongue of flame from its base upward: a tapered blade, its core a lighter, smaller tongue."""
    bx, by = base
    tip = (bx + lean, by - height)
    outer = blade((bx, by), tip, [half, half * 0.92, half * 0.62, half * 0.30], tip=0.45)
    m = part(g, outer, colours(ramp, 3), ramp, inline=2, emit=emit)
    if core and height > 7:
        inner = blade((bx + lean * 0.1, by - 0.5), (bx + lean * 0.62, by - height * 0.62),
                      [half * 0.55, half * 0.45, half * 0.2], tip=0.5)
        im = mask_of(inner) & m & ~edge_of(m)
        g.paint(im, ramp[5], separate=False, emit=emit)
        hot = mask_of(blade((bx, by - 0.5), (bx + lean * 0.3, by - height * 0.3), [half * 0.3, half * 0.2], tip=0.6))
        g.paint(hot & im, ramp[6], separate=False, emit=emit)
    return m


def sparkle(g, x, y, arm, colour, emit=1.4, core=lib.WHITE, diag=0):
    g.sparkle(x, y, arm, colour, emit=emit, core=core)
    for k in range(1, diag + 1):
        for dx, dy in ((k, k), (-k, k), (k, -k), (-k, -k)):
            g.dot(x + dx, y + dy, colour, emit)


# ----------------------------------------------------------------------------------------------------------------------
# The four glyphs, in design cells (96 across, y down). Each draws at its own size; a transform scales it to sit in the
# icon as the shipped glyphs do.


def iron_resolve(g, t=T(k=0.84, dy=1.0)):
    """The Vanguard's breastplate, tempered."""
    P = t.p
    # The plate: broad at the chest, narrowing to the waist.
    half = [(48, 18), (57, 18), (64, 21), (69, 27), (70, 36), (68, 46), (65, 55), (63, 62)]
    plate_pts = [P(x, y) for x, y in mirror(half)]
    plate = mask_of(polygon(plate_pts))
    idx = np.full((N, N), 3.0, np.float32)
    mid_x = P(48, 0)[0]
    idx = np.where(CX < mid_x, 4.0, 2.4)  # the lit half and the far half either side of the ridge
    # Lower down the plate turns under: a tone darker below the chest.
    idx = idx - np.clip((CY - P(0, 44)[1]) / t.w(18.0), 0, 1) * 0.9
    # A broad sheen down the lit half, and the bright ridge.
    sheen = (CX > P(36, 0)[0]) & (CX < P(41.5, 0)[0]) & (CY < P(0, 50)[1])
    idx = np.where(sheen, 5.6, idx)
    idx = np.where(np.abs(CX + 0.5 - mid_x - 0.5) < 0.6, 5.0, idx)
    cols = colours(PLATE, np.round(idx).astype(int))
    pil = lib.pillow_idx(plate, 3, 1.6, hi=5, lo=1)
    rimmask = edge_of(plate, 2) & ~edge_of(plate, 1)
    cols[rimmask] = colours(PLATE, np.round(pil).astype(int))[rimmask]
    part(g, plate, cols, PLATE, inline=1)

    # The brass chevron across the chest, the Vanguard's mark.
    for dx in (0.0, 1.3):
        chev = lib.union(blade(P(30 + dx * 0, 33 + dx), P(48, 43 + dx), [t.w(1.6)]),
                         blade(P(66, 33 + dx), P(48, 43 + dx), [t.w(1.6)]))
        cm = mask_of(chev) & plate & ~edge_of(plate)
        g.paint(cm, colours(BRASS, np.where(CX < mid_x, 5, 3)), separate=False)

    # The belt and its buckle.
    belt = mask_of(polygon([P(32, 59), P(64, 59), P(64.5, 65), P(31.5, 65)]))
    part(g, belt, skin_pillow(belt, BRASS, 3, 1.2, hi=5, lo=2), BRASS, inline=1)
    buckle = mask_of(polygon([P(44, 58), P(52, 58), P(52, 66), P(44, 66)]))
    part(g, buckle, skin_pillow(buckle, PLATE, 4, 1.0, hi=6, lo=2), PLATE, inline=1)

    # Two lames below, each a little wider, brass along the lower one's hem.
    for top, bot, w0, w1 in ((65, 72, 33, 35), (72, 80, 35, 37.5)):
        pts = [P(48 - w0 / 2 * 1.0 - 0.0, top), P(48 + w0 / 2, top), P(48 + w1 / 2, bot - 1.5), P(48, bot),
               P(48 - w1 / 2, bot - 1.5)]
        pts = [P(48 - w0 / 2, top)] + pts[1:]
        lm = mask_of(polygon(pts))
        part(g, lm, skin_pillow(lm, PLATE, 3, 1.4, hi=5, lo=1, shade=0.8), PLATE, inline=1)
    hem = edge_of(lm, 2) & (CY > P(0, 77)[1]) & lm
    g.paint(hem, colours(BRASS, 4), separate=False)

    # The gorget: a brass collar round a dark neck opening.
    gorget = mask_of(polygon([P(35, 14), P(61, 14), P(65, 20), P(58, 24.5), P(38, 24.5), P(31, 20)]))
    part(g, gorget, skin_pillow(gorget, BRASS, 4, 1.4, hi=5, lo=2), BRASS, inline=1)
    neck = mask_of(ellipse(*P(48, 16.2), t.w(10.0), t.w(3.6)))
    g.paint(neck & gorget, np.array([6, 10, 30], np.float32) / 255.0, separate=False)
    g.paint(edge_of(neck) & gorget & (CY > P(0, 16.2)[1]), BRASS[1], separate=False)

    # The pauldrons: a steel dome on each shoulder, brass along its lower edge, a lame under it.
    for side in (-1, 1):
        cx = 48 + side * 24.5
        for k, (dy, rw, rh) in enumerate(((44.5, 8.5, 5.0), (39.5, 10.5, 6.0))):
            lame = ellipse(*P(cx + side * (1.0 + k * 0.5), dy), t.w(rw), t.w(rh))
            lm = mask_of(lame)
            part(g, lm, skin_dome(lame, PLATE, 3, 2.0, glint=False), PLATE, inline=1)
            _, e = lame(CX, CY)
            hem = lm & (e["ny"] > 0.55) & ~edge_of(lm)
            g.paint(hem, BRASS[4], separate=False)
        dome = ellipse(*P(cx, 30.5), t.w(12.5), t.w(9.8))
        dm = mask_of(dome)
        cols = skin_dome(dome, PLATE, 3, 2.8)
        part(g, dm, cols, PLATE, inline=1)
        _, e = dome(CX, CY)
        rim = dm & (e["r"] > 0.70) & (e["ny"] > 0.18)
        g.paint(rim & ~edge_of(dm), colours(BRASS, np.where(e["nx"] * side < 0, 5, 3)), separate=False)
        rv = ellipse(*P(cx - side * 2.0, 26.0), t.w(1.9))
        part(g, rv, skin_dome(rv, BRASS, 4, 1.6, glint=False), BRASS, inline=1)

    # Rivets round the plate.
    for x, y in ((33.5, 47), (62.5, 47), (36, 55), (60, 55)):
        rv = ellipse(*P(x, y), t.w(1.8))
        part(g, rv, skin_dome(rv, BRASS, 4, 1.6, glint=False), BRASS, inline=1)

    # Tempered: a white glint on the lit shoulder and the chest.
    sparkle(g, *P(20, 22), 3, PALE[5], emit=1.6)
    sparkle(g, *P(38, 30), 2, PALE[6], emit=1.6)
    lib.radiance(g, VANGUARD.halo / 255.0, cx=48, cy=48, rays=12, strength=0.16, halo=0.10, r1=46.0, halo_r=26.0)


def shield_rush(g):
    """A brass heater shield charging right, streaks behind and the strike's flare before it."""
    centre = (54.0, 49.0)
    tilt = 13.0

    # The speed streaks first, behind the shield: tapered from the shield's edge back to a point.
    for y, x0, x1, w in ((36.5, 40, 13, 2.5), (49.5, 38, 6, 3.0), (62.5, 41, 15, 2.5)):
        st = blade((x0, y), (x1, y), [w, w * 0.85, w * 0.5, 0.2])
        m = mask_of(st)
        _, e = st(CX, CY)
        tone = np.where(e["t"] < 0.35, 5, np.where(e["t"] < 0.7, 4, 3))
        tone = np.where(e["s"] < -0.2, tone + 1, tone)
        part(g, m, colours(PALE, tone), PALE, inline=2, emit=0.9)

    def S(pts, k=1.0):
        return rotated([(centre[0] + x * k, centre[1] + y * k) for x, y in pts], tilt, centre)

    outline_pts = [(-22, -27), (0, -31), (22, -27), (22, 0), (16, 16), (0, 31), (-16, 16), (-22, 0)]
    sh = mask_of(polygon(S(outline_pts)))
    # The steel rim.
    part(g, sh, skin_pillow(sh, PLATE, 4, 2.0, hi=5, lo=2), PLATE, inline=1)
    face = ndimage.binary_erosion(sh, iterations=4)
    cols = skin_pillow(face, BRASS, 4, 1.8, hi=5, lo=2, shade=1.4)
    # A broad sheen on the lit side.
    fx, fy = rotated([(centre[0] - 10, centre[1])], tilt, centre)[0]
    a = math.radians(tilt)
    along_x = (CX - fx) * math.cos(a) + (CY - fy) * math.sin(a)
    across = -(CX - fx) * math.sin(a) + (CY - fy) * math.cos(a)
    sheen = face & (np.abs(along_x) < 2.6) & (across < 14)
    cols[sheen & ~edge_of(face, 2)] = BRASS[6]
    g.paint(face, cols, separate=False)
    g.paint(edge_of(face), BRASS[1], separate=False)
    # The Vanguard's chevron in steel across the face, and the steel boss above it.
    for d in (0.0,):
        chev = lib.union(blade(S([(-15, 2 + d)])[0], S([(0, 14 + d)])[0], [2.4]),
                         blade(S([(15, 2 + d)])[0], S([(0, 14 + d)])[0], [2.4]))
        cm = mask_of(chev) & face & ~edge_of(face)
        g.paint(ndimage.binary_dilation(cm) & face & ~edge_of(face), PLATE[1], separate=False)
        g.paint(cm, colours(PLATE, np.where(CX < S([(0, 0)])[0][0], 5, 4)), separate=False)
    boss = ellipse(*S([(0, -10)])[0], 6.8)
    part(g, boss, skin_dome(boss, PLATE, 4, 2.4), PLATE, inline=1)
    for x, y in ((-15, -21), (15, -21), (-15, -2), (15, -2)):
        rv = ellipse(*S([(x, y)])[0], 1.9)
        part(g, rv, skin_dome(rv, PLATE, 4, 1.6, glint=False), PLATE, inline=1)

    # The strike's flare at the leading edge: a white four-point star.
    fx, fy = S([(24, -16)])[0]
    rr = []
    for i in range(4):
        rr += [9.0 if i % 2 == 0 else 6.5, 1.7, 4.0, 1.7]
    st = lib.star(fx + 1, fy, rr, -90.0)
    m = part(g, polygon(st), colours(PALE, 5), PALE, inline=3, emit=1.4)
    g.paint(mask_of(ellipse(fx + 1, fy, 2.2)) & m, PALE[6], separate=False, emit=1.6)
    lib.radiance(g, VANGUARD.halo / 255.0, cx=54, cy=48, rays=10, strength=0.10, halo=0.10, r1=44.0, halo_r=24.0)


def tongue_pts(bx, by, h, w, lean=0.0, wiggle=0.0, n=16):
    """A curling tongue of flame from a rounded base at (bx, by) up to its tip: widest a quarter of the way up, its
    centre line bending by `lean` toward the tip and swaying by `wiggle` in between."""
    right, left = [], []
    for i in range(n + 1):
        t = i / n
        y = by - h * t
        xc = bx + lean * t * t + wiggle * math.sin(math.pi * t) * t
        ww = w * (1 - t) ** 0.8 * (0.80 + 0.20 * min(1.0, t / 0.2))
        right.append((xc + ww, y))
        left.append((xc - ww, y))
    base = [(bx + w * 0.78 * math.cos(math.radians(a)), by + w * 0.55 * math.sin(math.radians(a))) for a in
            range(10, 180, 20)]
    return right + left[::-1] + base


def curl_flame(g, bx, by, h, w, lean=0.0, wiggle=0.0, emit=1.2, licks=()):
    """A flame in three tones: the ember body (with any side licks, each (dx, dy, h, w, lean) from its base), a gold
    heart and a white-hot core low in it."""
    body = mask_of(polygon(tongue_pts(bx, by, h, w, lean, wiggle)))
    for dx, dy, lh, lw, ll in licks:
        body |= mask_of(polygon(tongue_pts(bx + dx, by + dy, lh, lw, ll, 0.0)))
    m = part(g, body, colours(FLAME, 3), FLAME, inline=2, emit=emit)
    lit = (CX - bx) * lib.LXY[0] > 0.6
    g.paint(m & lit & ~edge_of(m) & ~ndimage.binary_erosion(m, iterations=2), FLAME[4], separate=False, emit=emit)
    heart = mask_of(polygon(tongue_pts(bx + lean * 0.06, by - h * 0.02, h * 0.66, w * 0.56, lean * 0.5,
                                       wiggle * 0.6))) & m & ~edge_of(m)
    g.paint(heart, FLAME[5], separate=False, emit=emit)
    core = mask_of(polygon(tongue_pts(bx, by - h * 0.03, h * 0.34, w * 0.30, lean * 0.15))) & heart
    g.paint(core, FLAME[6], separate=False, emit=emit)
    return m


def flame_sigil(g):
    """A furnace glyph on the floor: an ember ring notched with white-hot runes round an inner ring, the floor inside
    lit, and a flame rising out of its centre."""
    cx, cy = 48.0, 64.0
    rx, sq = 37.0, 0.40

    band = ring(cx, cy, rx - 8.0, rx, squash=sq)
    inner = ring(cx, cy, rx - 15.0, rx - 12.0, squash=sq)
    _, e = band(CX, CY)
    ang = np.degrees(e["ang"])
    facing = -(np.cos(e["ang"]) * lib.LXY[0] + np.sin(e["ang"]) * lib.LXY[1])
    tone = np.round(2.4 + 1.0 * facing).astype(int)
    rune = (np.abs(((ang + 15) % 30) - 15) < 3.4) & (e["r"] > 0.2) & (e["r"] < 0.8)
    tone = np.where(rune, 6, tone)
    bm = mask_of(band)
    im = mask_of(inner)
    back = CY < cy

    # The floor inside the ring glows, drawn as light on the field, not as cells.
    xs = (np.arange(N * lib.UP, dtype=np.float32) + 0.5) / lib.UP
    X, Y = np.meshgrid(xs, xs)
    d = np.hypot(X - cx, (Y - cy) / sq) / (rx - 8.0)
    g.field += np.array([1.0, 0.40, 0.10], np.float32) * (np.clip(1 - d, 0, 1) ** 0.7 * 0.34)[..., None]

    # The far halves first, then the flame, then the near halves over its foot.
    part(g, im & back, colours(FLAME, 4), FLAME, inline=2, emit=0.9)
    part(g, bm & back, colours(FLAME, tone), FLAME, inline=1, emit=0.9)
    curl_flame(g, cx + 0.5, cy - 1.0, 45.0, 11.5, lean=4.0, wiggle=-6.0, emit=1.3,
               licks=((5.0, -8.0, 22.0, 4.8, 6.0), (-5.0, -6.0, 17.0, 4.2, -5.0)))
    curl_flame(g, cx - 13.0, cy - 0.5, 17.0, 4.6, lean=-3.0, wiggle=1.0, emit=1.1)
    curl_flame(g, cx + 14.5, cy - 0.5, 15.0, 4.2, lean=2.5, wiggle=-1.0, emit=1.1)
    part(g, im & ~back, colours(FLAME, 4), FLAME, inline=2, emit=0.9)
    part(g, bm & ~back, colours(FLAME, tone), FLAME, inline=1, emit=0.9)
    sparkle(g, cx - 21, 33, 1, FLAME[5], emit=1.2, core=FLAME[6])
    sparkle(g, cx + 22, 38, 1, FLAME[5], emit=1.2, core=FLAME[6])


def comet_arc(cx, cy, head, span, r_head, r_tail, w_head, w_tail):
    """One stroke of a whirl round (cx, cy): an arc running clockwise (y down) to its head at `head` degrees, `span`
    degrees long, its radius easing from r_tail to r_head and its half width swelling from w_tail to a round head of
    w_head. Extra: t (0 at the tail, 1 at the head) and s (-1 inside to 1 outside, across the stroke)."""
    hx = cx + r_head * math.cos(math.radians(head))
    hy = cy + r_head * math.sin(math.radians(head))

    def f(x, y):
        dx, dy = x - cx, y - cy
        r = np.hypot(dx, dy)
        back = (head - np.degrees(np.arctan2(dy, dx))) % 360.0
        on = back <= span
        t = np.clip(1.0 - back / span, 0, 1)
        rc = r_tail + (r_head - r_tail) * t
        w = w_tail + (w_head - w_tail) * t ** 1.4
        s = (r - rc) / w
        cap = np.hypot(x - hx, y - hy) <= w_head
        inside = (on & (np.abs(s) <= 1)) | cap
        return inside, dict(t=np.where(on, t, 1.0), s=np.where(on, s, 0.0))

    return f, (hx, hy)


def cinder_ward(g):
    """Ilya hooded in her ash robe, warded by a whirl of three ember strokes, each swelling to a white-hot cinder at its
    head, round a space lit from within."""
    cx, cy = 48.0, 49.0
    r_out = 31.0

    # The warded space glows, brightest toward the ward's edge (a bubble's rim), drawn as light on the field.
    xs = (np.arange(N * lib.UP, dtype=np.float32) + 0.5) / lib.UP
    X, Y = np.meshgrid(xs, xs)
    d = np.hypot(X - cx, Y - cy) / (r_out - 6.0)
    fade = np.clip((1.04 - d) / 0.16, 0, 1)
    g.field += np.array([1.0, 0.42, 0.12], np.float32) * ((0.27 + 0.30 * np.clip(d, 0, 1) ** 2.2) * fade)[..., None]

    # The ward: three comet strokes wheeling clockwise, a short gap between each tail and the head before it.
    heads = []
    for head in (-58.0, 62.0, 182.0):
        arc, hp = comet_arc(cx, cy, head, 104.0, r_out - 2.6, r_out - 6.2, 3.9, 1.2)
        m = mask_of(arc)
        _, e = arc(CX, CY)
        t, s = e["t"], e["s"]
        tone = 3 + (t > 0.30) + (t > 0.66)
        tone = np.where((np.abs(s) < 0.45) & (t > 0.15), tone + 1, tone)
        tone = np.where(s > 0.62, tone - 1, tone)
        part(g, m, colours(FLAME, tone), FLAME, inline=2, emit=1.25)
        heads.append(hp)

    # The cinder at each stroke's head: a white-hot diamond.
    for hx, hy in heads:
        r = 4.0
        pts = [(hx, hy - r * 1.25), (hx + r, hy), (hx, hy + r * 1.25), (hx - r, hy)]
        m = part(g, polygon(pts), colours(FLAME, 5), FLAME, inline=3, separate=False, emit=1.5)
        g.paint(mask_of(ellipse(hx, hy, 1.6)) & m, FLAME[6], separate=False, emit=1.6)

    # Ilya inside it: a hooded ash robe in silhouette against the lit space, its edge caught in the ward's ember light.
    # Drawn at its own size, then drawn in a little (k) so the lit space shows all round it.
    k = 0.88

    def P(x, y):
        return cx + (x - cx) * k, cy - 0.6 + (y - cy) * k

    hood = lib.union(ellipse(*P(cx, 41.4), 6.2 * k, 6.8 * k),
                     polygon([P(42.0, 40.6), P(54.0, 40.6), P(50.8, 32.0)]))
    robe = lib.union(ellipse(*P(cx, 51.0), 10.4 * k, 5.0 * k),
                     polygon([P(x, y) for x, y in ((37.8, 51.4), (58.2, 51.4), (60.6, 66.0), (54.0, 67.6),
                                                    (48.0, 66.6), (42.0, 67.6), (35.4, 66.0))]))
    body = mask_of(lib.union(hood, robe))
    tone = np.where(CX < cx - 3.0, 2, 1)
    cols = colours(ASH, tone)
    lit_edge = edge_of(body) & ((CX < cx - 1.0) & (CY < P(0, 58.0)[1]))
    cols[edge_of(body)] = FLAME[3]
    cols[lit_edge] = FLAME[4]
    g.paint(body, cols, separate=True, emit=0.0)
    g.emit[edge_of(body)] = FLAME[3] * 0.5
    # The face, shadowed in the hood, and the fold down the front of the robe.
    face = mask_of(ellipse(*P(cx + 0.6, 42.4), 3.3, 3.8)) & body & ~edge_of(body)
    g.paint(face, np.array([20, 8, 12], np.float32) / 255.0, separate=False)
    g.dot(*P(cx - 0.9, 42.5), FLAME[5], 1.2)
    g.dot(*P(cx + 2.1, 42.5), FLAME[5], 1.2)
    fold = (np.abs(CX + 0.5 - cx - 0.5) < 0.6) & (CY > P(0, 54.0)[1]) & body & ~edge_of(body, 2)
    g.paint(fold, ASH[0], separate=False)

    # Loose cinders flung off the whirl.
    sparkle(g, cx + 30, 24, 1, FLAME[5], emit=1.2, core=FLAME[6])
    sparkle(g, cx - 31, 72, 1, FLAME[5], emit=1.2, core=FLAME[6])
    g.dot(cx + 34, 79, FLAME[4], 1.0)
    g.dot(cx - 35, 19, FLAME[4], 1.0)


GLYPHS = {
    "IronResolve": (iron_resolve, VANGUARD),
    "ShieldRush": (shield_rush, VANGUARD),
    "FlameSigil": (flame_sigil, ILYA),
    "CinderWard": (cinder_ward, ILYA),
}


# ----------------------------------------------------------------------------------------------------------------------
# Composition, as build_ability_icons.build: the cells as hard pixels over the shipped field, the family's two-band
# outline, the glyph's light spread wide and near into the field, softened by a sixth of a cell.


def paint(name):
    draw, fam = GLYPHS[name]
    g = Glyph()
    draw(g)
    g.centre()
    # The shipped icons' two-band outline: the family's rim colour, then its dark line.
    ring1 = ndimage.binary_dilation(g.solid) & ~g.solid
    g.rgb[ring1] = fam.rim
    g.emit[ring1] = fam.rim * 0.25
    g.solid |= ring1
    ring2 = ndimage.binary_dilation(g.solid) & ~g.solid
    g.rgb[ring2] = fam.dark
    g.emit[ring2] = 0
    g.solid |= ring2
    return g, fam


_FIELD = []


def shipped_field():
    """The shipped icons' navy field. Inpainting it takes minutes, so it is kept under TestResults (git-ignored), keyed
    by the Forge Burst icon's bytes."""
    if not _FIELD:
        import hashlib
        with open(ability.FORGE, "rb") as fh:
            key = hashlib.sha1(fh.read()).hexdigest()[:16]
        cache = os.path.join(ROOT, "TestResults", "skill-icons", f"field-{key}.npy")
        if os.path.exists(cache):
            _FIELD.append(np.load(cache))
        else:
            _FIELD.append(ability.field().astype(np.float32))
            os.makedirs(os.path.dirname(cache), exist_ok=True)
            np.save(cache, _FIELD[0])
    return _FIELD[0]


def compose(g, fam):
    base = shipped_field()
    cells = (np.clip(g.rgb, 0, 1) * 255).astype(np.uint8)
    rgb = np.asarray(Image.fromarray(cells, "RGB").resize((SIZE, SIZE), Image.NEAREST)).astype(np.float32)
    alpha = np.asarray(Image.fromarray((g.solid * 255).astype(np.uint8), "L").resize((SIZE, SIZE), Image.NEAREST))
    alpha = alpha.astype(np.float32)[..., None] / 255.0
    emit = np.asarray(Image.fromarray((np.clip(g.emit.max(axis=2), 0, 1) * 255).astype(np.uint8), "L")
                      .resize((SIZE, SIZE), Image.NEAREST)).astype(np.float32) / 255.0
    # The shipped icons glow from their bright cells; these also from what they emit (flames, sparks, seams).
    bright = np.clip((rgb.max(axis=2) - 170.0) / 70.0, 0, 1) * alpha[..., 0] * 0.55
    source = np.maximum(bright, emit)
    wide = cv2.GaussianBlur(source, (0, 0), 46)[..., None]
    near = cv2.GaussianBlur(source, (0, 0), 14)[..., None]
    sil = cv2.GaussianBlur(alpha[..., 0], (0, 0), 40)[..., None]
    field_light = cv2.resize(g.field, (SIZE, SIZE), interpolation=cv2.INTER_LINEAR) * 255.0
    lit = base + fam.glow * (wide * 1.25 + near * 0.6) + fam.halo * sil * 0.10 + field_light
    lit = lit.clip(0, 255)
    out = lit * (1 - alpha) + rgb * alpha
    soft = cv2.GaussianBlur(out, (0, 0), 1.2)
    out = soft * alpha + out * (1 - alpha)
    return Image.fromarray(out.clip(0, 255).astype(np.uint8), "RGB")


def cells_preview(g, scale=6):
    cells = (np.clip(g.rgb, 0, 1) * 255).astype(np.uint8)
    img = Image.fromarray(cells, "RGB")
    navy = Image.new("RGB", img.size, (12, 17, 35))
    mask = Image.fromarray((g.solid * 255).astype(np.uint8), "L")
    navy.paste(img, (0, 0), mask)
    return navy.resize((N * scale, N * scale), Image.NEAREST)


# ----------------------------------------------------------------------------------------------------------------------
# .meta files.


def taken_guids():
    out = set()
    for folder, _, files in os.walk(os.path.join(ROOT, "Assets")):
        for f in files:
            if f.endswith(".meta"):
                with open(os.path.join(folder, f), encoding="utf-8", errors="ignore") as fh:
                    head = fh.read(200)
                m = re.search(r"^guid: ([0-9a-f]{32})$", head, re.M)
                if m:
                    out.add(m.group(1))
    return out


def write_meta(png_path, taken):
    meta_path = png_path + ".meta"
    if os.path.exists(meta_path):
        m = re.search(r"^guid: ([0-9a-f]{32})$", open(meta_path, encoding="utf-8").read(), re.M)
        return m.group(1), False
    template = open(TEMPLATE_META, encoding="utf-8").read()
    stem = os.path.splitext(os.path.basename(png_path))[0]
    while True:
        guid = uuid.uuid4().hex
        if guid not in taken:
            break
    taken.add(guid)
    sprite_id = uuid.uuid4().hex
    text = template.replace("4fde397b99924d3789d44ee95116c9fc", guid)
    text = text.replace("UI_Icon_CrucibleVent_v1_0", stem + "_0")
    text = text.replace("f443ac49b13f4a089474e2deda4151f0", sprite_id)
    assert guid in text and stem + "_0" in text and "CrucibleVent" not in text
    with open(meta_path, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(text)
    return guid, True


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", help="write the PNGs here instead of Assets (no .meta)")
    ap.add_argument("--cells", help="also write each glyph's cells, magnified, here")
    ap.add_argument("--only", nargs="*", default=None)
    args = ap.parse_args()
    names = args.only or list(GLYPHS)
    taken = None
    for name in names:
        g, fam = paint(name)
        image = compose(g, fam)
        folder = args.out or UI
        os.makedirs(folder, exist_ok=True)
        path = os.path.join(folder, f"UI_Icon_{name}_v1.png")
        image.save(path, optimize=True)
        line = path
        if not args.out:
            if taken is None:
                taken = taken_guids()
            guid, fresh = write_meta(path, taken)
            line += f"  guid {guid}{' (new)' if fresh else ''}"
        if args.cells:
            os.makedirs(args.cells, exist_ok=True)
            cells_preview(g).save(os.path.join(args.cells, f"cells-{name}.png"))
        print(line)


if __name__ == "__main__":
    main()
