using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // Runs authored floors through the real run, reward, upgrade, forge, choice, scaling, enrage and relic code at 60 Hz.
    // It mirrors the scene: a wave's pack fights together, the hero strikes the first living enemy in slot order, the
    // hero acts before the enemies each frame, and every hit on the hero reaches the relic with its attacker.
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
            public AttackPattern Pattern;

            public WeaponRuntime CreateRuntime() => new WeaponRuntime(Damage, Interval, 3f, 0f, Pattern);
        }

        internal struct Result
        {
            public int ClearedFloors;
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
        public const int ExperienceGrowth = 1;
        public static readonly Enemy Grunt = new Enemy { Name = "Grunt", Health = 50f, Damage = 6f, Interval = 1f, Gold = 5, Experience = 10 };
        public static readonly Enemy Runner = new Enemy { Name = "Runner", Health = 30f, Damage = 2f, Interval = 0.4f, Gold = 3, Experience = 10 };
        public static readonly Enemy Tank = new Enemy { Name = "Tank", Health = 120f, Damage = 12f, Interval = 2.5f, InitialDelay = 1.5f, Gold = 10, Experience = 10 };
        public static readonly Enemy Captain = new Enemy { Name = "Grunt Captain", Health = 140f, Damage = 9f, Interval = 1.2f, InitialDelay = 0.6f, Gold = 20, Experience = 10 };
        public static readonly Enemy Warden = new Enemy { Name = "Forge Warden", Health = 300f, Damage = 10f, Interval = 1.8f, InitialDelay = 1f, EnrageAt = 0.5f, Gold = 50, Experience = 10 };
        public static readonly Enemy Mite = new Enemy { Name = "Cinder Mite", Health = 15f, Damage = 1f, Interval = 1.5f, Gold = 1, Experience = 3 };

        // Weapon_Sword.asset: 10 damage every 0.8 s, cleaving the nearest enemy within 2 units of the target for 60%.
        public static HeroWeapon Sword() =>
            new HeroWeapon { Damage = 10f, Interval = 0.8f, Pattern = new AttackPattern(WeaponBehavior.Cleave, 2f, 0.6f) };

        // Weapon_Staff.asset: 18 damage every 1.2 s, and 75% to every enemy within 3.5 units of the target.
        public static HeroWeapon Staff() =>
            new HeroWeapon { Damage = 18f, Interval = 1.2f, Pattern = new AttackPattern(WeaponBehavior.Area, 3.5f, 0.75f) };

        // Weapon_Daggers.asset: 6 damage every 0.55 s; every third strike crits for double damage.
        public static HeroWeapon Daggers() =>
            new HeroWeapon { Damage = 6f, Interval = 0.55f, Pattern = new AttackPattern(WeaponBehavior.DirectHit, 0f, 0f, 3, 2f) };

        public static readonly Floor EmberHalls = new Floor
        {
            Rooms = new[]
            {
                new[] { new[] { Grunt }, new[] { Mite, Mite }, new[] { Mite, Mite, Mite } },
                new[] { new[] { Runner, Mite }, new[] { Mite, Mite, Mite }, new[] { Grunt, Mite, Mite } },
                new[] { new[] { Tank }, new[] { Mite, Mite, Mite } },
                null,
                new[] { new[] { Captain, Mite, Mite } },
                new[] { new[] { Warden } }
            }
        };

        public static readonly Floor QuicksilverVaults = new Floor
        {
            Rooms = new[]
            {
                new[] { new[] { Grunt, Mite }, new[] { Mite, Mite, Mite } },
                new[] { new[] { Tank, Mite }, new[] { Runner, Mite, Mite } },
                new[] { new[] { Grunt, Mite, Mite } },
                null,
                new[] { new[] { Captain, Mite } },
                new[] { new[] { Warden } }
            },
            HealthMultiplier = 1.4f,
            DamageMultiplier = 1.25f,
            ModifierDamagePercent = 0.2f,
            ModifierGoldPercent = 0.5f
        };

        public static UpgradeOption Damage() =>
            new UpgradeOption("upgrade_damage", "Tempered Edge", "+{0:0} damage per hit", WeaponStat.Damage,
                new StatModifier(ModifierOperation.Flat, 5f), 5);

        public static UpgradeOption Speed() =>
            new UpgradeOption("upgrade_attack_speed", "Quickened Grip", "+{0:0}% attack speed", WeaponStat.AttackSpeed,
                new StatModifier(ModifierOperation.Percent, 0.5f), 5);

        public static ForgeOption Mend() => new ForgeOption("forge_mend", "Mend", "Restore {0:0}% health", ForgeEffect.Heal, 0.4f);

        public static ForgeOption Temper() =>
            new ForgeOption("forge_temper", "Temper", "Gain {0:0} extra upgrade choice", ForgeEffect.BonusUpgrade, 1f);

        // EncounterController._packSpacing in Gameplay.unity.
        public const float PackSpacing = 1.7f;

        // Always descends. cardSlot is the upgrade card taken every time (clamped when fewer cards remain); floor 2 and
        // later always Mend, floor 1 Mends only when mendOnFloorOne is set.
        public static Result Run(Floor[] floors, int cardSlot, bool mendOnFloorOne, RelicOption relicOption = null,
            HeroWeapon heroWeapon = null, int experiencePerLevel = ExperiencePerLevel, int experienceGrowth = ExperienceGrowth)
        {
            var run = new RunState(experiencePerLevel, 0.5f, experienceGrowth);
            var rewards = new RewardService(run);
            WeaponRuntime weapon = (heroWeapon ?? Sword()).CreateRuntime();
            var hero = new HealthState(100f);
            var upgrades = new UpgradeService(run, weapon, new[] { Damage(), Speed() }, 2);
            var forge = new ForgeService(run, hero);
            var choices = new RunChoices(upgrades, forge);
            RelicRuntime relic = relicOption != null ? new RelicRuntime(relicOption) : null;
            var result = new Result();

            for (int f = 0; f < floors.Length; f++)
            {
                bool cleared = RunFloor(floors[f], run, rewards, weapon, hero, forge, choices, relic, cardSlot, f > 0 || mendOnFloorOne, ref result);
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

        private static bool RunFloor(Floor floor, RunState run, RewardService rewards, WeaponRuntime weapon, HealthState hero,
            ForgeService forge, RunChoices choices, RelicRuntime relic, int cardSlot, bool useMend, ref Result result)
        {
            const float step = 1f / 60f;
            var waves = new int[floor.Rooms.Length];
            for (int i = 0; i < waves.Length; i++)
                waves[i] = floor.Rooms[i]?.Length ?? 0;
            var progress = new FloorProgress(waves);

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
                float damageBonus = FloorScaling.DamageBonus(floor.DamageMultiplier, floor.ModifierDamagePercent);
                for (int i = 0; i < pack.Length; i++)
                {
                    enemies[i] = new HealthState(FloorScaling.Health(pack[i].Health, floor.HealthMultiplier, 0f));
                    enemyWeapons[i] = new WeaponRuntime(pack[i].Damage, pack[i].Interval, 3f, pack[i].InitialDelay);
                    if (damageBonus != 0f)
                        enemyWeapons[i].AddModifier(WeaponStat.Damage, new StatModifier(ModifierOperation.Percent, damageBonus));
                    enrages[i] = pack[i].EnrageAt > 0f ? new EnrageRule(pack[i].EnrageAt) : null;
                }

                float startSeconds = result.FightSeconds;
                weapon.Tick(10f);
                for (int frame = 0; frame < 200000 && hero.IsAlive && AnyAlive(enemies); frame++)
                {
                    if (frame > 0)
                    {
                        weapon.Tick(step);
                        for (int i = 0; i < pack.Length; i++)
                            enemyWeapons[i].Tick(step);
                        result.FightSeconds += step;
                    }

                    int target = FirstAlive(enemies);
                    if (weapon.IsReady)
                        weapon.TryAttack(enemies[target], null, Nearby(enemies, target, weapon.Pattern.SplashRadius));
                    Resolve(pack, enemies, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, ref result);

                    for (int i = 0; i < pack.Length && hero.IsAlive; i++)
                    {
                        if (!enemies[i].IsAlive)
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
                else if (Array.TrueForAll(pack, enemy => enemy == Mite))
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

        // Mirrors Targeting.CollectNear over PackLayout slots: living enemies within radius of the target, nearest first.
        internal static IReadOnlyList<IDamageable> Nearby(HealthState[] enemies, int target, float radius)
        {
            var nearby = new List<IDamageable>();
            if (radius <= 0f)
                return nearby;

            float origin = PackLayout.OffsetX(target, enemies.Length, PackSpacing);
            var distances = new List<float>();
            for (int i = 0; i < enemies.Length; i++)
            {
                if (i == target || !enemies[i].IsAlive)
                    continue;
                float distance = Math.Abs(PackLayout.OffsetX(i, enemies.Length, PackSpacing) - origin);
                if (distance > radius)
                    continue;
                int insertAt = nearby.Count;
                while (insertAt > 0 && distances[insertAt - 1] > distance)
                    insertAt--;
                nearby.Insert(insertAt, enemies[i]);
                distances.Insert(insertAt, distance);
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
