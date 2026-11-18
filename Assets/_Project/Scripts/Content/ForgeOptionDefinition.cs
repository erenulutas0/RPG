using Cryptforge.Localisation;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Content
{
    // Localization keys: <id>.name and <id>.description. Heal amounts are fractions of maximum health and read as
    // whole percentages in the description; BonusUpgrade amounts are upgrade counts.
    [CreateAssetMenu(menuName = "Cryptforge/Forge Option Definition")]
    public sealed class ForgeOptionDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _descriptionFormat;
        [SerializeField] private ForgeEffect _effect;
        [SerializeField] private float _amount;

        public string Id => _id;
        public string DisplayName => Loc.Text(_nameKey ??= ContentKeys.Key(_id, ContentKeys.Name), _displayName);
        public string DescriptionFormat => Loc.Text(_descriptionKey ??= ContentKeys.Key(_id, ContentKeys.Description), _descriptionFormat);
        [System.NonSerialized] private string _nameKey;
        [System.NonSerialized] private string _descriptionKey;
        public ForgeEffect Effect => _effect;
        public float Amount => _amount;

        // In the language the game speaks as the visit opens.
        public ForgeOption CreateOption() => new ForgeOption(_id, DisplayName, DescriptionFormat, _effect, _amount);
    }
}
