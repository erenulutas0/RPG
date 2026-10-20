"""Lossless packing of the two reference-tower Blender renders into the runtime atlas.

Never resizes or retouches pixels. Tile 0 (near furnace) and tile 1 (far furnace)
keep the slots of the previous industrial atlas, so the runtime cards, their
projection and depth bands are unchanged.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'ArtDirection/2026-10-20/reference-towers-01'
TARGET = ROOT / 'Assets/_Project/Resources/FoundryBoiler.png'
atlas = Image.new('RGBA', (2048, 1024))
tiles = []
for index in range(2):
    path = SOURCE / f'variant-{index}.png'
    with Image.open(path) as image:
        assert image.mode == 'RGBA' and image.size == (1024, 1024), path
        bounds = image.getchannel('A').getbbox()
        assert bounds is not None, f'Empty render: {path}'
        # At least 32 pixels to either tile edge keeps mip filtering from
        # mixing the neighbouring variant in ordinary phone views.
        assert bounds[0] >= 32 and bounds[2] <= 992, (path, bounds)
        assert bounds[1] > 0 and bounds[3] < 1024, (path, bounds)
        atlas.paste(image, (1024 * index, 0))
        assert atlas.crop((1024 * index, 0, 1024 * (index + 1), 1024)).tobytes() == image.tobytes()
        tiles.append(dict(source=str(path.relative_to(ROOT)), sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                          alpha_bounds=bounds, tile=index))
atlas.save(TARGET)
record = dict(atlas=str(TARGET.relative_to(ROOT)), size=atlas.size, tiles=tiles,
              sha256=hashlib.sha256(TARGET.read_bytes()).hexdigest())
(SOURCE / 'atlas-provenance.json').write_text(json.dumps(record, indent=2), encoding='utf-8')
print('Packed FoundryBoiler.png from two reference-tower renders; tile pixels preserved exactly.')
