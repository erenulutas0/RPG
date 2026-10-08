using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.UI
{
    // Shared grid sprites let floor rings, shadows and pulses bend over both ramp seams.
    // Sources/textures are borrowed; copies and the single material belong to the scene.
    internal sealed class FoundryGroundProjection
    {
        private const int Divisions = 16;
        private readonly Dictionary<Sprite, Sprite> _grids = new Dictionary<Sprite, Sprite>();
        private readonly List<SpriteRenderer> _shown = new List<SpriteRenderer>();
        private readonly List<Sprite> _sources = new List<Sprite>();
        private readonly List<Material> _materials = new List<Material>();
        private Material _material;

        internal void Apply(SpriteRenderer renderer, Vector3 region)
        {
            if (renderer == null || !renderer.enabled || renderer.sprite == null) return;
            // Most combat is on the unchanged court. Avoid grid/material work there;
            // conservative bounds include any ring that straddles the foot of the ramp.
            Vector3 high = renderer.bounds.max - region;
            if ((high.x + high.y * 2) * Cryptforge.Art.FoundryMap.Axis <= FoundrySurface.RampStart) return;
            if (_material == null)
            {
                var shader = Resources.Load<Shader>("FoundryGround");
                if (shader == null) return;
                _material = new Material(shader) { name = "Foundry surface ground sprites (scene)" };
                _material.SetVector("_Surface", new Vector4(FoundrySurface.RampStart,
                    1 / (FoundrySurface.RampEnd - FoundrySurface.RampStart), FoundrySurface.Rise, Cryptforge.Art.FoundryMap.Axis));
            }
            _material.SetVector("_Region", region);
            Sprite source = renderer.sprite;
            if (!_grids.TryGetValue(source, out var grid))
            {
                grid = Sprite.Create(source.texture, source.rect, source.pivot / source.rect.size,
                    source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                grid.name = source.name + " (surface grid)";
                var points = new Vector2[(Divisions + 1) * (Divisions + 1)];
                var triangles = new ushort[Divisions * Divisions * 6];
                // OverrideGeometry accepts pixel coordinates in Sprite.rect space; Unity
                // applies the pivot and pixels-per-unit itself (unlike Sprite.vertices).
                Vector2 low = Vector2.zero, size = source.rect.size;
                int t = 0;
                for (int y = 0; y <= Divisions; y++)
                    for (int x = 0; x <= Divisions; x++)
                    {
                        int i = y * (Divisions + 1) + x;
                        points[i] = low + Vector2.Scale(size, new Vector2(x / (float)Divisions, y / (float)Divisions));
                        if (x == Divisions || y == Divisions) continue;
                        triangles[t++] = (ushort)i; triangles[t++] = (ushort)(i + 1); triangles[t++] = (ushort)(i + Divisions + 2);
                        triangles[t++] = (ushort)i; triangles[t++] = (ushort)(i + Divisions + 2); triangles[t++] = (ushort)(i + Divisions + 1);
                    }
                grid.OverrideGeometry(points, triangles);
                _grids.Add(source, grid);
            }
            _shown.Add(renderer); _sources.Add(source); _materials.Add(renderer.sharedMaterial);
            renderer.sprite = grid; renderer.sharedMaterial = _material;
            Bounds bounds = grid.bounds;
            bounds.Encapsulate(bounds.max + Vector3.up * (FoundrySurface.Rise / Mathf.Max(.001f, Mathf.Abs(renderer.transform.lossyScale.y))));
            renderer.localBounds = bounds;
        }

        internal void Restore()
        {
            for (int i = _shown.Count - 1; i >= 0; i--)
                if (_shown[i] != null)
                {
                    _shown[i].sprite = _sources[i]; _shown[i].sharedMaterial = _materials[i];
                    _shown[i].ResetLocalBounds();
                }
            _shown.Clear(); _sources.Clear(); _materials.Clear();
        }
        internal void Dispose()
        {
            Restore();
            foreach (var sprite in _grids.Values) Object.Destroy(sprite);
            _grids.Clear();
            if (_material != null) Object.Destroy(_material);
        }
    }
}
