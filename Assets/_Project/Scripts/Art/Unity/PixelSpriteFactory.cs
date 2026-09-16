using System;
using UnityEngine;

namespace Cryptforge.Art
{
    // Turns canvases into sprites: point filtering, no mipmaps, a fixed pixel density, so every generated placeholder
    // shares one pixel grid. The caller owns what it creates and must destroy both sprite and texture. Most views
    // own their art; EnemySpriteCache instead owns shared enemy art for a session. Borrowing views never destroy it.
    public static class PixelSpriteFactory
    {
        // Texels per world unit for every placeholder sprite; the hero is about 1.25 units (40 texels) tall.
        public const float PixelsPerUnit = 32f;

        public static readonly Vector2 BottomCentre = new Vector2(0.5f, 0f);
        public static readonly Vector2 Centre = new Vector2(0.5f, 0.5f);

        public static Texture2D CreateTexture(PixelCanvas canvas, string name)
        {
            if (canvas == null)
                throw new ArgumentNullException(nameof(canvas));

            var texture = new Texture2D(canvas.Width, canvas.Height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color32[canvas.Width * canvas.Height];
            Rgba[] source = canvas.Pixels;
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(source[i].R, source[i].G, source[i].B, source[i].A);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        // A sprite over the whole texture; the pivot is in normalised texture coordinates (BottomCentre for feet).
        public static Sprite CreateSprite(Texture2D texture, Vector2 pivot, float pixelsPerUnit = PixelsPerUnit)
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));
            if (!(pixelsPerUnit > 0f))
                throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit));

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivot, pixelsPerUnit, 0,
                SpriteMeshType.FullRect);
            sprite.name = texture.name;
            return sprite;
        }

        public static Sprite CreateSprite(PixelCanvas canvas, string name, Vector2 pivot, float pixelsPerUnit = PixelsPerUnit) =>
            CreateSprite(CreateTexture(canvas, name), pivot, pixelsPerUnit);

        // Frees a sprite and its texture; safe on null and on already destroyed objects.
        public static void Destroy(Sprite sprite)
        {
            if (sprite == null)
                return;
            Texture2D texture = sprite.texture;
            UnityEngine.Object.Destroy(sprite);
            if (texture != null)
                UnityEngine.Object.Destroy(texture);
        }
    }
}
