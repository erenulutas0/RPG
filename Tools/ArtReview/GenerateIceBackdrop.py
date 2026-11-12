"""Floor-2 (frozen Quicksilver Vaults) backdrop plate, the ice counterpart of
ENV_Furnace_Cavern_v1.png: same 1024x1536 RGB size and composition (a chasm framed
by near cliffs left and right, a hazy far middle, a dark lower band where the arena
meets it), painted procedurally.

v4 (the lead's notes on v3): the cliffs are faceted ice-and-rock planes (Voronoi
shards, each a lit or shadowed plane) with a few large fractures instead of streaks;
the spires are two-tone faceted (lit and dark face split on a ridge, a thin bright
edge on the lit ridge, varied widths, leaning and broken tips); the aurora is a set
of soft curved ribbons with a gentle vertical fall-off.

Back to front: deep sky, aurora ribbons, far spires (hazy), mid spires, frost haze,
near cliffs with frost-light windows where the foundry had lava windows, icicles at
the top corners, the dark lower band, frost motes.

Usage: python Tools/ArtReview/GenerateIceBackdrop.py [preview.png]
See docs/49_ICE_MAP_ART_DIRECTION.md section 6.
"""
import math
import os
import sys
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy.spatial import cKDTree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Environment", "ENV_Ice_Vault_v1.png")
W, H = 1024, 1536
SS = 2  # supersampling for the polygon layers


def c(h):
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


SKY, SKY_HI = c("0D1424"), c("1A2742")
TEAL, VIOLET = c("3FD9B0"), c("8B6CE8")
HAZE = c("6E8299")
NEAR = c("111722")
SHADOW_DEEP, SHADOW = c("1C2129"), c("2A3140")
LIT, LIT_EDGE = c("8C9AAB"), c("BFE6F2")
ICE_LIT = c("5FA8C8")
FROST_LIGHT, QS = c("A6F0FF"), c("7FE3FF")
BAND = c("0A0E16")


def blur(a, sigma):
    h, w = a.shape
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    g = np.exp(-(fx * fx + fy * fy) * (sigma * sigma) * 2.0 * np.pi ** 2)
    return np.real(np.fft.ifft2(np.fft.fft2(a) * g)).astype(np.float32)


def noise(rng, sigma, shape=(H, W)):
    n = blur(rng.standard_normal(shape).astype(np.float32), sigma)
    n -= n.mean()
    return n / (n.std() + 1e-6)


def fbm(rng, base, octaves=4):
    out, amp, tot = np.zeros((H, W), np.float32), 1.0, 0.0
    for _ in range(octaves):
        out += noise(rng, base) * amp
        tot += amp
        amp *= 0.55
        base /= 2.0
    return out / tot


def lerp(a, b, t):
    t = t[..., None] if np.ndim(t) == 2 else t
    return a * (1 - t) + b * t


def to_rgb255(col):
    return tuple(int(max(0, min(1, x)) * 255 + 0.5) for x in col)


# ---------------------------------------------------------------- aurora

def aurora(rng, u, v):
    """Soft curved ribbons: bright, fairly crisp lower hem, slow fade upward."""
    total = np.zeros((H, W), np.float32)
    colour_t = np.zeros((H, W), np.float32)
    ribbons = [  # base y, amplitude, wavelength, phase, upward fade, strength
        (0.27, 0.070, 1.10, 0.6, 0.075, 1.00, -0.10),
        (0.19, 0.050, 0.80, 2.3, 0.050, 0.65, 0.08),
        (0.34, 0.040, 1.40, 4.1, 0.060, 0.45, 0.05),
    ]
    x = u[0]
    for (by, amp, wl, ph, fade, k, tilt) in ribbons:
        hem = by + tilt * (x - 0.5) + amp * np.sin(2 * np.pi * x / wl + ph) + 0.012 * np.sin(2 * np.pi * x / (wl * 0.37) + ph * 2)
        hem = hem[None, :]
        d = v - hem  # >0 below the hem
        above = np.exp(-np.maximum(-d, 0) / fade)
        below = np.exp(-(np.maximum(d, 0) / 0.016) ** 2)
        shape = np.where(d < 0, above, below)
        # brightness varies smoothly along the ribbon (folds), never as bars
        fold = 0.35 + 0.65 * np.sin(2 * np.pi * x / (wl * 0.6) + ph * 3) ** 2
        fold = fold[None, :] * (0.8 + 0.2 * np.clip(noise(rng, 40, (1, W)), -2, 2))
        total += shape * fold * k
        colour_t = np.maximum(colour_t, shape * 0)  # placeholder keeps shapes aligned
    # very soft, low-contrast fine rays (texture, not bars)
    fine = 1.0 + 0.08 * noise(rng, 3.5, (1, W))
    total *= fine
    total *= np.clip(1 - (v - 0.40) / 0.2, 0, 1)
    ct = np.clip(u * 1.15 - 0.08 + 0.12 * np.sin(v * 7 + u * 3), 0, 1)
    col = lerp(TEAL, VIOLET, ct)
    col = lerp(col, c("D9FFF2"), np.clip(total - 1.0, 0, 0.6) * 0.5)  # hot cores lift toward white
    return col, np.clip(total, 0, 1.6)


# ---------------------------------------------------------------- spires

def spire_layer(rng, count, xmin, xmax, base_y, hmin, hmax, wmin, wmax, lit, dark, edge):
    """Two-tone faceted spires drawn as polygons at SS x; returns (rgb, alpha, shade-mask)."""
    w2, h2 = W * SS, H * SS
    rgb = Image.new("RGB", (w2, h2), (0, 0, 0))
    a = Image.new("L", (w2, h2), 0)
    dr, da = ImageDraw.Draw(rgb), ImageDraw.Draw(a)
    spires = []
    for _ in range(count):
        spires.append((rng.uniform(hmin, hmax), rng.uniform(xmin, xmax), rng.uniform(wmin, wmax)))
    spires.sort(key=lambda s: -s[0])  # tall first, short in front
    for (hgt, cx, hw) in spires:
        lean = rng.uniform(-0.10, 0.10) * hgt
        ax, ay = cx + lean, base_y - hgt
        lb, rb = (cx - hw, base_y), (cx + hw, base_y)
        rbx = cx + rng.uniform(-0.25, 0.35) * hw
        rbase = (rbx, base_y)
        tone = rng.uniform(0.85, 1.12)
        lit_c = to_rgb255(lit * tone)
        lit2_c = to_rgb255(lit * tone * 0.86)
        dark_c = to_rgb255(dark * rng.uniform(0.9, 1.08))
        broken = rng.random() < 0.3

        def along(p, t):
            return (ax + (p[0] - ax) * t, ay + (p[1] - ay) * t)

        if broken:
            t = rng.uniform(0.12, 0.26)
            pl, pr, pm = along(lb, t), along(rb, t), along(rbase, t + rng.uniform(-0.04, 0.06))
            notch = ((pl[0] + pm[0]) / 2 + rng.uniform(-3, 3), (pl[1] + pm[1]) / 2 + rng.uniform(4, 14))
            lit_poly = [pl, notch, pm, rbase, lb]
            dark_poly = [pm, pr, rb, rbase]
            ridge_top = pm
            cap = [pl, notch, pm, pr, (pr[0] - 2, pr[1] - 4), (pm[0], pm[1] - 6)]
        else:
            lit_poly = [(ax, ay), rbase, lb]
            dark_poly = [(ax, ay), rb, rbase]
            ridge_top = (ax, ay)
            cap = None
        S = lambda pts: [(p[0] * SS, p[1] * SS) for p in pts]
        dr.polygon(S(lit_poly), fill=lit_c)
        da.polygon(S(lit_poly), fill=255)
        # secondary facet on the lit face: lower-left plane a touch darker
        mid_r = (ridge_top[0] + (rbase[0] - ridge_top[0]) * 0.55, ridge_top[1] + (base_y - ridge_top[1]) * 0.55)
        sub = [mid_r, rbase, (lb[0] + (rbase[0] - lb[0]) * 0.35, base_y), lb]
        dr.polygon(S(sub), fill=lit2_c)
        dr.polygon(S(dark_poly), fill=dark_c)
        da.polygon(S(dark_poly), fill=255)
        if cap:
            dr.polygon(S(cap[:3] + [cap[3]]), fill=to_rgb255(lit * tone * 0.75))
        # thin bright edge down the lit ridge (upper part only)
        e = along(rbase, 0.6) if not broken else (ridge_top[0] + (rbase[0] - ridge_top[0]) * 0.5,
                                                  ridge_top[1] + (base_y - ridge_top[1]) * 0.5)
        dr.line(S([ridge_top, e]), fill=to_rgb255(edge), width=2)
        # rim on the lit outer edge near the tip
        lt = along(lb, 0.25) if not broken else lit_poly[0]
        dr.line(S([ridge_top if not broken else lit_poly[0], lt]), fill=to_rgb255(edge * 0.8), width=1)
    rgb = rgb.resize((W, H), Image.LANCZOS)
    a = a.resize((W, H), Image.LANCZOS)
    return np.asarray(rgb).astype(np.float32) / 255.0, np.asarray(a).astype(np.float32) / 255.0


# ---------------------------------------------------------------- cliffs

def angular_edge(rng, base, amp, step, inward_bottom_sign):
    ys = np.arange(-step, H + step * 2, step, dtype=np.float32)
    xs = base + rng.uniform(-amp, amp, len(ys))
    xs += np.cumsum(rng.uniform(-amp * 0.3, amp * 0.3, len(ys)))
    xs -= (xs.mean() - base)
    y = np.arange(H, dtype=np.float32)
    e = np.interp(y, ys + rng.uniform(-step * 0.3, step * 0.3, len(ys)).cumsum() * 0 , xs)
    v = y / H
    e = e + inward_bottom_sign * ((1 - v) * 30 - np.clip(v - 0.82, 0, 1) * 600)
    return e


def cliffs(rng, xx, yy, u, v):
    left_e = angular_edge(rng, W * 0.24, 55, 70, 1)[:, None]
    right_e = angular_edge(rng, W * 0.76, 55, 70, -1)[:, None]
    left_m = np.clip((left_e - xx) / 1.5, 0, 1)
    right_m = np.clip((xx - right_e) / 1.5, 0, 1)
    mask = np.maximum(left_m, right_m)
    edge_d = np.where(xx < W / 2, left_e - xx, xx - right_e)

    # Voronoi shards, stretched vertically (tall ice planes).
    n_pts = 90
    px = np.concatenate([rng.uniform(-40, W * 0.40, n_pts // 2), rng.uniform(W * 0.60, W + 40, n_pts // 2)])
    py = rng.uniform(-40, H + 40, n_pts)
    sy = 0.6
    tree = cKDTree(np.stack([px, py * sy], 1))
    pts = np.stack([xx.ravel(), yy.ravel() * sy], 1)
    d, idx = tree.query(pts, k=2)
    d1 = d[:, 0].reshape(H, W).astype(np.float32)
    d2 = d[:, 1].reshape(H, W).astype(np.float32)
    cell = idx[:, 0].reshape(H, W)
    border = d2 - d1

    # each shard is a plane: a facing value plus a slight in-plane gradient
    face = rng.uniform(0, 1, n_pts).astype(np.float32) ** 1.3
    gx = rng.uniform(-1, 1, n_pts).astype(np.float32) / 900
    gy = rng.uniform(-1, 1, n_pts).astype(np.float32) / 1800
    s = face[cell] + gx[cell] * (xx - px[cell]) + gy[cell] * (yy - py[cell])
    # planes near the chasm opening catch the aurora; deep in the wall stays shadow
    catch = np.exp(-np.clip(edge_d, 0, None) / 70.0)
    lit_amt = np.clip((s - 0.35) * catch * 1.6, 0, 1)
    shade = np.clip(s * 0.9, 0, 1)
    col = lerp(SHADOW_DEEP * 0.6, SHADOW * 1.05, shade)
    col = lerp(col, lerp(LIT * 0.85, LIT_EDGE, np.clip(lit_amt * 1.4 - 0.5, 0, 1)), lit_amt ** 1.5 * 0.85)
    # ice glaze tint on some shards
    icy = (rng.random(n_pts) < 0.35)[cell]
    col = lerp(col, col * 0.7 + ICE_LIT * 0.3 * (0.3 + lit_amt[..., None]), icy * 0.5)
    # shard borders: dark seam, with a bright lip on lit shards
    seam = np.clip(1 - border / 1.6, 0, 1)
    lip = np.clip(1 - np.abs(border - 3.2) / 1.4, 0, 1) * np.clip(lit_amt * 2, 0, 1)
    col = lerp(col, c("0B0F15"), seam * 0.6)
    col = lerp(col, LIT_EDGE, lip * 0.55)
    # subtle painted texture inside planes (not streaks)
    col = col * (0.9 + 0.1 * fbm(rng, 14, 3))[..., None]

    # rim where the cliff silhouette meets the chasm
    rim = np.exp(-np.clip(edge_d, 0, None) / 3.5) * (0.55 + 0.45 * np.clip(fbm(rng, 25, 2) + 0.5, 0, 1))
    rim_col = lerp(LIT, LIT_EDGE, np.clip(1 - v * 1.1, 0, 1))
    col = lerp(col, rim_col, rim * mask * 0.85)

    # a few large fractures
    frac = Image.new("L", (W * SS, H * SS), 0)
    flip = Image.new("L", (W * SS, H * SS), 0)
    fd, fl = ImageDraw.Draw(frac), ImageDraw.Draw(flip)
    for side in (0, 1, 0, 1, 0, 1, 0):
        x0 = rng.uniform(30, W * 0.2) if side == 0 else rng.uniform(W * 0.8, W - 30)
        y0 = rng.uniform(80, H * 0.75)
        ptsl = [(x0, y0)]
        ang = rng.uniform(math.radians(60), math.radians(120))
        for _ in range(rng.integers(5, 10)):
            ang += rng.uniform(-0.5, 0.5)
            L = rng.uniform(18, 50)
            ptsl.append((ptsl[-1][0] + math.cos(ang) * L * 0.6, ptsl[-1][1] + math.sin(ang) * L))
            if rng.random() < 0.25:  # a short branch
                b = ptsl[-1]
                ba = ang + rng.choice([-1, 1]) * rng.uniform(0.6, 1.1)
                fd.line([(b[0] * SS, b[1] * SS), ((b[0] + math.cos(ba) * 30) * SS, (b[1] + math.sin(ba) * 30) * SS)], fill=200, width=2 * SS)
        fd.line([(p[0] * SS, p[1] * SS) for p in ptsl], fill=255, width=3 * SS, joint="curve")
        fl.line([((p[0] - 2) * SS, (p[1] - 1) * SS) for p in ptsl], fill=255, width=1 * SS)
    frac = np.asarray(frac.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    flip = np.asarray(flip.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    col = lerp(col, c("06090E"), frac * 0.9)
    col = lerp(col, LIT, flip * 0.45 * (0.4 + catch))
    return col, mask, edge_d


def main():
    rng = np.random.default_rng(2049)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    v, u = yy / H, xx / W

    # Sky, brightening into haze toward the horizon.
    img = lerp(SKY, SKY_HI, np.clip(v * 1.4, 0, 1) * 0.8 + fbm(rng, 90) * 0.06)
    img = lerp(img, HAZE * 0.75, np.clip((v - 0.35) / 0.45, 0, 1) ** 1.3 * 0.75)

    # Aurora ribbons, plus a faint glow on the sky beneath them.
    aur_col, aur = aurora(rng, u, v)
    img = img + aur_col * (aur * 1.1)[..., None]
    img = img + aur_col * (blur(aur, 40) * 0.25)[..., None]

    # Far spires, then mid spires (two-tone faceted), each fogged by depth.
    far_rgb, far_a = spire_layer(rng, 22, 170, 860, H * 0.90, H * 0.16, H * 0.46, 26, 84,
                                 c("405873"), c("1E2A3E"), c("9FD8EA"))
    fog = np.clip((v - 0.38) / 0.5, 0, 1) * 0.45 + 0.08
    far_rgb = lerp(far_rgb, HAZE * 0.62, fog)
    img = lerp(img, far_rgb, far_a)

    mid_rgb, mid_a = spire_layer(rng, 10, 250, 780, H * 0.93, H * 0.12, H * 0.30, 45, 120,
                                 c("4A6280"), c("141B28"), c("BFE6F2"))
    fog = np.clip((v - 0.60) / 0.4, 0, 1) * 0.22
    mid_rgb = lerp(mid_rgb, HAZE * 0.5, fog)
    img = lerp(img, mid_rgb, mid_a)

    # Frost haze rising from the chasm floor.
    haze_amt = np.clip((v - 0.55) / 0.35, 0, 1) ** 1.4 * (0.55 + 0.45 * np.clip(fbm(rng, 70) * 0.8 + 0.5, 0, 1))
    img = lerp(img, HAZE, haze_amt * 0.30)

    # Near cliffs.
    cl_col, cl_m, edge_d = cliffs(rng, xx, yy, u, v)
    img = lerp(img, cl_col, cl_m)

    # Frost-light windows where the foundry had lava windows (arched, cold glow).
    core = Image.new("L", (W * SS, H * SS), 0)
    cd = ImageDraw.Draw(core)
    windows = [(150, 560, 10, 46), (95, 760, 8, 30), (210, 420, 7, 24), (165, 880, 9, 38), (60, 1080, 7, 26),
               (940, 700, 10, 48), (975, 950, 8, 34), (880, 520, 7, 22), (905, 1120, 8, 30),
               (520, 640, 3, 9), (470, 900, 3, 8), (640, 1010, 3, 8), (590, 760, 2, 6)]
    for (wx, wy, ww, wh) in windows:
        x0, x1, y0, y1 = (wx - ww / 2) * SS, (wx + ww / 2) * SS, (wy - wh / 2) * SS, (wy + wh / 2) * SS
        cd.rectangle([x0, y0 + (x1 - x0) / 2, x1, y1], fill=255)
        cd.ellipse([x0, y0, x1, y0 + (x1 - x0)], fill=255)
    core = np.asarray(core.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    glow = blur(core, 8) * 5 + blur(core, 30) * 9
    img = img + QS * np.clip(glow, 0, 1.2)[..., None] * 0.42
    img = lerp(img, FROST_LIGHT, core * 0.9)

    # Icicles from the top corners (the foundry's stalactites), two-tone.
    ic = Image.new("RGB", (W * SS, H * SS), (0, 0, 0))
    ia = Image.new("L", (W * SS, H * SS), 0)
    di, dai = ImageDraw.Draw(ic), ImageDraw.Draw(ia)
    for _ in range(40):
        left = rng.random() < 0.5
        cx = rng.uniform(0, 330) if left else rng.uniform(694, 1024)
        depth = np.clip(abs(cx - 512) / 330, 0.3, 1.0)
        ln = rng.uniform(50, 240) * depth
        hw = rng.uniform(6, 20)
        tip = (cx + rng.uniform(-6, 6), ln)
        ridge = cx + rng.uniform(-0.3, 0.3) * hw
        S = lambda pts: [(p[0] * SS, p[1] * SS) for p in pts]
        di.polygon(S([(cx - hw, -2), (ridge, -2), tip]), fill=to_rgb255(c("3A4E66") * rng.uniform(0.9, 1.2)))
        di.polygon(S([(ridge, -2), (cx + hw, -2), tip]), fill=to_rgb255(c("151C28")))
        di.line(S([(ridge, ln * 0.1), tip]), fill=to_rgb255(LIT_EDGE * 0.8), width=SS)
        dai.polygon(S([(cx - hw, -2), (cx + hw, -2), tip]), fill=255)
    ic = np.asarray(ic.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    ia = np.asarray(ia.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    img = lerp(img, ic, ia)

    # The dark lower band where the arena meets the plate (rubble silhouette).
    band_top = H * 0.885 + noise(rng, 6, (1, W))[0] * 14 + noise(rng, 30, (1, W))[0] * 22
    band_m = np.clip((yy - band_top[None, :]) / 3.0, 0, 1)
    band_col = lerp(BAND, NEAR, np.clip(0.4 + 0.3 * fbm(rng, 10), 0, 1) * 0.6)
    band_col = lerp(band_col, BAND * 0.7, np.clip((v - 0.93) / 0.07, 0, 1))
    img = lerp(img, band_col, band_m)
    img = lerp(img, HAZE * 0.6, np.exp(-np.abs(yy - band_top[None, :]) / 10.0) * 0.18)

    # Drifting frost motes (few, soft).
    motes = (rng.random((H, W)) > 0.99990).astype(np.float32)
    img = img + LIT_EDGE * np.clip(blur(motes, 1.2) * 5, 0, 0.6)[..., None] * (1 - band_m[..., None])

    # Vignette, grain, painterly finish.
    vig = 1 - 0.35 * np.clip(((u - 0.5) ** 2 * 2.2 + (v - 0.45) ** 2 * 1.3), 0, 1)
    img = img * vig[..., None] + noise(rng, 0.8)[..., None] * 0.010
    img = np.clip(img, 0, 1)
    pil = Image.fromarray((img * 255 + 0.5).astype(np.uint8), "RGB")
    pil = pil.filter(ImageFilter.ModeFilter(3)).filter(ImageFilter.UnsharpMask(2, 50, 2))
    pil.save(OUT, optimize=True)
    print(OUT, pil.size, os.path.getsize(OUT), "bytes")
    if len(sys.argv) > 1:
        os.makedirs(os.path.dirname(sys.argv[1]), exist_ok=True)
        pil.save(sys.argv[1])


if __name__ == "__main__":
    main()
