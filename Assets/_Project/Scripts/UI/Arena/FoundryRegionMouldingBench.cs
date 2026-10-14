using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // This station reuses the work island's iron field and existing batch.
        // Shapes are assembled in the fixed camera's back-to-front order.
        private static void YardMouldingBenchFrame(Builder kit,float u0,float v0,float u1,float v1)
        {
            foreach(float u in new[]{u0+.16f,u1-.36f})
            {
                YardMouldingBenchLeg(kit,u,v1-.38f,v1-.58f);
                WorkshopMetalHull(kit,u,v0+.20f,u+.20f,v1-.20f,.30f,.43f,.02f,true);
                YardMouldingBenchLeg(kit,u,v0+.18f,v0+.34f);
            }
        }

        private static void YardMouldingBenchLeg(Builder kit,float u,float footV,float headV)
        {
            var foot=new[]{Project(u,footV)+Vector3.up*.10f,Project(u+.20f,footV)+Vector3.up*.10f,
                Project(u+.20f,footV+.20f)+Vector3.up*.10f,Project(u,footV+.20f)+Vector3.up*.10f};
            var head=new[]{Project(u-.025f,headV)+Vector3.up*.99f,Project(u+.225f,headV)+Vector3.up*.99f,
                Project(u+.225f,headV+.24f)+Vector3.up*.99f,Project(u-.025f,headV+.24f)+Vector3.up*.99f};
            foreach(int edge in new[]{1,2,0,3})
            {
                int next=(edge+1)%4;
                FullFace(kit,foot[edge],foot[next],head[next],head[edge],
                    edge==0?new Color(.43f,.47f,.53f):new Color(.29f,.33f,.40f));
            }
            FullFace(kit,head[0],head[1],head[2],head[3],new Color(.55f,.59f,.65f));
        }

        private static void YardWorkingMould(Builder kit,float u0,float v0,float u1,float v1,
            float bottom,float top,bool bearing,Vector3 origin)
        {
            int first=kit.V.Count;
            WorkshopMetalHull(kit,u0,v0,u1,v1,bottom,bottom+.055f,.025f,true);
            YardBenchIronFinish(kit,first,kit.V.Count,origin);

            // The sand top is partitioned around a real opening. Recess walls
            // join its perimeter to a lower floor; no full sand quad fills it.
            first=kit.V.Count;
            float sandTop=top-.04f,sandFloor=bottom+.085f;
            if(bearing)
            {
                YardBearingSandCavity(kit,u0+.12f,v0+.12f,u1-.12f,v1-.12f,sandTop,sandFloor);
                YardBearingSandTop(kit,u0+.12f,v0+.12f,u1-.12f,v1-.12f,sandTop);
            }
            else
            {
                YardIngotSandCavity(kit,u0+.12f,v0+.12f,u1-.12f,v1-.12f,sandTop,sandFloor);
                YardIngotSandTop(kit,u0+.12f,v0+.12f,u1-.12f,v1-.12f,sandTop);
            }
            YardMouldSandGrain(kit,first,kit.V.Count,origin);

            // Four substantial oak rails carry the same muted hardwood family
            // as the working planks and the neighbouring authored casting bench.
            YardMouldOakRail(kit,u0,v1-.12f,u1,v1,bottom+.055f,top,false,origin);
            YardMouldOakRail(kit,u1-.12f,v0+.12f,u1,v1-.12f,bottom+.055f,top,true,origin);
            YardMouldOakRail(kit,u0,v0,u1,v0+.12f,bottom+.055f,top,false,origin);
            YardMouldOakRail(kit,u0,v0+.12f,u0+.12f,v1-.12f,bottom+.055f,top,true,origin);

            // Connected U-handles have three plain cast bars. Removing tiny
            // bevel rings recovers geometry for the actual sand recesses.
            first=kit.V.Count;
            float middle=(u0+u1)*.5f;
            foreach(float edge in new[]{v0-.075f,v1-.02f})
            {
                bool far=edge>v0;
                float footStart=far?edge:edge+.04f,footEnd=far?edge+.055f:edge+.095f;
                float barStart=far?edge+.055f:edge,barEnd=far?edge+.095f:edge+.04f;
                YardMouldHardwareBox(kit,middle-.24f,footStart,middle-.17f,footEnd,top-.09f,top-.03f);
                YardMouldHardwareBox(kit,middle+.17f,footStart,middle+.24f,footEnd,top-.09f,top-.03f);
                YardMouldHardwareBox(kit,middle-.24f,barStart,middle+.24f,barEnd,top-.09f,top-.03f);
            }
            YardBenchIronFinish(kit,first,kit.V.Count,origin);
        }

        private static void YardMouldOakRail(Builder kit,float u0,float v0,float u1,float v1,
            float bottom,float top,bool alongV,Vector3 origin)
        {
            int first=kit.V.Count;
            WorkshopMetalHull(kit,u0,v0,u1,v1,bottom,top,.02f,true);
            YardBenchTimberFinish(kit,first,kit.V.Count,origin);
            if(!alongV)return;
            Vector2 along=((Vector2)(Project(0,1)-Project(0,0))).normalized;
            Vector2 across=new Vector2(-along.y,along.x);
            for(int i=first;i<kit.V.Count;i++)
            {
                Vector2 p=kit.V[i]-origin;
                kit.U[i]=new Vector2(.5f+Vector2.Dot(p,along)*.055f,.5f+Vector2.Dot(p,across)*.18f);
            }
        }

        private static Vector2[] YardIngotOpening(float u0,float v0,float u1,float v1)
        {
            float middle=(u0+u1)*.5f;
            return new[]{new Vector2(middle-.22f,v0+.17f),new Vector2(middle+.22f,v0+.17f),
                new Vector2(middle+.17f,v1-.17f),new Vector2(middle-.17f,v1-.17f)};
        }

        private static void YardIngotSandTop(Builder kit,float u0,float v0,float u1,float v1,float height)
        {
            var outer=new[]{new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u1,v1),new Vector2(u0,v1)};
            Vector2[] hole=YardIngotOpening(u0,v0,u1,v1);
            for(int edge=0;edge<4;edge++)
            {
                int next=(edge+1)%4;
                FullFace(kit,YardSandPoint(outer[edge],height),YardSandPoint(outer[next],height),
                    YardSandPoint(hole[next],height),YardSandPoint(hole[edge],height),new Color(.70f,.58f,.40f));
            }
        }

        private static void YardIngotSandCavity(Builder kit,float u0,float v0,float u1,float v1,float top,float floor)
        {
            Vector2[] hole=YardIngotOpening(u0,v0,u1,v1);
            Vector2 centre=new Vector2((u0+u1)*.5f,(v0+v1)*.5f);
            var bed=new Vector2[4];
            for(int i=0;i<4;i++)bed[i]=centre+Vector2.Scale(hole[i]-centre,new Vector2(.72f,.86f));
            FullFace(kit,YardSandPoint(bed[0],floor),YardSandPoint(bed[1],floor),
                YardSandPoint(bed[2],floor),YardSandPoint(bed[3],floor),new Color(.30f,.25f,.20f));
            foreach(int edge in new[]{1,2,0,3})
            {
                int next=(edge+1)%4;
                FullFace(kit,YardSandPoint(hole[edge],top),YardSandPoint(hole[next],top),
                    YardSandPoint(bed[next],floor),YardSandPoint(bed[edge],floor),
                    edge<2?new Color(.39f,.31f,.23f):new Color(.49f,.40f,.28f));
            }
        }

        // Every fourth boundary station is a real rectangle corner. Mapping
        // their directions to the circle avoids gaps at non-square sand corners.
        private static Vector2[] YardBearingBoundary(float u0,float v0,float u1,float v1)
        {
            var corners=new[]{new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u1,v1),new Vector2(u0,v1)};
            var points=new Vector2[16];
            for(int edge=0;edge<4;edge++)for(int i=0;i<4;i++)
                points[edge*4+i]=Vector2.Lerp(corners[edge],corners[(edge+1)%4],i*.25f);
            return points;
        }

        private static void YardBearingSandTop(Builder kit,float u0,float v0,float u1,float v1,float height)
        {
            Vector2 centre=new Vector2((u0+u1)*.5f,(v0+v1)*.5f);
            float radius=Mathf.Min(u1-u0,v1-v0)*.41f;
            Vector2[] outer=YardBearingBoundary(u0,v0,u1,v1);
            var hole=new Vector2[16];
            for(int i=0;i<16;i++)hole[i]=centre+(outer[i]-centre).normalized*radius;
            for(int i=0;i<16;i++)
            {
                int next=(i+1)%16;
                FullFace(kit,YardSandPoint(outer[i],height),YardSandPoint(outer[next],height),
                    YardSandPoint(hole[next],height),YardSandPoint(hole[i],height),new Color(.70f,.58f,.40f));
            }
            // One shared fan closes only the central sand core, leaving the
            // annular casting cavity open around it without degenerate quads.
            int first=kit.V.Count;
            kit.V.Add(YardSandPoint(centre,height));kit.U.Add(Vector2.zero);kit.C.Add(new Color(.74f,.62f,.44f));
            for(int i=0;i<16;i++)
            {
                kit.V.Add(YardSandPoint(Vector2.Lerp(centre,hole[i],.48f),height));
                kit.U.Add(Vector2.zero);kit.C.Add(new Color(.74f,.62f,.44f));
                kit.T.Add(first);kit.T.Add(first+1+i);kit.T.Add(first+1+(i+1)%16);
            }
        }

        private static void YardBearingSandCavity(Builder kit,float u0,float v0,float u1,float v1,float top,float floor)
        {
            Vector2 centre=new Vector2((u0+u1)*.5f,(v0+v1)*.5f);
            float radius=Mathf.Min(u1-u0,v1-v0)*.41f;
            Vector2[] boundary=YardBearingBoundary(u0,v0,u1,v1);
            var directions=new Vector2[16];
            for(int i=0;i<16;i++)directions[i]=(boundary[i]-centre).normalized*radius;
            // Floor first, then sloping outer wall and the central core wall.
            // The separately emitted top shoulders mask the physical opening.
            for(int face=0;face<3;face++)for(int i=0;i<16;i++)
            {
                int next=(i+1)%16;
                float ra=face==0?.91f:face==1?1f:.57f;
                float rb=face==0?.57f:face==1?.91f:.48f;
                float ha=face==1?top:floor,hb=face==2?top:floor;
                Color colour=face==0?new Color(.30f,.25f,.20f):face==1?
                    new Color(.39f,.31f,.23f):new Color(.49f,.40f,.28f);
                FullFace(kit,YardSandPoint(centre+directions[i]*ra,ha),YardSandPoint(centre+directions[next]*ra,ha),
                    YardSandPoint(centre+directions[next]*rb,hb),YardSandPoint(centre+directions[i]*rb,hb),colour);
            }
        }

        private static Vector3 YardSandPoint(Vector2 point,float height)=>Project(point.x,point.y)+Vector3.up*height;

        private static void YardMouldSandGrain(Builder kit,int first,int end,Vector3 origin)
        {
            for(int i=first;i<end;i++)
            {
                Vector3 p=kit.V[i]-origin;
                kit.U[i]=new Vector2(.5f+p.x*.21f,.5f+p.y*.21f);
            }
        }

        private static void YardMouldHardwareBox(Builder kit,float u0,float v0,float u1,float v1,float bottom,float top)
        {
            Vector3 a=Project(u0,v0),b=Project(u1,v0),c=Project(u1,v1),d=Project(u0,v1);
            FullFace(kit,a+Vector3.up*bottom,b+Vector3.up*bottom,b+Vector3.up*top,a+Vector3.up*top,new Color(.40f,.44f,.51f));
            FullFace(kit,d+Vector3.up*bottom,a+Vector3.up*bottom,a+Vector3.up*top,d+Vector3.up*top,new Color(.27f,.31f,.38f));
            FullFace(kit,a+Vector3.up*top,b+Vector3.up*top,c+Vector3.up*top,d+Vector3.up*top,new Color(.58f,.62f,.68f));
        }

        private static void YardMouldClamp(Builder kit,float u,float v)
        {
            // One L casting replaces two overlapping bevelled boxes. Its screw
            // still contacts the long flask's rim at 1.40 and reaches 1.54.
            var profile=new[]{new Vector2(u,1.13f),new Vector2(u+.10f,1.13f),
                new Vector2(u+.10f,1.43f),new Vector2(u+.28f,1.43f),
                new Vector2(u+.28f,1.49f),new Vector2(u,1.49f)};
            Vector3 depth=Project(u,v+.14f)-Project(u,v);
            for(int edge=1;edge<6;edge++)
            {
                int next=(edge+1)%6;
                Vector3 a=Project(profile[edge].x,v)+Vector3.up*profile[edge].y;
                Vector3 b=Project(profile[next].x,v)+Vector3.up*profile[next].y;
                FullFace(kit,a,b,b+depth,a+depth,edge==4?new Color(.58f,.62f,.68f):new Color(.30f,.34f,.41f));
            }
            FullFace(kit,Project(u,v)+Vector3.up*1.13f,Project(u+.10f,v)+Vector3.up*1.13f,
                Project(u+.10f,v)+Vector3.up*1.43f,Project(u,v)+Vector3.up*1.43f,new Color(.40f,.44f,.51f));
            FullFace(kit,Project(u,v)+Vector3.up*1.43f,Project(u+.28f,v)+Vector3.up*1.43f,
                Project(u+.28f,v)+Vector3.up*1.49f,Project(u,v)+Vector3.up*1.49f,new Color(.40f,.44f,.51f));
            YardMouldHardwareBox(kit,u+.11f,v+.035f,u+.17f,v+.105f,1.40f,1.52f);
            YardMouldHardwareBox(kit,u+.05f,v+.035f,u+.23f,v+.105f,1.50f,1.54f);
        }
    }
}
