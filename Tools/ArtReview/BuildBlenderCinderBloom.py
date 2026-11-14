"""The Cinder Bloom, floor 3's elite (docs/52 section 5, docs/51): a reproducible stylised model, rig, poses and the
sprite render for its EnemyArtSet - the elite contract the Grunt Captain's set uses (six bodies a facing, the strike
windup, the two remains and its own death) - and its open state, drawn in the garden's palette from the start (it has
no themed copy: it is born on floor 3).

Run with Blender 4.5 LTS, headless:

    blender --background --factory-startup --python Tools/ArtReview/BuildBlenderCinderBloom.py -- [options]

Options after the "--":
    --out DIR        raw frames and frames.json (default TestResults/cinder-bloom-render, git-ignored)
    --blend PATH     the saved scene (default ArtDirection/2026-11-14/cinder-bloom-01/cinder-bloom.blend)
    --poses LIST     a comma-separated subset of idle,step-a,step-b,strike,pass-a,pass-b,windup,unfold-0,unfold-1,open,
                     dying-0..dying-6,death-collapse,death-cool
    --facings LIST   a comma-separated subset of front,rear
    --samples N      Cycles samples per render pixel (default 10)
    --no-render      build and save the .blend, render nothing

Who it is: a foundry golem left standing when the fires went out; the garden grew a bloom in its cold furnace. A squat,
heavy figure of charred bark plates over a cast-iron furnace barrel, a verdigris ring round the furnace mouth in its
chest, a small head sunk between the shoulders under two dead branches, a cold chimney stack on its back with a twig
grown out of it, and a bark maul (a log bound in verdigris on a heartwood haft) carried on its right shoulder.

Its cycle (docs/52 section 5) is two silhouettes, not only two colours:
- CLOSED (the contract's drawings): two bark shutters close the furnace mouth, a thin orchid seam between them; the
  maul rides the shoulder and it walks and swings.
- OPEN (extra drawings: unfold-0, unfold-1, open; closing plays the unfold back, docs/52 section 5): it plants its feet, roots run from them into the loam, the
  maul's head drops to the floor at its side, the chest lifts, the shutters swing wide and an orchid bloom unfolds out of
  the furnace mouth, wider than the chest - the brightest shape on the floor: strike here. The furnace's rear grate
  lights with it, so the open state reads from behind as well.

Readability (docs/51 section 7, docs/52 section 2 rule 3): bark a step lighter than the loam with a dark outline; orchid
(hue 280-330) is the creature's only light; no gloamlight (that is the garden's) and no red-orange (the telegraphs')
anywhere on it. The bloom, the furnace seam and grate and the eyes are separate parts that the beauty pass draws black;
an aux pass draws only them - red their coverage, green a tone (lit and base-to-tip, 0..1), blue their glow -
so PackCinderBloomSprites.py lays the orchid ramp and its value in afterwards and the bloom can be tuned without
rendering again (docs/52 section 5 "Art": the bloom as a separate emissive part).

Nothing here is generated imagery: the geometry is lofted rings, panels and primitives, every rigid part rides one bone,
the bloom and the roots are rebuilt from a formula for each pose (constant topology), the shading is one authored toon
node group (BuildBlenderMirrorSentry.py's, its cold rim swapped for an ash rim and a moss-shade rim), and every frame is
a pose computed below.

Camera: orthographic, looking north and down 30 degrees, the game's 2:1 floor (ArenaFloor.DepthScale), as
BuildBlenderMirrorSentry.py. Heights are modelled in world units and the rig is stretched by 1/cos(30), so one unit of
height is one world unit on screen. Both facings look left (the game mirrors them for the right); the camera and the
key light never move. The root stays on the floor origin, which projects to the same render pixel: the pivot.
"""
import bpy
import bmesh
import json
import math
import sys
import time
from pathlib import Path
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[2]
ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []


def option(name, default=None):
    if name in ARGV:
        i = ARGV.index(name)
        return ARGV[i + 1] if i + 1 < len(ARGV) and not ARGV[i + 1].startswith('--') else True
    return default


OUT = Path(option('--out', str(ROOT / 'TestResults/cinder-bloom-render')))
BLEND = Path(option('--blend', str(ROOT / 'ArtDirection/2026-11-14/cinder-bloom-01/cinder-bloom.blend')))
SAMPLES = int(option('--samples', 10))
RENDER = not option('--no-render', False)

# ---- The sprite contract (Tools/ArtReview/PackCinderBloomSprites.py reads frames.json) ----
PPU = 128                      # final texels per world unit, as every enemy's
SS = 4                         # render at 4x and box-filter down: area-exact anti-aliasing, the pivot stays on a corner
CANVAS_PX = (288, 288)         # the living body's render canvas in final texels (the packer crops it)
PIVOT_PX = (144, 56)           # the floor origin in final texels from the canvas's bottom-left corner
DEATH_CANVAS_PX = (288, 256)   # the death's: the body bowing forward, the maul fallen beside it
DEATH_PIVOT_PX = (144, 72)
ELEVATION = math.radians(30)
HEIGHT = 1 / math.cos(ELEVATION)
# Both facings look left; the game mirrors them for the right. Degrees as BuildBlenderMirrorSentry.py (270 faces the
# camera, 180 screen left, 90 away): the front turns 25 degrees off the camera so the furnace (and the bloom) is seen
# nearly square; the rear 35 degrees off straight away, so the grate, the stack and one flank show.
FACINGS = [('front', 245), ('rear', 125)]
# Modelled in world units and drawn at 0.88: a head over the Captain, inside a texture budget near the Mirror Sentry's.
SCALE = 0.88
# World units one walk cycle of two steps covers (EnemyArtSet._walkCycleDistance; the Captain's 0.8): a short, heavy gait.
STRIDE = 0.72
BODY_POSES = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b', 'windup']
OPEN_POSES = ['unfold-0', 'unfold-1', 'open']
DYING = ['dying-%d' % k for k in range(7)]
REMAINS = ['death-collapse', 'death-cool']     # drawn once, front: the game mirrors the remains
POSES = BODY_POSES + OPEN_POSES + DYING + REMAINS

wanted_poses = option('--poses')
wanted_facings = option('--facings')
POSE_SET = [p for p in POSES if not wanted_poses or p in wanted_poses.split(',')]
FACING_SET = [f for f in FACINGS if not wanted_facings or f[0] in wanted_facings.split(',')]


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
scene.cycles.seed = 13
scene.cycles.max_bounces = 1
scene.cycles.diffuse_bounces = 0
scene.cycles.glossy_bounces = 0
scene.cycles.transmission_bounces = 0
scene.cycles.volume_bounces = 0
scene.cycles.transparent_max_bounces = 24
scene.cycles.pixel_filter_type = 'BOX'
scene.cycles.filter_width = 1.0
scene.render.film_transparent = True
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
# The key light, fixed to the screen: a dim dusk light from the upper left and a little in front (docs/51 section 5).
KEY = (-0.55 * RIGHT + 0.76 * UP + 0.34 * TOWARD).normalized()
# A pale ash rim from behind the upper left, so the bark's far edge still separates from the loam.
RIM = (-0.62 * RIGHT + 0.58 * UP - 0.52 * TOWARD).normalized()
# The garden's moss shade (docs/51: shadows tinted moss, not black) as a low rim from behind on the right.
SHADE_RIM = (0.58 * RIGHT - 0.25 * UP - 0.78 * TOWARD).normalized()
HALF = (KEY + TOWARD).normalized()


# ---------------------------------------------------------------------------------------------------------------------
# State shared by every material: the eyes' life, the furnace's light, and which pass renders
# ---------------------------------------------------------------------------------------------------------------------
STATE_NAMES = (('Life', 1.0), ('Aux pass', 0.0), ('Preview', 0.0), ('Seam tone', 0.6), ('Seam glow', 0.15),
               ('Grate tone', 0.3), ('Grate glow', 0.0))


def build_state_group():
    ng = bpy.data.node_groups.new('Bloom state', 'ShaderNodeTree')
    for name, _ in STATE_NAMES:
        ng.interface.new_socket(name, in_out='OUTPUT', socket_type='NodeSocketFloat')
    out = ng.nodes.new('NodeGroupOutput')
    for i, (name, value) in enumerate(STATE_NAMES):
        node = ng.nodes.new('ShaderNodeValue')
        node.name = name
        node.outputs[0].default_value = value
        ng.links.new(node.outputs[0], out.inputs[i])
    return ng


STATE = build_state_group()


def set_state(**values):
    for key, value in values.items():
        if value is not None:
            STATE.nodes[key.replace('_', ' ').capitalize()].outputs[0].default_value = value


# ---------------------------------------------------------------------------------------------------------------------
# Shading: one toon group, fed by authored palettes (BuildBlenderMirrorSentry.py's, the garden's rims)
# ---------------------------------------------------------------------------------------------------------------------
def build_toon_group():
    ng = bpy.data.node_groups.new('Bloom toon', 'ShaderNodeTree')
    io = ng.interface
    for name, kind, default in [
            ('Light', 'NodeSocketColor', (1, 1, 1, 1)), ('Mid', 'NodeSocketColor', (.5, .5, .5, 1)),
            ('Shadow', 'NodeSocketColor', (.2, .2, .2, 1)), ('Deep', 'NodeSocketColor', (.05, .05, .05, 1)),
            ('Spec', 'NodeSocketFloat', 0.0), ('Spec Color', 'NodeSocketColor', (1, 1, 1, 1)),
            ('Rim', 'NodeSocketFloat', 0.35), ('Occlusion', 'NodeSocketFloat', 1.0),
            ('Band Shift', 'NodeSocketFloat', 0.0), ('Grain', 'NodeSocketFloat', 0.0),
            ('Facet', 'NodeSocketFloat', 0.5)]:
        socket = io.new_socket(name, in_out='INPUT', socket_type=kind)
        socket.default_value = default
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

    def rgb(hexstr):
        node = n.new('ShaderNodeRGB')
        node.outputs[0].default_value = lin(hexstr)
        return node.outputs[0]

    normal = geo.outputs['Normal']
    half_lambert = math_node('MULTIPLY_ADD', dot(normal, vec(KEY)), 0.5, 0.5)
    facet = math_node('MULTIPLY', math_node('SUBTRACT', gi.outputs['Facet'], 0.5), 0.1)
    lit = math_node('ADD', math_node('ADD', half_lambert, gi.outputs['Band Shift']), facet)
    first = step(lit, 0.36, 0.5)
    second = step(lit, 0.72, 0.84)
    colour = mix(first, gi.outputs['Shadow'], gi.outputs['Mid'])
    colour = mix(second, colour, gi.outputs['Light'])
    coords = n.new('ShaderNodeTexCoord')
    brush = n.new('ShaderNodeTexNoise')
    brush.inputs['Scale'].default_value = 6.0
    brush.inputs['Detail'].default_value = 2.0
    l.new(coords.outputs['Generated'], brush.inputs['Vector'])
    drift = n.new('ShaderNodeCombineColor')
    for channel, spread in enumerate((0.14, 0.13, 0.12)):
        l.new(math_node('MULTIPLY_ADD', brush.outputs['Fac'], spread, 1 - spread / 2), drift.inputs[channel])
    paint = n.new('ShaderNodeMixRGB')
    paint.blend_type = 'MULTIPLY'
    paint.inputs['Fac'].default_value = 1.0
    l.new(colour, paint.inputs['Color1'])
    l.new(drift.outputs[0], paint.inputs['Color2'])
    colour = paint.outputs['Color']
    # Bark: charred plates furrowed up their length - thin, wandering dark fissures (distorted bands in object space,
    # broken by a noise so they never read as planks), the ridges between them catching a little ash where the key
    # falls - only where Grain is set.
    fissure = n.new('ShaderNodeTexWave')
    fissure.wave_type = 'BANDS'
    fissure.bands_direction = 'X'
    fissure.inputs['Scale'].default_value = 6.5
    fissure.inputs['Distortion'].default_value = 10.0
    fissure.inputs['Detail'].default_value = 3.0
    fissure.inputs['Detail Scale'].default_value = 1.6
    fissure.inputs['Detail Roughness'].default_value = 0.65
    l.new(coords.outputs['Object'], fissure.inputs['Vector'])
    breaker = n.new('ShaderNodeTexNoise')
    breaker.inputs['Scale'].default_value = 9.0
    breaker.inputs['Detail'].default_value = 1.0
    l.new(coords.outputs['Object'], breaker.inputs['Vector'])
    line = math_node('MULTIPLY', step(fissure.outputs['Fac'], 0.8, 0.93), step(breaker.outputs['Fac'], 0.38, 0.5))
    furrow = math_node('MULTIPLY', line, gi.outputs['Grain'])
    colour = mix(furrow, colour, gi.outputs['Deep'])
    ridge = step(fissure.outputs['Fac'], 0.25, 0.45)
    ash = math_node('MULTIPLY', math_node('MULTIPLY', math_node('SUBTRACT', 1.0, ridge), second),
                    math_node('MULTIPLY', gi.outputs['Grain'], 0.3))
    colour = mix(ash, colour, rgb('#A39A8E'))
    closed = math_node('SUBTRACT', 1.0, step(gi.outputs['Occlusion'], 0.3, 0.97))
    colour = mix(closed, colour, gi.outputs['Deep'])
    # An ash rim on edges turned to the upper left and away.
    facing = n.new('ShaderNodeLayerWeight')
    facing.inputs['Blend'].default_value = 0.4
    rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.5, 0.82), math_node('MAXIMUM', dot(normal, vec(RIM)), 0.0))
    rim = math_node('MULTIPLY', rim, gi.outputs['Rim'])
    colour = add_colour(colour, rgb('#B9B8A4'), math_node('MULTIPLY', rim, 0.45))
    # The moss shade from behind on the lower right: only the last sliver of the silhouette.
    shade = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.66, 0.9),
                      math_node('MAXIMUM', dot(normal, vec(SHADE_RIM)), 0.0))
    colour = add_colour(colour, rgb('#2F4A40'), math_node('MULTIPLY', shade, 0.6))
    half = math_node('MAXIMUM', dot(normal, vec(HALF)), 0.0)
    spec = math_node('MULTIPLY', step(math_node('POWER', half, 30.0), 0.35, 0.55), gi.outputs['Spec'])
    colour = add_colour(colour, gi.outputs['Spec Color'], spec)
    l.new(colour, go.inputs['Color'])
    return ng


TOON = build_toon_group()

PALETTES = {
    # light, mid, shadow, deep. Charred bark plates: docs/52's #4A3B33 as the shade, the lit planes an ash-brown a step
    # above the loam (#3C3F36 / #63665A), kept desaturated (no warm signal: red-orange is the telegraphs').
    'bark': ('#A09487', '#74665B', '#52443B', '#1E1714'),
    'bark-dark': ('#7E7166', '#584B42', '#3A2F28', '#140F0D'),
    # The maul's haft and the log's end grain: heartwood (docs/51 #5B4838), lifted for the end grain.
    'heartwood': ('#8E7B66', '#64533F', '#43372B', '#1A1510'),
    # The furnace: cold cast iron gone grey-green.
    'iron': ('#5F655D', '#3C413B', '#262A25', '#0E100D'),
    # Verdigris in place of brass (docs/51 #6FA38C).
    'verdigris': ('#A4CDB9', '#6FA38C', '#46705F', '#1F382E'),
}
OUTLINE = '#120F0E'


class Shading:
    """A material: toon colours per palette, emitted once; the scene has no lights. In the aux pass every material emits
    black except the bloom's family (red coverage, green tone, blue glow)."""

    def __init__(self, name):
        self.mat = bpy.data.materials.new(name)
        self.mat.use_nodes = True
        self.nodes = self.mat.node_tree.nodes
        self.links = self.mat.node_tree.links
        for node in list(self.nodes):
            self.nodes.remove(node)
        self.out = self.nodes.new('ShaderNodeOutputMaterial')
        self._occlusion = None
        self._facet = None
        self._state = None

    def state(self, name):
        if self._state is None:
            self._state = self.nodes.new('ShaderNodeGroup')
            self._state.node_tree = STATE
        return self._state.outputs[name]

    def attribute(self, name):
        a = self.nodes.new('ShaderNodeAttribute')
        a.attribute_type = 'GEOMETRY'
        a.attribute_name = name
        return a.outputs['Fac']

    def occlusion(self):
        if self._occlusion is None:
            a = self.nodes.new('ShaderNodeAttribute')
            a.attribute_type = 'GEOMETRY'
            a.attribute_name = 'occlusion'
            sep = self.nodes.new('ShaderNodeSeparateColor')
            self.links.new(a.outputs['Color'], sep.inputs[0])
            self._occlusion = sep.outputs[0]
        return self._occlusion

    def facet(self):
        if self._facet is None:
            self._facet = self.attribute('facet')
        return self._facet

    def toon(self, palette, spec=0.0, spec_color='#D8D6C8', rim=0.4, band_shift=0.0, occlusion=True, grain=0.0):
        light, mid, shadow, deep = PALETTES[palette] if isinstance(palette, str) else palette
        g = self.nodes.new('ShaderNodeGroup')
        g.node_tree = TOON
        g.inputs['Light'].default_value = lin(light)
        g.inputs['Mid'].default_value = lin(mid)
        g.inputs['Shadow'].default_value = lin(shadow)
        g.inputs['Deep'].default_value = lin(deep)
        g.inputs['Spec'].default_value = spec
        g.inputs['Grain'].default_value = grain
        g.inputs['Spec Color'].default_value = lin(spec_color)
        g.inputs['Rim'].default_value = rim
        g.inputs['Band Shift'].default_value = band_shift
        if occlusion:
            self.links.new(self.occlusion(), g.inputs['Occlusion'])
        self.links.new(self.facet(), g.inputs['Facet'])
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

    def combine(self, r, g, b):
        c = self.nodes.new('ShaderNodeCombineColor')
        for i, v in enumerate((r, g, b)):
            if isinstance(v, (int, float)):
                c.inputs[i].default_value = v
            else:
                self.links.new(v, c.inputs[i])
        return c.outputs[0]

    def lit(self):
        """The key's half-lambert on either side of a thin surface (a petal is seen from both)."""
        geo = self.nodes.new('ShaderNodeNewGeometry')
        flip = self.math('MULTIPLY_ADD', geo.outputs['Backfacing'], -2.0, 1.0)
        n = self.nodes.new('ShaderNodeVectorMath')
        n.operation = 'SCALE'
        self.links.new(geo.outputs['Normal'], n.inputs[0])
        self.links.new(flip, n.inputs['Scale'])
        key = self.nodes.new('ShaderNodeCombineXYZ')
        key.inputs[0].default_value, key.inputs[1].default_value, key.inputs[2].default_value = KEY
        d = self.nodes.new('ShaderNodeVectorMath')
        d.operation = 'DOT_PRODUCT'
        self.links.new(n.outputs['Vector'], d.inputs[0])
        self.links.new(key.outputs[0], d.inputs[1])
        return self.math('MULTIPLY_ADD', d.outputs['Value'], 0.5, 0.5)

    def finish(self, colour, aux=None):
        """Emits colour in the beauty pass and aux (or black) in the aux pass."""
        final = self.mix(self.state('Aux pass'), colour, aux if aux is not None else self.rgb('#000000'))
        e = self.nodes.new('ShaderNodeEmission')
        self.links.new(final, e.inputs['Color'])
        e.inputs['Strength'].default_value = 1.0
        self.links.new(e.outputs[0], self.out.inputs['Surface'])
        return self.mat


def plain(name, palette, **kw):
    s = Shading(name)
    return s.finish(s.toon(palette, **kw))


def outline_material():
    s = Shading('Contour (inverted hull)')
    emit = s.nodes.new('ShaderNodeEmission')
    colour = s.mix(s.state('Aux pass'), s.rgb(OUTLINE), s.rgb('#000000'))
    s.links.new(colour, emit.inputs['Color'])
    transparent = s.nodes.new('ShaderNodeBsdfTransparent')
    mix = s.nodes.new('ShaderNodeMixShader')
    s.links.new(s.nodes.new('ShaderNodeNewGeometry').outputs['Backfacing'], mix.inputs['Fac'])
    s.links.new(emit.outputs[0], mix.inputs[1])
    s.links.new(transparent.outputs[0], mix.inputs[2])
    s.links.new(mix.outputs[0], s.out.inputs['Surface'])
    return s.mat


def bloom_material():
    """The petals and the heart: black in the beauty (the packer lays the orchid ramp by coverage, so an edge texel is
    never counted twice), orchid in the saved scene's preview; in the aux pass red is coverage, green the tone - the
    petal's base-to-tip value times the key on either side - and blue the glow (the heart, the petals' bases)."""
    s = Shading('Bloom (tuned in the packer)')
    tone = s.math('MULTIPLY', s.attribute('tone'), s.math('MULTIPLY_ADD', s.lit(), 0.75, 0.45), clamp=True)
    beauty = s.mix(s.state('Preview'), s.rgb('#000000'), s.rgb('#D86BD0'))
    return s.finish(beauty, aux=s.combine(1.0, tone, s.attribute('glow')))


def furnace_light_material(name, tone_state, glow_state, preview):
    """The furnace's light seen through the shutters' seam (front) or the rear grate: a flat plate whose tone and glow
    are the pose's (closed a thin orchid seam, open a lit furnace)."""
    s = Shading(name)
    beauty = s.mix(s.state('Preview'), s.rgb('#000000'), s.rgb(preview))
    return s.finish(beauty, aux=s.combine(1.0, s.state(tone_state), s.state(glow_state)))


def eyes_material():
    """Two small orchid eyes, dimming as it dies."""
    s = Shading('Eyes')
    life = s.state('Life')
    beauty = s.mix(s.state('Preview'), s.rgb('#000000'), s.rgb('#D86BD0'))
    return s.finish(beauty, aux=s.combine(1.0, s.math('MULTIPLY_ADD', life, 0.45, 0.25),
                                          s.math('MULTIPLY', life, 0.35)))


MATERIALS = {}


def build_materials():
    m = MATERIALS
    m['outline'] = outline_material()
    m['bark'] = plain('Charred bark plates', 'bark', spec=0.1, rim=0.45, grain=0.85)
    m['bark-dark'] = plain('Bark limbs', 'bark-dark', spec=0.06, rim=0.35, grain=0.7)
    m['heartwood'] = plain('Heartwood haft', 'heartwood', spec=0.1, rim=0.35, grain=0.0)
    m['iron'] = plain('Cold furnace iron', 'iron', spec=0.3, spec_color='#B8C2B4', rim=0.3)
    m['shutter'] = plain('Shutter bark', 'bark', spec=0.1, rim=0.45, grain=0.85, occlusion=False)
    m['verdigris'] = plain('Verdigris', 'verdigris', spec=0.45, spec_color='#DDF0E6', rim=0.4, band_shift=0.04)
    m['bloom'] = bloom_material()
    m['seam'] = furnace_light_material('Furnace light (front)', 'Seam tone', 'Seam glow', '#B04CB0')
    m['grate'] = furnace_light_material('Furnace light (rear grate)', 'Grate tone', 'Grate glow', '#7A2E80')
    m['eyes'] = eyes_material()


build_materials()


# ---------------------------------------------------------------------------------------------------------------------
# Geometry helpers (BuildBlenderMirrorSentry.py's)
# ---------------------------------------------------------------------------------------------------------------------
PARTS = {}      # name -> object
INFO = {}       # name -> dict(bone, material, contour, chamfer, occluder, show)


def new_object(name, verts, faces, bone, material, contour=True, chamfer=0.006, occluder=False, flat=True, show=None):
    """show: None (always), or a predicate on the pose deciding whether the part renders."""
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in verts], [], faces)
    mesh.validate()
    bm = bmesh.new()
    bm.from_mesh(mesh)
    if all(len(e.link_faces) == 2 for e in bm.edges):
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    for poly in mesh.polygons:
        poly.use_smooth = not flat
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.data.materials.append(MATERIALS[material])
    PARTS[name] = obj
    INFO[name] = dict(bone=bone, material=material, contour=contour, chamfer=chamfer, occluder=occluder, show=show)
    return obj


def loft(name, rings, bone, material, closed=True, cap_start=False, cap_end=False, **kw):
    n = len(rings[0])
    m = len(rings)
    verts = [p for ring in rings for p in ring]
    faces = []
    cols = n if closed else n - 1
    for j in range(m - 1):
        for i in range(cols):
            i1 = (i + 1) % n
            faces.append((j * n + i, j * n + i1, (j + 1) * n + i1, (j + 1) * n + i))
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
    return new_object(name, verts, faces, bone, material, **kw)


def ring_points(cx, cy, z, rx, ry, count, start=-math.pi / 2, keel=0.0, back=0.0):
    """A polygon ring (counter-clockwise from above, first point at the front, -Y)."""
    pts = []
    for i in range(count):
        a = start + 2 * math.pi * i / count
        x, y = rx * math.cos(a), ry * math.sin(a)
        front = max(0.0, -math.sin(a))
        rear = max(0.0, math.sin(a))
        y -= keel * front ** 6
        y += back * rear ** 2
        pts.append(Vector((cx + x, cy + y, z)))
    return pts


def ellipsoid(name, centre, radii, bone, material, rings=6, segments=10, axes=None, lower=-1.0, upper=1.0, **kw):
    ax = axes or (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))
    verts, faces = [], []
    lats = [math.asin(lerp(lower, upper, j / rings)) for j in range(rings + 1)]
    for lat in lats:
        for i in range(segments):
            lon = 2 * math.pi * i / segments
            p = (ax[0] * (radii[0] * math.cos(lat) * math.cos(lon)) + ax[1] * (radii[1] * math.cos(lat) * math.sin(lon))
                 + ax[2] * (radii[2] * math.sin(lat)))
            verts.append(Vector(centre) + p)
    for j in range(rings):
        for i in range(segments):
            i1 = (i + 1) % segments
            faces.append((j * segments + i, j * segments + i1, (j + 1) * segments + i1, (j + 1) * segments + i))
    for cap, j, flip in [(lower > -0.999, 0, True), (upper < 0.999, rings, False)]:
        if not cap:
            continue
        ring = verts[j * segments:(j + 1) * segments]
        verts.append(sum(ring, Vector()) / segments)
        c = len(verts) - 1
        for i in range(segments):
            i1 = (i + 1) % segments
            face = (j * segments + i, j * segments + i1, c)
            faces.append(tuple(reversed(face)) if flip else face)
    if lower <= -0.999:
        verts.append(Vector(centre) - ax[2] * radii[2])
        c = len(verts) - 1
        for i in range(segments):
            faces.append((c, (i + 1) % segments, i))
    if upper >= 0.999:
        verts.append(Vector(centre) + ax[2] * radii[2])
        c = len(verts) - 1
        for i in range(segments):
            faces.append((c, rings * segments + i, rings * segments + (i + 1) % segments))
    return new_object(name, verts, faces, bone, material, **kw)


def box(name, centre, size, bone, material, axes=None, taper=1.0, **kw):
    ax = axes or (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))
    hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
    verts = []
    for sz in (-1, 1):
        t = taper if sz > 0 else 1.0
        for sy, sx in ((-1, -1), (-1, 1), (1, 1), (1, -1)):
            verts.append(Vector(centre) + ax[0] * (sx * hx * t) + ax[1] * (sy * hy * t) + ax[2] * (sz * hz))
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return new_object(name, verts, faces, bone, material, **kw)


def tube_rings(path, radii, count=8, twist_ref=Vector((0, -1, 0))):
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
        rings.append([p + normal * (rx * math.cos(2 * math.pi * i / count)) + binormal * (ry * math.sin(2 * math.pi * i / count))
                      for i in range(count)])
    return rings


def tube(name, path, radii, bone, material, count=8, cap_start=True, cap_end=True, twist_ref=Vector((0, -1, 0)), **kw):
    return loft(name, tube_rings(path, radii, count, twist_ref), bone, material, closed=True, cap_start=cap_start,
                cap_end=cap_end, **kw)


def plate(name, rows, thickness, bone, material, inward, **kw):
    """A curved plate: rows of points (its outer face, top row first), solidified inward along inward(p) by thickness
    - a bark plate or a shutter, closed on all sides so it outlines and occludes as a solid."""
    nr, nc = len(rows), len(rows[0])
    outer = [p for row in rows for p in row]
    inner = [p - inward(p) * thickness for p in outer]
    verts = outer + inner
    off = nr * nc
    faces = []
    for j in range(nr - 1):
        for i in range(nc - 1):
            a, b, c, d = j * nc + i, j * nc + i + 1, (j + 1) * nc + i + 1, (j + 1) * nc + i
            faces.append((a, b, c, d))
            faces.append((off + d, off + c, off + b, off + a))
    for j in range(nr - 1):
        for i in (0, nc - 1):
            a, d = j * nc + i, (j + 1) * nc + i
            faces.append((a, d, off + d, off + a) if i == 0 else (d, a, off + a, off + d))
    for i in range(nc - 1):
        for j in (0, nr - 1):
            a, b = j * nc + i, j * nc + i + 1
            faces.append((b, a, off + a, off + b) if j == 0 else (a, b, off + b, off + a))
    return new_object(name, verts, faces, bone, material, **kw)


# ---- Measurements (world units, feet on z = 0, facing -Y, the Bloom's left is +X) ----
# Squat and heavy: short trunk legs, a furnace barrel for a body, long arms, a small head sunk forward between big bark
# shoulders, two dead branches over it. Taller than the Captain (1.06) by its shoulders and branches, not its legs.
ANKLE_Z, KNEE_Z, HIP_Z = 0.08, 0.25, 0.43
HIP_X = 0.135
WAIST_Z = 0.5
CHEST_Z = 0.64
NECK_Z = 0.9
SHOULDER = Vector((0.305, 0.03, 0.87))
ELBOW = Vector((0.37, 0.06, 0.65))
WRIST = Vector((0.385, 0.03, 0.46))
FIST = Vector((0.39, 0.02, 0.405))
HEAD_BASE = Vector((0, -0.075, 0.9))
HEAD_TOP = 1.1
# The furnace barrel's rings: z, radius across, radius deep.
BARREL = [(0.45, 0.22, 0.18), (0.53, 0.262, 0.212), (0.66, 0.285, 0.228), (0.8, 0.282, 0.224), (0.9, 0.25, 0.2),
          (0.96, 0.19, 0.16)]
# The furnace mouth in the chest: its centre (on the barrel's face), its radius, the depth of the cavity behind it.
MOUTH = Vector((0.0, -0.232, 0.69))
MOUTH_R = 0.125
CAVITY_DEPTH = 0.075
SHUTTER_GAP = 0.016            # each shutter's edge stands this far off the middle: the closed seam
SHUTTER_R = 0.2                # the shutters close the whole furnace face
SHUTTER_OFF = 0.07             # how far the shutters stand proud of the furnace face (clear of the ring)
# The bloom grows out of the cavity along this axis: forward and a little up, so the camera (30 degrees down) sees it
# face on.
BLOOM_AXIS = Vector((0, -math.cos(math.radians(18)), math.sin(math.radians(18))))
# The maul at rest, along +z from the haft's butt: the haft, the log head across x, and where the two hands grip.
HAFT = 0.8
MAUL_HEAD_Z = 0.72
MAUL_HEAD_R = 0.088
MAUL_HEAD_HALF = 0.15
GRIP_R = Vector((0, 0, 0.1))
GRIP_L = Vector((0, 0, 0.44))


def mirror_x(v):
    return Vector((-v.x, v.y, v.z))


def side_point(v, side):
    return v if side > 0 else mirror_x(v)


def barrel_radius(z):
    """The barrel's (rx, ry) at a height, interpolated between its rings."""
    for (z0, rx0, ry0), (z1, rx1, ry1) in zip(BARREL, BARREL[1:]):
        if z0 <= z <= z1:
            t = (z - z0) / (z1 - z0)
            return lerp(rx0, rx1, t), lerp(ry0, ry1, t)
    return (BARREL[0][1], BARREL[0][2]) if z < BARREL[0][0] else (BARREL[-1][1], BARREL[-1][2])


def barrel_point(theta, z, out=0.0):
    """A point on the barrel at an angle from its front (-Y), + toward its left (+X), pushed out by out."""
    rx, ry = barrel_radius(z)
    return Vector(((rx + out) * math.sin(theta), -(ry + out) * math.cos(theta), z))


def barrel_normal(p):
    rx, ry = barrel_radius(p.z)
    n = Vector((p.x / (rx * rx), p.y / (ry * ry), 0))
    return n.normalized() if n.length > 1e-9 else Vector((0, -1, 0))


def branch(name, start, path_dirs, length, radius, bone, material='bark-dark', **kw):
    """A dead branch: a tapering tube along successive directions (each a share of its length)."""
    pts = [Vector(start)]
    for d, share in path_dirs:
        pts.append(pts[-1] + Vector(d).normalized() * (length * share))
    radii = [radius * (1 - 0.82 * k / (len(pts) - 1)) for k in range(len(pts))]
    return tube(name, pts, radii, bone, material, count=6, chamfer=0.0, **kw)


def build_legs():
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        x = HIP_X * side
        # The foot: a wide stump on a flat sole, three root toes splayed forward.
        rings = [ring_points(x, -0.01, z, rx, ry, 10) for z, rx, ry in
                 ((0.004, 0.105, 0.11), (0.05, 0.1, 0.104), (0.1, 0.082, 0.084))]
        loft('Stump ' + s, rings, 'foot.' + s, 'bark-dark', cap_start=True, cap_end=True, occluder=True)
        for k, a in enumerate((-32, 0, 30)):
            d = Vector((math.sin(math.radians(a + 8 * side)), -math.cos(math.radians(a + 8 * side)), 0))
            base = Vector((x, -0.01, 0.03)) + d * 0.07
            tube('Toe %s %d' % (s, k), [base, base + d * 0.06 + Vector((0, 0, -0.012)), base + d * 0.105 + Vector((0, 0, -0.022))],
                 [0.03, 0.022, 0.008], 'foot.' + s, 'bark-dark', count=6, chamfer=0.0)
        # The shin: a short trunk, flaring to the stump.
        tube('Shin ' + s, [Vector((x, 0.0, ANKLE_Z)), Vector((x, 0.0, (ANKLE_Z + KNEE_Z) / 2)), Vector((x, 0.0, KNEE_Z + 0.02))],
             [0.09, 0.08, 0.078], 'shin.' + s, 'bark-dark', count=9, occluder=True)
        # A bark greave over its front.
        rows = []
        for z in (KNEE_Z + 0.01, (ANKLE_Z + KNEE_Z) / 2 + 0.01, ANKLE_Z + 0.03):
            rows.append([Vector((x + 0.095 * math.sin(a), -0.095 * math.cos(a) - 0.005, z))
                         for a in [math.radians(t) for t in (-62, -24, 12, 50)]])
        plate('Greave ' + s, rows, 0.022, 'shin.' + s, 'bark',
              inward=lambda p, x=x: Vector((p.x - x, p.y, 0)).normalized())
        # The knee: a knot.
        ellipsoid('Knee ' + s, Vector((x, -0.035, KNEE_Z + 0.015)), (0.074, 0.066, 0.06), 'shin.' + s, 'bark',
                  rings=4, segments=8)
        # The thigh: a thick trunk up to the hip, under the bark skirt.
        tube('Thigh ' + s, [Vector((x, 0.0, KNEE_Z + 0.03)), Vector((x, 0.0, HIP_Z))], [0.088, 0.096], 'thigh.' + s,
             'bark-dark', count=9, occluder=True)


def build_torso():
    # The hips: a short barrel the legs hang from, and a skirt of bark plates round it.
    loft('Hips', [ring_points(0, 0.0, z, rx, ry, 14) for z, rx, ry in ((0.36, 0.17, 0.13), (0.47, 0.21, 0.17))],
         'pelvis', 'iron', cap_start=True, occluder=True)
    for k, theta in enumerate(range(-150, 180, 60)):
        a0, a1 = math.radians(theta - 27), math.radians(theta + 27)
        rows = []
        for z, out in ((0.5, 0.022), (0.42, 0.05), (0.33, 0.07)):
            rows.append([Vector(((0.215 + out) * math.sin(a), -(0.175 + out) * math.cos(a), z))
                         for a in (a0, (a0 + a1) / 2, a1)])
        plate('Skirt plate %d' % k, rows, 0.02, 'pelvis', 'bark',
              inward=lambda p: Vector((p.x, p.y, 0)).normalized())
    # The furnace barrel: cold iron.
    loft('Barrel', [ring_points(0, 0.0, z, rx, ry, 18) for z, rx, ry in BARREL], 'chest', 'iron',
         cap_start=True, cap_end=True, occluder=True, chamfer=0.007)
    # A bark mantle over the barrel's shoulders, round the sunk head.
    loft('Mantle', [ring_points(0, 0.01, z, rx, ry, 18, back=0.02) for z, rx, ry in
                    ((0.84, 0.3, 0.242), (0.93, 0.28, 0.226), (0.985, 0.2, 0.17), (1.0, 0.13, 0.11))], 'chest', 'bark',
         cap_end=True, occluder=True, chamfer=0.007)
    # Verdigris hoops round the barrel's top and waist.
    for z, name in ((0.84, 'Collar hoop'), (0.5, 'Waist hoop')):
        rx, ry = barrel_radius(z)
        pts = ring_points(0, 0.0, z, rx + 0.012, ry + 0.012, 18)
        tube(name, pts + pts[:2], [0.016] * 20, 'chest' if z > 0.6 else 'spine', 'verdigris', count=6,
             cap_start=False, cap_end=False, chamfer=0.0, twist_ref=Vector((0, 0, 1)))
    # Bark plates over the barrel's sides and back, leaving its face (the furnace) bare: long vertical slabs, slightly
    # gapped, a dark seam between each pair.
    for k, (theta, half, z0, z1) in enumerate([(52, 15, 0.52, 0.9), (82, 15, 0.5, 0.92), (112, 15, 0.52, 0.9),
                                               (145, 18, 0.53, 0.88), (-145, 18, 0.53, 0.88),
                                               (-112, 15, 0.52, 0.9), (-82, 15, 0.5, 0.92), (-52, 15, 0.52, 0.9)]):
        rows = []
        for z in (z1, (z0 + z1) / 2, z0):
            rows.append([barrel_point(math.radians(theta + u * half), z, 0.022 + 0.01 * (1 - abs(u)))
                         for u in (-1.0, -0.33, 0.33, 1.0)])
        plate('Bark plate %d' % k, rows, 0.024, 'chest', 'bark', inward=barrel_normal, occluder=True)
    # The furnace face: an iron plate round the mouth, the verdigris ring, the cavity's wall and its back.
    rows = []
    for v in (0.2, 0.1, 0.0, -0.1, -0.2):
        rows.append([barrel_point(math.radians(u), MOUTH.z + v, 0.008) for u in (-44, -22, 0, 22, 44)])
    plate('Furnace face', rows, 0.02, 'chest', 'iron', inward=barrel_normal, chamfer=0.004)
    ring = [MOUTH + Vector((MOUTH_R * math.cos(2 * math.pi * k / 20), -0.006, MOUTH_R * math.sin(2 * math.pi * k / 20)))
            for k in range(20)]
    tube('Mouth ring', ring + ring[:2], [0.024] * 22, 'chest', 'verdigris', count=7, cap_start=False, cap_end=False,
         chamfer=0.0, twist_ref=Vector((0, -1, 0)))
    for k in range(4):
        a = math.pi / 4 + k * math.pi / 2
        ellipsoid('Rivet %d' % k, MOUTH + Vector(((MOUTH_R + 0.045) * math.cos(a), -0.02, (MOUTH_R + 0.045) * math.sin(a))),
                  (0.014, 0.01, 0.014), 'chest', 'verdigris', rings=3, segments=6, contour=False, chamfer=0.0)
    # The furnace's light: a disc on the barrel's face inside the ring (the bloom grows out of it), and a strip right
    # behind the shutters' seam, so the closed seam shows from every angle the front is seen.
    disc = [MOUTH + Vector((MOUTH_R * 0.97 * math.cos(2 * math.pi * k / 16), -0.006,
                            MOUTH_R * 0.97 * math.sin(2 * math.pi * k / 16))) for k in range(16)]
    new_object('Furnace light', disc, [tuple(range(16))], 'chest', 'seam', contour=False, chamfer=0.0)
    rx, ry = barrel_radius(MOUTH.z)
    seam_y = MOUTH.y - SHUTTER_OFF + 0.012       # inside the shutters' thickness: seen only through the gap
    strip = [Vector((x, seam_y, MOUTH.z + v * (SHUTTER_R - 0.012))) for x, v in
             ((-0.03, -1), (0.03, -1), (0.03, 1), (-0.03, 1))]
    new_object('Seam light', strip, [(0, 1, 2, 3)], 'chest', 'seam', contour=False, chamfer=0.0,
               show=lambda p: p.shutters < 30)
    # The rear grate: the firebox door on its back, two thin bars over the furnace's light.
    gc = barrel_point(math.radians(180), 0.7, 0.0)
    door = [gc + Vector((u, 0.012, v)) for u, v in ((-0.11, -0.13), (0.11, -0.13), (0.11, 0.13), (-0.11, 0.13))]
    new_object('Grate light', door, [(0, 1, 2, 3)], 'chest', 'grate', contour=False, chamfer=0.0)
    for u in (-0.036, 0.036):
        box('Grate bar %+.2f' % u, gc + Vector((u, 0.024, 0)), (0.016, 0.016, 0.25), 'chest', 'iron', chamfer=0.0,
            contour=False)
    frame = [gc + Vector((0.112 * math.cos(2 * math.pi * k / 16), 0.024, 0.132 * math.sin(2 * math.pi * k / 16)))
             for k in range(16)]
    tube('Grate frame', frame + frame[:2], [0.016] * 18, 'chest', 'verdigris', count=6, cap_start=False, cap_end=False,
         chamfer=0.0, twist_ref=Vector((0, 1, 0)))
    # The cold chimney stack on the upper back, left of the spine, a dead twig grown out of it.
    base = Vector((0.11, 0.17, 0.8))
    top = Vector((0.13, 0.2, 1.1))
    tube('Stack', [base, base + (top - base) * 0.5, top], [0.062, 0.058, 0.056], 'chest', 'iron', count=10,
         cap_start=True, cap_end=True, occluder=True)
    lip = [top + Vector((0.07 * math.cos(2 * math.pi * k / 12), 0.07 * math.sin(2 * math.pi * k / 12), 0)) for k in range(12)]
    tube('Stack lip', lip + lip[:2], [0.016] * 14, 'chest', 'verdigris', count=6, cap_start=False, cap_end=False,
         chamfer=0.0, twist_ref=Vector((0, 0, 1)))
    branch('Stack twig', top + Vector((0, 0, -0.02)), [((0.35, 0.1, 1.0), 0.45), ((0.6, 0.2, 0.8), 0.35),
                                                       ((0.9, 0.1, 0.4), 0.2)], 0.24, 0.022, 'chest')
    branch('Stack twig fork', top + Vector((0.04, 0.02, 0.09)), [((-0.3, 0.1, 1.0), 0.6), ((-0.5, 0.2, 0.8), 0.4)],
           0.12, 0.013, 'chest')


def shutter_rows(side):
    """A shutter at rest: a bark D closing its half of the furnace face, its straight edge SHUTTER_GAP off the middle,
    standing proud of the barrel and following its curve. Rows top to bottom, columns from the middle outward."""
    rows = []
    for k in range(7):
        v = math.cos(math.pi * k / 6)          # 1 top .. -1 bottom
        z = MOUTH.z + v * SHUTTER_R
        reach = SHUTTER_R * math.sqrt(max(0.0, 1 - v * v)) ** 0.8
        row = []
        for u in (0.0, 0.3, 0.6, 0.85, 1.0):
            x = side * (SHUTTER_GAP + u * max(reach - SHUTTER_GAP, 0.004))
            rx, ry = barrel_radius(z)
            on_barrel = -ry * math.sqrt(max(0.0, 1 - (x / rx) ** 2))
            bulge = 0.012 * (1 - u * u) * (1 - v * v)
            front = lerp(min(on_barrel, MOUTH.y), on_barrel, u * u)
            row.append(Vector((x, front - SHUTTER_OFF - bulge, z)))
        rows.append(row)
    return rows


def build_shutters():
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        plate('Shutter ' + s, shutter_rows(side), 0.026, 'shutter.' + s, 'shutter',
              inward=lambda p: barrel_normal(p), chamfer=0.006, occluder=True)
        # Two verdigris hinge knuckles on its outer edge (nothing crosses the seam, so it reads as one orchid line).
        for dz in (0.1, -0.1):
            z = MOUTH.z + dz
            rx, ry = barrel_radius(z)
            x = side * (SHUTTER_R * 0.86)
            y = -ry * math.sqrt(max(0.0, 1 - (x / rx) ** 2)) - SHUTTER_OFF - 0.012
            box('Shutter hinge %s %+.1f' % (s, dz), Vector((x, y, z)), (0.05, 0.03, 0.034), 'shutter.' + s,
                'verdigris', chamfer=0.004, contour=False)


def build_head():
    hc = HEAD_BASE
    # A blocky bark head under a heavy brow, sunk forward between the shoulders.
    levels = [(0.0, 0.1, 0.095), (0.06, 0.112, 0.1), (0.13, 0.108, 0.098), (0.18, 0.088, 0.082), (0.205, 0.05, 0.05)]
    loft('Head', [ring_points(hc.x, hc.y, hc.z + h, rx, ry, 10, keel=0.012) for h, rx, ry in levels], 'head', 'bark',
         cap_start=True, cap_end=True, occluder=True, chamfer=0.007)
    box('Brow', hc + Vector((0, -0.088, 0.135)), (0.21, 0.05, 0.045), 'head', 'bark', chamfer=0.008)
    for side in (-1, 1):
        box('Eye %+d' % side, hc + Vector((0.045 * side, -0.1, 0.098)), (0.04, 0.012, 0.016), 'head', 'eyes',
            contour=False, chamfer=0.0)
    box('Jaw', hc + Vector((0, -0.07, 0.035)), (0.16, 0.06, 0.06), 'head', 'bark-dark', taper=1.1, chamfer=0.006)
    # Two dead branches out of the crown, forking: the silhouette's antlers.
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        start = hc + Vector((0.055 * side, 0.01, 0.17))
        branch('Branch ' + s, start, [((0.35 * side, 0.05, 1.0), 0.4), ((0.75 * side, 0.1, 0.7), 0.35),
                                      ((1.0 * side, 0.15, 0.25), 0.25)], 0.27, 0.026, 'head')
        fork = start + Vector((0.07 * side, 0.01, 0.1))
        branch('Branch fork ' + s, fork, [((0.05 * side, 0.0, 1.0), 0.6), ((-0.2 * side, 0.05, 0.9), 0.4)], 0.12, 0.014,
               'head')


def build_arms():
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
        # The shoulder: a big domed bark plate and a lower plate under it, riding part of the arm's swing.
        pc = sh + Vector((0.02 * side, 0.0, 0.04))
        ellipsoid('Pauldron ' + s, pc, (0.15, 0.14, 0.075), 'pauldron.' + s, 'bark', rings=4, segments=9,
                  lower=-0.35, upper=1.0, occluder=True, chamfer=0.008)
        ellipsoid('Pauldron lame ' + s, pc + Vector((0.025 * side, 0, -0.07)), (0.12, 0.12, 0.06), 'pauldron.' + s,
                  'bark-dark', rings=3, segments=10, lower=-0.4, upper=0.7)
        tube('Upper arm ' + s, [sh + (el - sh) * t for t in (0.1, 0.55, 1.0)], [0.068, 0.072, 0.07], 'upper_arm.' + s,
             'bark-dark', count=8)
        ellipsoid('Elbow ' + s, el, (0.07, 0.068, 0.068), 'forearm.' + s, 'bark', rings=4, segments=8)
        tube('Forearm ' + s, [el + (wr - el) * t for t in (0.1, 0.6, 1.0)], [0.072, 0.084, 0.08], 'forearm.' + s,
             'bark-dark', count=8)
        # A bark bracer on the forearm's outside.
        axis = (wr - el).normalized()
        out = Vector((side, -0.25, 0)).normalized()
        out = (out - axis * out.dot(axis)).normalized()
        across = axis.cross(out).normalized()
        rows = []
        for t in (0.15, 0.55, 0.92):
            c = el + (wr - el) * t
            r = 0.09 + 0.012 * t
            rows.append([c + (out * math.cos(a) + across * math.sin(a)) * r for a in (-1.0, -0.35, 0.35, 1.0)])
        plate('Bracer ' + s, rows, 0.02, 'forearm.' + s, 'bark',
              inward=lambda p, el=el, axis=axis: ((p - el) - axis * (p - el).dot(axis)).normalized())
        # The fist: a big knot of bark.
        ellipsoid('Fist ' + s, fi + Vector((0, 0, 0.005)), (0.078, 0.072, 0.082), 'hand.' + s, 'bark-dark',
                  rings=4, segments=8)


def build_maul():
    """The bark maul at rest along +z: a heartwood haft, a log head across it bound in two verdigris bands, its end grain
    a lighter heartwood."""
    tube('Haft', [Vector((0, 0, z)) for z in (0.0, 0.3, 0.6, HAFT)], [0.026, 0.024, 0.023, 0.024], 'maul', 'heartwood',
         count=7)
    hc = Vector((0, 0, MAUL_HEAD_Z))
    pts = [hc + Vector((x, 0, 0)) for x in (-MAUL_HEAD_HALF, -0.07, 0.0, 0.07, MAUL_HEAD_HALF)]
    radii = [MAUL_HEAD_R * 0.92, MAUL_HEAD_R, MAUL_HEAD_R * 1.04, MAUL_HEAD_R, MAUL_HEAD_R * 0.92]
    tube('Maul head', pts, radii, 'maul', 'bark', count=10, twist_ref=Vector((0, 0, 1)), occluder=True)
    for x in (-MAUL_HEAD_HALF, MAUL_HEAD_HALF):
        ring = [hc + Vector((x * 1.003, MAUL_HEAD_R * 0.86 * math.cos(2 * math.pi * k / 10),
                             MAUL_HEAD_R * 0.86 * math.sin(2 * math.pi * k / 10))) for k in range(10)]
        new_object('End grain %+.2f' % x, ring, [tuple(range(10)) if x > 0 else tuple(range(10))[::-1]], 'maul',
                   'heartwood', contour=False, chamfer=0.0)
    for x in (-0.095, 0.095):
        ring = [hc + Vector((x, (MAUL_HEAD_R + 0.008) * math.cos(2 * math.pi * k / 12),
                             (MAUL_HEAD_R + 0.008) * math.sin(2 * math.pi * k / 12))) for k in range(12)]
        tube('Maul band %+.2f' % x, ring + ring[:2], [0.014] * 14, 'maul', 'verdigris', count=6, cap_start=False,
             cap_end=False, chamfer=0.0, twist_ref=Vector((1, 0, 0)))
    box('Haft cap', Vector((0, 0, 0.0)), (0.05, 0.05, 0.035), 'maul', 'verdigris', chamfer=0.004)


# ---- The bloom and the roots: rebuilt for each pose from a formula, their topology fixed ----
OUTER_PETALS, INNER_PETALS = 5, 5
PETAL_ROWS, PETAL_COLS = 6, 5
ROOTS_A_FOOT = 4
ROOT_RINGS = 7


def petal_grid(index, count, inner, t, wilt):
    """One petal's rows of points (base to tip) and tones, in the chest's rest space. t opens it (0 a bud deep in the
    furnace, 1 wide open), wilt droops it and furls it (the death)."""
    a_axis = BLOOM_AXIS
    u1 = Vector((1, 0, 0))
    u2 = a_axis.cross(u1).normalized()
    phase = (math.pi / count) if inner else 0.0
    alpha = 2 * math.pi * index / count + phase + math.radians(90)
    radial = (u1 * math.cos(alpha) + u2 * math.sin(alpha)).normalized()
    tangent = a_axis.cross(radial).normalized()
    length = lerp(0.07, 0.36, t) * (0.68 if inner else 1.0) * lerp(1.0, 0.85, wilt)
    width = lerp(0.035, 0.17, t) * (0.72 if inner else 1.0)
    spread = math.radians(lerp(6, 80 if not inner else 50, t ** 0.8))
    curl = math.radians(lerp(0, 28 if not inner else 12, t))
    droop = math.radians(55 * wilt)
    centre = MOUTH + Vector((0, CAVITY_DEPTH * lerp(0.75, -0.2, t), 0)) + a_axis * (0.02 if inner else 0.0)
    down = Vector((0, 0, -1))
    rows, tones = [], []
    p = centre + radial * (0.012 if inner else 0.02)
    prev = p
    for k in range(PETAL_ROWS):
        s = k / (PETAL_ROWS - 1)
        angle = spread * (0.45 + 0.55 * s) + curl * s * s
        direction = (a_axis * math.cos(angle) + radial * math.sin(angle)).normalized()
        if droop > 0:
            direction = (direction * math.cos(droop * s) + down * math.sin(droop * s)).normalized()
        if k > 0:
            p = prev + direction * (length / (PETAL_ROWS - 1))
        prev = p
        normal = direction.cross(tangent).normalized()
        w = width * (0.25 + 0.75 * math.sin(math.pi * min(1.0, 0.1 + s * 0.85)) ** 0.5) * (1 - 0.35 * wilt * s)
        if k == PETAL_ROWS - 1:
            w *= 0.25
        cup = 0.32 * width * (1 - 0.6 * s) * lerp(1.0, 1.6, wilt)
        row, row_tone = [], []
        for c in range(PETAL_COLS):
            u = -1 + 2 * c / (PETAL_COLS - 1)
            row.append(p + tangent * (u * w) + normal * (cup * (1 - u * u) * -1.0))
            row_tone.append(min(1.0, lerp(0.3, 1.0, s ** 0.8) + (0.1 if inner else 0.0) - 0.28 * u * u))
        rows.append(row)
        tones.append(row_tone)
    return rows, tones


def bloom_mesh_data(t, wilt):
    verts, tones, glows = [], [], []
    for inner, count in ((False, OUTER_PETALS), (True, INNER_PETALS)):
        for i in range(count):
            rows, row_tones = petal_grid(i, count, inner, t, wilt)
            for k, (row, rt) in enumerate(zip(rows, row_tones)):
                s = k / (PETAL_ROWS - 1)
                verts.extend(row)
                tones.extend(rt)
                glows.extend([0.9 - 0.55 * s] * len(row))
    return verts, tones, glows


def bloom_faces():
    faces = []
    per = PETAL_ROWS * PETAL_COLS
    for petal in range(OUTER_PETALS + INNER_PETALS):
        base = petal * per
        for k in range(PETAL_ROWS - 1):
            for c in range(PETAL_COLS - 1):
                a = base + k * PETAL_COLS + c
                faces.append((a, a + 1, a + PETAL_COLS + 1, a + PETAL_COLS))
    return faces


def heart_mesh(t, wilt):
    """A squashed sphere (rings of 8) facing the axis."""
    centre = MOUTH + Vector((0, CAVITY_DEPTH * lerp(0.6, -0.4, t), 0)) + BLOOM_AXIS * 0.025 + Vector((0, 0, -0.025 * wilt))
    r = lerp(0.02, 0.046, t) * lerp(1.0, 0.7, wilt)
    u1 = Vector((1, 0, 0))
    u2 = BLOOM_AXIS.cross(u1).normalized()
    verts = []
    for j in range(5):
        lat = -math.pi / 2 + math.pi * j / 4
        for i in range(8):
            lon = 2 * math.pi * i / 8
            verts.append(centre + (u1 * math.cos(lon) + u2 * math.sin(lon)) * (r * math.cos(lat) + 1e-4)
                         + BLOOM_AXIS * (0.75 * r * math.sin(lat)))
    return verts


def heart_faces():
    faces = []
    for j in range(4):
        for i in range(8):
            i1 = (i + 1) % 8
            faces.append((j * 8 + i, j * 8 + i1, (j + 1) * 8 + i1, (j + 1) * 8 + i))
    return faces


def root_paths(feet, growth):
    """The roots run from each planted foot into the loam: ROOTS_A_FOOT tapering paths a foot, fanned outward."""
    paths = []
    for s, foot in feet:
        side = 1 if s == 'L' else -1
        for k in range(ROOTS_A_FOOT):
            # Fanned from forward-and-in round to back-and-out, on the foot's own side.
            a = math.radians((-20, 25, 70, 115)[k])
            heading = Vector((side * math.sin(a), -math.cos(a), 0)).normalized()
            length = (0.2, 0.3, 0.26, 0.18)[k] * growth + 0.005
            start = Vector((foot.x, foot.y - 0.01, 0.035)) + heading * 0.08
            pts = []
            for j in range(ROOT_RINGS):
                q = j / (ROOT_RINGS - 1)
                wobble = 0.03 * math.sin(3.1 * q + k * 1.7) * q
                side_vec = Vector((-heading.y, heading.x, 0))
                pts.append(start + heading * (length * q) + side_vec * wobble + Vector((0, 0, -0.03 * q + 0.012 * math.sin(math.pi * q))))
            paths.append(pts)
    return paths


def root_radii():
    return [0.028 * (1 - 0.85 * j / (ROOT_RINGS - 1)) + 0.003 for j in range(ROOT_RINGS)]


ROOT_FEET_REST = [('L', Vector((0.2, -0.02, ANKLE_Z))), ('R', Vector((-0.2, -0.02, ANKLE_Z)))]


def build_bloom_and_roots():
    verts, tones, glows = bloom_mesh_data(1.0, 0.0)
    obj = new_object('Bloom petals', verts, bloom_faces(), 'chest', 'bloom', contour=False, chamfer=0.0,
                     show=lambda p: p.bloom_t > 0.0)
    for name, values in (('tone', tones), ('glow', glows)):
        attr = obj.data.attributes.new(name, 'FLOAT', 'POINT')
        attr.data.foreach_set('value', values)
    heart = new_object('Bloom heart', heart_mesh(1.0, 0.0), heart_faces(), 'chest', 'bloom', contour=False,
                       chamfer=0.0, flat=False, show=lambda p: p.bloom_t > 0.0)
    n = len(heart.data.vertices)
    for name, value in (('tone', 1.0), ('glow', 1.0)):
        attr = heart.data.attributes.new(name, 'FLOAT', 'POINT')
        attr.data.foreach_set('value', [value] * n)
    for k, path in enumerate(root_paths(ROOT_FEET_REST, 1.0)):
        tube('Root %d' % k, path, root_radii(), 'root', 'bark-dark', count=5, chamfer=0.0,
             show=lambda p: p.roots > 0.0)


def rebuild(obj, verts):
    mesh = obj.data
    flat = [c for v in verts for c in v]
    mesh.vertices.foreach_set('co', flat)
    mesh.update()


def shape_bloom_and_roots(pose):
    if pose.bloom_t > 0:
        verts, _, _ = bloom_mesh_data(pose.bloom_t, pose.wilt)
        rebuild(PARTS['Bloom petals'], verts)
        rebuild(PARTS['Bloom heart'], heart_mesh(pose.bloom_t, pose.wilt))
    if pose.roots > 0:
        feet = [(s, pose.feet[s][0]) for s in ('L', 'R')]
        for k, path in enumerate(root_paths(feet, pose.roots)):
            rings = tube_rings(path, root_radii(), 5)
            pts = [p for ring in rings for p in ring]
            pts.append(sum(rings[0], Vector()) / 5)
            pts.append(sum(rings[-1], Vector()) / 5)
            rebuild(PARTS['Root %d' % k], pts)


build_legs()
build_torso()
build_shutters()
build_head()
build_arms()
build_maul()
build_bloom_and_roots()


# ---------------------------------------------------------------------------------------------------------------------
# Rig: bark and iron ride bones rigidly (BuildBlenderMirrorSentry.py's)
# ---------------------------------------------------------------------------------------------------------------------
arm_data = bpy.data.armatures.new('Cinder Bloom rig')
RIG = bpy.data.objects.new('Cinder Bloom', arm_data)
collection.objects.link(RIG)
bpy.context.view_layer.objects.active = RIG
RIG.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
eb = arm_data.edit_bones


def bone(name, head, tail, parent=None):
    b = eb.new(name)
    b.head = Vector(head)
    b.tail = Vector(tail)
    if parent:
        b.parent = eb[parent]
    b.use_connect = False
    return b


bone('root', (0, 0, 0), (0, -0.1, 0))
bone('pelvis', (0, 0, HIP_Z), (0, 0, WAIST_Z), 'root')
bone('spine', (0, 0, WAIST_Z), (0, 0, CHEST_Z), 'pelvis')
bone('chest', (0, 0, CHEST_Z), (0, 0, NECK_Z), 'spine')
bone('neck', (0, 0, NECK_Z), tuple(HEAD_BASE), 'chest')
bone('head', tuple(HEAD_BASE), (HEAD_BASE.x, HEAD_BASE.y, HEAD_TOP), 'neck')
for side, s in ((1, 'L'), (-1, 'R')):
    sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
    rx, ry = barrel_radius(MOUTH.z)
    hinge = Vector((side * (SHUTTER_R + 0.005), -ry * math.sqrt(1 - (SHUTTER_R / rx) ** 2) - SHUTTER_OFF, MOUTH.z))
    bone('shutter.' + s, hinge, hinge + Vector((0, 0, 0.1)), 'chest')
    bone('pauldron.' + s, sh, sh + Vector((0.1 * side, 0, 0.05)), 'chest')
    bone('upper_arm.' + s, sh, el, 'chest')
    bone('forearm.' + s, el, wr, 'upper_arm.' + s)
    bone('hand.' + s, wr, fi + (fi - wr).normalized() * 0.05, 'forearm.' + s)
    bone('thigh.' + s, (HIP_X * side, 0, HIP_Z), (HIP_X * side, 0, KNEE_Z), 'pelvis')
    bone('shin.' + s, (HIP_X * side, 0, KNEE_Z), (HIP_X * side, 0.0, ANKLE_Z), 'thigh.' + s)
    bone('foot.' + s, (HIP_X * side, 0.0, ANKLE_Z), (HIP_X * side, -0.12, 0.02), 'shin.' + s)
bone('maul', (0, 0, 0), (0, 0, 0.3), 'root')
bpy.ops.object.mode_set(mode='OBJECT')
REST = {b.name: b.matrix_local.copy() for b in arm_data.bones}
PARENT = {b.name: (b.parent.name if b.parent else None) for b in arm_data.bones}
FOLLOWERS = {'pauldron.L': ('upper_arm.L', 0.25), 'pauldron.R': ('upper_arm.R', 0.25)}


def depth(name):
    d = 0
    while PARENT[name]:
        name = PARENT[name]
        d += 1
    return d


# Parents before children, a follower after the bone it follows, and the maul straight after the root.
ORDER = sorted(REST, key=lambda b: (depth(b) + (1 if b in FOLLOWERS else 0) - (0.5 if b == 'maul' else 0), b))
for pb in RIG.pose.bones:
    pb.rotation_mode = 'QUATERNION'


def bind(obj, bone_name):
    group = obj.vertex_groups.new(name=bone_name)
    group.add(list(range(len(obj.data.vertices))), 1.0, 'REPLACE')
    obj.parent = RIG
    mod = obj.modifiers.new('Rig', 'ARMATURE')
    mod.object = RIG
    mod.use_deform_preserve_volume = False


for name, obj in PARTS.items():
    bind(obj, INFO[name]['bone'])

CONTOUR = 0.0085


def finish_modifiers(obj):
    info = INFO[obj.name]
    if info['chamfer'] > 0:
        bev = obj.modifiers.new('Chamfer', 'BEVEL')
        bev.width = info['chamfer']
        bev.segments = 1
        bev.limit_method = 'ANGLE'
        bev.angle_limit = math.radians(28)
        bev.harden_normals = False
    if info['contour']:
        obj.data.materials.append(MATERIALS['outline'])
        hull = obj.modifiers.new('Contour', 'SOLIDIFY')
        hull.thickness = CONTOUR
        hull.offset = 1.0
        hull.use_flip_normals = True
        hull.use_rim = False
        hull.use_even_offset = False
        hull.use_quality_normals = False
        hull.material_offset = 1


def facet_attribute(obj, seed):
    mesh = obj.data
    attr = mesh.attributes.new('facet', 'FLOAT', 'FACE')
    for i in range(len(mesh.polygons)):
        h = math.sin((i + 1) * 12.9898 + seed * 78.233) * 43758.5453
        attr.data[i].value = h - math.floor(h)


for k, obj in enumerate(PARTS.values()):
    facet_attribute(obj, k)


# ---------------------------------------------------------------------------------------------------------------------
# Posing (BuildBlenderMirrorSentry.py's solver): deltas about each bone's head in rest space, accumulated down the
# hierarchy; the maul is placed outright and the fists grip it; legs and arms by two-bone IK.
# ---------------------------------------------------------------------------------------------------------------------
def rot(x=0.0, y=0.0, z=0.0):
    """Degrees about the rig's axes: +x pitches a raised bone forward (toward -Y), +y leans it to the Bloom's left,
    +z turns it to the left."""
    return (Matrix.Rotation(math.radians(z), 3, 'Z') @ Matrix.Rotation(math.radians(y), 3, 'Y')
            @ Matrix.Rotation(math.radians(x), 3, 'X'))


class Pose:
    def __init__(self):
        self.rot = {}
        self.offset = Vector()
        self.feet = {}            # 'L'/'R' -> (ankle target, foot pitch degrees, foot yaw degrees)
        self.hands = {}           # 'L'/'R' -> (fist target, elbow pole)
        self.free = {}            # free bone -> 4x4 placement
        self.knee_poles = {}
        self.life = 1.0           # the eyes' light
        self.bloom = 1.0          # the bloom's value, for the packer (the dying dim)
        self.bloom_t = 0.0        # 0 closed (no bloom drawn), up to 1 wide open
        self.wilt = 0.0
        self.shutters = 0.0       # degrees each shutter swings open
        self.roots = 0.0          # 0 none, 1 grown
        self.seam = (0.8, 0.45)   # the front furnace light's tone and glow (closed: a thin orchid seam)
        self.grate = (0.34, 0.0)  # the rear grate's (closed: a dim ember of orchid)
        self.floor = False        # settle the lowest point onto the floor (the death's drawings)
        self.state = 'closed'

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


def maul_placement(grip, toward, roll=0.0):
    """The maul's G: its right-hand grip on grip, its haft pointing along toward (butt to head), rolled about the haft
    (degrees; 0 keeps the log's axis as level as the haft allows)."""
    d = Vector(toward).normalized()
    across = Vector((0, 0, 1)).cross(d) if abs(d.z) < 0.97 else Vector((1, 0, 0))
    across = (across - d * across.dot(d)).normalized()
    across = (Matrix.Rotation(math.radians(roll), 3, d) @ across).normalized()
    third = d.cross(across).normalized()
    r = Matrix((across, third, d)).transposed()      # local x -> across, local y -> third, local z -> d
    return Matrix.Translation(Vector(grip)) @ r.to_4x4() @ Matrix.Translation(-GRIP_R)


def hold_maul(p, grip, toward, roll=0.0, pole_r=(-0.6, 0.6, -0.4), left=None, pole_l=(0.6, 0.6, -0.4)):
    """Places the maul and puts the right fist on its grip; left is None (one hand), 'grip' (the left fist on the
    haft's middle) or a fist target of its own."""
    g = maul_placement(grip, toward, roll)
    p.free['maul'] = g
    p.hands['R'] = (g @ GRIP_R, Vector(pole_r))
    if left == 'grip':
        p.hands['L'] = (g @ GRIP_L, Vector(pole_l))
    return g


def drop_maul(p, head_at, toward, roll=0.0):
    """The maul let go: lying where its head rests (head_at the log's centre), the haft along toward from the head to the
    butt."""
    d = -Vector(toward).normalized()
    butt_to_head = d
    grip = Vector(head_at) - butt_to_head * (MAUL_HEAD_Z - GRIP_R.z)
    p.free['maul'] = maul_placement(grip, butt_to_head, roll)


def grip_toward(head, hand):
    """The right fist's grip and the haft's direction for a maul whose head rests at head, its haft leaning toward
    hand: the grip lands on the line from the head toward hand, the haft's own length from it."""
    d = (Vector(hand) - Vector(head)).normalized()
    grip = Vector(head) + d * (MAUL_HEAD_Z - GRIP_R.z)
    return grip, Vector(head) - grip


class Solver:
    def __init__(self, pose):
        self.pose = pose
        self.G = {}
        self.W = {}

    def place(self, b, W_total=None, G_total=None):
        parent = PARENT[b]
        Gp = self.G[parent] if parent else Matrix.Identity(4)
        Wp = self.W[parent] if parent else Matrix.Identity(3)
        if G_total is not None:
            D = Gp.inverted() @ G_total
            self.G[b] = G_total
            self.W[b] = G_total.to_3x3()
            return D
        h = REST[b].to_translation()
        if W_total is None:
            R = self.pose.rot.get(b, Matrix.Identity(3))
            W_total = Wp @ R
        else:
            R = Wp.inverted() @ W_total
        D = Matrix.Translation(h) @ R.to_4x4() @ Matrix.Translation(-h)
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
            if b in FOLLOWERS:
                leader, share = FOLLOWERS[b]
                base = self.W[PARENT[b]].to_quaternion()
                pending[b] = base.slerp(self.W[leader].to_quaternion(), share).to_matrix()
            D = self.place(b, pending.get(b), self.pose.free.get(b))
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
        pole = self.pose.knee_poles.get(s, yawm @ Vector((0, -1, 0.15)))
        knee, ankle, hinge = two_bone(hip, Vector(target), l1, l2, pole)
        rest_pole = Vector((0, -1, 0.15))
        _, _, rest_hinge = two_bone(REST['thigh.' + s].to_translation(), ankle0, l1, l2, rest_pole)
        W_thigh = frame_from(knee - hip, hinge) @ frame_from(knee0 - REST['thigh.' + s].to_translation(), rest_hinge).transposed()
        W_shin = frame_from(ankle - knee, hinge) @ frame_from(ankle0 - knee0, rest_hinge).transposed()
        W_foot = yawm @ rot(x=pitch)
        return {'thigh.' + s: W_thigh, 'shin.' + s: W_shin, 'foot.' + s: W_foot}

    def arm(self, s):
        target, pole = self.pose.hands[s]
        shoulder = self.G['chest'] @ REST['upper_arm.' + s].to_translation()
        el0 = REST['forearm.' + s].to_translation()
        wr0 = REST['hand.' + s].to_translation()
        sh0 = REST['upper_arm.' + s].to_translation()
        fist0 = side_point(FIST, 1 if s == 'L' else -1)
        l1 = (el0 - sh0).length
        l2 = (wr0 - el0).length
        reach = (Vector(target) - shoulder).normalized()
        wrist_target = Vector(target) - reach * (fist0 - wr0).length
        elbow, wrist, hinge = two_bone(shoulder, wrist_target, l1, l2, Vector(pole))
        rest_pole = Vector((0.3 if s == 'L' else -0.3, 1, -0.1))
        _, _, rest_hinge = two_bone(sh0, wr0, l1, l2, rest_pole)
        W_upper = frame_from(elbow - shoulder, hinge) @ frame_from(el0 - sh0, rest_hinge).transposed()
        W_fore = frame_from(wrist - elbow, hinge) @ frame_from(wr0 - el0, rest_hinge).transposed()
        return {'upper_arm.' + s: W_upper, 'forearm.' + s: W_fore, 'hand.' + s: W_fore}


def apply(pose):
    pose.add('shutter.L', rot(z=pose.shutters))
    pose.add('shutter.R', rot(z=-pose.shutters))
    solver = Solver(pose)
    basis = solver.solve()
    for b, m in basis.items():
        RIG.pose.bones[b].matrix_basis = m
    set_state(life=pose.life, seam_tone=pose.seam[0], seam_glow=pose.seam[1], grate_tone=pose.grate[0],
              grate_glow=pose.grate[1])
    shape_bloom_and_roots(pose)
    for name, obj in PARTS.items():
        show = INFO[name]['show']
        visible = True if show is None else bool(show(pose))
        obj.hide_render = not visible
        obj.hide_viewport = not visible
    return solver


# ---------------------------------------------------------------------------------------------------------------------
# The poses
# ---------------------------------------------------------------------------------------------------------------------
SHOULDER_CARRY = dict(grip=Vector((-0.2, -0.2, 0.84)), toward=Vector((-0.62, 0.66, 0.24)), roll=12)


def stance():
    """Closed: hunched and heavy, the head forward, the maul on its right shoulder (the head behind it), the left fist
    hanging by its thigh."""
    p = Pose()
    p.offset = Vector((0, 0, -0.015))
    p.add('pelvis', rot(x=2))
    p.add('spine', rot(x=4))
    p.add('chest', rot(x=4))
    p.add('neck', rot(x=6))
    p.add('head', rot(x=-6))
    p.feet['L'] = (Vector((0.15, -0.03, ANKLE_Z)), 0, 12)
    p.feet['R'] = (Vector((-0.15, 0.04, ANKLE_Z)), 0, -12)
    hold_maul(p, SHOULDER_CARRY['grip'], SHOULDER_CARRY['toward'], SHOULDER_CARRY['roll'], pole_r=(-0.8, 0.3, -0.5))
    p.hands['L'] = (Vector((0.4, -0.05, 0.36)), Vector((0.6, 0.8, -0.2)))
    return p


def idle():
    return stance()


def walk(phase):
    """A heavy golem's walk: distance-driven, two short steps a cycle, the right foot's contact at 0, the body rolling
    over each planted foot; the maul rides the shoulder, the left fist swings."""
    p = stance()
    bob = -0.02 * (0.5 + 0.5 * math.cos(4 * math.pi * phase))
    roll = math.sin(2 * math.pi * phase)
    p.offset = Vector((0.018 * roll, 0, bob - 0.012))
    excursion = STRIDE / 4
    duty = 0.55
    for s, offset, x in (('R', 0.0, -0.15), ('L', 0.5, 0.155)):
        q = (phase + offset) % 1.0
        if q < duty:
            t = q / duty
            y = lerp(-excursion, excursion, t)
            z = ANKLE_Z
            pitch = lerp(-3, 6, smooth(0.6, 1.0, t))
        else:
            t = (q - duty) / (1 - duty)
            y = lerp(excursion, -excursion, smooth(0.0, 1.0, t))
            z = ANKLE_Z + 0.07 * math.sin(math.pi * t) ** 1.2
            pitch = lerp(12, -6, t)
        p.feet[s] = (Vector((x, y, z)), pitch, 10 if s == 'L' else -10)
    p.add('pelvis', rot(y=-4 * roll, z=6 * roll))
    p.add('spine', rot(x=2, z=-3 * roll))
    p.add('chest', rot(x=2, y=2 * roll, z=-4 * roll))
    p.add('head', rot(z=-2 * roll))
    lift = 0.01 * math.cos(4 * math.pi * phase)
    hold_maul(p, SHOULDER_CARRY['grip'] + Vector((0.01 * roll, 0.02 * roll, lift)), SHOULDER_CARRY['toward'],
              SHOULDER_CARRY['roll'], pole_r=(-0.8, 0.3, -0.5))
    p.hands['L'] = (Vector((0.4, -0.06 + 0.14 * roll, 0.37 + 0.03 * abs(roll))), Vector((0.6, 0.8, -0.2)))
    return p


def windup():
    """The maul's beat: both fists on the haft, the maul heaved up behind the right shoulder, the body coiled back
    and the knees bent - the log head high over its back."""
    p = Pose()
    p.offset = Vector((0, 0.03, -0.05))
    p.add('pelvis', rot(x=-3, z=-10))
    p.add('spine', rot(x=-6, z=-8))
    p.add('chest', rot(x=-8, z=-12))
    p.add('neck', rot(x=8, z=10))
    p.add('head', rot(x=-4, z=10))
    p.feet['L'] = (Vector((0.17, -0.1, ANKLE_Z)), 0, 14)
    p.feet['R'] = (Vector((-0.16, 0.1, ANKLE_Z)), 0, -16)
    hold_maul(p, Vector((-0.14, -0.06, 0.98)), Vector((-0.5, 0.75, 0.42)), roll=8, pole_r=(-0.9, 0.0, -0.4),
              left='grip', pole_l=(0.7, -0.2, -0.6))
    return p


def strike():
    """The blow: a lunge onto the left foot, the maul brought down in front, its head on the floor, both fists on the
    haft, the body bent over it."""
    p = Pose()
    p.offset = Vector((0, -0.07, -0.07))
    p.add('pelvis', rot(x=8, z=8))
    p.add('spine', rot(x=12, z=6))
    p.add('chest', rot(x=10, z=8))
    p.add('neck', rot(x=-6, z=-6))
    p.add('head', rot(x=-8, z=-4))
    p.feet['L'] = (Vector((0.17, -0.22, ANKLE_Z)), 0, 8)
    p.feet['R'] = (Vector((-0.15, 0.18, ANKLE_Z + 0.01)), 12, -16)
    grip, toward = grip_toward(Vector((-0.02, -0.66, MAUL_HEAD_R + 0.004)), Vector((-0.12, -0.12, 0.62)))
    hold_maul(p, grip, toward, roll=0, pole_r=(-0.9, 0.3, -0.2), left='grip', pole_l=(0.9, 0.2, -0.3))
    return p


# ---- The open state (docs/52 section 5): rooted, the maul's head on the floor, the bloom out of the furnace ----
OPEN_FEET = {'L': (Vector((0.2, -0.02, ANKLE_Z)), 0, 22), 'R': (Vector((-0.2, 0.02, ANKLE_Z)), 0, -22)}


def rooted(lift):
    """The open body: feet planted wide, the chest lifted and turned up to bare the furnace (lift 0..1), the left arm
    drawn out of the bloom's way, the maul's head grounded at its right side, its haft leaning in the right fist."""
    p = Pose()
    p.offset = Vector((0, 0.01 * lift, -0.035 * lift - 0.015))
    p.add('pelvis', rot(x=lerp(2, -3, lift)))
    p.add('spine', rot(x=lerp(4, -6, lift)))
    p.add('chest', rot(x=lerp(4, -10, lift)))
    p.add('neck', rot(x=lerp(6, 10, lift)))
    p.add('head', rot(x=lerp(-6, -14, lift)))
    p.feet['L'], p.feet['R'] = OPEN_FEET['L'], OPEN_FEET['R']
    p.knee_poles = {'L': Vector((0.5, -1, 0.1)), 'R': Vector((-0.5, -1, 0.1))}
    grip, toward = grip_toward(Vector((-0.64, -0.12, MAUL_HEAD_R + 0.004)), Vector((-0.43, 0.02, 0.76)))
    hold_maul(p, grip, toward, roll=90, pole_r=(-0.8, 0.5, -0.2))
    p.hands['L'] = (Vector((lerp(0.42, 0.47, lift), lerp(-0.04, 0.06, lift), lerp(0.37, 0.42, lift))),
                    Vector((0.8, 0.6, -0.2)))
    return p


def open_pose(name):
    if name == 'open':
        p = rooted(1.0)
        p.bloom_t, p.shutters, p.roots = 1.0, 140.0, 1.0
        p.seam, p.grate = (0.9, 1.0), (0.92, 1.0)
    elif name == 'unfold-1':
        p = rooted(0.7)
        p.bloom_t, p.shutters, p.roots = 0.6, 100.0, 0.7
        p.seam, p.grate = (0.85, 0.75), (0.75, 0.6)
    else:  # unfold-0
        # The first beat: the maul coming off the shoulder, the feet planting, the shutters cracking on a bud.
        p = stance()
        p.feet['L'], p.feet['R'] = OPEN_FEET['L'], OPEN_FEET['R']
        p.offset = Vector((0, 0, -0.03))
        grip, toward = grip_toward(Vector((-0.6, -0.1, 0.4)), Vector((-0.3, -0.12, 0.84)))
        hold_maul(p, grip, toward, roll=60, pole_r=(-0.8, 0.5, -0.3))
        p.hands['L'] = (Vector((0.41, -0.04, 0.38)), Vector((0.6, 0.8, -0.2)))
        p.bloom_t, p.shutters, p.roots = 0.25, 34.0, 0.3
        p.seam, p.grate = (0.8, 0.5), (0.55, 0.25)
    p.state = 'open'
    return p


def dying(k):
    """Its own death, seven drawings: struck (the shutters burst and the bloom spills out), buckling as the maul drops,
    down on its knees, slumping, toppling forward, down, settled - the bloom wilting and dimming as it goes, the eyes
    out at the end."""
    p = Pose()
    p.floor = k >= 2
    wilt = (0.1, 0.35, 0.55, 0.7, 0.82, 0.92, 1.0)[k]
    p.bloom_t, p.wilt = (0.55, 0.7, 0.75, 0.75, 0.72, 0.7, 0.7)[k], wilt
    p.shutters = (70, 95, 105, 110, 110, 110, 110)[k]
    p.bloom = (1.15, 0.85, 0.65, 0.5, 0.38, 0.28, 0.22)[k]
    p.seam = ((0.8, 0.7), (0.6, 0.4), (0.5, 0.25), (0.42, 0.15), (0.36, 0.08), (0.32, 0.04), (0.3, 0.0))[k]
    p.grate = p.seam
    if k == 0:
        p.offset = Vector((0, 0.05, -0.02))
        p.add('pelvis', rot(x=-5))
        p.add('spine', rot(x=-9, z=6))
        p.add('chest', rot(x=-12, z=8))
        p.add('neck', rot(x=-6))
        p.add('head', rot(x=-18, z=-6))
        p.feet['L'] = (Vector((0.16, -0.03, ANKLE_Z)), 0, 12)
        p.feet['R'] = (Vector((-0.15, 0.08, ANKLE_Z)), 0, -14)
        grip, toward = grip_toward(Vector((-0.6, 0.3, 0.42)), Vector((-0.34, -0.02, 0.62)))
        hold_maul(p, grip, toward, roll=30, pole_r=(-0.6, 0.6, -0.5))
        p.hands['L'] = (Vector((0.44, 0.06, 0.6)), Vector((0.6, 0.4, -0.7)))
        p.life = 1.0
    elif k == 1:
        p.offset = Vector((0, 0.02, -0.1))
        p.add('pelvis', rot(x=8))
        p.add('spine', rot(x=10, z=4))
        p.add('chest', rot(x=10, z=4))
        p.add('neck', rot(x=10))
        p.add('head', rot(x=14))
        p.feet['L'] = (Vector((0.16, -0.05, ANKLE_Z)), 0, 12)
        p.feet['R'] = (Vector((-0.15, 0.08, ANKLE_Z)), 0, -14)
        p.knee_poles = {'L': Vector((0.3, -1, 0)), 'R': Vector((-0.3, -1, 0))}
        drop_maul(p, Vector((-0.5, 0.05, 0.16)), Vector((0.05, -0.6, -0.08)), roll=0)
        p.hands['R'] = (Vector((-0.38, -0.06, 0.34)), Vector((-0.6, 0.8, -0.2)))
        p.hands['L'] = (Vector((0.4, -0.06, 0.34)), Vector((0.6, 0.8, -0.2)))
        p.life = 0.8
    elif k == 2:
        p.offset = Vector((0, 0.03, -0.2))
        p.add('pelvis', rot(x=10))
        p.add('spine', rot(x=14))
        p.add('chest', rot(x=12))
        p.add('neck', rot(x=12))
        p.add('head', rot(x=20))
        p.feet['L'] = (Vector((0.16, -0.16, ANKLE_Z)), 0, 10)
        p.feet['R'] = (Vector((-0.14, 0.17, 0.07)), 70, -6)
        p.knee_poles = {'L': Vector((0.2, -1, 0.4)), 'R': Vector((0, -1, -0.6))}
        drop_maul(p, Vector((-0.52, 0.0, MAUL_HEAD_R)), Vector((0.1, -0.7, -0.08)), roll=0)
        p.hands['R'] = (Vector((-0.36, -0.12, 0.2)), Vector((-0.6, 0.6, -0.2)))
        p.hands['L'] = (Vector((0.38, -0.16, 0.24)), Vector((0.6, 0.6, -0.2)))
        p.life = 0.6
    elif k == 3:
        p.offset = Vector((0, 0.05, -0.24))
        p.add('pelvis', rot(x=20))
        p.add('spine', rot(x=20, y=-4))
        p.add('chest', rot(x=18, y=-4))
        p.add('neck', rot(x=12))
        p.add('head', rot(x=22, y=-8))
        p.feet['L'] = (Vector((0.15, 0.18, 0.07)), 70, 6)
        p.feet['R'] = (Vector((-0.14, 0.18, 0.07)), 70, -6)
        p.knee_poles = {'L': Vector((0, -1, -0.6)), 'R': Vector((0, -1, -0.6))}
        drop_maul(p, Vector((-0.52, 0.0, MAUL_HEAD_R)), Vector((0.1, -0.7, -0.08)), roll=0)
        p.hands['R'] = (Vector((-0.34, -0.24, 0.1)), Vector((-0.6, 0.6, -0.2)))
        p.hands['L'] = (Vector((0.36, -0.28, 0.12)), Vector((0.6, 0.6, -0.2)))
        p.life = 0.4
    else:
        settle = k - 4             # 0 toppling, 1 down, 2 settled
        p.offset = Vector((0, 0.06 + 0.02 * settle, -0.27 - 0.02 * settle))
        p.add('pelvis', rot(x=28 + 8 * settle))
        p.add('spine', rot(x=26 + 4 * settle, y=-4))
        p.add('chest', rot(x=22 + 4 * settle, y=-4))
        p.add('neck', rot(x=12 - 4 * settle))
        p.add('head', rot(x=10 - 6 * settle, y=-10, z=-8))
        p.feet['L'] = (Vector((0.15, 0.2, 0.07)), 76, 6)
        p.feet['R'] = (Vector((-0.14, 0.2, 0.07)), 76, -6)
        p.knee_poles = {'L': Vector((0, -1, -0.6)), 'R': Vector((0, -1, -0.6))}
        drop_maul(p, Vector((-0.52, 0.0, MAUL_HEAD_R)), Vector((0.1, -0.7, -0.08)), roll=0)
        reach = 0.36 + 0.05 * settle
        p.hands['R'] = (Vector((-0.34, -reach, 0.08 - 0.02 * settle)), Vector((-0.7, 0.4, 0.3)))
        p.hands['L'] = (Vector((0.36, -reach + 0.08, 0.08 - 0.02 * settle)), Vector((0.8, 0.3, 0.3)))
        p.life = (0.25, 0.1, 0.0)[settle]
    return p


def pose_for(name):
    if name == 'idle':
        return idle()
    if name in ('step-a', 'pass-a', 'step-b', 'pass-b'):
        return walk({'step-a': 0.0, 'pass-a': 0.25, 'step-b': 0.5, 'pass-b': 0.75}[name])
    if name == 'strike':
        return strike()
    if name == 'windup':
        return windup()
    if name in OPEN_POSES:
        return open_pose(name)
    if name.startswith('dying-'):
        return dying(int(name.split('-')[1]))
    # The two remains (EnemyArtSet._death): the settled body, the bloom's last light, then cold.
    p = dying(6)
    p.life = 0.0
    p.bloom = 0.16 if name == 'death-collapse' else 0.09
    p.seam = p.grate = (0.28, 0.0)
    return p


# ---------------------------------------------------------------------------------------------------------------------
# Baked contact occlusion (BuildBlenderMirrorSentry.py's)
# ---------------------------------------------------------------------------------------------------------------------
def bake_occlusion():
    apply(idle())
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    verts, polys = [], []
    for name, obj in PARTS.items():
        if not INFO[name]['occluder']:
            continue
        ev = obj.evaluated_get(deps)
        mesh = ev.to_mesh()
        base = len(verts)
        verts.extend([v.co.copy() for v in mesh.vertices])
        polys.extend([[base + i for i in p.vertices] for p in mesh.polygons])
        ev.to_mesh_clear()
    tree = BVHTree.FromPolygons(verts, polys, all_triangles=False, epsilon=0.0)
    dirs = []
    count = 36
    for i in range(count):
        zc = 1 - (i + 0.5) / count
        r = math.sqrt(max(0.0, 1 - zc * zc))
        a = i * 2.399963
        dirs.append(Vector((r * math.cos(a), r * math.sin(a), zc)))
    reach = 0.1
    norm = sum(d.z for d in dirs)
    for name, obj in PARTS.items():
        ev = obj.evaluated_get(deps)
        mesh = ev.to_mesh()
        values = []
        for v in mesh.vertices:
            n = v.normal.normalized()
            t = n.orthogonal().normalized()
            b = n.cross(t)
            origin = v.co + n * 0.005
            occluded = 0.0
            for d in dirs:
                ray = t * d.x + b * d.y + n * d.z
                hit = tree.ray_cast(origin, ray, reach)
                if hit[0] is not None:
                    occluded += (1 - hit[3] / reach) * d.z
            values.append(occluded)
        ev.to_mesh_clear()
        if len(values) != len(obj.data.vertices):
            values = [0.0] * len(obj.data.vertices)
        attr = obj.data.color_attributes.new('occlusion', 'FLOAT_COLOR', 'POINT')
        for i, o in enumerate(values):
            open_ = max(0.0, 1 - 1.8 * o / norm)
            attr.data[i].color = (open_, open_, open_, 1.0)


bake_occlusion()
for obj in PARTS.values():
    finish_modifiers(obj)


# ---------------------------------------------------------------------------------------------------------------------
# Camera, facings and the frame loop
# ---------------------------------------------------------------------------------------------------------------------
RIG.scale = (SCALE, SCALE, SCALE * HEIGHT)
camera_data = bpy.data.cameras.new('Game view: orthographic, 30 degrees down, looking north')
camera_data.type = 'ORTHO'
camera_data.sensor_fit = 'AUTO'
camera_data.clip_start = 0.1
camera_data.clip_end = 100
camera = bpy.data.objects.new('Camera', camera_data)
collection.objects.link(camera)
scene.camera = camera
camera.rotation_euler = (math.pi / 2 - ELEVATION, 0, 0)


def frame_camera(canvas, pivot):
    scene.render.resolution_x = canvas[0] * SS
    scene.render.resolution_y = canvas[1] * SS
    camera_data.ortho_scale = max(canvas) / PPU
    centre = ((canvas[0] / 2 - pivot[0]) / PPU) * RIGHT + ((canvas[1] / 2 - pivot[1]) / PPU) * UP
    camera.location = centre - FORWARD * 20


def set_facing(degrees):
    RIG.rotation_euler = (0, 0, math.radians(degrees - 270))


def lowest_z():
    """The lowest point of the visible parts in world units of height (the rig's own z, before its 1/cos stretch)."""
    deps = bpy.context.evaluated_depsgraph_get()
    low = 1e9
    for name, obj in PARTS.items():
        if obj.hide_render or name.startswith('Root'):
            continue
        ev = obj.evaluated_get(deps)
        mesh = ev.to_mesh()
        m = ev.matrix_world
        for v in mesh.vertices:
            low = min(low, (m @ v.co).z)
        ev.to_mesh_clear()
    return low


def settle(pose):
    """Puts the pose's lowest point on the floor (a death drawing lies or kneels on it) by moving the whole rig."""
    RIG.location = (0, 0, 0)
    bpy.context.view_layer.update()
    if pose.floor:
        RIG.location = (0, 0, 0.004 - lowest_z())
        bpy.context.view_layer.update()


def screen_point(solver, rest_point, bone_name):
    world = Matrix.LocRotScale(RIG.location, RIG.rotation_euler, RIG.scale) @ (solver.G[bone_name] @ rest_point)
    return (round(world.dot(RIGHT), 5), round(world.dot(UP), 5))


def render_all():
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    started = time.time()
    for facing, degrees in FACING_SET:
        set_facing(degrees)
        for pose_name in POSE_SET:
            if pose_name in REMAINS and facing != 'front':
                continue
            living = pose_name in BODY_POSES or pose_name in OPEN_POSES
            canvas, pivot = (CANVAS_PX, PIVOT_PX) if living else (DEATH_CANVAS_PX, DEATH_PIVOT_PX)
            frame_camera(canvas, pivot)
            pose = pose_for(pose_name)
            solver = apply(pose)
            settle(pose)
            for aux, suffix in ((0.0, ''), (1.0, '_aux')):
                set_state(aux_pass=aux)
                path = OUT / facing / (pose_name + suffix + '.png')
                path.parent.mkdir(parents=True, exist_ok=True)
                scene.render.filepath = str(path)
                bpy.ops.render.render(write_still=True)
            set_state(aux_pass=0.0)
            kind = ('body' if pose_name in BODY_POSES else 'open' if pose_name in OPEN_POSES else
                    'remains' if pose_name in REMAINS else 'dying')
            records.append(dict(facing=facing, pose=pose_name, file='%s/%s.png' % (facing, pose_name),
                                aux='%s/%s_aux.png' % (facing, pose_name), canvas=list(canvas), pivot=list(pivot),
                                bloom=pose.bloom, bloom_open=pose.bloom_t, life=pose.life, shutters=pose.shutters,
                                roots=pose.roots, state=pose.state,
                                furnace_centre=screen_point(solver, MOUTH, 'chest'), kind=kind))
            print('BLOOM_FRAME', facing, pose_name, '%.1fs' % (time.time() - started), flush=True)
    RIG.location = (0, 0, 0)
    frame_camera(CANVAS_PX, PIVOT_PX)
    meta = dict(blender=bpy.app.version_string, ppu=PPU, supersample=SS, elevation_degrees=30,
                height_compensation=HEIGHT, scale=SCALE, stride=round(STRIDE * SCALE, 4),
                facings={f: d for f, d in FACINGS}, poses=POSES, frames=records)
    existing = OUT / 'frames.json'
    if existing.exists() and (wanted_poses or wanted_facings):
        old = json.loads(existing.read_text())
        keep = {(r['facing'], r['pose']): r for r in old.get('frames', [])}
        for r in records:
            keep[(r['facing'], r['pose'])] = r
        meta['frames'] = list(keep.values())
    existing.write_text(json.dumps(meta, indent=1))


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
set_facing(FACINGS[0][1])
apply(open_pose('open'))
RIG.location = (0, 0, 0)
set_state(aux_pass=0.0, preview=1.0)     # the saved scene shows the bloom; the renders draw it black
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND), compress=True)
v, t = budget()
print('BLOOM_COMPLETE', 'parts', len(PARTS), 'vertices', v, 'triangles', t, flush=True)
