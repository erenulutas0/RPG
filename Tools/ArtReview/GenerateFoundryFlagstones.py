"""Procedural, seamlessly tiling flagstone paving for the foundry court.

Irregular laid flags (relaxed periodic Voronoi), recessed dusty mortar, a lit
upper-right arris and shaded lower-left arris per flag, quiet per-flag quarry
value, fine periodic mineral grain and sparse corner chips. The raster stays
near-neutral cool grey: forge light, ambient and wear remain vertex colour.

Usage: python Tools/ArtReview/GenerateFoundryFlagstones.py <out.png> [size] [flags] [seed]
"""
import sys
import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter, distance_transform_edt

OUT = sys.argv[1]
N = int(sys.argv[2]) if len(sys.argv) > 2 else 1024
FLAGS = int(sys.argv[3]) if len(sys.argv) > 3 else 96
SEED = int(sys.argv[4]) if len(sys.argv) > 4 else 7
rng = np.random.default_rng(SEED)


def periodic_noise(n, sigma, rng):
    """Tileable band-limited noise in [-1, 1] (FFT-filtered white noise)."""
    white = rng.standard_normal((n, n))
    f = np.fft.fftfreq(n)
    k = np.sqrt(f[:, None] ** 2 + f[None, :] ** 2)
    spectrum = np.fft.fft2(white) * np.exp(-(k * sigma) ** 2 * 2 * np.pi ** 2)
    out = np.real(np.fft.ifft2(spectrum))
    return out / (np.abs(out).max() + 1e-9)


# Relaxed periodic Voronoi seeds. Slight anisotropy (1.15 along u) gives laid
# courses rather than honeycomb cells.
pts = rng.random((FLAGS, 2))
yy, xx = np.mgrid[0:N, 0:N] / N
aniso = np.array([1.0, 1.15])


# Domain warp so laid edges are slightly irregular instead of ruler-straight.
warp_u = periodic_noise(N, N / 90, rng) * 5.0 / N
warp_v = periodic_noise(N, N / 90, rng) * 5.0 / N


def nearest(points, warped=False):
    gx_, gy_ = (xx + warp_u, yy + warp_v) if warped else (xx, yy)
    best = np.full((N, N), np.inf)
    second = np.full((N, N), np.inf)
    owner = np.zeros((N, N), int)
    for i, (px, py) in enumerate(points):
        dx = (gx_ - px + .5) % 1 - .5
        dy = (gy_ - py + .5) % 1 - .5
        d = np.sqrt((dx / aniso[0]) ** 2 + (dy / aniso[1]) ** 2)
        closer = d < best
        second = np.where(closer, best, np.minimum(second, d))
        owner = np.where(closer, i, owner)
        best = np.where(closer, d, best)
    return owner, best, second


for _ in range(3):  # Lloyd relaxation on the torus
    owner, _, _ = nearest(pts)
    for i in range(FLAGS):
        m = owner == i
        if not m.any():
            continue
        ang_x = np.angle(np.exp(2j * np.pi * xx[m]).mean()) / (2 * np.pi) % 1
        ang_y = np.angle(np.exp(2j * np.pi * yy[m]).mean()) / (2 * np.pi) % 1
        pts[i] = (ang_x, ang_y)
pts += rng.normal(0, .006, pts.shape)  # keep it hand-laid, not perfectly regular
pts %= 1
owner, d1, d2 = nearest(pts, warped=True)

# Continuous distance to the flag boundary in pixels: half the gap between the
# nearest and second-nearest seed (anisotropic metric). Unlike a raster distance
# transform this has no stair steps along diagonal joints.
dist = (d2 - d1) * .5 * N

# Irregular joint width: wobble the boundary with low-frequency noise.
wobble = periodic_noise(N, N / 40, rng)
mortar_half = N / 1024 * (2.0 + .8 * wobble)
inside = np.clip((dist - mortar_half) / 1.1, 0, 1)

# Worn arris: a shallow rounded rise over ~9 px, lit from screen upper right.
bevel = N / 1024 * 6.0
height = np.clip((dist - mortar_half) / bevel, 0, 1)
height = np.sqrt(height) * .6 + height * .4
gy, gx = np.gradient(gaussian_filter(np.tile(height, (3, 3)), 1.2)[N:2 * N, N:2 * N])
light = np.array([.62, -.78])  # u right, v up (= -row)
shade = np.clip((gx * light[0] + gy * light[1]) * bevel, -1, 1) * .20

# Per-flag quarry value and a slight cool/warm drift, kept quiet.
flag_value = 1 + rng.normal(0, .04, FLAGS)
flag_hue = rng.normal(0, 1, FLAGS)
value = flag_value[owner]
hue = flag_hue[owner]

# Mineral grain at three scales plus broad worn mottling that crosses flags.
grain = (periodic_noise(N, .9, rng) * .07 + periodic_noise(N, 2.6, rng) * .08 +
         periodic_noise(N, 9, rng) * .07 + periodic_noise(N, 36, rng) * .06)
# Faint quarry veins: thin ridges of a warped noise field.
vein = np.exp(-(periodic_noise(N, 22, rng) * 9) ** 2) * .05
grain -= vein
pits = np.clip(periodic_noise(N, .8, rng) - .7, 0, 1) * -.9
# Dust settles toward each flag's edges; centres are foot-worn a little lighter.
dust = np.clip(1 - dist / (bevel * 3.5), 0, 1) * .05
surface = value * (1 + grain + pits) * (1 + shade) * (1 - dust)

# Sparse chipped corners where joints meet.
corner = np.clip(1 - dist / (bevel * 1.6), 0, 1) * (periodic_noise(N, 2.5, rng) > .45)
surface *= 1 - corner * .12

mortar = .66 * (1 + periodic_noise(N, 2.0, rng) * .1)
lum = mortar + (surface - mortar) * inside

base = np.array([.5, .506, .52]) * float(sys.argv[5] if len(sys.argv) > 5 else 1.0)  # faintly cool; vertex light owns hue
rgb = lum[..., None] * base[None, None, :]
rgb[..., 0] *= 1 + hue * .010
rgb[..., 2] *= 1 - hue * .010
img = np.clip(rgb * 255, 0, 255).astype(np.uint8)
Image.fromarray(img, 'RGB').save(OUT)
print(OUT, N, FLAGS, SEED, 'mean', img.reshape(-1, 3).mean(0).round(1))
