"""Editable metal-only stock station within the existing shelf.

Run with portable Blender4.5.9. Shared study02 materials/lights; no Unity writes.
Existing stock and masonry stay live. No floor/shadow catcher baked.
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtDirection/2026-10-11/loading-bay-assets-01/stock'
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
cut = iron_variant('Broad freshly sheared stock ends', (.115, .13, .15), (.23, .255, .28), .42)
scaled = iron_variant('Warm oxidised rolled surface', (.042, .033, .027), (.11, .073, .047), .72)
rack = iron_variant('Receiving cradle cast iron', (.019, .024, .031), (.057, .069, .084), .68)


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

# Billets are staged in a retaining cradle rather than a uniform rectangular pile.
for v in [20.86,21.7]:cube('Billet skid',(2.5,v,3.4,v+.16),.18,.29,cast,.014)
for level in range(3):
    for column in range(2 if level<2 else 1):
        u=2.56+column*.4+(0.18 if level==2 else 0)
        bottom=.29+level*.23;top=bottom+.23
        start=20.73+(.13 if column==1 else .055*level)
        end=22.04-(.19 if column==1 else .045*level)
        billet=section('Unequal received billet',[((u,start,u+.34,end),bottom),
            ((u,start,u+.34,end),top-.04),
            ((u+.035,start+.035,u+.305,end-.035),top)],scaled if column==1 else stock,.012)
        billet.data.materials.append(cut)
        # A cut end is one complete existing face, not a floating painted plate.
        billet.data.polygons[1].material_index=1
for u in [2.47,3.39]:
    cube('Billet cradle end stop',(u,21.20,u+.065,21.46),.23,1.14,cast,.012)
cube('Billet cradle transverse shoe',(2.47,21.2,3.455,21.46),.18,.26,cast,.018)

# Upright strip coils are held in two floor cradles. Their holes and round silhouette
# explain rolled feedstock at map scale; no tiny loose scraps are added to the court.
def coil(front,back,material):
    vertices=[];faces=[];segments=64
    for v,radius in [(front,.52),(back,.52),(front,.23),(back,.23)]:
        for i in range(segments):
            a=i*math.tau/segments
            vertices.append(point(4.03+math.cos(a)*radius,v,.76+math.sin(a)*radius/HEIGHT))
    for i in range(segments):
        j=(i+1)%segments
        faces.extend([(i,j,segments+j,segments+i),
            (2*segments+i,3*segments+i,3*segments+j,2*segments+j),
            (i,2*segments+i,2*segments+j,j),
            (segments+i,segments+j,3*segments+j,3*segments+i)])
    mesh=bpy.data.meshes.new('Rolled strip coil');mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new('Rolled strip coil',mesh);bpy.context.collection.objects.link(obj)
    finish(obj,'Rolled strip coil',material,.009)
    obj.data.materials.append(cut)
    for index,polygon in enumerate(mesh.polygons):
        if index%4==2:polygon.material_index=1
    for radius in [.30,.37,.44]:
        bpy.ops.mesh.primitive_torus_add(major_segments=64,minor_segments=6,
            location=point(4.03,front-.002,.76),rotation=(math.pi/2,0,0),major_radius=radius,minor_radius=.003)
        finish(bpy.context.object,'Quiet wound strip seam',cast)
for index,front in enumerate([21.03,21.46]):
    # Narrow block cradles touch the coil below its lower quadrants.
    for u in [3.67,4.23]:cube('Coil cradle bearing',(u,front-.035,u+.16,front+.255),.18,.395,cast,.022)
    coil(front,front+.22,stock if index==0 else scaled)

# Six staggered hollow lengths make the receiving cradle visibly loaded. Actual
# freshly cut annuli contrast with the broad mill-scale tube bodies.
def pipe(start,v,height,end,material):
    vertices=[];faces=[];n=32
    for u,radius in [(start,.13),(end,.13),(start,.095),(end,.095)]:
        for i in range(n):
            a=i*math.tau/n
            vertices.append(point(u,v+math.cos(a)*radius,height+math.sin(a)*radius/HEIGHT))
    for i in range(n):
        j=(i+1)%n
        faces.extend([(i,j,n+j,n+i),(2*n+i,3*n+i,3*n+j,2*n+j),
                      (i,2*n+i,2*n+j,j),(n+i,n+j,3*n+j,3*n+i)])
    mesh=bpy.data.meshes.new('Hollow stored pipe');mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new('Hollow stored pipe',mesh);bpy.context.collection.objects.link(obj)
    finish(obj,'Hollow stored pipe',material,.004)
    mesh.materials.append(cut)
    mesh.materials.append(soot)
    for index,polygon in enumerate(mesh.polygons):
        polygon.material_index=1 if index%4 in (2,3) else (2 if index%4==1 else 0)
    for poly in mesh.polygons:poly.use_smooth=True
for u in [2.53,4.53]:
    cube('Receiving cradle broad sole',(u-.08,18.76,u+.34,20.08),.18,.25,rack,.025)
    cube('Receiving cradle cast crossmember',(u,18.82,u+.26,20.02),.25,.39,rack,.025)
    for v in [18.82,19.83]:
        cube('Receiving cradle raised retaining cheek',(u,v,u+.26,v+.12),.35,1.09,rack,.028)
        section('Receiving cradle integral haunch',[
            ((u-.075,v-.025,u+.335,v+.145),.25),
            ((u+.035,v+.015,u+.225,v+.105),.68)],rack,.015)
    # Upper bearing faces belong to the same supporting crossmember.
    cube('Cradle worn bearing seat',(u-.005,18.96,u+.265,19.80),.377,.395,steel,.007)
for start,v,end,material in [(2.21,19.04,5.08,stock),(2.37,19.34,4.93,scaled),(2.28,19.64,5.00,stock)]:
    pipe(start,v,.507,end,material)
upper_pipe_height=.503+math.sqrt(.26**2-.15**2)/HEIGHT
for start,v,end,material in [(2.31,19.19,5.02,scaled),(2.43,19.49,4.88,stock)]:
    pipe(start,v,upper_pipe_height+.004,end,material)
pipe(2.23,19.34,upper_pipe_height+.004+math.sqrt(.26**2-.15**2)/HEIGHT,4.96,stock)

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
scene.render.resolution_percentage=100;scene.render.film_transparent=True
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
scene.render.filepath=str(OUT/'stock-station.png')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'stock-station.blend'))
bpy.ops.render.render(write_still=True)
deps=bpy.context.evaluated_depsgraph_get();vertices=triangles=0;bounds=[]
for obj in scene.objects:
    if obj.type!='MESH':continue
    evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
    vertices+=len(mesh.vertices);triangles+=len(mesh.loop_triangles)
    bounds.extend([obj.matrix_world@vert.co for vert in mesh.vertices]);evaluated.to_mesh_clear()
physical=[min(p.x for p in bounds)+ANCHOR[0],min(p.y for p in bounds)+ANCHOR[1],
          max(p.x for p in bounds)+ANCHOR[0],max(p.y for p in bounds)+ANCHOR[1]]
height_range=[min(p.z for p in bounds)/HEIGHT,max(p.z for p in bounds)/HEIGHT]
assert physical[0]>=2 and physical[1]>=18.5 and physical[2]<=7 and physical[3]<=22.4,physical
assert height_range[0]>=.15 and height_range[1]<=1.6,height_range
screen=[world_to_camera_view(scene,camera,p) for p in bounds]
projected=[min(p.x for p in screen),min(p.y for p in screen),max(p.x for p in screen),max(p.y for p in screen)]
assert min(projected)>=0 and max(projected)<=1,projected
metadata=dict(blender=bpy.app.version_string,vertices=vertices,triangles=triangles,
    anchor=list(ANCHOR),ortho_scale=6.3,left=-3.15,right=3.15,bottom=-2.45,top=3.85,
    render_size=1024,height_compensation=HEIGHT,depth_footprint=[2,18.5,7,22.4],depth_height=1.6,
    equipment_bounds=physical,height_range=height_range,projected_normalized_bounds=projected,
    included=['quench tank and still water','five unequal billets with sheared end faces','two wound strip coils with cut faces and distinct broad mill scale','six staggered hollow pipes in a supported receiving cradle','raised retaining cheeks and integral haunches','tool rack'],
    preserved=['wood barrel and crate','crane','hoist','masonry shelf','walking space and camera'],
    source='Authored geometry; existing study02 procedural materials and lighting',
    limitation='Fixed-view metal-only transparent render; no floor or shadow catcher; Unity acceptance separate.')
(OUT/'projection.json').write_text(json.dumps(metadata,indent=2)+'\n')
print('STOCK_STATION_COMPLETE',vertices,triangles)
