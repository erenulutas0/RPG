// Graphics-enabled PlayMode fixture (Run-AnvilUiProof.ps1 installs it for one run and removes it): the Forge's Anvil,
// Efsun and Bag tabs (docs/54 phase 4) in the game itself, on a seeded profile with gear (AnvilFixtures: every rarity,
// +0 to +9, the Sword at +7, materials and boosters), drawn at 1080x2340 (a 19.5:9 phone, 100 px of cutout under its top
// and 76 over its bottom, as the choice-rarity proof sets it), every overlay canvas through the camera above the world,
// on a Turkish culture so any number written in the phone's culture would show. The result screen with its loot line,
// the forge with its two new buttons, each tab, a strike mid-hold, a held strike and a missed one, a reroll shown beside
// the old lines, the bag with a pick, a melt asking and an inheritance chosen. frames.txt lists each frame and what it
// shows. A second pass, on a profile that has cleared floor 1 alone, follows a hero skill's way (frames 20-25,
// frames-skills.txt): the result's line when a run opens skills 2 and 3, the forge's skills card, the Skills tab, a skill
// forged, the card moved on, and the next run's toast. No scene or asset is saved; nothing here ships.
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Localisation;
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
    public sealed class AnvilUiCapture
    {
        // The capture's language (Run-AnvilUiProof.ps1 -Language tr sets CRYPTFORGE_LANGUAGE): English frames in
        // anvil-ui-01, Turkish ones beside them in anvil-ui-01-tr (docs/60).
        private static Language CaptureLanguage => LocalisationRuntime.CaptureLanguage(out Language language) ? language : Language.English;
        private static string Output => CaptureLanguage == Language.Turkish ? "ArtDirection/2026-11-17/anvil-ui-01-tr/" : "ArtDirection/2026-11-17/anvil-ui-01/";
        private Language? _language;
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const int Width = 1080;
        private const int Height = 2340;
        private const int TopInset = 100;
        private const int BottomInset = 76;
        private const int OverWorld = 10000;

        private Scene _scene;
        private Camera _camera;
        private CombatSetup _setup;
        private RelicForgeView _forge;
        private AnvilView _anvil;
        private CultureInfo _culture;
        private readonly StringBuilder _frames = new StringBuilder();

        [SetUp]
        public void SpeakTheCaptureLanguage()
        {
            _language = Loc.Override;
            Loc.Override = CaptureLanguage;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureTheAnvilsTabs()
        {
            _culture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            // The ring at +8 struck with a Seal (30 %) holds on the first draw; the amulet at +7 (30 %) misses on the second.
            int seed = 1;
            while (!(AnvilRule.Roll(seed, 0) < 30 && AnvilRule.Roll(seed, 1) >= 30))
                seed++;
            TestProfile.Begin(AnvilFixtures.Profile(seed, extraItems: 14, ringLevel: 8));
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _scene = SceneManager.GetActiveScene();
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _forge = Object.FindFirstObjectByType<RelicForgeView>();
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            _camera = Camera.main;
            Directory.CreateDirectory(Output);
            _frames.Clear().Append("Each frame, at 1080x2340 on a tr-TR culture, the game in ")
                .Append(LanguageCodes.Code(CaptureLanguage)).Append(".\n");

            // A run's loot, then the end: the result's loot line.
            _setup.Loot.EliteFelled(1);
            _setup.Loot.BossFelled(1);
            _setup.Loot.FloorCleared(1);
            GameObject.Find("Vanguard").GetComponent<Health>().ApplyDamage(new DamageContext(100000f));
            yield return null;
            yield return new WaitForSecondsRealtime(.7f);
            Shoot("01-result-loot", "The result screen; the build label's last line: " + Object.FindFirstObjectByType<RunResultView>().LootLine);

            Tap(GameObject.Find("Forge Button").GetComponent<Button>());
            yield return new WaitForSecondsRealtime(.5f);
            _anvil = _forge.Anvil;
            Shoot("02-forge-entry", "The Relic Forge with Bag (left) and Anvil (right) beside its nameplate.");

            Tap(_anvil.AnvilEntry);
            yield return Settle();
            Shoot("03-anvil-sword", "Anvil: the Sword at +7 (Rare), " + _anvil.ChanceText + ", " + _anvil.BreakdownText);

            Tap(_anvil.PieceButton(3));
            Tap(_anvil.SealToggle);
            yield return Settle();
            Shoot("04-anvil-ring-seal", "Anvil: the Gold ring at +8, a Forge Seal on: " + _anvil.ChanceText + "; " + _anvil.CostText);

            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(_anvil.StrikeButton.gameObject, data, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(HoldToStrike.Seconds * .6f);
            Shoot("05-strike-holding", "The strike held for 0.24 s of its 0.4 s.");
            yield return new WaitForSecondsRealtime(HoldToStrike.Seconds * .6f);
            ExecuteEvents.Execute(_anvil.StrikeButton.gameObject, data, ExecuteEvents.pointerUpHandler);
            yield return new WaitForSecondsRealtime(.3f);
            Shoot("06-strike-held", "The strike held: " + _anvil.MomentText);
            yield return new WaitForSecondsRealtime(AnvilView.MomentSeconds);

            Tap(_anvil.PieceButton(2));
            yield return Settle();
            ExecuteEvents.Execute(_anvil.StrikeButton.gameObject, data, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(HoldToStrike.Seconds + .1f);
            ExecuteEvents.Execute(_anvil.StrikeButton.gameObject, data, ExecuteEvents.pointerUpHandler);
            yield return new WaitForSecondsRealtime(.3f);
            Shoot("07-strike-missed", "The amulet's strike missed: " + _anvil.MomentText + "; " + _anvil.MemoryText);
            yield return new WaitForSecondsRealtime(AnvilView.MomentSeconds);

            Tap(_anvil.TabButton(AnvilView.Tab.Efsun));
            yield return Settle();
            Shoot("08-efsun", "Efsun: the amulet's lines: " + _anvil.CurrentLinesText);
            Tap(_anvil.RerollWithGold);
            yield return Settle();
            Shoot("09-efsun-reroll", "A reroll shown beside the old: " + _anvil.NewLinesText);
            Tap(_anvil.TakeNew);
            yield return Settle();

            Tap(_anvil.TabButton(AnvilView.Tab.Bag));
            yield return Settle();
            Shoot("10-bag", "Bag: " + _anvil.BagCountText);
            ProfileGear gear = _setup.Profile.Gear;
            _anvil.PickBagItem(gear.FindItem(6));
            yield return Settle();
            Shoot("11-bag-pick", "A Gold amulet picked: " + _anvil.DetailText.Replace('\n', ' '));
            _anvil.PickBagItem(gear.FindItem(6));
            _anvil.PickBagItem(gear.FindItem(8));
            Tap(_anvil.MeltButton);
            yield return Settle();
            Shoot("12-bag-melt-asks", "Melt asks first: " + _anvil.MeltText.Replace('\n', ' '));
            _anvil.PickBagItem(gear.FindItem(8));
            _anvil.PickBagItem(gear.FindItem(7));
            Tap(_anvil.InheritButton);
            _anvil.PickBagItem(gear.FindItem(5));
            yield return Settle();
            Shoot("13-bag-inherit", "Inherit +5 from Bark Mail onto the Quicksilver Hauberk: " + _anvil.InheritText.Replace('\n', ' '));

            File.WriteAllText(Output + "frames.txt", _frames.ToString());
            LogAssert.NoUnexpectedReceived();
        }

        // The way to a hero skill (docs/57 sections 5.2 and 14, docs/63 section 12 answer 7): a run whose clears open skills
        // 2 and 3 and its result's line; the Relic Forge's skills card in the band over its start button; the Skills tab it
        // opens; skill 2 forged there and the card moved on to skill 3; the next run's HUD with its toast. frames-skills.txt
        // lists them.
        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureTheSkillsWay()
        {
            _culture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            TestProfile.Begin(new PlayerProfile(700, null, null, 1));
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.timeScale = 1;
            yield return LoadForCapture();
            Directory.CreateDirectory(Output);
            _frames.Clear().Append("Each frame, at 1080x2340 on a tr-TR culture, the game in ")
                .Append(LanguageCodes.Code(CaptureLanguage)).Append(".\n");

            // The run's clears of floors 2 and 3, the profile's first: its result names both skills over the forge's hint.
            _setup.Profile.RecordFloorCleared(2);
            _setup.Profile.RecordFloorCleared(3);
            GameObject.Find("Vanguard").GetComponent<Health>().ApplyDamage(new DamageContext(100000f));
            yield return null;
            yield return new WaitForSecondsRealtime(.7f);
            Shoot("20-result-skills-opened", "The result's skill line: " + Object.FindFirstObjectByType<RunResultView>().SkillLine);

            Tap(GameObject.Find("Forge Button").GetComponent<Button>());
            yield return new WaitForSecondsRealtime(.5f);
            _anvil = _forge.Anvil;
            Shoot("21-forge-skills-card", "The Relic Forge's skills card in the band over its start button: " +
                _forge.SkillsHeader.text + " / " + _forge.SkillsCardText);

            Tap(_forge.SkillsCard);
            yield return Settle();
            Shoot("22-skills-tab", "The Skills tab the card opens: " + _anvil.SkillsHeroText);
            Tap(_anvil.SkillCardButton(2));
            yield return Settle();
            Shoot("23-skills-tab-forged", "Skill 2 forged: " + _anvil.SkillCardText(2));
            Tap(_anvil.TabButton(AnvilView.Tab.Forge));
            yield return new WaitForSecondsRealtime(.5f);
            Shoot("24-forge-skills-card-next", "Back on the forge, the card moved on: " + _forge.SkillsHeader.text + " / " +
                _forge.SkillsCardText);

            // The next run carries it: once live, the toast names it and when to tap it, over the buttons' arc.
            Tap(GameObject.Find("Start Run Button").GetComponent<Button>());
            yield return new WaitForSecondsRealtime(.3f);
            yield return LoadedAgain();
            _setup.HeroStats.AddModifier(UpgradeStat.MaxHealth, new StatModifier(ModifierOperation.Flat, 1000000f));
            GameObject.Find("Vanguard").GetComponent<Health>().Heal(1000000f);
            var toast = Object.FindFirstObjectByType<SkillToastView>();
            float deadline = Time.realtimeSinceStartup + 6f;
            while (!toast.IsShowing && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(toast.IsShowing, Is.True, "The toast showed.");
            yield return new WaitForSecondsRealtime(.6f);
            Shoot("25-run-skill-toast", "The first run with Iron Resolve: " + toast.ShownLine);

            File.WriteAllText(Output + "frames-skills.txt", _frames.ToString());
        }

        private IEnumerator LoadForCapture()
        {
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            FindForCapture();
        }

        // After the start button's reload: the new scene's objects, two frames on so every view has started.
        private IEnumerator LoadedAgain()
        {
            yield return null;
            yield return null;
            FindForCapture();
        }

        private void FindForCapture()
        {
            _scene = SceneManager.GetActiveScene();
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _forge = Object.FindFirstObjectByType<RelicForgeView>();
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            _camera = Camera.main;
        }

        private static IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(AnvilView.InputDelay + .1f);
        }

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        // The phone's safe area on every safe area of the scene, and every overlay canvas drawn through the camera into a
        // texture the phone's size (each canvas's scale moved and put back so it measures itself against that texture, as
        // ChoiceRarityRuntimeCapture does); then everything put back.
        private void Shoot(string file, string what)
        {
            _frames.Append(file).Append(": ").Append(what).Append('\n');
            Canvas.ForceUpdateCanvases();
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
                // The canvases have measured themselves at the phone's size; the Anvil fits the safe area it now sees
                // (its Update fitted the batch run's small screen) and the canvases measure again.
                foreach (var anvil in Object.FindObjectsByType<AnvilView>(FindObjectsSortMode.None))
                    anvil.FitNow();
                Canvas.ForceUpdateCanvases();
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
            Loc.Override = _language;
            if (_culture != null)
                Thread.CurrentThread.CurrentCulture = _culture;
            Time.timeScale = 1;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Anvil capture cleanup " + System.Guid.NewGuid().ToString("N")));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
