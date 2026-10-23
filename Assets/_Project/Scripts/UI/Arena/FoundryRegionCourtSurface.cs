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

        // The processional ways connect the entrance and gate to the court between
        // their brass lane lines. As on the concept they are laid in smaller cut
        // stone than the court's, its courses turned across the way: the laid
        // stone's raster, on its light lattice only, in a field that runs with
        // each way (texture u across it, v along it, from the seal's centre) at
        // CourtWayScale the court's density - the way to the bridge in the bridge's
        // own field (BridgeField), its axis 4 degrees off the way's, so it runs on
        // into the deck without a seam. Built after CourtFlagstoneField, which would
        // otherwise give it the court's world field.
        private static readonly Vector2[] CourtProcessionalEnds=
            {new Vector2(-17.5f,-17.5f),new Vector2(15.9f,5)};
        private const float CourtProcessionalHalfWidth=1.85f,CourtProcessionalStoneWidth=.48f,CourtWayScale=1.35f;
        private static void BuildCourtProcessionalWay(Builder flags)
        {
            foreach(Vector2 end in CourtProcessionalEnds)
            {
                Vector2 direction=(end-CourtSealCentre).normalized;
                Vector2 side=new Vector2(-direction.y,direction.x);
                int first=flags.V.Count;
                CourtFittedFace(flags,CourtPathContour(end),1,Color.white);
                for(int i=first;i<flags.V.Count;i++)
                {
                    FoundrySurface.ToDesign(flags.V[i].x,flags.V[i].y,out float u,out float v);
                    Vector2 p=new Vector2(u,v)-CourtSealCentre;
                    flags.U[i]=end==CourtProcessionalEnds[0]?BridgeField(u,v):
                        new Vector2(Vector2.Dot(p,side),Vector2.Dot(p,direction))*CourtWayScale/FlagstoneSpan;
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
    }
}
