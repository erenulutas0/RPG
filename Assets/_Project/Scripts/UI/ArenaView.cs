using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cryptforge.UI
{
    // Placeholder look of the astral foundry arena chosen in `19`: a floating forge platform in a starry violet void. The
    // platform's top is a tiled rhombus on the arena floor with the hero near its near corner, and its near faces and a
    // glowing keel hang below. Everything is one vertex-coloured mesh built at startup, so no art asset is needed until final
    // art replaces this view. The view also makes the camera draw sprites higher on the screen first, so a nearer combatant
    // overlaps a farther one.
    public sealed class ArenaView : MonoBehaviour
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

        [SerializeField] private Camera _camera;
        // Draws vertex colours; the built-in Sprites-Default material does.
        [SerializeField] private Material _material;
        // The platform top on the arena floor, in floor units with the hero at the origin: its near corner behind the hero,
        // its far corner beyond the deepest pack slot, and its half-width at the side corners.
        [SerializeField] private float _nearCorner = -3f;
        [SerializeField] private float _farCorner = 14f;
        [SerializeField, Min(0.5f)] private float _halfWidth = 3.3f;
        [SerializeField, Range(2, 12)] private int _tilesPerEdge = 8;
        // How far the near faces hang below the top, and the keel below them, in world units.
        [SerializeField, Min(0f)] private float _faceDepth = 1.3f;
        [SerializeField, Min(0f)] private float _keelDepth = 2.2f;
        [SerializeField, Range(0, 400)] private int _starCount = 220;
        [SerializeField] private int _seed = 1409;
        // Below every combatant sprite.
        [SerializeField] private int _sortingOrder = -20;

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _triangles = new List<int>();
        private System.Random _random;
        private Mesh _mesh;

        public MeshRenderer ArenaRenderer { get; private set; }

        // True when the floor point lies on the platform's top.
        public bool IsOnPlatform(float floorX, float floorY)
        {
            float halfDepth = (_farCorner - _nearCorner) / 2f;
            float middle = (_farCorner + _nearCorner) / 2f;
            return Math.Abs(floorX) / _halfWidth + Math.Abs(floorY - middle) / halfDepth <= 1f;
        }

        private void Awake()
        {
            if (_camera == null || _material == null || !(_farCorner > _nearCorner))
            {
                Debug.LogError("ArenaView needs a camera, a vertex-colour material and a far corner beyond the near corner.", this);
                enabled = false;
                return;
            }

            _camera.transparencySortMode = TransparencySortMode.CustomAxis;
            _camera.transparencySortAxis = Vector3.up;

            _random = new System.Random(_seed);
            BuildVoid();
            BuildPlatform();
            _mesh = new Mesh { name = "Arena" };
            _mesh.SetVertices(_vertices);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();

            var meshObject = new GameObject("Arena Mesh");
            meshObject.transform.SetParent(transform, false);
            meshObject.AddComponent<MeshFilter>().sharedMesh = _mesh;
            ArenaRenderer = meshObject.AddComponent<MeshRenderer>();
            ArenaRenderer.sharedMaterial = _material;
            ArenaRenderer.sortingOrder = _sortingOrder;
            ArenaRenderer.shadowCastingMode = ShadowCastingMode.Off;
            ArenaRenderer.receiveShadows = false;
            ArenaRenderer.lightProbeUsage = LightProbeUsage.Off;
            ArenaRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private void OnDestroy()
        {
            if (_mesh != null)
                Destroy(_mesh);
        }

        private void BuildVoid()
        {
            Quad(new Vector2(-14f, -16f), new Vector2(14f, -16f), new Vector2(14f, -6f), new Vector2(-14f, -6f), VoidBottom);
            Quad(new Vector2(-14f, -6f), new Vector2(14f, -6f), new Vector2(14f, 10f), new Vector2(-14f, 10f),
                VoidBottom, VoidBottom, VoidTop, VoidTop);
            Quad(new Vector2(-14f, 10f), new Vector2(14f, 10f), new Vector2(14f, 20f), new Vector2(-14f, 20f), VoidTop);

            Glow(new Vector2(-2.2f, 6.8f), 3.4f, 2.6f, NebulaViolet);
            Glow(new Vector2(2.6f, 4.6f), 2.8f, 2.2f, NebulaBlue);
            Glow(new Vector2(-2.4f, -2.6f), 3f, 2.4f, NebulaBlue);
            Glow(new Vector2(2.3f, -4.2f), 2.6f, 2.2f, NebulaViolet);

            for (int i = 0; i < _starCount; i++)
            {
                var centre = new Vector2(Range(-4.6f, 4.6f), Range(-7.5f, 11f));
                float size = Range(0.018f, 0.05f);
                Color color = Color.Lerp(StarWhite, StarLilac, Range(0f, 1f)) * Range(0.35f, 1f);
                color.a = 1f;
                Diamond(centre, size, size, color);
                if (i % 16 == 0)
                {
                    Quad(centre + new Vector2(-size * 4f, -0.008f), centre + new Vector2(size * 4f, -0.008f),
                        centre + new Vector2(size * 4f, 0.008f), centre + new Vector2(-size * 4f, 0.008f), color);
                    Quad(centre + new Vector2(-0.008f, -size * 4f), centre + new Vector2(0.008f, -size * 4f),
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
                Quad(left, near, near + drop, left + drop, RockLeft);
                Quad(near, right, right + drop, near + drop, RockRight);
                Triangle(left + drop, near + drop, near + drop * 1.6f, RockLeft);
                Triangle(near + drop, right + drop, near + drop * 1.6f, RockRight);
                Diamond(centre, halfWidth, halfHeight, RockTop);
            }
        }

        private void BuildPlatform()
        {
            float middle = (_nearCorner + _farCorner) / 2f;
            Vector2 near = FloorPoint(0f, _nearCorner);
            Vector2 far = FloorPoint(0f, _farCorner);
            Vector2 left = FloorPoint(-_halfWidth, middle);
            Vector2 right = FloorPoint(_halfWidth, middle);
            var faceDrop = new Vector2(0f, -_faceDepth);
            Vector2 keel = near + faceDrop + new Vector2(0f, -_keelDepth);

            // The keel and the near faces come first; the top covers their upper edges.
            Glow(keel + new Vector2(0f, _keelDepth * 0.4f), 1.7f, 2.1f, EmberGlow);
            Triangle(left + faceDrop, near + faceDrop, keel, KeelLeft);
            Triangle(near + faceDrop, right + faceDrop, keel, KeelRight);
            var clearEmber = new Color(Ember.r, Ember.g, Ember.b, 0f);
            // Ember cracks run down each side of the keel toward its tip.
            foreach (float side in new[] { -1f, 1f })
            {
                for (int k = 1; k <= 2; k++)
                {
                    Vector2 start = Vector2.Lerp(near, side < 0f ? left : right, k * 0.3f) + faceDrop;
                    Vector2 end = Vector2.Lerp(start, keel, 0.55f);
                    Quad(start + new Vector2(-0.025f, 0f), start + new Vector2(0.025f, 0f), end + new Vector2(0.012f, 0f),
                        end + new Vector2(-0.012f, 0f), Ember, Ember, clearEmber, clearEmber);
                }
            }
            Quad(near + faceDrop + new Vector2(-0.06f, 0f), near + faceDrop + new Vector2(0.06f, 0f),
                keel + new Vector2(0.01f, 0.2f), keel + new Vector2(-0.01f, 0.2f), Ember, Ember, clearEmber, clearEmber);
            Face(left, near, faceDrop, FaceLeft);
            Face(near, right, faceDrop, FaceRight);

            const float rim = 0.02f;
            Quad(near, right, far, left, Brass);
            Quad(TopPoint(rim, rim), TopPoint(1f - rim, rim), TopPoint(1f - rim, 1f - rim), TopPoint(rim, 1f - rim), Grout);
            float cell = (1f - 2f * rim) / _tilesPerEdge;
            const float gap = 0.07f;
            for (int i = 0; i < _tilesPerEdge; i++)
            {
                for (int j = 0; j < _tilesPerEdge; j++)
                {
                    float s = rim + (i + gap / 2f) * cell;
                    float t = rim + (j + gap / 2f) * cell;
                    float size = (1f - gap) * cell;
                    // Tiles darken toward the far corner, which sits under the top HUD.
                    float shade = Mathf.Lerp(1f, 0.7f, (i + j + 1f) / (2f * _tilesPerEdge)) * Range(0.94f, 1.06f);
                    Color stone = ((i + j) % 2 == 0 ? StoneLight : StoneDark) * shade;
                    stone.a = 1f;
                    Quad(TopPoint(s, t), TopPoint(s + size, t), TopPoint(s + size, t + size), TopPoint(s, t + size), stone);
                }
            }

            // A brass inlay at the centre and ember lanterns at the corners.
            Vector2 centre = FloorPoint(0f, middle);
            float inlayWidth = _halfWidth * 0.16f;
            float inlayHeight = ArenaFloor.WorldY((_farCorner - _nearCorner) / 2f) * 0.16f;
            Diamond(centre, inlayWidth, inlayHeight, Brass);
            Diamond(centre, inlayWidth * 0.72f, inlayHeight * 0.72f, BrassDark);
            Diamond(centre, inlayWidth * 0.22f, inlayHeight * 0.22f, Brass);
            foreach (Vector2 corner in new[] { near, left, right })
            {
                Glow(corner, 0.55f, 0.4f, EmberGlow);
                Diamond(corner, 0.1f, 0.07f, Ember);
            }
        }

        // A near face: a vertical band hanging below one near edge of the top, with seams and ember slits.
        private void Face(Vector2 from, Vector2 to, Vector2 drop, Color color)
        {
            Quad(from, to, to + drop, from + drop, color);
            const int columns = 6;
            for (int k = 1; k < columns; k++)
            {
                Vector2 top = Vector2.Lerp(from, to, (float)k / columns);
                Quad(top + new Vector2(-0.02f, 0f), top + new Vector2(0.02f, 0f), top + drop + new Vector2(0.02f, 0f),
                    top + drop + new Vector2(-0.02f, 0f), FaceSeam);
                if (k % 2 == 0)
                {
                    Vector2 slitTop = top + new Vector2(0.14f, 0f) + drop * 0.5f;
                    Vector2 slitBottom = top + new Vector2(0.14f, 0f) + drop * 0.85f;
                    Glow((slitTop + slitBottom) / 2f, 0.22f, 0.3f, EmberGlow);
                    Quad(slitBottom + new Vector2(-0.035f, 0f), slitBottom + new Vector2(0.035f, 0f),
                        slitTop + new Vector2(0.035f, 0f), slitTop + new Vector2(-0.035f, 0f), Ember);
                }
            }
        }

        private static Vector2 FloorPoint(float floorX, float floorY) => new Vector2(floorX, ArenaFloor.WorldY(floorY));

        // A point on the platform top: s runs from the near corner toward the right corner, t toward the left corner.
        private Vector2 TopPoint(float s, float t)
        {
            float middle = (_nearCorner + _farCorner) / 2f;
            return FloorPoint((s - t) * _halfWidth, _nearCorner + (s + t) * (middle - _nearCorner));
        }

        private float Range(float min, float max) => min + (float)_random.NextDouble() * (max - min);

        private void Diamond(Vector2 centre, float halfWidth, float halfHeight, Color color) =>
            Quad(centre + new Vector2(0f, -halfHeight), centre + new Vector2(halfWidth, 0f), centre + new Vector2(0f, halfHeight),
                centre + new Vector2(-halfWidth, 0f), color);

        private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color) => Quad(a, b, c, d, color, color, color, color);

        private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color colorA, Color colorB, Color colorC, Color colorD)
        {
            int first = _vertices.Count;
            AddVertex(a, colorA);
            AddVertex(b, colorB);
            AddVertex(c, colorC);
            AddVertex(d, colorD);
            AddTriangle(first, first + 1, first + 2);
            AddTriangle(first, first + 2, first + 3);
        }

        private void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int first = _vertices.Count;
            AddVertex(a, color);
            AddVertex(b, color);
            AddVertex(c, color);
            AddTriangle(first, first + 1, first + 2);
        }

        // A soft ellipse: the full colour at the centre, fading to clear at the rim.
        private void Glow(Vector2 centre, float radiusX, float radiusY, Color color)
        {
            const int segments = 20;
            int first = _vertices.Count;
            AddVertex(centre, color);
            var clear = new Color(color.r, color.g, color.b, 0f);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                AddVertex(centre + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY), clear);
            }
            for (int i = 0; i < segments; i++)
                AddTriangle(first, first + 1 + i, first + 1 + (i + 1) % segments);
        }

        private void AddVertex(Vector2 position, Color color)
        {
            _vertices.Add(position);
            _colors.Add(color);
        }

        private void AddTriangle(int a, int b, int c)
        {
            _triangles.Add(a);
            _triangles.Add(b);
            _triangles.Add(c);
        }
    }
}
