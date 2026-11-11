using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The upgrade cards' painted icons in the Gameplay scene (UpgradeIconSet on RunChoiceView and RunHud): on the foundry
    // every category card - a new weapon, a weapon's line or evolution, a tome, a general card - shows its own icon by its
    // id, and the loadout row shows each weapon's and tome's icon beside its level; the set wins over the scene's own
    // table, which answers only what the set lacks, and the hall's legacy cards show the set's squareless repaint of their
    // icons (2026-11-12, so their tier ground shows); without the set the cards and the row are text, as they were.
    public sealed class UpgradeIconFlowTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Upgrade icons cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);TestProfile.End();
        }

        private static IEnumerator Load(string region)
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,region);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled=false;
        }

        private static Image[] CardIcons(RunChoiceView view)=>(Image[])typeof(RunChoiceView).GetField("_cardIcons",Private).GetValue(view);
        private static FieldInfo SetField=>typeof(RunChoiceView).GetField("_iconSet",Private);
        private static FieldInfo TableField=>typeof(RunChoiceView).GetField("_upgradeIcons",Private);

        // A pick that fills the row: a new weapon first, then a new tome, then an evolution, then a weapon's level.
        private static int Pick(UpgradeOffer offer)
        {
            foreach(Func<UpgradeOption,bool> wanted in new Func<UpgradeOption,bool>[]{
                        o=>o.Category==OfferCategory.NewWeapon,o=>o.Category==OfferCategory.NewTome,o=>o.IsEvolution,
                        o=>o.Category==OfferCategory.WeaponUpgrade})
                for(int c=0;c<offer.Choices.Count;c++)
                    if(wanted(offer.Choices[c]))return c;
            return 0;
        }

        private static string EvolutionOf(UpgradeService upgrades,string weaponId)
        {
            foreach(WeaponCard card in upgrades.WeaponCards)
                if(card.Id==weaponId&&card.Evolution!=null)return card.Evolution.Id;
            return null;
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
        }

        // A filled badge with its icon: the icon in place of the name, and the level beside it, inside the badge, clear of
        // the icon, and fitting its column at its largest size.
        private static void AssertBadge(Transform row,string badgeName,string level)
        {
            Canvas.ForceUpdateCanvases();
            Transform badge=row.Find(badgeName);
            Image icon=badge.Find("Icon").GetComponent<Image>();
            Text name=badge.Find("Name").GetComponent<Text>(),label=badge.Find("Level").GetComponent<Text>();
            Assert.That(icon.enabled,Is.True,badgeName+" shows its icon.");
            Assert.That(icon.raycastTarget,Is.False,badgeName+": a tap goes through it.");
            Assert.That(name.enabled,Is.False,badgeName+": the icon stands for the name.");
            Assert.That(label.enabled,Is.True,badgeName+" still shows its level.");
            Assert.That(label.text,Is.EqualTo(level),badgeName+"'s level.");
            Rect plate=ScreenRect((RectTransform)badge),levelRect=ScreenRect(label.rectTransform),iconRect=ScreenRect(icon.rectTransform);
            Assert.That(plate.Contains(levelRect.min)&&plate.Contains(levelRect.max),Is.True,badgeName+": the level inside the badge.");
            Assert.That(plate.Contains(iconRect.min)&&plate.Contains(iconRect.max),Is.True,badgeName+": the icon inside the badge.");
            Assert.That(levelRect.xMin,Is.GreaterThanOrEqualTo(iconRect.xMax-.5f),badgeName+": the level beside the icon, not over it.");
            var settings=label.GetGenerationSettings(label.rectTransform.rect.size);
            settings.resizeTextForBestFit=false;settings.fontSize=label.resizeTextMaxSize;
            float width=label.cachedTextGeneratorForLayout.GetPreferredWidth(label.text,settings)/label.pixelsPerUnit;
            float height=label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text,settings)/label.pixelsPerUnit;
            Assert.That(width,Is.LessThanOrEqualTo(label.rectTransform.rect.width),badgeName+": the level fits its column at its full size.");
            Assert.That(height,Is.LessThanOrEqualTo(label.rectTransform.rect.height+1f),badgeName+": and its height.");
        }

        [UnityTest, Timeout(120000)] public IEnumerator EachFoundryCardShowsItsOwnIconAndTheRowItsWeaponsAndTomes()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var upgrades=setup.Upgrades;
            var choices=Object.FindFirstObjectByType<RunChoiceView>();var hud=Object.FindFirstObjectByType<RunHud>();
            var set=(UpgradeIconSet)SetField.GetValue(choices);
            Assert.That(set,Is.Not.Null,"The choice panel holds the icon set.");
            Assert.That(upgrades.IsCategoryMode,Is.True,"The foundry offers by category.");
            LoadoutBadgesView badges=hud.LoadoutBadges;
            // A frame for HudArtView to dress the row in its sockets: the icons stay.
            yield return null;
            Assert.That(badges.WeaponBadgeIcon(0),Is.SameAs(set.SlotIconFor(upgrades.Loadout.At(0).WeaponId)).And.Not.Null,
                "The forge's weapon shows its icon from the start.");
            Assert.That(badges.WeaponBadgeIcon(1),Is.Null,"A free socket shows nothing.");
            AssertBadge(badges.Row,"Weapon Badge 1","1");

            var kinds=new HashSet<OfferCategory>();int cards=0;
            for(int i=0;i<40;i++)
            {
                if(upgrades.CurrentOffer==null)setup.Run.GrantBonusUpgrade();
                yield return null;
                UpgradeOffer offer=upgrades.CurrentOffer;
                if(offer==null)continue;
                Image[] icons=CardIcons(choices);
                Assert.That(icons.Length,Is.GreaterThanOrEqualTo(offer.Choices.Count),"Every card has its icon.");
                for(int c=0;c<offer.Choices.Count;c++)
                {
                    UpgradeOption option=offer.Choices[c];
                    Sprite own=set.CardIconFor(option.Id);
                    Assert.That(own,Is.Not.Null,option.Id+" has a painted icon.");
                    Assert.That(icons[c].sprite,Is.SameAs(own),"Card "+c+" shows "+option.Id+"'s own icon ("+option.Category+").");
                    Assert.That(icons[c].enabled,Is.True,"Card "+c+"'s icon shows.");
                    kinds.Add(option.Category);
                    cards++;
                }
                Assert.That(setup.Choices.TrySelect(setup.Choices.Current,Pick(offer)),Is.True);
            }
            Assert.That(cards,Is.GreaterThan(30));
            foreach(OfferCategory kind in new[]{OfferCategory.NewWeapon,OfferCategory.WeaponUpgrade,OfferCategory.NewTome})
                Assert.That(kinds,Has.Member(kind),"New weapons, their lines and tomes were all shown: "+kind+".");

            // The row: each weapon its own socket icon (its evolution's once taken), each tome taken its own.
            Assert.That(upgrades.Loadout.Count,Is.GreaterThan(1),"A weapon was taken on the way.");
            for(int s=0;s<upgrades.Loadout.Count;s++)
            {
                string id=upgrades.Loadout.At(s).WeaponId;
                string shown=upgrades.HasEvolved(id)?EvolutionOf(upgrades,id):id;
                Assert.That(badges.WeaponBadgeIcon(s),Is.SameAs(set.SlotIconFor(shown)).And.Not.Null,"Weapon badge "+s+" shows "+shown+".");
                AssertBadge(badges.Row,"Weapon Badge "+(s+1),upgrades.Loadout.At(s).Level.ToString());
            }
            Assert.That(upgrades.TomesHeld,Is.GreaterThan(0),"A tome was taken on the way.");
            var held=new List<Sprite>();
            foreach(UpgradeOption tome in upgrades.Tomes)
                if(upgrades.TomeLevel(tome.Id)>0)held.Add(set.SlotIconFor(tome.Id));
            var shownTomes=new List<Sprite>();
            for(int t=0;t<badges.ShownTomeBadges;t++)
            {
                shownTomes.Add(badges.TomeBadgeIcon(t));
                AssertBadge(badges.Row,"Tome Badge "+(t+1),badges.TomeBadgeText(t).Substring(badges.TomeBadgeText(t).LastIndexOf(' ')+1));
            }
            Assert.That(shownTomes,Is.EquivalentTo(held),"Every tome held shows its own icon.");
            for(int i=0;i<4&&setup.Choices.IsOpen;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheSetWinsTheTableAnswersWhatItLacksAndWithoutBothTheCardsAndTheRowAreText()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var upgrades=setup.Upgrades;
            var choices=Object.FindFirstObjectByType<RunChoiceView>();var hud=Object.FindFirstObjectByType<RunHud>();
            var set=(UpgradeIconSet)SetField.GetValue(choices);
            Assert.That(set,Is.Not.Null);
            object authored=TableField.GetValue(choices);

            // A table naming every id this run can offer, each with a sprite of its own: every card still shows the set's
            // (2026-11-12: the set's icons carry no square, so the card's tier ground shows round them).
            var ids=new List<string>();
            foreach(UpgradeOption option in upgrades.Pool)ids.Add(option.Id);
            foreach(WeaponCard card in upgrades.WeaponCards)
            {
                ids.Add(card.Id);
                foreach(UpgradeOption line in card.Lines)ids.Add(line.Id);
                if(card.Evolution!=null)ids.Add(card.Evolution.Id);
            }
            foreach(UpgradeOption tome in upgrades.Tomes)ids.Add(tome.Id);
            var texture=new Texture2D(4,4);
            Sprite marker=Sprite.Create(texture,new Rect(0,0,4,4),new Vector2(.5f,.5f));
            Type entryType=typeof(RunChoiceView).GetNestedType("UpgradeIcon",BindingFlags.NonPublic);
            Array table=Array.CreateInstance(entryType,ids.Count);
            var definitions=new List<UpgradeDefinition>();
            for(int i=0;i<ids.Count;i++)
            {
                var definition=ScriptableObject.CreateInstance<UpgradeDefinition>();
                typeof(UpgradeDefinition).GetField("_id",Private).SetValue(definition,ids[i]);
                definitions.Add(definition);
                object entry=Activator.CreateInstance(entryType);
                entryType.GetField("Definition").SetValue(entry,definition);
                entryType.GetField("Sprite").SetValue(entry,marker);
                table.SetValue(entry,i);
            }
            TableField.SetValue(choices,table);
            setup.Run.GrantBonusUpgrade();
            yield return null;
            UpgradeOffer offer=upgrades.CurrentOffer;
            Assert.That(offer,Is.Not.Null);
            Image[] icons=CardIcons(choices);
            for(int c=0;c<offer.Choices.Count;c++)
            {
                Assert.That(set.CardIconFor(offer.Choices[c].Id),Is.Not.Null,"The set has one too.");
                Assert.That(icons[c].sprite,Is.SameAs(set.CardIconFor(offer.Choices[c].Id)),
                    "Card "+c+" ("+offer.Choices[c].Id+"): the set wins over the scene's table.");
            }

            // The same table without the set: the table answers what the set does not.
            SetField.SetValue(choices,null);
            Assert.That(setup.Choices.TrySelect(setup.Choices.Current,0),Is.True);
            if(upgrades.CurrentOffer==null)setup.Run.GrantBonusUpgrade();
            yield return null;
            offer=upgrades.CurrentOffer;
            Assert.That(offer,Is.Not.Null);
            for(int c=0;c<offer.Choices.Count;c++)
                Assert.That(icons[c].sprite,Is.SameAs(marker),"Card "+c+" ("+offer.Choices[c].Id+"): the table answers without the set.");

            // The scene's table and the set again: the set answers every card.
            SetField.SetValue(choices,set);
            TableField.SetValue(choices,authored);
            Assert.That(setup.Choices.TrySelect(setup.Choices.Current,0),Is.True);
            if(upgrades.CurrentOffer==null)setup.Run.GrantBonusUpgrade();
            yield return null;
            offer=upgrades.CurrentOffer;
            Assert.That(offer,Is.Not.Null);
            for(int c=0;c<offer.Choices.Count;c++)
                Assert.That(icons[c].sprite,Is.SameAs(set.CardIconFor(offer.Choices[c].Id)),"Card "+c+": the set's icon for its id.");

            // Without the set: every category card is text only, as the foundry's cards were.
            SetField.SetValue(choices,null);
            Assert.That(setup.Choices.TrySelect(setup.Choices.Current,0),Is.True);
            if(upgrades.CurrentOffer==null)setup.Run.GrantBonusUpgrade();
            yield return null;
            offer=upgrades.CurrentOffer;
            Assert.That(offer,Is.Not.Null);
            for(int c=0;c<offer.Choices.Count;c++)
            {
                Assert.That(icons[c].enabled,Is.False,"Card "+c+" ("+offer.Choices[c].Id+") shows no icon without the set.");
                Assert.That(icons[c].sprite,Is.Null);
            }

            // The row without the set: names over levels, laid out as the row was built; with it again, the icons.
            LoadoutBadgesView badges=hud.LoadoutBadges;
            badges.UseIcons(null);
            Transform badge=badges.Row.Find("Weapon Badge 1");
            Assert.That(badges.WeaponBadgeIcon(0),Is.Null);
            Assert.That(badge.Find("Icon").GetComponent<Image>().enabled,Is.False);
            Assert.That(badge.Find("Name").GetComponent<Text>().enabled,Is.True,"The name shows again.");
            Assert.That(badges.WeaponBadgeText(0),Does.EndWith(" "+upgrades.Loadout.At(0).Level));
            var level=(RectTransform)badge.Find("Level");
            Assert.That(level.anchorMin,Is.EqualTo(Vector2.zero));
            Assert.That(level.anchorMax,Is.EqualTo(new Vector2(1f,0f)),"The level back under the name.");
            Assert.That(level.sizeDelta,Is.EqualTo(new Vector2(-8f,36f)));
            badges.UseIcons(set);
            Assert.That(badges.WeaponBadgeIcon(0),Is.SameAs(set.SlotIconFor(upgrades.Loadout.At(0).WeaponId)));
            AssertBadge(badges.Row,"Weapon Badge 1",upgrades.Loadout.At(0).Level.ToString());

            for(int i=0;i<4&&setup.Choices.IsOpen;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
            foreach(UpgradeDefinition definition in definitions)Object.Destroy(definition);
            Object.Destroy(marker);Object.Destroy(texture);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheHallsLegacyCardsShowTheSetsSquarelessIconsLikeEveryOtherCard()
        {
            yield return Load("hall");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var upgrades=setup.Upgrades;
            var choices=Object.FindFirstObjectByType<RunChoiceView>();
            var set=(UpgradeIconSet)SetField.GetValue(choices);
            Assert.That(upgrades.IsCategoryMode,Is.False);
            Assert.That(Object.FindFirstObjectByType<RunHud>().LoadoutBadges,Is.Null,"The hall has no loadout row.");
            // The scene's own table: Tempered Edge and Quickened Grip with their old painted icons, on their navy squares.
            // The set paints both again without the square and wins, so their tier ground shows round them.
            var own=new Dictionary<string,Sprite>();
            Type entryType=typeof(RunChoiceView).GetNestedType("UpgradeIcon",BindingFlags.NonPublic);
            foreach(object entry in (Array)TableField.GetValue(choices))
                own.Add(((UpgradeDefinition)entryType.GetField("Definition").GetValue(entry)).Id,(Sprite)entryType.GetField("Sprite").GetValue(entry));
            Assert.That(own.Keys,Is.EquivalentTo(new[]{"upgrade_damage","upgrade_attack_speed"}));
            bool tableShown=false,setShown=false;
            for(int i=0;i<12;i++)
            {
                if(upgrades.CurrentOffer==null)setup.Run.GrantBonusUpgrade();
                yield return null;
                UpgradeOffer offer=upgrades.CurrentOffer;
                if(offer==null)continue;
                Image[] icons=CardIcons(choices);
                for(int c=0;c<offer.Choices.Count;c++)
                {
                    string id=offer.Choices[c].Id;
                    if(own.TryGetValue(id,out Sprite painted))
                    {
                        Assert.That(painted.name,Does.StartWith("UI_Icon_"));
                        Assert.That(icons[c].sprite,Is.SameAs(set.CardIconFor(id)).And.Not.SameAs(painted),
                            id+" shows the set's icon, not the table's square one.");
                        Assert.That(icons[c].sprite.name,Is.EqualTo("UI_UpgradeIcons_"+id),id+": its own glyph, repainted.");
                        tableShown=true;
                    }
                    else
                    {
                        Assert.That(icons[c].sprite,Is.SameAs(set.CardIconFor(id)).And.Not.Null,id+" shows the set's icon.");
                        setShown=true;
                    }
                    Assert.That(icons[c].enabled,Is.True);
                }
                Assert.That(setup.Choices.TrySelect(setup.Choices.Current,0),Is.True);
            }
            Assert.That(tableShown&&setShown,Is.True,"Both kinds of card came up.");
            for(int i=0;i<4&&setup.Choices.IsOpen;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
