using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // One bounded dynamic mesh, shared vertex-colour material, no particle objects or textures.
    // All sources belong to scenery. Scaled time freezes smoke, embers, sparks and light with combat.
    public sealed class FoundryAtmosphereView : MonoBehaviour
    {
        private const int Segments=12;
        private const int VerticesPerDisc=Segments+1;
        private const int SmokeCount=24;
        private const int EmberCount=12;
        private const int GlowCount=4;
        // The furnace pit (DrawPit): a glow over the molten pool, faint wisps of heat and rising sparks, in that draw order.
        private const int HeatCount=3;
        internal const int SparkCount=20;
        private const int PitGlow=SmokeCount+EmberCount+GlowCount,FirstHeat=PitGlow+1,FirstSpark=FirstHeat+HeatCount;
        private const int Discs=FirstSpark+SparkCount;
        // The pit, after FoundryRegionMassing (PitCentre, PitMolten, PitBoilers, and the collar band's inner edge - the
        // opening, a rounded square PitHalf-PitCollar in half-size with corners of radius PitRound-RailDepth). Offsets run in
        // design units across the screen (+x) and into it (along u+v) from its centre. The pool lies PitPool below the deck
        // between the two inner boilers, whose drums' silhouettes lie 1.26 to either side of the centre column below the
        // deck and their domes' 1.16 above it; the opening's far edge there is its corner arc, centred PitArc in along the
        // diagonal, of radius PitArcRadius.
        private static readonly Vector2 PitCentre=new Vector2((FoundryMap.PitWest+FoundryMap.PitEast)*.5f,(FoundryMap.PitSouth+FoundryMap.PitNorth)*.5f);
        private const float PitPool=FoundryRegionView.PitMolten,PitArc=3.522f,PitArcRadius=2.96f,SparkAcross=1.12f;
        // The pit's glow, heat and sparks are depth-tested against the structures (FoundryDepth), as actors are; the rest of
        // the mesh stays flat at z 0, over every structure. All take one depth, that of screen height 0 (about where the pool
        // shows past the near rail): behind the inner boilers' domes (their footprints' diagonal lies at -.71), the near
        // collar and its lantern (-1.5 and down), which hide them, and in front of every piece of the far collar over these
        // columns (.1 and up; .7 and up behind the pool), which they rise past. The lining, the pool and the drums below the
        // deck write no depth.
        private static readonly float PitZ=FoundryDepth.Z(0);
        private static readonly Color PitGlowTone=new Color(1.4f,.6f,.18f);
        private static readonly Color HeatHot=new Color(1.35f,.6f,.2f),HeatCool=new Color(1f,.47f,.24f);
        // Sparks are born yellow-white and cool to orange; both under the sprite shader's 2x colour ceiling.
        private static readonly Color SparkHot=new Color(1.85f,1.4f,.6f),SparkCool=new Color(1.55f,.55f,.14f);
        private readonly Vector3[] _sources=new Vector3[4];
        private readonly Vector2[] _circle=new Vector2[Segments];
        private readonly float[] _smokeRadii=new float[SmokeCount*Segments];
        private Vector3[] _vertices;
        private Color[] _colors;
        private Mesh _mesh;
        private float _elapsed;
        private float _nextFrame;

        public void Build(Material material)
        {
            // Smoke rises from the round boilers' flue mouths.
            for(int i=0;i<4;i++)_sources[i]=FoundryRegionView.BoilerFlue(i);
            for(int i=0;i<Segments;i++)
                _circle[i]=new Vector2(Mathf.Cos(i*Mathf.PI*2/Segments),Mathf.Sin(i*Mathf.PI*2/Segments));
            // Stable soft lobes, authored once rather than randomising the outline each frame.
            for(int puff=0;puff<SmokeCount;puff++)for(int i=0;i<Segments;i++)
            {
                float angle=i*Mathf.PI*2/Segments;
                _smokeRadii[puff*Segments+i]=1+.13f*Mathf.Sin(angle*3+puff*2.37f)
                    +.08f*Mathf.Sin(angle*2-puff*1.41f);
            }
            _vertices=new Vector3[Discs*VerticesPerDisc];
            _colors=new Color[_vertices.Length];
            var triangles=new int[Discs*Segments*3];
            for(int disc=0;disc<Discs;disc++)for(int i=0;i<Segments;i++)
            {
                int at=(disc*Segments+i)*3,vertex=disc*VerticesPerDisc;
                triangles[at]=vertex;triangles[at+1]=vertex+1+i;
                triangles[at+2]=vertex+1+(i+1)%Segments;
            }
            _mesh=new Mesh{name="Foundry atmosphere owned mesh"};
            _mesh.MarkDynamic();
            Draw(0);
            _mesh.SetTriangles(triangles,0);
            // Covers all authored sources and their bounded drift, independent of camera movement, from the flat layer back
            // to the pit's depth.
            _mesh.bounds=new Bounds(new Vector3(6,10,PitZ*.5f),new Vector3(65,45,PitZ+2));
            gameObject.AddComponent<MeshFilter>().sharedMesh=_mesh;
            var renderer=gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;renderer.sortingOrder=-2;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        private void Update()
        {
            if(_mesh==null||Time.deltaTime<=0)return;
            _elapsed+=Time.deltaTime;
            if(_elapsed<_nextFrame)return;
            _nextFrame=_elapsed+1f/30;
            Draw(_elapsed);
        }

        private void Draw(float time)
        {
            for(int i=0;i<SmokeCount;i++)
            {
                int chimney=i/6;
                float age=Mathf.Repeat(time/(5.5f+chimney*.21f)+(i%6)/6f+chimney*.137f,1);
                float fade=Mathf.Sin(age*Mathf.PI)*(1-age*.3f);
                float bend=age*(.52f+Mathf.Sin(age*4.5f+chimney)*.19f);
                Vector3 p=_sources[chimney]+new Vector3(bend,age*2.65f,0);
                float spread=1+.1f*Mathf.Sin(i*2.37f);
                Color smoke=Color.Lerp(new Color(.62f,.48f,.36f),new Color(.57f,.60f,.67f),
                    Mathf.Clamp01(age*2.8f));
                smoke.a=fade*.40f;
                Smoke(i,p,(.18f+age*.7f)*spread,(.23f+age*.64f)/spread,age,smoke);
            }
            for(int i=0;i<EmberCount;i++)
            {
                int source=i/3;
                float age=Mathf.Repeat(time/(2.4f+(i%3)*.35f)+i*.618034f,1);
                float fade=Mathf.Sin(age*Mathf.PI);
                float drift=Mathf.Sin(i*2.1f)*.36f+Mathf.Sin(age*4+i)*age*.19f;
                Vector3 p=_sources[source]+new Vector3(drift,age*1.3f,0);
                Disc(SmokeCount+i,p,.023f,.048f,new Color(1,.43f+fade*.25f,.12f,fade*.85f));
            }
            for(int i=0;i<GlowCount;i++)
            {
                float pulse=.85f+.1f*Mathf.Sin(time*2.2f+i)+.05f*Mathf.Sin(time*4.7f+i*3);
                Disc(SmokeCount+EmberCount+i,_sources[i],.4f,.16f,new Color(1,.32f,.065f,.15f*pulse));
            }
            DrawPit(time);
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
        }

        // The furnace pit, over its molten pool between the inner boilers.
        private void DrawPit(float time)
        {
            // A warm glow over the pool and the lining's foot, breathing with the fire.
            Color glow=PitGlowTone;
            glow.a=.085f*(.86f+.09f*Mathf.Sin(time*1.7f)+.05f*Mathf.Sin(time*4.3f+1.3f));
            Disc(PitGlow,PitPoint(0,1.9f,1.45f-PitPool),2.05f,1.2f,glow);
            // Wisps of heat over the pool's far half, rising slowly past the deck as they spread, cool and fade.
            for(int i=0;i<HeatCount;i++)
            {
                float clock=time/(4.8f+i*.37f)+i/(float)HeatCount;
                int life=Mathf.FloorToInt(clock);
                float age=clock-life,fade=Mathf.Sin(age*Mathf.PI);
                float across=(Hash(i,life,5)-.5f)*1.1f+.07f*Mathf.Sin(time*1.3f+i*2.3f);
                Color heat=Color.Lerp(HeatHot,HeatCool,age);
                heat.a=fade*fade*.07f*(.85f+.15f*Mathf.Sin(time*2.7f+i*2.1f));
                Heat(FirstHeat+i,PitPoint(across,Mathf.Lerp(1.7f,2.7f,Hash(i,life,6)),age*(PitPool+1.1f)-PitPool+.5f),.3f+age*.55f,
                    .42f+age*.4f,(i*7+3)%SmokeCount,time,heat);
            }
            // Sparks, each born at a fresh point of the pool every life (SparkSpawn): a slowing climb from the pool to 2.2-3.2
            // above the deck with a little drift and flutter - which keep it inside the drums' silhouettes for the first third
            // of its life, while it is below the deck - cooling from yellow-white to orange as it shrinks, winking out over its
            // last 40%.
            for(int i=0;i<SparkCount;i++)
            {
                float clock=time/Mathf.Lerp(2.3f,3.1f,Hash(i,0,0))+i*.618034f;
                int life=Mathf.FloorToInt(clock);
                float age=clock-life,heat=1-age;
                Vector2 spawn=SparkSpawn(i,life);
                float climb=Mathf.Lerp(PitPool+2.2f,PitPool+3.2f,Hash(i,life,3))*age*(1.5f-.5f*age);
                float drift=(Hash(i,life,4)-.5f)*.6f*age*age+.05f*age*Mathf.Sin(age*15.7f+i*1.7f);
                Vector3 p=Project(spawn.x,spawn.y)+new Vector3(drift,climb-PitPool,0);
                p.z=PitZ;
                Color spark=Color.Lerp(SparkCool,SparkHot,heat*heat);
                spark.a=.95f*Mathf.Clamp01(age*16)*(1-Ease(.6f,1,age))*(.82f+.18f*Mathf.Sin(time*21+i*2.9f));
                float size=Mathf.Lerp(.017f,.03f,heat);
                Disc(FirstSpark+i,p,size,size*Mathf.Lerp(1.35f,2.3f,heat),spark);
            }
        }

        private void Smoke(int index,Vector3 center,float width,float height,float age,Color color)
        {
            int first=index*VerticesPerDisc;
            // An offset core and a slowly turning, sheared contour avoid a stack of round dots.
            _vertices[first]=center+new Vector3(-width*.12f,height*.06f,0);
            _colors[first]=color;
            Color clear=new Color(color.r,color.g,color.b,0);
            float rotation=index*1.73f+age*.6f;
            float cos=Mathf.Cos(rotation),sin=Mathf.Sin(rotation);
            for(int i=0;i<Segments;i++)
            {
                float radius=_smokeRadii[index*Segments+i];
                float x=(_circle[i].x*cos-_circle[i].y*sin)*radius;
                float y=(_circle[i].x*sin+_circle[i].y*cos)*radius;
                _vertices[first+i+1]=center+new Vector3((x+y*.16f)*width,y*height,0);
                _colors[first+i+1]=clear;
            }
        }

        // A wisp of heat: a soft lobed ellipse (a smoke puff's lobes) whose outline ripples sideways with height and time,
        // as air over the pool wavers.
        private void Heat(int index,Vector3 center,float width,float height,int lobes,float time,Color color)
        {
            int first=index*VerticesPerDisc;
            _vertices[first]=center;_colors[first]=color;
            Color clear=new Color(color.r,color.g,color.b,0);
            for(int i=0;i<Segments;i++)
            {
                float radius=_smokeRadii[lobes*Segments+i],y=_circle[i].y*radius;
                float x=_circle[i].x*radius+.16f*Mathf.Sin(y*3.4f-time*2.6f+index*1.9f);
                _vertices[first+i+1]=center+new Vector3(x*width,y*height,0);
                _colors[first+i+1]=clear;
            }
        }

        private void Disc(int index,Vector3 center,float width,float height,Color color)
        {
            int first=index*VerticesPerDisc;
            _vertices[first]=center;_colors[first]=color;
            Color clear=new Color(color.r,color.g,color.b,0);
            for(int i=0;i<Segments;i++)
            {
                _vertices[first+i+1]=center+new Vector3(_circle[i].x*width,_circle[i].y*height,0);
                _colors[first+i+1]=clear;
            }
        }

        // Where spark `spark` is born in its life `life`, in design units: on the pool, at most SparkAcross to either side of
        // the pit's centre column - inside the drums' silhouettes - and from .8 beyond the centre (into the screen) to .35
        // short of the lining's foot. All of it lies inside the rounded-square opening, never over walkable
        // court.
        internal static Vector2 SparkSpawn(int spark,int life)
        {
            float across=(Hash(spark,life,1)*2-1)*SparkAcross;
            float far=PitArc+Mathf.Sqrt(PitArcRadius*PitArcRadius-across*across)-.35f;
            return PitDesign(across,Mathf.Lerp(.8f,far,Hash(spark,life,2)));
        }

        // A point over the pit, height above the deck, from its offsets across the screen and into it; at the pit's depth.
        private static Vector3 PitPoint(float across,float deep,float height)
        {
            Vector2 design=PitDesign(across,deep);
            Vector3 p=Project(design.x,design.y);
            return new Vector3(p.x,p.y+height,PitZ);
        }
        private static Vector2 PitDesign(float across,float deep)=>PitCentre+new Vector2(deep+across,deep-across)*FoundryMap.Axis;

        // A stable value in [0, 1) for a pit element, its life and a salt: integer mixing, so no state, no allocation and
        // the same stream on every device.
        private static float Hash(int element,int life,int salt)
        {
            unchecked
            {
                uint h=(uint)element*0x9E3779B1u^(uint)life*0x85EBCA77u^(uint)salt*0xC2B2AE3Du;
                h^=h>>16;h*=0x7FEB352Du;h^=h>>15;h*=0x846CA68Bu;h^=h>>16;
                return (h>>8)*(1f/16777216f);
            }
        }

        // A Hermite ramp of x from 0 at edge0 to 1 at edge1, as the shading languages' smoothstep (Mathf.SmoothStep is not).
        private static float Ease(float edge0,float edge1,float x)
        {
            float t=Mathf.Clamp01((x-edge0)/(edge1-edge0));
            return t*t*(3-2*t);
        }

        private static Vector3 Project(float u,float v)
        {
            return FoundrySurface.Project(u,v);
        }
        private void OnDestroy(){if(_mesh!=null)Destroy(_mesh);}
    }
}
