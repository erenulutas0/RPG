// Graphics-enabled PlayMode fixture (Run-ChoiceRarityProof.ps1 installs it for one run and removes it): the level-up
// panel's cards in the game itself, each card's item on its tier's ground (ChoiceCardPainter's Painted Item Ground:
// common stone grey, rare steel blue, epic red, gold gilt). A foundry run on seed 260, whose offers draw every tier early
// (PaintedRunFeedbackTests), its offers granted and answered at once (the first card that is not an evolution) until
// every tier has been on show: each offer that brings a tier not yet shot is drawn at 1080x2340 (a 19.5:9 phone, 100 px
// of cutout under its top and 76 over its bottom as the room-pip proof sets a 20:9 one), every overlay canvas through the
// camera above the world. Then two staged frames, so all four tiers sit side by side: one real three-card offer shown
// again with its tiers set to common, rare and epic, and to gold, epic and rare (the icons, names and descriptions are
// the real offer's; only the tiers are staged, so a staged card's numbers are its drawn tier's). cards.txt lists every
// frame's cards. A second run on the hall stages a two-card offer holding Tempered Edge or Quickened Grip on epic and
// gold, their icons the set's squareless repaints (cards-hall.txt). No scene or asset is saved; nothing here ships.
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Cryptforge.Art;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    public sealed class ChoiceRarityRuntimeCapture
    {
        private const string Output = "ArtDirection/2026-11-12/choice-rarity-01/runtime/";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string Seed = "260";
        private const int Width = 1080;
        private const int Height = 2340;
        private const int TopInset = 100;
        private const int BottomInset = 76;
        // Every world renderer sorts below this; the canvases drawn through the camera keep their own order above it.
        private const int OverWorld = 10000;
        private static readonly string[] TierNames = { "common", "rare", "epic", "gold" };

        private Scene _scene;
        private Camera _camera;
        private CombatSetup _setup;
        private RunChoiceView _view;
        private ChoiceCardArtSet _art;
        private readonly StringBuilder _cards = new StringBuilder();

        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureEveryTiersCard()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RunSeedPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.WriteAllText(DevelopmentStart.RunSeedPath, Seed);
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _scene = SceneManager.GetActiveScene();
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _view = Object.FindFirstObjectByType<RunChoiceView>();
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            _camera = Camera.main;
            Assert.That(_setup.Run.Seed, Is.EqualTo(int.Parse(Seed)), "The run plays the chosen seed.");
            Assert.That(_view.PaintsCards, Is.True, "The painted cards.");
            _art = _view.CardArt;
            Directory.CreateDirectory(Output);
            _cards.Clear().Append("Each frame's cards, top to bottom: tier, id, and the item ground's sprite.\n");

            UpgradeService upgrades = _setup.Upgrades;
            var shot = new bool[RarityTable.TierCount];
            int found = 0;
            UpgradeOffer staging = null;
            ChoicePrompt stagingPrompt = null;
            for (int i = 0; i < 300 && (found < shot.Length || staging == null); i++)
            {
                if (upgrades.CurrentOffer == null)
                    _setup.Run.GrantBonusUpgrade();
                yield return null;
                UpgradeOffer offer = upgrades.CurrentOffer;
                if (offer == null)
                    continue;
                bool fresh = false, evolution = false;
                for (int c = 0; c < offer.Choices.Count; c++)
                {
                    if (offer.Choices[c].IsEvolution)
                    {
                        evolution = true;
                        continue;
                    }
                    fresh |= !shot[(int)offer.RarityAt(c)];
                }
                if (fresh && found < shot.Length)
                {
                    // Past the input delay, so the cards are shown as a finger finds them, not shaded.
                    yield return new WaitForSecondsRealtime(.4f);
                    var tiers = new StringBuilder();
                    for (int c = 0; c < offer.Choices.Count; c++)
                    {
                        UpgradeRarity rarity = offer.RarityAt(c);
                        tiers.Append('-').Append(offer.Choices[c].IsEvolution ? "evolution" : TierNames[(int)rarity]);
                        if (!offer.Choices[c].IsEvolution && !shot[(int)rarity])
                        {
                            shot[(int)rarity] = true;
                            found++;
                        }
                    }
                    Shoot("offer-" + i.ToString("00", CultureInfo.InvariantCulture) + tiers, offer, null);
                }
                if (found == shot.Length && !evolution && offer.Choices.Count == 3)
                {
                    staging = offer;
                    stagingPrompt = _setup.Choices.Current;
                    break;
                }
                Assert.That(_setup.Choices.TrySelect(_setup.Choices.Current, NotAnEvolution(offer)), Is.True);
            }
            Assert.That(staging, Is.Not.Null, "A three-card offer without an evolution came.");

            // The staged frames: the same offer shown again with its tiers set, so all four stand side by side.
            MethodInfo show = typeof(RunChoiceView).GetMethod("Show", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(show, Is.Not.Null);
            var sets = new[]
            {
                new[] { UpgradeRarity.Common, UpgradeRarity.Rare, UpgradeRarity.Epic },
                new[] { UpgradeRarity.Gold, UpgradeRarity.Epic, UpgradeRarity.Rare }
            };
            foreach (UpgradeRarity[] set in sets)
            {
                var cards = new List<ChoiceCard>();
                var tiers = new StringBuilder();
                for (int c = 0; c < 3; c++)
                {
                    cards.Add(new ChoiceCard(stagingPrompt.Cards[c].Name, stagingPrompt.Cards[c].Description, set[c]));
                    tiers.Append('-').Append(TierNames[(int)set[c]]);
                }
                show.Invoke(_view, new object[] { new ChoicePrompt(ChoiceKind.Upgrade, cards) });
                yield return new WaitForSecondsRealtime(.4f);
                Shoot("staged" + tiers, staging, set);
            }
            show.Invoke(_view, new object[] { _setup.Choices.Current });
            File.WriteAllText(Output + "cards.txt", _cards.ToString());
            Assert.That(found, Is.EqualTo(shot.Length), "Every tier was on show within three hundred offers.");
            for (int i = 0; i < 4 && _setup.Choices.IsOpen; i++)
                _setup.Choices.TrySelect(_setup.Choices.Current, NotAnEvolution(upgrades.CurrentOffer));
            LogAssert.NoUnexpectedReceived();
        }

        // The hall's two legacy cards, Tempered Edge and Quickened Grip, whose icons the set now paints without their navy
        // square: the hall's offers answered until one holds either, then that two-card offer shown again staged epic and
        // gold, and gold and epic, so each legacy icon stands on both grounds. cards-hall.txt lists the frames.
        [UnityTest, Timeout(300000)]
        public IEnumerator CaptureTheHallsLegacyCardsOnEpicAndGold()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RunSeedPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "hall");
            File.WriteAllText(DevelopmentStart.RunSeedPath, Seed);
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _scene = SceneManager.GetActiveScene();
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _view = Object.FindFirstObjectByType<RunChoiceView>();
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            _camera = Camera.main;
            Assert.That(_view.PaintsCards, Is.True, "The painted cards.");
            _art = _view.CardArt;
            Directory.CreateDirectory(Output);
            _cards.Clear().Append("The hall's legacy cards, staged on the epic and gold grounds: tier, id, ground.\n");

            UpgradeService upgrades = _setup.Upgrades;
            UpgradeOffer legacy = null;
            for (int i = 0; i < 40 && legacy == null; i++)
            {
                if (upgrades.CurrentOffer == null)
                    _setup.Run.GrantBonusUpgrade();
                yield return null;
                UpgradeOffer offer = upgrades.CurrentOffer;
                if (offer == null)
                    continue;
                bool both = offer.Choices.Count == 2;
                bool holds = false;
                for (int c = 0; c < offer.Choices.Count; c++)
                    holds |= offer.Choices[c].Id == "upgrade_damage" || offer.Choices[c].Id == "upgrade_attack_speed";
                if (both && holds)
                {
                    legacy = offer;
                    break;
                }
                Assert.That(_setup.Choices.TrySelect(_setup.Choices.Current, 0), Is.True);
            }
            Assert.That(legacy, Is.Not.Null, "A hall offer held Tempered Edge or Quickened Grip within forty.");

            ChoicePrompt prompt = _setup.Choices.Current;
            MethodInfo show = typeof(RunChoiceView).GetMethod("Show", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(show, Is.Not.Null);
            foreach (UpgradeRarity[] set in new[]
                     {
                         new[] { UpgradeRarity.Epic, UpgradeRarity.Gold },
                         new[] { UpgradeRarity.Gold, UpgradeRarity.Epic }
                     })
            {
                var cards = new List<ChoiceCard>();
                var tiers = new StringBuilder();
                for (int c = 0; c < 2; c++)
                {
                    cards.Add(new ChoiceCard(prompt.Cards[c].Name, prompt.Cards[c].Description, set[c]));
                    tiers.Append('-').Append(TierNames[(int)set[c]]);
                }
                show.Invoke(_view, new object[] { new ChoicePrompt(ChoiceKind.Upgrade, cards) });
                yield return new WaitForSecondsRealtime(.4f);
                Shoot("hall-legacy" + tiers, legacy, set);
                for (int c = 0; c < 2; c++)
                {
                    string id = legacy.Choices[c].Id;
                    if (id != "upgrade_damage" && id != "upgrade_attack_speed")
                        continue;
                    Image icon = _view.CardButton(c).transform.Find("Choice Icon").GetComponent<Image>();
                    Assert.That(icon.sprite.name, Is.EqualTo("UI_UpgradeIcons_" + id), $"{id}: the set's squareless icon.");
                }
            }
            show.Invoke(_view, new object[] { _setup.Choices.Current });
            File.WriteAllText(Output + "cards-hall.txt", _cards.ToString());
            for (int i = 0; i < 4 && _setup.Choices.IsOpen; i++)
                _setup.Choices.TrySelect(_setup.Choices.Current, 0);
            LogAssert.NoUnexpectedReceived();
        }

        private static int NotAnEvolution(UpgradeOffer offer)
        {
            if (offer == null)
                return 0;
            for (int c = 0; c < offer.Choices.Count; c++)
                if (!offer.Choices[c].IsEvolution)
                    return c;
            return 0;
        }

        // Each shown card wears its tier's ground under its icon; written to cards.txt.
        private void Check(string file, UpgradeOffer offer, UpgradeRarity[] staged)
        {
            _cards.Append(file).Append('\n');
            for (int c = 0; c < offer.Choices.Count; c++)
            {
                Button card = _view.CardButton(c);
                Transform part = card.transform.Find("Painted Item Ground");
                Assert.That(part, Is.Not.Null, $"{file}: card {c} has its item ground.");
                Image ground = part.GetComponent<Image>();
                Image icon = card.transform.Find("Choice Icon").GetComponent<Image>();
                bool evolution = offer.Choices[c].IsEvolution;
                UpgradeRarity rarity = staged != null ? staged[c] : offer.RarityAt(c);
                Sprite expected = evolution ? _art.Evolution.Ground : _art.Tier(rarity).Ground;
                // Unity's equality: an untiered card's empty reference need not be the same object as the set's.
                Assert.That(ground.sprite == expected, Is.True, $"{file}: card {c}'s ground.");
                Assert.That(ground.enabled, Is.EqualTo(expected != null), $"{file}: card {c}'s ground on show only with a sprite.");
                Assert.That(icon.enabled && icon.sprite != null, Is.True, $"{file}: card {c} shows its item.");
                _cards.Append("  ").Append(evolution ? "evolution" : TierNames[(int)rarity]).Append(' ')
                    .Append(offer.Choices[c].Id).Append(' ')
                    .Append(ground.enabled && ground.sprite != null ? ground.sprite.name : "(navy floor)").Append('\n');
            }
        }

        // The phone's safe area on every safe area of the scene and every overlay canvas drawn through the camera into a
        // texture the phone's size (each canvas's scale moved and put back so it measures itself against that texture, as
        // RoomPipsRuntimeCapture does); then everything put back.
        private void Shoot(string file, UpgradeOffer offer, UpgradeRarity[] staged)
        {
            Canvas.ForceUpdateCanvases();
            Check(file, offer, staged);
            var fitters = Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var fitted = new bool[fitters.Length];
            var anchors = new Vector4[fitters.Length];
            var offsets = new Vector4[fitters.Length];
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var drawn = new bool[canvases.Length];
            var orders = new int[canvases.Length];
            var distances = new float[canvases.Length];
            var cameras = new Camera[canvases.Length];
            RenderTexture target = RenderTexture.GetTemporary(Width, Height, 24);
            RenderTexture active = RenderTexture.active;
            RenderTexture cameraTarget = _camera.targetTexture;
            float aspect = _camera.aspect;
            Texture2D pixels = null;
            try
            {
                for (int i = 0; i < fitters.Length; i++)
                {
                    var rect = (RectTransform)fitters[i].transform;
                    fitted[i] = fitters[i].enabled;
                    anchors[i] = new Vector4(rect.anchorMin.x, rect.anchorMin.y, rect.anchorMax.x, rect.anchorMax.y);
                    offsets[i] = new Vector4(rect.offsetMin.x, rect.offsetMin.y, rect.offsetMax.x, rect.offsetMax.y);
                    fitters[i].enabled = false;
                    rect.anchorMin = new Vector2(0f, BottomInset / (float)Height);
                    rect.anchorMax = new Vector2(1f, 1f - TopInset / (float)Height);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
                _camera.aspect = (float)Width / Height;
                _camera.targetTexture = target;
                for (int i = 0; i < canvases.Length; i++)
                {
                    Canvas canvas = canvases[i];
                    drawn[i] = canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay;
                    if (!drawn[i])
                        continue;
                    orders[i] = canvas.sortingOrder;
                    distances[i] = canvas.planeDistance;
                    cameras[i] = canvas.worldCamera;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = _camera;
                    canvas.planeDistance = 1f;
                    canvas.sortingOrder = orders[i] + OverWorld;
                    float scaleBefore = canvas.scaleFactor;
                    canvas.scaleFactor = scaleBefore * .5f;
                    canvas.scaleFactor = scaleBefore;
                }
                Canvas.ForceUpdateCanvases();
                Canvas root = _view.CardButton(0).GetComponentInParent<Canvas>().rootCanvas;
                Assert.That(root.renderMode, Is.EqualTo(RenderMode.ScreenSpaceCamera), $"{file}: the panel is drawn.");
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (!drawn[i] || !canvases[i].isActiveAndEnabled)
                        continue;
                    Vector2 covered = ((RectTransform)canvases[i].transform).rect.size * canvases[i].scaleFactor;
                    Assert.That(covered.x, Is.EqualTo(Width).Within(.5f), $"{file}: {canvases[i].name} covers the frame.");
                    Assert.That(covered.y, Is.EqualTo(Height).Within(.5f), $"{file}: {canvases[i].name} covers the frame.");
                }
                UnityEngine.Rendering.SortingGroup.UpdateAllSortingGroups();
                _camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                pixels.Apply();
                WriteBmp(Output + file + ".bmp", pixels.GetPixels32(), Width, Height);
            }
            finally
            {
                if (pixels != null)
                    Object.Destroy(pixels);
                _camera.targetTexture = cameraTarget;
                _camera.aspect = aspect;
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(target);
                for (int i = 0; i < canvases.Length; i++)
                {
                    if (!drawn[i] || canvases[i] == null)
                        continue;
                    canvases[i].renderMode = RenderMode.ScreenSpaceOverlay;
                    canvases[i].worldCamera = cameras[i];
                    canvases[i].planeDistance = distances[i];
                    canvases[i].sortingOrder = orders[i];
                }
                for (int i = 0; i < fitters.Length; i++)
                {
                    if (fitters[i] == null)
                        continue;
                    var rect = (RectTransform)fitters[i].transform;
                    rect.anchorMin = new Vector2(anchors[i].x, anchors[i].y);
                    rect.anchorMax = new Vector2(anchors[i].z, anchors[i].w);
                    rect.offsetMin = new Vector2(offsets[i].x, offsets[i].y);
                    rect.offsetMax = new Vector2(offsets[i].z, offsets[i].w);
                    fitters[i].enabled = fitted[i];
                }
            }
        }

        // A 24-bit bottom-up BMP of a bottom-up frame.
        private static void WriteBmp(string path, Color32[] pixels, int width, int rows)
        {
            int stride = (width * 3 + 3) & ~3;
            int bytes = stride * rows;
            var row = new byte[stride];
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write((ushort)0x4d42); writer.Write(bytes + 54); writer.Write(0); writer.Write(54);
                writer.Write(40); writer.Write(width); writer.Write(rows); writer.Write((ushort)1); writer.Write((ushort)24);
                writer.Write(0); writer.Write(bytes); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color32 pixel = pixels[y * width + x];
                        row[x * 3] = pixel.b;
                        row[x * 3 + 1] = pixel.g;
                        row[x * 3 + 2] = pixel.r;
                    }
                    writer.Write(row);
                }
            }
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Choice rarity capture cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
