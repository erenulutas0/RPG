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
        public string DisplayName => _displayName;
        public ForgeEffect Effect => _effect;
        public float Amount => _amount;

        public ForgeOption CreateOption() => new ForgeOption(_id, _displayName, _descriptionFormat, _effect, _amount);
    }
}
