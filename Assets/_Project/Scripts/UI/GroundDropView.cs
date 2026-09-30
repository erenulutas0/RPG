using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using UnityEngine;

namespace Cryptforge.UI
{
    // Observes rewards; never collects or pays them. Pooled roots keep each resting drop sorted at its floor position.
    public sealed class GroundDropView : MonoBehaviour
    {
        private sealed class Visual
        {
            public Transform Root;
            public SpriteRenderer Body;
            public GameObject Shadow;
            public GroundDrops.Drop Drop;
            public Vector3 Origin;
            public float Elapsed;
            public float Duration;
            public float Scale;
        }

        // Fixed full-canvas registration of reward-v1. Width is based on the strong source silhouette, not padding.
        private const float PaintedScale = .45f / (96f / 128f * 835f / 1254f);
        private const float ExperienceScale = .30f / (96f / 128f * 800f / 1254f);
        private const float PickupSeconds = .22f;
        private const float DistantSeconds = .42f;
        private readonly List<Visual> _ground = new List<Visual>(32);
        private readonly List<Visual> _flights = new List<Visual>(32);
        private readonly Stack<Visual> _free = new Stack<Visual>(32);
        private GroundDrops _drops;
        private RunState _run;
        private Transform _hero;
        private EncounterController _encounters;
        private Sprite _painted;
        private Sprite _paintedExperience;
        private Sprite _coin;
        private Sprite _experience;
        private bool _subscribed;
        private int _floor;

        public int GroundCount => _ground.Count;
        public int FlyingCount => _flights.Count;
        public int PoolCount { get; private set; }
        public Sprite PaintedSprite => _painted;
        public Sprite ExperienceSprite => _paintedExperience;
        // The reward sprites on show, resting first and then flying, for presentation that restyles them without
        // touching their shadows (FoundryVisibilityView). Parked pool entries are not on show.
        internal int ShownCount => _ground.Count + _flights.Count;
        internal SpriteRenderer ShownBody(int index) =>
            index < _ground.Count ? _ground[index].Body : _flights[index - _ground.Count].Body;

        internal void Initialize(GroundDrops drops, RunState run, Transform hero, Sprite painted,
            EncounterController encounters, Sprite experience = null)
        {
            Unsubscribe();
            Clear();
            _drops = drops;
            _run = run;
            _hero = hero;
            _painted = painted;
            _paintedExperience = experience;
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
            Visual visual = _free.Count > 0 ? _free.Pop() : CreateVisual();
            visual.Drop = drop;
            visual.Origin = new Vector3(drop.X, ArenaFloor.WorldY(drop.Y), 0f);
            visual.Root.position = visual.Origin;
            visual.Body.transform.localPosition = Vector3.zero;
            visual.Body.sprite = SpriteFor(drop);
            visual.Scale = visual.Body.sprite == _painted && _painted != null ? PaintedScale
                : visual.Body.sprite == _paintedExperience && _paintedExperience != null ? ExperienceScale : 1f;
            visual.Body.transform.localScale = Vector3.one * visual.Scale;
            visual.Body.color = Color.white;
            visual.Body.enabled = visual.Body.sprite != null;
            visual.Shadow.SetActive(visual.Body.enabled);
            visual.Root.gameObject.SetActive(true);
            _ground.Add(visual);
        }

        private Sprite SpriteFor(GroundDrops.Drop drop)
        {
            if (drop.Experience > 0 && drop.Gold > 0 && _painted != null) return _painted;
            if (drop.Experience > 0 && drop.Gold == 0 && _paintedExperience != null) return _paintedExperience;
            // Missing-art fallbacks never imply absent gold. Imported sprites are borrowed.
            if (drop.Experience > 0)
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
            if (drop.Gold <= 0) return null;
            if (_coin == null)
                _coin = PixelSpriteFactory.CreateSprite(EffectArt.CoinFrames()[0], "Reward Gold Fallback", PixelSpriteFactory.BottomCentre);
            return _coin;
        }

        private static bool Same(GroundDrops.Drop a, GroundDrops.Drop b) =>
            a.X == b.X && a.Y == b.Y && a.Experience == b.Experience && a.Gold == b.Gold;

        private void OnCollecting(GroundDrops.Drop drop)
        {
            for (int i = 0; i < _ground.Count; i++)
            {
                Visual visual = _ground[i];
                if (!Same(visual.Drop, drop)) continue;
                _ground.RemoveAt(i); // First matching occurrence preserves coincident, identical rewards.
                visual.Elapsed = 0f;
                visual.Duration = DropRule.IsWithinReach(_hero.position.x, ArenaFloor.FloorY(_hero.position.y), drop.X, drop.Y)
                    ? PickupSeconds : DistantSeconds;
                visual.Shadow.SetActive(false);
                _flights.Add(visual);
                return;
            }
        }

        private void Reconcile()
        {
            if (_run.HasEnded) { Clear(); return; }
            bool matches = _ground.Count == _drops.Count;
            for (int i = 0; matches && i < _ground.Count; i++) matches = Same(_ground[i].Drop, _drops.At(i));
            if (matches) return;
            foreach (Visual visual in _ground) Release(visual);
            _ground.Clear();
            for (int i = 0; i < _drops.Count; i++) OnDropped(_drops.At(i));
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
