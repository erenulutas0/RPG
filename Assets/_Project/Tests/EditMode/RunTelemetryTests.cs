using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Cryptforge.Analytics;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The run recorder against the real pure services, an in-memory store and a hand-stepped clock. The scene's own
    // reactions (ending the run on Extract, paying kills, opening the checkpoint) are simulated in the order CombatSetup
    // subscribes them, after the recorder, so the tests pin the cause-then-effect order the log must show.
    public sealed class RunTelemetryTests
    {
        private const string Floor1 = "floor_ember_halls";
        private const string Floor2 = "floor_sunken_vaults";

        private static UpgradeOption Damage() =>
            new UpgradeOption("upgrade_damage", "Tempered Edge", "+{0:0} damage per hit", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Flat, 5f), 5);

        private static UpgradeOption Speed() =>
            new UpgradeOption("upgrade_attack_speed", "Quickened Grip", "+{0:0}% attack speed", UpgradeStat.AttackSpeed,
                new StatModifier(ModifierOperation.Percent, 0.5f), 5);

        private static UpgradeOption Health() =>
            new UpgradeOption("upgrade_health", "Vital Surge", "+{0:0} maximum health", UpgradeStat.MaxHealth,
                new StatModifier(ModifierOperation.Flat, 20f), 5);

        // The keys every line of these events has always had, in order. Category mode appends its own after them, so a
        // legacy line and the front of a category line read the same.
        private static readonly string[] Envelope = { "v", "seq", "t", "st", "session", "run", "event" };
        private static readonly string[] OfferKeys =
            With(Envelope, "choice_ids", "run_level", "pending", "offer_index", "choice_rarities");
        private static readonly string[] SelectionKeys =
            With(Envelope, "upgrade_id", "choice_slot", "run_level", "stacks", "offer_index", "rarity");
        private static readonly string[] RunEndKeys = With(Envelope,
            "result", "cause", "floors_completed", "rooms_completed", "run_level", "kills", "horde_kills", "upgrades_applied",
            "pulses", "gold", "gold_banked", "gold_lost", "active_sec", "choice_sec", "pause_sec", "background_sec", "hero_hp",
            "hero_max_hp");

        private static string[] With(string[] keys, params string[] more)
        {
            var all = new string[keys.Length + more.Length];
            keys.CopyTo(all, 0);
            more.CopyTo(all, keys.Length);
            return all;
        }

        private static ForgeOption Mend() =>new ForgeOption("forge_mend", "Mend", "Restore {0:0}% health", ForgeEffect.Heal, 0.4f);

        private static ForgeOption Temper() =>
            new ForgeOption("forge_temper", "Temper", "Gain {0:0} extra upgrade choice", ForgeEffect.BonusUpgrade, 1f);

        [Test]
        public void AnExtractedRunIsLoggedInCauseThenEffectOrderAndEndsOnce()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session);
            Assert.That(telemetry.Session.LastSequence, Is.EqualTo(1), "Creating the recorder writes nothing.");

            scene.Recorder.Begin();
            scene.Recorder.Begin();
            scene.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            scene.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            scene.Kill("enemy_grunt", 10, 3);
            UpgradeOffer firstOffer = scene.Upgrades.CurrentOffer;
            Assert.That(scene.PickUpgrade(1), Is.True);
            Assert.That(scene.Upgrades.TrySelect(firstOffer, 0), Is.False, "A stale tap is not a second selection.");
            scene.Kill("enemy_brute", 0, 2);
            scene.GoldChest(10);

            // The encounter opens the forge before it reports the forge room.
            scene.Forge.Open(new[] { Mend(), Temper() });
            scene.Room(1, Floor1, 2, RoomKind.Forge, null);
            ForgeOffer visit = scene.Forge.CurrentOffer;
            Assert.That(scene.Forge.TrySelect(visit, 1), Is.True);
            Assert.That(scene.Forge.TrySelect(visit, 0), Is.False);
            Assert.That(scene.PickUpgrade(0), Is.True);

            scene.Room(1, Floor1, 3, RoomKind.Boss, "enemy_warden");
            scene.Room(1, Floor1, 3, RoomKind.Boss, "enemy_grunt");
            scene.Kill("enemy_warden", 5, 20);
            scene.ClearFloor(1, Floor1, false);
            CheckpointOffer checkpoint = scene.Checkpoint.CurrentOffer;
            Assert.That(scene.Choose(CheckpointKind.Extract), Is.True);

            // Nothing after the end reaches the log: a repeated end, stale taps, late notifications, disposal.
            scene.Run.End(RunOutcome.Defeat);
            Assert.That(scene.Checkpoint.TrySelect(checkpoint, 0), Is.False);
            scene.Room(2, Floor2, 1, RoomKind.Combat, "enemy_grunt");
            scene.Kill("enemy_grunt", 10, 5);
            scene.Recorder.RecordFloorCleared(2, Floor2);
            scene.Recorder.RecordAbilityUsed();
            scene.Recorder.Dispose();

            List<Line> lines = telemetry.Lines();
            AssertLogShape(lines);
            Assert.That(Events(lines), Is.EqualTo(new[]
            {
                "session_start", "run_start",
                "room_start", "first_kill", "upgrade_offered", "upgrade_selected", "chest_opened",
                "currency_earned", "currency_earned", "room_complete",
                "room_start", "upgrade_offered", "forge_selected", "upgrade_selected",
                "room_complete",
                "room_start", "boss_start", "currency_earned", "room_complete", "boss_end", "floor_complete",
                "extract_choice", "currency_earned", "run_end"
            }));

            Line start = Single(lines, "run_start");
            Assert.That(start.Keys, Is.EqualTo(new[]
            {
                "v", "seq", "t", "st", "session", "run", "event",
                "run_ordinal", "hero_id", "weapon_id", "relic_id", "ability_id", "forge_gold", "deepest_floor", "seed"
            }));
            Assert.That(start.Integer("seed"), Is.EqualTo(0L), "The context's seed, zero when a test names none.");
            Assert.That((start.Text("run"), start.Integer("run_ordinal"), start.Text("hero_id"), start.Text("weapon_id")),
                Is.EqualTo(("s1-1", 1L, "hero_vanguard", "weapon_sword")));
            Assert.That((start.IsNull("relic_id"), start.Text("ability_id"), start.Integer("forge_gold"), start.Integer("deepest_floor")),
                Is.EqualTo((true, "ability_forge_burst", 100L, 0L)));

            List<Line> rooms = All(lines, "room_start");
            Assert.That(rooms[0].Keys, Is.EqualTo(new[]
            {
                "v", "seq", "t", "st", "session", "run", "event", "floor_index", "floor_id", "room_index", "room_kind", "hero_hp",
                "hero_max_hp"
            }));
            Assert.That((rooms[1].Text("floor_id"), rooms[1].Integer("room_index"), rooms[1].Text("room_kind")),
                Is.EqualTo((Floor1, 2L, "forge")));
            Assert.That((rooms[2].Integer("room_index"), rooms[2].Text("room_kind")), Is.EqualTo((3L, "boss")));

            Line firstKill = Single(lines, "first_kill");
            Assert.That((firstKill.Text("enemy_id"), firstKill.Integer("floor_index"), firstKill.Integer("room_index")),
                Is.EqualTo(("enemy_grunt", 1L, 1L)));

            List<Line> offers = All(lines, "upgrade_offered");
            // A legacy run writes none of category mode's keys (docs/42, PR 8).
            Assert.That(offers[0].Keys, Is.EqualTo(OfferKeys));
            Assert.That(All(lines, "upgrade_selected")[0].Keys, Is.EqualTo(SelectionKeys));
            Assert.That((offers[0].Text("choice_ids"), offers[0].Integer("run_level"), offers[0].Integer("pending")),
                Is.EqualTo(("upgrade_damage,upgrade_attack_speed", 1L, 1L)));
            Assert.That((offers[0].Integer("offer_index"), offers[1].Integer("offer_index")), Is.EqualTo((0L, 1L)),
                "Offers are numbered from zero, which names the stream each was drawn from.");
            // Seed 0's first offer under the four-colour odds (RarityTable); it read Common,Common under 70/25/5.
            Assert.That(offers[0].Text("choice_rarities"), Is.EqualTo("Rare,Epic"),
                "A tier per card, beside the ids rather than inside them, so a reader can count tiers without parsing.");
            List<Line> selections = All(lines, "upgrade_selected");
            Assert.That((selections[0].Text("upgrade_id"), selections[0].Integer("choice_slot"), selections[0].Integer("run_level"),
                selections[0].Integer("stacks")), Is.EqualTo(("upgrade_attack_speed", 1L, 1L, 1L)));
            Assert.That((selections[1].Text("upgrade_id"), selections[1].Integer("choice_slot"), selections[1].Integer("stacks")),
                Is.EqualTo(("upgrade_damage", 0L, 1L)));
            Assert.That((selections[0].Integer("offer_index"), selections[1].Integer("offer_index")), Is.EqualTo((0L, 1L)),
                "A pick names the offer it came from.");
            Assert.That(selections[0].Text("rarity"), Is.EqualTo("Epic"), "And the tier it was taken at: slot 1's.");

            // Temper grants the bonus choice inside its effect, so that offer is logged before forge_selected.
            Line forge = Single(lines, "forge_selected");
            Assert.That((forge.Text("option_id"), forge.Integer("choice_slot"), forge.Integer("floor_index")),
                Is.EqualTo(("forge_temper", 1L, 1L)));

            Line bossStart = Single(lines, "boss_start");
            Line bossEnd = Single(lines, "boss_end");
            Assert.That((bossStart.Integer("floor_index"), bossStart.Integer("room_index"), bossStart.Text("enemy_id")),
                Is.EqualTo((1L, 3L, "enemy_warden")), "The first boss seen names the boss room.");
            Assert.That((bossEnd.Text("enemy_id"), bossEnd.Text("result")), Is.EqualTo(("enemy_warden", "defeated")));

            Line floor = Single(lines, "floor_complete");
            Assert.That((floor.Integer("floor_index"), floor.Text("floor_id"), floor.Integer("rooms")), Is.EqualTo((1L, Floor1, 3L)));

            Line extract = Single(lines, "extract_choice");
            Assert.That((extract.Text("choice"), extract.Integer("floor_index"), extract.Integer("gold"), extract.Integer("gold_unsecured")),
                Is.EqualTo(("extract", 1L, 35L, 35L)), "The choice is logged before the scene ends the run.");

            Line end = Single(lines, "run_end");
            Assert.That(end.Keys, Is.EqualTo(RunEndKeys));
            Assert.That((end.Text("result"), end.Text("cause"), end.Integer("floors_completed"), end.Integer("rooms_completed")),
                Is.EqualTo(("extracted", "extracted", 1L, 3L)));
            Assert.That((end.Integer("run_level"), end.Integer("kills"), end.Integer("upgrades_applied")), Is.EqualTo((1L, 3L, 2L)));
            Assert.That(end.Integer("horde_kills"), Is.EqualTo(0L), "A run without a horde reports none.");
            Assert.That((end.Integer("gold"), end.Integer("gold_banked"), end.Integer("gold_lost")), Is.EqualTo((35L, 35L, 0L)));
            Assert.That(telemetry.Session.RunId, Is.Null);
        }

        // The foundry's category mode (docs/42, PR 8): each offer names its cards' kinds, a weapon picked up is logged with
        // its slot, each pick names its kind and the level it left its weapon or tome at, and run_end reports the build.
        // Every key a legacy line writes keeps its place; category mode's own come after them.
        [Test]
        public void ACategoryRunLogsItsPicksAndBuildAfterEveryLegacyKey()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session, true);
            scene.Recorder.Begin();
            scene.Room(1, Floor1, 1, RoomKind.Swarm, "enemy_mite");

            // The first level-up comes from a kill, the rest are granted.
            scene.Kill("enemy_mite", 10, 0);
            UpgradeOffer first = scene.Upgrades.CurrentOffer;
            var firstKinds = new List<string>();
            for (int i = 0; i < first.Choices.Count; i++)
                firstKinds.Add(first.Choices[i].Category.ToString());
            Assert.That(scene.TryTake(card => card.Category == OfferCategory.NewWeapon), Is.True);
            scene.LevelUpTaking(card => card.Category == OfferCategory.NewTome && card.Id == "tome_force", "Force");
            scene.LevelUpTaking(card => card.Category == OfferCategory.TomeUpgrade, "Force's next level");
            scene.LevelUpTaking(card => card.Category == OfferCategory.WeaponUpgrade
                && card.Id.StartsWith("line_daggers", StringComparison.Ordinal), "a Daggers line");
            scene.LevelUpTaking(card => card.Category == OfferCategory.General, "Vital Surge");
            Assert.That(scene.Upgrades.CurrentOffer, Is.Null);
            scene.Die("enemy_mite");

            List<Line> lines = telemetry.Lines();
            AssertLogShape(lines);
            Assert.That(Events(lines), Is.EqualTo(new[]
            {
                "session_start", "run_start", "room_start", "first_kill",
                "upgrade_offered", "weapon_acquired", "upgrade_selected",
                "upgrade_offered", "upgrade_selected",
                "upgrade_offered", "upgrade_selected",
                "upgrade_offered", "upgrade_selected",
                "upgrade_offered", "upgrade_selected",
                "room_complete", "run_end"
            }), "A weapon picked up is logged as it joins the loadout, just before the pick that took it.");
            Assert.That(Single(lines, "run_start").Text("weapon_id"), Is.EqualTo("weapon_sword"), "The weapon it started with.");

            List<Line> offers = All(lines, "upgrade_offered");
            Assert.That(offers[0].Keys, Is.EqualTo(With(OfferKeys, "choice_categories")));
            Assert.That(offers[0].Text("choice_categories"), Is.EqualTo(string.Join(",", firstKinds)),
                "A kind per card, in the order of choice_ids.");
            firstKinds.Sort(StringComparer.Ordinal);
            Assert.That(firstKinds, Is.EqualTo(new[] { "General", "NewTome", "NewTome", "NewWeapon", "WeaponUpgrade" }),
                "The first offer shows every card a fresh run can take, so the line names every kind but a tome's level.");

            Line acquired = Single(lines, "weapon_acquired");
            Assert.That(acquired.Keys, Is.EqualTo(With(Envelope, "weapon_id", "slot", "run_level", "floor_index", "room_index")));
            Assert.That((acquired.Text("weapon_id"), acquired.Integer("slot"), acquired.Integer("run_level"),
                acquired.Integer("floor_index"), acquired.Integer("room_index")), Is.EqualTo(("weapon_daggers", 1L, 1L, 1L, 1L)));

            List<Line> picks = All(lines, "upgrade_selected");
            string[] weaponPick = With(SelectionKeys, "category", "weapon_id", "weapon_level");
            string[] tomePick = With(SelectionKeys, "category", "tome_level");
            Assert.That(picks[0].Keys, Is.EqualTo(weaponPick));
            Assert.That((picks[0].Text("upgrade_id"), picks[0].Text("category"), picks[0].Text("weapon_id"),
                picks[0].Integer("weapon_level")), Is.EqualTo(("weapon_daggers", "NewWeapon", "weapon_daggers", 1L)));
            Assert.That(picks[1].Keys, Is.EqualTo(tomePick));
            Assert.That((picks[1].Text("upgrade_id"), picks[1].Text("category"), picks[1].Integer("tome_level")),
                Is.EqualTo(("tome_force", "NewTome", 1L)));
            Assert.That(picks[2].Keys, Is.EqualTo(tomePick));
            Assert.That((picks[2].Text("upgrade_id"), picks[2].Text("category"), picks[2].Integer("tome_level")),
                Is.EqualTo(("tome_force", "TomeUpgrade", 2L)), "The level after the pick.");
            Assert.That(picks[3].Keys, Is.EqualTo(weaponPick));
            Assert.That((picks[3].Text("category"), picks[3].Text("weapon_id"), picks[3].Integer("weapon_level")),
                Is.EqualTo(("WeaponUpgrade", "weapon_daggers", 2L)), "A line names the weapon it levelled.");
            Assert.That(picks[4].Keys, Is.EqualTo(With(SelectionKeys, "category")),
                "A general card's level is its stacks, which every line already writes.");
            Assert.That((picks[4].Text("upgrade_id"), picks[4].Text("category"), picks[4].Integer("stacks")),
                Is.EqualTo(("upgrade_health", "General", 1L)));

            Line end = Single(lines, "run_end");
            Assert.That(end.Keys, Is.EqualTo(With(RunEndKeys, "weapon_ids", "weapon_levels", "tome_ids", "tome_levels",
                "evolutions")));
            Assert.That((end.Text("weapon_ids"), end.Text("weapon_levels")), Is.EqualTo(("weapon_sword,weapon_daggers", "1,2")),
                "Slot order.");
            Assert.That((end.Text("tome_ids"), end.Text("tome_levels")), Is.EqualTo(("tome_force", "2")),
                "Only the tomes held.");
            Assert.That((end.Integer("evolutions"), end.Integer("pulses"), end.Integer("upgrades_applied")),
                Is.EqualTo((0L, 0L, 5L)));
        }

        // An evolution raises no level, so its pick carries the weapon's level as it stood; run_end counts it, and its
        // pulses are every slot's.
        [Test]
        public void ACategoryRunEndCountsItsEvolutionAndEverySlotsPulses()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session, true);
            scene.Recorder.Begin();
            scene.Room(1, Floor1, 1, RoomKind.Swarm, "enemy_mite");

            Func<UpgradeOption, bool> daggersLine = card => card.Category == OfferCategory.WeaponUpgrade && !card.IsEvolution
                && card.Id.StartsWith("line_daggers", StringComparison.Ordinal);
            scene.LevelUpTaking(card => card.Category == OfferCategory.NewWeapon, "the Daggers");
            scene.LevelUpTaking(card => card.Category == OfferCategory.NewTome && card.Id == "tome_precision", "Precision");
            scene.LevelUpTaking(card => card.Category == OfferCategory.TomeUpgrade && card.Id == "tome_precision",
                "Precision's next level");
            for (int i = 0; i < 4; i++)
                scene.LevelUpTaking(daggersLine, "a Daggers line");
            Assert.That(scene.Loadout.At(1).Level, Is.EqualTo(5));

            // The evolution is one entry of the Daggers' table now; until it comes up, every other card is taken instead.
            string evolutionId = DescentSimulation.DaggersEvolution().Id;
            bool evolved = false;
            for (int offer = 0; offer < 15 && !evolved; offer++)
            {
                scene.Run.GrantBonusUpgrade();
                evolved = scene.TryTake(card => card.IsEvolution && card.EvolvesFromId == "weapon_daggers");
                if (!evolved)
                    Assert.That(scene.TryTake(card => !card.IsEvolution && !daggersLine(card)), Is.True);
            }
            Assert.That(evolved, Is.True, "The Daggers' evolution came up.");
            Assert.That(scene.Upgrades.CurrentOffer, Is.Null);

            // A pulse on every fourth strike of each weapon: one from each slot.
            for (int slot = 0; slot < scene.Loadout.Count; slot++)
            {
                WeaponRuntime weapon = scene.Loadout.At(slot).Runtime;
                weapon.AddModifier(UpgradeStat.PulseDamage, new StatModifier(ModifierOperation.Flat, 0.5f));
                var target = new HealthState(100000f);
                for (int strike = 0; strike < PulseRule.Every; strike++)
                {
                    weapon.Tick(10f);
                    Assert.That(weapon.TryAttack(target), Is.True);
                }
                Assert.That(weapon.PulsesFired, Is.EqualTo(1));
            }
            scene.Die("enemy_mite");

            List<Line> lines = telemetry.Lines();
            AssertLogShape(lines);
            List<Line> picks = All(lines, "upgrade_selected");
            Line evolution = picks[picks.Count - 1];
            Assert.That((evolution.Text("upgrade_id"), evolution.Text("category"), evolution.Text("weapon_id"),
                evolution.Integer("weapon_level")), Is.EqualTo((evolutionId, "WeaponUpgrade", "weapon_daggers", 5L)));

            Line end = Single(lines, "run_end");
            Assert.That((end.Integer("evolutions"), end.Integer("pulses")), Is.EqualTo((1L, 2L)));
            Assert.That((end.Text("weapon_ids"), end.Text("weapon_levels")),
                Is.EqualTo(("weapon_sword,weapon_daggers", scene.Loadout.At(0).Level + ",5")));
            Assert.That(end.Text("tome_ids").Split(','), Does.Contain("tome_precision"));
        }

        [Test]
        public void PulsesAreTheOneWeaponsWithoutALoadout()
        {
            var weapon = new WeaponRuntime(10f, 1f, 1f);
            weapon.AddModifier(UpgradeStat.PulseDamage, new StatModifier(ModifierOperation.Flat, 0.5f));
            var target = new HealthState(1000f);
            for (int strike = 0; strike < PulseRule.Every; strike++)
            {
                weapon.Tick(10f);
                Assert.That(weapon.TryAttack(target), Is.True);
            }

            Assert.That(RunTelemetry.PulsesFired(null, weapon), Is.EqualTo(1));
            Assert.That(RunTelemetry.PulsesFired(null, null), Is.Zero);
            Assert.That(RunTelemetry.PulsesFired(new WeaponLoadout(0), weapon), Is.Zero,
                "A loadout's pulses are its slots', and an empty one has none.");
        }

        [Test]
        public void DescendingAndClearingTheFinalFloorLogsFloorCompleteBeforeVictory()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session);

            scene.Recorder.Begin();
            scene.Room(1, Floor1, 1, RoomKind.Boss, "enemy_warden");
            scene.Kill("enemy_warden", 0, 10);
            scene.ClearFloor(1, Floor1, false);
            // The encounter still reports the last room after the floor cleared; a closed room does not start again.
            scene.Room(1, Floor1, 1, RoomKind.Boss, "enemy_warden");
            Assert.That(scene.Choose(CheckpointKind.Descend), Is.True);
            scene.Recorder.RecordFloorCleared(1, Floor1);
            // Descending reports the next floor before its first room exists.
            scene.Room(2, Floor2, 0, RoomKind.Combat, null);
            scene.Room(2, Floor2, 1, RoomKind.Boss, "enemy_lich");
            scene.Kill("enemy_lich", 0, 7);
            scene.ClearFloor(2, Floor2, true);

            List<Line> lines = telemetry.Lines();
            AssertLogShape(lines);
            Assert.That(Events(lines), Is.EqualTo(new[]
            {
                "session_start", "run_start",
                "room_start", "boss_start", "first_kill", "currency_earned", "room_complete", "boss_end", "floor_complete",
                "extract_choice",
                "room_start", "boss_start", "currency_earned", "room_complete", "boss_end", "floor_complete",
                "currency_earned", "run_end"
            }));

            Line descend = Single(lines, "extract_choice");
            Assert.That((descend.Text("choice"), descend.Integer("gold"), descend.Integer("gold_unsecured")),
                Is.EqualTo(("descend", 10L, 10L)));
            List<Line> rooms = All(lines, "room_start");
            Assert.That((rooms[1].Integer("floor_index"), rooms[1].Text("floor_id"), rooms[1].Integer("room_index")),
                Is.EqualTo((2L, Floor2, 1L)));
            List<Line> floors = All(lines, "floor_complete");
            Assert.That((floors[1].Integer("floor_index"), floors[1].Text("floor_id"), floors[1].Integer("rooms")),
                Is.EqualTo((2L, Floor2, 1L)));

            Line end = Single(lines, "run_end");
            Assert.That((end.Text("result"), end.Text("cause"), end.Integer("floors_completed"), end.Integer("rooms_completed")),
                Is.EqualTo(("victory", "cleared", 2L, 2L)));
            Assert.That((end.Integer("gold"), end.Integer("gold_banked")), Is.EqualTo((17L, 17L)));
        }

        [Test]
        public void ADefeatClosesTheOpenBossRoomAndNamesTheKiller()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session);

            scene.Recorder.Begin();
            scene.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            scene.Kill("enemy_grunt", 0, 4);
            scene.Room(1, Floor1, 2, RoomKind.Boss, "enemy_warden");
            scene.Die("enemy_warden");
            scene.Recorder.ApplicationPaused(true);

            List<Line> lines = telemetry.Lines();
            AssertLogShape(lines);
            Assert.That(Events(lines), Is.EqualTo(new[]
            {
                "session_start", "run_start",
                "room_start", "first_kill", "currency_earned", "room_complete",
                "room_start", "boss_start", "room_complete", "boss_end",
                "currency_earned", "run_end", "app_background"
            }));

            List<Line> completes = All(lines, "room_complete");
            Assert.That((completes[1].Integer("room_index"), completes[1].Text("result"), completes[1].Number("hero_hp"),
                completes[1].Integer("kills")), Is.EqualTo((2L, "defeat", 0.0, 0L)));
            Assert.That(Single(lines, "boss_end").Text("result"), Is.EqualTo("hero_died"));
            Line bank = All(lines, "currency_earned")[1];
            Assert.That(bank.Keys, Is.EqualTo(new[]
            {
                "v", "seq", "t", "st", "session", "run", "event", "currency", "source", "amount"
            }), "Banked gold belongs to the run, not a room.");
            Assert.That((bank.Text("currency"), bank.Text("source"), bank.Integer("amount")), Is.EqualTo(("forge_gold", "run_bank", 2L)));

            Line end = Single(lines, "run_end");
            Assert.That((end.Text("result"), end.Text("cause"), end.Integer("rooms_completed"), end.Integer("floors_completed")),
                Is.EqualTo(("defeat", "killed_by:enemy_warden", 1L, 0L)));
            Assert.That((end.Integer("gold"), end.Integer("gold_banked"), end.Integer("gold_lost")), Is.EqualTo((4L, 2L, 2L)));
            Line background = Single(lines, "app_background");
            Assert.That((background.Has("run"), background.Bool("run_active")), Is.EqualTo((false, false)),
                "Nothing carries the run id after run_end.");

            // Without a known attacker the cause is plain, and a run with no banked gold reports none.
            var unnamed = new Telemetry();
            var quiet = new Scene(unnamed.Session);
            quiet.Recorder.Begin();
            quiet.Room(1, Floor1, 1, RoomKind.Elite, null);
            quiet.Die(null);
            List<Line> quietLines = unnamed.Lines();
            AssertLogShape(quietLines);
            Assert.That(Events(quietLines), Is.EqualTo(new[] { "session_start", "run_start", "room_start", "room_complete", "run_end" }));
            Assert.That(Single(quietLines, "room_complete").Text("room_kind"), Is.EqualTo("elite"));
            Assert.That(Single(quietLines, "run_end").Text("cause"), Is.EqualTo("killed"));
        }

        [Test]
        public void WavesAreLoggedInsideTheirRoomWithTheHeroHealthBesideItsMaximum()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session);
            RunTelemetry recorder = scene.Recorder;

            recorder.Begin();
            recorder.RecordWaveClear(1, 2, 5);
            scene.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            recorder.RecordWaveStart(1, 2, 3);
            recorder.Tick(1.5f, 1.5f);
            scene.Hero.Current = 70f;
            recorder.RecordWaveClear(1, 2, 9);
            recorder.RecordWaveClear(1, 2, 9);
            // Between waves the time counts for the room, not for either wave.
            recorder.Tick(0.5f, 0.5f);
            recorder.RecordWaveStart(2, 2, 4);
            recorder.Tick(2f, 2f);
            recorder.RecordWaveClear(2, 2, 12);
            // A health card raises the maximum before the boss.
            scene.Hero.Maximum = 120f;
            scene.Room(1, Floor1, 2, RoomKind.Boss, "enemy_warden");
            recorder.RecordWaveStart(1, 1, 1);
            scene.Hero.Current = 30f;
            scene.Kill("enemy_warden", 0, 5);
            recorder.RecordWaveClear(1, 1, 20);
            scene.ClearFloor(1, Floor1, true);

            List<Line> lines = telemetry.Lines();
            AssertLogShape(lines);
            Assert.That(Events(lines), Is.EqualTo(new[]
            {
                "session_start", "run_start",
                "room_start", "wave_start", "wave_clear", "wave_start", "wave_clear", "room_complete",
                "room_start", "boss_start", "wave_start", "first_kill", "wave_clear", "currency_earned", "room_complete", "boss_end",
                "floor_complete", "currency_earned", "run_end"
            }), "A clear without a spawn the recorder saw, and a repeated clear, write nothing.");

            List<Line> starts = All(lines, "wave_start");
            Assert.That(starts[0].Keys, Is.EqualTo(new[]
            {
                "v", "seq", "t", "st", "session", "run", "event",
                "floor_index", "room_index", "wave_index", "wave_count", "enemy_count", "hero_hp", "hero_max_hp"
            }));
            Assert.That((starts[0].Integer("wave_index"), starts[0].Integer("wave_count"), starts[0].Integer("enemy_count"),
                starts[0].Number("hero_hp"), starts[0].Number("hero_max_hp")), Is.EqualTo((1L, 2L, 3L, 100.0, 100.0)));
            Assert.That((starts[1].Integer("wave_index"), starts[1].Integer("enemy_count")), Is.EqualTo((2L, 4L)));
            Assert.That((starts[2].Integer("room_index"), starts[2].Integer("wave_index"), starts[2].Integer("wave_count")),
                Is.EqualTo((2L, 1L, 1L)));

            List<Line> clears = All(lines, "wave_clear");
            Assert.That(clears[0].Keys, Is.EqualTo(new[]
            {
                "v", "seq", "t", "st", "session", "run", "event",
                "floor_index", "room_index", "wave_index", "wave_count", "active_sec", "enemy_hits", "hero_hp", "hero_max_hp"
            }));
            Assert.That((clears[0].Integer("wave_index"), clears[0].Number("active_sec"), clears[0].Integer("enemy_hits"),
                clears[0].Number("hero_hp"), clears[0].Number("hero_max_hp")), Is.EqualTo((1L, 1.5, 9L, 70.0, 100.0)));
            Assert.That((clears[1].Integer("wave_index"), clears[1].Number("active_sec"), clears[1].Integer("enemy_hits")),
                Is.EqualTo((2L, 2.0, 12L)), "A wave's time starts at its spawn.");
            Assert.That(All(lines, "room_complete")[0].Number("active_sec"), Is.EqualTo(4.0));

            // Health beside its maximum wherever health is reported, so the boss is read as a share of a raised maximum.
            Line bossRoom = All(lines, "room_start")[1];
            Line bossStart = Single(lines, "boss_start");
            Line bossEnd = Single(lines, "boss_end");
            Line end = Single(lines, "run_end");
            Assert.That((bossRoom.Number("hero_hp"), bossRoom.Number("hero_max_hp")), Is.EqualTo((70.0, 120.0)));
            Assert.That((bossStart.Number("hero_hp"), bossStart.Number("hero_max_hp")), Is.EqualTo((70.0, 120.0)));
            Assert.That((clears[2].Number("hero_hp"), clears[2].Number("hero_max_hp")), Is.EqualTo((30.0, 120.0)));
            Assert.That((bossEnd.Number("hero_hp"), bossEnd.Number("hero_max_hp")), Is.EqualTo((30.0, 120.0)));
            Assert.That((end.Number("hero_hp"), end.Number("hero_max_hp")), Is.EqualTo((30.0, 120.0)));
            Assert.That(All(lines, "room_complete")[1].Number("hero_max_hp"), Is.EqualTo(120.0));
        }

        [Test]
        public void HordeSamplesAreLoggedInsideTheirRoomOnTheActiveClock()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session);
            RunTelemetry recorder = scene.Recorder;

            recorder.ObserveHorde(HordeState.Ramp, 1, 4, 0);
            recorder.Begin();
            scene.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            // A spawn heard before the first observation counts in the first window; that observation starts the cadence.
            recorder.RecordHordeSpawn();
            recorder.ObserveHorde(HordeState.Ramp, 1, 4, 0);
            recorder.RecordWaveStart(1, 2, 3);
            recorder.Tick(2.5f, 2.5f);
            recorder.RecordHordeSpawn();
            recorder.RecordKill("enemy_mite", 0);
            recorder.RecordHordeKill();
            recorder.ObserveHorde(HordeState.Ramp, 1, 4, 3);
            recorder.Tick(2.5f, 2.5f);
            scene.Hero.Current = 80f;
            recorder.ObserveHorde(HordeState.Ramp, 1, 5, 3);
            recorder.ObserveHorde(HordeState.Ramp, 1, 5, 3);
            // The player's pause and an open choice stop the clock, so a frozen run writes no sample however long it lasts.
            recorder.Tick(5f, 5f);
            Assert.That(scene.Pause.TryPause(), Is.True);
            recorder.ObserveHorde(HordeState.Ramp, 2, 5, 3);
            recorder.Tick(0f, 20f);
            recorder.ObserveHorde(HordeState.Ramp, 2, 5, 3);
            Assert.That(scene.Pause.TryResume(), Is.True);
            recorder.ObserveHorde(HordeState.Ramp, 2, 5, 3);
            recorder.RecordWaveClear(1, 2, 9);
            scene.Kill("enemy_grunt", 10, 0);
            Assert.That(scene.Pause.IsChoiceOpen, Is.True);
            recorder.Tick(5f, 5f);
            recorder.ObserveHorde(HordeState.Ramp, 2, 5, 0);
            Assert.That(scene.PickUpgrade(0), Is.True);
            // A long frame writes one sample, and the next is due a whole interval later.
            recorder.Tick(12f, 12f);
            recorder.RecordHordeSpawn();
            recorder.RecordHordeSpawn();
            recorder.RecordHordeSpawn();
            recorder.ObserveHorde(HordeState.Horde, 5, 10, 0);
            recorder.ObserveHorde(HordeState.Horde, 5, 10, 0);
            recorder.Tick(4.5f, 4.5f);
            recorder.ObserveHorde(HordeState.Horde, 5, 10, 0);
            // A released horde takes its unsampled window with it; the next horde starts its own cadence.
            recorder.RecordHordeSpawn();
            recorder.RecordHordeReleased();
            scene.Room(1, Floor1, 2, RoomKind.Combat, "enemy_grunt");
            recorder.ObserveHorde(HordeState.Horde, 0, 10, 0);
            recorder.Tick(0.5f, 0.5f);
            recorder.ObserveHorde(HordeState.Horde, 0, 10, 0);
            recorder.RecordHordeSpawn();
            recorder.RecordKill("enemy_mite", 0);
            recorder.RecordHordeKill();
            recorder.Tick(4.5f, 4.5f);
            recorder.ObserveHorde(HordeState.Pressure, 0, 16, 0);
            recorder.Tick(5f, 5f);
            recorder.ObserveHorde(HordeState.Boss, 1, 6, 1);
            scene.Die("enemy_grunt");
            recorder.Tick(10f, 10f);
            recorder.RecordHordeKill();
            recorder.ObserveHorde(HordeState.Boss, 1, 6, 1);

            List<Line> lines = telemetry.Lines();
            AssertLogShape(lines);
            Assert.That(Events(lines), Is.EqualTo(new[]
            {
                "session_start", "run_start",
                "room_start", "wave_start", "first_kill", "horde_sample", "horde_sample", "wave_clear", "upgrade_offered",
                "upgrade_selected", "horde_sample", "room_complete",
                "room_start", "horde_sample", "horde_sample", "room_complete", "run_end"
            }), "Nothing is sampled before the run begins, while it is frozen or after it ends.");

            List<Line> samples = All(lines, "horde_sample");
            Assert.That(samples[0].Keys, Is.EqualTo(new[]
            {
                "v", "seq", "t", "st", "session", "run", "event",
                "floor_index", "room_index", "state", "horde_alive", "horde_target", "wave_alive", "horde_spawned_window",
                "horde_kills_window", "hero_hp", "hero_max_hp"
            }));
            Assert.That((samples[0].Integer("floor_index"), samples[0].Integer("room_index"), samples[0].Text("state")),
                Is.EqualTo((1L, 1L, "ramp")));
            Assert.That((samples[0].Integer("horde_alive"), samples[0].Integer("horde_target"), samples[0].Integer("wave_alive")),
                Is.EqualTo((1L, 5L, 3L)));
            Assert.That((samples[0].Integer("horde_spawned_window"), samples[0].Integer("horde_kills_window"),
                samples[0].Number("hero_hp"), samples[0].Number("hero_max_hp")), Is.EqualTo((2L, 1L, 80.0, 100.0)));
            Assert.That((samples[1].Integer("horde_alive"), samples[1].Integer("horde_spawned_window"),
                samples[1].Integer("horde_kills_window")), Is.EqualTo((2L, 0L, 0L)), "Each window starts at the last sample.");
            Assert.That((samples[2].Text("state"), samples[2].Integer("horde_target"), samples[2].Integer("horde_spawned_window")),
                Is.EqualTo(("horde", 10L, 3L)));
            Assert.That((samples[3].Integer("room_index"), samples[3].Text("state"), samples[3].Integer("horde_spawned_window"),
                samples[3].Integer("horde_kills_window")), Is.EqualTo((2L, "pressure", 1L, 1L)));
            Assert.That((samples[4].Text("state"), samples[4].Integer("wave_alive")), Is.EqualTo(("boss", 1L)));

            Line end = Single(lines, "run_end");
            Assert.That((end.Integer("kills"), end.Integer("horde_kills")), Is.EqualTo((3L, 2L)),
                "A horde death after the run ended counts for neither.");
        }

        // Burn (docs/45): a run where something burned ends with its burn kills and spreads, after every other key.
        [Test]
        public void ARunThatBurnedEndsWithItsBurnCounts()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session);
            RunTelemetry recorder = scene.Recorder;
            recorder.Begin();
            scene.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            recorder.Tick(1f, 1f);
            recorder.RecordBurnSpread();
            recorder.RecordBurnSpread();
            recorder.RecordBurnKill();
            scene.Die("enemy_grunt");
            recorder.RecordBurnKill();

            Line end = Single(telemetry.Lines(), "run_end");
            Assert.That(end.Keys, Is.EqualTo(With(RunEndKeys, "burn_kills", "burn_spreads")), "Last, after every other key.");
            Assert.That((end.Integer("burn_kills"), end.Integer("burn_spreads")), Is.EqualTo((1L, 2L)),
                "A burn after the run ended counts for nothing.");
        }

        [Test]
        public void ActiveChoicePauseAndBackgroundTimeAreKeptApart()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session);
            RunTelemetry recorder = scene.Recorder;

            // Steps are exact binary fractions so every total compares exactly.
            recorder.Tick(8f, 8f);
            recorder.Begin();
            scene.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            recorder.Tick(1.5f, 1.5f);
            recorder.RecordAbilityUsed();
            Assert.That(scene.Pause.TryPause(), Is.True);
            recorder.Tick(0f, 0.25f);
            Assert.That(scene.Pause.TryResume(), Is.True);
            scene.Kill("enemy_grunt", 10, 0);
            Assert.That(scene.Pause.IsChoiceOpen, Is.True);
            recorder.Tick(0f, 2f);
            recorder.Tick(float.NaN, float.NaN);
            recorder.Tick(-1f, -1f);
            Assert.That(scene.PickUpgrade(0), Is.True);
            recorder.Tick(float.PositiveInfinity, float.PositiveInfinity);
            recorder.Tick(float.NegativeInfinity, float.NegativeInfinity);
            recorder.Tick(0.25f, 0.25f);

            telemetry.Clock.Advance(100);
            recorder.ApplicationPaused(true);
            recorder.ApplicationPaused(true);
            // An editor may keep ticking while the app is away; that time is background time, not play time.
            recorder.Tick(4f, 4f);
            telemetry.Clock.Advance(30);
            recorder.ApplicationPaused(false);
            recorder.ApplicationPaused(false);
            // The resume frame's delta covers the same thirty seconds.
            recorder.Tick(30f, 30f);
            recorder.Tick(0.5f, 0.5f);

            scene.Forge.Open(new[] { Mend(), Temper() });
            scene.Room(1, Floor1, 2, RoomKind.Forge, null);
            recorder.Tick(0f, 1f);
            Assert.That(scene.Forge.TrySelect(scene.Forge.CurrentOffer, 0), Is.True);
            recorder.Tick(0.5f, 0.5f);
            scene.ClearFloor(1, Floor1, false);
            recorder.Tick(0f, 0.75f);
            Assert.That(scene.Choose(CheckpointKind.Extract), Is.True);
            recorder.Tick(16f, 16f);

            List<Line> lines = telemetry.Lines();
            AssertLogShape(lines);
            Assert.That(Single(lines, "first_kill").Number("active_sec"), Is.EqualTo(1.5));
            Assert.That(Single(lines, "ability_used").Number("room_active_sec"), Is.EqualTo(1.5));
            Line background = Single(lines, "app_background");
            Assert.That((background.Bool("run_active"), background.Text("run")), Is.EqualTo((true, "s1-1")));
            Assert.That(Single(lines, "app_foreground").Number("background_sec"), Is.EqualTo(30.0));
            Assert.That(All(lines, "app_background").Count + All(lines, "app_foreground").Count, Is.EqualTo(2),
                "A repeated pause or resume changes nothing.");

            List<Line> rooms = All(lines, "room_complete");
            AssertTimes(rooms[0], 2.25, 2.0, 0.25);
            AssertTimes(rooms[1], 0.5, 1.0, 0.0);
            AssertTimes(Single(lines, "floor_complete"), 2.75, 3.0, 0.25);
            Line end = Single(lines, "run_end");
            AssertTimes(end, 2.75, 3.75, 0.25);
            Assert.That(end.Number("background_sec"), Is.EqualTo(30.0));
        }

        [Test]
        public void DisposeUnsubscribesAndInterruptsOnlyARunThatHasNotEnded()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session);
            scene.Recorder.Begin();
            scene.Room(1, Floor1, 2, RoomKind.Combat, "enemy_grunt");
            scene.Recorder.Tick(1f, 1f);
            scene.Pause.TryPause();
            scene.Recorder.Tick(0f, 0.5f);
            scene.Recorder.ApplicationPaused(true);
            telemetry.Clock.Advance(8);
            scene.Recorder.ApplicationPaused(false);
            Assert.That(telemetry.Session.Log.Buffered, Is.GreaterThan(0));

            scene.Recorder.Dispose();
            Assert.That(telemetry.Session.Log.Buffered, Is.Zero, "Disposal writes what was buffered.");
            Assert.That(telemetry.Session.RunId, Is.Null);
            long sequence = telemetry.Session.LastSequence;
            int written = telemetry.Store.Lines().Count;

            // Every source the recorder listened to fires again, and every scene call arrives late.
            scene.Pause.TryResume();
            scene.Kill("enemy_grunt", 10, 5);
            Assert.That(scene.PickUpgrade(0), Is.True);
            scene.Forge.Open(new[] { Temper() });
            Assert.That(scene.Forge.TrySelect(scene.Forge.CurrentOffer, 0), Is.True);
            Assert.That(scene.PickUpgrade(1), Is.True);
            scene.Room(1, Floor1, 3, RoomKind.Boss, "enemy_warden");
            scene.GoldChest(10);
            scene.Recorder.RecordAbilityUsed();
            scene.Recorder.Tick(1f, 1f);
            scene.Recorder.ApplicationPaused(true);
            scene.Recorder.ApplicationPaused(false);
            scene.ClearFloor(1, Floor1, false);
            Assert.That(scene.Choose(CheckpointKind.Extract), Is.True);
            Assert.That(scene.Relics.TrySelect(scene.Relics.Relics[0]), Is.True);
            scene.Recorder.Begin();
            scene.Recorder.Dispose();

            Assert.That(telemetry.Session.LastSequence, Is.EqualTo(sequence), "Nothing is emitted after disposal.");
            Assert.That(telemetry.Store.Lines().Count, Is.EqualTo(written));
            Assert.That((scene.Run.Outcome, scene.Profile.OwnedRelicIds.Count), Is.EqualTo((RunOutcome.Extracted, 1)),
                "The game's own reactions still run.");

            List<Line> lines = telemetry.Lines();
            AssertLogShape(lines);
            Assert.That(Events(lines), Is.EqualTo(new[]
            {
                "session_start", "run_start", "room_start", "app_background", "app_foreground", "run_interrupted"
            }), "An interrupted run has no room_complete or run_end.");
            Line interrupted = lines[lines.Count - 1];
            Assert.That(interrupted.Keys, Is.EqualTo(new[]
            {
                "v", "seq", "t", "st", "session", "run", "event",
                "reason", "floor_index", "room_index", "active_sec", "choice_sec", "pause_sec", "background_sec"
            }));
            Assert.That((interrupted.Text("run"), interrupted.Text("reason"), interrupted.Integer("floor_index"),
                interrupted.Integer("room_index")), Is.EqualTo(("s1-1", "scene_unloaded", 1L, 2L)));
            AssertTimes(interrupted, 1.0, 0.0, 0.5);
            Assert.That(interrupted.Number("background_sec"), Is.EqualTo(8.0));

            // A recorder that never began, or whose run ended, adds nothing when disposed.
            var idle = new Telemetry();
            new Scene(idle.Session).Recorder.Dispose();
            var ended = new Scene(idle.Session);
            ended.Recorder.Begin();
            ended.Room(1, Floor1, 1, RoomKind.Combat, null);
            ended.Die(null);
            ended.Recorder.Dispose();
            List<Line> idleLines = idle.Lines();
            AssertLogShape(idleLines);
            Assert.That(Events(idleLines), Is.EqualTo(new[] { "session_start", "run_start", "room_start", "room_complete", "run_end" }));
        }

        [Test]
        public void ARestartedSceneLogsANewRunInTheSameSessionWithoutDuplicates()
        {
            var telemetry = new Telemetry();
            var first = new Scene(telemetry.Session);
            first.Recorder.Begin();
            first.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            first.Kill("enemy_grunt", 10, 2);
            Assert.That(first.PickUpgrade(0), Is.True);
            first.Die("enemy_grunt");
            first.Recorder.Dispose();

            // Try again rebuilds every service; a quit mid-run then interrupts the second run.
            var second = new Scene(telemetry.Session);
            second.Recorder.Begin();
            second.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            second.Kill("enemy_grunt", 10, 2);
            Assert.That(second.PickUpgrade(1), Is.True);
            second.Recorder.Dispose();

            var third = new Scene(telemetry.Session);
            third.Recorder.Begin();
            // The disposed scenes' services keep raising events nobody should hear.
            first.Kill("enemy_grunt", 30, 2);
            second.Kill("enemy_grunt", 30, 2);
            Assert.That(second.PickUpgrade(0), Is.True);
            third.Room(1, Floor1, 1, RoomKind.Combat, null);

            List<Line> lines = telemetry.Lines();
            AssertLogShape(lines);
            Assert.That(Events(lines), Is.EqualTo(new[]
            {
                "session_start",
                "run_start", "room_start", "first_kill", "upgrade_offered", "upgrade_selected", "currency_earned", "room_complete",
                "currency_earned", "run_end",
                "run_start", "room_start", "first_kill", "upgrade_offered", "upgrade_selected", "run_interrupted",
                "run_start", "room_start"
            }));

            List<Line> starts = All(lines, "run_start");
            Assert.That((starts[0].Text("run"), starts[1].Text("run"), starts[2].Text("run")), Is.EqualTo(("s1-1", "s1-2", "s1-3")));
            Assert.That((starts[0].Integer("run_ordinal"), starts[1].Integer("run_ordinal"), starts[2].Integer("run_ordinal")),
                Is.EqualTo((1L, 2L, 3L)));
            List<Line> selections = All(lines, "upgrade_selected");
            Assert.That((selections[0].Text("run"), selections[0].Text("upgrade_id")), Is.EqualTo(("s1-1", "upgrade_damage")));
            Assert.That((selections[1].Text("run"), selections[1].Text("upgrade_id")), Is.EqualTo(("s1-2", "upgrade_attack_speed")));
        }

        [Test]
        public void ASceneDisposedAfterTheNextRunBeganInterruptsOnlyItsOwnRun()
        {
            // A scene replaced mid-run may be destroyed after the new scene already woke and began its run.
            var telemetry = new Telemetry();
            var old = new Scene(telemetry.Session);
            old.Recorder.Begin();
            old.Room(1, Floor1, 2, RoomKind.Combat, "enemy_grunt");
            var next = new Scene(telemetry.Session);
            next.Recorder.Begin();
            old.Recorder.Dispose();
            Assert.That(telemetry.Session.RunId, Is.EqualTo("s1-2"), "The late disposal leaves the newer run open.");

            next.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            next.Die(null);
            next.Recorder.Dispose();

            List<Line> lines = telemetry.Lines();
            Assert.That(Events(lines), Is.EqualTo(new[]
            {
                "session_start", "run_start", "room_start", "run_start", "run_interrupted", "room_start", "room_complete", "run_end"
            }));
            var runs = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                Assert.That(lines[i].Integer("seq"), Is.EqualTo(i + 1));
                runs.Add(lines[i].Has("run") ? lines[i].Text("run") : null);
            }
            Assert.That(runs, Is.EqualTo(new[] { null, "s1-1", "s1-1", "s1-2", "s1-1", "s1-2", "s1-2", "s1-2" }),
                "run_interrupted closes the disposed scene's run, never the run that began after it.");
            Line interrupted = Single(lines, "run_interrupted");
            Assert.That((interrupted.Integer("floor_index"), interrupted.Integer("room_index")), Is.EqualTo((1L, 2L)));
            Assert.That(Single(lines, "run_end").Text("result"), Is.EqualTo("defeat"));
            Assert.That(telemetry.Session.RunId, Is.Null);
        }

        [Test]
        public void CurrencyIsReportedPerRoomAndSourceAndBankedGoldAtRunEnd()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session);
            scene.Recorder.Begin();

            scene.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            scene.Kill("enemy_grunt", 0, 4);
            scene.Kill("enemy_grunt", 0, 0);
            scene.Kill("enemy_grunt", 0, 3);
            scene.Hero.Current = 50f;
            scene.HealChest("upgrade_damage", UpgradeRarity.Epic);

            scene.Room(1, Floor1, 2, RoomKind.Combat, "enemy_brute");
            scene.GoldChest(12);
            scene.Kill("enemy_brute", 0, 6);

            scene.Forge.Open(new[] { Mend() });
            scene.Room(1, Floor1, 3, RoomKind.Forge, null);
            Assert.That(scene.Forge.TrySelect(scene.Forge.CurrentOffer, 0), Is.True);
            scene.ClearFloor(1, Floor1, false);
            Assert.That(scene.Choose(CheckpointKind.Descend), Is.True);

            scene.Room(2, Floor2, 1, RoomKind.Combat, "enemy_grunt");
            scene.GoldChest(5);
            scene.Die(null);

            List<Line> lines = telemetry.Lines();
            AssertLogShape(lines);
            Assert.That(Events(lines), Is.EqualTo(new[]
            {
                "session_start", "run_start",
                "room_start", "first_kill", "chest_opened", "currency_earned", "room_complete",
                "room_start", "chest_opened", "currency_earned", "currency_earned", "room_complete",
                "room_start", "forge_selected", "room_complete", "floor_complete", "extract_choice",
                "room_start", "chest_opened", "currency_earned", "room_complete", "currency_earned", "run_end"
            }));

            List<Line> chests = All(lines, "chest_opened");
            Assert.That(chests[0].Keys, Is.EqualTo(new[]
            {
                "v", "seq", "t", "st", "session", "run", "event",
                "reward", "gold", "heal_fraction", "book", "book_rarity", "hero_hp", "hero_max_hp", "floor_index",
                "room_index"
            }));
            Assert.That((chests[0].Text("book"), chests[0].Text("book_rarity")), Is.EqualTo(("upgrade_damage", "Epic")),
                "A chest that held a book names it and the tier it came at.");
            Assert.That((chests[1].Text("book"), chests[1].Text("book_rarity")), Is.EqualTo(("none", "none")),
                "A chest that held none says so rather than leaving the fields out.");
            Assert.That((chests[0].Text("reward"), chests[0].Integer("gold"), chests[0].Number("heal_fraction"), chests[0].Number("hero_hp")),
                Is.EqualTo(("heal", 0L, 0.25, 75.0)), "A heal chest reports the health after its heal and no gold.");
            Assert.That((chests[1].Text("reward"), chests[1].Integer("gold"), chests[1].Integer("room_index")),
                Is.EqualTo(("gold", 12L, 2L)), "Chest gold is what the run actually gained, apart from kill gold.");

            List<Line> earned = All(lines, "currency_earned");
            AssertEarned(earned[0], "run_gold", "kills", 7, 1, 1);
            AssertEarned(earned[1], "run_gold", "kills", 6, 1, 2);
            AssertEarned(earned[2], "run_gold", "chest", 12, 1, 2);
            AssertEarned(earned[3], "run_gold", "chest", 5, 2, 1);
            Assert.That((earned[4].Text("currency"), earned[4].Text("source"), earned[4].Has("room_index")),
                Is.EqualTo(("forge_gold", "run_bank", false)));

            List<Line> rooms = All(lines, "room_complete");
            Assert.That((rooms[0].Integer("kills"), rooms[0].Integer("gold_kills"), rooms[0].Integer("gold_chests")), Is.EqualTo((3L, 7L, 0L)));
            Assert.That((rooms[1].Integer("kills"), rooms[1].Integer("gold_kills"), rooms[1].Integer("gold_chests")), Is.EqualTo((1L, 6L, 12L)));
            Assert.That((rooms[2].Integer("kills"), rooms[2].Integer("gold_kills"), rooms[2].Integer("gold_chests")), Is.EqualTo((0L, 0L, 0L)));
            Assert.That((rooms[3].Text("result"), rooms[3].Integer("gold_chests")), Is.EqualTo(("defeat", 5L)));

            // 25 was secured at the checkpoint; the defeat loses half of the 5 unsecured, rounded down.
            Line end = Single(lines, "run_end");
            Assert.That((end.Integer("gold"), end.Integer("gold_banked"), end.Integer("gold_lost")), Is.EqualTo((30L, 28L, 2L)));
            Assert.That(earned[4].Integer("amount"), Is.EqualTo(28));
            Assert.That(scene.Profile.Gold - 100, Is.EqualTo(28), "The banked amount is what reached the profile.");
        }

        [Test]
        public void ForgePurchasesAfterTheRunAreLoggedWithoutARunId()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session);
            scene.Recorder.Begin();
            scene.Room(1, Floor1, 1, RoomKind.Boss, "enemy_warden");
            scene.Kill("enemy_warden", 0, 20);
            scene.ClearFloor(1, Floor1, false);
            Assert.That(scene.Choose(CheckpointKind.Extract), Is.True);
            Assert.That(scene.Profile.Gold, Is.EqualTo(120));

            RelicOption secondWind = scene.Relics.Relics[0];
            RelicOption counterweight = scene.Relics.Relics[1];
            WeaponOption sword = scene.Weapons.Weapons[0];
            WeaponOption axe = scene.Weapons.Weapons[1];
            Assert.That(scene.Relics.TrySelect(counterweight), Is.False, "Unaffordable.");
            Assert.That(scene.Relics.TrySelect(secondWind), Is.True);
            Assert.That(scene.Relics.TrySelect(secondWind), Is.False, "Already equipped.");
            Assert.That(scene.Weapons.TrySelect(axe), Is.True);
            Assert.That(scene.Weapons.TrySelect(sword), Is.True, "Back to the starting weapon, which costs nothing.");
            Assert.That(scene.Weapons.TrySelect(axe), Is.True, "Equipping an owned weapon spends nothing.");

            List<Line> lines = telemetry.Lines();
            AssertLogShape(lines);
            List<Line> spent = All(lines, "currency_spent");
            Assert.That(spent.Count, Is.EqualTo(2));
            Assert.That(spent[0].Keys, Is.EqualTo(new[]
            {
                "v", "seq", "t", "st", "session", "event", "currency", "sink", "item_id", "amount"
            }), "A purchase after the run carries no run id.");
            Assert.That((spent[0].Text("currency"), spent[0].Text("sink"), spent[0].Text("item_id"), spent[0].Integer("amount")),
                Is.EqualTo(("forge_gold", "relic", "relic_second_wind", 60L)));
            Assert.That((spent[1].Has("run"), spent[1].Text("sink"), spent[1].Text("item_id"), spent[1].Integer("amount")),
                Is.EqualTo((false, "weapon", "weapon_axe", 40L)));
            Assert.That(lines.IndexOf(spent[0]), Is.GreaterThan(lines.IndexOf(Single(lines, "run_end"))));
            Assert.That(scene.Profile.Gold, Is.EqualTo(20));
        }

        [Test]
        public void ChoicesBackgroundAndRunEndWriteTheBufferAndOtherEventsWait()
        {
            var telemetry = new Telemetry();
            var scene = new Scene(telemetry.Session);
            scene.Recorder.Begin();
            scene.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            scene.Recorder.Tick(0.5f, 0.5f);
            Assert.That(telemetry.Store.Appends, Is.Empty, "Ordinary events are buffered, not written one by one.");

            scene.Kill("enemy_grunt", 10, 1);
            Assert.That(telemetry.Store.Appends.Count, Is.EqualTo(1), "An open choice writes the buffer.");
            Assert.That(Last(telemetry.Store.Lines()).Event, Is.EqualTo("upgrade_offered"), "The offer that opened the choice is included.");

            Assert.That(scene.PickUpgrade(0), Is.True);
            scene.Kill("enemy_grunt", 0, 1);
            scene.Recorder.Tick(0.5f, 0.5f);
            Assert.That(telemetry.Store.Appends.Count, Is.EqualTo(1));
            telemetry.Clock.Advance(10);
            scene.Recorder.Tick(0.5f, 0.5f);
            Assert.That(telemetry.Store.Appends.Count, Is.EqualTo(2), "Ticks write once the log's interval passed.");
            Assert.That(telemetry.Session.Log.Buffered, Is.Zero);

            scene.Recorder.ApplicationPaused(true);
            Assert.That(telemetry.Store.Appends.Count, Is.EqualTo(3));
            Assert.That(Last(telemetry.Store.Lines()).Event, Is.EqualTo("app_background"));
            scene.Recorder.ApplicationPaused(false);
            Assert.That(telemetry.Store.Appends.Count, Is.EqualTo(3));

            scene.Die("enemy_grunt");
            Assert.That(telemetry.Store.Appends.Count, Is.EqualTo(4));
            Assert.That(Last(telemetry.Store.Lines()).Event, Is.EqualTo("run_end"));
            AssertLogShape(ParseAll(telemetry.Store.Lines()));
        }

        [Test]
        public void RecordingWithAFailingStoreChangesNoGameplayResultAndReportsTheLoss()
        {
            var plain = new Scene(null);
            PlayDescent(plain);

            var telemetry = new Telemetry(8);
            for (int i = 0; i < 1000; i++)
                telemetry.Store.Failures.Enqueue(new IOException("The disk is full."));
            var recorded = new Scene(telemetry.Session);
            Assert.DoesNotThrow(() => PlayDescent(recorded));

            Assert.That((recorded.Run.Outcome, recorded.Run.Gold, recorded.Run.SecuredGold, recorded.Run.GoldBanked, recorded.Run.GoldLost),
                Is.EqualTo((plain.Run.Outcome, plain.Run.Gold, plain.Run.SecuredGold, plain.Run.GoldBanked, plain.Run.GoldLost)));
            Assert.That((recorded.Run.Level, recorded.Run.UpgradesApplied, recorded.Run.PendingUpgrades, recorded.Run.Experience),
                Is.EqualTo((plain.Run.Level, plain.Run.UpgradesApplied, plain.Run.PendingUpgrades, plain.Run.Experience)));
            Assert.That((recorded.Hero.Current, recorded.Profile.Gold, recorded.Profile.DeepestFloorCleared),
                Is.EqualTo((plain.Hero.Current, plain.Profile.Gold, plain.Profile.DeepestFloorCleared)));
            Assert.That((recorded.Pause.IsFrozen, recorded.Choices.IsOpen), Is.EqualTo((plain.Pause.IsFrozen, plain.Choices.IsOpen)));
            for (int i = 0; i < plain.Upgrades.Pool.Count; i++)
            {
                Assert.That(recorded.Upgrades.StacksOf(recorded.Upgrades.Pool[i]), Is.EqualTo(plain.Upgrades.StacksOf(plain.Upgrades.Pool[i])));
            }
            Assert.That(plain.Run.Outcome, Is.EqualTo(RunOutcome.Defeat), "The script reaches a defeat on the second floor.");

            TelemetryLog log = telemetry.Session.Log;
            Assert.That(telemetry.Store.Appends, Is.Empty);
            Assert.That(log.FailedWrites, Is.GreaterThan(0));
            Assert.That((log.Buffered, log.Dropped > 0), Is.EqualTo((8, true)), "The buffer stays bounded.");

            // Once the store recovers, the next event reports the loss first and the retained lines are written in order.
            telemetry.Store.Failures.Clear();
            long droppedBefore = log.Dropped;
            recorded.Recorder.ApplicationPaused(true);
            List<Line> lines = ParseAll(telemetry.Store.Lines());
            Assert.That(lines.Count, Is.EqualTo(8));
            Line dropped = lines[6];
            Assert.That((dropped.Event, lines[7].Event), Is.EqualTo(("telemetry_dropped", "app_background")));
            Assert.That(dropped.Integer("count"), Is.GreaterThan(0));
            Assert.That(dropped.Integer("failed_writes"), Is.EqualTo(log.FailedWrites));
            Assert.That(log.Dropped, Is.EqualTo(droppedBefore + 2), "Both new lines pushed an old one out of the full buffer.");
            Assert.That(lines[7].Has("run"), Is.False);
            for (int i = 1; i < lines.Count; i++)
                Assert.That(lines[i].Integer("seq"), Is.GreaterThan(lines[i - 1].Integer("seq")));
            recorded.Recorder.Dispose();
        }

        [Test]
        public void TheContextIsValidatedBeforeAnythingSubscribes()
        {
            Assert.Throws<ArgumentNullException>(() => new RunTelemetry(null));

            var telemetry = new Telemetry();
            var scene = new Scene(null);
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.Session = null)));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.Run = null)));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.Pause = null)));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.Choices = null)));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.Upgrades = null)));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.Forge = null)));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.Checkpoint = null)));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.KillerId = null)));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.HeroHealth = null)));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.HeroMaximumHealth = null)));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.HeroId = "")));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.WeaponId = null)));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.AbilityId = "")));
            Assert.Throws<ArgumentException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.RelicId = "")));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.ForgeGold = -1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunTelemetry(Mutated(scene, telemetry, c => c.DeepestFloorCleared = -1)));

            var unstarted = new TelemetrySession(new TelemetryLog(telemetry.Store, telemetry.Clock), telemetry.Clock, "s2");
            Assert.Throws<ArgumentException>(() => new RunTelemetry(scene.ContextFor(unstarted)),
                "session_start must stay the first line.");

            // The shops are optional; nothing is recorded before Begin or from a shop the recorder was not given.
            var recorder = new RunTelemetry(Mutated(scene, telemetry, c =>
            {
                c.Relics = null;
                c.Weapons = null;
                c.RelicId = "relic_second_wind";
            }));
            scene.Kill("enemy_grunt", 10, 3);
            Assert.That(scene.PickUpgrade(0), Is.True);
            Assert.That(telemetry.Session.LastSequence, Is.EqualTo(1), "Nothing is recorded before Begin.");
            recorder.Begin();
            Assert.That(scene.Relics.TrySelect(scene.Relics.Relics[0]), Is.True);
            List<Line> lines = telemetry.Lines();
            Assert.That(Events(lines), Is.EqualTo(new[] { "session_start", "run_start" }));
            Assert.That(lines[1].Text("relic_id"), Is.EqualTo("relic_second_wind"));
        }

        // One short Descent through every recorder input, in the order the scene raises them.
        private static void PlayDescent(Scene scene)
        {
            RunTelemetry recorder = scene.Recorder;
            recorder?.Begin();
            scene.Room(1, Floor1, 1, RoomKind.Combat, "enemy_grunt");
            recorder?.Tick(0.5f, 0.5f);
            scene.Kill("enemy_grunt", 10, 4);
            recorder?.Tick(0f, 0.5f);
            Assert.That(scene.PickUpgrade(1), Is.True);
            scene.GoldChest(10);
            scene.Forge.Open(new[] { Mend(), Temper() });
            scene.Room(1, Floor1, 2, RoomKind.Forge, null);
            recorder?.ApplicationPaused(true);
            recorder?.ApplicationPaused(false);
            Assert.That(scene.Forge.TrySelect(scene.Forge.CurrentOffer, 1), Is.True);
            Assert.That(scene.PickUpgrade(0), Is.True);
            scene.Room(1, Floor1, 3, RoomKind.Boss, "enemy_warden");
            recorder?.RecordAbilityUsed();
            scene.Hero.Current = 40f;
            scene.HealChest();
            scene.Kill("enemy_warden", 12, 20);
            Assert.That(scene.PickUpgrade(0), Is.True);
            scene.ClearFloor(1, Floor1, false);
            Assert.That(scene.Choose(CheckpointKind.Descend), Is.True);
            scene.Room(2, Floor2, 1, RoomKind.Elite, "enemy_brute");
            recorder?.Tick(1f, 1f);
            scene.Kill("enemy_brute", 3, 6);
            scene.Die("enemy_brute");
        }

        private static RunTelemetryContext Mutated(Scene scene, Telemetry telemetry, Action<RunTelemetryContext> change)
        {
            RunTelemetryContext context = scene.ContextFor(telemetry.Session);
            change(context);
            return context;
        }

        private static void AssertTimes(Line line, double active, double choice, double pause)
        {
            Assert.That((line.Number("active_sec"), line.Number("choice_sec"), line.Number("pause_sec")), Is.EqualTo((active, choice, pause)),
                line.Event + " at seq " + line.Integer("seq"));
        }

        private static void AssertEarned(Line line, string currency, string source, long amount, long floor, long room)
        {
            Assert.That((line.Text("currency"), line.Text("source"), line.Integer("amount"), line.Integer("floor_index"), line.Integer("room_index")),
                Is.EqualTo((currency, source, amount, floor, room)));
        }

        // The ordering rules every log must keep, whatever the scenario.
        private static void AssertLogShape(List<Line> lines)
        {
            Assert.That(lines.Count, Is.GreaterThan(0));
            Assert.That(lines[0].Event, Is.EqualTo("session_start"));
            Assert.That(lines[0].Has("run"), Is.False);
            string openRun = null;
            var closedRuns = new HashSet<string>();
            bool roomOpen = false;
            long roomFloor = 0;
            long roomIndex = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                Line line = lines[i];
                string where = line.Event + " at line " + i;
                Assert.That(line.Integer("seq"), Is.EqualTo(i + 1), "Sequence numbers are strictly increasing without gaps: " + where);
                Assert.That(line.Text("session"), Is.EqualTo(lines[0].Text("session")), where);
                if (i > 0)
                    Assert.That(line.Event, Is.Not.EqualTo("session_start"), where);

                string run = line.Has("run") ? line.Text("run") : null;
                if (line.Event == "run_start")
                {
                    Assert.That(openRun, Is.Null, "A run starts only after the previous one closed: " + where);
                    Assert.That(run, Is.Not.Null, where);
                    Assert.That(closedRuns.Contains(run), Is.False, "Run ids are unique: " + where);
                    openRun = run;
                    roomOpen = false;
                    roomFloor = 0;
                    roomIndex = 0;
                    continue;
                }

                // The open run is never a closed one, so this also keeps a closed run's id off every later line.
                Assert.That(run, Is.EqualTo(openRun), "Events of a run carry its id and nothing else does: " + where);

                switch (line.Event)
                {
                    case "run_end":
                    case "run_interrupted":
                        closedRuns.Add(openRun);
                        openRun = null;
                        break;
                    case "room_start":
                        Assert.That(roomOpen, Is.False, "The previous room completes before the next starts: " + where);
                        roomOpen = true;
                        roomFloor = line.Integer("floor_index");
                        roomIndex = line.Integer("room_index");
                        break;
                    case "room_complete":
                        Assert.That(roomOpen, Is.True, where);
                        Assert.That((line.Integer("floor_index"), line.Integer("room_index")), Is.EqualTo((roomFloor, roomIndex)), where);
                        roomOpen = false;
                        break;
                    case "wave_start":
                    case "wave_clear":
                    case "horde_sample":
                        Assert.That(roomOpen, Is.True, "A wave or a horde sample belongs to the open room: " + where);
                        Assert.That((line.Integer("floor_index"), line.Integer("room_index")), Is.EqualTo((roomFloor, roomIndex)), where);
                        break;
                    case "boss_end":
                        Assert.That(lines[i - 1].Event, Is.EqualTo("room_complete"), "boss_end follows its room_complete: " + where);
                        Assert.That((line.Integer("floor_index"), line.Integer("room_index")),
                            Is.EqualTo((lines[i - 1].Integer("floor_index"), lines[i - 1].Integer("room_index"))), where);
                        break;
                    case "currency_earned":
                        Assert.That(i + 1, Is.LessThan(lines.Count), "currency_earned is never a run's last line: " + where);
                        if (!line.Has("room_index"))
                        {
                            Assert.That(lines[i + 1].Event, Is.EqualTo("run_end"), "Banked gold is reported just before run_end: " + where);
                            break;
                        }
                        Line next = lines[i + 1];
                        Assert.That(next.Event == "currency_earned" || next.Event == "room_complete", Is.True,
                            "A room's earnings directly precede its room_complete: " + where);
                        Assert.That((next.Integer("floor_index"), next.Integer("room_index")),
                            Is.EqualTo((line.Integer("floor_index"), line.Integer("room_index"))), where);
                        break;
                }
            }
        }

        private static List<string> Events(List<Line> lines)
        {
            var events = new List<string>(lines.Count);
            foreach (Line line in lines)
                events.Add(line.Event);
            return events;
        }

        private static List<Line> All(List<Line> lines, string name)
        {
            var found = new List<Line>();
            foreach (Line line in lines)
            {
                if (line.Event == name)
                    found.Add(line);
            }
            return found;
        }

        private static Line Single(List<Line> lines, string name)
        {
            List<Line> found = All(lines, name);
            Assert.That(found.Count, Is.EqualTo(1), "Exactly one " + name);
            return found[0];
        }

        private static Line Last(List<string> lines) => Line.Parse(lines[lines.Count - 1]);

        private static List<Line> ParseAll(List<string> texts)
        {
            var lines = new List<Line>(texts.Count);
            foreach (string text in texts)
                lines.Add(Line.Parse(text));
            return lines;
        }

        // A started session over an in-memory store and a hand-stepped clock.
        private sealed class Telemetry
        {
            public readonly TelemetryLogTests.FakeClock Clock = new TelemetryLogTests.FakeClock();
            public readonly TelemetryLogTests.MemoryStore Store = new TelemetryLogTests.MemoryStore();
            public readonly TelemetrySession Session;

            public Telemetry(int maxBufferedEvents = 512)
            {
                var log = new TelemetryLog(Store, Clock, maxBufferedEvents, Math.Min(64, maxBufferedEvents));
                Session = new TelemetrySession(log, Clock, "s1");
                Session.Start(new TelemetryFields().Add("app_version", "test").Add("platform", "editor").Add("development", true));
            }

            // Writes what is buffered, then parses every line the store holds.
            public List<Line> Lines()
            {
                Session.Flush();
                return ParseAll(Store.Lines());
            }
        }

        private sealed class FakeHero : IHealable
        {
            public float Current { get; set; }
            // Settable so a test can raise it the way a health card does.
            public float Maximum { get; set; }
            public bool IsAlive => Current > 0f;

            public FakeHero(float maximum)
            {
                Maximum = maximum;
                Current = maximum;
            }

            public void Heal(float amount)
            {
                if (IsAlive)
                    Current = Math.Min(Maximum, Current + amount);
            }
        }

        // The pure services in CombatSetup's construction order, with its reactions subscribed after the recorder.
        private sealed class Scene
        {
            public readonly RunState Run = new RunState(10);
            public readonly FakeHero Hero = new FakeHero(100f);
            public readonly PlayerProfile Profile = new PlayerProfile(100, null, null, 0);
            public readonly RelicShop Relics;
            public readonly WeaponShop Weapons;
            public readonly RewardService Rewards;
            public readonly RunBank Bank;
            public readonly UpgradeService Upgrades;
            public readonly ForgeService Forge;
            public readonly CheckpointService Checkpoint;
            public readonly RunChoices Choices;
            public readonly RunPause Pause;
            public readonly RunTelemetry Recorder;
            // Category mode only: the hero starts with the Sword and may pick up the Daggers.
            public readonly WeaponLoadout Loadout;
            public string Killer;

            // categories builds the foundry's engine (docs/42): the Sword in slot 0, the Sword and Daggers as weapon cards,
            // Force and Precision as tomes, and Vital Surge as the only general card left once the weapon cards leave. Six
            // choices outnumber every card such a run can be shown at once, so each offer shows them all and a test takes
            // exactly the card it names.
            public Scene(TelemetrySession session, bool categories = false)
            {
                Relics = new RelicShop(Profile, new[]
                {
                    new RelicOption("relic_second_wind", "Second Wind", "{0:0}% below {1:0}%", RelicEffect.SecondWind, 0.3f, 0.25f, 60),
                    new RelicOption("relic_counterweight", "Counterweight", "{0:0}%", RelicEffect.CounterStrike, 0.5f, 0f, 500)
                });
                var sword = new WeaponOption("weapon_sword", "Sword", string.Empty, 0);
                Weapons = new WeaponShop(Profile, new[] { sword, new WeaponOption("weapon_axe", "Axe", string.Empty, 40) }, sword);
                Rewards = new RewardService(Run);
                Bank = new RunBank(Run, Profile);
                if (categories)
                {
                    WeaponCard swordCard = DescentSimulation.Sword().CreateCard();
                    WeaponCard daggersCard = DescentSimulation.Daggers().CreateCard();
                    Loadout = new WeaponLoadout(Run.Seed);
                    WeaponRuntime primary = swordCard.CreateRuntime();
                    Loadout.TryAdd(swordCard.Id, primary);
                    Upgrades = new UpgradeService(Run, primary, new[] { Damage(), Speed(), Health() }, 6, null, false,
                        Loadout, new[] { swordCard, daggersCard },
                        new[] { DescentSimulation.Force(), DescentSimulation.Precision() });
                }
                else
                {
                    Upgrades = new UpgradeService(Run, new WeaponRuntime(10f, 1f, 1f), new[] { Damage(), Speed() }, 2);
                }
                Forge = new ForgeService(Run, Hero);
                Checkpoint = new CheckpointService(Run);
                Choices = new RunChoices(Upgrades, Forge, Checkpoint);
                Pause = new RunPause(Run);
                Choices.Changed += () => Pause.SetChoiceOpen(Choices.IsOpen);
                if (session != null)
                    Recorder = new RunTelemetry(ContextFor(session));
                Checkpoint.Chosen += OnCheckpointChosen;
            }

            public RunTelemetryContext ContextFor(TelemetrySession session)
            {
                return new RunTelemetryContext
                {
                    Session = session,
                    Run = Run,
                    Pause = Pause,
                    Choices = Choices,
                    Upgrades = Upgrades,
                    Forge = Forge,
                    Checkpoint = Checkpoint,
                    Relics = Relics,
                    Weapons = Weapons,
                    HeroId = "hero_vanguard",
                    WeaponId = Weapons.Equipped.Id,
                    RelicId = Relics.Equipped?.Id,
                    AbilityId = "ability_forge_burst",
                    ForgeGold = Profile.Gold,
                    DeepestFloorCleared = Profile.DeepestFloorCleared,
                    KillerId = () => Killer,
                    HeroHealth = () => Hero.Current,
                    HeroMaximumHealth = () => Hero.Maximum,
                    // As CombatSetup wires it; a legacy scene here never set one.
                    Pulses = Loadout != null ? () => RunTelemetry.PulsesFired(Upgrades.Loadout, Loadout.Primary) : (Func<int>)null
                };
            }

            // Takes the first card of the open offer that match accepts; false, and nothing taken, when it shows none.
            public bool TryTake(Func<UpgradeOption, bool> match)
            {
                UpgradeOffer offer = Upgrades.CurrentOffer;
                Assert.That(offer, Is.Not.Null, "An offer is open.");
                for (int i = 0; i < offer.Choices.Count; i++)
                {
                    if (match(offer.Choices[i]))
                        return Upgrades.TrySelect(offer, i);
                }
                return false;
            }

            // One level-up, answered with the card match names.
            public void LevelUpTaking(Func<UpgradeOption, bool> match, string what)
            {
                Run.GrantBonusUpgrade();
                Assert.That(TryTake(match), Is.True, "The offer shows " + what + ".");
            }

            public void Room(int floor, string floorId, int room, RoomKind kind, string toughestEnemyId)
            {
                Recorder?.ObserveProgress(floor, floorId, room, kind, toughestEnemyId);
            }

            // The bridge hears EnemyDefeated before CombatSetup pays the reward.
            public void Kill(string enemyId, int experience, int gold)
            {
                var victim = new HealthState(1f);
                victim.ApplyDamage(new DamageContext(1f));
                Recorder?.RecordKill(enemyId, gold);
                Rewards.TryAwardKill(victim, experience, gold);
            }

            // ChestSpawner applies the reward, then raises Opened.
            public void GoldChest(int gold)
            {
                Run.AddGold(gold);
                Recorder?.RecordChestOpened(ChestRewardKind.Gold, 0f);
            }

            public void HealChest(string bookId = null, UpgradeRarity bookRarity = UpgradeRarity.Common)
            {
                Hero.Heal(Hero.Maximum * ChestRule.HealFraction);
                Recorder?.RecordChestOpened(ChestRewardKind.Heal, ChestRule.HealFraction, bookId, bookRarity);
            }

            public bool PickUpgrade(int slot) => Upgrades.TrySelect(Upgrades.CurrentOffer, slot);

            // The bridge hears FloorCleared before CombatSetup ends the Descent or opens the checkpoint.
            public void ClearFloor(int floor, string floorId, bool final)
            {
                Recorder?.RecordFloorCleared(floor, floorId);
                Profile.RecordFloorCleared(floor);
                if (final)
                {
                    Run.End(RunOutcome.Victory);
                    return;
                }

                Checkpoint.Open(new[]
                {
                    new CheckpointOption(CheckpointKind.Extract, "Extract", string.Empty),
                    new CheckpointOption(CheckpointKind.Descend, "Descend", string.Empty)
                });
            }

            public bool Choose(CheckpointKind kind) => Checkpoint.TrySelect(Checkpoint.CurrentOffer, kind == CheckpointKind.Extract ? 0 : 1);

            // Health.Damaged names the attacker before Died ends the run.
            public void Die(string killer)
            {
                Killer = killer;
                Hero.Current = 0f;
                Run.End(RunOutcome.Defeat);
            }

            private void OnCheckpointChosen(CheckpointKind kind)
            {
                if (kind == CheckpointKind.Extract)
                {
                    Run.End(RunOutcome.Extracted);
                    return;
                }
                Run.SecureGold();
            }
        }

        // One parsed log line: a flat JSON object read without a JSON library. Strings are unescaped; other values keep
        // their literal token (numbers, true, false, null).
        private sealed class Line
        {
            public readonly List<string> Keys = new List<string>();
            private readonly Dictionary<string, string> _values = new Dictionary<string, string>();
            private readonly HashSet<string> _strings = new HashSet<string>();

            public string Event => Text("event");

            public bool Has(string key) => _values.ContainsKey(key);

            public string Text(string key)
            {
                Assert.That(_strings.Contains(key), Is.True, "String field " + key);
                return _values[key];
            }

            public bool IsNull(string key) => Has(key) && !_strings.Contains(key) && _values[key] == "null";

            public bool Bool(string key)
            {
                string token = Token(key);
                Assert.That(token == "true" || token == "false", Is.True, "Boolean field " + key);
                return token == "true";
            }

            public long Integer(string key) => long.Parse(Token(key), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);

            public double Number(string key) => double.Parse(Token(key), NumberStyles.Float, CultureInfo.InvariantCulture);

            private string Token(string key)
            {
                Assert.That(Has(key) && !_strings.Contains(key), Is.True, "Literal field " + key);
                return _values[key];
            }

            public static Line Parse(string json)
            {
                var line = new Line();
                int i = 0;
                Expect(json, ref i, '{');
                while (true)
                {
                    string key = ReadString(json, ref i);
                    Expect(json, ref i, ':');
                    string value;
                    bool isString = i < json.Length && json[i] == '"';
                    if (isString)
                    {
                        value = ReadString(json, ref i);
                    }
                    else
                    {
                        int start = i;
                        while (i < json.Length && json[i] != ',' && json[i] != '}')
                            i++;
                        value = json.Substring(start, i - start);
                        Assert.That(value.Length, Is.GreaterThan(0), "A value in " + json);
                    }

                    Assert.That(line._values.ContainsKey(key), Is.False, "Duplicate key " + key);
                    line.Keys.Add(key);
                    line._values.Add(key, value);
                    if (isString)
                        line._strings.Add(key);

                    Assert.That(i, Is.LessThan(json.Length), "Unterminated object " + json);
                    if (json[i] == ',')
                    {
                        i++;
                        continue;
                    }
                    Expect(json, ref i, '}');
                    break;
                }
                Assert.That(i, Is.EqualTo(json.Length), "Nothing follows the object in " + json);
                return line;
            }

            private static void Expect(string json, ref int i, char expected)
            {
                Assert.That(i < json.Length && json[i] == expected, Is.True, "Expected '" + expected + "' at " + i + " in " + json);
                i++;
            }

            private static string ReadString(string json, ref int i)
            {
                Expect(json, ref i, '"');
                var builder = new StringBuilder();
                while (true)
                {
                    Assert.That(i, Is.LessThan(json.Length), "Unterminated string in " + json);
                    char c = json[i++];
                    if (c == '"')
                        return builder.ToString();
                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }

                    char escape = json[i++];
                    switch (escape)
                    {
                        case 'u':
                            builder.Append((char)int.Parse(json.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            i += 4;
                            break;
                        case 'n':
                            builder.Append('\n');
                            break;
                        case 'r':
                            builder.Append('\r');
                            break;
                        case 't':
                            builder.Append('\t');
                            break;
                        case 'b':
                            builder.Append('\b');
                            break;
                        case 'f':
                            builder.Append('\f');
                            break;
                        default:
                            builder.Append(escape);
                            break;
                    }
                }
            }
        }
    }
}
