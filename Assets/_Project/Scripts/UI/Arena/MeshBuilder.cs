using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.UI
{
    // Collects vertex-coloured triangles for one mesh: flat quads, triangles and soft glows for the large shapes of the
    // arena that need no pixel texture (the void's gradient, nebula glows, the platform's faces).
    public sealed class MeshBuilder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Color> _colors = new List<Color>();
        private readonly List<int> _triangles = new List<int>();

        public int VertexCount => _vertices.Count;

        public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color) => Quad(a, b, c, d, color, color, color, color);

        // Corners in order around the quad; each carries its own colour for gradients.
        public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color colorA, Color colorB, Color colorC, Color colorD)
        {
            int first = _vertices.Count;
            AddVertex(a, colorA);
            AddVertex(b, colorB);
            AddVertex(c, colorC);
            AddVertex(d, colorD);
            AddTriangle(first, first + 1, first + 2);
            AddTriangle(first, first + 2, first + 3);
        }

        public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int first = _vertices.Count;
            AddVertex(a, color);
            AddVertex(b, color);
            AddVertex(c, color);
            AddTriangle(first, first + 1, first + 2);
        }

        public void Diamond(Vector2 centre, float halfWidth, float halfHeight, Color color) =>
            Quad(centre + new Vector2(0f, -halfHeight), centre + new Vector2(halfWidth, 0f), centre + new Vector2(0f, halfHeight),
                centre + new Vector2(-halfWidth, 0f), color);

        // A soft ellipse: the full colour at the centre, fading to clear at the rim.
        public void Glow(Vector2 centre, float radiusX, float radiusY, Color color, int segments = 20)
        {
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

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (_vertices.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(_vertices);
            mesh.SetColors(_colors);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // A child object drawing the mesh with a vertex-colour material at the given sorting order.
        public static MeshRenderer Attach(Transform parent, string name, Mesh mesh, Material material, int sortingOrder)
        {
            var meshObject = new GameObject(name);
            meshObject.transform.SetParent(parent, false);
            meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = sortingOrder;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return renderer;
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
