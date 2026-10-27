using System.Collections;
using System.IO;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The run's growth on the hero (docs/43 item 1): a level bursts at his feet on scaled time, a card taken rings him in
    // its tier's colour on unscaled time, an evolution doubles the ring and flashes him white, a weapon picked up pops
    // embers. Presentation only; RunFeedbackView reads the run and the upgrade service.
    public sealed class RunFeedbackFlowTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Run feedback cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);TestProfile.End();
        }

        private static IEnumerator Load(string region)
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,region);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled=false;
        }

        [UnityTest, Timeout(90000)] public IEnumerator ALevelBurstsAndATakenCardFlashesItsTier()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var feedback=Object.FindFirstObjectByType<RunFeedbackView>();
            Assert.That(feedback,Is.Not.Null,"CombatSetup adds the run's feedback view.");
            Assert.That(feedback.ShownLevelUps,Is.EqualTo(0));
            RunState run=setup.Run;
            run.AddExperience(run.ExperienceForNextLevel-run.Experience);
            Assert.That(feedback.ShownLevelUps,Is.EqualTo(1),"A level gained bursts at the hero's feet.");
            Assert.That(feedback.IsLevelUpShowing,Is.True);
            // A second pick waiting keeps the run held after the first, so the card's flash can only end on unscaled time.
            run.GrantBonusUpgrade();
            yield return null;
            UpgradeOffer offer=setup.Upgrades.CurrentOffer;
            Assert.That(offer,Is.Not.Null,"The level opens an offer.");
            Assert.That(setup.Pause.IsFrozen,Is.True);
            UpgradeRarity rarity=offer.RarityAt(0);
            Assert.That(setup.Choices.TrySelect(setup.Choices.Current,0),Is.True);
            Assert.That(feedback.ShownCardFlashes,Is.EqualTo(1));
            Assert.That(feedback.LastFlashColour,Is.EqualTo(RunChoiceView.RarityColour(rarity)),"The flash wears the taken card's tier.");
            Assert.That(feedback.LastFlashWasGold,Is.EqualTo(rarity==UpgradeRarity.Gold));
            Assert.That(feedback.IsCardFlashShowing,Is.True);
            Assert.That(feedback.ShownEvolutions,Is.EqualTo(0),"A first card is never an evolution.");
            Assert.That(feedback.IsHeroFlashing,Is.False,"Only an evolution lights the hero.");
            Assert.That(setup.Pause.IsFrozen,Is.True,"The bonus pick holds the run.");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(feedback.IsCardFlashShowing,Is.False,"The card's flash plays out on unscaled time, behind the next offer.");
            Assert.That(feedback.IsLevelUpShowing,Is.True,"The level's burst runs on scaled time, so it waits for the offers to close.");
            for(int i=0;i<4&&setup.Choices.IsOpen;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
            yield return new WaitForSeconds(1.2f);
            // A drop gathered meanwhile may have levelled the hero again and held the run on a new burst.
            if(feedback.ShownLevelUps==1&&!setup.Choices.IsOpen)
                Assert.That(feedback.IsLevelUpShowing,Is.False,"Once the run moves again the burst plays out.");
            for(int i=0;i<4&&setup.Choices.IsOpen;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)] public IEnumerator AnEvolutionDoublesTheRingAndLightsTheHero()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var upgrades=setup.Upgrades;
            var feedback=Object.FindFirstObjectByType<RunFeedbackView>();
            var look=Object.FindFirstObjectByType<HeroLookView>();
            Assert.That(upgrades.IsCategoryMode,Is.True,"The foundry offers by category.");
            WeaponCard target=EvolvingWeapon(upgrades);
            Assert.That(target,Is.Not.Null,"The catalogue holds a weapon that evolves with one of its tomes.");
            int acquired=0;upgrades.WeaponAcquired+=(card,runtime)=>acquired++;
            bool evolved=false;Color evolutionColour=Color.clear;
            upgrades.Selected+=(option,slot)=>
            {
                if(!option.IsEvolution)return;
                evolved=true;evolutionColour=RunChoiceView.RarityColour(upgrades.LastSelected.RarityAt(slot));
            };
            Color bodyColour=look.Body.color;
            // Bonus offers, each answered the way a run chasing the weapon's evolution would: the weapon, its lines and its
            // tome first, never another tome while a better card is there, so the one it needs always has a slot.
            for(int i=0;i<300&&!evolved;i++)
            {
                if(upgrades.CurrentOffer==null)setup.Run.GrantBonusUpgrade();
                yield return null;
                UpgradeOffer offer=upgrades.CurrentOffer;
                if(offer==null)continue;
                Assert.That(setup.Choices.TrySelect(setup.Choices.Current,Preferred(offer,target)),Is.True);
            }
            Assert.That(evolved,Is.True,"An evolution came within three hundred offers.");
            Assert.That(feedback.ShownEvolutions,Is.EqualTo(1),"The evolution has its moment.");
            Assert.That(feedback.LastFlashColour,Is.EqualTo(evolutionColour),"Its first ring wears the card's tier.");
            Assert.That(feedback.ActiveEvolutionRings,Is.GreaterThanOrEqualTo(2),"The evolution doubles its ring.");
            Assert.That(feedback.IsHeroFlashing,Is.True,"The hero is lit white.");
            Assert.That(feedback.ShownWeaponPops,Is.EqualTo(acquired),"Every weapon picked up on the way popped its embers.");
            // Another pick held keeps the run frozen: the flash still ends, on unscaled time, and leaves the body as it was.
            setup.Run.GrantBonusUpgrade();
            Assert.That(setup.Pause.IsFrozen,Is.True);
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(feedback.IsHeroFlashing,Is.False,"The white fades behind the next offer.");
            Assert.That(feedback.ActiveEvolutionRings,Is.EqualTo(0));
            Assert.That(look.Body.color,Is.EqualTo(bodyColour),"The flash is an overlay; the body's own colour is untouched.");
            for(int i=0;i<4&&setup.Choices.IsOpen;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
            LogAssert.NoUnexpectedReceived();
        }

        // Ilya's rendered art hides the body the flash usually rides on, so its own animator lights it (docs/45 section 8).
        [UnityTest, Timeout(120000)] public IEnumerator AnEvolutionLightsARenderedHeroToo()
        {
            File.WriteAllText(DevelopmentStart.HeroPath,"hero_cinder_arcanist");
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var upgrades=setup.Upgrades;
            var feedback=Object.FindFirstObjectByType<RunFeedbackView>();
            var drawn=Object.FindFirstObjectByType<RenderedCharacterAnimator>();
            Assert.That(drawn,Is.Not.Null,"Ilya is drawn from its rendered art.");
            Assert.That(drawn.FlashRenderer,Is.Not.Null,"The flash shader is available.");
            WeaponCard target=EvolvingWeapon(upgrades);
            Assert.That(target,Is.Not.Null);
            bool evolved=false;
            upgrades.Selected+=(option,slot)=>{if(option.IsEvolution)evolved=true;};
            for(int i=0;i<300&&!evolved;i++)
            {
                if(upgrades.CurrentOffer==null)setup.Run.GrantBonusUpgrade();
                yield return null;
                UpgradeOffer offer=upgrades.CurrentOffer;
                if(offer==null)continue;
                Assert.That(setup.Choices.TrySelect(setup.Choices.Current,Preferred(offer,target)),Is.True);
            }
            Assert.That(evolved,Is.True,"An evolution came within three hundred offers.");
            setup.Run.GrantBonusUpgrade();
            yield return null;
            Assert.That(feedback.IsHeroFlashing,Is.True,"The hero is lit white.");
            Assert.That(drawn.FlashRenderer.enabled,Is.True,"The rendered hero's own silhouette carries the white.");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.That(feedback.IsHeroFlashing,Is.False,"The white fades behind the next offer.");
            Assert.That(drawn.FlashRenderer.enabled,Is.False);
            Assert.That(drawn.OverlayFlash,Is.EqualTo(0f));
            for(int i=0;i<4&&setup.Choices.IsOpen;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
            LogAssert.NoUnexpectedReceived();
        }

        // The weapon the hero carries if it can evolve, else the first in the catalogue that can and whose tome is there.
        private static WeaponCard EvolvingWeapon(UpgradeService upgrades)
        {
            WeaponCard found=null;
            string carried=upgrades.Loadout.At(0).WeaponId;
            for(int i=0;i<upgrades.WeaponCards.Count;i++)
            {
                WeaponCard card=upgrades.WeaponCards[i];
                if(card.Evolution==null||!HasTome(upgrades,card.EvolvesWithTomeId))continue;
                if(card.Id==carried)return card;
                if(found==null)found=card;
            }
            return found;
        }

        private static bool HasTome(UpgradeService upgrades,string id)
        {
            for(int i=0;i<upgrades.Tomes.Count;i++)if(upgrades.Tomes[i].Id==id)return true;
            return false;
        }

        private static int Preferred(UpgradeOffer offer,WeaponCard target)
        {
            int best=0,bestScore=-1;
            for(int c=0;c<offer.Choices.Count;c++)
            {
                int score=Score(offer.Choices[c],target);
                if(score>bestScore){best=c;bestScore=score;}
            }
            return best;
        }

        // Any evolution; the weapon itself while it is not carried (its card has the weapon's id); one of its lines; its
        // tome, new or a level; a general card; any other level; another weapon; another tome last.
        private static int Score(UpgradeOption option,WeaponCard target)
        {
            if(option.IsEvolution)return 7;
            if(option.Id==target.Id)return 6;
            for(int l=0;l<target.Lines.Count;l++)if(target.Lines[l].Id==option.Id)return 5;
            if(option.Id==target.EvolvesWithTomeId)return 4;
            switch(option.Category)
            {
                case OfferCategory.General:return 3;
                case OfferCategory.WeaponUpgrade:
                case OfferCategory.TomeUpgrade:return 2;
                case OfferCategory.NewWeapon:return 1;
                default:return 0;
            }
        }
    }
}
