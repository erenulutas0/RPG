"""Floor-3 (Gloam Gardens) backdrop plate concept, the garden counterpart of
ENV_Furnace_Cavern_v1.png and ENV_Ice_Vault_v1.png: same 1024x1536 RGB size and
composition (a chasm framed by near masses left and right, a hazy far middle, a dark
lower band where the arena meets it), painted procedurally.

The foundry's cliffs become two giant root-trunks; the far foundry towers stand
ruined and overgrown under tree crowns; the sky is a dusk-green gloam with the dimmed
sun (the Long Dimming, docs/44) low behind ash cloud; the foundry's lava windows
become gloamlight seed-lanterns and bracket fungi (memory-light, chartreuse); roots
hang from the top corners where the foundry had stalactites; ash and pollen drift.

Back to front: sky, dim sun, ash cloud bands, far ruined towers with crowns, mid
trees, gloam haze, near trunks (bark ridges, moss, lichen), hanging roots, lanterns
and fungi, the dark lower band, ash flakes and pollen motes.

Concept only: writes a preview, never into Assets.
Usage: python Tools/ArtReview/GenerateGloamBackdrop.py [preview.png]
Default out: E:/MobileGame/ArtDirection/2026-11-12/gloam-gardens-01/concept/backdrop.png
See docs/51_GLOAM_GARDENS_ART_DIRECTION.md section 6.
"""
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

W, H = 1024, 1536
SS = 2  # supersampling for the polygon layers
DEFAULT_OUT = os.path.join("E:/MobileGame", "ArtDirection", "2026-11-12", "gloam-gardens-01", "concept",
                           "backdrop.png")


def c(h):
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


# docs/51 palette
SKY_TOP, SKY_MID = c("0B110F"), c("23302A")
HAZE = c("767A68")
SUN, SUN_HALO = c("CDBE8E"), c("8E8A62")
FAR, FAR_DARK = c("2C362C"), c("1E2620")
MID, MID_RIM = c("18201A"), c("6E7A5A")
BARK_DEEP, BARK, HEARTWOOD = c("14100E"), c("2A221E"), c("5B4838")
MOSS, MOSS_LIT, LICHEN = c("3F5A32"), c("4F6B3A"), c("8FA35A")
GLOAM, GLOAM_CORE = c("C6EE6A"), c("E2F7A0")
ASH = c("A7A99C")
BAND = c("0B0F0C")


def blur(a, sy, sx=None):
    """Gaussian blur through the FFT; sx != sy stretches it (bark ridges, cloud bands)."""
    sx = sy if sx is None else sx
    h, w = a.shape
    fy = np.fft.fftfreq(h)[:, None]
    fx = np.fft.fftfreq(w)[None, :]
    g = np.exp(-((fx * sx) ** 2 + (fy * sy) ** 2) * 2.0 * np.pi ** 2)
    return np.real(np.fft.ifft2(np.fft.fft2(a) * g)).astype(np.float32)


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


def crown(dr, da, rng, cx, cy, rx, ry, col, rim=None, rim_dir=(0.0, -1.0), blobs=14):
    """A tree crown as a cluster of ellipses; the blobs on the sun side get a rim tone."""
    for _ in range(blobs):
        ang = rng.uniform(0, 2 * math.pi)
        r = rng.uniform(0.2, 1.0)
        bx, by = cx + math.cos(ang) * rx * r * 0.85, cy + math.sin(ang) * ry * r * 0.95
        br = rng.uniform(0.25, 0.62) * min(rx, ry)
        box = [(bx - br * 1.2) * SS, (by - br) * SS, (bx + br * 1.2) * SS, (by + br * 0.9) * SS]
        da.ellipse(box, fill=255)
        tone = rgb255(col * rng.uniform(0.85, 1.15))
        dr.ellipse(box, fill=tone)
        if rim is not None and (math.cos(ang) * rim_dir[0] + math.sin(ang) * rim_dir[1]) > 0.3:
            inner = [(bx - br * 0.9) * SS, (by - br * 0.95) * SS, (bx + br * 0.8) * SS, (by - br * 0.55) * SS]
            dr.chord(inner, 200, 340, fill=rgb255(rim * rng.uniform(0.7, 0.9)))


def tower(dr, da, rng, x, base, w, h, col):
    """A ruined foundry tower: a shaft, a stepped crown, one broken corner, a chimney stub."""
    tone = rgb255(col * rng.uniform(0.9, 1.1))
    top = base - h
    pts = [(x - w / 2, base), (x - w / 2, top + h * 0.12), (x - w * 0.62, top + h * 0.08), (x - w * 0.62, top + h * 0.03),
           (x - w * 0.2, top + h * 0.03), (x - w * 0.2, top - h * 0.04), (x + w * 0.05, top - h * 0.04),
           (x + w * 0.12, top + h * 0.02), (x + w * 0.35, top + h * 0.10), (x + w / 2, top + h * 0.18),
           (x + w / 2, base)]
    dr.polygon(S(pts), fill=tone)
    da.polygon(S(pts), fill=255)
    if rng.random() < 0.6:  # chimney stub
        cw = w * 0.18
        cx = x + rng.uniform(-0.25, 0.1) * w
        chim = [(cx - cw / 2, top), (cx - cw / 2, top - h * rng.uniform(0.15, 0.35)), (cx + cw / 2, top - h * 0.2),
                (cx + cw / 2, top)]
        dr.polygon(S(chim), fill=tone)
        da.polygon(S(chim), fill=255)
    # arched openings, darker
    for k in range(rng.integers(1, 3)):
        oy = top + h * (0.3 + 0.25 * k)
        ow = w * 0.22
        dr.rectangle([(x - ow / 2) * SS, oy * SS, (x + ow / 2) * SS, (oy + h * 0.12) * SS], fill=rgb255(col * 0.6))
        dr.ellipse([(x - ow / 2) * SS, (oy - ow / 2) * SS, (x + ow / 2) * SS, (oy + ow / 2) * SS], fill=rgb255(col * 0.6))
    return top


def hanging_root(dr, da, rng, x0, y0, length, width, col, edge, sway):
    """A tapered hanging root/vine: a polyline that narrows to a point and sways."""
    n = 16
    pts = []
    for i in range(n + 1):
        t = i / n
        x = x0 + sway * math.sin(t * 2.2 + rng.uniform(0, 0.3)) * t * length * 0.25
        pts.append((x, y0 + t * length))
    left, right = [], []
    for i, (x, y) in enumerate(pts):
        wv = width * (1 - i / n) ** 0.8 + 0.6
        left.append((x - wv / 2, y))
        right.append((x + wv / 2, y))
    poly = left + right[::-1]
    dr.polygon(S(poly), fill=rgb255(col * rng.uniform(0.85, 1.2)))
    da.polygon(S(poly), fill=255)
    dr.line(S(left[: int(n * 0.7)]), fill=rgb255(edge), width=max(1, SS))


def near_trunks(rng, xx, yy, v):
    """Two giant root-trunks framing the chasm; returns colour, mask and distance-to-edge."""
    y = np.arange(H, dtype=np.float32)
    out = []
    for side in (0, 1):
        base = W * (0.22 if side == 0 else 0.78)
        wav = (np.sin(y / 210 + rng.uniform(0, 6)) * 26 + np.sin(y / 67 + rng.uniform(0, 6)) * 9
               + noise(rng, 8, 8, (1, H))[0] * 4)
        # the trunk flares into roots at the bottom and leans into a bough at the top
        flare = np.clip((y / H - 0.70) / 0.25, 0, 1) ** 2 * 170
        bough = np.clip((0.12 - y / H) / 0.12, 0, 1) ** 1.6 * 70 * (1.0 if side == 0 else 0.6)
        sign = 1 if side == 0 else -1
        out.append(base + sign * (wav + flare + bough))
    left_e, right_e = out[0][:, None], out[1][:, None]
    left_m = np.clip((left_e - xx) / 1.5, 0, 1)
    right_m = np.clip((xx - right_e) / 1.5, 0, 1)
    mask = np.maximum(left_m, right_m)
    edge_d = np.where(xx < W / 2, left_e - xx, xx - right_e)
    # cylindrical shading: lit near the chasm edge (sky light), dark deep in the trunk
    roundness = np.clip(edge_d / 160.0, 0, 1)
    lit = np.exp(-np.clip(edge_d, 0, None) / 55.0)
    # bark: vertical ridges (stretched noise), deep furrows
    ridge = noise(rng, 26, 2.2) * 0.65 + noise(rng, 60, 5) * 0.35
    furrow = np.clip(-ridge - 0.55, 0, 1)
    tone = np.clip(0.45 + ridge * 0.22, 0, 1)
    col = lerp(BARK_DEEP, BARK, tone)
    col = lerp(col, HEARTWOOD, np.clip(lit * (0.6 + ridge * 0.4), 0, 1) * 0.75)
    col = lerp(col, BARK_DEEP * 0.6, furrow * 0.8)
    col = col * (1 - roundness[..., None] * 0.35)
    # moss on the ridges, heavier low on the trunk and at the bough; lichen specks
    m = fbm(rng, 34, 3, 1.3) + 0.3 * np.clip(ridge, 0, 1) + 0.7 * np.clip((v - 0.62) / 0.3, 0, 1) - 0.75
    moss = np.clip(m * 1.4, 0, 1) * (1 - furrow)
    moss_col = lerp(MOSS * 0.7, MOSS_LIT * 0.85, np.clip(lit * 1.5, 0, 1)) * (0.75 + 0.25 * tone[..., None])
    col = lerp(col, moss_col, moss * 0.6)
    lich = (rng.random((H, W)) > 0.9975).astype(np.float32)
    lich = np.clip(blur(lich, 1.4) * 9, 0, 1) * np.clip(m + 0.3, 0, 1)
    col = lerp(col, LICHEN * 0.8, lich * 0.7)
    # rim where the trunk silhouette meets the gloam
    rim = np.exp(-np.clip(edge_d, 0, None) / 3.0) * (0.5 + 0.5 * np.clip(fbm(rng, 25, 2) + 0.5, 0, 1))
    col = lerp(col, MID_RIM * 1.1, rim * mask * 0.7)
    return col, mask, edge_d


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else DEFAULT_OUT
    rng = np.random.default_rng(3051)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    v, u = yy / H, xx / W

    # Sky: deep green-black gloam brightening into ash haze toward the horizon.
    img = lerp(SKY_TOP, SKY_MID, np.clip(v * 1.6, 0, 1) * 0.9 + fbm(rng, 90) * 0.05)
    img = lerp(img, HAZE * 0.7, np.clip((v - 0.32) / 0.45, 0, 1) ** 1.3 * 0.7)

    # The dimmed sun: a pale ash-gold disc, low saturation, a wide soft halo; ash cloud bands cross it.
    sx, sy, sr = W * 0.53, H * 0.205, 74.0
    d = np.sqrt((xx - sx) ** 2 + (yy - sy) ** 2)
    disc = np.clip((sr - d) / 3.5, 0, 1)
    mottle = 0.95 + 0.05 * noise(rng, 14) - 0.08 * np.clip(d / sr, 0, 1) ** 3
    halo = np.exp(-np.clip(d - sr, 0, None) / 28.0) * 0.55 + np.exp(-np.clip(d - sr, 0, None) / 90.0) * 0.35 + np.exp(-np.clip(d - sr, 0, None) / 300.0) * 0.22
    img = img + SUN_HALO * halo[..., None] * 0.6
    img = lerp(img, SUN * mottle[..., None] * 0.92, disc * 0.95)
    clouds = np.clip(noise(rng, 7, 120) * 0.7 + noise(rng, 16, 260) * 0.5 - 0.15, 0, 1)
    clouds *= np.exp(-((v - 0.22) / 0.12) ** 2) + 0.35 * np.exp(-((v - 0.40) / 0.08) ** 2)
    cloud_col = lerp(c("1C2620"), SUN_HALO * 0.8, np.clip(halo * 1.2, 0, 1))
    img = lerp(img, cloud_col, np.clip(clouds, 0, 0.85))

    # Far: ruined foundry towers, overgrown, under tree crowns; fogged by depth.
    rgb, a, dr, da = layer()
    for _ in range(9):
        x = rng.uniform(200, 830)
        base = H * rng.uniform(0.80, 0.86)
        tw, th = rng.uniform(40, 80), H * rng.uniform(0.28, 0.46)
        top = tower(dr, da, rng, x, base, tw, th, FAR)
        if rng.random() < 0.8:
            crown(dr, da, rng, x + rng.uniform(-20, 20), top + rng.uniform(-10, 25), tw * rng.uniform(0.9, 1.6),
                  tw * rng.uniform(0.6, 1.0), FAR * 1.05, rim=FAR * 1.5, rim_dir=(0.0, -1.0), blobs=10)
        for _ in range(rng.integers(2, 6)):  # roots draped down the tower
            hanging_root(dr, da, rng, x + rng.uniform(-tw / 2, tw / 2), top + rng.uniform(0, 20),
                         rng.uniform(40, th * 0.6), rng.uniform(2, 5), FAR_DARK, FAR * 1.3, rng.uniform(-1, 1))
    for _ in range(7):  # free-standing far crowns
        cx = rng.uniform(220, 800)
        cy = H * rng.uniform(0.50, 0.66)
        crown(dr, da, rng, cx, cy, rng.uniform(60, 120), rng.uniform(40, 70), FAR * 0.95, rim=FAR * 1.45, blobs=12)
        dr.rectangle([(cx - 6) * SS, cy * SS, (cx + 6) * SS, H * 0.9 * SS], fill=rgb255(FAR * 0.9))
        da.rectangle([(cx - 6) * SS, cy * SS, (cx + 6) * SS, H * 0.9 * SS], fill=255)
    far_rgb, far_a = flatten(rgb, a)
    fog = np.clip((v - 0.45) / 0.5, 0, 1) * 0.25 + 0.04
    far_rgb = lerp(far_rgb, HAZE * 0.62, fog)
    img = lerp(img, far_rgb, far_a)

    # Mid: a few great trees with flared roots, dark, rim-lit from the sun side.
    rgb, a, dr, da = layer()
    for (tx, base, th, tw) in [(330, H * 0.93, H * 0.36, 30), (690, H * 0.94, H * 0.40, 34),
                               (505, H * 0.95, H * 0.26, 22)]:
        tx += rng.uniform(-20, 20)
        top = base - th
        lean = rng.uniform(-30, 30)
        trunk = [(tx - tw, base), (tx - tw * 0.45 + lean, top), (tx + tw * 0.45 + lean, top), (tx + tw, base)]
        dr.polygon(S(trunk), fill=rgb255(MID))
        da.polygon(S(trunk), fill=255)
        for k in range(5):  # root flare
            rx = tx + (k - 2) * tw * 0.9
            root = [(tx - tw * 0.4, base - th * 0.12), (rx + rng.uniform(-10, 10), base + 6), (tx + tw * 0.4, base - th * 0.12)]
            dr.polygon(S(root), fill=rgb255(MID * 0.9))
            da.polygon(S(root), fill=255)
        # sun-side rim along the trunk
        side = 1 if tx < sx else -1
        dr.line(S([(tx + side * tw * 0.45 + lean, top), (tx + side * tw, base)]), fill=rgb255(MID_RIM * 0.7), width=2 * SS)
        crown(dr, da, rng, tx + lean, top - th * 0.05, tw * 4.5, tw * 2.4, MID * 1.1, rim=MID_RIM,
              rim_dir=((sx - tx) / abs(sx - tx + 1e-3) * 0.4, -1.0), blobs=22)
        for _ in range(rng.integers(4, 8)):
            hanging_root(dr, da, rng, tx + lean + rng.uniform(-tw * 3, tw * 3), top + rng.uniform(0, tw),
                         rng.uniform(60, 180), rng.uniform(2, 4), MID * 0.8, MID_RIM * 0.6, rng.uniform(-1, 1))
    mid_rgb, mid_a = flatten(rgb, a)
    mid_rgb = lerp(mid_rgb, HAZE * 0.5, np.clip((v - 0.62) / 0.4, 0, 1) * 0.25)
    img = lerp(img, mid_rgb, mid_a)

    # The ash field: low pale mounds in the middle distance where the garden grows, with gloamlight sprouts.
    rgb, a, dr, da = layer()
    for k in range(3):
        base_y = H * (0.80 + k * 0.03)
        xs = np.linspace(-20, W + 20, 40)
        ys = base_y - (np.sin(xs / rng.uniform(60, 120) + rng.uniform(0, 6)) * 0.5 + 0.5) * rng.uniform(12, 30)
        poly = [(x, y) for x, y in zip(xs, ys)] + [(W + 20, H * 0.92), (-20, H * 0.92)]
        tone = lerp(MID * 1.3, ASH * 0.40, np.array(0.45 - k * 0.12))
        dr.polygon(S(poly), fill=rgb255(tone))
        da.polygon(S(poly), fill=255)
        dr.line(S(list(zip(xs, ys))), fill=rgb255(ASH * (0.42 - k * 0.05)), width=SS)
        for _ in range(60):  # dark tufts and saplings growing out of the ash
            i = rng.integers(0, len(xs) - 1)
            tx = xs[i] + rng.uniform(0, xs[1] - xs[0])
            ty = np.interp(tx, xs, ys) + rng.uniform(0, 10)
            th = rng.uniform(3, 14)
            for blade in range(3):
                dr.line(S([(tx, ty), (tx + rng.uniform(-4, 4), ty - th * rng.uniform(0.6, 1.0))]),
                        fill=rgb255(MID * 0.9), width=SS)
    ash_rgb, ash_a = flatten(rgb, a)
    ash_rgb = lerp(ash_rgb, HAZE * 0.55, 0.22)
    img = lerp(img, ash_rgb, ash_a)
    sprouts = Image.new("L", (W * SS, H * SS), 0)
    sd = ImageDraw.Draw(sprouts)
    for _ in range(40):
        px, py = rng.uniform(260, 770), H * rng.uniform(0.80, 0.88)
        sd.ellipse([(px - 1.3) * SS, (py - 1.3) * SS, (px + 1.3) * SS, (py + 1.3) * SS], fill=255)
    sprouts = np.asarray(sprouts.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    img = img + GLOAM * np.clip(blur(sprouts, 3) * 6 + sprouts, 0, 1)[..., None] * 0.45

    # Gloam haze rising from the chasm floor (ash and spores).
    haze_amt = np.clip((v - 0.55) / 0.35, 0, 1) ** 1.4 * (0.55 + 0.45 * np.clip(fbm(rng, 70) * 0.8 + 0.5, 0, 1))
    img = lerp(img, HAZE, haze_amt * 0.32)

    # Near trunks.
    tr_col, tr_m, edge_d = near_trunks(rng, xx, yy, v)
    img = lerp(img, tr_col, tr_m)

    # Hanging roots from the top corners (the foundry's stalactites) and across the top.
    rgb, a, dr, da = layer()
    for _ in range(46):
        left = rng.random() < 0.5
        x0 = rng.uniform(0, 360) if left else rng.uniform(664, 1024)
        depth = np.clip(abs(x0 - 512) / 340, 0.25, 1.0)
        hanging_root(dr, da, rng, x0, -4, rng.uniform(80, 420) * depth, rng.uniform(3, 11) * depth, BARK * 0.9,
                     HEARTWOOD * 0.9, rng.uniform(-1.2, 1.2))
    root_rgb, root_a = flatten(rgb, a)
    img = lerp(img, root_rgb, root_a)

    # Gloamlight: seed-lanterns hanging on threads from the roots and boughs, bracket fungi on the trunks.
    core = Image.new("L", (W * SS, H * SS), 0)
    thread = Image.new("L", (W * SS, H * SS), 0)
    cd, td = ImageDraw.Draw(core), ImageDraw.Draw(thread)
    lanterns = [(250, 240, 9), (300, 390, 7), (215, 520, 6), (760, 280, 9), (720, 450, 7), (810, 600, 6),
                (420, 330, 4), (610, 380, 4), (470, 560, 3), (560, 640, 3), (380, 700, 3), (650, 760, 3)]
    for (lx, ly, lr) in lanterns:
        td.line([lx * SS, 0, lx * SS, (ly - lr) * SS], fill=170, width=SS)
        cd.ellipse([(lx - lr * 0.8) * SS, (ly - lr * 1.2) * SS, (lx + lr * 0.8) * SS, (ly + lr * 1.2) * SS], fill=255)
    fungi = [(165, 640, 26), (120, 820, 20), (205, 940, 30), (80, 1100, 18), (150, 1210, 24),
             (880, 700, 28), (930, 880, 20), (845, 1010, 24), (950, 1160, 18), (870, 1260, 22)]
    for (fx, fy, fw) in fungi:
        # a bracket on the trunk's face: a flat half-ellipse with a lit underside line
        for k in range(rng.integers(1, 4)):
            ox, oy, ow = fx + rng.uniform(-10, 10), fy + k * fw * 0.45, fw * rng.uniform(0.6, 1.0)
            cd.chord([(ox - ow) * SS, (oy - ow * 0.35) * SS, (ox + ow) * SS, (oy + ow * 0.35) * SS], 0, 180, fill=200)
    core = np.asarray(core.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    thread = np.asarray(thread.resize((W, H), Image.LANCZOS)).astype(np.float32) / 255
    img = lerp(img, HEARTWOOD * 0.8, thread * 0.5)
    glow = blur(core, 6) * 4 + blur(core, 26) * 7
    img = img + GLOAM * np.clip(glow, 0, 1.2)[..., None] * 0.36
    img = lerp(img, GLOAM_CORE, core * 0.9)

    # The dark lower band where the arena meets the plate (roots and rubble).
    band_top = H * 0.885 + noise(rng, 6, 6, (1, W))[0] * 14 + noise(rng, 30, 30, (1, W))[0] * 22
    band_m = np.clip((yy - band_top[None, :]) / 3.0, 0, 1)
    band_col = lerp(BAND, MID, np.clip(0.4 + 0.3 * fbm(rng, 10), 0, 1) * 0.6)
    band_col = lerp(band_col, BAND * 0.7, np.clip((v - 0.93) / 0.07, 0, 1))
    img = lerp(img, band_col, band_m)
    img = lerp(img, HAZE * 0.6, np.exp(-np.abs(yy - band_top[None, :]) / 10.0) * 0.16)

    # Drifting ash flakes (pale, slightly streaked) and a few pollen motes (gloamlight).
    flakes = (rng.random((H, W)) > 0.99994).astype(np.float32)
    img = img + ASH * 0.8 * np.clip(blur(flakes, 2.6, 1.0) * 8, 0, 0.3)[..., None] * (1 - band_m[..., None])
    motes = (rng.random((H, W)) > 0.99993).astype(np.float32)
    img = img + GLOAM * np.clip(blur(motes, 1.3) * 7, 0, 0.7)[..., None] * (1 - band_m[..., None])

    # Vignette, grain, painterly finish.
    vig = 1 - 0.35 * np.clip(((u - 0.5) ** 2 * 2.2 + (v - 0.45) ** 2 * 1.3), 0, 1)
    img = img * vig[..., None] + noise(rng, 0.8)[..., None] * 0.010
    img = np.clip(img, 0, 1)
    pil = Image.fromarray((img * 255 + 0.5).astype(np.uint8), "RGB")
    pil = pil.filter(ImageFilter.ModeFilter(3)).filter(ImageFilter.UnsharpMask(2, 50, 2))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    pil.save(out, optimize=True)
    print(out, pil.size, os.path.getsize(out), "bytes")


if __name__ == "__main__":
    main()
