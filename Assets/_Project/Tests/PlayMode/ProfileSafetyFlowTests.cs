using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Save;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The profile's two save failures in the game (docs/21, 2026-11-26), each with its notice (ProfileNoticeView):
    //   - a full phone: every save throws, the session's progress lives in memory, and a reload (Restart, another map,
    //     hero or tier, a language switch) used to read the older file and lose it without a word; now the unsaved profile
    //     comes over the reload (UnsavedProfile), the new scene writes it again at once, and the player is told;
    //   - an older APK over a newer one: the newer build's profile used to be reset and rotated away by the next saves;
    //     now it stays as it is, the session plays on a new profile in memory (carried over a reload too), nothing is
    //     written, the result is not "saved" (no video, docs/58 3.1), and the player is told once a launch.
    public sealed class ProfileSafetyFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";

        private CombatSetup _setup;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Profile Safety Cleanup " + Guid.NewGuid().ToString("N")));
            if (scene.IsValid() && scene.isLoaded && scene.path == ScenePath)
                yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator AFailedSaveComesOverTheReloadAndThePlayerIsTold()
        {
            TestProfile.Begin(new PlayerProfile(10, null, null, 1));
            yield return LoadScene();
            Assert.That(Notice(), Is.Null, "Nothing to tell: no notice is built.");
            // A folder where the profile's temp file goes makes every save fail, as RewardOfferFlowTests' full phone does.
            string blocked = Path.Combine(TestProfile.Folder, ProfileStore.TempFileName);
            Directory.CreateDirectory(blocked);
            var errors = new List<string>();
            Application.LogCallback capture = (message, stack, type) =>
            {
                if (type == LogType.Error || type == LogType.Exception)
                    errors.Add(message);
            };
            Application.logMessageReceived += capture;
            // For the rest of this test only: the failed saves' errors are this test's subject, checked below.
            LogAssert.ignoreFailingMessages = true;
            try
            {
                // The profile changes (a bank, a forge buy) and its save fails.
                _setup.Profile.Deposit(25);
                ProfileNoticeView notice = Notice();
                Assert.That(notice, Is.Not.Null);
                Assert.That(notice.IsShowing, Is.True);
                Assert.That(notice.ShownLine, Is.EqualTo(ProfileNoticeView.SaveFailedText));
                _setup.Profile.Deposit(5);
                Assert.That(notice.ShownCount, Is.EqualTo(1), "Once a scene, however many saves fail.");
                Assert.That(UnsavedProfile.IsHeldFor(TestProfile.Folder), Is.True);
                Assert.That(TestProfile.ReadSaved().Gold, Is.EqualTo(10), "The file still holds the older profile.");

                // Restart: the session's gold comes over the reload; the new scene writes it again at once, fails, and
                // says so again.
                yield return Reload();
                Assert.That(_setup.Profile.Gold, Is.EqualTo(40), "The session's gold, not the file's 10.");
                Assert.That(_setup.Profile.DeepestFloorCleared, Is.EqualTo(1));
                Assert.That(Notice() != null && Notice().ShownLine == ProfileNoticeView.SaveFailedText, Is.True,
                    "The new scene's write failed too, and said so.");
                Assert.That(TestProfile.ReadSaved().Gold, Is.EqualTo(10));

                // Room is made; the next reload writes the carried profile at once and lets it go.
                Directory.Delete(blocked);
                yield return Reload();
                Assert.That(_setup.Profile.Gold, Is.EqualTo(40));
                Assert.That(UnsavedProfile.IsHeld, Is.False, "Released once a save landed.");
                Assert.That(TestProfile.ReadSaved().Gold, Is.EqualTo(40), "On disk now.");
                Assert.That(Notice(), Is.Null, "Nothing failed in this scene: nothing told.");
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
            Assert.That(errors, Is.Not.Empty);
            foreach (string error in errors)
                Assert.That(error, Does.StartWith("Saving the profile"), "Only the failed saves are reported.");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator ANewerBuildsProfileIsNeverWrittenOverAndThePlayerIsToldOnce()
        {
            TestProfile.Begin();
            string path = Path.Combine(TestProfile.Folder, ProfileStore.FileName);
            string newer = JsonUtility.ToJson(new ProfileSaveData
            {
                saveVersion = ProfileSaveData.CurrentVersion + 1,
                revision = 30,
                gold = 900,
                deepestFloorCleared = 3
            }, true);
            File.WriteAllText(path, newer);
            // Each load logs the newer build's file (ReportLoadProblems); the reload also logs the profile it carried.
            var newerLog = new Regex(@"^Profile NewerVersion from .*written by a newer build");
            LogAssert.Expect(LogType.Warning, newerLog);
            yield return LoadScene();

            Assert.That(_setup.Profile.Gold, Is.Zero, "A new profile in memory, the newer one unread.");
            ProfileNoticeView notice = Notice();
            Assert.That(notice, Is.Not.Null);
            Assert.That(notice.IsShowing, Is.True);
            Assert.That(notice.ShownLine, Is.EqualTo(ProfileNoticeView.NewerVersionText));

            _setup.Profile.Deposit(25);
            _setup.Run.End(RunOutcome.Extracted);
            yield return null;
            Assert.That(_setup.ResultSaved, Is.False, "Nothing is on disk, so no video is offered (docs/58 3.1).");
            Assert.That(File.ReadAllText(path), Is.EqualTo(newer), "The newer build's profile is untouched.");

            LogAssert.Expect(LogType.Warning, new Regex(@"^Profile: the profile this session could not save came over the reload"));
            LogAssert.Expect(LogType.Warning, newerLog);
            yield return Reload();
            Assert.That(_setup.Profile.Gold, Is.EqualTo(25), "The session's gold came over the reload, in memory.");
            Assert.That(Notice() == null || !Notice().IsShowing, Is.True, "Told once a launch: a reload says nothing new.");
            Assert.That(File.ReadAllText(path), Is.EqualTo(newer));
            Assert.That(File.Exists(path + ".bak") || File.Exists(path + ".tmp"), Is.False, "Nothing rotated, nothing written.");
            LogAssert.NoUnexpectedReceived();
        }

        private static ProfileNoticeView Notice() => Object.FindFirstObjectByType<ProfileNoticeView>();

        private IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Find();
        }

        // CombatSetup.RestartRun, as the result's Restart (and a map, hero or tier chosen) reloads the scene.
        private IEnumerator Reload()
        {
            CombatSetup previous = _setup;
            previous.RestartRun();
            float deadline = Time.realtimeSinceStartup + 10f;
            while (previous != null && Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return null;
            Find();
        }

        private void Find()
        {
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            Assert.That(_setup, Is.Not.Null);
            Assert.That(_setup.Profile, Is.Not.Null);
            // Nothing in the arena changes the run's gold while the test holds it.
            foreach (AttackController attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
        }
    }
}
