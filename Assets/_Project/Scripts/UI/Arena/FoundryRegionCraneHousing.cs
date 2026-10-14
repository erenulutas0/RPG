using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private static Vector4 CraneMachineryFoot=>WestBox(-26.12f,11.22f,-24.18f,14.70f);

        private void BuildCraneMachineryBase(ArenaView arena)
        {
            // This grounded assembly is shared by the authored crane and fallback.
            // Its broad lower mass surrounds the existing mast's lower bearing;
            // the card source, upper pivot and live hoist anchors stay unchanged.
            using(MassDepth(WestBox(-27,11,-24,15),8.6f))
            {
                var body=new Builder();
                BuildCraneMachineryCasting(body);
                Create("Grounded crane winch casting",body,arena.PlatformMaterials.FoundryIron,-11);
            }
        }

        private static void BuildCraneMachineryCasting(Builder body)
        {
            Vector4 box=CraneMachineryFoot;
            // One low sole is seated on the existing iron deck, clear of the
            // barrel/crate corner. The rear casing carries the original mast.
            WorkshopMetalHull(body,box.x,box.y,box.z,box.w,1.12f,1.32f,.045f,true);
            WorkshopMetalHull(body,box.x+.08f,12.40f,box.z-.08f,box.w-.07f,1.32f,2.21f,.06f,true);
            CraneMachineryHood(body,box);
            // Backing and roof precede the exposed drum. The near cheeks come
            // last; there is no solid face painted across the service opening.
            Vector3 backA=Project(box.x+.34f,12.40f)+Vector3.up*1.40f;
            Vector3 backB=Project(box.z-.34f,12.40f)+Vector3.up*1.40f;
            CraneMachineryFace(body,backA,backB,backB+Vector3.up*.70f,backA+Vector3.up*.70f,
                new Color(.20f,.24f,.31f),1.40f,2.10f);
            // Higher design-u is the far bearing. Painting both cheeks after the
            // drum lets that far side wall wrongly cover the exposed barrel.
            CraneMachineryCheek(body,box.z-.41f);
            BuildCraneMachineryDrum(body);
            CraneMachineryCheek(body,box.x+.08f);
            CraneMachineryHeader(body,box);
            FinishCraneMachinery(body);
        }

        private static void CraneMachineryCheek(Builder body,float u)
        {
            Vector4 box=CraneMachineryFoot;
            WorkshopMetalHull(body,u,box.y+.07f,u+.33f,12.12f,1.32f,2.17f,.055f,true);
            YardTrestleGusset(body,u+.03f,u+.30f,box.y+.20f,12.45f,1.83f,2.26f);
        }

        private static void CraneMachineryHeader(Builder body,Vector4 box)
        {
            // Broad flat end seats bear on the bevelled cheek caps. The middle
            // soffit rises, making an actual opening in the fixed projection;
            // a full-depth rectangular lintel hid almost all of the drum.
            float[] u={box.x+.15f,box.x+.34f,box.x+.60f,box.z-.60f,box.z-.34f,box.z-.15f};
            float[] bottom={2.17f,2.17f,2.40f,2.40f,2.17f,2.17f};
            float front=box.y+.14f;const float back=12.46f,top=2.56f;
            for(int i=0;i<u.Length-1;i++)
            {
                CraneMachineryFace(body,Project(u[i],back)+Vector3.up*bottom[i],
                    Project(u[i+1],back)+Vector3.up*bottom[i+1],Project(u[i+1],back)+Vector3.up*top,
                    Project(u[i],back)+Vector3.up*top,new Color(.31f,.35f,.43f),bottom[i],top);
                CraneMachineryFace(body,Project(u[i],front)+Vector3.up*bottom[i],
                    Project(u[i+1],front)+Vector3.up*bottom[i+1],Project(u[i+1],back)+Vector3.up*bottom[i+1],
                    Project(u[i],back)+Vector3.up*bottom[i],new Color(.23f,.27f,.34f),bottom[i],bottom[i+1]);
            }
            foreach(int i in new[]{0,u.Length-1})
                CraneMachineryFace(body,Project(u[i],front)+Vector3.up*bottom[i],
                    Project(u[i],back)+Vector3.up*bottom[i],Project(u[i],back)+Vector3.up*top,
                    Project(u[i],front)+Vector3.up*top,new Color(.35f,.40f,.49f),bottom[i],top);
            CraneMachineryFace(body,Project(u[0],front)+Vector3.up*top,Project(u[u.Length-1],front)+Vector3.up*top,
                Project(u[u.Length-1],back)+Vector3.up*top,Project(u[0],back)+Vector3.up*top,
                new Color(.58f,.63f,.71f),top,top);
            for(int i=0;i<u.Length-1;i++)
                CraneMachineryFace(body,Project(u[i],front)+Vector3.up*bottom[i],
                    Project(u[i+1],front)+Vector3.up*bottom[i+1],Project(u[i+1],front)+Vector3.up*top,
                    Project(u[i],front)+Vector3.up*top,new Color(.44f,.49f,.57f),bottom[i],top);
        }

        private static void CraneMachineryHood(Builder body,Vector4 box)
        {
            var lower=new[]{Project(box.x+.08f,12.40f),Project(box.z-.08f,12.40f),
                Project(box.z-.08f,box.w-.07f),Project(box.x+.08f,box.w-.07f)};
            var upper=new[]{Project(box.x+.24f,12.67f),Project(box.z-.24f,12.67f),
                Project(box.z-.24f,box.w-.25f),Project(box.x+.24f,box.w-.25f)};
            // A single long sloping shell gives the group a cast profile, with
            // an uninterrupted upper seat under the fixed crane mast.
            for(int edge=0;edge<4;edge++)
            {
                int next=(edge+1)%4;
                Color tone=edge==0?new Color(.51f,.56f,.64f):new Color(.35f,.40f,.49f);
                CraneMachineryFace(body,lower[edge]+Vector3.up*2.21f,lower[next]+Vector3.up*2.21f,
                    upper[next]+Vector3.up*2.60f,upper[edge]+Vector3.up*2.60f,tone,2.21f,2.60f);
            }
            CraneMachineryFace(body,upper[0]+Vector3.up*2.60f,upper[1]+Vector3.up*2.60f,
                upper[2]+Vector3.up*2.60f,upper[3]+Vector3.up*2.60f,new Color(.56f,.60f,.67f),2.60f,2.60f);
        }

        private static void BuildCraneMachineryDrum(Builder body)
        {
            Vector4 box=CraneMachineryFoot;
            float left=box.x+.35f,right=box.z-.35f;
            const float v=11.88f,axis=1.77f,radius=.30f;
            // A real cylinder in design-v/elevation, borne at both ends by the
            // side cheeks. The farther flange precedes the barrel and near end.
            CraneMachineryDrumEnd(body,right,v,axis);
            int[] sectors={0,1,2,3,12,13,14,15,4,5,6,7,8,9,10,11};
            foreach(int sector in sectors)
            {
                float a=sector*Mathf.PI/8,b=(sector+1)*Mathf.PI/8;
                float va=v+Mathf.Cos(a)*radius,vb=v+Mathf.Cos(b)*radius;
                float ha=axis+Mathf.Sin(a)*radius,hb=axis+Mathf.Sin(b)*radius;
                float light=.39f+.24f*Mathf.Max(0,Mathf.Sin((a+b)*.5f));
                CraneMachineryFace(body,Project(left,va)+Vector3.up*ha,Project(right,va)+Vector3.up*ha,
                    Project(right,vb)+Vector3.up*hb,Project(left,vb)+Vector3.up*hb,
                    new Color(light*.91f,light*.96f,light),ha,hb);
            }
            // Three broad wound turns are connected to the drum's actual curved
            // shell. They read as cable without scattering small loose machinery.
            foreach(float u in new[]{left+.25f,left+.59f,left+.93f})
                foreach(int sector in sectors)
                {
                    float a=sector*Mathf.PI/8,b=(sector+1)*Mathf.PI/8;
                    float va=v+Mathf.Cos(a)*.304f,vb=v+Mathf.Cos(b)*.304f;
                    float ha=axis+Mathf.Sin(a)*.304f,hb=axis+Mathf.Sin(b)*.304f;
                    CraneMachineryFace(body,Project(u-.035f,va)+Vector3.up*ha,Project(u+.035f,va)+Vector3.up*ha,
                        Project(u+.035f,vb)+Vector3.up*hb,Project(u-.035f,vb)+Vector3.up*hb,
                        new Color(.27f,.30f,.36f),ha,hb);
                }
            CraneMachineryDrumEnd(body,left,v,axis);
        }

        private static void CraneMachineryDrumEnd(Builder body,float u,float v,float axis)
        {
            // Closed end discs and a broad annular flange use the same
            // physical axis as the barrel, not a flat decal beside it.
            Vector3 centre=Project(u,v)+Vector3.up*axis;
            for(int sector=0;sector<16;sector++)
            {
                float a=sector*Mathf.PI/8,b=(sector+1)*Mathf.PI/8;
                Vector3 A=Project(u,v+Mathf.Cos(a)*.35f)+Vector3.up*(axis+Mathf.Sin(a)*.35f);
                Vector3 B=Project(u,v+Mathf.Cos(b)*.35f)+Vector3.up*(axis+Mathf.Sin(b)*.35f);
                CraneMachineryFace(body,centre,A,B,centre,new Color(.38f,.42f,.50f),axis,axis);
                body.T.RemoveRange(body.T.Count-3,3);
                CraneMachineryFace(body,Vector3.Lerp(centre,A,.76f),A,B,Vector3.Lerp(centre,B,.76f),
                    new Color(.68f,.60f,.46f),axis-.35f,axis+.35f);
            }
        }

        private static void CraneMachineryFace(Builder body,Vector3 a,Vector3 b,Vector3 c,Vector3 d,
            Color tone,float low,float high)
        {
            int first=body.V.Count;
            FullFace(body,a,b,c,d,tone);
            WorkedStockFace(body,first,Project(CraneMachineryFoot.x,CraneMachineryFoot.y),1.45f,low,high);
        }

        private static void FinishCraneMachinery(Builder body)
        {
            Vector4 box=CraneMachineryFoot;
            Vector3 centre=Project((box.x+box.z)*.5f,(box.y+box.w)*.5f)+Vector3.up*1.86f;
            for(int i=0;i<body.V.Count;i++)
            {
                // Shared stock mapping clamps elevated faces onto one atlas row.
                // A centered, common field keeps real 2D grain over every plane,
                // including the broad roof; no face UV is clamped or stretched.
                Vector3 local=body.V[i]-centre;
                body.U[i]=new Vector2(.50f+local.x*.105f,.50f+local.y*.105f);
                Color old=body.C[i];
                float value=old.r*.299f+old.g*.587f+old.b*.114f;
                float aged=.83f+.045f*FloorPatina(local.x*.68f+3.1f,local.y*.68f-2.7f);
                float foot=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.65f,-.18f,local.y));
                float shoulder=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.20f,.68f,local.y));
                float contact=1-foot*.10f+shoulder*.045f;
                // The working pocket shares cool cast iron with the crane and
                // transfer bench. Oil-dark lower contact and a lightly rubbed
                // shoulder retain depth, without globally raising the casting.
                // Existing fire-facing warmth stays a restrained local rust bias.
                float warmth=Mathf.Max(0,old.r-old.b)*.22f;
                body.C[i]=new Color((value*.97f+warmth)*aged*contact,value*aged*contact,
                    (value*1.055f-warmth*.14f)*aged*contact,1);
            }
        }

        private void BuildCraneHousing(ArenaView arena,Vector3 foot)
        {
            // The original uprights already spanned +/-0.45 screen units. Keep the
            // new cast body inside that silhouette and the existing crane landmark.
            var body=new Builder();
            float[] rows={.21f,1.25f,2.4f,3.5f,4.45f};
            for(int row=0;row+1<rows.Length;row++)
            {
                float low=rows[row],high=rows[row+1];
                float r0=CraneHousingHalf(low),r1=CraneHousingHalf(high);
                // Broad front and two chamfered side planes; the rear is never
                // visible in the fixed projection. Long samples retain rolled grain.
                CraneHousingFace(body,foot,new Vector3(-r0,low+.09f,0),new Vector3(-r0*.60f,low,0),
                    new Vector3(-r1*.60f,high,0),new Vector3(-r1,high+.09f,0),new Color(.38f,.43f,.52f));
                CraneHousingFace(body,foot,new Vector3(r0*.55f,low,0),new Vector3(r0,low+.09f,0),
                    new Vector3(r1,high+.09f,0),new Vector3(r1*.55f,high,0),new Color(.46f,.48f,.53f));
                CraneHousingFace(body,foot,new Vector3(-r0*.60f,low,0),new Vector3(r0*.55f,low,0),
                    new Vector3(r1*.55f,high,0),new Vector3(-r1*.60f,high,0),new Color(.83f,.84f,.85f));
            }
            // Upper shoulder carries the unchanged open bearing section, without
            // widening beyond the old mast. Its dark return makes one deliberate joint.
            CraneHousingFace(body,foot,new Vector3(-.45f,4.42f,0),new Vector3(.45f,4.42f,0),
                new Vector3(.45f,4.6f,0),new Vector3(-.45f,4.6f,0),new Color(.56f,.51f,.43f));
            CraneHousingFace(body,foot,new Vector3(-.45f,4.42f,0),new Vector3(.45f,4.42f,0),
                new Vector3(.45f,4.46f,0),new Vector3(-.45f,4.46f,0),new Color(.20f,.23f,.30f));
            // One tall access hatch replaces repeated lattice bays. Four broad
            // reveals describe its recess; it is equipment, not another tiny grille.
            Vector3 a=new Vector3(-.20f,1.73f,0),b=new Vector3(.18f,1.73f,0);
            Vector3 c=new Vector3(.16f,4.14f,0),d=new Vector3(-.18f,4.14f,0);
            Vector3 ia=a+new Vector3(.035f,.09f,0),ib=b+new Vector3(-.035f,.09f,0);
            Vector3 ic=c+new Vector3(-.035f,-.09f,0),id=d+new Vector3(.035f,-.09f,0);
            CraneHousingFace(body,foot,ia,ib,ic,id,new Color(.38f,.41f,.48f));
            CraneHousingFace(body,foot,a,b,ib,ia,new Color(.82f,.78f,.66f));
            CraneHousingFace(body,foot,b,c,ic,ib,new Color(.58f,.61f,.67f));
            CraneHousingFace(body,foot,c,d,id,ic,new Color(.19f,.22f,.29f));
            CraneHousingFace(body,foot,d,a,ia,id,new Color(.28f,.32f,.41f));
            CraneHousingFace(body,foot,new Vector3(.075f,2.68f,0),new Vector3(.12f,2.68f,0),
                new Vector3(.12f,2.94f,0),new Vector3(.075f,2.94f,0),new Color(.86f,.72f,.49f));
            CraneHousingWinch(body,foot);
            Create("Cast crane hoist housing",body,arena.PlatformMaterials.FoundryIron,-11);
        }

        private static float CraneHousingHalf(float height)=>Mathf.Lerp(.44f,.36f,
            Mathf.InverseLerp(.21f,4.45f,height));

        private static void CraneHousingFace(Builder mesh,Vector3 foot,Vector3 a,Vector3 b,Vector3 c,
            Vector3 d,Color tone)
        {
            int first=mesh.V.Count;
            FullFace(mesh,foot+a,foot+b,foot+c,foot+d,tone);
            for(int i=0;i<4;i++)
            {
                Vector3 local=mesh.V[first+i]-foot;
                // One continuous authored interior, never the panel's rivet border.
                mesh.U[first+i]=new Vector2(.50f+local.x*.46f,.19f+local.y*.135f);
                Color shaded=tone;
                float scale=.88f+.12f*Mathf.Clamp01(local.y/.8f);
                shaded.r*=scale;shaded.g*=scale;shaded.b*=scale;shaded.a=1;
                mesh.C[first+i]=MetalTone(shaded,mesh.V[first+i],1.1f+local.y);
            }
        }

        private static void CraneHousingWinch(Builder mesh,Vector3 foot)
        {
            // Horizontal cable reel in the lower service opening. End discs stay
            // within +/-0.32, well inside the unchanged +/-0.45 mast envelope.
            const float axisHeight=1.07f,radius=.23f,half=.24f;
            for(int strip=0;strip<8;strip++)
            {
                float t0=strip/8f,t1=(strip+1f)/8f;
                float y0=axisHeight+Mathf.Cos(t0*Mathf.PI)*radius;
                float y1=axisHeight+Mathf.Cos(t1*Mathf.PI)*radius;
                float value=.34f+.55f*Mathf.Max(0,Mathf.Sin((t0+.20f)*Mathf.PI));
                CraneHousingFace(mesh,foot,new Vector3(-half,y0,0),new Vector3(half,y0,0),
                    new Vector3(half,y1,0),new Vector3(-half,y1,0),new Color(value*.90f,value*.92f,value));
            }
            // A few broad wound turns read as cable, rather than another riveted plate.
            foreach(float x in new[]{-.15f,-.05f,.05f,.15f})
                CraneHousingFace(mesh,foot,new Vector3(x-.012f,axisHeight-radius*.90f,0),
                    new Vector3(x+.012f,axisHeight-radius*.90f,0),new Vector3(x+.012f,axisHeight+radius*.90f,0),
                    new Vector3(x-.012f,axisHeight+radius*.90f,0),new Color(.25f,.27f,.32f));
            foreach(float side in new[]{-1f,1f})
            {
                Vector3 centre=new Vector3(side*half,axisHeight,0);
                for(int i=0;i<16;i++)
                {
                    float a=i*Mathf.PI/8f,e=(i+1)*Mathf.PI/8f;
                    Vector3 outerA=new Vector3(Mathf.Cos(a)*.075f,Mathf.Sin(a)*.29f,0);
                    Vector3 outerB=new Vector3(Mathf.Cos(e)*.075f,Mathf.Sin(e)*.29f,0);
                    float light=.62f+.20f*Mathf.Max(0,Mathf.Sin((a+e)*.5f));
                    CraneHousingFace(mesh,foot,centre+outerA,centre+outerB,
                        centre+outerB*.70f,centre+outerA*.70f,new Color(light,light*.84f,light*.61f));
                    CraneHousingFace(mesh,foot,centre+outerA*.70f,centre+outerB*.70f,
                        centre,centre,new Color(.28f,.32f,.40f));
                }
            }
        }
    }
}
