"""Review sheets for the painted between-runs and pause screens (build_screen_art.py).

    python Tools/ArtReview/review_screen_art.py --out DIR [--backdrop-before PNG] [--backdrop-after PNG]

Rebuilds the Relic Forge, the run result and the pause panels from the Gameplay scene itself - every RectTransform's
anchors, pivot, size and position, every Image's sprite, type, colour and pixels-per-unit multiplier, every Button's
colour tint, every Text's size, best fit and alignment - on a 1080x2340 phone (the canvas's 1080-wide reference at one
canvas unit a pixel, its safe area the whole screen), with the texts the game sets (PrototypeText and the content's own
names, descriptions and prices), and draws them twice: as the scene shows them today, and as ScreenArtView restyles them
with the painted set (read back from Art/UI/Screens: the sheet, its meta's rects and 9-slice borders, and the set's
layout). Legacy uGUI is imitated closely enough to judge the art: Sliced images cut at their borders scaled by the
multiplier, Simple images stretched or fitted, colour tints multiplied in, Arial standing in for the built-in font with
Unity's best fit (the largest size between the minimum and the maximum whose wrapped lines fit the rect).

Writes:
  - forge-vanguard-before-after.png, forge-ilya-before-after.png: the forge with the Vanguard's three weapon cards and
    with Ilya's one (WeaponShop.IsOffered), each card in a real state.
  - result-victory-before-after.png, result-defeat-before-after.png, pause-before-after.png.
  - forge-states-x2.png: every card state, the chooser and the buttons' pressed and disabled states, enlarged.
  - sprites-and-borders.png: every painted sprite, enlarged, with its 9-slice borders marked.
"""
import math
import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "Assets"
SCENE = ROOT / "Assets/_Project/Scenes/Gameplay/Gameplay.unity"
SCREENS = ROOT / "Assets/_Project/Art/UI/Screens"
ARGS = sys.argv[1:]


def arg(name, default=None):
    return ARGS[ARGS.index(name) + 1] if name in ARGS else default


OUT = Path(arg("--out", ROOT / "TestResults/screen-art-review"))
W, H = 1080, 2340
FONT = "C:/Windows/Fonts/arial.ttf"
FONT_BOLD = "C:/Windows/Fonts/arialbd.ttf"

IMAGE_GUID = "fe87c0e1cc204ed48ad3b37840f39efc"
TEXT_GUID = "5f7201a12d95ffc409449d95f23cf332"
BUTTON_GUID = "4e29b1a8efbd4b44bb3f3716e73f07ff"


# ----------------------------------------------------------------------------------------------------------------------
# The scene, as Unity serialises it.


def parse_scene():
    text = SCENE.read_text(encoding="utf-8")
    docs = {}
    for m in re.finditer(r"^--- !u!(\d+) &(-?\d+)( stripped)?\n(.*?)(?=^--- |\Z)", text, re.S | re.M):
        docs[m.group(2)] = m.group(4)
    return docs


DOCS = parse_scene()


def field(body, name):
    m = re.search(r"^\s*" + re.escape(name) + r": (.*)$", body, re.M)
    return m.group(1).strip() if m else None


def vec(value):
    return tuple(float(v) for v in re.findall(r"[xyzwrgba]: (-?[\d.e-]+)", value))


def ref_id(value):
    m = re.search(r"fileID: (-?\d+)", value or "")
    return m.group(1) if m else "0"


def ref_guid(value):
    m = re.search(r"guid: ([0-9a-f]{32})", value or "")
    return m.group(1) if m else None


class Node:
    """A GameObject with its RectTransform and the uGUI components this review draws."""

    def __init__(self, go):
        body = DOCS[go]
        self.go = go
        self.name = field(body, "m_Name")
        self.active = field(body, "m_IsActive") == "1"
        self.image = self.text = self.button = None
        self.children = []
        self.rect_id = None
        for comp in re.findall(r"component: \{fileID: (-?\d+)\}", body):
            cb = DOCS.get(comp, "")
            kind = cb.split(":", 1)[0].strip()
            if kind == "RectTransform":
                self.rect_id = comp
                self.anchor_min = vec(field(cb, "m_AnchorMin"))
                self.anchor_max = vec(field(cb, "m_AnchorMax"))
                self.position = vec(field(cb, "m_AnchoredPosition"))
                self.size = vec(field(cb, "m_SizeDelta"))
                self.pivot = vec(field(cb, "m_Pivot"))
                kids = re.search(r"m_Children:(.*?)\n  m_Father", cb, re.S)
                self.child_rects = re.findall(r"fileID: (-?\d+)", kids.group(1)) if kids else []
            elif kind == "MonoBehaviour":
                script = ref_guid(field(cb, "m_Script"))
                if script == IMAGE_GUID:
                    self.image = dict(id=comp, sprite=field(cb, "m_Sprite"), type=int(field(cb, "m_Type")),
                                      color=vec(field(cb, "m_Color")), ppum=float(field(cb, "m_PixelsPerUnitMultiplier")),
                                      preserve=field(cb, "m_PreserveAspect") == "1", enabled=True)
                elif script == TEXT_GUID:
                    self.text = dict(id=comp, size=int(field(cb, "m_FontSize")), min=int(field(cb, "m_MinSize")),
                                     max=int(field(cb, "m_MaxSize")), best=field(cb, "m_BestFit") == "1",
                                     align=int(field(cb, "m_Alignment")), color=vec(field(cb, "m_Color")),
                                     bold=field(cb, "m_FontStyle") == "1", value="")
                elif script == BUTTON_GUID:
                    self.button = dict(id=comp, transition=int(field(cb, "m_Transition")),
                                       normal=vec(field(cb, "m_NormalColor")),
                                       disabled=vec(field(cb, "m_DisabledColor")),
                                       pressed=vec(field(cb, "m_PressedColor")), interactable=True, state="normal")
        for rect in self.child_rects:
            child_go = ref_id(field(DOCS[rect], "m_GameObject"))
            self.children.append(Node(child_go))

    def find(self, path):
        node = self
        for part in path.split("/"):
            node = next(c for c in node.children if c.name == part)
        return node


def find_go(name):
    for key, body in DOCS.items():
        if body.startswith("GameObject:") and field(body, "m_Name") == name:
            return key
    raise KeyError(name)


def panel(name):
    return Node(find_go(name))


# ----------------------------------------------------------------------------------------------------------------------
# Sprites by GUID, read as Unity cuts them.

GUID_PATHS = {}
for meta in ASSETS.rglob("*.meta"):
    if meta.suffix == ".meta" and meta.stem.split(".")[-1] in ("png", "tga"):
        m = re.search(r"^guid: ([0-9a-f]{32})", meta.read_text(encoding="utf-8", errors="ignore"), re.M)
        if m:
            GUID_PATHS[m.group(1)] = meta.with_suffix("")


class Sprite:
    def __init__(self, name, rgba, border, ppu):
        self.name, self.rgba, self.border, self.ppu = name, rgba, border, ppu

    @property
    def size(self):
        return self.rgba.shape[1], self.rgba.shape[0]


_SPRITES = {}


def load_texture(path):
    image = Image.open(path).convert("RGBA")
    rgba = np.asarray(image).astype(np.float32) / 255.0
    rgba = rgba.copy()
    rgba[..., :3] *= rgba[..., 3:]  # premultiplied from here on
    return rgba


def sprite_by_ref(value):
    """The sprite a serialised reference names ({fileID, guid}): a single sprite or one cut from a Multiple sheet."""
    guid, file_id = ref_guid(value), ref_id(value)
    if guid is None or file_id == "0":
        return None
    key = (guid, file_id)
    if key in _SPRITES:
        return _SPRITES[key]
    path = GUID_PATHS[guid]
    meta = Path(str(path) + ".meta").read_text(encoding="utf-8")
    rgba = load_texture(path)
    ppu = float(re.search(r"spritePixelsToUnits: ([\d.]+)", meta).group(1))
    if re.search(r"^  spriteMode: 2", meta, re.M):
        pattern = (r"      name: (\S+)\n      rect:\n        serializedVersion: 2\n        x: (\d+)\n        y: (\d+)\n"
                   r"        width: (\d+)\n        height: (\d+)\n(?:.*\n)*?      border: \{x: (\d+), y: (\d+), z: (\d+), "
                   r"w: (\d+)\}\n(?:.*\n)*?      internalID: (-?\d+)")
        for m in re.finditer(pattern, meta):
            if m.group(10) == file_id:
                x, y, w, h = map(int, m.group(2, 3, 4, 5))
                top = rgba.shape[0] - y - h
                sprite = Sprite(m.group(1), rgba[top:top + h, x:x + w], tuple(map(int, m.group(6, 7, 8, 9))), ppu)
                break
        else:
            raise KeyError(f"{path}: no sprite {file_id}")
    else:
        border = re.search(r"spriteBorder: (\{.*\})", meta)
        b = vec(border.group(1)) if border else (0, 0, 0, 0)
        sprite = Sprite(path.stem, rgba, tuple(int(v) for v in b), ppu)
    _SPRITES[key] = sprite
    return sprite


def asset_sprite(path_in_assets, file_id=21300000):
    meta = (ROOT / path_in_assets).with_name((ROOT / path_in_assets).name + ".meta").read_text(encoding="utf-8")
    guid = re.search(r"^guid: ([0-9a-f]{32})", meta, re.M).group(1)
    return sprite_by_ref(f"{{fileID: {file_id}, guid: {guid}, type: 3}}")


# ----------------------------------------------------------------------------------------------------------------------
# A tiny uGUI: premultiplied RGBA composited onto a float canvas, image order (row 0 at the top).


def resize(rgba, w, h):
    w, h = max(1, int(round(w))), max(1, int(round(h)))
    if (w, h) == (rgba.shape[1], rgba.shape[0]):
        return rgba
    channels = [np.asarray(Image.fromarray(np.ascontiguousarray(rgba[..., k]), "F").resize((w, h), Image.BILINEAR))
                for k in range(4)]
    return np.stack(channels, -1)


def blit(canvas, rgba, x, y, tint=(1, 1, 1, 1)):
    x, y = int(round(x)), int(round(y))
    h, w = rgba.shape[:2]
    x0, y0, x1, y1 = max(0, x), max(0, y), min(canvas.shape[1], x + w), min(canvas.shape[0], y + h)
    if x0 >= x1 or y0 >= y1:
        return
    t = np.asarray(tint, np.float32)
    src = rgba[y0 - y:y1 - y, x0 - x:x1 - x] * np.array([t[0] * t[3], t[1] * t[3], t[2] * t[3], t[3]], np.float32)
    dst = canvas[y0:y1, x0:x1]
    dst[..., :3] = src[..., :3] + dst[..., :3] * (1 - src[..., 3:])
    dst[..., 3:] = src[..., 3:] + dst[..., 3:] * (1 - src[..., 3:])


def sliced(sprite, w, h, ppum=1.0):
    """A Sliced image: corners kept, edges and centre stretched, the borders in canvas units = texels / (the sprite's
    density over the canvas's 100) / the multiplier, scaled down as Unity does when the rect is smaller than them."""
    l, b, r, t = (v / (sprite.ppu / 100.0) / ppum for v in sprite.border)
    sw, sh = sprite.size
    w, h = int(round(w)), int(round(h))
    if w <= 0 or h <= 0:
        return np.zeros((1, 1, 4), np.float32)
    if l + r > w:
        k = w / (l + r)
        l, r = l * k, r * k
    if b + t > h:
        k = h / (b + t)
        b, t = b * k, t * k
    bl, bb, br, bt = sprite.border
    out = np.zeros((h, w, 4), np.float32)
    xs = [(0, bl, 0, l), (bl, sw - br, l, w - r), (sw - br, sw, w - r, w)]
    ys = [(0, bt, 0, t), (bt, sh - bb, t, h - b), (sh - bb, sh, h - b, h)]
    for sy0, sy1, dy0, dy1 in ys:
        for sx0, sx1, dx0, dx1 in xs:
            dx0i, dx1i, dy0i, dy1i = int(round(dx0)), int(round(dx1)), int(round(dy0)), int(round(dy1))
            if sx1 <= sx0 or sy1 <= sy0 or dx1i <= dx0i or dy1i <= dy0i:
                continue
            out[dy0i:dy1i, dx0i:dx1i] = resize(sprite.rgba[sy0:sy1, sx0:sx1], dx1i - dx0i, dy1i - dy0i)
    return out


def to_canvas(rgb):
    out = np.ones(rgb.shape[:2] + (4,), np.float32)
    out[..., :3] = np.asarray(rgb, np.float32) / 255.0
    return out


def to_image(canvas):
    return Image.fromarray((np.clip(canvas[..., :3], 0, 1) * 255 + 0.5).astype(np.uint8), "RGB")


# ----------------------------------------------------------------------------------------------------------------------
# Layout: a RectTransform's rect in its parent's (canvas units, bottom-left origin).


def layout(node, parent):
    px, py, pw, ph = parent
    ax0, ay0 = px + node.anchor_min[0] * pw, py + node.anchor_min[1] * ph
    ax1, ay1 = px + node.anchor_max[0] * pw, py + node.anchor_max[1] * ph
    w, h = ax1 - ax0 + node.size[0], ay1 - ay0 + node.size[1]
    pivot_x = ax0 + (ax1 - ax0) * node.pivot[0] + node.position[0]
    pivot_y = ay0 + (ay1 - ay0) * node.pivot[1] + node.position[1]
    node.rect = (pivot_x - w * node.pivot[0], pivot_y - h * node.pivot[1], w, h)
    for child in node.children:
        layout(child, node.rect)


def screen_box(rect):
    """(left, top, width, height) in image order on the phone."""
    x, y, w, h = rect
    return x, H - (y + h), w, h


# ----------------------------------------------------------------------------------------------------------------------
# Text: Arial for the built-in font, Unity's best fit, wrapping and alignment.

_FONTS = {}


def font(size, bold):
    key = (size, bold)
    if key not in _FONTS:
        _FONTS[key] = ImageFont.truetype(FONT_BOLD if bold else FONT, size)
    return _FONTS[key]


def wrap(words, size, bold, width):
    lines = []
    for paragraph in words.split("\n"):
        line = ""
        for word in paragraph.split(" "):
            trial = word if not line else line + " " + word
            if font(size, bold).getlength(trial) <= width or not line:
                line = trial
            else:
                lines.append(line)
                line = word
        lines.append(line)
    return lines


def runs(value):
    """Lines of (text, size or None) from Unity's rich text, which the chooser uses for its second line only."""
    out = []
    for line in value.split("\n"):
        m = re.fullmatch(r"<size=(\d+)>(.*)</size>", line)
        out.append((m.group(2), int(m.group(1))) if m else (line, None))
    return out


def fitted(t, width, height):
    """The size best fit settles on and the wrapped lines with their sizes."""
    sizes = range(t["max"], t["min"] - 1, -1) if t["best"] else [t["size"]]
    for size in sizes:
        lines = []
        for words, fixed in runs(t["value"]):
            s = fixed or size
            lines += [(line, s) for line in wrap(words, s, t["bold"], width)]
        if sum(1.15 * s for _, s in lines) <= height + 0.5 or size == sizes[-1]:
            return size, lines
    raise AssertionError


def draw_text(canvas, node, colour=None):
    t = node.text
    if not t["value"]:
        return
    x, y, w, h = screen_box(node.rect)
    size, lines = fitted(t, w, h)
    total = sum(1.15 * s for _, s in lines)
    halign, valign = t["align"] % 3, t["align"] // 3
    top = y + {0: 0, 1: (h - total) / 2, 2: h - total}[valign]
    col = np.asarray(colour if colour is not None else t["color"][:3], np.float32)
    layer = Image.new("L", (W, H))
    d = ImageDraw.Draw(layer)
    cursor = top
    for line, s in lines:
        f = font(s, t["bold"])
        lw = f.getlength(line)
        lx = x + {0: 0, 1: (w - lw) / 2, 2: w - lw}[halign]
        baseline = cursor + (1.15 * s - 1.117 * s) / 2 + 0.905 * s
        d.text((lx, baseline), line, font=f, fill=255, anchor="ls")
        cursor += 1.15 * s
    a = np.asarray(layer, np.float32) / 255.0
    rgba = np.zeros((H, W, 4), np.float32)
    rgba[..., :3] = col * a[..., None]
    rgba[..., 3] = a
    blit(canvas, rgba, 0, 0)


# ----------------------------------------------------------------------------------------------------------------------
# Drawing a panel as uGUI does: depth first, each node's Image then its Text, then its children.


def tint_of(node):
    b = node.button
    if b is None or b["transition"] != 1:
        return (1, 1, 1, 1)
    if not b["interactable"]:
        return b["disabled"]
    return b["pressed"] if b["state"] == "pressed" else b["normal"]


def draw_image(canvas, node, sprite, kind, colour, ppum=1.0, preserve=False):
    x, y, w, h = screen_box(node.rect)
    if sprite is None:
        rgba = np.ones((max(1, int(round(h))), max(1, int(round(w))), 4), np.float32)
    elif kind == 1:
        rgba = sliced(sprite, w, h, ppum)
    elif preserve:
        sw, sh = sprite.size
        k = min(w / sw, h / sh)
        rgba = resize(sprite.rgba, sw * k, sh * k)
        x, y = x + (w - sw * k) / 2, y + (h - sh * k) / 2
    else:
        rgba = resize(sprite.rgba, w, h)
    blit(canvas, rgba, x, y, colour)


def draw(canvas, node):
    if not node.active:
        return
    img = node.image
    if img is not None and img["enabled"]:
        sprite = img["painted"] if "painted" in img else sprite_by_ref(img["sprite"])
        colour = np.asarray(img["color"], np.float32) * np.asarray(tint_of(node), np.float32)
        draw_image(canvas, node, sprite, img["type"], colour, img["ppum"], img["preserve"])
    if node.text is not None:
        draw_text(canvas, node)
    for child in node.children:
        draw(canvas, child)


# ----------------------------------------------------------------------------------------------------------------------
# What the game writes into the panels (PrototypeText, the content's names, descriptions and prices), on a phone in the
# owner's Turkish locale (decimal commas, as the forge capture shows).

INK = (0.93333334, 0.9098039, 0.8509804)
STATE_COLOURS = {"equipped": (137, 213, 180), "affordable": (238, 191, 108), "owned": (145, 188, 225),
                 "expensive": (163, 167, 183)}
WEAPONS = [("Sword", "10 damage every 0,8s. Cleaves a second enemy for 60%", 0),
           ("Staff", "11 damage every 1,1s. Blasts every enemy near the target for 75%", 120),
           ("Daggers", "6 damage every 0,5s. Crits for 200% once every 3 strikes", 180)]
RELICS = [("Second Wind", "Once per run, at 25% health or less, restore 25% of your health", 80,
           "Assets/_Project/Art/UI/UI_Icon_SecondWind_v1.png"),
          ("Counterweight", "When an enemy hits you, strike back for 60% of your weapon damage", 150,
           "Assets/_Project/Art/UI/UI_Icon_Counterweight_v1.png")]
VANGUARD_PORTRAIT = "Assets/_Project/Art/Characters/Vanguard/CHR_Vanguard_Front_Idle.tga"
ILYA_PORTRAIT = ("Assets/_Project/Art/Characters/CinderArcanist/CHR_CinderArcanist_1.png", -162986488548649134)


def state_text(state, price, need):
    return {"equipped": "Equipped", "owned": "Owned: tap to equip", "affordable": f"Forge for {price} gold",
            "expensive": f"{price} gold: need {need} more"}[state]


class Synthetic:
    """A node built in code (HeroChoiceView's chooser, ScreenArtView's painted parts): its rect given straight from
    anchors and offsets."""

    def __init__(self, name, anchor_min, anchor_max, offset_min, offset_max):
        self.name, self.active = name, True
        self.anchor_min, self.anchor_max, self.offset_min, self.offset_max = anchor_min, anchor_max, offset_min, offset_max
        self.image = self.text = self.button = None
        self.children = []

    def place(self, parent):
        px, py, pw, ph = parent
        x0 = px + self.anchor_min[0] * pw + self.offset_min[0]
        y0 = py + self.anchor_min[1] * ph + self.offset_min[1]
        x1 = px + self.anchor_max[0] * pw + self.offset_max[0]
        y1 = py + self.anchor_max[1] * ph + self.offset_max[1]
        self.rect = (x0, y0, x1 - x0, y1 - y0)

    def find(self, path):
        node = self
        for part in path.split("/"):
            node = next(c for c in node.children if c.name == part)
        return node


def image_dict(sprite=None, kind=0, colour=(1, 1, 1, 1), ppum=1.0, preserve=False):
    d = dict(sprite="{fileID: 0}", type=kind, color=colour, ppum=ppum, preserve=preserve, enabled=True)
    if sprite is not None:
        d["painted"] = sprite
    return d


def hero_chooser(safe, hero):
    """HeroChoiceView.Initialize on the forge's start row: the chooser left of the start button, copying its sprite and
    type (and so the multiplier's default of 1), its colour at 80%."""
    start = safe.find("Start Run Button")
    margin = -start.size[0] / 2
    chooser = Synthetic("Hero Choice", (0, start.anchor_min[1]), (0.4, start.anchor_max[1]), (margin, 0), (-8, 0))
    chooser.image = image_dict(sprite_by_ref(start.image["sprite"]), 1, (0.8, 0.8, 0.8, 1), 1.0)
    chooser.button = dict(transition=1, normal=(1, 1, 1, 1), pressed=(0.784, 0.784, 0.784, 1),
                          disabled=(0.784, 0.784, 0.784, 0.5), interactable=True, state="normal")
    if hero == "vanguard":
        portrait, words = asset_sprite(VANGUARD_PORTRAIT), "Vanguard\n<size=20></size>"
    else:
        portrait, words = asset_sprite(*ILYA_PORTRAIT), "Cinder Arcanist\n<size=20>Ilya Kest</size>"
    picture = Synthetic("Hero Choice Portrait", (0, 0), (0.3, 1), (10, 6), (0, -6))
    picture.image = image_dict(portrait, 0, preserve=True)
    words_node = Synthetic("Hero Choice Label", (0.3, 0), (1, 1), (16, 6), (-16, -6))
    words_node.text = dict(size=30, min=18, max=30, best=True, align=4, color=INK, bold=False, value=words)
    chooser.children = [picture, words_node]
    chooser.chosen, chooser.heroes = (0 if hero == "vanguard" else 1), 2
    # The start button narrows to the row's right.
    start.anchor_min = (0.4, start.anchor_min[1])
    start.synthetic_offsets = ((8, 0), (-margin, 0))
    safe.children.insert(safe.children.index(start), chooser)
    return chooser


def layout_one(node, parent):
    px, py, pw, ph = parent
    ax0, ay0 = px + node.anchor_min[0] * pw, py + node.anchor_min[1] * ph
    ax1, ay1 = px + node.anchor_max[0] * pw, py + node.anchor_max[1] * ph
    w, h = ax1 - ax0 + node.size[0], ay1 - ay0 + node.size[1]
    pivot_x = ax0 + (ax1 - ax0) * node.pivot[0] + node.position[0]
    pivot_y = ay0 + (ay1 - ay0) * node.pivot[1] + node.position[1]
    node.rect = (pivot_x - w * node.pivot[0], pivot_y - h * node.pivot[1], w, h)


def place(node, parent):
    """Lays a node out, with the start button's offsets as HeroChoiceView rewrites them."""
    if isinstance(node, Synthetic):
        node.place(parent)
    elif getattr(node, "synthetic_offsets", None):
        (l, b), (r, t) = node.synthetic_offsets
        px, py, pw, ph = parent
        x0, y0 = px + node.anchor_min[0] * pw + l, py + node.anchor_min[1] * ph + b
        x1, y1 = px + node.anchor_max[0] * pw + r, py + node.anchor_max[1] * ph + t
        node.rect = (x0, y0, x1 - x0, y1 - y0)
    else:
        layout_one(node, parent)
    for child in node.children:
        place(child, node.rect)


def set_text(node, value, colour=None):
    node.text["value"] = value
    if colour is not None:
        node.text["color"] = tuple(c / 255.0 for c in colour) if max(colour) > 1 else tuple(colour)


FORGE_SCENES = {
    # The Vanguard's whole rack, every state on show: the Sword he carries, the Staff he owns, the Daggers out of reach,
    # Second Wind worn and Counterweight ready to forge.
    "vanguard": dict(gold=160, floor=2, weapons=["equipped", "owned", "expensive"], relics=["equipped", "affordable"]),
    # Ilya carries her own Staff alone (WeaponShop.IsOffered): one card; Second Wind owned, Counterweight out of reach.
    "ilya": dict(gold=90, floor=3, weapons=[None, "equipped", None], relics=["owned", "expensive"]),
}


def fill_card(card, name, words, state, price, gold):
    set_text(card.find("Name Label"), name)
    set_text(card.find("Description Label"), words)
    set_text(card.find("State Label"), state_text(state, price, max(0, price - gold)), STATE_COLOURS[state])
    accent = card.find("State Accent")
    accent.image["color"] = tuple(c / 255.0 for c in STATE_COLOURS[state]) + (1.0,)
    card.button["interactable"] = state != "expensive"


def forge_scene(hero):
    scene = FORGE_SCENES[hero]
    root = panel("Forge Panel")
    root.active = True
    safe = root.find("Safe Area")
    set_text(safe.find("Forge Title"), "Relic Forge")
    set_text(safe.find("Forge Status Label"), f"Gold {scene['gold']}  |  Highest floor cleared: {scene['floor']}")
    set_text(safe.find("Weapons Header"), "Weapons: carry one")
    set_text(safe.find("Relics Header"), "Relics: equip one")
    set_text(safe.find("Start Run Button/Start Run Label"), "Start run")
    for i, state in enumerate(scene["weapons"]):
        card = safe.find(f"Weapon Card {i + 1}")
        card.state = state
        if state is None:
            card.active = False
            continue
        name, words, price = WEAPONS[i]
        fill_card(card, name, words, state, price, scene["gold"])
    for i, state in enumerate(scene["relics"]):
        card = safe.find(f"Relic Card {i + 1}")
        card.state = state
        name, words, price, icon = RELICS[i]
        fill_card(card, name, words, state, price, scene["gold"])
        card.find("Relic Artwork").image["painted"] = asset_sprite(icon)
    hero_chooser(safe, hero)
    return root


RESULT_SCENES = {
    "victory": dict(title="Ascent complete", colour=(0.95, 0.8, 0.35),
                    cause="Vanguard defeated the Forge Warden and cleared The Foundry",
                    progress="Floor 3  |  14 rooms  |  Level 11  |  XP 412", gold="Gold banked: 186",
                    build="Build: Sword  |  Second Wind (1x), Tempered Edge x3, Quickened Grip x2, Iron Skin x1",
                    hint="Forge gold 346: Daggers is ready to forge"),
    "defeat": dict(title="Defeated", colour=(1.0, 0.55, 0.35), cause="Cinder Arcanist fell to a Grunt Captain in Ember Hall",
                   progress="Floor 2  |  7 rooms  |  Level 6  |  XP 131", gold="Gold banked: 42  |  lost: 25",
                   build="Build: Staff  |  Tempered Edge x2, Quickened Grip x1",
                   hint="Forge gold 132: Counterweight costs 150"),
}


def result_scene(outcome):
    scene = RESULT_SCENES[outcome]
    root = panel("Result Panel")
    root.active = True
    safe = root.find("Safe Area")
    set_text(safe.find("Result Title"), scene["title"], scene["colour"])
    for name, key in (("Cause Label", "cause"), ("Progress Label", "progress"), ("Result Gold Label", "gold"),
                      ("Build Label", "build"), ("Forge Hint Label", "hint")):
        set_text(safe.find(name), scene[key])
    set_text(safe.find("Restart Button/Restart Label"), "Try again")
    set_text(safe.find("Forge Button/Forge Button Label"), "Relic Forge")
    return root


def pause_scene():
    root = panel("Pause Panel")
    root.active = True
    safe = root.find("Safe Area")
    set_text(safe.find("Pause Title"), "Paused")
    set_text(safe.find("Pause Hint Label"), "Combat waits until you resume")
    set_text(safe.find("Resume Button/Resume Label"), "Resume")
    return root


# ----------------------------------------------------------------------------------------------------------------------
# ScreenArtView, as the C# lays it: restyled images in place, and painted parts placed by anchors and offsets relative
# to the scene's own rects, so they follow the safe area as the scene's rects do.


def load_set():
    text = (SCREENS / "ScreenArtSet.asset").read_text(encoding="utf-8")
    sprites, values = {}, {}
    for m in re.finditer(r"^  (_\w+): (.*)$", text, re.M):
        name, value = m.groups()
        if value.startswith("{fileID:"):
            sprites[name] = sprite_by_ref(value)
        elif value.startswith("{x:"):
            values[name] = vec(value)
        else:
            values[name] = float(value)
    return sprites, values


def offsets_of(node):
    if isinstance(node, Synthetic):
        return node.offset_min, node.offset_max
    (px, py), (sx, sy), (vx, vy) = node.position, node.size, node.pivot
    return (px - sx * vx, py - sy * vy), (px + sx * (1 - vx), py + sy * (1 - vy))


def around(name, target, outset):
    """ScreenArtView.Around: the target's anchors, its offsets pushed out by the outset (left, bottom, right, top)."""
    (l, b), (r, t) = offsets_of(target)
    return Synthetic(name, target.anchor_min, target.anchor_max, (l - outset[0], b - outset[1]),
                     (r + outset[2], t + outset[3]))


def centre_on(name, target, size):
    """ScreenArtView.CentreOn: a fixed size centred on the target, anchored at its anchors' middle."""
    (l, b), (r, t) = offsets_of(target)
    mid = ((target.anchor_min[0] + target.anchor_max[0]) / 2, (target.anchor_min[1] + target.anchor_max[1]) / 2)
    cx, cy = (l + r) / 2, (b + t) / 2
    return Synthetic(name, mid, mid, (cx - size[0] / 2, cy - size[1] / 2), (cx + size[0] / 2, cy + size[1] / 2))


def painted(node, sprite, kind=1, colour=(1, 1, 1, 1), preserve=False):
    node.image = image_dict(sprite, kind, colour, 1.0, preserve)
    return node


def before(parent, target, node):
    parent.children.insert(parent.children.index(target), node)
    return node


def restyle(node, sprite):
    node.image.update(painted=sprite, type=1, ppum=1.0, color=(1, 1, 1, 1))


CARD_SPRITES = {"equipped": "_equippedCard", "affordable": "_affordableCard", "owned": "_ownedCard",
                "expensive": "_lockedCard"}
# The cards keep their colour tint, neutral now that each state has its own frame: pressed darkens, and a card that
# cannot be tapped (out of reach, or for the moment after each change) is dimmed a little more.
CARD_PRESSED, CARD_DISABLED = (0.8, 0.8, 0.8, 1), (0.86, 0.86, 0.86, 1)
LOCKED_ICON = (0.55, 0.55, 0.6, 1)


def button(node, art, kind):
    """A sprite swap: the state's own plate, no tint."""
    b = node.button
    state = "Disabled" if not b["interactable"] else "Pressed" if b["state"] == "pressed" else ""
    restyle(node, art[f"_{kind}{state}"])
    b["transition"] = 2


def paint_forge(root, art, values):
    safe = root.find("Safe Area")
    restyle(safe.find("Forge Surface"), art["_surface"])
    title = safe.find("Forge Title")
    before(safe, title, painted(centre_on("Painted Header Plate", title,
                                          (values["_headerPlateWidth"], art["_headerPlate"].size[1])), art["_headerPlate"]))
    for name in ("Weapons Header", "Relics Header"):
        header = safe.find(name)
        rule = Synthetic("Painted Header Rule", (0, 0), (1, 0), (0, values["_headerRuleLift"] - art["_rule"].size[1] / 2),
                         (0, values["_headerRuleLift"] + art["_rule"].size[1] / 2))
        header.children.append(painted(rule, art["_rule"]))
    for i in range(3):
        card = safe.find(f"Weapon Card {i + 1}")
        empty = around(f"Painted Empty Place {i + 1}", card, (0, 0, 0, 0))
        share = values["_emptySlotRuleShare"]
        rh = art["_rule"].size[1]
        empty.children = [painted(Synthetic("Rule", (0.5 - share / 2, 0.5), (0.5 + share / 2, 0.5), (0, -rh / 2), (0, rh / 2)),
                                  art["_rule"]),
                          painted(centre_on("Seal", Synthetic("", (0.5, 0.5), (0.5, 0.5), (0, 0), (0, 0)),
                                            art["_seal"].size), art["_seal"], 0)]
        empty.active = not card.active
        before(safe, card, empty)
    for prefix, count in (("Weapon Card", 3), ("Relic Card", 2)):
        for i in range(count):
            card = safe.find(f"{prefix} {i + 1}")
            if not card.active:
                continue
            restyle(card, art[CARD_SPRITES[card.state]])
            b = card.button
            b.update(normal=(1, 1, 1, 1), pressed=CARD_PRESSED, disabled=CARD_DISABLED)
            accent = card.find("State Accent")
            accent.image.update(painted=art["_stateAccent"], type=1, ppum=1.0)
            if prefix == "Relic Card":
                artwork = card.find("Relic Artwork")
                m = values["_iconWellMargin"]
                well = before(card, artwork, painted(around("Painted Icon Well", artwork, (m, m, m, m)), art["_iconWell"],
                                                     0, preserve=True))
                if card.state == "expensive":
                    well.image["color"] = LOCKED_ICON
                    artwork.image["color"] = LOCKED_ICON
    button(safe.find("Start Run Button"), art, "primary")
    chooser = safe.find("Hero Choice")
    button(chooser, art, "secondary")
    portrait = chooser.find("Hero Choice Portrait")
    before(chooser, portrait, painted(around("Painted Portrait Frame", portrait, values["_portraitFrameOutset"]),
                                      art["_portraitFrame"]))
    spacing, size = values["_pipSpacing"], art["_pipLit"].size
    heroes = getattr(chooser, "heroes", 2)
    for k in range(heroes):
        x = (k - (heroes - 1) / 2) * spacing
        pip = Synthetic(f"Painted Hero Pip {k + 1}", (0.65, 0), (0.65, 0), (x - size[0] / 2, -size[1] / 2),
                        (x + size[0] / 2, size[1] / 2))
        chooser.children.append(painted(pip, art["_pipLit"] if k == chooser.chosen else art["_pipDark"], 0))


def paint_result(root, art, values, outcome):
    safe = root.find("Safe Area")
    restyle(safe.find("Result Surface"), art["_surface"])
    title = safe.find("Result Title")
    victory = outcome != "defeat"
    plate = painted(around("Painted Title Plate", title, values["_titlePlateOutset"]),
                    art["_victoryPlate"] if victory else art["_defeatPlate"])
    crest_sprite = art["_victoryCrest"] if victory else art["_defeatCrest"]
    cw, ch = crest_sprite.size
    sink = values["_crestSink"]
    plate.children = [painted(Synthetic("Painted Title Crest", (0.5, 1), (0.5, 1), (-cw / 2, -sink), (cw / 2, ch - sink)),
                              crest_sprite, 0)]
    before(safe, title, plate)
    divider = safe.find("Result Divider")
    divider.image["enabled"] = False
    rh = art["_rule"].size[1]
    divider.children = [painted(Synthetic("Painted Rule", (0, 0.5), (1, 0.5), (0, -rh / 2), (0, rh / 2)), art["_rule"]),
                        painted(centre_on("Painted Stud", Synthetic("", (0.5, 0.5), (0.5, 0.5), (0, 0), (0, 0)),
                                          art["_stud"].size), art["_stud"], 0)]
    button(safe.find("Restart Button"), art, "primary")
    button(safe.find("Forge Button"), art, "secondary")


def paint_pause(root, art, values):
    safe = root.find("Safe Area")
    title, hint = safe.find("Pause Title"), safe.find("Pause Hint Label")
    (tl, tb), (tr, tt) = offsets_of(title)
    (hl, hb), (hr, ht) = offsets_of(hint)
    union = Synthetic("", title.anchor_min, title.anchor_max, (min(tl, hl), min(tb, hb)), (max(tr, hr), max(tt, ht)))
    before(safe, title, painted(around("Painted Pause Plate", union, values["_pausePlateOutset"]), art["_pausePlate"]))
    button(safe.find("Resume Button"), art, "primary")


def render(root, backdrop=None):
    place(root, (0, 0, W, H))
    canvas = to_canvas(backdrop if backdrop is not None else np.zeros((H, W, 3), np.uint8))
    draw(canvas, root)
    return to_image(canvas)


def label(image, words, at, size=34, colour=(235, 228, 210)):
    ImageDraw.Draw(image).text(at, words, font=ImageFont.truetype(FONT, size), fill=colour)


def side_by_side(before_image, after_image, name, title_before, title_after):
    sheet = Image.new("RGB", (W * 2 + 60, H + 90), (24, 22, 28))
    sheet.paste(before_image, (20, 70))
    sheet.paste(after_image, (W + 40, 70))
    label(sheet, title_before, (20, 20))
    label(sheet, title_after, (W + 40, 20))
    sheet.save(OUT / f"{name}-before-after.png")
    after_image.save(OUT / f"{name}-after.png")


def backdrops():
    """The run behind the result and the pause (both overlays let it show through): a capture with today's HUD for
    before, the same frame with the painted HUD for after, when given; else the dark the panels are drawn on."""
    pair = []
    for key in ("--backdrop-before", "--backdrop-after"):
        path = arg(key)
        pair.append(np.asarray(Image.open(path).convert("RGB").resize((W, H))) if path else None)
    return pair


def scenes():
    art, values = load_set()
    back_before, back_after = backdrops()
    out = {}
    for hero in ("vanguard", "ilya"):
        old = render(forge_scene(hero))
        root = forge_scene(hero)
        chooser = root.find("Safe Area/Hero Choice")
        chooser.chosen, chooser.heroes = (0 if hero == "vanguard" else 1), 2
        paint_forge(root, art, values)
        out[f"forge-{hero}"] = (old, render(root), root)
    for outcome in ("victory", "defeat"):
        old = render(result_scene(outcome), back_before)
        root = result_scene(outcome)
        paint_result(root, art, values, outcome)
        out[f"result-{outcome}"] = (old, render(root, back_after), root)
    old = render(pause_scene(), back_before)
    root = pause_scene()
    paint_pause(root, art, values)
    out["pause"] = (old, render(root, back_after), root)
    return out


TITLES = {
    "forge-vanguard": "The Vanguard's forge: Sword equipped, Staff owned, Daggers out of reach (160 of 180); "
                      "Second Wind worn, Counterweight ready to forge.",
    "forge-ilya": "Ilya's forge: her Staff alone (WeaponShop.IsOffered); Second Wind owned, Counterweight out of reach.",
    "result-victory": "The run result, a victory (an extraction wears the same).",
    "result-defeat": "The run result, a defeat (gold lost).",
    "pause": "The pause, over the fight (the painted HUD behind it after).",
}


def phone_sheet(before_image, after_image, name):
    sheet = Image.new("RGB", (W * 2 + 60, H + 130), (24, 22, 28))
    sheet.paste(before_image, (20, 110))
    sheet.paste(after_image, (W + 40, 110))
    label(sheet, "before: today's look, rebuilt from the scene", (20, 16))
    label(sheet, "after: painted (ScreenArtView with ScreenArtSet)", (W + 40, 16))
    label(sheet, TITLES[name], (20, 62), 30, (190, 184, 170))
    sheet.save(OUT / f"{name}-before-after.png")
    after_image.save(OUT / f"{name}-after.png")


def crop(image, node, pad=14):
    x, y, w, h = screen_box(node.rect)
    return image.crop((int(x - pad), int(y - pad), int(x + w + pad), int(y + h + pad)))


def zoom(image, factor):
    return image.resize((image.width * factor, image.height * factor), Image.NEAREST)


def states_sheet(shots, art, values):
    """Every card state from the Vanguard's forge at phone size with its left end enlarged x2; both buttons in their
    normal, pressed and disabled plates; and the chooser with each hero chosen, pressed and disabled."""
    _, after_image, root = shots["forge-vanguard"]
    area = root.find("Safe Area")
    rows = []
    for name, words in (("Weapon Card 1", "Equipped"), ("Relic Card 2", "Affordable"), ("Weapon Card 2", "Owned"),
                        ("Weapon Card 3", "Locked (too expensive), its tint dimmed as the forge disables it")):
        card = crop(after_image, area.find(name))
        rows.append((f"{words}", card, zoom(card.crop((0, 0, 330, card.height)), 2)))
    # The buttons: each plate at the start button's size and at the result's, drawn on the phone and cut out.
    tiles = []
    for kind, size, words in (("primary", (520, 166), "Start run"), ("secondary", (520, 124), "Relic Forge")):
        for state in ("", "Pressed", "Disabled"):
            canvas = to_canvas(np.full((H, W, 3), (12, 18, 35), np.uint8))
            blit(canvas, sliced(art[f"_{kind}{state}"], size[0], size[1]), 20, 20)
            words_node = Synthetic("", (0, 0), (0, 0), (0, 0), (0, 0))
            words_node.rect = (20 + 24, H - 20 - size[1] * 0.88, size[0] - 48, size[1] * 0.76)
            words_node.text = dict(size=42, min=22, max=42, best=True, align=4, color=INK, bold=True, value=words)
            draw_text(canvas, words_node)
            tiles.append((f"{kind} {state.lower() or 'normal'}", to_image(canvas).crop((0, 0, size[0] + 40, size[1] + 40))))
    choosers = []
    for hero in ("vanguard", "ilya"):
        _, image, scene_root = shots[f"forge-{hero}"]
        chooser = scene_root.find("Safe Area/Hero Choice")
        choosers.append((f"{'the Vanguard' if hero == 'vanguard' else 'Ilya'} chosen", crop(image, chooser, 18)))
        for state, sprite in (("pressed", "_secondaryPressed"), ("disabled", "_secondaryDisabled")):
            canvas = to_canvas(np.full((H, W, 3), (12, 18, 35), np.uint8))
            chooser.image["painted"] = art[sprite]
            draw(canvas, chooser)
            chooser.image["painted"] = art["_secondary"]
            choosers.append((f"{hero}, {state}", crop(to_image(canvas), chooser, 18)))
    width = 1700
    height = sum(max(r[1].height, r[2].height) + 50 for r in rows) + 2 * (210 + 50) + 2 * (210 + 50) + 80
    sheet = Image.new("RGB", (width, height), (24, 22, 28))
    y = 10
    label(sheet, "The forge's card states at phone size, and their left ends x2", (10, y), 28)
    y += 46
    for words, card, big in rows:
        label(sheet, words, (10, y), 24, (190, 184, 170))
        sheet.paste(card, (10, y + 34))
        sheet.paste(big.crop((0, 0, min(big.width, width - card.width - 40), big.height)), (card.width + 30, y + 34))
        y += max(card.height, big.height) + 50
    label(sheet, "Buttons: normal, pressed, disabled (a sprite swap)", (10, y), 28)
    y += 46
    x, row = 10, 0
    for i, (words, tile) in enumerate(tiles):
        if i == 3:
            x, y = 10, y + row + 44
            row = 0
        label(sheet, words, (x, y), 22, (190, 184, 170))
        sheet.paste(tile, (x, y + 30))
        x += tile.width + 14
        row = max(row, tile.height)
    y += row + 50
    label(sheet, "The hero chooser: the chosen hero in its niche, a pip a hero (the chosen one lit); pressed, disabled", (10, y), 28)
    y += 46
    x = 10
    for i, (words, tile) in enumerate(choosers):
        if i == 3:
            x, y = 10, y + tile.height + 44
        label(sheet, words, (x, y), 22, (190, 184, 170))
        sheet.paste(tile, (x, y + 30))
        x += tile.width + 20
    sheet = sheet.crop((0, 0, width, min(height, y + 260)))
    sheet.save(OUT / "forge-states.png")


def sprites_sheet(art):
    """Every painted sprite, enlarged, with its 9-slice borders marked in green."""
    entries = []
    seen = set()
    for key, sprite in art.items():
        if id(sprite) in seen:
            continue
        seen.add(id(sprite))
        factor = 2 if sprite.size[0] > 180 else 3 if sprite.size[0] > 60 else 5
        entries.append((sprite.name.replace("UI_Screens_", ""), sprite, factor))
    font_ = ImageFont.truetype(FONT, 20)
    placed, x, y, row_h, width = [], 20, 20, 0, 2200
    for name, sprite, factor in entries:
        tag = f"{name} {sprite.size[0]}x{sprite.size[1]} x{factor}" + (f"  border {sprite.border}" if any(sprite.border) else "")
        img_w, img_h = sprite.size[0] * factor, sprite.size[1] * factor
        tag_w = ImageDraw.Draw(Image.new("L", (1, 1))).textlength(tag, font=font_)
        if x + max(img_w, tag_w) + 20 > width:
            x, y, row_h = 20, y + row_h + 60, 0
        placed.append((tag, sprite, factor, x, y))
        x += int(max(img_w, tag_w)) + 30
        row_h = max(row_h, img_h)
    height = y + row_h + 80
    checker = np.indices((height, width)).sum(0) // 12 % 2
    base = np.where(checker[..., None] == 1, np.array([70, 66, 78]), np.array([56, 52, 64])).astype(np.uint8)
    sheet = Image.fromarray(base)
    d = ImageDraw.Draw(sheet)
    for tag, sprite, factor, x, y in placed:
        rgba = sprite.rgba
        canvas = to_canvas(np.asarray(sheet.crop((x, y + 30, x + rgba.shape[1] * factor, y + 30 + rgba.shape[0] * factor))))
        blit(canvas, np.repeat(np.repeat(rgba, factor, 0), factor, 1), 0, 0)
        sheet.paste(to_image(canvas), (x, y + 30))
        l, b, r, t = sprite.border
        w_, h_ = rgba.shape[1] * factor, rgba.shape[0] * factor
        for bx in ([l * factor] if l else []) + ([w_ - r * factor] if r else []):
            d.line([(x + bx, y + 30), (x + bx, y + 30 + h_)], fill=(80, 255, 120), width=2)
        for by in ([t * factor] if t else []) + ([h_ - b * factor] if b else []):
            d.line([(x, y + 30 + by), (x + w_, y + 30 + by)], fill=(80, 255, 120), width=2)
        d.text((x, y + 4), tag, font=font_, fill=(235, 228, 210))
    sheet.save(OUT / "sprites-and-borders.png")


def short_forge(art, values):
    """The Vanguard's forge on the shortest safe area the layout tests (1760 of 2340): the painted parts follow it."""
    root = forge_scene("vanguard")
    chooser = root.find("Safe Area/Hero Choice")
    chooser.chosen, chooser.heroes = 0, 2
    paint_forge(root, art, values)
    safe = root.find("Safe Area")
    margin = (H - 1760) / 2 / H
    safe.anchor_min, safe.anchor_max = (0.0, margin), (1.0, 1 - margin)
    image = render(root)
    image.save(OUT / "forge-vanguard-safe1760-after.png")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    art, values = load_set()
    shots = scenes()
    for name, (old, new, _) in shots.items():
        phone_sheet(old, new, name)
    states_sheet(shots, art, values)
    sprites_sheet(art)
    short_forge(art, values)
    print("wrote", OUT)


if __name__ == "__main__":
    main()
