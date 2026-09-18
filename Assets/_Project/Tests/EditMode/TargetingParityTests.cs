using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The scene's Targeting and the Descent simulation's Acquire pick the nearest living enemy, the earlier slot on a
    // tie, and until 2026-09-18 each squared its own floor offset. Under the Editor's JIT those two copies of the same
    // expression, compiled in two assemblies, disagreed in the last bit about a near-tie the movement budget produced -
    // a Grunt 1.4 units straight across and a Mite 1.4 units diagonally - so the scene struck the Mite and the
    // simulation the Grunt on the same frame, and the two walks parted. Both now measure through one never-inlined
    // body, ArenaFloor.FloorDistanceSquared, which cannot disagree with itself. This pins that body's answer on the
    // very floats of that frame, so a change of arithmetic (or of inlining) shows up here rather than nine frames into
    // a PlayMode parity run.
    public sealed class TargetingParityTests
    {
        // Scene frame 533 of the Sword kiting parity case: the hero, the Grunt in slot 1 and the Mite in slot 5.
        private const float HeroX = -1.86039162f;
        private const float HeroY = -5.63302755f;
        private const float GruntX = -0.460391641f;
        private const float GruntY = -5.63302755f;
        private const float MiteX = -0.8199889f;
        private const float MiteY = -4.69624472f;

        // The two runtimes do not even agree with each other here: the pure .NET runner puts the Mite a last bit nearer
        // (1.9599998 against 1.9599999), the Editor's Mono lands both on the same float and the earlier slot, the Grunt,
        // wins. That is fine, and it is the whole point - each runtime's scene and simulation now ask one body, so they
        // strike the same enemy whichever way that runtime rounds. What must never come back is one caller seeing the
        // Mite nearer while the other sees a tie.
        [Test]
        public void TheNearTieOfTheKitingFightIsMeasuredByOneBodyForBothCallers()
        {
            float grunt = ArenaFloor.FloorDistanceSquared(GruntX, GruntY, HeroX, HeroY);
            float mite = ArenaFloor.FloorDistanceSquared(MiteX, MiteY, HeroX, HeroY);

            Assert.That(grunt, Is.EqualTo(1.96f).Within(1e-6f), "Both stand 1.4 units from the hero.");
            Assert.That(mite, Is.EqualTo(1.96f).Within(1e-6f));
            Assert.That(mite, Is.LessThanOrEqualTo(grunt), "Nearer or tied, never further, in either runtime.");
            Assert.That(ArenaFloor.FloorDistanceSquared(MiteX, MiteY, HeroX, HeroY), Is.EqualTo(mite),
                "Asked twice, the body answers the same float.");
        }

        // The scene hands Targeting world positions with the depth halved; halving and doubling are exact, so measuring
        // after FloorY gives the simulation's floats bit for bit, not near ones.
        [Test]
        public void MeasuringThroughTheWorldHalvingChangesNoBit()
        {
            float direct = ArenaFloor.FloorDistanceSquared(MiteX, MiteY, HeroX, HeroY);
            float viaWorld = ArenaFloor.FloorDistanceSquared(
                MiteX, ArenaFloor.FloorY(ArenaFloor.WorldY(MiteY)), HeroX, ArenaFloor.FloorY(ArenaFloor.WorldY(HeroY)));
            Assert.That(viaWorld, Is.EqualTo(direct));
        }
    }
}
