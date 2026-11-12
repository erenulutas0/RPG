"""Floor-3 (Gloam Gardens) backdrop plate, ENV_Gloam_Garden_v1.png: the garden counterpart of
ENV_Furnace_Cavern_v1.png / FoundryCavernV2.png and ENV_Ice_Vault_v1.png, at the same 1024x1536 RGB size and
composition (a chasm framed by near masses left and right, a hazy far middle, a dark lower band where the arena
meets it), painted procedurally.

v1 of the plate (promoted from the concept's v4, docs/51 section 6 and its known gaps):
- the dimmed sun is the focal point, high in the sky (the Long Dimming seen for the first time): a flat, warm
  ash-gold disc (#CDBE8E, hue ~46, never red-orange) inside a darker corona ring, a wide dull halo, ash cloud
  bands crossing its lower edge; the brightest value on the plate;
- the far middle is the abandoned foundry overgrown: towers with crenellated crowns (the gate's merlons), chimney
  stacks with flared lips, an arched aqueduct span, roots draped over all of it and leaf crowns growing out of the
  tops; fogged deep into the gloam haze;
- three great trees in the mid-ground with leaf-mass crowns (clumps lit from the sun's side) and hanging drape;
- the near frame is two giant root-trunks (bark ridges, burls, moss, lichen, a sky rim on the chasm edge)
  swallowing ruined foundry masonry, with a verdigris pipe collar (brass gone #6FA38C);
- gloamlight (#C6EE6A / #E2F7A0) only on the frame: bracket fungi with lit undersides on the trunks and
  seed-lanterns hanging from the corner roots; nothing bright in the middle, where the arena sits, so no
  chartreuse dot reads as a pickup (docs/51 section 7.5);
- the arena-overlap zone (the plate's middle) is pulled toward its local mean (contrast and detail reduced), so
  the plate sits behind the playfield without competing with it;
- no orchid (hue 280-330, the enemies' colour) and no red-orange (the telegraphs' colour) anywhere.

The game draws the plate through Resources/FoundryCavern.shader (saturation x1.08, then a 10 % blend toward violet
(.20,.15,.30)); `in_game()` reproduces that for the review images.

Writes Assets/_Project/Art/Environment/ENV_Gloam_Garden_v1.png (its .meta is hand-written beside it: the ice
plate's import settings - Sprite, 64 ppu, no mips, Android ASTC 6x6 - under its own guid); an optional argument
also saves a preview copy there.
Usage: python -I Tools/ArtReview/GenerateGloamBackdrop.py [preview.png]
See docs/51_GLOAM_GARDENS_ART_DIRECTION.md section 6.
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Environment", "ENV_Gloam_Garden_v1.png")
W, H = 1024, 1536
SS = 2  # supersampling for the polygon layers


def c(h):
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


# docs/51 palette (plus darker steps of it)
SKY_TOP, SKY_MID = c("0B110F"), c("23302A")
HAZE = c("767A68")
SUN, SUN_RIM, SUN_HALO, CORONA = c("CDBE8E"), c("B9A877"), c("8E8A62"), c("2A2D22")
FAR, FAR_DARK = c("2C362C"), c("1E2620")
MID, MID_RIM = c("1A221C"), c("6E7A5A")
BARK_DEEP, BARK, HEARTWOOD = c("14100E"), c("2A221E"), c("5B4838")
MOSS, MOSS_LIT, LICHEN = c("3F5A32"), c("4F6B3A"), c("8FA35A")
WALL, WALL_CAP = c("2C3029"), c("7C8466")
VERDIGRIS = c("6FA38C")
GLOAM, GLOAM_CORE = c("C6EE6A"), c("E2F7A0")
ASH = c("A7A99C")
BAND = c("0B0F0C")

SUN_X, SUN_Y, SUN_R = W * 0.5, H * 0.165, 84.0


def blur(a, sy, sx=None):
    """Gaussian blur through the FFT; sx != sy stretches it (bark ridges, cloud bands)."""
    sx = sy if sx is None else sx
    h, w = a.shape
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    g = np.exp(-((fx * sx) ** 2 + (fy * sy) ** 2) * 2.0 * np.pi ** 2)
    return np.real(np.fft.ifft2(np.fft.fft2(a) * g)).astype(np.float32)


def blur3(img, s):
    return np.stack([blur(img[..., k], s) for k in range(3)], -1)


def noise(rng, sy, sx=None, shape=(H, W)):
    n = blur(rng.standard_normal(shape).astype(np.float32), sy, sx)
    n -= n.mean()
    return n / (n.std() + 1e-6)


def fbm(rng, base, octaves=4, stretch=1.0):
    out, amp, tot = np.zeros((H, W), np.float32), 1.0, 0.0
    for _ in range(octaves):
        out += noise(rng, base * stretch, base) * amp
        tot += amp
        amp *= 0.55
        base /= 2.0
    return out / tot


def lerp(a, b, t):
    t = t[..., None] if np.ndim(t) == 2 else t
    return a * (1 - t) + b * t


def rgb255(col):
    return tuple(int(max(0, min(1, x)) * 255 + 0.5) for x in col)


def layer():
    rgb = Image.new("RGB", (W * SS, H * SS), (0, 0, 0))
    a = Image.new("L", (W * SS, H * SS), 0)
    return rgb, a, ImageDraw.Draw(rgb), ImageDraw.Draw(a)


def flatten(rgb, a):
    rgb = np.asarray(rgb.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255.0
    a = np.asarray(a.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255.0
    return rgb, a


def S(pts):
    return [(p[0] * SS, p[1] * SS) for p in pts]


def E(box):
    return [box[0] * SS, box[1] * SS, box[2] * SS, box[3] * SS]


def unpremultiplied(rgb, a):
    """LANCZOS-resized layers drawn on black: divide the edge darkening back out."""
    return np.clip(rgb / np.maximum(a[..., None], 1e-3), 0, 1.5) * (a[..., None] > 1e-3)


# ---------------------------------------------------------------- shapes

def strand(dr, da, rng, x0, y0, length, width, col, edge=None, sway=0.0, n=14):
    """A tapered hanging root, vine or drape strand; the lit edge on its left (the sun is up and centre)."""
    phase = rng.uniform(0, 6)
    pts = []
    for i in range(n + 1):
        t = i / n
        x = x0 + sway * math.sin(t * 2.4 + phase) * t * length * 0.22
        pts.append((x, y0 + t * length))
    left, right = [], []
    for i, (x, y) in enumerate(pts):
        wv = width * (1 - i / n) ** 0.75 + 0.5
        left.append((x - wv / 2, y))
        right.append((x + wv / 2, y))
    poly = left + right[::-1]
    dr.polygon(S(poly), fill=rgb255(col))
    da.polygon(S(poly), fill=255)
    if edge is not None and width > 2:
        dr.line(S(left[: int(n * 0.75)]), fill=rgb255(edge), width=max(1, int(SS * min(2.0, width / 4))))
    return pts[-1]


def over(dst_rgb, dst_a, src_rgb, src_a):
    """Straight-alpha 'over'."""
    out_a = src_a + dst_a * (1 - src_a)
    out = (src_rgb * src_a[..., None] + dst_rgb * (dst_a * (1 - src_a))[..., None]) / np.maximum(out_a, 1e-4)[..., None]
    return out, out_a


def foliage(rng, specs, leafy):
    """Leaf-mass crowns as a height field: a union of soft lumps (denser and bigger on top), a leafy fringe cut by
    fine noise, each lump lit on the side toward the light and shaded underneath. specs: (cx, cy, rx, ry, col, lit,
    (lx, ly), lumps). Returns a full-size (rgb, alpha) layer."""
    rgb = np.zeros((H, W, 3), np.float32)
    alpha = np.zeros((H, W), np.float32)
    for (cx, cy, rx, ry, col, lit, light, lumps) in specs:
        lx, ly = light
        ln = math.hypot(lx, ly) or 1.0
        lx, ly = lx / ln, ly / ln
        x0, x1 = max(0, int(cx - rx * 1.5)), min(W, int(cx + rx * 1.5))
        y0, y1 = max(0, int(cy - ry * 1.6)), min(H, int(cy + ry * 1.5))
        if x1 <= x0 or y1 <= y0:
            continue
        ys, xs = np.mgrid[y0:y1, x0:x1].astype(np.float32)
        f = np.zeros_like(xs)
        for _ in range(lumps):
            ang = rng.uniform(0, 2 * math.pi)
            r = math.sqrt(rng.uniform(0, 1))
            bx, by = cx + math.cos(ang) * rx * r, cy + math.sin(ang) * ry * r
            by -= max(0.0, by - cy) * 0.35  # a flatter underside
            s = (rx + ry) * 0.5 * rng.uniform(0.09, 0.17) * (1.2 - 0.45 * r)
            f += np.exp(-(((xs - bx) / s) ** 2 + ((ys - by) / (s * 0.82)) ** 2) * 0.5)
        f = 1 - np.exp(-f * 1.3)  # a saturating union
        fringe = leafy[y0:y1, x0:x1]
        m = np.clip((f + fringe * 0.16 - 0.5) / 0.05, 0, 1)
        hgt = f + fringe * 0.05
        gy, gx = np.gradient(hgt)
        lam = np.clip(0.45 + (-gx * lx - gy * ly) * 9.0, 0, 1)
        under = np.clip((ys - cy) / ry, 0, 1)
        tone = lerp(col * 0.6, lit, lam ** 2.0 * 0.7) * (1 - 0.32 * under)[..., None]
        tone = tone * (0.92 + 0.16 * np.clip(fringe * 0.5 + 0.5, 0, 1))[..., None]
        sub_rgb, sub_a = over(rgb[y0:y1, x0:x1], alpha[y0:y1, x0:x1], tone, m)
        rgb[y0:y1, x0:x1], alpha[y0:y1, x0:x1] = sub_rgb, sub_a
    return rgb, alpha


def drapes(dr, da, rng, cx, cy, rx, ry, col, count):
    """Moss and root drape hanging from a crown's underside: short, thin, tapered."""
    for _ in range(count):
        t = rng.uniform(-0.85, 0.85)
        x0 = cx + t * rx
        y0 = cy + ry * 0.45 * math.sqrt(max(0.0, 1 - t * t))
        strand(dr, da, rng, x0, y0, rng.uniform(0.25, 0.8) * ry, rng.uniform(1.2, 2.6), col * rng.uniform(0.85, 1.1),
               None, rng.uniform(-0.5, 0.5), n=10)


def foundry_tower(dr, da, rng, x, base, w, h, col, rim, kind):
    """A ruined foundry tower silhouette: a battered shaft, then a crenellated crown (the gate's merlons), a chimney
    stack with a flared lip, or a broken gable; dark arched openings; the sun-side edge rimmed."""
    tone = col * rng.uniform(0.92, 1.08)
    top = base - h
    batter = w * 0.08
    shaft = [(x - w / 2 - batter, base), (x - w / 2, top), (x + w / 2, top), (x + w / 2 + batter, base)]
    dr.polygon(S(shaft), fill=rgb255(tone))
    da.polygon(S(shaft), fill=255)
    # a string course and a corbelled parapet
    dr.rectangle(E((x - w / 2 - 3, top + h * 0.06, x + w / 2 + 3, top + h * 0.06 + 3)), fill=rgb255(tone * 1.15))
    da.rectangle(E((x - w / 2 - 3, top + h * 0.06, x + w / 2 + 3, top + h * 0.06 + 3)), fill=255)
    crown_top = top
    if kind == "crenel":
        mw = w / 5.0
        dr.rectangle(E((x - w / 2 - 4, top - 6, x + w / 2 + 4, top + 2)), fill=rgb255(tone))
        da.rectangle(E((x - w / 2 - 4, top - 6, x + w / 2 + 4, top + 2)), fill=255)
        for k in range(5):
            if k % 2 == 0 and not (k == 4 and rng.random() < 0.5):  # one merlon broken off
                bx = x - w / 2 - 4 + k * (w + 8) / 5.0
                mh = rng.uniform(9, 14)
                dr.rectangle(E((bx, top - 6 - mh, bx + mw, top - 5)), fill=rgb255(tone))
                da.rectangle(E((bx, top - 6 - mh, bx + mw, top - 5)), fill=255)
        crown_top = top - 18
    elif kind == "chimney":
        cw = w * 0.34
        cx = x + rng.uniform(-0.2, 0.2) * w
        ch = h * rng.uniform(0.35, 0.55)
        stack = [(cx - cw / 2, top), (cx - cw * 0.42, top - ch), (cx + cw * 0.42, top - ch), (cx + cw / 2, top)]
        dr.polygon(S(stack), fill=rgb255(tone * 0.95))
        da.polygon(S(stack), fill=255)
        lip = (cx - cw * 0.62, top - ch - 7, cx + cw * 0.62, top - ch + 1)
        dr.rectangle(E(lip), fill=rgb255(tone * 1.05))
        da.rectangle(E(lip), fill=255)
        if rng.random() < 0.5:  # a second, shorter stack
            c2 = cx + (w * 0.33 if cx < x else -w * 0.33)
            ch2 = ch * 0.55
            dr.polygon(S([(c2 - cw * 0.35, top), (c2 - cw * 0.3, top - ch2), (c2 + cw * 0.3, top - ch2), (c2 + cw * 0.35, top)]),
                       fill=rgb255(tone * 0.9))
            da.polygon(S([(c2 - cw * 0.35, top), (c2 - cw * 0.3, top - ch2), (c2 + cw * 0.3, top - ch2), (c2 + cw * 0.35, top)]), fill=255)
        crown_top = top - ch * 0.4
    else:  # broken gable
        peak = (x + rng.uniform(-0.15, 0.15) * w, top - w * 0.55)
        brk = (x + w * 0.22, top - w * 0.18)
        gable = [(x - w / 2 - 2, top), peak, (peak[0] + w * 0.1, peak[1] + 10), brk, (x + w * 0.3, top - w * 0.1), (x + w / 2 + 2, top)]
        dr.polygon(S(gable), fill=rgb255(tone))
        da.polygon(S(gable), fill=255)
        crown_top = top - w * 0.3
    # arched openings
    rows = rng.integers(2, 4)
    for k in range(rows):
        oy = top + h * (0.18 + 0.24 * k)
        n = 1 if w < 46 else 2
        for j in range(n):
            ox = x + (j - (n - 1) / 2) * w * 0.38
            ow, oh = w * (0.22 if n == 2 else 0.3), h * 0.10
            dark = rgb255(tone * 0.72)
            dr.rectangle(E((ox - ow / 2, oy, ox + ow / 2, oy + oh)), fill=dark)
            dr.pieslice(E((ox - ow / 2, oy - ow / 2, ox + ow / 2, oy + ow / 2)), 180, 360, fill=dark)
    # sun-side rim (the sun is centre-high: the edge facing the middle)
    side = 1 if x < W / 2 else -1
    ex = x + side * w / 2
    dr.line(S([(ex, top + 2), (ex + side * batter, base)]), fill=rgb255(rim), width=SS)
    dr.line(S([(x - w / 2, top), (x + w / 2, top)]), fill=rgb255(rim * 0.9), width=SS)
    return crown_top


def aqueduct(dr, da, rng, x0, x1, y, depth, col, rim):
    """An arched span between two towers (the foundry plate's bridges), its arches open to the haze."""
    dr.rectangle(E((x0, y, x1, y + depth)), fill=rgb255(col))
    da.rectangle(E((x0, y, x1, y + depth)), fill=255)
    dr.line(S([(x0, y), (x1, y)]), fill=rgb255(rim), width=SS)
    n = max(2, int((x1 - x0) / 38))
    span = (x1 - x0) / n
    for k in range(n):
        ax = x0 + span * (k + 0.5)
        aw = span * 0.62
        top = y + depth * 0.38
        dr.rectangle(E((ax - aw / 2, top + aw / 2, ax + aw / 2, y + depth + 2)), fill=(0, 0, 0))
        da.rectangle(E((ax - aw / 2, top + aw / 2, ax + aw / 2, y + depth + 2)), fill=0)
        dr.pieslice(E((ax - aw / 2, top, ax + aw / 2, top + aw)), 180, 360, fill=(0, 0, 0))
        da.pieslice(E((ax - aw / 2, top, ax + aw / 2, top + aw)), 180, 360, fill=0)
    # piers continue down into the haze
    for k in range(n + 1):
        px = x0 + span * k
        pw = span * 0.19
        drop = rng.uniform(60, 140)
        dr.rectangle(E((px - pw, y + depth, px + pw, y + depth + drop)), fill=rgb255(col * 0.95))
        da.rectangle(E((px - pw, y + depth, px + pw, y + depth + drop)), fill=255)


# ---------------------------------------------------------------- near trunks

def near_trunks(rng, xx, yy, v):
    """Two giant root-trunks framing the chasm; returns colour, mask and distance-to-edge."""
    y = np.arange(H, dtype=np.float32)
    out = []
    for side in (0, 1):
        base = W * (0.205 if side == 0 else 0.795)
        wav = (np.sin(y / 230 + rng.uniform(0, 6)) * 24 + np.sin(y / 71 + rng.uniform(0, 6)) * 8
               + noise(rng, 8, 8, (1, H))[0] * 4)
        flare = np.clip((y / H - 0.68) / 0.24, 0, 1) ** 2 * 180  # root buttresses at the foot
        bough = np.clip((0.13 - y / H) / 0.13, 0, 1) ** 1.6 * (80 if side == 0 else 55)  # leaning into a bough
        sign = 1 if side == 0 else -1
        out.append(base + sign * (wav + flare + bough))
    left_e, right_e = out[0][:, None], out[1][:, None]
    left_m = np.clip((left_e - xx) / 1.5, 0, 1)
    right_m = np.clip((xx - right_e) / 1.5, 0, 1)
    mask = np.maximum(left_m, right_m)
    edge_d = np.where(xx < W / 2, left_e - xx, xx - right_e)
    roundness = np.clip(edge_d / 170.0, 0, 1)
    lit = np.exp(-np.clip(edge_d, 0, None) / 60.0)
    # bark: long vertical ridges with a slow lateral wander, deep furrows between
    warp = blur(rng.standard_normal((H, W)).astype(np.float32), 120) * 900
    ridge_src = noise(rng, 30, 2.4) * 0.6 + noise(rng, 70, 6) * 0.4
    ridge = np.take_along_axis(ridge_src, np.clip((xx + warp).astype(np.int32), 0, W - 1), 1)
    furrow = np.clip(-ridge - 0.5, 0, 1)
    plate = np.clip(ridge * 0.7 + 0.5, 0, 1)
    tone = np.clip(0.45 + ridge * 0.22 + fbm(rng, 18, 3) * 0.06, 0, 1)
    col = lerp(BARK_DEEP, BARK, tone)
    col = lerp(col, HEARTWOOD * 0.92, np.clip(lit * (0.55 + plate * 0.45), 0, 1) * 0.7)
    col = lerp(col, BARK_DEEP * 0.55, furrow * 0.85)
    # the trunk is a braid of great root cords twisting slowly up it: each cord round (lit on its chasm side, a dark
    # groove between cords), so the frame has volume instead of a flat streaked slab
    wander = (np.sin(yy / 160.0 + xx / 140.0) * 30 + blur(rng.standard_normal((H, W)).astype(np.float32), 60) * 2400)
    phase = (xx + wander) / 74.0
    f = phase - np.floor(phase)
    toward = np.where(xx < W / 2, f, 1 - f)  # 0 on the far side of a cord, 1 on its chasm side
    round_ = np.sin(np.pi * f)
    groove = np.exp(-(np.minimum(f, 1 - f) / 0.07) ** 2)
    cord = (0.72 + 0.38 * round_ * (0.45 + 0.55 * toward))
    col = col * cord[..., None]
    col = lerp(col, BARK_DEEP * 0.45, groove * 0.75)
    col = col * (1 - roundness[..., None] * 0.38)
    # burls: dark rings with a lit upper lip
    for _ in range(5):
        side = rng.random() < 0.5
        bx = rng.uniform(40, 150) if side else rng.uniform(W - 150, W - 40)
        by = rng.uniform(260, 1000)
        rr = rng.uniform(16, 26)
        d = np.sqrt(((xx - bx) / 1.0) ** 2 + ((yy - by) / 1.7) ** 2)
        ring = np.exp(-((d - rr) / 5.0) ** 2) * np.clip((yy - by) / rr + 0.6, 0, 1)  # shadowed lower lip only
        swell = np.clip(1 - d / (rr * 1.3), 0, 1) * np.clip((by - yy) / rr + 0.5, 0, 1)
        col = lerp(col, BARK_DEEP * 0.7, ring * 0.45 * mask)
        col = lerp(col, HEARTWOOD * 0.7, swell * 0.35 * mask)
    # moss on the ridges, heavier low and at the bough; lichen specks
    m = fbm(rng, 34, 3, 1.3) + 0.3 * np.clip(ridge, 0, 1) + 0.75 * np.clip((v - 0.6) / 0.3, 0, 1) \
        + 0.5 * np.clip((0.1 - v) / 0.1, 0, 1) - 0.72
    moss = np.clip(m * 1.4, 0, 1) * (1 - furrow)
    moss_col = lerp(MOSS * 0.66, MOSS_LIT * 0.85, np.clip(lit * 1.5, 0, 1)) * (0.72 + 0.28 * tone[..., None])
    col = lerp(col, moss_col, moss * 0.62)
    lich = (rng.random((H, W)) > 0.9973).astype(np.float32)
    lich = np.clip(blur(lich, 1.5) * 9, 0, 1) * np.clip(m + 0.3, 0, 1)
    col = lerp(col, LICHEN * 0.75, lich * 0.6)
    # rim where the trunk silhouette meets the gloam (sky light), brighter high up toward the sun
    rim = np.exp(-np.clip(edge_d, 0, None) / 3.2) * (0.5 + 0.5 * np.clip(fbm(rng, 25, 2) + 0.5, 0, 1))
    rim_col = lerp(MID_RIM * 1.15, SUN_RIM * 0.8, np.clip(1 - v * 2.2, 0, 1))
    col = lerp(col, rim_col, rim * mask * 0.7)
    return col, mask, edge_d


def swallowed_masonry(rng, img, tr_m, edge_d, xx, yy):
    """Ruined foundry walls the trunks have grown around: coursed blocks showing between root buttresses near the
    feet, mossed on their tops, with a verdigris pipe collar half-buried in bark."""
    blocks = np.zeros((H, W), np.float32)
    shade = np.zeros((H, W), np.float32)
    rgb, a, dr, da = layer()
    # one wall stump in each trunk's foot, between the root buttresses: big coursed blocks, a broken stepped top
    for (x0, y0, ww, hh) in [(18, 1085, 150, 110), (W - 172, 1120, 150, 96)]:
        course = 22
        rows = int(hh // course)
        for r in range(rows):
            off = (r % 2) * 22
            xs = x0 - off
            # the broken top: upper courses are shorter, stepping down toward the chasm
            reach = x0 + ww * (0.55 + 0.45 * r / max(1, rows - 1))
            while xs < reach:
                bw = rng.uniform(38, 52)
                t = rng.uniform(0.85, 1.12)
                box = (max(xs, x0) + 1.5, y0 + r * course + 1.5, min(xs + bw, reach) - 1.5, y0 + (r + 1) * course - 1.5)
                if box[2] - box[0] > 8:
                    dr.rectangle(E(box), fill=rgb255(WALL * t))
                    da.rectangle(E(box), fill=255)
                    # the block's top edge catches the sky: a lichen-grey arris
                    dr.line(S([(box[0], box[1]), (box[2], box[1])]), fill=rgb255(WALL_CAP * 0.55 * t), width=SS)
                xs += bw
    m_rgb, m_a = flatten(rgb, a)
    m_rgb = unpremultiplied(m_rgb, m_a)
    m_rgb = m_rgb * (0.9 + 0.1 * fbm(rng, 4, 2))[..., None]
    # only inside the trunk, and the bark laps over the wall's edges (smooth, not speckled)
    lap = np.clip((fbm(rng, 22, 2) + 0.35) * 2.5, 0, 1)
    vis = m_a * tr_m * lap * np.clip((edge_d - 14) / 16, 0, 1)
    moss = np.clip(fbm(rng, 9, 2) * 1.6 + 0.1, 0, 1)
    m_rgb = lerp(m_rgb, MOSS * 0.8, moss * 0.4)
    img = lerp(img, m_rgb, vis)
    # a verdigris band (brass gone green, docs/51) along the top course of each stump, dull and broken
    top_course = np.zeros((H, W), np.float32)
    for (x0, y0, ww) in [(18, 1085, 150), (W - 172, 1120, 150)]:
        top_course += ((yy >= y0 - 5) & (yy < y0 - 1) & (xx >= x0) & (xx < x0 + ww * 0.55)).astype(np.float32)
    band = blur(top_course, 0.8) * tr_m * lap * np.clip((edge_d - 14) / 16, 0, 1) * np.clip(fbm(rng, 6, 2) + 0.6, 0, 1)
    img = lerp(img, VERDIGRIS * 0.5, np.clip(band, 0, 1) * 0.8)
    return img


# ---------------------------------------------------------------- the shader, for previews

def in_game(img, tint=(1.04, 1.0, 1.06), atmosphere=0.10):
    """FoundryCavern.shader on this plate: the renderer colour, saturation x1.08, 10 % toward violet."""
    c_ = img * np.array(tint, np.float32)
    lum = c_ @ np.array([0.2126, 0.7152, 0.0722], np.float32)
    c_ = lum[..., None] + (c_ - lum[..., None]) * 1.08
    c_ = lerp(c_, np.array([0.20, 0.15, 0.30], np.float32), atmosphere)
    return np.clip(c_, 0, 1)


def paint():
    rng = np.random.default_rng(3051)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    v, u = yy / H, xx / W

    # Sky: green-black gloam, a little warmer and lighter around the sun, brightening into ash haze toward the horizon.
    img = lerp(SKY_TOP, SKY_MID, np.clip(v * 1.6, 0, 1) * 0.9 + fbm(rng, 90) * 0.05)
    img = lerp(img, HAZE * 0.66, np.clip((v - 0.34) / 0.45, 0, 1) ** 1.3 * 0.7)

    # The dimmed sun: flat ash-gold disc, faint limb darkening, a darker corona ring, then a broad dull halo.
    d = np.sqrt((xx - SUN_X) ** 2 + (yy - SUN_Y) ** 2)
    disc = np.clip((SUN_R - d) / 1.8, 0, 1)
    limb = np.clip(d / SUN_R, 0, 1)
    # a sun, not a moon: strong warm limb darkening, no maria; the lower half veiled by ash
    face = lerp(SUN * 1.03, SUN_RIM * 0.86, limb ** 2.2 * 0.85)
    face = lerp(face, SUN_RIM * 0.9, np.clip((yy - SUN_Y) / SUN_R, 0, 1) * 0.35)
    face = face * (0.99 + 0.01 * noise(rng, 26)[..., None])
    outside = np.clip(d - SUN_R, 0, None)
    # the Long Dimming: a soft dark corona close round the disc, then a broad dull warm halo
    corona_dark = np.exp(-((outside - 16) / 16.0) ** 2) * (d > SUN_R)
    halo = (np.exp(-outside / 46.0) * 0.32 + np.exp(-outside / 150.0) * 0.30 + np.exp(-outside / 420.0) * 0.20)
    img = img + SUN_HALO * halo[..., None] * 0.6
    img = lerp(img, CORONA, corona_dark * 0.42)
    img = lerp(img, face, disc)
    # ash cloud bands: broad soft streaks, one crossing the sun's lower third, a belt below and a thin one above
    streak = np.clip(noise(rng, 9, 230) * 0.8 + noise(rng, 22, 420) * 0.55 - 0.05, 0, 1)
    belt = (np.exp(-((v - (SUN_Y + SUN_R * 0.42) / H) / 0.028) ** 2) * 1.25
            + 0.6 * np.exp(-((v - 0.30) / 0.05) ** 2) + 0.45 * np.exp(-((v - 0.42) / 0.06) ** 2)
            + 0.3 * np.exp(-((v - 0.075) / 0.035) ** 2))
    clouds = np.clip(streak * belt, 0, 0.88)
    near_sun = np.clip(halo * 1.4, 0, 1)
    # dark undersides, warm-lit where the halo reaches them (a vertical gradient through each band)
    top_lit = np.clip(blur(clouds, 6) - blur(np.roll(clouds, 10, 0), 6), 0, 1) * 2.5
    cloud_col = lerp(c("19221D"), SUN_HALO * 0.58, np.clip(near_sun * 0.7 + top_lit * near_sun, 0, 1))
    img = lerp(img, cloud_col, clouds)

    # Far: the abandoned foundry overgrown - an aqueduct, towers (crenellated, chimneyed, broken gables), roots
    # draped over them, crowns growing from the tops. Fogged deep into the haze.
    rgb, a, dr, da = layer()
    aqueduct(dr, da, rng, 250, 520, H * 0.585, 34, FAR * 0.95, FAR * 1.55)
    aqueduct(dr, da, rng, 560, 790, H * 0.62, 30, FAR * 0.95, FAR * 1.55)
    towers = [(230, 0.86, 70, 0.40, "crenel"), (330, 0.84, 48, 0.30, "chimney"), (415, 0.86, 40, 0.22, "gable"),
              (545, 0.85, 56, 0.33, "chimney"), (640, 0.86, 44, 0.24, "crenel"), (740, 0.84, 62, 0.36, "chimney"),
              (820, 0.86, 52, 0.42, "gable"), (480, 0.88, 36, 0.17, "crenel")]
    leafy = noise(rng, 1.4) * 0.6 + noise(rng, 4.0) * 0.4
    far_crowns = []
    for (x, b, w, hh, kind) in towers:
        x += rng.uniform(-10, 10)
        base = H * b
        th = H * hh
        top = foundry_tower(dr, da, rng, x, base, w, th, FAR * 0.86, FAR * 1.5, kind)
        if rng.random() < 0.75:
            far_crowns.append((x + rng.uniform(-12, 12), top + rng.uniform(6, 16), w * rng.uniform(0.7, 1.15),
                               w * rng.uniform(0.42, 0.6), FAR * 0.95, FAR * 1.5, ((SUN_X - x) / 500.0, -1.0), 26))
        for _ in range(rng.integers(3, 7)):  # roots draped down the walls
            strand(dr, da, rng, x + rng.uniform(-w / 2, w / 2), base - th + rng.uniform(0, 30),
                   rng.uniform(40, th * 0.7), rng.uniform(2, 4.5), FAR_DARK, FAR * 1.3, rng.uniform(-1, 1))
    far_rgb, far_a = flatten(rgb, a)
    far_rgb = unpremultiplied(far_rgb, far_a)
    cr_rgb, cr_a = foliage(rng, far_crowns, leafy)
    far_rgb, far_a = over(far_rgb, far_a, cr_rgb, cr_a)
    far_rgb = far_rgb * (0.94 + 0.06 * fbm(rng, 6, 3))[..., None]
    fog = np.clip((v - 0.48) / 0.4, 0, 1) * 0.42 + 0.06
    far_rgb = lerp(far_rgb, HAZE * 0.62, fog)
    img = lerp(img, far_rgb, far_a)

    # Mid: three great trees, flared roots, leaf-mass crowns lit from the sun's side, hanging drape.
    rgb, a, dr, da = layer()
    mid_crowns = []
    for (tx, base, th, tw) in [(300, H * 0.93, H * 0.37, 30), (725, H * 0.94, H * 0.40, 34), (515, H * 0.955, H * 0.25, 22)]:
        tx += rng.uniform(-14, 14)
        top = base - th
        lean = rng.uniform(-24, 24)
        trunk = [(tx - tw, base), (tx - tw * 0.55, base - th * 0.5), (tx - tw * 0.42 + lean, top),
                 (tx + tw * 0.42 + lean, top), (tx + tw * 0.5, base - th * 0.5), (tx + tw, base)]
        dr.polygon(S(trunk), fill=rgb255(MID))
        da.polygon(S(trunk), fill=255)
        for k in range(5):  # root flare
            rx = tx + (k - 2) * tw * 0.95
            root = [(tx - tw * 0.45, base - th * 0.12), (rx + rng.uniform(-10, 10), base + 8), (tx + tw * 0.45, base - th * 0.12)]
            dr.polygon(S(root), fill=rgb255(MID * 0.92))
            da.polygon(S(root), fill=255)
        for k in range(2):  # two big limbs
            sgn = -1 if k == 0 else 1
            ly = top + th * rng.uniform(0.05, 0.2)
            limb = [(tx + lean * 0.8 - 6, ly + 14), (tx + lean + sgn * tw * 2.6, ly - th * 0.12),
                    (tx + lean + sgn * tw * 2.7, ly - th * 0.10 + 8), (tx + lean * 0.8 + 6, ly + 26)]
            dr.polygon(S(limb), fill=rgb255(MID))
            da.polygon(S(limb), fill=255)
        side = 1 if tx < SUN_X else -1
        dr.line(S([(tx + side * tw * 0.42 + lean, top), (tx + side * tw * 0.55, base - th * 0.5), (tx + side * tw, base)]),
                fill=rgb255(MID_RIM * 0.75), width=2 * SS)
        light = ((SUN_X - tx) / 400.0, -1.0)
        mid_crowns.append((tx + lean, top - th * 0.10, tw * 4.0, tw * 2.6, MID * 1.1, (MID_RIM + MOSS_LIT) * 0.45, light, 150))
    mid_rgb, mid_a = flatten(rgb, a)
    mid_rgb = unpremultiplied(mid_rgb, mid_a)
    cr_rgb, cr_a = foliage(rng, mid_crowns, leafy)
    mid_rgb, mid_a = over(mid_rgb, mid_a, cr_rgb, cr_a)
    rgb, a, dr, da = layer()
    for (cx, cy, rx, ry, *_rest) in mid_crowns:
        drapes(dr, da, rng, cx, cy, rx, ry, MID * 0.8, 34)
    dp_rgb, dp_a = flatten(rgb, a)
    mid_rgb, mid_a = over(mid_rgb, mid_a, unpremultiplied(dp_rgb, dp_a), dp_a)
    mid_rgb = mid_rgb * (0.93 + 0.07 * fbm(rng, 5, 3))[..., None]
    # pushed back into the haze: a silhouette, not a second subject behind the arena
    mid_rgb = lerp(mid_rgb, HAZE * 0.5, 0.18 + np.clip((v - 0.6) / 0.4, 0, 1) * 0.28)
    img = lerp(img, mid_rgb, mid_a)

    # The ash field: low pale mounds in the middle distance, tufts and saplings; dim, never lane-pale.
    rgb, a, dr, da = layer()
    for k in range(3):
        base_y = H * (0.80 + k * 0.03)
        xs = np.linspace(-20, W + 20, 44)
        ys = base_y - (np.sin(xs / rng.uniform(60, 120) + rng.uniform(0, 6)) * 0.5 + 0.5) * rng.uniform(12, 28)
        poly = [(x, y) for x, y in zip(xs, ys)] + [(W + 20, H * 0.92), (-20, H * 0.92)]
        tone = lerp(MID * 1.35, ASH * 0.38, np.array(0.45 - k * 0.12))
        dr.polygon(S(poly), fill=rgb255(tone))
        da.polygon(S(poly), fill=255)
        dr.line(S(list(zip(xs, ys))), fill=rgb255(ASH * (0.38 - k * 0.05)), width=SS)
        for _ in range(70):
            i = rng.integers(0, len(xs) - 1)
            tx = xs[i] + rng.uniform(0, xs[1] - xs[0])
            ty = np.interp(tx, xs, ys) + rng.uniform(0, 10)
            th = rng.uniform(3, 14)
            for _ in range(3):
                dr.line(S([(tx, ty), (tx + rng.uniform(-4, 4), ty - th * rng.uniform(0.6, 1.0))]), fill=rgb255(MID), width=SS)
    ash_rgb, ash_a = flatten(rgb, a)
    ash_rgb = unpremultiplied(ash_rgb, ash_a)
    ash_rgb = lerp(ash_rgb, HAZE * 0.55, 0.25) * (0.9 + 0.1 * fbm(rng, 5, 3, 0.4))[..., None]
    img = lerp(img, ash_rgb, ash_a)

    # Gloam haze rising from the chasm floor (ash and spores).
    haze_amt = np.clip((v - 0.52) / 0.36, 0, 1) ** 1.4 * (0.55 + 0.45 * np.clip(fbm(rng, 70) * 0.8 + 0.5, 0, 1))
    img = lerp(img, HAZE * 0.92, haze_amt * 0.34)

    # The arena-overlap zone: pull the middle toward its local mean (less contrast and detail behind the playfield).
    zone = np.exp(-(((u - 0.5) / 0.24) ** 2 + ((v - 0.66) / 0.24) ** 2) * 1.0)
    zone *= 1 - np.clip((0.3 - v) / 0.1, 0, 1)  # the sun keeps its edge
    soft = blur3(img, 3.0)
    mean = blur3(img, 40.0)
    img = lerp(img, soft, zone * 0.45)
    img = lerp(img, mean, zone * 0.30)

    # Near trunks and the walls they swallowed.
    tr_col, tr_m, edge_d = near_trunks(rng, xx, yy, v)
    img = lerp(img, tr_col, tr_m)
    img = swallowed_masonry(rng, img, tr_m, edge_d, xx, yy)

    # Hanging roots from the top corners (the foundry's stalactites); a few carry seed-lantern pods.
    rgb, a, dr, da = layer()
    tips = []
    for _ in range(48):
        left = rng.random() < 0.5
        x0 = rng.uniform(-10, 350) if left else rng.uniform(674, 1034)
        depth = np.clip(abs(x0 - 512) / 340, 0.25, 1.0)
        width = rng.uniform(3, 12) * depth
        tip = strand(dr, da, rng, x0, -4, rng.uniform(80, 400) * depth, width,
                     BARK * rng.uniform(0.8, 1.15), HEARTWOOD * 0.85, rng.uniform(-1.2, 1.2))
        if width > 4:
            tips.append(tip)
    for _ in range(14):  # fine rootlets and moss beards between them
        left = rng.random() < 0.5
        x0 = rng.uniform(0, 300) if left else rng.uniform(724, 1024)
        strand(dr, da, rng, x0, -2, rng.uniform(40, 160), 1.6, MOSS * 0.55, None, rng.uniform(-1, 1), n=8)
    root_rgb, root_a = flatten(rgb, a)
    root_rgb = unpremultiplied(root_rgb, root_a)
    img = lerp(img, root_rgb, root_a)

    # Gloamlight: seed-lanterns on threads under the corner roots and bracket fungi on the trunks - the frame only.
    core = Image.new("L", (W * SS, H * SS), 0)
    thread = Image.new("L", (W * SS, H * SS), 0)
    cd, td = ImageDraw.Draw(core), ImageDraw.Draw(thread)
    lanterns = [(240, 250, 8), (292, 405, 6), (205, 540, 5), (775, 290, 8), (728, 455, 6), (812, 610, 5),
                (150, 330, 7), (880, 360, 7)]
    caps = Image.new("RGB", (W * SS, H * SS), (0, 0, 0))
    caps_a = Image.new("L", (W * SS, H * SS), 0)
    kd, ka = ImageDraw.Draw(caps), ImageDraw.Draw(caps_a)
    for (lx, ly, lr) in lanterns:
        # a seed pod: a thread, a bark calyx on top, a teardrop of gloamlight under it
        td.line([lx * SS, 0, lx * SS, (ly - lr * 1.2) * SS], fill=150, width=SS)
        cd.polygon(S([(lx, ly + lr * 1.5), (lx - lr * 0.8, ly + lr * 0.1), (lx - lr * 0.55, ly - lr * 0.75),
                      (lx + lr * 0.55, ly - lr * 0.75), (lx + lr * 0.8, ly + lr * 0.1)]), fill=255)
        calyx = (lx - lr * 0.75, ly - lr * 1.35, lx + lr * 0.75, ly - lr * 0.35)
        kd.chord(E(calyx), 180, 360, fill=rgb255(BARK * 1.3))
        ka.chord(E(calyx), 180, 360, fill=255)
    core_a = np.asarray(core.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    thread_a = np.asarray(thread.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    img = lerp(img, HEARTWOOD * 0.7, thread_a * 0.45)

    # bracket fungi: shelves stepping down the bark; a bark-brown cap lit lichen-grey on its upper rim, a thin
    # gloamlight gill rim along its underside, and that light falling on the bark below
    gills = Image.new("L", (W * SS, H * SS), 0)
    gd = ImageDraw.Draw(gills)
    fungi = [(165, 650, 30), (118, 835, 22), (196, 950, 34), (88, 1110, 20), (150, 1215, 26),
             (868, 705, 32), (922, 885, 22), (846, 1015, 28), (944, 1165, 20), (864, 1262, 24)]
    for (fx, fy, fw) in fungi:
        n = rng.integers(2, 4)
        step = 1 if fx < W / 2 else -1  # smaller shelves step out toward the chasm
        for k in range(n):
            ow = fw * (1.0 - 0.22 * k) * rng.uniform(0.9, 1.1)
            ox, oy = fx + step * k * fw * 0.3 + rng.uniform(-4, 4), fy + k * fw * 0.7
            top = (ox - ow, oy - ow * 0.44, ox + ow, oy + ow * 0.12)
            kd.pieslice(E(top), 180, 360, fill=rgb255(HEARTWOOD * rng.uniform(0.5, 0.66)))
            ka.pieslice(E(top), 180, 360, fill=255)
            # growth rings on the cap
            for r in (0.75, 0.5):
                inner = (ox - ow * r, oy - ow * 0.44 * r, ox + ow * r, oy + ow * 0.12 * r)
                kd.arc(E(inner), 190, 350, fill=rgb255(HEARTWOOD * 0.42), width=SS)
            kd.arc(E((top[0] + 1, top[1] + 1, top[2] - 1, top[3] + 4)), 205, 335, fill=rgb255(WALL_CAP * 0.7), width=SS)
            # the gill rim: a flat underside, brightest at the middle
            gd.line(S([(ox - ow * 0.85, oy - 0.5), (ox + ow * 0.85, oy - 0.5)]), fill=150, width=SS)
            gd.line(S([(ox - ow * 0.5, oy - 0.5), (ox + ow * 0.5, oy - 0.5)]), fill=255, width=SS)
    cap_rgb = np.asarray(caps.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    cap_a = np.asarray(caps_a.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    gill_a = np.asarray(gills.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    # the glow: soft round the pods, and falling downward from the gills onto the bark below them
    below = np.zeros_like(gill_a)
    for k in range(1, 18):
        below += np.roll(gill_a, k, 0) * math.exp(-k / 6.0)
    glow = blur(core_a, 5) * 3.0 + blur(core_a, 20) * 5 + blur(below, 3, 5) * 0.55 + blur(gill_a, 14) * 2.5
    img = img + GLOAM * np.clip(glow, 0, 1.0)[..., None] * 0.26
    img = lerp(img, unpremultiplied(cap_rgb, cap_a), cap_a)
    img = lerp(img, GLOAM * 0.9, gill_a * 0.8)
    img = lerp(img, lerp(GLOAM, GLOAM_CORE, np.array(0.45)), core_a * 0.7)

    # The dark lower band where the arena meets the plate (roots and rubble), same height as the ice plate's.
    band_top = H * 0.885 + noise(rng, 6, 6, (1, W))[0] * 14 + noise(rng, 30, 30, (1, W))[0] * 22
    band_m = np.clip((yy - band_top[None, :]) / 3.0, 0, 1)
    band_col = lerp(BAND, MID, np.clip(0.4 + 0.3 * fbm(rng, 10), 0, 1) * 0.6)
    band_col = lerp(band_col, BAND * 0.7, np.clip((v - 0.93) / 0.07, 0, 1))
    img = lerp(img, band_col, band_m)
    img = lerp(img, HAZE * 0.6, np.exp(-np.abs(yy - band_top[None, :]) / 10.0) * 0.15)

    # Drifting ash flakes (pale, streaked sideways) and a few dim pollen motes, kept out of the middle.
    away = 1 - zone * 0.85
    flakes = (rng.random((H, W)) > 0.99995).astype(np.float32)
    img = img + ASH * 0.75 * np.clip(blur(flakes, 1.1, 1.8) * 9, 0, 0.26)[..., None] * ((1 - band_m) * away)[..., None]
    motes = (rng.random((H, W)) > 0.99994).astype(np.float32)
    img = img + GLOAM * np.clip(blur(motes, 1.2) * 6, 0, 0.45)[..., None] * ((1 - band_m) * away * (1 - tr_m * 0.5))[..., None]

    # Vignette, grain, painterly finish.
    vig = 1 - 0.33 * np.clip(((u - 0.5) ** 2 * 2.2 + (v - 0.42) ** 2 * 1.3), 0, 1)
    img = img * vig[..., None] + noise(rng, 0.8)[..., None] * 0.010
    img = np.clip(img, 0, 1)
    pil = Image.fromarray((img * 255 + 0.5).astype(np.uint8), "RGB")
    pil = pil.filter(ImageFilter.ModeFilter(3)).filter(ImageFilter.UnsharpMask(2, 45, 2))
    return pil


def main():
    pil = paint()
    pil.save(OUT, optimize=True)
    print(OUT, pil.size, os.path.getsize(OUT), "bytes")
    if len(sys.argv) > 1:
        os.makedirs(os.path.dirname(sys.argv[1]), exist_ok=True)
        pil.save(sys.argv[1])


if __name__ == "__main__":
    main()
