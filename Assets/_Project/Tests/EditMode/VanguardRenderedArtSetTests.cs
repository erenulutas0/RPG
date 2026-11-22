using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The Blender Vanguard's imported sprites (docs/08, 2026-11-22; rendered by Tools/ArtReview/BuildBlenderVanguard.py,
    // look pass 2, packed by PackVanguardSprites.py), as CinderArcanistArtSetTests pins Ilya's: the set is complete in
    // all eight facings, the optional dash (docs/57's Shield Rush) included, every frame is cut from the Vanguard's
    // CHR_VanguardBlender atlases at 100 pixels a unit with its pivot on the floor, the atlases import as Ilya's do inside
    // the measured memory, the portrait is the south idle, and the Vanguard wears the set and starts with its Sword alone
    // (the owner, 2026-11-22). A set without the dash stays valid; a dash drawn in only some facings does not.
    public sealed class VanguardRenderedArtSetTests
    {
        private const string Folder = "Assets/_Project/Art/Characters/Vanguard";
        private const string SetPath = Folder + "/VanguardRenderedArtSet.asset";
        // 2026-11-22: 6,025,472 texels as ASTC 4x4 (8 bits a texel; 5.75 MB) for the eight facings and the dash - the
        // sword held out widens the frames past Ilya's 4.20 MB. A little headroom, so a repack that grows is noticed.
        private const long BudgetBytes = 6_200_000L;
        // The most texels a standing frame's lowest pixels lie under the floor pivot: the near sabaton, and the Forge
        // Burst's blade driven into the floor ahead of a knight facing the camera.
        private const float FeetBelowPivot = 30f;

        private static readonly Dictionary<RenderedMotion, int> Frames = new Dictionary<RenderedMotion, int>
        {
            { RenderedMotion.Idle, 8 }, { RenderedMotion.Walk, 8 }, { RenderedMotion.WalkStrike, 8 },
            { RenderedMotion.Strike, 5 }, { RenderedMotion.Cast, 5 }, { RenderedMotion.Hit, 3 }, { RenderedMotion.Death, 5 },
            { RenderedMotion.Dash, 4 }
        };

        private static RenderedCharacterArtSet Set()
        {
            var set = AssetDatabase.LoadAssetAtPath<RenderedCharacterArtSet>(SetPath);
            Assert.That(set, Is.Not.Null, SetPath);
            return set;
        }

        // A copy of the set with only some of its clips and timings, the rest as the asset has them.
        private static RenderedCharacterArtSet Copy(RenderedCharacterArtSet set, Predicate<RenderedCharacterArtSet.Clip> keep,
            Predicate<RenderedCharacterArtSet.Timing> keepTiming = null)
        {
            const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
            var copy = ScriptableObject.CreateInstance<RenderedCharacterArtSet>();
            FieldInfo stride = typeof(RenderedCharacterArtSet).GetField("_strideLength", Private);
            stride.SetValue(copy, stride.GetValue(set));
            FieldInfo timings = typeof(RenderedCharacterArtSet).GetField("_timings", Private);
            var all = (RenderedCharacterArtSet.Timing[])timings.GetValue(set);
            timings.SetValue(copy, keepTiming == null ? all : Array.FindAll(all, keepTiming));
            FieldInfo clips = typeof(RenderedCharacterArtSet).GetField("_clips", Private);
            clips.SetValue(copy, Array.FindAll((RenderedCharacterArtSet.Clip[])clips.GetValue(set), keep));
            return copy;
        }

        private static HeroDefinition Hero(string name)
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>($"Assets/_Project/Data/Heroes/{name}.asset");
            Assert.That(hero, Is.Not.Null, name);
            return hero;
        }

        [Test]
        public void TheSetHasEveryMotionAndTheDashInEveryFacing()
        {
            RenderedCharacterArtSet set = Set();
            Assert.That(set.IsValid, Is.True);
            Assert.That(set.DrawsWest, Is.True, "All eight facings are rendered: nothing is mirrored.");
            Assert.That(set.Draws(RenderedMotion.Dash), Is.True, "The Shield Rush is drawn.");
            Assert.That(set.StrideLength, Is.EqualTo(1.0f).Within(1e-5f), "The render's stride (pass 2's longer legs).");
            var seen = new HashSet<Sprite>();
            foreach (KeyValuePair<RenderedMotion, int> clip in Frames)
            {
                Assert.That(set.FrameCount(clip.Key), Is.EqualTo(clip.Value), clip.Key.ToString());
                for (int f = 0; f < RenderedCharacterFrames.FacingCount; f++)
                    for (int k = 0; k < clip.Value; k++)
                    {
                        Sprite sprite = set.Frame(clip.Key, (RenderedFacing)f, k);
                        Assert.That(sprite, Is.Not.Null, $"{clip.Key} {(RenderedFacing)f} {k}");
                        Assert.That(seen.Add(sprite), Is.True, $"{clip.Key} {(RenderedFacing)f} {k} is its own frame.");
                        Assert.That(sprite.name, Is.EqualTo($"vanguard_{Clip(clip.Key)}_{Facing((RenderedFacing)f)}_{k}"),
                            "The packer's frame for that motion, facing and step.");
                    }
            }
            Assert.That(seen.Count, Is.EqualTo(368), "46 frames in each of the eight facings.");
            RenderedClipTiming[] timings = set.Timings();
            Assert.That(timings.Length, Is.EqualTo(RenderedCharacterFrames.MotionCount));
            Assert.That(timings[(int)RenderedMotion.Idle].Loops, Is.True);
            Assert.That(timings[(int)RenderedMotion.Walk].Loops, Is.True);
            Assert.That(timings[(int)RenderedMotion.Death].Loops, Is.False, "Death holds its last frame.");
            Assert.That(timings[(int)RenderedMotion.Dash].Loops, Is.False, "The Rush plays once.");
            Assert.That(timings[(int)RenderedMotion.Dash].Seconds, Is.EqualTo(0.3f).Within(0.01f), "docs/57: 4 units in 0.3 s.");
            foreach (RenderedMotion timed in new[] { RenderedMotion.Idle, RenderedMotion.Strike, RenderedMotion.Cast,
                         RenderedMotion.Hit, RenderedMotion.Death, RenderedMotion.Dash })
                Assert.That(timings[(int)timed].FramesPerSecond, Is.GreaterThan(0f), timed.ToString());
        }

        [Test]
        public void EveryFrameIsCutFromTheAtlasesAtTheGamesScaleWithItsFeetOnThePivot()
        {
            RenderedCharacterArtSet set = Set();
            foreach (KeyValuePair<RenderedMotion, int> clip in Frames)
                for (int f = 0; f < RenderedCharacterFrames.FacingCount; f++)
                    for (int k = 0; k < clip.Value; k++)
                    {
                        Sprite sprite = set.Frame(clip.Key, (RenderedFacing)f, k);
                        string at = $"{clip.Key} {(RenderedFacing)f} {k}";
                        Assert.That(AssetDatabase.GetAssetPath(sprite.texture), Does.StartWith(Folder + "/CHR_VanguardBlender_"), at);
                        Assert.That(sprite.pixelsPerUnit, Is.EqualTo(100f), at);
                        Assert.That(sprite.packed, Is.False, at);
                        Rect rect = sprite.rect;
                        Assert.That(sprite.pivot.x, Is.InRange(0f, rect.width), at);
                        Assert.That(sprite.pivot.y, Is.InRange(0f, rect.height * 0.5f), at);
                        if (clip.Key != RenderedMotion.Death)
                            Assert.That(sprite.pivot.y, Is.InRange(1f, FeetBelowPivot), at);
                        // About 1.25 units to the crest standing; the Burst's raised sword reaches 2.
                        Assert.That(rect.height / sprite.pixelsPerUnit, Is.InRange(0.5f, 2.1f), at);
                    }
        }

        [Test]
        public void TheHeroPortraitIsTheSouthIdle()
        {
            // Hero_Vanguard._portrait names the sprite by its atlas's GUID; the packer keeps it on that atlas.
            Assert.That(Hero("Hero_Vanguard").Portrait, Is.SameAs(Set().Frame(RenderedMotion.Idle, RenderedFacing.South, 0)));
        }

        // The dash is optional: Ilya's set has none and stays valid, so does this one without it; a dash in only some of
        // the facings the set draws, or one without its timing, is refused rather than shown half.
        [Test]
        public void ASetWithoutItsDashStaysValidAndAHalfDrawnDashIsRefused()
        {
            RenderedCharacterArtSet set = Set();
            RenderedCharacterArtSet noDash = Copy(set, clip => clip.Motion != RenderedMotion.Dash,
                timing => timing.Motion != RenderedMotion.Dash);
            RenderedCharacterArtSet partial = Copy(set, clip => !(clip.Motion == RenderedMotion.Dash &&
                                                                  clip.Facing == RenderedFacing.SouthWest));
            RenderedCharacterArtSet untimed = Copy(set, clip => true, timing => timing.Motion != RenderedMotion.Dash);
            RenderedCharacterArtSet five = Copy(set, clip => !RenderedCharacterFrames.IsWest(clip.Facing));
            try
            {
                Assert.That(noDash.IsValid, Is.True, "A set without the dash is whole.");
                Assert.That(noDash.DrawsWest, Is.True);
                Assert.That(noDash.Draws(RenderedMotion.Dash), Is.False);
                Assert.That(noDash.Timings()[(int)RenderedMotion.Dash].Frames, Is.Zero, "So the player shows the walk.");
                Assert.That(partial.IsValid, Is.False, "A dash missing from a facing the set draws.");
                Assert.That(untimed.IsValid, Is.False, "A dash without its timing.");
                Assert.That(five.IsValid, Is.True, "Five east facings, the dash's among them, are a whole set.");
                Assert.That(five.DrawsWest, Is.False);
                Assert.That(five.Draws(RenderedMotion.Dash), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(noDash);
                UnityEngine.Object.DestroyImmediate(partial);
                UnityEngine.Object.DestroyImmediate(untimed);
                UnityEngine.Object.DestroyImmediate(five);
            }
        }

        [Test]
        public void TheAtlasesImportLikeIlyasInsideTheMeasuredMemory()
        {
            long bytes = 0;
            string[] guids = AssetDatabase.FindAssets("CHR_VanguardBlender_ t:Texture2D", new[] { Folder });
            Assert.That(guids.Length, Is.EqualTo(6), "Six pages.");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                Assert.That(importer.isReadable, Is.False, path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), path);
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                Assert.That(android.overridden, Is.True, path);
                Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4), path);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture.width, Is.LessThanOrEqualTo(android.maxTextureSize), path + " is never downscaled.");
                Assert.That(texture.height, Is.LessThanOrEqualTo(android.maxTextureSize), path);
                bytes += (long)((texture.width + 3) / 4 * 4) * ((texture.height + 3) / 4 * 4);
            }
            Assert.That(bytes, Is.LessThanOrEqualTo(BudgetBytes), "The Vanguard's frames as ASTC 4x4.");
        }

        // The owner, 2026-11-22: the Vanguard wears this set (no longer the painted frames or the puppet, both kept in the
        // repo behind development files) and always starts with its sword and buckler, as Ilya with its Staff.
        [Test]
        public void TheVanguardWearsTheSetAndStartsWithItsSwordAlone()
        {
            HeroDefinition vanguard = Hero("Hero_Vanguard");
            Assert.That(vanguard.RenderedArt, Is.SameAs(Set()));
            Assert.That(vanguard.RenderedArt, Is.Not.SameAs(Hero("Hero_CinderArcanist").RenderedArt));
            Assert.That(vanguard.StartingWeapon.Id, Is.EqualTo("weapon_sword"));
            Assert.That(vanguard.StartsWithItsOwnWeaponOnly, Is.True);
            Assert.That(AssetDatabase.LoadAssetAtPath<VanguardArtSet>(Folder + "/VanguardArtSet.asset"), Is.Not.Null,
                "The painted frames stay in the repo.");
        }

        // The packer's names: vanguard_<clip>_<facing>_<frame>, as its frames.json lists them; the dash is the render's
        // shield-rush.
        private static string Clip(RenderedMotion motion) => motion switch
        {
            RenderedMotion.WalkStrike => "walk-strike",
            RenderedMotion.Dash => "shield-rush",
            _ => motion.ToString().ToLowerInvariant()
        };

        private static string Facing(RenderedFacing facing) => facing switch
        {
            RenderedFacing.SouthEast => "south-east",
            RenderedFacing.NorthEast => "north-east",
            RenderedFacing.NorthWest => "north-west",
            RenderedFacing.SouthWest => "south-west",
            _ => facing.ToString().ToLowerInvariant()
        };
    }
}
