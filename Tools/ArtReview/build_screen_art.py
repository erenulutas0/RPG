"""The between-runs and pause screens, painted (the UI round).

The in-run HUD was painted in riveted dark iron, gold inlay and embers (build_hud_art.py); the screens a run ends on were
still the old forged blank stretched over every panel, card and button alike, tinted to tell them apart, so an equipped
card, one to forge and one out of reach differed only in a word and a tint. This paints them in the HUD's language, as
the foundry's own forge and ledger:

  - Surface: the forge's and the ledger's frame (9-slice), the HUD panel's build at the screens' scale: an outline, a
    gilded edge, a domed iron rim, a gold inlay between grooves and a chamfer down to the panels' navy face, with a
    riveted corner plate and an ember at each corner.
  - Cards: the forge's weapon and relic cards (9-slice), one frame a state, so the state reads before the words do:
    Equipped is gilded all through, its corner embers burning and a warm light round the inside of its frame; Affordable
    is iron with a molten inlay and a glow rising from its foot, the metal hot and ready; Owned is iron with a cold
    steel inlay and steel studs, calm; Locked (too expensive) is soot-black iron, a tarnished inlay and dead coals, its
    face a shade darker. The state accent strip beside the words is painted light, so the forge's state colour still
    tints it.
  - Icon well: the socket a relic's picture sits in, its inside the very navy the painted icons carry, flat where the
    icon lies, so an icon painted on that navy shows no square.
  - Buttons: primary (Start run, Try again, Resume) a gold-rimmed plate of hot iron, glowing at its foot; secondary
    (Relic Forge, the hero chooser) iron with a gold inlay on a navy-iron face; each with a pressed state (pushed in, its
    lip in shadow; the primary's heat flaring) and a disabled one (dull metal, cold face), for a sprite swap.
  - Plates: the forge's nameplate behind its title, and the result's title plates - Victory gilded, its bosses' embers
    blazing, under a crest of flame on a forged mount; Defeat soot iron and tarnished bronze, the embers dead, the plate
    cracked from its left boss, under the same mount cracked and holding a dead coal and a wisp of smoke.
  - Pause plate: the ledger's frame with its embers banked low, behind the pause title and its hint.
  - Hero chooser: an arched niche behind the portrait, lit from its floor (the hero chosen stands in the forge's light),
    and the pips under the chooser, one a hero, the chosen one an ember and the rest dark sockets.
  - Rule and stud: a gold inlay line in an iron groove, tapering at its ends, and a forged lozenge with an ember: under
    the forge's section headers, as the ledger's divider, and where a hero who carries one weapon leaves the rack's
    other places empty.

One sheet, Assets/_Project/Art/UI/Screens/UI_Screens.png, at 100 texels a unit (the canvas's reference: a sprite paints
at its size on the 1080-wide canvas, its Image's multiplier 1), imported as Multiple sprites with their rects, pivots
and 9-slice borders in the .meta, bilinear, no mipmaps, ASTC 4x4 on Android, as the HUD's sheet; and the set,
ScreenArtSet.asset (Cryptforge.Art.ScreenArtSet), with every sprite and the few lengths ScreenArtView lays them out
by. Painted 4x supersampled as a height field lit from the top left, box-filtered in sRGB, exported straight-alpha with
colour bled into the clear texels (build_hud_art's canvas, build_skill_vfx's packing and export). Deterministic: the
same script paints the same sheet byte for byte. Existing .meta GUIDs and sprite ids are kept.

Usage:
    python Tools/ArtReview/build_screen_art.py                # paint the sheet, its meta and the set
    python Tools/ArtReview/build_screen_art.py --frames DIR   # also save every sprite as a PNG, for review
"""
import argparse
import math
import os
import uuid

import numpy as np
from PIL import Image

import build_hud_art as hud
import build_skill_vfx as skill
from build_hud_art import (FACE, GOLD, GOLD_DARK, GROOVE, IRON, IRON_DARK, OUTLINE, Mat, band, dome, ember, inside, ramp,
                           rivet, sd_box, sd_chamfer, sd_circle, sd_horn)
from build_skill_vfx import hex_rgb, smoothstep

ROOT = skill.ROOT
OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "UI", "Screens")
SCRIPT_META = os.path.join(ROOT, "Assets", "_Project", "Scripts", "Art", "Unity", "ScreenArtSet.cs.meta")
SHEET = "UI_Screens"
TPU = 100.0  # the canvas's reference pixels per unit

GOLD_BRIGHT = Mat("#D59A47", spec=1.0, gloss=26, speccol="#FFF0C4")
STEEL = Mat("#8A96AC", spec=0.95, gloss=26, speccol="#E8F2FF")
SOOT = Mat("#1D1E24", spec=0.10, gloss=8, speccol="#8890A0")
SOOT_DARK = Mat("#111217", spec=0.05, gloss=6, speccol="#808898")
TARNISH = Mat("#4B3F31", spec=0.22, gloss=10, speccol="#B4A48C")
TARNISH_DARK = Mat("#352C22", spec=0.10, gloss=10, speccol="#A89C88")  # a locked card's inlay: no light caught
DULL = Mat("#4E4A44", spec=0.16, gloss=10, speccol="#A8A296")
MOLTEN = Mat("#4A1408", spec=0.3, gloss=20, speccol="#FFD8A0")
CARD_FACE = Mat("#0A0F1E")  # a shade under the surface's navy: the cards read as panels set into it
LOCKED_FACE = Mat("#070A13")


class Canvas(hud.Canvas):
    """The HUD's canvas, but what is laid covers the light glowing under it, so a plate laid over an ember's heat or a
    flame's foot hides it; and light that lies behind the metal (a halo, rays) is laid under the lit result."""

    def lay(self, cover, mat, height=None, albedo=None):
        self.emit *= (1 - np.clip(cover, 0.0, 1.0))[..., None]
        super().lay(cover, mat, height, albedo)

    def under(self, rgb, alpha):
        a = np.clip(alpha, 0, 1)[..., None]
        self.rgba[..., :3] = self.rgba[..., :3] + np.asarray(rgb, np.float32) * a * (1 - self.rgba[..., 3:])
        self.rgba[..., 3:] = self.rgba[..., 3:] + a * (1 - self.rgba[..., 3:])


def full(like, value):
    return np.full_like(like, value)


def corner_plate(cv, cx, cy, half, chamfer, rim, plate, stud):
    """A riveted plate over a frame's corner, casting a soft shadow down-right, with a stud at its middle."""
    X, Y = cv.X, cv.Y
    sd = sd_chamfer(X, Y, cx - half, cy - half, cx + half, cy + half, chamfer)
    cast = sd_chamfer(X, Y, cx - half + 0.5, cy - half + 1.0, cx + half + 1.5, cy + half + 2.5, chamfer)
    cv.darken(0.55 * np.clip(1 - (cast + 0.5) / 2.2, 0, 1) * (1 - inside(sd)))
    pd = -sd
    cv.lay(band(pd, 0.0, 1.1), OUTLINE, height=full(pd, 1.0))
    cv.lay(band(pd, 1.1, 3.0), rim, height=1.4 + dome(pd, 1.1, 3.0, 1.0))
    cv.lay(band(pd, 3.0, 1e9), plate, height=full(pd, 2.4) + 0.25 * smoothstep(3.0, 8.0, pd))
    stud(cv, cx, cy)


def coal(cv, cx, cy, r, base):
    """A dead ember: a matte black coal where the ember was, a last faint red deep in it, catching no light."""
    sd = sd_circle(cv.X, cv.Y, cx, cy, r)
    cv.lay(inside(sd - 0.8) * (1 - inside(sd)), OUTLINE, height=full(sd, base))
    t = np.clip(-sd / r, 0, 1)
    cv.lay(inside(sd), Mat("#211311", spec=0.18, gloss=14, speccol="#B09890"), height=base + r * 0.35 * np.sqrt(t * (2 - t)))
    d = np.hypot(cv.X - cx, cv.Y - cy) / r
    cv.glow("#6A1A0C", 0.30 * np.clip(1 - d * 1.4, 0, 1) * inside(sd))


def lit(cv, cx, cy, r, base, heat):
    """An ember, or a dead coal when its heat is all but gone."""
    if heat < 0.2:
        coal(cv, cx, cy, r, base)
    else:
        ember(cv, cx, cy, r, base, heat=heat)


def corners(w, h, inset):
    return ((inset, inset), (w - inset, inset), (inset, h - inset), (w - inset, h - inset))


# ----------------------------------------------------------------------------------------------------------------------
# The sprites. Each returns (image, border (left, bottom, right, top) in texels, pivot (x, y) normalised).

SURFACE, SURFACE_BORDER, SURFACE_FRAME = 144, 44, 17.0


def surface(heat=1.0, plate_half=15.0):
    """The forge's and the ledger's frame. The forge's title stands 23 texels under its top edge and its cards 36 inside
    its sides, so the frame keeps to 17 and its corner plates to the corners."""
    cv = Canvas(SURFACE, SURFACE)
    X, Y = cv.X, cv.Y
    s = SURFACE
    d = -sd_chamfer(X, Y, 0.25, 0.25, s - 0.25, s - 0.25, 7.0)
    cv.lay(band(d, 0.0, 1.3), OUTLINE, height=full(d, 0.2))
    cv.lay(band(d, 1.3, 3.7), GOLD, height=0.2 + dome(d, 1.3, 3.7, 1.1))
    cv.lay(band(d, 3.7, 9.8), IRON, height=0.6 + dome(d, 3.7, 9.8, 2.6))
    cv.lay(band(d, 9.8, 10.7), GROOVE, height=full(d, 0.1))
    cv.lay(band(d, 10.7, 14.1), GOLD, height=dome(d, 10.7, 14.1, 1.5))
    cv.lay(band(d, 14.1, 15.0), GROOVE, height=full(d, 0.0))
    cv.lay(band(d, 15.0, SURFACE_FRAME), IRON_DARK, height=ramp(d, 15.0, SURFACE_FRAME, 0.8, 0.0))
    # The face: the panels' navy, flat, shaded only where the frame stands over it.
    cv.lay(band(d, SURFACE_FRAME, 1e9), FACE, height=full(d, 0.0))
    cv.darken(0.45 * (1 - smoothstep(SURFACE_FRAME, SURFACE_FRAME + 6.0, d)) * (d >= SURFACE_FRAME - 0.5))

    def stud(c, cx, cy):
        for dx, dy in ((-1, -1), (1, -1), (-1, 1), (1, 1)):
            rivet(c, cx + dx * (plate_half - 5.6), cy + dy * (plate_half - 5.6), 1.7, 2.6)
        ember(c, cx, cy, 5.2, 2.7, heat=heat)

    for cx, cy in corners(s, s, plate_half + 2.0):
        corner_plate(cv, cx, cy, plate_half, 5.5, GOLD, IRON, stud)
    return cv.shade().image(), (SURFACE_BORDER,) * 4, (0.5, 0.5)


PAUSE_PLATE = 128


def pause_plate():
    """The ledger's frame with its embers banked low: the forge waits."""
    image, _, pivot = surface(heat=0.42, plate_half=13.0)
    return image, (40,) * 4, pivot


CARD_W, CARD_H, CARD_BORDER = 112, 96, 30
CARD_FRAME = 11.0
# Each state's metal: the rim, the inlay, the chamfer, the corner plates' rim and plate, the stud, and the face.
CARD_STATES = {
    "Equipped": dict(rim=GOLD_BRIGHT, inlay=GOLD_BRIGHT, chamfer=GOLD_DARK, plate_rim=GOLD_BRIGHT, plate=GOLD,
                     stud="ember", heat=1.3, face=CARD_FACE, inner_glow=0.30),
    "Affordable": dict(rim=IRON, inlay=MOLTEN, chamfer=IRON_DARK, plate_rim=GOLD, plate=IRON, stud="ember", heat=1.0,
                       face=CARD_FACE, foot_glow=0.30),
    "Owned": dict(rim=IRON, inlay=STEEL, chamfer=IRON_DARK, plate_rim=STEEL, plate=IRON, stud="steel", face=CARD_FACE),
    "Locked": dict(rim=SOOT, inlay=TARNISH_DARK, chamfer=SOOT_DARK, plate_rim=TARNISH_DARK, plate=SOOT, stud="coal",
                   face=LOCKED_FACE),
}


def card(state):
    """A forge card's frame in one state. The words start 38 texels in and the state accent 18, so the frame keeps to
    11 and its corner plates (19 across) to the corners."""
    style = CARD_STATES[state]
    cv = Canvas(CARD_W, CARD_H)
    X, Y = cv.X, cv.Y
    w, h = CARD_W, CARD_H
    d = -sd_chamfer(X, Y, 0.25, 0.25, w - 0.25, h - 0.25, 4.5)
    cv.lay(band(d, 0.0, 1.2), OUTLINE, height=full(d, 0.2))
    cv.lay(band(d, 1.2, 5.0), style["rim"], height=0.2 + dome(d, 1.2, 5.0, 1.7))
    cv.lay(band(d, 5.0, 5.8), GROOVE, height=full(d, 0.1))
    cv.lay(band(d, 5.8, 8.2), style["inlay"], height=dome(d, 5.8, 8.2, 1.1))
    if state == "Affordable":
        # The molten inlay: hot metal in the groove, white-gold at its crown.
        across = np.clip(1 - np.abs(d - 7.0) / 1.2, 0, 1)
        cv.glow("#E8561A", 0.95 * across * band(d, 5.8, 8.2))
        cv.glow("#FFC45A", 0.75 * across ** 2.2 * band(d, 5.8, 8.2))
    cv.lay(band(d, 8.2, 9.0), GROOVE, height=full(d, 0.0))
    cv.lay(band(d, 9.0, CARD_FRAME), style["chamfer"], height=ramp(d, 9.0, CARD_FRAME, 0.7, 0.0))
    cv.lay(band(d, CARD_FRAME, 1e9), style["face"], height=full(d, 0.0))
    cv.darken(0.5 * (1 - smoothstep(CARD_FRAME, CARD_FRAME + 5.0, d)) * (d >= CARD_FRAME - 0.5))
    face = band(d, CARD_FRAME, 1e9)
    if "inner_glow" in style:
        # Worn: a warm light round the inside of the gilded frame.
        cv.glow("#D0782A", style["inner_glow"] * np.exp(-(np.maximum(d - CARD_FRAME, 0)) / 4.5) * face)
    if "foot_glow" in style:
        # Ready to forge: the heat rising from the card's foot.
        cv.glow("#C8501C", style["foot_glow"] * np.exp(-np.maximum(h - CARD_FRAME - Y, 0) / 9.0) * face)

    def stud(c, cx, cy):
        if style["stud"] == "steel":
            rivet(c, cx, cy, 3.0, 2.6, mat=STEEL)
        elif style["stud"] == "coal":
            coal(c, cx, cy, 3.3, 2.6)
        else:
            ember(c, cx, cy, 3.3, 2.6, heat=style["heat"])

    for cx, cy in corners(w, h, 9.5):
        corner_plate(cv, cx, cy, 9.0, 3.4, style["plate_rim"], style["plate"], stud)
    return cv.shade().image(), (CARD_BORDER,) * 4, (0.5, 0.5)


def accent():
    """The state accent strip beside a card's words, 4 texels across as its rect is: painted light with rounded ends,
    so the forge's state colour tints it."""
    cv = Canvas(4, 32)
    X, Y = cv.X, cv.Y
    shape = sd_box(X, Y, 0.15, 0.4, 3.85, 31.6, 1.8)
    across = np.clip(1 - np.abs(X - 2.0) / 2.0, 0, 1)
    value = 0.66 + 0.34 * across ** 0.7
    cv.lay(inside(shape), Mat("#FFFFFF"), albedo=value[..., None] * np.ones(3, np.float32))
    return cv.flat().image(), (0, 4, 0, 4), (0.5, 0.5)


ICON = 120  # the relic picture's square on its card (its rect is 120 wide and taller; the picture keeps its aspect)
WELL_MARGIN = 10  # the well reaches this far past the picture's square on each side: its shading stops short of it
WELL = ICON + 2 * WELL_MARGIN


def icon_well():
    """The socket a relic's picture sits in: an iron rim with a gold lip, and inside it the icons' own navy, flat from
    9.5 texels in, so an icon painted on that navy meets it with no seam. Drawn at its size and fitted to the picture's
    rect grown by the margin, keeping its aspect, so it rings the square the picture draws at any card height."""
    cv = Canvas(WELL, WELL)
    X, Y = cv.X, cv.Y
    d = -sd_chamfer(X, Y, 0.25, 0.25, WELL - 0.25, WELL - 0.25, 7.0)
    cv.lay(band(d, 0.0, 1.2), OUTLINE, height=full(d, 0.6))
    cv.lay(band(d, 1.2, 4.4), IRON, height=0.6 + dome(d, 1.2, 4.4, 1.5))
    cv.lay(band(d, 4.4, 5.6), GOLD_DARK, height=ramp(d, 4.4, 5.6, 1.0, 0.4))
    cv.lay(band(d, 5.6, 6.4), GROOVE, height=full(d, -0.2))
    cv.lay(band(d, 6.4, 1e9), FACE, height=full(d, 0.0))
    cv.darken(0.55 * (1 - smoothstep(6.4, 9.5, d)) * (d >= 6.0))
    for cx, cy in corners(WELL, WELL, 4.6):
        rivet(cv, cx, cy, 1.5, 1.6, mat=GOLD)
    return cv.shade().image(), (0, 0, 0, 0), (0.5, 0.5)


BUTTON_W, BUTTON_H, BUTTON_BORDER = 120, 96, 34
BUTTON_FACES = {
    # Top, middle and foot of the face; the heat at its foot.
    ("Primary", "Normal"): (("#4A2416", "#4A1E10", "#6A2810"), 0.55),
    ("Primary", "Pressed"): (("#2A120C", "#3A170D", "#6E2A10"), 0.85),
    ("Primary", "Disabled"): (("#211D1B", "#1C1918", "#201C1A"), 0.0),
    ("Secondary", "Normal"): (("#1F2739", "#161C2C", "#10141F"), 0.0),
    ("Secondary", "Pressed"): (("#0E121C", "#121725", "#161C2B"), 0.0),
    ("Secondary", "Disabled"): (("#16181E", "#13151A", "#111317"), 0.0),
}


def button(kind, state):
    """A button's plate: primary a gold rim round hot iron, secondary iron with a gold inlay round a navy-iron face; a
    gold rivet in each corner. Pressed, the face sinks (its bevel turns, its top lip in shadow) and the primary's heat
    flares; disabled, the metal is dull and the face cold."""
    cv = Canvas(BUTTON_W, BUTTON_H)
    X, Y = cv.X, cv.Y
    w, h = BUTTON_W, BUTTON_H
    pressed, disabled = state == "Pressed", state == "Disabled"
    d = -sd_chamfer(X, Y, 0.25, 0.25, w - 0.25, h - 0.25, 7.0)
    if kind == "Primary":
        rim, inlay = (DULL, DULL) if disabled else (GOLD, GOLD_BRIGHT)
    else:
        rim, inlay = (SOOT, DULL) if disabled else (IRON, GOLD)
    cv.lay(band(d, 0.0, 1.5), OUTLINE, height=full(d, 0.2))
    cv.lay(band(d, 1.5, 6.0), rim, height=0.2 + dome(d, 1.5, 6.0, 1.9))
    cv.lay(band(d, 6.0, 6.8), GROOVE, height=full(d, 0.2))
    cv.lay(band(d, 6.8, 8.4), inlay, height=0.3 + dome(d, 6.8, 8.4, 0.8))
    cv.lay(band(d, 8.4, 9.2), GROOVE, height=full(d, 0.2))
    bevel = IRON_DARK if not disabled else SOOT_DARK
    if pressed:
        cv.lay(band(d, 9.2, 12.0), bevel, height=ramp(d, 9.2, 12.0, 1.0, -0.6))
    else:
        cv.lay(band(d, 9.2, 12.0), bevel, height=ramp(d, 9.2, 12.0, 0.6, 2.4))
    stops, heat = BUTTON_FACES[(kind, state)]
    t = Y / h
    cols = np.stack([hex_rgb(c) for c in stops])
    tone = np.stack([np.interp(t, (0.0, 0.5, 1.0), cols[:, k]) for k in range(3)], -1).astype(np.float32)
    face_height = (-0.6 if pressed else 2.4) + 0.3 * smoothstep(12.0, 24.0, d)
    cv.lay(band(d, 12.0, 1e9), Mat("#FFFFFF", spec=0.10, gloss=8, speccol="#C8C0B8"), height=face_height, albedo=tone)
    face = band(d, 12.0, 1e9)
    if heat:
        # The heat at the plate's foot, as if it stood over the forge.
        depth = np.maximum(h - 12.0 - Y, 0)
        cv.glow("#D2501C", heat * np.exp(-depth / 9.0) * face)
        cv.glow("#FFB04A", 0.35 * heat * np.exp(-depth / 3.0) * face)
    if pressed:
        # The lip of the sunk face: the rim's shadow along its top.
        cv.darken(0.45 * np.exp(-np.maximum(Y - 12.0, 0) / 4.0) * face)
    rivet_mat = DULL if disabled else GOLD
    for cx, cy in corners(w, h, 13.5):
        rivet(cv, cx, cy, 2.4, 2.8 if not pressed else 1.2, mat=rivet_mat)
    return cv.shade().image(), (BUTTON_BORDER, BUTTON_BORDER - 4, BUTTON_BORDER, BUTTON_BORDER - 4), (0.5, 0.5)


# The plates: a long plaque with a boss at each end. (sprite width, height, body height, boss radius, borders)
FORGE_PLATE = dict(w=216, h=100, body=76, boss=33, end=14)
# The title plates' ends reach 50 past their bosses, so a defeat's crack lies in the border and never stretches.
TITLE_PLATE = dict(w=288, h=126, body=92, boss=38, end=50)
PLATE_STYLES = {
    "Header": dict(rim=GOLD, band=IRON, inlay=GOLD, face="#10162A", heat=0.9),
    "Victory": dict(rim=GOLD_BRIGHT, band=GOLD, inlay=GOLD_BRIGHT, face="#1C120E", heat=1.35, halo=True),
    "Defeat": dict(rim=TARNISH, band=SOOT, inlay=TARNISH, face="#0B0D13", heat=0.10, crack=True),
}


def plate(size, style_name):
    """A nameplate: a dark face between a gilded rim and an inlay, ending under a boss at each side - an iron disc in a
    gold rim round an ember. Victory's is gilded through with blazing embers and a halo; Defeat's is soot and tarnish,
    its embers dead, cracked from its left boss into its face (inside the left border, so the crack never stretches)."""
    style = PLATE_STYLES[style_name]
    w, h, body, r = size["w"], size["h"], size["body"], size["boss"]
    cv = Canvas(w, h)
    X, Y = cv.X, cv.Y
    cy = h / 2.0
    bx = r + 4.0
    top, foot = cy - body / 2.0, cy + body / 2.0
    d = -sd_chamfer(X, Y, bx, top, w - bx, foot, body * 0.32)
    cv.lay(band(d, 0.0, 1.3), OUTLINE, height=full(d, 0.2))
    cv.lay(band(d, 1.3, 4.5), style["rim"], height=0.2 + dome(d, 1.3, 4.5, 1.4))
    cv.lay(band(d, 4.5, 5.3), GROOVE, height=full(d, 0.1))
    cv.lay(band(d, 5.3, 9.5), style["band"], height=0.4 + dome(d, 5.3, 9.5, 1.8))
    cv.lay(band(d, 9.5, 10.3), GROOVE, height=full(d, 0.0))
    cv.lay(band(d, 10.3, 12.3), style["inlay"], height=dome(d, 10.3, 12.3, 0.9))
    cv.lay(band(d, 12.3, 13.1), GROOVE, height=full(d, -0.1))
    cv.lay(band(d, 13.1, 1e9), Mat(style["face"]), height=full(d, -0.2))
    cv.darken(0.5 * (1 - smoothstep(13.1, 19.0, d)) * (d >= 12.8))
    if style_name == "Victory":
        # The face warmed from its foot, as metal just out of the fire.
        cv.glow("#8A2A10", 0.22 * np.exp(-np.maximum(foot - 13.1 - Y, 0) / 10.0) * band(d, 13.1, 1e9))
    if style.get("crack"):
        crack(cv, bx + r * 0.55, cy - 9.0, band(d, 1.3, 1e9), length=r + size["end"] - r * 0.55 - 4)
    for side in (-1, 1):
        x = bx if side < 0 else w - bx
        boss(cv, x, cy, r, style)
    cv.shade()
    if style.get("halo"):
        # A soft gold light behind the plate.
        sd = np.minimum(sd_box(X, Y, bx, top, w - bx, foot, 8.0),
                        np.minimum(sd_circle(X, Y, bx, cy, r), sd_circle(X, Y, w - bx, cy, r)))
        cv.under(hex_rgb("#FFB24A"), 0.55 * np.exp(-np.maximum(sd, 0) / 4.5))
    side = int(bx + r + size["end"])
    return cv.image(), (side, 28, side, 28), (0.5, 0.5)


def boss(cv, cx, cy, r, style):
    """A plate's boss: an iron disc in a gold rim, eight rivets round it and, at its heart, an ember in a gold ring."""
    X, Y = cv.X, cv.Y
    sd = sd_circle(X, Y, cx, cy, r)
    cast = sd_circle(X, Y, cx + 1.5, cy + 2.2, r)
    cv.darken(0.6 * np.clip(1 - (cast + 0.5) / 3.0, 0, 1) * (1 - inside(sd)))
    pd = -sd
    cv.lay(band(pd, 0.0, 1.4), OUTLINE, height=full(pd, 1.2))
    cv.lay(band(pd, 1.4, 4.8), style["rim"], height=1.6 + dome(pd, 1.4, 4.8, 1.5))
    cv.lay(band(pd, 4.8, 5.6), GROOVE, height=full(pd, 1.4))
    disc = SOOT if style["band"] is SOOT else IRON
    cv.lay(band(pd, 5.6, 1e9), disc, height=1.8 + 1.4 * np.sqrt(np.clip(pd / r, 0, 1)))
    gem = r * 0.30
    rd = -sd_circle(X, Y, cx, cy, gem + 3.2)
    cv.lay(band(rd, 0.0, 1.0), OUTLINE, height=full(rd, 2.8))
    cv.lay(band(rd, 1.0, 3.2), style["inlay"], height=3.0 + dome(rd, 1.0, 3.2, 0.8))
    for k in range(8):
        a = math.radians(22.5 + 45 * k)
        rivet(cv, cx + (r * 0.68) * math.cos(a), cy + (r * 0.68) * math.sin(a), max(1.4, r * 0.06), 2.8,
              mat=style["inlay"])
    lit(cv, cx, cy, gem, 3.0, style["heat"])


def crack(cv, x0, y0, cover, length=52.0):
    """A crack run from x0 for length texels: a dark fissure widest at its start, a dim red heat deep in it, a chipped
    edge catching the light above it."""
    X, Y = cv.X, cv.Y
    k = length / 52.0
    shape = [(0, 0), (13, 5), (21, 2), (34, 13), (41, 11), (52, 21)]
    points = [(x0 + px * k, y0 + py * k ** 0.5) for px, py in shape]
    dist = np.full(X.shape, 1e9, np.float32)
    along = np.zeros(X.shape, np.float32)
    total = 0.0
    for (ax, ay), (bx_, by) in zip(points, points[1:]):
        vx, vy = bx_ - ax, by - ay
        seg_length = math.hypot(vx, vy)
        t = np.clip(((X - ax) * vx + (Y - ay) * vy) / (seg_length * seg_length), 0, 1)
        seg = np.hypot(X - (ax + t * vx), Y - (ay + t * vy))
        closer = seg < dist
        along = np.where(closer, (total + t * seg_length), along)
        dist = np.minimum(dist, seg)
        total += seg_length
    width = 2.2 * (1 - 0.75 * along / total)
    fissure = np.clip(width - dist + 0.5, 0, 1) * cover
    cv.lay(fissure, OUTLINE, height=full(X, -0.8))
    cv.glow("#A02C12", 0.8 * np.clip(width * 0.6 - dist + 0.3, 0, 1) * cover)
    chip = np.clip(1 - np.abs(dist - width - 0.9) / 0.6, 0, 1) * (Y < y0 + 12 + (X - x0) * 0.35) * cover
    cv.glow("#5A5A64", 0.35 * chip)


def flame(cv, base, mid, tip, r0, heat=1.0):
    """A tongue of flame from base, bending through mid, to tip: crimson at its edge, orange through, a yellow heart low
    in it, as the painted fires are."""
    X, Y = cv.X, cv.Y
    sd = sd_horn(X, Y, [base, mid, tip], r0, 0.8)
    core = sd_horn(X, Y, [(base[0], base[1] - r0 * 0.3), mid, tip], r0 * 0.42, 0.2)
    rise = np.clip((base[1] - Y) / max(base[1] - tip[1], 1), 0, 1)
    cv.lay(inside(sd + 1.0) * (1 - inside(sd)), OUTLINE, height=full(X, 2.0))
    cv.lay(inside(sd), Mat("#4A0C06"), height=full(X, 2.2))
    cv.glow("#B8301A", heat * 0.95 * inside(sd))
    cv.glow("#F07A22", heat * 0.80 * inside(sd + 2.0) * (0.25 + 0.75 * (1 - rise)))
    cv.glow("#FFD070", heat * 0.70 * inside(core) * np.clip(1.25 - rise * 1.3, 0, 1))
    cv.glow("#FFF4D0", heat * 0.35 * inside(core + 2.0) * np.clip(0.8 - rise * 1.6, 0, 1))


# The crest stands on the plate's rim, its top kept 22 texels under the ledger's top edge (5 inside its frame).
CREST_W, CREST_H = 208, 96
CREST_FLAMES = (  # (base, bend, tip, radius at the base), from the mount's foot
    ((-22, 24), (-37, 44), (-30, 66), 10.5),
    ((22, 24), (37, 44), (30, 66), 10.5),
    ((0, 24), (10, 58), (-4, 92), 14.0),
)


def cold_flame(cv, base, mid, tip, r0):
    """A flame gone out, in the same shape: ash-grey iron, unlit, a last red at its foot."""
    X, Y = cv.X, cv.Y
    sd = sd_horn(X, Y, [base, mid, tip], r0, 0.8)
    rise = np.clip((base[1] - Y) / max(base[1] - tip[1], 1), 0, 1)
    cv.lay(inside(sd + 1.0) * (1 - inside(sd)), OUTLINE, height=full(X, 2.0))
    ash = Mat("#3A3A40", spec=0.25, gloss=12, speccol="#B0B0B8")
    cv.lay(inside(sd), ash, height=2.2 + 1.6 * np.clip(-sd / r0, 0, 1),
           albedo=(hex_rgb("#2C2C32") * (1 - rise[..., None]) + hex_rgb("#4A4A52") * rise[..., None]))
    cv.glow("#8A2410", 0.55 * inside(sd) * np.clip(1 - rise / 0.22, 0, 1))


def crest(alight):
    """The crest over a result's title: a forged mount (a stepped iron block in a gold edge, riveted) holding a crown of
    three flames under a fan of gold light - or, for a defeat, the mount cracked and tarnished and the same three flames
    gone out, ash-grey with a last red at their feet and smoke rising from them."""
    cv = Canvas(CREST_W, CREST_H)
    X, Y = cv.X, cv.Y
    cx = CREST_W / 2.0
    foot = CREST_H - 1.0
    rim = GOLD if alight else TARNISH
    metal = IRON if alight else SOOT
    for (bx, by), (mx, my), (tx, ty), r0 in CREST_FLAMES:
        args = ((cx + bx, foot - by), (cx + mx, foot - my), (cx + tx, foot - ty), r0)
        flame(cv, *args) if alight else cold_flame(cv, *args)
    upper = sd_chamfer(X, Y, cx - 40, foot - 30, cx + 40, foot - 15, 4.5)
    lower = sd_chamfer(X, Y, cx - 60, foot - 17, cx + 60, foot, 4.5)
    mount = np.minimum(upper, lower)
    cast = np.minimum(sd_chamfer(X - 1.2, Y - 1.8, cx - 40, foot - 30, cx + 40, foot - 15, 4.5),
                      sd_chamfer(X - 1.2, Y - 1.8, cx - 60, foot - 17, cx + 60, foot, 4.5))
    cv.darken(0.5 * np.clip(1 - (cast + 0.5) / 2.0, 0, 1) * (1 - inside(mount)))
    for part in (lower, upper):
        pd = -part
        cv.lay(band(pd, 0.0, 1.3), OUTLINE, height=full(pd, 1.0))
        cv.lay(band(pd, 1.3, 3.6), rim, height=1.4 + dome(pd, 1.3, 3.6, 1.0))
        cv.lay(band(pd, 3.6, 1e9), metal, height=full(pd, 2.4) + 0.4 * smoothstep(3.6, 7.0, pd))
    for x in (cx - 51, cx + 51):
        rivet(cv, x, foot - 8.5, 2.2, 2.8, mat=rim)
    lit(cv, cx, foot - 8.5, 4.6, 2.9, 1.25 if alight else 0.12)
    if not alight:
        crack(cv, cx - 36, foot - 16, inside(mount) * 1.0)
    cv.shade()
    if alight:
        # A fan of gold light behind the flames.
        ang = np.degrees(np.arctan2(cx - X, foot - 27 - Y))
        rays = (0.5 + 0.5 * np.cos(np.radians(ang) * 11)) ** 5 * (np.abs(ang) < 72)
        dist = np.hypot(X - cx, Y - (foot - 27))
        fade = np.clip(1 - dist / 70.0, 0, 1) * np.clip((dist - 16) / 11, 0, 1)
        cv.under(hex_rgb("#FFC868"), 0.7 * rays * fade)
    else:
        for sx, sy, sr, a in ((cx - 4, foot - 85, 5.5, 0.36), (cx + 2, foot - 90, 6.0, 0.24),
                              (cx - 30, foot - 62, 4.2, 0.26), (cx + 30, foot - 62, 4.2, 0.26),
                              (cx - 28, foot - 70, 4.6, 0.14), (cx + 29, foot - 71, 4.6, 0.14)):
            puff = np.exp(-((X - sx) ** 2 + (Y - sy) ** 2) / (2 * sr * sr))
            cv.over(hex_rgb("#6A6468"), a * puff)
    return cv.image(), (0, 0, 0, 0), (0.5, 0.0)


# As wide as the niche is drawn (the chooser's portrait, 95, and its outset, 10 left and 3 right): never stretched across.
PORTRAIT_W, PORTRAIT_H = 108, 128


def portrait_frame():
    """The niche the chosen hero stands in: an arched iron frame with a gold lip round a dark alcove lit from its floor
    by the forge's light. Its width is the portrait's; its walls stretch with the row's height, the arch and the floor
    held in its borders."""
    cv = Canvas(PORTRAIT_W, PORTRAIT_H)
    X, Y = cv.X, cv.Y
    w, h = PORTRAIT_W, PORTRAIT_H
    rise = 16.0
    radius = ((w / 2.0) ** 2 + rise ** 2) / (2 * rise)
    shape = np.maximum(sd_box(X, Y, 0.25, 0.25, w - 0.25, h - 0.25, 3.0), sd_circle(X, Y, w / 2.0, 0.25 + radius, radius))
    d = -shape
    cv.lay(band(d, 0.0, 1.2), OUTLINE, height=full(d, 0.4))
    cv.lay(band(d, 1.2, 4.6), IRON, height=0.4 + dome(d, 1.2, 4.6, 1.6))
    cv.lay(band(d, 4.6, 6.0), GOLD, height=0.6 + dome(d, 4.6, 6.0, 0.7))
    cv.lay(band(d, 6.0, 6.8), GROOVE, height=full(d, -0.2))
    inner = band(d, 6.8, 1e9)
    cv.lay(inner, Mat("#080A12"), height=full(d, -0.4))
    cv.darken(0.5 * (1 - smoothstep(6.8, 11.0, d)) * (d >= 6.4))
    floor = h - 6.8
    depth = np.maximum(floor - Y, 0)
    spread = np.exp(-((X - w / 2.0) / 30.0) ** 2)
    cv.glow("#B8401A", 0.75 * spread * np.exp(-depth / 16.0) * inner)
    cv.glow("#FFB050", 0.55 * spread * np.exp(-depth / 4.0) * inner)
    return cv.shade().image(), (30, 30, 30, 40), (0.5, 0.5)


PIP = 16


def pip(chosen):
    """A hero's pip under the chooser: the chosen one an ember in a gold setting, the rest dark gems in iron."""
    cv = Canvas(PIP, PIP)
    c = PIP / 2.0
    sd = sd_circle(cv.X, cv.Y, c, c, c - 0.6)
    pd = -sd
    cv.lay(band(pd, 0.0, 1.1), OUTLINE, height=full(pd, 0.4))
    cv.lay(band(pd, 1.1, 2.8), GOLD if chosen else IRON, height=0.6 + dome(pd, 1.1, 2.8, 0.8))
    if chosen:
        ember(cv, c, c, c - 3.0, 1.0, heat=1.3)
    else:
        gem = -sd_circle(cv.X, cv.Y, c, c, c - 3.0)
        cv.lay(band(gem, 0.0, 1e9), Mat("#262B36", spec=0.6, gloss=30, speccol="#B8C4DC"),
               height=1.0 + 0.9 * np.sqrt(np.clip(gem / (c - 3.0), 0, 1)))
    return cv.shade().image(), (0, 0, 0, 0), (0.5, 0.5)


RULE_W, RULE_H, RULE_END = 128, 12, 52


def rule():
    """A gold inlay line in an iron groove, tapering to nothing at its ends (kept in the borders as it stretches)."""
    cv = Canvas(RULE_W, RULE_H)
    X, Y = cv.X, cv.Y
    cy = RULE_H / 2.0
    taper = smoothstep(0.0, RULE_END - 4.0, np.minimum(X, RULE_W - X))
    groove = np.clip(3.0 * taper + 0.4 - np.abs(Y - cy) + 0.5, 0, 1)
    line = np.clip(1.4 * taper - np.abs(Y - cy) + 0.5, 0, 1)
    cv.lay(groove, GROOVE, height=full(X, 0.0))
    cv.lay(line, GOLD, height=0.2 + 1.0 * np.clip(1 - np.abs(Y - cy) / np.maximum(1.4 * taper, 0.3), 0, 1))
    cv.shade()
    # A faint lit edge under the groove, as a groove cut in a face catches the light on its far side.
    edge = np.clip(1 - np.abs(Y - (cy + 3.0 * taper + 1.0)) / 0.8, 0, 1) * taper * 0.18
    cv.over((0.55, 0.6, 0.75), edge)
    return cv.image(), (RULE_END, 0, RULE_END, 0), (0.5, 0.5)


STUD_W, STUD_H = 44, 28


def stud():
    """The rule's centrepiece: a forged lozenge in a gold edge round an ember."""
    cv = Canvas(STUD_W, STUD_H)
    X, Y = cv.X, cv.Y
    cx, cy = STUD_W / 2.0, STUD_H / 2.0
    a, b = cx - 0.8, cy - 0.8
    sd = (np.abs(X - cx) * b + np.abs(Y - cy) * a - a * b) / math.hypot(a, b)
    cast = (np.abs(X - cx - 1.0) * b + np.abs(Y - cy - 1.4) * a - a * b) / math.hypot(a, b)
    cv.darken(0.5 * np.clip(1 - (cast + 0.5) / 1.8, 0, 1) * (1 - inside(sd)))
    pd = -sd
    cv.lay(band(pd, 0.0, 1.2), OUTLINE, height=full(pd, 0.4))
    cv.lay(band(pd, 1.2, 3.0), GOLD, height=0.6 + dome(pd, 1.2, 3.0, 0.9))
    cv.lay(band(pd, 3.0, 1e9), IRON, height=1.2 + 1.4 * np.sqrt(np.clip(pd / 9.0, 0, 1)))
    ember(cv, cx, cy, 4.4, 2.2, heat=1.0)
    return cv.shade().image(), (0, 0, 0, 0), (0.5, 0.5)


SEAL = 60


def seal():
    """The centrepiece of a rack's empty place: one of the plates' bosses on its own, its ember low."""
    cv = Canvas(SEAL, SEAL)
    boss(cv, SEAL / 2.0, SEAL / 2.0 - 1.0, SEAL / 2.0 - 3.5, dict(rim=GOLD, band=IRON, inlay=GOLD, heat=0.75))
    return cv.shade().image(), (0, 0, 0, 0), (0.5, 0.5)


def sprites():
    out = {
        "Surface": surface(),
        "PausePlate": pause_plate(),
        "CardEquipped": card("Equipped"),
        "CardAffordable": card("Affordable"),
        "CardOwned": card("Owned"),
        "CardLocked": card("Locked"),
        "StateAccent": accent(),
        "IconWell": icon_well(),
        "HeaderPlate": plate(FORGE_PLATE, "Header"),
        "VictoryPlate": plate(TITLE_PLATE, "Victory"),
        "DefeatPlate": plate(TITLE_PLATE, "Defeat"),
        "VictoryCrest": crest(True),
        "DefeatCrest": crest(False),
        "PortraitFrame": portrait_frame(),
        "PipLit": pip(True),
        "PipDark": pip(False),
        "Rule": rule(),
        "Stud": stud(),
        "Seal": seal(),
    }
    for kind in ("Primary", "Secondary"):
        for state in ("Normal", "Pressed", "Disabled"):
            out[kind + ("" if state == "Normal" else state)] = button(kind, state)
    return out


# ----------------------------------------------------------------------------------------------------------------------
# The layout ScreenArtView reads from the set, in canvas units: what the painter decided, kept with the paint.

LAYOUT = {
    # The result's title plate round the title's rect (left, bottom, right, top; positive outward): 900 by 126 round
    # its 844 by 100.
    "_titlePlateOutset": (28, 13, 28, 13),
    # The crest's foot below the plate's top edge: it stands on the plate's rim, clear of the title's capitals.
    "_crestSink": 28,
    # The forge's nameplate, centred on its title: as wide as this, as tall as its sprite.
    "_headerPlateWidth": 600,
    # The pause plate round the pause title and its hint.
    "_pausePlateOutset": (24, 30, 24, 22),
    # The icon well past the relic picture's square on each side.
    "_iconWellMargin": WELL_MARGIN,
    # The rule under each of the forge's section headers: its middle this far above the header's foot.
    "_headerRuleLift": 6,
    # The niche round the chooser's portrait: flush with the chooser's left end, top and foot (the portrait stands 10 in
    # from its left and 6 from its top and foot), so the niche is the chooser's end.
    "_portraitFrameOutset": (10, 6, 3, 6),
    # The pips: centred on the chooser's foot under its words, this far apart.
    "_pipSpacing": 20,
    # The rack's empty places: the rule's share of the card's width.
    "_emptySlotRuleShare": 0.6,
}


def build_sheet(painted, frames_dir):
    sprites_ = [hud.Packed(f"{SHEET}_{key}", *value) for key, value in painted.items()]
    sheet = skill.bleed(skill.pack(sprites_))
    png = os.path.join(OUT_DIR, SHEET + ".png")
    Image.fromarray(sheet, "RGBA").save(png, optimize=True)
    meta = png + ".meta"
    guid = skill.existing_guid(meta)
    ids = hud.existing_ids(meta)
    taken = {v[0] for v in ids.values()}
    for s in sprites_:
        if s.name not in ids:
            internal = hud.fresh_internal_id(taken)
            taken.add(internal)
            ids[s.name] = (internal, uuid.uuid4().hex)
    skill.write(meta, hud.texture_meta(guid, TPU, sprites_, ids))
    if frames_dir:
        os.makedirs(frames_dir, exist_ok=True)
        for s in sprites_:
            Image.fromarray(s.images[0], "RGBA").save(os.path.join(frames_dir, s.name + ".png"))
    size = skill.astc_bytes(sheet.shape[1], sheet.shape[0])
    print(f"{SHEET}.png {sheet.shape[1]}x{sheet.shape[0]}  ASTC 4x4 {size / 1024:.1f} KiB ({size / 1048576:.3f} MiB)")
    for s in sprites_:
        x, y, w, h = s.cells[0]
        print(f"  {s.name:28s} {w:4d}x{h:<4d} border {s.border}")
    return dict(guid=guid, ids=ids, sheet=sheet, sprites=sprites_, bytes=size)


SPRITE_FIELDS = (
    ("_surface", "Surface"), ("_pausePlate", "PausePlate"), ("_headerPlate", "HeaderPlate"),
    ("_victoryPlate", "VictoryPlate"), ("_defeatPlate", "DefeatPlate"), ("_victoryCrest", "VictoryCrest"),
    ("_defeatCrest", "DefeatCrest"), ("_equippedCard", "CardEquipped"), ("_affordableCard", "CardAffordable"),
    ("_ownedCard", "CardOwned"), ("_lockedCard", "CardLocked"), ("_stateAccent", "StateAccent"),
    ("_iconWell", "IconWell"), ("_primary", "Primary"), ("_primaryPressed", "PrimaryPressed"),
    ("_primaryDisabled", "PrimaryDisabled"), ("_secondary", "Secondary"), ("_secondaryPressed", "SecondaryPressed"),
    ("_secondaryDisabled", "SecondaryDisabled"), ("_portraitFrame", "PortraitFrame"), ("_pipLit", "PipLit"),
    ("_pipDark", "PipDark"), ("_rule", "Rule"), ("_stud", "Stud"), ("_seal", "Seal"),
)


def write_set(built):
    body = hud.asset_header(skill.existing_guid(SCRIPT_META), "ScreenArtSet")
    for field, key in SPRITE_FIELDS:
        body.append(f"  {field}: {hud.ref(built, key, SHEET)}")
    for field, value in LAYOUT.items():
        if isinstance(value, tuple):
            body.append(f"  {field}: {hud.vec(value)}")
        else:
            body.append(f"  {field}: {skill.number(float(value))}")
    path = os.path.join(OUT_DIR, "ScreenArtSet.asset")
    skill.write(path, "\n".join(body) + "\n")
    skill.write(path + ".meta", skill.ASSET_META.format(guid=skill.existing_guid(path + ".meta")))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--frames", help="also save every sprite as a PNG in this directory")
    args = parser.parse_args()
    if not os.path.exists(SCRIPT_META):
        raise SystemExit(f"{SCRIPT_META} is missing: the set needs its script's GUID.")
    os.makedirs(OUT_DIR, exist_ok=True)
    folder_meta = OUT_DIR + ".meta"
    skill.write(folder_meta, skill.FOLDER_META.format(guid=skill.existing_guid(folder_meta)))
    built = build_sheet(sprites(), args.frames)
    write_set(built)


if __name__ == "__main__":
    main()
