// Graphics-enabled PlayMode fixture (Run-ArenaProof.ps1 installs it for one run and removes it): P0's arena screen
// (docs/55 section 11) in the game itself, opened over the map choice as its button will open it, on a seeded profile with
// gear (AnvilFixtures, so the open arena's powers are real) and an arena save of a fixed seed, so every run draws the same
// rivals and the same duel. Drawn at 1080x2340 (a 19.5:9 phone, 100 px of cutout under its top and 76 over its bottom, as
// the Anvil's capture sets it), every overlay canvas through the camera above the world, on a Turkish culture so any number
// written in the phone's culture would show. The build, the training ladder, the open arena's offers, three moments of
// the duel - a strike landing, a skill, a round's end - and the result, each frame named by its language (en- or tr-).
// frames-<language>.txt lists each frame and what it shows. No scene or asset is saved; nothing here ships.
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Cryptforge.Arena;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Save;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    public sealed class ArenaViewCapture
    {
        private static Language CaptureLanguage => LocalisationRuntime.CaptureLanguage(out Language language) ? language : Language.English;
        private static string Prefix => LanguageCodes.Code(CaptureLanguage) + "-";
        private const string Output = "ArtDirection/2026-11-19/arena-view-01/";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // The arena save's seed: its rivals and its first fight's duel, the same on every run.
        private const int ArenaSeed = 20261119;
        private const int Width = 1080;
        private const int Height = 2340;
        private const int TopInset = 100;
        private const int BottomInset = 76;
        private const int OverWorld = 10000;

        private Language? _language;
        private Scene _scene;
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
        public IEnumerator CaptureTheArena()
        {
            _culture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
            TestProfile.Begin(AnvilFixtures.Profile());
            File.Delete(DevelopmentStart.MapChoicePath);
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            new ArenaSaveStore(ProfileLocation.Resolve()).Save(ArenaLadder.NewSave(ArenaSeed));
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _scene = SceneManager.GetActiveScene();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            _camera = Camera.main;
            Directory.CreateDirectory(Output);
            _frames.Clear().Append("Each frame, at 1080x2340 on a tr-TR culture, the game in ")
                .Append(LanguageCodes.Code(CaptureLanguage)).Append(". Arena save seed ").Append(ArenaSeed).Append(".\n");

            ArenaScreen screen = ArenaScreen.Open(setup);
            yield return Settle();
            screen.ToggleLine(ArenaStat.AttackSpeed);
            yield return Settle();
            Shoot(screen, "01-build", "The build: the Vanguard, the Sword, three efsun line types, the autopilot.");

            screen.ConfirmBuild();
            // A rival of the other hero, when the three offered hold one: both heroes' skills in the duel.
            int pick = 0;
            for (int i = 0; i < screen.Opponents.Count; i++)
            {
                if (screen.SaveData.ranked.rivals[screen.Opponents[i]].build.heroId == ArenaRules.Ilya)
                    pick = i;
            }
            screen.Select(pick);
            yield return Settle();
            Shoot(screen, "02-ladder-training", "The training ladder: " + screen.StatusText.Replace('\n', ' ') + "; card " + pick + " chosen.");

            screen.ShowMode(ArenaMode.Open);
            yield return Settle();
            Shoot(screen, "03-ladder-open", "The open arena's three offers: " + screen.CardText(0).Length + " / " + screen.CardText(1).Length +
                                             " / " + screen.CardText(2).Length + " characters; level chosen.");

            screen.ShowMode(ArenaMode.Ranked);
            screen.Select(pick);
            screen.FightSelected();
            // The capture moves the duel itself: the screen's own clock stops.
            screen.enabled = false;
            DuelLog log = screen.LastLog;
            DuelPlaybackView playback = screen.Playback;
            DuelTimeline timeline = playback.State.Timeline;

            DuelEvent strike = Find(log, e => e.Kind == DuelEventKind.Strike && e.Round == 0 && (e.Aux & 1) != 0 && e.Value > 0) ??
                               Find(log, e => e.Kind == DuelEventKind.Strike && e.Round == 0 && e.Value > 0) ?? default;
            playback.Seek(timeline.RoundStart(0) + DuelTimeline.IntroTicks + strike.Tick + 4);
            yield return null;
            Shoot(screen, "04-playback-strike", $"A strike landing in round 1, tick {strike.Tick}: side {strike.Side} hits for {DuelNumbers.Points(strike.Value)}" +
                                                ((strike.Aux & 1) != 0 ? " (a crit)" : string.Empty) + ".");

            DuelEvent skill = Find(log, e => e.Kind == DuelEventKind.Skill && e.Effect == ArenaEffect.FlameSigil) ??
                              Find(log, e => e.Kind == DuelEventKind.Skill && e.Effect == ArenaEffect.IronResolve) ??
                              Find(log, e => e.Kind == DuelEventKind.Skill) ?? default;
            playback.Seek(timeline.RoundStart(skill.Round) + DuelTimeline.IntroTicks + skill.Tick + 10);
            yield return null;
            Shoot(screen, "05-playback-skill", $"A skill: {skill.Effect} by side {skill.Side} in round {skill.Round + 1}, tick {skill.Tick}.");

            playback.Seek(timeline.RoundStart(0) + DuelTimeline.IntroTicks + timeline.RoundTicks(0) + 40);
            yield return null;
            Shoot(screen, "06-playback-round-end", "Round 1's end held: " + playback.BannerText + ", " + playback.ScoreText + ".");

            screen.enabled = true;
            screen.SkipPlayback();
            yield return Settle();
            Shoot(screen, "07-result", "The result: " + screen.ResultTitle + "; " + screen.RatingText + "; " + screen.WhyText.Replace("\n", " | "));

            File.WriteAllText(Output + "frames-" + LanguageCodes.Code(CaptureLanguage) + ".txt", _frames.ToString());
            LogAssert.NoUnexpectedReceived();
        }

        private static DuelEvent? Find(DuelLog log, Predicate<DuelEvent> match)
        {
            for (int i = 0; i < log.Count; i++)
            {
                if (match(log[i]))
                    return log[i];
            }
            return null;
        }

        private static IEnumerator Settle()
        {
            yield return new WaitForSecondsRealtime(ArenaScreen.InputDelay + .1f);
        }

        // The phone's safe area on every safe area of the scene, and every overlay canvas drawn through the camera into a
        // texture the phone's size (AnvilUiCapture's way); then everything put back.
        private void Shoot(ArenaScreen screen, string file, string what)
        {
            _frames.Append(Prefix).Append(file).Append(": ").Append(what).Append('\n');
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
                // The canvases have measured themselves at the phone's size; the arena fits the safe area it now sees.
                screen.FitNow();
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.SortingGroup.UpdateAllSortingGroups();
                _camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                pixels.Apply();
                WriteBmp(Output + Prefix + file + ".bmp", pixels.GetPixels32(), Width, Height);
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
            if (ArenaScreen.Current != null)
                ArenaScreen.Current.Close();
            Time.timeScale = 1;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Arena capture cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
