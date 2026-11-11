using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The horde's bodies are pooled (EncounterController.BodyPool, the allocation audit of 2026-11-11): a body that left
    // the floor comes back for the next of its kind reset as Instantiate makes a new one, and a spawn that stands one up
    // allocates a fraction of one that instantiates. The Final Swarm's plain kinds are pooled the same way (2026-11-12),
    // never the Forge Warden, whose EnrageBehaviour the pool does not reset.
    public sealed class HordeBodyPoolTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        // GC.Alloc samples one horde spawn saves by standing a shelved body up instead of instantiating one, as measured
        // (AShelvedBodyWalksInForAFractionOfANewOne), and the margin the bound leaves under it.
        private const int MeasuredSaving = 30;
        private const int SavingMargin = 10;
        // Game seconds the finale's swarm test gives the autopilot to fight its minute (DevelopmentAutopilot.SwarmStay)
        // and walk out: the walk and the exit's hold take a few seconds, and the rest is margin.
        private const float SwarmTestSeconds = 120f;
        private const string WardenId = "enemy_forge_warden";

        [UnitySetUp] public IEnumerator Begin() { TestProfile.Begin(); Time.timeScale = 1; yield return null; }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Horde body pool cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        private static IEnumerator Load()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            // Even frames, so the horde's clock and the retirements run the same however fast the batch is.
            Time.captureDeltaTime = 1f / 60;
        }

        // Every offer answered with its first card, and the hero kept standing.
        private static void KeepUp(CombatSetup setup, Health hero)
        {
            for (int i = 0; i < 4 && setup.Choices.IsOpen; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
            if (hero.IsAlive)
                hero.Heal(hero.Maximum);
        }

        private static int Listeners(object owner, string field)
        {
            var handler = (Delegate)owner.GetType().GetField(field, Private).GetValue(owner);
            return handler != null ? handler.GetInvocationList().Length : 0;
        }

        // A horde body as it walks in (HordeEnemySpawned, after the effects view bound its watch and began its arrival):
        // everything a new body and a reused one could differ by, but the health its floor's clock sets.
        private sealed class Walk
        {
            public bool Alive, Full;
            public float Maximum;
            public int Damaged, Changed, Died, Attacked, Struck, StruckWith;
            public bool Ready, Held, OwnHold;
            public float Cooldown;
            public int AttackCount, Weapons;
            public int Frame, WindupStep;
            public bool Rear, Mirrored, WindingUp, Enraged, Burning, Elite;
            public object Pose;
            public Color Tint;
            public Vector3 BodyScale, BodyAt, RootScale;
            public Quaternion RootTurn;
            public bool BodyShown;
            public float Alpha, Arrival;

            public static Walk Of(Health enemy, EncounterController encounters)
            {
                var attack = enemy.GetComponent<AttackController>();
                var look = enemy.GetComponent<EnemyLookView>();
                bool elite = false;
                for (int i = 0; i < encounters.Elites.Count; i++)
                    elite |= ReferenceEquals(encounters.Elites[i], enemy);
                elite |= ReferenceEquals(encounters.EliteLeader, enemy) || ReferenceEquals(encounters.GateBoss, enemy);
                Color tint = look.Body.color;
                return new Walk
                {
                    Alive = enemy.IsAlive, Full = enemy.Current == enemy.Maximum, Maximum = enemy.Maximum,
                    Damaged = Listeners(enemy, "Damaged"), Changed = Listeners(enemy, "Changed"), Died = Listeners(enemy, "Died"),
                    Attacked = Listeners(attack, "Attacked"), Struck = Listeners(attack, "Struck"),
                    StruckWith = Listeners(attack, "StruckWith"),
                    Ready = attack.Weapon.IsReady, Cooldown = attack.Weapon.CooldownRemaining, Held = attack.IsHeld,
                    OwnHold = ReferenceEquals(attack.FightHold, encounters.FightHold), AttackCount = attack.AttackCount,
                    Weapons = attack.WeaponCount,
                    Frame = look.PaintedFrame, WindupStep = look.WindupStep, Rear = look.RearFacing, Mirrored = look.Mirrored,
                    WindingUp = look.IsWindingUp, Enraged = look.ShowsEnraged, Pose = look.CurrentPose,
                    Burning = enemy.TryGetComponent(out BurnStatus _), Elite = elite,
                    Tint = new Color(tint.r, tint.g, tint.b, 1f), Alpha = tint.a, Arrival = look.ArrivalProgress,
                    BodyScale = look.Body.transform.localScale, BodyAt = look.Body.transform.localPosition,
                    BodyShown = look.Body.gameObject.activeSelf && look.Body.enabled,
                    RootScale = enemy.transform.localScale, RootTurn = enemy.transform.rotation
                };
            }

            public void Matches(Walk fresh)
            {
                Assert.That(Alive, Is.True, "Alive.");
                Assert.That(Full, Is.True, "At full health.");
                Assert.That((Damaged, Changed, Died), Is.EqualTo((fresh.Damaged, fresh.Changed, fresh.Died)),
                    "Its health is heard by a new body's listeners and no others.");
                Assert.That((Attacked, Struck, StruckWith), Is.EqualTo((fresh.Attacked, fresh.Struck, fresh.StruckWith)),
                    "Its attacker likewise.");
                Assert.That((Ready, Cooldown, Held, OwnHold, AttackCount, Weapons),
                    Is.EqualTo((fresh.Ready, fresh.Cooldown, fresh.Held, fresh.OwnHold, fresh.AttackCount, fresh.Weapons)),
                    "Its attack as a new one's: a new weapon, unheld, held by the reveal, nothing struck.");
                Assert.That(OwnHold, Is.True);
                Assert.That((Frame, WindupStep, Rear, Mirrored, WindingUp, Enraged),
                    Is.EqualTo((fresh.Frame, fresh.WindupStep, fresh.Rear, fresh.Mirrored, fresh.WindingUp, fresh.Enraged)),
                    "Standing, facing front, nothing wound up or enraged.");
                Assert.That(Pose, Is.EqualTo(fresh.Pose));
                Assert.That(Burning, Is.False, "No burn of its last life.");
                Assert.That(Elite, Is.False, "Not an elite, a Captain or the gate boss.");
                Assert.That(Tint, Is.EqualTo(fresh.Tint), "Its colour.");
                Assert.That((Alpha, Arrival), Is.EqualTo((fresh.Alpha, fresh.Arrival)), "Arriving as a new one does.");
                Assert.That((BodyScale, BodyAt, BodyShown), Is.EqualTo((fresh.BodyScale, fresh.BodyAt, fresh.BodyShown)),
                    "Its body shown, at rest and unsquashed.");
                Assert.That((RootScale, RootTurn), Is.EqualTo((fresh.RootScale, fresh.RootTurn)));
            }
        }

        [UnityTest, Timeout(150000)]
        public IEnumerator AReusedBodyWalksInAsANewOne()
        {
            yield return Load();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var encounters = setup.Encounters;
            var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            var seen = new Dictionary<Health, float>();
            var fresh = new Dictionary<EnemyDefinition, Walk>();
            Health reused = null;
            Walk reusedWalk = null, freshWalk = null;
            float lastLife = 0f;
            Action<int> spawned = slot =>
            {
                Health enemy = encounters.HordeEnemyAt(slot);
                EnemyDefinition kind = encounters.DefinitionOf(enemy);
                Walk walk = Walk.Of(enemy, encounters);
                if (seen.TryGetValue(enemy, out float maximum))
                {
                    if (reused == null && fresh.TryGetValue(kind, out Walk first))
                    {
                        reused = enemy;
                        reusedWalk = walk;
                        freshWalk = first;
                        lastLife = maximum;
                    }
                }
                else if (!fresh.ContainsKey(kind))
                    fresh[kind] = walk;
                seen[enemy] = enemy.Maximum;
            };
            // Bodies that walked in before this listened are known, so none of them is taken for a new one if it comes back.
            for (int i = 0; i < encounters.HordeEnemyCount; i++)
            {
                Health enemy = encounters.HordeEnemyAt(i);
                if (enemy != null)
                    seen[enemy] = enemy.Maximum;
            }
            encounters.HordeEnemySpawned += spawned;
            try
            {
                // Every horde enemy standing is set alight and felled, so bodies leave the floor burning and the floor
                // keeps sending more, until one comes back.
                float deadline = Time.realtimeSinceStartup + 90;
                while (reused == null && !setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
                {
                    KeepUp(setup, hero);
                    for (int i = 0; i < encounters.HordeEnemyCount; i++)
                    {
                        Health enemy = encounters.HordeEnemyAt(i);
                        if (enemy == null || !enemy.IsAlive)
                            continue;
                        BurnStatus.On(enemy)?.Ignite(1, false, hero, null);
                        enemy.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
                    }
                    yield return null;
                }
            }
            finally
            {
                encounters.HordeEnemySpawned -= spawned;
            }
            Assert.That(reused, Is.Not.Null, "A body that left the floor walks in again.");
            reusedWalk.Matches(freshWalk);
            Assert.That(reusedWalk.Maximum, Is.GreaterThanOrEqualTo(lastLife), "Its health is the floor's now, not its last life's.");

            // A frame on, as a new body's Start shows it: the bar whole.
            yield return null;
            KeepUp(setup, hero);
            var look = reused.GetComponent<EnemyLookView>();
            Assert.That(reused.IsAlive, Is.True);
            Assert.That(look.Bar.gameObject.activeSelf, Is.True, "Its bar is shown.");
            Assert.That(reused.isActiveAndEnabled, Is.True);

            // Its kill pays once: the reward service forgot its last life as it left the floor.
            int drops = setup.Drops.Count;
            reused.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(setup.Drops.Count, Is.EqualTo(drops + 1), "Its new life's kill drops its reward.");
            reused.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(setup.Drops.Count, Is.EqualTo(drops + 1), "Once.");
            LogAssert.NoUnexpectedReceived();
        }

        // What one horde or swarm spawn allocates on this thread: GC.Alloc samples, as ChoicePanelAllocationTests counts
        // them, and the managed heap's growth in bytes. What the spawn returned: for the horde's, whether it walked in.
        private static object Measure(EncounterController encounters, MethodInfo spawn, object[] args, out int allocations,
            out long bytes)
        {
            Recorder recorder = Recorder.Get("GC.Alloc");
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            long before = Profiler.GetMonoUsedSizeLong();
            recorder.enabled = true;
            object walked;
            try
            {
                walked = spawn.Invoke(encounters, args);
            }
            finally
            {
                recorder.enabled = false;
                recorder.CollectFromAllThreads();
            }
            bytes = Profiler.GetMonoUsedSizeLong() - before;
            allocations = recorder.sampleBlockCount;
            return walked;
        }

        // The perf probe's heavy horde with the bodies reused under it (2026-11-11: a body back from the pool still stood in
        // the place it left in the pack's motion, so the place read as living and its refill threw): the autopilot fights the
        // overrun floor - the clock past the crowd, the horde aiming for the scene's cap - at three times speed for 30 s of
        // game time, and on every frame every place of the horde and its motion hold the same body, a standing enemy's
        // body lives on the floor, no body stands in two places, and no exception is logged.
        [UnityTest, Timeout(240000)]
        public IEnumerator TheProbesHeavyHordeReusesBodiesWithoutLosingAPlace()
        {
            // The autopilot says which cohort it drives as it starts; the test ends on LogAssert.NoUnexpectedReceived.
            LogAssert.Expect(LogType.Log, new Regex(@"^\[Autopilot\] driving the hero as the careful cohort"));
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.AutopilotPath));
            File.WriteAllText(DevelopmentStart.AutopilotPath, "on");
            File.WriteAllText(DevelopmentStart.RunSeedPath, "11");
            File.WriteAllText(DevelopmentStart.FloorClockPath, "290");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var encounters = setup.Encounters;
            var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            Time.captureDeltaTime = 1f / 60;
            Time.timeScale = 3;

            var seen = new HashSet<Health>();
            int spawns = 0, reuses = 0;
            Action<int> spawned = slot =>
            {
                Health enemy = encounters.HordeEnemyAt(slot);
                spawns++;
                if (!seen.Add(enemy))
                    reuses++;
            };
            encounters.HordeEnemySpawned += spawned;
            string fault = null;
            int frames = 0, crowd = 0;
            try
            {
                for (int i = 0; i < encounters.HordeEnemyCount; i++)
                {
                    if (encounters.HordeEnemyAt(i) != null)
                        seen.Add(encounters.HordeEnemyAt(i));
                }
                float start = Time.time;
                float deadline = Time.realtimeSinceStartup + 180;
                while (Time.time - start < 30f && fault == null && !setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
                {
                    KeepUp(setup, hero);
                    yield return null;
                    frames++;
                    crowd = Mathf.Max(crowd, encounters.HordeAliveCount);
                    fault = encounters.HordePlacesFault();
                }
                Assert.That(Time.time - start, Is.GreaterThanOrEqualTo(30f), $"30 s of the fight were played ({frames} frames).");
            }
            finally
            {
                encounters.HordeEnemySpawned -= spawned;
            }
            Assert.That(fault, Is.Null, $"After {frames} frames, {spawns} spawns and {reuses} reuses.");
            Assert.That(setup.Run.HasEnded, Is.False, "The hero stood the whole fight.");
            Assert.That(crowd, Is.GreaterThanOrEqualTo(10), "The overrun's horde walked in.");
            Assert.That(reuses, Is.GreaterThan(0), $"Bodies came back from the pool ({spawns} spawns).");
            LogAssert.Expect(LogType.Log, new Regex(@"^\[HordeBodyPoolTests\] heavy horde"));
            Debug.Log($"[HordeBodyPoolTests] heavy horde: {frames} frames, {spawns} spawns, {reuses} reused bodies, a crowd of {crowd}.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(150000)]
        public IEnumerator AShelvedBodyWalksInForAFractionOfANewOne()
        {
            yield return Load();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var encounters = setup.Encounters;
            var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            float deadline = Time.realtimeSinceStartup + 20;
            while (encounters.HordeAliveCount < 2 && Time.realtimeSinceStartup < deadline)
            {
                KeepUp(setup, hero);
                yield return null;
            }
            Assert.That(encounters.HordeAliveCount, Is.GreaterThanOrEqualTo(2), "The horde walks in.");

            // The kind of the first horde enemy standing, by its role on the floor.
            Health first = null;
            for (int i = 0; i < encounters.HordeEnemyCount && first == null; i++)
            {
                Health enemy = encounters.HordeEnemyAt(i);
                if (enemy != null && enemy.IsAlive)
                    first = enemy;
            }
            EnemyDefinition kind = encounters.DefinitionOf(first);
            FloorDefinition floor = encounters.Floor;
            int role = -1;
            for (int r = 0; r < floor.HordeRoleCount && role < 0; r++)
            {
                if (floor.HordeRoleAt(r) == kind)
                    role = r;
            }
            Assert.That(role, Is.GreaterThanOrEqualTo(0));
            MethodInfo spawn = typeof(EncounterController).GetMethod("SpawnHordeEnemy", Private);
            int ordinal = 5000;

            // New bodies: nothing of the kind has left the floor yet. The first call warms the path; the second is counted.
            Assert.That(encounters.ShelvedBodyCountOf(kind), Is.Zero, "Nothing shelved before anything falls.");
            int fresh = int.MaxValue;
            long freshBytes = 0;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var args = new object[] { new HordeDirector.Spawn(role, ordinal++) };
                if ((bool)Measure(encounters, spawn, args, out int allocations, out long bytes) && attempt == 1)
                {
                    fresh = allocations;
                    freshBytes = bytes;
                }
                Assert.That(encounters.ShelvedBodyCountOf(kind), Is.Zero);
            }
            Assert.That(fresh, Is.LessThan(int.MaxValue), "A new body walked in.");

            // Shelved bodies: the kind is felled until its bodies are shelved, and each spawn of it then stands one up. The
            // smallest of up to three is counted, past the first, which may warm the path.
            int pooled = int.MaxValue;
            long pooledBytes = 0;
            int reuses = 0;
            deadline = Time.realtimeSinceStartup + 60;
            while (reuses < 3 && !setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
            {
                KeepUp(setup, hero);
                if (encounters.ShelvedBodyCountOf(kind) > 0)
                {
                    var args = new object[] { new HordeDirector.Spawn(role, ordinal++) };
                    int shelved = encounters.ShelvedBodyCountOf(kind);
                    if ((bool)Measure(encounters, spawn, args, out int allocations, out long bytes))
                    {
                        Assert.That(encounters.ShelvedBodyCountOf(kind), Is.EqualTo(shelved - 1), "The spawn stood a shelved body up.");
                        if (reuses > 0 && allocations < pooled)
                        {
                            pooled = allocations;
                            pooledBytes = bytes;
                        }
                        reuses++;
                    }
                }
                for (int i = 0; i < encounters.HordeEnemyCount; i++)
                {
                    Health enemy = encounters.HordeEnemyAt(i);
                    if (enemy != null && enemy.IsAlive && encounters.DefinitionOf(enemy) == kind)
                        enemy.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
                }
                yield return null;
            }
            Assert.That(reuses, Is.GreaterThanOrEqualTo(2), "Shelved bodies walked in again.");

            // The counts go to the log for the probe's reader; the test ends on LogAssert.NoUnexpectedReceived.
            LogAssert.Expect(LogType.Log, new Regex(@"^\[HordeBodyPoolTests\] a new body"));
            Debug.Log($"[HordeBodyPoolTests] a new body walks in for {fresh} allocations ({freshBytes} bytes), a shelved one for " +
                      $"{pooled} ({pooledBytes} bytes).");
            // Measured in Unity on 2026-11-11, before the record, weapon, name and listener cuts: a new body 80 allocations, a
            // shelved one 50. Those cuts only widen the gap, so the bound is that saving less a margin of 10.
            Assert.That(fresh - pooled, Is.GreaterThanOrEqualTo(MeasuredSaving - SavingMargin),
                $"A shelved body's spawn allocates {pooled} times, a new one's {fresh}: the pool saves less than it did.");
            LogAssert.NoUnexpectedReceived();
        }

        // Resources/Development/Floor_SwarmProof, a finale of one swarm room - the Final Swarm's ladder with more Mites -
        // played on the foundry as the finale is. Without the autopilot nothing strikes but the test.
        private static IEnumerator LoadSwarm(bool autopilot)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_swarm_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.WriteAllText(DevelopmentStart.RunSeedPath, "11");
            if (autopilot)
                File.WriteAllText(DevelopmentStart.AutopilotPath, "on");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            if (!autopilot)
            {
                foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                    attack.enabled = false;
                Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            }
            Time.captureDeltaTime = 1f / 60;
        }

        // Every kind the swarm room's ladder fields is pooled but the Forge Warden, which carries EnrageBehaviour; the
        // Warden is on the ladder, so the exclusion is in play.
        private static EnemyDefinition AssertPlainKinds(RoomDefinition room)
        {
            EnemyDefinition warden = null;
            for (int w = 0; w < room.WaveCount; w++)
            {
                for (int e = 0; e < room.WaveAt(w).EnemyCount; e++)
                {
                    EnemyDefinition kind = room.WaveAt(w).EnemyAt(e);
                    bool isWarden = kind.Id == WardenId;
                    if (isWarden)
                        warden = kind;
                    Assert.That(EncounterController.HasPlainBody(kind.Prefab), Is.EqualTo(!isWarden),
                        $"{kind.Id}'s body is {(isWarden ? "not " : string.Empty)}pooled.");
                }
            }
            Assert.That(warden, Is.Not.Null, "The ladder fields the Forge Warden.");
            return warden;
        }

        // The place's enemy has fallen: killed here if it still stands, so the director refills it in its own time.
        private static void Fell(EncounterController encounters, int place)
        {
            Health enemy = encounters.WaveEnemyAt(place);
            if (enemy != null && enemy.IsAlive)
                enemy.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(enemy != null && !enemy.IsAlive, Is.True, $"Place {place}'s enemy has fallen.");
        }

        private static object[] SwarmSpawn(EncounterController encounters, int place, int wave, int index) =>
            new object[] { new SwarmDirector.Spawn(place, wave, index, encounters.SwarmDifficulty) };

        // The Final Swarm with its bodies reused under it: the autopilot fights the finale's ladder at three times speed for
        // the minute it stays (DevelopmentAutopilot.SwarmStay) and walks out. On every frame every place of the swarm, its
        // pack's motion and the hero's targets by place hold the same body; a standing enemy's body lives on the floor and a
        // fallen one's is its own, still there - never a body shelved or standing up in another place; no body stands in
        // two places; and no exception is logged (EncounterController.SwarmPlacesFault). Each kind's first reused body walks
        // in as that kind's first new one did. Leaving the room takes its shelves with it.
        [UnityTest, Timeout(300000)]
        public IEnumerator TheFinalesSwarmReusesBodiesWithoutLosingAPlace()
        {
            // The autopilot says which cohort it drives as it starts; the test ends on LogAssert.NoUnexpectedReceived.
            LogAssert.Expect(LogType.Log, new Regex(@"^\[Autopilot\] driving the hero"));
            yield return LoadSwarm(true);
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var encounters = setup.Encounters;
            var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            Time.timeScale = 3;
            Assert.That(encounters.IsInSwarmRoom, Is.True, "The proof floor opens on its swarm.");
            AssertPlainKinds(encounters.CurrentRoom);

            // Bodies that walked in before this listened are known, so none of them is taken for a new one if it comes back.
            var seen = new HashSet<Health>();
            for (int i = 0; i < encounters.WaveEnemyCount; i++)
            {
                if (encounters.WaveEnemyAt(i) != null)
                    seen.Add(encounters.WaveEnemyAt(i));
            }
            var fresh = new Dictionary<EnemyDefinition, Walk>();
            var reusedWalks = new List<Walk>();
            var freshWalks = new List<Walk>();
            var compared = new List<EnemyDefinition>();
            int spawns = 0, reuses = 0;
            Action<int> spawned = slot =>
            {
                Health enemy = encounters.WaveEnemyAt(slot);
                EnemyDefinition kind = encounters.DefinitionOf(enemy);
                Walk walk = Walk.Of(enemy, encounters);
                spawns++;
                if (seen.Add(enemy))
                {
                    if (!fresh.ContainsKey(kind))
                        fresh[kind] = walk;
                    return;
                }
                reuses++;
                if (!compared.Contains(kind) && fresh.TryGetValue(kind, out Walk first))
                {
                    compared.Add(kind);
                    reusedWalks.Add(walk);
                    freshWalks.Add(first);
                }
            };
            encounters.SwarmEnemySpawned += spawned;
            string fault = null;
            int frames = 0, places = 0, crowd = 0;
            float start = Time.time;
            try
            {
                float deadline = Time.realtimeSinceStartup + 240;
                while (!setup.Run.HasEnded && Time.time - start < SwarmTestSeconds && fault == null &&
                       Time.realtimeSinceStartup < deadline)
                {
                    KeepUp(setup, hero);
                    // A choice's pause gives time back at one times speed as it closes (CombatSetup.ApplyPause).
                    if (!setup.Choices.IsOpen && Time.timeScale > 0f)
                        Time.timeScale = 3;
                    yield return null;
                    frames++;
                    places = Mathf.Max(places, encounters.WaveEnemyCount);
                    crowd = Mathf.Max(crowd, encounters.AliveEnemyCount);
                    fault = encounters.SwarmPlacesFault();
                }
            }
            finally
            {
                encounters.SwarmEnemySpawned -= spawned;
            }
            Assert.That(fault, Is.Null, $"After {frames} frames, {spawns} spawns and {reuses} reuses.");
            Assert.That(setup.Run.HasEnded, Is.True,
                $"The autopilot walked out of the swarm ({Time.time - start:0.0} s, {frames} frames).");
            Assert.That(setup.Run.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(places, Is.GreaterThanOrEqualTo(5), "The ladder's crowd walked in.");
            Assert.That(reuses, Is.GreaterThan(0), $"Bodies came back from the pool ({spawns} spawns).");
            Assert.That(reusedWalks.Count, Is.GreaterThan(0), "A reused body walked in where a new one of its kind had.");
            for (int i = 0; i < reusedWalks.Count; i++)
                reusedWalks[i].Matches(freshWalks[i]);

            // The room's shelves went with it (the proof floor has no horde to keep its kinds), and the bodies that left the
            // floor in its last frame are destroyed as they would have been shelved.
            Assert.That(encounters.ShelvedBodyCount, Is.Zero, "Leaving the swarm trimmed its shelves.");
            yield return null;
            yield return null;
            Assert.That(encounters.LeavingBodyCount, Is.Zero, "Nothing waits to be shelved after the swarm.");
            Assert.That(encounters.ShelvedBodyCount, Is.Zero);

            LogAssert.Expect(LogType.Log, new Regex(@"^\[HordeBodyPoolTests\] finale swarm"));
            Debug.Log($"[HordeBodyPoolTests] finale swarm: {frames} frames, {spawns} spawns, {reuses} reused bodies, " +
                      $"{places} places, a crowd of {crowd}, {reusedWalks.Count} kinds compared.");
            LogAssert.NoUnexpectedReceived();
        }

        // A swarm refill that stands a shelved body up allocates a fraction of one that instantiates. The proof's first
        // place, a Cinder Mite's, is felled and refilled by hand - SpawnSwarmEnemy, as the director's refill calls it -
        // first while nothing of the kind is shelved, then once the refills' fallen bodies are.
        [UnityTest, Timeout(150000)]
        public IEnumerator AShelvedSwarmBodyWalksInForAFractionOfANewOne()
        {
            yield return LoadSwarm(false);
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var encounters = setup.Encounters;
            var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            Assert.That(encounters.IsInSwarmRoom, Is.True, "The proof floor opens on its swarm.");
            const int place = 0;
            EnemyDefinition kind = encounters.CurrentRoom.WaveAt(0).EnemyAt(0);
            Assert.That(encounters.DefinitionOf(encounters.WaveEnemyAt(place)), Is.SameAs(kind));
            Assert.That(EncounterController.HasPlainBody(kind.Prefab), Is.True);
            MethodInfo spawn = typeof(EncounterController).GetMethod("SpawnSwarmEnemy", Private);

            // New bodies: nothing has left the floor yet, and what the refills retire waits out this frame. The first
            // refill warms the path; the second is counted. Each newcomer falls at once, so the director, told of the
            // fall, refills the place in its own time from a fallen body as it always does.
            Assert.That(encounters.ShelvedBodyCountOf(kind), Is.Zero, "Nothing shelved before anything falls.");
            int fresh = int.MaxValue;
            long freshBytes = 0;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Fell(encounters, place);
                Measure(encounters, spawn, SwarmSpawn(encounters, place, 0, 0), out int allocations, out long bytes);
                Assert.That(encounters.WaveEnemyAt(place).IsAlive, Is.True, "A newcomer stands in the place.");
                Assert.That(encounters.ShelvedBodyCountOf(kind), Is.Zero);
                if (attempt == 1)
                {
                    fresh = allocations;
                    freshBytes = bytes;
                }
                Fell(encounters, place);
            }

            // Shelved bodies: a frame on, the retired ones are shelved, and each refill stands one up. The smallest of up to
            // three is counted, past the first, which may warm the path.
            int pooled = int.MaxValue;
            long pooledBytes = 0;
            int reuses = 0;
            float deadline = Time.realtimeSinceStartup + 60;
            while (reuses < 3 && !setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
            {
                KeepUp(setup, hero);
                yield return null;
                int shelved = encounters.ShelvedBodyCountOf(kind);
                if (shelved == 0 || setup.Choices.IsOpen)
                    continue;
                Fell(encounters, place);
                Measure(encounters, spawn, SwarmSpawn(encounters, place, 0, 0), out int allocations, out long bytes);
                Assert.That(encounters.ShelvedBodyCountOf(kind), Is.EqualTo(shelved - 1), "The refill stood a shelved body up.");
                Assert.That(encounters.WaveEnemyAt(place).IsAlive, Is.True);
                if (reuses > 0 && allocations < pooled)
                {
                    pooled = allocations;
                    pooledBytes = bytes;
                }
                reuses++;
                Fell(encounters, place);
            }
            Assert.That(reuses, Is.GreaterThanOrEqualTo(2), "Shelved bodies walked in again.");
            Assert.That(encounters.SwarmPlacesFault(), Is.Null);

            // The counts go to the log for the probe's reader; the test ends on LogAssert.NoUnexpectedReceived.
            LogAssert.Expect(LogType.Log, new Regex(@"^\[HordeBodyPoolTests\] a new swarm body"));
            Debug.Log($"[HordeBodyPoolTests] a new swarm body walks in for {fresh} allocations ({freshBytes} bytes), a " +
                      $"shelved one for {pooled} ({pooledBytes} bytes).");
            // The swarm's two refills differ only by the body: Instantiate, Initialize and a new weapon, or ReuseBody (Revive,
            // Rearm restoring the weapon, the views reset). The place's name, its record and the record's closure, the entry
            // and the pack's place are made the same both ways. The horde's spawns differed by that body and no more when
            // Unity measured them on 2026-11-11 (a new body 80 allocations, a shelved one 50: its reuse still made a weapon,
            // a record, a closure and a name, as the new one did), so the body alone saves at least the horde's measured 30,
            // and the swarm's reuse, which makes no weapon, more. The bound is the horde's: that saving less a margin of 10.
            Assert.That(fresh - pooled, Is.GreaterThanOrEqualTo(MeasuredSaving - SavingMargin),
                $"A shelved swarm body's refill allocates {pooled} times, a new one's {fresh}: the pool saves less than the horde's.");
            LogAssert.NoUnexpectedReceived();
        }

        // The Forge Warden walks the finale's ladder as a plain swarm enemy - no gate boss, no elite - but its prefab
        // carries EnrageBehaviour, which the pool does not reset: its body is destroyed as its place fills again, never
        // shelved, while a Mite's beside it is.
        [UnityTest, Timeout(150000)]
        public IEnumerator TheSwarmsWardenIsNeverShelved()
        {
            yield return LoadSwarm(false);
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var encounters = setup.Encounters;
            Assert.That(encounters.IsInSwarmRoom, Is.True, "The proof floor opens on its swarm.");
            RoomDefinition room = encounters.CurrentRoom;
            EnemyDefinition wardenKind = AssertPlainKinds(room);
            int wardenWave = -1, wardenIndex = -1;
            for (int w = 0; w < room.WaveCount && wardenWave < 0; w++)
            {
                for (int e = 0; e < room.WaveAt(w).EnemyCount && wardenWave < 0; e++)
                {
                    if (room.WaveAt(w).EnemyAt(e) == wardenKind)
                    {
                        wardenWave = w;
                        wardenIndex = e;
                    }
                }
            }
            const int place = 0;
            EnemyDefinition mite = room.WaveAt(0).EnemyAt(0);
            Health opening = encounters.WaveEnemyAt(place);
            Assert.That(encounters.DefinitionOf(opening), Is.SameAs(mite));
            MethodInfo spawn = typeof(EncounterController).GetMethod("SpawnSwarmEnemy", Private);

            // The Warden walks into the opening Mite's place, which leaves for the shelf.
            Fell(encounters, place);
            spawn.Invoke(encounters, SwarmSpawn(encounters, place, wardenWave, wardenIndex));
            Health warden = encounters.WaveEnemyAt(place);
            Assert.That(encounters.DefinitionOf(warden), Is.SameAs(wardenKind));
            Assert.That(warden.GetComponent<EnrageBehaviour>(), Is.Not.Null);
            Assert.That(encounters.GateBoss, Is.Null, "The swarm's Warden is no gate boss.");
            Assert.That(encounters.Elites, Is.Empty, "Nor an elite.");

            // It falls, and a Mite takes its place: its body leaves the floor.
            Fell(encounters, place);
            spawn.Invoke(encounters, SwarmSpawn(encounters, place, 0, 0));
            Assert.That(warden.gameObject.activeSelf, Is.False, "The Warden's body left the floor.");
            Fell(encounters, place);
            yield return null;
            yield return null;
            Assert.That(warden == null, Is.True, "The Warden's body was destroyed, not kept.");
            Assert.That(encounters.ShelvedBodyCountOf(wardenKind), Is.Zero, "No Warden is shelved.");
            Assert.That(opening == null, Is.False, "The opening Mite's body was kept.");
            Assert.That(encounters.ShelvedBodyCountOf(mite), Is.GreaterThan(0), "And shelved, in the swarm room.");
            Assert.That(encounters.SwarmPlacesFault(), Is.Null);
            LogAssert.NoUnexpectedReceived();
        }

        // A strike's StruckWith lands after its TryAttack returns, when the strike's own kill may have released the body it
        // struck (the gate opening on the Warden's fall): a burn put on that body would ride the shelf into its next life,
        // past Revive's cleared Died. A body off the floor takes no burn (the pool's review, 2026-11-11).
        [Test]
        public void ABodyOffTheFloorTakesNoBurn()
        {
            Health body = new GameObject("Burn on a body off the floor").AddComponent<Health>();
            try
            {
                body.Initialize(10f);
                body.gameObject.SetActive(false);
                Assert.That(BurnStatus.On(body), Is.Null, "A body off the floor takes no burn.");
                Assert.That(body.GetComponent<BurnStatus>(), Is.Null, "Nothing was added to it.");
                body.gameObject.SetActive(true);
                Assert.That(BurnStatus.On(body), Is.Not.Null, "Back on the floor it burns again.");
            }
            finally
            {
                Object.Destroy(body.gameObject);
            }
        }
    }
}
