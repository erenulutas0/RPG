using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryReferenceTowerTests
    {
        // Source: Tools/ArtReview/BuildBlenderReferenceTowers.py, packed losslessly by
        // Tools/ArtReview/PackReferenceTowerAtlas.py (evidence ArtDirection/2026-10-20/reference-towers-01).
        const string Atlas="Assets/_Project/Resources/FoundryBoiler.png";

        [Test]
        public void TallFurnaceAtlasCarriesDarkIronTowersWithLitWindows()
        {
            var texture=new Texture2D(2,2);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(Atlas)),Is.True);
                Assert.That(texture.width,Is.EqualTo(2048));Assert.That(texture.height,Is.EqualTo(1024));
                Color32[] p=texture.GetPixels32();
                for(int tile=0;tile<2;tile++)
                {
                    int minX=int.MaxValue,maxX=-1,minRow=int.MaxValue,maxRow=-1;
                    double luma=0;int upper=0,warm=0;double blueOverRed=0;
                    for(int y=0;y<1024;y++)for(int x=0;x<1024;x++)
                    {
                        Color32 c=p[y*2048+tile*1024+x];
                        if(c.a<=8)continue;
                        int row=1023-y;
                        minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minRow=Mathf.Min(minRow,row);maxRow=Mathf.Max(maxRow,row);
                        // Upper tower, above the retained stone cornice of the foundation.
                        if(row<110||row>=430||c.a<=200)continue;
                        upper++;luma+=.299*c.r+.587*c.g+.114*c.b;blueOverRed+=c.b-c.r;
                        if(c.r>170&&c.g<150)warm++;
                    }
                    Assert.That(minX,Is.GreaterThanOrEqualTo(32));Assert.That(maxX,Is.LessThanOrEqualTo(992),"Mip-safe margins between the two variants.");
                    Assert.That(minRow,Is.GreaterThan(0));Assert.That(maxRow,Is.LessThan(1023));
                    Assert.That(upper,Is.GreaterThan(20000),"The upper tower is a substantial mass, not a thin stack.");
                    TestContext.WriteLine($"Tile {tile}: upper luma {luma/upper:F1}, blue-red {blueOverRed/upper:F1}, lit window pixels {warm}.");
                    Assert.That(luma/upper,Is.LessThan(56),"Blackened iron like the concept's towers (the rejected light-iron render measured ~61).");
                    Assert.That(blueOverRed/upper,Is.LessThan(2),"Neutral-warm iron; the former forged blue read teal in the court light.");
                    Assert.That(warm,Is.GreaterThan(150),"Rings of small glowing windows, as in the concept.");
                }
            }
            finally{Object.DestroyImmediate(texture);}
        }
    }
}
