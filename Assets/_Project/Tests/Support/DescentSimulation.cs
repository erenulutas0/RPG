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
            // One flag per room, or null for a floor with no swarm in it: a swarm room's waves are a ladder that
            // repeats its last entry and hardens wave by wave, and it never clears itself (RoomKind.Swarm, SwarmRule).
            public bool[] Swarm;

            public bool IsSwarm(int roomIndex) => Swarm != null && roomIndex < Swarm.Length && Swarm[roomIndex];
        }

        internal sealed class HeroWeapon
        {
            // The WeaponDefinition's id, which is what a loadout tells two weapons apart by; a hand-made weapon that
            // names none is held under UnnamedWeaponId.
            public string Id;
            public string DisplayName;
            // The weapon's own table, as its WeaponDefinition's _levelUps hold it, what it evolves into and the tome it
            // evolves with; empty and null for a hand-made weapon. A run in category mode draws a carried weapon's
            // upgrades, and once unlocked its evolution, from these.
            public UpgradeOption[] Lines;
            public UpgradeOption Evolution;
            public string EvolvesWithTome;
            public float Damage = 10f;
            public float Interval = 0.8f;
            public float Range = 3f;
            public AttackPattern Pattern;
            // A splinter the weapon starts the run with, as a share of a critical strike, for the same reason as below.
            public float CritSplashShare;
            // A rolled crit chance the weapon starts with, which a card grants in the game: the measurement of a
            // crit-gated effect has to be able to ask what the Sword would do having bought one.
            public float CritChance;
            // A pulse the weapon starts the run with, as a share of its damage. No weapon is authored with one - the
            // Forge Pulse card grants it - but a measurement that wants to know what a pulse is worth cannot wait for a
            // run to be offered the card, and a run long enough to be offered it is a different run. 0 is every weapon
            // the game ships.
            public float PulseShare;
            // The category-mode base (WeaponDefinition._categoryDamage/_categoryInterval); 0 keeps Damage and Interval.
            public float CategoryDamage;
            public float CategoryInterval;

            public WeaponRuntime CreateRuntime() => Create(Damage, Interval);

            public WeaponRuntime CreateCategoryRuntime() =>
                Create(CategoryDamage > 0f ? CategoryDamage : Damage, CategoryInterval > 0f ? CategoryInterval : Interval);

            private WeaponRuntime Create(float damage, float interval)
            {
                var runtime = new WeaponRuntime(damage, interval, Range, 0f, Pattern);
                if (PulseShare > 0f)
                    runtime.AddModifier(UpgradeStat.PulseDamage, new StatModifier(ModifierOperation.Flat, PulseShare));
                if (CritSplashShare > 0f)
                    runtime.AddModifier(UpgradeStat.CritSplash, new StatModifier(ModifierOperation.Flat, CritSplashShare));
                if (CritChance > 0f)
                    runtime.AddModifier(UpgradeStat.CritChance, new StatModifier(ModifierOperation.Flat, CritChance));
                return runtime;
            }

            // The weapon as category-mode offers see it, built the way WeaponDefinition.CreateCard builds the scene's.
            public WeaponCard CreateCard() =>
                new WeaponCard(Id ?? UnnamedWeaponId, DisplayName, string.Empty, CreateCategoryRuntime,
                    Lines ?? new UpgradeOption[0], Evolution, EvolvesWithTome);
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
            // Seconds the hero spent walking on an empty movement budget, at HeroStamina.EmptySpeed of its speed.
            public float StaminaEmptySeconds;
            // Seconds from a wave's spawn to the first strike its enemies landed on the hero, smallest over the run's waves;
            // float.MaxValue when no enemy ever struck. A spawn out of reach leaves it above 0.
            public float EarliestStrikeAfterSpawn;
            // How close two living enemies of a wave came, and how close a living enemy came to the hero, over every frame
            // from a spawn on. Kept squared while the run measures them; float.MaxValue until the first measurement.
            public float ClosestEnemyGapSquared;
            public float ClosestHeroDistanceSquared;
            // Seconds a living enemy that had not reached the hero spent held back by the pack with no step to take, summed
            // over every enemy of the run, and the longest any one enemy was held back without a break.
            public float EnemyStallSeconds;
            public float LongestEnemyStallSeconds;
            // Frames on which a living enemy stepped back against the step it took before, over every enemy of the run: how
            // often the pack turns round on the spot. Only steps of at least VisibleStep count, so a correction too small to
            // see is not a turn, and the first step after standing still for FlickerSeconds or more starts afresh, since
            // setting off is not turning back. A pack following a hero that turns round turns with it.
            public int EnemyStepReversals;
            // Of those, the ones that came within FlickerSeconds of the same enemy's previous one: an enemy stepping back and
            // forth on the spot rather than turning to follow the hero.
            public int EnemyStepFlickers;
            // Where the hero stood on the floor when the run ended, so a scene driver can prove it walked the same walk
            // and not merely reached the same result. (0, 0) for a run with no route, which never moves the hero.
            public float HeroFloorX;
            public float HeroFloorY;
            // Chests the hero walked onto, and how many of those held a book. A run with no route never leaves the
            // middle and so never opens one, which is why every balance number measured before chests existed is
            // unchanged.
            public int ChestsOpened;
            public int BooksTaken;
            // The swarm ramp's step the run reached, one-based - what a wave number used to mean, one step per
            // SwarmRule.StepSeconds - and the seconds it stood in the room. Both 0 on a floor with no swarm room.
            public int SwarmWave;
            public float SwarmSeconds;
            // When the swarm first put an enemy of its ladder's last wave on the floor, on the room's clock; 0 if it never did.
            public float LadderTopSeconds;
            // Level-up sittings the swarm opened, whether or not anything was waiting.
            public int Sittings;
            // The Descent's drops (docs/27, S6d): picked up by walking over them, and swept to the hero at a floor's end.
            public int DropsCollected;
            public int DropsSwept;
            // The run's level as floor one's last room - its boss - begins; -1 if the run never got there.
            public int FloorOneBossLevel;
            // Pulses the hero's weapons fired, and splinters they threw, summed over the loadout; 0 until a card grants
            // each.
            public int PulsesFired;
            public int SplintersFired;
            // Evolution stacks taken, and what the world ended up multiplying its enemies by for them.
            public int EvolutionsTaken;
            public float Difficulty;
            // Recorded picks a replay could not make, because the card it named was not offered.
            public int ReplayMisses;
            // What a category-mode run ended up carrying: weapons and distinct tomes held, and the highest level any one
            // of each reached. One weapon, no tomes and level 1 for every legacy run.
            public int WeaponsHeld;
            public int TomesHeld;
            public int HighestWeaponLevel;
            public int HighestTomeLevel;
            // The cards the run took, in order: what a replay of this run would hand back.
            public string[] Picks;

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
            new HeroWeapon { Id = "weapon_sword", DisplayName = "Sword", Lines = SwordLines(), Evolution = SwordEvolution(), EvolvesWithTome = "tome_force", Damage = 10f, Interval = 0.8f, CategoryDamage = 18f, CategoryInterval = 0.8f, Range = 2.1f, Pattern = new AttackPattern(WeaponBehavior.Cleave, 2f, 0.6f) };

        // Weapon_Staff.asset: 11 damage every 1.1 s from 2.1 units away, and 75% of it to every enemy within 3.5 units of the target.
        public static HeroWeapon Staff() =>
            new HeroWeapon { Id = "weapon_staff", DisplayName = "Staff", Lines = StaffLines(), Damage = 11f, Interval = 1.1f, CategoryDamage = 24f, CategoryInterval = 1.1f, Range = 2.4f, Pattern = new AttackPattern(WeaponBehavior.Area, 3.5f, 0.75f) };

        // Weapon_Daggers.asset: 6 damage every 0.5 s at close reach; every third strike crits for double damage.
        public static HeroWeapon Daggers() =>
            new HeroWeapon { Id = "weapon_daggers", DisplayName = "Daggers", Lines = DaggersLines(), Evolution = DaggersEvolution(), EvolvesWithTome = "tome_precision", Damage = 6f, Interval = 0.5f, CategoryDamage = 7f, CategoryInterval = 0.3f, Range = 1.8f, Pattern = new AttackPattern(WeaponBehavior.DirectHit, 0f, 0f, 3, 2f) };

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

        // The development swarm floor, mirroring Resources/Development/Floor_SwarmProof: one room whose waves do not run
        // out. Its twelve authored waves are a ladder - three enemies growing to the ten the formations hold, grunts
        // from the third, a runner from the sixth, the heavies from the tenth - and once it is spent the last wave
        // repeats while SwarmRule hardens every enemy in it, wave by wave. The shape comes from docs/26 Part 6, which
        // measured it before any of this was built; this authors it rather than generating it, so the asset and this
        // hold the same twelve waves.
        public static readonly Floor SwarmProof = new Floor
        {
            Rooms = new[]
            {
                new[]
                {
                    new[] { Mite, Mite, Mite },
                    new[] { Mite, Mite, Mite },
                    new[] { Grunt, Mite, Mite, Mite },
                    new[] { Grunt, Mite, Mite, Mite, Mite },
                    new[] { Grunt, Mite, Mite, Mite, Mite },
                    new[] { Runner, Grunt, Mite, Mite, Mite, Mite },
                    new[] { Runner, Grunt, Grunt, Mite, Mite, Mite },
                    new[] { Runner, Grunt, Grunt, Mite, Mite, Mite, Mite },
                    new[] { Runner, Grunt, Grunt, Grunt, Mite, Mite, Mite, Mite },
                    new[] { Tank, Runner, Grunt, Grunt, Grunt, Mite, Mite, Mite },
                    new[] { Tank, Runner, Grunt, Grunt, Grunt, Mite, Mite, Mite, Mite },
                    new[] { Tank, Warden, Runner, Grunt, Grunt, Grunt, Mite, Mite, Mite, Mite }
                }
            },
            Swarm = new[] { true },
            // Ember Halls' own rate, so a swarm opens at the difficulty the game already fights at.
            DamageMultiplier = 0.92f
        };

        // Data/Floors/Floor_FinalSwarm, the finale behind the last boss (docs/27 decision 5): the proof floor's ladder and
        // multipliers, unchanged, because that is the room docs/35 and docs/36 measured at the end of the Descent. One
        // definition here, so the finale and the proof floor cannot drift apart in the simulation; the scene test that
        // descends into the finale holds the asset to it.
        public static Floor FinalSwarm => SwarmProof;

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
            new UpgradeOption("upgrade_damage", "Tempered Edge", "+{0:0}% damage per hit", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Percent, 0.5f),
                new StatModifier(ModifierOperation.Percent, 0.75f),
                new StatModifier(ModifierOperation.Percent, 1.25f),
                new StatModifier(ModifierOperation.Percent, 1.67f), 5);

        public static UpgradeOption Speed() =>
            new UpgradeOption("upgrade_attack_speed", "Quickened Grip", "+{0:0}% attack speed", UpgradeStat.AttackSpeed,
                new StatModifier(ModifierOperation.Percent, 0.5f),
                new StatModifier(ModifierOperation.Percent, 0.75f),
                new StatModifier(ModifierOperation.Percent, 1.25f),
                new StatModifier(ModifierOperation.Percent, 1.67f), 5);

        public static UpgradeOption Range() =>
            new UpgradeOption("upgrade_range", "Long Reach", "+{0:0.0#} reach", UpgradeStat.Range,
                new StatModifier(ModifierOperation.Flat, 0.3f),
                new StatModifier(ModifierOperation.Flat, 0.6f),
                new StatModifier(ModifierOperation.Flat, 1f),
                new StatModifier(ModifierOperation.Flat, 1.33f), 3);

        public static UpgradeOption Crit() =>
            new UpgradeOption("upgrade_crit", "Keen Edge", "+{0:0%} chance to crit", UpgradeStat.CritChance,
                new StatModifier(ModifierOperation.Flat, 0.08f),
                new StatModifier(ModifierOperation.Flat, 0.15f),
                new StatModifier(ModifierOperation.Flat, 0.25f),
                new StatModifier(ModifierOperation.Flat, 0.33f), 4);

        public static UpgradeOption Health() =>
            new UpgradeOption("upgrade_health", "Vital Surge", "+{0:0} maximum health", UpgradeStat.MaxHealth,
                new StatModifier(ModifierOperation.Flat, 15f),
                new StatModifier(ModifierOperation.Flat, 30f),
                new StatModifier(ModifierOperation.Flat, 60f),
                new StatModifier(ModifierOperation.Flat, 80f), 5);

        public static UpgradeOption Armor() =>
            new UpgradeOption("upgrade_armor", "Iron Skin", "+{0:0} armor", UpgradeStat.Armor,
                new StatModifier(ModifierOperation.Flat, 20f),
                new StatModifier(ModifierOperation.Flat, 40f),
                new StatModifier(ModifierOperation.Flat, 80f),
                new StatModifier(ModifierOperation.Flat, 107f), 5);

        public static UpgradeOption MoveSpeed() =>
            new UpgradeOption("upgrade_move_speed", "Swift Boots", "+{0:0}% movement speed", UpgradeStat.MoveSpeed,
                new StatModifier(ModifierOperation.Percent, 0.05f),
                new StatModifier(ModifierOperation.Percent, 0.1f),
                new StatModifier(ModifierOperation.Percent, 0.15f),
                new StatModifier(ModifierOperation.Percent, 0.2f), 3);

        public static UpgradeOption Stamina() =>
            new UpgradeOption("upgrade_stamina", "Deep Lungs", "+{0:0.0#}s of running", UpgradeStat.StaminaBar,
                new StatModifier(ModifierOperation.Flat, 0.4f),
                new StatModifier(ModifierOperation.Flat, 0.8f),
                new StatModifier(ModifierOperation.Flat, 1.5f),
                new StatModifier(ModifierOperation.Flat, 2f), 3);

        public static UpgradeOption Luck() =>
            new UpgradeOption("upgrade_luck", "Fortune", "+{0:0} luck", UpgradeStat.Luck,
                new StatModifier(ModifierOperation.Flat, 6f),
                new StatModifier(ModifierOperation.Flat, 7.5f),
                new StatModifier(ModifierOperation.Flat, 9f),
                new StatModifier(ModifierOperation.Flat, 12f), 3);

        // Upgrade_Pulse.asset: every fourth strike bursts round the hero for a share of its damage (PulseRule). The first
        // card that changes what an attack does rather than a number it carries, and the last in the authored order, so
        // only a run that has levelled well past a Descent is ever offered it.
        // One stack, not three: measured over sixteen seeds and three weapons (docs/30), a share of one already doubles
        // or trebles how deep a run reaches, so a card that stacked to four would not be a card.
        public static UpgradeOption Pulse() =>
            new UpgradeOption("upgrade_pulse", "Forge Pulse", "Every 4th strike bursts for {0:0%} damage nearby",
                UpgradeStat.PulseDamage,
                new StatModifier(ModifierOperation.Flat, 0.4f),
                new StatModifier(ModifierOperation.Flat, 0.65f),
                new StatModifier(ModifierOperation.Flat, 1f),
                new StatModifier(ModifierOperation.Flat, 1.33f), 1);

        // Upgrade_CritSplash.asset: a critical strike splinters into everything near what it hit. Eleventh and last in
        // the authored order, so only a run that has levelled far past a Descent is offered it. One stack, like the
        // Pulse: a share of one is already the knee of the measured curve (docs/31), and two would put a full card
        // outside anything that was measured.
        public static UpgradeOption CritSplash() =>
            new UpgradeOption("upgrade_crit_splash", "Splintering Crit", "Crits splinter for {0:0%} nearby",
                UpgradeStat.CritSplash,
                new StatModifier(ModifierOperation.Flat, 0.4f),
                new StatModifier(ModifierOperation.Flat, 0.6f),
                new StatModifier(ModifierOperation.Flat, 1f),
                new StatModifier(ModifierOperation.Flat, 1.33f), 1);

        // The three evolutions. Each is gated on the card every measured run fills first being full, each is a stronger
        // version of the same stat, and each raises the world's difficulty by taking it (EvolutionRule). Positions twelve
        // to fourteen, behind the width, which does not apply to an evolution.
        public static UpgradeOption RuinousEdge() =>
            new UpgradeOption("upgrade_damage_evolved", "Ruinous Edge", "+{0:0}% damage, and a harder world",
                UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Percent, 0.9f),
                new StatModifier(ModifierOperation.Percent, 1.3f),
                new StatModifier(ModifierOperation.Percent, 2f),
                new StatModifier(ModifierOperation.Percent, 2.67f), 3, null, "upgrade_damage");

        public static UpgradeOption BlurredGrip() =>
            new UpgradeOption("upgrade_attack_speed_evolved", "Blurred Grip", "+{0:0}% attack speed, and a harder world",
                UpgradeStat.AttackSpeed,
                new StatModifier(ModifierOperation.Percent, 0.7f),
                new StatModifier(ModifierOperation.Percent, 1f),
                new StatModifier(ModifierOperation.Percent, 1.6f),
                new StatModifier(ModifierOperation.Percent, 2.13f), 3, null, "upgrade_attack_speed");

        public static UpgradeOption Ironblood() =>
            new UpgradeOption("upgrade_health_evolved", "Ironblood", "+{0:0} maximum health, and a harder world",
                UpgradeStat.MaxHealth,
                new StatModifier(ModifierOperation.Flat, 55f),
                new StatModifier(ModifierOperation.Flat, 80f),
                new StatModifier(ModifierOperation.Flat, 130f),
                new StatModifier(ModifierOperation.Flat, 173f), 3, null, "upgrade_health");

        // Data/Upgrades, in the order CombatSetup carries them. The scene and this must hold the same cards in the
        // same order, or a seed shows one thing in the game and another here.
        //
        // All nine now, where five used to stand, because how many of them a run is actually offered is no longer the
        // pool's business: UpgradePool.WidthAt reads the run's level and takes that many from the front (docs/27, S2c).
        // A Descent reaches level eleven and is offered the same five, seed for seed; a swarm room levels past the
        // thresholds and reaches Iron Skin, Swift Boots, Deep Lungs and Fortune.
        public static UpgradeOption[] Pool() => new[]
        {
            Damage(), Speed(), Range(), Crit(), Health(), Armor(), MoveSpeed(), Stamina(), Luck(), Pulse(), CritSplash(),
            RuinousEdge(), BlurredGrip(), Ironblood()
        };

        // The pool the balance pins use: the two cards the game carried before rarity, at one magnitude each, so those
        // tests keep measuring what they are about - relics, weapons and the burst - and do not move whenever a card is
        // added. What the shipped pool does is measured on its own, over seeds, in DescentSeedTests.
        public static UpgradeOption[] TwoCardPool() => new[]
        {
            new UpgradeOption("upgrade_damage", "Tempered Edge", "+{0:0}% damage per hit", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Percent, 0.5f), 5),
            new UpgradeOption("upgrade_attack_speed", "Quickened Grip", "+{0:0}% attack speed", UpgradeStat.AttackSpeed,
                new StatModifier(ModifierOperation.Percent, 0.5f), 5)
        };

        // The five cards a Descent is offered, handed over whole, for a measurement that wants the width rule out of
        // the way and asks what a pool of exactly five does.
        public static UpgradeOption[] FiveCardPool() => new[] { Damage(), Speed(), Range(), Crit(), Health() };

        // Data/Upgrades/WeaponLines, in the order each Weapon_*.asset's _levelUps holds them (docs/42 section 5). Every
        // tier is the report's 1 / 1.25 / 1.5 / 2 times green (40 section 6.1), and a line on Damage is Flat so that a
        // weapon's own damage adds up before the tomes' Force multiplies it (40 section 5.3). WeaponContentTests holds
        // these equal to the assets.
        public static UpgradeOption[] SwordLines() => new[]
        {
            Line("line_sword_damage", "Sword", "+{0:0.##} damage", UpgradeStat.Damage, ModifierOperation.Flat, 2f, 2.5f, 3f, 4f),
            Line("line_sword_area", "Sword", "+{0:0.#}% area", UpgradeStat.Area, ModifierOperation.Percent, 0.12f, 0.15f, 0.18f, 0.24f),
            Line("line_sword_range", "Sword", "+{0:0.0#} reach", UpgradeStat.Range, ModifierOperation.Flat, 0.3f, 0.375f, 0.45f, 0.6f)
        };

        public static UpgradeOption[] StaffLines() => new[]
        {
            Line("line_staff_damage", "Staff", "+{0:0.##} damage", UpgradeStat.Damage, ModifierOperation.Flat, 2f, 2.5f, 3f, 4f),
            Line("line_staff_area", "Staff", "+{0:0.#}% area", UpgradeStat.Area, ModifierOperation.Percent, 0.12f, 0.15f, 0.18f, 0.24f),
            Line("line_staff_attack_speed", "Staff", "+{0:0.#}% attack speed", UpgradeStat.AttackSpeed, ModifierOperation.Percent, 0.08f, 0.1f, 0.12f, 0.16f)
        };

        public static UpgradeOption[] DaggersLines() => new[]
        {
            Line("line_daggers_damage", "Daggers", "+{0:0.##} damage", UpgradeStat.Damage, ModifierOperation.Flat, 1f, 1.25f, 1.5f, 2f),
            Line("line_daggers_crit", "Daggers", "+{0:0.#%} crit chance", UpgradeStat.CritChance, ModifierOperation.Flat, 0.05f, 0.0625f, 0.075f, 0.1f),
            Line("line_daggers_attack_speed", "Daggers", "+{0:0.#}% attack speed", UpgradeStat.AttackSpeed, ModifierOperation.Percent, 0.08f, 0.1f, 0.12f, 0.16f)
        };

        // Data/Upgrades/Evolutions (docs/42, PR 5): what a weapon becomes at level 5 with its tome at level 2, each an
        // existing behavioural stat on that weapon's runtime alone, at the values of the general card that granted it
        // before - green, orange and red as that card's, gold a third above red as that card's is. The Sword, evolving
        // with Force, pulses (Forge Pulse's 0.4 / 0.65 / 1.0, docs/30 measured the pulse as the Sword's card); the
        // Daggers, evolving with Precision, splinter their crits (Splintering Crit's 0.4 / 0.6 / 1.0, the report's Rift
        // Echo and docs/31's conclusion). The Staff has none yet. WeaponContentTests holds these equal to the assets.
        public static UpgradeOption SwordEvolution() =>
            Tiered("evolution_sword", "Resonant Edge", "Every 4th strike bursts for {0:0%} damage nearby, and a harder world",
                UpgradeStat.PulseDamage, ModifierOperation.Flat, 0.4f, 0.65f, 1f, 1.33f, 1);

        public static UpgradeOption DaggersEvolution() =>
            Tiered("evolution_daggers", "Rift Echo", "Crits splinter for {0:0%} nearby, and a harder world",
                UpgradeStat.CritSplash, ModifierOperation.Flat, 0.4f, 0.6f, 1f, 1.33f, 1);

        // Seven picks take a weapon from level 1 to its last, so no one line can be taken more often than that.
        public const int LineMaxStacks = WeaponLoadout.MaxLevel - 1;

        private static UpgradeOption Line(string id, string name, string format, UpgradeStat stat, ModifierOperation op,
            float green, float orange, float red, float gold) =>
            Tiered(id, name, format, stat, op, green, orange, red, gold, LineMaxStacks);

        // Data/Upgrades/Tomes (docs/42 section 5): global grants to every weapon held and to every weapon picked up later,
        // eight levels each, three distinct at most. Green values, and the report's tier multipliers above them.
        public static UpgradeOption Force() =>
            Tiered("tome_force", "Force", "+{0:0.#}% damage to every weapon", UpgradeStat.Damage, ModifierOperation.Percent,
                0.08f, 0.1f, 0.12f, 0.16f, UpgradeService.MaxTomeLevel);

        public static UpgradeOption Tempo() =>
            Tiered("tome_tempo", "Tempo", "+{0:0.#}% attack speed to every weapon", UpgradeStat.AttackSpeed,
                ModifierOperation.Percent, 0.06f, 0.075f, 0.09f, 0.12f, UpgradeService.MaxTomeLevel);

        public static UpgradeOption Reach() =>
            Tiered("tome_reach", "Reach", "+{0:0.#}% area to every weapon", UpgradeStat.Area, ModifierOperation.Percent,
                0.08f, 0.1f, 0.12f, 0.16f, UpgradeService.MaxTomeLevel);

        public static UpgradeOption Precision() =>
            Tiered("tome_precision", "Precision", "+{0:0.#%} crit chance to every weapon", UpgradeStat.CritChance,
                ModifierOperation.Flat, 0.04f, 0.05f, 0.06f, 0.08f, UpgradeService.MaxTomeLevel);

        public static UpgradeOption Vitality() =>
            Tiered("tome_vitality", "Vitality", "+{0:0} maximum health", UpgradeStat.MaxHealth, ModifierOperation.Flat,
                12f, 15f, 18f, 24f, UpgradeService.MaxTomeLevel);

        public static UpgradeOption[] Tomes() => new[] { Force(), Tempo(), Reach(), Precision(), Vitality() };

        // The three hero weapons as category-mode offers draw them: all three drop whatever the forge owns (docs/42,
        // owner decisions), the one a run starts with included, for its levels.
        public static HeroWeapon[] WeaponCatalogue() => new[] { Sword(), Staff(), Daggers() };

        private static UpgradeOption Tiered(string id, string name, string format, UpgradeStat stat, ModifierOperation op,
            float green, float orange, float red, float gold, int maxStacks) =>
            new UpgradeOption(id, name, format, stat,
                new StatModifier(op, green), new StatModifier(op, orange), new StatModifier(op, red), new StatModifier(op, gold),
                maxStacks);

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

        // A hero turning round on the proof floors: full steer right, then left, then right, every second and a half, for two
        // minutes. It reads nothing but the fight clock, so the scene's driver walks it through the same frames.
        public static ScriptedRoute TurningRound()
        {
            var segments = new RouteSegment[80];
            for (int i = 0; i < segments.Length; i++)
                segments[i] = new RouteSegment((i + 1) * 1.5f, i % 2 == 0 ? 1f : -1f, 0f);
            return new ScriptedRoute(segments);
        }

        // How many seconds a run stands in a swarm before it decides to leave. A swarm never ends on its own, so a
        // simulation needs a policy where the scene has a player: this one stays until the time is up, then walks to the
        // exit through the crowd and holds it (ExitRule), and either gets out or dies trying. The default is long enough
        // that a run which reaches it has not been beaten, it has run out of patience - a finding rather than a result.
        public const float DefaultSwarmSeconds = 3600f;

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

        // The loadout id of a hand-made HeroWeapon that names none; a later unnamed one gets its slot's number after it.
        public const string UnnamedWeaponId = "weapon_unnamed";


        // Always descends. cardSlot is the upgrade card taken every time (clamped when fewer cards remain); floor 2 and
        // later always Mend, floor 1 Mends only when mendOnFloorOne is set. A null route leaves the hero at the floor
        // origin for the whole run, which is the balance baseline; heroSpeed is the walking speed a route steers.
        // moreWeapons fill the loadout's later slots from the first frame, fired after heroWeapon in their order; a legacy
        // card still writes to heroWeapon alone. Null is every run the game has measured: one weapon in slot 0, on the
        // run's own seed.
        //
        // weaponCatalogue or tomes switch on category mode (docs/42, PR 4): offers are drawn by kind of pick, a picked-up
        // weapon joins the loadout the strikes walk, and the card policy prefers kinds in a fixed order (CardPicker).
        // Pass the starting weapon in the catalogue too, or it is never levelled. Null for both is the legacy engine.
        public static Result Run(Floor[] floors, int cardSlot, bool mendOnFloorOne, RelicOption relicOption = null,
            HeroWeapon heroWeapon = null, int experiencePerLevel = ExperiencePerLevel, int experienceGrowth = ExperienceGrowth,
            HeroAbility ability = null, IHeroRoute route = null, float heroSpeed = HeroSpeed,
            HeroStamina stamina = null, int seed = 0, string[] preferIds = null,
            IReadOnlyList<UpgradeOption> pool = null, int choiceCount = 2, float swarmSeconds = DefaultSwarmSeconds,
            string[] replay = null, Action<int, float, int> onSwarmWave = null, bool groundDrops = true,
            HeroWeapon[] moreWeapons = null, HeroWeapon[] weaponCatalogue = null, UpgradeOption[] tomes = null)
        {
            var run = new RunState(experiencePerLevel, 0.5f, experienceGrowth, seed);
            var rewards = new RewardService(run);
            // The loadout seeds each slot's crits as it adds it: slot 0 on the run's seed, as the one weapon always was.
            var loadout = new WeaponLoadout(run.Seed);
            HeroWeapon first = heroWeapon ?? Sword();
            bool categories = weaponCatalogue != null || tomes != null;
            WeaponRuntime weapon = categories ? first.CreateCategoryRuntime() : first.CreateRuntime();
            loadout.TryAdd(first.Id ?? UnnamedWeaponId, weapon);
            if (moreWeapons != null)
            {
                for (int i = 0; i < moreWeapons.Length; i++)
                {
                    if (!loadout.TryAdd(moreWeapons[i].Id ?? UnnamedWeaponId + (i + 1), moreWeapons[i].CreateRuntime()))
                        throw new ArgumentException("A loadout holds three different weapons at most.", nameof(moreWeapons));
                }
            }
            AbilityRuntime burst = ability?.CreateRuntime();
            var hero = new HealthState(100f);
            // The budget is made first so the stat sheet starts from the bar this run actually carries; a caller that
            // hands in its own bar (a test asking what the game was before the budget) must not have it overwritten.
            HeroStamina heroStamina = stamina ?? HeroStamina.ForCurrentRules();
            var heroStats = new HeroStats(100f, heroSpeed, heroStamina.Bar);
            WeaponCard[] cards = null;
            if (weaponCatalogue != null)
            {
                cards = new WeaponCard[weaponCatalogue.Length];
                for (int i = 0; i < cards.Length; i++)
                    cards[i] = weaponCatalogue[i].CreateCard();
            }
            var upgrades = categories
                ? new UpgradeService(run, weapon, pool ?? Pool(), choiceCount, heroStats, heroStamina.Enabled, loadout,
                    cards ?? new WeaponCard[0], tomes ?? new UpgradeOption[0])
                : new UpgradeService(run, weapon, pool ?? Pool(), choiceCount, heroStats, heroStamina.Enabled);
            // Which card to take: the first offered id in preferIds when given, else the slot, clamped to the cards shown;
            // in category mode, before the slot, the first kind of pick in CardPicker's order.
            var picker = new CardPicker(upgrades, cardSlot, preferIds, replay, categories);
            var forge = new ForgeService(run, hero);
            var choices = new RunChoices(upgrades, forge);
            RelicRuntime relic = relicOption != null ? new RelicRuntime(relicOption) : null;
            var result = new Result
            {
                EarliestStrikeAfterSpawn = float.MaxValue,
                FloorOneBossLevel = -1,
                ClosestEnemyGapSquared = float.MaxValue,
                ClosestHeroDistanceSquared = float.MaxValue
            };
            // One hero, placed where every run starts and kept across waves, rooms and floors, as in the scene.
            var heroMotion = new HeroMotion(heroSpeed);
            // One movement budget for the whole run, as the scene keeps one on HeroMovementInput.
            // The sheet reaches the health, the walk and the bar through the one binder the scene also calls, so a card
            // changes the same fight in both. Bound after all three exist and pushed once, as CombatSetup does.
            heroStats.Changed += () => HeroStatsBinding.Apply(heroStats, hero, heroMotion, heroStamina);
            HeroStatsBinding.Apply(heroStats, hero, heroMotion, heroStamina);
            heroMotion.Place(0f, 0f, Platform);
            RouteView view = route != null ? new RouteView() : null;
            var chest = new ChestState();
            // The Descent's rewards on the floor, one set for the run as CombatSetup keeps one. groundDrops false is the
            // game before them (docs/27, S6d) - every kill paid at once - kept so a measurement can say what they changed.
            GroundDrops drops = groundDrops ? new GroundDrops(run) : null;

            for (int f = 0; f < floors.Length; f++)
            {
                bool cleared = RunFloor(floors[f], run, rewards, loadout, burst, hero, forge, choices, relic, picker, f > 0 || mendOnFloorOne, heroMotion, heroStamina, route, view, upgrades, heroStats, chest, swarmSeconds, onSwarmWave, drops, ref result);
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
            for (int slot = 0; slot < loadout.Count; slot++)
            {
                result.PulsesFired += loadout.At(slot).Runtime.PulsesFired;
                result.SplintersFired += loadout.At(slot).Runtime.SplintersFired;
            }
            result.Difficulty = heroStats.Difficulty;
            result.EvolutionsTaken = upgrades.EvolutionsTaken;
            result.ReplayMisses = picker.ReplayMisses;
            result.WeaponsHeld = loadout.Count;
            for (int slot = 0; slot < loadout.Count; slot++)
                result.HighestWeaponLevel = Math.Max(result.HighestWeaponLevel, loadout.At(slot).Level);
            result.TomesHeld = upgrades.TomesHeld;
            if (upgrades.Tomes != null)
                foreach (UpgradeOption tome in upgrades.Tomes)
                    result.HighestTomeLevel = Math.Max(result.HighestTomeLevel, upgrades.TomeLevel(tome.Id));
            result.Picks = picker.Taken.ToArray();
            result.UpgradesApplied = run.UpgradesApplied;
            result.HeroFloorX = heroMotion.X;
            result.HeroFloorY = heroMotion.Y;
            return result;
        }

        // The one chest the run has at a time, the way the scene keeps one spawner: a new combat room moves it and
        // closes it again, and its index counts every chest the run has placed, across floors.
        private sealed class ChestState
        {
            public int Index = -1;
            public float X;
            public float Y;
            public ChestReward Reward;
            public bool Placed;
            public bool Open;
        }

        private static bool RunFloor(Floor floor, RunState run, RewardService rewards, WeaponLoadout loadout, AbilityRuntime burst,
            HealthState hero, ForgeService forge, RunChoices choices, RelicRuntime relic, CardPicker cardSlot, bool useMend,
            HeroMotion heroMotion, HeroStamina stamina, IHeroRoute route, RouteView view, UpgradeService upgrades,
            HeroStats heroStats, ChestState chest, float swarmSeconds, Action<int, float, int> onSwarmWave, GroundDrops drops,
            ref Result result)
        {
            const float step = 1f / 60f;
            // The first weapon, which everything written for one weapon reads: the route's reach, the relic's damage.
            WeaponRuntime weapon = loadout.Primary;
            var waves = new int[floor.Rooms.Length];
            var endless = new bool[floor.Rooms.Length];
            for (int i = 0; i < waves.Length; i++)
            {
                waves[i] = floor.Rooms[i]?.Length ?? 0;
                endless[i] = floor.IsSwarm(i);
            }
            var progress = new FloorProgress(waves, endless);
            // Waves started on this floor so far; each wave starts one corner further round the arena.
            int waveOrdinal = 0;
            // Whether the pause before the next wave was lived frame by frame - a hero walking for its drops - so the weapon
            // has already cooled down through it and the next wave must not add the pause again.
            bool pauseLived = false;
            // The last wave's pack, which a forge's walk out still shows the route: dead, as the scene's is.
            PackMotion lastMotion = null;
            HealthState[] lastEnemies = null;
            WeaponRuntime[] lastWeapons = null;

            for (FloorStep next = progress.Advance(); next.Kind != FloorStepKind.Cleared; next = progress.Advance())
            {
                if (next.Kind == FloorStepKind.NonCombatRoom)
                {
                    forge.Open(new[] { Mend(), Temper() });
                    choices.TrySelect(choices.Current, useMend ? 0 : 1);
                    ChooseUpgrades(choices, cardSlot);
                    // The walk out of the forge: EncounterController's transition wait, in which a hero can fetch what the
                    // last room left lying. It starts from a fresh steer, as the scene's route is asked again after the
                    // forge's answer.
                    float forgeSteerX = 0f;
                    float forgeSteerY = 0f;
                    if (route != null && drops != null && drops.Count > 0 && lastMotion != null)
                        Ask(route, view, lastMotion, lastEnemies, lastWeapons, heroMotion, weapon.Range, result.FightSeconds,
                            chest, false, drops, out forgeSteerX, out forgeSteerY);
                    pauseLived = Linger(run, loadout, burst, hero, choices, cardSlot, heroMotion, stamina, route, view,
                        lastMotion, lastEnemies, lastWeapons, chest, upgrades, floor, drops, ref forgeSteerX, ref forgeSteerY,
                        ref result);
                    continue;
                }

                // A room's first wave brings its chest, as ChestSpawner does on the encounter that starts a new room;
                // later waves of the same room leave it where it is, opened or not.
                if (next.WaveIndex == 0)
                {
                    chest.Index++;
                    ChestRule.SpotFor(next.RoomIndex, Platform, out chest.X, out chest.Y);
                    chest.Reward = ChestRule.RewardFor(next.RoomIndex, chest.Index, run.Seed);
                    chest.Placed = true;
                    chest.Open = false;
                }

                // A swarm room has no waves: one pack for its whole life, refilled as it falls, hardening by the clock,
                // left by walking out (docs/27, S6c). It runs as one loop and hands the floor back when it is over.
                if (floor.IsSwarm(next.RoomIndex))
                {
                    bool survived = RunSwarmRoom(floor, next.RoomIndex, run, rewards, loadout, burst, hero, choices, relic,
                        cardSlot, heroMotion, stamina, route, view, upgrades, heroStats, chest, swarmSeconds, onSwarmWave,
                        ref waveOrdinal, ref result);
                    if (!survived)
                    {
                        result.DeathRoom = $"{next.RoomIndex + 1}.{result.SwarmWave}";
                        return false;
                    }
                    progress.LeaveEndlessRoom();
                    continue;
                }

                Enemy[] pack = floor.Rooms[next.RoomIndex][next.WaveIndex];
                var enemies = new HealthState[pack.Length];
                var enemyWeapons = new WeaponRuntime[pack.Length];
                var enrages = new EnrageRule[pack.Length];
                var rewarded = new bool[pack.Length];
                // How long each enemy has been held back without a break, where it stood after the last frame and the last step
                // it took.
                var steps = new StepLog(pack.Length);
                // The pack forms up round wherever the hero stands now, keeping a body's spacing between its spots.
                float spawnX = heroMotion.X;
                float spawnY = heroMotion.Y;
                var motion = new PackMotion(BodySpacing, PackLayout.HalfWidth * FormationSpacing, Platform, spawnX, spawnY);
                var placements = new EntryPlacement[pack.Length];
                EntrySides.Place(waveOrdinal, pack.Length, spawnX, spawnY, Platform, EntryDepth, FormationSpacing, BodySpacing, placements);
                // An ordinary wave hardens only by the run's difficulty, as EncounterController applies it.
                float scale = heroStats.Difficulty;
                float damageBonus = FloorScaling.DamageBonus(floor.DamageMultiplier * scale, floor.ModifierDamagePercent);
                for (int i = 0; i < pack.Length; i++)
                {
                    enemies[i] = new HealthState(FloorScaling.Health(pack[i].Health, floor.HealthMultiplier * scale, 0f));
                    enemyWeapons[i] = new WeaponRuntime(pack[i].Damage, pack[i].Interval, pack[i].Reach, pack[i].InitialDelay);
                    if (damageBonus != 0f)
                        enemyWeapons[i].AddModifier(UpgradeStat.Damage, new StatModifier(ModifierOperation.Percent, damageBonus));
                    enrages[i] = pack[i].EnrageAt > 0f ? new EnrageRule(pack[i].EnrageAt) : null;
                    motion.Add(enemies[i], placements[i], pack[i].Speed, pack[i].Reach);
                    steps.X[i] = steps.LastX[i] = motion.XOf(i);
                    steps.Y[i] = steps.LastY[i] = motion.YOf(i);
                }
                waveOrdinal++;

                if (result.ClearedFloors == 0 && result.FloorOneBossLevel < 0 && next.RoomIndex == floor.Rooms.Length - 1 &&
                    next.WaveIndex == 0)
                    result.FloorOneBossLevel = run.Level;
                float startSeconds = result.FightSeconds;
                // The steer the hero holds into the next frame, at rest on the frame a wave spawns, and when this wave first
                // landed a hit on the hero. No frame passes during the advance delay or an instant choice, so the hero
                // stands still through both, as it does in the scene while a choice panel is open.
                float steerX = 0f;
                float steerY = 0f;
                float firstStrike = -1f;
                if (!pauseLived)
                    Tick(loadout, AdvanceDelay);
                pauseLived = false;
                // Every wave starts with a full movement budget, as the scene does: HeroMovementInput fills it on
                // EncounterController.EncounterStarted, which is the same moment as this line.
                stamina.Fill();
                for (int frame = 0; frame < 200000 && hero.IsAlive && AnyAlive(enemies); frame++)
                {
                    bool moved = false;
                    if (frame > 0)
                    {
                        Tick(loadout, step);
                        burst?.Tick(step);
                        for (int i = 0; i < pack.Length; i++)
                            enemyWeapons[i].Tick(step);
                        // The scene's order: the hero walks from the steer it holds, then the pack steps toward where it is.
                        // HeroMovementInput reads the movement budget before walking and spends it after, on the
                        // displacement that really happened; this is the same two lines in the same order.
                        moved = heroMotion.Move(steerX, steerY, step * stamina.SpeedFactor, Platform);
                        stamina.Step(step, moved);
                        if (stamina.IsEmpty)
                            result.StaminaEmptySeconds += step;
                        motion.Step(step, heroMotion.X, heroMotion.Y);
                        result.FightSeconds += step;
                        ObserveSteps(motion, enemies, steps, step, result.FightSeconds, ref result);
                        // The drops, where the hero now stands: EncounterController reads them after the pack steps and
                        // before the chest is read. Not on the spawn frame, whose reading the pause before it already made.
                        if (drops != null)
                            result.DropsCollected += drops.CollectNear(heroMotion.X, heroMotion.Y);
                    }

                    // The chest is read once the hero has walked, as ChestSpawner reads the hero's floor position in its
                    // own Update; a hero already standing on a freshly placed chest opens it on the wave's first frame.
                    OpenChestIfReached(chest, hero, run, floor, upgrades, heroMotion, ref result);

                    int struck = StrikeInSlotOrder(loadout, motion, enemies, heroMotion.X, heroMotion.Y);
                    if (moved)
                        result.StrikesWhileMoving += struck;
                    Resolve(pack, enemies, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, drops, motion, ref result);
                    if (burst != null && burst.IsReady)
                    {
                        IReadOnlyList<IDamageable> inReach = WithinReachOfHero(motion, enemies, burst.Radius, heroMotion.X, heroMotion.Y);
                        if (inReach.Count > 0)
                        {
                            burst.TryUse(null, inReach);
                            result.AbilityUses++;
                            Resolve(pack, enemies, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, drops, motion, ref result);
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
                            Resolve(pack, enemies, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, drops, motion, ref result);
                        }
                    }

                    // A level-up a drop paid for this frame is answered now, at the frame's end, where the scene's player
                    // answers an offer that opened during it - so this frame's blows were struck without the new card, in
                    // both. A kill no longer pays at once, so Resolve asks nothing in the Descent.
                    ChooseUpgrades(choices, cardSlot);
                    Observe(motion, enemies, heroMotion, ref result);
                    if (route != null)
                        Ask(route, view, motion, enemies, enemyWeapons, heroMotion, weapon.Range, result.FightSeconds, chest,
                            false, drops, out steerX, out steerY);
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

                lastMotion = motion;
                lastEnemies = enemies;
                lastWeapons = enemyWeapons;
                // The floor's last wave ends it on the kill, as EncounterController advances at once; every other wave is
                // followed by the advance delay, which a hero can walk in.
                if (!progress.IsFinalWave)
                    pauseLived = Linger(run, loadout, burst, hero, choices, cardSlot, heroMotion, stamina, route, view, motion,
                        enemies, enemyWeapons, chest, upgrades, floor, drops, ref steerX, ref steerY, ref result);
            }

            // The floor's last enemy has fallen: whatever still lies flies to the hero, as CombatSetup sweeps the floor
            // before its checkpoint, and the level-ups it pays are answered before anything else.
            if (drops != null)
                result.DropsSwept += drops.CollectAll();
            ChooseUpgrades(choices, cardSlot);
            return true;
        }

        // The pause after a cleared wave, or a forge's walk out: EncounterController's advance delay, counted only on frames
        // no choice is open, as EncounterProgress.Tick and the transition wait count it. With drops on the floor it is
        // lived frame by frame, as the scene lives it - the hero walking free on its route's steer, since
        // HeroMovementInput spends the bar only while something is alive, picking up what it reaches and passing the
        // chest - and the next wave spawns on the frame the pause runs out; a drop that pays a level opens a choice, and
        // that frame does not count, so the pause is a frame longer in both. With nothing on the floor nothing can happen
        // in it, and the next wave adds the pause to the weapon's cooldown whole, as every number before drops was
        // measured. Returns whether the pause was lived frame by frame.
        private static bool Linger(RunState run, WeaponLoadout loadout, AbilityRuntime burst, HealthState hero,
            RunChoices choices, CardPicker cardSlot, HeroMotion heroMotion, HeroStamina stamina, IHeroRoute route,
            RouteView view, PackMotion motion, HealthState[] enemies, WeaponRuntime[] enemyWeapons, ChestState chest,
            UpgradeService upgrades, Floor floor, GroundDrops drops, ref float steerX, ref float steerY, ref Result result)
        {
            if (drops == null || drops.Count == 0)
                return false;

            const float step = 1f / 60f;
            float waited = 0f;
            for (int frame = 0; frame < 100000 && hero.IsAlive; frame++)
            {
                Tick(loadout, step);
                burst?.Tick(step);
                heroMotion.Move(steerX, steerY, step * stamina.SpeedFactor, Platform);
                stamina.Step(step, false);
                result.DropsCollected += drops.CollectNear(heroMotion.X, heroMotion.Y);
                // A drop that paid a level opened a choice on this frame, and a frame with a choice open does not count.
                bool counted = !choices.IsOpen;
                if (counted)
                    waited += step;
                bool runsOut = counted && waited >= AdvanceDelay;
                // On the frame the pause runs out the next wave spawns before ChestSpawner reads, so the reading on that
                // frame is the new wave's own, made on its first frame.
                if (!runsOut)
                    OpenChestIfReached(chest, hero, run, floor, upgrades, heroMotion, ref result);
                ChooseUpgrades(choices, cardSlot);
                if (runsOut)
                    return true;
                if (route != null && motion != null)
                    Ask(route, view, motion, enemies, enemyWeapons, heroMotion, loadout.Primary.Range, result.FightSeconds,
                        chest, false, drops, out steerX, out steerY);
            }
            return true;
        }

        // One swarm room, mirroring EncounterController's continuous mode frame for frame. The pack is at most ten places
        // for the room's whole life; SwarmDirector - the one body the scene also asks - says when a place fills and with
        // which enemy of the ladder at what difficulty. The run leaves by standing on the exit for ExitRule.DwellSeconds,
        // which this simulation does once swarmSeconds have passed, walking straight at the exit through whatever stands
        // in the way. Returns false if the hero died.
        private static bool RunSwarmRoom(Floor floor, int roomIndex, RunState run, RewardService rewards, WeaponLoadout loadout,
            AbilityRuntime burst, HealthState hero, RunChoices choices, RelicRuntime relic, CardPicker cardSlot,
            HeroMotion heroMotion, HeroStamina stamina, IHeroRoute route, RouteView view, UpgradeService upgrades,
            HeroStats heroStats, ChestState chest, float swarmSeconds, Action<int, float, int> onSwarmWave,
            ref int waveOrdinal, ref Result result)
        {
            const float step = 1f / 60f;
            WeaponRuntime weapon = loadout.Primary;
            const int Places = PackLayout.MaxPackSize;
            Enemy[][] ladder = floor.Rooms[roomIndex];
            var sizes = new int[ladder.Length];
            for (int i = 0; i < ladder.Length; i++)
                sizes[i] = ladder[i].Length;
            var director = new SwarmDirector(sizes);
            var spawns = new List<SwarmDirector.Spawn>(Places);

            var pack = new Enemy[Places];
            var enemies = new HealthState[Places];
            var enemyWeapons = new WeaponRuntime[Places];
            var enrages = new EnrageRule[Places];
            var rewarded = new bool[Places];
            var fell = new bool[Places];
            var steps = new StepLog(Places);
            var motion = new PackMotion(BodySpacing, PackLayout.HalfWidth * FormationSpacing, Platform, heroMotion.X,
                heroMotion.Y);
            var placements = new EntryPlacement[Places];
            ExitRule.SpotFor(Platform, out float exitX, out float exitY);
            var dwell = new ExitDwell();
            // The corner the next entry comes from, carried in a local because a local function cannot hold a ref; it is
            // written back on every way out of this method.
            int ordinal = waveOrdinal;
            // When the ladder's last wave first walked in, written back with the ordinal for the same reason.
            float ladderTop = 0f;
            // A place not yet in use holds a fallen stand-in, already paid for and already reported, so every helper
            // written for a wave - each of which skips the fallen before it reads a position - reads a swarm the same way
            // without ever touching a place the pack does not have.
            var unused = new HealthState(1f);
            unused.ApplyDamage(new DamageContext(1f));
            for (int i = 0; i < Places; i++)
            {
                enemies[i] = unused;
                rewarded[i] = true;
                fell[i] = true;
            }

            // Places a batch of spawns the way EncounterController does: each spawn is its own one-enemy entry, one corner
            // further round the arena than the last, placed round wherever the hero stands now.
            void Place(List<SwarmDirector.Spawn> batch, int count)
            {
                for (int s = 0; s < count; s++)
                {
                    SwarmDirector.Spawn spawn = batch[s];
                    Enemy definition = ladder[spawn.Wave][spawn.Index];
                    if (spawn.Wave == ladder.Length - 1 && ladderTop <= 0f)
                        ladderTop = director.Seconds;
                    EntrySides.Place(ordinal, 1, heroMotion.X, heroMotion.Y, Platform, EntryDepth, FormationSpacing,
                        BodySpacing, placements);
                    ordinal++;
                    float scale = spawn.Difficulty * heroStats.Difficulty;
                    float damageBonus = FloorScaling.DamageBonus(floor.DamageMultiplier * scale, floor.ModifierDamagePercent);
                    var health = new HealthState(FloorScaling.Health(definition.Health, floor.HealthMultiplier * scale, 0f));
                    var enemyWeapon = new WeaponRuntime(definition.Damage, definition.Interval, definition.Reach,
                        definition.InitialDelay);
                    if (damageBonus != 0f)
                        enemyWeapon.AddModifier(UpgradeStat.Damage, new StatModifier(ModifierOperation.Percent, damageBonus));
                    int slot = spawn.Slot;
                    if (slot < motion.Count)
                        motion.Replace(slot, health, placements[0], definition.Speed, definition.Reach);
                    else
                        motion.Add(health, placements[0], definition.Speed, definition.Reach);
                    pack[slot] = definition;
                    enemies[slot] = health;
                    enemyWeapons[slot] = enemyWeapon;
                    enrages[slot] = definition.EnrageAt > 0f ? new EnrageRule(definition.EnrageAt) : null;
                    rewarded[slot] = false;
                    fell[slot] = false;
                    steps.X[slot] = steps.LastX[slot] = motion.XOf(slot);
                    steps.Y[slot] = steps.LastY[slot] = motion.YOf(slot);
                }
            }

            // Tells the director about every place whose enemy fell since it last asked.
            void NoticeFallen()
            {
                for (int i = 0; i < motion.Count; i++)
                {
                    if (!fell[i] && enemies[i] != null && !enemies[i].IsAlive)
                    {
                        fell[i] = true;
                        director.Fell(i);
                    }
                }
            }

            int Alive()
            {
                int alive = 0;
                for (int i = 0; i < motion.Count; i++)
                    if (enemies[i] != null && enemies[i].IsAlive)
                        alive++;
                return alive;
            }

            // Level-ups wait for the room's sittings, as EncounterController holds them before it opens the pack.
            upgrades.Hold();
            director.Open(spawns);
            Place(spawns, spawns.Count);
            int reportedStep = -1;
            float steerX = 0f;
            float steerY = 0f;
            Tick(loadout, AdvanceDelay);
            // The bar fills once, as the room opens: there are no wave starts left to fill it again, which is the point.
            stamina.Fill();

            for (int frame = 0; frame < 216000 && hero.IsAlive; frame++)
            {
                int stepIndex = (int)(director.Seconds / SwarmRule.StepSeconds);
                if (stepIndex > reportedStep)
                {
                    reportedStep = stepIndex;
                    result.SwarmWave = stepIndex + 1;
                    onSwarmWave?.Invoke(stepIndex + 1, hero.Current, run.Level);
                }

                bool moved = false;
                if (frame > 0)
                {
                    Tick(loadout, step);
                    burst?.Tick(step);
                    for (int i = 0; i < motion.Count; i++)
                        enemyWeapons[i].Tick(step);
                    moved = heroMotion.Move(steerX, steerY, step * stamina.SpeedFactor, Platform);
                    // HeroMovementInput spends the bar only while something is alive to fight, read before this frame's
                    // refill because it runs before EncounterController. A continuous room has frames with nobody alive -
                    // the breath between a kill and its replacement - and walking through those is free in the scene, so
                    // it is free here too.
                    stamina.Step(step, moved && Alive() > 0);
                    if (stamina.IsEmpty)
                        result.StaminaEmptySeconds += step;
                    motion.Step(step, heroMotion.X, heroMotion.Y);
                    result.FightSeconds += step;
                    ObserveSteps(motion, enemies, steps, step, result.FightSeconds, ref result);

                    // EncounterController's continuous step, at the same point in the frame: the clock moves on and
                    // whatever is due walks in. Enemies placed now act from the next frame, as an enemy spawned during
                    // the scene's Update does.
                    director.Tick(step, Alive(), spawns);
                    Place(spawns, spawns.Count);

                    // The way out, read where the hero now stands. Leaving ends the room before anything strikes, as the
                    // scene releases the pack in the same Update that notices the exit has been held.
                    if (dwell.Step(step, !moved && ExitRule.IsWithinReach(heroMotion.X, heroMotion.Y, exitX, exitY)))
                    {
                        result.SwarmSeconds = director.Seconds;
                        waveOrdinal = ordinal;
                        result.LadderTopSeconds = ladderTop;
                        // Whatever the room held back is offered on the way out, as the scene releases it.
                        upgrades.Release();
                        ChooseUpgrades(choices, cardSlot);
                        return true;
                    }

                    // A sitting: the held level-ups open here and are taken at this frame's Resolve, after the hero's
                    // strike - where the scene's player answers an offer opened in this Update, before the next frame.
                    if (director.OfferDue)
                    {
                        upgrades.OpenSitting();
                        result.Sittings++;
                    }
                }

                OpenChestIfReached(chest, hero, run, floor, upgrades, heroMotion, ref result);

                HealthState[] current = enemies;
                int struck = StrikeInSlotOrder(loadout, motion, current, heroMotion.X, heroMotion.Y);
                if (moved)
                    result.StrikesWhileMoving += struck;
                Resolve(pack, current, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, null, motion, ref result);
                NoticeFallen();
                // A sitting opened this frame is answered here even when nothing died, which is the only time Resolve
                // would have asked; every other frame there is nothing to answer.
                ChooseUpgrades(choices, cardSlot);
                if (burst != null && burst.IsReady)
                {
                    IReadOnlyList<IDamageable> inReach = WithinReachOfHero(motion, current, burst.Radius, heroMotion.X,
                        heroMotion.Y);
                    if (inReach.Count > 0)
                    {
                        burst.TryUse(null, inReach);
                        result.AbilityUses++;
                        Resolve(pack, current, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, null, motion, ref result);
                        NoticeFallen();
                    }
                }

                for (int i = 0; i < motion.Count && hero.IsAlive; i++)
                {
                    if (!current[i].IsAlive || !IsHeroInReach(motion, i, enemyWeapons[i].Range, heroMotion.X, heroMotion.Y))
                        continue;
                    float before = hero.Current;
                    if (!enemyWeapons[i].TryAttack(hero, current[i]))
                        continue;

                    result.HeroHitsTaken++;
                    result.HeroDamageTaken += before - hero.Current;
                    if (relic != null && hero.Current < before)
                    {
                        relic.OnHeroDamaged(hero, current[i], weapon.Damage);
                        Resolve(pack, current, enemyWeapons, enrages, rewarded, floor, rewards, choices, cardSlot, null, motion, ref result);
                        NoticeFallen();
                    }
                }

                Observe(motion, current, heroMotion, ref result);
                // The steer for the next frame: the run's own route while it is staying, and straight for the exit once it
                // has decided to go.
                if (director.Seconds >= swarmSeconds)
                {
                    // Walk to it, then stop on it: the hold only counts while the hero stands still.
                    if (ExitRule.IsWithinReach(heroMotion.X, heroMotion.Y, exitX, exitY))
                    {
                        steerX = 0f;
                        steerY = 0f;
                    }
                    else
                    {
                        SteerToward(heroMotion.X, heroMotion.Y, exitX, exitY, out steerX, out steerY);
                    }
                }
                else if (route != null)
                {
                    // The room's own clock, which is what the scene hands its route in a swarm, and the way out.
                    view.ExitX = exitX;
                    view.ExitY = exitY;
                    Ask(route, view, motion, current, enemyWeapons, heroMotion, weapon.Range, director.Seconds, chest,
                        true, null, out steerX, out steerY);
                }
            }

            result.SwarmSeconds = director.Seconds;
            waveOrdinal = ordinal;
            result.LadderTopSeconds = ladderTop;
            return hero.IsAlive;
        }

        // A full-strength steer straight at a point, through the one distance body the arena uses everywhere.
        private static void SteerToward(float fromX, float fromY, float toX, float toY, out float steerX, out float steerY)
        {
            float distanceSquared = ArenaFloor.FloorDistanceSquared(toX, toY, fromX, fromY);
            if (distanceSquared <= 0f)
            {
                steerX = 0f;
                steerY = 0f;
                return;
            }
            float length = (float)Math.Sqrt(distanceSquared);
            steerX = (toX - fromX) / length;
            steerY = (toY - fromY) / length;
        }

        // The hero walks onto a closed chest: it mends or pays, and then grants whatever book it held, in the order
        // ChestSpawner.Open applies them.
        private static void OpenChestIfReached(ChestState chest, HealthState hero, RunState run, Floor floor,
            UpgradeService upgrades, HeroMotion heroMotion, ref Result result)
        {
            if (!chest.Placed || chest.Open || !hero.IsAlive || run.HasEnded ||
                !ChestRule.IsWithinReach(heroMotion.X, heroMotion.Y, chest.X, chest.Y))
                return;

            chest.Open = true;
            result.ChestsOpened++;
            if (chest.Reward.Kind == ChestRewardKind.Heal)
                hero.Heal(hero.Maximum * chest.Reward.HealFraction);
            else
                run.AddGold(FloorScaling.Gold(chest.Reward.Gold, floor.ModifierGoldPercent));
            if (chest.Reward.HoldsBook && upgrades.TryGrantBook(chest.Index, out _, out _))
                result.BooksTaken++;
        }

        // Enrage checks and kill rewards after any damage to the pack, in slot order like the scene's death callbacks.
        // A Descent kill drops its reward where the enemy fell (drops given); a swarm's pays at once and its level-up is
        // answered there and then, as it always was (drops null).
        private static void Resolve(Enemy[] pack, HealthState[] enemies, WeaponRuntime[] enemyWeapons, EnrageRule[] enrages, bool[] rewarded,
            Floor floor, RewardService rewards, RunChoices choices, CardPicker cardSlot, GroundDrops drops, PackMotion motion,
            ref Result result)
        {
            for (int i = 0; i < pack.Length; i++)
            {
                if (enrages[i] != null && enrages[i].Evaluate(enemies[i].Current, enemies[i].Maximum))
                    enemyWeapons[i].AddModifier(UpgradeStat.AttackSpeed, new StatModifier(ModifierOperation.Percent, 1f));
                if (enemies[i].IsAlive || rewarded[i])
                    continue;

                rewarded[i] = true;
                result.Kills++;
                int gold = FloorScaling.Gold(pack[i].Gold, floor.ModifierGoldPercent);
                if (drops != null)
                {
                    rewards.TryDropKill(enemies[i], pack[i].Experience, gold, motion.XOf(i), motion.YOf(i), drops);
                    continue;
                }
                rewards.TryAwardKill(enemies[i], pack[i].Experience, gold);
                ChooseUpgrades(choices, cardSlot);
            }
        }

        // Every held weapon's cooldown, in slot order, as AttackController ticks each before it fires it.
        private static void Tick(WeaponLoadout loadout, float seconds)
        {
            for (int slot = 0; slot < loadout.Count; slot++)
                loadout.At(slot).Runtime.Tick(seconds);
        }

        // Mirrors AttackController.Update for the hero: each held weapon in slot order acquires the nearest living enemy
        // within its own reach and strikes it if it is ready, the next slot only after the one before has struck, so a
        // later slot never strikes what an earlier one felled this frame - Acquire skips the fallen, as Targeting's
        // IsValid does. Each gathers its circles through the runtime's own radii, and nothing for a pulse or a splinter
        // it does not carry, exactly as AttackController skips them. Returns how many strikes landed. The caller
        // resolves kills once every slot has struck, as the scene's death callbacks follow the one Update.
        internal static int StrikeInSlotOrder(WeaponLoadout loadout, PackMotion motion, HealthState[] enemies, float heroX,
            float heroY)
        {
            int struck = 0;
            for (int slot = 0; slot < loadout.Count; slot++)
            {
                WeaponRuntime weapon = loadout.At(slot).Runtime;
                int target = Acquire(motion, enemies, weapon.Range, heroX, heroY);
                if (target >= 0 && weapon.IsReady &&
                    weapon.TryAttack(enemies[target], null,
                        Nearby(motion, enemies, target, weapon.SplashRadius),
                        // The pulse's circle is drawn round the hero, so it is gathered by the same body
                        // Targeting.CollectNear mirrors for the burst.
                        weapon.PulseDamage > 0f
                            ? WithinReachOfHero(motion, enemies, weapon.PulseRadius, heroX, heroY)
                            : null,
                        // The splinter's circle is round the struck enemy, which is what Nearby measures.
                        weapon.CritSplash > 0f
                            ? Nearby(motion, enemies, target, weapon.CritSplashRadius)
                            : null))
                    struck++;
            }
            return struck;
        }

        // Mirrors Targeting.Acquire for a hero at the floor origin, where it has always stood.
        internal static int Acquire(PackMotion motion, HealthState[] enemies, float range) => Acquire(motion, enemies, range, 0f, 0f);

        // Mirrors Targeting.Acquire for the hero: the nearest living enemy within range, the earlier slot on equal distance.
        // Distances come from ArenaFloor.FloorDistanceSquared, the one body Targeting also calls, candidate first, so a
        // near-tie resolves the same way here as in the scene.
        internal static int Acquire(PackMotion motion, HealthState[] enemies, float range, float heroX, float heroY)
        {
            int nearest = -1;
            float nearestDistanceSquared = range * range;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (!enemies[i].IsAlive)
                    continue;
                float distanceSquared = ArenaFloor.FloorDistanceSquared(motion.XOf(i), motion.YOf(i), heroX, heroY);
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
                if (enemies[i].IsAlive &&
                    ArenaFloor.FloorDistanceSquared(motion.XOf(i), motion.YOf(i), heroX, heroY) <= radiusSquared)
                    inReach.Add(enemies[i]);
            }
            return inReach;
        }

        // Mirrors an enemy's Targeting.Acquire, whose only candidate is the hero: the hero's position less the enemy's, the
        // way the scene subtracts it.
        private static bool IsHeroInReach(PackMotion motion, int enemy, float reach, float heroX, float heroY) =>
            ArenaFloor.FloorDistanceSquared(heroX, heroY, motion.XOf(enemy), motion.YOf(enemy)) <= reach * reach;

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

        // Two reversals of one enemy this close together are a flicker: six frames at 60 Hz.
        public const float FlickerSeconds = 0.1f;
        // The shortest step the reversal count looks at: a quarter of a texel, since the art draws 32 texels to a floor unit.
        public const float VisibleStep = 1f / 128f;

        // One wave's record of how its enemies walked: how long each has been held back without a break, where it stood when
        // its last step was counted, that step, and when it last turned back.
        private sealed class StepLog
        {
            public readonly float[] Stalled;
            public readonly float[] X;
            public readonly float[] Y;
            public readonly float[] StepX;
            public readonly float[] StepY;
            public readonly float[] Reversed;
            public readonly float[] LastX;
            public readonly float[] LastY;
            public readonly float[] Moved;

            public StepLog(int count)
            {
                Stalled = new float[count];
                X = new float[count];
                Y = new float[count];
                StepX = new float[count];
                StepY = new float[count];
                Reversed = new float[count];
                LastX = new float[count];
                LastY = new float[count];
                Moved = new float[count];
                for (int i = 0; i < count; i++)
                    Reversed[i] = float.MinValue;
            }
        }

        // Counts the frame against every living enemy the pack held back on its step, restarts the count of every other, and
        // counts a step taken back against the previous one.
        private static void ObserveSteps(PackMotion motion, HealthState[] enemies, StepLog steps, float step, float seconds, ref Result result)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                if (!enemies[i].IsAlive)
                {
                    steps.Stalled[i] = 0f;
                    continue;
                }

                if (motion.IsStalled(i))
                {
                    steps.Stalled[i] += step;
                    result.EnemyStallSeconds += step;
                    if (steps.Stalled[i] > result.LongestEnemyStallSeconds)
                        result.LongestEnemyStallSeconds = steps.Stalled[i];
                }
                else
                {
                    steps.Stalled[i] = 0f;
                }

                // How long it had stood still before this frame, if it moved at all on it.
                bool moved = motion.XOf(i) != steps.LastX[i] || motion.YOf(i) != steps.LastY[i];
                bool setsOff = moved && seconds - step - steps.Moved[i] >= FlickerSeconds;
                if (moved)
                {
                    steps.LastX[i] = motion.XOf(i);
                    steps.LastY[i] = motion.YOf(i);
                    steps.Moved[i] = seconds;
                }
                if (setsOff)
                {
                    steps.StepX[i] = 0f;
                    steps.StepY[i] = 0f;
                    steps.X[i] = motion.XOf(i);
                    steps.Y[i] = motion.YOf(i);
                    continue;
                }

                // A step is measured from where the enemy stood when its last step was counted, so small moves add up.
                float stepX = motion.XOf(i) - steps.X[i];
                float stepY = motion.YOf(i) - steps.Y[i];
                if (stepX * stepX + stepY * stepY < VisibleStep * VisibleStep)
                    continue;
                steps.X[i] = motion.XOf(i);
                steps.Y[i] = motion.YOf(i);
                if (stepX * steps.StepX[i] + stepY * steps.StepY[i] < 0f)
                {
                    result.EnemyStepReversals++;
                    if (seconds - steps.Reversed[i] <= FlickerSeconds)
                        result.EnemyStepFlickers++;
                    steps.Reversed[i] = seconds;
                }
                steps.StepX[i] = stepX;
                steps.StepY[i] = stepY;
            }
        }

        // Fills the route's view at the end of a frame and takes the steer the hero holds through the next one: the scene's
        // one-frame input lag, where the PlayMode driver calls HeroMovementInput.Hold after the frame's Update calls.
        private static void Ask(IHeroRoute route, RouteView view, PackMotion motion, HealthState[] enemies,
            WeaponRuntime[] enemyWeapons, HeroMotion heroMotion, float heroReach, float seconds, ChestState chest,
            bool hasExit, GroundDrops drops, out float steerX, out float steerY)
        {
            view.HasExit = hasExit;
            view.DropCount = drops != null ? Math.Min(drops.Count, RouteView.MaxDrops) : 0;
            for (int i = 0; i < view.DropCount; i++)
            {
                view.DropX[i] = drops.At(i).X;
                view.DropY[i] = drops.At(i).Y;
            }
            view.ChestX = chest.X;
            view.ChestY = chest.Y;
            view.ChestClosed = chest.Placed && !chest.Open;
            view.HeroX = heroMotion.X;
            view.HeroY = heroMotion.Y;
            view.HeroReach = heroReach;
            view.Platform = Platform;
            // The places the pack actually has. For a wave that is every enemy in it; a continuous swarm's pack has ten
            // places in its arrays and only uses as many as the ladder has opened so far.
            view.Count = motion.Count;
            view.Seconds = seconds;
            for (int i = 0; i < motion.Count; i++)
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
                float distanceSquared = ArenaFloor.FloorDistanceSquared(motion.XOf(i), motion.YOf(i), motion.XOf(target), motion.YOf(target));
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

        private static void ChooseUpgrades(RunChoices choices, CardPicker picker)
        {
            while (choices.Current != null && choices.Current.Kind == ChoiceKind.Upgrade)
                choices.TrySelect(choices.Current, picker.Slot(choices.Current.Cards.Count));
        }

        // A run's card policy. With a pool no larger than the choice count the slot is the card, as it always was; once
        // offers are drawn, a policy has to name cards by id or it stops meaning anything.
        internal sealed class CardPicker
        {
            private readonly UpgradeService _upgrades;
            private readonly int _slot;
            private readonly string[] _preferIds;
            private readonly string[] _replay;
            private readonly bool _byCategory;
            private int _picks;

            // Every card this run took, in order, so a simulated run can be replayed exactly as a recorded one is.
            public readonly List<string> Taken = new List<string>();

            // How many picks the replay could not make because the card was not in the offer. Zero for a faithful replay,
            // which is the only kind worth measuring against: offers are drawn from the seed and the stacks, so a replay
            // that took every recorded card saw every recorded offer.
            public int ReplayMisses { get; private set; }

            // byCategory is category mode's policy, and false keeps every picker that existed before it exactly as it was.
            public CardPicker(UpgradeService upgrades, int slot, string[] preferIds, string[] replay = null,
                bool byCategory = false)
            {
                _upgrades = upgrades;
                _slot = slot;
                _preferIds = preferIds;
                _replay = replay;
                _byCategory = byCategory;
            }

            public int Slot(int cardCount)
            {
                UpgradeOffer offer = _upgrades.CurrentOffer;
                int pick = _picks++;
                // A recorded run's k-th card at its k-th offer. Past the end of the recording the run plays on by the
                // ordinary policy, which is how a replay that outlives its original is allowed to keep going.
                int slot = Choose(offer, cardCount, pick);
                if (offer != null && slot < offer.Choices.Count)
                    Taken.Add(offer.Choices[slot].Id);
                return slot;
            }

            private int Choose(UpgradeOffer offer, int cardCount, int pick)
            {
                if (_replay != null && offer != null && pick < _replay.Length)
                {
                    for (int i = 0; i < offer.Choices.Count && i < cardCount; i++)
                        if (offer.Choices[i].Id == _replay[pick])
                            return i;
                    ReplayMisses++;
                }
                if (_preferIds != null && offer != null)
                {
                    for (int p = 0; p < _preferIds.Length; p++)
                        for (int i = 0; i < offer.Choices.Count && i < cardCount; i++)
                            if (offer.Choices[i].Id == _preferIds[p])
                                return i;
                }
                if (_byCategory && offer != null)
                {
                    int byCategory = ByCategory(offer, cardCount);
                    if (byCategory >= 0)
                        return byCategory;
                }
                return Math.Min(_slot, cardCount - 1);
            }

            // A simple build, not a good one: level a carried weapon whenever one is offered, pick weapons up until two
            // are carried, tomes until two are held, then deepen the tomes, then take a general card. Anything else - a
            // third weapon or tome - only when nothing in that list is offered, by the slot.
            private int ByCategory(UpgradeOffer offer, int cardCount)
            {
                int found = Find(offer, cardCount, OfferCategory.WeaponUpgrade);
                if (found < 0 && _upgrades.Loadout.Count < 2)
                    found = Find(offer, cardCount, OfferCategory.NewWeapon);
                if (found < 0 && _upgrades.TomesHeld < 2)
                    found = Find(offer, cardCount, OfferCategory.NewTome);
                if (found < 0)
                    found = Find(offer, cardCount, OfferCategory.TomeUpgrade);
                if (found < 0)
                    found = Find(offer, cardCount, OfferCategory.General);
                return found;
            }

            private static int Find(UpgradeOffer offer, int cardCount, OfferCategory category)
            {
                for (int i = 0; i < offer.Choices.Count && i < cardCount; i++)
                    if (offer.Choices[i].Category == category)
                        return i;
                return -1;
            }
        }
    }
}
