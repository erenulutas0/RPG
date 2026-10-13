using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private static void BuildGateButtressLower(Builder stone,Vector4 foot)
        {
            GateButtressTier(stone,foot,0,.48f,.08f,.94f);
            GateButtressTier(stone,new Vector4(foot.x+.11f,foot.y+.09f,foot.z-.11f,foot.w-.09f),
                .48f,2.05f,.09f,.90f);
            GateButtressTier(stone,new Vector4(foot.x+.20f,foot.y+.18f,foot.z-.20f,foot.w-.18f),
                2.05f,2.85f,.28f,.94f);
        }

        private static void GateButtressTier(Builder stone,Vector4 box,float bottom,
            float top,float inset,float shade)
        {
            var shell=new Builder();
            LitFoundryTier(shell,box.x,box.y,box.z,box.w,bottom,top,inset);
            // Replace each visible wall, rather than placing course quads over a
            // retained complete face. Mortar, arrises and stone interiors partition
            // the original tapered surface and preserve its sampled forge light.
            for(int face=0;face<3;face++)
            {
                int first=face*4,start=stone.V.Count;
                var points=new[]{shell.V[first],shell.V[first+1],shell.V[first+2],shell.V[first+3]};
                var light=new[]{shell.C[first],shell.C[first+1],shell.C[first+2],shell.C[first+3]};
                float width=Vector3.Distance(points[0],points[1]);
                if(face==2)
                    GateButtressPatch(stone,points,light,Vector2.zero,Vector2.right,Vector2.one,Vector2.up,shade*.91f);
                else
                {
                    int rows=Mathf.Max(1,Mathf.RoundToInt((top-bottom)/.82f));
                    int columns=Mathf.Max(1,Mathf.RoundToInt(width/1.25f));
                    for(int row=0;row<rows;row++)
                    {
                        float low=row/(float)rows,high=(row+1f)/rows;
                        float left=row%2==0?0:-.42f/columns;
                        int column=0;
                        while(left<1)
                        {
                            float right=Mathf.Min(1,left+1f/columns),a=Mathf.Max(0,left);
                            if(right>a)
                                GateButtressBlock(stone,points,light,new Vector4(a,low,right,high),
                                    width,top-bottom,shade*(1+((row*3+column)%5-2)*.013f));
                            left+=1f/columns;column++;
                        }
                    }
                }
                // One physical mineral field crosses all stones and their edges.
                // Per-cell UV restarts would make these large blocks look tiled.
                GateRecessGrain(stone,start,stone.V.Count,points[1]-points[0],face);
            }
            // Preserve the exact chamfered bearing. A new tier begins on this
            // cap; its lower footprint is always contained by this actual polygon.
            int cap=stone.V.Count;
            for(int first=12;first<shell.V.Count;first+=4)
            {
                int output=stone.V.Count;
                for(int i=0;i<4;i++)
                {
                    stone.V.Add(shell.V[first+i]);stone.U.Add(shell.U[first+i]);
                    Color c=shell.C[first+i]*shade;c.a=1;stone.C.Add(c);
                }
                stone.T.AddRange(new[]{output,output+1,output+2,output,output+2,output+3});
            }
            GateRecessGrain(stone,cap,stone.V.Count,Project(1,0)-Project(0,0),3);
        }

        private static void GateButtressBlock(Builder stone,Vector3[] face,Color[] light,
            Vector4 cell,float width,float height,float shade)
        {
            // Every row owns its complete cell, including its half-joints. No bed
            // remains under the dressed face, and adjacent cells share only edges.
            float jointX=Mathf.Min(.009f/width,(cell.z-cell.x)*.12f);
            float jointY=Mathf.Min(.009f/height,(cell.w-cell.y)*.12f);
            var outer=new[]{new Vector2(cell.x,cell.y),new Vector2(cell.z,cell.y),
                new Vector2(cell.z,cell.w),new Vector2(cell.x,cell.w)};
            var bed=new Vector4(cell.x+(cell.x>0?jointX:0),cell.y+(cell.y>0?jointY:0),
                cell.z-(cell.z<1?jointX:0),cell.w-(cell.w<1?jointY:0));
            var middle=new[]{new Vector2(bed.x,bed.y),new Vector2(bed.z,bed.y),
                new Vector2(bed.z,bed.w),new Vector2(bed.x,bed.w)};
            float edgeX=Mathf.Min(.035f/width,(bed.z-bed.x)*.12f);
            float edgeY=Mathf.Min(.035f/height,(bed.w-bed.y)*.12f);
            var inner=new[]{new Vector2(bed.x+edgeX,bed.y+edgeY),new Vector2(bed.z-edgeX,bed.y+edgeY),
                new Vector2(bed.z-edgeX,bed.w-edgeY),new Vector2(bed.x+edgeX,bed.w-edgeY)};
            for(int edge=0;edge<4;edge++)
            {
                int next=(edge+1)%4;
                if((outer[edge]-middle[edge]).sqrMagnitude>1e-12f||
                    (outer[next]-middle[next]).sqrMagnitude>1e-12f)
                    GateButtressPatch(stone,face,light,outer[edge],outer[next],middle[next],middle[edge],shade*.69f);
                float catchLight=edge==0?.83f:edge==2?1.08f:edge==1?.93f:1.015f;
                GateButtressPatch(stone,face,light,middle[edge],middle[next],inner[next],inner[edge],shade*catchLight);
            }
            GateButtressPatch(stone,face,light,inner[0],inner[1],inner[2],inner[3],shade);
        }

        private static void GateButtressPatch(Builder stone,Vector3[] face,Color[] light,
            Vector2 a,Vector2 b,Vector2 c,Vector2 d,float shade)
        {
            int first=stone.V.Count;
            stone.Quad(GateFaceLerp(face,a.x,a.y),GateFaceLerp(face,b.x,b.y),
                GateFaceLerp(face,c.x,c.y),GateFaceLerp(face,d.x,d.y),false,Color.white);
            var coords=new[]{a,b,c,d};
            for(int i=0;i<4;i++)
            {
                Vector2 p=coords[i];
                Color tint=Color.Lerp(Color.Lerp(light[0],light[1],p.x),
                    Color.Lerp(light[3],light[2],p.x),p.y)*shade;
                tint.a=1;stone.C[first+i]=tint;
            }
        }
    }
}
