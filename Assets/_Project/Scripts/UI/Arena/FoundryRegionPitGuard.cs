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
                    // No full opaque face crosses an opening. The coping owns the
                    // old handrail envelope; its stone top has no added bright rail.
                    PitGuardBlock(b,f0,f1,r0,r1,PitGuardLintel,RailTop,1.03f,false,false);
                }
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
