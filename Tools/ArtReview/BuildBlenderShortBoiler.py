"""Low foundry retort; loads the approved study02 material/light scene, replaces its meshes.
Run with portable Blender4.5.9. No Unity files are modified.
"""
import bpy, math, json
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtDirection/2026-10-09/short-boiler-01'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend'))
for o in list(bpy.data.objects):
    if o.type=='MESH':bpy.data.objects.remove(o,do_unlink=True)
HEIGHT=1/math.cos(math.radians(30))
stone=bpy.data.materials['Cool weathered basalt']
arch_stone=bpy.data.materials['Dressed arch basalt']
iron=bpy.data.materials['Forged blue iron']
bronze=bpy.data.materials['Rubbed old bronze']
soot=bpy.data.materials['Soot dark recess']
fire=bpy.data.materials['Contained forge heat']
def finish(obj, name, mat, bevel=0):
    obj.name = name
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new('Light catching dressed edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 2
        mod = obj.modifiers.new('Weighted face normals', 'WEIGHTED_NORMAL')
        mod.keep_sharp = True
    return obj


def cylinder(name, radius, low, high, mat, vertices=32, upper=None, bevel=.035):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius,
        radius2=radius if upper is None else upper,
        depth=(high-low)*HEIGHT, location=(0,0,(low+high)*.5*HEIGHT))
    return finish(bpy.context.object,name,mat,bevel)


def cube(name, location, size, mat, angle=0, bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=(location[0],location[1],location[2]*HEIGHT))
    o = bpy.context.object
    o.scale = (size[0],size[1],size[2]*HEIGHT)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    o.rotation_euler.z = angle
    return finish(o,name,mat,bevel)


def sector(name, inner, outer, low, high, start, end, mat):
    verts=[]
    steps=3
    for z in [low, high]:
        for r in [inner, outer]:
            for i in range(steps+1):
                a=start+(end-start)*i/steps
                verts.append((r*math.cos(a),r*math.sin(a),z*HEIGHT))
    faces=[]; k=steps+1
    for i in range(steps):
        faces.extend([(i,i+1,k+i+1,k+i),
            (2*k+i,3*k+i,3*k+i+1,2*k+i+1),
            (i,2*k+i,2*k+i+1,i+1),
            (k+i,k+i+1,3*k+i+1,3*k+i)])
    faces.extend([(0,k,3*k,2*k),(steps,2*k+steps,3*k+steps,k+steps)])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(verts,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    return finish(o,name,mat,.025)


def facade_piece(name, angle, radius, profile, depth, mat, bevel=.018):
    """Extrude a tangent-plane x/z polygon; authored arches, no external textures."""
    normal=Vector((math.cos(angle),math.sin(angle)))
    tangent=Vector((-math.sin(angle),math.cos(angle)))
    vertices=[]
    for r in [radius-depth*.5,radius+depth*.5]:
        for x,z in profile:
            p=normal*r+tangent*x
            vertices.append((p.x,p.y,z*HEIGHT))
    count=len(profile)
    faces=[tuple(reversed(range(count))),tuple(range(count,2*count))]
    faces += [(i,(i+1)%count,(i+1)%count+count,i+count) for i in range(count)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    o=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(o)
    return finish(o,name,mat,bevel)


def arch_profile(radius, bottom, spring):
    return [(-radius,bottom),(radius,bottom)] + [
        (radius*math.cos(i*math.pi/20),spring+radius*math.sin(i*math.pi/20))
        for i in range(21)]


# Low 2.28-unit silhouette preserves the existing foreground visibility envelope.
cylinder('Basalt footing',2,0,.30,stone,24,bevel=.045)
cylinder('Soot drum',1.77,.30,1.30,soot)
for row in range(2):
    for i in range(12):
        a=(i+(row%2)*.5)*math.tau/12
        sector('Dressed low masonry',1.68,1.86,.32+row*.46,.75+row*.46,a+.014,a+math.tau/12-.014,stone)
cylinder('Low stone cornice',1.92,1.30,1.45,arch_stone,24)
cylinder('Bronze lid rim',1.64,1.45,1.55,bronze,40)
cylinder('Pressed low lid',1.54,1.55,1.78,iron,48,1.12)
cylinder('Lid shoulder',1.12,1.78,1.93,iron,48,.64)
cylinder('Lid crown',.64,1.93,2.0,iron,40,.34)
cylinder('Short exhaust collar',.28,1.99,2.08,bronze,24)
cylinder('Short exhaust',.20,2.06,2.25,iron,24)
cylinder('Exhaust lip',.255,2.23,2.28,iron,24)
cylinder('Dark exhaust mouth',.195,2.28,2.281,soot,24,bevel=0)
for i in range(16):
    a=i*math.tau/16
    bpy.ops.mesh.primitive_uv_sphere_add(segments=8,ring_count=4,radius=.037,location=(1.642*math.cos(a),1.642*math.sin(a),1.5*HEIGHT))
    finish(bpy.context.object,'Lid rivet',bronze)
# Broad low segmental doors: distinct from the tall furnace's upright arches.
for a in [math.radians(195),math.radians(255)]:
    profile=arch_profile(.62,.38,.61)
    # Reduce arch rise to retain a squat fire opening.
    profile=[(x,.61+(z-.61)*.65 if z>.61 else z) for x,z in profile]
    facade_piece('Low arched recess',a,1.96,profile,.10,soot)
    heat=[(x*.89,.4+(z-.38)*.91) for x,z in profile]
    facade_piece('Contained retort coals',a,2.02,heat,.01,fire,0)
    for j in range(9):
        lo=j*math.pi/9+.014;hi=(j+1)*math.pi/9-.014
        pts=[(.62*math.cos(lo),.61+.403*math.sin(lo)),(.78*math.cos(lo),.61+.53*math.sin(lo)),(.78*math.cos(hi),.61+.53*math.sin(hi)),(.62*math.cos(hi),.61+.403*math.sin(hi))]
        facade_piece('Segmental arch stone',a,2.01,pts,.20,arch_stone)
    for sign in [-1,1]:
        x=sign*.70
        facade_piece('Short jamb',a,2.01,[(x-.075,.36),(x+.075,.36),(x+.075,.60),(x-.075,.60)],.20,arch_stone)
    for j in range(7):
        x=(j-3)*.16;top=.61+.65*math.sqrt(max(0,.55**2-x*x))
        facade_piece('Retort grate',a,2.08,[(x-.022,.40),(x+.022,.40),(x+.022,top),(x-.022,top)],.065,iron,.008)
    facade_piece('Firebox sill',a,2.025,[(-.81,.31),(.81,.31),(.81,.39),(-.81,.39)],.28,stone)
scene=bpy.context.scene
camera=scene.camera
target=Vector((0,0,1.14*HEIGHT))
camera.location=target+Vector((-12,-12,math.sqrt(96)))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.ortho_scale=6
scene.render.resolution_x=1024;scene.render.resolution_y=1024
scene.render.filepath=str(OUT/'short-boiler.png')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'short-boiler.blend'))
bpy.ops.render.render(write_still=True)
deps=bpy.context.evaluated_depsgraph_get();verts=tris=0
for o in scene.objects:
    if o.type!='MESH':continue
    evaluated=o.evaluated_get(deps);m=evaluated.to_mesh();m.calc_loop_triangles()
    verts+=len(m.vertices);tris+=len(m.loop_triangles);evaluated.to_mesh_clear()
(OUT/'model-budget.json').write_text(json.dumps(dict(vertices=verts,triangles=tris,ortho_scale=6,bottom=-1.86,top=4.14,design_height=2.28,source='Authored Blender geometry; shared study02 procedural materials and lights'),indent=2))
print('SHORT_BOILER_COMPLETE',verts,tris)
