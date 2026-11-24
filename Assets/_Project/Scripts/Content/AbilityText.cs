using Cryptforge.Localisation;

namespace Cryptforge.Content
{
    // An ability's words in the language the game speaks (docs/60): the hero's skills on the Forge's Skills tab (docs/57).
    // AbilityDefinition keeps its English fields as they are; its keys are <id>.name, <id>.description and <id>.tap_hint
    // (ContentKeys), each with the asset's own English as the fallback. The description's numbers - damage, radius,
    // cooldown, duration, amount - are written in the language's format (Loc.Numbers): "4,5 sn" in Turkish.
    public static class AbilityText
    {
        public static string Name(AbilityDefinition ability) =>
            ability == null ? string.Empty : Loc.Text(ContentKeys.Key(ability.Id, ContentKeys.Name), ability.DisplayName);

        public static string Description(AbilityDefinition ability) =>
            ability == null
                ? string.Empty
                : Loc.Format(ContentKeys.Key(ability.Id, ContentKeys.Description), ability.DescriptionFormat, ability.Damage,
                    ability.Radius, ability.Cooldown, ability.Duration, ability.Amount);

        // When to press it (docs/57 5.2's toast): "tap when the blows come"; empty for an ability without one.
        public static string TapHint(AbilityDefinition ability) =>
            ability == null || ability.TapHint.Length == 0
                ? string.Empty
                : Loc.Text(ContentKeys.Key(ability.Id, ContentKeys.TapHint), ability.TapHint);
    }
}
