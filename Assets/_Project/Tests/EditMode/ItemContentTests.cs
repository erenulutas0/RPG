using System.Collections.Generic;
using Cryptforge.Content;
using Cryptforge.Progression;
using NUnit.Framework;
using UnityEditor;

namespace Cryptforge.Tests
{
    // The items the Descent drops (docs/54 section 3.2): PrototypeEconomy lists one armour, one amulet and one ring for each
    // of the three floors, with unique ids, and each builds its template.
    public sealed class ItemContentTests
    {
        [Test]
        public void TheEconomyListsOneItemOfEachSlotForEachFloor()
        {
            var economy = AssetDatabase.LoadAssetAtPath<EconomyConfig>("Assets/_Project/Data/Economy/PrototypeEconomy.asset");
            Assert.That(economy, Is.Not.Null);
            Assert.That(economy.ItemCount, Is.EqualTo(9));
            var ids = new HashSet<string>();
            var places = new HashSet<(GearSlot, int)>();
            for (int i = 0; i < economy.ItemCount; i++)
            {
                ItemDefinition item = economy.ItemAt(i);
                Assert.That(item, Is.Not.Null, "item " + i);
                ItemTemplate template = item.CreateTemplate();
                Assert.That(ids.Add(template.Id), Is.True, template.Id + " is unique");
                Assert.That(places.Add((template.Slot, template.Floor)), Is.True, template.Id + " fills its own slot and floor");
                Assert.That(template.DisplayName, Is.Not.Empty);
                Assert.That(template.Id, Does.StartWith("item_"));
            }
        }
    }
}
