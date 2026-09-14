using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // Runs authored floors through the real run, reward, upgrade, forge, choice, scaling, enrage, relic and pack motion code
    // at 60 Hz. It mirrors the scene: a wave enters at the far end of the arena and walks in through PackMotion, the hero
    // strikes the nearest living enemy within reach on the floor, enemies strike once the hero is within theirs, the hero
    // acts before the enemies each frame, and every hit on the hero reaches the relic with its attacker.
    internal static class DescentSimulation
    {
        internal sealed class Enemy
        {
            public string Name;
            public float Health;
            public float Damage;
            public float Interval;
            public float InitialDelay;
            public float EnrageAt;
            public int Gold;
            public int Experience;
            // Floor units per second, and the weapon's reach on the floor.
            public float Speed;
            public float Reach;
        }

        // Rooms hold waves, waves hold the enemies that spawn together; a null room is the forge.
        internal sealed class Floor
        {
            public Enemy[][][] Rooms;
            public float HealthMultiplier = 1f;
            public float DamageMultiplier = 1f;
            public float ModifierDamagePercent;
            public float ModifierGoldPercent;
        }

        internal sealed class HeroWeapon
        {
            public float Damage = 10f;
            public float Interval = 0.8f;
            public float Range = 3f;
            public AttackPattern Pattern;

            public WeaponRuntime CreateRuntime() => new WeaponRuntime(Damage, Interval, Range, 0f, Pattern);
        }

        // Ability_ForgeBurst.asset: the hero's burst, fired by the player; the simulation fires it whenever it is ready and
        // an enemy stands within its radius of the hero.
        internal sealed class HeroAbility
        {
            public float Damage = 20f;
            public float Radius = 2.5f;
            public float Cooldown = 8f;

            public AbilityRuntime CreateRuntime() => new AbilityRuntime(Damage, Radius, Cooldown);
        }

        internal struct Result
        {
            public int ClearedFloors;
            public int AbilityUses;
            public float HeroHealth;
            public float HealthAfterFloorOne;
            public int Gold;
            public int GoldBanked;
            public int UnsecuredAtDeath;
            public int RelicTriggers;
            public int Kills;
            public int UpgradesApplied;
            public float FightSeconds;
            public float FloorOneFightSeconds;
            // Fight time against the enraging bosses, and against packs made only of Cinder Mites.
            public float BossFightSeconds;
            public float MitePackFightSeconds;
            public int FloorOneUpgrades;
            public string DeathRoom;
        }

        // Mirror Data/Enemies, Data/Weapons, Data/Floors and PrototypeEconomy.asset; update together with the assets.
        public const int ExperiencePerLevel = 10;
        public const int ExperienceGrowth = 2;
        public static readonly Enemy Grunt = new Enemy { Name = "Grunt", Health = 50f, Damage = 3.6f, Interval = 1f, Gold = 5, Experience = 10, Speed = 1.6f, Reach = 1.5f };
        public static readonly Enemy Runner = new Enemy { Name = "Runner", Health = 30f, Damage = 1.2f, Interval = 0.4f, Gold = 3, Experience = 10, Speed = 3f, Reach = 1.5f };
        public static readonly Enemy Tank = new Enemy { Name = "Tank", Health = 120f, Damage = 7.2f, Interval = 2.5f, InitialDelay = 1.5f, Gold = 10, Experience = 10, Speed = 0.9f, Reach = 1.7f };
        public static readonly Enemy Captain = new Enemy { Name = "Grunt Captain", Health = 140f, Damage = 5.4f, Interval = 1.2f, InitialDelay = 0.6f, Gold = 20, Experience = 10, Speed = 1.4f, Reach = 1.6f };
        public static readonly Enemy Warden = new Enemy { Name = "Forge Warden", Health = 300f, Damage = 6f, Interval = 1.8f, InitialDelay = 1f, EnrageAt = 0.5f, Gold = 50, Experience = 10, Speed = 1f, Reach = 1.7f };
        // Mites come in numbers, so each one is worth only a little experience and no gold.
        public static readonly Enemy Mite = new Enemy { Name = "Cinder Mite", Health = 15f, Damage = 0.6f, Interval = 1.5f, Gold = 0, Experience = 1, Speed = 2.2f, Reach = 1.5f };

        // Weapon_Sword.asset: 10 damage every 0.8 s, cleaving the nearest enemy within 2 units of the target for 60%.
        public static HeroWeapon Sword() =>
            new HeroWeapon { Damage = 10f, Interval = 0.8f, Range = 2.1f, Pattern = new AttackPattern(WeaponBehavior.Cleave, 2f, 0.6f) };

        // Weapon_Staff.asset: 11 damage every 1.1 s from 2.1 units away, and 75% of it to every enemy within 3.5 units of the target.
        public static HeroWeapon Staff() =>
            new HeroWeapon { Damage = 11f, Interval = 1.1f, Range = 2.4f, Pattern = new AttackPattern(WeaponBehavior.Area, 3.5f, 0.75f) };

        // Weapon_Daggers.asset: 6 damage every 0.5 s at close reach; every third strike crits for double damage.
        public static HeroWeapon Daggers() =>
            new HeroWeapon { Damage = 6f, Interval = 0.5f, Range = 1.8f, Pattern = new AttackPattern(WeaponBehavior.DirectHit, 0f, 0f, 3, 2f) };

        public static readonly Floor EmberHalls = new Floor
        {
            Rooms = new[]
            {
                new[] { new[] { Grunt, Mite }, new[] { Mite, Mite, Mite }, new[] { Grunt, Mite, Mite, Mite } },
                new[] { new[] { Runner, Mite, Mite }, new[] { Mite, Mite, Mite, Mite }, new[] { Tank, Mite, Mite } },
                new[] { new[] { Grunt, Runner, Mite, Mite }, new[] { Tank, Mite, Mite } },
                null,
                new[] { new[] { Captain, Grunt, Mite, Mite } },
                new[] { new[] { Warden } }
            },
            // Packs come from every corner at once, so each enemy hits for a little less than its authored damage.
            DamageMultiplier = 0.92f
        };

        public static readonly Floor QuicksilverVaults = new Floor
        {
            Rooms = new[]
            {
                new[] { new[] { Grunt, Mite, Mite, Mite }, new[] { Runner, Mite, Mite, Mite } },
                new[] { new[] { Tank, Mite, Mite, Mite }, new[] { Mite, Mite, Mite, Mite, Mite } },
                new[] { new[] { Grunt, Grunt, Mite, Mite, Mite } },
                null,
                new[] { new[] { Captain, Grunt, Mite, Mite } },
                new[] { new[] { Warden, Mite, Mite } }
            },
            HealthMultiplier = 1.4f,
            DamageMultiplier = 1.288f,
            ModifierDamagePercent = 0.2f,
            ModifierGoldPercent = 0.5f
        };

        public static UpgradeOption Damage() =>
            new UpgradeOption("upgrade_damage", "Tempered Edge", "+{0:0}% damage per hit", WeaponStat.Damage,
                new StatModifier(ModifierOperation.Percent, 0.5f), 5);

        public static UpgradeOption Speed() =>
            new UpgradeOption("upgrade_attack_speed", "Quickened Grip", "+{0:0}% attack speed", WeaponStat.AttackSpeed,
                new StatModifier(ModifierOperation.Percent, 0.5f), 5);

        public static ForgeOption Mend() => new ForgeOption("forge_mend", "Mend", "Restore {0:0}% health", ForgeEffect.Heal, 0.4f);

        // Data/Relics/Relic_SecondWind.asset and Relic_Counterweight.asset.
        public static RelicOption SecondWind() =>
            new RelicOption("relic_second_wind", "Second Wind", "Once per run, at {1:0}% health or less, restore {0:0}% of your health",
                RelicEffect.SecondWind, 0.25f, 0.25f, 80);

        public static RelicOption Counterweight() =>
            new RelicOption("relic_counterweight", "Counterweight", "When an enemy hits you, strike back for {0:0}% of your weapon damage",
                RelicEffect.CounterStrike, 0.6f, 0f, 150);

        public static ForgeOption Temper() =>
            new ForgeOption("forge_temper", "Temper", "Gain {0:0} extra upgrade choice", ForgeEffect.BonusUpgrade, 1f);

        public static HeroAbility ForgeBurst() => new HeroAbility { Damage = 20f, Radius = 2.5f, Cooldown = 8f };

        // EncounterController's _entryDepth, _formationSpacing and _bodySpacing in Gameplay.unity, in floor units.
        public const float EntryDepth = 5f;
        public const float FormationSpacing = 1f;
        public const float BodySpacing = 0.9f;

        // EncounterController._advanceDelay in Gameplay.unity. The hero's weapon keeps cooling down for this long between a
        // clear and the next wave, so a weapon slower than the delay starts the next wave still cooling down.
        public const float AdvanceDelay = 1f;

        // Always descends. cardSlot is the upgrade card taken every time (clamped when fewer cards remain); floor 2 and
        // later always Mend, floor 1 Mends only when mendOnFloorOne is set.
        public static Result Run(Floor[] floors, int cardSlot, bool mendOnFloorOne, RelicOption relicOption = null,
            HeroWeapon heroWeapon = null, int experiencePerLevel = ExperiencePerLevel, int experienceGrowth = ExperienceGrowth,
            HeroAbility ability = null)
        {
            var run = new RunState(experiencePerLevel, 0.5f, experienceGrowth);
            var rewards = new RewardService(run);
            WeaponRuntime weapon = (heroWeapon ?? Sword()).CreateRuntime();
            AbilityRuntime burst = ability?.CreateRuntime();
            var hero = new HealthState(100f);
            var upgrades = new UpgradeService(run, weapon, new[] { Damage(), Speed() }, 2);
            var forge = new ForgeService(run, hero);
            var choices = new RunChoices(upgrades, forge);
            RelicRuntime relic = relicOption != null ? new RelicRuntime(relicOption) : null;
            var result = new Result();

            for (int f = 0; f < floors.Length; f++)
            {
                bool cleared = RunFloor(floors[f], run, rewards, weapon, burst, hero, forge, choices, relic, cardSlot, f > 0 || mendOnFloorOne, ref result);
                if (f == 0)
                {
                    result.HealthAfterFloorOne = hero.Current;
                    result.FloorOneFightSeconds = result.FightSeconds;
                    result.FloorOneUpgrades = run.UpgradesApplied;
                }
                if (!cleared)
                {
                    result.UnsecuredAtDeath = run.UnsecuredGold;
                    run.End(RunOutcome.Defeat);
                    break;
                }
                result.ClearedFloors++;
                run.SecureGold();
            }

            if (!run.HasEnded)
                run.End(RunOutcome.Victory);
            result.HeroHealth = hero.Current;
            result.Gold = run.Gold;
            result.GoldBanked = run.GoldBanked;
            result.RelicTriggers = relic?.Triggers ?? 0;
            result.UpgradesApplied = run.UpgradesApplied;
            return result;
        }

        private static bool RunFloor(Floor floor, RunState run, RewardService rewards, WeaponRuntime weapon, AbilityRuntime burst,
            HealthState hero, ForgeService forge, RunChoices choices, RelicRuntime relic, int cardSlot, bool useMend, ref Result result)
        {
            const float step = 1f / 60f;
            var waves = new int[floor.Rooms.Length];
            for (int i = 0; i < waves.Length; i++)
                waves[i] = floor.Rooms[i]?.Length ?? 0;
            var progress = new FloorProgress(waves);
            // Waves started on this floor so far; each wave starts one corner further round the arena.
            int waveOrdinal = 0;

            for (FloorStep next = progress.Advance(); next.Kind != FloorStepKind.Cleared; next = progress.Advance())
            {
                if (next.Kind == FloorStepKind.NonCombatRoom)
                {
                    forge.Open(new[] { Mend(), Temper() });
                    choices.TrySelect(choices.Current, useMend ? 0 : 1);
                    ChooseUpgrades(choices, cardSlot);
                    continue;
                }

                Enemy[] pack = floor.Rooms[next.RoomIndex][next.WaveIndex];
                var enemies = new HealthState[pack.Length];
                var enemyWeapons = new WeaponRuntime[pack.Length];
                var enrages = new EnrageRule[pack.Length];
                var rewarded = new bool[pack.Length];
                var motion = new PackMotion(BodySpacing, PackLayout.HalfWidth * FormationSpacing);
                float damageBonus = FloorScaling.DamageBonus(floor.DamageMultiplier, floor.ModifierDamagePercent);
                for (int i = 0; i < pack.Length; i++)
                {
                    enemies[i] = new HealthState(FloorScaling.Health(pack[i].Health, floor.HealthMultiplier, 0f));
                    enemyWeapons[i] = new WeaponRuntime(pack[i].Damage, pack[i].Interval, pack[i].Reach, pack[i].InitialDelay);
                    if (damageBonus != 0f)
                        enemyWeapons[i].AddModifier(WeaponStat.Damage, new StatModifier(ModifierOperation.Percent, damageBonus));
                    enrages[i] = pack[i].EnrageAt > 0f ? new EnrageRule(pack[i].EnrageAt) : null;
                    EntrySides.Formation(waveOrdinal, i, pack.Length, out EntrySide side, out int indexOnSide, out int countOnSide);
                    PackLayout.Offset(indexOnSide, countOnSide, FormationSpacing, out float offsetX, out float offsetY);
                    motion.Add(enemies[i], offsetX, EntryDepth + offsetY, pack[i].Speed, pack[i].Reach, side);
                }
                waveOrdinal++;

                float startSeconds = result.FightSeconds;
                weapon.Tick(AdvanceDelay);
                for (int frame = 0; frame < 200000 && hero.IsAlive && AnyAlive(enemies); frame++)
                {
                    if (frame > 0)
                    {
                        weapon.Tick(step);
                        burst?.Tick(step);
                        for (int i = 0; i < pack.Length; i++)
                            enemyWeapons[i].Tick(step);
                        motion.Step(step);
                        result.FightSeconds += step;
                    }

                    int target = Acquire(motion, enemies, weapon.Range);
                    if (target >= 0 && weapon.IsReady)
                        weapon.TryAttack(enemies[target], null, Nearby(motion, enemies, target, weapon.Pattern.SplashRadius));
                    Resolve(pack, enemies, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, ref result);
                    if (burst != null && burst.IsReady)
                    {
                        IReadOnlyList<IDamageable> inReach = WithinReachOfHero(motion, enemies, burst.Radius);
                        if (inReach.Count > 0)
                        {
                            burst.TryUse(null, inReach);
                            result.AbilityUses++;
                            Resolve(pack, enemies, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, ref result);
                        }
                    }

                    for (int i = 0; i < pack.Length && hero.IsAlive; i++)
                    {
                        if (!enemies[i].IsAlive || !IsHeroInReach(motion, i, enemyWeapons[i].Range))
                            continue;
                        float before = hero.Current;
                        if (enemyWeapons[i].TryAttack(hero, enemies[i]) && relic != null && hero.Current < before)
                        {
                            relic.OnHeroDamaged(hero, enemies[i], weapon.Damage);
                            Resolve(pack, enemies, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, ref result);
                        }
                    }
                }

                float waveSeconds = result.FightSeconds - startSeconds;
                if (Array.Exists(pack, enemy => enemy.EnrageAt > 0f))
                    result.BossFightSeconds += waveSeconds;
                else if (Array.TrueForAll(pack, enemy => enemy.Name == Mite.Name))
                    result.MitePackFightSeconds += waveSeconds;

                if (!hero.IsAlive)
                {
                    result.DeathRoom = $"{next.RoomIndex + 1}.{next.WaveIndex + 1}";
                    return false;
                }
            }

            return true;
        }

        // Enrage checks and kill rewards after any damage to the pack, in slot order like the scene's death callbacks.
        private static void Resolve(Enemy[] pack, HealthState[] enemies, WeaponRuntime[] enemyWeapons, EnrageRule[] enrages, bool[] rewarded,
            Floor floor, RewardService rewards, RunChoices choices, int cardSlot, ref Result result)
        {
            for (int i = 0; i < pack.Length; i++)
            {
                if (enrages[i] != null && enrages[i].Evaluate(enemies[i].Current, enemies[i].Maximum))
                    enemyWeapons[i].AddModifier(WeaponStat.AttackSpeed, new StatModifier(ModifierOperation.Percent, 1f));
                if (enemies[i].IsAlive || rewarded[i])
                    continue;

                rewarded[i] = true;
                result.Kills++;
                rewards.TryAwardKill(enemies[i], pack[i].Experience, FloorScaling.Gold(pack[i].Gold, floor.ModifierGoldPercent));
                ChooseUpgrades(choices, cardSlot);
            }
        }

        // Mirrors Targeting.Acquire for the hero at the floor origin: the nearest living enemy within range, the earlier slot on
        // equal distance. Distances are computed exactly as Targeting computes them.
        internal static int Acquire(PackMotion motion, HealthState[] enemies, float range)
        {
            int nearest = -1;
            float nearestDistanceSquared = range * range;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (!enemies[i].IsAlive)
                    continue;
                float x = motion.XOf(i);
                float y = motion.YOf(i);
                float distanceSquared = x * x + y * y;
                if (nearest < 0 ? distanceSquared <= nearestDistanceSquared : distanceSquared < nearestDistanceSquared)
                {
                    nearest = i;
                    nearestDistanceSquared = distanceSquared;
                }
            }
            return nearest;
        }

        // Mirrors Targeting.CollectNear around the hero: the living enemies within the radius on the floor.
        internal static IReadOnlyList<IDamageable> WithinReachOfHero(PackMotion motion, HealthState[] enemies, float radius)
        {
            var inReach = new List<IDamageable>();
            float radiusSquared = radius * radius;
            for (int i = 0; i < enemies.Length; i++)
            {
                float x = motion.XOf(i);
                float y = motion.YOf(i);
                if (enemies[i].IsAlive && x * x + y * y <= radiusSquared)
                    inReach.Add(enemies[i]);
            }
            return inReach;
        }

        // Mirrors an enemy's Targeting.Acquire, whose only candidate is the hero at the origin.
        private static bool IsHeroInReach(PackMotion motion, int enemy, float reach)
        {
            float x = -motion.XOf(enemy);
            float y = -motion.YOf(enemy);
            return x * x + y * y <= reach * reach;
        }

        // Mirrors Targeting.CollectNear: living enemies within radius of the target on the floor, nearest first.
        internal static IReadOnlyList<IDamageable> Nearby(PackMotion motion, HealthState[] enemies, int target, float radius)
        {
            var nearby = new List<IDamageable>();
            if (radius <= 0f)
                return nearby;

            float radiusSquared = radius * radius;
            var distances = new List<float>();
            for (int i = 0; i < enemies.Length; i++)
            {
                if (i == target || !enemies[i].IsAlive)
                    continue;
                float dx = motion.XOf(i) - motion.XOf(target);
                float dy = motion.YOf(i) - motion.YOf(target);
                float distanceSquared = dx * dx + dy * dy;
                if (distanceSquared > radiusSquared)
                    continue;
                int insertAt = nearby.Count;
                while (insertAt > 0 && distances[insertAt - 1] > distanceSquared)
                    insertAt--;
                nearby.Insert(insertAt, enemies[i]);
                distances.Insert(insertAt, distanceSquared);
            }
            return nearby;
        }

        private static bool AnyAlive(HealthState[] enemies) => FirstAlive(enemies) >= 0;

        private static int FirstAlive(HealthState[] enemies)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].IsAlive)
                    return i;
            }
            return -1;
        }

        private static void ChooseUpgrades(RunChoices choices, int cardSlot)
        {
            while (choices.Current != null && choices.Current.Kind == ChoiceKind.Upgrade)
                choices.TrySelect(choices.Current, Math.Min(cardSlot, choices.Current.Cards.Count - 1));
        }
    }
}
