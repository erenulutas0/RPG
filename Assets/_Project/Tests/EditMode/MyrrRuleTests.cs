using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Rootmother Myrr's rules (docs/64; MyrrRule, RootSnare), without a scene: the snare creeps after the hero and never past
    // it, catches a hero inside it as the roots erupt and is gone after, her feet are inclusive at their reach and inside
    // her hook's, her hook is the Warden's hold unless free, the saplings root square to her line to the hero, and the
    // starting values stand where docs/64 section 3 puts them.
    public sealed class MyrrRuleTests
    {
        private const float Tolerance = 1e-5f;

        [Test]
        public void TheStartingValuesAreDocs64s()
        {
            Assert.That(MyrrRule.Every, Is.EqualTo(WardenRule.SlamEvery), "the Warden's beat");
            Assert.That(MyrrRule.EveryPhaseTwo, Is.EqualTo(WardenRule.SlamEveryEnraged));
            Assert.That(MyrrRule.Telegraph, Is.InRange(.8f, 1.2f), "inside the phone's 0.8-1.2 s");
            Assert.That(MyrrRule.SnareRadius, Is.LessThan(WardenRule.SlamRadius), "smaller than the Warden's circle");
            Assert.That(MyrrRule.SnareRadius, Is.GreaterThan(ThornlingRule.Radius), "larger than the Thornling's ring");
            float walk = DescentSimulation.HeroSpeed * DescentSimulation.FoundryPace;
            Assert.That(MyrrRule.CreepSpeed, Is.LessThan(walk * .5f), "under half the hero's walk");
            Assert.That(MyrrRule.FeetReach, Is.EqualTo(RegentRule.FeetReach), "the Regent's lesson, built in");
            Assert.That(MyrrRule.SnareShare, Is.LessThan(WardenRule.HeroShare));
            // A hero walking straight out from the centre a typical reaction (0.25 s) after the mark leaves the ring
            // before it erupts, with room: its walk less the creep, over the telegraph left.
            float gap = (walk - MyrrRule.CreepSpeed) * (MyrrRule.Telegraph - .25f);
            Assert.That(gap, Is.GreaterThan(MyrrRule.SnareRadius + .3f));
        }

        [Test]
        public void TheSnareCreepsAfterTheHeroAndNeverPastIt()
        {
            var snare = new RootSnare(1.5f, 1.5f);
            Assert.That(snare.IsDown, Is.False);
            snare.Creep(5f, 0f, 1f);
            Assert.That((snare.X, snare.Y), Is.EqualTo((0f, 0f)), "no ring, no creep");
            snare.Down(0f, 0f);
            snare.Creep(3f, 4f, .5f);
            Assert.That(snare.X, Is.EqualTo(.45f).Within(Tolerance), "0.75 units toward (3, 4)");
            Assert.That(snare.Y, Is.EqualTo(.6f).Within(Tolerance));
            Assert.That(snare.Crept, Is.EqualTo(.75f).Within(Tolerance));
            snare.Creep(.5f, .6f, 1f);
            Assert.That((snare.X, snare.Y), Is.EqualTo((.5f, .6f)), "onto the hero, never past");
            snare.Creep(.5f, .6f, 1f);
            Assert.That((snare.X, snare.Y), Is.EqualTo((.5f, .6f)), "a hero that stands is stood on");
            Assert.That(snare.Erupt(.5f + 1.5f, .6f), Is.True, "its edge is inside");
            Assert.That(snare.IsDown, Is.False, "the roots erupt once");
            Assert.That(snare.Erupt(.5f, .6f), Is.False, "no ring, no catch");
            snare.Down(0f, 0f);
            Assert.That(snare.Erupt(1.51f, 0f), Is.False, "beyond its radius");
            snare.Down(0f, 0f);
            snare.Clear();
            Assert.That(snare.Erupt(0f, 0f), Is.False, "withdrawn");
        }

        [Test]
        public void HerFeetAndHerHook()
        {
            Assert.That(MyrrRule.AtHerFeet(1.5f, 0f, 0f, 0f, 1.5f), Is.True, "inclusive at its reach");
            Assert.That(MyrrRule.AtHerFeet(1.51f, 0f, 0f, 0f, 1.5f), Is.False);
            Assert.That(MyrrRule.AtHerFeet(1.2f, .9f, 0f, 0f, 1.5f), Is.True);
            Assert.That(MyrrRule.HoldsHook(true, false), Is.True, "the Warden's hold");
            Assert.That(MyrrRule.HoldsHook(true, true), Is.False, "free at her feet or on a planted hero");
            Assert.That(MyrrRule.HoldsHook(false, false), Is.False);
            Assert.That(MyrrRule.SnareDamage(200f, .1f), Is.EqualTo(20f).Within(Tolerance));
        }

        [Test]
        public void SaplingsRootSquareToHerLineAndGrow()
        {
            // Myrr at (0, 0) facing the hero at (0, 4): her left is -x, her right +x.
            MyrrRule.SaplingSpot(0f, 0f, 0f, 4f, 3f, 1, out float x, out float y);
            Assert.That(x, Is.EqualTo(-3f).Within(Tolerance));
            Assert.That(y, Is.EqualTo(0f).Within(Tolerance));
            MyrrRule.SaplingSpot(0f, 0f, 0f, 4f, 3f, -1, out x, out y);
            Assert.That(x, Is.EqualTo(3f).Within(Tolerance));
            MyrrRule.SaplingSpot(2f, 1f, 2f, 1f, 3f, 1, out x, out y);
            Assert.That(ArenaFloor.FloorDistanceSquared(x, y, 2f, 1f), Is.EqualTo(9f).Within(1e-4f), "on her own spot, still 3 away");
            Assert.That(MyrrRule.Grown(7.99f, 8f), Is.False);
            Assert.That(MyrrRule.Grown(8f, 8f), Is.True);
            Assert.That(MyrrRule.SaplingHealth(5000f, .025f), Is.EqualTo(125f).Within(1e-3f));
            Assert.That(MyrrRule.SaplingHealth(10f, .025f), Is.EqualTo(1f), "never under one");
        }
    }
}
