using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Cryptforge.UI
{
    // The shader variants a scene's materials draw with, made ready before anything draws them (2026-11-11: the first
    // level-up's render waited about 6 ms beyond its own markers, and a variant's first use is one suspect). Every material
    // of every renderer and UI graphic in the scene, active or not, adds its shader's variant for the material's own
    // keywords to one ShaderVariantCollection, which is warmed. Nothing is drawn and nothing in the scene changes. Only the
    // perf probe calls it for now (TestResults/perf-probe-warmup.txt), to compare a run with it against one without.
    // Variants a draw picks for itself (a mask's clip, instancing) are not among them.
    public static class ShaderWarmup
    {
        public struct Result
        {
            public int Materials;
            public int Shaders;
            public int Variants;
            // Materials whose variant the collection would not take (a keyword set no variant has, a pass type it lacks).
            public int Refused;
            public double Milliseconds;
            public string ShaderNames;
        }

        public static Result WarmScene(Scene scene)
        {
            var watch = Stopwatch.StartNew();
            var materials = new HashSet<Material>();
            foreach (Renderer renderer in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.gameObject.scene != scene)
                    continue;
                foreach (Material material in renderer.sharedMaterials)
                    if (material != null && material.shader != null)
                        materials.Add(material);
            }
            foreach (Graphic graphic in Object.FindObjectsByType<Graphic>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // The graphic's own material, never materialForRendering: a mask's modifier would make a stencil material
                // here that nothing releases. The stencil is render state, not a variant.
                Material material = graphic.gameObject.scene == scene ? graphic.material : null;
                if (material != null && material.shader != null)
                    materials.Add(material);
            }

            var collection = new ShaderVariantCollection();
            var shaders = new SortedSet<string>(StringComparer.Ordinal);
            int refused = 0;
            foreach (Material material in materials)
            {
                shaders.Add(material.shader.name);
                if (!TryAdd(collection, material.shader, material.shaderKeywords))
                    refused++;
            }
            collection.WarmUp();
            var result = new Result
            {
                Materials = materials.Count,
                Shaders = shaders.Count,
                Variants = collection.variantCount,
                Refused = refused,
                ShaderNames = string.Join(", ", shaders)
            };
            Object.Destroy(collection);
            watch.Stop();
            result.Milliseconds = watch.Elapsed.TotalMilliseconds;
            return result;
        }

        // An unlit pass first (sprites, UI), then the forward base pass a lit shader draws in.
        private static bool TryAdd(ShaderVariantCollection collection, Shader shader, string[] keywords)
        {
            foreach (PassType pass in new[] { PassType.Normal, PassType.ForwardBase })
            {
                try
                {
                    collection.Add(new ShaderVariantCollection.ShaderVariant(shader, pass, keywords));
                    return true;
                }
                catch (ArgumentException)
                {
                }
            }
            return false;
        }
    }
}
