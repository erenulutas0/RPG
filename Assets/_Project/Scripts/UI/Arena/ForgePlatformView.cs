using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // The floating forge platform: a tiled rhombus top with a brass rim and inlay, ember lanterns at the corners, and the
    // near faces and glowing keel hanging below it. Built by ArenaView at startup from the arena geometry.
    public sealed class ForgePlatformView : MonoBehaviour
    {
        private static readonly Color StoneLight = new Color(0.31f, 0.29f, 0.34f);
        private static readonly Color StoneDark = new Color(0.26f, 0.24f, 0.29f);
        private static readonly Color Grout = new Color(0.12f, 0.1f, 0.14f);
        private static readonly Color Brass = new Color(0.74f, 0.54f, 0.25f);
        private static readonly Color BrassDark = new Color(0.4f, 0.28f, 0.14f);
        private static readonly Color FaceLeft = new Color(0.15f, 0.13f, 0.19f);
        private static readonly Color FaceRight = new Color(0.21f, 0.18f, 0.24f);
        private static readonly Color FaceSeam = new Color(0.08f, 0.07f, 0.11f);
        private static readonly Color KeelLeft = new Color(0.1f, 0.08f, 0.13f);
        private static readonly Color KeelRight = new Color(0.14f, 0.11f, 0.17f);
        private static readonly Color Ember = new Color(1f, 0.58f, 0.18f);
        private static readonly Color EmberGlow = new Color(1f, 0.42f, 0.1f, 0.45f);

        private const int TilesPerEdge = 8;
        // How far the near faces hang below the top, and the keel below them, in world units.
        private const float FaceDepth = 1.3f;
        private const float KeelDepth = 2.2f;
        private const int Seed = 2207;

        private ArenaGeometry _geometry;
        private Mesh _mesh;

        public MeshRenderer PlatformRenderer { get; private set; }

        // Builds the platform once; sortingOrder is just above the backdrop and below every combatant.
        public void Build(ArenaGeometry geometry, Material material, int sortingOrder)
        {
            _geometry = geometry;
            var builder = new MeshBuilder();
            Vector2 near = FloorPoint(0f, geometry.NearCorner);
            Vector2 far = FloorPoint(0f, geometry.FarCorner);
            Vector2 left = FloorPoint(-geometry.HalfWidth, geometry.Middle);
            Vector2 right = FloorPoint(geometry.HalfWidth, geometry.Middle);
            var faceDrop = new Vector2(0f, -FaceDepth);
            Vector2 keel = near + faceDrop + new Vector2(0f, -KeelDepth);

            // The keel and the near faces come first; the top covers their upper edges.
            builder.Glow(keel + new Vector2(0f, KeelDepth * 0.4f), 1.7f, 2.1f, EmberGlow);
            builder.Triangle(left + faceDrop, near + faceDrop, keel, KeelLeft);
            builder.Triangle(near + faceDrop, right + faceDrop, keel, KeelRight);
            var clearEmber = new Color(Ember.r, Ember.g, Ember.b, 0f);
            foreach (float side in new[] { -1f, 1f })
            {
                for (int k = 1; k <= 2; k++)
                {
                    Vector2 start = Vector2.Lerp(near, side < 0f ? left : right, k * 0.3f) + faceDrop;
                    Vector2 end = Vector2.Lerp(start, keel, 0.55f);
                    builder.Quad(start + new Vector2(-0.025f, 0f), start + new Vector2(0.025f, 0f), end + new Vector2(0.012f, 0f),
                        end + new Vector2(-0.012f, 0f), Ember, Ember, clearEmber, clearEmber);
                }
            }
            builder.Quad(near + faceDrop + new Vector2(-0.06f, 0f), near + faceDrop + new Vector2(0.06f, 0f),
                keel + new Vector2(0.01f, 0.2f), keel + new Vector2(-0.01f, 0.2f), Ember, Ember, clearEmber, clearEmber);
            Face(builder, left, near, faceDrop, FaceLeft);
            Face(builder, near, right, faceDrop, FaceRight);

            const float rim = 0.02f;
            builder.Quad(near, right, far, left, Brass);
            builder.Quad(TopPoint(rim, rim), TopPoint(1f - rim, rim), TopPoint(1f - rim, 1f - rim), TopPoint(rim, 1f - rim), Grout);
            float cell = (1f - 2f * rim) / TilesPerEdge;
            const float gap = 0.07f;
            for (int i = 0; i < TilesPerEdge; i++)
            {
                for (int j = 0; j < TilesPerEdge; j++)
                {
                    float s = rim + (i + gap / 2f) * cell;
                    float t = rim + (j + gap / 2f) * cell;
                    float size = (1f - gap) * cell;
                    // Tiles darken toward the far corner, which sits under the top HUD.
                    float shade = Mathf.Lerp(1f, 0.7f, (i + j + 1f) / (2f * TilesPerEdge)) * (0.94f + PixelNoise.Value(i, j, Seed) * 0.12f);
                    Color stone = ((i + j) % 2 == 0 ? StoneLight : StoneDark) * shade;
                    stone.a = 1f;
                    builder.Quad(TopPoint(s, t), TopPoint(s + size, t), TopPoint(s + size, t + size), TopPoint(s, t + size), stone);
                }
            }

            // A brass inlay at the centre and ember lanterns at the corners.
            Vector2 centre = FloorPoint(0f, geometry.Middle);
            float inlayWidth = geometry.HalfWidth * 0.16f;
            float inlayHeight = ArenaFloor.WorldY(geometry.HalfDepth) * 0.16f;
            builder.Diamond(centre, inlayWidth, inlayHeight, Brass);
            builder.Diamond(centre, inlayWidth * 0.72f, inlayHeight * 0.72f, BrassDark);
            builder.Diamond(centre, inlayWidth * 0.22f, inlayHeight * 0.22f, Brass);
            foreach (Vector2 corner in new[] { near, left, right })
            {
                builder.Glow(corner, 0.55f, 0.4f, EmberGlow);
                builder.Diamond(corner, 0.1f, 0.07f, Ember);
            }

            _mesh = builder.ToMesh("Forge Platform");
            PlatformRenderer = MeshBuilder.Attach(transform, "Platform", _mesh, material, sortingOrder);
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }

        // A near face: a vertical band hanging below one near edge of the top, with seams and ember slits.
        private static void Face(MeshBuilder builder, Vector2 from, Vector2 to, Vector2 drop, Color color)
        {
            builder.Quad(from, to, to + drop, from + drop, color);
            const int columns = 6;
            for (int k = 1; k < columns; k++)
            {
                Vector2 top = Vector2.Lerp(from, to, (float)k / columns);
                builder.Quad(top + new Vector2(-0.02f, 0f), top + new Vector2(0.02f, 0f), top + drop + new Vector2(0.02f, 0f),
                    top + drop + new Vector2(-0.02f, 0f), FaceSeam);
                if (k % 2 == 0)
                {
                    Vector2 slitTop = top + new Vector2(0.14f, 0f) + drop * 0.5f;
                    Vector2 slitBottom = top + new Vector2(0.14f, 0f) + drop * 0.85f;
                    builder.Glow((slitTop + slitBottom) / 2f, 0.22f, 0.3f, EmberGlow);
                    builder.Quad(slitBottom + new Vector2(-0.035f, 0f), slitBottom + new Vector2(0.035f, 0f),
                        slitTop + new Vector2(0.035f, 0f), slitTop + new Vector2(-0.035f, 0f), Ember);
                }
            }
        }

        private static Vector2 FloorPoint(float floorX, float floorY) => new Vector2(floorX, ArenaFloor.WorldY(floorY));

        private Vector2 TopPoint(float s, float t)
        {
            _geometry.TopPoint(s, t, out float x, out float y);
            return new Vector2(x, y);
        }
    }
}
