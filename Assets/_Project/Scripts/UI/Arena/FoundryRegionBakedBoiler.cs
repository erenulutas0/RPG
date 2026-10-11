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
            // Each atlas contains two equal square bakes: near on the left, far on the right.
            // Follow the shared assembly anchors rather than a second coordinate threshold.
            float tileU=centre==BoilerCentres[squat?3:1] ? .5f : 0f;
            BakedBoilerBand(origin,imageBottom,split,-12,material,imageBottom,size,tileU);
            BakedBoilerBand(origin,split,imageBottom+size,-10.5f,material,imageBottom,size,tileU);
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
            float tileU)
        {
            var b=new Builder();
            b.V.AddRange(new[]{origin+new Vector3(-size*.5f,bottom,0),origin+new Vector3(size*.5f,bottom,0),
                origin+new Vector3(size*.5f,top,0),origin+new Vector3(-size*.5f,top,0)});
            float v0=(bottom-imageBottom)/size,v1=(top-imageBottom)/size;
            b.U.AddRange(new[]{new Vector2(tileU,v0),new Vector2(tileU+.5f,v0),
                new Vector2(tileU+.5f,v1),new Vector2(tileU,v1)});
            for(int i=0;i<4;i++)b.C.Add(Color.white);
            b.T.AddRange(new[]{0,1,2,0,2,3});
            Create("Baked boiler band",b,material,order);
        }
    }
}
