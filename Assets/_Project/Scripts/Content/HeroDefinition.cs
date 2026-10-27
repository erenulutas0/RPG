using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.Content
{
    // What a hero carries that no card gives: Ember Memory is the Cinder Arcanist's (docs/45) - every hit the hero lands
    // sets one Burn stack on the struck enemy (BurnRule), which a fall then passes to its neighbours.
    public enum HeroPassive
    {
        None = 0,
        EmberMemory = 1
    }

    [CreateAssetMenu(menuName = "Cryptforge/Hero Definition")]
    public sealed class HeroDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        // The character's own name under the class on the select screen (docs/44 section 11): "Ilya Kest".
        [SerializeField] private string _subtitle;
        [SerializeField, Min(1f)] private float _maximumHealth;
        [SerializeField] private WeaponDefinition _startingWeapon;
        // The active ability the hero fires by touch.
        [SerializeField] private AbilityDefinition _ability;
        [SerializeField] private HeroPassive _passive = HeroPassive.None;
        // Open from the first run (the Vanguard), or unlocked by clearing The Foundry carrying _unlockWeapon.
        [SerializeField] private bool _unlockedFromStart = true;
        [SerializeField] private WeaponDefinition _unlockWeapon;
        // The select screen's line for a locked hero, and the hero's three lines (docs/44 section 2).
        [SerializeField] private string _unlockHint;
        [SerializeField] private string _selectLine;
        [SerializeField] private string _deathLine;
        [SerializeField] private string _victoryLine;
        // The hero's own pre-rendered look (docs/45: Ilya, rendered from one model in five facings). Empty, the hero
        // wears the painted Vanguard (HeroLookView).
        [SerializeField] private RenderedCharacterArtSet _renderedArt;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Subtitle => _subtitle ?? string.Empty;
        public float MaximumHealth => _maximumHealth;
        public WeaponDefinition StartingWeapon => _startingWeapon;
        public AbilityDefinition Ability => _ability;
        public HeroPassive Passive => _passive;
        public bool UnlockedFromStart => _unlockedFromStart;
        public WeaponDefinition UnlockWeapon => _unlockWeapon;
        public string UnlockHint => _unlockHint ?? string.Empty;
        public string SelectLine => _selectLine ?? string.Empty;
        public string DeathLine => _deathLine ?? string.Empty;
        public string VictoryLine => _victoryLine ?? string.Empty;
        public RenderedCharacterArtSet RenderedArt => _renderedArt;
    }
}
