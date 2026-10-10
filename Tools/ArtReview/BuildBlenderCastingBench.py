"""Editable moulding bench and screw press on the existing casting shelf.

Run with portable Blender4.5.9. Shared study02 materials/lights; no Unity writes.
Existing casting masonry stays live. No floor/shadow catcher baked.
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtDirection/2026-10-10/casting-bench-01'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend'))
for obj in list(bpy.data.objects):
    if obj.type == 'MESH':
        bpy.data.objects.remove(obj, do_unlink=True)
HEIGHT = 1 / math.cos(math.radians(30))
ANCHOR = (-15.5, 20.75)
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


def surface(name, dark, light, scale, stretch, roughness):
    mat = bpy.data.materials.new(name); mat.use_nodes = True
    nodes=mat.node_tree.nodes; links=mat.node_tree.links
    bsdf=nodes.get('Principled BSDF'); bsdf.inputs['Roughness'].default_value=roughness
    tex=nodes.new('ShaderNodeTexNoise'); tex.inputs['Scale'].default_value=scale
    tex.inputs['Detail'].default_value=3
    coord=nodes.new('ShaderNodeTexCoord'); mapping=nodes.new('ShaderNodeVectorMath')
    mapping.operation='MULTIPLY'; mapping.inputs[1].default_value=stretch
    links.new(coord.outputs['Generated'],mapping.inputs[0]); links.new(mapping.outputs[0],tex.inputs['Vector'])
    ramp=nodes.new('ShaderNodeValToRGB'); ramp.color_ramp.elements[0].color=(*dark,1)
    ramp.color_ramp.elements[1].color=(*light,1)
    links.new(tex.outputs['Fac'],ramp.inputs[0]); links.new(ramp.outputs['Color'],bsdf.inputs['Base Color'])
    bump=nodes.new('ShaderNodeBump'); bump.inputs['Strength'].default_value=.16
    bump.inputs['Distance'].default_value=.012
    links.new(tex.outputs['Fac'],bump.inputs['Height']); links.new(bump.outputs['Normal'],bsdf.inputs['Normal'])
    return mat


wood=surface('Worn warm moulding oak',(.070,.037,.021),(.24,.145,.073),5,(.30,5,5),.76)
sand=surface('Compacted ochre casting sand',(.16,.105,.052),(.34,.25,.13),85,(1,1,1),.95)

# A low, genuinely supported workbench. Gaps between the planks retain volume.
for u in [-17.38,-13.77]:
    for v in [19.56,20.46]:
        cube('Oak bench leg',(u,v,u+.18,v+.18),.18,.82,wood,.018)
    cube('Mortised lower stretcher',(u,19.52,u+.18,20.67),.35,.48,wood,.012)
for v in [19.55,20.5]:
    cube('Long lower stretcher',(-17.34,v,-13.56,v+.12),.39,.51,wood,.012)
for i in range(4):
    v=19.43+i*.31
    cube('Individual worn bench plank',(-17.56,v,-13.42,v+.30),.81,.97,wood,.018)
for u in [-17.4,-13.67]:
    cube('Forged bench end strap',(u,19.425,u+.075,20.665),.955,.987,cast,.006)
    for v in [19.6,20.49]:
        rod('Bench strap rivet',(u+.037,v,.98),(u+.037,v,1.012),.032,steel)

# Compact hand screw/clamp press, wholly in the rear western equipment zone.
cube('Press base',(-17.49,21.02,-16.24,22.07),.18,.34,cast,.035)
cube('Press lower platen',(-17.22,21.19,-16.49,21.85),.34,.59,stock,.025)
for u in [-17.4,-16.47]:
    cube('Press cast upright',(u,21.55,u+.18,21.8),.34,2.27,iron,.035)
    for z in [.43,2.10]:
        rod('Press frame bronze pin',(u+.09,21.52,z),(u+.09,21.82,z),.055,bronze)
cube('Press arched shoulder',(-17.48,21.48,-16.22,21.89),2.11,2.34,iron,.075)
rod('Threaded clamp spindle',(-16.85,21.66,.94),(-16.85,21.66,2.43),.072,steel)
cube('Moving press shoe',(-17.16,21.3,-16.55,21.95),.88,1.07,stock,.038)
rod('Bronze threaded bearing',(-16.85,21.66,2.08),(-16.85,21.66,2.38),.14,bronze)
rod('Press hand lever',(-17.30,21.66,2.40),(-16.40,21.66,2.40),.033,stock)
for u in [-17.29,-16.41]:
    rod('Lever end grip',(u,21.66,2.34),(u,21.66,2.47),.062,wood)
curve=bpy.data.curves.new('Real helical spindle thread','CURVE'); curve.dimensions='3D'
curve.bevel_depth=.016; curve.bevel_resolution=2
poly=curve.splines.new('POLY'); poly.points.add(240)
for i,p in enumerate(poly.points):
    a=i/240*math.tau*13; z=1.09+i/240*.98
    p.co=(*point(-16.85+math.cos(a)*.076,21.66+math.sin(a)*.076,z),1)
obj=bpy.data.objects.new('Real helical spindle thread',curve); bpy.context.collection.objects.link(obj)
obj.data.materials.append(stock)
# A compact closed flask in the press is distinct from the open moulds.
cube('Clamped oak flask',(-17.13,21.29,-16.59,21.89),.60,.85,wood,.025)
for u in [-17.1,-16.67]:cube('Flask pressure strap',(u,21.27,u+.05,21.91),.64,.87,cast,.008)


def mould(name,u,v,width,length,bottom):
    top=bottom+.23; rim=.075
    # Sand is a real volume with a tapered ingot cavity removed by Boolean.
    body=cube(name+' packed sand',(u+rim,v+rim,u+width-rim,v+length-rim),bottom+.025,top-.023,sand,0)
    cavity=section(name+' ingot negative',[
        ((u+width*.40,v+length*.24,u+width*.60,v+length*.76),bottom+.085),
        ((u+width*.30,v+length*.14,u+width*.70,v+length*.86),top+.06)],soot,0)
    modifier=body.modifiers.new('Real tapered ingot impression','BOOLEAN'); modifier.operation='DIFFERENCE'
    modifier.object=cavity; bpy.context.view_layer.objects.active=body
    bpy.ops.object.modifier_apply(modifier=modifier.name); bpy.data.objects.remove(cavity,do_unlink=True)
    for box in [(u,v,u+width,v+rim),(u,v+length-rim,u+width,v+length),
                (u,v+rim,u+rim,v+length-rim),(u+width-rim,v+rim,u+width,v+length-rim)]:
        cube(name+' oak flask rail',box,bottom,top,wood,.012)
    for x in [u+.10,u+width-.15]:
        for y in [v,v+length-.035]:cube(name+' iron corner band',(x,y,x+.05,y+.035),bottom+.025,top+.006,cast,.004)
    for x in [u-.032,u+width+.032]:
        rod(name+' lifting lug',(x,v+length*.40,bottom+.12),(x,v+length*.60,bottom+.12),.028,bronze)


# Front-left removed half and two open working moulds: exposed cavities face camera.
removed_before=set(bpy.data.objects)
mould('Removed mould half',-17.17,19.66,.81,.84,.99)
# The removed half is set aside at a slight angle rather than becoming a third
# mechanically repeated working flask. Every component rotates as one assembly.
from mathutils import Matrix
centre=Vector(point(-17.17+.405,19.66+.42,.99))
rotation=Matrix.Translation(centre)@Matrix.Rotation(math.radians(-10),4,'Z')@Matrix.Translation(-centre)
for obj in set(bpy.data.objects)-removed_before:obj.matrix_world=rotation@obj.matrix_world
mould('Open ingot mould',-15.91,19.63,.72,.88,.99)
mould('Second open mould',-14.93,19.66,.64,.84,.99)
# Finished billets sit on their own short low rack at the east rear.
for v in [21.08,21.76]:cube('Billet rack runner',(-15.71,v,-13.52,v+.12),.18,.34,wood,.012)
for i in range(3):
    u=-15.56+i*.69
    section('Finished cooled billet',[
        ((u,20.94,u+.50,21.97),.34),
        ((u+.055,21.01,u+.445,21.9),.57)],stock,.028)
    cube('Billet identification notch',(u+.09,21.00,u+.17,21.04),.47,.50,soot,.003)
# Two simple tools on the free bench end, no scattered visual clutter.
rod('Sand rammer handle',(-13.98,19.74,1.04),(-13.98,20.29,1.04),.034,wood)
cube('Sand rammer head',(-14.1,20.26,-13.86,20.40),.99,1.14,iron,.021)
rod('Mould pick',(-14.25,19.6,1.02),(-13.56,19.6,1.02),.014,steel)

scene=bpy.context.scene; camera=scene.camera
target=Vector((0,0,1.0*HEIGHT))
camera.location=target+Vector((-12,-12,math.sqrt(96)))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.ortho_scale=7
scene.render.resolution_x=1024; scene.render.resolution_y=1024
scene.render.resolution_percentage=100; scene.render.film_transparent=True
scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'
scene.render.filepath=str(OUT/'casting-bench.png')
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'casting-bench.blend'))
bpy.ops.render.render(write_still=True)
deps=bpy.context.evaluated_depsgraph_get(); vertices=triangles=0; bounds=[]
for obj in scene.objects:
    if obj.type not in ('MESH','CURVE'):continue
    evaluated=obj.evaluated_get(deps); mesh=evaluated.to_mesh(); mesh.calc_loop_triangles()
    vertices+=len(mesh.vertices); triangles+=len(mesh.loop_triangles)
    bounds.extend([obj.matrix_world@vert.co for vert in mesh.vertices]); evaluated.to_mesh_clear()
physical=[min(p.x for p in bounds)+ANCHOR[0],min(p.y for p in bounds)+ANCHOR[1],
          max(p.x for p in bounds)+ANCHOR[0],max(p.y for p in bounds)+ANCHOR[1]]
height_range=[min(p.z for p in bounds)/HEIGHT,max(p.z for p in bounds)/HEIGHT]
assert physical[0]>=-17.7 and physical[1]>=19.4 and physical[2]<=-13.35 and physical[3]<=22.1,physical
assert height_range[0]>=.1799 and height_range[1]<=2.5,height_range
metadata=dict(blender=bpy.app.version_string,vertices=vertices,triangles=triangles,
    anchor=list(ANCHOR),ortho_scale=7,left=-3.5,right=3.5,bottom=-2.5,top=4.5,
    render_size=1024,height_compensation=HEIGHT,camera_direction=[-12,-12,math.sqrt(96)],
    vertical_target=1.0,depth_footprint=[-18,19.1,-13,22.4],depth_height=2.5,
    equipment_bounds=physical,height_range=height_range,
    included=['oak moulding bench','rear hand screw press','three open sand moulds with real tapered cavities','three finished billets','rammer and mould pick'],
    source='Native authored 3D geometry; existing study02 metal materials and lights; procedural oak and sand',
    limitation='Fixed-view transparent render, no floor or shadow catcher; Unity integration and device acceptance pending.')
(OUT/'projection.json').write_text(json.dumps(metadata,indent=2)+'\n')
print('CASTING_BENCH_COMPLETE',vertices,triangles,physical,height_range)
