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
        // What the hero does, in one line, on the map choice's hero card (MapChoiceView): "Every hit burns...".
        [SerializeField] private string _role;
        [SerializeField, Min(1f)] private float _maximumHealth;
        // The hero's own weapon (docs/63, its signature weapon): every run starts with it - Ilya's crucible Staff since
        // 2026-10-29, the Vanguard's Sword since 2026-11-22, and the forge sells no other since 2026-11-23. In the run it may
        // still take any weapon a card offers; under the weapon roster another hero's own weapon is never one of them.
        [SerializeField] private WeaponDefinition _startingWeapon;
        // The active ability the hero fires by touch.
        [SerializeField] private AbilityDefinition _ability;
        // docs/57's skills 2 and 3 (the owner's answers 2026-11-18): the Vanguard's Iron Resolve and Shield Rush, Ilya's
        // Flame Sigil and Cinder Ward. Phase 4 opens them (the first clear of floors 2 and 3, then the Forge); until then a
        // run carries them only where a development file asks (DevelopmentStart.SkillSlots). Empty for a hero without.
        [SerializeField] private AbilityDefinition _secondAbility;
        [SerializeField] private AbilityDefinition _thirdAbility;
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
        // The hero as the forge's chooser pictures it (HeroChoiceView): one of its own standing frames. Empty, the
        // chooser shows the name alone.
        [SerializeField] private Sprite _portrait;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Subtitle => _subtitle ?? string.Empty;
        public string Role => _role ?? string.Empty;
        public float MaximumHealth => _maximumHealth;
        public WeaponDefinition StartingWeapon => _startingWeapon;
        public AbilityDefinition Ability => _ability;
        // The hero's skill in slot 1 (Ability), 2 or 3; null for an empty slot or any other number.
        public AbilityDefinition SkillAt(int slot) =>
            slot == 1 ? _ability : slot == 2 ? _secondAbility : slot == 3 ? _thirdAbility : null;
        public HeroPassive Passive => _passive;
        public bool UnlockedFromStart => _unlockedFromStart;
        public WeaponDefinition UnlockWeapon => _unlockWeapon;
        public string UnlockHint => _unlockHint ?? string.Empty;
        public string SelectLine => _selectLine ?? string.Empty;
        public string DeathLine => _deathLine ?? string.Empty;
        public string VictoryLine => _victoryLine ?? string.Empty;
        public RenderedCharacterArtSet RenderedArt => _renderedArt;
        public Sprite Portrait => _portrait;
    }
}
