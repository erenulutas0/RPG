using UnityEngine;

namespace Cryptforge.Art
{
    // The game-feel pass's one drawing (docs/43 section 1, 2026-11-22): a soft round mote of unit radius, white, its alpha
    // falling off from the centre, made once a session from code - no painted frame. Scaled and tinted it is every glow,
    // ash mote, ember and (stretched) glass shard the pass draws, so all of them batch on one texture. Views borrow it and
    // never destroy it; scene reloads never regenerate it.
    internal static class FeelArt
    {
        internal const string DiscName = "Feel soft disc";
        private const int Size = 32;
        private static Sprite _disc;

        internal static Sprite Disc()
        {
            if (_disc == null)
                _disc = CreateDisc();
            return _disc;
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
            PixelSpriteFactory.Destroy(_disc);
            _disc = null;
        }

        private static Sprite CreateDisc()
        {
            var pixels = new Color32[Size * Size];
            const float half = Size * .5f;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x + .5f - half) / half;
                    float dy = (y + .5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    // A bright core easing out to nothing at the rim: reads as a glow when large, a mote when small.
                    float alpha = Mathf.Clamp01(1f - r);
                    alpha *= alpha * (3f - 2f * alpha);
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }
            // Kept through every scene load's unload of unused assets: it is the session's, released on quit.
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = DiscName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontUnloadUnusedAsset
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            // Half the texture's width a unit: the disc's radius is one world unit at scale 1.
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(.5f, .5f), half, 0,
                SpriteMeshType.FullRect);
            sprite.name = DiscName;
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }
    }
}
