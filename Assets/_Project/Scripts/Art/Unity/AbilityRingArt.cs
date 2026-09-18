using UnityEngine;

namespace Cryptforge.Art
{
    // One immutable unit-radius marker per session. Views scale it; scene reloads never regenerate its texture.
    internal static class AbilityRingArt
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
            const int width = 512, height = 256;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float dx = x + .5f - width*.5f, dy = y + .5f - height*.5f;
                float length = Mathf.Sqrt(dx*dx + 4*dy*dy);
                float angle = Mathf.Atan2(dy*2, dx);
                float gradient = Mathf.Sqrt(Mathf.Cos(angle)*Mathf.Cos(angle) + 4*Mathf.Sin(angle)*Mathf.Sin(angle));
                float distance = Mathf.Abs(length - 240) / gradient;
                float phase = Mathf.Repeat(angle + Mathf.PI/8, Mathf.PI/4);
                bool gap = Mathf.Abs(phase - Mathf.PI/8) < .027f;
                float core = gap ? 0 : Mathf.Clamp01(2.1f - distance);
                float shadow = gap ? 0 : .36f * Mathf.Clamp01(3.5f - distance);
                // Four small inward lozenges reference the forge motif without inventing target points.
                float marker = 0;

                {
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * Mathf.PI/2;
                        float mx = Mathf.Cos(a)*231, my = Mathf.Sin(a)*115.5f;
                        float edge = Mathf.Abs(Mathf.Abs(dx-mx) + Mathf.Abs(dy-my)*1.5f - 4.5f);
                        marker = Mathf.Max(marker, Mathf.Clamp01(1.1f-edge));
                    }
                }
                core = Mathf.Max(core, marker);
                float alpha = Mathf.Max(core, shadow);
                Color color = Color.Lerp(new Color(.055f,.08f,.16f,alpha), new Color(.63f,.84f,1f,alpha), core);
                pixels[y*width+x] = color;
            }
            var texture = new Texture2D(width,height,TextureFormat.RGBA32,false)
            { name = "Shared ability arc seal", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply(false,true);
            var sprite = Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,.5f),ContourRadiusTexels,0,SpriteMeshType.FullRect);

            return sprite;
        }
    }
}
