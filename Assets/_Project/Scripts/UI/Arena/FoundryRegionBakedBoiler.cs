using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private Material _bakedBoilerMaterial;

        // The north pair shares one source material and two static batches. Imported bake is shared; material/meshes belong to this scene.
        // Keep the existing footprint, foundation, pipe anchors and atmosphere source.
        private bool BuildBakedBoiler(ArenaView arena,Vector2 centre)
        {
            if(_bakedBoilerMaterial==null)
            {
                var texture=Resources.Load<Texture2D>("FoundryBoiler");
                if(texture==null)return false;
                _bakedBoilerMaterial=new Material(Shader.Find("Sprites/Default"))
                    {name="Baked arched boiler",mainTexture=texture};
            }
            Vector3 origin=Project(centre.x,centre.y);
            // Separate lower drum and upper shell ranks so existing front rails and pipework
            // retain their painter ordering. Both use the same footprint depth/cutaway.
            BakedBoilerBand(origin,-1.8f,2.8f,-12);
            BakedBoilerBand(origin,2.8f,9f,-10.5f);

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

        private void BakedBoilerBand(Vector3 origin,float bottom,float top,float order)
        {
            var b=new Builder();
            b.V.AddRange(new[]{origin+new Vector3(-5.4f,bottom,0),origin+new Vector3(5.4f,bottom,0),
                origin+new Vector3(5.4f,top,0),origin+new Vector3(-5.4f,top,0)});
            float v0=(bottom+1.8f)/10.8f,v1=(top+1.8f)/10.8f;
            b.U.AddRange(new[]{new Vector2(0,v0),new Vector2(1,v0),new Vector2(1,v1),new Vector2(0,v1)});
            for(int i=0;i<4;i++)b.C.Add(Color.white);
            b.T.AddRange(new[]{0,1,2,0,2,3});
            Create("Baked boiler band",b,_bakedBoilerMaterial,order);
        }
    }
}
