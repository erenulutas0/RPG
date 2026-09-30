using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cryptforge.Core
{
    // A development-only still life for looking at the foundry's depth ordering (FoundryVisibilityView and the x-ray
    // silhouettes of Resources/FoundryActorXray.shader) on a device, where no ordinary run stands anyone behind a boiler
    // for long. With <profile folder>/development/depth-proof.txt holding any text, and the foundry preview on
    // (region-preview.txt holding foundry), the first wave to arrive is stopped where it stands and the scene is arranged
    // round the south-west boiler, design (17.9, -4.08): one enemy on the apron behind it, at the spot
    // Tools/ArtReview/FoundryRuntimeCapture uses; the room's chest and one ground drop behind it too; the hero on open
    // floor in front of it; and a second enemy of the wave, when it has one, on open court floor just in front of the
    // hero, where nothing covers it. The camera keeps following the hero, and at the portrait width only that boiler fits
    // the frame beside him, so everything hidden stands behind it, spread across its width: the enemy at its left with
    // its head above the crown, the chest left of centre and the drop right of centre, low enough on the apron that the
    // crown covers most or all of them. One "[DepthProof] arranged ..." line in the log gives every world position.
    //
    // Nothing walks after that. The encounter's update stops, so no pack walks, no drop is picked up and no wave, room or
    // floor follows; every attack stops, so nobody fights and the hero needs no protection; the wave's health is
    // disabled, so even the ability button, which still answers, fells nobody; the chest's spawner stops, so walking onto
    // the chest opens nothing. Nothing is won, banked or saved: no kill, no pickup, no chest and no floor clear can happen,
    // so the run never ends and pays nothing; closing the app leaves it unfinished, as closing any run does. The hero can
    // still be walked about to look from elsewhere.
    //
    // For the depth of combat effects (CombatEffectsView, whose effects above the floor FoundryVisibilityView sets at the
    // depth of the feet they stand on), both enemies take a strike every StrikeInterval: a thousandth of a point through
    // their health's state, which the disabled component would ignore, healed back to whole at once. The views show it as
    // any hit: the effects view a spark on the body, standing on the enemy's feet, and CombatantView the body's flash; the
    // amount rounds to no number. So one strike shows in the open court and is covered behind the boiler like the body it
    // lands on, save what rises above the crown. Only the encounter's per-fight hit tally counts it, which only a victory
    // shows and none can come; no health is lost, and no kill, reward or save can follow from it.
    //
    // Only the Editor and a development build look (DevelopmentStart.DepthProof), and only a scene whose arena is the
    // foundry is touched; without the flag, in a release build or in any other arena nothing is created at all.
    //
    // On the phone (persistentDataPath is /sdcard/Android/data/com.cryptforge.prototype/files), with the game's focus
    // checked before every capture (docs/25); the region-preview line is only needed if that file does not already hold
    // foundry:
    //   adb shell "mkdir -p /sdcard/Android/data/com.cryptforge.prototype/files/development"
    //   adb shell "echo foundry > /sdcard/Android/data/com.cryptforge.prototype/files/development/region-preview.txt"
    //   adb shell "echo on > /sdcard/Android/data/com.cryptforge.prototype/files/development/depth-proof.txt"
    //   adb shell am force-stop com.cryptforge.prototype
    //   adb shell am start -n com.cryptforge.prototype/com.unity3d.player.UnityPlayerActivity
    //   adb logcat -d -s Unity        (look for [DepthProof])
    //   adb shell screencap -p /sdcard/depth-proof.png
    //   adb pull /sdcard/depth-proof.png
    // A spark lasts about a quarter of a second, so most captures show the court enemy's; if one does not, take another.
    // The flag must be removed afterwards, or every development start on the foundry stands still like this:
    //   adb shell rm /sdcard/Android/data/com.cryptforge.prototype/files/development/depth-proof.txt
    //   adb shell rm /sdcard/depth-proof.png
    //   adb shell am force-stop com.cryptforge.prototype
    // Leave region-preview.txt as it was before, unless it was written only for this. In the Editor the same file goes
    // under Application.persistentDataPath/development.
    [DefaultExecutionOrder(-100)]
    public sealed class DepthProofArrangement : MonoBehaviour
    {
        public const string Tag = "[DepthProof]";
        internal const string ObjectName = "Depth Proof (development)";
        // Scaled seconds between strikes, the clock the effects run on, so a pause holds both. A spark lasts about a
        // quarter of a second: at this pace one is on show in most captures, without a hit on every frame.
        internal const float StrikeInterval = .3f;
        // Far below any enemy's health, and under the half point the effects view rounds to a number, so a strike shows
        // the spark and nothing to read.
        private const float StrikeDamage = .001f;
        // Spots in FoundryMap's design plane (u, v), turned into floor positions as the capture does. The south-west
        // boiler stands at (17.9, -4.08) with a plinth of radius 2 and the apron behind it begins at v -2; its squat crown
        // is tallest near its centre, so the hidden spots hug the apron's edge left of and right of centre.
        internal static readonly Vector2 EnemySpot = new Vector2(18.5f, -1.2f);
        internal static readonly Vector2 ChestSpot = new Vector2(19.4f, -1.85f);
        internal static readonly Vector2 DropSpot = new Vector2(21f, -1.9f);
        // The west court just in front of the boiler, clear of the court's edge by more than the hero's margin.
        internal static readonly Vector2 HeroSpot = new Vector2(13f, -7.6f);
        // Open court in front of the hero and a little to his left, under the hidden enemy on screen: three units inside
        // the court's east edge and five inside its south edge, where no structure stands in front of it.
        internal static readonly Vector2 CourtSpot = new Vector2(11f, -8.6f);

        private CombatSetup _setup;
        private ArenaView _arena;
        private HeroMovementInput _hero;
        private ChestSpawner _chests;
        private bool _arranged;
        private float _untilStrike;

        // The enemy standing behind the boiler, once the arrangement is made; for the scene test.
        internal Health Hidden { get; private set; }
        // The enemy in the open court, or null when the wave had no second one; for the scene test.
        internal Health Exposed { get; private set; }
        // Strikes that have landed on the hidden enemy since the arrangement; for the scene test.
        internal int Strikes { get; private set; }

        // The one piece of static state, and deliberate: a scene-load listener for the app's lifetime lets a development
        // tool arrange the scene without a reference in Gameplay.unity or in the composition root. Release builds never
        // listen. With domain reload off a second play would subscribe twice, hence the removal first.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Application.isEditor || Debug.isDebugBuild)
                SceneManager.sceneLoaded += OnSceneLoaded;
        }

        // After the scene's Awake, so the arena has resolved its region and the run exists, and before any Start. Every
        // scene without the flag costs one file check, and nothing is looked up or created unless it is there.
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!DevelopmentStart.DepthProof())
                return;

            CombatSetup setup = FindIn<CombatSetup>(scene);
            ArenaView arena = FindIn<ArenaView>(scene);
            HeroMovementInput hero = FindIn<HeroMovementInput>(scene);
            if (setup == null || arena == null || hero == null || hero.Motion == null || !arena.IsFoundryRegion ||
                arena.Region == null)
                return;

            var holder = new GameObject(ObjectName);
            SceneManager.MoveGameObjectToScene(holder, scene);
            DepthProofArrangement proof = holder.AddComponent<DepthProofArrangement>();
            proof._setup = setup;
            proof._arena = arena;
            proof._hero = hero;
            proof._chests = FindIn<ChestSpawner>(scene);
        }

        // Before the hero, the encounter and every attack update, so the first frame that could move anything is stopped;
        // and before the combat effects, so a strike's spark is animated from the frame it lands on.
        private void Update()
        {
            // A scene that could not start, or a run already over, is left as it is.
            if (_setup == null || _setup.Run == null || _setup.Run.HasEnded || _setup.Encounters == null || _hero == null)
            {
                Destroy(gameObject);
                return;
            }

            if (!_arranged)
            {
                // The first wave comes in the scene's Awake unless the floor opens on a forge; then this waits for it.
                Health hidden = FirstLiving(_setup.Encounters, null);
                if (hidden == null)
                    return;

                Arrange(hidden);
                _arranged = true;
            }

            // The first strike lands on the arrangement's own frame, then one every interval.
            _untilStrike -= Time.deltaTime;
            if (_untilStrike > 0f)
                return;
            _untilStrike = StrikeInterval;
            if (Strike(Hidden))
                Strikes++;
            Strike(Exposed);
        }

        private void Arrange(Health hidden)
        {
            EncounterController encounters = _setup.Encounters;
            // Without its update the encounter walks no pack, picks up no drop and never moves on: nothing that ends a
            // run or pays one can follow from it.
            encounters.enabled = false;
            // Nobody attacks: the pack never hits the hero, so the hero needs nothing more, and the hero hits nothing.
            foreach (AttackController attack in FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            // A disabled Health is out of every reach and ignores damage, so a burst of the ability, which still answers,
            // cannot fell one of the pack either, and no last kill can clear the floor and save the profile.
            for (int i = 0; i < encounters.WaveEnemyCount; i++)
            {
                Health enemy = encounters.WaveEnemyAt(i);
                if (enemy != null)
                    enemy.enabled = false;
            }

            Vector3 enemyAt = Place(hidden.transform, EnemySpot);

            // The chest's root is its sprite; its spawner stops first, so walking onto it opens nothing.
            string chest = "no chest in this room";
            if (_chests != null && _chests.HasChest && _chests.Body != null)
            {
                _chests.enabled = false;
                chest = "chest at " + Describe(Place(_chests.Body.transform, ChestSpot));
            }

            // Experience and gold, so the drop wears the painted reward art. It is never picked up: only the encounter's
            // update collects and only a floor clear sweeps, and neither can happen now.
            FoundryMap.ToFloor(DropSpot.x, DropSpot.y, out float dropX, out float dropY);
            _setup.Drops.Add(dropX, dropY, 1, 1);

            // Through its walk, as the capture places it, so a drag walks on from here; the camera follows it over.
            FoundryMap.ToFloor(HeroSpot.x, HeroSpot.y, out float heroX, out float heroY);
            _hero.Motion.Place(heroX, heroY, _arena.Geometry);
            Transform hero = _hero.transform;
            hero.position = new Vector3(heroX, ArenaFloor.WorldY(heroY), hero.position.z);

            // The wave's next enemy, where nothing covers it: its sparks are the ones that show.
            Health exposed = FirstLiving(encounters, hidden);
            string court = "no second enemy for the open court";
            if (exposed != null)
                court = exposed.name + " in the open court at " + Describe(Place(exposed.transform, CourtSpot));

            Hidden = hidden;
            Exposed = exposed;
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", FormattableString.Invariant(
                $"{Tag} arranged behind the south-west boiler: enemy {hidden.name} at {Describe(enemyAt)}, {chest}, drop at {Describe(new Vector3(dropX, ArenaFloor.WorldY(dropY)))}; hero in front at {Describe(hero.position)}, {court} (world units). The fight is stopped; every {StrikeInterval:0.0#} s each enemy placed takes a spark-only strike, healed at once. Remove development/{DevelopmentStart.DepthProofFileName} afterwards."));
        }

        // One strike, shown by every view as a hit: the effects view's spark, the body's flash. It goes through the state,
        // since the disabled component ignores damage, and only ever lands on a whole enemy, which a thousandth of a point
        // leaves alive and far above any enrage threshold; healing it by its maximum, which the heal caps, makes it exactly
        // whole again in the same call, so only the hit's own listeners ever see it hurt.
        private static bool Strike(Health enemy)
        {
            HealthState state = enemy != null ? enemy.State : null;
            if (state == null || state.Current < state.Maximum || state.Current <= StrikeDamage)
                return false;

            state.ApplyDamage(new DamageContext(StrikeDamage));
            state.Heal(state.Maximum);
            return true;
        }

        private static Vector3 Place(Transform actor, Vector2 design)
        {
            FoundryMap.ToFloor(design.x, design.y, out float x, out float y);
            actor.position = new Vector3(x, ArenaFloor.WorldY(y), actor.position.z);
            return actor.position;
        }

        // The wave's first living enemy other than except, in slot order.
        private static Health FirstLiving(EncounterController encounters, Health except)
        {
            for (int i = 0; i < encounters.WaveEnemyCount; i++)
            {
                Health enemy = encounters.WaveEnemyAt(i);
                if (enemy != null && enemy.IsAlive && enemy != except)
                    return enemy;
            }
            return null;
        }

        // Searched in the loaded scene only: while a restart loads, the previous scene's objects may not be gone yet.
        private static T FindIn<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                T found = roots[i].GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }
            return null;
        }

        private static string Describe(Vector3 position) => FormattableString.Invariant($"({position.x:0.00}, {position.y:0.00})");
    }
}
