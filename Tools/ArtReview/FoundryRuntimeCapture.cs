using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // Actual integrated view, with explicitly staged drops. No view transforms or sprites are substituted.
    public sealed class FoundryRuntimeCapture
    {
        private const string Output = "ArtDirection/2026-09-23/foundry-playable-01/";
        private Camera _camera;
        private Scene _scene;

        [UnityTest]
        public IEnumerator CaptureGroundDepthAndCollection()
        {
            Directory.CreateDirectory(Output);
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RunSeedPath));
            File.WriteAllText(DevelopmentStart.RunSeedPath, "42");
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.captureDeltaTime = 1f / 10f;
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            _scene = SceneManager.GetActiveScene();
            _camera = Camera.main;
            _camera.aspect = 1080f / 2340f;
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var hero = Object.FindFirstObjectByType<HeroLookView>();
            var view = Object.FindFirstObjectByType<GroundDropView>();
            Assert.That(view.PaintedSprite, Is.Not.Null);
            setup.Encounters.enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            _camera.GetComponent<ArenaCameraFollow>().enabled = false;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = _camera; canvas.planeDistance = 1;
                // Production canvases are overlays. Preserve that above-world ordering
                // when rendering them through a camera for screenshots, including modals.
                canvas.sortingOrder += 30000;
            }
            foreach (var fitter in Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                fitter.enabled = false;
                var rect = (RectTransform)fitter.transform;
                rect.anchorMin = new Vector2(0, 76f / 2340); rect.anchorMax = new Vector2(1, 1 - 100f / 2340);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080, 2340, 361, 2340 - 427);
            Object.FindFirstObjectByType<ArenaView>().Backdrop.FrameCavern();
            Canvas.ForceUpdateCanvases();
            var movement=hero.GetComponent<HeroMovementInput>();
            _camera.GetComponent<ArenaCameraFollow>().enabled=true;
            var arena=Object.FindFirstObjectByType<ArenaView>();
            Assert.That(arena.Geometry.IsFoundry,Is.True);
            Assert.That(arena.Region, Is.Not.Null);
            var route=new Cryptforge.Art.FoundryPursuit();
            Capture("start",1080,2340);
            var report=new System.Text.StringBuilder("point,seconds,x,y,stamina\n");
            int index=0;
            foreach(var design in new[]{new Vector2(-23,-23),new Vector2(-15,6),new Vector2(0,0),new Vector2(16,6),new Vector2(28,6)})
            {
                Cryptforge.Art.FoundryMap.ToFloor(design.x,design.y,out float gx,out float gy);
                var target=new Vector2(gx,gy);
                float elapsed=0;
                while(Vector2.Distance(new Vector2(movement.Motion.X,movement.Motion.Y),target)>.2f && elapsed<120)
                {
                    float tx=target.x,ty=target.y;route.Target(movement.Motion.X,movement.Motion.Y,target.x,target.y,ref tx,ref ty);
                    Vector2 direction=new Vector2(tx,ty)-new Vector2(movement.Motion.X,movement.Motion.Y);
                    movement.Hold(direction.normalized);
                    yield return null;elapsed+=Time.deltaTime;
                    Assert.That(arena.Geometry.IsOnPlatform(movement.Motion.X,movement.Motion.Y),Is.True);
                }
                movement.Release();yield return null;
                Assert.That(elapsed,Is.LessThan(120),"Waypoint not reachable");
                report.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1:F2},{2:F2},{3:F2},{4:F2}",index,elapsed,movement.Motion.X,movement.Motion.Y,movement.Stamina.Current));
                _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
                Capture("point-"+index,1080,2340);index++;
            }
            File.WriteAllText(Output+"walk.csv",report.ToString());
            // Portrait landmark review at the production follow width, with the existing HUD visible.
            // These are staged positions, not a walking/performance proof. Keep them before any closeup
            // camera changes or upgrade choices; TestProfile isolates all run and development-file writes.
            var follow = _camera.GetComponent<ArenaCameraFollow>();
            var gateReport = new System.Text.StringBuilder(
                "design_u,design_v,floor_x,floor_y,camera_x,camera_y,orthographic_size,visible_width,hud_enabled\n");
            // Last point aligns screen x with the pediment centre at design (32.18, 5):
            // equal u-v means equal projected x. It distinguishes lateral cropping from vertical HUD overlap.
            foreach (var gateDesign in new[] { new Vector2(22, 6), new Vector2(26, 6),
                new Vector2(28, 6), new Vector2(30, 2.82f) })
            {
                Cryptforge.Art.FoundryMap.ToFloor(gateDesign.x, gateDesign.y, out float gateX, out float gateY);
                Assert.That(arena.Geometry.IsOnPlatform(gateX, gateY), Is.True, "Gate review point must be walkable");
                movement.Release();
                movement.Motion.Place(gateX, gateY, arena.Geometry);
                hero.transform.position = new Vector3(movement.Motion.X, movement.Motion.Y * .5f, hero.transform.position.z);
                // Let actor/visibility updates observe the placement, then use the same portrait HUD band
                // as the route captures. Frame snaps through the live follow component's production Goal().
                yield return null;
                Canvas.ForceUpdateCanvases();
                follow.Frame(1080, 2340, 361, 2340 - 427);
                yield return null;
                yield return null;
                Assert.That(Vector3.Distance(_camera.transform.position, follow.Goal()), Is.LessThan(.002f),
                    "Portrait capture must use settled follow framing");
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (canvas.isRootCanvas) Assert.That(canvas.enabled, Is.True, "Portrait review keeps HUD enabled");
                Capture(gateDesign.x == 30 ? "gate-approach-centred" : "gate-approach-" + (int)gateDesign.x, 1080, 2340);
                Vector3 cameraPosition = _camera.transform.position;
                gateReport.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "{0:F2},{1:F2},{2:F4},{3:F4},{4:F4},{5:F4},{6:F4},{7:F4},true",
                    gateDesign.x, gateDesign.y, movement.Motion.X, movement.Motion.Y, cameraPosition.x, cameraPosition.y,
                    _camera.orthographicSize, follow.VisibleWidth));
                if (gateDesign.x == 30)
                {
                    CaptureGateFramingStudy(follow, hero.transform);
                    CaptureGateArrivalStudy(follow);
                }
            }
            File.WriteAllText(Output + "gate-approach.csv", gateReport.ToString());
            // Matched actual-render comparison at a walkable point behind the near boiler main.
            Cryptforge.Art.FoundryMap.ToFloor(22,0,out float reviewX,out float reviewY);
            movement.Motion.Place(reviewX,reviewY,arena.Geometry);
            hero.transform.position=new Vector3(reviewX,reviewY*.5f,hero.transform.position.z);
            yield return null;
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
            Capture("ground-readability-on",1080,2340);
            var visibility=arena.Region.GetComponent<FoundryVisibilityView>();
            visibility.enabled=false;
            Capture("ground-readability-off",1080,2340);
            visibility.enabled=true;yield return null;
            // Depth ordering against structures (2026-09-30). Each view twice: depth on, and the visibility view
            // disabled, which is the old ordering - every actor drawn over every structure, no cutaway.
            _camera.GetComponent<ArenaCameraFollow>().enabled=false;
            // Closeups without the HUD, which would cover most of a square frame.
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(canvas.isRootCanvas)canvas.enabled=false;
            // The hero just in front of the north-west boiler: nothing behind him may be cut away.
            Cryptforge.Art.FoundryMap.ToFloor(18.5f,10.4f,out float frontX,out float frontY);
            movement.Motion.Place(frontX,frontY,arena.Geometry);
            hero.transform.position=new Vector3(frontX,frontY*.5f,hero.transform.position.z);
            yield return null;
            float savedSize=_camera.orthographicSize,savedAspect=_camera.aspect;Vector3 savedCamera=_camera.transform.position;
            _camera.aspect=1;_camera.orthographicSize=4.2f;
            _camera.transform.position=new Vector3(frontX,frontY*.5f+1.4f,savedCamera.z);
            Capture("depth-hero-before-tower",1000,1000);
            visibility.enabled=false;
            Capture("depth-hero-before-tower-old",1000,1000);
            visibility.enabled=true;yield return null;
            // An enemy on the apron just behind the south-west boiler: the boiler is in front of it.
            var blocked=setup.Encounters.WaveEnemyAt(0);
            Cryptforge.Art.FoundryMap.ToFloor(18.5f,-1.2f,out float behindX,out float behindY);
            blocked.transform.position=new Vector3(behindX,behindY*.5f,blocked.transform.position.z);
            yield return null;
            _camera.orthographicSize=3.6f;
            _camera.transform.position=new Vector3(behindX+.4f,behindY*.5f-.9f,savedCamera.z);
            Capture("depth-enemy-behind-tower",1000,1000);
            visibility.enabled=false;
            Capture("depth-enemy-behind-tower-old",1000,1000);
            visibility.enabled=true;yield return null;
            _camera.orthographicSize=savedSize;_camera.aspect=savedAspect;_camera.transform.position=savedCamera;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(canvas.isRootCanvas)canvas.enabled=true;
            // Review the complete blockout without changing the playable camera or the scene asset.
            _camera.GetComponent<ArenaCameraFollow>().enabled=false;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(canvas.isRootCanvas)canvas.enabled=false;
            _camera.transform.position=new Vector3(1,-1,_camera.transform.position.z);
            _camera.aspect=4f/3f;_camera.orthographicSize=26;
            arena.Backdrop.FrameCavern();
            Capture("overview",1600,1200);
            _camera.aspect=1;_camera.orthographicSize=7.8f;
            _camera.transform.position=new Vector3(-24,1.5f,_camera.transform.position.z);
            Capture("crane-detail",1024,1024);
            _camera.orthographicSize=5;
            _camera.transform.position=new Vector3(-9.9f,-1.4f,_camera.transform.position.z);
            Capture("shaft-detail",1024,1024);
            _camera.orthographicSize=8.8f;
            _camera.transform.position=new Vector3(19,16.4f,_camera.transform.position.z);
            Capture("gate-detail",1200,1200);
            _camera.orthographicSize=4.3f;
            _camera.transform.position=new Vector3(21.2f,10,_camera.transform.position.z);
            Capture("furnace-detail",1200,1200);
            // Lift the review camera so the full plume fits; the playable camera is unchanged.
            _camera.transform.position+=Vector3.up*1.5f;
            // Hold this actual camera briefly to demonstrate live smoke and embers.
            for(int frame=0;frame<8;frame++)
            {
                yield return new WaitForSeconds(.2f);
                Capture("atmosphere-"+frame,800,800);
            }
            var atmosphere=Object.FindFirstObjectByType<FoundryAtmosphereView>();
            var atmosphereRenderer=atmosphere.GetComponent<MeshRenderer>();
            File.WriteAllText(Output+"atmosphere-info.txt",atmosphereRenderer.sharedMaterial.shader.name+"\n"+
                atmosphere.GetComponent<MeshFilter>().sharedMesh.bounds.ToString());
            atmosphereRenderer.enabled=false;
            Capture("atmosphere-off",800,800);
            atmosphereRenderer.enabled=true;
            Capture("atmosphere-on",800,800);
            _camera.orthographicSize=6;
            _camera.transform.position=new Vector3(0,-14,_camera.transform.position.z);
            Capture("bridge-detail",1200,1200);
            _camera.orthographicSize=6;
            _camera.transform.position=new Vector3(13,-5,_camera.transform.position.z);
            Capture("undercroft-detail",1200,1200);
            _camera.orthographicSize=5;
            _camera.transform.position=new Vector3(-18,-4,_camera.transform.position.z);
            Capture("workshop-detail",1200,1200);
            _camera.orthographicSize=4;
            _camera.transform.position=new Vector3(-11,8.3f,_camera.transform.position.z);
            Capture("pipe-storage-detail",1200,1200);
            // The forged UI panel at its largest - a level-up choice over the HUD - at the playable camera. Taken after
            // every timed capture: the open choice pauses the run, and nothing below waits on scaled time.
            _camera.orthographicSize=savedSize;_camera.aspect=savedAspect;_camera.transform.position=savedCamera;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(canvas.isRootCanvas)canvas.enabled=true;
            setup.Run.GrantBonusUpgrade();
            yield return new WaitForSecondsRealtime(.6f);
            Canvas.ForceUpdateCanvases();
            Capture("hud-choice",1080,2340);
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(canvas.isRootCanvas)canvas.enabled=false;
            var statics=arena.Region.GetComponentsInChildren<MeshFilter>();
            int vertices=0;foreach(var filter in statics)vertices+=filter.sharedMesh.vertexCount;
            File.WriteAllText(Output+"static-budget.txt","renderers="+statics.Length+"\nvertices="+vertices+"\n");
            // Export a clean, high-resolution image of the actual authored map for sharing.
            foreach(var enemy in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                enemy.gameObject.SetActive(false);
            foreach(var chest in Object.FindObjectsByType<ChestSpawner>(FindObjectsSortMode.None))
                foreach(var sprite in chest.GetComponentsInChildren<SpriteRenderer>())
                    sprite.enabled=false;
            hero.gameObject.SetActive(false);
            _camera.transform.position=new Vector3(1,-1,_camera.transform.position.z);
            _camera.aspect=4f/3f;_camera.orthographicSize=26;
            Capture("map-export",3200,2400);
            LogAssert.NoUnexpectedReceived();
        }

        // Art storyboard only: the live foundry has no safe non-combat gate-arrival phase yet.
        // Render a full landmark reveal without creating an automatic gameplay camera takeover.
        private void CaptureGateArrivalStudy(ArenaCameraFollow follow)
        {
            Vector3 originalPosition = _camera.transform.position;
            float originalSize = _camera.orthographicSize;
            bool originalFollow = follow.enabled;
            var roots = new System.Collections.Generic.List<Canvas>();
            var states = new System.Collections.Generic.List<bool>();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (canvas.isRootCanvas) { roots.Add(canvas); states.Add(canvas.enabled); }
            try
            {
                follow.enabled = false;
                Capture("gate-arrival-combat", 1080, 2340);
                foreach (var canvas in roots) canvas.enabled = false;
                Vector3 reveal = new Vector3(19, 16.4f, originalPosition.z);
                float revealSize = 17.6f * 2340f / (2f * 1080f);
                _camera.transform.position = Vector3.Lerp(originalPosition, reveal, .5f);
                _camera.orthographicSize = Mathf.Lerp(originalSize, revealSize, .5f);
                Capture("gate-arrival-transition", 1080, 2340);
                _camera.transform.position = reveal;
                _camera.orthographicSize = revealSize;
                Capture("gate-arrival-reveal", 1080, 2340);
            }
            finally
            {
                _camera.transform.position = originalPosition;
                _camera.orthographicSize = originalSize;
                follow.enabled = originalFollow;
                for (int i = 0; i < roots.Count; i++) roots[i].enabled = states[i];
                Canvas.ForceUpdateCanvases();
                Object.FindFirstObjectByType<ArenaView>().Backdrop.FrameCavern();
            }
            Assert.That(_camera.transform.position, Is.EqualTo(originalPosition));
            Assert.That(_camera.orthographicSize, Is.EqualTo(originalSize));
            Assert.That(follow.enabled, Is.EqualTo(originalFollow));
            for (int i = 0; i < roots.Count; i++) Assert.That(roots[i].enabled, Is.EqualTo(states[i]));
            Capture("gate-arrival-restored", 1080, 2340);
        }

        // Test-only lens/composition alternatives. No production serialized camera/HUD values are edited.
        // Freeze time between these renders so each alternative uses exactly the same scene pose.
        private void CaptureGateFramingStudy(ArenaCameraFollow follow, Transform hero)
        {
            Vector3 productionPosition = _camera.transform.position;
            float productionSize = _camera.orthographicSize;
            bool productionFollowEnabled = follow.enabled;
            var rows = new System.Text.StringBuilder(
                "variant,camera_x,camera_y,orthographic_size,visible_width,hero_viewport_x,hero_viewport_y\n");
            try
            {
                follow.enabled = false;
                foreach (string variant in new[] { "production", "up-1p5", "width-11" })
                {
                    _camera.transform.position = productionPosition + (variant == "up-1p5" ? Vector3.up * 1.5f : Vector3.zero);
                    _camera.orthographicSize = variant == "width-11" ? 11f * 2340f / (2f * 1080f) : productionSize;
                    Canvas.ForceUpdateCanvases();
                    Capture("gate-study-" + variant, 1080, 2340);
                    Vector3 viewport = _camera.WorldToViewportPoint(hero.position);
                    rows.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0},{1:F4},{2:F4},{3:F4},{4:F4},{5:F4},{6:F4}", variant,
                        _camera.transform.position.x, _camera.transform.position.y, _camera.orthographicSize,
                        _camera.orthographicSize * 2f * 1080f / 2340f, viewport.x, viewport.y));
                }
            }
            finally
            {
                _camera.transform.position = productionPosition;
                _camera.orthographicSize = productionSize;
                follow.enabled = productionFollowEnabled;
                Canvas.ForceUpdateCanvases();
                Object.FindFirstObjectByType<ArenaView>().Backdrop.FrameCavern();
            }
            Assert.That(_camera.transform.position, Is.EqualTo(productionPosition));
            Assert.That(_camera.orthographicSize, Is.EqualTo(productionSize));
            Assert.That(follow.enabled, Is.EqualTo(productionFollowEnabled));
            File.WriteAllText(Output + "gate-framing-study.csv", rows.ToString());
        }

        private void Capture(string name,int width,int height)
        {
            var target=RenderTexture.GetTemporary(width,height,24);var previous=RenderTexture.active;
            float aspect=_camera.aspect;_camera.aspect=(float)width/height;_camera.targetTexture=target;
            // Each isolated review camera must frame the backdrop as normal LateUpdate would.
            Object.FindFirstObjectByType<ArenaView>().Backdrop.FrameCavern();
            _camera.Render();RenderTexture.active=target;
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            using(var writer=new BinaryWriter(File.Create(Output+name+".bmp")))
            {
                int bytes=width*height*3;writer.Write((ushort)0x4d42);writer.Write(bytes+54);writer.Write(0);writer.Write(54);
                writer.Write(40);writer.Write(width);writer.Write(height);writer.Write((ushort)1);writer.Write((ushort)24);
                writer.Write(0);writer.Write(bytes);writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);
                foreach(Color32 p in pixels.GetPixels32()){writer.Write(p.b);writer.Write(p.g);writer.Write(p.r);}
            }
            Object.Destroy(pixels);_camera.targetTexture=null;_camera.aspect=aspect;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;Time.captureDeltaTime=0;

            if(_scene.IsValid()&&_scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Reward capture cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}

