using UnityEngine;

namespace Cryptforge.Art
{
    // One immutable unit-radius Staff impact per session. Views scale it; scene reloads never regenerate its texture.
    internal static class StaffSplashArt
    {
        internal const float ContourRadiusTexels = 240f;
        private static Sprite _shared;

        internal static Sprite Get()
        {
            if (_shared == null) _shared = Create();
            return _shared;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetSession()
        {
            Release();
            Application.quitting -= Release;
            Application.quitting += Release;
        }

        private static void Release()
        {
            PixelSpriteFactory.Destroy(_shared);
            _shared = null;
        }

        private static Sprite Create()
        {
            const int width=512,height=256;


            const float radius=240;

            var pixels=new Color32[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                float dx=x+.5f-width*.5f,dy=(y+.5f-height*.5f)*2;
                float length=Mathf.Sqrt(dx*dx+dy*dy);
                float angle=Mathf.Atan2(dy,dx);
                float phase=Mathf.Repeat(angle+Mathf.PI/4,Mathf.PI/2)/(Mathf.PI/2);
                // Four broad tapered arcs, without the ability's eight-segment seal or diamond markers.
                float taper=Mathf.Clamp01((phase-.10f)/.12f)*Mathf.Clamp01((.90f-phase)/.25f);
                float gradient=Mathf.Sqrt(Mathf.Cos(angle)*Mathf.Cos(angle)+4*Mathf.Sin(angle)*Mathf.Sin(angle));
                float distance=Mathf.Abs(length-radius)/gradient;
                float core=Mathf.Clamp01(2.3f*taper-distance);
                float glow=.28f*Mathf.Clamp01((7*taper-distance)/5);
                float alpha=Mathf.Max(core,glow);
                Color color=Color.Lerp(new Color(.44f,.23f,.9f),new Color(.91f,.79f,1),core);
                color.a=alpha; pixels[y*width+x]=color;
            }
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false)
            {name="Shared Staff impact arcs",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply(false,true);
            var sprite=Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,.5f),240f,0,SpriteMeshType.FullRect);
            return sprite;
        }
    }
}
