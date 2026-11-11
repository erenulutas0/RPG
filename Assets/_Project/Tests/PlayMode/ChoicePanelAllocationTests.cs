using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The run's first level-up was the perf probe's slowest frame (the 2026-11-11 allocation audit: 345 KB, where later
    // level-ups take ~40 KB): the choice panel starts inactive in the scene, so its first show woke every part of it -
    // each label making its text generator - cloned the third card a category offer needs and made every painted part.
    // RunChoiceView does all of that once at load now (Prewarm), where no one sees it, so the first offer's show costs what
    // every later one does.
    public sealed class ChoicePanelAllocationTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        // What the same prompt's first and second show may differ by, in allocations. Before Prewarm the first show made the
        // third card and 24 painted parts and woke a dozen labels: hundreds more than the second.
        private const int Margin = 40;

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
            SceneManager.SetActiveScene(SceneManager.CreateScene("Choice panel allocation cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // How many managed allocations the action makes on this thread, counted as Is.Not.AllocatingGCMemory counts them.
        private static int Allocations(Action act)
        {
            Recorder recorder = Recorder.Get("GC.Alloc");
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            try
            {
                act();
            }
            finally
            {
                recorder.enabled = false;
                recorder.CollectFromAllThreads();
            }
            return recorder.sampleBlockCount;
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheFirstOfferIsShownForWhatTheNextCosts()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;

            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<RunChoiceView>();
            Assert.That(setup.Upgrades.IsCategoryMode, Is.True, "The foundry offers by category.");
            Assert.That(view.IsOpen, Is.False, "Nothing is offered at load.");
            Assert.That(view.CardSlots, Is.EqualTo(3), "The third card is cloned at load.");
            for (int c = 0; c < view.CardSlots; c++)
            {
                Button card = view.CardButton(c);
                Assert.That(card.gameObject.activeInHierarchy, Is.False, $"Card {c} is hidden until an offer.");
                if (view.PaintsCards)
                    Assert.That(card.transform.Find("Painted Press"), Is.Not.Null, $"Card {c} is painted at load.");
            }

            // The view stops hearing the choices, so the test shows the offer itself: the first show the panel ever makes,
            // then, hidden, the same prompt again.
            var heard = (Action)Delegate.CreateDelegate(typeof(Action), view,
                typeof(RunChoiceView).GetMethod("OnChoicesChanged", Private));
            var show = (Action<ChoicePrompt>)Delegate.CreateDelegate(typeof(Action<ChoicePrompt>), view,
                typeof(RunChoiceView).GetMethod("Show", Private));
            setup.Choices.Changed -= heard;
            int first, again;
            ChoicePrompt prompt;
            try
            {
                setup.Run.GrantBonusUpgrade();
                prompt = setup.Choices.Current;
                Assert.That(prompt, Is.Not.Null);
                Assert.That(prompt.Kind, Is.EqualTo(ChoiceKind.Upgrade));
                Assert.That(view.IsOpen, Is.False, "The view did not hear the offer.");

                Action showPrompt = () => show(prompt);
                Action hide = () => show(null);
                first = Allocations(showPrompt);
                Assert.That(view.IsOpen, Is.True);
                hide();
                again = Allocations(showPrompt);
            }
            finally
            {
                setup.Choices.Changed += heard;
            }
            Debug.Log($"[ChoicePanelAllocationTests] first show {first} allocations, the same prompt again {again}.");
            Assert.That(first, Is.LessThanOrEqualTo(again + Margin),
                $"The first show allocates {first} times, the same prompt shown again {again}: the panel is not made ready at load.");

            // Shown as it always is: every card the offer drew, named and on show.
            for (int c = 0; c < prompt.Cards.Count; c++)
            {
                Button card = view.CardButton(c);
                Assert.That(card.gameObject.activeInHierarchy, Is.True, $"Card {c} is shown.");
                Assert.That(card.GetComponentInChildren<Text>().text, Is.EqualTo(prompt.Cards[c].Name), $"Card {c} names its pick.");
            }
            for (int i = 0; i < 4 && setup.Choices.IsOpen; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
            Assert.That(view.IsOpen, Is.False, "The pick closes the panel.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
