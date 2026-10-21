"""Procedural, seamlessly tiling cut-stone paving for the foundry court.

The approved concept lays the court in small hand-cut stones - rough
quadrilaterals in loose courses, thin dark joints, a quiet value spread from
stone to stone and the odd hairline crack - rather than large fitted flags. One
tile covers 12 design units (FlagstoneSpan). Courses run along u, .72-1.2 deep,
their bed joints wandering a little from knot to knot; each course is cut into
stones .8-1.6 long by joints that lean up to ~18 degrees, and about one stone
in eight is split into two across the course. Edges are domain-warped so no
joint is ruler-straight, corners are rounded, and a lit upper-right / shaded
lower-left arris gives the cut. The raster stays near-neutral cool grey: forge
light, ambient and wear remain vertex colour.

Usage: python Tools/ArtReview/GenerateFoundryCutStone.py <out.png> [size] [seed] [gain]
FoundryPavingV10 is the default: 1024, seed 11, gain 1.082.
"""
import sys
import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter

OUT = sys.argv[1]
N = int(sys.argv[2]) if len(sys.argv) > 2 else 1024
SEED = int(sys.argv[3]) if len(sys.argv) > 3 else 11
GAIN = float(sys.argv[4]) if len(sys.argv) > 4 else 1.082  # mean value .52, as FoundryPavingV9
SPAN = 12.0  # design units per tile
PX = N / SPAN
rng = np.random.default_rng(SEED)


def periodic_noise(n, sigma, rng):
    """Tileable band-limited noise in [-1, 1] (FFT-filtered white noise)."""
    white = rng.standard_normal((n, n))
    f = np.fft.fftfreq(n)
    k = np.sqrt(f[:, None] ** 2 + f[None, :] ** 2)
    spectrum = np.fft.fft2(white) * np.exp(-(k * sigma) ** 2 * 2 * np.pi ** 2)
    out = np.real(np.fft.ifft2(spectrum))
    return out / (np.abs(out).max() + 1e-9)


def partition(total, low, high):
    """Random lengths in [low, high] rescaled to fill total exactly."""
    parts = []
    while sum(parts) < total:
        parts.append(rng.uniform(low, high))
    if sum(parts) - total > total * .02 and len(parts) > 1:
        parts.pop()
    parts = np.array(parts)
    return parts * total / parts.sum()


def wandering(x, knots, offsets):
    """A periodic piecewise-linear offset through (knots, offsets), knots ascending in [0, SPAN)."""
    k = np.concatenate([knots - SPAN, knots, knots + SPAN])
    o = np.concatenate([offsets, offsets, offsets])
    return np.interp(x, k, o)


# Coordinates in design units (u = texture x, v up = -row), warped so joints wander a little.
yy, xx = np.mgrid[0:N, 0:N].astype(np.float32) / PX
gx = (xx + periodic_noise(N, N / 70, rng) * .04 + periodic_noise(N, N / 260, rng) * .01) % SPAN
# Shifted by .3 so the tile's edge falls mid-course, not on a bed joint.
gy = (yy + .3 + periodic_noise(N, N / 70, rng) * .04 + periodic_noise(N, N / 260, rng) * .01) % SPAN

# Bed joints: course edges, each wandering by up to .09 from knot to knot (the last is the first, a tile up).
rows = partition(SPAN, .72, 1.2)
edges = np.concatenate([[0], np.cumsum(rows)])
beds = []
for r in range(len(rows)):
    knots = np.sort(np.cumsum(partition(SPAN, 1.6, 2.8))[:-1] + rng.uniform(0, 1))
    beds.append((knots % SPAN, rng.uniform(-.09, .09, len(knots))))
beds.append(beds[0])
bed = np.stack([edges[r] + wandering(gx, *beds[r]) for r in range(len(rows) + 1)])
# Where the first bed wanders up past the tile's foot, the point belongs to the top course a tile down, and the
# other way round at the head.
gy = np.where(gy < bed[0], gy + SPAN, np.where(gy >= bed[-1], gy - SPAN, gy))
# The course holding each pixel (the bed below it; the top bed is the first one a tile up).
row = np.zeros((N, N), int)
for r in range(1, len(rows)):
    row[gy >= bed[r]] = r
below = np.take_along_axis(bed, row[None], 0)[0]
above = np.take_along_axis(bed, row[None] + 1, 0)[0]

owner = np.full((N, N), -1)
left = np.zeros((N, N), np.float32)
right = np.zeros((N, N), np.float32)
low = gy - below
high = above - gy
stones = 0
for r in range(len(rows)):
    m = row == r
    lengths = partition(SPAN, .8, 1.6)
    cuts = np.concatenate([[0], np.cumsum(lengths)[:-1]]) + rng.uniform(0, SPAN)
    lean = rng.uniform(-.32, .32, len(cuts))
    mid = (edges[r] + edges[r + 1]) * .5
    x, y = gx[m], gy[m]
    # Signed distance past each joint (leaning lines through the cuts at mid-course), periodic in u.
    past = np.stack([((x - c - t * (y - mid) + SPAN / 2) % SPAN - SPAN / 2) * np.cos(np.arctan(t))
                     for c, t in zip(cuts, lean)])
    # The stone is the joint the point is past by the least (non-negative), the next joint ends it.
    ahead = np.where(past >= 0, past, np.inf)
    b = np.argmin(ahead, 0)
    nxt = (b + 1) % len(cuts)
    left[m] = ahead[b, np.arange(b.size)]
    right[m] = -past[nxt, np.arange(b.size)]
    right[m] = np.where(right[m] < 0, right[m] + SPAN, right[m])
    ids = stones + b
    # About one stone in eight is split across the course, its cut leaning a little.
    split = rng.random(len(cuts)) < .125
    tilt = rng.uniform(-.12, .12, len(cuts))
    at = mid + rng.uniform(-.08, .08, len(cuts))
    across = y - (at[b] + tilt[b] * (left[m] - right[m]) * .5)
    upper = split[b] & (across >= 0)
    lower = split[b] & (across < 0)
    lo, hi = low[m], high[m]
    lo = np.where(upper, across, lo)
    hi = np.where(lower, -across, hi)
    low[m], high[m] = lo, hi
    owner[m] = np.where(upper, stones + len(cuts) + b, ids)
    stones += 2 * len(cuts)

# Distance to the stone's edge in pixels, its corners rounded (radius varies by stone).
corner_radius = rng.uniform(.08, .2, stones)[owner]
dx = np.minimum(left, right)
dy = np.minimum(low, high)
cx, cy = np.clip(corner_radius - dx, 0, None), np.clip(corner_radius - dy, 0, None)
rounded = corner_radius - np.sqrt(cx ** 2 + cy ** 2)
dist = np.where((cx > 0) & (cy > 0), rounded, np.minimum(dx, dy)) * PX

# Thin joints of slightly irregular width.
wobble = periodic_noise(N, N / 50, rng)
mortar_half = N / 1024 * (1.2 + .45 * wobble)
inside = np.clip((dist - mortar_half) / 1.0, 0, 1)

# Worn arris: a shallow rise over ~4 px, lit from screen upper right.
bevel = N / 1024 * 4.0
height = np.clip((dist - mortar_half) / bevel, 0, 1)
height = np.sqrt(height) * .6 + height * .4
sy, sx = (g[N:2 * N, N:2 * N] for g in np.gradient(gaussian_filter(np.tile(height, (3, 3)), 1.0)))
light = np.array([.62, -.78])  # u right, v up (= -row)
shade = np.clip((sx * light[0] + sy * light[1]) * bevel, -1, 1) * .20

# Per-stone quarry value and a slight cool/warm drift.
stone_value = 1 + rng.normal(0, .045, stones)
stone_hue = np.clip(rng.normal(0, 1, stones), -2, 2)
value = stone_value[owner]
hue = stone_hue[owner]

# Mineral grain at three scales plus broad mottling that crosses stones.
grain = (periodic_noise(N, .9, rng) * .06 + periodic_noise(N, 2.4, rng) * .07 +
         periodic_noise(N, 8, rng) * .06 + periodic_noise(N, 40, rng) * .07)
pits = np.clip(periodic_noise(N, .8, rng) - .72, 0, 1) * -.9
# Dust settles toward each stone's edges; centres are foot-worn a little lighter.
dust = np.clip(1 - dist / (bevel * 3.0), 0, 1) * .05

# Hairline cracks across about one stone in six: a wandering line through it, thinner and paler than a joint.
cracked = rng.random(stones) < .17
angle = rng.uniform(0, np.pi, stones)[owner]
offset = rng.uniform(-.2, .2, stones)[owner]
lx, ly = (left - right) * .5, (low - high) * .5
line = (lx * np.sin(angle) - ly * np.cos(angle) - offset) * PX + periodic_noise(N, N / 120, rng) * 3.0
crack = np.exp(-(line / .7) ** 2) * cracked[owner] * np.clip(dist / (bevel * 2), 0, 1)

surface = value * (1 + grain + pits) * (1 + shade) * (1 - dust) * (1 - crack * .2)

# Sparse chipped corners where joints meet.
chip = np.clip(1 - dist / (bevel * 1.8), 0, 1) * (periodic_noise(N, 2.5, rng) > .5)
surface *= 1 - chip * .12

mortar = .62 * (1 + periodic_noise(N, 2.0, rng) * .1)
lum = mortar + (surface - mortar) * inside

base = np.array([.5, .506, .52]) * GAIN  # faintly cool; vertex light owns hue
rgb = lum[..., None] * base[None, None, :]
rgb[..., 0] *= 1 + hue * .012
rgb[..., 2] *= 1 - hue * .012
img = np.clip(rgb * 255, 0, 255).astype(np.uint8)
Image.fromarray(img, 'RGB').save(OUT)
print(OUT, N, SEED, 'stones', len(np.unique(owner)), 'rows', len(rows), 'mean', img.reshape(-1, 3).mean(0).round(1))
