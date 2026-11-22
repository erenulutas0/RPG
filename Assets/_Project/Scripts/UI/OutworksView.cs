using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Localisation;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only, for docs/62's P0a (the Outworks phase behind development/outworks.txt; EncounterController owns the
    // phase, the anvils' charge, the Keeper, the Bell and the stair, HeroMovementInput the pads' leaps). A greybox in the
    // court's own lines, as the altars and the exit seal are drawn, so the owner can judge the mechanics on the phone before
    // any art: each Tithe Anvil a ring of its capture radius with a charge arc; each pad a ring with a chevron toward its
    // landing; the Ascent Stair sealed (the exit seal, ExitSealView, draws it once it opens); a warm lantern ring under the
    // Keeper; the hero's body lifted along its arc while its shadow keeps the ground (CombatantView.Lift); and edge arrows on
    // the tall band to what lies off the screen - at most OutworksRule.MaxArrows, the stair first, then the Keeper, then the
    // anvils nearest first, the open stair's own arrow (ExitSealView's) counted among them. Shown only while the phase runs.
    //
    // The arrows' labels (2026-11-23, the P0a captures): each stands on its arrow's inner side, whole inside the safe area
    // (EdgeLabel); an arrow that points the way an earlier one does under the same name merges into it (two anvils behind
    // one edge read as one ANVIL, not two stacked), and a different name that way loses its chevron and stacks its label
    // beside the earlier one's. A pad draws its centre - where the hero stands still to launch - and fills it as it does.
    // Runs after ExitSealView (order 100) so the open stair's label is placed before these are laid round it.
    [DefaultExecutionOrder(101)]
    public sealed class OutworksView : MonoBehaviour
    {
        private const int Segments = 48;
        private const float LabelReach = 3.2f;
        // Two arrows' tips this near (world units) point the same way; and the gap kept between two labels' boxes.
        private const float MergeReach = .6f;
        private const float LabelGap = .06f;
        private static readonly Color Ember = new Color(1f, .55f, .22f, 1f);
        private static readonly Color Gold = new Color(1f, .82f, .45f, 1f);
        private static readonly Color Ash = new Color(.55f, .5f, .46f, 1f);
        private static readonly Color Brass = new Color(.78f, .63f, .36f, 1f);
        private static readonly Color Ivory = new Color(.88f, .93f, .87f, 1f);
        private static readonly Color Lantern = new Color(1f, .7f, .3f, .85f);

        private CombatSetup _setup;
        private EncounterController _encounters;
        private Transform _hero;
        private CombatantView _heroView;
        private ArenaView _arena;
        private Camera _camera;
        private ArenaCameraFollow _framing;
        private Material _material;
        private GameObject _root;
        private GameObject[] _anvils;
        private LineRenderer[] _charges;
        private GameObject[] _pads;
        private GameObject _stair;
        private GameObject _lantern;
        private TextMesh _label;
        private string _shownLabel;
        private readonly LineRenderer[] _arrows = new LineRenderer[OutworksRule.MaxArrows];
        private readonly TextMesh[] _arrowLabels = new TextMesh[OutworksRule.MaxArrows];
        private readonly string[] _arrowShown = new string[OutworksRule.MaxArrows];
        private readonly MeshRenderer[] _arrowRenderers = new MeshRenderer[OutworksRule.MaxArrows];
        // The arrows laid this frame (the open stair's among them): their tips, names and label boxes.
        private readonly Vector3[] _laidCue = new Vector3[OutworksRule.MaxArrows];
        private readonly string[] _laidName = new string[OutworksRule.MaxArrows];
        private readonly Vector3[] _laidCentre = new Vector3[OutworksRule.MaxArrows];
        private readonly Vector3[] _laidExtents = new Vector3[OutworksRule.MaxArrows];
        private int _laid;
        private float _bandBottom = .20f, _bandTop = .80f;
        private ExitSealView _exitSeal;
        private LineRenderer[] _padDwell;
        private readonly Vector3[] _arc = new Vector3[Segments + 1];
        private readonly Vector3[] _corners = new Vector3[4];

        // For tests and captures: whether the phase is drawn, an anvil's ring and charge, the arrows standing (and the
        // labels this view draws, in order), the pad's dwell shown.
        public bool IsShown => _root != null && _root.activeSelf;
        public bool IsAnvilShown(int anvil) => _anvils != null && _anvils[anvil].activeSelf;
        public float ShownCharge(int anvil) => _charges != null && _charges[anvil].enabled && _encounters.Outworks != null
            ? _encounters.Outworks.Anvils.ShareOf(anvil)
            : 0f;
        public int ArrowsShown { get; private set; }
        public int ArrowLabelsShown { get; private set; }
        public string ArrowLabel(int index) => index >= 0 && index < ArrowLabelsShown ? _arrowShown[index] : null;
        public Bounds ArrowLabelBounds(int index) => _arrowRenderers[index].bounds;
        public bool ArrowChevronShown(int index) => index >= 0 && index < ArrowLabelsShown && _arrows[index].enabled;
        public float ShownPadDwell { get; private set; }
        public bool IsLanternShown => _lantern != null && _lantern.activeSelf;
        public float ShownLift { get; private set; }

        internal void Initialize(CombatSetup setup, Transform hero)
        {
            _setup = setup;
            _encounters = setup.Encounters;
            _hero = hero;
            _heroView = hero != null ? hero.GetComponent<CombatantView>() : null;
            _arena = FindFirstObjectByType<ArenaView>();
            _camera = Camera.main;
            _framing = _camera != null ? _camera.GetComponent<ArenaCameraFollow>() : null;
            _exitSeal = GetComponent<ExitSealView>();
            _material = SharedSpriteMaterial.For(_arena);
            _root = new GameObject("Outworks");
            _root.transform.SetParent(transform, false);

            OutworksPhaseShape();
            for (int i = 0; i < _arrows.Length; i++)
            {
                _arrows[i] = Line("Outworks Arrow " + i, transform, Ivory, .045f, 100);
                _arrows[i].positionCount = 3;
                _arrows[i].useWorldSpace = false;
                _arrowLabels[i] = Label("Outworks Arrow Label " + i, transform, .032f, Ivory);
                _arrowRenderers[i] = _arrowLabels[i].GetComponent<MeshRenderer>();
            }
            _label = Label("Outworks Instruction", _root.transform, .04f, Gold);
            Loc.Changed += Rewrite;
            Refresh();
        }

        // Everything that stands still: the anvils, the pads and the sealed stair, at their floor spots on the court's height.
        private void OutworksPhaseShape()
        {
            int count = OutworksRule.AnvilCount;
            _anvils = new GameObject[count];
            _charges = new LineRenderer[count];
            for (int i = 0; i < count; i++)
            {
                OutworksRule.AnvilSite(i, out float x, out float y);
                var anvil = new GameObject("Tithe Anvil " + i);
                anvil.transform.SetParent(_root.transform, false);
                Ring(anvil.transform, Ember, .06f, OutworksRule.AnvilRadius);
                Ring(anvil.transform, Ash, .03f, OutworksRule.AnvilRadius * .45f);
                // The anvil's glyph: a flat-topped anvil in three strokes.
                LineRenderer glyph = Line("Anvil Glyph", anvil.transform, Gold, .05f, 0);
                glyph.positionCount = 6;
                glyph.SetPositions(new[]
                {
                    Project(new Vector2(-.34f, .14f)), Project(new Vector2(.34f, .14f)), Project(new Vector2(.18f, -.02f)),
                    Project(new Vector2(.08f, -.22f)), Project(new Vector2(-.08f, -.22f)), Project(new Vector2(-.18f, -.02f))
                });
                anvil.transform.position = Ground(x, y);
                _anvils[i] = anvil;
                _charges[i] = Line("Anvil Charge", anvil.transform, Gold, .09f, 1);
                _charges[i].positionCount = Segments + 1;
            }
            int pads = OutworksRule.PadCount;
            _pads = new GameObject[pads];
            _padDwell = new LineRenderer[pads];
            for (int i = 0; i < pads; i++)
            {
                OutworksRule.PadLaunch(i, out float x, out float y);
                OutworksRule.PadLanding(i, out float tx, out float ty);
                var pad = new GameObject("Jump Pad " + i);
                pad.transform.SetParent(_root.transform, false);
                Ring(pad.transform, Ivory, .05f, OutworksRule.PadRadius);
                // Its centre, where the hero stands still to launch, and the beat filling round it.
                Ring(pad.transform, Ash, .035f, OutworksRule.PadTriggerRadius);
                _padDwell[i] = Line("Pad Dwell", pad.transform, Gold, .07f, 1);
                _padDwell[i].positionCount = Segments + 1;
                _padDwell[i].enabled = false;
                Vector2 heading = new Vector2(tx - x, ty - y).normalized;
                Vector2 side = new Vector2(-heading.y, heading.x);
                LineRenderer chevron = Line("Pad Chevron", pad.transform, Ivory, .05f, 0);
                chevron.positionCount = 3;
                chevron.SetPositions(new[]
                {
                    Project(-heading * .12f + side * .26f), Project(heading * .32f), Project(-heading * .12f - side * .26f)
                });
                pad.transform.position = Ground(x, y);
                _pads[i] = pad;
            }
            OutworksRule.StairSpot(out float sx, out float sy);
            _stair = new GameObject("Ascent Stair (sealed)");
            _stair.transform.SetParent(_root.transform, false);
            Ring(_stair.transform, Brass, .05f, OutworksRule.StairRadius);
            LineRenderer steps = Line("Stair Steps", _stair.transform, Brass, .05f, 0);
            steps.positionCount = 6;
            steps.SetPositions(new[]
            {
                Project(new Vector2(-.36f, -.24f)), Project(new Vector2(-.12f, -.24f)), Project(new Vector2(-.12f, 0f)),
                Project(new Vector2(.12f, 0f)), Project(new Vector2(.12f, .24f)), Project(new Vector2(.36f, .24f))
            });
            _stair.transform.position = Ground(sx, sy);
            _lantern = new GameObject("Keeper Lantern");
            _lantern.transform.SetParent(_root.transform, false);
            Ring(_lantern.transform, Lantern, .08f, 1.1f);
            Ring(_lantern.transform, Lantern, .04f, .7f);
        }

        private void Rewrite()
        {
            _shownLabel = null;
            for (int i = 0; i < _arrowShown.Length; i++)
                _arrowShown[i] = null;
        }

        private void OnDestroy()
        {
            Loc.Changed -= Rewrite;
            if (_heroView != null)
                _heroView.Lift = 0f;
        }

        private void LateUpdate() => Refresh();

        internal void Refresh()
        {
            if (_root == null || _encounters == null)
                return;
            bool shown = enabled && _encounters.IsInOutworks && _setup.Run != null && !_setup.Run.HasEnded;
            if (_root.activeSelf != shown)
                _root.SetActive(shown);
            HeroLeap leap = _encounters.Leap;
            ShownLift = shown && leap != null ? leap.Lift : 0f;
            if (_heroView != null && _heroView.Lift != ShownLift)
                _heroView.Lift = ShownLift;
            ArrowsShown = 0;
            ArrowLabelsShown = 0;
            ShownPadDwell = 0f;
            if (!shown)
            {
                HideArrowsFrom(0);
                return;
            }
            int dwellPad = leap != null ? leap.DwellPad : -1;
            for (int i = 0; i < _padDwell.Length; i++)
            {
                bool dwelling = i == dwellPad && leap.DwellShare > 0f;
                if (_padDwell[i].enabled != dwelling)
                    _padDwell[i].enabled = dwelling;
                if (!dwelling)
                    continue;
                ShownPadDwell = leap.DwellShare;
                for (int s = 0; s <= Segments; s++)
                {
                    float a = Mathf.PI / 2 - s * (Mathf.PI * 2 / Segments) * ShownPadDwell;
                    _arc[s] = Project(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * OutworksRule.PadTriggerRadius * 1.15f);
                }
                _padDwell[i].SetPositions(_arc);
            }

            OutworksPhase phase = _encounters.Outworks;
            AnvilField anvils = phase.Anvils;
            float heroX = _hero.position.x, heroY = ArenaFloor.FloorY(_hero.position.y);
            int near = -1;
            float best = LabelReach * LabelReach;
            for (int i = 0; i < _anvils.Length; i++)
            {
                bool open = !anvils.IsCaptured(i);
                if (_anvils[i].activeSelf != open)
                    _anvils[i].SetActive(open);
                if (!open)
                    continue;
                float share = anvils.ShareOf(i);
                _charges[i].enabled = share > 0f;
                if (share > 0f)
                {
                    for (int s = 0; s <= Segments; s++)
                    {
                        float a = Mathf.PI / 2 - s * (Mathf.PI * 2 / Segments) * share;
                        _arc[s] = Project(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * OutworksRule.AnvilRadius * 1.08f);
                    }
                    _charges[i].SetPositions(_arc);
                }
                float dx = heroX - anvils.XOf(i), dy = heroY - anvils.YOf(i), d = dx * dx + dy * dy;
                if (d < best)
                {
                    best = d;
                    near = i;
                }
            }
            if (_stair.activeSelf != !phase.StairOpen)
                _stair.SetActive(!phase.StairOpen);

            Health keeper = _encounters.KeeperLeader;
            float keeperX = 0f, keeperY = 0f;
            bool lantern = keeper != null && _encounters.TryFloorPositionOf(keeper, out keeperX, out keeperY);
            if (_lantern.activeSelf != lantern)
                _lantern.SetActive(lantern);
            if (lantern)
                _lantern.transform.position = Ground(keeperX, keeperY);

            // The world label: an anvil's instruction beside it, else the sealed stair's when near it.
            string text = null;
            Vector3 at = Vector3.zero;
            if (near >= 0)
            {
                text = Loc.Text("outworks.anvil_instruction", "TITHE ANVIL - Stand in the ring");
                at = _anvils[near].transform.position + Vector3.down * .7f;
            }
            else if (!phase.StairOpen && ArenaFloor.FloorDistanceSquared(heroX, heroY, phase.StairX, phase.StairY) < LabelReach * LabelReach)
            {
                text = Loc.Text("outworks.stair_sealed", "ASCENT STAIR - Opens at the Bell or with the Keeper's key");
                at = _stair.transform.position + Vector3.down * .7f;
            }
            _label.gameObject.SetActive(text != null);
            if (text != null)
            {
                _label.transform.position = at;
                if (_shownLabel != text)
                {
                    _label.text = text;
                    _shownLabel = text;
                }
            }

            // The arrows: the stair first (its own arrow, ExitSealView's, once open), the Keeper, then the anvils nearest first.
            _laid = 0;
            Band();
            string stairName = Loc.Text("outworks.stair_direction", "ASCENT STAIR");
            if (Offscreen(Ground(phase.StairX, phase.StairY), out Vector3 stairCue, out Vector3 stairDirection))
            {
                if (phase.StairOpen)
                {
                    // ExitSealView draws it; its label's box joins the layout so nothing here lands on it.
                    Bounds box = _exitSeal != null && _exitSeal.ShowsDirection ? _exitSeal.DirectionLabelBounds : default;
                    Lay(stairCue, stairName, box.center, box.extents);
                    ArrowsShown++;
                }
                else
                    Arrow(stairCue, stairDirection, stairName);
            }
            if (lantern && ArrowsShown < _arrows.Length &&
                Offscreen(Ground(keeperX, keeperY), out Vector3 keeperCue, out Vector3 keeperDirection))
                Arrow(keeperCue, keeperDirection, Loc.Text("outworks.keeper_direction", "KEEPER"));
            for (int pass = 0; pass < _anvils.Length && ArrowsShown < _arrows.Length; pass++)
            {
                int next = -1;
                float nextDistance = float.PositiveInfinity;
                for (int i = 0; i < _anvils.Length; i++)
                {
                    if (anvils.IsCaptured(i) || Taken(i, pass))
                        continue;
                    float d = ArenaFloor.FloorDistanceSquared(heroX, heroY, anvils.XOf(i), anvils.YOf(i));
                    if (d < nextDistance)
                    {
                        nextDistance = d;
                        next = i;
                    }
                }
                if (next < 0)
                    break;
                _order[pass] = next;
                if (Offscreen(_anvils[next].transform.position, out Vector3 cue, out Vector3 direction))
                    Arrow(cue, direction, Loc.Text("outworks.anvil_direction", "ANVIL"));
            }
            HideArrowsFrom(ArrowLabelsShown);
        }

        private readonly int[] _order = new int[8];

        // Whether an anvil was already ordered in an earlier pass of this frame.
        private bool Taken(int anvil, int pass)
        {
            for (int p = 0; p < pass; p++)
                if (_order[p] == anvil)
                    return true;
            return false;
        }

        // One arrow to what lies off the screen at cue's way: merged into an earlier arrow of the same name that points
        // the same way (nothing drawn, no slot taken); else its label on its inner side, inside the safe area, set clear
        // of the labels laid before it - without a chevron of its own when an earlier arrow already points this way.
        private void Arrow(Vector3 cue, Vector3 direction, string label)
        {
            bool chevron = true;
            for (int i = 0; i < _laid; i++)
            {
                if ((cue - _laidCue[i]).sqrMagnitude >= MergeReach * MergeReach)
                    continue;
                if (_laidName[i] == label)
                    return;
                chevron = false;
            }
            int index = ArrowLabelsShown;
            LineRenderer line = _arrows[index];
            line.enabled = chevron;
            if (chevron)
            {
                Vector3 side = new Vector3(-direction.y, direction.x, 0f);
                line.transform.position = cue;
                line.SetPosition(0, -direction * .18f + side * .12f);
                line.SetPosition(1, Vector3.zero);
                line.SetPosition(2, -direction * .18f - side * .12f);
            }
            TextMesh text = _arrowLabels[index];
            if (!text.gameObject.activeSelf)
                text.gameObject.SetActive(true);
            if (_arrowShown[index] != label)
            {
                text.text = label;
                _arrowShown[index] = label;
            }
            Vector3 extents = _arrowRenderers[index].bounds.extents;
            Vector3 centre = EdgeLabel.Place(_camera, cue, direction, extents, EdgeLabel.ChevronReach, _bandBottom, _bandTop);
            centre = ClearOfLaid(centre, extents);
            text.transform.position = centre;
            Lay(cue, label, centre, extents);
            ArrowLabelsShown++;
            ArrowsShown++;
        }

        // A label's centre moved off the labels laid before it: stacked above or below the one it meets, toward the band's
        // middle first, kept inside the safe area.
        private Vector3 ClearOfLaid(Vector3 centre, Vector3 extents)
        {
            if (_camera == null)
                return centre;
            float depth = _camera.WorldToViewportPoint(centre).z;
            float middle = _camera.ViewportToWorldPoint(new Vector3(.5f, (_bandBottom + _bandTop) * .5f, depth)).y;
            for (int pass = 0; pass < _laid; pass++)
            {
                int met = -1;
                for (int i = 0; i < _laid && met < 0; i++)
                    if (_laidExtents[i] != Vector3.zero &&
                        EdgeLabel.Overlaps(centre, extents, _laidCentre[i], _laidExtents[i], LabelGap))
                        met = i;
                if (met < 0)
                    return centre;
                float step = extents.y + _laidExtents[met].y + LabelGap;
                float toward = _laidCentre[met].y <= middle ? 1f : -1f;
                Vector3 moved = EdgeLabel.Clamp(_camera, new Vector3(centre.x, _laidCentre[met].y + toward * step, 0f),
                    extents, _bandBottom, _bandTop);
                if (EdgeLabel.Overlaps(moved, extents, _laidCentre[met], _laidExtents[met], LabelGap))
                    moved = EdgeLabel.Clamp(_camera, new Vector3(centre.x, _laidCentre[met].y - toward * step, 0f),
                        extents, _bandBottom, _bandTop);
                centre = moved;
            }
            return centre;
        }

        private void Lay(Vector3 cue, string name, Vector3 centre, Vector3 extents)
        {
            if (_laid >= _laidCue.Length)
                return;
            _laidCue[_laid] = cue;
            _laidName[_laid] = name;
            _laidCentre[_laid] = centre;
            _laidExtents[_laid] = extents;
            _laid++;
        }

        private void HideArrowsFrom(int index)
        {
            for (int i = index; i < _arrows.Length; i++)
            {
                if (_arrows[i] != null)
                    _arrows[i].enabled = false;
                if (_arrowLabels[i] != null && _arrowLabels[i].gameObject.activeSelf)
                    _arrowLabels[i].gameObject.SetActive(false);
            }
        }

        // Whether a world point is off the band between the HUD blocks; where on the band's edge its arrow stands and which
        // way it points (ExitSealView's reading of the band).
        // The band between the HUD blocks, as viewport heights, read once a frame for the arrows and their labels.
        private void Band()
        {
            float bottom = .20f, top = .80f;
            if (_framing != null && _framing.TopHud != null && _framing.BottomHud != null)
            {
                _framing.TopHud.GetWorldCorners(_corners);
                top = _corners[0].y / Screen.height - .04f;
                _framing.BottomHud.GetWorldCorners(_corners);
                bottom = _corners[1].y / Screen.height + .04f;
                if (top <= bottom)
                {
                    bottom = .20f;
                    top = .80f;
                }
            }
            _bandBottom = bottom;
            _bandTop = top;
        }

        private bool Offscreen(Vector3 position, out Vector3 cue, out Vector3 direction)
        {
            cue = position;
            direction = Vector3.up;
            if (_camera == null)
                return false;
            Vector3 viewport = _camera.WorldToViewportPoint(position);
            float bottom = _bandBottom, top = _bandTop;
            bool outside = viewport.x < .08f || viewport.x > .92f || viewport.y < bottom || viewport.y > top;
            if (!outside)
                return false;
            Vector3 edge = new Vector3(Mathf.Clamp(viewport.x, .08f, .92f), Mathf.Clamp(viewport.y, bottom, top), viewport.z);
            cue = _camera.ViewportToWorldPoint(edge);
            cue.z = 0f;
            Vector3 toward = position - cue;
            toward.z = 0f;
            direction = toward.sqrMagnitude > 1e-6f ? toward.normalized : Vector3.up;
            return true;
        }

        private Vector3 Ground(float floorX, float floorY)
        {
            var position = new Vector3(floorX, ArenaFloor.WorldY(floorY), 0f);
            if (_arena != null && _arena.IsFoundryRegion)
                position.y += FoundrySurface.Lift(position, _arena.transform.position);
            return position;
        }

        private void Ring(Transform parent, Color colour, float width, float radius)
        {
            LineRenderer ring = Line("Ring", parent, colour, width, 0);
            ring.positionCount = Segments + 1;
            for (int i = 0; i <= Segments; i++)
            {
                float a = i * Mathf.PI * 2 / Segments;
                ring.SetPosition(i, Project(new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius)));
            }
        }

        private LineRenderer Line(string name, Transform parent, Color colour, float width, int order)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(parent, false);
            line.sharedMaterial = _material;
            line.useWorldSpace = false;
            line.startColor = line.endColor = colour;
            line.startWidth = line.endWidth = width;
            line.sortingOrder = order;
            line.numCapVertices = 2;
            return line;
        }

        private static TextMesh Label(string name, Transform parent, float size, Color colour)
        {
            var labelObject = new GameObject(name);
            labelObject.transform.SetParent(parent, false);
            var label = labelObject.AddComponent<TextMesh>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            label.GetComponent<MeshRenderer>().sortingOrder = 100;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.characterSize = size;
            label.color = colour;
            labelObject.SetActive(false);
            return label;
        }

        private static Vector3 Project(Vector2 floor) => new Vector3(floor.x, ArenaFloor.WorldY(floor.y), 0f);
    }
}
