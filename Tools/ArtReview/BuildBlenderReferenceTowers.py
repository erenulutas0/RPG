"""Two editable furnace-tower bakes closer to the approved concept's towers.

Run in Blender 4.5 LTS background mode:
  TestResults/tools/blender-4.5.9-windows-x64/blender.exe --background --python Tools/ArtReview/BuildBlenderReferenceTowers.py

The original lower foundation (stone courses, arched fire mouths, grates and
cornice) is preserved from the production source blend; only the upper tower is
rebuilt. The concept's towers are stacked dark iron drums ringed with small
glowing windows, bright brass bands and a crenellated or stepped crown, joined
by thick piped collars. Contracts kept for the existing runtime cards:
foundation radius 2, receiver radius 1.6 with the pipe contact at height 4.05,
open flue centred at (0, 0, 7.2), same camera, lights and AgX view. All detail is
native mesh/material work; nothing is painted onto the render.
"""
import ast
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtDirection/2026-10-20/reference-towers-01'
SOURCE = ROOT / 'ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend'
HEIGHT = 1 / math.cos(math.radians(30))
OUT.mkdir(parents=True, exist_ok=True)

helpers = ast.parse((ROOT / 'Tools/ArtReview/BuildBlenderFurnace.py').read_text())
names = {'material', 'finish', 'cylinder', 'cube', 'sector', 'facade_piece'}
exec(compile(ast.Module(body=[n for n in helpers.body if isinstance(n, ast.FunctionDef)
                            and n.name in names], type_ignores=[]), '<furnace constructors>', 'exec'))
industrial = ast.parse((ROOT / 'Tools/ArtReview/BuildBlenderIndustrialFurnaces.py').read_text())
exec(compile(ast.Module(body=[n for n in industrial.body if isinstance(n, ast.FunctionDef)
                              and n.name in {'annular_lip', 'retained_lower_source', 'common_receiver'}],
                        type_ignores=[]), '<industrial constructors>', 'exec'))

# The visible faces look toward the camera at (-12, -12): azimuths around 225 degrees.
FRONT = math.radians(225)


def ramp_material(source, name, dark, light, roughness):
    mat = bpy.data.materials[source].copy()
    mat.name = name
    for node in mat.node_tree.nodes:
        if node.type == 'VALTORGB':
            node.color_ramp.elements[0].color = (*dark, 1)
            node.color_ramp.elements[-1].color = (*light, 1)
        if node.type == 'BSDF_PRINCIPLED':
            node.inputs['Roughness'].default_value = roughness
    return mat


def palette():
    # Neutral-warm blackened iron instead of the source's cyan-leaning forged blue.
    iron = ramp_material('Forged blue iron', 'Blackened tower iron', (.019, .017, .016), (.058, .053, .049), .64)
    plate = ramp_material('Forged blue iron', 'Riveted tower plate', (.025, .023, .021), (.072, .066, .060), .60)
    brass = ramp_material('Rubbed old bronze', 'Polished tower brass', (.11, .064, .026), (.31, .19, .074), .46)
    return iron, plate, brass


def window(name, angle, radius, z0, z1, width, soot, glow, frame):
    """A small arched window: dark reveal, a warm glowing pane set into it and a brass sill."""
    w = width * .5
    arch = [(-w, z0), (w, z0), (w, z1 - w)]
    for i in range(1, 6):
        a = math.pi * i / 6
        arch.append((w * math.cos(a), z1 - w + w * math.sin(a) * .9))
    arch.append((-w, z1 - w))
    facade_piece(name + ' reveal', angle, radius + .015, arch, .09, soot, .006)
    inner = [(x * .72, z0 + (z - z0) * .9 + .02) for x, z in arch]
    facade_piece(name + ' glowing pane', angle, radius + .05, inner, .02, glow, 0)
    normal = Vector((math.cos(angle), math.sin(angle)))
    cube(name + ' sill', (*(normal * (radius + .05)), z0 - .02), (.09, width * 1.25, .05), frame, angle, .01)


def window_ring(prefix, radius, z0, z1, width, count, soot, glow, frame, offset=0):
    for i in range(count):
        a = offset + i * math.tau / count
        window(f'{prefix} {i}', a, radius, z0, z1, width, soot, glow, frame)


def band(name, radius, z0, z1, brass):
    cylinder(name, radius, z0, z1, brass, 32, bevel=.012)


def crenellated_tower(soot, glow):
    """Near tower: two drums of lit windows under a crenellated parapet crown."""
    iron, plate, brass = palette()
    common_receiver(iron, brass, 0)
    cylinder('Lower window drum', 1.55, 4.38, 5.56, plate, 32, bevel=.03)
    band('Lower drum brass band', 1.60, 4.38, 4.50, brass)
    window_ring('Lower drum window', 1.55, 4.64, 5.38, .31, 10, soot, glow, brass, FRONT % (math.tau / 10))
    band('Mid tower brass band', 1.60, 5.56, 5.70, brass)
    cylinder('Upper window drum', 1.36, 5.70, 6.52, iron, 32, bevel=.03)
    window_ring('Upper drum window', 1.36, 5.82, 6.36, .25, 10, soot, glow, brass, FRONT % (math.tau / 10) + math.tau / 20)
    band('Upper tower brass band', 1.42, 6.48, 6.58, brass)
    cylinder('Parapet corbel ring', 1.50, 6.58, 6.72, plate, 32, bevel=.03)
    cylinder('Parapet wall', 1.46, 6.72, 6.88, iron, 32, bevel=.02)
    for i in range(10):
        a = FRONT + i * math.tau / 10
        p = Vector((math.cos(a), math.sin(a))) * 1.38
        cube('Parapet merlon', (p.x, p.y, 6.98), (.24, .34, .20), plate, a, .02)
    cylinder('Crown flue base', .62, 6.72, 6.96, iron, 24, bevel=.025)
    cylinder('Crown flue', .46, 6.96, 7.10, iron, 24, bevel=.02)
    annular_lip('Open crown flue lip', .40, .54, 7.06, 7.20, plate)
    cylinder('Dark flue interior', .40, 7.11, 7.12, soot, 24, bevel=0)


def domed_tower(soot, glow):
    """Far tower: one lit window drum carrying a stepped, ribbed dome."""
    iron, plate, brass = palette()
    common_receiver(iron, brass, 1)
    cylinder('Window drum', 1.55, 4.38, 5.72, plate, 32, bevel=.03)
    band('Drum foot brass band', 1.60, 4.38, 4.50, brass)
    window_ring('Drum window', 1.55, 4.64, 5.52, .33, 9, soot, glow, brass, FRONT % (math.tau / 9))
    band('Dome seat brass band', 1.62, 5.72, 5.86, brass)
    cylinder('Lower dome step', 1.50, 5.86, 6.18, iron, 32, 1.30, .03)
    band('Dome step band', 1.31, 6.18, 6.26, brass)
    cylinder('Upper dome step', 1.24, 6.26, 6.58, iron, 32, .92, .03)
    for i in range(12):
        a = FRONT + i * math.tau / 12
        n = Vector((math.cos(a), math.sin(a)))
        cube('Dome rib', (*(n * 1.36), 6.02), (.28, .09, .30), plate, a, .015)
        cube('Upper dome rib', (*(n * 1.07), 6.42), (.24, .08, .30), plate, a, .015)
    window_ring('Dome lantern window', .92, 6.61, 6.90, .17, 6, soot, glow, brass, FRONT % (math.tau / 6))
    cylinder('Dome lantern', .92, 6.58, 6.94, iron, 24, bevel=.025)
    band('Lantern cap band', .98, 6.92, 6.98, brass)
    cylinder('Lantern cap', .86, 6.98, 7.06, plate, 24, .52, .02)
    cylinder('Dome flue', .44, 7.02, 7.12, iron, 24, bevel=.02)
    annular_lip('Open dome flue lip', .38, .50, 7.08, 7.20, plate)
    cylinder('Dark dome flue interior', .38, 7.12, 7.13, soot, 24, bevel=0)


budgets = []
for variant, build in enumerate([crenellated_tower, domed_tower]):
    iron, bronze, soot, fire = retained_lower_source()
    build(soot, fire)
    scene = bpy.context.scene
    bpy.context.preferences.filepaths.save_version = 0
    scene.render.resolution_x = scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.film_transparent = True
    scene.cycles.samples = 48
    scene.cycles.use_denoising = True
    scene.camera.data.ortho_scale = 10.8
    scene.render.filepath = str(OUT / f'variant-{variant}.png')
    deps = bpy.context.evaluated_depsgraph_get()
    vertices = triangles = 0
    radius = top = 0
    for obj in scene.objects:
        if obj.type != 'MESH':
            continue
        evaluated = obj.evaluated_get(deps)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        vertices += len(mesh.vertices)
        triangles += len(mesh.loop_triangles)
        for vertex in mesh.vertices:
            p = obj.matrix_world @ vertex.co
            radius = max(radius, math.hypot(p.x, p.y))
            top = max(top, p.z / HEIGHT)
        evaluated.to_mesh_clear()
    budgets.append(dict(variant=variant, evaluated_vertices=vertices, evaluated_triangles=triangles,
                        actual_maximum_radius=radius, actual_maximum_height=top,
                        nominal_foundation_radius=2, shell_socket_radius=1.6, shell_socket_height=4.05,
                        flue_outlet=[0, 0, 7.2], width=1024, height=1024, ortho_scale=10.8, image_bottom=-1.8,
                        height_compensation=HEIGHT, blender=bpy.app.version_string,
                        source_blend=str(SOURCE.relative_to(ROOT)),
                        limitation='Native authoring meshes; only the RGBA bakes feed the existing runtime cards.'))
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / f'variant-{variant}.blend'))
    bpy.ops.render.render(write_still=True)
    print('REFERENCE_TOWER_VARIANT_COMPLETE', variant, vertices, triangles, flush=True)
(OUT / 'model-budget.json').write_text(json.dumps(budgets, indent=2))
