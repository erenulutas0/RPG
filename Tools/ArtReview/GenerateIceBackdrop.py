"""Floor-2 (frozen Quicksilver Vaults) backdrop plate, the ice counterpart of
ENV_Furnace_Cavern_v1.png: same 1024x1536 RGB size and composition (a chasm framed
by near cliffs left and right, a hazy far middle, a dark lower band where the arena
meets it), painted procedurally.

Layers, back to front: deep sky, aurora band (teal -> violet), far ice spires (hazy),
mid spires, frost haze rising from the bottom, near ice cliffs with frost-light
windows where the foundry had lava windows, icicles at the top corners, the dark
lower band. A mode filter and a light grain give it a painterly finish.

Usage: python Tools/ArtReview/GenerateIceBackdrop.py [preview.png]
See docs/49_ICE_MAP_ART_DIRECTION.md section 6.
"""
import os
import sys
import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "Assets", "_Project", "Art", "Environment", "ENV_Ice_Vault_v1.png")
W, H = 1024, 1536


def c(h):
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32) / 255.0


SKY, SKY_HI = c("0D1424"), c("1A2742")
TEAL, VIOLET = c("3FD9B0"), c("8B6CE8")
HAZE = c("6E8299")
FAR, MID, NEAR = c("2A3850"), c("1A2436"), c("111722")
ICE_LIT, ICE_EDGE = c("5FA8C8"), c("BFE6F2")
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
    for o in range(octaves):
        out += noise(rng, base / (2 ** o)) * amp
        tot += amp
        amp *= 0.55
    return out / tot


def streaks(rng, sx, sy):
    """Anisotropic noise: long vertical strokes (ice faces, aurora rays)."""
    n = rng.standard_normal((H, W)).astype(np.float32)
    fy = np.fft.fftfreq(H)[:, None]
    fx = np.fft.fftfreq(W)[None, :]
    g = np.exp(-((fx * sx) ** 2 + (fy * sy) ** 2) * 2.0 * np.pi ** 2)
    n = np.real(np.fft.ifft2(np.fft.fft2(n) * g)).astype(np.float32)
    n -= n.mean()
    return n / (n.std() + 1e-6)


def spire_profile(rng, count, xmin, xmax, base_y, hmin, hmax, wmin, wmax):
    """Top edge y(x) of a row of jagged spires (min over triangles)."""
    x = np.arange(W, dtype=np.float32)
    top = np.full(W, base_y, np.float32)
    for _ in range(count):
        cx = rng.uniform(xmin, xmax)
        hw = rng.uniform(wmin, wmax)
        hgt = rng.uniform(hmin, hmax)
        lean = rng.uniform(-0.25, 0.25)
        d = np.abs(x - cx - lean * hw) / hw
        tri = base_y - hgt * np.clip(1 - d, 0, 1) ** 0.85
        top = np.minimum(top, tri)
    return top


def lerp(a, b, t):
    t = t[..., None] if np.ndim(t) == 2 else t
    return a * (1 - t) + b * t


def main():
    rng = np.random.default_rng(2049)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    v, u = yy / H, xx / W

    # Sky.
    img = lerp(SKY, SKY_HI, np.clip(v * 1.4, 0, 1) * 0.8 + fbm(rng, 90) * 0.08)
    img = lerp(img, HAZE * 0.75, np.clip((v - 0.35) / 0.45, 0, 1) ** 1.3 * 0.75)

    # Aurora: a wavy band across the upper third with vertical rays and a soft skirt.
    centre = 0.22 + 0.05 * np.sin(u * 5.0 + 0.7) + 0.02 * np.sin(u * 13.0)
    dv = v - centre
    core = np.exp(-(dv / 0.05) ** 2)
    skirt = 0.7 * np.exp(-(dv / 0.05) ** 2 * (dv < 0)) * np.exp(-np.maximum(dv, 0) / 0.16) * np.clip(1 + dv / 0.08, 0, 1)
    skirt *= np.clip(1 - (v - 0.42) / 0.25, 0, 1)
    rays = np.clip(0.65 + 0.35 * streaks(rng, 14, 200) + 0.15 * streaks(rng, 4, 120), 0, 1.4)
    aur = (core + skirt) * rays * (0.75 + 0.25 * fbm(rng, 60))
    aur_col = lerp(TEAL, VIOLET, np.clip(u * 1.1 - 0.05 + 0.15 * np.sin(v * 9), 0, 1))
    img = img + aur_col * (np.clip(aur, 0, 1.5) * 0.85)[..., None]

    # Far spires (hazy, lit from the aurora above).
    far_top = spire_profile(rng, 40, 150, 880, H * 0.86, H * 0.12, H * 0.48, 16, 70)
    far_top += noise(rng, 2, (1, W))[0] * 4
    far_mask = np.clip((yy - far_top[None, :]) / 2.0, 0, 1)
    face = np.clip(0.5 + 0.5 * streaks(rng, 3, 40) * 0.6 + fbm(rng, 30) * 0.2, 0, 1)
    rim = np.exp(-np.clip(yy - far_top[None, :], 0, None) / 18.0)
    far_col = lerp(FAR * 0.95, ICE_LIT * 0.45, face * 0.3)
    far_col = lerp(far_col, aur_col * 0.6 + ICE_EDGE * 0.2, rim * 0.45)
    fog_far = np.clip((v - 0.40) / 0.5, 0, 1) * 0.45
    far_col = lerp(far_col, HAZE * 0.6, fog_far)
    img = lerp(img, far_col, far_mask)

    # Mid spires: darker, fewer, less haze.
    mid_top = spire_profile(rng, 16, 230, 800, H * 0.92, H * 0.18, H * 0.38, 30, 80)
    mid_top += noise(rng, 2, (1, W))[0] * 5
    mid_mask = np.clip((yy - mid_top[None, :]) / 2.0, 0, 1)
    face = np.clip(0.5 + 0.5 * streaks(rng, 3, 50) * 0.7, 0, 1)
    rim = np.exp(-np.clip(yy - mid_top[None, :], 0, None) / 12.0)
    mid_col = lerp(MID, ICE_LIT * 0.45, face * 0.3)
    mid_col = lerp(mid_col, ICE_EDGE * 0.55, rim * 0.4)
    mid_col = lerp(mid_col, HAZE * 0.5, np.clip((v - 0.55) / 0.4, 0, 1) * 0.3)
    img = lerp(img, mid_col, mid_mask)

    # Frost haze rising from the chasm floor.
    haze_amt = np.clip((v - 0.55) / 0.35, 0, 1) ** 1.4 * (0.55 + 0.45 * np.clip(fbm(rng, 70) * 0.8 + 0.5, 0, 1))
    img = lerp(img, HAZE, haze_amt * 0.30)

    # Near cliffs: left and right walls, ragged inner edges, dark ice with lit faces.
    def wall_edge(base, amp, seed_sigma):
        n = noise(rng, seed_sigma, (H, 1))[:, 0] * amp + noise(rng, seed_sigma / 5, (H, 1))[:, 0] * amp * 0.3
        return base + n + noise(rng, 6, (H, 1))[:, 0] * amp * 0.07
    left_e = wall_edge(W * 0.24, 70, 120)[:, None] + (1 - v[:, :1]) * 30 - np.clip(v[:, :1] - 0.82, 0, 1) * 600
    right_e = wall_edge(W * 0.76, 70, 120)[:, None] - (1 - v[:, :1]) * 30 + np.clip(v[:, :1] - 0.82, 0, 1) * 600
    left_m = np.clip((left_e - xx) / 2.0, 0, 1)
    right_m = np.clip((xx - right_e) / 2.0, 0, 1)
    near_m = np.maximum(left_m, right_m)
    edge_d = np.minimum(np.abs(xx - left_e), np.abs(xx - right_e))
    blocks = np.floor(yy / 46 + noise(rng, 20) * 0.8) % 3
    strata = np.clip(0.5 + 0.5 * streaks(rng, 60, 3), 0, 1)
    face = np.clip(0.4 + 0.25 * streaks(rng, 3, 22) + 0.25 * fbm(rng, 12) + 0.2 * strata + blocks * 0.06, 0, 1)
    near_col = lerp(NEAR * 0.9, MID * 1.5, face * 0.7)
    near_col = lerp(near_col, ICE_LIT * 0.7, np.exp(-edge_d / 26.0) * 0.35)
    near_col = lerp(near_col, ICE_EDGE * 0.8, np.exp(-edge_d / 5.0) * 0.45)
    img = lerp(img, near_col, near_m)

    # Frost-light windows where the foundry had lava windows (cold glow + bloom).
    glow = np.zeros((H, W), np.float32)
    core = np.zeros((H, W), np.float32)
    windows = [(150, 560, 10, 46), (95, 760, 8, 30), (210, 420, 7, 24), (165, 880, 9, 38), (60, 1080, 7, 26),
               (940, 700, 10, 48), (975, 950, 8, 34), (880, 520, 7, 22), (905, 1120, 8, 30),
               (520, 640, 3, 9), (470, 900, 3, 8), (640, 1010, 3, 8), (590, 760, 2, 6)]
    for (wx, wy, ww, wh) in windows:
        x0, x1, y0, y1 = int(wx - ww / 2), int(wx + ww / 2), int(wy - wh / 2), int(wy + wh / 2)
        core[y0:y1, x0:x1] = 1.0
    glow = blur(core, 10) * 6 + blur(core, 34) * 10
    img = img + QS * np.clip(glow, 0, 1.2)[..., None] * 0.45
    img = lerp(img, FROST_LIGHT, core * 0.85)

    # Icicles hanging from the top corners (the foundry's stalactites).
    x = np.arange(W, dtype=np.float32)
    drip = np.zeros(W, np.float32)
    for _ in range(46):
        side = rng.random() < 0.5
        cx = rng.uniform(0, 300) if side else rng.uniform(724, 1024)
        hw = rng.uniform(5, 18)
        ln = rng.uniform(40, 230) * (1.0 - min(abs(cx - 512) / 512, 1) * 0) * (1.6 - abs(cx - 512) / 512 * 0.0)
        ln *= np.clip(abs(cx - 512) / 300, 0.3, 1.0)
        drip = np.maximum(drip, ln * np.clip(1 - np.abs(x - cx) / hw, 0, 1) ** 1.5)
    shelf = np.clip(60 - np.abs(x - 512) / 512 * 0 , 0, None) * np.clip(np.abs(x - 512) / 300 - 0.55, 0, 1) * 2.2
    ice_top = drip + shelf
    ice_m = np.clip((ice_top[None, :] - yy) / 2.0, 0, 1)
    ice_col = lerp(NEAR, ICE_LIT * 0.5, np.clip(0.4 + 0.4 * streaks(rng, 2, 30), 0, 1) * 0.5)
    ice_col = lerp(ice_col, ICE_EDGE * 0.7, np.exp(-np.clip(ice_top[None, :] - yy, 0, None) / 4.0) * 0.5)
    img = lerp(img, ice_col, ice_m)

    # The dark lower band where the arena meets the plate (rubble silhouette).
    band_top = H * 0.885 + noise(rng, 6, (1, W))[0] * 14 + noise(rng, 30, (1, W))[0] * 22
    band_m = np.clip((yy - band_top[None, :]) / 3.0, 0, 1)
    band_col = lerp(BAND, NEAR, np.clip(0.4 + 0.3 * fbm(rng, 10), 0, 1) * 0.6)
    band_col = lerp(band_col, BAND * 0.7, np.clip((v - 0.93) / 0.07, 0, 1))
    img = lerp(img, band_col, band_m)
    img = lerp(img, HAZE * 0.6, np.exp(-np.abs(yy - band_top[None, :]) / 10.0) * 0.18)

    # Drifting frost motes (few, soft).
    motes = (rng.random((H, W)) > 0.99988).astype(np.float32)
    img = img + ICE_EDGE * np.clip(blur(motes, 1.2) * 5, 0, 0.6)[..., None] * (1 - band_m[..., None])

    # Vignette, grain, painterly finish.
    vig = 1 - 0.35 * np.clip(((u - 0.5) ** 2 * 2.2 + (v - 0.45) ** 2 * 1.3), 0, 1)
    img = img * vig[..., None] + fbm(rng, 1.2, 2)[..., None] * 0.012
    img = np.clip(img, 0, 1)
    pil = Image.fromarray((img * 255 + 0.5).astype(np.uint8), "RGB")
    pil = pil.filter(ImageFilter.ModeFilter(5)).filter(ImageFilter.UnsharpMask(2, 60, 2))
    pil.save(OUT, optimize=True)
    print(OUT, pil.size, os.path.getsize(OUT), "bytes")
    if len(sys.argv) > 1:
        os.makedirs(os.path.dirname(sys.argv[1]), exist_ok=True)
        pil.save(sys.argv[1])


if __name__ == "__main__":
    main()
