using System.Text;

namespace Cryptforge.Localisation
{
    // The keys of a content asset's text (docs/60 section 4): "<id>.<field>" - "enemy_forge_warden.name",
    // "relic_second_wind.description", "hero_cinder_arcanist.unlock_hint" - with the asset's own English as the fallback.
    // A room has no id of its own (it is authored inline in its floor), so its key is made from its English name:
    // "Captain's Post" is "room.captains_post.name".
    public static class ContentKeys
    {
        public const string Name = "name";
        public const string Description = "description";
        // A hero skill's moment to press it, on the toast of the first run that carries it (docs/57 5.2).
        public const string TapHint = "tap_hint";

        public static string Key(string id, string field) => string.IsNullOrEmpty(id) ? null : id + "." + field;

        public static string Room(string englishName)
        {
            if (string.IsNullOrEmpty(englishName))
                return null;
            var slug = new StringBuilder("room.", englishName.Length + 10);
            bool gap = false;
            foreach (char c in englishName)
            {
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    if (gap && slug.Length > 5)
                        slug.Append('_');
                    slug.Append(c);
                    gap = false;
                }
                else if (c >= 'A' && c <= 'Z')
                {
                    if (gap && slug.Length > 5)
                        slug.Append('_');
                    slug.Append((char)(c - 'A' + 'a'));
                    gap = false;
                }
                else if (c != '\'' && c != '’')
                {
                    gap = true;
                }
            }
            return slug.Append(".name").ToString();
        }
    }
}
