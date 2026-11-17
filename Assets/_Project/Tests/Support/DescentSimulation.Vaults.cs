using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Tests
{
    // The Quicksilver Vaults' own enemies (docs/50). The Mirror Sentry ships (the lead's decisions 2026-11-12): it leads
    // floor 2's Sentry Hall (DescentSimulation.QuicksilverVaults, MirrorSentry) with MirrorGuardRule's guard, stepped here
    // where the scene's MirrorGuardService steps it, and the step-in's flank (FootworkRule.Flank) circles it, as
    // DevelopmentAutopilot does. The Glass Regent is floor 2's gate boss (GlassRegent; the lead's decisions 2026-11-12):
    // its lanes (RegentLanes) are marked and judged where the scene's WardenSlamService marks them, and the pilots that
    // leave the Warden's mark away from it sidestep them (FootworkRule.Sidestep), as DevelopmentAutopilot does. The Silver
    // Wisp and the Faceless Clerk stay simulation-only research levers, off by default (VaultsEnemiesResearch): they exist
    // only on a floor 2 that VaultsChain reshapes, and every behaviour below keys on one of their definitions, the Sentry's
    // guard or the Regent, so a run that never meets them is the run it was - floor 1's rows of the default batch stay
    // byte-identical.
    internal static partial class DescentSimulation
    {
        // docs/50's starting values, authored (before the climb's floor-2 scaling).
        public static readonly Enemy SilverWisp = new Enemy { Name = "Silver Wisp", Health = 24f, Damage = 1f, Interval = .5f, Gold = 3, Experience = 10, Speed = 2.4f, Reach = 1.4f };
        // No melee strike (its weapon never ticks): its reach is the 2.8 it stands off at.
        public static readonly Enemy FacelessClerk = new Enemy { Name = "Faceless Clerk", Health = 70f, Damage = .01f, Interval = 1f, Gold = 8, Experience = 15, Speed = 1.2f, Reach = 2.8f };
        // The Regent's phase-2 copies: no strike, no reward; their health is a share of the Regent's maximum.
        public static readonly Enemy GlassCopy = new Enemy { Name = "Glass copy", Health = 1f, Damage = .01f, Interval = 1f, Gold = 0, Experience = 0, Speed = 1f, Reach = 1.7f };

        // Floor 1 as shipped above a floor 2 with the levers' creatures in place of the foundry's: the Grunt Captain back in
        // Sentry Hall (VaultsCaptain: floor 2 before 2026-11-12) or the Sentry at another health, the Wisp for the Runner
        // (the horde's third role and Mercury Stair's second wave), a Clerk at the head of Mercury Stair's first wave and in
        // place of a Grunt in Vault Gate's, the Forge Warden back at the gate at its own share (VaultsWarden: floor 2's gate
        // before 2026-11-12) or the Regent at another share (RegentHealthShare).
        // The Sentry at a share of its authored health: itself at 1, else a copy under its name (its guard reads the name).
        private static Enemy Sentry(float healthScale) => healthScale == 1f
            ? MirrorSentry
            : new Enemy
            {
                Name = MirrorSentry.Name, Health = MirrorSentry.Health * healthScale, Damage = MirrorSentry.Damage,
                Interval = MirrorSentry.Interval, InitialDelay = MirrorSentry.InitialDelay, Gold = MirrorSentry.Gold,
                Experience = MirrorSentry.Experience, Speed = MirrorSentry.Speed, Reach = MirrorSentry.Reach
            };

        public static FoundryFloor VaultsChain(FoundryLevers levers)
        {
            FoundryFloor one = FoundryFloorOne();
            FoundryFloor two = one.Next;
            var rooms = new Enemy[two.Rooms.Length][][];
            for (int r = 0; r < rooms.Length; r++)
            {
                if (two.Rooms[r] == null)
                    continue;
                rooms[r] = new Enemy[two.Rooms[r].Length][];
                for (int w = 0; w < rooms[r].Length; w++)
                {
                    Enemy[] wave = (Enemy[])two.Rooms[r][w].Clone();
                    for (int i = 0; i < wave.Length; i++)
                    {
                        if (levers.VaultsWisps && wave[i] == Runner)
                            wave[i] = SilverWisp;
                        if (wave[i] == MirrorSentry)
                            wave[i] = levers.VaultsCaptain ? Captain : Sentry(levers.SentryHealthScale);
                        if (levers.VaultsWarden && wave[i] == GlassRegent)
                            wave[i] = Warden;
                    }
                    rooms[r][w] = wave;
                }
            }
            if (levers.VaultsClerks)
            {
                Enemy[] stair = rooms[0][0];
                var led = new Enemy[stair.Length + 1];
                led[0] = FacelessClerk;
                Array.Copy(stair, 0, led, 1, stair.Length);
                rooms[0][0] = led;
                Enemy[] gate = rooms[2][0];
                int grunt = Array.IndexOf(gate, Grunt);
                if (grunt >= 0)
                {
                    gate[grunt] = gate[0];
                    gate[0] = FacelessClerk;
                }
            }
            FoundryFloor shaped = two.Reshaped(rooms, two.Kinds);
            if (levers.VaultsWarden)
                shaped.GateHealthShare = FloorTwoWardenShare;
            else if (levers.RegentHealthShare > 0f)
                shaped.GateHealthShare = levers.RegentHealthShare;
            if (levers.VaultsWisps)
                shaped.HordeRoles = new[] { Mite, Grunt, SilverWisp };
            one.Next = shaped;
            return one;
        }

        internal sealed partial class FoundryLevers
        {
            // docs/50's research creatures on floor 2, each off by default (VaultsChain reads them; the creatures' rules key on
            // their definitions). VaultsCaptain puts the Grunt Captain back at the head of Sentry Hall: floor 2 as it was
            // before the Mirror Sentry shipped (2026-11-12), for a comparison on the same seeds; VaultsWarden the Forge
            // Warden back at its gate, at its share then (FloorTwoWardenShare): floor 2's gate before the Glass Regent.
            public bool VaultsCaptain;
            public bool VaultsWisps;
            public bool VaultsClerks;
            public bool VaultsWarden;
            // The Mirror Sentry's guard (MirrorGuardRule, shipped; a research lever may move each): its turn, front arc, the
            // share of a front landing it takes, the reflection's share and its cap a second.
            public float SentryTurnDegrees = MirrorGuardRule.TurnDegrees;
            public float SentryArcDegrees = MirrorGuardRule.ArcDegrees;
            public float SentryFrontShare = MirrorGuardRule.FrontShare;
            public float SentryReflectShare = MirrorGuardRule.ReflectShare;
            public float SentryReflectCap = MirrorGuardRule.ReflectCap;
            // The step-in's flank round the Sentry (FootworkRule.Flank), as DevelopmentAutopilot circles it: shipped, for a
            // cohort that StepsIn (careful, first-card) when its best single-target weapon is short (the Daggers).
            public bool FlanksTheSentry = true;
            // The Sentry's health against its authored 200 (VaultsChain only; research: the fight's length against the flank).
            public float SentryHealthScale = 1f;
            // The Silver Wisp's blink (section 3): once, at an age of WispBlinkAge or more and WispBlinkMin-Max from the
            // hero, to WispLandDistance past the hero on the far side, a shimmer later; no strike for the hold after.
            public float WispBlinkAge = 1.5f;
            public float WispBlinkMin = 3f;
            public float WispBlinkMax = 7f;
            public float WispLandDistance = 3f;
            public float WispShimmerSeconds = .6f;
            public float WispArrivalHoldSeconds = .5f;
            // Round 2's alternative (research): the blink comes only once the hero has walked in a straight line (its
            // heading within WispKiteDegrees of where the line began) for WispKiteSeconds, and lands WispLandDistance
            // ahead along that line instead of mirrored. Off: section 3's blink.
            public bool WispKiteBlink;
            public float WispKiteSeconds = 1.5f;
            public float WispKiteDegrees = 20f;
            // The Faceless Clerk's copy (section 4): every ClerkEvery seconds (its first ClerkFirst after it walks in) a
            // mark of ClerkRadius where the hero stands, ClerkShare of its maximum a ClerkTelegraph later; the packs walk to
            // it while it stands; at most ClerkCopiesMax on the floor.
            public float ClerkEvery = 5f;
            public float ClerkFirst = 1.5f;
            public float ClerkTelegraph = 1.1f;
            public float ClerkRadius = 1.6f;
            public float ClerkShare = .1f;
            public int ClerkCopiesMax = 2;
            public bool ClerkDrawsPacks = true;
            // Round 2's alternative (research): the copy has no ring and no timer. It draws the packs and bursts as the
            // first pack body comes within ClerkTouch of it, hitting the hero for ClerkShare if it stands within
            // ClerkBlastRadius then; untouched for ClerkFadeSeconds it fades. A hero that has walked off is safe, one that
            // stands where the copy was is not. Off: section 4's copy, a mark that lands a telegraph later.
            public bool ClerkReachBlast;
            public float ClerkTouch = 1f;
            public float ClerkBlastRadius = 2.4f;
            public float ClerkFadeSeconds = 3f;
            // The Glass Regent (RegentRule, shipped; a research lever may move each) on the Warden's clock, tuned to
            // RegentEvery, RegentEveryPhaseTwo and RegentTelegraph: a lane RegentLaneWidth wide and RegentLaneLength long
            // from each body through the hero's spot, RegentLaneShare of its maximum once a beat; RegentCopies copies at
            // half health, each RegentCopyShare of its maximum (research: section 6's stage 2). RegentHealthShare
            // (VaultsChain only) is its gate share in place of the floor's (FloorTwoGateShare); 0 keeps the floor's.
            public float RegentLaneWidth = RegentRule.LaneWidth;
            public float RegentLaneLength = RegentRule.LaneLength;
            // How far past the hero's spot a lane reaches at the least (RegentRule.LanePast); below zero, no farther than
            // RegentLaneLength from the Regent, as the lane was before 2026-11-15.
            public float RegentLanePast = RegentRule.LanePast;
            public float RegentLaneShare = RegentRule.LaneShare;
            public float RegentEvery = RegentRule.Every;
            public float RegentEveryPhaseTwo = RegentRule.EveryPhaseTwo;
            public float RegentTelegraph = RegentRule.Telegraph;
            public int RegentCopies;
            public float RegentCopyShare = .06f;
            public float RegentHealthShare;
            // The sidestep across the Regent's lanes (FootworkRule.Sidestep), as DevelopmentAutopilot steps it: shipped, for
            // a cohort that leaves the Warden's mark away from it (AutopilotCohort.Sidesteps: careful, standing, first-card).
            // Off (research), those cohorts leave a lane's mark as they leave the Warden's, away from the Regent: along it.
            public bool SidestepsTheRegent = true;
            // The Regent's round of 2026-11-17 (docs/50 "The Regent's bite for the standing hero"), research, off by default:
            // a hero that has STOOD - whose spot as a mark goes down lies within RegentStoodWalk of the spot where the last
            // blow found it (none on a fight's first beat), or, with RegentNearReach above 0 instead, that stands within it of
            // the Regent as the mark goes down - draws RegentStoodLanes more lanes through the mark, cast from the Regent's
            // spot turned RegentStoodTurn degrees either way round it (the next pair twice as far), and the beat's blow takes
            // RegentStoodShare of its maximum in place of the lane's share (0: the lane's share).
            public float RegentStoodWalk;
            public float RegentNearReach;
            public int RegentStoodLanes;
            public float RegentStoodTurn = 120f;
            public float RegentStoodShare;
            // ...and the Regent's rod free through its telegraph (research): held only while the shards are in the floor
            // (the recovery after the blow), not from the mark - the lane is cast by its mirror, not its hands.
            public bool RegentRodInTelegraph;
            // ...or free through the telegraph only on a beat whose mark went down with the hero within RegentRodNearReach of
            // the Regent (0: off); and its hold after the blow, RegentRodRecovery seconds in place of the Warden's 1.5 (below
            // zero: the Warden's) - the shards run down the lane, no hammer stands in the floor.
            public float RegentRodNearReach;
            public float RegentRodRecovery = -1f;
            // ...or, with RegentRodNearReach, the rod held through that beat's telegraph as ever but not after its blow.
            public bool RegentRodNearAfterBlow;
            // ...or no hold after a blow that found the hero within RegentRodBlowReach of the Regent (0: off).
            public float RegentRodBlowReach;
            // ...only where the hero was within it as the mark went down too: it stayed at the Regent's side through the cast.
            public bool RegentRodBlowStayed;
            // The round's candidates (RegentRule.AtItsFeet, RegentRule.HoldsRod): the Regent holds its rod through its cast
            // and the shards' run only for a hero beyond RegentFeetReach of it (0: off, the Regent before the round) - read as
            // each mark goes down, for the whole beat (candidate A, "the rod at its feet"), or every frame of the hold with
            // RegentFeetEveryFrame (candidate B, "never at its feet").
            public float RegentFeetReach;
            public bool RegentFeetEveryFrame;
            // The rod's streak (research): a strike on a hero that has stayed within the rod's reach since the Regent's last
            // strike adds RegentRodStreakShare of the rod's damage for each strike of the streak before it, at most
            // RegentRodStreakMost of them (0: off).
            public float RegentRodStreakShare;
            public int RegentRodStreakMost = 2;

            public bool Vaults => VaultsWisps || VaultsClerks;

            // The Sentry's guard as these levers tune it.
            public MirrorGuardState SentryGuard() =>
                new MirrorGuardState(SentryTurnDegrees, SentryArcDegrees, SentryFrontShare, SentryReflectShare, SentryReflectCap);
        }

        internal sealed partial class FoundryResult
        {
            // docs/50's metrics, 0 on any floor without the four.
            public int SentryLandings;
            public int SentryFrontLandings;
            public float SentryReflected;
            public int WispBlinks;
            public int WispArrivalsInReach;
            public float TakenFromWisps;
            public int ClerkCopies;
            public int ClerkCopyHits;
            // Round 2's alternative Clerk (FoundryLevers.ClerkReachBlast): its copies burst by a pack, and those that faded.
            public int ClerkBursts;
            public int ClerkFades;
            public float DrawnSeconds;
            public int RegentBeats;
            public int RegentLaneHits;
            public int RegentCopiesShattered;
            public float RegentPhaseTwoAt = -1f;
            // The gate boss's fight as the hero stood in it (accounting only, docs/50's Regent round of 2026-11-17): seconds
            // of the fight while it runs (not held), of those the seconds the hero walked under GateStillSpeed and the
            // seconds it stood within the boss's own weapon's reach; and over the beats, the hero's walk from where the
            // last blow found it to where the next mark went down, and its distance from the boss at the mark.
            // The Regent's beats on a hero that had stood (FoundryLevers.RegentStoodWalk or RegentNearReach), and of those the
            // ones whose blow caught it.
            public int RegentStoodBeats;
            public int RegentStoodHits;
            // The beats on which the rod stayed free through the telegraph or after the blow (FoundryLevers.RegentRodNearReach,
            // RegentRodBlowReach), and the marks that went down with the hero at the Regent's feet (FoundryLevers.RegentFeetReach).
            public int RegentRodNearBeats;
            public int RegentFeetBeats;
            // The rod's strikes that came in a streak (FoundryLevers.RegentRodStreakShare).
            public int RegentStreakStrikes;
            public float GateWatchSeconds;
            public float GateStillSeconds;
            public float GateInBossReachSeconds;
            // ...of those, the seconds the boss's weapon was not held (it could swing).
            public float GateThreatSeconds;
            public int GateBeatsWatched;
            public float GateBeatWalkSum;
            public float GateMarkDistanceSum;
            // The marks that went down with the hero within each of GateMarkReaches of the boss (accounting only).
            public static readonly float[] GateMarkReaches = { 1.0f, 1.2f, 1.4f, 1.5f, 1.6f, 1.7f, 1.8f, 2.0f };
            public int[] GateMarkWithin = new int[GateMarkReaches.Length];
        }

        private sealed partial class Foe
        {
            // The Mirror Sentry's guard (MirrorGuardRule, the scene's MirrorGuard): null until its first step - and for every
            // other enemy.
            public MirrorGuardState Guard;
            // The Wisp's blink: spent (done or skipped), and while it shimmers the run second it lands and where.
            public bool Blinked;
            public float BlinkAt = -1f;
            public float BlinkX;
            public float BlinkY;
            // The Clerk's copy clock (NaN until first stepped).
            public float ClerkClock = float.NaN;
        }

        private sealed partial class FoundryRun
        {
            // The lanes of the Regent's beat standing now (RegentLanes, as WardenSlamService holds them): each body's spot as
            // the mark went down, through the mark.
            private RegentLanes _regentLanes;
            // Where the Regent's last blow found the hero (NaN before a fight's first blow), and whether the beat down is on a
            // hero that had stood (FoundryLevers.RegentStoodWalk, research).
            private float _regentFromX = float.NaN;
            private float _regentFromY;
            private bool _regentStood;
            // Whether the beat down went down with the hero within FoundryLevers.RegentRodNearReach of the Regent (research).
            private bool _regentRodNear;
            // Whether the last blow found the hero within FoundryLevers.RegentRodBlowReach of the Regent (research).
            private bool _regentBlowNear;
            private bool _regentMarkNear;
            // Whether the hero stands at the Regent's feet (FoundryLevers.RegentFeetReach): as the beat's mark went down, or this
            // frame (RegentFeetEveryFrame).
            private bool _regentAtFeet;
            // The rod's streak (research): strikes in a row on a hero that stayed within its reach, and whether it has.
            private int _rodStreak;
            private bool _rodKept;
            private readonly Foe[] _glassCopies = new Foe[2];
            // Reflection owed to the hero by a Sentry, paid on the next running frame (MirrorReflectionDue, as
            // MirrorGuardService pays it: never inside the hero's own strike, never while the fight holds).
            private readonly MirrorReflectionDue<Foe> _reflectDue = new MirrorReflectionDue<Foe>();
            private bool _reflecting;
            // Whether the floor holds a Mirror Sentry (EnterFloor): its guard is stepped only there.
            private bool _sentryFloor;
            // Round 2's alternatives: the hero's straight walk (FoundryLevers.WispKiteBlink) - since when, and its heading
            // then, NaN while it is not walking - and the Clerk's ringless copies (FoundryLevers.ClerkReachBlast).
            private float _straightSince = float.NaN;
            private float _straightX;
            private float _straightY;
            private float _walkFromX = float.NaN;
            private float _walkFromY;
            private readonly System.Collections.Generic.List<HazardMark> _silentCopies = new System.Collections.Generic.List<HazardMark>(2);

            private bool VaultsOn => _levers.Vaults && _floor.FloorNumber >= 2;

            // The gate fight's watch (accounting only): the hero's spot last frame and at the last blow (NaN before).
            private const float GateStillSpeed = 1f;
            private float _gateWatchX = float.NaN;
            private float _gateWatchY;
            private float _gateBlowX = float.NaN;
            private float _gateBlowY;

            private static bool IsRegent(Foe foe) => foe != null && ReferenceEquals(foe.Definition, GlassRegent);

            // The Sentry or a copy of it with another health (FoundryLevers.SentryHealthScale).
            private static bool IsSentry(Enemy kind) => ReferenceEquals(kind, MirrorSentry) || kind.Name == MirrorSentry.Name;

            // Whether a floor's rooms hold a Mirror Sentry anywhere.
            private static bool HoldsASentry(FoundryFloor floor)
            {
                for (int r = 0; r < floor.Rooms.Length; r++)
                for (int w = 0; floor.Rooms[r] != null && w < floor.Rooms[r].Length; w++)
                for (int e = 0; e < floor.Rooms[r][w].Length; e++)
                    if (IsSentry(floor.Rooms[r][w][e]))
                        return true;
                return false;
            }

            // Neither the Clerk nor a glass copy strikes in melee: no weapon ticks, and the pilot does not kite from them.
            private static bool Harmless(Foe foe) =>
                ReferenceEquals(foe.Definition, FacelessClerk) || ReferenceEquals(foe.Definition, GlassCopy);

            // Where the packs walk: to the newest standing Clerk copy (ClerkDrawsPacks), else to the hero.
            private float PackGoalX => DrawingCopy(out HazardMark copy) ? copy.X : HeroX;
            private float PackGoalY => DrawingCopy(out HazardMark copy) ? copy.Y : HeroY;

            private bool DrawingCopy(out HazardMark copy)
            {
                copy = null;
                if (_silentCopies.Count > 0 && _levers.ClerkDrawsPacks)
                {
                    copy = _silentCopies[_silentCopies.Count - 1];
                    return true;
                }
                if (_marks.Count == 0 || !_levers.ClerkDrawsPacks)
                    return false;
                for (int i = _marks.Count - 1; i >= 0; i--)
                {
                    if (_marks[i].Harm != ClerkHarm)
                        continue;
                    copy = _marks[i];
                    return true;
                }
                return false;
            }

            private const string ClerkHarm = "Clerk copy";

            // The four's clocks, stepped where the Warden's slam is (docs/50 rule 4; MirrorGuardService's -39 after
            // WardenSlamService's -40), held while a choice is open or the reveal holds. Nothing on a floor without them.
            private void StepVaults()
            {
                if (_run.HasEnded)
                    return;
                WatchGate();
                if (_sentryFloor)
                    StepSentries();
                // The Regent fallen: its copies shatter with it.
                if (_glassCopies[0] != null && !_choices.IsOpen && !_revealHold &&
                    (GateBoss == null || !GateBoss.IsAlive || !IsRegent(GateBoss)))
                    ShatterCopies();
                if (!VaultsOn || _choices.IsOpen || _revealHold)
                    return;
                if (_levers.WispKiteBlink)
                    TrackStraightWalk();
                if (_silentCopies.Count > 0)
                    StepSilentCopies();
                if (DrawingCopy(out _))
                    _result.DrawnSeconds += Step;
                int copies = _silentCopies.Count;
                for (int i = 0; i < _marks.Count; i++)
                    if (_marks[i].Harm == ClerkHarm)
                        copies++;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (!IsValid(foe) || foe.SpawnFrame == _frame)
                        continue;
                    Enemy kind = foe.Definition;
                    if (ReferenceEquals(kind, SilverWisp))
                        StepWisp(foe);
                    else if (ReferenceEquals(kind, FacelessClerk) && _heroHealth.IsAlive)
                    {
                        if (float.IsNaN(foe.ClerkClock))
                            foe.ClerkClock = _levers.ClerkEvery - _levers.ClerkFirst;
                        foe.ClerkClock += Step;
                        if (foe.ClerkClock >= _levers.ClerkEvery && copies < _levers.ClerkCopiesMax)
                        {
                            foe.ClerkClock = 0f;
                            copies++;
                            _result.ClerkCopies++;
                            if (_levers.ClerkReachBlast)
                                _silentCopies.Add(new HazardMark
                                {
                                    X = HeroX, Y = HeroY, Radius = _levers.ClerkBlastRadius, Onset = _seconds,
                                    LandsAt = _seconds + _levers.ClerkFadeSeconds, Share = _levers.ClerkShare, Harm = ClerkHarm
                                });
                            else
                                Mark(HeroX, HeroY, _levers.ClerkRadius, _levers.ClerkTelegraph, _levers.ClerkShare, ClerkHarm, HeroX, HeroY);
                        }
                    }
                }
            }

            // The gate fight's watch: accounting only, nothing reads it.
            private void WatchGate()
            {
                Foe boss = _slamBoss != null && _slamBoss.IsAlive ? _slamBoss : null;
                if (boss == null || _choices.IsOpen || _revealHold || !_heroHealth.IsAlive)
                {
                    _gateWatchX = float.NaN;
                    if (boss == null)
                        _gateBlowX = float.NaN;
                    return;
                }
                float x = HeroX, y = HeroY;
                _result.GateWatchSeconds += Step;
                if (!float.IsNaN(_gateWatchX) &&
                    ArenaFloor.FloorDistanceSquared(x, y, _gateWatchX, _gateWatchY) < GateStillSpeed * Step * GateStillSpeed * Step)
                    _result.GateStillSeconds += Step;
                _gateWatchX = x;
                _gateWatchY = y;
                float reach = boss.Weapon.Range;
                bool inReach = ArenaFloor.FloorDistanceSquared(x, y, boss.X, boss.Y) <= reach * reach;
                if (!inReach)
                    _rodKept = false;
                if (inReach)
                {
                    _result.GateInBossReachSeconds += Step;
                    if (!GateBossHeld(boss))
                        _result.GateThreatSeconds += Step;
                }
            }

            // The watch at a mark and at a blow.
            private void WatchMark(Foe boss)
            {
                float near = ArenaFloor.FloorDistanceSquared(_markX, _markY, boss.X, boss.Y);
                for (int i = 0; i < FoundryResult.GateMarkReaches.Length; i++)
                    if (near <= FoundryResult.GateMarkReaches[i] * FoundryResult.GateMarkReaches[i])
                        _result.GateMarkWithin[i]++;
                if (!float.IsNaN(_gateBlowX))
                {
                    _result.GateBeatsWatched++;
                    _result.GateBeatWalkSum += (float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(_markX, _markY, _gateBlowX, _gateBlowY));
                    _result.GateMarkDistanceSum += (float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(_markX, _markY, boss.X, boss.Y));
                }
            }

            private void WatchBlow()
            {
                _gateBlowX = HeroX;
                _gateBlowY = HeroY;
            }

            // MirrorGuardService.Update: while the fight runs, the reflection banked since the last running frame paid as one
            // hit with the Sentry as its source, then every standing Sentry's guard stepped (not on the frame it walks in);
            // while it holds (a choice, the reveal), neither.
            private void StepSentries()
            {
                bool held = _choices.IsOpen || _revealHold;
                float owed = _reflectDue.Take(!held && _heroHealth.IsAlive, out Foe from);
                if (owed > 0f)
                {
                    _reflecting = true;
                    _harmIsSlam = false;
                    _hero.ApplyDamage(new DamageContext(owed, from));
                    _reflecting = false;
                }
                if (held)
                    return;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (!IsValid(foe) || foe.SpawnFrame == _frame || !IsSentry(foe.Definition))
                        continue;
                    foe.Guard ??= _levers.SentryGuard();
                    foe.Guard.Step(Step, foe.X, foe.Y, HeroX, HeroY, _heroHealth.Maximum);
                }
            }

            private void StepWisp(Foe foe)
            {
                if (foe.BlinkAt >= 0f)
                {
                    if (_seconds < foe.BlinkAt)
                        return;
                    foe.BlinkAt = -1f;
                    float speed = foe.Definition.Speed;
                    foe.Motion.Vacate(foe.Slot);
                    foe.Motion.Replace(foe.Slot, foe, new EntryPlacement(EntrySides.SideOf(0, 0), 0f, foe.BlinkX, foe.BlinkY),
                        speed, foe.Weapon.Range);
                    _result.WispBlinks++;
                    float reach = foe.Weapon.Range;
                    if (ArenaFloor.FloorDistanceSquared(HeroX, HeroY, foe.BlinkX, foe.BlinkY) <= reach * reach)
                        _result.WispArrivalsInReach++;
                    return;
                }
                if (foe.Blinked || _seconds - foe.SpawnedAt < _levers.WispBlinkAge)
                    return;
                if (_levers.WispKiteBlink && (float.IsNaN(_straightSince) || _seconds - _straightSince < _levers.WispKiteSeconds))
                    return;
                float dx = HeroX - foe.X, dy = HeroY - foe.Y;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d < _levers.WispBlinkMin || d > _levers.WispBlinkMax)
                    return;
                foe.Blinked = true;
                // Section 3: mirrored through the hero; round 2's alternative: ahead along the hero's straight walk.
                float ax = _levers.WispKiteBlink ? _straightX : dx / d;
                float ay = _levers.WispKiteBlink ? _straightY : dy / d;
                float lx = HeroX + ax * _levers.WispLandDistance;
                float ly = HeroY + ay * _levers.WispLandDistance;
                if (!FoundryMap.Contains(lx, ly, HeroMotion.EdgeMargin))
                    return;
                foe.BlinkX = lx;
                foe.BlinkY = ly;
                foe.BlinkAt = _seconds + _levers.WispShimmerSeconds;
                foe.StaggeredUntil = Math.Max(foe.StaggeredUntil, foe.BlinkAt + _levers.WispArrivalHoldSeconds);
            }

            // The hero's straight walk: moving faster than half a unit a second with its heading within WispKiteDegrees of
            // the heading the line began on.
            private void TrackStraightWalk()
            {
                float x = HeroX, y = HeroY;
                if (float.IsNaN(_walkFromX))
                {
                    _walkFromX = x;
                    _walkFromY = y;
                    return;
                }
                float dx = x - _walkFromX, dy = y - _walkFromY;
                _walkFromX = x;
                _walkFromY = y;
                float moved = (float)Math.Sqrt(dx * dx + dy * dy);
                if (moved < .5f * Step)
                {
                    _straightSince = float.NaN;
                    return;
                }
                dx /= moved;
                dy /= moved;
                if (!float.IsNaN(_straightSince) &&
                    dx * _straightX + dy * _straightY >= (float)Math.Cos(_levers.WispKiteDegrees * Math.PI / 180.0))
                    return;
                _straightSince = _seconds;
                _straightX = dx;
                _straightY = dy;
            }

            // Round 2's Clerk copies: each bursts as a pack body (the horde, or a node wave's enemy that is not a Clerk)
            // comes within ClerkTouch of it - the hero hit if within ClerkBlastRadius then - or fades untouched.
            private void StepSilentCopies()
            {
                float touch = _levers.ClerkTouch * _levers.ClerkTouch;
                for (int c = _silentCopies.Count - 1; c >= 0; c--)
                {
                    HazardMark copy = _silentCopies[c];
                    bool touched = false;
                    for (int i = 0; i < TargetCount && !touched; i++)
                    {
                        Foe foe = TargetAt(i);
                        touched = IsValid(foe) && !Harmless(foe) && !foe.GateBoss &&
                                  ArenaFloor.FloorDistanceSquared(foe.X, foe.Y, copy.X, copy.Y) <= touch;
                    }
                    if (!touched)
                    {
                        if (_seconds < copy.LandsAt)
                            continue;
                        _silentCopies.RemoveAt(c);
                        _result.ClerkFades++;
                        continue;
                    }
                    _silentCopies.RemoveAt(c);
                    _result.ClerkBursts++;
                    if (!_heroHealth.IsAlive ||
                        ArenaFloor.FloorDistanceSquared(HeroX, HeroY, copy.X, copy.Y) > copy.Radius * copy.Radius)
                        continue;
                    _result.HazardHits++;
                    _hazardHarm = ClerkHarm;
                    _harmIsSlam = false;
                    _hero.ApplyDamage(new DamageContext(_heroHealth.Maximum * copy.Share));
                    _hazardHarm = null;
                }
            }

            // FoeDamaged's hook, MirrorGuard.Guard: the Sentry's guard on a landing of the hero's - its weapons' and its
            // ability's; a burn's, a bleed's or the pit's is no landing - while the hero stands in its front arc, and the
            // reflection it owes (capped by its budget). The landing as it was anywhere else.
            private DamageContext SentryGuard(Foe foe, DamageContext context, DamageSource source)
            {
                if (foe.Guard == null || (source != DamageSource.Weapon && source != DamageSource.Ability))
                    return context;
                float dealt = foe.Guard.Land(context.Amount, foe.X, foe.Y, HeroX, HeroY, out bool front, out float reflected);
                if (source == DamageSource.Weapon)
                {
                    _result.SentryLandings++;
                    if (front)
                        _result.SentryFrontLandings++;
                }
                if (!front)
                    return context;
                _reflectDue.Owe(reflected, foe);
                return new DamageContext(dealt, context.Source, context.IsCritical);
            }

            // HeroDamaged's hook: the four's harm, for the report.
            private void VaultsTaken(DamageContext context, float taken)
            {
                if (_reflecting)
                    _result.SentryReflected += taken;
                else if (context.Source is Foe foe && ReferenceEquals(foe.Definition, SilverWisp))
                    _result.TakenFromWisps += taken;
                if (_hazardHarm == ClerkHarm)
                    _result.ClerkCopyHits++;
            }

            // StepWarden's hooks for the Regent: its phase two (the copies), its lanes as the mark goes down, and whether
            // the beat's lanes catch the hero.
            private void RegentPhaseTwo(Foe boss)
            {
                _result.RegentPhaseTwoAt = _seconds - _waveStartedAt;
                for (int c = 0; c < _levers.RegentCopies && c < _glassCopies.Length; c++)
                {
                    if (_waveMotion.Count >= PackLayout.MaxPackSize)
                        break;
                    double turn = (c == 0 ? 120.0 : -120.0) * Math.PI / 180.0;
                    float ox = boss.X - HeroX, oy = boss.Y - HeroY;
                    float cos = (float)Math.Cos(turn), sin = (float)Math.Sin(turn);
                    float x = HeroX + ox * cos - oy * sin;
                    float y = HeroY + ox * sin + oy * cos;
                    if (!FoundryMap.Contains(x, y, HeroMotion.EdgeMargin))
                    {
                        x = boss.X;
                        y = boss.Y;
                    }
                    Foe copy = CreateFoe(GlassCopy, Math.Max(1f, boss.Health.Maximum * _levers.RegentCopyShare), 0f, false);
                    copy.Gold = 0;
                    copy.Experience = 0;
                    copy.Motion = _waveMotion;
                    copy.Slot = _waveMotion.Add(copy, new EntryPlacement(EntrySides.SideOf(0, 0), 0f, x, y), boss.Definition.Speed,
                        GlassCopy.Reach);
                    _wave.Add(copy);
                    _glassCopies[c] = copy;
                }
            }

            // WardenSlamService.Update for the Regent, every running frame of its fight before the clock steps (candidate B,
            // FoundryLevers.RegentFeetEveryFrame): whether the hero stands at its feet now.
            private void RegentFeet(Foe boss)
            {
                if (_levers.RegentFeetReach > 0f && _levers.RegentFeetEveryFrame)
                    _regentAtFeet = RegentRule.AtItsFeet(HeroX, HeroY, boss.X, boss.Y, _levers.RegentFeetReach);
            }

            // WardenSlamService.Marked for the Regent: a lane from its body - and each standing copy's - through the mark.
            private void RegentMarked(Foe boss)
            {
                _result.RegentBeats++;
                _regentLanes ??= new RegentLanes(_levers.RegentLaneWidth, _levers.RegentLaneLength, _levers.RegentLanePast);
                _regentLanes.Mark(_markX, _markY);
                _regentLanes.Add(boss.X, boss.Y);
                if (_levers.RegentFeetReach > 0f && !_levers.RegentFeetEveryFrame)
                {
                    _regentAtFeet = _regentLanes.AtItsFeet(_levers.RegentFeetReach);
                    if (_regentAtFeet)
                        _result.RegentFeetBeats++;
                }
                else if (_levers.RegentFeetReach > 0f && _regentLanes.AtItsFeet(_levers.RegentFeetReach))
                    _result.RegentFeetBeats++;
                _regentMarkNear = _levers.RegentRodBlowReach > 0f &&
                                  RegentRule.Stood(_markX, _markY, boss.X, boss.Y, _levers.RegentRodBlowReach);
                _regentRodNear = _levers.RegentRodNearReach > 0f &&
                                 RegentRule.Stood(_markX, _markY, boss.X, boss.Y, _levers.RegentRodNearReach);
                if (_regentRodNear)
                    _result.RegentRodNearBeats++;
                _regentStood = _levers.RegentNearReach > 0f
                    ? RegentRule.Stood(_markX, _markY, boss.X, boss.Y, _levers.RegentNearReach)
                    : _levers.RegentStoodWalk > 0f && !float.IsNaN(_regentFromX) &&
                      RegentRule.Stood(_markX, _markY, _regentFromX, _regentFromY, _levers.RegentStoodWalk);
                if (_regentStood)
                {
                    _result.RegentStoodBeats++;
                    for (int k = 0; k < _levers.RegentStoodLanes; k++)
                    {
                        float turn = _levers.RegentStoodTurn * (k / 2 + 1) * (k % 2 == 0 ? 1f : -1f);
                        RegentRule.TurnedRound(boss.X, boss.Y, _markX, _markY, turn, out float cx, out float cy);
                        _regentLanes.Add(cx, cy);
                    }
                }
                for (int c = 0; c < _glassCopies.Length; c++)
                {
                    Foe copy = _glassCopies[c];
                    if (copy == null || !IsValid(copy))
                        continue;
                    _regentLanes.Add(copy.X, copy.Y);
                }
            }

            // WardenSlamService.Slammed for the Regent: whether the beat's lanes catch the hero (once, however many it
            // stands in); the lanes go with the blow.
            private bool RegentLanesHit()
            {
                bool hit = _heroHealth.IsAlive && _regentLanes != null && _regentLanes.Holds(HeroX, HeroY, 0f);
                if (hit)
                {
                    _result.RegentLaneHits++;
                    if (_regentStood)
                        _result.RegentStoodHits++;
                }
                _regentLanes?.Clear();
                _regentFromX = HeroX;
                _regentFromY = HeroY;
                _regentBlowNear = _levers.RegentRodBlowReach > 0f && _slamBoss != null &&
                                  RegentRule.Stood(HeroX, HeroY, _slamBoss.X, _slamBoss.Y, _levers.RegentRodBlowReach) &&
                                  (!_levers.RegentRodBlowStayed || _regentMarkNear);
                if (_regentBlowNear)
                    _result.RegentRodNearBeats++;
                return hit;
            }

            // Whether the gate boss's weapon is held now (WardenSlamService.Hold): the clock's hold, or for a Regent with its rod
            // free through the telegraph (research) the recovery's alone.
            private bool GateBossHeld(Foe foe)
            {
                if (!IsRegent(foe))
                    return _slam.HoldsWeaponFor(_levers.WardenRecoverySeconds);
                float recovery = _levers.RegentRodRecovery >= 0f ? _levers.RegentRodRecovery : _levers.WardenRecoverySeconds;
                bool holds = _slam.HoldsWeaponFor(recovery);
                // The research levers' holds (the round's exploration).
                if (holds && _slam.IsMarked && (_levers.RegentRodInTelegraph || (_regentRodNear && !_levers.RegentRodNearAfterBlow)))
                    holds = false;
                if (holds && !_slam.IsMarked && ((_regentRodNear && _levers.RegentRodNearAfterBlow) || _regentBlowNear))
                    holds = false;
                return RegentRule.HoldsRod(holds, _regentAtFeet);
            }

            // The rod's streak (research): the strike just landed, and the shards it adds on a hero that stayed at its side.
            private void RegentRodStruck(Foe boss)
            {
                _rodStreak = _rodKept ? _rodStreak + 1 : 1;
                _rodKept = true;
                int before = Math.Min(_rodStreak - 1, _levers.RegentRodStreakMost);
                if (before <= 0 || !_heroHealth.IsAlive)
                    return;
                _result.RegentStreakStrikes++;
                _harmIsSlam = false;
                _hero.ApplyDamage(new DamageContext(boss.Weapon.Damage * _levers.RegentRodStreakShare * before, boss));
            }

            // The share of the hero's maximum the Regent's blow takes: the lane's, or a stood hero's (research).
            private float RegentBlowShare() =>
                _regentStood && _levers.RegentStoodShare > 0f ? _levers.RegentStoodShare : _levers.RegentLaneShare;

            // A new gate boss (StepWarden): the clock keeps the Regent's beat for a Regent, the Warden's otherwise, and no
            // lane is down.
            private void RegentTune(Foe gate)
            {
                _regentLanes?.Clear();
                _regentFromX = float.NaN;
                _regentStood = false;
                _regentRodNear = false;
                _regentBlowNear = false;
                _regentMarkNear = false;
                _regentAtFeet = false;
                _rodStreak = 0;
                _rodKept = false;
                if (gate != null && IsRegent(gate))
                    _slam.Tune(_levers.RegentEvery, _levers.RegentEveryPhaseTwo, _levers.RegentTelegraph);
                else
                    _slam.Tune(WardenRule.SlamEvery, WardenRule.SlamEveryEnraged, WardenRule.Telegraph);
            }

            // A copy felled: no kill, no reward, and the wave's count never held it.
            private bool GlassCopyFell(Foe foe)
            {
                if (!ReferenceEquals(foe.Definition, GlassCopy))
                    return false;
                if (!foe.Released)
                    _result.RegentCopiesShattered++;
                return true;
            }

            private void ShatterCopies()
            {
                for (int c = 0; c < _glassCopies.Length; c++)
                {
                    Foe copy = _glassCopies[c];
                    _glassCopies[c] = null;
                    if (copy == null || copy.Defeated)
                        continue;
                    copy.Released = true;
                    copy.Defeated = true;
                    if (copy.Motion != null && copy.Slot < copy.Motion.Count)
                        copy.Motion.Vacate(copy.Slot);
                }
            }

            // DevelopmentAutopilot.Goal's sidestep: against the Regent's lanes, a cohort that leaves the Warden's mark away from
            // it (AutopilotCohort.Sidesteps), given the habit (FoundryLevers.SidestepsTheRegent).
            private bool SidestepsLanes(AutopilotCohort cohort) =>
                _regentLanes != null && _regentLanes.Count > 0 && IsRegent(_slamBoss) && _levers.SidestepsTheRegent &&
                cohort.Sidesteps;

            // The nearest spot on the floor clear of every lane (FootworkRule.Sidestep); false when the hero already stands
            // clear.
            private bool SidestepGoal(float x, float y, out float gx, out float gy) =>
                FootworkRule.Sidestep(x, y, _regentLanes, out gx, out gy);

            // DevelopmentAutopilot.StepInGoal's flank: against a Sentry whose guard has been stepped, the step-in's spot turned
            // round it toward its rear (FootworkRule.Flank).
            private void Flank(Foe target, float x, float y, ref float gx, ref float gy)
            {
                if (!_levers.FlanksTheSentry || target.Guard == null || !target.Guard.IsGuarding)
                    return;
                FootworkRule.Flank(x, y, target.X, target.Y, target.Guard.Facing, ref gx, ref gy);
            }

            // The back-liner: once a wave's living enemies are Clerks alone, walk to the nearest until the best weapon
            // reaches it (docs/50 section 4's habit; every cohort, or a hall with a Clerk would never clear).
            private bool BackLinerGoal(float x, float y, out float gx, out float gy, out float stop)
            {
                gx = x;
                gy = y;
                stop = 0f;
                if (!VaultsOn || _waveAlive == 0 || GateBoss != null)
                    return false;
                Foe nearest = null;
                float best = float.PositiveInfinity;
                for (int i = 0; i < _wave.Count; i++)
                {
                    Foe foe = _wave[i];
                    if (!IsValid(foe))
                        continue;
                    if (!ReferenceEquals(foe.Definition, FacelessClerk))
                        return false;
                    float d = ArenaFloor.FloorDistanceSquared(x, y, foe.X, foe.Y);
                    if (d < best)
                    {
                        best = d;
                        nearest = foe;
                    }
                }
                if (nearest == null)
                    return false;
                gx = nearest.X;
                gy = nearest.Y;
                stop = 1.4f;
                return true;
            }
        }
    }
}
