using System.Collections.Generic;
using Cryptforge.Combat;

namespace Cryptforge.Tests
{
    // The Gloam Gardens' own enemies (docs/52), simulation research only (section 8, step 1; the lead's decisions
    // 2026-11-12: the Cinder Bloom's research first). The Cinder Bloom replaces the Grunt Captain at the head of floor 3's
    // Thorn Post (GloamChain) when FoundryLevers.GloamBloom asks for it; its bark-and-bloom cycle is BloomRule's, a
    // BloomClock on the foe stepped where the scene's GloamService would step it (after the Warden's slam and the Sentry's
    // guard, before every attacker), held while a choice is open or the reveal holds. Every behaviour below keys on a foe
    // that carries a clock, so a run that never meets a Bloom is the run it was: the default batch stays byte-identical.
    // The elite rooms' accounting (FoundryResult.EliteFights, EliteTaken*) is read for every elite leader - the Captains
    // too, for docs/52's baselines - and changes nothing a run does.
    internal static partial class DescentSimulation
    {
        // docs/52 section 5's starting values, authored (before the climb's floor-3 x1.96 health and x1.8032 damage: 549
        // health, 19.5 a maul): Bark Maul 10.8 every 1.8 s at reach 1.5 (the Grunt's), 0.6 s before the first; speed 1.2,
        // rooted while open; the Captain's 20 gold and 10 experience.
        public static readonly Enemy CinderBloom = new Enemy { Name = "Cinder Bloom", Health = 280f, Damage = 10.8f, Interval = 1.8f, InitialDelay = .6f, Gold = 20, Experience = 10, Speed = 1.2f, Reach = 1.5f };

        // The Bloom at the levers' health, maul and reach: itself at the starting values, else a copy under its name.
        private static Enemy Bloom(FoundryLevers levers) =>
            levers.BloomHealthScale == 1f && levers.BloomMaulScale == 1f && levers.BloomMaulReach == CinderBloom.Reach
                ? CinderBloom
                : new Enemy
                {
                    Name = CinderBloom.Name, Health = CinderBloom.Health * levers.BloomHealthScale,
                    Damage = CinderBloom.Damage * levers.BloomMaulScale, Interval = CinderBloom.Interval,
                    InitialDelay = CinderBloom.InitialDelay, Gold = CinderBloom.Gold, Experience = CinderBloom.Experience,
                    Speed = CinderBloom.Speed, Reach = levers.BloomMaulReach
                };

        // Floors 1 and 2 as VaultsChain makes them (the shipped Mirror Sentry on floor 2, or the levers' floor 2) above a
        // floor 3 with the levers' creatures in place of the foundry's: the Cinder Bloom for Thorn Post's Grunt Captain. Its
        // Grunt and two Cinder Mites, the halls and the Warden's Grove stay. With no Gloam lever it is VaultsChain's.
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
                    if (levers.GloamBloom && three.Kinds[r] == RoomKind.Elite && wave.Length > 0 && wave[0] == Captain)
                        wave[0] = Bloom(levers);
                    rooms[r][w] = wave;
                }
            }
            // Floor 2 is VaultsChain's own copy (Reshaped), so its next floor can be set without touching another chain.
            two.Next = three.Reshaped(rooms, three.Kinds);
            return one;
        }

        internal sealed partial class FoundryLevers
        {
            // docs/52's research creatures on floor 3, each off by default (GloamChain reads them).
            public bool GloamBloom;
            // The Cinder Bloom (section 5): its health and maul against the starting values', its maul's reach, and its
            // cycle (BloomRule's: closed, open, the share the closed bark lets through).
            public float BloomHealthScale = 1f;
            public float BloomMaulScale = 1f;
            public float BloomMaulReach = 1.5f;
            public float BloomClosed = BloomRule.ClosedSeconds;
            public float BloomOpen = BloomRule.OpenSeconds;
            public float BloomBark = BloomRule.BarkShare;

            public bool Gloam => GloamBloom;
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
        }

        private sealed partial class Foe
        {
            // The Cinder Bloom's cycle (BloomRule): null until its first step - and for every other enemy.
            public BloomClock Bloom;
            // An elite leader's seconds within reach of the hero's longest weapon (FoundryResult.EliteFights).
            public float InReachSeconds;
        }

        private sealed partial class FoundryRun
        {
            private static bool IsBloom(Enemy kind) => ReferenceEquals(kind, CinderBloom) || kind.Name == CinderBloom.Name;

            // GloamService's step, where the Warden's slam and the Sentry's guard step: every standing Bloom's clock runs
            // (not on the frame it walks in) and roots it while open; while the fight holds (a choice, the reveal), neither.
            private void StepGloam()
            {
                if (!_levers.Gloam || !_eliteWave || _run.HasEnded || _choices.IsOpen || _revealHold)
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
            private static bool Rooted(Foe foe) => foe.Bloom != null && foe.Bloom.IsOpen;

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
