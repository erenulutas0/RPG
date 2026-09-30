using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Static, below-deck architecture. The shared walking union remains authoritative.
    public sealed partial class FoundryRegionView
    {
        private void BuildUndercroft(ArenaView arena)
        {
            foreach(var cell in _cells)
            {
                int x=cell.x,y=cell.y;
                // Only the two exterior faces visible in the fixed isometric view.
                // The inset shaft has its own lining and must remain open.
                if(x>-13&&x<0&&y>0&&y<12)continue;
                if(x%4==0 && ExteriorSpan(x,y,true))
                    UndercroftArch(arena,Project(x,y),Project(x+4,y),x/4+y,!ExteriorSpan(x+4,y,true));
                if(y%4==0 && ExteriorSpan(x,y,false))
                    UndercroftArch(arena,Project(x,y),Project(x,y+4),y/4+x,!ExteriorSpan(x,y+4,false));
            }
            // Long suspension catenaries connect scenery anchors beneath the deck.
            SuspendedChain(arena,Project(-20,-10)+Vector3.down*4,
                Project(-12,-10)+Vector3.down*4,2.1f);
            SuspendedChain(arena,Project(4,-10)+Vector3.down*4,
                Project(12,-10)+Vector3.down*4,2.4f);
            foreach(float u in new[]{17f,25f})
                FurnaceDrain(arena,Project(u+1.5f,-5.1f));
        }

        private static bool ExteriorSpan(int x,int y,bool alongX)
        {
            for(int i=0;i<4;i++)
            {
                int u=x+(alongX?i:0),v=y+(alongX?0:i);
                if(!FoundryMap.Cell(u,v)||FoundryMap.Cell(u-(alongX?0:1),v-(alongX?1:0)))return false;
            }
            return true;
        }

        private void UndercroftArch(ArenaView arena,Vector3 a,Vector3 b,int cadence,bool terminal)
        {
            var stone=new Builder();
            // Odd count places one load-bearing key at the crown. Recessed mortar
            // remains behind the joints rather than opening cracks through the arch.
            const int count=13;
            for(int i=0;i<count;i++)
            {
                float t=i/(float)count,n=(i+1)/(float)count;
                Vector3 p=ArchIntrados(a,b,t),q=ArchIntrados(a,b,n);
                stone.WallQuad(p,q,q+Vector3.up*.8f,p+Vector3.up*.8f,false,new Color(.19f,.21f,.29f));
                Vector3 left=Vector3.Lerp(p,q,.035f),right=Vector3.Lerp(p,q,.965f);
                bool key=i==count/2;
                Vector3 lower=Vector3.down*(key?.10f:0);
                Vector3 upper=Vector3.up*(key?.96f:.78f);
                float variation=1+(((i*7+Mathf.Abs(cadence))%5)-2)*.018f;
                Color shade=i<count/2?new Color(.49f,.50f,.60f):new Color(.37f,.39f,.49f);
                if(key)shade=new Color(.54f,.53f,.59f);
                shade*=variation;shade.a=1;
                stone.WallQuad(left+lower,right+lower,right+upper,left+upper,false,shade);
                // A narrow lit upper arris and dark return give each wedge thickness.
                stone.WallQuad(left+upper,right+upper,right+upper+Vector3.down*.045f,
                    left+upper+Vector3.down*.045f,false,new Color(.57f,.57f,.64f));
                stone.WallQuad(left+lower,right+lower,right+lower+Vector3.down*.11f,
                    left+lower+Vector3.down*.11f,false,new Color(.17f,.19f,.26f));
            }
            ShadeUndercroftDepth(stone,a,b);
            Create("Undercroft arches and ribs",stone,arena.PlatformMaterials.FoundryWall,-16);
            // Every third pier carries a deeper buttress. End spans receive their own
            // support so neither arch foot terminates as a floating masonry blade.
            UndercroftPier(arena,a,cadence%3==0);
            if(terminal)UndercroftPier(arena,b,true);
        }

        private static Vector3 ArchIntrados(Vector3 a,Vector3 b,float t)
        {
            return Vector3.Lerp(a,b,t)+Vector3.down*(5.8f-2.7f*Mathf.Sin(t*Mathf.PI));
        }

        private void UndercroftPier(ArenaView arena,Vector3 anchor,bool major)
        {
            float half=major?.42f:.29f,depth=major?8.8f:7.25f;
            Vector3 top=anchor+Vector3.down*2.5f;
            Vector3 foot=anchor+Vector3.down*depth;
            Vector3 bevel=new Vector3(half*.32f,.12f,0);
            var stone=new Builder();
            stone.WallQuad(top+Vector3.left*half,top+Vector3.right*half,
                foot+Vector3.right*half*.55f,foot+Vector3.left*half*.55f,false,new Color(.4f,.43f,.54f));
            stone.WallQuad(top+Vector3.left*half-bevel,top+Vector3.left*half,
                foot+Vector3.left*half*.55f,foot+Vector3.left*half*.55f-bevel*.45f,false,new Color(.26f,.29f,.39f));
            stone.WallQuad(top+Vector3.right*half,top+Vector3.right*half+bevel,
                foot+Vector3.right*half*.55f+bevel*.45f,foot+Vector3.right*half*.55f,false,new Color(.48f,.49f,.58f));
            // Shoulder and lower course join the pier to the deck and the arch spring.
            foreach(float drop in new[]{.22f,3.12f})
            {
                Vector3 p=top+Vector3.down*drop;
                float width=half*(drop<1?1.18f:.92f);
                stone.WallQuad(p+Vector3.left*width,p+Vector3.right*width,
                    p+Vector3.right*width+Vector3.down*.2f,p+Vector3.left*width+Vector3.down*.2f,
                    false,new Color(.46f,.46f,.53f));
            }
            ShadeUndercroftDepth(stone,anchor,anchor);
            Create("Faceted undercroft buttresses",stone,arena.PlatformMaterials.FoundryWall,-16);
            if(!major)return;
            var iron=new Builder();
            Vector3 plate=top+Vector3.down*.55f;
            FullFace(iron,plate+Vector3.left*.22f,plate+Vector3.right*.22f,
                plate+Vector3.right*.22f+Vector3.down*1.2f,plate+Vector3.left*.22f+Vector3.down*1.2f,
                new Color(.43f,.43f,.46f));
            foreach(float x in new[]{-.14f,.14f})
                foreach(float y in new[]{.14f,1.06f})
                {
                    Vector3 p=plate+new Vector3(x,-y,0);
                    FullFace(iron,p+Vector3.up*.045f,p+Vector3.right*.04f,
                        p+Vector3.down*.045f,p+Vector3.left*.04f,new Color(.8f,.68f,.46f));
                }
            // The existing discharge rank is above stone and below the walking floor;
            // do not rely on equal-order material batches to expose the anchor plates.
            ShadeUndercroftDepth(iron,anchor,anchor);
            Create("Undercroft iron anchors",iron,arena.PlatformMaterials.FoundryIron,-15.4f);
        }

        // Authored build-time shading, not transparency or realtime lighting. Read depth
        // from the local deck line so distant and near facades receive the same treatment.
        private static void ShadeUndercroftDepth(Builder mesh,Vector3 deckA,Vector3 deckB)
        {
            for(int i=0;i<mesh.V.Count;i++)
            {
                Vector3 p=mesh.V[i];
                float t=Mathf.Abs(deckB.x-deckA.x)>.001f?
                    Mathf.Clamp01((p.x-deckA.x)/(deckB.x-deckA.x)):0;
                float depth=Vector3.Lerp(deckA,deckB,t).y-p.y;
                float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.5f,8.8f,depth));
                Color c=mesh.C[i];
                float shade=Mathf.Lerp(1,.64f,fade);
                mesh.C[i]=new Color(c.r*shade*(1-.07f*fade),c.g*shade,
                    c.b*shade*(1+.035f*fade),c.a);
            }
        }

        private void SuspendedChain(ArenaView arena,Vector3 a,Vector3 b,float sag)
        {
            const int count=8;
            for(int i=0;i<count;i++)
            {
                float t=i/(float)count,n=(i+1)/(float)count;
                Chain(arena,Vector3.Lerp(a,b,t)+Vector3.down*(4*sag*t*(1-t)),
                    Vector3.Lerp(a,b,n)+Vector3.down*(4*sag*n*(1-n)),.11f,-16);
            }
        }

        private void FurnaceDrain(ArenaView arena,Vector3 mouth)
        {
            var iron=new Builder();
            Vector3 bottom=mouth+Vector3.down*6.7f;
            FullFace(iron,mouth+Vector3.left*.24f,bottom+Vector3.left*.13f,
                bottom+Vector3.right*.13f,mouth+Vector3.right*.24f,new Color(.4f,.4f,.46f));
            Create("Furnace discharge casing",iron,arena.PlatformMaterials.FoundryIron,-15.4f);
            if(arena.PlatformMaterials.FoundryEmbers==null)return;
            var heat=new Builder();
            FullFace(heat,mouth+Vector3.down*.4f+Vector3.left*.11f,bottom+Vector3.left*.035f,
                bottom+Vector3.right*.035f,mouth+Vector3.down*.4f+Vector3.right*.11f,new Color(.8f,.43f,.18f));
            for(int i=0;i<heat.U.Count;i++)heat.U[i]=new Vector2(.065f+heat.U[i].x*.035f,.7f+heat.U[i].y*.25f);
            Create("Contained furnace discharge",heat,arena.PlatformMaterials.FoundryEmbers,-15.5f);
        }
    }
}
