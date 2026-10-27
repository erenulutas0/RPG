using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // Pooled strikes, hit sparks, numbers, deaths, arrivals and coins. Optional imported slash/spark/death frames are borrowed;
    // remaining procedural sprites are generated once in Awake. Per-hit effects reuse objects and arrays. Effects run
    // on scaled time so a pause freezes them; the shake runs on unscaled time so it always settles. Each effect above the
    // floor remembers the floor it stands on, which the foundry turns into depth while a camera renders
    // (FoundryVisibilityView); everywhere else that height is never read. The skills draw here too (docs/45): a burning
    // enemy's flame (BurnFlames, its own pool of one renderer per burning enemy), each Crucible Vent eruption, the ember
    // Ember Memory passes to a neighbour, and the Forge Burst's shockwave and sparks. Their painted art (a
    // SkillEffectArtSet, one sheet per effect family) is borrowed and cut into sprites here, as the painted strikes are
    // borrowed; without it they fall back to the pixel art BurnArt and EffectArt draw. Painted, the Vent's cast rings the
    // hero in ember rather than in the ability's steel seal, and the Forge Burst flashes at the hero. The Daggers' double
    // slash, the crit star, the smoke, the coins and the numbers are painted too when a CombatFeedbackArtSet is given (one
    // sheet for them all, cut here likewise), and otherwise EffectArt's pixel art.
    public sealed class CombatEffectsView : MonoBehaviour
    {
        private sealed class Effect
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Sprite[] Frames;
            public float FrameDuration;
            public float Lifetime;
            public float Elapsed;
            public bool Loop;
            public Vector3 Origin;
            public Vector3 Velocity;
            public float Gravity;
            public bool Active;
            public bool Fade;
            // The colour a fading effect fades from; white for everything but the arrival puff.
            public Color Tint;
            // How far a fading effect settles into the floor as it goes, in world units.
            public float Sink;
            public bool AbilityPulse;
            public bool StaffPulse;
            // The Forge Burst's shockwave, which races out from the hero to PulseRadius.
            public bool Shockwave;
            public float PulseRadius;
            // Turned along its flight each frame: a sprite drawn facing +x, a streak whose tail trails behind it.
            public bool FaceMotion;
            // Drawn above the floor and standing on Ground (world): the feet of the actor it belongs to, or the floor under
            // the point it was placed at. Floor pulses never are; they already sort under the foundry's structures.
            public bool AboveFloor;
            public Vector3 Ground;
        }

        // Numbers are read, not seen as things in the world, so they stay on the flat layer over every foundry structure,
        // like the HUD: a structure edge crossing a rising number would cut digits off and change the value it reads, and
        // an enemy hidden behind a structure is still marked by its silhouette, which its number rises from.
        private sealed class Number
        {
            public Transform Transform;
            public SpriteRenderer[] Digits;
            public int DigitCount;
            public Vector3 Ground;
            public float Elapsed;
            public Vector3 Origin;
            public bool Active;
        }

        // One slot of the current wave. Health.Damaged carries only the context, so each slot forwards its own enemy; the
        // slots are bound and released as waves come and go, never allocated per hit.
        private sealed class EnemyWatch
        {
            private readonly CombatEffectsView _view;
            public Health Enemy;
            public Transform Root;
            public Vector3 BodyOffset;
            public EnemyLookView Look;

            public EnemyWatch(CombatEffectsView view) => _view = view;

            public void OnDamaged(DamageContext context) => _view.OnEnemyDamaged(this, context);
        }

        // Where effects land on an enemy whose body has no renderer yet, above its feet.
        private const float DefaultBodyHeight = 0.45f;
        private const float NumberLift = 0.25f;
        private const float NumberJitter = 0.35f;
        private const float StrikeFrameDuration = 0.05f;
        private const float StaffPulseDuration = .21f;
        private const float StarFrameDuration = 0.07f;
        private const float SparkFrameDuration = 0.06f;
        private const float SmokeFrameDuration = 0.12f;
        private const float SmokeRise = 0.4f;
        private const float SmokeScale = 0.55f;
        // A horde enemy's arrival: its body fades in over this long (EnemyLookView.Arrive) over a faint ember puff at its
        // feet, the death smoke's frames thinner, smaller and quicker. The puffs keep slots of their own past the shared
        // pool, so a crowd walking in never cuts a death's remains short.
        internal const float ArrivalSeconds = 0.25f;
        private const int PuffSlots = 6;
        private const float PuffFrameDuration = 0.1f;
        private const float PuffRise = 0.25f;
        // The puff's centre starts just above the feet, so it billows round the ankles rather than under the floor.
        private const float PuffLift = 0.08f;
        private const float PuffScale = 0.4f;
        private static readonly Color PuffTint = new Color(1f, 1f, 1f, 0.55f);
        // Burn's ember (docs/45): the Arcanist's accent, #E77232.
        private static readonly Color BurnTint = new Color(.906f, .447f, .196f, 1f);
        // The remains settle this far into the floor as they fade, so a fall ends lying down rather than lifting off.
        private const float RemainsSink = 0.05f;
        private const float CoinFrameDuration = 0.08f;
        private const float CoinGravity = 9.8f;
        private const float CoinLaunch = 2.4f;
        private const float CoinSpread = 0.9f;
        private const int CoinCount = 3;
        private const float MinRingScale = 0.75f;
        private const float MaxRingScale = 1.5f;
        // The skills' bursts - eruptions, passed embers, the Forge Burst's shockwave and sparks - keep slots of their own
        // past the puffs, round robin among themselves: a Vent across a crowd never cuts short the falls and hit sparks it
        // causes, and they never cut short its eruptions. Forty holds ten full eruptions at once.
        private const int BurstSlots = 40;
        // A Crucible Vent eruption: the column, scaled by the stacks it spent, and the embers it throws up, which rise
        // faster and larger with more stacks; a second cluster from three stacks.
        private const float VentColumnFrameDuration = 0.08f;
        private const float VentColumnScale = 0.62f;
        private const float VentColumnScalePerStack = 0.1f;
        private const float VentEmberFrameDuration = 0.2f;
        private const float VentEmberDuration = 0.6f;
        private const float VentEmberRise = 0.7f;
        private const float VentEmberRisePerStack = 0.12f;
        private const float VentEmberScale = 0.8f;
        private const float VentEmberScalePerStack = 0.08f;
        // How far up the column, at its scale, the embers leave it.
        private const float VentEmberLift = 0.45f;
        // Ember Memory's passed ember: from the fallen body to its neighbour's in this long, over an arc this high.
        internal const float SpreadFlightSeconds = 0.24f;
        private const float SpreadArcHeight = 0.35f;
        private const float SpreadFrameDuration = 0.06f;
        // The Forge Burst's shockwave grows from this share of the burst's reach to all of it, then fades.
        private const float ShockwaveFrameDuration = 0.07f;
        private const float ShockwaveGrowSeconds = 0.16f;
        private const float ShockwaveStart = 0.25f;
        // World radius of the pixel shockwave sprite at scale 1; the painted one is a unit ring.
        private const float PixelShockwaveSpriteRadius = EffectArt.ShockwaveRadius / PixelSpriteFactory.PixelsPerUnit;
        // The painted bursts' frame times: the painted strips carry more frames than the pixel ones, over about as long
        // (the eruption a little longer, so its column reads at phone size before it breaks).
        private const float PaintedVentColumnFrameDuration = 0.05f;
        private const float PaintedVentEmberFrameDuration = 0.15f;
        private const float PaintedShockwaveFrameDuration = 0.06f;
        private const float PaintedForgeSparkFrameDuration = 0.1f;
        // The painted Vent's cast ring burns out over the ability pulse's life; the Forge Burst's flash is gone faster,
        // centred on the hero's body this high over its feet.
        private const float VentCastFrameDuration = 0.07f;
        private const float ForgeFlashFrameDuration = 0.04f;
        private const float ForgeFlashLift = 0.5f;
        // Its sparks fly out along the floor at 1.6-2.4 times the reach a second, popping up as they go.
        internal const int ForgeSparkCount = 8;
        private const float ForgeSparkDuration = 0.3f;
        private const float ForgeSparkGravity = 6f;
        private const float ForgeSparkPop = 1.2f;
        private const float ForgeSparkLift = 0.3f;
        private const float ForgeSparkSpeed = 1.6f;
        private const float ForgeSparkSpeedSpread = 0.8f;
        // The painted feedback's frame times: its strips carry more frames than the pixel ones, the cuts and the star over
        // about as long, the death smoke and the arrival puff over exactly as long, and the coin one whole turn in eight.
        private const float PaintedDoubleSlashFrameDuration = 0.04f;
        private const float PaintedStarFrameDuration = 0.05f;
        private const float PaintedSmokeFrameDuration = 0.06f;
        private const float PaintedPuffFrameDuration = 0.05f;
        private const float PaintedCoinFrameDuration = 0.04f;
        // Sorting orders above the base: the floor ring lowest, coins highest. A burning enemy's flame sits on the base
        // itself, over the combatants and under every spark and strike that lands on it.

        private const int FlameOrder = 0;
        private const int SparkOrder = 1;
        private const int SmokeOrder = 1;
        private const int StrikeOrder = 2;
        private const int StarOrder = 3;
        private const int CoinOrder = 4;
        private const int NumberSeed = 41;
        private const int CoinSeed = 43;
        private const int ShakeSeed = 47;
        private const int VentEmberSeed = 57;
        private const int ForgeSparkSeed = 59;

        [SerializeField] private EncounterController _encounter;
        [SerializeField] private Health _hero;
        [SerializeField] private AttackController _heroAttack;
        // Optional: the hero's ability, whose burst rings the hero at its full radius.
        [SerializeField] private AbilityController _heroAbility;
        // Optional: the chests, whose opening pops coins or shows the heal.
        [SerializeField] private ChestSpawner _chests;
        [SerializeField] private Camera _camera;
        // Draws sprites; the built-in Sprites-Default material does.
        [SerializeField] private Material _material;
        [SerializeField] private StrikeArtSet _paintedArt;
        public bool UsesPaintedStrikes => _paintedArt != null && _paintedArt.IsValid;
        // Optional: the skills' painted effects; without them the skills draw the pixel art.
        [SerializeField] private SkillEffectArtSet _skillArt;
        public bool UsesPaintedSkills => _skillArt != null && _skillArt.IsValid;
        // Optional: the painted double slash, crit star, smoke, coin and numerals; without them, EffectArt's pixel art.
        [SerializeField] private CombatFeedbackArtSet _feedbackArt;
        public bool UsesPaintedFeedback => _feedbackArt != null && _feedbackArt.IsValid;
        [SerializeField, Min(1)] private int _effectPoolSize = 24;
        [SerializeField, Min(1)] private int _numberPoolSize = 12;
        // Effects draw from this order up to four above it, over the combatants; numbers draw over the health bars.
        [SerializeField] private int _sortingOrder = 5;
        [SerializeField] private int _numberSortingOrder = 12;
        [SerializeField, Min(0f)] private float _numberRise = 0.6f;
        [SerializeField, Min(0.01f)] private float _numberDuration = 0.7f;
        [SerializeField, Min(0.01f)] private float _coinDuration = 0.5f;
        // The blast ring shows this fraction of the staff's splash radius: the whole radius would ring the entire platform.
        [SerializeField, Range(0.1f, 1f)] private float _ringSplashFraction = 0.5f;
        [SerializeField, Min(0f)] private float _shakeAmplitude = 0.03f;
        [SerializeField, Min(0.01f)] private float _shakeDuration = 0.1f;

        private readonly List<Sprite> _sprites = new List<Sprite>();
        // Sprites cut from the painted skill sheets: this view owns them, the art set owns the sheets.
        private readonly List<Sprite> _cutSprites = new List<Sprite>();
        private Sprite[] _slash;
        private Sprite[] _ring;
        private Sprite[] _abilityPulse;
        private int _groundOrder;
        private Sprite[] _doubleSlash;
        private Sprite[] _star;
        private Sprite[] _spark;
        private Sprite[] _smoke;
        private Sprite[] _coin;
        private Sprite[] _digits;
        private Sprite[] _critDigits;
        private Sprite[] _ventColumn;
        private Sprite[] _ventEmbers;
        private Sprite[] _emberArc;
        private Sprite[] _shockwave;
        private Sprite[] _forgeSpark;
        // Painted only: the Vent's ember cast ring and the Forge Burst's flash.
        private Sprite[] _ventCast;
        private Sprite[] _forgeFlash;
        // The frame times of the art in use, painted or pixel, and the shockwave sprite's world radius at scale 1.
        private float _ventColumnFrameDuration;
        private float _ventEmberFrameDuration;
        private float _shockwaveFrameDuration;
        private float _forgeSparkFrameDuration;
        private float _shockwaveSpriteRadius;
        // The same for the feedback art in use, and its numerals' stride and glyph width, normal and crit, in world units.
        private float _doubleSlashFrameDuration;
        private float _starFrameDuration;
        private float _smokeFrameDuration;
        private float _puffFrameDuration;
        private float _coinFrameDuration;
        private float _digitStride;
        private float _critDigitStride;
        private float _digitWidth;
        private float _critDigitWidth;
        private GameObject _poolRoot;
        private Effect[] _effects;
        private BurnFlames _flames;
        private int _nextBurst;
        private int _ventCount;
        private int _forgeBursts;
        private Number[] _numbers;
        private EnemyWatch[] _watches;
        // The floor's horde, watched by its places, so its hits and falls show as the wave's do.
        private EnemyWatch[] _hordeWatches;
        private int _watchCount;
        private int _nextEffect;
        private int _nextPuff;
        private int _nextNumber;
        private int _hitCount;
        private int _deathCount;
        private bool _critPending;
        private bool _ready;
        private bool _subscribed;
        private float _shakeRemaining;
        private int _shakeStep;
        private Vector3 _shakeRest;
        private Vector3 _shakeOffset;

        public int ActiveEffectCount
        {
            get
            {
                int count = 0;
                for (int i = 0; _effects != null && i < _effects.Length; i++)
                {
                    if (_effects[i].Active)
                        count++;
                }
                return count;
            }
        }

        public int ActiveNumberCount
        {
            get
            {
                int count = 0;
                for (int i = 0; _numbers != null && i < _numbers.Length; i++)
                {
                    if (_numbers[i].Active)
                        count++;
                }
                return count;
            }
        }

        public bool IsShaking => _shakeRemaining > 0f;

        // The flame on show on this burning enemy, or null, how many are on show and may be, every frame of the flame
        // at a stack count, and the shockwave sprite's world radius at scale 1: for the tests.
        internal SpriteRenderer BurnFlameOn(Health enemy) => _flames?.On(enemy);
        internal int ShownFlameCount => _flames != null ? _flames.ShownCount : 0;
        internal int FlameCapacity => _flames != null ? _flames.SlotCount : 0;
        internal Sprite[] BurnFlameFrames(int stacks) => _flames?.Frames(stacks);
        internal bool PaintsBurnFlames => _flames != null && _flames.IsPainted;
        internal float ShockwaveSpriteRadius => _shockwaveSpriteRadius;

        // Pool slots, and the effect on show above the floor in one with the world height of the floor it stands on, for
        // presentation that depth-tests effects against scenery (FoundryVisibilityView). Floor pulses and numbers are
        // never returned, and a parked slot is null. The burning enemies' flames follow the effects' slots: each stands on
        // its enemy's feet, so the structure that hides an enemy hides its flame.
        internal int EffectSlotCount => _effects != null ? _effects.Length + _flames.SlotCount : 0;

        internal Transform AboveFloorEffect(int slot, out float groundY)
        {
            if (slot >= _effects.Length)
            {
                Transform flame = _flames.Surface(slot - _effects.Length, out Vector3 feet);
                groundY = feet.y;
                return flame;
            }
            Effect effect = _effects[slot];
            groundY = effect.Ground.y;
            return effect.Active && effect.AboveFloor ? effect.Transform : null;
        }

        internal Transform SurfaceEffect(int slot, out Vector3 ground, out bool above, out SpriteRenderer floor)
        {
            if (slot >= _effects.Length)
            {
                above = true;
                floor = null;
                return _flames.Surface(slot - _effects.Length, out ground);
            }
            var effect = _effects[slot]; ground = effect.Ground; above = effect.AboveFloor;
            floor = !above && effect.Active ? effect.Renderer : null;
            return effect.Active ? effect.Transform : null;
        }
        internal int NumberSlotCount => _numbers != null ? _numbers.Length : 0;
        internal Transform SurfaceNumber(int slot, out Vector3 ground)
        {
            var number = _numbers[slot]; ground = number.Ground;
            return number.Active ? number.Transform : null;
        }

        private void Awake()
        {
            if (_encounter == null || _hero == null || _heroAttack == null || _camera == null || _material == null)
            {
                Debug.LogError("CombatEffectsView needs the encounter controller, the hero's health and attack, the camera and a sprite material.", this);
                enabled = false;
                return;
            }

            BuildSprites();
            BuildPools();
            _watches = new EnemyWatch[PackLayout.MaxPackSize];
            for (int i = 0; i < _watches.Length; i++)
                _watches[i] = new EnemyWatch(this);
            _hordeWatches = new EnemyWatch[PackLayout.MaxPackSize];
            for (int i = 0; i < _hordeWatches.Length; i++)
                _hordeWatches[i] = new EnemyWatch(this);
            _ready = true;
        }

        private void Start()
        {
            var arena = FindFirstObjectByType<ArenaView>();
            if (arena != null && arena.IsFoundryRegion)
                _groundOrder = arena.GroundEffectSortingOrder;
        }

        private void OnEnable()
        {
            if (!_ready)
                return;

            _encounter.EncounterStarted += OnEncounterStarted;
            _encounter.EnemyDefeated += OnEnemyDefeated;
            _encounter.HordeEnemySpawned += OnHordeEnemySpawned;
            _encounter.HordeReleased += ReleaseHorde;
            _heroAttack.Struck += OnHeroStruck;
            _heroAttack.StruckWith += OnHeroStruckWith;
            _hero.Damaged += OnHeroDamaged;
            if (_heroAbility != null)
            {
                _heroAbility.Used += OnAbilityUsed;
                _heroAbility.Vented += OnVented;
            }
            if (_chests != null)
                _chests.Opened += OnChestOpened;
            // Any burn in the scene, whoever lit it: a tick finds a burning enemy the strikes did not (a spread, a test's
            // own ignition) within half a second.
            BurnStatus.Burned += OnBurned;
            BurnStatus.Spread += OnBurnSpread;
            _subscribed = true;
            // The first wave may have spawned in an earlier Awake, before this view could listen.
            if (_encounter.WaveEnemyCount > 0)
                BindWave();
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                _encounter.EncounterStarted -= OnEncounterStarted;
                _encounter.EnemyDefeated -= OnEnemyDefeated;
                _encounter.HordeEnemySpawned -= OnHordeEnemySpawned;
                _encounter.HordeReleased -= ReleaseHorde;
                ReleaseHorde();
                _heroAttack.Struck -= OnHeroStruck;
                _heroAttack.StruckWith -= OnHeroStruckWith;
                _hero.Damaged -= OnHeroDamaged;
                if (_heroAbility != null)
                {
                    _heroAbility.Used -= OnAbilityUsed;
                    _heroAbility.Vented -= OnVented;
                }
                if (_chests != null)
                    _chests.Opened -= OnChestOpened;
                BurnStatus.Burned -= OnBurned;
                BurnStatus.Spread -= OnBurnSpread;
                _subscribed = false;
            }
            ReleaseWave();
            HideAll();
            _flames?.Clear();
            EndShake();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _sprites.Count; i++)
                PixelSpriteFactory.Destroy(_sprites[i]);
            _sprites.Clear();
            // Only the sprites: their sheets are the art set's.
            for (int i = 0; i < _cutSprites.Count; i++)
                Destroy(_cutSprites[i]);
            _cutSprites.Clear();
            _flames?.Dispose();
            _flames = null;
            if (_poolRoot != null)
                Destroy(_poolRoot);
        }

        private void BuildSprites()
        {
            _slash = UsesPaintedStrikes ? _paintedArt.Slash : Build(EffectArt.SlashFrames(), "VFX Slash", PixelSpriteFactory.Centre);
            _ring = new[] { StaffSplashArt.Get() }; // Borrowed session art.
            _abilityPulse = new[] { AbilityRingArt.Get() }; // Borrowed; never added to _sprites.
            _spark = UsesPaintedStrikes ? _paintedArt.Spark : Build(EffectArt.SparkFrames(), "VFX Spark", PixelSpriteFactory.Centre);
            // Imported references are borrowed; only generated sprites enter the destruction list.
            BuildFeedbackSprites();

            if (UsesPaintedSkills)
            {
                // Each strip pins where its painted frames say: the column at its foot, the streaks at their heads so
                // they trail behind the point they fly through, the rings at their centres.
                _ventColumn = Cut(_skillArt.VentColumn, "VFX Vent Column ");
                _ventEmbers = Cut(_skillArt.VentEmbers, "VFX Vent Embers ");
                _ventCast = Cut(_skillArt.VentRing, "VFX Vent Cast ");
                _emberArc = Cut(_skillArt.EmberArc, "VFX Ember Arc ");
                _shockwave = Cut(_skillArt.Shockwave, "VFX Forge Shockwave ");
                _forgeSpark = Cut(_skillArt.ForgeSpark, "VFX Forge Spark ");
                _forgeFlash = Cut(_skillArt.ForgeFlash, "VFX Forge Flash ");
                _ventColumnFrameDuration = PaintedVentColumnFrameDuration;
                _ventEmberFrameDuration = PaintedVentEmberFrameDuration;
                _shockwaveFrameDuration = PaintedShockwaveFrameDuration;
                _forgeSparkFrameDuration = PaintedForgeSparkFrameDuration;
                _shockwaveSpriteRadius = 1f;
                return;
            }
            // The Arcanist's bursts are drawn on BurnArt's finer grid; the column stands on the floor at its foot, and the
            // streaks pin at their heads so they trail behind the point they fly through.
            _ventColumn = Build(BurnArt.VentColumnFrames(), "VFX Vent Column", PixelSpriteFactory.BottomCentre, BurnArt.TexelsPerUnit);
            _ventEmbers = Build(BurnArt.VentEmberFrames(), "VFX Vent Embers", PixelSpriteFactory.Centre, BurnArt.TexelsPerUnit);
            _emberArc = Build(BurnArt.EmberArcFrames(), "VFX Ember Arc",
                new Vector2((BurnArt.EmberArcHeadX + .5f) / BurnArt.EmberArcWidth, .5f), BurnArt.TexelsPerUnit);
            _shockwave = Build(EffectArt.ShockwaveFrames(), "VFX Forge Shockwave", PixelSpriteFactory.Centre);
            _forgeSpark = Build(EffectArt.ForgeSparkFrames(), "VFX Forge Spark",
                new Vector2((EffectArt.ForgeSparkHeadX + .5f) / EffectArt.ForgeSparkWidth, .5f));
            _ventColumnFrameDuration = VentColumnFrameDuration;
            _ventEmberFrameDuration = VentEmberFrameDuration;
            _shockwaveFrameDuration = ShockwaveFrameDuration;
            _forgeSparkFrameDuration = ForgeSparkDuration * .5f;
            _shockwaveSpriteRadius = PixelShockwaveSpriteRadius;
        }

        // The double slash, crit star, smoke, coin and numerals, and the times and layout of the art in use. Either way the
        // numerals pivot at their bottom-left corners, so a number is laid out left to right by stride.
        private void BuildFeedbackSprites()
        {
            if (UsesPaintedFeedback)
            {
                // Cut from the set's one sheet, so the strikes, puffs, coins and every number on screen batch together.
                _doubleSlash = Cut(_feedbackArt.DoubleSlash, "VFX Double Slash ");
                _star = Cut(_feedbackArt.CritStar, "VFX Crit Star ");
                _smoke = Cut(_feedbackArt.Smoke, "VFX Smoke ");
                _coin = Cut(_feedbackArt.Coin, "VFX Coin ");
                _digits = Cut(_feedbackArt.Digits, "VFX Digit ");
                _critDigits = Cut(_feedbackArt.CritDigits, "VFX Crit Digit ");
                _doubleSlashFrameDuration = PaintedDoubleSlashFrameDuration;
                _starFrameDuration = PaintedStarFrameDuration;
                _smokeFrameDuration = PaintedSmokeFrameDuration;
                _puffFrameDuration = PaintedPuffFrameDuration;
                _coinFrameDuration = PaintedCoinFrameDuration;
                _digitStride = _feedbackArt.DigitStride;
                _critDigitStride = _feedbackArt.CritDigitStride;
                _digitWidth = _digits[0].bounds.size.x;
                _critDigitWidth = _critDigits[0].bounds.size.x;
                return;
            }
            _doubleSlash = Build(EffectArt.DoubleSlashFrames(), "VFX Double Slash", PixelSpriteFactory.Centre);
            _star = Build(EffectArt.StarBurstFrames(), "VFX Crit Star", PixelSpriteFactory.Centre);
            _smoke = Build(EffectArt.SmokeFrames(), "VFX Smoke", PixelSpriteFactory.Centre);
            _coin = Build(EffectArt.CoinFrames(), "VFX Coin", PixelSpriteFactory.Centre);
            var digits = new PixelCanvas[10];
            var critDigits = new PixelCanvas[10];
            for (int digit = 0; digit < 10; digit++)
            {
                digits[digit] = EffectArt.DigitCanvas(digit, Rgba.White);
                critDigits[digit] = EffectArt.DigitCanvas(digit, PixelPalette.CoinLight, EffectArt.CritNumberScale);
            }
            _digits = Build(digits, "VFX Digit", Vector2.zero);
            _critDigits = Build(critDigits, "VFX Crit Digit", Vector2.zero);
            _doubleSlashFrameDuration = StrikeFrameDuration;
            _starFrameDuration = StarFrameDuration;
            _smokeFrameDuration = SmokeFrameDuration;
            _puffFrameDuration = PuffFrameDuration;
            _coinFrameDuration = CoinFrameDuration;
            // Outlined numerals overlap their outlines at the stride, so a glyph is the bare numeral and an outline each
            // side; a crit is all of it at CritNumberScale.
            const float texel = 1f / PixelSpriteFactory.PixelsPerUnit;
            _digitStride = EffectArt.DigitStride * texel;
            _critDigitStride = EffectArt.DigitStride * EffectArt.CritNumberScale * texel;
            _digitWidth = (EffectArt.DigitWidth + 2) * texel;
            _critDigitWidth = (EffectArt.DigitWidth + 2) * EffectArt.CritNumberScale * texel;
        }

        private Sprite[] Cut(SkillEffectArtSet.Strip strip, string prefix)
        {
            Sprite[] sprites = strip.Cut(prefix);
            _cutSprites.AddRange(sprites);
            return sprites;
        }

        private Sprite[] Build(PixelCanvas[] canvases, string name, Vector2 pivot,
            float pixelsPerUnit = PixelSpriteFactory.PixelsPerUnit)
        {
            var sprites = new Sprite[canvases.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = PixelSpriteFactory.CreateSprite(canvases[i], $"{name} {i}", pivot, pixelsPerUnit);
                _sprites.Add(sprites[i]);
            }
            return sprites;
        }

        private void BuildPools()
        {
            _poolRoot = new GameObject("Combat Effects");
            _poolRoot.transform.SetParent(transform, false);

            _effects = new Effect[_effectPoolSize + PuffSlots + BurstSlots];
            for (int i = 0; i < _effects.Length; i++)
            {
                SpriteRenderer renderer = CreateRenderer($"Effect {i}", _poolRoot.transform, _sortingOrder);
                _effects[i] = new Effect { Transform = renderer.transform, Renderer = renderer };
            }
            // One flame for every enemy the scene lets stand at once (HordeRule.SceneCap, horde and waves together); a
            // burning enemy past that waits for a free one.
            _flames = new BurnFlames(_poolRoot.transform, _material, _sortingOrder + FlameOrder, PackLayout.MaxPackSize,
                UsesPaintedSkills ? _skillArt : null);

            _numbers = new Number[_numberPoolSize];
            for (int i = 0; i < _numbers.Length; i++)
            {
                var root = new GameObject($"Number {i}");
                root.transform.SetParent(_poolRoot.transform, false);
                var digits = new SpriteRenderer[EffectArt.MaxDigits];
                for (int d = 0; d < digits.Length; d++)
                    digits[d] = CreateRenderer($"Digit {d}", root.transform, _numberSortingOrder);
                _numbers[i] = new Number { Transform = root.transform, Digits = digits };
            }
        }

        private SpriteRenderer CreateRenderer(string name, Transform parent, int sortingOrder)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sharedMaterial = _material;
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = false;
            return renderer;
        }

        private void OnEncounterStarted() => BindWave();

        private void BindWave()
        {
            ReleaseWave();
            int count = Mathf.Min(_encounter.WaveEnemyCount, _watches.Length);
            for (int i = 0; i < count; i++)
            {
                Health enemy = _encounter.WaveEnemyAt(i);
                if (enemy != null)
                    Bind(_watches[i], enemy);
            }
            _watchCount = count;
        }

        // A horde enemy has walked into a place: its watch follows the newcomer instead of the body it replaced.
        private void OnHordeEnemySpawned(int slot)
        {
            if (slot < 0 || slot >= _hordeWatches.Length)
                return;
            EnemyWatch watch = _hordeWatches[slot];
            Unbind(watch);
            Health enemy = _encounter.HordeEnemyAt(slot);
            if (enemy == null)
                return;
            Bind(watch, enemy);
            if (watch.Look != null)
                watch.Look.Arrive(ArrivalSeconds);
            ShowArrivalPuff(watch.Root.position);
        }

        // Reserved slots, round robin among themselves: when all are busy the oldest puff gives way, never a remains.
        private void ShowArrivalPuff(Vector3 feet)
        {
            Effect puff = Arm(_effects[_effectPoolSize + _nextPuff], _smoke, _puffFrameDuration, _smoke.Length * _puffFrameDuration,
                feet + Vector3.up * PuffLift, _sortingOrder + SmokeOrder, Vector3.up * PuffRise, 0f, false, PuffScale);
            _nextPuff = (_nextPuff + 1) % PuffSlots;
            puff.Fade = true;
            puff.Tint = PuffTint;
            puff.Renderer.color = PuffTint;
            StandOn(puff, feet);
        }

        private void ReleaseHorde()
        {
            if (_hordeWatches == null)
                return;
            for (int i = 0; i < _hordeWatches.Length; i++)
                Unbind(_hordeWatches[i]);
        }

        private void Unbind(EnemyWatch watch)
        {
            if (!ReferenceEquals(watch.Enemy, null))
                watch.Enemy.Damaged -= watch.OnDamaged;
            watch.Enemy = null;
            watch.Root = null;
            watch.Look = null;
        }

        private void Bind(EnemyWatch watch, Health enemy)
        {
            watch.Enemy = enemy;
            watch.Root = enemy.transform;
            watch.Look = enemy.GetComponent<EnemyLookView>();
            // Effects land at the middle of the body, whatever look the enemy was given. Only its place on screen: the
            // foundry may have moved the body in z for a render (FoundryVisibilityView), which effects must not inherit.
            SpriteRenderer body = enemy.GetComponentInChildren<SpriteRenderer>();
            Vector3 offset = body != null ? body.bounds.center - watch.Root.position : Vector3.up * DefaultBodyHeight;
            offset.z = 0f;
            watch.BodyOffset = offset;
            enemy.Damaged += watch.OnDamaged;
        }

        // Unsubscribing goes through the managed object, so it is safe after Unity destroyed the enemy.
        private void ReleaseWave()
        {
            for (int i = 0; i < _watchCount; i++)
            {
                EnemyWatch watch = _watches[i];
                if (!ReferenceEquals(watch.Enemy, null))
                    watch.Enemy.Damaged -= watch.OnDamaged;
                watch.Enemy = null;
                watch.Root = null;
                watch.Look = null;
            }
            _watchCount = 0;
        }

        private EnemyWatch FindWatch(Health enemy)
        {
            for (int i = 0; i < _watchCount; i++)
            {
                if (ReferenceEquals(_watches[i].Enemy, enemy))
                    return _watches[i];
            }
            for (int i = 0; _hordeWatches != null && i < _hordeWatches.Length; i++)
            {
                if (ReferenceEquals(_hordeWatches[i].Enemy, enemy))
                    return _hordeWatches[i];
            }
            return null;
        }

        private Vector3 AnchorOf(EnemyWatch watch, Health enemy)
        {
            if (watch != null && watch.Root != null)
                return watch.Root.position + watch.BodyOffset;
            return enemy != null ? enemy.transform.position + Vector3.up * DefaultBodyHeight : Vector3.zero;
        }

        // The floor the enemy stands on (its root, which the foundry's depth never moves), for the effects on its body.
        private static Vector3 FeetOf(EnemyWatch watch, Health enemy)
        {
            if (watch != null && watch.Root != null)
                return watch.Root.position;
            return enemy != null ? enemy.transform.position : Vector3.zero;
        }

        private void OnEnemyDamaged(EnemyWatch watch, DamageContext context)
        {
            Vector3 anchor = AnchorOf(watch, watch.Enemy);
            Effect spark = StandOn(Show(_spark, SparkFrameDuration, anchor, _sortingOrder + SparkOrder), FeetOf(watch, watch.Enemy));
            // A burning enemy sparks in ember (docs/45), so a Burn reads on the floor without a status icon: its ticks
            // spark twice a second until it goes out.
            BurnStatus burn = watch.Enemy != null ? watch.Enemy.GetComponent<BurnStatus>() : null;
            if (burn != null && burn.IsBurning)
            {
                spark.Tint = BurnTint;
                spark.Renderer.color = BurnTint;
            }

            bool fromHero = ReferenceEquals(context.Source, _hero);
            if (fromHero && context.IsCritical)
                _critPending = true;

            int amount = Mathf.RoundToInt(context.Amount);
            if (amount <= 0)
                return;

            _hitCount++;
            float jitter = (PixelNoise.Value(_hitCount, 0, NumberSeed) - 0.5f) * NumberJitter;
            ShowGroundNumber(amount, context.IsCritical, anchor + new Vector3(jitter, NumberLift, 0f), FeetOf(watch, watch.Enemy));
        }

        private void OnHeroStruck(Health target)
        {
            bool critical = _critPending;
            _critPending = false;
            DrawStrike(target, _heroAttack.Weapon, critical);
        }

        // A weapon after the first (docs/42 PR 9) strikes with its own effect - a Staff picked up beside the Sword rings,
        // the Sword beside the Staff slashes. The first weapon's strike is drawn by OnHeroStruck, which pairs it with its
        // crit; a later weapon's crit is not paired yet, so it shows its strike without the star.
        private void OnHeroStruckWith(Health target, WeaponRuntime weapon)
        {
            // Every weapon's hit may set its target alight (Ember Memory), so its flame shows on the frame of the hit
            // rather than at the burn's first tick.
            _flames.Consider(target);
            if (ReferenceEquals(weapon, _heroAttack.Weapon))
                return;
            DrawStrike(target, weapon, false);
        }

        private void DrawStrike(Health target, WeaponRuntime weapon, bool critical)
        {
            if (weapon == null || target == null)
                return;

            EnemyWatch watch = FindWatch(target);
            Vector3 anchor = AnchorOf(watch, target);
            Vector3 ground = FeetOf(watch, target);
            switch (EffectArt.StrikeFor(weapon.Pattern.Behavior))
            {
                case StrikeEffect.SwordSlash:
                    Effect slash = StandOn(ShowMoving(_slash, StrikeFrameDuration, _slash.Length * StrikeFrameDuration, anchor,
                        _sortingOrder + StrikeOrder, Vector3.zero, 0, false), ground);
                    if (UsesPaintedStrikes)
                    {
                        Vector3 direction = target.transform.position - _hero.transform.position;
                        slash.Transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 45);
                    }
                    break;
                case StrikeEffect.StaffBlast:
                    Vector3 feet = watch != null && watch.Root != null ? watch.Root.position : target.transform.position;
                    float radius = RingScale(weapon) * EffectArt.RingFullRadius / PixelSpriteFactory.PixelsPerUnit;
                    Effect impact = ShowMoving(_ring, StaffPulseDuration, StaffPulseDuration, feet, _groundOrder,
                        Vector3.zero, 0, false, radius * (24f / 38));
                    impact.StaffPulse = true;
                    impact.PulseRadius = radius;
                    break;
                default:
                    Effect cuts = StandOn(Show(_doubleSlash, _doubleSlashFrameDuration, anchor, _sortingOrder + StrikeOrder),
                        ground);
                    // Painted, the first cut leads away from the hero, as the painted sword slash turns to its target.
                    if (UsesPaintedFeedback)
                        cuts.Renderer.flipX = target.transform.position.x < _hero.transform.position.x;
                    if (critical)
                        StandOn(Show(_star, _starFrameDuration, anchor, _sortingOrder + StarOrder), ground);
                    break;
            }
        }

        // The ring sprite's full frame reaches RingFullRadius texels either side; scale it to the shown part of the splash.
        // The runtime's radius rather than the pattern's, so a ring widened by Area is drawn as wide as it strikes.
        private float RingScale(WeaponRuntime weapon)
        {
            float wantedTexels = weapon.SplashRadius * _ringSplashFraction * PixelSpriteFactory.PixelsPerUnit;
            return Mathf.Clamp(wantedTexels / EffectArt.RingFullRadius, MinRingScale, MaxRingScale);
        }

        // A heal shows its amount over the chest; gold pops coins and shows the sum in gold. The position is where the
        // chest stands on the floor, so its spark and coins stand there too.
        private void OnChestOpened(ChestReward reward, Vector3 position)
        {
            Vector3 anchor = position + Vector3.up * 0.5f;
            if (reward.Kind == ChestRewardKind.Heal)
            {
                ShowGroundNumber(Mathf.RoundToInt(_hero.Maximum * reward.HealFraction), false, anchor, position);
                StandOn(Show(_spark, SparkFrameDuration, anchor, _sortingOrder + SparkOrder), position);
                return;
            }
            _deathCount++;
            for (int i = 0; i < CoinCount; i++)
            {
                float spread = (i - (CoinCount - 1) * 0.5f) * CoinSpread + (PixelNoise.Value(i, _deathCount, CoinSeed) - 0.5f) * 0.4f;
                float launch = CoinLaunch + PixelNoise.Value(i, _deathCount, CoinSeed + 1) * 0.6f;
                StandOn(ShowMoving(_coin, _coinFrameDuration, _coinDuration, position, _sortingOrder + CoinOrder,
                    new Vector3(spread, launch, 0f), CoinGravity, true), position);
            }
            ShowGroundNumber(reward.Gold, true, anchor, position);
        }

        // A Crucible Vent eruption (docs/45) where the burning enemy stood: an ember ring as wide as its splash, a column of
        // flame from its feet and embers rising off it, both larger the more stacks it spent. The burst slots, so a Vent
        // through a crowd keeps every eruption beside the sparks, numbers and falls it causes.
        private void OnVented(Health enemy, int stacks)
        {
            if (enemy == null)
                return;
            EnemyWatch watch = FindWatch(enemy);
            Vector3 feet = FeetOf(watch, enemy);
            float radius = BurnRule.VentSplashRadius;
            Effect ring = ShowBurst(_ring, StaffPulseDuration, StaffPulseDuration, feet, _groundOrder,
                Vector3.zero, 0f, radius * (24f / 38), false);
            ring.StaffPulse = true;
            ring.PulseRadius = radius;
            ring.Tint = BurnTint;
            ring.Renderer.color = BurnTint;

            int spent = Mathf.Clamp(stacks, 1, BurnRule.MaxStacks);
            float scale = VentColumnScale + VentColumnScalePerStack * spent;
            StandOn(ShowBurst(_ventColumn, _ventColumnFrameDuration, _ventColumn.Length * _ventColumnFrameDuration, feet,
                _sortingOrder + StrikeOrder, Vector3.zero, 0f, scale, false), feet);
            _ventCount++;
            int clusters = spent >= 3 ? 2 : 1;
            for (int i = 0; i < clusters; i++)
            {
                // The second cluster mirrors the first and drifts the other way, so a big eruption sprays both sides.
                float side = i == 0 ? 1f : -1f;
                float drift = side * (.1f + .15f * PixelNoise.Value(i, _ventCount, VentEmberSeed));
                var velocity = new Vector3(drift, VentEmberRise + VentEmberRisePerStack * spent, 0f);
                Vector3 origin = feet + Vector3.up * (VentEmberLift * scale);
                Effect embers = StandOn(ShowBurst(_ventEmbers, _ventEmberFrameDuration, VentEmberDuration, origin,
                    _sortingOrder + StarOrder, velocity, 0f, VentEmberScale + VentEmberScalePerStack * spent, false), feet);
                embers.Fade = true;
                embers.Renderer.flipX = i == 1;
            }
        }

        // A burn tick: its enemy burns, so it carries a flame if it did not already.
        private void OnBurned(Health enemy, float damage) => _flames.Track(enemy, 0f);

        // Ember Memory passes a stack from the fallen to a neighbour: an ember hops from one body to the other over a low
        // arc, and the neighbour's flame shows as it lands, so the spread reads as cause and effect.
        private void OnBurnSpread(Health fallen, Health neighbour)
        {
            if (fallen == null || neighbour == null)
                return;
            Vector3 from = AnchorOf(FindWatch(fallen), fallen);
            EnemyWatch target = FindWatch(neighbour);
            Vector3 to = AnchorOf(target, neighbour);
            // Launched so the arc's apex stands SpreadArcHeight over the straight line and it lands on the body at the end.
            const float flight = SpreadFlightSeconds;
            const float gravity = 8f * SpreadArcHeight / (flight * flight);
            Vector3 velocity = (to - from) / flight + Vector3.up * (.5f * gravity * flight);
            Effect ember = StandOn(ShowBurst(_emberArc, SpreadFrameDuration, flight, from, _sortingOrder + SparkOrder,
                velocity, gravity, 1f, true), FeetOf(target, neighbour));
            ember.FaceMotion = true;
            _flames.Track(neighbour, flight);
        }

        // The burst's ring covers the ability's whole radius, unlike the staff's, which shows only part of its splash; the
        // painted Vent's cast rings it in burning ember instead of the ability's steel seal. The Forge Burst also sends a
        // shockwave out to that radius and flings forge sparks, painted with a flash at the hero; the Vent's own
        // eruptions are its blast.
        private void OnAbilityUsed()
        {
            float radius = _heroAbility.Ability.Radius;
            Vector3 feet = _hero.transform.position;
            AbilityKind kind = _heroAbility.Ability.Kind;
            bool ember = kind == AbilityKind.Vent && _ventCast != null;
            Effect pulse = ember
                ? ShowBurst(_ventCast, VentCastFrameDuration, .21f, feet, _groundOrder, Vector3.zero, 0f, radius * .6f, false)
                : ShowMoving(_abilityPulse, .21f, .21f, feet, _groundOrder, Vector3.zero, 0f, false, radius * .6f);
            pulse.AbilityPulse = true;
            pulse.PulseRadius = radius;
            pulse.Renderer.color = new Color(1f, 1f, 1f, .85f);
            if (kind != AbilityKind.Burst)
                return;

            Effect wave = ShowBurst(_shockwave, _shockwaveFrameDuration, _shockwave.Length * _shockwaveFrameDuration, feet,
                _groundOrder, Vector3.zero, 0f, ShockwaveStart * radius / _shockwaveSpriteRadius, false);
            wave.Shockwave = true;
            wave.PulseRadius = radius;
            if (_forgeFlash != null)
                StandOn(ShowBurst(_forgeFlash, ForgeFlashFrameDuration, _forgeFlash.Length * ForgeFlashFrameDuration,
                    feet + Vector3.up * ForgeFlashLift, _sortingOrder + SparkOrder, Vector3.zero, 0f, 1f, false), feet);
            _forgeBursts++;
            Vector3 origin = feet + Vector3.up * ForgeSparkLift;
            for (int i = 0; i < ForgeSparkCount; i++)
            {
                float angle = (i + (PixelNoise.Value(i, _forgeBursts, ForgeSparkSeed) - .5f) * .6f) * (2f * Mathf.PI / ForgeSparkCount);
                float speed = radius * (ForgeSparkSpeed + ForgeSparkSpeedSpread * PixelNoise.Value(i, _forgeBursts, ForgeSparkSeed + 1));
                // Out along the floor, which the camera sees at half its depth, with a pop upward that gravity takes back.
                var velocity = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed * .5f + ForgeSparkPop, 0f);
                Effect spark = StandOn(ShowBurst(_forgeSpark, _forgeSparkFrameDuration, ForgeSparkDuration, origin,
                    _sortingOrder + SparkOrder, velocity, ForgeSparkGravity, 1f, false), feet);
                spark.FaceMotion = true;
                spark.Fade = true;
            }
        }

        private void OnEnemyDefeated(Health enemy)
        {
            EnemyWatch watch = FindWatch(enemy);
            Vector3 anchor = AnchorOf(watch, enemy);
            // The collapse, the smoke and the swarm's coins all stand where the enemy fell.
            Vector3 ground = FeetOf(watch, enemy);
            // Remains outlive the actor without delaying death rewards or the next wave; fallback smoke stays small.
            var painted = watch != null && watch.Look != null ? watch.Look.PaintedArt : null;
            if (painted != null)
            {
                Effect remains = StandOn(ShowMoving(painted.Death, .16f, .4f, enemy.transform.position, _sortingOrder + SmokeOrder,
                    Vector3.zero, 0, false), ground);
                remains.Renderer.flipX = watch.Look.Mirrored;
                remains.Fade = true;
                remains.Sink = RemainsSink;
            }
            else StandOn(ShowMoving(_smoke, _smokeFrameDuration, _smoke.Length * _smokeFrameDuration, anchor, _sortingOrder + SmokeOrder,
                Vector3.up * SmokeRise, 0f, false, SmokeScale), ground);
            // Descent rewards now remain on the floor; its drop view is the collectible cue. Swarm pays immediately.
            if (!_encounter.IsInSwarmRoom || _encounter.GoldRewardOf(enemy) <= 0)
                return;

            _deathCount++;
            for (int i = 0; i < CoinCount; i++)
            {
                float spread = (i - (CoinCount - 1) * 0.5f) * CoinSpread + (PixelNoise.Value(i, _deathCount, CoinSeed) - 0.5f) * 0.4f;
                float launch = CoinLaunch + PixelNoise.Value(i, _deathCount, CoinSeed + 1) * 0.6f;
                StandOn(ShowMoving(_coin, _coinFrameDuration, _coinDuration, anchor, _sortingOrder + CoinOrder,
                    new Vector3(spread, launch, 0f), CoinGravity, true), ground);
            }
        }

        private void OnHeroDamaged(DamageContext context)
        {
            if (_shakeRemaining <= 0f)
            {
                _shakeRest = _camera.transform.position;
                _shakeOffset = Vector3.zero;
            }
            _shakeRemaining = _shakeDuration;
        }

        private Effect Show(Sprite[] frames, float frameDuration, Vector3 origin, int sortingOrder, float scale = 1f) =>
            ShowMoving(frames, frameDuration, frames.Length * frameDuration, origin, sortingOrder, Vector3.zero, 0f, false, scale);

        // Marks an effect as drawn above the floor, standing on a floor contact (world). The foundry uses it for height/depth.
        private static Effect StandOn(Effect effect, Vector3 ground)
        {
            effect.AboveFloor = true;
            effect.Ground = ground;
            return effect;
        }

        // Takes the next pool slot round robin, so when every slot is busy the oldest effect gives way.
        private Effect ShowMoving(Sprite[] frames, float frameDuration, float lifetime, Vector3 origin, int sortingOrder,
            Vector3 velocity, float gravity, bool loop, float scale = 1f)
        {
            Effect effect = _effects[_nextEffect];
            _nextEffect = (_nextEffect + 1) % _effectPoolSize;
            return Arm(effect, frames, frameDuration, lifetime, origin, sortingOrder, velocity, gravity, loop, scale);
        }

        // The skills' reserved slots (BurstSlots), round robin among themselves.
        private Effect ShowBurst(Sprite[] frames, float frameDuration, float lifetime, Vector3 origin, int sortingOrder,
            Vector3 velocity, float gravity, float scale, bool loop)
        {
            Effect effect = _effects[_effectPoolSize + PuffSlots + _nextBurst];
            _nextBurst = (_nextBurst + 1) % BurstSlots;
            return Arm(effect, frames, frameDuration, lifetime, origin, sortingOrder, velocity, gravity, loop, scale);
        }

        private static Effect Arm(Effect effect, Sprite[] frames, float frameDuration, float lifetime, Vector3 origin,
            int sortingOrder, Vector3 velocity, float gravity, bool loop, float scale)
        {
            effect.Frames = frames;
            effect.FrameDuration = frameDuration;
            effect.Lifetime = lifetime;
            effect.Elapsed = 0f;
            effect.Loop = loop;
            effect.Origin = origin;
            effect.Velocity = velocity;
            effect.Gravity = gravity;
            effect.Active = true;
            effect.Fade = false;
            effect.Tint = Color.white;
            effect.Sink = 0f;
            effect.AbilityPulse = false;
            effect.StaffPulse = false;
            effect.Shockwave = false;
            effect.PulseRadius = 0f;
            effect.FaceMotion = false;
            effect.AboveFloor = false;
            effect.Ground = origin;
            effect.Transform.position = origin;
            effect.Transform.localRotation = Quaternion.identity;
            effect.Transform.localScale = new Vector3(scale, scale, 1f);
            effect.Renderer.sprite = frames[0];
            effect.Renderer.sortingOrder = sortingOrder;
            effect.Renderer.color = Color.white;
            effect.Renderer.flipX = false;
            effect.Renderer.enabled = true;
            return effect;
        }

        private void ShowNumber(int value, bool critical, Vector3 origin) => ShowGroundNumber(value, critical, origin, origin);

        private void ShowGroundNumber(int value, bool critical, Vector3 origin, Vector3 ground)
        {
            Number number = _numbers[_nextNumber];
            _nextNumber = (_nextNumber + 1) % _numbers.Length;
            Sprite[] font = critical ? _critDigits : _digits;
            int count = EffectArt.DigitCount(value);
            float stride = critical ? _critDigitStride : _digitStride;
            // The row runs from the first glyph's left edge to the last one's right: a stride for each glyph after the
            // first, and one glyph's width.
            float width = (count - 1) * stride + (critical ? _critDigitWidth : _digitWidth);
            float left = -width * 0.5f;
            for (int i = 0; i < number.Digits.Length; i++)
            {
                SpriteRenderer digit = number.Digits[i];
                bool used = i < count;
                digit.enabled = used;
                if (!used)
                    continue;
                digit.sprite = font[EffectArt.DigitAt(value, count - 1 - i)];
                digit.transform.localPosition = new Vector3(left + i * stride, 0f, 0f);
                digit.color = Color.white;
            }
            number.DigitCount = count;
            number.Origin = origin;
            number.Ground = ground;
            number.Elapsed = 0f;
            number.Active = true;
            number.Transform.position = origin;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _effects.Length; i++)
            {
                Effect effect = _effects[i];
                if (!effect.Active)
                    continue;

                effect.Elapsed += deltaTime;
                if (effect.Elapsed >= effect.Lifetime)
                {
                    Hide(effect);
                    continue;
                }

                int frame = (int)(effect.Elapsed / effect.FrameDuration);
                frame = effect.Loop ? frame % effect.Frames.Length : Mathf.Min(frame, effect.Frames.Length - 1);
                Sprite sprite = effect.Frames[frame];
                if (!ReferenceEquals(effect.Renderer.sprite, sprite))
                    effect.Renderer.sprite = sprite;
                if (effect.AbilityPulse || effect.StaffPulse)
                {
                    float progress = Mathf.Clamp01(effect.Elapsed / .14f);
                    float radius = effect.PulseRadius * Mathf.Lerp(effect.StaffPulse ? 24f / 38 : .6f, 1f, 1f - (1f - progress) * (1f - progress));
                    effect.Transform.localScale = new Vector3(radius, radius, 1f);
                    // From its tint, so a Vent's ember ring stays ember as it fades; every other pulse's tint is white.
                    Color pulse = effect.Tint;
                    pulse.a *= (effect.StaffPulse ? 1f : .85f) * Mathf.Clamp01((effect.Lifetime - effect.Elapsed) / .11f);
                    effect.Renderer.color = pulse;
                }
                if (effect.Shockwave)
                {
                    float grow = Mathf.Clamp01(effect.Elapsed / ShockwaveGrowSeconds);
                    float reach = Mathf.Lerp(ShockwaveStart, 1f, 1f - (1f - grow) * (1f - grow));
                    float size = effect.PulseRadius / _shockwaveSpriteRadius * reach;
                    effect.Transform.localScale = new Vector3(size, size, 1f);
                    Color wave = effect.Tint;
                    wave.a *= Mathf.Clamp01((effect.Lifetime - effect.Elapsed) / .1f);
                    effect.Renderer.color = wave;
                }
                if (effect.Fade)
                {
                    float left = Mathf.Clamp01((effect.Lifetime - effect.Elapsed) / .16f);
                    Color tint = effect.Tint;
                    tint.a *= left;
                    effect.Renderer.color = tint;
                    if (effect.Sink != 0f)
                        effect.Transform.position = effect.Origin + Vector3.down * (effect.Sink * (1f - left));
                }
                if (effect.Gravity != 0f || effect.Velocity != Vector3.zero)
                {
                    float t = effect.Elapsed;
                    Vector3 offset = effect.Velocity * t;
                    offset.y -= 0.5f * effect.Gravity * t * t;
                    effect.Transform.position = effect.Origin + offset;
                    if (effect.FaceMotion)
                    {
                        float rising = effect.Velocity.y - effect.Gravity * t;
                        effect.Transform.localRotation = Quaternion.Euler(0f, 0f,
                            Mathf.Atan2(rising, effect.Velocity.x) * Mathf.Rad2Deg);
                    }
                }
            }

            for (int i = 0; i < _numbers.Length; i++)
            {
                Number number = _numbers[i];
                if (!number.Active)
                    continue;

                number.Elapsed += deltaTime;
                float t = number.Elapsed / _numberDuration;
                if (t >= 1f)
                {
                    Hide(number);
                    continue;
                }

                // Rises fast then eases out, and fades over its last part so the next hit's number stands out.
                float rise = 1f - (1f - t) * (1f - t);
                number.Transform.position = number.Origin + Vector3.up * (_numberRise * rise);
                float alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                for (int d = 0; d < number.DigitCount; d++)
                {
                    Color color = number.Digits[d].color;
                    color.a = alpha;
                    number.Digits[d].color = color;
                }
            }
        }

        // After the camera framing, which only moves the camera when the screen changes; the shake is measured from the
        // framing's own position each frame so a frame change mid-shake is kept, and the rest position is restored exactly.
        private void LateUpdate()
        {
            // After every Update has moved the enemies and lit, spent or put out their burns, so each flame stands on its
            // enemy in the frame it is drawn, and on scaled time, so a pause holds the flicker as it holds the burn.
            _flames.Tick(Time.deltaTime);
            if (_shakeRemaining <= 0f)
                return;

            Vector3 current = _camera.transform.position;
            if (current != _shakeRest + _shakeOffset)
                _shakeRest = current - _shakeOffset;

            _shakeRemaining -= Time.unscaledDeltaTime;
            if (_shakeRemaining <= 0f)
            {
                EndShake();
                return;
            }

            _shakeStep++;
            float strength = _shakeAmplitude * (_shakeRemaining / _shakeDuration);
            float x = PixelNoise.Value(_shakeStep, 0, ShakeSeed) < 0.5f ? -strength : strength;
            float y = PixelNoise.Value(_shakeStep, 1, ShakeSeed) < 0.5f ? -strength : strength;
            _shakeOffset = new Vector3(x, y, 0f);
            _camera.transform.position = _shakeRest + _shakeOffset;
        }

        private void EndShake()
        {
            if (_shakeRemaining <= 0f && _shakeOffset == Vector3.zero)
                return;
            _shakeRemaining = 0f;
            _shakeOffset = Vector3.zero;
            if (_camera != null)
                _camera.transform.position = _shakeRest;
        }

        private static void Hide(Effect effect)
        {
            effect.Active = false;
            effect.Renderer.enabled = false;
        }

        private static void Hide(Number number)
        {
            number.Active = false;
            for (int d = 0; d < number.Digits.Length; d++)
                number.Digits[d].enabled = false;
        }

        private void HideAll()
        {
            for (int i = 0; _effects != null && i < _effects.Length; i++)
                Hide(_effects[i]);
            for (int i = 0; _numbers != null && i < _numbers.Length; i++)
                Hide(_numbers[i]);
        }
    }
}
