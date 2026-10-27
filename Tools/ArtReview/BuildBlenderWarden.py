"""The Forge Warden, the Foundry's gate boss (docs/44 sections 1.6 and 1.8): a reproducible stylised model, rig, poses
and the sprite render for its EnemyArtSet.

Run with Blender 4.5 LTS, headless:

    blender --background --python Tools/ArtReview/BuildBlenderWarden.py -- [options]

Options after the "--":
    --out DIR        raw frames and frames.json (default TestResults/warden-render, git-ignored)
    --blend PATH     the saved scene (default ArtDirection/2026-10-27/warden-blender-01/forge-warden.blend)
    --poses LIST     a comma-separated subset of idle,step-a,step-b,strike,pass-a,pass-b,windup,death-0,death-1
    --facings LIST   a comma-separated subset of front,rear
    --variants LIST  cool (the first phase), hot (the second: visor and seams burning hotter)
    --samples N      Cycles samples per render pixel (default 10)
    --no-render      build and save the .blend, render nothing

Who it is: the first warden, forced to seal the workers' exit in the uprising, and it cannot step away from the door.
Tragic, not evil. A towering gatekeeper in blackened forge-iron plate with brass rivets, the furnace that drives it
showing through every seam and the narrow slit of its helm, a long-hafted forge hammer on its shoulder, and on its
left wrist the shackle that chained it to the door, a broken length of chain still hanging from it.

Nothing here is generated imagery: the geometry is lofted rings and primitives, every part rides one bone rigidly
(plate armour does not bend), the shading is one authored toon node group, and every frame is a pose computed by the
functions below. One model renders every frame, both facings and both phases, so details cannot drift between them.

Camera: orthographic, looking north and down 30 degrees, the game's 2:1 floor (ArenaFloor.DepthScale), as
BuildBlenderArcanist.py. Heights are modelled in design units and the rig is stretched by 1/cos(30), so one design unit
of height is one world unit on screen. The rig turns for the two facings, both looking left (the game mirrors them for
the right); the camera and the key light never move, so the light comes from the screen's upper left in every frame.
The root stays on the floor origin, which projects to the same render pixel in every frame: the sprite pivot.

Each pose renders twice: the beauty, and a glow pass in which only the burning parts show (everything else black),
from which PackWardenSprites.py lays the embers' soft bloom over the downsampled frame.
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


OUT = Path(option('--out', str(ROOT / 'TestResults/warden-render')))
BLEND = Path(option('--blend', str(ROOT / 'ArtDirection/2026-10-27/warden-blender-01/forge-warden.blend')))
SAMPLES = int(option('--samples', 10))
RENDER = not option('--no-render', False)

# ---- The sprite contract (Tools/ArtReview/PackWardenSprites.py reads frames.json) ----
PPU = 128                      # final texels per world unit: the painted Grunt's and Mite's 128
SS = 4                         # render at 4x and box-filter down: area-exact anti-aliasing, the pivot stays on a corner
CANVAS_PX = (336, 416)         # the render canvas in final texels: room for the raised hammer and the slam's reach
PIVOT_PX = (168, 56)           # the floor origin in final texels from the canvas's bottom-left corner
ELEVATION = math.radians(30)
HEIGHT = 1 / math.cos(ELEVATION)
# Both facings look left; the game mirrors them for the right. Degrees as BuildBlenderArcanist.py (270 faces the
# camera, 180 screen left, 90 away): the front turns 36 degrees off the camera so the visor slit and the chest read,
# the rear 40 degrees off straight away so the back's furnace grate and the hammer's head both show.
FACINGS = [('front', 234), ('rear', 130)]
# The whole figure is modelled a little large and scaled down to stand about 1.75 times a painted Grunt's height.
SCALE = 0.96
# Floor units (in the model's units, before SCALE) one walk cycle of two heavy steps covers.
STRIDE = 1.12
POSES = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b', 'windup', 'death-0', 'death-1']
DEATHS = {'death-0', 'death-1'}
VARIANTS = ['cool', 'hot']
GLOW_SCALE = 0.25              # the glow pass writes emission at a quarter, so the 8-bit file never clips it

wanted_poses = option('--poses')
wanted_facings = option('--facings')
wanted_variants = option('--variants')
POSE_SET = [p for p in POSES if not wanted_poses or p in wanted_poses.split(',')]
FACING_SET = [f for f in FACINGS if not wanted_facings or f[0] in wanted_facings.split(',')]
VARIANT_SET = [v for v in VARIANTS if not wanted_variants or v in wanted_variants.split(',')]


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
scene.cycles.seed = 11
scene.cycles.max_bounces = 1
scene.cycles.diffuse_bounces = 0
scene.cycles.glossy_bounces = 0
scene.cycles.transmission_bounces = 0
scene.cycles.volume_bounces = 0
scene.cycles.transparent_max_bounces = 24
scene.cycles.pixel_filter_type = 'BOX'
scene.cycles.filter_width = 1.0
scene.render.film_transparent = True
scene.render.resolution_x = CANVAS_PX[0] * SS
scene.render.resolution_y = CANVAS_PX[1] * SS
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
# The key light, fixed to the screen: from the upper left and a little in front (the painted roster's light).
KEY = (-0.55 * RIGHT + 0.76 * UP + 0.34 * TOWARD).normalized()
# A cool rim from behind the upper left: the far edge of dark iron still separates from the dark stone floor.
RIM = (-0.62 * RIGHT + 0.58 * UP - 0.52 * TOWARD).normalized()
# A warm rim from behind, low on the right: the foundry's fires, opposite the cool key.
WARM_RIM = (0.58 * RIGHT - 0.25 * UP - 0.78 * TOWARD).normalized()
HALF = (KEY + TOWARD).normalized()


# ---------------------------------------------------------------------------------------------------------------------
# State shared by every material: the phase's heat, the windup's flare, death's cooling, and which pass renders
# ---------------------------------------------------------------------------------------------------------------------
def build_state_group():
    ng = bpy.data.node_groups.new('Warden state', 'ShaderNodeTree')
    for name in ('Heat', 'Flare', 'Life', 'Glow pass'):
        ng.interface.new_socket(name, in_out='OUTPUT', socket_type='NodeSocketFloat')
    out = ng.nodes.new('NodeGroupOutput')
    for i, (name, value) in enumerate((('Heat', 0.0), ('Flare', 1.0), ('Life', 1.0), ('Glow pass', 0.0))):
        node = ng.nodes.new('ShaderNodeValue')
        node.name = name
        node.outputs[0].default_value = value
        ng.links.new(node.outputs[0], out.inputs[i])
    return ng


STATE = build_state_group()


def set_state(heat=None, flare=None, life=None, glow_pass=None):
    for name, value in (('Heat', heat), ('Flare', flare), ('Life', life), ('Glow pass', glow_pass)):
        if value is not None:
            STATE.nodes[name].outputs[0].default_value = value


# ---------------------------------------------------------------------------------------------------------------------
# Shading: one toon group, fed by authored palettes
# ---------------------------------------------------------------------------------------------------------------------
def build_toon_group():
    ng = bpy.data.node_groups.new('Warden toon', 'ShaderNodeTree')
    io = ng.interface
    for name, kind, default in [
            ('Light', 'NodeSocketColor', (1, 1, 1, 1)), ('Mid', 'NodeSocketColor', (.5, .5, .5, 1)),
            ('Shadow', 'NodeSocketColor', (.2, .2, .2, 1)), ('Deep', 'NodeSocketColor', (.05, .05, .05, 1)),
            ('Spec', 'NodeSocketFloat', 0.0), ('Spec Color', 'NodeSocketColor', (1, 1, 1, 1)),
            ('Rim', 'NodeSocketFloat', 0.35), ('Occlusion', 'NodeSocketFloat', 1.0),
            ('Band Shift', 'NodeSocketFloat', 0.0), ('Glint', 'NodeSocketFloat', 0.0),
            ('Facet', 'NodeSocketFloat', 0.5), ('Ember', 'NodeSocketFloat', 1.0)]:
        socket = io.new_socket(name, in_out='INPUT', socket_type=kind)
        socket.default_value = default
    # A colour, not a shader: materials mix zones as colours and emit once (as BuildBlenderArcanist.py).
    io.new_socket('Color', in_out='OUTPUT', socket_type='NodeSocketColor')
    n, l = ng.nodes, ng.links
    gi = n.new('NodeGroupInput')
    go = n.new('NodeGroupOutput')
    geo = n.new('ShaderNodeNewGeometry')
    state = n.new('ShaderNodeGroup')
    state.node_tree = STATE

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
    # Every facet of a plate takes one value: each flat face carries its own small random shift ('facet'), so the
    # planes read hand-painted, a little lighter or darker than the next, as on the painted Grunt's boulders.
    facet = math_node('MULTIPLY', math_node('SUBTRACT', gi.outputs['Facet'], 0.5), 0.09)
    lit = math_node('ADD', math_node('ADD', half_lambert, gi.outputs['Band Shift']), facet)
    # Painted value steps with clean transitions: shadow, the local colour, the lit plane. Dark iron reads as dark
    # until a plane turns to the key; then it flashes the cool steel of its light colour.
    first = step(lit, 0.36, 0.5)
    second = step(lit, 0.73, 0.85)
    colour = mix(first, gi.outputs['Shadow'], gi.outputs['Mid'])
    colour = mix(second, colour, gi.outputs['Light'])
    # A loose brush breakup on the undeformed surface (Generated coordinates), so it sticks to the plate.
    coords = n.new('ShaderNodeTexCoord')
    brush = n.new('ShaderNodeTexNoise')
    brush.inputs['Scale'].default_value = 6.0
    brush.inputs['Detail'].default_value = 2.0
    l.new(coords.outputs['Generated'], brush.inputs['Vector'])
    drift = n.new('ShaderNodeCombineColor')
    for channel, spread in enumerate((0.22, 0.22, 0.2)):
        l.new(math_node('MULTIPLY_ADD', brush.outputs['Fac'], spread, 1 - spread / 2), drift.inputs[channel])
    paint = n.new('ShaderNodeMixRGB')
    paint.blend_type = 'MULTIPLY'
    paint.inputs['Fac'].default_value = 1.0
    l.new(colour, paint.inputs['Color1'])
    l.new(drift.outputs[0], paint.inputs['Color2'])
    colour = paint.outputs['Color']
    # Baked contact occlusion (1 open, 0 closed) pulls hard toward the deep colour: under the pauldrons, between the
    # plates, under the helm's brim.
    closed = math_node('SUBTRACT', 1.0, step(gi.outputs['Occlusion'], 0.3, 0.97))
    colour = mix(closed, colour, gi.outputs['Deep'])
    # The furnace inside lights the iron round it from below and within: surfaces turned down catch an ember bounce,
    # stronger in the second phase and gone as it dies.
    heat = state.outputs['Heat']
    life = state.outputs['Life']
    bounce = math_node('MULTIPLY', step(dot(normal, vec(Vector((0, 0, -1)))), 0.2, 0.8), 0.13)
    bounce = math_node('MULTIPLY', bounce, math_node('MULTIPLY', math_node('MULTIPLY_ADD', heat, 0.6, 1.0), life))
    bounce = math_node('MULTIPLY', bounce, gi.outputs['Ember'])
    colour = add_colour(colour, rgb('#FF6A2A'), bounce)
    # A cool rim on edges turned to the upper left and away.
    facing = n.new('ShaderNodeLayerWeight')
    facing.inputs['Blend'].default_value = 0.4
    rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.5, 0.82), math_node('MAXIMUM', dot(normal, vec(RIM)), 0.0))
    rim = math_node('MULTIPLY', rim, gi.outputs['Rim'])
    colour = add_colour(colour, rgb('#B9CCF2'), math_node('MULTIPLY', rim, 0.55))
    # A warm rim from behind on the lower right, the foundry's fires: only the last sliver of the silhouette.
    warm_rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.66, 0.9),
                         math_node('MAXIMUM', dot(normal, vec(WARM_RIM)), 0.0))
    colour = add_colour(colour, rgb('#FF8A45'), math_node('MULTIPLY', warm_rim, 0.6))
    # A hard painted highlight for steel and brass, and a pin-point glint inside it.
    half = math_node('MAXIMUM', dot(normal, vec(HALF)), 0.0)
    spec = math_node('MULTIPLY', step(math_node('POWER', half, 36.0), 0.35, 0.55), gi.outputs['Spec'])
    colour = add_colour(colour, gi.outputs['Spec Color'], spec)
    glint = math_node('MULTIPLY', step(math_node('POWER', half, 180.0), 0.4, 0.7), gi.outputs['Glint'])
    colour = add_colour(colour, rgb('#FFFDF2'), glint)
    l.new(colour, go.inputs['Color'])
    return ng


TOON = build_toon_group()

PALETTES = {
    # light, mid, shadow, deep. Blackened forge iron whose lit planes flash cool steel, so the figure reads on the
    # orange of the lava as well as on the dark stone floor.
    'iron': ('#8B9DBE', '#3C3A45', '#211E27', '#0D0B10'),
    # The darker under-plates: the rerebraces, the fauld, the inner thighs.
    'iron-dark': ('#6F7B95', '#2F2D37', '#1A181F', '#0B090D'),
    'brass': ('#FFE3A2', '#C9913D', '#72481F', '#34200D'),
    # The shackle and its chain: worn iron, rubbed bright where it chafed, lighter than the plate so it reads.
    'steel': ('#DCE3EE', '#8A8D99', '#4A4852', '#1F1D23'),
    'wood': ('#8C6A4E', '#5B3F2E', '#37261B', '#1B120C'),
    'leather': ('#6E5042', '#493328', '#2B1D17', '#150E0B'),
    'soot': ('#3E3843', '#221E26', '#141117', '#09080B'),
}
OUTLINE = '#120D14'

# The furnace's colours: each burning material has a first-phase ramp (edge to core) and a second-phase one, hotter.
EMBERS = {
    #           first edge  first core  second edge  second core  strength
    'core': ('#A9300E', '#FF8C34', '#F2701E', '#FFD98A', 1.0),
    'seam': ('#E2581C', '#FFC55E', '#FF9A36', '#FFF3D2', 1.15),
    'visor': ('#FF7A26', '#FFE0A0', '#FFB646', '#FFFDF0', 1.5),
    'face': ('#4E1A12', '#A8401D', '#E2561E', '#FFB04C', 1.0),
    'stack': ('#C2401A', '#FFB050', '#FF8A30', '#FFF0C0', 1.1),
}


class Shading:
    """Builds a material: toon colours per palette, mixed as colours by the zones the material computes, emitted once.
    The material shades itself; the scene has no lights. In the glow pass every material emits black except the
    burning ones, which emit their colour scaled by GLOW_SCALE."""

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
            a = self.nodes.new('ShaderNodeAttribute')
            a.attribute_type = 'GEOMETRY'
            a.attribute_name = 'facet'
            self._facet = a.outputs['Fac']
        return self._facet

    def toon(self, palette, spec=0.0, spec_color='#EAF1FF', rim=0.4, band_shift=0.0, occlusion=True, glint=0.0,
             ember=1.0):
        light, mid, shadow, deep = PALETTES[palette] if isinstance(palette, str) else palette
        g = self.nodes.new('ShaderNodeGroup')
        g.node_tree = TOON
        g.inputs['Light'].default_value = lin(light)
        g.inputs['Mid'].default_value = lin(mid)
        g.inputs['Shadow'].default_value = lin(shadow)
        g.inputs['Deep'].default_value = lin(deep)
        g.inputs['Spec'].default_value = spec
        g.inputs['Glint'].default_value = glint
        g.inputs['Spec Color'].default_value = lin(spec_color)
        g.inputs['Rim'].default_value = rim
        g.inputs['Band Shift'].default_value = band_shift
        g.inputs['Ember'].default_value = ember
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

    def scale(self, colour, factor):
        m = self.nodes.new('ShaderNodeVectorMath')
        m.operation = 'SCALE'
        self.links.new(colour, m.inputs[0])
        if isinstance(factor, (int, float)):
            m.inputs['Scale'].default_value = factor
        else:
            self.links.new(factor, m.inputs['Scale'])
        return m.outputs[0]

    def noise(self, scale, detail=2.0):
        coord = self.nodes.new('ShaderNodeTexCoord')
        tex = self.nodes.new('ShaderNodeTexNoise')
        tex.inputs['Scale'].default_value = scale
        tex.inputs['Detail'].default_value = detail
        self.links.new(coord.outputs['Generated'], tex.inputs['Vector'])
        return tex.outputs['Fac']

    def backfacing(self):
        return self.nodes.new('ShaderNodeNewGeometry').outputs['Backfacing']

    def finish(self, colour, glow=None):
        """Emits colour in the beauty pass. glow: the burning colour the glow pass shows, or None for black."""
        black = self.rgb('#000000')
        pass_colour = self.scale(glow, GLOW_SCALE) if glow is not None else black
        final = self.mix(self.state('Glow pass'), colour, pass_colour)
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
    # The hull's normals are flipped: its far side faces the camera and draws, its near side is see-through.
    emit = s.nodes.new('ShaderNodeEmission')
    colour = s.mix(s.state('Glow pass'), s.rgb(OUTLINE), s.rgb('#000000'))
    s.links.new(colour, emit.inputs['Color'])
    transparent = s.nodes.new('ShaderNodeBsdfTransparent')
    mix = s.nodes.new('ShaderNodeMixShader')
    s.links.new(s.backfacing(), mix.inputs['Fac'])
    s.links.new(emit.outputs[0], mix.inputs[1])
    s.links.new(transparent.outputs[0], mix.inputs[2])
    s.links.new(mix.outputs[0], s.out.inputs['Surface'])
    return s.mat


def ember_material(key):
    """A burning material: a noisy gradient from its edge colour to its core, the first phase's or (by Heat) the
    second's, brightened by the windup's Flare and dimmed by death's Life."""
    cool_edge, cool_core, hot_edge, hot_core, strength = EMBERS[key]
    s = Shading('Ember ' + key)
    ramps = []
    for edge, core in ((cool_edge, cool_core), (hot_edge, hot_core)):
        ramp = s.nodes.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position = 0.15
        ramp.color_ramp.elements[0].color = lin(edge)
        ramp.color_ramp.elements[1].position = 0.85
        ramp.color_ramp.elements[1].color = lin(core)
        s.links.new(s.noise(24.0 if key != 'face' else 9.0, 1.5), ramp.inputs[0])
        ramps.append(ramp.outputs[0])
    colour = s.mix(s.state('Heat'), ramps[0], ramps[1])
    # The windup flares the visor and the seams; the second phase burns stronger; a dying furnace cools to a dull red.
    flare = s.state('Flare') if key in ('visor', 'seam', 'core', 'stack') else 1.0
    gain = s.math('MULTIPLY', s.math('MULTIPLY_ADD', s.state('Heat'), 0.25, strength), flare)
    life = s.state('Life')
    colour = s.mix(s.math('SUBTRACT', 1.0, life), s.scale(colour, gain), s.rgb('#3A140E'))
    return s.finish(colour, glow=s.scale(colour, s.math('MULTIPLY', life, life)))


MATERIALS = {}


def build_materials():
    m = MATERIALS
    m['outline'] = outline_material()
    m['iron'] = plain('Blackened iron', 'iron', spec=0.55, spec_color='#E4ECFF', glint=0.6)
    m['iron-dark'] = plain('Blackened iron, under-plates', 'iron-dark', spec=0.35, spec_color='#C9D6F0', rim=0.3)
    m['brass'] = plain('Brass', 'brass', spec=1.1, spec_color='#FFF1C4', glint=1.4, rim=0.25)
    m['steel'] = plain('Worn chain iron', 'steel', spec=0.9, spec_color='#FFFFFF', glint=1.2, rim=0.5)
    m['wood'] = plain('Charred haft', 'wood', spec=0.12, spec_color='#C9A27A', rim=0.25)
    m['leather'] = plain('Grip leather', 'leather', spec=0.1, rim=0.25)
    m['soot'] = plain('Soot recess', 'soot', rim=0.0, ember=0.4)
    for key in EMBERS:
        m[key] = ember_material(key)


build_materials()


# ---------------------------------------------------------------------------------------------------------------------
# Geometry: every part is low-poly and flat-shaded, chamfered by a bevel so its edges catch the key in painted lines
# ---------------------------------------------------------------------------------------------------------------------
PARTS = {}      # name -> object
INFO = {}       # name -> dict(bone, material, contour, chamfer, occluder)


def new_object(name, verts, faces, bone, material, contour=True, chamfer=0.006, occluder=False, flat=True):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in verts], [], faces)
    mesh.validate()
    # A closed part faces outward whatever order its rings ran in; an open plate keeps the front its rows give it.
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
    PARTS[name] = obj
    INFO[name] = dict(bone=bone, material=material, contour=contour, chamfer=chamfer, occluder=occluder)
    return obj


def loft(name, rings, bone, material, closed=True, cap_start=False, cap_end=False, **kw):
    """Quads between consecutive rings (each a list of points, same length, counter-clockwise seen from the side the
    normals face). Caps fan to the ring's centre."""
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
    """A polygon ring (counter-clockwise from above, first point at the front, -Y). keel pushes the front centre
    forward (a breastplate's ridge, a helm's prow), back pushes the back out."""
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


def frame_points(origin, axis, rings):
    """Rings built round an axis: rings is [(t, rx, ry)], t along the axis from origin; returns point rings."""
    axis = Vector(axis)
    length = axis.length
    d = axis.normalized()
    ref = Vector((0, -1, 0)) if abs(d.y) < 0.9 else Vector((1, 0, 0))
    u = (ref - d * ref.dot(d)).normalized()     # toward the front
    v = d.cross(u)
    out = []
    for t, rx, ry, *rest in rings:
        count = rest[0] if rest else 8
        c = Vector(origin) + d * (t * length)
        out.append([c + u * (ry * math.cos(2 * math.pi * k / count)) + v * (rx * math.sin(2 * math.pi * k / count))
                    for k in range(count)])
    return out


def ellipsoid(name, centre, radii, bone, material, rings=6, segments=10, axes=None, lower=-1.0, upper=1.0, **kw):
    """A faceted ellipsoid (or a band of one, lower..upper in latitude sine), optionally on its own axes."""
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
    for cap, j, flip, pole in [(lower > -0.999, 0, True, None), (upper < 0.999, rings, False, None)]:
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
    """A box on its own axes (x, y, z unit vectors), size the full extents; taper scales the +z face."""
    ax = axes or (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))
    hx, hy, hz = size[0] / 2, size[1] / 2, size[2] / 2
    verts = []
    for sz in (-1, 1):
        t = taper if sz > 0 else 1.0
        for sy, sx in ((-1, -1), (-1, 1), (1, 1), (1, -1)):
            verts.append(Vector(centre) + ax[0] * (sx * hx * t) + ax[1] * (sy * hy * t) + ax[2] * (sz * hz))
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return new_object(name, verts, faces, bone, material, **kw)


def tube(name, path, radii, bone, material, count=8, cap_start=True, cap_end=True, twist_ref=Vector((0, -1, 0)), **kw):
    """A tube along a polyline with a radius per point (r or (rx, ry)); its frames are transported to stay untwisted."""
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
    return loft(name, rings, bone, material, closed=True, cap_start=cap_start, cap_end=cap_end, **kw)


def torus(name, centre, axes, radii, wire, bone, material, count=14, wire_count=6, arc=(0.0, 2 * math.pi), **kw):
    """A ring of wire: centre, axes (u, v, normal), radii (ru, rv) of its path, wire radius; arc for a broken link."""
    u, v, nrm = axes
    a0, a1 = arc
    closed = abs(a1 - a0 - 2 * math.pi) < 1e-6
    steps = count if closed else count - 1
    path = []
    for i in range(count if closed else count):
        a = a0 + (a1 - a0) * i / steps
        path.append(Vector(centre) + u * (radii[0] * math.cos(a)) + v * (radii[1] * math.sin(a)))
    rings = []
    for i, p in enumerate(path):
        a = a0 + (a1 - a0) * i / steps
        tangent = (-u * (radii[0] * math.sin(a)) + v * (radii[1] * math.cos(a))).normalized()
        out = (p - Vector(centre)).normalized()
        out = (out - tangent * out.dot(tangent)).normalized()
        rings.append([p + out * (wire * math.cos(2 * math.pi * k / wire_count)) + nrm * (wire * math.sin(2 * math.pi * k / wire_count))
                      for k in range(wire_count)])
    if closed:
        n = wire_count
        verts = [p for ring in rings for p in ring]
        faces = []
        m = len(rings)
        for j in range(m):
            j1 = (j + 1) % m
            for i in range(n):
                i1 = (i + 1) % n
                faces.append((j * n + i, j * n + i1, j1 * n + i1, j1 * n + i))
        return new_object(name, verts, faces, bone, material, **kw)
    return loft(name, rings, bone, material, closed=True, cap_start=True, cap_end=True, **kw)


RIVETS = {}     # bone -> list of (point, normal, radius)


def rivet(bone, point, normal, radius=0.013):
    RIVETS.setdefault(bone, []).append((Vector(point), Vector(normal).normalized(), radius))


def build_rivets():
    """Every rivet a low brass dome; one mesh per bone, no contour (a dark ring would swallow a two-texel rivet)."""
    for bone, items in RIVETS.items():
        verts, faces = [], []
        for p, nrm, r in items:
            ref = Vector((0, 0, 1)) if abs(nrm.z) < 0.9 else Vector((1, 0, 0))
            u = (ref - nrm * ref.dot(nrm)).normalized()
            v = nrm.cross(u)
            base = len(verts)
            seg = 6
            for j, (lift, scale) in enumerate(((0.0, 1.0), (0.45, 0.78), (0.75, 0.38))):
                for i in range(seg):
                    a = 2 * math.pi * (i + 0.5 * j) / seg
                    verts.append(p + nrm * (r * lift) + (u * math.cos(a) + v * math.sin(a)) * (r * scale))
            verts.append(p + nrm * (r * 0.85))
            top = len(verts) - 1
            for j in range(2):
                for i in range(seg):
                    i1 = (i + 1) % seg
                    faces.append((base + j * seg + i, base + j * seg + i1, base + (j + 1) * seg + i1, base + (j + 1) * seg + i))
            for i in range(seg):
                faces.append((base + 2 * seg + i, base + 2 * seg + (i + 1) % seg, top))
        new_object('Rivets ' + bone, verts, faces, bone, 'brass', contour=False, chamfer=0.0)


# ---- Measurements (design units, feet on z = 0, facing -Y, the Warden's left is +X) ----
# About 1.7 times a Grunt's height and twice its mass: a barrel of a chest on thick legs, the helm sunk between slab
# pauldrons so the head sits low between the shoulders, long arms ending in heavy fists at the hip.
ANKLE_Z, KNEE_Z, HIP_Z = 0.11, 0.44, 0.76
HIP_X = 0.155
WAIST_Z = 0.88
CHEST_Z = 1.08
NECK_Z = 1.34
SHOULDER = Vector((0.33, 0.03, 1.26))
ELBOW = Vector((0.425, 0.075, 0.99))
WRIST = Vector((0.445, 0.02, 0.775))
FIST = Vector((0.452, 0.0, 0.69))        # the fist's centre: the point that grips
HEAD_BASE = Vector((0, -0.024, 1.36))
HELM_TOP = 1.615
HAFT = 1.2                               # pommel to the head's centre
HEAD_LEN, HEAD_W = 0.5, 0.24
HAMMER_REST = Vector((-0.95, 0.0, 0.06))  # the pommel, the hammer standing upright out of the way at rest
LINK = 0.062                             # chain link spacing
LINKS = 5


def mirror_x(v):
    return Vector((-v.x, v.y, v.z))


def side_point(v, side):
    return v if side > 0 else mirror_x(v)


def build_legs():
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        x = HIP_X * side
        # Sabaton: a heavy blunt wedge of a foot lofted heel to toe on a flat sole; two lames over the instep.
        rings = []
        for y, w, zc, hz in [(0.1, 0.072, 0.056, 0.054), (0.05, 0.09, 0.07, 0.068), (-0.04, 0.097, 0.068, 0.066),
                             (-0.13, 0.094, 0.052, 0.05), (-0.21, 0.082, 0.038, 0.036), (-0.26, 0.06, 0.026, 0.024)]:
            rings.append([Vector((x + w * math.cos(a), y, max(0.004, zc + hz * math.sin(a))))
                          for a in [2 * math.pi * i / 8 for i in range(8)]])
        loft('Sabaton ' + s, rings, 'foot.' + s, 'iron', cap_start=True, cap_end=True, chamfer=0.008)
        for k, (y, h) in enumerate([(-0.06, 0.132), (-0.135, 0.1)]):
            pts = [Vector((x + 0.099 * math.cos(a), y, 0.01 + h * max(0.0, math.sin(a)) ** 0.8)) for a in
                   [math.radians(d) for d in (0, 30, 60, 90, 120, 150, 180)]]
            tube('Sabaton lame %s %d' % (s, k), pts, [(0.013, 0.024)] * len(pts), 'foot.' + s, 'iron-dark', count=6,
                 twist_ref=Vector((0, 1, 0)))
        # Greave: ankle to knee, the calf's bulge behind; poleyn: a pointed knee cop with a side wing.
        greave = [(0.0, 0.086, 0.084, 8), (0.25, 0.106, 0.11, 8), (0.55, 0.116, 0.12, 8), (0.85, 0.108, 0.106, 8),
                  (1.0, 0.1, 0.098, 8)]
        loft('Greave ' + s, frame_points(Vector((x, 0.005, ANKLE_Z + 0.02)), Vector((0, -0.005, KNEE_Z - ANKLE_Z - 0.05)),
                                         greave), 'shin.' + s, 'iron', cap_start=True, cap_end=True, occluder=True)
        ellipsoid('Poleyn ' + s, Vector((x, -0.066, KNEE_Z + 0.005)), (0.088, 0.072, 0.082), 'shin.' + s, 'iron',
                  rings=4, segments=8, lower=-0.6, upper=1.0)
        ellipsoid('Poleyn wing ' + s, Vector((x + 0.085 * side, -0.025, KNEE_Z)), (0.014, 0.064, 0.07), 'shin.' + s,
                  'iron', rings=3, segments=6)
        rivet('shin.' + s, Vector((x, -0.14, KNEE_Z + 0.012)), Vector((0, -1, 0.2)), 0.015)
        # Knee core: the furnace showing through the knee's joint.
        ellipsoid('Knee core ' + s, Vector((x, 0.0, KNEE_Z + 0.025)), (0.085, 0.085, 0.05), 'shin.' + s, 'core',
                  rings=3, segments=8, contour=False, chamfer=0.0)
        # Cuisse: knee to hip.
        cuisse = [(0.0, 0.1, 0.097, 8), (0.3, 0.12, 0.117, 8), (0.7, 0.134, 0.13, 8), (1.0, 0.134, 0.126, 8)]
        loft('Cuisse ' + s, frame_points(Vector((x, 0.0, KNEE_Z + 0.065)), Vector((0, 0, HIP_Z - KNEE_Z - 0.04)), cuisse),
             'thigh.' + s, 'iron-dark', cap_start=True, cap_end=True, occluder=True)
        # Tasset: a broad riveted plate hanging over the front of the thigh, riding it (its own bone follows the thigh).
        tas = []
        for z, w, out in [(0.85, 0.15, 0.0), (0.775, 0.156, 0.012), (0.7, 0.15, 0.024), (0.635, 0.132, 0.034)]:
            row = []
            for t in (-1.0, -0.5, 0.0, 0.5, 1.0):
                a = t * 0.95
                row.append(Vector((x + w * math.sin(a), -0.135 - out - 0.06 * math.cos(a) + 0.035 * abs(t), z)))
            tas.append(row)
        verts = [p for row in tas for p in row]
        faces = []
        for j in range(len(tas) - 1):
            for i in range(4):
                faces.append((j * 5 + i, (j + 1) * 5 + i, (j + 1) * 5 + i + 1, j * 5 + i + 1))
        new_object('Tasset ' + s, verts, faces, 'tasset.' + s, 'iron', chamfer=0.0)
        for z in (0.8, 0.69):
            for sx in (-1, 1):
                rivet('tasset.' + s, Vector((x + 0.1 * sx, -0.158 - (0.85 - z) * 0.32, z)), Vector((0.3 * sx, -1, 0)), 0.013)


def breast_ring(z, rx, ry, keel, count=16):
    return ring_points(0, 0.0, z, rx, ry, count, keel=keel, back=0.03)


# The breastplate's rings: z, radius across, radius deep, the keel's push. A barrel chest, widest just under the arms.
CHEST_RINGS = [(1.055, 0.33, 0.24, 0.032), (1.12, 0.362, 0.256, 0.045), (1.2, 0.378, 0.262, 0.052),
               (1.27, 0.364, 0.252, 0.044), (1.315, 0.31, 0.215, 0.028), (1.35, 0.21, 0.16, 0.0)]


def on_breastplate(z, x_across, lift=0.006):
    """The point on the breastplate's front at height z where it is x_across from the keel, lifted off the plate."""
    for (z0, rx0, ry0, k0), (z1, rx1, ry1, k1) in zip(CHEST_RINGS, CHEST_RINGS[1:]):
        if z0 <= z <= z1:
            t = (z - z0) / (z1 - z0)
            rx, ry, keel = lerp(rx0, rx1, t), lerp(ry0, ry1, t), lerp(k0, k1, t)
            break
    else:
        rx, ry, keel = CHEST_RINGS[-1][1:]
    d = math.asin(max(-1.0, min(1.0, x_across / rx)))
    a = -math.pi / 2 + d
    front = max(0.0, -math.sin(a))
    p = Vector((rx * math.cos(a), ry * math.sin(a) - keel * front ** 6, z))
    n = Vector((math.cos(a) / rx, math.sin(a) / ry, 0)).normalized()
    return p + n * lift


def build_torso():
    # Belt and fauld: a heavy leather belt with a brass plate, two iron lames below it round the hips.
    loft('Belt', [ring_points(0, 0.0, z, 0.302, 0.226, 16) for z in (0.855, 0.93)], 'pelvis', 'leather', occluder=True)
    box('Buckle', Vector((0, -0.236, 0.892)), (0.13, 0.032, 0.095), 'pelvis', 'brass', chamfer=0.008)
    for dx in (-0.045, 0.045):
        rivet('pelvis', Vector((dx, -0.255, 0.892)), Vector((0, -1, 0)), 0.012)
    for k, (z0, z1, r) in enumerate([(0.795, 0.86, 0.312), (0.735, 0.8, 0.322)]):
        rings = [ring_points(0, 0.004, z0, r, r * 0.76, 16, back=0.02),
                 ring_points(0, 0.004, z1, r - 0.012, r * 0.74, 16, back=0.01)]
        loft('Fauld %d' % k, rings, 'pelvis', 'iron-dark', occluder=True)
    # The waist's seam: the furnace shows between the belt and the plackart.
    loft('Waist core', [ring_points(0, 0.0, z, 0.272, 0.204, 14) for z in (0.915, 0.98)], 'spine', 'core',
         cap_start=True, cap_end=True, contour=False, chamfer=0.0)
    # Plackart (the belly plate, rides the spine) and breastplate (the chest): a ridged keel down the front.
    plack = [(0.955, 0.276, 0.206, 0.02), (1.02, 0.302, 0.222, 0.028), (1.075, 0.32, 0.232, 0.032)]
    loft('Plackart', [ring_points(0, 0.0, z, rx, ry, 16, keel=k, back=0.012) for z, rx, ry, k in plack], 'spine', 'iron',
         occluder=True)
    loft('Breastplate', [breast_ring(z, rx, ry, k) for z, rx, ry, k in CHEST_RINGS], 'chest', 'iron', cap_end=True,
         occluder=True, chamfer=0.007)
    # The torso's furnace, inside the plates, so every gap glows.
    ellipsoid('Torso core', Vector((0, 0.005, 1.12)), (0.29, 0.2, 0.2), 'chest', 'core', rings=4, segments=10,
              contour=False, chamfer=0.0)
    # Seams where the plates meet, burning: the breastplate's lower edge, and the two joints that set its keel plate
    # off from the flanks, climbing from the belly and turning out toward the arms under the collar.
    seam = []
    for i in range(13):
        a = math.radians(-90 - 72 + 12 * i)
        seam.append(Vector((0.336 * math.cos(a), 0.246 * math.sin(a) - 0.034 * max(0.0, -math.sin(a)) ** 6, 1.06)))
    tube('Seam chest', seam, [0.0095] * len(seam), 'chest', 'seam', count=6, contour=False, chamfer=0.0)
    for side in (-1, 1):
        path = [on_breastplate(z, side * x) for z, x in ((1.07, 0.1), (1.13, 0.1), (1.19, 0.101), (1.25, 0.104),
                                                          (1.3, 0.11))]
        tube('Seam keel ' + ('L' if side > 0 else 'R'), path, [0.0085, 0.009, 0.009, 0.0085, 0.006], 'chest', 'seam',
             count=6, contour=False, chamfer=0.0)
    # Rivets along the breastplate's upper rim, down its flanks, and either side of the keel plate.
    for i in range(9):
        a = math.radians(-90 - 64 + 16 * i)
        rivet('chest', Vector((0.315 * math.cos(a), 0.222 * math.sin(a) - 0.014, 1.3)),
              Vector((math.cos(a), math.sin(a), 0.6)), 0.014)
    for side in (-1, 1):
        for z in (1.11, 1.17, 1.23):
            a = math.radians(-90 + 54 * side)
            rivet('chest', Vector((0.376 * math.cos(a), 0.262 * math.sin(a) - 0.004, z)), Vector((math.cos(a), math.sin(a), 0)),
                  0.014)
        for z in (1.1, 1.2):
            p = on_breastplate(z, side * 0.135, 0.002)
            rivet('chest', p, Vector((p.x, p.y, 0)), 0.012)
    # The back: a furnace grate between the shoulder blades, three burning slots behind iron bars.
    grate_c = Vector((0, 0.272, 1.16))
    box('Grate frame', grate_c, (0.25, 0.032, 0.2), 'chest', 'iron-dark', chamfer=0.008,
        axes=(Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, -0.25, 1)).normalized()))
    for k, dz in enumerate((-0.054, 0.0, 0.054)):
        box('Grate slot %d' % k, grate_c + Vector((0, 0.02, dz)), (0.19, 0.012, 0.03), 'chest', 'core', contour=False,
            chamfer=0.0)
    for dx in (-0.105, 0.105):
        for dz in (-0.082, 0.082):
            rivet('chest', grate_c + Vector((dx, 0.021, dz)), Vector((0, 1, 0)), 0.012)
    # Gorget: the high collar the helm sinks into.
    gorget = [ring_points(0, 0.0, 1.32, 0.226, 0.178, 12), ring_points(0, -0.006, 1.405, 0.176, 0.148, 12)]
    loft('Gorget', gorget, 'neck', 'iron', occluder=True, chamfer=0.007)
    loft('Neck core', [ring_points(0, -0.01, z, 0.134, 0.12, 10) for z in (1.37, 1.44)], 'neck', 'core',
         cap_start=True, cap_end=True, contour=False, chamfer=0.0)


def build_helm():
    """A heavy riveted bucket helm with a prow down its front, one narrow burning slit across it and a brass brow
    band: no face, no eyes, only the furnace looking out."""
    hc = HEAD_BASE
    levels = [(0.0, 0.14, 0.136, 0.03), (0.07, 0.144, 0.14, 0.034), (0.16, 0.142, 0.138, 0.036),
              (0.215, 0.136, 0.132, 0.032), (0.24, 0.11, 0.108, 0.022), (0.255, 0.06, 0.06, 0.0)]
    rings = [ring_points(hc.x, hc.y, hc.z + h, rx, ry, 12, keel=k) for h, rx, ry, k in levels]
    loft('Helm', rings, 'head', 'iron', cap_start=True, cap_end=True, occluder=True, chamfer=0.007)
    # The crest: a low riveted ridge from brow to nape.
    crest = [hc + Vector((0, -0.118, 0.218)), hc + Vector((0, -0.06, 0.252)), hc + Vector((0, 0.02, 0.264)),
             hc + Vector((0, 0.09, 0.244)), hc + Vector((0, 0.132, 0.19))]
    tube('Helm crest', crest, [(0.014, 0.026)] * len(crest), 'head', 'iron', count=6, twist_ref=Vector((1, 0, 0)))
    # Brow band of brass, riveted round.
    band = [ring_points(hc.x, hc.y, hc.z + h, 0.147, 0.143, 12, keel=0.036) for h in (0.168, 0.198)]
    loft('Brow band', band, 'head', 'brass')
    for i in range(12):
        a = -math.pi / 2 + 2 * math.pi * i / 12 + math.pi / 12
        rivet('head', hc + Vector((0.15 * math.cos(a), 0.146 * math.sin(a), 0.183)), Vector((math.cos(a), math.sin(a), 0)),
              0.011)

    # The visor: a soot recess across the prow and, in it, the burning slit.
    def across(h, r, spread, count=9, out=0.0):
        pts = []
        for i in range(count):
            a = -math.pi / 2 + spread * (i / (count - 1) - 0.5)
            front = max(0.0, -math.sin(a))
            pts.append(hc + Vector(((r + out) * math.cos(a), (r + out) * math.sin(a) - 0.034 * front ** 6, h)))
        return pts
    # (A tube's first radius runs along twist_ref, here up the helm; the second across its surface.)
    tube('Visor recess', across(0.115, 0.146, math.radians(150)), [(0.026, 0.006)] * 9, 'head', 'soot', count=6,
         twist_ref=Vector((0, 0, 1)), contour=False, chamfer=0.0)
    tube('Visor slit', across(0.115, 0.148, math.radians(132)), [(0.0135, 0.006)] * 9, 'head', 'visor', count=6,
         twist_ref=Vector((0, 0, 1)), contour=False, chamfer=0.0)
    # Breaths: three small burning holes low on the left cheek, the side turned to the camera; rivets down the prow.
    for k in range(3):
        a = -math.pi / 2 + math.radians(34 + 9 * k)
        ellipsoid('Breath %d' % k, hc + Vector((0.145 * math.cos(a), 0.141 * math.sin(a), 0.048 - 0.004 * k)),
                  (0.009, 0.009, 0.009), 'head', 'visor', rings=2, segments=6, contour=False, chamfer=0.0)
    for h in (0.03, 0.072):
        rivet('head', hc + Vector((0, -0.176, h)), Vector((0, -1, 0)), 0.011)


def build_arms():
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
        # Pauldron: three slab lames stepping down the shoulder, each an eight-sided mesa with a flat top; they ride
        # their own bone, which lifts with the arm at a little under half its swing.
        for k, (lift, out, r, h, tilt_out) in enumerate([(0.15, 0.03, 0.205, 0.105, 0.5), (0.075, 0.085, 0.188, 0.095, 0.85),
                                                          (0.0, 0.125, 0.16, 0.085, 1.25)]):
            c = sh + Vector((out * side, 0.0, lift))
            tilt = Vector((tilt_out * side, 0.0, 1.0)).normalized()
            fwd = Vector((0, -1, 0))
            ux = fwd.cross(tilt).normalized()
            ellipsoid('Pauldron %s %d' % (s, k), c, (r, r * 0.96, h), 'pauldron.' + s, 'iron', rings=2, segments=8,
                      axes=(ux, fwd, tilt), lower=-0.3, upper=0.72, occluder=True, chamfer=0.009)
            # Rivets along each lame's outer rim.
            for i in range(5):
                a = math.radians(-64 + 32 * i)
                rim_r = r * math.cos(math.asin(-0.25))
                p = c + ux * (rim_r * math.cos(a) * -side) + fwd * (rim_r * 0.96 * math.sin(a)) - tilt * (h * 0.25)
                if k == 0 or i % 2 == 0:
                    rivet('pauldron.' + s, p + (p - c).normalized() * 0.006, (p - c).normalized(), 0.013)
        ellipsoid('Shoulder core ' + s, sh + Vector((0.0, 0.0, -0.02)), (0.11, 0.11, 0.1), 'upper_arm.' + s, 'core',
                  rings=3, segments=8, contour=False, chamfer=0.0)
        # Rerebrace, couter (the elbow cop, a pointed wing behind), vambrace flaring to the gauntlet's cuff.
        loft('Rerebrace ' + s, frame_points(sh, el - sh, [(0.05, 0.098, 0.095, 8), (0.6, 0.094, 0.092, 8),
                                                         (1.0, 0.086, 0.084, 8)]), 'upper_arm.' + s, 'iron-dark',
             cap_start=True, cap_end=True, occluder=True)
        ellipsoid('Couter ' + s, el + Vector((0.006 * side, 0.022, 0.0)), (0.094, 0.09, 0.08), 'forearm.' + s, 'iron',
                  rings=4, segments=8)
        ellipsoid('Couter wing ' + s, el + Vector((0.034 * side, 0.085, 0.012)), (0.056, 0.034, 0.068), 'forearm.' + s,
                  'iron', rings=3, segments=6)
        ellipsoid('Elbow core ' + s, el, (0.08, 0.08, 0.08), 'forearm.' + s, 'core', rings=3, segments=8, contour=False,
                  chamfer=0.0)
        loft('Vambrace ' + s, frame_points(el, wr - el, [(0.12, 0.086, 0.086, 8), (0.55, 0.084, 0.082, 8),
                                                        (0.86, 0.092, 0.09, 8), (1.0, 0.112, 0.108, 8)]),
             'forearm.' + s, 'iron', cap_start=True, cap_end=False)
        # Gauntlet: a broad cuff, then a closed fist with a row of knuckle plates and the thumb over them.
        d = (fi - wr).normalized()
        across_ = Vector((1, 0, 0)) - d * d.x
        across_.normalize()
        front = d.cross(across_).normalized()
        if front.y > 0:
            front = -front
        axes = (across_, front, d)
        box('Gauntlet ' + s, fi + d * 0.005, (0.14, 0.158, 0.148), 'hand.' + s, 'iron', axes=axes, taper=0.86,
            chamfer=0.014)
        for k in range(4):
            box('Knuckle %s %d' % (s, k), fi + d * 0.034 + front * 0.082 + across_ * (-0.051 + 0.034 * k),
                (0.031, 0.03, 0.058), 'hand.' + s, 'iron-dark', axes=axes, chamfer=0.005)
        box('Thumb ' + s, fi + front * 0.058 - across_ * (0.072 * side) - d * 0.022, (0.034, 0.058, 0.07), 'hand.' + s,
            'iron-dark', axes=axes, chamfer=0.007)
        loft('Cuff ' + s, frame_points(wr - (wr - el).normalized() * 0.034, (fi - wr) * 0.55 + (wr - el).normalized() * 0.034,
                                       [(0.0, 0.118, 0.114, 8), (1.0, 0.127, 0.122, 8)]), 'hand.' + s, 'iron',
             cap_start=True, cap_end=False)
        rivet('forearm.' + s, wr + (wr - el).normalized() * -0.06 + Vector((0.092 * side, -0.035, 0)),
              Vector((side, -0.4, 0)), 0.013)


SHACKLE_EYE = Vector()


def build_shackle():
    """On the left wrist the shackle that chained the Warden to the door: a thick worn cuff over the vambrace with an
    eye on its outer side, and from it a short length of chain, the last link broken open where it tore."""
    global SHACKLE_EYE
    el, wr = ELBOW, WRIST
    d = (wr - el).normalized()
    c = el.lerp(wr, 0.68)
    u = Vector((1, 0, 0)) - d * d.x
    u.normalize()
    v = d.cross(u).normalized()
    rings = []
    for t, r in ((-0.042, 0.102), (-0.025, 0.116), (0.025, 0.116), (0.042, 0.102)):
        rings.append([c + d * t + (u * math.cos(2 * math.pi * k / 10) + v * math.sin(2 * math.pi * k / 10)) * r
                      for k in range(10)])
    loft('Shackle', rings, 'forearm.L', 'steel', chamfer=0.007)
    box('Shackle hinge', c + u * 0.112 - v * 0.035, (0.036, 0.034, 0.06), 'forearm.L', 'brass', axes=(u, v, d),
        chamfer=0.005)
    for k in (0, 3, 6):
        a = 2 * math.pi * k / 10 + 0.4
        rivet('forearm.L', c + (u * math.cos(a) + v * math.sin(a)) * 0.118, u * math.cos(a) + v * math.sin(a), 0.012)
    # The eye, on the outer side, a little low; the chain hangs from it.
    eye_c = c + u * 0.128 + d * 0.022
    torus('Shackle eye', eye_c, (d, u, v), (0.026, 0.026), 0.011, 'forearm.L', 'steel', count=10, wire_count=6)
    SHACKLE_EYE = eye_c + d * 0.026
    # The chain, built hanging straight down from the eye at rest; each link rides its own free bone.
    for k in range(LINKS):
        centre = SHACKLE_EYE + Vector((0, 0, -(k + 0.5) * LINK))
        if k % 2 == 0:
            axes = (Vector((0, 0, 1)), Vector((1, 0, 0)), Vector((0, 1, 0)))
        else:
            axes = (Vector((0, 0, 1)), Vector((0, 1, 0)), Vector((1, 0, 0)))
        arc = (0.0, 2 * math.pi) if k < LINKS - 1 else (math.radians(75), math.radians(75 + 280))
        torus('Chain %d' % k, centre, axes, (0.04, 0.025), 0.0125, 'chain%d' % k, 'steel', count=12, wire_count=6,
              arc=arc, chamfer=0.0)


HAMMER_HEAD_C = Vector()


def build_hammer():
    """A long-hafted forge hammer, built upright at its rest out of the way: the pommel at HAMMER_REST, the haft up
    +Z, the head's long axis along X. Charred wood bound in brass, leather at the grips, a heavy iron head with brass
    bands and two striking faces still hot from the forge."""
    global HAMMER_HEAD_C
    p0 = HAMMER_REST
    up = Vector((0, 0, 1))
    path = [p0 + up * t for t in (0.0, 0.3, 0.6, 0.9, HAFT - 0.08)]
    tube('Haft', path, [0.032, 0.033, 0.034, 0.036, 0.038], 'hammer', 'wood', count=8)
    for a, b in ((0.04, 0.28), (0.46, 0.66)):
        tube('Grip %.2f' % a, [p0 + up * a, p0 + up * b], [0.04, 0.04], 'hammer', 'leather', count=8)
    for t in (0.31, 0.43, 0.98):
        tube('Ferrule %.2f' % t, [p0 + up * (t - 0.016), p0 + up * (t + 0.016)], [0.044, 0.044], 'hammer', 'brass', count=8)
    ellipsoid('Pommel', p0 + up * -0.022, (0.054, 0.054, 0.046), 'hammer', 'iron', rings=3, segments=8)
    head = p0 + up * HAFT
    HAMMER_HEAD_C = head
    box('Hammer head', head, (HEAD_LEN, HEAD_W, HEAD_W), 'hammer', 'iron', chamfer=0.018)
    box('Hammer collar', head - up * 0.14, (0.105, 0.105, 0.08), 'hammer', 'iron-dark', chamfer=0.012)
    for sx in (-1, 1):
        box('Head band %d' % sx, head + Vector((0.13 * sx, 0, 0)), (0.048, HEAD_W + 0.022, HEAD_W + 0.022), 'hammer',
            'brass', chamfer=0.007)
        box('Hammer face %d' % sx, head + Vector((HEAD_LEN / 2 * sx, 0, 0)), (0.014, HEAD_W - 0.036, HEAD_W - 0.036),
            'hammer', 'face', contour=False, chamfer=0.0)
        # Two rivets on each broad side of the head, inside the bands.
        for sy in (-1, 1):
            rivet('hammer', head + Vector((0.07 * sx, (HEAD_W / 2 + 0.002) * sy, 0)), Vector((0, sy, 0)), 0.015)
    for sy in (-1, 1):
        box('Langet %d' % sy, head - up * 0.22 + Vector((0, 0.036 * sy, 0)), (0.032, 0.014, 0.24), 'hammer', 'brass',
            chamfer=0.003)


def crack(name, points, bone, radius=0.0075):
    """A crack that opens only in the second phase: the furnace overheating splits the plate along it."""
    tube(name, points, [radius * (1.0 if 0 < k < len(points) - 1 else 0.55) for k in range(len(points))], bone, 'seam',
         count=6, contour=False, chamfer=0.0)
    INFO[name]['hot_only'] = True


def helm_point(h, degrees, lift=0.005):
    """A point on the helm's surface at height h above its base, degrees round from the front (+ to its left)."""
    hc = HEAD_BASE
    a = -math.pi / 2 + math.radians(degrees)
    front = max(0.0, -math.sin(a))
    r = 0.143 + lift
    return hc + Vector((r * math.cos(a), r * 0.97 * math.sin(a) - 0.034 * front ** 6, h))


def pauldron_point(side, k, u, v, lift=0.005):
    """A point on the flat top of pauldron lame k: u across it (outward +), v along it (forward +), both -1..1."""
    lift_, out, r, h, tilt_out = [(0.15, 0.03, 0.205, 0.105, 0.5), (0.075, 0.085, 0.188, 0.095, 0.85),
                                  (0.0, 0.125, 0.16, 0.085, 1.25)][k]
    sh = side_point(SHOULDER, side)
    c = sh + Vector((out * side, 0.0, lift_))
    tilt = Vector((tilt_out * side, 0.0, 1.0)).normalized()
    fwd = Vector((0, -1, 0))
    ux = fwd.cross(tilt).normalized()
    top = r * math.cos(math.asin(0.72)) * 0.8
    return c + tilt * (h * 0.72 + lift) + ux * (-side * u * top) + fwd * (v * top)


def build_hot_cracks():
    """The second phase: the plates split and the furnace shows through - cracks across the pauldrons, down the helm's
    cheek, out from the keel across the chest, up the greaves. Hidden in the first phase's frames."""
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        crack('Crack pauldron %s' % s, [pauldron_point(side, 0, u, v) for u, v in
                                        ((-0.9, 0.35), (-0.4, 0.15), (-0.05, 0.3), (0.35, -0.05), (0.85, -0.25))],
              'pauldron.' + s)
        crack('Crack pauldron %s low' % s, [pauldron_point(side, 1, u, v) for u, v in ((0.2, 0.8), (0.45, 0.3), (0.8, 0.1))],
              'pauldron.' + s, 0.0065)
        path = [on_breastplate(z, side * x) for z, x in ((1.17, 0.103), (1.185, 0.15), (1.21, 0.19), (1.22, 0.235),
                                                          (1.245, 0.27))]
        crack('Crack chest %s' % s, path, 'chest')
        x = HIP_X * side
        crack('Crack greave %s' % s, [Vector((x + 0.012 * side, -0.112, z)) for z in (0.2, 0.25, 0.29, 0.33)] +
              [Vector((x + 0.03 * side, -0.104, 0.36))], 'shin.' + s, 0.0065)
    crack('Crack helm', [helm_point(h, d) for h, d in ((0.165, 40), (0.135, 46), (0.09, 43), (0.055, 50), (0.02, 47))],
          'head', 0.0065)
    crack('Crack plackart', [Vector((0.16, -0.21, z)) + Vector((0.01 * math.sin(z * 90), 0, 0)) for z in
                             (0.965, 0.99, 1.015, 1.04)], 'spine', 0.0065)


build_legs()
build_torso()
build_helm()
build_arms()
build_shackle()
build_hammer()
build_hot_cracks()
build_rivets()

# Glowing parts carry no contour: a dark ring round a flame reads as a hole.
for name, obj in PARTS.items():
    obj.data.materials.append(MATERIALS[INFO[name]['material']])


# ---------------------------------------------------------------------------------------------------------------------
# Rig: plate rides bones rigidly
# ---------------------------------------------------------------------------------------------------------------------
arm_data = bpy.data.armatures.new('Forge Warden rig')
RIG = bpy.data.objects.new('Forge Warden', arm_data)
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
bone('head', tuple(HEAD_BASE), (HEAD_BASE.x, HEAD_BASE.y, HELM_TOP), 'neck')
for side, s in ((1, 'L'), (-1, 'R')):
    sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
    bone('pauldron.' + s, sh, sh + Vector((0.1 * side, 0, 0.05)), 'chest')
    bone('upper_arm.' + s, sh, el, 'chest')
    bone('forearm.' + s, el, wr, 'upper_arm.' + s)
    bone('hand.' + s, wr, fi + (fi - wr).normalized() * 0.05, 'forearm.' + s)
    bone('thigh.' + s, (HIP_X * side, 0, HIP_Z), (HIP_X * side, 0, KNEE_Z), 'pelvis')
    bone('tasset.' + s, (HIP_X * side, -0.1, HIP_Z + 0.06), (HIP_X * side, -0.12, 0.58), 'pelvis')
    bone('shin.' + s, (HIP_X * side, 0, KNEE_Z), (HIP_X * side, 0.005, ANKLE_Z), 'thigh.' + s)
    bone('foot.' + s, (HIP_X * side, 0.005, ANKLE_Z), (HIP_X * side, -0.2, 0.03), 'shin.' + s)
bone('hammer', tuple(HAMMER_REST), tuple(HAMMER_REST + Vector((0, 0, HAFT))), 'root')
for k in range(LINKS):
    top = SHACKLE_EYE + Vector((0, 0, -k * LINK))
    bone('chain%d' % k, tuple(top), tuple(top + Vector((0, 0, -LINK))), 'root')
bpy.ops.object.mode_set(mode='OBJECT')
REST = {b.name: b.matrix_local.copy() for b in arm_data.bones}
PARENT = {b.name: (b.parent.name if b.parent else None) for b in arm_data.bones}
FOLLOWERS = {'pauldron.L': ('upper_arm.L', 0.24), 'pauldron.R': ('upper_arm.R', 0.24),
             'tasset.L': ('thigh.L', 0.62), 'tasset.R': ('thigh.R', 0.62)}
FREE = {'hammer'} | {'chain%d' % k for k in range(LINKS)}


def depth(name):
    d = 0
    while PARENT[name]:
        name = PARENT[name]
        d += 1
    return d


# Parents before children; a follower after the bone it follows; the chain after the forearm that carries its shackle.
ORDER = sorted(REST, key=lambda b: (depth(b) + (1 if b in FOLLOWERS else 0) + (50 if b.startswith('chain') else 0), b))
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

CONTOUR = 0.0095


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
    """A deterministic random value per face, so each plane is painted a little lighter or darker than the next."""
    mesh = obj.data
    attr = mesh.attributes.new('facet', 'FLOAT', 'FACE')
    for i in range(len(mesh.polygons)):
        h = math.sin((i + 1) * 12.9898 + seed * 78.233) * 43758.5453
        attr.data[i].value = h - math.floor(h)


for k, obj in enumerate(PARTS.values()):
    facet_attribute(obj, k)


# ---------------------------------------------------------------------------------------------------------------------
# Posing: deltas are rotations about each bone's head in the rig's rest space, accumulated down the hierarchy
# (G_bone = G_parent * D_bone); Blender's matrix_basis is then rest^-1 * D * rest. (As BuildBlenderArcanist.py, with
# free bones placed outright - the hammer, the chain - and followers that turn part of the way with another bone.)
# ---------------------------------------------------------------------------------------------------------------------
def rot(x=0.0, y=0.0, z=0.0):
    """Degrees about the rig's axes: +x pitches a raised bone forward (toward -Y), +y leans it to the Warden's left,
    +z turns it to the left."""
    return (Matrix.Rotation(math.radians(z), 3, 'Z') @ Matrix.Rotation(math.radians(y), 3, 'Y')
            @ Matrix.Rotation(math.radians(x), 3, 'X'))


class Pose:
    def __init__(self):
        self.rot = {}             # bone -> 3x3 delta in rest space
        self.offset = Vector()    # pelvis translation
        self.feet = {}            # 'L'/'R' -> (ankle target, foot pitch degrees, foot yaw degrees)
        self.hands = {}           # 'L'/'R' -> (fist target, elbow pole)
        self.free = {}            # free bone -> 4x4 placement (rest space to posed rig space)
        self.chain = None         # (swing about x, swing about y) in degrees
        self.heat_flare = 1.0     # the visor's and seams' brightness this pose (the windup flares)
        self.life = 1.0           # the furnace dying in death

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


def hammer_placement(grip_target, direction, head_axis, grip_s):
    """The hammer's G: the haft point grip_s from the pommel lands on grip_target, the haft points along direction
    (pommel to head), the head's long axis along head_axis (made square to the haft)."""
    d = Vector(direction).normalized()
    a = Vector(head_axis) - d * Vector(head_axis).dot(d)
    a.normalize()
    r = Matrix((a, d.cross(a), d)).transposed().to_4x4()
    rest_grip = HAMMER_REST + Vector((0, 0, grip_s))
    return Matrix.Translation(Vector(grip_target)) @ r @ Matrix.Translation(-rest_grip)


def hold(p, grip_r, direction, head_axis, s_r=0.42, s_l=None, pole_r=(-0.8, 0.3, -0.6), pole_l=(0.8, 0.3, -0.6)):
    """The right fist holds the haft at s_r from the pommel at grip_r; the left one, if s_l, further along it."""
    g = hammer_placement(grip_r, direction, head_axis, s_r)
    p.free['hammer'] = g
    p.hands['R'] = (Vector(grip_r), Vector(pole_r))
    if s_l is not None:
        p.hands['L'] = (g @ (HAMMER_REST + Vector((0, 0, s_l))), Vector(pole_l))


class Solver:
    """Evaluates a Pose into per-bone G matrices (rest space to posed space) and Blender's matrix_basis."""

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
            if b.startswith('chain'):
                self.place_chain()
            G_total = self.pose.free.get(b)
            D = self.place(b, pending.get(b), G_total)
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
        # The target is the fist; the wrist lies short of it along the line from the shoulder (the wrist straight).
        reach = (Vector(target) - shoulder).normalized()
        wrist_target = Vector(target) - reach * (fist0 - wr0).length
        elbow, wrist, hinge = two_bone(shoulder, wrist_target, l1, l2, Vector(pole))
        rest_pole = Vector((0.3 if s == 'L' else -0.3, 1, -0.1))
        _, _, rest_hinge = two_bone(sh0, wr0, l1, l2, rest_pole)
        W_upper = frame_from(elbow - shoulder, hinge) @ frame_from(el0 - sh0, rest_hinge).transposed()
        W_fore = frame_from(wrist - elbow, hinge) @ frame_from(wr0 - el0, rest_hinge).transposed()
        return {'upper_arm.' + s: W_upper, 'forearm.' + s: W_fore, 'hand.' + s: W_fore}

    def place_chain(self):
        """The chain hangs from the shackle's eye under its own weight, swung by the pose (a pendulum's lag)."""
        if 'chain0' in self.pose.free:
            return
        eye = self.G['forearm.L'] @ SHACKLE_EYE
        swing_x, swing_y = self.pose.chain or (0.0, 0.0)
        top = eye
        for k in range(LINKS):
            share = (k + 1) / LINKS
            down = (rot(x=swing_x * (0.6 + 0.5 * share), y=swing_y * (0.6 + 0.5 * share)) @ Vector((0, 0, -1))).normalized()
            centre = top + down * (LINK * 0.5)
            rest_centre = SHACKLE_EYE + Vector((0, 0, -(k + 0.5) * LINK))
            turn = Vector((0, 0, -1)).rotation_difference(down).to_matrix().to_4x4()
            self.pose.free['chain%d' % k] = Matrix.Translation(centre) @ turn @ Matrix.Translation(-rest_centre)
            top = top + down * LINK


def rig_matrix():
    return Matrix.LocRotScale(RIG.location, RIG.rotation_euler, RIG.scale)


def apply(pose):
    solver = Solver(pose)
    basis = solver.solve()
    for b, m in basis.items():
        RIG.pose.bones[b].matrix_basis = m
    set_state(flare=pose.heat_flare, life=pose.life)
    return solver


# ---------------------------------------------------------------------------------------------------------------------
# The poses. Every function returns a Pose; the base stance is shared.
# ---------------------------------------------------------------------------------------------------------------------
ACROSS = Vector((1, 0, 0))


def local_floor(x_world, y_world, degrees):
    """A floor offset from the root as the camera sees it (x to screen right, y away) in the rig's own frame for a
    facing: the two facings are separate drawings, so a blow can be aimed to land toward the screen's left in each."""
    a = math.radians(degrees - 270)
    return Vector(((x_world * math.cos(a) + y_world * math.sin(a)) / SCALE,
                   (-x_world * math.sin(a) + y_world * math.cos(a)) / SCALE, 0))


def stance(breath=0.0):
    """Heavy and a little stooped, as if at the end of a shift that never ends: knees soft, the chest rounded, the
    helm sunk between the shoulders and looking ahead. The left arm hangs with its shackle and the broken chain."""
    p = Pose()
    p.offset = Vector((0, 0, -0.018))
    p.add('pelvis', rot(x=2))
    p.add('spine', rot(x=5))
    p.add('chest', rot(x=8 + breath))
    p.add('neck', rot(x=12))
    p.add('head', rot(x=-14))
    p.feet['L'] = (Vector((0.18, 0.035, ANKLE_Z)), 0, 12)
    p.feet['R'] = (Vector((-0.175, -0.05, ANKLE_Z)), 0, -14)
    p.hands['L'] = (Vector((0.48, -0.07, 0.7)), Vector((0.6, 0.8, -0.2)))
    p.chain = (5.0, -4.0)
    return p


def sentry_hold(p, lift=0.0, swing=0.0):
    """The hammer held like a sentry's: the right fist round the haft at mid-length, the head down at the floor
    beside the right foot, the pommel up by the shoulder. Walking, it lifts off the floor and swings a little."""
    head = Vector((-0.5 + 0.02 * swing, -0.24 - 0.07 * swing, HEAD_W / 2 + 0.004 + lift))
    direction = Vector((-0.08 + 0.02 * swing, -0.13 - 0.1 * swing, -1.0))
    g = hammer_placement(head, direction, ACROSS, HAFT)
    p.free['hammer'] = g
    p.hands['R'] = (g @ (HAMMER_REST + Vector((0, 0, 0.5))), Vector((-0.9, 0.5, -0.2)))


def idle(variant):
    p = stance()
    sentry_hold(p)
    return p


def walk_legs(p, phase):
    """The heavy walk's lower body: distance-driven, two steps a cycle, the right foot's contact at 0."""
    bob = -0.03 * (0.5 + 0.5 * math.cos(4 * math.pi * phase))
    p.offset = Vector((0.01 * math.sin(2 * math.pi * phase), 0, bob - 0.016))
    excursion = STRIDE / 4
    duty = 0.5
    for s, offset, x in (('R', 0.0, -0.17), ('L', 0.5, 0.175)):
        q = (phase + offset) % 1.0
        if q < duty:      # planted: slides back at the ground's speed
            t = q / duty
            y = lerp(-excursion, excursion, t)
            z = ANKLE_Z
            pitch = lerp(-4, 8, smooth(0.6, 1.0, t))
        else:             # swinging through, lifted
            t = (q - duty) / (1 - duty)
            y = lerp(excursion, -excursion, smooth(0.0, 1.0, t))
            z = ANKLE_Z + 0.11 * math.sin(math.pi * t) ** 1.2
            pitch = lerp(18, -10, t)
        p.feet[s] = (Vector((x, y, z)), pitch, 12 if s == 'L' else -12)
    swing = math.sin(2 * math.pi * phase)
    p.add('pelvis', rot(z=6 * swing))
    p.add('spine', rot(x=3, z=-3 * swing))
    p.add('chest', rot(x=3, z=-4 * swing))
    p.add('head', rot(x=-3, z=-1 * swing))
    return swing


def walk(phase):
    p = stance()
    swing = walk_legs(p, phase)
    # The free arm swings against its leg; the chain swings after it; the hammer swings from the fist with the right
    # leg, its head clear of the floor.
    p.hands['L'] = (Vector((0.48, -0.07 + 0.12 * swing, 0.71 + 0.03 * abs(swing))), Vector((0.6, 0.8, -0.2)))
    lag = math.sin(2 * math.pi * (phase - 0.12))
    p.chain = (-18 * lag, -6.0)
    sentry_hold(p, lift=0.05 + 0.02 * math.cos(4 * math.pi * phase), swing=-swing)
    return p


def strike_head(p, head, grip, apart, axis=Vector((0, 0, 1)), pole_r=(-0.9, 0.4, -0.3), pole_l=(0.9, 0.2, -0.4)):
    """Both fists on the haft, the right at grip and the left apart further down it, the head's centre on head."""
    direction = head - grip
    reach = direction.length
    g = hammer_placement(grip, direction, axis, HAFT - reach)
    p.free['hammer'] = g
    p.hands['R'] = (Vector(grip), Vector(pole_r))
    p.hands['L'] = (g @ (HAMMER_REST + Vector((0, 0, HAFT - reach + apart))), Vector(pole_l))


def strike(variant, degrees):
    """The blow landing: both fists on the haft, the head driven into the floor ahead and to the screen's left (where
    the facing looks), its face flat on the stone, the body folded over it and the legs braced wide."""
    p = stance()
    # Where the head lands, as the camera sees it: well clear of the body toward the screen's left.
    land = local_floor(-0.74, -0.2 if degrees > 180 else 0.24, degrees)
    toward = land.normalized()
    # Turn the body into the blow: its yaw toward the landing, its weight over the leading leg.
    yaw = math.degrees(math.atan2(toward.x, -toward.y))    # rot(z=+) turns the figure to its left (+X)
    p.offset = Vector((0, 0, -0.1)) + toward * 0.06
    p.add('pelvis', rot(x=9, z=yaw * 0.45))
    p.add('spine', rot(x=10, z=yaw * 0.2))
    p.add('chest', rot(x=11, z=yaw * 0.2))
    p.add('neck', rot(x=4))
    p.add('head', rot(x=-8))
    side = Vector((-toward.y, toward.x, 0))
    p.feet['L'] = (Vector((0.2, 0.0, ANKLE_Z)) + toward * 0.2 + side * 0.04, 0, 16)
    p.feet['R'] = (Vector((-0.22, 0.0, ANKLE_Z)) - toward * 0.17, 6, -18)
    # The haft about 45 degrees down, so the face lands flat: the head's centre this high above the floor.
    head = land + Vector((0, 0, 0.27))
    grip = toward * 0.34 + Vector((0, 0, 0.8)) - side * 0.04
    strike_head(p, head, grip, 0.24)
    p.chain = (34.0, -10.0)
    return p


def windup(variant):
    """The telegraph: the hammer hauled high over the head in both fists, the body arched back, the furnace flaring
    in the slit and the seams. Readable at a glance: the tallest silhouette the Warden has, every other pose holding
    the hammer low."""
    p = stance()
    p.offset = Vector((0, 0.03, -0.035))
    p.add('pelvis', rot(x=-4))
    p.add('spine', rot(x=-8))
    p.add('chest', rot(x=-12))
    p.add('neck', rot(x=-6))
    p.add('head', rot(x=-8))
    p.feet['L'] = (Vector((0.21, -0.13, ANKLE_Z)), 0, 14)
    p.feet['R'] = (Vector((-0.2, 0.12, ANKLE_Z)), 0, -16)
    # Both fists close together above the helm, the haft running up and back from them, the head high behind.
    grip = Vector((-0.07, 0.05, 1.66))
    direction = Vector((0.1, 0.8, 0.62))
    hold(p, grip, direction, Vector((0, -0.7, 0.7)), s_r=0.28, s_l=0.48, pole_r=(-0.9, -0.1, -0.3),
         pole_l=(0.9, -0.1, -0.3))
    p.chain = (-26.0, 12.0)
    p.heat_flare = 1.45
    return p


def death(k):
    """The furnace failing: first the knees go and the hammer slips, then the Warden kneels, slumped over itself as
    if still on guard, the helm bowed, the slit dark, the hammer on the floor and the broken chain at rest."""
    p = stance()
    c = [dict(drop=0.24, slump=0.35, life=0.55), dict(drop=0.4, slump=1.0, life=0.12)][k]
    drop, slump = c['drop'], c['slump']
    p.offset = Vector((0, 0.05 + 0.04 * slump, -drop))
    p.add('pelvis', rot(x=8 * slump))
    p.add('spine', rot(x=10 + 14 * slump))
    p.add('chest', rot(x=12 + 16 * slump, y=-4 * slump))
    p.add('neck', rot(x=10 + 14 * slump))
    p.add('head', rot(x=6 + 12 * slump, z=-6 * slump))
    kneel = smooth(0.1, 0.36, drop)
    for s, x in (('R', -0.18), ('L', 0.185)):
        p.feet[s] = (Vector((x, lerp(0.0, 0.38, kneel), lerp(ANKLE_Z, 0.075, kneel))), lerp(0, 72, kneel), 0)
    # The left hand rests on the floor or the knee, the chain lying by it; the hammer falls to the right.
    p.hands['L'] = (Vector((lerp(0.47, 0.38, slump), lerp(-0.12, -0.42, slump), lerp(0.55, 0.13, slump))),
                    Vector((0.6, 0.8, -0.2)))
    if k == 0:
        # The head has hit the floor to the right; the fist still holds the haft, slipping.
        head = Vector((-0.6, -0.46, HEAD_W / 2 + 0.004))
        grip = Vector((-0.34, -0.2, 0.5))
        direction = head - grip
        g = hammer_placement(grip, direction, Vector((0, 0, 1)), HAFT - direction.length)
        p.free['hammer'] = g
        p.hands['R'] = (grip, Vector((-0.9, 0.3, -0.4)))
    else:
        # Let go: the hammer lies on the floor at the kneeling figure's side, its head clear of the body toward the
        # screen's left, the haft running back to the open hand.
        head = local_floor(-0.78, -0.12, FACINGS[0][1]) + Vector((0, 0, HEAD_W / 2 + 0.002))
        pommel = local_floor(-0.08, -0.5, FACINGS[0][1]) + Vector((0, 0, 0.04))
        g = hammer_placement(head, head - pommel, Vector((0, 0, 1)), HAFT)
        p.free['hammer'] = g
        p.hands['R'] = (pommel.lerp(head, 0.3) + Vector((0, 0, 0.09)), Vector((-0.9, 0.3, -0.4)))
    p.chain = (40.0 * slump, 0.0)
    p.life = c['life']
    return p


def pose_for(name, variant, degrees=FACINGS[0][1]):
    if name == 'idle':
        return idle(variant)
    if name.startswith('step') or name.startswith('pass'):
        # Contact at a quarter's start: A is the right foot's contact, B the left's; the passings half a step later.
        phase = {'step-a': 0.0, 'pass-a': 0.25, 'step-b': 0.5, 'pass-b': 0.75}[name]
        return walk(phase)
    if name == 'strike':
        return strike(variant, degrees)
    if name == 'windup':
        return windup(variant)
    return death(int(name[-1]))


# ---------------------------------------------------------------------------------------------------------------------
# Baked contact occlusion: rays from every vertex in the stance, against the plates that do not swing about.
# ---------------------------------------------------------------------------------------------------------------------
def bake_occlusion():
    apply(idle('cool'))
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
    reach = 0.16
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
camera_data.ortho_scale = max(CANVAS_PX) / PPU
camera_data.sensor_fit = 'AUTO'
camera_data.clip_start = 0.1
camera_data.clip_end = 100
camera = bpy.data.objects.new('Camera', camera_data)
collection.objects.link(camera)
scene.camera = camera
# The canvas centre sits this far from the floor origin, so the origin lands on PIVOT_PX.
centre = ((CANVAS_PX[0] / 2 - PIVOT_PX[0]) / PPU) * RIGHT + ((CANVAS_PX[1] / 2 - PIVOT_PX[1]) / PPU) * UP
camera.location = centre - FORWARD * 20
camera.rotation_euler = (math.pi / 2 - ELEVATION, 0, 0)


def set_facing(degrees):
    RIG.rotation_euler = (0, 0, math.radians(degrees - 270))


def screen_point(solver, rest_point, bone_name):
    """A rest-space point of a bone's geometry, posed, on screen in world units from the floor origin (unmirrored)."""
    world = rig_matrix() @ (solver.G[bone_name] @ rest_point)
    return (round(world.dot(RIGHT), 5), round(world.dot(UP), 5))


def show_cracks(hot):
    for name, obj in PARTS.items():
        if INFO[name].get('hot_only'):
            obj.hide_render = not hot


def render_all():
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    started = time.time()
    for facing, degrees in FACING_SET:
        set_facing(degrees)
        for pose_name in POSE_SET:
            if pose_name in DEATHS and facing != 'front':
                continue     # the remains are one drawing, mirrored by the game
            variants = ['cool'] if pose_name in DEATHS else VARIANT_SET
            for variant in variants:
                pose = pose_for(pose_name, variant, degrees)
                solver = apply(pose)
                set_state(heat=1.0 if variant == 'hot' else 0.0)
                show_cracks(variant == 'hot')
                bpy.context.view_layer.update()
                stem = '%s_%s' % (pose_name, variant)
                for glow_pass, suffix in ((0.0, ''), (1.0, '_glow')):
                    set_state(glow_pass=glow_pass)
                    path = OUT / facing / (stem + suffix + '.png')
                    path.parent.mkdir(parents=True, exist_ok=True)
                    scene.render.filepath = str(path)
                    bpy.ops.render.render(write_still=True)
                set_state(glow_pass=0.0)
                records.append(dict(facing=facing, pose=pose_name, variant=variant,
                                    file=str((OUT / facing / (stem + '.png')).relative_to(OUT)).replace('\\', '/'),
                                    glow=str((OUT / facing / (stem + '_glow.png')).relative_to(OUT)).replace('\\', '/'),
                                    visor=screen_point(solver, HEAD_BASE + Vector((0, -0.17, 0.118)), 'head'),
                                    hammer=screen_point(solver, HAMMER_HEAD_C, 'hammer')))
                print('WARDEN_FRAME', facing, pose_name, variant, '%.1fs' % (time.time() - started), flush=True)
    meta = dict(blender=bpy.app.version_string, ppu=PPU, supersample=SS, canvas=list(CANVAS_PX), pivot=list(PIVOT_PX),
                elevation_degrees=30, height_compensation=HEIGHT, scale=SCALE, stride=round(STRIDE * SCALE, 4),
                glow_scale=GLOW_SCALE,
                facings={f: d for f, d in FACINGS}, poses=POSES, frames=records)
    existing = OUT / 'frames.json'
    if existing.exists() and (wanted_poses or wanted_facings or wanted_variants):
        old = json.loads(existing.read_text())
        keep = {(r['facing'], r['pose'], r['variant']): r for r in old.get('frames', [])}
        for r in records:
            keep[(r['facing'], r['pose'], r['variant'])] = r
        meta['frames'] = list(keep.values())
    existing.write_text(json.dumps(meta, indent=1))


def bake_actions():
    """One action per pose on the rig, so the .blend can be scrubbed; the frame loop itself poses directly."""
    RIG.animation_data_create()
    for k, name in enumerate(POSES):
        action = bpy.data.actions.new('Warden ' + name)
        action.use_fake_user = True
        RIG.animation_data.action = action
        apply(pose_for(name, 'cool'))
        for pb in RIG.pose.bones:
            pb.keyframe_insert('location', frame=1)
            pb.keyframe_insert('rotation_quaternion', frame=1)
            pb.keyframe_insert('scale', frame=1)
    RIG.animation_data.action = bpy.data.actions['Warden idle']


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
set_facing(FACINGS[0][1])
apply(idle('cool'))
set_state(heat=0.0, glow_pass=0.0)
show_cracks(False)      # the saved scene shows the first phase; set Heat to 1 and unhide the cracks for the second
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND), compress=True)
v, t = budget()
print('WARDEN_COMPLETE', 'parts', len(PARTS), 'vertices', v, 'triangles', t, flush=True)
