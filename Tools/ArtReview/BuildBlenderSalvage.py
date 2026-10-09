"""Editable metal-only salvage workstation in the existing foundry footprints.

Run with portable Blender4.5.9. Shared study02 materials/lights; no Unity writes.
Masonry shelf and anvil bedding stay live geometry. No floor/shadow catcher baked.
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtDirection/2026-10-09/salvage-kit-01'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend'))
for obj in list(bpy.data.objects):
    if obj.type == 'MESH':
        bpy.data.objects.remove(obj, do_unlink=True)
HEIGHT = 1 / math.cos(math.radians(30))
ANCHOR = (-26.725, 6.7)
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


# Existing anvil metal foot stays at .48 above the separately rendered stone bedding.
cube('Broad anvil foot',(-28.32,5.86,-27.38,7.32),.48,.63,cast,.04)
section('Continuous forged anvil waist',[
    ((-28.19,6.05,-27.51,7.13),.63),
    ((-28.075,6.23,-27.625,6.97),.79),
    ((-28.06,6.28,-27.64,6.92),.85),
    ((-28.105,6.16,-27.595,7.01),.95),
    ((-28.25,5.80,-27.45,7.15),1.08)],cast,.035)
head = cube('Worked steel anvil heel',(-28.25,5.58,-27.45,7.15),1.08,1.28,steel,.018)
# A real square hardy hole is inset through the heel, not painted emissive detail.
cutter = cube('Hardy hole cutter',(-27.99,5.77,-27.78,5.98),1.0,1.4,soot,0)
modifier = head.modifiers.new('Square hardy hole','BOOLEAN')
modifier.operation = 'DIFFERENCE'; modifier.object = cutter
bpy.context.view_layer.objects.active = head
bpy.ops.object.modifier_apply(modifier=modifier.name)
bpy.data.objects.remove(cutter,do_unlink=True)
cube('Hardy hole dark interior',(-27.985,5.775,-27.785,5.975),1.001,1.005,soot,0)

# Rounded horn shares the existing tip and projected silhouette; its elliptical
# section tapers into a slightly upturned point rather than a flat triangle.
vertices=[]; faces=[]; rings=6; segments=20
for ring in range(rings):
    t=ring/(rings-1)
    v=7.15+t*.88
    width=.35*(1-t)**.9+.008*t
    half_height=.10*(1-t)+.008*t
    center_height=1.18+.06*t
    for index in range(segments):
        angle=index*math.tau/segments
        vertices.append(point(-27.85+math.cos(angle)*width,v,center_height+math.sin(angle)*half_height))
for ring in range(rings-1):
    for i in range(segments):
        a=ring*segments+i;b=ring*segments+(i+1)%segments
        faces.append((a,b,b+segments,a+segments))
faces.extend([tuple(reversed(range(segments))),tuple(range((rings-1)*segments,rings*segments))])
mesh=bpy.data.meshes.new('Round forged horn');mesh.from_pydata(vertices,[],faces);mesh.update()
obj=bpy.data.objects.new('Round forged horn',mesh);bpy.context.collection.objects.link(obj)
finish(obj,'Round forged horn',steel,.01)
rod('Resting hammer handle',(-27.81,6.19,1.31),(-27.95,6.78,1.31),.027,bronze)
cube('Resting hammer head',(-28.13,6.64,-27.73,6.87),1.29,1.43,cast,.025)
cube('Hammer striking face',(-28.13,6.65,-28.10,6.86),1.30,1.42,steel,.008)

# A steel parts chest replaces the current atlas-framed box. The original low
# footprint, lid height and central dull latch remain; no handles extend off shelf.
cube('Parts chest shell',(-26.3,7.15,-24.85,8.05),.2,.88,cast,.035)
cube('Recessed front plate',(-26.18,7.146,-24.97,7.168),.32,.75,iron,.017)
cube('Recessed end plate',(-26.303,7.27,-26.278,7.93),.32,.75,iron,.014)
cube('Parts chest lid shadow',(-26.295,7.155,-24.855,8.045),.835,.87,soot,.014)
cube('Dressed chest lid',(-26.3,7.15,-24.85,8.05),.87,.911,iron,.014)
for u in [-26.03,-25.12]:
    cube('Iron chest front strap',(u-.035,7.145,u+.035,7.173),.23,.9,steel,.008)
    cube('Iron chest lid strap',(u-.035,7.17,u+.035,8.03),.911,.92,steel,.003)
cube('Quiet bronze latch',(-25.64,7.14,-25.51,7.18),.65,.82,bronze,.012)
cube('Latch slot',(-25.59,7.135,-25.56,7.142),.685,.755,soot,.003)

# Five original ingots in two staggered courses. Broad sloped shoulders and mild
# scale variation distinguish solid cast stock from a stack of sheet-metal boxes.
for level in range(2):
    for row in range(3-level):
        u0=-26.35+row*.51+level*.24;u1=u0+.44
        bottom=.2+level*.22;top=.41+level*.22
        section('Stacked cast ingot',[
            ((u0,5.35,u1,6.85),bottom),
            ((u0,5.35,u1,6.85),top-.065),
            ((u0+.055,5.405,u1-.055,6.795),top)],stock,.016)

scene=bpy.context.scene
camera=scene.camera
target=Vector((0,0,1*HEIGHT))
camera.location=target+Vector((-12,-12,math.sqrt(96)))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.ortho_scale=5.5
scene.render.resolution_x=1024;scene.render.resolution_y=1024
scene.render.filepath=str(OUT/'salvage-kit.png')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'salvage-kit.blend'))
bpy.ops.render.render(write_still=True)
deps=bpy.context.evaluated_depsgraph_get();vertices=triangles=0
for obj in scene.objects:
    if obj.type!='MESH':continue
    evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
    vertices+=len(mesh.vertices);triangles+=len(mesh.loop_triangles);evaluated.to_mesh_clear()
metadata=dict(blender=bpy.app.version_string,vertices=vertices,triangles=triangles,
    anchor=list(ANCHOR),ortho_scale=5.5,left=-2.75,right=2.75,bottom=-1.75,top=3.75,
    render_size=1024,height_compensation=HEIGHT,design_top=1.43,
    depth_footprint=[-29,5,-24.45,8.4],depth_height=1.6,
    included=['anvil metal and resting hammer','steel parts chest','five cast ingots'],
    preserve_runtime=['masonry salvage shelf','anvil stone bedding','nearby barrels/crates/sack','all walkable space'],
    source='Authored geometry and study02 procedural materials/lights',
    limitation='Fixed-view metal-only art candidate; no Unity integration/device acceptance. No floor or shadow catcher.')
(OUT/'projection.json').write_text(json.dumps(metadata,indent=2)+'\n')
print('SALVAGE_KIT_COMPLETE',vertices,triangles)
