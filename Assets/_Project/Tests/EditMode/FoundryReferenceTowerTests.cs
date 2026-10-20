using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryReferenceTowerTests
    {
        // Sources: Tools/ArtReview/BuildBlenderReferenceTowers.py and BuildBlenderReferenceRetorts.py,
        // packed losslessly by PackReferenceTowerAtlas.py / PackReferenceRetortAtlas.py
        // (evidence ArtDirection/2026-10-20/reference-towers-01 and reference-retorts-01).
        // Upper rows cover each tower above its retained stone cornice. Luma limits sit below the
        // rejected light-iron tower render (~61) and the former blue retort hoods (~60-62).
        [TestCase("Assets/_Project/Resources/FoundryBoiler.png",110,430,56f,150)]
        [TestCase("Assets/_Project/Resources/FoundryShortBoiler.png",200,520,59f,250)]
        public void FurnaceAtlasCarriesDarkIronTowersWithLitWindows(string Atlas,int top,int cornice,float maximumLuma,int minimumWindows)
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
                        if(row<top||row>=cornice||c.a<=200)continue;
                        upper++;luma+=.299*c.r+.587*c.g+.114*c.b;blueOverRed+=c.b-c.r;
                        if(c.r>170&&c.g<150)warm++;
                    }
                    Assert.That(minX,Is.GreaterThanOrEqualTo(32));Assert.That(maxX,Is.LessThanOrEqualTo(992),"Mip-safe margins between the two variants.");
                    Assert.That(minRow,Is.GreaterThan(0));Assert.That(maxRow,Is.LessThan(1023));
                    Assert.That(upper,Is.GreaterThan(20000),"The upper tower is a substantial mass, not a thin stack.");
                    TestContext.WriteLine($"{Path.GetFileName(Atlas)} tile {tile}: upper luma {luma/upper:F1}, blue-red {blueOverRed/upper:F1}, lit window pixels {warm}.");
                    Assert.That(luma/upper,Is.LessThan(maximumLuma),"Blackened iron like the concept's towers.");
                    Assert.That(blueOverRed/upper,Is.LessThan(2),"Neutral-warm iron; the former forged blue read teal in the court light.");
                    Assert.That(warm,Is.GreaterThan(minimumWindows),"Rings of small glowing windows, as in the concept (the flat retort hood had 149).");
                }
            }
            finally{Object.DestroyImmediate(texture);}
        }
    }
}
