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
    // the Regent stood through the hero's spot, RegentLanes.Width across and LengthOf long, drawn by the same rules as the
    // ring - its edge red-orange and pulsing faster as the blow nears, a fill growing across it from its middle line to its
    // edges through the telegraph, the whole lane flashing hot as the shards run. Each lane is the Regent's two 9-slice
    // sprites (GlassRegent.LaneEdge and LaneFill; Tools/ArtReview/BuildRegentLaneMark.py) at the lane's start, turned in
    // floor space along it, under a parent that squashes y by ArenaFloor.DepthScale so the stripe lies on the floor.
    //
    // And the Regent's feet (RegentRule.FeetReach; docs/50 "The Regent's bite for the standing hero"): a violet ring over a
    // dark shade round it while it is fought - violet, a creature's mark that never hurts by itself (docs/49 rule 4) -
    // that brightens, widens and pulses through a beat whose mark found the hero inside it, when the Regent keeps its rod
    // swinging through the cast (WardenSlamService.RodAtItsFeet) and raises no windup. A hero beyond the ring as the lane
    // goes down sees the Regent raise its mirror and hold its rod, as the Warden raises its hammer.
    //
    // Rootmother Myrr's mark is her root snare (WardenSlamService.Snare; docs/64 sections 4-5): a ring of MyrrRule.SnareRadius
    // drawn by the ring's rules - its edge red-orange, the telegraph's colour, pulsing faster as the roots near, its fill
    // growing out to it, the whole snare flashing hot as they erupt - that MOVES: laid each frame at the service's mark, the
    // snare's centre creeping after the hero. Orchid root tips crown its rim (a creature's colour, docs/52 section 2.1 rule
    // 3), reaching out as the telegraph fills and bursting as the roots erupt. Her feet ring is the Regent's, in her orchid
    // over a plum shade, pulsing through a beat whose snare went down with the hero at her feet
    // (WardenSlamService.HookAtHerFeet: her hook swings, no windup raised). And a hero her roots caught shows them round its
    // feet, orchid over bark, for the second it stands planted (WardenSlamService.IsHeroPlanted).
    [DefaultExecutionOrder(100)]
    public sealed class WardenSlamView : MonoBehaviour
    {
        private const int Segments = 40;
        private const float FlashSeconds = .35f;
        private static readonly Color Ember = new Color(1f, .32f, .12f, 1f);
        private static readonly Color Hot = new Color(1f, .82f, .45f, 1f);
        // The fill's narrowest, as a share of the lane's width, at the mark.
        private const float LaneFillFloor = .05f;
        // The Regent's feet ring: docs/49's aurora violet, quiet at rest and pulsing on a beat that found the hero inside.
        // The game-feel pass (docs/43 section 1, 2026-11-22) made it read on the floor: at 0.3 alpha and 0.05 wide it was
        // lost on the Vaults' pale stone at phone size (about 3 px), so it rests at 0.6 and 0.075 wide over a dark violet
        // shade line twice as wide that separates it from light and dark floors alike, and widens as it pulses. Still
        // violet - a creature's mark that never hurts by itself (docs/49 rule 4) - and still under every actor.
        private static readonly Color Violet = new Color(.6f, .48f, 1f, 1f);
        private static readonly Color VioletShade = new Color(.14f, .07f, .28f, 1f);
        internal const float FeetRest = .6f;
        internal const float FeetWidth = .075f;
        internal const float FeetPulseWidth = .1f;
        private const float FeetShadeWidth = .15f;
        private const float FeetShadeAlpha = .45f;
        // Rootmother Myrr's colours (docs/64 section 7): her orchid bud (#D86BD0) for the snare's root tips, her feet ring
        // and the planted roots, its pale bloom (#F6C2F0) as they burst, a plum shade under her ring, the bark (#4A3B33)
        // under the planted roots. Nothing of hers is gloamlight (docs/51 rule 5); the snare's own ring is the telegraph's
        // red-orange.
        internal static readonly Color Orchid = new Color(.847f, .42f, .816f, 1f);
        private static readonly Color OrchidPale = new Color(.965f, .761f, .941f, 1f);
        private static readonly Color OrchidShade = new Color(.2f, .06f, .22f, 1f);
        private static readonly Color Bark = new Color(.29f, .231f, .2f, 1f);
        // The snare's root tips: how many round its rim, how far past it they reach at rest, through the telegraph and as
        // the roots erupt.
        internal const int RootTips = 14;
        private const float TipRest = .1f;
        private const float TipGrow = .18f;
        private const float TipBurst = .4f;
        // The planted roots round a caught hero's feet: their ring's radius and how far their tips reach past it, their
        // fade over the plant's last moment.
        internal const float PlantedRadius = .42f;
        private const float PlantedReach = .22f;
        private const float PlantedFade = .2f;
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
        // The ring round the Regent's (or Myrr's) feet and its shade under it, made as either is first fought.
        private LineRenderer _feet;
        private LineRenderer _feetShade;
        // Myrr's snare: its own transform (laid at the snare's centre each frame), its edge, fill and root tips, made as she
        // first marks; whether the mark down is hers.
        private Transform _snare;
        private LineRenderer _snareEdge;
        private LineRenderer _snareFill;
        private LineRenderer _snareRoots;
        private readonly Vector3[] _rootPoints = new Vector3[RootTips * 2];
        private bool _snareMark;
        // The roots round a planted hero's feet and their bark under them, made as the roots first catch it.
        private Transform _planted;
        private LineRenderer _plantedRoots;
        private LineRenderer _plantedShade;
        private readonly Vector3[] _plantedPoints = new Vector3[RootTips * 2];
        internal LineRenderer FeetRingLine => _feet;
        internal LineRenderer FeetShadeLine => _feetShade;
        public bool IsMarkShown => _mark != null && _mark.gameObject.activeSelf;
        // Whether the ring round the Regent's feet shows, and whether it pulses (the beat down found the hero inside it).
        public bool IsFeetShown => _feet != null && _feet.gameObject.activeSelf;
        public bool IsFeetPulsing { get; private set; }
        public float ShownFill { get; private set; }
        // The Regent's lanes showing now (0 for the Warden's ring, or none down).
        public int ShownLanes { get; private set; }
        // A lane showing, for a check: its sprites' transform (at its start, turned along it) and its fill's size.
        public Transform LaneAt(int lane) => lane < ShownLanes ? _laneEdges[lane].transform : null;
        public Vector2 LaneFillSize(int lane) => lane < ShownLanes ? _laneFills[lane].size : Vector2.zero;
        // Myrr's snare showing (her mark down, or its eruption's flash), where it is drawn on the floor (its centre, the
        // foundry's lift taken off), its fill's scale and how far its root tips reach past its rim now.
        public bool IsSnareShown => _snare != null && _snare.gameObject.activeSelf;
        public Vector2 SnareShownAt { get; private set; }
        public float SnareFillScale => _snareFill != null ? _snareFill.transform.localScale.x : 0f;
        public float SnareTipReach { get; private set; }
        internal LineRenderer SnareEdgeLine => _snareEdge;
        internal LineRenderer SnareRootsLine => _snareRoots;
        // The roots round a planted hero's feet showing.
        public bool IsPlantedShown => _planted != null && _planted.gameObject.activeSelf;
        internal LineRenderer PlantedRootsLine => _plantedRoots;

        internal void Initialize(WardenSlamService service)
        {
            _service = service;
            _arena = FindFirstObjectByType<ArenaView>();
            _material = SharedSpriteMaterial.For(_arena);
            _mark = new GameObject("Warden Mark").transform;
            _mark.SetParent(transform, false);
            _edge = Ring(_mark, "Warden Mark Edge", .07f, WardenRule.SlamRadius);
            _fill = Ring(_mark, "Warden Mark Fill", .05f, WardenRule.SlamRadius);
            _mark.gameObject.SetActive(false);
            service.Marked += OnMarked;
            service.Slammed += _ => _flash = FlashSeconds;
            service.Slammed += OnSlammed;
        }

        // Where the mark is down, on the floor, in world space.
        private Vector3 MarkPoint => new Vector3(_service.MarkX, ArenaFloor.WorldY(_service.MarkY), 0f);

        private LineRenderer Ring(Transform parent, string name, float width, float radius)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.sharedMaterial = _material; line.useWorldSpace = false; line.loop = true; line.numCapVertices = 2;
            line.sortingOrder = 0; line.positionCount = Segments; line.startWidth = line.endWidth = width;
            for (int k = 0; k < Segments; k++)
            {
                float a = k * Mathf.PI * 2 / Segments;
                line.SetPosition(k, new Vector3(Mathf.Cos(a) * radius, ArenaFloor.WorldY(Mathf.Sin(a) * radius), 0));
            }
            return line;
        }

        // A point on the floor (floor units) as the world draws it, lifted onto the foundry's surface there.
        private Vector3 Lifted(float floorX, float floorY)
        {
            var p = new Vector3(floorX, ArenaFloor.WorldY(floorY), 0f);
            if (_arena != null && _arena.IsFoundryRegion) p.y += FoundrySurface.Lift(p, _arena.transform.position);
            return p;
        }

        private void OnMarked()
        {
            var p = new Vector3(_service.MarkX, ArenaFloor.WorldY(_service.MarkY), 0);
            if (_arena != null && _arena.IsFoundryRegion) p.y += FoundrySurface.Lift(p, _arena.transform.position);
            _mark.position = p;
            _flash = 0f;
            LayLanes(p.y - ArenaFloor.WorldY(_service.MarkY));
            // Myrr's snare: laid where it went down, then each frame where it has crept (LateTick).
            _snareMark = _service.MarksSnare;
            if (_snareMark)
            {
                if (_snare == null)
                    MakeSnare();
                PlaceSnare();
            }
            Health boss = enabled ? _service.Boss : null;
            // A Regent that found the hero at its feet raises no windup: its rod keeps swinging through the cast. Nor does
            // Myrr with the hero at her feet: her hook swings through the snare's telegraph.
            if ((_service.MarksLanes && _service.RodAtItsFeet) || (_service.MarksSnare && _service.HookAtHerFeet))
                boss = null;
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
            ShowFeet();
            ShowPlanted();
            bool marked = enabled && _service.IsFighting && _service.Clock.IsMarked;
            // A mark withdrawn without a blow (the boss fell or changed, the fight stopped): the hammer comes down.
            if (!marked && _windingUp != null)
            {
                _windingUp.CancelWindup();
                _windingUp = null;
            }
            float flash = Mathf.Clamp01(_flash / FlashSeconds);
            bool shown = marked || flash > 0f;
            bool ring = shown && !_laneMark && !_snareMark;
            if (_mark.gameObject.activeSelf != ring)
                _mark.gameObject.SetActive(ring);
            ShowLanes(shown && _laneMark ? _lanes : 0);
            bool snare = shown && _snareMark && _snare != null;
            if (_snare != null && _snare.gameObject.activeSelf != snare)
                _snare.gameObject.SetActive(snare);
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
            Color fill = Hot;
            fill.a = fillAlpha;
            if (_snareMark)
            {
                // Where the snare has crept to this frame; the root tips reach out with the telegraph and burst with the roots.
                PlaceSnare();
                _snareEdge.startColor = _snareEdge.endColor = edge;
                _snareFill.startColor = _snareFill.endColor = fill;
                _snareFill.transform.localScale = new Vector3(scale, scale, 1f);
                SnareTipReach = flash > 0f ? Mathf.Lerp(TipRest + TipGrow, TipBurst, flash) : TipRest + TipGrow * ShownFill;
                LayRoots(_snareRoots, _rootPoints, MyrrRule.SnareRadius - .03f, MyrrRule.SnareRadius + SnareTipReach);
                Color tips = flash > 0f ? Color.Lerp(Orchid, OrchidPale, flash) : Orchid;
                tips.a = flash > 0f ? flash : .75f + .25f * pulse;
                _snareRoots.startColor = _snareRoots.endColor = tips;
                return;
            }
            _edge.startColor = _edge.endColor = edge;
            _fill.startColor = _fill.endColor = fill;
            _fill.transform.localScale = new Vector3(scale, scale, 1f);
        }

        // Myrr's snare's renderers under a transform of their own: its edge and fill at MyrrRule.SnareRadius, its root tips.
        private void MakeSnare()
        {
            _snare = new GameObject("Myrr Snare").transform;
            _snare.SetParent(transform, false);
            _snareEdge = Ring(_snare, "Myrr Snare Edge", .07f, MyrrRule.SnareRadius);
            _snareFill = Ring(_snare, "Myrr Snare Fill", .05f, MyrrRule.SnareRadius);
            _snareRoots = RootLine(_snare, "Myrr Snare Roots", .05f, 0);
            LayRoots(_snareRoots, _rootPoints, MyrrRule.SnareRadius - .03f, MyrrRule.SnareRadius + TipRest);
            _snare.gameObject.SetActive(false);
        }

        // The snare at the service's mark - the snare's centre now - lifted as the Warden's ring is.
        private void PlaceSnare()
        {
            _snare.position = Lifted(_service.MarkX, _service.MarkY);
            SnareShownAt = new Vector2(_service.MarkX, _service.MarkY);
        }

        // A crown of root tips: RootTips points out at outer, each between two in at inner, a little off their even spacing
        // so they read as roots, not a cog. Floor units round the line's transform, seen at the floor's depth.
        private static void LayRoots(LineRenderer line, Vector3[] points, float inner, float outer)
        {
            int n = points.Length / 2;
            for (int k = 0; k < points.Length; k++)
            {
                bool tip = (k & 1) == 1;
                float a = k * Mathf.PI / n + (tip ? .22f * Mathf.PI / n * Mathf.Sin(k * 1.7f) : 0f);
                float r = tip ? outer : inner;
                points[k] = new Vector3(Mathf.Cos(a) * r, ArenaFloor.WorldY(Mathf.Sin(a) * r), 0f);
            }
            line.SetPositions(points);
        }

        private LineRenderer RootLine(Transform parent, string name, float width, int order)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.sharedMaterial = _material; line.useWorldSpace = false; line.loop = true; line.numCapVertices = 1;
            line.numCornerVertices = 1;
            line.sortingOrder = order; line.positionCount = RootTips * 2; line.startWidth = line.endWidth = width;
            return line;
        }

        // The roots round a planted hero's feet while it stands planted, fading over the plant's last moment: orchid tips
        // over a bark ring, on the floor under the hero (its effects' order, under every actor).
        private void ShowPlanted()
        {
            Health hero = _service.Hero;
            bool planted = enabled && hero != null && hero.IsAlive && _service.IsHeroPlanted;
            if (!planted)
            {
                if (_planted != null && _planted.gameObject.activeSelf)
                    _planted.gameObject.SetActive(false);
                return;
            }
            if (_planted == null)
            {
                _planted = new GameObject("Myrr Planted Roots").transform;
                _planted.SetParent(transform, false);
                _plantedShade = RootLine(_planted, "Myrr Planted Bark", .12f, -1);
                _plantedRoots = RootLine(_planted, "Myrr Planted Tips", .06f, 0);
                LayRoots(_plantedShade, new Vector3[RootTips * 2], PlantedRadius - .04f, PlantedRadius + PlantedReach * .6f);
                LayRoots(_plantedRoots, _plantedPoints, PlantedRadius, PlantedRadius + PlantedReach);
            }
            if (!_planted.gameObject.activeSelf)
                _planted.gameObject.SetActive(true);
            Vector3 p = hero.transform.position;
            if (_arena != null && _arena.IsFoundryRegion) p.y += FoundrySurface.Lift(p, _arena.transform.position);
            _planted.position = new Vector3(p.x, p.y, 0f);
            float left = _service.PlantedUntil - Time.time;
            float fade = Mathf.Clamp01(left / PlantedFade);
            Color tips = Orchid;
            tips.a = .9f * fade;
            _plantedRoots.startColor = _plantedRoots.endColor = tips;
            Color bark = Bark;
            bark.a = .8f * fade;
            _plantedShade.startColor = _plantedShade.endColor = bark;
        }

        // The ring round the Regent's feet while it is fought: faint, or pulsing violet through a beat whose mark found the hero
        // inside it (its rod free through the cast). Round Myrr's the same ring in her orchid over a plum shade, pulsing
        // through a beat whose snare went down with the hero at her feet (her hook free through it).
        private void ShowFeet()
        {
            bool myrr = _service.MarksSnare;
            Health boss = enabled && (_service.MarksLanes || myrr) ? _service.Boss : null;
            if (boss == null)
            {
                IsFeetPulsing = false;
                if (_feet != null && _feet.gameObject.activeSelf)
                    _feet.gameObject.SetActive(false);
                return;
            }
            if (_feet == null)
                _feet = FeetRing();
            if (!_feet.gameObject.activeSelf)
                _feet.gameObject.SetActive(true);
            Vector3 p = boss.transform.position;
            if (_arena != null && _arena.IsFoundryRegion) p.y += FoundrySurface.Lift(p, _arena.transform.position);
            _feet.transform.position = new Vector3(p.x, p.y, 0f);
            IsFeetPulsing = (myrr ? _service.HookAtHerFeet : _service.RodAtItsFeet) && _service.Clock.HoldsWeapon;
            float beat = IsFeetPulsing ? .5f + .5f * Mathf.Sin(Time.time * 18f) : 0f;
            Color ring = myrr ? Orchid : Violet;
            ring.a = IsFeetPulsing ? .8f + .2f * beat : FeetRest;
            _feet.startColor = _feet.endColor = ring;
            _feet.startWidth = _feet.endWidth = IsFeetPulsing ? Mathf.Lerp(FeetWidth, FeetPulseWidth, beat) : FeetWidth;
            Color shade = myrr ? OrchidShade : VioletShade;
            shade.a = IsFeetPulsing ? FeetShadeAlpha + .15f * beat : FeetShadeAlpha;
            _feetShade.startColor = _feetShade.endColor = shade;
        }

        private LineRenderer FeetRing()
        {
            LineRenderer line = FeetLine("Regent Feet", transform, 0, FeetWidth);
            // The shade rides under the ring, on its transform, one order lower: still over the floor (its effects sort
            // well under 0) and under every actor.
            _feetShade = FeetLine("Regent Feet Shade", line.transform, -1, FeetShadeWidth);
            return line;
        }

        private LineRenderer FeetLine(string name, Transform parent, int order, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.sharedMaterial = _material; line.useWorldSpace = false; line.loop = true; line.numCapVertices = 2;
            line.sortingOrder = order; line.positionCount = Segments; line.startWidth = line.endWidth = width;
            // The Regent's reach and Myrr's are one (MyrrRule.FeetReach is RegentRule.FeetReach): one ring serves both.
            for (int k = 0; k < Segments; k++)
            {
                float a = k * Mathf.PI * 2 / Segments;
                line.SetPosition(k, new Vector3(Mathf.Cos(a) * RegentRule.FeetReach, ArenaFloor.WorldY(Mathf.Sin(a) * RegentRule.FeetReach), 0));
            }
            return line;
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
                _laneEdges[i].size = new Vector2(lanes.LengthOf(i), lanes.Width);
                _laneFills[i].size = new Vector2(lanes.LengthOf(i), LaneFillFloor * lanes.Width);
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
