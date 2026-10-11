using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private Material _bakedBoilerMaterial,_bakedShortBoilerMaterial;

        // Each boiler pair shares one source material and two static batches.
        // Imported bakes are asset-owned; source materials and meshes belong to this scene.
        // Keep the existing footprint, foundation, pipe anchors and atmosphere source.
        private bool BuildBakedBoiler(ArenaView arena,Vector2 centre,bool squat=false)
        {
            ref Material material=ref (squat?ref _bakedShortBoilerMaterial:ref _bakedBoilerMaterial);
            if(material==null)
            {
                var texture=Resources.Load<Texture2D>(squat?"FoundryShortBoiler":"FoundryBoiler");
                if(texture==null)return false;
                material=new Material(Shader.Find("Sprites/Default"))
                    {name="Baked arched boiler",mainTexture=texture};
            }
            Vector3 origin=Project(centre.x,centre.y);
            // Separate lower drum and upper shell ranks so existing front rails and pipework
            // retain their painter ordering. Both use the same footprint depth/cutaway.
            float imageBottom=squat?-1.86f:-1.8f,size=squat?6f:10.8f,split=squat?1.2f:2.8f;
            BakedBoilerBand(origin,imageBottom,split,-12,material,imageBottom,size,centre);
            BakedBoilerBand(origin,split,imageBottom+size,-10.5f,material,imageBottom,size,centre,true,squat);
            if(squat)
            {
                // The existing main joins at height1.8; keep its original runtime socket.
                var socket=new Builder();
                BuildSquatBoilerSocket(socket,centre);
                Create("Short boiler service socket",socket,arena.PlatformMaterials.FoundryIron,-10.5f);
            }

            // Quiet radial contact shade, emitted with floor decals, under architecture/actors.
            var shadow=new Builder();
            const int sides=32;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                int n=shadow.V.Count;
                shadow.V.Add(origin);
                shadow.V.Add(origin+new Vector3(Mathf.Cos(a)*2.24f,Mathf.Sin(a)*1.12f,0));
                shadow.V.Add(origin+new Vector3(Mathf.Cos(b)*2.24f,Mathf.Sin(b)*1.12f,0));
                // Borrow the existing opaque coping atlas; black tint contributes alpha only.
                for(int j=0;j<3;j++)shadow.U.Add(new Vector2(.5f,.65f));
                shadow.C.Add(new Color(0,0,0,.28f));shadow.C.Add(Color.clear);shadow.C.Add(Color.clear);
                shadow.T.AddRange(new[]{n,n+1,n+2});
            }
            Create("Baked boiler contact shade",shadow,arena.PlatformMaterials.Coping,FloorDecal);
            return true;
        }

        private void BakedBoilerBand(Vector3 origin,float bottom,float top,float order,Material material,float imageBottom,float size,
            Vector2 centre,bool upper=false,bool squat=false)
        {
            var b=new Builder();
            b.V.AddRange(new[]{origin+new Vector3(-size*.5f,bottom,0),origin+new Vector3(size*.5f,bottom,0),
                origin+new Vector3(size*.5f,top,0),origin+new Vector3(-size*.5f,top,0)});
            float v0=(bottom-imageBottom)/size,v1=(top-imageBottom)/size;
            b.U.AddRange(new[]{new Vector2(0,v0),new Vector2(1,v0),new Vector2(1,v1),new Vector2(0,v1)});
            for(int i=0;i<4;i++)b.C.Add(Color.white);
            b.T.AddRange(new[]{0,1,2,0,2,3});
            // Append to the actual image batch: separate same-rank iron batches cannot
            // guarantee that a fitted service part draws over its own baked shell.
            if(upper&&squat&&centre.x<22)
            {
                int first=b.V.Count;
                BakedBoilerRoofHatch(b,centre);
                // Verified opaque steel patch in the unchanged squat source:
                // (0.52,0.53) RGB84/91/102.
                // Native face lighting supplies relief; never sample fire or alpha.
                Vector2 sample=new Vector2(.52f,.53f);
                for(int i=first;i<b.U.Count;i++)b.U[i]=sample;
            }
            Create("Baked boiler band",b,material,order);
        }

        private static void BakedBoilerRoofHatch(Builder b,Vector2 c)
        {
            // A shallow inspection lid seats directly into the front hood slope.
            // The previous four-foot pedestal read as furniture and was rejected.
            int first=b.C.Count;
            WorkshopMetalHull(b,c.x-.92f,c.y-.94f,c.x,c.y+.18f,1.68f,1.76f,.045f);
            FullFace(b,Project(c.x-.82f,c.y-.84f)+Vector3.up*1.758f,
                Project(c.x-.10f,c.y-.84f)+Vector3.up*1.758f,
                Project(c.x-.10f,c.y+.08f)+Vector3.up*1.758f,
                Project(c.x-.82f,c.y+.08f)+Vector3.up*1.758f,new Color(.46f,.49f,.52f));
            // Short hinge leaves seat on the rear edge; the central pull is shallow.
            foreach(float u in new[]{-.76f,-.30f})
                WorkshopMetalHull(b,c.x+u,c.y+.03f,c.x+u+.14f,c.y+.20f,1.75f,1.80f,.012f);
            foreach(float u in new[]{-.61f,-.38f})
                WorkshopMetalHull(b,c.x+u,c.y-.45f,c.x+u+.055f,c.y-.34f,1.76f,1.82f,.009f);
            WorkshopMetalHull(b,c.x-.61f,c.y-.45f,c.x-.325f,c.y-.34f,1.805f,1.845f,.009f);
            // Neutral steel, close to the baked hood; only bevels catch a quiet edge.
            for(int i=first;i<b.C.Count;i++)
            {
                Color tone=b.C[i];b.C[i]=new Color(tone.r*1.48f,tone.g*1.36f,tone.b*1.20f,1);
            }
        }
    }
}
