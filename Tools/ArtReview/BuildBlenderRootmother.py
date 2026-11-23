"""Rootmother Myrr, the Gloam Gardens' gate boss (docs/44 section 1.8, docs/64): a reproducible stylised model, rig, poses
and the sprite render for her review sheets (docs/64 stage 4; not yet worn by any prefab).

Run with Blender 4.5 LTS, headless:

    blender --background --factory-startup --python Tools/ArtReview/BuildBlenderRootmother.py -- [options]

Options after the "--":
    --out DIR        raw frames and frames.json (default TestResults/rootmother-render, git-ignored)
    --blend PATH     the saved scene (default ArtDirection/2026-11-23/rootmother-01/rootmother.blend)
    --poses LIST     a comma-separated subset of idle-a,idle-b,windup-0,windup-1,windup-2,strike-0,strike-1,strike-2,
                     phase-0,phase-1,phase-2,fall-struck,fall-sink,fall-slump,fall-stump
    --facings LIST   a comma-separated subset of front,rear
    --variants LIST  cool (the first phase: her buds closed) and hot (the second: the grafted blooms open, the heart and
                     the root veins lit)
    --samples N      Cycles samples per render pixel (default 8)
    --no-render      build and save the .blend, render nothing

Who she is: the gardener who grafted lost futures onto trees. A tall, hunched matriarch grown half into a tree: a body
of charred bark (docs/51's enemy bark #4A3B33) on a skirt of six roots that splay onto the floor - she has no legs, she
walks on her roots - a pale heartwood mask with orchid eyes, a mantle of deep plum leaves, and grafted onto her back a
canopy of seven branches, each tipped with an orchid bud: the futures she keeps. In her right hand a grafting hook, a
long heartwood haft with a curved verdigris blade (docs/51's verdigris replacing brass); her left hand is three long
root fingers. Her seed-pod heart glows orchid in a cage of bark ribs. Nothing on her is red-orange (the telegraph's
colour, docs/52 section 2.1 rule 3) or gloamlight (the garden's, docs/51 rule 5): orchid is the creature.

The read at phone scale: the wide canopy over a hunched dark body, its orchid buds the brightest points; the pale mask
under it; the hook's verdigris crescent standing beside the canopy; the root skirt wider at the floor than any foundry
golem. The second phase reads as the buds opening into blooms (petals, brighter) and orchid veins along the roots.

The clips (docs/64 section 7): idle (two breaths), the telegraph ("Be still. Be planted.": she gathers, rears up with
the hook raised and her root hand thrust at the hero, quivering at its peak), the root strike (the hook swung down and
driven into the floor ahead, roots bursting up round the blade, the recovery), the phase change (she rears back, the
canopy bursts into bloom, she settles), and death (rocked back, sinking into her own roots, slumping forward, a stump
with its canopy fallen and its blooms dark). Both facings look left (the game mirrors them for the right); the camera
and the key light never move; the root stays on the floor origin, which projects to the same render pixel in every
frame: the pivot. A holdout at the floor hides what sinks under it.

Nothing here is generated imagery: the geometry is lofted rings and primitives, every part rides one bone rigidly, the
shading is one authored toon node group (BuildBlenderGlassRegent.py's, without its chrome band, lit for dusk), and every
frame is a pose computed by the functions below. One model renders every frame, both facings and both phases. The rig,
camera, solver, contract and frame loop are BuildBlenderGlassRegent.py's (BuildBlenderWarden.py's before it), so her
sprites keep the boss contract's camera, scale and pivot.

Each pose renders twice: the beauty, in which the glowing parts show their unlit colour, and an emission pass in which
every glowing part writes its glow into its own channel and everything else is black - red the seed-pod heart, green
the buds, blooms and eyes, blue the second phase's root veins. review_rootmother.py colours each channel orchid and adds
it with its own gain, so each group's value is tuned without rendering again.
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


OUT = Path(option('--out', str(ROOT / 'TestResults/rootmother-render')))
BLEND = Path(option('--blend', str(ROOT / 'ArtDirection/2026-11-23/rootmother-01/rootmother.blend')))
SAMPLES = int(option('--samples', 8))
RENDER = not option('--no-render', False)

# ---- The sprite contract: the Glass Regent's (128 texels a world unit, 4x supersampled), a little larger canvas for the
# canopy and the hook driven into the floor ahead ----
PPU = 128
SS = 4
CANVAS_PX = (448, 512)
PIVOT_PX = (224, 92)
ELEVATION = math.radians(30)
HEIGHT = 1 / math.cos(ELEVATION)
FACINGS = [('front', 234), ('rear', 130)]
# Built in design units a little over the Regent's height; drawn at 0.88 so she stands the tallest boss without her root
# skirt crowding the hero's kite (review pass 1: at 1.0 she stood 1.24x the Regent at phone scale).
SCALE = 0.88
IDLES = ['idle-a', 'idle-b']
WINDUPS = ['windup-0', 'windup-1', 'windup-2']
STRIKES = ['strike-0', 'strike-1', 'strike-2']
PHASES = ['phase-0', 'phase-1', 'phase-2']
FALLS = ['fall-struck', 'fall-sink', 'fall-slump', 'fall-stump']
SHARED_FALLS = {'fall-slump', 'fall-stump'}       # the front's drawings serve the rear too (the Regent's rule)
POSES = [*IDLES, *WINDUPS, *STRIKES, *PHASES, *FALLS]
VARIANTS = ['cool', 'hot']
GLOW_SCALE = 0.5
CHANNELS = {'heart': (1, 0, 0), 'bloom': (0, 1, 0), 'vein': (0, 0, 1)}
# Each clip's frames, their seconds each, for the review's timing strip and a later EnemyArtSet.
CLIP_SECONDS = {'idle': 0.6, 'windup': 0.4, 'strike': 0.12, 'phase': 0.3, 'fall': 0.35}

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
scene.cycles.seed = 23
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
# The dusk's rim from behind the upper left (the dimmed sun low behind the ash cloud, docs/51 #CDBE8E, muted), so the far
# edge of the dark bark separates from the loam.
RIM = (-0.62 * RIGHT + 0.58 * UP - 0.52 * TOWARD).normalized()
# A cool verdigris rim from behind, low on the right, opposite the key: never gloamlight (docs/51 rule 5).
LOW_RIM = (0.58 * RIGHT - 0.25 * UP - 0.78 * TOWARD).normalized()
HALF = (KEY + TOWARD).normalized()


# ---------------------------------------------------------------------------------------------------------------------
# State shared by every material: the phase, the telegraph's flare, death's dimming, and which pass renders
# ---------------------------------------------------------------------------------------------------------------------
def build_state_group():
    ng = bpy.data.node_groups.new('Rootmother state', 'ShaderNodeTree')
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
# Shading: one toon group fed by authored palettes (BuildBlenderGlassRegent.py's, lit for the garden's dusk)
# ---------------------------------------------------------------------------------------------------------------------
def build_toon_group():
    ng = bpy.data.node_groups.new('Rootmother toon', 'ShaderNodeTree')
    io = ng.interface
    for name, kind, default in [
            ('Light', 'NodeSocketColor', (1, 1, 1, 1)), ('Mid', 'NodeSocketColor', (.5, .5, .5, 1)),
            ('Shadow', 'NodeSocketColor', (.2, .2, .2, 1)), ('Deep', 'NodeSocketColor', (.05, .05, .05, 1)),
            ('Spec', 'NodeSocketFloat', 0.0), ('Spec Color', 'NodeSocketColor', (1, 1, 1, 1)),
            ('Rim', 'NodeSocketFloat', 0.35), ('Occlusion', 'NodeSocketFloat', 1.0),
            ('Band Shift', 'NodeSocketFloat', 0.0), ('Glint', 'NodeSocketFloat', 0.0),
            ('Facet', 'NodeSocketFloat', 0.5)]:
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

    normal = geo.outputs['Normal']
    half_lambert = math_node('MULTIPLY_ADD', dot(normal, vec(KEY)), 0.5, 0.5)
    facet = math_node('MULTIPLY', math_node('SUBTRACT', gi.outputs['Facet'], 0.5), 0.11)
    lit = math_node('ADD', math_node('ADD', half_lambert, gi.outputs['Band Shift']), facet)
    first = step(lit, 0.36, 0.5)
    second = step(lit, 0.73, 0.85)
    colour = mix(first, gi.outputs['Shadow'], gi.outputs['Mid'])
    colour = mix(second, colour, gi.outputs['Light'])
    # The bark's grain: a stretched noise drifting the value a little, coarser than the Regent's brush.
    coords = n.new('ShaderNodeTexCoord')
    brush = n.new('ShaderNodeTexNoise')
    brush.inputs['Scale'].default_value = 9.0
    brush.inputs['Detail'].default_value = 3.0
    l.new(coords.outputs['Generated'], brush.inputs['Vector'])
    drift = n.new('ShaderNodeCombineColor')
    for channel, spread in enumerate((0.26, 0.24, 0.24)):
        l.new(math_node('MULTIPLY_ADD', brush.outputs['Fac'], spread, 1 - spread / 2), drift.inputs[channel])
    paint = n.new('ShaderNodeMixRGB')
    paint.blend_type = 'MULTIPLY'
    paint.inputs['Fac'].default_value = 1.0
    l.new(colour, paint.inputs['Color1'])
    l.new(drift.outputs[0], paint.inputs['Color2'])
    colour = paint.outputs['Color']
    # Baked contact occlusion pulls hard toward the deep colour: under the mantle, between the roots, in the ribs.
    closed = math_node('SUBTRACT', 1.0, step(gi.outputs['Occlusion'], 0.3, 0.97))
    colour = mix(closed, colour, gi.outputs['Deep'])
    # Her own blooms' orchid bounced onto the undersides (the canopy over her), dimming with death's Life.
    life = state.outputs['Life']
    bounce = math_node('MULTIPLY', step(dot(normal, vec(Vector((0, 0, -1)))), 0.2, 0.8), 0.07)
    bounce = math_node('MULTIPLY', bounce, math_node('MULTIPLY_ADD', state.outputs['Heat'], 0.8, 0.6))
    bounce = math_node('MULTIPLY', bounce, life)
    colour = add_colour(colour, rgb('#D86BD0'), bounce)
    facing = n.new('ShaderNodeLayerWeight')
    facing.inputs['Blend'].default_value = 0.4
    rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.5, 0.82), math_node('MAXIMUM', dot(normal, vec(RIM)), 0.0))
    rim = math_node('MULTIPLY', rim, gi.outputs['Rim'])
    colour = add_colour(colour, rgb('#B9B49C'), math_node('MULTIPLY', rim, 0.5))
    low_rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.66, 0.9),
                        math_node('MAXIMUM', dot(normal, vec(LOW_RIM)), 0.0))
    colour = add_colour(colour, rgb('#7FA595'), math_node('MULTIPLY', low_rim, 0.4))
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
    # docs/51's enemy bark body #4A3B33 as the mid: a step lighter than the ash loam (#3C3F36) with the dark outline.
    'bark': ('#8C7766', '#4A3B33', '#2D241F', '#140F0C'),
    # The roots of her skirt and her root hand: a step darker, so the canopy and the mask read first.
    'root': ('#6E5C4E', '#3A2E27', '#231B17', '#0E0A08'),
    'root-deep': ('#2E241F', '#1C1613', '#120E0C', '#080605'),
    # The heartwood mask: the lightest wood, the face that reads first under the canopy.
    'mask': ('#E0CBB0', '#A8896C', '#6A5240', '#2A1E15'),
    # The hook's haft and the branches' young wood.
    'heartwood': ('#A88E74', '#6B5442', '#40312A', '#1A120E'),
    # The mantle's leaves: docs/51's bloom deep #5A1F66, the enemies' family, far from the moss of the walls.
    'leaf': ('#9A5A98', '#5A1F66', '#3A1442', '#1A081F'),
    # The hook's blade: verdigris, the garden's replacement for brass (docs/51 section 4).
    'verdigris': ('#C2E2D2', '#6FA38C', '#3D6655', '#17291F'),
    # The buds and blooms, unlit: light enough to read as flowers with no glow at all.
    'bloom': ('#F6C2F0', '#D86BD0', '#8E3A8A', '#3E1340'),
    'heart': ('#F6C2F0', '#C45ABC', '#7A2C78', '#320E35'),
}
OUTLINE = '#120D0B'


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

    def toon(self, palette, spec=0.0, spec_color='#F4EEDC', rim=0.4, band_shift=0.0, occlusion=True, glint=0.0):
        light, mid, shadow, deep = PALETTES[palette]
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


def glow_material(name, palette, channel, noise_scale=12.0, heat_gain=0.6, base=0.6):
    """A glowing part: its unlit toon colour in the beauty pass, and in the emission pass a noisy mask in its channel,
    raised by the telegraph's Flare and by the second phase (Heat), dimmed by death's Life (squared)."""
    s = Shading(name)
    colour = s.toon(palette, spec=0.6, spec_color='#FFFFFF', glint=0.8, rim=0.55)
    # Dying, the flower withers to a dark husk with the Life that dims its glow.
    colour = s.mix(s.math('SUBTRACT', 1.0, s.state('Life')), colour, s.rgb('#3B2433'))
    mask = s.math('MULTIPLY_ADD', s.noise(noise_scale, 1.5), 0.4, 0.6)
    mask = s.math('MULTIPLY', mask, s.math('MULTIPLY_ADD', s.state('Heat'), heat_gain, base))
    mask = s.math('MULTIPLY', mask, s.state('Flare'))
    life = s.state('Life')
    mask = s.math('MULTIPLY', mask, s.math('MULTIPLY', life, life))
    return s.finish(colour, mask=mask, channel=channel)


def vein_material():
    """The second phase's root veins: pale orchid light in the beauty pass, the vein channel in the emission pass."""
    s = Shading('Root vein')
    life = s.state('Life')
    return s.finish(s.rgb('#B04AA8'), mask=s.math('MULTIPLY', s.math('MULTIPLY', life, life), 0.7), channel='vein')


def holdout_material():
    s = Shading('Floor holdout')
    h = s.nodes.new('ShaderNodeHoldout')
    s.links.new(h.outputs[0], s.out.inputs['Surface'])
    return s.mat


MATERIALS = {}


def build_materials():
    m = MATERIALS
    m['outline'] = outline_material()
    m['bark'] = plain('Charred bark', 'bark', spec=0.05, rim=0.4)
    m['root'] = plain('Root', 'root', spec=0.04, rim=0.3)
    m['root-deep'] = plain('Root shadow', 'root-deep', rim=0.1)
    m['mask'] = plain('Heartwood mask', 'mask', spec=0.25, spec_color='#FFF6E8', rim=0.5, band_shift=0.04)
    m['heartwood'] = plain('Heartwood', 'heartwood', spec=0.15, rim=0.4)
    m['leaf'] = plain('Plum leaves', 'leaf', spec=0.12, spec_color='#F2D3EE', rim=0.45)
    m['verdigris'] = plain('Verdigris blade', 'verdigris', spec=0.9, spec_color='#FFFFFF', glint=0.9, rim=0.5)
    m['heart'] = glow_material('Seed-pod heart', 'heart', 'heart', noise_scale=8.0, heat_gain=0.7, base=0.65)
    m['bud'] = glow_material('Grafted bud', 'bloom', 'bloom', heat_gain=0.8, base=0.45)
    m['eyes'] = glow_material('Eyes', 'bloom', 'bloom', heat_gain=0.4, base=0.8)
    m['vein'] = vein_material()
    m['holdout'] = holdout_material()


build_materials()


# ---------------------------------------------------------------------------------------------------------------------
# Geometry: low-poly parts, flat-shaded and chamfered (the mask smooth-shaded)
# ---------------------------------------------------------------------------------------------------------------------
PARTS = {}      # name -> object
INFO = {}       # name -> dict(bone, material, contour, chamfer, occluder, show)


def new_object(name, verts, faces, bone, material, contour=True, chamfer=0.006, occluder=False, flat=True,
               show='all'):
    """show: 'all', 'hot' (the second phase only: the blooms' petals, the root veins), 'cool' (the first phase only: the
    closed buds' sepals) or 'strike' (only the root strike's frames: the roots bursting round the blade)."""
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
    out = []
    for f in faces:
        a, b, c = (Vector(verts[i]) for i in f[:3])
        normal = (b - a).cross(c - a)
        centre = sum((Vector(verts[i]) for i in f), Vector()) / len(f)
        radial = Vector((centre.x, centre.y - axis_y, 0))
        out.append(tuple(reversed(f)) if normal.dot(radial) < 0 else f)
    return out


def grid(name, rows, bone, material, axis_y=0.0, **kw):
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


def ring_points(cx, cy, z, rx, ry, count, start=-math.pi / 2):
    return [Vector((cx + rx * math.cos(start + 2 * math.pi * i / count), cy + ry * math.sin(start + 2 * math.pi * i / count), z))
            for i in range(count)]


def ellipsoid(name, centre, radii, bone, material, rings=6, segments=10, axes=None, **kw):
    ax = axes or (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))
    verts, faces = [], []
    lats = [math.asin(lerp(-1.0, 1.0, j / rings)) for j in range(rings + 1)]
    for lat in lats[1:-1]:
        for i in range(segments):
            lon = 2 * math.pi * i / segments
            p = (ax[0] * (radii[0] * math.cos(lat) * math.cos(lon)) + ax[1] * (radii[1] * math.cos(lat) * math.sin(lon))
                 + ax[2] * (radii[2] * math.sin(lat)))
            verts.append(Vector(centre) + p)
    inner = len(lats) - 2
    for j in range(inner - 1):
        for i in range(segments):
            i1 = (i + 1) % segments
            faces.append((j * segments + i, j * segments + i1, (j + 1) * segments + i1, (j + 1) * segments + i))
    verts.append(Vector(centre) - ax[2] * radii[2])
    bottom = len(verts) - 1
    for i in range(segments):
        faces.append((bottom, (i + 1) % segments, i))
    verts.append(Vector(centre) + ax[2] * radii[2])
    top = len(verts) - 1
    base = (inner - 1) * segments
    for i in range(segments):
        faces.append((top, base + i, base + (i + 1) % segments))
    return new_object(name, verts, faces, bone, material, **kw)


def tube(name, path, radii, bone, material, count=7, cap_start=True, cap_end=True, twist_ref=Vector((0, -1, 0)), **kw):
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
        rings.append([p + normal * (r * math.cos(2 * math.pi * i / count)) + binormal * (r * math.sin(2 * math.pi * i / count))
                      for i in range(count)])
    return loft(name, rings, bone, material, closed=True, cap_start=cap_start, cap_end=cap_end, **kw)


def thorn(name, base, axis, length, radius, bone, material, sides=4, **kw):
    """A cone: a thorn, a sepal, a leaf's point."""
    a = Vector(axis).normalized()
    ref = Vector((0, -1, 0)) if abs(a.y) < 0.9 else Vector((1, 0, 0))
    u = (ref - a * ref.dot(a)).normalized()
    v = a.cross(u)
    verts = [Vector(base) + (u * math.cos(2 * math.pi * k / sides) + v * math.sin(2 * math.pi * k / sides)) * radius
             for k in range(sides)]
    verts.append(Vector(base) + a * length)
    verts.append(Vector(base) - a * (length * 0.1))
    faces = []
    for k in range(sides):
        k1 = (k + 1) % sides
        faces.append((k, k1, sides))
        faces.append((k1, k, sides + 1))
    return new_object(name, verts, faces, bone, material, **kw)


def leaf(name, base, along, across, length, width, bone, material, curl=0.25, **kw):
    """A leaf or a petal: a flat diamond plate with a midrib fold, curling its tip toward curl x its normal."""
    along = Vector(along).normalized()
    across = (Vector(across) - along * Vector(across).dot(along)).normalized()
    normal = along.cross(across)
    pts = []
    for t, w in ((0.0, 0.0), (0.3, 1.0), (0.65, 0.85), (1.0, 0.0)):
        c = Vector(base) + along * (length * t) + normal * (curl * length * t * t)
        pts.append((c, w))
    verts = []
    for c, w in pts:
        verts.append(c - across * (width * w))
        verts.append(c + normal * (0.012 * w) + across * 0.0)
        verts.append(c + across * (width * w))
    faces = []
    for j in range(len(pts) - 1):
        for i in range(2):
            faces.append((j * 3 + i, j * 3 + i + 1, (j + 1) * 3 + i + 1, (j + 1) * 3 + i))
    return new_object(name, verts, faces, bone, material, **kw)


def mirror_x(v):
    return Vector((-v.x, v.y, v.z))


def side_point(v, side):
    return v if side > 0 else mirror_x(v)


# ---- Measurements (design units, the floor at z = 0, facing -Y, her left is +X) ----
# Taller than the Glass Regent to her canopy, hunched: a root skirt wider at the floor than any golem, a narrow bark
# body leaning forward, a long neck under a long mask, the canopy rising off her back above her head.
WAIST_Z = 0.95
PELVIS_Z = 0.88
CHEST_Z = 1.2
NECK_Z = 1.5
HEAD_BASE = Vector((0, -0.02, 1.58))
HEAD_C = Vector((0, -0.04, 1.71))
HEAD_R = (0.11, 0.095, 0.155)
SHOULDER = Vector((0.27, 0.02, 1.43))
ELBOW = Vector((0.38, 0.05, 1.13))
WRIST = Vector((0.41, 0.0, 0.87))
FIST = Vector((0.415, -0.01, 0.81))
HEART_C = Vector((0, -0.19, 1.26))
HOOK_REST = Vector((-1.0, 0.0, 0.0))     # the hook's butt at rest, the haft standing upright out of the way
HAFT = 1.55                              # butt to the blade's heel
SKIRT_ANGLES = [0, 60, 120, 180, 240, 300]   # degrees round her from the front, + to her left
ERUPT_REST = Vector((1.2, 0.0, 0.0))     # the strike's roots, built round this point, placed by their own bone


def around(degrees, r, z, ry=None):
    a = -math.pi / 2 + math.radians(degrees)
    return Vector((r * math.cos(a), (ry if ry is not None else r) * math.sin(a), z))


def build_skirt():
    """Six root panels from the waist to the floor, each a curved bark wedge with a keel, and from its hem a root tip
    running out along the floor and curling into it; each on its own bone, so the skirt flares as she rears and splays
    as she sinks. In the second phase an orchid vein runs down each panel's keel and out along its root."""
    for k, deg in enumerate(SKIRT_ANGLES):
        name = 'skirt.%d' % k
        rows = []
        for z, r, ry in ((WAIST_Z, 0.21, 0.17), (0.72, 0.29, 0.24), (0.44, 0.38, 0.32), (0.16, 0.47, 0.4), (0.03, 0.52, 0.44)):
            row = []
            for t in (-25, -12, 0, 12, 25):
                p = around(deg + t, r, z, ry)
                keel = (1.0 - abs(t) / 25.0) * 0.04 * (1 - z)
                p += Vector((p.x, p.y, 0)).normalized() * keel
                row.append(p)
            rows.append(row)
        grid('Skirt panel %d' % k, rows, name, 'bark' if k % 2 == 0 else 'root', occluder=True, chamfer=0.004)
        hem = around(deg, 0.5, 0.03, 0.43)
        out = Vector((hem.x, hem.y, 0)).normalized()
        side = Vector((-out.y, out.x, 0))
        bend = 0.06 if k % 2 == 0 else -0.06
        path = [hem + Vector((0, 0, 0.06)), hem + out * 0.16 + Vector((0, 0, 0.04)),
                hem + out * 0.32 + side * bend + Vector((0, 0, 0.025)), hem + out * 0.46 + side * bend * 2.2 + Vector((0, 0, 0.01)),
                hem + out * 0.55 + side * bend * 3.4 + Vector((0, 0, -0.01))]
        tube('Root tip %d' % k, path, [0.075, 0.056, 0.04, 0.022, 0.006], name, 'root', count=6)
        # A knot on the panel and a thorn off it, so the skirt reads as roots, not cloth.
        knot = around(deg + 8, 0.35, 0.52, 0.3)
        ellipsoid('Root knot %d' % k, knot, (0.035, 0.03, 0.04), name, 'root', rings=4, segments=6)
        vein = [around(deg, r + 0.012, z, ry + 0.012) for z, r, ry in ((0.86, 0.24, 0.2), (0.55, 0.35, 0.3), (0.2, 0.46, 0.39))]
        vein += [hem + Vector((0, 0, 0.06)), hem + out * 0.22 + Vector((0, 0, 0.045))]
        tube('Root vein %d' % k, vein, [0.006, 0.0075, 0.008, 0.0075, 0.004], name, 'vein', count=5, contour=False,
             chamfer=0.0, show='hot')


def build_skirt_core():
    """The dark heart of the skirt between its root panels: a cone of the root's deep, so the gaps read as shadowed
    roots, not as the floor through her."""
    rings = [ring_points(0, 0, z, r, ry, 12) for z, r, ry in ((WAIST_Z - 0.02, 0.19, 0.15), (0.6, 0.29, 0.24), (0.03, 0.43, 0.36))]
    loft('Skirt core', rings, 'pelvis', 'root-deep', cap_start=True, cap_end=True, occluder=True, contour=False)


def build_body():
    """The bark body: a lofted column from the waist to the neck, the seed-pod heart in a cage of bark ribs, the plum leaf
    mantle round the shoulders, the neck and the long heartwood mask with its orchid eyes and a heavy bark brow."""
    rings = []
    for z, rx, ry, y in ((0.9, 0.19, 0.15, 0.0), (1.02, 0.2, 0.16, 0.0), (1.16, 0.24, 0.18, 0.0), (1.3, 0.28, 0.2, 0.01),
                         (1.42, 0.29, 0.19, 0.02), (1.5, 0.2, 0.14, 0.02)):
        rings.append(ring_points(0, y, z, rx, ry, 10))
    loft('Body', rings, 'spine', 'bark', cap_start=True, cap_end=True, occluder=True)
    ellipsoid('Seed-pod heart', HEART_C, (0.07, 0.05, 0.095), 'chest', 'heart', rings=6, segments=8, chamfer=0.0)
    for k, a in enumerate((-50, -20, 20, 50)):
        x = math.sin(math.radians(a))
        path = [Vector((x * 0.13, -0.15, 1.12)), Vector((x * 0.1, -0.23, 1.2)), Vector((x * 0.08, -0.25, 1.3)),
                Vector((x * 0.1, -0.2, 1.39))]
        tube('Rib %d' % k, path, [0.018, 0.016, 0.016, 0.018], 'chest', 'bark', count=5)
    # The mantle: two rings of plum leaves over the shoulders and down the back.
    for k in range(14):
        deg = -150 + k * (300 / 13)
        base = around(deg, 0.22, 1.47, 0.16)
        out = Vector((base.x, base.y, 0)).normalized()
        along = (out * 0.8 + Vector((0, 0, -0.55))).normalized()
        leaf('Mantle leaf %d' % k, base, along, Vector((-out.y, out.x, 0)), 0.26 + 0.04 * (k % 3), 0.07, 'chest', 'leaf',
             curl=0.18)
    for k in range(9):
        deg = 120 + k * (120 / 8)
        base = around(deg, 0.2, 1.36, 0.15)
        out = Vector((base.x, base.y, 0)).normalized()
        along = (out * 0.5 + Vector((0, 0, -0.85))).normalized()
        leaf('Back leaf %d' % k, base, along, Vector((-out.y, out.x, 0)), 0.32, 0.065, 'chest', 'leaf', curl=0.12)
    tube('Neck', [Vector((0, 0.0, NECK_Z - 0.02)), Vector((0, -0.01, 1.56)), Vector((0, -0.02, 1.62))], [0.06, 0.055, 0.05],
         'neck', 'bark', count=8)
    ellipsoid('Mask', HEAD_C, HEAD_R, 'head', 'mask', rings=8, segments=12, chamfer=0.0, flat=False)
    # The long chin of the mask, pointing down: a gardener's patient face.
    thorn('Chin', HEAD_C + Vector((0, -0.04, -0.1)), Vector((0, -0.25, -1)), 0.09, 0.045, 'head', 'mask', sides=6)
    tube('Brow', [Vector((-0.1, -0.07, 1.75)), Vector((-0.04, -0.12, 1.765)), Vector((0.04, -0.12, 1.765)),
                  Vector((0.1, -0.07, 1.75))], [0.02, 0.026, 0.026, 0.02], 'head', 'bark', count=6)
    for side in (-1, 1):
        ellipsoid('Eye %s' % ('L' if side > 0 else 'R'), Vector((0.042 * side, -0.112, 1.715)), (0.022, 0.01, 0.014),
                  'head', 'eyes', rings=4, segments=6, contour=False, chamfer=0.0)
    # Bark plates crowning the mask, where the branches begin.
    for k, (x, tilt) in enumerate(((-0.07, -0.5), (0.0, 0.0), (0.07, 0.5))):
        thorn('Crown bark %d' % k, Vector((x, 0.02, 1.8)), Vector((tilt, 0.4, 1)), 0.12, 0.035, 'head', 'bark', sides=5)


# The canopy: seven branches grafted onto her back and shoulders, each tipped with a bud (the futures she keeps).
BRANCHES = [
    # path from the back up and out (her left is +X); the last point carries the bud.
    [(0.0, 0.14, 1.48), (0.02, 0.2, 1.78), (0.0, 0.24, 2.02), (0.03, 0.22, 2.2)],
    [(0.1, 0.12, 1.47), (0.24, 0.17, 1.72), (0.4, 0.22, 1.9), (0.52, 0.2, 2.03)],
    [(-0.1, 0.12, 1.47), (-0.24, 0.17, 1.72), (-0.4, 0.22, 1.9), (-0.52, 0.2, 2.03)],
    [(0.2, 0.08, 1.45), (0.42, 0.1, 1.56), (0.62, 0.12, 1.7), (0.74, 0.1, 1.84)],
    [(-0.2, 0.08, 1.45), (-0.42, 0.1, 1.56), (-0.62, 0.12, 1.7), (-0.74, 0.1, 1.84)],
    [(0.06, 0.18, 1.5), (0.16, 0.34, 1.76), (0.26, 0.4, 1.96), (0.3, 0.38, 2.1)],
    [(-0.06, 0.18, 1.5), (-0.16, 0.34, 1.76), (-0.26, 0.4, 1.96), (-0.3, 0.38, 2.1)],
]
BUDS = []      # (centre, direction) of every bud, for the review's footprint


def build_canopy():
    for k, pts in enumerate(BRANCHES):
        path = [Vector(p) for p in pts]
        tube('Branch %d' % k, path, [0.05, 0.036, 0.024, 0.014], 'canopy', 'heartwood' if k % 2 else 'bark', count=6)
        # A side twig off the branch's middle with a smaller bud.
        mid = path[1].lerp(path[2], 0.5)
        twig_dir = (path[2] - path[1]).normalized().cross(Vector((0, 1, 0))).normalized()
        if twig_dir.z < 0:
            twig_dir = -twig_dir
        twig = [mid, mid + twig_dir * 0.1 + Vector((0, 0, 0.06)), mid + twig_dir * 0.17 + Vector((0, 0, 0.13))]
        tube('Twig %d' % k, twig, [0.016, 0.011, 0.007], 'canopy', 'heartwood', count=5)
        for j, (tip, size, prev) in enumerate(((path[-1], 1.0, path[-2]), (twig[-1], 0.7, twig[-2]))):
            d = (tip - prev).normalized()
            centre = tip + d * 0.03 * size
            BUDS.append((centre, d))
            ellipsoid('Bud %d.%d' % (k, j), centre, (0.045 * size, 0.045 * size, 0.06 * size), 'canopy', 'bud', rings=5,
                      segments=7, chamfer=0.0,
                      axes=frame_axes(d))
            # Closed: three bark sepals clasp the bud (the first phase).
            u, v, w = frame_axes(d)
            for s in range(3):
                a = 2 * math.pi * s / 3
                out = u * math.cos(a) + v * math.sin(a)
                thorn('Sepal %d.%d.%d' % (k, j, s), centre - w * 0.04 * size + out * 0.03 * size, w * 0.9 + out * 0.35,
                      0.07 * size, 0.022 * size, 'canopy', 'bark', sides=3, show='cool')
            # Open: five orchid petals (the second phase).
            for s in range(5):
                a = 2 * math.pi * s / 5 + 0.3
                out = u * math.cos(a) + v * math.sin(a)
                leaf('Petal %d.%d.%d' % (k, j, s), centre - w * 0.02 * size, (out * 0.75 + w * 0.45), w, 0.11 * size,
                     0.045 * size, 'canopy', 'bud', curl=-0.25, show='hot', contour=True)
        # Thorns along the branch.
        for j, t in enumerate((0.35, 0.7)):
            p = path[1].lerp(path[2], t) if j else path[0].lerp(path[1], 0.6)
            thorn('Branch thorn %d.%d' % (k, j), p, Vector((0.3 if p.x >= 0 else -0.3, -0.2, 0.6)), 0.06, 0.012, 'canopy',
                  'bark', sides=3)


def frame_axes(d):
    w = Vector(d).normalized()
    ref = Vector((0, -1, 0)) if abs(w.y) < 0.9 else Vector((1, 0, 0))
    u = (ref - w * ref.dot(w)).normalized()
    v = w.cross(u)
    return (u, v, w)


def build_arms():
    """Long thin bark arms; the right fist knotted to grip the hook, the left hand three long root fingers."""
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
        tube('Upper arm ' + s, [sh, sh.lerp(el, 0.5), el], [0.055, 0.045, 0.04], 'upper_arm.' + s, 'bark', count=7,
             occluder=True)
        ellipsoid('Elbow ' + s, el, (0.05, 0.05, 0.05), 'forearm.' + s, 'bark', rings=4, segments=7)
        tube('Forearm ' + s, [el, el.lerp(wr, 0.5), wr], [0.042, 0.038, 0.032], 'forearm.' + s, 'bark', count=7)
        # Leaf cuffs at the wrist.
        for k in range(3):
            a = 2 * math.pi * k / 3
            out = Vector((math.cos(a), math.sin(a), 0))
            leaf('Cuff leaf %s%d' % (s, k), wr + out * 0.03, (out * 0.6 + Vector((0, 0, 0.8))), Vector((0, 0, 1)).cross(out),
                 0.1, 0.03, 'hand.' + s, 'leaf', curl=0.1)
        if side < 0:
            ellipsoid('Fist R', fi, (0.05, 0.045, 0.05), 'hand.R', 'root', rings=4, segments=7)
        else:
            for k, spread in enumerate((-0.5, 0.0, 0.5)):
                d = (fi - wr).normalized()
                across = Vector((0, 1, 0))
                path = [wr + d * 0.02, wr + d * 0.1 + across * spread * 0.03, wr + d * 0.19 + across * spread * 0.06 + Vector((0.02, 0, 0)),
                        wr + d * 0.27 + across * spread * 0.08 + Vector((0.05, 0, 0))]
                tube('Root finger %d' % k, path, [0.022, 0.017, 0.012, 0.004], 'hand.L', 'root', count=5)


HOOK_BLADE_C = Vector()


def build_hook():
    """The grafting hook: a long heartwood haft bound in leaf-wrap at its grip, a verdigris collar, and a curved
    verdigris blade - a pruning hook's crescent, its edge turned in - at the top. Built upright at its rest: the butt at
    HOOK_REST, the haft up +Z, the blade curving toward -Y (her front) at rest."""
    global HOOK_BLADE_C
    p0 = HOOK_REST
    up = Vector((0, 0, 1))
    tube('Hook haft', [p0 + up * t for t in (0.0, 0.5, 1.0, HAFT)], [0.022, 0.024, 0.024, 0.026], 'hook', 'heartwood',
         count=7, chamfer=0.0)
    tube('Hook grip', [p0 + up * 0.78, p0 + up * 1.02], [0.03, 0.03], 'hook', 'leaf', count=7, chamfer=0.0)
    tube('Hook collar', [p0 + up * (HAFT - 0.04), p0 + up * (HAFT + 0.03)], [0.036, 0.036], 'hook', 'verdigris', count=7,
         chamfer=0.0)
    # The blade: a crescent from the heel up and over toward the front, then down - a strip lofted along the arc, wide at
    # the heel and narrowing to a point, thin across.
    heel = p0 + up * (HAFT + 0.03)
    centre = heel + Vector((0, -0.2, 0.06))
    radius = 0.25
    rings = []
    steps = 9
    for i in range(steps + 1):
        t = i / steps
        a = math.radians(lerp(-20, 215, t))      # round the centre, in the haft's plane (y, z)
        p = centre + Vector((0, math.cos(a) * radius, math.sin(a) * radius))
        inward = (centre - p).normalized()
        width = lerp(0.1, 0.006, t ** 1.25)
        thickness = lerp(0.02, 0.005, t)
        across = Vector((1, 0, 0))
        rings.append([p, p + inward * width + across * thickness * 0.5, p + inward * width * 1.05,
                      p + inward * width - across * thickness * 0.5])
    loft('Hook blade', rings, 'hook', 'verdigris', cap_start=True, cap_end=True, chamfer=0.0)
    HOOK_BLADE_C = centre + Vector((0, 0.0, radius * 0.9))


def build_eruption():
    """The root strike's roots bursting up round the blade where it lands: five roots and a ring of torn earth-bark
    shards, built round ERUPT_REST on their own bone; shown only in the strike's frames."""
    c = ERUPT_REST
    for k in range(5):
        a = 2 * math.pi * k / 5 + 0.4
        out = Vector((math.cos(a), math.sin(a), 0))
        h = 0.5 + 0.12 * (k % 3)
        path = [c + out * 0.14 + Vector((0, 0, -0.1)), c + out * 0.18 + Vector((0, 0, h * 0.4)),
                c + out * 0.12 + Vector((0, 0, h * 0.8)), c + out * 0.02 + Vector((0, 0, h))]
        tube('Erupting root %d' % k, path, [0.05, 0.04, 0.026, 0.006], 'erupt', 'root', count=6, show='strike')
        thorn('Root thorn %d' % k, path[2], out * 0.8 + Vector((0, 0, 0.4)), 0.07, 0.014, 'erupt', 'root', sides=3,
              show='strike')
    for k in range(7):
        a = 2 * math.pi * k / 7
        out = Vector((math.cos(a), math.sin(a), 0))
        thorn('Torn shard %d' % k, c + out * 0.3, out * 0.6 + Vector((0, 0, 0.8)), 0.09, 0.03, 'erupt', 'bark', sides=4,
              show='strike')


build_skirt()
build_skirt_core()
build_body()
build_canopy()
build_arms()
build_hook()
build_eruption()

# The floor: a holdout, so what sinks under it (her dying body, the roots before they burst) is not drawn.
floor_mesh = bpy.data.meshes.new('Floor holdout')
floor_mesh.from_pydata([(-6, -6, 0), (6, -6, 0), (6, 6, 0), (-6, 6, 0)], [], [(0, 1, 2, 3)])
FLOOR = bpy.data.objects.new('Floor holdout', floor_mesh)
collection.objects.link(FLOOR)
FLOOR.data.materials.append(MATERIALS['holdout'])

for name, obj in PARTS.items():
    obj.data.materials.append(MATERIALS[INFO[name]['material']])


# ---------------------------------------------------------------------------------------------------------------------
# Rig: every part rides one bone rigidly
# ---------------------------------------------------------------------------------------------------------------------
arm_data = bpy.data.armatures.new('Rootmother rig')
RIG = bpy.data.objects.new('Rootmother Myrr', arm_data)
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
bone('pelvis', (0, 0, PELVIS_Z), (0, 0, WAIST_Z), 'root')
bone('spine', (0, 0, WAIST_Z), (0, 0, CHEST_Z), 'pelvis')
bone('chest', (0, 0, CHEST_Z), (0, 0, NECK_Z), 'spine')
bone('neck', (0, 0, NECK_Z), tuple(HEAD_BASE), 'chest')
bone('head', tuple(HEAD_BASE), (HEAD_BASE.x, HEAD_BASE.y, 1.86), 'neck')
bone('canopy', (0, 0.12, 1.45), (0, 0.18, 1.95), 'chest')
for side, s in ((1, 'L'), (-1, 'R')):
    sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
    bone('upper_arm.' + s, sh, el, 'chest')
    bone('forearm.' + s, el, wr, 'upper_arm.' + s)
    bone('hand.' + s, wr, fi + (fi - wr).normalized() * 0.05, 'forearm.' + s)
for k, deg in enumerate(SKIRT_ANGLES):
    hem = around(deg, 0.5, 0.03, 0.43)
    bone('skirt.%d' % k, (hem.x * 0.42, hem.y * 0.42, WAIST_Z), tuple(hem), 'pelvis')
bone('hook', tuple(HOOK_REST), tuple(HOOK_REST + Vector((0, 0, HAFT))), 'root')
bone('erupt', tuple(ERUPT_REST), tuple(ERUPT_REST + Vector((0, 0, 0.5))), 'root')
bpy.ops.object.mode_set(mode='OBJECT')
REST = {b.name: b.matrix_local.copy() for b in arm_data.bones}
REST_TAIL = {b.name: b.tail_local.copy() for b in arm_data.bones}
PARENT = {b.name: (b.parent.name if b.parent else None) for b in arm_data.bones}


def depth(name):
    d = 0
    while PARENT[name]:
        name = PARENT[name]
        d += 1
    return d


ORDER = sorted(REST, key=lambda b: (depth(b), b))
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
    mesh = obj.data
    attr = mesh.attributes.new('facet', 'FLOAT', 'FACE')
    smooth_part = any(poly.use_smooth for poly in mesh.polygons)
    for i in range(len(mesh.polygons)):
        h = math.sin(((0 if smooth_part else i) + 1) * 12.9898 + seed * 78.233) * 43758.5453
        attr.data[i].value = 0.5 + 0.4 * (h - math.floor(h) - 0.5) if smooth_part else h - math.floor(h)


for k, obj in enumerate(PARTS.values()):
    facet_attribute(obj, k)


# ---------------------------------------------------------------------------------------------------------------------
# Posing (BuildBlenderGlassRegent.py's solver, without legs: she walks on her roots): rotations about each bone's head
# in the rig's rest space, accumulated down the hierarchy; the hands by two-bone reach; the hook and the eruption placed
# outright.
# ---------------------------------------------------------------------------------------------------------------------
def rot(x=0.0, y=0.0, z=0.0):
    return (Matrix.Rotation(math.radians(z), 3, 'Z') @ Matrix.Rotation(math.radians(y), 3, 'Y')
            @ Matrix.Rotation(math.radians(x), 3, 'X'))


class Pose:
    def __init__(self):
        self.rot = {}
        self.offset = Vector()
        self.hands = {}
        self.free = {}
        self.flare = 1.0
        self.life = 1.0

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


def hook_placement(grip_target, direction, grip_s, turn=ACROSS):
    """The hook's G: the haft point grip_s from the butt lands on grip_target, the haft points along direction (butt to
    blade), the blade's plane turned by turn."""
    d = Vector(direction).normalized()
    a = Vector(turn) - d * Vector(turn).dot(d)
    a.normalize()
    r = Matrix((a, d.cross(a), d)).transposed().to_4x4()
    rest_grip = HOOK_REST + Vector((0, 0, grip_s))
    return Matrix.Translation(Vector(grip_target)) @ r @ Matrix.Translation(-rest_grip)


def on_hook(p, s):
    return p.free['hook'] @ (HOOK_REST + Vector((0, 0, s)))


def erupt_placement(at, scale):
    """The eruption's G: its roots round the point at, risen to scale of their height (0 under the floor, 1 burst)."""
    return (Matrix.Translation(Vector(at)) @ Matrix.Diagonal((1.0, 1.0, max(0.02, scale), 1.0)) @
            Matrix.Translation(-ERUPT_REST))


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
            if b.startswith('upper_arm') and side in self.pose.hands:
                pending.update(self.arm(side))
            G_total = self.pose.free.get(b)
            if b == 'erupt' and G_total is None:
                G_total = erupt_placement(Vector((0, 0, -2)), 0.02)
            D = self.place(b, pending.get(b), G_total)
            basis[b] = REST[b].inverted() @ D @ REST[b]
        return basis

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
    set_state(flare=pose.flare, life=pose.life)
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


def skirt(p, flare=0.0, sink=0.0):
    """Every root panel turned out from her body by flare degrees (rearing), and down onto the floor by sink (dying)."""
    for k, deg in enumerate(SKIRT_ANGLES):
        out = around(deg, 1.0, 0.0)
        tilt = out.cross(Vector((0, 0, 1))).normalized()       # the panel's hinge, tangent round her waist
        p.add('skirt.%d' % k, Matrix.Rotation(math.radians(-(flare + sink)), 3, tilt))


def stance(breath=0.0, sway=0.0):
    """Hunched over her garden: the body leaning forward, the long neck carrying the mask up to look ahead, the canopy
    over her back; the hook held upright as a staff in the right hand, its blade over her right shoulder; the root hand
    hanging forward, fingers loose."""
    p = Pose()
    p.offset = Vector((0, 0, -0.005 + 0.008 * breath))
    p.add('spine', rot(x=7))
    p.add('chest', rot(x=11 - 2 * breath))
    p.add('neck', rot(x=-9))
    p.add('head', rot(x=-6 + breath, z=2 * sway))
    p.add('canopy', rot(x=-6 + 1.5 * breath, y=2.5 * sway))
    butt = Vector((-0.47, -0.16, 0.02))
    p.free['hook'] = hook_placement(butt, Vector((-0.05, -0.06, 1.0)), 0.0)
    p.hands['R'] = (on_hook(p, 0.95), Vector((-0.9, 0.5, -0.2)))
    p.hands['L'] = (Vector((0.36, -0.3, 0.95 + 0.01 * breath)), Vector((0.6, 0.8, -0.2)))
    return p


def idle(name):
    breath = 1.0 if name == 'idle-b' else 0.0
    p = stance(breath, sway=1.0 if name == 'idle-b' else -1.0)
    skirt(p, flare=1.5 * breath)
    return p


def windup(name):
    """"Be still. Be planted." - she gathers (0), rears up with the hook raised high and her root hand thrust out at the
    hero, the canopy spread (1), and quivers at its peak, the roots of her skirt lifting (2). Her buds flare."""
    p = Pose()
    stage = WINDUPS.index(name)
    if stage == 0:
        p.offset = Vector((0, 0.03, -0.05))
        p.add('spine', rot(x=12))
        p.add('chest', rot(x=16, z=-8))
        p.add('neck', rot(x=-12))
        p.add('head', rot(x=-6))
        p.add('canopy', rot(x=-2))
        grip = Vector((-0.42, 0.18, 1.05))
        p.free['hook'] = hook_placement(grip, Vector((-0.25, 0.35, 0.9)), 0.95)
        p.hands['R'] = (grip, Vector((-0.9, 0.2, -0.3)))
        p.hands['L'] = (Vector((0.3, -0.42, 0.72)), Vector((0.7, 0.6, -0.3)))
        skirt(p, flare=4)
        p.flare = 1.25
    else:
        peak = stage == 2
        p.offset = Vector((0, 0.04, 0.04 if peak else 0.02))
        p.add('spine', rot(x=-3))
        p.add('chest', rot(x=-6 if peak else -3, z=6))
        p.add('neck', rot(x=-4))
        p.add('head', rot(x=-14 if peak else -10, z=-4))
        p.add('canopy', rot(x=-14 if peak else -10, y=3 if peak else 0))
        grip = Vector((-0.34, 0.02, 1.98 if peak else 1.9))
        p.free['hook'] = hook_placement(grip, Vector((-0.12, 0.2 if peak else 0.1, 1.0)), 0.9)
        p.hands['R'] = (grip, Vector((-0.9, 0.2, -0.3)))
        p.hands['L'] = (Vector((0.3, -0.66, 1.18 if peak else 1.26)), Vector((0.8, 0.3, -0.6)))
        skirt(p, flare=12 if peak else 8)
        p.flare = 1.85 if peak else 1.55
    return p


def strike(name, degrees):
    """The root strike: the hook swung down (0), its blade driven into the floor ahead with roots bursting up round it
    (1), and dragged back as the roots sink (2). The blade lands ahead of her and to the screen's left (where the facing
    looks)."""
    stage = STRIKES.index(name)
    p = Pose()
    land = local_floor(-0.95, -0.12 if degrees > 180 else 0.2, degrees)
    toward = land.normalized()
    yaw = math.degrees(math.atan2(toward.x, -toward.y))
    bend = (14, 26, 14)[stage]
    p.offset = Vector((0, 0, (-0.03, -0.08, -0.04)[stage])) + toward * (0.03, 0.08, 0.04)[stage]
    p.add('spine', rot(x=bend * 0.5, z=yaw * 0.25))
    p.add('chest', rot(x=bend, z=yaw * 0.25))
    p.add('neck', rot(x=-bend * 0.6))
    p.add('head', rot(x=-4))
    p.add('canopy', rot(x=bend * 0.4))
    if stage == 0:
        grip = toward * 0.25 + Vector((0, 0, 1.45))
        blade_dir = toward * 0.9 + Vector((0, 0, 0.45))
        p.free['hook'] = hook_placement(grip, blade_dir, 0.95)
    else:
        # The blade's heel to the floor at the landing point: the haft running back up to her grip.
        blade = land + Vector((0, 0, 0.12))
        grip = toward * (0.32 if stage == 1 else 0.2) + Vector((0, 0, 0.86 if stage == 1 else 0.98))
        reach = (blade - grip).length
        p.free['hook'] = hook_placement(grip, blade - grip, HAFT - reach)
        p.free['erupt'] = erupt_placement(land * (1.0 if stage == 1 else 0.98), 1.0 if stage == 1 else 0.45)
    p.hands['R'] = (on_hook(p, 0.95), Vector((-0.9, 0.4, -0.3)))
    p.hands['L'] = (on_hook(p, 0.95 + 0.24), Vector((0.9, 0.2, -0.4))) if stage > 0 else \
        (Vector((0.34, -0.2, 1.1)), Vector((0.6, 0.8, -0.2)))
    skirt(p, flare=(6, 10, 4)[stage])
    p.flare = (1.4, 1.0, 1.0)[stage]
    return p


def phase(name):
    """The phase change at half her health: she rears back, arms flung wide (0, the first phase's colours); the canopy
    bursts into bloom, her head thrown back (1); she settles, blooming, into her stance (2)."""
    stage = PHASES.index(name)
    if stage == 2:
        p = stance(0.5, sway=0.0)
        skirt(p, flare=3)
        p.flare = 1.2
        return p
    p = Pose()
    p.offset = Vector((0, 0.05, 0.03 if stage == 1 else 0.0))
    p.add('spine', rot(x=-6))
    p.add('chest', rot(x=-12 if stage == 1 else -8))
    p.add('neck', rot(x=-8))
    p.add('head', rot(x=-22 if stage == 1 else -10))
    p.add('canopy', rot(x=-16 if stage == 1 else -8))
    grip = Vector((-0.72, -0.05, 1.42 if stage == 1 else 1.3))
    p.free['hook'] = hook_placement(grip, Vector((-0.45, 0.0, 1.0)), 0.95)
    p.hands['R'] = (grip, Vector((-0.6, 0.3, -0.8)))
    p.hands['L'] = (Vector((0.7, -0.08, 1.52 if stage == 1 else 1.38)), Vector((0.6, 0.3, -0.8)))
    skirt(p, flare=14 if stage == 1 else 9)
    p.flare = 2.0 if stage == 1 else 1.4
    return p


FALL_LIFE = {'fall-struck': (1.0, 1.6), 'fall-sink': (0.6, 1.0), 'fall-slump': (0.3, 1.0), 'fall-stump': (0.1, 1.0)}


def fall(name, degrees):
    """Her death ("Something... is growing underneath."): rocked back by the killing blow (struck); sinking into her own
    roots, the skirt splaying, bowed over the hook planted beside her (sink); slumping forward as the canopy droops (slump);
    and a stump with its canopy fallen over it, its blooms dark (stump). The floor's holdout hides what sinks under it."""
    p = Pose()
    p.life, p.flare = FALL_LIFE[name]
    ahead = local_floor(-0.55, 0.1 if degrees > 180 else 0.2, degrees)
    if name == 'fall-struck':
        p.offset = Vector((0, 0.08, -0.02))
        p.add('spine', rot(x=-6))
        p.add('chest', rot(x=-12, y=-4))
        p.add('neck', rot(x=-6))
        p.add('head', rot(x=-24, z=10))
        p.add('canopy', rot(x=-12))
        grip = Vector((-0.5, -0.05, 1.05))
        p.free['hook'] = hook_placement(grip, Vector((-0.35, -0.2, 1.0)), 0.95)
        p.hands['R'] = (grip, Vector((-0.9, 0.3, -0.3)))
        p.hands['L'] = (Vector((0.55, 0.05, 1.25)), Vector((0.6, 0.8, -0.2)))
        skirt(p, flare=8)
        return p
    sink = {'fall-sink': 0.32, 'fall-slump': 0.58, 'fall-stump': 0.8}[name]
    bow = {'fall-sink': 22, 'fall-slump': 40, 'fall-stump': 62}[name]
    droop = {'fall-sink': 20, 'fall-slump': 42, 'fall-stump': 70}[name]
    p.offset = Vector((0, -0.04, -sink))
    p.add('spine', rot(x=bow * 0.4))
    p.add('chest', rot(x=bow * 0.6))
    p.add('neck', rot(x=bow * 0.5))
    p.add('head', rot(x=10, z=-8))
    p.add('canopy', rot(x=droop, y=-6))
    skirt(p, sink=12 + sink * 70)
    butt = ahead + Vector((0, 0, 0.0))
    if name == 'fall-stump':
        # The hook fallen flat beside the stump.
        p.free['hook'] = hook_placement(ahead + Vector((0, 0, 0.03)), Vector((0.3, -1.0, 0.03)), 0.0)
        p.hands['R'] = (Vector((-0.3, -0.35, 0.3)), Vector((-0.9, 0.2, 0.4)))
        p.hands['L'] = (Vector((0.3, -0.35, 0.28)), Vector((0.9, 0.2, 0.4)))
    else:
        p.free['hook'] = hook_placement(butt, Vector((0.08, 0.1, 1.0)), 0.0)
        p.hands['R'] = (on_hook(p, 0.75 - sink * 0.6), Vector((-0.9, 0.3, -0.3)))
        p.hands['L'] = (Vector((0.3, -0.38, 0.7 - sink * 0.6)), Vector((0.6, 0.8, -0.2)))
    return p


def pose_for(name, variant, degrees=FACINGS[0][1]):
    if name in IDLES:
        return idle(name)
    if name in WINDUPS:
        return windup(name)
    if name in STRIKES:
        return strike(name, degrees)
    if name in PHASES:
        return phase(name)
    return fall(name, degrees)


# ---------------------------------------------------------------------------------------------------------------------
# Baked contact occlusion: rays from every vertex in the stance, against the parts that do not swing about.
# ---------------------------------------------------------------------------------------------------------------------
def bake_occlusion():
    apply(idle('idle-a'))
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
    count = 32
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
    for name, obj in PARTS.items():
        show = INFO[name]['show']
        if show == 'hot':
            visible = variant == 'hot'
        elif show == 'cool':
            visible = variant != 'hot'
        elif show == 'strike':
            visible = pose_name in ('strike-1', 'strike-2')
        else:
            visible = True
        obj.hide_render = not visible


def variants_for(pose_name):
    if pose_name in FALLS or pose_name in ('phase-1', 'phase-2'):
        wanted = ['hot']
    elif pose_name == 'phase-0':
        wanted = ['cool']
    else:
        wanted = VARIANTS
    return [v for v in wanted if v in VARIANT_SET]


def clip_of(pose_name):
    return pose_name.split('-')[0]


def render_all():
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    started = time.time()
    for facing, degrees in FACING_SET:
        set_facing(degrees)
        for pose_name in POSE_SET:
            if pose_name in SHARED_FALLS and facing != 'front':
                continue
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
                records.append(dict(facing=facing, pose=pose_name, clip=clip_of(pose_name), variant=variant,
                                    seconds=CLIP_SECONDS[clip_of(pose_name)],
                                    file=str((OUT / facing / (stem + '.png')).relative_to(OUT)).replace('\\', '/'),
                                    glow=str((OUT / facing / (stem + '_glow.png')).relative_to(OUT)).replace('\\', '/'),
                                    heart=screen_point(solver, HEART_C, 'chest'),
                                    head=screen_point(solver, HEAD_C, 'head'),
                                    blade=screen_point(solver, HOOK_BLADE_C, 'hook')))
                print('ROOTMOTHER_FRAME', facing, pose_name, variant, '%.1fs' % (time.time() - started), flush=True)
    meta = dict(blender=bpy.app.version_string, ppu=PPU, supersample=SS, canvas=list(CANVAS_PX), pivot=list(PIVOT_PX),
                elevation_degrees=30, height_compensation=HEIGHT, scale=SCALE, glow_scale=GLOW_SCALE, channels=CHANNELS,
                facings={f: d for f, d in FACINGS}, poses=POSES, clip_seconds=CLIP_SECONDS, frames=records)
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
        action = bpy.data.actions.new('Rootmother ' + name)
        action.use_fake_user = True
        RIG.animation_data.action = action
        apply(pose_for(name, 'cool'))
        for pb in RIG.pose.bones:
            pb.keyframe_insert('location', frame=1)
            pb.keyframe_insert('rotation_quaternion', frame=1)
            pb.keyframe_insert('scale', frame=1)
    RIG.animation_data.action = bpy.data.actions['Rootmother idle-a']


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
apply(idle('idle-a'))
set_state(heat=0.0, glow_pass=0.0)
show_for('cool', 'idle-a')     # the saved scene shows the first phase; set Heat to 1 and unhide the petals for the second
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND), compress=True)
v, t = budget()
print('ROOTMOTHER_COMPLETE', 'parts', len(PARTS), 'vertices', v, 'triangles', t, flush=True)
