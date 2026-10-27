"""The Foundry's Runner and Tank (docs/44, The Foundry): faceted basalt golems built by a script, posed and rendered to
the painted Grunt's six-pose sprite contract (EnemyArtSet), and the brass the Grunt Captain's kitbash wears. A
Grunt-proportioned golem renders as the calibration figure and, with a helm and a bell, as the rejected rendered
candidate for the Captain.

Run with Blender 4.5 LTS, headless, once per enemy:

    blender --background --python Tools/ArtReview/BuildBlenderFoundryEnemies.py -- --enemy NAME

Options after the "--":
    --enemy NAME     runner, tank, captain-brass (the helm and the bell for build_foundry_enemies.py), grunt (the
                     calibration figure, beside the painted Grunt) or captain (the rejected rendered candidate)
    --out DIR        raw frames, lava masks and frames.json (default TestResults/foundry-enemies-render, git-ignored)
    --blend PATH     the saved scene (default ArtDirection/2026-10-27/foundry-enemies-01/<enemy>.blend)
    --frames LIST    a comma-separated subset of the frame names (front-idle, rear-strike, death-collapse, ...)
    --samples N      Cycles samples per render pixel (default 12)
    --no-render      build and save the .blend, render nothing

Nothing here is generated imagery. Every rock is the convex hull of points on a rounded box, its near-flat facets
merged and its edges bevelled, so it shades in the painted Grunt's broad planes; the lava is emissive geometry seen
through the gaps between rocks; the shading is one authored toon node group with the Grunt's sampled basalt values;
each rock carries an inverted-hull contour, so every boulder is outlined as the painted ones are. The rocks ride
empties that a small forward-and-inverse-kinematics skeleton places for every pose.

Camera: orthographic, looking north and down 30 degrees (the game's 2:1 floor, as BuildBlenderArcanist.py), heights
stretched by 1/cos(30) so a design unit of height is a world unit on screen. The two facings look LEFT, as the painted
Grunt's do (the game mirrors them for the right): the front three quarters toward the camera, the rear three quarters
away (the Runner turns further, so its lean reads). The camera and the key light never move, so the light comes from
the screen's upper left in every frame. The floor origin projects to the same pixel corner in every frame: the pivot.
"""
import bpy
import bmesh
import json
import math
import random
import sys
import time
import zlib
from pathlib import Path
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
ARGV = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []


def option(name, default=None):
    if name in ARGV:
        i = ARGV.index(name)
        return ARGV[i + 1] if i + 1 < len(ARGV) and not ARGV[i + 1].startswith('--') else True
    return default


ENEMY = option('--enemy', 'runner')
OUT = Path(option('--out', str(ROOT / 'TestResults/foundry-enemies-render'))) / ENEMY
BLEND = Path(option('--blend', str(ROOT / 'ArtDirection/2026-10-27/foundry-enemies-01' / (ENEMY + '.blend'))))
SAMPLES = int(option('--samples', 12))
WANTED = option('--frames')
RENDER = not option('--no-render', False)

# ---- The sprite contract (build_foundry_enemies.py reads frames.json) ----
PPU = 128                      # final texels per world unit, as the painted Grunt
SS = 4                         # render at 4x and box-filter down: area-exact anti-aliasing, the pivot on a corner
ELEVATION = math.radians(30)
HEIGHT = 1 / math.cos(ELEVATION)
# Degrees, 0 east, 90 north: both look left, as the painted Grunt's front (down-left) and rear (up-left) do.
FACINGS = {'front': 245, 'rear': 120}
# EnemyArtSet order: idle, contact A, contact B, strike, then the appended passing A and B.
POSES = ['idle', 'step-a', 'step-b', 'strike', 'pass-a', 'pass-b']
DEATHS = ['death-collapse', 'death-cool']


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


def name_seed(name):
    """A seed from a name that is the same on every run (Python's own str hash is salted per process)."""
    return zlib.crc32(name.encode()) % 100000


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
scene.cycles.transparent_max_bounces = 32
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
# The key light, fixed to the screen: from the upper left and a little in front, as on the painted Grunt.
KEY = (-0.5 * RIGHT + 0.82 * UP + 0.18 * TOWARD).normalized()
# A cool rim from behind the upper left: the lit edge the painted rocks carry on their top-left planes.
RIM = (-0.6 * RIGHT + 0.62 * UP - 0.5 * TOWARD).normalized()
HALF = (KEY + TOWARD).normalized()

# ---------------------------------------------------------------------------------------------------------------------
# Shading: one toon group, fed by palettes sampled from the painted Grunt
# ---------------------------------------------------------------------------------------------------------------------
PALETTES = {
    # highlight, light, mid, shadow, deep: the painted Grunt's imported basalt (CHR_Grunt_front-idle.tga, 128 texels
    # a unit) by value band - its top tenth, 75-90th, 50-75th, 25-50th and lowest quarter - a step wider at the ends,
    # because a facet here holds one value where the painted plane holds a gradient, and a little more violet: the
    # first renders measured greyer than the sprite (saturation 0.26 against 0.32).
    'basalt': ('#9087B6', '#655A88', '#4A3E64', '#362A48', '#24182A'),
    # The Runner's thinner, hotter shards: the same stone a step lighter, so the lean figure does not sink into stone.
    'shard': ('#9E94C4', '#6F6594', '#534870', '#3D3152', '#291D32'),
    # Soot-black foundry iron: darker and warmer than the basalt, so plates read as a second material.
    'iron': ('#76707A', '#4A454E', '#2F2B33', '#1E1B21', '#121014'),
    'rivet': ('#C9C2C8', '#8E878F', '#5A545C', '#38333A', '#1E1A20'),
    # Cast iron caps: darker than the plates, so a dome turned full to the light stays iron and does not go grey.
    'pauldron': ('#5C5660', '#3C3840', '#29262C', '#1A181C', '#100E12'),
    # Cast brass, deep enough that a plane turned full to the light stays gold rather than going pale.
    'brass': ('#FFDA7E', '#E0A23F', '#A86C26', '#6A3F18', '#34200C'),
    # Dark stone of the eye sockets and the vent rim.
    'socket': ('#3A3046', '#2A2232', '#1E1824', '#140F18', '#0C090E'),
}
OUTLINE = '#160F17'
LAVA_CORE, LAVA_HOT, LAVA_EDGE = '#FFB431', '#F57A10', '#B8301A'


def build_toon_group():
    ng = bpy.data.node_groups.new('Foundry golem toon', 'ShaderNodeTree')
    io = ng.interface
    for name, kind, default in [
            ('Highlight', 'NodeSocketColor', (1, 1, 1, 1)), ('Light', 'NodeSocketColor', (.7, .7, .7, 1)),
            ('Mid', 'NodeSocketColor', (.5, .5, .5, 1)), ('Shadow', 'NodeSocketColor', (.2, .2, .2, 1)),
            ('Deep', 'NodeSocketColor', (.05, .05, .05, 1)), ('Soft', 'NodeSocketFloat', 0.25),
            ('Facet', 'NodeSocketFloat', 0.06), ('Brush', 'NodeSocketFloat', 0.12), ('Rim', 'NodeSocketFloat', 0.5),
            ('Spec', 'NodeSocketFloat', 0.0), ('Spec Color', 'NodeSocketColor', (1, 1, 1, 1)),
            ('Glint', 'NodeSocketFloat', 0.0), ('Band Shift', 'NodeSocketFloat', 0.0)]:
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

    # The painted rocks are broad planes with a soft turn inside each: the face's own normal, eased a little toward
    # the smoothed one, so a facet keeps one value with a gentle gradient across it.
    eased = n.new('ShaderNodeVectorMath')
    eased.operation = 'ADD'
    l.new(geo.outputs['True Normal'], eased.inputs[0])
    soft = n.new('ShaderNodeVectorMath')
    soft.operation = 'SCALE'
    l.new(geo.outputs['Normal'], soft.inputs[0])
    l.new(gi.outputs['Soft'], soft.inputs['Scale'])
    l.new(soft.outputs[0], eased.inputs[1])
    normal = n.new('ShaderNodeVectorMath')
    normal.operation = 'NORMALIZE'
    l.new(eased.outputs[0], normal.inputs[0])
    normal = normal.outputs[0]
    lit = math_node('MULTIPLY_ADD', dot(normal, vec(KEY)), 0.5, 0.5)
    # Each facet is laid in with its own stroke: a small per-face value shift (the 'facet' attribute, -1 to 1).
    facet = n.new('ShaderNodeAttribute')
    facet.attribute_type = 'GEOMETRY'
    facet.attribute_name = 'facet'
    lit = math_node('MULTIPLY_ADD', facet.outputs['Fac'], gi.outputs['Facet'], lit)
    lit = math_node('ADD', lit, gi.outputs['Band Shift'])
    # Five painted values with soft steps, the Grunt's: deep, shadow, mid, light and the lit top-left planes.
    colour = mix(step(lit, 0.3, 0.42), gi.outputs['Deep'], gi.outputs['Shadow'])
    colour = mix(step(lit, 0.5, 0.6), colour, gi.outputs['Mid'])
    colour = mix(step(lit, 0.66, 0.75), colour, gi.outputs['Light'])
    colour = mix(step(lit, 0.83, 0.92), colour, gi.outputs['Highlight'])
    # A loose brush breakup on the undeformed surface (Generated coordinates), so it sticks to the rock in every pose.
    coords = n.new('ShaderNodeTexCoord')
    brush = n.new('ShaderNodeTexNoise')
    brush.inputs['Scale'].default_value = 3.2
    brush.inputs['Detail'].default_value = 2.0
    l.new(coords.outputs['Generated'], brush.inputs['Vector'])
    breakup = math_node('MULTIPLY_ADD', math_node('SUBTRACT', brush.outputs['Fac'], 0.5), gi.outputs['Brush'], 1.0)
    paint = n.new('ShaderNodeMixRGB')
    paint.blend_type = 'MULTIPLY'
    paint.inputs['Fac'].default_value = 1.0
    l.new(colour, paint.inputs['Color1'])
    grey = n.new('ShaderNodeCombineColor')
    for channel in range(3):
        l.new(breakup, grey.inputs[channel])
    l.new(grey.outputs[0], paint.inputs['Color2'])
    colour = paint.outputs['Color']
    # The lit edge: planes turned to the upper left and away catch a pale rim, as the painted boulders' top edges do.
    facing = n.new('ShaderNodeLayerWeight')
    facing.inputs['Blend'].default_value = 0.35
    rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.5, 0.85), math_node('MAXIMUM', dot(normal, vec(RIM)), 0.0))
    rim = math_node('MULTIPLY', rim, gi.outputs['Rim'])
    rim_colour = n.new('ShaderNodeRGB')
    rim_colour.outputs[0].default_value = lin('#D9D4F0')
    colour = add_colour(colour, rim_colour.outputs[0], math_node('MULTIPLY', rim, 0.4))
    # A hard painted highlight for brass and iron, and a pin-point glint inside it.
    half = math_node('MAXIMUM', dot(normal, vec(HALF)), 0.0)
    spec = math_node('MULTIPLY', step(math_node('POWER', half, 40.0), 0.3, 0.55), gi.outputs['Spec'])
    colour = add_colour(colour, gi.outputs['Spec Color'], spec)
    glint_colour = n.new('ShaderNodeRGB')
    glint_colour.outputs[0].default_value = lin('#FFFDF0')
    glint = math_node('MULTIPLY', step(math_node('POWER', half, 200.0), 0.4, 0.7), gi.outputs['Glint'])
    colour = add_colour(colour, glint_colour.outputs[0], glint)
    l.new(colour, go.inputs['Color'])
    return ng


TOON = build_toon_group()


class Shading:
    """A material that shades itself: toon colours, emitted once (the scene has no lights)."""

    def __init__(self, name):
        self.mat = bpy.data.materials.new(name)
        self.mat.use_nodes = True
        self.nodes = self.mat.node_tree.nodes
        self.links = self.mat.node_tree.links
        for node in list(self.nodes):
            self.nodes.remove(node)
        self.out = self.nodes.new('ShaderNodeOutputMaterial')

    def toon(self, palette, **inputs):
        g = self.nodes.new('ShaderNodeGroup')
        g.node_tree = TOON
        for key, value in zip(('Highlight', 'Light', 'Mid', 'Shadow', 'Deep'), PALETTES[palette]):
            g.inputs[key].default_value = lin(value)
        for key, value in inputs.items():
            name = key.replace('_', ' ').title()
            g.inputs[name].default_value = lin(value) if isinstance(value, str) else value
        return g.outputs['Color']

    def finish(self, colour, lava=None):
        e = self.nodes.new('ShaderNodeEmission')
        self.links.new(colour, e.inputs['Color'])
        e.inputs['Strength'].default_value = 1.0
        self.links.new(e.outputs[0], self.out.inputs['Surface'])
        if lava is not None:
            aov = self.nodes.new('ShaderNodeOutputAOV')
            aov.aov_name = 'lava'
            self.links.new(lava, aov.inputs['Value'])
        return self.mat


def plain(name, palette, **inputs):
    s = Shading(name)
    return s.finish(s.toon(palette, **inputs))


def lava_material(name, core=LAVA_CORE, hot=LAVA_HOT, edge=LAVA_EDGE, bloom=0.5):
    """Molten stone: a hot yellow core where it faces the camera, orange, a red edge where it turns away. Its
    strength (a Value node the frame loop drives: 1 alive, low as the remains cool) dims colour and the lava mask
    together; the mask (the 'lava' AOV, `bloom` at full strength: 0.5 for a seam, 1 for the Runner's heart, which
    must outshine every seam) is what the pack step blooms."""
    s = Shading(name)
    strength = s.nodes.new('ShaderNodeValue')
    strength.name = 'Lava strength'
    strength.outputs[0].default_value = 1.0
    facing = s.nodes.new('ShaderNodeLayerWeight')
    facing.inputs['Blend'].default_value = 0.5
    ramp = s.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = 0.05
    ramp.color_ramp.elements[0].color = lin(core)
    ramp.color_ramp.elements[1].position = 0.7
    ramp.color_ramp.elements[1].color = lin(edge)
    mid = ramp.color_ramp.elements.new(0.3)
    mid.color = lin(hot)
    s.links.new(facing.outputs['Facing'], ramp.inputs[0])
    # As the remains cool the glow sinks toward a dull red, not toward black.
    cooled = s.nodes.new('ShaderNodeMixRGB')
    s.links.new(strength.outputs[0], cooled.inputs['Fac'])
    cooled.inputs['Color1'].default_value = lin('#3C0B06')
    s.links.new(ramp.outputs[0], cooled.inputs['Color2'])
    mask = s.nodes.new('ShaderNodeMath')
    mask.operation = 'MULTIPLY'
    mask.inputs[1].default_value = bloom
    s.links.new(strength.outputs[0], mask.inputs[0])
    return s.finish(cooled.outputs['Color'], mask.outputs[0])


def outline_material():
    s = Shading('Contour (inverted hull)')
    emit = s.nodes.new('ShaderNodeEmission')
    emit.inputs['Color'].default_value = lin(OUTLINE)
    transparent = s.nodes.new('ShaderNodeBsdfTransparent')
    mix = s.nodes.new('ShaderNodeMixShader')
    s.links.new(s.nodes.new('ShaderNodeNewGeometry').outputs['Backfacing'], mix.inputs['Fac'])
    s.links.new(emit.outputs[0], mix.inputs[1])
    s.links.new(transparent.outputs[0], mix.inputs[2])
    s.links.new(mix.outputs[0], s.out.inputs['Surface'])
    return s.mat


MATERIALS = {
    'outline': outline_material(),
    'basalt': plain('Basalt', 'basalt'),
    'shard': plain('Runner shard', 'shard', rim=0.6),
    'iron': plain('Soot iron', 'iron', spec=0.55, spec_color='#B9B2BE', soft=0.15, facet=0.03, brush=0.18),
    'rivet': plain('Rivet', 'rivet', spec=1.1, spec_color='#F2EEF4', glint=1.2, soft=1.0, facet=0.0),
    'pauldron': plain('Cast iron cap', 'pauldron', spec=0.7, spec_color='#A8A2AE', soft=0.3, facet=0.03, brush=0.2),
    'brass': plain('Brass', 'brass', spec=1.1, spec_color='#FFEDB8', glint=1.4, soft=0.6, facet=0.02, band_shift=-0.06),
    'socket': plain('Socket', 'socket', rim=0.0, facet=0.0),
    'lava': lava_material('Lava'),
    'eye': lava_material('Ember eye', core='#FFF6B0', hot='#FFD24A', edge='#F08A24'),
    # The Runner's heart: hotter than any seam, nearly white at its centre.
    'core': lava_material('Runner heart', core='#FFF8D0', hot='#FFD050', edge='#F7861E', bloom=1.0),
}

# ---------------------------------------------------------------------------------------------------------------------
# Geometry: faceted rocks, lava, small parts
# ---------------------------------------------------------------------------------------------------------------------
CONTOUR = 0.0075          # the inverted hull's width: about one final texel on the inner contours
PARTS = []                # (object, segment, rest matrix in its segment's rest space)


def finish(obj, material, contour=True, smooth_faces=True, seed=0):
    mesh = obj.data
    mesh.materials.append(MATERIALS[material])
    for poly in mesh.polygons:
        poly.use_smooth = smooth_faces
    rng = random.Random(seed * 7919 + 13)
    attr = mesh.attributes.new('facet', 'FLOAT', 'FACE')
    for i in range(len(mesh.polygons)):
        attr.data[i].value = rng.uniform(-1, 1)
    if contour:
        mesh.materials.append(MATERIALS['outline'])
        hull = obj.modifiers.new('Contour', 'SOLIDIFY')
        hull.thickness = CONTOUR
        hull.offset = 1.0
        hull.use_flip_normals = True
        hull.use_rim = False
        hull.use_even_offset = False
        hull.use_quality_normals = False
        hull.material_offset = 1
    return obj


def mesh_object(name, bm):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    return obj


def rock_bmesh(size, seed, boxy=3.2, count=14, merge=14.0, bevel=0.1):
    """A cut boulder: the convex hull of points on a rounded box (|x|^p + |y|^p + |z|^p = 1, p = boxy), nearly flat
    neighbours merged into broad facets, the edges chamfered so each one catches the light as a painted edge."""
    rng = random.Random(seed)
    bm = bmesh.new()
    golden = math.pi * (3 - math.sqrt(5))
    for i in range(count):
        z = 1 - 2 * (i + 0.5) / count
        r = math.sqrt(max(0.0, 1 - z * z))
        a = i * golden + rng.uniform(-0.35, 0.35)
        d = Vector((r * math.cos(a), r * math.sin(a), z + rng.uniform(-0.12, 0.12))).normalized()
        k = (abs(d.x) ** boxy + abs(d.y) ** boxy + abs(d.z) ** boxy) ** (-1 / boxy)
        v = d * k * rng.uniform(0.9, 1.04)
        bm.verts.new((v.x * size[0], v.y * size[1], v.z * size[2]))
    result = bmesh.ops.convex_hull(bm, input=bm.verts[:])
    unused = [g for g in result['geom_interior'] + result['geom_unused'] if isinstance(g, bmesh.types.BMVert)]
    if unused:
        bmesh.ops.delete(bm, geom=unused, context='VERTS')
    bmesh.ops.dissolve_limit(bm, angle_limit=math.radians(merge), verts=bm.verts[:], edges=bm.edges[:])
    if bevel > 0:
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bevel * min(size), offset_type='OFFSET', segments=1,
                        profile=0.5, affect='EDGES', clamp_overlap=True)
    return bm


def rock(name, segment, centre, size, rot=(0, 0, 0), seed=0, material='basalt', **shape):
    obj = mesh_object(name, rock_bmesh(size, seed or name_seed(name), **shape))
    finish(obj, material, seed=seed or name_seed(name))
    place(obj, segment, centre, rot)
    return obj


def blob(name, segment, centre, size, rot=(0, 0, 0), material='lava', subdivisions=2, contour=False):
    """A glowing core or seam, lumpy so what shows through a gap is uneven (never a clean ring), or a small domed
    part (rivet, bolt), smooth."""
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=subdivisions, radius=1.0)
    if material in ('lava', 'core'):
        rng = random.Random(name_seed(name))
        bumps = [Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1))).normalized() for _ in range(5)]
        weights = [rng.uniform(-0.35, 0.3) for _ in bumps]
        for v in bm.verts:
            d = v.co.normalized()
            v.co = d * (1.0 + sum(w * max(0.0, d.dot(b)) ** 3 for w, b in zip(weights, bumps)))
    bmesh.ops.scale(bm, vec=Vector(size), verts=bm.verts)
    obj = mesh_object(name, bm)
    finish(obj, material, contour=contour)
    place(obj, segment, centre, rot)
    return obj


def vein(name, segment, start, end, width=0.006, material='lava'):
    """A crack of lava from start to end (rest design space), its glow standing just proud of the stone."""
    start, end = Vector(start), Vector(end)
    axis = end - start
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    bmesh.ops.scale(bm, vec=Vector((width, width, axis.length / 2)), verts=bm.verts)
    obj = mesh_object(name, bm)
    finish(obj, material, contour=False)
    up = Vector((0, 0, 1))
    turn = up.rotation_difference(axis.normalized()).to_euler('XYZ')
    place(obj, segment, tuple((start + end) / 2), tuple(math.degrees(a) for a in turn))
    return obj


def slab(name, segment, centre, size, rot=(0, 0, 0), material='iron', bevel=0.25, seed=0, warp=0.0, contour=True,
         bend=0.0):
    """A plate: a bevelled box, its corners nudged so no two plates are the same, optionally bent round the body it
    covers - its sides `bend` behind its middle (+Y), so a breastplate wraps the chest instead of standing flat."""
    rng = random.Random(seed or name_seed(name))
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=2.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * size[0] * rng.uniform(1 - warp, 1 + warp), v.co.y * size[1],
                       v.co.z * size[2] * rng.uniform(1 - warp, 1 + warp)))
    if bend:
        across = [e for e in bm.edges if abs(e.verts[0].co.x - e.verts[1].co.x) > 1e-6]
        bmesh.ops.subdivide_edges(bm, edges=across, cuts=7, use_grid_fill=True)
    bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bevel * min(size), offset_type='OFFSET', segments=2, profile=0.5,
                    affect='EDGES', clamp_overlap=True)
    for v in bm.verts:
        v.co.y += bend * (v.co.x / size[0]) ** 2
    obj = mesh_object(name, bm)
    finish(obj, material, contour=contour, seed=seed or name_seed(name))
    place(obj, segment, centre, rot)
    return obj


def euler(rot):
    x, y, z = (math.radians(a) for a in rot)
    return (Matrix.Rotation(z, 4, 'Z') @ Matrix.Rotation(y, 4, 'Y') @ Matrix.Rotation(x, 4, 'X'))


def place(obj, segment, centre, rot):
    obj.parent = RIG.empty[segment]
    obj.matrix_parent_inverse = Matrix.Identity(4)
    rest = Matrix.Translation(Vector(centre)) @ euler(rot)
    obj.matrix_basis = rest
    PARTS.append((obj, segment, rest))


# ---------------------------------------------------------------------------------------------------------------------
# The skeleton: rest joints in design space (feet on z = 0, facing -Y, the golem's left is +X); every pose is a
# rotation about each joint in its parent's posed frame, plus inverse kinematics for the planted feet.
# ---------------------------------------------------------------------------------------------------------------------
def rot(x=0.0, y=0.0, z=0.0):
    """Degrees about the design axes: +x tips a raised segment forward (toward -Y) and swings a hanging one back;
    +y leans it toward the golem's left (+X) when raised; +z turns it to its left."""
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
    return middle, a + dn * dist, dn.cross(p).normalized()


class Rig:
    """Joint empties under one root. The root carries the facing and the 1/cos(30) height stretch, so every joint's
    own transform stays rigid (an object's basis cannot hold the shear a stretched rotation makes; parenting can)."""

    def __init__(self):
        self.joint = {}
        self.parent = {}
        self.order = []
        self.empty = {}
        self.root = bpy.data.objects.new('Golem', None)
        self.root.empty_display_type = 'ARROWS'
        self.root.empty_display_size = 0.2
        self.root.scale = (1, 1, HEIGHT)
        collection.objects.link(self.root)

    def add(self, name, joint, parent=None):
        self.joint[name] = Vector(joint)
        self.parent[name] = parent
        self.order.append(name)
        empty = bpy.data.objects.new('Joint ' + name, None)
        empty.empty_display_type = 'PLAIN_AXES'
        empty.empty_display_size = 0.05
        collection.objects.link(empty)
        empty.parent = self.root
        empty.matrix_parent_inverse = Matrix.Identity(4)
        self.empty[name] = empty


class Pose:
    def __init__(self):
        self.rot = {}            # segment -> 3x3 rotation about its joint, in its parent's posed frame
        self.offset = Vector()   # the pelvis's translation
        self.feet = {}           # 'L'/'R' -> (ankle target, 3x3 world rotation of the foot)
        self.lava = 1.0
        self.pile = None         # death: how far the rocks have fallen (0 standing, 1 heaped)

    def add(self, segment, m):
        self.rot[segment] = m @ self.rot.get(segment, Matrix.Identity(3))


def solve(pose):
    """Per segment G: rest design space to posed design space (4x4), and W: its world rotation (3x3)."""
    G, W = {}, {}
    pending = {}
    for s in RIG.order:
        parent = RIG.parent[s]
        Gp = G[parent] if parent else Matrix.Identity(4)
        Wp = W[parent] if parent else Matrix.Identity(3)
        side = s[-1] if s[-2:] in ('.L', '.R') else None
        if s.startswith('thigh') and side in pose.feet:
            pending.update(leg(G, W, pose, side))
        j = RIG.joint[s]
        if s in pending:
            R = Wp.inverted() @ pending[s]
        else:
            R = pose.rot.get(s, Matrix.Identity(3))
        D = Matrix.Translation(j) @ R.to_4x4() @ Matrix.Translation(-j)
        if parent is None:
            D = Matrix.Translation(pose.offset) @ D
        G[s] = Gp @ D
        W[s] = Wp @ R
    return G, W


def leg(G, W, pose, s):
    target, foot_rot = pose.feet[s]
    hip0, knee0, ankle0 = RIG.joint['thigh.' + s], RIG.joint['shin.' + s], RIG.joint['foot.' + s]
    hip = G[RIG.parent['thigh.' + s]] @ hip0
    l1, l2 = (knee0 - hip0).length, (ankle0 - knee0).length
    pole = foot_rot @ Vector((0, -1, 0.1))
    knee, ankle, hinge = two_bone(hip, Vector(target), l1, l2, pole)
    _, _, rest_hinge = two_bone(hip0, ankle0, l1, l2, Vector((0, -1, 0.1)))
    W_thigh = frame_from(knee - hip, hinge) @ frame_from(knee0 - hip0, rest_hinge).transposed()
    W_shin = frame_from(ankle - knee, hinge) @ frame_from(ankle0 - knee0, rest_hinge).transposed()
    return {'thigh.' + s: W_thigh, 'shin.' + s: W_shin, 'foot.' + s: foot_rot}


def apply(pose, degrees):
    RIG.root.rotation_euler = (0, 0, math.radians(degrees - 270))
    G, _ = solve(pose)
    if pose.pile is not None:
        G = heap(G, pose.pile)
    for s, empty in RIG.empty.items():
        empty.matrix_basis = G[s]
    for obj, segment, rest in PARTS:
        obj.matrix_basis = rest
        # Fallen apart, a joint's seam is no longer squeezed between two rocks: it shows as a smaller ember.
        if pose.pile is not None and obj.data.materials[0] is MATERIALS['lava']:
            obj.matrix_basis = rest @ Matrix.Scale(0.55, 4)
    for key in ('lava', 'eye', 'core'):
        MATERIALS[key].node_tree.nodes['Lava strength'].outputs[0].default_value = pose.lava
    bpy.context.view_layer.update()


def segment_bounds(segment, M):
    """The lowest point and the centre of a segment's parts under the transform M (rest space to posed space)."""
    lowest, total, count = 1e9, Vector(), 0
    for obj, s, rest in PARTS:
        if s != segment:
            continue
        for corner in obj.bound_box:
            at = M @ rest @ Vector(corner)
            lowest = min(lowest, at.z)
            total += at
            count += 1
    return lowest, (total / count if count else M.to_translation())


def heap(G, amount):
    """The remains: every body part falls as one piece - the head, each fist, the shoulders - toward the floor and
    inward, tumbling a little, the higher parts landing higher on the heap, none sinking through the floor. Seeded by
    the segment's name, so both death frames agree and the second only settles further."""
    fallen = {}
    for segment in RIG.order:
        rng = random.Random(segment)
        _, centre = segment_bounds(segment, G[segment])
        target = Vector((centre.x * 0.78 + rng.uniform(-0.03, 0.03), centre.y * 0.7 + 0.03 + rng.uniform(-0.02, 0.03),
                         0.05 + centre.z * 0.3 * (1.0 - 0.18 * (amount - 1.0))))
        target = centre.lerp(target, min(1.0, amount))
        forward = 34 if segment in ('spine', 'head') else 0
        tumble = rot(x=(forward + rng.uniform(-22, 22)) * amount, y=rng.uniform(-22, 22) * amount,
                     z=rng.uniform(-18, 18) * amount)
        M = Matrix.Translation(target) @ tumble.to_4x4() @ Matrix.Translation(-centre) @ G[segment]
        lowest, _ = segment_bounds(segment, M)
        if lowest < 0.004:
            M = Matrix.Translation(Vector((0, 0, 0.004 - lowest))) @ M
        fallen[segment] = M
    return fallen


RIG = Rig()


def mirror(v, side):
    return Vector((v[0] * side, v[1], v[2]))


def humanoid(j):
    """The golems' shared skeleton from the left side's joints (mirrored for the right)."""
    RIG.add('pelvis', j['pelvis'])
    RIG.add('spine', j['waist'], 'pelvis')
    RIG.add('head', j['neck'], 'spine')
    for side, s in ((1, 'L'), (-1, 'R')):
        RIG.add('upper_arm.' + s, mirror(j['shoulder'], side), 'spine')
        RIG.add('forearm.' + s, mirror(j['elbow'], side), 'upper_arm.' + s)
        RIG.add('fist.' + s, mirror(j['wrist'], side), 'forearm.' + s)
        RIG.add('thigh.' + s, mirror(j['hip'], side), 'pelvis')
        RIG.add('shin.' + s, mirror(j['knee'], side), 'thigh.' + s)
        RIG.add('foot.' + s, mirror(j['ankle'], side), 'shin.' + s)


def swing(a):
    """A hanging limb swung forward by a degrees (toward the golem's front, -Y)."""
    return rot(x=-a)


def spread(side, a):
    """A hanging limb lifted out to its own side by a degrees."""
    return rot(y=-a * side)


# ---------------------------------------------------------------------------------------------------------------------
# The Grunt-proportioned golem: the calibration figure (rendered beside the painted Grunt to tune the material) and,
# with a crest helm and a warning bell, the rendered candidate for the Grunt Captain.
# ---------------------------------------------------------------------------------------------------------------------
GRUNT_JOINTS = dict(pelvis=(0, 0.02, 0.34), waist=(0, 0.02, 0.42), neck=(0, -0.05, 0.69), shoulder=(0.27, 0.02, 0.68),
                    elbow=(0.38, 0.0, 0.5), wrist=(0.41, -0.02, 0.33), hip=(0.12, 0.02, 0.3),
                    knee=(0.13, -0.01, 0.17), ankle=(0.135, 0.0, 0.075))


def build_grunt(scale=1.0, captain=False):
    """Broad shoulders over a compact chest, the small head sunk between them, huge fists hanging wide and low, short
    thick legs: the painted Grunt's masses. The rocks overlap so the lava shows only in cracks."""
    humanoid({k: tuple(c * scale for c in v) for k, v in GRUNT_JOINTS.items()})
    S = scale

    def at(*v):
        return tuple(c * S for c in v)

    rock('Hips', 'pelvis', at(0, 0.02, 0.33), at(0.17, 0.12, 0.08), (0, 0, 4), 101)
    rock('Belly', 'spine', at(0, -0.04, 0.455), at(0.165, 0.11, 0.09), (6, 0, -3), 102)
    rock('Chest L', 'spine', at(0.105, -0.08, 0.6), at(0.135, 0.09, 0.13), (8, -6, 8), 103, boxy=3.6)
    rock('Chest R', 'spine', at(-0.105, -0.08, 0.6), at(0.135, 0.09, 0.13), (8, 6, -8), 104, boxy=3.6)
    rock('Back L', 'spine', at(0.09, 0.085, 0.63), at(0.13, 0.1, 0.13), (-8, 6, -6), 105, boxy=3.2)
    rock('Back R', 'spine', at(-0.09, 0.085, 0.63), at(0.13, 0.1, 0.13), (-8, -6, 6), 109, boxy=3.2)
    rock('Small of back', 'spine', at(0, 0.075, 0.47), at(0.16, 0.1, 0.085), (-4, 0, 3), 126, boxy=3.2)
    blob('Back seam', 'spine', at(0, 0.06, 0.57), at(0.16, 0.09, 0.15))
    rock('Collar', 'spine', at(0, 0.0, 0.715), at(0.18, 0.11, 0.05), (0, 0, 0), 106)
    rock('Head', 'head', at(0, -0.1, 0.75), at(0.09, 0.08, 0.075), (4, 0, 0), 107, boxy=3.8, bevel=0.14)
    rock('Brow', 'head', at(0, -0.155, 0.785), at(0.085, 0.035, 0.03), (12, 0, 0), 108, boxy=4.0, bevel=0.2)
    # Lava: a core in the chest that the plates part over, and thin seams at the joints that the rocks nearly close.
    blob('Core', 'spine', at(0, -0.02, 0.57), at(0.16, 0.1, 0.14))
    blob('Collar seam', 'spine', at(0, -0.07, 0.69), at(0.12, 0.07, 0.03))
    blob('Waist seam', 'pelvis', at(0, -0.03, 0.39), at(0.13, 0.09, 0.025))
    for side, s in ((1, 'L'), (-1, 'R')):
        rock('Shoulder ' + s, 'upper_arm.' + s, at(0.32 * side, 0.01, 0.695), at(0.15, 0.13, 0.13), (0, -10 * side, 10 * side),
             110 + side, boxy=3.0)
        rock('Arm ' + s, 'upper_arm.' + s, at(0.37 * side, 0.0, 0.56), at(0.09, 0.09, 0.09), (0, 0, 20 * side), 112 + side)
        rock('Forearm ' + s, 'forearm.' + s, at(0.4 * side, -0.01, 0.42), at(0.105, 0.105, 0.1), (0, 0, -15 * side), 114 + side,
             boxy=3.4)
        rock('Fist ' + s, 'fist.' + s, at(0.42 * side, -0.04, 0.245), at(0.125, 0.115, 0.105), (0, 0, 12 * side), 116 + side,
             boxy=3.8)
        rock('Knuckles ' + s, 'fist.' + s, at(0.41 * side, -0.13, 0.225), at(0.09, 0.045, 0.065), (0, 0, 8 * side), 118 + side,
             boxy=4.0)
        rock('Thigh ' + s, 'thigh.' + s, at(0.12 * side, 0.01, 0.235), at(0.085, 0.085, 0.075), (0, 0, 0), 120 + side)
        rock('Shin ' + s, 'shin.' + s, at(0.13 * side, -0.005, 0.13), at(0.08, 0.08, 0.07), (0, 0, 10 * side), 122 + side)
        rock('Foot ' + s, 'foot.' + s, at(0.135 * side, -0.045, 0.04), at(0.1, 0.12, 0.045), (0, 0, -8 * side), 124 + side,
             boxy=3.8)
        blob('Shoulder seam ' + s, 'spine', at(0.22 * side, -0.02, 0.655), at(0.05, 0.06, 0.05))
        blob('Elbow seam ' + s, 'upper_arm.' + s, at(0.385 * side, -0.01, 0.495), at(0.06, 0.06, 0.025))
        blob('Wrist seam ' + s, 'forearm.' + s, at(0.41 * side, -0.03, 0.33), at(0.07, 0.065, 0.022))
        blob('Hip seam ' + s, 'pelvis', at(0.12 * side, -0.01, 0.29), at(0.05, 0.05, 0.03))
        blob('Knee seam ' + s, 'thigh.' + s, at(0.13 * side, -0.02, 0.175), at(0.05, 0.05, 0.022))
        blob('Ankle seam ' + s, 'shin.' + s, at(0.135 * side, -0.015, 0.08), at(0.05, 0.05, 0.02))
        blob('Eye ' + s, 'head', at(0.034 * side, -0.172, 0.755), at(0.024, 0.012, 0.012), (0, 0, -14 * side), 'eye', 1)
    # The furnace grille under the collar: a dark ring of stone with three glowing bars.
    blob('Vent rim', 'spine', at(-0.04, -0.165, 0.58), at(0.055, 0.02, 0.055), (8, 0, 0), 'socket', 2, contour=True)
    for i, x in enumerate((-0.02, 0.0, 0.02)):
        blob('Vent bar %d' % i, 'spine', at(-0.04 + x, -0.182, 0.58), at(0.006, 0.006, 0.035), (8, 0, 0), 'lava', 1)
    if captain:
        build_captain_dress(S)


def build_captain_dress(S):
    """The Captain's brass: a crested helm on the head and the warning bell on a chain across the chest."""
    def at(*v):
        return tuple(c * S for c in v)
    slab('Helm', 'head', at(0, -0.095, 0.79), at(0.1, 0.085, 0.035), (4, 0, 0), 'brass', bevel=0.45, seed=301)
    slab('Helm brim', 'head', at(0, -0.12, 0.765), at(0.105, 0.08, 0.012), (8, 0, 0), 'brass', bevel=0.4, seed=302)
    slab('Crest', 'head', at(0, -0.07, 0.86), at(0.016, 0.085, 0.06), (-8, 0, 0), 'brass', bevel=0.45, seed=303)
    bell_profile = [(0.0, 0.0), (0.035, 0.0), (0.04, 0.006), (0.035, 0.02), (0.028, 0.045), (0.02, 0.06), (0.0, 0.066)]
    lathe('Bell', 'spine', at(0.02, -0.17, 0.5), bell_profile, S)
    blob('Clapper', 'spine', at(0.02, -0.17, 0.497), at(0.01, 0.01, 0.01), (0, 0, 0), 'brass', 1)


def lathe(name, segment, centre, profile, S, count=20):
    bm = bmesh.new()
    rings = []
    for r, z in profile:
        ring = []
        for i in range(count):
            a = 2 * math.pi * i / count
            ring.append(bm.verts.new((r * S * math.cos(a), r * S * math.sin(a), z * S)))
        rings.append(ring)
    for a, b in zip(rings, rings[1:]):
        for i in range(count):
            j = (i + 1) % count
            bm.faces.new((a[i], a[j], b[j], b[i]))
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-6)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    obj = mesh_object(name, bm)
    finish(obj, 'brass', seed=name_seed(name))
    place(obj, segment, centre, (0, 0, 0))
    return obj


# ---------------------------------------------------------------------------------------------------------------------
# Poses shared by the golems: a stance, a four-pose walk (contact, passing, contact, passing), a strike and the heap
# ---------------------------------------------------------------------------------------------------------------------
class Gait:
    def __init__(self, foot_x, ankle_z, stride, lift, bob, sway, arm, lean, twist, toe_out=6.0):
        self.foot_x, self.ankle_z, self.stride, self.lift, self.bob = foot_x, ankle_z, stride, lift, bob
        self.sway, self.arm, self.lean, self.twist, self.toe_out = sway, arm, lean, twist, toe_out


def stance(g, crouch=0.0):
    p = Pose()
    p.offset = Vector((0, 0, -crouch))
    p.feet['L'] = (Vector((g.foot_x, 0.0, g.ankle_z)), rot(z=g.toe_out))
    p.feet['R'] = (Vector((-g.foot_x, 0.0, g.ankle_z)), rot(z=-g.toe_out))
    return p


def contact(g, lead):
    """Contact: the lead foot planted forward, the trailing foot back on its toe; the hips at their lowest, the arms
    swung against the legs."""
    p = stance(g, g.bob)
    side = 1 if lead == 'L' else -1
    trail = 'R' if lead == 'L' else 'L'
    p.feet[lead] = (Vector((g.foot_x * side, -g.stride, g.ankle_z)), rot(x=-4, z=g.toe_out * side))
    p.feet[trail] = (Vector((-g.foot_x * side, g.stride, g.ankle_z + 0.35 * g.lift)), rot(x=-24, z=-g.toe_out * side))
    p.add('pelvis', rot(z=-g.twist * side))
    p.add('spine', rot(x=g.lean, z=g.twist * 1.6 * side))
    p.add('head', rot(z=-g.twist * side))
    p.add('upper_arm.' + lead, swing(-g.arm))
    p.add('upper_arm.' + trail, swing(g.arm))
    p.add('forearm.' + trail, swing(g.arm * 0.5))
    return p


def passing(g, support):
    """Passing: the support foot planted under the hips, the other lifted and swinging through beside it; the hips at
    their highest and over the support foot."""
    p = stance(g, -g.bob * 0.4)
    side = 1 if support == 'L' else -1
    free = 'R' if support == 'L' else 'L'
    p.offset += Vector((g.sway * side, 0, 0))
    p.feet[support] = (Vector((g.foot_x * side * 0.85, 0.01, g.ankle_z)), rot(z=g.toe_out * side))
    p.feet[free] = (Vector((-g.foot_x * side * 0.9, -0.01, g.ankle_z + g.lift)), rot(x=-16, z=-g.toe_out * side))
    p.add('pelvis', rot(y=-3 * side))
    p.add('spine', rot(x=g.lean * 0.8, y=2 * side))
    p.add('upper_arm.' + support, swing(g.arm * 0.25))
    p.add('upper_arm.' + free, swing(-g.arm * 0.25))
    return p


def grunt_strike(g):
    """The near (left) fist driven forward and down at the target, the body leaning in behind it; feet braced."""
    p = stance(g, 0.03)
    p.feet['L'] = (Vector((g.foot_x, -g.stride * 0.7, g.ankle_z)), rot(z=g.toe_out))
    p.feet['R'] = (Vector((-g.foot_x, g.stride * 0.6, g.ankle_z + 0.01)), rot(x=-14, z=-g.toe_out))
    p.add('pelvis', rot(z=-10))
    p.add('spine', rot(x=g.lean + 14, z=-14))
    p.add('head', rot(x=-8, z=10))
    p.add('upper_arm.L', swing(78) @ spread(1, 6))
    p.add('forearm.L', swing(8))
    p.add('upper_arm.R', swing(-30) @ spread(-1, 8))
    p.add('forearm.R', swing(30))
    return p


def golem_poses(g, strike):
    poses = {'idle': stance(g), 'step-a': contact(g, 'L'), 'step-b': contact(g, 'R'), 'strike': strike(g),
             'pass-a': passing(g, 'L'), 'pass-b': passing(g, 'R')}
    collapse = stance(g, 0.0)
    collapse.pile = 1.0
    collapse.lava = 0.85
    cool = stance(g, 0.0)
    cool.pile = 1.12
    cool.lava = 0.22
    poses['death-collapse'] = collapse
    poses['death-cool'] = cool
    return poses


# ---------------------------------------------------------------------------------------------------------------------
# The Runner: fast and fragile. A lean sprinter of basalt shards on long reverse-jointed legs, leaning into its run,
# the head thrust out low, the arms trailing; an open ribcage round a white-hot heart, three shards swept back along
# its spine. Nothing about it is a boulder, so it never reads as a small Grunt.
# ---------------------------------------------------------------------------------------------------------------------
RUNNER_JOINTS = dict(pelvis=(0, 0.04, 0.5), waist=(0, 0.04, 0.55), neck=(0, -0.11, 0.79), shoulder=(0.1, -0.05, 0.76),
                     elbow=(0.135, 0.03, 0.635), wrist=(0.145, 0.1, 0.53), hip=(0.07, 0.04, 0.48),
                     knee=(0.08, -0.075, 0.3), ankle=(0.08, 0.06, 0.13))
RUNNER_TOE = Vector((0.08, -0.07, 0.012))      # the toe's tip in the rest stance; the foot runs heel to toe
RUNNER_LEAN = 30.0                              # the torso leans into the run even standing


def build_runner():
    humanoid(RUNNER_JOINTS)
    shard = dict(boxy=2.4, count=11, merge=16.0, bevel=0.08)
    rock('Pelvis', 'pelvis', (0, 0.05, 0.5), (0.085, 0.075, 0.055), (0, 0, 6), 201, 'shard', **shard)
    # The ribcage: two rib plates and the spine plate, parted at the front over the heart.
    rock('Rib L', 'spine', (0.084, -0.02, 0.655), (0.05, 0.08, 0.095), (12, -20, 28), 202, 'shard', **shard)
    rock('Rib R', 'spine', (-0.084, -0.02, 0.655), (0.05, 0.08, 0.095), (12, 20, -28), 203, 'shard', **shard)
    rock('Blade L', 'spine', (0.05, 0.055, 0.67), (0.045, 0.055, 0.1), (-8, 10, 10), 204, 'shard', **shard)
    rock('Blade R', 'spine', (-0.05, 0.055, 0.67), (0.045, 0.055, 0.1), (-8, -10, -10), 205, 'shard', **shard)
    rock('Collar', 'spine', (0, -0.025, 0.77), (0.115, 0.07, 0.04), (10, 0, 0), 206, 'shard', **shard)
    blob('Heart', 'spine', (0, -0.06, 0.66), (0.062, 0.06, 0.078), material='core')
    blob('Heart back', 'spine', (0, 0.07, 0.67), (0.026, 0.035, 0.085), material='core')
    for k, (a, b) in enumerate((((0.03, -0.085, 0.69), (0.075, -0.075, 0.62)), ((-0.03, -0.085, 0.69), (-0.07, -0.08, 0.61)),
                                ((0.02, -0.09, 0.64), (0.045, -0.07, 0.575)), ((0.0, 0.1, 0.7), (0.04, 0.1, 0.62)),
                                ((-0.01, 0.1, 0.68), (-0.045, 0.095, 0.6)))):
        vein('Heart vein %d' % k, 'spine', a, b)
    # Three fins swept back along the spine: the silhouette's speed.
    for i, (y, z, length, tilt) in enumerate(((0.075, 0.79, 0.085, -58), (0.11, 0.7, 0.075, -62), (0.115, 0.6, 0.06, -66))):
        rock('Spine shard %d' % i, 'spine', (0, y, z), (0.022, 0.03, length), (tilt, 0, 0), 207 + i, 'shard',
             boxy=2.0, count=9, merge=18.0, bevel=0.06)
    # The head: a wedge thrust forward and low, a heavy brow over two slit eyes.
    rock('Head', 'head', (0, -0.17, 0.83), (0.058, 0.085, 0.048), (-6, 0, 0), 211, 'shard', boxy=2.6, count=12, merge=14.0)
    rock('Brow', 'head', (0, -0.2, 0.86), (0.06, 0.045, 0.022), (6, 0, 0), 212, 'shard', boxy=3.0, count=11, merge=16.0)
    rock('Jaw', 'head', (0, -0.2, 0.8), (0.04, 0.055, 0.018), (-12, 0, 0), 213, 'shard', **shard)
    blob('Neck seam', 'spine', (0, -0.09, 0.775), (0.04, 0.045, 0.03))
    for side, s in ((1, 'L'), (-1, 'R')):
        blob('Eye ' + s, 'head', (0.027 * side, -0.247, 0.838), (0.019, 0.011, 0.009), (0, 0, -18 * side), 'eye', 1)
        rock('Shoulder ' + s, 'upper_arm.' + s, (0.11 * side, -0.05, 0.77), (0.055, 0.055, 0.05), (0, 0, 15 * side),
             214 + side, 'shard', **shard)
        rock('Arm ' + s, 'upper_arm.' + s, (0.122 * side, -0.01, 0.7), (0.042, 0.042, 0.065), (32, 0, 0), 216 + side,
             'shard', **shard)
        rock('Forearm ' + s, 'forearm.' + s, (0.14 * side, 0.065, 0.585), (0.04, 0.04, 0.062), (40, 0, 0), 218 + side,
             'shard', **shard)
        rock('Hand ' + s, 'fist.' + s, (0.145 * side, 0.105, 0.525), (0.034, 0.034, 0.03), (0, 0, 0), 230 + side, 'shard',
             **shard)
        for k, (dx, dy, dz) in enumerate(((0.0, 0.03, -0.035), (0.02, 0.018, -0.04), (-0.02, 0.018, -0.04))):
            rock('Claw %s %d' % (s, k), 'fist.' + s, ((0.145 + dx) * side, 0.105 + dy, 0.52 + dz), (0.012, 0.015, 0.038),
                 (35, 0, 0), 220 + 3 * k + side, 'shard', boxy=2.0, count=9, merge=18.0, bevel=0.05)
        rock('Thigh ' + s, 'thigh.' + s, (0.075 * side, -0.02, 0.39), (0.062, 0.068, 0.11), (-30, 0, 0), 232 + side,
             'shard', **shard)
        rock('Shin ' + s, 'shin.' + s, (0.08 * side, -0.005, 0.215), (0.038, 0.04, 0.105), (38, 0, 0), 234 + side, 'shard',
             **shard)
        rock('Foot ' + s, 'foot.' + s, (0.08 * side, -0.005, 0.07), (0.032, 0.035, 0.09), (-44, 0, 0), 236 + side,
             'shard', **shard)
        rock('Toe ' + s, 'foot.' + s, (0.08 * side, -0.065, 0.02), (0.034, 0.036, 0.02), (-10, 0, 0), 238 + side, 'shard',
             **shard)
        blob('Hip seam ' + s, 'pelvis', (0.072 * side, 0.02, 0.48), (0.028, 0.03, 0.03))
        blob('Knee seam ' + s, 'thigh.' + s, (0.08 * side, -0.07, 0.3), (0.022, 0.024, 0.022))
        vein('Thigh crack ' + s, 'thigh.' + s, (0.11 * side, -0.05, 0.44), (0.12 * side, -0.07, 0.35))
        blob('Shoulder seam ' + s, 'spine', (0.085 * side, -0.05, 0.75), (0.03, 0.03, 0.03))


class RunnerGait:
    stride = 0.19      # how far ahead and behind the feet reach at contact (design units)
    lift = 0.1         # the swinging toe's height at the passing pose


def runner_foot(toe, pitch=0.0, side=1):
    """The heel target and the foot's world rotation for a toe at `toe`, the foot pitched by `pitch` degrees (+ tips
    the toe down, the heel up)."""
    s = 'L' if side > 0 else 'R'
    heel0 = RIG.joint['foot.' + s]
    toe0 = mirror(RUNNER_TOE, side)
    W = rot(x=pitch)
    return Vector(toe) - W @ (toe0 - heel0), W


def runner_stance(crouch=0.0, lean=RUNNER_LEAN):
    p = Pose()
    p.offset = Vector((0, 0, -crouch))
    p.add('spine', rot(x=lean))
    p.add('head', rot(x=-lean * 0.75))
    for side, s in ((1, 'L'), (-1, 'R')):
        heel, W = runner_foot(mirror(RUNNER_TOE, side), 0.0, side)
        p.feet[s] = (heel, W)
    return p


def runner_contact(lead):
    """Contact: the lead foot reaches far ahead and lands on its toe, the trailing leg drives back off its toe; the
    body at its lowest, the arms pumping against the legs."""
    g = RunnerGait
    p = runner_stance(0.035, RUNNER_LEAN + 4)
    side = 1 if lead == 'L' else -1
    trail = 'R' if lead == 'L' else 'L'
    heel, W = runner_foot((0.075 * side, -g.stride - 0.06, 0.02), -8, side)
    p.feet[lead] = (heel, W)
    heel, W = runner_foot((-0.075 * side, g.stride + 0.04, 0.03), 38, -side)
    p.feet[trail] = (heel, W)
    p.add('pelvis', rot(z=-8 * side))
    p.add('spine', rot(z=12 * side))
    p.add('head', rot(z=-6 * side))
    p.add('upper_arm.' + lead, swing(-40))
    p.add('forearm.' + lead, swing(20))
    p.add('upper_arm.' + trail, swing(42))
    p.add('forearm.' + trail, swing(34))
    return p


def runner_passing(support):
    """Passing: the support foot under the hips, the other leg tucked high and forward, knee up; the body at its
    highest, light on its toes."""
    p = runner_stance(-0.03, RUNNER_LEAN + 2)
    side = 1 if support == 'L' else -1
    free = 'R' if support == 'L' else 'L'
    heel, W = runner_foot((0.07 * side, 0.03, 0.012), 6, side)
    p.feet[support] = (heel, W)
    heel, W = runner_foot((-0.07 * side, 0.05, RunnerGait.lift + 0.04), 30, -side)
    p.feet[free] = (heel, W)
    p.add('pelvis', rot(y=-4 * side))
    p.add('spine', rot(y=3 * side))
    p.add('upper_arm.' + support, swing(12))
    p.add('upper_arm.' + free, swing(-12))
    return p


def runner_strike():
    """The pounce: a lunge onto the near (left) foot, the body stretched long and low, both claws thrown forward at
    the target either side of the head, so the strike reads from the front and the rear alike."""
    p = runner_stance(0.06, RUNNER_LEAN + 10)
    heel, W = runner_foot((0.08, -0.21, 0.012), -4, 1)
    p.feet['L'] = (heel, W)
    heel, W = runner_foot((-0.075, 0.22, 0.03), 40, -1)
    p.feet['R'] = (heel, W)
    p.add('head', rot(x=-8))
    for side, s in ((1, 'L'), (-1, 'R')):
        p.add('upper_arm.' + s, swing(82) @ spread(side, 26))
        p.add('forearm.' + s, swing(-8))
    return p


def runner_poses():
    poses = {'idle': runner_stance(), 'step-a': runner_contact('L'), 'step-b': runner_contact('R'),
             'strike': runner_strike(), 'pass-a': runner_passing('L'), 'pass-b': runner_passing('R')}
    for name, amount, lava in (('death-collapse', 1.0, 0.85), ('death-cool', 1.12, 0.22)):
        p = runner_stance()
        p.pile, p.lava = amount, lava
        poses[name] = p
    return poses


# ---------------------------------------------------------------------------------------------------------------------
# The Tank: slow and heavy. A massive slab of basalt as wide as it is tall, sunk low between its shoulders, arms as
# thick as the body ending in fists near the floor; soot-black foundry iron bolted over it - banded chest and back
# plates, a furnace grille, cast iron pauldrons, a brow plate over the eyes - on short thick legs in a wide stance.
# ---------------------------------------------------------------------------------------------------------------------
TANK_JOINTS = dict(pelvis=(0, 0.03, 0.36), waist=(0, 0.03, 0.42), neck=(0, -0.1, 0.71), shoulder=(0.3, 0.03, 0.74),
                   elbow=(0.43, -0.01, 0.53), wrist=(0.47, -0.07, 0.335), hip=(0.18, 0.03, 0.32),
                   knee=(0.21, -0.02, 0.19), ankle=(0.22, 0.0, 0.085))


def rivets(segment, centre, half_w, half_h, frame, depth, count_w, count_h, inset=0.02, size=0.014, bend=0.0):
    """Bolt heads round the border of a plate's face. The plate's frame (a 4x4 rotation) maps local -Y onto the
    face's outward normal, local X and Z across the face; the face sits `depth` out from the plate's centre, its
    sides `bend` further in where the plate is bent (slab)."""
    angles = frame.to_euler('XYZ')
    degrees = tuple(math.degrees(a) for a in angles)
    for i in range(count_w):
        for j in range(count_h):
            if 0 < i < count_w - 1 and 0 < j < count_h - 1:
                continue
            x = lerp(-half_w + inset, half_w - inset, i / max(1, count_w - 1))
            z = lerp(-half_h + inset, half_h - inset, j / max(1, count_h - 1))
            at = Vector(centre) + (frame @ Vector((x, -depth + bend * (x / half_w) ** 2, z, 1.0))).to_3d()
            blob('Rivet %s %d %d %.3f' % (segment, i, j, centre[0]), segment, tuple(at), (size, size * 0.6, size), degrees,
                 'rivet', 1)


def build_tank():
    humanoid(TANK_JOINTS)
    big = dict(boxy=3.4, count=14, merge=14.0, bevel=0.1)
    rock('Hips', 'pelvis', (0, 0.03, 0.34), (0.24, 0.16, 0.09), (0, 0, 3), 301, **big)
    rock('Bulk', 'spine', (0, 0.05, 0.57), (0.29, 0.2, 0.2), (0, 0, 0), 302, boxy=3.0, count=16, merge=12.0, bevel=0.08)
    rock('Hump L', 'spine', (0.12, 0.09, 0.77), (0.16, 0.14, 0.11), (-10, 8, -6), 303, **big)
    rock('Hump R', 'spine', (-0.12, 0.09, 0.77), (0.16, 0.14, 0.11), (-10, -8, 6), 304, **big)
    # Lava where the slabs part: a core behind the plates, the seam between the humps, the waist and the neck.
    blob('Core', 'spine', (0, 0.03, 0.6), (0.23, 0.17, 0.15))
    blob('Hump seam', 'spine', (0, 0.1, 0.76), (0.05, 0.12, 0.07))
    blob('Waist seam', 'pelvis', (0, -0.05, 0.4), (0.15, 0.09, 0.022))
    # Banded soot iron over the chest: an upper plate under the chin and a belly plate holding the furnace grille,
    # each bolted round its border, the core glowing in the gap between them.
    upper_rot, lower_rot = (18, 0, 0), (6, 0, 0)
    slab('Chest plate', 'spine', (0, -0.165, 0.625), (0.21, 0.03, 0.07), upper_rot, 'iron', bevel=0.35, seed=311, warp=0.05,
         bend=0.06)
    rivets('spine', (0, -0.165, 0.625), 0.21, 0.07, euler(upper_rot), 0.03, 5, 2, inset=0.028, bend=0.06)
    slab('Belly plate', 'spine', (0, -0.182, 0.47), (0.19, 0.03, 0.075), lower_rot, 'iron', bevel=0.35, seed=312, warp=0.05,
         bend=0.055)
    rivets('spine', (0, -0.182, 0.47), 0.19, 0.075, euler(lower_rot), 0.03, 5, 2, inset=0.028, bend=0.055)
    slab('Grille frame', 'spine', (0, -0.214, 0.47), (0.09, 0.012, 0.038), lower_rot, 'iron', bevel=0.3, seed=313)
    for i, x in enumerate((-0.054, -0.027, 0.0, 0.027, 0.054)):
        slab('Grille bar %d' % i, 'spine', (x, -0.221, 0.47), (0.007, 0.006, 0.028), lower_rot, 'lava', bevel=0.3,
             contour=False)
    # Banded plates over the back too, the core glowing between them.
    for name, (y, z, half_w, half_h, tilt, seed) in (('Upper back plate', (0.22, 0.66, 0.22, 0.075, -24, 314)),
                                                    ('Lower back plate', (0.245, 0.495, 0.2, 0.07, -8, 315))):
        r = (tilt, 0, 0)
        slab(name, 'spine', (0, y, z), (half_w, 0.03, half_h), r, 'iron', bevel=0.35, seed=seed, warp=0.05, bend=-0.06)
        rivets('spine', (0, y, z), half_w, half_h, euler(r) @ Matrix.Rotation(math.pi, 4, 'Z'), 0.03, 5, 2, inset=0.028,
               bend=0.06)
    # The head, broad and sunk low between the humps, under an iron brow plate.
    rock('Head', 'head', (0, -0.135, 0.745), (0.1, 0.085, 0.075), (6, 0, 0), 320, boxy=3.6, count=14, merge=14.0)
    slab('Brow plate', 'head', (0, -0.185, 0.79), (0.105, 0.03, 0.024), (22, 0, 0), 'iron', bevel=0.4, seed=321)
    blob('Neck seam', 'spine', (0, -0.11, 0.7), (0.09, 0.06, 0.035))
    for side, s in ((1, 'L'), (-1, 'R')):
        blob('Eye ' + s, 'head', (0.038 * side, -0.212, 0.752), (0.027, 0.013, 0.012), (0, 0, -12 * side), 'eye', 1)
        # The shoulders grow out of the bulk; the pauldron sits down over each like a cast iron cap, bolted at its rim.
        rock('Shoulder ' + s, 'upper_arm.' + s, (0.325 * side, 0.02, 0.735), (0.17, 0.16, 0.14), (0, -8 * side, 10 * side),
             330 + side, **big)
        dome('Pauldron ' + s, 'upper_arm.' + s, (0.335 * side, 0.01, 0.785), (0.165, 0.155, 0.08), (0, -16 * side, 0),
             'pauldron')
        for k in range(5):
            a = math.radians(-150 + 75 * k)
            at = (0.335 * side + 0.152 * math.cos(a) * side, 0.01 + 0.143 * math.sin(a), 0.792)
            blob('Pauldron rivet %s %d' % (s, k), 'upper_arm.' + s, at, (0.013, 0.013, 0.01), (0, 0, 0), 'rivet', 1)
        rock('Arm ' + s, 'upper_arm.' + s, (0.395 * side, 0.01, 0.6), (0.105, 0.105, 0.1), (0, 0, 20 * side), 334 + side,
             **big)
        rock('Forearm ' + s, 'forearm.' + s, (0.445 * side, -0.03, 0.43), (0.125, 0.12, 0.11), (0, 0, -12 * side),
             336 + side, **big)
        rock('Fist ' + s, 'fist.' + s, (0.475 * side, -0.09, 0.215), (0.15, 0.14, 0.125), (0, 0, 10 * side), 340 + side,
             boxy=3.8, count=14, merge=14.0, bevel=0.1)
        rock('Knuckles ' + s, 'fist.' + s, (0.465 * side, -0.215, 0.19), (0.11, 0.05, 0.075), (0, 0, 8 * side), 342 + side,
             boxy=4.0, count=14, merge=14.0, bevel=0.1)
        rock('Thigh ' + s, 'thigh.' + s, (0.185 * side, 0.02, 0.255), (0.115, 0.11, 0.085), (0, 0, 0), 344 + side, **big)
        rock('Shin ' + s, 'shin.' + s, (0.21 * side, -0.01, 0.14), (0.105, 0.1, 0.075), (0, 0, 8 * side), 346 + side, **big)
        rock('Foot ' + s, 'foot.' + s, (0.22 * side, -0.055, 0.045), (0.125, 0.145, 0.048), (0, 0, -6 * side), 348 + side,
             boxy=3.8, count=14, merge=14.0, bevel=0.1)
        blob('Shoulder seam ' + s, 'spine', (0.22 * side, -0.03, 0.7), (0.06, 0.08, 0.06))
        blob('Elbow seam ' + s, 'upper_arm.' + s, (0.43 * side, -0.02, 0.53), (0.058, 0.058, 0.026))
        blob('Wrist seam ' + s, 'forearm.' + s, (0.47 * side, -0.06, 0.335), (0.066, 0.062, 0.022))
        blob('Hip seam ' + s, 'pelvis', (0.18 * side, -0.01, 0.31), (0.052, 0.052, 0.028))
        blob('Knee seam ' + s, 'thigh.' + s, (0.21 * side, -0.03, 0.19), (0.052, 0.052, 0.021))


def dome(name, segment, centre, radii, rot_, material):
    """A domed shell (a pauldron): the top of a flattened sphere, its rim rolled under."""
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=6, radius=1.0)
    cut = [v for v in bm.verts if v.co.z < -0.05]
    bmesh.ops.delete(bm, geom=cut, context='VERTS')
    for v in bm.verts:
        v.co = Vector((v.co.x * radii[0], v.co.y * radii[1], max(v.co.z, 0.0) * radii[2]))
    bmesh.ops.contextual_create(bm, geom=[e for e in bm.edges if e.is_boundary])
    obj = mesh_object(name, bm)
    finish(obj, material, seed=name_seed(name))
    place(obj, segment, centre, rot_)
    return obj


def band(name, segment, centre, radius, half_height, material, count=20):
    """A ring of iron round a limb (a bracer): a short thick-walled cylinder."""
    bm = bmesh.new()
    outer_lo, outer_hi, inner_lo, inner_hi = [], [], [], []
    for i in range(count):
        a = 2 * math.pi * i / count
        c, s_ = math.cos(a), math.sin(a)
        outer_lo.append(bm.verts.new((c * radius, s_ * radius, -half_height)))
        outer_hi.append(bm.verts.new((c * radius, s_ * radius, half_height)))
        inner_lo.append(bm.verts.new((c * radius * 0.86, s_ * radius * 0.86, -half_height)))
        inner_hi.append(bm.verts.new((c * radius * 0.86, s_ * radius * 0.86, half_height)))
    for i in range(count):
        j = (i + 1) % count
        bm.faces.new((outer_lo[i], outer_lo[j], outer_hi[j], outer_hi[i]))
        bm.faces.new((inner_hi[i], inner_hi[j], inner_lo[j], inner_lo[i]))
        bm.faces.new((outer_hi[i], outer_hi[j], inner_hi[j], inner_hi[i]))
        bm.faces.new((inner_lo[i], inner_lo[j], outer_lo[j], outer_lo[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bmesh.ops.bevel(bm, geom=[e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) < 1e-6], offset=0.008,
                    offset_type='OFFSET', segments=1, profile=0.5, affect='EDGES', clamp_overlap=True)
    obj = mesh_object(name, bm)
    finish(obj, material, seed=name_seed(name))
    place(obj, segment, centre, (0, 0, 0))
    return obj


def tank_strike(g):
    """The slam: both fists hammered down on the floor ahead, the body thrown forward over them, knees bent."""
    p = stance(g, 0.06)
    p.feet['L'] = (Vector((g.foot_x, -0.04, g.ankle_z)), rot(z=g.toe_out))
    p.feet['R'] = (Vector((-g.foot_x, 0.08, g.ankle_z)), rot(x=-8, z=-g.toe_out))
    p.add('spine', rot(x=26))
    p.add('head', rot(x=-18))
    for side, s in ((1, 'L'), (-1, 'R')):
        p.add('upper_arm.' + s, swing(62) @ spread(side, -6))
        p.add('forearm.' + s, swing(24))
    return p


def tank_poses():
    g = Gait(0.2, 0.085, 0.06, 0.045, 0.03, 0.045, 12, 6, 7, toe_out=10)
    poses = golem_poses(g, tank_strike)
    # Heavy steps: the body rolls onto the planted leg and sinks into each contact.
    for name, side in (('step-a', 1), ('step-b', -1)):
        poses[name].add('spine', rot(y=4 * side))
        poses[name].offset += Vector((0.02 * side, 0, -0.01))
    for name, side in (('pass-a', 1), ('pass-b', -1)):
        poses[name].add('spine', rot(y=6 * side))
    return poses


# ---------------------------------------------------------------------------------------------------------------------
# The Grunt Captain's brass, rendered alone for the kitbash (build_foundry_enemies.py lays it over the painted
# Grunt's frames): a crested captain's helm and the warning bell, each with its anchor on the floor origin - the helm's
# brim centre, the bell's hanging loop - so the pack step places it by that one point.
# ---------------------------------------------------------------------------------------------------------------------
def build_brass_props():
    RIG.add('helm', (0, 0, 0))
    RIG.add('bell', (0, 0, 0))
    # The helm: a faceted brass dome over a rolled brim, a tall comb crest running front to back, swept up toward the
    # back as a captain's crest is, bolted at the brim.
    dome('Helm dome', 'helm', (0, 0, 0.006), (0.118, 0.108, 0.088), (0, 0, 0), 'brass')
    band('Helm brim', 'helm', (0, 0, 0.004), 0.124, 0.014, 'brass')
    crest = bmesh.new()
    outline_pts = [(0.0, -0.105, 0.055), (0.0, -0.1, 0.115), (0.0, -0.07, 0.17), (0.0, -0.02, 0.205), (0.0, 0.04, 0.222),
                   (0.0, 0.092, 0.205), (0.0, 0.128, 0.16), (0.0, 0.135, 0.08), (0.0, 0.06, 0.07), (0.0, -0.04, 0.07)]
    for side in (-1, 1):
        for x, y, z in outline_pts:
            crest.verts.new((0.017 * side, y, z))
    crest.verts.ensure_lookup_table()
    n = len(outline_pts)
    crest.faces.new([crest.verts[i] for i in range(n)])
    crest.faces.new([crest.verts[n + i] for i in reversed(range(n))])
    for i in range(n):
        j = (i + 1) % n
        crest.faces.new((crest.verts[i], crest.verts[n + i], crest.verts[n + j], crest.verts[j]))
    bmesh.ops.recalc_face_normals(crest, faces=crest.faces[:])
    bmesh.ops.bevel(crest, geom=crest.edges[:], offset=0.004, offset_type='OFFSET', segments=1, profile=0.5,
                    affect='EDGES', clamp_overlap=True)
    obj = mesh_object('Helm crest', crest)
    finish(obj, 'brass', seed=name_seed('Helm crest'))
    place(obj, 'helm', (0, 0, 0), (0, 0, 0))
    for k in range(7):
        a = math.radians(-90 + 30 * (k - 3))
        blob('Helm rivet %d' % k, 'helm', (0.126 * math.cos(a), 0.116 * math.sin(a), 0.006), (0.011, 0.011, 0.011),
             (0, 0, 0), 'rivet', 1)
    # The warning bell: a cast bell on its loop, the clapper showing under the lip.
    profile = [(0.0, -0.002), (0.05, -0.002), (0.056, 0.004), (0.05, 0.014), (0.036, 0.04), (0.03, 0.066),
               (0.026, 0.084), (0.012, 0.094), (0.0, 0.096)]
    flipped = [(r, -0.11 + z) for r, z in profile]
    lathe('Bell', 'bell', (0, 0, 0), flipped, 1.0)
    loop = bmesh.new()
    bmesh.ops.create_cone(loop, cap_ends=True, segments=10, radius1=0.016, radius2=0.016, depth=0.006)
    obj = mesh_object('Bell loop', loop)
    finish(obj, 'brass', seed=name_seed('Bell loop'))
    place(obj, 'bell', (0, 0, -0.006), (90, 0, 0))
    blob('Bell clapper', 'bell', (0, 0, -0.112), (0.014, 0.014, 0.014), (0, 0, 0), 'brass', 1)


BRASS_FRAMES = {
    # name: (prop, facing degrees, tilt about the prop's own Y axis, its own X axis)
    'helm-front': ('helm', 245, 0, 0),
    'helm-rear': ('helm', 120, 0, 0),
    'bell-front': ('bell', 245, 0, 0),
    'helm-fallen': ('helm', 245, 72, -10),
    'bell-fallen': ('bell', 245, 0, 84),
}


def brass_pose(name):
    prop, degrees, tilt_y, tilt_x = BRASS_FRAMES[name]
    p = Pose()
    p.offset = Vector((0, 0, 0))
    for other in ('helm', 'bell'):
        p.rot[other] = Matrix.Diagonal((0, 0, 0)) if other != prop else rot(x=tilt_x, y=tilt_y)
    return p, degrees


# ---------------------------------------------------------------------------------------------------------------------
# The enemies
# ---------------------------------------------------------------------------------------------------------------------
SPECS = {
    # canvas (final texels, wide x high) and the floor origin's texel corner from the canvas's bottom left
    'grunt': dict(canvas=(180, 180), pivot=(90, 18)),
    'captain': dict(canvas=(208, 208), pivot=(104, 20)),
    'runner': dict(canvas=(176, 168), pivot=(88, 18), facings={'front': 230, 'rear': 130}, scale=1.08),
    'tank': dict(canvas=(240, 216), pivot=(120, 34)),
    # The Captain's brass alone, each prop's anchor on the floor origin.
    'captain-brass': dict(canvas=(80, 80), pivot=(40, 36)),
}

if ENEMY == 'runner':
    build_runner()
    POSE_SET = runner_poses()
elif ENEMY == 'tank':
    build_tank()
    POSE_SET = tank_poses()
elif ENEMY == 'grunt':
    build_grunt()
    POSE_SET = golem_poses(Gait(0.135, 0.075, 0.07, 0.06, 0.018, 0.02, 26, 4, 6), grunt_strike)
elif ENEMY == 'captain':
    build_grunt(1.15, captain=True)
    g = Gait(0.17, 0.08, 0.08, 0.07, 0.02, 0.022, 24, 4, 6)
    POSE_SET = golem_poses(g, grunt_strike)
elif ENEMY == 'captain-brass':
    build_brass_props()
    POSE_SET = None
else:
    sys.exit('Unknown enemy ' + ENEMY)

CANVAS = SPECS[ENEMY]['canvas']
PIVOT = SPECS[ENEMY]['pivot']
# The whole figure's size on screen (SPECS 'scale'): a design drawn in its own units, set to its pixel look's size.
SCALE = SPECS[ENEMY].get('scale', 1.0)
RIG.root.scale = (SCALE, SCALE, SCALE * HEIGHT)

# ---------------------------------------------------------------------------------------------------------------------
# Camera, lava mask and the frame loop
# ---------------------------------------------------------------------------------------------------------------------
scene.render.resolution_x = CANVAS[0] * SS
scene.render.resolution_y = CANVAS[1] * SS
camera_data = bpy.data.cameras.new('Game view: orthographic, 30 degrees down, looking north')
camera_data.type = 'ORTHO'
camera_data.ortho_scale = max(CANVAS) / PPU
camera_data.clip_start = 0.1
camera_data.clip_end = 100
camera = bpy.data.objects.new('Camera', camera_data)
collection.objects.link(camera)
scene.camera = camera
centre = (CANVAS[0] / 2 - PIVOT[0]) / PPU * RIGHT + (CANVAS[1] / 2 - PIVOT[1]) / PPU * UP
camera.location = centre - FORWARD * 20
camera.rotation_euler = (math.pi / 2 - ELEVATION, 0, 0)

# The lava mask: the 'lava' AOV (the emissive parts' strength), written beside each frame by the compositor.
aov = scene.view_layers[0].aovs.add()
aov.name = 'lava'
aov.type = 'VALUE'
scene.use_nodes = True
tree = scene.node_tree
for node in list(tree.nodes):
    tree.nodes.remove(node)
layers = tree.nodes.new('CompositorNodeRLayers')
composite = tree.nodes.new('CompositorNodeComposite')
tree.links.new(layers.outputs['Image'], composite.inputs['Image'])
mask_out = tree.nodes.new('CompositorNodeOutputFile')
mask_out.format.file_format = 'PNG'
mask_out.format.color_mode = 'BW'
mask_out.format.color_depth = '16'
tree.links.new(layers.outputs['lava'], mask_out.inputs[0])


def all_frame_names():
    if POSE_SET is None:
        return list(BRASS_FRAMES)
    return ['%s-%s' % (facing, pose) for facing in FACINGS for pose in POSES] + DEATHS


def frame_names():
    return [n for n in all_frame_names() if not WANTED or n in WANTED.split(',')]


def pose_of(name):
    if POSE_SET is None:
        return brass_pose(name)
    facings = SPECS[ENEMY].get('facings', FACINGS)
    if name.startswith('death'):
        return POSE_SET[name], facings['front']
    facing, pose = name.split('-', 1)
    return POSE_SET[pose], facings[facing]


def screen_extent():
    """The posed figure's extent on screen in world units from the floor origin (left, right, bottom, top), from
    every part's bounding box: recorded per frame in frames.json, for choosing the canvas (the pack step refuses a
    frame that touches the canvas's edge)."""
    lo_x = lo_y = 1e9
    hi_x = hi_y = -1e9
    for obj, _, _ in PARTS:
        for corner in obj.bound_box:
            at = obj.matrix_world @ Vector(corner)
            x, y = at.dot(RIGHT), at.dot(UP)
            lo_x, hi_x, lo_y, hi_y = min(lo_x, x), max(hi_x, x), min(lo_y, y), max(hi_y, y)
    return lo_x, hi_x, lo_y, hi_y


def render_all():
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'mask').mkdir(exist_ok=True)
    started = time.time()
    records = []
    for name in frame_names():
        pose, degrees = pose_of(name)
        apply(pose, degrees)
        scene.render.filepath = str(OUT / (name + '.png'))
        mask_out.base_path = str(OUT / 'mask')
        mask_out.file_slots[0].path = name + '_'
        bpy.ops.render.render(write_still=True)
        written = OUT / 'mask' / ('%s_%04d.png' % (name, scene.frame_current))
        target = OUT / 'mask' / (name + '.png')
        if target.exists():
            target.unlink()
        written.rename(target)
        records.append(dict(name=name, file=name + '.png', mask='mask/' + name + '.png', facing_degrees=degrees,
                            lava=pose.lava, units=[round(c, 6) for c in screen_extent()]))
        print('FOUNDRY_FRAME', ENEMY, name, '%.1fs' % (time.time() - started), flush=True)
    meta_path = OUT / 'frames.json'
    meta = dict(blender=bpy.app.version_string, enemy=ENEMY, ppu=PPU, supersample=SS, canvas=CANVAS, pivot=PIVOT,
                elevation_degrees=30, height_compensation=HEIGHT, facings=SPECS[ENEMY].get('facings', FACINGS),
                frames=records)
    if meta_path.exists() and WANTED:
        old = json.loads(meta_path.read_text())
        keep = {r['name']: r for r in old.get('frames', [])}
        for r in records:
            keep[r['name']] = r
        meta['frames'] = list(keep.values())
    meta_path.write_text(json.dumps(meta, indent=1))


def bake_timeline():
    """Every pose keyed on the joints and rocks along the timeline, a marker naming each, so the .blend scrubs."""
    names = all_frame_names()
    for frame, name in enumerate(names, start=1):
        pose, degrees = pose_of(name)
        apply(pose, degrees)
        RIG.root.keyframe_insert('rotation_euler', frame=frame)
        for empty in RIG.empty.values():
            empty.keyframe_insert('location', frame=frame)
            empty.keyframe_insert('rotation_euler', frame=frame)
            empty.keyframe_insert('scale', frame=frame)
        for obj, _, _ in PARTS:
            obj.keyframe_insert('location', frame=frame)
            obj.keyframe_insert('rotation_euler', frame=frame)
        scene.timeline_markers.new(name, frame=frame)
    scene.frame_start, scene.frame_end = 1, len(names)
    scene.frame_set(1)


if RENDER:
    render_all()
bake_timeline()
BLEND.parent.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(BLEND), compress=True)
print('FOUNDRY_COMPLETE', ENEMY, 'rocks', len(PARTS), flush=True)
