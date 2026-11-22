"""The Vanguard rebuilt in Ilya's Blender pipeline (a pilot, 2026-11-22): a reproducible stylised plate knight, rig,
animation and sprite render, in the style, camera and sprite contract of BuildBlenderArcanist.py.

Run with Blender 4.5 LTS, headless:

    blender --background --factory-startup --python Tools/ArtReview/BuildBlenderVanguard.py -- [options]

Options after the "--":
    --out DIR        raw frames and frames.json (default TestResults/vanguard-render, git-ignored)
    --blend PATH     the saved scene (default ArtDirection/2026-11-22/vanguard-blender-01/vanguard.blend)
    --facings LIST   a comma-separated subset of south,south-east,east,north-east,north,north-west,west,south-west
    --clips LIST     a comma-separated subset of idle,walk,walk-strike,strike,cast,hit,death,shield-rush
    --samples N      Cycles samples per render pixel (default 10)
    --preview        only the first frame of each clip (--frame N: that frame instead)
    --no-render      build and save the .blend, render nothing

Who it is: the painted Vanguard (ArtDirection/2026-09-17/vanguard-directions-01/front-idle-v1.png) - a chunky knight in
blue steel plate, gold trim and gold rivet bosses, silver edging on the plates, a rounded visored helm with a gold crest
and a slit that glows blue, a dark blue cape, chain mail at the joints, a brown belt; an arming sword in the right hand
and a round buckler in the left (the game's Sword loadout draws both). Its proportions are Ilya's "dark cute": a big
helm sunk between the pauldrons, a barrel chest, short legs and big round sabatons.

Nothing here is generated imagery: the geometry is lofted rings and deformed primitives, the plate rides one bone each
rigidly (the breastplate, the cloth and the mail are weighted from the geometry), the pauldrons and tassets follow
their limbs part of the way (BuildBlenderWarden.py's followers), the shading is Ilya's toon node group fed by authored
palettes sampled from the painting,
and every frame is a pose computed by the functions below. One model renders every frame and every facing.

Camera, light and pivot are BuildBlenderArcanist.py's, unchanged: orthographic, looking north and down 30 degrees, the
rig stretched by 1/cos(30), eight facings by turning the rig with the camera and the key fixed (the light on the
screen's upper left, the sword in the right hand, nothing mirrored), the floor origin on the same render pixel in every
frame. The clips are the game's RenderedMotion with its timings (RenderedCharacterArtSet), plus docs/57's Shield Rush.
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


OUT = Path(option('--out', str(ROOT / 'TestResults/vanguard-render'))).resolve()
BLEND = Path(option('--blend', str(ROOT / 'ArtDirection/2026-11-22/vanguard-blender-01/vanguard.blend'))).resolve()
SAMPLES = int(option('--samples', 10))
PREVIEW = bool(option('--preview', False))
PREVIEW_FRAME = option('--frame')
RENDER = not option('--no-render', False)

# ---- The sprite contract: BuildBlenderArcanist.py's, so the frames pack and draw as Ilya's do ----
PPU = 100
SS = 4
CANVAS = 2.2
CANVAS_HEIGHT = 2.5
PIVOT = (110, 45)
ELEVATION = math.radians(30)
HEIGHT = 1 / math.cos(ELEVATION)
FACINGS = [('south', 270), ('south-east', 315), ('east', 0), ('north-east', 45), ('north', 90),
           ('north-west', 135), ('west', 180), ('south-west', 225)]
# The run is driven by distance. The legs are shorter than Ilya's, so a cycle of two steps covers 0.9 floor units
# (Ilya 1.2): RenderedCharacterArtSet's StrideLength is per set. Each foot is planted DUTY of the cycle, sliding back
# at the floor's speed; the rest is a short flight, a trot.
STRIDE = 0.9
DUTY = 0.4
CLIPS = {  # frames, frames a second, loops: RenderedMotion's timings (Ilya's art set), then the Rush
    'idle': (8, 5.0, True),
    'walk': (8, 0.0, True),
    'walk-strike': (8, 0.0, True),
    'strike': (5, 20.0, False),
    'cast': (5, 14.0, False),
    'hit': (3, 20.0, False),
    'death': (5, 9.0, False),
    # Not a RenderedMotion: docs/57's Shield Rush, 4 units in 0.3 s, shown while the dash lasts.
    'shield-rush': (4, 13.3, False),
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


def V(x, y, z):
    return Vector((x, y, z))


# ---------------------------------------------------------------------------------------------------------------------
# Scene: BuildBlenderArcanist.py's
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
scene.render.resolution_x = int(CANVAS * PPU * SS)
scene.render.resolution_y = int(round(CANVAS_HEIGHT * PPU * SS))
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
KEY = (-0.52 * RIGHT + 0.78 * UP + 0.36 * TOWARD).normalized()
RIM = (-0.65 * RIGHT + 0.55 * UP - 0.52 * TOWARD).normalized()
WARM_RIM = (0.55 * RIGHT - 0.3 * UP - 0.78 * TOWARD).normalized()
HALF = (KEY + TOWARD).normalized()


# ---------------------------------------------------------------------------------------------------------------------
# Shading: Ilya's toon group, unchanged but for its point light, which here is the forge heat in the blade
# ---------------------------------------------------------------------------------------------------------------------
def build_toon_group():
    ng = bpy.data.node_groups.new('Vanguard toon', 'ShaderNodeTree')
    io = ng.interface
    for name, kind, default in [
            ('Light', 'NodeSocketColor', (1, 1, 1, 1)), ('Mid', 'NodeSocketColor', (.5, .5, .5, 1)),
            ('Shadow', 'NodeSocketColor', (.2, .2, .2, 1)), ('Deep', 'NodeSocketColor', (.05, .05, .05, 1)),
            ('Glow', 'NodeSocketColor', (0, 0, 0, 1)), ('Spec', 'NodeSocketFloat', 0.0),
            ('Spec Color', 'NodeSocketColor', (1, 1, 1, 1)), ('Rim', 'NodeSocketFloat', 0.35),
            ('Occlusion', 'NodeSocketFloat', 1.0), ('Band Shift', 'NodeSocketFloat', 0.0),
            ('Glint', 'NodeSocketFloat', 0.0)]:
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

    normal = geo.outputs['Normal']
    half_lambert = math_node('MULTIPLY_ADD', dot(normal, vec(KEY)), 0.5, 0.5)
    lit = math_node('ADD', half_lambert, gi.outputs['Band Shift'])
    first = step(lit, 0.32, 0.56)
    second = step(lit, 0.6, 0.86)
    colour = mix(first, gi.outputs['Shadow'], gi.outputs['Mid'])
    colour = mix(second, colour, gi.outputs['Light'])
    colour = add_colour(colour, gi.outputs['Light'], math_node('MULTIPLY', step(lit, 0.86, 0.98), 0.1))
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
    closed = math_node('SUBTRACT', 1.0, step(gi.outputs['Occlusion'], 0.3, 0.97))
    colour = mix(closed, colour, gi.outputs['Deep'])
    bounce = math_node('MULTIPLY', math_node('MAXIMUM', dot(normal, vec(Vector((0, 0, -1)))), 0.0), 0.16)
    warm = n.new('ShaderNodeRGB')
    warm.outputs[0].default_value = lin('#FF7A3A')
    colour = add_colour(colour, warm.outputs[0], bounce)
    # The forge heat: a warm point at the blade's middle, placed and scaled every frame by apply() - out at rest, up
    # through the Forge Burst - lights what faces it (the helm's side, the pauldron, the fist, the gold).
    forge = n.new('ShaderNodeCombineXYZ')
    forge.name = 'Forge position'
    forge_strength = n.new('ShaderNodeValue')
    forge_strength.name = 'Forge strength'
    forge_strength.outputs[0].default_value = 0.0
    to_light = n.new('ShaderNodeVectorMath')
    to_light.operation = 'SUBTRACT'
    l.new(forge.outputs[0], to_light.inputs[0])
    l.new(geo.outputs['Position'], to_light.inputs[1])
    length = n.new('ShaderNodeVectorMath')
    length.operation = 'LENGTH'
    l.new(to_light.outputs['Vector'], length.inputs[0])
    toward = n.new('ShaderNodeVectorMath')
    toward.operation = 'NORMALIZE'
    l.new(to_light.outputs['Vector'], toward.inputs[0])
    falloff = math_node('DIVIDE', 1.0, math_node('ADD', 1.0, math_node('POWER', math_node(
        'DIVIDE', length.outputs['Value'], 0.16), 2.0)))
    ember_light = math_node('MULTIPLY', math_node('MAXIMUM', dot(normal, toward.outputs['Vector']), 0.0), falloff)
    ember_light = math_node('MULTIPLY', math_node('MULTIPLY', ember_light, forge_strength.outputs[0]), 0.32)
    ember = n.new('ShaderNodeRGB')
    ember.outputs[0].default_value = lin('#FF8A3D')
    colour = add_colour(colour, ember.outputs[0], ember_light)
    facing = n.new('ShaderNodeLayerWeight')
    facing.inputs['Blend'].default_value = 0.4
    rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.55, 0.85),
                    math_node('MAXIMUM', dot(normal, vec(RIM)), 0.0))
    rim = math_node('MULTIPLY', rim, gi.outputs['Rim'])
    rim_colour = n.new('ShaderNodeRGB')
    rim_colour.outputs[0].default_value = lin('#C9D6FF')
    colour = add_colour(colour, rim_colour.outputs[0], math_node('MULTIPLY', rim, 0.45))
    warm_rim = math_node('MULTIPLY', step(facing.outputs['Facing'], 0.7, 0.92),
                         math_node('MAXIMUM', dot(normal, vec(WARM_RIM)), 0.0))
    warm_rim_colour = n.new('ShaderNodeRGB')
    warm_rim_colour.outputs[0].default_value = lin('#FF9C55')
    colour = add_colour(colour, warm_rim_colour.outputs[0], math_node('MULTIPLY', warm_rim, 0.55))
    half = math_node('MAXIMUM', dot(normal, vec(HALF)), 0.0)
    spec = math_node('MULTIPLY', step(math_node('POWER', half, 48.0), 0.35, 0.55), gi.outputs['Spec'])
    colour = add_colour(colour, gi.outputs['Spec Color'], spec)
    glint_colour = n.new('ShaderNodeRGB')
    glint_colour.outputs[0].default_value = lin('#FFFDF0')
    glint = math_node('MULTIPLY', step(math_node('POWER', half, 220.0), 0.4, 0.7), gi.outputs['Glint'])
    colour = add_colour(colour, glint_colour.outputs[0], glint)
    glow = n.new('ShaderNodeMixRGB')
    glow.blend_type = 'ADD'
    glow.inputs['Fac'].default_value = 1.0
    l.new(colour, glow.inputs['Color1'])
    l.new(gi.outputs['Glow'], glow.inputs['Color2'])
    l.new(glow.outputs['Color'], go.inputs['Color'])
    return ng


TOON = build_toon_group()

PALETTES = {
    # light, mid, shadow, deep - sampled from the painted Vanguard (front-idle-v1.png): its lit plates #6E98FD,
    # #446AD2, its shadowed ones #22326A, its deepest #0A1E55.
    'plate': ('#7299FA', '#3A5ECC', '#1F3082', '#0A1442'),
    # The cape and the tabard: the same blue as cloth, deeper and less bright, so the plate reads in front of it.
    'cape': ('#3F55B0', '#253784', '#162155', '#090E2A'),
    'cape-lining': ('#2C3A7A', '#1C275A', '#131A3D', '#090C20'),
    'silver': ('#FBFCFF', '#CBD0DC', '#868C9E', '#3C4052'),
    # The painted gold: #FCD366 lit, #DC9852 and #B57333 local, #804C21 in shadow.
    'gold': ('#FFE08A', '#DA9A4A', '#8E5524', '#40230C'),
    'mail': ('#7A7D8E', '#4A4C5A', '#2C2D37', '#15151B'),
    'mail-dark': ('#55576A', '#33343F', '#1F2028', '#0E0E13'),
    'leather': ('#A06A40', '#77492A', '#4A2A17', '#24130A'),
    'grip': ('#6E4A36', '#4A3024', '#2C1C15', '#150D0A'),
    'steel': ('#F5F8FF', '#B9C3D6', '#717C94', '#323848'),
    'slit': ('#1A1D33', '#10121F', '#0A0B14', '#05060B'),
}
OUTLINE = '#120F1D'


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

    def toon(self, palette, spec=0.0, spec_color='#FFF4D6', rim=0.35, band_shift=0.0, occlusion=True, glint=0.0):
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
    # Blue steel: the painted plates' hard white highlight on every round plane turned to the key, and a glint.
    m['plate'] = plain('Blue steel plate', 'plate', spec=0.95, spec_color='#E2ECFF', glint=1.3, rim=0.45,
                       band_shift=-0.08)
    m['silver'] = plain('Silver edging', 'silver', spec=0.8, spec_color='#FFFFFF', glint=1.2, rim=0.4)
    m['gold'] = plain('Gold trim', 'gold', spec=1.15, spec_color='#FFF1C4', glint=1.5, rim=0.3)
    m['leather'] = plain('Belt leather', 'leather', spec=0.18, spec_color='#E8C49A', rim=0.25)
    m['grip'] = plain('Grip leather', 'grip', spec=0.1, rim=0.2)
    m['slit'] = plain('Visor recess', 'slit', rim=0.0)

    # Chain mail: dark iron with a fine ring speckle fixed to the undeformed surface.
    s = Shading('Chain mail')
    rings = s.step(s.noise(38.0, 0.0), 0.48, 0.56)
    m['mail'] = s.finish(s.mix(rings, s.toon('mail', spec=0.4, spec_color='#C8D0E8', rim=0.3), s.toon('mail-dark')))

    # The cape: dark blue cloth, a deeper lining inside, darkening toward the hem.
    s = Shading('Cape')
    u, v = s.uv()
    hem = s.math('MULTIPLY', s.math('SUBTRACT', 1.0, s.step(v, 0.0, 0.45)), 0.45)
    colour = s.mix(hem, s.toon('cape', rim=0.5), s.toon('cape-lining', rim=0.3))
    m['cape'] = s.finish(s.mix(s.backfacing(), colour, s.toon('cape-lining', rim=0.0)))

    # The tabard: the cape's blue cloth with a silver edge down both sides and along the hem.
    s = Shading('Tabard')
    u, v = s.uv()
    edge = s.math('MAXIMUM', s.math('MAXIMUM', s.math('LESS_THAN', u, 0.1), s.math('GREATER_THAN', u, 0.9)),
                  s.math('LESS_THAN', v, 0.1))
    colour = s.mix(edge, s.toon('cape', rim=0.45, band_shift=0.04), s.toon('silver', spec=0.6, spec_color='#FFFFFF'))
    m['tabard'] = s.finish(s.mix(s.backfacing(), colour, s.toon('cape-lining', rim=0.0)))

    # The tassets: blue plate edged in silver along the hem and the sides.
    s = Shading('Tasset')
    u, v = s.uv()
    edge = s.math('MAXIMUM', s.math('MAXIMUM', s.math('LESS_THAN', u, 0.07), s.math('GREATER_THAN', u, 0.93)),
                  s.math('LESS_THAN', v, 0.16))
    colour = s.mix(edge, s.toon('plate', spec=0.95, spec_color='#E2ECFF', glint=1.3, rim=0.45, band_shift=-0.08),
                   s.toon('silver', spec=0.8, spec_color='#FFFFFF', glint=1.0))
    m['tasset'] = s.finish(s.mix(s.backfacing(), colour, s.toon('plate', rim=0.0, band_shift=-0.25)))

    # The visor's light: a blue glow, hot white at its core; its strength flickers and dies with the knight.
    s = Shading('Visor glow')
    strength, node = s.value(1.0)
    node.name = 'Ember strength'
    ramp = s.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = 0.2
    ramp.color_ramp.elements[0].color = lin('#69B8FF')
    ramp.color_ramp.elements[1].position = 0.6
    ramp.color_ramp.elements[1].color = lin('#F2FBFF')
    s.links.new(s.noise(14.0, 1.0), ramp.inputs[0])
    m['eye'] = s.finish(ramp.outputs[0], s.math('MULTIPLY', strength, 1.25))

    # The blade: bright steel that heats to the forge's orange through the Forge Burst ('Forge glow', 0 at rest).
    s = Shading('Steel blade')
    steel = s.toon('steel', spec=1.1, spec_color='#FFFFFF', glint=1.8, rim=0.6)
    heat, node = s.value(0.0)
    node.name = 'Forge glow'
    ramp = s.nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = 0.25
    ramp.color_ramp.elements[0].color = lin('#FF5212')
    ramp.color_ramp.elements[1].position = 0.85
    ramp.color_ramp.elements[1].color = lin('#FFD27A')
    s.links.new(s.noise(18.0, 1.0), ramp.inputs[0])
    m['blade'] = s.finish(s.mix(heat, steel, ramp.outputs[0]), s.math('MULTIPLY_ADD', heat, 0.35, 1.0))

    # The sword's arc: a pale steel smear, half see-through, on the strike's and the Burst's fast frames only.
    s = Shading('Swing smear')
    emit = s.nodes.new('ShaderNodeEmission')
    tint = s.nodes.new('ShaderNodeRGB')
    tint.name = 'Smear colour'
    tint.outputs[0].default_value = lin('#E6F0FF')
    s.links.new(tint.outputs[0], emit.inputs['Color'])
    transparent = s.nodes.new('ShaderNodeBsdfTransparent')
    mix = s.nodes.new('ShaderNodeMixShader')
    mix.inputs['Fac'].default_value = 0.44
    s.links.new(transparent.outputs[0], mix.inputs[1])
    s.links.new(emit.outputs[0], mix.inputs[2])
    s.links.new(mix.outputs[0], s.out.inputs['Surface'])
    m['smear'] = s.mat


build_materials()


# ---------------------------------------------------------------------------------------------------------------------
# Geometry
# ---------------------------------------------------------------------------------------------------------------------
PARTS = {}      # name -> object
INFO = {}       # name -> dict(bone, contour, subdivide, occluder)
OCCLUDERS = []


def new_object(name, verts, faces, uvs=None, bone=None, material=None, contour=True, subdivide=0, occluder=False,
               smooth_shade=True):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(v) for v in verts], [], faces)
    mesh.update()
    if uvs is not None:
        layer = mesh.uv_layers.new(name='UVMap')
        for poly in mesh.polygons:
            for loop_index, corner in zip(poly.loop_indices, uvs[poly.index]):
                layer.data[loop_index].uv = corner
    # A closed part faces outward whatever order its rings ran in; an open sheet keeps the front its rows give it.
    bm = bmesh.new()
    bm.from_mesh(mesh)
    if all(len(e.link_faces) == 2 for e in bm.edges):
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    for poly in mesh.polygons:
        poly.use_smooth = smooth_shade
    obj = bpy.data.objects.new(name, mesh)
    collection.objects.link(obj)
    obj.data.materials.append(MATERIALS[material])
    PARTS[name] = obj
    INFO[name] = dict(bone=bone, contour=contour, subdivide=subdivide)
    if occluder:
        OCCLUDERS.append(obj)
    return obj


def loft(name, rings, closed=True, cap_end=False, cap_start=False, **kw):
    """Quads between consecutive rings (each a list of points, same length); u runs around the ring (0..1), v along
    the rings (0..1). Rings counter-clockwise seen from above, bottom first, face outward."""
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
    return new_object(name, verts, faces, uvs, **kw)


def oval(cx, cy, z, rx, ry, count, start=-90.0, end=270.0, closed=True, keel=0.0, wobble=None):
    """Points of an ellipse at height z, counter-clockwise seen from above, from start to end in degrees (-90 is the
    front, -Y; 0 the knight's left, +X). keel pushes the front centre forward (a breastplate's ridge, a helm's prow)."""
    pts = []
    steps = count if closed else count - 1
    for i in range(count):
        a = math.radians(start + (end - start) * i / steps)
        r = 1 + (wobble(a) if wobble else 0)
        front = max(0.0, -math.sin(a))
        pts.append(V(cx + rx * r * math.cos(a), cy + ry * r * math.sin(a) - keel * front ** 4, z))
    return pts


def ellipsoid(name, centre, radii, axes=None, lower=-1.0, upper=1.0, rings=10, segments=16, **kw):
    """An ellipsoid (or a band of one, lower..upper in latitude sine), optionally on its own axes (u, v, w)."""
    ax = axes or (V(1, 0, 0), V(0, 1, 0), V(0, 0, 1))
    c = Vector(centre)
    verts, faces = [], []
    # Rings evenly spaced in latitude; an end at a pole stops one step short of it and fans to the pole.
    lo, hi = math.asin(max(-1.0, lower)), math.asin(min(1.0, upper))
    pole_lo, pole_hi = lower <= -0.999, upper >= 0.999
    step = (hi - lo) / (rings + pole_lo + pole_hi)
    lats = [lo + step * (j + pole_lo) for j in range(rings + 1)]
    for lat in lats:
        for i in range(segments):
            lon = 2 * math.pi * i / segments
            verts.append(c + ax[0] * (radii[0] * math.cos(lat) * math.cos(lon))
                         + ax[1] * (radii[1] * math.cos(lat) * math.sin(lon)) + ax[2] * (radii[2] * math.sin(lat)))
    for j in range(rings):
        for i in range(segments):
            i1 = (i + 1) % segments
            faces.append((j * segments + i, j * segments + i1, (j + 1) * segments + i1, (j + 1) * segments + i))
    for j, at_pole, flip in [(0, lower <= -0.999, True), (rings, upper >= 0.999, False)]:
        ring = verts[j * segments:(j + 1) * segments]
        verts.append(sum(ring, Vector()) / segments if not at_pole else c + ax[2] * (radii[2] * (1 if j else -1)))
        k = len(verts) - 1
        for i in range(segments):
            face = (j * segments + i, j * segments + (i + 1) % segments, k)
            faces.append(tuple(reversed(face)) if flip else face)
    return new_object(name, verts, faces, **kw)


def tube(name, path, radii, count=10, closed=True, cap_start=True, cap_end=True, twist_ref=V(0, -1, 0), **kw):
    """A tube along a polyline with a radius per point (r or (rx, ry): rx along twist_ref); untwisted frames."""
    rings = []
    prev_normal = None
    for k, p in enumerate(path):
        tangent = (path[min(k + 1, len(path) - 1)] - path[max(k - 1, 0)]).normalized()
        if prev_normal is None:
            normal = (twist_ref - tangent * twist_ref.dot(tangent))
            if normal.length < 1e-6:
                normal = V(1, 0, 0) - tangent * tangent.x
            normal.normalize()
        else:
            normal = (prev_normal - tangent * prev_normal.dot(tangent)).normalized()
        prev_normal = normal
        binormal = tangent.cross(normal)
        r = radii[k]
        rx, ry = (r, r) if isinstance(r, (int, float)) else r
        rings.append([p + normal * (rx * math.cos(2 * math.pi * i / count)) + binormal * (ry * math.sin(2 * math.pi * i / count))
                      for i in range(count)])
    return loft(name, rings, closed=closed, cap_start=cap_start, cap_end=cap_end, **kw)


def loop_tube(name, path, radius, count=8, twist_ref=V(0, 0, 1), **kw):
    """A closed ring of tube along a closed path (a trim band, a rim)."""
    rings = []
    m = len(path)
    for k, p in enumerate(path):
        tangent = (path[(k + 1) % m] - path[(k - 1) % m]).normalized()
        normal = (twist_ref - tangent * twist_ref.dot(tangent)).normalized()
        binormal = tangent.cross(normal)
        rx, ry = (radius, radius) if isinstance(radius, (int, float)) else radius
        rings.append([p + normal * (rx * math.cos(2 * math.pi * i / count)) + binormal * (ry * math.sin(2 * math.pi * i / count))
                      for i in range(count)])
    n = count
    verts = [p for ring in rings for p in ring]
    faces = []
    for j in range(m):
        j1 = (j + 1) % m
        for i in range(n):
            i1 = (i + 1) % n
            faces.append((j * n + i, j * n + i1, j1 * n + i1, j1 * n + i))
    return new_object(name, verts, faces, **kw)


def band(name, centre, axis, profile, count=16, **kw):
    """A ring round an axis: profile [(radius, offset along the axis)], bottom first. A trim band round a limb."""
    d = Vector(axis).normalized()
    ref = V(0, -1, 0) if abs(d.y) < 0.9 else V(1, 0, 0)
    u = (ref - d * ref.dot(d)).normalized()
    w = d.cross(u)
    rings = [[Vector(centre) + d * h + (u * math.cos(2 * math.pi * i / count) + w * math.sin(2 * math.pi * i / count)) * r
              for i in range(count)] for r, h in profile]
    return loft(name, rings, closed=True, **kw)


def boss(name, at, normal, radius, height, **kw):
    """A round gold rivet boss: a low dome on a surface."""
    n = Vector(normal).normalized()
    ref = V(0, 0, 1) if abs(n.z) < 0.9 else V(1, 0, 0)
    u = (ref - n * ref.dot(n)).normalized()
    w = n.cross(u)
    return ellipsoid(name, Vector(at), (radius, radius, height), axes=(u, w, n), lower=-0.2, upper=1.0, rings=4,
                     segments=12, **kw)


def mirror_x(v):
    return V(-v.x, v.y, v.z)


def side_point(v, side):
    return Vector(v) if side > 0 else mirror_x(v)


# ---- Measurements (design units: feet on z = 0, facing -Y, the knight's left is +X) ----
# Short legs, a barrel chest and a big helm sunk between the pauldrons: about 1.27 to the crest's top.
ANKLE_Z, KNEE_Z, HIP_Z = 0.075, 0.25, 0.43
HIP_X = 0.088
WAIST_Z = 0.495
SHOULDER = V(0.175, 0.0, 0.79)
ELBOW = V(0.214, 0.026, 0.636)
WRIST = V(0.228, 0.0, 0.5)
FIST = V(0.232, -0.01, 0.455)
FIST_R = mirror_x(FIST)
FIST_L = FIST.copy()
HELM_C = V(0, -0.006, 0.96)
# The breastplate: z, radius across, radius deep, centre y, the keel's push. Widest just under the arms.
CHEST = [(0.49, 0.152, 0.115, 0.0, 0.0), (0.55, 0.16, 0.12, 0.0, 0.006), (0.62, 0.18, 0.132, 0.0, 0.014),
         (0.69, 0.196, 0.14, 0.0, 0.02), (0.75, 0.2, 0.142, 0.002, 0.018), (0.80, 0.184, 0.132, 0.004, 0.01),
         (0.835, 0.145, 0.108, 0.004, 0.0), (0.858, 0.095, 0.08, 0.004, 0.0)]
# The helm: a rounded bucket with a gentle prow, flaring a little at its rim.
HELM = [(0.85, 0.116, 0.12, 0.004, 0.0), (0.878, 0.139, 0.145, 0.0, 0.011), (0.925, 0.149, 0.154, -0.004, 0.019),
        (0.977, 0.15, 0.155, -0.004, 0.019), (1.024, 0.144, 0.149, -0.002, 0.011), (1.066, 0.129, 0.134, 0.002, 0.005),
        (1.099, 0.102, 0.106, 0.006, 0.0), (1.122, 0.066, 0.069, 0.008, 0.0), (1.136, 0.028, 0.03, 0.01, 0.0),
        (1.142, 0.01, 0.011, 0.01, 0.0)]
# The cape: z, radius across, radius deep, centre y, the arc it spans round the back (degrees). Hung from the
# shoulders behind the pauldrons; its sides fall behind the arms.
CAPE = [(0.25, 0.34, 0.2, 0.13, 178), (0.34, 0.32, 0.19, 0.122, 174), (0.45, 0.295, 0.178, 0.112, 170),
        (0.56, 0.268, 0.163, 0.1, 166), (0.67, 0.245, 0.15, 0.088, 166), (0.76, 0.224, 0.134, 0.07, 178),
        (0.82, 0.19, 0.112, 0.05, 198), (0.855, 0.14, 0.09, 0.035, 212)]


def table_ring(table, z):
    """A ring table's row (rx, ry, cy, keel...) at a height, interpolated."""
    for row0, row1 in zip(table, table[1:]):
        if row0[0] <= z <= row1[0]:
            t = (z - row0[0]) / (row1[0] - row0[0])
            return [lerp(a, b, t) for a, b in zip(row0[1:], row1[1:])]
    return list(table[0][1:]) if z < table[0][0] else list(table[-1][1:])


def surface_point(table, z, degrees, lift=0.0):
    """The point at height z, degrees round from the front (+ toward the knight's left), on a ring table's surface,
    lifted off it along its horizontal normal."""
    rx, ry, cy, keel = table_ring(table, z)[:4]
    a = math.radians(-90 + degrees)
    front = max(0.0, -math.sin(a))
    p = V(rx * math.cos(a), cy + ry * math.sin(a) - keel * front ** 4, z)
    n = V(math.cos(a) / rx, math.sin(a) / ry, 0).normalized()
    return p + n * lift, n


def build_legs():
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        x = HIP_X * side
        thigh, shin, foot = 'thigh.' + s, 'shin.' + s, 'foot.' + s
        tube('Thigh mail ' + s, [V(x, 0, HIP_Z + 0.03), V(x, -0.002, KNEE_Z + 0.02)], [0.072, 0.062], 12, bone=thigh,
             material='mail', occluder=True, subdivide=1)
        # Cuisse: a plate over the front of the thigh, gold banded above the knee.
        rings = [oval(x, -0.004, z, r, r * 0.96, 14, start=-90 - 88, end=-90 + 88, closed=False)
                 for z, r in [(0.29, 0.075), (0.33, 0.082), (0.38, 0.084), (0.42, 0.079)]]
        loft('Cuisse ' + s, rings, closed=False, bone=thigh, material='plate', subdivide=1)
        band('Cuisse band ' + s, V(x, -0.004, 0.288), V(0, 0, 1), [(0.079, -0.011), (0.086, -0.006), (0.086, 0.006),
                                                                  (0.079, 0.011)], bone=thigh, material='gold')
        # Poleyn: a round knee cop with a gold boss; greave: knee to ankle with the calf's swell, gold banded.
        ellipsoid('Poleyn ' + s, V(x, -0.032, KNEE_Z + 0.008), (0.071, 0.064, 0.066), bone=shin, material='plate',
                  rings=8, segments=14, subdivide=1)
        boss('Knee boss ' + s, V(x, -0.096, KNEE_Z + 0.01), V(0, -1, 0.15), 0.025, 0.014, bone=shin, material='gold')
        rings = [oval(x, cy, z, rx, ry, 16) for z, rx, ry, cy in
                 [(0.085, 0.054, 0.056, 0.006), (0.13, 0.062, 0.066, 0.008), (0.18, 0.069, 0.073, 0.012),
                  (0.215, 0.068, 0.07, 0.008), (0.245, 0.062, 0.062, 0.0)]]
        loft('Greave ' + s, rings, cap_start=True, cap_end=True, bone=shin, material='plate', occluder=True,
             subdivide=1)
        band('Greave band ' + s, V(x, 0.004, 0.222), V(0, 0, 1), [(0.068, -0.011), (0.074, -0.006), (0.074, 0.006),
                                                                 (0.068, 0.011)], bone=shin, material='gold')
        band('Ankle band ' + s, V(x, 0.006, 0.094), V(0, 0, 1), [(0.056, -0.011), (0.063, -0.006), (0.063, 0.006),
                                                                (0.056, 0.011)], bone=shin, material='gold')
        # Sabaton: a big round boot on a flat sole, a gold rim round the sole and a gold strap over the instep.
        sole = 0.006
        stations = [(0.06, 0.044, 0.052), (0.036, 0.055, 0.074), (-0.005, 0.062, 0.082), (-0.056, 0.066, 0.068),
                    (-0.098, 0.064, 0.055), (-0.132, 0.055, 0.041), (-0.153, 0.037, 0.027)]
        rings = []
        for y, w, h in stations:
            arc = [V(x + w * math.cos(math.radians(t)), y, sole + h * math.sin(math.radians(t)))
                   for t in (0, 30, 60, 90, 120, 150, 180)]
            rings.append(arc + [V(x - w * 0.55, y, sole), V(x, y, sole), V(x + w * 0.55, y, sole)])
        loft('Sabaton ' + s, rings, cap_start=True, cap_end=True, bone=foot, material='plate', occluder=True,
             subdivide=1)
        rim = [V(x + (w + 0.004) * sx, y, sole + 0.012) for sx in (1,) for y, w, h in stations]
        rim += [V(x, stations[-1][0] - 0.006, sole + 0.012)]
        rim += [V(x - (w + 0.004), y, sole + 0.012) for y, w, h in reversed(stations)]
        rim += [V(x, stations[0][0] + 0.006, sole + 0.012)]
        loop_tube('Sole rim ' + s, rim, (0.011, 0.007), 6, twist_ref=V(0, 0, 1), bone=foot, material='gold')
        strap = [V(x + 0.065 * math.cos(math.radians(t)), -0.03, sole + 0.004 + 0.074 * math.sin(math.radians(t)))
                 for t in range(0, 181, 20)]
        tube('Instep strap ' + s, strap, [(0.011, 0.006)] * len(strap), 6, twist_ref=V(0, 1, 0), bone=foot,
             material='gold')
        boss('Instep boss ' + s, V(x + 0.067 * side, -0.03, 0.045), V(side, -0.25, 0.2), 0.019, 0.011, bone=foot,
             material='gold')


def build_torso():
    rings = [oval(0, cy, z, rx, ry, 28, keel=keel) for z, rx, ry, cy, keel in CHEST]
    loft('Breastplate', rings, cap_end=True, bone='*torso', material='plate', occluder=True, subdivide=2)
    # The silver collar round the neck and the silver chevron across the chest, the painted Vanguard's two bright lines.
    collar = [surface_point(CHEST, 0.83, d, 0.004)[0] for d in range(0, 360, 12)]
    loop_tube('Collar', collar, (0.011, 0.009), 8, bone='chest', material='silver', subdivide=1)
    chevron = []
    for d in range(-60, 61, 6):
        z = 0.712 + 0.04 * (abs(d) / 60) ** 1.15
        chevron.append(surface_point(CHEST, z, d, 0.005)[0])
    tube('Chevron', chevron, [(0.011, 0.007)] * len(chevron), 8, twist_ref=V(0, 0, 1), bone='chest',
         material='silver', subdivide=1)
    # The pauldron straps' gold bosses on the upper chest.
    for side in (-1, 1):
        p, n = surface_point(CHEST, 0.785, 58 * side, 0.004)
        boss('Strap boss ' + ('L' if side > 0 else 'R'), p, n + V(0, 0, 0.3), 0.028, 0.016, bone='chest',
             material='gold')
    # Chain mail at the neck, under the helm's rim.
    loft('Gorget mail', [oval(0, 0.004, z, r, r * 0.92, 18) for z, r in ((0.84, 0.1), (0.9, 0.086))],
         bone='neck', material='mail', occluder=True)
    # The belt: brown leather with three gold bosses at the front.
    path = oval(0, 0.0, WAIST_Z, 0.163, 0.124, 32)
    loop_tube('Belt', path, (0.022, 0.012), 8, twist_ref=V(0, 0, 1), bone='pelvis', material='leather',
              occluder=True)
    for d in (0, -46, 46):
        p, n = surface_point([(0.4, 0.163, 0.124, 0.0, 0.0), (0.6, 0.163, 0.124, 0.0, 0.0)], WAIST_Z, d, 0.012)
        boss('Belt boss %d' % d, p, n, 0.03 if d == 0 else 0.025, 0.016, bone='pelvis', material='gold')


def build_skirt():
    # Mail skirt round the hips; over it at the front a blue tabard panel edged in silver, and a plate tasset over
    # each hip edged in silver.
    rings = [oval(0, 0.012, z, rx, rx * 0.7, 24) for z, rx in ((0.33, 0.19), (0.38, 0.181), (0.44, 0.166), (0.5, 0.152))]
    loft('Mail skirt', rings, bone='*skirt', material='mail', occluder=True, subdivide=1)
    rings = [oval(0, 0.0, z, rx, ry, 10, start=-90 - 27, end=-90 + 27, closed=False)
             for z, rx, ry in ((0.262, 0.206, 0.17), (0.31, 0.203, 0.166), (0.37, 0.195, 0.157), (0.43, 0.181, 0.144),
                               (0.49, 0.165, 0.13))]
    loft('Tabard', rings, closed=False, bone='*tabard', material='tabard', subdivide=1)
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        a0, a1 = (-90 + 28, -90 + 104) if side > 0 else (-90 - 104, -90 - 28)
        rings = [oval(0, 0.0, z, rx, ry, 12, start=a0, end=a1, closed=False)
                 for z, rx, ry in ((0.345, 0.215, 0.166), (0.39, 0.207, 0.16), (0.44, 0.192, 0.146), (0.488, 0.174, 0.132))]
        loft('Tasset ' + s, rings, closed=False, bone='tasset.' + s, material='tasset', subdivide=1)


def build_arms():
    for side in (-1, 1):
        s = 'L' if side > 0 else 'R'
        sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
        upper, fore, hand = 'upper_arm.' + s, 'forearm.' + s, 'hand.' + s
        tube('Rerebrace ' + s, [sh + V(0, 0, -0.02), sh.lerp(el, 0.5), el + V(0, 0, 0.02)], [0.06, 0.059, 0.055], 12,
             bone=upper, material='plate', subdivide=1)
        ellipsoid('Elbow mail ' + s, el, (0.055, 0.055, 0.055), bone=fore, material='mail', rings=6, segments=10)
        ellipsoid('Couter ' + s, el + V(0.014 * side, 0.026, 0.004), (0.056, 0.046, 0.058), bone=fore,
                  material='plate', rings=6, segments=12, subdivide=1)
        d = (wr - el).normalized()
        tube('Vambrace ' + s, [el + d * 0.02, el.lerp(wr, 0.5), wr + d * 0.012], [0.056, 0.06, 0.067], 12,
             bone=fore, material='plate', subdivide=1)
        band('Vambrace band ' + s, el.lerp(wr, 0.34), d, [(0.059, -0.01), (0.065, -0.005), (0.065, 0.005),
                                                          (0.059, 0.01)], bone=fore, material='gold')
        band('Cuff band ' + s, wr + d * 0.006, d, [(0.066, -0.012), (0.073, -0.006), (0.073, 0.006), (0.066, 0.012)],
             bone=fore, material='gold')
        # The gauntlet: a big round fist and a thumb over it.
        ellipsoid('Gauntlet ' + s, fi, (0.055, 0.06, 0.06), bone=hand, material='plate', rings=8, segments=14,
                  subdivide=1)
        ellipsoid('Thumb ' + s, fi + V(-0.03 * side, -0.035, 0.01), (0.02, 0.022, 0.026), bone=hand,
                  material='plate', rings=5, segments=8)
        # Pauldron: a big dome, a lame under it, each edged in silver; it rides its own bone, which lifts with the
        # arm a little (a follower).
        tilt = V(0.36 * side, 0.0, 1.0).normalized()
        fwd = V(0, -1, 0)
        ux = fwd.cross(tilt).normalized() * -side
        if ux.x * side < 0:
            ux = -ux
        c = sh + V(0.03 * side, 0.0, 0.02)
        ellipsoid('Pauldron ' + s, c, (0.112, 0.116, 0.09), axes=(ux, fwd, tilt), lower=-0.32, upper=1.0, rings=8,
                  segments=20, bone='pauldron.' + s, material='plate', occluder=True, subdivide=1)
        lip = math.cos(math.asin(-0.32))
        rim = [c + ux * (0.112 * lip * math.cos(a)) + fwd * (0.116 * lip * math.sin(a)) - tilt * (0.09 * 0.32)
               for a in [2 * math.pi * i / 28 for i in range(28)]]
        loop_tube('Pauldron rim ' + s, rim, 0.011, 8, twist_ref=tilt, bone='pauldron.' + s, material='silver')
        c2 = c - tilt * 0.058 + ux * 0.016
        ellipsoid('Pauldron lame ' + s, c2, (0.1, 0.106, 0.064), axes=(ux, fwd, tilt), lower=-0.5, upper=0.55,
                  rings=4, segments=20, bone='pauldron.' + s, material='plate', subdivide=1)
        lip2 = math.cos(math.asin(-0.5))
        rim2 = [c2 + ux * (0.1 * lip2 * math.cos(a)) + fwd * (0.106 * lip2 * math.sin(a)) - tilt * (0.064 * 0.5)
                for a in [2 * math.pi * i / 28 for i in range(28)]]
        loop_tube('Pauldron lame rim ' + s, rim2, 0.01, 8, twist_ref=tilt, bone='pauldron.' + s, material='silver')


def helm_point(phi_degrees):
    """The point on the helm's midline at phi degrees round from the front (0 level with the visor's centre, 90 the
    top, 180 the back), from its centre, found by bisection on the ring table."""
    phi = math.radians(phi_degrees)
    d = V(0, -math.cos(phi), math.sin(phi))

    def inside(p):
        if p.z < HELM[0][0] or p.z > HELM[-1][0]:
            return False
        rx, ry, cy, keel = table_ring(HELM, p.z)
        reach = ry + keel if p.y < cy else ry
        return abs(p.y - cy) <= reach

    lo, hi = 0.0, 0.4
    for _ in range(30):
        mid = (lo + hi) / 2
        if inside(HELM_C + d * mid):
            lo = mid
        else:
            hi = mid
    return HELM_C + d * lo, d


def build_helm():
    rings = [oval(0, cy, z, rx, ry, 32, keel=keel) for z, rx, ry, cy, keel in HELM]
    loft('Helm', rings, cap_start=True, cap_end=True, bone='head', material='plate', occluder=True, subdivide=2)
    # Silver: the rim of the helm, and the painted visor's frame - a brow bar over the slit, a side bar down each
    # cheek to the rim and a bar down the middle under the slit (a T).
    rim = [surface_point(HELM, 0.865, d, 0.004)[0] for d in range(0, 360, 12)]
    loop_tube('Helm rim', rim, (0.012, 0.009), 8, bone='head', material='silver', subdivide=1)
    bar = [surface_point(HELM, 0.99, d, 0.004)[0] for d in range(-64, 65, 4)]
    tube('Visor brow', bar, [(0.012, 0.007)] * len(bar), 8, twist_ref=V(0, 0, 1), bone='head', material='silver',
         subdivide=1)
    for name, d, z0 in (('Visor bar', 0, 0.94), ('Visor side L', 64, 0.99), ('Visor side R', -64, 0.99)):
        bar = [surface_point(HELM, lerp(z0, 0.872, t / 6), d, 0.004)[0] for t in range(7)]
        across = V(0, 0, 1).cross(surface_point(HELM, 0.95, d)[1])
        tube(name, bar, [(0.011, 0.007)] * len(bar), 8, twist_ref=across, bone='head', material='silver',
             subdivide=1)
    # The slit, a dark recess under the brow, and in it the blue light standing proud of it.
    slit = [surface_point(HELM, 0.961, d, 0.0)[0] for d in range(-52, 53, 4)]
    tube('Visor slit', slit, [(0.017, 0.005)] * len(slit), 6, twist_ref=V(0, 0, 1), bone='head', material='slit',
         contour=False)
    eye = [surface_point(HELM, 0.961, d, 0.008)[0] for d in range(-42, 43, 3)]
    tube('Visor light', eye, [(0.0105, 0.004)] * len(eye), 6, twist_ref=V(0, 0, 1), bone='head', material='eye',
         contour=False)
    # The gold crest: a raised fin from the visor's brow over the top to the nape.
    path, radii = [], []
    phis = list(range(12, 201, 6))
    for k, phi in enumerate(phis):
        p, d = helm_point(phi)
        path.append(p + d * 0.016)
        t = k / (len(phis) - 1)
        taper = smooth(0.0, 0.08, t) * 0.4 + 0.6 if t < 0.5 else 1 - 0.45 * smooth(0.75, 1.0, t)
        radii.append((0.019 * taper, 0.026 * taper))
    tube('Crest', path, radii, 8, twist_ref=V(1, 0, 0), bone='head', material='gold', subdivide=1)
    # Gold bosses where the visor pivots, at either side.
    for side in (-1, 1):
        p, n = surface_point(HELM, 0.961, 72 * side, 0.004)
        boss('Visor boss ' + ('L' if side > 0 else 'R'), p, n, 0.03, 0.016, bone='head', material='gold')


def build_cape():
    rings = []
    count = 40
    for k, (z, rx, ry, cy, span) in enumerate(CAPE):
        t = 1 - k / (len(CAPE) - 1)            # 1 at the hem, 0 at the top
        # Vertical folds, deepening toward the hem, so the key picks them out in painted stripes (Ilya's robe).
        wobble = (lambda a, t=t: 0.11 * t ** 0.9 * (0.55 * math.cos(7 * a + 0.4) + 0.3 * math.cos(13 * a + 1.9)
                                                   + 0.15 * math.cos(3 * a + 1.0)))
        ring = oval(0, cy, z, rx, ry, count, start=90 - span / 2, end=90 + span / 2, closed=False, wobble=wobble)
        if k == 0:
            for i, p in enumerate(ring):
                a = math.radians(90 - span / 2 + span * i / (count - 1))
                p.z += 0.012 * math.sin(7 * a + 0.5) + 0.02 * (1 - math.sin(a))   # a ragged hem, shorter at the sides
        rings.append(ring)
    loft('Cape', rings, closed=False, bone='*cape', material='cape', occluder=True, subdivide=2)


SWORD_GRIP = FIST_R.copy()     # the right fist's centre: the grip runs through it along -Y, the blade forward
BLADE_BASE = SWORD_GRIP + V(0, -0.062, 0)
BLADE_TIP = SWORD_GRIP + V(0, -0.6, 0)
BLADE_MID = BLADE_BASE.lerp(BLADE_TIP, 0.5)
SHIELD_C = FIST_L + V(0.074, 0, 0)    # the buckler's centre, its face +X at rest, the left fist on its handle
SHIELD_R = 0.165


def build_sword():
    g = SWORD_GRIP
    tube('Sword grip', [g + V(0, 0.062, 0), g + V(0, -0.045, 0)], [0.015, 0.015], 8, bone='sword', material='grip')
    ellipsoid('Pommel', g + V(0, 0.076, 0), (0.022, 0.022, 0.022), bone='sword', material='gold', rings=5, segments=10)
    guard = [g + V(0, -0.052 + 0.014 * (abs(z) / 0.09) ** 2, z) for z in (-0.09, -0.06, -0.03, 0.0, 0.03, 0.06, 0.09)]
    tube('Crossguard', guard, [(0.012, 0.012), (0.014, 0.014), (0.016, 0.015), (0.018, 0.016), (0.016, 0.015),
                               (0.014, 0.014), (0.012, 0.012)], 8, twist_ref=V(0, -1, 0), bone='sword', material='gold')
    # The blade: a flattened diamond in section, flat-shaded so each bevel takes its own value.
    rings = []
    for y, w, t in [(0.0, 0.06, 0.016), (0.08, 0.058, 0.016), (0.3, 0.054, 0.015), (0.46, 0.048, 0.013),
                    (0.515, 0.03, 0.009), (0.538, 0.004, 0.002)]:
        c = BLADE_BASE + V(0, -y, 0)
        rings.append([c + V(0, 0, -w / 2), c + V(t / 2, 0, 0), c + V(0, 0, w / 2), c + V(-t / 2, 0, 0)])
    loft('Blade', rings, cap_start=True, cap_end=True, bone='sword', material='blade', smooth_shade=False)


def build_shield():
    """A round buckler: a shallow blue dome, a gold rim, a gold ring and a steel boss, six gold rivets."""
    c, R = SHIELD_C, SHIELD_R
    nx, uy, uz = V(1, 0, 0), V(0, 1, 0), V(0, 0, 1)
    rings = []
    for k in range(7):
        r = R * (k + 1) / 7
        bulge = 0.032 * (1 - (r / R) ** 2)
        rings.append([c + nx * bulge + (uy * math.cos(2 * math.pi * i / 32) + uz * math.sin(2 * math.pi * i / 32)) * r
                      for i in range(32)])
    back = [c - nx * 0.014 + (uy * math.cos(2 * math.pi * i / 32) + uz * math.sin(2 * math.pi * i / 32)) * R
            for i in range(32)]
    rings.append(back)
    verts = [c + nx * 0.032]
    faces = []
    for i in range(32):
        faces.append((0, 1 + i, 1 + (i + 1) % 32))
    for j in range(len(rings) - 1):
        for i in range(32):
            i1 = (i + 1) % 32
            a, b = 1 + j * 32, 1 + (j + 1) * 32
            faces.append((a + i, b + i, b + i1, a + i1))
    verts += [p for ring in rings for p in ring]
    verts.append(c - nx * 0.014)
    last = len(verts) - 1
    base = 1 + (len(rings) - 1) * 32
    for i in range(32):
        faces.append((base + (i + 1) % 32, base + i, last))
    new_object('Shield', verts, faces, bone='shield', material='plate', subdivide=1)
    rim = [c + (uy * math.cos(2 * math.pi * i / 36) + uz * math.sin(2 * math.pi * i / 36)) * (R + 0.002)
           for i in range(36)]
    loop_tube('Shield rim', rim, (0.017, 0.015), 8, twist_ref=nx, bone='shield', material='gold')
    ring = [c + nx * 0.024 + (uy * math.cos(2 * math.pi * i / 24) + uz * math.sin(2 * math.pi * i / 24)) * 0.072
            for i in range(24)]
    loop_tube('Shield ring', ring, 0.009, 6, twist_ref=nx, bone='shield', material='gold')
    ellipsoid('Shield boss', c + nx * 0.03, (0.046, 0.046, 0.03), axes=(uy, uz, nx), lower=-0.1, upper=1.0,
              rings=5, segments=16, bone='shield', material='silver', subdivide=1)
    for k in range(6):
        a = 2 * math.pi * k / 6 + math.pi / 6
        r = 0.122
        at = c + nx * (0.032 * (1 - (r / R) ** 2) + 0.002) + (uy * math.cos(a) + uz * math.sin(a)) * r
        boss('Shield rivet %d' % k, at, nx, 0.013, 0.008, bone='shield', material='gold', contour=False)


def build_smear():
    """The sword's arc: a strip whose points apply() moves to where the blade swept since the last frame."""
    n = SMEAR_SAMPLES + 1
    verts = [V(0, 0, 0)] * (2 * n)
    faces = [(2 * i, 2 * i + 1, 2 * i + 3, 2 * i + 2) for i in range(n - 1)]
    mesh = bpy.data.meshes.new('Swing smear')
    mesh.from_pydata([tuple(v) for v in verts], [], faces)
    mesh.materials.append(MATERIALS['smear'])
    obj = bpy.data.objects.new('Swing smear', mesh)
    collection.objects.link(obj)
    return obj


SMEAR_SAMPLES = 10

build_legs()
build_torso()
build_skirt()
build_arms()
build_helm()
build_cape()
build_sword()
build_shield()
SMEAR = build_smear()


# ---------------------------------------------------------------------------------------------------------------------
# Rig
# ---------------------------------------------------------------------------------------------------------------------
arm_data = bpy.data.armatures.new('Vanguard rig')
RIG = bpy.data.objects.new('Vanguard', arm_data)
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


def cape_anchor(z, degrees):
    rx, ry, cy = table_ring(CAPE, z)[:3]
    a = math.radians(degrees)
    return V(rx * math.cos(a), cy + ry * math.sin(a), z)


CAPE_COLUMNS = {'L': 28, 'C': 90, 'R': 152}
bone('root', (0, 0, 0), (0, -0.1, 0))
bone('pelvis', (0, 0, HIP_Z), (0, 0, 0.55), 'root')
bone('spine', (0, 0, 0.55), (0, 0, 0.67), 'pelvis')
bone('chest', (0, 0, 0.67), (0, 0, 0.84), 'spine')
bone('neck', (0, 0, 0.84), (0, 0, 0.9), 'chest')
bone('head', (0, 0, 0.9), (0, 0, 1.2), 'neck')
for side, s in ((1, 'L'), (-1, 'R')):
    sh, el, wr, fi = (side_point(v, side) for v in (SHOULDER, ELBOW, WRIST, FIST))
    bone('pauldron.' + s, sh, sh + V(0.1 * side, 0, 0.04), 'chest')
    bone('upper_arm.' + s, sh, el, 'chest')
    bone('forearm.' + s, el, wr, 'upper_arm.' + s)
    bone('hand.' + s, wr, fi + (fi - wr).normalized() * 0.04, 'forearm.' + s)
    bone('thigh.' + s, (HIP_X * side, 0, HIP_Z), (HIP_X * side, 0, KNEE_Z), 'pelvis')
    bone('shin.' + s, (HIP_X * side, 0, KNEE_Z), (HIP_X * side, 0.004, ANKLE_Z), 'thigh.' + s)
    bone('foot.' + s, (HIP_X * side, 0.004, ANKLE_Z), (HIP_X * side, -0.1, 0.02), 'shin.' + s)
    bone('tasset.' + s, (0.12 * side, -0.07, 0.49), (0.16 * side, -0.1, 0.35), 'pelvis')
bone('tabard1', (0, -0.13, 0.48), (0, -0.16, 0.37), 'pelvis')
bone('tabard2', (0, -0.16, 0.37), (0, -0.2, 0.27), 'tabard1')
for col, degrees in CAPE_COLUMNS.items():
    top, mid, hem = cape_anchor(0.8, degrees), cape_anchor(0.52, degrees), cape_anchor(0.25, degrees)
    bone('cape%s1' % col, top, mid, 'chest')
    bone('cape%s2' % col, mid, hem, 'cape%s1' % col)
bone('sword', SWORD_GRIP, SWORD_GRIP + V(0, -0.1, 0), 'hand.R')
bone('shield', FIST_L, FIST_L + V(0.1, 0, 0), 'hand.L')
bpy.ops.object.mode_set(mode='OBJECT')
REST = {b.name: b.matrix_local.copy() for b in arm_data.bones}
PARENT = {b.name: (b.parent.name if b.parent else None) for b in arm_data.bones}
# Plate that rides one bone but turns part of the way with another: the pauldron lifts a little with the arm, the
# tasset swings with its thigh.
FOLLOWERS = {'pauldron.L': ('upper_arm.L', 0.3), 'pauldron.R': ('upper_arm.R', 0.3),
             'tasset.L': ('thigh.L', 0.55), 'tasset.R': ('thigh.R', 0.55)}


def depth(name):
    d = 0
    while PARENT[name]:
        name = PARENT[name]
        d += 1
    return d


ORDER = sorted(REST, key=lambda b: (depth(b) + (1 if b in FOLLOWERS else 0), b))
for pb in RIG.pose.bones:
    pb.rotation_mode = 'QUATERNION'


# ---- Weights: the plate rides its bone rigidly; cloth and mail are weighted from the geometry ----
def weights_for(name, co):
    x, y, z = co
    key = INFO[name]['bone']
    if not key.startswith('*'):
        return {key: 1.0}
    if key == '*torso':
        pelvis = smooth(0.6, 0.53, z)
        chest = smooth(0.64, 0.74, z)
        return {'pelvis': pelvis, 'spine': (1 - pelvis) * (1 - chest), 'chest': chest * (1 - smooth(0.85, 0.88, z)),
                'neck': smooth(0.85, 0.88, z)}
    if key == '*skirt':
        top = smooth(0.42, 0.48, z)
        left = smooth(-0.07, 0.07, x)
        legs = (1 - top) * 0.45
        return {'pelvis': 1 - legs, 'thigh.L': legs * left, 'thigh.R': legs * (1 - left)}
    if key == '*tabard':
        top = smooth(0.43, 0.48, z)
        upper = smooth(0.3, 0.42, z)
        return {'pelvis': top, 'tabard1': (1 - top) * upper, 'tabard2': (1 - top) * (1 - upper)}
    if key == '*cape':
        cy = table_ring(CAPE, z)[2]
        ang = math.degrees(math.atan2(y - cy, x))
        if ang < -90:
            ang += 360
        shares = {col: math.exp(-((ang - c) / 42) ** 2) for col, c in CAPE_COLUMNS.items()}
        total = sum(shares.values()) + 1e-6
        t = (0.84 - z) / (0.84 - 0.25)
        chest = 1 - smooth(0.0, 0.2, t)
        low = smooth(0.38, 0.78, t)
        w = {'chest': chest}
        for col, share in shares.items():
            w['cape%s1' % col] = (1 - chest) * (1 - low) * share / total
            w['cape%s2' % col] = (1 - chest) * low * share / total
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
# The smear's points are written in the rig's space each frame: it hangs from the rig, no deformation.
SMEAR.parent = RIG


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
    info = INFO[obj.name]
    if info['subdivide']:
        sub = obj.modifiers.new('Smooth', 'SUBSURF')
        sub.levels = sub.render_levels = info['subdivide']
        sub.quality = 3
    clamp = obj.modifiers.new('Floor', 'NODES')
    clamp.node_group = FLOOR
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


# ---------------------------------------------------------------------------------------------------------------------
# Posing: BuildBlenderArcanist.py's solver - deltas are rotations about each bone's head in the rig's rest space,
# accumulated down the hierarchy - with the Warden's followers.
# ---------------------------------------------------------------------------------------------------------------------
def rot(x=0.0, y=0.0, z=0.0):
    """Degrees about the rig's axes: +x pitches a raised bone forward (toward -Y) and swings a hanging one back, +y
    leans a raised bone to the knight's left, +z turns it to the left."""
    return (Matrix.Rotation(math.radians(z), 3, 'Z') @ Matrix.Rotation(math.radians(y), 3, 'Y')
            @ Matrix.Rotation(math.radians(x), 3, 'X'))


class Pose:
    def __init__(self):
        self.rot = {}
        self.offset = Vector()
        self.feet = {}            # 'L'/'R' -> (ankle target, foot pitch degrees, foot yaw degrees)
        self.hands = {}           # 'L'/'R' -> (fist target, 3x3 world rotation of the hand, elbow pole)
        self.glow = {'eye': 1.0, 'blade': 0.0}
        self.smear = None         # [(base, tip)] in the rig's space, oldest first, or None
        self.smear_colour = '#E6F0FF'

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
        p = V(0, -1, 0) - dn * dn.y
    p.normalize()
    middle = a + dn * (l1 * cos_a) + p * (l1 * sin_a)
    end = a + dn * dist
    return middle, end, dn.cross(p).normalized()


class Solver:
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
                follow = base.slerp(self.W[leader].to_quaternion(), share).to_matrix()
                pending[b] = follow @ self.pose.rot.get(b, Matrix.Identity(3))
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
        pole = yawm @ V(0, -1, 0.15)
        knee, ankle, hinge = two_bone(hip, Vector(target), l1, l2, pole)
        rest_pole = V(0, -1, 0.15)
        _, _, rest_hinge = two_bone(REST['thigh.' + s].to_translation(), ankle0, l1, l2, rest_pole)
        W_thigh = frame_from(knee - hip, hinge) @ frame_from(knee0 - REST['thigh.' + s].to_translation(), rest_hinge).transposed()
        W_shin = frame_from(ankle - knee, hinge) @ frame_from(ankle0 - knee0, rest_hinge).transposed()
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
        # The target is the fist's centre (on the sword's grip, behind the buckler's boss); the wrist follows from the
        # hand's own rotation.
        fist0 = FIST_L if s == 'L' else FIST_R
        wrist_target = Vector(target) - hand_rot @ (fist0 - wr0)
        elbow, wrist, hinge = two_bone(shoulder, wrist_target, l1, l2, Vector(pole))
        rest_pole = V(0.3 if s == 'L' else -0.3, 1, -0.1)
        _, _, rest_hinge = two_bone(sh0, wr0, l1, l2, rest_pole)
        W_upper = frame_from(elbow - shoulder, hinge) @ frame_from(el0 - sh0, rest_hinge).transposed()
        W_fore = frame_from(wrist - elbow, hinge) @ frame_from(wr0 - el0, rest_hinge).transposed()
        return {'upper_arm.' + s: W_upper, 'forearm.' + s: W_fore, 'hand.' + s: hand_rot}


def held(direction, toward, roll=0.0):
    """The right hand's rotation holding the sword's blade along direction, its edges in the plane that holds the
    forearm (toward: from the fist toward the elbow, roughly), rolled about the blade by roll degrees."""
    d = Vector(direction).normalized()
    w = Vector(toward) - d * Vector(toward).dot(d)
    if w.length < 1e-5:
        w = V(0, 0, 1) - d * d.z
    w.normalize()
    if roll:
        w = Matrix.Rotation(math.radians(roll), 3, d) @ w
    F1 = Matrix((d.cross(w), d, w)).transposed()
    F0 = Matrix((V(-1, 0, 0), V(0, -1, 0), V(0, 0, 1))).transposed()
    return F1 @ F0.transposed()


def braced(normal, toward):
    """The left hand's rotation holding the buckler's face along normal, the fist's back toward the elbow."""
    n = Vector(normal).normalized()
    u = Vector(toward) - n * Vector(toward).dot(n)
    if u.length < 1e-5:
        u = V(0, 0, 1) - n * n.z
    u.normalize()
    F1 = Matrix((n, u, n.cross(u))).transposed()
    F0 = Matrix((V(1, 0, 0), V(0, 0, 1), V(0, -1, 0))).transposed()
    return F1 @ F0.transposed()


def turn(a, b, t):
    return a.to_quaternion().slerp(b.to_quaternion(), t).to_matrix()


def blade_direction(az=None, el=None, pitch=None, yaw=0.0):
    """A blade direction: by azimuth and elevation (az 0 forward, + to the knight's left), or in the vertical plane by
    pitch (0 forward, 90 straight up, past 90 back over the head) turned by yaw."""
    if pitch is not None:
        p = math.radians(pitch)
        return rot(z=yaw) @ V(0, -math.cos(p), math.sin(p))
    a, e = math.radians(az), math.radians(el)
    return V(math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))


def rig_matrix():
    return Matrix.LocRotScale(RIG.location, RIG.rotation_euler, RIG.scale)


def forge_world(solver):
    return rig_matrix() @ (solver.G['sword'] @ BLADE_MID)


def blade_ends(pose):
    solver = Solver(pose)
    solver.solve()
    return solver.G['sword'] @ BLADE_BASE, solver.G['sword'] @ BLADE_TIP


def apply(pose):
    solver = Solver(pose)
    basis = solver.solve()
    for b, m in basis.items():
        RIG.pose.bones[b].matrix_basis = m
    MATERIALS['eye'].node_tree.nodes['Ember strength'].outputs[0].default_value = pose.glow['eye']
    MATERIALS['blade'].node_tree.nodes['Forge glow'].outputs[0].default_value = min(1.0, pose.glow['blade'])
    at = forge_world(solver)
    light = TOON.nodes['Forge position']
    for i in range(3):
        light.inputs[i].default_value = at[i]
    TOON.nodes['Forge strength'].outputs[0].default_value = pose.glow['blade']
    # The smear: a strip from where the blade was to where it is, narrowing toward its tail.
    mesh = SMEAR.data
    if pose.smear:
        n = len(pose.smear)
        for i in range(SMEAR_SAMPLES + 1):
            base, tip = pose.smear[min(i, n - 1)]     # a shorter sweep repeats its last sample: empty quads
            t = min(i, n - 1) / (n - 1)
            inner = base.lerp(tip, 0.12 + 0.68 * (1 - t) ** 1.3)
            mesh.vertices[2 * i].co = inner
            mesh.vertices[2 * i + 1].co = tip + (tip - base).normalized() * 0.012
        SMEAR.hide_render = False
        MATERIALS['smear'].node_tree.nodes['Smear colour'].outputs[0].default_value = lin(pose.smear_colour)
    else:
        for v in mesh.vertices:
            v.co = V(0, 0, 0)
        SMEAR.hide_render = True
    mesh.update()
    return basis


def forge_screen(pose):
    """The blade's middle on screen, in world units from the floor origin: where a forge glow may be laid."""
    solver = Solver(pose)
    solver.solve()
    world = forge_world(solver)
    return world.dot(RIGHT), world.dot(UP)


# ---------------------------------------------------------------------------------------------------------------------
# The clips. Every function returns a Pose for a frame; the base stance is shared.
# ---------------------------------------------------------------------------------------------------------------------
SHOULDER_R = mirror_x(SHOULDER)
# The idle sword points out to the right and a little back, down 30 degrees: on screen the painting's diagonal in the
# front three-quarter facings, and never hanging plumb like a cane in the side ones.
SWORD_REST = V(-0.248, -0.085, 0.49)     # the right fist in the stance
SWORD_DIR_REST = blade_direction(az=-112, el=-30)
SHIELD_REST = V(0.236, -0.105, 0.5)      # the left fist: the buckler at the hip, turned forward and out
SHIELD_N_REST = V(0.22, -0.97, 0.02)


def guard_hands(p, breath=0.0):
    p.hands['R'] = (SWORD_REST + V(0, 0, 0.004 * breath), held(SWORD_DIR_REST, SHOULDER_R - SWORD_REST, roll=-35),
                    (-0.6, 0.8, -0.2))
    p.hands['L'] = (SHIELD_REST + V(0, 0, 0.005 * breath), braced(SHIELD_N_REST, V(0.05, 0.3, 1)), (0.7, 0.7, -0.3))


def cape(p, back=0.0, flutter=0.0, spread=0.0, sway=0.0, torso=0.0):
    """The cape's columns: swung back (+), fluttering (the hem more), spread at the sides, swayed to the left (+).
    torso is how far the chest pitches forward in the pose: the cape hangs from the chest, so a bowed chest would
    lever it up behind like a board; back is measured from the vertical instead."""
    back -= torso
    p.add('capeC1', rot(x=back, y=-sway))
    p.add('capeC2', rot(x=back * 0.5 + 5 * flutter, y=-sway * 0.5))
    for col, sign in (('L', 1), ('R', -1)):
        p.add('cape%s1' % col, rot(x=back * 0.85 + 2 * flutter * sign, y=-sign * spread - sway))
        p.add('cape%s2' % col, rot(x=back * 0.45 + 4 * flutter, y=-sign * spread * 0.5 - sway * 0.5))


def stance(breath=0.0, sway=0.0):
    p = Pose()
    # Square and planted: the chest up, the helm level, the feet wide.
    p.add('pelvis', rot(x=-2))
    p.add('spine', rot(x=3 + 0.5 * breath))
    p.add('chest', rot(x=3 - 1.3 * breath, z=-3))
    p.add('neck', rot(x=2))
    p.add('head', rot(x=-5 + 1.0 * breath, z=4))
    p.offset = V(0.003 * sway, 0, 0.003 * breath - 0.012)
    p.feet['L'] = (V(0.105, 0.018, ANKLE_Z), 0, 12)
    p.feet['R'] = (V(-0.102, -0.028, ANKLE_Z), 0, -14)
    guard_hands(p, breath)
    cape(p, back=3 + 0.8 * breath, flutter=0.4 * sway, sway=0.8 * sway)
    return p


def flicker(k, rate=1.0):
    return 0.86 + 0.09 * math.sin(k * 2.39 * rate + 0.4) + 0.06 * math.sin(k * 5.11 * rate + 1.3)


def idle(k, n):
    b = math.sin(2 * math.pi * k / n)
    sway = math.sin(2 * math.pi * k / n + 1.1)
    p = stance(b, sway)
    p.glow['eye'] = flicker(k)
    return p


def walk_legs(p, phase, lean=8.0):
    """The run's lower body and its weight: distance-driven, two steps a cycle, the right foot's contact at 0."""
    bob = -0.03 * (0.5 + 0.5 * math.cos(4 * math.pi * (phase - 0.07)))
    p.offset = V(0.008 * math.sin(2 * math.pi * phase), 0, bob - 0.02)
    excursion = STRIDE * DUTY / 2
    for s, offset, x in (('R', 0.0, -0.092), ('L', 0.5, 0.092)):
        q = (phase + offset) % 1.0
        if q < DUTY:       # planted: slides back at the floor's speed
            t = q / DUTY
            y = lerp(-excursion, excursion, t)
            z = ANKLE_Z
            pitch = lerp(-8, 12, smooth(0.6, 1.0, t))
        else:              # swinging through, lifted high: a trot
            t = (q - DUTY) / (1 - DUTY)
            y = lerp(excursion, -excursion, smooth(0.0, 1.0, t))
            z = ANKLE_Z + 0.085 * math.sin(math.pi * t) ** 1.1
            pitch = lerp(32, -14, t)
        p.feet[s] = (V(x, y, z), pitch, 0)
    swing = math.cos(2 * math.pi * phase)      # +1 at the right foot's contact (the right leg forward)
    p.add('pelvis', rot(z=-8 * swing))
    p.add('spine', rot(x=lean * 0.45, z=4 * swing))
    p.add('chest', rot(x=lean * 0.4, z=5 * swing))
    p.add('head', rot(x=-lean * 0.6, z=2 * swing))
    # The tassets ride their thighs (followers); the tabard flaps a beat late; the cape streams behind and flutters.
    lag = 2 * math.pi * (phase - 0.08)
    p.add('tabard1', rot(x=-10 * math.cos(lag) * 0.4 - 6))
    p.add('tabard2', rot(x=-12 * math.cos(2 * lag)))
    flutter = math.cos(4 * math.pi * (phase - 0.12))
    cape(p, back=20 + 3 * flutter, flutter=1.6 * flutter, spread=4, sway=3 * math.sin(2 * math.pi * (phase - 0.15)))
    return swing


def walk(k, n):
    phase = k / n
    p = stance()
    p.rot = {}
    swing = walk_legs(p, phase)
    # The sword arm swings against its leg, the blade trailing low; the buckler is carried forward, swinging less.
    fist = V(-0.25, 0.0 + 0.085 * swing, 0.5 + 0.02 * (1 - abs(swing)))
    d = blade_direction(az=-150 + 35 * (1 - swing) / 2, el=-42)
    p.hands['R'] = (fist, held(d, SHOULDER_R - fist), (-0.6, 0.9, -0.2))
    fist = V(0.21, -0.13 - 0.045 * swing, 0.55 + 0.012 * abs(swing))
    p.hands['L'] = (fist, braced(V(0.42, -0.9, 0.05), V(0.1, 0.5, 1)), (0.8, 0.6, -0.3))
    return p


# The strike: a forehand cut sweeping from the right across the front to the left. Like Ilya's, it begins at the hit
# (the damage has landed): most of the way through the cut, the cut's end, the overshoot, the recoil and the settle.
# Each key: the blade's azimuth and elevation, the right fist, the chest's turn, the lean.
STRIKE_WINDUP = dict(az=-118, el=12, fist=(-0.27, 0.02, 0.7), twist=-16, lean=0)
STRIKE_KEYS = [
    dict(az=-30, el=-2, fist=(-0.15, -0.26, 0.64), twist=2, lean=7),
    dict(az=34, el=-8, fist=(-0.01, -0.29, 0.6), twist=18, lean=10),
    dict(az=64, el=-16, fist=(0.08, -0.24, 0.56), twist=24, lean=10),
    dict(az=20, el=-34, fist=(-0.08, -0.2, 0.53), twist=10, lean=6),
    dict(az=-78, el=-40, fist=(-0.23, -0.1, 0.5), twist=0, lean=2),
]


def lerp_key(a, b, t):
    out = {}
    for key in a:
        if isinstance(a[key], tuple):
            out[key] = tuple(lerp(x, y, t) for x, y in zip(a[key], b[key]))
        else:
            out[key] = lerp(a[key], b[key], t)
    return out


def strike_upper(p, key):
    twist, lean = key['twist'], key['lean']
    p.add('pelvis', rot(z=twist * 0.3))
    p.add('spine', rot(x=lean * 0.4, z=twist * 0.4))
    p.add('chest', rot(x=lean * 0.5, z=twist * 0.6))
    p.add('head', rot(x=-lean * 0.6, z=-twist * 0.5))
    fist = V(*key['fist'])
    if 'pitch' in key:
        d = blade_direction(pitch=key['pitch'], yaw=key.get('yaw', 0.0))
    else:
        d = blade_direction(az=key['az'], el=key['el'])
    p.hands['R'] = (fist, held(d, SHOULDER_R - fist), (-0.8, 0.5, -0.4))
    # The buckler swings out to the left for balance, its face forward.
    out = smooth(-10, 20, twist)
    shield = SHIELD_REST.lerp(V(0.27, -0.02, 0.6), out)
    p.hands['L'] = (shield, braced(SHIELD_N_REST.lerp(V(0.8, -0.55, 0.1), out), V(0.0, 0.4, 1)), (0.8, 0.5, -0.3))


def swept(make_key, keys_from, keys_to, base_pose):
    """The blade's sweep from one key to the next, sampled: [(base, tip)] in the rig's space, oldest first."""
    out = []
    for i in range(SMEAR_SAMPLES + 1):
        q = base_pose()
        make_key(q, lerp_key(keys_from, keys_to, i / SMEAR_SAMPLES))
        out.append(blade_ends(q))
    return out


def strike_feet(p):
    p.feet['R'] = (V(-0.11, -0.07, ANKLE_Z), 0, -20)
    p.feet['L'] = (V(0.1, 0.06, ANKLE_Z), 0, 18)


def strike_stance():
    p = stance()
    strike_feet(p)
    return p


def strike(k, n):
    key = STRIKE_KEYS[k]
    p = strike_stance()
    strike_upper(p, key)
    cape(p, back=4 + 0.3 * key['lean'], flutter=0.5, sway=-key['twist'] * 0.25)
    if k < 3:
        previous = STRIKE_WINDUP if k == 0 else STRIKE_KEYS[k - 1]
        p.smear = swept(strike_upper, previous, key, strike_stance)
        if k == 2:      # the overshoot's smear is the tail of the cut: short
            p.smear = p.smear[SMEAR_SAMPLES // 2:]
    return p


def walk_strike(k, n):
    """The run's legs and weight under the cut at its full sweep, smear and all: shown for a strike's duration while
    the Vanguard runs, so the stride never stops for an attack."""
    p = walk(k, n)
    strike_upper(p, STRIKE_KEYS[1])
    p.smear = swept(strike_upper, STRIKE_KEYS[0], STRIKE_KEYS[1], lambda: walk(k, n))
    return p


# The Forge Burst (docs/57: "the anvil slammed down - a shockwave round the hero"): the sword raised high, brought down
# two-handed in a chop, its point driven into the floor ahead as the blade flares forge-hot, then the recovery.
CAST_KEYS = [  # the raise, the chop, the impact, the recovery, the settle
    dict(pitch=128, yaw=-6, fist=(-0.1, -0.02, 1.08), crouch=-0.012, lean=-8, glow=0.45, shield=0.0),
    dict(pitch=22, yaw=-4, fist=(-0.07, -0.25, 0.79), crouch=0.03, lean=10, glow=0.85, shield=0.6),
    dict(pitch=-58, yaw=-4, fist=(-0.05, -0.23, 0.47), crouch=0.085, lean=24, glow=1.45, shield=1.0),
    dict(pitch=-54, yaw=-12, fist=(-0.1, -0.19, 0.49), crouch=0.05, lean=13, glow=0.75, shield=0.7),
    dict(pitch=-50, yaw=-24, fist=(-0.19, -0.13, 0.49), crouch=0.016, lean=4, glow=0.25, shield=0.25),
]


def cast_upper(p, key):
    """The Burst's whole body at a key: the crouch, the lean, the sword and the buckler (the smear samples it too)."""
    cast_feet(p, key['crouch'])
    lean = key['lean']
    p.add('spine', rot(x=lean * 0.45))
    p.add('chest', rot(x=lean * 0.55, z=-4))
    p.add('head', rot(x=-lean * 0.55))
    fist = V(*key['fist'])
    d = blade_direction(pitch=key['pitch'], yaw=key['yaw'])
    p.hands['R'] = (fist, held(d, SHOULDER_R - fist), (-0.9, 0.4, -0.3))
    out = key['shield']
    shield = SHIELD_REST.lerp(V(0.31, 0.02, 0.6), out)
    p.hands['L'] = (shield, braced(SHIELD_N_REST.lerp(V(0.92, -0.2, 0.32), out), V(-0.1, 0.4, 1)), (0.9, 0.4, -0.3))


def cast_feet(p, crouch):
    p.offset = V(0, -0.012 * max(0.0, crouch) / 0.085, -crouch - 0.012)
    p.feet['R'] = (V(-0.11, -0.09, ANKLE_Z), 0, -14)
    p.feet['L'] = (V(0.11, 0.07, ANKLE_Z), 0, 14)


def cast(k, n):
    key = CAST_KEYS[k]
    p = stance()
    cast_upper(p, key)
    blown = smooth(0.0, 0.085, key['crouch'])
    cape(p, back=6 + 18 * blown, flutter=0.8 * blown, spread=7 * blown, torso=key['lean'])
    p.add('tabard1', rot(x=-6 * blown))
    p.glow['blade'] = key['glow']
    p.glow['eye'] = 1.0 + 0.4 * blown
    if k in (1, 2):
        p.smear = swept(cast_upper, CAST_KEYS[k - 1], key, stance)
        p.smear_colour = '#FFC890'
    return p


def hit(k, n):
    """A flinch: the chest snaps back and the helm turns from the blow, the buckler comes up, the sword arm flies out;
    the cape whips forward a beat late and the visor flares; then back to the stance."""
    amount = [1.0, 0.7, 0.28][k]
    whip = [0.4, 1.0, 0.45][k]
    p = stance()
    p.offset = V(0, 0.038 * amount, -0.016 * amount - 0.012)
    p.add('pelvis', rot(x=-4 * amount))
    p.add('spine', rot(x=-9 * amount))
    p.add('chest', rot(x=-11 * amount, y=4 * amount))
    p.add('head', rot(x=-14 * amount, z=16 * amount))
    fist = SWORD_REST + V(-0.05, 0.07, 0.07) * amount
    d = SWORD_DIR_REST.lerp(blade_direction(az=-70, el=-20), amount)
    p.hands['R'] = (fist, held(d, SHOULDER_R - fist), (-0.6, 0.8, -0.2))
    fist = SHIELD_REST.lerp(V(0.11, -0.21, 0.7), amount)
    p.hands['L'] = (fist, braced(SHIELD_N_REST.lerp(V(0.22, -0.94, 0.25), amount), V(0.5, 0.4, -0.2)), (0.9, 0.3, -0.3))
    cape(p, back=-8 * whip, flutter=-0.5 * whip, torso=-24 * amount)
    p.glow['eye'] = 1.0 + 0.7 * amount
    return p


def death(k, n):
    """A stagger back, then the knees go: the Vanguard kneels and slumps forward over its knees, the sword falls flat
    to the floor still in its fist, the buckler drops face up beside it, and the visor's light goes out. A heap
    rather than a body laid out, so it reads from every facing; the root never moves."""
    keys = [
        dict(drop=0.0, back=1.0, slump=0.0, fall=0.0, eye=1.0),
        dict(drop=0.07, back=0.35, slump=0.2, fall=0.2, eye=0.8),
        dict(drop=0.16, back=0.0, slump=0.5, fall=0.5, eye=0.55),
        dict(drop=0.215, back=0.0, slump=0.8, fall=0.82, eye=0.25),
        dict(drop=0.24, back=0.0, slump=1.0, fall=1.0, eye=0.04),
    ]
    c = keys[k]
    p = stance()
    p.rot = {}
    drop, slump, fall = c['drop'], c['slump'], c['fall']
    p.offset = V(0, 0.032 * c['back'] + 0.03 * slump, -drop - 0.012)
    p.add('pelvis', rot(x=-5 * c['back'] + 10 * slump))
    p.add('spine', rot(x=-9 * c['back'] + 20 * slump))
    p.add('chest', rot(x=-8 * c['back'] + 20 * slump, z=-6 * slump))
    p.add('neck', rot(x=10 * slump))
    p.add('head', rot(x=-12 * c['back'] + 16 * slump, z=-10 * slump, y=8 * slump))
    kneel = smooth(0.05, 0.22, drop)
    for s, x in (('R', -0.1), ('L', 0.1)):
        p.feet[s] = (V(x, lerp(-0.02, 0.14, kneel), lerp(ANKLE_Z, 0.045, kneel)), lerp(0, 72, kneel), 0)
    fist = SWORD_REST.lerp(V(-0.27, -0.16, 0.052), fall) + V(0, 0.04, 0.05) * c['back']
    d = turn(held(SWORD_DIR_REST, SHOULDER_R - SWORD_REST, roll=-35), held(V(-0.5, -0.86, 0.0), V(0, 0, 1), roll=90),
             smooth(0.0, 1.0, fall))
    p.hands['R'] = (fist, d, (-0.6, 0.8, -0.2))
    fist = SHIELD_REST.lerp(V(0.27, -0.04, 0.1), fall)
    p.hands['L'] = (fist, turn(braced(SHIELD_N_REST, V(0.05, 0.3, 1)), braced(V(0.45, -0.25, 0.86), V(-0.3, 0.6, 0.1)),
                               smooth(0.0, 1.0, fall)), (0.7, 0.7, -0.3))
    # The cape settles over the back as the chest bows; the floor flattens what reaches it.
    pool = smooth(0.04, 0.24, drop)
    cape(p, back=6, flutter=-0.4 * c['back'], spread=12 * pool, torso=-22 * c['back'] + 50 * slump)
    p.add('tabard1', rot(x=-20 * pool))
    p.add('tabard2', rot(x=-18 * pool))
    p.glow['eye'] = c['eye']
    return p


def shield_rush(k, n):
    """docs/57's Shield Rush, buckler first through the crowd: a brace, then three driving strides leaning in behind the
    buckler, the sword trailing, the cape streaming."""
    if k == 0:
        p = stance()
        p.offset = V(0, -0.01, -0.05)
        p.feet['R'] = (V(-0.11, 0.08, ANKLE_Z), 18, -10)
        p.feet['L'] = (V(0.1, -0.08, ANKLE_Z), 0, 10)
        lean = 12
        cape(p, back=14, flutter=0.6, spread=4, torso=12)
    else:
        p = stance()
        p.rot = {}
        walk_legs(p, [0.12, 0.37, 0.62, 0.87][k], lean=16)
        p.offset = p.offset + V(0, 0, -0.012)
        lean = 18
        cape(p, back=30, flutter=1.1 * math.cos(math.pi * k), spread=6, torso=31)
    p.add('spine', rot(x=lean * 0.5))
    p.add('chest', rot(x=lean * 0.5, z=8))
    p.add('head', rot(x=-lean * 0.75, z=-6))
    fist = V(0.07, -0.25, 0.64)
    p.hands['L'] = (fist, braced(V(0.16, -0.98, 0.05), V(0.85, 0.35, -0.25)), (0.9, 0.2, -0.4))
    fist = V(-0.25, 0.1, 0.56)
    p.hands['R'] = (fist, held(blade_direction(az=-158, el=-35), SHOULDER_R - fist), (-0.7, 0.6, -0.3))
    p.glow['eye'] = 1.15
    return p


CLIP_POSES = {'idle': idle, 'walk': walk, 'walk-strike': walk_strike, 'strike': strike, 'cast': cast, 'hit': hit,
              'death': death, 'shield-rush': shield_rush}


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
    dirs = []
    count = 40
    for i in range(count):
        zc = 1 - (i + 0.5) / count
        r = math.sqrt(max(0.0, 1 - zc * zc))
        a = i * 2.399963
        dirs.append(V(r * math.cos(a), r * math.sin(a), zc))
    reach = 0.1
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
                hit_ = tree.ray_cast(origin, ray, reach)
                if hit_[0] is not None:
                    occluded += (1 - hit_[3] / reach) * d.z
            values.append(occluded)
        ev.to_mesh_clear()
        norm = sum(d.z for d in dirs)
        attr = obj.data.color_attributes.new('occlusion', 'FLOAT_COLOR', 'POINT')
        for i, o in enumerate(values):
            open_ = max(0.0, 1 - 2.1 * o / norm)
            attr.data[i].color = (open_, open_, open_, 1.0)


bake_occlusion()
for obj in PARTS.values():
    finish_modifiers(obj)


# ---------------------------------------------------------------------------------------------------------------------
# Camera, facings and the frame loop: BuildBlenderArcanist.py's
# ---------------------------------------------------------------------------------------------------------------------
RIG.scale = (1, 1, HEIGHT)
camera_data = bpy.data.cameras.new('Game view: orthographic, 30 degrees down, looking north')
camera_data.type = 'ORTHO'
camera_data.ortho_scale = max(CANVAS, CANVAS_HEIGHT)
camera_data.clip_start = 0.1
camera_data.clip_end = 100
camera = bpy.data.objects.new('Camera', camera_data)
collection.objects.link(camera)
scene.camera = camera
centre = (CANVAS * PPU / 2 - PIVOT[0]) / PPU * RIGHT + (CANVAS_HEIGHT * PPU / 2 - PIVOT[1]) / PPU * UP
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
                fx, fy = forge_screen(pose)
                records.append(dict(facing=facing, clip=clip, frame=k, file=str(path.relative_to(OUT)).replace('\\', '/'),
                                    forge=[round(fx, 5), round(fy, 5)], heat=round(pose.glow['blade'], 4),
                                    smear=pose.smear is not None))
                print('VANGUARD_FRAME', facing, clip, k, '%.1fs' % (time.time() - started), flush=True)
    meta = dict(blender=bpy.app.version_string, ppu=PPU, supersample=SS, canvas=CANVAS, canvas_height=CANVAS_HEIGHT,
                pivot=PIVOT, elevation_degrees=30, height_compensation=HEIGHT, stride=STRIDE,
                clips={c: dict(frames=CLIPS[c][0], fps=CLIPS[c][1], loops=CLIPS[c][2]) for c in CLIPS},
                rendered_motion=['idle', 'walk', 'walk-strike', 'strike', 'cast', 'hit', 'death'],
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
        action = bpy.data.actions.new('Vanguard ' + clip)
        action.use_fake_user = True
        RIG.animation_data.action = action
        for k in range(frames):
            apply(CLIP_POSES[clip](k, frames))
            for pb in RIG.pose.bones:
                pb.keyframe_insert('location', frame=k + 1)
                pb.keyframe_insert('rotation_quaternion', frame=k + 1)
                pb.keyframe_insert('scale', frame=k + 1)
    RIG.animation_data.action = bpy.data.actions['Vanguard idle']


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
print('VANGUARD_COMPLETE', 'vertices', v, 'triangles', t, flush=True)
