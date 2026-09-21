using Cryptforge.Core;
using Cryptforge.Economy;
using UnityEngine;

namespace Cryptforge.Tests
{
    // For a scene test about something other than the Descent's drops - a choice panel, the pause, telemetry, the result
    // screen - that needs the first pack to level the hero on its own: picks up every drop on the frame it falls, after
    // every other Update and before a test's coroutine resumes, which is the game as it was before drops (docs/27 S6d,
    // docs/37), a kill's reward reaching the run on the kill's own frame. A test about drops, or one held against the
    // simulation, never attaches it.
    [AddComponentMenu("")]
    [DefaultExecutionOrder(1000)]
    public sealed class DropVacuum : MonoBehaviour
    {
        private GroundDrops _drops;

        public static DropVacuum Attach(CombatSetup setup)
        {
            var vacuum = setup.gameObject.AddComponent<DropVacuum>();
            vacuum._drops = setup.Drops;
            return vacuum;
        }

        private void Update()
        {
            if (_drops != null && _drops.Count > 0)
                _drops.CollectAll();
        }
    }
}
