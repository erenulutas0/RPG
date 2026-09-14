using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // The astral void behind the platform: a violet gradient with nebula glows as a vertex-colour mesh (the lowest thing
    // drawn, SkyRenderer), then pixel sprites for the haze clouds, the spiral galaxy, two star layers, and the floating
    // islands with their orbital rings, chains, lantern flames and drifting rubble, all placed by VoidLayout. Everything
    // is built once in Build; Update only moves transforms, swaps pre-built sprites and tints renderers, so nothing
    // allocates per frame and Time.timeScale = 0 freezes the drift and the twinkle.
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

        private static readonly Vector2 TopCentre = new Vector2(0.5f, 1f);

        private readonly List<Sprite> _sprites = new List<Sprite>();
        private Mesh _mesh;
        private Drifter[] _drifters;
        private Flicker[] _sparkles;
        private Flicker[] _lanterns;
        private SpriteRenderer[] _starLayers;

        // The sky mesh: the lowest renderer of the scene, at the sorting order Build was given.
        public MeshRenderer SkyRenderer { get; private set; }

        // Builds the backdrop once; sortingOrder is the lowest order in the scene and the sprites use the four orders
        // above it (haze and galaxy, stars and back ring halves, islands, front ring halves and flames).
        public void Build(ArenaGeometry geometry, Material material, int sortingOrder)
        {
            if (material == null)
            {
                Debug.LogError("VoidBackdropView needs a vertex-colour material for its sky.", this);
                enabled = false;
                return;
            }

            BuildSky(material, sortingOrder);

            VoidScene scene = VoidLayout.Create();
            int hazeOrder = sortingOrder + 1;
            int starOrder = sortingOrder + 2;
            int propOrder = sortingOrder + 3;
            int frontOrder = sortingOrder + 4;

            for (int i = 0; i < scene.Hazes.Length; i++)
            {
                HazePlacement haze = scene.Hazes[i];
                Sprite sprite = MakeSprite(VoidArt.DrawHaze(haze.Width, haze.Height, haze.Seed), "Void Haze " + i, PixelSpriteFactory.Centre, VoidArt.HazeTexelsPerUnit);
                AddRenderer(transform, "Haze " + i, sprite, new Vector3(haze.X, haze.Y, 0f), hazeOrder);
            }
            GalaxyPlacement galaxy = scene.Galaxy;
            AddRenderer(transform, "Galaxy", MakeSprite(VoidArt.DrawGalaxy(galaxy.Radius, galaxy.Seed), "Void Galaxy", PixelSpriteFactory.Centre), new Vector3(galaxy.X, galaxy.Y, 0f), hazeOrder);

            // The star field hangs from its top so it always sorts first among the star-order sprites.
            _starLayers = new SpriteRenderer[2];
            int starWidth = Mathf.RoundToInt(VoidScene.StarFieldWidth * VoidArt.TexelsPerUnit);
            int starHeight = Mathf.RoundToInt(VoidScene.StarFieldHeight * VoidArt.TexelsPerUnit);
            var starTop = new Vector3(VoidScene.StarFieldLeft + VoidScene.StarFieldWidth / 2f, VoidScene.StarFieldBottom + VoidScene.StarFieldHeight, 0f);
            for (int layer = 0; layer < _starLayers.Length; layer++)
            {
                Sprite sprite = MakeSprite(VoidArt.DrawStarField(starWidth, starHeight, VoidScene.StarFieldSeed, layer), "Void Stars " + layer, TopCentre);
                _starLayers[layer] = AddRenderer(transform, "Stars " + layer, sprite, starTop, starOrder);
            }

            Sprite sparkleLit = MakeSprite(VoidArt.DrawSparkle(true), "Void Sparkle Lit", PixelSpriteFactory.Centre);
            Sprite sparkleDim = MakeSprite(VoidArt.DrawSparkle(false), "Void Sparkle Dim", PixelSpriteFactory.Centre);
            _sparkles = new Flicker[scene.Sparkles.Length];
            for (int i = 0; i < _sparkles.Length; i++)
            {
                SparklePlacement sparkle = scene.Sparkles[i];
                SpriteRenderer renderer = AddRenderer(transform, "Sparkle " + i, sparkleLit, new Vector3(sparkle.X, sparkle.Y, 0f), frontOrder);
                _sparkles[i] = new Flicker { Renderer = renderer, Lit = sparkleLit, Dim = sparkleDim, Phase = sparkle.Phase, ShowingLit = true };
            }

            Sprite flameTall = MakeSprite(VoidArt.DrawLantern(0), "Void Flame Tall", PixelSpriteFactory.BottomCentre);
            Sprite flameLean = MakeSprite(VoidArt.DrawLantern(1), "Void Flame Lean", PixelSpriteFactory.BottomCentre);
            _drifters = new Drifter[scene.Islands.Length + scene.Rubble.Length];
            _lanterns = new Flicker[scene.Islands.Length];
            for (int i = 0; i < scene.Islands.Length; i++)
            {
                IslandPlacement placement = scene.Islands[i];
                IslandArt art = VoidArt.DrawIsland(placement.Spec);
                Transform root = NewChild(transform, "Island " + i, new Vector3(placement.X, placement.Y, 0f));
                _drifters[i] = new Drifter { Transform = root, Base = root.localPosition, Phase = placement.DriftPhase };

                if (placement.HasRing)
                {
                    var ringCentre = new Vector3(0f, placement.RingCentreY(art) - placement.Y, 0f);
                    AddRenderer(root, "Ring Back", MakeSprite(VoidArt.DrawRing(placement.RingWidth, placement.RingHeight, false), "Void Ring Back " + i, PixelSpriteFactory.Centre), ringCentre, starOrder);
                    AddRenderer(root, "Ring Front", MakeSprite(VoidArt.DrawRing(placement.RingWidth, placement.RingHeight, true), "Void Ring Front " + i, PixelSpriteFactory.Centre), ringCentre, frontOrder);
                }
                AddRenderer(root, "Rock", MakeSprite(art.Canvas, "Void Island " + i, PixelSpriteFactory.BottomCentre), Vector3.zero, propOrder);
                for (int c = 0; c < placement.Chains.Length; c++)
                {
                    ChainSpec chain = placement.Chains[c];
                    placement.ChainTop(art, chain, out float chainX, out float chainY);
                    Sprite sprite = MakeSprite(VoidArt.DrawChain(chain.Links), "Void Chain " + i + "." + c, TopCentre);
                    AddRenderer(root, "Chain " + c, sprite, new Vector3(chainX - placement.X, chainY - placement.Y, 0f), propOrder);
                }
                var flameFoot = new Vector3((art.LanternX + 0.5f - placement.Spec.Width / 2f) / VoidArt.TexelsPerUnit, art.LanternY / (float)VoidArt.TexelsPerUnit, 0f);
                SpriteRenderer flame = AddRenderer(root, "Lantern", flameTall, flameFoot, frontOrder);
                _lanterns[i] = new Flicker { Renderer = flame, Lit = flameTall, Dim = flameLean, Phase = placement.DriftPhase * 0.37f, ShowingLit = true };
            }
            for (int i = 0; i < scene.Rubble.Length; i++)
            {
                RubblePlacement rubble = scene.Rubble[i];
                Sprite sprite = MakeSprite(VoidArt.DrawRubble(rubble.Size, rubble.Seed), "Void Rubble " + i, PixelSpriteFactory.Centre);
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

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
            foreach (Sprite sprite in _sprites)
                PixelSpriteFactory.Destroy(sprite);
            _sprites.Clear();
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

        // Every sprite is tracked so OnDestroy frees its texture.
        private Sprite MakeSprite(PixelCanvas canvas, string name, Vector2 pivot, float pixelsPerUnit = PixelSpriteFactory.PixelsPerUnit)
        {
            Sprite sprite = PixelSpriteFactory.CreateSprite(canvas, name, pivot, pixelsPerUnit);
            _sprites.Add(sprite);
            return sprite;
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
