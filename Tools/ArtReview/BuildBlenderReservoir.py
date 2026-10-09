"""Author the existing foundry pressure-vessel silhouette with boiler-study materials.

Run with Blender 4.5 LTS. Writes only ArtDirection/reservoir-01. The footing,
branch, relief stem and valve stay procedural in Unity; no Unity Assets change.
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtDirection/2026-10-09/reservoir-01'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend'))
for obj in list(bpy.data.objects):
    if obj.type == 'MESH':
        bpy.data.objects.remove(obj, do_unlink=True)
HEIGHT = 1 / math.cos(math.radians(30))
iron = bpy.data.materials['Forged blue iron']
bronze = bpy.data.materials['Rubbed old bronze']
soot = bpy.data.materials['Soot dark recess']


def finish(obj, name, material, bevel=0):
    obj.name = name
    obj.data.materials.append(material)
    if bevel:
        modifier = obj.modifiers.new('Dressed rolled edges', 'BEVEL')
        modifier.width = bevel
        modifier.segments = 2
        modifier = obj.modifiers.new('Weighted face normals', 'WEIGHTED_NORMAL')
        modifier.keep_sharp = True
    return obj


def cylinder(name, radius, bottom, top, material, upper=None, bevel=.012):
    bpy.ops.mesh.primitive_cone_add(vertices=48, radius1=radius,
        radius2=radius if upper is None else upper, depth=(top-bottom)*HEIGHT,
        location=(0, 0, (bottom+top)*.5*HEIGHT))
    return finish(bpy.context.object, name, material, bevel)


def cube(name, size, height, material, bevel):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(0, 0, height*HEIGHT))
    obj = bpy.context.object
    obj.scale = (size[0], size[1], size[2]*HEIGHT)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, material, bevel)


def rivet(angle, height, radius, material=bronze):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=4, radius=.023,
        location=(radius*math.cos(angle), radius*math.sin(angle), height*HEIGHT))
    return finish(bpy.context.object, 'Restrained belt fastener', material)


# Exactly the runtime iron support footprint: 1.52 square, .25 to .43 high.
cube('Riveted iron foot', (1.52, 1.52, .18), .34, iron, .035)
cylinder('Lower dished end', .68, .43, .69, iron, .82)
cylinder('Lower hull course', .82, .69, .91, iron)
cylinder('Lower retaining belt', .85, .91, 1.04, iron)
cylinder('Continuous rolled pressure shell', .82, 1.04, 2.13, iron)
cylinder('Upper retaining belt', .85, 2.13, 2.26, iron)
cylinder('Upper hull course', .82, 2.26, 2.45, iron)
cylinder('Pressed shoulder', .82, 2.45, 2.77, iron, .57)
cylinder('Domed pressure crown', .57, 2.77, 2.91, iron, .27)
cube('Relief valve seat', (.4, .4, .13), 2.975, iron, .025)

# Sparse fasteners read as metal construction; no bright fire or extra apparatus.
for height in [.975, 2.195]:
    for index in range(12):
        rivet(index*math.tau/12, height, .847)
# Two narrow curved seams stop before the belts, preserving the broad quiet plate.
for angle in [math.pi, math.pi*1.5]:
    vertices = []
    for height in [1.045, 2.125]:
        for a in [angle-.009, angle+.009]:
            vertices.append((.822*math.cos(a), .822*math.sin(a), height*HEIGHT))
    mesh = bpy.data.meshes.new('Rolled plate joint')
    mesh.from_pydata(vertices, [], [(0, 1, 3, 2)])
    mesh.update()
    obj = bpy.data.objects.new('Rolled plate joint', mesh)
    bpy.context.collection.objects.link(obj)
    finish(obj, 'Rolled plate joint', soot)

scene = bpy.context.scene
camera = scene.camera
target = Vector((0, 0, 1.5*HEIGHT))
camera.location = target + Vector((-12, -12, math.sqrt(96)))
camera.rotation_euler = (target-camera.location).to_track_quat('-Z', 'Y').to_euler()
camera.data.ortho_scale = 4
scene.render.resolution_x = 1024
scene.render.resolution_y = 1024
scene.render.filepath = str(OUT / 'reservoir.png')
bpy.context.preferences.filepaths.save_version = 0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT / 'reservoir.blend'))
bpy.ops.render.render(write_still=True)

deps = bpy.context.evaluated_depsgraph_get()
vertices = triangles = 0
for obj in scene.objects:
    if obj.type != 'MESH':
        continue
    evaluated = obj.evaluated_get(deps)
    mesh = evaluated.to_mesh()
    mesh.calc_loop_triangles()
    vertices += len(mesh.vertices)
    triangles += len(mesh.loop_triangles)
    evaluated.to_mesh_clear()
metadata = dict(blender=bpy.app.version_string, vertices=vertices, triangles=triangles,
    ortho_scale=4, render_size=1024, bottom=-.5, top=3.5, left=-2, right=2,
    design_bottom=.25, design_top=3.04, body_radius=.85, foot_half_extent=.76,
    height_compensation=HEIGHT, source='Authored mesh; approved study02 procedural materials and lights',
    preserve_runtime=['2x2 masonry footing .25 surface', 'branch at1.72 height, radius .82 socket',
                      'stem from3.04 to3.31', 'wheel at3.35', 'separate near/far painter ordering'],
    limitation='Static fixed-view tank-body bake. No foundation, branch, stem, wheel or ground shadow baked. Not installed or device reviewed.')
(OUT / 'projection.json').write_text(json.dumps(metadata, indent=2) + '\n')
print('RESERVOIR_COMPLETE', vertices, triangles)
