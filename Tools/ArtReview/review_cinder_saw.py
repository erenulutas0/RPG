"""Review images for the Cinder Saw's painted blade and edge ring (build_cinder_saw_vfx.py), laid over the lead's own
in-game capture of the pixel blade (Tools/ArtReview/SawRuntimeCapture.cs) at that capture's scale.

    python Tools/ArtReview/review_cinder_saw.py --capture DIR --out DIR

The capture's square frames are rendered at a fixed orthographic size round the hero (saw-*: 2.4, so 112.5 pixels a
world unit; wide-*: 4.2, 64.3 a unit; the S23 shows 120 a unit). For each, the pixel blade is found by its own palette
(EffectArt.SawFrames: EmberHot, EmberLight, Ember, EmberDark, Lava), painted out from the floor round it, and the painted
blade drawn where it stood - its centre, the same orbit and lift - exactly as the game imports it (the sheet and the
strike set's cells, before ASTC), resampled bilinearly and blended straight-alpha in sRGB as the project's gamma colour
space does. Writes:

    beside-<frame>.png   the lead's frame and the same frame with the painted blade, side by side, same scale
    ring-<frame>.png     the same with the edge ring at the aura's edge, where the view draws it (its SplashRadius)
    frames.png           the blade's frames at 4x and at the phone's scale, on the foundry's stone and on its lava, and
                         the ring's frames at the phone's scale at the base aura's 2.0
    review-sheet.png     four of the pairs above stacked, the lead's frames on the left
    orbit.gif            the blade circling the hero at the view's timing over a painted-out capture, the ring once
"""
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = Path(__file__).resolve().parents[2]
ARGS = sys.argv[1:]


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


CAPTURE = Path(arg("--capture", str(ROOT / "ArtDirection/2026-11-25/saw-runtime-01")))
OUT = Path(arg("--out", str(ROOT / "TestResults/cinder-saw-review")))
STRIKES = ROOT / "Assets/_Project/Art/Effects/FoundryStrikes"
PHONE = 120.0
# CombatEffectsView's numbers for the blade and the ring.
ORBIT_SECONDS = 0.7
ORBIT_SHARE = 0.85
LIFT = 0.18
FRAME_SECONDS = 0.035
RING_FRAME_SECONDS = 0.07
RING_GROW_SECONDS = 0.14
RING_FADE_SECONDS = 0.11
RING_ALPHA = 0.6
# The pixel blade's palette (PixelPalette), which nothing else in the foundry draws in all five.
OLD_SAW = ["#FDFE93", "#FE7B1A", "#E55640", "#A54C2B", "#881007"]
STONE = (74, 67, 88)
LAVA = (176, 74, 28)


def strip(field):
    text = (STRIKES / "FoundryStrikeArtSet.asset").read_text(encoding="utf-8")
    block = re.search(r"(?ms)^  %s:\n(.*?)(?=^  _|\Z)" % field, text).group(1)
    tpu = float(re.search(r"TexelsPerUnit: ([\d.]+)", block).group(1))
    px, py = (float(v) for v in re.search(r"Pivot: \{x: ([\d.\-e]+), y: ([\d.\-e]+)\}", block).groups())
    height = int(re.search(r"SheetHeight: (\d+)", block).group(1))
    cells = [tuple(int(v) for v in m) for m in
             re.findall(r"- X: (\d+)\n\s+Y: (\d+)\n\s+Width: (\d+)\n\s+Height: (\d+)", block)]
    sheet = np.asarray(Image.open(STRIKES / "VFX_CinderSaw.png").convert("RGBA"))
    # Cells are in Unity's texel space, row 0 at the bottom.
    frames = [sheet[height - y - h:height - y, x:x + w] for x, y, w, h in cells]
    return frames, tpu, (px, py)


BLADE, BLADE_TPU, BLADE_PIVOT = strip("_sawBlade")
RING, RING_TPU, RING_PIVOT = strip("_sawRing")


def scaled(frame, factor):
    """A frame resampled by factor, bilinearly on premultiplied colour (as the GPU filters a straight-alpha texture
    without fringes once colour is bled into the clear texels)."""
    f = frame.astype(np.float64) / 255.0
    pre = np.concatenate([f[..., :3] * f[..., 3:4], f[..., 3:4]], axis=-1)
    h, w = f.shape[:2]
    size = (max(1, round(w * factor)), max(1, round(h * factor)))
    out = np.stack([np.asarray(Image.fromarray(pre[..., c].astype(np.float32), "F").resize(size, Image.BILINEAR))
                    for c in range(4)], axis=-1)
    return out


def draw(canvas, frame, pivot, at, factor, alpha=1.0):
    """Blends a frame onto canvas (float RGB, 0-1, sRGB) with its pivot at the pixel at, straight alpha in sRGB."""
    pre = scaled(frame, factor) * alpha
    h, w = pre.shape[:2]
    x0 = int(round(at[0] - pivot[0] * w))
    y0 = int(round(at[1] - (1 - pivot[1]) * h))
    cx0, cy0 = max(0, x0), max(0, y0)
    cx1, cy1 = min(canvas.shape[1], x0 + w), min(canvas.shape[0], y0 + h)
    if cx1 <= cx0 or cy1 <= cy0:
        return
    part = pre[cy0 - y0:cy1 - y0, cx0 - x0:cx1 - x0]
    canvas[cy0:cy1, cx0:cx1] = part[..., :3] + canvas[cy0:cy1, cx0:cx1] * (1 - part[..., 3:4])


def old_blade(image):
    """The pixel blade's centre and its painted-out mask: the largest blob of its palette, grown to take its outline."""
    rgb = image[..., :3].astype(np.int32)
    hit = np.zeros(rgb.shape[:2], bool)
    for h in OLD_SAW:
        c = np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)])
        hit |= np.abs(rgb - c).sum(axis=-1) < 40
    labels, count = ndimage.label(ndimage.binary_closing(hit, iterations=3))
    if count == 0:
        return None, None
    sizes = ndimage.sum(hit, labels, range(1, count + 1))
    blob = labels == (1 + int(np.argmax(sizes)))
    ys, xs = np.nonzero(blob)
    centre = ((xs.min() + xs.max() + 1) / 2, (ys.min() + ys.max() + 1) / 2)
    radius = max(xs.max() - xs.min(), ys.max() - ys.min()) / 2 + 4
    yy, xx = np.mgrid[:image.shape[0], :image.shape[1]]
    mask = ((xx + 0.5 - centre[0]) ** 2 + (yy + 0.5 - centre[1]) ** 2) <= radius ** 2
    mask &= ndimage.binary_dilation(blob, iterations=4)
    return centre, mask


def painted_out(image, mask):
    """The masked pixels filled from the nearest unmasked ones and softened: the floor where the pixel blade stood."""
    out = image[..., :3].astype(np.float64) / 255.0
    _, (iy, ix) = ndimage.distance_transform_edt(mask, return_indices=True)
    filled = out[iy, ix]
    soft = np.stack([ndimage.gaussian_filter(filled[..., c], 2.0) for c in range(3)], axis=-1)
    out[mask] = soft[mask]
    return out


def to_image(canvas):
    return Image.fromarray((np.clip(canvas, 0, 1) * 255 + 0.5).astype(np.uint8), "RGB")


def ring_radius(splash):
    """The Saw's ring, in world units: the aura's edge (CombatEffectsView draws it at the weapon's SplashRadius)."""
    return splash


def tile(frame, pivot, factor, background, pad=6):
    h, w = frame.shape[:2]
    out = np.ones((int(h * factor) + 2 * pad, int(w * factor) + 2 * pad, 3)) * np.array(background) / 255.0
    draw(out, frame, pivot, (pivot[0] * w * factor + pad, (1 - pivot[1]) * h * factor + pad), factor)
    return out


def grid(rows, gap=4):
    """Rows of images laid left to right, top to bottom, on a dark ground."""
    width = max(sum(t.shape[1] + gap for t in row) for row in rows) + gap
    height = sum(max(t.shape[0] for t in row) + gap for row in rows) + gap
    sheet = np.ones((height, width, 3)) * 0.06
    y = gap
    for row in rows:
        x = gap
        for t in row:
            sheet[y:y + t.shape[0], x:x + t.shape[1]] = t
            x += t.shape[1] + gap
        y += max(t.shape[0] for t in row) + gap
    return sheet


def capture_frames():
    for path in sorted(CAPTURE.glob("saw-*.png")) + sorted(CAPTURE.glob("wide-*.png")):
        yield path, 540 / (2 * (2.4 if path.name.startswith("saw") else 4.2))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    chosen = {}
    for path, unit in capture_frames():
        image = np.asarray(Image.open(path).convert("RGB"))
        centre, mask = old_blade(image)
        if centre is None:
            continue
        hero = (image.shape[1] / 2, image.shape[0] / 2)
        # The pixel blade's floor point, and from it the aura's radius the view circled at.
        floor_x = (centre[0] - hero[0]) / unit
        floor_y = -((centre[1] + LIFT * unit) - hero[1]) / unit / 0.5
        splash = float(np.hypot(floor_x, floor_y)) / ORBIT_SHARE
        canvas = painted_out(image, mask)
        frame = int(path.stem.split("-")[1]) % len(BLADE)
        draw(canvas, BLADE[frame], BLADE_PIVOT, centre, unit / BLADE_TPU)
        before = image.astype(np.float64) / 255.0
        pair = np.concatenate([before, np.ones((before.shape[0], 8, 3)) * 0.08, canvas], axis=1)
        to_image(pair).save(OUT / ("beside-%s.png" % path.stem))
        ring = canvas.copy()
        radius = ring_radius(splash)
        draw(ring, RING[0], RING_PIVOT, hero, unit * radius / RING_TPU, RING_ALPHA)
        draw(ring, BLADE[frame], BLADE_PIVOT, centre, unit / BLADE_TPU)
        to_image(np.concatenate([before, np.ones((before.shape[0], 8, 3)) * 0.08, ring], axis=1)).save(
            OUT / ("ring-%s.png" % path.stem))
        chosen[path.stem] = (painted_out(image, mask), unit, splash)
        print("%s: the pixel blade at (%.0f, %.0f), its circle %.2f units, the ring %.2f" % (
            path.name, centre[0], centre[1], splash, radius))

    # The blade at 4x and at the phone's scale, on the stone and on the lava; the ring at the phone's scale at 2.0.
    rows = []
    for background in (STONE, LAVA):
        rows.append([tile(frame, BLADE_PIVOT, 4.0, background) for frame in BLADE])
        rows.append([tile(frame, BLADE_PIVOT, PHONE / BLADE_TPU, background) for frame in BLADE])
    rows.append([tile(frame, RING_PIVOT, PHONE * 2.0 / RING_TPU, STONE) for frame in RING])
    to_image(grid(rows)).save(OUT / "frames.png")
    pairs = [OUT / ("%s.png" % name) for name in ("beside-saw-003", "beside-saw-005", "ring-saw-009", "beside-wide-013")]
    pairs = [np.asarray(Image.open(p)).astype(np.float64) / 255.0 for p in pairs if p.exists()]
    if pairs:
        to_image(grid([[p] for p in pairs])).save(OUT / "review-sheet.png")

    # The orbit at the view's timing, two turns, over the first close frame painted out; the ring once.
    stem = next((s for s in chosen if s.startswith("saw")), None)
    if stem:
        base, unit, splash = chosen[stem]
        hero = (base.shape[1] / 2, base.shape[0] / 2)
        frames = []
        step = 0.03
        for k in range(int(2 * ORBIT_SECONDS / step)):
            t = k * step
            canvas = base.copy()
            if t < 0.21:
                radius = ring_radius(splash)
                grow = min(1.0, t / RING_GROW_SECONDS)
                shown = radius * (24 / 38 + (1 - 24 / 38) * (1 - (1 - grow) ** 2))
                fade = min(1.0, (0.21 - t) / RING_FADE_SECONDS)
                draw(canvas, RING[min(int(t / RING_FRAME_SECONDS), len(RING) - 1)], RING_PIVOT, hero,
                     unit * shown / RING_TPU, RING_ALPHA * fade)
            turn = 2 * np.pi * t / ORBIT_SECONDS
            r = splash * ORBIT_SHARE
            at = (hero[0] + np.cos(turn) * r * unit, hero[1] - (np.sin(turn) * r * 0.5 + LIFT) * unit)
            draw(canvas, BLADE[int(t / FRAME_SECONDS) % len(BLADE)], BLADE_PIVOT, at, unit / BLADE_TPU)
            frames.append(to_image(canvas).quantize(256, method=Image.Quantize.MEDIANCUT))
        frames[0].save(OUT / "orbit.gif", save_all=True, append_images=frames[1:], duration=int(step * 1000), loop=0)


if __name__ == "__main__":
    main()
