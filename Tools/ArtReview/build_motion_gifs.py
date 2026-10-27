"""Builds review GIFs from HeroMotionCapture's frames (Tools/ArtReview/Run-HeroMotionCapture.ps1).

    python Tools/ArtReview/build_motion_gifs.py <capture_root> [out_dir]

capture_root holds one folder per presentation (vanguard-frames, vanguard-puppet, arcanist), each with numbered
BMP or PNG frames captured at 30 frames a second and marks.txt naming where each segment starts. For every presentation it
writes <name>-all.gif and one GIF per segment; for every segment more than one presentation recorded, a side-by-side
compare-<segment>.gif with a caption under each column. out_dir defaults to capture_root/gifs.
"""
import os
import sys

from PIL import Image, ImageDraw

FRAME_MS = 33  # 30 frames a second, as captured (every second frame of 60).
CAPTION = 34


def segments(folder):
    marks = []
    path = os.path.join(folder, "marks.txt")
    if os.path.exists(path):
        for line in open(path, encoding="utf-8"):
            parts = line.split()
            if len(parts) == 2:
                marks.append((parts[0], int(parts[1])))
    frames = sorted(f for f in os.listdir(folder) if f.endswith((".png", ".bmp")))
    spans = {}
    for i, (name, start) in enumerate(marks):
        end = marks[i + 1][1] if i + 1 < len(marks) else len(frames)
        spans[name] = frames[start:end]
    return frames, spans


def save_gif(images, path):
    if not images:
        return
    first, rest = images[0], images[1:]
    first.save(path, save_all=True, append_images=rest, duration=FRAME_MS, loop=0, optimize=True, disposal=2)


def load(folder, names, scale=1.0):
    out = []
    for name in names:
        image = Image.open(os.path.join(folder, name)).convert("RGB")
        if scale != 1.0:
            image = image.resize((int(image.width * scale), int(image.height * scale)), Image.LANCZOS)
        out.append(image)
    return out


def main():
    root = sys.argv[1]
    out = sys.argv[2] if len(sys.argv) > 2 else os.path.join(root, "gifs")
    os.makedirs(out, exist_ok=True)
    setups = sorted(d for d in os.listdir(root) if os.path.isdir(os.path.join(root, d)) and d != "gifs")
    spans_by_setup = {}
    for setup in setups:
        folder = os.path.join(root, setup)
        frames, spans = segments(folder)
        spans_by_setup[setup] = spans
        save_gif(load(folder, frames, .5), os.path.join(out, f"{setup}-all.gif"))
        for name, names in spans.items():
            save_gif(load(folder, names), os.path.join(out, f"{setup}-{name}.gif"))
        print(f"{setup}: {len(frames)} frames, segments {', '.join(spans)}")

    segment_names = []
    for spans in spans_by_setup.values():
        for name in spans:
            if name not in segment_names:
                segment_names.append(name)
    for name in segment_names:
        present = [s for s in setups if name in spans_by_setup[s] and spans_by_setup[s][name]]
        if len(present) < 2:
            continue
        columns = [load(os.path.join(root, s), spans_by_setup[s][name]) for s in present]
        length = min(len(c) for c in columns)
        width, height = columns[0][0].size
        composed = []
        for i in range(length):
            sheet = Image.new("RGB", (width * len(columns), height + CAPTION), (18, 16, 22))
            draw = ImageDraw.Draw(sheet)
            for c, column in enumerate(columns):
                sheet.paste(column[i], (c * width, 0))
                draw.text((c * width + 10, height + 9), present[c], fill=(230, 220, 200))
            composed.append(sheet)
        save_gif(composed, os.path.join(out, f"compare-{name}.gif"))
        print(f"compare-{name}: {', '.join(present)} x {length} frames")


if __name__ == "__main__":
    main()
