using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Static, below-deck architecture. The shared walking union remains authoritative.
    public sealed partial class FoundryRegionView
    {
        private void BuildUndercroft(ArenaView arena)
        {
            // Piers go in after every arch. Arches and piers share one masonry batch, painted in emission order, and _cells
            // lists the court before the room west of it, so the arch from (-8,-14) followed the major pier at (-4,-14),
            // right of the bridge head, and laid its foot over the pier's left half. Now every pier covers both arch feet.
            var piers=new List<(Vector2 at,bool major)>();
            foreach(var cell in _cells)
            {
                int x=cell.x,y=cell.y;
                // Only the two exterior faces visible in the fixed isometric view.
                // The inset shaft has its own lining and must remain open.
                if(x>-13&&x<0&&y>0&&y<12)continue;
                if(x%4==0 && ExteriorSpan(x,y,true))
                    UndercroftArch(arena,new Vector2(x,y),new Vector2(x+4,y),x/4+y,!ExteriorSpan(x+4,y,true),piers);
                if(y%4==0 && ExteriorSpan(x,y,false))
                    UndercroftArch(arena,new Vector2(x,y),new Vector2(x,y+4),y/4+x,!ExteriorSpan(x,y+4,false),piers);
            }
            foreach(var pier in piers)UndercroftPier(arena,pier.at,pier.major);
            // Long suspension catenaries connect scenery anchors beneath the deck.
            SuspendedChain(arena,Project(-20,-10)+Vector3.down*4,
                Project(-12,-10)+Vector3.down*4,2.1f);
            SuspendedChain(arena,Project(4,-10)+Vector3.down*4,
                Project(12,-10)+Vector3.down*4,2.4f);
            // The near drain stops behind the court. At the far one's 6.7 its foot came out below the court's south edge on
            // screen (6.29 under its mouth), the casing's rank (-16) painting it over that face (-17), which stands in front.
            foreach(var drain in new[]{new Vector2(17,5.2f),new Vector2(25,6.7f)})
                FurnaceDrain(arena,Project(drain.x+1.5f,-5.1f),drain.y);
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

        private void UndercroftArch(ArenaView arena,Vector2 from,Vector2 to,int cadence,bool terminal,
            List<(Vector2 at,bool major)> piers)
        {
            Vector3 a=Project(from.x,from.y),b=Project(to.x,to.y);
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
                Color shade=i<count/2?new Color(.53f,.53f,.60f):new Color(.42f,.43f,.51f);
                if(key)shade=new Color(.58f,.56f,.60f);
                shade*=variation;shade.a=1;
                // A broad inner return separates the load-bearing arch face from the
                // opening. Both planes stay inside the existing wedge contour, with
                // continuous world masonry UVs rather than an extra applied trim.
                Vector3 returnTop=Vector3.Lerp(lower,upper,.3f);
                Color returnShade=new Color(shade.r*.68f,shade.g*.7f,shade.b*.75f);
                stone.WallQuad(left+lower,right+lower,right+returnTop,left+returnTop,false,returnShade);
                stone.WallQuad(left+returnTop,right+returnTop,right+upper,left+upper,false,shade);
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
            piers.Add((from,cadence%3==0));
            if(terminal)piers.Add((to,true));
        }

        private static Vector3 ArchIntrados(Vector3 a,Vector3 b,float t)
        {
            return Vector3.Lerp(a,b,t)+Vector3.down*(5.8f-2.7f*Mathf.Sin(t*Mathf.PI));
        }

        private void UndercroftPier(ArenaView arena,Vector2 at,bool major)
        {
            Vector3 anchor=Project(at.x,at.y);
            float half=major?.42f:.29f,depth=major?8.8f:7.25f;
            Vector3 top=anchor+Vector3.down*2.5f;
            Vector3 foot=anchor+Vector3.down*depth;
            Vector3 bevel=new Vector3(half*.32f,.12f,0);
            var stone=new Builder();
            // Broad longitudinal planes make the shaft read as structural masonry
            // below the deck. This only partitions the old front quad: the outline,
            // taper, depth owner and continuously projected stone UVs are unchanged.
            float[] across={-1f,-.56f,.56f,1f};
            Color[] front={new Color(.32f,.35f,.45f),new Color(.45f,.46f,.53f),
                new Color(.39f,.41f,.50f)};
            for(int face=0;face<3;face++)
                stone.WallQuad(top+Vector3.right*(half*across[face]),
                    top+Vector3.right*(half*across[face+1]),
                    foot+Vector3.right*(half*.55f*across[face+1]),
                    foot+Vector3.right*(half*.55f*across[face]),false,front[face]);
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
            // A bastion or a boiler's foundation stands in front of four major piers (12,-14; -18,0; -18,16; 20,-2). Their
            // stone paints under those masses (one batch, emitted first), but the plate's rank is over all the stone below
            // the deck, so it painted over the mass in front: across the top of the bastion at (12,-14), for one.
            if(!major||PierScreened(at))return;
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

        // True where a built bastion or a boiler's foundation stands between the face at design point p and the camera,
        // within a unit of it. The sight line runs to -u -v climbing .71 a unit, so out to there a plate (3.05 to 4.25
        // under the deck) is still level with a bastion's block, capped 2.3 down, or a boiler's drum.
        private static bool PierScreened(Vector2 p)
        {
            for(float t=0;t<=1.001f;t+=.25f)
            {
                Vector2 q=p-new Vector2(t,t);
                foreach(var b in Bastions)if(q.x>=b.x&&q.x<=b.z&&q.y>=b.y&&q.y<=b.w&&MassClear(b))return true;
                foreach(var c in BoilerCentres)if((q-c).sqrMagnitude<BoilerPlinth*BoilerPlinth)return true;
            }
            return false;
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

        // A boiler's discharge chute: an iron casing hung from the mouth, its face open in a slot from ChuteSlot down that
        // shows the molten metal. Casing (FoundryIron -15.4) and metal (FoundryEmbers -15.5) share the rank -16, where
        // Unity paints batches by bounds centre on the sort axis, the higher first: the abyss chains hold the iron's centre
        // near -9.7 and the metal holds the embers' near +1.3, so the iron paints last, and a solid casing hid the metal
        // entirely (to 2026-10-01). The casing is now a frame - a collar over the mouth and a rail either side of the slot -
        // the metal tucked ChuteTuck under it, so either order shows the same chute. The metal stays in the embers batch:
        // its reach up the screen keeps that centre above the pit lining's, which the molten pool relies on (PitPool).
        // Without the ember material the casing stays solid.
        private void FurnaceDrain(ArenaView arena,Vector3 mouth,float length)
        {
            var embers=arena.PlatformMaterials.FoundryEmbers;
            var iron=new Builder();
            Color plate=new Color(.4f,.4f,.46f);
            float top=ChuteHalf(0,length),collar=ChuteHalf(ChuteSlot,length),foot=ChuteHalf(length,length);
            if(embers==null)ChuteQuad(iron,mouth,length,plate,new Vector2(0,-top),new Vector2(length,-foot),
                new Vector2(length,foot),new Vector2(0,top));
            else
            {
                ChuteQuad(iron,mouth,length,plate,new Vector2(0,-top),new Vector2(ChuteSlot,-collar),
                    new Vector2(ChuteSlot,collar),new Vector2(0,top));
                foreach(float side in new[]{-1f,1f})
                    ChuteQuad(iron,mouth,length,plate,new Vector2(ChuteSlot,side*collar),new Vector2(length,side*foot),
                        new Vector2(length,side*ChuteOpening.y),new Vector2(ChuteSlot,side*ChuteOpening.x));
            }
            Create("Furnace discharge casing",iron,arena.PlatformMaterials.FoundryIron,-15.4f);
            if(embers==null)return;
            // The metal over the ember texture's hot seam, as the beacons, lantern panes and fire windows are lit (the
            // crust patch it sampled before would have read as dark slag): bright at the mouth, cooling to the foot.
            var heat=new Builder();
            Vector3 head=mouth+Vector3.down*(ChuteSlot-ChuteTuck),end=mouth+Vector3.down*length;
            float wide=ChuteOpening.x+ChuteTuck,narrow=ChuteOpening.y+ChuteTuck;
            FullFace(heat,head+Vector3.left*wide,end+Vector3.left*narrow,end+Vector3.right*narrow,
                head+Vector3.right*wide,ChuteHot);
            heat.C[1]=heat.C[2]=ChuteCool;
            HotSeam(heat,0);
            Create("Contained furnace discharge",heat,embers,-15.5f);
        }

        // The casing's half-width runs .24 at the mouth to .13 at the foot; the slot's, ChuteOpening, .11 at its head to .035.
        private const float ChuteSlot=.4f,ChuteTuck=.01f;
        private static readonly Vector2 ChuteOpening=new Vector2(.11f,.035f);
        private static readonly Color ChuteHot=new Color(1.35f,1.5f,1.2f),ChuteCool=new Color(.85f,.75f,.6f);
        private static float ChuteHalf(float depth,float length)=>Mathf.Lerp(.24f,.13f,depth/length);

        // One quad of a chute's casing from corners (depth under the mouth, offset across), mapped as the solid casing's full
        // face is: the iron plate's u down the length, v across the casing's width at that depth.
        private static void ChuteQuad(Builder b,Vector3 mouth,float length,Color color,Vector2 p,Vector2 q,Vector2 r,Vector2 s)
        {
            int n=b.V.Count;
            foreach(var k in new[]{p,q,r,s})
            {
                b.V.Add(mouth+new Vector3(k.y,-k.x,0));b.C.Add(color);
                b.U.Add(new Vector2(k.x/length,.5f+k.y/(2*ChuteHalf(k.x,length))));
            }
            b.T.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
    }
}
