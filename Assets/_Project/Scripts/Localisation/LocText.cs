namespace Cryptforge.Localisation
{
    // A key with the English the game shows for it, for words kept in a list (GearText's stat names, LootText's materials):
    // Get() is Loc.Text(Key, English). Written as new LocText("key", "English") with both literals, so the table's checks
    // find the key and compare the English (LocalisationChecks).
    public readonly struct LocText
    {
        public LocText(string key, string english)
        {
            Key = key;
            English = english;
        }

        public string Key { get; }
        public string English { get; }

        public string Get() => Loc.Text(Key, English);

        public override string ToString() => Get();
    }
}
