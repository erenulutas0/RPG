using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // An embedded freight-transfer bay ties the salvage edge to the north stock
        // station. All pieces are flush paving/inlays, never sleepers, a raised cart,
        // a collision obstacle or a new interaction. Ordinary movement crosses them.
        private void BuildProductionYard(ArenaView arena)
        {
            var bed=new Builder();
            Vector2 centre=new Vector2(-24,7);
            var plate=new Color(.73f,.75f,.80f,.52f);
            var recess=new Color(.59f,.60f,.65f,.72f);
            var rail=new Color(.59f,.55f,.46f,.86f);

            // The working lane keeps a stone floor between its rails. Broad shoulders
            // at the loading edge distinguish this approach from the ceremonial brass.
            YardPanel(bed,new Vector2(-29.65f,6.02f),new Vector2(-27.55f,7.98f),plate);
            YardPanel(bed,new Vector2(-24.98f,10.50f),new Vector2(-23.02f,17.65f),
                new Color(.77f,.78f,.82f,.30f));

            const int segments=48;
            const float inner=3.22f,outer=3.7f;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                // Radial iron bed is deliberately plain: no compass rays, magical
                // glyph, luminous perimeter or closed black disc under the actors.
                SealQuad(bed,centre+SealPolar(a,inner),centre+SealPolar(b,inner),
                    centre+SealPolar(b,outer),centre+SealPolar(a,outer),plate,SealStone);
                Vector2 p=centre+SealPolar(a,3.48f),q=centre+SealPolar(b,3.48f);
                YardRail(bed,p,q,recess,rail,i);
                if(i%6==0)
                {
                    p=centre+SealPolar(a,inner);q=centre+SealPolar(a,outer);
                    Strip(bed,p.x,p.y,q.x,q.y,.065f,recess,CopingTrim.Stone,i);
                }
            }

            // A long transfer table inset into the circular bed: six broad plates,
            // with a straight through-track that can be understood at portrait scale.
            for(int i=0;i<6;i++)
            {
                float u=-27.43f+i*(6.86f/6);
                YardPanel(bed,new Vector2(u+.025f,6.08f),new Vector2(u+6.86f/6-.025f,7.92f),
                    new Color(.62f,.66f,.73f,.63f));
            }
            // Match the two rail gauges at their radial junctions. The north approach
            // ends in a flush service plate before the existing non-walkable shelf.
            foreach(float v in new[]{6.4f,7.6f})
                YardRail(bed,new Vector2(-29.65f,v),new Vector2(-20.57f,v),recess,rail,3);
            foreach(float u in new[]{-24.6f,-23.4f})
                YardRail(bed,new Vector2(u,10.43f),new Vector2(u,17.42f),recess,rail,7);
            YardPanel(bed,new Vector2(-24.98f,17.42f),new Vector2(-23.02f,17.68f),plate);
            // Flat pivot cover; its small rectangular outline is mechanical rather
            // than another decorated medallion competing with the central court seal.
            YardPanel(bed,centre-new Vector2(.34f,.26f),centre+new Vector2(.34f,.26f),
                new Color(.7f,.68f,.63f,.6f));
            Create("Embedded freight transfer yard",bed,arena.PlatformMaterials.Coping,FloorDecal);
        }

        private static void YardPanel(Builder mesh,Vector2 min,Vector2 max,Color tone)
        {
            // Subdivide long plates for the same local floor-light sampling as inlays.
            int alongU=Mathf.Max(1,Mathf.CeilToInt(max.x-min.x));
            int alongV=Mathf.Max(1,Mathf.CeilToInt(max.y-min.y));
            for(int u=0;u<alongU;u++)for(int v=0;v<alongV;v++)
            {
                float u0=Mathf.Lerp(min.x,max.x,u/(float)alongU),u1=Mathf.Lerp(min.x,max.x,(u+1f)/alongU);
                float v0=Mathf.Lerp(min.y,max.y,v/(float)alongV),v1=Mathf.Lerp(min.y,max.y,(v+1f)/alongV);
                SealQuad(mesh,new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u1,v1),new Vector2(u0,v1),tone,SealStone);
            }
        }

        private static void YardRail(Builder mesh,Vector2 a,Vector2 b,Color groove,Color rail,int seed)
        {
            Strip(mesh,a.x,a.y,b.x,b.y,.23f,groove,CopingTrim.Stone,seed);
            Strip(mesh,a.x,a.y,b.x,b.y,.09f,rail,CopingTrim.Brass,seed);
        }
    }
}
