using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using UnityEngine;

namespace Cryptforge.UI
{
    // Observes rewards; never collects or pays them. Pooled roots keep each resting pile sorted at its floor position.
    // Drops lying in one small square of the floor share one pile on show, and only the piles nearest the hero are drawn.
    // Each visual is a reward sprite and its contact shadow, and on the device they did not batch: drops left lying added
    // draw calls, and SetPass calls beside them, until the floor ended - 70 to 250 in 40 s of a standing hero on the S23
    // (docs/21, the density probe). On the foundry a visual costs three: the body's two silhouette passes and its shadow.
    // Piles are presentation only: GroundDrops still holds, picks up and pays every drop on its own, so the scene and the
    // Descent simulation collect the same drops on the same frame, and a floor pays what its kills paid.
    public sealed class GroundDropView : MonoBehaviour
    {
        private sealed class Visual
        {
            public Transform Root;
            public SpriteRenderer Body;
            public GameObject Shadow;
            public Vector3 Origin;
            public float Elapsed;
            public float Duration;
            public float Scale;
        }

        // The drops of one square of the floor, oldest first; it stands on the oldest.
        private sealed class Pile
        {
            public long Cell;
            public int Serial;
            public readonly List<GroundDrops.Drop> Drops = new List<GroundDrops.Drop>(4);
            public Visual Visual;
            public float Distance;
            public bool Touched;
            public int TakenExperience;
            public int TakenGold;
        }

        private sealed class NearestFirst : IComparer<Pile>
        {
            public int Compare(Pile a, Pile b)
            {
                int byDistance = a.Distance.CompareTo(b.Distance);
                return byDistance != 0 ? byDistance : a.Serial.CompareTo(b.Serial);
            }
        }

        // A pile is the drops of one square of the floor this wide. Its corners lie closer than the pickup reach
        // (.7 x 1.414 < DropRule.PickupRadius), so a hero standing on any drop of a pile gathers the whole of it.
        internal const float PileCell = .7f;
        // Piles drawn at once, nearest the hero first: three draw calls each on the foundry, so drops left lying cost at
        // most sixty however many there are. A pile beyond the cap waits unseen, still pays when walked over, and comes on
        // show as the hero nears it. Twenty keeps every drop of the ground-reward review captures (eighteen) on show.
        internal const int MaxShownPiles = 20;
        // How far the hero walks, in floor units, before the piles on show are chosen again.
        private const float RechooseDistance = 1f;
        // A pile reads larger the more it holds, by this much for each drop past the first, up to five.
        private const float PileGrowth = .06f;
        private const int PileGrowthDrops = 5;
        // Fixed full-canvas registration of reward-v1. Width is based on the strong source silhouette, not padding.
        private const float PaintedScale = .45f / (96f / 128f * 835f / 1254f);
        private const float ExperienceScale = .30f / (96f / 128f * 800f / 1254f);
        private const float PickupSeconds = .22f;
        private const float DistantSeconds = .42f;
        private readonly List<Visual> _ground = new List<Visual>(MaxShownPiles);
        private readonly List<Visual> _flights = new List<Visual>(32);
        private readonly Stack<Visual> _free = new Stack<Visual>(32);
        private readonly List<Pile> _piles = new List<Pile>(64);
        private readonly Dictionary<long, Pile> _pileAt = new Dictionary<long, Pile>(64);
        private readonly Stack<Pile> _freePiles = new Stack<Pile>(64);
        private readonly List<Pile> _touched = new List<Pile>(8);
        private readonly List<Pile> _ranked = new List<Pile>(64);
        private readonly NearestFirst _nearestFirst = new NearestFirst();
        // The drops as this view last saw GroundDrops hold them, in its order, so a missed event shows as a difference.
        private readonly List<GroundDrops.Drop> _lying = new List<GroundDrops.Drop>(64);
        private GroundDrops _drops;
        private RunState _run;
        private Transform _hero;
        private EncounterController _encounters;
        private Sprite _painted;
        private Sprite _paintedExperience;
        private Sprite _paintedGold;
        private Sprite _coin;
        private Sprite _experience;
        private bool _subscribed;
        private bool _lost;
        private int _floor;
        private int _serial;
        private float _chosenX;
        private float _chosenY;

        // Piles resting on show; each is one reward sprite and its shadow, however many drops it holds.
        public int GroundCount => _ground.Count;
        public int FlyingCount => _flights.Count;
        public int PoolCount { get; private set; }
        public Sprite PaintedSprite => _painted;
        public Sprite ExperienceSprite => _paintedExperience;
        public Sprite GoldSprite => _paintedGold;
        // The reward sprites on show, resting first and then flying, for presentation that restyles them without
        // touching their shadows (FoundryVisibilityView). Parked pool entries are not on show.
        internal int ShownCount => _ground.Count + _flights.Count;
        internal SpriteRenderer ShownBody(int index) =>
            index < _ground.Count ? _ground[index].Body : _flights[index - _ground.Count].Body;
        // Every pile lying, on show or waiting beyond the cap, oldest first.
        internal int PileCount => _piles.Count;
        internal int PileSize(int index) => _piles[index].Drops.Count;
        internal GroundDrops.Drop PileAnchor(int index) => _piles[index].Drops[0];
        internal bool IsPileShown(int index) => _piles[index].Visual != null;
        internal Transform PileRoot(int index) => _piles[index].Visual != null ? _piles[index].Visual.Root : null;
        // The drops the piles hold: GroundDrops.Count while the view follows the floor.
        internal int PiledDropCount
        {
            get
            {
                int count = 0;
                foreach (Pile pile in _piles) count += pile.Drops.Count;
                return count;
            }
        }

        internal void Initialize(GroundDrops drops, RunState run, Transform hero, Sprite painted,
            EncounterController encounters, Sprite experience = null, Sprite gold = null)
        {
            Unsubscribe();
            Clear();
            _drops = drops;
            _run = run;
            _hero = hero;
            _painted = painted;
            _paintedExperience = experience;
            _paintedGold = gold;
            _encounters = encounters;
            _floor = encounters != null ? encounters.FloorNumber : 0;
            // A small initial pool, with growth rather than overwriting a still-uncollected reward.
            while (PoolCount < 16) _free.Push(CreateVisual());
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() { Unsubscribe(); Clear(); }

        private void Subscribe()
        {
            if (_subscribed || _drops == null || _run == null || _hero == null) return;
            _drops.Dropped += OnDropped;
            _drops.Collecting += OnCollecting;
            _drops.Changed += Reconcile;
            _run.Ended += Clear;
            if (_encounters != null) _encounters.EncounterStarted += OnEncounterStarted;
            _subscribed = true;
            Reconcile();
        }

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _drops.Dropped -= OnDropped;
            _drops.Collecting -= OnCollecting;
            _drops.Changed -= Reconcile;
            _run.Ended -= Clear;
            if (_encounters != null) _encounters.EncounterStarted -= OnEncounterStarted;
            _subscribed = false;
        }

        private Visual CreateVisual()
        {
            var root = new GameObject("Ground Reward");
            root.transform.SetParent(transform, false);
            GroundedSorting.Attach(root.transform, 0, 1);
            var body = new GameObject("Reward Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(root.transform, false);
            body.sortingOrder = 1;
            var shadow = ContactShadowView.Attach(root.transform, body, .36f, null);
            root.SetActive(false);
            PoolCount++;
            return new Visual { Root = root.transform, Body = body, Shadow = shadow.gameObject };
        }

        private void OnDropped(GroundDrops.Drop drop)
        {
            if (_run.HasEnded) return;
            _lying.Add(drop);
            Pile pile = Lay(drop);
            if (pile.Visual != null) Dress(pile);
            // A new pile: on show at once while the cap has room, or if it lies nearer than one on show.
            else if (pile.Drops.Count == 1) ShowNearest();
        }

        private Pile Lay(GroundDrops.Drop drop)
        {
            long cell = CellOf(drop.X, drop.Y);
            if (!_pileAt.TryGetValue(cell, out Pile pile))
            {
                pile = _freePiles.Count > 0 ? _freePiles.Pop() : new Pile();
                pile.Cell = cell;
                pile.Serial = _serial++;
                _pileAt.Add(cell, pile);
                _piles.Add(pile);
            }
            pile.Drops.Add(drop);
            return pile;
        }

        private static long CellOf(float x, float y) =>
            ((long)Mathf.FloorToInt(x / PileCell) << 32) | (uint)Mathf.FloorToInt(y / PileCell);

        // Puts the piles nearest the hero on show, at most MaxShownPiles, and parks the rest with their drops still lying.
        internal void ShowNearest()
        {
            float heroX = _hero.position.x;
            float heroY = ArenaFloor.FloorY(_hero.position.y);
            _chosenX = heroX;
            _chosenY = heroY;
            if (_piles.Count <= MaxShownPiles)
            {
                foreach (Pile pile in _piles)
                    if (pile.Visual == null) Show(pile);
                return;
            }
            _ranked.Clear();
            foreach (Pile pile in _piles)
            {
                GroundDrops.Drop anchor = pile.Drops[0];
                pile.Distance = ArenaFloor.FloorDistanceSquared(heroX, heroY, anchor.X, anchor.Y);
                _ranked.Add(pile);
            }
            _ranked.Sort(_nearestFirst);
            // Park first, so the piles coming on show take the visuals just parked rather than new ones.
            for (int i = MaxShownPiles; i < _ranked.Count; i++) Park(_ranked[i]);
            for (int i = 0; i < MaxShownPiles; i++)
                if (_ranked[i].Visual == null) Show(_ranked[i]);
            _ranked.Clear();
        }

        private void Show(Pile pile)
        {
            Visual visual = _free.Count > 0 ? _free.Pop() : CreateVisual();
            pile.Visual = visual;
            _ground.Add(visual);
            Dress(pile);
            visual.Root.gameObject.SetActive(true);
        }

        private void Park(Pile pile)
        {
            if (pile.Visual == null) return;
            _ground.Remove(pile.Visual);
            Release(pile.Visual);
            pile.Visual = null;
        }

        // Stands a pile on its oldest drop, in the art of everything it holds and a little larger for each drop it adds.
        private void Dress(Pile pile)
        {
            int experience = 0, gold = 0;
            foreach (GroundDrops.Drop drop in pile.Drops)
            {
                experience += drop.Experience;
                gold += drop.Gold;
            }
            GroundDrops.Drop anchor = pile.Drops[0];
            float growth = 1f + PileGrowth * Mathf.Min(pile.Drops.Count - 1, PileGrowthDrops);
            Place(pile.Visual, new Vector3(anchor.X, ArenaFloor.WorldY(anchor.Y), 0f), experience, gold, growth);
        }

        private void Place(Visual visual, Vector3 origin, int experience, int gold, float growth)
        {
            visual.Origin = origin;
            visual.Root.position = origin;
            visual.Body.transform.localPosition = Vector3.zero;
            visual.Body.sprite = SpriteFor(experience, gold);
            // The gold-only coins are the pile's own, painted at its size, so they keep its scale.
            float scale = visual.Body.sprite != null && (visual.Body.sprite == _painted || visual.Body.sprite == _paintedGold)
                ? PaintedScale
                : visual.Body.sprite == _paintedExperience && _paintedExperience != null ? ExperienceScale : 1f;
            visual.Scale = scale * growth;
            visual.Body.transform.localScale = Vector3.one * visual.Scale;
            visual.Body.color = Color.white;
            visual.Body.enabled = visual.Body.sprite != null;
            visual.Shadow.SetActive(visual.Body.enabled);
        }

        private Sprite SpriteFor(int experience, int gold)
        {
            if (experience > 0 && gold > 0 && _painted != null) return _painted;
            if (experience > 0 && gold == 0 && _paintedExperience != null) return _paintedExperience;
            if (experience == 0 && gold > 0 && _paintedGold != null) return _paintedGold;
            // Missing-art fallbacks never imply absent gold. Imported sprites are borrowed.
            if (experience > 0)
            {
                if (_experience == null)
                {
                    var gem = new PixelCanvas(12, 16);
                    gem.FillTriangle(6, 15, 1, 7, 6, 0, PixelPalette.SpellLight);
                    gem.FillTriangle(6, 15, 10, 7, 6, 0, PixelPalette.Spell);
                    gem.Outline(PixelPalette.Outline);
                    _experience = PixelSpriteFactory.CreateSprite(gem, "Reward XP Fallback", PixelSpriteFactory.BottomCentre);
                }
                return _experience;
            }
            if (gold <= 0) return null;
            if (_coin == null)
                _coin = PixelSpriteFactory.CreateSprite(EffectArt.CoinFrames()[0], "Reward Gold Fallback", PixelSpriteFactory.BottomCentre);
            return _coin;
        }

        private static bool Same(GroundDrops.Drop a, GroundDrops.Drop b) =>
            a.X == b.X && a.Y == b.Y && a.Experience == b.Experience && a.Gold == b.Gold;

        private static int IndexOf(List<GroundDrops.Drop> drops, GroundDrops.Drop drop)
        {
            for (int i = 0; i < drops.Count; i++)
                if (Same(drops[i], drop)) return i;
            return -1;
        }

        // A drop leaves its pile as it pays; the pile shows what happened once GroundDrops has finished (Gather).
        private void OnCollecting(GroundDrops.Drop drop)
        {
            // First matching occurrence, as GroundDrops itself picks up the oldest of coincident, identical rewards.
            int lying = IndexOf(_lying, drop);
            if (lying < 0 || !_pileAt.TryGetValue(CellOf(drop.X, drop.Y), out Pile pile)) { _lost = true; return; }
            int member = IndexOf(pile.Drops, drop);
            if (member < 0) { _lost = true; return; }
            _lying.RemoveAt(lying);
            pile.Drops.RemoveAt(member);
            if (!pile.Touched)
            {
                pile.Touched = true;
                _touched.Add(pile);
            }
            pile.TakenExperience += drop.Experience;
            pile.TakenGold += drop.Gold;
        }

        private void Reconcile()
        {
            if (_run.HasEnded) { Clear(); return; }
            if (_lost || !Follows()) { Rebuild(); return; }
            if (_touched.Count > 0) Gather();
        }

        private bool Follows()
        {
            if (_lying.Count != _drops.Count) return false;
            for (int i = 0; i < _lying.Count; i++)
                if (!Same(_lying[i], _drops.At(i))) return false;
            return true;
        }

        // The drops just picked up leave their piles. A pile emptied flies to the hero whole; one still holding drops sends
        // what was taken and stands again on its oldest drop left. A pile parked beyond the cap was never on show and sends
        // nothing, so a floor's last sweep flies at most the piles on show.
        private void Gather()
        {
            bool emptied = false;
            foreach (Pile pile in _touched)
            {
                Visual visual = pile.Visual;
                if (pile.Drops.Count == 0)
                {
                    if (visual != null)
                    {
                        _ground.Remove(visual);
                        Fly(visual);
                    }
                    Retire(pile);
                    emptied = true;
                    continue;
                }
                if (visual != null)
                {
                    Visual taken = _free.Count > 0 ? _free.Pop() : CreateVisual();
                    Place(taken, visual.Origin, pile.TakenExperience, pile.TakenGold, 1f);
                    taken.Root.gameObject.SetActive(true);
                    Fly(taken);
                    Dress(pile);
                }
                pile.Touched = false;
                pile.TakenExperience = 0;
                pile.TakenGold = 0;
            }
            _touched.Clear();
            // A place on show came free: the nearest pile waiting takes it.
            if (emptied && _piles.Count > _ground.Count) ShowNearest();
        }

        private void Fly(Visual visual)
        {
            visual.Elapsed = 0f;
            visual.Duration = DropRule.IsWithinReach(_hero.position.x, ArenaFloor.FloorY(_hero.position.y),
                visual.Origin.x, ArenaFloor.FloorY(visual.Origin.y)) ? PickupSeconds : DistantSeconds;
            visual.Shadow.SetActive(false);
            _flights.Add(visual);
        }

        private void Retire(Pile pile)
        {
            _pileAt.Remove(pile.Cell);
            _piles.Remove(pile);
            Recycle(pile);
        }

        private void Recycle(Pile pile)
        {
            pile.Visual = null;
            pile.Drops.Clear();
            pile.Touched = false;
            pile.TakenExperience = 0;
            pile.TakenGold = 0;
            _freePiles.Push(pile);
        }

        // Lays every drop again from GroundDrops, oldest first: after the view was off, or once it lost track of one.
        private void Rebuild()
        {
            foreach (Visual visual in _ground) Release(visual);
            _ground.Clear();
            ForgetPiles();
            for (int i = 0; i < _drops.Count; i++)
            {
                GroundDrops.Drop drop = _drops.At(i);
                _lying.Add(drop);
                Lay(drop);
            }
            if (_piles.Count > 0) ShowNearest();
        }

        private void ForgetPiles()
        {
            foreach (Pile pile in _piles) Recycle(pile);
            _piles.Clear();
            _pileAt.Clear();
            _touched.Clear();
            _lying.Clear();
            _lost = false;
        }

        private void OnEncounterStarted()
        {
            if (_encounters.FloorNumber == _floor) return;
            _floor = _encounters.FloorNumber;
            // A checkpoint can pause an in-flight sweep; do not bring its cosmetic trail onto the next floor.
            foreach (Visual visual in _flights) Release(visual);
            _flights.Clear();
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f || _hero == null) return;
            // Piles wait beyond the cap: choose again once the hero has walked a step from where they were chosen.
            if (_subscribed && _piles.Count > _ground.Count && ArenaFloor.FloorDistanceSquared(_hero.position.x,
                    ArenaFloor.FloorY(_hero.position.y), _chosenX, _chosenY) >= RechooseDistance * RechooseDistance)
                ShowNearest();
            for (int i = _flights.Count - 1; i >= 0; i--)
            {
                Visual visual = _flights[i];
                visual.Elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(visual.Elapsed / visual.Duration);
                if (t >= 1f) { Release(visual); _flights.RemoveAt(i); continue; }
                float ease = t * t * (3f - 2f * t);
                // Root follows the floor for sorting; a separate child provides the small decorative hop.
                visual.Root.position = Vector3.Lerp(visual.Origin, _hero.position, ease);
                visual.Body.transform.localPosition = Vector3.up * (Mathf.Sin(t * Mathf.PI) * .18f);
                visual.Body.transform.localScale = Vector3.one * (visual.Scale * (1f - .5f * t));
                visual.Body.color = new Color(1f, 1f, 1f, 1f - Mathf.Clamp01((t - .65f) / .35f));
            }
        }

        private void Release(Visual visual)
        {
            visual.Root.gameObject.SetActive(false);
            _free.Push(visual);
        }

        private void Clear()
        {
            foreach (Visual visual in _ground) Release(visual);
            foreach (Visual visual in _flights) Release(visual);
            _ground.Clear();
            _flights.Clear();
            ForgetPiles();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            PixelSpriteFactory.Destroy(_coin);
            PixelSpriteFactory.Destroy(_experience);
            // The painted sprite and contact shadow are borrowed. Their owners control native lifetime.
        }
    }
}
