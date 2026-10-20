using System.Collections;
using System.IO;
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
    // The gate's leaves re-lay the painted door exactly while shut and swing outward onto the landing, clear of the gate
    // exit, when open.
    public sealed class FoundryGateDoorTests
    {
        [UnitySetUp] public IEnumerator Begin()
        {
            TestProfile.Begin();
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;Time.timeScale=0;
        }
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;var old=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Gate door cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);TestProfile.End();
        }

        [UnityTest] public IEnumerator ShutLeavesReLayThePaintingAndOpenLeavesFrameTheExit()
        {
            var door=Object.FindFirstObjectByType<FoundryGateDoorMotion>();
            Assert.That(door,Is.Not.Null);Assert.That(door.Opening,Is.Zero);
            var mesh=door.GetComponent<MeshFilter>().sharedMesh;
            var material=door.GetComponent<MeshRenderer>().sharedMaterial;
            Assert.That(material.mainTexture.name,Is.EqualTo("GateFacade"),"The leaves are the facade's own painting.");
            Vector3[] shut=mesh.vertices;
            door.Pose(0f);
            Assert.That(mesh.vertices,Is.EqualTo(shut),"Shut leaves sit exactly where they were built.");
            door.Pose(1f);
            Vector3[] open=mesh.vertices;
            int moved=0;
            FoundryMap.GateExitSpot(out float ex,out float ey);
            FoundryMap.ToDesign(ex,ey,out float eu,out float ev);
            for(int i=0;i<open.Length;i++)
            {
                if(open[i]==shut[i])continue;
                moved++;
                Assert.That(open[i].z,Is.EqualTo(shut[i].z),"Structure depth stays authored.");
                // Read the design position back without the vertex's height above the landing: the shut vertex tells
                // which of the leaf's two heights it carries.
                float height=0;
                foreach(float candidate in new[]{FoundryRegionView.GateDoorBottomV*6.5f,FoundryRegionView.GateDoorTopV*6.5f})
                {
                    FoundrySurface.ToDesign(shut[i].x,shut[i].y-candidate,out float su,out _);
                    if(Mathf.Abs(su-FoundryRegionView.GateDoorFaceU)<.01f)height=candidate;
                }
                Assert.That(height,Is.GreaterThan(0f),"Every moving vertex belongs to a shut leaf on the facade plane.");
                FoundrySurface.ToDesign(open[i].x,open[i].y-height,out float u,out float v);
                Assert.That(u,Is.LessThanOrEqualTo(FoundryRegionView.GateDoorFaceU+.001f),"Leaves open outward, never through the wall.");
                Assert.That(u,Is.GreaterThan(FoundryMap.GateLandingStart),"Open leaves stay on the landing.");
            }
            Assert.That(moved,Is.EqualTo(4),"Each leaf's free edge (bottom and top) and nothing else swings; hinges stay.");
            door.Pose(0f);
            Assert.That(mesh.vertices,Is.EqualTo(shut));
            LogAssert.NoUnexpectedReceived();
            yield return null;
        }
    }
}
