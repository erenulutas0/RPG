using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // Runs authored floors through the real run, reward, upgrade, forge, choice, scaling, enrage, relic, hero motion and
    // pack motion code at 60 Hz. It mirrors the scene: a wave enters round the hero and walks in through PackMotion, the
    // hero strikes the nearest living enemy within reach on the floor, enemies strike once the hero is within theirs, the
    // hero acts before the enemies each frame, and every hit on the hero reaches the relic with its attacker.
    //
    // The hero walks too, when a run is given an IHeroRoute: HeroMovementInput (execution order -60) steers it from the
    // steer it holds, then EncounterController (-50) steps the pack toward where it now stands, and the attacks follow. So
    // the route is asked at the end of a frame and its answer is walked at the start of the next one, the same one-frame
    // input lag the scene's PlayMode driver has when it calls HeroMovementInput.Hold after a frame's Update calls. Without
    // a route the hero stands at the floor origin, where it has always stood: subtracting 0f is exact, so every distance,
    // every placement and every balance number is bit for bit the one the stationary baseline was measured with.
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
            public int Level;
            // What the movement report reads: strikes the hero took and the health they cost it.
            public int HeroHitsTaken;
            public float HeroDamageTaken;
            // Frames the hero stood outside the rim margin it keeps. HeroMotion never lets it, so this stays 0.
            public int FramesOffPlatform;
            // Hero strikes landed on a frame the hero also walked.
            public int StrikesWhileMoving;
            // Seconds from a wave's spawn to the first strike its enemies landed on the hero, smallest over the run's waves;
            // float.MaxValue when no enemy ever struck. A spawn out of reach leaves it above 0.
            public float EarliestStrikeAfterSpawn;
            // How close two living enemies of a wave came, and how close a living enemy came to the hero, over every frame
            // from a spawn on. Kept squared while the run measures them; float.MaxValue until the first measurement.
            public float ClosestEnemyGapSquared;
            public float ClosestHeroDistanceSquared;
            // Where the hero stood on the floor when the run ended, so a scene driver can prove it walked the same walk
            // and not merely reached the same result. (0, 0) for a run with no route, which never moves the hero.
            public float HeroFloorX;
            public float HeroFloorY;

            public float ClosestEnemyGap => (float)Math.Sqrt(ClosestEnemyGapSquared);
            public float ClosestHeroDistance => (float)Math.Sqrt(ClosestHeroDistanceSquared);
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

        // The density proof's floors: one combat room holding one wave of ten, with Ember Halls' multipliers, so the densest
        // pack the formations hold can be fought by a standing hero and a walking one under the same rules. Ten Cinder Mites
        // swarm at 2.2 floor units a second; the trailing wave puts a Grunt in slots 0 and 1, which enter from two corners
        // at 1.6 and fall behind the mites, so the hero is chased by a fast ring and a slow one at once.
        public static readonly Floor DensityProofMites = new Floor
        {
            Rooms = new[] { new[] { new[] { Mite, Mite, Mite, Mite, Mite, Mite, Mite, Mite, Mite, Mite } } },
            DamageMultiplier = 0.92f
        };

        public static readonly Floor DensityProofTrailing = new Floor
        {
            Rooms = new[] { new[] { new[] { Grunt, Grunt, Mite, Mite, Mite, Mite, Mite, Mite, Mite, Mite } } },
            DamageMultiplier = 0.92f
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
        // HeroMovementInput._speed in Gameplay.unity, in floor units per second at full steer.
        public const float HeroSpeed = 2.5f;
        // ArenaView's corners in Gameplay.unity: the platform the packs enter on, stop on, and the hero walks on.
        public static readonly ArenaGeometry Platform = new ArenaGeometry(-9f, 9f, 9f);

        // EncounterController._advanceDelay in Gameplay.unity. The hero's weapon keeps cooling down for this long between a
        // clear and the next wave, so a weapon slower than the delay starts the next wave still cooling down.
        public const float AdvanceDelay = 1f;

        // Always descends. cardSlot is the upgrade card taken every time (clamped when fewer cards remain); floor 2 and
        // later always Mend, floor 1 Mends only when mendOnFloorOne is set. A null route leaves the hero at the floor
        // origin for the whole run, which is the balance baseline; heroSpeed is the walking speed a route steers.
        public static Result Run(Floor[] floors, int cardSlot, bool mendOnFloorOne, RelicOption relicOption = null,
            HeroWeapon heroWeapon = null, int experiencePerLevel = ExperiencePerLevel, int experienceGrowth = ExperienceGrowth,
            HeroAbility ability = null, IHeroRoute route = null, float heroSpeed = HeroSpeed)
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
            var result = new Result
            {
                EarliestStrikeAfterSpawn = float.MaxValue,
                ClosestEnemyGapSquared = float.MaxValue,
                ClosestHeroDistanceSquared = float.MaxValue
            };
            // One hero, placed where every run starts and kept across waves, rooms and floors, as in the scene.
            var heroMotion = new HeroMotion(heroSpeed);
            heroMotion.Place(0f, 0f, Platform);
            RouteView view = route != null ? new RouteView() : null;

            for (int f = 0; f < floors.Length; f++)
            {
                bool cleared = RunFloor(floors[f], run, rewards, weapon, burst, hero, forge, choices, relic, cardSlot, f > 0 || mendOnFloorOne, heroMotion, route, view, ref result);
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
            result.Level = run.Level;
            result.Gold = run.Gold;
            result.GoldBanked = run.GoldBanked;
            result.RelicTriggers = relic?.Triggers ?? 0;
            result.UpgradesApplied = run.UpgradesApplied;
            result.HeroFloorX = heroMotion.X;
            result.HeroFloorY = heroMotion.Y;
            return result;
        }

        private static bool RunFloor(Floor floor, RunState run, RewardService rewards, WeaponRuntime weapon, AbilityRuntime burst,
            HealthState hero, ForgeService forge, RunChoices choices, RelicRuntime relic, int cardSlot, bool useMend,
            HeroMotion heroMotion, IHeroRoute route, RouteView view, ref Result result)
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
                // The pack forms up round wherever the hero stands now, keeping a body's spacing between its spots.
                float spawnX = heroMotion.X;
                float spawnY = heroMotion.Y;
                var motion = new PackMotion(BodySpacing, PackLayout.HalfWidth * FormationSpacing, Platform, spawnX, spawnY);
                var placements = new EntryPlacement[pack.Length];
                EntrySides.Place(waveOrdinal, pack.Length, spawnX, spawnY, Platform, EntryDepth, FormationSpacing, BodySpacing, placements);
                float damageBonus = FloorScaling.DamageBonus(floor.DamageMultiplier, floor.ModifierDamagePercent);
                for (int i = 0; i < pack.Length; i++)
                {
                    enemies[i] = new HealthState(FloorScaling.Health(pack[i].Health, floor.HealthMultiplier, 0f));
                    enemyWeapons[i] = new WeaponRuntime(pack[i].Damage, pack[i].Interval, pack[i].Reach, pack[i].InitialDelay);
                    if (damageBonus != 0f)
                        enemyWeapons[i].AddModifier(WeaponStat.Damage, new StatModifier(ModifierOperation.Percent, damageBonus));
                    enrages[i] = pack[i].EnrageAt > 0f ? new EnrageRule(pack[i].EnrageAt) : null;
                    motion.Add(enemies[i], placements[i], pack[i].Speed, pack[i].Reach);
                }
                waveOrdinal++;

                float startSeconds = result.FightSeconds;
                // The steer the hero holds into the next frame, at rest on the frame a wave spawns, and when this wave first
                // landed a hit on the hero. No frame passes during the advance delay or an instant choice, so the hero
                // stands still through both, as it does in the scene while a choice panel is open.
                float steerX = 0f;
                float steerY = 0f;
                float firstStrike = -1f;
                weapon.Tick(AdvanceDelay);
                for (int frame = 0; frame < 200000 && hero.IsAlive && AnyAlive(enemies); frame++)
                {
                    bool moved = false;
                    if (frame > 0)
                    {
                        weapon.Tick(step);
                        burst?.Tick(step);
                        for (int i = 0; i < pack.Length; i++)
                            enemyWeapons[i].Tick(step);
                        // The scene's order: the hero walks from the steer it holds, then the pack steps toward where it is.
                        moved = heroMotion.Move(steerX, steerY, step, Platform);
                        motion.Step(step, heroMotion.X, heroMotion.Y);
                        result.FightSeconds += step;
                    }

                    int target = Acquire(motion, enemies, weapon.Range, heroMotion.X, heroMotion.Y);
                    if (target >= 0 && weapon.IsReady &&
                        weapon.TryAttack(enemies[target], null, Nearby(motion, enemies, target, weapon.Pattern.SplashRadius)) && moved)
                        result.StrikesWhileMoving++;
                    Resolve(pack, enemies, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, ref result);
                    if (burst != null && burst.IsReady)
                    {
                        IReadOnlyList<IDamageable> inReach = WithinReachOfHero(motion, enemies, burst.Radius, heroMotion.X, heroMotion.Y);
                        if (inReach.Count > 0)
                        {
                            burst.TryUse(null, inReach);
                            result.AbilityUses++;
                            Resolve(pack, enemies, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, ref result);
                        }
                    }

                    for (int i = 0; i < pack.Length && hero.IsAlive; i++)
                    {
                        if (!enemies[i].IsAlive || !IsHeroInReach(motion, i, enemyWeapons[i].Range, heroMotion.X, heroMotion.Y))
                            continue;
                        float before = hero.Current;
                        if (!enemyWeapons[i].TryAttack(hero, enemies[i]))
                            continue;

                        result.HeroHitsTaken++;
                        result.HeroDamageTaken += before - hero.Current;
                        if (firstStrike < 0f)
                            firstStrike = result.FightSeconds - startSeconds;
                        if (relic != null && hero.Current < before)
                        {
                            relic.OnHeroDamaged(hero, enemies[i], weapon.Damage);
                            Resolve(pack, enemies, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, ref result);
                        }
                    }

                    Observe(motion, enemies, heroMotion, ref result);
                    if (route != null)
                        Ask(route, view, motion, enemies, enemyWeapons, heroMotion, weapon.Range, result.FightSeconds, out steerX, out steerY);
                }

                if (firstStrike >= 0f && firstStrike < result.EarliestStrikeAfterSpawn)
                    result.EarliestStrikeAfterSpawn = firstStrike;
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

        // Mirrors Targeting.Acquire for a hero at the floor origin, where it has always stood.
        internal static int Acquire(PackMotion motion, HealthState[] enemies, float range) => Acquire(motion, enemies, range, 0f, 0f);

        // Mirrors Targeting.Acquire for the hero: the nearest living enemy within range, the earlier slot on equal distance.
        // Distances are computed exactly as Targeting computes them, candidate minus origin, so the scene and the simulation
        // get the same floats; with the hero at the origin subtracting 0f leaves every value as it was.
        internal static int Acquire(PackMotion motion, HealthState[] enemies, float range, float heroX, float heroY)
        {
            int nearest = -1;
            float nearestDistanceSquared = range * range;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (!enemies[i].IsAlive)
                    continue;
                float x = motion.XOf(i) - heroX;
                float y = motion.YOf(i) - heroY;
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
        internal static IReadOnlyList<IDamageable> WithinReachOfHero(PackMotion motion, HealthState[] enemies, float radius,
            float heroX, float heroY)
        {
            var inReach = new List<IDamageable>();
            float radiusSquared = radius * radius;
            for (int i = 0; i < enemies.Length; i++)
            {
                float x = motion.XOf(i) - heroX;
                float y = motion.YOf(i) - heroY;
                if (enemies[i].IsAlive && x * x + y * y <= radiusSquared)
                    inReach.Add(enemies[i]);
            }
            return inReach;
        }

        // Mirrors an enemy's Targeting.Acquire, whose only candidate is the hero: the hero's position less the enemy's, the
        // way the scene subtracts it.
        private static bool IsHeroInReach(PackMotion motion, int enemy, float reach, float heroX, float heroY)
        {
            float x = heroX - motion.XOf(enemy);
            float y = heroY - motion.YOf(enemy);
            return x * x + y * y <= reach * reach;
        }

        // Watches a wave the way the movement report reads it: how close two living enemies came, how close one came to the
        // hero, and whether the hero ever stood outside the rim margin HeroMotion bounds it by.
        private static void Observe(PackMotion motion, HealthState[] enemies, HeroMotion heroMotion, ref Result result)
        {
            if (!Platform.IsOnPlatform(heroMotion.X, heroMotion.Y, HeroMotion.EdgeMargin))
                result.FramesOffPlatform++;

            for (int i = 0; i < enemies.Length; i++)
            {
                if (!enemies[i].IsAlive)
                    continue;
                float dx = motion.XOf(i) - heroMotion.X;
                float dy = motion.YOf(i) - heroMotion.Y;
                float distanceSquared = dx * dx + dy * dy;
                if (distanceSquared < result.ClosestHeroDistanceSquared)
                    result.ClosestHeroDistanceSquared = distanceSquared;

                for (int other = 0; other < i; other++)
                {
                    if (!enemies[other].IsAlive)
                        continue;
                    float apartX = motion.XOf(i) - motion.XOf(other);
                    float apartY = motion.YOf(i) - motion.YOf(other);
                    float apartSquared = apartX * apartX + apartY * apartY;
                    if (apartSquared < result.ClosestEnemyGapSquared)
                        result.ClosestEnemyGapSquared = apartSquared;
                }
            }
        }

        // Fills the route's view at the end of a frame and takes the steer the hero holds through the next one: the scene's
        // one-frame input lag, where the PlayMode driver calls HeroMovementInput.Hold after the frame's Update calls.
        private static void Ask(IHeroRoute route, RouteView view, PackMotion motion, HealthState[] enemies,
            WeaponRuntime[] enemyWeapons, HeroMotion heroMotion, float heroReach, float seconds, out float steerX, out float steerY)
        {
            view.HeroX = heroMotion.X;
            view.HeroY = heroMotion.Y;
            view.HeroReach = heroReach;
            view.Platform = Platform;
            view.Count = enemies.Length;
            view.Seconds = seconds;
            for (int i = 0; i < enemies.Length; i++)
            {
                view.EnemyX[i] = motion.XOf(i);
                view.EnemyY[i] = motion.YOf(i);
                view.EnemyReach[i] = enemyWeapons[i].Range;
                view.Alive[i] = enemies[i].IsAlive;
            }
            route.Steer(view, out steerX, out steerY);
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
