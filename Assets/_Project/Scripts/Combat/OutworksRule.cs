using System;
using System.Runtime.CompilerServices;
using Cryptforge.Art;

namespace Cryptforge.Combat
{
    // docs/62's P0a, the Outworks mechanics probe (2026-11-22, behind development/outworks.txt; off by default): before
    // floor 1's halls, the foundry court plays a short open phase - three Tithe Anvils to capture, one roaming Keeper that
    // carries the Stair Key, two pad pairs that leap the lava pit, and a clock that rings the Bell, after which the swarm
    // rises and the hero holds the Ascent Stair to go on into the halls. The owner's answers (docs/62 section 11): the
    // world comes in addition to the halls, jumping is automatic, and the climb stays 13-16 minutes.
    //
    // Everything here is pure and shared, so the scene (EncounterController, HeroMovementInput, the views) and the foundry
    // simulation (DescentSimulation.Outworks.cs) play one phase: the sites, the anvils' charge, the Keeper's patrol and
    // aggro, the pad's arc, the Bell and the stair. Distances go through one body that is never inlined (Within), squared
    // radii cast to float, for the reason HeroDash and the stamina lesson give (docs/21): Mono and .NET part in the last bit.
    // Every number is a P0a starting value, measured in the simulation (docs/62 section 12), not a tuned one.
    public static class OutworksRule
    {
        // ---- The anvils (docs/62 section 2.3) -------------------------------------------------------------------------

        // A Tithe Anvil charges while the hero stands inside its ring - moving inside it is allowed, so a hero can kite in
        // circles - and is captured after ChargeSeconds of it. Outside the ring its charge drains at DecayShare of the
        // fill rate (gentler than Megabonk's quick reset, because a thumb slips). A capture grants one level-up's choice of
        // cards (RunState.GrantBonusUpgrade, the offer an altar or the forge's Temper opens).
        public const float AnvilRadius = 2.2f;
        public const float AnvilChargeSeconds = 5f;
        public const float AnvilDecayShare = .5f;

        // Where the three anvils stand, in the court's design coordinates (FoundryMap: u east along the court, v north):
        // the workyard's south, the workyard's north and the east court's south - two across the pit from where the run
        // begins, so the pads matter, and none on a hall's district, an altar or a work island.
        private static readonly float[,] AnvilSites = { { -27f, -9f }, { -26f, 14.5f }, { 10f, -13f } };
        public static int AnvilCount => AnvilSites.GetLength(0);

        public static void AnvilSite(int anvil, out float x, out float y) =>
            FoundryMap.ToFloor(AnvilSites[anvil, 0], AnvilSites[anvil, 1], out x, out y);

        // ---- The Keeper (docs/62 section 2.4) ---------------------------------------------------------------------------

        // Measured (docs/62 section 12, seeds 1-200): at 0:45 the careful pilot killed it by 1:05 and left the phase at 1:13;
        // at 1:00 every playing cohort's phase lands in 1:30-1:52 with the same level at the Warden and no more deaths.
        // The Keeper walks onto the court this long into the phase: the floor's first elite room's first wave - its Grunt
        // Captain at KeeperHealthShare of its health, with the Captain's escort - at the waypoint of its loop farthest from
        // the hero, and patrols the loop at KeeperPatrolShare of its walking speed. It turns on the hero once the hero
        // comes within KeeperAggroRadius or strikes any of its pack, and then follows as any wave does. Its Captain's fall
        // drops the Stair Key, which opens the stair before the Bell. A Keeper still roaming when the Bell rings leaves
        // the court: the key's chance has passed.
        public const float KeeperSpawnSeconds = 60f;
        public const float KeeperAggroRadius = 7f;
        public const float KeeperPatrolShare = .7f;
        public const float KeeperHealthShare = 12f;
        // What the Captain pays as the Keeper, times its authored experience and gold.
        public const float KeeperExperienceShare = 3f;
        public const float KeeperGoldShare = 2f;
        // A waypoint counts as reached within this of it (the Captain stops at its reach, 1.6, short of a goal).
        public const float KeeperWaypointReach = 2.2f;

        // The Keeper's Captain's pay, from its authored experience and its floor-scaled gold. One body, never inlined.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int KeeperExperience(int authored, float share) => (int)(authored * share + .5f);

        public static int KeeperGold(int scaledGold) => scaledGold * (int)KeeperGoldShare;

        // The Keeper's loop round the pit through the north court and the workyard, clear of the islands and braziers.
        private static readonly float[,] KeeperWaypoints =
        {
            { 4f, 14f }, { -10f, 16.5f }, { -24f, 8f }, { -21f, -6f }, { -10f, -6f }, { 4f, 2f }
        };
        public static int KeeperWaypointCount => KeeperWaypoints.GetLength(0);

        public static void KeeperWaypoint(int waypoint, out float x, out float y) =>
            FoundryMap.ToFloor(KeeperWaypoints[waypoint, 0], KeeperWaypoints[waypoint, 1], out x, out y);

        // The waypoint the Keeper walks on at: the loop's farthest from the hero, the earlier on a tie.
        public static int KeeperStart(float heroX, float heroY)
        {
            int best = 0;
            float bestDistance = -1f;
            for (int i = 0; i < KeeperWaypointCount; i++)
            {
                KeeperWaypoint(i, out float x, out float y);
                float distance = DistanceSquared(heroX, heroY, x, y);
                if (distance > bestDistance)
                {
                    best = i;
                    bestDistance = distance;
                }
            }
            return best;
        }

        // ---- The Bell and the Ascent Stair (docs/62 sections 1.2 and 2.6) ---------------------------------------------

        // The phase's clock rings the Bell here (active seconds, frozen through choices as the floor's clock is; measured
        // against 1:30 and 2:00 in docs/62 section 12): the
        // swarm rises (SwarmDirector over BellLadder) and the stair opens for everyone. The stair is held as the opened
        // gate is (ExitRule's hold at the gate's radius: standing still on it for ExitRule.DwellSeconds).
        public const float BellSeconds = 105f;
        public const float StairRadius = ExitRule.GateRadius;
        // The stair stands at the court's north-east, near the screen's top (the tall world's rule), clear of the halls.
        private const float StairU = 10f, StairV = 12f;

        public static void StairSpot(out float x, out float y) => FoundryMap.ToFloor(StairU, StairV, out x, out y);

        // The Bell's swarm: a ladder of the floor's horde roles by index in the horde table (0 the lightest: floor 1's
        // Cinder Mite, Grunt, Runner), one wave every SwarmRule.LadderSeconds, the last repeating - SwarmDirector's own
        // rules. Its enemies are fodder as the horde's are (HordeRule's health, damage and experience shares, the floor's
        // clock grown, no gold), hardened by the swarm's ramp (SwarmRule.DifficultyAtSeconds).
        private static readonly int[][] BellLadder =
        {
            new[] { 0, 0, 1, 0 },
            new[] { 0, 1, 0, 2, 0, 1 },
            new[] { 0, 1, 2, 0, 1, 0, 2, 1 }
        };
        public static int BellWaveCount => BellLadder.Length;
        public static int BellWaveSize(int wave) => BellLadder[wave].Length;

        // The horde role of a Bell wave's place, within a table of roleCount roles.
        public static int BellRole(int wave, int index, int roleCount)
        {
            if (roleCount < 1)
                throw new ArgumentOutOfRangeException(nameof(roleCount));
            int role = BellLadder[wave][index % BellLadder[wave].Length];
            return role < roleCount ? role : roleCount - 1;
        }

        public static int[] BellSizes()
        {
            var sizes = new int[BellLadder.Length];
            for (int i = 0; i < sizes.Length; i++)
                sizes[i] = BellLadder[i].Length;
            return sizes;
        }

        // ---- The horde in the Outworks (docs/62 section 4.2) ------------------------------------------------------------

        // No halls are cleared yet, so the horde's state follows the phase's clock: a thin opening, the crowd, then the
        // pressure that holds to the Bell and through it. The targets and the floor clock's growth are HordeRule's and
        // HordeClock's; only the timings are new.
        public const float HordeFrom = 20f;
        public const float PressureFrom = 60f;

        // The parity lever (docs/62 section 4.2: "P0 tunes one lever"): while the phase runs a horde enemy - the Bell's swarm
        // too - pays this share of its authored experience instead of HordeRule.ExperienceShare, so the hero meets the
        // Warden at today's level: the phase's thin opening and its Keeper farm less than the gate's wait it replaces.
        public const float HordeExperienceShare = .4f;

        // A horde enemy's experience while the phase runs, from its authored reward: HordeExperienceShare of it, rounded,
        // at least one (HordeRule.Experience's shape). One body, never inlined.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int HordeExperience(int authored)
        {
            if (authored < 0)
                throw new ArgumentOutOfRangeException(nameof(authored));
            return authored == 0 ? 0 : Math.Max(1, (int)(authored * HordeExperienceShare + 0.5f));
        }

        public static HordeState HordeStateAt(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            return seconds < HordeFrom ? HordeState.Ramp : seconds < PressureFrom ? HordeState.Horde : HordeState.Pressure;
        }

        // ---- The halls after it (docs/62 section 11: the climb stays 13-16 minutes) ------------------------------------

        // With the Outworks on, floor 1's halls keep their authored rooms and play on the same floor clock the phase
        // started. Measured (docs/62 section 12, seeds 1-200): the phase comes out of the gate's wait - the Warden still
        // walks out at the clock's 3:30, floor 1 stays 4:05-4:35 and the climb 13:20-14:35 - so the halls need no trim,
        // and a trim (one surge a hall, no extra elite) only cost a level at the Warden. These knobs, read instead of
        // Floor_EmberHalls' own only while the switch is on (EncounterController.HallSurgesOn, ExtraElitesOn,
        // GateNotBeforeOn), stand at the asset's values for the S23's A/B to move if the owner's floor runs long.
        public const int TrimmedHallSurges = 2;
        public const int TrimmedExtraElites = 1;
        public const float TrimmedGateSeconds = 210f;

        // ---- The pads (docs/62 section 2.2) -----------------------------------------------------------------------------

        // A jump pad launches the hero, whenever its feet come within PadRadius of the pad, along a fixed arc to a fixed
        // landing in PadSeconds: the sprite rises up to PadHeight above the ground track, which the shadow keeps, and the
        // hero can neither be struck nor walk mid-arc. Enemies never use a pad. Each landing stands outside every pad's
        // circle, so no landing launches again; and a pad launches only once the hero has stood off it since its last.
        public const float PadRadius = .7f;
        public const float PadSeconds = .9f;
        public const float PadHeight = 1.2f;

        // Two pairs across the pit (design u -16..-4, v 2..14): west to east and back at v 8, south to north and back at
        // u -10; each launch 1.4 units off the pit's edge, each landing 1.8 beyond the other launch.
        private static readonly float[,] PadSites =
        {
            { -17.4f, 8f, -.8f, 8f }, { -2.6f, 8f, -19.2f, 8f },
            { -10f, .6f, -10f, 17.2f }, { -10f, 15.4f, -10f, -1.2f }
        };
        public static int PadCount => PadSites.GetLength(0);

        public static void PadLaunch(int pad, out float x, out float y) =>
            FoundryMap.ToFloor(PadSites[pad, 0], PadSites[pad, 1], out x, out y);

        public static void PadLanding(int pad, out float x, out float y) =>
            FoundryMap.ToFloor(PadSites[pad, 2], PadSites[pad, 3], out x, out y);

        // ---- Edge arrows (docs/62 section 2.6) --------------------------------------------------------------------------

        // At most this many arrows at once, stair first, then the Keeper, then the anvils nearest first.
        public const int MaxArrows = 4;

        // ---- The shared arithmetic --------------------------------------------------------------------------------------

        // Whether (x, y) stands within radius of (cx, cy). One body, never inlined; the square of the radius rounded to
        // float before the comparison, so Mono's wider intermediates cannot part from .NET's.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Within(float x, float y, float cx, float cy, float radius)
        {
            float dx = x - cx;
            float dy = y - cy;
            float d2 = dx * dx + dy * dy;
            return d2 <= (float)(radius * radius);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float DistanceSquared(float x, float y, float tx, float ty)
        {
            float dx = tx - x;
            float dy = ty - y;
            return dx * dx + dy * dy;
        }

        // Where the arc's ground track stands at progress (0 at the launch, 1 at the landing): a straight line, the
        // shadow's. One body, never inlined.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ArcPoint(float fromX, float fromY, float toX, float toY, float progress, out float x,
            out float y)
        {
            float t = progress < 0f ? 0f : progress > 1f ? 1f : progress;
            x = fromX + (toX - fromX) * t;
            y = fromY + (toY - fromY) * t;
        }

        // How far above its ground track the hero's sprite stands at progress: a parabola peaking at PadHeight halfway.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float ArcLift(float progress)
        {
            float t = progress < 0f ? 0f : progress > 1f ? 1f : progress;
            return 4f * PadHeight * t * (1f - t);
        }

        // A walk's length from (x, y) to (gx, gy) on the court, estimated round the pit: straight where the way is clear,
        // else by the best one or two of the pit's corners (with the walk's clearance). For a pilot's choice of a pad, not
        // for movement. One body, never inlined.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float WalkEstimate(float x, float y, float gx, float gy)
        {
            const float clearance = .65f;
            if (FoundryMap.Segment(x, y, gx, gy, clearance))
                return Length(gx - x, gy - y);
            float best = float.PositiveInfinity;
            for (int a = 0; a < PitCorners; a++)
            {
                PitCorner(a, out float ax, out float ay);
                bool fromHere = FoundryMap.Segment(x, y, ax, ay, clearance);
                if (fromHere && FoundryMap.Segment(ax, ay, gx, gy, clearance))
                    best = Math.Min(best, Length(ax - x, ay - y) + Length(gx - ax, gy - ay));
                if (!fromHere)
                    continue;
                for (int b = 0; b < PitCorners; b++)
                {
                    if (b == a)
                        continue;
                    PitCorner(b, out float bx, out float by);
                    if (FoundryMap.Segment(ax, ay, bx, by, clearance) && FoundryMap.Segment(bx, by, gx, gy, clearance))
                        best = Math.Min(best, Length(ax - x, ay - y) + Length(bx - ax, by - ay) + Length(gx - bx, gy - by));
                }
            }
            return float.IsPositiveInfinity(best) ? Length(gx - x, gy - y) * 1.5f : best;
        }

        // The pad whose leap shortens the walk to (gx, gy) the most - walking to its launch, then from its landing - by
        // at least PadSaving units; -1 when none does.
        public const float PadSaving = 3f;

        public static int PadShortcut(float x, float y, float gx, float gy)
        {
            float walk = WalkEstimate(x, y, gx, gy);
            int best = -1;
            float bestRoute = walk - PadSaving;
            for (int pad = 0; pad < PadCount; pad++)
            {
                PadLaunch(pad, out float lx, out float ly);
                PadLanding(pad, out float tx, out float ty);
                float route = WalkEstimate(x, y, lx, ly) + WalkEstimate(tx, ty, gx, gy);
                if (route < bestRoute)
                {
                    best = pad;
                    bestRoute = route;
                }
            }
            return best;
        }

        private const int PitCorners = 4;

        // The pit's corners, a walk's clearance and a little more outside it, in floor coordinates.
        private static void PitCorner(int corner, out float x, out float y)
        {
            const float relief = 1.2f;
            float u = (corner & 1) == 0 ? FoundryMap.PitWest - relief : FoundryMap.PitEast + relief;
            float v = (corner & 2) == 0 ? FoundryMap.PitSouth - relief : FoundryMap.PitNorth + relief;
            FoundryMap.ToFloor(u, v, out x, out y);
        }

        private static float Length(float x, float y) => (float)Math.Sqrt(x * x + y * y);
    }

    // The three anvils: each one's charge, whether it is captured, and which the hero stands in. Pure.
    public sealed class AnvilField
    {
        private readonly float[] _x;
        private readonly float[] _y;
        private readonly float[] _charge;
        private readonly bool[] _captured;

        public AnvilField()
        {
            int count = OutworksRule.AnvilCount;
            _x = new float[count];
            _y = new float[count];
            _charge = new float[count];
            _captured = new bool[count];
            for (int i = 0; i < count; i++)
                OutworksRule.AnvilSite(i, out _x[i], out _y[i]);
            Standing = -1;
        }

        public int Count => _x.Length;
        public float XOf(int anvil) => _x[anvil];
        public float YOf(int anvil) => _y[anvil];
        public bool IsCaptured(int anvil) => _captured[anvil];
        // Seconds of charge an anvil holds, and as a share of a capture.
        public float ChargeOf(int anvil) => _charge[anvil];
        public float ShareOf(int anvil) => _charge[anvil] / OutworksRule.AnvilChargeSeconds;
        // The uncaptured anvil whose ring the hero stands in, or -1.
        public int Standing { get; private set; }
        public int Captured { get; private set; }

        // One frame of deltaTime with the hero's feet at (heroX, heroY): the ring it stands in charges, every other
        // uncaptured anvil drains at OutworksRule.AnvilDecayShare. The anvil captured this frame, or -1.
        public int Step(float heroX, float heroY, float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            int on = -1;
            for (int i = 0; i < _x.Length; i++)
            {
                if (!_captured[i] && OutworksRule.Within(heroX, heroY, _x[i], _y[i], OutworksRule.AnvilRadius))
                {
                    on = i;
                    break;
                }
            }
            Standing = on;
            int captured = -1;
            for (int i = 0; i < _x.Length; i++)
            {
                if (_captured[i])
                    continue;
                if (i == on)
                {
                    _charge[i] += deltaTime;
                    if (_charge[i] >= OutworksRule.AnvilChargeSeconds)
                    {
                        _charge[i] = OutworksRule.AnvilChargeSeconds;
                        _captured[i] = true;
                        Captured++;
                        Standing = -1;
                        captured = i;
                    }
                }
                else if (_charge[i] > 0f)
                {
                    _charge[i] -= deltaTime * OutworksRule.AnvilDecayShare;
                    if (_charge[i] < 0f)
                        _charge[i] = 0f;
                }
            }
            return captured;
        }
    }

    public enum KeeperState
    {
        // Not yet on the court.
        Waiting = 0,
        // On its loop, not yet turned on the hero.
        Patrolling = 1,
        // Turned on the hero: it follows as a wave does.
        Hunting = 2,
        // Its Captain has fallen: the key is the hero's.
        Fallen = 3,
        // The Bell rang while it stood: it left the court.
        Withdrawn = 4
    }

    // The Outworks phase as one state: its clock, the anvils, the Keeper's state and loop, the key, the Bell and the
    // stair's hold. The scene's EncounterController and the simulation step it the same way, frame by frame. Pure.
    public sealed class OutworksPhase
    {
        private readonly ExitDwell _stair = new ExitDwell();
        private int _waypoint;

        public OutworksPhase() : this(OutworksRule.BellSeconds, OutworksRule.KeeperSpawnSeconds)
        {
        }

        // A phase whose Bell rings at bellSeconds and whose Keeper walks on at keeperSeconds (the simulation's research
        // levers; the scene plays OutworksRule's).
        public OutworksPhase(float bellSeconds, float keeperSeconds = OutworksRule.KeeperSpawnSeconds)
        {
            if (!(bellSeconds > 0f) || float.IsInfinity(bellSeconds))
                throw new ArgumentOutOfRangeException(nameof(bellSeconds));
            if (!(keeperSeconds >= 0f) || float.IsInfinity(keeperSeconds))
                throw new ArgumentOutOfRangeException(nameof(keeperSeconds));
            BellSeconds = bellSeconds;
            KeeperSeconds = keeperSeconds;
            Anvils = new AnvilField();
            OutworksRule.StairSpot(out float x, out float y);
            StairX = x;
            StairY = y;
        }

        public float Seconds { get; private set; }
        public float BellSeconds { get; }
        public float KeeperSeconds { get; }
        public AnvilField Anvils { get; }
        public KeeperState Keeper { get; private set; }
        public bool KeyHeld { get; private set; }
        public bool BellRung { get; private set; }
        // The hero held the stair: the phase is over and the halls go on.
        public bool Left => _stair.Left;
        public float StairX { get; }
        public float StairY { get; }
        public float StairHeld => _stair.Held;
        // The stair opens with the key, or for everyone at the Bell.
        public bool StairOpen => KeyHeld || BellRung;
        public int Waypoint => _waypoint;
        // When the key was taken and the Bell rang, on the phase's clock; -1 for neither yet.
        public float KeyAt { get; private set; } = -1f;
        public float BellAt { get; private set; } = -1f;

        // One frame of the phase's clock. True on the frame the Keeper is due to walk on, which the caller answers with
        // KeeperArrived once it stands.
        public bool KeeperDue => Keeper == KeeperState.Waiting && Seconds >= KeeperSeconds && !BellRung;

        // The clock moves on; true when this frame rang the Bell.
        public bool Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            Seconds += deltaTime;
            if (BellRung || Left || Seconds < BellSeconds)
                return false;
            BellRung = true;
            BellAt = Seconds;
            if (Keeper == KeeperState.Patrolling || Keeper == KeeperState.Hunting)
                Keeper = KeeperState.Withdrawn;
            return true;
        }

        // The Keeper has walked on at the waypoint start (OutworksRule.KeeperStart) and patrols from there.
        public void KeeperArrived(int start)
        {
            if (Keeper != KeeperState.Waiting)
                throw new InvalidOperationException("The Keeper walks on once.");
            Keeper = KeeperState.Patrolling;
            _waypoint = start;
        }

        // Where the Keeper's pack walks this frame: on the loop while it patrols - the next waypoint once this one is
        // reached - or on the hero once it has turned, which it does when the hero comes within its aggro radius or the
        // pack is struck. True while the Keeper stands (patrolling or hunting).
        public bool KeeperGoal(float keeperX, float keeperY, float heroX, float heroY, bool struck, out float goalX,
            out float goalY)
        {
            goalX = heroX;
            goalY = heroY;
            if (Keeper != KeeperState.Patrolling && Keeper != KeeperState.Hunting)
                return false;
            if (Keeper == KeeperState.Patrolling &&
                (struck || OutworksRule.Within(heroX, heroY, keeperX, keeperY, OutworksRule.KeeperAggroRadius)))
                Keeper = KeeperState.Hunting;
            if (Keeper == KeeperState.Hunting)
                return true;
            OutworksRule.KeeperWaypoint(_waypoint, out goalX, out goalY);
            if (OutworksRule.Within(keeperX, keeperY, goalX, goalY, OutworksRule.KeeperWaypointReach))
            {
                _waypoint = (_waypoint + 1) % OutworksRule.KeeperWaypointCount;
                OutworksRule.KeeperWaypoint(_waypoint, out goalX, out goalY);
            }
            return true;
        }

        // The Keeper's Captain fell: the Stair Key is the hero's, and the stair opens.
        public void KeeperFell()
        {
            if (Keeper != KeeperState.Patrolling && Keeper != KeeperState.Hunting)
                return;
            Keeper = KeeperState.Fallen;
            KeyHeld = true;
            KeyAt = Seconds;
        }

        // The anvils for one frame of the hero standing at (heroX, heroY); the anvil captured, or -1.
        public int StepAnvils(float heroX, float heroY, float deltaTime) =>
            Left ? -1 : Anvils.Step(heroX, heroY, deltaTime);

        // The stair's hold for one frame: it counts only while the stair is open and the hero stood still on it (ExitRule's
        // hold, at the gate's radius). True once held: the phase is over.
        public bool StepStair(float deltaTime, float heroX, float heroY, bool moved) =>
            _stair.Step(deltaTime, StairOpen && !moved && OutworksRule.Within(heroX, heroY, StairX, StairY, OutworksRule.StairRadius));
    }

    // A pad's leap as the walk step carries it (docs/62 section 2.2): Begin at a pad, then each walk step moves the hero
    // along the arc's ground track through HeroMotion.Carry - over the pit, the stick ignored - until it lands. While it
    // travels the hero is airborne: no hit lands on it (the scene's guard, the simulation's HeroDamaged) and nothing it walks
    // into stops it. Plain state, stepped at the walk step (HeroMovementInput's order in the scene, the simulation's frame
    // start); nothing here draws.
    public sealed class HeroLeap
    {
        private const float Slack = 1e-4f;
        private float _fromX;
        private float _fromY;
        private float _toX;
        private float _toY;
        private float _travelled;
        // The pad launched from last, which launches again only once the hero has stood off it.
        private int _spent = -1;

        public bool IsAirborne { get; private set; }
        public int Pad { get; private set; } = -1;
        // Where the leap lands: what a spawn round the hero is placed round while it is mid-arc over the pit.
        public float LandingX => _toX;
        public float LandingY => _toY;
        public int Launches { get; private set; }
        // How far along the arc, 0 to 1, and the sprite's height above its ground track there.
        public float Progress => IsAirborne ? Math.Min(1f, _travelled / OutworksRule.PadSeconds) : 0f;
        public float Lift => IsAirborne ? OutworksRule.ArcLift(Progress) : 0f;

        // Whether the hero at (x, y) launches from a pad now: within its radius, not mid-arc, and not still on the pad it
        // last launched from. True when it did.
        public bool TryLaunch(float x, float y)
        {
            if (IsAirborne)
                return false;
            int on = -1;
            for (int pad = 0; pad < OutworksRule.PadCount; pad++)
            {
                OutworksRule.PadLaunch(pad, out float px, out float py);
                if (OutworksRule.Within(x, y, px, py, OutworksRule.PadRadius))
                {
                    on = pad;
                    break;
                }
            }
            if (on < 0 || on == _spent)
            {
                if (on < 0)
                    _spent = -1;
                return false;
            }
            Pad = on;
            _spent = on;
            OutworksRule.PadLaunch(on, out _fromX, out _fromY);
            OutworksRule.PadLanding(on, out _toX, out _toY);
            _travelled = 0f;
            IsAirborne = true;
            Launches++;
            return true;
        }

        // One walk step of an airborne hero: the arc's ground track at the step's end, carried onto motion. False when not
        // airborne. The step that lands puts the hero on the landing and ends the leap.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Move(HeroMotion motion, float deltaTime)
        {
            if (motion == null)
                throw new ArgumentNullException(nameof(motion));
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!IsAirborne)
                return false;
            _travelled += deltaTime;
            float progress = _travelled / OutworksRule.PadSeconds;
            if (progress >= 1f - Slack)
            {
                motion.Carry(_toX, _toY);
                IsAirborne = false;
                _travelled = 0f;
                return true;
            }
            OutworksRule.ArcPoint(_fromX, _fromY, _toX, _toY, progress, out float x, out float y);
            motion.Carry(x, y);
            return true;
        }

        // Ends a leap at once on its landing (the phase ended under it).
        public void Land(HeroMotion motion)
        {
            if (motion == null)
                throw new ArgumentNullException(nameof(motion));
            if (!IsAirborne)
                return;
            motion.Carry(_toX, _toY);
            IsAirborne = false;
            _travelled = 0f;
        }
    }
}
