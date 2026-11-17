using System.Collections.Generic;
using Cryptforge.Combat;

namespace Cryptforge.Tests
{
    // The Gloam Gardens' own enemies (docs/52). The Cinder Bloom ships (`bloom-m20-h70-r1.2`, the lead's decisions 2026-11-15):
    // it leads floor 3's Thorn Post (DescentSimulation.GloamGardens, CinderBloom) and its bark-and-bloom cycle is BloomRule's,
    // a BloomClock on the foe stepped where the scene's GloamService steps it (after the Warden's slam and the Sentry's
    // guard, before every attacker), held while a choice is open or the reveal holds. Every behaviour below keys on a foe
    // that carries a clock, so floors 1 and 2, which hold no Bloom, are the runs they were. The research levers stay
    // (GloamEnemiesResearch): GloamCaptain puts the Grunt Captain back at Thorn Post (floor 3 before 2026-11-15), and the
    // Bloom's numbers move its tuning. The elite rooms' accounting (FoundryResult.EliteFights, EliteTaken*) is read for
    // every elite leader and changes nothing a run does.
    internal static partial class DescentSimulation
    {
        // docs/52 section 5's starting values (research round 1): 280 health, Bark Maul 10.8 at reach 1.5 (the Grunt's), the
        // rest the shipped Bloom's (CinderBloom). The shipped Bloom is 70 % of that health (ShippedHealthScale).
        public const float ShippedHealthScale = .7f;
        public const float Docs52Health = 280f;
        public const float Docs52MaulDamage = 10.8f;
        public const float Docs52MaulReach = 1.5f;

        // docs/52 section 3's Pollen Wraith with its pollen (research only; the shipped Wraith has none and the simulation
        // plays it as the Mite): the Cinder Mite's body, strike and speed unchanged, its pollen (PollenRule) its one
        // addition, in the Mite's place in floor 3's horde (role 0) and its three halls. Its own object, so the pollen keys on
        // it and never on a Mite.
        public static readonly Enemy PollinatingWraith = new Enemy { Name = "Pollen Wraith", Health = 15f, Damage = .6f, Interval = 1.5f, Gold = 0, Experience = 1, Speed = 2.2f, Reach = 1.5f };
        // docs/52 section 4's Thornling: the Runner's body, strike and speed unchanged (its dive is its one addition,
        // ThornlingRule), in the Runner's place in floor 3's horde (role 2) and Ash Orchard's second wave (research only).
        public static readonly Enemy Thornling = new Enemy { Name = "Thornling", Health = 30f, Damage = 1.2f, Interval = .4f, Gold = 3, Experience = 10, Speed = 3f, Reach = 1.5f };

        // Whether the levers leave the shipped Bloom as it is.
        private static bool ShippedBloom(FoundryLevers levers) =>
            levers.BloomHealthScale == ShippedHealthScale && levers.BloomMaulDamage == CinderBloom.Damage &&
            levers.BloomMaulReach == CinderBloom.Reach && levers.BloomSpeed == CinderBloom.Speed;

        // The Bloom at the levers' health, maul, reach and speed: itself as shipped, else a copy under its name (its rules
        // read the name).
        private static Enemy Bloom(FoundryLevers levers) =>
            ShippedBloom(levers)
                ? CinderBloom
                : new Enemy
                {
                    Name = CinderBloom.Name, Health = Docs52Health * levers.BloomHealthScale,
                    Damage = levers.BloomMaulDamage, Interval = CinderBloom.Interval,
                    InitialDelay = CinderBloom.InitialDelay, Gold = CinderBloom.Gold, Experience = CinderBloom.Experience,
                    Speed = levers.BloomSpeed, Reach = levers.BloomMaulReach
                };

        // Floors 1 and 2 as VaultsChain makes them (the shipped Mirror Sentry on floor 2, or the levers' floor 2) above floor
        // 3 with the levers' Thorn Post: the Grunt Captain back (GloamCaptain), or the Bloom tuned otherwise. Its Grunt and two
        // Cinder Mites, the halls and the Warden's Grove stay. With the levers at the shipped Bloom it is VaultsChain's.
        public static FoundryFloor GloamChain(FoundryLevers levers)
        {
            FoundryFloor one = VaultsChain(levers);
            if (!levers.Gloam)
                return one;
            FoundryFloor two = one.Next;
            FoundryFloor three = two.Next;
            var rooms = new Enemy[three.Rooms.Length][][];
            for (int r = 0; r < rooms.Length; r++)
            {
                if (three.Rooms[r] == null)
                    continue;
                rooms[r] = new Enemy[three.Rooms[r].Length][];
                for (int w = 0; w < rooms[r].Length; w++)
                {
                    Enemy[] wave = (Enemy[])three.Rooms[r][w].Clone();
                    if (three.Kinds[r] == RoomKind.Elite && wave.Length > 0 && wave[0] == CinderBloom)
                        wave[0] = levers.GloamCaptain ? Captain : Bloom(levers);
                    // The Wraiths and the Thornling in the halls only (the lead's decision 4): Thorn Post's and the Grove's
                    // Cinder Mites stay.
                    if (three.Kinds[r] == RoomKind.Combat)
                        for (int i = 0; i < wave.Length; i++)
                        {
                            if (levers.GloamWraiths && wave[i] == Mite)
                                wave[i] = PollinatingWraith;
                            if (levers.GloamThornlings && wave[i] == Runner)
                                wave[i] = Thornling;
                        }
                    rooms[r][w] = wave;
                }
            }
            // Floor 2 is VaultsChain's own copy (Reshaped), so its next floor can be set without touching another chain.
            FoundryFloor shaped = three.Reshaped(rooms, three.Kinds);
            if (levers.GloamWraiths || levers.GloamThornlings)
            {
                var roles = (Enemy[])three.HordeRoles.Clone();
                for (int i = 0; i < roles.Length; i++)
                {
                    if (levers.GloamWraiths && roles[i] == Mite)
                        roles[i] = PollinatingWraith;
                    if (levers.GloamThornlings && roles[i] == Runner)
                        roles[i] = Thornling;
                }
                shaped.HordeRoles = roles;
            }
            two.Next = shaped;
            return one;
        }

        internal sealed partial class FoundryLevers
        {
            // Floor 3's Thorn Post before the Bloom shipped (2026-11-15): the Grunt Captain at its head (GloamChain).
            public bool GloamCaptain;
            // The Cinder Bloom (docs/52 section 5; shipped `bloom-m20-h70-r1.2`): its health against docs/52's 280, its maul's
            // damage and reach, its walk while closed, and its cycle (BloomRule's: closed, open, the share the closed bark
            // lets through). GloamChain reads the first four; the cycle's are read wherever a Bloom stands.
            public float BloomHealthScale = ShippedHealthScale;
            public float BloomMaulDamage = CinderBloom.Damage;
            public float BloomMaulReach = CinderBloom.Reach;
            public float BloomSpeed = CinderBloom.Speed;
            // A windup on its maul (research past docs/52's list; 0, none, is the shipped maul): a swing begins as the ready
            // maul finds the hero in reach and lands BloomWindup seconds later only if the hero still stands in reach; a
            // swing that finds nobody whiffs and the maul waits, ready, for the next. The open bloom drops a swing.
            public float BloomWindup;
            public float BloomClosed = BloomRule.ClosedSeconds;
            public float BloomOpen = BloomRule.OpenSeconds;
            public float BloomBark = BloomRule.BarkShare;

            // docs/52 section 3's Pollen Wraith (research, off by default): in the Mite's place in floor 3's horde and halls
            // (GloamChain), its pollen (PollenRule) at these rates - the rise a second for each Wraith within WraithRadius of
            // a still hero (at most WraithMost), the fall a second while moving, the bloom's share of the hero's maximum
            // health, the smoothed speed under which the hero is still.
            public bool GloamWraiths;
            public float WraithRate = PollenRule.RisePerWraith;
            public int WraithMost = PollenRule.MostWraiths;
            public float WraithFall = PollenRule.FallPerSecond;
            public float WraithShare = PollenRule.BloomShare;
            public float WraithRadius = PollenRule.Radius;
            public float WraithStillSpeed = PollenRule.StillSpeed;
            // The redesign screen (research 2026-11-16, after the specified Wraith was found idle): (a) WraithClouds - a
            // fallen Wraith leaves a pollen cloud of WraithCloudRadius for WraithCloudSeconds where it fell, and the clouds the
            // hero stands in feed the pollen in place of the bodies near (still only, or with WraithCloudWhileMoving whenever
            // it is inside one, falling only outside); (c) WraithSettleAfter - the pollen rises only once the hero has stood
            // still that many seconds without a break.
            public bool WraithClouds;
            public float WraithCloudRadius = 1.2f;
            public float WraithCloudSeconds = 4f;
            public bool WraithCloudWhileMoving;
            public float WraithSettleAfter;
            // docs/52 section 4's Thornling (research, off by default): in the Runner's place in floor 3's horde and Ash
            // Orchard (GloamChain), its dive (ThornlingRule) - walked ThornlingWalk, ThornlingDiveMin-Max from the hero, a
            // ring of ThornlingRadius filling ThornlingTelegraph, ThornlingShare of the hero's maximum health, stuck
            // ThornlingStuck, one ring on the floor and ThornlingGap after the last eruption.
            public bool GloamThornlings;
            public float ThornlingTelegraph = ThornlingRule.Telegraph;
            public float ThornlingRadius = ThornlingRule.Radius;
            public float ThornlingShare = ThornlingRule.Share;
            public float ThornlingGap = ThornlingRule.Gap;
            public float ThornlingWalk = ThornlingRule.WalkSeconds;
            public float ThornlingStuck = ThornlingRule.StuckSeconds;
            public float ThornlingDiveMin = ThornlingRule.DiveMin;
            public float ThornlingDiveMax = ThornlingRule.DiveMax;
            // Research 2026-11-16 (after seeds 1-400 at lead-integrate-2 put the clumsy cohort's cost at the gate): no new ring
            // while the gate boss stands, so the Warden's mark is the fight's one escape.
            public bool ThornlingQuietAtGate;
            // Research diagnostic (2026-11-16): the Thornling comes up where it dove, not at the ring's centre (the ring
            // still hurts there) - what the dive's closing of the gap costs on its own.
            public bool ThornlingEruptsHome;
            // Research diagnostic (2026-11-16): the ring is no mark for the pilots (nobody leaves it, no step-in pauses) - with
            // ThornlingShare 0, the Thornling's body alone (its dive, its time under the ash, its stuck second).
            public bool ThornlingUnseen;
            // The balance rule's A/A probe (research 2026-11-17, docs/56): on entering floor NudgeFloor the realistic pilot's
            // dice give NudgeDraws draws that nothing reads - the game is the shipped one, only the hands' later draws shift,
            // so the runs diverge from that floor on as any change would make them. 0: off.
            public int NudgeFloor;
            public int NudgeDraws = 1;

            // Whether GloamChain reshapes floor 3.
            public bool Gloam => GloamCaptain || !ShippedBloom(this) || GloamWraiths || GloamThornlings;
        }

        // One elite room's leader felled: its kind, from walking in to falling, the seconds it stood within reach of the
        // hero's longest weapon, its maximum health, and - for a Bloom - the open windows begun by its fall and whether it
        // fell open.
        internal readonly struct EliteFight
        {
            public readonly string Kind;
            public readonly float Lifetime;
            public readonly float InReach;
            public readonly float Health;
            public readonly int Windows;
            public readonly bool FellOpen;

            public EliteFight(string kind, float lifetime, float inReach, float health, int windows, bool fellOpen)
            {
                Kind = kind;
                Lifetime = lifetime;
                InReach = inReach;
                Health = health;
                Windows = windows;
                FellOpen = fellOpen;
            }

            // The hero's damage a second on the leader while in reach (docs/52 section 5's D), as the leader took it.
            public float DamageInReach => InReach > 0f ? Health / InReach : float.NaN;
        }

        internal sealed partial class FoundryResult
        {
            // The floor's elite leaders felled (null when none fell), and the health the hero lost while an elite room's wave
            // stood: in all, and from the leader (the Bloom's maul, the Captain's blows), its escort, the horde, the rest.
            public List<EliteFight> EliteFights;
            public float EliteTaken;
            public float EliteTakenFromLeader;
            public float EliteTakenFromEscort;
            public float EliteTakenFromHorde;
            // The Bloom's landings while closed and open, and what the bark took off them.
            public int BloomClosedLandings;
            public int BloomOpenLandings;
            public float BloomBarked;
            // Swings of a maul with a windup that found the hero gone.
            public int BloomWhiffs;
            // docs/52's Pollen Wraith: the blooms (in all, and while a hall's or an elite room's wave stood), the health they
            // took, and - read on every floor 3, Wraiths or not - the seconds the hero stood still within the Wraith's radius
            // of a body in the Wraith's place (a Wraith, or a horde or hall Cinder Mite: the Wraith's opportunity).
            public int PollenBlooms;
            public int PollenBloomsInRooms;
            public float TakenFromPollen;
            public float StillNearSeconds;
            // The redesign screen's clouds: left, and the seconds the hero stood in one (and still in one).
            public int PollenClouds;
            public float InCloudSeconds;
            public float StillInCloudSeconds;
            // Its parts, read alike: the seconds the hero stood still at all, and those it stood within the radius of such a
            // body, still or not.
            public float StillSeconds;
            public float NearSeconds;
            // docs/52's Thornling: the rings put down, the eruptions that caught the hero (and of them, those whose ring
            // overlapped a lapse of the pilot's, a card's included), the health they took, the rings cancelled (their
            // Thornling left the floor), the hero's weapon landings on a stuck Thornling, and the shortest ring that hit
            // (from its start to its eruption: the telegraph holds).
            public int ThornRings;
            public int ThornHits;
            public int ThornHitsInLapse;
            public float TakenFromThorns;
            public int ThornRingsCancelled;
            public int ThornStuckLandings;
            public float ThornShortestHit = float.PositiveInfinity;
        }

        private sealed partial class Foe
        {
            // The Cinder Bloom's cycle (BloomRule): null until its first step - and for every other enemy.
            public BloomClock Bloom;
            // An elite leader's seconds within reach of the hero's longest weapon (FoundryResult.EliteFights).
            public float InReachSeconds;
            // The run second the Bloom's swing began (FoundryLevers.BloomWindup), NaN while none is under way.
            public float SwingSince = float.NaN;
            // The Thornling's dive (ThornlingRule): null until its first step - and for every other enemy; and under the
            // ash (no target: FoundryRun.IsValid, FoeDamaged).
            public ThornlingClock Thorn;
            public bool Burrowed;
        }

        private sealed partial class FoundryRun
        {
            private static bool IsBloom(Enemy kind) => ReferenceEquals(kind, CinderBloom) || kind.Name == CinderBloom.Name;

            // GloamService's step, where the Warden's slam and the Sentry's guard step: the Blooms, then floor 3's pollen and
            // Thornlings (research levers; the pollen's stillness is read on every floor 3 for the report).
            private void StepGloam()
            {
                StepBlooms();
                if (!ReferenceEquals(_floor, _gloamFloor))
                    GloamFloorBegan();
                if (_pollen != null)
                    StepPollen();
                if (_thornField != null)
                    StepThornlings();
            }

            // Every standing Bloom's clock runs (not on the frame it walks in) and roots it while open; while the fight holds
            // (a choice, the reveal), neither.
            private void StepBlooms()
            {
                if (!_eliteWave || _run.HasEnded || _choices.IsOpen || _revealHold)
                    return;
                for (int i = 0; i < _wave.Count; i++)
                {
                    Foe foe = _wave[i];
                    if (!IsValid(foe) || foe.SpawnFrame == _frame || !IsBloom(foe.Definition))
                        continue;
                    foe.Bloom ??= new BloomClock(_levers.BloomClosed, _levers.BloomOpen, _levers.BloomBark);
                    foe.Bloom.Step(Step);
                    foe.Motion.SetSpeed(foe.Slot, foe.Bloom.Speed(foe.Definition.Speed));
                }
            }

            // FoeDamaged's hook, the bark ahead of the Bloom's health: every landing (BloomRule.DamageOn) once its clock runs.
            private DamageContext BloomBark(Foe foe, DamageContext context)
            {
                bool open = foe.Bloom.IsOpen;
                float taken = foe.Bloom.Land(context.Amount);
                if (open)
                    _result.BloomOpenLandings++;
                else
                {
                    _result.BloomClosedLandings++;
                    _result.BloomBarked += context.Amount - taken;
                }
                return open ? context : new DamageContext(taken, context.Source, context.IsCritical);
            }

            // EnemyAttack's hook: the open Bloom holds its maul, as the Warden's weapon is held through its slam - it neither
            // ticks nor strikes.
            private static bool Rooted(Foe foe)
            {
                if (foe.Bloom == null || !foe.Bloom.IsOpen)
                    return false;
                foe.SwingSince = float.NaN;
                return true;
            }

            // The windup (FoundryLevers.BloomWindup), with the ready maul: whether the swing lands now. A swing begins as the
            // hero stands in reach, and when its windup is done it lands on a hero in reach or whiffs.
            private bool BloomSwings(Foe foe, bool inReach)
            {
                if (float.IsNaN(foe.SwingSince))
                {
                    if (inReach)
                        foe.SwingSince = _seconds;
                    return false;
                }
                if (!BloomRule.SwingDone(_seconds - foe.SwingSince, _levers.BloomWindup))
                    return false;
                foe.SwingSince = float.NaN;
                if (!inReach)
                    _result.BloomWhiffs++;
                return inReach;
            }

            // The frame's elite accounting, while an elite room's wave stands with the hero alive: its leader's seconds within
            // reach of the hero's longest weapon (the reveal never holds an elite room).
            private void MeasureElite()
            {
                if (_wave.Count == 0)
                    return;
                Foe leader = _wave[0];
                if (!leader.EliteLeader || leader.Defeated || !leader.IsAlive)
                    return;
                float reach = 0f;
                for (int i = 0; i < _loadout.Count; i++)
                    reach = System.Math.Max(reach, _loadout.At(i).Runtime.Range);
                if (ArenaFloor.FloorDistanceSquared(leader.X, leader.Y, HeroX, HeroY) <= reach * reach)
                    leader.InReachSeconds += Step;
            }

            // HeroDamaged's hook: what the hero lost while an elite room's wave stood, by source.
            private void EliteTaken(DamageContext context, float taken)
            {
                if (!_eliteWave || _waveAlive <= 0)
                    return;
                _result.EliteTaken += taken;
                if (!(context.Source is Foe foe))
                    return;
                if (foe.EliteLeader)
                    _result.EliteTakenFromLeader += taken;
                else if (foe.Elite)
                    _result.EliteTakenFromEscort += taken;
                else if (foe.Horde)
                    _result.EliteTakenFromHorde += taken;
            }

            // docs/52's Pollen Wraith and Thornling (research levers): the floor they were last stepped on, the hero's pollen
            // and smoothed speed (on every floor 3, for the stillness reading; its rise only with Wraiths), where the hero stood
            // at the last step, and the floor's one Thornling ring with its diver and its mark for the pilot.
            private FoundryFloor _gloamFloor;
            private PollenState _pollen;
            private float _pollenX;
            private float _pollenY;
            private ThornlingField _thornField;
            private Foe _thornDiver;
            private HazardMark _thornMark;
            private bool _thornLapsed;

            // The redesign screen's pollen clouds (FoundryLevers.WraithClouds): where a Wraith fell, and since when (the
            // mark's Onset, held with the fight).
            private List<HazardMark> _clouds;

            private const string PollenHarm = "Pollen bloom";

            // FoeDied's hook (FoundryLevers.WraithClouds): a fallen Wraith leaves its cloud where it fell.
            private void WraithFell(Foe foe)
            {
                if (_clouds == null || !IsWraith(foe))
                    return;
                _clouds.Add(new HazardMark { X = foe.X, Y = foe.Y, Radius = _levers.WraithCloudRadius, Onset = 0f });
                _result.PollenClouds++;
            }

            // The clouds aged by a running frame (a gone one dropped), and how many hold the hero.
            private int CloudsHolding()
            {
                int holding = 0;
                for (int i = _clouds.Count - 1; i >= 0; i--)
                {
                    HazardMark cloud = _clouds[i];
                    cloud.Onset += Step;
                    if (!PollenRule.CloudStands(cloud.Onset, _levers.WraithCloudSeconds))
                    {
                        _clouds.RemoveAt(i);
                        continue;
                    }
                    if (PollenRule.Near(ArenaFloor.FloorDistanceSquared(cloud.X, cloud.Y, HeroX, HeroY), cloud.Radius))
                        holding++;
                }
                if (holding > 0)
                {
                    _result.InCloudSeconds += Step;
                    if (_pollen.IsStill)
                        _result.StillInCloudSeconds += Step;
                }
                return holding;
            }
            private const string ThornHarm = "Thornling eruption";

            private static bool IsWraith(Foe foe) => ReferenceEquals(foe.Definition, PollinatingWraith);

            private static bool IsThornling(Foe foe) => ReferenceEquals(foe.Definition, Thornling);

            // A body in the Wraith's place: a Wraith, or a Cinder Mite of the horde or a hall (not Thorn Post's, not the
            // Grove's escort).
            private static bool InWraithsPlace(Foe foe) =>
                IsWraith(foe) || (ReferenceEquals(foe.Definition, Mite) && !foe.Elite && !foe.Escort);

            // A new floor: the pollen and the ring start again (the scene resets them at each checkpoint), on floor 3 only.
            private void GloamFloorBegan()
            {
                _gloamFloor = _floor;
                if (_thornMark != null)
                    _marks.Remove(_thornMark);
                _thornMark = null;
                _thornDiver = null;
                if (_levers.NudgeFloor == _floor.FloorNumber && _dice != null)
                    for (int i = 0; i < _levers.NudgeDraws; i++)
                        _dice.Disc(1f, out _, out _);
                bool three = _floor.FloorNumber == 3;
                _pollen = three
                    ? new PollenState(_levers.WraithRate, _levers.WraithMost, _levers.WraithFall, _levers.WraithStillSpeed,
                        PollenRule.SpeedWindow)
                    : null;
                _pollenX = HeroX;
                _pollenY = HeroY;
                _clouds = three && _levers.GloamWraiths && _levers.WraithClouds ? new List<HazardMark>() : null;
                _thornField = three && _levers.GloamThornlings
                    ? new ThornlingField(_levers.ThornlingTelegraph, _levers.ThornlingGap)
                    : null;
            }

            // The hero's pollen, once a running frame: its smoothed speed from its walk since the last step, the living
            // Wraiths within the radius, the bloom. Held while the fight holds (a choice, the reveal) or the hero is down.
            private void StepPollen()
            {
                float dx = HeroX - _pollenX, dy = HeroY - _pollenY;
                _pollenX = HeroX;
                _pollenY = HeroY;
                if (_run.HasEnded || _choices.IsOpen || _revealHold || !_heroHealth.IsAlive)
                    return;
                int wraiths = 0;
                bool near = false;
                float radius = _levers.WraithRadius;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (!IsValid(foe) || !InWraithsPlace(foe))
                        continue;
                    if (!PollenRule.Near(ArenaFloor.FloorDistanceSquared(foe.X, foe.Y, HeroX, HeroY), radius))
                        continue;
                    near = true;
                    if (IsWraith(foe))
                        wraiths++;
                }
                bool bloomed;
                if (_clouds != null)
                    bloomed = _pollen.Step(Step, PollenRule.Distance(dx, dy), CloudsHolding(), _levers.WraithCloudWhileMoving,
                        _levers.WraithSettleAfter);
                else if (_levers.WraithSettleAfter > 0f)
                    bloomed = _pollen.Step(Step, PollenRule.Distance(dx, dy), wraiths, false, _levers.WraithSettleAfter);
                else
                    bloomed = _pollen.Step(Step, PollenRule.Distance(dx, dy), wraiths);
                if (_pollen.IsStill)
                    _result.StillSeconds += Step;
                if (near)
                    _result.NearSeconds += Step;
                if (near && _pollen.IsStill)
                    _result.StillNearSeconds += Step;
                if (!bloomed)
                    return;
                _result.PollenBlooms++;
                if (_waveAlive > 0)
                    _result.PollenBloomsInRooms++;
                float before = _heroHealth.Current;
                _hazardHarm = PollenHarm;
                _harmIsSlam = false;
                _hero.ApplyDamage(new DamageContext(PollenRule.BloomDamage(_heroHealth.Maximum, _levers.WraithShare)));
                _hazardHarm = null;
                _result.TakenFromPollen += before - _heroHealth.Current;
            }

            // The Thornlings, once a running frame: each one's clock (walking, stuck) and its walk speed; the floor's ring -
            // filling, then the eruption at its centre; or, with none down and the gap passed, the first free Thornling that
            // has walked long enough within the dive's distances dives and a ring goes down at the hero's spot. Held while the
            // fight holds: the ring's mark keeps its time to land.
            private void StepThornlings()
            {
                if (_run.HasEnded || _choices.IsOpen || _revealHold || !_heroHealth.IsAlive)
                {
                    if (_thornMark != null)
                    {
                        _thornMark.Onset += Step;
                        _thornMark.LandsAt += Step;
                    }
                    return;
                }
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (foe == null || foe.Released || !foe.IsAlive || foe.SpawnFrame == _frame || !IsThornling(foe))
                        continue;
                    foe.Thorn ??= new ThornlingClock(_levers.ThornlingWalk, _levers.ThornlingStuck);
                    foe.Thorn.Step(Step);
                    foe.Motion.SetSpeed(foe.Slot, foe.Thorn.Speed(foe.Definition.Speed));
                }
                if (_thornField.IsDown)
                {
                    if (_thornDiver == null || _thornDiver.Released || !_thornDiver.IsAlive)
                    {
                        _thornField.Cancel();
                        _marks.Remove(_thornMark);
                        _thornMark = null;
                        _thornDiver = null;
                        _result.ThornRingsCancelled++;
                        return;
                    }
                    if (_seconds < _lapseUntil)
                        _thornLapsed = true;
                    if (!_thornField.Step(Step))
                        return;
                    Erupt();
                    return;
                }
                _thornField.Step(Step);
                if (!_thornField.MayRing || ThornlingRule.Quiet(_levers.ThornlingQuietAtGate, GateBoss != null && GateBoss.IsAlive))
                    return;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (!IsValid(foe) || foe.Thorn == null ||
                        !foe.Thorn.WouldDive(ArenaFloor.FloorDistanceSquared(foe.X, foe.Y, HeroX, HeroY),
                            _levers.ThornlingDiveMin, _levers.ThornlingDiveMax))
                        continue;
                    foe.Thorn.Dive();
                    foe.Burrowed = true;
                    foe.Motion.SetSpeed(foe.Slot, 0f);
                    _thornDiver = foe;
                    _thornLapsed = _seconds < _lapseUntil;
                    _thornField.Down(HeroX, HeroY);
                    _thornMark = new HazardMark
                    {
                        X = HeroX, Y = HeroY, Radius = _levers.ThornlingRadius, Onset = _seconds,
                        LandsAt = _seconds + _levers.ThornlingTelegraph, Share = _levers.ThornlingShare, Harm = ThornHarm,
                        SourceX = foe.X, SourceY = foe.Y
                    };
                    if (!_levers.ThornlingUnseen)
                        _marks.Add(_thornMark);
                    _result.ThornRings++;
                    return;
                }
            }

            // The ring filled: the Thornling up at its centre, stuck; the hero caught if inside.
            private void Erupt()
            {
                Foe foe = _thornDiver;
                HazardMark mark = _thornMark;
                _marks.Remove(mark);
                _thornMark = null;
                _thornDiver = null;
                foe.Thorn.Erupt();
                foe.Burrowed = false;
                foe.Motion.Vacate(foe.Slot);
                float ux = _levers.ThornlingEruptsHome ? mark.SourceX : _thornField.X;
                float uy = _levers.ThornlingEruptsHome ? mark.SourceY : _thornField.Y;
                foe.Motion.Replace(foe.Slot, foe, new EntryPlacement(EntrySides.SideOf(0, 0), 0f, ux, uy),
                    foe.Thorn.Speed(foe.Definition.Speed), foe.Weapon.Range);
                if (!_thornField.Holds(HeroX, HeroY, _levers.ThornlingRadius))
                    return;
                _result.ThornHits++;
                if (_thornLapsed)
                    _result.ThornHitsInLapse++;
                _result.ThornShortestHit = System.Math.Min(_result.ThornShortestHit, _seconds - mark.Onset);
                float before = _heroHealth.Current;
                _hazardHarm = ThornHarm;
                _harmIsSlam = false;
                _hero.ApplyDamage(new DamageContext(ThornlingRule.EruptionDamage(_heroHealth.Maximum, _levers.ThornlingShare)));
                _hazardHarm = null;
                _result.TakenFromThorns += before - _heroHealth.Current;
            }

            // FoeDied's hook: an elite leader's fight, for the report.
            private void EliteLeaderFell(Foe foe)
            {
                _result.EliteFights ??= new List<EliteFight>(2);
                _result.EliteFights.Add(new EliteFight(foe.Definition.Name, _seconds - foe.SpawnedAt, foe.InReachSeconds,
                    foe.Health.Maximum, foe.Bloom != null ? foe.Bloom.WindowsBegun : 0, foe.Bloom != null && foe.Bloom.IsOpen));
            }
        }
    }
}
