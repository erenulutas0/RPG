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
                    rooms[r][w] = wave;
                }
            }
            // Floor 2 is VaultsChain's own copy (Reshaped), so its next floor can be set without touching another chain.
            two.Next = three.Reshaped(rooms, three.Kinds);
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

            // Whether GloamChain reshapes Thorn Post.
            public bool Gloam => GloamCaptain || !ShippedBloom(this);
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
        }

        private sealed partial class Foe
        {
            // The Cinder Bloom's cycle (BloomRule): null until its first step - and for every other enemy.
            public BloomClock Bloom;
            // An elite leader's seconds within reach of the hero's longest weapon (FoundryResult.EliteFights).
            public float InReachSeconds;
            // The run second the Bloom's swing began (FoundryLevers.BloomWindup), NaN while none is under way.
            public float SwingSince = float.NaN;
        }

        private sealed partial class FoundryRun
        {
            private static bool IsBloom(Enemy kind) => ReferenceEquals(kind, CinderBloom) || kind.Name == CinderBloom.Name;

            // GloamService's step, where the Warden's slam and the Sentry's guard step: every standing Bloom's clock runs
            // (not on the frame it walks in) and roots it while open; while the fight holds (a choice, the reveal), neither.
            private void StepGloam()
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
