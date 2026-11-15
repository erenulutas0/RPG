"""Floor 3's two horde creatures (docs/52 sections 3 and 4, docs/51): reproducible stylised models, rigs, poses and the
sprite renders for their EnemyArtSets, drawn in the garden's palette from the start (born on floor 3: no themed copy).

    blender --background --factory-startup --python Tools/ArtReview/BuildBlenderGloamHorde.py -- --creature NAME [options]

Options after the "--":
    --creature NAME  wraith (the Pollen Wraith, the Cinder Mite's slot) or thornling (the Thornling, the Runner's)
    --out DIR        raw frames and frames.json (default TestResults/gloam-horde-render/<creature>, git-ignored)
    --blend PATH     the saved scene (default TestResults/gloam-horde-render/<creature>.blend)
    --poses LIST     a comma-separated subset of the creature's poses
    --facings LIST   a comma-separated subset of front,rear
    --samples N      Cycles samples per render pixel (default 10)
    --no-render      build and save the .blend, render nothing

THE POLLEN WRAITH (the Mite's contract: idle, step A, step B, strike a facing, the strike windup, the two remains, its
own death of seven drawings a facing). What the memory-trees shed: a small pale-orchid mote drifting a hand over the
loam - a teardrop puff of pollen-down (`#F6C2F0` lit) cupped from below by six orchid bracts, a dark seed core
(`#5A1F66`) on its face like a single eye, a crown of five pappus wisps, three filaments trailing behind. Pale ORCHID,
never ash-white (Ilya's robe is ash-white, docs/52 section 3). It drifts (the step drawings bob it and sway the
filaments), snaps (the strike: lunged forward, bracts flared like a closing flower) and bursts (its death: the puff pops,
the wisps scatter, the spent husk falls and greys). The seed core is a separate part the beauty pass draws black: the aux
pass writes its coverage and tone and the glow round it, so PackGloamHordeSprites.py lays its plum ramp and the pale
light about it (and a faint halo round the whole puff) without rendering again (docs/52 "Art": the core a separate
emissive part).

THE THORNLING (the Runner's contract: six bodies a facing - idle, step A, step B, strike, pass A, pass B - the strike
windup, the two remains and its own death of five drawings a facing; plus, outside the set, the dive, the eruption and
the stuck pose). The Runner's thorned cousin: a lean, hunched runner of dark bark (`#4A3B33`, darker than the Runner's
recolour: Ilya's robe), long root legs, long arms ending in three thorn claws, a ridge of thorns down its back, three
swept thorns on its narrow skull and a root tail; the thorns' tips orchid (the only light on it). It lopes low (the
walk is a run), rakes with its right claws, dives head first into the ash (dive-0..2), bursts out of it (erupt-0..2)
and sticks, claws caught in the ash (stuck-0..1). Under the floor everything is held out: a holdout plane at the floor
cuts the body where the ash would, so the dive and the eruption need no masks.

Nothing here is generated imagery: lofted rings, plates and primitives, rigid parts riding bones, the Wraith's puff,
bracts, wisps and filaments rebuilt from a formula for each pose (constant topology), one authored toon node group
(BuildBlenderCinderBloom.py's), every frame a pose computed below. Camera, scale and facings as
BuildBlenderCinderBloom.py: orthographic, 30 degrees down, 128 texels a world unit, both facings looking left.
No red-orange (the telegraphs') and no gloamlight (the garden's) anywhere on either (docs/52 section 2 rule 3).
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


CREATURE = option('--creature', 'wraith')
assert CREATURE in ('wraith', 'thornling'), CREATURE
OUT = Path(option('--out', str(ROOT / 'TestResults/gloam-horde-render' / CREATURE)))
BLEND = Path(option('--blend', str(ROOT / 'TestResults/gloam-horde-render' / (CREATURE + '.blend'))))
SAMPLES = int(option('--samples', 10))
RENDER = not option('--no-render', False)

PPU = 128
SS = 4
ELEVATION = math.radians(30)
HEIGHT = 1 / math.cos(ELEVATION)
FACINGS = [('front', 245), ('rear', 125)]

if CREATURE == 'wraith':
    CANVAS_PX = (144, 128)
    PIVOT_PX = (72, 30)
    DEATH_CANVAS_PX = (160, 128)
    DEATH_PIVOT_PX = (80, 40)
    SCALE = 0.82           # small: the most numerous body on the floor (the Mite stands about 0.5 tall)
    STRIDE = 1.1            # world units a drift cycle (two bobs) covers: EnemyArtSet._walkCycleDistance
    BODY_POSES = ['idle', 'step-a', 'step-b', 'strike', 'windup']
    EXTRA_POSES = []
    DYING = ['dying-%d' % k for k in range(7)]
    REMAINS = ['death-husk', 'death-dust']
else:
    CANVAS_PX = (240, 224)
    PIVOT_PX = (120, 64)
    DEATH_CANVAS_PX = (256, 208)
    DEATH_PIVOT_PX = (128, 64)
    SCALE = 1.0
    STRIDE = 1.0            # the Runner's 1.0
    BODY_POSES = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b', 'windup']
    EXTRA_POSES = ['dive-0', 'dive-1', 'dive-2', 'erupt-0', 'erupt-1', 'erupt-2', 'stuck-0', 'stuck-1']
    DYING = ['dying-%d' % k for k in range(5)]
    REMAINS = ['death-collapse', 'death-cool']
POSES = BODY_POSES + EXTRA_POSES + DYING + REMAINS

wanted_poses = option('--poses')
wanted_facings = option('--facings')
POSE_SET = [p for p in POSES if not wanted_poses or p in wanted_poses.split(',')]
FACING_SET = [f for f in FACINGS if not wanted_facings or f[0] in wanted_facings.split(',')]


def lin(hexstr, alpha=1.0):
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
# Scene (BuildBlenderCinderBloom.py's)
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

RIGHT = Vector((1, 0, 0))
UP = Vector((0, math.sin(ELEVATION), math.cos(ELEVATION)))
FORWARD = Vector((0, math.cos(ELEVATION), -math.sin(ELEVATION)))
TOWARD = -FORWARD
KEY = (-0.55 * RIGHT + 0.76 * UP + 0.34 * TOWARD).normalized()
RIM = (-0.62 * RIGHT + 0.58 * UP - 0.52 * TOWARD).normalized()
SHADE_RIM = (0.58 * RIGHT - 0.25 * UP - 0.78 * TOWARD).normalized()
HALF = (KEY + TOWARD).normalized()

STATE_NAMES = (('Life', 1.0), ('Aux pass', 0.0), ('Preview', 0.0))


def build_state_group():
    ng = bpy.data.node_groups.new('Horde state', 'ShaderNodeTree')
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


def build_toon_group():
    """BuildBlenderCinderBloom.py's toon group, unchanged: two soft bands on a half-lambert key, a brushed value drift,
    bark fissures where Grain is set, baked contact occlusion, an ash rim and a moss-shade rim, a small specular."""
    ng = bpy.data.node_groups.new('Horde toon', 'ShaderNodeTree')
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
    fissure = n.new('ShaderNodeTexWave')
    fissure.wave_type = 'BANDS'
    fissure.bands_direction = 'X'
    fissure.inputs['Scale'].default_value = 9.0
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
    facing = n.new('ShaderNodeLayerWeight')
    facing.inputs['Blend'].default_value = 0.4
    rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.5, 0.82), math_node('MAXIMUM', dot(normal, vec(RIM)), 0.0))
    rim = math_node('MULTIPLY', rim, gi.outputs['Rim'])
    colour = add_colour(colour, rgb('#B9B8A4'), math_node('MULTIPLY', rim, 0.45))
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
    # The Wraith: pale orchid pollen-down (docs/52: #F6C2F0 lit), never ash-white - the mid and shade stay plainly
    # orchid so the mote never sits at Ilya's robe's value AND hue.
    'puff': ('#F6C2F0', '#E09AD8', '#AE62AE', '#4E1F58'),
    'wisp': ('#FAD6F4', '#E8AAE2', '#B672B6', '#4E1F58'),
    'bract': ('#E39CDC', '#C26CBE', '#86408E', '#34123F'),
    'filament': ('#E7A6E0', '#C47AC2', '#8A4892', '#34123F'),
    # The Thornling: dark bark (docs/52 #4A3B33 the shade), a step below the Runner's recolour; limbs darker still.
    'thorn-bark': ('#7A6758', '#55463C', '#3F322B', '#14100D'),
    'thorn-limb': ('#6C5B4F', '#4A3B33', '#342922', '#110C0A'),
    'thorn-root': ('#6A5A4E', '#463831', '#312620', '#100B09'),
}
OUTLINES = {'wraith': '#1C1020', 'thornling': '#120F0E'}
OUTLINE = OUTLINES[CREATURE]


class Shading:
    """BuildBlenderCinderBloom.py's: a toon material emitted once (the scene has no lights); in the aux pass every
    material emits black except the aux family (red coverage, green tone, blue glow)."""

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

    def dot_world(self, direction):
        geo = self.nodes.new('ShaderNodeNewGeometry')
        key = self.nodes.new('ShaderNodeCombineXYZ')
        key.inputs[0].default_value, key.inputs[1].default_value, key.inputs[2].default_value = direction
        d = self.nodes.new('ShaderNodeVectorMath')
        d.operation = 'DOT_PRODUCT'
        self.links.new(geo.outputs['Normal'], d.inputs[0])
        self.links.new(key.outputs[0], d.inputs[1])
        return d.outputs['Value']

    def lit(self):
        return self.math('MULTIPLY_ADD', self.dot_world(KEY), 0.5, 0.5)

    def glint(self):
        """A small hard specular spot (the toon's): a wet seed's highlight."""
        half = self.math('MAXIMUM', self.dot_world(HALF), 0.0)
        r = self.nodes.new('ShaderNodeMapRange')
        r.interpolation_type = 'SMOOTHSTEP'
        self.links.new(self.math('POWER', half, 24.0), r.inputs['Value'])
        r.inputs['From Min'].default_value = 0.35
        r.inputs['From Max'].default_value = 0.6
        return r.outputs['Result']

    def finish(self, colour, aux=None):
        final = self.mix(self.state('Aux pass'), colour, aux if aux is not None else self.rgb('#000000'))
        e = self.nodes.new('ShaderNodeEmission')
        self.links.new(final, e.inputs['Color'])
        e.inputs['Strength'].default_value = 1.0
        self.links.new(e.outputs[0], self.out.inputs['Surface'])
        return self.mat


def plain(name, palette, aux_glow=None, **kw):
    """A toon material; aux_glow writes that much glow (blue) in the aux pass with no coverage, so the packer can bloom
    light from the part (the Wraith's puff) without colouring it."""
    s = Shading(name)
    aux = s.combine(0.0, 0.0, aux_glow) if aux_glow else None
    return s.finish(s.toon(palette, **kw), aux=aux)


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


def seed_core_material():
    """The Wraith's seed core: black in the beauty; in the aux pass red its coverage, green its tone (a dark plum seed,
    the key lifting it a little, a hard wet glint) and blue its glow (the pale light round it), times its life."""
    s = Shading('Seed core (tuned in the packer)')
    life = s.state('Life')
    tone = s.math('ADD', s.math('MULTIPLY_ADD', s.lit(), 0.22, 0.12), s.math('MULTIPLY', s.glint(), 0.75), clamp=True)
    beauty = s.mix(s.state('Preview'), s.rgb('#000000'), s.rgb('#5A1F66'))
    return s.finish(beauty, aux=s.combine(1.0, tone, s.math('MULTIPLY', life, 1.0)))


def thorn_tip_material():
    """The Thornling's thorn tips: black in the beauty; in the aux pass coverage, a tone rising base to tip and with the
    key (the vertex attribute 'tone' times the light), and a little glow at the tip, times its life."""
    s = Shading('Thorn tips (tuned in the packer)')
    life = s.state('Life')
    tone = s.math('MULTIPLY', s.attribute('tone'), s.math('MULTIPLY_ADD', s.lit(), 0.7, 0.5), clamp=True)
    beauty = s.mix(s.state('Preview'), s.rgb('#000000'), s.rgb('#D86BD0'))
    glow = s.math('MULTIPLY', s.math('MULTIPLY', s.attribute('tone'), 0.5), life)
    return s.finish(beauty, aux=s.combine(1.0, tone, glow))


def eyes_material():
    s = Shading('Eyes')
    life = s.state('Life')
    beauty = s.mix(s.state('Preview'), s.rgb('#000000'), s.rgb('#D86BD0'))
    return s.finish(beauty, aux=s.combine(1.0, s.math('MULTIPLY_ADD', life, 0.55, 0.25),
                                          s.math('MULTIPLY', life, 0.6)))


def holdout_material():
    """The ash floor: holds out (transparent) whatever lies under the floor in both passes."""
    mat = bpy.data.materials.new('Ash floor (holdout)')
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    for node in list(nodes):
        nodes.remove(node)
    out = nodes.new('ShaderNodeOutputMaterial')
    hold = nodes.new('ShaderNodeHoldout')
    mat.node_tree.links.new(hold.outputs[0], out.inputs['Surface'])
    return mat


MATERIALS = {}


def build_materials():
    m = MATERIALS
    m['outline'] = outline_material()
    m['holdout'] = holdout_material()
    if CREATURE == 'wraith':
        m['puff'] = plain('Pollen down', 'puff', aux_glow=0.32, spec=0.12, spec_color='#FFF2FC', rim=0.3)
        m['wisp'] = plain('Pappus wisps', 'wisp', aux_glow=0.25, rim=0.3)
        m['bract'] = plain('Bracts', 'bract', spec=0.1, spec_color='#FBE3F8', rim=0.35, band_shift=0.02)
        m['filament'] = plain('Filaments', 'filament', aux_glow=0.12, rim=0.3)
        m['core'] = seed_core_material()
    else:
        m['bark'] = plain('Thorn bark', 'thorn-bark', spec=0.08, rim=0.5, grain=0.45)
        m['limb'] = plain('Root limbs', 'thorn-limb', spec=0.06, rim=0.45, grain=0.35)
        m['root'] = plain('Root tail and talons', 'thorn-root', spec=0.05, rim=0.4, grain=0.5)
        m['tip'] = thorn_tip_material()
        m['eyes'] = eyes_material()


build_materials()

# ---------------------------------------------------------------------------------------------------------------------
# Geometry helpers (BuildBlenderCinderBloom.py's)
# ---------------------------------------------------------------------------------------------------------------------
PARTS = {}
INFO = {}


def new_object(name, verts, faces, bone, material, contour=True, chamfer=0.006, occluder=False, flat=True, show=None,
               facet=True):
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
    INFO[name] = dict(bone=bone, material=material, contour=contour, chamfer=chamfer, occluder=occluder, show=show,
                      facet=facet)
    return obj


def loft_data(rings, closed=True, cap_start=False, cap_end=False):
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
    return verts, faces


def loft(name, rings, bone, material, closed=True, cap_start=False, cap_end=False, **kw):
    verts, faces = loft_data(rings, closed, cap_start, cap_end)
    return new_object(name, verts, faces, bone, material, **kw)


def ring_points(cx, cy, z, rx, ry, count, start=-math.pi / 2, keel=0.0, back=0.0):
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


def ellipsoid_data(centre, radii, rings=6, segments=10, axes=None, lower=-1.0, upper=1.0, warp=None):
    """The ellipsoid's verts and faces; warp(lat, lon, point) may move a point (the Wraith's teardrop tip)."""
    ax = axes or (Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1)))
    verts, faces = [], []
    lats = [math.asin(lerp(lower, upper, j / rings)) for j in range(rings + 1)]
    for lat in lats:
        for i in range(segments):
            lon = 2 * math.pi * i / segments
            p = Vector(centre) + (ax[0] * (radii[0] * math.cos(lat) * math.cos(lon)) + ax[1] * (radii[1] * math.cos(lat) * math.sin(lon))
                                  + ax[2] * (radii[2] * math.sin(lat)))
            verts.append(warp(lat, lon, p) if warp else p)
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
        p = Vector(centre) - ax[2] * radii[2]
        verts.append(warp(-math.pi / 2, 0.0, p) if warp else p)
        c = len(verts) - 1
        for i in range(segments):
            faces.append((c, (i + 1) % segments, i))
    if upper >= 0.999:
        p = Vector(centre) + ax[2] * radii[2]
        verts.append(warp(math.pi / 2, 0.0, p) if warp else p)
        c = len(verts) - 1
        for i in range(segments):
            faces.append((c, rings * segments + i, rings * segments + (i + 1) % segments))
    return verts, faces


def ellipsoid(name, centre, radii, bone, material, rings=6, segments=10, axes=None, lower=-1.0, upper=1.0, warp=None,
              **kw):
    verts, faces = ellipsoid_data(centre, radii, rings, segments, axes, lower, upper, warp)
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
            normal = (prev_normal - tangent * prev_normal.dot(tangent))
            normal = normal.normalized() if normal.length > 1e-9 else prev_normal
        prev_normal = normal
        binormal = tangent.cross(normal)
        r = radii[k]
        rx, ry = (r, r) if isinstance(r, (int, float)) else r
        rings.append([p + normal * (rx * math.cos(2 * math.pi * i / count)) + binormal * (ry * math.sin(2 * math.pi * i / count))
                      for i in range(count)])
    return rings


def tube_data(path, radii, count=8, cap_start=True, cap_end=True, twist_ref=Vector((0, -1, 0))):
    return loft_data(tube_rings(path, radii, count, twist_ref), True, cap_start, cap_end)


def tube(name, path, radii, bone, material, count=8, cap_start=True, cap_end=True, twist_ref=Vector((0, -1, 0)), **kw):
    verts, faces = tube_data(path, radii, count, cap_start, cap_end, twist_ref)
    return new_object(name, verts, faces, bone, material, **kw)


def plate_data(rows, thickness, inward):
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
    return verts, faces


def plate(name, rows, thickness, bone, material, inward, **kw):
    verts, faces = plate_data(rows, thickness, inward)
    return new_object(name, verts, faces, bone, material, **kw)


def thorn(name, base, direction, length, radius, bone, tip_share=0.42, bend=None, material='limb', count=6):
    """A thorn: a tapering cone along direction (curving toward bend over its length), its base bark and its tip an
    orchid part of its own (the 'tone' attribute rising base to tip) - the Thornling's only light."""
    d = Vector(direction).normalized()
    b = Vector(bend).normalized() if bend is not None else d
    steps = 6
    pts = []
    for k in range(steps + 1):
        s = k / steps
        dd = (d * (1 - s * 0.6) + b * (s * 0.6)).normalized()
        pts.append(dd)
    path = [Vector(base)]
    for k in range(1, steps + 1):
        path.append(path[-1] + pts[k] * (length / steps))
    cut = int(round(steps * (1 - tip_share)))
    radii = [radius * (1 - 0.92 * k / steps) + 0.002 for k in range(steps + 1)]
    tube(name, path[:cut + 1], radii[:cut + 1], bone, material, count=count, chamfer=0.0, cap_start=True, cap_end=True)
    obj = tube(name + ' tip', path[cut:], [r * 1.02 for r in radii[cut:]], bone, 'tip', count=count, chamfer=0.0,
               contour=True, cap_start=True, cap_end=True)
    n = len(obj.data.vertices)
    rings = steps + 1 - cut
    tones = []
    for j in range(rings):
        tones.extend([0.55 + 0.45 * j / max(1, rings - 1)] * count)
    tones += [0.55, 1.0]                       # the caps' centres: the start's, then the tip's
    assert len(tones) == n, (name, len(tones), n)
    attr = obj.data.attributes.new('tone', 'FLOAT', 'POINT')
    attr.data.foreach_set('value', tones)
    return obj


def mirror_x(v):
    return Vector((-v.x, v.y, v.z))


def side_point(v, side):
    return v if side > 0 else mirror_x(v)


# ---------------------------------------------------------------------------------------------------------------------
# The Pollen Wraith: formula meshes on one 'body' bone
# ---------------------------------------------------------------------------------------------------------------------
W_CENTRE = Vector((0, 0, 0.34))     # the puff's centre at rest: a hand over the loam, a clear gap over its shadow
W_R = (0.142, 0.132, 0.134)
BRACTS, BRACT_ROWS, BRACT_COLS = 6, 6, 4
WISPS = 7
FILAMENTS, FIL_RINGS = 3, 8


def wraith_shapes(p):
    """Every formula part's verts (and faces) for a pose: the puff, the seed core, the bracts, the wisps and their
    seed-balls, the filaments. In the body bone's rest space (the bone carries the pose's lean and drift)."""
    out = {}
    size = p.size
    sx, sy, sz = (p.squash[0] * size, p.squash[1] * size, p.squash[2] * size)
    c = W_CENTRE
    rx, ry, rz = W_R[0] * sx, W_R[1] * sy, W_R[2] * sz

    def tip(lat, lon, q):
        # A teardrop of pollen-down: the crown drawn up to a soft point, the flanks scalloped into soft lobes so the
        # silhouette reads as down, not an egg.
        k = max(0.0, math.sin(lat)) ** 3
        lobe = 1 + 0.1 * math.cos(lat) ** 2 * math.sin(7 * lon + 1.6 * lat + 0.4)
        r = q - c
        return c + Vector((r.x * lobe, r.y * lobe, r.z)) + Vector((0, 0, 0.032 * size * k))

    out['Puff'] = ellipsoid_data(c, (rx, ry, rz), rings=10, segments=24, warp=tip)
    core_c = c + Vector((0, -ry * 0.84, 0.028 * sz))
    out['Seed core'] = ellipsoid_data(core_c, (0.054 * size * p.core, 0.04 * size * p.core, 0.064 * size * p.core),
                                      rings=6, segments=12)
    # Bracts: orchid scales cupping the puff from below, peeling out as it opens (the strike, the burst).
    bottom = c + Vector((0, 0, -rz * 0.96))
    for i in range(BRACTS):
        a = 2 * math.pi * i / BRACTS + math.pi / BRACTS + math.radians(8)
        radial = Vector((math.sin(a), -math.cos(a), 0))
        tangent = Vector((math.cos(a), math.sin(a), 0))
        rows = []
        for k in range(BRACT_ROWS):
            s = k / (BRACT_ROWS - 1)
            phi = lerp(0.18, 1.38, s)                      # from the bottom pole round toward the equator
            surf = c + radial * (math.sin(phi) * lerp(rx, ry, abs(radial.y))) + Vector((0, 0, -math.cos(phi) * rz))
            peel = p.open * 0.085 * size * s * s + 0.012 * size
            droop = p.droop * 0.12 * size * s * s
            centre_pt = surf + radial * peel + Vector((0, 0, -droop))
            w = 0.07 * size * math.sin(math.pi * min(1.0, 0.12 + 0.88 * s ** 0.85)) ** 0.75 * (1 - 0.15 * s)
            row = []
            for col in range(BRACT_COLS):
                u = -1 + 2 * col / (BRACT_COLS - 1)
                cup = 0.01 * size * (1 - u * u)
                row.append(centre_pt + tangent * (u * w) + radial * cup)
            rows.append(row)
        out['Bract %d' % i] = plate_data(rows, 0.012 * size, lambda q, c=c: (q - c).normalized())
    # Pappus wisps on the crown, each ending in a small seed-ball: the "pollen" in the silhouette.
    crown = c + Vector((0, 0, rz + 0.03 * size))
    for k in range(WISPS):
        az = 2 * math.pi * k / WISPS + 0.35
        tilt = math.radians(lerp(28, 62, p.spread) + (8 if k % 2 else 0))
        d0 = Vector((math.sin(tilt) * math.sin(az), -math.sin(tilt) * math.cos(az), math.cos(tilt)))
        length = (0.06 + 0.025 * (k % 2)) * size
        lift = p.detach * Vector((d0.x, d0.y, 0.6)) * 0.09
        path = [crown + lift * 0.4]
        for j in range(1, 4):
            s = j / 3
            dd = (d0 + Vector((d0.x, d0.y, -0.6)) * (0.35 * s * p.droop_wisps)).normalized()
            path.append(path[-1] + dd * (length / 3) + lift * (0.2 * s))
        out['Wisp %d' % k] = tube_data(path, [0.017 * size, 0.014 * size, 0.01 * size, 0.008 * size], count=6)
        ball = path[-1] + (path[-1] - path[-2]).normalized() * 0.012 * size + lift * 0.5
        out['Wisp ball %d' % k] = ellipsoid_data(ball, (0.021 * size, 0.021 * size, 0.019 * size), rings=4, segments=8)
    # Filaments: trailing behind (+Y, away from where it drifts), swaying.
    for f in range(FILAMENTS):
        side = (-1, 0, 1)[f]
        start = bottom + Vector((0.035 * side * size, 0.02 * size, 0.01))
        length = (0.17, 0.21, 0.15)[f] * size * p.tail
        path = []
        for j in range(FIL_RINGS):
            s = j / (FIL_RINGS - 1)
            back = p.drag * 0.75 * s ** 1.3
            down = (1 - p.drag * 0.72) * s
            # Each filament curls its own way (a wisp of smoke, not a jellyfish's comb), the sway swinging them.
            curl = 0.18 * math.sin(math.pi * s * 1.6 + f * 2.1) * s
            sway = p.sway * 0.35 * math.sin(math.pi * s * 1.2 + f * 0.9) * s + side * 0.22 * s + curl
            path.append(start + Vector((sway, back, -down)) * length)
        radii = [(0.02 * (1 - 0.88 * j / (FIL_RINGS - 1)) + 0.0025) * size for j in range(FIL_RINGS)]
        out['Filament %d' % f] = tube_data(path, radii, count=6)
    return out


def build_wraith():
    p = Pose()
    shapes = wraith_shapes(p)
    for name, (verts, faces) in shapes.items():
        if name == 'Puff':
            new_object(name, verts, faces, 'body', 'puff', chamfer=0.0, flat=False, occluder=True, facet=False)
        elif name == 'Seed core':
            new_object(name, verts, faces, 'body', 'core', chamfer=0.0, flat=False, contour=True)
        elif name.startswith('Bract'):
            new_object(name, verts, faces, 'body', 'bract', chamfer=0.0, flat=False)
        elif name.startswith('Wisp ball'):
            new_object(name, verts, faces, 'body', 'wisp', chamfer=0.0, flat=False, facet=False,
                       show=lambda q: q.wisps)
        elif name.startswith('Wisp'):
            new_object(name, verts, faces, 'body', 'wisp', chamfer=0.0, flat=False, facet=False,
                       show=lambda q: q.wisps)
        else:
            new_object(name, verts, faces, 'body', 'filament', chamfer=0.0, flat=False, facet=False)


def shape_wraith(pose):
    for name, (verts, _) in wraith_shapes(pose).items():
        rebuild(PARTS[name], verts)


# ---------------------------------------------------------------------------------------------------------------------
# The Thornling: rigid bark and thorn parts on a biped rig
# ---------------------------------------------------------------------------------------------------------------------
ANKLE_Z, KNEE_Z, HIP_Z = 0.06, 0.25, 0.45
HIP_X = 0.075
WAIST_Z = 0.53
CHEST_Z = 0.64
NECK_Z = 0.8
SHOULDER = Vector((0.12, 0.0, 0.775))
ELBOW = Vector((0.15, 0.03, 0.59))
WRIST = Vector((0.16, 0.0, 0.42))
FIST = Vector((0.165, -0.01, 0.38))
HEAD_BASE = Vector((0, -0.04, 0.84))
HEAD_TOP = 0.96


def build_thornling():
    # Legs: long root legs, a knotted knee, a talon foot of two root toes and a back spur.
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        x = HIP_X * side
        tube('Thigh ' + s, [Vector((x, 0.0, HIP_Z)), Vector((x, -0.01, (HIP_Z + KNEE_Z) / 2)), Vector((x, 0.0, KNEE_Z + 0.02))],
             [0.05, 0.046, 0.04], 'thigh.' + s, 'limb', count=8, occluder=True)
        ellipsoid('Knee ' + s, Vector((x, -0.02, KNEE_Z + 0.01)), (0.042, 0.042, 0.04), 'shin.' + s, 'bark', rings=4,
                  segments=8)
        tube('Shin ' + s, [Vector((x, 0.0, KNEE_Z)), Vector((x, 0.0, (KNEE_Z + ANKLE_Z) / 2)), Vector((x, 0.0, ANKLE_Z))],
             [0.038, 0.032, 0.03], 'shin.' + s, 'limb', count=8, occluder=True)
        for k, a in enumerate((-18, 16)):
            d = Vector((math.sin(math.radians(a + 6 * side)), -math.cos(math.radians(a + 6 * side)), 0))
            base = Vector((x, -0.01, ANKLE_Z - 0.01))
            tube('Toe %s %d' % (s, k), [base, base + d * 0.06 + Vector((0, 0, -0.03)), base + d * 0.11 + Vector((0, 0, -0.05))],
                 [0.022, 0.016, 0.006], 'foot.' + s, 'root', count=6, chamfer=0.0)
        tube('Heel spur ' + s, [Vector((x, 0.01, ANKLE_Z)), Vector((x, 0.06, ANKLE_Z - 0.03))], [0.018, 0.005],
             'foot.' + s, 'root', count=5, chamfer=0.0)
    # The pelvis knot and a root tail.
    ellipsoid('Pelvis', Vector((0, 0.0, HIP_Z + 0.02)), (0.1, 0.085, 0.07), 'pelvis', 'bark', rings=5, segments=10,
              occluder=True)
    tail = [Vector((0, 0.06, HIP_Z + 0.01)), Vector((0, 0.15, HIP_Z - 0.04)), Vector((0, 0.24, HIP_Z - 0.06)),
            Vector((0.02, 0.32, HIP_Z - 0.04))]
    tube('Root tail', tail, [0.04, 0.03, 0.018, 0.006], 'pelvis', 'root', count=7, chamfer=0.0)
    thorn('Tail thorn', Vector((0, 0.17, HIP_Z - 0.02)), (0, 0.4, 1), 0.07, 0.016, 'pelvis', bend=(0, 1, 0.4))
    # The waist and a lean ribbed chest of bark plates, a keel down its front.
    loft('Waist', [ring_points(0, 0.0, z, rx, ry, 10) for z, rx, ry in ((HIP_Z + 0.04, 0.08, 0.07), (WAIST_Z, 0.065, 0.058),
                                                                         (CHEST_Z - 0.02, 0.075, 0.066))],
         'spine', 'limb', cap_start=True, cap_end=True, occluder=True)
    loft('Chest', [ring_points(0, 0.005, z, rx, ry, 12, keel=0.02, back=0.02) for z, rx, ry in
                   ((CHEST_Z - 0.03, 0.085, 0.07), (CHEST_Z + 0.05, 0.115, 0.085), (0.74, 0.125, 0.09),
                    (NECK_Z, 0.1, 0.075), (NECK_Z + 0.025, 0.06, 0.05))],
         'chest', 'bark', cap_start=True, cap_end=True, occluder=True, chamfer=0.005)
    # Rib plates over the chest's flanks.
    for side in (-1, 1):
        for k, z in enumerate((CHEST_Z + 0.02, CHEST_Z + 0.075)):
            rows = []
            for dz in (0.022, 0.0, -0.022):
                rows.append([Vector((side * (0.1 + 0.012 * u), -0.07 + 0.06 * u, z + dz - 0.01 * u)) for u in (0.0, 0.5, 1.0)])
            plate('Rib %d %d' % (side, k), rows, 0.012, 'chest', 'bark',
                  inward=lambda q: Vector((q.x, q.y, 0)).normalized(), chamfer=0.0)
    # The back's ridge of thorns, curving back and up, longest over the shoulders.
    ridge = [('spine', 0.5, 0.075, 0.016), ('spine', 0.58, 0.095, 0.018), ('chest', 0.66, 0.12, 0.022),
             ('chest', 0.73, 0.15, 0.025), ('chest', 0.79, 0.13, 0.022)]
    for k, (bone_name, z, length, radius) in enumerate(ridge):
        thorn('Back thorn %d' % k, Vector((0, 0.06 + 0.02 * (z > 0.6), z)), (0, 0.75, 0.6), length, radius, bone_name,
              bend=(0, 1.0, 0.25))
    for side in (-1, 1):
        thorn('Flank thorn %+d' % side, Vector((side * 0.07, 0.05, 0.7)), (side * 0.6, 0.6, 0.5), 0.09, 0.016, 'chest',
              bend=(side * 0.5, 0.8, 0.1))
    # Neck and the narrow skull, two orchid eye slits, three swept crown thorns.
    tube('Neck', [Vector((0, 0.0, NECK_Z)), Vector((0, -0.02, (NECK_Z + HEAD_BASE.z) / 2 + 0.01)), HEAD_BASE + Vector((0, 0, 0.02))],
         [0.04, 0.036, 0.034], 'neck', 'limb', count=8)
    hc = HEAD_BASE + Vector((0, 0, 0.05))
    # The skull: a narrow wedge pointing forward (-Y), rings across x and z stacked along y.
    levels = ((0.06, 0.012, 0.042, 0.04), (0.02, 0.012, 0.06, 0.052), (-0.04, 0.0, 0.056, 0.047),
              (-0.1, -0.014, 0.036, 0.03), (-0.15, -0.024, 0.014, 0.012))
    rings = [[hc + Vector((rx * math.cos(2 * math.pi * i / 10), y, dz + rz * math.sin(2 * math.pi * i / 10)))
              for i in range(10)] for y, dz, rx, rz in levels]
    loft('Skull', rings, 'head', 'bark', cap_start=True, cap_end=True, occluder=True)
    box('Jaw', hc + Vector((0, -0.07, -0.04)), (0.06, 0.1, 0.022), 'head', 'limb', taper=0.8, chamfer=0.004)
    for side in (-1, 1):
        box('Eye %+d' % side, hc + Vector((0.034 * side, -0.075, 0.012)), (0.026, 0.01, 0.01), 'head', 'eyes',
            contour=False, chamfer=0.0,
            axes=(Vector((1, 0, -0.35 * side)).normalized(), Vector((0, 1, 0)), Vector((0.35 * side, 0, 1)).normalized()))
    for k, (x, length) in enumerate(((-0.035, 0.13), (0.0, 0.17), (0.035, 0.13))):
        thorn('Crown thorn %d' % k, hc + Vector((x, 0.0, 0.035)), (x * 6, 0.7, 0.75), length, 0.021, 'head',
              bend=(x * 4, 1.0, 0.1))
    # A collar of thorns over the chest's top, so the creature reads as thorned from the front as well.
    for side in (-1, 1):
        for k, (x, z, length) in enumerate(((0.05, 0.79, 0.08), (0.095, 0.755, 0.07))):
            thorn('Collar thorn %+d %d' % (side, k), Vector((side * x, -0.05, z)), (side * 0.35, -0.45, 0.85), length,
                  0.016, 'chest', bend=(side * 0.4, -0.1, 1.0))
        thorn('Chest thorn %+d' % side, Vector((side * 0.07, -0.08, 0.69)), (side * 0.55, -0.8, 0.25), 0.06, 0.014,
              'chest', bend=(side * 0.6, -0.4, 0.7))
    # Arms: lean, an elbow spur, a forearm of two bark splints and three thorn claws.
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
        ellipsoid('Shoulder ' + s, sh + Vector((0.01 * side, 0.0, 0.01)), (0.05, 0.05, 0.045), 'upper_arm.' + s, 'bark',
                  rings=4, segments=8)
        thorn('Shoulder thorn ' + s, sh + Vector((0.02 * side, 0.02, 0.03)), (side * 0.45, 0.35, 0.8), 0.085, 0.016,
              'upper_arm.' + s, bend=(side * 0.3, 0.8, 0.5))
        tube('Upper arm ' + s, [sh + (el - sh) * t for t in (0.1, 0.55, 1.0)], [0.032, 0.03, 0.028], 'upper_arm.' + s,
             'limb', count=7)
        thorn('Elbow spur ' + s, el + Vector((0, 0.02, 0)), (side * 0.3, 1.0, 0.3), 0.07, 0.015, 'forearm.' + s,
              bend=(side * 0.2, 0.7, 0.7))
        tube('Forearm ' + s, [el + (wr - el) * t for t in (0.05, 0.6, 1.0)], [0.03, 0.034, 0.03], 'forearm.' + s, 'bark',
             count=7)
        mid = el + (wr - el) * 0.45
        thorn('Forearm thorn ' + s, mid + Vector((side * 0.025, 0.0, 0)), (side * 1.0, 0.2, 0.35), 0.06, 0.013,
              'forearm.' + s, bend=(side * 0.6, 0.0, 0.8))
        hand = wr + (fi - wr) * 0.5
        ellipsoid('Hand ' + s, hand, (0.03, 0.028, 0.03), 'hand.' + s, 'limb', rings=3, segments=7)
        down = (fi - wr).normalized()
        for k, spread in enumerate((-0.45, 0.0, 0.45)):
            d = (down + Vector((spread * 0.5 * side, -0.55, 0)) + Vector((spread * 0.3, 0, 0))).normalized()
            thorn('Claw %s %d' % (s, k), hand + d * 0.02, d, 0.1, 0.012, 'hand.' + s, tip_share=0.3,
                  bend=(d + Vector((0, -0.6, -0.3))).normalized(), material='root', count=5)


def rebuild(obj, verts):
    mesh = obj.data
    flat = [c for v in verts for c in v]
    mesh.vertices.foreach_set('co', flat)
    mesh.update()


# ---------------------------------------------------------------------------------------------------------------------
# Rig
# ---------------------------------------------------------------------------------------------------------------------
class Pose:
    def __init__(self):
        self.rot = {}
        self.offset = Vector()
        self.feet = {}
        self.hands = {}
        self.free = {}
        self.knee_poles = {}
        self.life = 1.0
        self.glow = 1.0           # the aux family's value for the packer (the dying dim)
        self.ash = 0.0            # how far the packer greys the drawing (a spent husk)
        self.floor = False        # settle the lowest point onto the floor
        self.sink = False         # the ash floor holds out what lies under it (the dive, the eruption, stuck)
        # The Wraith's formula: squash (x, y, z), size, how far the bracts peel and droop, the wisps' spread and
        # droop, whether they show, how far they have flown, the filaments' sway, drag and length, the core's size.
        self.squash = (1.0, 1.0, 1.0)
        self.size = 1.0
        self.open = 0.15
        self.droop = 0.0
        self.spread = 0.4
        self.droop_wisps = 1.0
        self.wisps = True
        self.detach = 0.0
        self.sway = 0.0
        self.drag = 0.85
        self.tail = 1.0
        self.core = 1.0

    def add(self, bone_name, m):
        self.rot[bone_name] = m @ self.rot.get(bone_name, Matrix.Identity(3))


if CREATURE == 'wraith':
    build_wraith()
else:
    build_thornling()

arm_data = bpy.data.armatures.new('Rig')
RIG = bpy.data.objects.new('Pollen Wraith' if CREATURE == 'wraith' else 'Thornling', arm_data)
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
if CREATURE == 'wraith':
    bone('body', tuple(W_CENTRE), tuple(W_CENTRE + Vector((0, 0, 0.15))), 'root')
    OFFSET_BONE = 'body'
else:
    OFFSET_BONE = 'pelvis'
    bone('pelvis', (0, 0, HIP_Z), (0, 0, WAIST_Z), 'root')
    bone('spine', (0, 0, WAIST_Z), (0, 0, CHEST_Z), 'pelvis')
    bone('chest', (0, 0, CHEST_Z), (0, 0, NECK_Z), 'spine')
    bone('neck', (0, 0, NECK_Z), tuple(HEAD_BASE), 'chest')
    bone('head', tuple(HEAD_BASE), (HEAD_BASE.x, HEAD_BASE.y, HEAD_TOP), 'neck')
    for side, s in ((1, 'L'), (-1, 'R')):
        sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
        bone('upper_arm.' + s, sh, el, 'chest')
        bone('forearm.' + s, el, wr, 'upper_arm.' + s)
        bone('hand.' + s, wr, fi + (fi - wr).normalized() * 0.05, 'forearm.' + s)
        bone('thigh.' + s, (HIP_X * side, 0, HIP_Z), (HIP_X * side, 0, KNEE_Z), 'pelvis')
        bone('shin.' + s, (HIP_X * side, 0, KNEE_Z), (HIP_X * side, 0.0, ANKLE_Z), 'thigh.' + s)
        bone('foot.' + s, (HIP_X * side, 0.0, ANKLE_Z), (HIP_X * side, -0.1, 0.02), 'shin.' + s)
bpy.ops.object.mode_set(mode='OBJECT')
REST = {b.name: b.matrix_local.copy() for b in arm_data.bones}
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

CONTOUR = 0.0075 if CREATURE == 'wraith' else 0.0085


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
    if not INFO[obj.name]['facet']:
        attr.data.foreach_set('value', [0.5] * len(mesh.polygons))      # soft down: no carved facets
        return
    for i in range(len(mesh.polygons)):
        h = math.sin((i + 1) * 12.9898 + seed * 78.233) * 43758.5453
        attr.data[i].value = h - math.floor(h)


for k, obj in enumerate(PARTS.values()):
    facet_attribute(obj, k)


def rot(x=0.0, y=0.0, z=0.0):
    """Degrees about the rig's axes: +x pitches a raised bone forward (toward -Y), +y leans it to the creature's left,
    +z turns it to the left."""
    return (Matrix.Rotation(math.radians(z), 3, 'Z') @ Matrix.Rotation(math.radians(y), 3, 'Y')
            @ Matrix.Rotation(math.radians(x), 3, 'X'))


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
    """BuildBlenderCinderBloom.py's: deltas about each bone's head in rest space, accumulated down the hierarchy; legs
    and arms by two-bone IK."""

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
        if b == OFFSET_BONE:
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


FLOOR_PLANE = None


def apply(pose):
    solver = Solver(pose)
    basis = solver.solve()
    for b, m in basis.items():
        RIG.pose.bones[b].matrix_basis = m
    set_state(life=pose.life)
    if CREATURE == 'wraith':
        shape_wraith(pose)
    for name, obj in PARTS.items():
        show = INFO[name]['show']
        visible = True if show is None else bool(show(pose))
        obj.hide_render = not visible
        obj.hide_viewport = not visible
    if FLOOR_PLANE is not None:
        FLOOR_PLANE.hide_render = not pose.sink
        FLOOR_PLANE.hide_viewport = not pose.sink
    return solver


# ---------------------------------------------------------------------------------------------------------------------
# The poses
# ---------------------------------------------------------------------------------------------------------------------
def wraith_pose(name):
    p = Pose()
    if name == 'idle':
        p.offset = Vector((0, 0, 0.0))
        p.sway, p.drag = 0.2, 0.85
    elif name == 'step-a':
        # The drift's high beat: risen, a little stretched, the filaments swung one way.
        p.offset = Vector((0, 0, 0.03))
        p.squash = (0.96, 0.96, 1.05)
        p.sway, p.drag = 1.0, 0.92
        p.add('body', rot(x=6))
        p.spread = 0.3
    elif name == 'step-b':
        # The low beat: sunk and spread, the filaments swung the other way and lifted behind.
        p.offset = Vector((0, 0, -0.02))
        p.squash = (1.06, 1.05, 0.93)
        p.sway, p.drag = -1.0, 1.0
        p.add('body', rot(x=10))
        p.spread = 0.55
    elif name == 'windup':
        # Drawn back and up before the snap: stretched tall, bracts clenched, the wisps bristling.
        p.offset = Vector((0, 0.05, 0.035))
        p.squash = (0.9, 0.92, 1.12)
        p.add('body', rot(x=-14))
        p.open = -0.1
        p.spread = 1.0
        p.droop_wisps = 0.2
        p.sway, p.drag = 0.3, 0.2
        p.core = 1.08
    elif name == 'strike':
        # The snap: lunged forward and down, squashed into the blow, the bracts flared open like a flower closing on
        # its prey, the filaments streaming behind.
        p.offset = Vector((0, -0.09, -0.04))
        p.squash = (1.1, 1.0, 0.88)
        p.add('body', rot(x=26))
        p.open = 1.0
        p.spread = 0.75
        p.sway, p.drag = -0.4, 1.0
        p.tail = 1.1
        p.core = 1.12
    elif name.startswith('dying-') or name in REMAINS:
        k = int(name.split('-')[1]) if name.startswith('dying-') else 6
        # The burst: struck (squashed, flared, the core flaring), the puff popping and the wisps flying, the spent
        # husk falling, landing and greying.
        p.size = (1.0, 0.82, 0.62, 0.48, 0.4, 0.36, 0.34)[k]
        p.squash = ((1.14, 1.1, 0.84), (1.1, 1.08, 0.95), (1.0, 1.0, 1.0), (1.0, 1.0, 0.95), (1.05, 1.05, 0.85),
                    (1.1, 1.1, 0.75), (1.12, 1.12, 0.72))[k]
        p.open = (0.7, 1.1, 1.4, 1.5, 1.6, 1.7, 1.75)[k]
        p.droop = (0.0, 0.1, 0.4, 0.8, 1.0, 1.1, 1.15)[k]
        p.spread = (1.2, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0)[k]
        p.wisps = k <= 2
        p.detach = (0.0, 0.7, 1.4, 0, 0, 0, 0)[k]
        p.droop_wisps = 1.4
        p.offset = Vector((0, (0.04, 0.03, 0.02, 0.0, 0.0, 0.0, 0.0)[k], (0.0, -0.01, -0.06, -0.13, -0.19, -0.2, -0.2)[k]))
        p.add('body', rot(x=(-10, -6, 10, 25, 40, 55, 60)[k]))
        p.sway, p.drag = (0.6, -0.5, 0.2, 0.0, 0.0, 0.0, 0.0)[k], (0.3, 0.2, 0.1, 0.0, 0.0, 0.0, 0.0)[k]
        p.tail = (1.0, 0.9, 0.8, 0.7, 0.6, 0.55, 0.5)[k]
        p.glow = (1.6, 1.15, 0.8, 0.55, 0.35, 0.22, 0.15)[k]
        p.life = 1.0
        p.ash = (0.0, 0.0, 0.1, 0.22, 0.35, 0.45, 0.5)[k]
        p.floor = k >= 4
        if name == 'death-husk':
            p.glow, p.ash = 0.1, 0.55
        elif name == 'death-dust':
            p.size, p.squash, p.open, p.droop = 0.3, (1.2, 1.2, 0.55), 1.9, 1.25
            p.glow, p.ash = 0.0, 0.72
            p.tail = 0.4
    return p


def stance():
    """The Thornling's hunch: pitched forward from the hips, the head held level and low, knees bent, the claws
    hanging forward of the knees."""
    p = Pose()
    p.offset = Vector((0, 0.0, -0.06))
    p.add('pelvis', rot(x=22))
    p.add('spine', rot(x=12))
    p.add('chest', rot(x=10))
    p.add('neck', rot(x=-26))
    p.add('head', rot(x=-16))
    p.feet['L'] = (Vector((0.1, -0.02, ANKLE_Z)), 0, 10)
    p.feet['R'] = (Vector((-0.1, 0.06, ANKLE_Z)), 0, -12)
    p.knee_poles = {'L': Vector((0.15, -1, 0.1)), 'R': Vector((-0.15, -1, 0.1))}
    p.hands['L'] = (Vector((0.17, -0.2, 0.36)), Vector((0.5, 0.8, -0.2)))
    p.hands['R'] = (Vector((-0.17, -0.16, 0.38)), Vector((-0.5, 0.8, -0.2)))
    return p


def run(phase):
    """The lope: distance-driven, low and long, a beat of flight between the footfalls; the right foot's contact at 0;
    the claws swinging against the legs."""
    p = stance()
    bob = -0.035 * (0.5 + 0.5 * math.cos(4 * math.pi * phase))
    roll = math.sin(2 * math.pi * phase)
    p.offset = Vector((0.012 * roll, 0, bob - 0.06))
    excursion = STRIDE * 0.24
    duty = 0.42
    for s, offset, x in (('R', 0.0, -0.085), ('L', 0.5, 0.085)):
        q = (phase + offset) % 1.0
        if q < duty:
            t = q / duty
            y = lerp(-excursion, excursion, t)
            z = ANKLE_Z
            pitch = lerp(-6, 10, smooth(0.6, 1.0, t))
        else:
            t = (q - duty) / (1 - duty)
            y = lerp(excursion, -excursion, smooth(0.0, 1.0, t))
            z = ANKLE_Z + 0.13 * math.sin(math.pi * t) ** 1.1
            pitch = lerp(30, -10, t)
        p.feet[s] = (Vector((x, y, z)), pitch, 8 if s == 'L' else -8)
    p.add('pelvis', rot(x=4, y=-3 * roll, z=8 * roll))
    p.add('spine', rot(x=2, z=-4 * roll))
    p.add('chest', rot(y=2 * roll, z=-6 * roll))
    p.add('head', rot(z=-3 * roll))
    # The arms swing opposite the legs, low, the claws trailing.
    p.hands['L'] = (Vector((0.17, -0.18 - 0.17 * roll, 0.37 + 0.04 * abs(roll))), Vector((0.5, 0.8, -0.2)))
    p.hands['R'] = (Vector((-0.17, -0.18 + 0.17 * roll, 0.37 + 0.04 * abs(roll))), Vector((-0.5, 0.8, -0.2)))
    return p


def thornling_pose(name):
    if name == 'idle':
        return stance()
    if name in ('step-a', 'pass-a', 'step-b', 'pass-b'):
        return run({'step-a': 0.0, 'pass-a': 0.25, 'step-b': 0.5, 'pass-b': 0.75}[name])
    if name == 'windup':
        # The rake drawn back: the right claws raised high behind the shoulder, the body coiled and turned away.
        p = stance()
        p.offset = Vector((0, 0.04, -0.07))
        p.add('pelvis', rot(x=-4, z=-12))
        p.add('chest', rot(x=-6, z=-16))
        p.add('neck', rot(x=6, z=12))
        p.add('head', rot(z=8))
        p.feet['L'] = (Vector((0.11, -0.1, ANKLE_Z)), 0, 12)
        p.feet['R'] = (Vector((-0.1, 0.12, ANKLE_Z)), 0, -14)
        p.hands['R'] = (Vector((-0.22, 0.14, 0.86)), Vector((-0.8, 0.2, -0.6)))
        p.hands['L'] = (Vector((0.16, -0.24, 0.42)), Vector((0.5, 0.8, -0.2)))
        return p
    if name == 'strike':
        # The rake: lunged onto the left foot, the right claws swept forward and down across, the head driving in.
        p = stance()
        p.offset = Vector((0, -0.1, -0.1))
        p.add('pelvis', rot(x=10, z=10))
        p.add('spine', rot(x=8, z=6))
        p.add('chest', rot(x=6, z=10))
        p.add('neck', rot(x=-6, z=-8))
        p.add('head', rot(x=-6, z=-6))
        p.feet['L'] = (Vector((0.11, -0.24, ANKLE_Z)), 0, 8)
        p.feet['R'] = (Vector((-0.1, 0.2, ANKLE_Z + 0.01)), 18, -14)
        p.hands['R'] = (Vector((0.04, -0.48, 0.2)), Vector((-0.8, 0.4, -0.3)))
        p.hands['L'] = (Vector((0.22, 0.02, 0.4)), Vector((0.6, 0.6, -0.3)))
        return p
    if name.startswith('dive-'):
        k = int(name[-1])
        p = stance()
        p.sink = True
        if k == 0:
            # Gathering to dive: crouched deep, pitched over, both claws to the ash ahead.
            p.offset = Vector((0, -0.04, -0.15))
            p.add('pelvis', rot(x=22))
            p.add('spine', rot(x=10))
            p.add('neck', rot(x=10))
            p.add('head', rot(x=12))
            p.feet['L'] = (Vector((0.1, 0.02, ANKLE_Z)), 0, 10)
            p.feet['R'] = (Vector((-0.1, 0.08, ANKLE_Z)), 0, -10)
            p.hands['L'] = (Vector((0.07, -0.36, 0.03)), Vector((0.5, 0.6, 0.4)))
            p.hands['R'] = (Vector((-0.07, -0.34, 0.03)), Vector((-0.5, 0.6, 0.4)))
            p.glow = 1.0
        elif k == 1:
            # Head first into the ash: claws and head under, the hind legs kicking up behind.
            p.offset = Vector((0, -0.16, -0.38))
            p.add('pelvis', rot(x=48))
            p.add('spine', rot(x=12))
            p.add('neck', rot(x=24))
            p.add('head', rot(x=20))
            p.feet['L'] = (Vector((0.09, 0.2, 0.12)), 40, 6)
            p.feet['R'] = (Vector((-0.09, 0.26, 0.2)), 50, -6)
            p.knee_poles = {'L': Vector((0, 1, -0.3)), 'R': Vector((0, 1, -0.3))}
            p.hands['L'] = (Vector((0.08, -0.5, -0.25)), Vector((0.5, 0.6, 0.4)))
            p.hands['R'] = (Vector((-0.08, -0.48, -0.25)), Vector((-0.5, 0.6, 0.4)))
        else:
            # Nearly gone: the feet, the tail and the last back thorns over the ash.
            p.offset = Vector((0, -0.22, -0.6))
            p.add('pelvis', rot(x=70))
            p.add('spine', rot(x=10))
            p.add('neck', rot(x=20))
            p.feet['L'] = (Vector((0.07, 0.12, 0.14)), 80, 6)
            p.feet['R'] = (Vector((-0.07, 0.16, 0.2)), 90, -6)
            p.knee_poles = {'L': Vector((0, 1, -0.6)), 'R': Vector((0, 1, -0.6))}
            p.hands['L'] = (Vector((0.08, -0.5, -0.5)), Vector((0.5, 0.6, 0.4)))
            p.hands['R'] = (Vector((-0.08, -0.48, -0.5)), Vector((-0.5, 0.6, 0.4)))
        return p
    if name.startswith('erupt-'):
        k = int(name[-1])
        p = Pose()
        p.sink = True
        p.knee_poles = {'L': Vector((0.15, -1, 0.1)), 'R': Vector((-0.15, -1, 0.1))}
        if k == 0:
            # Breaking the ash: the crown thorns and the claws first, reaching up.
            p.offset = Vector((0, 0.02, -0.66))
            p.add('pelvis', rot(x=-6))
            p.add('chest', rot(x=-4))
            p.add('neck', rot(x=-20))
            p.add('head', rot(x=-24))
            p.feet['L'] = (Vector((0.1, 0.0, -0.6)), 0, 10)
            p.feet['R'] = (Vector((-0.1, 0.04, -0.6)), 0, -10)
            p.hands['L'] = (Vector((0.16, -0.12, 0.2)), Vector((0.7, 0.2, -0.6)))
            p.hands['R'] = (Vector((-0.16, -0.1, 0.24)), Vector((-0.7, 0.2, -0.6)))
        elif k == 1:
            # Half out, flung upright: the arms thrown up and wide, the head back.
            p.offset = Vector((0, 0.0, -0.3))
            p.add('pelvis', rot(x=-4))
            p.add('spine', rot(x=-6))
            p.add('chest', rot(x=-8))
            p.add('neck', rot(x=-14))
            p.add('head', rot(x=-20))
            p.feet['L'] = (Vector((0.1, 0.0, -0.26)), 0, 10)
            p.feet['R'] = (Vector((-0.1, 0.04, -0.26)), 0, -10)
            p.hands['L'] = (Vector((0.34, -0.06, 0.82)), Vector((0.6, 0.4, -0.6)))
            p.hands['R'] = (Vector((-0.34, -0.04, 0.78)), Vector((-0.6, 0.4, -0.6)))
        else:
            # Out and airborne: arched, the arms spread, the legs tucked under.
            p.offset = Vector((0, 0.0, 0.06))
            p.add('pelvis', rot(x=-8))
            p.add('spine', rot(x=-6))
            p.add('chest', rot(x=-6))
            p.add('neck', rot(x=-10))
            p.add('head', rot(x=-14))
            p.feet['L'] = (Vector((0.1, -0.04, 0.26)), 30, 10)
            p.feet['R'] = (Vector((-0.1, 0.06, 0.22)), 40, -10)
            p.hands['L'] = (Vector((0.36, -0.08, 0.7)), Vector((0.6, 0.4, -0.6)))
            p.hands['R'] = (Vector((-0.36, -0.06, 0.66)), Vector((-0.6, 0.4, -0.6)))
        return p
    if name.startswith('stuck-'):
        k = int(name[-1])
        p = stance()
        p.sink = True
        # Caught: crouched, both claws plunged into the ash ahead and held there, the feet sunk to the toes; the
        # second drawing heaves back against them.
        p.offset = Vector((0, 0.02 * k, (-0.16, -0.12)[k]))
        p.add('pelvis', rot(x=(26, 16)[k]))
        p.add('spine', rot(x=(12, 6)[k]))
        p.add('chest', rot(x=(8, -4)[k]))
        p.add('neck', rot(x=(-30, -6)[k]))
        p.add('head', rot(x=(-20, 8)[k], z=(0, -10)[k]))
        p.feet['L'] = (Vector((0.13, 0.02, 0.02)), 0, 16)
        p.feet['R'] = (Vector((-0.13, 0.08, 0.02)), 0, -16)
        p.knee_poles = {'L': Vector((0.5, -1, 0.1)), 'R': Vector((-0.5, -1, 0.1))}
        p.hands['L'] = (Vector((0.1, -0.3, -0.12)), Vector((0.6, 0.6, 0.2)))
        p.hands['R'] = (Vector((-0.1, -0.28, -0.12)), Vector((-0.6, 0.6, 0.2)))
        return p
    k = int(name.split('-')[1]) if name.startswith('dying-') else 4
    p = Pose()
    p.floor = k >= 3
    p.glow = (1.3, 0.9, 0.65, 0.45, 0.3)[k]
    p.life = (1.0, 0.75, 0.5, 0.3, 0.15)[k]
    if k == 0:
        # Struck: thrown back and up, the arms flung, the head snapped back.
        p.offset = Vector((0, 0.06, -0.03))
        p.add('pelvis', rot(x=-6))
        p.add('spine', rot(x=-10, z=6))
        p.add('chest', rot(x=-12, z=8))
        p.add('neck', rot(x=-10))
        p.add('head', rot(x=-24, z=-8))
        p.feet['L'] = (Vector((0.1, -0.06, ANKLE_Z)), 0, 12)
        p.feet['R'] = (Vector((-0.1, 0.12, ANKLE_Z + 0.04)), 20, -14)
        p.hands['L'] = (Vector((0.3, 0.06, 0.72)), Vector((0.6, 0.4, -0.7)))
        p.hands['R'] = (Vector((-0.28, 0.1, 0.66)), Vector((-0.6, 0.4, -0.7)))
    elif k == 1:
        # Buckling: the knees going, pitched forward, the claws dropping.
        p.offset = Vector((0, 0.02, -0.16))
        p.add('pelvis', rot(x=20))
        p.add('spine', rot(x=14, z=4))
        p.add('chest', rot(x=12))
        p.add('neck', rot(x=8))
        p.add('head', rot(x=16))
        p.feet['L'] = (Vector((0.1, -0.08, ANKLE_Z)), 0, 12)
        p.feet['R'] = (Vector((-0.1, 0.1, ANKLE_Z)), 0, -14)
        p.knee_poles = {'L': Vector((0.3, -1, 0)), 'R': Vector((-0.3, -1, 0))}
        p.hands['L'] = (Vector((0.18, -0.16, 0.18)), Vector((0.6, 0.8, -0.2)))
        p.hands['R'] = (Vector((-0.18, -0.12, 0.2)), Vector((-0.6, 0.8, -0.2)))
    elif k == 2:
        # On its knees, falling forward onto its claws.
        p.offset = Vector((0, -0.02, -0.28))
        p.add('pelvis', rot(x=34))
        p.add('spine', rot(x=18))
        p.add('chest', rot(x=12))
        p.add('neck', rot(x=10))
        p.add('head', rot(x=22))
        p.feet['L'] = (Vector((0.1, 0.16, 0.05)), 70, 8)
        p.feet['R'] = (Vector((-0.1, 0.16, 0.05)), 70, -8)
        p.knee_poles = {'L': Vector((0, -1, -0.6)), 'R': Vector((0, -1, -0.6))}
        p.hands['L'] = (Vector((0.16, -0.36, 0.04)), Vector((0.6, 0.6, -0.2)))
        p.hands['R'] = (Vector((-0.16, -0.32, 0.04)), Vector((-0.6, 0.6, -0.2)))
    else:
        # Down, then crumbling into the ash: flat on its front, the limbs splayed, the thorns dimming.
        settle_k = k - 3
        p.offset = Vector((0, 0.04 + 0.02 * settle_k, -0.36 - 0.02 * settle_k))
        p.add('pelvis', rot(x=62 + 10 * settle_k))
        p.add('spine', rot(x=14, y=-4))
        p.add('chest', rot(x=8, y=-4))
        p.add('neck', rot(x=-4))
        p.add('head', rot(x=6, y=-8, z=-10))
        p.feet['L'] = (Vector((0.14, 0.34, 0.05)), 80, 14)
        p.feet['R'] = (Vector((-0.12, 0.32, 0.05)), 80, -10)
        p.knee_poles = {'L': Vector((0, -1, -0.6)), 'R': Vector((0, -1, -0.6))}
        p.hands['L'] = (Vector((0.3, -0.36, 0.04)), Vector((0.8, 0.3, 0.3)))
        p.hands['R'] = (Vector((-0.26, -0.42, 0.04)), Vector((-0.7, 0.4, 0.3)))
    if name == 'death-collapse':
        p.glow, p.life = 0.2, 0.1
    elif name == 'death-cool':
        p.glow, p.life, p.ash = 0.06, 0.0, 0.25
    return p


def pose_for(name):
    return wraith_pose(name) if CREATURE == 'wraith' else thornling_pose(name)


# ---------------------------------------------------------------------------------------------------------------------
# Baked contact occlusion (BuildBlenderCinderBloom.py's)
# ---------------------------------------------------------------------------------------------------------------------
def bake_occlusion():
    apply(pose_for('idle'))
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
    reach = 0.08
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

# The ash floor: a holdout plane on the floor, shown only for the poses under it.
if CREATURE == 'thornling':
    mesh = bpy.data.meshes.new('Ash floor')
    mesh.from_pydata([(-3, -3, 0), (3, -3, 0), (3, 3, 0), (-3, 3, 0)], [], [(0, 1, 2, 3)])
    FLOOR_PLANE = bpy.data.objects.new('Ash floor', mesh)
    collection.objects.link(FLOOR_PLANE)
    mesh.materials.append(MATERIALS['holdout'])
    FLOOR_PLANE.hide_render = True

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
    deps = bpy.context.evaluated_depsgraph_get()
    low = 1e9
    for name, obj in PARTS.items():
        if obj.hide_render:
            continue
        ev = obj.evaluated_get(deps)
        mesh = ev.to_mesh()
        m = ev.matrix_world
        for v in mesh.vertices:
            low = min(low, (m @ v.co).z)
        ev.to_mesh_clear()
    return low


def settle(pose):
    RIG.location = (0, 0, 0)
    bpy.context.view_layer.update()
    if pose.floor:
        RIG.location = (0, 0, 0.004 - lowest_z())
        bpy.context.view_layer.update()


def screen_point(solver, rest_point, bone_name):
    world = Matrix.LocRotScale(RIG.location, RIG.rotation_euler, RIG.scale) @ (solver.G[bone_name] @ rest_point)
    return (round(world.dot(RIGHT), 5), round(world.dot(UP), 5))


def floor_crossing():
    """Where the shown body crosses the floor (z = 0), as screen points in world units from the pivot (x right, y up):
    the packer rings them with disturbed ash."""
    deps = bpy.context.evaluated_depsgraph_get()
    pts = []
    for name, obj in PARTS.items():
        if obj.hide_render or name.startswith(('Toe', 'Heel')):
            continue                # a standing talon touches the floor; only what plunges into the ash rings it
        ev = obj.evaluated_get(deps)
        mesh = ev.to_mesh()
        m = ev.matrix_world
        co = [m @ v.co for v in mesh.vertices]
        for e in mesh.edges:
            a, b = co[e.vertices[0]], co[e.vertices[1]]
            if (a.z > 0) != (b.z > 0) and abs(a.z - b.z) > 1e-9:
                t = a.z / (a.z - b.z)
                p = a + (b - a) * t
                pts.append((round(p.dot(RIGHT), 4), round(p.dot(UP), 4)))
        ev.to_mesh_clear()
    return pts[::max(1, len(pts) // 200)]


def kind_of(pose_name):
    return ('body' if pose_name in BODY_POSES else 'extra' if pose_name in EXTRA_POSES else
            'remains' if pose_name in REMAINS else 'dying')


def render_all():
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    started = time.time()
    for facing, degrees in FACING_SET:
        set_facing(degrees)
        for pose_name in POSE_SET:
            if pose_name in REMAINS and facing != 'front':
                continue
            kind = kind_of(pose_name)
            canvas, pivot = (CANVAS_PX, PIVOT_PX) if kind in ('body', 'extra') else (DEATH_CANVAS_PX, DEATH_PIVOT_PX)
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
            anchor_bone = 'body' if CREATURE == 'wraith' else 'chest'
            anchor_rest = W_CENTRE if CREATURE == 'wraith' else Vector((0, 0, 0.72))
            records.append(dict(facing=facing, pose=pose_name, file='%s/%s.png' % (facing, pose_name),
                                aux='%s/%s_aux.png' % (facing, pose_name), canvas=list(canvas), pivot=list(pivot),
                                glow=pose.glow, life=pose.life, ash=pose.ash, sink=pose.sink, kind=kind,
                                floor_points=floor_crossing() if pose.sink else [],
                                body_centre=screen_point(solver, anchor_rest, anchor_bone)))
            print('HORDE_FRAME', CREATURE, facing, pose_name, '%.1fs' % (time.time() - started), flush=True)
    RIG.location = (0, 0, 0)
    frame_camera(CANVAS_PX, PIVOT_PX)
    meta = dict(blender=bpy.app.version_string, creature=CREATURE, ppu=PPU, supersample=SS, elevation_degrees=30,
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
apply(pose_for('idle'))
RIG.location = (0, 0, 0)
set_state(aux_pass=0.0, preview=1.0)
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND), compress=True)
v, t = budget()
print('HORDE_COMPLETE', CREATURE, 'parts', len(PARTS), 'vertices', v, 'triangles', t, flush=True)
