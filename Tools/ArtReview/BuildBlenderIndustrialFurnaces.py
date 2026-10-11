"""Two editable industrial furnace bakes, preserving the production projection.

Run in Blender 4.5 LTS background mode. The original source blend and every
Unity asset remain untouched. All visible detail is native mesh/material work.
"""
import ast
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtDirection/2026-10-11/industrial-furnace-01'
SOURCE = ROOT / 'ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend'
HEIGHT = 1 / math.cos(math.radians(30))
OUT.mkdir(parents=True, exist_ok=True)

# Reuse the authored mesh constructors, not the older script's scene/output code.
helpers = ast.parse((ROOT / 'Tools/ArtReview/BuildBlenderFurnace.py').read_text())
names = {'material', 'finish', 'cylinder', 'cube', 'sector', 'facade_piece'}
exec(compile(ast.Module(body=[n for n in helpers.body if isinstance(n, ast.FunctionDef)
                            and n.name in names], type_ignores=[]), '<furnace constructors>', 'exec'))


def upper_material(name, factor, roughness):
    mat = bpy.data.materials['Forged blue iron'].copy()
    mat.name = name
    nodes = mat.node_tree.nodes
    shader = nodes.get('Principled BSDF')
    shader.inputs['Roughness'].default_value = roughness
    for node in nodes:
        if node.type == 'VALTORGB':
            for entry in node.color_ramp.elements:
                c = entry.color
                entry.color = (c[0]*factor, c[1]*factor, c[2]*factor, 1)
    # Broad forged-surface variation affects roughness too, rather than a glossy
    # sheet with unrelated small flecks. This remains a native procedural graph.
    grain = nodes.new('ShaderNodeTexNoise')
    grain.name = 'Broad wrought surface roughness'
    grain.inputs['Scale'].default_value = 4.5
    grain.inputs['Detail'].default_value = 2
    mapping = nodes.new('ShaderNodeMapRange')
    mapping.inputs['From Min'].default_value = 0
    mapping.inputs['From Max'].default_value = 1
    mapping.inputs['To Min'].default_value = roughness-.1
    mapping.inputs['To Max'].default_value = roughness+.14
    mat.node_tree.links.new(grain.outputs['Fac'], mapping.inputs['Value'])
    mat.node_tree.links.new(mapping.outputs['Result'], shader.inputs['Roughness'])
    return mat


def bearing_rib(name, angle, r0, r1, bottom, top, width, depth, mat):
    normal = Vector((math.cos(angle), math.sin(angle)))
    tangent = Vector((-math.sin(angle), math.cos(angle)))
    points = []
    for r, z in [(r0, bottom), (r1, top)]:
        for radial, side in [(-depth/2, -width/2), (-depth/2, width/2),
                             (depth/2, width/2), (depth/2, -width/2)]:
            p = normal*(r+radial)+tangent*side
            points.append((p.x, p.y, z*HEIGHT))
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(points, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, mat, .024)


def annular_lip(name, inner, outer, bottom, top, mat):
    points = []
    count = 24
    for z, radius in [(bottom, inner), (bottom, outer), (top, inner), (top, outer)]:
        points.extend((radius*math.cos(i*math.tau/count), radius*math.sin(i*math.tau/count), z*HEIGHT)
                      for i in range(count))
    faces = []
    for i in range(count):
        j = (i+1)%count
        faces.extend([(i, j, count+j, count+i), (2*count+i, 3*count+i, 3*count+j, 2*count+j),
                      (i, 2*count+i, 2*count+j, j), (count+i, count+j, 3*count+j, 3*count+i)])
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(points, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj, name, mat, .01)


def retained_lower_source():
    bpy.ops.wm.open_mainfile(filepath=str(SOURCE))
    # The complete lower foundation, masonry, arched heat/grates and cornice are
    # preserved. The cylindrical upper receiver is rebuilt for its own purpose.
    for obj in list(bpy.context.scene.objects):
        if obj.type != 'MESH':
            continue
        low = min((obj.matrix_world @ Vector(p)).z for p in obj.bound_box)/HEIGHT
        if low > 3.60:
            bpy.data.objects.remove(obj, do_unlink=True)
    # Cohesive quarry variation, assigned per existing stone, never tiny decals.
    source = bpy.data.materials['Cool weathered basalt']
    stones = []
    for i, factor in enumerate([.89, .96, 1.035]):
        mat = source.copy()
        mat.name = 'Quarry basalt family ' + str(i)
        for node in mat.node_tree.nodes:
            if node.type == 'VALTORGB':
                for entry in node.color_ramp.elements:
                    c = entry.color
                    entry.color = (c[0]*factor, c[1]*factor, c[2]*factor, 1)
        stones.append(mat)
    for obj in bpy.context.scene.objects:
        if obj.name.startswith('Basalt course'):
            words = obj.name.split()
            row, index = int(words[2]), int(words[4])
            obj.data.materials[0] = stones[(row*7+index*11)%3]
    return (bpy.data.materials['Forged blue iron'], bpy.data.materials['Rubbed old bronze'],
            bpy.data.materials['Soot dark recess'], bpy.data.materials['Contained forge heat'])


def common_receiver(iron, bronze, variant):
    cylinder('Load bearing circular receiver socket', 1.60, 3.58, 4.32, iron, 24)
    cylinder('Receiver foot retaining ring', 1.65, 3.58, 3.77, bronze, 24)
    cylinder('Receiver bearing flange', 1.66, 4.22, 4.38, iron, 24)
    # Sparse large fasteners are secondary to complete load-bearing massing.
    for i in range(12):
        a = i*math.tau/12
        cube('Receiver flange seat', (1.60*math.cos(a), 1.60*math.sin(a), 4.31),
             (.15, .16, .12), iron, a, .018)


def square_receiver(iron, bronze, soot, fire):
    """Near furnace: an angular cast receiver with a broad stepped iron stack."""
    dark = upper_material('Sooted cast recessed panels', .60, .69)
    face = upper_material('Weathered cast pressure chest', 1.10, .62)
    edge = upper_material('Rubbed load bearing edges', 1.26, .53)
    common_receiver(iron, bronze, 0)
    cube('Square receiver bearing shoe', (0, 0, 4.43), (2.64, 2.64, .22), iron, bevel=.08)
    cube('Rectangular pressure stack body', (0, 0, 5.36), (2.22, 2.22, 1.72), face, bevel=.10)
    # The two visible faces have broad, deeply framed inspection panels. Four
    # bearing piers continue into the supporting shoe, not floating face strips.
    for a in [math.pi, math.pi*1.5]:
        normal = Vector((math.cos(a), math.sin(a)))
        tangent = Vector((-math.sin(a), math.cos(a)))
        cube('Recessed service panel', (*normal*1.125, 5.35), (.06, 1.63, 1.24), dark, a, .05)
        for side in [-1, 1]:
            p = normal*1.16+tangent*(side*.89)
            cube('Continuous chest bearing pier', (*p, 5.36), (.19, .22, 1.73), edge, a, .032)
        for z in [4.72, 5.98]:
            cube('Inspection panel cross casting', (*normal*1.18, z), (.14, 1.78, .14), iron, a, .025)
        # A deep service opening, not another large bright fireplace.
        cube('Upper inspection casting', (*normal*1.18, 5.37), (.12, .38, .64), iron, a, .035)
        cube('Upper inspection recess', (*normal*1.252, 5.37), (.02, .20, .44), soot, a, .015)
        cube('Quiet lower inspection heat', (*normal*1.267, 5.24), (.012, .12, .12), fire, a, .008)
    cube('Broad receiver lid seat', (0, 0, 6.28), (2.44, 2.44, .20), iron, bevel=.07)
    cube('Raised inspection crown', (0, 0, 6.46), (1.98, 1.98, .18), face, bevel=.08)
    cube('Central exhaust bearing', (0, 0, 6.60), (1.14, 1.14, .16), iron, bevel=.055)
    cube('Square exhaust shaft', (0, 0, 6.88), (.76, .76, .48), iron, bevel=.025)
    for side in [-1, 1]:
        cube('Open square mouth front rail', (0, side*.41, 7.135), (.92, .10, .13), edge, bevel=.012)
        cube('Open square mouth side rail', (side*.41, 0, 7.135), (.10, .72, .13), edge, bevel=.012)
    cube('Recessed square exhaust interior', (0, 0, 7.13), (.72, .72, .009), soot, bevel=.005)


def ribbed_stack(iron, bronze, soot, fire):
    """Far furnace: tapered exhaust barrel carried by substantial sloped ribs."""
    face = upper_material('Heat cycled exhaust iron', .91, .64)
    edge = upper_material('Rib bearing edges', 1.18, .56)
    common_receiver(iron, bronze, 1)
    cylinder('Tapered exhaust chamber', 1.50, 4.34, 5.42, face, 24, 1.03, .045)
    cylinder('Upright exhaust drum', 1.03, 5.42, 6.37, face, 24, bevel=.035)
    for i in range(8):
        a = math.tau*i/8
        bearing_rib('Continuous sloping cast rib', a, 1.49, 1.065, 4.34, 5.44, .19, .20, edge)
        bearing_rib('Upper exhaust jacket rib', a, 1.065, 1.065, 5.43, 6.40, .15, .16, iron)
    cylinder('Shoulder compression ring', 1.15, 5.36, 5.49, iron, 24, bevel=.025)
    cylinder('Upper exhaust collector', 1.19, 6.33, 6.50, iron, 24, bevel=.04)
    cylinder('Cast neck reduction', 1.06, 6.50, 6.75, face, 24, .54, .035)
    cylinder('Central hot exhaust', .44, 6.73, 7.10, iron, 24, bevel=.025)
    annular_lip('Open annular exhaust lip', .40, .52, 7.06, 7.20, edge)
    cylinder('Dark exhaust interior', .40, 7.11, 7.12, soot, 24, bevel=0)
    for a in [math.radians(195), math.radians(255)]:
        normal = Vector((math.cos(a), math.sin(a)))
        cube('Receiver inspection casting', (*normal*1.625, 3.99), (.12, .31, .39), iron, a, .025)
        cube('Receiver inspection soot', (*normal*1.70, 3.99), (.015, .18, .25), soot, a, .01)
        cube('Receiver residual heat', (*normal*1.713, 3.94), (.01, .11, .10), fire, a, .006)


budgets = []
for variant, build in enumerate([square_receiver, ribbed_stack]):
    iron, bronze, soot, fire = retained_lower_source()
    build(iron, bronze, soot, fire)
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
            top = max(top, p.z/HEIGHT)
        evaluated.to_mesh_clear()
    budget = dict(variant=variant, evaluated_vertices=vertices, evaluated_triangles=triangles,
                  actual_maximum_radius=radius, actual_maximum_height=top,
                  nominal_foundation_radius=2, shell_socket_radius=1.6,
                  shell_socket_height=4.05, flue_outlet=[0, 0, 7.2],
                  width=1024, height=1024, ortho_scale=10.8, image_bottom=-1.8,
                  height_compensation=HEIGHT, blender=bpy.app.version_string,
                  source_blend=str(SOURCE.relative_to(ROOT)),
                  limitation='Native authoring meshes; only the RGBA bakes are intended for existing runtime cards.')
    budgets.append(budget)
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT / f'variant-{variant}.blend'))
    bpy.ops.render.render(write_still=True)
    print('INDUSTRIAL_FURNACE_VARIANT_COMPLETE', variant, vertices, triangles, flush=True)
(OUT / 'model-budget.json').write_text(json.dumps(budgets, indent=2))
