using System.Collections;
using System.Collections.Generic;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class FoundryFlameMotionTests
    {
        [UnityTest]
        public IEnumerator OnlyTipsMoveAndPauseDisablePreserveDepthAndBounds()
        {
            var host=new GameObject("Flame lifecycle test");
            var mesh=new Mesh();
            float previous=Time.timeScale;
            var rest=new[]{new Vector3(0,0,3),new Vector3(1,0,4),new Vector3(1,1,4),new Vector3(0,1,3)};
            mesh.vertices=rest;mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
            var motion=host.AddComponent<FoundryFlameMotion>();
            motion.Initialize(mesh,new List<FoundryFlameVertex>{new FoundryFlameVertex(2,1.55f,1),new FoundryFlameVertex(3,1.55f,2)});
            var padded=mesh.bounds;
            try
            {
                Time.timeScale=1;
                yield return new WaitForSeconds(.15f);
                var live=mesh.vertices;
                Assert.That(live[0],Is.EqualTo(rest[0]),"Coal/base vertices must stay fixed.");
                Assert.That(live[1],Is.EqualTo(rest[1]));
                Assert.That(live[2],Is.Not.EqualTo(rest[2]));
                for(int i=0;i<live.Length;i++)
                {
                    Assert.That(live[i].z,Is.EqualTo(rest[i].z),"Structure depth cannot drift with fire.");
                    Assert.That(padded.Contains(live[i]),Is.True);
                }
                Time.timeScale=0;
                yield return new WaitForSecondsRealtime(.12f);
                Assert.That(mesh.vertices,Is.EqualTo(live),"Paused combat must freeze fire.");
                motion.enabled=false;
                Assert.That(mesh.vertices,Is.EqualTo(rest));
                Assert.That(mesh.bounds,Is.EqualTo(padded));
                motion.enabled=true;Time.timeScale=1;
                yield return new WaitForSeconds(.12f);
                Assert.That(mesh.vertices[2],Is.Not.EqualTo(rest[2]));
                Assert.That(mesh.vertices[0],Is.EqualTo(rest[0]));
                Assert.That(mesh.bounds,Is.EqualTo(padded));
            }
            finally { Time.timeScale=previous;Object.DestroyImmediate(host);Object.DestroyImmediate(mesh); }
        }
    }
}
