using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // docs/57's four new skills painted (Tools/ArtReview/build_hero_skill_vfx.py): the optional families the Gameplay
    // scene's skill set carries - Iron Resolve's bell and rivets, the Shield Rush's wake and impact, the Flame Sigil's glyph
    // and Cinder Ward's shell and flare - how their sheets import and what they cost on the phone, and what each strip
    // paints, read back from the sheets as painted (the imported textures are compressed and unreadable). A hero's effect
    // never paints the red-orange docs/52 section 2.1 rule 3 keeps for what hurts on a timer, nor the orchid docs/51 gives
    // the enemies: the Vanguard's are its steel, Ilya's furnace gold and ash.
    public sealed class PaintedHeroSkillEffectTests
    {
        private const string SetPath = "Assets/_Project/Art/Effects/Skills/SkillEffectArtSet.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // The four sheets together as ASTC 4x4 (a byte a texel), and every painted skill sheet with them: under the 3 MB
        // PaintedSkillEffectTests names for the effects.
        private const int BudgetBytes = 1024 * 1024;
        private const int SkillsBudgetBytes = 3 * 1024 * 1024;

        private SkillEffectArtSet _set;
        private readonly Dictionary<Texture2D, Texture2D> _painted = new Dictionary<Texture2D, Texture2D>();

        [OneTimeSetUp]
        public void Load() => _set = AssetDatabase.LoadAssetAtPath<SkillEffectArtSet>(SetPath);

        [OneTimeTearDown]
        public void Unload()
        {
            foreach (Texture2D texture in _painted.Values)
                Object.DestroyImmediate(texture);
            _painted.Clear();
        }

        // Each family: its name, whose it is, and its strips with the fewest frames each may have.
        private (string Name, bool Vanguard, (SkillEffectArtSet.Strip Strip, string Name, int Frames)[] Strips)[] Families() =>
            new[]
            {
                ("Iron Resolve", true, new[] { (_set.ResolveClose, "close", 3), (_set.ResolveHold, "hold", 4),
                    (_set.ResolveEnd, "end", 3), (_set.ResolveRivets, "rivets", 3) }),
                ("Shield Rush", true, new[] { (_set.RushStreak, "wake", 4), (_set.RushImpact, "impact", 4) }),
                ("Flame Sigil", false, new[] { (_set.SigilIgnite, "ignition", 2), (_set.SigilGlyph, "glyph", 4) }),
                ("Cinder Ward", false, new[] { (_set.WardRaise, "rise", 3), (_set.WardShell, "shell", 6),
                    (_set.WardBreak, "break", 3), (_set.WardFlare, "flare", 3) }),
            };

        [Test]
        public void TheScenesSkillSetCarriesTheFourFamiliesEachOnASheetOfItsOwn()
        {
            Assert.That(_set, Is.Not.Null, SetPath);
            Assert.That(_set.IsValid && _set.HasVentSplash, Is.True, "The rest of the set is as it was.");
            Assert.That(_set.HasIronResolve, Is.True, "Iron Resolve's family is whole.");
            Assert.That(_set.HasShieldRush, Is.True, "The Shield Rush's.");
            Assert.That(_set.HasFlameSigil, Is.True, "The Flame Sigil's.");
            Assert.That(_set.HasCinderWard, Is.True, "Cinder Ward's.");
            Assert.That(File.ReadAllText(ScenePath), Does.Contain(AssetDatabase.AssetPathToGUID(SetPath)),
                "The Gameplay scene's CombatEffectsView borrows it.");

            var sheets = new List<Texture2D> { _set.Flames(1).Sheet, _set.VentColumn.Sheet, _set.Shockwave.Sheet,
                _set.VentSplash.Sheet };
            foreach (var family in Families())
            {
                Texture2D sheet = family.Strips[0].Strip.Sheet;
                sheets.Add(sheet);
                Assert.That(AssetDatabase.GetAssetPath(sheet), Does.StartWith("Assets/_Project/Art/Effects/Skills/"),
                    $"{family.Name}: beside the other skill sheets.");
                var taken = new List<RectInt>();
                foreach (var (strip, name, frames) in family.Strips)
                {
                    string at = $"{family.Name}'s {name}";
                    Assert.That(strip.Sheet, Is.SameAs(sheet), $"{at}: one sheet for the family, so it batches.");
                    Assert.That(strip.Length, Is.GreaterThanOrEqualTo(frames), $"{at}: painted frames.");
                    Assert.That((strip.Sheet.width, strip.Sheet.height), Is.EqualTo((strip.SheetWidth, strip.SheetHeight)),
                        $"{at}: it imports at the size it was painted, so its cells cut true.");
                    foreach (SkillEffectArtSet.Cell cell in strip.Frames)
                    {
                        Assert.That((cell.Width, cell.Height), Is.EqualTo((strip.Frames[0].Width, strip.Frames[0].Height)),
                            $"{at}: every frame cut alike, so it plays about one point.");
                        var grown = new RectInt(cell.X - 1, cell.Y - 1, cell.Width + 2, cell.Height + 2);
                        foreach (RectInt other in taken)
                            Assert.That(grown.Overlaps(other), Is.False, $"{at}: {grown} overlaps {other}.");
                        taken.Add(new RectInt(cell.X, cell.Y, cell.Width, cell.Height));
                    }
                }
            }
            Assert.That(sheets, Is.Unique, "A sheet for each family, beside the Burn's, the Vent's, the Forge's and the eruption ring's.");
        }

        [Test]
        public void TheirSheetsImportAsTheOtherSkillSheetsDoWithinBudget()
        {
            string ventPath = AssetDatabase.GetAssetPath(_set.VentColumn.Sheet);
            long bytes = 0;
            foreach (var family in Families())
            {
                SkillEffectArtSet.Strip strip = family.Strips[0].Strip;
                string path = AssetDatabase.GetAssetPath(strip.Sheet);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), $"{path}: smooth, as the painted actors.");
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.alphaIsTransparency, Is.True, path);
                Assert.That(importer.isReadable, Is.False, path);
                Assert.That(importer.sRGBTexture, Is.True, path);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), $"{path}: cut as painted.");
                TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
                Assert.That(android.overridden, Is.True, path);
                Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4), path);
                Assert.That(android.maxTextureSize, Is.GreaterThanOrEqualTo(Mathf.Max(strip.SheetWidth, strip.SheetHeight)),
                    $"{path}: the phone gets it whole.");
                // And the very importer of the skill sheets beside it, line for line but for its own GUIDs and density.
                Assert.That(ImportSettings(path), Is.EqualTo(ImportSettings(ventPath)), $"{path} imports as {ventPath} does.");
                Assert.That(strip.SheetWidth % 4 == 0 && strip.SheetHeight % 4 == 0, Is.True, $"{path}: whole ASTC blocks.");
                bytes += (long)strip.SheetWidth * strip.SheetHeight;
            }
            Assert.That(bytes, Is.LessThanOrEqualTo(BudgetBytes), "The four skills' effects spend at most a MiB on the phone.");
            long skills = bytes;
            foreach (SkillEffectArtSet.Strip strip in new[] { _set.Flames(1), _set.VentColumn, _set.Shockwave, _set.VentSplash })
                skills += (long)strip.SheetWidth * strip.SheetHeight;
            Assert.That(skills, Is.LessThan(SkillsBudgetBytes), "Every painted skill sheet together stays under 3 MB.");
        }

        [Test]
        public void NoStripPaintsTheTelegraphsRedOrangeNorTheEnemiesOrchidAndEachKeepsItsHerosColour()
        {
            foreach (var family in Families())
            {
                // The Vanguard's steel blue; Ilya's furnace gold, from its deep shade to white-hot.
                float low = family.Vanguard ? 190f : 32f, high = family.Vanguard ? 240f : 60f;
                foreach (var (strip, name, _) in family.Strips)
                {
                    string at = $"{family.Name}'s {name}";
                    int own = 0;
                    for (int i = 0; i < strip.Length; i++)
                    {
                        foreach (Color32 c in Frame(strip, i, out _, out _))
                        {
                            if (c.a <= 32)
                                continue;
                            Color.RGBToHSV(c, out float h, out float s, out float v);
                            float hue = h * 360f;
                            bool vivid = s >= .35f && v >= .35f;
                            Assert.That(vivid && (hue < 30f || hue >= 345f), Is.False,
                                $"{at}, frame {i}: {c} is red-orange (docs/52 section 2.1 rule 3).");
                            Assert.That(vivid && hue >= 280f && hue <= 330f, Is.False, $"{at}, frame {i}: {c} is the enemies' orchid.");
                            if (c.a > 64 && v > .7f && s >= .2f)
                            {
                                own++;
                                Assert.That(hue, Is.InRange(low, high), $"{at}, frame {i}: {c} is not its hero's colour.");
                            }
                        }
                    }
                    Assert.That(own, Is.GreaterThan(0), $"{at}: it shows in its hero's colour.");
                }
            }
        }

        [Test]
        public void IronResolvesBellClosesHoldsClearOverTheHeroAndFallsAway()
        {
            foreach (SkillEffectArtSet.Strip strip in new[] { _set.ResolveClose, _set.ResolveHold, _set.ResolveEnd })
            {
                Assert.That(strip.Pivot.x, Is.EqualTo(.5f).Within(.03f), "The bell pins at the hero's feet, under its middle.");
                Assert.That(strip.Pivot.y, Is.InRange(.05f, .35f), "Its feet low in the frame, the floor's depth under them.");
            }
            SkillEffectArtSet.Strip hold = _set.ResolveHold;
            float unit = hold.TexelsPerUnit;
            int holdLeft = int.MaxValue;
            for (int i = 0; i < hold.Length; i++)
            {
                Color32[] texels = Frame(hold, i, out int w, out int h);
                string at = $"The hold, frame {i}";
                Assert.That(ClearBorder(texels, w, h), Is.True, $"{at}: a clear texel round the cell.");
                int cx = Mathf.RoundToInt(hold.Pivot.x * w), feet = Mathf.RoundToInt(hold.Pivot.y * h);
                // The Vanguard shows through it: its middle, 0.6 over its feet, under a sheen.
                Assert.That(MaxAlphaNear(texels, w, h, cx, feet + Mathf.RoundToInt(.6f * unit), 4), Is.LessThan(110),
                    $"{at}: the hero shows through the bell.");
                // Its outline stands round the hero's body at either side.
                int body = feet + Mathf.RoundToInt(.5f * unit);
                Assert.That(MaxAlphaNear(texels, w, h, cx - Mathf.RoundToInt(.63f * unit), body, 3), Is.GreaterThan(150),
                    $"{at}: its left side.");
                Assert.That(MaxAlphaNear(texels, w, h, cx + Mathf.RoundToInt(.63f * unit), body, 3), Is.GreaterThan(150),
                    $"{at}: its right side.");
                Assert.That(MaxAlphaNear(texels, w, h, cx, feet + Mathf.RoundToInt(1.55f * unit), 3), Is.GreaterThan(120),
                    $"{at}: its top over the head.");
                Assert.That(Differ(texels, Frame(hold, (i + 1) % hold.Length, out _, out _)), Is.True, $"{at}: its glint moves.");
                holdLeft = Mathf.Min(holdLeft, cx - LeftmostVisible(texels, w, h));
            }

            // Closing: the staves fly in wider than it holds, then lock with a white clang.
            SkillEffectArtSet.Strip close = _set.ResolveClose;
            Color32[] first = Frame(close, 0, out int fw, out int fh);
            float firstLeft = (Mathf.RoundToInt(close.Pivot.x * fw) - LeftmostVisible(first, fw, fh)) / close.TexelsPerUnit;
            Assert.That(firstLeft, Is.GreaterThan(holdLeft / unit + .1f), "The staves start out from the bell.");
            int clang = -1, whitest = -1;
            for (int i = 0; i < close.Length; i++)
            {
                int white = Count(Frame(close, i, out _, out _), IsNearWhite);
                if (white > whitest)
                {
                    whitest = white;
                    clang = i;
                }
            }
            Assert.That(clang, Is.GreaterThan(0).And.LessThan(close.Length), "They lock with a white clang after they fly in.");

            // Falling away: it flashes as it cracks, thins frame by frame and leaves only steel flakes.
            SkillEffectArtSet.Strip end = _set.ResolveEnd;
            Assert.That(Count(Frame(end, 0, out _, out _), IsNearWhite), Is.GreaterThan(whitest / 4), "A clear end: it flashes.");
            int last = int.MaxValue;
            for (int i = 0; i < end.Length; i++)
            {
                int alpha = TotalAlpha(Frame(end, i, out _, out _));
                Assert.That(alpha, Is.LessThan(last), $"The end, frame {i}: thinner than the frame before.");
                last = alpha;
            }
            Assert.That(Count(Frame(end, end.Length - 1, out _, out _), IsNearWhite), Is.Zero, "Then only steel flakes.");

            // The rivets: a unit floor ring round the feet, half as deep as it is wide, fading over its frames.
            SkillEffectArtSet.Strip rivets = _set.ResolveRivets;
            AssertFloorRing(rivets, "The rivets", 1f);
            Assert.That(TotalAlpha(Frame(rivets, rivets.Length - 1, out _, out _)),
                Is.LessThan(TotalAlpha(Frame(rivets, 0, out _, out _))), "The rivets dim.");
        }

        [Test]
        public void TheRushTrailsAWakeBehindItsShieldAndStrikesAheadOfIt()
        {
            SkillEffectArtSet.Strip wake = _set.RushStreak;
            Assert.That(wake.Pivot.x, Is.GreaterThan(.7f), "The wake pins at its head, leading along +x.");
            Assert.That(wake.Pivot.y, Is.EqualTo(.5f).Within(.05f));
            float unit = wake.TexelsPerUnit;
            float longest = 0f;
            for (int i = 0; i < wake.Length; i++)
            {
                Color32[] texels = Frame(wake, i, out int w, out int h);
                string at = $"The wake, frame {i}";
                Assert.That(ClearBorder(texels, w, h), Is.True, $"{at}: a clear texel round the cell.");
                int head = Mathf.RoundToInt(wake.Pivot.x * w);
                Assert.That(RightmostVisible(texels, w, h), Is.LessThan(head + .45f * unit), $"{at}: little leads the shield.");
                longest = Mathf.Max(longest, (head - LeftmostVisible(texels, w, h)) / unit);
                Assert.That(Count(texels, IsSteel), Is.GreaterThan(0), $"{at}: steel blue.");
            }
            Assert.That(longest, Is.GreaterThan(1.2f), "At its longest the wake trails well behind the hero.");

            SkillEffectArtSet.Strip impact = _set.RushImpact;
            Assert.That(impact.Pivot.x, Is.InRange(.35f, .65f), "The impact pins at the shield's contact.");
            Color32[] hit = Frame(impact, 0, out int iw, out int ih);
            int cx = Mathf.RoundToInt(impact.Pivot.x * iw);
            int ahead = 0, behind = 0;
            for (int y = 0; y < ih; y++)
            {
                for (int x = 0; x < iw; x++)
                {
                    if (!IsNearWhite(hit[y * iw + x]))
                        continue;
                    if (x > cx + 4)
                        ahead++;
                    else if (x < cx - 4)
                        behind++;
                }
            }
            Assert.That(ahead, Is.GreaterThan(behind * 2), "Its crescent of shocked air stands ahead, along the dash.");
            Assert.That(TotalAlpha(Frame(impact, impact.Length - 1, out _, out _)), Is.LessThan(TotalAlpha(hit)),
                "And settles into dust.");
            for (int i = 0; i < impact.Length; i++)
            {
                Color32[] texels = Frame(impact, i, out int w, out int h);
                Assert.That(ClearBorder(texels, w, h), Is.True, $"The impact, frame {i}: a clear texel round the cell.");
            }
        }

        [Test]
        public void TheSigilIsAUnitGlyphOfRunesAndAGrateHotterAsItIgnites()
        {
            int hottestGlyph = 0;
            foreach (SkillEffectArtSet.Strip strip in new[] { _set.SigilIgnite, _set.SigilGlyph })
            {
                bool glyph = strip == _set.SigilGlyph;
                Assert.That(strip.Pivot.x, Is.EqualTo(.5f).Within(.02f), "It pins at its centre, where Ilya drew it.");
                for (int i = 0; i < strip.Length; i++)
                {
                    string at = $"The {(glyph ? "glyph" : "ignition")}, frame {i}";
                    Color32[] texels = Frame(strip, i, out int w, out int h);
                    Assert.That(ClearBorder(texels, w, h), Is.True, $"{at}: a clear texel round the cell.");
                    float cx = strip.Pivot.x * w, cy = strip.Pivot.y * h, unit = strip.TexelsPerUnit;
                    // A unit floor ring: its band ends a unit out across the floor, and half that toward the camera, where
                    // no flame licks so the floor stays clear before what stands in it.
                    Assert.That(Outermost(texels, w, h, cx, cy, unit, 1, 0), Is.InRange(.95f, 1.07f), $"{at}: a unit to the right.");
                    Assert.That(Outermost(texels, w, h, cx, cy, unit, -1, 0), Is.InRange(.95f, 1.07f), $"{at}: and to the left.");
                    Assert.That(Outermost(texels, w, h, cx, cy, unit, 0, -1), Is.InRange(.95f, 1.07f),
                        $"{at}: and toward the camera, at the floor's depth.");
                    if (!glyph)
                        continue;
                    Assert.That(texels[Mathf.FloorToInt(cy) * w + Mathf.FloorToInt(cx)].a, Is.LessThan(32),
                        $"{at}: Ilya stands in a clear middle.");
                    float runes = BandShare(texels, w, h, cx, cy, unit, .84f, .94f);
                    Assert.That(runes, Is.InRange(.03f, .6f), $"{at}: runes between the band's lines, not a solid band.");
                    Assert.That(Differ(texels, Frame(strip, (i + 1) % strip.Length, out _, out _)), Is.True,
                        $"{at}: its runes' heat runs round it.");
                    hottestGlyph = Mathf.Max(hottestGlyph, Count(texels, IsWhiteHot));
                }
            }
            int igniting = 0;
            for (int i = 0; i < _set.SigilIgnite.Length; i++)
                igniting = Mathf.Max(igniting, Count(Frame(_set.SigilIgnite, i, out _, out _), IsWhiteHot));
            Assert.That(igniting, Is.GreaterThan(hottestGlyph), "It ignites white-hot, then burns gold.");
        }

        [Test]
        public void TheWardWhirlsAshAndCindersRoundIlyaAndBlowsApart()
        {
            foreach (SkillEffectArtSet.Strip strip in new[] { _set.WardRaise, _set.WardShell, _set.WardBreak })
            {
                Assert.That(strip.Pivot.x, Is.EqualTo(.5f).Within(.03f), "The shell pins at Ilya's feet.");
                Assert.That(strip.Pivot.y, Is.InRange(.05f, .4f));
            }
            SkillEffectArtSet.Strip shell = _set.WardShell;
            float unit = shell.TexelsPerUnit;
            for (int i = 0; i < shell.Length; i++)
            {
                Color32[] texels = Frame(shell, i, out int w, out int h);
                string at = $"The shell, frame {i}";
                Assert.That(ClearBorder(texels, w, h), Is.True, $"{at}: a clear texel round the cell.");
                Assert.That(Count(texels, IsAsh), Is.GreaterThan(20), $"{at}: ash-white flakes and gusts.");
                Assert.That(Count(texels, IsGold), Is.GreaterThan(20), $"{at}: cinders.");
                int cx = Mathf.RoundToInt(shell.Pivot.x * w), feet = Mathf.RoundToInt(shell.Pivot.y * h);
                Assert.That(MedianAlpha(texels, w, h, cx, feet + Mathf.RoundToInt(.6f * unit), Mathf.RoundToInt(.12f * unit)),
                    Is.LessThan(100), $"{at}: Ilya shows through it.");
                Assert.That(Differ(texels, Frame(shell, (i + 1) % shell.Length, out _, out _)), Is.True, $"{at}: it whirls.");
            }
            SkillEffectArtSet.Strip blown = _set.WardBreak;
            int lastSpan = 0, lastAlpha = int.MaxValue;
            for (int i = 0; i < blown.Length; i++)
            {
                Color32[] texels = Frame(blown, i, out int w, out int h);
                int span = RightmostVisible(texels, w, h) - LeftmostVisible(texels, w, h);
                Assert.That(span, Is.GreaterThan(lastSpan), $"The break, frame {i}: it blows wider.");
                int alpha = TotalAlpha(texels);
                if (i > 0)
                    Assert.That(alpha, Is.LessThan(lastAlpha), $"The break, frame {i}: and thins.");
                lastSpan = span;
                lastAlpha = alpha;
            }
            SkillEffectArtSet.Strip flare = _set.WardFlare;
            Assert.That(flare.Pivot.x, Is.EqualTo(.5f).Within(.04f), "At the striker's middle.");
            Assert.That(flare.Pivot.y, Is.EqualTo(.5f).Within(.06f));
            Color32[] burst = Frame(flare, 0, out int fw, out int fh);
            Assert.That(IsWhiteHot(burst[Mathf.FloorToInt(flare.Pivot.y * fh) * fw + Mathf.FloorToInt(flare.Pivot.x * fw)]),
                Is.True, "A white-hot flash where the blow broke.");
            Assert.That(Count(Frame(flare, flare.Length - 1, out _, out _), IsWhiteHot), Is.Zero, "And gone.");
        }

        // A unit floor ring pinned at its centre: a clear middle and its band a unit out across the floor and half that up it.
        private void AssertFloorRing(SkillEffectArtSet.Strip ring, string what, float radius)
        {
            Assert.That(ring.Pivot.x, Is.EqualTo(.5f).Within(.02f), $"{what}: pinned at its centre.");
            Assert.That(ring.Pivot.y, Is.EqualTo(.5f).Within(.03f));
            for (int i = 0; i < ring.Length; i++)
            {
                Color32[] texels = Frame(ring, i, out int w, out int h);
                Assert.That(ClearBorder(texels, w, h), Is.True, $"{what}, frame {i}: a clear texel round the cell.");
                float cx = ring.Pivot.x * w, cy = ring.Pivot.y * h, unit = ring.TexelsPerUnit;
                Assert.That(texels[Mathf.FloorToInt(cy) * w + Mathf.FloorToInt(cx)].a, Is.Zero, $"{what}, frame {i}: a clear middle.");
                Assert.That(Outermost(texels, w, h, cx, cy, unit, 1, 0), Is.InRange(radius - .05f, radius + .1f),
                    $"{what}, frame {i}: a unit across.");
                Assert.That(Outermost(texels, w, h, cx, cy, unit, 0, -1), Is.InRange(radius - .05f, radius + .1f),
                    $"{what}, frame {i}: and toward the camera at the floor's depth.");
            }
        }

        // The texels of one frame, row 0 at the bottom, from the sheet as painted.
        private Color32[] Frame(SkillEffectArtSet.Strip strip, int index, out int width, out int height)
        {
            if (!_painted.TryGetValue(strip.Sheet, out Texture2D sheet))
            {
                sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.That(sheet.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(strip.Sheet))), Is.True);
                _painted[strip.Sheet] = sheet;
            }
            SkillEffectArtSet.Cell cell = strip.Frames[index];
            width = cell.Width;
            height = cell.Height;
            Color[] pixels = sheet.GetPixels(cell.X, cell.Y, cell.Width, cell.Height);
            var texels = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                texels[i] = pixels[i];
            return texels;
        }

        // A sheet's .meta without the lines that are its own: its GUID, its sprite's and its density.
        private static string[] ImportSettings(string path)
        {
            var kept = new List<string>();
            foreach (string line in File.ReadAllLines(path + ".meta"))
            {
                string setting = line.Trim();
                if (!setting.StartsWith("guid:") && !setting.StartsWith("spriteID:") && !setting.StartsWith("spritePixelsToUnits:"))
                    kept.Add(line);
            }
            return kept.ToArray();
        }

        // In units of the strip's texels a unit, the floor distance of the outermost texel over half alpha along a ray from
        // the centre (dy along the floor's depth: a floor unit up the screen is ArenaFloor.DepthScale of one).
        private static float Outermost(Color32[] texels, int width, int height, float cx, float cy, float unit, int dx, int dy)
        {
            for (float r = 1.3f; r > 0f; r -= .002f)
            {
                int x = Mathf.FloorToInt(cx + dx * r * unit);
                int y = Mathf.FloorToInt(cy + dy * r * unit * ArenaFloor.DepthScale);
                if (x >= 0 && y >= 0 && x < width && y < height && texels[y * width + x].a > 128)
                    return r;
            }
            return 0f;
        }

        // The share of the floor annulus between two radii covered over half alpha.
        private static float BandShare(Color32[] texels, int width, int height, float cx, float cy, float unit, float from, float to)
        {
            int inside = 0, covered = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float fx = (x + .5f - cx) / unit, fy = (y + .5f - cy) / unit / ArenaFloor.DepthScale;
                    float d = Mathf.Sqrt(fx * fx + fy * fy);
                    if (d < from || d > to)
                        continue;
                    inside++;
                    if (texels[y * width + x].a > 128 && Luminance(texels[y * width + x]) > 150f)
                        covered++;
                }
            }
            return inside > 0 ? covered / (float)inside : 0f;
        }

        private static bool IsNearWhite(Color32 c) => c.a > 128 && c.r > 220 && c.g > 225 && c.b > 230;
        private static bool IsWhiteHot(Color32 c) => c.a > 200 && c.r > 245 && c.g > 230 && c.b > 170;
        private static bool IsSteel(Color32 c) => c.a > 128 && c.b > c.r + 40;
        private static bool IsAsh(Color32 c) => c.a > 128 && Luminance(c) > 190f && Mathf.Max(c.r, Mathf.Max(c.g, c.b)) -
            Mathf.Min(c.r, Mathf.Min(c.g, c.b)) < 30;
        private static bool IsGold(Color32 c) => c.a > 128 && c.r > 200 && c.g > 120 && c.r > c.b + 80;
        private static float Luminance(Color32 c) => .299f * c.r + .587f * c.g + .114f * c.b;

        private static int Count(Color32[] texels, System.Func<Color32, bool> test)
        {
            int count = 0;
            foreach (Color32 texel in texels)
            {
                if (test(texel))
                    count++;
            }
            return count;
        }

        private static int TotalAlpha(Color32[] texels)
        {
            int total = 0;
            foreach (Color32 texel in texels)
                total += texel.a;
            return total;
        }

        private static bool ClearBorder(Color32[] texels, int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                if (texels[x].a > 0 || texels[(height - 1) * width + x].a > 0)
                    return false;
            }
            for (int y = 0; y < height; y++)
            {
                if (texels[y * width].a > 0 || texels[y * width + width - 1].a > 0)
                    return false;
            }
            return true;
        }

        private static bool Differ(Color32[] a, Color32[] b)
        {
            if (a.Length != b.Length)
                return true;
            for (int i = 0; i < a.Length; i++)
            {
                if (!a[i].Equals(b[i]))
                    return true;
            }
            return false;
        }

        private static int LeftmostVisible(Color32[] texels, int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (texels[y * width + x].a > 40)
                        return x;
                }
            }
            return width;
        }

        private static int RightmostVisible(Color32[] texels, int width, int height)
        {
            for (int x = width - 1; x >= 0; x--)
            {
                for (int y = 0; y < height; y++)
                {
                    if (texels[y * width + x].a > 40)
                        return x;
                }
            }
            return -1;
        }

        private static int MaxAlphaNear(Color32[] texels, int width, int height, int cx, int cy, int radius)
        {
            int max = 0;
            for (int y = Mathf.Max(0, cy - radius); y <= Mathf.Min(height - 1, cy + radius); y++)
            {
                for (int x = Mathf.Max(0, cx - radius); x <= Mathf.Min(width - 1, cx + radius); x++)
                    max = Mathf.Max(max, texels[y * width + x].a);
            }
            return max;
        }

        private static int MedianAlpha(Color32[] texels, int width, int height, int cx, int cy, int radius)
        {
            var alphas = new List<int>();
            for (int y = Mathf.Max(0, cy - radius); y <= Mathf.Min(height - 1, cy + radius); y++)
            {
                for (int x = Mathf.Max(0, cx - radius); x <= Mathf.Min(width - 1, cx + radius); x++)
                    alphas.Add(texels[y * width + x].a);
            }
            alphas.Sort();
            return alphas[alphas.Count / 2];
        }
    }
}
