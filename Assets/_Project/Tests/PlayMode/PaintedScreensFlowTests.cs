using System.Collections;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // The between-runs and pause screens painted in the Gameplay scene (ScreenArtSet on the Result Canvas's
    // ScreenArtView): the forge wears its frames, each card the frame of its state and following it as it changes, a
    // relic's picture its well; the chooser frames the chosen hero and its pips follow the choice, and a hero who carries
    // one weapon leaves the rack's other places painted, not bare; the result's title wears its victory or its defeat;
    // the pause shows its plate; the buttons swap to their pressed and disabled plates; no rect the layout reads moves and
    // nothing painted catches a tap; and a view without its set, or with a broken one, leaves today's look.
    public sealed class PaintedScreensFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private CombatSetup _setup;
        private ScreenArtView _view;
        private ScreenArtSet _art;
        private RelicForgeView _forge;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Painted screens cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        private IEnumerator Load(PlayerProfile profile = null)
        {
            TestProfile.Begin(profile);
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _view = Object.FindFirstObjectByType<ScreenArtView>();
            _forge = Object.FindFirstObjectByType<RelicForgeView>();
            Assert.That(_view, Is.Not.Null, "The Result Canvas carries the painter.");
            Assert.That(_view.UsesPaintedArt, Is.True);
            _art = _view.Art;
        }

        // The forge opened, the frame after it (the rack's places and the pips follow it), past its input delay.
        private IEnumerator OpenForge()
        {
            _forge.Open();
            yield return null;
            yield return new WaitForSecondsRealtime(.4f);
        }

        private Transform ForgeArea => _forge.transform.Find("Forge Panel/Safe Area");

        private Button Card(string name) => ForgeArea.Find(name).GetComponent<Button>();

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        // A rect's corners in world space: bottom left and top right.
        private static (Vector2 min, Vector2 max) Corners(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0], corners[2]);
        }

        private static float Scale(Component c) => c.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;

        private static void AssertPainted(Image image, Sprite sprite, string name)
        {
            Assert.That(image.sprite, Is.SameAs(sprite), name);
            Assert.That(image.type, Is.EqualTo(Image.Type.Sliced), $"{name}: 9-sliced.");
            Assert.That(image.pixelsPerUnitMultiplier, Is.EqualTo(1f), $"{name}: its frame at the size it was painted.");
            Assert.That(image.color, Is.EqualTo(Color.white), $"{name}: in its own colours.");
        }

        private static void AssertPlates(Button button, Sprite normal, Sprite pressed, Sprite disabled, string name)
        {
            AssertPainted(button.image, normal, name);
            Assert.That(button.targetGraphic, Is.SameAs(button.image), $"{name}: still the button's own graphic.");
            Assert.That(button.transition, Is.EqualTo(Selectable.Transition.SpriteSwap), $"{name}: its states are painted.");
            Assert.That(button.spriteState.pressedSprite, Is.SameAs(pressed), $"{name}: pressed.");
            Assert.That(button.spriteState.disabledSprite, Is.SameAs(disabled), $"{name}: disabled.");
        }

        private static void AssertNothingPaintedCatchesATap(Component root)
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
                if (image.name.StartsWith("Painted "))
                    Assert.That(image.raycastTarget, Is.False, image.name);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheForgeWearsItsFramesAndEachCardTheFrameOfItsState()
        {
            // The Vanguard's rack in every state: the Sword he carries, the Staff he owns, the Daggers out of reach (160 of
            // 180), Second Wind worn and Counterweight ready to forge (150).
            yield return Load(new PlayerProfile(160, new[] { "relic_second_wind" }, "relic_second_wind", 2, new[] { "weapon_staff" }));
            yield return OpenForge();
            Transform area = ForgeArea;

            Image surface = area.Find("Forge Surface").GetComponent<Image>();
            AssertPainted(surface, _art.Surface, "The forge's surface");
            Assert.That(surface.rectTransform.anchorMin, Is.EqualTo(new Vector2(0f, .015f)), "Where it was.");
            Assert.That(surface.rectTransform.anchorMax, Is.EqualTo(new Vector2(1f, .995f)));
            Assert.That(surface.rectTransform.sizeDelta, Is.EqualTo(new Vector2(-48f, 0f)));

            // The nameplate behind the title, centred on it.
            Text title = area.Find("Forge Title").GetComponent<Text>();
            Image plate = _view.HeaderPlate;
            Assert.That(plate.sprite, Is.SameAs(_art.HeaderPlate));
            Assert.That(plate.transform.parent, Is.SameAs(title.transform.parent));
            Assert.That(plate.transform.GetSiblingIndex(), Is.EqualTo(title.transform.GetSiblingIndex() - 1), "Drawn behind the title.");
            (Vector2 plateMin, Vector2 plateMax) = Corners(plate.rectTransform);
            (Vector2 titleMin, Vector2 titleMax) = Corners(title.rectTransform);
            Assert.That(((plateMin + plateMax) / 2f - (titleMin + titleMax) / 2f).magnitude, Is.LessThan(.5f), "Centred on the title.");
            Assert.That(plateMax.x - plateMin.x, Is.EqualTo(_art.HeaderPlateWidth * Scale(plate)).Within(.5f));
            Assert.That(title.text, Is.EqualTo("Relic Forge"), "Its words as they were.");

            // A rule under each section's header.
            Assert.That(_view.HeaderRules.Count, Is.EqualTo(2));
            foreach (Image rule in _view.HeaderRules)
            {
                Assert.That(rule.sprite, Is.SameAs(_art.Rule));
                Assert.That(rule.transform.parent.GetComponent<Text>(), Is.Not.Null, "Under a header.");
            }

            // Each card in the frame of its state; the written state and its colour as RelicForgeView sets them.
            (string card, UnlockStatus state, string words)[] expected =
            {
                ("Weapon Card 1", UnlockStatus.Equipped, "Equipped"),
                ("Weapon Card 2", UnlockStatus.Owned, "Owned: tap to equip"),
                ("Weapon Card 3", UnlockStatus.TooExpensive, "180 gold: need 20 more"),
                ("Relic Card 1", UnlockStatus.Equipped, "Equipped"),
                ("Relic Card 2", UnlockStatus.Affordable, "Forge for 150 gold"),
            };
            foreach ((string name, UnlockStatus state, string words) in expected)
            {
                Button card = Card(name);
                AssertPainted(card.image, _art.CardFor(state), $"{name}, {state}");
                Assert.That(card.transition, Is.EqualTo(Selectable.Transition.ColorTint), $"{name}: a neutral tint over its frame.");
                Assert.That(card.colors.normalColor, Is.EqualTo(Color.white));
                Assert.That(card.interactable, Is.EqualTo(state != UnlockStatus.TooExpensive), $"{name}: locked only out of reach.");
                Assert.That(card.transform.Find("State Label").GetComponent<Text>().text, Is.EqualTo(words));
                Image accent = card.transform.Find("State Accent").GetComponent<Image>();
                Assert.That(accent.sprite, Is.SameAs(_art.StateAccent), $"{name}: the painted strip.");
                Assert.That(accent.color, Is.EqualTo(card.transform.Find("State Label").GetComponent<Text>().color),
                    $"{name}: still in the state's colour.");
            }
            Assert.That(Card("Weapon Card 1").GetComponent<RectTransform>().anchorMin, Is.EqualTo(new Vector2(0f, .69f)), "Every card where it was.");
            Assert.That(Card("Weapon Card 1").GetComponent<RectTransform>().sizeDelta, Is.EqualTo(new Vector2(-120f, 0f)));

            // A relic's picture in its well, which rings the square the picture draws.
            for (int i = 0; i < 2; i++)
            {
                Image picture = Card($"Relic Card {i + 1}").transform.Find("Relic Artwork").GetComponent<Image>();
                Image well = _view.IconWells[i];
                Assert.That(well.sprite, Is.SameAs(_art.IconWell));
                Assert.That(well.preserveAspect, Is.True);
                Assert.That(well.transform.parent, Is.SameAs(picture.transform.parent));
                Assert.That(well.transform.GetSiblingIndex(), Is.EqualTo(picture.transform.GetSiblingIndex() - 1), "Under the picture.");
                Assert.That(well.color, Is.EqualTo(Color.white), "Neither relic is out of reach.");
                (Vector2 wellMin, Vector2 wellMax) = Corners(well.rectTransform);
                (Vector2 picMin, Vector2 picMax) = Corners(picture.rectTransform);
                float margin = _art.IconWellMargin * Scale(well);
                Assert.That(wellMin.x, Is.EqualTo(picMin.x - margin).Within(.5f));
                Assert.That(wellMax.x, Is.EqualTo(picMax.x + margin).Within(.5f));
                Assert.That((wellMin.y + wellMax.y) / 2f, Is.EqualTo((picMin.y + picMax.y) / 2f).Within(.5f));
                Assert.That(picture.rectTransform.anchorMin, Is.EqualTo(new Vector2(0f, .22f)), "The picture where it was.");
            }

            AssertPlates(area.Find("Start Run Button").GetComponent<Button>(), _art.Primary, _art.PrimaryPressed, _art.PrimaryDisabled,
                "Start run");

            // Forging Counterweight: its frame follows its state, and Second Wind's follows it off.
            Tap(Card("Relic Card 2"));
            Assert.That(_setup.Profile.EquippedRelicId, Is.EqualTo("relic_counterweight"));
            Assert.That(Card("Relic Card 2").image.sprite, Is.SameAs(_art.CardFor(UnlockStatus.Equipped)), "Worn now.");
            Assert.That(Card("Relic Card 1").image.sprite, Is.SameAs(_art.CardFor(UnlockStatus.Owned)), "Owned, no longer worn.");
            // Gold 10 now: the Staff is still owned, so nothing else changes its frame.
            Assert.That(Card("Weapon Card 2").image.sprite, Is.SameAs(_art.CardFor(UnlockStatus.Owned)));
            AssertNothingPaintedCatchesATap(_forge);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ARelicOutOfReachDimsItsPictureWithItsWell()
        {
            yield return Load(new PlayerProfile(90, null, null, 0));
            yield return OpenForge();
            Assert.That(Card("Relic Card 1").image.sprite, Is.SameAs(_art.CardFor(UnlockStatus.Affordable)), "Second Wind at 80.");
            Assert.That(Card("Relic Card 2").image.sprite, Is.SameAs(_art.CardFor(UnlockStatus.TooExpensive)), "Counterweight at 150.");
            Image picture = Card("Relic Card 2").transform.Find("Relic Artwork").GetComponent<Image>();
            Assert.That(picture.sprite.name, Is.EqualTo("UI_Icon_Counterweight_v1"), "Its own picture, as ever.");
            Assert.That(_view.IconWells[1].color, Is.EqualTo(picture.color), "The well and the picture dimmed alike.");
            Assert.That(picture.color.r, Is.LessThan(.7f), "Dimmed, out of reach.");
            Assert.That(_view.IconWells[0].color, Is.EqualTo(Color.white), "Second Wind lit.");
            Tap(Card("Relic Card 1"));
            Assert.That(Card("Relic Card 1").image.sprite, Is.SameAs(_art.CardFor(UnlockStatus.Equipped)));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheChooserFramesTheChosenHeroAndOneWeaponLeavesTheRackPainted()
        {
            yield return Load();
            yield return OpenForge();
            HeroChoiceView choice = _forge.HeroChoice;
            Assert.That(choice, Is.Not.Null, "Two heroes: the forge carries a chooser.");
            AssertPlates(choice.Button, _art.Secondary, _art.SecondaryPressed, _art.SecondaryDisabled, "The chooser");

            // The niche behind the portrait, round it.
            Image portrait = choice.Portrait;
            Image frame = _view.PortraitFrame;
            Assert.That(frame.sprite, Is.SameAs(_art.PortraitFrame));
            Assert.That(frame.transform.parent, Is.SameAs(portrait.transform.parent));
            Assert.That(frame.transform.GetSiblingIndex(), Is.EqualTo(portrait.transform.GetSiblingIndex() - 1), "Behind the portrait.");
            (Vector2 frameMin, Vector2 frameMax) = Corners(frame.rectTransform);
            (Vector2 portraitMin, Vector2 portraitMax) = Corners(portrait.rectTransform);
            Assert.That(frameMin.x <= portraitMin.x && frameMin.y <= portraitMin.y && frameMax.x >= portraitMax.x &&
                        frameMax.y >= portraitMax.y, Is.True, "Framing the hero.");
            (Vector2 chooserMin, Vector2 chooserMax) = Corners((RectTransform)choice.Button.transform);
            Assert.That(frameMin.x, Is.EqualTo(chooserMin.x).Within(.5f), "Flush with the chooser's end.");
            Assert.That(frameMin.y >= chooserMin.y - .5f && frameMax.y <= chooserMax.y + .5f, Is.True, "Within its row.");
            Assert.That(choice.ShownPortrait, Is.SameAs(choice.Current.Portrait), "The portrait itself as HeroChoiceView sets it.");

            // A pip a hero, the chosen one lit.
            Assert.That(_view.HeroPips.Count, Is.EqualTo(_setup.Heroes.Count));
            Assert.That(choice.Current, Is.SameAs(_setup.Heroes[0]));
            Assert.That(_view.HeroPips[0].sprite, Is.SameAs(_art.PipLit), "The Vanguard chosen.");
            Assert.That(_view.HeroPips[1].sprite, Is.SameAs(_art.PipDark));
            for (int i = 0; i < 3; i++)
                Assert.That(_view.EmptyPlace(i).activeSelf, Is.False, "His whole rack: no place empty.");

            // Ilya: her Staff alone, in the rack's middle place, the others painted.
            choice.Cycle();
            yield return null;
            Assert.That(choice.Current.Id, Is.EqualTo("hero_cinder_arcanist"));
            Assert.That(_view.HeroPips[0].sprite, Is.SameAs(_art.PipDark));
            Assert.That(_view.HeroPips[1].sprite, Is.SameAs(_art.PipLit), "Ilya chosen.");
            Assert.That(Card("Weapon Card 2").gameObject.activeSelf, Is.True);
            Assert.That(Card("Weapon Card 2").image.sprite, Is.SameAs(_art.CardFor(UnlockStatus.Equipped)), "Her Staff, carried.");
            foreach (int slot in new[] { 0, 2 })
            {
                Button hidden = Card($"Weapon Card {slot + 1}");
                GameObject place = _view.EmptyPlace(slot);
                Assert.That(hidden.gameObject.activeSelf, Is.False);
                Assert.That(place.activeSelf, Is.True, $"Place {slot + 1}: painted while empty.");
                Assert.That(place.transform.parent, Is.SameAs(hidden.transform.parent));
                (Vector2 placeMin, Vector2 placeMax) = Corners((RectTransform)place.transform);
                (Vector2 cardMin, Vector2 cardMax) = Corners((RectTransform)hidden.transform);
                Assert.That((placeMin - cardMin).magnitude + (placeMax - cardMax).magnitude, Is.LessThan(1f), "In the card's own place.");
                Assert.That(place.transform.Find("Painted Seal").GetComponent<Image>().sprite, Is.SameAs(_art.Seal));
                Assert.That(place.transform.Find("Painted Rule").GetComponent<Image>().sprite, Is.SameAs(_art.Rule));
                Assert.That(place.GetComponent<Button>() == null && place.GetComponentInChildren<Text>() == null, Is.True,
                    "Nothing to tap or read: the forge's rows stay its rows.");
            }
            Assert.That(_view.EmptyPlace(1).activeSelf, Is.False, "The Staff's own place holds the Staff.");

            // Back to the Vanguard: his whole rack again.
            choice.Cycle();
            yield return null;
            Assert.That(_view.HeroPips[0].sprite, Is.SameAs(_art.PipLit));
            for (int i = 0; i < 3; i++)
                Assert.That(_view.EmptyPlace(i).activeSelf, Is.False);
            AssertNothingPaintedCatchesATap(_forge);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheResultsTitleWearsItsVictoryOrItsDefeat()
        {
            foreach (RunOutcome outcome in new[] { RunOutcome.Victory, RunOutcome.Extracted, RunOutcome.Defeat })
            {
                yield return Load();
                _setup.Run.End(outcome);
                yield return null;
                var result = Object.FindFirstObjectByType<RunResultView>();
                Assert.That(result.IsOpen, Is.True);
                bool lost = outcome == RunOutcome.Defeat;
                Transform area = result.transform.Find("Result Panel/Safe Area");
                AssertPainted(area.Find("Result Surface").GetComponent<Image>(), _art.Surface, "The ledger's surface");

                Text title = area.Find("Result Title").GetComponent<Text>();
                Assert.That(title.text, Is.EqualTo(lost ? "Defeated" : outcome == RunOutcome.Extracted ? "Extracted" : "Ascent complete"),
                    "Its words as RunResultView sets them.");
                Image plate = _view.TitlePlate, crest = _view.TitleCrest;
                Assert.That(plate.sprite, Is.SameAs(lost ? _art.DefeatPlate : _art.VictoryPlate), $"{outcome}: its plate.");
                Assert.That(crest.sprite, Is.SameAs(lost ? _art.DefeatCrest : _art.VictoryCrest), $"{outcome}: its crest.");
                Assert.That(plate.transform.GetSiblingIndex(), Is.EqualTo(title.transform.GetSiblingIndex() - 1), "Behind the title.");
                float scale = Scale(plate);
                (Vector2 plateMin, Vector2 plateMax) = Corners(plate.rectTransform);
                (Vector2 titleMin, Vector2 titleMax) = Corners(title.rectTransform);
                Vector4 outset = _art.TitlePlateOutset;
                Assert.That(plateMin.x, Is.EqualTo(titleMin.x - outset.x * scale).Within(.5f), "Round the title.");
                Assert.That(plateMax.y, Is.EqualTo(titleMax.y + outset.w * scale).Within(.5f));
                Assert.That(title.rectTransform.sizeDelta, Is.EqualTo(new Vector2(844f, 100f)), "The title where it was.");
                (Vector2 crestMin, Vector2 crestMax) = Corners(crest.rectTransform);
                Assert.That(crest.transform.parent, Is.SameAs(plate.transform), "Standing on the plate.");
                Assert.That(crestMin.y, Is.EqualTo(plateMax.y - _art.CrestSink * scale).Within(.5f), "Its foot on the plate's rim.");
                Assert.That((crestMin.x + crestMax.x) / 2f, Is.EqualTo((plateMin.x + plateMax.x) / 2f).Within(.5f));
                Vector2 surfaceTop = Corners(area.Find("Result Surface").GetComponent<RectTransform>()).max;
                Assert.That(crestMax.y, Is.LessThan(surfaceTop.y - 17f * scale), "Inside the ledger, clear of its frame.");
                // The words, not the title's rect: the rect is taller than the line it holds.
                float wordsTop = (titleMin.y + titleMax.y) / 2f + title.preferredHeight / 2f * scale;
                Assert.That(crestMin.y, Is.GreaterThan(wordsTop - 1f * scale), "Clear of the title's words.");

                // The divider a rule now, its rect where it was.
                Image divider = area.Find("Result Divider").GetComponent<Image>();
                Assert.That(divider.enabled, Is.False, "The flat line gives way to the rule.");
                Assert.That(_view.DividerRule.sprite, Is.SameAs(_art.Rule));
                Assert.That(divider.rectTransform.sizeDelta, Is.EqualTo(new Vector2(844f, 2f)));

                // The buttons: the primary and the secondary, disabled for the moment after the result opens.
                Button restart = area.Find("Restart Button").GetComponent<Button>();
                Button forge = area.Find("Forge Button").GetComponent<Button>();
                AssertPlates(restart, _art.Primary, _art.PrimaryPressed, _art.PrimaryDisabled, "Try again");
                AssertPlates(forge, _art.Secondary, _art.SecondaryPressed, _art.SecondaryDisabled, "Relic Forge");
                Assert.That(restart.interactable, Is.False);
                Assert.That(restart.image.overrideSprite, Is.SameAs(_art.PrimaryDisabled), "It shows its disabled plate.");
                yield return new WaitForSecondsRealtime(.6f);
                Assert.That(restart.interactable, Is.True);
                Assert.That(restart.image.overrideSprite, Is.SameAs(_art.Primary), "Then its own.");
                AssertNothingPaintedCatchesATap(result);
                LogAssert.NoUnexpectedReceived();
                // The next outcome loads the scene afresh over this one.
            }
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator PauseShowsItsPlateBehindItsWords()
        {
            yield return Load();
            var pause = Object.FindFirstObjectByType<PauseView>();
            Assert.That(_setup.Pause.TryPause(), Is.True);
            yield return null;
            Assert.That(pause.IsOpen, Is.True);
            Transform area = pause.transform.Find("Pause Panel/Safe Area");
            Text title = area.Find("Pause Title").GetComponent<Text>();
            Text hint = area.Find("Pause Hint Label").GetComponent<Text>();
            Image plate = _view.PausePlate;
            Assert.That(plate.gameObject.activeInHierarchy, Is.True, "It shows with the pause.");
            AssertPainted(plate, _art.PausePlate, "The pause plate");
            Assert.That(plate.transform.GetSiblingIndex(), Is.EqualTo(title.transform.GetSiblingIndex() - 1), "Behind the title.");
            (Vector2 plateMin, Vector2 plateMax) = Corners(plate.rectTransform);
            foreach (Text words in new[] { title, hint })
            {
                (Vector2 min, Vector2 max) = Corners(words.rectTransform);
                Assert.That(plateMin.x <= min.x && plateMin.y <= min.y && plateMax.x >= max.x && plateMax.y >= max.y, Is.True,
                    $"Round {words.name}.");
            }
            Assert.That(title.text, Is.EqualTo("Paused"));
            Button resume = area.Find("Resume Button").GetComponent<Button>();
            AssertPlates(resume, _art.Primary, _art.PrimaryPressed, _art.PrimaryDisabled, "Resume");
            Assert.That(resume.image.overrideSprite, Is.SameAs(_art.PrimaryDisabled), "Disabled as the overlay appears.");
            yield return new WaitForSecondsRealtime(.45f);
            Assert.That(resume.interactable, Is.True);
            Assert.That(resume.image.overrideSprite, Is.SameAs(_art.Primary));
            Tap(resume);
            Assert.That(pause.IsOpen, Is.False, "It resumes as ever.");
            AssertNothingPaintedCatchesATap(pause);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator WithoutItsSetTheScreensKeepTodaysLook()
        {
            yield return Load();
            ScreenArtSet art = _art;
            // A screen of its own - a surface, a title on it and a button - given no set, a broken one, then the real one.
            var canvas = new GameObject("Fallback canvas", typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var broken = ScriptableObject.CreateInstance<ScreenArtSet>();
            Assert.That(broken.IsValid, Is.False);
            var flat = new Color(.1f, .1f, .2f);
            foreach (ScreenArtSet given in new[] { null, broken, art })
            {
                var root = new GameObject("Screen " + (given == null ? "without a set" : given == broken ? "with a broken set" : "with the set"),
                    typeof(RectTransform));
                root.SetActive(false);
                root.transform.SetParent(canvas.transform, false);
                Image surface = new GameObject("Surface", typeof(RectTransform)).AddComponent<Image>();
                surface.transform.SetParent(root.transform, false);
                surface.color = flat;
                surface.pixelsPerUnitMultiplier = 7f;
                Text title = new GameObject("Title", typeof(RectTransform)).AddComponent<Text>();
                title.transform.SetParent(root.transform, false);
                Image plate = new GameObject("Button", typeof(RectTransform)).AddComponent<Image>();
                plate.transform.SetParent(root.transform, false);
                var button = plate.gameObject.AddComponent<Button>();
                ColorBlock tint = button.colors;
                tint.normalColor = new Color(1f, .93f, .8f);
                button.colors = tint;
                var view = root.AddComponent<ScreenArtView>();
                typeof(ScreenArtView).GetField("_art", Private).SetValue(view, given);
                typeof(ScreenArtView).GetField("_resultSurface", Private).SetValue(view, surface);
                typeof(ScreenArtView).GetField("_resultTitle", Private).SetValue(view, title);
                typeof(ScreenArtView).GetField("_restartButton", Private).SetValue(view, button);
                root.SetActive(true);
                yield return null;
                yield return null;
                bool painted = given == art;
                Assert.That(view.UsesPaintedArt, Is.EqualTo(painted), root.name);
                Assert.That(surface.sprite == art.Surface, Is.EqualTo(painted), $"{root.name}: the surface.");
                Assert.That(button.transition == Selectable.Transition.SpriteSwap, Is.EqualTo(painted), $"{root.name}: the button.");
                Assert.That(root.transform.childCount, Is.EqualTo(painted ? 4 : 3), $"{root.name}: a title plate only when painted.");
                if (!painted)
                {
                    Assert.That(surface.color, Is.EqualTo(flat), $"{root.name}: the flat look as it was.");
                    Assert.That(surface.pixelsPerUnitMultiplier, Is.EqualTo(7f));
                    Assert.That(button.colors.normalColor, Is.EqualTo(new Color(1f, .93f, .8f)), $"{root.name}: its tint as it was.");
                    Assert.That(view.enabled, Is.False, $"{root.name}: and nothing left running.");
                }
            }
            Object.Destroy(canvas.gameObject);
            Object.Destroy(broken);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
