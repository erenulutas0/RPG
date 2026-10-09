using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
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
