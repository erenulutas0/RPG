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

        // Two paired dressed-stone settings relate the entrance and gate to the
        // same court centre. Their plain open middles remain ordinary battle
        // paving; there is no enclosing target, raised kerb or luminous route.
        private static readonly Vector2[] CourtProcessionalEnds=
            {new Vector2(-17.5f,-17.5f),new Vector2(15.9f,5)};
        private const float CourtProcessionalHalfWidth=1.85f,CourtProcessionalStoneWidth=.48f;
        private static void BuildCourtProcessionalStone(Builder mesh)
        {
            foreach(Vector2 end in CourtProcessionalEnds)
            {
                Vector2 direction=(end-CourtSealCentre).normalized;
                Vector2 side=new Vector2(-direction.y,direction.x);
                // All four setting courses stop outside the stone circle. Even
                // their nearest inside corner retains a small plain-stone joint.
                float start=Mathf.Sqrt((CourtStoneRadius+.16f)*(CourtStoneRadius+.16f)-
                    (CourtProcessionalHalfWidth-CourtProcessionalStoneWidth*.5f)*
                    (CourtProcessionalHalfWidth-CourtProcessionalStoneWidth*.5f));
                float length=Vector2.Distance(CourtSealCentre,end)-start;
                int blocks=Mathf.CeilToInt(length/2.2f);
                foreach(float sign in new[]{-1f,1f})
                {
                    Vector2 offset=side*(sign*CourtProcessionalHalfWidth);
                    for(int k=0;k<blocks;k++)
                    {
                        Vector2 a=CourtSealCentre+direction*(start+length*k/blocks)+offset;
                        Vector2 b=CourtSealCentre+direction*(start+length*(k+1)/blocks)+offset;
                        Vector2 half=side*(CourtProcessionalStoneWidth*.5f);
                        var bed=new[]{a-half,b-half,b+half,a+half};
                        CeremonialStoneFace(mesh,bed,.86f);
                        // Shared floor grain/light, with short cross-joints in
                        // broad blocks rather than another unbroken gold stripe.
                        Vector2 inset=direction*.025f,edge=side*.022f;
                        CourtDressedStone(mesh,new[]{a+inset-half+edge,b-inset-half+edge,
                            b-inset+half-edge,a+inset+half-edge});
                    }
                }
            }
        }

        // The circular medallion is laid from the court's own stone. Its dressed
        // shoulders are a partition of each original face, never a raised decal
        // or an extra outline laid on top of the brass compass.
        private static void CourtDressedStone(Builder mesh,Vector2[] polygon)
        {
            Vector2[] outer=CourtStoneContour(polygon);
            if(outer.Length<3)return;
            Vector2 centre=Vector2.zero;
            foreach(Vector2 p in outer)centre+=p;
            centre/=outer.Length;
            float mineral=FloorPatina(centre.x*.16f+5.7f,centre.y*.16f-9.4f);
            float tone=Mathf.Lerp(.96f,1.03f,mineral);
            Color family=PavingMineralFamily(centre.x,centre.y);
            // True inset intersections preserve curved fitted ends and radial
            // wedges. The shared inset helper bounds tiny chamfer retreats.
            Vector2[] inner=InsetFlagContour(outer,.046f);
            int first=mesh.V.Count,points=0;
            for(int k=0;k<outer.Length;k++)
            {
                int next=(k+1)%outer.Length;
                int count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(outer[k],outer[next])/.5f));
                Vector2 edge=(outer[next]-outer[k]).normalized;
                float facing=Mathf.Clamp01((edge.x+edge.y)*.5f+.5f);
                for(int j=0;j<count;j++)
                {
                    float t=j/(float)count;
                    Vector2 a=Vector2.Lerp(outer[k],outer[next],t),b=Vector2.Lerp(inner[k],inner[next],t);
                    float rubbed=FloorPatina(a.x*1.7f+3,a.y*1.7f-7)*.55f;
                    float edgeTone=tone*Mathf.Lerp(Mathf.Lerp(.88f,1.065f,facing),1,rubbed);
                    PavingVertex(mesh,a.x,a.y,edgeTone,family,FloorLight(a.x,a.y));
                    PavingVertex(mesh,b.x,b.y,edgeTone,family,FloorLight(b.x,b.y));
                    points++;
                }
            }
            for(int k=0;k<points;k++)
            {
                int a=first+k*2,b=first+((k+1)%points)*2;
                mesh.T.Add(a);mesh.T.Add(b);mesh.T.Add(b+1);
                mesh.T.Add(a);mesh.T.Add(b+1);mesh.T.Add(a+1);
            }
            // Preserve the existing half-unit floor-light lattice and world UVs.
            // Exchange only this face's former ceremonial tint for the same
            // mineral family used by the immediately adjacent court flags.
            int face=mesh.C.Count;
            CeremonialStoneFace(mesh,inner,tone);
            for(int i=face;i<mesh.C.Count;i++)
            {
                Color c=mesh.C[i];
                mesh.C[i]=new Color(c.r*family.r/1.025f,c.g*family.g/1.015f,c.b*family.b/.99f,1);
            }
        }

        private static Vector2[] CourtStoneContour(Vector2[] polygon)
        {
            // Cell clipping can repeat an exact point or leave a collinear one.
            // Remove these before a geometric inset; the footprint is identical.
            var points=new List<Vector2>();
            foreach(Vector2 p in polygon)
                if(points.Count==0||(p-points[points.Count-1]).sqrMagnitude>1e-12f)points.Add(p);
            if(points.Count>1&&(points[0]-points[points.Count-1]).sqrMagnitude<=1e-12f)points.RemoveAt(points.Count-1);
            bool changed=true;
            while(changed&&points.Count>=3)
            {
                changed=false;
                for(int i=0;i<points.Count;i++)
                {
                    Vector2 a=points[(i+points.Count-1)%points.Count],b=points[i],c=points[(i+1)%points.Count];
                    if(Mathf.Abs(FlagCross(b-a,c-b))>1e-7f)continue;
                    points.RemoveAt(i);changed=true;break;
                }
            }
            float area=0;
            for(int i=0;i<points.Count;i++)area+=FlagCross(points[i],points[(i+1)%points.Count]);
            if(area<0)points.Reverse();
            return points.ToArray();
        }
    }
}
