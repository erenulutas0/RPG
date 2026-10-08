using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only. The encounter owns position, dwell and success; this view never advances an exit timer.
    [DefaultExecutionOrder(100)]
    public sealed class ExitSealView : MonoBehaviour
    {
        private const int Segments = 64;
        private CombatSetup _setup;
        private PrototypeTextDefinition _text;
        private Camera _camera;
        private ArenaCameraFollow _framing;
        private ArenaView _arena;
        private GameObject _plate;
        private LineRenderer _progress;
        private LineRenderer _arrow;
        private Material _material;
        private TextMesh _label;
        private readonly Vector3[] _arc = new Vector3[Segments + 1];
        private readonly Vector3[] _corners = new Vector3[4];
        public float ShownProgress { get; private set; }
        public bool IsVisible => _plate != null && _plate.activeSelf;
        public bool ShowsDirection => _arrow != null && _arrow.enabled;
        public Vector3 ExitPosition => _plate.transform.position;

        internal void Initialize(CombatSetup setup, PrototypeTextDefinition text)
        {
            _setup = setup; _text = text; _camera = Camera.main;
            _framing = _camera.GetComponent<ArenaCameraFollow>();
            _arena = FindFirstObjectByType<ArenaView>();
            _material = new Material(Shader.Find("Sprites/Default"));
            _plate = new GameObject("Finale Exit Seal"); _plate.transform.SetParent(transform, false);
            var brass = new Color(.78f,.63f,.36f,1);
            var ivory = new Color(.78f,.93f,.87f,1);
            var boundary = Line("Exit Boundary", _plate.transform, brass, .025f, 0);
            for (int i=0;i<=Segments;i++) _arc[i]=Point(i*(Mathf.PI*2/Segments),ExitRule.Radius);
            boundary.positionCount=_arc.Length; boundary.SetPositions(_arc);
            for(int i=0;i<4;i++)
            {
                var corner=Line("Brass Corner",_plate.transform,brass,.05f,0);
                float angle=Mathf.PI/4+i*Mathf.PI/2;
                Vector2 a=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                Vector2 b=new Vector2(-a.y,a.x);
                Vector2 centre=a*.72f;
                corner.positionCount=3;
                corner.SetPosition(0,Project(centre-a*.12f-b*.13f));
                corner.SetPosition(1,Project(centre+b*0));
                corner.SetPosition(2,Project(centre-a*.12f+b*.13f));
            }
            var gate=Line("Gateway",_plate.transform,ivory,.05f,0);
            gate.positionCount=4;gate.SetPositions(new[]{Project(new Vector2(-.23f,-.25f)),Project(new Vector2(-.23f,.28f)),Project(new Vector2(.23f,.28f)),Project(new Vector2(.23f,-.25f))});
            var glyph=Line("Gateway Arrow",_plate.transform,ivory,.04f,0);
            glyph.positionCount=5;glyph.SetPositions(new[]{Project(new Vector2(0,-.18f)),Project(new Vector2(0,.12f)),Project(new Vector2(-.10f,.01f)),Project(new Vector2(0,.12f)),Project(new Vector2(.10f,.01f))});
            _progress=Line("Exit Hold",_plate.transform,new Color(.56f,.95f,.80f),.055f,0);
            _arrow=Line("Exit Direction",transform,ivory,.04f,100);_arrow.positionCount=3;
            var labelObject=new GameObject("Exit Instruction");labelObject.transform.SetParent(transform,false);
            _label=labelObject.AddComponent<TextMesh>();_label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.GetComponent<MeshRenderer>().sharedMaterial=_label.font.material;
            _label.GetComponent<MeshRenderer>().sortingOrder=100;
            _label.anchor=TextAnchor.MiddleCenter;_label.alignment=TextAlignment.Center;_label.fontSize=48;_label.characterSize=.045f;_label.color=ivory;
            Refresh();
        }

        private LineRenderer Line(string name,Transform parent,Color colour,float width,int order)
        {
            var line=new GameObject(name).AddComponent<LineRenderer>();line.transform.SetParent(parent,false);
            line.sharedMaterial=_material;line.useWorldSpace=false;line.startColor=line.endColor=colour;
            line.startWidth=line.endWidth=width;line.sortingOrder=order;line.numCapVertices=2;return line;
        }
        private static Vector3 Project(Vector2 floor)=>new Vector3(floor.x,ArenaFloor.WorldY(floor.y),0);
        private static Vector3 Point(float angle,float radius)=>Project(new Vector2(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius));
        private void LateUpdate()=>Refresh();
        private void Refresh()
        {
            if(_plate==null)return;
            var encounter=_setup.Encounters;
            bool visible=enabled&&encounter.IsInSwarmRoom&&!encounter.IsFloorCleared&&!_setup.Run.HasEnded;
            _plate.SetActive(visible);_label.gameObject.SetActive(visible);_arrow.enabled=false;
            ShownProgress=visible?Mathf.Clamp01(encounter.ExitHeldSeconds/ExitRule.DwellSeconds):0;
            if(!visible)return;
            Vector3 position=new Vector3(encounter.ExitFloorX,ArenaFloor.WorldY(encounter.ExitFloorY),0);
            if(_arena!=null&&_arena.IsFoundryRegion)position.y+=FoundrySurface.Lift(position,_arena.transform.position);
            _plate.transform.position=position;
            _progress.enabled=ShownProgress>0;
            for(int i=0;i<=Segments;i++)_arc[i]=Point(Mathf.PI/2-i*(Mathf.PI*2/Segments)*ShownProgress,ExitRule.Radius);
            _progress.positionCount=_arc.Length;_progress.SetPositions(_arc);
            Vector3 viewport=_camera.WorldToViewportPoint(position);
            float bottom=.20f,top=.80f;
            if(_framing!=null)
            {
                _framing.TopHud.GetWorldCorners(_corners);top=_corners[0].y/Screen.height-.04f;
                _framing.BottomHud.GetWorldCorners(_corners);bottom=_corners[1].y/Screen.height+.04f;
                if(top<=bottom){bottom=.20f;top=.80f;}
            }
            bool outside=viewport.x<.08f||viewport.x>.92f||viewport.y<bottom||viewport.y>top;
            Vector3 cue=position;
            if(outside)
            {
                Vector3 edge=new Vector3(Mathf.Clamp(viewport.x,.08f,.92f),Mathf.Clamp(viewport.y,bottom,top),viewport.z);
                cue=_camera.ViewportToWorldPoint(edge);cue.z=0;
                Vector3 direction=(position-cue).normalized,side=new Vector3(-direction.y,direction.x,0);
                _arrow.transform.position=cue;_arrow.enabled=true;
                _arrow.SetPosition(0,-direction*.16f+side*.10f);_arrow.SetPosition(1,Vector3.zero);_arrow.SetPosition(2,-direction*.16f-side*.10f);
            }
            _label.transform.position=cue+Vector3.down*.45f;
            string instruction=outside?_text.ExitDirection:_text.ExitInstruction;
            if(_label.text!=instruction)_label.text=instruction;
        }
        private void OnDisable(){if(_plate!=null)_plate.SetActive(false);if(_label!=null)_label.gameObject.SetActive(false);if(_arrow!=null)_arrow.enabled=false;}
        private void OnDestroy(){if(_material!=null)Destroy(_material);if(_plate!=null)Destroy(_plate);if(_arrow!=null)Destroy(_arrow.gameObject);if(_label!=null)Destroy(_label.gameObject);}
    }
}
