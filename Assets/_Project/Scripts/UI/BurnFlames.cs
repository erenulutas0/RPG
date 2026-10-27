using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // The flame a burning enemy carries (docs/45): one pooled renderer per burning enemy, its size and heat the burn's
    // stacks, flickering on scaled time so a pause holds it, following the enemy and gone the frame the burn goes out,
    // the enemy falls or its body is taken away. CombatEffectsView owns it and tells it who may have caught fire - the
    // hero's struck target, a burn tick, a spread - so it never searches the scene: a crowd of forty costs one pass over
    // its slots a frame and allocates nothing. Every frame of every size is cut from one sheet texture, so the crowd's
    // flames share one texture and batch together: the painted sheet of the skills' art set when the scene gives one
    // (borrowed), else BurnArt's pixel sheet (owned). The painted flame frames its body from the feet up - tongues out of
    // the shoulders and flanks, a crown behind the head - where the pixel one stands on its belly.
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
        // The painted flame was laid out round a body as tall as the painted Grunt stands (ReferenceBodyHeight, its art
        // set's VisualTop) and scales with the body it burns on, from the Mite's size to a Warden's.
        private const float PaintedMinScale = .56f;
        private const float PaintedMaxScale = 1.5f;
        // Its loop is longer and smoother than the pixel one's: more frames at about the same rate.
        private const float PaintedFrameSeconds = .07f;
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
        // The pixel sheet, which this owns; null while the frames are cut from the painted sheet, which it borrows.
        private readonly Texture2D _sheet;
        private readonly bool _painted;
        private readonly float _frameSeconds;
        private readonly float _cycleSeconds;
        private int _pendingCount;
        private int _tracked;
        private float _clock;

        // painted: the skills' art set, or null for BurnArt's pixel flames.
        public BurnFlames(Transform parent, Material material, int sortingOrder, int capacity, SkillEffectArtSet painted)
        {
            _painted = painted != null && painted.IsValid;
            _frames = new Sprite[BurnRule.MaxStacks][];
            if (_painted)
            {
                // Named as the pixel frames are, "VFX Burn Flame {stacks}-{frame}".
                for (int stacks = 1; stacks <= BurnRule.MaxStacks; stacks++)
                    _frames[stacks - 1] = painted.Flames(stacks).Cut($"VFX Burn Flame {stacks}-");
                _frameSeconds = PaintedFrameSeconds;
            }
            else
            {
                _sheet = PixelSpriteFactory.CreateTexture(BurnArt.FlameSheet(), "VFX Burn Flames");
                for (int stacks = 1; stacks <= BurnRule.MaxStacks; stacks++)
                {
                    _frames[stacks - 1] = new Sprite[BurnArt.FlameFrameCount];
                    for (int frame = 0; frame < BurnArt.FlameFrameCount; frame++)
                    {
                        BurnArt.FlameCell(stacks, frame, out int x, out int y);
                        var rect = new Rect(x, y, BurnArt.FlameWidth(stacks), BurnArt.FlameHeight(stacks));
                        Sprite sprite = Sprite.Create(_sheet, rect, PixelSpriteFactory.BottomCentre, BurnArt.TexelsPerUnit,
                            0, SpriteMeshType.FullRect);
                        sprite.name = $"VFX Burn Flame {stacks}-{frame}";
                        _frames[stacks - 1][frame] = sprite;
                    }
                }
                _frameSeconds = FrameSeconds;
            }
            _cycleSeconds = _frameSeconds * _frames[0].Length;

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

        public bool IsPainted => _painted;

        // Every frame of the flicker at this many stacks, for the tests.
        public Sprite[] Frames(int stacks) => _frames[stacks - 1];

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
            if (_clock >= _cycleSeconds)
                _clock -= _cycleSeconds * Mathf.Floor(_clock / _cycleSeconds);
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
                Sprite[] frames = _frames[stacks - 1];
                Sprite sprite = frames[(int)(time / _frameSeconds) % frames.Length];
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

        // The sprites are cut from the one sheet, so each sprite goes and the sheet goes once - the pixel sheet only, as
        // the painted one is the art set's.
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
            free.Phase = PixelNoise.Value(_tracked++, 0, PhaseSeed) * _cycleSeconds;
            if (_painted)
                PlacePainted(free);
            else
                Place(free);
        }

        // Where on the body the flame stands and how large it is, from the body as it is now: the look's own renderer
        // when the enemy has one, as the hit sparks find it, and only its place on screen, not the foundry's depth.
        private static void Place(Flame flame)
        {
            SpriteRenderer body = BodyOf(flame.Enemy, out _);
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

        // The painted flame pins at the enemy's feet under the middle of its body and is sized by how tall the body
        // stands: a painted look's own measure of that (its art set's VisualTop, which its bar hangs from) rather than
        // its sprite rect, which leaves the Grunt room to strike in; a drawn look's sprite top, drawn to its size.
        private static void PlacePainted(Flame flame)
        {
            SpriteRenderer body = BodyOf(flame.Enemy, out EnemyLookView look);
            Vector3 offset = Vector3.zero;
            float top = ReferenceBodyHeight;
            if (body != null)
            {
                Bounds bounds = body.bounds;
                offset.x = bounds.center.x - flame.Root.position.x;
                EnemyArtSet painted = look != null ? look.PaintedArt : null;
                top = painted != null ? painted.VisualTop : bounds.max.y - flame.Root.position.y;
            }
            flame.Offset = offset;
            float scale = Mathf.Clamp(top / ReferenceBodyHeight, PaintedMinScale, PaintedMaxScale);
            flame.Transform.localScale = new Vector3(scale, scale, 1f);
            flame.Transform.position = flame.Root.position + offset;
        }

        private static SpriteRenderer BodyOf(Health enemy, out EnemyLookView look) =>
            enemy.TryGetComponent(out look) && look.Body != null ? look.Body : enemy.GetComponentInChildren<SpriteRenderer>();

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
