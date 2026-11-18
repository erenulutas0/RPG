using Cryptforge.Localisation;
using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Content
{
    // A permanent unlock sold in the Relic Forge and equipped before a run. Localization keys: <id>.name and
    // <id>.description; the description format receives Amount and Threshold as whole percentages.
    [CreateAssetMenu(menuName = "Cryptforge/Relic Definition")]
    public sealed class RelicDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _descriptionFormat;
        [SerializeField] private RelicEffect _effect;
        [SerializeField, Min(0f)] private float _amount;
        [SerializeField, Range(0f, 1f)] private float _threshold;
        [SerializeField, Min(0)] private int _price;

        public string Id => _id;
        public string DisplayName => Loc.Text(_nameKey ??= ContentKeys.Key(_id, ContentKeys.Name), _displayName);
        public string DescriptionFormat => Loc.Text(_descriptionKey ??= ContentKeys.Key(_id, ContentKeys.Description), _descriptionFormat);
        [System.NonSerialized] private string _nameKey;
        [System.NonSerialized] private string _descriptionKey;
        public RelicEffect Effect => _effect;
        public int Price => _price;

        public RelicOption CreateOption() =>
            new RelicOption(_id, DisplayName, DescriptionFormat, _effect, _amount, _threshold, _price);
    }
}
