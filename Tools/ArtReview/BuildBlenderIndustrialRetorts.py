"""Low foundry retort; loads the approved study02 material/light scene, replaces its meshes.
Run with portable Blender4.5.9. No Unity files are modified.
"""
import bpy, math, json
from bpy_extras.object_utils import world_to_camera_view
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'ArtDirection/2026-10-11/industrial-retort-01'
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



def loft(name, outline, lower, upper, scale, material, bevel=.022):
    vertices=[(x*s,y*s,z*HEIGHT) for z,s in [(lower,1),(upper,scale)] for x,y in outline]
    n=len(outline)
    faces=[tuple(reversed(range(n))),tuple(range(n,n*2))]
    faces.extend([(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)])
    mesh=bpy.data.meshes.new(name);mesh.from_pydata(vertices,[],faces);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    return finish(obj,name,material,bevel)


def rod(name,start,end,radius,material):
    a=Vector((start[0],start[1],start[2]*HEIGHT))
    b=Vector((end[0],end[1],end[2]*HEIGHT))
    bpy.ops.mesh.primitive_cylinder_add(vertices=12,radius=radius,depth=(b-a).length,location=(a+b)*.5)
    obj=bpy.context.object;obj.rotation_euler=(b-a).to_track_quat('Z','Y').to_euler()
    return finish(obj,name,material,.009)


# Quiet procedural finish differences belong to whole manufactured panels.
rolled=iron.copy();rolled.name='Retort rolled steel with weathered scale'
for node in rolled.node_tree.nodes:
    if node.type=='VALTORGB':
        node.color_ramp.elements[0].color=(.023,.029,.034,1)
        node.color_ramp.elements[-1].color=(.086,.101,.108,1)
rolled.node_tree.nodes.get('Principled BSDF').inputs['Roughness'].default_value=.58


def manufactured_hood(variant):
    cylinder('Continuous iron seating flange',1.60,1.45,1.53,iron,16,bevel=.025)
    if variant==0:
        # Eight folded planes, a flat inspection deck and an asymmetric hatch.
        outline=[(-1.02,-1.30),(1.02,-1.30),(1.43,-.89),(1.43,.89),
                 (1.02,1.30),(-1.02,1.30),(-1.43,.89),(-1.43,-.89)]
        loft('Folded octagonal furnace hood',outline,1.52,1.98,.66,rolled,.035)
        cube('Seated inspection hatch gasket',(-.60,-.40,1.994),(.59,.51,.028),soot,bevel=.018)
        cube('Low inspection hatch casting',(-.60,-.40,2.044),(.56,.48,.074),iron,bevel=.035)
        for x in [-.78,-.43]:
            rod('Inspection hatch seated hinge',(x,-.19,2.073),(x+.10,-.19,2.073),.043,bronze)
        for x in [-.75,-.45]:
            cube('Inspection handle foot',(x,-.47,2.10),(.07,.08,.05),iron,bevel=.012)
        rod('Inspection hatch handle',(-.75,-.47,2.14),(-.45,-.47,2.14),.030,iron)
        cube('Hatch front locking dog',(-.60,-.66,2.045),(.17,.10,.11),bronze,bevel=.02)
        # Broad folded corner seams are attached to the sloping plate.
        for i in [0,2,4,6]:
            x,y=outline[i]
            rod('Hood fold reinforcement',(x*.98,y*.98,1.55),(x*.67,y*.67,1.99),.033,iron)
    else:
        outline=[(1.54*math.cos(i*math.tau/12),1.54*math.sin(i*math.tau/12)) for i in range(12)]
        loft('Twelve pressed furnace hood panels',outline,1.52,1.96,.43,rolled,.018)
        # Alternating ribs and small bearing shoes distinguish this retort from
        # the inspection-deck variant without a second bronze pot rim.
        for i in range(0,12,2):
            a=i*math.tau/12;x,y=math.cos(a),math.sin(a)
            rod('Supported hood rib',(x*1.48,y*1.48,1.55),(x*.68,y*.68,1.97),.050,iron)
            cube('Rib flange shoe',(x*1.48,y*1.48,1.555),(.21,.14,.09),iron,a,bevel=.02)
        # A single service door sits on the front inclined panel, with its
        # lower/upper ends on the exact hood profile and broad side cheeks.
        a=math.radians(225);n=Vector((math.cos(a),math.sin(a)));t=Vector((-n.y,n.x))
        def panel_height(radius):
            # Angle225 lies at the middle of one polygon face, whose apothem
            # is smaller than the vertex radius. Seat hardware on that face.
            outer=1.54*math.cos(math.pi/12)
            return 1.52+(outer-radius)*.44/(outer*(1-.43))
        verts=[]
        for radius in [1.31,.94]:
            for side in [-.21,.21]:
                p=n*radius+t*side;verts.append((p.x,p.y,(panel_height(radius)+.010)*HEIGHT))
        mesh=bpy.data.meshes.new('Seated ribbed hood service door');mesh.from_pydata(verts,[],[(0,1,3,2)]);mesh.update()
        obj=bpy.data.objects.new('Seated ribbed hood service door',mesh);bpy.context.collection.objects.link(obj)
        finish(obj,obj.name,iron)
        solid=obj.modifiers.new('Seated service door thickness','SOLIDIFY');solid.thickness=.012;solid.offset=1
        for side in [-.24,.24]:
            p=n*1.33+t*side;q=n*.93+t*side
            rod('Service door bearing',(p.x,p.y,panel_height(1.33)+.020),
                (q.x,q.y,panel_height(.93)+.020),.035,bronze)
    # The smoke source remains centred and ends at exactly the runtime2.28.
    cylinder('Centred exhaust seat',.29,1.95,2.025,iron,16,bevel=.018)
    cylinder('Low exhaust neck',.20,2.01,2.23,iron,16,bevel=.012)
    cylinder('Exhaust outlet lip',.25,2.22,2.28,iron,16,bevel=.008)
    cylinder('Dark exhaust mouth',.185,2.274,2.28,soot,16,bevel=0)
    for i in range(8):
        a=(i+.5)*math.tau/8
        cylinder_obj=cube('Seating flange bolt',(1.51*math.cos(a),1.51*math.sin(a),1.55),(.065,.065,.045),bronze,a,.012)


def build_base_and_doors():
    cylinder('Basalt footing',2,0,.30,stone,24,bevel=.045)
    cylinder('Soot drum',1.77,.30,1.30,soot)
    for row in range(2):
        for i in range(12):
            a=(i+(row%2)*.5)*math.tau/12
            sector('Dressed low masonry',1.68,1.86,.32+row*.46,.75+row*.46,a+.014,a+math.tau/12-.014,stone)
    cylinder('Low stone cornice',1.92,1.30,1.45,arch_stone,24)

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
bpy.context.preferences.filepaths.save_version=0
budgets=[]
for variant in [0,1]:
    for obj in list(bpy.data.objects):
        if obj.type=='MESH':bpy.data.objects.remove(obj,do_unlink=True)
    build_base_and_doors()
    manufactured_hood(variant)
    camera=scene.camera;target=Vector((0,0,1.14*HEIGHT))
    camera.location=target+Vector((-12,-12,math.sqrt(96)))
    camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.ortho_scale=6
    scene.render.resolution_x=1024;scene.render.resolution_y=1024;scene.render.resolution_percentage=100
    scene.render.film_transparent=True;scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
    scene.cycles.samples=48
    scene.render.filepath=str(OUT/f'variant-{variant}.png')
    bpy.context.view_layer.update()
    deps=bpy.context.evaluated_depsgraph_get();vertices=triangles=0;bounds=[]
    for obj in scene.objects:
        if obj.type!='MESH':continue
        evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh();mesh.calc_loop_triangles()
        vertices+=len(mesh.vertices);triangles+=len(mesh.loop_triangles)
        bounds.extend([obj.matrix_world@v.co for v in mesh.vertices]);evaluated.to_mesh_clear()
    screen=[world_to_camera_view(scene,camera,p) for p in bounds]
    projected=[min(p.x for p in screen),min(p.y for p in screen),max(p.x for p in screen),max(p.y for p in screen)]
    physical=[min(p.x for p in bounds),min(p.y for p in bounds),max(p.x for p in bounds),max(p.y for p in bounds)]
    height_range=[min(p.z for p in bounds)/HEIGHT,max(p.z for p in bounds)/HEIGHT]
    assert height_range[0]>=-.001 and height_range[1]<=2.2801,height_range
    assert min(projected)>=0 and max(projected)<=1,projected
    record=dict(variant=variant,blender=bpy.app.version_string,vertices=vertices,triangles=triangles,
        ortho_scale=6,left=-3,right=3,bottom=-1.86,top=4.14,render_size=1024,
        height_compensation=HEIGHT,height_range=height_range,physical_bounds=physical,
        projected_normalized_bounds=projected,collision_footprint='Existing radius2 base; original fire mouth geometry retained',
        exhaust_outlet=[0,0,2.28],service_socket='Runtime socket at1.8 unchanged; no socket baked into source',
        source='Native editable geometry; original study02 procedural materials/lights; no raster postprocessing',
        limitations='Static fixed-view source render, not a runtime mesh or Unity/device acceptance')
    (OUT/f'variant-{variant}-budget.json').write_text(json.dumps(record,indent=2)+'\n')
    bpy.ops.wm.save_as_mainfile(filepath=str(OUT/f'variant-{variant}.blend'))
    bpy.ops.render.render(write_still=True)
    budgets.append(record)
    print('RETORT_VARIANT_COMPLETE',variant,vertices,triangles,flush=True)
(OUT/'model-budget.json').write_text(json.dumps(budgets,indent=2)+'\n')
# Inspect the archived original source rather than assuming its overhang bounds.
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'ArtDirection/2026-10-09/short-boiler-01/short-boiler.blend'))
bpy.context.view_layer.update();deps=bpy.context.evaluated_depsgraph_get();old_bounds=[]
for obj in bpy.context.scene.objects:
    if obj.type!='MESH':continue
    evaluated=obj.evaluated_get(deps);mesh=evaluated.to_mesh()
    old_bounds.extend([obj.matrix_world@v.co for v in mesh.vertices]);evaluated.to_mesh_clear()
original=[min(p.x for p in old_bounds),min(p.y for p in old_bounds),max(p.x for p in old_bounds),max(p.y for p in old_bounds)]
for budget in budgets:
    assert all(abs(a-b)<.0001 for a,b in zip(budget['physical_bounds'],original)),(budget['physical_bounds'],original)
(OUT/'original-contract-comparison.json').write_text(json.dumps(dict(original_source='ArtDirection/2026-10-09/short-boiler-01/short-boiler.blend',
    original_physical_bounds=original,new_bounds_equal=True,original_camera_scale=bpy.context.scene.camera.data.ortho_scale,
    original_max_height=max(p.z for p in old_bounds)/HEIGHT,new_max_height=2.28,
    note='Original decorative fire arches overhang the radius2 plinth; preserved exactly, not new collision expansion.'),indent=2)+'\n')
