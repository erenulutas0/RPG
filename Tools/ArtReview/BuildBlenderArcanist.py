"""Ilya Kest, the Cinder Arcanist (docs/45): a reproducible stylised character, rig, animation and sprite render.

Run with Blender 4.5 LTS, headless:

    blender --background --python Tools/ArtReview/BuildBlenderArcanist.py -- [options]

Options after the "--":
    --out DIR        raw frames and frames.json (default TestResults/arcanist-render, git-ignored)
    --blend PATH     the saved scene (default ArtDirection/2026-10-27/arcanist-blender-01/ilya-kest.blend)
    --facings LIST   a comma-separated subset of south,south-east,east,north-east,north
    --clips LIST     a comma-separated subset of idle,walk,walk-strike,strike,cast,hit,death
    --samples N      Cycles samples per render pixel (default 10)
    --preview        only the first frame of each clip
    --no-render      build and save the .blend, render nothing

Nothing here is generated imagery: the geometry is lofted rings and deformed primitives, the vertex weights are
computed from the geometry, the shading is one authored toon node group, and every frame is a pose computed by the
functions below. One model renders every frame and every facing, so details cannot drift between frames.

Camera: orthographic, looking north and down 30 degrees, the game's 2:1 floor (ArenaFloor.DepthScale). Heights are
modelled in design units and the rig is stretched by 1/cos(30) (as BuildBlenderFurnace.py), so one design unit of
height is one world unit on screen. The rig rotates for the five facings; the camera and the key light never move,
so the light comes from the screen's upper left in every frame. The root stays on the floor origin, which projects
to the same render pixel in every frame: the sprite pivot.
"""
import bpy
import json
import math
import sys
import time
from pathlib import Path
from mathutils import Matrix, Quaternion, Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[2]
ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []


def option(name, default=None):
    if name in ARGV:
        i = ARGV.index(name)
        return ARGV[i + 1] if i + 1 < len(ARGV) and not ARGV[i + 1].startswith('--') else True
    return default


OUT = Path(option('--out', str(ROOT / 'TestResults/arcanist-render')))
BLEND = Path(option('--blend', str(ROOT / 'ArtDirection/2026-10-27/arcanist-blender-01/ilya-kest.blend')))
SAMPLES = int(option('--samples', 10))
PREVIEW = bool(option('--preview', False))
PREVIEW_FRAME = option('--frame')         # with --preview: this frame of each clip (clamped), else the first
RENDER = not option('--no-render', False)
HOOD = option('--hood', 'dark')           # design comparison: a soot cowl with an ash-burnt tip (chosen), or ash

# ---- The sprite contract (Tools/ArtReview/PackArcanistSprites.py reads frames.json) ----
PPU = 100                      # final texels per world unit; the sprites import at 100 pixels per unit
SS = 4                         # render at 4x and box-filter down: area-exact anti-aliasing, the pivot stays on a corner
CANVAS = 2.2                   # world units across and up the square canvas
PIVOT = (110, 45)              # the floor origin in final pixels from the canvas's bottom-left corner
ELEVATION = math.radians(30)
HEIGHT = 1 / math.cos(ELEVATION)
FACINGS = [('south', 270), ('south-east', 315), ('east', 0), ('north-east', 45), ('north', 90)]
# Distance-driven walk: floor units one cycle of two steps covers (RenderedCharacterArtSet.StrideLength).
STRIDE = 1.2
CLIPS = {  # frames, frames a second, loops
    'idle': (8, 5.0, True),
    'walk': (8, 0.0, True),          # driven by distance, not time
    'walk-strike': (8, 0.0, True),   # the walk's legs under the strike's arms
    'strike': (5, 20.0, False),
    'cast': (5, 14.0, False),
    'hit': (3, 20.0, False),
    'death': (5, 9.0, False),
}

wanted_facings = option('--facings')
wanted_clips = option('--clips')
FACING_SET = [f for f in FACINGS if not wanted_facings or f[0] in wanted_facings.split(',')]
CLIP_SET = [c for c in CLIPS if not wanted_clips or c in wanted_clips.split(',')]


def lin(hexstr, alpha=1.0):
    """sRGB hex to the linear RGBA the node inputs take; the Standard view transform writes it back exactly."""
    h = hexstr.lstrip('#')
    out = []
    for i in (0, 2, 4):
        c = int(h[i:i + 2], 16) / 255
        out.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return (*out, alpha)


def smooth(e0, e1, x):
    t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def lerp(a, b, t):
    return a + (b - a) * t


# ---------------------------------------------------------------------------------------------------------------------
# Scene
# ---------------------------------------------------------------------------------------------------------------------
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = SAMPLES
scene.cycles.use_adaptive_sampling = False
scene.cycles.use_denoising = False
scene.cycles.seed = 7
scene.cycles.max_bounces = 1
scene.cycles.diffuse_bounces = 0
scene.cycles.glossy_bounces = 0
scene.cycles.transmission_bounces = 0
scene.cycles.volume_bounces = 0
scene.cycles.transparent_max_bounces = 24
scene.cycles.pixel_filter_type = 'BOX'
scene.cycles.filter_width = 1.0
scene.render.film_transparent = True
scene.render.resolution_x = scene.render.resolution_y = int(CANVAS * PPU * SS)
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
scene.render.image_settings.color_mode = 'RGBA'
scene.render.image_settings.color_depth = '8'
scene.render.image_settings.compression = 15
scene.view_settings.view_transform = 'Standard'
scene.view_settings.look = 'None'
scene.view_settings.exposure = 0
scene.view_settings.gamma = 1
scene.world = bpy.data.worlds.new('No light: the materials shade themselves')
scene.world.use_nodes = True
scene.world.node_tree.nodes['Background'].inputs['Strength'].default_value = 0
bpy.context.preferences.filepaths.save_version = 0
collection = scene.collection

# Screen axes in world space. The camera looks north (+Y) and down 30 degrees.
RIGHT = Vector((1, 0, 0))
UP = Vector((0, math.sin(ELEVATION), math.cos(ELEVATION)))
FORWARD = Vector((0, math.cos(ELEVATION), -math.sin(ELEVATION)))
TOWARD = -FORWARD
# The key light, fixed to the screen: from the upper left and a little in front.
KEY = (-0.52 * RIGHT + 0.78 * UP + 0.36 * TOWARD).normalized()
# A cool rim from behind the upper left, so the far edge of a light robe still separates from a light floor.
RIM = (-0.65 * RIGHT + 0.55 * UP - 0.52 * TOWARD).normalized()
HALF = (KEY + TOWARD).normalized()


# ---------------------------------------------------------------------------------------------------------------------
# Shading: one toon group, fed by authored palettes
# ---------------------------------------------------------------------------------------------------------------------
def build_toon_group():
    ng = bpy.data.node_groups.new('Ilya toon', 'ShaderNodeTree')
    io = ng.interface
    for name, kind, default in [
            ('Light', 'NodeSocketColor', (1, 1, 1, 1)), ('Mid', 'NodeSocketColor', (.5, .5, .5, 1)),
            ('Shadow', 'NodeSocketColor', (.2, .2, .2, 1)), ('Deep', 'NodeSocketColor', (.05, .05, .05, 1)),
            ('Glow', 'NodeSocketColor', (0, 0, 0, 1)), ('Spec', 'NodeSocketFloat', 0.0),
            ('Spec Color', 'NodeSocketColor', (1, 1, 1, 1)), ('Rim', 'NodeSocketFloat', 0.35),
            ('Occlusion', 'NodeSocketFloat', 1.0), ('Band Shift', 'NodeSocketFloat', 0.0)]:
        socket = io.new_socket(name, in_out='INPUT', socket_type=kind)
        socket.default_value = default
    # A colour, not a shader: materials mix zones as colours and emit once. (Cycles 4.5 here evaluated a Mix Shader
    # whose factor came from the UV map as zero, while the same factor mixed colours correctly.)
    io.new_socket('Color', in_out='OUTPUT', socket_type='NodeSocketColor')
    n, l = ng.nodes, ng.links
    gi = n.new('NodeGroupInput')
    go = n.new('NodeGroupOutput')
    geo = n.new('ShaderNodeNewGeometry')

    def vec(v):
        c = n.new('ShaderNodeCombineXYZ')
        c.inputs[0].default_value, c.inputs[1].default_value, c.inputs[2].default_value = v
        return c.outputs[0]

    def math_node(op, a, b=None, c=None):
        m = n.new('ShaderNodeMath')
        m.operation = op
        for i, value in enumerate((a, b, c)):
            if value is None:
                continue
            if isinstance(value, (int, float)):
                m.inputs[i].default_value = value
            else:
                l.new(value, m.inputs[i])
        return m.outputs[0]

    def dot(a, b):
        d = n.new('ShaderNodeVectorMath')
        d.operation = 'DOT_PRODUCT'
        l.new(a, d.inputs[0])
        l.new(b, d.inputs[1])
        return d.outputs['Value']

    def step(value, low, high):
        r = n.new('ShaderNodeMapRange')
        r.interpolation_type = 'SMOOTHSTEP'
        l.new(value, r.inputs['Value'])
        r.inputs['From Min'].default_value = low
        r.inputs['From Max'].default_value = high
        return r.outputs['Result']

    def mix(fac, a, b):
        m = n.new('ShaderNodeMixRGB')
        if isinstance(fac, (int, float)):
            m.inputs['Fac'].default_value = fac
        else:
            l.new(fac, m.inputs['Fac'])
        l.new(a, m.inputs['Color1'])
        l.new(b, m.inputs['Color2'])
        return m.outputs['Color']

    def add_colour(a, b, fac):
        m = n.new('ShaderNodeMixRGB')
        m.blend_type = 'ADD'
        l.new(fac, m.inputs['Fac'])
        l.new(a, m.inputs['Color1'])
        l.new(b, m.inputs['Color2'])
        return m.outputs['Color']

    normal = geo.outputs['Normal']
    half_lambert = math_node('MULTIPLY_ADD', dot(normal, vec(KEY)), 0.5, 0.5)
    lit = math_node('ADD', half_lambert, gi.outputs['Band Shift'])
    # Painted value steps with wide, soft transitions - shadow, the local colour, the lit plane - so folds and the
    # body's turn read as gradients, as on the painted Vanguard, while the large planes keep a clean colour.
    first = step(lit, 0.3, 0.56)
    second = step(lit, 0.64, 0.9)
    colour = mix(first, gi.outputs['Shadow'], gi.outputs['Mid'])
    colour = mix(second, colour, gi.outputs['Light'])
    # A loose brush breakup, about six per cent either way: a low-frequency drift on the undeformed surface (Generated
    # coordinates), so it sticks to the cloth in every pose instead of crawling.
    coords = n.new('ShaderNodeTexCoord')
    brush = n.new('ShaderNodeTexNoise')
    brush.inputs['Scale'].default_value = 5.0
    brush.inputs['Detail'].default_value = 1.5
    l.new(coords.outputs['Generated'], brush.inputs['Vector'])
    drift = n.new('ShaderNodeCombineColor')
    for channel, spread in enumerate((0.3, 0.3, 0.24)):
        l.new(math_node('MULTIPLY_ADD', brush.outputs['Fac'], spread, 1 - spread / 2), drift.inputs[channel])
    paint = n.new('ShaderNodeMixRGB')
    paint.blend_type = 'MULTIPLY'
    paint.inputs['Fac'].default_value = 1.0
    l.new(colour, paint.inputs['Color1'])
    l.new(drift.outputs[0], paint.inputs['Color2'])
    colour = paint.outputs['Color']
    # Baked contact occlusion (1 open, 0 closed) pulls toward the deep colour: under the hood, the mantle, the belt.
    closed = math_node('SUBTRACT', 1.0, step(gi.outputs['Occlusion'], 0.2, 0.95))
    colour = mix(math_node('MULTIPLY', closed, 0.9), colour, gi.outputs['Deep'])
    # The foundry's glow from below: surfaces turned down catch a faint ember bounce.
    bounce = math_node('MULTIPLY', math_node('MAXIMUM', dot(normal, vec(Vector((0, 0, -1)))), 0.0), 0.16)
    warm = n.new('ShaderNodeRGB')
    warm.outputs[0].default_value = lin('#FF7A3A')
    colour = add_colour(colour, warm.outputs[0], bounce)
    # A cool rim on edges turned to the upper left and away: separates the light robe from a light floor.
    facing = n.new('ShaderNodeLayerWeight')
    facing.inputs['Blend'].default_value = 0.4
    rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.55, 0.85),
                    math_node('MAXIMUM', dot(normal, vec(RIM)), 0.0))
    rim = math_node('MULTIPLY', rim, gi.outputs['Rim'])
    rim_colour = n.new('ShaderNodeRGB')
    rim_colour.outputs[0].default_value = lin('#C9D6FF')
    colour = add_colour(colour, rim_colour.outputs[0], math_node('MULTIPLY', rim, 0.45))
    # A hard painted highlight for brass and glass.
    spec = math_node('POWER', math_node('MAXIMUM', dot(normal, vec(HALF)), 0.0), 48.0)
    spec = math_node('MULTIPLY', step(spec, 0.35, 0.55), gi.outputs['Spec'])
    colour = add_colour(colour, gi.outputs['Spec Color'], spec)
    glow = n.new('ShaderNodeMixRGB')
    glow.blend_type = 'ADD'
    glow.inputs['Fac'].default_value = 1.0
    l.new(colour, glow.inputs['Color1'])
    l.new(gi.outputs['Glow'], glow.inputs['Color2'])
    l.new(glow.outputs['Color'], go.inputs['Color'])
    return ng


TOON = build_toon_group()

PALETTES = {
    # light, mid, shadow, deep
    'robe': ('#F1ECE2', '#D8CFC2', '#B3A9B3', '#7A707E'),
    'lining': ('#8E7F86', '#6E6068', '#4E434B', '#2E262D'),
    'soot': ('#5A4E5C', '#352C38', '#201B22', '#120E14'),
    'scorch': ('#8A786C', '#6A5A52', '#4A3D3A', '#2A2120'),
    'charcoal': ('#8C7F86', '#544950', '#2F282F', '#181418'),
    'ash': ('#CFC7C0', '#A89F9A', '#7B7275', '#463E42'),
    'leather': ('#7A5A48', '#54392E', '#33221C', '#1C120F'),
    'skin': ('#F7D7BC', '#E5B494', '#B07D6C', '#6E4842'),
    'hair': ('#7A5A4C', '#56403A', '#3A2A28', '#201618'),
    'brass': ('#FFE29A', '#D39A45', '#7E552C', '#3E2814'),
    'wood': ('#9A7550', '#6C4D39', '#432F25', '#24180F'),
    'glove': ('#7A5C4E', '#553E35', '#33241F', '#1C1311'),
    'cloth': ('#544852', '#3A3139', '#262027', '#141015'),
    'boot': ('#6A5246', '#45352F', '#2A201D', '#160F0E'),
    'glass': ('#B9D2DC', '#7B97A5', '#4D6672', '#27353C'),
    'eye': ('#3A2C34', '#241A22', '#180F16', '#0C080B'),
}
OUTLINE = '#160F17'
EMBER = '#E77232'


class Shading:
    """Builds a material: toon colours per palette, mixed as colours by the zones the material computes, emitted
    once. The material shades itself; the scene has no lights."""

    def __init__(self, name):
        self.mat = bpy.data.materials.new(name)
        self.mat.use_nodes = True
        self.nodes = self.mat.node_tree.nodes
        self.links = self.mat.node_tree.links
        for node in list(self.nodes):
            self.nodes.remove(node)
        self.out = self.nodes.new('ShaderNodeOutputMaterial')
        self._occlusion = None

    def occlusion(self):
        if self._occlusion is None:
            a = self.nodes.new('ShaderNodeAttribute')
            a.attribute_type = 'GEOMETRY'
            a.attribute_name = 'occlusion'
            sep = self.nodes.new('ShaderNodeSeparateColor')
            self.links.new(a.outputs['Color'], sep.inputs[0])
            self._occlusion = sep.outputs[0]
        return self._occlusion

    def value(self, v):
        node = self.nodes.new('ShaderNodeValue')
        node.outputs[0].default_value = v
        return node.outputs[0], node

    def toon(self, palette, spec=0.0, spec_color='#FFF4D6', rim=0.35, band_shift=0.0, occlusion=True):
        light, mid, shadow, deep = PALETTES[palette] if isinstance(palette, str) else palette
        g = self.nodes.new('ShaderNodeGroup')
        g.node_tree = TOON
        g.inputs['Light'].default_value = lin(light)
        g.inputs['Mid'].default_value = lin(mid)
        g.inputs['Shadow'].default_value = lin(shadow)
        g.inputs['Deep'].default_value = lin(deep)
        g.inputs['Spec'].default_value = spec
        g.inputs['Spec Color'].default_value = lin(spec_color)
        g.inputs['Rim'].default_value = rim
        g.inputs['Band Shift'].default_value = band_shift
        if occlusion:
            self.links.new(self.occlusion(), g.inputs['Occlusion'])
        return g.outputs['Color']

    def rgb(self, hexstr):
        node = self.nodes.new('ShaderNodeRGB')
        node.outputs[0].default_value = lin(hexstr)
        return node.outputs[0]

    def mix(self, fac, a, b):
        m = self.nodes.new('ShaderNodeMixRGB')
        if isinstance(fac, (int, float)):
            m.inputs['Fac'].default_value = fac
        else:
            self.links.new(fac, m.inputs['Fac'])
        self.links.new(a, m.inputs['Color1'])
        self.links.new(b, m.inputs['Color2'])
        return m.outputs['Color']

    def math(self, op, a, b=None, c=None, clamp=False):
        m = self.nodes.new('ShaderNodeMath')
        m.operation = op
        m.use_clamp = clamp
        for i, value in enumerate((a, b, c)):
            if value is None:
                continue
            if isinstance(value, (int, float)):
                m.inputs[i].default_value = value
            else:
                self.links.new(value, m.inputs[i])
        return m.outputs[0]

    def step(self, value, low, high):
        r = self.nodes.new('ShaderNodeMapRange')
        r.interpolation_type = 'SMOOTHSTEP'
        self.links.new(value, r.inputs['Value'])
        r.inputs['From Min'].default_value = low
        r.inputs['From Max'].default_value = high
        return r.outputs['Result']

    def band(self, value, low, high):
        """1 inside [low, high], 0 outside: a thread or a trim line."""
        return self.math('MULTIPLY', self.math('GREATER_THAN', value, low), self.math('LESS_THAN', value, high))

    def uv(self, layer='UVMap'):
        node = self.nodes.new('ShaderNodeUVMap')
        node.uv_map = layer
        sep = self.nodes.new('ShaderNodeSeparateXYZ')
        self.links.new(node.outputs['UV'], sep.inputs[0])
        return sep.outputs[0], sep.outputs[1]

    def noise(self, scale, detail=2.0):
        coord = self.nodes.new('ShaderNodeTexCoord')
        tex = self.nodes.new('ShaderNodeTexNoise')
        tex.inputs['Scale'].default_value = scale
        tex.inputs['Detail'].default_value = detail
        # Generated coordinates follow the undeformed mesh, so the pattern sticks to the cloth in every pose.
        self.links.new(coord.outputs['Generated'], tex.inputs['Vector'])
        return tex.outputs['Fac']

    def backfacing(self):
        return self.nodes.new('ShaderNodeNewGeometry').outputs['Backfacing']

    def finish(self, colour, strength=None):
        e = self.nodes.new('ShaderNodeEmission')
        self.links.new(colour, e.inputs['Color'])
        if strength is not None:
            self.links.new(strength, e.inputs['Strength'])
        else:
            e.inputs['Strength'].default_value = 1.0
        self.links.new(e.outputs[0], self.out.inputs['Surface'])
        return self.mat


def plain(name, palette, **kw):
    s = Shading(name)
    return s.finish(s.toon(palette, **kw))


def outline_material():
    s = Shading('Contour (inverted hull)')
    # The hull's normals are flipped: its far side faces the camera and draws, its near side is see-through.
    emit = s.nodes.new('ShaderNodeEmission')
    emit.inputs['Color'].default_value = lin(OUTLINE)
    transparent = s.nodes.new('ShaderNodeBsdfTransparent')
    mix = s.nodes.new('ShaderNodeMixShader')
    s.links.new(s.backfacing(), mix.inputs['Fac'])
    s.links.new(emit.outputs[0], mix.inputs[1])
    s.links.new(transparent.outputs[0], mix.inputs[2])
    s.links.new(mix.outputs[0], s.out.inputs['Surface'])
    return s.mat


MATERIALS = {}


def build_materials():
    m = MATERIALS
    m['outline'] = outline_material()
    for name in ['skin', 'hair', 'glove', 'cloth', 'boot', 'eye', 'soot', 'leather']:
        m[name] = plain(name.title(), name)
    m['brass'] = plain('Brass', 'brass', spec=0.9)
    m['piping'] = plain('Ash piping', 'ash' if HOOD == 'dark' else 'soot', rim=0.5)
    m['wood'] = plain('Cracked wood', 'wood', spec=0.15, spec_color='#C9A27A')
    ember = EMBER

    # The robe: ash white, soot trims on the front edges and the hem, a scorched hem with smouldering ember threads.
    s = Shading('Ash robe')
    u, v = s.uv()
    trim = s.math('MAXIMUM', s.math('LESS_THAN', u, 0.035), s.math('GREATER_THAN', u, 0.965))
    trim = s.math('MAXIMUM', trim, s.math('LESS_THAN', v, 0.06))
    scorch = s.math('MULTIPLY', s.math('SUBTRACT', 1.0, s.step(v, 0.05, 0.26)), s.step(s.noise(7.0), 0.3, 0.6))
    seam = s.math('MULTIPLY', s.band(v, 0.066, 0.084), s.step(s.noise(14.0), 0.42, 0.5))
    front_seam = s.math('MAXIMUM', s.band(u, 0.035, 0.048), s.band(u, 0.952, 0.965))
    front_seam = s.math('MULTIPLY', front_seam, s.step(s.noise(11.0), 0.46, 0.56))
    glow = s.math('MAXIMUM', seam, front_seam)
    colour = s.mix(scorch, s.toon('robe'), s.toon('scorch'))
    colour = s.mix(trim, colour, s.toon('soot'))
    colour = s.mix(s.backfacing(), colour, s.toon('lining', rim=0.0))
    colour = s.mix(glow, colour, s.rgb(ember))
    m['robe'] = s.finish(colour)

    # The upper robe: soot trim down the front. The mantle: soot-black, a charcoal hem band, one ember thread above it,
    # and an ember sigil embroidered on the back - a ring round a flame, Ilya's mark. The dark mantle splits the light
    # robe from the head, so the figure reads in three shapes instead of one white column.
    s = Shading('Ash torso')
    u, v = s.uv()
    trim = s.math('MAXIMUM', s.math('LESS_THAN', u, 0.03), s.math('GREATER_THAN', u, 0.97))
    colour = s.mix(trim, s.toon('robe'), s.toon('soot'))
    m['torso'] = s.finish(s.mix(s.backfacing(), colour, s.toon('lining', rim=0.0)))

    s = Shading('Soot mantle')
    u, v = s.uv()
    trim = s.math('LESS_THAN', v, 0.16)
    thread = s.math('MULTIPLY', s.band(v, 0.175, 0.205), s.step(s.noise(16.0), 0.42, 0.5))
    # The sigil: distance from a point on the mantle's back (u = 0.5), in roughly isotropic units.
    du = s.math('MULTIPLY', s.math('SUBTRACT', u, 0.5), 3.6)
    dv = s.math('SUBTRACT', v, 0.42)
    dist = s.math('SQRT', s.math('ADD', s.math('MULTIPLY', du, du), s.math('MULTIPLY', dv, dv)))
    ring = s.band(dist, 0.135, 0.175)
    flame = s.math('MULTIPLY', s.math('LESS_THAN', s.math('ABSOLUTE', du), s.math('MULTIPLY', 0.055,
                   s.math('SUBTRACT', 0.12, dv))), s.band(dv, -0.08, 0.1))
    sigil = s.math('MAXIMUM', ring, flame)
    colour = s.mix(trim, s.toon('soot', rim=0.5), s.toon('charcoal'))
    colour = s.mix(s.math('MAXIMUM', thread, sigil), colour, s.rgb(ember))
    m['mantle'] = s.finish(s.mix(s.backfacing(), colour, s.toon('soot', rim=0.0)))

    # The hood (chosen in review, --hood dark): a soot-dark cowl whose crooked tip has burnt to pale ash toward a live
    # ember at the point, like a wick; the face looks out of it, framed by an ash piping roll. The ash variant (--hood
    # light) was rejected: a tall pale hood over a pale robe read as one white column. Lining soot inside.
    s = Shading('Cowl')
    u, v = s.uv()
    scorch = s.math('MULTIPLY', s.step(v, 0.5, 0.68), s.math('ADD', 0.78, s.math('MULTIPLY', 0.22, s.noise(12.0))))
    colour = s.mix(scorch, s.toon('robe', band_shift=0.02), s.toon('charcoal'))
    if HOOD == 'dark':
        colour = s.mix(scorch, s.toon('charcoal', rim=0.9), s.toon('ash', rim=0.5))
    colour = s.mix(s.backfacing(), colour, s.toon('lining', rim=0.0))
    colour = s.mix(s.step(v, 0.95, 0.98), colour, s.rgb('#F28A3C'))
    m['hood'] = s.finish(colour)

    # Sleeves: ash white, a soot cuff at the open end and an ember thread.
    s = Shading('Ash sleeve')
    u, v = s.uv()
    trim = s.math('GREATER_THAN', v, 0.8)
    glow = s.math('MULTIPLY', s.band(v, 0.75, 0.785), s.step(s.noise(18.0), 0.42, 0.5))
    colour = s.mix(trim, s.toon('robe'), s.toon('soot'))
    colour = s.mix(s.backfacing(), colour, s.toon('lining', rim=0.0))
    m['sleeve'] = s.finish(s.mix(glow, colour, s.rgb(ember)))

    # Emissive accents. Their strength is a Value node the frame loop drives (flicker, flare, death).
    for key, core, edge in [('lens-flame', '#FFE7A0', '#F2862F'), ('crucible', '#FFC56A', '#E77232'),
                            ('flask', '#FFB25A', '#E2652A'), ('flame', '#FFF0B0', '#F08A30'),
                            ('crack', '#FFB070', '#E77232')]:
        s = Shading('Ember ' + key)
        strength, node = s.value(1.0)
        node.name = 'Ember strength'
        # Hot core to ember edge: a small gradient reads as fire, not as a flat dot.
        ramp = s.nodes.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position = 0.0
        ramp.color_ramp.elements[0].color = lin(edge)
        ramp.color_ramp.elements[1].position = 0.8
        ramp.color_ramp.elements[1].color = lin(core)
        if key == 'flame':
            coord = s.nodes.new('ShaderNodeTexCoord')
            sep = s.nodes.new('ShaderNodeSeparateXYZ')
            s.links.new(coord.outputs['Generated'], sep.inputs[0])
            s.links.new(s.math('SUBTRACT', 1.0, sep.outputs[2]), ramp.inputs[0])
        else:
            s.links.new(s.noise(30.0 if key != 'crack' else 60.0, 1.0), ramp.inputs[0])
        m[key] = s.finish(ramp.outputs[0], strength)

    # The cracked lens: grey-blue glass with a bright painted crack.
    m['glass'] = plain('Cracked lens glass', 'glass', spec=1.0, spec_color='#F4FBFF', rim=0.6)
    s = Shading('Lens crack')
    m['lens-crack'] = s.finish(s.rgb('#F2F8FA'))


build_materials()


# ---------------------------------------------------------------------------------------------------------------------
# Geometry
# ---------------------------------------------------------------------------------------------------------------------
PARTS = {}      # name -> object
OCCLUDERS = []  # bodies that cast the baked contact occlusion (never the moving staff or hands)


def new_object(name, verts, faces, uvs=None, smooth_shade=True):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in verts], [], faces)
    mesh.update()
    if uvs is not None:
        layer = mesh.uv_layers.new(name='UVMap')
        for poly in mesh.polygons:
            for loop_index, corner in zip(poly.loop_indices, uvs[poly.index]):
                layer.data[loop_index].uv = corner
    for poly in mesh.polygons:
        poly.use_smooth = smooth_shade
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    PARTS[name] = obj
    return obj


def loft(name, rings, closed=True, cap_end=False, cap_start=False):
    """Quads between consecutive rings (each a list of points, same length). Rings run counter-clockwise seen from
    the side the normals face; u runs around the ring (0..1), v along the rings (0..1)."""
    n = len(rings[0])
    m = len(rings)
    verts = [p for ring in rings for p in ring]
    faces, uvs = [], []
    cols = n if closed else n - 1
    for j in range(m - 1):
        for i in range(cols):
            i1 = (i + 1) % n
            faces.append((j * n + i, j * n + i1, (j + 1) * n + i1, (j + 1) * n + i))
            u0, u1 = i / cols, (i + 1) / cols
            v0, v1 = j / (m - 1), (j + 1) / (m - 1)
            uvs.append([(u0, v0), (u1, v0), (u1, v1), (u0, v1)])
    for cap, j, flip in [(cap_start, 0, True), (cap_end, m - 1, False)]:
        if not cap:
            continue
        centre = sum(rings[j], Vector()) / n
        verts.append(centre)
        c = len(verts) - 1
        for i in range(cols):
            i1 = (i + 1) % n
            face = (j * n + i, j * n + i1, c)
            faces.append(tuple(reversed(face)) if flip else face)
            uvs.append([(0.5, float(j > 0))] * 3)
    return new_object(name, verts, faces, uvs)


def ellipse(cx, cy, z, rx, ry, count, start=0.0, end=2 * math.pi, closed=True, wobble=None):
    pts = []
    steps = count if closed else count - 1
    for i in range(count):
        a = start + (end - start) * i / steps
        r = 1 + (wobble(a) if wobble else 0)
        pts.append(Vector((cx + rx * r * math.cos(a), cy + ry * r * math.sin(a), z)))
    return pts


def sphere(name, centre, radii, rings=12, segments=20):
    verts, faces = [], []
    for j in range(1, rings):
        lat = -math.pi / 2 + math.pi * j / rings
        for i in range(segments):
            lon = 2 * math.pi * i / segments
            verts.append(Vector((centre[0] + radii[0] * math.cos(lat) * math.cos(lon),
                                 centre[1] + radii[1] * math.cos(lat) * math.sin(lon),
                                 centre[2] + radii[2] * math.sin(lat))))
    bottom = len(verts)
    verts.append(Vector((centre[0], centre[1], centre[2] - radii[2])))
    top = len(verts)
    verts.append(Vector((centre[0], centre[1], centre[2] + radii[2])))
    for j in range(rings - 2):
        for i in range(segments):
            i1 = (i + 1) % segments
            faces.append((j * segments + i, j * segments + i1, (j + 1) * segments + i1, (j + 1) * segments + i))
    for i in range(segments):
        i1 = (i + 1) % segments
        faces.append((bottom, i1, i))
        faces.append((top, (rings - 2) * segments + i, (rings - 2) * segments + i1))
    return new_object(name, verts, faces)


def tube(name, path, radii, count=10, closed=True, cap_start=True, cap_end=True, twist_ref=Vector((0, -1, 0))):
    """A tube along a polyline with a radius per point (rx, ry or r); its frames are transported to stay untwisted."""
    rings = []
    prev_normal = None
    for k, p in enumerate(path):
        tangent = (path[min(k + 1, len(path) - 1)] - path[max(k - 1, 0)]).normalized()
        if prev_normal is None:
            normal = (twist_ref - tangent * twist_ref.dot(tangent))
            if normal.length < 1e-6:
                normal = Vector((1, 0, 0)) - tangent * tangent.x
            normal.normalize()
        else:
            normal = (prev_normal - tangent * prev_normal.dot(tangent)).normalized()
        prev_normal = normal
        binormal = tangent.cross(normal)
        r = radii[k]
        rx, ry = (r, r) if isinstance(r, (int, float)) else r
        ring = [p + normal * (rx * math.cos(2 * math.pi * i / count)) + binormal * (ry * math.sin(2 * math.pi * i / count))
                for i in range(count)]
        rings.append(ring)
    return loft(name, rings, closed=closed, cap_start=cap_start, cap_end=cap_end)


def lathe(name, profile, centre, axis_up=Vector((0, 0, 1)), count=18, cap_start=False, cap_end=False):
    rings = []
    for r, h in profile:
        rings.append([Vector(centre) + Vector((r * math.cos(2 * math.pi * i / count),
                                               r * math.sin(2 * math.pi * i / count), h)) for i in range(count)])
    return loft(name, rings, closed=True, cap_start=cap_start, cap_end=cap_end)


# ---- Measurements (design units: metres-like, feet on z = 0, facing -Y, Ilya's left is +X) ----
HIP_Z, KNEE_Z, ANKLE_Z = 0.50, 0.275, 0.055
HIP_X = 0.064
WAIST_Z = 0.565
SHOULDER = Vector((0.112, 0.004, 0.828))
ELBOW = Vector((0.146, 0.016, 0.664))
WRIST = Vector((0.166, -0.006, 0.508))
HAND_END = Vector((0.170, -0.012, 0.452))
HEAD_C = Vector((0, -0.02, 1.0))
HEAD_R = 0.108
STAFF_LOW, STAFF_HIGH = -0.55, 0.72      # shaft ends relative to the grip
GRIP = Vector((-0.170, -0.016, 0.475))   # the right fist's centre in the rest pose
LENS_R = 0.031


def mirror_x(v):
    return Vector((-v.x, v.y, v.z))


def build_body():
    # Head: a slightly long egg, skin; two dark eyes, a small nose, a faint smirk.
    sphere('Head', HEAD_C, (HEAD_R * 0.96, HEAD_R * 0.98, HEAD_R * 1.04), 14, 24)
    for side in (-1, 1):
        sphere('Eye ' + ('L' if side > 0 else 'R'), HEAD_C + Vector((0.037 * side, -0.094, -0.008)),
               (0.0135, 0.009, 0.0165), 6, 10)
    sphere('Nose', HEAD_C + Vector((0, -0.108, -0.034)), (0.0145, 0.018, 0.0135), 6, 10)
    smirk = [HEAD_C + Vector((0.022 * math.cos(a) - 0.003, -0.099 + 0.006 * abs(math.cos(a)), -0.066 + 0.009 * math.sin(a)))
             for a in [math.radians(x) for x in (200, 230, 260, 290, 320, 340)]]
    tube('Smirk', smirk, [0.0036] * len(smirk), 6)
    # Two dark locks escape the hood beside the goggles and fall along the cheeks, framing the face.
    for side in (-1, 1):
        a = math.radians(58 * side)
        root = HEAD_C + Vector((math.sin(a) * HEAD_R * 0.9, -math.cos(a) * HEAD_R * 0.9, 0.055))
        path = [root, root + Vector((0.008 * side, -0.012, -0.035)), root + Vector((0.004 * side, -0.016, -0.075)),
                root + Vector((-0.004 * side, -0.012, -0.1))]
        tube('Lock ' + ('L' if side > 0 else 'R'), path, [0.017, 0.015, 0.011, 0.002], 6, cap_end=False)

    # Trousers and boots, mostly under the robe.
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        x = HIP_X * side
        tube('Leg ' + s, [Vector((x, 0, HIP_Z + 0.03)), Vector((x, 0, KNEE_Z)), Vector((x, 0.004, ANKLE_Z + 0.025))],
             [0.046, 0.037, 0.031], 10)
        boot = sphere('Boot ' + s, Vector((x, -0.022, 0.034)), (0.036, 0.064, 0.036), 8, 14)
        for v in boot.data.vertices:
            if v.co.z < 0.012:
                v.co.z = 0.012 - (0.012 - v.co.z) * 0.25      # a flat sole
        tube('Boot cuff ' + s, [Vector((x, 0.004, 0.05)), Vector((x, 0.004, 0.10))], [0.036, 0.034], 12)
        OCCLUDERS.extend([PARTS['Leg ' + s]])

    # Inner arms (an undershirt) and gloved hands.
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        sh = SHOULDER if side > 0 else mirror_x(SHOULDER)
        el = ELBOW if side > 0 else mirror_x(ELBOW)
        wr = WRIST if side > 0 else mirror_x(WRIST)
        tube('Arm ' + s, [sh, el, wr], [0.03, 0.025, 0.021], 8)
        hand_c = (WRIST.lerp(HAND_END, 0.55)) if side > 0 else GRIP
        glove = sphere('Glove ' + s, hand_c, (0.026, 0.024, 0.033), 8, 12)
        sphere('Thumb ' + s, hand_c + Vector((-0.016 * side, -0.016, 0.01)), (0.011, 0.011, 0.014), 6, 8)


def build_robe():
    # Skirt: an open-fronted bell from the waist to the hem; u runs from the opening's left edge round the back to its
    # right edge, v from the hem (0) to the waist (1). The gap widens toward the hem so the legs walk through it.
    heights = [0.03, 0.10, 0.18, 0.27, 0.36, 0.45, 0.52, 0.58]
    radii = [(0.252, 0.204), (0.228, 0.184), (0.203, 0.164), (0.172, 0.139), (0.151, 0.124), (0.131, 0.108),
             (0.115, 0.096), (0.108, 0.090)]
    gaps = [58, 50, 40, 30, 20, 10, 2, 0.5]
    rings = []
    count = 56
    for k, (z, (rx, ry), gap) in enumerate(zip(heights, radii, gaps)):
        g = math.radians(gap)
        start = math.radians(-90) + g / 2
        end = math.radians(270) - g / 2
        t = k / (len(heights) - 1)
        # Irregular vertical folds, deepening toward the hem: they catch the key light in painted stripes.
        wobble = (lambda a, t=t: 0.14 * (1 - t) ** 1.1 * (0.55 * math.cos(7 * a + 0.3) + 0.3 * math.cos(12 * a + 2.1)
                                                            + 0.15 * math.cos(3 * a + 1.0)))
        ring = ellipse(0, 0.012 * (1 - t), z, rx, ry, count, start, end, closed=False, wobble=wobble)
        # The back hangs a little longer and the front edges a little shorter, as a robe falls from a hunched back;
        # the hem is burnt ragged.
        for i, p in enumerate(ring):
            a = start + (end - start) * i / (count - 1)
            back = max(0.0, p.y) / ry
            p.z -= 0.018 * back * (1 - t)
            if k <= 1:
                ragged = 0.016 * (0.5 + 0.5 * math.sin(23 * a + 1.3)) * (0.5 + 0.5 * math.sin(9 * a + 0.2))
                p.z += ragged * (1.0 if k == 0 else 0.4)
            if k == 0:
                p.z = max(p.z, 0.012)
        rings.append(ring)
    skirt = loft('Robe skirt', rings, closed=False)
    OCCLUDERS.append(skirt)
    SKIRT_PROFILE.extend((z, rx, ry) for z, (rx, ry) in zip(heights, radii))

    # Upper robe: closed rings from the waist to the neck; u starts at the front centre, where its soot trim runs.
    torso_rings = []
    for z, rx, ry in [(0.53, 0.106, 0.088), (0.60, 0.108, 0.088), (0.68, 0.116, 0.091), (0.75, 0.122, 0.090),
                      (0.81, 0.124, 0.086), (0.855, 0.104, 0.074), (0.885, 0.058, 0.05)]:
        torso_rings.append(ellipse(0, 0.004, z, rx, ry, 32, math.radians(-90), math.radians(270), closed=True))
    OCCLUDERS.append(loft('Robe torso', torso_rings, closed=True))

    # Mantle: a short cowl cape over the shoulders under the hood, scalloped at its soot hem.
    mantle_rings = []
    for z, rx, ry, w in [(0.715, 0.192, 0.158, 0.05), (0.76, 0.182, 0.150, 0.03), (0.81, 0.152, 0.128, 0.0),
                         (0.86, 0.104, 0.092, 0.0), (0.905, 0.066, 0.062, 0.0)]:
        wobble = (lambda a, w=w: w * math.sin(7 * a))
        ring = ellipse(0, 0.008, z, rx, ry, 40, math.radians(-90), math.radians(270), closed=True, wobble=wobble)
        mantle_rings.append(ring)
    OCCLUDERS.append(loft('Mantle', mantle_rings, closed=True))

    # Sash belt with a brass buckle and three ember flasks on the left hip.
    belt_path = ellipse(0, 0.004, WAIST_Z, 0.113, 0.094, 28, math.radians(-90), math.radians(270), closed=True)
    belt_path.append(belt_path[0])
    belt = tube('Belt', belt_path, [(0.016, 0.011)] * len(belt_path), 8, cap_start=False, cap_end=False,
                twist_ref=Vector((0, 0, 1)))
    OCCLUDERS.append(belt)
    sphere('Buckle', Vector((0, -0.1, WAIST_Z)), (0.018, 0.008, 0.016), 6, 10)

    # The flask belt: slung low across the hips, down to Ilya's left, snug on the robe; three ember flasks hang from
    # it at the front-left and a brass buckle sits at the front-right.
    def on_robe(phi, z, out=1.07):
        rx, ry = skirt_radius(z)
        t = (z - 0.03) / (0.58 - 0.03)
        return Vector((rx * out * math.cos(phi), 0.012 * (1 - t) + ry * out * math.sin(phi), z))

    def belt_z(phi):
        return 0.505 - 0.045 * math.cos(phi)

    sling = [on_robe(2 * math.pi * i / 36, belt_z(2 * math.pi * i / 36)) for i in range(37)]
    OCCLUDERS.append(tube('Flask belt', sling, [(0.014, 0.0085)] * len(sling), 8, cap_start=False, cap_end=False,
                          twist_ref=Vector((0, 0, 1))))
    phi = math.radians(-118)
    sphere('Flask belt buckle', on_robe(phi, belt_z(phi), 1.13), (0.017, 0.012, 0.015), 6, 10)
    for k, degrees in enumerate((-66, -44, -22)):
        phi = math.radians(degrees)
        at = on_robe(phi, belt_z(phi) - 0.034, 1.16)
        sphere('Flask %d' % k, at, (0.0165, 0.0165, 0.022), 8, 12)
        sphere('Flask cap %d' % k, at + Vector((0, 0, 0.026)), (0.0095, 0.0095, 0.008), 5, 8)


SKIRT_PROFILE = []


def skirt_radius(z):
    """The skirt's ellipse radii at a height, interpolated between its rings."""
    prof = SKIRT_PROFILE
    for (z0, rx0, ry0), (z1, rx1, ry1) in zip(prof, prof[1:]):
        if z0 <= z <= z1:
            t = (z - z0) / (z1 - z0)
            return lerp(rx0, rx1, t), lerp(ry0, ry1, t)
    return (prof[0][1], prof[0][2]) if z < prof[0][0] else (prof[-1][1], prof[-1][2])


def build_sleeves():
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        sh = SHOULDER if side > 0 else mirror_x(SHOULDER)
        el = ELBOW if side > 0 else mirror_x(ELBOW)
        wr = WRIST if side > 0 else mirror_x(WRIST)
        path = [sh + Vector((0, 0, 0.012)), sh.lerp(el, 0.5), el, el.lerp(wr, 0.55), wr + (wr - el).normalized() * 0.035]
        radii = [0.055, 0.056, 0.066, 0.086, 0.106]
        sleeve = tube('Sleeve ' + s, path, radii, 14, cap_start=False, cap_end=False, twist_ref=Vector((0, -1, 0)))
        # The bell droops: its underside hangs past the wrist.
        axis = (wr - sh).normalized()
        for v in sleeve.data.vertices:
            along = (v.co - sh).dot(axis) / (wr - sh).length
            if along > 0.75:
                off = v.co - (sh + axis * (along * (wr - sh).length))
                underside = max(0.0, -off.z / 0.07)
                v.co.z -= 0.03 * smooth(0.75, 1.0, along) * underside


def bezier(p0, p1, p2, p3, t):
    u = 1 - t
    return p0 * u ** 3 + p1 * (3 * u * u * t) + p2 * (3 * u * t * t) + p3 * t ** 3


def build_hood():
    """Open arcs stacked round the head and closing over the brow, then a crooked tip: up, over to Ilya's left and a
    little back, drooping at its burnt end - the tall asymmetric hood docs/45 asks for, never a straight cone."""
    rings = []
    count = 36
    levels = [  # z, radius x, radius y, centre y, front gap (degrees)
        (0.875, 0.192, 0.172, 0.034, 84),
        (0.925, 0.174, 0.160, 0.032, 112),
        (0.98, 0.162, 0.152, 0.028, 122),
        (1.035, 0.153, 0.147, 0.028, 118),
        (1.085, 0.147, 0.141, 0.030, 98),
        (1.12, 0.137, 0.132, 0.036, 60),
        (1.15, 0.122, 0.12, 0.042, 0),
    ]
    for z, rx, ry, cy, gap in levels:
        g = math.radians(max(gap, 0.01))
        start = math.radians(-90) + g / 2
        end = math.radians(270) - g / 2
        rings.append(ellipse(0, cy, z, rx, ry, count, start, end, closed=False))
    # The tip: rings carried along a curve (parallel transport keeps them untwisted), shrinking to the point.
    top = levels[-1]
    b0 = Vector((0, top[3], top[0]))
    b1 = Vector((0.0, top[3] + 0.0, 1.26))
    b2 = Vector((0.10, top[3] + 0.06, 1.355))
    b3 = Vector((0.21, top[3] + 0.12, 1.13))
    steps = 14
    x_axis, y_axis, tangent = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))
    points = [bezier(b0, b1, b2, b3, k / steps) for k in range(steps + 1)]
    for k in range(1, steps + 1):
        ahead = points[min(k + 1, steps)] - points[k - 1]
        turn = tangent.rotation_difference(ahead.normalized())
        x_axis, y_axis, tangent = turn @ x_axis, turn @ y_axis, ahead.normalized()
        s = k / steps
        r = (1 - s) ** 0.6 * (1 + 0.12 * math.sin(math.pi * min(1.0, s * 2.2)))
        ring = []
        for i in range(count):
            a = math.radians(-90) + (2 * math.pi - 1e-4) * i / (count - 1)
            ring.append(points[k] + x_axis * (top[1] * r * math.cos(a)) + y_axis * (top[2] * r * math.sin(a)))
        rings.append(ring)
    hood = loft('Hood', rings, closed=False)
    tip = points[-1]
    # The closed rings' first and last points coincide at the front: merge them so the brow and tip are seamless.
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(hood.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0015)
    bm.to_mesh(hood.data)
    bm.free()
    hood.data.update()
    OCCLUDERS.append(hood)
    # The rim: a soot roll along the face opening, from under the chin up and over the brow and back down.
    left, right = [], []
    for z, rx, ry, cy, gap in levels[:-1]:
        g = math.radians(gap)
        a = math.radians(-90) + g / 2
        left.append(Vector((rx * math.cos(a), cy + ry * math.sin(a), z)))
        right.append(Vector((-rx * math.cos(a), cy + ry * math.sin(a), z)))
    brow = Vector((0, levels[-2][3] - levels[-2][2] * 0.98, levels[-2][0] + 0.014))
    path = left + [brow] + list(reversed(right))
    tube('Hood rim', path, [0.0135] * len(path), 8, cap_start=True, cap_end=True, twist_ref=Vector((0, -1, 0)))
    return [bezier(b0, b1, b2, b3, k / 64) for k in range(65)]


def hood_param(co):
    """How far along the hood's crooked tip a rest-pose point lies: 0 on the cowl round the head, 1 at the point."""
    p = Vector(co)
    if p.z < HOOD_CURVE[0].z - 0.01:
        return 0.0
    best = min(range(len(HOOD_CURVE)), key=lambda i: (HOOD_CURVE[i] - p).length_squared)
    return best / (len(HOOD_CURVE) - 1)


def build_goggles():
    # Pushed up on the brow, tilted up: brass rims, the right lens holding a small living flame, the left cracked.
    tilt = math.radians(38)
    strap = ellipse(0, 0.03, 1.064, 0.149, 0.143, 30, closed=True)
    strap.append(strap[0])
    tube('Goggle strap', strap, [(0.0075, 0.005)] * len(strap), 6, cap_start=False, cap_end=False,
         twist_ref=Vector((0, 0, 1)))
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        centre = Vector((0.042 * side, -0.114, 1.07))
        normal = Vector((0.12 * side, -math.cos(tilt), math.sin(tilt))).normalized()
        u_axis = Vector((1, 0, 0))
        u_axis = (u_axis - normal * u_axis.dot(normal)).normalized()
        v_axis = normal.cross(u_axis)
        rim = [centre + (u_axis * math.cos(2 * math.pi * i / 16) + v_axis * math.sin(2 * math.pi * i / 16)) * LENS_R
               for i in range(16)]
        rim.append(rim[0])
        tube('Goggle rim ' + s, rim, [0.0088] * len(rim), 6, cap_start=False, cap_end=False, twist_ref=normal)
        lens = [centre + normal * 0.002 + (u_axis * math.cos(2 * math.pi * i / 16) + v_axis * math.sin(2 * math.pi * i / 16))
                * (LENS_R * 0.92) for i in range(16)]
        disc = loft('Lens ' + s, [lens, [centre + normal * 0.006] * 16], closed=True)
        if side < 0:
            # The living flame: a small teardrop standing in the lens.
            base = centre + normal * 0.006 - v_axis * 0.012
            flame = []
            for k, (h, r) in enumerate([(0, 0.0), (0.004, 0.009), (0.011, 0.012), (0.019, 0.009), (0.027, 0.004),
                                        (0.034, 0.0)]):
                flame.append(base + v_axis * h)
            tube('Lens flame', flame, [0.001, 0.009, 0.012, 0.009, 0.0045, 0.0005], 8, twist_ref=normal)
        else:
            crack = [centre + normal * 0.0075 + u_axis * x + v_axis * y for x, y in
                     [(-0.022, 0.012), (-0.008, 0.004), (-0.002, -0.006), (0.009, -0.004), (0.02, -0.014)]]
            tube('Lens crack', crack, [0.0022] * len(crack), 5)
    bridge = [Vector((-0.013, -0.122, 1.072)), Vector((0, -0.126, 1.076)), Vector((0.013, -0.122, 1.072))]
    tube('Goggle bridge', bridge, [0.0065] * 3, 6)


def build_staff():
    """A long, slightly crooked shaft of cracked wood bound with brass, a brass crucible head with three prongs, a bed
    of embers and a small flame. Built upright about the right fist's grip."""
    g = GRIP
    path, radii = [], []
    for k in range(15):
        t = k / 14
        h = lerp(STAFF_LOW, STAFF_HIGH - 0.02, t)
        bend = 0.012 * math.sin(t * math.pi * 1.3) + 0.006 * math.sin(t * 9.0)
        path.append(g + Vector((bend, 0.004 * math.sin(t * 7.0), h)))
        knot = 1 + 0.18 * math.exp(-((t - 0.63) / 0.04) ** 2) + 0.12 * math.exp(-((t - 0.3) / 0.03) ** 2)
        radii.append(0.02 * lerp(0.82, 1.08, t) * knot)
    tube('Staff shaft', path, radii, 10)
    # Two glowing cracks in the upper shaft, and the brass bands that bind them.
    for k, (a0, a1, side) in enumerate([(0.30, 0.52, 1), (0.42, 0.6, -1)]):
        pts = []
        for i in range(6):
            t = lerp(a0, a1, i / 5)
            h = lerp(STAFF_LOW, STAFF_HIGH, t)
            ang = side * (0.9 + 0.5 * math.sin(i * 1.9))
            r = 0.0208 * lerp(0.82, 1.08, t) * 1.02
            pts.append(g + Vector((r * math.cos(ang) + 0.012 * math.sin(t * math.pi * 1.3),
                                   -r * math.sin(abs(ang)) - 0.002, h)))
        tube('Staff crack %d' % k, pts, [0.0024] * len(pts), 5)
    for h, r in [(0.44, 0.025), (0.63, 0.026)]:
        lathe('Staff band %.2f' % h, [(r, h - 0.012), (r * 1.06, h), (r, h + 0.012)], g + Vector((0.012 * math.sin(
            ((h - STAFF_LOW) / (STAFF_HIGH - STAFF_LOW)) * math.pi * 1.3), 0, 0)), count=12)
    head = g + Vector((0.004, 0, STAFF_HIGH - 0.03))
    c = 1.22    # the crucible reads as the staff's signature at 100 pixels a unit: a little oversized
    lathe('Crucible', [(0.015 * c, -0.01), (0.022 * c, 0.004 * c), (0.034 * c, 0.022 * c), (0.043 * c, 0.042 * c),
                       (0.047 * c, 0.056 * c), (0.045 * c, 0.062 * c), (0.037 * c, 0.058 * c)], head, count=20,
          cap_start=True)
    for k in range(3):
        a = 2 * math.pi * k / 3 + 0.4
        base = head + Vector((0.045 * c * math.cos(a), 0.045 * c * math.sin(a), 0.052 * c))
        tip = head + Vector((0.06 * c * math.cos(a), 0.06 * c * math.sin(a), 0.098 * c))
        tube('Crucible prong %d' % k, [base, base.lerp(tip, 0.5) + Vector((0, 0, 0.004)), tip], [0.0085, 0.007, 0.003], 6)
    sphere('Crucible embers', head + Vector((0, 0, 0.05 * c)), (0.038 * c, 0.038 * c, 0.012 * c), 6, 16)
    flame = []
    for h in [0.0, 0.015, 0.035, 0.058, 0.08, 0.1]:
        flame.append(head + Vector((0, 0, 0.058 * c + h)))
    tube('Crucible flame', flame, [0.02, 0.033, 0.03, 0.02, 0.009, 0.001], 10)
    return head + Vector((0, 0, 0.06 * c))


build_body()
build_robe()
build_sleeves()
HOOD_CURVE = build_hood()
HOOD_TIP = HOOD_CURVE[-1]
build_goggles()
CRUCIBLE_REST = build_staff()

MATERIAL_OF = {
    'Head': 'skin', 'Nose': 'skin', 'Eye L': 'eye', 'Eye R': 'eye', 'Smirk': 'eye',
    'Robe skirt': 'robe', 'Robe torso': 'torso', 'Mantle': 'mantle', 'Hood': 'hood', 'Hood rim': 'piping',
    'Belt': 'soot', 'Buckle': 'brass', 'Flask belt': 'leather', 'Flask belt buckle': 'brass', 'Goggle strap': 'soot', 'Goggle rim L': 'brass', 'Goggle rim R': 'brass',
    'Goggle bridge': 'brass', 'Lens L': 'glass', 'Lens R': 'lens-flame', 'Lens flame': 'flame', 'Lens crack': 'lens-crack',
    'Staff shaft': 'wood', 'Crucible': 'brass', 'Crucible embers': 'crucible', 'Crucible flame': 'flame',
    'Sleeve L': 'sleeve', 'Sleeve R': 'sleeve', 'Arm L': 'cloth', 'Arm R': 'cloth',
    'Glove L': 'glove', 'Glove R': 'glove', 'Thumb L': 'glove', 'Thumb R': 'glove',
    'Leg L': 'cloth', 'Leg R': 'cloth', 'Boot L': 'boot', 'Boot R': 'boot', 'Boot cuff L': 'soot', 'Boot cuff R': 'soot',
}
for name, obj in PARTS.items():
    key = MATERIAL_OF.get(name)
    if key is None:
        key = ('hair' if name.startswith('Lock') else 'brass' if name.startswith(('Staff band', 'Crucible prong', 'Flask cap'))
               else 'crack' if name.startswith('Staff crack') else 'flask' if name.startswith('Flask') else None)
    if key is None:
        raise RuntimeError('No material for ' + name)
    obj.data.materials.append(MATERIALS[key])
# Glowing and tiny parts carry no contour: a dark ring round a flame reads as a hole.
NO_CONTOUR = {'Eye L', 'Eye R', 'Smirk', 'Lens flame', 'Lens crack', 'Lens L', 'Lens R', 'Crucible embers',
              'Crucible flame', 'Nose'} | {n for n in PARTS if n.startswith('Staff crack')}
SUBDIVIDE = {'Head', 'Robe skirt', 'Robe torso', 'Mantle', 'Hood', 'Sleeve L', 'Sleeve R', 'Glove L', 'Glove R',
             'Boot L', 'Boot R', 'Crucible', 'Staff shaft', 'Belt', 'Leg L', 'Leg R', 'Hood rim', 'Crucible flame'}


# ---------------------------------------------------------------------------------------------------------------------
# Rig
# ---------------------------------------------------------------------------------------------------------------------
arm_data = bpy.data.armatures.new('Ilya rig')
RIG = bpy.data.objects.new('Ilya', arm_data)
collection.objects.link(RIG)
bpy.context.view_layer.objects.active = RIG
RIG.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
eb = arm_data.edit_bones
BONES = {}


def bone(name, head, tail, parent=None, roll=0.0):
    b = eb.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    b.roll = roll
    if parent:
        b.parent = eb[parent]
    b.use_connect = False
    BONES[name] = (Vector(head), Vector(tail))
    return b


bone('root', (0, 0, 0), (0, -0.1, 0))
bone('pelvis', (0, 0, HIP_Z), (0, 0, 0.6), 'root')
bone('spine', (0, 0, 0.6), (0, 0, 0.71), 'pelvis')
bone('chest', (0, 0, 0.71), (0, 0, 0.85), 'spine')
bone('neck', (0, 0, 0.85), (0, -0.006, 0.925), 'chest')
bone('head', (0, -0.006, 0.925), (0, -0.006, 1.12), 'neck')
bone('hood1', tuple(HOOD_CURVE[0]), tuple(HOOD_CURVE[28]), 'head')
bone('hood2', tuple(HOOD_CURVE[28]), tuple(HOOD_TIP), 'hood1')
lens_r = Vector((-0.042, -0.118, 1.07))
bone('lens_flame', tuple(lens_r - Vector((0, 0, 0.012))), tuple(lens_r + Vector((0, 0, 0.03))), 'head')
for side, s in ((1, 'L'), (-1, 'R')):
    sh = SHOULDER if side > 0 else mirror_x(SHOULDER)
    el = ELBOW if side > 0 else mirror_x(ELBOW)
    wr = WRIST if side > 0 else mirror_x(WRIST)
    he = HAND_END if side > 0 else mirror_x(HAND_END)
    bone('upper_arm.' + s, sh, el, 'chest')
    bone('forearm.' + s, el, wr, 'upper_arm.' + s)
    bone('hand.' + s, wr, he, 'forearm.' + s)
    bone('thigh.' + s, (HIP_X * side, 0, HIP_Z), (HIP_X * side, 0, KNEE_Z), 'pelvis')
    bone('shin.' + s, (HIP_X * side, 0, KNEE_Z), (HIP_X * side, 0.004, ANKLE_Z), 'thigh.' + s)
    bone('foot.' + s, (HIP_X * side, 0.004, ANKLE_Z), (HIP_X * side, -0.08, 0.02), 'shin.' + s)
    bone('robe_front1.' + s, (0.05 * side, -0.075, 0.56), (0.105 * side, -0.125, 0.30), 'pelvis')
    bone('robe_front2.' + s, (0.105 * side, -0.125, 0.30), (0.15 * side, -0.17, 0.04), 'robe_front1.' + s)
bone('robe_back1', (0, 0.075, 0.56), (0, 0.125, 0.30), 'pelvis')
bone('robe_back2', (0, 0.125, 0.30), (0, 0.18, 0.04), 'robe_back1')
bone('staff', tuple(GRIP), tuple(GRIP + Vector((0, 0, 0.1))), 'hand.R')
bone('crucible_flame', tuple(CRUCIBLE_REST), tuple(CRUCIBLE_REST + Vector((0, 0, 0.08))), 'staff')
bpy.ops.object.mode_set(mode='OBJECT')
REST = {b.name: b.matrix_local.copy() for b in arm_data.bones}
PARENT = {b.name: (b.parent.name if b.parent else None) for b in arm_data.bones}


def depth(name):
    d = 0
    while PARENT[name]:
        name = PARENT[name]
        d += 1
    return d


ORDER = sorted(REST, key=depth)   # parents before children
for pb in RIG.pose.bones:
    pb.rotation_mode = 'QUATERNION'


# ---- Weights: computed from the geometry, never painted ----
def weights_for(name, co):
    """{bone: weight} for a rest-pose vertex of the named part."""
    x, y, z = co

    def side_of(prefix):
        return prefix + ('.L' if x > 0 else '.R')

    if name in ('Head', 'Eye L', 'Eye R', 'Nose', 'Smirk', 'Goggle strap', 'Goggle rim L', 'Goggle rim R',
                'Goggle bridge', 'Lens L', 'Lens R', 'Lens crack', 'Hood rim') or name.startswith('Lock'):
        return {'head': 1.0}
    if name == 'Lens flame':
        return {'lens_flame': 1.0}
    if name == 'Hood':
        along = hood_param(co)
        w2 = smooth(0.38, 0.75, along)
        w1 = smooth(0.04, 0.34, along) * (1 - w2)
        head = 1 - w1 - w2
        # The drape under the chin and on the shoulders follows the neck and chest.
        low = smooth(0.95, 0.88, z)
        return {'head': head * (1 - low), 'neck': head * low * 0.6, 'chest': head * low * 0.4, 'hood1': w1, 'hood2': w2}
    if name in ('Staff shaft', 'Crucible', 'Crucible embers') or name.startswith(('Staff crack', 'Staff band', 'Crucible prong')):
        return {'staff': 1.0}
    if name == 'Crucible flame':
        return {'crucible_flame': 1.0}
    if name.startswith(('Glove', 'Thumb')):
        return {'hand.' + name[-1]: 1.0}
    if name.startswith(('Arm', 'Sleeve')):
        s = name[-1]
        sh = SHOULDER if s == 'L' else mirror_x(SHOULDER)
        wr = WRIST if s == 'L' else mirror_x(WRIST)
        along = (Vector(co) - sh).dot((wr - sh).normalized()) / (wr - sh).length
        fore = smooth(0.38, 0.62, along)
        torso = 1 - smooth(-0.05, 0.16, along)
        hand = smooth(0.96, 1.08, along)
        return {'chest': torso, 'upper_arm.' + s: (1 - fore) * (1 - torso), 'forearm.' + s: fore * (1 - torso) * (1 - hand),
                'hand.' + s: hand * (1 - torso)}
    if name.startswith(('Leg', 'Boot')):
        s = name[-1]
        if name.startswith('Boot cuff'):
            return {'shin.' + s: 1.0}
        if name.startswith('Boot'):
            return {'foot.' + s: smooth(0.075, 0.03, z) if z > 0.03 else 1.0, 'shin.' + s: 1 - (smooth(0.075, 0.03, z) if z > 0.03 else 1.0)}
        shin = smooth(0.31, 0.24, z)
        pelvis = smooth(0.47, 0.53, z)
        return {'shin.' + s: shin, 'thigh.' + s: (1 - shin) * (1 - pelvis), 'pelvis': pelvis}
    if name in ('Belt', 'Buckle'):
        return {'pelvis': 1.0}
    if name.startswith('Flask'):
        # The slung belt and its flasks ride the robe they lie on.
        return weights_for('Robe skirt', co)
    if name == 'Robe torso':
        pelvis = smooth(0.64, 0.57, z)
        chest = smooth(0.66, 0.76, z)
        return {'pelvis': pelvis, 'spine': (1 - pelvis) * (1 - chest), 'chest': chest * (1 - smooth(0.86, 0.9, z)),
                'neck': smooth(0.86, 0.9, z)}
    if name == 'Mantle':
        neck = smooth(0.86, 0.905, z)
        return {'chest': 1 - neck, 'neck': neck}
    if name == 'Robe skirt':
        t = (z - 0.03) / (0.58 - 0.03)
        top = smooth(0.62, 0.97, t)
        upper = smooth(0.18, 0.62, t)
        a = math.atan2(x, -y)          # 0 at the front, + toward Ilya's left
        w_l = max(0.0, math.cos(a - math.radians(62))) ** 2 if a > -0.3 else 0.0
        w_r = max(0.0, math.cos(a + math.radians(62))) ** 2 if a < 0.3 else 0.0
        w_b = max(0.0, -math.cos(a)) ** 1.5
        total = w_l + w_r + w_b + 1e-6
        w = {'pelvis': top}
        rest = 1 - top
        for key, share in (('robe_front%d.L', w_l), ('robe_front%d.R', w_r), ('robe_back%d', w_b)):
            share = share / total
            w[key % 1] = w.get(key % 1, 0) + rest * share * upper
            w[key % 2] = w.get(key % 2, 0) + rest * share * (1 - upper)
        return w
    raise RuntimeError('No weights for ' + name)


def bind(obj):
    weights = {}
    for v in obj.data.vertices:
        for b, w in weights_for(obj.name, v.co).items():
            if w > 1e-4:
                weights.setdefault(b, []).append((v.index, w))
    for b, entries in weights.items():
        group = obj.vertex_groups.new(name=b)
        for index, w in entries:
            group.add([index], w, 'REPLACE')
    obj.parent = RIG
    mod = obj.modifiers.new('Rig', 'ARMATURE')
    mod.object = RIG
    mod.use_deform_preserve_volume = False


for obj in PARTS.values():
    bind(obj)


# ---- Floor clamp (geometry nodes): cloth and props that reach the floor lie on it instead of sinking ----
def floor_clamp_group():
    ng = bpy.data.node_groups.new('Lie on the floor', 'GeometryNodeTree')
    ng.interface.new_socket('Geometry', in_out='INPUT', socket_type='NodeSocketGeometry')
    ng.interface.new_socket('Geometry', in_out='OUTPUT', socket_type='NodeSocketGeometry')
    n, l = ng.nodes, ng.links
    gi, go = n.new('NodeGroupInput'), n.new('NodeGroupOutput')
    setp = n.new('GeometryNodeSetPosition')
    pos = n.new('GeometryNodeInputPosition')
    sep = n.new('ShaderNodeSeparateXYZ')
    mx = n.new('ShaderNodeMath')
    mx.operation = 'MAXIMUM'
    mx.inputs[1].default_value = 0.004
    comb = n.new('ShaderNodeCombineXYZ')
    l.new(pos.outputs[0], sep.inputs[0])
    l.new(sep.outputs[0], comb.inputs[0])
    l.new(sep.outputs[1], comb.inputs[1])
    l.new(sep.outputs[2], mx.inputs[0])
    l.new(mx.outputs[0], comb.inputs[2])
    l.new(gi.outputs[0], setp.inputs['Geometry'])
    l.new(comb.outputs[0], setp.inputs['Position'])
    l.new(setp.outputs[0], go.inputs[0])
    return ng


FLOOR = floor_clamp_group()
CONTOUR = 0.0085


def finish_modifiers(obj):
    if obj.name in SUBDIVIDE:
        sub = obj.modifiers.new('Smooth', 'SUBSURF')
        sub.levels = sub.render_levels = 2 if obj.name in ('Hood', 'Robe skirt', 'Mantle', 'Head') else 1
        sub.quality = 3
    clamp = obj.modifiers.new('Floor', 'NODES')
    clamp.node_group = FLOOR
    if obj.name not in NO_CONTOUR:
        obj.data.materials.append(MATERIALS['outline'])
        hull = obj.modifiers.new('Contour', 'SOLIDIFY')
        hull.thickness = CONTOUR
        hull.offset = 1.0
        hull.use_flip_normals = True
        hull.use_rim = False
        # Uniform offset: even thickness divides by the fold's cosine and spikes where cloth folds sharply.
        hull.use_even_offset = False
        hull.use_quality_normals = False
        hull.material_offset = 1


# ---------------------------------------------------------------------------------------------------------------------
# Posing: deltas are rotations about each bone's head in the rig's rest space, accumulated down the hierarchy
# (G_bone = G_parent * D_bone); Blender's matrix_basis is then rest^-1 * D * rest.
# ---------------------------------------------------------------------------------------------------------------------
def rot(x=0.0, y=0.0, z=0.0):
    """Degrees about the rig's axes: +x pitches a raised bone forward (toward -Y), +y leans it to Ilya's left,
    +z turns it to the left."""
    return (Matrix.Rotation(math.radians(z), 3, 'Z') @ Matrix.Rotation(math.radians(y), 3, 'Y')
            @ Matrix.Rotation(math.radians(x), 3, 'X'))


class Pose:
    def __init__(self):
        self.rot = {}             # bone -> 3x3 delta in rest space
        self.offset = Vector()    # pelvis translation
        self.feet = {}            # 'L'/'R' -> (ankle target, foot pitch degrees, foot yaw degrees)
        self.hands = {}           # 'L'/'R' -> (grip or wrist target, 3x3 world rotation of the hand, elbow pole)
        self.scale = {}           # bone -> uniform scale (the flames)
        self.glow = {'lens-flame': 1.0, 'flame': 1.0, 'crucible': 1.0, 'flask': 1.0, 'crack': 1.0}

    def add(self, bone_name, m):
        self.rot[bone_name] = m @ self.rot.get(bone_name, Matrix.Identity(3))


def frame_from(direction, hinge):
    y = direction.normalized()
    x = (hinge - y * hinge.dot(y)).normalized()
    z = x.cross(y)
    return Matrix((x, y, z)).transposed()


def two_bone(a, target, l1, l2, pole):
    d = target - a
    dist = min(max(d.length, abs(l1 - l2) + 1e-4), l1 + l2 - 1e-4)
    dn = d.normalized()
    cos_a = (l1 * l1 + dist * dist - l2 * l2) / (2 * l1 * dist)
    sin_a = math.sqrt(max(0.0, 1 - cos_a * cos_a))
    p = pole - dn * pole.dot(dn)
    if p.length < 1e-6:
        p = Vector((0, -1, 0)) - dn * dn.y
    p.normalize()
    middle = a + dn * (l1 * cos_a) + p * (l1 * sin_a)
    end = a + dn * dist
    return middle, end, dn.cross(p).normalized()


class Solver:
    """Evaluates a Pose into per-bone G matrices (rest space to posed space) and Blender's matrix_basis."""

    def __init__(self, pose):
        self.pose = pose
        self.G = {}
        self.W = {}

    def place(self, b, W_total=None):
        parent = PARENT[b]
        Gp = self.G[parent] if parent else Matrix.Identity(4)
        Wp = self.W[parent] if parent else Matrix.Identity(3)
        h = REST[b].to_translation()
        if W_total is None:
            R = self.pose.rot.get(b, Matrix.Identity(3))
            W_total = Wp @ R
        else:
            R = Wp.inverted() @ W_total
        D = Matrix.Translation(h) @ R.to_4x4() @ Matrix.Translation(-h)
        s = self.pose.scale.get(b)
        if s is not None:
            D = D @ Matrix.Translation(h) @ Matrix.Diagonal((s[0], s[1], s[2], 1)) @ Matrix.Translation(-h)
        if b == 'pelvis':
            D = Matrix.Translation(self.pose.offset) @ D
        self.G[b] = Gp @ D
        self.W[b] = W_total
        return D

    def solve(self):
        basis = {}
        pending = {}
        for b in ORDER:
            side = b[-1] if b[-2:] in ('.L', '.R') else None
            if b.startswith('thigh') and side in self.pose.feet:
                pending.update(self.leg(side))
            if b.startswith('upper_arm') and side in self.pose.hands:
                pending.update(self.arm(side))
            D = self.place(b, pending.get(b))
            basis[b] = REST[b].inverted() @ D @ REST[b]
        return basis

    def leg(self, s):
        target, pitch, yaw = self.pose.feet[s]
        hip = self.G['pelvis'] @ REST['thigh.' + s].to_translation()
        knee0 = REST['shin.' + s].to_translation()
        ankle0 = REST['foot.' + s].to_translation()
        l1 = (knee0 - REST['thigh.' + s].to_translation()).length
        l2 = (ankle0 - knee0).length
        yawm = rot(z=yaw)
        pole = yawm @ Vector((0, -1, 0.15))
        knee, ankle, hinge = two_bone(hip, Vector(target), l1, l2, pole)
        rest_pole = Vector((0, -1, 0.15))
        _, _, rest_hinge = two_bone(REST['thigh.' + s].to_translation(), ankle0, l1, l2, rest_pole)
        thigh_dir0 = knee0 - REST['thigh.' + s].to_translation()
        shin_dir0 = ankle0 - knee0
        W_thigh = frame_from(knee - hip, hinge) @ frame_from(thigh_dir0, rest_hinge).transposed()
        W_shin = frame_from(ankle - knee, hinge) @ frame_from(shin_dir0, rest_hinge).transposed()
        W_foot = yawm @ rot(x=pitch)
        return {'thigh.' + s: W_thigh, 'shin.' + s: W_shin, 'foot.' + s: W_foot}

    def arm(self, s):
        target, hand_rot, pole = self.pose.hands[s]
        shoulder = self.G['chest'] @ REST['upper_arm.' + s].to_translation()
        el0 = REST['forearm.' + s].to_translation()
        wr0 = REST['hand.' + s].to_translation()
        sh0 = REST['upper_arm.' + s].to_translation()
        l1 = (el0 - sh0).length
        l2 = (wr0 - el0).length
        # The target is the fist (the grip) for the staff hand, the wrist for the free hand; the wrist follows from the
        # hand's own rotation either way.
        grip0 = GRIP if s == 'R' else wr0
        wrist_target = Vector(target) - hand_rot @ (grip0 - wr0)
        elbow, wrist, hinge = two_bone(shoulder, wrist_target, l1, l2, Vector(pole))
        rest_pole = Vector((0.3 if s == 'L' else -0.3, 1, -0.1))
        _, _, rest_hinge = two_bone(sh0, wr0, l1, l2, rest_pole)
        W_upper = frame_from(elbow - shoulder, hinge) @ frame_from(el0 - sh0, rest_hinge).transposed()
        W_fore = frame_from(wrist - elbow, hinge) @ frame_from(wr0 - el0, rest_hinge).transposed()
        return {'upper_arm.' + s: W_upper, 'forearm.' + s: W_fore, 'hand.' + s: hand_rot}


def apply(pose):
    basis = Solver(pose).solve()
    for b, m in basis.items():
        RIG.pose.bones[b].matrix_basis = m
    for key, value in pose.glow.items():
        node = MATERIALS[key].node_tree.nodes.get('Ember strength')
        if node:
            node.outputs[0].default_value = value
    return basis


def crucible_screen(pose):
    """The crucible's mouth on screen, in world units from the floor origin (unmirrored), recorded per frame in
    frames.json for effects that want to start at the staff's head."""
    solver = Solver(pose)
    solver.solve()
    world = RIG.matrix_world @ (solver.G['staff'] @ CRUCIBLE_REST)
    return (world.dot(RIGHT), world.dot(UP))


# ---------------------------------------------------------------------------------------------------------------------
# The clips. Every function returns a Pose for a frame; the base stance is shared.
# ---------------------------------------------------------------------------------------------------------------------
STAFF_GRIP_REST = Vector((-0.205, -0.085, 0.60))   # where the right fist holds the staff upright in the stance
LEFT_WRIST_REST = Vector((0.175, -0.035, 0.53))


def stance(breath=0.0, sway=0.0):
    p = Pose()
    # Slightly hunched and quick: the hips forward, the chest rounded, the head lifted to look ahead.
    p.add('pelvis', rot(x=2))
    p.add('spine', rot(x=9 + 0.6 * breath))
    p.add('chest', rot(x=17 - 1.6 * breath))
    p.add('neck', rot(x=12))
    p.add('head', rot(x=-30 + 1.2 * breath, y=3))
    p.offset = Vector((0.004 * sway, 0, 0.003 * breath))
    p.feet['L'] = (Vector((0.074, 0.01, ANKLE_Z)), 0, 8)
    p.feet['R'] = (Vector((-0.07, -0.03, ANKLE_Z)), 0, -10)
    p.hands['R'] = (STAFF_GRIP_REST + Vector((0, 0, 0.004 * breath)), rot(y=-4, x=-2), (-0.5, 0.8, -0.3))
    p.hands['L'] = (LEFT_WRIST_REST + Vector((0, 0, 0.006 * breath)), rot(x=-8, y=6, z=10), (0.6, 0.8, -0.2))
    p.add('hood1', rot(x=4 + 1.5 * breath, y=2 * sway))
    p.add('hood2', rot(x=5 + 2.5 * breath, y=3 * sway))
    return p


def flicker(k, rate=1.0):
    # A deterministic flicker: two incommensurate sines, so eight frames never repeat a shape exactly.
    return 0.82 + 0.12 * math.sin(k * 2.39 * rate + 0.4) + 0.08 * math.sin(k * 5.11 * rate + 1.3)


def flames(p, k, lens=1.0, flare=1.0):
    f = flicker(k)
    g = flicker(k + 3, 1.3)
    # Width, width, height about each flame's root, in the rig's rest axes.
    w, h = lerp(0.85, 1.12, f - 0.6) * lens, lerp(0.8, 1.25, g - 0.6) * lens
    p.scale['lens_flame'] = (w, w, h)
    w, h = lerp(0.85, 1.1, g - 0.6) * flare, lerp(0.75, 1.25, f - 0.6) * flare
    p.scale['crucible_flame'] = (w, w, h)
    p.glow['lens-flame'] = lerp(0.9, 1.25, f - 0.6) * lens
    p.glow['flame'] = lerp(0.95, 1.2, g - 0.6) * max(lens, 0.15)
    p.glow['crucible'] = lerp(0.9, 1.25, g - 0.6) * flare
    p.glow['flask'] = 1.0 + 0.1 * math.sin(k * 1.7)


def idle(k, n):
    b = math.sin(2 * math.pi * k / n)
    sway = math.sin(2 * math.pi * k / n + 1.1)
    p = stance(b, sway)
    flames(p, k)
    return p


def walk_legs(p, phase, lean=10.0):
    """The walk's lower body and its weight: distance-driven, two steps a cycle, the right foot's contact at 0."""
    bob = -0.022 * (0.5 + 0.5 * math.cos(4 * math.pi * phase))
    p.offset = Vector((0.006 * math.sin(2 * math.pi * phase), 0, bob))
    excursion = 0.19
    duty = 0.45
    for s, offset, x in (('R', 0.0, -0.068), ('L', 0.5, 0.068)):
        q = (phase + offset) % 1.0
        if q < duty:      # planted: slides back at the ground's speed
            t = q / duty
            y = lerp(-excursion, excursion, t)
            z = ANKLE_Z
            pitch = lerp(-6, 10, smooth(0.6, 1.0, t))
        else:             # swinging through, lifted
            t = (q - duty) / (1 - duty)
            y = lerp(excursion, -excursion, smooth(0.0, 1.0, t))
            z = ANKLE_Z + 0.075 * math.sin(math.pi * t) ** 1.2
            pitch = lerp(26, -14, t)
        p.feet[s] = (Vector((x, y, z)), pitch, 0)
    swing = math.sin(2 * math.pi * phase)     # +1 while the left leg leads... (the right is planted forward at 0)
    p.add('pelvis', rot(z=7 * swing))
    p.add('spine', rot(x=lean * 0.4, z=-3 * swing))
    p.add('chest', rot(x=lean * 0.35, z=-4 * swing))
    p.add('head', rot(x=-lean * 0.55, z=-2 * swing))
    # The robe answers the legs a moment late: the front panels kick with their leg, the back trails and flutters.
    lag = 2 * math.pi * (phase - 0.07)
    p.add('robe_front1.R', rot(x=-14 * math.cos(lag)))
    p.add('robe_front2.R', rot(x=-12 * math.cos(lag - 0.5)))
    p.add('robe_front1.L', rot(x=14 * math.cos(lag)))
    p.add('robe_front2.L', rot(x=12 * math.cos(lag - 0.5)))
    flutter = math.cos(4 * math.pi * (phase - 0.1))
    p.add('robe_back1', rot(x=9 + 3 * flutter))
    p.add('robe_back2', rot(x=8 + 6 * flutter))
    # The hood's tip trails and bobs after the body: down after each contact, springing back on the passing.
    tip = math.cos(4 * math.pi * (phase - 0.12))
    p.add('hood1', rot(x=8 + 3 * tip, y=-2 * swing))
    p.add('hood2', rot(x=10 + 6 * tip, y=-4 * math.sin(2 * math.pi * (phase - 0.15))))
    return swing


def walk(k, n):
    phase = k / n
    p = stance()
    swing = walk_legs(p, phase)
    # The free arm swings against its leg; the staff hand carries the staff leaning a little forward.
    p.hands['L'] = (LEFT_WRIST_REST + Vector((0.01, 0.06 * swing, 0.02 + 0.015 * abs(swing))),
                    rot(x=-10 - 14 * swing, y=6, z=10), (0.6, 0.8, -0.2))
    p.hands['R'] = (STAFF_GRIP_REST + Vector((0, -0.05 * swing - 0.02, 0.01 * abs(swing))),
                    rot(x=12 + 8 * swing, y=-6), (-0.5, 0.8, -0.3))
    flames(p, k, flare=1.0)
    return p


def strike_upper(p, t):
    """The staff thrust: the crucible whipped forward along the line of the attack, the free hand back for balance.
    t is the strike's progress; it begins at the hit, as the damage already landed (VanguardAnimator.Attacked): most of
    the way through the swing, then the full thrust, a small overshoot, the recoil and the settle."""
    reach = [0.7, 1.0, 1.08, 0.45, 0.12][min(4, int(t * 5))] if t < 1 else 0.0
    p.add('spine', rot(x=6 * reach, z=-10 * reach))
    p.add('chest', rot(x=8 * reach, z=-14 * reach))
    p.add('head', rot(x=-6 * reach, z=10 * reach))
    # Aimed forward and up, like a bolt thrown from the crucible: it reads from every facing and keeps the frame narrow.
    grip = STAFF_GRIP_REST.lerp(Vector((-0.12, -0.25, 0.72)), reach)
    p.hands['R'] = (grip, rot(x=lerp(-2, 50, reach), y=lerp(-4, 8, reach), z=lerp(0, 18, reach)),
                    (-0.9, 0.3, -0.4))
    p.hands['L'] = (LEFT_WRIST_REST.lerp(Vector((0.2, 0.08, 0.6)), reach),
                    rot(x=lerp(-8, 30, reach), y=6, z=10), (0.6, 0.8, -0.2))
    p.add('hood2', rot(x=-6 * reach))
    return reach


def strike(k, n):
    t = (k + 0.5) / n
    p = stance()
    reach = strike_upper(p, t)
    p.add('pelvis', rot(z=-6 * reach))
    p.feet['R'] = (Vector((-0.075, -0.09, ANKLE_Z)), 0, -14)
    p.feet['L'] = (Vector((0.075, 0.06, ANKLE_Z)), 0, 10)
    p.add('robe_back1', rot(x=6 * reach))
    p.add('robe_back2', rot(x=8 * reach))
    # The crucible flares at the thrust and dies back as the staff returns.
    flames(p, k, flare=[1.7, 2.6, 1.9, 1.25, 1.0][k])
    return p


def walk_strike(k, n):
    """The walk's legs and weight under the strike's thrust: shown for a strike's duration while Ilya walks, at the
    walk's own frame for the distance, so the stride never stops for an attack."""
    p = walk(k, n)
    strike_upper(p, 0.3)
    flames(p, k, flare=2.2)
    return p


def cast(k, n):
    """Crucible Vent: gather both hands to the chest, then push both forward - the staff's crucible driven at the
    floor ahead, the free palm open - the robe and hood blown back by the eruption."""
    keys = [  # gather, push, the eruption's peak, recover, settle
        dict(push=-0.6, lean=-4, crouch=0.02, flare=1.3),
        dict(push=0.45, lean=7, crouch=0.025, flare=1.9),
        dict(push=1.0, lean=15, crouch=0.03, flare=2.8),
        dict(push=0.5, lean=7, crouch=0.016, flare=1.6),
        dict(push=0.1, lean=1, crouch=0.004, flare=1.1),
    ]
    c = keys[k]
    p = stance()
    push = c['push']
    p.offset = Vector((0, 0.01 * push, -c['crouch']))
    p.add('spine', rot(x=c['lean'] * 0.5))
    p.add('chest', rot(x=c['lean'] * 0.6))
    p.add('head', rot(x=-c['lean'] * 0.5))
    p.feet['R'] = (Vector((-0.08, -0.1, ANKLE_Z)), 0, -12)
    p.feet['L'] = (Vector((0.08, 0.07, ANKLE_Z)), 0, 12)
    gather = max(0.0, -push)
    out = max(0.0, push)
    grip = STAFF_GRIP_REST.lerp(Vector((-0.06, -0.12, 0.72)), gather).lerp(Vector((-0.08, -0.3, 0.66)), out)
    p.hands['R'] = (grip, rot(x=lerp(-2, 44, out) - 10 * gather, y=lerp(-4, 4, out), z=lerp(0, 10, out)),
                    (-0.9, 0.4, -0.3))
    palm = LEFT_WRIST_REST.lerp(Vector((0.05, -0.08, 0.74)), gather).lerp(Vector((0.08, -0.34, 0.72)), out)
    p.hands['L'] = (palm, rot(x=lerp(-8, -80, out) - 30 * gather, y=lerp(6, 0, out), z=lerp(10, -6, out)),
                    (0.9, 0.4, -0.3))
    blown = out * 1.0
    p.add('robe_back1', rot(x=6 + 10 * blown))
    p.add('robe_back2', rot(x=6 + 14 * blown))
    p.add('robe_front1.L', rot(x=-4 * blown, y=6 * blown))
    p.add('robe_front1.R', rot(x=-4 * blown, y=-6 * blown))
    p.add('hood1', rot(x=4 + 8 * blown))
    p.add('hood2', rot(x=6 + 14 * blown))
    flames(p, k, flare=c['flare'])
    p.glow['flask'] = 1.0 + 0.6 * out
    return p


def hit(k, n):
    """A flinch: the chest snaps back and the head turns from the blow, the hips give a little, the free hand flies up,
    the hood's tip whips forward a beat late and the lens flame jumps; then back to the stance."""
    amount = [1.0, 0.7, 0.28][k]
    whip = [0.4, 1.0, 0.45][k]
    p = stance()
    p.offset = Vector((0, 0.035 * amount, -0.018 * amount))
    p.add('pelvis', rot(x=-5 * amount))
    p.add('spine', rot(x=-11 * amount))
    p.add('chest', rot(x=-13 * amount, y=4 * amount))
    p.add('head', rot(x=-12 * amount, z=10 * amount))
    p.hands['R'] = (STAFF_GRIP_REST + Vector((-0.025, 0.06, 0.035)) * amount, rot(x=-16 * amount, y=-12 * amount),
                    (-0.5, 0.8, -0.3))
    p.hands['L'] = (LEFT_WRIST_REST + Vector((0.05, -0.02, 0.12)) * amount, rot(x=-40 * amount, y=6, z=10),
                    (0.6, 0.8, -0.2))
    p.add('hood1', rot(x=6 * whip))
    p.add('hood2', rot(x=14 * whip))
    p.add('robe_back1', rot(x=-7 * amount))
    p.add('robe_front1.L', rot(x=4 * amount))
    p.add('robe_front1.R', rot(x=4 * amount))
    flames(p, k, lens=1.0 + 0.45 * amount)
    return p


def death(k, n):
    """A stagger, then the knees go and Ilya sinks into the robe, curling forward over the knees; the staff topples
    to the floor still in hand and the flame in the lens dims to an ember. "Ah. That one was mine." A heap rather than
    a body laid out, so it reads from every facing and stays compact. The root never moves."""
    keys = [  # hips' drop, recoil, slump, staff's fall, lens, hood's flop
        dict(drop=0.0, back=1.0, slump=0.0, staff=0.0, lens=1.0, flop=0.0),
        dict(drop=0.1, back=0.35, slump=0.2, staff=0.18, lens=0.85, flop=0.2),
        dict(drop=0.22, back=0.0, slump=0.5, staff=0.45, lens=0.7, flop=0.5),
        dict(drop=0.29, back=0.0, slump=0.8, staff=0.78, lens=0.5, flop=0.8),
        dict(drop=0.325, back=0.0, slump=1.0, staff=1.0, lens=0.2, flop=1.0),
    ]
    c = keys[k]
    p = stance()
    drop, slump = c['drop'], c['slump']
    p.offset = Vector((0, 0.035 * c['back'] + 0.03 * slump, -drop))
    p.add('pelvis', rot(x=-5 * c['back'] + 10 * slump))
    p.add('spine', rot(x=-9 * c['back'] + 24 * slump))
    p.add('chest', rot(x=-8 * c['back'] + 22 * slump))
    p.add('neck', rot(x=14 * slump))
    p.add('head', rot(x=-10 * c['back'] + 18 * slump, z=-8 * slump))
    # Kneeling: the knees come down in front and the shins fold back along the floor.
    kneel = smooth(0.06, 0.3, drop)
    for s, x in (('R', -0.075), ('L', 0.075)):
        p.feet[s] = (Vector((x, lerp(-0.02, 0.15, kneel), lerp(ANKLE_Z, 0.04, kneel))), lerp(0, 75, kneel), 0)
    # The staff topples to Ilya's right and forward, still in the fist, which comes down with it to the floor.
    st = c['staff']
    p.hands['R'] = (Vector((lerp(-0.205, -0.24, st), lerp(-0.085, -0.2, st), lerp(0.6, 0.06, st) - 0.04 * c['back'])),
                    rot(x=lerp(-2, 40, st), y=lerp(-4, -52, st)), (-0.6, 0.8, -0.2))
    p.hands['L'] = (Vector((lerp(0.175, 0.13, slump), lerp(-0.035, -0.2, slump), lerp(0.53, 0.07, slump))),
                    rot(x=lerp(-8, 60, slump), y=6, z=10), (0.6, 0.8, -0.2))
    # The robe pools: every panel splays outward as the hips come down, and the floor flattens what reaches it.
    pool = smooth(0.04, 0.32, drop)
    p.add('robe_back1', rot(x=24 * pool - 10 * slump))
    p.add('robe_back2', rot(x=30 * pool))
    p.add('robe_front1.L', rot(x=-22 * pool, y=18 * pool))
    p.add('robe_front2.L', rot(x=-26 * pool, y=22 * pool))
    p.add('robe_front1.R', rot(x=-22 * pool, y=-18 * pool))
    p.add('robe_front2.R', rot(x=-26 * pool, y=-22 * pool))
    # The hood's tip flops down as the head bows.
    p.add('hood1', rot(x=8 * c['flop'], y=10 * c['flop']))
    p.add('hood2', rot(x=14 * c['flop'], y=26 * c['flop']))
    flames(p, k, lens=c['lens'], flare=lerp(1.0, 0.15, st))
    p.glow['flask'] = lerp(1.0, 0.5, slump)
    p.glow['crack'] = lerp(1.0, 0.35, slump)
    return p


CLIP_POSES = {'idle': idle, 'walk': walk, 'walk-strike': walk_strike, 'strike': strike, 'cast': cast, 'hit': hit,
              'death': death}


# ---------------------------------------------------------------------------------------------------------------------
# Baked contact occlusion: rays from every vertex in the stance, against the body that does not swing about.
# ---------------------------------------------------------------------------------------------------------------------
def bake_occlusion():
    apply(idle(0, 8))
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    verts, polys = [], []
    for obj in OCCLUDERS:
        ev = obj.evaluated_get(deps)
        mesh = ev.to_mesh()
        base = len(verts)
        verts.extend([v.co.copy() for v in mesh.vertices])
        polys.extend([[base + i for i in p.vertices] for p in mesh.polygons])
        ev.to_mesh_clear()
    tree = BVHTree.FromPolygons(verts, polys, all_triangles=False, epsilon=0.0)
    # Fixed directions on the hemisphere (a Fibonacci spiral), so the bake is the same on every run.
    dirs = []
    count = 40
    for i in range(count):
        zc = 1 - (i + 0.5) / count
        r = math.sqrt(max(0.0, 1 - zc * zc))
        a = i * 2.399963
        dirs.append(Vector((r * math.cos(a), r * math.sin(a), zc)))
    reach = 0.11
    for obj in PARTS.values():
        ev = obj.evaluated_get(deps)
        mesh = ev.to_mesh()
        values = []
        for v in mesh.vertices:
            n = v.normal.normalized()
            t = n.orthogonal().normalized()
            b = n.cross(t)
            origin = v.co + n * 0.004
            occluded = 0.0
            for d in dirs:
                ray = t * d.x + b * d.y + n * d.z
                hit = tree.ray_cast(origin, ray, reach)
                if hit[0] is not None:
                    occluded += (1 - hit[3] / reach) * d.z
            values.append(occluded)
        ev.to_mesh_clear()
        norm = sum(d.z for d in dirs)
        attr = obj.data.color_attributes.new('occlusion', 'FLOAT_COLOR', 'POINT')
        for i, o in enumerate(values):
            open_ = max(0.0, 1 - 1.6 * o / norm)
            attr.data[i].color = (open_, open_, open_, 1.0)


bake_occlusion()
for obj in PARTS.values():
    finish_modifiers(obj)


# ---------------------------------------------------------------------------------------------------------------------
# Camera, facings and the frame loop
# ---------------------------------------------------------------------------------------------------------------------
RIG.scale = (1, 1, HEIGHT)
camera_data = bpy.data.cameras.new('Game view: orthographic, 30 degrees down, looking north')
camera_data.type = 'ORTHO'
camera_data.ortho_scale = CANVAS
camera_data.clip_start = 0.1
camera_data.clip_end = 100
camera = bpy.data.objects.new('Camera', camera_data)
collection.objects.link(camera)
scene.camera = camera
# The canvas centre sits this far up the screen from the floor origin, so the origin lands on PIVOT.
centre = (CANVAS * PPU / 2 - PIVOT[0]) / PPU * RIGHT + (CANVAS * PPU / 2 - PIVOT[1]) / PPU * UP
camera.location = centre - FORWARD * 20
camera.rotation_euler = (math.pi / 2 - ELEVATION, 0, 0)


def set_facing(degrees):
    RIG.rotation_euler = (0, 0, math.radians(degrees - 270))


def render_all():
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    started = time.time()
    for facing, degrees in FACING_SET:
        set_facing(degrees)
        for clip in CLIP_SET:
            frames, fps, loops = CLIPS[clip]
            make = CLIP_POSES[clip]
            shown = [min(int(PREVIEW_FRAME), frames - 1)] if PREVIEW and PREVIEW_FRAME else [0] if PREVIEW else range(frames)
            for k in shown:
                pose = make(k, frames)
                apply(pose)
                bpy.context.view_layer.update()
                path = OUT / facing / ('%s_%d.png' % (clip, k))
                path.parent.mkdir(parents=True, exist_ok=True)
                scene.render.filepath = str(path)
                bpy.ops.render.render(write_still=True)
                cx, cy = crucible_screen(pose)
                records.append(dict(facing=facing, clip=clip, frame=k, file=str(path.relative_to(OUT)).replace('\\', '/'),
                                    crucible=[round(cx, 5), round(cy, 5)]))
                print('ARCANIST_FRAME', facing, clip, k, '%.1fs' % (time.time() - started), flush=True)
    meta = dict(blender=bpy.app.version_string, ppu=PPU, supersample=SS, canvas=CANVAS, pivot=PIVOT,
                elevation_degrees=30, height_compensation=HEIGHT, stride=STRIDE,
                clips={c: dict(frames=CLIPS[c][0], fps=CLIPS[c][1], loops=CLIPS[c][2]) for c in CLIPS},
                facings=[f for f, _ in FACINGS], frames=records, preview=PREVIEW)
    existing = OUT / 'frames.json'
    if existing.exists() and (PREVIEW or wanted_facings or wanted_clips):
        old = json.loads(existing.read_text())
        keep = {(r['facing'], r['clip'], r['frame']): r for r in old.get('frames', [])}
        for r in records:
            keep[(r['facing'], r['clip'], r['frame'])] = r
        meta['frames'] = list(keep.values())
    existing.write_text(json.dumps(meta, indent=1))


def bake_actions():
    """One action per clip on the rig, so the .blend can be scrubbed; the frame loop itself poses directly."""
    RIG.animation_data_create()
    for clip, (frames, fps, loops) in CLIPS.items():
        action = bpy.data.actions.new('Ilya ' + clip)
        action.use_fake_user = True
        RIG.animation_data.action = action
        for k in range(frames):
            apply(CLIP_POSES[clip](k, frames))
            for pb in RIG.pose.bones:
                pb.keyframe_insert('location', frame=k + 1)
                pb.keyframe_insert('rotation_quaternion', frame=k + 1)
                pb.keyframe_insert('scale', frame=k + 1)
    RIG.animation_data.action = bpy.data.actions['Ilya idle']


def budget():
    deps = bpy.context.evaluated_depsgraph_get()
    verts = tris = 0
    for obj in PARTS.values():
        mesh = obj.evaluated_get(deps).to_mesh()
        mesh.calc_loop_triangles()
        verts += len(mesh.vertices)
        tris += len(mesh.loop_triangles)
        obj.evaluated_get(deps).to_mesh_clear()
    return verts, tris


if RENDER:
    render_all()
bake_actions()
set_facing(270)
apply(idle(0, 8))
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND), compress=True)
v, t = budget()
print('ARCANIST_COMPLETE', 'vertices', v, 'triangles', t, flush=True)
