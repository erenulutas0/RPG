using System.Collections;
using System.Linq;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class StaffSplashTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            TestProfile.Begin(new PlayerProfile(0,null,null,0,new[]{"weapon_staff"},"weapon_staff"));
            Time.timeScale=1; Time.captureDeltaTime=1f/60;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale=1;Time.captureDeltaTime=0;
            var scene=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Staff cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);TestProfile.End();
        }
        [UnityTest]
        public IEnumerator RealStaffStrikeKeepsDamageImmediateAndPulseGroundedPausedAndBounded()
        {
            var hero=Object.FindFirstObjectByType<HeroLookView>();
            var attack=hero.GetComponent<AttackController>();
            Object.FindFirstObjectByType<EncounterController>().enabled=false;
            foreach(var other in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))other.enabled=false;
            var target=new GameObject("Staff target").AddComponent<Health>();target.Initialize(10000);
            target.transform.position=hero.transform.position+Vector3.right;
            hero.GetComponent<Targeting>().SetCandidates(new[]{target});
            bool struck=false;
            attack.Struck+=_=>{struck=true;Time.timeScale=0;};
            attack.enabled=true;
            for(int i=0;i<100&&!struck;i++)yield return null;
            Assert.That(struck,Is.True);
            Assert.That(target.Current,Is.EqualTo(10000-attack.Weapon.Damage));
            attack.enabled=false;
            var pulse=GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>()
                .Single(r=>r.enabled&&r.sprite==StaffSplashArt.Get());
            Assert.That(pulse.sortingOrder,Is.Zero);
            Vector3 origin=pulse.transform.position,scale=pulse.transform.localScale;
            Color color=pulse.color;
            Assert.That(scale.x,Is.InRange(1.1f,1.75f));
            target.transform.position+=Vector3.right;
            yield return new WaitForSecondsRealtime(.12f);
            Assert.That(pulse.transform.position,Is.EqualTo(origin));
            Assert.That(pulse.transform.localScale,Is.EqualTo(scale));Assert.That(pulse.color,Is.EqualTo(color));
            Time.timeScale=1;
            for(int i=0;i<16;i++)
            {
                yield return null;
                Assert.That(pulse.transform.localScale.x,Is.LessThanOrEqualTo(1.75f));
            }
            Assert.That(pulse.enabled,Is.False);
            Assert.That(StaffSplashArt.Get().texture.isReadable,Is.False);
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        public IEnumerator ReloadBorrowsTheSameStaffTextureAndSessionResetReleasesIt()
        {
            Sprite first=StaffSplashArt.Get();Texture2D texture=first.texture;
            Assert.That(texture.width,Is.EqualTo(512));Assert.That(texture.height,Is.EqualTo(256));
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");yield return null;
            yield return Resources.UnloadUnusedAssets();
            Assert.That(StaffSplashArt.Get(),Is.SameAs(first));
            Assert.That(StaffSplashArt.Get().texture,Is.SameAs(texture));
            // Release only after unloading consumers; matches application session cleanup.
            var scene=SceneManager.GetActiveScene();SceneManager.SetActiveScene(SceneManager.CreateScene("Staff reset"));
            yield return SceneManager.UnloadSceneAsync(scene);
            StaffSplashArt.ResetSession();yield return null;
            Assert.That(first==null,Is.True);Assert.That(texture==null,Is.True);
            Assert.That(StaffSplashArt.Get(),Is.Not.SameAs(first));
        }
    }
}
