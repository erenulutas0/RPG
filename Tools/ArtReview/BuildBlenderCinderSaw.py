"""The Cinder Saw's blade (docs/63 section 13): a reproducible stylised model and the rotation render that
build_cinder_saw_vfx.py packs into the strike set's painted blade (StrikeArtSet._sawBlade).

Run with Blender 4.5 LTS, headless (about half a minute on four threads):

    blender --background --factory-startup --python Tools/ArtReview/BuildBlenderCinderSaw.py -- [options]

Options after the "--":
    --out DIR        raw frames and frames.json (default TestResults/cinder-saw-render, git-ignored)
    --blend PATH     the saved scene (default <out>/cinder-saw.blend)
    --samples N      Cycles samples per render pixel (default 8)
    --threads N      render threads (default 4)
    --preview        only the first frame (its smear steps too), for a look
    --no-render      build and save the .blend, render nothing

What it is: a round saw blade of dark forged iron, about half a world unit across, the size that reads beside the
rendered Vanguard at the phone's scale. Ten hooked teeth (the upgrade icon's ten) lean the way it turns
(counter-clockwise on the screen, as the blade circles the hero), each climbing slowly from a rounded gullet to its
point and dropping back along an undercut face; a chamfer catches the key light round every edge; five round
lightening holes show the floor through the plate, so the turn reads at the hub as well as at the rim; a polished steel
boss with a dark axle socket sits in the middle. The teeth are hot: in the emission pass the iron writes its heat into
the red channel - nothing on the flat plate but each tooth's last stretch to its point, and the bevels and rim that edge
every tooth from root to point - which build_cinder_saw_vfx.py colours yellow-white (never ember or the telegraphs'
red-orange: docs/52 section 2.1 rule 3, docs/51 section 2) and blooms.

Nothing here is generated imagery: the plate is the tooth outline extruded and cut by the holes, the boss a lathed
profile, the shading the project's toon group (BuildBlenderGlassRegent.py's: painted value bands from a key fixed to
the screen, a cool rim, a hard specular and glint) fed by authored palettes, and every frame a turn of the blade about
its axle computed below.

Camera: orthographic, looking north and down 30 degrees, as every rendered actor. The blade's axle points at the camera
tilted 15 degrees toward upright, so the disc reads nearly round and its top rim shows a sliver of thickness. The axle
projects to the canvas's middle (a texel corner) in every frame: the pivot.

The loop: the blade repeats every fifth of a turn (ten teeth, five holes, every lathed part in multiples of five), so
FRAMES frames spread over a fifth of a turn close the loop exactly, each a quarter of a tooth on from the last. Each
frame renders SMEAR_STEPS times - the frame's own angle and the angles it turned through just before - so the pack can
lay a faint motion smear behind the blade and trail its hot points into short arcs.
"""
import bpy
import bmesh
import json
import math
import sys
import time
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []


def option(name, default=None):
    if name in ARGV:
        i = ARGV.index(name)
        return ARGV[i + 1] if i + 1 < len(ARGV) and not ARGV[i + 1].startswith('--') else True
    return default


OUT = Path(option('--out', str(ROOT / 'TestResults/cinder-saw-render'))).resolve()
BLEND = Path(option('--blend', str(OUT / 'cinder-saw.blend'))).resolve()
SAMPLES = int(option('--samples', 8))
THREADS = int(option('--threads', 4))
RENDER = not option('--no-render', False)
PREVIEW = bool(option('--preview', False))

# ---- The sprite contract (build_cinder_saw_vfx.py reads frames.json) ----
PPU = 128                      # final texels a world unit: the painted strikes' and the Glass Regent's 128
SS = 4                         # rendered at 4x, box-filtered down by the pack
CANVAS = 100                   # final texels, square: the blade, its bloom and its smear, the axle on the middle
ELEVATION = math.radians(30)
TILT = math.radians(15)        # the axle from the camera toward upright: a sliver of the top rim shows
FRAMES = 8
TEETH = 10
HOLES = 5
LOOP_DEGREES = 360.0 / HOLES   # the blade's own period: a fifth of a turn closes the loop
SMEAR_STEPS = 6                # renders a frame: its own angle, then the turn just behind it
SMEAR_DEGREES = 20.0           # how far back the smear reaches
GLOW_SCALE = 0.5               # the emission pass writes the heat at half, so a hot bevel never clips the 8 bits

# ---- Measurements, world units, in the blade's own frame (the axle along +Z, the face toward the camera at +Z) ----
R_TIP = 0.25                   # the teeth's points: half a unit across
R_ROOT = 0.204                 # where each tooth's back starts: a solid disc with a rim of teeth
GULLET = 0.012                 # how far the rounded gullet dips under the root
HOOK = 0.01                    # how far back along the rim the undercut face's foot sits behind its point
THICK = 0.03
CHAMFER = 0.0045
HOLE_AT = 0.122
HOLE_R = 0.029
HUB_R = 0.064
HUB_H = 0.02
BORE_R = 0.021


def lin(hexstr, alpha=1.0):
    """sRGB hex to the linear RGBA the node inputs take; the Standard view transform writes it back exactly."""
    h = hexstr.lstrip('#')
    out = []
    for i in (0, 2, 4):
        c = int(h[i:i + 2], 16) / 255
        out.append(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4)
    return (*out, alpha)


# ---------------------------------------------------------------------------------------------------------------------
# Scene: the rendered actors' (BuildBlenderGlassRegent.py), with the thread count capped
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
scene.cycles.transparent_max_bounces = 8
scene.cycles.pixel_filter_type = 'BOX'
scene.cycles.filter_width = 1.0
scene.render.threads_mode = 'FIXED'
scene.render.threads = THREADS
scene.render.film_transparent = True
scene.render.resolution_x = CANVAS * SS
scene.render.resolution_y = CANVAS * SS
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
# The blade's frame: its axle toward the camera, tilted toward upright; its own up in the plane of the disc.
AXLE = (TOWARD * math.cos(TILT) - UP * math.sin(TILT)).normalized()
BLADE_UP = (UP * math.cos(TILT) + TOWARD * math.sin(TILT)).normalized()
BASIS = Matrix((RIGHT, BLADE_UP, AXLE)).transposed().to_4x4()
# The key light, fixed to the screen: from the upper left and a little in front (the rendered roster's light).
KEY = (-0.55 * RIGHT + 0.76 * UP + 0.34 * TOWARD).normalized()
# A cool rim from behind the upper left, so the iron's far edge parts from a dark floor.
RIM = (-0.62 * RIGHT + 0.58 * UP - 0.52 * TOWARD).normalized()
HALF = (KEY + TOWARD).normalized()


# ---------------------------------------------------------------------------------------------------------------------
# State: which pass renders
# ---------------------------------------------------------------------------------------------------------------------
def build_state_group():
    ng = bpy.data.node_groups.new('Saw state', 'ShaderNodeTree')
    ng.interface.new_socket('Glow pass', in_out='OUTPUT', socket_type='NodeSocketFloat')
    out = ng.nodes.new('NodeGroupOutput')
    node = ng.nodes.new('ShaderNodeValue')
    node.name = 'Glow pass'
    node.outputs[0].default_value = 0.0
    ng.links.new(node.outputs[0], out.inputs[0])
    return ng


STATE = build_state_group()


def set_glow_pass(on):
    STATE.nodes['Glow pass'].outputs[0].default_value = 1.0 if on else 0.0


# ---------------------------------------------------------------------------------------------------------------------
# Shading: the toon group, fed by authored palettes
# ---------------------------------------------------------------------------------------------------------------------
class Nodes:
    """Small helpers over one node tree."""

    def __init__(self, tree):
        self.n, self.l = tree.nodes, tree.links

    def value(self, v):
        node = self.n.new('ShaderNodeValue')
        node.outputs[0].default_value = v
        return node.outputs[0]

    def vec(self, v):
        c = self.n.new('ShaderNodeCombineXYZ')
        c.inputs[0].default_value, c.inputs[1].default_value, c.inputs[2].default_value = v
        return c.outputs[0]

    def rgb(self, hexstr):
        node = self.n.new('ShaderNodeRGB')
        node.outputs[0].default_value = lin(hexstr)
        return node.outputs[0]

    def math(self, op, a, b=None, c=None, clamp=False):
        m = self.n.new('ShaderNodeMath')
        m.operation = op
        m.use_clamp = clamp
        for i, value in enumerate((a, b, c)):
            if value is None:
                continue
            if isinstance(value, (int, float)):
                m.inputs[i].default_value = value
            else:
                self.l.new(value, m.inputs[i])
        return m.outputs[0]

    def dot(self, a, b):
        d = self.n.new('ShaderNodeVectorMath')
        d.operation = 'DOT_PRODUCT'
        self.l.new(a, d.inputs[0])
        self.l.new(b, d.inputs[1])
        return d.outputs['Value']

    def step(self, value, low, high):
        r = self.n.new('ShaderNodeMapRange')
        r.interpolation_type = 'SMOOTHSTEP'
        self.l.new(value, r.inputs['Value'])
        r.inputs['From Min'].default_value = low
        r.inputs['From Max'].default_value = high
        return r.outputs['Result']

    def mix(self, fac, a, b):
        m = self.n.new('ShaderNodeMixRGB')
        if isinstance(fac, (int, float)):
            m.inputs['Fac'].default_value = fac
        else:
            self.l.new(fac, m.inputs['Fac'])
        self.l.new(a, m.inputs['Color1'])
        self.l.new(b, m.inputs['Color2'])
        return m.outputs['Color']

    def add(self, a, b, fac):
        m = self.n.new('ShaderNodeMixRGB')
        m.blend_type = 'ADD'
        if isinstance(fac, (int, float)):
            m.inputs['Fac'].default_value = fac
        else:
            self.l.new(fac, m.inputs['Fac'])
        self.l.new(a, m.inputs['Color1'])
        self.l.new(b, m.inputs['Color2'])
        return m.outputs['Color']

    def multiply(self, a, b):
        m = self.n.new('ShaderNodeMixRGB')
        m.blend_type = 'MULTIPLY'
        m.inputs['Fac'].default_value = 1.0
        self.l.new(a, m.inputs['Color1'])
        self.l.new(b, m.inputs['Color2'])
        return m.outputs['Color']


def folded_radius_and_angle(h, coords):
    """The blade's object coordinates as (radius, the angle folded into one mirrored half of the blade's period): a
    texture read through them repeats as the blade does, so the loop closes without a seam."""
    sep = h.n.new('ShaderNodeSeparateXYZ')
    h.l.new(coords, sep.inputs[0])
    x, y = sep.outputs['X'], sep.outputs['Y']
    r = h.math('SQRT', h.math('ADD', h.math('MULTIPLY', x, x), h.math('MULTIPLY', y, y)))
    a = h.math('ARCTAN2', y, x)
    period = math.radians(LOOP_DEGREES)
    a = h.math('WRAP', a, period, 0.0)
    a = h.math('ABSOLUTE', h.math('SUBTRACT', a, period / 2))
    return r, a


def build_toon_group():
    ng = bpy.data.node_groups.new('Saw toon', 'ShaderNodeTree')
    io = ng.interface
    for name, kind, default in [
            ('Light', 'NodeSocketColor', (1, 1, 1, 1)), ('Mid', 'NodeSocketColor', (.5, .5, .5, 1)),
            ('Shadow', 'NodeSocketColor', (.2, .2, .2, 1)), ('Deep', 'NodeSocketColor', (.05, .05, .05, 1)),
            ('Spec', 'NodeSocketFloat', 0.0), ('Spec Color', 'NodeSocketColor', (1, 1, 1, 1)),
            ('Rim', 'NodeSocketFloat', 0.35), ('Band Shift', 'NodeSocketFloat', 0.0),
            ('Glint', 'NodeSocketFloat', 0.0), ('Mottle', 'NodeSocketFloat', 0.0)]:
        socket = io.new_socket(name, in_out='INPUT', socket_type=kind)
        socket.default_value = default
    io.new_socket('Color', in_out='OUTPUT', socket_type='NodeSocketColor')
    h = Nodes(ng)
    gi = h.n.new('NodeGroupInput')
    go = h.n.new('NodeGroupOutput')
    geo = h.n.new('ShaderNodeNewGeometry')
    coords = h.n.new('ShaderNodeTexCoord')
    normal = geo.outputs['Normal']
    half_lambert = h.math('MULTIPLY_ADD', h.dot(normal, h.vec(KEY)), 0.5, 0.5)
    # The forged iron's mottle: broad patches a shade lighter or darker, read through the folded coordinates so it
    # turns with the blade and repeats with it.
    r, a = folded_radius_and_angle(h, coords.outputs['Object'])
    folded = h.n.new('ShaderNodeCombineXYZ')
    h.l.new(h.math('MULTIPLY', r, h.math('COSINE', a)), folded.inputs[0])
    h.l.new(h.math('MULTIPLY', r, h.math('SINE', a)), folded.inputs[1])
    mottle = h.n.new('ShaderNodeTexNoise')
    mottle.inputs['Scale'].default_value = 14.0
    mottle.inputs['Detail'].default_value = 1.0
    h.l.new(folded.outputs[0], mottle.inputs['Vector'])
    shift = h.math('MULTIPLY', h.math('SUBTRACT', mottle.outputs['Fac'], 0.5), gi.outputs['Mottle'])
    lit = h.math('ADD', h.math('ADD', half_lambert, gi.outputs['Band Shift']), shift)
    # Painted value steps with clean transitions: deep, shadow, the local colour, the lit plane.
    deep = h.step(lit, 0.16, 0.26)
    first = h.step(lit, 0.38, 0.5)
    second = h.step(lit, 0.74, 0.86)
    colour = h.mix(deep, gi.outputs['Deep'], gi.outputs['Shadow'])
    colour = h.mix(first, colour, gi.outputs['Mid'])
    colour = h.mix(second, colour, gi.outputs['Light'])
    # A cool rim on edges turned to the upper left and away.
    facing = h.n.new('ShaderNodeLayerWeight')
    facing.inputs['Blend'].default_value = 0.4
    rim = h.math('MULTIPLY', h.step(facing.outputs['Facing'], 0.5, 0.82), h.math('MAXIMUM', h.dot(normal, h.vec(RIM)), 0.0))
    colour = h.add(colour, h.rgb('#C4CEE4'), h.math('MULTIPLY', h.math('MULTIPLY', rim, gi.outputs['Rim']), 0.55))
    half = h.math('MAXIMUM', h.dot(normal, h.vec(HALF)), 0.0)
    spec = h.math('MULTIPLY', h.step(h.math('POWER', half, 30.0), 0.35, 0.55), gi.outputs['Spec'])
    colour = h.add(colour, gi.outputs['Spec Color'], spec)
    glint = h.math('MULTIPLY', h.step(h.math('POWER', half, 160.0), 0.4, 0.7), gi.outputs['Glint'])
    colour = h.add(colour, h.rgb('#FFFFFF'), glint)
    h.l.new(colour, go.inputs['Color'])
    return ng


TOON = build_toon_group()

PALETTES = {
    # light, mid, shadow, deep. Dark forged iron, cool so the hot teeth's yellow-white parts from it, a step darker
    # than the foundry's violet-grey paving; its lit bevels light enough to draw the edges on a dark floor.
    'iron': ('#7E879A', '#3B404C', '#262A33', '#121419'),
    # The boss: polished steel, the lightest thing on the blade but for the hot points.
    'steel': ('#E2E7F0', '#A3ABBC', '#5E6576', '#272B35'),
    'socket': ('#2A2D36', '#14161C', '#0C0D12', '#07080B'),
}


def material(name, palette, heat=False, **kw):
    """A self-shaded toon material. In the emission pass it writes its heat into the red channel (scaled by
    GLOW_SCALE), or black. The heat, by the radius in the blade's own frame: on the flat plate only each tooth's last
    stretch to its point; on the bevels and the rim (whatever faces out of the disc's plane) the whole tooth, root to
    point."""
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    tree = mat.node_tree
    for node in list(tree.nodes):
        tree.nodes.remove(node)
    h = Nodes(tree)
    out = h.n.new('ShaderNodeOutputMaterial')
    light, mid, shadow, deep = PALETTES[palette]
    g = h.n.new('ShaderNodeGroup')
    g.node_tree = TOON
    g.inputs['Light'].default_value = lin(light)
    g.inputs['Mid'].default_value = lin(mid)
    g.inputs['Shadow'].default_value = lin(shadow)
    g.inputs['Deep'].default_value = lin(deep)
    for key, socket in (('spec', 'Spec'), ('rim', 'Rim'), ('band_shift', 'Band Shift'), ('glint', 'Glint'),
                        ('mottle', 'Mottle')):
        if key in kw:
            g.inputs[socket].default_value = kw[key]
    g.inputs['Spec Color'].default_value = lin(kw.get('spec_color', '#EEF3FF'))
    colour = g.outputs['Color']
    if heat:
        coords = h.n.new('ShaderNodeTexCoord')
        r, _ = folded_radius_and_angle(h, coords.outputs['Object'])
        # The points: the flat plate glows only in each tooth's last stretch.
        points = h.math('POWER', h.step(r, R_TIP - 0.034, R_TIP - 0.003), 1.5)
        sep = h.n.new('ShaderNodeSeparateXYZ')
        h.l.new(coords.outputs['Normal'], sep.inputs[0])
        # The edges: the rim and the chamfers that face out of the plane (1 there, 0 on the flat faces) glow along the
        # whole tooth, from its root to its point.
        edge = h.math('SUBTRACT', 1.0, h.math('ABSOLUTE', sep.outputs['Z']))
        edges = h.math('MULTIPLY', edge, h.math('MULTIPLY', h.step(r, R_ROOT - 0.012, R_TIP - 0.006), 1.25))
        mask = h.math('MAXIMUM', points, edges)
        glow = h.n.new('ShaderNodeCombineColor')
        h.l.new(h.math('MULTIPLY', mask, GLOW_SCALE), glow.inputs[0])
        glow.inputs[1].default_value = 0.0
        glow.inputs[2].default_value = 0.0
        pass_colour = glow.outputs[0]
    else:
        pass_colour = h.rgb('#000000')
    state = h.n.new('ShaderNodeGroup')
    state.node_tree = STATE
    final = h.mix(state.outputs['Glow pass'], colour, pass_colour)
    e = h.n.new('ShaderNodeEmission')
    h.l.new(final, e.inputs['Color'])
    e.inputs['Strength'].default_value = 1.0
    h.l.new(e.outputs[0], out.inputs['Surface'])
    return mat


MATERIALS = {
    'iron': material('Forged iron, hot teeth', 'iron', heat=True, spec=0.55, rim=0.5, glint=0.9, mottle=0.16,
                     band_shift=0.02),
    'steel': material('Polished steel boss', 'steel', spec=1.0, spec_color='#FFFFFF', rim=0.45, glint=1.3),
    'socket': material('Axle socket', 'socket', rim=0.2),
}


# ---------------------------------------------------------------------------------------------------------------------
# Geometry, in the blade's own frame: the axle along +Z, the face toward the camera at +Z
# ---------------------------------------------------------------------------------------------------------------------
def polar(theta, r):
    return (r * math.cos(theta), r * math.sin(theta))


def tooth_outline():
    """The rim, counter-clockwise: each tooth's back climbs slowly from its root to its point, the undercut face drops
    back from the point toward the centre with its foot a little behind the point (a hook leaning the way the blade
    turns), and a rounded gullet dips under the root before the next back."""
    period = 2 * math.pi / TEETH
    points = []
    tip_u = 0.74
    for k in range(TEETH):
        t0 = k * period
        # The back: a slightly convex climb.
        for i in range(14):
            u = i / 14 * tip_u
            r = R_ROOT + (R_TIP - R_ROOT) * (u / tip_u) ** 0.92
            points.append(polar(t0 + u * period, r))
        tip = t0 + tip_u * period
        points.append(polar(tip - 0.006, R_TIP))
        points.append(polar(tip, R_TIP - 0.0015))
        # The face: from the point inward, its foot HOOK behind it along the rim.
        foot_r = R_ROOT + 0.004
        foot = tip - HOOK / foot_r
        for i in range(1, 5):
            s = i / 4
            r = R_TIP - 0.0015 + (foot_r - (R_TIP - 0.0015)) * s
            points.append(polar(tip + (foot - tip) * s, r))
        # The gullet: a round dip from the face's foot to the next back's start.
        end = t0 + period
        for i in range(1, 10):
            s = i / 10
            r = foot_r + (R_ROOT - foot_r) * s - GULLET * math.sin(math.pi * s)
            points.append(polar(foot + (end - foot) * s, r))
    return points


def extruded(name, outline, z0, z1, material_name):
    """A flat slab of the outline between z0 and z1, its faces flat-shaded."""
    bm = bmesh.new()
    front = [bm.verts.new((x, y, z1)) for x, y in outline]
    back = [bm.verts.new((x, y, z0)) for x, y in outline]
    bm.faces.new(front)
    bm.faces.new(list(reversed(back)))
    n = len(outline)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((back[i], back[j], front[j], front[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.data.materials.append(MATERIALS[material_name])
    return obj


def cylinder(name, at, radius, z0, z1, segments=30):
    """A round cutter centred HOLE_AT out at the polar angle at, its first corner pointing away from the axle, so the
    blade's turn by one hole's spacing maps every hole's corners onto the next hole's."""
    cx, cy = polar(at, HOLE_AT)
    outline = [(cx + radius * math.cos(at + 2 * math.pi * i / segments),
                cy + radius * math.sin(at + 2 * math.pi * i / segments)) for i in range(segments)]
    return extruded(name, outline, z0, z1, 'iron')


def lathe(name, profile, material_name, segments=40):
    """A solid of revolution about the axle from (radius, z) pairs, inner to outer; segments a multiple of HOLES, so the
    blade's period maps it onto itself."""
    bm = bmesh.new()
    rings = []
    for r, z in profile:
        if r == 0.0:
            rings.append([bm.verts.new((0.0, 0.0, z))] * segments)      # a cap's centre: the first faces fan from it
            continue
        rings.append([bm.verts.new((r * math.cos(2 * math.pi * i / segments), r * math.sin(2 * math.pi * i / segments), z))
                      for i in range(segments)])
    for a, b in zip(rings, rings[1:]):
        for i in range(segments):
            j = (i + 1) % segments
            bm.faces.new((a[i], b[i], b[j]) if a[i] is a[j] else (a[i], a[j], b[j], b[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.data.materials.append(MATERIALS[material_name])
    return obj


def bake(obj):
    """Applies the object's modifiers into its mesh, flat-shaded."""
    deps = bpy.context.evaluated_depsgraph_get()
    mesh = bpy.data.meshes.new_from_object(obj.evaluated_get(deps), preserve_all_data_layers=True, depsgraph=deps)
    old = obj.data
    obj.modifiers.clear()
    obj.data = mesh
    bpy.data.meshes.remove(old)
    for poly in mesh.polygons:
        poly.use_smooth = False


def build_blade():
    parts = []
    plate = extruded('Plate', tooth_outline(), -THICK / 2, THICK / 2, 'iron')
    cutters = [cylinder('Lightening hole %d' % k, math.pi / 2 + 2 * math.pi * k / HOLES, HOLE_R, -THICK, THICK)
               for k in range(HOLES)]
    for k, cutter in enumerate(cutters):
        cut = plate.modifiers.new('Hole %d' % k, 'BOOLEAN')
        cut.operation = 'DIFFERENCE'
        cut.solver = 'EXACT'
        cut.object = cutter
    bevel = plate.modifiers.new('Chamfer', 'BEVEL')
    bevel.width = CHAMFER
    bevel.segments = 1
    bevel.limit_method = 'ANGLE'
    bevel.angle_limit = math.radians(30)
    bevel.harden_normals = False
    bake(plate)
    for cutter in cutters:
        bpy.data.objects.remove(cutter)
    parts.append(plate)
    # The boss: a lathed collar standing proud of the plate, chamfered at its outer edge and rounding into the bore.
    face = THICK / 2
    parts.append(lathe('Boss', [(BORE_R, face + HUB_H * 0.55), (BORE_R + 0.006, face + HUB_H * 0.9),
                                (HUB_R * 0.62, face + HUB_H), (HUB_R - 0.007, face + HUB_H * 0.92),
                                (HUB_R, face + HUB_H * 0.45), (HUB_R, face - 0.002)], 'steel'))
    # The axle socket, dark in the bore.
    parts.append(lathe('Socket', [(0.0, face + HUB_H * 0.3), (BORE_R + 0.001, face + HUB_H * 0.3),
                                  (BORE_R + 0.001, face - 0.002)], 'socket'))
    return parts


PARTS = build_blade()
BLADE = bpy.data.objects.new('Cinder Saw', None)
collection.objects.link(BLADE)
for part in PARTS:
    part.parent = BLADE


def turn(degrees):
    """The blade turned counter-clockwise on the screen by degrees about its axle."""
    BLADE.matrix_world = BASIS @ Matrix.Rotation(math.radians(degrees), 4, 'Z')


# ---------------------------------------------------------------------------------------------------------------------
# Camera and the frame loop
# ---------------------------------------------------------------------------------------------------------------------
camera_data = bpy.data.cameras.new('Game view: orthographic, 30 degrees down, looking north')
camera_data.type = 'ORTHO'
camera_data.sensor_fit = 'AUTO'
camera_data.clip_start = 0.1
camera_data.clip_end = 100
camera_data.ortho_scale = CANVAS / PPU
camera = bpy.data.objects.new('Camera', camera_data)
collection.objects.link(camera)
scene.camera = camera
camera.rotation_euler = (math.pi / 2 - ELEVATION, 0, 0)
camera.location = -FORWARD * 20


def angles(frame):
    """The frame's own angle first, then the turn behind it, oldest last."""
    own = frame * LOOP_DEGREES / FRAMES
    return [own - SMEAR_DEGREES * j / (SMEAR_STEPS - 1) for j in range(SMEAR_STEPS)]


def render_all():
    OUT.mkdir(parents=True, exist_ok=True)
    records = []
    started = time.time()
    for frame in range(1 if PREVIEW else FRAMES):
        steps = []
        for j, degrees in enumerate(angles(frame)):
            turn(degrees)
            bpy.context.view_layer.update()
            files = {}
            for glow, suffix in ((False, ''), (True, '_glow')):
                set_glow_pass(glow)
                path = OUT / ('frame%d_step%d%s.png' % (frame, j, suffix))
                scene.render.filepath = str(path)
                bpy.ops.render.render(write_still=True)
                files['glow' if glow else 'file'] = path.name
            set_glow_pass(False)
            steps.append(dict(degrees=round(degrees, 4), **files))
        records.append(dict(frame=frame, steps=steps))
        print('SAW_FRAME', frame, '%.1fs' % (time.time() - started), flush=True)
    meta = dict(blender=bpy.app.version_string, ppu=PPU, supersample=SS, canvas=CANVAS, pivot=[CANVAS / 2, CANVAS / 2],
                elevation_degrees=30, tilt_degrees=math.degrees(TILT), teeth=TEETH, holes=HOLES,
                loop_degrees=LOOP_DEGREES, smear_degrees=SMEAR_DEGREES, glow_scale=GLOW_SCALE, tip_radius=R_TIP,
                root_radius=R_ROOT, frames=records)
    (OUT / 'frames.json').write_text(json.dumps(meta, indent=1))


def budget():
    deps = bpy.context.evaluated_depsgraph_get()
    verts = tris = 0
    for obj in PARTS:
        mesh = obj.evaluated_get(deps).to_mesh()
        mesh.calc_loop_triangles()
        verts += len(mesh.vertices)
        tris += len(mesh.loop_triangles)
        obj.evaluated_get(deps).to_mesh_clear()
    return verts, tris


if RENDER:
    render_all()
turn(0.0)
set_glow_pass(False)
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND), compress=True)
v, t = budget()
print('SAW_COMPLETE', 'parts', len(PARTS), 'vertices', v, 'triangles', t, flush=True)
