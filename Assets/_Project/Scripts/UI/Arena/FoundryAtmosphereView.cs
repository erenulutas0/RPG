using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // One bounded dynamic mesh, shared vertex-colour material, no particle objects or textures.
    // All sources belong to scenery. Scaled time freezes smoke, embers and light with combat.
    public sealed class FoundryAtmosphereView : MonoBehaviour
    {
        private const int Segments=12;
        private const int VerticesPerDisc=Segments+1;
        private const int SmokeCount=24;
        private const int EmberCount=30;
        private const int GlowCount=6;
        private readonly Vector3[] _sources=new Vector3[6];
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
            _sources[4]=Project(-10,7)+Vector3.down*.5f;
            _sources[5]=Project(-6,7)+Vector3.down*.5f;
            for(int i=0;i<Segments;i++)
                _circle[i]=new Vector2(Mathf.Cos(i*Mathf.PI*2/Segments),Mathf.Sin(i*Mathf.PI*2/Segments));
            // Stable soft lobes, authored once rather than randomising the outline each frame.
            for(int puff=0;puff<SmokeCount;puff++)for(int i=0;i<Segments;i++)
            {
                float angle=i*Mathf.PI*2/Segments;
                _smokeRadii[puff*Segments+i]=1+.13f*Mathf.Sin(angle*3+puff*2.37f)
                    +.08f*Mathf.Sin(angle*2-puff*1.41f);
            }
            int discs=SmokeCount+EmberCount+GlowCount;
            _vertices=new Vector3[discs*VerticesPerDisc];
            _colors=new Color[_vertices.Length];
            var triangles=new int[discs*Segments*3];
            for(int disc=0;disc<discs;disc++)for(int i=0;i<Segments;i++)
            {
                int at=(disc*Segments+i)*3,vertex=disc*VerticesPerDisc;
                triangles[at]=vertex;triangles[at+1]=vertex+1+i;
                triangles[at+2]=vertex+1+(i+1)%Segments;
            }
            _mesh=new Mesh{name="Foundry atmosphere owned mesh"};
            _mesh.MarkDynamic();
            Draw(0);
            _mesh.SetTriangles(triangles,0);
            // Covers all authored sources and their bounded drift, independent of camera movement.
            _mesh.bounds=new Bounds(new Vector3(6,10,0),new Vector3(65,45,2));
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
                int source=i<12?i/3:4+(i%2);
                float age=Mathf.Repeat(time/(2.4f+(i%3)*.35f)+i*.618034f,1);
                float fade=Mathf.Sin(age*Mathf.PI);
                float drift=Mathf.Sin(i*2.1f)*.36f+Mathf.Sin(age*4+i)*age*.19f;
                Vector3 p=_sources[source]+new Vector3(drift,age*(source<4?1.3f:1.8f),0);
                Disc(SmokeCount+i,p,.023f,.048f,new Color(1,.43f+fade*.25f,.12f,fade*.85f));
            }
            for(int i=0;i<GlowCount;i++)
            {
                float pulse=.85f+.1f*Mathf.Sin(time*2.2f+i)+.05f*Mathf.Sin(time*4.7f+i*3);
                Disc(SmokeCount+EmberCount+i,_sources[i],i<4?.4f:.75f,i<4?.16f:.3f,
                    new Color(1,.32f,.065f,.15f*pulse));
            }
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
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

        private static Vector3 Project(float u,float v)
        {
            FoundryMap.ToFloor(u,v,out float x,out float y);
            return new Vector3(x,y*.5f,0);
        }
        private void OnDestroy(){if(_mesh!=null)Destroy(_mesh);}
    }
}
