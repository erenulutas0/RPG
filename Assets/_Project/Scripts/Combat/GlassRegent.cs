using UnityEngine;

namespace Cryptforge.Combat
{
    // The Glass Regent in the scene (RegentRule; docs/50 section 6 and the lead's decisions 2026-11-12): on the Regent's
    // prefab, it tells WardenSlamService that this gate boss marks a lane - from where it stands through the hero's spot -
    // where the Forge Warden marks a circle, on the same clock and with the same hold of its weapon. It carries the lane's
    // look too, the two 9-slice sprites WardenSlamView lays along the floor (Tools/ArtReview/BuildRegentLaneMark.py): the
    // edge, tinted as the Warden's mark's edge, and the red-orange fill that grows across the lane with the telegraph.
    // Off the foundry's gate - the hall arena, a swarm's ladder - nothing reads it and the Regent fights as authored.
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public sealed class GlassRegent : MonoBehaviour
    {
        [SerializeField] private Sprite _laneEdge;
        [SerializeField] private Sprite _laneFill;

        public Sprite LaneEdge => _laneEdge;
        public Sprite LaneFill => _laneFill;
    }
}
