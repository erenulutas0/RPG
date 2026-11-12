using UnityEngine;

namespace Cryptforge.UI
{
    // The heat stopping at the stair (docs/49 question 5): as the Descent lands on floor 2, frost creeps in from the
    // screen's edges and melts away again over Duration seconds. One owned ring mesh on the camera, in the arena's
    // vertex-colour material, drawn after the world and under the HUD (every canvas is Screen Space - Overlay); no
    // collider and no canvas, so it never takes input. Its arrays are made once; a frame of the play writes into them and
    // allocates nothing. Unscaled time, each step capped, so a level-up opening at once or a long first frame on the new
    // floor neither freezes it nor skips it.
    public sealed class FrostTransitionView : MonoBehaviour
    {
        internal const string ObjectName="Frost transition";
        internal const float Duration=.6f;
        internal const int SortingOrder=32000;
        private const int Around=48;
        private const float MaxStep=1f/30;
        // The frost's reach in from the edge at its peak, as a share of the screen's shorter side, and the spikes' extra.
        private const float Reach=.16f,Spike=.09f;
        private static readonly Color EdgeFrost=new Color(.75f,.9f,.97f),BodyFrost=new Color(.5f,.85f,1f);
        private Camera _camera;
        private Mesh _mesh;
        private MeshRenderer _renderer;
        private Vector3[] _vertices;
        private Color[] _colours;
        private readonly Vector2[] _edge=new Vector2[Around],_inward=new Vector2[Around];
        private readonly float[] _spike=new float[Around];
        private float _elapsed;
        private float _framedSize=-1,_framedAspect=-1;

        internal bool IsPlaying{get;private set;}
        internal int Plays{get;private set;}
        // The frost's strength now, 0..1: in fast, out slower.
        internal float Strength=>IsPlaying?Envelope(_elapsed/Duration):0;
        internal static float Envelope(float t)
        {
            if(t<=0||t>=1)return 0;
            return t<.25f?Mathf.SmoothStep(0,1,t/.25f):Mathf.SmoothStep(1,0,(t-.25f)/.75f);
        }

        internal void Build(Camera camera,Material material)
        {
            if(_mesh!=null)return;
            _camera=camera;
            for(int i=0;i<Around;i++)_spike[i]=(i%2==0?1f:.35f)*(.6f+.4f*Mathf.Abs(Mathf.Sin(i*2.39f)));
            _vertices=new Vector3[Around*3];
            _colours=new Color[Around*3];
            var triangles=new int[Around*2*2*3];
            for(int i=0,at=0;i<Around;i++)
            {
                int j=(i+1)%Around;
                for(int row=0;row<2;row++)
                {
                    int a=i*3+row,b=j*3+row;
                    triangles[at++]=a;triangles[at++]=b;triangles[at++]=b+1;
                    triangles[at++]=a;triangles[at++]=b+1;triangles[at++]=a+1;
                }
            }
            _mesh=new Mesh{name=ObjectName};
            _mesh.MarkDynamic();
            _mesh.vertices=_vertices;_mesh.colors=_colours;_mesh.triangles=triangles;
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
            float strength=Envelope(_elapsed/Duration);
            // The frost creeps in fast and holds while it fades.
            float creep=1-Mathf.Pow(1-Mathf.Clamp01(_elapsed/(Duration*.4f)),3);
            float side=Mathf.Min(_framedSize*2,_framedSize*2*_framedAspect);
            for(int i=0;i<Around;i++)
            {
                Vector2 edge=_edge[i],inward=_inward[i];
                float reach=side*creep*(Reach+Spike*_spike[i]);
                Vector2 body=edge+inward*reach*.45f,tip=edge+inward*reach;
                _vertices[i*3]=new Vector3(edge.x,edge.y,0);
                _vertices[i*3+1]=new Vector3(body.x,body.y,0);
                _vertices[i*3+2]=new Vector3(tip.x,tip.y,0);
                Color outer=EdgeFrost;outer.a=.62f*strength;
                Color mid=BodyFrost;mid.a=.3f*strength;
                Color inner=BodyFrost;inner.a=0;
                _colours[i*3]=outer;_colours[i*3+1]=mid;_colours[i*3+2]=inner;
            }
            _mesh.vertices=_vertices;_mesh.colors=_colours;
            _mesh.RecalculateBounds();
        }

        // The ring's outer edge on the camera's view rectangle (a little past it), and each point's way in, redone only
        // when the camera's size or aspect changes. Placed just past the near plane, straight in front of the camera.
        private void Frame()
        {
            transform.localPosition=new Vector3(0,0,_camera.nearClipPlane+.05f);
            transform.localRotation=Quaternion.identity;transform.localScale=Vector3.one;
            float size=_camera.orthographicSize,aspect=_camera.aspect;
            if(Mathf.Approximately(size,_framedSize)&&Mathf.Approximately(aspect,_framedAspect))return;
            _framedSize=size;_framedAspect=aspect;
            float halfY=size*1.04f,halfX=size*aspect*1.04f;
            const int perSide=Around/4;
            for(int i=0;i<Around;i++)
            {
                // Round the rectangle, a quarter of the points to a side, each side starting at its corner so the ring
                // closes over all four.
                float f=(float)(i%perSide)/perSide,x,y;Vector2 inward;
                switch(i/perSide)
                {
                    case 0:x=Mathf.Lerp(-halfX,halfX,f);y=-halfY;inward=Vector2.up;break;
                    case 1:x=halfX;y=Mathf.Lerp(-halfY,halfY,f);inward=Vector2.left;break;
                    case 2:x=Mathf.Lerp(halfX,-halfX,f);y=halfY;inward=Vector2.down;break;
                    default:x=-halfX;y=Mathf.Lerp(halfY,-halfY,f);inward=Vector2.right;break;
                }
                _edge[i]=new Vector2(x,y);
                // Corners lean in on both axes, so the frost thickens there as frost on glass does.
                float cx=Mathf.Abs(x)/halfX,cy=Mathf.Abs(y)/halfY;
                if(cx>.85f&&cy>.85f)inward=new Vector2(-Mathf.Sign(x),-Mathf.Sign(y))*1.2f;
                _inward[i]=inward;
            }
        }

        private void OnDestroy()
        {
            if(_mesh!=null)Destroy(_mesh);
        }
    }
}
