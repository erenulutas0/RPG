using System;
using Cryptforge.Combat;
using Cryptforge.UI;
using UnityEngine;

namespace Cryptforge.Core
{
    // The foundry gate boss's slams in the scene (WardenRule): from the moment the Warden has walked out of the gate and
    // the camera has come back, the clock marks where the hero stands and the hammer lands there a telegraph later. From
    // the mark until the hammer is out of the floor again the Warden's own weapon is held (WardenSlamClock.HoldsWeapon).
    // Only the foundry's gate boss slams; the hall's Warden, the Descent simulation and every other enemy fight as
    // authored. It also hands the gate boss to the hero's attacker (AttackController.GateBoss), so a weapon's landings on
    // it take the weapon's share (WardenRule.SwordShare: the Sword is the Warden's weapon).
    //
    // A gate boss that carries a GlassRegent - floor 2's since 2026-11-12 (docs/50 section 6, the lead's decisions) - keeps
    // its own beat (RegentRule) and marks a lane instead of a circle: as the mark goes down where the hero stands, a lane
    // from where the Regent stands through it (Lanes), and the blow catches a hero still inside it for RegentRule.LaneShare
    // of its maximum health. Its rod is held as the Warden's hammer is, from the mark until the recovery after the blow -
    // except on a beat whose mark went down with the hero at its feet (RegentRule.FeetReach, RodAtItsFeet; docs/50 "The
    // Regent's bite for the standing hero"): that hero is struck through the cast. The foundry simulation marks and judges
    // the same RegentLanes, and holds the rod by the same RegentRule.HoldsRod, at the same point of its frame
    // (DescentSimulation.Vaults.cs).
    //
    // A gate boss that carries a RootmotherMyrr - floor 3's since 2026-11-25 (docs/64 sections 15-16) - keeps her own beat
    // (MyrrRule: the Warden's clock, a 1.2 s telegraph) and lays a root snare (Snare, MyrrRule.SnareRadius) at the hero's
    // spot as the mark goes down. Every running frame, before the clock steps, the snare creeps after the hero at
    // MyrrRule.CreepSpeed and the mark is its centre (MarkX, MarkY); as the roots erupt they catch a hero still inside it for
    // MyrrRule.SnareShare of its maximum and plant it for MyrrRule.PlantSeconds of scaled time (IsHeroPlanted:
    // HeroMovementInput holds its walk; its weapons and its ability still strike). Her hook is held as the Warden's hammer -
    // except through a beat whose snare went down with the hero at her feet (MyrrRule.FeetReach, HookAtHerFeet), and
    // through the recovery after the roots caught the hero (HookFree; MyrrRule.HoldsHook). The foundry simulation steps the
    // same RootSnare through the same MyrrRule bodies at the same point of its frame (DescentSimulation.Myrr.cs).
    //
    // It runs after EncounterController (-50), which brings the gate boss out, and before every script of the default
    // order, every AttackController among them: the Warden's weapon is held from the very frame its mark goes down, not
    // one frame later as Unity happens to order the two, and the foundry simulation steps the clock at the same point of
    // its frame (DescentSimulation.Foundry, FoundrySimulationSceneValueTests).
    [DefaultExecutionOrder(-40)]
    public sealed class WardenSlamService : MonoBehaviour
    {
        private CombatSetup _setup;
        private Health _hero;
        private AttackController _heroAttack;
        private Health _boss;
        private AttackController _bossAttack;
        private readonly WardenSlamClock _clock = new WardenSlamClock();
        private readonly RegentLanes _lanes = new RegentLanes();
        private GlassRegent _regent;
        private readonly RootSnare _snare = new RootSnare();
        private RootmotherMyrr _myrr;
        // When the hero her roots caught walks again, on scaled time (MyrrRule.PlantedUntil).
        private float _plantedUntil = float.NegativeInfinity;

        public WardenSlamClock Clock => _clock;
        // The Glass Regent's lanes of the beat that is down (none between beats, or for the Warden).
        public RegentLanes Lanes => _lanes;
        // The gate boss's GlassRegent while a Regent is fought: its marks are lanes. Null for the Warden.
        public GlassRegent Regent => Boss != null ? _regent : null;
        public bool MarksLanes => Regent != null;
        // The gate boss's RootmotherMyrr while she is fought: her marks are root snares. Null for the Warden and the Regent.
        public RootmotherMyrr Myrr => Boss != null ? _myrr : null;
        public bool MarksSnare => Myrr != null;
        // Her snare of the beat that is down (IsDown false between beats, or for any other gate boss).
        public RootSnare Snare => _snare;
        public Health Boss => _boss != null && _boss.IsAlive ? _boss : null;
        public bool IsFighting => Boss != null;
        public bool IsEnraged { get; private set; }
        // Whether the Regent's beat down went down with the hero at its feet (RegentRule.FeetReach): it holds no rod through
        // this beat's cast and shards' run (RegentRule.HoldsRod). Read again as each mark goes down; false for the Warden.
        public bool RodAtItsFeet { get; private set; }
        // Whether Myrr's snare of this beat went down with the hero at her feet (MyrrRule.FeetReach): no hook held through the
        // beat. Read again as each snare goes down; false for the other gate bosses.
        public bool HookAtHerFeet { get; private set; }
        // Whether her hook is free through this beat: at her feet as the snare went down, or the roots caught the hero (free
        // through the recovery too). MyrrRule.HoldsHook's hookFree.
        public bool HookFree { get; private set; }
        // The hero her roots caught, planted: no walk until PlantedUntil (scaled time, Time.time).
        public bool IsHeroPlanted => MyrrRule.IsPlanted(Time.time, _plantedUntil);
        public float PlantedUntil => _plantedUntil;
        // How many times her roots have caught the hero in this scene.
        public int Plantings { get; private set; }
        // Where the mark is down, on the floor: the Warden's circle's centre, the Regent's mark, Myrr's snare's centre now.
        public float MarkX { get; private set; }
        public float MarkY { get; private set; }
        public event Action Marked;
        // A blow has landed; whether it caught the hero.
        public event Action<bool> Slammed;
        public bool LastSlamHit { get; private set; }
        // How far from the mark's centre the hero stood when the last blow landed, on the floor.
        public float LastSlamDistance { get; private set; }

        internal void Initialize(CombatSetup setup, Health hero, AttackController heroAttack)
        {
            _setup = setup;
            _hero = hero;
            _heroAttack = heroAttack;
        }

        // The hero the gate boss fights (the planted roots are drawn round its feet).
        public Health Hero => _hero;

        private void Update()
        {
            EncounterController encounter = _setup.Encounters;
            // The gate's boss is read, not waited for: on a floor that opens on its boss it walks out while the scene
            // is still being set up, before anything could listen.
            Health gate = encounter.GateBoss;
            if (gate != _boss)
            {
                Hold(false);
                _boss = gate;
                _bossAttack = gate != null ? gate.GetComponent<AttackController>() : null;
                _regent = gate != null ? gate.GetComponent<GlassRegent>() : null;
                _myrr = gate != null ? gate.GetComponent<RootmotherMyrr>() : null;
                if (_heroAttack != null)
                    _heroAttack.GateBoss = gate;
                IsEnraged = false;
                RodAtItsFeet = false;
                HookAtHerFeet = false;
                HookFree = false;
                encounter.SetBossEnraged(false);
                _clock.Reset();
                _lanes.Clear();
                _snare.Clear();
                if (_regent != null)
                    _clock.Tune(RegentRule.Every, RegentRule.EveryPhaseTwo, RegentRule.Telegraph);
                else if (_myrr != null)
                    _clock.Tune(MyrrRule.Every, MyrrRule.EveryPhaseTwo, MyrrRule.Telegraph);
                else
                    _clock.Tune(WardenRule.SlamEvery, WardenRule.SlamEveryEnraged, WardenRule.Telegraph);
            }
            Health boss = Boss;
            if (boss == null)
                return;
            // The run is over: no mark goes down and no blow lands. One left down - the hero fell to a horde's bite or the
            // pit under it - is withdrawn and the hammer comes down without its blow, as WardenSlamView withdraws a mark
            // when the fight stops; held, it stood raised behind the result (the run flow's review, 2026-11-11).
            if (_setup.Run.HasEnded)
            {
                if (_clock.HoldsWeapon)
                {
                    _clock.Reset();
                    _lanes.Clear();
                    _snare.Clear();
                    Hold(false);
                }
                return;
            }
            if (Time.deltaTime <= 0f || _setup.Choices.IsOpen || encounter.IsRevealHeld)
                return;

            if (!IsEnraged && WardenRule.IsEnraged(boss.Current, boss.Maximum))
            {
                IsEnraged = true;
                encounter.SetBossEnraged(true);
            }
            // Myrr's snare creeps after the hero before the clock steps, as the simulation's MyrrCreep: the mark is its centre.
            if (_myrr != null && _snare.IsDown)
            {
                _snare.Creep(_hero.transform.position.x, ArenaFloor.FloorY(_hero.transform.position.y), Time.deltaTime);
                MarkX = _snare.X;
                MarkY = _snare.Y;
            }
            switch (_clock.Advance(Time.deltaTime, IsEnraged))
            {
                case WardenSlamClock.Step.Marked:
                    MarkX = _hero.transform.position.x;
                    MarkY = ArenaFloor.FloorY(_hero.transform.position.y);
                    // The Regent's lane: from where it stands, through the mark.
                    if (_regent != null)
                    {
                        _lanes.Mark(MarkX, MarkY);
                        BossFloor(encounter, boss, out float regentX, out float regentY);
                        _lanes.Add(regentX, regentY);
                        // At its feet as the lane goes down: no rod held through this beat.
                        RodAtItsFeet = _lanes.AtItsFeet(RegentRule.FeetReach);
                    }
                    // Myrr's snare: down at the hero's spot; her hook free through this beat for a hero at her feet.
                    else if (_myrr != null)
                    {
                        _snare.Down(MarkX, MarkY);
                        BossFloor(encounter, boss, out float myrrX, out float myrrY);
                        HookAtHerFeet = MyrrRule.AtHerFeet(MarkX, MarkY, myrrX, myrrY, MyrrRule.FeetReach);
                        HookFree = HookAtHerFeet;
                    }
                    Marked?.Invoke();
                    break;
                case WardenSlamClock.Step.Slammed:
                    float heroX = _hero.transform.position.x, heroY = ArenaFloor.FloorY(_hero.transform.position.y);
                    float distanceSquared = ArenaFloor.FloorDistanceSquared(heroX, heroY, MarkX, MarkY);
                    LastSlamDistance = Mathf.Sqrt(distanceSquared);
                    if (_regent != null)
                    {
                        // The shards run down the lanes: a hero inside any takes one lane's share, however many.
                        LastSlamHit = _hero.IsAlive && _lanes.Holds(heroX, heroY, 0f);
                        if (LastSlamHit)
                            _hero.ApplyDamage(new DamageContext(_hero.Maximum * RegentRule.LaneShare, boss));
                        _lanes.Clear();
                    }
                    else if (_myrr != null)
                    {
                        // The roots erupt: a hero still inside the snare is caught, planted, and her hook free through the
                        // recovery; she is the source, so the result screen names her if the roots are the last.
                        LastSlamHit = _hero.IsAlive && _snare.Erupt(heroX, heroY);
                        _snare.Clear();
                        if (LastSlamHit)
                        {
                            _plantedUntil = MyrrRule.PlantedUntil(Time.time, MyrrRule.PlantSeconds);
                            Plantings++;
                            HookFree = true;
                            _hero.ApplyDamage(new DamageContext(MyrrRule.SnareDamage(_hero.Maximum, MyrrRule.SnareShare), boss));
                        }
                    }
                    else
                    {
                        LastSlamHit = _hero.IsAlive && distanceSquared <= WardenRule.SlamRadius * WardenRule.SlamRadius;
                        // The Warden is the source, so the result screen names it if the blow is the last.
                        if (LastSlamHit)
                            _hero.ApplyDamage(new DamageContext(_hero.Maximum * WardenRule.HeroShare, boss));
                    }
                    Slammed?.Invoke(LastSlamHit);
                    break;
            }
            // The Regent holds its rod as the Warden its hammer, but never for a hero at its feet; Myrr her hook, but never
            // while it is free.
            Hold(_regent != null ? RegentRule.HoldsRod(_clock.HoldsWeapon, RodAtItsFeet)
                : _myrr != null ? MyrrRule.HoldsHook(_clock.HoldsWeapon, HookFree)
                : _clock.HoldsWeapon);
        }

        // Where the gate boss stands on its pack's floor (the simulation's boss.X, Y), or under its body if the pack has
        // lost it.
        private static void BossFloor(EncounterController encounter, Health boss, out float x, out float y)
        {
            if (!encounter.TryFloorPositionOf(boss, out x, out y))
            {
                x = boss.transform.position.x;
                y = ArenaFloor.FloorY(boss.transform.position.y);
            }
        }

        // The Warden's weapon waits while its hammer is going up or is in the floor.
        private void Hold(bool held)
        {
            if (_bossAttack != null)
                _bossAttack.IsHeld = held;
        }
    }
}
