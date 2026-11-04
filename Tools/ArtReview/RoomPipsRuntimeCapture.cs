// Graphics-enabled PlayMode fixture (Run-RoomPipsRuntimeProof.ps1 installs it for one run and removes it): the HUD's
// room-pip row on the foundry, in the game itself, at two phone shapes. The foundry plays floor 1 as seven rooms - its
// V0.1 elite before the gate (EncounterController.FoundryRooms) - against the scene's six pips, so RunHud.GrowRoomPips
// copies the sixth a step along the row and HudArtView paints the copy. Four moments of one run: the floor's start (seven
// pips, the first lit), room 6 - the second elite - (six lit), the Warden's room (all seven lit) and floor 2, reached
// through the opened gate and the checkpoint's Descend (six shown, the seventh hidden). Each is drawn at 1080x2400 (a
// 20:9 phone with the S23's safe area as the movement HUD proof sets it: 100 px under the top, 76 over the bottom) and
// 1080x1920 (a 16:9 phone without a cutout: all of it safe), every overlay canvas through the camera above the world and
// the camera framed from the HUD as the phone frames it, with a crop of the top band; pip-rects.txt says where every pip
// and the seventh's neighbours landed in each frame. The run is driven, not staged: offers are answered at once (Descend
// at the checkpoint, the first card of anything else), the hero is kept standing and carried to each hall's district, and
// the node waves before each stop are felled as they come. The floor's clock starts at 3:32 (development/floor-clock.txt),
// past floor 1's gate time, so the room before the gate never holds. No scene or asset is saved; nothing here ships.
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using Cryptforge.Art;
using Cryptforge.Combat;
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
    public sealed class RoomPipsRuntimeCapture
    {
        private const string Output = "ArtDirection/2026-11-04/room-pips-runtime-01/";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const float Step = 1f / 60f;
        // Past floor 1's gate time (FoundryGateNotBeforeSeconds, 210 s), so the cleared room 6 goes straight on to the
        // Warden, and a minute and a half before the overrun: the clock reads 1:28 as a floor starts.
        private const float ClockStart = 212f;
        private const int Seed = 11;
        // The band each frame is also cropped to: the safe area's top inset and this much below it - the top surface (to
        // 252), the floor clock under it (262-326), or the boss readout (265-327) with the clock below that (338-402).
        private const int TopBand = 410;
        // Every world renderer sorts below this (the highest, the altar's and the gate's instruction labels, at 100). The
        // canvases drawn through the camera keep their own order above it, as overlay canvases draw over the whole camera
        // on the phone.
        private const int OverWorld = 10000;
        private static readonly Color32 Lit = new Color32(218, 165, 84, 255);
        private static readonly Color32 Unlit = new Color32(60, 60, 79, 255);
        // What the seventh pip must keep clear of (RunHud.GrowRoomPips, PaintedHudFlowTests), beside the loadout row.
        private static readonly string[] Neighbours =
            { "Floor Label", "Gold Icon", "Gold Count", "Pause Button", "Equipped Relic Badge", "Danger Readout" };

        private readonly struct Phone
        {
            public readonly string Name;
            public readonly int Width;
            public readonly int Height;
            public readonly int TopInset;
            public readonly int BottomInset;

            public Phone(int width, int height, int topInset, int bottomInset)
            {
                Name = width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture);
                Width = width;
                Height = height;
                TopInset = topInset;
                BottomInset = bottomInset;
            }
        }

        // Both 1080 wide, so the HUD's scaler (1080x1920 reference, matching width) lays the canvas out at scale 1.
        private static readonly Phone[] Phones = { new Phone(1080, 2400, 100, 76), new Phone(1080, 1920, 0, 0) };

        private readonly Vector3[] _corners = new Vector3[4];
        private Scene _scene;
        private Camera _camera;
        private ArenaView _arena;
        private ArenaCameraFollow _follow;
        private CombatSetup _setup;
        private EncounterController _encounters;
        private RunHud _hud;
        private HudArtSet _art;
        private Health _hero;

        [UnityTest, Timeout(900000)]
        public IEnumerator CaptureTheRoomPipRow()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.WriteAllText(DevelopmentStart.FloorClockPath, ClockStart.ToString(CultureInfo.InvariantCulture));
            File.WriteAllText(DevelopmentStart.RunSeedPath, Seed.ToString(CultureInfo.InvariantCulture));
            Time.timeScale = 1;
            Time.captureDeltaTime = Step;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _scene = SceneManager.GetActiveScene();
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _encounters = _setup.Encounters;
            _hud = Object.FindFirstObjectByType<RunHud>();
            var painter = Object.FindFirstObjectByType<HudArtView>();
            var movement = Object.FindFirstObjectByType<HeroMovementInput>();
            movement.enabled = false;
            _hero = movement.GetComponent<Health>();
            _camera = Camera.main;
            _follow = _camera.GetComponent<ArenaCameraFollow>();
            _arena = Object.FindFirstObjectByType<ArenaView>();
            // HudArtView has painted in its Start and dressed RunHud's pips in its first LateUpdate.
            yield return null;
            Assert.That(_arena.IsFoundryRegion, Is.True, "The foundry.");
            Assert.That(painter.UsesPaintedArt, Is.True, "The painted HUD.");
            _art = painter.Art;
            Assert.That(_encounters.FloorNumber, Is.EqualTo(1));
            Assert.That(_encounters.RoomCount, Is.EqualTo(_encounters.Floor.RoomCount + _encounters.Floor.FoundryExtraElites),
                "Floor 1's extra elite is a room.");
            Assert.That(_encounters.RoomCount, Is.EqualTo(7), "Seven rooms against the scene's six pips.");
            Assert.That(_encounters.Floor.FoundryGateNotBeforeSeconds, Is.LessThan(ClockStart), "The gate's time has passed.");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "pip-rects.txt",
                "Room pips on the foundry, as each frame shows them: x and y in the frame's pixels from its top left. The HUD's\n" +
                "own numbers (RunHud.GrowRoomPips: the seventh at x 628-710, y 133-141) count y from the safe area's top, which\n" +
                "is the frame's top less the safe area's top inset.\n");

            // 1. The floor's start: the first wave walks in round the hero.
            yield return Play(1f);
            Assert.That(_encounters.RoomNumber, Is.EqualTo(1));
            AssertPips(7, 1, "Floor 1, room 1");
            Shoot("01-floor1-room1", 7, 1);

            // 2. Room 6, the second elite (a copy of the first, at a district of its own), its pack walking in.
            yield return Drive(() => _encounters.RoomNumber == 6 && !_encounters.IsRoomSiteOpen && _encounters.AliveEnemyCount > 0,
                6, 240f, "room 6's elite pack");
            yield return Play(.75f);
            Assert.That(_encounters.RoomNumber, Is.EqualTo(6));
            Assert.That(_encounters.CurrentRoom.Kind, Is.EqualTo(RoomKind.Elite));
            AssertPips(7, 6, "Floor 1, room 6");
            Shoot("02-floor1-room6-elite", 7, 6);

            // 3. The Warden's room, once the camera has turned to the gate and back: every pip lit, the last with it.
            var reveal = _setup.GetComponent<BossRevealView>();
            yield return Drive(() => _encounters.CurrentRoom != null && _encounters.CurrentRoom.Kind == RoomKind.Boss &&
                    _setup.Warden.IsFighting && !_encounters.IsRevealHeld && !reveal.IsRevealing,
                7, 120f, "the Warden's fight");
            yield return Play(.5f);
            Assert.That(_encounters.RoomNumber, Is.EqualTo(_encounters.RoomCount), "The floor's last room.");
            AssertPips(7, 7, "Floor 1, the Warden's room");
            Shoot("03-floor1-room7-warden", 7, 7);

            // 4. Floor 2: the Warden felled, the hero standing in the opened gate, Descend taken at the checkpoint. Its six
            // rooms show six pips; the copy stays, hidden.
            Health warden = _setup.Warden.Boss;
            Assert.That(warden, Is.Not.Null, "The Warden stands.");
            warden.ApplyDamage(new DamageContext(warden.Current + PackTestUtility.Lethal));
            yield return Drive(() => _encounters.IsGateExitOpen, 0, 10f, "the opened gate");
            FoundryMap.GateExitSpot(out float gateX, out float gateY);
            _hero.transform.position = new Vector3(gateX, ArenaFloor.WorldY(gateY), 0f);
            yield return Drive(() => _encounters.FloorNumber == 2 && _encounters.RoomNumber == 1 && _encounters.AliveEnemyCount > 0,
                0, 30f, "floor 2's first wave");
            yield return Play(1f);
            Assert.That(_encounters.FloorNumber, Is.EqualTo(2));
            Assert.That(_encounters.RoomCount, Is.EqualTo(6), "Floor 2 plays six rooms.");
            Assert.That(_encounters.RoomNumber, Is.EqualTo(1));
            AssertPips(6, 1, "Floor 2, room 1");
            Shoot("04-floor2-room1", 6, 1);
        }

        // Game time with the run kept going and nothing else done for it.
        private IEnumerator Play(float seconds)
        {
            int frames = Mathf.RoundToInt(seconds / Step);
            for (int frame = 0; frame < frames && !_setup.Run.HasEnded; frame++)
            {
                Keep();
                yield return null;
            }
            Keep();
            Assert.That(_setup.Run.HasEnded, Is.False, "The run goes on.");
        }

        // The run driven until reached says so: each hall's district walked to at once, and on floor 1 every node wave of a
        // room before fellBefore felled as it comes. Bounded by game time.
        private IEnumerator Drive(System.Func<bool> reached, int fellBefore, float seconds, string what)
        {
            int frames = Mathf.RoundToInt(seconds / Step);
            for (int frame = 0; frame < frames && !reached() && !_setup.Run.HasEnded; frame++)
            {
                Keep();
                if (_encounters.IsRoomSiteOpen)
                    _hero.transform.position = new Vector3(_encounters.RoomSiteFloorX,
                        ArenaFloor.WorldY(_encounters.RoomSiteFloorY), 0f);
                else if (!_encounters.IsInNonCombatRoom && _encounters.FloorNumber == 1 && _encounters.RoomNumber < fellBefore)
                    PackTestUtility.KillWave(_encounters);
                yield return null;
            }
            Keep();
            Assert.That(reached(), Is.True,
                $"{what} is reached; stopped on floor {_encounters.FloorNumber}, room {_encounters.RoomNumber}, run ended: {_setup.Run.HasEnded}.");
        }

        // The run kept going for the camera: every offer answered at once - the checkpoint with Descend, anything else
        // with its first card - and the hero healed whole.
        private void Keep()
        {
            for (int i = 0; i < 8 && _setup.Choices.IsOpen; i++)
                _setup.Choices.TrySelect(_setup.Choices.Current,
                    _setup.Choices.Current.Kind == ChoiceKind.Checkpoint ? DescendSlot() : 0);
            if (_hero.IsAlive)
                _hero.Heal(_hero.Maximum);
        }

        private int DescendSlot()
        {
            CheckpointOffer offer = _setup.Checkpoint.CurrentOffer;
            for (int i = 0; offer != null && i < offer.Choices.Count; i++)
                if (offer.Choices[i].Kind == CheckpointKind.Descend)
                    return i;
            return -1;
        }

        // RunHud's pips as shown: the scene's six and the copy, the first shown ones in the row, lit up to the room.
        private void AssertPips(int shown, int lit, string when)
        {
            Assert.That(_setup.Choices.IsOpen, Is.False, $"{when}: no offer over the frame.");
            Assert.That(_hud.RoomPipCount, Is.EqualTo(7), $"{when}: the scene's six pips and floor 1's copy.");
            for (int i = 0; i < _hud.RoomPipCount; i++)
            {
                Image pip = _hud.RoomPipAt(i);
                Assert.That(pip.gameObject.activeSelf, Is.EqualTo(i < shown), $"{when}: pip {i + 1} shown or hidden.");
                if (i >= shown)
                    continue;
                Assert.That(pip.sprite, Is.SameAs(_art.Pip), $"{when}: pip {i + 1} is painted.");
                Assert.That((Color32)pip.color, Is.EqualTo(i < lit ? Lit : Unlit), $"{when}: pip {i + 1} lit up to the room.");
            }
        }

        // This frame at both phones; what each measured is kept even when a check of it fails.
        private void Shoot(string name, int shown, int lit)
        {
            var rects = new StringBuilder();
            try
            {
                foreach (Phone phone in Phones)
                    Shoot(name + "-" + phone.Name, phone, shown, lit, rects);
            }
            finally
            {
                File.AppendAllText(Output + "pip-rects.txt", rects.ToString());
            }
        }

        // The phone's safe area on every safe area of the scene, every overlay canvas drawn through the camera into a
        // texture the phone's size, the camera framed from the HUD as ArenaCameraFollow frames the phone; then everything
        // put back. The frame and its top band are written as BMP before the layout is checked, so a wrong frame stays.
        private void Shoot(string file, Phone phone, int shown, int lit, StringBuilder rects)
        {
            var fitters = Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var fitted = new bool[fitters.Length];
            var anchors = new Vector4[fitters.Length];
            var offsets = new Vector4[fitters.Length];
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var drawn = new bool[canvases.Length];
            var orders = new int[canvases.Length];
            var distances = new float[canvases.Length];
            var cameras = new Camera[canvases.Length];
            RenderTexture target = RenderTexture.GetTemporary(phone.Width, phone.Height, 24);
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
                    rect.anchorMin = new Vector2(0f, phone.BottomInset / (float)phone.Height);
                    rect.anchorMax = new Vector2(1f, 1f - phone.TopInset / (float)phone.Height);
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
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
                }
                _camera.aspect = (float)phone.Width / phone.Height;
                _camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                // The free band runs from the bottom block's head to the top block's foot, read off the HUD as this phone
                // lays it out (ArenaCameraFollow.LateUpdate on the device).
                float bandTop = OnFrame(_follow.TopHud).yMin;
                float bandBottom = OnFrame(_follow.BottomHud).yMax;
                _follow.Frame(phone.Width, phone.Height, bandBottom, bandTop);
                _arena.Backdrop.FrameCavern();
                Canvas.ForceUpdateCanvases();
                UnityEngine.Rendering.SortingGroup.UpdateAllSortingGroups();
                Canvas root = _hud.RoomPipAt(0).canvas.rootCanvas;
                Vector2 size = ((RectTransform)root.transform).rect.size;
                float scale = root.scaleFactor;
                Record(rects, file, phone, shown, lit, root, size, scale);

                _camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(phone.Width, phone.Height, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, phone.Width, phone.Height), 0, 0);
                pixels.Apply();
                Color32[] colours = pixels.GetPixels32();
                WriteBmp(Output + file + ".bmp", colours, phone.Width, 0, phone.Height);
                int band = Mathf.Min(phone.Height, phone.TopInset + TopBand);
                WriteBmp(Output + file + "-top.bmp", colours, phone.Width, phone.Height - band, band);

                Assert.That(scale, Is.EqualTo(1f).Within(1e-3f), $"{file}: the HUD at the scale a 1080-wide phone gives it.");
                Assert.That(size.x, Is.EqualTo(phone.Width).Within(.5f), $"{file}: the HUD canvas as wide as the frame.");
                Assert.That(size.y, Is.EqualTo(phone.Height).Within(.5f), $"{file}: the HUD canvas as tall as the frame.");
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

        // Where every pip landed in this frame, and whether the seventh keeps clear of its neighbours and on the top surface.
        private void Record(StringBuilder rects, string file, Phone phone, int shown, int lit, Canvas root, Vector2 size, float scale)
        {
            rects.Append('\n').Append(file).Append(": floor ").Append(_encounters.FloorNumber).Append(", room ")
                .Append(_encounters.RoomNumber).Append(" of ").Append(_encounters.RoomCount).Append("; ").Append(shown)
                .Append(" pips shown, ").Append(lit).Append(" lit; safe area ").Append(phone.TopInset).Append(" px under the top, ")
                .Append(phone.BottomInset).Append(" over the bottom; HUD canvas ").Append(F(size.x)).Append('x').Append(F(size.y))
                .Append(" at scale ").Append(scale.ToString("0.###", CultureInfo.InvariantCulture)).Append("; floor clock \"")
                .Append(_hud.FloorClock != null ? _hud.FloorClock.ShownText : string.Empty).Append("\"\n");
            for (int i = 0; i < _hud.RoomPipCount; i++)
            {
                Image pip = _hud.RoomPipAt(i);
                rects.Append("  pip ").Append(i + 1).Append(": ");
                if (!pip.gameObject.activeSelf)
                {
                    rects.Append("hidden\n");
                    continue;
                }
                rects.Append(Text(OnFrame(pip.rectTransform), phone.Height)).Append(i < lit ? ", lit\n" : "\n");
            }
            if (shown < 7)
                return;
            Rect last = OnFrame(_hud.RoomPipAt(6).rectTransform);
            Transform safeArea = _hud.RoomPipAt(6).transform.parent;
            foreach (string name in Neighbours)
            {
                RectTransform neighbour = Named(safeArea, root.transform, name);
                if (neighbour == null)
                {
                    rects.Append("  ").Append(name).Append(": not in the HUD\n");
                    continue;
                }
                Neighbour(rects, name, neighbour, last, phone.Height);
            }
            if (_hud.LoadoutBadges != null)
                Neighbour(rects, "Loadout row", _hud.LoadoutBadges.Row, last, phone.Height);
            RectTransform surface = Named(safeArea, root.transform, "Top Surface");
            if (surface != null)
            {
                Rect panel = OnFrame(surface);
                bool inside = panel.xMin <= last.xMin && last.xMax <= panel.xMax && panel.yMin <= last.yMin && last.yMax <= panel.yMax;
                rects.Append("  Top Surface: ").Append(Text(panel, phone.Height))
                    .Append(inside ? ", pip 7 inside it\n" : ", pip 7 NOT inside it\n");
            }
        }

        private void Neighbour(StringBuilder rects, string name, RectTransform neighbour, Rect last, int height)
        {
            Rect rect = OnFrame(neighbour);
            rects.Append("  ").Append(name).Append(": ").Append(Text(rect, height))
                .Append(neighbour.gameObject.activeInHierarchy ? string.Empty : " (hidden)")
                .Append(last.Overlaps(rect) ? ", pip 7 OVERLAPS it\n" : ", pip 7 clear\n");
        }

        // A HUD rect as the frame shows it, in the frame's pixels from its bottom left: its place on its root canvas, whose
        // rect is the whole frame at the canvas's scale. Read off the canvas, so it holds wherever the canvas stands.
        private Rect OnFrame(RectTransform rect)
        {
            Canvas root = rect.GetComponentInParent<Canvas>(true).rootCanvas;
            var canvas = (RectTransform)root.transform;
            Rect whole = canvas.rect;
            float scale = root.scaleFactor;
            rect.GetWorldCorners(_corners);
            Vector3 a = canvas.InverseTransformPoint(_corners[0]);
            Vector3 b = canvas.InverseTransformPoint(_corners[2]);
            return Rect.MinMaxRect((Mathf.Min(a.x, b.x) - whole.xMin) * scale, (Mathf.Min(a.y, b.y) - whole.yMin) * scale,
                (Mathf.Max(a.x, b.x) - whole.xMin) * scale, (Mathf.Max(a.y, b.y) - whole.yMin) * scale);
        }

        // A frame rect in whole pixels from the frame's top left, as an image viewer reads it.
        private static string Text(Rect rect, int height) =>
            $"x {Mathf.RoundToInt(rect.xMin)}-{Mathf.RoundToInt(rect.xMax)}, " +
            $"y {height - Mathf.RoundToInt(rect.yMax)}-{height - Mathf.RoundToInt(rect.yMin)}";

        // A child of the pips' safe area by name, as PaintedHudFlowTests finds them; anywhere in the HUD otherwise.
        private static RectTransform Named(Transform safeArea, Transform root, string name)
        {
            if (safeArea.Find(name) is RectTransform child)
                return child;
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.name == name)
                    return rect;
            return null;
        }

        private static string F(float value) => value.ToString("0.#", CultureInfo.InvariantCulture);

        // A 24-bit bottom-up BMP of rows firstRow to firstRow + rows of a bottom-up frame: all of it, or its top band.
        private static void WriteBmp(string path, Color32[] pixels, int width, int firstRow, int rows)
        {
            int stride = (width * 3 + 3) & ~3;
            int bytes = stride * rows;
            var row = new byte[stride];
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write((ushort)0x4d42); writer.Write(bytes + 54); writer.Write(0); writer.Write(54);
                writer.Write(40); writer.Write(width); writer.Write(rows); writer.Write((ushort)1); writer.Write((ushort)24);
                writer.Write(0); writer.Write(bytes); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                for (int y = firstRow; y < firstRow + rows; y++)
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
            Time.captureDeltaTime = 0;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Room pips capture cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
