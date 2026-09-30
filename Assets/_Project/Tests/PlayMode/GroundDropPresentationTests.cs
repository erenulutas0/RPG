using System.Collections;
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

        [UnityTest] public IEnumerator CoincidentDropsRemainSeparateAndPoolDoesNotOverwriteRewards()
        {
            for(int i=0;i<40;i++)_setup.Drops.Add(2,1,1,1);
            Assert.That(_view.GroundCount,Is.EqualTo(40));Assert.That(_view.PoolCount,Is.EqualTo(40));
            Assert.That(_setup.Drops.CollectNear(0,0),Is.Zero);
            _view.enabled=false;
            Assert.That(_view.GroundCount,Is.Zero);Assert.That(_setup.Drops.Count,Is.EqualTo(40));
            _view.enabled=true;
            Assert.That(_view.GroundCount,Is.EqualTo(40));Assert.That(_view.PoolCount,Is.EqualTo(40));
            Assert.That(_setup.Drops.CollectAll(),Is.EqualTo(40));
            Assert.That(_view.GroundCount,Is.Zero);Assert.That(_view.FlyingCount,Is.EqualTo(40));
            Assert.That(_setup.Run.Gold,Is.EqualTo(40));Assert.That(_setup.Run.Experience,Is.EqualTo(40));
            while(_setup.Upgrades.CurrentOffer!=null)Assert.That(_setup.Upgrades.TrySelect(_setup.Upgrades.CurrentOffer,0),Is.True);
            yield return new WaitForSeconds(.5f);
            Assert.That(_view.FlyingCount,Is.Zero);
            _setup.Drops.Add(-2,-1,1,2);
            Assert.That(_view.PoolCount,Is.EqualTo(40));Assert.That(_view.GroundCount,Is.EqualTo(1));
            Assert.That(Body().color,Is.EqualTo(Color.white));
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

        private SpriteRenderer Body()
        {
            foreach(var renderer in _view.GetComponentsInChildren<SpriteRenderer>())
                if(renderer.name=="Reward Body"&&renderer.enabled)return renderer;
            Assert.Fail("No visible reward body.");return null;
        }
    }
}
