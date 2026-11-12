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
            Assert.That(brass.b, Is.GreaterThan(brass.r), "Brass becomes ice.");
            Assert.That(brass.a, Is.EqualTo(.8f));
            Color stone = FoundryRegionView.IceTint(new Color(.7f, .7f, .7f, 1f));
            Assert.That(stone.b, Is.GreaterThan(stone.r), "Stone leans cold.");
            Assert.That(stone.r, Is.LessThan(.7f), "Stone darkens.");
            Color lava = FoundryRegionView.IceTint(new Color(1.4f, .6f, .18f, 1f));
            Assert.That(lava.b, Is.GreaterThan(lava.r), "Lava becomes cold light.");
        }
    }
}
