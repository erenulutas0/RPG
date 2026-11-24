using UnityEngine;

namespace Cryptforge.Combat
{
    // Rootmother Myrr in the scene (MyrrRule; docs/64, shipped as floor 3's gate boss 2026-11-25): on her prefab, it tells
    // WardenSlamService that this gate boss lays a root snare - a ring at the hero's spot that creeps after it through the
    // telegraph, catches a hero still inside as the roots erupt and plants it - where the Forge Warden marks a circle, on the
    // Warden's clock and hold, her hook free for a hero at her feet as the snare goes down or caught by the roots. Its look is
    // WardenSlamView's (the moving red-orange ring with orchid root tips, her orchid feet ring, the planted roots) and
    // BossFeelView's (her orchid tell and bloom, her withering fall); nothing to carry. Off the foundry's gate - the hall
    // arena, a swarm's ladder - nothing reads it and she fights as authored.
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public sealed class RootmotherMyrr : MonoBehaviour
    {
    }
}
