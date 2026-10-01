using System.Collections;
using System.IO;
using Cryptforge.Core;
using Cryptforge.UI;
using Cryptforge.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class FoundryRegionTests
    {
        private Scene _scene;
        [SetUp] public void Begin()=>TestProfile.Begin();
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;
            if(_scene.IsValid()&&_scene.isLoaded){SceneManager.SetActiveScene(SceneManager.CreateScene("Foundry cleanup"));yield return SceneManager.UnloadSceneAsync(_scene);}
            TestProfile.End();
        }
        [UnityTest] public IEnumerator FlagCreatesPlayableMapAndReloadReleasesMeshes()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene=SceneManager.GetActiveScene();Time.timeScale=0;
            var arena=Object.FindFirstObjectByType<ArenaView>();
            var setup=Object.FindFirstObjectByType<CombatSetup>();
            var movement=Object.FindFirstObjectByType<HeroMovementInput>();
            Assert.That(arena.Region,Is.Not.Null);Assert.That(arena.Platform,Is.Null);
            var gate=arena.PlatformMaterials.GateFacade;
            Assert.That(gate,Is.Not.Null,"Foundry facade must be an imported shared material.");
            Assert.That(((Texture2D)gate.mainTexture).isReadable,Is.False);
            Assert.That(gate.mainTexture.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            bool usesGate=false;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterial.mainTexture==gate.mainTexture)usesGate=true;
            Assert.That(usesGate,Is.True);
            var surfaces=new[]{arena.PlatformMaterials.FoundryWall,arena.PlatformMaterials.FurnaceFacade,arena.PlatformMaterials.FoundryIron,arena.PlatformMaterials.FoundryEmbers,arena.PlatformMaterials.FoundryPaving};
            foreach(var shared in surfaces)
            {
                Assert.That(shared,Is.Not.Null);
                Assert.That(((Texture2D)shared.mainTexture).isReadable,Is.False);
                bool used=false;
                foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.sharedMaterial.mainTexture==shared.mainTexture)used=true;
                Assert.That(used,Is.True,"Imported foundry surface must be used by the scene.");
            }
            Assert.That(arena.Region.GetComponentsInChildren<MeshRenderer>().Length,Is.LessThanOrEqualTo(17),"Static architecture, including the separate iron overlay rank, must stay batched.");
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                Assert.That(renderer.sortingOrder,Is.InRange(-18,-1),"Scenery must remain above the cavern and below combatants.");

            var visibility=arena.Region.GetComponent<FoundryVisibilityView>();
            Assert.That(visibility,Is.Not.Null);
            var ownedMaterials=new System.Collections.Generic.HashSet<Material>();
            int floorDecals=0;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.sortingOrder < -14) continue;
                // Flush floor decals share the parapet rank but are the walking surface: the cutaway never takes them.
                if(renderer.name==FoundryRegionView.FloorDecalLayer)
                {
                    floorDecals++;
                    Assert.That(renderer.sharedMaterial.shader.name,Is.Not.EqualTo("Cryptforge/Foundry Cutaway"),
                        "Brass inlays must not vanish around the hero's feet.");
                    continue;
                }
                var variant=renderer.sharedMaterial;
                Assert.That(variant.shader.name,Is.EqualTo("Cryptforge/Foundry Cutaway"));
                ownedMaterials.Add(variant);
            }
            Assert.That(floorDecals,Is.EqualTo(1),"One batch of flush floor decals.");
            yield return null; // Start and LateUpdate bind the actual hero, including while paused.
            foreach(var variant in ownedMaterials)
            {
                Vector4 focus=variant.GetVector("_HeroFocus");
                Assert.That(focus.w,Is.EqualTo(1));
                Assert.That(focus.x,Is.EqualTo(movement.transform.position.x).Within(.001f));
                Assert.That(focus.y,Is.EqualTo(movement.transform.position.y+.35f).Within(.001f));
            }
            // Structures carry their ground-contact depth; while a camera renders, actor sprites follow their feet.
            // Roots never move, and gameplay reading positions between renders sees the original z.
            float heroDepth=FoundryDepth.ActorZ(arena.Region.transform.position,movement.transform.position.y);
            foreach(var variant in ownedMaterials)Assert.That(variant.GetVector("_HeroFocus").z,Is.EqualTo(heroDepth).Within(.001f));
            Vector3 heroRoot=movement.transform.position;
            visibility.ApplyActorDepth();
            Assert.That(movement.transform.childCount,Is.GreaterThan(0));
            foreach(Transform child in movement.transform)Assert.That(child.position.z,Is.EqualTo(heroDepth).Within(.001f));
            Assert.That(movement.transform.position,Is.EqualTo(heroRoot),"Depth must never move the gameplay root.");
            visibility.RestoreActorDepth();
            foreach(Transform child in movement.transform)Assert.That(child.localPosition.z,Is.Zero,"Between renders actors keep their authored z.");
            visibility.ApplyActorDepth();
            bool writesDepth=false;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.sharedMaterial.shader.name!="Cryptforge/Foundry Cutaway")continue;
                var structure=renderer.GetComponent<MeshFilter>().sharedMesh;
                var flags=new System.Collections.Generic.List<Vector2>();structure.GetUVs(1,flags);
                Assert.That(flags.Count,Is.EqualTo(structure.vertexCount),"Every structure vertex states whether it writes depth.");
                var points=structure.vertices;
                for(int i=0;i<points.Length;i++)if(flags[i].x>0){writesDepth=true;Assert.That(points[i].z,Is.GreaterThan(0f));}
            }
            Assert.That(writesDepth,Is.True,"Tall structures must occlude actors behind them.");
            // Floor light, decal light and tints multiply vertex colours above 1; the sprite shaders carry them in a
            // fixed4 interpolator, whose guaranteed range on every mobile precision is +/-2 (after the material tint).
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                var lit=renderer.GetComponent<MeshFilter>().sharedMesh;
                var colours=new System.Collections.Generic.List<Color>();lit.GetColors(colours);
                Color tint=renderer.sharedMaterial.HasProperty("_Color")?renderer.sharedMaterial.color:Color.white;
                float peak=0,low=0;
                foreach(var c in colours)
                {
                    peak=Mathf.Max(peak,Mathf.Max(c.r*tint.r,Mathf.Max(c.g*tint.g,c.b*tint.b)));
                    low=Mathf.Min(low,Mathf.Min(c.r,Mathf.Min(c.g,c.b)));
                }
                Assert.That(peak,Is.LessThan(2f),renderer.name+": vertex light beyond the shader's guaranteed colour range.");
                Assert.That(low,Is.GreaterThanOrEqualTo(0f),renderer.name+": negative vertex colour.");
            }
            visibility.enabled=false;
            foreach(var variant in ownedMaterials)Assert.That(variant.GetVector("_HeroFocus").w,Is.Zero);
            foreach(Transform child in movement.transform)Assert.That(child.localPosition.z,Is.Zero,"Without the view actors return to the flat layer and draw over every structure.");
            visibility.enabled=true;
            var ring=Object.FindFirstObjectByType<AbilityRingView>().Ring;
            Assert.That(ring.sortingOrder,Is.EqualTo(arena.GroundEffectSortingOrder));
            Assert.That(ring.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder,Is.EqualTo(ring.sortingOrder));
            Assert.That(Object.FindFirstObjectByType<AbilityController>().TryUse(),Is.True);
            SpriteRenderer groundPulse=null;
            foreach(var effect in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
            {
                if(!effect.enabled || effect.sprite!=Cryptforge.Art.AbilityRingArt.Get())continue;
                groundPulse=effect;
                Assert.That(effect.sortingOrder,Is.EqualTo(arena.GroundEffectSortingOrder));
            }
            Assert.That(groundPulse,Is.Not.Null,"The real ability event must produce a grounded pulse.");
            // A floor pulse already sorts under the structures: while a camera renders it keeps the flat layer.
            visibility.RestoreActorDepth();
            float pulseZ=groundPulse.transform.position.z;
            visibility.ApplyActorDepth();
            Assert.That(groundPulse.transform.position.z,Is.EqualTo(pulseZ),"Ground effects are left out of the foundry's depth.");
            visibility.RestoreActorDepth();

            var atmosphere=Object.FindFirstObjectByType<FoundryAtmosphereView>();
            Assert.That(atmosphere,Is.Not.Null);
            var atmosphereMesh=atmosphere.GetComponent<MeshFilter>().sharedMesh;
            var atmosphereRenderer=atmosphere.GetComponent<MeshRenderer>();
            Assert.That(atmosphereRenderer.sharedMaterial,Is.SameAs(arena.Material));
            Assert.That(atmosphereRenderer.sortingOrder,Is.LessThan(1),"Atmosphere cannot cover combatant silhouettes.");
            var initialAtmosphere=atmosphereMesh.vertices;
            Time.timeScale=1;
            yield return new WaitForSeconds(.12f);
            Assert.That(atmosphereMesh.vertices,Is.Not.EqualTo(initialAtmosphere),"Active atmosphere should move.");


            Assert.That(setup.Encounters.AliveEnemyCount,Is.GreaterThan(0));
            for(int i=0;i<setup.Encounters.WaveEnemyCount;i++)
            {
                Vector3 p=setup.Encounters.WaveEnemyAt(i).transform.position;
                Assert.That(arena.Geometry.IsOnPlatform(p.x,p.y*2,.6f),Is.True);
            }
            // Combat effects above the floor stand at the depth of their ground contact while a camera renders, as the actors
            // do: a hit spark takes the struck enemy's feet, not its own height on the body, so the structure hiding the
            // enemy hides its hit too. Numbers and floor pulses keep the flat layer; between renders, and without the view,
            // every effect is where its animation put it.
            var effects=Object.FindFirstObjectByType<CombatEffectsView>();
            Health struck=null;
            for(int i=0;i<setup.Encounters.WaveEnemyCount&&struck==null;i++)
                if(setup.Encounters.WaveEnemyAt(i).IsAlive)struck=setup.Encounters.WaveEnemyAt(i);
            Assert.That(struck,Is.Not.Null);
            visibility.RestoreActorDepth();
            struck.ApplyDamage(new DamageContext(.001f)); // A spark and no number: the amount rounds to nothing.
            var flatZ=new System.Collections.Generic.Dictionary<Transform,float>();
            foreach(var shown in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
                if(shown.enabled)flatZ.Add(shown.transform,shown.transform.position.z);
            visibility.ApplyActorDepth();
            Vector3 regionOrigin=arena.Region.transform.position;
            var aboveFloor=new System.Collections.Generic.HashSet<Transform>();
            int onStruckFeet=0;
            for(int i=0;i<effects.EffectSlotCount;i++)
            {
                Transform raised=effects.AboveFloorEffect(i,out float groundY);
                if(raised==null)continue;
                aboveFloor.Add(raised);
                Assert.That(flatZ.ContainsKey(raised),Is.True,"Only effects on show are placed in depth.");
                Assert.That(raised.position.z,Is.EqualTo(FoundryDepth.ActorZ(regionOrigin,groundY)).Within(.001f),
                    "An effect above the floor stands at the depth of its ground contact.");
                if(groundY==struck.transform.position.y)onStruckFeet++;
            }
            Assert.That(onStruckFeet,Is.GreaterThan(0),"The hit spark stands on the struck enemy's feet.");
            int placed=visibility.PlacedCount;
            visibility.ApplyActorDepth();
            Assert.That(visibility.PlacedCount,Is.EqualTo(placed),"Every apply asks the pool afresh: no effect is remembered twice.");
            foreach(var shown in flatZ)
                if(!aboveFloor.Contains(shown.Key))
                    Assert.That(shown.Key.position.z,Is.EqualTo(shown.Value),"Numbers and floor pulses keep the flat layer, over every structure.");
            visibility.RestoreActorDepth();
            foreach(var shown in flatZ)
                Assert.That(shown.Key.position.z,Is.EqualTo(shown.Value).Within(.0001f),"Between renders effects keep the z their animation gave them.");
            visibility.ApplyActorDepth();
            visibility.enabled=false;
            foreach(var shown in flatZ)
                Assert.That(shown.Key.position.z,Is.EqualTo(shown.Value).Within(.0001f),"Without the view effects return to the flat layer.");
            visibility.enabled=true;
            // Enemies hidden by a structure keep a silhouette: every live enemy body shares the scene's x-ray
            // material while the view runs, and gets its own material back when it stops.
            visibility.ApplyActorDepth();
            Material xray=null;
            for(int i=0;i<setup.Encounters.WaveEnemyCount;i++)
            {
                var body=setup.Encounters.WaveEnemyAt(i).GetComponent<EnemyLookView>().Body;
                Assert.That(body.sharedMaterial.shader.name,Is.EqualTo(FoundryVisibilityView.XrayShaderName));
                if(xray==null)xray=body.sharedMaterial;
                Assert.That(body.sharedMaterial,Is.SameAs(xray),"One shared silhouette material, not one per enemy.");
                // The health bar is a readout: it keeps its authored depth while the body goes to its feet depth.
                var enemy=setup.Encounters.WaveEnemyAt(i).GetComponent<EnemyLookView>();
                Assert.That(enemy.Bar,Is.Not.Null);
                Assert.That(enemy.Bar.localPosition.z,Is.Zero,"Health bars stay on top of scenery, like the damage numbers.");
            }
            Assert.That(xray.passCount,Is.EqualTo(2));
            visibility.enabled=false;
            for(int i=0;i<setup.Encounters.WaveEnemyCount;i++)
                Assert.That(setup.Encounters.WaveEnemyAt(i).GetComponent<EnemyLookView>().Body.sharedMaterial.shader.name,Is.EqualTo("Sprites/Default"));
            visibility.enabled=true;
            // Drops behind a structure keep a silhouette too, in loot gold rather than the enemies' ember: only the reward
            // body, one shared material, kept by a pooled drop when it is shown again, and handed back when the view stops.
            var drops=setup.GetComponent<GroundDropView>();
            setup.Drops.Add(1.5f,1f,0,1); // Gold only: if the hero reaches it later it cannot bring a level choice.
            Assert.That(drops.ShownCount,Is.GreaterThan(0));
            for(int i=0;i<drops.ShownCount;i++)Assert.That(drops.ShownBody(i).sharedMaterial.shader.name,Is.EqualTo("Sprites/Default"));
            visibility.ApplyActorDepth();
            Material loot=drops.ShownBody(0).sharedMaterial;
            Assert.That(loot.shader.name,Is.EqualTo(FoundryVisibilityView.XrayShaderName));
            Assert.That(loot,Is.Not.SameAs(xray),"Loot must not read as a threat.");
            Assert.That(loot.GetColor("_XrayColor"),Is.EqualTo(FoundryVisibilityView.LootXrayColor));
            for(int i=0;i<drops.ShownCount;i++)Assert.That(drops.ShownBody(i).sharedMaterial,Is.SameAs(loot),"One shared loot material, not one per drop.");
            foreach(var sprite in drops.ShownBody(0).transform.parent.GetComponentsInChildren<SpriteRenderer>())
                if(sprite!=drops.ShownBody(0))Assert.That(sprite.sharedMaterial,Is.Not.SameAs(loot),"Only the reward body, never its shadow.");
            foreach(var sprite in movement.GetComponentsInChildren<SpriteRenderer>())
                Assert.That(sprite.sharedMaterial==xray||sprite.sharedMaterial==loot,Is.False,"The hero never gets a silhouette.");
            int registered=visibility.XrayedCount;
            drops.enabled=false;drops.enabled=true; // Parks every drop in the pool and shows it again from there.
            visibility.ApplyActorDepth();
            Assert.That(visibility.XrayedCount,Is.EqualTo(registered),"A re-used pooled drop is not registered twice.");
            for(int i=0;i<drops.ShownCount;i++)Assert.That(drops.ShownBody(i).sharedMaterial,Is.SameAs(loot));
            visibility.enabled=false;
            for(int i=0;i<drops.ShownCount;i++)Assert.That(drops.ShownBody(i).sharedMaterial.shader.name,Is.EqualTo("Sprites/Default"));
            visibility.enabled=true;
            // The chest's root is its sprite, with no children: while a camera renders the root itself stands at the depth of
            // its feet, so a boiler in front hides it, and a hidden chest shows in the drops' loot gold. Between renders it
            // stands where gameplay put it, and without the view it has its own material back.
            var chests=Object.FindFirstObjectByType<ChestSpawner>();
            Assert.That(chests.HasChest,Is.True,"The foundry's first room has a chest.");
            var chest=chests.Body;
            Assert.That(chest.transform.childCount,Is.Zero);
            Assert.That(GroundedSorting.Actors,Does.Contain(chest.transform));
            Assert.That(chest.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder,Is.EqualTo(GroundedSorting.ActorOrder));
            visibility.RestoreActorDepth();
            Vector3 chestSpot=chest.transform.position;
            Assert.That(chestSpot.x,Is.EqualTo(chests.ChestFloorX).Within(.0001f));
            Assert.That(chestSpot.y,Is.EqualTo(ArenaFloor.WorldY(chests.ChestFloorY)).Within(.0001f));
            Assert.That(chestSpot.z,Is.Zero);
            Assert.That(chest.sharedMaterial.shader.name,Is.EqualTo("Sprites/Default"));
            visibility.ApplyActorDepth();
            float chestDepth=FoundryDepth.ActorZ(arena.Region.transform.position,chestSpot.y);
            Assert.That(chest.transform.position.z,Is.EqualTo(chestDepth).Within(.001f),"Structures in front must hide the chest.");
            Assert.That(chest.transform.position.x,Is.EqualTo(chestSpot.x).Within(.0001f),"Depth moves the chest in z only.");
            Assert.That(chest.transform.position.y,Is.EqualTo(chestSpot.y).Within(.0001f),"Depth moves the chest in z only.");
            Assert.That(chest.sharedMaterial,Is.SameAs(loot),"A hidden chest reads as loot, sharing the drops' material.");
            int withChest=visibility.XrayedCount;
            visibility.ApplyActorDepth();
            Assert.That(visibility.XrayedCount,Is.EqualTo(withChest),"The chest is registered once.");
            visibility.RestoreActorDepth();
            Assert.That(chest.transform.localPosition.z,Is.Zero,"Between renders the chest keeps its authored z.");
            Assert.That(chest.transform.position.x,Is.EqualTo(chestSpot.x).Within(.0001f));
            Assert.That(chest.transform.position.y,Is.EqualTo(chestSpot.y).Within(.0001f));
            Assert.That(chest.sharedMaterial,Is.SameAs(loot),"The chest keeps the loot material between renders, like the drops.");
            visibility.ApplyActorDepth();
            visibility.enabled=false;
            Assert.That(chest.transform.localPosition.z,Is.Zero,"Without the view the chest returns to the flat layer.");
            Assert.That(chest.sharedMaterial.shader.name,Is.EqualTo("Sprites/Default"));
            visibility.enabled=true;
            var mesh=arena.Region.GetComponentInChildren<MeshFilter>().sharedMesh;
            var material=arena.Region.GetComponentInChildren<MeshRenderer>().sharedMaterial;
            Assert.That(setup.Pause.TryPause(),Is.True);
            yield return null; // Finish the frame whose delta was calculated before the pause.
            var pausedAtmosphere=atmosphereMesh.vertices;
            Vector3 before=movement.transform.position;movement.Hold(Vector2.right);yield return null;
            Assert.That(movement.transform.position,Is.EqualTo(before),"Paused map must not move.");
            Assert.That(atmosphereMesh.vertices,Is.EqualTo(pausedAtmosphere),"Atmosphere must freeze with combat.");
            File.Delete(DevelopmentStart.RegionPreviewPath);
            Assert.That(arena.IsFoundryRegion,Is.True,"Mode is frozen for a loaded scene.");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene=SceneManager.GetActiveScene();yield return null;
            Assert.That(Object.FindFirstObjectByType<ArenaView>().Region,Is.Null);
            Assert.That(Object.FindFirstObjectByType<FoundryVisibilityView>(),Is.Null,"Ordinary halls place neither actors nor effects in depth.");
            Assert.That(mesh==null,Is.True);
            Assert.That(material==null,Is.EqualTo(ownedMaterials.Contains(material)),"Only scene-owned material variants are destroyed.");
            Assert.That(gate!=null,Is.True,"Reload must not destroy shared facade assets.");
            foreach(var variant in ownedMaterials)Assert.That(variant==null,Is.True,"Scene cutaway variants must be released on reload.");
            Assert.That(xray==null,Is.True,"The enemy silhouette material must be released on reload.");
            Assert.That(loot==null,Is.True,"The loot silhouette material must be released on reload.");
            Assert.That(atmosphereMesh==null,Is.True,"Reload must release dynamic atmosphere geometry.");
            Assert.That(Object.FindFirstObjectByType<FoundryAtmosphereView>(),Is.Null,"Ordinary hall does not acquire foundry atmosphere.");
            foreach(var shared in surfaces)Assert.That(shared!=null,Is.True,"Reload must preserve shared dressing materials.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
