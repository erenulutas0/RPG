using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class GroundDropPresentationTests
    {
        private CombatSetup _setup;
        private GroundDropView _view;

        [UnitySetUp] public IEnumerator Load()
        {
            TestProfile.Begin();Time.timeScale=1;
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RunSeedPath));
            File.WriteAllText(DevelopmentStart.RunSeedPath,"42");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            _setup=Object.FindFirstObjectByType<CombatSetup>();
            _view=_setup.GetComponent<GroundDropView>();
            _setup.Encounters.enabled=false;
            foreach(var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))attack.enabled=false;
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;
            var scene=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Ground reward cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        [UnityTest] public IEnumerator RealKillDropsAtFeetAndPaysOnlyOnCollection()
        {
            var enemy=_setup.Encounters.WaveEnemyAt(0);
            Vector3 feet=enemy.transform.position;
            int xp=_setup.Run.Experience,gold=_setup.Run.Gold;
            enemy.ApplyDamage(new DamageContext(100000));
            Assert.That(_view.GroundCount,Is.EqualTo(1));
            Assert.That(_setup.Run.Experience,Is.EqualTo(xp));Assert.That(_setup.Run.Gold,Is.EqualTo(gold));
            var renderer=Body();
            Assert.That(renderer.sprite,Is.SameAs(_view.PaintedSprite));
            Assert.That(renderer.transform.parent.position,Is.EqualTo(feet));
            Assert.That(renderer.transform.parent.GetComponent<SortingGroup>().sortingOrder,Is.EqualTo(1));
            Assert.That(renderer.transform.parent.Find("Contact Shadow").GetComponent<SortingGroup>().sortingOrder,Is.LessThan(1));
            var drop=_setup.Drops.At(0);
            _setup.Drops.CollectNear(drop.X,drop.Y);
            Assert.That(_view.GroundCount,Is.Zero);Assert.That(_view.FlyingCount,Is.EqualTo(1));
            Assert.That(_setup.Run.Experience,Is.EqualTo(xp+drop.Experience));Assert.That(_setup.Run.Gold,Is.EqualTo(gold+drop.Gold));
            Assert.That(_setup.Drops.CollectNear(drop.X,drop.Y),Is.Zero);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator CoincidentDropsShareOnePileThatPaysEveryOne()
        {
            for(int i=0;i<40;i++)_setup.Drops.Add(2,1,1,1);
            Assert.That(_view.GroundCount,Is.EqualTo(1));Assert.That(_view.PileSize(0),Is.EqualTo(40));
            Assert.That(_view.PoolCount,Is.EqualTo(16),"One pile on show takes one pooled visual, not forty.");
            Assert.That(_setup.Drops.CollectNear(0,0),Is.Zero);
            _view.enabled=false;
            Assert.That(_view.GroundCount,Is.Zero);Assert.That(_setup.Drops.Count,Is.EqualTo(40));
            _view.enabled=true;
            Assert.That(_view.GroundCount,Is.EqualTo(1));Assert.That(_view.PileSize(0),Is.EqualTo(40));
            Assert.That(_view.PoolCount,Is.EqualTo(16));
            Assert.That(_setup.Drops.CollectAll(),Is.EqualTo(40));
            Assert.That(_view.GroundCount,Is.Zero);Assert.That(_view.FlyingCount,Is.EqualTo(1));Assert.That(_view.PileCount,Is.Zero);
            Assert.That(_setup.Run.Gold,Is.EqualTo(40));Assert.That(_setup.Run.Experience,Is.EqualTo(40));
            while(_setup.Upgrades.CurrentOffer!=null)Assert.That(_setup.Upgrades.TrySelect(_setup.Upgrades.CurrentOffer,0),Is.True);
            yield return new WaitForSeconds(.5f);
            Assert.That(_view.FlyingCount,Is.Zero);
            _setup.Drops.Add(-2,-1,1,2);
            Assert.That(_view.PoolCount,Is.EqualTo(16));Assert.That(_view.GroundCount,Is.EqualTo(1));
            Assert.That(Body().color,Is.EqualTo(Color.white));
            LogAssert.NoUnexpectedReceived();
        }

        // A hundred kills left lying around a hero who stands, as on the S23's density probe (docs/21), where every drop
        // on show was a body and a shadow that never batched: four drops in each of 25 squares of the floor, a step apart,
        // so more piles lie than the view draws. However many drops lie, the view draws a bounded set of sprites.
        [UnityTest] public IEnumerator AHundredDropsLeftLyingDrawABoundedNumberOfSprites()
        {
            Vector3 hero=Object.FindFirstObjectByType<HeroLookView>().transform.position;
            int xp=_setup.Run.Experience,gold=_setup.Run.Gold,experience=0,coins=0;
            for(int i=0;i<100;i++)
            {
                int square=i%25;
                float x=(3+2*(square%5)+.5f)*GroundDropView.PileCell+(i/25%2==0?-.15f:.15f);
                float y=(-5+2*(square/5)+.5f)*GroundDropView.PileCell+(i/50==0?-.15f:.15f);
                int dropXp=i%2==0?1:3,dropGold=i%2==0?0:2;
                _setup.Drops.Add(x,y,dropXp,dropGold);experience+=dropXp;coins+=dropGold;
            }
            Assert.That(_setup.Drops.Count,Is.EqualTo(100));
            Assert.That(_view.PileCount,Is.EqualTo(25));Assert.That(_view.PiledDropCount,Is.EqualTo(100));
            Assert.That(_view.GroundCount,Is.EqualTo(GroundDropView.MaxShownPiles));
            AssertBoundedSprites(GroundDropView.MaxShownPiles);
            float heroY=ArenaFloor.FloorY(hero.y),farthestShown=0,nearestParked=float.MaxValue;
            for(int i=0;i<_view.PileCount;i++)
            {
                Assert.That(_view.PileSize(i),Is.EqualTo(4));
                var anchor=_view.PileAnchor(i);
                float distance=ArenaFloor.FloorDistanceSquared(hero.x,heroY,anchor.X,anchor.Y);
                if(_view.IsPileShown(i))farthestShown=Mathf.Max(farthestShown,distance);
                else nearestParked=Mathf.Min(nearestParked,distance);
            }
            Assert.That(farthestShown,Is.LessThanOrEqualTo(nearestParked),"The piles on show are the ones nearest the hero.");
            yield return null;
            Assert.That(_view.GroundCount,Is.EqualTo(GroundDropView.MaxShownPiles),"A hero who stands changes nothing.");
            AssertBoundedSprites(GroundDropView.MaxShownPiles);
            // Piles are presentation: the floor's sweep pays every drop exactly, and only the piles on show fly.
            Assert.That(_setup.Drops.CollectAll(),Is.EqualTo(100));
            Assert.That(_setup.Run.Experience,Is.EqualTo(xp+experience));Assert.That(_setup.Run.Gold,Is.EqualTo(gold+coins));
            Assert.That(_view.PileCount,Is.Zero);Assert.That(_view.GroundCount,Is.Zero);
            Assert.That(_view.FlyingCount,Is.EqualTo(GroundDropView.MaxShownPiles));
            AssertBoundedSprites(GroundDropView.MaxShownPiles);
            Assert.That(_view.PoolCount,Is.LessThanOrEqualTo(Mathf.Max(16,GroundDropView.MaxShownPiles)),"The pool never grows past the piles on show.");
            while(_setup.Upgrades.CurrentOffer!=null)Assert.That(_setup.Upgrades.TrySelect(_setup.Upgrades.CurrentOffer,0),Is.True);
            yield return new WaitForSeconds(.5f);
            Assert.That(_view.FlyingCount,Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        // Two drops in one square, the second beyond the reach of a hero standing on the first's side: only the first pays,
        // it flies alone, and what is left stands on the drop that is left.
        [UnityTest] public IEnumerator APileGivesUpOnlyTheDropsWithinReach()
        {
            _setup.Drops.Add(2.85f,.1f,1,0);
            _setup.Drops.Add(3.45f,.6f,1,1);
            Assert.That(_view.PileCount,Is.EqualTo(1));Assert.That(_view.GroundCount,Is.EqualTo(1));
            Assert.That(Body().sprite,Is.SameAs(_view.PaintedSprite),"A pile wears the art of everything it holds.");
            AssertStandsOn(_view.PileRoot(0),2.85f,.1f);
            int xp=_setup.Run.Experience,gold=_setup.Run.Gold;
            Assert.That(_setup.Drops.CollectNear(2f,.1f),Is.EqualTo(1));
            Assert.That(_setup.Run.Experience,Is.EqualTo(xp+1));Assert.That(_setup.Run.Gold,Is.EqualTo(gold));
            Assert.That(_view.GroundCount,Is.EqualTo(1));Assert.That(_view.FlyingCount,Is.EqualTo(1));
            Assert.That(_view.PileSize(0),Is.EqualTo(1));
            AssertStandsOn(_view.PileRoot(0),3.45f,.6f);
            Assert.That(_setup.Drops.CollectNear(3.45f,.6f),Is.EqualTo(1));
            Assert.That(_setup.Run.Gold,Is.EqualTo(gold+1));
            Assert.That(_view.PileCount,Is.Zero);Assert.That(_view.GroundCount,Is.Zero);Assert.That(_view.FlyingCount,Is.EqualTo(2));
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        // A row of lone drops east of the hero, more than the view draws: the far end comes on show as he nears it, and the
        // near end, which still lies, waits unseen until he comes back.
        [UnityTest] public IEnumerator PilesBeyondTheCapComeOnShowAsTheHeroNearsThem()
        {
            Transform hero=Object.FindFirstObjectByType<HeroLookView>().transform;
            Vector3 start=hero.position;
            int count=GroundDropView.MaxShownPiles+5;
            for(int i=0;i<count;i++)_setup.Drops.Add(start.x+3+1.4f*i,ArenaFloor.FloorY(start.y),1,0);
            Assert.That(_view.PileCount,Is.EqualTo(count));Assert.That(_view.GroundCount,Is.EqualTo(GroundDropView.MaxShownPiles));
            Assert.That(_view.IsPileShown(0),Is.True);Assert.That(_view.IsPileShown(count-1),Is.False);
            hero.position=new Vector3(start.x+3+1.4f*(count-1),start.y,start.z);
            _view.ShowNearest();
            Assert.That(_view.IsPileShown(count-1),Is.True);Assert.That(_view.IsPileShown(0),Is.False);
            Assert.That(_view.GroundCount,Is.EqualTo(GroundDropView.MaxShownPiles));Assert.That(_setup.Drops.Count,Is.EqualTo(count));
            hero.position=start;
            _view.ShowNearest();
            Assert.That(_view.IsPileShown(0),Is.True);Assert.That(_view.IsPileShown(count-1),Is.False);
            AssertBoundedSprites(GroundDropView.MaxShownPiles);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator LevelChoiceFreezesFlightAndDeathNeverCollectsRestingDrops()
        {
            _setup.Drops.Add(.25f,0,10,0);
            Assert.That(Body().sprite,Is.SameAs(_view.ExperienceSprite));
            Assert.That(Body().transform.localScale.x,Is.EqualTo(.30f/(96f/128f*800f/1254f)).Within(.001f));
            _setup.Drops.CollectNear(0,0);
            Assert.That(_setup.Run.Level,Is.EqualTo(1));Assert.That(_setup.Run.Gold,Is.Zero);
            Assert.That(_setup.Pause.IsFrozen,Is.True);
            yield return null;
            var body=Body();var position=body.transform.parent.position;var scale=body.transform.localScale;var colour=body.color;
            yield return new WaitForSecondsRealtime(.28f);
            Assert.That(body.transform.parent.position,Is.EqualTo(position));Assert.That(body.transform.localScale,Is.EqualTo(scale));
            Assert.That(body.color,Is.EqualTo(colour));Assert.That(_view.FlyingCount,Is.EqualTo(1));
            Assert.That(_setup.Upgrades.TrySelect(_setup.Upgrades.CurrentOffer,0),Is.True);
            yield return new WaitForSeconds(.3f);
            Assert.That(_view.FlyingCount,Is.Zero);
            _setup.Drops.Add(3,0,1,7);
            Object.FindFirstObjectByType<HeroLookView>().GetComponent<Health>().ApplyDamage(new DamageContext(100000));
            Assert.That(_view.GroundCount,Is.Zero);Assert.That(_view.FlyingCount,Is.Zero);
            Assert.That(_setup.Drops.Count,Is.EqualTo(1));Assert.That(_setup.Run.Gold,Is.Zero);
            Assert.That(_setup.Drops.CollectAll(),Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest] public IEnumerator CurrencyFallbacksAndReloadPreserveBorrowedNativeArt()
        {
            Sprite sprite=_view.PaintedSprite;
            Sprite experience=_view.ExperienceSprite;
            Assert.That(experience,Is.Not.Null);
            Assert.That(experience.texture.isReadable,Is.False);
            Assert.That(experience.texture.width,Is.EqualTo(96));
            Assert.That(experience.texture.format,Is.EqualTo(TextureFormat.ASTC_4x4));
            Assert.That(experience.pivot.x,Is.EqualTo(96f*623/1254).Within(.01f));
            Assert.That(experience.pivot.y,Is.EqualTo(96f*111/1254).Within(.01f));
            Assert.That(sprite,Is.Not.Null);Assert.That(sprite.texture.isReadable,Is.False);
            Assert.That(sprite.texture.width,Is.EqualTo(96));Assert.That(sprite.texture.height,Is.EqualTo(96));
            Assert.That(sprite.texture.format,Is.EqualTo(TextureFormat.ASTC_4x4));
            Assert.That(sprite.texture.filterMode,Is.EqualTo(FilterMode.Bilinear));Assert.That(sprite.texture.mipmapCount,Is.EqualTo(1));
            Assert.That(sprite.pixelsPerUnit,Is.EqualTo(128));
            Assert.That(sprite.pivot.x,Is.EqualTo(96f*636/1254).Within(.01f));
            Assert.That(sprite.pivot.y,Is.EqualTo(96f*132/1254).Within(.01f));
            _setup.Drops.Add(2,0,0,1);
            Assert.That(Body().sprite.name,Is.EqualTo("Reward Gold Fallback"));
            _setup.Drops.Add(-2,0,1,0);
            bool xpFound=false;
            foreach(var renderer in _view.GetComponentsInChildren<SpriteRenderer>())
                if(renderer.sprite!=null && renderer.sprite==experience)xpFound=true;
            Assert.That(xpFound,Is.True);
            _setup.Drops.Add(1,3,0,0); // Maintained in the value list, without a false currency symbol.
            Assert.That(_view.GroundCount,Is.EqualTo(3));
            var previousView=_view;
            var previousDrops=_setup.Drops;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            _view=Object.FindFirstObjectByType<GroundDropView>();
            Assert.That(previousView==null,Is.True);
            Assert.That(sprite!=null&&sprite.texture!=null,Is.True);
            Assert.That(experience!=null&&experience.texture!=null,Is.True);
            Assert.That(_view.ExperienceSprite,Is.SameAs(experience));
            Assert.That(_view.PaintedSprite,Is.SameAs(sprite));Assert.That(_view.GroundCount,Is.Zero);
            previousDrops.Add(0,0,1,1); // A destroyed subscriber must not receive this event.
            Assert.That(_view.GroundCount,Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        // The sprites drawn for rewards, resting or flying: a body and a shadow per visual on show, in at most two
        // materials (the sprites' own, and the foundry's loot silhouette on the bodies), however many drops lie.
        private void AssertBoundedSprites(int visuals)
        {
            int drawn=0;var materials=new HashSet<Material>();
            foreach(var renderer in _view.GetComponentsInChildren<SpriteRenderer>())
            {
                if(renderer.transform.parent==null||renderer.transform.parent.name!="Ground Reward")continue;
                if(!renderer.enabled||renderer.sprite==null)continue;
                drawn++;materials.Add(renderer.sharedMaterial);
            }
            Assert.That(drawn,Is.LessThanOrEqualTo(2*visuals),"A reward body and its shadow per visual on show.");
            Assert.That(materials.Count,Is.LessThanOrEqualTo(2));
        }

        private static void AssertStandsOn(Transform root,float x,float y)
        {
            Assert.That(root,Is.Not.Null,"The pile is on show.");
            Assert.That(Vector3.Distance(root.position,new Vector3(x,ArenaFloor.WorldY(y),0)),Is.LessThan(1e-4f));
        }

        private SpriteRenderer Body()
        {
            foreach(var renderer in _view.GetComponentsInChildren<SpriteRenderer>())
                if(renderer.name=="Reward Body"&&renderer.enabled)return renderer;
            Assert.Fail("No visible reward body.");return null;
        }
    }
}
