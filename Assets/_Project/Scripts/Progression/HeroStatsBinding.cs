using Cryptforge.Combat;

namespace Cryptforge.Progression
{
    // Pushes a HeroStats sheet into the three things that read it. Absolute values, never deltas, so calling it twice
    // changes nothing the second time and the scene and the Descent simulation cannot drift apart by applying a card a
    // different number of times. Both call it on HeroStats.Changed and once at the start.
    public static class HeroStatsBinding
    {
        public static void Apply(HeroStats stats, HealthState health, HeroMotion motion, HeroStamina stamina)
        {
            if (stats == null)
                return;

            // A bigger hero is a healthier one: the health it gains arrives filled, which is what makes the card worth
            // taking mid-fight rather than only at the start of a floor.
            health?.SetMaximum(stats.MaxHealth);
            health?.SetDamageReduction(stats.DamageReduction);
            motion?.SetSpeed(stats.MoveSpeed);
            stamina?.SetBar(stats.StaminaBar);
        }
    }
}
