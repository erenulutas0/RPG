using System;
using Cryptforge.Combat;
using Cryptforge.UI;
using UnityEngine;

namespace Cryptforge.Core
{
    // The foundry pit's eruptions in the scene (PitEruptionRule): the clock runs while a wave stands and nothing holds the
    // fight - no open choice, no boss reveal - and starts over between fights; on the spill it burns the hero and every
    // standing enemy inside the rim's band. Nothing happens off the foundry or once the run has ended.
    public sealed class PitEruptionService : MonoBehaviour
    {
        private CombatSetup _setup;
        private Health _hero;
        private PitEruptionClock _clock;

        public PitEruptionClock Clock => _clock;
        public bool IsActive => _clock != null;
        // True while a fight is running the clock.
        public bool IsRunning { get; private set; }
        public event Action Erupted;

        internal void Initialize(CombatSetup setup, Health hero)
        {
            _setup = setup;
            _hero = hero;
            var arena = FindFirstObjectByType<ArenaView>();
            if (arena != null && arena.IsFoundryRegion)
                _clock = new PitEruptionClock();
        }

        private void Update()
        {
            if (_clock == null)
                return;
            var encounter = _setup.Encounters;
            IsRunning = !_setup.Run.HasEnded && encounter.AliveEnemyCount > 0 && !encounter.IsInSwarmRoom;
            if (!IsRunning)
            {
                _clock.Reset();
                return;
            }
            if (Time.deltaTime <= 0f || _setup.Choices.IsOpen || encounter.IsRevealHeld || !_clock.Step(Time.deltaTime))
                return;
            if (_hero.IsAlive && PitEruptionRule.InBand(_hero.transform.position.x, ArenaFloor.FloorY(_hero.transform.position.y)))
                _hero.ApplyDamage(new DamageContext(_hero.Maximum * PitEruptionRule.HeroShare));
            for (int i = 0; i < encounter.WaveEnemyCount; i++)
            {
                Health enemy = encounter.WaveEnemyAt(i);
                if (enemy == null || !enemy.IsAlive)
                    continue;
                Vector3 p = enemy.transform.position;
                if (PitEruptionRule.InBand(p.x, ArenaFloor.FloorY(p.y)))
                    enemy.ApplyDamage(new DamageContext(enemy.Maximum * PitEruptionRule.EnemyShare));
            }
            Erupted?.Invoke();
        }
    }
}
