using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Optional imported cavern plate behind the platform. Without it, the procedural astral-void fallback uses a
    // violet gradient with nebula glows as a vertex-colour mesh (the lowest thing
    // drawn, SkyRenderer), then pixel sprites for the haze clouds, the spiral galaxy, two star layers, and the floating
    // islands with their orbital rings, chains, lantern flames and drifting rubble, all placed by VoidLayout. Everything
    // borrows session-owned sprites in Build; its small sky mesh and animation state remain local. Update moves
    // transforms, swaps pre-built sprites and tints renderers, so nothing
    // allocates per frame and Time.timeScale = 0 freezes the drift and the twinkle.
    [DefaultExecutionOrder(20)]
    public sealed class VoidBackdropView : MonoBehaviour
    {
        private const float DriftAmplitude = 0.05f;
        private const float DriftPeriod = 8f;
        private const float TwinklePeriod = 2.4f;
        // Sparkle frames swap a little more than once a second; lantern flames flicker at seven swaps a second.
        private const float SparkleRate = 1.4f;
        private const float FlickerRate = 7f;
        private const float GlowRate = 5.3f;
        private const float TwoPi = Mathf.PI * 2f;

        private struct Drifter
        {
            public Transform Transform;
            public Vector3 Base;
            public float Phase;
        }

        private struct Flicker
        {
            public SpriteRenderer Renderer;
            public Sprite Lit;
            public Sprite Dim;
            public float Phase;
            public bool ShowingLit;
        }

        private Mesh _mesh;
        private Camera _camera;
        public SpriteRenderer CavernRenderer { get; private set; }
        private Drifter[] _drifters;
        private Flicker[] _sparkles;
        private Flicker[] _lanterns;
        private SpriteRenderer[] _starLayers;

        // The sky mesh: the lowest renderer of the scene, at the sorting order Build was given.
        public MeshRenderer SkyRenderer { get; private set; }

        // Builds the backdrop once; sortingOrder is the lowest order in the scene and the sprites use the four orders
        // above it (haze and galaxy, stars and back ring halves, islands, front ring halves and flames).
        public void Build(ArenaGeometry geometry, Material material, int sortingOrder, Sprite cavern = null, Camera camera = null)
        {
            if (material == null)
            {
                Debug.LogError("VoidBackdropView needs a vertex-colour material for its sky.", this);
                enabled = false;
                return;
            }

            BuildSky(material, sortingOrder);

            if (cavern != null && camera != null)
            {
                _camera = camera;
                CavernRenderer = AddRenderer(transform, "Furnace Cavern", cavern, Vector3.zero, sortingOrder + 1);
                FrameCavern();
                return; // Imported art is asset-owned; do not generate the unused procedural backdrop.
            }

            VoidSprites sprites = ArenaSpriteCache.Backdrop;
            VoidScene scene = sprites.Scene;
            int hazeOrder = sortingOrder + 1;
            int starOrder = sortingOrder + 2;
            int propOrder = sortingOrder + 3;
            int frontOrder = sortingOrder + 4;

            for (int i = 0; i < scene.Hazes.Length; i++)
            {
                HazePlacement haze = scene.Hazes[i];
                Sprite sprite = sprites.Haze(i);
                AddRenderer(transform, "Haze " + i, sprite, new Vector3(haze.X, haze.Y, 0f), hazeOrder);
            }
            GalaxyPlacement galaxy = scene.Galaxy;
            AddRenderer(transform, "Galaxy", sprites.Galaxy, new Vector3(galaxy.X, galaxy.Y, 0f), hazeOrder);

            // The star field hangs from its top so it always sorts first among the star-order sprites.
            _starLayers = new SpriteRenderer[2];
            var starTop = new Vector3(VoidScene.StarFieldLeft + VoidScene.StarFieldWidth / 2f, VoidScene.StarFieldBottom + VoidScene.StarFieldHeight, 0f);
            for (int layer = 0; layer < _starLayers.Length; layer++)
            {
                Sprite sprite = sprites.Stars(layer);
                _starLayers[layer] = AddRenderer(transform, "Stars " + layer, sprite, starTop, starOrder);
            }

            Sprite sparkleLit = sprites.SparkleLit;
            Sprite sparkleDim = sprites.SparkleDim;
            _sparkles = new Flicker[scene.Sparkles.Length];
            for (int i = 0; i < _sparkles.Length; i++)
            {
                SparklePlacement sparkle = scene.Sparkles[i];
                SpriteRenderer renderer = AddRenderer(transform, "Sparkle " + i, sparkleLit, new Vector3(sparkle.X, sparkle.Y, 0f), frontOrder);
                _sparkles[i] = new Flicker { Renderer = renderer, Lit = sparkleLit, Dim = sparkleDim, Phase = sparkle.Phase, ShowingLit = true };
            }

            Sprite flameTall = sprites.FlameTall;
            Sprite flameLean = sprites.FlameLean;
            _drifters = new Drifter[scene.Islands.Length + scene.Rubble.Length];
            _lanterns = new Flicker[scene.Islands.Length];
            for (int i = 0; i < scene.Islands.Length; i++)
            {
                IslandPlacement placement = scene.Islands[i];
                IslandSprites art = sprites.Island(i);
                Transform root = NewChild(transform, "Island " + i, new Vector3(placement.X, placement.Y, 0f));
                _drifters[i] = new Drifter { Transform = root, Base = root.localPosition, Phase = placement.DriftPhase };

                if (placement.HasRing)
                {
                    AddRenderer(root, "Ring Back", art.RingBack, art.RingAnchor, starOrder);
                    AddRenderer(root, "Ring Front", art.RingFront, art.RingAnchor, frontOrder);
                }
                AddRenderer(root, "Rock", art.Rock, Vector3.zero, propOrder);
                for (int c = 0; c < placement.Chains.Length; c++)
                {
                    AddRenderer(root, "Chain " + c, art.Chain(c), art.ChainAnchor(c), propOrder);
                }
                SpriteRenderer flame = AddRenderer(root, "Lantern", flameTall, art.FlameAnchor, frontOrder);
                _lanterns[i] = new Flicker { Renderer = flame, Lit = flameTall, Dim = flameLean, Phase = placement.DriftPhase * 0.37f, ShowingLit = true };
            }
            for (int i = 0; i < scene.Rubble.Length; i++)
            {
                RubblePlacement rubble = scene.Rubble[i];
                Sprite sprite = sprites.Rubble(i);
                SpriteRenderer renderer = AddRenderer(transform, "Rubble " + i, sprite, new Vector3(rubble.X, rubble.Y, 0f), propOrder);
                _drifters[scene.Islands.Length + i] = new Drifter { Transform = renderer.transform, Base = renderer.transform.localPosition, Phase = 1.1f + i * 0.8f };
            }
        }

        private void Update()
        {
            if (_drifters == null)
                return;

            float time = Time.time;
            for (int i = 0; i < _drifters.Length; i++)
            {
                ref Drifter drifter = ref _drifters[i];
                float lift = DriftAmplitude * Mathf.Sin(time * TwoPi / DriftPeriod + drifter.Phase);
                drifter.Transform.localPosition = new Vector3(drifter.Base.x, drifter.Base.y + lift, drifter.Base.z);
            }
            for (int i = 0; i < _starLayers.Length; i++)
            {
                float pulse = Mathf.Sin(time * TwoPi / TwinklePeriod + i * Mathf.PI);
                _starLayers[i].color = new Color(1f, 1f, 1f, 0.72f + 0.28f * pulse);
            }
            for (int i = 0; i < _sparkles.Length; i++)
                Show(ref _sparkles[i], (Mathf.FloorToInt((time + _sparkles[i].Phase) * SparkleRate) & 1) == 0);
            for (int i = 0; i < _lanterns.Length; i++)
            {
                ref Flicker lantern = ref _lanterns[i];
                Show(ref lantern, (Mathf.FloorToInt((time + lantern.Phase) * FlickerRate) & 1) == 0);
                lantern.Renderer.color = new Color(1f, 1f, 1f, 0.86f + 0.14f * Mathf.Sin(time * GlowRate + lantern.Phase));
            }
        }

        private void LateUpdate() => FrameCavern();

        // Cover the full camera without stretching the source. Small bounded parallax gives depth without exposing
        // image edges at any rim or screen aspect; the floor and collision geometry remain world-anchored.
        internal void FrameCavern()
        {
            if (CavernRenderer == null || _camera == null)
                return;
            Vector2 size = CavernRenderer.sprite.bounds.size;
            float height = 2f * _camera.orthographicSize;
            float width = height * _camera.aspect;
            float scale = Mathf.Max(width / size.x, height / size.y) * 1.08f;
            CavernRenderer.transform.localScale = new Vector3(scale, scale, 1f);
            Vector3 centre = _camera.transform.position;
            CavernRenderer.transform.position = new Vector3(
                centre.x - Mathf.Clamp(centre.x * .04f, -width * .025f, width * .025f),
                centre.y - Mathf.Clamp(centre.y * .04f, -height * .025f, height * .025f), transform.position.z);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }

        // Deep indigo below lifting to violet toward the upper right like the mockup, with soft nebula glows where the
        // mockup's haze sits: behind the galaxy, behind the top-left island and around the lower islands.
        private void BuildSky(Material material, int sortingOrder)
        {
            var builder = new MeshBuilder();
            Color deep = ToColor(PixelPalette.VoidDeep);
            Color mid = ToColor(PixelPalette.VoidMid);
            Color light = ToColor(PixelPalette.VoidLight);
            Color violet = Color.Lerp(light, ToColor(PixelPalette.NebulaViolet), 0.55f);
            builder.Quad(new Vector2(-14f, -16f), new Vector2(14f, -16f), new Vector2(14f, -5f), new Vector2(-14f, -5f), deep, deep, mid, deep);
            builder.Quad(new Vector2(-14f, -5f), new Vector2(14f, -5f), new Vector2(14f, 9f), new Vector2(-14f, 9f), deep, mid, violet, light);
            builder.Quad(new Vector2(-14f, 9f), new Vector2(14f, 9f), new Vector2(14f, 20f), new Vector2(-14f, 20f), light, violet, violet, light);

            Color glowViolet = ToColor(PixelPalette.NebulaViolet);
            Color glowBright = ToColor(PixelPalette.NebulaGlow);
            builder.Glow(new Vector2(1.9f, 6.3f), 2.8f, 2.1f, WithAlpha(glowBright, 0.3f));
            builder.Glow(new Vector2(-1.9f, 5.9f), 2.4f, 2.4f, WithAlpha(glowViolet, 0.45f));
            builder.Glow(new Vector2(-1.7f, -2.5f), 2.8f, 2.3f, WithAlpha(glowViolet, 0.4f));
            builder.Glow(new Vector2(2.7f, 3.2f), 2.2f, 3.2f, WithAlpha(glowViolet, 0.3f));
            builder.Glow(new Vector2(2.2f, -3.2f), 2.2f, 1.9f, WithAlpha(glowBright, 0.18f));

            _mesh = builder.ToMesh("Void Sky");
            SkyRenderer = MeshBuilder.Attach(transform, "Sky", _mesh, material, sortingOrder);
        }

        private static SpriteRenderer AddRenderer(Transform parent, string name, Sprite sprite, Vector3 localPosition, int sortingOrder)
        {
            Transform child = NewChild(parent, name, localPosition);
            var renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static Transform NewChild(Transform parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            return child.transform;
        }

        private static void Show(ref Flicker flicker, bool lit)
        {
            if (flicker.ShowingLit == lit)
                return;
            flicker.ShowingLit = lit;
            flicker.Renderer.sprite = lit ? flicker.Lit : flicker.Dim;
        }

        private static Color ToColor(Rgba color) => new Color32(color.R, color.G, color.B, color.A);

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
