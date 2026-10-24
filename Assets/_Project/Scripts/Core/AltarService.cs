using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Progression;
using Cryptforge.UI;
using UnityEngine;

namespace Cryptforge.Core
{
    // The foundry's altars in the scene (AltarRule): reads where the hero stands each frame, and when it has stayed on
    // an unused altar long enough, raises the run's difficulty and grants a bonus upgrade, whose offer opens as a level-up's
    // does. Each floor's altars answer again. Nothing happens off the foundry, while a choice is open, or once the run ended.
    public sealed class AltarService : MonoBehaviour
    {
        private CombatSetup _setup;
        private Transform _hero;
        private AltarField _field;
        private int _floor;

        public AltarField Field => _field;
        public bool IsActive => _field != null;
        // Raised once per altar taken, after the cost is applied and before the grant.
        public event Action<int> Taken;

        internal void Initialize(CombatSetup setup, Transform hero)
        {
            _setup = setup;
            _hero = hero;
            var arena = FindFirstObjectByType<ArenaView>();
            if (arena == null || !arena.IsFoundryRegion)
                return;
            var x = new float[FoundryMap.AltarCount];
            var y = new float[FoundryMap.AltarCount];
            for (int i = 0; i < x.Length; i++)
                FoundryMap.AltarSite(i, out x[i], out y[i]);
            _field = new AltarField(x, y);
            _floor = setup.Encounters.FloorNumber;
        }

        private float HeroX => _hero.position.x;
        private float HeroY => ArenaFloor.FloorY(_hero.position.y);

        private void Update()
        {
            if (_field == null || _setup.Run.HasEnded)
                return;
            if (_setup.Encounters.FloorNumber != _floor)
            {
                _floor = _setup.Encounters.FloorNumber;
                _field.Reset();
            }
            float x = HeroX, y = HeroY;
            if (Time.deltaTime <= 0f || _setup.Choices.IsOpen)
                return;
            int taken = _field.Step(x, y, Time.deltaTime);
            if (taken < 0)
                return;
            _setup.HeroStats.AddModifier(UpgradeStat.Difficulty, new StatModifier(ModifierOperation.Flat, AltarRule.DifficultyCost));
            // Reported before the grant, whose offer follows it in the run's log.
            Taken?.Invoke(taken);
            _setup.Run.GrantBonusUpgrade();
        }
    }
}
