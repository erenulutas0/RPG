"""Reproducible, external-art-only furnace study. Run with Blender 4.5 LTS.

No Unity Assets are changed. Materials are authored node graphs, not generated images.
The 30-degree orthographic camera matches the game's 2:1 ground projection;
vertical model coordinates are compensated so one design-height unit projects to one unit.
"""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "ArtDirection/2026-10-09/blender-furnace-01"
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
HEIGHT = 1 / math.cos(math.radians(30))


def material(name, dark, light, metallic=0, roughness=.65, scale=5, bump=.045):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    n, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = n.get('Principled BSDF')
    bsdf.inputs['Metallic'].default_value = metallic
    bsdf.inputs['Roughness'].default_value = roughness
    tex = n.new('ShaderNodeTexNoise')
    tex.inputs['Scale'].default_value = scale
    tex.inputs['Detail'].default_value = 3
    ramp = n.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = .18
    ramp.color_ramp.elements[0].color = (*dark, 1)
    ramp.color_ramp.elements[1].position = .82
    ramp.color_ramp.elements[1].color = (*light, 1)
    links.new(tex.outputs['Fac'], ramp.inputs[0])
    links.new(ramp.outputs[0], bsdf.inputs['Base Color'])
    fine = n.new('ShaderNodeTexNoise')
    fine.inputs['Scale'].default_value = 95
    fine.inputs['Detail'].default_value = 2
    relief = n.new('ShaderNodeBump')
    relief.inputs['Strength'].default_value = .22
    relief.inputs['Distance'].default_value = bump
    links.new(fine.outputs['Fac'], relief.inputs['Height'])
    links.new(relief.outputs['Normal'], bsdf.inputs['Normal'])
    return mat


stone = material('Violet basalt', (.033,.031,.043), (.105,.09,.108), scale=3)
iron = material('Forged blue iron', (.022,.029,.034), (.065,.078,.081), .72,.46,7,.022)
bronze = material('Rubbed old bronze', (.095,.055,.023), (.27,.17,.066), .68,.42,9,.018)
soot = material('Soot dark recess', (.004,.005,.008), (.018,.02,.022), .1,.9)
fire = bpy.data.materials.new('Contained forge heat')
fire.use_nodes = True
bs = fire.node_tree.nodes.get('Principled BSDF')
bs.inputs['Base Color'].default_value = (.65,.08,.008,1)
bs.inputs['Emission Color'].default_value = (1,.12,.008,1)
bs.inputs['Emission Strength'].default_value = 5


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


# Match the tall production boiler's 2-unit radius and 7.2-unit design height.
cylinder('Foundation',2,0,.38,stone,16)
cylinder('Mortar backing',1.77,.38,3.35,soot)
for row in range(5):
    for i in range(12):
        a=(i+(row%2)*.5)*math.tau/12
        sector(f'Basalt course {row} block {i}',1.68,1.86,.42+row*.57,.95+row*.57,
               a+.012,a+math.tau/12-.012,stone)
cylinder('Overhanging dressed cornice',2.03,3.30,3.60,stone,16,bevel=.065)
cylinder('Pressure vessel',1.60,3.62,5.40,iron)
for low,high,radius in [(3.62,3.78,1.66),(4.45,4.55,1.635),(5.26,5.45,1.67)]:
    cylinder('Bronze pressure band',radius,low,high,bronze)
    for i in range(16):
        a=i*math.tau/16
        bpy.ops.mesh.primitive_uv_sphere_add(segments=8, ring_count=4, radius=.045,
            location=((radius+.008)*math.cos(a),(radius+.008)*math.sin(a),(low+high)*.5*HEIGHT))
        finish(bpy.context.object,'Band fastener',bronze)
for i in range(8):
    a=i*math.tau/8
    cube('Vertical plate binding',(1.602*math.cos(a),1.602*math.sin(a),4.53),
         (.055,.085,1.43),iron,a)
cylinder('Pressed shoulder',1.60,5.45,5.68,iron,32,1.34)
cylinder('Formed hood',1.34,5.68,6.06,iron,32,.74)
cylinder('Hood neck',.74,6.06,6.30,iron,24,.37)
cylinder('Neck collar',.41,6.27,6.39,bronze,24)
cylinder('Flue',.32,6.36,7.12,iron,20)
cylinder('Flue lip',.40,7.07,7.20,iron,20)
cylinder('Dark flue mouth',.315,7.20,7.205,soot,20,bevel=0)

# Two lower fireboxes face the same southwest viewing half as the production grates.
for a in [math.radians(195), math.radians(255)]:
    outward=Vector((math.cos(a),math.sin(a)))
    def panel(name, radius, z, width, depth, height, mat, bevel=.02):
        return cube(name,(outward.x*radius,outward.y*radius,z),(depth,width,height),mat,a,bevel)
    panel('Firebox cast surround',1.84,1.74,1.15,.24,1.82,iron,.08)
    panel('Firebox bronze rebate',1.979,1.75,.98,.055,1.60,bronze)
    panel('Firebox inner darkness',2.015,1.75,.83,.035,1.43,soot)
    panel('Coal heat',2.04,1.70,.70,.015,1.24,fire,0)
    for j in range(5):
        o=panel('Heavy fire grate',2.066,1.73,.045,.06,1.38,iron,.012)
        o.location.x += -math.sin(a)*(j-2)*.155
        o.location.y += math.cos(a)*(j-2)*.155


def light(name, loc, energy, color, size):
    data=bpy.data.lights.new(name,'AREA');data.energy=energy;data.color=color;data.shape='DISK';data.size=size
    obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj);obj.location=loc
    obj.rotation_euler=(Vector((0,0,3))-obj.location).to_track_quat('-Z','Y').to_euler()


light('Broad cool skylight',(-5,-6,10),1100,(.62,.70,1),7)
light('Soft warm forge bounce',(-3,-4,1.3),180,(1,.23,.045),3)
light('Quiet upper rim',(4,2,9),700,(.72,.69,.82),5)
scene=bpy.context.scene
scene.world.color=(.08,.08,.08)
scene.render.engine='CYCLES';scene.cycles.samples=32;scene.cycles.use_denoising=True
scene.render.resolution_x=1200;scene.render.resolution_y=1200;scene.render.resolution_percentage=100
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
scene.render.film_transparent=True
scene.view_settings.view_transform='AgX'
camera=bpy.data.cameras.new('Matched 2 to 1 orthographic')
obj=bpy.data.objects.new('Camera',camera);bpy.context.collection.objects.link(obj)
target=Vector((0,0,3.6*HEIGHT));obj.location=target+Vector((-12,-12,math.sqrt(96)))
obj.rotation_euler=(target-obj.location).to_track_quat('-Z','Y').to_euler()
camera.type='ORTHO';camera.ortho_scale=10.8;scene.camera=obj
scene.render.filepath=str(OUT/'furnace-blender.png')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'furnace-study.blend'))
bpy.ops.render.render(write_still=True)
# Explicit preview mesh export. Node materials are not baked for game use.
for o in bpy.context.selected_objects:o.select_set(False)
for o in scene.objects:
    if o.type=='MESH':o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUT/'furnace-study.fbx'),use_selection=True,
    object_types={'MESH'},bake_anim=False,add_leaf_bones=False)
deps=bpy.context.evaluated_depsgraph_get();verts=tris=0
for o in scene.objects:
    if o.type!='MESH':continue
    m=o.evaluated_get(deps).to_mesh();m.calc_loop_triangles()
    verts+=len(m.vertices);tris+=len(m.loop_triangles)
    o.evaluated_get(deps).to_mesh_clear()
(OUT/'model-budget.json').write_text(json.dumps(dict(blender=bpy.app.version_string,
    evaluated_vertices=verts,evaluated_triangles=tris,height_compensation=HEIGHT,
    ortho_scale=10.8,render_size=1200,source='Authored geometry and procedural material nodes',
    limitation='Study mesh; materials not baked, not installed or mobile budget approved'),indent=2))
print('FURNACE_STUDY_COMPLETE',verts,tris)
