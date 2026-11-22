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
    [DefaultExecutionOrder(100)]
    public sealed class OutworksView : MonoBehaviour
    {
        private const int Segments = 48;
        private const float LabelReach = 3.2f;
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
        private readonly Vector3[] _arc = new Vector3[Segments + 1];
        private readonly Vector3[] _corners = new Vector3[4];

        // For tests and captures: whether the phase is drawn, an anvil's ring and charge, the arrows standing.
        public bool IsShown => _root != null && _root.activeSelf;
        public bool IsAnvilShown(int anvil) => _anvils != null && _anvils[anvil].activeSelf;
        public float ShownCharge(int anvil) => _charges != null && _charges[anvil].enabled && _encounters.Outworks != null
            ? _encounters.Outworks.Anvils.ShareOf(anvil)
            : 0f;
        public int ArrowsShown { get; private set; }
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
            for (int i = 0; i < pads; i++)
            {
                OutworksRule.PadLaunch(i, out float x, out float y);
                OutworksRule.PadLanding(i, out float tx, out float ty);
                var pad = new GameObject("Jump Pad " + i);
                pad.transform.SetParent(_root.transform, false);
                Ring(pad.transform, Ivory, .05f, OutworksRule.PadRadius);
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
            if (!shown)
            {
                HideArrowsFrom(0);
                return;
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
            int arrow = 0;
            bool stairOff = Offscreen(Ground(phase.StairX, phase.StairY), out Vector3 stairCue, out Vector3 stairDirection);
            if (stairOff)
            {
                if (phase.StairOpen)
                    ArrowsShown++;
                else
                    Arrow(arrow++, stairCue, stairDirection, Loc.Text("outworks.stair_direction", "ASCENT STAIR"));
            }
            if (lantern && arrow < _arrows.Length - (stairOff && phase.StairOpen ? 1 : 0) &&
                Offscreen(Ground(keeperX, keeperY), out Vector3 keeperCue, out Vector3 keeperDirection))
                Arrow(arrow++, keeperCue, keeperDirection, Loc.Text("outworks.keeper_direction", "KEEPER"));
            int limit = _arrows.Length - (stairOff && phase.StairOpen ? 1 : 0);
            for (int pass = 0; pass < _anvils.Length && arrow < limit; pass++)
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
                    Arrow(arrow++, cue, direction, Loc.Text("outworks.anvil_direction", "ANVIL"));
            }
            HideArrowsFrom(arrow);
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

        private void Arrow(int index, Vector3 cue, Vector3 direction, string label)
        {
            LineRenderer line = _arrows[index];
            Vector3 side = new Vector3(-direction.y, direction.x, 0f);
            line.transform.position = cue;
            line.SetPosition(0, -direction * .18f + side * .12f);
            line.SetPosition(1, Vector3.zero);
            line.SetPosition(2, -direction * .18f - side * .12f);
            line.enabled = true;
            TextMesh text = _arrowLabels[index];
            text.gameObject.SetActive(true);
            text.transform.position = cue - direction * .42f;
            if (_arrowShown[index] != label)
            {
                text.text = label;
                _arrowShown[index] = label;
            }
            ArrowsShown++;
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
        private bool Offscreen(Vector3 position, out Vector3 cue, out Vector3 direction)
        {
            cue = position;
            direction = Vector3.up;
            if (_camera == null)
                return false;
            Vector3 viewport = _camera.WorldToViewportPoint(position);
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
