"""Review images for docs/57's four painted skill effects (build_hero_skill_vfx.py), composited from the game's own
sprites as CombatEffectsView draws them.

    python Tools/ArtReview/review_hero_skill_vfx.py [--out DIR] [--art-direction DIR]

From a worktree, pass --out E:/MobileGame/ArtDirection/2026-11-25/skill-vfx-01 (the main checkout's, untracked).

Every strip is read back from its sheet and SkillEffectArtSet.asset, exactly what Unity imports (before ASTC), and drawn
as the GPU does: bilinear sampling, straight alpha blended in sRGB (the project's gamma colour space), at the phone's
scale (the Samsung S23 shows 120 pixels a world unit) and at four times it. Beside the rendered Vanguard and Ilya (their
packed atlases, the frames.json of their packs under ArtDirection), on the Foundry's court (an in-game capture at the
phone's scale), on its lava (the pit's sampled colours) and on the Gloam Gardens' loam (GloamPavingV1, its cobbles sized
as the court's). Timed and sized by the view's own rules: the bell's 3 closing frames at 0.06 s, its hold loop at 0.15 s
and its fall at 0.07 s; the rivets at a unit's radius over 0.3 s; the Rush's wake riding the hero's middle 0.45 over its
feet and the impact 0.3 ahead of it; the glyph grown from 0.3 of its 2.5 radius over 0.2 s and dimmed to 0.3 over its last
second; the ward's rise, whirl and break, and its flare at a striker.

Writes into DIR (default ArtDirection/2026-11-25/skill-vfx-01, untracked):
    <skill>-frames.png   each strip's frames at 4x (top) and at the phone's scale (below), on the court, the lava and
                         the loam, the hero standing in them where the effect is the hero's
    <skill>-play.png     the skill as it plays, a frame every 0.1 s (the guard and the glyph sampled over their seconds),
                         at the phone's scale beside the hero on the three floors
    <skill>.gif          the same at its real timing, on the court
    review-sheet.png     the four skills at the phone's scale on the court, side by side
"""
import json
import math
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
ARGS = sys.argv[1:]


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


def art_direction():
    """ArtDirection is untracked: in a worktree it is the main checkout's."""
    for folder in [ROOT] + list(ROOT.parents):
        if (folder / "ArtDirection" / "2026-11-22" / "vanguard-blender-pack" / "frames.json").exists():
            return folder / "ArtDirection"
    raise SystemExit("ArtDirection with the heroes' frames.json not found; pass --art-direction")


ART = Path(arg("--art-direction")) if arg("--art-direction") else art_direction()
OUT = Path(arg("--out", str(ART / "2026-11-25" / "skill-vfx-01")))
SKILLS = ROOT / "Assets/_Project/Art/Effects/Skills"
CHARACTERS = ROOT / "Assets/_Project/Art/Characters"
PHONE = 120.0
COURT = ART / "2026-10-23/foundry-rim-pipes-01/after/approach-0-0.png"
LAVA = (176, 74, 28)
LAVA_HOT = (236, 132, 46)
LOAM = ROOT / "Assets/_Project/Art/Environment/Gloam/GloamPavingV1.png"
LOAM_PIXELS_A_UNIT = 300.0  # the loam's cobbles about the size of the court's at the phone's scale

# CombatEffectsView's numbers.
RESOLVE_CLOSE, RESOLVE_HOLD, RESOLVE_END, RIVET_FRAME, RIVET_RADIUS = 0.06, 0.15, 0.07, 0.1, 1.0
RUSH_FRAME, IMPACT_FRAME, RUSH_LIFT, IMPACT_REACH, RUSH_LENGTH, RUSH_SECONDS = 0.06, 0.05, 0.45, 0.3, 4.0, 0.3
SIGIL_IGNITE, SIGIL_START, SIGIL_FRAME, SIGIL_DIM, SIGIL_DIM_TO, SIGIL_RADIUS, SIGIL_SECONDS = 0.2, 0.3, 0.1, 1.0, 0.3, 2.5, 5.0
WARD_RAISE, WARD_SHELL, WARD_BREAK, FLARE_FRAME, WARD_SECONDS = 0.06, 0.08, 0.07, 0.05, 2.0
GUARD_SECONDS = 4.0


class Sprite:
    def __init__(self, rgba, pivot, ppu):
        self.rgba = rgba.astype(np.float32) / 255.0
        self.pivot, self.ppu = pivot, ppu


def strips():
    text = (SKILLS / "SkillEffectArtSet.asset").read_text(encoding="utf-8")
    sheets = {}
    for meta in SKILLS.glob("*.png.meta"):
        guid = re.search(r"^guid: (\w+)", meta.read_text(encoding="utf-8"), re.M).group(1)
        sheets[guid] = np.asarray(Image.open(str(meta)[:-5]).convert("RGBA"))
    found = {}
    for name in ("resolveClose", "resolveHold", "resolveEnd", "resolveRivets", "rushStreak", "rushImpact", "sigilIgnite",
                 "sigilGlyph", "wardRaise", "wardShell", "wardBreak", "wardFlare"):
        block = re.search(r"(?ms)^  _%s:\n(.*?)(?=^  _|\Z)" % name, text).group(1)
        sheet = sheets[re.search(r"guid: (\w+)", block).group(1)]
        tpu = float(re.search(r"TexelsPerUnit: ([\d.]+)", block).group(1))
        px, py = (float(v) for v in re.search(r"Pivot: \{x: ([-\d.e]+), y: ([-\d.e]+)\}", block).groups())
        h = sheet.shape[0]
        cells = [tuple(int(v) for v in c) for c in
                 re.findall(r"- X: (\d+)\n\s+Y: (\d+)\n\s+Width: (\d+)\n\s+Height: (\d+)", block)]
        found[name] = [Sprite(sheet[h - y - ch:h - y, x:x + w], (px, py), tpu) for x, y, w, ch in cells]
    return found


def hero_frames(report, folder, prefix):
    data = json.loads(report.read_text(encoding="utf-8"))
    pages = [np.asarray(Image.open(folder / a["file"]).convert("RGBA")) for a in data["atlases"]]
    frames = {}
    for f in data["frames"]:
        if not f["name"].startswith(prefix):
            continue
        page = pages[f["atlas"]]
        x, y, w, h = f["rect"]
        top = page.shape[0] - y - h
        frames[f["name"][len(prefix):]] = Sprite(page[top:top + h, x:x + w], tuple(f["pivot"]), data["pixels_per_unit"])
    return frames


def bilinear(texture, tx, ty):
    """GPU bilinear sampling of a straight-alpha texture at texel coordinates (tx columns, ty rows from the top), on
    premultiplied colour so a bled clear texel never darkens an edge; clamped at the edges."""
    h, w = texture.shape[:2]
    x0, y0 = np.floor(tx).astype(int), np.floor(ty).astype(int)
    fx, fy = (tx - x0).astype(np.float32)[..., None], (ty - y0).astype(np.float32)[..., None]
    xa, xb = np.clip(x0, 0, w - 1), np.clip(x0 + 1, 0, w - 1)
    ya, yb = np.clip(y0, 0, h - 1), np.clip(y0 + 1, 0, h - 1)
    pre = np.concatenate([texture[..., :3] * texture[..., 3:], texture[..., 3:]], axis=-1)
    top = pre[ya, xa] * (1 - fx) + pre[ya, xb] * fx
    low = pre[yb, xa] * (1 - fx) + pre[yb, xb] * fx
    return top * (1 - fy) + low * fy


class View:
    """A canvas of world space at scale pixels a unit, centred on centre."""

    def __init__(self, background, centre, scale):
        self.canvas = background.astype(np.float32) / 255.0
        self.centre, self.scale = centre, scale

    def draw(self, sprite, position, size=1.0, alpha=1.0, angle=0.0, flip=False):
        """The sprite with its pivot at position (world), scaled by size, turned by angle degrees about the pivot."""
        tw, th = sprite.rgba.shape[1], sprite.rgba.shape[0]
        h, w = self.canvas.shape[:2]
        cx = w / 2 + (position[0] - self.centre[0]) * self.scale
        cy = h / 2 - (position[1] - self.centre[1]) * self.scale
        k = self.scale * size / sprite.ppu  # canvas pixels a texel
        reach = math.hypot(tw, th) * k
        x0, x1 = max(0, int(cx - reach)), min(w, int(cx + reach) + 1)
        y0, y1 = max(0, int(cy - reach)), min(h, int(cy + reach) + 1)
        if x0 >= x1 or y0 >= y1:
            return
        px, py = np.meshgrid(np.arange(x0, x1) + 0.5 - cx, np.arange(y0, y1) + 0.5 - cy)
        a = math.radians(angle)
        # Into the sprite's frame: undo the turn (screen y runs down), then texels from the pivot.
        u = (px * math.cos(a) - py * math.sin(a)) / k
        v = (px * math.sin(a) + py * math.cos(a)) / k
        if flip:
            u = -u
        tx = sprite.pivot[0] * tw + u - 0.5
        ty = (1 - sprite.pivot[1]) * th + v - 0.5
        inside = (tx > -1) & (tx < tw) & (ty > -1) & (ty < th)
        sample = bilinear(sprite.rgba, tx, ty) * inside[..., None] * alpha
        region = self.canvas[y0:y1, x0:x1, :3]
        self.canvas[y0:y1, x0:x1, :3] = sample[..., :3] + region * (1 - sample[..., 3:])

    def image(self):
        return Image.fromarray((np.clip(self.canvas[..., :3], 0, 1) * 255 + 0.5).astype(np.uint8), "RGB")


def shadow(view, at, width):
    """ContactShadowView's soft ellipse under an actor, on the 2:1 floor."""
    size = 64
    yy, xx = np.mgrid[0:size // 2, 0:size]
    d = np.hypot((xx + 0.5 - size / 2) / (size / 2), (yy + 0.5 - size / 4) / (size / 4))
    rgba = np.zeros((size // 2, size, 4), np.uint8)
    rgba[..., 0], rgba[..., 1], rgba[..., 2] = 8, 6, 12
    rgba[..., 3] = (np.clip(1.0 - d, 0, 1) ** 0.8 * 110).astype(np.uint8)
    view.draw(Sprite(rgba, (0.5, 0.5), size / width), at)


_FLOORS = {}


def floor(kind, size, scale):
    """A patch of floor size pixels at scale pixels a unit: the court (an in-game capture at the phone's scale), the
    pit's lava, or the loam."""
    key = (kind, size, scale)
    if key in _FLOORS:
        return _FLOORS[key]
    w, h = size
    if kind == "court":
        court = Image.open(COURT).convert("RGB").crop((20, 300, 900, 900))  # clear of its gold inlay
        grow = scale / PHONE
        court = court.resize((int(court.width * grow), int(court.height * grow)), Image.BILINEAR)
        out = Image.new("RGB", size)
        for oy in range(0, h, court.height):
            for ox in range(0, w, court.width):
                out.paste(court, (ox, oy))
        image = np.asarray(out)
    elif kind == "loam":
        loam = Image.open(LOAM).convert("RGB")
        side = max(1, int(loam.width * scale / LOAM_PIXELS_A_UNIT))
        loam = loam.resize((side, side), Image.BILINEAR)
        out = Image.new("RGB", size)
        for oy in range(0, h, side):
            for ox in range(0, w, side):
                out.paste(loam, (ox, oy))
        image = np.asarray(out)
    else:
        yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
        wave = 0.5 + 0.5 * np.sin(xx / scale * 3.1 + np.sin(yy / scale * 2.3) * 1.7) * np.sin(yy / scale * 4.0)
        image = (np.array(LAVA)[None, None] * (1 - wave[..., None] * 0.45) +
                 np.array(LAVA_HOT)[None, None] * wave[..., None] * 0.45).astype(np.uint8)
    _FLOORS[key] = image
    return image


def label(image, text):
    draw = ImageDraw.Draw(image)
    draw.rectangle((0, 0, 8 + 6 * len(text), 16), fill=(0, 0, 0))
    draw.text((4, 3), text, fill=(235, 235, 235))
    return image


def grid(rows, gap=6, background=(14, 14, 18)):
    width = max(sum(t.width + gap for t in row) for row in rows) + gap
    height = sum(max(t.height for t in row) + gap for row in rows) + gap
    sheet = Image.new("RGB", (width, height), background)
    y = gap
    for row in rows:
        x = gap
        for t in row:
            sheet.paste(t, (x, y))
            x += t.width + gap
        y += max(t.height for t in row) + gap
    return sheet


# ----------------------------------------------------------------------------------------------------------------------
# What the view draws at time t after the press, as a list of (sprite, position, size, alpha, angle); the hero's frame.


def guard_at(art, t):
    shell, drawn = [], []
    close = art["resolveClose"]
    rise = len(close) * RESOLVE_CLOSE
    if t < rise:
        shell = [close[min(int(t / RESOLVE_CLOSE), len(close) - 1)]]
    elif t < GUARD_SECONDS:
        hold = art["resolveHold"]
        shell = [hold[int((t - rise) / RESOLVE_HOLD) % len(hold)]]
    else:
        end = art["resolveEnd"]
        k = int((t - GUARD_SECONDS) / RESOLVE_END)
        shell = [end[k]] if k < len(end) else []
    rivets = art["resolveRivets"]
    k = int(t / RIVET_FRAME)
    floor_parts = [(rivets[k], (0, 0), RIVET_RADIUS, 1.0, 0.0)] if k < len(rivets) else []
    return floor_parts, [(s, (0, 0), 1.0, 1.0, 0.0) for s in shell]


def rush_at(art, t, heading=(1.0, 0.0)):
    angle = math.degrees(math.atan2(heading[1] * 0.5, heading[0]))
    travel = min(t, RUSH_SECONDS) / RUSH_SECONDS * RUSH_LENGTH
    at = (heading[0] * travel, heading[1] * travel * 0.5)
    parts = []
    wake = art["rushStreak"]
    k = int(t / RUSH_FRAME)
    if k < len(wake):
        left = len(wake) * RUSH_FRAME - t
        parts.append((wake[k], (at[0], at[1] + RUSH_LIFT), 1.0, min(1.0, left / 0.16), angle))
    if t >= RUSH_SECONDS:
        impact = art["rushImpact"]
        k = int((t - RUSH_SECONDS) / IMPACT_FRAME)
        if k < len(impact):
            a = math.radians(angle)
            parts.append((impact[k], (at[0] + math.cos(a) * IMPACT_REACH, at[1] + math.sin(a) * IMPACT_REACH + RUSH_LIFT),
                          1.0, 1.0, angle))
    return at, parts


def sigil_at(art, t):
    if t >= SIGIL_SECONDS:
        return []
    grow = min(1.0, t / SIGIL_IGNITE)
    size = SIGIL_RADIUS * (SIGIL_START + (1 - SIGIL_START) * (1 - (1 - grow) ** 2))
    alpha = SIGIL_DIM_TO + (1 - SIGIL_DIM_TO) * min(1.0, max(0.0, (SIGIL_SECONDS - t) / SIGIL_DIM))
    if t < SIGIL_IGNITE:
        ignite = art["sigilIgnite"]
        sprite = ignite[min(int(t / (SIGIL_IGNITE / len(ignite))), len(ignite) - 1)]
    else:
        glyph = art["sigilGlyph"]
        sprite = glyph[int((t - SIGIL_IGNITE) / SIGIL_FRAME) % len(glyph)]
    return [(sprite, (0, 0), size, alpha, 0.0)]


def ward_at(art, t):
    rise = len(art["wardRaise"]) * WARD_RAISE
    if t < rise:
        return [(art["wardRaise"][int(t / WARD_RAISE)], (0, 0), 1.0, 1.0, 0.0)]
    if t < WARD_SECONDS:
        shell = art["wardShell"]
        return [(shell[int((t - rise) / WARD_SHELL) % len(shell)], (0, 0), 1.0, 1.0, 0.0)]
    k = int((t - WARD_SECONDS) / WARD_BREAK)
    return [(art["wardBreak"][k], (0, 0), 1.0, 1.0, 0.0)] if k < len(art["wardBreak"]) else []


# ----------------------------------------------------------------------------------------------------------------------


def scene(kind, size, scale, centre, hero, hero_at, floor_parts=(), above=(), enemy=None):
    view = View(floor(kind, size, scale), centre, scale)
    for sprite, at, s, a, angle in floor_parts:
        view.draw(sprite, at, s, a, angle)
    shadow(view, hero_at, 0.62)
    if enemy is not None:
        shadow(view, enemy[1], 0.6)
        view.draw(enemy[0], enemy[1])
    view.draw(hero, hero_at)
    for sprite, at, s, a, angle in above:
        view.draw(sprite, at, s, a, angle)
    return view.image()


def frames_sheet(name, groups, hero, scale_rows=(4.0, 1.0), kinds=("court", "lava", "loam"), centre=(0, 0.6),
                 size=(2.4, 2.4), ground=False, hero_in=True):
    """Every frame of the groups (lists of strips) on each floor, at 4x and at the phone's scale."""
    rows = []
    for mult in scale_rows:
        scale = PHONE * mult
        px = (int(size[0] * scale), int(size[1] * scale))
        for kind in kinds:
            row = []
            for strip_name, strip, s in groups:
                for i, sprite in enumerate(strip):
                    parts = [(sprite, (0, 0), s, 1.0, 0.0)]
                    image = scene(kind, px, scale, centre, hero, (0, 0), floor_parts=parts if ground else (),
                                  above=() if ground else parts) if hero_in else \
                        scene(kind, px, scale, centre, hero, (99, 99), floor_parts=parts if ground else (),
                              above=() if ground else parts)
                    row.append(label(image, f"{strip_name} {i} {kind} {'x4' if mult == 4 else 'phone'}"))
            rows.append(row)
    return grid(rows)


def play_sheet(times, render):
    rows = []
    for kind in ("court", "lava", "loam"):
        rows.append([label(render(kind, t), f"{t:.2f} s {kind}") for t in times])
    return grid(rows)


def save_gif(frames, path, milliseconds):
    sample = Image.new("RGB", (frames[0].width, frames[0].height * 4))
    for k, frame in enumerate(frames[:: max(1, len(frames) // 4)][:4]):
        sample.paste(frame, (0, frames[0].height * k))
    palette = sample.quantize(255, method=Image.MEDIANCUT)
    indexed = [f.quantize(palette=palette, dither=Image.FLOYDSTEINBERG) for f in frames]
    indexed[0].save(path, save_all=True, append_images=indexed[1:], duration=milliseconds, loop=0, optimize=True)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    art = strips()
    vanguard = hero_frames(ART / "2026-11-22/vanguard-blender-pack/frames.json", CHARACTERS / "Vanguard", "vanguard_")
    ilya = hero_frames(ART / "2026-10-27/arcanist-blender-01/frames.json", CHARACTERS / "CinderArcanist", "")
    v_idle, i_idle = vanguard["idle_south-east_0"], ilya["idle_south-east_0"]
    grunt = Sprite(np.asarray(Image.open(CHARACTERS / "SlagBrute/CHR_Grunt_front-idle.tga").convert("RGBA")), (0.5, 0.1),
                   128.0)

    # Iron Resolve.
    sheet = frames_sheet("resolve", [("close", art["resolveClose"], 1.0), ("hold", art["resolveHold"], 1.0),
                                     ("end", art["resolveEnd"], 1.0)], v_idle, size=(2.2, 2.4))
    sheet.save(OUT / "iron-resolve-frames.png")
    frames_sheet("rivets", [("rivets", art["resolveRivets"], RIVET_RADIUS)], v_idle, size=(2.6, 1.8), centre=(0, 0.3),
                 ground=True).save(OUT / "iron-resolve-rivets-frames.png")

    def guard_scene(kind, t, scale=PHONE):
        floor_parts, above = guard_at(art, t)
        return scene(kind, (int(2.6 * scale), int(2.4 * scale)), scale, (0, 0.6), v_idle, (0, 0), floor_parts, above)

    times = [0.0, 0.06, 0.12, 0.2, 0.5, 1.0, 2.0, 3.9, 4.0, 4.07, 4.14]
    play_sheet(times, guard_scene).save(OUT / "iron-resolve-play.png")
    save_gif([guard_scene("court", k / 30) for k in range(int(4.4 * 30))], OUT / "iron-resolve.gif", 33)

    # Shield Rush: east, then up the floor to the north-east, the rendered dash under the wake.
    frames_sheet("rush", [("streak", art["rushStreak"], 1.0), ("impact", art["rushImpact"], 1.0)],
                 vanguard["shield-rush_east_1"], size=(3.0, 1.6), centre=(-0.6, 0.45)).save(OUT / "shield-rush-frames.png")

    def rush_scene(kind, t, heading=(1.0, 0.0), scale=PHONE):
        at, parts = rush_at(art, t, heading)
        facing = "east" if abs(heading[1]) < 0.5 else "north-east"
        hero = vanguard["shield-rush_%s_%d" % (facing, min(3, int(t / 0.075)))] if t < RUSH_SECONDS else \
            vanguard["idle_%s_0" % facing]
        target = (at[0] + heading[0] * 0.95, at[1] + heading[1] * 0.95 * 0.5) if t >= RUSH_SECONDS else \
            (heading[0] * (RUSH_LENGTH + 0.95), heading[1] * (RUSH_LENGTH + 0.95) * 0.5)
        mid = (heading[0] * RUSH_LENGTH * 0.5, heading[1] * RUSH_LENGTH * 0.25 + 0.5)
        return scene(kind, (int(6.4 * scale), int(2.6 * scale)), scale, mid, hero, at, above=parts, enemy=(grunt, target))

    times = [0.0, 0.06, 0.12, 0.18, 0.24, 0.3, 0.35, 0.4, 0.45]
    play_sheet(times, rush_scene).save(OUT / "shield-rush-play.png")
    up = (math.cos(math.radians(40)), math.sin(math.radians(40)))
    grid([[label(rush_scene(kind, t, up), f"{t:.2f} s {kind}, up the floor") for t in (0.12, 0.24, 0.35)]
          for kind in ("court", "lava", "loam")]).save(OUT / "shield-rush-diagonal.png")
    save_gif([rush_scene("court", k / 60) for k in range(int(0.6 * 60))], OUT / "shield-rush.gif", 33)

    # Flame Sigil, at the glyph's 2.5 with Ilya at its centre and a Grunt wading in.
    frames_sheet("sigil", [("ignite", art["sigilIgnite"], SIGIL_RADIUS), ("glyph", art["sigilGlyph"], SIGIL_RADIUS)],
                 i_idle, scale_rows=(1.0,), size=(6.0, 3.6), centre=(0, 0.3), ground=True).save(
        OUT / "flame-sigil-frames.png")
    frames_sheet("sigil-4x", [("glyph", art["sigilGlyph"][:1], 1.0)], i_idle, scale_rows=(4.0,), size=(2.4, 1.5),
                 centre=(0, 0.05), ground=True, hero_in=False).save(OUT / "flame-sigil-frames-4x.png")

    def sigil_scene(kind, t, scale=PHONE):
        return scene(kind, (int(6.4 * scale), int(3.8 * scale)), scale, (0, 0.35), i_idle, (-0.9, -0.3),
                     sigil_at(art, t), enemy=(grunt, (1.4, 0.35)))

    times = [0.0, 0.1, 0.2, 0.5, 2.5, 4.2, 4.6, 4.95]
    play_sheet(times, sigil_scene).save(OUT / "flame-sigil-play.png")
    save_gif([sigil_scene("court", k / 20, scale=60.0) for k in range(int(5.3 * 20))], OUT / "flame-sigil.gif", 50)

    # Cinder Ward.
    frames_sheet("ward", [("raise", art["wardRaise"], 1.0), ("shell", art["wardShell"], 1.0),
                          ("break", art["wardBreak"], 1.0)], i_idle, size=(2.2, 2.3)).save(OUT / "cinder-ward-frames.png")

    def ward_scene(kind, t, scale=PHONE):
        view_parts = ward_at(art, t)
        image = scene(kind, (int(3.2 * scale), int(2.4 * scale)), scale, (0.5, 0.6), i_idle, (0, 0), above=view_parts,
                      enemy=(grunt, (1.25, 0.1)))
        if 0.8 <= t < 0.8 + len(art["wardFlare"]) * FLARE_FRAME:
            view = View(np.asarray(image), (0.5, 0.6), scale)
            view.draw(art["wardFlare"][int((t - 0.8) / FLARE_FRAME)], (1.25, 0.55))
            image = view.image()
        return image

    times = [0.0, 0.06, 0.12, 0.3, 0.8, 0.85, 1.5, 2.0, 2.07, 2.14]
    play_sheet(times, ward_scene).save(OUT / "cinder-ward-play.png")
    frames_sheet("flare", [("flare", art["wardFlare"], 1.0)], grunt, size=(1.4, 1.4), centre=(0, 0.5),
                 hero_in=False).save(OUT / "cinder-ward-flare-frames.png")
    save_gif([ward_scene("court", k / 30) for k in range(int(2.4 * 30))], OUT / "cinder-ward.gif", 33)

    grid([[label(guard_scene("court", 1.2), "Iron Resolve, holding"), label(rush_scene("court", 0.2), "Shield Rush"),
           label(sigil_scene("court", 1.5), "Flame Sigil"), label(ward_scene("court", 0.9), "Cinder Ward, a blow absorbed")],
          [label(guard_scene("loam", 0.06), "Iron Resolve, the clang"), label(rush_scene("loam", 0.35), "the impact"),
           label(sigil_scene("loam", 0.1), "the ignition"), label(ward_scene("loam", 2.07), "the ward breaking")]]
         ).save(OUT / "review-sheet.png")
    print("wrote", OUT)


if __name__ == "__main__":
    main()
