using UnityEngine;

namespace Cryptforge.UI
{
    // Frost melting into ash (docs/51 question 6): as the Descent lands on floor 3, the last of floor 2's frost recedes
    // from the screen's edges while ash flakes and a few gloamlight motes drift in from the sides and settle away, over
    // Duration seconds. Visual only, in the style of FrostTransitionView: one owned mesh on the camera (the frost ring and
    // the motes' quads), the arena's vertex-colour material, drawn after the world and under the HUD; no collider and no
    // canvas, so it never takes input. Its arrays are made once; a frame of the play writes into them and allocates
    // nothing. Unscaled time, each step capped. No orchid: the motes are ash #A7A99C and gloamlight #C6EE6A.
    public sealed class GloamTransitionView : MonoBehaviour
    {
        internal const string ObjectName="Gloam transition";
        internal const float Duration=.7f;
        internal const int SortingOrder=FrostTransitionView.SortingOrder;
        private const int Around=48;
        // Ash flakes and pollen: one in four lit (gloamlight), as the atmosphere's motes are. Enough, and large enough, to
        // read in the first 0.3 s; they come in from the sides and stop short of the middle third, where the hero stands.
        internal const int Motes=48;
        private const float MaxStep=1f/30;
        // The frost's reach at the start, as a share of the screen's shorter side (the frost transition's peak), and how
        // soon in the play it is gone.
        private const float Reach=.16f,Spike=.09f,FrostGone=.45f;
        private static readonly Color EdgeFrost=new Color(.75f,.9f,.97f),BodyFrost=new Color(.5f,.85f,1f);
        private static readonly Color Ash=new Color(167/255f,169/255f,156/255f),Gloamlight=new Color(198/255f,238/255f,106/255f);
        private Camera _camera;
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private Vector3[] _vertices;
        private Color[] _colours;
        private readonly Vector2[] _edge=new Vector2[Around],_inward=new Vector2[Around];
        private readonly float[] _spike=new float[Around];
        // Each mote's start (a share of the half-width and half-height, from a side), its drift and its size.
        private readonly Vector2[] _moteStart=new Vector2[Motes],_moteDrift=new Vector2[Motes];
        private readonly float[] _moteSize=new float[Motes],_moteDelay=new float[Motes];
        private float _elapsed;
        private float _framedSize=-1,_framedAspect=-1;

        internal bool IsPlaying{get;private set;}
        internal int Plays{get;private set;}
        // The frost's share left now, 1 at the start to 0 by FrostGone of the play.
        internal static float FrostLeft(float t)=>t<=0?1:t>=FrostGone?0:Mathf.SmoothStep(1,0,t/FrostGone);
        // A mote's strength at its own time u (0..1): in over the first fifth, held, out over the last 40 %.
        internal static float MoteEnvelope(float u)
        {
            if(u<=0||u>=1)return 0;
            return u<.2f?Mathf.SmoothStep(0,1,u/.2f):u<.6f?1:Mathf.SmoothStep(1,0,(u-.6f)/.4f);
        }

        internal void Build(Camera camera,Material material)
        {
            if(_mesh!=null)return;
            _camera=camera;
            for(int i=0;i<Around;i++)_spike[i]=(i%2==0?1f:.35f)*(.6f+.4f*Mathf.Abs(Mathf.Sin(i*2.39f)));
            for(int m=0;m<Motes;m++)
            {
                // From the left or the right edge (alternating), at a height spread over the screen, drifting inward and
                // a little down, as ash settles.
                float side=m%2==0?-1:1;
                _moteStart[m]=new Vector2(side*(.95f+.08f*Hash(m*3+1)),Mathf.Lerp(-.9f,.95f,Hash(m*7+2)));
                _moteDrift[m]=new Vector2(-side*(.3f+.3f*Hash(m*11+3)),-(.05f+.15f*Hash(m*13+4)));
                _moteSize[m]=(m%4==0?.017f:.012f)+.008f*Hash(m*17+5);
                _moteDelay[m]=.2f*Hash(m*19+6);
            }
            int ring=Around*3;
            _vertices=new Vector3[ring+Motes*4];
            _colours=new Color[_vertices.Length];
            var triangles=new int[Around*2*2*3+Motes*6];
            int at=0;
            for(int i=0;i<Around;i++)
            {
                int j=(i+1)%Around;
                for(int row=0;row<2;row++)
                {
                    int a=i*3+row,b=j*3+row;
                    triangles[at++]=a;triangles[at++]=b;triangles[at++]=b+1;
                    triangles[at++]=a;triangles[at++]=b+1;triangles[at++]=a+1;
                }
            }
            for(int m=0;m<Motes;m++)
            {
                int v=ring+m*4;
                triangles[at++]=v;triangles[at++]=v+1;triangles[at++]=v+2;
                triangles[at++]=v;triangles[at++]=v+2;triangles[at++]=v+3;
            }
            _mesh=new Mesh{name=ObjectName};
            _mesh.MarkDynamic();
            _mesh.vertices=_vertices;_mesh.colors=_colours;_mesh.triangles=triangles;
            _mesh.bounds=new Bounds(Vector3.zero,Vector3.one);
            gameObject.AddComponent<MeshFilter>().sharedMesh=_mesh;
            _renderer=gameObject.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial=material;_renderer.sortingOrder=SortingOrder;
            _renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;_renderer.receiveShadows=false;
            _renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;
            _renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;
            _renderer.enabled=false;
        }

        internal void Play()
        {
            if(_mesh==null)return;
            _elapsed=0;IsPlaying=true;Plays++;
            _renderer.enabled=true;
            Draw();
        }

        private void LateUpdate()
        {
            if(!IsPlaying)return;
            Advance(Mathf.Min(Time.unscaledDeltaTime,MaxStep));
        }

        internal void Advance(float seconds)
        {
            if(!IsPlaying)return;
            _elapsed+=seconds;
            if(_elapsed>=Duration){IsPlaying=false;_renderer.enabled=false;return;}
            Draw();
        }

        private void Draw()
        {
            if(_camera==null)return;
            Frame();
            float t=_elapsed/Duration;
            float side=Mathf.Min(_framedSize*2,_framedSize*2*_framedAspect);
            // A camera not yet sized (or a degenerate aspect) draws nothing rather than non-finite positions.
            if(!(side>0)||float.IsInfinity(side)||!(t>=0)){_renderer.enabled=false;return;}
            _renderer.enabled=true;
            // The frost recedes: its reach and its strength go together.
            float frost=FrostLeft(t);
            for(int i=0;i<Around;i++)
            {
                Vector2 edge=_edge[i],inward=_inward[i];
                float reach=side*frost*(Reach+Spike*_spike[i]);
                Vector2 body=edge+inward*reach*.45f,tip=edge+inward*reach;
                _vertices[i*3]=new Vector3(edge.x,edge.y,0);
                _vertices[i*3+1]=new Vector3(body.x,body.y,0);
                _vertices[i*3+2]=new Vector3(tip.x,tip.y,0);
                Color outer=EdgeFrost;outer.a=.62f*frost;
                Color mid=BodyFrost;mid.a=.3f*frost;
                Color inner=BodyFrost;inner.a=0;
                _colours[i*3]=outer;_colours[i*3+1]=mid;_colours[i*3+2]=inner;
            }
            // The motes drift in: each its own short life inside the play, a small diamond facing the camera.
            float halfX=_framedSize*_framedAspect,halfY=_framedSize;
            int ring=Around*3;
            for(int m=0;m<Motes;m++)
            {
                float u=Mathf.Clamp01((t-_moteDelay[m])/(1-_moteDelay[m]));
                float strength=MoteEnvelope(u);
                Vector2 p=_moteStart[m]+_moteDrift[m]*u;
                float x=p.x*halfX,y=p.y*halfY,r=side*_moteSize[m]*(.6f+.4f*strength);
                int v=ring+m*4;
                _vertices[v]=new Vector3(x,y+r,0);_vertices[v+1]=new Vector3(x+r*.7f,y,0);
                _vertices[v+2]=new Vector3(x,y-r,0);_vertices[v+3]=new Vector3(x-r*.7f,y,0);
                Color c=m%4==0?Gloamlight:Ash;c.a=(m%4==0?.95f:.85f)*strength;
                _colours[v]=c;_colours[v+1]=c;_colours[v+2]=c;_colours[v+3]=c;
            }
            _mesh.vertices=_vertices;_mesh.colors=_colours;
            // Explicit, finite bounds round the camera-local ring (the view rectangle plus its margin).
            _mesh.bounds=new Bounds(Vector3.zero,new Vector3(_framedSize*_framedAspect*2.4f,_framedSize*2.4f,.1f));
        }

        // The ring's outer edge on the camera's view rectangle, redone only when the camera's size or aspect changes.
        private void Frame()
        {
            transform.localPosition=new Vector3(0,0,_camera.nearClipPlane+.05f);
            transform.localRotation=Quaternion.identity;transform.localScale=Vector3.one;
            float size=_camera.orthographicSize,aspect=_camera.aspect;
            if(!(size>0)||!(aspect>0)||float.IsInfinity(size)||float.IsInfinity(aspect))return;
            if(Mathf.Approximately(size,_framedSize)&&Mathf.Approximately(aspect,_framedAspect))return;
            _framedSize=size;_framedAspect=aspect;
            for(int i=0;i<Around;i++)FrostTransitionView.RingPoint(i,Around,size,aspect,out _edge[i],out _inward[i]);
        }

        // A deterministic 0..1 from an integer (no System.Random in the game).
        private static float Hash(int n)
        {
            float s=Mathf.Sin(n*12.9898f+78.233f)*43758.5453f;
            return s-Mathf.Floor(s);
        }

        private void OnDestroy()
        {
            if(_mesh!=null)Destroy(_mesh);
        }
    }
}
