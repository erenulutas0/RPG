using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // The pit is an industrial stone enclosure, not the turned balustrade used
        // by the surviving corner returns. Keep its exact curved band, eight posts
        // and depth owners. Each bay is now carried by a seated sill, two broad
        // openings and one recessed stone panel beneath a substantial coping.
        private const float PitGuardSill=.26f,PitGuardLintel=.59f;

        private static void PitGuardRun(Builder b,Vector2[][] lines,bool near)
        {
            Vector2[] front=lines[0],axis=lines[1],back=lines[2];
            int chords=axis.Length-1;
            var along=new float[axis.Length];
            for(int i=1;i<=chords;i++)along[i]=along[i-1]+Vector2.Distance(axis[i-1],axis[i]);
            float length=along[chords],centre=length*.5f;
            const float respond=.12f,panelHalf=.29f,shoulder=.065f,barHalf=.027f;
            float leftBar=(respond+centre-panelHalf)*.5f,rightBar=length-leftBar;
            var divisions=new[]{respond,leftBar-barHalf,leftBar+barHalf,centre-panelHalf,
                centre-panelHalf+shoulder,centre+panelHalf-shoulder,centre+panelHalf,
                rightBar-barHalf,rightBar+barHalf,length-respond};
            bool fromEnd=front[0].x+front[0].y<front[chords].x+front[chords].y;
            for(int step=0;step<chords;step++)
            {
                int j=fromEnd?chords-1-step:step;
                var cuts=new List<float>{along[j],along[j+1]};
                foreach(float cut in divisions)if(cut>along[j]+.00001f&&cut<along[j+1]-.00001f)cuts.Add(cut);
                cuts.Sort();
                for(int part=0;part+1<cuts.Count;part++)
                {
                    int k=fromEnd?cuts.Count-2-part:part;
                    float s0=cuts[k],s1=cuts[k+1],mid=(s0+s1)*.5f;
                    float a=(s0-along[j])/(along[j+1]-along[j]);
                    float z=(s1-along[j])/(along[j+1]-along[j]);
                    Vector2 f0=Vector2.Lerp(front[j],front[j+1],a),f1=Vector2.Lerp(front[j],front[j+1],z);
                    Vector2 r0=Vector2.Lerp(back[j],back[j+1],a),r1=Vector2.Lerp(back[j],back[j+1],z);
                    // A continuous bearing takes the weight; its lower near face
                    // starts at the existing collar rather than painting over it.
                    PitGuardBlock(b,f0,f1,r0,r1,near?RailPlinth:0,PitGuardSill,.96f,false,false);
                    bool end=mid<respond||mid>length-respond;
                    bool panel=Mathf.Abs(mid-centre)<panelHalf;
                    bool iron=Mathf.Abs(mid-leftBar)<barHalf||Mathf.Abs(mid-rightBar)<barHalf;
                    if(end||panel)
                    {
                        bool recessed=panel&&Mathf.Abs(mid-centre)<panelHalf-shoulder;
                        float reveal=recessed?.19f:0;
                        PitGuardBlock(b,Vector2.Lerp(f0,r0,reveal),Vector2.Lerp(f1,r1,reveal),r0,r1,
                            PitGuardSill,PitGuardLintel,recessed?.72f:1f,false,true);
                    }
                    else if(iron)
                    {
                        PitGuardBlock(b,Vector2.Lerp(f0,r0,.40f),Vector2.Lerp(f1,r1,.40f),
                            Vector2.Lerp(f0,r0,.65f),Vector2.Lerp(f1,r1,.65f),
                            PitGuardSill,PitGuardLintel,.46f,true,true);
                    }
                }
                // Coping stock follows the whole bay, independently of the small
                // cuts needed by the iron stems and recessed panels below it.
                // It retains the original front and top planes and depth order.
                PitGuardCoping(b,front[j],front[j+1],back[j],back[j+1],along[j],along[j+1],length,
                    Mathf.RoundToInt(front[0].x*11+front[0].y*17),fromEnd);
            }
        }

        private static void PitGuardCoping(Builder b,Vector2 f0,Vector2 f1,Vector2 r0,Vector2 r1,
            float start,float end,float length,int seed,bool fromEnd)
        {
            int stones=Mathf.Max(2,Mathf.RoundToInt(length/1.2f));
            float stock=length/stones;
            const float halfJoint=.009f;
            var cuts=new List<float>{start,end};
            for(int stone=1;stone<stones;stone++)
                foreach(float edge in new[]{stone*stock-halfJoint,stone*stock+halfJoint})
                    if(edge>start+.00001f&&edge<end-.00001f)cuts.Add(edge);
            // An uncut coping stone otherwise has only its end vertices, both
            // outside the worn shoulder. Include the selected shoulder itself so
            // actual mesh colours can carry it, not merely an unsampled function.
            for(int stone=0;stone<stones;stone++)
            {
                int cut=Mathf.Abs(seed+stone*7);if(cut%4!=1)continue;
                float shoulder=(stone+.32f+(cut%3)*.12f)*stock;
                if(shoulder>start+.00001f&&shoulder<end-.00001f)cuts.Add(shoulder);
            }
            cuts.Sort();
            Vector2 outward=((f0+f1)-(r0+r1)).normalized;
            Color face=PitFacing(Mathf.Atan2(outward.y,outward.x))*1.03f;face.a=1;
            Color top=Tinted(CapTone,new Color(.9682f,.9682f,.9682f));
            for(int step=0;step+1<cuts.Count;step++)
            {
                int i=fromEnd?cuts.Count-2-step:step;
                float a=cuts[i],z=cuts[i+1],mid=(a+z)*.5f;
                // Cuts originate in one dimension; equal boundaries have no area
                // and must never become projected rounding slivers.
                if(z<=a)continue;
                float s=(a-start)/(end-start),t=(z-start)/(end-start);
                Vector2 left=Vector2.Lerp(f0,f1,s),right=Vector2.Lerp(f0,f1,t);
                Vector2 rearLeft=Vector2.Lerp(r0,r1,s),rearRight=Vector2.Lerp(r0,r1,t);
                int stone=Mathf.Clamp(Mathf.FloorToInt(mid/stock),0,stones-1);
                int joint=Mathf.RoundToInt(mid/stock);
                bool mortar=joint>0&&joint<stones&&Mathf.Abs(mid-joint*stock)<halfJoint+.000001f;
                float offset=stone*stock;
                int cut=Mathf.Abs(seed+stone*7);
                float variant=1+(cut%5-2)*.012f;
                var light=face*variant;light.a=1;
                var cap=top*variant;cap.a=1;
                if(mortar)
                {
                    PitCopingFront(b,left,right,a,z,offset,PitGuardLintel,RailTop,light*.60f,light*.60f,outward,0,0);
                    PitCopingTop(b,left,right,rearLeft,rearRight,a,z,offset,0,1,cap*.63f,cap*.63f,0,0);
                    continue;
                }
                float wearA=PitCopingWear((a-offset)/stock,cut),wearZ=PitCopingWear((z-offset)/stock,cut);
                const float arris=.026f;
                // The quiet upper arris and worn shoulder replace portions of the
                // existing planes. There is no complete face below another decal.
                PitCopingFront(b,left,right,a,z,offset,PitGuardLintel,RailTop-arris,
                    light*.86f,light*.96f,outward,0,0);
                PitCopingFront(b,left,right,a,z,offset,RailTop-arris,RailTop,
                    light*.96f,light*.99f,outward,wearA,wearZ);
                PitCopingTop(b,left,right,rearLeft,rearRight,a,z,offset,0,.22f,
                    cap*.99f,cap,wearA,wearZ);
                PitCopingTop(b,left,right,rearLeft,rearRight,a,z,offset,.22f,1,
                    cap,cap*.97f,0,0);
            }
        }

        private static float PitCopingWear(float along,int cut)
        {
            // Only selected broad stones carry a worn shoulder. It fades within
            // that stone, avoiding an equally bright dashed outline around the pit.
            if(cut%4!=1)return 0;
            float position=.32f+(cut%3)*.12f;
            float amount=Mathf.Clamp01(1-Mathf.Abs(along-position)/.34f);
            return .09f*amount*amount;
        }

        private static Vector2 PitCopingGrain(float along,float depth,float height)=>
            new Vector2(.045f+along*.095f,.448f+(height-PitGuardLintel+depth)*.095f);

        private static void PitCopingFront(Builder b,Vector2 left,Vector2 right,float a,float z,float offset,
            float bottom,float top,Color lower,Color upper,Vector2 outward,float wearA,float wearZ)
        {
            Vector3 p=Project(left.x,left.y),q=Project(right.x,right.y);
            int first=b.V.Count;
            FullFace(b,p+Vector3.up*bottom,q+Vector3.up*bottom,q+Vector3.up*top,p+Vector3.up*top,Color.white);
            Vector2 toward=(PitCentre-(left+right)*.5f).normalized;
            float facing=Mathf.Max(0,Vector2.Dot(outward,toward));
            var rise=new[]{bottom,bottom,top,top};
            var run=new[]{a,z,z,a};
            for(int i=0;i<4;i++)
            {
                float bounce=facing*.10f*Mathf.Exp(-rise[i]/.7f);
                Color tone=i<2?lower:upper;
                float wear=i==2?wearZ:i==3?wearA:0;
                b.C[first+i]=Tinted(tone,new Color((1+bounce)*(1+wear),(1+bounce*.28f)*(1+wear),
                    (1-bounce*.24f)*(1+wear)));
                b.C[first+i]=new Color(b.C[first+i].r,b.C[first+i].g,b.C[first+i].b,1);
                b.U[first+i]=PitCopingGrain(run[i]-offset,0,rise[i]);
            }
        }

        private static void PitCopingTop(Builder b,Vector2 left,Vector2 right,Vector2 rearLeft,Vector2 rearRight,
            float a,float z,float offset,float front,float back,Color near,Color far,float wearA,float wearZ)
        {
            Vector2 p=Vector2.Lerp(left,rearLeft,front),q=Vector2.Lerp(right,rearRight,front);
            Vector2 r=Vector2.Lerp(right,rearRight,back),s=Vector2.Lerp(left,rearLeft,back);
            int first=b.V.Count;
            FullFace(b,Project(p.x,p.y)+Vector3.up*RailTop,Project(q.x,q.y)+Vector3.up*RailTop,
                Project(r.x,r.y)+Vector3.up*RailTop,Project(s.x,s.y)+Vector3.up*RailTop,Color.white);
            float leftDepth=Vector2.Distance(left,rearLeft),rightDepth=Vector2.Distance(right,rearRight);
            var depth=new[]{leftDepth*front,rightDepth*front,rightDepth*back,leftDepth*back};
            var run=new[]{a,z,z,a};
            for(int i=0;i<4;i++)
            {
                Color tone=i<2?near:far;
                float wear=i==0?wearA:i==1?wearZ:0;
                b.C[first+i]=new Color(tone.r*(1+wear),tone.g*(1+wear),tone.b*(1+wear),1);
                b.U[first+i]=PitCopingGrain(run[i]-offset,depth[i],RailTop);
            }
        }

        private static void PitGuardBlock(Builder b,Vector2 f0,Vector2 f1,Vector2 r0,Vector2 r1,
            float bottom,float top,float gain,bool iron,bool returns)
        {
            Vector2 outward=((f0+f1)-(r0+r1)).normalized;
            // Back faces are occluded by the body in this fixed projection. Emit
            // the two end returns before the camera-facing plane and cap so the
            // recess has physical depth without a dark overlay across the bay.
            if(returns)
            {
                PitGuardFace(b,r0,f0,bottom,top,-(f1-f0).normalized,gain*.79f,iron);
                PitGuardFace(b,f1,r1,bottom,top,(f1-f0).normalized,gain*.87f,iron);
            }
            PitGuardFace(b,f0,f1,bottom,top,outward,gain,iron);
            Vector3 a=Project(f0.x,f0.y)+Vector3.up*top,c=Project(f1.x,f1.y)+Vector3.up*top;
            Vector3 d=Project(r1.x,r1.y)+Vector3.up*top,e=Project(r0.x,r0.y)+Vector3.up*top;
            int first=b.V.Count;
            FullFace(b,a,c,d,e,Tinted(CapTone,new Color(gain*.94f,gain*.94f,gain*.94f)));
            for(int i=0;i<4;i++)
            {
                Vector3 p=b.V[first+i];
                b.U[first+i]=iron?new Vector2(.47f+(p.x-a.x)*.08f,.49f+(p.y-a.y)*.08f):
                    new Vector2(p.x/2.8f,i<2?HandrailCourse.y:HandrailCourse.x);
            }
        }

        private static void PitGuardFace(Builder b,Vector2 a,Vector2 c,float bottom,float top,
            Vector2 normal,float gain,bool iron)
        {
            Vector3 p=Project(a.x,a.y),q=Project(c.x,c.y);
            int first=b.V.Count;
            Color tone=PitFacing(Mathf.Atan2(normal.y,normal.x))*gain;tone.a=1;
            if(iron)tone=Tinted(tone,new Color(.80f,.88f,.99f));
            CourseFace(b,p,q,bottom,top,tone,PlinthCourse);
            // Only the existing pool-facing surfaces receive this weak lower
            // bounce. It describes the returns, without brightening the court or
            // casting a second orange sheet over the established pit illumination.
            Vector2 toward=(PitCentre-(a+c)*.5f).normalized;
            float facing=Mathf.Max(0,Vector2.Dot(normal,toward));
            for(int i=0;i<4;i++)
            {
                float height=i<2?bottom:top;
                float bounce=facing*.10f*Mathf.Exp(-height/.7f);
                b.C[first+i]=Tinted(tone,new Color(1+bounce,1+bounce*.28f,1-bounce*.24f));
                if(iron)b.U[first+i]=new Vector2(.47f+(b.V[first+i].x-p.x)*.08f,
                    .49f+(height-bottom)*.08f);
            }
        }
    }
}
