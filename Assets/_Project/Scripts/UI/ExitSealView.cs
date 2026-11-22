using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Localisation;
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
        private MeshRenderer _labelRenderer;
        // What the label was last given. TextMesh.text is read from the native text, a new string on every read, so the
        // view never reads it back: a frame with the seal or a marked hall on show then allocates nothing.
        private string _shownInstruction;
        private bool _instructionShown;
        private readonly Vector3[] _arc = new Vector3[Segments + 1];
        private readonly Vector3[] _corners = new Vector3[4];
        public float ShownProgress { get; private set; }
        public bool IsVisible => _plate != null && _plate.activeSelf;
        public bool ShowsDirection => _arrow != null && _arrow.enabled;
        // The label's box in the world while it names the way off the screen (OutworksView lays its own arrows round it).
        public Bounds DirectionLabelBounds { get; private set; }
        public Vector3 ExitPosition => _plate.transform.position;

        internal void Initialize(CombatSetup setup, PrototypeTextDefinition text)
        {
            _setup = setup; _text = text; _camera = Camera.main;
            _framing = _camera.GetComponent<ArenaCameraFollow>();
            _arena = FindFirstObjectByType<ArenaView>();
            _material = SharedSpriteMaterial.For(_arena);
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
            _label.GetComponent<MeshRenderer>().sortingOrder=100;_labelRenderer=_label.GetComponent<MeshRenderer>();
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
        private void LateUpdate(){using(PerformanceMarkers.LateExitSeal.Auto())Refresh();}
        internal void Refresh()
        {
            if(_plate==null)return;
            var encounter=_setup.Encounters;
            bool hall=encounter.IsRoomSiteOpen;
            // docs/62 P0a's Ascent Stair, once the Keeper's key or the Bell opens it: the gate's seal, held as the gate is.
            bool stair=encounter.IsStairOpen;
            bool visible=enabled&&(encounter.IsExitOpen||hall||stair)&&!_setup.Run.HasEnded;
            _plate.SetActive(visible);_label.gameObject.SetActive(visible);_arrow.enabled=false;
            ShownProgress=visible&&!hall?Mathf.Clamp01((stair?encounter.Outworks.StairHeld:encounter.ExitHeldSeconds)/ExitRule.DwellSeconds):0;
            if(!visible)return;
            // A marked hall is entered, not held: the same seal at its district, at the district's radius.
            Vector3 position=hall?new Vector3(encounter.RoomSiteFloorX,ArenaFloor.WorldY(encounter.RoomSiteFloorY),0):
                stair?new Vector3(encounter.Outworks.StairX,ArenaFloor.WorldY(encounter.Outworks.StairY),0):
                new Vector3(encounter.ExitFloorX,ArenaFloor.WorldY(encounter.ExitFloorY),0);
            if(_arena!=null&&_arena.IsFoundryRegion)position.y+=FoundrySurface.Lift(position,_arena.transform.position);
            _plate.transform.position=position;
            // The seal is drawn at the finale's radius; the opened gate's wider hold scales the whole plate.
            float scale=(hall?FoundryMap.RoomSiteRadius:stair?OutworksRule.StairRadius:encounter.ExitRadius)/ExitRule.Radius;_plate.transform.localScale=new Vector3(scale,scale,1);
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
            bool gate=encounter.IsGateExitOpen;
            string instruction=hall?(outside?_text.HallDirection:_text.HallInstruction):
                stair?(outside?Loc.Text("outworks.stair_direction","ASCENT STAIR"):Loc.Text("outworks.stair_instruction","ASCENT STAIR - Stand still to climb")):
                outside?(gate?_text.GateDirection:_text.ExitDirection):(gate?_text.GateInstruction:_text.ExitInstruction);
            if(!_instructionShown||_shownInstruction!=instruction){_label.text=instruction;_shownInstruction=instruction;_instructionShown=true;}
            DirectionLabelBounds=default;
            if(outside)
            {
                Vector3 edge=new Vector3(Mathf.Clamp(viewport.x,.08f,.92f),Mathf.Clamp(viewport.y,bottom,top),viewport.z);
                Vector3 cue=_camera.ViewportToWorldPoint(edge);cue.z=0;
                Vector3 direction=(position-cue).normalized,side=new Vector3(-direction.y,direction.x,0);
                _arrow.transform.position=cue;_arrow.enabled=true;
                _arrow.SetPosition(0,-direction*.16f+side*.10f);_arrow.SetPosition(1,Vector3.zero);_arrow.SetPosition(2,-direction*.16f-side*.10f);
                // The way's name on the arrow's inner side, whole inside the safe area (2026-11-23: the Outworks
                // capture's ASCENT STAIR ran off the screen's right edge when the label stood centred under its arrow).
                Vector3 extents=_labelRenderer.bounds.extents;
                _label.transform.position=EdgeLabel.Place(_camera,cue,direction,extents,EdgeLabel.ChevronReach,bottom,top);
                DirectionLabelBounds=new Bounds(_label.transform.position,extents*2f);
            }
            else
                _label.transform.position=position+Vector3.down*.45f;
        }
        private void OnDisable(){DirectionLabelBounds=default;if(_plate!=null)_plate.SetActive(false);if(_label!=null)_label.gameObject.SetActive(false);if(_arrow!=null)_arrow.enabled=false;}
        private void OnDestroy(){if(_plate!=null)Destroy(_plate);if(_arrow!=null)Destroy(_arrow.gameObject);if(_label!=null)Destroy(_label.gameObject);}
    }

    // Where an edge arrow's label stands (ExitSealView's way, OutworksView's arrows): on the arrow's inner side - its box
    // set back from the chevron along the way it points, by the chevron's reach and the box's own depth that way - and
    // clamped whole inside the screen's safe area with a margin, and inside the band between the HUD blocks the arrows
    // stand on (bottom and top, viewport heights). Presentation only.
    internal static class EdgeLabel
    {
        // The chevron's length behind its tip, and a gap past it.
        public const float ChevronReach = .26f;
        // Inside the safe area by this share of the screen's width (and of its height at the band's ends).
        public const float Margin = .03f;

        public static Vector3 Place(Camera camera, Vector3 cue, Vector3 direction, Vector3 extents, float reach,
            float bottom, float top)
        {
            float depth = reach + Mathf.Abs(direction.x) * extents.x + Mathf.Abs(direction.y) * extents.y;
            Vector3 centre = cue - direction * depth;
            centre.z = 0f;
            return Clamp(camera, centre, extents, bottom, top);
        }

        // A label's centre moved so its box (extents, world units) lies inside the safe area's margin and the band.
        public static Vector3 Clamp(Camera camera, Vector3 centre, Vector3 extents, float bottom, float top)
        {
            if (camera == null || Screen.width <= 0 || Screen.height <= 0)
                return centre;
            Rect safe = Screen.safeArea;
            float left = safe.xMin / Screen.width + Margin;
            float right = safe.xMax / Screen.width - Margin;
            float low = Mathf.Max(bottom, safe.yMin / Screen.height + Margin);
            float high = Mathf.Min(top, safe.yMax / Screen.height - Margin);
            float z = camera.WorldToViewportPoint(centre).z;
            Vector3 min = camera.ViewportToWorldPoint(new Vector3(left, low, z));
            Vector3 max = camera.ViewportToWorldPoint(new Vector3(right, high, z));
            centre.x = Within(centre.x, min.x + extents.x, max.x - extents.x);
            centre.y = Within(centre.y, min.y + extents.y, max.y - extents.y);
            return centre;
        }

        // Whether two boxes (centres and extents) overlap, with a gap between them.
        public static bool Overlaps(Vector3 a, Vector3 aExtents, Vector3 b, Vector3 bExtents, float gap) =>
            Mathf.Abs(a.x - b.x) < aExtents.x + bExtents.x + gap && Mathf.Abs(a.y - b.y) < aExtents.y + bExtents.y + gap;

        private static float Within(float value, float low, float high) =>
            low > high ? (low + high) * .5f : Mathf.Clamp(value, low, high);
    }
}
