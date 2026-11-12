using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only: the Warden's mark on the floor - a ring the hammer's reach wide where the hero stood, and an inner
    // ring growing out to it through the telegraph, so the moment it fills is the moment the blow lands - then a flash
    // where it lands. The Warden's body tells it too: a painted Warden raises its hammer while the mark is down and
    // brings it down as the blow lands (EnemyLookView.WindUp and Slam). WardenSlamService owns the clock, the mark and
    // the blow.
    //
    // The Glass Regent's mark is its lanes instead (WardenSlamService.Lanes; docs/50 section 6): each a stripe from where
    // the Regent stood through the hero's spot, RegentLanes.Width across and Length long, drawn by the same rules as the
    // ring - its edge red-orange and pulsing faster as the blow nears, a fill growing across it from its middle line to its
    // edges through the telegraph, the whole lane flashing hot as the shards run. Each lane is the Regent's two 9-slice
    // sprites (GlassRegent.LaneEdge and LaneFill; Tools/ArtReview/BuildRegentLaneMark.py) at the lane's start, turned in
    // floor space along it, under a parent that squashes y by ArenaFloor.DepthScale so the stripe lies on the floor.
    [DefaultExecutionOrder(100)]
    public sealed class WardenSlamView : MonoBehaviour
    {
        private const int Segments = 40;
        private const float FlashSeconds = .35f;
        private static readonly Color Ember = new Color(1f, .32f, .12f, 1f);
        private static readonly Color Hot = new Color(1f, .82f, .45f, 1f);
        // The fill's narrowest, as a share of the lane's width, at the mark.
        private const float LaneFillFloor = .05f;
        private WardenSlamService _service;
        private ArenaView _arena;
        private Material _material;
        private Transform _mark;
        private LineRenderer _edge;
        private LineRenderer _fill;
        private float _flash;
        // The boss look holding its hammer up for the mark that is down.
        private EnemyLookView _windingUp;
        // The Regent's lanes: a squashing parent, the lane's own transform and its two sprites, one set a lane, made as
        // a Regent first marks; how many show, and whether the mark down is lanes.
        private readonly Transform[] _laneFloors = new Transform[RegentRule.MaxLanes];
        private readonly SpriteRenderer[] _laneEdges = new SpriteRenderer[RegentRule.MaxLanes];
        private readonly SpriteRenderer[] _laneFills = new SpriteRenderer[RegentRule.MaxLanes];
        private readonly float[] _laneWidths = new float[RegentRule.MaxLanes];
        private int _lanes;
        private bool _laneMark;
        public bool IsMarkShown => _mark != null && _mark.gameObject.activeSelf;
        public float ShownFill { get; private set; }
        // The Regent's lanes showing now (0 for the Warden's ring, or none down).
        public int ShownLanes { get; private set; }
        // A lane showing, for a check: its sprites' transform (at its start, turned along it) and its fill's size.
        public Transform LaneAt(int lane) => lane < ShownLanes ? _laneEdges[lane].transform : null;
        public Vector2 LaneFillSize(int lane) => lane < ShownLanes ? _laneFills[lane].size : Vector2.zero;

        internal void Initialize(WardenSlamService service)
        {
            _service = service;
            _arena = FindFirstObjectByType<ArenaView>();
            _material = SharedSpriteMaterial.For(_arena);
            _mark = new GameObject("Warden Mark").transform;
            _mark.SetParent(transform, false);
            _edge = Ring("Warden Mark Edge", .07f);
            _fill = Ring("Warden Mark Fill", .05f);
            _mark.gameObject.SetActive(false);
            service.Marked += OnMarked;
            service.Slammed += _ => _flash = FlashSeconds;
            service.Slammed += OnSlammed;
        }

        // Where the mark is down, on the floor, in world space.
        private Vector3 MarkPoint => new Vector3(_service.MarkX, ArenaFloor.WorldY(_service.MarkY), 0f);

        private LineRenderer Ring(string name, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(_mark, false);
            line.sharedMaterial = _material; line.useWorldSpace = false; line.loop = true; line.numCapVertices = 2;
            line.sortingOrder = 0; line.positionCount = Segments; line.startWidth = line.endWidth = width;
            for (int k = 0; k < Segments; k++)
            {
                float a = k * Mathf.PI * 2 / Segments;
                line.SetPosition(k, new Vector3(Mathf.Cos(a) * WardenRule.SlamRadius, ArenaFloor.WorldY(Mathf.Sin(a) * WardenRule.SlamRadius), 0));
            }
            return line;
        }

        private void OnMarked()
        {
            var p = new Vector3(_service.MarkX, ArenaFloor.WorldY(_service.MarkY), 0);
            if (_arena != null && _arena.IsFoundryRegion) p.y += FoundrySurface.Lift(p, _arena.transform.position);
            _mark.position = p;
            _flash = 0f;
            LayLanes(p.y - ArenaFloor.WorldY(_service.MarkY));
            Health boss = enabled ? _service.Boss : null;
            _windingUp = boss != null ? boss.GetComponent<EnemyLookView>() : null;
            if (_windingUp != null)
                _windingUp.WindUp(MarkPoint);
        }

        private void OnSlammed(bool hit)
        {
            if (_windingUp != null)
                _windingUp.Slam(MarkPoint);
            _windingUp = null;
        }

        private void LateUpdate()
        {
            using (PerformanceMarkers.LateWarden.Auto())
                LateTick();
        }

        private void LateTick()
        {
            if (_mark == null)
                return;
            if (_flash > 0f)
                _flash -= Time.deltaTime;
            bool marked = enabled && _service.IsFighting && _service.Clock.IsMarked;
            // A mark withdrawn without a blow (the boss fell or changed, the fight stopped): the hammer comes down.
            if (!marked && _windingUp != null)
            {
                _windingUp.CancelWindup();
                _windingUp = null;
            }
            float flash = Mathf.Clamp01(_flash / FlashSeconds);
            bool shown = marked || flash > 0f;
            bool ring = shown && !_laneMark;
            if (_mark.gameObject.activeSelf != ring)
                _mark.gameObject.SetActive(ring);
            ShowLanes(shown && _laneMark ? _lanes : 0);
            if (!shown)
            {
                ShownFill = 0f;
                return;
            }
            ShownFill = marked ? _service.Clock.TelegraphProgress : 1f;
            // The edge pulses faster as the blow nears; the fill grows out to it; the blow flashes the whole mark hot.
            float pulse = .6f + .4f * Mathf.Sin(Time.time * Mathf.Lerp(9f, 24f, ShownFill));
            Color edge = flash > 0f ? Color.Lerp(Ember, Hot, flash) : Ember;
            edge.a = flash > 0f ? flash : pulse;
            float fillAlpha = flash > 0f ? flash : .4f + .6f * ShownFill;
            float scale = Mathf.Max(LaneFillFloor, ShownFill);
            if (_laneMark)
            {
                // The lane's fill sprite carries its own red-orange: it is tinted white, the fill's alpha in its tint.
                for (int i = 0; i < ShownLanes; i++)
                {
                    _laneEdges[i].color = edge;
                    _laneFills[i].color = new Color(1f, 1f, 1f, fillAlpha);
                    Vector2 size = _laneEdges[i].size;
                    _laneFills[i].size = new Vector2(size.x, scale * _laneWidths[i]);
                }
                return;
            }
            _edge.startColor = _edge.endColor = edge;
            Color fill = Hot;
            fill.a = fillAlpha;
            _fill.startColor = _fill.endColor = fill;
            _fill.transform.localScale = new Vector3(scale, scale, 1f);
        }

        // The Regent's lanes as the mark goes down (WardenSlamService.Lanes, set before Marked): each laid from where its
        // body stood along its direction, lifted as the ring is at the mark. The Warden's mark lays none.
        private void LayLanes(float lift)
        {
            GlassRegent regent = _service.Regent;
            RegentLanes lanes = _service.Lanes;
            _laneMark = regent != null && lanes.Count > 0 && regent.LaneEdge != null && regent.LaneFill != null;
            _lanes = 0;
            if (!_laneMark)
            {
                _laneMark = regent != null;
                return;
            }
            for (int i = 0; i < lanes.Count && i < _laneEdges.Length; i++)
            {
                if (_laneEdges[i] == null)
                    MakeLane(i);
                _laneEdges[i].sprite = regent.LaneEdge;
                _laneFills[i].sprite = regent.LaneFill;
                lanes.Direction(i, out float dx, out float dy);
                Transform floor = _laneFloors[i];
                floor.localPosition = new Vector3(0f, lift, 0f);
                Transform lane = _laneEdges[i].transform.parent;
                lane.localPosition = new Vector3(lanes.OriginX(i), lanes.OriginY(i), 0f);
                lane.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dy, dx) * Mathf.Rad2Deg);
                _laneWidths[i] = lanes.Width;
                _laneEdges[i].size = new Vector2(lanes.Length, lanes.Width);
                _laneFills[i].size = new Vector2(lanes.Length, LaneFillFloor * lanes.Width);
                _lanes++;
            }
        }

        // One lane's renderers: a parent squashed to the floor's depth, the lane's transform in floor units, its edge
        // over its fill.
        private void MakeLane(int i)
        {
            var floor = new GameObject("Regent Lane " + (i + 1)).transform;
            floor.SetParent(transform, false);
            floor.localScale = new Vector3(1f, ArenaFloor.DepthScale, 1f);
            var lane = new GameObject("Lane").transform;
            lane.SetParent(floor, false);
            _laneFloors[i] = floor;
            _laneFills[i] = LaneSprite(lane, "Lane Fill", 0);
            _laneEdges[i] = LaneSprite(lane, "Lane Edge", 1);
            floor.gameObject.SetActive(false);
        }

        private SpriteRenderer LaneSprite(Transform lane, string name, int order)
        {
            var sprite = new GameObject(name).AddComponent<SpriteRenderer>();
            sprite.transform.SetParent(lane, false);
            sprite.sharedMaterial = _material;
            sprite.drawMode = SpriteDrawMode.Sliced;
            sprite.sortingOrder = order;
            return sprite;
        }

        private void ShowLanes(int count)
        {
            ShownLanes = count;
            for (int i = 0; i < _laneFloors.Length; i++)
            {
                if (_laneFloors[i] == null)
                    continue;
                bool shown = i < count;
                if (_laneFloors[i].gameObject.activeSelf != shown)
                    _laneFloors[i].gameObject.SetActive(shown);
            }
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.Marked -= OnMarked;
                _service.Slammed -= OnSlammed;
            }
            // The material is shared (SharedSpriteMaterial), not this view's to destroy.
        }
    }
}
