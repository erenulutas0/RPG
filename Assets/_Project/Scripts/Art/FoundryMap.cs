using System;
using Cryptforge.Combat;

namespace Cryptforge.Art
{
    // One immutable layout shared by rendering, movement, spawn placement and pursuit.
    public static class FoundryMap
    {
        public const float Axis = .70710678f;
        // Reference-led court: four units of west-yard span are reassigned to the
        // entrance forecourt; pit, gate and bridge endpoints keep their anchors.
        // A narrower gate causeway ends in a full-width landing. These same bounds
        // own the floor mesh, hero clearance, spawning and the pursuit visibility graph.
        public const int West=-32,South=-18,North=18,East=14;
        // Shorter causeway; rigid landing and gate architecture translate together.
        // The court, furnace opening and arrival bridge retain their world anchors.
        public const int GateOffset=-4,GateLandingStart=28+GateOffset,GateLandingEnd=32+GateOffset;
        public const float GateLandingCentre=(GateLandingStart+GateLandingEnd)*.5f;
        // Original west service assemblies were authored against the -24 court edge.
        public const int WestAssemblyOffset=West+24;
        // Interior production equipment moves half the west extension, keeping
        // working space on both sides between the moving rim and the fixed pit.
        public const float WorkyardShift=(West+30)*.5f;
        public const int PitWest=-16,PitSouth=2,PitEast=-4,PitNorth=14;
        public const int CornerCut=6;
        public const int NorthCornerCut=4,CornerCount=4;
        // Raised work islands occupy existing paving. Rendering and navigation
        // share their exact footprint; they do not cut holes in the floor union.
        private static readonly float[,] WorkIslands=
        {
            {-26f+WorkyardShift,-4f,-22f+WorkyardShift,-1.8f},
            {-21.8f+WorkyardShift,11.8f,-18.6f+WorkyardShift,14.0f}
        };
        public static int WorkIslandCount=>WorkIslands.GetLength(0);
        public static float WorkIslandBound(int island,int coordinate)=>WorkIslands[island,coordinate];
        // Four diagonal edges share one boundary with render clipping and pursuit.
        public static bool CourtPlane(float u,float v) => u+v>=West+South+CornerCut && u-v<=East-South-CornerCut &&
            u+v<=East+North-NorthCornerCut && u-v>=West-North+NorthCornerCut;
        public static bool UncutCell(int u,int v) => Inside(u+.5f,v+.5f,false);
        private static readonly int[,] Rooms =
        {
            {West,South,PitWest,North},{PitEast,South,East,North},
            {PitWest,South,PitEast,PitSouth},{PitWest,PitNorth,PitEast,North},
            {East,0,GateLandingStart,10},{GateLandingStart,-2,GateLandingEnd,12}
        };
        public const int BridgeStart=-66,BridgeEnd=-22,BridgeHalfWidth=5;
        public const int BridgeWestSeam=2*South-BridgeHalfWidth,BridgeEastSeam=2*South+BridgeHalfWidth;
        public static bool Bridge(float u,float v) => u-v>=-BridgeHalfWidth&&u-v<=BridgeHalfWidth&&u+v>=BridgeStart&&u+v<=BridgeEnd;
        public static int RectangleCount => Rooms.GetLength(0);
        public static int Bound(int rectangle,int coordinate) => Rooms[rectangle,coordinate];
        public static void ToFloor(float u,float v,out float x,out float y) { x=(u-v)*Axis;y=(u+v)*Axis; }
        public static void ToDesign(float x,float y,out float u,out float v) { u=(x+y)*Axis;v=(y-x)*Axis; }
        public static bool Cell(int u,int v) => Inside(u+.5f,v+.5f);
        private static bool Inside(float u,float v,bool clipped=true)
        {
            for(int i=0;i<RectangleCount;i++)if(u>=Rooms[i,0]&&u<=Rooms[i,2]&&v>=Rooms[i,1]&&v<=Rooms[i,3]&&
                (!clipped||i>=4||CourtPlane(u,v)))return true;
            return Bridge(u,v);
        }
        public static bool Contains(float x,float y,float margin)
        {
            ToDesign(x,y,out float u,out float v);
            return SurfaceContains(x,y,margin)&&!WorkIslandIntersection(u,v,u,v,margin);
        }
        // For paving, inlays and exterior mass placement: occupied paving remains
        // a physical surface even though actors cannot walk through its equipment.
        public static bool SurfaceContains(float x,float y,float margin)
        {
            ToDesign(x,y,out float u,out float v);
            // Conservative square clearance in the orthonormal design plane. All void strips exceed twice the margin.
            return Inside(u,v)&&Inside(u-margin,v-margin)&&Inside(u+margin,v-margin)&&Inside(u-margin,v+margin)&&Inside(u+margin,v+margin);
        }
        public static bool Segment(float x,float y,float tx,float ty,float margin)
        {
            ToDesign(x,y,out float u,out float v);ToDesign(tx,ty,out float tu,out float tv);
            return !WorkIslandIntersection(u,v,tu,tv,margin)&&
                Line(u-margin,v-margin,tu-margin,tv-margin)&&Line(u+margin,v-margin,tu+margin,tv-margin)&&
                Line(u-margin,v+margin,tu-margin,tv+margin)&&Line(u+margin,v+margin,tu+margin,tv+margin);
        }
        // Exact swept square clearance: endpoint/corner sampling alone can miss
        // a narrow crossing or a tangent. Boundary contact is conservatively blocked.
        private static bool WorkIslandIntersection(float u,float v,float tu,float tv,float margin)
        {
            // Keep nominal boundary contact conservative after the orthonormal
            // float round trip on Mono, .NET and IL2CPP (as for entry rim relief).
            float clearance=margin+1e-4f;
            for(int i=0;i<WorkIslandCount;i++)
            {
                float enter=0,exit=1;
                if(Slab(u,tu-u,WorkIslands[i,0]-clearance,WorkIslands[i,2]+clearance,ref enter,ref exit)&&
                    Slab(v,tv-v,WorkIslands[i,1]-clearance,WorkIslands[i,3]+clearance,ref enter,ref exit))return true;
            }
            return false;
        }
        // Exact interval union: sampling could miss a tiny corner cut and leave a pursuer stuck there.
        private static bool Line(float u,float v,float tu,float tv)
        {
            float covered=0;
            for(int pass=0;pass<=RectangleCount;pass++)
            {
                float furthest=covered;
                for(int r=0;r<=RectangleCount;r++)
                {
                    float enter=0,exit=1;
                    bool intersects=r==RectangleCount
                        ? Slab(u-v,(tu-tv)-(u-v),-BridgeHalfWidth,BridgeHalfWidth,ref enter,ref exit)&&Slab(u+v,(tu+tv)-(u+v),BridgeStart,BridgeEnd,ref enter,ref exit)
                        : Slab(u,tu-u,Rooms[r,0],Rooms[r,2],ref enter,ref exit)&&Slab(v,tv-v,Rooms[r,1],Rooms[r,3],ref enter,ref exit);
                    if(intersects&&r<4)intersects=Slab(u+v,(tu+tv)-(u+v),West+South+CornerCut,East+North-NorthCornerCut,ref enter,ref exit)&&
                        Slab(u-v,(tu-tv)-(u-v),West-North+NorthCornerCut,East-South-CornerCut,ref enter,ref exit);
                    if(!intersects)continue;
                    if(enter<=covered+1e-6f)furthest=Math.Max(furthest,exit);
                }
                if(furthest>=1)return true;
                if(furthest<=covered)return false;
                covered=furthest;
            }
            return false;
        }
        private static bool Slab(float origin,float direction,float low,float high,ref float enter,ref float exit)
        {
            if(Math.Abs(direction)<1e-8f)return origin>=low&&origin<=high;
            float a=(low-origin)/direction,b=(high-origin)/direction;
            enter=Math.Max(enter,Math.Min(a,b));exit=Math.Min(exit,Math.Max(a,b));return enter<=exit;
        }
        public static bool Move(ref float x,ref float y,float dx,float dy,float margin)
        {
            float startX=x,startY=y;
            int steps=Math.Max(1,(int)Math.Ceiling(Math.Max(Math.Abs(dx),Math.Abs(dy))/.15f));
            ToDesign(dx/steps,dy/steps,out float du,out float dv);
            for(int i=0;i<steps;i++)
            {
                float nx=x+dx/steps,ny=y+dy/steps;
                if(Segment(x,y,nx,ny,margin)){x=nx;y=ny;continue;}
                ToFloor(du,0,out float sx,out float sy);
                if(Segment(x,y,x+sx,y+sy,margin)){x+=sx;y+=sy;}
                ToFloor(0,dv,out sx,out sy);
                if(Segment(x,y,x+sx,y+sy,margin)){x+=sx;y+=sy;}
            }
            return x!=startX||y!=startY;
        }
        // The gate landing in front of the door (design u 24..28, v -2..12): the floor's boss walks out of the gate onto
        // it, and the opened gate is left from its centre line by standing still (ExitRule), as the finale's exit is.
        public const float GateExitU=26.6f,GateExitV=5f,GateBossU=27.1f,GateBossSpacing=1.3f;
        public static void GateExitSpot(out float x,out float y)=>ToFloor(GateExitU,GateExitV,out x,out y);
        // The boss first, on the door's axis; any escort alternates either side of it along the landing.
        public static void PlaceAtGate(int count,EntryPlacement[] output)
        {
            for(int slot=0;slot<count;slot++)
            {
                float v=GateExitV+((slot+1)/2)*(slot%2==1?-GateBossSpacing:GateBossSpacing);
                ToFloor(GateBossU,v,out float x,out float y);
                output[slot]=new EntryPlacement(EntrySide.Far,0,x,y);
            }
        }
        public static void PlacePack(int ordinal,int count,float hx,float hy,float separation,EntryPlacement[] output)
        {
            for(int slot=0;slot<count;slot++)
            {
                bool found=false;
                for(float radius=3.5f;radius<=12&&!found;radius+=.75f)
                for(int k=0;k<64&&!found;k++)
                {
                    double angle=(k+(ordinal+slot)*17)*Math.PI/32;
                    float x=hx+(float)Math.Cos(angle)*radius,y=hy+(float)Math.Sin(angle)*radius;
                    if(!Contains(x,y,HeroMotion.EdgeMargin+.02f)||!Segment(hx,hy,x,y,HeroMotion.EdgeMargin))continue;
                    bool clear=true;
                    for(int j=0;j<slot;j++)if(ArenaFloor.FloorDistanceSquared(x,y,output[j].X,output[j].Y)<separation*separation){clear=false;break;}
                    if(!clear)continue;
                    output[slot]=new EntryPlacement(EntrySides.SideOf(ordinal,slot),0,x,y);found=true;
                }
                if(!found)throw new InvalidOperationException("Foundry entry has insufficient separated visible floor.");
            }
        }
    }
}
