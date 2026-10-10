"""Editable metal-only stock station within the existing shelf.

Run with portable Blender4.5.9. Shared study02 materials/lights; no Unity writes.
Existing stock and masonry stay live. No floor/shadow catcher baked.
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtDirection/2026-10-10/stock-station-01'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend'))
for obj in list(bpy.data.objects):
    if obj.type == 'MESH':
        bpy.data.objects.remove(obj, do_unlink=True)
HEIGHT = 1 / math.cos(math.radians(30))
ANCHOR = (4.5, 20.45)
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


# Open quenching tank: four real walls around quiet recessed liquid, no glow.
for u in [5.4,6.4]:
    cube('Tank skid',(u,20.75,u+.28,22.05),.18,.32,cast,.018)
for box in [(5.3,21.94,6.8,22.1),(6.64,20.7,6.8,21.94),
            (5.3,20.7,6.8,20.86),(5.3,20.86,5.46,21.94)]:
    cube('Welded quench shell',box,.32,.85,iron,.022)
water=iron_variant('Still quench liquid',(.009,.018,.024),(.025,.039,.046),.22)
for node in water.node_tree.nodes:
    if node.type=='BUMP':node.inputs['Strength'].default_value=.04
cube('Recessed quench water',(5.46,20.86,6.64,21.94),.56,.58,water,.002)
for u in [5.48,6.48]:cube('Tank welded rib',(u,20.695,u+.12,20.76),.34,.80,stock,.009)
for v in [21.02,21.65]:cube('Tank end rib',(5.295,v,5.36,v+.12),.34,.80,stock,.009)

# Six original billets on two skids; same stacked arrangement and total height.
for v in [20.86,21.7]:cube('Billet skid',(2.5,v,3.4,v+.16),.18,.29,cast,.014)
for level in range(3):
    for column in range(2):
        u=2.56+column*.4;bottom=.29+level*.17;top=.44+level*.17
        section('Rolled billet',[((u,20.78,u+.34,21.98),bottom),
            ((u,20.78,u+.34,21.98),top-.025),
            ((u+.025,20.805,u+.315,21.955),top)],stock,.008)

# Two flat strip coils with an actual centre bore and restrained concentric seams.
def coil(bottom,top):
    vertices=[];faces=[];segments=64
    for z,radius in [(bottom,.52),(top,.52),(bottom,.23),(top,.23)]:
        for i in range(segments):
            a=i*math.tau/segments
            vertices.append(point(4.03+math.cos(a)*radius,21.35+math.sin(a)*radius,z))
    for i in range(segments):
        j=(i+1)%segments
        faces.extend([(i,j,segments+j,segments+i),
            (2*segments+i,3*segments+i,3*segments+j,2*segments+j),
            (segments+i,segments+j,3*segments+j,3*segments+i)])
    mesh=bpy.data.meshes.new('Rolled strip coil');mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new('Rolled strip coil',mesh);bpy.context.collection.objects.link(obj)
    finish(obj,'Rolled strip coil',stock,.009)
    for radius in [.30,.37,.44]:
        bpy.ops.mesh.primitive_torus_add(major_segments=64,minor_segments=6,
            location=point(4.03,21.35,top+.001),major_radius=radius,minor_radius=.003)
        finish(bpy.context.object,'Quiet wound strip seam',cast)
for layer in range(2):coil(.18+layer*.16,.32+layer*.16)

# Three hollow stock pipes. The open cut rim is continuous, not a painted cap.
def pipe(v):
    vertices=[];faces=[];n=32
    for u,radius in [(2.3,.095),(5,.095),(2.3,.067),(5,.067)]:
        for i in range(n):
            a=i*math.tau/n
            vertices.append(point(u,v+math.cos(a)*radius,.58+math.sin(a)*radius/HEIGHT))
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),
                      (i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
    mesh=bpy.data.meshes.new('Hollow stored pipe');mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new('Hollow stored pipe',mesh);bpy.context.collection.objects.link(obj)
    finish(obj,'Hollow stored pipe',iron,.004)
    for poly in mesh.polygons:poly.use_smooth=True
for u in [2.5,4.55]:cube('Pipe saddle',(u,18.78,u+.25,20.03),.18,.43,cast,.015)
for v in [19,19.4,19.8]:pipe(v)
for u in [2.7,4.6]:rod('Pipe retaining strap',(u,18.94,.70),(u,19.86,.70),.0275,stock)

# Tool rack retains its footprint and recognizable tongs/hammer arrangement.
for u in [5.65,6.65]:
    rod('Rack upright',(u,19.8,.2),(u,19.8,1.3),.035,iron)
    rod('Rack foot',(u,19.5,.2),(u,20.05,.2),.04,cast)
rod('Rack crossbar',(5.65,19.8,1.24),(6.65,19.8,1.24),.045,iron)
for u in [5.9,6.35]:
    for s in [-1,1]:
        rod('Tong handle',(u+s*.04,19.78,1.20),(u-s*.018,19.78,.57),.0175,steel)
        rod('Tong jaw',(u,19.78,.57),(u+s*.085,19.78,.41),.02,steel)
        rod('Tong jaw tip',(u+s*.085,19.78,.41),(u+s*.045,19.78,.38),.018,steel)
rod('Rack hammer handle',(6.12,19.8,1.23),(6.12,19.8,.73),.0275,bronze)
cube('Rack hammer head',(5.97,19.7,6.27,19.9),.62,.78,cast,.022)

scene=bpy.context.scene;camera=scene.camera
target=Vector((0,0,.7*HEIGHT))
camera.location=target+Vector((-12,-12,math.sqrt(96)))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.ortho_scale=6.3
scene.render.resolution_x=1024;scene.render.resolution_y=1024
scene.render.filepath=str(OUT/'stock-station.png')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'stock-station.blend'))
bpy.ops.render.render(write_still=True)
deps=bpy.context.evaluated_depsgraph_get();vertices=triangles=0
for obj in scene.objects:
    if obj.type!='MESH':continue
    evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
    vertices+=len(mesh.vertices);triangles+=len(mesh.loop_triangles);evaluated.to_mesh_clear()
metadata=dict(blender=bpy.app.version_string,vertices=vertices,triangles=triangles,
    anchor=list(ANCHOR),ortho_scale=6.3,left=-3.15,right=3.15,bottom=-2.45,top=3.85,
    render_size=1024,height_compensation=HEIGHT,depth_footprint=[2,18.5,7,22.4],depth_height=1.6,
    included=['quench tank and still water','six billets','two strip coils','three hollow pipes','tool rack'],
    preserved=['wood barrel and crate','crane','hoist','masonry shelf','walking space and camera'],
    source='Authored geometry; existing study02 procedural materials and lighting',
    limitation='Fixed-view metal-only transparent render; no floor or shadow catcher; Unity acceptance separate.')
(OUT/'projection.json').write_text(json.dumps(metadata,indent=2)+'\n')
print('STOCK_STATION_COMPLETE',vertices,triangles)
