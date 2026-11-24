using Cryptforge.Art;
using Cryptforge.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // Each painted foundry enemy has a copy per themed floor (EnemyArtSet.ForTheme): frosted on floor 2 (docs/49) and
    // bloomed on floor 3 (docs/51, Tools/ArtReview/GenerateGloamEnemyArt.py), pose for pose with the painted set, the
    // white flashes shared. A copy has no copies of its own, and the foundry has none. Floor 3's own enemies (the Cinder
    // Bloom, the Pollen Wraith, the Thornling, and its gate boss Rootmother Myrr, docs/64) are born in the garden's
    // palette and have no copies at all.
    public sealed class EnemyThemeArtTests
    {
        const string Art = "Assets/_Project/Art/Characters/";
        static readonly string[] Sets =
        {
            "SlagBrute/SlagBruteArtSet", "Runner/RunnerArtSet", "Tank/TankArtSet", "GruntCaptain/GruntCaptainArtSet",
            "CinderMite/CinderMiteArtSet", "ForgeWarden/ForgeWardenArtSet",
        };
        // Floor 3's own enemies (docs/52 section 2 rule 6): drawn in the garden's palette from the start, so they have
        // no themed copies at all - the set is its own look on every floor, and no Ice/ or Gloam/ folder holds copies.
        static readonly string[] BornOnFloorThree =
        {
            "CinderBloom/CinderBloomArtSet", "PollenWraith/PollenWraithArtSet", "Thornling/ThornlingArtSet",
            "RootmotherMyrr/RootmotherMyrrArtSet",
        };

        [Test]
        public void FloorThreesOwnEnemiesHaveNoThemedCopies()
        {
            foreach (string name in BornOnFloorThree)
            {
                Assert.That(System.Array.IndexOf(Sets, name), Is.LessThan(0), name + " is not a painted foundry set.");
                var own = AssetDatabase.LoadAssetAtPath<EnemyArtSet>(Art + name + ".asset");
                Assert.That(own, Is.Not.Null, name);
                Assert.That(own.IsValid, Is.True, name);
                foreach (FloorTheme theme in new[] { FloorTheme.Foundry, FloorTheme.Ice, FloorTheme.Gloam })
                    Assert.That(own.ForTheme(theme), Is.Null, name + ": " + theme);
                Assert.That(own.Frosted, Is.Null, name);
                string folder = Art + name.Substring(0, name.IndexOf('/'));
                Assert.That(AssetDatabase.IsValidFolder(folder + "/Ice"), Is.False, name);
                Assert.That(AssetDatabase.IsValidFolder(folder + "/Gloam"), Is.False, name);
            }
        }

        [Test]
        public void EveryPaintedEnemyHasAFrostedAndABloomedCopyPoseForPose()
        {
            foreach (string name in Sets)
            {
                var painted = AssetDatabase.LoadAssetAtPath<EnemyArtSet>(Art + name + ".asset");
                Assert.That(painted, Is.Not.Null, name);
                EnemyArtSet frosted = painted.ForTheme(FloorTheme.Ice);
                EnemyArtSet bloomed = painted.ForTheme(FloorTheme.Gloam);
                Assert.That(painted.ForTheme(FloorTheme.Foundry), Is.Null, name + ": the foundry wears the painted set.");
                Assert.That(frosted, Is.SameAs(painted.Frosted), name + ": Frosted is the ice floor's copy.");
                Assert.That(bloomed, Is.Not.Null, name + " has a bloomed copy.");
                Assert.That(bloomed, Is.Not.SameAs(frosted), name);
                Assert.That(AssetDatabase.GetAssetPath(bloomed), Is.EqualTo(Art + name.Replace("/", "/Gloam/").Replace("ArtSet", "GloamArtSet") + ".asset"));
                foreach (FloorTheme theme in new[] { FloorTheme.Foundry, FloorTheme.Ice, FloorTheme.Gloam })
                    Assert.That(bloomed.ForTheme(theme), Is.Null, name + ": a copy has no copies of its own.");
                Assert.That(bloomed.HasWindup, Is.EqualTo(painted.HasWindup), name);
                Assert.That(bloomed.HasEnragedFrames, Is.EqualTo(painted.HasEnragedFrames), name);
                Assert.That(bloomed.HasFall, Is.EqualTo(painted.HasFall), name);
                Assert.That(bloomed.HasStrikeWindup, Is.EqualTo(painted.HasStrikeWindup), name);
                Assert.That(bloomed.HasOwnDeath, Is.EqualTo(painted.HasOwnDeath), name);
                Assert.That(bloomed.VisualTop, Is.EqualTo(painted.VisualTop).Within(1e-4f), name + ": the bar stands where it stood.");
                int frames = painted.HasPassingFrames ? 6 : 4;
                for (int i = 0; i < frames; i++)
                    foreach (bool rear in new[] { false, true })
                    {
                        EnemyArtSet.Frame own = painted.GetFrame(i, rear), copy = bloomed.GetFrame(i, rear);
                        Assert.That(copy.Body, Is.Not.SameAs(own.Body), name + " frame " + i);
                        Assert.That(copy.Body, Is.Not.SameAs(frosted.GetFrame(i, rear).Body), name + " frame " + i);
                        Assert.That(copy.Flash, Is.SameAs(own.Flash), name + " frame " + i + ": the flash is shared.");
                        Assert.That(copy.Body.name, Is.EqualTo(own.Body.name), name + " frame " + i);
                        Assert.That(AssetDatabase.GetAssetPath(copy.Body), Does.Contain("/Gloam/"), name + " frame " + i);
                    }
            }
        }
    }
}
