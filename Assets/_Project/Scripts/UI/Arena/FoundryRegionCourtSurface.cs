using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // The medallion belongs to the deeper entrance court. This local design
        // contract never moves the walking floor, pit or global SealPolar helper.
        private static readonly Vector2 CourtSealCentre=new Vector2(-3,-5);
        private const float CourtStoneRadius=5.05f,CourtFieldRadius=3.75f,CourtMetalRadius=4.77f;
        private static Vector2 CourtSealPoint(float angle,float radius)=>CourtSealCentre+SealPolar(angle,radius);

        // Complete broad stone settings connect the entrance and gate to the
        // court. Their material stays quiet; fitted cross-joints carry the route.
        private static readonly Vector2[] CourtProcessionalEnds=
            {new Vector2(-17.5f,-17.5f),new Vector2(15.9f,5)};
        private const float CourtProcessionalHalfWidth=1.85f,CourtProcessionalStoneWidth=.48f;
        private static void BuildCourtProcessionalStone(Builder mesh)
        {
            foreach(Vector2 end in CourtProcessionalEnds)
            {
                Vector2 direction=(end-CourtSealCentre).normalized;
                Vector2 side=new Vector2(-direction.y,direction.x);
                float length=Vector2.Distance(CourtSealCentre,end)-CourtPathStart;
                int rows=Mathf.CeilToInt(length/3.2f);
                for(int row=0;row<rows;row++)
                {
                    Vector2 a=CourtSealCentre+direction*(CourtPathStart+length*row/rows);
                    Vector2 b=CourtSealCentre+direction*(CourtPathStart+length*(row+1)/rows);
                    // Alternate broad cross-flags with a fitted pair. The offset
                    // joint does not recreate the surrounding axis-aligned grid.
                    float split=row%2==0?CourtPathHalfWidth:(row%4==1?.48f:-.48f);
                    CourtFittedFlag(mesh,new[]{a-side*CourtPathHalfWidth,b-side*CourtPathHalfWidth,b+side*split,a+side*split});
                    if(row%2==1)
                        CourtFittedFlag(mesh,new[]{a+side*split,b+side*split,b+side*CourtPathHalfWidth,a+side*CourtPathHalfWidth});
                }
            }
        }

        // Continue the original court approach across the bridge. The inner edge
        // is the exact existing rotated path cap; the far edge lies across the
        // physical entrance threshold. This small taper avoids a kinked overlap
        // or a second ribbon beside the unchanged brass lanes.
        private static Vector2[] CourtBridgeContour()
        {
            Vector2[] approach=CourtPathContour(CourtProcessionalEnds[0]);
            float threshold=Cryptforge.Art.FoundryMap.BridgeStart+Kerb*1.41421356f;
            float half=CourtPathHalfWidth*1.41421356f;
            return new[]{approach[1],DeckDesign(threshold,-half),
                DeckDesign(threshold,half),approach[2]};
        }

        private static void BuildCourtBridgeStone(Builder mesh)
        {
            Vector2[] field=CourtBridgeContour();
            float length=Vector2.Distance((field[0]+field[3])*.5f,(field[1]+field[2])*.5f);
            int rows=Mathf.CeilToInt(length/3.2f);
            for(int row=0;row<rows;row++)
            {
                float from=row/(float)rows,to=(row+1)/(float)rows;
                Vector2 leftA=Vector2.Lerp(field[0],field[1],from),leftB=Vector2.Lerp(field[0],field[1],to);
                Vector2 rightA=Vector2.Lerp(field[3],field[2],from),rightB=Vector2.Lerp(field[3],field[2],to);
                // Broad transverse flags alternate with a fitted pair. Each row
                // shares its complete boundary with the next, including mortar.
                // Both end courses remain whole bearing stones.
                if(row%2==0||row==rows-1)
                    CourtFittedFlag(mesh,new[]{leftA,leftB,rightB,rightA});
                else
                {
                    float split=row%4==1?.39f:.61f;
                    Vector2 middleA=Vector2.Lerp(leftA,rightA,split),middleB=Vector2.Lerp(leftB,rightB,split);
                    CourtFittedFlag(mesh,new[]{leftA,leftB,middleB,middleA});
                    CourtFittedFlag(mesh,new[]{middleA,middleB,rightB,rightA});
                }
            }
        }

    }
}
