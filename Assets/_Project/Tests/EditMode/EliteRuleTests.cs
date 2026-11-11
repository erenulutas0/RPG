using Cryptforge.Combat;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Daggers are the elites' weapon (docs/41, status 2026-11-10 night; EliteRule): every landing of the Daggers on one
    // of the foundry's elites - an elite room's Grunt Captain - deals EliteRule.DaggersShare of what it would, and nothing
    // else moves: not their landings on the Captain's escort or anything else, not another weapon's on the elites, not the
    // Sword's share on the gate boss.
    public sealed class EliteRuleTests
    {
        private static readonly AttackPattern DaggersPattern = new AttackPattern(WeaponBehavior.DirectHit, 0f, 0f, 3, 2f);

        [Test]
        public void TheDaggersDealTheElitesTheirShareAndEveryOtherWeaponItsOwn()
        {
            Assert.That(EliteRule.DaggersId, Is.EqualTo("weapon_daggers"), "The Daggers' WeaponDefinition id.");
            Assert.That(EliteRule.DaggersShare, Is.EqualTo(12f));
            Assert.That(EliteRule.EliteShareOf(EliteRule.DaggersId), Is.EqualTo(EliteRule.DaggersShare));
            foreach (string id in new[] { WardenRule.SwordId, "weapon_staff", string.Empty, null })
                Assert.That(EliteRule.EliteShareOf(id), Is.EqualTo(1f), id ?? "null");
            Assert.That(EliteRule.DamageOn(7f, EliteRule.DaggersShare, true), Is.EqualTo(84f), "The Daggers on an elite.");
            Assert.That(EliteRule.DamageOn(7f, EliteRule.DaggersShare, false), Is.EqualTo(7f), "The Daggers on anything else.");
            Assert.That(EliteRule.DamageOn(24f, EliteRule.EliteShareOf("weapon_staff"), true), Is.EqualTo(24f), "The Staff on an elite.");

            var captain = new HealthState(100f);
            var grunt = new HealthState(100f);
            Assert.That(EliteRule.IsAmong(captain, new IDamageable[] { grunt, captain }), Is.True);
            Assert.That(EliteRule.IsAmong(captain, new IDamageable[] { grunt }), Is.False);
            Assert.That(EliteRule.IsAmong(captain, null), Is.False, "No elites standing.");
            Assert.That(EliteRule.IsAmong(null, new IDamageable[] { null }), Is.False);
        }

        // The same rule through the strike the scene's AttackController and the foundry simulation both fire
        // (WeaponRuntime.TryAttack), with the share the loadout hands each weapon by its id.
        [Test]
        public void ThroughTheStrikeOnlyTheDaggersLandingsOnTheElitesTakeTheShare()
        {
            var loadout = new WeaponLoadout(7);
            WeaponRuntime sword = Held(loadout, WardenRule.SwordId, new AttackPattern(WeaponBehavior.Cleave, 2f, .5f));
            WeaponRuntime staff = Held(loadout, "weapon_staff", new AttackPattern(WeaponBehavior.Area, 3.5f, .5f));
            WeaponRuntime daggers = Held(loadout, EliteRule.DaggersId, DaggersPattern);
            float share = EliteRule.DaggersShare;
            Assert.That(daggers.EliteShare, Is.EqualTo(share));
            Assert.That(sword.EliteShare, Is.EqualTo(1f));
            Assert.That(staff.EliteShare, Is.EqualTo(1f));
            Assert.That(daggers.GateBossShare, Is.EqualTo(1f), "The Daggers deal the gate boss their own.");

            // An elite room's Captain stands with a Mite of its escort; a horde Grunt beside them. Only the Captain is an
            // elite.
            var captain = new HealthState(10000f);
            var mite = new HealthState(10000f);
            var horde = new HealthState(10000f);
            IDamageable[] elites = { captain };
            Assert.That(daggers.TryAttack(captain, null, null, null, null, null, null, elites), Is.True);
            Assert.That(Lost(captain), Is.EqualTo(10f * share).Within(1e-3f), "Their strike on the elite.");
            daggers.Tick(1f);
            Assert.That(daggers.TryAttack(mite, null, null, null, null, null, null, elites), Is.True);
            Assert.That(Lost(mite), Is.EqualTo(10f).Within(1e-3f), "Their strike on its escort, as authored.");
            // The third strike crits for double on the Captain, and splinters onto what stands near.
            daggers.AddModifier(UpgradeStat.CritSplash, new StatModifier(ModifierOperation.Flat, .5f));
            daggers.Tick(1f);
            Assert.That(daggers.TryAttack(captain, null, null, null, new IDamageable[] { captain, mite, horde }, null, null, elites), Is.True);
            Assert.That(Lost(captain), Is.EqualTo(10f * share + 20f * share).Within(1e-3f), "Their crit on the elite.");
            float splinter = CritSplashRule.DamageOf(20f, .5f);
            Assert.That(Lost(mite), Is.EqualTo(10f + splinter).Within(1e-3f), "Their splinter on the escort, as authored.");
            Assert.That(Lost(horde), Is.EqualTo(splinter).Within(1e-3f), "Their splinter on the horde, as authored.");
            // A splinter off the sixth strike's crit on the escort reaches the Captain at the share.
            for (int i = 0; i < 2; i++)
            {
                daggers.Tick(1f);
                Assert.That(daggers.TryAttack(horde, null, null, null, null, null, null, elites), Is.True);
            }
            daggers.Tick(1f);
            Assert.That(daggers.TryAttack(mite, null, null, null, new IDamageable[] { mite, captain }, null, null, elites), Is.True);
            Assert.That(daggers.SplintersFired, Is.EqualTo(2));
            Assert.That(Lost(captain), Is.EqualTo(30f * share + splinter * share).Within(1e-2f), "Their splinter on the elite.");

            // The Staff and the Sword deal the elite their own; the Sword's share on the gate boss stands beside it.
            var other = new HealthState(1000f);
            var otherMite = new HealthState(1000f);
            IDamageable[] more = { other };
            Assert.That(staff.TryAttack(otherMite, null, new IDamageable[] { otherMite, other }, elites: more), Is.True);
            Assert.That(Lost(other), Is.EqualTo(5f).Within(1e-3f), "The Staff's splash on an elite, as authored.");
            var boss = new HealthState(1000f);
            Assert.That(sword.TryAttack(other, null, new IDamageable[] { other, boss }, gateBoss: boss, elites: more), Is.True);
            Assert.That(Lost(other), Is.EqualTo(15f).Within(1e-3f), "The Sword on an elite, as authored.");
            Assert.That(Lost(boss), Is.EqualTo(5f * WardenRule.SwordShare).Within(1e-3f), "Its cleave on the gate boss, half again.");
            daggers.Tick(1f);
            Assert.That(daggers.TryAttack(boss, null, null, null, null, null, boss, more), Is.True);
            Assert.That(Lost(boss), Is.EqualTo(7.5f + 10f).Within(1e-3f), "The Daggers on the gate boss, as authored.");
        }

        // Where no elites stand - the halls, the horde, the hall arena, the Descent - and on a weapon no loadout holds (every
        // enemy's, the hall arena's hero), the Daggers deal what they are authored to.
        [Test]
        public void WithoutElitesOrALoadoutTheDaggersDealTheirOwn()
        {
            WeaponRuntime daggers = Held(new WeaponLoadout(7), EliteRule.DaggersId, DaggersPattern);
            var captain = new HealthState(1000f);
            Assert.That(daggers.TryAttack(captain), Is.True);
            Assert.That(Lost(captain), Is.EqualTo(10f), "No elites handed to the strike.");
            daggers.Tick(1f);
            Assert.That(daggers.TryAttack(captain, null, null, null, null, null, null, new IDamageable[0]), Is.True);
            Assert.That(Lost(captain), Is.EqualTo(20f), "None standing.");

            var unheld = new WeaponRuntime(10f, .5f, 1.8f, 0f, DaggersPattern);
            Assert.That(unheld.EliteShare, Is.EqualTo(1f));
            var elite = new HealthState(1000f);
            Assert.That(unheld.TryAttack(elite, null, null, null, null, null, null, new IDamageable[] { elite }), Is.True);
            Assert.That(Lost(elite), Is.EqualTo(10f), "Daggers no loadout holds.");
            Assert.That(() => unheld.EliteShare = 0f, Throws.InstanceOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => unheld.EliteShare = float.NaN, Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }

        private static WeaponRuntime Held(WeaponLoadout loadout, string id, AttackPattern pattern)
        {
            var runtime = new WeaponRuntime(10f, .8f, 2.1f, 0f, pattern);
            Assert.That(loadout.TryAdd(id, runtime), Is.True, id);
            return runtime;
        }

        private static float Lost(HealthState health) => health.Maximum - health.Current;
    }
}
