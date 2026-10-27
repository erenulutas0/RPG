"""Builds before/after review GIFs from two HeroMotionCapture runs (Tools/ArtReview/Run-HeroMotionCapture.ps1).

    python Tools/ArtReview/compare_motion.py <before_root> <after_root> <out_dir> <before_setup>:<after_setup> [...]
        [--scale 0.6]

Each pair names a presentation folder in the before capture and one in the after capture, for example
vanguard-frames:vanguard-puppet (the painted frames the build shipped against the painted puppet) or arcanist:arcanist
(Ilya in the Vanguard's art against its own). For every segment both captured (idle, the walks, attacks, the ability)
it writes ba-<after_setup>-<segment>.gif with the before column left and the after column right, captioned, plus
ba-<after_setup>-walks.gif: the four walks one after another, the reel a reviewer watches first. --scale shrinks the
frames (a full-size reel runs to tens of megabytes, too much to open on a phone).
"""
import os
import sys

from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_motion_gifs import CAPTION, load, save_gif, segments  # noqa: E402

WALKS = ("walk-right", "walk-down", "walk-left", "walk-up")
# The walks reel keeps only the hero and the floor under it (capture pixels, 480 square): the full frame's floor detail
# is most of a GIF's size.
REEL_CROP = (90, 50, 390, 410)


def side_by_side(left, right, captions, crop=None):
    if crop:
        left = [image.crop(crop) for image in left]
        right = [image.crop(crop) for image in right]
    length = min(len(left), len(right))
    width, height = left[0].size
    out = []
    for i in range(length):
        sheet = Image.new("RGB", (width * 2, height + CAPTION), (18, 16, 22))
        sheet.paste(left[i], (0, 0))
        sheet.paste(right[i].resize((width, height), Image.LANCZOS) if right[i].size != (width, height) else right[i],
                    (width, 0))
        draw = ImageDraw.Draw(sheet)
        for c, caption in enumerate(captions):
            draw.text((c * width + 10, height + 9), caption, fill=(230, 220, 200))
        out.append(sheet)
    return out


def main():
    args = sys.argv[1:]
    scale = 1.0
    if "--scale" in args:
        i = args.index("--scale")
        scale = float(args[i + 1])
        del args[i:i + 2]
    if len(args) < 4:
        print(__doc__)
        sys.exit(2)
    before_root, after_root, out = args[0], args[1], args[2]
    os.makedirs(out, exist_ok=True)
    for pair in args[3:]:
        before_setup, after_setup = pair.split(":")
        before_folder = os.path.join(before_root, before_setup)
        after_folder = os.path.join(after_root, after_setup)
        _, before_spans = segments(before_folder)
        _, after_spans = segments(after_folder)
        captions = (f"before: {before_setup}", f"after: {after_setup}")
        reel = []
        for name, before_names in before_spans.items():
            after_names = after_spans.get(name)
            if not before_names or not after_names:
                continue
            before, after = load(before_folder, before_names, scale), load(after_folder, after_names, scale)
            save_gif(side_by_side(before, after, captions), os.path.join(out, f"ba-{after_setup}-{name}.gif"))
            if name in WALKS:
                reel.extend(side_by_side(before, after, captions, tuple(round(v * scale) for v in REEL_CROP)))
            print(f"ba-{after_setup}-{name}: {min(len(before), len(after))} frames")
        if reel:
            save_gif(reel, os.path.join(out, f"ba-{after_setup}-walks.gif"))
            print(f"ba-{after_setup}-walks: {len(reel)} frames")


if __name__ == "__main__":
    main()
