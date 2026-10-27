using System;
using System.Collections.Generic;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The cut-out puppet's code-drawn stand-in parts: a rig that hangs together, stands on its soles at the Vanguard's
    // height, holds its weapons at the hip and reads in two tones a facing.
    public sealed class PuppetArtTests
    {
        private static readonly PuppetBone[] Cut =
        {
            PuppetBone.Pelvis, PuppetBone.Torso, PuppetBone.Head, PuppetBone.Cape, PuppetBone.ThighLeft, PuppetBone.ShinLeft,
            PuppetBone.ThighRight, PuppetBone.ShinRight, PuppetBone.UpperArmLeft, PuppetBone.ForearmLeft, PuppetBone.HandLeft,
            PuppetBone.UpperArmRight, PuppetBone.ForearmRight, PuppetBone.HandRight
        };

        [TestCase(false)]
        [TestCase(true)]
        public void EveryPartHangsFromAnEarlierPartAndEveryLimbIsCut(bool front)
        {
            PuppetPlaceholderPart[] parts = PuppetPlaceholderArt.Draw(front);
            Assert.That(parts.Length, Is.EqualTo(PuppetPlaceholderArt.PartCount));
            var bones = new HashSet<PuppetBone>();
            for (int i = 0; i < parts.Length; i++)
            {
                PuppetPlaceholderPart part = parts[i];
                Assert.That(part.Parent, Is.InRange(-1, i - 1), part.Name);
                Assert.That(bones.Add(part.Bone), Is.True, part.Name + " is the only part on its bone.");
                Assert.That(part.Canvas.CountVisible(), Is.GreaterThan(8), part.Name);
                Assert.That(part.Canvas.Contains(part.PivotX, part.PivotY), Is.True, part.Name + ": the pivot is on the canvas.");
                Assert.That(part.Canvas.Get(part.PivotX, part.PivotY).A, Is.GreaterThan(0), part.Name + " turns about itself.");
            }
            CollectionAssert.AreEquivalent(Cut, bones);
            Assert.That(parts[PuppetPlaceholderArt.Pelvis].Parent, Is.EqualTo(-1), "Only the pelvis hangs from the feet.");
            Assert.That(parts[PuppetPlaceholderArt.HandRight].Bone, Is.EqualTo(PuppetBone.HandRight));
            Assert.That(parts[PuppetPlaceholderArt.HandLeft].Bone, Is.EqualTo(PuppetBone.HandLeft));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TheStandingFigureStandsOnItsSolesAtTheVanguardsHeight(bool front)
        {
            PuppetPlaceholderPart[] parts = PuppetPlaceholderArt.Draw(front);
            float[] x = new float[parts.Length], y = new float[parts.Length];
            float lowest = float.MaxValue, highest = float.MinValue;
            for (int i = 0; i < parts.Length; i++)
            {
                PuppetPlaceholderPart part = parts[i];
                x[i] = part.JointX + (part.Parent >= 0 ? x[part.Parent] : 0f);
                y[i] = part.JointY + (part.Parent >= 0 ? y[part.Parent] : 0f);
                Rows(part.Canvas, out int bottom, out int top);
                // The pivot is a texel's centre: a row's lower edge is half a texel below the row's own centre.
                lowest = Math.Min(lowest, y[i] + bottom - part.PivotY - 0.5f);
                highest = Math.Max(highest, y[i] + top - part.PivotY + 0.5f);
            }
            Assert.That(lowest, Is.EqualTo(0f).Within(0.01f), "The outline under the soles ends on the floor line.");
            Assert.That(highest, Is.InRange(38f, 44f), "About the Vanguard's 1.3 units.");

            int hand = PuppetPlaceholderArt.HandRight;
            float grip = y[hand] + PuppetPlaceholderArt.GripY;
            Assert.That(grip, Is.InRange(12f, 17f), "The weapon hand hangs at the hip, where HeroArt's weapons were drawn.");
            float shoulder = y[PuppetPlaceholderArt.UpperArmRight];
            Assert.That(shoulder, Is.GreaterThan(grip + 8f));
            Assert.That(y[PuppetPlaceholderArt.ThighRight], Is.EqualTo(y[PuppetPlaceholderArt.ThighLeft]), "Level hips.");
            Assert.That(x[PuppetPlaceholderArt.ThighRight] * x[PuppetPlaceholderArt.ThighLeft], Is.LessThan(0f),
                "The legs stand either side of the centre line.");
        }

        [Test]
        public void EachFistHoldsItsGripInsideItself()
        {
            foreach (bool front in new[] { false, true })
            {
                PuppetPlaceholderPart[] parts = PuppetPlaceholderArt.Draw(front);
                foreach (int index in new[] { PuppetPlaceholderArt.HandRight, PuppetPlaceholderArt.HandLeft })
                {
                    PuppetPlaceholderPart hand = parts[index];
                    int gx = hand.PivotX + (int)PuppetPlaceholderArt.GripX;
                    int gy = hand.PivotY + (int)PuppetPlaceholderArt.GripY;
                    Assert.That(hand.Canvas.Get(gx, gy).IsOpaque, Is.True, hand.Name);
                    Assert.That(hand.Canvas.Get(gx, gy), Is.Not.EqualTo(PixelPalette.Outline), hand.Name + ": inside the outline.");
                }
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TheNearArmDrawsOverTheTorsoAndTheFarLimbsUnderIt(bool front)
        {
            PuppetPlaceholderPart[] parts = PuppetPlaceholderArt.Draw(front);
            int torso = parts[PuppetPlaceholderArt.Torso].Sorting;
            foreach (int near in new[] { PuppetPlaceholderArt.UpperArmRight, PuppetPlaceholderArt.ForearmRight, PuppetPlaceholderArt.HandRight })
                Assert.That(parts[near].Sorting, Is.GreaterThan(torso), parts[near].Name);
            foreach (int far in new[] { PuppetPlaceholderArt.UpperArmLeft, PuppetPlaceholderArt.ForearmLeft, PuppetPlaceholderArt.HandLeft,
                PuppetPlaceholderArt.ThighLeft, PuppetPlaceholderArt.ShinLeft })
                Assert.That(parts[far].Sorting, Is.LessThan(torso), parts[far].Name);
            Assert.That(PuppetPlaceholderArt.RightItemSorting, Is.InRange(parts[PuppetPlaceholderArt.ForearmRight].Sorting + 1,
                parts[PuppetPlaceholderArt.HandRight].Sorting - 2), "The weapon sits under the fist, its orb too, over the forearm.");
            Assert.That(PuppetPlaceholderArt.LeftItemSorting, Is.GreaterThan(parts[PuppetPlaceholderArt.HandLeft].Sorting));
            Assert.That(PuppetPlaceholderArt.LeftItemSorting, Is.LessThan(torso), "The off-hand item hides behind the body.");
            // Seen from behind the cape covers the back; from the front it hangs behind everything.
            int cape = parts[PuppetPlaceholderArt.Cape].Sorting;
            if (front)
                Assert.That(cape, Is.LessThan(parts[PuppetPlaceholderArt.UpperArmLeft].Sorting));
            else
                Assert.That(cape, Is.GreaterThan(torso));
        }

        [Test]
        public void EveryPartIsOutlinedAndEachFacingReadsInTwoTones()
        {
            foreach (bool front in new[] { false, true })
            {
                PuppetPlaceholderPart[] parts = PuppetPlaceholderArt.Draw(front);
                Rgba near = front ? PuppetPlaceholderArt.FrontNear : PuppetPlaceholderArt.RearNear;
                Rgba far = front ? PuppetPlaceholderArt.FrontFar : PuppetPlaceholderArt.RearFar;
                foreach (PuppetPlaceholderPart part in parts)
                    Assert.That(Count(part.Canvas, PixelPalette.Outline), Is.GreaterThan(6), part.Name + " is outlined.");
                foreach (int index in new[] { PuppetPlaceholderArt.Torso, PuppetPlaceholderArt.ThighRight, PuppetPlaceholderArt.UpperArmRight })
                {
                    Assert.That(Count(parts[index].Canvas, near), Is.GreaterThan(0), parts[index].Name + " in the near tone.");
                    Assert.That(Count(parts[index].Canvas, far), Is.Zero, parts[index].Name);
                }
                foreach (int index in new[] { PuppetPlaceholderArt.Cape, PuppetPlaceholderArt.ThighLeft, PuppetPlaceholderArt.UpperArmLeft })
                {
                    Assert.That(Count(parts[index].Canvas, far), Is.GreaterThan(0), parts[index].Name + " in the far tone.");
                    Assert.That(Count(parts[index].Canvas, near), Is.Zero, parts[index].Name);
                }
                // Shaded from the upper left: each limb has lighter and darker texels than its tone.
                PixelCanvas thigh = parts[PuppetPlaceholderArt.ThighRight].Canvas;
                Assert.That(Count(thigh, near.Lerp(Rgba.White, 0.3f)), Is.GreaterThan(0));
                Assert.That(Count(thigh, near.Scale(0.65f)), Is.GreaterThan(0));
            }
            Assert.That(PuppetPlaceholderArt.FrontNear, Is.Not.EqualTo(PuppetPlaceholderArt.RearNear), "A capture shows the turn.");
            Assert.That(PuppetPlaceholderArt.FrontFar, Is.Not.EqualTo(PuppetPlaceholderArt.RearFar));
        }

        [Test]
        public void EachDrawGivesFreshCanvases()
        {
            PuppetPlaceholderPart[] first = PuppetPlaceholderArt.Draw(false);
            PuppetPlaceholderPart[] second = PuppetPlaceholderArt.Draw(false);
            Assert.That(second[0].Canvas, Is.Not.SameAs(first[0].Canvas), "The caller owns what it draws.");
            CollectionAssert.AreEqual(first[PuppetPlaceholderArt.Head].Canvas.Pixels, second[PuppetPlaceholderArt.Head].Canvas.Pixels);
            Assert.That(PuppetPlaceholderArt.Draw(true)[PuppetPlaceholderArt.Head].Canvas.Pixels,
                Is.Not.EqualTo(first[PuppetPlaceholderArt.Head].Canvas.Pixels), "The front head shows a face.");
        }

        private static void Rows(PixelCanvas canvas, out int bottom, out int top)
        {
            bottom = int.MaxValue;
            top = int.MinValue;
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    if (canvas.Get(x, y).A == 0)
                        continue;
                    bottom = Math.Min(bottom, y);
                    top = Math.Max(top, y);
                }
            }
        }

        private static int Count(PixelCanvas canvas, Rgba colour)
        {
            int count = 0;
            for (int i = 0; i < canvas.Pixels.Length; i++)
            {
                if (canvas.Pixels[i] == colour)
                    count++;
            }
            return count;
        }
    }
}
