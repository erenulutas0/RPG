using System;
using Cryptforge.Combat;

namespace Cryptforge.Progression
{
    // What a run can grow about the hero itself rather than about its weapon: how much it can take, how hard it is to
    // hurt, how fast it walks and how long it can run (docs/27 section 4, slice S2). The weapon's own stats - damage,
    // cadence, reach and the chance to crit - stay on WeaponRuntime, because that is where they are used.
    //
    // This holds numbers and nothing else. HeroStatsBinding is what pushes them into the health, the motion and the
    // movement budget, and both the scene and the Descent simulation call it, so one card changes one fight the same
    // way in both.
    public sealed class HeroStats
    {
        // Armor is not a percentage: it is a pool that buys reduction with diminishing returns, reduction =
        // armor / (armor + Softness), so 50 armor takes a third off, 100 takes half, and no amount ever takes it all.
        // A hard cap would make the last points worthless without saying so; this says so in its shape.
        public const float ArmorSoftness = 100f;

        private readonly ModifiableStat _maxHealth;
        private readonly ModifiableStat _armor;
        private readonly ModifiableStat _moveSpeed;
        private readonly ModifiableStat _staminaBar;
        private readonly ModifiableStat _luck;

        public float MaxHealth => _maxHealth.Value;
        public float Armor => _armor.Value;
        public float MoveSpeed => _moveSpeed.Value;
        public float StaminaBar => _staminaBar.Value;
        // Luck never touches combat. It lowers how much of a rarity draw the common tier takes, and nothing else.
        public float Luck => _luck.Value;

        // The share of a hit armor takes away, in [0, 1).
        public float DamageReduction => _armor.Value / (_armor.Value + ArmorSoftness);

        public event Action Changed;

        public HeroStats(float maxHealth, float moveSpeed, float staminaBar, float armor = 0f)
        {
            // Floors are safety, not balance: a hero cannot lose all its health, its walk or its bar to a negative card.
            _maxHealth = new ModifiableStat(maxHealth, 1f);
            _armor = new ModifiableStat(armor, 0f);
            _moveSpeed = new ModifiableStat(moveSpeed, 0.1f);
            _staminaBar = new ModifiableStat(staminaBar, 0.1f);
            _luck = new ModifiableStat(0f, 0f);
        }

        public static bool Owns(UpgradeStat stat) =>
            stat == UpgradeStat.MaxHealth || stat == UpgradeStat.Armor ||
            stat == UpgradeStat.MoveSpeed || stat == UpgradeStat.StaminaBar || stat == UpgradeStat.Luck;

        public void AddModifier(UpgradeStat stat, StatModifier modifier)
        {
            switch (stat)
            {
                case UpgradeStat.MaxHealth:
                    _maxHealth.AddModifier(modifier);
                    break;
                case UpgradeStat.Armor:
                    _armor.AddModifier(modifier);
                    break;
                case UpgradeStat.MoveSpeed:
                    _moveSpeed.AddModifier(modifier);
                    break;
                case UpgradeStat.StaminaBar:
                    _staminaBar.AddModifier(modifier);
                    break;
                case UpgradeStat.Luck:
                    _luck.AddModifier(modifier);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stat), "That stat belongs to the weapon, not the hero.");
            }

            Changed?.Invoke();
        }
    }
}
