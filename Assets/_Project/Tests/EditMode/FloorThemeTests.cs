using Cryptforge.Content;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // Floor 2 (the Quicksilver Vaults) is dressed in ice; floor 1 keeps the foundry, and so does any floor that says nothing.
    public sealed class FloorThemeTests
    {
        const string SecondFloor = "Assets/_Project/Data/Floors/Floor_QuicksilverVaults.asset";
        const string ThirdFloor = "Assets/_Project/Data/Floors/Floor_GloamGardens.asset";

        [Test]
        public void FloorTwoPicksIceAndFloorOneTheFoundry()
        {
            var second = AssetDatabase.LoadAssetAtPath<FloorDefinition>(SecondFloor);
            Assert.That(second, Is.Not.Null);
            Assert.That(second.Theme, Is.EqualTo(FloorTheme.Ice));
            int firsts = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:FloorDefinition", new[] { "Assets/_Project/Data/Floors" }))
            {
                var floor = AssetDatabase.LoadAssetAtPath<FloorDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (floor == null || floor.NextFloor != second)
                    continue;
                firsts++;
                Assert.That(floor.Theme, Is.EqualTo(FloorTheme.Foundry), floor.name + " descends to floor 2 and stays foundry.");
            }
            Assert.That(firsts, Is.GreaterThan(0), "Some authored floor descends to the Quicksilver Vaults.");
            Assert.That(ScriptableObject.CreateInstance<FloorDefinition>().Theme, Is.EqualTo(FloorTheme.Foundry));
        }

        // Floor 3 (Gloam Gardens, docs/48) wears its own look (docs/51): descending from floor 2's ice dresses the region
        // overgrown; floors 1 and 2 keep theirs.
        [Test]
        public void FloorThreePicksTheGloamAndFloorsOneAndTwoKeepTheirs()
        {
            var second = AssetDatabase.LoadAssetAtPath<FloorDefinition>(SecondFloor);
            Assert.That(second, Is.Not.Null);
            Assert.That(second.Theme, Is.EqualTo(FloorTheme.Ice));
            FloorDefinition third = second.NextFloor;
            Assert.That(third, Is.Not.Null);
            Assert.That(third.Id, Is.EqualTo("floor_gloam_gardens"));
            Assert.That(third.Theme, Is.EqualTo(FloorTheme.Gloam));
            Assert.That(AssetDatabase.LoadAssetAtPath<FloorDefinition>(ThirdFloor), Is.SameAs(third));
            Assert.That((int)FloorTheme.Gloam, Is.EqualTo(2), "The asset stores the number.");
        }

        [Test]
        public void GloamTurnsBrassVerdigrisLavaGloamlightAndKeepsAlpha()
        {
            Color brass = FoundryRegionView.GloamTint(new Color(.9f, .62f, .25f, .8f));
            Assert.That(brass.r, Is.LessThan(.9f), "A warm vertex colour cools off its orange.");
            Assert.That(brass.a, Is.EqualTo(.8f));
            Color stone = FoundryRegionView.GloamTint(new Color(.7f, .7f, .7f, 1f));
            Assert.That(stone.g, Is.GreaterThanOrEqualTo(stone.b), "Stone leans olive, not blue.");
            Assert.That(Saturation(stone), Is.LessThan(.2f), "Stone stays desaturated (docs/51 section 7).");
            Color lava = FoundryRegionView.GloamTint(new Color(1.4f, .6f, .18f, 1f));
            Assert.That(lava.g, Is.GreaterThan(lava.r), "Lava becomes gloamlight (hue 78: more green than red).");
            Assert.That(lava.r, Is.GreaterThan(lava.b), "Gloamlight, not cyan.");
            Color rail = FoundryRegionView.GloamRail(new Color(1f, .95f, .82f, 1f), false);
            Assert.That(Mathf.Max(rail.r, Mathf.Max(rail.g, rail.b)), Is.LessThan(.6f), "The sap channel is dim, never neon.");
            Assert.That(rail.g, Is.GreaterThan(rail.b), "Sap, not ice.");
            Color pool = FoundryRegionView.GloamTint(new Color(1.9f, 1.4f, .8f, 1f));
            Assert.That(Mathf.Max(pool.r, Mathf.Max(pool.g, pool.b)), Is.LessThan(1.2f), "A fire's pool on stone does not blow out.");
            Color flame = FoundryRegionView.GloamTint(new Color(1.6f, 1.1f, .4f, 1f), false, true);
            Assert.That(flame.g, Is.GreaterThan(flame.r), "A flame becomes a gloamlight lantern, never amber.");
            Color ember = FoundryRegionView.GloamAtmosphere(new Color(1f, .5f, .12f, .8f));
            Assert.That(Saturation(ember), Is.LessThan(.2f), "An ember becomes a grey ash flake.");
            Assert.That(ember.a, Is.EqualTo(.8f));
            Color mote = FoundryRegionView.GloamAtmosphere(new Color(1f, .5f, .12f, .8f), true);
            Assert.That(mote.g, Is.GreaterThan(mote.r), "One mote in five is gloamlight pollen.");
            // No colour of the region's dress lands in the enemies' orchid band (hue 280-330, docs/51 section 7.3).
            foreach (Color c in new[] { brass, stone, lava, rail, pool, flame, ember, mote,
                FoundryRegionView.GloamRail(new Color(1f, .95f, .82f, 1f), true), FoundryRegionView.GloamBackdrop(Color.white) })
            {
                Color.RGBToHSV(new Color(Mathf.Clamp01(c.r / 2), Mathf.Clamp01(c.g / 2), Mathf.Clamp01(c.b / 2)), out float h, out float s, out _);
                Assert.That(s < .05f || h * 360f < 280f || h * 360f > 330f, Is.True, "Orchid is the enemies' alone: " + c);
            }
        }

        // The Ice path is the one it was before the theme table: the same colours for the same inputs.
        [Test]
        public void TheIceTintIsUnchangedByTheThemeTable()
        {
            AssertColour(FoundryRegionView.IceTint(new Color(.9f, .62f, .25f, .8f)), FoundryRegionView.Tint(FloorTheme.Ice, new Color(.9f, .62f, .25f, .8f)));
            AssertColour(FoundryRegionView.IceTint(new Color(.7f, .7f, .7f, 1f), true), new Color(.30f * .7f, .33f * .7f, .39f * .7f, 1f));
            Color stone = FoundryRegionView.IceTint(new Color(.5f, .5f, .5f, 1f));
            AssertColour(stone, new Color(.93f * .5f, .96f * .5f, .5f, 1f));
            Color lava = FoundryRegionView.IceTint(new Color(1.4f, .6f, .18f, 1f), false, true);
            AssertColour(lava, new Color(127f / 255 * .7f * 1.4f, 227f / 255 * .7f * 1.4f, .7f * 1.4f, 1f));
            Color rail = FoundryRegionView.IceRail(new Color(.5f, .5f, .5f, 1f), false);
            AssertColour(rail, new Color(95f / 255 * .45f, 168f / 255 * .45f, 200f / 255 * .45f, 1f));
            Color smoke = FoundryRegionView.ColdAtmosphere(new Color(.5f, .5f, .5f, .3f));
            AssertColour(smoke, new Color(110f / 255 * .95f, 130f / 255 * .95f, 153f / 255 * .95f, .3f));
        }

        static void AssertColour(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(1e-4f), actual + " vs " + expected);
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(1e-4f), actual + " vs " + expected);
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(1e-4f), actual + " vs " + expected);
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(1e-4f), actual + " vs " + expected);
        }

        static float Saturation(Color c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)), min = Mathf.Min(c.r, Mathf.Min(c.g, c.b));
            return max <= 1e-4f ? 0f : (max - min) / max;
        }

        [Test]
        public void IceTurnsBrassColdAndKeepsStoneNeutralAndAlpha()
        {
            Color brass = FoundryRegionView.IceTint(new Color(.9f, .62f, .25f, .8f));
            Assert.That(brass.b, Is.GreaterThan(brass.r), "A warm vertex colour turns cold.");
            Assert.That(brass.a, Is.EqualTo(.8f));
            Color stone = FoundryRegionView.IceTint(new Color(.7f, .7f, .7f, 1f));
            Assert.That(stone.b, Is.GreaterThan(stone.r), "Stone leans cold.");
            Assert.That(stone.r, Is.LessThan(.7f), "Stone darkens.");
            Color lava = FoundryRegionView.IceTint(new Color(1.4f, .6f, .18f, 1f));
            Assert.That(lava.b, Is.GreaterThan(lava.r), "Lava becomes cold light.");
            Color rail = FoundryRegionView.IceRail(new Color(1f, .95f, .82f, 1f), false);
            Assert.That(rail.b, Is.GreaterThan(.35f), "The brass rail becomes mirror-ice, not dark.");
            Assert.That(rail.b, Is.LessThan(.7f), "Inlaid ice, not neon.");
            Color pool = FoundryRegionView.IceTint(new Color(1.9f, 1.4f, .8f, 1f));
            Assert.That(Mathf.Max(pool.r, Mathf.Max(pool.g, pool.b)), Is.LessThan(1.2f), "A fire's pool on stone does not blow out.");
            Color flame = FoundryRegionView.IceTint(new Color(1.6f, 1.1f, .4f, 1f), false, true);
            Assert.That(flame.b, Is.GreaterThan(flame.r), "A flame becomes frost-light.");
            Color ember = FoundryRegionView.ColdAtmosphere(new Color(1f, .5f, .12f, .8f));
            Assert.That(ember.b, Is.GreaterThan(ember.r), "Embers become frost motes.");
            Assert.That(ember.a, Is.EqualTo(.8f));
        }

        [Test]
        public void ThePlatformSetCarriesFloorTwosIceArt()
        {
            var set = AssetDatabase.LoadAssetAtPath<Cryptforge.Art.PlatformMaterialSet>(
                "Assets/_Project/Art/Environment/Platform/PlatformMaterials.asset");
            Assert.That(set, Is.Not.Null);
            Assert.That(set.IcePaving, Is.Not.Null);
            Assert.That(set.IceMasonry, Is.Not.Null);
            Assert.That(set.IceBackdrop, Is.Not.Null);
            // The cold variants of the glowing textures, swapped in by name on floor 2 (fires, pit, boiler windows).
            foreach (string name in new[] { "FoundryEmbers", "FoundryPitLining", "FoundryBoiler", "FoundryShortBoiler", "ForgeKeeper", "FoundryCrane",
                "FoundryPavingV8", "FoundryIronV2", "FoundryCastIronV2", "FoundryBoilerWrap", "ENV_Platform_Floor" })
                Assert.That(Resources.Load<Texture2D>("Ice/" + name), Is.Not.Null, name + " has an ice variant.");
            Assert.That(set.FoundryEmbers.mainTexture.name, Is.EqualTo("FoundryEmbers"));
            Assert.That(set.FoundryPitLining.mainTexture.name, Is.EqualTo("FoundryPitLining"));
        }
    }
}
