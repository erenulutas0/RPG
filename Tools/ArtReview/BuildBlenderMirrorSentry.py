"""The Mirror Sentry, floor 2's elite (docs/50 section 5, docs/49): a reproducible stylised model, rig, poses and the
sprite render for its EnemyArtSet - the elite contract the Grunt Captain's set uses (six bodies a facing, the strike
windup, the two remains and its own death), drawn in the ice palette from the start (it has no frosted copy).

Run with Blender 4.5 LTS, headless:

    blender --background --python Tools/ArtReview/BuildBlenderMirrorSentry.py -- [options]

Options after the "--":
    --out DIR        raw frames and frames.json (default TestResults/mirror-sentry-render, git-ignored)
    --blend PATH     the saved scene (default ArtDirection/2026-11-12/mirror-sentry-01/mirror-sentry.blend)
    --poses LIST     a comma-separated subset of idle,step-a,step-b,strike,pass-a,pass-b,windup,dying-0..dying-6,
                     death-collapse,death-cool
    --facings LIST   a comma-separated subset of front,rear
    --samples N      Cycles samples per render pixel (default 10)
    --no-render      build and save the .blend, render nothing

Who it is: a guard of the Quicksilver Vaults, whose quicksilver froze into mirror-ice. A tall knight in silver-and-ice
plate over black iron mail, ice crystals grown out of its pauldrons and its helm's crown, one violet slit for a face,
carrying a tower shield whose face is a mirror. It turns the mirror to the hero (docs/50: the mirror guard) and its
blow is the Mirror Bash, the shield thrust forward. It dies kneeling: the mirror cracks, the Sentry goes down on a knee,
bows forward and the shield falls face-down.

Readability (docs/49 section 7, docs/50 section 2): silver plate a step lighter than the slate floor, a dark outline,
no warm colour anywhere (red-orange is the telegraphs'), and the mirror the brightest shape on the body. The mirror's
face is a separate plane that the beauty pass draws black; an aux pass draws only it - red its coverage, green its
diagonal sheen coordinate - so PackMirrorSentrySprites.py lays its colour and value in afterwards and the mirror can be
tuned without rendering again. The aux pass's blue is the violet visor's light, for the packer's soft bloom.

Nothing here is generated imagery: the geometry is lofted rings and primitives, every part rides one bone rigidly
(plate armour does not bend), the shading is one authored toon node group (as BuildBlenderWarden.py's, the foundry's
warm bounce and rim taken out, a violet shade rim in their place), and every frame is a pose computed below.

Camera: orthographic, looking north and down 30 degrees, the game's 2:1 floor (ArenaFloor.DepthScale), as
BuildBlenderWarden.py. Heights are modelled in design units and the rig is stretched by 1/cos(30), so one design unit
of height is one world unit on screen. Both facings look left (the game mirrors them for the right); the camera and
the key light never move. The root stays on the floor origin, which projects to the same render pixel: the pivot.
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


OUT = Path(option('--out', str(ROOT / 'TestResults/mirror-sentry-render')))
BLEND = Path(option('--blend', str(ROOT / 'ArtDirection/2026-11-12/mirror-sentry-01/mirror-sentry.blend')))
SAMPLES = int(option('--samples', 10))
RENDER = not option('--no-render', False)

# ---- The sprite contract (Tools/ArtReview/PackMirrorSentrySprites.py reads frames.json) ----
PPU = 128                      # final texels per world unit, as every enemy's
SS = 4                         # render at 4x and box-filter down: area-exact anti-aliasing, the pivot stays on a corner
CANVAS_PX = (200, 240)         # the living body's render canvas in final texels (the packer trims it)
PIVOT_PX = (100, 40)           # the floor origin in final texels from the canvas's bottom-left corner
DEATH_CANVAS_PX = (288, 288)   # the death's: the body bowing forward and the shield falling in front of it
DEATH_PIVOT_PX = (144, 68)
ELEVATION = math.radians(30)
HEIGHT = 1 / math.cos(ELEVATION)
# Both facings look left; the game mirrors them for the right. Degrees as BuildBlenderWarden.py (270 faces the camera,
# 180 screen left, 90 away): the front turns 30 degrees off the camera so the mirror is seen nearly square and reads
# first; the rear 35 degrees off straight away, so the shield's edge and the crystals still show.
FACINGS = [('front', 240), ('rear', 125)]
# The figure is modelled at full size and scaled to stand about 1.4 units, some 30 % over the Captain's figure.
SCALE = 0.85
# Model units one walk cycle of two steps covers; times SCALE, EnemyArtSet._walkCycleDistance (the Captain's 0.8).
STRIDE = 0.94
BODY_POSES = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b', 'windup']
DYING = ['dying-%d' % k for k in range(7)]
REMAINS = ['death-collapse', 'death-cool']     # drawn once, front: the game mirrors the remains
POSES = BODY_POSES + DYING + REMAINS
AUX_SCALE = 1.0

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
# The key light, fixed to the screen: a cool frost-light from the upper left and a little in front.
KEY = (-0.55 * RIGHT + 0.76 * UP + 0.34 * TOWARD).normalized()
# A pale rim from behind the upper left, so silver's far edge still separates from the slate.
RIM = (-0.62 * RIGHT + 0.58 * UP - 0.52 * TOWARD).normalized()
# The floor's violet shade (docs/49: shadows tinted violet, not black) as a low rim from behind on the right.
SHADE_RIM = (0.58 * RIGHT - 0.25 * UP - 0.78 * TOWARD).normalized()
HALF = (KEY + TOWARD).normalized()


# ---------------------------------------------------------------------------------------------------------------------
# State shared by every material: the visor's life (death dims it), the cracks, and which pass renders
# ---------------------------------------------------------------------------------------------------------------------
def build_state_group():
    ng = bpy.data.node_groups.new('Sentry state', 'ShaderNodeTree')
    names = (('Life', 1.0), ('Aux pass', 0.0), ('Mirror preview', 0.0))
    for name, _ in names:
        ng.interface.new_socket(name, in_out='OUTPUT', socket_type='NodeSocketFloat')
    out = ng.nodes.new('NodeGroupOutput')
    for i, (name, value) in enumerate(names):
        node = ng.nodes.new('ShaderNodeValue')
        node.name = name
        node.outputs[0].default_value = value
        ng.links.new(node.outputs[0], out.inputs[i])
    return ng


STATE = build_state_group()


def set_state(life=None, aux_pass=None, mirror_preview=None):
    for name, value in (('Life', life), ('Aux pass', aux_pass), ('Mirror preview', mirror_preview)):
        if value is not None:
            STATE.nodes[name].outputs[0].default_value = value


# ---------------------------------------------------------------------------------------------------------------------
# Shading: one toon group, fed by authored palettes (BuildBlenderWarden.py's, cold)
# ---------------------------------------------------------------------------------------------------------------------
def build_toon_group():
    ng = bpy.data.node_groups.new('Sentry toon', 'ShaderNodeTree')
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
    facet = math_node('MULTIPLY', math_node('SUBTRACT', gi.outputs['Facet'], 0.5), 0.08)
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
    for channel, spread in enumerate((0.14, 0.14, 0.12)):
        l.new(math_node('MULTIPLY_ADD', brush.outputs['Fac'], spread, 1 - spread / 2), drift.inputs[channel])
    paint = n.new('ShaderNodeMixRGB')
    paint.blend_type = 'MULTIPLY'
    paint.inputs['Fac'].default_value = 1.0
    l.new(colour, paint.inputs['Color1'])
    l.new(drift.outputs[0], paint.inputs['Color2'])
    colour = paint.outputs['Color']
    closed = math_node('SUBTRACT', 1.0, step(gi.outputs['Occlusion'], 0.3, 0.97))
    colour = mix(closed, colour, gi.outputs['Deep'])
    # A frost rim on edges turned to the upper left and away.
    facing = n.new('ShaderNodeLayerWeight')
    facing.inputs['Blend'].default_value = 0.4
    rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.5, 0.82), math_node('MAXIMUM', dot(normal, vec(RIM)), 0.0))
    rim = math_node('MULTIPLY', rim, gi.outputs['Rim'])
    colour = add_colour(colour, rgb('#BFE6F2'), math_node('MULTIPLY', rim, 0.5))
    # The violet shade from behind on the lower right: only the last sliver of the silhouette (never warm).
    shade = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.66, 0.9),
                      math_node('MAXIMUM', dot(normal, vec(SHADE_RIM)), 0.0))
    colour = add_colour(colour, rgb('#6A55C0'), math_node('MULTIPLY', shade, 0.5))
    half = math_node('MAXIMUM', dot(normal, vec(HALF)), 0.0)
    spec = math_node('MULTIPLY', step(math_node('POWER', half, 36.0), 0.35, 0.55), gi.outputs['Spec'])
    colour = add_colour(colour, gi.outputs['Spec Color'], spec)
    glint = math_node('MULTIPLY', step(math_node('POWER', half, 180.0), 0.4, 0.7), gi.outputs['Glint'])
    colour = add_colour(colour, rgb('#F4F8FF'), glint)
    l.new(colour, go.inputs['Color'])
    return ng


TOON = build_toon_group()

PALETTES = {
    # light, mid, shadow, deep. Silver plate: its local colour a step above the slate floor's light (#67717F), its
    # shade leaning violet-blue (docs/49's shadow tint), never the floor's own slate; its lit planes kept well under the
    # mirror (review round 1: paler plate competed with the mirror by value at the phone's scale).
    'plate': ('#A9B3C3', '#7B8698', '#4A5368', '#22263A'),
    'plate-dark': ('#8A94A7', '#5C657A', '#373E51', '#191C28'),
    # Black iron mail and the undersuit: the dark mass the silver sits on, so the figure keeps a dark core.
    'iron': ('#5E6879', '#2E3442', '#1B1F29', '#0C0E14'),
    # Ice crystals: mirror-ice to ice edge (docs/49 palette).
    'ice': ('#E2F5FB', '#8FCBE2', '#4C8FAE', '#1F4E66'),
    # The shield's back and grip: dark iron with silver studs.
    'shield-back': ('#7A8496', '#3C4352', '#232835', '#10121A'),
}
OUTLINE = '#120F1A'
VIOLET = '#8B6CE8'        # docs/49: floor 2's enemy glow
VIOLET_CORE = '#D7CCFF'


class Shading:
    """A material: toon colours per palette, emitted once; the scene has no lights. In the aux pass every material emits
    black except the mirror (red its coverage, green its sheen coordinate) and the visor (blue its light)."""

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

    def toon(self, palette, spec=0.0, spec_color='#EEF4FF', rim=0.4, band_shift=0.0, occlusion=True, glint=0.0):
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

    def generated(self):
        coord = self.nodes.new('ShaderNodeTexCoord')
        sep = self.nodes.new('ShaderNodeSeparateXYZ')
        self.links.new(coord.outputs['Generated'], sep.inputs[0])
        return sep.outputs

    def backfacing(self):
        return self.nodes.new('ShaderNodeNewGeometry').outputs['Backfacing']

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
    s.links.new(s.backfacing(), mix.inputs['Fac'])
    s.links.new(emit.outputs[0], mix.inputs[1])
    s.links.new(transparent.outputs[0], mix.inputs[2])
    s.links.new(mix.outputs[0], s.out.inputs['Surface'])
    return s.mat


def mirror_material():
    """The mirror's face: black in the beauty pass (the packer adds its colour by coverage, so an edge texel is never
    counted twice), pale silver-violet in the saved scene's preview; in the aux pass red is coverage and green the
    sheen coordinate, a diagonal across the face from its upper left (0) to its lower right (1)."""
    s = Shading('Mirror face (tuned in the packer)')
    g = s.generated()
    sheen = s.math('MULTIPLY', s.math('ADD', g[0], s.math('SUBTRACT', 1.0, g[2])), 0.5, clamp=True)
    beauty = s.mix(s.state('Mirror preview'), s.rgb('#000000'), s.rgb('#DCD4FA'))
    return s.finish(beauty, aux=s.combine(1.0, sheen, 0.0))


def crack_material():
    """The cracks across the dying mirror: ink in the beauty, black in the aux pass, so they cut the mirror's mask."""
    s = Shading('Mirror crack')
    return s.finish(s.rgb('#1A1530'))


def visor_material():
    """The violet slit: a lilac core dimming to the floor's violet as the Sentry dies; its light in the aux blue."""
    s = Shading('Violet visor')
    ramp = s.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = 0.2
    ramp.color_ramp.elements[0].color = lin(VIOLET)
    ramp.color_ramp.elements[1].position = 0.8
    ramp.color_ramp.elements[1].color = lin(VIOLET_CORE)
    noise = s.nodes.new('ShaderNodeTexNoise')
    noise.inputs['Scale'].default_value = 18.0
    coord = s.nodes.new('ShaderNodeTexCoord')
    s.links.new(coord.outputs['Generated'], noise.inputs['Vector'])
    s.links.new(noise.outputs['Fac'], ramp.inputs[0])
    life = s.state('Life')
    colour = s.mix(life, s.rgb('#2A2240'), ramp.outputs[0])
    return s.finish(colour, aux=s.combine(0.0, 0.0, s.math('MULTIPLY', life, life)))


MATERIALS = {}


def build_materials():
    m = MATERIALS
    m['outline'] = outline_material()
    m['plate'] = plain('Silver plate', 'plate', spec=0.4, spec_color='#DDE6F2', glint=0.3, rim=0.4)
    m['plate-dark'] = plain('Silver under-plates', 'plate-dark', spec=0.25, spec_color='#C8D2E2', rim=0.3)
    m['iron'] = plain('Black iron mail', 'iron', spec=0.25, spec_color='#AAB6CC', rim=0.3)
    m['ice'] = plain('Ice crystal', 'ice', spec=0.9, spec_color='#FFFFFF', glint=1.0, rim=0.6, band_shift=0.05)
    m['shield-back'] = plain('Shield back', 'shield-back', spec=0.3, rim=0.3)
    m['mirror'] = mirror_material()
    m['crack'] = crack_material()
    m['visor'] = visor_material()


build_materials()


# ---------------------------------------------------------------------------------------------------------------------
# Geometry helpers (BuildBlenderWarden.py's): low-poly, flat-shaded, chamfered so edges catch the key in painted lines
# ---------------------------------------------------------------------------------------------------------------------
PARTS = {}      # name -> object
INFO = {}       # name -> dict(bone, material, contour, chamfer, occluder, crack)


def new_object(name, verts, faces, bone, material, contour=True, chamfer=0.006, occluder=False, flat=True,
               crack=False):
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
    INFO[name] = dict(bone=bone, material=material, contour=contour, chamfer=chamfer, occluder=occluder, crack=crack)
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
    """Rings round an axis: rings is [(t, rx, ry[, count])], t along the axis from origin."""
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


def crystal(name, base, direction, length, radius, bone, sides=6, twist=0.0, **kw):
    """An ice crystal: a hexagonal prism sunk at its base, its last third a faceted point."""
    d = Vector(direction).normalized()
    ref = Vector((0, -1, 0)) if abs(d.y) < 0.9 else Vector((1, 0, 0))
    u = (ref - d * ref.dot(d)).normalized()
    v = d.cross(u)
    rings = []
    for t, r in ((0.0, radius * 0.9), (0.62, radius), (0.7, radius * 0.94)):
        c = Vector(base) + d * (t * length)
        rings.append([c + (u * math.cos(2 * math.pi * k / sides + twist) + v * math.sin(2 * math.pi * k / sides + twist)) * r
                      for k in range(sides)])
    verts = [p for ring in rings for p in ring]
    faces = []
    for j in range(len(rings) - 1):
        for i in range(sides):
            i1 = (i + 1) % sides
            faces.append((j * sides + i, j * sides + i1, (j + 1) * sides + i1, (j + 1) * sides + i))
    verts.append(Vector(base) + d * length)
    tip = len(verts) - 1
    top = (len(rings) - 1) * sides
    for i in range(sides):
        faces.append((top + i, top + (i + 1) % sides, tip))
    verts.append(Vector(base) - d * 0.002)
    bottom = len(verts) - 1
    for i in range(sides):
        faces.append(((i + 1) % sides, i, bottom))
    kw.setdefault('chamfer', 0.0)
    return new_object(name, verts, faces, bone, 'ice', **kw)


# ---- Measurements (design units, feet on z = 0, facing -Y, the Sentry's left is +X) ----
# Tall and lean, a head taller than the Captain: long greaves, a narrow waist, a broad chest, a tall great helm with an
# ice crown, ice crystals off both pauldrons.
ANKLE_Z, KNEE_Z, HIP_Z = 0.095, 0.415, 0.735
HIP_X = 0.105
WAIST_Z = 0.84
CHEST_Z = 1.0
NECK_Z = 1.2
SHOULDER = Vector((0.232, 0.005, 1.145))
ELBOW = Vector((0.268, 0.035, 0.905))
WRIST = Vector((0.272, 0.005, 0.70))
FIST = Vector((0.274, -0.005, 0.64))
HEAD_BASE = Vector((0, -0.012, 1.215))
HELM_TOP = 1.475
# The shield, at rest: its centre, upright, its mirror facing -Y. Its outline (across, up) from its centre.
SHIELD_C = Vector((0.0, -0.45, 0.76))
SHIELD_W = 0.40
SHIELD_OUTLINE = [(0.9 * u, 0.9 * v) for u, v in (
    (0.0, -0.37), (0.12, -0.31), (0.2, -0.2), (0.205, 0.0), (0.2, 0.25), (0.175, 0.325), (0.11, 0.36), (0.0, 0.372),
    (-0.11, 0.36), (-0.175, 0.325), (-0.2, 0.25), (-0.205, 0.0), (-0.2, -0.2), (-0.12, -0.31))]
SHIELD_THICK = 0.05
SHIELD_GRIP = SHIELD_C + Vector((0.0, 0.07, 0.03))


def mirror_x(v):
    return Vector((-v.x, v.y, v.z))


def side_point(v, side):
    return v if side > 0 else mirror_x(v)


def build_legs():
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        x = HIP_X * side
        # Sabaton: a long pointed foot of overlapping lames, lofted heel to toe on a flat sole.
        rings = []
        for y, w, zc, hz in [(0.075, 0.056, 0.05, 0.048), (0.03, 0.068, 0.058, 0.056), (-0.05, 0.07, 0.05, 0.048),
                             (-0.125, 0.06, 0.036, 0.034), (-0.19, 0.04, 0.022, 0.02), (-0.235, 0.014, 0.012, 0.01)]:
            rings.append([Vector((x + w * math.cos(a), y, max(0.004, zc + hz * math.sin(a))))
                          for a in [2 * math.pi * i / 8 for i in range(8)]])
        loft('Sabaton ' + s, rings, 'foot.' + s, 'plate', cap_start=True, cap_end=True, chamfer=0.006)
        # Greave: ankle to knee, the calf's swell behind; a frost-white ridge down its shin.
        greave = [(0.0, 0.058, 0.058, 8), (0.3, 0.074, 0.08, 8), (0.62, 0.078, 0.082, 8), (1.0, 0.068, 0.066, 8)]
        loft('Greave ' + s, frame_points(Vector((x, 0.005, ANKLE_Z + 0.02)), Vector((0, -0.005, KNEE_Z - ANKLE_Z - 0.04)),
                                         greave), 'shin.' + s, 'plate', cap_start=True, cap_end=True, occluder=True)
        # Poleyn: a pointed knee cop with a fan-shaped wing.
        ellipsoid('Poleyn ' + s, Vector((x, -0.05, KNEE_Z + 0.005)), (0.07, 0.06, 0.068), 'shin.' + s, 'plate',
                  rings=4, segments=8, lower=-0.6, upper=1.0)
        ellipsoid('Poleyn wing ' + s, Vector((x + 0.068 * side, -0.02, KNEE_Z)), (0.012, 0.05, 0.058), 'shin.' + s,
                  'plate-dark', rings=3, segments=6)
        # Mail at the knee's back, then the cuisse up to the hip.
        ellipsoid('Knee mail ' + s, Vector((x, 0.005, KNEE_Z + 0.02)), (0.064, 0.064, 0.05), 'shin.' + s, 'iron',
                  rings=3, segments=8, contour=False, chamfer=0.0)
        cuisse = [(0.0, 0.074, 0.07, 8), (0.4, 0.088, 0.086, 8), (1.0, 0.098, 0.094, 8)]
        loft('Cuisse ' + s, frame_points(Vector((x, 0.0, KNEE_Z + 0.05)), Vector((0, 0, HIP_Z - KNEE_Z - 0.03)), cuisse),
             'thigh.' + s, 'plate-dark', cap_start=True, cap_end=True, occluder=True)
        # Tasset: a long plate hanging over the front of the thigh, riding it part of the way.
        tas = []
        for z, w, out in [(0.80, 0.11, 0.0), (0.72, 0.114, 0.012), (0.64, 0.108, 0.022), (0.575, 0.092, 0.03)]:
            row = []
            for t in (-1.0, -0.5, 0.0, 0.5, 1.0):
                a = t * 0.95
                row.append(Vector((x + w * math.sin(a), -0.118 - out - 0.05 * math.cos(a) + 0.03 * abs(t), z)))
            tas.append(row)
        verts = [p for row in tas for p in row]
        faces = []
        for j in range(len(tas) - 1):
            for i in range(4):
                faces.append((j * 5 + i, (j + 1) * 5 + i, (j + 1) * 5 + i + 1, j * 5 + i + 1))
        new_object('Tasset ' + s, verts, faces, 'tasset.' + s, 'plate', chamfer=0.0)


# The breastplate's rings: z, radius across, radius deep, the keel's push. A narrow waist, the chest widest under the
# arms: the tall knight's V.
CHEST_RINGS = [(0.965, 0.2, 0.148, 0.02), (1.03, 0.236, 0.162, 0.034), (1.1, 0.258, 0.17, 0.042),
               (1.16, 0.248, 0.164, 0.034), (1.205, 0.2, 0.135, 0.014), (1.235, 0.13, 0.1, 0.0)]


def build_torso():
    # The mail skirt: black iron flaring from the belt to mid-thigh, under the tassets.
    skirt = [ring_points(0, 0.006, z, rx, ry, 16, back=0.01) for z, rx, ry in
             ((0.86, 0.19, 0.142), (0.76, 0.205, 0.16), (0.63, 0.225, 0.182))]
    loft('Mail skirt', skirt, 'pelvis', 'iron', occluder=True)
    loft('Belt', [ring_points(0, 0.0, z, 0.196, 0.146, 16) for z in (0.835, 0.885)], 'pelvis', 'plate-dark',
         occluder=True)
    box('Buckle', Vector((0, -0.156, 0.86)), (0.07, 0.024, 0.06), 'pelvis', 'plate', chamfer=0.006)
    plack = [(0.86, 0.182, 0.136, 0.012), (0.92, 0.19, 0.142, 0.018), (0.985, 0.204, 0.15, 0.022)]
    loft('Plackart', [ring_points(0, 0.0, z, rx, ry, 16, keel=k, back=0.01) for z, rx, ry, k in plack], 'spine',
         'plate', occluder=True)
    loft('Breastplate', [ring_points(0, 0.0, z, rx, ry, 16, keel=k, back=0.025) for z, rx, ry, k in CHEST_RINGS],
         'chest', 'plate', cap_end=True, occluder=True, chamfer=0.007)
    # A frost-white keel down the breastplate's centre and the plates' edges catch the key as painted lines.
    keel = [Vector((0, -0.17 - 0.04 * smooth(0.96, 1.1, z) + 0.03 * smooth(1.16, 1.23, z), z))
            for z in (0.98, 1.04, 1.1, 1.16, 1.21)]
    tube('Keel', keel, [0.009] * len(keel), 'chest', 'ice', count=6, contour=False, chamfer=0.0)
    # Gorget: the high collar the helm sits on.
    loft('Gorget', [ring_points(0, 0.0, 1.2, 0.15, 0.118, 12), ring_points(0, -0.004, 1.275, 0.118, 0.1, 12)], 'neck',
         'plate', occluder=True, chamfer=0.006)
    loft('Neck mail', [ring_points(0, -0.008, z, 0.09, 0.084, 10) for z in (1.24, 1.31)], 'neck', 'iron',
         cap_start=True, cap_end=True, contour=False, chamfer=0.0)


def helm_point(h, degrees, lift=0.004):
    """A point on the helm's surface h above its base, degrees round from its front (+ toward its left)."""
    rx, ry, keel = 0.108, 0.116, 0.034
    a = -math.pi / 2 + math.radians(degrees)
    front = max(0.0, -math.sin(a))
    p = Vector((rx * math.cos(a), ry * math.sin(a) - keel * front ** 6, 0))
    n = Vector((math.cos(a) / rx, math.sin(a) / ry, 0)).normalized()
    return HEAD_BASE + Vector((0, 0, h)) + p + n * lift


def build_helm():
    """A tall great helm with a prow down its front, one violet slit across it, and a crown of ice."""
    hc = HEAD_BASE
    levels = [(0.0, 0.1, 0.106, 0.02), (0.06, 0.108, 0.116, 0.03), (0.14, 0.108, 0.116, 0.034),
              (0.2, 0.1, 0.106, 0.03), (0.24, 0.074, 0.078, 0.014), (0.26, 0.034, 0.034, 0.0)]
    rings = [ring_points(hc.x, hc.y, hc.z + h, rx, ry, 12, keel=k) for h, rx, ry, k in levels]
    loft('Helm', rings, 'head', 'plate', cap_start=True, cap_end=True, occluder=True, chamfer=0.007)
    # The slit: one narrow band of violet across the front, a little below the helm's widest.
    slit = [helm_point(0.125, d, 0.006) for d in range(-52, 53, 13)]
    tube('Visor', slit, [(0.0075, 0.012)] * len(slit), 'head', 'visor', count=6, contour=False, chamfer=0.0,
         twist_ref=Vector((0, 0, 1)))
    # A breath slit under it, dark.
    breath = [helm_point(h, 0, 0.004) for h in (0.035, 0.065, 0.095)]
    tube('Breath', breath, [0.006] * 3, 'head', 'iron', count=5, contour=False, chamfer=0.0)
    # The crown: a tall crystal and two lower ones leaning out, growing out of the helm's top.
    top = hc + Vector((0, 0.0, 0.245))
    crystal('Crown', top + Vector((0, 0.005, -0.02)), (0.0, 0.12, 1.0), 0.19, 0.032, 'head', twist=0.3)
    crystal('Crown L', top + Vector((0.035, 0.0, -0.035)), (0.55, 0.15, 1.0), 0.12, 0.022, 'head', twist=0.1)
    crystal('Crown R', top + Vector((-0.035, 0.0, -0.035)), (-0.55, 0.15, 1.0), 0.11, 0.022, 'head', twist=0.5)


def build_arms():
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
        # Pauldron: a domed shoulder plate and a lame below it, riding part of the arm's swing; ice grown out of it.
        pc = sh + Vector((0.02 * side, 0.0, 0.03))
        ellipsoid('Pauldron ' + s, pc, (0.11, 0.115, 0.08), 'pauldron.' + s, 'plate', rings=4, segments=10,
                  lower=-0.25, upper=1.0, occluder=True, chamfer=0.006)
        ellipsoid('Pauldron lame ' + s, pc + Vector((0.012 * side, 0, -0.06)), (0.104, 0.11, 0.06),
                  'pauldron.' + s, 'plate-dark', rings=3, segments=10, lower=-0.3, upper=0.7)
        crystal('Pauldron ice %s 0' % s, pc + Vector((0.03 * side, 0.01, 0.05)), (0.5 * side, 0.15, 1.0), 0.15, 0.026,
                'pauldron.' + s, twist=0.2)
        crystal('Pauldron ice %s 1' % s, pc + Vector((0.075 * side, -0.02, 0.03)), (0.95 * side, -0.05, 0.6), 0.1,
                0.02, 'pauldron.' + s, twist=0.6)
        # Rerebrace, couter, vambrace, gauntlet.
        tube('Rerebrace ' + s, [sh + (el - sh) * t for t in (0.15, 0.55, 0.95)], [0.052, 0.054, 0.05],
             'upper_arm.' + s, 'plate-dark', count=8)
        ellipsoid('Couter ' + s, el, (0.056, 0.056, 0.056), 'forearm.' + s, 'plate', rings=4, segments=8)
        tube('Vambrace ' + s, [el + (wr - el) * t for t in (0.15, 0.6, 1.0)], [0.046, 0.05, 0.044],
             'forearm.' + s, 'plate', count=8)
        ellipsoid('Gauntlet ' + s, fi + Vector((0, 0, 0.005)), (0.05, 0.046, 0.06), 'hand.' + s, 'plate-dark',
                  rings=4, segments=8)
        loft('Cuff ' + s, frame_points(wr + (wr - el).normalized() * -0.01, (wr - el).normalized() * 0.05,
                                        [(0.0, 0.052, 0.05), (1.0, 0.06, 0.056)]), 'hand.' + s, 'plate', cap_end=True)


def shield_point(u, v, depth=0.0):
    """A point on the shield at rest: u across (+ toward the Sentry's left), v up, depth toward its back (+Y)."""
    return SHIELD_C + Vector((u, depth, v))


def build_shield():
    """The tower shield: a silver slab with a raised rim, its face a mirror (a separate plane, see mirror_material),
    its back dark iron with a grip."""
    half = SHIELD_THICK / 2
    front = [shield_point(u, v, -half) for u, v in SHIELD_OUTLINE]
    back = [shield_point(u, v, half) for u, v in SHIELD_OUTLINE]
    n = len(SHIELD_OUTLINE)
    verts = front + back + [shield_point(0, 0, -half), shield_point(0, 0, half)]
    faces = []
    for i in range(n):
        i1 = (i + 1) % n
        faces.append((i, i1, n + i1, n + i))
        faces.append((2 * n, i1, i))
        faces.append((2 * n + 1, n + i, n + i1))
    new_object('Shield', verts, faces, 'shield', 'plate', chamfer=0.008)
    # The mirror: the outline inset, a hair proud of the slab's face.
    inset = 0.86
    face = [shield_point(u * inset, v * inset + 0.004, -half - 0.003) for u, v in SHIELD_OUTLINE]
    new_object('Mirror', face, [tuple(range(n))[::-1]], 'shield', 'mirror', contour=False, chamfer=0.0)
    # The rim: a raised silver lip round the mirror, so the bright slab is framed by a line of plate and ink.
    rim = [shield_point(u * 0.93, v * 0.93 + 0.002, -half - 0.004) for u, v in SHIELD_OUTLINE]
    tube('Shield rim', rim + rim[:2], [0.014] * (n + 2), 'shield', 'plate', count=6, cap_start=False, cap_end=False,
         chamfer=0.0, twist_ref=Vector((0, -1, 0)))
    # The back: a dark iron plate over the slab's back, a grip bar and two straps.
    back_plate = [shield_point(u * 0.94, v * 0.94, half + 0.003) for u, v in SHIELD_OUTLINE]
    new_object('Shield back', back_plate, [tuple(range(n))], 'shield', 'shield-back', contour=False, chamfer=0.0)
    box('Grip', SHIELD_GRIP + Vector((0, 0.005, 0)), (0.16, 0.03, 0.035), 'shield', 'shield-back', chamfer=0.006)
    for dv in (-0.16, 0.2):
        box('Strap %+.2f' % dv, shield_point(0, dv, half + 0.012), (0.24, 0.018, 0.04), 'shield', 'plate-dark',
            chamfer=0.004)
    # The cracks the dying mirror shows: shown only in the death (INFO crack), a hair in front of the face.
    centre = (0.03, 0.06)
    for k, (a, length) in enumerate(((20, 0.2), (95, 0.28), (160, 0.22), (215, 0.3), (290, 0.26), (340, 0.17))):
        pts = []
        for t in (0.0, 0.35, 0.7, 1.0):
            bend = math.radians(a + 14 * math.sin(3 * t + k))
            pts.append(shield_point(centre[0] + math.cos(bend) * length * t * 0.75,
                                    centre[1] + math.sin(bend) * length * t, -half - 0.007))
        tube('Crack %d' % k, pts, [0.0055, 0.005, 0.0042, 0.003], 'shield', 'crack', count=4, contour=False,
             chamfer=0.0, crack=True, twist_ref=Vector((0, -1, 0)))


build_legs()
build_torso()
build_helm()
build_arms()
build_shield()


# ---------------------------------------------------------------------------------------------------------------------
# Rig: plate rides bones rigidly (BuildBlenderWarden.py's)
# ---------------------------------------------------------------------------------------------------------------------
arm_data = bpy.data.armatures.new('Mirror Sentry rig')
RIG = bpy.data.objects.new('Mirror Sentry', arm_data)
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
bone('shield', tuple(SHIELD_C), tuple(SHIELD_C + Vector((0, 0, 0.3))), 'root')
bpy.ops.object.mode_set(mode='OBJECT')
REST = {b.name: b.matrix_local.copy() for b in arm_data.bones}
PARENT = {b.name: (b.parent.name if b.parent else None) for b in arm_data.bones}
FOLLOWERS = {'pauldron.L': ('upper_arm.L', 0.22), 'pauldron.R': ('upper_arm.R', 0.22),
             'tasset.L': ('thigh.L', 0.6), 'tasset.R': ('thigh.R', 0.6)}


def depth(name):
    d = 0
    while PARENT[name]:
        name = PARENT[name]
        d += 1
    return d


# Parents before children, a follower after the bone it follows, and the shield straight after the root.
ORDER = sorted(REST, key=lambda b: (depth(b) + (1 if b in FOLLOWERS else 0) - (0.5 if b == 'shield' else 0), b))
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
# Posing (BuildBlenderWarden.py's solver): deltas about each bone's head in rest space, accumulated down the hierarchy;
# the shield is placed outright and the left fist grips it; legs and arms by two-bone IK.
# ---------------------------------------------------------------------------------------------------------------------
def rot(x=0.0, y=0.0, z=0.0):
    """Degrees about the rig's axes: +x pitches a raised bone forward (toward -Y), +y leans it to the Sentry's left,
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
        self.life = 1.0           # the visor's light
        self.mirror = 1.0         # the mirror's value, for the packer (the struck flash, the dying dim)
        self.cracks = False
        self.floor = False        # settle the lowest point onto the floor (the death's drawings)

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


def shield_placement(centre, yaw=0.0, pitch=0.0, roll=0.0):
    """The shield's G: its centre on centre, turned yaw (+ its face toward the Sentry's left), pitched (+ its top
    forward) and rolled (+ its top toward the Sentry's left)."""
    r = rot(x=pitch, y=roll, z=yaw).to_4x4()
    return Matrix.Translation(Vector(centre)) @ r @ Matrix.Translation(-SHIELD_C)


def hold_shield(p, centre, yaw=0.0, pitch=0.0, roll=0.0, pole=(0.9, 0.25, -0.5), grip=True):
    g = shield_placement(centre, yaw, pitch, roll)
    p.free['shield'] = g
    if grip:
        p.hands['L'] = (g @ SHIELD_GRIP, Vector(pole))


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
    solver = Solver(pose)
    basis = solver.solve()
    for b, m in basis.items():
        RIG.pose.bones[b].matrix_basis = m
    set_state(life=pose.life)
    for name, obj in PARTS.items():
        if INFO[name]['crack']:
            obj.hide_render = not pose.cracks
            obj.hide_viewport = not pose.cracks
    return solver


# ---------------------------------------------------------------------------------------------------------------------
# The poses
# ---------------------------------------------------------------------------------------------------------------------
def stance():
    """Upright and watchful, the chest up, the mirror held square in front of the body, the right fist at the hip."""
    p = Pose()
    p.offset = Vector((0, 0, -0.012))
    p.add('pelvis', rot(x=1))
    p.add('spine', rot(x=-2))
    p.add('chest', rot(x=-2))
    p.add('neck', rot(x=4))
    p.add('head', rot(x=-2))
    p.feet['L'] = (Vector((0.13, -0.04, ANKLE_Z)), 0, 10)
    p.feet['R'] = (Vector((-0.12, 0.05, ANKLE_Z)), 0, -14)
    p.hands['R'] = (Vector((-0.285, -0.03, 0.635)), Vector((-0.6, 0.8, -0.2)))
    hold_shield(p, (0.055, -0.27, 0.80), yaw=6)
    return p


def idle():
    return stance()


def walk(phase):
    """A measured guard's walk: distance-driven, two steps a cycle, the right foot's contact at 0; the shield held
    steady in front, bobbing with the body, the right fist swinging against its leg."""
    p = stance()
    bob = -0.022 * (0.5 + 0.5 * math.cos(4 * math.pi * phase))
    p.offset = Vector((0.008 * math.sin(2 * math.pi * phase), 0, bob - 0.01))
    excursion = STRIDE / 4
    duty = 0.5
    for s, offset, x in (('R', 0.0, -0.115), ('L', 0.5, 0.12)):
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
            pitch = lerp(16, -8, t)
        p.feet[s] = (Vector((x, y, z)), pitch, 8 if s == 'L' else -8)
    swing = math.sin(2 * math.pi * phase)
    p.add('pelvis', rot(z=5 * swing))
    p.add('spine', rot(x=2, z=-2 * swing))
    p.add('chest', rot(x=2, z=-3 * swing))
    p.add('head', rot(z=-1 * swing))
    p.hands['R'] = (Vector((-0.28, -0.03 - 0.13 * swing, 0.645 + 0.025 * abs(swing))), Vector((-0.6, 0.8, -0.2)))
    hold_shield(p, (0.055 + 0.01 * swing, -0.275 + 0.015 * swing, 0.80 + 0.012 * math.cos(4 * math.pi * phase)),
                yaw=6 + 3 * swing)
    return p


def windup():
    """The Mirror Bash's beat: the shield drawn back to the left shoulder and turned edge-on, the body coiled away, the
    knees bent, the right fist forward for balance - the mirror's face swung off the hero."""
    p = Pose()
    p.offset = Vector((0, 0.03, -0.055))
    p.add('pelvis', rot(x=-2, z=10))
    p.add('spine', rot(x=-4, z=8))
    p.add('chest', rot(x=-6, z=14))
    p.add('neck', rot(x=4, z=-12))
    p.add('head', rot(x=2, z=-14))
    p.feet['L'] = (Vector((0.15, 0.07, ANKLE_Z)), 0, 18)
    p.feet['R'] = (Vector((-0.12, -0.12, ANKLE_Z)), 0, -10)
    p.hands['R'] = (Vector((-0.2, -0.24, 0.84)), Vector((-0.8, 0.2, -0.6)))
    hold_shield(p, (0.25, -0.12, 0.9), yaw=68, pitch=-6, roll=-4, pole=(1.0, 0.4, -0.3))
    return p


def strike():
    """The Mirror Bash: a lunge onto the left foot, the shield driven forward and up, the body behind it."""
    p = Pose()
    p.offset = Vector((0, -0.09, -0.05))
    p.add('pelvis', rot(x=6, z=-6))
    p.add('spine', rot(x=8, z=-4))
    p.add('chest', rot(x=6, z=-8))
    p.add('neck', rot(x=-4, z=6))
    p.add('head', rot(x=-6, z=6))
    p.feet['L'] = (Vector((0.13, -0.27, ANKLE_Z)), 0, 6)
    p.feet['R'] = (Vector((-0.11, 0.2, ANKLE_Z + 0.01)), 14, -16)
    p.hands['R'] = (Vector((-0.29, 0.2, 0.74)), Vector((-0.8, -0.4, -0.3)))
    hold_shield(p, (0.01, -0.6, 0.86), yaw=-4, pitch=-10, pole=(0.9, 0.3, -0.6))
    return p


def dying(k):
    """Its own death, seven drawings: struck (the mirror cracks and flashes), buckling, down on the right knee with the
    shield planted, slumping onto both knees as the shield tips, the shield falling face-down, bowed to the floor,
    and settled, the visor out."""
    p = Pose()
    p.cracks = True
    p.floor = k >= 2
    if k == 0:
        p.offset = Vector((0, 0.045, -0.02))
        p.add('pelvis', rot(x=-4))
        p.add('spine', rot(x=-9, z=6))
        p.add('chest', rot(x=-12, z=8))
        p.add('neck', rot(x=-8))
        p.add('head', rot(x=-16, z=-6))
        p.feet['L'] = (Vector((0.14, -0.03, ANKLE_Z)), 0, 12)
        p.feet['R'] = (Vector((-0.12, 0.08, ANKLE_Z)), 0, -14)
        p.hands['R'] = (Vector((-0.33, 0.07, 0.86)), Vector((-0.6, 0.3, -0.7)))
        hold_shield(p, (0.15, -0.22, 0.86), yaw=30, pitch=10, roll=6, pole=(1.0, 0.2, -0.4))
        p.life, p.mirror = 1.0, 1.3
    elif k == 1:
        p.offset = Vector((0, 0.02, -0.13))
        p.add('pelvis', rot(x=8))
        p.add('spine', rot(x=10, z=4))
        p.add('chest', rot(x=12, z=4))
        p.add('neck', rot(x=10))
        p.add('head', rot(x=16))
        p.feet['L'] = (Vector((0.14, -0.05, ANKLE_Z)), 0, 12)
        p.feet['R'] = (Vector((-0.12, 0.08, ANKLE_Z)), 0, -14)
        p.knee_poles = {'L': Vector((0.3, -1, 0)), 'R': Vector((-0.3, -1, 0))}
        p.hands['R'] = (Vector((-0.27, -0.06, 0.5)), Vector((-0.6, 0.8, -0.2)))
        hold_shield(p, (0.11, -0.3, 0.62), yaw=14, pitch=8, roll=4)
        p.life, p.mirror = 0.85, 0.95
    elif k == 2:
        p.offset = Vector((0, 0.03, -0.36))
        p.add('pelvis', rot(x=10))
        p.add('spine', rot(x=14))
        p.add('chest', rot(x=14))
        p.add('neck', rot(x=12))
        p.add('head', rot(x=22))
        p.feet['L'] = (Vector((0.12, -0.24, ANKLE_Z)), 0, 10)
        p.feet['R'] = (Vector((-0.105, 0.27, 0.075)), 72, -6)
        p.knee_poles = {'L': Vector((0.2, -1, 0.4)), 'R': Vector((0, -1, -0.6))}
        p.hands['R'] = (Vector((-0.24, -0.12, 0.3)), Vector((-0.6, 0.6, -0.2)))
        hold_shield(p, (0.11, -0.44, 0.375), yaw=10, pitch=4, roll=3, pole=(0.9, 0.3, -0.3))
        p.life, p.mirror = 0.65, 0.75
    elif k == 3:
        p.offset = Vector((0, 0.05, -0.4))
        p.add('pelvis', rot(x=20))
        p.add('spine', rot(x=20, y=-4))
        p.add('chest', rot(x=18, y=-4))
        p.add('neck', rot(x=12))
        p.add('head', rot(x=24, y=-8))
        p.feet['L'] = (Vector((0.11, 0.27, 0.075)), 72, 6)
        p.feet['R'] = (Vector((-0.105, 0.27, 0.075)), 72, -6)
        p.knee_poles = {'L': Vector((0, -1, -0.6)), 'R': Vector((0, -1, -0.6))}
        p.hands['R'] = (Vector((-0.24, -0.2, 0.2)), Vector((-0.6, 0.6, -0.2)))
        p.hands['L'] = (Vector((0.24, -0.26, 0.22)), Vector((0.6, 0.6, -0.2)))
        hold_shield(p, (0.1, -0.53, 0.31), yaw=8, pitch=36, roll=5, grip=False)
        p.life, p.mirror = 0.45, 0.55
    else:
        settle = k - 4             # 0 toppling, 1 down, 2 settled
        p.offset = Vector((0, 0.06 + 0.02 * settle, -0.43 - 0.02 * settle))
        p.add('pelvis', rot(x=28 + 8 * settle))
        p.add('spine', rot(x=26 + 4 * settle, y=-4))
        p.add('chest', rot(x=22 + 4 * settle, y=-4))
        p.add('neck', rot(x=12 - 4 * settle))
        p.add('head', rot(x=10 - 6 * settle, y=-10, z=-8))
        p.feet['L'] = (Vector((0.11, 0.29, 0.07)), 76, 6)
        p.feet['R'] = (Vector((-0.105, 0.29, 0.07)), 76, -6)
        p.knee_poles = {'L': Vector((0, -1, -0.6)), 'R': Vector((0, -1, -0.6))}
        reach = 0.38 + 0.06 * settle
        p.hands['R'] = (Vector((-0.25, -reach, 0.08 - 0.02 * settle)), Vector((-0.7, 0.4, 0.3)))
        p.hands['L'] = (Vector((0.3, -reach + 0.12, 0.08 - 0.02 * settle)), Vector((0.8, 0.3, 0.3)))
        pitch = (64, 89, 89)[settle]
        hold_shield(p, (0.12, (-0.66, -0.76, -0.77)[settle], (0.17, 0.035, 0.03)[settle]), yaw=14, pitch=pitch, roll=4,
                    grip=False)
        p.life = (0.3, 0.12, 0.0)[settle]
        p.mirror = (0.35, 0.2, 0.15)[settle]
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
    if name.startswith('dying-'):
        return dying(int(name.split('-')[1]))
    # The two remains (EnemyArtSet._death): the settled body, the visor's last light, then out.
    p = dying(6)
    p.life = 0.25 if name == 'death-collapse' else 0.0
    p.mirror = 0.15 if name == 'death-collapse' else 0.1
    return p


# ---------------------------------------------------------------------------------------------------------------------
# Baked contact occlusion (BuildBlenderWarden.py's)
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
    reach = 0.12
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
            body = pose_name in BODY_POSES
            canvas, pivot = (CANVAS_PX, PIVOT_PX) if body else (DEATH_CANVAS_PX, DEATH_PIVOT_PX)
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
            records.append(dict(facing=facing, pose=pose_name, file='%s/%s.png' % (facing, pose_name),
                                aux='%s/%s_aux.png' % (facing, pose_name), canvas=list(canvas), pivot=list(pivot),
                                mirror=pose.mirror, life=pose.life,
                                mirror_centre=screen_point(solver, SHIELD_C, 'shield'),
                                kind='body' if body else ('remains' if pose_name in REMAINS else 'dying')))
            print('SENTRY_FRAME', facing, pose_name, '%.1fs' % (time.time() - started), flush=True)
    RIG.location = (0, 0, 0)
    frame_camera(CANVAS_PX, PIVOT_PX)
    meta = dict(blender=bpy.app.version_string, ppu=PPU, supersample=SS, elevation_degrees=30,
                height_compensation=HEIGHT, scale=SCALE, stride=round(STRIDE * SCALE, 4), aux_scale=AUX_SCALE,
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
apply(idle())
RIG.location = (0, 0, 0)
set_state(aux_pass=0.0, mirror_preview=1.0)     # the saved scene shows the mirror; the renders draw it black
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND), compress=True)
v, t = budget()
print('SENTRY_COMPLETE', 'parts', len(PARTS), 'vertices', v, 'triangles', t, flush=True)
