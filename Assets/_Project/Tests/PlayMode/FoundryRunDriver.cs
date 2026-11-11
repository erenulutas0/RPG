using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // Plays a run on the foundry for the flow tests that follow it past a room: the scene loaded on a development floor
    // (Resources/Development) with the run's seed pinned, the hero's own strikes and walk off so the test deals every blow
    // and places the hero by hand, every offer answered - a level-up with its first card, the forge with its first option
    // - and the floor driven up to whatever the test waits for: an open district walked onto, each standing wave felled
    // as it stands, the gate's boss left to the test. A checkpoint is always the test's to answer.
    internal static class FoundryRunDriver
    {
        public const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // Every run driven here plays this seed: its offers, the horde's draws and the chests' books are all drawn from
        // it, so a run replays the way it played (DevelopmentStart.RunSeedPath).
        public const string Seed = "1111";
        public const int CheckpointExtract = 0;
        public const int CheckpointDescend = 1;

        public static CombatSetup Setup => Object.FindFirstObjectByType<CombatSetup>();
        public static Health Hero => Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();

        // The scene on a floor of Resources/Development (null keeps the scene's own, the shipped climb) on the given arena,
        // each foundry floor's clock started clockStart seconds on when that is above zero (DevelopmentStart.FloorClockPath).
        public static IEnumerator Load(string floor, float clockStart = 0f, string region = "foundry")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            if (floor != null)
                File.WriteAllText(DevelopmentStart.StartFloorPath, floor);
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, region);
            File.WriteAllText(DevelopmentStart.RunSeedPath, Seed);
            if (clockStart > 0f)
                File.WriteAllText(DevelopmentStart.FloorClockPath, clockStart.ToString(CultureInfo.InvariantCulture));
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            HoldHero();
        }

        // The hero's strikes and walk off: the test deals every blow and places the hero by hand.
        public static void HoldHero()
        {
            var input = Object.FindFirstObjectByType<HeroMovementInput>();
            input.enabled = false;
            input.GetComponent<AttackController>().enabled = false;
        }

        public static void Place(Health hero, float floorX, float floorY) =>
            hero.transform.position = new Vector3(floorX, ArenaFloor.WorldY(floorY), 0f);

        // Every offer the run holds open answered - an open choice pauses the game until it is - and the hero healed to
        // full unless heal is false.
        public static void Keep(CombatSetup setup, Health hero, bool heal = true)
        {
            for (int i = 0; i < 8; i++)
            {
                ChoicePrompt prompt = setup.Choices.Current;
                if (prompt == null || prompt.Kind == ChoiceKind.Checkpoint)
                    break;
                Assert.That(setup.Choices.TrySelect(prompt, 0), Is.True, $"The first card of a {prompt.Kind} offer can be taken.");
            }
            if (heal && hero.IsAlive && hero.Current < hero.Maximum)
                hero.Heal(hero.Maximum);
        }

        // Frames of the run until done says the test is there, everything open answered and the hero kept standing: an
        // open district is walked onto, and each standing wave falls as it stands - but the gate's boss, and its escort
        // while it stands, are the test's. Fails on the real-time deadline or when the run ends first.
        public static IEnumerator Drive(CombatSetup setup, Health hero, Func<bool> done, float seconds = 60f)
        {
            EncounterController encounters = setup.Encounters;
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!done() && !setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
            {
                Keep(setup, hero);
                if (encounters.IsRoomSiteOpen)
                    Place(hero, encounters.RoomSiteFloorX, encounters.RoomSiteFloorY);
                else if (encounters.GateBoss == null && !encounters.IsInSwarmRoom)
                    PackTestUtility.KillWave(encounters);
                yield return null;
            }
            Keep(setup, hero);
            Assert.That(done(), Is.True,
                $"Stopped on floor {encounters.FloorNumber}, room {encounters.RoomNumber}, the run {setup.Run.Outcome}.");
        }

        // Frames until the condition holds, everything open answered, the hero healed unless heal is false; the caller
        // asserts what it waited for.
        public static IEnumerator Until(CombatSetup setup, Health hero, Func<bool> condition, float seconds, bool heal = true)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                Keep(setup, hero, heal);
                yield return null;
            }
            Keep(setup, hero, heal);
        }

        // Game time passing, everything open answered, the hero healed unless heal is false, bounded in real time so a
        // pause can never hang a run.
        public static IEnumerator Hold(CombatSetup setup, Health hero, float seconds, bool heal = true)
        {
            float end = Time.time + seconds, deadline = Time.realtimeSinceStartup + seconds + 20f;
            while (Time.time < end && Time.realtimeSinceStartup < deadline)
            {
                Keep(setup, hero, heal);
                yield return null;
            }
            Keep(setup, hero, heal);
            Assert.That(Time.time, Is.GreaterThanOrEqualTo(end), "Game time must pass; nothing may stay paused.");
        }

        // The opened gate walked out of: the hero placed on its spot and left standing there until the floor clears.
        public static IEnumerator WalkOutOfTheGate(CombatSetup setup, Health hero)
        {
            EncounterController encounters = setup.Encounters;
            Assert.That(encounters.IsGateExitOpen, Is.True, "The gate is open.");
            FoundryMap.GateExitSpot(out float x, out float y);
            Place(hero, x, y);
            float deadline = Time.realtimeSinceStartup + 20f;
            while (encounters.IsGateExitOpen && !setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
            {
                Keep(setup, hero);
                yield return null;
            }
            Assert.That(encounters.IsGateExitOpen, Is.False, "Standing still on the opened gate walks out of it.");
        }

        // The checkpoint the cleared floor opens, behind the level-ups its swept drops paid for.
        public static IEnumerator UntilCheckpoint(CombatSetup setup, Health hero)
        {
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!IsCheckpoint(setup.Choices.Current) && !setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
            {
                Keep(setup, hero);
                yield return null;
            }
            Assert.That(IsCheckpoint(setup.Choices.Current), Is.True, "The cleared floor offers Extract or Descend.");
        }

        // The floor driven to its gate, the gate's whole wave felled and the opened gate walked out of: the floor is
        // cleared and its checkpoint stands open.
        public static IEnumerator ClearThroughTheGate(CombatSetup setup, Health hero)
        {
            EncounterController encounters = setup.Encounters;
            yield return Drive(setup, hero, () => encounters.GateBoss != null);
            PackTestUtility.KillWave(encounters);
            Assert.That(encounters.IsGateExitOpen, Is.True, $"Floor {encounters.FloorNumber}'s gate opens as its wave falls.");
            yield return WalkOutOfTheGate(setup, hero);
            yield return UntilCheckpoint(setup, hero);
        }

        // The ended run started again as the result screen's Try again does (CombatSetup.RestartRun): one fresh scene, the
        // ended one gone, the new hero held as Load holds it.
        public static IEnumerator Restart(CombatSetup previous)
        {
            int loads = 0;
            UnityAction<Scene, LoadSceneMode> counted = (scene, mode) => loads++;
            SceneManager.sceneLoaded += counted;
            try
            {
                previous.RestartRun();
                float deadline = Time.realtimeSinceStartup + 10f;
                while ((loads == 0 || previous != null) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                yield return null;
            }
            finally
            {
                SceneManager.sceneLoaded -= counted;
            }
            Assert.That(loads, Is.EqualTo(1), "The restart loads one scene.");
            Assert.That(previous == null, Is.True, "The ended run's scene is gone.");
            HoldHero();
        }

        // The first standing enemy of the floor's horde, or null.
        public static Health StandingHordeEnemy(EncounterController encounters)
        {
            for (int i = 0; i < encounters.HordeEnemyCount; i++)
            {
                Health enemy = encounters.HordeEnemyAt(i);
                if (enemy != null && enemy.IsAlive)
                    return enemy;
            }
            return null;
        }

        // The HUD's row of room pips as shown: how many stand on it, and how many of those are lit as reached (the colour
        // RunHud.RefreshEncounter gives a room the floor has got to).
        public static (int Shown, int Lit) Pips(RunHud hud)
        {
            int shown = 0, lit = 0;
            for (int i = 0; i < hud.RoomPipCount; i++)
            {
                Image pip = hud.RoomPipAt(i);
                if (!pip.gameObject.activeSelf)
                    continue;
                shown++;
                Color32 colour = pip.color;
                if (colour.r == 218 && colour.g == 165 && colour.b == 84)
                    lit++;
            }
            return (shown, lit);
        }

        public static string Label(string name) => GameObject.Find(name).GetComponent<Text>().text;

        private static bool IsCheckpoint(ChoicePrompt prompt) => prompt != null && prompt.Kind == ChoiceKind.Checkpoint;
    }
}
