using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cryptforge.Art
{
    // One immutable nine-mesh set per cached geometry, independent of texture choice. No per-frame generation.
    internal sealed class PlatformMaterialMeshes : IDisposable
    {
        private readonly ArenaGeometry _geometry;
        private readonly Mesh[] _parts = new Mesh[9];
        internal int Count => _parts.Length;
        internal Mesh this[int index] => _parts[index];
        internal bool Matches(ArenaGeometry geometry) => geometry.NearCorner == _geometry.NearCorner &&
            geometry.FarCorner == _geometry.FarCorner && geometry.HalfWidth == _geometry.HalfWidth;

        internal PlatformMaterialMeshes(ArenaGeometry geometry)
        {
            _geometry = geometry;
            try
            {
                // Preserve the original hall's perpendicular coping width instead of scaling it with the region.
                float inset = (.45f / Mathf.Sqrt(2f)) * Mathf.Sqrt(
                    1f / (geometry.HalfWidth * geometry.HalfWidth) + 1f / (geometry.HalfDepth * geometry.HalfDepth));
                Vector3[] outer = Corners(1f), inner = Corners(Mathf.Max(.5f, 1f - inset));
                float edgeScale = Mathf.Sqrt((geometry.HalfWidth * geometry.HalfWidth +
                    geometry.HalfDepth * geometry.HalfDepth) / 162f);
                _parts[0] = Quad("Painted Floor", inner, FloorUv(inner), Color.white);
                for (int i = 0; i < 4; i++)
                {
                    int next = (i + 1) % 4;
                    // Shared corner positions make true miter joins; the old top sprite is absent underneath.
                    _parts[i + 1] = Quad("Painted Coping " + i,
                        new[] { outer[i], outer[next], inner[next], inner[i] }, Uv(2 * edgeScale, 1), Color.white);
                }
                Vector3 drop = new Vector3(0, -PlatformLayout.FaceDepth, 0);
                for (int i = 0; i < 2; i++)
                {
                    int side = i == 0 ? 1 : 3;
                    Color tone = i == 0 ? new Color(.84f, .84f, .88f, 1) : Color.white;
                    _parts[5 + i] = Quad("Painted Near Face " + i,
                        new[] { outer[0] + drop, outer[side] + drop, outer[side], outer[0] }, Uv(3 * edgeScale, 1), tone);
                    // A narrow dark masonry lip covers the procedural keel's uppermost texel row.
                    Vector3 bottom = drop + new Vector3(0, -.07f, 0), top = drop + new Vector3(0, .09f, 0);
                    _parts[7 + i] = Quad("Foundation Join " + i,
                        new[] { outer[0] + bottom, outer[side] + bottom, outer[side] + top, outer[0] + top },
                        Uv(3 * edgeScale, .1f), new Color(.48f, .46f, .5f, 1));
                }
            }
            catch { Dispose(); throw; }
        }

        private Vector3[] Corners(float scale)
        {
            float y = _geometry.WorldMiddle, half = (_geometry.WorldTop - _geometry.WorldBottom) * .5f;
            return new[] { new Vector3(0, y - half * scale), new Vector3(_geometry.HalfWidth * scale, y),
                new Vector3(0, y + half * scale), new Vector3(-_geometry.HalfWidth * scale, y) };
        }
        private static Vector2[] Uv(float u, float v) => new[] { Vector2.zero, new Vector2(u, 0), new Vector2(u, v), new Vector2(0, v) };
        // Match the original 9-unit hall, but keep masonry fixed in floor space when the region grows.
        // World y is half floor y. Independent planar coordinates avoid shearing tiles on elongated diamonds.
        private Vector2[] FloorUv(Vector3[] vertices)
        {
            const float span = 9f * .95f;
            var result = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                float x = vertices[i].x / span;
                float y = (vertices[i].y - _geometry.WorldMiddle) * 2f / span;
                result[i] = new Vector2(1f + x + y, 1f - x + y);
            }
            return result;
        }
        private static Mesh Quad(string name, Vector3[] vertices, Vector2[] uv, Color color)
        {
            var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = new[] { 0, 1, 2, 0, 2, 3 },
                colors = new[] { color, color, color, color } };
            mesh.RecalculateBounds();
            return mesh;
        }
        public void Dispose()
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                if (_parts[i] != null) Object.Destroy(_parts[i]);
                _parts[i] = null;
            }
        }
    }
}
