using System.Reflection;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEditor;

namespace Cryptforge.Tests
{
    // The foundry floor of the Descent simulation (DescentSimulation.RunFoundry) reads values that live on the scene's
    // MonoBehaviours and assets, which the pure .NET runner cannot compile or load. Each is copied there as a constant;
    // these hold every copy to its source in the Editor, so a change to the game that the simulation has not followed
    // fails here rather than drifting the regression harness silently (docs/21, the targeting divergence).
    public sealed class FoundrySimulationSceneValueTests
    {
        [Test]
        public void TheSceneConstantsAreTheSimulations()
        {
            Assert.That(DescentSimulation.FoundryPace, Is.EqualTo(HeroMovementInput.FoundryPace));
            Assert.That(DescentSimulation.GateMarchFactor, Is.EqualTo(EncounterController.GateMarchFactor));
            Assert.That(DescentSimulation.GateMarchRange, Is.EqualTo(EncounterController.GateMarchRange));
            Assert.That(DescentSimulation.RevealTurnSeconds, Is.EqualTo(BossRevealView.TurnSeconds));
            Assert.That(DescentSimulation.RevealHoldSeconds, Is.EqualTo(BossRevealView.HoldSeconds));
        }

        // DevelopmentAutopilot keeps its policy private; read it the way the runtime holds it.
        [Test]
        public void TheAutopilotsPolicyIsTheSimulations()
        {
            Assert.That(Autopilot<float>("LureDepth"), Is.EqualTo(DescentSimulation.PilotLureDepth));
            Assert.That(Autopilot<float>("ShelterDepth"), Is.EqualTo(DescentSimulation.PilotShelterDepth));
            Assert.That(Autopilot<float>("Arrived"), Is.EqualTo(DescentSimulation.PilotArrived));
            Assert.That(Autopilot<float>("DropDetour"), Is.EqualTo(DescentSimulation.PilotDropDetour));
            Assert.That(Autopilot<float>("DetourSlack"), Is.EqualTo(DescentSimulation.PilotDetourSlack));
            Assert.That(Autopilot<float>("DetourBudget"), Is.EqualTo(DescentSimulation.PilotDetourBudget));
            Assert.That(Autopilot<float>("KiteReach"), Is.EqualTo(DescentSimulation.PilotKiteReach));
            Assert.That(Autopilot<float>("KiteTurn"), Is.EqualTo(DescentSimulation.PilotKiteTurn));
            Assert.That(Autopilot<float>("KiteStride"), Is.EqualTo(DescentSimulation.PilotKiteStride));
            Assert.That(Autopilot<int>("AbilityCrowd"), Is.EqualTo(DescentSimulation.PilotAbilityCrowd));
        }

        // Who the scene's autopilot plays as comes from development/autopilot.txt's words (AutopilotCohort.FromSwitch, in
        // DevelopmentAutopilot.Initialize), who the simulation's plays as from its levers; the habits are one body, so what
        // must agree is the reading: the careful pilot for a line naming no cohort ("on", as PerfProbe and
        // HordePacingMeasurement write it) and for the simulation's default levers, and each cohort for its name after a
        // mode ("<mode> <cohort>", as HordePacingMeasurement writes TestResults/horde-cohort.txt).
        [Test]
        public void TheAutopilotsCohortsAreTheSimulations()
        {
            Assert.That(AutopilotCohort.FromSwitch("on"), Is.SameAs(new DescentSimulation.FoundryLevers().Cohort));
            Assert.That(AutopilotCohort.FromSwitch("on"), Is.SameAs(AutopilotCohort.Careful));
            foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
            {
                Assert.That(AutopilotCohort.FromSwitch("on " + cohort.Name), Is.SameAs(cohort), cohort.Name);
                Assert.That(AutopilotCohort.FromSwitch("linger " + cohort.Name), Is.SameAs(cohort), cohort.Name);
            }
        }

        private static T Autopilot<T>(string name)
        {
            FieldInfo field = typeof(DevelopmentAutopilot).GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"DevelopmentAutopilot.{name}");
            return (T)field.GetValue(null);
        }

        // The scene's script order, as the simulation steps a frame (DescentSimulation.Foundry's header): the hero walks,
        // the encounter moves the packs and brings the gate boss out, the Warden's slam clock marks and holds the gate
        // boss's weapon, and only then the default-order scripts - every AttackController, the hero's ability, the burns,
        // the altars, the pit, the chest, the autopilot. While WardenSlamService had no order of its own Unity ran it before
        // or after the attackers as it pleased, and the Warden could swing on the frame its mark went down.
        [Test]
        public void TheScenesScriptOrderIsTheSimulations()
        {
            int walk = ScriptOrder<HeroMovementInput>("UI/HeroMovementInput.cs");
            int encounter = ScriptOrder<EncounterController>("Combat/EncounterController.cs");
            // The Glass Regent's lanes are marked and judged on the Warden's clock, in WardenSlamService (docs/50 section 6),
            // as the simulation marks them in StepWarden.
            int slams = ScriptOrder<WardenSlamService>("Core/WardenSlamService.cs");
            // The Mirror Sentries' guards step right after the Warden's slam clock, as DescentSimulation.Foundry's frame
            // steps StepVaults after StepWarden, and before every strike lands on them.
            int mirrors = ScriptOrder<MirrorGuardService>("Core/MirrorGuardService.cs");
            // The Cinder Blooms' clocks step right after the mirrors, as the simulation's frame steps StepGloam after
            // StepVaults, and before every strike: an open Bloom's maul is held and a landing meets the bark as the clock stands.
            int gloam = ScriptOrder<GloamService>("Core/GloamService.cs");
            Assert.That(walk, Is.LessThan(encounter), "the hero walks before the packs move");
            Assert.That(encounter, Is.LessThan(slams), "the gate boss is out before its clock reads it");
            Assert.That(slams, Is.LessThan(mirrors), "the mirrors turn after the Warden's clock, as the simulation steps them");
            Assert.That(mirrors, Is.LessThan(gloam), "the Blooms' clocks after the mirrors, as the simulation steps them");
            var after = new (string Name, int Order)[]
            {
                (nameof(AttackController), ScriptOrder<AttackController>("Combat/AttackController.cs")),
                (nameof(AbilityController), ScriptOrder<AbilityController>("Combat/AbilityController.cs")),
                (nameof(BurnStatus), ScriptOrder<BurnStatus>("Combat/BurnStatus.cs")),
                (nameof(AltarService), ScriptOrder<AltarService>("Core/AltarService.cs")),
                (nameof(PitEruptionService), ScriptOrder<PitEruptionService>("Core/PitEruptionService.cs")),
                (nameof(ChestSpawner), ScriptOrder<ChestSpawner>("Combat/ChestSpawner.cs")),
                (nameof(DevelopmentAutopilot), ScriptOrder<DevelopmentAutopilot>("Core/DevelopmentAutopilot.cs"))
            };
            foreach ((string name, int order) in after)
            {
                Assert.That(order, Is.GreaterThan(slams), name + " runs after the Warden's slam clock");
                Assert.That(order, Is.GreaterThan(mirrors), name + " runs after the mirrors' guards");
                Assert.That(order, Is.GreaterThan(gloam), name + " runs after the Blooms' clocks");
            }
        }

        // The Mirror Sentry (MirrorGuardRule; the lead's decisions 2026-11-12, `sentry-t45-h125-c2-f50`): the scene's Sentry
        // carries the guard on its prefab, ahead of its Health, with MirrorGuardRule's tuning, and the simulation's shipped
        // levers tune its Sentry with the same constants and flank it as DevelopmentAutopilot does; the asset is the
        // simulation's (AssertEnemy) and its proof floor fields it alone.
        [Test]
        public void TheMirrorSentryIsTheSimulations()
        {
            var sentry = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_MirrorSentry.asset");
            Assert.That(sentry, Is.Not.Null);
            AssertEnemy(sentry, DescentSimulation.MirrorSentry, sentry.Id);
            Assert.That(sentry.Id, Is.EqualTo("enemy_mirror_sentry"));
            Assert.That(sentry.Weapon.Id, Is.EqualTo("weapon_mirror_bash"));
            Assert.That(sentry.Weapon.DisplayName, Is.EqualTo("Mirror Bash"));
            Assert.That(sentry.MaximumHealth, Is.EqualTo(160f * 1.25f), "125 % of docs/50's 160");
            var guard = sentry.Prefab.GetComponent<MirrorGuard>();
            Assert.That(guard, Is.Not.Null, "the Sentry's prefab carries its mirror");
            Assert.That(guard.State.ArcDegrees, Is.EqualTo(MirrorGuardRule.ArcDegrees));
            var captain = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_GruntCaptain.asset");
            Assert.That(captain.Prefab.GetComponent<MirrorGuard>(), Is.Null, "the Captain has none");
            // Its own Blender-rendered set, which has no frosted copy: the set is its own floor-2 look (docs/50 section 2).
            var look = sentry.Prefab.GetComponent<EnemyLookView>();
            Assert.That(look, Is.Not.Null);
            var art = new SerializedObject(look).FindProperty("_paintedArt").objectReferenceValue as Cryptforge.Art.EnemyArtSet;
            Assert.That(art, Is.Not.Null, "painted");
            Assert.That(art.name, Is.EqualTo("MirrorSentryArtSet"));
            Assert.That(art.IsValid, Is.True);
            Assert.That(art.Frosted, Is.Null);

            var shipped = new DescentSimulation.FoundryLevers();
            Assert.That(shipped.SentryTurnDegrees, Is.EqualTo(MirrorGuardRule.TurnDegrees));
            Assert.That(shipped.SentryArcDegrees, Is.EqualTo(MirrorGuardRule.ArcDegrees));
            Assert.That(shipped.SentryFrontShare, Is.EqualTo(MirrorGuardRule.FrontShare));
            Assert.That(shipped.SentryReflectShare, Is.EqualTo(MirrorGuardRule.ReflectShare));
            Assert.That(shipped.SentryReflectCap, Is.EqualTo(MirrorGuardRule.ReflectCap));
            Assert.That(shipped.SentryHealthScale, Is.EqualTo(1f));
            Assert.That(shipped.FlanksTheSentry, Is.True, "the simulation's pilot flanks it as DevelopmentAutopilot does");
            Assert.That(shipped.VaultsCaptain, Is.False);

            FloorDefinition proof = null;
            foreach (FloorDefinition floor in UnityEngine.Resources.LoadAll<FloorDefinition>(DevelopmentStart.ResourcesFolder))
                if (floor.Id == "floor_sentry_proof")
                    proof = floor;
            Assert.That(proof, Is.Not.Null, "Resources/Development/Floor_SentryProof");
            Assert.That(proof.RoomCount, Is.EqualTo(1));
            Assert.That(proof.RoomAt(0).Kind, Is.EqualTo(RoomKind.Elite));
            Assert.That(proof.RoomAt(0).WaveAt(0).EnemyCount, Is.EqualTo(1));
            Assert.That(proof.RoomAt(0).WaveAt(0).EnemyAt(0), Is.SameAs(sentry));
        }

        // The Cinder Bloom (BloomRule; docs/52, the lead's decisions 2026-11-15, `bloom-m20-h70-r1.2`): the scene's Bloom carries
        // its bark on its prefab, ahead of its Health, and its open-state look beside its painted set, which has no gloam copy
        // (born on floor 3: the set is its own look); the asset and its Bark Maul are the simulation's (AssertEnemy) - 20 % of
        // docs/52's maul at reach 1.2 - the simulation's shipped levers cycle it with BloomRule's tuning, and its proof floor
        // fields it alone in an elite room.
        [Test]
        public void TheCinderBloomIsTheSimulations()
        {
            var bloom = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_CinderBloom.asset");
            Assert.That(bloom, Is.Not.Null);
            AssertEnemy(bloom, DescentSimulation.CinderBloom, bloom.Id);
            Assert.That(bloom.Id, Is.EqualTo("enemy_cinder_bloom"));
            Assert.That(bloom.Weapon.Id, Is.EqualTo("weapon_bark_maul"));
            Assert.That(bloom.Weapon.DisplayName, Is.EqualTo("Bark Maul"));
            Assert.That(bloom.Weapon.Damage, Is.EqualTo(DescentSimulation.Docs52MaulDamage * .2f), "20 % of docs/52's 10.8");
            Assert.That(bloom.Weapon.Range, Is.EqualTo(1.2f), "reach 1.2, reach 1.0's fallback for its look");
            Assert.That(bloom.MaximumHealth, Is.EqualTo(196f), "70 % of docs/52's 280");
            Assert.That(bloom.Prefab.GetComponent<CinderBloom>(), Is.Not.Null, "the Bloom's prefab carries its bark");
            Assert.That(bloom.Prefab.GetComponent<CinderBloomView>(), Is.Not.Null, "and its open-state look");
            var captain = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_GruntCaptain.asset");
            Assert.That(captain.Prefab.GetComponent<CinderBloom>(), Is.Null, "the Captain has none");
            var look = bloom.Prefab.GetComponent<EnemyLookView>();
            Assert.That(look, Is.Not.Null);
            var art = new SerializedObject(look).FindProperty("_paintedArt").objectReferenceValue as Cryptforge.Art.EnemyArtSet;
            Assert.That(art, Is.Not.Null, "painted");
            Assert.That(art.name, Is.EqualTo("CinderBloomArtSet"));
            Assert.That(art.IsValid, Is.True);
            Assert.That(art.ForTheme(FloorTheme.Gloam), Is.Null, "no gloam copy: the set is its own look");
            var view = new SerializedObject(bloom.Prefab.GetComponent<CinderBloomView>());
            foreach (string side in new[] { "_front", "_rear" })
            {
                SerializedProperty frames = view.FindProperty(side);
                Assert.That(frames.arraySize, Is.EqualTo(3), side + ": unfold-0, unfold-1, open");
                for (int i = 0; i < 3; i++)
                {
                    Assert.That(frames.GetArrayElementAtIndex(i).FindPropertyRelative("Body").objectReferenceValue, Is.Not.Null, $"{side} {i}");
                    Assert.That(frames.GetArrayElementAtIndex(i).FindPropertyRelative("Flash").objectReferenceValue, Is.Not.Null, $"{side} {i} flash");
                }
            }
            Assert.That(view.FindProperty("_barkChip").arraySize, Is.EqualTo(3));

            var shipped = new DescentSimulation.FoundryLevers();
            Assert.That(shipped.BloomClosed, Is.EqualTo(BloomRule.ClosedSeconds));
            Assert.That(shipped.BloomOpen, Is.EqualTo(BloomRule.OpenSeconds));
            Assert.That(shipped.BloomBark, Is.EqualTo(BloomRule.BarkShare));
            Assert.That(shipped.BloomWindup, Is.EqualTo(0f), "the shipped maul has no windup lever");
            Assert.That(shipped.GloamCaptain, Is.False);
            Assert.That(shipped.Gloam, Is.False, "the shipped levers leave Thorn Post as the asset has it");

            FloorDefinition proof = null;
            foreach (FloorDefinition floor in UnityEngine.Resources.LoadAll<FloorDefinition>(DevelopmentStart.ResourcesFolder))
                if (floor.Id == "floor_bloom_proof")
                    proof = floor;
            Assert.That(proof, Is.Not.Null, "Resources/Development/Floor_BloomProof");
            Assert.That(proof.RoomCount, Is.EqualTo(1));
            Assert.That(proof.RoomAt(0).Kind, Is.EqualTo(RoomKind.Elite));
            Assert.That(proof.RoomAt(0).WaveAt(0).EnemyCount, Is.EqualTo(1));
            Assert.That(proof.RoomAt(0).WaveAt(0).EnemyAt(0), Is.SameAs(bloom));
        }

        private const string PollenWraithId = "enemy_pollen_wraith";
        private const string CinderMiteId = "enemy_cinder_mite";

        // The Pollen Wraith (docs/52 section 3; the lead's decision 2026-11-17): the Cinder Mite reskinned, with no pollen. Its
        // asset is the Mite's - health, speed, the Mite Bite itself, gold and experience - under its own id and name, and the
        // simulation plays it as the Mite (DescentSimulation.PollenWraith). Its prefab is a variant of the Grunt's as the
        // Mite's is, the Mite's body in every component and value but the painted set: PollenWraithArtSet, which has no
        // themed copy (born on floor 3, the set is its own look), where the Mite keeps CinderMiteArtSet. It stands on floor 3
        // alone: no other floor in the project names it.
        [Test]
        public void ThePollenWraithIsTheCinderMiteReskinned()
        {
            var wraith = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_PollenWraith.asset");
            var mite = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_CinderMite.asset");
            Assert.That(wraith, Is.Not.Null);
            Assert.That(mite, Is.Not.Null);
            Assert.That(wraith.Id, Is.EqualTo(PollenWraithId));
            Assert.That(wraith.DisplayName, Is.EqualTo("Pollen Wraith"));
            Assert.That(mite.Id, Is.EqualTo(CinderMiteId));
            Assert.That((wraith.MaximumHealth, wraith.MoveSpeed, wraith.GoldReward, wraith.ExperienceReward),
                Is.EqualTo((mite.MaximumHealth, mite.MoveSpeed, mite.GoldReward, mite.ExperienceReward)), "the Mite's numbers");
            Assert.That(wraith.Weapon, Is.SameAs(mite.Weapon), "the Mite Bite itself");
            Assert.That(DescentSimulation.PollenWraith, Is.SameAs(DescentSimulation.Mite), "the simulation plays it as the Mite");
            AssertEnemy(wraith, DescentSimulation.PollenWraith, wraith.Id, true);
            AssertEnemy(mite, DescentSimulation.Mite, mite.Id);

            // The prefab: a Grunt variant, as the Mite's is, the Mite's body but for its painted set.
            Assert.That(wraith.Prefab, Is.Not.Null, "the definition finds the variant's Health");
            Assert.That(wraith.Prefab, Is.Not.SameAs(mite.Prefab));
            UnityEngine.GameObject body = wraith.Prefab.gameObject, miteBody = mite.Prefab.gameObject;
            Assert.That(body.name, Is.EqualTo("PollenWraith"));
            Assert.That(PrefabUtility.GetPrefabAssetType(body), Is.EqualTo(PrefabAssetType.Variant));
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(body), Is.Not.Null);
            Assert.That(PrefabUtility.GetCorrespondingObjectFromSource(body),
                Is.SameAs(PrefabUtility.GetCorrespondingObjectFromSource(miteBody)), "a variant of the Mite's own base, the Grunt");
            AssertSameBody(miteBody, body, "_paintedArt");
            var look = body.GetComponent<EnemyLookView>();
            Assert.That(look, Is.Not.Null);
            Assert.That(look.Look, Is.EqualTo(Cryptforge.Art.EnemyLook.Mite), "the Mite's look: its compact bar");
            var art = new SerializedObject(look).FindProperty("_paintedArt").objectReferenceValue as Cryptforge.Art.EnemyArtSet;
            Assert.That(art, Is.SameAs(AssetDatabase.LoadAssetAtPath<Cryptforge.Art.EnemyArtSet>(
                "Assets/_Project/Art/Characters/PollenWraith/PollenWraithArtSet.asset")));
            Assert.That(art.IsValid, Is.True);
            foreach (FloorTheme theme in new[] { FloorTheme.Foundry, FloorTheme.Ice, FloorTheme.Gloam })
                Assert.That(art.ForTheme(theme), Is.Null, theme + ": the set is its own look");
            var miteArt = new SerializedObject(miteBody.GetComponent<EnemyLookView>()).FindProperty("_paintedArt").objectReferenceValue;
            Assert.That(miteArt, Is.SameAs(AssetDatabase.LoadAssetAtPath<Cryptforge.Art.EnemyArtSet>(
                "Assets/_Project/Art/Characters/CinderMite/CinderMiteArtSet.asset")), "the Mite keeps its own");

            // Floor 3 alone names it.
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(FloorDefinition), new[] { "Assets/_Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var floor = AssetDatabase.LoadAssetAtPath<FloorDefinition>(path);
                if (floor.Id == "floor_gloam_gardens")
                    continue;
                for (int role = 0; role < floor.HordeRoleCount; role++)
                    Assert.That(floor.HordeRoleAt(role), Is.Not.SameAs(wraith), $"{path} horde role {role}");
                for (int room = 0; room < floor.RoomCount; room++)
                    Assert.That(Count(floor.RoomAt(room), PollenWraithId), Is.Zero, $"{path} room {room}");
            }
        }

        // The enemies of a room's waves with the given id.
        private static int Count(RoomDefinition room, string id)
        {
            int count = 0;
            if (room == null)
                return count;
            for (int wave = 0; wave < room.WaveCount; wave++)
                for (int e = 0; e < room.WaveAt(wave).EnemyCount; e++)
                    if (room.WaveAt(wave).EnemyAt(e) != null && room.WaveAt(wave).EnemyAt(e).Id == id)
                        count++;
            return count;
        }

        // Two prefab bodies alike component for component and value for value, but the named fields: the same components
        // in the same places, each visible serialized value equal, a reference to a part of the body naming the same part
        // (the bodies' roots differ), any other reference the same asset.
        private static void AssertSameBody(UnityEngine.GameObject expected, UnityEngine.GameObject actual, params string[] except)
        {
            UnityEngine.Component[] a = expected.GetComponentsInChildren<UnityEngine.Component>(true);
            UnityEngine.Component[] b = actual.GetComponentsInChildren<UnityEngine.Component>(true);
            Assert.That(b.Length, Is.EqualTo(a.Length), actual.name + ": its components");
            for (int i = 0; i < a.Length; i++)
            {
                string where = actual.name + ": " + PartOf(a[i], expected.transform);
                Assert.That(PartOf(b[i], actual.transform), Is.EqualTo(PartOf(a[i], expected.transform)), where);
                SerializedProperty pa = new SerializedObject(a[i]).GetIterator(), pb = new SerializedObject(b[i]).GetIterator();
                int compared = 0;
                while (pa.NextVisible(true))
                {
                    Assert.That(pb.NextVisible(true), Is.True, where + ": " + pa.propertyPath);
                    Assert.That(pb.propertyPath, Is.EqualTo(pa.propertyPath), where);
                    if (System.Array.IndexOf(except, pa.propertyPath) >= 0 || pa.propertyType == SerializedPropertyType.Generic)
                        continue;
                    compared++;
                    if (pa.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        UnityEngine.Object ra = pa.objectReferenceValue, rb = pb.objectReferenceValue;
                        if (ra is UnityEngine.Component || ra is UnityEngine.GameObject)
                            Assert.That(PartOf(rb, actual.transform), Is.EqualTo(PartOf(ra, expected.transform)), where + ": " + pa.propertyPath);
                        else
                            Assert.That(rb, Is.SameAs(ra), where + ": " + pa.propertyPath);
                        continue;
                    }
                    Assert.That(SerializedProperty.DataEquals(pa, pb), Is.True, where + ": " + pa.propertyPath);
                }
                Assert.That(pb.NextVisible(true), Is.False, where + ": nothing more");
                Assert.That(compared, Is.GreaterThan(0), where);
            }
        }

        // A component or game object of a body by its place under the body's root and its type ("Enemy Body:SpriteRenderer").
        private static string PartOf(UnityEngine.Object part, UnityEngine.Transform root)
        {
            UnityEngine.Transform at = part is UnityEngine.Component c ? c.transform : part is UnityEngine.GameObject g ? g.transform : null;
            if (at == null)
                return "not a part: " + (part != null ? part.name : "none");
            string path = string.Empty;
            for (; at != null && at != root; at = at.parent)
                path = path.Length == 0 ? at.name : at.name + "/" + path;
            Assert.That(at, Is.SameAs(root), "a part of the body itself");
            return path + ":" + part.GetType().Name;
        }

        // The Glass Regent (RegentRule; the lead's decisions 2026-11-12): the scene's Regent is the Forge Warden's prefab with
        // a GlassRegent - its lanes' sprites on it - and its own Blender-rendered set, and the simulation's shipped levers mark
        // its lanes with RegentRule's tuning on its beat and sidestep them as DevelopmentAutopilot does; the asset is the
        // simulation's (AssertEnemy) and its proof floor fields it alone at the gate.
        [Test]
        public void TheGlassRegentIsTheSimulations()
        {
            var regent = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_GlassRegent.asset");
            Assert.That(regent, Is.Not.Null);
            AssertEnemy(regent, DescentSimulation.GlassRegent, regent.Id);
            Assert.That(regent.Id, Is.EqualTo("enemy_glass_regent"));
            Assert.That(regent.Weapon.Id, Is.EqualTo("weapon_glass_rod"));
            Assert.That(regent.Weapon.DisplayName, Is.EqualTo("Glass Rod"));
            var lanes = regent.Prefab.GetComponent<GlassRegent>();
            Assert.That(lanes, Is.Not.Null, "the Regent's prefab marks lanes");
            Assert.That(lanes.LaneEdge, Is.Not.Null, "the lane's edge sprite");
            Assert.That(lanes.LaneFill, Is.Not.Null, "the lane's fill sprite");
            Assert.That(lanes.LaneEdge.border, Is.Not.EqualTo(UnityEngine.Vector4.zero), "9-sliced");
            Assert.That(regent.Prefab.GetComponent<EnrageBehaviour>(), Is.Not.Null, "its phase two, as the Warden's");
            var warden = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/Enemy_ForgeWarden.asset");
            Assert.That(warden.Prefab.GetComponent<GlassRegent>(), Is.Null, "the Warden marks circles");
            Assert.That(DescentSimulation.GlassRegent.EnrageAt, Is.EqualTo(DescentSimulation.Warden.EnrageAt));
            // Its own Blender-rendered set, which has no frosted copy: the set is its own floor-2 look (docs/50 section 2).
            var look = regent.Prefab.GetComponent<EnemyLookView>();
            Assert.That(look, Is.Not.Null);
            var art = new SerializedObject(look).FindProperty("_paintedArt").objectReferenceValue as Cryptforge.Art.EnemyArtSet;
            Assert.That(art, Is.Not.Null, "painted");
            Assert.That(art.name, Is.EqualTo("GlassRegentArtSet"));
            Assert.That(art.IsValid, Is.True);
            Assert.That(art.Frosted, Is.Null);
            // Its glass copies' look, held for section 6's copies (the shipped Regent casts none: RegentCopies 0 below).
            Assert.That(lanes.CopyArt, Is.Not.Null, "the copies' set");
            Assert.That(lanes.CopyArt.name, Is.EqualTo("GlassRegentCopyArtSet"));
            Assert.That(lanes.CopyArt, Is.Not.SameAs(art));
            Assert.That(lanes.CopyArt.Frosted, Is.Null);

            var shipped = new DescentSimulation.FoundryLevers();
            Assert.That(shipped.RegentLaneWidth, Is.EqualTo(RegentRule.LaneWidth));
            Assert.That(shipped.RegentLaneLength, Is.EqualTo(RegentRule.LaneLength));
            Assert.That(shipped.RegentLaneShare, Is.EqualTo(RegentRule.LaneShare));
            Assert.That(shipped.RegentEvery, Is.EqualTo(RegentRule.Every));
            Assert.That(shipped.RegentEveryPhaseTwo, Is.EqualTo(RegentRule.EveryPhaseTwo));
            Assert.That(shipped.RegentTelegraph, Is.EqualTo(RegentRule.Telegraph));
            Assert.That(shipped.RegentCopies, Is.Zero, "stage 1: no copies");
            Assert.That(shipped.RegentHealthShare, Is.Zero, "the floor's own share");
            Assert.That(shipped.SidestepsTheRegent, Is.True, "the simulation's pilot sidesteps it as DevelopmentAutopilot does");
            Assert.That(shipped.VaultsWarden, Is.False);
            // The rod at its feet (docs/50, 2026-11-17): read as each mark goes down, as WardenSlamService reads it, inside the
            // Glass Rod's reach; none of the round's research holds.
            Assert.That(shipped.RegentFeetReach, Is.EqualTo(RegentRule.FeetReach));
            Assert.That(shipped.RegentFeetEveryFrame, Is.False);
            Assert.That(regent.Weapon.Range, Is.GreaterThan(RegentRule.FeetReach), "its feet lie inside the rod's reach");
            Assert.That(shipped.RegentRodInTelegraph, Is.False);
            Assert.That(shipped.RegentRodNearReach, Is.Zero);
            Assert.That(shipped.RegentRodBlowReach, Is.Zero);
            Assert.That(shipped.RegentRodRecovery, Is.LessThan(0f), "the Warden's recovery");
            Assert.That(shipped.RegentStoodWalk, Is.Zero);
            Assert.That(shipped.RegentNearReach, Is.Zero);
            Assert.That(shipped.RegentRodStreakShare, Is.Zero);

            FloorDefinition proof = null;
            foreach (FloorDefinition floor in UnityEngine.Resources.LoadAll<FloorDefinition>(DevelopmentStart.ResourcesFolder))
                if (floor.Id == "floor_regent_proof")
                    proof = floor;
            Assert.That(proof, Is.Not.Null, "Resources/Development/Floor_RegentProof");
            Assert.That(proof.RoomCount, Is.EqualTo(1));
            Assert.That(proof.RoomAt(0).Kind, Is.EqualTo(RoomKind.Boss));
            Assert.That(proof.RoomAt(0).WaveAt(0).EnemyCount, Is.EqualTo(1));
            Assert.That(proof.RoomAt(0).WaveAt(0).EnemyAt(0), Is.SameAs(regent));
        }

        // The order Unity runs a script in: its Script Execution Order setting where one is set, else its
        // DefaultExecutionOrder, else 0.
        private static int ScriptOrder<T>(string path) where T : UnityEngine.MonoBehaviour
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/_Project/Scripts/" + path);
            Assert.That(script, Is.Not.Null, path);
            Assert.That(script.GetClass(), Is.EqualTo(typeof(T)), path);
            int set = MonoImporter.GetExecutionOrder(script);
            return set != 0 ? set : typeof(T).GetCustomAttribute<UnityEngine.DefaultExecutionOrder>()?.order ?? 0;
        }

        private const string FirstFloor = "Assets/_Project/Data/Floors/Floor_EmberHalls.asset";

        // Floor_EmberHalls as the foundry plays it: the rooms' kinds and waves, the horde's roles, climb-scaled, and its V0.1
        // knobs (surges, extra elites, the gate's clock) as the simulation's shipped levers.
        [Test]
        public void TheFoundryFloorIsTheAsset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            AssertFloor(asset, DescentSimulation.FoundryFloorOne());
            Assert.That(asset.FoundryHallSurges, Is.EqualTo(FoundryBatch.Shipped().HallSurges));
            Assert.That(asset.FoundryExtraElites, Is.EqualTo(FoundryBatch.Shipped().ExtraElites));
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.EqualTo(FoundryBatch.Shipped().GateNotBeforeSeconds));
            Assert.That(asset.FoundryGateHealthShare, Is.Zero, "floor 1's Warden keeps WardenRule.GateHealthShare");
            Assert.That(WardenRule.GateHealthShareOn(asset.FoundryGateHealthShare), Is.EqualTo(FoundryBatch.Shipped().WardenHealthShare));
        }

        // The climb's floor 2, which a run goes down to after a floor-1 clear (FoundryLevers.Floors = 2): Floor_EmberHalls'
        // next floor as the foundry plays it - its rooms, horde, climb scaling, Cursed Gold and its own foundry knobs (no
        // V0.1 surges or elite, floor 1's gate time; its gate boss's own share) - its gate boss the Glass Regent with two Cinder Mites,
        // the districts the round goes on from, and below it floor 3, Gloam Gardens, which the simulation plays after a
        // floor-2 clear with FoundryLevers.Floors = 3 (TheFoundrysThirdFloorIsTheAsset; the finale before docs/48).
        [Test]
        public void TheFoundrysSecondFloorIsTheAsset()
        {
            var first = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            FloorDefinition asset = first.NextFloor;
            DescentSimulation.FoundryFloor floor = DescentSimulation.FoundryFloorOne().Next;
            Assert.That(floor, Is.Not.Null);
            Assert.That(floor.FloorNumber, Is.EqualTo(2));
            AssertFloor(asset, floor);
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.EqualTo(210f), "floor 2's gate holds for floor 1's 3:30 (docs/47)");

            // Its gate boss: the boss room's first enemy walks out of the gate and WardenRule's share and hold are its; the
            // others walk out beside it. The Glass Regent since 2026-11-12 (docs/50, the lead's decisions), where floor 1 keeps
            // its Forge Warden.
            int boss = System.Array.IndexOf(floor.Kinds, RoomKind.Boss);
            int firstBoss = System.Array.IndexOf(DescentSimulation.FoundryFloorOne().Kinds, RoomKind.Boss);
            Assert.That(asset.RoomAt(boss).WaveAt(0).EnemyAt(0).Id, Is.EqualTo("enemy_glass_regent"));
            Assert.That(first.RoomAt(firstBoss).WaveAt(0).EnemyAt(0).Id, Is.EqualTo("enemy_forge_warden"));
            Assert.That(floor.Rooms[boss][0][0], Is.SameAs(DescentSimulation.GlassRegent));
            Assert.That(floor.Rooms[boss][0].Length, Is.EqualTo(3), "two Cinder Mites beside it");

            // Its elite: the Mirror Sentry leads Sentry Hall (the lead's decisions 2026-11-12), where floor 1 keeps its Captain.
            int elite = System.Array.IndexOf(floor.Kinds, RoomKind.Elite);
            Assert.That(asset.RoomAt(elite).DisplayName, Is.EqualTo("Sentry Hall"));
            Assert.That(asset.RoomAt(elite).WaveAt(0).EnemyAt(0).Id, Is.EqualTo("enemy_mirror_sentry"));
            Assert.That(floor.Rooms[elite][0][0], Is.SameAs(DescentSimulation.MirrorSentry));
            int firstElite = System.Array.IndexOf(DescentSimulation.FoundryFloorOne().Kinds, RoomKind.Elite);
            Assert.That(first.RoomAt(firstElite).WaveAt(0).EnemyAt(0).Id, Is.EqualTo("enemy_grunt_captain"));

            // Its gate boss's share is the floor's own - the Regent's 7.0 (the lead's decisions 2026-11-12; the Warden's was
            // 8.5 / 1.4) - and the simulation's floor 2 plays the asset's (LaterFloorLevers, through the scene's
            // WardenRule.GateHealthShareOn).
            Assert.That(asset.FoundryGateHealthShare, Is.EqualTo(DescentSimulation.FloorTwoGateShare));
            Assert.That(asset.FoundryGateHealthShare, Is.EqualTo(7f));
            Assert.That(DescentSimulation.LaterFloorLevers(FoundryBatch.Shipped(), floor).WardenHealthShare,
                Is.EqualTo(WardenRule.GateHealthShareOn(asset.FoundryGateHealthShare)));

            // The districts go on from floor 1's authored halls.
            Assert.That(DescentSimulation.LaterHalls(DescentSimulation.FoundryFloorOne()), Is.EqualTo(EncounterController.LaterHalls(first)));
            Assert.That(DescentSimulation.LaterHalls(floor), Is.EqualTo(EncounterController.LaterHalls(asset)));

            Assert.That(asset.NextFloor, Is.Not.Null);
            Assert.That(asset.NextFloor.Id, Is.EqualTo("floor_gloam_gardens"), "floor 3 (docs/48)");
            Assert.That(floor.Next, Is.Not.Null, "the simulation's floor 2 leads to floor 3 too");
            Assert.That(floor.Next.Id, Is.EqualTo(asset.NextFloor.Id));
        }

        // The climb's floor 3 (docs/48 phase 1), which a run goes down to after a floor-2 clear (FoundryLevers.Floors = 3):
        // Floor_QuicksilverVaults' next floor as the foundry plays it - floor 2's six rooms under the garden's names, climb-
        // scaled at floor 3 (the first floor where ClimbScaling.Boss's x1.15 reaches the gate boss), no modifier, floor 1's
        // three horde roles, no surges or extra elite, the gate held for 3:00 and its Warden's own share, which the
        // simulation reads through the scene's WardenRule.GateHealthShareOn - its gate boss the Forge Warden with two Cinder
        // Mites, its halls' Mites and its horde's Pollen Wraiths since 2026-11-17 (the Mite reskinned: the simulation's Mite),
        // its own overgrown look (docs/51), the districts the round goes on from, and below it the
        // finale's swarm room, which the simulation plays after a floor-3 clear (FoundryLevers.Floors = 4,
        // TheFoundrysFinaleIsTheAsset).
        [Test]
        public void TheFoundrysThirdFloorIsTheAsset()
        {
            var first = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            FloorDefinition second = first.NextFloor;
            FloorDefinition asset = second.NextFloor;
            Assert.That(asset, Is.SameAs(AssetDatabase.LoadAssetAtPath<FloorDefinition>("Assets/_Project/Data/Floors/Floor_GloamGardens.asset")));
            DescentSimulation.FoundryFloor floor = DescentSimulation.FoundryFloorOne().Next.Next;
            Assert.That(floor, Is.Not.Null);
            Assert.That(floor.FloorNumber, Is.EqualTo(3));
            AssertFloor(asset, floor, wraiths: true);
            Assert.That(asset.DisplayName, Is.EqualTo("Gloam Gardens"));
            Assert.That(asset.Modifier, Is.Null, "no modifier on floor 3's first build");
            Assert.That(asset.Theme, Is.EqualTo(FloorTheme.Gloam), "floor 3 wears its own overgrown look (docs/51)");
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.EqualTo(DescentSimulation.FloorThreeGateSeconds), "floor 3's gate");
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.EqualTo(180f), "floor 3's gate holds for 3:00, half a minute before floors 1 and 2's");
            Assert.That(asset.EnemyHealthMultiplier, Is.EqualTo(ClimbScaling.Health(3)).Within(1e-5f));
            Assert.That(asset.EnemyDamageMultiplier, Is.EqualTo(ClimbScaling.Damage(3)).Within(1e-5f));

            // Its gate boss: the Forge Warden - floor 1's, where floor 2 has the Glass Regent - two Cinder Mites beside it,
            // at the floor's own share.
            int boss = System.Array.IndexOf(floor.Kinds, RoomKind.Boss);
            Assert.That(asset.RoomAt(boss).WaveAt(0).EnemyAt(0).Id, Is.EqualTo(first.RoomAt(boss).WaveAt(0).EnemyAt(0).Id),
                "floor 1's gate boss, the Forge Warden");
            Assert.That(asset.RoomAt(boss).WaveAt(0).EnemyAt(0).Id, Is.EqualTo("enemy_forge_warden"));
            Assert.That(floor.Rooms[boss][0][0], Is.SameAs(DescentSimulation.Warden));
            Assert.That(floor.Rooms[boss][0].Length, Is.EqualTo(3), "two Cinder Mites beside it");
            Assert.That(asset.FoundryGateHealthShare, Is.EqualTo(DescentSimulation.FloorThreeGateShare));
            Assert.That(DescentSimulation.LaterFloorLevers(FoundryBatch.Shipped(), floor).WardenHealthShare,
                Is.EqualTo(WardenRule.GateHealthShareOn(asset.FoundryGateHealthShare)));
            Assert.That(ClimbScaling.Boss(floor.FloorNumber), Is.EqualTo(ClimbScaling.BossGrowth), "floor 3's gate is the first the boss growth reaches");

            // Its elite: the Cinder Bloom leads Thorn Post since 2026-11-15 (docs/52), with the Captain's escort.
            int elite = System.Array.IndexOf(floor.Kinds, RoomKind.Elite);
            Assert.That(asset.RoomAt(elite).DisplayName, Is.EqualTo("Thorn Post"), "Thorn Post keeps its name (docs/52)");
            Assert.That(asset.RoomAt(elite).WaveAt(0).EnemyAt(0).Id, Is.EqualTo("enemy_cinder_bloom"));
            Assert.That(floor.Rooms[elite][0][0], Is.SameAs(DescentSimulation.CinderBloom));

            // The Pollen Wraith (the lead's decision 2026-11-17, docs/52 section 3) takes the Mite's place in the horde's
            // role 0 and in the three halls' waves - Ash Orchard 6, Rootbound Walk 8, Bloom Gate 3 - and Thorn Post's two and
            // the Grove's two stay Cinder Mites, so the elite and the gate are read on their own.
            Assert.That(asset.HordeRoleCount, Is.EqualTo(3));
            Assert.That(asset.HordeRoleAt(0).Id, Is.EqualTo(PollenWraithId), "the horde's role 0");
            Assert.That(asset.HordeRoleAt(1).Id, Is.EqualTo("enemy_grunt"));
            Assert.That(asset.HordeRoleAt(2).Id, Is.EqualTo("enemy_runner"));
            Assert.That(second.HordeRoleAt(0).Id, Is.EqualTo(CinderMiteId), "floor 2's horde keeps its Mite");
            Assert.That(first.HordeRoleAt(0).Id, Is.EqualTo(CinderMiteId), "floor 1's too");
            var rooms = new[] { ("Ash Orchard", 6, 0), ("Rootbound Walk", 8, 0), ("Bloom Gate", 3, 0), ("The Grafting Forge", 0, 0),
                                ("Thorn Post", 0, 2), ("Warden's Grove", 0, 2) };
            Assert.That(asset.RoomCount, Is.EqualTo(rooms.Length));
            for (int room = 0; room < rooms.Length; room++)
            {
                (string name, int wraiths, int mites) = rooms[room];
                Assert.That(asset.RoomAt(room).DisplayName, Is.EqualTo(name));
                Assert.That(Count(asset.RoomAt(room), PollenWraithId), Is.EqualTo(wraiths), name + "'s Pollen Wraiths");
                Assert.That(Count(asset.RoomAt(room), CinderMiteId), Is.EqualTo(mites), name + "'s Cinder Mites");
            }

            // The districts go on from floors 1 and 2's authored halls.
            Assert.That(DescentSimulation.LaterHalls(floor), Is.EqualTo(EncounterController.LaterHalls(asset)));

            Assert.That(asset.NextFloor, Is.Not.Null);
            Assert.That(asset.NextFloor.RoomAt(0).Kind, Is.EqualTo(RoomKind.Swarm), "the finale");
            Assert.That(floor.Next, Is.Not.Null, "the simulation's floor 3 leads to the finale too");
            Assert.That(floor.Next.Id, Is.EqualTo(asset.NextFloor.Id));
        }

        // The climb's last floor, which a run goes down to after a floor-3 clear (FoundryLevers.Floors = 4,
        // DescentSimulation.FinaleFloor, FoundryFinale): Floor_GloamGardens' next floor as the foundry plays it - as authored, no
        // extra elite before a boss it does not have - one swarm room whose ladder's waves and enemies are the asset's, at
        // its own multipliers (it is not climb-scaled), with no modifier, no horde, none of the foundry's knobs and nothing
        // below it; and the development autopilot's stay in it before it walks to the exit.
        [Test]
        public void TheFoundrysFinaleIsTheAsset()
        {
            var first = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            FloorDefinition asset = first.NextFloor.NextFloor.NextFloor;
            DescentSimulation.FoundryFloor floor = DescentSimulation.FoundryFloorOne().Next.Next.Next;
            Assert.That(asset, Is.Not.Null);
            Assert.That(floor, Is.Not.Null);
            Assert.That(asset.Id, Is.EqualTo(floor.Id));
            Assert.That(floor.FloorNumber, Is.EqualTo(DescentSimulation.FinaleFloor));
            Assert.That(DescentSimulation.FinaleFloor, Is.EqualTo(4), "floor 1, floor 2, floor 3, the finale");
            Assert.That(asset.ClimbScaled, Is.EqualTo(floor.ClimbScaled), "the finale keeps its own multipliers");
            Assert.That(asset.ClimbScaled, Is.False);
            Assert.That(asset.EnemyHealthMultiplier, Is.EqualTo(floor.EnemyHealthMultiplier));
            Assert.That(asset.EnemyDamageMultiplier, Is.EqualTo(floor.EnemyDamageMultiplier));
            Assert.That(asset.Modifier, Is.Null);
            Assert.That((floor.ModifierHealthPercent, floor.ModifierDamagePercent, floor.ModifierGoldPercent), Is.EqualTo((0f, 0f, 0f)));
            Assert.That(asset.FoundryHallSurges, Is.EqualTo(floor.HallSurges));
            Assert.That(asset.FoundryExtraElites, Is.EqualTo(floor.ExtraElites));
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.EqualTo(floor.GateNotBeforeSeconds));
            Assert.That(asset.FoundryGateHealthShare, Is.EqualTo(floor.GateHealthShare));
            Assert.That(asset.HordeRoleCount, Is.EqualTo(floor.HordeRoles.Length), "no horde in the finale");
            Assert.That(asset.NextFloor, Is.Null);
            Assert.That(floor.Next, Is.Null);

            RoomDefinition[] played = EncounterController.FoundryRooms(asset);
            Assert.That(played.Length, Is.EqualTo(floor.Kinds.Length), "the foundry plays it as authored");
            for (int room = 0; room < played.Length; room++)
            {
                RoomDefinition definition = played[room];
                string where = $"{asset.Id} room {room}";
                Assert.That(definition.Kind, Is.EqualTo(floor.Kinds[room]), where);
                Assert.That(definition.WaveCount, Is.EqualTo(floor.Rooms[room].Length), where);
                for (int wave = 0; wave < definition.WaveCount; wave++)
                {
                    WaveDefinition waveDefinition = definition.WaveAt(wave);
                    Assert.That(waveDefinition.EnemyCount, Is.EqualTo(floor.Rooms[room][wave].Length), $"{where} wave {wave}");
                    for (int e = 0; e < waveDefinition.EnemyCount; e++)
                        AssertEnemy(waveDefinition.EnemyAt(e), floor.Rooms[room][wave][e], $"{where} wave {wave} enemy {e}");
                }
            }

            Assert.That(Autopilot<float>("SwarmStay"), Is.EqualTo(DescentSimulation.PilotSwarmStay));
        }

        // A foundry floor of the simulation against its asset: id, climb scaling, modifier, its own foundry knobs, the rooms'
        // kinds, waves and enemies (a forge's options in the order the autopilot takes them) and the horde's roles. On floor 3
        // (wraiths) a Pollen Wraith of the asset is the simulation's Mite (AssertEnemy).
        private static void AssertFloor(FloorDefinition asset, DescentSimulation.FoundryFloor floor, bool wraiths = false)
        {
            Assert.That(asset, Is.Not.Null);
            string where = asset.Id;
            Assert.That(asset.Id, Is.EqualTo(floor.Id));
            Assert.That(asset.ClimbScaled, Is.True, where + ": the simulation scales the floor by ClimbScaling");
            FloorModifierDefinition modifier = asset.Modifier;
            Assert.That(modifier != null ? modifier.EnemyHealthPercent : 0f, Is.EqualTo(floor.ModifierHealthPercent), where);
            Assert.That(modifier != null ? modifier.EnemyDamagePercent : 0f, Is.EqualTo(floor.ModifierDamagePercent), where);
            Assert.That(modifier != null ? modifier.GoldPercent : 0f, Is.EqualTo(floor.ModifierGoldPercent), where);
            Assert.That(asset.FoundryHallSurges, Is.EqualTo(floor.HallSurges), where);
            Assert.That(asset.FoundryExtraElites, Is.EqualTo(floor.ExtraElites), where);
            Assert.That(asset.FoundryGateNotBeforeSeconds, Is.EqualTo(floor.GateNotBeforeSeconds), where);
            Assert.That(asset.FoundryGateHealthShare, Is.EqualTo(floor.GateHealthShare), where);
            Assert.That(asset.RoomCount, Is.EqualTo(floor.Kinds.Length), where);
            for (int room = 0; room < asset.RoomCount; room++)
            {
                RoomDefinition definition = asset.RoomAt(room);
                Assert.That(definition.Kind, Is.EqualTo(floor.Kinds[room]), $"{where} room {room}");
                if (definition.Kind == RoomKind.Forge)
                {
                    Assert.That(floor.Rooms[room], Is.Null, $"{where} room {room}");
                    Assert.That(definition.ForgeOptionAt(0).Id, Is.EqualTo(DescentSimulation.Mend().Id), "the autopilot takes the first");
                    Assert.That(definition.ForgeOptionAt(1).Id, Is.EqualTo(DescentSimulation.Temper().Id));
                    continue;
                }
                Assert.That(definition.WaveCount, Is.EqualTo(floor.Rooms[room].Length), $"{where} room {room}");
                for (int wave = 0; wave < definition.WaveCount; wave++)
                {
                    WaveDefinition waveDefinition = definition.WaveAt(wave);
                    Assert.That(waveDefinition.EnemyCount, Is.EqualTo(floor.Rooms[room][wave].Length), $"{where} room {room} wave {wave}");
                    for (int e = 0; e < waveDefinition.EnemyCount; e++)
                        AssertEnemy(waveDefinition.EnemyAt(e), floor.Rooms[room][wave][e], $"{where} room {room} wave {wave} enemy {e}", wraiths);
                }
            }
            Assert.That(asset.HordeRoleCount, Is.EqualTo(floor.HordeRoles.Length), where);
            for (int role = 0; role < asset.HordeRoleCount; role++)
                AssertEnemy(asset.HordeRoleAt(role), floor.HordeRoles[role], $"{where} horde role {role}", wraiths);
        }

        // An enemy of the simulation against its asset. The Pollen Wraith is the Cinder Mite reskinned (the lead's decision
        // 2026-11-17): where wraiths are allowed (floor 3), a Wraith of the asset stands where the simulation has its Mite
        // (DescentSimulation.PollenWraith, the Mite itself under the Mite's name), its own name and every number the Mite's.
        // Elsewhere a Wraith fails on its name.
        private static void AssertEnemy(EnemyDefinition definition, DescentSimulation.Enemy enemy, string where, bool wraiths = false)
        {
            bool wraith = wraiths && definition.Id == PollenWraithId;
            if (wraith)
                Assert.That(enemy, Is.SameAs(DescentSimulation.PollenWraith), where + ": the simulation plays the Wraith as its Mite");
            Assert.That(definition.DisplayName, Is.EqualTo(wraith ? "Pollen Wraith" : enemy.Name), where);
            Assert.That(definition.MaximumHealth, Is.EqualTo(enemy.Health), where);
            Assert.That(definition.MoveSpeed, Is.EqualTo(enemy.Speed), where);
            Assert.That(definition.ExperienceReward, Is.EqualTo(enemy.Experience), where);
            Assert.That(definition.GoldReward, Is.EqualTo(enemy.Gold), where);
            Assert.That(definition.Weapon.Damage, Is.EqualTo(enemy.Damage), where);
            Assert.That(definition.Weapon.Interval, Is.EqualTo(enemy.Interval), where);
            Assert.That(definition.Weapon.Range, Is.EqualTo(enemy.Reach), where);
            Assert.That(definition.Weapon.InitialDelay, Is.EqualTo(enemy.InitialDelay), where);
        }

        [Test]
        public void TheFoundryHeroesAreTheAssets()
        {
            AssertHero("Assets/_Project/Data/Heroes/Hero_Vanguard.asset", DescentSimulation.Vanguard(), HeroPassive.None);
            AssertHero("Assets/_Project/Data/Heroes/Hero_CinderArcanist.asset", DescentSimulation.CinderArcanist(), HeroPassive.EmberMemory);
            var economy = AssetDatabase.LoadAssetAtPath<EconomyConfig>("Assets/_Project/Data/Economy/PrototypeEconomy.asset");
            Assert.That(economy.CategoryChoiceCount, Is.EqualTo(DescentSimulation.FoundryChoiceCount));
            Assert.That(economy.ExperiencePerLevel, Is.EqualTo(DescentSimulation.ExperiencePerLevel));
            Assert.That(economy.ExperienceGrowth, Is.EqualTo(DescentSimulation.ExperienceGrowth));
        }

        private static void AssertHero(string path, DescentSimulation.FoundryHero hero, HeroPassive passive)
        {
            var asset = AssetDatabase.LoadAssetAtPath<HeroDefinition>(path);
            Assert.That(asset.Id, Is.EqualTo(hero.Id), path);
            Assert.That(asset.MaximumHealth, Is.EqualTo(hero.MaximumHealth), path);
            Assert.That(asset.StartingWeapon.Id, Is.EqualTo(hero.Weapon.Id), path);
            Assert.That(asset.StartingWeapon.CategoryDamage, Is.EqualTo(hero.Weapon.CategoryDamage), path);
            Assert.That(asset.StartingWeapon.CategoryInterval, Is.EqualTo(hero.Weapon.CategoryInterval), path);
            Assert.That(asset.StartingWeapon.Range, Is.EqualTo(hero.Weapon.Range), path);
            Assert.That(asset.Ability.Damage, Is.EqualTo(hero.Ability.Damage), path);
            Assert.That(asset.Ability.Radius, Is.EqualTo(hero.Ability.Radius), path);
            Assert.That(asset.Ability.Cooldown, Is.EqualTo(hero.Ability.Cooldown), path);
            Assert.That(asset.Ability.Kind, Is.EqualTo(hero.Ability.Kind), path);
            Assert.That(asset.Passive, Is.EqualTo(passive), path);
            Assert.That(hero.EmberMemory, Is.EqualTo(passive == HeroPassive.EmberMemory), path);
        }
    }
}
