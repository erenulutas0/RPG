using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The foundry builds Megabonk's way (docs/42 PR 7): offers come by category, the forge's weapon is the first of up to
    // three, and a weapon taken from an offer fires beside it. The hall arena keeps the legacy cards.
    public sealed class WeaponLoadoutFlowTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Loadout cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);TestProfile.End();
        }

        private static IEnumerator Load(string region)
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,region);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled=false;
        }

        [UnityTest, Timeout(90000)] public IEnumerator ATakenWeaponFiresBesideTheForgesOne()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var upgrades=setup.Upgrades;
            var attack=Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<AttackController>();
            Assert.That(upgrades.IsCategoryMode,Is.True,"The foundry offers by category.");
            Assert.That(upgrades.Loadout.Primary,Is.SameAs(setup.Weapon),"The forge's weapon is the first.");
            Assert.That(attack.WeaponCount,Is.EqualTo(1));
            bool acquired=false;upgrades.WeaponAcquired+=(card,runtime)=>acquired=true;
            // Bonus offers until one shows a new weapon, which is taken; every other offer takes its first card.
            for(int i=0;i<40&&!acquired;i++)
            {
                if(upgrades.CurrentOffer==null)setup.Run.GrantBonusUpgrade();
                yield return null;
                UpgradeOffer offer=upgrades.CurrentOffer;
                if(offer==null)continue;
                int slot=0;
                for(int c=0;c<offer.Choices.Count;c++)if(offer.Choices[c].Category==OfferCategory.NewWeapon){slot=c;break;}
                Assert.That(setup.Choices.TrySelect(setup.Choices.Current,slot),Is.True);
            }
            Assert.That(acquired,Is.True,"A new weapon came within forty offers.");
            Assert.That(upgrades.Loadout.Count,Is.EqualTo(2));
            Assert.That(attack.WeaponCount,Is.EqualTo(2),"It fires beside the first.");
            Assert.That(attack.Weapon,Is.SameAs(setup.Weapon),"The body still swings with the forge's weapon.");
            for(int i=0;i<4&&setup.Choices.IsOpen;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheHallKeepsTheLegacyCards()
        {
            yield return Load("hall");
            var setup=Object.FindFirstObjectByType<CombatSetup>();
            Assert.That(setup.Upgrades.IsCategoryMode,Is.False);
            Assert.That(Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<AttackController>().WeaponCount,Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
