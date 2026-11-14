using System;
using Cryptforge.Core;

namespace Cryptforge.Progression
{
    // The map a run starts on (MapChoiceView, docs/53): the Foundry (floor 1), the Quicksilver Vaults (floor 2) or the
    // Gloam Gardens (floor 3). A hero that starts deeper is given the power it would typically have on arriving there by
    // the stairs: the level the simulation's careful pilot reaches by then, with every level-up of it still to choose, so
    // the existing level-up panel offers them one after another before the first wave. The offers are the run's own
    // (RunRandom by the run's seed and the offer's index), so the same seed offers the same cards.
    //
    // The levels are the Descent simulation's (Tests/Support/DescentSimulation*.cs, FoundryBatchReport with FOUNDRY_FLOORS=4,
    // FOUNDRY_COHORTS=1, FOUNDRY_LEVERS=0, FOUNDRY_REALISM=typical, seeds 1-200, on 2026-11-14's floors): the level a run
    // carries down its checkpoint, the careful cohort with a typical human's hands, both heroes pooled, P50 -
    //   floor 2: 31 (393 runs; P25 30, range 29-33; every cohort but the auto-farmer 30),
    //   floor 3: 44 (385 runs; range 42-46; every cohort but the auto-farmer 44).
    // No gold comes with it: a run's gold buys nothing during the run and is banked into the profile's forge gold as the run
    // ends, so the gold a hero would have carried down would be forge gold the player never earned.
    public static class DescentStart
    {
        public const int FirstFloor = 1;
        // The deepest map a run may start on; the finale is never a start.
        public const int DeepestStart = 3;
        public const int FloorTwoArrivalLevel = 31;
        public const int FloorThreeArrivalLevel = 44;

        // Whether a run may be asked to start on this floor.
        public static bool IsStart(int floorNumber) => floorNumber >= FirstFloor && floorNumber <= DeepestStart;

        // The level a hero starting on this floor begins at: 0 (a fresh hero) on floor 1 or anything that is not a start.
        public static int CatchUpLevel(int startFloor)
        {
            switch (startFloor)
            {
                case 2: return FloorTwoArrivalLevel;
                case 3: return FloorThreeArrivalLevel;
                default: return 0;
            }
        }

        // Raises a fresh run to the start floor's level, its level-ups left to choose (RunState.PendingUpgrades), and
        // returns how many levels it gained. Nothing on floor 1, or once the run has ended or already stands that high.
        public static int CatchUp(RunState run, int startFloor)
        {
            if (run == null)
                throw new ArgumentNullException(nameof(run));
            int level = CatchUpLevel(startFloor);
            if (run.HasEnded || level <= run.Level)
                return 0;
            int before = run.Level;
            run.AddExperience(run.ExperienceToReach(level) - run.Experience);
            return run.Level - before;
        }
    }
}
