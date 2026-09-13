using System.Collections.Generic;
using Cryptforge.Content;
using Cryptforge.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // A bad Forge entry must stop the scene with one error naming the asset, before a profile or a run exists.
    public sealed class ForgeCatalogTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void DestroyDefinitions()
        {
            foreach (Object created in _created)
                Object.DestroyImmediate(created);
            _created.Clear();
        }

        [Test]
        public void ValidDefinitionsBecomeForgeOptionsInCatalogOrder()
        {
            RelicDefinition wind = Relic("Relic_SecondWind", "relic_second_wind", 0.25f);
            WeaponDefinition sword = Weapon("Weapon_Sword", "weapon_sword");
            WeaponDefinition staff = Weapon("Weapon_Staff", "weapon_staff");

            bool created = ForgeCatalog.TryCreate(new[] { wind }, new[] { sword, staff }, out RelicOption[] relics,
                out WeaponOption[] weapons, out string error);

            Assert.That(created, Is.True, error);
            Assert.That(error, Is.Null);
            Assert.That(relics[0].Id, Is.EqualTo("relic_second_wind"));
            Assert.That(weapons[0].Id, Is.EqualTo("weapon_sword"));
            Assert.That(weapons[1].Id, Is.EqualTo("weapon_staff"));
            Assert.That(weapons[1].Description, Is.EqualTo(string.Format("{0:0} damage every {1:0.0#}s", 10f, 0.8f)));
            Assert.That(weapons[1].Price, Is.EqualTo(120));
        }

        [Test]
        public void DuplicateIdsAreReportedByAssetName()
        {
            WeaponDefinition staff = Weapon("Weapon_Staff", "weapon_staff");
            WeaponDefinition copy = Weapon("Weapon_StaffCopy", "weapon_staff");
            Assert.That(ForgeCatalog.TryCreate(new RelicDefinition[0], new[] { staff, copy }, out _, out WeaponOption[] weapons,
                out string error), Is.False);
            Assert.That(error, Does.Contain("Weapon_StaffCopy").And.Contain("weapon_staff"));
            Assert.That(weapons, Is.Null, "Nothing is handed out from a broken catalog.");

            RelicDefinition wind = Relic("Relic_SecondWind", "relic_second_wind", 0.25f);
            RelicDefinition windCopy = Relic("Relic_WindCopy", "relic_second_wind", 0.25f);
            Assert.That(ForgeCatalog.TryCreate(new[] { wind, windCopy }, new[] { staff }, out _, out _, out error), Is.False);
            Assert.That(error, Does.Contain("Relic_WindCopy"));
        }

        [Test]
        public void BrokenFormatsAmountsAndCombatDataAreReportedBeforeARunStarts()
        {
            WeaponDefinition sword = Weapon("Weapon_Sword", "weapon_sword");
            WeaponDefinition typo = Weapon("Weapon_Typo", "weapon_typo", "{5} damage");
            WeaponDefinition badCrit = Weapon("Weapon_BadCrit", "weapon_bad_crit", critEvery: 3, critMultiplier: 1f);
            RelicDefinition overheal = Relic("Relic_Overheal", "relic_overheal", 1.5f);

            Assert.That(ForgeCatalog.TryCreate(new RelicDefinition[0], new[] { sword, typo }, out _, out _, out string error), Is.False);
            Assert.That(error, Does.StartWith("Weapon_Typo"));
            Assert.That(ForgeCatalog.TryCreate(new RelicDefinition[0], new[] { sword, badCrit }, out _, out _, out error), Is.False,
                "A weapon nobody carries yet is still checked.");
            Assert.That(error, Does.StartWith("Weapon_BadCrit"));
            Assert.That(ForgeCatalog.TryCreate(new[] { overheal }, new[] { sword }, out _, out _, out error), Is.False);
            Assert.That(error, Does.StartWith("Relic_Overheal"));
            Assert.That(ForgeCatalog.TryCreate(new RelicDefinition[0], new WeaponDefinition[] { sword, null }, out _, out _, out error), Is.False);
            Assert.That(error, Does.Contain("slot 1"));
        }

        private WeaponDefinition Weapon(string name, string id, string format = "{0:0} damage every {1:0.0#}s", int critEvery = 0,
            float critMultiplier = 0f)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponDefinition>();
            weapon.name = name;
            _created.Add(weapon);
            var data = new SerializedObject(weapon);
            data.FindProperty("_id").stringValue = id;
            data.FindProperty("_displayName").stringValue = name;
            data.FindProperty("_damage").floatValue = 10f;
            data.FindProperty("_interval").floatValue = 0.8f;
            data.FindProperty("_range").floatValue = 3f;
            data.FindProperty("_critEvery").intValue = critEvery;
            data.FindProperty("_critMultiplier").floatValue = critMultiplier;
            data.FindProperty("_forgeDescriptionFormat").stringValue = format;
            data.FindProperty("_forgePrice").intValue = 120;
            data.ApplyModifiedPropertiesWithoutUndo();
            return weapon;
        }

        // A Second Wind relic; amounts above 1 are invalid.
        private RelicDefinition Relic(string name, string id, float amount)
        {
            var relic = ScriptableObject.CreateInstance<RelicDefinition>();
            relic.name = name;
            _created.Add(relic);
            var data = new SerializedObject(relic);
            data.FindProperty("_id").stringValue = id;
            data.FindProperty("_displayName").stringValue = name;
            data.FindProperty("_descriptionFormat").stringValue = "Heal {0:0}% at {1:0}%";
            data.FindProperty("_effect").enumValueIndex = (int)RelicEffect.SecondWind;
            data.FindProperty("_amount").floatValue = amount;
            data.FindProperty("_threshold").floatValue = 0.25f;
            data.FindProperty("_price").intValue = 80;
            data.ApplyModifiedPropertiesWithoutUndo();
            return relic;
        }
    }
}
