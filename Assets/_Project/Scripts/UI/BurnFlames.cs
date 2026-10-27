using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // The flame a burning enemy carries (docs/45): one pooled renderer per burning enemy, its size and heat the burn's
    // stacks, flickering on scaled time so a pause holds it, following the enemy and gone the frame the burn goes out,
    // the enemy falls or its body is taken away. CombatEffectsView owns it and tells it who may have caught fire - the
    // hero's struck target, a burn tick, a spread - so it never searches the scene: a crowd of forty costs one pass over
    // its slots a frame and allocates nothing. Every frame of every size is cut from one sheet texture this owns, so the
    // crowd's flames share one texture and batch together.
    internal sealed class BurnFlames
    {
        private sealed class Flame
        {
            public Health Enemy;
            public BurnStatus Burn;
            public Transform Root;
            // From the enemy's feet to the flame's foot, a quarter of the way up its body.
            public Vector3 Offset;
            public float Phase;
            // Seconds before it shows: an enemy that caught a spread waits for the ember to land.
            public float Hidden;
            public float Alpha;
            public Transform Transform;
            public SpriteRenderer Renderer;
        }

        // Matches the hit spark's fallback for an enemy without a body renderer (CombatEffectsView.DefaultBodyHeight).
        private const float DefaultBodyHeight = .45f;
        // The flame's foot stands this share of the body's half height below its middle: a quarter of the way up.
        private const float FootShare = .5f;
        // The flame keeps one size on a body of this height in world units (the painted Grunt's frame), and grows or
        // shrinks with the body within these bounds, so a Mite is not buried and a Warden's burn still reads.
        private const float ReferenceBodyHeight = 1.05f;
        private const float MinScale = .7f;
        private const float MaxScale = 1.3f;
        private const float FrameSeconds = .09f;
        private const float CycleSeconds = FrameSeconds * BurnArt.FlameFrameCount;
        // The last moments of a burn gutter out over this long rather than blink off.
        private const float GutterSeconds = .3f;
        private const int PhaseSeed = 53;
        // Hero strikes waiting for the end of the frame: Ember Memory lights its target in the same event, possibly after
        // this view heard it. More in one frame than this wait for the burn's first tick instead.
        private const int PendingCapacity = 16;

        private readonly Flame[] _flames;
        private readonly Health[] _pending = new Health[PendingCapacity];
        // [stacks - 1][frame]
        private readonly Sprite[][] _frames;
        private readonly Texture2D _sheet;
        private int _pendingCount;
        private int _tracked;
        private float _clock;

        public BurnFlames(Transform parent, Material material, int sortingOrder, int capacity)
        {
            _sheet = PixelSpriteFactory.CreateTexture(BurnArt.FlameSheet(), "VFX Burn Flames");
            _frames = new Sprite[BurnRule.MaxStacks][];
            for (int stacks = 1; stacks <= BurnRule.MaxStacks; stacks++)
            {
                _frames[stacks - 1] = new Sprite[BurnArt.FlameFrameCount];
                for (int frame = 0; frame < BurnArt.FlameFrameCount; frame++)
                {
                    BurnArt.FlameCell(stacks, frame, out int x, out int y);
                    var rect = new Rect(x, y, BurnArt.FlameWidth(stacks), BurnArt.FlameHeight(stacks));
                    Sprite sprite = Sprite.Create(_sheet, rect, PixelSpriteFactory.BottomCentre, BurnArt.TexelsPerUnit, 0,
                        SpriteMeshType.FullRect);
                    sprite.name = $"VFX Burn Flame {stacks}-{frame}";
                    _frames[stacks - 1][frame] = sprite;
                }
            }

            var root = new GameObject("Burn Flames").transform;
            root.SetParent(parent, false);
            _flames = new Flame[capacity];
            for (int i = 0; i < _flames.Length; i++)
            {
                var child = new GameObject($"Flame {i}");
                child.transform.SetParent(root, false);
                var renderer = child.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = material;
                renderer.sortingOrder = sortingOrder;
                renderer.enabled = false;
                _flames[i] = new Flame { Transform = child.transform, Renderer = renderer };
            }
        }

        public int SlotCount => _flames.Length;

        public int ShownCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _flames.Length; i++)
                {
                    if (_flames[i].Renderer.enabled)
                        count++;
                }
                return count;
            }
        }

        // The flame on show on this enemy, or null: none, not yet landed, or gone out.
        public SpriteRenderer On(Health enemy)
        {
            for (int i = 0; i < _flames.Length; i++)
            {
                Flame flame = _flames[i];
                if (ReferenceEquals(flame.Enemy, enemy))
                    return flame.Renderer.enabled ? flame.Renderer : null;
            }
            return null;
        }

        // The flame of a slot on show, standing on its enemy's feet, for the foundry's depth (FoundryVisibilityView).
        public Transform Surface(int slot, out Vector3 ground)
        {
            Flame flame = _flames[slot];
            bool shown = flame.Renderer.enabled && flame.Root != null;
            ground = shown ? flame.Root.position : Vector3.zero;
            return shown ? flame.Transform : null;
        }

        // The hero struck this enemy; whether it burns is known once the frame's strike events have all run.
        public void Consider(Health enemy)
        {
            if (enemy != null && _pendingCount < _pending.Length)
                _pending[_pendingCount++] = enemy;
        }

        // After every Update has moved the enemies and lit, spent or put out their burns, so each flame stands on its
        // enemy in the frame it is drawn.
        public void Tick(float deltaTime)
        {
            _clock += deltaTime;
            if (_clock >= CycleSeconds)
                _clock -= CycleSeconds * Mathf.Floor(_clock / CycleSeconds);
            for (int i = 0; i < _pendingCount; i++)
            {
                Track(_pending[i], 0f);
                _pending[i] = null;
            }
            _pendingCount = 0;

            for (int i = 0; i < _flames.Length; i++)
            {
                Flame flame = _flames[i];
                if (ReferenceEquals(flame.Enemy, null))
                    continue;
                // A destroyed enemy or burn compares equal to null here, so a retired body ends its flame too.
                if (flame.Enemy == null || !flame.Enemy.IsAlive || flame.Burn == null || !flame.Burn.IsBurning ||
                    flame.Root == null || !flame.Root.gameObject.activeInHierarchy)
                {
                    Release(flame);
                    continue;
                }
                if (flame.Hidden > 0f)
                {
                    flame.Hidden -= deltaTime;
                    if (flame.Hidden > 0f)
                        continue;
                }

                int stacks = Mathf.Clamp(flame.Burn.Stacks, 1, BurnRule.MaxStacks);
                float time = _clock + flame.Phase;
                int frame = (int)(time / FrameSeconds) % BurnArt.FlameFrameCount;
                Sprite sprite = _frames[stacks - 1][frame];
                if (!ReferenceEquals(flame.Renderer.sprite, sprite))
                    flame.Renderer.sprite = sprite;
                float alpha = Mathf.Clamp01(flame.Burn.State.Remaining / GutterSeconds);
                if (alpha != flame.Alpha)
                {
                    flame.Alpha = alpha;
                    flame.Renderer.color = new Color(1f, 1f, 1f, alpha);
                }
                flame.Transform.position = flame.Root.position + flame.Offset;
                if (!flame.Renderer.enabled)
                    flame.Renderer.enabled = true;
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _flames.Length; i++)
                Release(_flames[i]);
            for (int i = 0; i < _pendingCount; i++)
                _pending[i] = null;
            _pendingCount = 0;
        }

        // The sprites are cut from the one sheet, so each sprite goes and the sheet goes once.
        public void Dispose()
        {
            Clear();
            for (int stacks = 0; stacks < _frames.Length; stacks++)
            {
                for (int frame = 0; frame < _frames[stacks].Length; frame++)
                    Object.Destroy(_frames[stacks][frame]);
            }
            if (_sheet != null)
                Object.Destroy(_sheet);
        }

        // This enemy may burn now (a tick, a spread caught, a strike); if it does, its flame shows after delay seconds. An
        // enemy already carrying one keeps it as it is.
        public void Track(Health enemy, float delay)
        {
            if (enemy == null || !enemy.IsAlive)
                return;
            Flame free = null;
            for (int i = 0; i < _flames.Length; i++)
            {
                Flame flame = _flames[i];
                if (ReferenceEquals(flame.Enemy, enemy))
                    return;
                if (free == null && ReferenceEquals(flame.Enemy, null))
                    free = flame;
            }
            // Every renderer busy: the enemy's next burn tick asks again.
            if (free == null)
                return;
            // TryGetComponent: every Vanguard hit is considered too, and a failed GetComponent allocates in the Editor.
            if (!enemy.TryGetComponent(out BurnStatus burn) || !burn.IsBurning)
                return;

            free.Enemy = enemy;
            free.Burn = burn;
            free.Root = enemy.transform;
            free.Hidden = delay;
            free.Alpha = -1f;
            free.Phase = PixelNoise.Value(_tracked++, 0, PhaseSeed) * CycleSeconds;
            Place(free);
        }

        // Where on the body the flame stands and how large it is, from the body as it is now: the look's own renderer
        // when the enemy has one, as the hit sparks find it, and only its place on screen, not the foundry's depth.
        private static void Place(Flame flame)
        {
            SpriteRenderer body = flame.Enemy.TryGetComponent(out EnemyLookView look) && look.Body != null
                ? look.Body : flame.Enemy.GetComponentInChildren<SpriteRenderer>();
            Vector3 offset;
            float height;
            if (body != null)
            {
                Bounds bounds = body.bounds;
                offset = bounds.center - flame.Root.position;
                offset.y -= bounds.extents.y * FootShare;
                height = bounds.size.y;
            }
            else
            {
                offset = Vector3.up * (DefaultBodyHeight * FootShare);
                height = ReferenceBodyHeight;
            }
            offset.z = 0f;
            flame.Offset = offset;
            float scale = Mathf.Clamp(height / ReferenceBodyHeight, MinScale, MaxScale);
            flame.Transform.localScale = new Vector3(scale, scale, 1f);
            flame.Transform.position = flame.Root.position + offset;
        }

        private static void Release(Flame flame)
        {
            flame.Enemy = null;
            flame.Burn = null;
            flame.Root = null;
            flame.Hidden = 0f;
            flame.Renderer.enabled = false;
        }
    }
}
