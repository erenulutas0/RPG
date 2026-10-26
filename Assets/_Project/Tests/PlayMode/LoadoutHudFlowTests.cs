using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The foundry's build on the screen (docs/42 section 4): a row of weapon and tome badges on the top panel where the
    // legacy row was, and a kind line on each category card. The hall keeps the legacy row and the legacy cards.
    public sealed class LoadoutHudFlowTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Loadout HUD cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);TestProfile.End();
        }

        private static IEnumerator Load(string region)
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,region);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled=false;
        }

        private static PrototypeTextDefinition Strings()=>Resources.FindObjectsOfTypeAll<PrototypeTextDefinition>()[0];

        // The kind line each card should wear, from the same text asset and service state the view reads.
        private static string ExpectedKind(UpgradeService upgrades,UpgradeOption option,PrototypeTextDefinition text)
        {
            if(option.IsEvolution)return text.CardEvolution;
            switch(option.Category)
            {
                case OfferCategory.NewWeapon:return text.CardNewWeapon;
                case OfferCategory.NewTome:return text.CardNewTome;
                case OfferCategory.WeaponUpgrade:
                case OfferCategory.TomeUpgrade:
                    int level=upgrades.StacksOf(option);return string.Format(text.CardLevelUpFormat,level,level+1);
                default:return string.Empty;
            }
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
        }

        private static void AssertClear(RectTransform row,Transform parent,string name)
        {
            Transform other=parent.Find(name);
            Assert.That(other,Is.Not.Null,name+" is on the safe area.");
            Assert.That(ScreenRect(row).Overlaps(ScreenRect((RectTransform)other)),Is.False,"The row keeps clear of "+name+".");
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheFoundryShowsTheWeaponsAndTomesItHolds()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var upgrades=setup.Upgrades;
            var hud=Object.FindFirstObjectByType<RunHud>();var choices=Object.FindFirstObjectByType<RunChoiceView>();
            PrototypeTextDefinition text=Strings();
            Assert.That(upgrades.IsCategoryMode,Is.True,"The foundry offers by category.");
            LoadoutBadgesView badges=hud.LoadoutBadges;
            Assert.That(badges,Is.Not.Null,"The HUD builds the loadout row in category mode.");
            Assert.That(badges.ShownWeaponBadges,Is.EqualTo(1),"The forge's weapon is the first badge.");
            Assert.That(badges.ShownTomeBadges,Is.EqualTo(0));
            Assert.That(badges.WeaponBadgeText(0),Does.EndWith(" 1"));
            Assert.That(GameObject.Find("Weapon Name"),Is.Null,"The legacy weapon name gives the row its band.");
            Assert.That(GameObject.Find("Damage Badge"),Is.Null);
            Assert.That(GameObject.Find("AttackSpeed Badge"),Is.Null);
            Canvas.ForceUpdateCanvases();
            Transform safeArea=badges.Row.parent;
            AssertClear(badges.Row,safeArea,"Danger Readout");
            AssertClear(badges.Row,safeArea,"Equipped Relic Badge");
            AssertClear(badges.Row,safeArea,"Floor Label");
            AssertClear(badges.Row,safeArea,"Gold Count");
            AssertClear(badges.Row,safeArea,"Pause Button");
            for(int p=1;p<=6;p++)AssertClear(badges.Row,safeArea,"Room Pip "+p);

            // Bonus offers until one shows a new weapon, which is taken; every other offer takes its first card. Each shown
            // card's kind line is checked on the way.
            bool acquired=false;WeaponCard taken=null;upgrades.WeaponAcquired+=(card,runtime)=>{acquired=true;taken=card;};
            int kindsChecked=0;
            for(int i=0;i<40&&!acquired;i++)
            {
                if(upgrades.CurrentOffer==null)setup.Run.GrantBonusUpgrade();
                yield return null;
                UpgradeOffer offer=upgrades.CurrentOffer;
                if(offer==null)continue;
                for(int c=0;c<offer.Choices.Count&&c<choices.CardSlots;c++)
                {
                    Assert.That(choices.CardKindText(c),Is.EqualTo(ExpectedKind(upgrades,offer.Choices[c],text)),
                        "Card "+c+" ("+offer.Choices[c].Id+", "+offer.Choices[c].Category+") says what kind of pick it is.");
                    kindsChecked++;
                }
                int slot=0;
                for(int c=0;c<offer.Choices.Count;c++)if(offer.Choices[c].Category==OfferCategory.NewWeapon){slot=c;break;}
                Assert.That(setup.Choices.TrySelect(setup.Choices.Current,slot),Is.True);
            }
            Assert.That(acquired,Is.True,"A new weapon came within forty offers.");
            Assert.That(kindsChecked,Is.GreaterThan(0));
            Assert.That(upgrades.Loadout.Count,Is.EqualTo(2));
            Assert.That(badges.ShownWeaponBadges,Is.EqualTo(2),"The weapon taken joins the row.");
            Assert.That(badges.WeaponBadgeText(1),Does.EndWith(" 1"),"A weapon picked up starts at level 1.");
            Assert.That(badges.WeaponBadgeText(1),Does.StartWith(taken.DisplayName.Substring(0,3)));
            Assert.That(badges.WeaponBadgeText(0),Does.EndWith(" "+upgrades.Loadout.At(0).Level));
            Assert.That(badges.ShownTomeBadges,Is.EqualTo(upgrades.TomesHeld),"Every tome taken on the way is shown.");
            for(int i=0;i<4&&setup.Choices.IsOpen;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheHallShowsNoCategoryBadges()
        {
            yield return Load("hall");
            var setup=Object.FindFirstObjectByType<CombatSetup>();
            var choices=Object.FindFirstObjectByType<RunChoiceView>();
            Assert.That(setup.Upgrades.IsCategoryMode,Is.False);
            Assert.That(Object.FindFirstObjectByType<RunHud>().LoadoutBadges,Is.Null,"The legacy cards keep the legacy row.");
            Assert.That(Object.FindFirstObjectByType<LoadoutBadgesView>(),Is.Null);
            Assert.That(GameObject.Find("Weapon Name"),Is.Not.Null,"The forge weapon's name stays on the top panel.");
            setup.Run.GrantBonusUpgrade();
            yield return null;
            Assert.That(choices.IsOpen,Is.True);
            for(int c=0;c<choices.CardSlots;c++)
                Assert.That(choices.CardKindText(c),Is.Empty,"A legacy card has no kind line.");
            for(int i=0;i<4&&setup.Choices.IsOpen;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
