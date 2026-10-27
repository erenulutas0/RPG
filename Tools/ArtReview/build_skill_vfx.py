"""Painted skill effects for the Cinder Arcanist and the Vanguard (docs/45).

The skills' effects were first drawn from code as crisp pixel art (BurnArt, EffectArt). Beside the painted actors (128
texels a unit, bilinear) that style clashed, and a small pixel flame on the Grunt's own orange core or on a fiery Cinder
Mite was hard to read. This paints them again in the actors' smooth style:

  - Burn: the flame a burning enemy carries at 1-5 stacks, one loop each. Tongues rise from the shoulders and flanks so
    they frame the body against the floor instead of covering its chest; layered crimson, ember, warm yellow and a
    white-hot heart, a thin soot edge so an orange flame still parts from orange lava and fiery enemies, ember
    highlights, and a faint warm glow over the body as its ember tint.
  - Ember Memory: the ember that hops from a fallen enemy to each neighbour it lights.
  - Crucible Vent: the eruption column at each vented enemy, the embers it throws up, the ember cast ring, and the
    eruption ring each eruption draws on the floor at its splash's radius: a burning band with flames leaping off its
    far side, tearing into embers and leaving a scorched ring.
  - Forge Burst: the forge-hammer shockwave ring (white edge, steel-blue band, heat ripples, soot rim), its steel
    sparks and the flash at the hero.

Each family is packed onto one sheet, so a crowd's flames or a Vent across a crowd batch (the Vent's eruption ring rides
a sheet of its own, VFX_Skill_VentSplash, which the art set holds as optional), and
Assets/_Project/Art/Effects/Skills/SkillEffectArtSet.asset is written with every strip's cells, pivot and density
(Cryptforge.Art.SkillEffectArtSet). Frames are painted supersampled, then box-filtered down, in sRGB as the project
blends (gamma colour space), and exported straight-alpha with colour bled into the clear texels so bilinear filtering
and ASTC never fringe. Deterministic: the same script paints the same sheets. Existing .meta GUIDs are kept, so a
repaint never breaks a reference.

Usage:
    python Tools/ArtReview/build_skill_vfx.py               # paint the sheets, metas and art set
    python Tools/ArtReview/build_skill_vfx.py --frames DIR  # also save every frame as a PNG, for review
"""
import argparse
import math
import os
import re
import uuid

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "Effects", "Skills")
SCRIPT_META = os.path.join(ROOT, "Assets", "_Project", "Scripts", "Art", "Unity", "SkillEffectArtSet.cs.meta")
ASSET_PATH = os.path.join(OUT_DIR, "SkillEffectArtSet.asset")

SS = 4  # supersampling per axis
GUTTER = 2  # clear texels between cells, so bilinear sampling never reaches a neighbour
MAX_STACKS = 5


def hex_rgb(value):
    value = value.lstrip("#")
    return np.array([int(value[i:i + 2], 16) / 255.0 for i in (0, 2, 4)], np.float32)


# The Arcanist's palette (docs/45) and the Vanguard's steel.
EMBER = hex_rgb("#E77232")
CRIMSON = hex_rgb("#B73E48")
CRIMSON_DEEP = hex_rgb("#7E2035")
EMBER_DEEP = hex_rgb("#D0502A")
YELLOW = hex_rgb("#FFC861")
YELLOW_HOT = hex_rgb("#FFD45E")
WHITE_HOT = hex_rgb("#FFF3C6")
SOOT = hex_rgb("#26100F")
STEEL_DEEP = hex_rgb("#1F3C78")
STEEL = hex_rgb("#3E7BD6")
STEEL_LIGHT = hex_rgb("#8FC2FF")
STEEL_PALE = hex_rgb("#D8ECFF")
WHITE = hex_rgb("#FFFFFF")
STEEL_SOOT = hex_rgb("#10141F")


# ----------------------------------------------------------------------------------------------------------------------
# Painting primitives. A layer is premultiplied RGBA float32, image order (row 0 at the top).


class Frame:
    """One frame painted in world units: the rectangle [x0, x1] x [y0, y1] around the strip's pivot at (0, 0), at tpu
    texels a unit, supersampled SS times per axis."""

    def __init__(self, x0, y0, x1, y1, tpu):
        self.w = int(round((x1 - x0) * tpu))
        self.h = int(round((y1 - y0) * tpu))
        self.x0, self.y0, self.tpu = x0, y1 - self.h / tpu, tpu
        self.x1, self.y1 = x0 + self.w / tpu, y1
        step = 1.0 / (tpu * SS)
        xs = self.x0 + (np.arange(self.w * SS) + 0.5) * step
        ys = self.y1 - (np.arange(self.h * SS) + 0.5) * step
        self.X, self.Y = np.meshgrid(xs.astype(np.float32), ys.astype(np.float32))
        self.px = step  # a supersample in world units
        self.rgba = np.zeros((self.h * SS, self.w * SS, 4), np.float32)

    def paint(self, color, alpha):
        """Source-over: colour (3,) or (H, W, 3) at coverage alpha (H, W)."""
        a = np.clip(alpha, 0.0, 1.0)[..., None]
        self.rgba[..., :3] = color * a + self.rgba[..., :3] * (1.0 - a)
        self.rgba[..., 3:] = a + self.rgba[..., 3:] * (1.0 - a)

    def coverage(self):
        return self.rgba[..., 3]

    def resolve(self):
        """Box-filtered to the final texels, still premultiplied."""
        h, w = self.h, self.w
        return self.rgba.reshape(h, SS, w, SS, 4).mean(axis=(1, 3))


def blur(field, sigma_texels):
    """Gaussian blur of a supersampled field by a radius given in final texels."""
    if sigma_texels <= 0:
        return field
    return ndimage.gaussian_filter(field, sigma_texels * SS, mode="constant")


def grow(field, texels):
    """Grey dilation by a disc of the given radius in final texels: the field widened by an edge."""
    radius = max(1, int(round(texels * SS)))
    yy, xx = np.mgrid[-radius:radius + 1, -radius:radius + 1]
    disc = (xx * xx + yy * yy) <= radius * radius
    return ndimage.grey_dilation(field, footprint=disc)


def shrink(field, texels):
    radius = max(1, int(round(texels * SS)))
    yy, xx = np.mgrid[-radius:radius + 1, -radius:radius + 1]
    disc = (xx * xx + yy * yy) <= radius * radius
    return ndimage.grey_erosion(field, footprint=disc)


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def mix(a, b, t):
    t = np.asarray(t, np.float32)
    if t.ndim:
        t = t[..., None]
    return a * (1.0 - t) + b * t


class Noise:
    """Band-limited noise in [-1, 1], periodic over `period` world units on both axes, so a flame's noise can scroll up
    by exactly one period over a loop and the loop closes seamlessly."""

    def __init__(self, seed, period, feature, size=128):
        rng = np.random.default_rng(seed)
        white = rng.standard_normal((size, size))
        sigma = feature / period * size
        f = np.fft.fftfreq(size)
        k = np.sqrt(f[:, None] ** 2 + f[None, :] ** 2)
        tile = np.real(np.fft.ifft2(np.fft.fft2(white) * np.exp(-(k * sigma) ** 2 * 2 * np.pi ** 2)))
        self.tile = (tile / (np.abs(tile).max() + 1e-9)).astype(np.float32)
        self.size, self.period = size, period

    def __call__(self, x, y):
        scale = self.size / self.period
        coords = [(-y * scale) % self.size, (x * scale) % self.size]
        return ndimage.map_coordinates(self.tile, coords, order=1, mode="grid-wrap").astype(np.float32)


def disc(frame, cx, cy, radius, soft=0.0):
    """Coverage of a disc, anti-aliased, optionally soft-edged (soft in world units)."""
    d = np.sqrt((frame.X - cx) ** 2 + (frame.Y - cy) ** 2)
    edge = max(frame.px, soft)
    return np.clip((radius - d) / edge + 0.5, 0.0, 1.0)


# ----------------------------------------------------------------------------------------------------------------------
# Fire. A tongue is a teardrop on a centreline that leans and waves; a flame is a union of tongues painted in nested
# bands: the full shape in crimson, an inset in ember, a smaller one in warm yellow and a heart in white-hot.

# (width, height, base lift in half widths) of each band against the tongue's outline: the full shape in crimson, an
# inset in ember, a smaller one in warm yellow and a heart in white-hot, each standing a little higher than the last.
BANDS = ((1.0, 1.0, 0.0), (0.76, 0.88, 0.3), (0.44, 0.6, 0.75), (0.24, 0.36, 1.1))


def profile(t, sharp):
    """Half-width along a tongue from its base (0) to its tip (1): widest an eighth of the way up, then a long curved
    taper to a sharp tip."""
    q = 3.0
    f = (1.0 - t) ** sharp * (1.0 + q * t)
    tmax = (q - sharp) / (q * (sharp + 1.0))
    peak = (1.0 - tmax) ** sharp * (1.0 + q * tmax)
    return f / peak


def tongue_cover(frame, tg, band, phase, warp):
    """Anti-aliased coverage of one band of a tongue at loop angle phase. tg: x, y (base), h, w (half width), lean (tip
    offset), wave (amplitude), ph (phase offset), sharp (tip exponent), flick (height flicker), sway (tip sway)."""
    X, Y = frame.X, frame.Y
    kw, kh, lift = band
    ph = tg.get("ph", 0.0)
    h = tg["h"] * (1.0 + tg.get("flick", 0.08) * math.sin(phase + ph * 1.7))
    w = tg["w"]
    t_out = np.clip((Y - tg["y"]) / h, 0.0, 1.0)
    # The centreline leans toward its tip, sways over the loop and carries a wave that travels up it.
    lean = tg.get("lean", 0.0) + tg.get("sway", 0.03) * math.sin(phase + ph)
    cx = tg["x"] + lean * t_out ** 1.6
    cx = cx + tg.get("wave", 0.0) * np.sin(2 * np.pi * 0.9 * t_out - phase - ph) * t_out
    if warp is not None:
        cx = cx + warp * w * (0.1 + 1.1 * t_out)
    if tg.get("foot", 0.0) > 0:
        # A tongue rising out of a body is hottest at its base: every band reaches down into the fade, so the foot
        # melts into the body in ember and yellow rather than ending on a crimson rim.
        lift = lift * 0.3 - 0.6
    base = tg["y"] + lift * w * kw
    hh = h * kh - lift * w * kw
    t = (Y - base) / hh
    tc = np.clip(t, 0.0, 1.0)
    sharp = tg.get("sharp", 1.9)
    half = w * kw * profile(tc, sharp)
    cap = w * kw * 0.9
    capped = np.sqrt(np.clip(1.0 - ((Y - base) / cap) ** 2, 0.0, 1.0))
    half = np.where(t < 0, w * kw * profile(0.0, sharp) * capped, half)
    margin = half - np.abs(X - cx)
    cover = np.clip(margin / frame.px + 0.5, 0.0, 1.0)
    # Nothing past the tip or under the foot: a zero half-width must not leave a hairline down the frame.
    cover = np.where((t > 1.0) | (half <= 0.0), 0.0, cover)
    foot = tg.get("foot", 0.0)
    if foot > 0:
        # It rises out of what it stands on, fading in from its base rather than sitting on it as a cut-out drop.
        cover = cover * smoothstep(tg["y"] - foot * 0.35, tg["y"] + foot, Y)
    return cover.astype(np.float32)


def fork(tg, side, at, size, lean):
    """A smaller tongue branching off tg's side at a share of its height, so a large tongue licks in two tips."""
    return dict(x=tg["x"] + side * tg["w"] * 0.45 + tg.get("lean", 0) * at ** 1.6, y=tg["y"] + tg["h"] * at,
                h=tg["h"] * size, w=tg["w"] * (0.35 + 0.4 * size), lean=side * lean, wave=tg.get("wave", 0) * 0.6,
                ph=tg.get("ph", 0) + 1.9 * side, sharp=1.7, flick=tg.get("flick", 0.08) * 1.4,
                sway=tg.get("sway", 0.03) * 1.3, foot=tg.get("foot", 0.0))


def paint_fire(frame, tongues, phase, heat, noise, wisps=(), embers=(), glow=0.2, rim=1.2, body_glow=None,
               palette=None, opacity=1.0):
    """Paints a flame of tongues. heat (0-1) sets how far the hot bands reach: one Burn stack never shows white-hot.
    A tongue with a foot (in world units) fades in over it from its base (tongue_cover); opacity fades the whole
    flame, as a piece torn off a column burns out."""
    edge, outer, outer_deep, mid, mid_deep, inner, inner_hot, heart = palette or (
        SOOT, CRIMSON, CRIMSON_DEEP, EMBER, EMBER_DEEP, YELLOW, YELLOW_HOT, WHITE_HOT)
    scroll = phase / (2 * np.pi) * noise.period
    warp = noise(frame.X, frame.Y - scroll) * 0.6 + noise(frame.X * 2.7 + 0.31, frame.Y * 2.7 - 2.7 * scroll) * 0.25
    bands = []
    for b, band in enumerate(BANDS):
        if b == 3 and heat < 0.3:
            bands.append(None)
            continue
        cover = np.zeros_like(frame.X)
        for tg in tongues:
            kw, kh, lift = band
            if b >= 2:
                # Hotter flames carry larger inner bands.
                boost = 0.85 + 0.3 * heat
                kw, kh = min(0.9, kw * boost), min(0.95, kh * boost)
            cover = np.maximum(cover, tongue_cover(frame, tg, (kw, kh, lift), phase, warp))
        if b <= 1:
            for wisp in wisps:
                cover = np.maximum(cover, tongue_cover(frame, wisp, band, phase, warp))
        bands.append(cover)
    # Inner bands never paint past the band around them.
    for b in range(1, len(bands)):
        if bands[b] is not None:
            bands[b] = np.minimum(bands[b], shrink(bands[b - 1], 0.7))
    outline = bands[0]

    if body_glow is not None:
        cx, cy, rx, ry, strength = body_glow
        d = np.sqrt(((frame.X - cx) / rx) ** 2 + ((frame.Y - cy) / ry) ** 2)
        frame.paint(mid, strength * smoothstep(1.0, 0.0, d) ** 1.5 * opacity)
    if glow > 0:
        halo = blur(outline, 5.0)
        frame.paint(mid, np.clip(halo * glow * 2.5, 0.0, glow) * opacity)
    if rim > 0:
        frame.paint(edge, blur(grow(outline, rim), 0.45) * 0.82 * opacity)

    span = max(1e-6, frame.y1 - frame.y0)
    height = np.clip((frame.Y - frame.y0) / span, 0.0, 1.0)
    shade = noise(frame.X * 2.3 + 0.37, frame.Y * 2.3 - scroll * 2.0)
    frame.paint(mix(outer, outer_deep, np.clip(height * 0.8 + 0.15 * shade, 0.0, 1.0)), blur(bands[0], 0.3) * opacity)
    frame.paint(mix(mid, mid_deep, np.clip(0.7 * height + 0.2 * shade - 0.1, 0.0, 1.0)), blur(bands[1], 0.55) * opacity)
    frame.paint(mix(inner_hot, inner, np.clip(0.9 * height + 0.25 * shade, 0.0, 1.0)), blur(bands[2], 0.8) * opacity)
    if bands[3] is not None:
        frame.paint(heart, blur(bands[3], 1.0) * min(1.0, (heat - 0.25) * 2.2) * opacity)

    for ember in embers:
        paint_ember(frame, *ember)


def paint_ember(frame, x, y, radius, hot, stretch=1.8, angle=90.0, palette=None):
    """A glowing ember streaking along its flight (angle in degrees, stretch its length over its width): a faint warm
    halo, a thin soot edge for the lava, and a core cooling from white-hot to crimson as hot falls from 1 to 0."""
    outer, mid, inner, heart = palette or (CRIMSON, EMBER, YELLOW, WHITE_HOT)
    c, s = math.cos(math.radians(angle)), math.sin(math.radians(angle))
    u = ((frame.X - x) * c + (frame.Y - y) * s) / stretch
    v = -(frame.X - x) * s + (frame.Y - y) * c
    d = np.sqrt(u * u + v * v)

    def dot(r, soft):
        return np.clip((r - d) / max(frame.px, soft) + 0.5, 0.0, 1.0)

    frame.paint(mix(outer, mid, hot), dot(radius * 2.4, radius * 2.0) * 0.28 * (0.5 + 0.5 * hot))
    frame.paint(SOOT, dot(radius * 1.5, radius * 0.4) * 0.6)
    core = mix(mix(outer, mid, min(1.0, hot * 2.0)), mix(inner, heart, max(0.0, hot * 2.0 - 1.0)), min(1.0, hot * 1.5))
    frame.paint(core, dot(radius, radius * 0.3))


def loop_rise(i, phase, period=1.0):
    """Where on its rise (0 to 1) loop particle i is at loop angle phase: evenly staggered, wrapping each loop."""
    return ((phase / (2 * np.pi)) * period + i * 0.6180339887) % 1.0


# ----------------------------------------------------------------------------------------------------------------------
# Burn flames.

FLAME_FRAMES = 8
FLAME_TPU = 100.0
# A frame spans this much around the enemy's feet (the pivot), for a body the size of the painted Grunt; BurnFlames
# scales it to the body it burns on. Painted wide enough for the tallest flicker, then cropped per stack to what the
# loop paints.
FLAME_EXTENT = (-0.76, -0.08, 0.76, 1.86)


def flame_tongues(stacks):
    """The tongues of a flame at this many stacks, for a body the size of the painted Grunt (about a unit wide, its head
    0.9 over its feet at the origin): they rise out of its shoulders and flanks and lean outward, framing the body
    against the floor rather than covering its chest and face, and from three stacks a crown climbs from behind its
    head. One stack is a single shoulder alight; five, the whole silhouette."""
    s = stacks
    tongues = []

    def add(x, y, h, w, lean, ph, wave=0.03, foot=0.09, sharp=1.9, flick=0.1, sway=0.035, forks=()):
        tg = dict(x=x, y=y, h=h, w=w, lean=lean, ph=ph, wave=wave, foot=foot, sharp=sharp, flick=flick, sway=sway)
        tongues.append(tg)
        for side, at, size, fork_lean in forks:
            tongues.append(fork(tg, side, at, size, fork_lean))

    # Shoulders: the first stack lights one, the second both; from three each licks in two tips.
    right = (0.34, 0.35, 0.38, 0.42, 0.48)[s - 1]
    add(0.31, 0.62, right, (0.076, 0.076, 0.084, 0.09, 0.096)[s - 1], 0.07, 0.0,
        forks=((1, 0.32, 0.5, 0.1),) if s >= 3 else ())
    if s >= 2:
        left = (0, 0.3, 0.34, 0.38, 0.44)[s - 1]
        add(-0.3, 0.64, left, (0, 0.072, 0.08, 0.086, 0.092)[s - 1], -0.06, 2.3,
            forks=((-1, 0.35, 0.48, 0.09),) if s >= 3 else ())
    # Flanks: low licks out of the body's sides, leaning away from it.
    add(-0.45, 0.27, (0.17, 0.19, 0.23, 0.27, 0.32)[s - 1], 0.044 + 0.004 * s, -0.08, 4.1, foot=0.07, flick=0.14)
    if s >= 3:
        add(0.46, 0.29, (0, 0, 0.2, 0.25, 0.3)[s - 1], 0.044 + 0.004 * s, 0.08, 1.2, foot=0.07, flick=0.14)
    # The crown, climbing from behind the head so the face stays clear.
    if s >= 3:
        add(0.03, 0.82, (0, 0, 0.24, 0.32, 0.42)[s - 1], 0.056 + 0.006 * s, 0.02, 3.4, foot=0.07,
            forks=((-1, 0.4, 0.45, 0.06),) if s >= 5 else ())
    if s >= 4:
        add(-0.15, 0.79, (0, 0, 0, 0.22, 0.29)[s - 1], 0.048 + 0.004 * s, -0.04, 5.2, foot=0.06)
        add(0.18, 0.78, (0, 0, 0, 0.2, 0.27)[s - 1], 0.048 + 0.004 * s, 0.05, 0.8, foot=0.06)
    if s >= 5:
        # A full burn licks up the legs too.
        add(-0.4, 0.06, 0.24, 0.042, -0.07, 2.8, foot=0.06, flick=0.16)
        add(0.42, 0.05, 0.22, 0.042, 0.07, 5.9, foot=0.06, flick=0.16)
    return tongues


def flame_frame(stacks, index, noise):
    phase = 2 * np.pi * index / FLAME_FRAMES
    frame = Frame(*FLAME_EXTENT, FLAME_TPU)
    tongues = flame_tongues(stacks)
    heat = (stacks - 1) / (MAX_STACKS - 1)
    # A wisp breaks off the tallest tongues' tips once a loop and rises, shrinking.
    wisps = []
    tallest = sorted(range(len(tongues)), key=lambda i: -(tongues[i]["y"] + tongues[i]["h"]))[: 1 + (stacks + 1) // 2]
    for n, i in enumerate(tallest):
        tg = tongues[i]
        rise = loop_rise(n, phase)
        size = (1.0 - rise) ** 1.2
        if size < 0.15:
            continue
        tip_x = tg["x"] + tg.get("lean", 0.0) * 1.1
        wisps.append(dict(x=tip_x, y=tg["y"] + tg["h"] * (0.72 + 0.55 * rise),
                          h=(0.07 + 0.012 * stacks) * size, w=(0.018 + 0.003 * stacks) * size,
                          lean=tg.get("lean", 0.0) * 0.3, wave=0.0, ph=float(n), flick=0.0, sway=0.0, sharp=1.4))
    embers = []
    for i in range(1 + stacks):
        rise = loop_rise(i * 1.37 + 0.2, phase)
        side = -1 if i % 2 else 1
        x = side * (0.1 + 0.32 * ((i * 0.37) % 1.0)) + 0.04 * math.sin(phase * 2 + i)
        y = 0.75 + 0.04 * stacks + rise * (0.45 + 0.05 * stacks)
        fade = smoothstep(0.0, 0.15, rise) * smoothstep(1.0, 0.65, rise)
        r = (0.006 + 0.0016 * stacks) * (0.6 + 0.4 * fade)
        drift = math.degrees(math.atan2(1.0, 0.25 * math.cos(phase * 2 + i)))
        if fade > 0.05:
            embers.append((x, y, r, float(fade * (0.45 + 0.11 * stacks)), 2.2, drift))
    body = (0.0, 0.45, 0.5, 0.46, 0.1 + 0.04 * stacks)
    paint_fire(frame, tongues, phase, heat, noise, wisps=wisps, embers=embers, glow=0.14 + 0.025 * stacks,
               body_glow=body)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# Ember Memory: the ember passed from the fallen to a neighbour, facing +x with its head at the pivot.

ARC_FRAMES = 4
ARC_TPU = 100.0


def streak(frame, length, radius, wave, phase):
    """Coverage and heat of a tapered streak trailing behind the origin along -x: a run of soft beads thinning toward
    the tail, rippling as it flies. Heat falls from 1 at the head to 0 at the tail."""
    body = np.zeros_like(frame.X)
    heat = np.zeros_like(frame.X)
    for k in range(28):
        t = k / 27.0
        x = -t * length
        y = wave * math.sin(phase + t * 7.0) * t
        r = radius * (1.0 - t) ** 0.85 + radius * 0.1
        d = np.sqrt((frame.X - x) ** 2 + (frame.Y - y) ** 2)
        cover = np.clip((r - d) / frame.px + 0.5, 0.0, 1.0) * (1.0 - t ** 2.0)
        body = np.maximum(body, cover)
        heat = np.maximum(heat, cover * (1.0 - t))
    return body, heat


def ember_arc_frame(index):
    frame = Frame(-0.6, -0.18, 0.18, 0.18, ARC_TPU)
    phase = 2 * np.pi * index / ARC_FRAMES
    body, heat = streak(frame, 0.38 + 0.03 * math.sin(phase), 0.03, 0.012, phase)
    frame.paint(EMBER, np.clip(blur(body, 3.0) * 0.5, 0, 0.3))
    frame.paint(SOOT, blur(grow(body, 1.1), 0.4) * 0.72)
    frame.paint(mix(CRIMSON_DEEP, CRIMSON, smoothstep(0.0, 0.35, heat)), blur(body, 0.3))
    frame.paint(EMBER, blur(smoothstep(0.3, 0.55, heat), 0.6))
    frame.paint(YELLOW, blur(smoothstep(0.58, 0.8, heat), 0.8))
    frame.paint(WHITE_HOT, blur(smoothstep(0.84, 0.97, heat), 0.8))
    # Sparks shed from the tail, flickering frame to frame.
    for k in range(3):
        t = (k * 0.31 + index * 0.17) % 1.0
        x = -0.14 - t * 0.26
        y = (0.03 + 0.014 * k) * (1 if (k + index) % 2 else -1)
        paint_ember(frame, x, y, 0.0065 * (1.0 - 0.4 * t), 0.85 - 0.6 * t, stretch=1.5, angle=0.0)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# Crucible Vent: the eruption standing on a vented enemy's feet (bottom-centre pivot), the embers it throws up, and
# the unit-radius ember ring the cast draws round the hero.

VENT_FRAMES = 8
VENT_TPU = 100.0
VENT_EMBER_FRAMES = 4
RING_TPU = 180.0
RING_FRAMES = 3


def floor_glow(frame, rx, ry, strength, color, cy=0.0):
    d = np.sqrt((frame.X / rx) ** 2 + ((frame.Y - cy) / ry) ** 2)
    frame.paint(color, strength * smoothstep(1.0, 0.0, d) ** 1.6)


def smoke(frame, puffs):
    """Soft soot puffs (x, y, radius, alpha) the eruption leaves as it gutters: they darken lava under the fire."""
    for x, y, r, a in puffs:
        frame.paint(hex_rgb("#3A2426"), disc(frame, x, y, r, soft=r * 0.9) * a)


def vent_column_frame(index, noise):
    frame = Frame(-0.62, -0.26, 0.62, 1.78, VENT_TPU)
    phase = 2 * np.pi * index / VENT_FRAMES
    i = index
    # By frame: a white-hot flare at the feet, the column shooting up, standing, breaking into rising tongues, then
    # embers over a guttering foot.
    height = (0.42, 0.98, 1.24, 1.16, 0.66, 0.38, 0.22, 0.0)[i]
    width = (0.21, 0.19, 0.18, 0.16, 0.14, 0.11, 0.08, 0.0)[i]
    splash = (0.3, 0.36, 0.32, 0.26, 0.2, 0.14, 0.09, 0.0)[i]
    flare = (1.0, 0.85, 0.7, 0.55, 0.42, 0.3, 0.2, 0.12)[i]
    heat = (1.0, 1.0, 1.0, 0.9, 0.7, 0.45, 0.25, 0.1)[i]

    floor_glow(frame, 0.5, 0.21, 0.5 * flare, EMBER)
    floor_glow(frame, 0.32, 0.13, 0.75 * flare, YELLOW)
    if i <= 1:
        floor_glow(frame, 0.22, 0.09, 1.0 - 0.5 * i, WHITE_HOT)
    if i >= 4:
        smoke(frame, [(-0.1 + 0.05 * i, 0.5 + 0.12 * i, 0.07 + 0.012 * i, 0.14 + 0.025 * (7 - i)),
                      (0.12 - 0.04 * i, 0.38 + 0.13 * i, 0.06 + 0.01 * i, 0.12 + 0.025 * (7 - i))])

    tongues = []
    if height > 0:
        tongues.append(dict(x=0.0, y=0.0, h=height, w=width, lean=0.02, wave=0.025, ph=0.4, sharp=1.05, flick=0.04,
                            sway=0.02, foot=0.1))
        tongues.append(dict(x=-0.07, y=0.0, h=height * 0.72, w=width * 0.62, lean=-0.06, wave=0.02, ph=1.7,
                            sharp=1.35, flick=0.05, sway=0.02, foot=0.1))
        tongues.append(dict(x=0.08, y=0.0, h=height * 0.64, w=width * 0.56, lean=0.07, wave=0.02, ph=3.1,
                            sharp=1.35, flick=0.05, sway=0.02, foot=0.1))
    if splash > 0:
        # Licks flung out along the floor at the column's foot.
        for side, ph in ((-1, 0.9), (1, 2.6)):
            tongues.append(dict(x=side * 0.15, y=0.0, h=splash, w=0.055, lean=side * 0.2, wave=0.015, ph=ph,
                                sharp=1.8, flick=0.08, sway=0.02, foot=0.06))
            tongues.append(dict(x=side * 0.27, y=-0.02, h=splash * 0.62, w=0.04, lean=side * 0.17, wave=0.01,
                                ph=ph + 1.3, sharp=1.9, flick=0.1, sway=0.02, foot=0.05))
            tongues.append(dict(x=side * 0.36, y=-0.05, h=splash * 0.38, w=0.03, lean=side * 0.12, wave=0.01,
                                ph=ph + 2.2, sharp=2.0, flick=0.1, sway=0.02, foot=0.04))
    if tongues:
        paint_fire(frame, tongues, phase, heat, noise, glow=0.28, rim=1.3)
    # The column breaking: its top tears into tongues that rise on, shrinking and burning out.
    for k, (start, x, size) in enumerate(((3, -0.03, 1.0), (3, 0.07, 0.65), (4, -0.09, 0.75), (5, 0.04, 0.55))):
        if i < start:
            continue
        age = i - start
        y = (1.0 if start == 3 else 0.6 + 0.12 * (start - 4)) + 0.17 * age + 0.04 * k
        s = size * (1.0 - 0.18 * age)
        piece = dict(x=x + 0.02 * age * (1 if k % 2 else -1), y=y, h=0.3 * s, w=0.07 * s, lean=0.025 * (1 if k % 2 else -1),
                     wave=0.01, ph=k, sharp=1.5, flick=0.0, sway=0.0, foot=0.08 * s)
        paint_fire(frame, [piece], phase, heat * 0.8, noise, glow=0.15, rim=0.8, opacity=max(0.0, 1.0 - 0.28 * age))
    # Embers flung from the column, more and higher as it breaks: thrown clear of it, beside and above, never specks on
    # its own fire.
    for k in range(3 + i):
        t = (k * 0.618) % 1.0
        side = -1 if k % 2 else 1
        x = side * (width * 1.3 + 0.05 + 0.1 * t + 0.02 * i)
        y = 0.25 + (0.95 + 0.06 * i) * ((k * 0.37 + 0.13 * i) % 1.0)
        hot = max(0.0, 1.0 - 0.1 * i - 0.25 * t)
        if i >= 1:
            paint_ember(frame, x, y, 0.007 + 0.004 * ((k * 0.53) % 1.0), hot, stretch=1.4, angle=90 + 30 * side * t)
    return frame


def vent_ember_frame(index):
    """The embers an eruption throws up, which the view sends rising: hot as they leave, then cooling to crimson and
    thinning out, each frame a little higher and further out. The view centres them on the column, which still burns
    under them, so they keep clear of its middle and fly up either side of it rather than speckling its fire."""
    frame = Frame(-0.4, -0.26, 0.4, 0.42, VENT_TPU)
    cool = index / (VENT_EMBER_FRAMES - 1)
    seeds = ((-0.2, -0.1, 1.0), (0.18, -0.06, 0.8), (-0.25, 0.07, 0.9), (0.23, 0.09, 0.6), (-0.18, 0.18, 0.5),
             (0.27, 0.2, 0.7), (0.2, -0.15, 0.45), (-0.29, -0.02, 0.35), (0.16, 0.14, 0.3))
    for k, (x, y, size) in enumerate(seeds):
        if k >= len(seeds) - 2 * index:
            continue  # thinning out as they cool
        lift = 0.04 * index * (0.6 + 0.4 * size)
        drift = 0.02 * index * (1 if x > 0 else -1)
        hot = max(0.0, 1.0 - cool * (0.8 + 0.35 * (1.0 - size)))
        paint_ember(frame, x + drift, y + lift, (0.008 + 0.011 * size) * (1.0 - 0.25 * cool), hot, stretch=1.35,
                    angle=90 + 25 * (x * 5))
    return frame


def vent_ring_frame(index, noise):
    """The cast ring round the hero, a unit radius on the floor (half as deep as it is wide): a white-hot line of
    ignition, a burning band with flames licking up from it, then the band breaking into embers."""
    tongue_room = 0.26
    frame = Frame(-1.16, -0.62, 1.16, 0.62 + tongue_room, RING_TPU)
    X, Y = frame.X, frame.Y
    d = np.sqrt(X ** 2 + (Y / 0.5) ** 2)
    angle = np.arctan2(Y / 0.5, X)
    ca, sa = np.cos(angle), np.sin(angle)
    depth = (1.0 - d) + noise(ca * 0.6, sa * 0.6) * 0.012  # inward from the rim, in units of x
    band = (0.05, 0.075, 0.06)[index] * (1.0 + 0.35 * noise(ca * 1.3 + 2.0, sa * 1.3))
    strength = (1.0, 0.95, 0.6)[index]
    breakup = np.ones_like(X)
    if index == 2:
        breakup = smoothstep(-0.25, 0.35, noise(ca * 1.7 + 3.0, sa * 1.7))
    mid = depth - band * 0.5
    frame.paint(EMBER, smoothstep(band + 0.1, 0.0, np.abs(mid)) * 0.3 * strength * breakup)
    frame.paint(SOOT, smoothstep(0.03, 0.0, np.abs(depth + 0.014)) * (depth < 0.012) * 0.55 * strength * breakup)
    frame.paint(CRIMSON, smoothstep(band * 0.62, band * 0.45, np.abs(mid)) * strength * breakup)
    frame.paint(EMBER, smoothstep(band * 0.45, band * 0.25, np.abs(mid)) * strength * breakup)
    frame.paint(mix(YELLOW, WHITE_HOT, 0.6 if index == 0 else 0.0),
                smoothstep(band * 0.24, 0.0, np.abs(mid)) * strength * breakup)
    # Flames licking up from the band, screen-up, at uneven steps round it.
    if index <= 1:
        # Close enough to overlap into a burning wall, the front ones lower so the ring keeps its floor.
        tongues = []
        # Broad, soft-shouldered licks that lean along the ring as it spreads, not thorns standing on it.
        count = 24
        for k in range(count):
            a = 2 * np.pi * (k + 0.3 * math.sin(k * 2.3)) / count
            x, y = math.cos(a) * 0.975, math.sin(a) * 0.4875
            back = 0.65 + 0.35 * math.sin(a)
            h = (0.08 if index == 0 else 0.14) * (0.6 + 0.55 * ((k * 0.618) % 1.0)) * back
            tongues.append(dict(x=x, y=y - 0.025, h=h, w=0.064, lean=0.05 * math.cos(a) + 0.02 * math.sin(k * 1.7),
                                wave=0.012, ph=k, sharp=1.25, flick=0.12, sway=0.015, foot=0.04))
        paint_fire(frame, tongues, 2 * np.pi * index / 3, 0.6, noise, glow=0.12, rim=0.6)
    else:
        for k in range(18):
            a = 2 * np.pi * k / 18 + 0.2
            paint_ember(frame, math.cos(a) * 0.98, math.sin(a) * 0.49 + 0.03 + 0.04 * ((k * 0.618) % 1.0),
                        0.009 + 0.004 * (k % 2), 0.55, stretch=1.3)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# Crucible Vent: the eruption ring each eruption draws on the floor round the enemy it spends, at the splash's radius.
# It rides a sheet of its own (VFX_Skill_VentSplash): the art set holds it as optional, so a set painted before it is
# still whole and the three sheets above stay as they were painted, and it draws on the floor order apart from the
# column and embers, so a sheet of its own costs a Vent across a crowd no batch.

SPLASH_TPU = 136.0  # about a texel a phone pixel once grown to the 1.2-unit splash (144 pixels a unit)
SPLASH_FRAMES = 6
# Room round the unit contour for the soot rim and the embers thrown off it, and over its far side for the flames.
SPLASH_EXTENT = (-1.16, -0.6, 1.16, 0.84)
SPLASH_LICKS = 30
# The band burns this far inside the contour, so the soot rim round it, its outermost paint, ends on the splash.
SPLASH_RIM = 0.02
SCORCH = hex_rgb("#2B1715")


def vent_splash_frame(index, noise, fine, fire_noise):
    """The eruption ring, a unit radius across the floor and half as deep (ArenaFloor.DepthScale), which the view grows
    from 24/38 of the splash to all of it over its first 0.14 s and fades over its last 0.11 s, as it grew the
    procedural ring; so the frames carry the burst. A white-hot line of ignition with the first licks; flames leaping
    off a burning band, tallest in clusters along the far side so the near side keeps its floor; the licks shedding
    wisps as the band tears into burning pieces; then embers, the last pieces and a scorched ring. Its leading edge
    holds the splash's radius throughout; only its inner side burns ragged into the floor."""
    frame = Frame(*SPLASH_EXTENT, SPLASH_TPU)
    X, Y = frame.X, frame.Y
    d = np.sqrt(X ** 2 + (Y / 0.5) ** 2)
    angle = np.arctan2(Y / 0.5, X)
    ca, sa = np.cos(angle), np.sin(angle)
    # Inward from the band's leading edge, in units of the radius across; it wavers a little, as a painted line does.
    depth = (1.0 - d) - SPLASH_RIM + noise(ca * 0.7, sa * 0.7) * 0.008
    # By frame: the band's width, its strength and heat, how far it has torn apart (None while whole), the licks'
    # height, the scorch it leaves and the heat trailing it toward the feet.
    band, strength, heat, breakup, tall, scorch, trail = (
        (0.05, 1.0, 1.0, None, 0.15, 0.0, 0.16),
        (0.08, 1.0, 0.92, None, 0.3, 0.0, 0.1),
        (0.075, 1.0, 0.74, 0.1, 0.25, 0.08, 0.0),
        (0.065, 0.96, 0.52, 0.3, 0.13, 0.15, 0.0),
        (0.058, 0.9, 0.36, 0.48, 0.05, 0.2, 0.0),
        (0.05, 0.78, 0.22, 0.64, 0.0, 0.2, 0.0))[index]
    # The ignition's white-hot flash down the band's middle, gone by the third frame.
    flash = (1.0, 0.35, 0.0, 0.0, 0.0, 0.0)[index]
    band = band * (1.0 + 0.4 * noise(ca * 1.3 + 2.0, sa * 1.3))
    width = band * (0.82 + 0.42 * fine(X + 0.3, Y * 2.0 - 0.11 * index))
    whole = np.ones_like(X)
    if breakup is not None:
        whole = smoothstep(-0.7 + 1.5 * breakup, -0.35 + 1.5 * breakup, noise(ca * 2.2 + 3.0, sa * 2.2) * 0.9 + 0.1)
    mid = depth - width * 0.5
    across = np.abs(mid) / np.maximum(width * 0.5, 1e-4)  # 0 on the band's middle line, 1 at its edges
    # Hot spots and cooler stretches round the band, flickering frame to frame.
    along = noise(ca * 4.0 + 7.0, sa * 4.0 - 0.9 * index)
    hot = np.clip(heat * (0.8 + 0.32 * noise(ca * 1.8 + 6.0, sa * 1.8) + 0.16 * along), 0.0, 1.0)

    if scorch > 0:
        frame.paint(SCORCH, smoothstep(band * 1.4, band * 0.3, np.abs(depth - band * 0.5)) * scorch)
    if trail > 0:
        # Heat trailing the band toward the feet, never a wash over the whole floor.
        frame.paint(EMBER, smoothstep(0.22, 0.0, depth - width) * (depth > width) * trail)
    frame.paint(EMBER, smoothstep(width * 0.5 + 0.045, 0.0, np.abs(mid)) * 0.28 * strength * whole)
    # The soot rim just outside the leading edge, which is what reads on the Foundry's orange lava.
    frame.paint(SOOT, smoothstep(0.03, 0.0, np.abs(depth + 0.012)) * (depth < 0.01) * 0.6 * strength * whole)
    edge = mix(CRIMSON_DEEP, CRIMSON, smoothstep(0.1, 0.5, hot))
    body = mix(CRIMSON, EMBER, smoothstep(0.15, 0.6, hot))
    core = mix(mix(EMBER, YELLOW, smoothstep(0.35, 0.75, hot)), WHITE_HOT, smoothstep(0.85, 1.0, hot))
    if flash > 0:
        core = mix(core, WHITE_HOT, flash * smoothstep(0.62, 0.1, across))
    frame.paint(edge, smoothstep(1.24, 0.92, across) * strength * whole)
    frame.paint(body, smoothstep(0.92, 0.56, across) * strength * whole)
    frame.paint(core, smoothstep(0.56, 0.06, across) * strength * whole)

    if tall > 0:
        tongues, wisps = [], []
        for k in range(SPLASH_LICKS):
            a = 2 * np.pi * (k + 0.3 * math.sin(k * 2.3)) / SPLASH_LICKS + 0.15
            # Tallest along the far side and in clusters round it, with lower licks between, never an even crown; they
            # die away round the sides, so the near side is a clean burning band and keeps its floor, never a row of
            # thorns in front of the enemy.
            back = smoothstep(-0.5, 0.75, math.sin(a))
            cluster = float(noise(np.array([math.cos(a) * 1.1 + 4.0]), np.array([math.sin(a) * 1.1]))[0])
            h = tall * (0.5 + 0.65 * ((k * 0.618) % 1.0)) * (0.1 + 0.9 * back) * (0.8 + 0.45 * cluster)
            if back < 0.12 or h < 0.03:
                continue
            x, y = math.cos(a) * 0.955, math.sin(a) * 0.4775
            tg = dict(x=x, y=y - 0.02, h=h, w=0.056 + 0.02 * ((k * 0.37) % 1.0),
                      lean=0.06 * math.cos(a) + 0.015 * math.sin(k * 1.7), wave=0.012, ph=k, sharp=1.2, flick=0.12,
                      sway=0.015, foot=0.04)
            tongues.append(tg)
            if h > 0.17 and index in (1, 2):
                tongues.append(fork(tg, 1 if k % 2 else -1, 0.38, 0.5, 0.08))
            if index >= 2 and h > 0.1:
                # A wisp tears off the tip and rises on, smaller each frame.
                rise = (index - 1) * 0.5
                wisps.append(dict(x=x + tg["lean"] * 1.2, y=y + h * (0.85 + 0.35 * rise), h=0.05 * (1.2 - 0.3 * rise),
                                  w=0.018, lean=tg["lean"] * 0.3, wave=0.0, ph=float(k), flick=0.0, sway=0.0,
                                  sharp=1.4))
        paint_fire(frame, tongues, 2 * np.pi * index / SPLASH_FRAMES, heat, fire_noise, wisps=wisps, glow=0.1,
                   rim=0.8)

    if index >= 1:
        # Embers thrown up off the band, the same ones frame to frame: every one's flight is drawn before any is skipped.
        rng = np.random.default_rng(17)
        for k in range(18):
            a, start = rng.uniform(0, 2 * np.pi), int(rng.integers(1, 3))
            out, up = rng.uniform(0.01, 0.028), rng.uniform(0.03, 0.06)
            size, cool = rng.uniform(0.007, 0.012), rng.random()
            if index < start or math.sin(a) < -0.35:
                continue
            age = index - start + 0.5
            reach = 0.97 + out * age
            x, y = math.cos(a) * reach, math.sin(a) * 0.5 * reach + 0.02 + up * age
            flight = math.degrees(math.atan2(math.sin(a) * 0.5 * out + up, math.cos(a) * out))
            paint_ember(frame, x, y, size * (1.0 - 0.08 * age), max(0.0, 0.95 - 0.18 * age - 0.2 * cool), stretch=1.6,
                        angle=flight)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# Forge Burst: the Vanguard's hammer-blow shockwave on the floor (a unit radius), its steel sparks (facing +x, head at
# the pivot) and the flash at the hero.

SHOCK_TPU = 180.0
SHOCK_FRAMES = 4
SPARK_TPU = 128.0
SPARK_FRAMES = 3
FLASH_TPU = 100.0
FLASH_FRAMES = 3


def shockwave_frame(index, noise):
    """A white leading edge on a steel-blue band with a soot rim outside it (blue and the rim are what read on the
    Foundry's orange); behind it the air shimmers as heat does, light and dark ripples that waver round the ring,
    first over a brief pale flash, then thinning, then breaking up into dust."""
    frame = Frame(-1.06, -0.56, 1.06, 0.56, SHOCK_TPU)
    X, Y = frame.X, frame.Y
    d = np.sqrt(X ** 2 + (Y / 0.5) ** 2)
    angle = np.arctan2(Y / 0.5, X)
    ca, sa = np.cos(angle), np.sin(angle)
    depth = (1.0 - d) + noise(ca * 0.8, sa * 0.8) * 0.01
    band = (0.1, 0.07, 0.048, 0.032)[index] * (1.0 + 0.3 * noise(ca * 1.5 + 4.0, sa * 1.5))
    strength = (1.0, 0.95, 0.75, 0.45)[index]
    breakup = np.ones_like(X)
    if index >= 2:
        breakup = smoothstep(-0.3 + 0.25 * (index - 2), 0.3 + 0.2 * (index - 2),
                             noise(ca * 2.0 + 5.0, sa * 2.0))
    behind = depth - band
    if index <= 1:
        # The flash trails the band toward the hero, never a full disc: a full wash muddied the orange lava.
        frame.paint(STEEL_LIGHT, smoothstep(0.3, 0.0, behind) * (behind > 0) * (0.24 - 0.12 * index))
    # Heat shimmer: two wavering ripples behind the band.
    shimmer = noise(ca * 3.0 + 7.0, sa * 3.0 + index)
    trail = smoothstep(0.26, 0.02, behind) * (behind > 0)
    ripple = np.sin(behind * 2 * np.pi / (0.085 + 0.02 * shimmer) - index * 1.6 + shimmer * 1.5)
    frame.paint(STEEL_PALE, trail * np.clip(ripple, 0, 1) ** 2 * 0.2 * strength * breakup)
    frame.paint(STEEL_DEEP, trail * np.clip(-ripple, 0, 1) ** 2 * 0.14 * strength * breakup)
    # The soot rim outside the leading edge, then the band and its white edge.
    frame.paint(STEEL_SOOT, smoothstep(0.034, 0.0, -depth) * (depth <= 0.002) * 0.6 * strength * breakup)
    body = smoothstep(band + 0.012, band - 0.012, depth) * smoothstep(-0.004, 0.004, depth)
    tone = mix(STEEL_LIGHT, STEEL, smoothstep(0.0, band * 0.8, depth))
    tone = mix(tone, STEEL_DEEP, smoothstep(band * 0.55, band, depth) * 0.55)
    frame.paint(tone, body * strength * breakup)
    # Speed lines: short radial streaks of light through the band, the blow's force carried outward.
    if index <= 1:
        spokes = np.clip(np.cos(angle * 36.0 + 3.0 * noise(ca + 9.0, sa)) ** 30, 0, 1)
        frame.paint(STEEL_PALE, spokes * body * smoothstep(band, band * 0.2, depth) * 0.55 * strength)
    lead = smoothstep(0.024, 0.006, np.abs(depth - 0.008))
    frame.paint(WHITE if index < 2 else STEEL_PALE, lead * strength * breakup)
    # Dust and grit kicked up off the floor at the band.
    rng = np.random.default_rng(31 + index)
    for k in range(18 + 4 * index):
        a = rng.uniform(0, 2 * np.pi)
        r = 1.0 - rng.uniform(-0.02, band.mean() + 0.06)
        x, y = math.cos(a) * r, math.sin(a) * 0.5 * r
        size = rng.uniform(0.006, 0.014)
        light = rng.random() < 0.5
        frame.paint(STEEL_PALE if light else hex_rgb("#2B2F3A"),
                    disc(frame, x, y, size, soft=0.004) * (0.55 if light else 0.5) * strength)
    return frame


def forge_spark_frame(index):
    frame = Frame(-0.46, -0.12, 0.12, 0.12, SPARK_TPU)
    body, heat = streak(frame, (0.3, 0.22, 0.13)[index], 0.017, 0.0, 0.0)
    frame.paint(STEEL, np.clip(blur(body, 2.5) * 0.6, 0, 0.4))
    frame.paint(STEEL_SOOT, blur(grow(body, 0.9), 0.4) * 0.6)
    frame.paint(mix(STEEL_DEEP, STEEL, smoothstep(0.0, 0.3, heat)), blur(body, 0.3))
    frame.paint(STEEL_LIGHT, blur(smoothstep(0.3, 0.6, heat), 0.5))
    frame.paint(WHITE, blur(smoothstep(0.62, 0.85, heat), 0.6))
    frame.paint(WHITE, disc(frame, 0.0, 0.0, 0.014, soft=0.006))
    return frame


def forge_flash_frame(index):
    """The blow's flash at the hero: a white-hot burst in a steel-blue glow with eight short broad rays, swelling and
    gone."""
    frame = Frame(-1.12, -0.92, 1.12, 0.98, FLASH_TPU)
    X, Y = frame.X, frame.Y
    r = np.sqrt(X ** 2 + (Y * 1.2) ** 2)
    a = np.arctan2(Y, X)
    size = (0.46, 0.62, 0.74)[index]
    strength = (1.0, 0.72, 0.36)[index]
    frame.paint(STEEL, smoothstep(size * 1.15, 0.0, r) ** 1.4 * 0.55 * strength)
    rays = np.clip(np.cos(a * 4.0 + 0.4) ** 2, 0, 1) ** 10 * (0.75 + 0.25 * np.cos(a * 8.0))
    frame.paint(STEEL_LIGHT, rays * smoothstep(size * 1.4, size * 0.12, r) * 0.9 * strength)
    frame.paint(STEEL_PALE, smoothstep(size * 0.62, 0.0, r) * 0.9 * strength * (1.0 if index == 0 else 0.6))
    frame.paint(WHITE, smoothstep(size * 0.34, 0.0, r) * strength)
    return frame


# ----------------------------------------------------------------------------------------------------------------------
# Export.


def to_image(premultiplied):
    """Straight-alpha RGBA bytes (image order), with colour bled into clear texels so bilinear filtering and block
    compression never pull a dark fringe from them. A blur's last invisible tail (under 2/255) is cleared, so it never
    widens a frame's cell."""
    a = np.where(premultiplied[..., 3] < 2.0 / 255.0, 0.0, premultiplied[..., 3])
    rgb = np.where(a[..., None] > 1e-6, premultiplied[..., :3] / np.maximum(a[..., None], 1e-6), 0.0)
    known = a > 0.0
    if known.any() and not known.all():
        _, (iy, ix) = ndimage.distance_transform_edt(~known, return_indices=True)
        rgb = rgb[iy, ix]
    out = np.concatenate([np.clip(rgb, 0, 1), np.clip(a, 0, 1)[..., None]], axis=-1)
    return (out * 255.0 + 0.5).astype(np.uint8)


def crop_box(name, images, margin=1):
    """The smallest box (x0, y0, x1, y1 in image order) holding every image's visible texels, plus a clear margin."""
    union = np.zeros(images[0].shape[:2], bool)
    for img in images:
        union |= img[..., 3] > 0
    ys, xs = np.nonzero(union)
    h, w = union.shape
    if xs.min() < margin or ys.min() < margin or xs.max() >= w - margin or ys.max() >= h - margin:
        # Paint that reaches the frame's edge was cut off there, and would leave no clear texel round the cell.
        raise ValueError(f"{name}: a frame's paint reaches its edge; widen its extent")
    return xs.min() - margin, ys.min() - margin, xs.max() + 1 + margin, ys.max() + 1 + margin


class Strip:
    """Frames of one effect, cropped alike, with the pivot each pins at."""

    def __init__(self, name, frames):
        resolved = [to_image(f.resolve()) for f in frames]
        x0, y0, x1, y1 = crop_box(name, resolved)
        self.name = name
        self.images = [img[y0:y1, x0:x1] for img in resolved]
        f = frames[0]
        self.tpu = f.tpu
        width, height = x1 - x0, y1 - y0
        # The world origin of the painted frame, in texels from its left and bottom edges, then from the crop's.
        origin_x = -f.x0 * f.tpu - x0
        origin_y = -f.y0 * f.tpu - (f.h - y1)
        self.pivot = (origin_x / width, origin_y / height)
        self.cells = []
        self.sheet = None


def shelves(order, width):
    """Shelf layout of the frames at the given sheet width: each frame's corner (image order) and the sheet height."""
    placements = []
    x = y = shelf = 0
    for s, i, img in order:
        h, w = img.shape[:2]
        if x + w > width:
            x, y, shelf = 0, y + shelf + GUTTER, 0
        placements.append((s, i, img, x, y))
        x += w + GUTTER
        shelf = max(shelf, h)
    return placements, (y + shelf + 3) // 4 * 4


def pack(strips, widest=2048):
    """Shelf-packs every frame of the strips onto one sheet, tallest frames first, at the width of least area (ASTC
    memory is the area), squarest on a tie. Each strip records its cells in Unity's texel space, row 0 at the bottom."""
    order = []
    for s in strips:
        s.cells = [None] * len(s.images)
        for i, img in enumerate(s.images):
            order.append((s, i, img))
    order.sort(key=lambda item: (-item[2].shape[0], -item[2].shape[1]))
    best = None
    narrowest = max(img.shape[1] for _, _, img in order)
    for width in range((narrowest + 3) // 4 * 4, widest + 1, 4):
        _, height = shelves(order, width)
        key = (width * height, abs(height - width))
        if height <= widest and (best is None or key < best[0]):
            best = (key, width)
    width = best[1]
    placements, height = shelves(order, width)
    sheet = np.zeros((height, width, 4), np.uint8)
    for s, i, img, px, py in placements:
        h, w = img.shape[:2]
        sheet[py:py + h, px:px + w] = img
        s.cells[i] = (px, height - py - h, w, h)
    return sheet


def bleed(sheet):
    """Fills the clear texels' colour from the nearest painted texel across the whole sheet, so neither bilinear
    filtering at a frame's edge nor an ASTC block straddling one pulls in black."""
    known = sheet[..., 3] > 0
    if known.any() and not known.all():
        _, (iy, ix) = ndimage.distance_transform_edt(~known, return_indices=True)
        sheet = sheet.copy()
        sheet[..., :3] = sheet[iy, ix, :3]
    return sheet


def astc_bytes(width, height):
    """ASTC 4x4: 16 bytes a 4x4 block, a byte a texel."""
    return ((width + 3) // 4) * ((height + 3) // 4) * 16


# ----------------------------------------------------------------------------------------------------------------------
# Unity files.


def existing_guid(meta_path):
    if os.path.exists(meta_path):
        match = re.search(r"^guid: ([0-9a-f]{32})", open(meta_path, encoding="utf-8").read(), re.M)
        if match:
            return match.group(1)
    return uuid.uuid4().hex


def existing_sprite_id(meta_path):
    if os.path.exists(meta_path):
        match = re.search(r"^    spriteID: ([0-9a-f]{32})", open(meta_path, encoding="utf-8").read(), re.M)
        if match:
            return match.group(1)
    return uuid.uuid4().hex


# Modelled on the painted strikes' import (Art/Effects/FoundryStrikes): bilinear, no mipmaps, ASTC 4x4 on Android, at
# the sheet's full size, alpha as transparency so the importer too bleeds colour into the clear texels.
TEXTURE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {tpu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: 48
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 1
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""

ASSET_META = """fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 11400000
  userData:
  assetBundleName:
  assetBundleVariant:
"""

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def write(path, text):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def number(value):
    text = f"{value:.7f}".rstrip("0").rstrip(".")
    return "0" if text in ("", "-0") else text


def strip_yaml(strip, guid, sheet, indent):
    pad = " " * indent
    lines = [f"{pad}Sheet: {{fileID: 2800000, guid: {guid}, type: 3}}",
             f"{pad}SheetWidth: {sheet.shape[1]}",
             f"{pad}SheetHeight: {sheet.shape[0]}",
             f"{pad}TexelsPerUnit: {number(strip.tpu)}",
             f"{pad}Pivot: {{x: {number(strip.pivot[0])}, y: {number(strip.pivot[1])}}}",
             f"{pad}Frames:"]
    for x, y, w, h in strip.cells:
        lines += [f"{pad}- X: {x}", f"{pad}  Y: {y}", f"{pad}  Width: {w}", f"{pad}  Height: {h}"]
    return "\n".join(lines)


def write_asset(families, script_guid):
    body = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000", "MonoBehaviour:",
            "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
            f"  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}", "  m_Name: SkillEffectArtSet",
            "  m_EditorClassIdentifier: "]
    fields = {}
    for family in families.values():
        for strip in family["strips"]:
            fields[strip.name] = (strip, family["guid"], family["sheet"])
    body.append("  _flames:")
    for stacks in range(1, MAX_STACKS + 1):
        strip, guid, sheet = fields[f"flame{stacks}"]
        body.append("  - " + strip_yaml(strip, guid, sheet, 4)[4:])
    for field, name in (("_emberArc", "emberArc"), ("_ventColumn", "ventColumn"), ("_ventEmbers", "ventEmbers"),
                        ("_ventRing", "ventRing"), ("_ventSplash", "ventSplash"), ("_shockwave", "shockwave"),
                        ("_forgeSpark", "forgeSpark"), ("_forgeFlash", "forgeFlash")):
        strip, guid, sheet = fields[name]
        body.append(f"  {field}:")
        body.append(strip_yaml(strip, guid, sheet, 4))
    write(ASSET_PATH, "\n".join(body) + "\n")


# ----------------------------------------------------------------------------------------------------------------------
# The families: one sheet each.


def build_flames(noise):
    strips = []
    for stacks in range(1, MAX_STACKS + 1):
        strips.append(Strip(f"flame{stacks}", [flame_frame(stacks, i, noise) for i in range(FLAME_FRAMES)]))
    return strips


def build_families():
    noise = Noise(7, period=0.9, feature=0.05)
    ring_noise = Noise(11, period=1.0, feature=0.08)
    burn = build_flames(noise) + [Strip("emberArc", [ember_arc_frame(i) for i in range(ARC_FRAMES)])]
    vent = [Strip("ventColumn", [vent_column_frame(i, noise) for i in range(VENT_FRAMES)]),
            Strip("ventEmbers", [vent_ember_frame(i) for i in range(VENT_EMBER_FRAMES)]),
            Strip("ventRing", [vent_ring_frame(i, ring_noise) for i in range(RING_FRAMES)])]
    forge = [Strip("shockwave", [shockwave_frame(i, ring_noise) for i in range(SHOCK_FRAMES)]),
             Strip("forgeSpark", [forge_spark_frame(i) for i in range(SPARK_FRAMES)]),
             Strip("forgeFlash", [forge_flash_frame(i) for i in range(FLASH_FRAMES)])]
    splash_noise = Noise(23, period=1.0, feature=0.08)
    splash_fine = Noise(31, period=1.0, feature=0.03)
    splash = [Strip("ventSplash", [vent_splash_frame(i, splash_noise, splash_fine, noise)
                                   for i in range(SPLASH_FRAMES)])]
    return {"VFX_Skill_Burn": burn, "VFX_Skill_Vent": vent, "VFX_Skill_Forge": forge, "VFX_Skill_VentSplash": splash}


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--frames", help="also save every frame as a PNG in this directory")
    args = parser.parse_args()

    os.makedirs(OUT_DIR, exist_ok=True)
    folder_meta = OUT_DIR + ".meta"
    write(folder_meta, FOLDER_META.format(guid=existing_guid(folder_meta)))
    if not os.path.exists(SCRIPT_META):
        raise SystemExit(f"{SCRIPT_META} is missing: the art set needs its script's GUID.")
    families = {}
    total = 0
    for name, strips in build_families().items():
        sheet = bleed(pack(strips))
        png = os.path.join(OUT_DIR, name + ".png")
        Image.fromarray(sheet, "RGBA").save(png, optimize=True)
        meta = png + ".meta"
        guid = existing_guid(meta)
        write(meta, TEXTURE_META.format(guid=guid, sprite_id=existing_sprite_id(meta), tpu=number(strips[0].tpu)))
        families[name] = dict(strips=strips, guid=guid, sheet=sheet)
        size = astc_bytes(sheet.shape[1], sheet.shape[0])
        total += size
        print(f"{name}.png {sheet.shape[1]}x{sheet.shape[0]}  ASTC 4x4 {size / 1024:.0f} KiB")
        if args.frames:
            os.makedirs(args.frames, exist_ok=True)
            for s in strips:
                for i, img in enumerate(s.images):
                    Image.fromarray(img, "RGBA").save(os.path.join(args.frames, f"{s.name}_{i}.png"))
    write_asset(families, existing_guid(SCRIPT_META))
    asset_meta = ASSET_PATH + ".meta"
    write(asset_meta, ASSET_META.format(guid=existing_guid(asset_meta)))
    print(f"total ASTC 4x4 {total / 1024:.0f} KiB ({total / 1048576:.2f} MiB)")


if __name__ == "__main__":
    main()
