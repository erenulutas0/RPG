using Cryptforge.Art;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    // A puppet facing without painted soles takes its sole from the shin's sprite (PuppetRig.Leg): heel behind and toe
    // ahead in the rig's own frame whichever way the sheet was painted, so a left-painted facing rolls its boot heel to
    // toe as a right-painted one does.
    public sealed class PuppetRigTests
    {
        [Test]
        public void ADefaultSolePutsTheToeAheadWhicheverWayTheSheetWasPainted()
        {
            var texture = new Texture2D(20, 20);
            // A shin 20 texels wide and 10 high hung from its top centre: its bottom runs -0.1..0.1 at 100 a unit.
            Sprite shin = Sprite.Create(texture, new Rect(0, 0, 20, 10), new Vector2(.5f, 1f), 100f);
            Sprite block = Sprite.Create(texture, new Rect(0, 0, 10, 10), new Vector2(.5f, .5f), 100f);
            var material = new Material(Shader.Find("Sprites/Default"));
            var parent = new GameObject("Puppet rig test");
            try
            {
                foreach (bool facesLeft in new[] { false, true })
                {
                    var facing = new PuppetArtSet.Facing
                    {
                        Parts = new[]
                        {
                            new PuppetArtSet.Part { Name = "Pelvis", Sprite = block, Flash = block, Parent = -1, Bone = PuppetBone.Pelvis },
                            new PuppetArtSet.Part { Name = "Thigh", Sprite = block, Flash = block, Parent = 0, Joint = new Vector2(.05f, -.05f), Bone = PuppetBone.ThighRight },
                            new PuppetArtSet.Part { Name = "Shin", Sprite = shin, Flash = shin, Parent = 1, Joint = new Vector2(0f, -.2f), Bone = PuppetBone.ShinRight }
                        },
                        RightHand = 0,
                        LeftHand = 0,
                        FacesLeft = facesLeft
                    };
                    PuppetRig rig = PuppetRig.Build(facing, parent.transform, facesLeft ? "Left" : "Right", material, 0, 0);
                    PuppetLeg leg = rig.Legs.Right;
                    Assert.That(leg.ToeX, Is.GreaterThan(leg.HeelX), (facesLeft ? "Left" : "Right") + "-painted: the toe leads.");
                    Assert.That(leg.ToeX - leg.HeelX, Is.EqualTo(.18f).Within(1e-4f), "The shin's bottom a texel in from each end.");
                    Assert.That(leg.HeelY, Is.EqualTo(-.09f).Within(1e-4f));
                }
            }
            finally
            {
                Object.DestroyImmediate(parent);
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(shin);
                Object.DestroyImmediate(block);
                Object.DestroyImmediate(texture);
            }
        }
    }
}
