using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only - the game-feel pass on the foundry's gate boss (docs/43 section 1, 2026-11-22), the Forge Warden
    // and the Glass Regent, over what their looks already play (the raised hammer or mirror and the strike pose,
    // EnemyLookView.WindUp and Slam; the hotter second-phase frames; the painted fall) and WardenSlamView's mark:
    // - the tell: from the moment a mark goes down to the blow the body rises onto its toes, leans back away from the mark
    //   and glows - a silhouette of its own frame in its colour (ember for the Warden, glass violet for the Regent) gathering
    //   to the blow and beating with the mark's own edge, the tremble of the last quarter before it lands. It is the
    //   clock's own progress (WardenSlamClock.TelegraphProgress), read in the same frame as the mark, so it begins on the
    //   mark's frame and ends on the blow's. The blow kicks the body forward and down and the glow flashes once. A Regent
    //   keeping its rod at its feet (WardenSlamService.RodAtItsFeet) raises nothing: its cast still reads in the glow.
    // - phase two (WardenSlamService.IsEnraged): a burst - two rings over the floor and embers thrown up - and from then a
    //   slow glow in a hotter colour, a tint shift that never touches the body's own colours (its painted second phase
    //   draws the enrage; CombatantView keeps no tint for it).
    // - death: the Warden's fall opens with a ring and a spray of embers and ends with its remains dissolving in ash; the
    //   Regent shatters - glass shards flung out and falling, a white ring - and its remains end in glints.
    // Telegraphs stay the clearest thing on screen: through a telegraph the glow never passes GameFeel.TellGlowPeak (only
    // the blow's and phase two's brief flashes go past it, to 0.7), and the rings are thin and gone in half a second.
    //
    // Its state steps right after the service's clock (Update at -30, the service's at -40), so the tell is the clock's of
    // the same frame. The body is moved only while the tell or the kick plays, over EnemyLookView's rest and its flinch,
    // after every Update (LateUpdate), and is put back once; nothing else it owns is touched. Its glow is one renderer,
    // hung under the boss's body while it is fought - so it mirrors, sorts and stands at depth with it - and taken back
    // when it falls. The rings, embers, shards and ash are pooled under one root, made once. Scaled time: a pause holds it.
    [DefaultExecutionOrder(-30)]
    public sealed class BossFeelView : MonoBehaviour
    {
        public const string RootName = "Boss Feel";
        internal const int ParticleSlots = 40;
        internal const int RingSlots = 3;
        private const int RingPoints = 40;
        // The tell at full: the rise (world units), the lean back (degrees), the tremble of its last quarter.
        private const float WardenRise = .1f;
        private const float RegentRise = .08f;
        private const float WardenLean = 6f;
        private const float RegentLean = 4f;
        private const float TrembleFrom = .75f;
        private const float TrembleDegrees = .8f;
        // The blow's kick: forward toward the mark and down, settling over KickSeconds; the glow's flash with it.
        internal const float KickSeconds = .28f;
        private const float KickLean = 7f;
        private const float KickDip = .05f;
        private const float KickGlow = .6f;
        // Phase two's slow glow: a beat of PhaseTwoPeriod between these strengths.
        private const float PhaseTwoPeriod = 1.4f;
        private const float PhaseTwoLow = .08f;
        private const float PhaseTwoHigh = .18f;
        private const float BurstFlashSeconds = .45f;
        private const float BurstFlash = .7f;

        private static readonly Color WardenGlow = new Color(1f, .5f, .18f, 1f);
        private static readonly Color WardenHot = new Color(1f, .3f, .1f, 1f);
        private static readonly Color RegentGlow = new Color(.82f, .74f, 1f, 1f);
        private static readonly Color RegentHot = new Color(.92f, .45f, 1f, 1f);
        private static readonly Color Ember = new Color(1f, .55f, .22f, .95f);
        private static readonly Color Ash = new Color(.4f, .38f, .38f, .85f);
        private static readonly Color Glass = new Color(.85f, .92f, 1f, .95f);
        private static readonly Color GlassViolet = new Color(.75f, .62f, 1f, .9f);

        private sealed class Particle
        {
            public SpriteRenderer Renderer;
            public bool Active;
            public float Delay;
            public float Elapsed;
            public float Lifetime;
            public Vector3 Origin;
            public Vector3 Velocity;
            public float Gravity;
            public float Width;
            public float Length;
            public bool Shard;
            public float Spin;
            public Color Colour;
        }

        private sealed class Ring
        {
            public LineRenderer Line;
            public Vector3[] Points;
            public bool Active;
            public float Delay;
            public float Elapsed;
            public float Lifetime;
            public Vector3 Centre;
            public float From;
            public float To;
            public float FromWidth;
            public float ToWidth;
            public Color Colour;
        }

        private WardenSlamService _service;
        private ArenaView _arena;
        private Material _material;
        private Transform _root;
        private Particle[] _particles;
        private Ring[] _rings;
        private int _nextParticle;
        private int _nextRing;
        private int _seed;
        private readonly Vector3[] _unit = new Vector3[RingPoints];

        // The boss watched: its health, look and whether it is the Regent; the glow hung under its body.
        private Health _boss;
        private EnemyLookView _look;
        private bool _regent;
        private SpriteRenderer _glow;
        private Sprite _glowSource;
        private System.Action _onDied;
        // The tell and the kick, and whether the body carries either (and must be put back once).
        private bool _telling;
        private float _side = 1f;
        private bool _kicking;
        private bool _kickBody;
        private float _kickElapsed;
        private bool _bodyMoved;
        private bool _enraged;
        private float _burstFlash;
        private float _phaseClock;
        // The fall's end: when its ash or glints come, where, and whose.
        private bool _deathPending;
        private float _deathElapsed;
        private float _deathEndsAt;
        private Vector3 _deathFeet;
        private float _deathTop;
        private bool _deathRegent;

        // For tests and the capture.
        public bool IsTelling => _telling;
        public float TellProgress { get; private set; }
        public bool IsKicking => _kicking;
        public bool ShowsPhaseTwo => _enraged;
        public int PhaseTwoBursts { get; private set; }
        public int DeathsShown { get; private set; }
        public bool LastDeathShattered { get; private set; }
        public int DeathEndsShown { get; private set; }
        public float GlowAlpha => _glow != null && _glow.enabled ? _glow.color.a : 0f;
        internal SpriteRenderer Glow => _glow;
        internal Transform Root => _root;
        public int ShownParticles
        {
            get
            {
                int shown = 0;
                for (int i = 0; _particles != null && i < _particles.Length; i++)
                    if (_particles[i].Active)
                        shown++;
                return shown;
            }
        }
        public int ShownRings
        {
            get
            {
                int shown = 0;
                for (int i = 0; _rings != null && i < _rings.Length; i++)
                    if (_rings[i].Active)
                        shown++;
                return shown;
            }
        }

        internal void Initialize(WardenSlamService service)
        {
            if (_service != null)
                return;
            _service = service;
            _arena = FindFirstObjectByType<ArenaView>();
            _material = SharedSpriteMaterial.For(_arena);
            _root = new GameObject(RootName).transform;
            _root.SetParent(transform, false);
            for (int k = 0; k < RingPoints; k++)
            {
                float a = k * Mathf.PI * 2f / RingPoints;
                _unit[k] = new Vector3(Mathf.Cos(a), ArenaFloor.WorldY(Mathf.Sin(a)), 0f);
            }
            _particles = new Particle[ParticleSlots];
            for (int i = 0; i < _particles.Length; i++)
            {
                var sprite = new GameObject("Boss Mote " + i).AddComponent<SpriteRenderer>();
                sprite.transform.SetParent(_root, false);
                sprite.sharedMaterial = _material;
                sprite.sprite = FeelArt.Disc();
                // Over the combatants, as the hit sparks are (CombatEffectsView's order plus one).
                sprite.sortingOrder = 6;
                sprite.enabled = false;
                _particles[i] = new Particle { Renderer = sprite };
            }
            _rings = new Ring[RingSlots];
            for (int i = 0; i < _rings.Length; i++)
            {
                var line = new GameObject("Boss Ring " + i).AddComponent<LineRenderer>();
                line.transform.SetParent(_root, false);
                line.sharedMaterial = _material;
                line.useWorldSpace = true;
                line.loop = true;
                line.numCapVertices = 2;
                line.positionCount = RingPoints;
                // On the floor under the actors, as the Warden's mark is.
                line.sortingOrder = 0;
                line.enabled = false;
                _rings[i] = new Ring { Line = line, Points = new Vector3[RingPoints] };
            }
            _glow = MakeGlow();
            service.Marked += OnMarked;
            service.Slammed += OnSlammed;
        }

        private SpriteRenderer MakeGlow()
        {
            var glow = new GameObject("Boss Glow").AddComponent<SpriteRenderer>();
            glow.transform.SetParent(_root, false);
            glow.sharedMaterial = _material;
            glow.enabled = false;
            return glow;
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.Marked -= OnMarked;
                _service.Slammed -= OnSlammed;
            }
            // The scene is going: the glow is not hung back under a root that may be going first; it goes with the boss.
            Unbind(false);
        }

        // The mark is down: the tell begins on this very frame (the service's Update, before any look's).
        private void OnMarked()
        {
            if (_boss == null || !isActiveAndEnabled)
                return;
            _telling = true;
            _kicking = false;
            TellProgress = 0f;
            float dx = _service.MarkX - _boss.transform.position.x;
            if (Mathf.Abs(dx) > .05f)
                _side = dx > 0f ? 1f : -1f;
        }

        private void OnSlammed(bool hit)
        {
            if (_boss == null || !isActiveAndEnabled)
                return;
            // WardenSlamView has already brought the windup down (it heard the blow first): a body the tell raised kicks.
            _kickBody = _telling && _bodyMoved;
            _telling = false;
            TellProgress = 0f;
            _kicking = true;
            _kickElapsed = 0f;
        }

        // Right after the service has stepped the clock (its Update runs at -40, this one at -30): the tell's state is the
        // clock's of this very frame.
        private void Update()
        {
            if (_service == null)
                return;
            using (PerformanceMarkers.LateBossFeel.Auto())
                Step(Time.deltaTime);
        }

        // After every Update - the look's frame and flinch, CombatantView's flash and nudge - the body and the glow.
        private void LateUpdate()
        {
            if (_service == null)
                return;
            using (PerformanceMarkers.LateBossFeel.Auto())
            {
                float dt = Time.deltaTime;
                if (_boss != null)
                {
                    MoveBody();
                    ShowGlow();
                }
                TickDeath(dt);
                TickParticles(dt);
                TickRings(dt);
            }
        }

        private void Step(float dt)
        {
            Follow();
            if (_boss == null)
                return;
            bool marked = _service.IsFighting && _service.Clock.IsMarked;
            // A mark withdrawn without a blow (the fight stopped): the tell lets go.
            if (_telling && !marked)
                _telling = false;
            if (_telling)
                TellProgress = _service.Clock.TelegraphProgress;
            if (!_enraged && _service.IsEnraged)
                PhaseTwo();
            if (dt <= 0f)
                return;
            _phaseClock = (_phaseClock + dt) % PhaseTwoPeriod;
            if (_kicking)
            {
                _kickElapsed += dt;
                if (_kickElapsed >= KickSeconds)
                    _kicking = false;
            }
            if (_burstFlash > 0f)
                _burstFlash = Mathf.Max(0f, _burstFlash - dt);
        }

        // The gate's boss as the service reads it: bound as it walks out, let go as it falls or the fight changes.
        private void Follow()
        {
            Health boss = _service.Boss;
            if (ReferenceEquals(boss, _boss) || (boss == null && _boss == null))
                return;
            Unbind();
            if (boss == null)
                return;
            _boss = boss;
            _boss.TryGetComponent(out _look);
            _regent = _boss.TryGetComponent(out GlassRegent _);
            _enraged = false;
            _telling = false;
            _kicking = false;
            _burstFlash = 0f;
            _boss.Died += _onDied ??= OnBossDied;
            if (_glow == null)
                _glow = MakeGlow();
            if (_look != null && _look.Body != null)
            {
                SpriteRenderer body = _look.Body;
                _glow.transform.SetParent(body.transform, false);
                _glow.transform.localPosition = Vector3.zero;
                _glow.transform.localRotation = Quaternion.identity;
                _glow.transform.localScale = Vector3.one;
                _glow.sortingLayerID = body.sortingLayerID;
                // Over the body and its hit flash (CombatantView's, one over the body), inside the boss's group.
                _glow.sortingOrder = body.sortingOrder + 2;
            }
        }

        // Lets the boss go: its glow back under the root, hidden; the body put back if it still stands.
        private void Unbind(bool takeGlowBack = true)
        {
            if (!ReferenceEquals(_boss, null))
                _boss.Died -= _onDied;
            if (_bodyMoved && _look != null && _look.Body != null)
                PutBodyBack();
            _bodyMoved = false;
            _telling = false;
            _kicking = false;
            if (_glow != null)
            {
                _glow.enabled = false;
                if (takeGlowBack && _root != null)
                    _glow.transform.SetParent(_root, false);
            }
            _glowSource = null;
            _boss = null;
            _look = null;
        }

        // The tell's rise and lean back, the last quarter's tremble, or the blow's kick forward and down; over the look's
        // rest and its flinch. Only while the look holds its windup: a Regent at its feet raises nothing.
        private void MoveBody()
        {
            if (_look == null || _look.Body == null || !_boss.IsAlive)
                return;
            float rise = 0f, lean = 0f;
            bool moving = false;
            if (_telling && _look.IsWindingUp)
            {
                float told = GameFeel.TellEase(TellProgress);
                rise = (_regent ? RegentRise : WardenRise) * told;
                // Back, away from the mark: a mark to the right tips the top left (counter-clockwise).
                lean = _side * (_regent ? RegentLean : WardenLean) * told;
                if (TellProgress > TrembleFrom)
                    lean += TrembleDegrees * Mathf.Sin(Time.time * 70f) * (TellProgress - TrembleFrom) / (1f - TrembleFrom);
                moving = true;
            }
            else if (_kicking && _kickBody)
            {
                float left = 1f - _kickElapsed / KickSeconds;
                float k = left * left;
                rise = -KickDip * k;
                lean = -_side * KickLean * k;
                moving = true;
            }
            if (moving)
            {
                Transform body = _look.Body.transform;
                body.localPosition = _look.BodyRest + new Vector3(0f, rise, 0f);
                body.localRotation = Quaternion.Euler(0f, 0f, lean + _look.FlinchTip);
                _bodyMoved = true;
            }
            else if (_bodyMoved)
                PutBodyBack();
        }

        private void PutBodyBack()
        {
            Transform body = _look.Body.transform;
            body.localPosition = _look.BodyRest;
            float tip = _look.FlinchTip;
            body.localRotation = tip != 0f ? Quaternion.Euler(0f, 0f, tip) : Quaternion.identity;
            _bodyMoved = false;
        }

        // The glow over the body: the tell's (beating with the mark's edge), the blow's and phase two's flash, phase two's
        // slow beat - the strongest of them - in the boss's colour, hotter from phase two.
        private void ShowGlow()
        {
            if (_glow == null || _look == null || _look.Body == null)
                return;
            float strength = 0f;
            if (_telling)
            {
                float pulse = .5f + .5f * Mathf.Sin(Time.time * Mathf.Lerp(9f, 24f, TellProgress));
                strength = GameFeel.TellGlow(TellProgress, pulse);
            }
            if (_kicking)
                strength = Mathf.Max(strength, KickGlow * (1f - _kickElapsed / KickSeconds));
            if (_burstFlash > 0f)
                strength = Mathf.Max(strength, BurstFlash * _burstFlash / BurstFlashSeconds);
            if (_enraged)
            {
                float beat = .5f - .5f * Mathf.Cos(2f * Mathf.PI * _phaseClock / PhaseTwoPeriod);
                strength = Mathf.Max(strength, Mathf.Lerp(PhaseTwoLow, PhaseTwoHigh, beat));
            }
            SpriteRenderer body = _look.Body;
            bool shown = strength > 0f && body.enabled && body.gameObject.activeInHierarchy && body.sprite != null;
            if (shown && !ReferenceEquals(body.sprite, _glowSource))
            {
                Sprite silhouette = _look.SilhouetteOf(body.sprite);
                _glow.sprite = silhouette;
                _glowSource = body.sprite;
            }
            shown &= _glow.sprite != null;
            if (shown)
            {
                Color colour = _enraged ? (_regent ? RegentHot : WardenHot) : (_regent ? RegentGlow : WardenGlow);
                colour.a = Mathf.Min(GameFeel.TellGlowPeak + .2f, strength);
                _glow.color = colour;
                // The silhouette mirrors with the body it lies on (the body's own scale carries the mirror).
                _glow.flipX = body.flipX;
            }
            if (_glow.enabled != shown)
                _glow.enabled = shown;
        }

        // Phase two: a burst over the floor and from the body, then the hotter slow glow.
        private void PhaseTwo()
        {
            _enraged = true;
            PhaseTwoBursts++;
            _burstFlash = BurstFlashSeconds;
            Vector3 feet = Feet(_boss.transform.position);
            float top = _look != null && _look.PaintedArt != null ? _look.PaintedArt.VisualTop : 1.6f;
            Color hot = _regent ? RegentHot : WardenHot;
            ArmRing(feet, 0f, .5f, .3f, 2.4f, .16f, .02f, Fade(hot, .9f));
            ArmRing(feet, .08f, .45f, .2f, 1.6f, .12f, .02f, Fade(_regent ? Glass : Ember, .8f));
            _seed++;
            for (int i = 0; i < 12; i++)
            {
                float n = PixelNoise.Value(i, _seed, 83);
                float a = (i + .5f + (n - .5f) * .5f) * Mathf.PI * 2f / 12f;
                Vector3 from = feet + new Vector3(Mathf.Cos(a) * .3f, top * (.35f + .3f * n), 0f);
                var velocity = new Vector3(Mathf.Cos(a) * (1.2f + n), 1.4f + 1.2f * n, 0f);
                ArmParticle(from, velocity, 4f, .55f + .25f * n, .05f, .05f, false, 0f, _regent ? GlassViolet : Ember, 0f);
            }
        }

        private void OnBossDied()
        {
            if (_boss == null)
                return;
            DeathsShown++;
            Vector3 feet = Feet(_boss.transform.position);
            EnemyArtSet art = _look != null ? _look.PaintedArt : null;
            float top = art != null ? art.VisualTop : 1.6f;
            _deathRegent = _regent;
            LastDeathShattered = _regent;
            _deathFeet = feet;
            _deathTop = top;
            _deathPending = true;
            _deathElapsed = 0f;
            // The painted fall's own length, less a beat, so its last drawing dissolves as it fades.
            _deathEndsAt = art != null && art.HasFall ? Mathf.Max(.3f, art.FallSeconds - .2f) : .3f;
            _seed++;
            if (_regent)
            {
                // The shatter: glass shards flung out from the body, turning, falling; a white ring at its feet.
                ArmRing(feet, 0f, .45f, .3f, 2.2f, .14f, .02f, Fade(Glass, .95f));
                for (int i = 0; i < 16; i++)
                {
                    float n = PixelNoise.Value(i, _seed, 89);
                    float m = PixelNoise.Value(i, _seed, 97);
                    float a = (i + .5f + (n - .5f) * .6f) * Mathf.PI * 2f / 16f;
                    Vector3 from = feet + new Vector3(Mathf.Cos(a) * .25f, top * (.25f + .5f * m), 0f);
                    var velocity = new Vector3(Mathf.Cos(a) * (1.6f + 1.4f * n), .8f + 2.2f * m, 0f);
                    ArmParticle(from, velocity, 6f, .6f + .35f * n, .04f, .14f + .08f * m, true, (n - .5f) * 900f,
                        i % 3 == 0 ? GlassViolet : Glass, 0f);
                }
            }
            else
            {
                // The Warden's fall opens with a ring and a spray of embers off its body.
                ArmRing(feet, 0f, .6f, .4f, 3f, .18f, .02f, Fade(WardenHot, .9f));
                for (int i = 0; i < 18; i++)
                {
                    float n = PixelNoise.Value(i, _seed, 101);
                    float m = PixelNoise.Value(i, _seed, 103);
                    float a = (i + .5f) * Mathf.PI / 18f;
                    Vector3 from = feet + new Vector3((n - .5f) * .7f, top * (.2f + .6f * m), 0f);
                    var velocity = new Vector3(Mathf.Cos(a) * (1f + 1.2f * n), .9f + 1.6f * Mathf.Sin(a) * (.6f + .4f * m), 0f);
                    ArmParticle(from, velocity, 4.5f, .5f + .4f * m, .045f + .03f * n, .045f + .03f * n, false, 0f,
                        i % 2 == 0 ? Ember : Fade(WardenHot, .9f), 0f);
                }
            }
            Unbind();
        }

        // The fall's end: the Warden's remains dissolve in ash, the Regent's in glints.
        private void TickDeath(float dt)
        {
            if (!_deathPending || dt <= 0f)
                return;
            _deathElapsed += dt;
            if (_deathElapsed < _deathEndsAt)
                return;
            _deathPending = false;
            DeathEndsShown++;
            _seed++;
            int count = _deathRegent ? 10 : 16;
            for (int i = 0; i < count; i++)
            {
                float n = PixelNoise.Value(i, _seed, 107);
                float m = PixelNoise.Value(i, _seed, 109);
                // Along the remains lying on the floor, about the body's width either side of its feet.
                Vector3 from = _deathFeet + new Vector3((n - .5f) * 1.4f, .05f + .3f * m, 0f);
                var velocity = new Vector3((m - .5f) * .3f, .45f + .5f * n, 0f);
                if (_deathRegent)
                    ArmParticle(from, velocity, 0f, .5f + .3f * m, .03f + .02f * n, .03f + .02f * n, false, 0f, Glass,
                        (i % 4) * .05f);
                else
                    ArmParticle(from, velocity, -.2f, .7f + .4f * m, .045f + .03f * n, .045f + .03f * n, false, 0f,
                        i % 3 == 0 ? Ember : Ash, (i % 4) * .04f);
            }
            if (!_deathRegent)
                ArmRing(_deathFeet, 0f, .5f, .5f, 1.4f, .1f, .02f, Fade(Ember, .5f));
        }

        private Vector3 Feet(Vector3 p)
        {
            if (_arena != null && _arena.IsFoundryRegion)
                p.y += FoundrySurface.Lift(p, _arena.transform.position);
            p.z = 0f;
            return p;
        }

        private static Color Fade(Color colour, float alpha)
        {
            colour.a = alpha;
            return colour;
        }

        private void ArmParticle(Vector3 origin, Vector3 velocity, float gravity, float lifetime, float width, float length,
            bool shard, float spin, Color colour, float delay)
        {
            Particle p = _particles[_nextParticle];
            _nextParticle = (_nextParticle + 1) % _particles.Length;
            p.Active = true;
            p.Delay = delay;
            p.Elapsed = 0f;
            p.Lifetime = lifetime;
            p.Origin = origin;
            p.Velocity = velocity;
            p.Gravity = gravity;
            p.Width = width;
            p.Length = length;
            p.Shard = shard;
            p.Spin = spin;
            p.Colour = colour;
            DrawParticle(p);
        }

        // Flying under its gravity (negative rises), thinning to nothing; a shard turned along its flight and spinning.
        private static void DrawParticle(Particle p)
        {
            if (p.Elapsed < p.Delay)
            {
                p.Renderer.enabled = false;
                return;
            }
            float t = p.Elapsed - p.Delay;
            float u = Mathf.Clamp01(t / p.Lifetime);
            Vector3 at = p.Origin + p.Velocity * t;
            at.y -= .5f * p.Gravity * t * t;
            Transform transform = p.Renderer.transform;
            transform.position = at;
            float shrink = 1f - .5f * u;
            transform.localScale = new Vector3(p.Width * shrink, p.Length * shrink, 1f);
            if (p.Shard)
            {
                float rising = p.Velocity.y - p.Gravity * t;
                float heading = Mathf.Atan2(rising, p.Velocity.x) * Mathf.Rad2Deg - 90f;
                transform.rotation = Quaternion.Euler(0f, 0f, heading + p.Spin * t);
            }
            else if (transform.rotation != Quaternion.identity)
                transform.rotation = Quaternion.identity;
            Color colour = p.Colour;
            colour.a *= Mathf.Min(1f, u * 10f) * (1f - u);
            p.Renderer.color = colour;
            p.Renderer.enabled = true;
        }

        private void TickParticles(float dt)
        {
            if (dt <= 0f)
                return;
            for (int i = 0; i < _particles.Length; i++)
            {
                Particle p = _particles[i];
                if (!p.Active)
                    continue;
                p.Elapsed += dt;
                if (p.Elapsed >= p.Delay + p.Lifetime)
                {
                    p.Active = false;
                    p.Renderer.enabled = false;
                    continue;
                }
                DrawParticle(p);
            }
        }

        private void ArmRing(Vector3 centre, float delay, float lifetime, float from, float to, float fromWidth, float toWidth,
            Color colour)
        {
            Ring ring = _rings[_nextRing];
            _nextRing = (_nextRing + 1) % _rings.Length;
            ring.Active = true;
            ring.Delay = delay;
            ring.Elapsed = 0f;
            ring.Lifetime = lifetime;
            ring.Centre = centre;
            ring.From = from;
            ring.To = to;
            ring.FromWidth = fromWidth;
            ring.ToWidth = toWidth;
            ring.Colour = colour;
            DrawRing(ring);
        }

        // A floor ring spreading from its centre as it thins and fades.
        private void DrawRing(Ring ring)
        {
            if (ring.Elapsed < ring.Delay)
            {
                ring.Line.enabled = false;
                return;
            }
            float u = Mathf.Clamp01((ring.Elapsed - ring.Delay) / ring.Lifetime);
            float eased = 1f - (1f - u) * (1f - u);
            float radius = Mathf.Lerp(ring.From, ring.To, eased);
            for (int k = 0; k < RingPoints; k++)
                ring.Points[k] = ring.Centre + _unit[k] * radius;
            ring.Line.SetPositions(ring.Points);
            float width = Mathf.Lerp(ring.FromWidth, ring.ToWidth, eased);
            ring.Line.startWidth = ring.Line.endWidth = width;
            Color colour = ring.Colour;
            colour.a *= 1f - u;
            ring.Line.startColor = ring.Line.endColor = colour;
            ring.Line.enabled = true;
        }

        private void TickRings(float dt)
        {
            if (dt <= 0f)
                return;
            for (int i = 0; i < _rings.Length; i++)
            {
                Ring ring = _rings[i];
                if (!ring.Active)
                    continue;
                ring.Elapsed += dt;
                if (ring.Elapsed >= ring.Delay + ring.Lifetime)
                {
                    ring.Active = false;
                    ring.Line.enabled = false;
                    continue;
                }
                DrawRing(ring);
            }
        }
    }
}
