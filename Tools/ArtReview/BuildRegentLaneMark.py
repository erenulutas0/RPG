"""The Glass Regent's lane mark (docs/50 section 6): the red-orange telegraph of its sight line, in the Forge Warden's
mark's format, as two 9-slice sprites.

    python Tools/ArtReview/BuildRegentLaneMark.py

The Warden's mark (WardenSlamView) is two LineRenderers on the shared sprite material: an edge ring the blow's reach
wide, tinted Ember (1, .32, .12) and pulsing faster as the blow nears, and an inner ring growing out to it through the
telegraph (WardenSlamClock.TelegraphProgress), tinted Hot (1, .82, .45) - the moment it fills is the moment the blow
lands - then the whole mark flashing hot. The lane keeps every one of those rules; only the shape changes, a rectangle
from the Regent through the hero's spot, RegentLaneWidth across and RegentLaneLength long (1.2 x 10 floor units).

    FX_RegentLane_Edge.png  white: a rounded rectangle outline 9 texels (0.07 units, the Warden edge ring's width) at 128
                            texels a unit. Tinted at runtime exactly as the Warden's edge (Ember, its pulse in alpha;
                            the blow's flash lerps it to Hot).
    FX_RegentLane_Fill.png  coloured: a stripe of the telegraph red-orange #FF5A3C (docs/49 rule 4) at 42 % opacity
                            with Hot long edges (the Warden's inner ring), sitting just inside the edge. Tinted white
                            with the Warden fill's alpha (.4 + .6 x progress; the flash's on the blow).

Runtime recipe (the runtime agent's; nothing is wired here): both SpriteRenderers on SharedSpriteMaterial, drawMode
Sliced, pivot at the lane's start (the Regent's end, x 0, y 0.5), under one transform at the Regent's floor point,
rotated in floor space to the lane's direction, whose parent squashes y by ArenaFloor.DepthScale (so the stripe lies on
the 2:1 floor; the edge's 9 texels then thin across a depth-running lane as the Warden's ring does). Edge size =
(length, width); fill size = (length, max(.05, progress) x width) - the stripe grows from the lane's middle line out
to its edges and fills them as the shards run.

Each .meta is the Forge Warden's idle sprite's .meta with a fresh GUID (kept on re-runs), the pivot (0, 0.5) and the
9-slice border. Writes Assets/_Project/Art/Characters/GlassRegent/Lane/FX_RegentLane_Edge.png, _Fill.png (+ .meta).
"""
import re
import uuid
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Assets/_Project/Art/Characters/GlassRegent/Lane'
WARDEN_META = ROOT / 'Assets/_Project/Art/Characters/ForgeWarden/CHR_Warden_front_idle.png.meta'
SIZE = 64
BORDER = 20          # texels on every side kept unstretched by the 9-slice
RADIUS = 14          # the corners' radius, texels
EDGE = 9             # the edge's stroke, texels: 0.07 units at 128 a unit, the Warden edge ring's width
FILL_INSET = 6       # the fill sits this far inside the edge's outer line, under the edge's stroke
FILL_LINE = 6        # the fill's Hot long edges
RED_ORANGE = (0xFF, 0x5A, 0x3C)
HOT = (0xFF, 0xD1, 0x73)        # WardenSlamView.Hot, (1, .82, .45)
FILL_ALPHA = 0.42
SS = 8               # supersamples per texel side, for the anti-aliased curves


def rounded_box_distance(x, y, half_w, half_h, r):
    """Signed distance to a rounded rectangle centred at the origin (negative inside)."""
    qx = np.abs(x) - (half_w - r)
    qy = np.abs(y) - (half_h - r)
    outside = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0))
    inside = np.minimum(np.maximum(qx, qy), 0)
    return outside + inside - r


def coverage(fn):
    """The share of each texel where fn(x, y) holds, from SS x SS samples; x right, y up, from the texture's centre."""
    s = (np.arange(SIZE * SS) + 0.5) / SS - SIZE / 2
    x, y = np.meshgrid(s, -s)
    hit = fn(x, y).astype(np.float64)
    return hit.reshape(SIZE, SS, SIZE, SS).mean(axis=(1, 3))


def edge():
    half = SIZE / 2
    d = lambda x, y: rounded_box_distance(x, y, half - 1, half - 1, RADIUS)
    ring = coverage(lambda x, y: (d(x, y) <= 0) & (d(x, y) > -EDGE))
    out = np.zeros((SIZE, SIZE, 4), np.uint8)
    out[..., :3] = 255
    out[..., 3] = (ring * 255 + 0.5).astype(np.uint8)
    return out


def fill():
    half = SIZE / 2 - 1 - FILL_INSET
    r = RADIUS - FILL_INSET
    body = coverage(lambda x, y: rounded_box_distance(x, y, half, half, r) <= 0)
    # The Hot lines along the lane's two long sides (the texture's top and bottom), the Warden's inner ring.
    lines = coverage(lambda x, y: (rounded_box_distance(x, y, half, half, r) <= 0) & (np.abs(y) > half - FILL_LINE))
    a = body * FILL_ALPHA * (1 - lines) + lines
    rgb = (np.array(RED_ORANGE) * (body * FILL_ALPHA * (1 - lines))[..., None] + np.array(HOT) * lines[..., None])
    rgb = rgb / np.maximum(a, 1e-6)[..., None]
    out = np.zeros((SIZE, SIZE, 4), np.uint8)
    out[..., :3] = np.clip(rgb + 0.5, 0, 255).astype(np.uint8)
    out[..., 3] = (np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8)
    out[out[..., 3] == 0] = 0
    return out


def write(name, pixels):
    path = OUT / (name + '.png')
    meta = Path(str(path) + '.meta')
    guid = None
    if meta.exists():
        m = re.search(r'^guid: ([0-9a-f]{32})', meta.read_text(encoding='utf-8'), re.M)
        guid = m.group(1) if m else None
    guid = guid or uuid.uuid4().hex
    Image.fromarray(pixels, 'RGBA').save(path, optimize=True)
    text = WARDEN_META.read_text(encoding='utf-8')
    text = re.sub(r'^guid: [0-9a-f]{32}', 'guid: ' + guid, text, count=1, flags=re.M)
    text = re.sub(r'^  spritePivot: \{[^}]*\}', '  spritePivot: {x: 0, y: 0.5}', text, count=1, flags=re.M)
    text = re.sub(r'^  spriteBorder: \{[^}]*\}', '  spriteBorder: {x: %d, y: %d, z: %d, w: %d}' % ((BORDER,) * 4), text,
                  count=1, flags=re.M)
    assert 'spriteMeshType: 0' in text, 'A sliced sprite needs a full-rect mesh.'
    meta.write_text(text, encoding='utf-8', newline='\n')
    return guid


OUT.mkdir(parents=True, exist_ok=True)
folder_meta = Path(str(OUT) + '.meta')
if not folder_meta.exists():
    folder_meta.write_text('fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n'
                           '  userData: \n  assetBundleName: \n  assetBundleVariant: \n' % uuid.uuid4().hex,
                           encoding='utf-8', newline='\n')
print('LANE_MARK edge %s fill %s' % (write('FX_RegentLane_Edge', edge()), write('FX_RegentLane_Fill', fill())))
