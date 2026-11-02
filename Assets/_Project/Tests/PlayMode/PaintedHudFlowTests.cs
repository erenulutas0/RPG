using System.Collections;
using System.IO;
using System.Reflection;
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
    // The in-run HUD and the enemies' bars painted in the Gameplay scene (HudArtSet on the HUD Canvas's HudArtView,
    // EnemyBarArtSet on the enemy prefabs): the panels, the pause button, the pips, the loadout sockets and the ability's
    // ring wear the painted sprites in the scene's own rects; the hero's bars show their fills, a hit's chunk trails and
    // drains, and the light rides the fill's end; the boss bar is painted, its frame keeping to its rect's foot and clear
    // of the name; an enemy's bar wears the painted fill and drains with damage, the chunk following, and the Mite's is the
    // compact one; and a view or an enemy without its set, or with a broken one, is the flat HUD and the pixel bar.
    public sealed class PaintedHudFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

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
            SceneManager.SetActiveScene(SceneManager.CreateScene("Painted HUD cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // The foundry (whose run is by category, so the loadout row shows), on the given development floor if any, with
        // every attack off; two frames, so HudArtView has painted (Start) and dressed the loadout row (its first LateUpdate).
        private static IEnumerator Load(string floor = null)
        {
            if (floor != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
                File.WriteAllText(DevelopmentStart.StartFloorPath, floor);
            }
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            yield return null;
        }

        private static IEnumerator Until(System.Func<bool> condition, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
                yield return null;
        }

        private static Image ImageOf(string name) => GameObject.Find(name).GetComponent<Image>();

        // A rect's corners on screen: bottom left and top right.
        private static (Vector2 min, Vector2 max) Screen(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0], corners[2]);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheHudWearsThePaintedFramesAndItsBarsShowTheirFills()
        {
            yield return Load();
            var view = Object.FindFirstObjectByType<HudArtView>();
            Assert.That(view, Is.Not.Null, "The HUD Canvas carries the painter.");
            Assert.That(view.UsesPaintedArt, Is.True);
            HudArtSet art = view.Art;

            // The panels and the pause button in their forged frames, in the scene's own rects.
            foreach ((string name, Vector2 size, Vector2 at) in new[] { ("Top Surface", new Vector2(1032, 244), new Vector2(0, -8)),
                         ("Lower Surface", new Vector2(1032, 252), new Vector2(0, 60)) })
            {
                Image panel = ImageOf(name);
                Assert.That(panel.sprite, Is.SameAs(art.Panel), name);
                Assert.That(panel.type, Is.EqualTo(Image.Type.Sliced), $"{name}: 9-sliced.");
                Assert.That(panel.pixelsPerUnitMultiplier, Is.EqualTo(1f), $"{name}: its frame at the size it was painted.");
                Assert.That(panel.color, Is.EqualTo(Color.white), $"{name}: in its own colours.");
                Assert.That(panel.rectTransform.sizeDelta, Is.EqualTo(size), $"{name}: where it was.");
                Assert.That(panel.rectTransform.anchoredPosition, Is.EqualTo(at));
            }
            Image pause = ImageOf("Pause Button");
            Assert.That(pause.sprite, Is.SameAs(art.Button));
            Assert.That(pause.GetComponent<Button>().targetGraphic, Is.SameAs(pause), "Still the button's own graphic.");
            Image pip = ImageOf("Room Pip 1");
            Assert.That(pip.sprite, Is.SameAs(art.Pip));
            Assert.That((Color32)pip.color, Is.EqualTo(new Color32(218, 165, 84, 255)), "RunHud still tints the room it is in.");

            // The ability's ring round the same icon, the cooldown still its radial sweep in the scene's colour.
            Image bezel = ImageOf("Ability Bezel");
            Assert.That(bezel.sprite, Is.SameAs(art.AbilityRing));
            Image cooldown = GameObject.Find("Ability Button").transform.Find("Cooldown Fill").GetComponent<Image>();
            Assert.That(cooldown.sprite, Is.SameAs(art.AbilityCooldown));
            Assert.That(cooldown.type == Image.Type.Filled && cooldown.fillMethod == Image.FillMethod.Radial360, Is.True);
            Assert.That(cooldown.color.b, Is.GreaterThan(cooldown.color.r), "Tinted the scene's cooling blue.");
            Assert.That(ImageOf("Ability Icon").sprite.name, Does.StartWith("UI_Icon_"), "The ability's own icon, unchanged.");

            // The loadout row's sockets: the forge's weapon in a whole one, the free slots faint.
            var hud = Object.FindFirstObjectByType<RunHud>();
            Assert.That(hud.LoadoutBadges, Is.Not.Null, "A foundry run carries the loadout row.");
            Assert.That(view.PaintsLoadout, Is.True);
            Transform row = hud.LoadoutBadges.Row;
            Image weapon = row.Find("Weapon Badge 1").GetComponent<Image>();
            Image freeWeapon = row.Find("Weapon Badge 2").GetComponent<Image>();
            Image freeTome = row.Find("Tome Badge 1").GetComponent<Image>();
            Assert.That(weapon.sprite, Is.SameAs(art.WeaponSlot));
            Assert.That(weapon.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(weapon.color.a, Is.EqualTo(1f), "The forge's weapon fills its socket.");
            Assert.That(freeWeapon.sprite, Is.SameAs(art.WeaponSlot));
            Assert.That(freeWeapon.color.a, Is.LessThan(.5f), "A free slot is a faint plate.");
            Assert.That(freeTome.sprite, Is.SameAs(art.TomeSlot));
            Assert.That(row.Find("Weapon Badge 1/Accent").GetComponent<Image>().sprite, Is.SameAs(art.SlotAccent));
            Assert.That(hud.LoadoutBadges.WeaponBadgeText(0), Is.Not.Empty, "It still names the weapon and its level.");

            // The hero's health: the fill RunHud drives, painted at its window's size, in a trough and a frame; the bar's
            // own rect where it was.
            HudArtView.PaintedBar hero = view.Hero;
            Health health = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            RectTransform heroBar = GameObject.Find("Hero Bar").GetComponent<RectTransform>();
            Assert.That(hero.Fill, Is.SameAs(heroBar.Find("Fill").GetComponent<Image>()));
            Assert.That(heroBar.sizeDelta, Is.EqualTo(new Vector2(698, 22)));
            Assert.That(heroBar.anchoredPosition, Is.EqualTo(new Vector2(64, 210)));
            Assert.That(heroBar.GetComponent<Image>().enabled, Is.False, "The flat backing gives way to the trough.");
            Assert.That(hero.Fill.sprite, Is.SameAs(art.Hero.Fill));
            Assert.That(hero.Fill.rectTransform.rect.size, Is.EqualTo(art.Hero.Fill.rect.size), "Cropped, never stretched.");
            Assert.That(hero.Trough.sprite, Is.SameAs(art.Hero.Trough));
            Assert.That(hero.Frame.sprite, Is.SameAs(art.Hero.Frame));
            Assert.That(hero.Frame.transform.GetSiblingIndex(), Is.EqualTo(heroBar.childCount - 1), "The frame lies over the fill.");
            Assert.That(hero.Fill.fillAmount, Is.EqualTo(health.Current / health.Maximum).Within(1e-5f));
            Assert.That(hero.Trail.enabled, Is.False, "No chunk at full health.");
            Assert.That(hero.Glint.enabled, Is.False, "No light at a full bar's end.");

            // A hit: the fill drops, the chunk holds where it stood, the light rides the fill's end; then it drains.
            float before = hero.Fill.fillAmount;
            health.ApplyDamage(new DamageContext(health.Maximum * .3f));
            yield return null;
            float after = health.Current / health.Maximum;
            Assert.That(hero.Fill.fillAmount, Is.EqualTo(after).Within(1e-5f), "RunHud's fill, as ever.");
            Assert.That(hero.Trail.enabled, Is.True, "A pale chunk shows what the hit took.");
            Assert.That(hero.Trail.fillAmount, Is.EqualTo(before).Within(1e-4f));
            Assert.That(hero.Glint.enabled, Is.True);
            Assert.That(hero.Glint.rectTransform.anchorMin.x, Is.EqualTo(after).Within(1e-5f), "The light at the fill's end.");
            yield return Until(() => !hero.Trail.enabled, 2f);
            Assert.That(hero.Trail.enabled, Is.False, "The chunk drains within a second or so.");
            Assert.That(hero.Fill.fillAmount, Is.EqualTo(after).Within(1e-5f));

            // Experience only grows: no chunk.
            Assert.That(view.Experience.Trail, Is.Null);
            Assert.That(view.Experience.Fill.sprite, Is.SameAs(art.Experience.Fill));
            Assert.That(view.Experience.Fill.rectTransform.rect.size, Is.EqualTo(art.Experience.Fill.rect.size));

            // Nothing painted catches a tap meant for the fight or a button.
            foreach (Image image in GameObject.Find("HUD Canvas").GetComponentsInChildren<Image>(true))
                if (image.name.StartsWith("Painted "))
                    Assert.That(image.raycastTarget, Is.False, image.name);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheBossBarIsPaintedHeavierKeepingToItsFootAndClearOfItsName()
        {
            yield return Load("floor_gate_boss_proof");
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            var readout = GameObject.Find("Boss Readout").GetComponent<CanvasGroup>();
            yield return Until(() =>
            {
                for (int i = 0; i < 8 && setup.Choices.Current != null && setup.Choices.Current.Kind == ChoiceKind.Upgrade; i++)
                    setup.Choices.TrySelect(setup.Choices.Current, 0);
                return setup.Encounters.GateBoss != null && setup.Encounters.CurrentEnemy != null && readout.alpha == 1f;
            }, 30f);
            Assert.That(readout.alpha, Is.EqualTo(1f), "The gate boss's readout shows.");
            var view = Object.FindFirstObjectByType<HudArtView>();
            HudArtSet art = view.Art;
            HudArtView.PaintedBar boss = view.Boss;
            RectTransform bar = GameObject.Find("Enemy Bar").GetComponent<RectTransform>();
            Assert.That(Camera.main.GetComponent<ArenaCameraFollow>().TopHud, Is.SameAs(bar), "Still the camera's top band.");
            Assert.That(bar.sizeDelta, Is.EqualTo(new Vector2(936, 14)), "Its rect where it was.");
            Assert.That(bar.anchoredPosition, Is.EqualTo(new Vector2(0, -313)));
            Assert.That(boss.Fill.sprite, Is.SameAs(art.Boss.Fill));
            Assert.That(boss.Trough.sprite, Is.SameAs(art.Boss.Trough));
            Assert.That(boss.Frame.sprite, Is.SameAs(art.Boss.Frame));
            Assert.That(boss.Fill.rectTransform.rect.size, Is.EqualTo(art.Boss.Fill.rect.size), "Cropped, never stretched.");

            float scale = bar.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
            (Vector2 rectMin, Vector2 rectMax) = Screen(bar);
            (Vector2 frameMin, Vector2 frameMax) = Screen(boss.Frame.rectTransform);
            (Vector2 fillMin, Vector2 fillMax) = Screen(boss.Fill.rectTransform);
            Assert.That(frameMin.y, Is.EqualTo(rectMin.y).Within(.01f), "The frame's foot is the rect's: the fight's band starts below it.");
            Assert.That(fillMin.y, Is.EqualTo(rectMin.y + 4f * scale).Within(.01f), "The fill lifted over the frame's foot.");
            Assert.That(fillMax.y, Is.EqualTo(rectMax.y + 6f * scale).Within(.01f), "And grown above the rect.");
            Assert.That(frameMax.x - frameMin.x, Is.GreaterThan(rectMax.x - rectMin.x + 80f * scale), "Its furnace ends reach past the bar.");

            // The rim over the fill stands clear of the name's glyphs; only the end caps rise past it, beside the name.
            Text name = GameObject.Find("Boss Name").GetComponent<Text>();
            Canvas.ForceUpdateCanvases();
            float lowest = float.MaxValue, left = float.MaxValue, right = float.MinValue;
            foreach (UIVertex vertex in name.cachedTextGenerator.verts)
            {
                Vector3 local = vertex.position / name.pixelsPerUnit;
                Vector3 world = name.rectTransform.TransformPoint(local);
                lowest = Mathf.Min(lowest, world.y);
                left = Mathf.Min(left, world.x);
                right = Mathf.Max(right, world.x);
            }
            Assert.That(lowest, Is.LessThan(float.MaxValue), "The name is set.");
            // Glyph quads carry a texel or so of the font's padding below the ink, and the batch window's small canvas
            // rasterises the font up to a screen pixel lower.
            Assert.That(lowest, Is.GreaterThanOrEqualTo(fillMax.y + 6f * scale - 2f * scale - 1f), "The rim keeps under the name.");
            Assert.That(left, Is.GreaterThan(fillMin.x), "The name lies between the end caps.");
            Assert.That(right, Is.LessThan(fillMax.x));
            Image plate = view.BossNamePlate;
            Assert.That(plate.sprite, Is.SameAs(art.BossNamePlate));
            Assert.That(plate.transform.GetSiblingIndex(), Is.EqualTo(name.transform.GetSiblingIndex() - 1), "Drawn behind the name.");

            // A blow: the molten fill drops, its chunk trails and drains.
            Health health = setup.Encounters.CurrentEnemy;
            float before = boss.Fill.fillAmount;
            health.ApplyDamage(new DamageContext(health.Maximum * .2f));
            yield return null;
            Assert.That(boss.Fill.fillAmount, Is.LessThan(before));
            Assert.That(boss.Trail.enabled, Is.True);
            Assert.That(boss.Trail.fillAmount, Is.EqualTo(before).Within(1e-4f));
            yield return Until(() => !boss.Trail.enabled, 2f);
            Assert.That(boss.Trail.enabled, Is.False, "And drains to the fill.");
            LogAssert.NoUnexpectedReceived();
        }

        // A copy of an enemy in the scene, woken with its bar set replaced (when replace is true), its health fresh.
        private static EnemyLookView Copy(EnemyLookView source, bool replace, EnemyBarArtSet set)
        {
            var holder = new GameObject("Inactive holder");
            holder.SetActive(false);
            EnemyLookView look = Object.Instantiate(source, source.transform.position + Vector3.right * 2f, Quaternion.identity,
                holder.transform);
            if (replace)
                typeof(EnemyLookView).GetField("_barArt", Private).SetValue(look, set);
            look.transform.SetParent(null, true);
            Object.Destroy(holder);
            look.GetComponent<Health>().Initialize(100f);
            return look;
        }

        // The bar a copy built for itself: its last child (a copy of a live enemy also carries the original's).
        private static Transform BuiltBar(EnemyLookView look) => look.transform.GetChild(look.transform.childCount - 1);

        [UnityTest, Timeout(90000)]
        public IEnumerator AnEnemyBarWearsThePaintedFillAndDrainsWithDamage()
        {
            yield return Load();
            EnemyLookView source = null;
            foreach (var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if (look.Look != EnemyLook.Mite && look.PaintedBar != null)
                    source = look;
            Assert.That(source, Is.Not.Null, "The crowd wears painted bars.");
            EnemyBarArtSet set = source.PaintedBar;
            // Every bar of the crowd draws from the one sheet with the default sprite material, so forty bars batch.
            Material material = source.Bar.Find("Fill").GetComponent<SpriteRenderer>().sharedMaterial;
            foreach (var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
            {
                Assert.That(look.PaintedBar, Is.SameAs(set), "Every enemy wears the one set.");
                foreach (string part in new[] { "Back", "Trail", "Fill" })
                {
                    var renderer = look.Bar.Find(part).GetComponent<SpriteRenderer>();
                    Assert.That(renderer.sprite.texture, Is.SameAs(set.BackFor(false).texture), part);
                    Assert.That(renderer.sharedMaterial, Is.SameAs(material), part);
                }
                Assert.That(look.BarChunk.enabled, Is.EqualTo(look.BarChunk.IsShowing), "A chunk updates only while it shows.");
            }

            EnemyLookView enemy = Copy(source, false, null);
            yield return null;
            Transform bar = BuiltBar(enemy);
            Assert.That(bar.name, Is.EqualTo("Health Bar"));
            var back = bar.Find("Back").GetComponent<SpriteRenderer>();
            var trail = bar.Find("Trail").GetComponent<SpriteRenderer>();
            var fill = bar.Find("Fill").GetComponent<SpriteRenderer>();
            Assert.That(back.sprite, Is.SameAs(set.BackFor(false)));
            Assert.That(trail.sprite, Is.SameAs(set.TrailFor(false)));
            Assert.That(fill.sprite, Is.SameAs(set.FillFor(false)));
            Assert.That(back.sortingOrder < trail.sortingOrder && trail.sortingOrder < fill.sortingOrder, Is.True,
                "The chunk lies between the backing and the fill.");
            Assert.That(back.bounds.size.x, Is.EqualTo(26f / 32f).Within(1e-4f), "The pixel bar's room over a head.");
            Assert.That(back.bounds.size.y, Is.EqualTo(5f / 32f).Within(1e-4f));
            Assert.That(fill.bounds.center.y, Is.EqualTo(back.bounds.center.y).Within(1e-4f), "The fill in the channel.");
            Assert.That(fill.bounds.center.x, Is.EqualTo(back.bounds.center.x).Within(1e-4f));
            Assert.That(trail.enabled, Is.False, "No chunk at full health.");

            float left = fill.bounds.min.x;
            enemy.GetComponent<Health>().ApplyDamage(new DamageContext(25f));
            Assert.That(fill.transform.localScale.x, Is.EqualTo(.75f), "The fill drains with the hit, from its left edge.");
            Assert.That(fill.bounds.min.x, Is.EqualTo(left).Within(1e-5f));
            Assert.That(trail.enabled, Is.True, "The chunk shows what it took.");
            Assert.That(trail.transform.localScale.x, Is.EqualTo(1f));
            Assert.That(enemy.BarChunk.enabled, Is.True);
            yield return Until(() => !trail.enabled, 2f);
            Assert.That(trail.enabled, Is.False, "Then drains to the fill.");
            Assert.That(enemy.BarChunk.enabled, Is.False, "And stops updating.");
            Assert.That(fill.transform.localScale.x, Is.EqualTo(.75f));
            enemy.GetComponent<Health>().ApplyDamage(new DamageContext(1000f));
            Assert.That(bar.gameObject.activeSelf, Is.False, "A fallen enemy's bar hides at once.");
            Object.Destroy(enemy.gameObject);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheMitesBarIsTheCompactOneEvenOnceSqueezed()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            EnemyLookView mite = null, grunt = null;
            foreach (var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
            {
                if (look.Look == EnemyLook.Mite)
                    mite = look;
                else
                    grunt = look;
            }
            Assert.That(mite, Is.Not.Null);
            Assert.That(grunt, Is.Not.Null);
            EnemyBarArtSet set = mite.PaintedBar;
            Assert.That(set, Is.Not.Null, "The Mite wears the painted bar.");
            Transform bar = mite.Bar;
            Assert.That(bar.localScale, Is.EqualTo(new Vector3(.65f, .8f, 1f)), "Squeezed to the Mite's size, as the pixel bar was.");
            var back = bar.Find("Back").GetComponent<SpriteRenderer>();
            var fill = bar.Find("Fill").GetComponent<SpriteRenderer>();
            Assert.That(back.sprite, Is.SameAs(set.BackFor(true)), "Its compact backing.");
            Assert.That(fill.sprite, Is.SameAs(set.FillFor(true)));
            Assert.That(bar.Find("Trail").GetComponent<SpriteRenderer>().sprite, Is.SameAs(set.TrailFor(true)));
            Assert.That(back.bounds.size.x, Is.EqualTo(26f / 32f * .65f).Within(1e-5f));
            // Its frame across, once squeezed, is as thick as a full bar's, within a texel of the painted 128 a unit.
            var fullBack = grunt.Bar.Find("Back").GetComponent<SpriteRenderer>();
            var fullFill = grunt.Bar.Find("Fill").GetComponent<SpriteRenderer>();
            float side = (back.bounds.size.x - fill.sprite.rect.width / 128f * .65f) / 2f;
            float fullSide = (fullBack.bounds.size.x - fullFill.sprite.rect.width / 128f) / 2f;
            Assert.That(side, Is.EqualTo(fullSide).Within(1f / 128f));
            // Measured from the bar, which walks on with the Mite between the two frames.
            float left = fill.bounds.min.x - bar.position.x;
            Health health = mite.GetComponent<Health>();
            health.ApplyDamage(new DamageContext(health.Current - health.Maximum * .5f));
            yield return null;
            Assert.That(fill.transform.localScale.x, Is.EqualTo(.5f).Within(1f / fill.sprite.rect.width), "It drains in its own texels.");
            Assert.That(fill.bounds.min.x - bar.position.x, Is.EqualTo(left).Within(1e-5f), "From its left end.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator WithoutTheirSetsTheHudIsFlatAndTheBarsArePixels()
        {
            yield return Load();
            HudArtSet art = Object.FindFirstObjectByType<HudArtView>().Art;

            // A HUD of its own - a panel, a bar's backing and its fill - given no set, a broken one, then the real one.
            var canvas = new GameObject("Fallback canvas", typeof(Canvas)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var broken = ScriptableObject.CreateInstance<HudArtSet>();
            Assert.That(broken.IsValid, Is.False);
            foreach (HudArtSet given in new[] { null, broken, art })
            {
                var root = new GameObject("HUD " + (given == null ? "without a set" : given == broken ? "with a broken set" : "with the set"),
                    typeof(RectTransform));
                root.SetActive(false);
                root.transform.SetParent(canvas.transform, false);
                Image panel = new GameObject("Panel", typeof(RectTransform)).AddComponent<Image>();
                panel.transform.SetParent(root.transform, false);
                panel.color = new Color(.1f, .1f, .2f);
                Image backing = new GameObject("Bar", typeof(RectTransform)).AddComponent<Image>();
                backing.transform.SetParent(root.transform, false);
                backing.rectTransform.sizeDelta = new Vector2(698, 22);
                Image fill = new GameObject("Fill", typeof(RectTransform)).AddComponent<Image>();
                fill.transform.SetParent(backing.transform, false);
                fill.rectTransform.anchorMin = Vector2.zero;
                fill.rectTransform.anchorMax = Vector2.one;
                fill.rectTransform.sizeDelta = Vector2.zero;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                fill.color = Color.red;
                var view = root.AddComponent<HudArtView>();
                typeof(HudArtView).GetField("_art", Private).SetValue(view, given);
                typeof(HudArtView).GetField("_topSurface", Private).SetValue(view, panel);
                typeof(HudArtView).GetField("_heroFill", Private).SetValue(view, fill);
                root.SetActive(true);
                yield return null;
                yield return null;
                bool painted = given == art;
                Assert.That(view.UsesPaintedArt, Is.EqualTo(painted), root.name);
                Assert.That(panel.sprite == art.Panel, Is.EqualTo(painted), $"{root.name}: the panel.");
                Assert.That(fill.sprite == art.Hero.Fill, Is.EqualTo(painted), $"{root.name}: the fill.");
                Assert.That(backing.enabled, Is.EqualTo(!painted), $"{root.name}: the flat backing.");
                Assert.That(backing.transform.childCount, Is.EqualTo(painted ? 4 : 1), $"{root.name}: trough, chunk and frame only when painted.");
                if (!painted)
                {
                    Assert.That(panel.color, Is.EqualTo(new Color(.1f, .1f, .2f)), $"{root.name}: the flat look as it was.");
                    Assert.That(fill.color, Is.EqualTo(Color.red));
                    Assert.That(view.enabled, Is.False, $"{root.name}: and nothing left running.");
                }
                else
                {
                    // Relaid out after its Start (the canvas or the safe area resizing the bar), the light still
                    // stands 3 past the fill's foot and top.
                    RectTransform glint = view.Hero.Glint.rectTransform, fillRect = fill.rectTransform;
                    foreach (float height in new[] { 22f, 40f })
                    {
                        backing.rectTransform.sizeDelta = new Vector2(698, height);
                        yield return null;
                        float y = glint.localPosition.y;
                        Assert.That(y + glint.rect.yMin, Is.EqualTo(fillRect.rect.yMin - 3f).Within(1e-3f),
                            $"The light's foot over a bar {height} tall.");
                        Assert.That(y + glint.rect.yMax, Is.EqualTo(fillRect.rect.yMax + 3f).Within(1e-3f),
                            $"Its top over a bar {height} tall.");
                    }
                }
            }
            Object.Destroy(canvas.gameObject);
            Object.Destroy(broken);

            // An enemy without the bar set, or with a broken one, builds the pixel bar exactly as before.
            EnemyLookView source = Object.FindFirstObjectByType<EnemyLookView>();
            var brokenBars = ScriptableObject.CreateInstance<EnemyBarArtSet>();
            Assert.That(brokenBars.IsValid, Is.False);
            foreach (EnemyBarArtSet given in new[] { null, brokenBars })
            {
                EnemyLookView enemy = Copy(source, true, given);
                yield return null;
                Transform bar = BuiltBar(enemy);
                Assert.That(enemy.PaintedBar, Is.Null);
                Assert.That(enemy.BarChunk, Is.Null);
                Assert.That(bar.Find("Trail"), Is.Null, "No chunk on the pixel bar.");
                Assert.That(bar.Find("Back").GetComponent<SpriteRenderer>().sprite, Is.SameAs(EnemySpriteCache.BarBack));
                var fill = bar.Find("Fill").GetComponent<SpriteRenderer>();
                Assert.That(fill.sprite, Is.SameAs(EnemySpriteCache.BarFill));
                enemy.GetComponent<Health>().ApplyDamage(new DamageContext(25f));
                Assert.That(fill.transform.localScale.x, Is.EqualTo(.75f), "Whole texels of the pixel bar's 24.");
                Object.Destroy(enemy.gameObject);
            }
            Object.Destroy(brokenBars);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
