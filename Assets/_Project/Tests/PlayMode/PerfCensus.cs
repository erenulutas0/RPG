using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cryptforge.Combat;
using Cryptforge.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cryptforge.Tests
{
    // A census of what the gameplay camera sees at one frame, for PerfProbe, worked out from the scene rather than from
    // the renderer, so it holds in a batch run without graphics too: the renderers on show inside the phone's view, by
    // kind; their distinct textures and materials; how much of the screen the alpha-blended sprites and lines cover
    // (FullRect quads, so their bounds are what they fill); and an estimate of the sprite batches - the renderers in an
    // approximate draw order (sorting layer, order, then far to near by the floor they stand on, then within a sorting
    // group), counted as runs of one material and one texture, which is the most dynamic batching can merge. The
    // estimate is a floor, not a measurement; a graphics-enabled run's draw calls are the measurement.
    internal sealed class PerfCensus
    {
        internal sealed class Kind
        {
            public int Count;
            public float Coverage;
            public int Passes;
            public readonly HashSet<int> Textures = new HashSet<int>();
            public readonly HashSet<int> Materials = new HashSet<int>();
        }

        private struct Entry
        {
            public int Layer;
            public int Order;
            public float Depth;
            public int InnerLayer;
            public int InnerOrder;
            public float InnerDepth;
            public int Material;
            public int Texture;
            public int Passes;
            public bool Sprite;
        }

        public int Frame;
        public int Alive;
        public int Visible;
        public int VisibleSprites;
        public int VisibleMeshes;
        public int DistinctTextures;
        public int DistinctMaterials;
        // Runs of one material and one texture along the estimated draw order (sprites and lines), and every pass of every
        // visible renderer: the floor and the ceiling of the draws they make.
        public int SpriteRuns;
        public int SpriteDraws;
        public int AllPasses;
        public int EnemyBodies;
        public int EnemyBodyTextures;
        public int EnemyBodyChanges;
        public int UiGraphics;
        public float Coverage;
        public readonly SortedDictionary<string, Kind> Kinds = new SortedDictionary<string, Kind>();
        public string TopTextures = string.Empty;

        internal static PerfCensus Take(Camera camera, float aspect, int frame, int alive)
        {
            var census = new PerfCensus { Frame = frame, Alive = alive };
            float halfHeight = camera.orthographicSize;
            float halfWidth = halfHeight * aspect;
            Vector3 centre = camera.transform.position;
            Rect view = new Rect(centre.x - halfWidth, centre.y - halfHeight, halfWidth * 2f, halfHeight * 2f);
            float viewArea = view.width * view.height;
            var arena = Object.FindFirstObjectByType<ArenaView>();
            Transform arenaRoot = arena != null ? arena.transform : null;
            var chest = Object.FindFirstObjectByType<ChestSpawner>();
            SpriteRenderer chestBody = chest != null ? chest.Body : null;

            var entries = new List<Entry>();
            var bodies = new List<KeyValuePair<float, int>>();
            var textures = new Dictionary<int, int>();
            var textureNames = new Dictionary<int, string>();
            var materials = new HashSet<int>();
            var shared = new List<Material>();
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;
                var sprite = renderer as SpriteRenderer;
                if (sprite != null && sprite.sprite == null)
                    continue;
                Bounds bounds = renderer.bounds;
                Rect rect = Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
                if (!rect.Overlaps(view))
                    continue;
                string kind = KindOf(renderer, arenaRoot, chestBody);
                bool flat = sprite != null || renderer is LineRenderer || renderer is TrailRenderer;
                renderer.GetSharedMaterials(shared);
                int passes = 0;
                int materialId = 0;
                foreach (Material material in shared)
                {
                    if (material == null)
                        continue;
                    passes += Mathf.Max(1, material.passCount);
                    if (materialId == 0)
                        materialId = material.GetInstanceID();
                    materials.Add(material.GetInstanceID());
                }
                int textureId = 0;
                string textureName = null;
                if (sprite != null && sprite.sprite.texture != null)
                {
                    textureId = sprite.sprite.texture.GetInstanceID();
                    textureName = sprite.sprite.texture.name;
                }
                else if (shared.Count > 0 && shared[0] != null && shared[0].HasProperty("_MainTex"))
                {
                    Texture main = shared[0].GetTexture("_MainTex");
                    if (main != null)
                    {
                        textureId = main.GetInstanceID();
                        textureName = main.name;
                    }
                }
                if (textureId != 0)
                {
                    textures[textureId] = textures.TryGetValue(textureId, out int seen) ? seen + 1 : 1;
                    if (!textureNames.ContainsKey(textureId))
                        textureNames[textureId] = string.IsNullOrEmpty(textureName) ? "(unnamed)" : textureName;
                }

                float coverage = 0f;
                if (flat)
                {
                    float width = Mathf.Min(rect.xMax, view.xMax) - Mathf.Max(rect.xMin, view.xMin);
                    float height = Mathf.Min(rect.yMax, view.yMax) - Mathf.Max(rect.yMin, view.yMin);
                    if (width > 0f && height > 0f)
                        coverage = width * height / viewArea;
                }

                if (!census.Kinds.TryGetValue(kind, out Kind totals))
                    census.Kinds[kind] = totals = new Kind();
                totals.Count++;
                totals.Coverage += coverage;
                totals.Passes += passes;
                if (textureId != 0)
                    totals.Textures.Add(textureId);
                if (materialId != 0)
                    totals.Materials.Add(materialId);

                census.Visible++;
                census.AllPasses += passes;
                census.Coverage += coverage;
                if (flat)
                    census.VisibleSprites++;
                else
                    census.VisibleMeshes++;
                if (kind == "enemy body")
                    bodies.Add(new KeyValuePair<float, int>(renderer.transform.position.y, textureId));

                var entry = new Entry
                {
                    Material = materialId,
                    Texture = textureId,
                    Passes = passes,
                    Sprite = flat,
                    InnerLayer = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID),
                    InnerOrder = renderer.sortingOrder,
                    InnerDepth = -renderer.transform.position.y,
                };
                SortingGroup group = renderer.GetComponentInParent<SortingGroup>();
                if (group != null)
                {
                    entry.Layer = SortingLayer.GetLayerValueFromID(group.sortingLayerID);
                    entry.Order = group.sortingOrder;
                    entry.Depth = -group.transform.position.y;
                }
                else
                {
                    entry.Layer = entry.InnerLayer;
                    entry.Order = entry.InnerOrder;
                    entry.Depth = entry.InnerDepth;
                    entry.InnerLayer = 0;
                    entry.InnerOrder = 0;
                    entry.InnerDepth = 0f;
                }
                entries.Add(entry);
            }

            // Far to near, then the groups' own order; equal keys are put next to their material and texture, which flatters
            // the estimate: no draw order Unity picks can batch better.
            entries.Sort((a, b) =>
            {
                int c = a.Layer.CompareTo(b.Layer);
                if (c == 0) c = a.Order.CompareTo(b.Order);
                if (c == 0) c = a.Depth.CompareTo(b.Depth);
                if (c == 0) c = a.InnerLayer.CompareTo(b.InnerLayer);
                if (c == 0) c = a.InnerOrder.CompareTo(b.InnerOrder);
                if (c == 0) c = a.InnerDepth.CompareTo(b.InnerDepth);
                if (c == 0) c = a.Material.CompareTo(b.Material);
                if (c == 0) c = a.Texture.CompareTo(b.Texture);
                return c;
            });
            int lastMaterial = int.MinValue, lastTexture = int.MinValue;
            foreach (Entry entry in entries)
            {
                if (!entry.Sprite)
                    continue;
                census.SpriteDraws += entry.Passes;
                if (entry.Material != lastMaterial || entry.Texture != lastTexture)
                    census.SpriteRuns += entry.Passes;
                lastMaterial = entry.Material;
                lastTexture = entry.Texture;
            }

            // The enemies' bodies far to near, as their sorting groups put them: a body whose texture differs from the one
            // drawn before it cannot join its batch.
            bodies.Sort((a, b) => b.Key.CompareTo(a.Key));
            var bodyTextures = new HashSet<int>();
            for (int i = 0; i < bodies.Count; i++)
            {
                bodyTextures.Add(bodies[i].Value);
                if (i > 0 && bodies[i].Value != bodies[i - 1].Value)
                    census.EnemyBodyChanges++;
            }
            census.EnemyBodies = bodies.Count;
            census.EnemyBodyTextures = bodyTextures.Count;
            census.DistinctTextures = textures.Count;
            census.DistinctMaterials = materials.Count;

            var ranked = new List<KeyValuePair<int, int>>(textures);
            ranked.Sort((a, b) => b.Value.CompareTo(a.Value));
            var top = new StringBuilder();
            for (int i = 0; i < ranked.Count && i < 8; i++)
                top.Append(i > 0 ? ", " : string.Empty).Append(textureNames[ranked[i].Key]).Append(" x").Append(ranked[i].Value);
            census.TopTextures = top.ToString();

            foreach (var graphic in Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None))
                if (graphic.isActiveAndEnabled && graphic.canvasRenderer != null && !graphic.canvasRenderer.cull &&
                    graphic.color.a > 0f)
                    census.UiGraphics++;
            return census;
        }

        // What a renderer belongs to, from the objects the game names and the views it carries.
        private static string KindOf(Renderer renderer, Transform arenaRoot, SpriteRenderer chestBody)
        {
            Transform self = renderer.transform;
            if (renderer.TryGetComponent(out ContactShadowView _))
                return "contact shadow";
            if (chestBody != null && renderer == chestBody)
                return "chest";
            string own = self.name;
            if (own == AbilityRingView.RingName)
                return "ability ring";
            for (Transform part = self; part != null; part = part.parent)
            {
                string name = part.name;
                if (name == "Health Bar")
                    return "enemy bar";
                if (name == "Burn Flames")
                    return "burn flame";
                if (name.StartsWith("Number ", System.StringComparison.Ordinal))
                    return "damage number";
                if (name == "Combat Effects")
                    return "combat effect";
                if (name == "Run Feedback")
                    return "run feedback";
                if (name == "Ground Reward")
                    return "ground drop";
                if (name == "Warden Mark")
                    return "warden mark";
                if (part.TryGetComponent(out EnemyLookView _))
                    return own == "Flash" ? "hit flash" : "enemy body";
                if (part.TryGetComponent(out HeroLookView _))
                    return own == "Flash" || own == "Evolution Flash" || own == RenderedCharacterAnimator.FlashName
                        ? "hit flash" : "hero";
                if (arenaRoot != null && part == arenaRoot)
                    return renderer is MeshRenderer ? "arena mesh" : "arena sprite";
            }
            return renderer is MeshRenderer ? "other mesh" : "other";
        }

        internal void AppendTo(StringBuilder line)
        {
            line.Append("visible ").Append(Visible).Append(" (").Append(VisibleSprites).Append(" sprites and lines, ")
                .Append(VisibleMeshes).Append(" meshes) at ").Append(Alive).Append(" enemies alive; ")
                .Append(DistinctTextures).Append(" textures, ").Append(DistinctMaterials).Append(" materials; ")
                .Append("sprite draws ").Append(SpriteDraws).Append(", estimated sprite batches >= ").Append(SpriteRuns)
                .Append("; all passes ").Append(AllPasses).Append("; enemy bodies ").Append(EnemyBodies).Append(" on ")
                .Append(EnemyBodyTextures).Append(" textures, ").Append(EnemyBodyChanges)
                .Append(" texture changes far to near; alpha coverage ").Append(Coverage.ToString("0.00", CultureInfo.InvariantCulture))
                .Append(" screens; UI graphics ").Append(UiGraphics).AppendLine();
            foreach (var pair in Kinds)
            {
                Kind kind = pair.Value;
                line.Append("      ").Append(pair.Key.PadRight(15)).Append(' ').Append(kind.Count.ToString().PadLeft(4))
                    .Append(" renderers, ").Append(kind.Textures.Count.ToString().PadLeft(3)).Append(" textures, ")
                    .Append(kind.Materials.Count.ToString().PadLeft(2)).Append(" materials, ")
                    .Append(kind.Passes.ToString().PadLeft(4)).Append(" passes, coverage ")
                    .Append(kind.Coverage.ToString("0.00", CultureInfo.InvariantCulture)).AppendLine(" screens");
            }
            line.Append("      most drawn textures: ").AppendLine(TopTextures);
        }
    }
}
