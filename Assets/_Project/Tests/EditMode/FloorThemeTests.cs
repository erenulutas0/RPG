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
            Assert.That(rail.b, Is.GreaterThan(.4f), "The brass rail becomes mirror-ice, not dark.");
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
            foreach (string name in new[] { "FoundryEmbers", "FoundryPitLining", "FoundryBoiler", "FoundryShortBoiler" })
                Assert.That(Resources.Load<Texture2D>("Ice/" + name), Is.Not.Null, name + " has an ice variant.");
            Assert.That(set.FoundryEmbers.mainTexture.name, Is.EqualTo("FoundryEmbers"));
            Assert.That(set.FoundryPitLining.mainTexture.name, Is.EqualTo("FoundryPitLining"));
        }
    }
}
