"""Editable stock hoist, matching the existing rear-shelf assembly exactly.

Run with portable Blender4.5.9. Shared study02 materials/lights; no Unity writes.
Existing stock and masonry stay live. No floor/shadow catcher baked.
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtDirection/2026-10-10/stock-hoist-01'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend'))
for obj in list(bpy.data.objects):
    if obj.type == 'MESH':
        bpy.data.objects.remove(obj, do_unlink=True)
HEIGHT = 1 / math.cos(math.radians(30))
ANCHOR = (4.5, 22.08)
iron = bpy.data.materials['Forged blue iron']
bronze = bpy.data.materials['Rubbed old bronze']
soot = bpy.data.materials['Soot dark recess']


def iron_variant(name, dark, light, roughness):
    material = iron.copy()
    material.name = name
    nodes = material.node_tree.nodes
    nodes.get('Principled BSDF').inputs['Roughness'].default_value = roughness
    ramp = next(node for node in nodes if node.type == 'VALTORGB')
    ramp.color_ramp.elements[0].color = (*dark, 1)
    ramp.color_ramp.elements[1].color = (*light, 1)
    return material


steel = iron_variant('Worked steel face', (.075, .085, .10), (.16, .18, .21), .37)
for node in steel.node_tree.nodes:
    if node.type == 'BUMP':
        node.inputs['Distance'].default_value = .007
        node.inputs['Strength'].default_value = .12
cast = iron_variant('Dark forged casting', (.018, .023, .032), (.046, .056, .071), .60)
stock = iron_variant('Mill scale cast stock', (.043, .052, .065), (.10, .118, .14), .57)


def point(u, v, z):
    return (u-ANCHOR[0], v-ANCHOR[1], z*HEIGHT)


def finish(obj, name, material, bevel=0):
    obj.name = name
    obj.data.materials.append(material)
    if bevel:
        modifier = obj.modifiers.new('Forged edge light', 'BEVEL')
        modifier.width = bevel
        modifier.segments = 3
        modifier = obj.modifiers.new('Weighted face normals', 'WEIGHTED_NORMAL')
        modifier.keep_sharp = True
    return obj


def cube(name, box, bottom, top, material, bevel=.014):
    u0, v0, u1, v1 = box
    bpy.ops.mesh.primitive_cube_add(size=1, location=point((u0+u1)*.5, (v0+v1)*.5, (bottom+top)*.5))
    obj = bpy.context.object
    obj.scale = (u1-u0, v1-v0, (top-bottom)*HEIGHT)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, material, bevel)


def section(name, courses, material, bevel=.025):
    vertices = []
    for box, height in courses:
        u0, v0, u1, v1 = box
        vertices.extend([point(u0,v0,height),point(u1,v0,height),point(u1,v1,height),point(u0,v1,height)])
    faces = [(3,2,1,0)]
    for level in range(len(courses)-1):
        for i in range(4):
            a=level*4+i; b=level*4+(i+1)%4
            faces.append((a,b,b+4,a+4))
    top = (len(courses)-1)*4
    faces.append(tuple(range(top,top+4)))
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    return finish(obj,name,material,bevel)


def rod(name, a, b, radius, material):
    a, b = Vector(point(*a)), Vector(point(*b))
    direction = b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=16,radius=radius,depth=direction.length,location=(a+b)*.5)
    obj = bpy.context.object
    obj.rotation_euler = direction.to_track_quat('Z','Y').to_euler()
    return finish(obj,name,material,.008)


for u in [2.3, 6.7]:
    cube('Bearing foot',(u-.2,22.13,u+.2,22.38),.18,.38,cast,.02)
    cube('Rolled upright',(u-.12,22.17,u+.12,22.35),.38,3.08,iron,.022)
    cube('Head seat',(u-.18,22.13,u+.18,22.38),2.86,3.12,cast,.025)
cube('Rolled crosshead web',(2.08,22.17,6.92,22.32),3.03,3.38,iron,.014)
cube('Crosshead top flange',(2.05,22.10,6.95,22.40),3.32,3.44,stock,.02)
cube('Crosshead lower flange',(2.08,22.13,6.92,22.37),3.03,3.11,stock,.016)
rod('Left forged knee',(2.3,22.26,2.35),(3.15,22.26,3.05),.075,cast)
rod('Right forged knee',(6.7,22.26,2.35),(5.85,22.26,3.05),.075,cast)
for u in [3.485,5.515]:
    cube('Rounded bearing housing',(u-.165,21.95,u+.165,22.32),2.38,3.04,cast,.065)
    rod('Bronze bearing bush',(u-.18,22.08,2.66),(u+.18,22.08,2.66),.145,bronze)
rod('Winding drum axle',(3.4,22.08,2.66),(5.6,22.08,2.66),.095,steel)
rod('Cast drum barrel',(3.57,22.08,2.66),(5.43,22.08,2.66),.255,cast)
for u in [3.63,5.37]:
    rod('Drum retaining flange',(u-.045,22.08,2.66),(u+.045,22.08,2.66),.34,iron)
# A continuous wound cable makes shallow rounded grooves instead of black bars.
curve=bpy.data.curves.new('Wound lifting cable','CURVE');curve.dimensions='3D'
curve.bevel_depth=.023;curve.bevel_resolution=2
spline=curve.splines.new('POLY');n=641;spline.points.add(n-1)
for i,p in enumerate(spline.points):
    t=i/(n-1);angle=t*math.tau*20
    x,y,z=point(3.72+t*1.56,22.08+math.cos(angle)*.274,2.66+math.sin(angle)*.274/HEIGHT)
    p.co=(x,y,z,1)
obj=bpy.data.objects.new('Wound lifting cable',curve);bpy.context.collection.objects.link(obj)
obj.data.materials.append(stock)
# Real linked oval chain, preserving the old drop endpoints and quiet dark finish.
for u in [3.8,5.15]:
    for i in range(8):
        z=2.32-i*.145
        bpy.ops.mesh.primitive_torus_add(major_segments=16,minor_segments=6,
            location=point(u,22.04,z),major_radius=.064,minor_radius=.013)
        obj=bpy.context.object;obj.scale=(.67,1.25,1)
        obj.rotation_euler=(math.pi/2,0,math.pi/2 if i%2 else 0)
        finish(obj,'Forged hanging chain',stock)
cube('Lifting yoke',(3.62,21.93,5.33,22.15),1.0,1.17,iron,.032)
for u in [3.8,5.15]:
    cube('Yoke fastening strap',(u-.06,21.915,u+.06,22.165),1.03,1.20,bronze,.01)

scene=bpy.context.scene;camera=scene.camera
target=Vector((0,0,1.8*HEIGHT))
camera.location=target+Vector((-12,-12,math.sqrt(96)))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.ortho_scale=5.4
scene.render.resolution_x=1024;scene.render.resolution_y=1024
scene.render.filepath=str(OUT/'stock-hoist.png')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'stock-hoist.blend'))
bpy.ops.render.render(write_still=True)
metadata=dict(anchor=list(ANCHOR),ortho_scale=5.4,left=-2.7,right=2.7,
    bottom=-.9,top=4.5,render_size=1024,height_compensation=HEIGHT,
    depth_footprint=[2,18.5,7,22.4],depth_height=3.6,
    source='Authored geometry and study02 procedural materials/lights',
    preserved=['shelf footprint','3.44 crosshead height','stock and tank','movement and camera'],
    limitation='Fixed-view transparent hoist-only bake; runtime and device acceptance separate.')
(OUT/'projection.json').write_text(json.dumps(metadata,indent=2)+'\n')
print('STOCK_HOIST_COMPLETE')
