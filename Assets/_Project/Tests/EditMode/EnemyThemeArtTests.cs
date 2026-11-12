using Cryptforge.Art;
using Cryptforge.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // Each painted foundry enemy has a copy per themed floor (EnemyArtSet.ForTheme): frosted on floor 2 (docs/49) and
    // bloomed on floor 3 (docs/51, Tools/ArtReview/GenerateGloamEnemyArt.py), pose for pose with the painted set, the
    // white flashes shared. A copy has no copies of its own, and the foundry has none.
    public sealed class EnemyThemeArtTests
    {
        const string Art = "Assets/_Project/Art/Characters/";
        static readonly string[] Sets =
        {
            "SlagBrute/SlagBruteArtSet", "Runner/RunnerArtSet", "Tank/TankArtSet", "GruntCaptain/GruntCaptainArtSet",
            "CinderMite/CinderMiteArtSet", "ForgeWarden/ForgeWardenArtSet",
        };

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
