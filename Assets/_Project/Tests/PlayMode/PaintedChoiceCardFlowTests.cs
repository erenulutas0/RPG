using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    // The run's choice cards painted in the Gameplay scene (ChoiceCardArtSet on the Upgrade Canvas's RunChoiceView): an
    // upgrade offer's cards each wear their tier's frame and an evolution its own, with its aura and crest; the forge's
    // cards the neutral one and the checkpoint's Extract and Climb theirs; every part sits round the label or icon it
    // frames as the layout left it (three stacked cards, or the hall's two), none takes a tap; the press is the button's
    // graphic, shaded while a card waits out its delay and lit in the treatment's colour under a finger, the frame keeping
    // its own colours; and without the set, or with a broken one, the panel is the scene's own.
    public sealed class PaintedChoiceCardFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly string[] Parts =
        {
            "Painted Aura", "Painted Crest", "Painted Press", "Painted Name Plate", "Painted Description", "Painted Well",
            "Painted Sigil", "Painted Well Lip", "Painted Item Ground"
        };

        [UnitySetUp]
        public IEnumerator Begin()
        {
            TestProfile.Begin();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Painted cards cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // The region's run; beforeStart, if given, sees the choice view after its Awake and before its Start (sceneLoaded
        // runs between them), so a test can take its set away.
        private static IEnumerator Load(string region, System.Action<RunChoiceView> beforeStart = null)
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, region);
            UnityEngine.Events.UnityAction<Scene, LoadSceneMode> hook = (scene, mode) =>
            {
                if (beforeStart == null)
                    return;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    var view = root.GetComponentInChildren<RunChoiceView>(true);
                    if (view != null)
                        beforeStart(view);
                }
            };
            SceneManager.sceneLoaded += hook;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            SceneManager.sceneLoaded -= hook;
            yield return null;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
        }

        private static Image Part(Button card, string name)
        {
            Transform found = card.transform.Find(name);
            return found != null ? found.GetComponent<Image>() : null;
        }

        private static ChoiceCardArtSet.Treatment Expected(ChoiceCardArtSet art, UpgradeOffer offer, int slot) =>
            offer.Choices[slot].IsEvolution ? art.Evolution : art.Tier(offer.RarityAt(slot));

        private static void AssertEdges(RectTransform part, RectTransform target, Vector4 margin, string because)
        {
            Assert.That(part.anchorMin, Is.EqualTo(target.anchorMin), because);
            Assert.That(part.anchorMax, Is.EqualTo(target.anchorMax), because);
            Assert.That(Vector2.Distance(part.offsetMin, target.offsetMin - new Vector2(margin.x, margin.y)), Is.LessThan(.01f),
                because + ": its bottom left edge.");
            Assert.That(Vector2.Distance(part.offsetMax, target.offsetMax + new Vector2(margin.z, margin.w)), Is.LessThan(.01f),
                because + ": its top right edge.");
        }

        private static void AssertColour(Color actual, Color expected, string because)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(.02f), because + $" ({actual} for {expected})");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(.02f), because);
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(.02f), because);
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(.02f), because);
        }

        // Every label on the card still fits its rect at its minimum font size, as ChoiceCardsFlowTests holds them.
        private static void AssertTextFits(Button card)
        {
            foreach (Text label in card.GetComponentsInChildren<Text>())
            {
                var settings = label.GetGenerationSettings(new Vector2(label.rectTransform.rect.width, 0));
                settings.resizeTextForBestFit = false;
                settings.fontSize = label.resizeTextMinSize;
                float needed = label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
                Assert.That(needed, Is.LessThanOrEqualTo(label.rectTransform.rect.height + 1f),
                    card.name + "/" + label.name + " ('" + label.text + "') truncates even at its minimum font size.");
            }
        }

        // A card in the treatment it should wear, its parts round the scene's own labels and icon.
        private static void AssertPainted(Button card, ChoiceCardArtSet art, ChoiceCardArtSet.Treatment look, string name)
        {
            var frame = card.GetComponent<Image>();
            Assert.That(frame.sprite, Is.SameAs(look.Frame), $"{name}: its treatment's frame.");
            Assert.That(frame.type, Is.EqualTo(Image.Type.Sliced), name);
            Assert.That(frame.pixelsPerUnitMultiplier, Is.EqualTo(1f), $"{name}: its frame at the size it was painted.");
            Assert.That(frame.color, Is.EqualTo(Color.white), name);
            Assert.That(frame.raycastTarget, Is.True, $"{name}: the card itself still takes the tap.");
            foreach (string part in Parts)
            {
                Image image = Part(card, part);
                Assert.That(image, Is.Not.Null, $"{name}: {part} is laid.");
                Assert.That(image.raycastTarget, Is.False, $"{name}: {part} never takes a tap.");
            }
            RectTransform label = card.transform.Find("Name Label").GetComponent<RectTransform>();
            RectTransform description = card.transform.Find("Description Label").GetComponent<RectTransform>();
            Image icon = card.transform.Find("Choice Icon").GetComponent<Image>();
            Image plate = Part(card, "Painted Name Plate");
            Assert.That(plate.sprite, Is.SameAs(look.NamePlate), name);
            Assert.That(plate.transform.GetSiblingIndex(), Is.EqualTo(label.GetSiblingIndex() - 1), $"{name}: the plate lies behind the name.");
            AssertEdges(plate.rectTransform, label, art.NamePlateMargin, $"{name}: the name plate round the name");
            Image recess = Part(card, "Painted Description");
            Assert.That(recess.transform.GetSiblingIndex(), Is.EqualTo(description.GetSiblingIndex() - 1), name);
            AssertEdges(recess.rectTransform, description, art.DescriptionMargin, $"{name}: the recess round the description");
            float m = art.WellMargin;
            Image well = Part(card, "Painted Well"), sigil = Part(card, "Painted Sigil"), lip = Part(card, "Painted Well Lip");
            Assert.That(well.sprite, Is.SameAs(look.Well), name);
            AssertEdges(well.rectTransform, icon.rectTransform, new Vector4(m, m, m, m), $"{name}: the well round the icon");
            Assert.That(well.transform.GetSiblingIndex(), Is.LessThan(icon.transform.GetSiblingIndex()), $"{name}: behind the icon.");
            Assert.That(lip.transform.GetSiblingIndex(), Is.EqualTo(icon.transform.GetSiblingIndex() + 1), $"{name}: the lip over its edge.");
            AssertEdges(lip.rectTransform, icon.rectTransform, Vector4.zero, $"{name}: the lip on the icon's edge");
            Image ground = Part(card, "Painted Item Ground");
            // An untiered treatment (the evolution's, the forge's, Extract's and Climb's) has no ground: the part is empty and
            // switched off, and the well's navy floor shows as before. An empty sprite reference is compared with Unity's
            // own equality, since the set's and the image's empty references need not be the same object.
            if (look.Ground != null)
                Assert.That(ground.sprite, Is.SameAs(look.Ground), $"{name}: its treatment's item ground.");
            else
                Assert.That(ground.sprite == null, Is.True, $"{name}: no item ground, the well's navy floor.");
            Assert.That(ground.enabled, Is.EqualTo(look.Ground != null), $"{name}: a ground only where its treatment has one.");
            Assert.That(ground.type, Is.EqualTo(Image.Type.Simple), name);
            AssertEdges(ground.rectTransform, icon.rectTransform, Vector4.zero, $"{name}: the ground exactly under the icon");
            Assert.That(ground.transform.GetSiblingIndex(), Is.GreaterThan(well.transform.GetSiblingIndex()),
                $"{name}: the ground on the well's floor.");
            Assert.That(ground.transform.GetSiblingIndex(), Is.LessThan(sigil.transform.GetSiblingIndex()),
                $"{name}: under the sigil and the icon.");
            Assert.That(sigil.sprite, Is.SameAs(look.Sigil), name);
            Assert.That(sigil.enabled, Is.EqualTo(!(icon.enabled && icon.sprite != null)), $"{name}: the sigil only while the well is empty.");
            Image aura = Part(card, "Painted Aura"), crest = Part(card, "Painted Crest");
            Assert.That(aura.enabled, Is.EqualTo(look.Aura != null), $"{name}: an aura only where its treatment has one.");
            if (look.Aura != null)
            {
                Vector4 o = look.AuraOutset;
                Assert.That(aura.sprite, Is.SameAs(look.Aura), name);
                Assert.That(aura.rectTransform.anchorMin == Vector2.zero && aura.rectTransform.anchorMax == Vector2.one, Is.True, name);
                Assert.That(Vector2.Distance(aura.rectTransform.offsetMin, new Vector2(-o.x, -o.y)), Is.LessThan(.01f),
                    $"{name}: past the card by its outset.");
                Assert.That(Vector2.Distance(aura.rectTransform.offsetMax, new Vector2(o.z, o.w)), Is.LessThan(.01f), name);
            }
            Assert.That(crest.enabled, Is.EqualTo(look.Crest != null), $"{name}: a crest only where its treatment has one.");
            if (look.Crest != null)
            {
                Assert.That(crest.sprite, Is.SameAs(look.Crest), name);
                Assert.That(crest.rectTransform.anchorMin, Is.EqualTo(new Vector2(.5f, 1f)), $"{name}: on the middle of the top edge.");
                Assert.That(crest.rectTransform.sizeDelta, Is.EqualTo(look.Crest.rect.size), $"{name}: at its painted size.");
            }
            var press = (Image)card.targetGraphic;
            Assert.That(press, Is.SameAs(Part(card, "Painted Press")), $"{name}: the press is the button's graphic.");
            Assert.That(press.rectTransform.offsetMin.magnitude + press.rectTransform.offsetMax.magnitude, Is.LessThan(.01f),
                $"{name}: exactly over the card.");
            Assert.That(card.transition, Is.EqualTo(Selectable.Transition.ColorTint), name);
            Assert.That(card.colors.pressedColor, Is.EqualTo(look.PressColour), $"{name}: lit in its treatment's colour.");
            Assert.That(card.colors.disabledColor, Is.EqualTo(art.DisabledColour), name);
            Assert.That(card.colors.normalColor.a, Is.Zero, $"{name}: clear at rest.");
            AssertTextFits(card);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator AnUpgradeOffersCardsEachWearTheirTiersFrame()
        {
            yield return Load("foundry");
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            UpgradeService upgrades = setup.Upgrades;
            var view = Object.FindFirstObjectByType<RunChoiceView>();
            Assert.That(view.PaintsCards, Is.True, "The scene's set paints the cards.");
            ChoiceCardArtSet art = view.CardArt;

            // The panel, restyled once: the dim, the surface's frame, the title's plate and its crest.
            Transform panel = view.transform.Find("Choice Panel");
            Assert.That(panel.GetComponent<Image>().sprite, Is.SameAs(art.Backdrop));
            Assert.That(panel.GetComponent<Image>().raycastTarget, Is.True, "The dim still stops a tap reaching the run.");
            Transform surface = panel.Find("Safe Area/Choice Surface");
            Assert.That(surface.GetComponent<Image>().sprite, Is.SameAs(art.Surface));
            Assert.That(surface.GetComponent<Image>().pixelsPerUnitMultiplier, Is.EqualTo(1f));
            Image titlePlate = surface.Find("Painted Title Plate").GetComponent<Image>();
            RectTransform title = surface.Find("Choice Title").GetComponent<RectTransform>();
            Assert.That(titlePlate.transform.GetSiblingIndex(), Is.EqualTo(title.GetSiblingIndex() - 1), "Behind the title.");
            AssertEdges(titlePlate.rectTransform, title, art.TitlePlateMargin, "The title's plate");
            Assert.That(titlePlate.raycastTarget, Is.False);
            Assert.That(titlePlate.transform.Find("Painted Title Crest").GetComponent<Image>().sprite, Is.SameAs(art.TitleCrest));

            // Bonus offers until a three-card one has been seen; every card of each wears its tier's frame.
            var worn = new HashSet<Sprite>();
            bool stacked = false;
            for (int i = 0; i < 40 && !(stacked && worn.Count >= 2); i++)
            {
                if (upgrades.CurrentOffer == null)
                    setup.Run.GrantBonusUpgrade();
                UpgradeOffer offer = upgrades.CurrentOffer;
                Assert.That(offer, Is.Not.Null);
                Assert.That(view.IsOpen, Is.True);
                Canvas.ForceUpdateCanvases();
                for (int c = 0; c < offer.Choices.Count; c++)
                {
                    ChoiceCardArtSet.Treatment look = Expected(art, offer, c);
                    AssertPainted(view.CardButton(c), art, look, $"Offer {i} card {c} ({offer.RarityAt(c)})");
                    Assert.That(view.CardButton(c).GetComponentInChildren<Text>().color,
                        Is.EqualTo(RunChoiceView.RarityColour(offer.RarityAt(c))), "The name still wears its tier's colour.");
                    worn.Add(look.Frame);
                }
                if (offer.Choices.Count == 3)
                {
                    stacked = true;
                    Assert.That(((RectTransform)view.CardButton(2).transform).sizeDelta.y, Is.LessThan(252f),
                        "Three cards are stacked, shortened, and their parts with them.");
                }
                Assert.That(setup.Choices.TrySelect(setup.Choices.Current, 0), Is.True);
                yield return null;
            }
            Assert.That(stacked, Is.True, "A three-card offer came within forty.");
            Assert.That(worn.Count, Is.GreaterThanOrEqualTo(2), "Different tiers wore different frames.");
            for (int i = 0; i < 4 && setup.Choices.IsOpen; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheHallsTwoCardsKeepTheScenesRectsAndTheirIconsSitInTheirWells()
        {
            yield return Load("hall");
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<RunChoiceView>();
            ChoiceCardArtSet art = view.CardArt;
            // With the icon set every hall card shows its icon; a handful of offers is enough, before the pool runs short.
            bool iconSeen = false;
            for (int i = 0; i < 6 && !iconSeen; i++)
            {
                setup.Run.GrantBonusUpgrade();
                UpgradeOffer offer = setup.Upgrades.CurrentOffer;
                Assert.That(offer.Choices.Count, Is.EqualTo(2));
                Canvas.ForceUpdateCanvases();
                for (int c = 0; c < 2; c++)
                {
                    Button card = view.CardButton(c);
                    Assert.That(((RectTransform)card.transform).sizeDelta, Is.EqualTo(new Vector2(896f, 252f)), "The scene's size.");
                    AssertPainted(card, art, Expected(art, offer, c), $"Hall offer {i} card {c}");
                    Image icon = card.transform.Find("Choice Icon").GetComponent<Image>();
                    if (icon.enabled && icon.sprite != null)
                        iconSeen = true;
                }
                Assert.That(setup.Choices.TrySelect(setup.Choices.Current, 0), Is.True);
                yield return null;
            }
            Assert.That(iconSeen, Is.True, "A card's icon sits in its well.");
            // A tap through the icon and its well still reaches the card's button.
            setup.Run.GrantBonusUpgrade();
            yield return new WaitForSecondsRealtime(.3f);
            Canvas.ForceUpdateCanvases();
            Button first = view.CardButton(0);
            var pointer = new PointerEventData(EventSystem.current)
                { position = first.transform.Find("Choice Icon").GetComponent<RectTransform>().position };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(), Is.SameAs(first));
            for (int i = 0; i < 4 && setup.Choices.IsOpen; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
            LogAssert.NoUnexpectedReceived();
        }

        // A run seed whose offers draw every tier early (PaintedRunFeedbackTests: six gold cards in the first twenty offers).
        private const string EveryTierEarlySeed = "260";

        // The owner's 2026-11-12 ask: every card's item on its tier's ground - common stone grey, rare steel blue, epic
        // red, gold gilt - each tier its own, the untiered treatments none. A foundry run on a seed that draws every tier
        // early, its offers answered until each tier has been shown on a card that is not an evolution.
        [UnityTest, Timeout(120000)]
        public IEnumerator EveryTiersCardShowsItsItemOnItsTiersOwnGround()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RunSeedPath));
            File.WriteAllText(DevelopmentStart.RunSeedPath, EveryTierEarlySeed);
            yield return Load("foundry");
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            Assert.That(setup.Run.Seed, Is.EqualTo(int.Parse(EveryTierEarlySeed)), "The run plays the chosen seed.");
            UpgradeService upgrades = setup.Upgrades;
            var view = Object.FindFirstObjectByType<RunChoiceView>();
            Assert.That(view.PaintsCards, Is.True);
            ChoiceCardArtSet art = view.CardArt;

            var grounds = new HashSet<Sprite>();
            for (int t = 0; t < RarityTable.TierCount; t++)
            {
                Sprite ground = art.Tier((UpgradeRarity)t).Ground;
                Assert.That(ground, Is.Not.Null, $"{(UpgradeRarity)t} has an item ground.");
                Assert.That(ground.texture, Is.SameAs(art.Surface.texture), $"{(UpgradeRarity)t}: on the cards' one sheet.");
                Assert.That(ground.rect.width, Is.EqualTo(ground.rect.height), $"{(UpgradeRarity)t}: square, as the icon is.");
                grounds.Add(ground);
            }
            Assert.That(grounds.Count, Is.EqualTo(RarityTable.TierCount), "Each tier its own ground.");
            foreach (ChoiceCardArtSet.Treatment untiered in new[] { art.Evolution, art.Neutral, art.Extract, art.Climb })
                Assert.That(untiered.Ground == null, Is.True, "The untiered treatments keep the well's navy floor.");

            var seen = new bool[RarityTable.TierCount];
            int found = 0;
            for (int i = 0; i < 300 && found < seen.Length; i++)
            {
                if (upgrades.CurrentOffer == null)
                    setup.Run.GrantBonusUpgrade();
                yield return null;
                UpgradeOffer offer = upgrades.CurrentOffer;
                if (offer == null)
                    continue;
                Canvas.ForceUpdateCanvases();
                for (int c = 0; c < offer.Choices.Count; c++)
                {
                    if (offer.Choices[c].IsEvolution)
                        continue;
                    UpgradeRarity rarity = offer.RarityAt(c);
                    Button card = view.CardButton(c);
                    string name = $"Offer {i} card {c} ({rarity}, {offer.Choices[c].Id})";
                    AssertPainted(card, art, art.Tier(rarity), name);
                    Image ground = Part(card, "Painted Item Ground");
                    Assert.That(ground.sprite, Is.SameAs(art.Tier(rarity).Ground), $"{name}: its tier's ground.");
                    Assert.That(ground.enabled && ground.gameObject.activeInHierarchy, Is.True, $"{name}: on show.");
                    Assert.That(ground.canvasRenderer.GetAlpha(), Is.EqualTo(1f), $"{name}: at full strength.");
                    Image icon = card.transform.Find("Choice Icon").GetComponent<Image>();
                    Assert.That(icon.enabled && icon.sprite != null, Is.True, $"{name}: its item's icon stands on it.");
                    Assert.That(icon.transform.GetSiblingIndex(), Is.GreaterThan(ground.transform.GetSiblingIndex()),
                        $"{name}: the icon over its ground.");
                    if (!seen[(int)rarity])
                    {
                        seen[(int)rarity] = true;
                        found++;
                    }
                }
                Assert.That(setup.Choices.TrySelect(setup.Choices.Current, NotAnEvolution(offer)), Is.True);
            }
            Assert.That(found, Is.EqualTo(seen.Length), "Every tier came within three hundred offers.");
            for (int i = 0; i < 4 && setup.Choices.IsOpen; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
            LogAssert.NoUnexpectedReceived();
        }

        private static int NotAnEvolution(UpgradeOffer offer)
        {
            for (int c = 0; c < offer.Choices.Count; c++)
                if (!offer.Choices[c].IsEvolution)
                    return c;
            return 0;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator AnEvolutionCardWearsItsOwnTreatment()
        {
            yield return Load("foundry");
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            UpgradeService upgrades = setup.Upgrades;
            var view = Object.FindFirstObjectByType<RunChoiceView>();
            ChoiceCardArtSet art = view.CardArt;
            WeaponCard target = EvolvingWeapon(upgrades);
            Assert.That(target, Is.Not.Null, "The catalogue holds a weapon that evolves with one of its tomes.");
            bool seen = false;
            // Bonus offers answered the way a run chasing the weapon's evolution would, until one holds the evolution.
            for (int i = 0; i < 300 && !seen; i++)
            {
                if (upgrades.CurrentOffer == null)
                    setup.Run.GrantBonusUpgrade();
                yield return null;
                UpgradeOffer offer = upgrades.CurrentOffer;
                if (offer == null)
                    continue;
                for (int c = 0; c < offer.Choices.Count; c++)
                {
                    if (!offer.Choices[c].IsEvolution)
                        continue;
                    seen = true;
                    Canvas.ForceUpdateCanvases();
                    Button card = view.CardButton(c);
                    AssertPainted(card, art, art.Evolution, $"The evolution in slot {c}");
                    Assert.That(art.Evolution.Frame, Is.Not.SameAs(art.Tier(offer.RarityAt(c)).Frame),
                        "Its own frame, not its tier's.");
                    Assert.That(Part(card, "Painted Aura").enabled && Part(card, "Painted Crest").enabled, Is.True,
                        "Its heat, its horns and its crown of fire.");
                    Assert.That(view.CardKindText(c), Is.Not.Empty, "Its kind line still says what it is.");
                    for (int other = 0; other < offer.Choices.Count; other++)
                        if (other != c)
                            AssertPainted(view.CardButton(other), art, Expected(art, offer, other), $"Beside it, card {other}");
                }
                if (!seen)
                    Assert.That(setup.Choices.TrySelect(setup.Choices.Current, Preferred(offer, target)), Is.True);
            }
            Assert.That(seen, Is.True, "An evolution was offered within three hundred offers.");
            for (int i = 0; i < 4 && setup.Choices.IsOpen; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ForgeAndCheckpointCardsWearTheirOwnUntieredTreatments()
        {
            yield return Load("foundry");
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<RunChoiceView>();
            ChoiceCardArtSet art = view.CardArt;
            var tiers = new HashSet<Sprite>();
            for (int tier = 0; tier < RarityTable.TierCount; tier++)
                tiers.Add(art.Tier((UpgradeRarity)tier).Frame);
            tiers.Add(art.Evolution.Frame);

            Assert.That(setup.Forge.Open(new[]
            {
                new ForgeOption("forge_mend", "Mend", "Restore {0:0}% of your health", ForgeEffect.Heal, .4f),
                new ForgeOption("forge_temper", "Temper", "Gain {0:0} extra upgrade choice", ForgeEffect.BonusUpgrade, 1f)
            }), Is.True);
            Assert.That(setup.Choices.Current.Kind, Is.EqualTo(ChoiceKind.Forge));
            Canvas.ForceUpdateCanvases();
            for (int c = 0; c < 2; c++)
            {
                AssertPainted(view.CardButton(c), art, art.Neutral, $"Forge card {c}");
                Assert.That(tiers.Contains(view.CardButton(c).GetComponent<Image>().sprite), Is.False, "No tier's frame.");
            }
            Assert.That(setup.Choices.TrySelect(setup.Choices.Current, 0), Is.True, "Mend.");
            yield return null;

            Assert.That(setup.Checkpoint.Open(new[]
            {
                new CheckpointOption(CheckpointKind.Extract, "Extract", "Bank all 0 gold and end the run"),
                new CheckpointOption(CheckpointKind.Descend, "Climb", "Secure 0 gold")
            }), Is.True);
            Assert.That(setup.Choices.Current.Kind, Is.EqualTo(ChoiceKind.Checkpoint));
            Canvas.ForceUpdateCanvases();
            AssertPainted(view.CardButton(0), art, art.Extract, "Extract");
            AssertPainted(view.CardButton(1), art, art.Climb, "Climb");
            Assert.That(new HashSet<Sprite> { art.Extract.Frame, art.Climb.Frame, art.Neutral.Frame }.Count, Is.EqualTo(3),
                "Extract and Climb read apart, and apart from the forge.");
            Assert.That(tiers.Contains(art.Extract.Frame) || tiers.Contains(art.Climb.Frame), Is.False, "Neither claims a tier.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator AWaitingCardIsShadedAndAPressedOneLitTheFrameKeepingItsColours()
        {
            yield return Load("foundry");
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var view = Object.FindFirstObjectByType<RunChoiceView>();
            ChoiceCardArtSet art = view.CardArt;
            setup.Run.GrantBonusUpgrade();
            UpgradeOffer offer = setup.Upgrades.CurrentOffer;
            Button card = view.CardButton(0);
            var press = (Image)card.targetGraphic;
            Image frame = card.GetComponent<Image>();
            ChoiceCardArtSet.Treatment look = Expected(art, offer, 0);
            Assert.That(card.interactable, Is.False, "The card waits out the input delay.");
            AssertColour(press.canvasRenderer.GetColor(), art.DisabledColour, "A waiting card is shaded from the first frame");
            AssertColour(frame.canvasRenderer.GetColor(), Color.white, "Its frame keeps its own colours");

            yield return new WaitForSecondsRealtime(.45f);
            Assert.That(card.interactable, Is.True);
            Assert.That(press.canvasRenderer.GetColor().a, Is.LessThan(.02f), "Ready, the press is clear.");

            Canvas.ForceUpdateCanvases();
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = ((RectTransform)card.transform).position
            };
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(.25f);
            AssertColour(press.canvasRenderer.GetColor(), look.PressColour, "Under a finger the card is lit in its colour");
            AssertColour(frame.canvasRenderer.GetColor(), Color.white, "Its frame still keeps its own colours");
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(card.gameObject, pointer, ExecuteEvents.pointerExitHandler);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(press.canvasRenderer.GetColor().a, Is.LessThan(.02f), "Let go, the light goes out.");
            Assert.That(setup.Choices.IsOpen, Is.True, "A press without a click takes nothing.");

            // The first tap takes the card and shades the rest at once, as before.
            card.onClick.Invoke();
            Assert.That(setup.Upgrades.LastSelected, Is.SameAs(offer));
            for (int i = 0; i < 4 && setup.Choices.IsOpen; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator WithoutItsSetOrWithABrokenOneThePanelKeepsTheScenesLook()
        {
            var broken = ScriptableObject.CreateInstance<ChoiceCardArtSet>();
            try
            {
                foreach (ChoiceCardArtSet given in new[] { null, broken })
                {
                    string which = given == null ? "Without a set" : "With a broken set";
                    yield return Load("foundry", view => typeof(RunChoiceView).GetField("_cardArt", Private).SetValue(view, given));
                    var setup = Object.FindFirstObjectByType<CombatSetup>();
                    var choices = Object.FindFirstObjectByType<RunChoiceView>();
                    Assert.That(choices.PaintsCards, Is.False, which);
                    setup.Run.GrantBonusUpgrade();
                    UpgradeOffer offer = setup.Upgrades.CurrentOffer;
                    for (int c = 0; c < offer.Choices.Count; c++)
                    {
                        Button card = choices.CardButton(c);
                        var frame = card.GetComponent<Image>();
                        Assert.That(frame.sprite.name, Is.EqualTo("UI_Panel_ForgedBlank_v2"), $"{which}: the scene's card.");
                        Assert.That(frame.pixelsPerUnitMultiplier, Is.EqualTo(7f), which);
                        Assert.That(card.targetGraphic, Is.SameAs(frame), $"{which}: the card is still its button's graphic.");
                        Assert.That(card.colors.pressedColor, Is.EqualTo(new Color(.7f, .79f, .9f, 1f)), $"{which}: the scene's tints.");
                        Assert.That(card.colors.normalColor, Is.EqualTo(Color.white), which);
                        foreach (string part in Parts)
                            Assert.That(card.transform.Find(part), Is.Null, $"{which}: no {part}.");
                        Assert.That(card.GetComponentInChildren<Text>().color, Is.EqualTo(RunChoiceView.RarityColour(offer.RarityAt(c))),
                            $"{which}: the tier on the name, as before.");
                    }
                    Transform panel = choices.transform.Find("Choice Panel");
                    Color dim = panel.GetComponent<Image>().color;
                    AssertColour(dim, new Color(.02745098f, .03529412f, .07058824f, .8627451f), $"{which}: the scene's flat dim");
                    Transform surface = panel.Find("Safe Area/Choice Surface");
                    Assert.That(surface.GetComponent<Image>().sprite.name, Is.EqualTo("UI_Panel_ForgedBlank_v2"), which);
                    Assert.That(surface.Find("Painted Title Plate"), Is.Null, which);
                    for (int i = 0; i < 4 && setup.Choices.IsOpen; i++)
                        setup.Choices.TrySelect(setup.Choices.Current, 0);
                }
            }
            finally
            {
                Object.Destroy(broken);
            }
            LogAssert.NoUnexpectedReceived();
        }

        // The weapon the hero carries if it can evolve, else the first in the catalogue that can and whose tome is there
        // (as RunFeedbackFlowTests chases an evolution).
        private static WeaponCard EvolvingWeapon(UpgradeService upgrades)
        {
            WeaponCard found = null;
            string carried = upgrades.Loadout.At(0).WeaponId;
            for (int i = 0; i < upgrades.WeaponCards.Count; i++)
            {
                WeaponCard card = upgrades.WeaponCards[i];
                if (card.Evolution == null || !HasTome(upgrades, card.EvolvesWithTomeId))
                    continue;
                if (card.Id == carried)
                    return card;
                if (found == null)
                    found = card;
            }
            return found;
        }

        private static bool HasTome(UpgradeService upgrades, string id)
        {
            for (int i = 0; i < upgrades.Tomes.Count; i++)
                if (upgrades.Tomes[i].Id == id)
                    return true;
            return false;
        }

        private static int Preferred(UpgradeOffer offer, WeaponCard target)
        {
            int best = 0, bestScore = -1;
            for (int c = 0; c < offer.Choices.Count; c++)
            {
                int score = Score(offer.Choices[c], target);
                if (score > bestScore)
                {
                    best = c;
                    bestScore = score;
                }
            }
            return best;
        }

        // The weapon itself while it is not carried; one of its lines; its tome, new or a level; a general card; any
        // other level; another weapon; another tome last.
        private static int Score(UpgradeOption option, WeaponCard target)
        {
            if (option.IsEvolution)
                return 7;
            if (option.Id == target.Id)
                return 6;
            for (int l = 0; l < target.Lines.Count; l++)
                if (target.Lines[l].Id == option.Id)
                    return 5;
            if (option.Id == target.EvolvesWithTomeId)
                return 4;
            switch (option.Category)
            {
                case OfferCategory.General:
                    return 3;
                case OfferCategory.WeaponUpgrade:
                case OfferCategory.TomeUpgrade:
                    return 2;
                case OfferCategory.NewWeapon:
                    return 1;
                default:
                    return 0;
            }
        }
    }
}
