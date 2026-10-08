using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundrySurfaceTests
    {
        [Test] public void ProjectionRoundTripsAcrossBothRampSeams()
        {
            for (float u = 24; u <= 34; u += .125f)
                for (float v = -4; v <= 14; v += .5f)
                {
                    Vector3 p = FoundrySurface.Project(u, v);
                    FoundrySurface.ToDesign(p.x, p.y, out float actualU, out float actualV);
                    Assert.That(actualU, Is.EqualTo(u).Within(.00002f));
                    Assert.That(actualV, Is.EqualTo(v).Within(.00002f));
                }
        }
        [Test] public void SurfaceIsContinuousAndLeavesTheCourtAndNavigationUntouched()
        {
            foreach (float u in new[] { -30f, 0, 14, 27, 28 })
            {
                FoundryMap.ToFloor(u, 5, out float x, out float y);
                Assert.That(FoundrySurface.Project(u, 5), Is.EqualTo(new Vector3(x, y * .5f, 0)));
            }
            foreach (float seam in new[] { FoundrySurface.RampStart, FoundrySurface.RampEnd })
                Assert.That(Vector3.Distance(FoundrySurface.Project(seam - .0001f, 5),
                    FoundrySurface.Project(seam + .0001f, 5)), Is.LessThan(.0004f));
            FoundryMap.ToFloor(31, 5, out float fx, out float fy);
            Assert.That(FoundryMap.Contains(fx, fy, .4f), Is.True);
            var origin = new Vector3(4, -7, 2);
            Assert.That(FoundrySurface.Lift(new Vector3(fx, fy * .5f, 0) + origin, origin),
                Is.EqualTo(FoundrySurface.Rise).Within(.00001f));
        }
    }
}
