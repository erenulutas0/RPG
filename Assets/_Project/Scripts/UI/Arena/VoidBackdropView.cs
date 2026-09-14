using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // The astral void behind the platform: a violet gradient, nebula glows, stars and distant floating rock. Built by
    // ArenaView at startup; the sky mesh is the lowest thing drawn (SkyRenderer), everything else sits above it.
    public sealed class VoidBackdropView : MonoBehaviour
    {
        private static readonly Color VoidTop = new Color(0.15f, 0.08f, 0.28f);
        private static readonly Color VoidBottom = new Color(0.04f, 0.025f, 0.09f);
        private static readonly Color NebulaViolet = new Color(0.5f, 0.22f, 0.72f, 0.3f);
        private static readonly Color NebulaBlue = new Color(0.26f, 0.22f, 0.7f, 0.24f);
        private static readonly Color StarWhite = new Color(1f, 0.98f, 0.94f);
        private static readonly Color StarLilac = new Color(0.78f, 0.74f, 1f);
        private static readonly Color RockTop = new Color(0.2f, 0.17f, 0.26f);
        private static readonly Color RockLeft = new Color(0.1f, 0.08f, 0.14f);
        private static readonly Color RockRight = new Color(0.14f, 0.11f, 0.19f);

        // Floating rocks around the platform: centre, top half-width and height, in world units.
        private static readonly Vector4[] Rocks =
        {
            new Vector4(-2.5f, 7.5f, 0.24f, 0.75f),
            new Vector4(2.45f, 6.3f, 0.19f, 0.5f),
            new Vector4(-2.85f, -2.7f, 0.28f, 0.85f),
            new Vector4(2.7f, -3.6f, 0.24f, 0.65f),
            new Vector4(-1.9f, -5.8f, 0.15f, 0.4f),
            new Vector4(2f, 8.8f, 0.13f, 0.35f)
        };

        private const int StarCount = 220;
        private const int Seed = 1409;

        private Mesh _mesh;

        public MeshRenderer SkyRenderer { get; private set; }

        // Builds the backdrop once; sortingOrder is the lowest order in the scene.
        public void Build(ArenaGeometry geometry, Material material, int sortingOrder)
        {
            var builder = new MeshBuilder();
            builder.Quad(new Vector2(-14f, -16f), new Vector2(14f, -16f), new Vector2(14f, -6f), new Vector2(-14f, -6f), VoidBottom);
            builder.Quad(new Vector2(-14f, -6f), new Vector2(14f, -6f), new Vector2(14f, 10f), new Vector2(-14f, 10f),
                VoidBottom, VoidBottom, VoidTop, VoidTop);
            builder.Quad(new Vector2(-14f, 10f), new Vector2(14f, 10f), new Vector2(14f, 20f), new Vector2(-14f, 20f), VoidTop);

            builder.Glow(new Vector2(-2.2f, 6.8f), 3.4f, 2.6f, NebulaViolet);
            builder.Glow(new Vector2(2.6f, 4.6f), 2.8f, 2.2f, NebulaBlue);
            builder.Glow(new Vector2(-2.4f, -2.6f), 3f, 2.4f, NebulaBlue);
            builder.Glow(new Vector2(2.3f, -4.2f), 2.6f, 2.2f, NebulaViolet);

            for (int i = 0; i < StarCount; i++)
            {
                var centre = new Vector2(Range(i, 0, -4.6f, 4.6f), Range(i, 1, -7.5f, 11f));
                float size = Range(i, 2, 0.018f, 0.05f);
                Color color = Color.Lerp(StarWhite, StarLilac, Range(i, 3, 0f, 1f)) * Range(i, 4, 0.35f, 1f);
                color.a = 1f;
                builder.Diamond(centre, size, size, color);
                if (i % 16 == 0)
                {
                    builder.Quad(centre + new Vector2(-size * 4f, -0.008f), centre + new Vector2(size * 4f, -0.008f),
                        centre + new Vector2(size * 4f, 0.008f), centre + new Vector2(-size * 4f, 0.008f), color);
                    builder.Quad(centre + new Vector2(-0.008f, -size * 4f), centre + new Vector2(0.008f, -size * 4f),
                        centre + new Vector2(0.008f, size * 4f), centre + new Vector2(-0.008f, size * 4f), color);
                }
            }

            foreach (Vector4 rock in Rocks)
            {
                var centre = new Vector2(rock.x, rock.y);
                float halfWidth = rock.z;
                float halfHeight = halfWidth * 0.6f;
                var drop = new Vector2(0f, -rock.w);
                Vector2 left = centre + new Vector2(-halfWidth, 0f);
                Vector2 right = centre + new Vector2(halfWidth, 0f);
                Vector2 near = centre + new Vector2(0f, -halfHeight);
                builder.Quad(left, near, near + drop, left + drop, RockLeft);
                builder.Quad(near, right, right + drop, near + drop, RockRight);
                builder.Triangle(left + drop, near + drop, near + drop * 1.6f, RockLeft);
                builder.Triangle(near + drop, right + drop, near + drop * 1.6f, RockRight);
                builder.Diamond(centre, halfWidth, halfHeight, RockTop);
            }

            _mesh = builder.ToMesh("Void Sky");
            SkyRenderer = MeshBuilder.Attach(transform, "Sky", _mesh, material, sortingOrder);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }

        private static float Range(int index, int channel, float min, float max) => min + PixelNoise.Value(index, channel, Seed) * (max - min);
    }
}
