"""Floor 3 (Gloam Gardens) growth atlas for the props step (docs/51 sections 4 and 9.5): hanging roots, fungus and
seed-pod clusters and moss-cap fringes, painted into one RGBA sheet, Resources/Gloam/GloamGrowth.png.

Why one atlas: the ice builder (FoundryRegionView.BuildIceCrystals, FoundryRegionIceCrystals.cs) emits ONE mesh in
ONE material (the coping material, Sprites/Default) and draws it untextured - its UVs are all (0,0) and ApplyTheme's
Bare dress puts Texture2D.whiteTexture on it, so the ice's look is vertex colour alone. The garden growth takes that
mesh's place under the same IceCrystalVertexCap (6,000), the same outer-rim slots and the same single renderer; to stay
one draw with a texture, every growth piece samples this one sheet. The vertex colour then multiplies it (white = the
painted value; a darker grey can shade pieces farther round the rim).

Layout (pixels from the top-left of the 1024x1024 sheet; Unity UV v = 1 - y/1024; anchors are where a piece meets the
platform):
- Hanging roots (replace the icicles): 8 cells, 128 x 512, at x = 128*i, y = 0..511. Each root hangs from the top of
  its cell; anchor = top centre (u = (128*i + 64)/1024, v = 1 - 8/1024). Lengths vary by cell (about 300-490 px);
  the quad may use the whole cell (the rest is transparent).
- Fungus and seed-pod clusters (replace the crystal clusters): 4 cells, 256 x 256, at x = 256*i, y = 512..767;
  anchor = bottom centre (u = (256*i + 128)/1024, v = 1 - 760/1024). Cell 0 is the tallest (region edge), 3 the
  lowest. Tips in gloamlight #C6EE6A; no orchid anywhere.
- Moss-cap fringes (the rime cap's texture counterpart along the rim, structures only): 2 strips, 1024 x 128, at
  y = 768..895 and 896..1023. Each is seamless over its whole width and splits into 4 quarters of 256 px; consecutive
  rim edges take consecutive quarters (k, k+1, ... mod 4) and join without a seam. The rim line is 32 px below the
  strip's top: the cushion sits above it (on the cap's edge), the drape hangs below it down the sidewall.
Every piece keeps at least 16 px of transparent margin to its cell's sides, so mip levels up to 3 do not bleed between
cells; transparent texels carry the nearest painted colour (no dark fringes under bilinear filtering).

Writes Assets/_Project/Resources/Gloam/GloamGrowth.png (RGBA) with a hand-written .meta (Resources/Ice's RGBA import
settings: Default texture, mipmaps, clamp, bilinear, max 1024, Android ASTC 6x6; a fresh guid, kept on re-runs).
Optional argument: a preview copy path.
Usage: python -I Tools/ArtReview/GenerateGloamGrowthTextures.py [preview.png]
"""
import math
import os
import re
import sys
import uuid

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Resources", "Gloam", "GloamGrowth.png")
N = 1024
SS = 2

ROOT_CELLS = [(128 * i, 0, 128, 512) for i in range(8)]
CLUSTER_CELLS = [(256 * i, 512, 256, 256) for i in range(4)]
MOSS_STRIPS = [(0, 768, 1024, 128), (0, 896, 1024, 128)]
RIM_LINE = 32  # px below a strip's top


def c(h):
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


BARK_DEEP, BARK, HEARTWOOD = c("14100E"), c("2A221E"), c("5B4838")
MOSS, MOSS_LIT, LICHEN, MOSS_SHADE = c("3F5A32"), c("4F6B3A"), c("8FA35A"), c("1F2E2A")
WALL_CAP = c("7C8466")
GLOAM, GLOAM_CORE = c("C6EE6A"), c("E2F7A0")
ASH = c("A7A99C")


def blur(a, sy, sx=None):
    sx = sy if sx is None else sx
    h, w = a.shape
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    g = np.exp(-((fx * sx) ** 2 + (fy * sy) ** 2) * 2.0 * np.pi ** 2)
    return np.real(np.fft.ifft2(np.fft.fft2(a) * g)).astype(np.float32)


def noise(rng, sy, sx=None, shape=(N, N)):
    n = blur(rng.standard_normal(shape).astype(np.float32), sy, sx)
    n -= n.mean()
    return n / (n.std() + 1e-6)


def lerp(a, b, t):
    t = t[..., None] if np.ndim(t) == 2 else t
    return a * (1 - t) + b * t


def over(dst_rgb, dst_a, src_rgb, src_a):
    out_a = src_a + dst_a * (1 - src_a)
    out = (src_rgb * src_a[..., None] + dst_rgb * (dst_a * (1 - src_a))[..., None]) / np.maximum(out_a, 1e-4)[..., None]
    return out, out_a


def mask_of(draw_fn, box):
    """Rasterise a PIL drawing (in sheet pixels) supersampled, returned at sheet size for the box (x, y, w, h)."""
    x, y, w, h = box
    im = Image.new("L", (w * SS, h * SS), 0)
    draw_fn(ImageDraw.Draw(im), lambda px, py: ((px - x) * SS, (py - y) * SS))
    return np.asarray(im.resize((w, h), Image.LANCZOS)).astype(np.float32) / 255.0


# ---------------------------------------------------------------- hanging roots

def root_path(rng, x0, y0, length, width, sway, n=40):
    phase = rng.uniform(0, 6)
    pts, widths = [], []
    for i in range(n + 1):
        t = i / n
        x = x0 + sway * (math.sin(t * 2.6 + phase) - math.sin(phase)) * length * 0.07
        pts.append((x, y0 + t * length))
        widths.append(width * (1 - t) ** 0.8 + 1.2)
    return pts, widths


def tube(pts, widths, to_px):
    """A tapered tube polygon along pts."""
    left, right = [], []
    for i, (x, y) in enumerate(pts):
        if i + 1 < len(pts):
            dx, dy = pts[i + 1][0] - x, pts[i + 1][1] - y
        else:
            dx, dy = x - pts[i - 1][0], y - pts[i - 1][1]
        ln = math.hypot(dx, dy) or 1.0
        nx, ny = -dy / ln, dx / ln
        w = widths[i] / 2
        left.append(to_px(x + nx * w, y + ny * w))
        right.append(to_px(x - nx * w, y - ny * w))
    return left + right[::-1]


def paint_root(rng, cell, rgb, alpha, length):
    x, y, w, h = cell
    cx = x + w / 2
    top = y + 8
    width = rng.uniform(20, 28)
    sway = rng.uniform(-1, 1)
    pts, widths = root_path(rng, cx, top, length, width, sway)
    branches = []
    for _ in range(rng.integers(1, 4)):
        k = rng.integers(8, 26)
        bx, by = pts[k]
        side = rng.choice([-1, 1])
        bl = rng.uniform(0.25, 0.45) * length
        bpts = []
        for i in range(17):
            t = i / 16
            bpts.append((bx + side * (t ** 0.7) * rng.uniform(22, 30), by + t * bl))
        bw = [widths[k] * 0.42 * (1 - i / 16) ** 0.8 + 0.8 for i in range(17)]
        branches.append((bpts, bw))
    ys, xs = np.mgrid[y:y + h, x:x + w].astype(np.float32)
    # body mask
    m = mask_of(lambda d, P: [d.polygon(tube(pts, widths, P), fill=255)] +
                [d.polygon(tube(bp, bw, P), fill=255) for (bp, bw) in branches], cell)
    # across-the-root coordinate for round shading: distance to the centre line, signed (left = lit)
    centre_x = np.interp(ys[:, 0], [p[1] for p in pts], [p[0] for p in pts])[:, None]
    half = np.interp(ys[:, 0], [p[1] for p in pts], widths)[:, None] / 2
    s = np.clip((xs - centre_x) / np.maximum(half, 1), -1, 1)  # -1 left edge .. 1 right edge
    in_main = np.abs(xs - centre_x) <= half + 0.5
    s = np.where(in_main, s, 0.15)  # branches: mostly mid-tone
    lit = np.clip(-s * 1.2 + 0.1, 0, 1) ** 1.4  # the left (heartwood) edge
    shade = np.clip(s, 0, 1) ** 1.2
    rx = slice(y, y + h), slice(x, x + w)
    streak = noise(rng, 14, 1.4)[rx]
    ring = (np.sin((ys - top) / rng.uniform(26, 40) * 2 * np.pi + streak * 0.8) > 0.93).astype(np.float32)
    col = lerp(BARK, BARK_DEEP, shade * 0.85)
    col = lerp(col, HEARTWOOD * 1.05, lit * 0.85)
    col = col * (0.88 + 0.12 * np.clip(streak * 0.5 + 0.5, 0, 1))[..., None]
    col = lerp(col, BARK_DEEP * 0.7, ring * 0.35)
    # moss where it leaves the rim and in a few patches down its upper half
    mossy = np.clip(1 - (ys - top) / rng.uniform(40, 70), 0, 1) + \
        np.clip(noise(rng, 6)[rx] * 1.2 - 1.0, 0, 1) * np.clip(1 - (ys - top) / (length * 0.6), 0, 1)
    mossy = np.clip(mossy, 0, 1) * (0.5 + 0.5 * np.clip(-s + 0.3, 0, 1))
    col = lerp(col, lerp(MOSS, MOSS_LIT, lit), mossy * 0.8)
    # a soft cushion of moss at the anchor so the root grows out of the rim, not out of a straight cut
    cush = np.clip(1 - np.sqrt(((xs - cx) / (width * 0.9)) ** 2 + ((ys - top) / 12.0) ** 2), 0, 1)
    cush = np.clip(cush * 2.2, 0, 1) * (0.7 + 0.3 * np.clip(noise(rng, 2)[rx] + 0.5, 0, 1))
    col = lerp(col, lerp(MOSS, MOSS_LIT, np.clip(1 - (ys - top) / 12.0, 0, 1)), cush)
    m = np.maximum(m, (cush > 0.25).astype(np.float32) * cush)
    out_rgb, out_a = over(rgb[rx], alpha[rx], col, m)
    rgb[rx], alpha[rx] = out_rgb, out_a


# ---------------------------------------------------------------- clusters

def paint_cluster(rng, cell, rgb, alpha, kind):
    x, y, w, h = cell
    cx, foot = x + w / 2, y + 248
    ys, xs = np.mgrid[y:y + h, x:x + w].astype(np.float32)
    rx = slice(y, y + h), slice(x, x + w)
    layer_rgb = np.zeros((h, w, 3), np.float32)
    layer_a = np.zeros((h, w), np.float32)
    glow_src = np.zeros((h, w), np.float32)
    grain = noise(rng, 1.6)[rx]

    # seed-pod stalks: thin curved stems, a closed bark calyx and a gloamlight tip; tallest in cell 0
    n_stalks = {0: 5, 1: 2, 2: 3, 3: 2}[kind]
    tall = {0: (150, 200), 1: (80, 110), 2: (90, 130), 3: (50, 75)}[kind]
    stalks = []
    for k in range(n_stalks):
        spread = (k - (n_stalks - 1) / 2) / max(1, n_stalks - 1)
        ln = rng.uniform(*tall) * (1 - 0.25 * abs(spread))
        bx = cx + spread * 40 + rng.uniform(-6, 6)
        lean = spread * rng.uniform(25, 45)
        stalks.append((bx, ln, lean))
    for (bx, ln, lean) in stalks:
        pts = [(bx + lean * (t ** 1.6), foot - 6 - ln * t) for t in np.linspace(0, 1, 24)]
        wd = [5.5 * (1 - t) + 2.2 for t in np.linspace(0, 1, 24)]
        m = mask_of(lambda d, P: d.polygon(tube(pts, wd, P), fill=255), cell)
        side = np.clip((xs - (bx + lean * 0.5)) / 6, -1, 1)
        stem = lerp(BARK * 1.25, HEARTWOOD * 0.9, np.clip(-side, 0, 1) * 0.7)
        layer_rgb, layer_a = over(layer_rgb, layer_a, stem, m)
        tx, ty = pts[-1]
        pr = rng.uniform(7, 10) * (1.0 if kind != 3 else 0.8)
        # the pod: a teardrop of gloamlight under a dark calyx
        pod = mask_of(lambda d, P: d.ellipse([*P(tx - pr * 0.75, ty - pr * 1.25), *P(tx + pr * 0.75, ty + pr * 0.95)], fill=255), cell)
        dist = np.sqrt(((xs - tx) / (pr * 0.75)) ** 2 + ((ys - (ty - pr * 0.1)) / pr) ** 2)
        podc = lerp(GLOAM_CORE, GLOAM * 0.78, np.clip(dist, 0, 1) ** 1.5)
        layer_rgb, layer_a = over(layer_rgb, layer_a, podc, pod)
        glow_src += pod
        calyx = mask_of(lambda d, P: d.pieslice([*P(tx - pr * 0.85, ty + pr * 0.2), *P(tx + pr * 0.85, ty + pr * 1.5)], 0, 180, fill=255), cell)
        layer_rgb, layer_a = over(layer_rgb, layer_a, lerp(BARK * 1.2, HEARTWOOD * 0.8, np.clip((tx - xs) / pr, 0, 1) * 0.6), calyx)

    # bracket shelves fanning out of the wall foot
    n_shelves = {0: 3, 1: 5, 2: 4, 3: 3}[kind]
    base_w = {0: 34, 1: 48, 2: 40, 3: 30}[kind]
    shelves = []
    for k in range(n_shelves):
        t = k / max(1, n_shelves - 1)
        ow = base_w * (1.0 - 0.35 * t) * rng.uniform(0.85, 1.1)
        ox = cx + (t - 0.5) * (100 if kind == 1 else 80) + rng.uniform(-8, 8)
        oy = foot - 10 - (1 - abs(t - 0.5) * 2) * (70 if kind == 1 else 36) - rng.uniform(0, 14)
        shelves.append((oy, ox, ow))
    shelves.sort()  # higher shelves first, lower ones in front
    for (oy, ox, ow) in shelves:
        thick = ow * 0.22
        body = mask_of(lambda d, P: d.ellipse([*P(ox - ow, oy - ow * 0.34), *P(ox + ow, oy + ow * 0.30)], fill=255), cell)
        lip = mask_of(lambda d, P: d.chord([*P(ox - ow, oy - ow * 0.34 + thick), *P(ox + ow, oy + ow * 0.30 + thick)], 0, 180, fill=255), cell)
        # top surface: concentric growth bands, pale lichen rim, darker toward the wall
        r = np.sqrt(((xs - ox) / ow) ** 2 + ((ys - oy) / (ow * 0.32)) ** 2)
        bands = 0.5 + 0.5 * np.sin(r * 15 + grain * 0.6)
        topc = lerp(HEARTWOOD * 0.62, HEARTWOOD * 0.95, bands * 0.5 + (1 - r) * 0.2)
        topc = lerp(topc, WALL_CAP * 0.95, np.clip((r - 0.78) / 0.2, 0, 1) * 0.8)
        topc = lerp(topc, MOSS_LIT, np.clip(noise(rng, 3)[rx] - 0.8, 0, 1) * 0.6)
        topc = topc * (0.9 + 0.1 * np.clip(grain * 0.5 + 0.5, 0, 1))[..., None]
        underside = lerp(BARK_DEEP * 1.2, BARK, np.clip((xs - (ox - ow)) / (2 * ow), 0, 1) * 0.3)
        layer_rgb, layer_a = over(layer_rgb, layer_a, underside, np.clip(lip - body, 0, 1))
        layer_rgb, layer_a = over(layer_rgb, layer_a, topc, body)
        # the gill rim: a thin gloamlight line under the front lip, dim (never neon)
        gill = mask_of(lambda d, P: d.arc([*P(ox - ow * 0.92, oy - ow * 0.30 + thick), *P(ox + ow * 0.92, oy + ow * 0.30 + thick + 1)],
                                          20, 160, fill=255, width=max(1, int(1.6 * SS))), cell)
        layer_rgb, layer_a = over(layer_rgb, layer_a, GLOAM * 0.72, gill * 0.9)
        glow_src += gill * 0.5

    # a moss mound at the foot ties the cluster to the wall
    mound = np.clip(1 - np.sqrt(((xs - cx) / 78) ** 2 + ((ys - foot) / 16) ** 2), 0, 1)
    mound_m = np.clip((mound + grain * 0.12 - 0.05) * 6, 0, 1) * (mound > 0) * (ys <= foot + 2)
    moss_c = lerp(MOSS * 0.85, MOSS_LIT, np.clip((foot - ys) / 16, 0, 1))
    moss_c = lerp(moss_c, LICHEN * 0.9, np.clip(grain - 1.4, 0, 1))
    layer_rgb, layer_a = over(layer_rgb, layer_a, moss_c, mound_m)

    # a soft gloamlight halo round the pods, inside the texture (alpha-blended, so it reads as light on the wall)
    halo = np.clip(blur(np.pad(glow_src, 32), 7)[32:-32, 32:-32] * 1.4, 0, 0.45)
    halo_a = halo * (1 - layer_a)
    layer_rgb, layer_a = over(layer_rgb, layer_a, np.broadcast_to(GLOAM, (h, w, 3)).copy(), halo_a)
    out_rgb, out_a = over(rgb[rx], alpha[rx], layer_rgb, layer_a)
    rgb[rx], alpha[rx] = out_rgb, out_a


# ---------------------------------------------------------------- moss caps

def periodic_noise(rng, h, w, sy, sx):
    n = blur(rng.standard_normal((h, w)).astype(np.float32), sy, sx)
    n -= n.mean()
    return n / (n.std() + 1e-6)


def paint_moss(rng, strip, rgb, alpha, sparse):
    x, y, w, h = strip
    rim = RIM_LINE
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    # all noise periodic in x over the strip's width (FFT blur on a wrapped field), so quarters join seamlessly
    lump = periodic_noise(rng, h, w, 6, 14)
    bumps = np.zeros(w, np.float32)
    for _ in range(80):
        bx = rng.uniform(0, w)
        br = rng.uniform(8, 18)
        d = np.minimum(np.abs(np.arange(w) - bx), w - np.abs(np.arange(w) - bx))
        bumps = np.maximum(bumps, np.sqrt(np.clip(1 - (d / br) ** 2, 0, 1)) * br * rng.uniform(0.7, 1.1))
    top_edge = rim - np.clip(bumps * (1.0 if not sparse else 0.7), 5, 28)  # cushion silhouette above the rim
    cushion = np.clip((ys - top_edge[None, :]) / 1.5, 0, 1) * (ys <= rim + 4)
    # drape: strands of hanging moss and rootlets below the rim line, lengths varying along the strip
    drape = np.zeros((h, w), np.float32)
    count = 130 if not sparse else 70
    im = Image.new("L", (w * SS, h * SS), 0)
    d = ImageDraw.Draw(im)
    for _ in range(count):
        sx = rng.uniform(0, w)
        ln = rng.uniform(18, 46) if rng.random() < 0.65 else rng.uniform(40, h - rim - 14)
        if sparse:
            ln *= 0.7
        wd = rng.uniform(3, 7)
        sway = rng.uniform(-6, 6)
        for shift in (-w, 0, w):  # wrap so the strip stays seamless
            pts = []
            for t in np.linspace(0, 1, 12):
                pts.append(((sx + shift + sway * t * t) * SS, (rim + t * ln) * SS))
            left = [(px - wd * (1 - t) ** 0.7 * SS / 2, py) for (px, py), t in zip(pts, np.linspace(0, 1, 12))]
            right = [(px + wd * (1 - t) ** 0.7 * SS / 2, py) for (px, py), t in zip(pts, np.linspace(0, 1, 12))]
            d.polygon(left + right[::-1], fill=255)
    drape = np.asarray(im.resize((w, h), Image.LANCZOS)).astype(np.float32) / 255.0
    # a continuous moss skirt just under the rim, ragged
    skirt_len = 8 + 6 * np.clip(periodic_noise(rng, 1, w, 1, 10)[0] * 0.5 + 0.5, 0, 1.5)
    skirt = np.clip((rim + skirt_len[None, :] - ys) / 2, 0, 1) * (ys >= rim - 2)
    m = np.clip(np.maximum(cushion, np.maximum(drape, skirt)), 0, 1)
    # shading: the cushion is lit from above (lumps), the drape darkens toward its tips and into moss shade
    hgt = blur(np.pad(cushion, ((0, 0), (0, 0))), 3) + lump * 0.06
    gy, gx = np.gradient(hgt)
    lam = np.clip(0.5 + (-gy) * 7 + (-gx) * 2, 0, 1)
    up = np.clip((rim - ys) / 24, 0, 1)
    down = np.clip((ys - rim) / (h - rim), 0, 1)
    col = lerp(MOSS * 0.8, MOSS_LIT * 1.08, lam * (0.4 + 0.6 * up))
    col = lerp(col, MOSS_SHADE * 0.9, down ** 0.7 * 0.85)
    col = col * (0.9 + 0.12 * np.clip(lump * 0.5 + 0.5, 0, 1))[..., None]
    speck = np.clip(periodic_noise(rng, h, w, 0.9, 0.9) - 2.0, 0, 1) * up
    col = lerp(col, LICHEN, speck * 0.8)
    # a few drape strands are rootlets: bark brown, not moss
    roots = (np.clip(periodic_noise(rng, 1, w, 1, 4)[0] - 1.3, 0, 1) > 0)[None, :] * (ys > rim + 6)
    col = np.where(roots[..., None], lerp(BARK, HEARTWOOD, np.array(0.3)), col)
    # soften the very tips of the drape
    m = m * np.clip(1 - np.clip((ys - (h - 16)) / 14, 0, 1), 0, 1)
    rx = slice(y, y + h), slice(x, x + w)
    out_rgb, out_a = over(rgb[rx], alpha[rx], col, m)
    rgb[rx], alpha[rx] = out_rgb, out_a


# ---------------------------------------------------------------- assembly

def bleed(rgb, alpha, steps=24):
    """Carry each painted colour outward into the transparent texels (no dark fringes under filtering or mips)."""
    filled = alpha > 0.02
    out = rgb * filled[..., None]
    known = filled.copy()
    for _ in range(steps):
        acc = np.zeros_like(out)
        cnt = np.zeros(known.shape, np.float32)
        for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            acc += np.roll(np.roll(out * known[..., None], dy, 0), dx, 1)
            cnt += np.roll(np.roll(known.astype(np.float32), dy, 0), dx, 1)
        grow = (~known) & (cnt > 0)
        out[grow] = acc[grow] / cnt[grow][..., None]
        known = known | grow
    out[~known] = np.array([0.16, 0.19, 0.14], np.float32)
    return out


def paint():
    rng = np.random.default_rng(5151)
    rgb = np.zeros((N, N, 3), np.float32)
    alpha = np.zeros((N, N), np.float32)
    lengths = [490, 360, 430, 300, 470, 330, 400, 450]
    for cell, ln in zip(ROOT_CELLS, lengths):
        paint_root(rng, cell, rgb, alpha, ln)
    for k, cell in enumerate(CLUSTER_CELLS):
        paint_cluster(rng, cell, rgb, alpha, k)
    paint_moss(rng, MOSS_STRIPS[0], rgb, alpha, False)
    paint_moss(rng, MOSS_STRIPS[1], rgb, alpha, True)
    rgb = bleed(np.clip(rgb, 0, 1), alpha)
    rgba = np.concatenate([np.clip(rgb, 0, 1), np.clip(alpha, 0, 1)[..., None]], -1)
    return Image.fromarray((rgba * 255 + 0.5).astype(np.uint8), "RGBA")


META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
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
  maxTextureSize: 1024
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
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
    maxTextureSize: 1024
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
    maxTextureSize: 1024
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
    maxTextureSize: 1024
    resizeAlgorithm: 0
    textureFormat: 50
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
    spriteID:
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


def main():
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    im = paint()
    im.save(OUT, optimize=True)
    meta = OUT + ".meta"
    guid = None
    if os.path.exists(meta):
        guid = re.search(r"^guid: (\w+)", open(meta).read(), re.M).group(1)
    else:
        with open(meta, "w", newline="\n") as f:
            f.write(META.format(guid=uuid.uuid4().hex))
    print(OUT, im.size, os.path.getsize(OUT), "bytes", "guid kept" if guid else "new guid")
    for name, cells in (("root", ROOT_CELLS), ("cluster", CLUSTER_CELLS), ("moss", MOSS_STRIPS)):
        for i, (x, y, w, h) in enumerate(cells):
            print("  %s %d: uv rect x=%.4f y=%.4f w=%.4f h=%.4f" % (name, i, x / N, 1 - (y + h) / N, w / N, h / N))
    if len(sys.argv) > 1:
        os.makedirs(os.path.dirname(sys.argv[1]), exist_ok=True)
        im.save(sys.argv[1])


if __name__ == "__main__":
    main()
