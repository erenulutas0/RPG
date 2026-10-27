using System;
using UnityEngine;

namespace Cryptforge.Art
{
    // A cut-out puppet's parts (docs/45 section 4 B): each facing painted once and cut into pieces that PuppetRig hangs from
    // one another and PuppetAnimator turns with PuppetGait's poses, so one painting gives every motion. Imported, shared
    // assets, like VanguardArtSet: the rig borrows these sprites and never destroys them. With no set assigned the hero
    // builds code-drawn stand-ins (PuppetPlaceholder) with the same shape of data.
    //
    // Two facings, rear three-quarter and front three-quarter, each normally painted facing screen-right; leftward travel
    // mirrors the whole puppet, as VanguardAnimator mirrors its frames.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Puppet Set")]
    public sealed class PuppetArtSet : ScriptableObject
    {
        [Serializable]
        public struct Part
        {
            // The part's GameObject name.
            public string Name;
            // Its pivot is the joint the part turns about: a thigh's at the hip, a shin's at the knee, a head's at the neck,
            // a torso's at the waist. Every part of a facing shares one pixels-per-unit, so the figure is about 1.25 units
            // tall like the Vanguard.
            public Sprite Sprite;
            // Optional: a white silhouette of the same sprite, same size and pivot, shown during a hit flash (a white tint
            // on a painted sprite shows nothing). A part without one simply does not flash.
            public Sprite Flash;
            // The index of an earlier part of this facing it hangs from, or -1 to hang it from the feet.
            public int Parent;
            // Where its pivot sits in the parent's space at rest, in world units: on the parent's sprite, measured from the
            // parent's pivot (from the feet, on the floor line, for a root part).
            public Vector2 Joint;
            // Which pose channel turns it.
            public PuppetBone Bone;
            // Draw order over the hero body's sorting order: the far limbs below the torso, the near arm above it.
            public int Sorting;
        }

        [Serializable]
        public struct Facing
        {
            // Parents before children.
            public Part[] Parts;
            // The parts the weapons hang from, as indices into Parts - the fists, or the forearms where a sheet paints the
            // gauntlet on them (docs/45, P5), whose grips then take the wrist's turn; the grip in each, in that part's
            // space (world units from its pivot); and the held weapon's draw order over the body's, so it can sit between
            // the forearm and the fingers that close over it.
            public int RightHand;
            public Vector2 RightGrip;
            public int RightItemSorting;
            public int LeftHand;
            public Vector2 LeftGrip;
            public int LeftItemSorting;
            // Optional: the two ends of each boot's sole, heel and toe, in its shin part's space (world units from the
            // knee, x then y of one end, then of the other), so the walk's bob plants the boot it stands on; all zero,
            // the rig takes the bottom corners of the shin's sprite.
            public Vector4 RightSole;
            public Vector4 LeftSole;
            // True when the sheet was painted facing screen-left (docs/45's front prompt asks for down-left): the rig then
            // mirrors it and turns every joint the other way, so the poses need not know.
            public bool FacesLeft;
        }

        [SerializeField] private Facing _rear;
        [SerializeField] private Facing _front;
        // Optional painted weapons, each pivoted on its grip and drawn for a hand hanging at rest (the sword and daggers
        // pointing up and forward, the staff upright); empty, the hero keeps HeroArt's generated weapons.
        [SerializeField] private Sprite _sword;
        [SerializeField] private Sprite _shield;
        [SerializeField] private Sprite _staff;
        [SerializeField] private Sprite _dagger;

        public Facing Rear => _rear;
        public Facing Front => _front;
        public Facing Get(bool front) => front ? _front : _rear;
        public Sprite Sword => _sword;
        public Sprite Shield => _shield;
        // A painted staff includes its crystal, like VanguardArtSet's: the generated orb is then hidden.
        public Sprite Staff => _staff;
        public Sprite Dagger => _dagger;

        public bool IsValid => IsComplete(_rear) && IsComplete(_front);

        // Every part has a sprite and hangs from an earlier part or the feet, and both hands are parts.
        public static bool IsComplete(Facing facing)
        {
            Part[] parts = facing.Parts;
            if (parts == null || parts.Length == 0)
                return false;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Sprite == null || parts[i].Parent < -1 || parts[i].Parent >= i)
                    return false;
            }
            return facing.RightHand >= 0 && facing.RightHand < parts.Length
                && facing.LeftHand >= 0 && facing.LeftHand < parts.Length;
        }

        // Fills a set built at runtime (the placeholder); an imported set is authored in the Inspector instead.
        internal void Fill(Facing rear, Facing front)
        {
            _rear = rear;
            _front = front;
        }
    }
}
