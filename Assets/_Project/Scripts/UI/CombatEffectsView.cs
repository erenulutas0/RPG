using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // Combat effects generated from code in the astral foundry style: the hero's strike at the enemy it hit, an ember
    // spark and a floating damage number on every enemy hit, smoke and coins when an enemy dies, and a small camera shake
    // when the hero is hurt. Every sprite is drawn once in Awake and every effect object is pooled, so combat never
    // allocates. Effects run on scaled time so a pause freezes them; the shake runs on unscaled time so it always settles.
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
        }

        private sealed class Number
        {
            public Transform Transform;
            public SpriteRenderer[] Digits;
            public int DigitCount;
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

            public EnemyWatch(CombatEffectsView view) => _view = view;

            public void OnDamaged(DamageContext context) => _view.OnEnemyDamaged(this, context);
        }

        // Where effects land on an enemy whose body has no renderer yet, above its feet.
        private const float DefaultBodyHeight = 0.45f;
        private const float NumberLift = 0.25f;
        private const float NumberJitter = 0.35f;
        private const float StrikeFrameDuration = 0.05f;
        private const float RingFrameDuration = 0.07f;
        private const float StarFrameDuration = 0.07f;
        private const float SparkFrameDuration = 0.06f;
        private const float SmokeFrameDuration = 0.12f;
        private const float SmokeRise = 0.4f;
        private const float SmokeScale = 0.55f;
        private const float CoinFrameDuration = 0.08f;
        private const float CoinGravity = 9.8f;
        private const float CoinLaunch = 2.4f;
        private const float CoinSpread = 0.9f;
        private const int CoinCount = 3;
        private const float MinRingScale = 0.75f;
        private const float MaxRingScale = 1.5f;
        // Sorting orders above the base: the floor ring lowest, coins highest.
        private const int RingOrder = 0;
        private const int SparkOrder = 1;
        private const int SmokeOrder = 1;
        private const int StrikeOrder = 2;
        private const int StarOrder = 3;
        private const int CoinOrder = 4;
        private const int NumberSeed = 41;
        private const int CoinSeed = 43;
        private const int ShakeSeed = 47;

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
        private Sprite[] _slash;
        private Sprite[] _ring;
        private Sprite[] _doubleSlash;
        private Sprite[] _star;
        private Sprite[] _spark;
        private Sprite[] _smoke;
        private Sprite[] _coin;
        private Sprite[] _digits;
        private Sprite[] _critDigits;
        private GameObject _poolRoot;
        private Effect[] _effects;
        private Number[] _numbers;
        private EnemyWatch[] _watches;
        private int _watchCount;
        private int _nextEffect;
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
            _ready = true;
        }

        private void OnEnable()
        {
            if (!_ready)
                return;

            _encounter.EncounterStarted += OnEncounterStarted;
            _encounter.EnemyDefeated += OnEnemyDefeated;
            _heroAttack.Struck += OnHeroStruck;
            _hero.Damaged += OnHeroDamaged;
            if (_heroAbility != null)
                _heroAbility.Used += OnAbilityUsed;
            if (_chests != null)
                _chests.Opened += OnChestOpened;
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
                _heroAttack.Struck -= OnHeroStruck;
                _hero.Damaged -= OnHeroDamaged;
                if (_heroAbility != null)
                    _heroAbility.Used -= OnAbilityUsed;
                if (_chests != null)
                    _chests.Opened -= OnChestOpened;
                _subscribed = false;
            }
            ReleaseWave();
            HideAll();
            EndShake();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _sprites.Count; i++)
                PixelSpriteFactory.Destroy(_sprites[i]);
            _sprites.Clear();
            if (_poolRoot != null)
                Destroy(_poolRoot);
        }

        private void BuildSprites()
        {
            _slash = Build(EffectArt.SlashFrames(), "VFX Slash", PixelSpriteFactory.Centre);
            _ring = Build(EffectArt.RingFrames(), "VFX Blast Ring", PixelSpriteFactory.Centre);
            _doubleSlash = Build(EffectArt.DoubleSlashFrames(), "VFX Double Slash", PixelSpriteFactory.Centre);
            _star = Build(EffectArt.StarBurstFrames(), "VFX Crit Star", PixelSpriteFactory.Centre);
            _spark = Build(EffectArt.SparkFrames(), "VFX Spark", PixelSpriteFactory.Centre);
            _smoke = Build(EffectArt.SmokeFrames(), "VFX Smoke", PixelSpriteFactory.Centre);
            _coin = Build(EffectArt.CoinFrames(), "VFX Coin", PixelSpriteFactory.Centre);

            // Numerals pivot at their bottom-left corner so a number is laid out left to right by stride.
            var digits = new PixelCanvas[10];
            var critDigits = new PixelCanvas[10];
            for (int digit = 0; digit < 10; digit++)
            {
                digits[digit] = EffectArt.DigitCanvas(digit, Rgba.White);
                critDigits[digit] = EffectArt.DigitCanvas(digit, PixelPalette.CoinLight, EffectArt.CritNumberScale);
            }
            _digits = Build(digits, "VFX Digit", Vector2.zero);
            _critDigits = Build(critDigits, "VFX Crit Digit", Vector2.zero);
        }

        private Sprite[] Build(PixelCanvas[] canvases, string name, Vector2 pivot)
        {
            var sprites = new Sprite[canvases.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = PixelSpriteFactory.CreateSprite(canvases[i], $"{name} {i}", pivot);
                _sprites.Add(sprites[i]);
            }
            return sprites;
        }

        private void BuildPools()
        {
            _poolRoot = new GameObject("Combat Effects");
            _poolRoot.transform.SetParent(transform, false);

            _effects = new Effect[_effectPoolSize];
            for (int i = 0; i < _effects.Length; i++)
            {
                SpriteRenderer renderer = CreateRenderer($"Effect {i}", _poolRoot.transform, _sortingOrder);
                _effects[i] = new Effect { Transform = renderer.transform, Renderer = renderer };
            }

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
                EnemyWatch watch = _watches[i];
                if (enemy == null)
                    continue;

                watch.Enemy = enemy;
                watch.Root = enemy.transform;
                // Effects land at the middle of the body, whatever look the enemy was given.
                SpriteRenderer body = enemy.GetComponentInChildren<SpriteRenderer>();
                watch.BodyOffset = body != null ? body.bounds.center - watch.Root.position : Vector3.up * DefaultBodyHeight;
                enemy.Damaged += watch.OnDamaged;
            }
            _watchCount = count;
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
            return null;
        }

        private Vector3 AnchorOf(EnemyWatch watch, Health enemy)
        {
            if (watch != null && watch.Root != null)
                return watch.Root.position + watch.BodyOffset;
            return enemy != null ? enemy.transform.position + Vector3.up * DefaultBodyHeight : Vector3.zero;
        }

        private void OnEnemyDamaged(EnemyWatch watch, DamageContext context)
        {
            Vector3 anchor = AnchorOf(watch, watch.Enemy);
            Show(_spark, SparkFrameDuration, anchor, _sortingOrder + SparkOrder);

            bool fromHero = ReferenceEquals(context.Source, _hero);
            if (fromHero && context.IsCritical)
                _critPending = true;

            int amount = Mathf.RoundToInt(context.Amount);
            if (amount <= 0)
                return;

            _hitCount++;
            float jitter = (PixelNoise.Value(_hitCount, 0, NumberSeed) - 0.5f) * NumberJitter;
            ShowNumber(amount, context.IsCritical, anchor + new Vector3(jitter, NumberLift, 0f));
        }

        private void OnHeroStruck(Health target)
        {
            bool critical = _critPending;
            _critPending = false;
            WeaponRuntime weapon = _heroAttack.Weapon;
            if (weapon == null || target == null)
                return;

            EnemyWatch watch = FindWatch(target);
            Vector3 anchor = AnchorOf(watch, target);
            switch (EffectArt.StrikeFor(weapon.Pattern.Behavior))
            {
                case StrikeEffect.SwordSlash:
                    Show(_slash, StrikeFrameDuration, anchor, _sortingOrder + StrikeOrder);
                    break;
                case StrikeEffect.StaffBlast:
                    Vector3 feet = watch != null && watch.Root != null ? watch.Root.position : target.transform.position;
                    Show(_ring, RingFrameDuration, feet, _sortingOrder + RingOrder, RingScale(weapon));
                    break;
                default:
                    Show(_doubleSlash, StrikeFrameDuration, anchor, _sortingOrder + StrikeOrder);
                    if (critical)
                        Show(_star, StarFrameDuration, anchor, _sortingOrder + StarOrder);
                    break;
            }
        }

        // The ring sprite's full frame reaches RingFullRadius texels either side; scale it to the shown part of the splash.
        private float RingScale(WeaponRuntime weapon)
        {
            float wantedTexels = weapon.Pattern.SplashRadius * _ringSplashFraction * PixelSpriteFactory.PixelsPerUnit;
            return Mathf.Clamp(wantedTexels / EffectArt.RingFullRadius, MinRingScale, MaxRingScale);
        }

        // A heal shows its amount over the chest; gold pops coins and shows the sum in gold.
        private void OnChestOpened(ChestReward reward, Vector3 position)
        {
            Vector3 anchor = position + Vector3.up * 0.5f;
            if (reward.Kind == ChestRewardKind.Heal)
            {
                ShowNumber(Mathf.RoundToInt(_hero.Maximum * reward.HealFraction), false, anchor);
                Show(_spark, SparkFrameDuration, anchor, _sortingOrder + SparkOrder);
                return;
            }
            _deathCount++;
            for (int i = 0; i < CoinCount; i++)
            {
                float spread = (i - (CoinCount - 1) * 0.5f) * CoinSpread + (PixelNoise.Value(i, _deathCount, CoinSeed) - 0.5f) * 0.4f;
                float launch = CoinLaunch + PixelNoise.Value(i, _deathCount, CoinSeed + 1) * 0.6f;
                ShowMoving(_coin, CoinFrameDuration, _coinDuration, position, _sortingOrder + CoinOrder,
                    new Vector3(spread, launch, 0f), CoinGravity, true);
            }
            ShowNumber(reward.Gold, true, anchor);
        }

        // The burst's ring covers the ability's whole radius, unlike the staff's, which shows only part of its splash.
        private void OnAbilityUsed()
        {
            float scale = _heroAbility.Ability.Radius * PixelSpriteFactory.PixelsPerUnit / EffectArt.RingFullRadius;
            Show(_ring, RingFrameDuration, _hero.transform.position, _sortingOrder + RingOrder, scale);
        }

        private void OnEnemyDefeated(Health enemy)
        {
            EnemyWatch watch = FindWatch(enemy);
            Vector3 anchor = AnchorOf(watch, enemy);
            // Smaller than the fallen enemy, so the puff never hides the hero standing beside it.
            ShowMoving(_smoke, SmokeFrameDuration, _smoke.Length * SmokeFrameDuration, anchor, _sortingOrder + SmokeOrder,
                Vector3.up * SmokeRise, 0f, false, SmokeScale);
            if (_encounter.GoldRewardOf(enemy) <= 0)
                return;

            _deathCount++;
            for (int i = 0; i < CoinCount; i++)
            {
                float spread = (i - (CoinCount - 1) * 0.5f) * CoinSpread + (PixelNoise.Value(i, _deathCount, CoinSeed) - 0.5f) * 0.4f;
                float launch = CoinLaunch + PixelNoise.Value(i, _deathCount, CoinSeed + 1) * 0.6f;
                ShowMoving(_coin, CoinFrameDuration, _coinDuration, anchor, _sortingOrder + CoinOrder,
                    new Vector3(spread, launch, 0f), CoinGravity, true);
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

        private void Show(Sprite[] frames, float frameDuration, Vector3 origin, int sortingOrder, float scale = 1f) =>
            ShowMoving(frames, frameDuration, frames.Length * frameDuration, origin, sortingOrder, Vector3.zero, 0f, false, scale);

        // Takes the next pool slot round robin, so when every slot is busy the oldest effect gives way.
        private void ShowMoving(Sprite[] frames, float frameDuration, float lifetime, Vector3 origin, int sortingOrder,
            Vector3 velocity, float gravity, bool loop, float scale = 1f)
        {
            Effect effect = _effects[_nextEffect];
            _nextEffect = (_nextEffect + 1) % _effects.Length;
            effect.Frames = frames;
            effect.FrameDuration = frameDuration;
            effect.Lifetime = lifetime;
            effect.Elapsed = 0f;
            effect.Loop = loop;
            effect.Origin = origin;
            effect.Velocity = velocity;
            effect.Gravity = gravity;
            effect.Active = true;
            effect.Transform.position = origin;
            effect.Transform.localScale = new Vector3(scale, scale, 1f);
            effect.Renderer.sprite = frames[0];
            effect.Renderer.sortingOrder = sortingOrder;
            effect.Renderer.color = Color.white;
            effect.Renderer.enabled = true;
        }

        private void ShowNumber(int value, bool critical, Vector3 origin)
        {
            Number number = _numbers[_nextNumber];
            _nextNumber = (_nextNumber + 1) % _numbers.Length;
            Sprite[] font = critical ? _critDigits : _digits;
            int scale = critical ? EffectArt.CritNumberScale : 1;
            int count = EffectArt.DigitCount(value);
            float stride = EffectArt.DigitStride * scale / PixelSpriteFactory.PixelsPerUnit;
            // Outlined numerals overlap their outlines at the stride, so the row is the bare width plus one outline each side.
            float width = (EffectArt.NumberWidth(count) + 2) * scale / PixelSpriteFactory.PixelsPerUnit;
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
                if (effect.Gravity != 0f || effect.Velocity != Vector3.zero)
                {
                    float t = effect.Elapsed;
                    Vector3 offset = effect.Velocity * t;
                    offset.y -= 0.5f * effect.Gravity * t * t;
                    effect.Transform.position = effect.Origin + offset;
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
