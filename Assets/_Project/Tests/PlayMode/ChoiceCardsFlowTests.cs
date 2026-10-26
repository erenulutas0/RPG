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
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // The foundry's category offers show three cards (docs/40 9.2, docs/42): the choice panel clones the scene's last
    // card for the third and stacks the three inside the Choice Surface. The hall's offers keep the scene's two cards
    // exactly where it put them.
    public sealed class ChoiceCardsFlowTests
    {
        [UnitySetUp] public IEnumerator Begin(){TestProfile.Begin();Time.timeScale=1;yield return null;}
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Choice cards cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);TestProfile.End();
        }

        private static IEnumerator Load(string region)
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,region);
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled=false;
        }

        // The Choice Panel's canvas is screen-space overlay, so world corners are screen pixels.
        private static Rect ScreenRect(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
        }

        private static bool Inside(Rect inner,Rect outer)=>
            inner.xMin>=outer.xMin-.5f&&inner.xMax<=outer.xMax+.5f&&inner.yMin>=outer.yMin-.5f&&inner.yMax<=outer.yMax+.5f;

        // Every label on the card still fits its rect at its minimum font size, as the result and forge panels are held to.
        private static void AssertTextFits(Button card)
        {
            foreach(Text label in card.GetComponentsInChildren<Text>())
            {
                var settings=label.GetGenerationSettings(new Vector2(label.rectTransform.rect.width,0));
                settings.resizeTextForBestFit=false;settings.fontSize=label.resizeTextMinSize;
                float needed=label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text,settings)/label.pixelsPerUnit;
                Assert.That(needed,Is.LessThanOrEqualTo(label.rectTransform.rect.height+1f),
                    card.name+"/"+label.name+" ('"+label.text+"') truncates even at its minimum font size.");
            }
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheFoundryShowsThreeCardsInsideTheSurface()
        {
            yield return Load("foundry");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var upgrades=setup.Upgrades;
            var choices=Object.FindFirstObjectByType<RunChoiceView>();
            Assert.That(upgrades.IsCategoryMode,Is.True,"The foundry offers by category.");
            Assert.That(choices.CardSlots,Is.EqualTo(2),"The scene authors two cards.");

            // Bonus offers until one shows three cards; any smaller one takes its first card. The offer opens and the panel
            // shows it on the grant itself, so the input delay is checked before a frame passes.
            UpgradeOffer offer=null;
            for(int i=0;i<40&&offer==null;i++)
            {
                if(upgrades.CurrentOffer==null)setup.Run.GrantBonusUpgrade();
                UpgradeOffer drawn=upgrades.CurrentOffer;
                if(drawn!=null&&drawn.Choices.Count==3){offer=drawn;break;}
                if(drawn!=null)Assert.That(setup.Choices.TrySelect(setup.Choices.Current,0),Is.True);
                yield return null;
            }
            Assert.That(offer,Is.Not.Null,"A three-card offer came within forty.");
            Assert.That(choices.IsOpen,Is.True);
            Assert.That(choices.CardSlots,Is.EqualTo(3),"The panel cloned a third card.");
            for(int c=0;c<3;c++)
            {
                Assert.That(choices.CardButton(c).gameObject.activeInHierarchy,Is.True,"Card "+c+" is shown.");
                Assert.That(choices.CardButton(c).interactable,Is.False,"Card "+c+" waits out the input delay.");
            }
            yield return new WaitForSecondsRealtime(.3f);

            Canvas.ForceUpdateCanvases();
            var surface=(RectTransform)choices.CardButton(0).transform.parent;
            Assert.That(surface.name,Is.EqualTo("Choice Surface"));
            Assert.That(choices.CardButton(2).transform.parent,Is.SameAs(surface),"The clone sits on the same surface.");
            Rect bounds=ScreenRect(surface);
            Rect title=ScreenRect((RectTransform)surface.Find("Choice Title"));
            var cards=new Rect[3];ChoicePrompt prompt=setup.Choices.Current;
            for(int c=0;c<3;c++)
            {
                Button card=choices.CardButton(c);
                Assert.That(card.interactable,Is.True,"Card "+c+" takes a tap once the delay is over.");
                cards[c]=ScreenRect((RectTransform)card.transform);
                Assert.That(Inside(cards[c],bounds),Is.True,"Card "+c+" "+cards[c]+" lies inside the surface "+bounds+".");
                Assert.That(cards[c].Overlaps(title),Is.False,"Card "+c+" keeps clear of the title.");
                Assert.That(card.GetComponentInChildren<Text>().text,Is.EqualTo(prompt.Cards[c].Name),"Card "+c+" names its pick.");
                AssertTextFits(card);
            }
            for(int a=0;a<3;a++)
                for(int b=a+1;b<3;b++)
                    Assert.That(cards[a].Overlaps(cards[b]),Is.False,"Cards "+a+" and "+b+" do not overlap.");
            Assert.That(cards[0].yMin,Is.GreaterThan(cards[1].yMax),"The cards stack top to bottom.");
            Assert.That(cards[1].yMin,Is.GreaterThan(cards[2].yMax));
            Assert.That(cards[0].yMin-cards[1].yMax,Is.EqualTo(cards[1].yMin-cards[2].yMax).Within(1f),"The gaps are even.");

            // The third card selects slot 2: the offer's third card is the one applied.
            int pickedSlot=-1;UpgradeOption picked=null;
            upgrades.Selected+=(option,slot)=>{picked=option;pickedSlot=slot;};
            UpgradeOption third=offer.Choices[2];
            choices.CardButton(2).onClick.Invoke();
            Assert.That(pickedSlot,Is.EqualTo(2));
            Assert.That(picked,Is.SameAs(third),"The offer's third card is the one applied.");
            Assert.That(upgrades.LastSelected,Is.SameAs(offer));
            for(int i=0;i<4&&setup.Choices.IsOpen;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheHallKeepsItsTwoCards()
        {
            yield return Load("hall");
            var setup=Object.FindFirstObjectByType<CombatSetup>();var upgrades=setup.Upgrades;
            var choices=Object.FindFirstObjectByType<RunChoiceView>();
            Assert.That(upgrades.IsCategoryMode,Is.False);
            setup.Run.GrantBonusUpgrade();
            yield return null;
            UpgradeOffer offer=upgrades.CurrentOffer;
            Assert.That(offer,Is.Not.Null);
            Assert.That(offer.Choices.Count,Is.EqualTo(2));
            Assert.That(choices.IsOpen,Is.True);
            Assert.That(choices.CardSlots,Is.EqualTo(2),"No card is cloned for two.");
            Assert.That(GameObject.Find("Choice Button 3"),Is.Null);
            yield return new WaitForSecondsRealtime(.3f);
            // Exactly the scene's layout: 896x252 cards centred at y 101 and -191 on the surface.
            float[] centres={101f,-191f};
            for(int c=0;c<2;c++)
            {
                var card=(RectTransform)choices.CardButton(c).transform;
                Assert.That(choices.CardButton(c).gameObject.activeInHierarchy,Is.True);
                Assert.That(choices.CardButton(c).interactable,Is.True);
                Assert.That(card.anchoredPosition,Is.EqualTo(new Vector2(0f,centres[c])),"Card "+c+" sits where the scene put it.");
                Assert.That(card.sizeDelta,Is.EqualTo(new Vector2(896f,252f)),"Card "+c+" keeps the scene's size.");
            }
            int pickedSlot=-1;upgrades.Selected+=(option,slot)=>pickedSlot=slot;
            choices.CardButton(1).onClick.Invoke();
            Assert.That(pickedSlot,Is.EqualTo(1));
            for(int i=0;i<4&&setup.Choices.IsOpen;i++)setup.Choices.TrySelect(setup.Choices.Current,0);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
