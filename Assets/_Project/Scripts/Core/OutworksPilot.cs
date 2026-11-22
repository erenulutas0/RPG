using System;
using Cryptforge.Combat;

namespace Cryptforge.Core
{
    // How the development autopilot plays docs/62's Outworks probe (P0a), per cohort: the one choice of objective that
    // DevelopmentAutopilot and the foundry simulation (DescentSimulation.Outworks.cs) both read, so a cohort is one player
    // in the scene and the simulation (AutopilotCohort's rule). The objective only: how the hero walks there (the pads,
    // the drops beside the way, the kite) stays with each pilot's own steer, as for a hall.
    //
    // docs/62 section 7.3's habits, from the cohorts' names (a cohort drops habits the careful pilot has; these are the
    // probe's modelling, to be checked against the owner's own telemetry):
    // - careful: captures every anvil it can reach, hunts the Keeper when it is near or the anvils are done, uses the
    //   pads, and with the key leaves once its anvils are done; with nothing to go for it takes the altars and the drops in
    //   sight, else holds its ground (never the pit's lure: the pads stand on the rim);
    // - standing: captures and stands - it never hunts the Keeper (it fights it where it comes);
    // - first-card: beelines - with the key it leaves at once;
    // - clumsy: misses the far anvils, never takes a pad and never hunts the Keeper (it fights it where it comes);
    // - auto-farmer: misses the far anvils, never takes a pad and never hunts the Keeper.
    // A realistic pilot (PilotRealism) also leaves an anvil off the screen unnoticed now and then (the caller's noticed
    // flags; docs/62 section 7.3's "missing 30 % of off-screen shrines").
    public static class OutworksPilot
    {
        public enum Objective
        {
            None = 0,
            Stair = 1,
            Keeper = 2,
            Anvil = 3
        }

        public readonly struct Habits
        {
            public readonly bool UsesPads;
            public readonly bool HuntsKeeper;
            public readonly bool LeavesAtOnce;
            // How far off an uncaptured anvil may stand for the pilot to go for it; infinity for every anvil.
            public readonly float AnvilReach;

            public Habits(bool usesPads, bool huntsKeeper, bool leavesAtOnce, float anvilReach)
            {
                UsesPads = usesPads;
                HuntsKeeper = huntsKeeper;
                LeavesAtOnce = leavesAtOnce;
                AnvilReach = anvilReach;
            }
        }

        // A Keeper this near is worth the walk even with anvils left.
        public const float KeeperHuntReach = 12f;
        // The far anvils a missing cohort leaves: farther than this from where it stands.
        public const float MissedAnvilReach = 16f;
        // An anvil is on the screen within this of the hero (the band's half height, docs/62 section 2.1), and a
        // realistic pilot notices an off-screen one with NoticeShare's chance (drawn once an anvil a run).
        public const float OnScreenReach = 9f;
        public const float NoticeShare = .7f;
        // How near the stair's centre the pilot stops, inside its hold radius, and an anvil's, well inside its ring.
        public const float StairStop = .25f;
        public const float AnvilStop = .5f;

        public static Habits Of(AutopilotCohort cohort)
        {
            if (cohort == null)
                throw new ArgumentNullException(nameof(cohort));
            switch (cohort.Name)
            {
                case "standing":
                    return new Habits(true, false, false, float.PositiveInfinity);
                case "first-card":
                    return new Habits(true, true, true, float.PositiveInfinity);
                case "clumsy":
                    return new Habits(false, false, true, MissedAnvilReach);
                case "auto-farmer":
                    return new Habits(false, false, true, MissedAnvilReach);
                default:
                    return new Habits(true, true, false, float.PositiveInfinity);
            }
        }

        // Whether the pilot is leaving: the Bell has rung, or the key is held and the pilot is done (at once for a pilot
        // that leaves at once, else once no anvil it would go for is left). The stair then comes before anything else,
        // the kite included, as the finale's exit does (DevelopmentAutopilot, LeavesSwarm).
        public static bool Leaving(OutworksPhase phase, Habits habits, float x, float y, bool[] noticed)
        {
            if (phase == null)
                throw new ArgumentNullException(nameof(phase));
            if (phase.Left || !phase.StairOpen)
                return false;
            if (phase.BellRung || habits.LeavesAtOnce)
                return true;
            return NearestAnvil(phase, habits, x, y, noticed) < 0;
        }

        // The objective short of leaving: the Keeper when the pilot hunts and it is near or the anvils are done, else
        // the nearest anvil the pilot goes for; None when there is nothing (the pilot then walks as between halls: the
        // altars, the drops). The goal's point and where the pilot stops short of it.
        public static Objective Choose(OutworksPhase phase, Habits habits, float x, float y, bool keeperStands,
            float keeperX, float keeperY, bool[] noticed, out float goalX, out float goalY, out float stop)
        {
            if (phase == null)
                throw new ArgumentNullException(nameof(phase));
            goalX = x;
            goalY = y;
            stop = AnvilStop;
            if (phase.Left)
                return Objective.None;
            int anvil = NearestAnvil(phase, habits, x, y, noticed);
            bool keeper = keeperStands && habits.HuntsKeeper &&
                          (anvil < 0 || OutworksRule.Within(x, y, keeperX, keeperY, KeeperHuntReach));
            if (keeper)
            {
                goalX = keeperX;
                goalY = keeperY;
                // At the edge of a weapon's reach: the fight itself is the kite's and the weapons'.
                stop = 1.4f;
                return Objective.Keeper;
            }
            if (anvil >= 0)
            {
                goalX = phase.Anvils.XOf(anvil);
                goalY = phase.Anvils.YOf(anvil);
                return Objective.Anvil;
            }
            return Objective.None;
        }

        // The uncaptured anvil nearest the pilot that it goes for (within its reach, noticed), or -1.
        public static int NearestAnvil(OutworksPhase phase, Habits habits, float x, float y, bool[] noticed)
        {
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            AnvilField anvils = phase.Anvils;
            for (int i = 0; i < anvils.Count; i++)
            {
                if (anvils.IsCaptured(i) || (noticed != null && !noticed[i]))
                    continue;
                float distance = OutworksRule.DistanceSquared(x, y, anvils.XOf(i), anvils.YOf(i));
                if (!float.IsPositiveInfinity(habits.AnvilReach) && distance > (float)(habits.AnvilReach * habits.AnvilReach))
                    continue;
                if (distance < bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }
            return best;
        }
    }
}
