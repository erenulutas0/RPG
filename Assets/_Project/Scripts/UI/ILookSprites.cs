using UnityEngine;

namespace Cryptforge.UI
{
    // A look view that generated the sprites shown on a combatant's body. CombatantView asks it for a white silhouette of
    // the body's current sprite so a hit flash can be drawn as an overlay: tinting a textured sprite white shows nothing.
    // Returns null for sprites the view did not assign.
    public interface ILookSprites
    {
        Sprite SilhouetteOf(Sprite bodySprite);
    }
}
