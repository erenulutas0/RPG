// Graphics-enabled PlayMode fixture (Run-RewardOfferProof.ps1 installs it for one run and removes it): the result screen's
// rewarded offer and the "Skip reward videos" product (docs/58 7.4) in the game itself, over the offline fakes, drawn at
// 1080x2340 (a 19.5:9 phone, 100 px of cutout under its top and 76 over its bottom, as the tier and Anvil proofs set it),
// every overlay canvas through the camera above the world, on a Turkish culture so any number written in the phone's
// culture would show. In English, then in Turkish (docs/60): the offer with a video ready (Video), the owner's skip (Skip),
// no video ready (NoAd, development/ads.txt "noad"), the bonus credited after a watched video, a failed video's retry
// (ads.txt "failed"), and the pause overlay's entry beside the language toggle. Each from a fresh profile whose ledger
// already counts one completed run, so the screen is the second. frames.txt lists each frame and what it shows. No scene
// or asset is saved; nothing here ships.
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Monetisation;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    public sealed class RewardOfferCapture
    {
        private const string Output = "ArtDirection/2026-11-23/reward-offer-01/";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const int RunGold = 120;
        private const int Width = 1080;
        private const int Height = 2340;
        private const int TopInset = 100;
        private const int BottomInset = 76;
        private const int OverWorld = 10000;

        private Language? _language;
        private Camera _camera;
        private CultureInfo _culture;
        private CombatSetup _setup;
        private RunResultView _result;
        private readonly StringBuilder _frames = new StringBuilder();

        [SetUp]
        public void RememberTheLanguage()
        {
            _language = Loc.Override;
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator CaptureTheRewardOffer()
        {
            _culture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            Directory.CreateDirectory(Output);
            _frames.Clear().Append("Each frame, at 1080x2340 on a tr-TR culture; the run banked ").Append(RunGold)
                .Append(" gold (bonus +").Append(RewardOfferRule.BonusGold(RunGold)).Append(").\n");

            foreach (Language language in new[] { Language.English, Language.Turkish })
            {
                Loc.Override = language;
                string code = LanguageCodes.Code(language);

                // A video ready, then watched: the bonus credited and shown.
                yield return Load(false, null);
                yield return EndTheRun();
                Shoot(code + "-01-video", "Video: \"" + _result.OfferButtonText + "\" | link \"" + _result.SkipLink.ShownText + "\"");
                yield return WaitOutTheDelay();
                Tap(_result.OfferButton);
                yield return null;
                Shoot(code + "-04-credited", "Credited: \"" + _result.OfferNoteText + "\" | gold \"" +
                                             GameObject.Find("Result Gold Label").GetComponent<Text>().text + "\"");

                // The owner of the skip: the same bonus without a video.
                yield return Load(true, null);
                yield return EndTheRun();
                Shoot(code + "-02-skip", "Skip: \"" + _result.OfferButtonText + "\" (no link for an owner)");

                // No video ready.
                yield return Load(false, DevelopmentStart.AdsNoAd);
                yield return EndTheRun();
                Shoot(code + "-03-noad", "NoAd: \"" + _result.OfferButtonText + "\" | link \"" + _result.SkipLink.ShownText + "\"");

                // A failed video: one retry on the same screen.
                yield return Load(false, DevelopmentStart.AdsFailed);
                yield return EndTheRun();
                yield return WaitOutTheDelay();
                Tap(_result.OfferButton);
                yield return null;
                Shoot(code + "-05-failed-retry", "Failed, the retry: \"" + _result.OfferButtonText + "\" | \"" +
                                                 _result.OfferNoteText + "\"");

                // The pause overlay's settings: the skip above the language toggle.
                yield return Load(false, null);
                Assert.That(_setup.Pause.TryPause(), Is.True);
                yield return new WaitForSecondsRealtime(.6f);
                PauseView pause = Object.FindFirstObjectByType<PauseView>();
                Shoot(code + "-06-pause", "Pause: \"" + pause.SkipVideos.ShownText + "\" above \"" +
                                          pause.LanguageToggle.ShownText + "\"");
            }

            File.WriteAllText(Output + "frames.txt", _frames.ToString());
            LogAssert.NoUnexpectedReceived();
        }

        // A fresh profile whose ledger counts one completed run, the skip cached when owned, development/ads.txt when named;
        // the foundry's gameplay scene with its run started at once.
        private IEnumerator Load(bool ownsSkip, string ads)
        {
            yield return Leave();
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            var ledger = new RewardLedger();
            ledger.RecordCompletedRun("earlier");
            new RewardLedgerStore(TestProfile.Folder).Save(ledger);
            if (ownsSkip)
            {
                var owned = new EntitlementCacheData();
                owned.Grant(StoreProducts.SkipRewardVideos);
                new EntitlementCache(TestProfile.Folder).Save(owned);
            }
            if (ads != null)
                File.WriteAllText(DevelopmentStart.AdsPath, ads);
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _camera = Camera.main;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _result = Object.FindFirstObjectByType<RunResultView>();
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            yield return new WaitForSecondsRealtime(1f);
        }

        private IEnumerator EndTheRun()
        {
            _setup.Run.AddGold(Math.Max(0, RunGold - _setup.Run.Gold));
            _setup.Run.End(RunOutcome.Extracted);
            yield return null;
            yield return new WaitForSecondsRealtime(.7f);
        }

        private static IEnumerator WaitOutTheDelay()
        {
            yield return new WaitForSecondsRealtime(.2f);
        }

        private static IEnumerator Leave()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded && scene.path == ScenePath)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Reward offer capture " + Guid.NewGuid().ToString("N")));
                yield return SceneManager.UnloadSceneAsync(scene);
            }
            TestProfile.End();
        }

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        // The phone's safe area on every safe area of the scene, and every overlay canvas drawn through the camera into a
        // texture the phone's size (each canvas's scale moved and put back so it measures itself against that texture, as
        // TierUiCapture does); then everything put back.
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
            yield return Leave();
        }
    }
}
