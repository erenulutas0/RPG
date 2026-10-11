"""Lossless packing of two native Blender renders; never resizes or retouches pixels.

Writes review atlases only. Runtime import is a separate, reviewed step.
"""
from pathlib import Path
import hashlib
import json
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtDirection/2026-10-11/foundry-industrial-01/sources'
OUT.mkdir(parents=True, exist_ok=True)
records = []
for source, target in [('industrial-furnace-01', 'FoundryBoiler'),
                       ('industrial-retort-01', 'FoundryShortBoiler')]:
    atlas = Image.new('RGBA', (2048, 1024))
    tiles = []
    for index in range(2):
        path = ROOT / f'ArtDirection/2026-10-11/{source}/variant-{index}.png'
        with Image.open(path) as image:
            assert image.mode == 'RGBA' and image.size == (1024, 1024), path
            alpha = image.getchannel('A')
            bounds = alpha.getbbox()
            assert bounds is not None, f'Empty render: {path}'
            # Blank columns are authored render margins, not generated padding.
            # At least 32 pixels to either tile edge prevents nearby mip filtering
            # from mixing visible parts of adjacent variants in normal phone views.
            assert bounds[0] >= 32 and bounds[2] <= 992, (path, bounds)
            assert bounds[1] > 0 and bounds[3] < 1024, (path, bounds)
            atlas.paste(image, (1024 * index, 0))
            assert atlas.crop((1024*index, 0, 1024*(index+1), 1024)).tobytes() == image.tobytes()
            tiles.append(dict(source=str(path.relative_to(ROOT)),
                              sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                              alpha_bounds=bounds, tile=index))
    path = OUT / f'{target}.png'
    atlas.save(path)
    records.append(dict(atlas=path.name, size=atlas.size, tiles=tiles,
                        sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
(OUT / 'atlas-provenance.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
print('Packed two 2048x1024 RGBA atlases; source tile pixels preserved exactly.')
