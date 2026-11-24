using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;

namespace Cryptforge.Tests
{
    // Rootmother Myrr, floor 3's own gate boss (docs/64; shipped 2026-11-25 at share 4.7 without saplings, section 15): she
    // holds the Warden's Grove (GloamGardens, FoundryFloorThree; DescentSimulation.RootmotherMyrr), its two Cinder Mites
    // beside her, on the Warden's clock with her own mark (MyrrRule): the root snare that creeps after the hero and plants
    // it, her hook free for a hero at her feet or caught by the roots - and, for the research only (MyrrSaplings above 0,
    // docs/64 section 3's design), in phase two the grafted saplings that grow into Thornlings if they stand. Stepped where
    // the scene's WardenSlamService steps her (StepWarden's hooks: the snare's creep before the clock, its mark, its
    // eruption) and GloamService would the saplings (StepGloam). FoundryLevers.GloamWarden puts the Forge Warden back at her
    // gate (floor 3 before 2026-11-25) for a comparison on the same seeds. Every behaviour keys on her definition or on her
    // garden, which exists only while she is fought, so a run that never meets her - floors 1 and 2 always - is the run it
    // was.
    internal static partial class DescentSimulation
    {
        // Her sapling: rooted, no strike and no reward; its health a share of hers, set as it is planted.
        public static readonly Enemy GraftedSapling = new Enemy { Name = "Grafted sapling", Health = 1f, Damage = .01f, Interval = 1f, Gold = 0, Experience = 0, Speed = 0f, Reach = 1f };
        // What a sapling grows into: docs/52's Thornling body (the Runner's numbers) without its dive, at the floor's tier as a
        // hall's enemy; no reward (its place in her fight is her mechanic's, as the Regent's copies are).
        public static readonly Enemy GraftedThornling = new Enemy { Name = "Thornling", Health = 30f, Damage = 1.2f, Interval = .4f, Gold = 0, Experience = 0, Speed = 3f, Reach = 1.5f };

        // Myrr at the levers' hook and walk: herself as written, else a copy under her name (her rules read the name).
        private static Enemy Myrr(FoundryLevers levers) =>
            levers.MyrrHookDamage == RootmotherMyrr.Damage && levers.MyrrSpeed == RootmotherMyrr.Speed
                ? RootmotherMyrr
                : new Enemy
                {
                    Name = RootmotherMyrr.Name, Health = RootmotherMyrr.Health, Damage = levers.MyrrHookDamage,
                    Interval = RootmotherMyrr.Interval, InitialDelay = RootmotherMyrr.InitialDelay, EnrageAt = RootmotherMyrr.EnrageAt,
                    Gold = RootmotherMyrr.Gold, Experience = RootmotherMyrr.Experience, Speed = levers.MyrrSpeed,
                    Reach = RootmotherMyrr.Reach
                };

        internal sealed partial class FoundryLevers
        {
            // docs/64's Rootmother Myrr at floor 3's gate (shipped 2026-11-25): GloamWarden puts the Forge Warden back there
            // at its share then (FloorThreeWardenShare; floor 3 before her), for a comparison on the same seeds. Her tuning
            // (research; each default is the shipped value): her gate share (0 keeps the floor's, FloorThreeGateShare), her
            // hook and walk, the beat (MyrrRule's Every, EveryPhaseTwo, Telegraph), the snare (radius, creep, share, plant),
            // her feet, whether roots that caught the hero free her hook through the recovery, and the saplings (how many at
            // phase two - MyrrRule.ShippedSaplings, none, as she ships; docs/64 section 3's MyrrRule.Saplings for the
            // research - then one every MyrrSaplingEvery, their growth, health share, distance, and the most saplings and
            // Thornlings standing).
            public bool GloamWarden;
            public float MyrrHealthShare;
            public float MyrrHookDamage = RootmotherMyrr.Damage;
            public float MyrrSpeed = RootmotherMyrr.Speed;
            public float MyrrEvery = MyrrRule.Every;
            public float MyrrEveryPhaseTwo = MyrrRule.EveryPhaseTwo;
            public float MyrrTelegraph = MyrrRule.Telegraph;
            public float MyrrSnareRadius = MyrrRule.SnareRadius;
            public float MyrrCreepSpeed = MyrrRule.CreepSpeed;
            public float MyrrSnareShare = MyrrRule.SnareShare;
            public float MyrrPlantSeconds = MyrrRule.PlantSeconds;
            public float MyrrFeetReach = MyrrRule.FeetReach;
            public bool MyrrPlantedFreesHook = true;
            public int MyrrSaplings = MyrrRule.ShippedSaplings;
            public float MyrrSaplingEvery = MyrrRule.SaplingEvery;
            public float MyrrSaplingGrow = MyrrRule.SaplingGrow;
            public float MyrrSaplingShare = MyrrRule.SaplingShare;
            public float MyrrSaplingDistance = MyrrRule.SaplingDistance;
            public int MyrrSaplingsMost = MyrrRule.SaplingsMost;
            public int MyrrThornlingsMost = MyrrRule.ThornlingsMost;
        }

        internal sealed partial class FoundryResult
        {
            // docs/64's Myrr (0 on any floor without her): her beats, the snares that caught the hero, the seconds it stood
            // planted, how far the snares crept, the beats that went down at her feet, the beats on which her hook was free
            // through the recovery (roots caught the hero), the run second her phase two began (from the gate wave's start),
            // the saplings planted and cut, the Thornlings grown and felled, and what her mark, her hook and her grown
            // Thornlings took from the hero.
            public int MyrrBeats;
            public int MyrrSnareHits;
            public float MyrrPlantedSeconds;
            public float MyrrCrept;
            public int MyrrFeetBeats;
            public int MyrrPlantedBeats;
            public float MyrrPhaseTwoAt = -1f;
            public int MyrrSaplingsPlanted;
            public int MyrrSaplingsCut;
            public int MyrrThornlingsGrown;
            public int MyrrThornlingsFelled;
            public float TakenFromSnare;
            public float TakenFromHook;
            public float TakenFromGrafted;
        }

        private sealed partial class Foe
        {
            // A grafted sapling's age, NaN for every other enemy.
            public float SaplingAge = float.NaN;
        }

        private sealed partial class FoundryRun
        {
            // Myrr's snare (null until she is first fought), whether her hook is free through this beat (the mark went down at
            // her feet, or the roots caught the hero), when the hero walks again, and her garden: the saplings and Thornlings
            // standing in her fight (null with no Myrr fought), its clock for the next sapling.
            private RootSnare _snare;
            private bool _myrrHookFree;
            private float _plantedUntil = float.NegativeInfinity;
            private List<Foe> _garden;
            private float _nextSapling;
            private int _saplingSide;

            private static bool IsMyrr(Foe foe) =>
                foe != null && (ReferenceEquals(foe.Definition, RootmotherMyrr) || foe.Definition.Name == RootmotherMyrr.Name);

            private static bool IsSapling(Foe foe) => ReferenceEquals(foe.Definition, GraftedSapling);

            private static bool IsGrafted(Foe foe) => ReferenceEquals(foe.Definition, GraftedThornling);

            // Whether the hero stands planted: no walk this frame.
            private bool Planted => MyrrRule.IsPlanted(_seconds, _plantedUntil);

            // StepWarden's new gate boss: Myrr's beat on the clock, her snare at the levers' radius and creep, a garden.
            private void MyrrTune(Foe gate)
            {
                _myrrHookFree = false;
                _snare?.Clear();
                if (_garden != null)
                    Wither();
                if (gate == null || !IsMyrr(gate))
                {
                    _garden = null;
                    return;
                }
                _slam.Tune(_levers.MyrrEvery, _levers.MyrrEveryPhaseTwo, _levers.MyrrTelegraph);
                _snare = new RootSnare(_levers.MyrrSnareRadius, _levers.MyrrCreepSpeed);
                _garden = new List<Foe>(8);
            }

            // Before the clock steps, every running frame of her fight: the snare down creeps after the hero, and the mark the
            // pilot reads is its centre.
            private void MyrrCreep()
            {
                if (_snare == null || !_snare.IsDown)
                    return;
                float before = _snare.Crept;
                _snare.Creep(HeroX, HeroY, Step);
                _result.MyrrCrept += _snare.Crept - before;
                _markX = _snare.X;
                _markY = _snare.Y;
            }

            // The beat's mark (WardenSlamService.Marked): the snare down at the hero's spot; her hook free through the beat for
            // a hero at her feet.
            private void MyrrMarked(Foe boss)
            {
                _result.MyrrBeats++;
                _snare.Down(_markX, _markY);
                _myrrHookFree = _levers.MyrrFeetReach > 0f &&
                                MyrrRule.AtHerFeet(_markX, _markY, boss.X, boss.Y, _levers.MyrrFeetReach);
                if (_myrrHookFree)
                    _result.MyrrFeetBeats++;
            }

            // The roots erupt (WardenSlamService.Slammed): whether they catch the hero; a caught hero is planted and her hook
            // is free through the recovery.
            private bool MyrrSnareHit()
            {
                bool hit = _heroHealth.IsAlive && _snare != null && _snare.Erupt(HeroX, HeroY);
                if (!hit)
                    return false;
                _result.MyrrSnareHits++;
                _plantedUntil = MyrrRule.PlantedUntil(_seconds, _levers.MyrrPlantSeconds);
                _result.MyrrPlantedSeconds += _levers.MyrrPlantSeconds;
                if (_levers.MyrrPlantedFreesHook && !_myrrHookFree)
                {
                    _myrrHookFree = true;
                    _result.MyrrPlantedBeats++;
                }
                return true;
            }

            // Whether Myrr's hook is held now (GateBossHeld): the clock's hold, but never when it is free.
            private bool MyrrHookHeld() =>
                MyrrRule.HoldsHook(_slam.HoldsWeaponFor(_levers.WardenRecoverySeconds), _myrrHookFree);

            // Phase two (StepWarden, the frame she crosses half her health): the first saplings.
            private void MyrrPhaseTwo(Foe boss)
            {
                _result.MyrrPhaseTwoAt = _seconds - _waveStartedAt;
                for (int i = 0; i < _levers.MyrrSaplings; i++)
                    Plant(boss);
                _nextSapling = _levers.MyrrSaplingEvery;
            }

            // GloamService's step for her garden, every running frame while she stands: a sapling every MyrrSaplingEvery in
            // phase two, each sapling's growth into a Thornling. Gone with her (Wither).
            private void StepGarden()
            {
                if (_garden == null)
                    return;
                Foe boss = _slamBoss != null && _slamBoss.IsAlive && IsMyrr(_slamBoss) ? _slamBoss : null;
                if (boss == null)
                {
                    if (_garden.Count > 0 && !_choices.IsOpen && !_revealHold)
                        Wither();
                    return;
                }
                if (_run.HasEnded || _choices.IsOpen || _revealHold || !_heroHealth.IsAlive)
                    return;
                if (_slamEnraged && _levers.MyrrSaplings > 0)
                {
                    _nextSapling -= Step;
                    if (_nextSapling <= 0f)
                    {
                        Plant(boss);
                        _nextSapling = _levers.MyrrSaplingEvery;
                    }
                }
                for (int i = 0; i < _garden.Count; i++)
                {
                    Foe sapling = _garden[i];
                    if (!IsSapling(sapling) || !IsValid(sapling))
                        continue;
                    sapling.SaplingAge += Step;
                    if (MyrrRule.Grown(sapling.SaplingAge, _levers.MyrrSaplingGrow) && Standing(IsGrafted) < _levers.MyrrThornlingsMost)
                        Grow(sapling, i);
                }
            }

            // How many of the garden's bodies of a kind stand.
            private int Standing(Func<Foe, bool> kind)
            {
                int standing = 0;
                for (int i = 0; i < _garden.Count; i++)
                    if (kind(_garden[i]) && IsValid(_garden[i]))
                        standing++;
                return standing;
            }

            // A sapling roots beside her, on her sides in turn (MyrrRule.SaplingSpot), on the floor; none past the most.
            private void Plant(Foe boss)
            {
                if (_waveMotion == null || _waveMotion.Count >= PackLayout.MaxPackSize ||
                    Standing(IsSapling) >= _levers.MyrrSaplingsMost)
                    return;
                _saplingSide = _saplingSide > 0 ? -1 : 1;
                MyrrRule.SaplingSpot(boss.X, boss.Y, HeroX, HeroY, _levers.MyrrSaplingDistance, _saplingSide, out float x,
                    out float y);
                if (!FoundryMap.Contains(x, y, HeroMotion.EdgeMargin))
                    MyrrRule.SaplingSpot(boss.X, boss.Y, HeroX, HeroY, _levers.MyrrSaplingDistance, -_saplingSide, out x, out y);
                if (!FoundryMap.Contains(x, y, HeroMotion.EdgeMargin))
                {
                    x = boss.X;
                    y = boss.Y;
                }
                Foe sapling = CreateFoe(GraftedSapling,
                    MyrrRule.SaplingHealth(boss.Health.Maximum, _levers.MyrrSaplingShare), 0f, false);
                sapling.Gold = 0;
                sapling.Experience = 0;
                sapling.SaplingAge = 0f;
                sapling.Motion = _waveMotion;
                sapling.Slot = _waveMotion.Add(sapling, new EntryPlacement(EntrySides.SideOf(0, 0), 0f, x, y), 0f,
                    GraftedSapling.Reach);
                _wave.Add(sapling);
                _garden.Add(sapling);
                _result.MyrrSaplingsPlanted++;
            }

            // A sapling grown: its place taken by a Thornling at the floor's tier, as a hall's enemy (SpawnWave's scaling).
            private void Grow(Foe sapling, int index)
            {
                int n = _floor.FloorNumber;
                float scale = _heroStats.Difficulty;
                float damageBonus = FloorScaling.DamageBonus(FloorScaling.Tiered(ClimbScaling.Damage(n), _tierDamage) * scale,
                    _floor.ModifierDamagePercent);
                float healthScale = FloorScaling.Tiered(ClimbScaling.Health(n), _tierHealth) * scale;
                float x = sapling.X, y = sapling.Y;
                sapling.Released = true;
                sapling.Defeated = true;
                sapling.Motion.Vacate(sapling.Slot);
                Foe thornling = CreateFoe(GraftedThornling,
                    FloorScaling.Health(GraftedThornling.Health, healthScale, _floor.ModifierHealthPercent), damageBonus, false);
                thornling.Gold = 0;
                thornling.Experience = 0;
                thornling.Motion = sapling.Motion;
                thornling.Slot = sapling.Slot;
                thornling.Motion.Replace(thornling.Slot, thornling, new EntryPlacement(EntrySides.SideOf(0, 0), 0f, x, y),
                    GraftedThornling.Speed, GraftedThornling.Reach);
                _wave.Add(thornling);
                _garden[index] = thornling;
                _result.MyrrThornlingsGrown++;
            }

            // Her garden withers as she falls (or the fight changes): saplings and Thornlings leave the floor unrewarded.
            private void Wither()
            {
                for (int i = 0; i < _garden.Count; i++)
                {
                    Foe foe = _garden[i];
                    if (foe == null || foe.Defeated)
                        continue;
                    foe.Released = true;
                    foe.Defeated = true;
                    if (foe.Motion != null && foe.Slot < foe.Motion.Count)
                        foe.Motion.Vacate(foe.Slot);
                }
                _garden.Clear();
            }

            // FoeDied's hook: a sapling cut or a grown Thornling felled - no kill, no reward, and the gate wave's count never
            // held it.
            private bool GardenFell(Foe foe)
            {
                if (IsSapling(foe))
                {
                    if (!foe.Released)
                        _result.MyrrSaplingsCut++;
                    return true;
                }
                if (!IsGrafted(foe))
                    return false;
                if (!foe.Released)
                    _result.MyrrThornlingsFelled++;
                return true;
            }

            // HeroDamaged's hook while she is fought: what her mark, her hook and her grown Thornlings took.
            private void MyrrTaken(DamageContext context, float taken)
            {
                if (!(context.Source is Foe foe))
                    return;
                if (IsMyrr(foe) && _harmIsSlam)
                    _result.TakenFromSnare += taken;
                else if (IsMyrr(foe))
                    _result.TakenFromHook += taken;
                else if (IsGrafted(foe))
                    _result.TakenFromGrafted += taken;
            }
        }
    }
}
