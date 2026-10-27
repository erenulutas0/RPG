using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.Art
{
    // A PuppetArtSet built at runtime from PuppetPlaceholderArt's canvases, so the puppet runs before the painted parts
    // exist and the painted set later drops into the same fields. The caller owns what this creates: every sprite (with
    // its texture) lands in owned, and the set itself must be destroyed too.
    internal static class PuppetPlaceholder
    {
        internal static PuppetArtSet Create(List<Sprite> owned)
        {
            var set = ScriptableObject.CreateInstance<PuppetArtSet>();
            set.name = "Puppet Placeholder";
            set.hideFlags = HideFlags.DontSave;
            set.Fill(Facing(false, owned), Facing(true, owned));
            return set;
        }

        private static PuppetArtSet.Facing Facing(bool front, List<Sprite> owned)
        {
            const float texel = 1f / PixelSpriteFactory.PixelsPerUnit;
            PuppetPlaceholderPart[] drawn = PuppetPlaceholderArt.Draw(front);
            var parts = new PuppetArtSet.Part[drawn.Length];
            string prefix = front ? "Puppet Front " : "Puppet Rear ";
            for (int i = 0; i < drawn.Length; i++)
            {
                PuppetPlaceholderPart part = drawn[i];
                // The centre of the pivot texel, as HeroLookView pivots its weapons, so a part's texels share the grid.
                var pivot = new Vector2((part.PivotX + 0.5f) / part.Canvas.Width, (part.PivotY + 0.5f) / part.Canvas.Height);
                Sprite sprite = PixelSpriteFactory.CreateSprite(part.Canvas, prefix + part.Name, pivot);
                Sprite flash = PixelSpriteFactory.CreateSprite(part.Canvas.Silhouette(Rgba.White), prefix + part.Name + " Flash", pivot);
                owned.Add(sprite);
                owned.Add(flash);
                parts[i] = new PuppetArtSet.Part
                {
                    Name = part.Name,
                    Sprite = sprite,
                    Flash = flash,
                    Parent = part.Parent,
                    Joint = new Vector2(part.JointX * texel, part.JointY * texel),
                    Bone = part.Bone,
                    Sorting = part.Sorting
                };
            }
            var grip = new Vector2(PuppetPlaceholderArt.GripX * texel, PuppetPlaceholderArt.GripY * texel);
            return new PuppetArtSet.Facing
            {
                Parts = parts,
                RightHand = PuppetPlaceholderArt.HandRight,
                RightGrip = grip,
                RightItemSorting = PuppetPlaceholderArt.RightItemSorting,
                LeftHand = PuppetPlaceholderArt.HandLeft,
                LeftGrip = grip,
                LeftItemSorting = PuppetPlaceholderArt.LeftItemSorting,
                FacesLeft = false
            };
        }
    }
}
