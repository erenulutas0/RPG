using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The combat feedback's painted art in the Gameplay scene (CombatFeedbackArtSet): the Daggers' double slash and the
    // crit star over it, the numbers, the smoke of a fall without painted remains and of an arrival, and a gold chest's
    // coins, each cut from the one painted sheet and placed, sorted and timed as the pixel art was; and, without the set,
    // the pixel art laid out exactly as before.
    public sealed class PaintedFeedbackTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // The painted sheet every feedback sprite is cut from.
        private const string SheetName = "VFX_Feedback";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private CombatEffectsView _effects;
        private Health _hero;
        private Transform _pool;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Painted feedback cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        // The density proof's painted crowd with the encounter and every attack off, so a test stages its own fight.
        private IEnumerator Load(PlayerProfile profile = null)
        {
            TestProfile.Begin(profile);
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            _effects = Object.FindFirstObjectByType<CombatEffectsView>();
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _pool = GameObject.Find("Combat Effects").transform;
            Assert.That(_effects.UsesPaintedFeedback, Is.True, "The scene's painted feedback art.");
        }

        // Every pooled effect on show whose sprite is one of the named frames.
        private List<SpriteRenderer> Shown(string spritePrefix)
        {
            var shown = new List<SpriteRenderer>();
            foreach (var renderer in _pool.GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite != null && renderer.sprite.name.StartsWith(spritePrefix))
                    shown.Add(renderer);
            return shown;
        }

        // The numbers on show in a view's pool, each its digits from left to right.
        private static List<List<SpriteRenderer>> Numbers(Transform pool)
        {
            var numbers = new List<List<SpriteRenderer>>();
            foreach (Transform root in pool)
            {
                if (!root.name.StartsWith("Number "))
                    continue;
                var digits = new List<SpriteRenderer>();
                foreach (Transform child in root)
                {
                    var digit = child.GetComponent<SpriteRenderer>();
                    if (digit != null && digit.enabled)
                        digits.Add(digit);
                }
                if (digits.Count == 0)
                    continue;
                digits.Sort((a, b) => a.transform.localPosition.x.CompareTo(b.transform.localPosition.x));
                numbers.Add(digits);
            }
            return numbers;
        }

        // What a number reads, from its numerals' sprites, and whether they are the crit font.
        private static string Reads(List<SpriteRenderer> digits, out bool critical)
        {
            critical = digits[0].sprite.name.StartsWith("VFX Crit Digit ");
            var text = new StringBuilder();
            foreach (var digit in digits)
                text.Append(digit.sprite.name[digit.sprite.name.Length - 1]);
            return text.ToString();
        }

        // The view's own handlers for what the tests cannot raise on cue: a chest opening, a fall, an arrival.
        private static void Call(CombatEffectsView view, string method, params object[] arguments)
        {
            MethodInfo info = typeof(CombatEffectsView).GetMethod(method, Private);
            Assert.That(info, Is.Not.Null, method);
            info.Invoke(view, arguments);
        }

        private Health WaveGrunt()
        {
            foreach (var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if (look.Look == EnemyLook.Grunt && look.GetComponent<Health>().IsAlive) return look.GetComponent<Health>();
            return null;
        }

        [UnityTest]
        public IEnumerator TheDaggersCrossTwoPaintedCutsAndACritStarsOverThem()
        {
            yield return Load(new PlayerProfile(0, null, null, 0, new[] { "weapon_daggers" }, "weapon_daggers"));
            Health grunt = WaveGrunt();
            Assert.That(grunt, Is.Not.Null);
            var attack = _hero.GetComponent<AttackController>();
            grunt.transform.position = _hero.transform.position + Vector3.right * .4f;
            _hero.GetComponent<Targeting>().SetCandidates(new[] { grunt });
            attack.enabled = true;

            // Every third Daggers strike crits (RelicForgeFlowTests): each strike crosses its cuts on the struck body, and
            // the crit lays its star over them and shows its number in the crit font.
            SpriteRenderer first = null, star = null;
            for (int i = 0; i < 300 && star == null; i++)
            {
                yield return null;
                grunt.Heal(grunt.Maximum);
                List<SpriteRenderer> cuts = Shown("VFX Double Slash");
                if (first == null && cuts.Count > 0)
                {
                    first = cuts[0];
                    Assert.That(first.sprite.texture.name, Is.EqualTo(SheetName), "Cut from the painted sheet.");
                    Assert.That(first.flipX, Is.False, "To the hero's right, the first cut leads rightward, away from it.");
                    Assert.That(first.transform.position.x, Is.EqualTo(grunt.transform.position.x).Within(.15f), "On the struck body.");
                    Assert.That(first.transform.position.y, Is.GreaterThan(grunt.transform.position.y + .2f));
                    List<SpriteRenderer> sparks = Shown("VFX_spark_");
                    Assert.That(sparks.Count, Is.GreaterThan(0), "The painted hit spark lands with it.");
                    Assert.That(first.sortingOrder, Is.GreaterThan(sparks[0].sortingOrder), "The cuts draw over the spark.");
                    List<List<SpriteRenderer>> numbers = Numbers(_pool);
                    Assert.That(numbers.Count, Is.EqualTo(1), "And the hit's number.");
                    Assert.That(numbers[0][0].sprite.texture, Is.SameAs(first.sprite.texture), "From the same sheet, so they batch.");
                }
                List<SpriteRenderer> stars = Shown("VFX Crit Star");
                if (stars.Count > 0)
                {
                    star = stars[0];
                    SpriteRenderer under = null;
                    foreach (var cut in cuts)
                        if (Vector3.Distance(cut.transform.position, star.transform.position) < 1e-4f) under = cut;
                    Assert.That(under, Is.Not.Null, "The crit's star stands on its strike's cuts.");
                    Assert.That(star.sortingOrder, Is.GreaterThan(under.sortingOrder), "Over them.");
                    Assert.That(star.sprite.texture, Is.SameAs(under.sprite.texture), "One sheet for both.");
                    Assert.That(Shown("VFX Crit Digit ").Count, Is.GreaterThan(0), "Its number in the crit font.");
                }
            }
            Assert.That(first, Is.Not.Null, "The Daggers struck.");
            Assert.That(star, Is.Not.Null, "A crit came within three strikes.");
            Assert.That(attack.AttackCount, Is.GreaterThanOrEqualTo(3));

            // A strike's cuts and star are over in a fifth of a second, well before the next strike.
            for (int i = 0; i < 15; i++)
            {
                yield return null;
                grunt.Heal(grunt.Maximum);
            }
            Assert.That(star.enabled, Is.False, "The star is gone.");
            Assert.That(Shown("VFX Double Slash").Count, Is.Zero, "And the cuts.");

            // To the hero's left, the cuts are mirrored, so the first still leads away from the hero.
            grunt.transform.position = _hero.transform.position + Vector3.left * .4f;
            List<SpriteRenderer> mirrored = null;
            for (int i = 0; i < 60 && (mirrored == null || mirrored.Count == 0); i++)
            {
                yield return null;
                grunt.Heal(grunt.Maximum);
                mirrored = Shown("VFX Double Slash");
            }
            Assert.That(mirrored, Is.Not.Empty);
            Assert.That(mirrored[0].flipX, Is.True);
            attack.enabled = false;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PaintedNumbersKeepThePixelSpacingRiseAndGoFromTheirOwnPool()
        {
            yield return Load();
            int renderers = _pool.GetComponentsInChildren<SpriteRenderer>(true).Length;
            Vector3 at = _hero.transform.position + Vector3.up * 2f;
            var shown = new[] { (7, false), (48, false), (315, false), (1250, true) };
            for (int i = 0; i < shown.Length; i++)
                Call(_effects, "ShowNumber", shown[i].Item1, shown[i].Item2, at + Vector3.right * (i - 1.5f) * 1.2f);
            Assert.That(_effects.ActiveNumberCount, Is.EqualTo(shown.Length));

            List<List<SpriteRenderer>> numbers = Numbers(_pool);
            Assert.That(numbers.Count, Is.EqualTo(shown.Length));
            Texture sheet = numbers[0][0].sprite.texture;
            Assert.That(sheet.name, Is.EqualTo(SheetName));
            float normalHeight = 0f, critHeight = 0f;
            foreach (List<SpriteRenderer> digits in numbers)
            {
                string text = Reads(digits, out bool critical);
                int index = System.Array.FindIndex(shown, s => s.Item1.ToString() == text);
                Assert.That(index, Is.Not.Negative, $"{text} reads as a number that was shown.");
                Assert.That(critical, Is.EqualTo(shown[index].Item2), $"{text}: in its own font.");
                // The pixel numerals' spacing, set left to right from the numerals' bottom-left corners.
                float stride = EffectArt.DigitStride * (critical ? EffectArt.CritNumberScale : 1) / PixelSpriteFactory.PixelsPerUnit;
                foreach (SpriteRenderer digit in digits)
                {
                    Assert.That(digit.sprite.texture, Is.SameAs(sheet), $"{text}: every numeral on one sheet, so numbers batch.");
                    Assert.That(digit.sprite.pivot, Is.EqualTo(Vector2.zero), $"{text}: pinned at its bottom-left corner.");
                    Assert.That(digit.transform.localPosition.y, Is.Zero);
                    Assert.That(digit.sortingOrder, Is.EqualTo(digits[0].sortingOrder));
                }
                for (int d = 1; d < digits.Count; d++)
                    Assert.That(digits[d].transform.localPosition.x - digits[d - 1].transform.localPosition.x, Is.EqualTo(stride).Within(1e-5f),
                        $"{text}: numerals a stride apart.");
                float row = digits[digits.Count - 1].transform.localPosition.x + digits[0].sprite.bounds.size.x;
                Assert.That(digits[0].transform.localPosition.x + row, Is.EqualTo(0f).Within(1e-4f), $"{text}: centred where it rises from.");
                if (critical)
                    critHeight = digits[0].sprite.bounds.size.y;
                else
                    normalHeight = digits[0].sprite.bounds.size.y;
            }
            Assert.That(critHeight / normalHeight, Is.InRange(1.8f, 2.2f), "A crit is twice the size, as the pixel crit is.");

            Transform rising = numbers[0][0].transform.parent;
            float from = rising.position.y;
            for (int i = 0; i < 30; i++) yield return null;
            Assert.That(rising.position.y, Is.GreaterThan(from + .3f), "It rises.");
            Assert.That(numbers[0][0].color.a, Is.LessThan(1f), "And fades over its last part.");
            for (int i = 0; i < 20; i++) yield return null;
            Assert.That(_effects.ActiveNumberCount, Is.Zero, "Gone in the number's time.");
            Assert.That(Numbers(_pool), Is.Empty);

            // A crowd's numbers past the pool's size: the oldest gives way, and no renderer is made for any.
            for (int i = 0; i < _effects.NumberSlotCount + 2; i++)
                Call(_effects, "ShowNumber", i + 1, i % 3 == 0, at);
            Assert.That(_effects.ActiveNumberCount, Is.EqualTo(_effects.NumberSlotCount));
            Assert.That(_pool.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(renderers));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PaintedSmokeRisesFromAFallWithoutRemainsAndPuffsFaintAtAnArrival()
        {
            yield return Load();
            // An enemy without painted remains (a drawn look) falls in smoke.
            var fallen = new GameObject("Fallen without remains").AddComponent<Health>();
            fallen.Initialize(10f);
            fallen.transform.position = _hero.transform.position + Vector3.right * 1.5f;
            Call(_effects, "OnEnemyDefeated", fallen);
            List<SpriteRenderer> smoke = Shown("VFX Smoke");
            Assert.That(smoke.Count, Is.EqualTo(1), "It falls in smoke.");
            SpriteRenderer death = smoke[0];
            Assert.That(death.sprite.texture.name, Is.EqualTo(SheetName));
            Assert.That(death.transform.localScale.x, Is.EqualTo(.55f).Within(1e-4f), "Sized as the pixel smoke was.");
            Assert.That(death.transform.position.x, Is.EqualTo(fallen.transform.position.x).Within(1e-4f), "Where it fell.");
            float risenFrom = death.transform.position.y;
            Sprite start = death.sprite;
            for (int i = 0; i < 8; i++) yield return null;
            Assert.That(death.transform.position.y, Is.GreaterThan(risenFrom), "It rises.");
            Assert.That(death.sprite, Is.Not.SameAs(start), "And billows.");
            for (int i = 0; i < 16; i++) yield return null;
            Assert.That(death.enabled, Is.False, "Gone in the pixel smoke's time.");

            // A horde newcomer's arrival: the same smoke, smaller and faint, round its ankles.
            Vector3 feet = _hero.transform.position + Vector3.left * 1.5f;
            Call(_effects, "ShowArrivalPuff", feet);
            smoke = Shown("VFX Smoke");
            Assert.That(smoke.Count, Is.EqualTo(1));
            SpriteRenderer puff = smoke[0];
            Assert.That(puff.transform.localScale.x, Is.EqualTo(.4f).Within(1e-4f));
            Assert.That(puff.color.a, Is.EqualTo(.55f).Within(1e-3f), "Faint.");
            Assert.That(puff.transform.position.y, Is.EqualTo(feet.y + .08f).Within(1e-4f), "Round its ankles.");
            Assert.That(puff.sortingOrder, Is.EqualTo(death.sortingOrder), "Sorted as the death smoke.");
            float faintest = 1f;
            for (int i = 0; i < 24 && puff.enabled; i++)
            {
                yield return null;
                if (puff.enabled)
                    faintest = Mathf.Min(faintest, puff.color.a);
            }
            Assert.That(faintest, Is.LessThan(.55f), "It fades as it goes.");
            Assert.That(puff.enabled, Is.False, "Gone in the pixel puff's time.");
            Object.Destroy(fallen.gameObject);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AGoldChestSpinsThreePaintedCoinsUpAndDownAndShowsItsGoldInTheCritFont()
        {
            yield return Load();
            Vector3 chest = _hero.transform.position + Vector3.right * 1.5f;
            Call(_effects, "OnChestOpened", new ChestReward(ChestRewardKind.Gold, 0f, 25), chest);
            List<SpriteRenderer> coins = Shown("VFX Coin");
            Assert.That(coins.Count, Is.EqualTo(3), "Three coins pop from it.");
            List<List<SpriteRenderer>> numbers = Numbers(_pool);
            Assert.That(numbers.Count, Is.EqualTo(1));
            Assert.That(Reads(numbers[0], out bool critical), Is.EqualTo("25"));
            Assert.That(critical, Is.True, "Its gold in the crit font.");
            var start = new Vector3[coins.Count];
            var faces = new Sprite[coins.Count];
            for (int i = 0; i < coins.Count; i++)
            {
                Assert.That(coins[i].sprite.texture, Is.SameAs(numbers[0][0].sprite.texture), "Coins and numbers share the sheet.");
                Assert.That(coins[i].sortingOrder, Is.EqualTo(coins[0].sortingOrder));
                start[i] = coins[i].transform.position;
                faces[i] = coins[i].sprite;
            }
            for (int i = 0; i < 6; i++) yield return null;
            for (int i = 0; i < coins.Count; i++)
            {
                Assert.That(coins[i].transform.position.y, Is.GreaterThan(start[i].y), $"Coin {i} pops up.");
                Assert.That(coins[i].sprite, Is.Not.SameAs(faces[i]), $"Coin {i} spins.");
            }
            float left = float.MaxValue, right = float.MinValue;
            foreach (SpriteRenderer coin in coins)
            {
                left = Mathf.Min(left, coin.transform.position.x);
                right = Mathf.Max(right, coin.transform.position.x);
            }
            Assert.That(right - left, Is.GreaterThan(.1f), "They spread.");
            // Up to the top of the arc and back down while they fly.
            float peak = coins[1].transform.position.y, last = peak;
            for (int i = 0; i < 22 && coins[1].enabled; i++)
            {
                yield return null;
                if (!coins[1].enabled)
                    break;
                last = coins[1].transform.position.y;
                peak = Mathf.Max(peak, last);
            }
            Assert.That(peak, Is.GreaterThan(start[1].y + .2f), "High enough to read as a pop.");
            Assert.That(last, Is.LessThan(peak), "And fall.");
            for (int i = 0; i < 10; i++) yield return null;
            foreach (SpriteRenderer coin in coins)
                Assert.That(coin.enabled, Is.False, "Gone in the coins' time.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WithoutTheSetAViewDrawsThePixelArtLaidOutAsBefore()
        {
            yield return Load();
            // A second view over the same scene, given everything the scene's has but the feedback set.
            var holder = new GameObject("Pixel feedback");
            holder.SetActive(false);
            var pixel = holder.AddComponent<CombatEffectsView>();
            foreach (string field in new[] { "_encounter", "_hero", "_heroAttack", "_heroAbility", "_chests", "_camera", "_material",
                         "_paintedArt", "_skillArt" })
            {
                FieldInfo info = typeof(CombatEffectsView).GetField(field, Private);
                Assert.That(info, Is.Not.Null, field);
                info.SetValue(pixel, info.GetValue(_effects));
            }
            holder.SetActive(true);
            Assert.That(pixel.UsesPaintedFeedback, Is.False);

            foreach ((string field, int frames) in new[] { ("_doubleSlash", EffectArt.DoubleSlashFrames().Length),
                         ("_star", EffectArt.StarBurstFrames().Length), ("_smoke", EffectArt.SmokeFrames().Length),
                         ("_coin", EffectArt.CoinFrames().Length) })
            {
                var sprites = (Sprite[])typeof(CombatEffectsView).GetField(field, Private).GetValue(pixel);
                Assert.That(sprites.Length, Is.EqualTo(frames), $"{field}: EffectArt's frames.");
                foreach (Sprite sprite in sprites)
                {
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(PixelSpriteFactory.PixelsPerUnit), field);
                    Assert.That(sprite.texture.filterMode, Is.EqualTo(FilterMode.Point), $"{field}: crisp pixel art.");
                }
            }

            Vector3 at = _hero.transform.position + Vector3.up * 2f;
            Call(pixel, "ShowNumber", 315, false, at);
            Call(pixel, "ShowNumber", 1250, true, at + Vector3.right * 2f);
            List<List<SpriteRenderer>> numbers = Numbers(holder.transform.Find("Combat Effects"));
            Assert.That(numbers.Count, Is.EqualTo(2));
            foreach (List<SpriteRenderer> digits in numbers)
            {
                string text = Reads(digits, out bool critical);
                int scale = critical ? EffectArt.CritNumberScale : 1;
                Assert.That(text, Is.EqualTo(critical ? "1250" : "315"));
                Assert.That(digits[0].sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                // Exactly the pixel layout: a stride of six texels, the row the bare numerals and an outline either side.
                float texel = scale / PixelSpriteFactory.PixelsPerUnit;
                Assert.That(digits[0].transform.localPosition.x, Is.EqualTo(-(EffectArt.NumberWidth(text.Length) + 2) * texel * .5f));
                for (int d = 1; d < digits.Count; d++)
                    Assert.That(digits[d].transform.localPosition.x - digits[d - 1].transform.localPosition.x,
                        Is.EqualTo(EffectArt.DigitStride * texel));
            }
            Object.Destroy(holder);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
