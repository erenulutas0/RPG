using UnityEngine;

namespace Cryptforge.Art
{
    // One facing of a PuppetArtSet as a SpriteRenderer hierarchy: each part a child of its parent at its joint, its sprite
    // pivoted on that joint, so turning a part's transform turns everything hung from it. Building allocates; posing and
    // flashing only write transforms and renderers. The rig borrows the set's sprites and never destroys them.
    internal sealed class PuppetRig
    {
        private readonly Transform[] _bones;
        private readonly SpriteRenderer[] _renderers;
        private readonly PuppetBone[] _kinds;
        private readonly Vector2[] _joints;
        private readonly Sprite[] _sprites;
        private readonly Sprite[] _flashes;
        // -1 for a sheet painted facing left: mirrored at the root, its joints must turn the other way to pose the same.
        private readonly float _turn;
        // A sheet whose forearm carries the gauntlet (docs/45, P5) has no hand part to take the wrist's turn: its grip
        // takes it instead, so a held weapon still turns at the wrist.
        private bool _rightGripTurns;
        private bool _leftGripTurns;

        private PuppetRig(GameObject root, int count, float turn)
        {
            Root = root;
            _bones = new Transform[count];
            _renderers = new SpriteRenderer[count];
            _kinds = new PuppetBone[count];
            _joints = new Vector2[count];
            _sprites = new Sprite[count];
            _flashes = new Sprite[count];
            _turn = turn;
        }

        // The rig's own root, at the feet: it carries the whole figure's fall and a left-painted sheet's mirror.
        public GameObject Root { get; }
        // Where each hand's weapon is held, children of the hand parts.
        public Transform RightGrip { get; private set; }
        public Transform LeftGrip { get; private set; }
        public int RightItemSorting { get; private set; }
        public int LeftItemSorting { get; private set; }
        // Both legs as painted, for the walk's bob (PuppetGaitInput.Legs).
        public PuppetLegs Legs { get; private set; }
        public int PartCount => _bones.Length;
        public Transform Bone(int index) => _bones[index];
        public SpriteRenderer Renderer(int index) => _renderers[index];
        public PuppetBone Kind(int index) => _kinds[index];

        // The first part on this bone, or -1.
        public int IndexOf(PuppetBone bone)
        {
            for (int i = 0; i < _kinds.Length; i++)
            {
                if (_kinds[i] == bone)
                    return i;
            }
            return -1;
        }

        // Builds the facing under parent. Every renderer takes the body's material and sorting layer and sorts at the
        // body's order plus the part's offset, inside the actor's sorting group.
        public static PuppetRig Build(PuppetArtSet.Facing facing, Transform parent, string name, Material material,
            int sortingLayer, int baseOrder)
        {
            PuppetArtSet.Part[] parts = facing.Parts;
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localScale = new Vector3(facing.FacesLeft ? -1f : 1f, 1f, 1f);
            var rig = new PuppetRig(root, parts.Length, facing.FacesLeft ? -1f : 1f);
            for (int i = 0; i < parts.Length; i++)
            {
                PuppetArtSet.Part part = parts[i];
                var child = new GameObject(string.IsNullOrEmpty(part.Name) ? "Part " + i : part.Name);
                child.transform.SetParent(part.Parent >= 0 ? rig._bones[part.Parent] : root.transform, false);
                child.transform.localPosition = part.Joint;
                var renderer = child.AddComponent<SpriteRenderer>();
                renderer.sprite = part.Sprite;
                if (material != null)
                    renderer.sharedMaterial = material;
                renderer.sortingLayerID = sortingLayer;
                renderer.sortingOrder = baseOrder + part.Sorting;
                rig._bones[i] = child.transform;
                rig._renderers[i] = renderer;
                rig._kinds[i] = part.Bone;
                rig._joints[i] = part.Joint;
                rig._sprites[i] = part.Sprite;
                rig._flashes[i] = part.Flash;
            }
            rig.RightGrip = Grip("Right Grip", rig._bones[facing.RightHand], facing.RightGrip);
            rig.LeftGrip = Grip("Left Grip", rig._bones[facing.LeftHand], facing.LeftGrip);
            rig._rightGripTurns = parts[facing.RightHand].Bone != PuppetBone.HandRight;
            rig._leftGripTurns = parts[facing.LeftHand].Bone != PuppetBone.HandLeft;
            rig.RightItemSorting = facing.RightItemSorting;
            rig.LeftItemSorting = facing.LeftItemSorting;
            rig.Legs = new PuppetLegs
            {
                Right = Leg(parts, PuppetBone.ThighRight, PuppetBone.ShinRight, facing.RightSole, rig._turn),
                Left = Leg(parts, PuppetBone.ThighLeft, PuppetBone.ShinLeft, facing.LeftSole, rig._turn)
            };
            return rig;
        }

        // Every part at its joint plus the pose's offsets, turned by its bone's angle; the root falls about the feet.
        public void Apply(in PuppetPose pose)
        {
            Root.transform.localRotation = Quaternion.Euler(0f, 0f, pose.Fall);
            for (int i = 0; i < _bones.Length; i++)
            {
                PuppetBone kind = _kinds[i];
                Vector2 joint = _joints[i];
                _bones[i].localPosition = new Vector3(joint.x + _turn * pose.Forward(kind), joint.y + pose.Rise(kind), 0f);
                _bones[i].localRotation = Quaternion.Euler(0f, 0f, _turn * pose.Rotation(kind));
            }
            if (_rightGripTurns)
                RightGrip.localRotation = Quaternion.Euler(0f, 0f, _turn * pose.WristRight);
            if (_leftGripTurns)
                LeftGrip.localRotation = Quaternion.Euler(0f, 0f, _turn * pose.WristLeft);
        }

        // Shows each part's white silhouette tinted by the flash colour, or its own sprite again.
        public void Flash(bool on, Color color)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                bool lit = on && _flashes[i] != null;
                _renderers[i].sprite = lit ? _flashes[i] : _sprites[i];
                _renderers[i].color = lit ? color : Color.white;
            }
        }

        private static Transform Grip(string name, Transform hand, Vector2 offset)
        {
            var grip = new GameObject(name).transform;
            grip.SetParent(hand, false);
            grip.localPosition = offset;
            return grip;
        }

        // A leg for the bob, in the terms of a figure facing right: the knee is the shin's joint on its thigh, the sole
        // the set's or, without one, the bottom corners of the shin's sprite a texel in from its edges. No thigh with a
        // shin hung from it gives the gait's reference leg.
        private static PuppetLeg Leg(PuppetArtSet.Part[] parts, PuppetBone thighBone, PuppetBone shinBone, Vector4 sole,
            float turn)
        {
            int thigh = -1;
            int shin = -1;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Bone == thighBone && thigh < 0)
                    thigh = i;
                else if (parts[i].Bone == shinBone && shin < 0)
                    shin = i;
            }
            if (thigh < 0 || shin < 0 || parts[shin].Parent != thigh || parts[shin].Sprite == null)
                return default;
            if (sole == Vector4.zero)
            {
                Bounds bounds = parts[shin].Sprite.bounds;
                float texel = 1f / parts[shin].Sprite.pixelsPerUnit;
                sole = new Vector4(bounds.min.x + texel, bounds.min.y + texel, bounds.max.x - texel, bounds.min.y + texel);
            }
            Vector2 knee = parts[shin].Joint;
            return new PuppetLeg
            {
                KneeX = turn * knee.x, KneeY = knee.y, HeelX = turn * sole.x, HeelY = sole.y, ToeX = turn * sole.z,
                ToeY = sole.w
            };
        }
    }
}
