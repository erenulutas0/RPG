using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only: what the run's growth looks like on the hero (docs/43, item 1). A level gained bursts at his feet
    // - a warm gold ring spreading over the floor and a few sparks rising round him - on scaled time, so a level-up that
    // opens an offer holds its burst under the panel and plays it out as the run moves again. A card taken rings him in
    // its tier's colour (RunChoiceView's four): a bright core first, the ring spreading after it; a gold card's ring is
    // wider and slower and sends gold sparks up. An evolution doubles the ring with a second in ember, the colour that
    // also marks the danger it brings, and flashes the hero white over his own silhouette. A weapon picked up pops a few
    // embers off his waist. The picks play on unscaled time: a pick is made while the offer holds the run still, and its
    // flash is seen as the panel closes, or behind the next one. No camera nudge: the only shake is CombatEffectsView's
    // own, which nothing outside it can start.
    //
    // Everything is made up front - rings and sparks as LineRenderers on one Sprites/Default material, and one overlay on
    // the hero's body - and an effect only moves, recolours and re-widens them; when every slot is busy the oldest gives
    // way. The effects follow the hero's feet, raised on the foundry onto the surface he stands on (FoundrySurface), the
    // rings at the floor's order like his contact shadow and ability ring, the sparks over the combatants like the hit
    // sparks. It reads the run and the upgrade service and writes nothing to either.
    [DefaultExecutionOrder(100)]
    public sealed class RunFeedbackView : MonoBehaviour
    {
        private enum Kind
        {
            LevelUp,
            Card,
            Evolution,
            Weapon
        }

        private sealed class Ring
        {
            public LineRenderer Line;
            public Kind Kind;
            public bool Active;
            public bool Unscaled;
            // Seconds after arming before the ring shows, so a second ring can follow the first.
            public float Delay;
            public float Elapsed;
            public float Lifetime;
            public float FromRadius;
            public float ToRadius;
            public float FromWidth;
            public float ToWidth;
            public Color Colour;
        }

        // A short streak whose head flies from Start (relative to the feet) at Velocity under Gravity, its tail behind it
        // along the way it is going, fading to nothing.
        private sealed class Spark
        {
            public LineRenderer Line;
            public Kind Kind;
            public bool Active;
            public bool Unscaled;
            public float Delay;
            public float Elapsed;
            public float Lifetime;
            public Vector3 Start;
            public Vector3 Velocity;
            public float Gravity;
            public float Length;
            public float Width;
            public Color Colour;
        }

        private const int RingSlots = 8;
        private const int SparkSlots = 32;
        private const int RingSegments = 40;
        // A long frame - the one the panel closes on, say - moves an effect on by at most this much, so a hitch never
        // swallows a flash whole.
        private const float MaxStep = 1f / 20f;
        // Over the combatants, with the hit sparks (CombatEffectsView draws them from 5).
        private const int AirOrder = 6;
        // Where a picked-up weapon's embers leave from, above the feet.
        private const float WaistHeight = .35f;
        private const int SparkSeed = 53;

        public const float LevelUpSeconds = .5f;
        private const float CardSeconds = .42f;
        private const float GoldSeconds = .7f;
        private const float CoreSeconds = .16f;
        private const float GoldCoreSeconds = .24f;
        private const float HeroFlashSeconds = .3f;

        private static readonly Color LevelGold = new Color(1f, .78f, .3f, 1f);
        private static readonly Color LevelSpark = new Color(1f, .92f, .6f, 1f);
        private static readonly Color Ember = new Color(1f, .55f, .22f, 1f);

        private RunState _run;
        private UpgradeService _upgrades;
        private Transform _hero;
        private ArenaView _arena;
        private bool _foundry;
        private Material _material;
        private Transform _root;
        private Ring[] _rings;
        private Spark[] _sparks;
        private int _nextRing;
        private int _nextSpark;
        private int _sparkBursts;
        // A unit floor circle as the screen sees it, and the ring scratch it is scaled into each frame.
        private readonly Vector3[] _unit = new Vector3[RingSegments];
        private readonly Vector3[] _points = new Vector3[RingSegments];
        private HeroLookView _look;
        private SpriteRenderer _heroBody;
        private SpriteRenderer _heroFlash;
        private Sprite _flashSource;
        private float _heroFlashRemaining;
        private bool _subscribed;

        // For tests and for anything that wants to know what the player was shown.
        public int ShownLevelUps { get; private set; }
        // Every card taken, an evolution included.
        public int ShownCardFlashes { get; private set; }
        public int ShownEvolutions { get; private set; }
        public int ShownWeaponPops { get; private set; }
        // The tier colour the last card taken was ringed in; clear until one is.
        public Color LastFlashColour { get; private set; } = Color.clear;
        public bool LastFlashWasGold { get; private set; }
        public bool IsLevelUpShowing => AnyActive(Kind.LevelUp);
        public bool IsCardFlashShowing => AnyActive(Kind.Card) || AnyActive(Kind.Evolution);
        public int ActiveEvolutionRings => ActiveRings(Kind.Evolution);
        public bool IsHeroFlashing => _heroFlash != null && _heroFlash.enabled && _heroFlash.sprite != null;

        internal void Initialize(RunState run, UpgradeService upgrades, Health hero)
        {
            _run = run;
            _upgrades = upgrades;
            _hero = hero.transform;
            _arena = FindFirstObjectByType<ArenaView>();
            _foundry = _arena != null && _arena.IsFoundryRegion;
            int floorOrder = _foundry ? _arena.GroundEffectSortingOrder : 0;
            _material = new Material(Shader.Find("Sprites/Default"));
            _root = new GameObject("Run Feedback").transform;
            _root.SetParent(transform, false);
            for (int k = 0; k < RingSegments; k++)
            {
                float a = k * Mathf.PI * 2f / RingSegments;
                _unit[k] = new Vector3(Mathf.Cos(a), ArenaFloor.WorldY(Mathf.Sin(a)), 0f);
            }

            _rings = new Ring[RingSlots];
            for (int i = 0; i < _rings.Length; i++)
            {
                LineRenderer line = Line("Feedback Ring " + i, floorOrder);
                line.loop = true;
                line.positionCount = RingSegments;
                _rings[i] = new Ring { Line = line };
            }
            _sparks = new Spark[SparkSlots];
            for (int i = 0; i < _sparks.Length; i++)
            {
                LineRenderer line = Line("Feedback Spark " + i, AirOrder);
                line.numCapVertices = 2;
                line.positionCount = 2;
                _sparks[i] = new Spark { Line = line };
            }

            // The overlay lives under the body, so the body's mirroring, its depth on the foundry and its hiding on death
            // all carry it; its order is set when it shows, once the look has chosen its art.
            _look = hero.GetComponent<HeroLookView>();
            _heroBody = _look != null ? _look.Body : null;
            if (_heroBody != null)
            {
                var flash = new GameObject("Evolution Flash");
                flash.transform.SetParent(_heroBody.transform, false);
                _heroFlash = flash.AddComponent<SpriteRenderer>();
                _heroFlash.enabled = false;
            }

            _run.LevelChanged += OnLevelChanged;
            _upgrades.Selected += OnSelected;
            _upgrades.WeaponAcquired += OnWeaponAcquired;
            _subscribed = true;
            Follow();
        }

        private LineRenderer Line(string name, int order)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(_root, false);
            line.sharedMaterial = _material;
            line.useWorldSpace = false;
            line.sortingOrder = order;
            line.enabled = false;
            return line;
        }

        // A disabled view shows nothing and counts nothing: a line armed now would stand frozen until it was enabled.
        private void OnLevelChanged()
        {
            if (!isActiveAndEnabled)
                return;
            ShownLevelUps++;
            ArmRing(Kind.LevelUp, false, 0f, LevelUpSeconds, LevelGold, .2f, .95f, .1f, .02f);
            Rise(Kind.LevelUp, false, 7, .32f, 1.8f, LevelUpSeconds, LevelSpark);
        }

        // Selected comes after LastSelected holds the offer the card came from, so the tier is the one the card showed.
        private void OnSelected(UpgradeOption option, int slot)
        {
            if (!isActiveAndEnabled)
                return;
            UpgradeOffer offer = _upgrades.LastSelected;
            UpgradeRarity rarity = offer != null ? offer.RarityAt(slot) : UpgradeRarity.Common;
            Color colour = RunChoiceView.RarityColour(rarity);
            bool gold = rarity == UpgradeRarity.Gold;
            ShownCardFlashes++;
            LastFlashColour = colour;
            LastFlashWasGold = gold;
            if (option != null && option.IsEvolution)
            {
                ShowEvolution(colour);
                return;
            }

            Color core = Color.Lerp(colour, Color.white, .55f);
            if (gold)
            {
                ArmRing(Kind.Card, true, 0f, GoldCoreSeconds, core, .16f, .42f, .28f, .08f);
                ArmRing(Kind.Card, true, .04f, GoldSeconds, colour, .2f, 1.3f, .12f, .03f);
                Rise(Kind.Card, true, 6, .4f, 2.1f, .6f, Color.Lerp(colour, Color.white, .35f));
                return;
            }
            ArmRing(Kind.Card, true, 0f, CoreSeconds, core, .12f, .3f, .22f, .06f);
            ArmRing(Kind.Card, true, .03f, CardSeconds, colour, .2f, .85f, .08f, .02f);
        }

        // The card's own ring, as wide as a gold one, and a second in ember just behind it; a white core; ember sparks;
        // and the hero's silhouette lit white over him.
        private void ShowEvolution(Color colour)
        {
            ShownEvolutions++;
            ArmRing(Kind.Evolution, true, 0f, GoldCoreSeconds, Color.Lerp(colour, Color.white, .7f), .14f, .4f, .3f, .08f);
            ArmRing(Kind.Evolution, true, .04f, GoldSeconds, colour, .2f, 1.25f, .13f, .03f);
            ArmRing(Kind.Evolution, true, .18f, .6f, Ember, .15f, .95f, .1f, .025f);
            Rise(Kind.Evolution, true, 8, .45f, 2.4f, .6f, Color.Lerp(Ember, Color.white, .3f));
            ShowHeroFlash();
        }

        // A few embers thrown up and out from the waist, falling as they fade, over a small ember ring at the feet.
        private void OnWeaponAcquired(WeaponCard card, WeaponRuntime runtime)
        {
            if (!isActiveAndEnabled)
                return;
            ShownWeaponPops++;
            ArmRing(Kind.Weapon, true, 0f, .3f, Ember, .1f, .45f, .06f, .015f);
            const int count = 6;
            Color hot = Color.Lerp(Ember, Color.white, .25f);
            _sparkBursts++;
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(25f, 155f, i / (count - 1f)) * Mathf.Deg2Rad;
                float n = PixelNoise.Value(i, _sparkBursts, SparkSeed);
                var velocity = new Vector3(Mathf.Cos(a) * 1.6f, .9f + Mathf.Sin(a) * 1.1f + n * .3f, 0f);
                ArmSpark(Kind.Weapon, true, 0f, .38f, new Vector3(0f, WaistHeight, 0f), velocity, 6.5f, .09f, .045f, hot);
            }
        }

        // Sparks round the feet on a floor ellipse, rising and slowing as they fade, a few at a time.
        private void Rise(Kind kind, bool unscaled, int count, float radius, float speed, float lifetime, Color colour)
        {
            _sparkBursts++;
            for (int i = 0; i < count; i++)
            {
                float n = PixelNoise.Value(i, _sparkBursts, SparkSeed);
                float a = (i + .5f + (n - .5f) * .6f) * Mathf.PI * 2f / count;
                var start = new Vector3(Mathf.Cos(a) * radius, ArenaFloor.WorldY(Mathf.Sin(a) * radius) + .05f, 0f);
                var velocity = new Vector3(Mathf.Cos(a) * .3f, speed + n * .5f, 0f);
                ArmSpark(kind, unscaled, (i % 3) * .04f, lifetime * (.85f + .15f * n), start, velocity, speed * 1.3f,
                    .14f, .05f, colour);
            }
        }

        private void ArmRing(Kind kind, bool unscaled, float delay, float lifetime, Color colour, float fromRadius,
            float toRadius, float fromWidth, float toWidth)
        {
            Ring ring = _rings[_nextRing];
            _nextRing = (_nextRing + 1) % _rings.Length;
            ring.Kind = kind;
            ring.Active = true;
            ring.Unscaled = unscaled;
            ring.Delay = delay;
            ring.Elapsed = 0f;
            ring.Lifetime = lifetime;
            ring.Colour = colour;
            ring.FromRadius = fromRadius;
            ring.ToRadius = toRadius;
            ring.FromWidth = fromWidth;
            ring.ToWidth = toWidth;
            Draw(ring);
        }

        private void ArmSpark(Kind kind, bool unscaled, float delay, float lifetime, Vector3 start, Vector3 velocity,
            float gravity, float length, float width, Color colour)
        {
            Spark spark = _sparks[_nextSpark];
            _nextSpark = (_nextSpark + 1) % _sparks.Length;
            spark.Kind = kind;
            spark.Active = true;
            spark.Unscaled = unscaled;
            spark.Delay = delay;
            spark.Elapsed = 0f;
            spark.Lifetime = lifetime;
            spark.Start = start;
            spark.Velocity = velocity;
            spark.Gravity = gravity;
            spark.Length = length;
            spark.Width = width;
            spark.Colour = colour;
            Draw(spark);
        }

        private void ShowHeroFlash()
        {
            if (_heroFlash == null)
                return;
            _heroFlash.sortingLayerID = _heroBody.sortingLayerID;
            // Where CombatantView puts its hit flash: over the imported hero's front hand too.
            _heroFlash.sortingOrder = _heroBody.sortingOrder + (_look.UsesPaintedArt ? 5 : 1);
            _heroFlashRemaining = HeroFlashSeconds;
            _flashSource = null;
            DrawHeroFlash();
        }

        private void LateUpdate()
        {
            if (_root == null)
                return;
            float scaled = Mathf.Min(Time.deltaTime, MaxStep);
            float unscaled = Mathf.Min(Time.unscaledDeltaTime, MaxStep);
            Follow();
            for (int i = 0; i < _rings.Length; i++)
            {
                Ring ring = _rings[i];
                if (!ring.Active)
                    continue;
                ring.Elapsed += ring.Unscaled ? unscaled : scaled;
                Draw(ring);
            }
            for (int i = 0; i < _sparks.Length; i++)
            {
                Spark spark = _sparks[i];
                if (!spark.Active)
                    continue;
                spark.Elapsed += spark.Unscaled ? unscaled : scaled;
                Draw(spark);
            }
            if (_heroFlashRemaining > 0f)
            {
                _heroFlashRemaining -= unscaled;
                DrawHeroFlash();
            }
        }

        // The hero's feet as drawn: his root, raised on the foundry onto the surface under it.
        private void Follow()
        {
            Vector3 feet = _hero.position;
            feet.z = 0f;
            if (_foundry)
                feet.y += FoundrySurface.Lift(feet, _arena.transform.position);
            _root.position = feet;
        }

        // Spreads fast and eases out, thins as it goes, and holds its light for the first third before fading.
        private void Draw(Ring ring)
        {
            float shown = ring.Elapsed - ring.Delay;
            if (shown >= ring.Lifetime)
            {
                ring.Active = false;
                ring.Line.enabled = false;
                return;
            }
            if (shown < 0f)
            {
                ring.Line.enabled = false;
                return;
            }

            float t = shown / ring.Lifetime;
            float ease = 1f - (1f - t) * (1f - t) * (1f - t);
            float radius = Mathf.Lerp(ring.FromRadius, ring.ToRadius, ease);
            for (int k = 0; k < RingSegments; k++)
                _points[k] = _unit[k] * radius;
            LineRenderer line = ring.Line;
            line.SetPositions(_points);
            line.startWidth = line.endWidth = Mathf.Lerp(ring.FromWidth, ring.ToWidth, t);
            Color colour = ring.Colour;
            colour.a *= 1f - Mathf.SmoothStep(0f, 1f, (t - .3f) / .7f);
            line.startColor = line.endColor = colour;
            line.enabled = true;
        }

        private void Draw(Spark spark)
        {
            float shown = spark.Elapsed - spark.Delay;
            if (shown >= spark.Lifetime)
            {
                spark.Active = false;
                spark.Line.enabled = false;
                return;
            }
            if (shown < 0f)
            {
                spark.Line.enabled = false;
                return;
            }

            float t = shown / spark.Lifetime;
            Vector3 head = spark.Start + spark.Velocity * shown;
            head.y -= .5f * spark.Gravity * shown * shown;
            Vector3 heading = spark.Velocity;
            heading.y -= spark.Gravity * shown;
            heading = heading.sqrMagnitude > 1e-4f ? heading.normalized : Vector3.up;
            LineRenderer line = spark.Line;
            line.SetPosition(0, head);
            line.SetPosition(1, head - heading * spark.Length);
            line.startWidth = spark.Width * (1f - .5f * t);
            line.endWidth = 0f;
            Color colour = spark.Colour;
            colour.a *= 1f - Mathf.SmoothStep(0f, 1f, (t - .35f) / .65f);
            line.startColor = colour;
            colour.a = 0f;
            line.endColor = colour;
            line.enabled = true;
        }

        // The look may change the body's frame mid-flash; the overlay keeps to the silhouette of what is shown. It never
        // touches the body's own colour, so there is nothing to restore when it ends.
        private void DrawHeroFlash()
        {
            if (_heroFlashRemaining <= 0f || !_heroBody.enabled)
            {
                EndHeroFlash();
                return;
            }
            if (!ReferenceEquals(_heroBody.sprite, _flashSource))
            {
                _flashSource = _heroBody.sprite;
                _heroFlash.sprite = _look.SilhouetteOf(_flashSource);
            }
            float t = _heroFlashRemaining / HeroFlashSeconds;
            _heroFlash.color = new Color(1f, 1f, 1f, .9f * t * t);
            _heroFlash.enabled = _heroFlash.sprite != null;
        }

        private void EndHeroFlash()
        {
            _heroFlashRemaining = 0f;
            _flashSource = null;
            if (_heroFlash != null)
                _heroFlash.enabled = false;
        }

        private bool AnyActive(Kind kind)
        {
            for (int i = 0; _rings != null && i < _rings.Length; i++)
                if (_rings[i].Active && _rings[i].Kind == kind)
                    return true;
            for (int i = 0; _sparks != null && i < _sparks.Length; i++)
                if (_sparks[i].Active && _sparks[i].Kind == kind)
                    return true;
            return false;
        }

        private int ActiveRings(Kind kind)
        {
            int count = 0;
            for (int i = 0; _rings != null && i < _rings.Length; i++)
                if (_rings[i].Active && _rings[i].Kind == kind)
                    count++;
            return count;
        }

        // Also on the way out of the scene, where a pooled line may already be gone.
        private void HideAll()
        {
            for (int i = 0; _rings != null && i < _rings.Length; i++)
            {
                _rings[i].Active = false;
                if (_rings[i].Line != null)
                    _rings[i].Line.enabled = false;
            }
            for (int i = 0; _sparks != null && i < _sparks.Length; i++)
            {
                _sparks[i].Active = false;
                if (_sparks[i].Line != null)
                    _sparks[i].Line.enabled = false;
            }
            EndHeroFlash();
        }

        private void OnDisable() => HideAll();

        private void OnDestroy()
        {
            if (_subscribed)
            {
                _run.LevelChanged -= OnLevelChanged;
                _upgrades.Selected -= OnSelected;
                _upgrades.WeaponAcquired -= OnWeaponAcquired;
                _subscribed = false;
            }
            if (_heroFlash != null)
                Destroy(_heroFlash.gameObject);
            if (_material != null)
                Destroy(_material);
        }
    }
}
