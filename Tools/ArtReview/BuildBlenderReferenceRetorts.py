"""Low foundry retorts in the reference towers' family (dark iron, brass bands, lit windows).

Run with portable Blender 4.5.9:
  TestResults/tools/blender-4.5.9-windows-x64/blender.exe --background --python Tools/ArtReview/BuildBlenderReferenceRetorts.py

Keeps the 2026-10-11 retort contract exactly: the basalt footing, masonry, squat
segmental fire doors, grates and cornice are rebuilt by the same constructors;
height stays <= 2.28 (south structures stay squat), the exhaust outlet stays at
(0, 0, 2.28), the physical bounds equal the original source and the camera,
ortho scale 6 and lights are unchanged. Only the hood above the cornice changes.
No raster post-processing.
"""
import ast
import bpy
import json
import math
from bpy_extras.object_utils import world_to_camera_view
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtDirection/2026-10-20/reference-retorts-01'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend'))
HEIGHT = 1 / math.cos(math.radians(30))
stone = bpy.data.materials['Cool weathered basalt']
arch_stone = bpy.data.materials['Dressed arch basalt']
iron = bpy.data.materials['Forged blue iron']
bronze = bpy.data.materials['Rubbed old bronze']
soot = bpy.data.materials['Soot dark recess']
fire = bpy.data.materials['Contained forge heat']

# Reuse the retort's own constructors (and its base/door builder) verbatim.
source = ast.parse((ROOT / 'Tools/ArtReview/BuildBlenderIndustrialRetorts.py').read_text())
wanted = {'finish', 'cylinder', 'cube', 'sector', 'facade_piece', 'arch_profile', 'loft', 'rod', 'build_base_and_doors'}
exec(compile(ast.Module(body=[n for n in source.body if isinstance(n, ast.FunctionDef) and n.name in wanted],
                        type_ignores=[]), '<retort constructors>', 'exec'))
FRONT = math.radians(225)


def ramp_material(base, name, dark, light, roughness):
    mat = bpy.data.materials[base].copy()
    mat.name = name
    for node in mat.node_tree.nodes:
        if node.type == 'VALTORGB':
            node.color_ramp.elements[0].color = (*dark, 1)
            node.color_ramp.elements[-1].color = (*light, 1)
        if node.type == 'BSDF_PRINCIPLED':
            node.inputs['Roughness'].default_value = roughness
    return mat


# Same palette as Tools/ArtReview/BuildBlenderReferenceTowers.py.
black = ramp_material('Forged blue iron', 'Blackened tower iron', (.019, .017, .016), (.058, .053, .049), .64)
plate = ramp_material('Forged blue iron', 'Riveted tower plate', (.025, .023, .021), (.072, .066, .060), .60)
brass = ramp_material('Rubbed old bronze', 'Polished tower brass', (.11, .064, .026), (.31, .19, .074), .46)


def window(name, angle, radius, z0, z1, width):
    w = width * .5
    arch = [(-w, z0), (w, z0), (w, z1 - w)]
    for i in range(1, 6):
        a = math.pi * i / 6
        arch.append((w * math.cos(a), z1 - w + w * math.sin(a) * .9))
    arch.append((-w, z1 - w))
    facade_piece(name + ' reveal', angle, radius + .012, arch, .07, soot, .005)
    inner = [(x * .72, z0 + (z - z0) * .9 + .015) for x, z in arch]
    facade_piece(name + ' glowing pane', angle, radius + .04, inner, .015, fire, 0)


def hood(variant):
    cylinder('Continuous iron seating flange', 1.60, 1.45, 1.53, black, 24, bevel=.025)
    cylinder('Window drum brass foot', 1.50, 1.53, 1.58, brass, 32, bevel=.01)
    cylinder('Low window drum', 1.45, 1.58, 1.92, plate, 32, bevel=.02)
    for i in range(10):
        window(f'Low drum window {i}', FRONT % (math.tau / 10) + i * math.tau / 10, 1.45, 1.62, 1.89, .22)
    cylinder('Window drum brass head', 1.50, 1.92, 1.97, brass, 32, bevel=.01)
    if variant == 0:
        # A stepped, ribbed dome like the concept's far tower crown.
        cylinder('Lower dome step', 1.44, 1.97, 2.09, black, 32, 1.02, .02)
        for i in range(12):
            a = FRONT + i * math.tau / 12
            n = Vector((math.cos(a), math.sin(a)))
            cube('Dome rib', (*(n * 1.24), 2.03), (.26, .08, .13), plate, a, .012)
        cylinder('Dome step band', 1.04, 2.09, 2.12, brass, 32, bevel=.008)
        cylinder('Upper dome step', .98, 2.12, 2.19, black, 32, .46, .02)
    else:
        # A low corbelled parapet with merlons, like the concept's near tower.
        cylinder('Parapet corbel', 1.48, 1.97, 2.02, plate, 32, bevel=.02)
        cylinder('Parapet deck', 1.38, 2.02, 2.08, black, 32, bevel=.015)
        for i in range(10):
            a = FRONT + i * math.tau / 10
            p = Vector((math.cos(a), math.sin(a))) * 1.36
            cube('Parapet merlon', (p.x, p.y, 2.13), (.24, .30, .11), plate, a, .015)
        cylinder('Raised flue deck', .70, 2.08, 2.16, black, 24, bevel=.02)
    cylinder('Centred exhaust seat', .29, 2.15, 2.19, black, 16, bevel=.012)
    cylinder('Low exhaust neck', .20, 2.18, 2.23, black, 16, bevel=.012)
    cylinder('Exhaust outlet lip', .25, 2.22, 2.28, plate, 16, bevel=.008)
    cylinder('Dark exhaust mouth', .185, 2.274, 2.28, soot, 16, bevel=0)


scene = bpy.context.scene
bpy.context.preferences.filepaths.save_version = 0
budgets = []
for variant in [0, 1]:
    for obj in list(bpy.data.objects):
        if obj.type == 'MESH':
            bpy.data.objects.remove(obj, do_unlink=True)
    build_base_and_doors()
    hood(variant)
    camera = scene.camera
    target = Vector((0, 0, 1.14 * HEIGHT))
    camera.location = target + Vector((-12, -12, math.sqrt(96)))
    camera.rotation_euler = (target - camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.ortho_scale = 6
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.cycles.samples = 48
    scene.render.filepath = str(OUT / f'variant-{variant}.png')
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    vertices = triangles = 0
    bounds = []
    for obj in scene.objects:
        if obj.type != 'MESH':
            continue
        evaluated = obj.evaluated_get(deps)
        mesh = evaluated.to_mesh()
        mesh.calc_loop_triangles()
        vertices += len(mesh.vertices)
        triangles += len(mesh.loop_triangles)
        bounds.extend([obj.matrix_world @ v.co for v in mesh.vertices])
        evaluated.to_mesh_clear()
    screen = [world_to_camera_view(scene, camera, p) for p in bounds]
    projected = [min(p.x for p in screen), min(p.y for p in screen), max(p.x for p in screen), max(p.y for p in screen)]
    physical = [min(p.x for p in bounds), min(p.y for p in bounds), max(p.x for p in bounds), max(p.y for p in bounds)]
    height_range = [min(p.z for p in bounds) / HEIGHT, max(p.z for p in bounds) / HEIGHT]
    assert height_range[0] >= -.001 and height_range[1] <= 2.2801, height_range
    assert min(projected) >= 0 and max(projected) <= 1, projected
    record = dict(variant=variant, blender=bpy.app.version_string, vertices=vertices, triangles=triangles,
                  ortho_scale=6, render_size=1024, height_compensation=HEIGHT, height_range=height_range,
                  physical_bounds=physical, projected_normalized_bounds=projected, exhaust_outlet=[0, 0, 2.28],
                  source='Native editable geometry; study02 lights; reference-tower palette; no raster postprocessing')
    budgets.append(record)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / f'variant-{variant}.blend'))
    bpy.ops.render.render(write_still=True)
    print('REFERENCE_RETORT_VARIANT_COMPLETE', variant, vertices, triangles, flush=True)
(OUT / 'model-budget.json').write_text(json.dumps(budgets, indent=2) + '\n')
original = json.loads((ROOT / 'ArtDirection/2026-10-11/industrial-retort-01/original-contract-comparison.json').read_text())
for budget in budgets:
    assert all(abs(a - b) < .0001 for a, b in zip(budget['physical_bounds'], original['original_physical_bounds'])), \
        (budget['physical_bounds'], original['original_physical_bounds'])
print('REFERENCE_RETORT_BOUNDS_MATCH_ORIGINAL', flush=True)
