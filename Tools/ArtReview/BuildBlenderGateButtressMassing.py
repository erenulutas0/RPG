"""Complete outer gate buttress massing replacement, fixed-view RGBA bake.

Run with Blender 4.5.9 LTS. Writes external art evidence only, never Unity Assets.
The complete above-deck bay replaces native faces; foundation/door/props stay live.
"""
import bpy
import hashlib
import json
import math
import numpy as np
from pathlib import Path
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'ArtDirection/2026-10-14/foundry-gate-massing-01/masonry-bay'
OUT.mkdir(parents=True, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT / 'ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend'))
for obj in list(bpy.data.objects):
    if obj.type == 'MESH':
        bpy.data.objects.remove(obj, do_unlink=True)

HEIGHT = 1 / math.cos(math.radians(30))
ANCHOR = (28.975, -5.375)
FOOT = (27.05, -6.60, 30.90, -4.15)
SHAFT = (27.53, -6.14, 30.42, -4.61)
CONTACTS = [0, .48, 2.05, 3.65, 6.55, 6.82, 7.20, 7.35]
EXTERNAL_CONTACTS = [0, 6.55, 6.82, 7.20, 7.35]
SHAFT_BOTTOM = 3.65
SHAFT_TOP = 6.55
BLIND_DEPTH = .48
OPENING = [(.33,.17),(.67,.17),(.67,.69),(.5,.84),(.33,.69)]
FRAME = [(.12,.08),(.88,.08),(.88,.75),(.5,.94),(.12,.75)]
models = []
contact_stations = []
component_stations = {}
component_caps = {}


def point(u, v, z):
    # All bay u >= 27.05: Unity Project(anchor) alone supplies the +1.25 ramp lift.
    return Vector((u-ANCHOR[0], v-ANCHOR[1], z*HEIGHT))


def stone_material(name, gain, rough=.84):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bs = nodes.get('Principled BSDF')
    bs.inputs['Roughness'].default_value = rough
    bs.inputs['Specular IOR Level'].default_value = .24
    tex = nodes.new('ShaderNodeTexCoord')
    coarse = nodes.new('ShaderNodeTexNoise')
    coarse.inputs['Scale'].default_value = 2.3
    coarse.inputs['Detail'].default_value = 4
    links.new(tex.outputs['Object'], coarse.inputs['Vector'])
    ramp = nodes.new('ShaderNodeValToRGB')
    ramp.color_ramp.elements[0].position = .12
    ramp.color_ramp.elements[0].color = (.060*gain,.066*gain,.079*gain,1)
    ramp.color_ramp.elements[1].position = .90
    ramp.color_ramp.elements[1].color = (.115*gain,.124*gain,.147*gain,1)
    links.new(coarse.outputs['Fac'], ramp.inputs[0])
    links.new(ramp.outputs[0], bs.inputs['Base Color'])
    pores = nodes.new('ShaderNodeTexNoise')
    pores.inputs['Scale'].default_value = 43
    pores.inputs['Detail'].default_value = 3
    pores.inputs['Roughness'].default_value = .73
    links.new(tex.outputs['Object'], pores.inputs['Vector'])
    bump = nodes.new('ShaderNodeBump')
    bump.inputs['Strength'].default_value = .32
    bump.inputs['Distance'].default_value = .035
    links.new(pores.outputs['Fac'], bump.inputs['Height'])
    bevel = nodes.new('ShaderNodeBevel')
    bevel.inputs['Radius'].default_value = .018
    bevel.samples = 4
    links.new(bevel.outputs['Normal'], bump.inputs['Normal'])
    links.new(bump.outputs['Normal'], bs.inputs['Normal'])
    return mat


stones = [stone_material('Gate quarried basalt %02d'%i, g) for i,g in enumerate([.90,.96,1,1.05,1.10])]
dressed = stone_material('Gate dressed bearing basalt',1.13,.78)
recess = stone_material('Gate protected niche basalt',.55,.92)
mortar = stone_material('Gate deep mineral joints',.40,.96)


def surface(name, verts, faces, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([tuple(p) for p in verts], [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name,mesh)
    bpy.context.collection.objects.link(obj)
    obj.data.materials.append(material)
    models.append(obj)
    return obj


def quad(name, p, material):
    return surface(name,p,[(0,1,2,3)],material)


def lerp_face(face, x, y):
    return face[0].lerp(face[1],x).lerp(face[3].lerp(face[2],x),y)


def stone_patch(name, p, material, inward, bevel=.030):
    # The shared boundary is the original wall plane. Its broad face lies on
    # that plane too; only the bed-side arris retreats into the real masonry.
    # This preserves actual contact/silhouette stations without a front overlay.
    width = (p[1]-p[0]).length
    height = (p[3]-p[0]).length
    bx = min(.16,bevel/max(.01,width))
    by = min(.16,bevel/max(.01,height))
    inside = [lerp_face(p,bx,by),lerp_face(p,1-bx,by),
              lerp_face(p,1-bx,1-by),lerp_face(p,bx,1-by)]
    # Preserve every original outer boundary, including the niche throat and
    # bed contacts. The recessed arris starts just INSIDE that boundary: merely
    # moving the entire outside ring inward changes the actual opening.
    gx=bx*.18;gy=by*.18
    groove = [lerp_face(p,gx,gy)+inward*.035,lerp_face(p,1-gx,gy)+inward*.035,
              lerp_face(p,1-gx,1-gy)+inward*.035,lerp_face(p,gx,1-gy)+inward*.035]
    verts = list(p)+groove+inside
    faces = [(8,9,10,11)]
    for i in range(4):
        j=(i+1)%4
        faces.append((i,j,j+4,i+4))
        faces.append((i+4,j+4,j+8,i+8))
    return surface(name,verts,faces,material)


def octagon(box, z, corner):
    u0,v0,u1,v1=box
    return [point(u0+corner,v0,z),point(u1-corner,v0,z),
            point(u1,v0+corner,z),point(u1,v1-corner,z),
            point(u1-corner,v1,z),point(u0+corner,v1,z),
            point(u0,v1-corner,z),point(u0,v0+corner,z)]


def tier_rings(box,bottom,top,inset):
    u0,v0,u1,v1=box
    corner=min(.18,min(u1-u0,v1-v0)*.15)
    upper=(u0+inset,v0+inset,u1-inset,v1-inset)
    top_corner=min(corner*.75,(min(u1-u0,v1-v0)-2*inset)*.25)
    return octagon(box,bottom,corner),octagon(upper,top,top_corner)


def coursed_face(name,p,rows,columns,material=None):
    normal = (p[1]-p[0]).cross(p[3]-p[0]).normalized()
    inward = Vector((-normal.x,-normal.y,0)).normalized()
    # A real recessed mortar plane under the independent stone blocks, never a
    # coplanar full-face duplicate. It is only visible in the narrow bed joints.
    quad(name+' recessed mortar',[v+inward*.038 for v in p],mortar)
    for row in range(rows):
        y0=row/rows;y1=(row+1)/rows
        offset=0 if row%2==0 else -.43/columns
        col=0
        while offset<1-1e-7:
            x0=max(0,offset);x1=min(1,offset+1/columns)
            if x1>x0:
                gapx=.009/max(.01,(p[1]-p[0]).length)
                gapy=.008/max(.01,(p[3]-p[0]).length)
                lo=x0+(gapx if x0>0 else 0);hi=x1-(gapx if x1<1 else 0)
                low=y0+(gapy if row else 0);high=y1-(gapy if row+1<rows else 0)
                cell=[lerp_face(p,lo,low),lerp_face(p,hi,low),
                      lerp_face(p,hi,high),lerp_face(p,lo,high)]
                stone_patch(name+' dressed block %d %d'%(row,col),cell,
                            material or stones[(row*3+col*2)%len(stones)],inward)
            offset+=1/columns;col+=1


def tier(name,box,bottom,top,inset,rows=1,cap_material=dressed):
    lo,hi=tier_rings(box,bottom,top,inset)
    contact_stations.extend(lo+hi)
    component_stations[name] = dict(bottom=lo, top=hi)
    for side in range(8):
        j=(side+1)%8;p=[lo[side],lo[j],hi[j],hi[side]]
        if side in (0,6):
            cols=max(1,round((p[1]-p[0]).length/1.2))
            coursed_face(name+' face%d'%side,p,rows,cols,cap_material if top-bottom<.5 else None)
        else:
            quad(name+' closed return%d'%side,p,stones[2])
    # Unchanged complete cap: these exact perimeter stations bear the next tier.
    component_caps[name] = surface(name+' closed bearing cap',hi,[tuple(range(8))],cap_material)
    return lo,hi


def split_patch(name,p,pieces,material,inward):
    for i in range(pieces):
        lo=i/pieces+(0 if i==0 else .0045)
        hi=(i+1)/pieces-(0 if i+1==pieces else .0045)
        part=[p[0].lerp(p[1],lo),p[0].lerp(p[1],hi),
              p[3].lerp(p[2],hi),p[3].lerp(p[2],lo)]
        stone_patch(name+' stone%d'%i,part,material,inward,.037)
        if i:
            a=i/pieces-.0045;b=i/pieces+.0045
            quad(name+' recessed joint%d'%i,[p[0].lerp(p[1],a)+inward*.038,
                 p[0].lerp(p[1],b)+inward*.038,p[3].lerp(p[2],b)+inward*.038,
                 p[3].lerp(p[2],a)+inward*.038],mortar)


def shaft():
    lo,hi=tier_rings(SHAFT,SHAFT_BOTTOM,SHAFT_TOP,.065)
    contact_stations.extend(lo+hi)
    component_stations['Short blind upper bay'] = dict(bottom=lo, top=hi)
    for side in range(8):
        j=(side+1)%8;face=[lo[side],lo[j],hi[j],hi[side]]
        if side not in (0,6):
            quad('Shaft closed return%d'%side,face,stones[2])
            continue
        inward=Vector((0,BLIND_DEPTH,0)) if side==0 else Vector((BLIND_DEPTH,0,0))
        unit=inward.normalized()
        at=lambda p:lerp_face(face,*p)
        opening=[at(p) for p in OPENING]
        frame=[at(p) for p in FRAME]
        border=[face[0],face[1],face[2],face[2].lerp(face[3],.5),face[3]]
        throat=[p+inward*.25 for p in opening]
        back=[p+inward for p in opening]
        contact_stations.extend(frame+throat+back)
        surface('Niche%d physically closed back'%side,back,[tuple(range(5))],recess)
        for i in range(5):
            k=(i+1)%5
            quad('Niche%d deep return%d'%(side,i),[throat[i],throat[k],back[k],back[i]],stones[1])
            body=[border[i],border[k],frame[k],frame[i]]
            casing=[frame[i],frame[k],throat[k],throat[i]]
            split_patch('Niche%d body%d'%(side,i),body,3 if i in (1,4) else 1,stones[(i+2)%5],unit)
            split_patch('Niche%d fitted frame%d'%(side,i),casing,
                        1 if i==0 else 3 if i in (1,4) else 2,dressed,unit)
    component_caps['Short blind upper bay'] = surface('Shaft original bearing cap',hi,[tuple(range(8))],dressed)


tier('Broad footing',FOOT,0,.48,.08)
tier('Lower ashlar pier',(27.16,-6.51,30.79,-4.24),.48,2.05,.09,2)
tier('Deep raked structural haunch',(27.25,-6.42,30.70,-4.33),2.05,SHAFT_BOTTOM,.28,2)
shaft()
tier('Deep crown bearing',(27.42,-6.25,30.53,-4.50),6.55,6.82,.07)
tier('Raked crown',(27.54,-6.14,30.41,-4.61),6.82,7.20,.30)
tier('Crown coping',(27.84,-5.84,30.11,-4.91),7.20,7.35,.04)

# One shared neutral environment: the same unmirrored orientation can serve both
# north/south bays without baking two contradictory local flames into the stone.
for light in bpy.data.objects:
    if light.type != 'LIGHT':
        continue
    if light.name == 'Broad cool skylight':
        light.data.energy=1250;light.data.color=(.75,.79,1)
        light.location=Vector((-5,-6,11))
    elif light.name == 'Soft warm forge bounce':
        light.data.energy=18;light.data.color=(1,.55,.30)
    elif light.name == 'Quiet upper rim':
        light.data.energy=650;light.data.color=(.76,.77,.88)
    light.rotation_euler=(Vector((0,0,3.6*HEIGHT))-light.location).to_track_quat('-Z','Y').to_euler()

scene=bpy.context.scene;camera=scene.camera
target=Vector((0,0,3.75*HEIGHT))
camera.location=target+Vector((-12,-12,math.sqrt(96)))
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.ortho_scale=10
scene.render.resolution_x=1024;scene.render.resolution_y=2048
scene.render.resolution_percentage=100
scene.render.film_transparent=True
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
scene.cycles.samples=48;scene.cycles.use_denoising=True
scene.render.filepath=str(OUT/'gate-masonry.png')
bpy.context.preferences.filepaths.save_version=0

deps=bpy.context.evaluated_depsgraph_get();vertices=triangles=0;bounds=[]
for obj in models:
    evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
    vertices+=len(mesh.vertices);triangles+=len(mesh.loop_triangles)
    bounds.extend(obj.matrix_world@v.co for v in mesh.vertices)
    evaluated.to_mesh_clear()
physical=[min(p.x for p in bounds)+ANCHOR[0],min(p.y for p in bounds)+ANCHOR[1],
          max(p.x for p in bounds)+ANCHOR[0],max(p.y for p in bounds)+ANCHOR[1]]
heights=[min(p.z for p in bounds)/HEIGHT,max(p.z for p in bounds)/HEIGHT]
assert max(abs(physical[i]-FOOT[i]) for i in range(4))<1e-5,physical
assert abs(heights[0])<1e-5 and abs(heights[1]-7.35)<1e-5,heights
max_contact_error=max(min((actual-expected).length for actual in bounds) for expected in contact_stations)
assert max_contact_error<1e-6,max_contact_error


def polygon_area(polygon):
    return abs(sum(a[0]*b[1]-a[1]*b[0] for a,b in zip(polygon,polygon[1:]+polygon[:1])))*.5


def clip_polygon(subject,clip):
    # Convex measured bearing caps are counter-clockwise in the native u/v plane.
    result=list(subject)
    for a,b in zip(clip,clip[1:]+clip[:1]):
        if not result:
            break
        output=[]
        cross=lambda p:(b[0]-a[0])*(p[1]-a[1])-(b[1]-a[1])*(p[0]-a[0])
        for p,q in zip(result,result[1:]+result[:1]):
            dp,dq=cross(p),cross(q)
            if dp>=-1e-7:
                output.append(p)
            if (dp>=0)!=(dq>=0):
                amount=dp/(dp-dq)
                output.append((p[0]+(q[0]-p[0])*amount,p[1]+(q[1]-p[1])*amount))
        result=output
    return result


# Measure the actual cap mesh, then locate the next course's authored feet in
# actual evaluated geometry. Crown overhang is intentional; its broad seated
# contact is measured rather than claiming its complete base fits on the shaft.
join_order=list(component_stations)
bearings=[]
for lower,upper in zip(join_order,join_order[1:]):
    cap=component_caps[lower]
    measured_cap=[cap.matrix_world@v.co for v in cap.data.vertices]
    feet=[min(bounds,key=lambda actual:(actual-expected).length)
          for expected in component_stations[upper]['bottom']]
    z_error=max(abs(a.z-b.z) for a in measured_cap for b in feet)/HEIGHT
    assert z_error<1e-5,(lower,upper,z_error)
    cap_uv=[(p.x,p.y) for p in measured_cap]
    feet_uv=[(p.x,p.y) for p in feet]
    contact_area=polygon_area(clip_polygon(feet_uv,cap_uv))
    upper_area=polygon_area(feet_uv)
    support_fraction=contact_area/upper_area
    assert support_fraction>.64,(lower,upper,support_fraction)
    bearings.append(dict(lower=lower,upper=upper,height=sum(p.z for p in feet)/len(feet)/HEIGHT,
        measured_contact_area=contact_area,upper_foot_area=upper_area,
        supported_fraction=support_fraction,max_vertical_gap=z_error))
assert abs(bearings[2]['height']-SHAFT_BOTTOM)<1e-5,bearings[2]
screen=[world_to_camera_view(scene,camera,p) for p in bounds]
projected=[min(p.x for p in screen),min(p.y for p in screen),max(p.x for p in screen),max(p.y for p in screen)]
assert min(projected)>=0 and max(projected)<=1,projected
metadata=dict(blender=bpy.app.version_string,vertices=vertices,triangles=triangles,
    anchor=list(ANCHOR),far_anchor=[28.975,15.375],ortho_scale=10,
    left=-2.5,right=2.5,bottom=-1.25,top=8.75,render_width=1024,render_height=2048,
    height_compensation=HEIGHT,camera_direction=[-12,-12,math.sqrt(96)],vertical_target=3.75,
    depth_footprint=list(FOOT),far_depth_footprint=[27.05,14.15,30.90,16.60],depth_height=7.35,
    physical_bounds=physical,height_range=heights,projected_normalized_bounds=projected,
    construction_contact_heights=CONTACTS,unchanged_external_contact_heights=EXTERNAL_CONTACTS,
    replaced_internal_contact=dict(before=2.85,after=SHAFT_BOTTOM),
    shaft_box=list(SHAFT),shaft_height=[SHAFT_BOTTOM,SHAFT_TOP],shaft_taper=.065,
    verified_contact_station_count=len(contact_stations),max_contact_station_error=max_contact_error,
    measured_bearings=bearings,
    blind_depth=BLIND_DEPTH,throat_depth_fraction=.25,opening=OPENING,frame=FRAME,
    projection='X=(u-v)*0.70710678; Y=(u+v)*0.35355339+Height(u)+z; bake excludes Height(u)',
    unity_anchor_rise=1.25,intended_import=[512,1024],android_format='ASTC_6x6 with mipmaps',
    source='Versioned native Blender massing replacement derived from BuildBlenderGateMasonry.py; no raster edit',
    expected_runtime_delta=dict(vertices=0,renderers=0,texture_resources=0,material_resources=0),
    baseline_loaded_texture=dict(width=512,height=1024,format='ASTC_6x6',mipmaps=11,
        readable=False,editor_texture_memory_bytes=630880),
    limitation='Fixed-view colour/normal-light bake, not realtime volumetric geometry. Existing texture replacement must be imported and measured in Unity; device acceptance is not proven by this render.')
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'gate-masonry.blend'))
bpy.ops.render.render(write_still=True)
# Read the rendered pixels only to measure image coverage. No raster edit is
# performed; the production PNG is the direct native Blender render above.
rendered=bpy.data.images.load(str(OUT/'gate-masonry.png'),check_existing=False)
pixels=np.empty(len(rendered.pixels),dtype=np.float32)
rendered.pixels.foreach_get(pixels)
alpha=pixels.reshape((rendered.size[1],rendered.size[0],4))[:,:,3]
ys,xs=np.nonzero(alpha>.01)
alpha_bounds=[int(xs.min()),int(ys.min()),int(xs.max()),int(ys.max())]
assert alpha_bounds[0]>0 and alpha_bounds[1]>0 and alpha_bounds[2]<rendered.size[0]-1 and alpha_bounds[3]<rendered.size[1]-1,alpha_bounds
metadata['rendered_image']=dict(width=rendered.size[0],height=rendered.size[1],alpha_bounds_bottom_origin=alpha_bounds,
    bytes=(OUT/'gate-masonry.png').stat().st_size,sha256=hashlib.sha256((OUT/'gate-masonry.png').read_bytes()).hexdigest())
(OUT/'projection.json').write_text(json.dumps(metadata,indent=2)+'\n')
print('GATE_BUTTRESS_MASSING_COMPLETE',vertices,triangles,physical,heights,projected,alpha_bounds)
