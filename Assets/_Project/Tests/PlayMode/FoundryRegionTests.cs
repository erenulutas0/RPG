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
        private static bool FloorCovers(Mesh mesh,Vector2 point)
        {
            var vertices=mesh.vertices;var triangles=mesh.triangles;
            float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
            for(int i=0;i<triangles.Length;i+=3)
            {
                Vector2 a=vertices[triangles[i]],b=vertices[triangles[i+1]],c=vertices[triangles[i+2]];
                float area=Cross(b-a,c-a);
                if(Mathf.Abs(area)<1e-8f)continue;
                float u=Cross(b-point,c-point)/area,v=Cross(c-point,a-point)/area,w=1-u-v;
                if(u>=-1e-5f&&v>=-1e-5f&&w>=-1e-5f)return true;
            }
            return false;
        }
        [Test] public void ArchitectureLightFollowsFireHeightAndRecessesBelowLedges()
        {
            Color near=FoundryRegionView.ArchitectureLight(-8,2,0,2);
            Color above=FoundryRegionView.ArchitectureLight(-8,2,14,2);
            Assert.That(near.r/near.b,Is.GreaterThan(above.r/above.b+.1f),"Pit heat must fall away above the fire, not climb the entire structure.");
            Color recessed=FoundryRegionView.ArchitectureLight(-8,2,0,0);
            Assert.That(recessed.r,Is.LessThan(near.r*.9f),"The ledge underside needs localized contact shade.");
        }
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
            // Test the actual generated triangles, not just the map predicates: clipped ashlar
            // must leave the removed tips empty and keep the lanes/bridge covered.
            Mesh paving=null;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterial==arena.PlatformMaterials.FoundryPaving)paving=renderer.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(paving,Is.Not.Null);
            foreach(var sample in new[]{new Vector3(-23,-13,0),new Vector3(13,-13,0),
                new Vector3(-20.17f,-9.77f,1),new Vector3(9.83f,-9.77f,1),new Vector3(-23,-23,1),
                new Vector3(-23,17,0),new Vector3(13,17,0),new Vector3(-21.17f,14.77f,1),new Vector3(10.83f,14.77f,1)})
            {
                Cryptforge.Art.FoundryMap.ToFloor(sample.x,sample.y,out float px,out float py);
                Assert.That(FloorCovers(paving,new Vector2(px,py*.5f)),Is.EqualTo(sample.z>0),$"Rendered floor at {sample}");
            }
            var gate=arena.PlatformMaterials.GateFacade;
            Assert.That(gate,Is.Not.Null,"Foundry facade must be an imported shared material.");
            Assert.That(((Texture2D)gate.mainTexture).isReadable,Is.False);
            Assert.That(gate.mainTexture.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            bool usesGate=false;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterial.mainTexture==gate.mainTexture)usesGate=true;
            Assert.That(usesGate,Is.True);
            Assert.That(arena.PlatformMaterials.FoundryPitLining,Is.Not.SameAs(arena.PlatformMaterials.FurnaceFacade),"The pit lining must stay independent of the boiler wrap.");
            var surfaces=new[]{arena.PlatformMaterials.FoundryWall,arena.PlatformMaterials.FurnaceFacade,arena.PlatformMaterials.FoundryPitLining,arena.PlatformMaterials.FoundryIron,arena.PlatformMaterials.FoundryEmbers,arena.PlatformMaterials.FoundryPaving};
            foreach(var shared in surfaces)
            {
                Assert.That(shared,Is.Not.Null);
                Assert.That(((Texture2D)shared.mainTexture).isReadable,Is.False);
                Assert.That(Mathf.IsPowerOfTwo(shared.mainTexture.width)&&Mathf.IsPowerOfTwo(shared.mainTexture.height),Is.True,"Mipmapped foundry surfaces must be POT to preserve Android compression.");
                bool used=false;
                foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                    if(renderer.sharedMaterial.mainTexture==shared.mainTexture)used=true;
                Assert.That(used,Is.True,"Imported foundry surface must be used by the scene.");
            }
            var keeper=arena.PlatformMaterials.ForgeKeeper;
            Assert.That(keeper,Is.Not.Null,"The paired bridge guardians use one imported material.");
            var keeperTexture=keeper.mainTexture as Texture2D;
            Assert.That(keeperTexture,Is.Not.Null);
            Assert.That(keeperTexture.isReadable,Is.False);
            Assert.That(keeperTexture.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(Mathf.IsPowerOfTwo(keeperTexture.width)&&Mathf.IsPowerOfTwo(keeperTexture.height),Is.True);
            Mesh keeperMesh=null;
            Material keeperVariant=null;
            int keeperBatches=0,otherBatches=0;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.sharedMaterial.mainTexture!=keeperTexture){otherBatches++;continue;}
                keeperBatches++;
                keeperVariant=renderer.sharedMaterial;
                keeperMesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                Assert.That(renderer.sortingOrder,Is.EqualTo(-6),"The shared guardian rank remains below actors.");
                Assert.That(keeperVariant.shader.name,Is.EqualTo("Cryptforge/Foundry Cutaway"));
                Assert.That(keeperVariant,Is.Not.SameAs(keeper),"The scene owns its cutaway variant, not the imported material.");
                Assert.That(keeperMesh.vertexCount,Is.EqualTo(8),"Two guardian cards share one batch.");
                Assert.That(keeperMesh.triangles.Length,Is.EqualTo(12));
                var keeperFlags=new System.Collections.Generic.List<Vector2>();
                keeperMesh.GetUVs(1,keeperFlags);
                Assert.That(keeperFlags.Count,Is.EqualTo(keeperMesh.vertexCount));
                foreach(var flag in keeperFlags)
                    Assert.That(flag,Is.EqualTo(Vector2.one),"Every guardian vertex writes depth and supports hero cutaway.");
                var keeperPoints=keeperMesh.vertices;
                var keeperUvs=keeperMesh.uv;
                for(int side=0;side<2;side++)
                {
                    int first=side*4;
                    // The complete card is outboard of the bridge's walking width, including its transparent margin.
                    float sign=Mathf.Sign(keeperPoints[first].x);
                    for(int corner=0;corner<4;corner++)
                    {
                        var point=keeperPoints[first+corner];
                        Assert.That(Mathf.Sign(point.x),Is.EqualTo(sign));
                        Assert.That(Mathf.Abs(point.x),Is.GreaterThan(4f*Cryptforge.Art.FoundryMap.Axis));
                        Assert.That(point.z,Is.GreaterThan(0f));
                    }
                    Assert.That(keeperUvs[first],Is.EqualTo(Vector2.zero));
                    Assert.That(keeperUvs[first+1],Is.EqualTo(Vector2.right));
                    Assert.That(keeperUvs[first+2],Is.EqualTo(Vector2.one));
                    Assert.That(keeperUvs[first+3],Is.EqualTo(Vector2.up),"Both cards preserve the source's baked lighting orientation.");
                }
            }
            Assert.That(keeperBatches,Is.EqualTo(1),"The guardian pair adds exactly one shared batch.");
            Assert.That(otherBatches,Is.LessThanOrEqualTo(17),"The pre-existing architecture retains its renderer budget.");
            Assert.That(arena.Region.GetComponentsInChildren<MeshRenderer>().Length,Is.LessThanOrEqualTo(18),
                "The approved guardian material adds one renderer to the previous 17-renderer budget.");


            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                Assert.That(renderer.sortingOrder,Is.InRange(-18,-1),"Scenery must remain above the cavern and below combatants.");

            foreach(var post in FoundryRegionView.BridgeHeadPosts)
                for(int u=0;u<=14;u++)for(int v=0;v<=14;v++)
                {
                    Cryptforge.Art.FoundryMap.ToFloor(post.x-.7f+u*.1f,post.y-.7f+v*.1f,out float x,out float y);
                    Assert.That(Cryptforge.Art.FoundryMap.Contains(x,y,0),Is.False,"Guardian plinths stay outside the complete walking union.");
                }
            var visibility=arena.Region.GetComponent<FoundryVisibilityView>();
            // The gate's fire baskets hang on posts the balustrade places for itself: a rail change must not leave one
            // floating over a span or bare void.
            var regionView=arena.Region.GetComponent<FoundryRegionView>();
            foreach(float v in FoundryRegionView.GateBrazierPosts)
            {
                bool held=false;
                foreach(var post in regionView.RailPostBoxes)
                    if(FoundryRegionView.GateBrazierU>=post.x&&FoundryRegionView.GateBrazierU<=post.z&&v>=post.y&&v<=post.w)held=true;
                Assert.That(held,Is.True,"A balustrade post holds the gate brazier at v "+v+".");
            }
            Assert.That(visibility,Is.Not.Null);
            var ownedMaterials=new System.Collections.Generic.HashSet<Material>();
            int floorDecals=0;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                // Flush floor decals are the walking surface: the cutaway never takes them, and they sort at the paving's
                // rank, drawn after it, so raised structures at -14 (bridge coping, block sides) paint over them.
                if(renderer.name==FoundryRegionView.FloorDecalLayer)
                {
                    floorDecals++;
                    Assert.That(renderer.sharedMaterial.shader.name,Is.Not.EqualTo("Cryptforge/Foundry Cutaway"),
                        "Brass inlays must not vanish around the hero's feet.");
                    Assert.That(renderer.sortingOrder,Is.EqualTo(-15),"Decals sort at the paving's rank, under every raised structure.");
                    foreach(var other in arena.Region.GetComponentsInChildren<MeshRenderer>())
                        if(other!=renderer&&other.sortingOrder==renderer.sortingOrder)
                            Assert.That(renderer.bounds.center.y,Is.LessThan(other.bounds.center.y),"Decals draw after the paving under them.");
                    continue;
                }
                if(renderer.sortingOrder < -14) continue;
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
            // The atmosphere is a sibling of the region, so the region's colour check above never covered it.
            var tones=new System.Collections.Generic.List<Color>();atmosphereMesh.GetColors(tones);
            foreach(var c in tones)
            {
                Assert.That(Mathf.Max(c.r,Mathf.Max(c.g,c.b)),Is.LessThan(2f),"Atmosphere colour beyond the shader's range.");
                Assert.That(Mathf.Min(c.r,Mathf.Min(c.g,c.b)),Is.GreaterThanOrEqualTo(0f),"Negative atmosphere colour.");
            }
            // The pit's sparks and haze are depth-tested against the collar (behind the near pieces and the domes, in front
            // of the far collar), and are born only inside the collar's opening, never over walkable court.
            int pitVertices=0;
            foreach(var p in atmosphereMesh.vertices)
                if(p.z!=0){pitVertices++;Assert.That(p.z,Is.InRange(FoundryDepth.Z(-.6f),FoundryDepth.Z(.09f)));}
            Assert.That(pitVertices,Is.GreaterThan(0),"The furnace pit has sparks and haze.");
            for(int spark=0;spark<FoundryAtmosphereView.SparkCount;spark++)
                for(int life=0;life<200;life++)
                {
                    Vector2 at=FoundryAtmosphereView.SparkSpawn(spark,life);
                    // Distance to the rounded-square opening, not its inscribed circle:
                    // the enlarged pit has long straight sides around the corner arcs.
                    Vector2 corner=new Vector2(Mathf.Max(0,Mathf.Abs(at.x+10)-2.49f),Mathf.Max(0,Mathf.Abs(at.y-8)-2.49f));
                    Assert.That(corner.magnitude,Is.LessThan(2.96f),"Sparks are born inside the collar's opening.");
                    Assert.That(Cryptforge.Art.FoundryMap.Cell(Mathf.FloorToInt(at.x),Mathf.FloorToInt(at.y)),Is.False,"Never over walkable court.");
                }


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
            Assert.That(keeperMesh==null,Is.True,"Reload releases the guardian mesh.");
            Assert.That(keeperVariant==null,Is.True,"Reload releases the guardian cutaway material.");
            Assert.That(keeper!=null&&keeperTexture!=null,Is.True,"Shared keeper assets survive reload.");
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
