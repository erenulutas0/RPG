using System.Collections.Generic;

namespace Cryptforge.Combat
{
    // Who stands near an enemy that has just fallen, for a weapon whose kills burst (KillBurstRule). WeaponRuntime knows
    // nowhere, and unlike the splinter's circle this one cannot be gathered before the strike: which enemies a strike
    // fells - and so where each burst is centred - is only known once it has landed. So whoever fires the weapon answers
    // when asked: the scene's AttackController through Targeting.CollectNear, the Descent simulation through the Nearby
    // that mirrors it.
    public interface INeighbourhood
    {
        // The other living enemies within radius of centre on the floor, nearest first; centre itself is never among
        // them, and it may be dead. The list may be reused by the next call.
        IReadOnlyList<IDamageable> Near(IDamageable centre, float radius);
    }
}
