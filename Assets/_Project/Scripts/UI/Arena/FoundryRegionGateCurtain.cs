using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // Active curtain construction only. Shared enclosure/fallback emitters keep
        // their previous geometry and material behavior.
        private static void GateCurtainWall(Builder stone,bool far)
        {
            float v0=far?13f:-6.6f,v1=far?16.6f:-3f;
            GateEnclosureTier(stone,GateU(32),v0,GateU(36),v1,0,.46f,.06f,.91f,true);
            GateEnclosureTier(stone,GateU(32.16f),v0+.12f,GateU(35.88f),v1-.12f,.46f,2.9f,.035f,.82f);
            GateEnclosureTier(stone,GateU(32.05f),v0+.05f,GateU(35.98f),v1-.05f,2.9f,3.13f,.025f,.94f,true);
            GateCurtainUpperTier(stone,new Vector4(GateU(32.2f),v0+.16f,GateU(35.84f),v1-.16f),far);
            GateEnclosureTier(stone,GateU(32.02f),v0+.02f,GateU(36),v1-.02f,6.55f,6.82f,.045f,.98f,true);
            GateCurtainRoof(stone,new Vector4(GateU(32.15f),v0+.14f,GateU(35.87f),v1-.14f));
        }

        private static Vector3[] GateCurtainOpening(Vector4 box,bool far,bool inner)
        {
            const float low=3.53f,high=6.12f;
            float centre=far?.155f:.845f,half=.085f,h=high-low;
            var shape=new[]{new Vector2(centre-half,low),new Vector2(centre+half,low),
                new Vector2(centre+half,high-h*.27f),new Vector2(centre,high),
                new Vector2(centre-half,high-h*.27f)};
            var points=new Vector3[5];
            for(int i=0;i<5;i++)
            {
                Vector2 p=shape[i];
                if(inner)p=new Vector2(Mathf.Lerp(p.x,centre,.25f),Mathf.Lerp(p.y,low+h*.46f,.13f));
                points[i]=GateEnclosureFace(box,true,p.x,p.y,3.13f,6.55f,.05f);
            }
            return points;
        }

        private static void GateCurtainUpperTier(Builder mesh,Vector4 box,bool far)
        {
            var shell=new Builder();LitFoundryTier(shell,box.x,box.y,box.z,box.w,3.13f,6.55f,.05f);
            for(int i=0;i<shell.C.Count;i++){Color c=shell.C[i]*.85f;c.a=1;shell.C[i]=c;}
            // The broad side keeps fitted stock. The front is completely replaced
            // below: no old full wall survives behind its blind opening.
            var side=new[]{shell.V[0],shell.V[1],shell.V[2],shell.V[3]};
            var light=new[]{shell.C[0],shell.C[1],shell.C[2],shell.C[3]};
            int sideStart=mesh.V.Count;
            GateCraftCourses(mesh,side,light,3.42f,0,71);
            GateRecessGrain(mesh,sideStart,mesh.V.Count,side[1]-side[0],0);
            GateCurtainFront(mesh,shell,box,far);
            // Original closed chamfer and all eight cap triangles are retained.
            for(int first=8;first<shell.V.Count;first+=4)
            {
                int start=mesh.V.Count;
                for(int i=0;i<4;i++){mesh.V.Add(shell.V[first+i]);mesh.U.Add(shell.U[first+i]);mesh.C.Add(shell.C[first+i]);}
                mesh.T.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
        }

        private static void GateCurtainFront(Builder mesh,Builder shell,Vector4 box,bool far)
        {
            // Existing relief station order runs along increasing V; the shell's
            // visible front runs the other way. Reorder it without moving points.
            var face=new[]{shell.V[5],shell.V[4],shell.V[7],shell.V[6]};
            Vector3[] outer=GateCurtainOpening(box,far,false),inner=GateCurtainOpening(box,far,true);
            float centre=far?.155f:.845f;
            Vector3 head=GateEnclosureFace(box,true,centre,6.55f,3.13f,6.55f,.05f);
            var border=new[]{face[0],face[1],face[2],head,face[3]};
            Vector3 inward=Project(box.x+.18f,box.y)-Project(box.x,box.y);
            Vector3 hub=Vector3.zero;foreach(Vector3 p in inner)hub+=p;hub=hub/5+inward;
            Color tone=shell.C[6];int first=mesh.V.Count;
            // Backing then returns, then front stock. The unchanged throat masks
            // the hidden portions naturally in the fixed painter view.
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                HeaderStonePatch(mesh,inner[i]+inward,inner[j]+inward,hub,hub,tone*.58f,face[0],face[1],face[3]);
            }
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                float gain=i==0?1.02f:i==1?.66f:i==2?.63f:i==3?.79f:.92f;
                HeaderStonePatch(mesh,inner[i],inner[j],inner[j]+inward,inner[i]+inward,
                    tone*gain,face[0],face[1],face[3]);
            }
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                GateRecessCoursedFace(mesh,border[i],border[j],outer[j],outer[i],tone*.93f,.030f,
                    i,i==1||i==4?3:1,true);
            }
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                float gain=i==0?1.10f:i==2||i==3?1.03f:.98f;
                GateRecessCoursedFace(mesh,outer[i],outer[j],inner[j],inner[i],tone*gain,.035f,
                    i+5,i==1||i==4?3:i==2||i==3?2:1,true);
            }
            // Physical light varies over the whole parent wall; fitted pieces do
            // not each restart their own illumination or mineral sample.
            for(int i=first;i<mesh.V.Count;i++)
            {
                Vector2 p=GateRecessCoordinates(face,mesh.V[i]);
                Color inherited=Color.Lerp(Color.Lerp(shell.C[5],shell.C[4],p.x),
                    Color.Lerp(shell.C[6],shell.C[7],p.x),p.y);
                Color c=mesh.C[i];
                mesh.C[i]=new Color(c.r*inherited.r/Mathf.Max(.01f,tone.r),
                    c.g*inherited.g/Mathf.Max(.01f,tone.g),c.b*inherited.b/Mathf.Max(.01f,tone.b),1);
            }
            GateRecessGrain(mesh,first,mesh.V.Count,face[1]-face[0],1);
        }

        private static float GateCurtainRoofHeight(float u,float a,float c)=>
            7.02f-.08f*Mathf.Abs((u-(a+c)*.5f)/((c-a)*.5f));

        private static void GateCurtainRoof(Builder mesh,Vector4 box)
        {
            const float bottom=6.82f,top=7.02f,inset=.11f;
            var shell=new Builder();LitFoundryTier(shell,box.x,box.y,box.z,box.w,bottom,top,inset);
            for(int i=0;i<shell.C.Count;i++){Color tone=shell.C[i]*.91f;tone.a=1;shell.C[i]=tone;}
            float a=box.x+inset,b=box.y+inset,c=box.z-inset,d=box.w-inset,middle=(a+c)*.5f;
            int first=mesh.V.Count;
            // Closed front gable and both side returns preserve all old bottom
            // stations. Only the eaves retreat inside the original top envelope.
            for(int face=0;face<3;face++)
            {
                int source=face*4,pieces=face==0?2:1;
                for(int piece=0;piece<pieces;piece++)
                {
                    float lo=piece/(float)pieces,hi=(piece+1f)/pieces;
                    var p=new[]{Vector3.Lerp(shell.V[source],shell.V[source+1],lo),
                        Vector3.Lerp(shell.V[source],shell.V[source+1],hi),
                        Vector3.Lerp(shell.V[source+3],shell.V[source+2],hi),
                        Vector3.Lerp(shell.V[source+3],shell.V[source+2],lo)};
                    var colors=new[]{Color.Lerp(shell.C[source],shell.C[source+1],lo),
                        Color.Lerp(shell.C[source],shell.C[source+1],hi),
                        Color.Lerp(shell.C[source+3],shell.C[source+2],hi),
                        Color.Lerp(shell.C[source+3],shell.C[source+2],lo)};
                    for(int end=2;end<4;end++)
                    {
                        Vector3 floor=p[end]-Vector3.up*top;
                        FoundrySurface.ToDesign(floor.x,floor.y,out float u,out _);
                        float height=GateCurtainRoofHeight(u,a,c);
                        p[end]+=Vector3.up*(height-top);
                        colors[end]=Color.Lerp(colors[end==2?1:0],colors[end],(height-bottom)/(top-bottom));
                    }
                    GateCraftCourses(mesh,p,colors,top-bottom,face,109);
                }
            }
            var outline=new List<Vector2>();
            for(int i=12;i<shell.V.Count;i+=4)
            {
                Vector3 floor=shell.V[i]-Vector3.up*top;
                FoundrySurface.ToDesign(floor.x,floor.y,out float u,out float v);outline.Add(new Vector2(u,v));
            }
            const int rows=3;const float gap=.008f;
            for(int row=rows-1;row>=0;row--)
            {
                float low=Mathf.Lerp(b,d,row/(float)rows),high=Mathf.Lerp(b,d,(row+1f)/rows);
                for(int slope=1;slope>=0;slope--)
                {
                    GateCurtainRoofPatch(mesh,outline,slope==0?a:middle,slope==0?middle:c,
                        low+(row==0?0:gap),high-(row==rows-1?0:gap),a,c,
                        shell.C[12]*(slope==0?.96f:.90f));
                    if(row>0)GateCurtainRoofPatch(mesh,outline,slope==0?a:middle,slope==0?middle:c,
                        low-gap,low+gap,a,c,shell.C[12]*.68f);
                }
            }
            GateRecessGrain(mesh,first,mesh.V.Count,Project(0,1)-Project(0,0),0);
        }

        private static void GateCurtainRoofPatch(Builder mesh,List<Vector2> outline,float left,float right,
            float low,float high,float roofLeft,float roofRight,Color tone)
        {
            var polygon=GateClipCoping(outline,0,left,true);polygon=GateClipCoping(polygon,0,right,false);
            polygon=GateClipCoping(polygon,1,low,true);polygon=GateClipCoping(polygon,1,high,false);
            if(polygon.Count<3)return;
            int first=mesh.V.Count;tone.a=1;
            foreach(Vector2 p in polygon)
            {
                mesh.V.Add(Project(p.x,p.y)+Vector3.up*GateCurtainRoofHeight(p.x,roofLeft,roofRight));
                mesh.U.Add(Vector2.zero);mesh.C.Add(tone);
            }
            for(int i=1;i+1<polygon.Count;i++)mesh.T.AddRange(new[]{first,first+i,first+i+1});
        }
    }
}
