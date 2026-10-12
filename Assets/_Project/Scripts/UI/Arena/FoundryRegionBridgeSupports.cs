using Cryptforge.Art;
using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private const float BridgeParapetWidth=.78f,BridgeParapetHeight=.78f,BridgeParapetStep=.75f;

        private void BuildBridgeParapets(ArenaView arena)
        {
            // Short panels carry their own depth separators, entirely outside the
            // walking union. A long design-axis bounding box would claim the lane.
            // The stone bond spans four panels, so ownership is finer than the art.
            foreach(float side in new[]{-1f,1f})
            {
                float end=2*FoundryMap.South+side*(FoundryMap.BridgeHalfWidth+BridgeParapetWidth);
                end=Mathf.Max(end,side<0?FoundryMap.BridgeWestSeam:FoundryMap.BridgeEastSeam);
                int panels=Mathf.CeilToInt((end-FoundryMap.BridgeStart)/BridgeParapetStep);
                for(int panel=panels-1;panel>=0;panel--)
                {
                    float s=FoundryMap.BridgeStart+panel*BridgeParapetStep;
                    var outline=BridgeParapetOutline(s,Mathf.Min(s+BridgeParapetStep,end),side);
                    if(outline.Count<3)continue;
                    Vector4 box=BridgeParapetOwner(outline);
                    int owner=RegisterDepthFootprint(box.x,box.y,box.z,box.w,BridgeParapetHeight,false,false);
                    using(new DepthScope(this,owner))
                    {
                        var stone=new Builder();
                        BridgeParapetCourses(stone,outline);
                        Create("Connected entrance parapet",stone,arena.PlatformMaterials.FoundryWall,-14);
                    }
                }
            }
        }

        private static List<Vector2> BridgeParapetOutline(float s0,float s1,float side)
        {
            float inner=side*FoundryMap.BridgeHalfWidth,outer=side*(FoundryMap.BridgeHalfWidth+BridgeParapetWidth);
            // This is the same physical court plane as the floor clip. In
            // particular the west outer edge meets the court before its inner one.
            return BridgeCourtClip(new List<Vector2>{new Vector2(s0,inner),new Vector2(s0,outer),
                new Vector2(s1,outer),new Vector2(s1,inner)});
        }

        private static Vector4 BridgeParapetOwner(List<Vector2> outline)
        {
            Vector2 centre=Vector2.zero;
            float s0=float.MaxValue,s1=float.MinValue,d0=float.MaxValue,d1=float.MinValue;
            foreach(Vector2 p in outline)
            {
                centre+=p;s0=Mathf.Min(s0,p.x);s1=Mathf.Max(s1,p.x);d0=Mathf.Min(d0,p.y);d1=Mathf.Max(d1,p.y);
            }
            centre/=outline.Count;
            Vector2 design=DeckDesign(centre.x,centre.y);
            // A design-axis rectangle INSCRIBED in the clipped panel, not its
            // bounding box. Every separator corner stays on the masonry footprint.
            float half=Mathf.Min(Mathf.Min(centre.x-s0,s1-centre.x),Mathf.Min(centre.y-d0,d1-centre.y))*.5f;
            half=Mathf.Min(half,FoundryMap.South-design.y)*.95f;
            return new Vector4(design.x-half,design.y-half,design.x+half,design.y+half);
        }

        private static void BridgeParapetCourses(Builder stone,List<Vector2> outline)
        {
            // A rooted plinth, two fitted wall courses and a broad weathered cap
            // connect the already seated lanterns and keeper console. The inner
            // edge stays exactly on d=+/-5 and the top remains below actor torsos.
            float lo=float.MaxValue,hi=float.MinValue;
            foreach(Vector2 p in outline){lo=Mathf.Min(lo,p.x);hi=Mathf.Max(hi,p.x);}
            // The keeper's broad seat starts at .18 and its painted foot at .38.
            // Step the parapet down across the existing console instead of burying
            // either socket under the new cap.
            float height=hi>BridgeHeadS-.95f&&lo<BridgeHeadS+.95f?.18f:BridgeParapetHeight;
            float body=height-.18f,middle=(body-.1f)*.5f;
            BridgeParapetTier(stone,outline,-.90f,-.10f,new Color(.43f,.47f,.56f),false);
            BridgeParapetTier(stone,outline,-.10f,middle,new Color(.57f,.60f,.68f),false);
            BridgeParapetTier(stone,outline,middle,body,new Color(.59f,.62f,.70f),false);
            BridgeParapetTier(stone,outline,body,height,new Color(.67f,.70f,.78f),true);
        }

        private static void BridgeParapetTier(Builder stone,List<Vector2> outline,float bottom,float top,Color tone,bool cap)
        {
            // Far return first; near return last in the shared transparent batch.
            for(int i=outline.Count-1;i>=0;i--)
            {
                Vector2 p=outline[i],q=outline[(i+1)%outline.Count];
                if(Vector2.Distance(p,q)<.0001f)continue;
                if(cap){BridgeParapetCapEdge(stone,p,q,bottom,top,tone);continue;}
                Vector3 a=Deck(p.x,p.y),b=Deck(q.x,q.y);
                int first=stone.V.Count;
                GateStonePatch(stone,a+Vector3.up*bottom,b+Vector3.up*bottom,
                    b+Vector3.up*top,a+Vector3.up*top,tone);
                bool along=Mathf.Abs(p.x-q.x)>.001f;
                float faceLight=along?(p.y<0?.89f:1f):.81f;
                float blockStart=FoundryMap.BridgeStart+3*Mathf.Floor((Mathf.Min(p.x,q.x)-FoundryMap.BridgeStart)/3);
                for(int k=0;k<4;k++)
                {
                    Vector2 at=k==0||k==3?p:q;
                    float h=k<2?bottom:top;
                    // Keep grain continuous inside each three-unit fitted block;
                    // owner subdivisions must not become tiny repeated bricks.
                    stone.U[first+k]=BridgeFortressWallGrain(at,h,blockStart,along);
                    // The deep plinth stays seated against the existing bridge
                    // fascia; readable cool upper stone still carries the lamps.
                    float contact=Mathf.Lerp(.85f,1f,Mathf.InverseLerp(-.9f,-.1f,h));
                    stone.C[first+k]=new Color(tone.r*faceLight*contact,tone.g*faceLight*contact,tone.b*faceLight*contact,1);
                }
            }
            if(!cap)return;
            BridgeParapetCap(stone,outline,top,tone);
        }

        private static void BridgeParapetCapEdge(Builder stone,Vector2 p,Vector2 q,float bottom,float top,Color tone)
        {
            // End returns meet the same bevel/joint profile as the cap above;
            // a single flat upper edge would leave a hole beneath its raised crown.
            var cuts=new List<float>{0,1};
            void Cut(float value,float a,float b)
            {
                if(Mathf.Abs(b-a)<.00001f)return;
                float t=(value-a)/(b-a);if(t>.00001f&&t<.99999f)cuts.Add(t);
            }
            float side=Mathf.Sign(p.y);
            foreach(float across in new[]{.14f,BridgeParapetWidth-.14f})
                Cut(side*(FoundryMap.BridgeHalfWidth+across),p.y,q.y);
            int first=Mathf.FloorToInt((Mathf.Min(p.x,q.x)-FoundryMap.BridgeStart)/3),
                last=Mathf.FloorToInt((Mathf.Max(p.x,q.x)-FoundryMap.BridgeStart)/3);
            for(int block=first;block<=last;block++)foreach(float d in new[]{0f,.03f,2.97f,3f})
                Cut(FoundryMap.BridgeStart+block*3+d,p.x,q.x);
            cuts.Sort();
            float faceLight=Mathf.Abs(p.x-q.x)>.001f?(p.y<0?.89f:1f):.81f;
            for(int i=0;i<cuts.Count-1;i++)
            {
                if(cuts[i+1]-cuts[i]<.00001f)continue;
                Vector2 a=Vector2.Lerp(p,q,cuts[i]),b=Vector2.Lerp(p,q,cuts[i+1]);
                int start=stone.V.Count;
                GateStonePatch(stone,Deck(a.x,a.y)+Vector3.up*bottom,Deck(b.x,b.y)+Vector3.up*bottom,
                    Deck(b.x,b.y)+Vector3.up*BridgeParapetCapHeight(b,top),
                    Deck(a.x,a.y)+Vector3.up*BridgeParapetCapHeight(a,top),tone*faceLight);
                float blockStart=FoundryMap.BridgeStart+3*Mathf.Floor(((a.x+b.x)*.5f-FoundryMap.BridgeStart)/3);
                bool along=Mathf.Abs(a.x-b.x)>.001f;
                for(int k=0;k<4;k++)
                {
                    Vector2 point=k==0||k==3?a:b;
                    stone.U[start+k]=BridgeFortressWallGrain(point,k<2?bottom:BridgeParapetCapHeight(point,top),blockStart,along);
                }
            }
        }

        private static float BridgeParapetCapJoint(float s)
        {
            float phase=Mathf.Repeat(s-FoundryMap.BridgeStart,3);
            return Mathf.Clamp01(Mathf.Min(phase,3-phase)/.03f);
        }

        private static float BridgeParapetCapHeight(Vector2 point,float top)
        {
            float across=Mathf.Abs(point.y)-FoundryMap.BridgeHalfWidth;
            float bevel=Mathf.Clamp01(Mathf.Min(across,BridgeParapetWidth-across)/.14f);
            // A shallow geometric joint keeps the projected cap monotone along s;
            // a deep V in such a narrow gap folds the 2D surface over itself.
            return top-.09f*(1-bevel)-.008f*(1-BridgeParapetCapJoint(point.x));
        }

        private static void BridgeParapetCap(Builder stone,List<Vector2> outline,float top,Color tone)
        {
            float lo=float.MaxValue,hi=float.MinValue;
            foreach(Vector2 p in outline){lo=Mathf.Min(lo,p.x);hi=Mathf.Max(hi,p.x);}
            float side=Mathf.Sign(outline[0].y);
            float[] across={0,.14f,BridgeParapetWidth-.14f,BridgeParapetWidth};
            int firstBlock=Mathf.FloorToInt((lo-FoundryMap.BridgeStart)/3),lastBlock=Mathf.FloorToInt((hi-FoundryMap.BridgeStart)/3);
            for(int block=firstBlock;block<=lastBlock;block++)
            {
                float start=FoundryMap.BridgeStart+block*3;
                float[] along={start,start+.03f,start+2.97f,start+3};
                for(int run=0;run<3;run++)for(int band=0;band<3;band++)
                {
                    float a=side*(FoundryMap.BridgeHalfWidth+across[band]),b=side*(FoundryMap.BridgeHalfWidth+across[band+1]);
                    var polygon=DeckClip(DeckClip(DeckClip(DeckClip(outline,0,along[run],1),0,along[run+1],-1),
                        1,Mathf.Min(a,b),1),1,Mathf.Max(a,b),-1);
                    if(polygon.Count<3)continue;
                    float area=0;Vector2 centre=Vector2.zero;
                    for(int i=0;i<polygon.Count;i++)
                    {
                        Vector2 p=polygon[i],q=polygon[(i+1)%polygon.Count];area+=p.x*q.y-q.x*p.y;centre+=p;
                    }
                    if(Mathf.Abs(area)<.00001f)continue;
                    centre/=polygon.Count;
                    int first=stone.V.Count;
                    float facing=band==1?0:band==0?-side:side;
                    float light=band==1?1.03f:facing>0?1.16f:.82f;
                    foreach(Vector2 p in polygon)BridgeParapetCapVertex(stone,p,top,tone,light,start);
                    BridgeParapetCapVertex(stone,centre,top,tone,light,start);
                    for(int i=0;i<polygon.Count;i++)
                        stone.T.AddRange(new[]{first+i,first+(i+1)%polygon.Count,first+polygon.Count});
                }
            }
        }

        private static void BridgeParapetCapVertex(Builder stone,Vector2 p,float top,Color tone,float light,float blockStart)
        {
            stone.V.Add(Deck(p.x,p.y)+Vector3.up*BridgeParapetCapHeight(p,top));
            float joint=.76f+.24f*BridgeParapetCapJoint(p.x);
            Color value=tone*(light*joint);value.a=1;stone.C.Add(value);
            stone.U.Add(BridgeFortressCapGrain(p,blockStart));
        }

        private static readonly float[] BridgeSupportStations=
        {
            BridgeHeadS-2.9f,(BridgeHeadS-2.9f+FoundryMap.BridgeStart+1.2f)*.5f,FoundryMap.BridgeStart+1.2f
        };

        private void BuildEntranceBridgeSupports(ArenaView arena)
        {
            // Repeated transverse masonry bents support the longer real deck. All
            // geometry is below its sidewall; the arch openings remain open void.
            foreach(float s in BridgeSupportStations)
            {
                // Shared transparent stone batch: far rows precede near rows;
                // all recessed returns precede that row's solid front silhouette.
                var stone=new Builder();var front=new Builder();
                const float spring=-6.1f,depth=1.3f;
                float half=FoundryMap.BridgeHalfWidth;
                Vector3 At(float d,float h,float along=0)=>Deck(s+along,d)+Vector3.up*h;
                for(int i=0;i<11;i++)
                {
                    float a=i*Mathf.PI/11,b=(i+1)*Mathf.PI/11;
                    Vector3 innerA=At(Mathf.Cos(a)*(half-1.05f),spring+Mathf.Sin(a)*2.35f);
                    Vector3 innerB=At(Mathf.Cos(b)*(half-1.05f),spring+Mathf.Sin(b)*2.35f);
                    Vector3 outerA=At(Mathf.Cos(a)*(half+.18f),spring+Mathf.Sin(a)*3.42f);
                    Vector3 outerB=At(Mathf.Cos(b)*(half+.18f),spring+Mathf.Sin(b)*3.42f);
                    Color face=new Color(.61f,.64f,.72f)*(i==5?1.08f:1f);
                    FortressDressedArch(front,innerA,innerB,outerB,outerA,face);
                    Vector3 back=Deck(s+depth,0)-Deck(s,0);
                    FortressMineralFace(stone,innerA,innerA+back,innerB+back,innerB,new Color(.31f,.35f,.45f),false);
                    // The filled spandrel bears the straight underside of the deck.
                    FortressMineralFace(front,outerA,outerB,At(Mathf.Cos(b)*(half+.18f),-2.7f),
                        At(Mathf.Cos(a)*(half+.18f),-2.7f),new Color(.49f,.53f,.62f),false);
                }
                foreach(float sign in new[]{-1f,1f})
                {
                    float a=sign*(half-1.05f),b=sign*(half+.18f);
                    FortressCoursedFace(front,At(a,-8.4f),At(b,-8.4f),At(b,spring),At(a,spring),new Color(.56f,.60f,.68f),3,1);
                    FortressCoursedFace(stone,At(b,-8.4f),At(b,-8.4f,depth),At(b,spring,depth),At(b,spring),new Color(.39f,.43f,.52f),3,1);
                    FortressCoursedFace(stone,At(a,-8.4f,depth),At(a,-8.4f),At(a,spring),At(a,spring,depth),new Color(.31f,.35f,.45f),3,1);
                }
                ShadeUndercroftDepth(stone,Deck(s,0),Deck(s,0));
                ShadeUndercroftDepth(front,Deck(s,0),Deck(s,0));
                Create("Entrance bridge masonry returns",stone,arena.PlatformMaterials.FoundryWall,-16);
                Create("Entrance bridge masonry bents",front,arena.PlatformMaterials.FoundryWall,-16);
            }
        }
    }
}
