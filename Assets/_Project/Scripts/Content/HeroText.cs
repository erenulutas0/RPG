using Cryptforge.Localisation;

namespace Cryptforge.Content
{
    // A hero's words in the language the game speaks (docs/60). HeroDefinition keeps its English fields as they are; its
    // keys are <id>.name, <id>.subtitle, <id>.role and <id>.unlock_hint (ContentKeys), each with the hero's own English as
    // the fallback. The views read a hero's words through here, never its fields.
    public static class HeroText
    {
        public static string Name(HeroDefinition hero) =>
            hero == null ? string.Empty : Loc.Text(ContentKeys.Key(hero.Id, ContentKeys.Name), hero.DisplayName);

        public static string Subtitle(HeroDefinition hero) =>
            hero == null ? string.Empty : Loc.Text(ContentKeys.Key(hero.Id, "subtitle"), hero.Subtitle);

        public static string Role(HeroDefinition hero) =>
            hero == null ? string.Empty : Loc.Text(ContentKeys.Key(hero.Id, "role"), hero.Role);

        public static string UnlockHint(HeroDefinition hero) =>
            hero == null ? string.Empty : Loc.Text(ContentKeys.Key(hero.Id, "unlock_hint"), hero.UnlockHint);
    }
}
