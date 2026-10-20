using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // The concept divides the laid court with one worn bronze network: an
        // octagon setting around the seal, spokes to the rim and paired lane lines
        // beside both processional ways. Each line is the seal's construction - a
        // stone channel with a thin metal course - and is clipped to open walking
        // floor, so it never runs into the pit, under equipment or past the kerb.
        internal const float CourtInlayRadius=7f;
        private const float CourtInlayStep=.25f,CourtInlayClearance=.55f;
        // Spokes along the processional ways are replaced by their lane lines.
        private static readonly int[] CourtInlaySpokes={0,1,2,3,4,6,7};

        private void BuildCourtInlayNetwork(ArenaView arena)
        {
            var inlay=new Builder();
            BuildCourtInlayGeometry(inlay);
            Create("Court bronze inlay network",inlay,arena.PlatformMaterials.Coping,FloorDecal);
        }

        private static void BuildCourtInlayGeometry(Builder inlay)
        {
            var octagon=new Vector2[8];
            for(int k=0;k<8;k++)octagon[k]=CourtSealCentre+new Vector2(Mathf.Cos(k*Mathf.PI/4),Mathf.Sin(k*Mathf.PI/4))*CourtInlayRadius;
            int seed=0;
            for(int k=0;k<8;k++)CourtInlayRun(inlay,octagon[k],octagon[(k+1)%8],false,ref seed);
            foreach(int k in CourtInlaySpokes)
            {
                Vector2 direction=(octagon[k]-CourtSealCentre).normalized;
                CourtInlayRun(inlay,octagon[k],octagon[k]+direction*40,true,ref seed);
            }
            foreach(Vector2 end in CourtProcessionalEnds)
            {
                Vector2 direction=(end-CourtSealCentre).normalized,side=new Vector2(-direction.y,direction.x);
                foreach(float sign in new[]{-1f,1f})
                {
                    Vector2 offset=side*sign*(CourtPathHalfWidth+.16f);
                    // Begin where the lane line leaves the octagon setting.
                    float t=CourtPathStart;
                    while(t<CourtInlayRadius+2&&InsideCourtOctagon(CourtSealCentre+direction*t+offset,octagon))t+=CourtInlayStep*.5f;
                    CourtInlayRun(inlay,CourtSealCentre+direction*t+offset,end+offset,false,ref seed);
                }
            }
            WeatherHeraldry(inlay);
        }

        // Emits the open stretches of a..b. A spoke (firstOnly) is one stretch from
        // its octagon vertex to the first rim, pit or equipment boundary.
        private static void CourtInlayRun(Builder inlay,Vector2 a,Vector2 b,bool firstOnly,ref int seed)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/CourtInlayStep));
            int start=-1;
            for(int i=0;i<=steps+1;i++)
            {
                bool open=i<=steps&&CourtInlayOpen(Vector2.Lerp(a,b,i/(float)steps));
                if(open)
                {
                    if(start<0)
                    {
                        if(firstOnly&&i>0)return;
                        start=i;
                    }
                    continue;
                }
                if(start<0)continue;
                if(i-1-start>=2)
                    CourtInlayLine(inlay,Vector2.Lerp(a,b,start/(float)steps),Vector2.Lerp(a,b,(i-1)/(float)steps),seed++);
                if(firstOnly)return;
                start=-1;
            }
        }

        private static void CourtInlayLine(Builder inlay,Vector2 p,Vector2 q,int seed)
        {
            var channel=new Color(.84f,.82f,.83f);
            Strip(inlay,p.x,p.y,q.x,q.y,.14f,channel,CopingTrim.Stone,seed);
            Strip(inlay,p.x,p.y,q.x,q.y,.08f,CeremonialBrass,CopingTrim.Brass,seed);
        }

        private static bool CourtInlayOpen(Vector2 design)
        {
            if(Vector2.Distance(design,CourtSealCentre)<CourtStoneRadius+.1f)return false;
            FoundryMap.ToFloor(design.x,design.y,out float x,out float y);
            if(!FoundryMap.Contains(x,y,CourtInlayClearance))return false;
            foreach(Vector2 boiler in BoilerCentres)
                if(Vector2.Distance(design,boiler)<BoilerPlinth+CourtInlayClearance)return false;
            foreach(var lamp in ForgeLamps())
                if(DistanceFromFloorBox(design.x,design.y,lamp.Box.x,lamp.Box.y,lamp.Box.z,lamp.Box.w)<CourtInlayClearance)return false;
            return true;
        }

        private static bool InsideCourtOctagon(Vector2 p,Vector2[] octagon)
        {
            for(int k=0;k<8;k++)
            {
                Vector2 a=octagon[k],b=octagon[(k+1)%8];
                if((b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x)<0)return false;
            }
            return true;
        }
    }
}
