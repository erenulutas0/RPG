"""Read-only float32 reconstruction: tuple double sums versus stored Vector3 sums."""
import itertools
import json
import struct

def f(x): return struct.unpack('f',struct.pack('f',x))[0]
def area(p): return sum(p[i][0]*p[(i+1)%len(p)][1]-p[i][1]*p[(i+1)%len(p)][0] for i in range(len(p)))*.5
def clip(source,window):
    out=list(source); sign=1 if area(window)>0 else -1
    for a,b in zip(window,window[1:]+window[:1]):
        old=out;out=[]
        if not old:break
        side=lambda p:sign*((b[0]-a[0])*(p[1]-a[1])-(b[1]-a[1])*(p[0]-a[0]))
        prev=old[-1];d0=side(prev)
        for p in old:
            d1=side(p)
            if (d1>=0)!=(d0>=0):
                t=d0/(d0-d1);out.append((prev[0]+(p[0]-prev[0])*t,prev[1]+(p[1]-prev[1])*t))
            if d1>=0:out.append(p)
            prev=p;d0=d1
    return out

expected=.29114001144611734
actual=.29113985198513603
best=[]
for project_steps,lerp_steps,taper_steps,centre_steps in itertools.product([False,True],repeat=4):
    cast=f if project_steps else lambda x:x
    def project(u,v):
        return (f(cast(u-v)*f(.70710678)),f(cast(cast(cast(u+v)*f(.70710678))*.5)+1.25))
    def lerp(a,b,t): return f(a+((f(b-a) if lerp_steps else b-a)*t))
    u0=f(f(32.2)-4);u1=f(f(35.84)-4);v0=f(13+f(.16));v1=f(f(16.6)-f(.16))
    low=f(3.53);high=f(6.12);h=f(high-low);centre=f(.155);half=f(.085)
    shape=[(f(centre-half),low),(f(centre+half),low),(f(centre+half),f(high-h*f(.27))),
           (centre,high),(f(centre-half),f(high-h*f(.27)))]
    throat=[]
    for x,z in shape:
        x=lerp(x,centre,f(.25))
        middle=f(low+(f(h*f(.46)) if centre_steps else h*f(.46)))
        z=lerp(z,middle,f(.13))
        inv=f((f(z-f(3.13)) if taper_steps else z-f(3.13))/(f(f(6.55)-f(3.13)) if taper_steps else f(6.55)-f(3.13)))
        taper=f(f(.05)*inv)
        p=project(f(u0+taper),lerp(f(v0+taper),f(v1-taper),x))
        throat.append((p[0],f(p[1]+z)))
    a=project(f(u0+f(.18)),v0);b=project(u0,v0)
    delta=(f(a[0]-b[0]),f(a[1]-b[1]))
    unrounded=[(p[0]+delta[0],p[1]+delta[1]) for p in throat]
    rounded=[(f(p[0]+delta[0]),f(p[1]+delta[1])) for p in throat]
    old_visible=clip(unrounded,throat)
    old=abs(area(old_visible));stored=abs(area(clip(rounded,throat)))
    overlap=abs(area(clip(old_visible,rounded)))
    best.append(dict(error=abs(old-expected)+abs(overlap-actual),modes=[project_steps,lerp_steps,taper_steps,centre_steps],
                     tuple_double_area=old,stored_vector_area=stored,area_difference=old-stored,
                     rounded_geometry_overlap_in_old_oracle=overlap,
                     translation=list(delta),throat=throat,tuple_rear=unrounded,stored_rear=rounded))
best.sort(key=lambda x:x['error'])
print(json.dumps(best[:2],indent=2))
