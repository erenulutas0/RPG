using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only - the game-feel pass on the hero (docs/43 section 1, 2026-11-22). The body swings with the first
    // weapon alone (AttackController.Attacked; docs/42, risks), so a weapon fired from slot 1 or 2 shows a small cue of its
    // own at the moment it strikes (AttackController.StruckWith): the Sword an off-hand flourish - a ghost of the blade
    // sweeping from behind the off shoulder over the top toward the struck enemy, a steel arc trailing it; the Staff a
    // raised glow - a ghost staff lifted at the off side, a glow swelling at its tip and a thin ring spreading from it; the
    // Daggers a quick double flick - two ghost blades flicking out at the waist one after the other, each with its streak.
    // Which cue a weapon plays is GameFeel.CueFor its strike behaviour, as CombatEffectsView picks its strike effect; its
    // length GameFeel.CueSeconds, inside the weapon's interval.
    //
    // A cue never hides the hero or an enemy: its ghosts draw behind the whole figure, inside the hero's sorting group, so
    // only what reaches past the silhouette shows; its trails and glows draw over it but thin and translucent, at the
    // off shoulder, the staff's tip or the waist - never the face - and every cue is gone within a fifth of a second or
    // so. The ghosts are the weapons' existing sprites (the puppet's painted ones, else the painted frames', else the
    // loadout's own), no new frame; the glow is FeelArt's code-drawn disc.
    //
    // Everything is made once, under one child of the hero (RootName), one rig a cue, and only moved, recoloured and shown
    // after that: no allocation per strike or per frame. Being a child of the hero it moves with it, sorts inside its
    // group and stands at its depth on the foundry (FoundryVisibilityView moves an actor's children). Scaled time: a pause
    // holds a cue as it holds the strike. It reads the attacker and writes nothing to it.
    //
    // The hero's reactions (stage 2), over what its look already plays - the puppet's flinch, stride, idle and collapse,
    // Ilya's rendered hit, walk, idle and death frames, the painted frames' swing, bob and breath, and CombatantView's flash,
    // all kept as they are:
    // - a blow squashes the figure away from it and shoves it a few texels back (GameFeel.Squash, HitEnvelope), on the
    //   puppet's rig roots and its root's place, on the rendered drawing (RenderedCharacterAnimator.Squash) or on the
    //   painted frames' weight transform; never on the hero, its shadow or its weapons' loadouts;
    // - death lets the collapse play while a thin stream of ash rises off the falling body, then the body bursts into ash
    //   and embers along where it lies, over a fading ember glow;
    // - a level stretches the figure up and settles it (GameFeel.PopEnvelope) over a gold halo behind it - RunFeedbackView's
    //   ring and sparks at the feet stay the level's burst;
    // - a weapon picked up rises over the hero's head as a ghost of itself, turning once over a glow, and the figure pops -
    //   on scaled time, so a pick made under the offer's pause plays as the run moves again.
    // The motes, halos and the picked weapon's ghost are pooled with the cues and made with them; nothing is made after.
    [DisallowMultipleComponent]
    public sealed class HeroFeelView : MonoBehaviour
    {
        public const string RootName = "Hero Feel";
        // Inside the hero's sorting group: ghosts behind every part the puppet paints (its parts sort -20 to +11 about the
        // body, its held items a little past), trails and glows over every part.
        internal const int BehindFigure = 40;
        internal const int OverFigure = 40;
        private const int TrailPoints = 9;
        private const int StreakPoints = 5;
        private const int RingPoints = 18;

        // The flourish: its pivot behind the off shoulder (hero-local, facing right), the trail's share of the sweep behind
        // the blade, the ghost's and the trail's strength at full.
        private const float FlourishPivotX = -.1f;
        private const float FlourishPivotY = .8f;
        private const float FlourishTrailLag = .35f;
        private const float FlourishGhostAlpha = .8f;
        private const float FlourishTrailAlpha = .6f;
        private const float FlourishTrailWidth = .05f;
        // The pulse: the staff stands at the off side, raised this far and tilted out; its tip's glow swells from and to
        // these radii, its ring spreads from and to these.
        private const float PulseX = -.3f;
        private const float PulseY = .22f;
        private const float PulseRaise = .14f;
        private const float PulseTilt = 12f;
        private const float PulseGhostAlpha = .75f;
        private const float PulseGlowFrom = .07f;
        private const float PulseGlowTo = .27f;
        private const float PulseGlowAlpha = .9f;
        private const float PulseRingFrom = .05f;
        private const float PulseRingTo = .34f;
        private const float PulseRingAlpha = .7f;
        private const float PulseRingWidth = .03f;
        // The flick: two blades at the waist on the struck side, the second a little higher and StaggerShare of the cue
        // later, each flicking over FlickShare of it.
        private const float FlickX = .17f;
        private const float FlickY = .42f;
        private const float FlickRise = .12f;
        private const float FlickStagger = .38f;
        private const float FlickShare = .62f;
        private const float FlickGhostAlpha = .85f;
        private const float FlickStreakAlpha = .6f;
        private const float FlickStreakWidth = .04f;
        // A ghost's reach from its grip, as its sprite is drawn, held to a sensible length.
        private const float MinimumReach = .25f;
        private const float MaximumReach = .8f;

        // The Staff's own violet (StaffSplashArt's arcs), the Sword's and the Daggers' steel.
        private static readonly Color Steel = new Color(.9f, .95f, 1f, 1f);
        private static readonly Color StaffGlow = new Color(.91f, .79f, 1f, 1f);
        private static readonly Color StaffRing = new Color(.62f, .45f, .98f, 1f);

        private sealed class Cue
        {
            public SlotCue Kind;
            public SpriteRenderer[] Sprites;
            public LineRenderer[] Lines;
            public Vector3[][] Points;
            public bool Active;
            public float Elapsed;
            public float Seconds;
            public float Side;
            public float Reach;
            public int Shown;
        }

        // Death: the collapse's ash stream every StreamEvery until AshDelay (the puppet's topple is nearly down at 0.8 s of
        // its 1 s, PuppetGait.DeathSeconds; Ilya's death frames likewise), then the burst along the body. The painted
        // frames have no collapse, so their burst is at once where the hero stood.
        internal const float AshDelay = .8f;
        private const float StreamEvery = .09f;
        private const int BurstMotes = 14;
        internal const int MoteSlots = 24;
        private const float DeathGlowSeconds = .45f;
        // The level's halo behind the figure and the picked weapon's rise over the head.
        private const float LevelGlowSeconds = .4f;
        internal const float PickupSeconds = .7f;
        private const float PickupFrom = .95f;
        private const float PickupRise = .7f;
        private static readonly Color Ash = new Color(.36f, .34f, .34f, .9f);
        private static readonly Color Ember = new Color(1f, .55f, .22f, .95f);
        private static readonly Color LevelGold = new Color(1f, .82f, .35f, 1f);

        private sealed class Mote
        {
            public SpriteRenderer Renderer;
            public bool Active;
            public float Elapsed;
            public float Lifetime;
            public Vector3 Origin;
            public Vector3 Velocity;
            public float Size;
            public Color Colour;
        }

        // A glow disc that swells and fades once: the level's halo, the death's ember glow, the pick's.
        private sealed class Glow
        {
            public SpriteRenderer Renderer;
            public bool Active;
            public float Elapsed;
            public float Lifetime;
            public float FromRadius;
            public float ToRadius;
            public Color Colour;
        }

        private AttackController _attack;
        private HeroLookView _look;
        private Health _health;
        private RunState _run;
        private UpgradeService _upgrades;
        private Transform _root;
        private Material _material;
        private Cue[] _cues;
        private float _side = 1f;
        private bool _subscribed;
        private System.Action<Health, WeaponRuntime> _onStruckWith;
        private System.Action<DamageContext> _onDamaged;
        private System.Action _onDied;
        private System.Action _onLevelChanged;
        private System.Action<WeaponCard, WeaponRuntime> _onWeaponAcquired;

        // The figure the reactions move: whichever draws the hero - the puppet, the rendered drawing or the painted
        // frames' weight transform - found when a reaction starts.
        private PuppetAnimator _puppet;
        private RenderedCharacterAnimator _drawn;
        private Transform _pose;
        private bool _hitting;
        private float _hitElapsed;
        private float _awayX;
        private float _awayY;
        private float _shove;
        private bool _popping;
        private float _popElapsed;
        // A squash is written on the figure (and must be taken off once it ends); this frame's, for the painted frames'
        // weight, which its animator rewrites every frame before LateUpdate.
        private bool _felt;
        private float _scaleX = 1f;
        private float _scaleY = 1f;
        private Vector3 _offset;

        private Mote[] _motes;
        private int _nextMote;
        private int _moteSeed;
        private bool _dying;
        private float _deathElapsed;
        private float _ashDelay;
        private float _nextStream;
        private bool _burst;
        private Glow _halo;
        private Glow _pickGlow;
        private SpriteRenderer _pickGhost;
        private bool _picking;
        private bool _pickStarted;
        private float _pickElapsed;

        // The child every cue lives under, for tests and the perf census.
        internal Transform Root => _root;
        // The reactions, for tests and the capture: the blow's recoil (0 to 1) and the level's pop now, whether the death's
        // ash is under way and has burst, the motes on show, and the picked weapon's rise.
        public float Recoil => _hitting ? GameFeel.HitEnvelope(_hitElapsed / GameFeel.HitSeconds) : 0f;
        public float Pop => _popping ? GameFeel.PopEnvelope(_popElapsed / GameFeel.PopSeconds) : 0f;
        public bool IsDissolving => _dying;
        public bool HasBurstIntoAsh => _burst;
        public int ShownMotes
        {
            get
            {
                int shown = 0;
                for (int i = 0; _motes != null && i < _motes.Length; i++)
                    if (_motes[i].Active)
                        shown++;
                return shown;
            }
        }
        public bool IsPickupShowing => _picking;
        public int PickupsShown { get; private set; }
        public int LevelPopsShown { get; private set; }
        // The scale and offset the reactions put on the figure this frame ((1, 1) and zero at rest).
        public Vector2 FigureScale => new Vector2(_scaleX, _scaleY);
        public Vector3 FigureOffset => _offset;
        internal SpriteRenderer PickupGhost => _pickGhost;
        public bool IsCueShowing(SlotCue cue) => _cues != null && _cues[(int)cue].Active;
        // How many times each cue has played this scene.
        public int CuesShown(SlotCue cue) => _cues != null ? _cues[(int)cue].Shown : 0;
        // A cue's renderers, for tests: its ghosts and glows, then its trails.
        internal SpriteRenderer[] CueSprites(SlotCue cue) => _cues[(int)cue].Sprites;
        internal LineRenderer[] CueLines(SlotCue cue) => _cues[(int)cue].Lines;

        // attack drives the slot cues; health, run and upgrades the reactions (each may be null: that reaction never plays).
        internal void Initialize(AttackController attack, Health health = null, RunState run = null,
            UpgradeService upgrades = null)
        {
            if (_root != null)
                return;
            _attack = attack;
            _health = health;
            _run = run;
            _upgrades = upgrades;
            _look = GetComponent<HeroLookView>();
            _material = SharedSpriteMaterial.For(FindFirstObjectByType<ArenaView>());
            _root = new GameObject(RootName).transform;
            _root.SetParent(transform, false);
            SpriteRenderer body = _look != null ? _look.Body : null;
            int layer = body != null ? body.sortingLayerID : 0;
            int order = body != null ? body.sortingOrder : 0;
            _cues = new Cue[3];
            _cues[(int)SlotCue.Flourish] = Build(SlotCue.Flourish, 1, 1, TrailPoints, layer, order);
            _cues[(int)SlotCue.Pulse] = Build(SlotCue.Pulse, 2, 1, RingPoints, layer, order);
            _cues[(int)SlotCue.Flick] = Build(SlotCue.Flick, 2, 2, StreakPoints, layer, order);
            Line(SlotCue.Pulse, 0).loop = true;
            // The glow is the disc, over the figure.
            SpriteRenderer glow = _cues[(int)SlotCue.Pulse].Sprites[1];
            glow.sprite = FeelArt.Disc();
            glow.sortingOrder = order + OverFigure;

            // The reactions' motes over the figure, the halos behind it, the picked weapon over it.
            _motes = new Mote[MoteSlots];
            for (int i = 0; i < _motes.Length; i++)
                _motes[i] = new Mote { Renderer = Disc("Ash Mote " + i, layer, order + OverFigure) };
            _halo = new Glow { Renderer = Disc("Hero Halo", layer, order - BehindFigure - 1) };
            _pickGlow = new Glow { Renderer = Disc("Pickup Glow", layer, order + OverFigure - 1) };
            _pickGhost = Disc("Pickup Ghost", layer, order + OverFigure);
            _pickGhost.sprite = null;
            Subscribe();
        }

        private SpriteRenderer Disc(string name, int layer, int order)
        {
            var sprite = new GameObject(name).AddComponent<SpriteRenderer>();
            sprite.transform.SetParent(_root, false);
            sprite.sharedMaterial = _material;
            sprite.sprite = FeelArt.Disc();
            sprite.sortingLayerID = layer;
            sprite.sortingOrder = order;
            sprite.enabled = false;
            return sprite;
        }

        private LineRenderer Line(SlotCue cue, int index) => _cues[(int)cue].Lines[index];

        private Cue Build(SlotCue kind, int sprites, int lines, int points, int layer, int order)
        {
            var cue = new Cue
            {
                Kind = kind,
                Sprites = new SpriteRenderer[sprites],
                Lines = new LineRenderer[lines],
                Points = new Vector3[lines][]
            };
            for (int i = 0; i < sprites; i++)
            {
                var sprite = new GameObject(kind + " Ghost " + i).AddComponent<SpriteRenderer>();
                sprite.transform.SetParent(_root, false);
                sprite.sharedMaterial = _material;
                sprite.sortingLayerID = layer;
                sprite.sortingOrder = order - BehindFigure;
                sprite.enabled = false;
                cue.Sprites[i] = sprite;
            }
            for (int i = 0; i < lines; i++)
            {
                var line = new GameObject(kind + " Trail " + i).AddComponent<LineRenderer>();
                line.transform.SetParent(_root, false);
                line.sharedMaterial = _material;
                line.useWorldSpace = false;
                line.numCapVertices = 2;
                line.positionCount = points;
                line.sortingLayerID = layer;
                line.sortingOrder = order + OverFigure;
                line.enabled = false;
                cue.Lines[i] = line;
                cue.Points[i] = new Vector3[points];
            }
            return cue;
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            Unsubscribe();
            if (_cues == null)
                return;
            for (int i = 0; i < _cues.Length; i++)
                End(_cues[i]);
            EndReactions();
        }

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || _attack == null)
                return;
            _attack.StruckWith += _onStruckWith ??= OnStruckWith;
            if (_health != null)
            {
                _health.Damaged += _onDamaged ??= OnDamaged;
                _health.Died += _onDied ??= OnDied;
            }
            if (_run != null)
                _run.LevelChanged += _onLevelChanged ??= OnLevelChanged;
            if (_upgrades != null)
                _upgrades.WeaponAcquired += _onWeaponAcquired ??= OnWeaponAcquired;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;
            if (_attack != null)
                _attack.StruckWith -= _onStruckWith;
            if (_health != null)
            {
                _health.Damaged -= _onDamaged;
                _health.Died -= _onDied;
            }
            if (_run != null)
                _run.LevelChanged -= _onLevelChanged;
            if (_upgrades != null)
                _upgrades.WeaponAcquired -= _onWeaponAcquired;
            _subscribed = false;
        }

        // The first weapon's strike is the body's swing: only the weapons after it cue.
        private void OnStruckWith(Health target, WeaponRuntime weapon)
        {
            if (weapon == null || ReferenceEquals(weapon, _attack.Weapon) || !isActiveAndEnabled)
                return;
            Play(GameFeel.CueFor(weapon.Pattern.Behavior), target, weapon.Interval);
        }

        // Starts (or restarts) a cue toward the struck target. Internal for the capture tool, which plays each cue on a
        // standing hero.
        internal void Play(SlotCue kind, Health target, float interval)
        {
            if (_cues == null)
                return;
            if (target != null)
            {
                float dx = target.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > .05f)
                    _side = dx > 0f ? 1f : -1f;
            }
            Cue cue = _cues[(int)kind];
            cue.Active = true;
            cue.Elapsed = 0f;
            cue.Seconds = GameFeel.CueSeconds(kind, interval);
            cue.Side = _side;
            cue.Shown++;
            Sprite ghost = GhostOf(kind);
            cue.Reach = ghost != null ? Mathf.Clamp(ghost.bounds.max.y, MinimumReach, MaximumReach) : .45f;
            int ghosts = kind == SlotCue.Pulse ? 1 : cue.Sprites.Length;
            for (int i = 0; i < ghosts; i++)
            {
                cue.Sprites[i].sprite = ghost;
                cue.Sprites[i].flipX = _side < 0f;
            }
            Draw(cue);
        }

        private Sprite GhostOf(SlotCue kind)
        {
            if (_look == null)
                return null;
            HeroWeaponPart part = kind == SlotCue.Flourish ? HeroWeaponPart.Sword
                : kind == SlotCue.Pulse ? HeroWeaponPart.StaffShaft
                : HeroWeaponPart.DaggerRight;
            return _look.WeaponSprite(part);
        }

        // Before every look's LateUpdate: this frame's squash and shove are on the puppet's rig and the rendered drawing
        // before the puppet places its weapons on its hands and the drawing is shown.
        private void Update()
        {
            if (_cues == null)
                return;
            using (PerformanceMarkers.LateHeroFeel.Auto())
                Advance(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (_cues == null)
                return;
            using (PerformanceMarkers.LateHeroFeel.Auto())
            {
                float dt = Time.deltaTime;
                WeighPaintedFrames(dt);
                Tick(dt);
                TickDeath(dt);
                TickMotes(dt);
                TickGlow(_halo, dt);
                TickGlow(_pickGlow, dt);
                TickPickup(dt);
            }
        }

        // ---- The hero's reactions (stage 2) ----

        private void OnDamaged(DamageContext context)
        {
            if (!isActiveAndEnabled || _dying || _health == null || !_health.IsAlive)
                return;
            // Away from whoever dealt it, along the floor. A blow with no one behind it squashes without a shove.
            _awayX = 0f;
            _awayY = 1f;
            _shove = 0f;
            if (context.Source is Component source && source != null)
            {
                Vector3 from = source.transform.position;
                Vector3 at = transform.position;
                float dx = at.x - from.x;
                float dy = ArenaFloor.FloorY(at.y) - ArenaFloor.FloorY(from.y);
                float length = Mathf.Sqrt(dx * dx + dy * dy);
                if (length > 1e-4f)
                {
                    _awayX = dx / length;
                    _awayY = dy / length;
                    _shove = 1f;
                }
            }
            FindFigure();
            // A blow on a figure still recoiling carries on from where the recoil stands, back up its rise to the peak,
            // rather than snapping to rest for a frame: a horde's blows a tenth of a second apart hold the recoil.
            _hitElapsed = _hitting ? GameFeel.HitAttack * GameFeel.HitSeconds * Recoil : 0f;
            _hitting = true;
            ApplyFigure();
        }

        private void OnDied()
        {
            if (_dying)
                return;
            FindFigure();
            _dying = true;
            _deathElapsed = 0f;
            _nextStream = 0f;
            _burst = false;
            // The collapse plays on the figure's own shape; the painted frames have none, so they burst at once.
            _hitting = false;
            _popping = false;
            ApplyFigure();
            _ashDelay = _puppet != null || _drawn != null ? AshDelay : 0f;
            for (int i = 0; i < _cues.Length; i++)
                End(_cues[i]);
            EndPickup();
        }

        private void OnLevelChanged()
        {
            if (!isActiveAndEnabled || _dying || _cues == null)
                return;
            LevelPopsShown++;
            StartPop();
            Color halo = LevelGold;
            halo.a = .5f;
            ArmGlow(_halo, new Vector3(0f, .65f, 0f), LevelGlowSeconds, .35f, .8f, halo);
        }

        // Shown once the run moves: a pick is made under the offer's pause.
        private void OnWeaponAcquired(WeaponCard card, WeaponRuntime runtime)
        {
            if (!isActiveAndEnabled || _dying || runtime == null || _cues == null)
                return;
            PickupsShown++;
            _picking = true;
            _pickStarted = false;
            _pickElapsed = 0f;
            _pickGhost.sprite = GhostOf(GameFeel.CueFor(runtime.Pattern.Behavior));
            _pickGhost.enabled = false;
        }

        private void StartPop()
        {
            FindFigure();
            // A pop under way (a level, then a pick) rises again from where it stands, as a blow does.
            float asin = Mathf.Asin(Mathf.Clamp01(Pop)) / Mathf.PI;
            _popElapsed = _popping ? asin * asin * GameFeel.PopSeconds : 0f;
            _popping = true;
            ApplyFigure();
        }

        // Whichever draws the hero now: a test may swap the drawing, so it is found again as each reaction starts.
        private void FindFigure()
        {
            if (_puppet == null)
                TryGetComponent(out _puppet);
            if (_drawn == null)
                TryGetComponent(out _drawn);
            _pose = null;
            if (_puppet == null && _drawn == null && _look != null && _look.UsesPaintedArt && _look.Body != null)
            {
                // The painted frames' weight transform sits between the hero and its body (VanguardAnimator.PoseName).
                Transform parent = _look.Body.transform.parent;
                if (parent != null && parent != transform)
                    _pose = parent;
            }
        }

        private void Advance(float dt)
        {
            if (dt <= 0f)
                return;
            if (_hitting)
            {
                _hitElapsed += dt;
                if (_hitElapsed >= GameFeel.HitSeconds)
                    _hitting = false;
            }
            if (_popping)
            {
                _popElapsed += dt;
                if (_popElapsed >= GameFeel.PopSeconds)
                    _popping = false;
            }
            ApplyFigure();
        }

        // This frame's squash and shove from the blow and the pop, written on the figure while either plays and once more,
        // at rest, as the last ends.
        private void ApplyFigure()
        {
            float hit = Recoil;
            float pop = Pop;
            GameFeel.Squash(hit, _awayX, _awayY, out float scaleX, out float scaleY);
            scaleX *= 1f - .5f * GameFeel.PopStretch * pop;
            scaleY *= 1f + GameFeel.PopStretch * pop;
            float shove = GameFeel.ShoveDistance * hit * _shove;
            _scaleX = scaleX;
            _scaleY = scaleY;
            _offset = new Vector3(_awayX * shove, ArenaFloor.WorldY(_awayY * shove), 0f);
            bool active = hit > 0f || pop > 0f;
            if (!active && !_felt)
                return;
            WriteFigure(scaleX, scaleY, _offset);
            _felt = active;
        }

        // The puppet squashes on its rigs' roots (at the feet; their mirror kept) and is shoved on its own root's place,
        // which nothing else moves; the rendered drawing squashes through its mirror and is shoved on its root's place. The
        // painted frames' weight is rewritten every frame by its animator, so LateUpdate weighs it (WeighPaintedFrames).
        private void WriteFigure(float scaleX, float scaleY, Vector3 offset)
        {
            if (_puppet != null)
            {
                SquashRig(_puppet.RearRig, scaleX, scaleY);
                SquashRig(_puppet.FrontRig, scaleX, scaleY);
                if (_puppet.Root != null)
                    _puppet.Root.localPosition = offset;
            }
            if (_drawn != null)
            {
                _drawn.Squash = new Vector2(scaleX, scaleY);
                if (_drawn.Root != null)
                    _drawn.Root.localPosition = offset;
            }
        }

        private static void SquashRig(PuppetRig rig, float scaleX, float scaleY)
        {
            if (rig == null || rig.Root == null)
                return;
            Transform root = rig.Root.transform;
            root.localScale = new Vector3(root.localScale.x < 0f ? -scaleX : scaleX, scaleY, 1f);
        }

        // After the painted frames' animator has written this frame's weight (HeroLookView's Update): the reactions on top.
        // Only on a frame it wrote, so a pause never compounds them.
        private void WeighPaintedFrames(float dt)
        {
            // Only while its animator draws (HeroLookView ticks it), so a squash never compounds on a weight nobody rewrote.
            if (_pose == null || dt <= 0f || !_felt || _look == null || !_look.isActiveAndEnabled)
                return;
            _pose.localScale = Vector3.Scale(_pose.localScale, new Vector3(_scaleX, _scaleY, 1f));
            _pose.localPosition += _offset;
        }

        // The collapse's ash: a thin stream off the falling body, then the burst along where it lies.
        private void TickDeath(float dt)
        {
            if (!_dying || _burst || dt <= 0f)
                return;
            _deathElapsed += dt;
            while (_nextStream < _ashDelay && _deathElapsed >= _nextStream)
            {
                StreamMote();
                _nextStream += StreamEvery;
            }
            if (_deathElapsed < _ashDelay)
                return;
            _burst = true;
            BodyLine(out Vector3 from, out Vector3 to);
            _moteSeed++;
            for (int i = 0; i < BurstMotes; i++)
            {
                float n = PixelNoise.Value(i, _moteSeed, 71);
                float m = PixelNoise.Value(i, _moteSeed, 73);
                Vector3 at = Vector3.Lerp(from, to, (i + .5f) / BurstMotes) + new Vector3((n - .5f) * .16f, (m - .5f) * .12f, 0f);
                var velocity = new Vector3((m - .5f) * .6f, .5f + .6f * n, 0f);
                ArmMote(at, velocity, .6f + .35f * m, .04f + .035f * n, i % 3 == 0 ? Ember : Ash);
            }
            Color glow = Ember;
            glow.a = .55f;
            ArmGlow(_halo, Vector3.Lerp(from, to, .5f), DeathGlowSeconds, .25f, .8f, glow);
        }

        private void StreamMote()
        {
            BodyLine(out Vector3 from, out Vector3 to);
            _moteSeed++;
            float n = PixelNoise.Value(_moteSeed, 1, 79);
            float m = PixelNoise.Value(_moteSeed, 2, 79);
            Vector3 at = Vector3.Lerp(from, to, .2f + .7f * n) + new Vector3((m - .5f) * .2f, 0f, 0f);
            ArmMote(at, new Vector3((m - .5f) * .3f, .55f + .25f * n, 0f), .45f + .15f * m, .035f + .015f * n,
                (_moteSeed % 3) == 0 ? Ember : Ash);
        }

        // Where the body is, from its feet to its head, in the root's space: the puppet's head bone as it falls; Ilya's
        // death lies across the feet; the painted frames' body is hidden where it stood.
        private void BodyLine(out Vector3 from, out Vector3 to)
        {
            from = new Vector3(0f, .1f, 0f);
            PuppetRig rig = _puppet != null ? _puppet.ActiveRig : null;
            int head = rig != null ? rig.IndexOf(PuppetBone.Head) : -1;
            if (head >= 0)
            {
                Vector3 h = rig.Bone(head).position - transform.position;
                to = new Vector3(h.x, Mathf.Max(.08f, h.y), 0f);
                return;
            }
            if (_drawn != null)
            {
                from = new Vector3(-.45f, .14f, 0f);
                to = new Vector3(.45f, .14f, 0f);
                return;
            }
            to = new Vector3(0f, 1f, 0f);
        }

        private void ArmMote(Vector3 origin, Vector3 velocity, float lifetime, float size, Color colour)
        {
            Mote mote = _motes[_nextMote];
            _nextMote = (_nextMote + 1) % _motes.Length;
            mote.Active = true;
            mote.Elapsed = 0f;
            mote.Lifetime = lifetime;
            mote.Origin = origin;
            mote.Velocity = velocity;
            mote.Size = size;
            mote.Colour = colour;
            DrawMote(mote);
        }

        // Rising and slowing, thinning and shrinking to nothing: ash lifting off.
        private static void DrawMote(Mote mote)
        {
            float u = Mathf.Clamp01(mote.Elapsed / mote.Lifetime);
            Transform at = mote.Renderer.transform;
            at.localPosition = mote.Origin + mote.Velocity * (mote.Elapsed * (1f - .35f * u));
            float size = mote.Size * (1f - .55f * u);
            at.localScale = new Vector3(size, size, 1f);
            Color colour = mote.Colour;
            colour.a *= Mathf.Min(1f, u / .12f) * (1f - u);
            mote.Renderer.color = colour;
            mote.Renderer.enabled = true;
        }

        private void TickMotes(float dt)
        {
            if (dt <= 0f)
                return;
            for (int i = 0; i < _motes.Length; i++)
            {
                Mote mote = _motes[i];
                if (!mote.Active)
                    continue;
                mote.Elapsed += dt;
                if (mote.Elapsed >= mote.Lifetime)
                {
                    mote.Active = false;
                    mote.Renderer.enabled = false;
                    continue;
                }
                DrawMote(mote);
            }
        }

        private static void ArmGlow(Glow glow, Vector3 centre, float lifetime, float fromRadius, float toRadius, Color colour)
        {
            glow.Active = true;
            glow.Elapsed = 0f;
            glow.Lifetime = lifetime;
            glow.FromRadius = fromRadius;
            glow.ToRadius = toRadius;
            glow.Colour = colour;
            glow.Renderer.transform.localPosition = centre;
            DrawGlow(glow);
        }

        private static void DrawGlow(Glow glow)
        {
            float u = Mathf.Clamp01(glow.Elapsed / glow.Lifetime);
            float radius = Mathf.Lerp(glow.FromRadius, glow.ToRadius, EaseOut(u));
            glow.Renderer.transform.localScale = new Vector3(radius, radius, 1f);
            Color colour = glow.Colour;
            colour.a *= (1f - u) * (1f - u);
            glow.Renderer.color = colour;
            glow.Renderer.enabled = true;
        }

        private static void TickGlow(Glow glow, float dt)
        {
            if (!glow.Active || dt <= 0f)
                return;
            glow.Elapsed += dt;
            if (glow.Elapsed >= glow.Lifetime)
            {
                glow.Active = false;
                glow.Renderer.enabled = false;
                return;
            }
            DrawGlow(glow);
        }

        // The picked weapon: from the shoulders up over the head, turning once about its middle, over a warm glow.
        private void TickPickup(float dt)
        {
            if (!_picking || dt <= 0f)
                return;
            if (!_pickStarted)
            {
                _pickStarted = true;
                StartPop();
                Color warm = Ember;
                warm.a = .5f;
                ArmGlow(_pickGlow, new Vector3(0f, PickupFrom, 0f), PickupSeconds, .18f, .42f, warm);
            }
            else
                _pickElapsed += dt;
            if (_pickElapsed >= PickupSeconds)
            {
                EndPickup();
                return;
            }
            float t = Mathf.Clamp01(_pickElapsed / PickupSeconds);
            float rise = EaseOut(t);
            var at = new Vector3(0f, PickupFrom + PickupRise * rise, 0f);
            if (_pickGlow.Active)
                _pickGlow.Renderer.transform.localPosition = at;
            Sprite sprite = _pickGhost.sprite;
            if (sprite == null)
                return;
            float strength = t < .15f ? t / .15f : t > .6f ? 1f - (t - .6f) / .4f : 1f;
            float scale = Mathf.Lerp(.9f, 1.15f, rise);
            Quaternion turn = Quaternion.Euler(0f, 0f, 360f * rise);
            Transform ghost = _pickGhost.transform;
            ghost.localRotation = turn;
            ghost.localScale = new Vector3(scale, scale, 1f);
            ghost.localPosition = at - turn * (sprite.bounds.center * scale);
            _pickGhost.color = new Color(1f, 1f, 1f, strength);
            _pickGhost.enabled = true;
        }

        private void EndPickup()
        {
            _picking = false;
            _pickStarted = false;
            if (_pickGhost != null)
                _pickGhost.enabled = false;
            if (_pickGlow != null)
            {
                _pickGlow.Active = false;
                if (_pickGlow.Renderer != null)
                    _pickGlow.Renderer.enabled = false;
            }
        }

        // Everything back at rest and hidden (the view disabled, the scene going).
        private void EndReactions()
        {
            _hitting = false;
            _popping = false;
            if (_felt)
                WriteFigure(1f, 1f, Vector3.zero);
            _felt = false;
            _scaleX = _scaleY = 1f;
            _offset = Vector3.zero;
            for (int i = 0; _motes != null && i < _motes.Length; i++)
            {
                _motes[i].Active = false;
                if (_motes[i].Renderer != null)
                    _motes[i].Renderer.enabled = false;
            }
            if (_halo != null)
            {
                _halo.Active = false;
                if (_halo.Renderer != null)
                    _halo.Renderer.enabled = false;
            }
            EndPickup();
        }

        // ---- The slot cues (stage 1) ----

        private void Tick(float dt)
        {
            if (dt <= 0f)
                return;
            for (int i = 0; i < _cues.Length; i++)
            {
                Cue cue = _cues[i];
                if (!cue.Active)
                    continue;
                cue.Elapsed += dt;
                if (cue.Elapsed >= cue.Seconds)
                {
                    End(cue);
                    continue;
                }
                Draw(cue);
            }
        }

        private void Draw(Cue cue)
        {
            float t = cue.Seconds > 0f ? Mathf.Clamp01(cue.Elapsed / cue.Seconds) : 1f;
            switch (cue.Kind)
            {
                case SlotCue.Flourish:
                    DrawFlourish(cue, t);
                    break;
                case SlotCue.Pulse:
                    DrawPulse(cue, t);
                    break;
                default:
                    DrawFlick(cue, t);
                    break;
            }
        }

        // A blade direction for the hero facing right, turned to the cue's side.
        private static float Toward(float degrees, float side) => side > 0f ? degrees : 180f - degrees;

        private static Vector3 Along(Vector3 from, float degrees, float reach)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector3(from.x + Mathf.Cos(radians) * reach, from.y + Mathf.Sin(radians) * reach, 0f);
        }

        private void DrawFlourish(Cue cue, float t)
        {
            float strength = GameFeel.CueEnvelope(t);
            var pivot = new Vector3(cue.Side * FlourishPivotX, FlourishPivotY, 0f);
            float angle = Toward(GameFeel.FlourishAngle(t), cue.Side);
            ShowGhost(cue.Sprites[0], pivot, angle, Steel, FlourishGhostAlpha * strength);
            // The arc behind the blade's tip, from where it was a share of the sweep ago to where it is.
            float tail = Mathf.Max(0f, t - FlourishTrailLag);
            Vector3[] points = cue.Points[0];
            float reach = cue.Reach * .9f;
            for (int i = 0; i < points.Length; i++)
            {
                float u = Mathf.Lerp(tail, t, i / (points.Length - 1f));
                points[i] = Along(pivot, Toward(GameFeel.FlourishAngle(u), cue.Side), reach);
            }
            ShowLine(cue.Lines[0], points, Steel, 0f, FlourishTrailAlpha * strength, .005f, FlourishTrailWidth * strength);
        }

        private void DrawPulse(Cue cue, float t)
        {
            float strength = GameFeel.CueEnvelope(t);
            float lift = PulseRaise * EaseOut(Mathf.Clamp01(t / .35f));
            var grip = new Vector3(cue.Side * PulseX, PulseY + lift, 0f);
            // Tilted out, away from the body: the off side is -Side, so its top leans further that way.
            float tilt = cue.Side * PulseTilt;
            ShowGhost(cue.Sprites[0], grip, 90f + tilt, Color.white, PulseGhostAlpha * strength);
            Vector3 tip = Along(grip, 90f + tilt, cue.Reach);
            SpriteRenderer glow = cue.Sprites[1];
            float radius = Mathf.Lerp(PulseGlowFrom, PulseGlowTo, EaseOut(t));
            glow.transform.localPosition = tip;
            glow.transform.localRotation = Quaternion.identity;
            glow.transform.localScale = new Vector3(radius, radius, 1f);
            Color colour = StaffGlow;
            colour.a = PulseGlowAlpha * strength * (1f - .5f * t);
            glow.color = colour;
            glow.enabled = true;
            Vector3[] ring = cue.Points[0];
            float spread = Mathf.Lerp(PulseRingFrom, PulseRingTo, EaseOut(t));
            for (int i = 0; i < ring.Length; i++)
            {
                float a = i * Mathf.PI * 2f / ring.Length;
                ring[i] = new Vector3(tip.x + Mathf.Cos(a) * spread, tip.y + Mathf.Sin(a) * spread, 0f);
            }
            float fade = PulseRingAlpha * (1f - t);
            ShowLine(cue.Lines[0], ring, StaffRing, fade, fade, PulseRingWidth, PulseRingWidth);
        }

        private void DrawFlick(Cue cue, float t)
        {
            for (int k = 0; k < 2; k++)
            {
                float own = (t - k * FlickStagger) / FlickShare;
                SpriteRenderer blade = cue.Sprites[k];
                LineRenderer streak = cue.Lines[k];
                if (own < 0f || own >= 1f)
                {
                    blade.enabled = false;
                    streak.enabled = false;
                    continue;
                }
                float strength = GameFeel.CueEnvelope(own);
                var grip = new Vector3(cue.Side * FlickX, FlickY + k * FlickRise, 0f);
                // The first flicks from up and back down to forward, the second from level to forward and down.
                float from = k == 0 ? 120f : 75f;
                float to = k == 0 ? 15f : -30f;
                float angle = Toward(Mathf.Lerp(from, to, EaseOut(own)), cue.Side);
                ShowGhost(blade, grip, angle, Steel, FlickGhostAlpha * strength);
                Vector3[] points = cue.Points[k];
                float tail = Mathf.Max(0f, own - .5f);
                for (int i = 0; i < points.Length; i++)
                {
                    float u = Mathf.Lerp(tail, own, i / (points.Length - 1f));
                    points[i] = Along(grip, Toward(Mathf.Lerp(from, to, EaseOut(u)), cue.Side), cue.Reach * .85f);
                }
                ShowLine(streak, points, Steel, 0f, FlickStreakAlpha * strength, .004f, FlickStreakWidth * strength);
            }
        }

        // A ghost on its grip, its blade along the direction (degrees; the sprites are drawn blade up). A cue whose weapon
        // has no sprite shows its trails alone.
        private static void ShowGhost(SpriteRenderer ghost, Vector3 grip, float degrees, Color colour, float alpha)
        {
            if (ghost.sprite == null)
            {
                ghost.enabled = false;
                return;
            }
            Transform at = ghost.transform;
            at.localPosition = grip;
            at.localRotation = Quaternion.Euler(0f, 0f, degrees - 90f);
            at.localScale = Vector3.one;
            colour.a = alpha;
            ghost.color = colour;
            ghost.enabled = true;
        }

        private static void ShowLine(LineRenderer line, Vector3[] points, Color colour, float tailAlpha, float headAlpha,
            float tailWidth, float headWidth)
        {
            line.SetPositions(points);
            Color tail = colour;
            tail.a = tailAlpha;
            Color head = colour;
            head.a = headAlpha;
            line.startColor = tail;
            line.endColor = head;
            line.startWidth = tailWidth;
            line.endWidth = headWidth;
            line.enabled = true;
        }

        private static void End(Cue cue)
        {
            cue.Active = false;
            for (int i = 0; i < cue.Sprites.Length; i++)
                if (cue.Sprites[i] != null)
                    cue.Sprites[i].enabled = false;
            for (int i = 0; i < cue.Lines.Length; i++)
                if (cue.Lines[i] != null)
                    cue.Lines[i].enabled = false;
        }

        private static float EaseOut(float t)
        {
            float inverse = 1f - Mathf.Clamp01(t);
            return 1f - inverse * inverse;
        }
    }
}
