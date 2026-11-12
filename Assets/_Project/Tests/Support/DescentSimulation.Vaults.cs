using System;
using System.Runtime.CompilerServices;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Tests
{
    // The Quicksilver Vaults' own enemies (docs/50, simulation-only research 2026-11-12): the Mirror Sentry, the Silver
    // Wisp, the Faceless Clerk and the Glass Regent, each a research lever read by this simulation alone and off by default
    // (VaultsEnemiesResearch). No floor of the game changes: the four exist only on a floor 2 that VaultsChain reshapes,
    // and every behaviour below keys on one of their definitions, so a run that never meets them is the run it was.
    internal static partial class DescentSimulation
    {
        // docs/50's starting values, authored (before the climb's floor-2 scaling).
        public static readonly Enemy SilverWisp = new Enemy { Name = "Silver Wisp", Health = 24f, Damage = 1f, Interval = .5f, Gold = 3, Experience = 10, Speed = 2.4f, Reach = 1.4f };
        // No melee strike (its weapon never ticks): its reach is the 2.8 it stands off at.
        public static readonly Enemy FacelessClerk = new Enemy { Name = "Faceless Clerk", Health = 70f, Damage = .01f, Interval = 1f, Gold = 8, Experience = 15, Speed = 1.2f, Reach = 2.8f };
        public static readonly Enemy MirrorSentry = new Enemy { Name = "Mirror Sentry", Health = 160f, Damage = 5.4f, Interval = 1.4f, InitialDelay = .6f, Gold = 20, Experience = 10, Speed = 1.1f, Reach = 1.6f };
        public static readonly Enemy GlassRegent = new Enemy { Name = "Glass Regent", Health = 300f, Damage = 5f, Interval = 1.8f, InitialDelay = 1f, EnrageAt = .5f, Gold = 50, Experience = 10, Speed = 1f, Reach = 1.7f };
        // The Regent's phase-2 copies: no strike, no reward; their health is a share of the Regent's maximum.
        public static readonly Enemy GlassCopy = new Enemy { Name = "Glass copy", Health = 1f, Damage = .01f, Interval = 1f, Gold = 0, Experience = 0, Speed = 1f, Reach = 1.7f };

        // The research habits docs/50 asks of the pilot, outside AutopilotCohort.Habit.All as StepsInOnTheGateBoss was: no
        // cohort has one unless the levers give it. Flanks extends the step-in (a cohort that StepsIn); Sidesteps extends
        // the escape from the Warden's mark (a cohort that EscapesAwayFromTheWarden).
        [Flags]
        internal enum VaultsHabit
        {
            None = 0,
            // Against the Mirror Sentry, its step-in orbits the Sentry towards its rear at the step-in reach.
            Flanks = 1 << 0,
            // Against the Glass Regent's lanes, the nearest spot clear of every lane rather than out of a circle.
            Sidesteps = 1 << 1
        }

        // Floor 1 as shipped above a floor 2 with the levers' creatures in place of the foundry's: the Sentry for Sentry
        // Hall's Captain, the Wisp for the Runner (the horde's third role and Mercury Stair's second wave), a Clerk at the
        // head of Mercury Stair's first wave and in place of a Grunt in Vault Gate's, the Regent for the Warden.
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
                        if (levers.VaultsSentry && wave[i] == Captain)
                            wave[i] = MirrorSentry;
                        if (levers.VaultsRegent && wave[i] == Warden)
                            wave[i] = GlassRegent;
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
            if (levers.VaultsWisps)
                shaped.HordeRoles = new[] { Mite, Grunt, SilverWisp };
            one.Next = shaped;
            return one;
        }

        internal sealed partial class FoundryLevers
        {
            // docs/50's four on floor 2, each off by default (VaultsChain reads them; the creatures' rules key on their
            // definitions).
            public bool VaultsSentry;
            public bool VaultsWisps;
            public bool VaultsClerks;
            public bool VaultsRegent;
            public VaultsHabit VaultsHabits;
            // The Mirror Sentry's guard (docs/50 section 5).
            public float SentryTurnDegrees = 90f;
            public float SentryArcDegrees = 120f;
            public float SentryFrontShare = .35f;
            public float SentryReflectShare = .2f;
            public float SentryReflectCap = .04f;
            // The Silver Wisp's blink (section 3): once, at an age of WispBlinkAge or more and WispBlinkMin-Max from the
            // hero, to WispLandDistance past the hero on the far side, a shimmer later; no strike for the hold after.
            public float WispBlinkAge = 1.5f;
            public float WispBlinkMin = 3f;
            public float WispBlinkMax = 7f;
            public float WispLandDistance = 3f;
            public float WispShimmerSeconds = .6f;
            public float WispArrivalHoldSeconds = .5f;
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
            // The Glass Regent (section 6) on the Warden's clock (WardenSlamClock as it is: its enraged beat, 4 s, stands
            // for docs/50's 4.5 s): a lane RegentLaneWidth wide and RegentLaneLength long from each body through the
            // hero's spot, RegentLaneShare of its maximum once a beat; RegentCopies copies at half health, each
            // RegentCopyShare of its maximum. Its health share is FoundryLevers.LaterWardenHealthShare.
            public float RegentLaneWidth = 1.2f;
            public float RegentLaneLength = 10f;
            public float RegentLaneShare = .18f;
            public int RegentCopies = 2;
            public float RegentCopyShare = .06f;

            public bool Vaults => VaultsSentry || VaultsWisps || VaultsClerks || VaultsRegent;
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
            public float DrawnSeconds;
            public int RegentBeats;
            public int RegentLaneHits;
            public int RegentCopiesShattered;
            public float RegentPhaseTwoAt = -1f;
        }

        // The arithmetic both a scene and this simulation would run, each in one never-inlined body (docs/50 rule 1).
        internal static class VaultsRule
        {
            // A lane from (ax, ay) through (mx, my), length long and halfWidth either side: whether (px, py) stands in it.
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static bool InLane(float px, float py, float ax, float ay, float mx, float my, float length, float halfWidth)
            {
                float dx = mx - ax, dy = my - ay;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d > 1e-5f)
                {
                    dx /= d;
                    dy /= d;
                }
                else
                {
                    dx = 0f;
                    dy = -1f;
                }
                float along = (px - ax) * dx + (py - ay) * dy;
                if (along < 0f)
                    along = 0f;
                else if (along > length)
                    along = length;
                float cx = ax + dx * along - px, cy = ay + dy * along - py;
                return cx * cx + cy * cy <= halfWidth * halfWidth;
            }

            // The Sentry's facing turned towards (tx, ty) from (x, y) by at most maxTurn radians, in (-pi, pi].
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static float Turn(float facing, float x, float y, float tx, float ty, float maxTurn)
            {
                float want = (float)Math.Atan2(ty - y, tx - x);
                float delta = Wrap(want - facing);
                if (delta > maxTurn)
                    delta = maxTurn;
                else if (delta < -maxTurn)
                    delta = -maxTurn;
                return Wrap(facing + delta);
            }

            // Whether (px, py) stands inside the front arc (arcDegrees in all) of a body at (x, y) facing facing.
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static bool InFront(float facing, float x, float y, float px, float py, float arcDegrees)
            {
                float toward = (float)Math.Atan2(py - y, px - x);
                return Math.Abs(Wrap(toward - facing)) <= arcDegrees * (float)Math.PI / 360f;
            }

            public static float Wrap(float angle)
            {
                const float tau = 2f * (float)Math.PI;
                while (angle > (float)Math.PI)
                    angle -= tau;
                while (angle <= -(float)Math.PI)
                    angle += tau;
                return angle;
            }
        }

        private sealed partial class Foe
        {
            // The Sentry's facing (radians; NaN until first stepped) and its reflection's budget (NaN until first stepped).
            public float Facing = float.NaN;
            public float ReflectBudget = float.NaN;
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
            // The lanes of the Regent's beat standing now: each body's spot as the mark went down.
            private readonly float[] _laneX = new float[3];
            private readonly float[] _laneY = new float[3];
            private int _lanes;
            private readonly Foe[] _glassCopies = new Foe[2];
            // Reflection owed to the hero by a Sentry, paid next frame (a hit on the hero inside the hero's own strike
            // would land mid-attack).
            private float _reflectOwed;
            private Foe _reflectFrom;
            private bool _reflecting;

            private bool VaultsOn => _levers.Vaults && _floor.FloorNumber >= 2;

            private static bool IsRegent(Foe foe) => foe != null && ReferenceEquals(foe.Definition, GlassRegent);

            // Neither the Clerk nor a glass copy strikes in melee: no weapon ticks, and the pilot does not kite from them.
            private static bool Harmless(Foe foe) =>
                ReferenceEquals(foe.Definition, FacelessClerk) || ReferenceEquals(foe.Definition, GlassCopy);

            // Where the packs walk: to the newest standing Clerk copy (ClerkDrawsPacks), else to the hero.
            private float PackGoalX => DrawingCopy(out HazardMark copy) ? copy.X : HeroX;
            private float PackGoalY => DrawingCopy(out HazardMark copy) ? copy.Y : HeroY;

            private bool DrawingCopy(out HazardMark copy)
            {
                copy = null;
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

            // The four's clocks, stepped where the Warden's slam is (docs/50 rule 4), held while a choice is open or the
            // reveal holds. Nothing on a floor without them.
            private void StepVaults()
            {
                if (!VaultsOn || _run.HasEnded)
                    return;
                if (_reflectOwed > 0f && _heroHealth.IsAlive)
                {
                    float owed = _reflectOwed;
                    _reflectOwed = 0f;
                    _reflecting = true;
                    _harmIsSlam = false;
                    _hero.ApplyDamage(new DamageContext(owed, _reflectFrom));
                    _reflecting = false;
                }
                if (_choices.IsOpen || _revealHold)
                    return;
                if (DrawingCopy(out _))
                    _result.DrawnSeconds += Step;
                // The Regent fallen: its copies shatter with it.
                if (_glassCopies[0] != null && (GateBoss == null || !GateBoss.IsAlive || !IsRegent(GateBoss)))
                    ShatterCopies();
                int copies = 0;
                for (int i = 0; i < _marks.Count; i++)
                    if (_marks[i].Harm == ClerkHarm)
                        copies++;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (!IsValid(foe) || foe.SpawnFrame == _frame)
                        continue;
                    Enemy kind = foe.Definition;
                    if (ReferenceEquals(kind, MirrorSentry))
                        StepSentry(foe);
                    else if (ReferenceEquals(kind, SilverWisp))
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
                            Mark(HeroX, HeroY, _levers.ClerkRadius, _levers.ClerkTelegraph, _levers.ClerkShare, ClerkHarm, HeroX, HeroY);
                        }
                    }
                }
            }

            private void StepSentry(Foe foe)
            {
                float cap = _heroHealth.Maximum * _levers.SentryReflectCap;
                foe.ReflectBudget = float.IsNaN(foe.ReflectBudget) ? cap : Math.Min(cap, foe.ReflectBudget + cap * Step);
                if (float.IsNaN(foe.Facing))
                    foe.Facing = (float)Math.Atan2(HeroY - foe.Y, HeroX - foe.X);
                else
                    foe.Facing = VaultsRule.Turn(foe.Facing, foe.X, foe.Y, HeroX, HeroY,
                        _levers.SentryTurnDegrees * (float)Math.PI / 180f * Step);
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
                float dx = HeroX - foe.X, dy = HeroY - foe.Y;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d < _levers.WispBlinkMin || d > _levers.WispBlinkMax)
                    return;
                foe.Blinked = true;
                float lx = HeroX + dx / d * _levers.WispLandDistance;
                float ly = HeroY + dy / d * _levers.WispLandDistance;
                if (!FoundryMap.Contains(lx, ly, HeroMotion.EdgeMargin))
                    return;
                foe.BlinkX = lx;
                foe.BlinkY = ly;
                foe.BlinkAt = _seconds + _levers.WispShimmerSeconds;
                foe.StaggeredUntil = Math.Max(foe.StaggeredUntil, foe.BlinkAt + _levers.WispArrivalHoldSeconds);
            }

            // FoeDamaged's hook: the Sentry's guard on a landing of the hero's while the hero stands in its front arc,
            // and the reflection it owes (capped by its budget). The landing as it was anywhere else.
            private DamageContext SentryGuard(Foe foe, DamageContext context, DamageSource source)
            {
                if (!ReferenceEquals(foe.Definition, MirrorSentry) || float.IsNaN(foe.Facing) ||
                    (source != DamageSource.Weapon && source != DamageSource.Ability))
                    return context;
                bool front = VaultsRule.InFront(foe.Facing, foe.X, foe.Y, HeroX, HeroY, _levers.SentryArcDegrees);
                if (source == DamageSource.Weapon)
                {
                    _result.SentryLandings++;
                    if (front)
                        _result.SentryFrontLandings++;
                }
                if (!front)
                    return context;
                float want = context.Amount * _levers.SentryReflectShare;
                float paid = Math.Min(want, Math.Max(0f, foe.ReflectBudget));
                if (paid > 0f)
                {
                    foe.ReflectBudget -= paid;
                    _reflectOwed += paid;
                    _reflectFrom = foe;
                }
                return new DamageContext(context.Amount * _levers.SentryFrontShare, context.Source, context.IsCritical);
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

            private void RegentMarked(Foe boss)
            {
                _result.RegentBeats++;
                _lanes = 0;
                _laneX[_lanes] = boss.X;
                _laneY[_lanes++] = boss.Y;
                for (int c = 0; c < _glassCopies.Length; c++)
                {
                    Foe copy = _glassCopies[c];
                    if (copy == null || !IsValid(copy))
                        continue;
                    _laneX[_lanes] = copy.X;
                    _laneY[_lanes++] = copy.Y;
                }
            }

            private bool InLanes(float x, float y, float margin)
            {
                float half = _levers.RegentLaneWidth * .5f + margin;
                for (int i = 0; i < _lanes; i++)
                    if (VaultsRule.InLane(x, y, _laneX[i], _laneY[i], _markX, _markY, _levers.RegentLaneLength, half))
                        return true;
                return false;
            }

            private bool RegentLanesHit()
            {
                bool hit = _heroHealth.IsAlive && InLanes(HeroX, HeroY, 0f);
                if (hit)
                    _result.RegentLaneHits++;
                return hit;
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

            // The sidestep habit: a cohort that leaves the Warden's mark away from it, given the habit, against lanes.
            private bool SidestepsLanes(AutopilotCohort cohort) =>
                _lanes > 0 && IsRegent(_slamBoss) && (_levers.VaultsHabits & VaultsHabit.Sidesteps) != 0 &&
                cohort.Has(AutopilotCohort.Habit.EscapesAwayFromTheWarden);

            private static readonly float[] SidestepRings = { .5f, .8f, 1.1f, 1.5f, 2f, 2.6f, 3.2f };
            private const int SidestepBearings = 24;
            private const float SidestepMargin = .35f;

            // The nearest spot on the floor clear of every lane by a margin (the first ring with one; on it the one
            // farthest from the lanes' aim); false when the hero already stands clear.
            private bool SidestepGoal(float x, float y, out float gx, out float gy)
            {
                gx = x;
                gy = y;
                if (!InLanes(x, y, SidestepMargin * .5f))
                    return false;
                for (int r = 0; r < SidestepRings.Length; r++)
                {
                    float best = -1f;
                    for (int b = 0; b < SidestepBearings; b++)
                    {
                        double angle = b * 2.0 * Math.PI / SidestepBearings;
                        float cx = x + SidestepRings[r] * (float)Math.Cos(angle);
                        float cy = y + SidestepRings[r] * (float)Math.Sin(angle);
                        if (InLanes(cx, cy, SidestepMargin) || !FoundryMap.Contains(cx, cy, HeroMotion.EdgeMargin))
                            continue;
                        float away = ArenaFloor.FloorDistanceSquared(cx, cy, _markX, _markY);
                        if (away <= best)
                            continue;
                        best = away;
                        gx = cx;
                        gy = cy;
                    }
                    if (best >= 0f)
                        return true;
                }
                return false;
            }

            // The flank habit: against the Sentry, the step-in's spot turned round the Sentry towards its rear, at most
            // 40 degrees a frame's goal, at the same reach.
            private void Flank(Foe target, float x, float y, ref float gx, ref float gy)
            {
                if ((_levers.VaultsHabits & VaultsHabit.Flanks) == 0 || !ReferenceEquals(target.Definition, MirrorSentry) ||
                    float.IsNaN(target.Facing))
                    return;
                float r = (float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(gx, gy, target.X, target.Y));
                float at = (float)Math.Atan2(y - target.Y, x - target.X);
                float rear = VaultsRule.Wrap(target.Facing + (float)Math.PI);
                float delta = VaultsRule.Wrap(rear - at);
                const float stepMax = 40f * (float)Math.PI / 180f;
                if (delta > stepMax)
                    delta = stepMax;
                else if (delta < -stepMax)
                    delta = -stepMax;
                float fx = target.X + r * (float)Math.Cos(at + delta);
                float fy = target.Y + r * (float)Math.Sin(at + delta);
                if (!FoundryMap.Contains(fx, fy, HeroMotion.EdgeMargin))
                    return;
                gx = fx;
                gy = fy;
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
