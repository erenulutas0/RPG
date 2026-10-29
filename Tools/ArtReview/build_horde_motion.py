"""The Foundry horde's anticipation and deaths (art round 3, 2026-10-29): a strike windup in both facings and a death of
each kind's own for the Grunt, the Cinder Mite, the Runner, the Tank and the Grunt Captain - EnemyArtSet's optional
_strikeWindup and _deathFront/_deathRear.

    python Tools/ArtReview/build_horde_motion.py [--only grunt,mite,runner,tank,captain] [--preview DIR]
                                                 [--render DIR]

Each enemy keeps the method it was made with (ArtDirection/2026-10-27/foundry-enemies-01/README.md):

- The Grunt: its own painted masters. The front windup is the front sheet's windup cell (the image-right fist raised,
  the arm its strike throws); the rear one is the rejected rear v3 sheet's windup, the one cell drawn with the right
  arm, its alpha haze matted off and its colours fitted to the v2 sheet the shipped rear frames come from. Both are
  registered by their feet against the idle and sampled the way the Unity import sampled the shipped frames (a box mip
  chain on the straight colours, trilinear), which reproduces them to about one level of 255. The death crumbles the
  idle itself: the painted figure is cut into its own rocks along its outlines and lava seams, the seams flare white as
  it cracks, and the rocks fall under gravity onto a heap, tumbling, each outlined, cooling as they settle.
- The Grunt Captain: the same, on the Captain's kitbash (the Grunt's masters scaled 1.15 and turned red-hot, the brass
  laid over each frame): the windups are the Grunt's, dressed; in its death the bell's chain snaps and the bell drops to
  the floor and tips over, and the helm is knocked off and tumbles onto the heap.
- The Cinder Mite: its own painted masters. The windup coils the idle back and down over its feet, the flame flaring
  and the seams heating; the death swells white-hot and bursts, its rocks flung out and falling round the spot, embers
  rising off them.
- The Runner and the Tank: rendered by BuildBlenderFoundryEnemies.py --motion (TestResults/horde-motion-render), packed
  as their walk was (build_foundry_enemies.process_render). The Runner coils, claws drawn back, before its pounce, and
  in death pitches forward onto its face, its heart going out; the Tank rears with both fists up before its slam, and
  in death topples forward like a felled slab, its grille dimming.

Windups share their enemy's walking canvas and pivot exactly, so the body never jumps. Death drawings are each cropped
to what they show (whole ASTC blocks) with the pivot on the same floor root. Everything is regenerated on every run and
is byte for byte the same: existing GUIDs are kept, new files get fresh uuid4 GUIDs; the five sets keep every line they
had and gain the optional parts at their end. ArtDirection/2026-10-29/horde-motion-01/sprites.json lists every texture.
"""
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage as ndi
from scipy.spatial import ConvexHull

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_foundry_enemies as fe  # noqa: E402

ROOT = fe.ROOT
ARGS = sys.argv[1:]


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


ONLY = arg('--only', 'grunt,mite,runner,tank,captain').split(',')
PREVIEW = arg('--preview')
RENDER = Path(arg('--render', str(ROOT / 'TestResults/horde-motion-render')))
DOCS = ROOT / 'ArtDirection/2026-10-29/horde-motion-01'
MASTERS = ROOT / 'ArtDirection/2026-09-17'
PPU = fe.PPU
INK = np.array(fe.OUTLINE) / 255.0


# ---------------------------------------------------------------------------------------------------------------------
# Colour and compositing (premultiplied linear RGBA in float, as build_foundry_enemies works)
# ---------------------------------------------------------------------------------------------------------------------
def straight_to_premult(px):
    """Straight sRGB RGBA in [0, 1] to premultiplied linear."""
    a = px[..., 3:4]
    return np.concatenate([fe.to_linear(px[..., :3]) * a, a], axis=-1)


def premult_to_straight(px):
    a = px[..., 3:4]
    s = np.where(a > 1e-6, px[..., :3] / np.maximum(a, 1e-6), 0)
    return np.concatenate([fe.to_srgb(s), a], axis=-1)


def over(dst, src):
    return src + dst * (1 - src[..., 3:4])


def to_uint8(straight):
    out = (np.clip(straight, 0, 1) * 255 + 0.5).astype(np.uint8)
    out[out[..., 3] == 0] = 0
    return out


def transform(px, data, size, resample=Image.BICUBIC):
    """An affine resample of a premultiplied float image, channel by channel (PIL's inverse mapping `data`)."""
    layer = np.stack([np.asarray(Image.fromarray(px[..., k].astype(np.float32), 'F').transform(
        size, Image.AFFINE, data, resample=resample)) for k in range(px.shape[2])], axis=-1)
    layer = np.clip(layer, 0, None)
    layer[..., 3] = np.clip(layer[..., 3], 0, 1)
    layer[..., :3] = np.minimum(layer[..., :3], layer[..., 3:4])
    return layer


# ---------------------------------------------------------------------------------------------------------------------
# The Unity import's sampling, reproduced: Graphics.Blit from a texture with a box-filtered mip chain (Texture2D.Apply
# on straight sRGB values) at the shipped frames' scale, trilinear. Measured against the shipped Grunt idles it agrees
# to about one level of 255 on average.
# ---------------------------------------------------------------------------------------------------------------------
def mip_chain(img, levels):
    chain = [img]
    for _ in range(levels):
        a = chain[-1]
        h, w = a.shape[0] // 2 * 2, a.shape[1] // 2 * 2
        chain.append(a[:h, :w].reshape(h // 2, 2, w // 2, 2, a.shape[2]).mean(axis=(1, 3)))
    return chain


def bilinear(img, xs, ys):
    """Bilinear at texel-space coordinates (x right, y down, texel centres at +0.5), clamped to the edge."""
    h, w = img.shape[:2]
    x = np.clip(xs - 0.5, 0, w - 1)
    y = np.clip(ys - 0.5, 0, h - 1)
    x0 = np.floor(x).astype(int)
    y0 = np.floor(y).astype(int)
    x1 = np.minimum(x0 + 1, w - 1)
    y1 = np.minimum(y0 + 1, h - 1)
    fx = (x - x0)[..., None]
    fy = (y - y0)[..., None]
    return (img[y0, x0] * (1 - fx) + img[y0, x1] * fx) * (1 - fy) + (img[y1, x0] * (1 - fx) + img[y1, x1] * fx) * fy


def blit(straight, sx, sy, scale):
    """Samples a straight sRGB image at the source coordinates of each output texel's centre (sx along x, sy down
    the rows), minified by `scale` source texels a texel, as the GPU's trilinear filter does."""
    lod = math.log2(scale)
    level = int(math.floor(lod))
    t = lod - level
    chain = mip_chain(straight, level + 1)
    X, Y = np.meshgrid(sx, sy)
    lo = bilinear(chain[level], X / 2 ** level, Y / 2 ** level)
    hi = bilinear(chain[level + 1], X / 2 ** (level + 1), Y / 2 ** (level + 1))
    return lo * (1 - t) + hi * t


def blit_registered(straight, root, size, pivot, scale):
    """An output canvas `size` (w, h) whose floor root, the texel corner `pivot` (from the bottom left), samples the
    source's floor root `root` (BuildGruntAssets' mapping, its one-row offset included), `scale` source texels a
    texel. Returns straight sRGB RGBA in [0, 1], top row first."""
    w, h = size
    i = np.arange(w) + 0.5
    j = np.arange(h) + 0.5                      # from the bottom
    sx = root[0] + (i - pivot[0]) * scale
    sy = root[1] + 1 - (j - pivot[1]) * scale
    return blit(straight, sx, sy, scale)[::-1]


# ---------------------------------------------------------------------------------------------------------------------
# Painted masters: isolating one figure from its sheet
# ---------------------------------------------------------------------------------------------------------------------
def sheet(path):
    return np.asarray(Image.open(path).convert('RGBA')).astype(np.float64) / 255.0


def isolate(img, box, threshold=0.5):
    """The figure of the sheet's largest connected shape inside box (x0, y0, x1, y1), everything else cleared - the
    painted sheets do not keep to their cells, so a neighbour's fist or foot never comes along."""
    x0, y0, x1, y1 = box
    a = img[..., 3] >= threshold
    inside = np.zeros_like(a)
    inside[y0:y1, x0:x1] = a[y0:y1, x0:x1]
    labels, n = ndi.label(inside)
    sizes = ndi.sum(inside, labels, range(1, n + 1))
    keep = labels == (int(np.argmax(sizes)) + 1)
    keep = ndi.binary_dilation(keep, iterations=3)
    out = img.copy()
    out[~keep] = 0
    return out


def feet(img, band=60):
    """The figure's lowest opaque row and the mean column of its lowest band (the feet), in sheet texels."""
    ys, xs = np.nonzero(img[..., 3] > 0.5)
    low = ys.max()
    return low, xs[ys >= low - band].mean()


def register_like(cell, idle, idle_root):
    """A floor root for a pose cell: its feet put where the idle's are against the idle's own root."""
    low, mid = feet(cell)
    idle_low, idle_mid = feet(idle)
    return (mid - (idle_mid - idle_root[0]), low - (idle_low - idle_root[1]))


# ---------------------------------------------------------------------------------------------------------------------
# The Grunt
# ---------------------------------------------------------------------------------------------------------------------
GRUNT_REG = json.loads((MASTERS / 'grunt-polish-01/registration.json').read_text())
GRUNT_CANVAS = (180, 180)
GRUNT_PIVOT = (90, 18)                       # .5 and .1 of the canvas, as the shipped metas say
GRUNT_SCALE = 640 / 180                      # source texels a sprite texel (sourceScale 1)


def grunt_registered(name):
    """A shipped Grunt cell as the importer isolated it (its rectangle of the sheet) and its floor root."""
    f = next(f for f in GRUNT_REG['frames'] if f['name'] == name)
    img = sheet(MASTERS / 'grunt-polish-01' / f['file'])
    x0, y0, x1, y1 = f['bounds']
    out = np.zeros_like(img)
    out[y0:y1, x0:x1] = img[y0:y1, x0:x1]
    return out, tuple(f['root']), f['sourceScale']


def rear_v3_windup():
    """The rear v3 sheet's windup - the one cell drawn raising the near fist its strike throws - matted off its haze
    (the sheet's figures are opaque to 247 of 255 and the haze round them is fainter) and its colours fitted to the v2
    sheet by least squares on the cells v3 left unchanged, so it sits beside the shipped rear frames."""
    v2 = sheet(MASTERS / 'grunt-polish-01/rear-sheet-v2.png') * 255
    v3 = sheet(MASTERS / 'grunt-polish-01/rear-sheet-v3.png') * 255
    xs, ys = [], []
    for x0, y0, x1, y1 in ((0, 0, 512, 500), (512, 0, 1024, 512), (0, 512, 512, 1024)):
        m = (v2[y0:y1, x0:x1, 3] >= 253) & (v3[y0:y1, x0:x1, 3] >= 250)
        xs.append(v3[y0:y1, x0:x1, :3][m])
        ys.append(v2[y0:y1, x0:x1, :3][m])
    X = np.concatenate(xs)
    A = np.concatenate([X, np.ones((len(X), 1))], axis=1)
    fit, *_ = np.linalg.lstsq(A, np.concatenate(ys), rcond=None)
    alpha = np.clip((v3[..., 3] - 247) / 5, 0, 1)
    alpha = np.maximum(alpha, ndi.binary_fill_holes(alpha > 0.5)) * (254 / 255)
    rgb = np.clip(np.concatenate([v3[..., :3], np.ones(v3.shape[:2] + (1,))], axis=-1) @ fit, 0, 255) / 255
    return isolate(np.concatenate([rgb, alpha[..., None]], axis=-1), (540, 515, 975, 995))


def grunt_windups():
    """The two windup bodies on the Grunt's walking canvas, straight sRGB RGBA uint8."""
    idle, root, _ = grunt_registered('front-idle')
    front = isolate(sheet(MASTERS / 'grunt-motion-01/front-sheet-v2.png'), (536, 512, 990, 1010))
    front_root = register_like(front, isolate(idle, (40, 40, 520, 500)), root)
    rear_idle, rear_root, _ = grunt_registered('rear-idle')
    rear = rear_v3_windup()
    rear_at = register_like(rear, isolate(rear_idle, (50, 20, 490, 490)), rear_root)
    return {
        'front-windup': to_uint8(blit_registered(front, front_root, GRUNT_CANVAS, GRUNT_PIVOT, GRUNT_SCALE)),
        'rear-windup': to_uint8(blit_registered(rear, rear_at, GRUNT_CANVAS, GRUNT_PIVOT, GRUNT_SCALE)),
    }


# ---------------------------------------------------------------------------------------------------------------------
# Rocks: a painted figure cut into its own rocks, and their fall
# ---------------------------------------------------------------------------------------------------------------------
class Piece:
    """One rock: its texels (straight sRGB RGBA) in a box of the work canvas, its centre and lowest texel relative to
    the floor root (x right, y up, work texels), its area and how much of it is lava."""

    def __init__(self, px, box, root):
        self.px = px
        self.x0, self.y0 = box
        ys, xs = np.nonzero(px[..., 3] > 0.5)
        self.cx = self.x0 + xs.mean() - root[0]
        self.cy = root[1] - (self.y0 + ys.mean())
        self.bottom = root[1] - (self.y0 + ys.max() + 1)
        self.top = root[1] - (self.y0 + ys.min())
        self.left = self.x0 + xs.min() - root[0]
        self.right = self.x0 + xs.max() + 1 - root[0]
        self.area = len(ys)
        self.pivot = (xs.mean(), ys.mean())     # its own centre in its box
        # How round it is (1) or how thin a sliver (toward 0): a sliver seen edge on would show its unpainted side.
        if len(ys) > 2:
            ev = np.linalg.eigvalsh(np.cov(np.stack([xs, ys]).astype(np.float64)))
            self.round = math.sqrt(max(ev[0], 1e-9) / max(ev[1], 1e-9))
            # A crescent (a rock half hidden behind another) tumbles less, so its unpainted side stays turned away.
            solidity = len(ys) / max(1.0, ConvexHull(np.stack([xs, ys], 1)).volume)
            self.round *= min(1.0, max(0.25, (solidity - 0.72) / 0.22))
        else:
            self.round = 1.0


def lava_of(straight):
    """How molten each texel is, from its warmth (build_foundry_enemies.red_hot's measure)."""
    warm = straight[..., 0] - straight[..., 2]
    return np.clip((warm - 0.18) / 0.22, 0, 1) * (straight[..., 0] > 0.4)


def cut_rocks(img, root, min_area, erode=2, solid=0.86, dark=0.09, lines=0):
    """The figure's rocks: its texels split along their dark outlines and lava seams (each boundary texel goes to the
    nearest rock), rocks smaller than min_area merged into their neighbours, and so are rocks less solid than `solid`
    (their area over their convex hull's) and any stray bit cut off from its rock. On stone as dark as its outlines
    (the Mite's), `lines` finds the outlines as thin valleys of dark narrower than that many texels instead."""
    a = img[..., 3]
    rgb = img[..., :3]
    lum = rgb @ np.array([.2126, .7152, .0722])
    figure = a > 0.02
    boundary = (lum < dark) | (lava_of(img) > 0.3)
    if lines:
        boundary |= ndi.black_tophat(lum, size=lines) > 0.05
    interior = ndi.binary_erosion((a > 0.5) & ~boundary, iterations=erode)
    labels, n = ndi.label(interior)
    sizes = ndi.sum(np.ones_like(labels), labels, index=np.arange(1, n + 1))
    keep = np.zeros(n + 1, bool)
    keep[1:] = sizes >= min_area
    labels = np.where(keep[labels], labels, 0)
    _, (iy, ix) = ndi.distance_transform_edt(labels == 0, return_indices=True)
    owner = labels[iy, ix] * figure
    # A rock's texels cut off from it (round another rock's corner) go to the rock they touch most.
    for label in np.unique(owner[owner > 0]):
        parts, count = ndi.label(owner == label)
        if count < 2:
            continue
        sizes = ndi.sum(np.ones_like(parts), parts, index=np.arange(1, count + 1))
        for k in range(1, count + 1):
            if k == int(np.argmax(sizes)) + 1:
                continue
            bit = parts == k
            ring = ndi.binary_dilation(bit, iterations=2) & ~bit & (owner > 0) & (owner != label)
            others, counts = np.unique(owner[ring], return_counts=True)
            owner[bit] = others[np.argmax(counts)] if len(others) else 0
    # A rock half hidden behind another was painted only where it shows - a crescent - and would tumble as one: it
    # falls with the neighbour it shares the most edge with (the boulder behind the head goes with the head). Not on a
    # figure whose rocks all wrap round lava (solid 0), where it would merge everything into one.
    # Only small rocks merge, and never into a rock that would grow past an eighth of the figure, so a figure of many
    # concave rocks is never merged into one.
    total = (owner > 0).sum()
    settled = set()
    while solid > 0:
        worst = None
        for label in np.unique(owner[owner > 0]):
            if label in settled:
                continue
            mask = owner == label
            ys, xs = np.nonzero(mask)
            if len(ys) < 3 or len(ys) > total * 0.05:
                continue
            solidity = len(ys) / max(1.0, ConvexHull(np.stack([xs, ys], 1)).volume)
            if solidity < solid and (worst is None or solidity < worst[0]):
                worst = (solidity, label, mask)
        if worst is None:
            break
        _, label, mask = worst
        ring = ndi.binary_dilation(mask, iterations=3) & ~mask & (owner > 0)
        neighbours, counts = np.unique(owner[ring], return_counts=True)
        target = neighbours[np.argmax(counts)] if len(neighbours) else None
        if target is None or (owner == target).sum() + mask.sum() > total / 8:
            settled.add(label)
            continue
        owner[mask] = target
    pieces = []
    for label in np.unique(owner[owner > 0]):
        mask = owner == label
        ys, xs = np.nonzero(mask)
        y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
        px = img[y0:y1, x0:x1].copy()
        px[~mask[y0:y1, x0:x1]] = 0
        pieces.append(Piece(px, (x0, y0), root))
    return pieces


def heat(straight, amount):
    """A rock's seams at a heat: above 1 they flare toward white-yellow (the crack), below 1 they cool toward a dull
    ember red and then the stone's own dark."""
    lava = lava_of(straight)[..., None]
    rgb = straight[..., :3]
    if amount >= 1:
        hot = np.array([1.0, 0.93, 0.62])
        rgb = rgb + (hot - rgb) * lava * min(1.0, amount - 1)
    else:
        ember = np.array([0.42, 0.12, 0.08])
        cold = 1 - amount
        rgb = rgb + (ember - rgb) * lava * min(1.0, cold * 1.3)
        rgb = rgb * (1 - lava * max(0.0, cold - 0.5) * 0.9)
    return np.concatenate([rgb, straight[..., 3:]], axis=-1)


def crack_lines(piece_px, width=2):
    """The rim of a rock, inside it, where it parts from the others: the crack that glows as it breaks."""
    a = piece_px[..., 3] > 0.5
    rim = a & ~ndi.binary_erosion(a, iterations=width)
    return rim


class Fall:
    """Where each rock is at a time: it drops from where it stood under gravity and lands on the heap, drifting out,
    tumbling and bouncing once, each rock a little after the last. Fixed by a seed, so a rerun is the same."""

    def __init__(self, pieces, seed, unit, gravity=10.0, spread=1.22, nestle=0.35, depth=0.05, delay=0.03, tumble=38,
                 out=None, lift=None, topple=0.0):
        rng = np.random.default_rng(seed)
        self.pieces = pieces
        self.unit = unit
        self.g = gravity * unit
        n = len(pieces)
        self.angle = np.zeros(n)
        self.x1 = np.zeros(n)
        self.y1 = np.zeros(n)
        self.t0 = np.zeros(n)
        self.vx = np.zeros(n)
        self.vy = np.zeros(n)
        heights = {}
        bins = 8
        order = sorted(range(n), key=lambda i: pieces[i].bottom)
        for i in order:
            p = pieces[i]
            k = min(1.0, (p.area / max(q.area for q in pieces)) ** 0.5)
            # Outward rocks roll outward; the higher a rock stood, the further it topples from the body's axis.
            side = 1 if p.cx >= 0 else -1
            self.angle[i] = ((side if rng.uniform() < 0.7 else -side) * rng.uniform(0.35, 1.0) * tumble * (1.15 - 0.7 * k)
                             * min(1.0, p.round / 0.55))
            # Rocks near the axis topple less, so the heap keeps its middle.
            reach = min(1.0, abs(p.cx) / (0.22 * unit))
            self.x1[i] = p.cx * spread + side * topple * max(0.0, p.cy) * reach + rng.uniform(-0.05, 0.05) * unit
            if out is not None:
                self.x1[i] = p.cx + out[i][0] * unit
            # The heap: each rock comes to rest on whatever already lies under it, sunk a little into the gaps.
            width = p.right - p.left
            lo = int(math.floor((self.x1[i] - width / 2) / bins))
            hi = int(math.ceil((self.x1[i] + width / 2) / bins))
            under = max([heights.get(c, 0.0) for c in range(lo, hi)] + [0.0])
            floor = rng.uniform(-depth, depth) * unit
            rest = max(floor, under - nestle * (p.top - p.bottom)) if under > 0 else floor
            rest = min(rest, p.bottom)
            self.y1[i] = rest - p.bottom + p.cy - (p.cy - p.bottom) * (1 - math.cos(math.radians(self.angle[i]))) * 0.4
            for c in range(lo, hi):
                heights[c] = max(heights.get(c, 0.0), rest + (p.top - p.bottom) * 0.75)
            self.t0[i] = rng.uniform(0, delay)
            if lift is not None:
                self.vx[i], self.vy[i] = lift[i][0] * unit, lift[i][1] * unit

    def at(self, t):
        """For each rock: (x offset, y offset, angle) at time t, in work texels and degrees, and whether it has landed."""
        states = []
        for i, p in enumerate(self.pieces):
            s = max(0.0, t - self.t0[i])
            drop = p.cy - self.y1[i]
            # Up then down: vy is its launch upward (a burst), the heap's rest below where it started.
            a = self.g
            # Time to land: cy + vy s - g s^2 / 2 = y1.
            disc = self.vy[i] ** 2 + 2 * a * drop
            land = (self.vy[i] + math.sqrt(max(0.0, disc))) / a if a > 0 else 1e9
            if s < land:
                y = p.cy + self.vy[i] * s - 0.5 * a * s * s
                f = s / land
                x = p.cx + (self.x1[i] - p.cx) * (1 - (1 - f) ** 2)
                ang = self.angle[i] * f
                landed = False
            else:
                since = s - land
                bounce = max(0.0, 1 - since / 0.1)
                y = self.y1[i] + 0.018 * self.unit * math.sin(math.pi * min(1.0, since / 0.1)) * bounce
                x = self.x1[i]
                ang = self.angle[i]
                landed = True
            states.append((x - p.cx, y - p.cy, ang, landed))
        return states


def draw_rocks(pieces, states, size, root, order, recolor, outline=2.4, exploded=0.0, glow=None, base=None):
    """The rocks at their places on a work canvas (premultiplied linear): each turned about its own centre, outlined
    in the painted ink, drawn in `order` (back to front), over `base` if given."""
    w, h = size
    canvas = np.zeros((h, w, 4)) if base is None else base
    for i in order:
        p = pieces[i]
        dx, dy, ang, _ = states[i]
        px = recolor(i, p.px)
        if glow is not None and glow[i] > 0:
            rim = crack_lines(p.px)[..., None]
            hot = np.array([1.0, 0.9, 0.55])
            px = np.concatenate([px[..., :3] + (hot - px[..., :3]) * rim * glow[i], px[..., 3:]], axis=-1)
        pm = straight_to_premult(px)
        # Room to turn and to carry an outline.
        pad = int(math.hypot(*pm.shape[:2]) / 2) + int(outline) + 4
        pm = np.pad(pm, ((pad, pad), (pad, pad), (0, 0)))
        a = pm[..., 3]
        ring = ndi.grey_dilation(a, footprint=disk(outline)) if outline > 0 else a
        ink = np.concatenate([np.broadcast_to(fe.to_linear(INK), a.shape + (3,)) * ring[..., None], ring[..., None]], -1)
        pm = over(ink, pm)
        cx, cy = p.pivot[0] + pad, p.pivot[1] + pad
        # Where its centre lands on the canvas.
        ex = (p.cx * exploded, p.cy * exploded)
        tx = root[0] + p.cx + dx + ex[0]
        ty = root[1] - (p.cy + dy + ex[1])
        r = math.radians(ang)
        c, s = math.cos(r), math.sin(r)
        # Inverse map: canvas (u, v) -> piece (x, y), turned clockwise by ang on screen.
        data = (c, s, cx - c * tx - s * ty, -s, c, cy + s * tx - c * ty)
        canvas = over(canvas, transform(pm, data, (w, h)))
    return canvas


def disk(r):
    k = int(math.ceil(r))
    y, x = np.mgrid[-k:k + 1, -k:k + 1]
    return x * x + y * y <= r * r + 0.25


def chips(seed, count, origin, unit, size):
    """Small flakes of stone knocked off where the heap lands, as (x, y, vx, vy, radius) in work texels from the root."""
    rng = np.random.default_rng(seed)
    out = []
    for _ in range(count):
        out.append((origin[0] + rng.uniform(-0.35, 0.35) * unit, origin[1] + rng.uniform(0.0, 0.08) * unit,
                    rng.uniform(-0.9, 0.9) * unit, rng.uniform(0.6, 1.4) * unit, rng.uniform(0.6, 1.0) * size))
    return out


def draw_chips(canvas, flakes, t, start, root, unit, stone=(0.18, 0.17, 0.24), life=0.28, gravity=9.0):
    """The flakes, flying from `start` for `life` seconds and fading, drawn as small dark outlined stones."""
    s = t - start
    if s <= 0 or s >= life:
        return canvas
    img = Image.new('RGBA', (canvas.shape[1], canvas.shape[0]), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    fade = 1 - s / life
    for x, y, vx, vy, r in flakes:
        px = root[0] + x + vx * s
        py = root[1] - (y + vy * s - 0.5 * gravity * unit * s * s)
        if py > root[1] + 0.06 * unit:
            continue
        ink = tuple(int(c * 255) for c in INK) + (int(255 * fade),)
        body = tuple(int(c * 255) for c in stone) + (int(255 * fade),)
        d.ellipse((px - r - 2, py - r - 2, px + r + 2, py + r + 2), fill=ink)
        d.ellipse((px - r, py - r * 0.8, px + r, py + r * 0.8), fill=body)
    layer = straight_to_premult(np.asarray(img).astype(np.float64) / 255)
    return over(canvas, layer)


def crop_to_content(straight_u8, pivot, margin=2):
    """The texels a drawing shows plus a margin, each side a whole ASTC block, and the pivot in that crop (from its
    bottom left) - the floor root stays the same point in the world."""
    h, w = straight_u8.shape[:2]
    ys, xs = np.nonzero(straight_u8[..., 3] > 0)
    # The floor root stays inside the drawing (a body fallen away from the camera lies wholly above it).
    root_x, root_row = int(pivot[0]), int(h - pivot[1])
    x0, x1 = max(0, min(xs.min(), root_x - 1) - margin), min(w, max(xs.max() + 1, root_x + 1) + margin)
    y0, y1 = max(0, min(ys.min(), root_row - 1) - margin), min(h, max(ys.max() + 1, root_row + 1) + margin)
    cw = -(-(x1 - x0) // 4) * 4
    ch = -(-(y1 - y0) // 4) * 4
    out = np.zeros((ch, cw, 4), np.uint8)
    out[ch - (y1 - y0):, :x1 - x0] = straight_u8[y0:y1, x0:x1]
    # The crop's bottom edge is the canvas's row y1, h - y1 texels above the canvas's own bottom.
    return out, (pivot[0] - x0, pivot[1] - (h - y1))


# The Grunt's crumble: a step of 0.06 s, drawings at these times, the last held while it fades (CombatEffectsView fades
# a death's last 0.16 s).
DEATH_STEP = 0.06
GRUNT_DEATH_TIMES = [0.0, 0.06, 0.12, 0.18, 0.25, 0.36]
GRUNT_DEATH_STEPS = [0, 1, 2, 3, 4, 5, 5, 5, 5, 5]       # 0.6 s
GRUNT_WORK = (900, 760)                                   # the heap's canvas in source texels
GRUNT_WORK_ROOT = (450, 620)


def grunt_death(facing):
    """The Grunt crumbling into its own rocks, in a facing: drawings on a free canvas with the floor root's texel."""
    name = facing + '-idle'
    img, root, _ = grunt_registered(name)
    img = isolate(img, (40, 20, 520, 500))
    unit = PPU * GRUNT_SCALE
    rear = facing == 'rear'
    pieces = cut_rocks(img, root, min_area=300, erode=1 if rear else 2, dark=0.12 if rear else 0.09,
                       solid=0.9 if rear else 0.86)
    fall = Fall(pieces, seed=11 if facing == 'front' else 13, unit=unit, gravity=15, spread=1.15, nestle=0.45,
                tumble=70, topple=0.2)
    out_size = (int(GRUNT_WORK[0] / GRUNT_SCALE // 4 * 4), int(GRUNT_WORK[1] / GRUNT_SCALE // 4 * 4))
    out_pivot = (out_size[0] // 2, 34)
    order = sorted(range(len(pieces)), key=lambda i: -fall.y1[i])
    flakes = chips(17 if facing == 'front' else 19, 9, (0, 0.05 * unit), unit, 7)
    drawings = []
    for k, t in enumerate(GRUNT_DEATH_TIMES):
        states = fall.at(t)
        # The crack flares the seams white and splits the rocks along their rims; the rocks cool as they settle.
        flare = 1.7 if t == 0 else max(0.25, 1.35 - t * 2.6)
        glow = [0.4 if t == 0 else max(0.0, 0.3 - t * 2.5)] * len(pieces)
        canvas = draw_rocks(pieces, states, GRUNT_WORK, GRUNT_WORK_ROOT, order,
                            lambda i, px: heat(px, flare), exploded=0.012 if t == 0 else 0.0, glow=glow)
        canvas = draw_chips(canvas, flakes, t, 0.22, GRUNT_WORK_ROOT, unit)
        straight = premult_to_straight(canvas)
        sampled = blit_registered(straight, (GRUNT_WORK_ROOT[0], GRUNT_WORK_ROOT[1] - 1), out_size, out_pivot,
                                  GRUNT_SCALE)
        drawings.append(crop_to_content(to_uint8(sampled), out_pivot))
    return drawings


# ---------------------------------------------------------------------------------------------------------------------
# The Grunt Captain: the Grunt's poses kitbashed as build_foundry_enemies.captain_frame does
# ---------------------------------------------------------------------------------------------------------------------
CAPTAIN_SHIPPED = (168, 176)
CAPTAIN_SHIPPED_PIVOT = (84, 18)
# Where the shipped canvas sits in captain_frame's 208-texel canvas (build_foundry_enemies.fit).
CAPTAIN_CROP = (fe.CAPTAIN_PIVOT[0] - CAPTAIN_SHIPPED_PIVOT[0],
                fe.CAPTAIN_CANVAS - fe.CAPTAIN_PIVOT[1] - (CAPTAIN_SHIPPED[1] - CAPTAIN_SHIPPED_PIVOT[1]))


def captain_cell(straight, root, size=(fe.WORK_SIZE, fe.WORK_SIZE), at=fe.WORK_ROOT, k=1.0):
    """A Grunt figure (straight, on its sheet, floor root `root`) on a Captain work canvas as fe.registered places a
    registered cell: premultiplied, scaled by its sourceScale and the Captain's size, its root on `at`."""
    ys, xs = np.nonzero(straight[..., 3] > 0)
    x0, y0, x1, y1 = xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
    cell = straight_to_premult(straight[y0:y1, x0:x1])
    s = k * fe.WORK_PER_CANVAS
    dims = (round((x1 - x0) * s), round((y1 - y0) * s))
    cell = fe.resize_premultiplied(cell, dims)
    left = round((x0 - root[0]) * s + at[0])
    top = round((y0 - root[1]) * s + at[1])
    canvas = np.zeros((size[1], size[0], 4))
    cx0, cy0 = max(0, left), max(0, top)
    cx1, cy1 = min(size[0], left + dims[0]), min(size[1], top + dims[1])
    canvas[cy0:cy1, cx0:cx1] = cell[cy0 - top:cy1 - top, cx0 - left:cx1 - left]
    return canvas


def grunt_windup_sources():
    """The Grunt's two windup figures on their sheets and their floor roots (as grunt_windups registers them)."""
    idle, root, _ = grunt_registered('front-idle')
    front = isolate(sheet(MASTERS / 'grunt-motion-01/front-sheet-v2.png'), (536, 512, 990, 1010))
    front_root = register_like(front, isolate(idle, (40, 40, 520, 500)), root)
    rear_idle, rear_root, _ = grunt_registered('rear-idle')
    rear = rear_v3_windup()
    rear_at = register_like(rear, isolate(rear_idle, (50, 20, 490, 490)), rear_root)
    # In registration.json's terms, as fe.registered takes them.
    return {'front-windup': (front, front_root), 'rear-windup': (rear, rear_at)}


# Where the Captain's brass sits on its windups, in work texels on captain_frame's canvas, placed by eye as
# build_foundry_enemies.CAPTAIN_DRESS places it on the walk (see that table for the fields).
CAPTAIN_WINDUP_DRESS = {
    'front-windup': dict(head=(318, 450, 330), brim=375, bell=(398, 486), chain=((334, 450), (440, 440)), turn=3),
    'rear-windup': dict(head=(272, 445, 242), brim=289, back=((294, 327), (428, 331)), turn=-5),
}


def captain_dressed(work, dress, front, chain_on=True):
    """build_foundry_enemies.captain_frame's brass laid over a recoloured work canvas."""
    x0, x1, _ = dress['head']
    helm = fe.brass_prop('helm-front' if front else 'helm-rear')
    width = (x1 - x0) * (1.1 if front else 0.86)
    scale = width / (2 * 0.124 * PPU * fe.WORK)
    px = work
    if chain_on:
        if front:
            px = fe.chain(px, [(dress['chain'][0], dress['bell'], 10), (dress['bell'], dress['chain'][1], 10)])
            px = fe.place_prop(px, fe.brass_prop('bell-front'), dress['bell'], fe.CAPTAIN_SCALE)
        else:
            px = fe.chain(px, [(dress['back'][0], dress['back'][1], 22)], links=16)
    px = fe.place_prop(px, helm, ((x0 + x1) / 2, dress['brim']), scale, dress.get('turn', 0.0))
    return px


def captain_windups():
    out = {}
    for name, (straight, root) in grunt_windup_sources().items():
        work = fe.red_hot(captain_cell(straight, root))
        px = captain_dressed(work, CAPTAIN_WINDUP_DRESS[name], name.startswith('front'))
        small = fe.downsample(px)
        full = fe.finish(small[..., :3], small[..., 3:4])
        x0, y0 = CAPTAIN_CROP
        out[name] = full[y0:y0 + CAPTAIN_SHIPPED[1], x0:x0 + CAPTAIN_SHIPPED[0]]
    return out


CAPTAIN_WORK = (1400, 1000)                  # the heap's canvas in Captain work texels (fe.WORK a texel)
CAPTAIN_WORK_ROOT = (700, 860)
CAPTAIN_DEATH_TIMES = [0.0, 0.06, 0.12, 0.18, 0.25, 0.33, 0.45]
CAPTAIN_DEATH_STEPS = [0, 1, 2, 3, 4, 5, 6, 6, 6, 6, 6]   # 0.66 s


def captain_recolor(violet, amount, flare):
    """A rock of the Grunt's violet master as the Captain wears it (fe.red_hot at a heat), its seams - found on the
    violet, where they are told from the stone by their warmth - flaring white-hot by `flare`."""
    px = premult_to_straight(fe.red_hot(straight_to_premult(violet), amount))
    if flare > 0:
        lava = lava_of(violet)[..., None]
        hot = np.array([1.0, 0.92, 0.6])
        px = np.concatenate([px[..., :3] + (hot - px[..., :3]) * lava * flare, px[..., 3:]], axis=-1)
    return px


def top_at(canvas, x, below):
    """The highest opaque row of a premultiplied canvas round column x, no higher than `below` rows search from."""
    col = canvas[:, max(0, int(x) - 6):int(x) + 6, 3].max(axis=1)
    rows = np.nonzero(col > 0.5)[0]
    return rows.min() if len(rows) else below


def captain_death(facing):
    """The Captain crumbling as the Grunt does, in ember red: as it cracks the bell's chain snaps and the bell drops
    to the floor and tips over, and the helm is knocked off backward and tumbles onto the heap."""
    name = facing + '-idle'
    front = facing == 'front'
    img, root, _ = grunt_registered(name)
    img = isolate(img, (40, 20, 520, 500))
    work = captain_cell(img, root, CAPTAIN_WORK, CAPTAIN_WORK_ROOT)
    violet = premult_to_straight(work)
    unit = PPU * fe.WORK
    pieces = cut_rocks(violet, CAPTAIN_WORK_ROOT, min_area=500, erode=1 if not front else 2,
                       dark=0.12 if not front else 0.09, solid=0.9 if not front else 0.86)
    fall = Fall(pieces, seed=47 if front else 53, unit=unit, gravity=15, spread=1.15, nestle=0.45, tumble=70,
                topple=0.2)
    order = sorted(range(len(pieces)), key=lambda i: -fall.y1[i])
    flakes = chips(59 if front else 61, 9, (0, 0.05 * unit), unit, 8)
    # The brass where it is worn, moved from captain_frame's canvas onto this one.
    shift = (CAPTAIN_WORK_ROOT[0] - fe.WORK_ROOT[0], CAPTAIN_WORK_ROOT[1] - fe.WORK_ROOT[1])
    dress = fe.CAPTAIN_DRESS[name]
    x0, x1, _ = dress['head']
    helm_at = ((x0 + x1) / 2 + shift[0], dress['brim'] + shift[1])
    helm_scale = (x1 - x0) * (1.1 if front else 0.86) / (2 * 0.124 * PPU * fe.WORK)
    helm = fe.brass_prop('helm-front' if front else 'helm-rear')
    bell_at = (dress['bell'][0] + shift[0], dress['bell'][1] + shift[1]) if front else \
        ((x0 + x1) / 2 + shift[0] - 0.04 * unit, dress['brim'] + shift[1] + 0.32 * unit)
    # The heap as it ends, for where the helm comes down on it.
    final = draw_rocks(pieces, fall.at(CAPTAIN_DEATH_TIMES[-1]), CAPTAIN_WORK, CAPTAIN_WORK_ROOT, order,
                       lambda i, px: px, outline=fe.WORK * 0.6)
    # The helm is knocked off backward (right, for a figure looking left; toward the camera from behind) and comes
    # down on the heap; the bell falls from the chest straight to the floor ahead of the feet.
    helm_land_x = helm_at[0] + 0.2 * unit
    helm_land_y = top_at(final, helm_land_x, CAPTAIN_WORK_ROOT[1]) + 0.03 * unit
    bell_land = (bell_at[0] - 0.08 * unit, CAPTAIN_WORK_ROOT[1] + (0.05 if front else -0.07) * unit)
    g = 15 * unit
    helm_vy = 0.55 * unit
    helm_t = (helm_vy + math.sqrt(helm_vy ** 2 + 2 * g * (helm_land_y - helm_at[1]))) / g
    bell_t = math.sqrt(2 * max(1.0, bell_land[1] - bell_at[1]) / g)
    drawings = []
    for t in CAPTAIN_DEATH_TIMES:
        states = fall.at(t)
        flare = 0.85 if t == 0 else max(0.0, 0.35 - t * 3)
        amount = 1.0 if t < 0.1 else max(0.3, 1.0 - (t - 0.1) * 2.2)
        glow = [0.4 if t == 0 else max(0.0, 0.3 - t * 2.5)] * len(pieces)
        canvas = np.zeros((CAPTAIN_WORK[1], CAPTAIN_WORK[0], 4))
        # The bell, under the rocks from behind (it falls on the far side of the heap), over them from the front.
        if t > 0:
            if t < bell_t:
                f = t / bell_t
                at = (bell_at[0] + (bell_land[0] - bell_at[0]) * f, bell_at[1] + 0.5 * g * t * t)
                bell = (fe.brass_prop('bell-front'), at, fe.CAPTAIN_SCALE, 40 * f)
            else:
                since = t - bell_t
                hop = 0.025 * unit * math.sin(math.pi * min(1.0, since / 0.12)) * max(0.0, 1 - since / 0.12)
                bell = (fe.brass_prop('bell-fallen'), (bell_land[0] - 0.04 * unit, bell_land[1] - hop),
                        fe.CAPTAIN_SCALE, 0.0)
        if t > 0 and not front:
            canvas = fe.place_prop(canvas, *bell)
        canvas = draw_rocks(pieces, states, CAPTAIN_WORK, CAPTAIN_WORK_ROOT, order,
                            lambda i, px: captain_recolor(px, amount, flare), outline=fe.WORK * 0.6,
                            exploded=0.012 if t == 0 else 0.0, glow=glow, base=canvas)
        canvas = draw_chips(canvas, flakes, t, 0.22, CAPTAIN_WORK_ROOT, unit, stone=(0.24, 0.1, 0.09))
        if t == 0:
            # Still worn as it cracks.
            canvas = captain_dressed(canvas, dress_shifted(dress, shift), front)
        else:
            if front:
                canvas = fe.place_prop(canvas, *bell)
            if t < helm_t:
                at = (helm_at[0] + 0.2 * unit * t / helm_t, helm_at[1] - helm_vy * t + 0.5 * g * t * t)
                canvas = fe.place_prop(canvas, helm, at, helm_scale, 110 * t / helm_t)
            else:
                since = t - helm_t
                hop = 0.02 * unit * math.sin(math.pi * min(1.0, since / 0.1)) * max(0.0, 1 - since / 0.1)
                canvas = fe.place_prop(canvas, fe.brass_prop('helm-fallen'), (helm_land_x, helm_land_y - hop),
                                       fe.CAPTAIN_SCALE, 0.0)
        small = fe.downsample(canvas)
        full = fe.finish(small[..., :3], small[..., 3:4])
        pivot = (CAPTAIN_WORK_ROOT[0] // fe.WORK, (CAPTAIN_WORK[1] - CAPTAIN_WORK_ROOT[1]) // fe.WORK)
        drawings.append(crop_to_content(full, pivot))
    return drawings


def dress_shifted(dress, shift):
    """A CAPTAIN_DRESS entry moved by `shift` work texels."""
    out = dict(dress)
    out['head'] = (dress['head'][0] + shift[0], dress['head'][1] + shift[0], dress['head'][2] + shift[1])
    out['brim'] = dress['brim'] + shift[1]
    for key in ('bell',):
        if key in dress:
            out[key] = (dress[key][0] + shift[0], dress[key][1] + shift[1])
    for key in ('chain', 'back'):
        if key in dress:
            out[key] = tuple((x + shift[0], y + shift[1]) for x, y in dress[key])
    return out


def captain_windup_work(out_dir):
    """The windups' recoloured work frames with a grid, for placing the brass (as fe.captain_work)."""
    out_dir.mkdir(parents=True, exist_ok=True)
    for name, (straight, root) in grunt_windup_sources().items():
        px = fe.red_hot(captain_cell(straight, root))
        img = Image.fromarray(fe.finish(px[..., :3], px[..., 3:4]), 'RGBA')
        bg = Image.new('RGBA', img.size, (40, 40, 52, 255))
        bg.alpha_composite(img)
        d = ImageDraw.Draw(bg)
        for v in range(0, fe.WORK_SIZE, 40):
            colour = (90, 90, 110, 255) if v % 200 else (150, 150, 180, 255)
            d.line((v, 0, v, fe.WORK_SIZE), fill=colour)
            d.line((0, v, fe.WORK_SIZE, v), fill=colour)
        for v in range(0, fe.WORK_SIZE, 200):
            d.text((v + 2, 2), str(v), fill=(255, 255, 255, 255))
            d.text((2, v + 2), str(v), fill=(255, 255, 255, 255))
        bg.save(out_dir / ('captain-work_%s.png' % name))


# ---------------------------------------------------------------------------------------------------------------------
# The Cinder Mite
# ---------------------------------------------------------------------------------------------------------------------
MITE = MASTERS / 'mite-runtime-01'
MITE_CANVAS = (80, 68)
# BuildMiteAssets: the crop x 80, top 240, 1056 x 896 of each master stretched over the 80 x 68 canvas, the pivot at
# (508/1056, 32/896) of it - the floor root at (588, 1104) in the master.
MITE_SCALE = (1056 / 80, 896 / 68)
MITE_ROOT = (80 + 508, 240 + 896 - 32)
MITE_PIVOT = (508 / MITE_SCALE[0], 32 / MITE_SCALE[1])


def mite_blit(img, size=MITE_CANVAS, pivot=MITE_PIVOT, root=MITE_ROOT):
    """BuildMiteAssets' GPU sample of a master: its floor root on the pivot, a texel MITE_SCALE master texels."""
    w, h = size
    i = np.arange(w) + 0.5
    j = np.arange(h) + 0.5
    sx = root[0] + (i - pivot[0]) * MITE_SCALE[0]
    sy = root[1] - (j - pivot[1]) * MITE_SCALE[1]
    return blit(img, sx, sy, max(MITE_SCALE))[::-1]


def mite_master(facing):
    return sheet(MITE / ('mite-%s-idle.png' % facing))


def warp(img, root, sx, sy, shear):
    """The figure squashed and leaned about its floor root (premultiplied resample of a straight image): x scaled by
    sx, height by sy, its top carried `shear` of its height to the right (back, for a figure looking left)."""
    pm = straight_to_premult(img)
    rx, ry = root
    # Inverse: output (u, v) -> input. up' = ry - v, up = up'/sy; x - rx = (u - rx - up' * shear / sy... ) / sx
    a, b = 1 / sx, shear / (sx * sy)
    c = rx - a * rx - b * ry
    e = 1 / sy
    f = ry - e * ry
    data = (a, b, c, 0.0, e, f)
    return premult_to_straight(transform(pm, data, (img.shape[1], img.shape[0])))


def mite_windup(facing):
    """The Mite coiled back over its feet before it lunges: squashed, leaned back, its seams and flame run hot."""
    img = mite_master(facing)
    coiled = warp(img, MITE_ROOT, 1.07, 0.86, 0.16)
    coiled = heat(coiled, 1.25)
    return to_uint8(mite_blit(coiled))


# The Mite's burst: a step of 0.06 s over 0.54 s.
MITE_DEATH_TIMES = [0.0, 0.06, 0.12, 0.19, 0.27, 0.37, 0.47]
MITE_DEATH_STEPS = [0, 1, 2, 3, 4, 5, 5, 6, 6]
MITE_WORK = (2300, 1500)
MITE_WORK_ROOT = (1150, 1250)


def embers(seed, count, unit):
    """Sparks off the burst, as (x, y, vx, vy, radius, life) in work texels from the root."""
    rng = np.random.default_rng(seed)
    out = []
    for k in range(count):
        a = rng.uniform(0, 2 * math.pi)
        out.append((math.cos(a) * rng.uniform(0.02, 0.1) * unit, rng.uniform(0.12, 0.3) * unit,
                    math.cos(a) * rng.uniform(0.25, 0.8) * unit, rng.uniform(0.5, 1.4) * unit,
                    rng.uniform(0.55, 1.0), rng.uniform(0.3, 0.5)))
    return out


def glow_disc(size, centre, radius, colour, strength):
    """A soft round light (premultiplied linear), brightest at its centre."""
    w, h = size
    y, x = np.mgrid[0:h, 0:w]
    d = np.hypot(x - centre[0], (y - centre[1]) * 1.25) / radius
    a = np.clip(1 - d, 0, 1) ** 2 * strength
    rgb = fe.to_linear(np.array(colour))
    return np.concatenate([rgb * a[..., None], a[..., None]], axis=-1)


def draw_embers(canvas, sparks, t, start, root, unit, texel):
    s = t - start
    if s <= 0:
        return canvas
    layer = np.zeros_like(canvas)
    for x, y, vx, vy, r, life in sparks:
        if s >= life:
            continue
        fade = 1 - s / life
        px = root[0] + x + vx * s
        py = root[1] - (y + vy * s - 0.4 * unit * s * s)
        rad = texel * (0.8 + 1.1 * r) * (0.6 + 0.4 * fade)
        hot = (1.0, 0.85 * fade + 0.45 * (1 - fade), 0.45 * fade)
        x0, x1 = int(px - rad * 3), int(px + rad * 3) + 1
        y0, y1 = int(py - rad * 3), int(py + rad * 3) + 1
        if x1 <= 0 or y1 <= 0 or x0 >= canvas.shape[1] or y0 >= canvas.shape[0]:
            continue
        yy, xx = np.mgrid[max(0, y0):min(canvas.shape[0], y1), max(0, x0):min(canvas.shape[1], x1)]
        d = np.hypot(xx - px, yy - py) / rad
        core = np.clip(1.5 - d, 0, 1)
        halo = np.clip(1 - d / 3, 0, 1) ** 2 * 0.45
        a = np.clip(core + halo, 0, 1) * fade
        rgb = fe.to_linear(np.array(hot))
        sub = layer[max(0, y0):min(canvas.shape[0], y1), max(0, x0):min(canvas.shape[1], x1)]
        add = np.concatenate([rgb * a[..., None], a[..., None]], axis=-1)
        sub[:] = over(sub, add)
    return over(canvas, layer)


def flame_of(img):
    """The Mite's crown of flame: the largest molten shape on it."""
    hot = (lava_of(img) > 0.35) & (img[..., 3] > 0.3)
    labels, n = ndi.label(hot)
    sizes = ndi.sum(hot, labels, range(1, n + 1))
    return ndi.binary_dilation(labels == int(np.argmax(sizes)) + 1, iterations=4)


def mite_death(facing):
    """The Mite swelling white-hot and bursting: its rocks flung out round the spot and falling, embers rising off."""
    img = isolate(mite_master(facing), (0, 0, 1176, 1338))
    unit = PPU * MITE_SCALE[0]
    # On the burst's own canvas, the root moved so the flung rocks have room.
    big = np.zeros((MITE_WORK[1], MITE_WORK[0], 4))
    ox, oy = MITE_WORK_ROOT[0] - MITE_ROOT[0], MITE_WORK_ROOT[1] - MITE_ROOT[1]
    big[oy:oy + img.shape[0], ox:ox + img.shape[1]] = img
    # The flame goes up in the burst; the rocks under it are what is flung.
    stones = big.copy()
    stones[flame_of(big)] = 0
    pieces = cut_rocks(stones, MITE_WORK_ROOT, min_area=1500, erode=1, dark=0.05, solid=0, lines=9)
    rng = np.random.default_rng(23 if facing == 'front' else 29)
    out, lift = [], []
    for p in pieces:
        # Flung out from the body's middle, upward, the outer rocks farthest.
        dx = p.cx / unit
        side = 1 if dx >= 0 else -1
        out.append((dx * 1.1 + side * rng.uniform(0.03, 0.1), 0))
        lift.append((0, rng.uniform(0.6, 1.2)))
    fall = Fall(pieces, seed=31 if facing == 'front' else 37, unit=unit, gravity=11, nestle=0.2, depth=0.06,
                delay=0.0, tumble=160, out=out, lift=lift)
    sparks = embers(41 if facing == 'front' else 43, 16, unit)
    out_size = (160, 104)
    out_pivot = (80, 30)
    order = sorted(range(len(pieces)), key=lambda i: -fall.y1[i])
    drawings = []
    texel = MITE_SCALE[0]
    centre = (MITE_WORK_ROOT[0], MITE_WORK_ROOT[1] - 0.22 * unit)
    for t in MITE_DEATH_TIMES:
        if t == 0:
            # Swelling white-hot: the whole Mite a little larger, its seams and flame flaring.
            swollen = warp(big, MITE_WORK_ROOT, 1.1, 1.08, 0.0)
            canvas = straight_to_premult(heat(swollen, 2.0))
            canvas = over(canvas, glow_disc(MITE_WORK, centre, 0.32 * unit, (1.0, 0.75, 0.35), 0.35))
        else:
            states = fall.at(t - 0.06)
            flare = max(0.3, 1.6 - (t - 0.06) * 3.2)
            canvas = draw_rocks(pieces, states, MITE_WORK, MITE_WORK_ROOT, order, lambda i, px: heat(px, flare),
                                outline=texel * 0.6)
            if t < 0.15:
                k = 1 - (t - 0.06) / 0.09
                flash = glow_disc(MITE_WORK, centre, (0.3 + 0.25 * (1 - k)) * unit, (1.0, 0.8, 0.4), 0.85 * k)
                canvas = over(canvas, flash)
            canvas = draw_embers(canvas, sparks, t, 0.06, MITE_WORK_ROOT, unit, texel)
        straight = premult_to_straight(canvas)
        sampled = mite_blit(straight, out_size, out_pivot, MITE_WORK_ROOT)
        drawings.append(crop_to_content(to_uint8(sampled), out_pivot))
    return drawings


# ---------------------------------------------------------------------------------------------------------------------
# The Runner and the Tank: their Blender renders (BuildBlenderFoundryEnemies.py --motion), packed as their walk was
# ---------------------------------------------------------------------------------------------------------------------
FOUNDRY = {e['enemy']: e for e in json.loads((fe.DOCS / 'sprites.json').read_text())['enemies']}
# The Runner's death over 0.6 s, the Tank's over 0.66 s, a step of 0.06 s: the struck drawing, the stumble, the fall,
# the impact held a beat, then lying still while it fades.
RENDERED_DEATH_STEPS = {
    'runner': [0, 1, 2, 3, 3, 4, 4, 4, 4, 4],
    'tank': [0, 0, 1, 2, 3, 3, 4, 4, 4, 4, 4],
}


def rendered_frames(enemy):
    """Every motion render of the enemy at the game's resolution, with the render canvas's pivot."""
    meta = json.loads((RENDER / enemy / 'frames.json').read_text())
    frames = {f['name']: fe.process_render(RENDER / enemy / f['file'], RENDER / enemy / f['mask'], meta['supersample'])
              for f in meta['frames']}
    return frames, tuple(meta['pivot'])


def rendered_windups(enemy):
    """The two windups cropped to the walk's own canvas about the same floor root."""
    frames, pivot = rendered_frames(enemy)
    w, h = FOUNDRY[enemy]['size']
    px, py = FOUNDRY[enemy]['pivot']
    out = {}
    for facing in ('front', 'rear'):
        full = frames[facing + '-windup']
        H = full.shape[0]
        x0 = pivot[0] - px
        y0 = (H - pivot[1]) - (h - py)
        crop = full[y0:y0 + h, x0:x0 + w]
        lost = full[..., 3].astype(int).sum() - crop[..., 3].astype(int).sum()
        if lost > 0:
            sys.exit('%s %s-windup leaves its walking canvas by %d alpha: pull the pose in' % (enemy, facing, lost))
        out[facing + '-windup'] = crop
    return out


def rendered_deaths(enemy, facing):
    frames, pivot = rendered_frames(enemy)
    H = next(iter(frames.values())).shape[0]
    drawings = []
    for k in range(1, 6):
        drawings.append(crop_to_content(frames['%s-dying-%d' % (facing, k)], pivot))
    return drawings


# ---------------------------------------------------------------------------------------------------------------------
# Assets: TGAs and metas beside each enemy's walk, and the optional parts appended to its set
# ---------------------------------------------------------------------------------------------------------------------
ENEMIES = {
    # folder, file stem, the separator its file names use, its set; the windup's beat in seconds (EnemyLookView shows
    # it at most half the blow's interval): the quick Runner's shortest, the Tank's slam the longest.
    'grunt': dict(folder='SlagBrute', stem='Grunt', sep='-', set='SlagBruteArtSet', windup=0.2),
    'mite': dict(folder='CinderMite', stem='Mite', sep='_', set='CinderMiteArtSet', windup=0.18),
    'runner': dict(folder='Runner', stem='Runner', sep='-', set='RunnerArtSet', windup=0.15),
    'tank': dict(folder='Tank', stem='Tank', sep='-', set='TankArtSet', windup=0.25),
    'captain': dict(folder='GruntCaptain', stem='Captain', sep='-', set='GruntCaptainArtSet', windup=0.22),
}
# Where the set's own lines end and the optional parts this script owns begin.
TAIL = '  _strikeWindup:'


def windups_of(enemy):
    if enemy == 'grunt':
        return grunt_windups()
    if enemy == 'mite':
        return {'front-windup': mite_windup('front'), 'rear-windup': mite_windup('rear')}
    if enemy == 'captain':
        return captain_windups()
    return rendered_windups(enemy)


def deaths_of(enemy):
    """Each facing's drawings (texels, pivot) and the steps that play them."""
    if enemy == 'grunt':
        return {f: grunt_death(f) for f in ('front', 'rear')}, GRUNT_DEATH_STEPS
    if enemy == 'mite':
        return {f: mite_death(f) for f in ('front', 'rear')}, MITE_DEATH_STEPS
    if enemy == 'captain':
        return {f: captain_death(f) for f in ('front', 'rear')}, CAPTAIN_DEATH_STEPS
    return {f: rendered_deaths(enemy, f) for f in ('front', 'rear')}, RENDERED_DEATH_STEPS[enemy]


def walking_pivot(folder, stem, sep):
    """The spritePivot line of the enemy's front idle meta, verbatim, so a windup's pivot is exactly the walk's."""
    meta = (fe.ART / folder / ('CHR_%s_front%sidle.tga.meta' % (stem, sep))).read_text(encoding='utf-8')
    return next(line for line in meta.split('\n') if line.strip().startswith('spritePivot:'))


def write_texture(path, pixels, pivot_line=None, pivot=None):
    """A TGA and its meta (fe.TEXTURE_META, the import settings every enemy's frames share), keeping its GUID."""
    meta = Path(str(path) + '.meta')
    guid = fe.existing_guid(meta)
    fe.write_tga(path, pixels)
    h, w = pixels.shape[:2]
    text = fe.TEXTURE_META.format(guid=guid, px=(pivot[0] / w) if pivot else 0.5, py=(pivot[1] / h) if pivot else 0.5,
                                  android=256 if max(w, h) <= 256 else 512)
    if pivot_line is not None:
        text = '\n'.join(pivot_line if line.strip().startswith('spritePivot:') else line for line in text.split('\n'))
    with open(meta, 'wb') as f:
        f.write(text.encode('utf-8'))
    return dict(file=path.name, guid=guid, size=[w, h], astc_4x4_bytes=fe.astc_bytes(w, h)), guid


def ref(guid):
    return '{fileID: 21300000, guid: %s, type: 3}' % guid


def patch_set(path, windup, seconds, front, rear, step):
    """The set's own lines kept as they are; the optional parts (any earlier run's replaced) appended."""
    text = path.read_bytes().decode('utf-8')
    if '\n' + TAIL in text:
        text = text[:text.index('\n' + TAIL) + 1]
    lines = [TAIL]
    for body, flash in windup:
        lines += ['  - Body: ' + ref(body), '    Flash: ' + ref(flash)]
    lines.append('  _strikeWindupSeconds: %s' % seconds)
    lines.append('  _deathFront:')
    lines += ['  - ' + ref(g) for g in front]
    lines.append('  _deathRear:')
    lines += ['  - ' + ref(g) for g in rear]
    lines.append('  _deathStep: %s' % step)
    with open(path, 'wb') as f:
        f.write((text + '\n'.join(lines) + '\n').encode('utf-8'))


def pack(enemy):
    spec = ENEMIES[enemy]
    folder = fe.ART / spec['folder']
    sep, stem = spec['sep'], spec['stem']
    textures = []
    windup = []
    pivot_line = walking_pivot(spec['folder'], stem, sep)
    shapes = set()
    poses = windups_of(enemy)
    for facing in ('front', 'rear'):
        px = poses[facing + '-windup']
        shapes.add(px.shape)
        guids = []
        for flash in (False, True):
            path = folder / ('CHR_%s_%s%swindup%s.tga' % (stem, facing, sep, '_Flash' if flash else ''))
            record, guid = write_texture(path, fe.flash_of(px) if flash else px, pivot_line=pivot_line)
            textures.append(record)
            guids.append(guid)
        windup.append(guids)
    idle = np.asarray(Image.open(folder / ('CHR_%s_front%sidle.tga' % (stem, sep))))
    if shapes != {idle.shape}:
        sys.exit('%s windups are not on the walk\'s canvas: %s against %s' % (enemy, shapes, idle.shape))
    drawings, steps = deaths_of(enemy)
    order = {}
    for facing in ('front', 'rear'):
        guids = []
        for k, (px, pivot) in enumerate(drawings[facing]):
            name = 'dying' + sep + facing + sep + str(k)
            record, guid = write_texture(folder / ('CHR_%s_%s.tga' % (stem, name)), px, pivot=pivot)
            record['pivot'] = [int(pivot[0]), int(pivot[1])]
            textures.append(record)
            guids.append(guid)
        order[facing] = [guids[s] for s in steps]
    patch_set(folder / (spec['set'] + '.asset'), windup, spec['windup'], order['front'], order['rear'], DEATH_STEP)
    total = sum(t['astc_4x4_bytes'] for t in textures)
    seconds = round(len(steps) * DEATH_STEP, 3)
    print('HORDE_PACKED %s windup %.2fs death %.2fs textures %d ASTC4x4 %d bytes' % (enemy, spec['windup'], seconds,
                                                                                    len(textures), total))
    return dict(enemy=enemy, art_set=spec['set'] + '.asset', strike_windup_seconds=spec['windup'], death_step=DEATH_STEP,
                death_seconds=seconds, death_steps=steps, textures=textures, astc_4x4_bytes=total)


# ---------------------------------------------------------------------------------------------------------------------
# Preview (no assets written)
# ---------------------------------------------------------------------------------------------------------------------
def preview_strip(images, path, scale=2, bg=(92, 86, 90, 255)):
    w = sum(im.shape[1] for im in images) * scale + 8 * len(images)
    h = max(im.shape[0] for im in images) * scale
    strip = Image.new('RGBA', (w, h), bg)
    x = 0
    for im in images:
        tile = Image.fromarray(im, 'RGBA').resize((im.shape[1] * scale, im.shape[0] * scale), Image.LANCZOS)
        strip.alpha_composite(tile, (x, h - tile.size[1]))
        x += tile.size[0] + 8
    strip.save(path)


def main():
    if PREVIEW:
        out = Path(PREVIEW)
        out.mkdir(parents=True, exist_ok=True)
        if 'grunt' in ONLY:
            w = grunt_windups()
            idle = np.asarray(Image.open(fe.ART / 'SlagBrute/CHR_Grunt_front-idle.tga').convert('RGBA'))
            strike = np.asarray(Image.open(fe.ART / 'SlagBrute/CHR_Grunt_front-strike.tga').convert('RGBA'))
            ridle = np.asarray(Image.open(fe.ART / 'SlagBrute/CHR_Grunt_rear-idle.tga').convert('RGBA'))
            rstrike = np.asarray(Image.open(fe.ART / 'SlagBrute/CHR_Grunt_rear-strike.tga').convert('RGBA'))
            preview_strip([idle, w['front-windup'], strike, ridle, w['rear-windup'], rstrike], out / 'grunt_windup.png')
            for facing in ('front', 'rear'):
                d = grunt_death(facing)
                preview_strip([im for im, _ in d], out / ('grunt_death_%s.png' % facing))
                print('grunt death', facing, [(im.shape[1], im.shape[0], pv) for im, pv in d])
        for enemy in ('runner', 'tank'):
            if enemy not in ONLY:
                continue
            folder = fe.ART / fe.ENEMIES[enemy]['folder']
            stem = fe.ENEMIES[enemy]['stem']
            shipped = [np.asarray(Image.open(folder / ('CHR_%s_%s.tga' % (stem, n))).convert('RGBA'))
                       for n in ('front-idle', 'front-strike', 'rear-idle', 'rear-strike')]
            w = rendered_windups(enemy)
            preview_strip([shipped[0], w['front-windup'], shipped[1], shipped[2], w['rear-windup'], shipped[3]],
                          out / ('%s_windup.png' % enemy))
            for facing in ('front', 'rear'):
                d = rendered_deaths(enemy, facing)
                preview_strip([im for im, _ in d], out / ('%s_death_%s.png' % (enemy, facing)))
                print(enemy, 'death', facing, [(im.shape[1], im.shape[0]) for im, _ in d])
        if 'captain' in ONLY:
            w = captain_windups()
            folder = fe.ART / 'GruntCaptain'
            shipped = [np.asarray(Image.open(folder / ('CHR_Captain_%s.tga' % n)).convert('RGBA'))
                       for n in ('front-idle', 'front-strike', 'rear-idle', 'rear-strike')]
            preview_strip([shipped[0], w['front-windup'], shipped[1], shipped[2], w['rear-windup'], shipped[3]],
                          out / 'captain_windup.png')
            for facing in ('front', 'rear'):
                d = captain_death(facing)
                preview_strip([im for im, _ in d], out / ('captain_death_%s.png' % facing))
                print('captain death', facing, [(im.shape[1], im.shape[0]) for im, _ in d])
        if 'mite' in ONLY:
            shipped = {n: np.asarray(Image.open(fe.ART / ('CinderMite/CHR_Mite_%s.tga' % n)).convert('RGBA'))
                       for n in ('front_idle', 'front_strike', 'rear_idle', 'rear_strike')}
            mine = to_uint8(mite_blit(mite_master('front'))).astype(int)
            d = np.abs(mine - shipped['front_idle'].astype(int))
            print('mite idle reproduction: mean %.2f max %d' % (d.mean(), d.max()))
            preview_strip([shipped['front_idle'], mite_windup('front'), shipped['front_strike'], shipped['rear_idle'],
                           mite_windup('rear'), shipped['rear_strike']], out / 'mite_windup.png', scale=4)
            for facing in ('front', 'rear'):
                d = mite_death(facing)
                preview_strip([im for im, _ in d], out / ('mite_death_%s.png' % facing), scale=3)
                print('mite death', facing, [(im.shape[1], im.shape[0]) for im, _ in d])
        return

    report = [pack(enemy) for enemy in ONLY]
    DOCS.mkdir(parents=True, exist_ok=True)
    sprites = DOCS / 'sprites.json'
    known = {r['enemy']: r for r in json.loads(sprites.read_text()).get('enemies', [])} if sprites.exists() else {}
    known.update({r['enemy']: r for r in report})
    everything = [known[e] for e in ENEMIES if e in known]
    total = sum(r['astc_4x4_bytes'] for r in everything)
    with open(sprites, 'wb') as f:
        f.write((json.dumps(dict(pixels_per_unit=PPU, enemies=everything, total_astc_4x4_bytes=total), indent=1)
                 + '\n').encode('utf-8'))
    print('HORDE_TOTAL ASTC4x4 %d bytes (%.2f MiB) for %s' % (total, total / 2 ** 20,
                                                             ', '.join(r['enemy'] for r in everything)))


if __name__ == '__main__':
    main()
