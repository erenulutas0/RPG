"""The in-run HUD and the health bars, painted (art round 3).

The HUD was the plainest thing left on screen: two dark panels with a thin gold line, a flat red line for the hero's
health and a thin blue one for his experience, a flat orange line under the boss's name, flat rectangles for the
loadout row and the room pips, and over every enemy a flat red bar on a dark backing. This paints them in the family of
the game's painted UI (the ability icons, the forge panel, the run cards) and of the painted actors, in the palette of the
foundry: dark iron, warm gold trim, ember accents.

  - Panel: the top and bottom panels' forged frame (9-slice). A dark outline, a rounded iron rim lit from the top left,
    a gold inlay between two dark grooves and a small chamfer down to a blue-black iron face; at each corner a riveted
    plate with an ember set in it, glowing faintly. The face keeps the old panel's dark so every label reads as before.
  - Button: the pause button's plate (9-slice), a raised iron face in a gold rim.
  - Pip: the room pips (9-slice), a small rounded bar painted light so RunHud's gold and slate still tint it.
  - Slots: the loadout row's sockets (9-slice), recessed wells in an iron rim, warm for a weapon and cool for a tome,
    and a light strip under each that the row's accent colours tint.
  - Ability ring: the forged ring round the ability's icon (as large as the button: an outer gold rim, an iron band with
    eight gold rivets, a recessed channel and an inner gold rim over the icon well's edge), and the channel alone painted
    light for the cooldown's radial sweep, which the scene's cooldown colour tints.
  - Bars: the hero's health (a blood-red fill with a lit top, over a dark red channel), his experience (crystal blue, the
    colour of the experience gems) and the boss's (a heavier frame whose ends are furnace clamps with an ember grille
    and a spike, round a molten fill, white-gold at its top cooling to crimson), each with a frame laid over the fill's
    edges (9-slice), a light for the fill's end and, for the two that drain, a pale chunk for what a hit just took. Each
    fill is painted at the size of the window it is cropped in, so a fill amount crops it and never stretches it. The
    boss bar's frame keeps to its rect's bottom row (the camera's band starts there), so its fill is lifted inside it.
  - Boss name plate: a smoked band behind the boss's name over the floor.
  - Enemy bars: a forged backing (an outline, a lit iron rim, a dark channel) the size of the pixel bar (26 by 5 of its
    texels, so a painted bar takes the same room and the tests' budgets hold), its fill and its chunk, at the actors' 128
    texels a unit (a texel is about a phone pixel at the camera's framing); and the Cinder Mite's compact bar, whose frame
    is painted thicker across (6 texels a side instead of 4) so that, once EnemyLookView squeezes the bar to 0.65 by 0.8,
    it is as even as the full one.

Two sheets: Assets/_Project/Art/UI/Hud/UI_Hud.png (100 texels a unit, the canvas's reference, so a sprite is painted at
its size on the 1080-wide canvas) and Assets/_Project/Art/UI/Hud/UI_EnemyBar.png (128), each imported as Multiple sprites
with their rects, pivots and 9-slice borders in the .meta, bilinear, no mipmaps, ASTC 4x4 on Android, like the painted
effect sheets; and the two sets, HudArtSet.asset and EnemyBarArtSet.asset (Cryptforge.Art.HudArtSet, EnemyBarArtSet),
with every sprite and the bars' windows, outsets and lights' widths. Everything is painted 4x supersampled as a height
field lit from the top left (rims, inlays, rivets and grooves catch the light like the painted actors' metal), box-
filtered down in sRGB as the project blends, and exported straight-alpha with colour bled into the clear texels; the
packing and export are build_skill_vfx's. Deterministic: the same script paints the same sheets byte for byte. Existing
.meta GUIDs and sprite ids are kept, so a repaint never breaks a reference.

Usage:
    python Tools/ArtReview/build_hud_art.py                # paint the sheets, metas and sets
    python Tools/ArtReview/build_hud_art.py --frames DIR   # also save every sprite as a PNG, for review
"""
import argparse
import math
import os
import re
import uuid

import numpy as np
from PIL import Image

import build_skill_vfx as skill
from build_skill_vfx import hex_rgb, smoothstep

ROOT = skill.ROOT
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "UI", "Hud")
SCRIPTS = os.path.join(ROOT, "Assets", "_Project", "Scripts", "Art", "Unity")
HUD_SCRIPT_META = os.path.join(SCRIPTS, "HudArtSet.cs.meta")
BAR_SCRIPT_META = os.path.join(SCRIPTS, "EnemyBarArtSet.cs.meta")
HUD_SHEET, BAR_SHEET = "UI_Hud", "UI_EnemyBar"
HUD_TPU = 100.0  # the canvas's reference pixels per unit: a sprite paints at its size on the 1080-wide canvas
BAR_TPU = 128.0  # the painted actors' density

SS = 4  # supersampling per axis
# Lit from the top left, as the painted actors are (image order: x right, y down, z toward the viewer).
LIGHT = np.array([-0.36, -0.58, 0.73], np.float32)
LIGHT /= np.linalg.norm(LIGHT)
HALF = (LIGHT + np.array([0, 0, 1], np.float32))
HALF /= np.linalg.norm(HALF)
AMBIENT, DIFFUSE = 0.40, 0.80


# ----------------------------------------------------------------------------------------------------------------------
# Materials and the canvas. A sprite is painted in pixels (image order, row 0 at the top) as layers that each lay a
# material and a height where they cover; the canvas is then lit once from its height field.


class Mat:
    def __init__(self, albedo, spec=0.0, gloss=12.0, speccol="#FFFFFF"):
        self.albedo = hex_rgb(albedo) if isinstance(albedo, str) else np.asarray(albedo, np.float32)
        self.spec, self.gloss = spec, gloss
        self.speccol = hex_rgb(speccol)


OUTLINE = Mat("#06060A")
GROOVE = Mat("#08080C")
IRON = Mat("#30323C", spec=0.32, gloss=14, speccol="#C8D2E8")
IRON_DARK = Mat("#191B22", spec=0.18, gloss=10, speccol="#B8C2D8")
GOLD = Mat("#B57C33", spec=0.95, gloss=22, speccol="#FFE6A8")
GOLD_DARK = Mat("#7A4E1E", spec=0.6, gloss=18, speccol="#FFD890")
FACE = Mat("#0C1224")  # the panels' face: the old panel's navy, which the painted icons' own backgrounds match
BUTTON_FACE = Mat("#161A24", spec=0.12, gloss=8, speccol="#B8C2D8")
LIGHT_METAL = Mat("#DCDCDC", spec=0.45, gloss=16)  # painted light for a tint


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        xs = (np.arange(w * SS, dtype=np.float32) + 0.5) / SS
        ys = (np.arange(h * SS, dtype=np.float32) + 0.5) / SS
        self.X, self.Y = np.meshgrid(xs, ys)
        shape = self.X.shape
        self.height = np.zeros(shape, np.float32)
        self.albedo = np.zeros(shape + (3,), np.float32)
        self.spec = np.zeros(shape, np.float32)
        self.gloss = np.full(shape, 12.0, np.float32)
        self.speccol = np.ones(shape + (3,), np.float32)
        self.alpha = np.zeros(shape, np.float32)
        self.emit = np.zeros(shape + (3,), np.float32)
        self.rgba = None

    def lay(self, cover, mat, height=None, albedo=None):
        """The material (and its height, if given) where cover reaches, over what is there."""
        c = np.clip(cover, 0.0, 1.0).astype(np.float32)
        c3 = c[..., None]
        colour = mat.albedo if albedo is None else albedo
        self.albedo = colour * c3 + self.albedo * (1 - c3)
        if height is not None:
            self.height = height * c + self.height * (1 - c)
        self.spec = mat.spec * c + self.spec * (1 - c)
        self.gloss = mat.gloss * c + self.gloss * (1 - c)
        self.speccol = mat.speccol * c3 + self.speccol * (1 - c3)
        self.alpha = c + self.alpha * (1 - c)

    def darken(self, amount):
        """Shades what is laid (an occlusion or a cast shadow), 0 none to 1 black."""
        self.albedo *= (1 - np.clip(amount, 0, 1))[..., None]

    def glow(self, colour, amount):
        """Light added on top, unlit (an ember's heat)."""
        self.emit += hex_rgb(colour) * np.clip(amount, 0, None)[..., None]

    def shade(self, relief=1.0):
        gy, gx = np.gradient(self.height * relief, 1.0 / SS)
        n = np.stack([-gx, -gy, np.ones_like(gx)], -1)
        n /= np.linalg.norm(n, axis=-1, keepdims=True)
        diffuse = np.clip(n @ LIGHT, 0, 1)
        spec = self.spec * np.clip(n @ HALF, 0, 1) ** self.gloss
        rgb = self.albedo * (AMBIENT + DIFFUSE * diffuse)[..., None] + self.speccol * spec[..., None] + self.emit
        self.rgba = np.concatenate([np.clip(rgb, 0, 1) * self.alpha[..., None], self.alpha[..., None]], -1)
        return self

    def flat(self):
        """Unlit: the albedo as laid, for fills and lights whose shading is painted in."""
        rgb = self.albedo + self.emit
        self.rgba = np.concatenate([np.clip(rgb, 0, 1) * self.alpha[..., None], self.alpha[..., None]], -1)
        return self

    def over(self, rgb, alpha):
        """Paints straight colour over the lit result (lip shadows, glints)."""
        a = np.clip(alpha, 0, 1)[..., None]
        self.rgba[..., :3] = np.asarray(rgb, np.float32) * a + self.rgba[..., :3] * (1 - a)
        self.rgba[..., 3:] = a + self.rgba[..., 3:] * (1 - a)

    def image(self):
        h, w = self.h, self.w
        premultiplied = self.rgba.reshape(h, SS, w, SS, 4).mean(axis=(1, 3))
        return skill.to_image(premultiplied)


# Signed distances (negative inside), in pixels.


def sd_box(X, Y, x0, y0, x1, y1, r=0.0):
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    hx, hy = (x1 - x0) / 2 - r, (y1 - y0) / 2 - r
    qx, qy = np.abs(X - cx) - hx, np.abs(Y - cy) - hy
    outside = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0))
    return outside + np.minimum(np.maximum(qx, qy), 0) - r


def sd_chamfer(X, Y, x0, y0, x1, y1, c):
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    qx, qy = np.abs(X - cx) - (x1 - x0) / 2, np.abs(Y - cy) - (y1 - y0) / 2
    return np.maximum(np.maximum(qx, qy), (qx + qy + c) / math.sqrt(2))


def sd_circle(X, Y, cx, cy, r):
    return np.hypot(X - cx, Y - cy) - r


def inside(sd):
    """Coverage of a distance field, anti-aliased over a supersample."""
    return np.clip(0.5 - sd * SS, 0, 1)


def band(d, d0, d1):
    """Coverage of the band d0 <= d < d1 of an inward distance d."""
    return np.clip((d - d0) * SS + 0.5, 0, 1) * np.clip((d1 - d) * SS + 0.5, 0, 1)


def dome(d, d0, d1, height):
    """A rounded profile across a band: 0 at its edges, height at its middle."""
    t = np.clip((d - d0) / (d1 - d0), 0, 1)
    return height * np.sin(np.pi * t)


def across_rim(from_outline, to_window, height):
    """A rounded profile across a rim of any width: 0 at its outline and at the window it frames, height between."""
    s = np.clip(from_outline / np.maximum(from_outline + to_window, 1e-3), 0, 1)
    return height * np.sin(np.pi * s)


def ramp(d, d0, d1, h0, h1):
    t = np.clip((d - d0) / (d1 - d0), 0, 1)
    return h0 + (h1 - h0) * t


def rivet(cv, cx, cy, r, base, mat=GOLD, rise=None):
    """A domed rivet standing on base height, with its outline."""
    sd = sd_circle(cv.X, cv.Y, cx, cy, r)
    cv.lay(inside(sd - 0.7) * (1 - inside(sd)), OUTLINE, height=np.full_like(sd, base))
    t = np.clip(-sd / r, 0, 1)
    cv.lay(inside(sd), mat, height=base + (rise if rise is not None else r * 0.55) * np.sqrt(t * (2 - t)))


def ember(cv, cx, cy, r, base, heat=1.0):
    """An ember set in metal: a dark-red stone lit from inside, hot at its heart, with a glint and a faint glow."""
    sd = sd_circle(cv.X, cv.Y, cx, cy, r)
    cv.lay(inside(sd - 0.8) * (1 - inside(sd)), OUTLINE, height=np.full_like(sd, base))
    t = np.clip(-sd / r, 0, 1)
    cv.lay(inside(sd), Mat("#5A0E08", spec=0.9, gloss=40), height=base + r * 0.45 * np.sqrt(t * (2 - t)))
    d = np.hypot(cv.X - cx, cv.Y - cy) / r
    cv.glow("#E8561A", heat * 0.9 * np.clip(1 - d, 0, 1) ** 0.8 * inside(sd))
    cv.glow("#FFC45A", heat * 0.8 * np.clip(1 - d * 1.7, 0, 1) ** 1.4 * inside(sd))
    # The heat on the metal round it.
    cv.glow("#C8401A", heat * 0.20 * np.exp(-((d - 1) * r / (r * 1.2)) ** 2) * (1 - inside(sd)))


# ----------------------------------------------------------------------------------------------------------------------
# The sprites. Each returns (image, border (left, bottom, right, top) in texels, pivot (x, y) normalised).


PANEL_W, PANEL_H, PANEL_BORDER = 128, 104, 28
PANEL_FRAME = 13.6  # the frame's depth into the panel: the face starts here


def panel():
    """The panels' forged frame. The pause button stands 16 texels inside the top panel's right edge and 22 below its top,
    so the frame and its corner plates keep within 14 and 20."""
    cv = Canvas(PANEL_W, PANEL_H)
    X, Y = cv.X, cv.Y
    d = -sd_chamfer(X, Y, 0.25, 0.25, PANEL_W - 0.25, PANEL_H - 0.25, 5.0)
    # Outside in: an outline, a gilded outer edge, the iron rim, a groove, the gold inlay, a groove and a chamfer down
    # to the face - the old panel's two gold lines, with forged iron between them.
    cv.lay(band(d, 0.0, 1.1), OUTLINE, height=np.full_like(d, 0.2))
    cv.lay(band(d, 1.1, 2.9), GOLD, height=0.2 + dome(d, 1.1, 2.9, 0.9))
    cv.lay(band(d, 2.9, 7.4), IRON, height=0.6 + dome(d, 2.9, 7.4, 2.0))
    cv.lay(band(d, 7.4, 8.1), GROOVE, height=np.full_like(d, 0.1))
    cv.lay(band(d, 8.1, 11.3), GOLD, height=dome(d, 8.1, 11.3, 1.4))
    cv.lay(band(d, 11.3, 12.1), GROOVE, height=np.zeros_like(d))
    cv.lay(band(d, 12.1, PANEL_FRAME), IRON_DARK, height=ramp(d, 12.1, PANEL_FRAME, 0.7, 0.0))
    # The face: the old panel's navy exactly, flat and unlit, so the coin, the relic and every label sit on it as before
    # (the painted icons carry that navy as their background); shaded only in a narrow band where the frame stands over
    # it, which ends before the relic badge's foot (19 texels in from the panel's bottom edge).
    cv.lay(band(d, PANEL_FRAME, 1e9), FACE, height=np.zeros_like(d))
    occlusion = 0.42 * (1 - smoothstep(PANEL_FRAME, PANEL_FRAME + 4.5, d)) * (d >= PANEL_FRAME - 0.5)
    cv.darken(occlusion)
    # The corner plates: riveted iron in a gold edge, each with an ember, casting a soft shadow down-right.
    for cx, cy in ((10.5, 10.5), (PANEL_W - 10.5, 10.5), (10.5, PANEL_H - 10.5), (PANEL_W - 10.5, PANEL_H - 10.5)):
        plate = sd_chamfer(X, Y, cx - 9.5, cy - 9.5, cx + 9.5, cy + 9.5, 3.6)
        shadow = sd_chamfer(X, Y, cx - 9.0, cy - 8.5, cx + 10.5, cy + 11, 3.6)
        cv.darken(0.55 * np.clip(1 - (shadow + 0.5) / 2.2, 0, 1) * (1 - inside(plate)))
        pd = -plate
        cv.lay(band(pd, 0.0, 1.1), OUTLINE, height=np.full_like(pd, 1.0))
        cv.lay(band(pd, 1.1, 3.1), GOLD, height=1.4 + dome(pd, 1.1, 3.1, 1.0))
        cv.lay(band(pd, 3.1, 1e9), IRON, height=np.full_like(pd, 2.4) + 0.25 * smoothstep(3.1, 8, pd))
        ember(cv, cx, cy, 3.5, 2.6)
    return cv.shade().image(), (PANEL_BORDER,) * 4, (0.5, 0.5)


BUTTON_SIZE, BUTTON_BORDER = 72, 24


def button():
    """The pause button's plate: a raised iron face in a gold rim, with a gold rivet in each corner."""
    cv = Canvas(BUTTON_SIZE, BUTTON_SIZE)
    X, Y = cv.X, cv.Y
    s = BUTTON_SIZE
    d = -sd_chamfer(X, Y, 0.25, 0.25, s - 0.25, s - 0.25, 7.0)
    cv.lay(band(d, 0.0, 1.3), OUTLINE, height=np.full_like(d, 0.2))
    cv.lay(band(d, 1.3, 5.2), GOLD, height=0.2 + dome(d, 1.3, 5.2, 1.7))
    cv.lay(band(d, 5.2, 6.2), GROOVE, height=np.full_like(d, 0.2))
    cv.lay(band(d, 6.2, 9.0), IRON, height=ramp(d, 6.2, 9.0, 0.6, 2.6))
    face = BUTTON_FACE.albedo * (1 + 0.18 * (1 - Y / s))[..., None]
    cv.lay(band(d, 9.0, 1e9), BUTTON_FACE, height=2.6 + 0.4 * smoothstep(9, 20, d), albedo=face)
    for cx, cy in ((11.5, 11.5), (s - 11.5, 11.5), (11.5, s - 11.5), (s - 11.5, s - 11.5)):
        rivet(cv, cx, cy, 2.1, 2.7)
    return cv.shade().image(), (BUTTON_BORDER,) * 4, (0.5, 0.5)


def pip():
    """A room pip: a rounded bar painted light, so RunHud's colours tint it; 8 texels tall, as the pips are."""
    cv = Canvas(24, 8)
    X, Y = cv.X, cv.Y
    sd = sd_box(X, Y, 0.3, 0.3, 23.7, 7.7, 3.4)
    d = -sd
    cv.lay(band(d, 0, 1.0), Mat("#3A3A3A"), height=np.zeros_like(d))
    cv.lay(band(d, 1.0, 1e9), LIGHT_METAL, height=dome(np.clip(d, 0, 3.0), 1.0, 5.0, 1.6))
    return cv.shade().image(), (5, 0, 5, 0), (0.5, 0.5)


SLOT_W, SLOT_H, SLOT_BORDER = 48, 44, 14


def slot(warm):
    """A loadout socket: a recessed well in an iron rim, warm iron for a weapon, cool for a tome."""
    cv = Canvas(SLOT_W, SLOT_H)
    X, Y = cv.X, cv.Y
    d = -sd_chamfer(X, Y, 0.25, 0.25, SLOT_W - 0.25, SLOT_H - 0.25, 4.0)
    rim = Mat("#3A3430" if warm else "#2E3442", spec=0.3, gloss=14, speccol="#D8D0C8" if warm else "#C8D4EC")
    well = Mat("#1B130E" if warm else "#0E1322")
    cv.lay(band(d, 0.0, 1.2), OUTLINE, height=np.full_like(d, 1.2))
    cv.lay(band(d, 1.2, 4.0), rim, height=1.2 + dome(d, 1.2, 4.0, 1.2))
    cv.lay(band(d, 4.0, 5.0), GOLD_DARK, height=ramp(d, 4.0, 5.0, 1.0, 0.6))
    cv.lay(band(d, 5.0, 1e9), well, height=np.zeros_like(d) + ramp(d, 5.0, 7.0, 0.5, 0.0))
    occlusion = 0.6 * (1 - smoothstep(5.0, 11.0, d)) * (d > 4.5)
    # The well's top lies in the rim's shadow; its bottom edge catches a little light.
    occlusion *= 1 + 0.6 * (Y < SLOT_H / 2)
    cv.darken(np.clip(occlusion, 0, 0.85))
    cv.shade()
    return cv.image(), (SLOT_BORDER,) * 4, (0.5, 0.5)


def slot_accent():
    """The strip under a socket, painted light for the row's accent colours: a lit line with soft ends."""
    cv = Canvas(16, 4)
    X, Y = cv.X, cv.Y
    across = np.clip(1 - np.abs(Y - 2.0) / 2.0, 0, 1)
    cv.lay(inside(sd_box(X, Y, 0.5, 0.0, 15.5, 4.0, 1.6)), Mat("#FFFFFF"),
           albedo=(0.62 + 0.38 * across ** 0.6)[..., None] * np.ones(3, np.float32))
    return cv.flat().image(), (5, 0, 5, 0), (0.5, 0.5)


RING = 234  # the ability button's size
WELL = 77.0  # the icon well's radius (154 across): the ring's inner lip covers its edge


def ability_ring():
    cv = Canvas(RING, RING)
    c = RING / 2
    r = np.hypot(cv.X - c, cv.Y - c)
    out = 115.6 - r  # inward distance from the outer edge
    # A soft shadow beyond the outer edge, onto the panel.
    shadow = np.clip(1 - (r - 115.0) / 2.2, 0, 1) * (r > 115.0)
    cv.lay(band(out, 0.0, 1.3), OUTLINE, height=np.full_like(r, 0.2))
    cv.lay(band(out, 1.3, 6.6), GOLD, height=0.2 + dome(out, 1.3, 6.6, 2.2))
    cv.lay(band(out, 6.6, 7.4), GROOVE, height=np.full_like(r, 0.2))
    cv.lay(band(out, 7.4, 20.6), IRON, height=0.4 + dome(out, 7.4, 20.6, 3.0))
    cv.lay(band(out, 20.6, 21.4), GROOVE, height=np.full_like(r, -0.2))
    # The channel the cooldown sweeps: recessed dark iron.
    cv.lay(band(out, 21.4, 30.6), IRON_DARK, height=-0.6 - dome(out, 21.4, 30.6, 0.5))
    channel = band(out, 21.4, 30.6)
    cv.darken(0.35 * channel * (1 - smoothstep(21.4, 25, out)))
    cv.lay(band(out, 30.6, 31.4), GROOVE, height=np.full_like(r, -0.2))
    cv.lay(band(out, 31.4, 36.2), GOLD, height=dome(out, 31.4, 36.2, 1.6))
    lip_end = 115.6 - (WELL - 1.4)
    cv.lay(band(out, 36.2, lip_end), OUTLINE, height=np.full_like(r, -0.4))
    # Eight gold rivets on the iron band, between the compass points.
    for k in range(8):
        a = math.radians(22.5 + 45 * k)
        rivet(cv, c + 101.6 * math.cos(a), c + 101.6 * math.sin(a), 3.0, 3.0)
    # Four embers at the compass points.
    for k in range(4):
        a = math.radians(90 * k)
        ember(cv, c + 101.6 * math.cos(a), c + 101.6 * math.sin(a), 3.4, 3.2)
    cv.shade()
    cv.over((0.0, 0.0, 0.0), 0.45 * shadow ** 1.5 * (r < 117))
    return cv.image(), (0, 0, 0, 0), (0.5, 0.5)


def ability_cooldown():
    """The ring's channel alone, painted light with soft edges, for the cooldown's radial sweep."""
    cv = Canvas(RING, RING)
    c = RING / 2
    r = np.hypot(cv.X - c, cv.Y - c)
    out = 115.6 - r
    across = np.clip((out - 21.4) / 9.2, 0, 1)
    value = 0.78 + 0.22 * np.sin(np.pi * across)
    cover = np.clip((out - 21.0) / 1.2, 0, 1) * np.clip((31.0 - out) / 1.2, 0, 1) * 0.92
    cv.lay(cover, Mat("#FFFFFF"), albedo=value[..., None] * np.ones(3, np.float32))
    return cv.flat().image(), (0, 0, 0, 0), (0.5, 0.5)


def name_plate():
    """A smoked band behind the boss's name: darkest round the middle, gone at its ends and edges."""
    cv = Canvas(128, 42)
    u = (cv.X / 128 - 0.5)
    v = (cv.Y / 42 - 0.5)
    alpha = 0.62 * (1 - smoothstep(0.16, 0.5, np.abs(u))) * (1 - smoothstep(0.26, 0.5, np.abs(v)))
    cv.lay(alpha, Mat("#05060A"))
    return cv.flat().image(), (0, 0, 0, 0), (0.5, 0.5)


# The bars. A window is (left, bottom, right, top) offsets from the bar's own rect (positive outward) and an outset
# how far the frame reaches past the window, in canvas units = texels.

HERO_W, HERO_H = 698, 22
XP_W, XP_H = 698, 8
BOSS_W, BOSS_H = 936, 16
HERO_OUTSET = (12, 6, 12, 6)
XP_OUTSET = (7, 4, 7, 4)
BOSS_OUTSET = (48, 4, 48, 30)  # the caps reach the top panel's width (1032), and rise 30 over the fill
# The boss bar's rect is 14 tall and its frame may not reach below it (the camera's band starts at its foot), so its fill
# is lifted 4 inside it, over the frame's foot, and grows 6 above it to 16.
BOSS_WINDOW = (0, -4, 0, 6)
LIP = 2.0


def bar_frame(outset, window_height, lip, cap_rivet, rim_gold=True):
    """A frame laid over a window's edges: an outline, a lit iron rim, a gold line round the window and, inside it, a lip
    of shadow under the top edge and a glint along the bottom; a rivet in each end cap. As tall as it is drawn, so its
    caps never stretch."""
    left, bottom, right, top = outset
    wide = left + right + 40
    tall = top + bottom + window_height
    cv = Canvas(wide, tall)
    X, Y = cv.X, cv.Y
    wx0, wy0, wx1, wy1 = left, top, wide - right, tall - bottom  # the window, image order
    d = -sd_chamfer(X, Y, 0.25, 0.25, wide - 0.25, tall - 0.25, min(4.0, top * 0.7))
    dw = sd_box(X, Y, wx0, wy0, wx1, wy1)  # outside the window positive
    frame = inside(-dw)  # everything outside the window
    cv.lay(frame * band(d, 0, 1.2), OUTLINE, height=np.full_like(d, 0.2))
    cv.lay(frame * band(d, 1.2, 1e9), IRON, height=0.2 + across_rim(d - 1.2, dw, 1.6))
    if rim_gold:
        cv.lay(frame * np.clip(1 - np.abs(dw - 1.6) / 0.8, 0, 1), GOLD, height=0.6 + dome(dw, 0.8, 2.4, 0.7))
    cv.lay(frame * np.clip(1 - dw / 0.8, 0, 1), GROOVE, height=np.zeros_like(d))
    for cx in (left / 2.0 + 0.5, wide - right / 2.0 - 0.5):
        if cap_rivet:
            rivet(cv, cx, tall / 2.0, cap_rivet, 1.4)
    cv.shade()
    # The lip inside the window: the top edge's shadow, the left and right ends' shadow and a faint glint at the foot.
    win = inside(dw)
    depth_top = Y - wy0
    depth_side = np.minimum(X - wx0, wx1 - X)
    depth_bottom = wy1 - Y
    shade_alpha = win * np.maximum(0.75 * np.clip(1 - depth_top / lip, 0, 1) ** 1.3,
                                   0.6 * np.clip(1 - depth_side / lip, 0, 1) ** 1.3)
    cv.over((0.0, 0.0, 0.0), shade_alpha)
    cv.over((1.0, 0.95, 0.85), win * 0.16 * np.clip(1 - depth_bottom / (lip * 0.6), 0, 1))
    border = (left + int(lip), bottom + int(lip), right + int(lip), top + int(lip))
    return cv.image(), border, (0.5, 0.5)


def sd_horn(X, Y, points, r0, r1):
    """A tapered stroke along a quadratic curve through three control points, r0 thick at the first and r1 at the last:
    a horn, a spike."""
    out = np.full(X.shape, 1e9, np.float32)
    pts = np.asarray(points, np.float32)
    steps = 48
    for k in range(steps + 1):
        t = k / steps
        p = (1 - t) ** 2 * pts[0] + 2 * (1 - t) * t * pts[1] + t * t * pts[2]
        out = np.minimum(out, np.hypot(X - p[0], Y - p[1]) - (r0 + (r1 - r0) * t))
    return out


def boss_frame():
    """The boss bar's frame: a heavier rim round the window and, at each end, a horned furnace clamp - a tall iron block
    in a gold edge with an ember grille, a horn curling up and out from its top and a spike thrust outward - rising well
    above the bar (where the name is not) and never below its rect."""
    left, bottom, right, top = BOSS_OUTSET
    wide = left + right + 40
    tall = top + bottom + BOSS_H
    cv = Canvas(wide, tall)
    X, Y = cv.X, cv.Y
    wx0, wy0, wx1, wy1 = left, top, wide - right, tall - bottom
    rim_top = wy0 - 6.0  # the rim stands 6 above the window (the name's lowest texel is 2 above that) and 4 below it
    rim = sd_chamfer(X, Y, wx0 - 10, rim_top, wx1 + 10, tall - 0.25, 3.0)
    dw = sd_box(X, Y, wx0, wy0, wx1, wy1)
    frame = inside(-dw)
    d = -rim
    cv.lay(frame * band(d, 0, 1.5), OUTLINE, height=np.full_like(d, 0.2))
    cv.lay(frame * band(d, 1.5, 1e9), IRON, height=0.2 + across_rim(d - 1.5, dw, 1.8))
    cv.lay(frame * inside(rim) * np.clip(1 - np.abs(dw - 1.7) / 0.9, 0, 1), GOLD, height=0.8 + dome(dw, 0.8, 2.6, 0.8))
    cv.lay(frame * inside(rim) * np.clip(1 - dw / 0.8, 0, 1), GROOVE, height=np.zeros_like(d))
    for side in (-1, 1):
        # Mirrored in shape, lit from the same top left. t runs outward from the window's end.
        edge = wx0 if side < 0 else wx1

        def ox(t):
            return edge + side * t

        x0, x1 = sorted((ox(-2.0), ox(37.0)))
        horn_path = [(ox(25.0), 13.0), (ox(37.0), 9.0), (ox(44.5), 1.2)]
        spike_path = [(ox(34.0), 32.0), (ox(40.0), 32.0), (ox(46.5), 32.0)]
        block = sd_chamfer(X, Y, x0, 10.0, x1, tall - 0.25, 5.5)
        horn = sd_horn(X, Y, horn_path, 6.2, 1.0)
        spike = sd_horn(X, Y, spike_path, 6.0, 0.8)
        shape = np.minimum(np.minimum(block, horn), spike)
        # A soft shadow cast down-right onto the rim and the floor.
        sx, sy = X - 1.2, Y - 1.6
        cast = np.minimum(np.minimum(sd_chamfer(sx, sy, x0, 10.0, x1, tall + 2, 5.5), sd_horn(sx, sy, horn_path, 6.2, 1.0)),
                          sd_horn(sx, sy, spike_path, 6.0, 0.8))
        cv.darken(0.55 * np.clip(1 - (cast + 0.5) / 2.2, 0, 1) * (1 - inside(shape)))
        # The horn and the spike: forged iron, rounded, behind the block, gilded along their outer half so they read on
        # the dark floor.
        for part, root, tip in ((horn, horn_path[0], horn_path[2]), (spike, spike_path[0], spike_path[2])):
            pd = -part
            cv.lay(band(pd, 0, 1.4), OUTLINE, height=np.full_like(pd, 0.6))
            cv.lay(band(pd, 1.4, 1e9), IRON, height=0.8 + 2.0 * smoothstep(1.4, 5.0, pd))
            reach = math.hypot(tip[0] - root[0], tip[1] - root[1])
            outward = np.clip((reach - np.hypot(X - tip[0], Y - tip[1])) / reach, 0, 1)
            cv.lay(band(pd, 1.4, 1e9) * smoothstep(0.35, 0.55, outward), GOLD, height=0.8 + 2.0 * smoothstep(1.4, 5.0, pd))
        bd = -block
        cv.lay(band(bd, 0, 1.5), OUTLINE, height=np.full_like(bd, 0.6))
        cv.lay(band(bd, 1.5, 4.0), GOLD, height=0.8 + dome(bd, 1.5, 4.0, 1.4))
        cv.lay(band(bd, 4.0, 4.8), GROOVE, height=np.full_like(bd, 0.9))
        cv.lay(band(bd, 4.8, 1e9), IRON, height=1.6 + 1.0 * smoothstep(4.8, 9.0, bd))
        # The furnace mouth: an arched opening onto the fire, white-hot at its floor, barred by two iron rods.
        mx0, mx1 = sorted((ox(7.0), ox(28.0)))
        crown = 17.0
        arch = sd_circle(X, Y, (mx0 + mx1) / 2, crown + (mx1 - mx0) / 2, (mx1 - mx0) / 2)
        mouth = np.maximum(sd_box(X, Y, mx0, crown, mx1, 43.5, 0.0), np.where(Y < crown + (mx1 - mx0) / 2, arch, -1e9))
        cv.lay(inside(mouth - 1.3) * (1 - inside(mouth)), OUTLINE, height=np.full_like(bd, 1.2))
        depth = np.clip((Y - crown) / (43.5 - crown), 0, 1)  # 0 at the arch's crown, 1 at the mouth's floor
        fire = inside(mouth)
        cv.lay(fire, Mat("#2A0604"), height=np.full_like(bd, 0.2))
        cv.glow("#B8301A", 0.9 * fire * (0.35 + 0.65 * depth))
        cv.glow("#F08A2A", 0.9 * fire * smoothstep(0.35, 1.0, depth))
        cv.glow("#FFE6A0", 0.8 * fire * smoothstep(0.75, 1.0, depth))
        # The glow spilling onto the iron round the mouth.
        cv.glow("#C8401A", 0.28 * np.exp(-(np.maximum(mouth, 0) / 2.6) ** 2) * (1 - fire) * inside(block))
        for bx in (mx0 + (mx1 - mx0) / 3, mx0 + 2 * (mx1 - mx0) / 3):
            bar_sd = sd_box(X, Y, bx - 1.2, crown - 3, bx + 1.2, 44.5, 0.6)
            cv.lay(inside(bar_sd + 0.8) * (1 - inside(bar_sd)) * fire, OUTLINE, height=np.full_like(bd, 1.6))
            cv.lay(inside(bar_sd) * inside(mouth - 1.0), IRON, height=1.6 + dome(-bar_sd, 0, 1.1, 0.8))
        for rx, ry in ((ox(32.5), 15.0), (ox(32.5), 45.5), (ox(2.5), 15.0), (ox(2.5), 45.5)):
            rivet(cv, rx, ry, 1.9, 2.7)
    cv.shade()
    win = inside(dw)
    shade_alpha = win * np.maximum(0.75 * np.clip(1 - (Y - wy0) / LIP, 0, 1) ** 1.3,
                                   0.6 * np.clip(1 - np.minimum(X - wx0, wx1 - X) / LIP, 0, 1) ** 1.3)
    cv.over((0.0, 0.0, 0.0), shade_alpha)
    cv.over((1.0, 0.9, 0.7), win * 0.16 * np.clip(1 - (wy1 - Y) / (LIP * 0.6), 0, 1))
    border = (left + int(LIP), bottom + int(LIP), right + int(LIP), top + int(LIP))
    return cv.image(), border, (0.5, 0.5)


def trough(width, height, colour, glow=None):
    """The channel a fill drains into: dark, its top in the rim's shadow, a little lighter at its foot."""
    cv = Canvas(width, height)
    Y = cv.Y
    t = Y / height
    value = 0.55 + 0.45 * smoothstep(0.0, 1.0, t)
    tone = hex_rgb(colour) * value[..., None]
    cv.lay(np.ones_like(Y), Mat(colour), albedo=tone)
    if glow:
        cv.glow(glow, 0.10 * smoothstep(0.5, 1.0, t))
    return cv.flat().image(), (4, 0, 4, 0), (0.5, 0.5)


def vertical(cv, stops):
    """A vertical ramp through (t, colour) stops, t from the top (0) to the foot (1)."""
    t = cv.Y / cv.h
    ts = [s[0] for s in stops]
    cols = np.stack([hex_rgb(s[1]) for s in stops])
    return np.stack([np.interp(t, ts, cols[:, k]) for k in range(3)], -1).astype(np.float32)


def periodic_noise(seed, w, h, feature_x, feature_y):
    """Smooth noise in [-1, 1] over the canvas, stretched along x (molten flow, a liquid's sheen)."""
    rng = np.random.default_rng(seed)
    gw, gh = max(2, int(w / feature_x) + 3), max(2, int(h / feature_y) + 3)
    grid = rng.standard_normal((gh, gw)).astype(np.float32)
    img = Image.fromarray(grid, "F").resize((w * SS + 2 * SS * int(feature_x), h * SS + 2 * SS * int(feature_y)),
                                            Image.BICUBIC)
    a = np.asarray(img, np.float32)[SS * int(feature_y):SS * int(feature_y) + h * SS,
                                    SS * int(feature_x):SS * int(feature_x) + w * SS]
    return a / (np.abs(a).max() + 1e-6)


def hero_fill():
    cv = Canvas(HERO_W, HERO_H)
    base = vertical(cv, [(0.0, "#FF8F7E"), (0.16, "#FF6656"), (0.34, "#E43A31"), (0.66, "#BE2129"),
                         (0.86, "#8A1220"), (1.0, "#5C0B18")])
    sheen = periodic_noise(41, HERO_W, HERO_H, 46, 30)
    base = base * (1 + 0.06 * sheen)[..., None]
    # A wet highlight riding the upper third.
    hl = np.exp(-((cv.Y - 5.2) / 1.6) ** 2) * (0.55 + 0.25 * periodic_noise(43, HERO_W, HERO_H, 70, 40))
    base = base + hex_rgb("#FFC8B8") * (0.35 * np.clip(hl, 0, 1))[..., None]
    cv.lay(np.ones_like(cv.Y), Mat("#FFFFFF"), albedo=base)
    return cv.flat().image(), (0, 0, 0, 0), (0.5, 0.5)


def xp_fill():
    cv = Canvas(XP_W, XP_H)
    base = vertical(cv, [(0.0, "#C6ECFF"), (0.25, "#6FC0FF"), (0.5, "#3A8CF0"), (0.8, "#2459C8"), (1.0, "#16348A")])
    sheen = periodic_noise(47, XP_W, XP_H, 38, 20)
    base = base * (1 + 0.07 * sheen)[..., None]
    rng = np.random.default_rng(53)
    # A few glints in the crystal, soft, so the bar reads as gem-light rather than as dashes.
    for _ in range(14):
        x = rng.uniform(4, XP_W - 4)
        y = rng.uniform(2.4, 3.6)
        s = np.exp(-(((cv.X - x) / 2.2) ** 2 + ((cv.Y - y) / 0.9) ** 2))
        base = base + (0.32 * s)[..., None] * hex_rgb("#E8F8FF")
    cv.lay(np.ones_like(cv.Y), Mat("#FFFFFF"), albedo=np.clip(base, 0, 1))
    return cv.flat().image(), (0, 0, 0, 0), (0.5, 0.5)


def boss_fill():
    cv = Canvas(BOSS_W, BOSS_H)
    base = vertical(cv, [(0.0, "#FFF0B0"), (0.14, "#FFC45A"), (0.32, "#F78A26"), (0.56, "#DC4E1C"),
                         (0.8, "#A82418"), (1.0, "#621010")])
    flow = periodic_noise(61, BOSS_W, BOSS_H, 30, 7)
    fine = periodic_noise(67, BOSS_W, BOSS_H, 9, 4)
    heat = 0.5 + 0.5 * flow
    base = base * (0.86 + 0.24 * heat + 0.06 * fine)[..., None]
    # Dark crust drifting on the molten metal, mostly low on it.
    crust = smoothstep(0.55, 0.85, 0.5 + 0.5 * periodic_noise(71, BOSS_W, BOSS_H, 22, 6)) * smoothstep(0.3, 0.9, cv.Y / BOSS_H)
    base = base * (1 - 0.45 * crust)[..., None]
    cv.lay(np.ones_like(cv.Y), Mat("#FFFFFF"), albedo=np.clip(base, 0, 1))
    return cv.flat().image(), (0, 0, 0, 0), (0.5, 0.5)


def trail(height, top, foot):
    """The pale chunk a hit takes, under the fill; stretched across the window, so plain across."""
    cv = Canvas(32, height)
    base = vertical(cv, [(0.0, top), (0.3, top), (1.0, foot)])
    cv.lay(np.ones_like(cv.Y), Mat("#FFFFFF"), albedo=base)
    return cv.flat().image(), (0, 0, 0, 0), (0.5, 0.5)


def glint(colour):
    """The light at a fill's end: a hot line with a soft glow, fading at its ends."""
    w, h = 16, 32
    cv = Canvas(w, h)
    dx = np.abs(cv.X - w / 2)
    along = np.clip(1 - np.abs(cv.Y - h / 2) / (h / 2), 0, 1) ** 0.6
    core = np.clip(1 - dx / 1.3, 0, 1)
    halo = np.exp(-(dx / 3.2) ** 2) * 0.55
    alpha = np.clip(np.maximum(core, halo) * along, 0, 1)
    col = np.where((core > halo)[..., None], hex_rgb("#FFFFFF"), hex_rgb(colour))
    cv.lay(alpha, Mat("#FFFFFF"), albedo=col)
    return cv.flat().image(), (0, 0, 0, 0), (0.5, 0.5)


# The enemy bars, at 128 texels a unit: the pixel bar's 26 by 5 texels of 32 a unit.
EB_W, EB_H = 104, 20
EB_FILL_H = 12


def enemy_back(side):
    """The backing: an outline, a lit iron rim (side texels at the ends, 4 along the top and foot) and a dark channel
    the fill covers exactly, its top in the rim's shadow."""
    cv = Canvas(EB_W, EB_H)
    X, Y = cv.X, cv.Y
    top = (EB_H - EB_FILL_H) / 2
    wx0, wy0, wx1, wy1 = side, top, EB_W - side, EB_H - top
    d = -sd_box(X, Y, 0.25, 0.25, EB_W - 0.25, EB_H - 0.25, 3.2)
    dw = sd_box(X, Y, wx0, wy0, wx1, wy1, 0.6)
    frame = inside(-dw)
    # Dark iron with a lit edge: the dark reads on the pale stone and the orange lava, the lit edge on a dark crowd.
    rim = Mat("#2A2830", spec=0.85, gloss=10, speccol="#F0E2CC")
    cv.lay(frame * band(d, 0, 1.4), OUTLINE, height=np.full_like(d, 0.1))
    cv.lay(frame * band(d, 1.4, 1e9), rim, height=0.1 + across_rim(d - 1.4, dw, 1.3))
    cv.lay(frame * np.clip(1 - dw / 0.7, 0, 1), GROOVE, height=np.zeros_like(d))
    channel = vertical(cv, [(0.0, "#0B0507"), (0.45, "#170A0D"), (1.0, "#24101A")])
    cv.lay(inside(dw), Mat("#FFFFFF"), height=np.full_like(d, -0.3), albedo=channel)
    cv.shade()
    return cv.image(), (0, 0, 0, 0), (0.5, 0.5)


def enemy_fill(width):
    cv = Canvas(width, EB_FILL_H)
    base = vertical(cv, [(0.0, "#A83A36"), (0.07, "#FF8C7C"), (0.22, "#FF5E50"), (0.42, "#E2342F"),
                         (0.72, "#B71F2A"), (0.86, "#7C1020"), (1.0, "#4E0814")])
    cv.lay(np.ones_like(cv.Y), Mat("#FFFFFF"), albedo=base)
    return cv.flat().image(), (0, 0, 0, 0), (0.0, 0.5)


def enemy_trail(width):
    cv = Canvas(width, EB_FILL_H)
    base = vertical(cv, [(0.0, "#C89A70"), (0.08, "#FFF0D2"), (0.4, "#FFDDB0"), (1.0, "#E8A266")])
    cv.lay(np.ones_like(cv.Y), Mat("#FFFFFF"), albedo=base)
    return cv.flat().image(), (0, 0, 0, 0), (0.0, 0.5)


def hud_sprites():
    hero_frame = bar_frame(HERO_OUTSET, HERO_H, LIP, 2.3)
    xp_frame = bar_frame(XP_OUTSET, XP_H, 1.5, 1.5)
    return {
        "Panel": panel(),
        "Button": button(),
        "Pip": pip(),
        "WeaponSlot": slot(True),
        "TomeSlot": slot(False),
        "SlotAccent": slot_accent(),
        "AbilityRing": ability_ring(),
        "AbilityCooldown": ability_cooldown(),
        "BossNamePlate": name_plate(),
        "HeroTrough": trough(48, HERO_H, "#2A0E12"),
        "HeroFill": hero_fill(),
        "HeroTrail": trail(HERO_H, "#FFE4C0", "#F2A66C"),
        "HeroGlint": glint("#FFB8A0"),
        "HeroFrame": hero_frame,
        "XpTrough": trough(48, XP_H, "#0E1832"),
        "XpFill": xp_fill(),
        "XpGlint": glint("#9AD8FF"),
        "XpFrame": xp_frame,
        "BossTrough": trough(48, BOSS_H, "#1A0706", glow="#E0501A"),
        "BossFill": boss_fill(),
        "BossTrail": trail(BOSS_H, "#FFF2CC", "#F5C47A"),
        "BossGlint": glint("#FFD878"),
        "BossFrame": boss_frame(),
    }


def bar_sprites():
    return {
        "Back": enemy_back(4.0),
        "Fill": enemy_fill(EB_W - 8),
        "Trail": enemy_trail(EB_W - 8),
        "CompactBack": enemy_back(6.0),
        "CompactFill": enemy_fill(EB_W - 12),
        "CompactTrail": enemy_trail(EB_W - 12),
    }


# The bars' layout, written into HudArtSet: window offsets from the bar's rect and the frame's outset past the window
# (left, bottom, right, top), and the width of the light at the fill's end.
BARS = {
    "_hero": dict(prefix="Hero", window=(0, 0, 0, 0), outset=HERO_OUTSET, glint=14, trail=True),
    "_experience": dict(prefix="Xp", window=(0, 0, 0, 0), outset=XP_OUTSET, glint=10, trail=False),
    "_boss": dict(prefix="Boss", window=BOSS_WINDOW, outset=BOSS_OUTSET, glint=14, trail=True),
}


# ----------------------------------------------------------------------------------------------------------------------
# Packing and the Unity files.


class Packed:
    def __init__(self, name, image, border, pivot):
        self.name, self.images, self.border, self.pivot = name, [image], border, pivot
        self.cells = []


def existing_ids(meta_path):
    """name -> (internalID, spriteID) of the sprites an existing meta holds, so a repaint keeps every reference."""
    ids = {}
    if not os.path.exists(meta_path):
        return ids
    text = open(meta_path, encoding="utf-8").read()
    for match in re.finditer(r"      name: (\S+)\n(?:.*\n)*?      spriteID: ([0-9a-f]{32})\n      internalID: (-?\d+)", text):
        ids[match.group(1)] = (int(match.group(3)), match.group(2))
    return ids


def fresh_internal_id(taken):
    while True:
        value = int.from_bytes(uuid.uuid4().bytes[:8], "little", signed=True)
        if value not in (0, 21300000) and value not in taken:
            return value


SPRITE_META = """    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: {w}
        height: {h}
      alignment: 9
      pivot: {{x: {px}, y: {py}}}
      border: {{x: {bl}, y: {bb}, z: {br}, w: {bt}}}
      customData:
      outline: []
      physicsShape: []
      tessellationDetail: -1
      bones: []
      spriteID: {sprite_id}
      internalID: {internal_id}
      vertices: []
      indices:
      edges: []
      weights: []
"""


def texture_meta(guid, tpu, sprites, ids):
    """The painted effect sheets' import (bilinear, no mipmaps, ASTC 4x4 on Android at full size, alpha as transparency),
    as Multiple sprites with their rects, pivots and borders."""
    base = skill.TEXTURE_META.format(guid=guid, sprite_id="", tpu=skill.number(tpu))
    table = "".join(f"  - first:\n      213: {ids[s.name][0]}\n    second: {s.name}\n" for s in sprites)
    base = base.replace("  internalIDToNameTable: []\n", "  internalIDToNameTable:\n" + table)
    base = base.replace("  spriteMode: 1\n", "  spriteMode: 2\n")
    entries = "".join(SPRITE_META.format(
        name=s.name, x=s.cells[0][0], y=s.cells[0][1], w=s.cells[0][2], h=s.cells[0][3],
        px=skill.number(s.pivot[0]), py=skill.number(s.pivot[1]), bl=s.border[0], bb=s.border[1], br=s.border[2],
        bt=s.border[3], sprite_id=ids[s.name][1], internal_id=ids[s.name][0]) for s in sprites)
    base = base.replace("    sprites: []\n", "    sprites:\n" + entries)
    base = base.replace("    spriteID: \n", "    spriteID: \n")
    names = "".join(f"      {s.name}: {ids[s.name][0]}\n" for s in sprites)
    base = base.replace("    nameFileIdTable: {}\n", "    nameFileIdTable:\n" + names)
    return base


def build_sheet(name, tpu, painted, frames_dir):
    sprites = [Packed(f"{name}_{key}", *value) for key, value in painted.items()]
    sheet = skill.bleed(skill.pack(sprites))
    png = os.path.join(OUT_DIR, name + ".png")
    Image.fromarray(sheet, "RGBA").save(png, optimize=True)
    meta = png + ".meta"
    guid = skill.existing_guid(meta)
    ids = existing_ids(meta)
    taken = {v[0] for v in ids.values()}
    for s in sprites:
        if s.name not in ids:
            internal = fresh_internal_id(taken)
            taken.add(internal)
            ids[s.name] = (internal, uuid.uuid4().hex)
    skill.write(meta, texture_meta(guid, tpu, sprites, ids))
    if frames_dir:
        os.makedirs(frames_dir, exist_ok=True)
        for s in sprites:
            Image.fromarray(s.images[0], "RGBA").save(os.path.join(frames_dir, s.name + ".png"))
    size = skill.astc_bytes(sheet.shape[1], sheet.shape[0])
    print(f"{name}.png {sheet.shape[1]}x{sheet.shape[0]}  ASTC 4x4 {size / 1024:.1f} KiB")
    for s in sprites:
        x, y, w, h = s.cells[0]
        print(f"  {s.name:28s} {w:4d}x{h:<4d} border {s.border}")
    return dict(guid=guid, ids=ids, sheet=sheet, sprites=sprites, bytes=size)


def ref(built, key, sheet_name):
    return f"{{fileID: {built['ids'][f'{sheet_name}_{key}'][0]}, guid: {built['guid']}, type: 3}}"


def vec(v):
    return "{x: %s, y: %s, z: %s, w: %s}" % tuple(skill.number(float(c)) for c in v)


def asset_header(script_guid, name):
    return ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000", "MonoBehaviour:",
            "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
            f"  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}", f"  m_Name: {name}",
            "  m_EditorClassIdentifier: "]


def write_hud_set(built):
    body = asset_header(skill.existing_guid(HUD_SCRIPT_META), "HudArtSet")
    for field, key in (("_panel", "Panel"), ("_button", "Button"), ("_pip", "Pip"), ("_weaponSlot", "WeaponSlot"),
                       ("_tomeSlot", "TomeSlot"), ("_slotAccent", "SlotAccent"), ("_abilityRing", "AbilityRing"),
                       ("_abilityCooldown", "AbilityCooldown"), ("_bossNamePlate", "BossNamePlate")):
        body.append(f"  {field}: {ref(built, key, HUD_SHEET)}")
    for field, bar in BARS.items():
        p = bar["prefix"]
        body.append(f"  {field}:")
        body.append(f"    _trough: {ref(built, p + 'Trough', HUD_SHEET)}")
        body.append(f"    _fill: {ref(built, p + 'Fill', HUD_SHEET)}")
        body.append(f"    _trail: {ref(built, p + 'Trail', HUD_SHEET) if bar['trail'] else '{fileID: 0}'}")
        body.append(f"    _glint: {ref(built, p + 'Glint', HUD_SHEET)}")
        body.append(f"    _frame: {ref(built, p + 'Frame', HUD_SHEET)}")
        body.append(f"    _window: {vec(bar['window'])}")
        body.append(f"    _frameOutset: {vec(bar['outset'])}")
        body.append(f"    _glintWidth: {skill.number(bar['glint'])}")
    path = os.path.join(OUT_DIR, "HudArtSet.asset")
    skill.write(path, "\n".join(body) + "\n")
    skill.write(path + ".meta", skill.ASSET_META.format(guid=skill.existing_guid(path + ".meta")))


def write_bar_set(built):
    body = asset_header(skill.existing_guid(BAR_SCRIPT_META), "EnemyBarArtSet")
    for field, key in (("_back", "Back"), ("_fill", "Fill"), ("_trail", "Trail"), ("_compactBack", "CompactBack"),
                       ("_compactFill", "CompactFill"), ("_compactTrail", "CompactTrail")):
        body.append(f"  {field}: {ref(built, key, BAR_SHEET)}")
    path = os.path.join(OUT_DIR, "EnemyBarArtSet.asset")
    skill.write(path, "\n".join(body) + "\n")
    skill.write(path + ".meta", skill.ASSET_META.format(guid=skill.existing_guid(path + ".meta")))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--frames", help="also save every sprite as a PNG in this directory")
    args = parser.parse_args()
    for meta in (HUD_SCRIPT_META, BAR_SCRIPT_META):
        if not os.path.exists(meta):
            raise SystemExit(f"{meta} is missing: the sets need their scripts' GUIDs.")
    os.makedirs(OUT_DIR, exist_ok=True)
    folder_meta = OUT_DIR + ".meta"
    skill.write(folder_meta, skill.FOLDER_META.format(guid=skill.existing_guid(folder_meta)))
    hud = build_sheet(HUD_SHEET, HUD_TPU, hud_sprites(), args.frames)
    bars = build_sheet(BAR_SHEET, BAR_TPU, bar_sprites(), args.frames)
    write_hud_set(hud)
    write_bar_set(bars)
    total = hud["bytes"] + bars["bytes"]
    print(f"total ASTC 4x4 {total / 1024:.1f} KiB ({total / 1048576:.3f} MiB)")


if __name__ == "__main__":
    main()
