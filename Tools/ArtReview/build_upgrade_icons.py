"""Every upgrade card's icon, painted (UI round).

Only the legacy general cards had icons (Tempered Edge, Quickened Grip, in the Gameplay scene's own table): on the
foundry, where the run builds by category, every card - a weapon, its lines and its evolution, a tome, a general card -
was text. This paints one icon for every id an offer can show, in the family of the painted icons beside them
(Art/UI/UI_Icon_*_v1.png): chunky pixel art on their own 96-cell grid, each part banded onto a few picked tones and
outlined in its own darkest one, a near-black line round the glyph, lit from the top left, a soft halo of the glyph's
colour and the glow of what it emits spread into the painted icons' navy field.

  - The weapons: the Sword (a broad steel blade with a ridge and a chiselled point, a gold guard, a wrapped grip and a
    gold pommel), the Staff (a dark shaft bound in gold, gold claws round the violet orb its blast is the colour of) and
    the Daggers (two crossed leaf blades with ring pommels).
  - A weapon's line: the weapon, smaller, with the mark of the stat it raises at its foot.
  - The stats' marks, the same wherever a stat is: damage an ember impact burst, crit the gold needle star the game
    flashes on a crit, area two cyan rings spreading, range a lime arrow flying out, attack speed two electric bolts,
    health a blood-red heart.
  - A tome: a thick book face on, its cover in a hue of its own, gold corners, border and clasp, and its stat's mark in a
    gold medallion; Force crimson, Precision violet, Reach teal, Tempo blue, Vitality green.
  - The general cards: Iron Skin a heater shield, Keen Edge a sword's point with the crit star, Splintering Crit the
    star breaking into shards, Vital Surge a heart and a white cross, Fortune a four-leaf clover, Swift Boots a winged
    leather boot, Forge Pulse a forge hammer's shock ring, Long Reach a spear and the range arrow, Deep Lungs a pair of
    lungs.
  - An evolution is the thing it evolves from, made richer - gold or ember worked into it, a radiance of soft rays and a
    halo behind it, more light thrown and sparkles round it: Resonant Edge the Sword ringing in gold arcs, Cinder Nova
    the Staff's orb burned white-hot in a nova of violet and ember spikes, Rift Echo the Daggers before a torn rift with
    their echoes beside them, Ironblood Vital Surge's heart forged in steel plates bound in gold with blood molten in its
    seams. Ruinous Edge and Blurred Grip are drawn from Tempered Edge's and Quickened Grip's own painted glyphs (read off
    their 96-cell grid): the sword's blade run molten and cracked under a burning crescent, the gauntlet worked in gold
    leaving its outline behind it twice.
  - The hall's two legacy cards themselves, Tempered Edge and Quickened Grip (2026-11-12): their own painted glyphs read
    off the grid as they are, the swing and sparks or the bolts beside them kept glowing, without the navy square their
    painted icons carry, so the choice card's tier ground shows round them as round every other icon (legacy_card).

Each icon is painted in cells, shown as hard pixels at three texels a cell (softened by a sixth of a cell, as the painted
icons' edges are) and box-filtered to the card's 144 from the middle 86 cells of the grid, so its glyph fills about four
fifths of the card's icon square and reads at the 120 a three-card offer gives it; its cells come out the size of the
painted icons' when the hall shows them at 160. It is exported with straight alpha: opaque where the glyph is, and
elsewhere the least alpha that carries the glow, so laid on the navy it is exactly the painted icons' square, and laid on
anything else (the loadout row's sockets, a card face of another colour) it shows no square. The weapons, their
evolutions and the tomes are rendered again for the loadout row's 56 sockets from the same painting, cropped close and
filtered straight down with their glow kept low, so they need no mip chain.

One sheet, Assets/_Project/Art/UI/UpgradeIcons/UI_UpgradeIcons.png: the card icons on a fixed grid seven across, two
clear texels apart, the socket icons in the last row's free cells (or, once those are too few, a strip of their own under
the grid); Multiple sprites, bilinear, no mipmaps, ASTC 4x4 on
Android, as the painted HUD's sheet. And Cryptforge.Art.UpgradeIconSet, UpgradeIconSet.asset, with each id's card icon
and socket icon. Deterministic: the same script paints the same files byte for byte, and existing GUIDs and sprite ids
are kept, so a repaint never breaks a reference.

Usage:
    python Tools/ArtReview/build_upgrade_icons.py                     # paint the sheet, its meta and the set
    python Tools/ArtReview/build_upgrade_icons.py --frames DIR        # also save every sprite as a PNG
    python Tools/ArtReview/build_upgrade_icons.py --preview DIR [--only ID ...]  # a review sheet only, project untouched
"""
import argparse
import math
import os
import re
import uuid

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))

# ----------------------------------------------------------------------------------------------------------------------
# The grid. The painted icons are chunky pixel art about 96 cells across their square (1254 texels, 13 to a cell); every
# icon here is painted on that same grid, so its pixels are the same size as theirs at any size the card shows it.

N = 96
SUB = 4  # coverage samples per cell per axis
UP = 3  # working texels per cell
CARD = 144  # the card icon: 96 cells at 1.5 texels each
NAVY = np.array([12, 17, 35], np.float32) / 255.0  # the painted icons' field (and the HUD panels' face)

LIGHT = np.array([-0.52, -0.66, 0.54], np.float32)
LIGHT /= np.linalg.norm(LIGHT)


def hexc(value):
    value = value.lstrip("#")
    return np.array([int(value[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


def unit(v):
    v = np.asarray(v, np.float32)
    return v / max(1e-6, float(np.linalg.norm(v)))


LXY = unit(LIGHT[:2])


class Ramp:
    def __init__(self, *colours):
        self.c = [hexc(c) for c in colours]

    def __len__(self):
        return len(self.c)

    def __getitem__(self, i):
        return self.c[max(0, min(len(self.c) - 1, i))]

    @property
    def top(self):
        return len(self.c) - 1


# Dark to light. The blues and the gold are the painted icons' own; the rest are the game's: the Staff's blast violet,
# the ember of the Arcanist and the forge, the health bar's blood red.
STEEL = Ramp("#06124A", "#0C2C92", "#1650D2", "#2A7AF8", "#56A8FF", "#9CD8FF", "#E4F6FF")
IRON = Ramp("#080F2A", "#121E4A", "#1E3172", "#2E4996", "#4A6CBC", "#7E9EE0", "#C2D6FA")
GOLD = Ramp("#3A1C04", "#744006", "#B26A12", "#DE9A26", "#FAC644", "#FFEA96", "#FFFBE4")
EMBER = Ramp("#46080E", "#8E1814", "#D43E18", "#FF7222", "#FFA83E", "#FFD87E", "#FFF8D6")
VIOLET = Ramp("#1C0640", "#3A0E80", "#5E1EC2", "#8A44F6", "#B482FF", "#DEC4FF", "#FAF2FF")
LEATHER = Ramp("#070A1C", "#0E1536", "#182254", "#25357A", "#3850A0")
WOOD = Ramp("#140906", "#28130C", "#402214", "#5C341E", "#7E4E2C", "#A46C40")
RED = Ramp("#3C0410", "#7C0A20", "#C21632", "#F03A4C", "#FF7A7E", "#FFC2BE", "#FFF2F0")
GREEN = Ramp("#032616", "#084A28", "#127C3A", "#22B04C", "#58DA6E", "#AEF6B2", "#F0FFF0")
CYAN = Ramp("#032E3C", "#055A72", "#0A8EAA", "#1AC2D8", "#5EE8F0", "#BEFAFA", "#F2FFFF")
LIME = Ramp("#1E2E04", "#3C5808", "#688C0E", "#9AC21E", "#CAE858", "#EEFEB0", "#FCFFEE")
PINK = Ramp("#380A1A", "#701636", "#B03054", "#DE5A76", "#FF92A2", "#FFCED4", "#FFF4F6")
PALE = Ramp("#4A5878", "#7486AA", "#A2B4D6", "#CCDAF0", "#EEF4FF", "#FFFFFF")

OUTLINE = hexc("#03050E")
WHITE = hexc("#FFFFFF")


# ----------------------------------------------------------------------------------------------------------------------
# Shapes: each is a function of point arrays (x, y in cells, y down) returning (inside, extra), where extra carries what
# its skin needs. Coverage is sampled SUB x SUB per cell; a cell is the shape's when at least half of it is inside.

_c = (np.arange(N, dtype=np.float32) + 0.5)
CX, CY = np.meshgrid(_c, _c)
_s = (np.arange(N * SUB, dtype=np.float32) + 0.5) / SUB
SX, SY = np.meshgrid(_s, _s)


def mask_of(shape, threshold=0.5):
    if isinstance(shape, np.ndarray):
        return shape
    inside, _ = shape(SX, SY)
    return inside.reshape(N, SUB, N, SUB).mean(axis=(1, 3)) >= threshold


def blade(p0, p1, widths, tip=0.0, curve=0.0):
    """An elongated part from p0 (its base) to p1 (its end): its half width runs through `widths` (evenly spaced from
    base to end), and over the last `tip` of its length it tapers to a point; `curve` bows it sideways (cells at the
    middle). Extra: t (0 at the base, 1 at the end), s (-1 to 1 across) and the across direction v."""
    p0, p1 = np.asarray(p0, np.float32), np.asarray(p1, np.float32)
    axis = p1 - p0
    length = float(np.linalg.norm(axis))
    u = axis / length
    v = np.array([-u[1], u[0]], np.float32)
    ws = np.asarray(widths, np.float32)

    def f(x, y):
        dx, dy = x - p0[0], y - p0[1]
        t = (dx * u[0] + dy * u[1]) / length
        s = dx * v[0] + dy * v[1]
        if curve:
            s = s - curve * 4 * np.clip(t, 0, 1) * (1 - np.clip(t, 0, 1))
        w = np.interp(np.clip(t, 0, 1), np.linspace(0, 1, len(ws)), ws)
        if tip > 0:
            w = np.where(t > 1 - tip, w * np.clip((1 - t) / tip, 0, 1) ** 0.9, w)
        inside = (t >= 0) & (t <= 1) & (np.abs(s) <= w)
        return inside, dict(t=t, s=s / np.maximum(w, 1e-3), v=v, u=u)

    return f


def ellipse(cx, cy, rx, ry=None, angle=0.0):
    ry = rx if ry is None else ry
    a = math.radians(angle)
    ca, sa = math.cos(a), math.sin(a)

    def f(x, y):
        dx, dy = x - cx, y - cy
        lx, ly = dx * ca + dy * sa, -dx * sa + dy * ca
        nx, ny = lx / rx, ly / ry
        # The normal's screen direction (for a skin), rotated back.
        sx, sy = nx * ca - ny * sa, nx * sa + ny * ca
        return nx * nx + ny * ny <= 1.0, dict(nx=sx, ny=sy, r=np.sqrt(nx * nx + ny * ny))

    return f


def ring(cx, cy, r0, r1, squash=1.0, a0=None, a1=None):
    """An annulus (optionally squashed vertically, optionally only from angle a0 to a1 in degrees, y down)."""
    def f(x, y):
        dx, dy = x - cx, (y - cy) / squash
        r = np.hypot(dx, dy)
        ang = np.degrees(np.arctan2(dy, dx))
        inside = (r >= r0) & (r <= r1)
        if a0 is not None:
            inside &= _between(ang, a0, a1)
        return inside, dict(r=(r - r0) / max(1e-3, r1 - r0), ang=np.radians(ang), dx=dx, dy=dy)

    return f


def _between(ang, a0, a1):
    span = (a1 - a0) % 360
    return ((ang - a0) % 360) <= span


def polygon(points):
    pts = [(float(x) * SUB, float(y) * SUB) for x, y in points]
    img = Image.new("L", (N * SUB, N * SUB), 0)
    ImageDraw.Draw(img).polygon(pts, fill=255)
    big = np.asarray(img) > 127

    def f(x, y):
        if x.shape != SX.shape:
            raise ValueError("a polygon is sampled on the coverage grid only")
        return big, {}

    return f


def union(*shapes):
    def f(x, y):
        inside = np.zeros(x.shape, bool)
        for s in shapes:
            inside |= s(x, y)[0]
        return inside, {}

    return f


def along(a, d, k):
    return (a[0] + d[0] * k, a[1] + d[1] * k)


# ----------------------------------------------------------------------------------------------------------------------
# Skins: how a part is banded onto its ramp, as a pixel artist would - picked tones, not a smooth light. The light comes
# from the top left, so of a part's two sides the one whose outward normal faces it is the lit one.


def colours(ramp, idx):
    idx = np.clip(np.asarray(idx), 0, len(ramp) - 1)
    out = np.zeros((N, N, 3), np.float32)
    for i in range(len(ramp)):
        out[idx == i] = ramp[i]
    return out


def lit_sign(v):
    """-1 when the part's -s side faces the light, so that s * lit_sign(v) is negative on the lit side."""
    return 1.0 if float(np.dot(v, LXY)) < 0 else -1.0


def skin_blade(shape, ramp, lit=5, shadow=2, tipzone=0.83):
    """A blade's two facets either side of its ridge: the lit one light, its edge lighter, the far one deep with a
    lighter band by the ridge and a darker one at its edge, a light line on the ridge, and a chiselled point whose lit
    face is the brightest of all."""
    _, e = shape(CX, CY)
    s = e["s"] * lit_sign(e["v"])
    t = e["t"]
    idx = np.where(s < 0, lit, shadow)
    idx = np.where((s >= 0) & (s < 0.38), shadow + 1, idx)
    idx = np.where((s < -0.74) & (t > 0.08), lit + 1, idx)
    idx = np.where(s > 0.80, shadow - 1, idx)
    idx = np.where((s < 0) & (t < 0.07), lit - 1, idx)
    idx = np.where(np.abs(s) < 0.10, lit, idx)
    idx = np.where((t > tipzone) & (s < 0.05), lit + 1, np.where(t > tipzone, shadow + 2, idx))
    return colours(ramp, idx)


def skin_tube(shape, ramp, mid=3, spread=2.0, gloss=True):
    """A round bar across its width: light on the lit side with a glint band, dark on the far side."""
    _, e = shape(CX, CY)
    s = e["s"] * lit_sign(e["v"])
    idx = np.round(mid - s * spread).astype(int)
    if gloss:
        idx = np.where((s > -0.72) & (s < -0.28), mid + int(round(spread)), idx)
    return colours(ramp, idx)


def skin_dome(shape, ramp, mid=3, spread=2.6, glint=True):
    _, e = shape(CX, CY)
    nx, ny = e["nx"], e["ny"]
    nz = np.sqrt(np.clip(1 - nx * nx - ny * ny, 0, 1))
    toward = -(nx * LXY[0] + ny * LXY[1])
    idx = np.round(mid + spread * (0.72 * toward + 0.38 * (nz - 0.55))).astype(int)
    if glint:
        idx = np.where((nx + 0.42) ** 2 + (ny + 0.42) ** 2 < 0.05, len(ramp) - 1, idx)
    return colours(ramp, idx)


def skin_pillow(mask, ramp, face=3, bevel=2.0, hi=None, lo=None, shade=0.0):
    """A flat face with a bevelled rim, lighter where the rim faces the light and darker where it faces away; `shade`
    darkens the face toward the bottom right by up to that many tones."""
    d = ndimage.distance_transform_edt(np.pad(mask, 1))[1:-1, 1:-1].astype(np.float32)
    sm = ndimage.gaussian_filter(np.clip(d, 0, bevel + 3), 1.2)
    gy, gx = np.gradient(sm)
    facing = -(gx * LXY[0] + gy * LXY[1])  # the rim's inward slope faces away from the light where this is positive
    rim = (d <= bevel) & mask
    idx = np.full(mask.shape, face, np.float32)
    if shade:
        ys, xs = np.nonzero(mask)
        if len(xs):
            k = ((CX - xs.mean()) * -LXY[0] + (CY - ys.mean()) * -LXY[1]) / max(1.0, np.ptp(xs) + np.ptp(ys)) * 2
            idx = idx - np.clip(k, -0.5, 1.0) * shade
    idx = np.where(rim & (facing < -0.04), face + 1 if hi is None else hi, idx)
    idx = np.where(rim & (facing > 0.04), face - 1 if lo is None else lo, idx)
    return colours(ramp, np.round(idx).astype(int))


def edge_of(mask, width=1):
    inner = ndimage.binary_erosion(mask, iterations=width, border_value=0)
    return mask & ~inner


# ----------------------------------------------------------------------------------------------------------------------
# The canvas an icon is painted on: cells of colour, which are the glyph's, an emission that glows into the field, and
# light laid on the field itself (an evolution's rays and halo).


class Glyph:
    def __init__(self):
        self.rgb = np.zeros((N, N, 3), np.float32)
        self.solid = np.zeros((N, N), bool)
        self.emit = np.zeros((N, N, 3), np.float32)
        self.field = np.zeros((N * UP, N * UP, 3), np.float32)
        self.glow_extra = np.zeros((N * UP, N * UP, 3), np.float32)

    def paint(self, mask, cols, separate=True, emit=0.0, emit_colour=None):
        mask = np.asarray(mask, bool)
        if separate:
            line = ndimage.binary_dilation(mask) & ~mask & self.solid
            self.rgb[line] = OUTLINE
            self.emit[line] = 0
        cols = np.broadcast_to(np.asarray(cols, np.float32), (N, N, 3))
        self.rgb[mask] = cols[mask]
        self.solid |= mask
        if emit:
            ec = cols if emit_colour is None else np.broadcast_to(np.asarray(emit_colour, np.float32), (N, N, 3))
            self.emit[mask] = ec[mask] * emit
        else:
            self.emit[mask] = 0

    def dot(self, x, y, colour, emit=0.0):
        x, y = int(round(x - 0.5)), int(round(y - 0.5))
        if 0 <= x < N and 0 <= y < N:
            self.rgb[y, x] = colour
            self.solid[y, x] = True
            self.emit[y, x] = np.asarray(colour) * emit

    def sparkle(self, x, y, arm, colour, emit=1.2, core=WHITE):
        """A four-point twinkle of one-cell arms, `arm` cells long each way, centred on the cell at (x, y)."""
        for k in range(1, arm + 1):
            for dx, dy in ((k, 0), (-k, 0), (0, k), (0, -k)):
                self.dot(x + dx, y + dy, colour, emit)
        self.dot(x, y, core, emit)

    def outline(self, dark=OUTLINE):
        """A near-black line round the whole glyph, as the painted icons have outside their own coloured rims."""
        ring1 = ndimage.binary_dilation(self.solid) & ~self.solid
        self.rgb[ring1] = dark
        self.emit[ring1] = 0
        self.solid |= ring1

    def centre(self):
        """Moves the whole painting (cells, emission and the field's light) by whole cells so the glyph's bounds sit
        in the middle of the grid."""
        ys, xs = np.nonzero(self.solid)
        dx = int(round(N / 2 - (xs.min() + xs.max() + 1) / 2))
        dy = int(round(N / 2 - (ys.min() + ys.max() + 1) / 2))
        if dx or dy:
            self.rgb = _moved(self.rgb, dx, dy)
            self.solid = _moved(self.solid, dx, dy)
            self.emit = _moved(self.emit, dx, dy)
            self.field = _moved(self.field, dx * UP, dy * UP)
            self.glow_extra = _moved(self.glow_extra, dx * UP, dy * UP)


def _moved(arr, dx, dy):
    out = np.zeros_like(arr)
    h, w = arr.shape[:2]
    ys, yd = (slice(0, h - dy), slice(dy, h)) if dy >= 0 else (slice(-dy, h), slice(0, h + dy))
    xs, xd = (slice(0, w - dx), slice(dx, w)) if dx >= 0 else (slice(-dx, w), slice(0, w + dx))
    out[yd, xd] = arr[ys, xs]
    return out


def part(g, shape, cols, ramp=None, inline=1, separate=True, emit=0.0, emit_colour=None):
    """Paints a part: its colours with its own outline (the ramp's `inline` tone) on its edge cells, and a near-black
    line wherever it lies over a part painted before it, so overlapping parts read apart."""
    m = mask_of(shape)
    cols = np.array(np.broadcast_to(np.asarray(cols, np.float32), (N, N, 3)))
    if ramp is not None and inline is not None:
        cols[edge_of(m)] = ramp[inline]
    g.paint(m, cols, separate=separate, emit=emit, emit_colour=emit_colour)
    return m


# ----------------------------------------------------------------------------------------------------------------------
# Composition: the cells as hard pixels at three texels a cell, softened by a sixth of a cell as the painted icons'
# edges are; the glow of what the glyph emits (and of its brightest cells) spread into the navy field; then box-filtered
# down to the card's 144.


def upsample(cells):
    return np.repeat(np.repeat(cells, UP, axis=0), UP, axis=1)


def blur(img, sigma):
    if img.ndim == 3:
        return np.stack([ndimage.gaussian_filter(img[..., k], sigma) for k in range(img.shape[2])], -1)
    return ndimage.gaussian_filter(img, sigma)


# The card icon shows the middle WINDOW cells of the grid: a glyph about seventy cells across then fills some four fifths
# of the card's icon square, so it reads at the 120 a three-card offer gives it, and its cells come out the size the
# painted icons' are when the hall shows them at 160. The glyph keeps MARGIN cells clear of the window's edge.
WINDOW = 86
MARGIN = 2
LO = (N - WINDOW) // 2  # the window's first cell


def edge_fade(size, width, lo=0.0, hi=None):
    """1 inside [lo, hi) texels on both axes, falling smoothly to 0 at its edge over `width` texels: the field's light
    dies before the sprite's edge, so its border texels are clear and no neighbour on the sheet bleeds in."""
    hi = size if hi is None else hi
    k = np.arange(size, dtype=np.float32) + 0.5
    e = np.clip(np.minimum(k - lo, hi - k) / width, 0, 1)
    e = e * e * (3 - 2 * e)
    return np.minimum.outer(e, e)


def compose(g, halo, halo_strength=0.30, glow=1.0, bloom=0.45):
    ys, xs = np.nonzero(g.solid)
    lo, hi = LO + MARGIN, N - LO - MARGIN
    if len(xs) and (xs.min() < lo or ys.min() < lo or xs.max() >= hi or ys.max() >= hi):
        raise ValueError(f"the glyph reaches the icon's edge: x {xs.min()}..{xs.max()}, y {ys.min()}..{ys.max()}, "
                         f"room {lo}..{hi - 1}")
    rgb = upsample(g.rgb)
    solid = upsample(g.solid.astype(np.float32))
    emit = upsample(g.emit)
    bright = np.clip((g.rgb.max(axis=2) - 0.70) / 0.30, 0, 1) * g.solid
    emit = emit + upsample(bright[..., None] * g.rgb) * bloom + g.glow_extra
    wide = blur(emit, 15.0)
    near = blur(emit, 4.5)
    # A soft halo of the glyph's own colour round its silhouette, as every painted icon carries.
    sil = blur(solid, 10.0)[..., None]
    hc = np.asarray(halo, np.float32)
    light = g.field + hc * sil * halo_strength + glow * (wide * 1.1 + near * 0.5)
    field = NAVY + light * edge_fade(N * UP, 3.0 * UP, LO * UP, (N - LO) * UP)[..., None]
    soft = blur(rgb, 0.5)
    cover = np.clip(blur(solid, 0.5), 0, 1)
    out = soft * cover[..., None] + np.clip(field, 0, 1) * (1 - cover[..., None])
    out = out + near * 0.08 * cover[..., None]
    return np.clip(out, 0, 1), cover


def downsample(img, size):
    h = img.shape[0]
    if h % size == 0:
        k = h // size
        return img.reshape(size, k, size, k, *img.shape[2:]).mean(axis=(1, 3))
    chans = img if img.ndim == 3 else img[..., None]
    out = np.stack([np.asarray(Image.fromarray(np.ascontiguousarray(chans[..., c]).astype(np.float32), "F")
                               .resize((size, size), Image.BOX)) for c in range(chans.shape[2])], -1)
    return out if img.ndim == 3 else out[..., 0]


def to_alpha(final, cover):
    """Straight RGBA that, laid over the navy, is exactly `final`: opaque wherever the glyph is, and elsewhere the least
    alpha that carries the glow, so the icon also sits in a socket or on any other face without a square round it."""
    n = NAVY[None, None, :]
    need_dark = 1 - final / np.maximum(n, 1e-6)
    need_light = (final - n) / (1 - n)
    a = np.clip(np.max(np.maximum(need_dark, need_light), axis=2), 0, 1)
    a = np.maximum(a, np.clip(cover, 0, 1))
    c = np.where(a[..., None] > 1e-4, (final - (1 - a[..., None]) * n) / np.maximum(a[..., None], 1e-4), n)
    return np.concatenate([np.clip(c, 0, 1), a[..., None]], -1)


# ----------------------------------------------------------------------------------------------------------------------
# Parts, in design cells round the icon's centre; a transform (offset, scale) places them, so a line's card can show its
# weapon smaller in a corner.


class T:
    def __init__(self, dx=0.0, dy=0.0, k=1.0, cx=48.0, cy=48.0):
        self.dx, self.dy, self.k, self.cx, self.cy = dx, dy, k, cx, cy

    def p(self, x, y):
        return (self.cx + (x - self.cx) * self.k + self.dx, self.cy + (y - self.cy) * self.k + self.dy)

    def w(self, v):
        return v * self.k


def sword(g, t=T(), blade_ramp=STEEL, guard_ramp=GOLD, grip_ramp=LEATHER, pommel_ramp=GOLD, length=62.0, emit=0.0):
    """The Vanguard's sword, point to the top right: a broad steel blade with a ridge and a chiselled point, a gold
    guard with knobbed ends, a wrapped grip and a gold pommel."""
    a = (34.0, 62.0)  # the guard's centre
    d = unit((1, -1))
    x = unit((1, 1))
    tip = along(a, d, length)
    bl = blade(t.p(*a), t.p(*tip), [t.w(7.0), t.w(6.8), t.w(6.3)], tip=0.17)
    part(g, bl, skin_blade(bl, blade_ramp), blade_ramp, emit=emit)
    guard = blade(t.p(*along(a, x, -14.5)), t.p(*along(a, x, 14.5)),
                  [t.w(3.9), t.w(2.8), t.w(2.8), t.w(2.8), t.w(2.8), t.w(3.9)])
    part(g, guard, skin_tube(guard, guard_ramp, 3, 2), guard_ramp)
    grip = blade(t.p(*along(a, d, -2.5)), t.p(*along(a, d, -15.5)), [t.w(3.6)])
    gm = mask_of(grip)
    gcols = skin_tube(grip, grip_ramp, 2, 1.4, gloss=False)
    _, ge = grip(CX, CY)
    gcols[gm & (np.mod(ge["t"] * 4.0 + ge["s"] * 0.35, 1.0) < 0.38)] = grip_ramp[grip_ramp.top]
    part(g, gm, gcols, grip_ramp, inline=0)
    pc = along(a, d, -20.0)
    pom = ellipse(*t.p(*pc), t.w(5.2))
    part(g, pom, skin_dome(pom, pommel_ramp), pommel_ramp)
    return dict(tip=t.p(*tip), guard=t.p(*a), pommel=t.p(*pc), d=d, x=x, t=t, a=a, length=length)


def staff(g, t=T(), orb_ramp=VIOLET, shaft_ramp=WOOD, metal=GOLD, orb_r=12.5, emit=1.0, orb=True):
    """The Staff: a dark shaft bound in gold leaning to the top right, and at its head gold claws round a violet crystal
    orb, the colour of its blast."""
    foot = (26.0, 82.0)
    head = (55.0, 40.0)
    d = unit((head[0] - foot[0], head[1] - foot[1]))
    x = np.array([-d[1], d[0]], np.float32)
    length = float(np.hypot(head[0] - foot[0], head[1] - foot[1]))
    shaft = blade(t.p(*foot), t.p(*head), [t.w(3.4), t.w(3.8), t.w(4.2)])
    part(g, shaft, skin_tube(shaft, shaft_ramp, 3, 2), shaft_ramp, inline=0)
    for at in (0.12, 0.46, 0.80):
        b = blade(t.p(*along(foot, d, at * length - 2.0)), t.p(*along(foot, d, at * length + 2.0)), [t.w(5.0)])
        part(g, b, skin_tube(b, metal, 3, 2), metal)
    sp = blade(t.p(*along(foot, d, 1.0)), t.p(*along(foot, d, -7.0)), [t.w(3.0), t.w(2.0)], tip=0.6)
    part(g, sp, skin_tube(sp, metal, 3, 2), metal)
    oc = along(head, d, 13.0)
    col = blade(t.p(*along(head, d, -5.0)), t.p(*along(head, d, 3.0)), [t.w(3.8), t.w(4.8), t.w(6.6)])
    part(g, col, skin_tube(col, metal, 3, 2), metal)
    ocp = t.p(*oc)
    if orb:
        o = ellipse(*ocp, t.w(orb_r))
        om = mask_of(o)
        ocols = skin_dome(o, orb_ramp, 3, 2.8, glint=False)
        core = mask_of(ellipse(ocp[0] + t.w(1.8), ocp[1] + t.w(1.8), t.w(orb_r * 0.40)))
        ocols[core] = orb_ramp[orb_ramp.top - 1]
        hl = mask_of(ellipse(ocp[0] - t.w(orb_r * 0.40), ocp[1] - t.w(orb_r * 0.40), t.w(2.1)))
        ocols[hl] = orb_ramp[orb_ramp.top]
        part(g, om, ocols, orb_ramp, emit=emit)
    for side in (-1, 1):
        base = along(along(head, d, 2.5), x, side * 5.2)
        mid = along(along(oc, d, -2.0), x, side * (orb_r + 1.6))
        tipc = along(along(oc, d, orb_r * 0.72), x, side * (orb_r * 0.62))
        c1 = blade(t.p(*base), t.p(*mid), [t.w(2.7), t.w(2.4)])
        c2 = blade(t.p(*mid), t.p(*tipc), [t.w(2.4), t.w(1.7)], tip=0.45)
        m1, m2 = mask_of(c1), mask_of(c2)
        cc = skin_tube(c1, metal, 3, 2)
        cc[m2] = skin_tube(c2, metal, 3, 2)[m2]
        part(g, m1 | m2, cc, metal)
    return dict(orb=ocp, head=t.p(*head), foot=t.p(*foot), orb_r=t.w(orb_r), d=d, x=x, t=t)


def dagger(g, base, angle, t=T(), length=46.0, blade_ramp=STEEL, metal=GOLD, grip_ramp=LEATHER, emit=0.0):
    """One of the Daggers: a leaf blade, a small guard, a wrapped grip and a ring pommel, its guard at `base` and its
    point `angle` degrees from the right (y down)."""
    a = math.radians(angle)
    d = np.array([math.cos(a), math.sin(a)], np.float32)
    x = np.array([-d[1], d[0]], np.float32)
    tip = along(base, d, length)
    bl = blade(t.p(*base), t.p(*tip), [t.w(3.8), t.w(4.8), t.w(4.4), t.w(3.4)], tip=0.30)
    part(g, bl, skin_blade(bl, blade_ramp, tipzone=0.72), blade_ramp, emit=emit)
    gd = blade(t.p(*along(base, x, -7.8)), t.p(*along(base, x, 7.8)), [t.w(2.7), t.w(2.1), t.w(2.1), t.w(2.7)])
    part(g, gd, skin_tube(gd, metal, 3, 2), metal)
    grip = blade(t.p(*along(base, d, -1.8)), t.p(*along(base, d, -10.5)), [t.w(2.6)])
    gm = mask_of(grip)
    gcols = skin_tube(grip, grip_ramp, 2, 1.4, gloss=False)
    _, ge = grip(CX, CY)
    gcols[gm & (np.mod(ge["t"] * 3.0 + ge["s"] * 0.3, 1.0) < 0.4)] = grip_ramp[grip_ramp.top]
    part(g, gm, gcols, grip_ramp, inline=0)
    rc = t.p(*along(base, d, -14.6))
    rg = ring(rc[0], rc[1], t.w(1.5), t.w(4.3))
    _, re_ = rg(CX, CY)
    facing = -(np.cos(re_["ang"]) * LXY[0] + np.sin(re_["ang"]) * LXY[1])
    part(g, rg, colours(metal, np.round(3 + 1.6 * facing).astype(int)), metal)
    return dict(tip=t.p(*tip), base=t.p(*base), d=d, x=x)


def daggers(g, t=T(), **kw):
    """The Daggers: two crossed, points up and out, the right one laid over the left."""
    left = dagger(g, (35.8, 61.5), -58.0, t, **kw)
    right = dagger(g, (60.2, 61.5), -122.0, t, **kw)
    return dict(left=left, right=right, t=t)


# ----------------------------------------------------------------------------------------------------------------------
# The stats' marks: one per stat, the same on a tome's cover, beside a weapon on its line's card and as a general card.
#   damage - an ember impact burst          crit - the gold needle star the game flashes on a crit
#   area   - rings spreading, cyan          range - a lime arrow flying outward
#   attack speed - two electric bolts       health - a blood-red heart


PARCH = Ramp("#3E2E1E", "#7A6446", "#B49C72", "#DCCAA0", "#F6ECCE", "#FFFBEC")
ELECTRIC = Ramp("#06205A", "#0C46B0", "#1A7EF0", "#4CBAFF", "#A6E6FF", "#E6FAFF", "#FFFFFF")


def star(cx, cy, radii, rot=-90.0):
    """A star polygon: one point per entry of radii, evenly round, the first at rot degrees (y down)."""
    k = len(radii)
    return [(cx + r * math.cos(math.radians(rot + 360.0 * i / k)), cy + r * math.sin(math.radians(rot + 360.0 * i / k)))
            for i, r in enumerate(radii)]


def burst(g, cx, cy, r, ramp=EMBER, emit=0.9):
    """Damage: an impact burst, its spikes uneven like a blow's."""
    rr = []
    for i in range(10):
        rr += [r * (1.0 if i % 2 == 0 else 0.80), r * 0.46]
    rot = -90.0 + 9.0
    m = part(g, polygon(star(cx, cy, rr, rot)), colours(ramp, 3), ramp, emit=emit)
    inner = mask_of(polygon(star(cx, cy, [v * 0.62 for v in rr], rot))) & m
    g.paint(inner & ~edge_of(m), ramp[5], separate=False, emit=emit)
    g.paint(mask_of(ellipse(cx, cy, r * 0.26)) & m, ramp[6], separate=False, emit=emit)
    # The lit spikes catch a lighter tone along their top-left edges.
    lit = edge_of(m, 2) & ~edge_of(m, 1) & ((CX - cx) * LXY[0] + (CY - cy) * LXY[1] > 0)
    g.paint(lit & ~inner, ramp[4], separate=False, emit=emit)
    return m


def crit_star(g, cx, cy, r, ramp=GOLD, emit=1.1):
    """Crit: the gold needle star - four long points, four short between them, a white-hot heart."""
    rr = []
    for i in range(4):
        rr += [r, r * 0.22, r * 0.48, r * 0.22]
    m = part(g, polygon(star(cx, cy, rr, -90.0)), colours(ramp, 4), ramp, inline=2, emit=emit)
    inner = mask_of(polygon(star(cx, cy, [v * 0.58 for v in rr], -90.0)))
    g.paint(inner & ~edge_of(m), ramp[5], separate=False, emit=emit)
    g.paint(mask_of(ellipse(cx, cy, r * 0.17)) & m, ramp[6], separate=False, emit=emit)
    return m


def rings(g, cx, cy, r, ramp=CYAN, emit=0.7):
    """Area: two rings spreading from a bright centre, and four marks outside the outer one, flying out."""
    ms = []
    for r0, r1 in ((r * 0.70, r * 0.95), (r * 0.30, r * 0.50)):
        rg = ring(cx, cy, r0, r1)
        _, e = rg(CX, CY)
        facing = -(np.cos(e["ang"]) * LXY[0] + np.sin(e["ang"]) * LXY[1])
        ms.append(part(g, rg, colours(ramp, np.round(3.6 + 1.4 * facing).astype(int)), ramp, emit=emit))
    part(g, ellipse(cx, cy, r * 0.16), ramp[6], ramp, inline=4, emit=emit)
    return ms[0]


def range_arrow(g, cx, cy, r, ramp=LIME, emit=0.6, angle=-45.0):
    """Range: a broad arrow flying outward, two chevrons streaming behind it."""
    a = math.radians(angle)
    # The group (head and chevrons) is centred on (cx, cy), the head ahead of it.
    cx, cy = cx + math.cos(a) * r * 0.30, cy + math.sin(a) * r * 0.30
    d = np.array([math.cos(a), math.sin(a)], np.float32)
    x = np.array([-d[1], d[0]], np.float32)
    head_tip = along((cx, cy), d, r)
    head_base = along((cx, cy), d, r * 0.18)
    pts = [head_tip, along(head_base, x, r * 0.62), along(head_base, x, r * 0.24),
           along(along((cx, cy), d, -r * 0.55), x, r * 0.24), along(along((cx, cy), d, -r * 0.55), x, -r * 0.24),
           along(head_base, x, -r * 0.24), along(head_base, x, -r * 0.62)]
    m = mask_of(polygon(pts))
    part(g, m, skin_pillow(m, ramp, 4, 1.6, hi=5, lo=2), ramp, emit=emit)
    for k in (0.80, 1.12):
        c0 = along((cx, cy), d, -r * k)
        chev = [along(c0, d, r * 0.12), along(along(c0, d, -r * 0.18), x, r * 0.46), along(along(c0, d, -r * 0.34), x, r * 0.46),
                along(c0, d, -r * 0.04), along(along(c0, d, -r * 0.34), x, -r * 0.46), along(along(c0, d, -r * 0.18), x, -r * 0.46)]
        cm = mask_of(polygon(chev))
        part(g, cm, colours(ramp, 3 if k < 1 else 2), ramp, emit=emit * 0.6)
    return m


BOLT = [(0.40, 0.00), (0.86, 0.00), (0.58, 0.40), (0.88, 0.40), (0.26, 1.00), (0.42, 0.55), (0.12, 0.55)]


def bolts(g, cx, cy, r, ramp=ELECTRIC, emit=1.0, pair=True):
    """Attack speed: two electric bolts, the front one larger."""
    def one(bx, by, h, tone):
        pts = [(bx + (px - 0.5) * h * 0.78, by + (py - 0.5) * h) for px, py in BOLT]
        m = mask_of(polygon(pts))
        cols = skin_pillow(m, ramp, tone, 1.4, hi=tone + 2, lo=tone - 1)
        return part(g, m, cols, ramp, emit=emit)
    if pair:
        one(cx - r * 0.42, cy - r * 0.10, r * 1.45, 3)
        return one(cx + r * 0.28, cy + r * 0.06, r * 1.85, 4)
    return one(cx, cy, r * 2.0, 4)


def heart_shape(cx, cy, r):
    lobe = r * 0.56
    return union(ellipse(cx - r * 0.46, cy - r * 0.18, lobe), ellipse(cx + r * 0.46, cy - r * 0.18, lobe),
                 polygon([(cx - r * 0.98, cy - r * 0.02), (cx + r * 0.98, cy - r * 0.02), (cx, cy + r * 0.98)]))


def heart(g, cx, cy, r, ramp=RED, emit=0.4, face=3):
    """Health: a heart, domed, with a glint on its upper left lobe."""
    m = mask_of(heart_shape(cx, cy, r))
    cols = skin_pillow(m, ramp, face, max(1.5, r * 0.16), hi=face + 1, lo=face - 1, shade=1.2)
    gl = mask_of(ellipse(cx - r * 0.50, cy - r * 0.34, r * 0.17, r * 0.13, -40)) & m
    cols[gl] = ramp[ramp.top]
    gl2 = mask_of(ellipse(cx - r * 0.24, cy - r * 0.50, r * 0.08)) & m
    cols[gl2] = ramp[ramp.top - 1]
    return part(g, m, cols, ramp, emit=emit)


# ----------------------------------------------------------------------------------------------------------------------
# Items.


def tome(g, cover_ramp, mark, t=T(), mark_r=14.0, corner=GOLD):
    """A tome: a thick book standing face on, its pages showing at the right and the foot, a leather cover with a gold
    border, corner caps and a clasp, and a gold medallion holding the stat's mark."""
    x0, y0, x1, y1 = 22.0, 13.0, 68.0, 79.0
    page = mask_of(polygon([t.p(x0 + 6, y0 + 4), t.p(x1 + 5, y0 + 4), t.p(x1 + 5, y1 + 4), t.p(x0 + 6, y1 + 5)]))
    pcols = colours(PARCH, 3)
    lines = page & (np.mod(CY, 2.0) < 1.0)
    pcols[lines] = PARCH[2]
    part(g, page, pcols, PARCH, inline=1)
    cover = mask_of(polygon([t.p(x0, y0), t.p(x1, y0), t.p(x1, y1), t.p(x0, y1)]))
    cols = skin_pillow(cover, cover_ramp, 3, 1.5, hi=4, lo=2, shade=1.0)
    part(g, cover, cols, cover_ramp, inline=1)
    # The spine's ridge on the left, two raised bands across it.
    sp = mask_of(polygon([t.p(x0, y0), t.p(x0 + 6.5, y0), t.p(x0 + 6.5, y1), t.p(x0, y1)])) & cover
    scol = colours(cover_ramp, 2)
    scol[sp & (CX < t.p(x0 + 2.5, 0)[0])] = cover_ramp[3]
    g.paint(sp & ~edge_of(cover), scol, separate=False)
    for yb in (y0 + 9, y1 - 9):
        b = mask_of(polygon([t.p(x0 + 1, yb - 1.6), t.p(x0 + 6.5, yb - 1.6), t.p(x0 + 6.5, yb + 1.6), t.p(x0 + 1, yb + 1.6)]))
        part(g, b, skin_pillow(b, GOLD, 4, 1, hi=5, lo=2), GOLD, inline=2, separate=False)
    # The border, inset from the cover's edge, right of the spine.
    bx0, by0, bx1, by1 = x0 + 9.5, y0 + 3.5, x1 - 3.5, y1 - 3.5
    outer = mask_of(polygon([t.p(bx0, by0), t.p(bx1, by0), t.p(bx1, by1), t.p(bx0, by1)]))
    inner = ndimage.binary_erosion(outer, iterations=1)
    g.paint(outer & ~inner, GOLD[3], separate=False)
    # Corner caps on the two outer corners, and on the spine's.
    for cxp, cyp, sx, sy in ((x1, y0, -1, 1), (x1, y1, -1, -1), (x0, y0, 1, 1), (x0, y1, 1, -1)):
        tri = mask_of(polygon([t.p(cxp + sx * 0.0, cyp), t.p(cxp + sx * 9.0, cyp), t.p(cxp, cyp + sy * 9.0)]))
        part(g, tri & cover, skin_pillow(tri & cover, corner, 4, 1, hi=5, lo=2), corner, inline=1, separate=False)
    # The clasp on the fore edge.
    cy_mid = (y0 + y1) / 2
    clasp = mask_of(polygon([t.p(x1 - 4, cy_mid - 4), t.p(x1 + 6, cy_mid - 4), t.p(x1 + 6, cy_mid + 4), t.p(x1 - 4, cy_mid + 4)]))
    part(g, clasp, skin_pillow(clasp, GOLD, 4, 1, hi=5, lo=2), GOLD, inline=1)
    # The medallion: a gold rim round a dark well, the mark inside.
    mc = t.p((bx0 + bx1) / 2, (by0 + by1) / 2)
    rim = ring(mc[0], mc[1], t.w(mark_r + 1.0), t.w(mark_r + 3.6))
    _, e = rim(CX, CY)
    facing = -(np.cos(e["ang"]) * LXY[0] + np.sin(e["ang"]) * LXY[1])
    part(g, rim, colours(GOLD, np.round(3.4 + 1.6 * facing).astype(int)), GOLD, inline=1)
    well = mask_of(ellipse(mc[0], mc[1], t.w(mark_r + 1.0)))
    g.paint(well, cover_ramp[0], separate=False)
    mark(g, mc[0], mc[1], t.w(mark_r))
    return dict(centre=mc)


def shield(g, t=T(), face=IRON, trim=GOLD):
    """Iron Skin: a heater shield, an iron face in a gold rim, a gold boss and four rivets."""
    pts = [t.p(20, 18), t.p(48, 12), t.p(76, 18), t.p(75, 46), t.p(68, 66), t.p(48, 84), t.p(28, 66), t.p(21, 46)]
    m = mask_of(polygon(pts))
    part(g, m, skin_pillow(m, trim, 4, 2.0, hi=5, lo=2), trim, inline=1)
    inner = ndimage.binary_erosion(m, iterations=4)
    cols = skin_pillow(inner, face, 3, 2.0, hi=4, lo=2, shade=1.2)
    # A broad sheen down its lit half.
    sheen = inner & (CX < t.p(40, 0)[0]) & (CX > t.p(30, 0)[0]) & (CY < t.p(0, 60)[1])
    cols[sheen & ~edge_of(inner, 2)] = face[4]
    g.paint(inner, cols, separate=False)
    g.paint(edge_of(inner), face[1], separate=False)
    # The cross ridge.
    ridge = mask_of(blade(t.p(48, 16), t.p(48, 80), [t.w(2.0)])) & inner
    g.paint(ridge, colours(face, 4), separate=False)
    rr = mask_of(blade(t.p(25, 40), t.p(71, 40), [t.w(2.0)])) & inner
    g.paint(rr, colours(face, 4), separate=False)
    boss = ellipse(*t.p(48, 40), t.w(8.5))
    part(g, boss, skin_dome(boss, trim, 3, 2.6), trim)
    for px, py in ((28, 24), (68, 24), (32, 60), (64, 60)):
        rv = ellipse(*t.p(px, py), t.w(2.2))
        part(g, rv, skin_dome(rv, trim, 3, 2.0, glint=False), trim)
    return m


def clover(g, t=T(), ramp=GREEN):
    """Fortune: a four-leaf clover, each leaf a heart, with a stem and a gold glint."""
    c = t.p(48, 44)
    stem = blade(t.p(48, 50), t.p(64, 84), [t.w(2.6), t.w(2.2), t.w(1.8)], curve=-4.0)
    part(g, stem, skin_tube(stem, ramp, 2, 1.5), ramp, inline=0)
    for k, ang in enumerate((-90, 0, 90, 180)):
        a = math.radians(ang)
        d = (math.cos(a), math.sin(a))
        lc = (c[0] + d[0] * t.w(15.5), c[1] + d[1] * t.w(15.5))
        r = t.w(15.0)
        # A heart pointing at the clover's centre: two lobes away from it.
        x = (-d[1], d[0])
        lob = r * 0.52
        l1 = (lc[0] + x[0] * r * 0.45 + d[0] * r * 0.18, lc[1] + x[1] * r * 0.45 + d[1] * r * 0.18)
        l2 = (lc[0] - x[0] * r * 0.45 + d[0] * r * 0.18, lc[1] - x[1] * r * 0.45 + d[1] * r * 0.18)
        tipp = (lc[0] - d[0] * r * 0.92, lc[1] - d[1] * r * 0.92)
        b1 = (l1[0] + x[0] * lob * 0.95 - d[0] * lob * 0.3, l1[1] + x[1] * lob * 0.95 - d[1] * lob * 0.3)
        b2 = (l2[0] - x[0] * lob * 0.95 - d[0] * lob * 0.3, l2[1] - x[1] * lob * 0.95 - d[1] * lob * 0.3)
        shape = union(ellipse(*l1, lob), ellipse(*l2, lob), polygon([b1, b2, tipp]))
        m = mask_of(shape)
        cols = skin_pillow(m, ramp, 3, 1.6, hi=4, lo=2, shade=0.8)
        vein = mask_of(blade(tipp, (lc[0] + d[0] * r * 0.55, lc[1] + d[1] * r * 0.55), [0.55])) & m
        cols[vein] = ramp[2]
        part(g, m, cols, ramp)
    g.paint(mask_of(ellipse(*c, t.w(3.0))), ramp[2], separate=False)
    g.sparkle(*t.p(76, 20), 3, GOLD[5], emit=1.4)
    g.sparkle(*t.p(22, 72), 2, GOLD[4], emit=1.2)


BROWN = Ramp("#1C0C06", "#3C1E0E", "#623218", "#8A4C24", "#B06C36", "#D49256", "#F0BE84")


def boot(g, t=T(), ramp=BROWN, trim=GOLD, wing=PALE):
    """Swift Boots: a leather boot striding right - a turned-down gold cuff, two straps, a gold toe cap and a heel - and
    a white wing swept back from its ankle, speed streaming behind it."""
    # Speed lines first, so the boot and its wing lie over them.
    for yy, x0, ln in ((44, 11, 15), (56, 8, 17), (68, 13, 13)):
        sl = blade(t.p(x0, yy), t.p(x0 + ln, yy), [t.w(0.7), t.w(1.3)])
        part(g, sl, colours(CYAN, 4), CYAN, inline=3, emit=0.9)
    shaft = [(36, 18), (60, 18), (60, 50), (66, 56), (80, 61), (87, 67), (88, 74), (86, 78), (36, 78), (34, 62), (36, 48)]
    body = union(polygon([t.p(*p) for p in shaft]), ellipse(*t.p(80, 70), t.w(7.5)))
    m = mask_of(body)
    cols = skin_pillow(m, ramp, 3, 2.0, hi=4, lo=2, shade=1.2)
    # The leather's sheen down the shin.
    sheen = m & ~edge_of(m, 3) & (CX > t.p(40, 0)[0]) & (CX < t.p(45, 0)[0]) & (CY < t.p(0, 54)[1])
    cols[sheen] = ramp[4]
    part(g, m, cols, ramp)
    for yb in (36, 47):
        st = mask_of(polygon([t.p(35, yb), t.p(61, yb - 2), t.p(61, yb + 2.4), t.p(35, yb + 4.4)])) & m
        part(g, st, skin_pillow(st, trim, 3, 1, hi=4, lo=2), trim, inline=1, separate=False)
    cuff = mask_of(polygon([t.p(33, 12), t.p(63, 12), t.p(62, 24), t.p(34, 24)]))
    part(g, cuff, skin_pillow(cuff, trim, 4, 1.4, hi=5, lo=2), trim)
    sole = mask_of(polygon([t.p(34, 76), t.p(88, 76), t.p(88, 82), t.p(34, 82)]))
    part(g, sole, colours(LEATHER, 2), LEATHER, inline=0)
    heel = mask_of(polygon([t.p(34, 80), t.p(46, 80), t.p(45, 86), t.p(35, 86)]))
    part(g, heel, colours(LEATHER, 1), LEATHER, inline=0)
    toe = mask_of(union(polygon([t.p(70, 59), t.p(81, 62), t.p(80, 77), t.p(70, 77)]), ellipse(*t.p(80, 70), t.w(7.5)))) & m
    part(g, toe, skin_pillow(toe, trim, 4, 1.4, hi=5, lo=2), trim, separate=False)
    # The wing: four feathers swept back and up from the ankle.
    base = t.p(36, 42)
    for ln, ang, wd in ((25, 196, 4.8), (24, 214, 4.6), (20, 232, 4.2), (15, 250, 3.6)):
        a = math.radians(ang)
        tipp = (base[0] + math.cos(a) * t.w(ln), base[1] + math.sin(a) * t.w(ln))
        f = blade(base, tipp, [t.w(wd * 0.55), t.w(wd), t.w(wd * 0.85)], tip=0.42, curve=t.w(2.4))
        part(g, f, skin_tube(f, wing, 3, 1.6), wing)


def lungs(g, t=T(), ramp=PINK):
    """Deep Lungs: a pair of lungs drawn full under their windpipe."""
    tr = blade(t.p(48, 12), t.p(48, 40), [t.w(3.2)])
    tm = mask_of(tr)
    tcols = skin_tube(tr, PALE, 2, 1.2, gloss=False)
    _, te = tr(CX, CY)
    tcols[tm & (np.mod(te["t"] * 6, 1) < 0.35)] = PALE[3]
    part(g, tm, tcols, PALE)
    for side in (-1, 1):
        br = blade(t.p(48, 38), t.p(48 + side * 12, 48), [t.w(2.6), t.w(2.0)])
        part(g, br, skin_tube(br, PALE, 2, 1.2, gloss=False), PALE)
    for side in (-1, 1):
        pts = [t.p(48 + side * 5, 30), t.p(48 + side * 16, 22), t.p(48 + side * 28, 36), t.p(48 + side * 32, 62),
               t.p(48 + side * 28, 80), t.p(48 + side * 14, 82), t.p(48 + side * 5, 72), t.p(48 + side * 7, 52)]
        if side > 0:
            pts = pts[::-1]
        m = mask_of(polygon(pts))
        cols = skin_pillow(m, ramp, 3, 2.0, hi=4, lo=2, shade=1.0)
        # Lobes: a darker fissure across each.
        fis = mask_of(blade(t.p(48 + side * 8, 54), t.p(48 + side * 30, 46), [t.w(0.8)])) & m
        cols[fis] = ramp[1]
        part(g, m, cols, ramp)


def hammer(g, t=T(), head=IRON, handle=WOOD, shock=EMBER):
    """Forge Pulse: a forge hammer, its face glowing from the strike, a shock ring bursting from it and four sparks
    thrown - one for each fourth blow's burst."""
    h0, h1 = t.p(18, 86), t.p(46, 46)
    d = unit((h1[0] - h0[0], h1[1] - h0[1]))
    x = np.array([-d[1], d[0]], np.float32)
    hc = along(h1, d, 5.0)
    face_c = along(hc, x, t.w(15.5))
    # The shock first, behind the head.
    ang0 = math.degrees(math.atan2(x[1], x[0]))
    for r0, r1, span, tone in ((10.0, 14.0, 80, 5), (17.0, 20.4, 64, 4), (23.4, 26.0, 50, 3)):
        rg = ring(face_c[0], face_c[1], t.w(r0), t.w(r1), a0=ang0 - span, a1=ang0 + span)
        part(g, rg, colours(shock, tone), shock, inline=2, emit=1.1)
    hd = blade(h0, h1, [t.w(3.2), t.w(3.8)])
    part(g, hd, skin_tube(hd, handle, 3, 2), handle, inline=0)
    hb = blade(along(hc, x, t.w(-17.0)), along(hc, x, t.w(15.0)), [t.w(8.4), t.w(9.4), t.w(9.4), t.w(9.4)])
    part(g, hb, skin_tube(hb, head, 3, 2), head)
    face = blade(along(hc, x, t.w(11.5)), along(hc, x, t.w(15.0)), [t.w(9.4)])
    part(g, face, skin_tube(face, shock, 4, 1.5), shock, inline=2, separate=False, emit=0.9)
    band_ = blade(along(hc, x, t.w(-3.2)), along(hc, x, t.w(3.2)), [t.w(10.2)])
    part(g, band_, skin_tube(band_, GOLD, 3, 2), GOLD)
    for k, off in enumerate((-48, -16, 16, 48)):
        a = math.radians(ang0 + off)
        g.sparkle(face_c[0] + math.cos(a) * t.w(30.0), face_c[1] + math.sin(a) * t.w(30.0), 2 if k in (1, 2) else 1,
                  shock[5], emit=1.5)


def spear(g, t=T(), blade_ramp=STEEL, shaft=WOOD, metal=GOLD):
    """Long Reach: a spear thrust out to the top right - a broad leaf head on a gold collar, a gold-bound shaft."""
    foot, neck = t.p(16, 84), t.p(52, 44)
    sh = blade(foot, neck, [t.w(3.3), t.w(3.7)])
    part(g, sh, skin_tube(sh, shaft, 3, 2), shaft, inline=0)
    d = unit((neck[0] - foot[0], neck[1] - foot[1]))
    for at in (0.2, 0.55):
        b = blade(along(foot, d, t.w(at * 54 - 1.8)), along(foot, d, t.w(at * 54 + 1.8)), [t.w(4.6)])
        part(g, b, skin_tube(b, metal, 3, 2), metal)
    tipp = along(neck, d, t.w(32.0))
    hd = blade(neck, tipp, [t.w(3.6), t.w(8.0), t.w(6.6)], tip=0.46)
    part(g, hd, skin_blade(hd, blade_ramp, tipzone=0.60), blade_ramp)
    col = blade(along(neck, d, t.w(-5.0)), along(neck, d, t.w(1.8)), [t.w(5.0)])
    part(g, col, skin_tube(col, metal, 3, 2), metal)
    return dict(tip=tipp, d=d)


# ----------------------------------------------------------------------------------------------------------------------
# The two general cards that evolve from a painted icon (Tempered Edge, Quickened Grip) are drawn from that icon's own
# glyph: its cells read off its 96-cell grid, the navy field and the glow left behind, so the evolution is unmistakably
# the card it grew from.

UI_ART = os.path.join(ROOT, "Assets", "_Project", "Art", "UI")


def legacy_glyph(name, cells=N):
    """The painted icon's glyph on the grid, read at `cells` across (fewer than the grid's to draw it smaller) and laid
    in the grid's top left: (colours, mask of its largest part). The glyph is found at twice the grid's resolution, where
    its glow and anything drawn beside it (the Tempered Edge's swing) stand apart from it, then read at the size asked."""
    src = Image.open(os.path.join(UI_ART, f"UI_Icon_{name}_v1.png")).convert("RGB")
    full = np.asarray(src.resize((2 * N, 2 * N), Image.BOX), np.float32) / 255.0
    far = np.abs(full - NAVY).max(axis=2) > 46 / 255.0
    dark = full.mean(axis=2) < NAVY.mean() - 4 / 255.0
    glyph = ndimage.binary_fill_holes(ndimage.binary_closing(far | dark, iterations=1))
    lab, n = ndimage.label(glyph)
    mask = lab == (int(np.argmax(ndimage.sum(glyph, lab, range(1, n + 1)))) + 1)
    im = np.zeros((N, N, 3), np.float32) + NAVY
    out = np.zeros((N, N), bool)
    im[:cells, :cells] = np.asarray(src.resize((cells, cells), Image.BOX), np.float32) / 255.0
    small = np.asarray(Image.fromarray(mask.astype(np.float32), "F").resize((cells, cells), Image.BOX)) >= 0.5
    out[:cells, :cells] = small
    return im, out


def legacy_card(name, cells, glow=1.3):
    """A painted legacy card's own icon on this grid, its navy square dropped (the owner's 2026-11-12 ask: the item on its
    tier's ground): its glyph (legacy_glyph) in its own colours, cell for cell, and the bright marks drawn beside it - the
    Tempered Edge's swing and sparks, the Quickened Grip's bolts - read where they are lit well above the navy and painted
    glowing, as the icon painted them. Its halo is the field's light round it, so it carries none of the old square."""
    def draw(g):
        im, m = legacy_glyph(name, cells)
        far = np.abs(im - NAVY).max(axis=2)
        lit = (im.max(axis=2) > 0.50) & (far > 0.30) & ~ndimage.binary_dilation(m, iterations=1)
        lab, count = ndimage.label(lit)
        marks = np.zeros_like(lit)
        for k in range(1, count + 1):
            if (lab == k).sum() >= 2:
                marks |= lab == k
        ys, xs = np.nonzero(m | marks)
        ox = int(round((N - 1) / 2 - (xs.min() + xs.max()) / 2))
        oy = int(round((N - 1) / 2 - (ys.min() + ys.max()) / 2))
        im, m, marks = shift(im, ox, oy), shift(m, ox, oy), shift(marks, ox, oy)
        g.paint(marks, im, separate=False, emit=glow)
        g.paint(m, im, separate=True)
    return draw


def recolour(cols, mask, ramp, lo=0.0, hi=1.0):
    """The cells' own light, ranked, laid onto a ramp: their darkest at the ramp's dark end, their lightest at its light."""
    lum = cols @ np.array([0.30, 0.55, 0.15], np.float32)
    vals = lum[mask]
    if not len(vals):
        return cols
    q = (lum - np.percentile(vals, 3)) / max(1e-4, np.percentile(vals, 97) - np.percentile(vals, 3))
    idx = np.round(np.clip(lo + (hi - lo) * q, 0, 1) * ramp.top).astype(int)
    out = cols.copy()
    rc = colours(ramp, idx)
    out[mask] = rc[mask]
    return out


def shift(arr, dx, dy):
    out = np.zeros_like(arr)
    h, w = arr.shape[:2]
    ys, yd = (slice(0, h - dy), slice(dy, h)) if dy >= 0 else (slice(-dy, h), slice(0, h + dy))
    xs, xd = (slice(0, w - dx), slice(dx, w)) if dx >= 0 else (slice(-dx, w), slice(0, w + dx))
    out[yd, xd] = arr[ys, xs]
    return out


# ----------------------------------------------------------------------------------------------------------------------
# Evolutions: richer than what they evolve from - gold or ember worked into the metal, a radiance behind the glyph and
# more light thrown off it.


def radiance(g, colour, cx=48.0, cy=48.0, rays=12, strength=0.22, halo=0.20, r1=44.0, phase=0.0, halo_r=24.0):
    """Soft rays and a halo laid on the field round (cx, cy), fading out well inside the icon's edge."""
    xs = (np.arange(N * UP, dtype=np.float32) + 0.5) / UP
    X, Y = np.meshgrid(xs, xs)
    dx, dy = X - cx, Y - cy
    r = np.hypot(dx, dy)
    ang = np.arctan2(dy, dx)
    spokes = np.clip(np.cos(ang * rays + phase), 0, 1) ** 6
    fall = np.clip(1 - r / r1, 0, 1) ** 1.6 * np.clip(r / 8.0, 0, 1)
    disc = np.exp(-(r / halo_r) ** 2)
    g.field += np.asarray(colour, np.float32) * (spokes * fall * strength + disc * halo)[..., None]


RESONANT = Ramp("#081A56", "#1442B0", "#2678F4", "#56AEFF", "#9EDCFF", "#E0F6FF", "#FFFFFF")


def resonant_edge(g):
    """Resonant Edge: the Sword ringing - its blade lit from within and inlaid with gold down its ridge, the blow's
    resonance rippling out from both its edges in gold arcs, under a radiance."""
    radiance(g, GOLD_HALO, strength=0.24, halo=0.18)
    a, d = (34.0, 62.0), unit((1, -1))
    centre = along(a, d, 28.0)
    for side in (45.0, 225.0):
        for r0, r1, span, tone in ((14.5, 17.2, 36, 5), (21.0, 23.4, 30, 4), (27.4, 29.2, 23, 3)):
            part(g, ring(centre[0], centre[1], r0, r1, a0=side - span, a1=side + span), colours(GOLD, tone), GOLD,
                 inline=2, emit=1.1)
    sword(g, blade_ramp=RESONANT, emit=0.30)
    inlay = mask_of(blade(along(a, d, 5.0), along(a, d, 50.0), [0.7, 0.7, 0.4]))
    g.paint(inlay, GOLD[5], separate=False, emit=1.0)
    for sx, sy, arm in ((82, 14, 3), (16, 34, 2), (60, 84, 2)):
        g.sparkle(sx, sy, arm, GOLD[5], emit=1.4)


NOVA = Ramp("#4A0A10", "#A0201A", "#E8501C", "#FF8A2C", "#FFC24E", "#FFEA9A", "#FFFFF0")


def cinder_nova(g):
    """Cinder Nova: the Staff erupting - its orb burned white-hot, a nova of flame spikes round it, violet where the
    staff's blast was and ember within, under a radiance."""
    orb = along((55.0, 40.0), unit((29, -42)), 13.0)
    radiance(g, EMBER_HALO, cx=orb[0], cy=orb[1], strength=0.26, halo=0.22, r1=44)
    rr = []
    for i in range(12):
        rr += [21.5 if i % 2 == 0 else 18.5, 13.8]
    outer = mask_of(polygon(star(orb[0], orb[1], rr, -90 + 7.5)))
    part(g, outer, colours(VIOLET, 4), VIOLET, inline=2, emit=1.1)
    inner = mask_of(polygon(star(orb[0], orb[1], [v * 0.80 for v in rr], -90 + 22.5)))
    g.paint(inner & ~edge_of(outer), NOVA[4], separate=False, emit=1.2)
    staff(g, orb_ramp=NOVA, emit=1.5)
    for sx, sy, arm in ((18, 40, 2), (84, 60, 2), (34, 14, 2), (72, 82, 1)):
        g.sparkle(sx, sy, arm, NOVA[5], emit=1.4)


RIFT = Ramp("#14063A", "#2E0C74", "#5420B8", "#7E4AEC", "#AE86FF", "#DCC8FF", "#FAF4FF")


def rift_echo(g):
    """Rift Echo: the Daggers crossed before a rift torn open behind them - a dark void with lit violet lips - each
    with its echo a step out beside it in violet, under a radiance."""
    radiance(g, VIOLET_HALO, strength=0.22, halo=0.20, phase=0.3)
    tear = [(49, 9), (55, 24), (49, 34), (58, 48), (50, 62), (54, 78), (47, 88), (44, 74), (39, 60), (46, 47), (38, 34),
            (45, 22)]
    tm = mask_of(polygon(tear))
    part(g, tm, colours(RIFT, 5), RIFT, inline=3, emit=1.2)
    void = ndimage.binary_erosion(tm, iterations=2)
    g.paint(void, RIFT[0], separate=False)
    for sx, sy in ((48, 28), (50, 52), (46, 70)):
        g.dot(sx, sy, RIFT[6], emit=1.2)
    for base, angle, dx in (((35.8, 61.5), -58.0, -7.0), ((60.2, 61.5), -122.0, 7.0)):
        dagger(g, (base[0] + dx, base[1] + 3.0), angle, blade_ramp=RIFT, metal=RIFT, grip_ramp=RIFT, emit=0.6)
    daggers(g)
    for sx, sy, arm in ((16, 18, 2), (80, 18, 2), (82, 82, 1), (14, 84, 1)):
        g.sparkle(sx, sy, arm, RIFT[5], emit=1.4)


# ----------------------------------------------------------------------------------------------------------------------
# The cards.

LINE_T = T(dx=-7.0, dy=-6.0, k=0.86)
MARK = (70.0, 70.0)


def line(weapon, mark, mark_r=18.5, t=LINE_T, at=MARK):
    def draw(g):
        weapon(g, t)
        mark(g, at[0], at[1], mark_r)
    return draw


def staff_line(mark, **kw):
    return line(lambda g, t: staff(g, t), mark, **kw)


def sword_line(mark, **kw):
    return line(lambda g, t: sword(g, t), mark, **kw)


def daggers_line(mark, **kw):
    return line(lambda g, t: daggers(g, t), mark, t=T(dx=-7.0, dy=-8.0, k=0.96), **kw)


FORCE_COVER = Ramp("#200608", "#4A0E12", "#741A1C", "#9A2A26", "#BE4234")
PRECISION_COVER = Ramp("#14062A", "#2A0E52", "#44187E", "#5E26A6", "#7C3CCC")
REACH_COVER = Ramp("#031C22", "#063A44", "#0A5A68", "#127C8C", "#1E9CAE")
TEMPO_COVER = Ramp("#060E2E", "#0C1C56", "#162E82", "#2444A8", "#3A60CC")
VITALITY_COVER = Ramp("#041A0C", "#0A3418", "#125226", "#1C7034", "#2A9046")

BLUE_HALO, VIOLET_HALO, GOLD_HALO, EMBER_HALO = hexc("#2A6CFF"), hexc("#8A40FF"), hexc("#FFB030"), hexc("#FF6A20")
RED_HALO, GREEN_HALO, CYAN_HALO, LIME_HALO = hexc("#FF3048"), hexc("#30D060"), hexc("#20C8E0"), hexc("#A0D020")


MOLTEN = Ramp("#1E0408", "#4A0A12", "#86161A", "#C8361A", "#F86A22", "#FFA846", "#FFDE94", "#FFFBEA")


def crescent(cx, cy, r, thick, a0, a1):
    """A swing's arc: a ring thickest at its middle, tapering to points at both ends (angles in degrees, y down)."""
    def f(x, y):
        dx, dy = x - cx, y - cy
        rr = np.hypot(dx, dy)
        ang = np.degrees(np.arctan2(dy, dx))
        k = ((ang - a0) % 360) / max(1e-3, (a1 - a0) % 360)
        inside = _between(ang, a0, a1)
        w = thick * np.clip(np.sin(np.clip(k, 0, 1) * math.pi), 0, 1) ** 0.8
        return inside & (rr <= r) & (rr >= r - w), dict(k=k)
    return f


def ruinous_edge(g):
    """Ruinous Edge: the Tempered Edge's own sword with its blade run molten and cracked, its swing grown into a burning
    crescent, under a radiance."""
    radiance(g, EMBER_HALO, strength=0.24, halo=0.20)
    im, m = legacy_glyph("TemperedEdge", 84)
    ys, xs = np.nonzero(m)
    ox, oy = int(round(44 - (xs.min() + xs.max()) / 2)), int(round(50 - (ys.min() + ys.max()) / 2))
    im, m = shift(im, ox, oy), shift(m, ox, oy)
    blue = m & (im[..., 2] > im[..., 0] + 0.08)
    gold = m & (im[..., 0] > im[..., 2] + 0.12)
    # The guard: the largest run of gold (the pommel has a gold collar of its own).
    lab, count = ndimage.label(gold)
    guard = lab == (int(np.argmax(ndimage.sum(gold, lab, range(1, count + 1)))) + 1)
    gx, gy = CX[guard].mean(), CY[guard].mean()
    d = unit((1, -1))
    xs_ = unit((1, 1))
    along_blade = (CX - gx) * d[0] + (CY - gy) * d[1]
    bl = blue & (along_blade > 2.5)
    pommel = blue & (along_blade < -12.0)
    cols = recolour(im, bl, MOLTEN, 0.12, 1.0)
    cols = recolour(cols, pommel, EMBER, 0.1, 0.95)
    # The crescent behind the sword, on its swing side: round a point up and left of the blade's middle.
    mid = (float(CX[bl].mean()), float(CY[bl].mean()))
    cc = mid
    arc = crescent(cc[0], cc[1], 22.0, 6.5, -24, 114)
    am = mask_of(arc)
    _, ae = arc(CX, CY)
    acols = colours(EMBER, np.where(np.abs(ae["k"] - 0.5) < 0.22, 5, 4))
    part(g, am & ~m, acols, EMBER, inline=2, emit=1.2)
    g.paint(m, cols, separate=True, emit=0.0)
    # Cracks of molten light down the blade: a zigzag along its ridge.
    pts = [along((gx, gy), d, k) for k in (6, 13, 20, 28, 36, 44)]
    xs = unit((1, 1))
    pts = [along(p, xs, w) for p, w in zip(pts, (1.4, -1.8, 1.6, -1.6, 1.4, -0.8))]
    for p0, p1 in zip(pts[:-1], pts[1:]):
        g.paint(mask_of(blade(p0, p1, [0.55])) & bl & ~edge_of(m, 2), MOLTEN[7], separate=False, emit=1.5)
    g.emit[bl] = np.maximum(g.emit[bl], cols[bl] * 0.35)
    for sx, sy, a_ in ((84, 52, 2), (64, 86, 2), (84, 28, 1), (14, 30, 1)):
        g.sparkle(sx, sy, a_, EMBER[5], emit=1.4)


GOLD_IRON = Ramp("#200E04", "#4A2606", "#7A440C", "#B06A14", "#DE9A26", "#FCC948", "#FFEC9A", "#FFFBE4")
GHOST = Ramp("#160C08", "#2C1A0C", "#4A2E12", "#6C461C", "#906228", "#B47E36")


def blurred_grip(g):
    """Blurred Grip: the Quickened Grip's own gauntlet worked in gold, punching so fast its outline is left behind it
    twice in glowing amber, its lightning turned to ember, under a radiance."""
    radiance(g, GOLD_HALO, strength=0.22, halo=0.18)
    im, m = legacy_glyph("QuickenedGrip", 74)
    ys, xs = np.nonzero(m)
    trail = 8
    # The fist and its two echoes, a step apart down to the bottom left, centred together in the icon.
    ox = int(round(48 - (xs.min() - 2 * trail + xs.max()) / 2))
    oy = int(round(48 - (ys.min() + ys.max() + 2 * trail) / 2))
    fm = shift(m, ox, oy)
    for k, step in enumerate((2, 1)):
        echo = shift(m, ox - step * trail, oy + step * trail)
        line = edge_of(echo, 2) & ~fm
        g.paint(line, GHOST[3 + k], separate=False, emit=0.7)
    fcols = recolour(shift(im, ox, oy), fm, GOLD_IRON, 0.0, 1.0)
    g.paint(fm, fcols, separate=True)
    for bx, by, r in ((76, 72, 8.0), (86, 58, 6.5)):
        bolts(g, bx, by, r, ramp=EMBER, emit=1.3, pair=False)
    for sx, sy, a_ in ((84, 14, 2), (14, 20, 1), (60, 88, 1)):
        g.sparkle(sx, sy, a_, GOLD[5], emit=1.4)




def ironblood(g):
    """Ironblood: the Vital Surge's heart forged in iron plates bound in gold, blood running molten in the seams between
    them, under a radiance."""
    radiance(g, hexc("#FF4050"), strength=0.62, halo=0.40, r1=48.0, halo_r=28.0)
    m = heart(g, 48, 50, 34, ramp=IRON, emit=0.0, face=3)
    # The gold binding round its edge, lit where it faces the light.
    band_ = edge_of(m, 3) & ~edge_of(m, 1)
    ys, xs = np.nonzero(m)
    facing = -((CX - xs.mean()) * LXY[0] + (CY - ys.mean()) * LXY[1])
    g.paint(band_, colours(GOLD, np.where(facing > 0, 5, 3)), separate=False)
    # Molten seams between the plates, and rivets along them.
    for p0, p1 in (((48, 24), (48, 84)), ((18, 44), (78, 44))):
        seam = mask_of(blade(p0, p1, [1.6])) & m & ~edge_of(m, 3)
        g.paint(seam, RED[4], separate=False, emit=1.6)
        core = mask_of(blade(p0, p1, [0.6])) & seam
        g.paint(core, RED[6], separate=False, emit=1.8)
    for px, py in ((36, 36), (60, 36), (36, 56), (60, 56), (48, 70)):
        rv = ellipse(px, py, 2.3)
        part(g, rv, skin_dome(rv, GOLD, 3, 2.0, glint=False), GOLD)
    for sx, sy, a_ in ((82, 16, 3), (14, 22, 2), (84, 76, 2), (18, 80, 1)):
        g.sparkle(sx, sy, a_, RED[5], emit=1.5)


def vital_surge(g):
    heart(g, 46, 52, 33)
    # A white cross rising from it.
    cx, cy = 70, 26
    plus = union(blade((cx - 8, cy), (cx + 8, cy), [3.0]), blade((cx, cy - 8), (cx, cy + 8), [3.0]))
    pm = mask_of(plus)
    part(g, pm, skin_pillow(pm, PALE, 4, 1.2, hi=5, lo=3), PALE, inline=1, emit=0.8)


def keen_edge(g):
    info = sword(g, T(dx=-6, dy=6, k=0.86))
    crit_star(g, 66, 30, 20)


def splintering_crit(g):
    crit_star(g, 46, 50, 24)
    for ang, dist, sz in ((-30, 34, 5.0), (40, 33, 4.4), (115, 34, 4.8), (200, 32, 4.2), (250, 35, 3.8)):
        a = math.radians(ang)
        cx, cy = 46 + math.cos(a) * dist, 50 + math.sin(a) * dist
        d = (math.cos(a), math.sin(a))
        x = (-d[1], d[0])
        pts = [along((cx, cy), d, sz), along((cx, cy), x, sz * 0.5), along((cx, cy), d, -sz * 0.8), along((cx, cy), x, -sz * 0.6)]
        sm = mask_of(polygon(pts))
        part(g, sm, skin_pillow(sm, GOLD, 4, 1, hi=5, lo=3), GOLD, inline=2, emit=0.9)
    # The shards' trails back to the star.
    for ang in (-30, 40, 115, 200, 250):
        a = math.radians(ang)
        p0 = (46 + math.cos(a) * 18, 50 + math.sin(a) * 18)
        p1 = (46 + math.cos(a) * 27, 50 + math.sin(a) * 27)
        g.paint(mask_of(blade(p0, p1, [0.6])) & ~g.solid, GOLD[4], separate=False, emit=1.0)


def long_reach(g):
    spear(g)
    range_arrow(g, 70, 70, 17, angle=-45)


def forge_pulse(g):
    hammer(g, T(k=0.98))


def iron_skin(g):
    shield(g)


def fortune(g):
    clover(g)


def swift_boots(g):
    boot(g, T(dx=-3.0, k=0.97))


def deep_lungs(g):
    lungs(g)


ICONS = [
    # (id, draw, halo colour, evolved)
    ("weapon_sword", lambda g: sword(g, T(k=1.08)), BLUE_HALO, False),
    ("line_sword_damage", sword_line(burst), BLUE_HALO, False),
    ("line_sword_area", sword_line(rings), BLUE_HALO, False),
    ("line_sword_range", sword_line(range_arrow), BLUE_HALO, False),
    ("evolution_sword", resonant_edge, GOLD_HALO, True),
    ("weapon_staff", lambda g: staff(g), VIOLET_HALO, False),
    ("line_staff_damage", staff_line(burst), VIOLET_HALO, False),
    ("line_staff_area", staff_line(rings), VIOLET_HALO, False),
    ("line_staff_attack_speed", staff_line(bolts), VIOLET_HALO, False),
    ("evolution_staff", cinder_nova, EMBER_HALO, True),
    ("weapon_daggers", lambda g: daggers(g, T(k=1.24)), BLUE_HALO, False),
    ("line_daggers_damage", daggers_line(burst), BLUE_HALO, False),
    ("line_daggers_crit", daggers_line(crit_star), BLUE_HALO, False),
    ("line_daggers_attack_speed", daggers_line(bolts), BLUE_HALO, False),
    ("evolution_daggers", rift_echo, VIOLET_HALO, True),
    ("tome_force", lambda g: tome(g, FORCE_COVER, burst), RED_HALO, False),
    ("tome_precision", lambda g: tome(g, PRECISION_COVER, crit_star), GOLD_HALO, False),
    ("tome_reach", lambda g: tome(g, REACH_COVER, rings), CYAN_HALO, False),
    ("tome_tempo", lambda g: tome(g, TEMPO_COVER, bolts), BLUE_HALO, False),
    ("tome_vitality", lambda g: tome(g, VITALITY_COVER, heart), RED_HALO, False),
    ("upgrade_armor", iron_skin, BLUE_HALO, False),
    ("upgrade_attack_speed_evolved", blurred_grip, GOLD_HALO, True),
    ("upgrade_crit", keen_edge, GOLD_HALO, False),
    ("upgrade_crit_splash", splintering_crit, GOLD_HALO, False),
    ("upgrade_damage_evolved", ruinous_edge, EMBER_HALO, True),
    ("upgrade_health", vital_surge, RED_HALO, False),
    ("upgrade_health_evolved", ironblood, RED_HALO, True),
    ("upgrade_luck", fortune, GREEN_HALO, False),
    ("upgrade_move_speed", swift_boots, CYAN_HALO, False),
    ("upgrade_pulse", forge_pulse, EMBER_HALO, False),
    ("upgrade_range", long_reach, LIME_HALO, False),
    ("upgrade_stamina", deep_lungs, CYAN_HALO, False),
    # The hall's two legacy cards, their own painted glyphs without the navy square (legacy_card); appended, so the grid
    # moves none of the icons before them.
    ("upgrade_damage", legacy_card("TemperedEdge", 96), BLUE_HALO, False),
    ("upgrade_attack_speed", legacy_card("QuickenedGrip", 90), BLUE_HALO, False),
]


# ----------------------------------------------------------------------------------------------------------------------
# Rendering: each icon for its card (144 square, on the navy, exported with the field's light as alpha), and the
# weapons, their evolutions and the tomes once more for the loadout row's sockets: the glyph cropped close and filtered
# straight down to the socket's 56 from the same painting, its glow kept low, so it reads at a third of the card's size
# without a mip chain.

SLOT = 56
# The ids whose icon the loadout row shows: what a weapon socket holds (a weapon, or its evolution once taken) and what a
# tome socket holds.
SLOT_IDS = ("weapon_sword", "evolution_sword", "weapon_staff", "evolution_staff", "weapon_daggers", "evolution_daggers",
            "tome_force", "tome_precision", "tome_reach", "tome_tempo", "tome_vitality")


def paint_icon(draw):
    g = Glyph()
    draw(g)
    g.outline()
    g.centre()
    return g


def render_card(g, halo, evolved):
    work, cover = compose(g, halo, 0.34 if evolved else 0.28, 1.15 if evolved else 1.0)
    a, b = LO * UP, (N - LO) * UP
    final = downsample(work[a:b, a:b], CARD)
    return to_alpha(final, downsample(cover[a:b, a:b], CARD)), final


def render_slot(g, halo, evolved):
    """The glyph cropped to its square bounds plus two cells, its glow at half strength, box-filtered to SLOT."""
    work, cover = compose(g, halo, 0.16 if evolved else 0.12, 0.7 if evolved else 0.55)
    ys, xs = np.nonzero(g.solid)
    side = max(xs.max() - xs.min(), ys.max() - ys.min()) + 1 + 4
    cx, cy = (xs.min() + xs.max() + 1) / 2.0, (ys.min() + ys.max() + 1) / 2.0
    x0 = int(round((cx - side / 2.0) * UP))
    y0 = int(round((cy - side / 2.0) * UP))
    size = side * UP
    pad = size
    big = np.pad(work, ((pad, pad), (pad, pad), (0, 0)), mode="edge")
    bc = np.pad(cover, pad)
    crop = big[y0 + pad:y0 + pad + size, x0 + pad:x0 + pad + size]
    ccrop = bc[y0 + pad:y0 + pad + size, x0 + pad:x0 + pad + size]
    final = downsample(crop, SLOT)
    rgba = to_alpha(final, downsample(ccrop, SLOT))
    # The crop's edge cuts the glow: fade it out over the last two texels so the socket shows no square.
    fade = edge_fade(SLOT, 2.0)
    rgba[..., 3] = np.maximum(rgba[..., 3] * fade, np.where(downsample(ccrop, SLOT) > 0.5, rgba[..., 3], 0))
    return rgba, final


def build_icons(only=None):
    """id -> dict(card=RGBA, card_final=RGB over the navy, slot=RGBA or None, evolved)."""
    built = {}
    for name, draw, halo, evolved in ICONS:
        if only and name not in only:
            continue
        g = paint_icon(draw)
        try:
            card, final = render_card(g, halo, evolved)
        except ValueError as error:
            raise ValueError(f"{name}: {error}") from None
        slot = render_slot(g, halo, evolved)[0] if name in SLOT_IDS else None
        built[name] = dict(card=card, card_final=final, slot=slot, evolved=evolved)
    return built


def to_bytes(rgba):
    return (np.clip(rgba, 0, 1) * 255.0 + 0.5).astype(np.uint8)


# ----------------------------------------------------------------------------------------------------------------------
# The sheet: the card icons on a fixed grid, seven across, two clear texels between cells; the socket icons in the last
# row's free cells. A grid rather than a packer, so an icon added later moves none of the others.

OUT_DIR = os.path.join(ROOT, "Assets", "_Project", "Art", "UI", "UpgradeIcons")
SHEET = "UI_UpgradeIcons"
SCRIPT_META = os.path.join(ROOT, "Assets", "_Project", "Scripts", "Art", "Unity", "UpgradeIconSet.cs.meta")
SET_PATH = os.path.join(OUT_DIR, "UpgradeIconSet.asset")
TPU = 100.0  # the canvas's reference pixels per unit, as the HUD's sheet
GUTTER = 2
COLUMNS = 7


def layout(card_ids, slot_ids):
    """Each sprite's rect (x, y from the sheet's top left, w, h) and the sheet's size."""
    rects = {}
    for i, name in enumerate(card_ids):
        r, c = divmod(i, COLUMNS)
        rects[name] = (c * (CARD + GUTTER), r * (CARD + GUTTER), CARD, CARD)
    used = len(card_ids)
    rows = (used + COLUMNS - 1) // COLUMNS
    last_row, first_free = divmod(used, COLUMNS)
    if first_free == 0:
        last_row, first_free = rows, 0
        rows += 1
    x0 = first_free * (CARD + GUTTER)
    y0 = last_row * (CARD + GUTTER)
    width = COLUMNS * (CARD + GUTTER) - GUTTER
    per_row = (width - x0 + GUTTER) // (SLOT + GUTTER)
    height = rows * (CARD + GUTTER) - GUTTER
    if per_row * ((CARD + GUTTER) // (SLOT + GUTTER)) < len(slot_ids):
        # Too few free cells left in the last row (34 card icons leave one): the socket icons get a strip of their own
        # under the grid, as many rows of them as they need.
        x0, y0 = 0, rows * (CARD + GUTTER)
        per_row = (width + GUTTER) // (SLOT + GUTTER)
        height = y0 + -(-len(slot_ids) // per_row) * (SLOT + GUTTER) - GUTTER
    for i, name in enumerate(slot_ids):
        r, c = divmod(i, per_row)
        rects[name + "_slot"] = (x0 + c * (SLOT + GUTTER), y0 + r * (SLOT + GUTTER), SLOT, SLOT)
    return rects, ((width + 3) // 4 * 4, (height + 3) // 4 * 4)


def bleed(sheet):
    """Colour pulled into the clear texels from the nearest painted one, so neither bilinear filtering nor an ASTC block
    straddling a sprite's edge pulls in black."""
    known = sheet[..., 3] > 0
    if known.any() and not known.all():
        _, (iy, ix) = ndimage.distance_transform_edt(~known, return_indices=True)
        sheet = sheet.copy()
        sheet[..., :3] = sheet[iy, ix, :3]
    return sheet


def existing_guid(path):
    if os.path.exists(path):
        m = re.search(r"^guid: ([0-9a-f]{32})", open(path, encoding="utf-8").read(), re.M)
        if m:
            return m.group(1)
    return uuid.uuid4().hex


def existing_ids(meta_path):
    """name -> (internalID, spriteID) of the sprites an existing meta holds, so a repaint keeps every reference."""
    ids = {}
    if os.path.exists(meta_path):
        text = open(meta_path, encoding="utf-8").read()
        for m in re.finditer(r"      name: (\S+)\n(?:.*\n)*?      spriteID: ([0-9a-f]{32})\n      internalID: (-?\d+)", text):
            ids[m.group(1)] = (int(m.group(3)), m.group(2))
    return ids


def fresh_internal_id(taken):
    while True:
        value = int.from_bytes(uuid.uuid4().bytes[:8], "little", signed=True)
        if value not in (0, 21300000) and value not in taken:
            return value


def write(path, data):
    """Bytes as they are; text with LF line ends, as every Unity file here has."""
    if isinstance(data, str):
        data = data.encode("utf-8")
    with open(path, "wb") as f:
        f.write(data)


def number(value):
    text = f"{value:.7f}".rstrip("0").rstrip(".")
    return "0" if text in ("", "-0") else text


# The painted HUD's sheet import (Art/UI/Hud/UI_Hud.png.meta): bilinear, no mipmaps, ASTC 4x4 on Android at full size,
# alpha as transparency, Multiple sprites.
TEXTURE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable:
{table}  externalObjects: {{}}
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
  spriteMode: 2
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
    sprites:
{sprites}    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: 
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable:
{names}  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""

SPRITE_META = """    - serializedVersion: 2
      name: {name}
      rect:
        serializedVersion: 2
        x: {x}
        y: {y}
        width: {w}
        height: {h}
      alignment: 9
      pivot: {{x: 0.5, y: 0.5}}
      border: {{x: 0, y: 0, z: 0, w: 0}}
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


def astc_bytes(width, height):
    """ASTC 4x4: 16 bytes a 4x4 block, a byte a texel."""
    return ((width + 3) // 4) * ((height + 3) // 4) * 16


def build(frames=None):
    if not os.path.exists(SCRIPT_META):
        raise SystemExit(f"{SCRIPT_META} is missing: the set needs its script's GUID.")
    built = build_icons()
    card_ids = [name for name, *_ in ICONS]
    slot_ids = [name for name in SLOT_IDS if name in built]
    rects, (width, height) = layout(card_ids, slot_ids)
    sheet = np.zeros((height, width, 4), np.uint8)
    for name in card_ids:
        x, y, w, h = rects[name]
        sheet[y:y + h, x:x + w] = to_bytes(built[name]["card"])
    for name in slot_ids:
        x, y, w, h = rects[name + "_slot"]
        sheet[y:y + h, x:x + w] = to_bytes(built[name]["slot"])
    sheet = bleed(sheet)

    os.makedirs(OUT_DIR, exist_ok=True)
    folder_meta = OUT_DIR + ".meta"
    write(folder_meta, FOLDER_META.format(guid=existing_guid(folder_meta)))
    png = os.path.join(OUT_DIR, SHEET + ".png")
    import io
    buf = io.BytesIO()
    Image.fromarray(sheet, "RGBA").save(buf, format="PNG", optimize=True)
    write(png, buf.getvalue())

    meta = png + ".meta"
    guid = existing_guid(meta)
    ids = existing_ids(meta)
    taken = {v[0] for v in ids.values()}
    names = [f"{SHEET}_{key}" for key in card_ids] + [f"{SHEET}_{key}_slot" for key in slot_ids]
    for n in names:
        if n not in ids:
            ids[n] = (fresh_internal_id(taken), uuid.uuid4().hex)
            taken.add(ids[n][0])
    keys = card_ids + [k + "_slot" for k in slot_ids]
    table = "".join(f"  - first:\n      213: {ids[f'{SHEET}_{k}'][0]}\n    second: {SHEET}_{k}\n" for k in keys)
    sprites = ""
    for k in keys:
        x, y, w, h = rects[k]
        sprites += SPRITE_META.format(name=f"{SHEET}_{k}", x=x, y=height - y - h, w=w, h=h,
                                      sprite_id=ids[f"{SHEET}_{k}"][1], internal_id=ids[f"{SHEET}_{k}"][0])
    name_table = "".join(f"      {SHEET}_{k}: {ids[f'{SHEET}_{k}'][0]}\n" for k in keys)
    write(meta, TEXTURE_META.format(guid=guid, table=table, tpu=number(TPU), sprites=sprites, names=name_table))

    script_guid = existing_guid(SCRIPT_META)
    body = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:", "--- !u!114 &11400000", "MonoBehaviour:",
            "  m_ObjectHideFlags: 0", "  m_CorrespondingSourceObject: {fileID: 0}", "  m_PrefabInstance: {fileID: 0}",
            "  m_PrefabAsset: {fileID: 0}", "  m_GameObject: {fileID: 0}", "  m_Enabled: 1", "  m_EditorHideFlags: 0",
            f"  m_Script: {{fileID: 11500000, guid: {script_guid}, type: 3}}", "  m_Name: UpgradeIconSet",
            "  m_EditorClassIdentifier: ", "  _icons:"]
    for k in card_ids:
        body.append(f"  - _id: {k}")
        body.append(f"    _card: {{fileID: {ids[f'{SHEET}_{k}'][0]}, guid: {guid}, type: 3}}")
        if k in slot_ids:
            body.append(f"    _slot: {{fileID: {ids[f'{SHEET}_{k}_slot'][0]}, guid: {guid}, type: 3}}")
        else:
            body.append("    _slot: {fileID: 0}")
    write(SET_PATH, "\n".join(body) + "\n")
    write(SET_PATH + ".meta", ASSET_META.format(guid=existing_guid(SET_PATH + ".meta")))

    size = astc_bytes(width, height)
    print(f"{SHEET}.png {width}x{height}  {len(card_ids)} card icons {CARD}x{CARD}, {len(slot_ids)} socket icons "
          f"{SLOT}x{SLOT}  ASTC 4x4 {size} bytes ({size / 1024:.1f} KiB, {size / 1048576:.3f} MiB)")
    if frames:
        os.makedirs(frames, exist_ok=True)
        for k in card_ids:
            Image.fromarray(to_bytes(built[k]["card"]), "RGBA").save(os.path.join(frames, k + ".png"))
        for k in slot_ids:
            Image.fromarray(to_bytes(built[k]["slot"]), "RGBA").save(os.path.join(frames, k + "_slot.png"))
    return built


def preview(out, only=None):
    """Every icon over the navy at the card's size, eight across, without touching the project."""
    os.makedirs(out, exist_ok=True)
    built = build_icons(only)
    finals = [b["card_final"] for b in built.values()]
    cols = 8
    rows = (len(finals) + cols - 1) // cols
    sheet = np.zeros((rows * CARD, cols * CARD, 3), np.float32) + NAVY
    for i, f in enumerate(finals):
        r, c = divmod(i, cols)
        sheet[r * CARD:(r + 1) * CARD, c * CARD:(c + 1) * CARD] = f
    Image.fromarray(to_bytes(sheet)).save(os.path.join(out, "sheet.png"))
    slots = [b["slot"] for b in built.values() if b["slot"] is not None]
    if slots:
        strip = np.zeros((SLOT + 8, len(slots) * (SLOT + 8), 3), np.float32) + hexc("#0E1322")
        for i, s in enumerate(slots):
            x = i * (SLOT + 8) + 4
            a = s[..., 3:]
            strip[4:4 + SLOT, x:x + SLOT] = s[..., :3] * a + strip[4:4 + SLOT, x:x + SLOT] * (1 - a)
        Image.fromarray(to_bytes(strip)).resize((strip.shape[1] * 2, strip.shape[0] * 2), Image.NEAREST).save(
            os.path.join(out, "slots_x2.png"))


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--frames", help="also save every sprite as a PNG in this directory")
    parser.add_argument("--preview", help="only paint a review sheet into this directory; the project is not touched")
    parser.add_argument("--only", nargs="*", help="with --preview, only these ids")
    args = parser.parse_args()
    if args.preview:
        preview(args.preview, args.only)
    else:
        build(args.frames)


if __name__ == "__main__":
    main()
