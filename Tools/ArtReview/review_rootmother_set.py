"""Rootmother Myrr's art set review (docs/64 section 14): her packed sprites (PackRootmotherSprites.py) as the game holds
them, her new walk and walking-telegraph rows, at the S23's phone scale beside the Glass Regent and the Forge Warden, and
review_rootmother.py's colour checks over the new frames.

    python Tools/ArtReview/review_rootmother_set.py [--render DIR] [--out DIR]

--render: BuildBlenderRootmother.py's --out (default TestResults/rootmother-render); --out: the review folder, which
also holds the pack's frames.json (default ArtDirection/2026-11-25/rootmother-02, untracked). Writes:
    1-walk-cycle.png        the walk's quarters (contact A, passing A, contact B, passing B) after the idle, and the
                            telegraph standing and carried at a walk, front and rear, in both phases, on the mid loam
    2-walk-cycle.gif        the walk and the walking telegraph played at her cadence (a quarter every 0.25 s: 0.88 world
                            units a cycle at 0.9 a second) at the phone's scale, beside the Regent's and the Warden's
    3-phone-scale.png       idle, walk, telegraph and strike in both phases at the phone's scale (4.6 world units across
                            1080 px) beside the Regent and the Warden (floor 3's bloomed copy, the boss she replaces), on
                            the deep, mid and light ash loam, and a squint
    4-art-set.png           every drawing in the set, row by row as EnemyArtSet holds them
    checks.json             review_rootmother.py's checks (warning red-orange and gloamlight texels, both 0; the orchid
                            share; the idle's luminance against the mid loam's) over the packed set and over the new rows
                            lit as rootmother-01 lit its frames; the memory figures beside the Regent's
Nothing here is generated imagery: it composites the packed sprites and the renders.
"""
import json
import re
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import review_rootmother as first  # noqa: E402  (its checks, its lighting, its loam and its font)

ROOT = Path(__file__).resolve().parents[2]
ARGV = sys.argv[1:]


def option(name, default):
    return ARGV[ARGV.index(name) + 1] if name in ARGV else default


RENDER = Path(option('--render', str(ROOT / 'TestResults/rootmother-render')))
OUT = Path(option('--out', str(ROOT / 'ArtDirection/2026-11-25/rootmother-02')))
first.RENDER = RENDER        # its lighting reads the same renders
CHARACTERS = ROOT / 'Assets/_Project/Art/Characters'
MYRR = CHARACTERS / 'RootmotherMyrr'
PPU = 128
PHONE = first.PHONE_PX_PER_UNIT / PPU          # the S23: 4.6 world units across 1080 px
LOAM = first.LOAM
INK = (230, 226, 214)
QUARTERS = ['step-a', 'pass-a', 'step-b', 'pass-b']
WINDUP_QUARTERS = ['windup-step-a', 'windup-pass-a', 'windup', 'windup-pass-b']
QUARTER_MS = 250
NEW_ROWS = set(QUARTERS) | set(WINDUP_QUARTERS)


class Sprite:
    """A packed sprite: its pixels and its pivot in texels from the top-left."""

    def __init__(self, image, pivot):
        self.image = image
        self.px = pivot[0] * image.width
        self.py = image.height - pivot[1] * image.height

    def scaled(self, factor):
        w, h = max(1, round(self.image.width * factor)), max(1, round(self.image.height * factor))
        out = Sprite(self.image.resize((w, h), Image.LANCZOS), (0.5, 0.5))
        out.px, out.py = self.px * factor, self.py * factor
        return out


def from_meta(png):
    text = Path(str(png) + '.meta').read_text(encoding='utf-8')
    m = re.search(r'spritePivot: \{x: ([0-9.]+), y: ([0-9.]+)\}', text)
    return Sprite(Image.open(png).convert('RGBA'), (float(m.group(1)), float(m.group(2))))


def boss(folder, prefix, facing, pose, hot=False):
    return from_meta(CHARACTERS / folder / ('%s_%s_%s%s.png' % (prefix, facing, pose, '_hot' if hot else '')))


def myrr(facing, pose, hot=False):
    return from_meta(MYRR / ('CHR_Myrr_%s_%s%s.png' % (facing, pose, '_hot' if hot else '')))


def regent(facing, pose):
    return boss('GlassRegent', 'CHR_GlassRegent', facing, pose)


def warden(facing, pose):
    return boss('ForgeWarden/Gloam', 'CHR_Warden', facing, pose)


def layout(frames):
    """One layout for several strips of the same columns (a GIF's frames): each column's widest half, the tallest above
    and below the floor, so nothing shifts or is cut from one frame to the next."""
    halves = [max(round(max(s.px, s.image.width - s.px)) for s in column) for column in zip(*frames)]
    above = max(s.py for frame in frames for s in frame)
    below = max(s.image.height - s.py for frame in frames for s in frame)
    return halves, above, below


def strip(items, background, floor_gap=40, label_size=16, pad=24, fixed=None):
    """Sprites stood side by side on one floor line, each labelled under it; fixed is a layout() shared by others."""
    if fixed is None:
        fixed = layout([[s for _, s in items]])
    halves, above, below = fixed
    width = pad + sum(2 * half + pad for half in halves)
    height = round(above + below) + floor_gap + 16
    panel = Image.new('RGBA', (width, height), background + (255,))
    draw = ImageDraw.Draw(panel)
    x = pad
    floor = round(above) + 8
    for (label, s), half in zip(items, halves):
        panel.alpha_composite(s.image, (round(x + half - s.px), round(floor - s.py)))
        draw.text((x, height - floor_gap + 10), label, fill=INK, font=first.font(label_size))
        x += 2 * half + pad
    return panel


def stack(panels, title, background=(20, 22, 18)):
    width = max(p.width for p in panels)
    sheet = Image.new('RGBA', (width, 44 + sum(p.height for p in panels)), background + (255,))
    ImageDraw.Draw(sheet).text((16, 10), title, fill=INK, font=first.font(22))
    y = 44
    for p in panels:
        sheet.alpha_composite(p, (0, y))
        y += p.height
    return sheet


def walk_sheet():
    panels = []
    for facing in ('front', 'rear'):
        for hot in (False, True):
            phase = 'phase two' if hot else 'phase one'
            items = [('%s idle' % facing, myrr(facing, 'idle', hot))]
            items += [('%s %s' % (q, phase), myrr(facing, q, hot)) for q in QUARTERS]
            items += [('windup (hold)', myrr(facing, 'windup', hot))]
            items += [(q, myrr(facing, q, hot)) for q in WINDUP_QUARTERS if q != 'windup']
            panels.append(strip(items, LOAM['loam']))
    return stack(panels, 'Rootmother Myrr - the walk (contact A, passing A, contact B, passing B) and the telegraph '
                         'carried at a walk (its contact B is the standing hold); 128 texels a unit, mid ash loam')


def phone_items(hot):
    """Myrr's rows at the phone's scale, then the Regent and the Warden."""
    shown = [('Myrr idle', myrr('front', 'idle', hot)), ('step A', myrr('front', 'step-a', hot)),
             ('pass A', myrr('front', 'pass-a', hot)), ('telegraph', myrr('front', 'windup', hot)),
             ('strike', myrr('front', 'strike', hot)), ('rear walk', myrr('rear', 'step-b', hot))]
    shown += [('Glass Regent', regent('front', 'idle')), ('its telegraph', regent('front', 'windup')),
              ('Forge Warden (floor 3)', warden('front', 'idle')), ('its telegraph', warden('front', 'windup'))]
    return [(label, s.scaled(PHONE)) for label, s in shown]


def phone_sheet():
    panels = []
    for tone in ('deep', 'loam', 'light'):
        for hot in (False, True):
            panels.append(strip(phone_items(hot), LOAM[tone], label_size=18))
    mid = panels[2]
    squint = mid.resize((mid.width // 4, mid.height // 4), Image.BOX).filter(ImageFilter.GaussianBlur(1.2))
    squint = squint.resize((squint.width * 2, squint.height * 2), Image.NEAREST)
    holder = Image.new('RGBA', (squint.width + 40, squint.height + 40), (20, 22, 18, 255))
    holder.alpha_composite(squint, (20, 20))
    return stack(panels + [holder], 'Phone scale (S23: 4.6 world units across 1080 px): Myrr in phase one, then phase '
                                    'two, beside the Glass Regent and the Forge Warden, on deep / mid / light ash loam; '
                                    'squint of the mid loam\'s first phase below')


def gif():
    """Each boss walking in place at the phone's scale, a quarter every QUARTER_MS: Myrr in both phases and facings, the
    Regent, the Warden; below, the telegraph carried at a walk."""
    rows = []
    for windup in (False, True):
        quarters = WINDUP_QUARTERS if windup else QUARTERS
        others = ['windup-step-a', 'windup-pass-a', 'windup', 'windup-pass-b'] if windup else \
            ['step-a', 'pass-a', 'step-b', 'pass-b']
        quarters_items = []
        for k in range(4):
            items = [('Myrr', myrr('front', quarters[k])), ('phase two', myrr('front', quarters[k], True)),
                     ('rear', myrr('rear', quarters[k])), ('Regent', regent('front', others[k])),
                     ('Warden (floor 3)', warden('front', others[k]))]
            quarters_items.append([(l, s.scaled(PHONE)) for l, s in items])
        fixed = layout([[s for _, s in items] for items in quarters_items])
        rows.append([strip(items, LOAM['loam'], label_size=18, fixed=fixed) for items in quarters_items])
    width = max(r[0].width for r in rows)
    height = sum(r[0].height for r in rows)
    out = []
    for k in range(4):
        frame = Image.new('RGBA', (width, height), (20, 22, 18, 255))
        y = 0
        for r in rows:
            frame.alpha_composite(r[k], (0, y))
            y += r[k].height
        out.append(frame.convert('RGB').convert('P', palette=Image.ADAPTIVE, colors=160))
    return out


def set_sheet(report):
    groups = {}
    for s in report['sprites']:
        groups.setdefault(s['kind'], []).append(s)
    panels = []
    for kind in ('cool', 'hot', 'flash', 'fall', 'remains'):
        if kind not in groups:
            continue
        for facing in ('front', 'rear', 'both'):
            items = [(s['pose'], from_meta(MYRR / s['file'])) for s in groups[kind] if s['facing'] == facing]
            if items:
                panels.append(strip(items, LOAM['loam'] if kind != 'flash' else (60, 40, 60), label_size=13, pad=10))
    return stack(panels, 'Rootmother Myrr - every drawing in RootmotherMyrrArtSet.asset: phase one, phase two, the flashes, '
                         'the fall and the two remains')


def packed_checks(report, meta):
    """review_rootmother.py's checks over the game's own sprites: every body and every fall drawing (the white flashes,
    which carry no colour, aside), keyed as its render is so the idle's luminance reads her idle-a."""
    sprites = {}
    for s in report['sprites']:
        if s['kind'] == 'flash':
            continue
        variant = 'hot' if s['kind'] in ('hot', 'fall', 'remains') else 'cool'
        pose = report['rendered'].get(s['pose'], s['pose'])
        facing = s['facing'] if s['facing'] != 'both' else 'front'
        assert (facing, pose, variant) not in sprites, s['file']
        sprites[(facing, pose, variant)] = Image.open(MYRR / s['file']).convert('RGBA')
    return first.checks(sprites, meta)


def new_row_checks(meta):
    """The same checks over the new rows' raw frames, lit as rootmother-01 lit its frames (comparable with its
    checks.json), the idle among them for the luminance."""
    sprites = {}
    for r in meta['frames']:
        if r['pose'] in NEW_ROWS or (r['pose'] == 'idle-a' and r['facing'] == 'front' and r['variant'] == 'cool'):
            sprites[(r['facing'], r['pose'], r['variant'])] = first.lit(r, meta['supersample'])
    result = first.checks(sprites, meta)
    result['frames_new_rows'] = sum(1 for k in sprites if k[1] in NEW_ROWS)
    lums = {}
    for key, s in sprites.items():
        if key[1] not in NEW_ROWS or key[2] != 'cool':
            continue
        total = count = 0
        for r_, g_, b_, a_ in s.convert("RGBA").get_flattened_data() if hasattr(s, "get_flattened_data") else s.getdata():
            if a_ >= 128:
                total += 0.2126 * r_ + 0.7152 * g_ + 0.0722 * b_
                count += 1
        lums['%s %s' % key[:2]] = round(total / max(1, count) / 255, 3)
    result['walk_mean_luminance_cool'] = lums
    return result


def main():
    report = json.loads((OUT / 'frames.json').read_text())
    meta = json.loads((RENDER / 'frames.json').read_text())
    OUT.mkdir(parents=True, exist_ok=True)
    walk_sheet().save(OUT / '1-walk-cycle.png')
    frames = gif()
    frames[0].save(OUT / '2-walk-cycle.gif', save_all=True, append_images=frames[1:], duration=QUARTER_MS, loop=0)
    phone_sheet().save(OUT / '3-phone-scale.png')
    set_sheet(report).save(OUT / '4-art-set.png')
    result = dict(packed_set=packed_checks(report, meta), new_rows_lit_as_pass_1=new_row_checks(meta),
                  memory=report['memory'], visual_top=report['visual_top'], shadow_width=report['shadow_width'],
                  walk_cycle_distance=report['walk_cycle_distance'])
    (OUT / 'checks.json').write_text(json.dumps(result, indent=1))
    print('ROOTMOTHER_SET_REVIEW', json.dumps({k: result[k] for k in ('packed_set', 'new_rows_lit_as_pass_1')}))


if __name__ == '__main__':
    main()
