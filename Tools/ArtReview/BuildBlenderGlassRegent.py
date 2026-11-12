"""The Glass Regent, the Quicksilver Vaults' gate boss (docs/44 section 1.8, docs/50 section 6): a reproducible stylised
model, rig, poses and the sprite render for its EnemyArtSet and its copies' set.

Run with Blender 4.5 LTS, headless:

    blender --background --python Tools/ArtReview/BuildBlenderGlassRegent.py -- [options]

Options after the "--":
    --out DIR        raw frames and frames.json (default TestResults/glass-regent-render, git-ignored)
    --blend PATH     the saved scene (default ArtDirection/2026-11-12/glass-regent-01/glass-regent.blend)
    --poses LIST     a comma-separated subset of idle,step-a,step-b,strike,pass-a,pass-b,windup,windup-step-a,
                     windup-pass-a,windup-pass-b,fall-struck,fall-knee,fall-topple,fall-down,shards-burst,shards-heap
    --facings LIST   a comma-separated subset of front,rear
    --variants LIST  cool (the first phase), hot (the second: cracks in the glass, the heart brighter), copy (the glass
                     copies of the second phase: no crown, no heart, no eyes - PackGlassRegentSprites.py pales them)
    --samples N      Cycles samples per render pixel (default 10)
    --no-render      build and save the .blend, render nothing

Who it is: the archivist who, wanting to protect identities, came to value the copies above their owners. A tall figure
of liquid mirror in an archivist's robe, crowned in glass, a violet heart set in its chest, a long silver rod tipped
with a glass crystal in its right hand. Born on floor 2 in the ice palette (docs/49): quicksilver chrome, a mid violet
robe with silver trims, glass that glows the floor's enemy violet #8B6CE8. Nothing on it is red-orange: that colour is
the lane's alone (docs/50 section 2 rule 3).

The read at phone scale (docs/50 section 6 "Readability"): the crown - a ring of glass spikes, tallest at the front -
over a bright mirror head and a tall flared collar, a wide capelet, a long dark-violet robe, and the rod's crystal
standing beside the crown. The copies keep the robe and the collar but lose the crown, the heart and the eyes: a shape
difference, not only a colour.

Nothing here is generated imagery: the geometry is lofted rings and primitives, every part rides one bone rigidly (the
robe's panels are stiff and ride follower bones), the shading is one authored toon node group with a chrome
reflection band for the mirror, and every frame is a pose computed by the functions below. One model renders every
frame, both facings, both phases and the copies, so details cannot drift between them. The rig, the camera, the solver,
the contract and the frame loop are BuildBlenderWarden.py's, so the Regent's sprites keep the Forge Warden's contract.

Camera: orthographic, looking north and down 30 degrees, the game's 2:1 floor (ArenaFloor.DepthScale), as
BuildBlenderWarden.py. Heights are modelled in design units and the rig is stretched by 1/cos(30), so one design unit of
height is one world unit on screen. Both facings look left (the game mirrors them for the right); the camera and the key
light never move. The root stays on the floor origin, which projects to the same render pixel in every frame: the pivot.

Each pose renders twice: the beauty, in which the glass parts show only their unlit glass, and an emission pass in which
every glowing part writes its glow into its own channel and everything else is black - red the heart, green the crown,
the eyes, the rod's crystal and the collar's shards, blue the second phase's cracks. PackGlassRegentSprites.py colours
each channel and adds it with its own gain, so each group's value is tuned in the pack, without rendering again.
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


OUT = Path(option('--out', str(ROOT / 'TestResults/glass-regent-render')))
BLEND = Path(option('--blend', str(ROOT / 'ArtDirection/2026-11-12/glass-regent-01/glass-regent.blend')))
SAMPLES = int(option('--samples', 10))
RENDER = not option('--no-render', False)

# ---- The sprite contract (PackGlassRegentSprites.py reads frames.json); the Warden's, with a taller canvas ----
PPU = 128                      # final texels per world unit: the painted Grunt's and the Warden's 128
SS = 4                         # render at 4x and box-filter down: area-exact anti-aliasing, the pivot on a corner
CANVAS_PX = (400, 480)         # the render canvas in final texels: room for the rod raised over the crown
PIVOT_PX = (200, 56)           # the floor origin in final texels from the canvas's bottom-left corner
# The fall's canvas (and the copies' shattered remains): wider and shallower, for a body lying at full length.
FALL_CANVAS_PX = (608, 400)
FALL_PIVOT_PX = (304, 120)
ELEVATION = math.radians(30)
HEIGHT = 1 / math.cos(ELEVATION)
# Both facings look left; degrees as BuildBlenderWarden.py (270 faces the camera, 180 screen left, 90 away).
FACINGS = [('front', 234), ('rear', 130)]
SCALE = 1.0
# Floor units one walk cycle of two measured steps covers: a longer leg than the Warden's, a statelier stride.
STRIDE = 1.0
WINDUP_WALK = {'windup-step-a': 0.0, 'windup-pass-a': 0.25, 'windup-step-b': 0.5, 'windup-pass-b': 0.75}
FALLS = ['fall-struck', 'fall-knee', 'fall-topple', 'fall-down']
SHARED_FALLS = {'fall-topple', 'fall-down'}
# A copy shatters (docs/50 section 6): its two remains are a burst of glass and the heap it leaves. Front only (the
# game mirrors the remains).
SHARDS = ['shards-burst', 'shards-heap']
POSES = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b', 'windup', *WINDUP_WALK, *FALLS, *SHARDS]
VARIANTS = ['cool', 'hot', 'copy']
GLOW_SCALE = 0.5               # the emission pass writes each mask at half, so a flared mask never clips the 8 bits
CHANNELS = {'heart': (1, 0, 0), 'crown': (0, 1, 0), 'crack': (0, 0, 1)}

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


class Random:
    """A small deterministic generator, so the shards fall the same way on every run."""

    def __init__(self, seed):
        self.state = seed

    def next(self):
        self.state = (self.state * 1103515245 + 12345) % 2147483648
        return self.state / 2147483648

    def range(self, a, b):
        return a + (b - a) * self.next()


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
# A cool rim from behind the upper left, so the far edge of the violet robe separates from the slate floor.
RIM = (-0.62 * RIGHT + 0.58 * UP - 0.52 * TOWARD).normalized()
# A frost-light rim from behind, low on the right: the floor's braziers (docs/49 #A6F0FF), opposite the key.
FROST_RIM = (0.58 * RIGHT - 0.25 * UP - 0.78 * TOWARD).normalized()
HALF = (KEY + TOWARD).normalized()


# ---------------------------------------------------------------------------------------------------------------------
# State shared by every material: the phase, the windup's flare, death's dimming, and which pass renders
# ---------------------------------------------------------------------------------------------------------------------
def build_state_group():
    ng = bpy.data.node_groups.new('Regent state', 'ShaderNodeTree')
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
# Shading: one toon group, fed by authored palettes, with a chrome band for the liquid mirror
# ---------------------------------------------------------------------------------------------------------------------
def build_toon_group():
    ng = bpy.data.node_groups.new('Regent toon', 'ShaderNodeTree')
    io = ng.interface
    for name, kind, default in [
            ('Light', 'NodeSocketColor', (1, 1, 1, 1)), ('Mid', 'NodeSocketColor', (.5, .5, .5, 1)),
            ('Shadow', 'NodeSocketColor', (.2, .2, .2, 1)), ('Deep', 'NodeSocketColor', (.05, .05, .05, 1)),
            ('Spec', 'NodeSocketFloat', 0.0), ('Spec Color', 'NodeSocketColor', (1, 1, 1, 1)),
            ('Rim', 'NodeSocketFloat', 0.35), ('Occlusion', 'NodeSocketFloat', 1.0),
            ('Band Shift', 'NodeSocketFloat', 0.0), ('Glint', 'NodeSocketFloat', 0.0),
            ('Facet', 'NodeSocketFloat', 0.5), ('Chrome', 'NodeSocketFloat', 0.0)]:
        socket = io.new_socket(name, in_out='INPUT', socket_type=kind)
        socket.default_value = default
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

    def scale(colour, factor):
        m = n.new('ShaderNodeVectorMath')
        m.operation = 'SCALE'
        l.new(colour, m.inputs[0])
        l.new(factor, m.inputs['Scale'])
        return m.outputs[0]

    normal = geo.outputs['Normal']
    half_lambert = math_node('MULTIPLY_ADD', dot(normal, vec(KEY)), 0.5, 0.5)
    facet = math_node('MULTIPLY', math_node('SUBTRACT', gi.outputs['Facet'], 0.5), 0.09)
    lit = math_node('ADD', math_node('ADD', half_lambert, gi.outputs['Band Shift']), facet)
    # Painted value steps with clean transitions: shadow, the local colour, the lit plane.
    first = step(lit, 0.36, 0.5)
    second = step(lit, 0.73, 0.85)
    colour = mix(first, gi.outputs['Shadow'], gi.outputs['Mid'])
    colour = mix(second, colour, gi.outputs['Light'])
    coords = n.new('ShaderNodeTexCoord')
    brush = n.new('ShaderNodeTexNoise')
    brush.inputs['Scale'].default_value = 6.0
    brush.inputs['Detail'].default_value = 2.0
    l.new(coords.outputs['Generated'], brush.inputs['Vector'])
    drift = n.new('ShaderNodeCombineColor')
    for channel, spread in enumerate((0.2, 0.2, 0.22)):
        l.new(math_node('MULTIPLY_ADD', brush.outputs['Fac'], spread, 1 - spread / 2), drift.inputs[channel])
    paint = n.new('ShaderNodeMixRGB')
    paint.blend_type = 'MULTIPLY'
    paint.inputs['Fac'].default_value = 1.0
    l.new(colour, paint.inputs['Color1'])
    l.new(drift.outputs[0], paint.inputs['Color2'])
    colour = paint.outputs['Color']
    # The liquid mirror: a painted chrome band by how far a surface turns to the screen's up - the vault's bright frost
    # sky above a dark horizon line, the slate floor's violet-grey reflection below - shaded down on the side away
    # from the key, so the mirror still has a light and a dark side.
    ramp = n.new('ShaderNodeValToRGB')
    elements = ramp.color_ramp.elements
    stops = [(0.0, '#2B2F42'), (0.36, '#4C4C6E'), (0.44, '#141622'), (0.49, '#141622'), (0.53, '#E3EAF7'),
             (0.78, '#A3AEC6'), (1.0, '#CBD4E6')]
    elements[0].position, elements[0].color = stops[0][0], lin(stops[0][1])
    elements[1].position, elements[1].color = stops[-1][0], lin(stops[-1][1])
    for position, hexstr in stops[1:-1]:
        e = elements.new(position)
        e.color = lin(hexstr)
    l.new(math_node('MULTIPLY_ADD', dot(normal, vec(UP)), 0.5, 0.5), ramp.inputs['Fac'])
    chrome = scale(ramp.outputs['Color'], math_node('MULTIPLY_ADD', first, 0.45, 0.55))
    colour = mix(gi.outputs['Chrome'], colour, chrome)
    # Baked contact occlusion pulls hard toward the deep colour: under the collar, between the robe's panels.
    closed = math_node('SUBTRACT', 1.0, step(gi.outputs['Occlusion'], 0.3, 0.97))
    colour = mix(closed, colour, gi.outputs['Deep'])
    # The floor's enemy violet bounced up from below (docs/49): surfaces turned down catch a little of it.
    life = state.outputs['Life']
    bounce = math_node('MULTIPLY', step(dot(normal, vec(Vector((0, 0, -1)))), 0.2, 0.8), 0.1)
    bounce = math_node('MULTIPLY', bounce, life)
    colour = add_colour(colour, rgb('#8B6CE8'), bounce)
    # A cool rim on edges turned to the upper left and away.
    facing = n.new('ShaderNodeLayerWeight')
    facing.inputs['Blend'].default_value = 0.4
    rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.5, 0.82), math_node('MAXIMUM', dot(normal, vec(RIM)), 0.0))
    rim = math_node('MULTIPLY', rim, gi.outputs['Rim'])
    colour = add_colour(colour, rgb('#B9CCF2'), math_node('MULTIPLY', rim, 0.55))
    # The frost-light's rim from behind on the lower right: only the last sliver of the silhouette.
    frost_rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.66, 0.9),
                          math_node('MAXIMUM', dot(normal, vec(FROST_RIM)), 0.0))
    colour = add_colour(colour, rgb('#A6F0FF'), math_node('MULTIPLY', frost_rim, 0.45))
    half = math_node('MAXIMUM', dot(normal, vec(HALF)), 0.0)
    spec = math_node('MULTIPLY', step(math_node('POWER', half, 36.0), 0.35, 0.55), gi.outputs['Spec'])
    colour = add_colour(colour, gi.outputs['Spec Color'], spec)
    glint = math_node('MULTIPLY', step(math_node('POWER', half, 180.0), 0.4, 0.7), gi.outputs['Glint'])
    colour = add_colour(colour, rgb('#FFFFFF'), glint)
    l.new(colour, go.inputs['Color'])
    return ng


TOON = build_toon_group()

PALETTES = {
    # light, mid, shadow, deep.
    # The liquid mirror under the chrome band: cold, lighter than the slate floor so the head and hands read first.
    'mirror': ('#F1F4FA', '#A7B1C4', '#596075', '#1B1E2B'),
    # Silver: the trims, the circlet, the rod's shaft (docs/49 silver #A9B4BF as the mid).
    'silver': ('#F4F6FA', '#A9B4BF', '#5A626F', '#21252D'),
    # The archivist's robe: a deep indigo-violet, a step darker than the slate floor and far from its hue (docs/49
    # rule 3), so the silver, the mirror head and the glowing crown stand out over a dark column.
    'robe': ('#7C6FC2', '#473D82', '#2A2452', '#120E26'),
    'robe-dark': ('#4E4580', '#30295A', '#1E1A3A', '#0D0A1C'),
    # The glass, unlit: the pack lights it from the emission pass. Light enough to read as glass with no glow at all.
    'glass': ('#F2EEFF', '#B6A6EE', '#6957B6', '#271E59'),
    'heart': ('#C3B2FF', '#7258CC', '#3D2C86', '#1A1242'),
}
OUTLINE = '#120D14'


class Shading:
    """Builds a material: toon colours per palette, emitted once. The material shades itself; the scene has no lights.
    In the emission pass every material emits black except the glowing ones, which write their mask into their
    channel (CHANNELS), scaled by GLOW_SCALE."""

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
             chrome=0.0):
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
        g.inputs['Chrome'].default_value = chrome
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

    def finish(self, colour, mask=None, channel=None):
        """Emits colour in the beauty pass. mask: the glow's strength (0..1, a flare above 1) the emission pass writes
        into the channel, or None for black."""
        if mask is not None:
            r, g, b = CHANNELS[channel]
            node = self.nodes.new('ShaderNodeCombineColor')
            for i, on in enumerate((r, g, b)):
                if on:
                    self.links.new(self.math('MULTIPLY', mask, GLOW_SCALE), node.inputs[i])
                else:
                    node.inputs[i].default_value = 0.0
            pass_colour = node.outputs[0]
        else:
            pass_colour = self.rgb('#000000')
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


def glass_material(name, palette, channel, flares=True, noise_scale=16.0):
    """Glowing glass: its unlit toon glass in the beauty pass, and in the emission pass a noisy mask in its channel,
    raised by the windup's Flare and dimmed by death's Life (squared, as the Warden's furnace cools)."""
    s = Shading(name)
    colour = s.toon(palette, spec=1.1, spec_color='#FFFFFF', glint=1.5, rim=0.6)
    mask = s.math('MULTIPLY_ADD', s.noise(noise_scale, 1.5), 0.45, 0.55)
    if flares:
        mask = s.math('MULTIPLY', mask, s.state('Flare'))
    life = s.state('Life')
    mask = s.math('MULTIPLY', mask, s.math('MULTIPLY', life, life))
    return s.finish(colour, mask=mask, channel=channel)


def crack_material():
    """The second phase's cracks: pale violet light in the beauty pass, the crack channel in the emission pass."""
    s = Shading('Glass crack')
    colour = s.rgb('#D7CCFF')
    life = s.state('Life')
    return s.finish(colour, mask=s.math('MULTIPLY', life, 1.0), channel='crack')


MATERIALS = {}


def build_materials():
    m = MATERIALS
    m['outline'] = outline_material()
    m['mirror'] = plain('Liquid mirror', 'mirror', spec=0.9, spec_color='#FFFFFF', glint=1.2, rim=0.55, chrome=0.72)
    m['silver'] = plain('Silver trim', 'silver', spec=1.0, spec_color='#FFFFFF', glint=1.2, rim=0.5, chrome=0.4)
    m['robe'] = plain('Archivist robe', 'robe', spec=0.1, spec_color='#C9C0FF', rim=0.45)
    m['robe-dark'] = plain('Robe lining', 'robe-dark', spec=0.05, rim=0.3)
    m['glass'] = plain('Plain glass', 'glass', spec=1.1, spec_color='#FFFFFF', glint=1.5, rim=0.6)
    m['heart'] = glass_material('Glass heart', 'heart', 'heart', noise_scale=9.0)
    m['crown'] = glass_material('Glass crown', 'glass', 'crown')
    m['crack'] = crack_material()


build_materials()


# ---------------------------------------------------------------------------------------------------------------------
# Geometry: low-poly parts, flat-shaded and chamfered (the mirror's rounded parts smooth-shaded, liquid)
# ---------------------------------------------------------------------------------------------------------------------
PARTS = {}      # name -> object
INFO = {}       # name -> dict(bone, material, contour, chamfer, occluder, show)


def new_object(name, verts, faces, bone, material, contour=True, chamfer=0.006, occluder=False, flat=True,
               show='all'):
    """show: 'all', 'regent' (hidden on the copies: the crown, the heart, the eyes, the collar's shards), 'hot' (the
    second phase only: the cracks), or 'burst' / 'heap' (only the copies' shattered remains)."""
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
    PARTS[name] = obj
    INFO[name] = dict(bone=bone, material=material, contour=contour, chamfer=chamfer, occluder=occluder, show=show)
    return obj


def outward(verts, faces, axis_y=0.0):
    """An open plate's faces turned to face away from the body's vertical axis (x = 0, y = axis_y), so its contour hull
    grows outward."""
    out = []
    for f in faces:
        a, b, c = (Vector(verts[i]) for i in f[:3])
        normal = (b - a).cross(c - a)
        centre = sum((Vector(verts[i]) for i in f), Vector()) / len(f)
        radial = Vector((centre.x, centre.y - axis_y, 0))
        out.append(tuple(reversed(f)) if normal.dot(radial) < 0 else f)
    return out


def grid(name, rows, bone, material, axis_y=0.0, **kw):
    """An open plate from rows of points (each the same length)."""
    cols = len(rows[0])
    verts = [p for row in rows for p in row]
    faces = []
    for j in range(len(rows) - 1):
        for i in range(cols - 1):
            faces.append((j * cols + i, j * cols + i + 1, (j + 1) * cols + i + 1, (j + 1) * cols + i))
    return new_object(name, verts, outward(verts, faces, axis_y), bone, material, **kw)


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


def around(degrees, rx, ry, z, y0=0.0):
    """A point round the body's axis, degrees from the front (+ to the Regent's left, +X)."""
    a = -math.pi / 2 + math.radians(degrees)
    return Vector((rx * math.cos(a), y0 + ry * math.sin(a), z))


def frame_points(origin, axis, rings):
    axis = Vector(axis)
    length = axis.length
    d = axis.normalized()
    ref = Vector((0, -1, 0)) if abs(d.y) < 0.9 else Vector((1, 0, 0))
    u = (ref - d * ref.dot(d)).normalized()
    v = d.cross(u)
    out = []
    for t, rx, ry, *rest in rings:
        count = rest[0] if rest else 8
        c = Vector(origin) + d * (t * length)
        out.append([c + u * (ry * math.cos(2 * math.pi * k / count)) + v * (rx * math.sin(2 * math.pi * k / count))
                    for k in range(count)])
    return out


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


def tube(name, path, radii, bone, material, count=8, cap_start=True, cap_end=True, twist_ref=Vector((0, -1, 0)), **kw):
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


def torus(name, centre, axes, radii, wire, bone, material, count=14, wire_count=6, **kw):
    u, v, nrm = axes
    rings = []
    for i in range(count):
        a = 2 * math.pi * i / count
        p = Vector(centre) + u * (radii[0] * math.cos(a)) + v * (radii[1] * math.sin(a))
        tangent = (-u * (radii[0] * math.sin(a)) + v * (radii[1] * math.cos(a))).normalized()
        out = (p - Vector(centre)).normalized()
        out = (out - tangent * out.dot(tangent)).normalized()
        rings.append([p + out * (wire * math.cos(2 * math.pi * k / wire_count)) + nrm * (wire * math.sin(2 * math.pi * k / wire_count))
                      for k in range(wire_count)])
    n = wire_count
    verts = [p for ring in rings for p in ring]
    faces = []
    for j in range(count):
        j1 = (j + 1) % count
        for i in range(n):
            i1 = (i + 1) % n
            faces.append((j * n + i, j * n + i1, j1 * n + i1, j1 * n + i))
    return new_object(name, verts, faces, bone, material, **kw)


def crystal(name, base, axis, length, radius, bone, material, sides=4, foot=0.18, twist=0.0, **kw):
    """A glass shard: a prism ring a fifth of the way up, a long point above it and a short one below."""
    a = Vector(axis).normalized()
    ref = Vector((0, -1, 0)) if abs(a.y) < 0.9 else Vector((1, 0, 0))
    u = (ref - a * ref.dot(a)).normalized()
    v = a.cross(u)
    ring_c = Vector(base) + a * (length * 0.2)
    verts = [ring_c + (u * math.cos(twist + 2 * math.pi * k / sides) + v * math.sin(twist + 2 * math.pi * k / sides)) * radius
             for k in range(sides)]
    verts.append(Vector(base) + a * length)
    verts.append(Vector(base) - a * (length * foot))
    top, bottom = sides, sides + 1
    faces = []
    for k in range(sides):
        k1 = (k + 1) % sides
        faces.append((k, k1, top))
        faces.append((k1, k, bottom))
    return new_object(name, verts, faces, bone, material, **kw)


def crack(name, points, bone, radius=0.006):
    """A crack that opens only in the second phase: the glass splitting, violet-white light inside."""
    tube(name, points, [radius * (1.0 if 0 < k < len(points) - 1 else 0.5) for k in range(len(points))], bone, 'crack',
         count=5, contour=False, chamfer=0.0, show='hot')


# ---- Measurements (design units, feet on z = 0, facing -Y, the Regent's left is +X) ----
# Taller and slimmer than the Warden: long legs under a robe to the floor, a narrow chest, a long neck, an egg of a
# mirror head, the crown above it, the rod's crystal at the crown's height.
ANKLE_Z, KNEE_Z, HIP_Z = 0.1, 0.48, 0.87
HIP_X = 0.1
WAIST_Z = 0.98
CHEST_Z = 1.16
NECK_Z = 1.44
SHOULDER = Vector((0.24, 0.02, 1.39))
ELBOW = Vector((0.31, 0.06, 1.13))
WRIST = Vector((0.335, 0.02, 0.9))
FIST = Vector((0.34, 0.0, 0.835))
HEAD_BASE = Vector((0, -0.01, 1.47))
HEAD_C = Vector((0, -0.012, 1.605))
HEAD_R = (0.098, 0.106, 0.146)
CROWN_Z = 1.69
ROD_REST = Vector((-0.8, 0.0, 0.0))      # the rod's butt, the rod standing upright out of the way at rest
HAFT = 1.68                              # butt to the crystal's centre
CRYSTAL_LEN = 0.36
ROBE_TOP = 0.97


def mirror_x(v):
    return Vector((-v.x, v.y, v.z))


def side_point(v, side):
    return v if side > 0 else mirror_x(v)


def build_legs():
    """Mirror legs, mostly under the robe: a long shin and a pointed foot step out from under the hem."""
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        x = HIP_X * side
        rings = []
        for y, w, zc, hz in [(0.07, 0.05, 0.05, 0.045), (0.02, 0.062, 0.058, 0.055), (-0.06, 0.066, 0.05, 0.048),
                             (-0.15, 0.056, 0.034, 0.032), (-0.23, 0.03, 0.018, 0.016)]:
            rings.append([Vector((x + w * math.cos(a), y, max(0.004, zc + hz * math.sin(a))))
                          for a in [2 * math.pi * i / 8 for i in range(8)]])
        loft('Foot ' + s, rings, 'foot.' + s, 'mirror', cap_start=True, cap_end=True, chamfer=0.0, flat=False)
        shin = [(0.0, 0.05, 0.05, 8), (0.4, 0.062, 0.064, 8), (1.0, 0.068, 0.068, 8)]
        loft('Shin ' + s, frame_points(Vector((x, 0.004, ANKLE_Z)), Vector((0, -0.004, KNEE_Z - ANKLE_Z)), shin),
             'shin.' + s, 'mirror', cap_start=True, cap_end=True, occluder=True, chamfer=0.0, flat=False)
        ellipsoid('Knee ' + s, Vector((x, -0.03, KNEE_Z)), (0.07, 0.07, 0.07), 'shin.' + s, 'mirror', rings=4, segments=8,
                  chamfer=0.0, flat=False)
        thigh = [(0.0, 0.07, 0.07, 8), (1.0, 0.085, 0.085, 8)]
        loft('Thigh ' + s, frame_points(Vector((x, 0.0, KNEE_Z + 0.03)), Vector((0, 0, HIP_Z - KNEE_Z - 0.03)), thigh),
             'thigh.' + s, 'mirror', cap_start=True, cap_end=True, chamfer=0.0, flat=False)


# The mirror chest's rings: z, radius across, radius deep, the keel's push. Narrow, a little proud at the sternum.
CHEST_RINGS = [(0.97, 0.175, 0.14, 0.01), (1.07, 0.195, 0.148, 0.02), (1.18, 0.222, 0.158, 0.03),
               (1.29, 0.23, 0.158, 0.03), (1.37, 0.196, 0.142, 0.015), (1.45, 0.1, 0.09, 0.0)]
HEART_C = Vector((0, -0.178, 1.25))


def chest_point(z, x_across, lift=0.004):
    """The point on the mirror chest's front at height z where it is x_across from the keel."""
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


def bodice_gap(z):
    """Half the front opening of the robe's bodice, degrees: a deep V that shows the mirror chest and the heart."""
    return 5 + 34 * smooth(1.0, 1.43, z)


def build_torso():
    loft('Chest', [ring_points(0, 0.0, z, rx, ry, 16, keel=k, back=0.01) for z, rx, ry, k in CHEST_RINGS], 'chest',
         'mirror', cap_start=True, cap_end=True, occluder=True, chamfer=0.0, flat=False)
    loft('Neck', [ring_points(0, -0.005, z, 0.058, 0.056, 10) for z in (1.4, 1.52)], 'neck', 'mirror',
         cap_start=True, cap_end=True, chamfer=0.0, flat=False)
    # The bodice: the robe over the chest, open down the front in a V; its edges trimmed in silver.
    rows = []
    for z, rx, ry, _ in CHEST_RINGS[:-1] + [(1.44, 0.135, 0.115, 0.0)]:
        g = bodice_gap(z)
        rows.append([around(lerp(g, 360 - g, i / 15), rx + 0.016, ry + 0.016, z) for i in range(16)])
    grid('Bodice', rows, 'chest', 'robe', occluder=True, chamfer=0.0, flat=False)
    for side in (-1, 1):
        edge = []
        for z, rx, ry, _ in CHEST_RINGS[:-1] + [(1.44, 0.135, 0.115, 0.0)]:
            edge.append(around(side * bodice_gap(z), rx + 0.022, ry + 0.022, z))
        tube('Bodice trim ' + ('L' if side > 0 else 'R'), edge, [0.011] * len(edge), 'chest', 'silver', count=6,
             chamfer=0.0)
    # The heart: a faceted violet crystal set in the sternum, in a silver setting that the copies keep empty.
    ellipsoid('Heart', HEART_C, (0.052, 0.04, 0.082), 'chest', 'heart', rings=2, segments=6, contour=False,
              chamfer=0.0, show='regent')
    torus('Heart setting', HEART_C + Vector((0, 0.008, 0)), (Vector((1, 0, 0)), Vector((0, 0, 1)), Vector((0, -1, 0))),
          (0.066, 0.098), 0.011, 'chest', 'silver', count=12, wire_count=5, chamfer=0.0)
    # The girdle: a silver band at the waist with a clasp, the robe's skirt hanging from it.
    loft('Girdle', [ring_points(0, 0.0, z, 0.212, 0.172, 16) for z in (0.95, 1.005)], 'spine', 'silver', occluder=True,
         chamfer=0.0)
    box('Clasp', Vector((0, -0.18, 0.977)), (0.07, 0.03, 0.08), 'spine', 'silver', chamfer=0.008)


# The robe's skirt in six stiff panels round the hips, each riding a follower of its thigh, a split down the front
# that widens to the hem so the stepping legs show, and a shallow point at each panel's hem: a robe of panes.
PANELS = [('front', 'L', 4, 15, 52), ('side', 'L', 50, 50, 114), ('back', 'L', 110, 110, 181),
          ('front', 'R', -4, -15, -52), ('side', 'R', -50, -50, -114), ('back', 'R', -110, -110, -181)]
HEM = {'front': 0.15, 'side': 0.1, 'back': 0.06}


def build_robe():
    loft('Lining', [ring_points(0, 0.01, z, rx, ry, 14) for z, rx, ry in ((0.97, 0.2, 0.16), (0.7, 0.24, 0.2),
                                                                           (0.44, 0.27, 0.23))],
         'pelvis', 'robe-dark', occluder=True, chamfer=0.0, flat=False)
    for kind, s, inner_top, inner_hem, outer in PANELS:
        rows = []
        cols = 6
        for j in range(8):
            t = j / 7
            inner = lerp(inner_top, inner_hem, t)
            row = []
            for i in range(cols):
                u = i / (cols - 1)
                deg = lerp(inner, outer, u)
                tip = 1 - abs(2 * u - 1)
                hem = HEM[kind] - 0.05 * tip
                z = lerp(ROBE_TOP, hem, t)
                rx = lerp(0.198, 0.37, t ** 0.85)
                ry = lerp(0.158, 0.33, t ** 0.85)
                p = around(deg, rx, ry, z, 0.01)
                if abs(deg) > 90:
                    p.y += 0.04 * t            # the back hangs out a little behind the stride
                row.append(p)
            rows.append(row)
        bone_name = 'robe_%s.%s' % (kind, s)
        grid('Robe %s %s' % (kind, s), rows, bone_name, 'robe', occluder=True, chamfer=0.0, flat=False)
        hem_row = [p + Vector((p.x, p.y, 0)).normalized() * 0.004 for p in rows[-1]]
        tube('Robe hem %s %s' % (kind, s), hem_row, [0.012] * len(hem_row), bone_name, 'silver', count=6, chamfer=0.0)
        if kind == 'front':
            edge = [row[0] + Vector((row[0].x, row[0].y, 0)).normalized() * 0.004 for row in rows]
            tube('Robe edge ' + s, edge, [0.011] * len(edge), bone_name, 'silver', count=6, chamfer=0.0)


def collar_point(degrees, t, lift=0.0):
    """A point on the collar: degrees from the front (the collar spans 62..298 through the back), t up it (0..1)."""
    back = math.cos(math.radians(degrees - 180)) * 0.5 + 0.5          # 1 at the back centre, 0 at the front
    top = lerp(1.58, 1.71, back ** 1.5)
    z = lerp(1.41, top, t)
    rx = lerp(0.125, 0.26, t ** 1.2) + lift
    ry = lerp(0.105, 0.2, t ** 1.2) + lift
    return around(degrees, rx, ry, z, 0.015 + 0.05 * t * back)


def build_collar_and_mantle():
    """The archivist's high collar of mirror flaring up behind the head, its rim in short points, and a capelet over
    the shoulders in two halves whose hems end in points: the wide, tall shoulder line of a ruler."""
    cols = 21
    rows = []
    for j in range(6):
        t = j / 5
        row = []
        for i in range(cols):
            deg = lerp(62, 298, i / (cols - 1))
            p = collar_point(deg, t)
            if j == 5 and i % 2 == 1:
                p.z += 0.045                     # the rim's points
            row.append(p)
        rows.append(row)
    grid('Collar', rows, 'chest', 'mirror', axis_y=0.015, occluder=True, chamfer=0.0)
    # The capelet: from the collar's foot out over the shoulders, down to above the elbows, pointed at the hem.
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        rows = []
        cols = 9
        for j in range(6):
            t = j / 5
            row = []
            for i in range(cols):
                deg = side * lerp(24, 181, i / (cols - 1))
                z = lerp(1.49, 1.19, t)
                if j == 5:
                    z -= 0.065 * (i % 2)
                rx = lerp(0.165, 0.4, t ** 0.75)
                ry = lerp(0.135, 0.3, t ** 0.75)
                row.append(around(deg, rx, ry, z, 0.02))
            rows.append(row)
        grid('Capelet ' + s, rows, 'mantle.' + s, 'robe', axis_y=0.02, occluder=True, chamfer=0.0, flat=False)
        hem = [p + Vector((p.x, p.y - 0.02, 0)).normalized() * 0.004 for p in rows[-1]]
        tube('Capelet hem ' + s, hem, [0.012] * len(hem), 'mantle.' + s, 'silver', count=6, chamfer=0.0)
    # Three glass shards at the collar's back, the crown's echo from behind: the copies lose them with the crown.
    for k, (deg, h) in enumerate(((180, 0.2), (152, 0.15), (208, 0.15))):
        base = collar_point(deg, 0.9, -0.01)
        axis = Vector((base.x * 1.6, 0.32, 1.0))          # leaning out and back, away from the head
        crystal('Collar shard %d' % k, base, axis, h, 0.024, 'chest', 'crown', sides=4, twist=0.6, show='regent')


def head_point(degrees, z, lift=0.004):
    """A point on the mirror head's surface at height z, degrees round from the front."""
    dz = (z - HEAD_C.z) / HEAD_R[2]
    k = math.sqrt(max(0.0, 1 - dz * dz))
    return around(degrees, HEAD_R[0] * k + lift, HEAD_R[1] * k + lift, z, HEAD_C.y)


def build_head():
    """An egg of liquid mirror with no face but two thin violet eye-slits, a silver circlet and the glass crown: nine
    spikes, tallest at the front, leaning out."""
    ellipsoid('Head', HEAD_C, HEAD_R, 'head', 'mirror', rings=8, segments=14, occluder=True, chamfer=0.0, flat=False)
    for side in (-1, 1):
        p = head_point(side * 26, 1.612, 0.002)
        ellipsoid('Eye ' + ('L' if side > 0 else 'R'), p, (0.026, 0.012, 0.0085), 'head', 'crown', rings=2, segments=6,
                  contour=False, chamfer=0.0, show='regent')
    rx, ry = abs(head_point(90, CROWN_Z, 0.012).x), abs(head_point(0, CROWN_Z, 0.012).y - HEAD_C.y)
    torus('Circlet', Vector((0, HEAD_C.y, CROWN_Z)), (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))),
          (rx, ry), 0.016, 'head', 'silver', count=16, wire_count=6, chamfer=0.0, show='regent')
    for k in range(9):
        deg = -160 + 40 * k
        front = math.cos(math.radians(deg / 2)) ** 2           # 1 at the front, 0 at the back
        h = 0.11 + 0.22 * front ** 1.6
        base = around(deg, rx, ry, CROWN_Z + 0.006, HEAD_C.y)
        out = Vector((base.x, base.y - HEAD_C.y, 0)).normalized()
        crystal('Crown spike %d' % k, base, out * 0.3 + Vector((0, 0, 1)), h, 0.024 + 0.008 * front, 'head', 'crown',
                sides=4, twist=0.78, show='regent')


def build_arms():
    """Long bell sleeves of the robe over mirror arms; mirror hands."""
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
        loft('Sleeve upper ' + s, frame_points(sh, el - sh, [(0.0, 0.07, 0.07, 8), (1.0, 0.07, 0.07, 8)]),
             'upper_arm.' + s, 'robe', cap_start=True, cap_end=True, occluder=True, chamfer=0.0, flat=False)
        loft('Sleeve ' + s, frame_points(el, wr - el, [(-0.08, 0.072, 0.072, 10), (0.55, 0.088, 0.088, 10),
                                                      (1.0, 0.122, 0.118, 10)]),
             'forearm.' + s, 'robe', cap_start=True, cap_end=False, occluder=True, chamfer=0.0, flat=False)
        d = (wr - el).normalized()
        u = Vector((1, 0, 0)) - d * d.x
        u.normalize()
        torus('Cuff ' + s, wr - d * 0.004, (u, d.cross(u).normalized(), d), (0.12, 0.116), 0.012, 'forearm.' + s,
              'silver', count=12, wire_count=5, chamfer=0.0)
        loft('Wrist ' + s, frame_points(wr - d * 0.05, fi - wr + d * 0.05, [(0.0, 0.035, 0.035, 8), (1.0, 0.04, 0.04, 8)]),
             'hand.' + s, 'mirror', cap_start=True, cap_end=True, chamfer=0.0, flat=False)
        hd = (fi - wr).normalized()
        ellipsoid('Hand ' + s, fi + hd * 0.01, (0.044, 0.04, 0.062), 'hand.' + s, 'mirror', rings=4, segments=8,
                  chamfer=0.0, flat=False)
        ellipsoid('Thumb ' + s, fi + Vector((-0.03 * side, -0.035, 0.012)), (0.018, 0.018, 0.03), 'hand.' + s, 'mirror',
                  rings=3, segments=6, chamfer=0.0, flat=False)


ROD_CRYSTAL_C = Vector()


def build_rod():
    """The Glass Rod (docs/50 section 6): a long silver staff, a butt spike, a grip of the robe's dark cloth, and at
    its head a silver claw of three prongs holding a long glass crystal with two smaller shards. Built upright at its
    rest out of the way: the butt at ROD_REST, the shaft up +Z."""
    global ROD_CRYSTAL_C
    p0 = ROD_REST
    up = Vector((0, 0, 1))
    tube('Rod shaft', [p0 + up * t for t in (0.04, 0.5, 1.0, HAFT - 0.16)], [0.02, 0.021, 0.022, 0.024], 'rod', 'silver',
         count=8, chamfer=0.0)
    tube('Rod butt', [p0 + up * -0.03, p0 + up * 0.02, p0 + up * 0.07], [0.006, 0.026, 0.026], 'rod', 'silver', count=8,
         chamfer=0.0)
    tube('Rod grip', [p0 + up * 0.78, p0 + up * 1.06], [0.028, 0.028], 'rod', 'robe-dark', count=8, chamfer=0.0)
    for t in (0.76, 1.08, HAFT - 0.17):
        tube('Rod ring %.2f' % t, [p0 + up * (t - 0.014), p0 + up * (t + 0.014)], [0.034, 0.034], 'rod', 'silver',
             count=8, chamfer=0.0)
    c = p0 + up * HAFT
    ROD_CRYSTAL_C = c
    crystal('Rod crystal', c - up * (CRYSTAL_LEN * 0.42), up, CRYSTAL_LEN, 0.052, 'rod', 'crown', sides=5, foot=0.3)
    for k, (dx, dy, tilt) in enumerate(((0.05, -0.02, 0.5), (-0.045, 0.03, -0.45))):
        crystal('Rod shard %d' % k, c + Vector((dx, dy, -0.1)), Vector((tilt, 0.1, 1)), 0.15, 0.024, 'rod', 'crown',
                sides=4, twist=0.4)
    for k in range(3):
        a = 2 * math.pi * k / 3 + 0.3
        out = Vector((math.cos(a), math.sin(a), 0))
        path = [c - up * 0.17 + out * 0.03, c - up * 0.1 + out * 0.07, c + out * 0.075, c + up * 0.07 + out * 0.045]
        tube('Rod prong %d' % k, path, [0.012, 0.011, 0.009, 0.005], 'rod', 'silver', count=5, chamfer=0.0)


def build_hot_cracks():
    """The second phase: the glass splits and violet-white light shows through - down the head from the crown, across
    the chest round the heart, up the collar, over the hands. Hidden in the first phase's frames and the copies'."""
    crack('Crack head', [head_point(d, z) for d, z in ((-14, 1.72), (-20, 1.68), (-12, 1.64), (-22, 1.6), (-16, 1.55),
                                                         (-24, 1.51))], 'head', 0.0055)
    crack('Crack head back', [head_point(d, z) for d, z in ((150, 1.72), (160, 1.66), (148, 1.6), (158, 1.54))], 'head',
          0.0055)
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        crack('Crack chest ' + s, [chest_point(z, side * x) for z, x in ((1.26, 0.07), (1.29, 0.1), (1.27, 0.13),
                                                                         (1.31, 0.16))], 'chest', 0.0055)
        crack('Crack collar ' + s, [collar_point(side * d % 360, t, 0.006) for d, t in ((110, 0.1), (118, 0.35),
                                                                                      (108, 0.6), (116, 0.9))],
              'chest', 0.0065)
        fi = side_point(FIST, side)
        crack('Crack hand ' + s, [fi + Vector((0.044 * side, -0.01, z)) for z in (0.05, 0.02, -0.01, -0.04)],
              'hand.' + s, 0.005)
    crack('Crack collar back', [collar_point(d, t, 0.006) for d, t in ((176, 0.15), (186, 0.4), (174, 0.65),
                                                                       (184, 0.95))], 'chest', 0.0065)


def build_shards():
    """A copy's two remains (docs/50: 'a copy shatters with an effect'): a burst of glass in the air over a low heap,
    then the heap it leaves - pale glass on the floor round where it stood. Shown only in the shards poses."""
    rnd = Random(20261112)
    for k in range(16):
        a = rnd.range(0, 2 * math.pi)
        r = rnd.range(0.05, 0.42)
        z = rnd.range(0.12, 1.45) * (1 - r * 0.6)
        axis = Vector((rnd.range(-1, 1), rnd.range(-1, 1), rnd.range(-0.4, 1)))
        crystal('Burst shard %d' % k, Vector((r * math.cos(a), r * math.sin(a), z)), axis, rnd.range(0.07, 0.16),
                rnd.range(0.018, 0.034), 'root', 'glass', sides=4, twist=rnd.range(0, 1.5), show='burst')
    for k in range(18):
        a = rnd.range(0, 2 * math.pi)
        r = rnd.range(0.0, 0.46) ** 0.8
        length = rnd.range(0.07, 0.17)
        flat = Vector((math.cos(a + rnd.range(-1, 1)), math.sin(a + rnd.range(-1, 1)), rnd.range(0.0, 0.35)))
        base = Vector((r * math.cos(a), r * math.sin(a) * 0.9, 0.012))
        for show in ('burst', 'heap'):
            if show == 'burst' and k % 2:
                continue
            crystal('%s floor shard %d' % (show.title(), k), base, flat, length, rnd.range(0.018, 0.032), 'root', 'glass',
                    sides=4, twist=rnd.range(0, 1.5), show=show)
    for show in ('burst', 'heap'):
        ellipsoid('%s mound' % show.title(), Vector((0, 0.01, 0.0)), (0.2, 0.17, 0.07 if show == 'heap' else 0.04),
                  'root', 'glass', rings=2, segments=7, lower=0.0, upper=0.999, show=show)


build_legs()
build_torso()
build_robe()
build_collar_and_mantle()
build_head()
build_arms()
build_rod()
build_hot_cracks()
build_shards()

for name, obj in PARTS.items():
    obj.data.materials.append(MATERIALS[INFO[name]['material']])


# ---------------------------------------------------------------------------------------------------------------------
# Rig: every part rides one bone rigidly; the robe's panels and the capelet ride followers
# ---------------------------------------------------------------------------------------------------------------------
arm_data = bpy.data.armatures.new('Glass Regent rig')
RIG = bpy.data.objects.new('Glass Regent', arm_data)
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
bone('head', tuple(HEAD_BASE), (HEAD_BASE.x, HEAD_BASE.y, 1.76), 'neck')
for side, s in ((1, 'L'), (-1, 'R')):
    sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
    bone('mantle.' + s, sh, sh + Vector((0.1 * side, 0, -0.05)), 'chest')
    bone('upper_arm.' + s, sh, el, 'chest')
    bone('forearm.' + s, el, wr, 'upper_arm.' + s)
    bone('hand.' + s, wr, fi + (fi - wr).normalized() * 0.05, 'forearm.' + s)
    bone('thigh.' + s, (HIP_X * side, 0, HIP_Z), (HIP_X * side, 0, KNEE_Z), 'pelvis')
    bone('shin.' + s, (HIP_X * side, 0, KNEE_Z), (HIP_X * side, 0.004, ANKLE_Z), 'thigh.' + s)
    bone('foot.' + s, (HIP_X * side, 0.004, ANKLE_Z), (HIP_X * side, -0.2, 0.03), 'shin.' + s)
    for kind in ('front', 'side', 'back'):
        bone('robe_%s.%s' % (kind, s), (HIP_X * side, 0, HIP_Z), (HIP_X * side, 0, 0.3), 'pelvis')
bone('rod', tuple(ROD_REST), tuple(ROD_REST + Vector((0, 0, HAFT))), 'root')
bpy.ops.object.mode_set(mode='OBJECT')
REST = {b.name: b.matrix_local.copy() for b in arm_data.bones}
REST_TAIL = {b.name: b.tail_local.copy() for b in arm_data.bones}
PARENT = {b.name: (b.parent.name if b.parent else None) for b in arm_data.bones}
# The robe's front panels go most of the way with their thigh, the side ones less, the back ones least; the capelet
# lifts a little with its arm.
FOLLOWERS = {'mantle.L': ('upper_arm.L', 0.2), 'mantle.R': ('upper_arm.R', 0.2)}
for s in ('L', 'R'):
    FOLLOWERS.update({'robe_front.' + s: ('thigh.' + s, 0.72), 'robe_side.' + s: ('thigh.' + s, 0.42),
                      'robe_back.' + s: ('thigh.' + s, 0.3)})
FREE = {'rod'}


def depth(name):
    d = 0
    while PARENT[name]:
        name = PARENT[name]
        d += 1
    return d


ORDER = sorted(REST, key=lambda b: (depth(b) + (1 if b in FOLLOWERS else 0), b))
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
    """A deterministic random value per face on the flat-shaded parts (the glass, the trims), so each facet is painted
    a little lighter or darker than the next; one value for a whole smooth part (the mirror, the robe's panels), whose
    small faces would otherwise read as a checker."""
    mesh = obj.data
    attr = mesh.attributes.new('facet', 'FLOAT', 'FACE')
    smooth_part = any(poly.use_smooth for poly in mesh.polygons)
    for i in range(len(mesh.polygons)):
        h = math.sin(((0 if smooth_part else i) + 1) * 12.9898 + seed * 78.233) * 43758.5453
        attr.data[i].value = 0.5 + 0.4 * (h - math.floor(h) - 0.5) if smooth_part else h - math.floor(h)


for k, obj in enumerate(PARTS.values()):
    facet_attribute(obj, k)


# ---------------------------------------------------------------------------------------------------------------------
# Posing (BuildBlenderWarden.py's solver): rotations about each bone's head in the rig's rest space, accumulated down
# the hierarchy; the rod placed outright; followers turning part of the way with their leader.
# ---------------------------------------------------------------------------------------------------------------------
def rot(x=0.0, y=0.0, z=0.0):
    return (Matrix.Rotation(math.radians(z), 3, 'Z') @ Matrix.Rotation(math.radians(y), 3, 'Y')
            @ Matrix.Rotation(math.radians(x), 3, 'X'))


class Pose:
    def __init__(self):
        self.rot = {}
        self.offset = Vector()
        self.feet = {}
        self.hands = {}
        self.free = {}
        self.heat_flare = 1.0
        self.life = 1.0
        self.knee_poles = {}
        self.foot_rolls = {}

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


ACROSS = Vector((1, 0, 0))


def rod_placement(grip_target, direction, grip_s, turn=ACROSS):
    """The rod's G: the shaft point grip_s from the butt lands on grip_target, the shaft points along direction (butt
    to crystal)."""
    d = Vector(direction).normalized()
    a = Vector(turn) - d * Vector(turn).dot(d)
    a.normalize()
    r = Matrix((a, d.cross(a), d)).transposed().to_4x4()
    rest_grip = ROD_REST + Vector((0, 0, grip_s))
    return Matrix.Translation(Vector(grip_target)) @ r @ Matrix.Translation(-rest_grip)


def on_rod(p, s):
    """The posed point s along the rod from its butt."""
    return p.free['rod'] @ (ROD_REST + Vector((0, 0, s)))


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
        pole = self.pose.knee_poles.get(s, yawm @ Vector((0, -1, 0.15)))
        knee, ankle, hinge = two_bone(hip, Vector(target), l1, l2, pole)
        rest_pole = Vector((0, -1, 0.15))
        _, _, rest_hinge = two_bone(REST['thigh.' + s].to_translation(), ankle0, l1, l2, rest_pole)
        W_thigh = frame_from(knee - hip, hinge) @ frame_from(knee0 - REST['thigh.' + s].to_translation(), rest_hinge).transposed()
        W_shin = frame_from(ankle - knee, hinge) @ frame_from(ankle0 - knee0, rest_hinge).transposed()
        W_foot = yawm @ rot(x=pitch) if s not in self.pose.foot_rolls else rot(x=pitch, y=self.pose.foot_rolls[s], z=yaw)
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
# The poses
# ---------------------------------------------------------------------------------------------------------------------
def local_floor(x_world, y_world, degrees):
    """A floor offset from the root as the camera sees it (x to screen right, y away) in the rig's own frame for a
    facing."""
    a = math.radians(degrees - 270)
    return Vector(((x_world * math.cos(a) + y_world * math.sin(a)) / SCALE,
                   (-x_world * math.sin(a) + y_world * math.cos(a)) / SCALE, 0))


def stance(breath=0.0):
    """Upright and still, a ruler at an audience: the chest lifted, the head level and a little raised, the feet
    close. The left hand rests open at the hip."""
    p = Pose()
    p.offset = Vector((0, 0, -0.01))
    p.add('pelvis', rot(x=-1))
    p.add('spine', rot(x=-2))
    p.add('chest', rot(x=-3 + breath))
    p.add('neck', rot(x=4))
    p.add('head', rot(x=-4))
    p.feet['L'] = (Vector((0.115, 0.03, ANKLE_Z)), 0, 10)
    p.feet['R'] = (Vector((-0.11, -0.04, ANKLE_Z)), 0, -10)
    p.hands['L'] = (Vector((0.33, -0.1, 0.87)), Vector((0.6, 0.8, -0.2)))
    return p


def staff_hold(p, lift=0.0, swing=0.0):
    """The rod held as a sceptre: upright beside the right foot, the right fist round it at the grip, the crystal
    standing beside the crown. Walking, it lifts off the floor and swings a little with the stride."""
    butt = Vector((-0.4 + 0.02 * swing, -0.15 - 0.07 * swing, 0.01 + lift))
    direction = Vector((0.035, -0.05 - 0.05 * swing, 1.0))
    p.free['rod'] = rod_placement(butt, direction, 0.0)
    p.hands['R'] = (on_rod(p, 0.93), Vector((-0.9, 0.5, -0.2)))


def idle(variant):
    p = stance()
    staff_hold(p)
    return p


def walk_legs(p, phase):
    """A measured walk: distance-driven, two steps a cycle, the right foot's contact at 0; the body barely bobs."""
    bob = -0.02 * (0.5 + 0.5 * math.cos(4 * math.pi * phase))
    p.offset = Vector((0.006 * math.sin(2 * math.pi * phase), 0, bob - 0.01))
    excursion = STRIDE / 4
    duty = 0.5
    for s, offset, x in (('R', 0.0, -0.11), ('L', 0.5, 0.115)):
        q = (phase + offset) % 1.0
        if q < duty:
            t = q / duty
            y = lerp(-excursion, excursion, t)
            z = ANKLE_Z
            pitch = lerp(-4, 8, smooth(0.6, 1.0, t))
        else:
            t = (q - duty) / (1 - duty)
            y = lerp(excursion, -excursion, smooth(0.0, 1.0, t))
            z = ANKLE_Z + 0.09 * math.sin(math.pi * t) ** 1.2
            pitch = lerp(16, -10, t)
        p.feet[s] = (Vector((x, y, z)), pitch, 10 if s == 'L' else -10)
    swing = math.sin(2 * math.pi * phase)
    p.add('pelvis', rot(z=5 * swing))
    p.add('spine', rot(z=-2.5 * swing))
    p.add('chest', rot(z=-3 * swing))
    p.add('head', rot(z=-1 * swing))
    return swing


def walk(phase):
    p = stance()
    swing = walk_legs(p, phase)
    p.hands['L'] = (Vector((0.33, -0.1 + 0.1 * swing, 0.88 + 0.02 * abs(swing))), Vector((0.6, 0.8, -0.2)))
    staff_hold(p, lift=0.035 + 0.015 * math.cos(4 * math.pi * phase), swing=-swing)
    return p


def strike(variant, degrees):
    """The rod driven down the lane: both hands on the shaft, the crystal brought down to the floor ahead and to the
    screen's left (where the facing looks), the body leaning into it, the legs set apart."""
    p = stance()
    land = local_floor(-0.82, -0.18 if degrees > 180 else 0.22, degrees)
    toward = land.normalized()
    yaw = math.degrees(math.atan2(toward.x, -toward.y))
    p.offset = Vector((0, 0, -0.06)) + toward * 0.05
    p.add('pelvis', rot(x=6, z=yaw * 0.45))
    p.add('spine', rot(x=8, z=yaw * 0.2))
    p.add('chest', rot(x=9, z=yaw * 0.2))
    p.add('neck', rot(x=2))
    p.add('head', rot(x=-6))
    side = Vector((-toward.y, toward.x, 0))
    p.feet['L'] = (Vector((0.14, 0.0, ANKLE_Z)) + toward * 0.17 + side * 0.03, 0, 14)
    p.feet['R'] = (Vector((-0.15, 0.0, ANKLE_Z)) - toward * 0.15, 6, -16)
    crystal_c = land + Vector((0, 0, 0.12))
    grip = toward * 0.3 + Vector((0, 0, 0.92)) - side * 0.03
    reach = (crystal_c - grip).length
    p.free['rod'] = rod_placement(grip, crystal_c - grip, HAFT - reach)
    p.hands['R'] = (Vector(grip), Vector((-0.9, 0.4, -0.3)))
    p.hands['L'] = (on_rod(p, HAFT - reach + 0.22), Vector((0.9, 0.2, -0.4)))
    return p


WINDUP_OFFSET = Vector((0, 0.025, -0.02))


def windup_upper(p, lift=0.0, sway=0.0, dip=0.0):
    """The telegraph above the hips ("Hold still. I need your face."): the rod raised high overhead in the right hand,
    upright, its crystal flaring over the crown; the left hand reaching out, open, toward the one it marks; the head
    lifted. lift and sway carry it on a walk's rise, fall and sway; dip nods the rod on the stride."""
    p.add('pelvis', rot(x=-3))
    p.add('spine', rot(x=-5))
    p.add('chest', rot(x=-7, z=8))
    p.add('neck', rot(x=-2))
    p.add('head', rot(x=-8, z=-6))
    grip = Vector((-0.2 + sway, -0.04, 1.9 + lift))
    p.free['rod'] = rod_placement(grip, Vector((-0.08, -0.1 + dip, 1.0)), 0.95)
    p.hands['R'] = (grip, Vector((-0.9, 0.2, -0.3)))
    p.hands['L'] = (Vector((0.24 + sway * 0.5, -0.46, 1.24 + lift * 0.6)), Vector((0.8, 0.3, -0.6)))
    p.heat_flare = 1.6


def windup_walk(phase):
    """The telegraph carried at a walk, so a Regent that keeps coming through the telegraph steps rather than glides:
    the walk's legs at the walk's phase under the windup's raised rod."""
    p = stance()
    walk_legs(p, phase)
    for s in ('L', 'R'):
        target, pitch, yaw = p.feet[s]
        p.feet[s] = (target, pitch * 0.5, yaw)
    p.offset = p.offset + Vector((0, WINDUP_OFFSET.y, -0.008))
    windup_upper(p, lift=p.offset.z - WINDUP_OFFSET.z, sway=p.offset.x, dip=0.03 * math.cos(4 * math.pi * phase))
    return p


# The boss's own fall (EnemyArtSet._fallFront/_fallRear), in the second phase's colours with the heart dimming as it
# goes. The killing blow rocks it back, the crowned head thrown up, the rod slipping; it sinks onto its right knee,
# leaning on the rod planted beside it, head bowed under the crown - the held beat ("I kept the wrong one..."); it
# folds and topples toward the screen's left; and it lies at full length, the rod fallen beyond its hand. The Warden's
# holds, so the fall keeps its beat.
FALL_SECONDS = {'fall-struck': 0.16, 'fall-knee': 0.44, 'fall-topple': 0.16, 'fall-down': 0.56}
#                                life  flare
FALL_HEAT = {'fall-struck': (1.0, 1.75), 'fall-knee': (0.6, 1.0), 'fall-topple': (0.32, 1.0), 'fall-down': (0.14, 1.0)}
FALL_TURN = {'fall-struck': 0.0, 'fall-knee': 0.0, 'fall-topple': 1.0, 'fall-down': 1.0}


def fall_yaw(degrees):
    d = local_floor(-1.0, 0.0, degrees).normalized()
    return math.degrees(math.atan2(d.x, -d.y))


def turned(p, yaw):
    t = rot(z=yaw)
    p.offset = t @ p.offset
    p.rot['pelvis'] = t @ p.rot.get('pelvis', Matrix.Identity(3))
    p.feet = {s: (t @ target, pitch, foot_yaw + yaw) for s, (target, pitch, foot_yaw) in p.feet.items()}
    p.knee_poles = {s: t @ v for s, v in p.knee_poles.items()}
    p.hands = {s: (t @ target, t @ pole) for s, (target, pole) in p.hands.items()}
    p.free = {b: t.to_4x4() @ g for b, g in p.free.items()}
    return p


def fall(name, degrees):
    p = Pose()
    p.life, p.heat_flare = FALL_HEAT[name]
    yaw = fall_yaw(degrees) * FALL_TURN[name]
    rod_side = local_floor(-0.75, 0.15 if degrees > 180 else 0.25, degrees)
    if name == 'fall-struck':
        p.offset = Vector((0, 0.07, -0.06))
        p.add('pelvis', rot(x=-7, z=6))
        p.add('spine', rot(x=-5))
        p.add('chest', rot(x=-7, y=-5))
        p.add('neck', rot(x=-4))
        p.add('head', rot(x=-18, z=10))
        p.feet['L'] = (Vector((0.13, 0.02, ANKLE_Z)), 0, 12)
        p.feet['R'] = (Vector((-0.13, -0.08, ANKLE_Z)), 0, -14)
        # The rod slipping: its crystal down to the floor beside it, the fist still round the shaft.
        crystal_c = rod_side + Vector((0, 0, 0.1))
        grip = Vector((-0.42, -0.1, 0.78))
        reach = (crystal_c - grip).length
        p.free['rod'] = rod_placement(grip, crystal_c - grip, HAFT - reach)
        p.hands['R'] = (grip, Vector((-0.9, 0.3, -0.3)))
        p.hands['L'] = (Vector((0.52, 0.06, 1.05)), Vector((0.6, 0.8, -0.2)))
    elif name == 'fall-knee':
        # Down on the right knee, the left foot planted, leaning on the rod planted upright beside it; bowed.
        p.offset = Vector((0, 0.04, -0.32))
        p.add('pelvis', rot(x=8))
        p.add('spine', rot(x=12))
        p.add('chest', rot(x=16))
        p.add('neck', rot(x=20))
        p.add('head', rot(x=6, z=-6))
        p.feet['R'] = (Vector((-0.12, 0.36, 0.08)), 70, -6)
        p.feet['L'] = (Vector((0.14, -0.22, ANKLE_Z)), 0, 10)
        butt = Vector((-0.34, -0.34, 0.01))
        p.free['rod'] = rod_placement(butt, Vector((0.06, 0.08, 1.0)), 0.0)
        p.hands['R'] = (on_rod(p, 0.8), Vector((-0.9, 0.3, -0.3)))
        p.hands['L'] = (Vector((0.2, -0.32, 0.5)), Vector((0.6, 0.8, -0.2)))
    elif name == 'fall-topple':
        p.offset = Vector((0, -0.12, -0.44))
        p.add('pelvis', rot(x=44))
        p.add('spine', rot(x=10))
        p.add('chest', rot(x=8))
        p.add('neck', rot(x=10))
        p.add('head', rot(x=-8, z=-20))
        for s, x in (('R', -0.12), ('L', 0.125)):
            p.feet[s] = (Vector((x, 0.38, 0.075)), 72, 0)
        crystal_c = Vector((-0.4, -1.3, 0.06))
        p.free['rod'] = rod_placement(crystal_c, Vector((0.02, -1, 0.04)), HAFT)
        p.hands['R'] = (on_rod(p, 0.9), Vector((-0.9, 0.1, -0.4)))
        p.hands['L'] = (Vector((0.34, -0.72, 0.08)), Vector((0.9, 0.1, -0.4)))
    else:
        p.offset = Vector((0, -0.32, -0.6))
        p.add('pelvis', rot(x=84, y=22))
        p.add('neck', rot(x=14))
        p.add('head', rot(x=-6, z=50))
        for s, x in (('R', -0.11), ('L', 0.11)):
            p.feet[s] = (Vector((x, 0.42, 0.13)), 150, 0)
            p.knee_poles[s] = Vector((0, 0, -1))
        crystal_c = Vector((-0.42, -1.7, 0.06))
        p.free['rod'] = rod_placement(crystal_c, Vector((0, -1, 0.03)), HAFT)
        p.hands['R'] = (on_rod(p, 0.95), Vector((-0.9, 0.2, 0.4)))
        p.hands['L'] = (Vector((0.42, -0.32, 0.08)), Vector((0.9, 0.2, 0.4)))
    return turned(p, yaw) if yaw else p


def pose_for(name, variant, degrees=FACINGS[0][1]):
    if name == 'idle' or name in SHARDS:
        return idle(variant)
    if name.startswith('step') or name.startswith('pass'):
        phase = {'step-a': 0.0, 'pass-a': 0.25, 'step-b': 0.5, 'pass-b': 0.75}[name]
        return walk(phase)
    if name == 'strike':
        return strike(variant, degrees)
    if name == 'windup':
        # The standing windup is the walking windup's contact B, so one drawing serves both (the Warden's contract).
        return windup_walk(0.5)
    if name in WINDUP_WALK:
        return windup_walk(WINDUP_WALK[name])
    return fall(name, degrees)


# ---------------------------------------------------------------------------------------------------------------------
# Baked contact occlusion: rays from every vertex in the stance, against the parts that do not swing about.
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
    reach = 0.14
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
            open_ = max(0.0, 1 - 1.6 * o / norm)
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


frame_camera(CANVAS_PX, PIVOT_PX)


def set_facing(degrees):
    RIG.rotation_euler = (0, 0, math.radians(degrees - 270))


def screen_point(solver, rest_point, bone_name):
    world = rig_matrix() @ (solver.G[bone_name] @ rest_point)
    return (round(world.dot(RIGHT), 5), round(world.dot(UP), 5))


def show_for(variant, pose_name):
    """Which parts render: the shards only in the copies' remains (and nothing else there); the cracks only in the
    second phase; the crown, the heart, the eyes and the collar's shards never on a copy."""
    remains = {'shards-burst': 'burst', 'shards-heap': 'heap'}.get(pose_name)
    for name, obj in PARTS.items():
        show = INFO[name]['show']
        if remains:
            visible = show == remains
        elif show in ('burst', 'heap'):
            visible = False
        elif show == 'hot':
            visible = variant == 'hot'
        elif show == 'regent':
            visible = variant != 'copy'
        else:
            visible = True
        obj.hide_render = not visible


FOOTPRINT = [('pelvis', 0.24), ('spine', 0.22), ('chest', 0.24), ('neck', 0.12), ('head', 0.13), ('thigh.L', 0.1),
             ('thigh.R', 0.1), ('shin.L', 0.08), ('shin.R', 0.08), ('foot.L', 0.07), ('foot.R', 0.07),
             ('upper_arm.L', 0.08), ('upper_arm.R', 0.08), ('forearm.L', 0.1), ('forearm.R', 0.1), ('hand.L', 0.06),
             ('hand.R', 0.06), ('robe_back.L', 0.2), ('robe_back.R', 0.2), ('robe_front.L', 0.2), ('robe_front.R', 0.2),
             ('rod', 0.04)]


def footprint(solver):
    out = []
    m = rig_matrix()
    for bone_name, radius in FOOTPRINT:
        head = REST[bone_name].to_translation()
        tail = REST_TAIL[bone_name]
        for i in range(5):
            world = m @ (solver.G[bone_name] @ head.lerp(tail, i / 4))
            out.append([round(world.x, 4), round(world.y * math.sin(ELEVATION), 4), round(world.z / HEIGHT, 4),
                        round(radius * SCALE, 4)])
    world = m @ (solver.G['rod'] @ ROD_CRYSTAL_C)
    out.append([round(world.x, 4), round(world.y * math.sin(ELEVATION), 4), round(world.z / HEIGHT, 4),
                round(0.08 * SCALE, 4)])
    return out


def shards_footprint(pose_name):
    """The remains' contact: every visible shard near the floor, from the root's frame (both facings share them)."""
    out = []
    m = rig_matrix()
    show = 'burst' if pose_name == 'shards-burst' else 'heap'
    for name, obj in PARTS.items():
        if INFO[name]['show'] != show:
            continue
        pts = [m @ v.co for v in obj.data.vertices]
        c = sum(pts, Vector()) / len(pts)
        r = max((p - c).length for p in pts)
        out.append([round(c.x, 4), round(c.y * math.sin(ELEVATION), 4), round(min(p.z for p in pts) / HEIGHT, 4),
                    round(min(r, 0.2), 4)])
    return out


def variants_for(pose_name):
    if pose_name in FALLS:
        return ['hot'] if 'hot' in VARIANT_SET else []
    if pose_name in SHARDS:
        return ['copy'] if 'copy' in VARIANT_SET else []
    # The copies do not swing (docs/50): their strike is their idle, so it is not drawn.
    return [v for v in VARIANT_SET if not (v == 'copy' and pose_name == 'strike')]


def render_all():
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    started = time.time()
    for facing, degrees in FACING_SET:
        set_facing(degrees)
        for pose_name in POSE_SET:
            if (pose_name in SHARED_FALLS or pose_name in SHARDS) and facing != 'front':
                continue
            falling = pose_name in FALLS or pose_name in SHARDS
            canvas, pivot = (FALL_CANVAS_PX, FALL_PIVOT_PX) if falling else (CANVAS_PX, PIVOT_PX)
            frame_camera(canvas, pivot)
            for variant in variants_for(pose_name):
                pose = pose_for(pose_name, variant, degrees)
                solver = apply(pose)
                set_state(heat=1.0 if variant == 'hot' else 0.0)
                show_for(variant, pose_name)
                bpy.context.view_layer.update()
                stem = '%s_%s' % (pose_name, variant)
                for glow_pass, suffix in ((0.0, ''), (1.0, '_glow')):
                    set_state(glow_pass=glow_pass)
                    path = OUT / facing / (stem + suffix + '.png')
                    path.parent.mkdir(parents=True, exist_ok=True)
                    scene.render.filepath = str(path)
                    bpy.ops.render.render(write_still=True)
                set_state(glow_pass=0.0)
                record = dict(facing=facing, pose=pose_name, variant=variant,
                              file=str((OUT / facing / (stem + '.png')).relative_to(OUT)).replace('\\', '/'),
                              glow=str((OUT / facing / (stem + '_glow.png')).relative_to(OUT)).replace('\\', '/'),
                              heart=screen_point(solver, HEART_C, 'chest'),
                              crystal=screen_point(solver, ROD_CRYSTAL_C, 'rod'))
                if falling:
                    record.update(canvas=list(canvas), pivot=list(pivot),
                                  seconds=FALL_SECONDS.get(pose_name, 0.0),
                                  footprint=shards_footprint(pose_name) if pose_name in SHARDS else footprint(solver))
                records.append(record)
                print('REGENT_FRAME', facing, pose_name, variant, '%.1fs' % (time.time() - started), flush=True)
    frame_camera(CANVAS_PX, PIVOT_PX)
    meta = dict(blender=bpy.app.version_string, ppu=PPU, supersample=SS, canvas=list(CANVAS_PX), pivot=list(PIVOT_PX),
                elevation_degrees=30, height_compensation=HEIGHT, scale=SCALE, stride=round(STRIDE * SCALE, 4),
                glow_scale=GLOW_SCALE, channels=CHANNELS,
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
    RIG.animation_data_create()
    for name in POSES:
        action = bpy.data.actions.new('Regent ' + name)
        action.use_fake_user = True
        RIG.animation_data.action = action
        apply(pose_for(name, 'cool'))
        for pb in RIG.pose.bones:
            pb.keyframe_insert('location', frame=1)
            pb.keyframe_insert('rotation_quaternion', frame=1)
            pb.keyframe_insert('scale', frame=1)
    RIG.animation_data.action = bpy.data.actions['Regent idle']


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
show_for('cool', 'idle')     # the saved scene shows the first phase; set Heat to 1 and unhide the cracks for the second
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND), compress=True)
v, t = budget()
print('REGENT_COMPLETE', 'parts', len(PARTS), 'vertices', v, 'triangles', t, flush=True)
