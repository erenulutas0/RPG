// Graphics-enabled PlayMode fixture (Run-TierUiProof.ps1 installs it for one run and removes it): the difficulty tier row
// on the hero and map choice (docs/54 phase 6, section 21) in the game itself, drawn at 1080x2340 (a 19.5:9 phone, 100 px
// of cutout under its top and 76 over its bottom, as the Anvil's and the choice-rarity proofs set it), every overlay canvas
// through the camera above the world, on a Turkish culture so any number written in the phone's culture would show. A fresh
// profile (Forged lit, the three tiers above it locked with their hints); a geared profile that walked out of Forged's
// Descent (Tempered open), then Tempered chosen (the deeper maps locked on Tempered, the line on the tier with the hero's
// gear); the run it starts (the HUD's floor line naming Tempered) and its result (the title naming it); a profile with every
// tier open and Abyssal chosen. frames.txt lists each frame and what it shows. The map choice's language toggle stands in
// its title's band; with -Language tr the game speaks Turkish (docs/60 pass 2) and the frames land in tier-ui-01-tr. No
// scene or asset is saved; nothing here ships.
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
    public sealed class TierUiCapture
    {
        // The capture's language (Run-TierUiProof.ps1 -Language tr sets CRYPTFORGE_LANGUAGE): English frames in tier-ui-01,
        // Turkish ones beside them in tier-ui-01-tr (docs/60 pass 2).
        private static Language CaptureLanguage => LocalisationRuntime.CaptureLanguage(out Language language) ? language : Language.English;
        private static string Output => CaptureLanguage == Language.Turkish ? "ArtDirection/2026-11-17/tier-ui-01-tr/" : "ArtDirection/2026-11-17/tier-ui-01/";
        private Language? _language;
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const int Width = 1080;
        private const int Height = 2340;
        private const int TopInset = 100;
        private const int BottomInset = 76;
        private const int OverWorld = 10000;

        private Camera _camera;
        private CultureInfo _culture;
        private readonly StringBuilder _frames = new StringBuilder();

        [SetUp]
        public void SpeakTheCaptureLanguage()
        {
            _language = Loc.Override;
            Loc.Override = CaptureLanguage;
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator CaptureTheTierChoice()
        {
            _culture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            Directory.CreateDirectory(Output);
            _frames.Clear().Append("Each frame, at 1080x2340 on a tr-TR culture, the game in ")
                .Append(LanguageCodes.Code(CaptureLanguage)).Append(".\n");

            // A fresh profile: Forged lit, Tempered, Inferno and Abyssal locked.
            yield return Load(new PlayerProfile());
            MapChoiceView view = Object.FindFirstObjectByType<MapChoiceView>();
            yield return Settle();
            Shoot("01-fresh", "A fresh profile: " + Row(view) + " | " + view.TierDetailText.Replace('\n', ' '));

            // The Anvil's geared profile (the Sword +7, a +3 armour, a +7 amulet, a +9 ring) that walked out of Forged's Descent.
            PlayerProfile veteran = AnvilFixtures.Profile();
            veteran.RecordFloorCleared(DescentStart.FinaleFloor);
            veteran.RecordDescentCompleted(DifficultyTier.Forged);
            yield return Load(veteran);
            view = Object.FindFirstObjectByType<MapChoiceView>();
            yield return Settle();
            Shoot("02-forged-done", "Forged's Descent completed: " + Row(view) + " | " + view.TierDetailText.Replace('\n', ' '));
            Tap(view.TierCardButton(1));
            yield return Settle();
            Shoot("03-tempered-chosen", "Tempered chosen: " + Row(view) + " | maps open " + view.IsMapOpenAt(0) + "/" +
                                        view.IsMapOpenAt(1) + "/" + view.IsMapOpenAt(2) + " (" + view.MapStateAt(1) + ") | " +
                                        view.TierDetailText.Replace('\n', ' '));

            // The Foundry on Tempered: the scene reloads with the tier.
            CombatSetup before = Object.FindFirstObjectByType<CombatSetup>();
            Tap(view.CardButton(0));
            float deadline = Time.realtimeSinceStartup + 30f;
            CombatSetup setup = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                setup = before == null ? Object.FindFirstObjectByType<CombatSetup>() : null;
                if (setup != null && setup.Run != null)
                    break;
                yield return null;
            }
            Assert.That(setup, Is.Not.Null, "The scene loaded again on Tempered.");
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            _camera = Camera.main;
            yield return new WaitForSecondsRealtime(1.5f);
            Shoot("04-hud-tempered", "The run on " + setup.Tier + ": the HUD's floor line \"" +
                                     GameObject.Find("Floor Label").GetComponent<Text>().text.Replace('\n', ' ') + "\"");
            GameObject.Find("Vanguard").GetComponent<Health>().ApplyDamage(new DamageContext(100000f));
            yield return null;
            yield return new WaitForSecondsRealtime(.7f);
            Shoot("05-result-tempered", "The result: \"" + GameObject.Find("Result Title").GetComponent<Text>().text + "\"");

            // Every tier open, Abyssal chosen.
            PlayerProfile master = AnvilFixtures.Profile(swordLevel: 9);
            master.RecordFloorCleared(DescentStart.FinaleFloor);
            foreach (DifficultyTier tier in new[] { DifficultyTier.Forged, DifficultyTier.Tempered, DifficultyTier.Inferno })
            {
                master.RecordFloorCleared(DescentStart.FinaleFloor, tier);
                master.RecordDescentCompleted(tier);
            }
            master.SelectTier(DifficultyTier.Abyssal);
            yield return Load(master);
            view = Object.FindFirstObjectByType<MapChoiceView>();
            yield return Settle();
            Shoot("06-abyssal-chosen", "Every tier open, Abyssal chosen: " + Row(view) + " | " + view.TierDetailText.Replace('\n', ' '));

            File.WriteAllText(Output + "frames.txt", _frames.ToString());
            LogAssert.NoUnexpectedReceived();
        }

        private IEnumerator Load(PlayerProfile profile)
        {
            TestProfile.Begin(profile);
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.Delete(DevelopmentStart.MapChoicePath);
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _camera = Camera.main;
        }

        private static string Row(MapChoiceView view)
        {
            var text = new StringBuilder();
            for (int i = 0; i < view.TierCardCount; i++)
                text.Append(i == 0 ? string.Empty : "; ").Append(view.TierNameAt(i)).Append(" (").Append(view.TierStateAt(i)).Append(')');
            return text.ToString();
        }

        private static IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(MapChoiceView.InputDelay + .3f);
        }

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        // The phone's safe area on every safe area of the scene, and every overlay canvas drawn through the camera into a
        // texture the phone's size (each canvas's scale moved and put back so it measures itself against that texture, as
        // AnvilUiCapture does); the map choice fits the safe area it then sees (MapChoiceView.FitNow, as AnvilView.FitNow:
        // its Update fitted the batch run's small screen); then everything put back.
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
                foreach (var choice in Object.FindObjectsByType<MapChoiceView>(FindObjectsSortMode.None))
                    choice.FitNow();
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
            RunStartRequest.Clear();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Tier capture cleanup"));
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            TestProfile.End();
        }
    }
}
