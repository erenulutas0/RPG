using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Localisation;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // Shows why and how far the run went, what the Forge holds now, and offers a single restart or the Relic Forge.
    // Both buttons are briefly disabled after opening so a tap aimed at combat cannot act by accident. In the language the
    // game speaks (docs/60): the hero's name through HeroText, the numbers in the language's format, the buttons' words
    // following a language change. From the second completed run, the one rewarded offer under the gold and the "Skip
    // reward videos" link under it (docs/58 7.4; RunResultView.Offer.cs).
    public sealed partial class RunResultView : MonoBehaviour
    {
        [SerializeField] private CombatSetup _setup;
        [SerializeField] private EncounterController _encounters;
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private RelicForgeView _forge;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _causeLabel;
        [SerializeField] private Text _progressLabel;
        [SerializeField] private Text _goldLabel;
        [SerializeField] private Text _buildLabel;
        [SerializeField] private Text _forgeHintLabel;
        [SerializeField] private Text _restartLabel;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Text _forgeButtonLabel;
        [SerializeField] private Button _forgeButton;
        [SerializeField, Min(0f)] private float _inputDelay = 0.5f;
        [SerializeField] private Color _victoryColor = new Color(0.95f, 0.8f, 0.35f);
        [SerializeField] private Color _defeatColor = new Color(1f, 0.55f, 0.35f);
        private float _inputEnabledAt;
        private bool _awaitingInputDelay;
        private bool _subscribed;

        public bool IsOpen => _panel != null && _panel.activeSelf;

        private void Start()
        {
            if (_setup == null || _setup.Run == null || _setup.Upgrades == null || _setup.Relics == null || _encounters == null ||
                _encounters.Floor == null || _heroDefinition == null || _text == null || _forge == null || _panel == null ||
                _titleLabel == null || _causeLabel == null || _progressLabel == null || _goldLabel == null || _buildLabel == null ||
                _forgeHintLabel == null || _restartLabel == null || _restartButton == null || _forgeButtonLabel == null ||
                _forgeButton == null)
            {
                Debug.LogError("RunResultView is missing a scene or content reference.", this);
                enabled = false;
                return;
            }

            _panel.SetActive(false);
            LocalisedLabel.Bind(_restartLabel, () => _text.RestartLabel);
            LocalisedLabel.Bind(_forgeButtonLabel, () => _text.ForgeButtonLabel);
            _restartButton.onClick.AddListener(OnRestart);
            _forgeButton.onClick.AddListener(OnForge);
            BuildOffer();
            // RunBank subscribed in CombatSetup.Awake, so the run's gold is already in the profile when Show runs.
            _setup.Run.Ended += Show;
            _subscribed = true;
            if (_setup.Run.HasEnded)
                Show();
        }

        private void Show()
        {
            RunState run = _setup.Run;
            string hero = HeroText.Name(_setup.Hero != null ? _setup.Hero : _heroDefinition);
            // A defeat names the enemy whose hit landed last; a victory names the final wave's boss, even when its pack
            // outlived it.
            EnemyDefinition killer = run.Outcome != RunOutcome.Defeat
                ? _encounters.ToughestDefinition
                : _setup.LastAttacker != null ? _setup.LastAttacker : _encounters.CurrentDefinition;
            string enemyName = killer != null ? killer.DisplayName : string.Empty;
            string roomName = _encounters.CurrentRoom != null ? _encounters.CurrentRoom.DisplayName : string.Empty;
            string floorName = _encounters.Floor.DisplayName;
            switch (run.Outcome)
            {
                case RunOutcome.Victory:
                    _titleLabel.text = _text.ResultVictoryTitle;
                    _titleLabel.color = _victoryColor;
                    // The Descent's finale is a swarm left by its exit, its pack released with the room before the run
                    // ends, so no boss is left to name: it read "defeated the  and cleared" on every Ascent (the run
                    // flow's review, 2026-11-11).
                    _causeLabel.text = killer != null
                        ? string.Format(Loc.Numbers, _text.ResultVictoryCauseFormat, hero, enemyName, floorName)
                        : string.Format(Loc.Numbers, _text.ResultFinaleCauseFormat, hero, floorName);
                    break;
                case RunOutcome.Extracted:
                    _titleLabel.text = _text.ResultExtractedTitle;
                    _titleLabel.color = _victoryColor;
                    _causeLabel.text = string.Format(Loc.Numbers, _text.ResultExtractedCauseFormat, hero, floorName);
                    break;
                default:
                    _titleLabel.text = _text.ResultDefeatTitle;
                    _titleLabel.color = _defeatColor;
                    // The pit's spill has no attacker: it named the enemy whose hit landed before it (CombatSetup.LastHurtByPit).
                    _causeLabel.text = _setup.LastHurtByPit
                        ? string.Format(Loc.Numbers, _text.ResultPitCauseFormat, hero, roomName.Length > 0 ? roomName : floorName)
                        : string.Format(Loc.Numbers, _text.ResultDefeatCauseFormat, hero, enemyName, roomName);
                    break;
            }

            // A run on a tier above Forged names it after the title (docs/54 section 21): "Ascent complete  ·  Tempered".
            _titleLabel.text = TierText.AfterTitle(_titleLabel.text, _setup.Tier);
            _progressLabel.text = string.Format(Loc.Numbers, _text.ResultProgressFormat, _encounters.FloorNumber, _encounters.TotalRoomsCleared,
                run.Level, run.Experience);
            DrawGold();
            _buildLabel.text = string.Format(Loc.Numbers, _text.ResultBuildFormat, _setup.HeroWeapon.DisplayName, DescribeBuild());
            // The run's loot (docs/54) as the build label's last line, one short line ("Loot: Ember 4, Seal 1, 1 item"); the
            // Bag tab names each item. Never past the label's foot: a line that would overflow gives way to the bare count,
            // and that to nothing.
            AppendLoot();
            SkillLine = DescribeOpenedSkills();
            DrawForgeHint();
            // After the labels: the offer may make room for itself under the gold and write the gold again once credited.
            OpenOffer();

            _panel.SetActive(true);
            SetButtonsInteractable(false);
            _inputEnabledAt = Time.unscaledTime + _inputDelay;
            _awaitingInputDelay = true;
        }

        // The run's banked gold, and the offer's bonus with it once credited (docs/58 7.4).
        private void DrawGold()
        {
            RunState run = _setup.Run;
            int gold = run.GoldBanked + OfferCreditedGold;
            _goldLabel.text = run.GoldLost > 0
                ? string.Format(Loc.Numbers, _text.ResultGoldLostFormat, gold, run.GoldLost)
                : string.Format(Loc.Numbers, _text.ResultGoldFormat, gold);
        }

        // The loot line as shown, for tests and captures; empty when the run dropped nothing or the label had no room.
        public string LootLine { get; private set; } = string.Empty;

        private void AppendLoot()
        {
            LootLine = string.Empty;
            if (_setup.Loot == null || _setup.Bank == null)
                return;
            LootText.Compact(_setup.Loot, _setup.Bank.ItemDeposits, out string banked, out string lost);
            if (banked.Length == 0 && lost.Length == 0)
                return;
            string build = _buildLabel.text;
            string line = lost.Length > 0
                ? string.Format(Loc.Numbers, _text.ResultLootLostFormat,
                    banked.Length > 0 ? banked : Loc.Text("result.loot_nothing", "nothing"), lost)
                : string.Format(Loc.Numbers, _text.ResultLootFormat, banked);
            if (Fits(build + "\n" + line))
            {
                LootLine = line;
                _buildLabel.text = build + "\n" + line;
                return;
            }
            int pieces = _setup.Loot.Items.Count;
            for (int i = 0; i < GearCurrency.MaterialCount; i++)
                pieces += _setup.Loot.SecuredMaterial((MaterialKind)i) +
                          (_setup.Loot.Settlement != null ? _setup.Loot.Settlement.MaterialsKept[i] : 0);
            string bare = string.Format(Loc.Numbers, _text.ResultLootFormat,
                Loc.Format("result.loot_pieces", "{0} pieces (see the Bag)", pieces));
            if (Fits(build + "\n" + bare))
            {
                LootLine = bare;
                _buildLabel.text = build + "\n" + bare;
            }
        }

        // Whether the build label holds this text inside its rect at its font, as the label would lay it out (best fit
        // included: the generator is asked at the label's own settings).
        private bool Fits(string text)
        {
            RectTransform rect = _buildLabel.rectTransform;
            Vector2 size = rect.rect.size;
            if (size.x <= 0f || size.y <= 0f || _buildLabel.font == null)
                return true;
            TextGenerationSettings settings = _buildLabel.GetGenerationSettings(size);
            settings.resizeTextForBestFit = false;
            float scale = _buildLabel.pixelsPerUnit > 0f ? _buildLabel.pixelsPerUnit : 1f;
            float height = _buildLabel.cachedTextGeneratorForLayout.GetPreferredHeight(text, settings) / scale;
            return height <= size.y + .5f;
        }

        private string DescribeBuild()
        {
            var entries = new List<string>();
            if (_setup.Relic != null)
                entries.Add(string.Format(Loc.Numbers, _text.ResultRelicFormat, _setup.Relic.Relic.DisplayName, _setup.Relic.Triggers));

            UpgradeService upgrades = _setup.Upgrades;
            for (int i = 0; i < upgrades.Pool.Count; i++)
            {
                UpgradeOption option = upgrades.Pool[i];
                int stacks = upgrades.StacksOf(option);
                if (stacks > 0)
                    entries.Add(string.Format(Loc.Numbers, _text.ResultUpgradeFormat, option.DisplayName, stacks));
            }

            return entries.Count > 0 ? string.Join(", ", entries) : _text.ResultNoUpgrades;
        }

        // The line saying this run's floor clear opened a hero skill for the profile, as shown; empty when it opened none.
        public string SkillLine { get; private set; } = string.Empty;

        // The forge's hint, under the skill line when the run opened a skill: the skill is the nearest thing the Forge now
        // sells, so it comes first, in the call's gold.
        private void DrawForgeHint()
        {
            string hint = DescribeForge();
            _forgeHintLabel.text = SkillLine.Length > 0
                ? "<color=#" + ColorUtility.ToHtmlStringRGB(_victoryColor) + ">" + SkillLine + "</color>\n" + hint
                : hint;
        }

        // docs/57 section 14: a floor's first clear opens skill 2 (floor 2) or skill 3 (floor 3) for the profile, whichever
        // hero cleared it; the result says so in one line, naming the run's hero's skills, and where they are forged (the
        // Relic Forge, the button under it). Both at once when one run cleared floors 2 and 3 for the first time.
        private string DescribeOpenedSkills()
        {
            HeroDefinition hero = _setup.Hero != null ? _setup.Hero : _heroDefinition;
            int before = _setup.DeepestFloorClearedAtStart;
            int after = _setup.Profile.DeepestFloorCleared;
            AbilityDefinition first = null, second = null;
            for (int slot = HeroSkillUnlock.FirstSlot + 1; slot <= HeroSkillUnlock.LastSlot; slot++)
            {
                AbilityDefinition skill = hero.SkillAt(slot);
                if (skill == null || !HeroSkillUnlock.OpensBetween(slot, before, after))
                    continue;
                if (first == null)
                    first = skill;
                else
                    second = skill;
            }
            if (first == null)
                return string.Empty;
            return second == null
                ? Loc.Format("result.skill_opened", "New skill opened: {0}. Forge it at the Relic Forge", AbilityText.Name(first))
                : Loc.Format("result.skills_opened", "New skills opened: {0} and {1}. Forge them at the Relic Forge",
                    AbilityText.Name(first), AbilityText.Name(second));
        }

        // The "one more run" hook from 04_CORE_LOOP_RETENTION: always name the nearest unlock the forge sells. That is a
        // relic alone since 2026-11-23: every hero carries its own weapon and the weapon rack is gone (docs/63).
        private string DescribeForge()
        {
            int gold = _setup.Profile.Gold;
            RelicOption relic = _setup.Relics.NextUnlock;
            if (relic == null)
                return string.Format(Loc.Numbers, _text.ForgeHintRelicsCompleteFormat, gold);
            return gold >= relic.Price
                ? string.Format(Loc.Numbers, _text.ForgeHintReadyFormat, gold, relic.DisplayName)
                : string.Format(Loc.Numbers, _text.ForgeHintNextFormat, gold, relic.DisplayName, relic.Price);
        }

        private void Update()
        {
            if (!_awaitingInputDelay || Time.unscaledTime < _inputEnabledAt)
                return;

            _awaitingInputDelay = false;
            SetButtonsInteractable(true);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _restartButton.interactable = interactable;
            _forgeButton.interactable = interactable;
            SetOfferInteractable(interactable);
        }

        private void OnRestart()
        {
            if (_awaitingInputDelay || !_restartButton.interactable)
                return;

            SetButtonsInteractable(false);
            // Leaving the screen ends its offer: declined, or as its video or its ask ended (RewardOfferCoordinator.Close).
            CloseOffer();
            _setup.RestartRun();
        }

        private void OnForge()
        {
            if (_awaitingInputDelay || !_forgeButton.interactable)
                return;

            _forge.Open();
        }

        private void OnDestroy()
        {
            DestroyOffer();
            if (!_subscribed)
                return;

            _setup.Run.Ended -= Show;
            if (_restartButton != null)
                _restartButton.onClick.RemoveListener(OnRestart);
            if (_forgeButton != null)
                _forgeButton.onClick.RemoveListener(OnForge);
        }
    }
}
