using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // The rim's service main, after the approved concept, whose boilers send heavy pipes along the court's rim: from the
    // north-west boiler's drum west over its service landing, round an elbow and on along the outside of the court's
    // north rail, carried on iron brackets off the rim, banded in brass. It stands wholly off the walking union, behind
    // the rail: in the masonry batch (-11), emitted before the massing's rail, posts and lanterns, which paint over its
    // lower part, as the rim clutter does (EmitClutter's clean-patch colour mapping, toned as rolled service iron).
    public sealed partial class FoundryRegionView
    {
        // The run's centreline (design) and height: out of the drum over the landing's barrels, then behind the rail clear
        // of its lantern posts (North + .72), ending in a blind flange toward the court's north corner.
        private const float RimPipeHeight=1.45f,RimPipeWidth=.66f,RimPipeV=FoundryMap.North+1.2f;
        private static readonly Vector2[] RimPipeRun={new Vector2(16.55f,16.9f),new Vector2(14.95f,16.9f),
            new Vector2(14.95f,RimPipeV),new Vector2(-3.5f,RimPipeV)};

        // A lighter return line runs beside the main, farther out and lower.
        private const float RimReturnWidth=.34f,RimReturnHeight=1.05f,RimReturnV=RimPipeV+.72f;

        private void BuildRimPipes(ArenaView arena)
        {
            var mesh=new Builder();
            // The return first: it lies behind the main.
            {
                Vector3 a=Project(RimPipeRun[2].x+.72f,RimReturnV)+Vector3.up*RimReturnHeight;
                Vector3 b=Project(RimPipeRun[3].x+1.2f,RimReturnV)+Vector3.up*RimReturnHeight;
                RimPipeSection(mesh,a,b,RimReturnWidth*.5f,RimReturnWidth*.5f,false);
                int bands=Mathf.Max(1,Mathf.RoundToInt(Vector3.Distance(a,b)/2.6f));
                Vector3 d=(b-a).normalized;
                for(int k=0;k<=bands;k++)
                {
                    Vector3 p=Vector3.Lerp(a,b,k/(float)bands);
                    RimPipeSection(mesh,p-d*.05f,p+d*.05f,RimReturnWidth*.64f,RimReturnWidth*.64f,true);
                }
            }
            for(int i=0;i+1<RimPipeRun.Length;i++)
            {
                Vector3 a=Project(RimPipeRun[i].x,RimPipeRun[i].y)+Vector3.up*RimPipeHeight;
                Vector3 b=Project(RimPipeRun[i+1].x,RimPipeRun[i+1].y)+Vector3.up*RimPipeHeight;
                RimPipeSection(mesh,a,b,RimPipeWidth*.5f,RimPipeWidth*.5f,false);
                // Brass bands every ~2.2 units, and broader collars at either end of a straight.
                float length=Vector3.Distance(a,b);
                int bands=Mathf.Max(1,Mathf.RoundToInt(length/2.2f));
                Vector3 d=(b-a).normalized;
                for(int k=0;k<=bands;k++)
                {
                    Vector3 p=Vector3.Lerp(a,b,k/(float)bands);
                    bool end=k==0||k==bands;
                    float half=RimPipeWidth*(end?.2f:.12f);
                    RimPipeSection(mesh,p-d*half,p+d*half,RimPipeWidth*(end?.72f:.62f),RimPipeWidth*(end?.72f:.62f),true);
                }
            }
            // Brackets off the rim under the long run: an iron arm from the rail band out under the pipe, every ~3.5 units.
            Vector2 from=RimPipeRun[2],to=RimPipeRun[3];
            int arms=Mathf.Max(2,Mathf.RoundToInt(Vector2.Distance(from,to)/3.5f));
            for(int k=1;k<arms;k++)
            {
                float u=Mathf.Lerp(from.x,to.x,k/(float)arms);
                Vector4 arm=new Vector4(u-.09f,FoundryMap.North+RailBand,u+.09f,RimPipeV+.14f);
                MassBox(mesh,arm,RimPipeHeight-RimPipeWidth*.5f-.16f,RimPipeHeight-RimPipeWidth*.5f,true,true,.55f);
            }
            // The run's far end: a blind flange.
            {
                Vector3 end=Project(to.x,to.y)+Vector3.up*RimPipeHeight,d=(end-(Project(from.x,from.y)+Vector3.up*RimPipeHeight)).normalized;
                RimPipeSection(mesh,end-d*.05f,end+d*.05f,RimPipeWidth*.8f,RimPipeWidth*.8f,true);
            }
            for(int i=0;i<mesh.C.Count;i++)mesh.C[i]=Tinted(mesh.C[i],IronPatchMean);
            EmitClutter(arena,mesh,false,Project(6,RimPipeV)+Vector3.up*RimPipeHeight);
        }

        // A horizontal pipe section's near half (the camera sees no more), rolled-iron or brass toned as the service mains.
        private static void RimPipeSection(Builder mesh,Vector3 a,Vector3 b,float radiusA,float radiusB,bool brass)
        {
            Vector3 axis=(b-a).normalized,normal=new Vector3(-axis.y,axis.x,0);
            if(Vector2.Dot(new Vector2(normal.x,normal.y),ChainLight)<0)normal=-normal;
            FinishedPipeSection(mesh,a,b,normal,radiusA,radiusB,RimPipeHeight,brass?1.05f:1f,0,1,brass);
        }
    }
}
