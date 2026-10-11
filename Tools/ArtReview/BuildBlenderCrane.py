"""Fixed-view foundry crane; existing screen anchors, study02 materials/light."""
import bpy
import json
import math
from pathlib import Path
from mathutils import Vector

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtDirection/2026-10-11/loading-bay-assets-01/crane'
OUT.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend'))
for obj in list(bpy.data.objects):
    if obj.type=='MESH': bpy.data.objects.remove(obj,do_unlink=True)
R=Vector((math.sqrt(.5),-math.sqrt(.5),0))
D=Vector((math.sqrt(.375),math.sqrt(.375),-.5))
H=1/math.cos(math.pi/6)
iron=bpy.data.materials['Forged blue iron']
bronze=bpy.data.materials['Rubbed old bronze']
soot=bpy.data.materials['Soot dark recess']

def metal(name,dark,light,rough):
    mat=iron.copy();mat.name=name
    nodes=mat.node_tree.nodes
    nodes.get('Principled BSDF').inputs['Roughness'].default_value=rough
    ramp=next(n for n in nodes if n.type=='VALTORGB')
    ramp.color_ramp.elements[0].color=(*dark,1)
    ramp.color_ramp.elements[1].color=(*light,1)
    for n in nodes:
        if n.type=='BUMP':
            n.inputs['Distance'].default_value=.006
            n.inputs['Strength'].default_value=.14
    return mat

steel=metal('Crane worked steel',(.050,.057,.065),(.115,.130,.148),.58)
cast=metal('Crane forged casting',(.025,.028,.034),(.064,.070,.080),.70)
stock=metal('Crane mill scale',(.035,.039,.046),(.085,.094,.107),.65)
bearing=metal('Crane dark machined bearing',(.028,.034,.041),(.075,.087,.099),.57)
edge=metal('Crane rubbed working edges',(.062,.068,.076),(.135,.145,.158),.51)
jib=metal('Crane cast channel webs',(.022,.025,.030),(.056,.062,.072),.69)

def point(x,y,depth=0):
    # Upright metal really follows world Z. View-direction extrusion contributes
    # no screen displacement; projected rear offsets are authored explicitly.
    return R*x+Vector((0,0,y*H))+D*depth

def finish(obj,name,mat,bevel=.015):
    obj.name=name;obj.data.materials.append(mat)
    if bevel:
        m=obj.modifiers.new('Forged edge light','BEVEL');m.width=bevel;m.segments=3
        m=obj.modifiers.new('Weighted face normals','WEIGHTED_NORMAL');m.keep_sharp=True
    return obj

def prism(name,polygon,mat,depth=.18,offset=(.04,.025),front=0,bevel=.012):
    n=len(polygon)
    vertices=[point(x,y,front) for x,y in polygon]
    vertices += [point(x+offset[0],y+offset[1],front+depth) for x,y in polygon]
    faces=[tuple(reversed(range(n))),tuple(range(n,n*2))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    # Recalculate consistent outward normals before bevel/lighting.
    bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT')
    obj.select_set(False)
    return finish(obj,name,mat,bevel)

def box(name,x0,y0,x1,y1,mat,**kwargs):
    return prism(name,[(x0,y0),(x1,y0),(x1,y1),(x0,y1)],mat,**kwargs)

def beam(name,a,b,width,mat=iron,**kwargs):
    a,b=Vector(a),Vector(b);delta=b-a
    normal=Vector((-delta.y,delta.x)).normalized()*width*.5
    return prism(name,[a+normal,b+normal,b-normal,a-normal],mat,**kwargs)

def cylinder(name,a,b,radius,mat,bevel=.009):
    a,b=point(*a),point(*b);delta=b-a
    bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=radius,depth=delta.length,location=(a+b)*.5)
    obj=bpy.context.object;obj.rotation_euler=delta.to_track_quat('Z','Y').to_euler()
    return finish(obj,name,mat,bevel)

def disk(name,x,y,r,depth,mat):
    return cylinder(name,(x,y,depth),(x,y,depth+.08),r,mat)

mast=Vector((0,6.5));rear=mast+Vector((1,0))
tip=Vector((-7.5*math.sqrt(.5),6.5*math.sqrt(.5)*.5+6.7))
normal=Vector((-(tip-rear).y,(tip-rear).x)).normalized()*.72

# Shoe dimensions and tapered cast mast follow the original projected silhouette.
box('Crane sole plate',-.67,.02,.67,.21,cast,depth=.28,offset=(.18,.12),bevel=.025)
prism('Tapered cast mast',[(-.44,.21),(.44,.21),(.43,1.42),(.34,1.78),
      (.34,4.45),(-.34,4.45),(-.34,1.78),(-.43,1.42)],
      cast,depth=.48,offset=(0,.07),bevel=.05)
# Recessed continuous inspection panel; restrained rolled-steel rim and fasteners.
box('Inspection panel shadow',-.20,1.73,.18,4.14,soot,front=-.055,depth=.035,offset=(0,0),bevel=.018)
for x in [-.20,.16]:
    beam('Inspection panel side',(x,1.75),(x,4.10),.035,stock,front=-.08,offset=(0,0))
beam('Inspection panel sill',(-.20,1.74),(.18,1.74),.045,steel,front=-.08,offset=(0,0))
box('Inspection latch',.075,2.68,.12,2.94,bronze,front=-.105,offset=(.015,.01),bevel=.012)
prism('Mast socket shoulder',[(-.44,4.36),(.44,4.36),(.50,4.48),(.50,4.64),
      (-.50,4.64),(-.50,4.48)],cast,depth=.33,offset=(0,.035),bevel=.025)
for x in [-.30,.30]:beam('Open upper mast chord',(x,4.60),(x,6.50),.30,cast,offset=(.06,.025))
for i in range(2):
    side=1 if i%2==0 else -1
    beam('Upper mast diagonal',(.3*side,4.6+i*.95),(-.3*side,4.6+(i+1)*.95),.14,stock,front=.02)

# Open box jib: same rear, tip, transverse depth and five alternating truss bays.
offset=Vector((.18,-.24))
for drop,width in [(Vector((0,0)),.20),(-normal,.19)]:
    beam('Rear jib chord',rear+drop+offset,tip+drop+offset,width,stock,front=.28)
for i in range(4):
    p=rear.lerp(tip,i/3)
    for drop in [Vector((0,0)),-normal]:
        beam('Jib cross bearer',p+drop,p+drop+offset,.12,cast,front=.06)
beam('Upper jib cast channel',rear,tip,.32,jib,depth=.29,offset=(.035,.028),bevel=.023)
beam('Lower jib cast channel',rear-normal,tip-normal,.28,jib,depth=.29,offset=(.035,.028),bevel=.021)
# Two genuine channel lips enclose each broad dark web. The working edge is
# restrained; the large face is rough cast iron instead of a smooth steel stripe.
unit_normal=normal.normalized()
for drop,width in [(Vector((0,0)),.32),(-normal,.28)]:
    for side in [-1,1]:
        lip=unit_normal*(side*(width*.5-.020))
        beam('Rolled channel lip',rear+drop+lip,tip+drop+lip,.045,
             edge if side==1 else stock,depth=.13,front=-.06,offset=(.015,.01),bevel=.007)
for i in range(5):
    p=rear.lerp(tip,i/5);q=rear.lerp(tip,(i+1)/5)
    beam('Jib diagonal web',p if i%2==0 else p-normal,q-normal if i%2==0 else q,.17,stock,depth=.14)
for p in [rear,tip]:beam('Jib end casting',p+normal*.10,p-normal*1.10,.27,cast,depth=.27,bevel=.03)
beam('Mast pivot crosshead',mast,rear,.28,stock,depth=.25,bevel=.025)
beam('Crane tension stay',(0,3),tip,.14,steel,depth=.10,front=.18,offset=(0,0))
# A continuous cast yoke straddles both upper chords and receives the pin. This
# supports the complete pivot mass rather than pinning a bright disc to a shelf.
prism('Main cast pivot yoke',[(-.49,6.05),(.49,6.05),(.55,6.41),(.38,6.78),
      (.13,6.88),(-.31,6.80),(-.55,6.52)],cast,depth=.36,front=-.045,
      offset=(.075,.06),bevel=.045)
box('Main bearing saddle',-.53,6.04,.53,6.23,stock,depth=.30,
    front=-.06,offset=(.09,.055),bevel=.026)
# The .08-deep rings overlap by .01 and seat into the yoke front, avoiding the
# source's former tiny gaps along the camera-depth extrusion direction.
disk('Main bearing steel flange',0,6.5,.32,-.12,bearing)
disk('Main bearing bronze bush',0,6.5,.178,-.19,bronze)
disk('Main bearing centre',0,6.5,.11,-.26,cast)

# Counterweight and its two hangers retain their original endpoints and bounds.
box('Crane counterweight',rear.x-.32,rear.y-.85,rear.x+.36,rear.y-.13,cast,
    depth=.30,offset=(.22,.15),front=.23,bevel=.025)
for x in [-.2,.2]:beam('Counterweight hanger',rear+Vector((x,.04)),rear+Vector((x,-.88)),.07,stock,front=-.02)
for i in [1,2]:beam('Counterweight course',rear+Vector((-.29,-.85+i*.24)),rear+Vector((.33,-.85+i*.24)),.035,steel,front=-.03)

# Existing lower winch, front crank and anchor gussets are baked as one assembly;
# live BuildCraneHardware must be skipped after successful texture load.
for side in [-1,1]:
    prism('Crane anchor gusset',[(side*.5,0),(side*.15,0),(side*.15,.8)],
        stock,front=-.12,depth=.16,offset=(.025,0),bevel=.018)
cylinder('Lower winch drum',(-.24,1.07,-.22),(.24,1.07,-.22),.23,cast)
for x in [-.24,.24]:cylinder('Winch cable flange',(x-.035,1.07,-.22),(x+.035,1.07,-.22),.29,steel)
for x in [-.15,-.075,0,.075,.15]:cylinder('Wound winch cable',(x-.023,1.07,-.22),(x+.023,1.07,-.22),.24,stock,.004)
# Connected winch cheeks and gearcase form a single seated lower machine. They
# stay inside the established shoe and leave the working cable spool exposed.
prism('Lower winch gearbox',[(-.38,.42),(.49,.42),(.65,.56),(.65,.91),
      (.38,1.00),(-.38,.88)],cast,front=-.255,depth=.19,offset=(.02,.018),bevel=.038)
for side in [-1,1]:
    x=side*.31
    prism('Winch bearing cheek',[(x-.075,.83),(x+.075,.83),(x+.09,1.28),
          (x+.04,1.39),(x-.04,1.39),(x-.09,1.28)],stock,
          front=-.25,depth=.15,offset=(.018,.018),bevel=.022)
box('Winch housing foot',-.43,.40,.57,.52,stock,front=-.28,depth=.17,offset=(.02,.015),bevel=.022)
disk('Lower crank flange',.38,.68,.30,-.33,bearing)
disk('Lower crank bronze bush',.38,.68,.19,-.40,bronze)
disk('Lower crank centre',.38,.68,.105,-.47,cast)
beam('Lower hand crank',(.38,.68),(.52,.84),.052,steel,front=-.62,offset=(0,0))

scene=bpy.context.scene;camera=scene.camera
target=point(-2,5)
camera.location=target-D*21
camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
camera.data.ortho_scale=10.8
scene.render.resolution_x=1024;scene.render.resolution_y=1024
scene.render.filepath=str(OUT/'crane.png')
scene.render.film_transparent=True
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
# Evaluate the native bevelled geometry against the original fixed camera frame
# before producing a runtime candidate. Screen anchors are analytic and immutable.
from bpy_extras.object_utils import world_to_camera_view
bpy.context.view_layer.update()
graph=bpy.context.evaluated_depsgraph_get()
projected=[];vertices=0;triangles=0
for obj in bpy.context.scene.objects:
    if obj.type!='MESH':continue
    evaluated=obj.evaluated_get(graph);mesh=evaluated.to_mesh()
    mesh.calc_loop_triangles();vertices+=len(mesh.vertices);triangles+=len(mesh.loop_triangles)
    for vertex in mesh.vertices:
        p=world_to_camera_view(scene,camera,evaluated.matrix_world@vertex.co)
        assert all(math.isfinite(value) for value in p),obj.name
        assert 0.0<=p.x<=1.0 and 0.0<=p.y<=1.0,(obj.name,list(p))
        projected.append((p.x,p.y))
    evaluated.to_mesh_clear()
anchors={name:list(world_to_camera_view(scene,camera,point(*value)))[:2]
         for name,value in [('mast',mast),('rear',rear),('tip',tip)]}
for name,value in [('mast',mast),('rear',rear),('tip',tip)]:
    expected=((value.x+7.4)/10.8,(value.y+.4)/10.8)
    assert max(abs(anchors[name][i]-expected[i]) for i in range(2))<0.00001,(name,anchors[name],expected)
bpy.context.preferences.filepaths.save_version=0
bpy.ops.wm.save_as_mainfile(filepath=str(OUT/'crane.blend'))
bpy.ops.render.render(write_still=True)
metadata=dict(anchor='WestProject(-25.5,13)+up*1.1',left=-7.4,right=3.4,bottom=-.4,top=10.4,
    ortho_scale=10.8,render_size=1024,mast=list(mast),rear=list(rear),tip=list(tip),
    depth_footprint=[-27,11,-24,15],depth_footprint_x_offset='WestShift',depth_height=8.6,
    runtime_rank=-11.5,live=['masonry footing','metal footing cap','hanging chain','tip sheave','payload basket','lantern'],
    evaluated_vertices=vertices,evaluated_triangles=triangles,
    projected_bounds=[min(p[0] for p in projected),min(p[1] for p in projected),
                      max(p[0] for p in projected),max(p[1] for p in projected)],
    projected_anchors=anchors,
    source='Authored geometry; study02 procedural materials and lights',
    limitation='Fixed-view camera-space exact-anchor bake. All component shading is static.')
(OUT/'projection.json').write_text(json.dumps(metadata,indent=2)+'\n')
print('CRANE_COMPLETE')
