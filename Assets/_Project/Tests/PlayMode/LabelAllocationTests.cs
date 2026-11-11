using System.Collections;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Cryptforge.Tests
{
    // The exit seal and the altars say what to do in TextMeshes, whose text is read from the native text into a new string
    // on every read. Both views ask every frame what their label should say, and read the label back to compare: one
    // string a frame for as long as the seal, a marked hall or an altar's bargain was on show - most of a foundry floor,
    // since the next hall stays marked while the hero walks to it. Each view now remembers what it gave its label instead,
    // so its frame allocates nothing, and the label still changes the moment what it should say does.
    public sealed class LabelAllocationTests
    {
        [UnitySetUp]
        public IEnumerator Begin()
        {
            TestProfile.Begin();
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Label allocation cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // Nothing strikes and the hero stands where the test puts it.
        private static IEnumerator Load(string floor, string region)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, floor);
            if (region != null)
                File.WriteAllText(DevelopmentStart.RegionPreviewPath, region);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
        }

        private static PrototypeTextDefinition Strings() => Resources.FindObjectsOfTypeAll<PrototypeTextDefinition>()[0];

        [UnityTest, Timeout(90000)]
        public IEnumerator TheExitSealSaysWhatToDoWithoutAllocating()
        {
            yield return Load("floor_swarm_proof", null);
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = setup.GetComponent<ExitSealView>();
            TextMesh label = setup.transform.Find("Exit Instruction").GetComponent<TextMesh>();
            PrototypeTextDefinition text = Strings();
            Assert.That(view.IsVisible, Is.True, "The swarm proof's exit seal is on show from the start.");

            // The first call compiles the frame's path; every later one is the steady state.
            view.Refresh();
            Assert.That(() => view.Refresh(), Is.Not.AllocatingGCMemory());
            Assert.That(label.text, Is.EqualTo(view.ShowsDirection ? text.ExitDirection : text.ExitInstruction));

            // The seal off the screen: the label turns to the way there at once, and still allocates nothing after.
            Camera camera = Camera.main;
            camera.GetComponent<ArenaCameraFollow>().enabled = false;
            camera.transform.position += Vector3.right * 30;
            view.Refresh();
            Assert.That(view.ShowsDirection, Is.True, "The seal is off the screen.");
            Assert.That(label.text, Is.EqualTo(text.ExitDirection));
            Assert.That(() => view.Refresh(), Is.Not.AllocatingGCMemory());
            Assert.That(label.text, Is.EqualTo(text.ExitDirection));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator AnAltarsBargainIsShownWithoutAllocating()
        {
            yield return Load("floor_room_sites_proof", "foundry");
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = setup.GetComponent<AltarView>();
            TextMesh label = setup.transform.Find("Altar Instruction").GetComponent<TextMesh>();
            Transform hero = Object.FindFirstObjectByType<HeroMovementInput>().transform;
            Assert.That(setup.Altars.IsActive, Is.True, "The foundry has altars.");

            // Beside the first altar: near enough for its bargain, too far to start taking it.
            FoundryMap.AltarSite(0, out float x, out float y);
            hero.position = new Vector3(x + AltarRule.Radius + .5f, ArenaFloor.WorldY(y), 0f);
            yield return null;
            Assert.That(label.gameObject.activeSelf, Is.True, "The bargain is spelled out beside the altar.");
            Assert.That(label.text, Is.EqualTo(Strings().AltarInstruction));

            view.Refresh();
            Assert.That(() => view.Refresh(), Is.Not.AllocatingGCMemory());
            Assert.That(label.gameObject.activeSelf, Is.True);
            Assert.That(label.text, Is.EqualTo(Strings().AltarInstruction));
            Assert.That(view.IsShown(0), Is.True, "The altar is still there to take.");
            LogAssert.NoUnexpectedReceived();
        }
    }

    // Unity calls the gate arrival's OnGUI every frame for as long as the view lives - the whole run, long after the
    // arrival - and before it GUILayout's layout pass, which allocates every frame. Its hint is a plain GUI.Label, so the
    // view skips the pass.
    public sealed class GateArrivalLayoutTests
    {
        [Test]
        public void TheArrivalSkipsTheLayoutPass()
        {
            var host = new GameObject("Gate arrival layout");
            try
            {
                var arrival = host.AddComponent<GateArrivalView>();
                Assert.That(arrival.useGUILayout, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
