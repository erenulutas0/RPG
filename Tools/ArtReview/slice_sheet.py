#!/usr/bin/env python3
"""Cut ChatGPT character art into game-ready pieces: a cut-out PARTS sheet or a FRAME sheet.

The owner generates the art in ChatGPT (docs/45_CINDER_ARCANIST_SPEC.md, sections 5 and 7). Two kinds arrive:
a parts sheet (separate body parts for the puppet rig, scattered on one image) and a frame sheet (N poses side
by side). ChatGPT output is imperfect, so both commands first key a painted background (white, grey, a gradient
or a fake checkerboard) when the sheet is not truly transparent, then clean specks and soft shadows.

Usage:
  python Tools/ArtReview/slice_sheet.py parts  SHEET.png OUT_DIR [--names names.txt] [--min-area N] [--pad P]
                                               [--key auto|none|#RRGGBB]
  python Tools/ArtReview/slice_sheet.py frames SHEET.png OUT_DIR --count N [--align feet|none]
                                               [--anchor com|overlap] [--equal-cells] [--key auto|none|#RRGGBB]
  python Tools/ArtReview/slice_sheet.py --self-test [--keep DIR]

  parts   Writes NN_<name>.png per part (cropped with padding, only that part's pixels), manifest.json and
          preview.png (every part outlined, its index drawn large). Name the parts from the preview, then run
          again with --names: one name per line in index order, or "index: name" lines; '#' starts a comment.
          Order is top-to-bottom by rows, then left-to-right.
  frames  Writes frame_NN.png on one shared canvas, frames.json (each frame's floor line, centre of mass,
          height and drift) and preview.png (the cuts on the sheet above, the strip on its shared floor line
          below, an onion-skin overlay last). --align feet (default) shifts every frame so its floor line and
          horizontal centre of mass match frame 0; --anchor overlap matches the best silhouette overlap
          instead (an arm thrown forward or a staff does not slide the body); --align none keeps each pose
          where it sat in its cell. Cuts fall in the empty column gaps between poses; where poses touch, at
          the emptiest column near the equal-width position (--equal-cells forces equal widths).

  Shared options: --key auto keys a painted background when the sheet's border is not transparent
  (none = trust the alpha as it is, #RRGGBB = key exactly that colour); --tolerance T (RGB distance of the
  background key; default from the border noise); --alpha-threshold A (default 128); --min-area N (smaller
  blobs are specks); --merge-dist D (a speck within D px of a part joins it, farther ones are dropped);
  --pad P (default 8); --fringe B (px of keyed edge to unmix from the background, 0 = hard edge);
  --no-holes (keep background pockets enclosed by a part, e.g. between an arm and the body).

Coordinates are sheet pixels, origin top-left, y down. A bbox is [x0, y0, x1, y1) with x1/y1 exclusive.
Points are pixel-edge positions (the top-left corner of pixel (0, 0) is 0, 0). A "pivot" is a Unity sprite
pivot in the written PNG (0..1, origin bottom-left).
"""
from __future__ import annotations

import argparse
import contextlib
import hashlib
import io
import json
import math
import re
import sys
import tempfile
import time
import traceback
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont
from scipy import ndimage

TOOL = 'slice_sheet'
VERSION = 1
EIGHT = np.ones((3, 3), dtype=bool)                  # parts and specks: 8-connected
FOUR = ndimage.generate_binary_structure(2, 1)       # background flood: 4-connected, so it cannot slip diagonally
LUMA = np.array([0.299, 0.587, 0.114], dtype=np.float32)
PALETTE = [(255, 70, 70), (70, 200, 255), (255, 205, 0), (120, 255, 90), (255, 110, 220), (0, 230, 190),
           (255, 150, 40), (175, 135, 255), (230, 255, 60), (255, 255, 255), (70, 140, 255), (255, 95, 145)]
TOP_JOINT, BOTTOM_JOINT, DROPPED = (80, 255, 120), (255, 150, 40), (255, 0, 255)
COORDINATES = ('sheet pixels, origin top-left, y down; bbox [x0, y0, x1, y1) exclusive; points are pixel-edge '
               'positions; pivot is a Unity sprite pivot of the written PNG (0..1, origin bottom-left)')


class SheetError(Exception):
    """A problem with the input that the user has to fix; printed without a traceback."""


# ------------------------------------------------------------------------------------------------ small helpers

def load_rgba(path):
    with Image.open(path) as image:
        return np.array(image.convert('RGBA'), dtype=np.uint8)


def save_png(array, path):
    Image.fromarray(np.ascontiguousarray(array)).save(path)


def file_sha256(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def _r(value, digits=2):
    return round(float(value), digits)


def _hex(colour):
    return '#' + ''.join(f'{int(round(float(c))):02X}' for c in np.clip(colour, 0, 255)[:3])


def _norm(vectors):
    return np.sqrt(np.einsum('...c,...c->...', vectors, vectors))


def _true_runs(mask):
    """(start, end) of every run of True in a 1-D bool array, end exclusive."""
    padded = np.r_[False, mask, False].astype(np.int8)
    change = np.flatnonzero(np.diff(padded))
    return list(zip(change[::2].tolist(), change[1::2].tolist()))


def parse_key(value):
    text = value.strip().lower()
    if text in ('auto', 'none'):
        return text
    match = re.fullmatch(r'#?([0-9a-f]{6})', text)
    if match:
        digits = match.group(1)
        return tuple(int(digits[i:i + 2], 16) for i in (0, 2, 4))
    raise argparse.ArgumentTypeError('--key takes auto, none or a colour such as #FFFFFF')


def _key_text(key):
    return _hex(key) if isinstance(key, tuple) else key


# ------------------------------------------------------------------------------------------- background keying

@dataclass
class Background:
    mode: str                  # flat | colour | checker
    expected: np.ndarray       # H x W x 3 float32: the background colour under every pixel
    match: np.ndarray          # H x W float32: how far every pixel is from that background (RGB distance)
    border: np.ndarray         # match at the opaque border samples (the noise estimate for the tolerance)
    two_tone: object           # checker: H x W bool, True where the nearer checker colour is the lighter one
    report: dict


def _border_samples(h, w, ring):
    mask = np.zeros((h, w), bool)
    mask[:ring] = mask[-ring:] = True
    mask[:, :ring] = mask[:, -ring:] = True
    return np.nonzero(mask)


def _flat_background(rgb, ys, xs):
    """A plain or gently graded background: a robust quadratic surface fitted to the border."""
    h, w = rgb.shape[:2]
    samples = rgb[ys, xs]
    u = xs / max(w - 1, 1) - 0.5
    v = ys / max(h - 1, 1) - 0.5
    design = np.stack([np.ones_like(u), u, v, u * u, u * v, v * v], axis=1)
    keep = np.ones(len(samples), bool)
    for _ in range(6):    # refit without the outliers (a part touching the border, a stray mark)
        coef = np.linalg.lstsq(design[keep], samples[keep], rcond=None)[0]
        resid = _norm(design @ coef - samples)
        limit = max(3.0 * 1.4826 * float(np.median(resid[keep])), 4.0)
        new_keep = resid <= limit
        if new_keep.sum() < 16 or np.array_equal(new_keep, keep):
            break
        keep = new_keep
    c = coef.astype(np.float32)
    gu = (np.arange(w, dtype=np.float32) / max(w - 1, 1) - 0.5)[None, :, None]
    gv = (np.arange(h, dtype=np.float32) / max(h - 1, 1) - 0.5)[:, None, None]
    expected = np.clip(c[0] + gu * c[1] + gv * c[2] + gu * gu * c[3] + gu * gv * c[4] + gv * gv * c[5], 0, 255)
    expected = expected.astype(np.float32)
    corners = expected[[0, 0, -1, -1], [0, -1, 0, -1]]
    report = dict(mode='flat', colour=_hex(expected[h // 2, w // 2]), corners=[_hex(x) for x in corners],
                  border_fit=_r(keep.mean(), 3))
    return Background('flat', expected, _norm(rgb - expected), resid, None, report)


def _colour_background(rgb, ys, xs, colour):
    c = np.array(colour, np.float32)
    match = _norm(rgb - c)
    expected = np.broadcast_to(c, rgb.shape)
    return Background('colour', expected, match, match[ys, xs], None, dict(mode='colour', colour=_hex(c)))


def _interior_runs(tone):
    """Lengths of the runs of one checker tone along a line, leaving out partial and interrupted runs."""
    change = np.flatnonzero(np.diff(tone)) + 1
    starts = np.r_[0, change]
    lengths = np.r_[change, tone.size] - starts
    ok = tone[starts] >= 0
    interior = np.zeros(len(starts), bool)
    if len(starts) > 2:
        interior[1:-1] = ok[1:-1] & ok[:-2] & ok[2:]
    return lengths[interior]


def _best_phase(lines, cell):
    best, best_score = 0, -1.0
    for offset in range(int(math.ceil(cell))):
        score = 0.0
        for tone in lines:
            valid = tone >= 0
            if valid.any():
                pattern = (np.floor((np.arange(tone.size) + offset) / cell).astype(np.int64) & 1)[valid]
                agree = float(np.mean(pattern == tone[valid]))
                score += max(agree, 1.0 - agree) * valid.sum()
        if score > best_score:
            best, best_score = offset, score
    return best


def _segment(rgb, a, b):
    """Distance of every pixel to the colour segment a-b, the projection onto it and its position t (0..1)."""
    ab = (b - a).astype(np.float32)
    t = np.clip(((rgb - a) @ ab) / float(ab @ ab), 0.0, 1.0)
    projection = a + t[..., None] * ab
    return _norm(rgb - projection), projection.astype(np.float32), t


def _checker_background(rgb, ys, xs, ring):
    """A painted 'transparency' checkerboard: two tones alternating along the border with a steady period."""
    samples = rgb[ys, xs]
    luma = samples @ LUMA
    split = float(np.median(luma))
    if not (luma <= split).any() or not (luma > split).any():
        return None
    a, b = np.median(samples[luma <= split], 0), np.median(samples[luma > split], 0)
    for _ in range(8):
        nearer_b = _norm(samples - b) < _norm(samples - a)
        if nearer_b.all() or not nearer_b.any():
            return None
        a, b = np.median(samples[~nearer_b], 0), np.median(samples[nearer_b], 0)
    a, b = a.astype(np.float32), b.astype(np.float32)
    da, db = _norm(samples - a), _norm(samples - b)
    nearer_b = db < da
    separation = float(_norm(a - b))
    if separation < 15:
        return None                      # two tones that close are noise or a gradient on a plain background
    reach = max(6.0, 0.25 * separation)
    inlier = np.minimum(da, db) <= reach
    if inlier.mean() < 0.75 or min(np.mean(inlier & nearer_b), np.mean(inlier & ~nearer_b)) < 0.2:
        return None
    h, w = rgb.shape[:2]
    r = max(1, ring // 2)

    def tones(line):
        la, lb = _norm(line - a), _norm(line - b)
        tone = (lb < la).astype(np.int8)
        tone[np.minimum(la, lb) > reach] = -1
        return tone

    lines_x = [tones(rgb[r, :]), tones(rgb[h - 1 - r, :])]
    lines_y = [tones(rgb[:, r]), tones(rgb[:, w - 1 - r])]
    lengths = np.concatenate([_interior_runs(t) for t in lines_x + lines_y])
    if lengths.size < 6:
        return None                      # two tones but no alternation: a two-tone gradient, not a checker
    typical = float(np.median(lengths))
    steady = np.abs(lengths - typical) <= max(1.5, 0.25 * typical)
    if typical < 4 or steady.mean() < 0.6:
        return None                      # painted checkers have cells of 8-32 px; 1-3 px runs are noise

    cell = float(lengths[steady].mean())
    ox, oy = _best_phase(lines_x, cell), _best_phase(lines_y, cell)
    px = np.floor((np.arange(w) + ox) / cell).astype(np.int64)
    py = np.floor((np.arange(h) + oy) / cell).astype(np.int64)
    parity = ((py[:, None] + px[None, :]) & 1).astype(bool)
    agree = float(np.mean(parity[ys, xs][inlier] == nearer_b[inlier]))
    fit = max(agree, 1.0 - agree)
    segment_distance, projection, t = _segment(rgb, a, b)
    if fit >= 0.9:
        # Regular grid: each pixel is compared with the one tone it should have. Within 1.5 px of a cell edge
        # either tone (or a blend of the two) passes, since a painted checker is never registered exactly.
        lighter = parity ^ (agree < 0.5)
        expected = np.where(lighter[..., None], b, a).astype(np.float32)
        fx, fy = (np.arange(w) + ox) % cell, (np.arange(h) + oy) % cell
        near_edge = ((fy < 1.5) | (fy > cell - 1.5))[:, None] | ((fx < 1.5) | (fx > cell - 1.5))[None, :]
        match = _norm(rgb - expected)
        match = np.where(near_edge, np.minimum(match, segment_distance), match)
        model = 'grid'
    else:
        expected, match, model = projection, segment_distance, 'either tone'   # irregular painted checker
    report = dict(mode='checker', colours=[_hex(a), _hex(b)], cell=_r(cell), phase=[ox, oy],
                  pattern_fit=_r(fit, 3), model=model)
    return Background('checker', expected, match.astype(np.float32), match[ys, xs], t >= 0.5, report)


def _auto_tolerance(residual):
    residual = residual[np.isfinite(residual)]
    if residual.size == 0:
        return 16.0
    limit = max(3.0 * 1.4826 * float(np.median(residual)), 6.0)
    kept = residual[residual <= limit]
    p95 = float(np.percentile(kept, 95)) if kept.size else 0.0
    return float(np.clip(1.5 * p95 + 6.0, 8.0, 48.0))


def _defringe(rgb, background, model, tolerance, band):
    """Unmix the keyed edge: each foreground pixel within `band` px of the background is split into the colour
    of the most foreground-like pixel near it and a coverage alpha, so no white or grey halo is left."""
    fg = ~background
    alpha = np.where(fg, 255, 0).astype(np.uint8)
    colour = rgb.copy()
    if band <= 0 or not fg.any():
        return alpha, colour
    inner = ndimage.binary_erosion(fg, structure=EIGHT, iterations=band, border_value=1)
    eys, exs = np.nonzero(fg & ~inner)
    if eys.size == 0:
        return alpha, colour
    h, w = fg.shape
    best = np.full(eys.size, -1.0, np.float32)
    by, bx = eys.copy(), exs.copy()
    reach = band + 1
    for dy in range(-reach, reach + 1):
        yy = np.clip(eys + dy, 0, h - 1)
        for dx in range(-reach, reach + 1):
            xx = np.clip(exs + dx, 0, w - 1)
            far = np.where(fg[yy, xx], model.match[yy, xx], -1.0)
            better = far > best
            best[better], by[better], bx[better] = far[better], yy[better], xx[better]
    pixel = rgb[eys, exs].astype(np.float32)
    back = np.asarray(model.expected[eys, exs], np.float32)
    fore = rgb[by, bx].astype(np.float32)
    span = fore - back
    span2 = np.einsum('ij,ij->i', span, span)
    a = np.clip(np.einsum('ij,ij->i', pixel - back, span) / np.maximum(span2, 1e-6), 0.0, 1.0)
    a[span2 < (2.0 * tolerance) ** 2] = 1.0       # foreground too close to the background to unmix: keep it
    unmixed = back + (pixel - back) / np.maximum(a, 1e-3)[:, None]
    unmixed = np.where(a[:, None] >= 0.2, unmixed, fore)
    colour[eys, exs] = np.clip(np.rint(unmixed), 0, 255).astype(np.uint8)
    alpha[eys, exs] = np.rint(a * 255).astype(np.uint8)
    return alpha, colour


def key_background(rgba, key='auto', tolerance=None, holes=True, fringe=2):
    """Make a painted background transparent. Returns (new RGBA array, report dict).

    auto: a mostly transparent border means the sheet is already transparent (nothing is keyed). Otherwise the
    border decides between a checkerboard and a flat/graded colour; the background is flood-filled in from the
    border within the tolerance, and enclosed pockets that clearly show the background are keyed too.
    """
    h, w = rgba.shape[:2]
    alpha = rgba[..., 3]
    if key == 'none':
        return rgba.copy(), dict(mode='none')
    ring = max(2, min(h, w) // 256)
    ys, xs = _border_samples(h, w, ring)
    if key == 'auto' and np.mean(alpha[ys, xs] < 16) >= 0.5:
        return rgba.copy(), dict(mode='transparent')
    opaque = alpha[ys, xs] >= 128
    ys, xs = ys[opaque], xs[opaque]
    if key == 'auto' and ys.size < 16:
        return rgba.copy(), dict(mode='transparent')
    rgb = rgba[..., :3].astype(np.float32)
    if isinstance(key, tuple):
        model = _colour_background(rgb, ys, xs, key)
    else:
        model = _checker_background(rgb, ys, xs, ring) or _flat_background(rgb, ys, xs)
    tol = float(tolerance) if tolerance is not None else _auto_tolerance(model.border)
    labels, count = ndimage.label((model.match <= tol) | (alpha < 16), structure=FOUR)
    is_bg = np.zeros(count + 1, bool)
    is_bg[np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]])] = True
    is_bg[0] = False
    pockets_keyed = 0
    if holes and count:
        areas = np.bincount(labels.ravel(), minlength=count + 1)
        pockets = np.flatnonzero((areas >= max(64, h * w // 6000)) & ~is_bg)
        pockets = pockets[pockets > 0]
        if pockets.size:
            inside = np.isin(labels, pockets)
            sub_labels, sub_match = labels[inside], model.match[inside]
            sure = np.atleast_1d(ndimage.median(sub_match, sub_labels, pockets)) <= 0.5 * tol
            if model.two_tone is not None:    # a checker pocket must show both tones
                share = np.atleast_1d(ndimage.mean(model.two_tone[inside], sub_labels, pockets))
                sure &= (share >= 0.15) & (share <= 0.85)
            is_bg[pockets[sure]] = True
            pockets_keyed = int(sure.sum())
    background = is_bg[labels]
    key_alpha, colour = _defringe(rgba[..., :3], background, model, tol, fringe)
    out = rgba.copy()
    out[..., :3] = colour
    out[..., 3] = np.minimum(alpha, key_alpha)
    report = dict(model.report, tolerance=_r(tol, 1), pockets_keyed=pockets_keyed,
                  keyed_share=_r(background.mean(), 3))
    if report.get('border_fit', 1.0) < 0.5:
        report['note'] = 'the border is not one colour: check the keying in preview.png'
    return out, report


# ------------------------------------------------------------------------------------------ pieces and specks

@dataclass
class Pieces:
    owner: np.ndarray          # int32 H x W: 0 = nothing, k = piece k (its solid pixels and the soft edge)
    hard: np.ndarray           # bool H x W: alpha >= threshold and kept
    count: int
    merged: int
    dropped: int
    dropped_mask: np.ndarray   # bool H x W: solid specks that were dropped (magenta in the preview)


def find_pieces(alpha, threshold, min_area, merge_dist, soft_reach=3.0):
    """Connected blobs of solid pixels. Blobs below min_area are specks: one within merge_dist of a large blob
    joins it, others are dropped. Soft pixels (0 < alpha < threshold) join the blob within soft_reach px, so
    anti-aliased edges stay and faint shadows farther out go."""
    hard = alpha >= threshold
    labels, n = ndimage.label(hard, structure=EIGHT)
    areas = np.bincount(labels.ravel(), minlength=n + 1)
    areas[0] = 0
    big = np.flatnonzero(areas >= min_area)
    small = np.flatnonzero((areas > 0) & (areas < min_area))
    remap = np.zeros(n + 1, np.int32)
    remap[big] = np.arange(1, big.size + 1, dtype=np.int32)
    merged = 0
    if big.size and small.size:
        distance, (iy, ix) = ndimage.distance_transform_edt(remap[labels] == 0, return_indices=True)
        nearest = np.atleast_1d(ndimage.minimum(distance, labels, small))
        where = ndimage.minimum_position(distance, labels, small)
        for label, gap, (py, px) in zip(small, nearest, where):
            if gap <= merge_dist:
                remap[label] = remap[labels[iy[py, px], ix[py, px]]]
                merged += 1
    owner = remap[labels]
    dropped_mask = hard & (owner == 0)
    hard &= owner > 0
    soft = (alpha > 0) & (owner == 0) & ~dropped_mask
    if big.size and soft.any():
        distance, (iy, ix) = ndimage.distance_transform_edt(owner == 0, return_indices=True)
        take = soft & (distance <= soft_reach)
        owner[take] = owner[iy[take], ix[take]]
    return Pieces(owner, hard, int(big.size), merged, int(small.size) - merged, dropped_mask)


def _settings(args, h, w):
    return dict(key=_key_text(args.key), alpha_threshold=args.alpha_threshold,
                min_area=args.min_area if args.min_area is not None else max(64, round(0.0002 * h * w)),
                merge_dist=args.merge_dist if args.merge_dist is not None else _r(max(6.0, 0.006 * max(h, w)), 1),
                pad=args.pad, fringe=args.fringe, holes=args.holes)


# ------------------------------------------------------------------------------------------------------- parts

def _end_points(solid, x0, y0):
    """Top-centre and bottom-centre of a part: the mean x of its top and bottom few rows."""
    rows = np.flatnonzero(solid.any(axis=1))
    top, bottom = int(rows[0]), int(rows[-1])
    band = max(1, int(round(0.04 * (bottom - top + 1))))
    top_x = np.nonzero(solid[top:top + band])[1].mean() + 0.5
    bottom_x = np.nonzero(solid[bottom - band + 1:bottom + 1])[1].mean() + 0.5
    return (x0 + top_x, y0 + top), (x0 + bottom_x, y0 + bottom + 1)


def _principal_axis(solid, x0, y0):
    """Long axis of a part (for limbs painted at an angle): tilt from vertical and its two ends, upper first."""
    yy, xx = np.nonzero(solid)
    if yy.size < 3:
        return dict(angle_deg=0.0, elongation=1.0, ends=[[x0, y0], [x0, y0]])
    step = yy.size // 200000 + 1
    pts = np.stack([xx[::step] + 0.5 + x0, yy[::step] + 0.5 + y0]).astype(np.float64)
    mean = pts.mean(axis=1)
    values, vectors = np.linalg.eigh(np.cov(pts))
    vx, vy = vectors[:, 1]
    if vy < 0 or (vy == 0 and vx < 0):
        vx, vy = -vx, -vy               # point down the part, so the first end is the upper one
    proj = (pts[0] - mean[0]) * vx + (pts[1] - mean[1]) * vy
    lo, hi = np.percentile(proj, [1, 99])
    ends = [[mean[0] + lo * vx, mean[1] + lo * vy], [mean[0] + hi * vx, mean[1] + hi * vy]]
    return dict(angle_deg=_r(math.degrees(math.atan2(vx, vy)), 1),
                elongation=_r(math.sqrt(values[1] / max(values[0], 1e-9))), ends=ends)


def _describe_parts(rgba, pieces, pad):
    ids = np.arange(1, pieces.count + 1)
    slices = ndimage.find_objects(pieces.owner)
    areas = ndimage.sum_labels(pieces.hard, pieces.owner, ids)
    centres = ndimage.center_of_mass(rgba[..., 3].astype(np.float32), pieces.owner, ids)
    parts = []
    for k, sl, area, (cy, cx) in zip(ids, slices, areas, centres):
        x0, y0, x1, y1 = sl[1].start, sl[0].start, sl[1].stop, sl[0].stop
        own = pieces.owner[sl] == k
        crop = np.zeros((y1 - y0 + 2 * pad, x1 - x0 + 2 * pad, 4), np.uint8)
        crop[pad:pad + y1 - y0, pad:pad + x1 - x0][own] = rgba[sl][own]
        parts.append(dict(owner=int(k), bbox=[int(x0), int(y0), int(x1), int(y1)], image=crop,
                          origin=(x0 - pad, y0 - pad), area=int(area), centroid=(cx + 0.5, cy + 0.5),
                          ends=_end_points(own & pieces.hard[sl], x0, y0),
                          axis=_principal_axis(own & pieces.hard[sl], x0, y0)))
    return parts


def order_rows(boxes):
    """Reading order: rows top to bottom, left to right inside a row. A box joins the current row when it
    overlaps the row's mean vertical band by at least half of the smaller height."""
    rows = []
    for i in sorted(range(len(boxes)), key=lambda j: (boxes[j][1] + boxes[j][3]) / 2):
        x0, y0, x1, y1 = boxes[i]
        if rows:
            row = rows[-1]
            top = float(np.mean([boxes[j][1] for j in row]))
            bottom = float(np.mean([boxes[j][3] for j in row]))
            if min(bottom, y1) - max(top, y0) >= 0.5 * min(bottom - top, y1 - y0):
                row.append(i)
                continue
        rows.append([i])
    return [sorted(row, key=lambda j: (boxes[j][0] + boxes[j][2]) / 2) for row in rows]


def read_names(path, count):
    """Names in index order, one per line, or 'index: name' lines. Returns (names or None per part, warnings)."""
    lines = [line.split('#', 1)[0].strip() for line in Path(path).read_text(encoding='utf-8-sig').splitlines()]
    lines = [line for line in lines if line]
    pairs = [re.fullmatch(r'(\d+)\s*[:=]\s*(.+)', line) for line in lines]
    names, warnings = [None] * count, []
    if lines and all(pairs):
        for pair in pairs:
            index = int(pair.group(1))
            if index < count:
                names[index] = pair.group(2).strip()
            else:
                warnings.append(f'names: there is no part {index} (the sheet has {count})')
    elif any(pairs):
        raise SheetError(f'{path} mixes "index: name" lines with plain lines; use one style')
    else:
        for index, name in enumerate(lines[:count]):
            names[index] = name
        if len(lines) != count:
            warnings.append(f'names: {len(lines)} names for {count} parts')
    return names, warnings


def _slug(name):
    return re.sub(r'[^a-z0-9]+', '_', name.lower()).strip('_') or 'part'


def _remove_stale(out_dir, kind, keep):
    """Delete the PNGs a previous run of this tool listed in its own manifest and this run did not rewrite."""
    manifest = out_dir / ('manifest.json' if kind == 'parts' else 'frames.json')
    try:
        old = json.loads(manifest.read_text(encoding='utf-8'))
    except (OSError, ValueError):
        return
    if old.get('tool') != TOOL or old.get('kind') != kind:
        return
    for entry in old.get('parts', old.get('frames', [])):
        name = Path(str(entry.get('file', ''))).name
        if name.endswith('.png') and name not in keep and (out_dir / name).is_file():
            (out_dir / name).unlink()


def run_parts(args):
    rgba = load_rgba(args.sheet)
    h, w = rgba.shape[:2]
    settings = _settings(args, h, w)
    keyed, key_report = key_background(rgba, args.key, args.tolerance, args.holes, args.fringe)
    settings['tolerance'] = key_report.get('tolerance')
    pieces = find_pieces(keyed[..., 3], args.alpha_threshold, settings['min_area'], settings['merge_dist'])
    if pieces.count == 0:
        raise SheetError(f'no part reaches --min-area {settings["min_area"]} px (key: {key_report["mode"]})')
    found = _describe_parts(keyed, pieces, args.pad)
    rows = order_rows([part['bbox'] for part in found])
    ordered = []
    for row_index, row in enumerate(rows):
        for i in row:
            found[i]['row'] = row_index
            ordered.append(found[i])
    names, warnings = read_names(args.names, len(ordered)) if args.names else ([None] * len(ordered), [])
    out_dir = Path(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    entries, used = [], set()
    for index, (part, name) in enumerate(zip(ordered, names)):
        stem = f'{index:02d}_{_slug(name)}' if name else f'part_{index:02d}'
        while stem + '.png' in used:
            stem += '_b'
        used.add(stem + '.png')
        save_png(part['image'], out_dir / (stem + '.png'))
        ox, oy = part['origin']
        cw, ch = part['image'].shape[1], part['image'].shape[0]

        def point(p):
            lx, ly = p[0] - ox, p[1] - oy
            return dict(sheet=[_r(p[0], 1), _r(p[1], 1)], local=[_r(lx, 1), _r(ly, 1)],
                        pivot=[_r(lx / cw, 4), _r(1 - ly / ch, 4)])

        axis = part['axis']
        entries.append(dict(
            index=index, name=name or f'part_{index:02d}', file=stem + '.png', row=part['row'],
            bbox=part['bbox'], size=[cw, ch], crop_origin=[int(ox), int(oy)], area=part['area'],
            centroid=[_r(part['centroid'][0], 1), _r(part['centroid'][1], 1)],
            joints=dict(top=point(part['ends'][0]), bottom=point(part['ends'][1])),
            axis=dict(angle_deg=axis['angle_deg'], elongation=axis['elongation'],
                      ends_local=[[_r(x - ox, 1), _r(y - oy, 1)] for x, y in axis['ends']]),
            owner=part['owner']))
    _remove_stale(out_dir, 'parts', used)
    manifest = dict(tool=TOOL, version=VERSION, kind='parts', source=str(args.sheet),
                    source_sha256=file_sha256(args.sheet), sheet_size=[w, h], coordinates=COORDINATES,
                    key=key_report, settings=settings,
                    specks=dict(merged=pieces.merged, dropped=pieces.dropped,
                                dropped_pixels=int(pieces.dropped_mask.sum())),
                    warnings=warnings, parts=[{k: v for k, v in e.items() if k != 'owner'} for e in entries])
    (out_dir / 'manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    header = (f'{len(entries)} parts | key {_key_summary(key_report)} | green = top joint, orange = bottom '
              f'joint, magenta = dropped specks')
    _parts_preview(keyed, pieces, entries, header, out_dir / 'preview.png', named=bool(args.names))
    print(f'key: {_key_summary(key_report)}')
    print(f'parts: {len(entries)} in {len(rows)} rows; specks merged {pieces.merged}, dropped {pieces.dropped} '
          f'(min-area {settings["min_area"]} px, merge-dist {settings["merge_dist"]} px)')
    print(' idx  name               bbox x0,y0,x1,y1        png        area     top joint     bottom joint')
    for e in entries:
        top, bottom = e['joints']['top']['sheet'], e['joints']['bottom']['sheet']
        print(f' {e["index"]:>3}  {e["name"][:18]:<18} {",".join(map(str, e["bbox"])):<23} '
              f'{e["size"][0]:>4}x{e["size"][1]:<5} {e["area"]:>8}  {top[0]:>6.1f},{top[1]:<6.1f} '
              f'{bottom[0]:>6.1f},{bottom[1]:.1f}')
    for warning in warnings:
        print(f'warning: {warning}')
    print(f'wrote {len(entries)} PNGs, manifest.json, preview.png -> {out_dir}')
    return 0


def _key_summary(report):
    mode = report['mode']
    if mode in ('transparent', 'none'):
        return mode
    colours = ' '.join(report.get('colours', [report.get('colour', '')]))
    extra = f' cell {report["cell"]} ({report["model"]})' if mode == 'checker' else ''
    return f'{mode} {colours}{extra}, tolerance {report["tolerance"]}'


# ----------------------------------------------------------------------------------------------------- preview

def _font(size):
    try:
        return ImageFont.load_default(size=size)
    except Exception:          # Pillow without FreeType: the small bitmap font
        return ImageFont.load_default()


def _text(draw, bounds, xy, text, font, fill, centre=False):
    stroke = max(1, round(getattr(font, 'size', 11) / 14))
    left, top, right, bottom = draw.textbbox((0, 0), text, font=font, stroke_width=stroke)
    x, y = xy
    if centre:
        x, y = x - (left + right) / 2, y - (top + bottom) / 2
    x = min(max(x, -left), bounds[0] - right)
    y = min(max(y, -top), bounds[1] - bottom)
    draw.text((x, y), text, font=font, fill=fill, stroke_width=stroke, stroke_fill=(0, 0, 0))


def _backdrop(w, h, cell=16):
    """A mid-grey checker: light and dark paint both read on it, and keyed holes show as holes."""
    tone = ((np.arange(h)[:, None] // cell + np.arange(w)[None, :] // cell) & 1).astype(bool)
    rgba = np.where(tone[..., None], np.array([118, 122, 130, 255], np.uint8), np.array([92, 96, 104, 255], np.uint8))
    return Image.fromarray(rgba.astype(np.uint8))


def _parts_preview(rgba, pieces, entries, header, path, named):
    h, w = rgba.shape[:2]
    image = _backdrop(w, h)
    image.alpha_composite(Image.fromarray(rgba))
    canvas = np.array(image)
    canvas[pieces.dropped_mask] = DROPPED + (255,)
    lut = np.zeros((pieces.count + 1, 3), np.uint8)
    for e in entries:
        lut[e['owner']] = PALETTE[e['index'] % len(PALETTE)]
    owner = pieces.owner
    edge = np.zeros(owner.shape, bool)
    vertical, horizontal = owner[1:] != owner[:-1], owner[:, 1:] != owner[:, :-1]
    edge[1:] |= vertical
    edge[:-1] |= vertical
    edge[:, 1:] |= horizontal
    edge[:, :-1] |= horizontal
    ids = np.where(edge & (owner > 0), owner, 0)
    thick = max(1, round(max(h, w) / 900))
    if thick > 1:
        ids = ndimage.maximum_filter(ids, size=2 * thick - 1)
    line = ids > 0
    canvas[line, :3] = lut[ids[line]]
    canvas[line, 3] = 255
    out = Image.fromarray(canvas)
    draw = ImageDraw.Draw(out)
    big, small = _font(max(18, round(min(h, w) / 20))), _font(max(12, round(min(h, w) / 60)))
    size = getattr(big, 'size', 11)
    dot = max(3, 3 * thick)
    specks, _ = ndimage.label(pieces.dropped_mask, structure=EIGHT)
    for sl in ndimage.find_objects(specks)[:2000]:        # ring every dropped speck so it can be found
        sx, sy, r = (sl[1].start + sl[1].stop) / 2, (sl[0].start + sl[0].stop) / 2, 4 * thick + 4
        draw.ellipse([sx - r, sy - r, sx + r, sy + r], outline=DROPPED, width=thick)
    for e in entries:
        colour = PALETTE[e['index'] % len(PALETTE)]
        x0, y0, x1, y1 = e['bbox']
        draw.rectangle([x0, y0, x1 - 1, y1 - 1], outline=colour, width=thick)
        for joint, fill in (('top', TOP_JOINT), ('bottom', BOTTOM_JOINT)):
            jx, jy = e['joints'][joint]['sheet']
            draw.ellipse([jx - dot, jy - dot, jx + dot, jy + dot], fill=fill, outline=(0, 0, 0))
        cx, cy = e['centroid']
        if min(x1 - x0, y1 - y0) < 1.5 * size:      # too small to carry its label: write it above (or below)
            cx = (x0 + x1) / 2
            cy = y0 - 0.7 * size if y0 >= 1.4 * size else y1 + 0.7 * size
        _text(draw, (w, h), (cx, cy), str(e['index']), big, colour, centre=True)
        if named:
            _text(draw, (w, h), (cx, cy + 0.8 * size), e['name'], small, (255, 255, 255), centre=True)
    _text(draw, (w, h), (8, 6), header, small, (255, 255, 255))
    out.convert('RGB').save(path)


# ------------------------------------------------------------------------------------------------------ frames

def find_cells(occupancy, count, equal=False):
    """Cut positions between N frames (pixel-edge x) and how each was found: 'gap' (an empty column run),
    'valley' (the emptiest column near the equal-width position: frames touch there) or 'equal'."""
    width = occupancy.size
    if count <= 1:
        return [], []
    if equal:
        return [k * width / count for k in range(1, count)], ['equal'] * (count - 1)
    filled = np.flatnonzero(occupancy > 0)
    c0, c1 = int(filled[0]), int(filled[-1]) + 1
    gaps = [(s + c0, e + c0) for s, e in _true_runs(occupancy[c0:c1] == 0)]
    if len(gaps) == count - 1:
        return [(s + e) / 2 for s, e in gaps], ['gap'] * (count - 1)
    cell = (c1 - c0) / count
    smooth = ndimage.uniform_filter1d(occupancy.astype(np.float32), size=5, mode='nearest')
    bounds, methods, used = [], [], set()
    for k in range(1, count):
        target = c0 + k * cell
        lo, hi = target - 0.4 * cell, target + 0.4 * cell
        options = [g for g in gaps if lo <= (g[0] + g[1]) / 2 <= hi and g not in used]
        if options:
            gap = max(options, key=lambda g: (g[1] - g[0], -abs((g[0] + g[1]) / 2 - target)))
            used.add(gap)
            bounds.append((gap[0] + gap[1]) / 2)
            methods.append('gap')
        else:
            a, b = max(int(lo), 0), min(int(math.ceil(hi)), width)
            window = smooth[a:b]
            lowest = np.flatnonzero(window <= window.min() + 1e-6) + a
            column = int(lowest[np.argmin(np.abs(lowest + 0.5 - target))])
            bounds.append(column + 0.5)
            methods.append('valley')
    if any(b <= a for a, b in zip(bounds, bounds[1:])):
        return [c0 + k * cell for k in range(1, count)], ['equal'] * (count - 1)
    return bounds, methods


def assign_cells(pieces, bounds):
    """Frame index of every owned pixel. A blob goes whole to the cell holding its centre of mass, so a cape
    tip that pokes past the cut stays with its frame; a blob that clearly spans a cut (frames touching) is
    split by column there."""
    cuts = np.asarray(bounds, np.float64)
    width = pieces.owner.shape[1]
    cell_map = np.full(pieces.owner.shape, -1, np.int16)
    if pieces.count == 0:
        return cell_map, []
    margin = 0.15 * float(np.median(np.diff(np.r_[0.0, cuts, float(width)])))
    ids = list(range(1, pieces.count + 1))
    centres = ndimage.center_of_mass(pieces.hard, pieces.owner, ids)
    warnings = []
    for k, sl, (_, cx) in zip(ids, ndimage.find_objects(pieces.owner), centres):
        mask = pieces.owner[sl] == k
        x0, x1 = sl[1].start, sl[1].stop
        view = cell_map[sl]
        crossed = [b for b in bounds if x0 < b - margin and x1 > b + margin]
        if crossed:
            columns = np.searchsorted(cuts, np.arange(x0, x1) + 0.5).astype(np.int16)
            view[mask] = np.broadcast_to(columns, mask.shape)[mask]
            warnings.append('frames touch at x = ' + ', '.join(f'{b:.0f}' for b in crossed) +
                            ': that blob was split by column; check those frames in preview.png')
        else:
            view[mask] = int(np.searchsorted(cuts, cx + 0.5))
    return cell_map, warnings


UPPER_BODY = 0.55    # share of a frame's height, from the top, that --anchor overlap matches (head, torso, arms)


def _standing_mask(frame, height):
    """A frame's upper-body pixels in a box `height` rows tall whose bottom row is the frame's floor row."""
    ys, xs = frame['solid']
    rows = ys - (frame['floor_row'] - height + 1)
    keep = (rows >= 0) & (rows < height) & (ys < frame['bbox'][1] + UPPER_BODY * frame['height'])
    mask = np.zeros((height, frame['width']), np.float32)
    mask[rows[keep], xs[keep] - frame['bbox'][0]] = 1.0
    return mask


def _overlap_dx(ref, frame):
    """The x shift that lays this frame's upper body over frame 0's best, both standing on their floor line.
    Only shared mass counts, so an arm thrown forward, a staff or a cast effect does not pull it, and the
    legs, which are what a walk cycle moves, are left out."""
    height = max(ref['height'], frame['height'])
    a, b = _standing_mask(ref, height), _standing_mask(frame, height)
    n = a.shape[1] + b.shape[1] - 1
    overlap = np.fft.irfft((np.fft.rfft(a, n, axis=1) * np.fft.rfft(b[:, ::-1], n, axis=1)).sum(axis=0), n)
    t = int(np.argmax(overlap)) - (b.shape[1] - 1)      # frame column c lands on frame-0 column c + t
    return ref['bbox'][0] + t - frame['bbox'][0]


def run_frames(args):
    if args.count < 1:
        raise SheetError('--count must be at least 1')
    rgba = load_rgba(args.sheet)
    h, w = rgba.shape[:2]
    settings = _settings(args, h, w)
    settings.update(align=args.align, anchor=args.anchor if args.align == 'feet' else None,
                    equal_cells=args.equal_cells)
    keyed, key_report = key_background(rgba, args.key, args.tolerance, args.holes, args.fringe)
    settings['tolerance'] = key_report.get('tolerance')
    pieces = find_pieces(keyed[..., 3], args.alpha_threshold, settings['min_area'], settings['merge_dist'])
    if pieces.count == 0:
        raise SheetError(f'nothing reaches --min-area {settings["min_area"]} px (key: {key_report["mode"]})')
    bounds, methods = find_cells(pieces.hard.sum(axis=0), args.count, args.equal_cells)
    cell_map, warnings = assign_cells(pieces, bounds)
    edges = [0.0] + [float(b) for b in bounds] + [float(w)]
    frames = []
    for f in range(args.count):
        own = cell_map == f
        ys, xs = np.nonzero(own & pieces.hard)
        if ys.size == 0:
            raise SheetError(f'frame {f} (x {edges[f]:.0f}-{edges[f + 1]:.0f}) is empty: is --count right?')
        top = int(ys.min())
        row_counts = np.bincount(ys - top)
        solid_rows = np.flatnonzero(row_counts >= max(2, round(0.01 * (xs.max() - xs.min() + 1))))
        floor_row = top + int(solid_rows[-1] if solid_rows.size else row_counts.size - 1)
        oy, ox = np.nonzero(own)
        frames.append(dict(index=f, cell=[_r(edges[f], 1), _r(edges[f + 1], 1)],
                           bbox=[int(xs.min()), top, int(xs.max()) + 1, int(ys.max()) + 1],
                           floor_row=floor_row, com_x=float(xs.mean()) + 0.5,
                           centre_x=(int(xs.min()) + int(xs.max()) + 1) / 2, width=int(xs.max() - xs.min() + 1),
                           height=floor_row + 1 - top, pixels=(oy, ox), solid=(ys, xs),
                           extent=(int(ox.min()), int(oy.min()), int(ox.max()) + 1, int(oy.max()) + 1)))
    ref = frames[0]
    for fr in frames:
        if args.align == 'feet':
            by_com = int(round(ref['com_x'] - fr['com_x']))
            by_overlap = _overlap_dx(ref, fr)
            fr['shift'] = [by_overlap if args.anchor == 'overlap' else by_com, ref['floor_row'] - fr['floor_row']]
            if abs(by_com - by_overlap) > max(3, 0.04 * ref['width']):
                warnings.append(f'frame {fr["index"]}: centre of mass and silhouette overlap disagree by '
                                f'{abs(by_com - by_overlap)} px (an arm, staff or effect pulls the centre of mass)'
                                + ('' if args.anchor == 'overlap' else '; --anchor overlap keeps the body still'))
        else:
            fr['shift'] = [-int(round(edges[fr['index']])), 0]    # keep each pose where it sat in its cell
    ux0 = min(fr['extent'][0] + fr['shift'][0] for fr in frames)
    uy0 = min(fr['extent'][1] + fr['shift'][1] for fr in frames)
    ux1 = max(fr['extent'][2] + fr['shift'][0] for fr in frames)
    uy1 = max(fr['extent'][3] + fr['shift'][1] for fr in frames)
    pad = args.pad
    origin_x, origin_y = ux0 - pad, uy0 - pad
    cw, ch = ux1 - ux0 + 2 * pad, uy1 - uy0 + 2 * pad
    out_dir = Path(args.out_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    canvases, entries = [], []
    for fr in frames:
        dx, dy = fr['shift']
        oy, ox = fr['pixels']
        canvas = np.zeros((ch, cw, 4), np.uint8)
        canvas[oy + dy - origin_y, ox + dx - origin_x] = keyed[oy, ox]
        name = f'frame_{fr["index"]:02d}.png'
        save_png(canvas, out_dir / name)
        canvases.append(canvas)
        floor_c, com_c = fr['floor_row'] + dy - origin_y, fr['com_x'] + dx - origin_x
        entries.append(dict(index=fr['index'], file=name, cell=fr['cell'], bbox=fr['bbox'],
                            floor_row=fr['floor_row'], com_x=_r(fr['com_x'], 1), centre_x=_r(fr['centre_x'], 1),
                            width=fr['width'], height=fr['height'], scale=_r(fr['height'] / ref['height'], 3),
                            shift=[dx, dy], canvas=dict(floor_row=int(floor_c), com_x=_r(com_c, 1),
                                                        pivot=[_r(com_c / cw, 4), _r(1 - (floor_c + 1) / ch, 4)])))
    for e in entries[1:]:
        if abs(e['scale'] - 1) > 0.03:
            warnings.append(f'frame {e["index"]} is {e["scale"]:.2f}x the height of frame 0 (scale drift, '
                            f'or a crouching/stretching pose)')
    cells = [dict(x=_r(b, 1), method=m) for b, m in zip(bounds, methods)]
    if 'valley' in methods or 'equal' in methods:
        warnings.append('not every cut fell in an empty column gap: check the cuts in preview.png')
    centres = [fr['com_x'] - (edges[fr['index']] + edges[fr['index'] + 1]) / 2 for fr in frames]
    drift = dict(floor_row_spread=max(fr['floor_row'] for fr in frames) - min(fr['floor_row'] for fr in frames),
                 com_from_cell_centre=[_r(c, 1) for c in centres],
                 height_ratio=[min(e['scale'] for e in entries), max(e['scale'] for e in entries)])
    _remove_stale(out_dir, 'frames', {e['file'] for e in entries})
    report = dict(tool=TOOL, version=VERSION, kind='frames', source=str(args.sheet),
                  source_sha256=file_sha256(args.sheet), sheet_size=[w, h], coordinates=COORDINATES,
                  count=args.count, align=args.align, canvas_size=[cw, ch], key=key_report, settings=settings,
                  cuts=cells, specks=dict(merged=pieces.merged, dropped=pieces.dropped), drift=drift,
                  warnings=warnings, frames=entries)
    (out_dir / 'frames.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    _frames_preview(keyed, frames, bounds, methods, canvases, entries, args.align, out_dir / 'preview.png')
    print(f'key: {_key_summary(key_report)}')
    print(f'cuts: {", ".join(f"{c["x"]:.0f} ({c["method"]})" for c in cells) or "none"}; '
          f'specks merged {pieces.merged}, dropped {pieces.dropped}')
    print(' frame  cell x          bbox x0,y0,x1,y1      floor   com_x  height  scale  shift')
    for e in entries:
        print(f' {e["index"]:>5}  {e["cell"][0]:>6.0f}-{e["cell"][1]:<6.0f} {",".join(map(str, e["bbox"])):<21} '
              f'{e["floor_row"]:>5} {e["com_x"]:>7.1f} {e["height"]:>7} {e["scale"]:>6.3f}  {e["shift"]}')
    if args.align == 'feet':
        print(f'canvas {cw}x{ch}: every frame stands on row {entries[0]["canvas"]["floor_row"]}, centre of mass '
              f'at x {entries[0]["canvas"]["com_x"]}')
    else:
        print(f'canvas {cw}x{ch}: frames kept where they sat in their cells (align none)')
    for warning in warnings:
        print(f'warning: {warning}')
    print(f'wrote {len(entries)} PNGs, frames.json, preview.png -> {out_dir}')
    return 0


def _frames_preview(keyed, frames, bounds, methods, canvases, entries, align, path):
    h, w = keyed.shape[:2]
    ch, cw = canvases[0].shape[:2]
    gap = max(4, cw // 40)
    tiles = len(canvases) + 1
    strip = Image.new('RGBA', (tiles * cw + (tiles - 1) * gap, ch), (30, 30, 34, 255))
    overlay = _backdrop(cw, ch)
    for i, canvas in enumerate(canvases):
        tile = _backdrop(cw, ch)
        tile.alpha_composite(Image.fromarray(canvas))
        strip.paste(tile, (i * (cw + gap), 0))
        ghost = canvas.copy()
        ghost[..., 3] = (ghost[..., 3].astype(np.float32) * 0.45).astype(np.uint8)
        overlay.alpha_composite(Image.fromarray(ghost))
    strip.paste(overlay, ((tiles - 1) * (cw + gap), 0))
    draw = ImageDraw.Draw(strip)
    width = max(1, round(ch / 300))
    font = _font(max(12, round(ch / 16)))
    floor_y = entries[0]['canvas']['floor_row'] + 1
    draw.line([(0, floor_y), (strip.width, floor_y)], fill=(255, 64, 64), width=width)
    for e in entries:
        x0 = e['index'] * (cw + gap)
        com_x = x0 + e['canvas']['com_x']
        own_floor = e['canvas']['floor_row'] + 1
        draw.line([(com_x, own_floor - ch * 0.1), (com_x, own_floor + ch * 0.03)], fill=(70, 200, 255), width=width)
        if align != 'feet':
            draw.line([(x0, own_floor), (x0 + cw, own_floor)], fill=(255, 150, 40), width=width)
        _text(draw, strip.size, (x0 + 4, 2), str(e['index']), font, (255, 255, 255))
    _text(draw, strip.size, ((tiles - 1) * (cw + gap) + 4, 2), 'overlay', font, (255, 255, 255))

    sheet = _backdrop(w, h)
    sheet.alpha_composite(Image.fromarray(keyed))
    sdraw = ImageDraw.Draw(sheet)
    line = max(1, round(w / max(strip.width, 1) * 2), round(w / 700))
    for b, m in zip(bounds, methods):
        sdraw.line([(b, 0), (b, h)], fill=(80, 255, 120) if m == 'gap' else (255, 150, 40), width=line)
    for fr in frames:
        x0, y0, x1, y1 = fr['bbox']
        sdraw.rectangle([x0, y0, x1 - 1, y1 - 1], outline=(70, 200, 255), width=line)
        sdraw.line([(x0 - 8, fr['floor_row'] + 1), (x1 + 8, fr['floor_row'] + 1)], fill=(255, 64, 64), width=line)
    scale = strip.width / w
    sheet = sheet.resize((strip.width, max(1, round(h * scale))), Image.Resampling.LANCZOS)
    final = Image.new('RGBA', (strip.width, sheet.height + gap + ch), (30, 30, 34, 255))
    final.paste(sheet, (0, 0))
    final.paste(strip, (0, sheet.height + gap))
    if final.width > 4096:
        final = final.resize((4096, round(final.height * 4096 / final.width)), Image.Resampling.LANCZOS)
    final.convert('RGB').save(path)


# --------------------------------------------------------------------------------------------------- self-test

# Synthetic parts in their expected reading order: (shape, geometry, colour, outlined).
_PARTS = [
    ('ellipse', (80, 60, 260, 250), (180, 90, 60), True),
    ('rect', (340, 50, 560, 290), (60, 80, 160), True),
    ('poly', ((650, 70), (900, 70), (950, 270), (620, 260)), (40, 40, 70), True),
    ('ellipse', (1000, 80, 1150, 240), (216, 207, 194), False),   # Ilya's ash-white robe, bare edge on the bg
    ('ellipse', (90, 360, 170, 560), (200, 160, 90), True),
    ('ring', ((260, 340, 480, 560), (320, 400, 420, 500)), (120, 40, 40), True),
    ('poly', ((560, 350), (760, 350), (760, 400), (610, 400), (610, 560), (560, 560)), (70, 130, 70), True),
    ('rect', (650, 450, 740, 540), (230, 200, 40), True),        # inside the L's bounding box
    ('poly', ((860, 330), (900, 330), (1120, 560), (1080, 570)), (150, 100, 200), True),   # a slanted limb
    ('ellipse', (100, 650, 230, 860), (90, 60, 40), True),
    ('rect', (330, 640, 430, 870), (50, 50, 50), True),
    ('ellipse', (560, 700, 640, 790), (220, 180, 150), True),
]
_NAMES = ['head', 'torso', 'cape', 'robe', 'upper arm', 'ring', 'l piece', 'square', 'forearm', 'thigh', 'shin',
          'hand']
_SS = 3   # supersampling for anti-aliased synthetic edges


def _part_box(geometry, shape):
    if shape == 'poly':
        xs, ys = [p[0] for p in geometry], [p[1] for p in geometry]
        return min(xs), min(ys), max(xs), max(ys)
    return geometry[0] if shape == 'ring' else geometry


def _synthetic_parts(size=(1200, 900)):
    w, h = size
    big = Image.new('RGBA', (w * _SS, h * _SS), (0, 0, 0, 0))
    draw = ImageDraw.Draw(big)

    def box(b):
        return [b[0] * _SS, b[1] * _SS, b[2] * _SS - 1, b[3] * _SS - 1]

    for shape, geometry, colour, outlined in _PARTS:
        fill = colour + (255,)
        line = tuple(int(c * 0.45) for c in colour) + (255,) if outlined else None
        width = 3 * _SS if outlined else 0
        if shape == 'ellipse':
            draw.ellipse(box(geometry), fill=fill, outline=line, width=width)
        elif shape == 'rect':
            draw.rounded_rectangle(box(geometry), radius=12 * _SS, fill=fill, outline=line, width=width)
        elif shape == 'poly':
            draw.polygon([(x * _SS, y * _SS) for x, y in geometry], fill=fill, outline=line, width=width)
        else:
            draw.ellipse(box(geometry[0]), fill=fill, outline=line, width=width)
            draw.ellipse(box(geometry[1]), fill=(0, 0, 0, 0), outline=line, width=width)
    sheet = np.array(big.resize(size, Image.Resampling.BOX))
    sheet[154:157, 264:267] = (180, 90, 60, 255)      # 3 px off the head's right edge: joins the head
    sheet[612:616, 600:604] = (30, 30, 30, 255)       # far from everything: dropped
    sheet[850:854, 1150:1154] = (30, 30, 30, 255)     # dropped
    return sheet


def _flatten(rgba, background):
    a = rgba[..., 3:4].astype(np.float32) / 255
    return np.clip(np.rint(rgba[..., :3] * a + background * (1 - a)), 0, 255).astype(np.uint8)


def _noise(rng, shape, amount):
    return rng.integers(-amount, amount + 1, size=shape).astype(np.float32)


def _figure(draw, cx, floor, s, legs, colour):
    """A stick-simple character standing on `floor` (its lowest solid row is floor - 1)."""
    def box(x0, y0, x1, y1):
        return [x0 * _SS, y0 * _SS, x1 * _SS - 1, y1 * _SS - 1]
    r = 28 * s
    draw.ellipse(box(cx - r, floor - 228 * s, cx + r, floor - 172 * s), fill=colour)
    draw.rectangle(box(cx - 35 * s, floor - 175 * s, cx + 35 * s, floor - 78 * s), fill=colour)
    for offset in legs:
        draw.rectangle(box(cx + offset * s - 9 * s, floor - 82 * s, cx + offset * s + 9 * s, floor), fill=colour)
    return box


_FRAME_WIDTHS = [300, 320, 290, 310, 300, 330]            # cells are not equal
_DRIFT_X, _DRIFT_Y = [0, 7, -5, 12, -9, 3], [0, -6, 4, -10, 8, 2]
_SCALES = [1.0, 1.0, 1.08, 1.0, 0.95, 1.0]
_LEGS = [(-14, 14), (-26, 8), (-10, 24), (-22, 20), (-12, 12), (-30, 18)]


def _synthetic_frames(rng):
    w, h = sum(_FRAME_WIDTHS), 520
    big = Image.new('RGBA', (w * _SS, h * _SS), (0, 0, 0, 0))
    draw = ImageDraw.Draw(big)
    colour = (40, 70, 150, 255)
    left, truth = 0, []
    for i, cell in enumerate(_FRAME_WIDTHS):
        cx, floor, s = left + cell / 2 + _DRIFT_X[i], 440 + _DRIFT_Y[i], _SCALES[i]
        box = _figure(draw, cx, floor, s, _LEGS[i], colour)
        if i == 2:     # an arm reaching past the equal-width cell line
            draw.rectangle(box(cx + 35 * s - 4, floor - 160 * s, cx + 35 * s + 140, floor - 148 * s), fill=colour)
        if i == 4:     # a floating orb: an empty column run inside the frame
            draw.ellipse(box(cx - 99, floor - 164, cx - 71, floor - 136), fill=colour)
        truth.append(dict(cx=cx, floor=floor))
        left += cell
    rgba = np.array(big.resize((w, h), Image.Resampling.BOX))
    background = np.full((h, w, 3), 200, np.float32) + _noise(rng, (h, w, 3), 3)
    return _flatten(rgba, np.clip(background, 0, 255)), truth


def _synthetic_touching():
    w, h = 800, 300
    big = Image.new('RGBA', (w * _SS, h * _SS), (0, 0, 0, 0))
    draw = ImageDraw.Draw(big)
    colour = (150, 60, 40, 255)
    for cx in (100, 300, 500, 700):
        _figure(draw, cx, 270, 0.8, (-12, 12), colour)
    cape = [(320, 140), (480, 210), (480, 216), (320, 160)]          # frame 1's cape touches frame 2
    draw.polygon([(x * _SS, y * _SS) for x, y in cape], fill=colour)
    return np.array(big.resize((w, h), Image.Resampling.BOX))


def self_test(keep=None):
    results = []

    def check(ok, what):
        results.append(bool(ok))
        print(('PASS  ' if ok else 'FAIL  ') + what)

    def run(*argv):
        with contextlib.redirect_stdout(io.StringIO()) as captured:
            code = main([str(a) for a in argv])
        if code != 0:
            raise AssertionError(f'{" ".join(map(str, argv))} exited {code}:\n{captured.getvalue()}')
        return captured.getvalue()

    def load(path):
        return json.loads(Path(path).read_text(encoding='utf-8'))

    started = time.perf_counter()
    holder = contextlib.nullcontext(str(keep)) if keep else tempfile.TemporaryDirectory(prefix='slice_sheet_')
    try:
        with holder as root_name:
            root = Path(root_name)
            root.mkdir(parents=True, exist_ok=True)
            rng = np.random.default_rng(7)
            _test_parts(root, rng, run, load, check)
            _test_frames(root, rng, run, load, check)
    except Exception:
        traceback.print_exc()
        results.append(False)
        print('FAIL  the self-test raised')
    passed = bool(results) and all(results)
    where = f' (sheets and outputs kept in {keep})' if keep else ''
    print(f'SELF-TEST {"PASSED" if passed else "FAILED"}: {sum(results)}/{len(results)} checks in '
          f'{time.perf_counter() - started:.1f} s{where}')
    return 0 if passed else 1


def _test_parts(root, rng, run, load, check):
    clean = _synthetic_parts()
    h, w = clean.shape[:2]
    shadow = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(shadow).ellipse([90, 862, 240, 890], fill=(0, 0, 0, 40))     # soft shadow under the thigh
    transparent = Image.alpha_composite(Image.fromarray(clean), shadow)
    transparent.save(root / 'parts_transparent.png')
    gradient = np.linspace(255, 244, h, dtype=np.float32)[:, None, None]
    white = _flatten(clean, np.clip(gradient + _noise(rng, (h, w, 3), 3), 0, 255))
    save_png(white, root / 'parts_white.png')
    tone = ((np.arange(h)[:, None] // 16 + np.arange(w)[None, :] // 16) & 1).astype(bool)
    checker_bg = np.where(tone[..., None], 204.0, 255.0) + _noise(rng, (h, w, 3), 2)
    save_png(_flatten(clean, np.clip(checker_bg, 0, 255)), root / 'parts_checker.png')
    (root / 'names.txt').write_text('# parts in preview order\n' + '\n'.join(_NAMES) + '\n', encoding='utf-8')
    (root / 'names_by_index.txt').write_text('0: head\n3: robe   # the ash-white one\n', encoding='utf-8')
    truth = [_part_box(g, s) for s, g, _, _ in _PARTS]

    run('parts', root / 'parts_transparent.png', root / 'out_parts_transparent')      # first pass: unnamed
    first = load(root / 'out_parts_transparent' / 'manifest.json')['parts']
    run('parts', root / 'parts_transparent.png', root / 'out_parts_transparent', '--names', root / 'names.txt')
    ref = load(root / 'out_parts_transparent' / 'manifest.json')
    parts = ref['parts']
    pngs = sorted(p.name for p in (root / 'out_parts_transparent').glob('*.png'))
    check(first[0]['file'] == 'part_00.png' and 'part_00.png' not in pngs and len(pngs) == len(parts) + 1,
          f'rerun with --names replaces the unnamed PNGs ({len(pngs)} PNGs incl. preview)')
    check(ref['key']['mode'] == 'transparent' and len(parts) == 12,
          f'transparent sheet: key {ref["key"]["mode"]}, {len(parts)} parts (want 12)')
    order_ok = len(parts) == 12 and all(
        abs((p['bbox'][0] + p['bbox'][2]) / 2 - (t[0] + t[2]) / 2) <= 6 and
        abs((p['bbox'][1] + p['bbox'][3]) / 2 - (t[1] + t[3]) / 2) <= 6 for p, t in zip(parts, truth))
    check(order_ok, 'parts come out in rows, top-to-bottom then left-to-right')
    check(ref['specks']['merged'] >= 1 and parts[0]['bbox'][2] >= 266,
          f'the speck beside the head joined it (merged {ref["specks"]["merged"]}, head x1 {parts[0]["bbox"][2]})')
    check(ref['specks']['dropped'] >= 2, f'far specks dropped ({ref["specks"]["dropped"]})')
    check(parts[9]['bbox'][3] <= 866, f'the soft shadow under the thigh is not part of it (y1 {parts[9]["bbox"][3]})')
    check([p['file'] for p in parts[:2]] == ['00_head.png', '01_torso.png'] and parts[4]['name'] == 'upper arm',
          'names.txt maps the order to names and file names')
    l_piece = load_rgba(root / 'out_parts_transparent' / parts[6]['file'])
    yellow = (l_piece[..., 3] > 0) & (_norm(l_piece[..., :3].astype(np.float32) - (230, 200, 40)) < 60)
    check(not yellow.any(), 'a part inside another part\'s bounding box does not leak into its crop')
    top, bottom = parts[4]['joints']['top']['sheet'], parts[4]['joints']['bottom']['sheet']
    check(abs(top[0] - 130) <= 3 and abs(top[1] - 360) <= 2 and abs(bottom[0] - 130) <= 3
          and abs(bottom[1] - 560) <= 2, f'joint hints of the upper arm: top {top}, bottom {bottom}')
    check(30 <= parts[8]['axis']['angle_deg'] <= 55, f'slanted forearm axis {parts[8]["axis"]["angle_deg"]} deg')

    reference_alpha = np.array(transparent)[..., 3] >= 128
    for label, sheet in (('white', 'parts_white.png'), ('checker', 'parts_checker.png')):
        out = root / f'out_parts_{label}'
        extra = ['--names', root / 'names_by_index.txt'] if label == 'white' else []
        run('parts', root / sheet, out, *extra)
        manifest = load(out / 'manifest.json')
        got = manifest['parts']
        want = 'flat' if label == 'white' else 'checker'
        check(manifest['key']['mode'] == want and len(got) == 12,
              f'{label} sheet: key {_key_summary(manifest["key"])}, {len(got)} parts (want {want}, 12)')
        same = len(got) == 12 and all(abs(a['bbox'][i] - b['bbox'][i]) <= 3 for a, b in zip(got, parts)
                                      for i in range(4))
        check(same, f'{label} sheet: the same parts in the same order as the transparent sheet')
        keyed, _ = key_background(load_rgba(root / sheet))
        solid = keyed[..., 3] >= 128
        iou = (solid & reference_alpha).sum() / max((solid | reference_alpha).sum(), 1)
        check(iou >= 0.985 and keyed[0, 0, 3] == 0 and keyed[-1, -1, 3] == 0,
              f'{label} sheet: keyed background is transparent (solid-pixel IoU {iou:.4f} with the true alpha)')
        if len(got) != 12:
            continue
        crops = [load_rgba(out / p['file']) for p in got]
        check(all(c[0, 0, 3] == 0 and c[-1, -1, 3] == 0 and c[0, -1, 3] == 0 and c[-1, 0, 3] == 0 for c in crops),
              f'{label} sheet: every crop has transparent corners')
        ring, (ox, oy) = crops[5], got[5]['crop_origin']
        check(ring[450 - oy, 370 - ox, 3] == 0, f'{label} sheet: the background inside the ring is keyed')
        robe = crops[3]
        centre = robe[160 - got[3]['crop_origin'][1], 1075 - got[3]['crop_origin'][0], :3].astype(int)
        check(got[3]['area'] >= 0.95 * parts[3]['area'] and np.abs(centre - (216, 207, 194)).max() <= 8,
              f'{label} sheet: the ash-white robe survives keying ({got[3]["area"]} of {parts[3]["area"]} px)')
        shin = crops[10]
        edge_luma = float(np.percentile(shin[shin[..., 3] > 0][:, :3].astype(np.float32) @ LUMA, 99))
        check(edge_luma < 140, f'{label} sheet: no light halo on the dark shin (99th percentile luma {edge_luma:.0f})')
        if label == 'white':
            check(got[3]['file'] == '03_robe.png' and got[1]['file'] == 'part_01.png',
                  '"index: name" lines name only the listed parts')


def _test_frames(root, rng, run, load, check):
    sheet, truth = _synthetic_frames(rng)
    save_png(sheet, root / 'frames_drift.png')
    run('frames', root / 'frames_drift.png', root / 'out_frames_feet', '--count', 6, '--align', 'feet')
    report = load(root / 'out_frames_feet' / 'frames.json')
    frames = report['frames']
    check(report['key']['mode'] == 'flat' and len(frames) == 6 and all(c['method'] == 'gap' for c in report['cuts']),
          f'frame sheet: key {report["key"]["mode"]}, 6 frames cut at gaps {[c["x"] for c in report["cuts"]]}')
    floors = [f['floor_row'] - frames[0]['floor_row'] for f in frames]
    check(floors == [d - _DRIFT_Y[0] for d in _DRIFT_Y], f'measured floor drift {floors} (drawn {_DRIFT_Y})')
    scales = [f['scale'] for f in frames]
    check(abs(scales[2] - 1.08) <= 0.02 and abs(scales[4] - 0.95) <= 0.02, f'measured scale drift {scales}')
    check(frames[2]['bbox'][2] >= 935 and frames[4]['bbox'][0] <= truth[4]['cx'] - 95,
          'the arm past the cell line and the floating orb stay with their own frames')
    images = [load_rgba(root / 'out_frames_feet' / f['file']) for f in frames]
    check(len({im.shape for im in images}) == 1,
          f'all frames share one canvas {images[0].shape[1]}x{images[0].shape[0]}')
    measured = []
    for im in images:
        ys, xs = np.nonzero(im[..., 3] >= 128)
        measured.append((int(ys.max()), xs.mean() + 0.5))
    floor_ok = all(m[0] == measured[0][0] for m in measured)
    com_ok = all(abs(m[1] - measured[0][1]) <= 1.0 for m in measured)
    check(floor_ok and com_ok, '--align feet: same floor row and centre of mass within 1 px: '
          + ', '.join(f'({m[0]}, {m[1]:.1f})' for m in measured))
    check(all(im[0, 0, 3] == 0 and im[-1, -1, 3] == 0 for im in images), 'frame corners are transparent (keyed)')
    pulled = sorted(int(w.split()[1].rstrip(':')) for w in report['warnings'] if 'disagree' in w)
    check(pulled == [2, 4], f'the centre of mass pulled by the arm and the orb is warned for frames {pulled}')
    run('frames', root / 'frames_drift.png', root / 'out_frames_overlap', '--count', 6, '--anchor', 'overlap')
    still = load(root / 'out_frames_overlap' / 'frames.json')['frames']
    bodies = [t['cx'] + f['shift'][0] for t, f in zip(truth, still)]
    floors_ok = len({f['canvas']['floor_row'] for f in still}) == 1
    check(floors_ok and max(bodies) - min(bodies) <= 1.0,
          '--anchor overlap: same floor row and body centres within 1 px: ' + ', '.join(f'{b:.1f}' for b in bodies))
    run('frames', root / 'frames_drift.png', root / 'out_frames_none', '--count', 6, '--align', 'none')
    loose = load(root / 'out_frames_none' / 'frames.json')['frames']
    kept = [f['canvas']['floor_row'] - loose[0]['canvas']['floor_row'] for f in loose]
    check(kept == floors, f'--align none keeps the drawn drift {kept}')

    save_png(_synthetic_touching(), root / 'frames_touching.png')
    run('frames', root / 'frames_touching.png', root / 'out_frames_touching', '--count', 4)
    touching = load(root / 'out_frames_touching' / 'frames.json')
    inside = all(f['cell'][0] <= f['com_x'] <= f['cell'][1] for f in touching['frames'])
    split = any('touch' in warning for warning in touching['warnings'])
    check(len(touching['frames']) == 4 and inside and split,
          f'touching frames: cuts {[(c["x"], c["method"]) for c in touching["cuts"]]}, split warned {split}')


# -------------------------------------------------------------------------------------------------------- main

def build_parser():
    parser = argparse.ArgumentParser(prog='slice_sheet.py', description=__doc__.split('\n\n')[0],
                                     formatter_class=argparse.RawDescriptionHelpFormatter,
                                     epilog='See the module docstring for the full usage.')
    parser.add_argument('--self-test', action='store_true', help='build synthetic sheets and check the results')
    parser.add_argument('--keep', type=Path, help='with --self-test: keep the sheets and outputs in this folder')
    sub = parser.add_subparsers(dest='command')

    def shared(command):
        command.add_argument('sheet', type=Path)
        command.add_argument('out_dir', type=Path)
        command.add_argument('--key', type=parse_key, default='auto', help='auto | none | #RRGGBB (default auto)')
        command.add_argument('--tolerance', type=float, help='background RGB distance (default: from the border)')
        command.add_argument('--alpha-threshold', type=int, default=128, help='solid pixels (default 128)')
        command.add_argument('--min-area', type=int, help='smaller blobs are specks (default 0.02%% of the sheet)')
        command.add_argument('--merge-dist', type=float, help='specks this close join a part (default 0.6%% of '
                                                               'the longer side, at least 6 px)')
        command.add_argument('--pad', type=int, default=8, help='transparent padding around each crop (default 8)')
        command.add_argument('--fringe', type=int, default=2, help='keyed edge px to unmix (default 2, 0 = off)')
        command.add_argument('--no-holes', dest='holes', action='store_false',
                             help='do not key background pockets enclosed by a part')

    parts = sub.add_parser('parts', help='cut a parts sheet into one PNG per part')
    shared(parts)
    parts.add_argument('--names', type=Path, help='names in preview order, or "index: name" lines')
    frames = sub.add_parser('frames', help='cut a frame sheet into N frames on one shared canvas')
    shared(frames)
    frames.add_argument('--count', type=int, required=True, help='number of frames on the sheet')
    frames.add_argument('--align', choices=['feet', 'none'], default='feet', help='default feet')
    frames.add_argument('--anchor', choices=['com', 'overlap'], default='com',
                        help='with --align feet: match the centre of mass (default) or the best silhouette overlap')
    frames.add_argument('--equal-cells', action='store_true', help='cut at equal widths instead of at the gaps')
    return parser


def main(argv=None):
    parser = build_parser()
    args = parser.parse_args(argv)
    if args.self_test:
        return self_test(args.keep)
    if not args.command:
        parser.print_help()
        return 2
    try:
        return run_parts(args) if args.command == 'parts' else run_frames(args)
    except (SheetError, OSError) as error:
        print(f'error: {error}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())
