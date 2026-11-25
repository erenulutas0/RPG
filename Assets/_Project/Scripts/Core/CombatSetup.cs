using System;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Analytics;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Economy;
using Cryptforge.Localisation;
using Cryptforge.Monetisation;
using Cryptforge.Progression;
using Cryptforge.UI;
using Cryptforge.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cryptforge.Core
{
    // Scene composition and run lifecycle: which services exist, and what ends or continues the run.
    // Combat and presentation own their own updates/events.
    public sealed class CombatSetup : MonoBehaviour
    {
        [SerializeField] private HeroDefinition _heroDefinition;
        // Every playable hero, the default first; the profile's choice picks one (docs/45, the Cinder Arcanist).
        [SerializeField] private HeroDefinition[] _heroes;
        [SerializeField] private EconomyConfig _economy;
        [SerializeField] private UpgradeDefinition[] _upgrades;
        [SerializeField] private RelicDefinition[] _relics;
        // The hero weapons a run's cards can hand over, in card order; every hero's starting weapon must be one of them.
        // Since 2026-11-23 the Relic Forge sells none: every hero starts with its own weapon (docs/63).
        [SerializeField] private WeaponDefinition[] _weapons;
        // docs/63's shared weapons built behind the weapon roster's lever (the Magma Mortar, the Forge Bow, and phase 2's
        // Cinder Saw, section 13): offered only
        // when development/weapon-roster.txt turns the roster on, after _weapons, every hero's own weapon then kept to that
        // hero (WeaponRoster). Off - every release build and the default batch - a run's cards are _weapons' alone.
        [SerializeField] private WeaponDefinition[] _rosterWeapons;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private Health _hero;
        [SerializeField] private AttackController _attack;
        [SerializeField] private AbilityController _ability;
        [SerializeField] private RelicBehaviour _relicBehaviour;
        [SerializeField] private EncounterController _encounters;
        [SerializeField] private Sprite _groundRewardSprite;
        [SerializeField] private Sprite _groundExperienceSprite;
        // Gold alone (an overrun kill pays no experience): the pile's own two coins; empty, the pixel coin.
        [SerializeField] private Sprite _groundGoldSprite;
        // The run's growth on the hero painted (RunFeedbackView); empty, it draws its lines.
        [SerializeField] private RunFeedbackArtSet _runFeedbackArt;
        private RewardService _rewards;
        // Burn's own neighbourhood (TargetingNeighbourhood): never the AttackController's, whose list a burst walks.
        private INeighbourhood _burnNeighbourhood;
        private ProfileStore _profileStore;
        private bool _timeFrozen;
        private bool _restarting;
        private RunTelemetryBridge _telemetry;
        // A deeper start's level-ups are open: the first wave waits for the last of them (OnChoicesChanged).
        private bool _firstWaveAfterChoices;
        // What Ember Memory's burns tick at: the gear's burn-damage factor (GearStats.BurnFactor), 1 without one.
        private float _burnFactor = 1f;
        // This run's id when telemetry gave it none (a run ended before it began recording): one fresh GUID a run.
        private string _fallbackRunId;

        // The floor this run began on: 1, or the deeper map chosen before this load (MapChoiceView, DescentStart).
        public int StartFloor { get; private set; } = 1;
        // The difficulty tier this run is played on (docs/54 section 21): the one chosen with the map before this load, or
        // the profile's choice - always an open tier. Forged is the Descent as it shipped.
        public DifficultyTier Tier { get; private set; }
        // The map choice is open: the first wave and the run's telemetry wait for a map (StartOnMap).
        public bool IsChoosingMap { get; private set; }
        // The language this scene's words were made in (docs/60): the level-up cards, the forge's and the shops' were
        // named as it loaded. The map choice's toggle may change it before the run starts; StartOnMap then loads the scene
        // again, so the whole run speaks the language chosen.
        public Language SceneLanguage { get; private set; }
        public bool LanguageChangedSinceLoad => Loc.Language != SceneLanguage;

        // The hero this run plays: the profile's choice when it may be played, otherwise the default.
        public HeroDefinition Hero { get; private set; }
        // Every playable hero, the default first, for the select screen.
        public IReadOnlyList<HeroDefinition> Heroes => _heroes != null && _heroes.Length > 0
            ? (IReadOnlyList<HeroDefinition>)_heroes : new[] { _heroDefinition };

        // The wave the hero is fighting, for the few systems that follow the fight itself rather than the run.
        public EncounterController Encounters => _encounters;
        // What this run has grown about the hero itself; the weapon's own stats live on Weapon.
        public HeroStats HeroStats { get; private set; }
        public RunState Run { get; private set; }
        public WeaponRuntime Weapon { get; private set; }
        // The hero's active ability this run, from the hero definition.
        public AbilityRuntime Ability { get; private set; }
        public UpgradeService Upgrades { get; private set; }
        // The Descent's rewards lying on the floor (docs/27, S6d), for whatever shows them.
        public GroundDrops Drops { get; private set; }
        // The foundry's altars (inactive elsewhere).
        public AltarService Altars { get; private set; }
        // The foundry pit's eruptions (inactive elsewhere).
        public PitEruptionService Pit { get; private set; }
        // The foundry gate boss's slams (WardenRule).
        public WardenSlamService Warden { get; private set; }
        // The Mirror Sentries' guards (MirrorGuardRule): their turn, their budget and the reflection they pay the hero.
        public MirrorGuardService Mirrors { get; private set; }
        // The Gloam Gardens' own enemies (docs/52): the Cinder Bloom's cycle, its rooting and its held maul.
        public GloamService Gloam { get; private set; }
        public ForgeService Forge { get; private set; }
        public CheckpointService Checkpoint { get; private set; }
        public RunChoices Choices { get; private set; }
        public RunPause Pause { get; private set; }
        public PlayerProfile Profile { get; private set; }
        // The profile's depth record as this run began (PlayerProfile.DeepestFloorCleared at the load): the result screen
        // reads which of the hero skills this run's clears opened (HeroSkillUnlock.OpensBetween, docs/57 section 14).
        public int DeepestFloorClearedAtStart { get; private set; }
        public RelicShop Relics { get; private set; }
        // The weapon the hero carries this run: its own starting weapon (docs/63: every hero is bound to its own since
        // 2026-11-22, and the forge's weapon rack is gone since 2026-11-23), or development/start-weapon.txt's.
        public WeaponDefinition HeroWeapon { get; private set; }
        public RunBank Bank { get; private set; }
        // The run's floor materials, boosters and items (docs/54 phase 3): dropped by bosses, elites, floor clears and the
        // walk-out, banked by Bank like gold except items, which are always kept.
        public RunLoot Loot { get; private set; }
        // The Anvil over the profile (docs/54): basma, efsun, melt, inheritance and equipping, between runs.
        public Anvil Anvil { get; private set; }
        // What this run wears (docs/54 phase 5), read from the profile as the run began; empty (IsEmpty) for a profile
        // without gear.
        public GearLoadout Gear { get; private set; } = GearLoadout.None;
        // Null when no relic is equipped.
        public RelicRuntime Relic { get; private set; }
        // The enemy whose hit last damaged the hero; after a defeat, the killer named on the result screen.
        public EnemyDefinition LastAttacker { get; private set; }
        // The foundry pit's spill burnt the hero after any enemy's hit (PitEruptionService.IsBurningHero): after a defeat
        // the pit is the cause, not the enemy whose hit landed before it - its blow has no attacker, and LastAttacker
        // named that enemy (the run flow's review, 2026-11-11). An enemy's next hit clears it.
        public bool LastHurtByPit { get; private set; }
        // The killer telemetry writes into run_end's cause (killed_by:<id>): the pit's id, the last attacker's, or none.
        public string KillerTelemetryId => LastHurtByPit ? PitEruptionService.KillerId : LastAttacker != null ? LastAttacker.Id : null;
        // The run's result is on disk (docs/58 3.1, save before any video): the profile was written once more as the run
        // ended, after the bank's deposit, and that write returned without an exception. False until then, false when it
        // failed (the result screen then offers no video), and false again from RestartRun. Neither the bank's own save,
        // which swallows a failure, nor a run that banked nothing (no save at all) can stand for it.
        public bool ResultSaved { get; private set; }
        // The run's id for the result screen's offer (docs/58 7.4): telemetry's (RunTelemetry.RunId, kept after run_end), or
        // one fresh GUID for this run when telemetry has none.
        public string RunId
        {
            get
            {
                string recorded = _telemetry != null ? _telemetry.RunId : null;
                if (!string.IsNullOrEmpty(recorded))
                    return recorded;
                return _fallbackRunId ??= Guid.NewGuid().ToString("N");
            }
        }

        private void Awake()
        {
            SceneLanguage = Loc.Language;
            if (_heroDefinition == null || _economy == null || _text == null || _hero == null || _attack == null ||
                _ability == null || _relicBehaviour == null || _encounters == null || _heroDefinition.StartingWeapon == null ||
                _heroDefinition.Ability == null ||
                !AllPresent(_upgrades) || !AllPresent(_relics) || !AllPresent(_weapons) ||
                (_rosterWeapons != null && _rosterWeapons.Length > 0 && !AllPresent(_rosterWeapons)) ||
                Array.IndexOf(_weapons, _heroDefinition.StartingWeapon) < 0)
            {
                Debug.LogError("CombatSetup is missing required scene or definition references.", this);
                enabled = false;
                return;
            }

            // The weapons are checked here too (their combat data, ids and texts), though the forge sells none of them now -
            // the roster's with the rest, so a broken one is reported at startup whether or not the lever is on.
            if (!ForgeCatalog.TryCreate(_relics, AllWeapons(), out RelicOption[] relics, out WeaponOption[] _, out string catalogError))
            {
                Debug.LogError($"CombatSetup cannot build the Relic Forge catalog. {catalogError}", this);
                enabled = false;
                return;
            }

            Application.targetFrameRate = 60;
            // The profile is read from disk on every scene load, so a restart needs no persistent service object - unless a
            // save of this session did not land (a full phone, or a newer build's file: read only): then the profile it
            // could not write comes over the reload instead of the older file (UnsavedProfile, ProfileStore.LoadSession).
            _profileStore = new ProfileStore(ProfileLocation.Resolve());
            Profile = _profileStore.LoadSession();
            DeepestFloorClearedAtStart = Profile.DeepestFloorCleared;
            ReportLoadProblems();
            TellProfileState();
            Relics = new RelicShop(Profile, relics);
            // The map, hero and tier chosen before this load (MapChoiceView), if any: taken here, before the hero is chosen.
            int wanted = RunStartRequest.Take(out string requestedHero, out DifficultyTier? requestedTier);
            Hero = ChooseHero(requestedHero);
            Tier = requestedTier.HasValue && Profile.Tiers.IsOpen(requestedTier.Value) ? requestedTier.Value : Profile.Tiers.Selected;
            // Every hero starts with its own weapon (the Vanguard its Sword since 2026-11-22, Ilya its Staff); in the Editor
            // and a development build development/start-weapon.txt may start it with another (DevelopmentStart.StartWeaponId).
            // A weapon the retired forge rack sold and the profile still records (PlayerProfile.OwnedWeaponIds) is not read.
            HeroWeapon = DevelopmentStartWeapon() ?? Hero.StartingWeapon;

            _hero.Initialize(Hero.MaximumHealth);
            // On the foundry a run builds Megabonk's way (docs/42, PR 7): the forge's weapon is the first of up to three,
            // every weapon of the catalogue can come as a card whatever the forge owns (the owner, 2026-10-26), and the
            // economy's tomes level every weapon at once. The hall arena keeps the legacy cards, so the scene tests that
            // pin the scene to the Descent simulation still measure the same engine.
            var arena = FindFirstObjectByType<ArenaView>();
            bool categories = arena != null && arena.IsFoundryRegion && _economy.TomeCount > 0;
            Weapon = categories ? HeroWeapon.CreateCategoryRuntime() : HeroWeapon.CreateRuntime();
            _attack.Initialize(Weapon);
            // Ember Memory (docs/45): every hit of every weapon the hero carries sets the struck enemy alight.
            if (Hero.Passive == HeroPassive.EmberMemory)
            {
                _burnNeighbourhood = new TargetingNeighbourhood(_hero.GetComponent<Targeting>());
                _attack.StruckWith += IgniteStruck;
            }
            Ability = Hero.Ability.CreateRuntime();
            _ability.Initialize(Ability);
            // A development file may choose the seed; otherwise a fresh one. Either way it is recorded on run_start.
            int seed = DevelopmentStart.TryRunSeed(out int chosen) ? chosen : RunSeeds.Fresh();
            Run = new RunState(_economy.ExperiencePerLevel, _economy.AtRiskGoldLoss, _economy.ExperienceGrowth, seed);
            _rewards = new RewardService(Run);
            Loot = new RunLoot(Run, ItemTemplates(), !Profile.Gear.FirstArmourGiven, _economy.AtRiskGoldLoss, Tier);
            // Created before any view subscribes to Run.Ended, so banked gold and loot reach the profile before results show.
            Bank = new RunBank(Run, Profile, Loot);
            // Right after the bank's deposit and before any view: the result is written once more, and ResultSaved says
            // whether that write landed (docs/58 3.1: no video before the run is saved).
            Run.Ended += SaveResult;
            Anvil = new Anvil(Profile);
            Profile.Changed += SaveProfile;
            // Nothing burns on behind the result screen (review, 2026-10-27).
            Run.Ended += BurnStatus.ExtinguishAll;
            RelicOption equipped = Relics.Equipped;
            if (equipped != null)
            {
                Relic = new RelicRuntime(equipped);
                _relicBehaviour.Initialize(Relic);
            }

            var options = new UpgradeOption[_upgrades.Length];
            for (int i = 0; i < _upgrades.Length; i++)
                options[i] = _upgrades[i].CreateOption();
            // The run's stat sheet, bound to the three things that read it. HeroMovementInput builds the walk and the
            // bar in its own Awake, which runs before this one, so both exist to bind now.
            var movement = _hero.GetComponent<HeroMovementInput>();
            HeroStats = new HeroStats(Hero.MaximumHealth,
                movement != null ? movement.Motion.Speed : 2.5f,
                movement != null ? movement.Stamina.Bar : HeroStamina.DefaultBar);
            HeroStats.Changed += ApplyHeroStats;
            // The density probe measures the phone with N enemies standing (development/horde-probe.txt); a hero that
            // falls in the first seconds would leave it measuring the result screen, so the probe's hero cannot fall.
            if (DevelopmentStart.HordeProbe() > 0)
                HeroStats.AddModifier(UpgradeStat.MaxHealth, new StatModifier(ModifierOperation.Flat, 1000000f));
            ApplyHeroStats();
            Weapon.Seed = Run.Seed;
            WeaponLoadout loadout = null;
            WeaponCard[] weaponCards = null;
            UpgradeOption[] tomes = null;
            if (categories)
            {
                loadout = new WeaponLoadout(Run.Seed);
                loadout.TryAdd(HeroWeapon.Id, Weapon);
                // Today's catalogue, or docs/63's roster behind its development file (RunCatalogue).
                WeaponDefinition[] catalogue = RunCatalogue();
                weaponCards = new WeaponCard[catalogue.Length];
                for (int i = 0; i < catalogue.Length; i++)
                    weaponCards[i] = catalogue[i].CreateCard();
                tomes = new UpgradeOption[_economy.TomeCount];
                for (int i = 0; i < tomes.Length; i++)
                    tomes[i] = _economy.TomeAt(i).CreateOption();
            }
            // docs/63 section 13's crowd-weapon guarantee goes with the roster's development file: while the hero holds no
            // crowd weapon and a slot is free, its new-weapon cards are crowd weapons (UpgradeService, the simulation's body).
            Upgrades = new UpgradeService(Run, Weapon, options,
                categories ? _economy.CategoryChoiceCount : _economy.UpgradeChoiceCount, HeroStats,
                movement != null ? movement.Stamina.Enabled : HeroStamina.CurrentRulesEnabled, loadout, weaponCards, tomes,
                categories && DevelopmentStart.WeaponRoster());
            // A weapon taken from an offer joins the hero's attack after the ones it already carries.
            Upgrades.WeaponAcquired += (card, runtime) =>
            {
                using (PerformanceMarkers.PickAddWeapon.Auto())
                    _attack.AddWeapon(runtime);
            };
            Forge = new ForgeService(Run, _hero);
            Checkpoint = new CheckpointService(Run);
            Choices = new RunChoices(Upgrades, Forge, Checkpoint);
            Pause = new RunPause(Run);
            Pause.Changed += ApplyPause;
            Choices.Changed += OnChoicesChanged;
            Drops = new GroundDrops(Run);
            // The gear the profile wears for this run's weapon (docs/54 section 20): that weapon's own basma and lines (per
            // weapon id) and the equipped armour, amulet and ring, through the one body the Descent simulation calls at the
            // same moment - the weapon's stats onto every weapon the hero holds or will hold, the hero's onto its sheet,
            // and the rules onto the shares, the heals, the ability and the drops. A profile without gear touches nothing,
            // and the run is today's. Its burn line is read where Ember Memory lights a burn (IgniteStruck).
            Gear = new GearLoadout(Profile.Gear.EquippedPieces(HeroWeapon.Id));
            GearStats.Apply(Gear, HeroStats, loadout, Weapon, _hero.State, Ability, Drops);
            _burnFactor = GearStats.BurnFactor(Gear);
            StartSkills(movement);
            // docs/57 5.2: the first run that carries a newly forged skill names it and says when to tap it, once per skill
            // and profile. Presentation only; nothing at all for a run with no such skill.
            gameObject.AddComponent<SkillToastView>().Initialize(this, _ability);
            // The map this run starts on (MapChoiceView): the one chosen before this load, if any - the scene reloads to
            // start deeper - or, with none chosen, the choice itself, which holds the first wave and the run's telemetry
            // until the player taps a map.
            StartFloor = _encounters.ResolveStartFloor(wanted);
            IsChoosingMap = wanted <= 0 && DevelopmentStart.OffersMapChoice();
            RunTelemetryBridge telemetry = AttachTelemetry(equipped);
            _telemetry = telemetry;
            telemetry.WatchDrops(Drops);
            gameObject.AddComponent<ExitSealView>().Initialize(this, _text);
            gameObject.AddComponent<BossRevealView>().Initialize(this, movement);
            gameObject.AddComponent<GroundDropView>().Initialize(Drops, Run, _hero.transform, _groundRewardSprite, _encounters, _groundExperienceSprite,
                _groundGoldSprite);
            Altars = gameObject.AddComponent<AltarService>();
            // After telemetry, so the choice is logged before Extract ends the run or Descend moves on.
            Checkpoint.Chosen += OnCheckpointChosen;
            _hero.Damaged += OnHeroDamaged;
            _hero.Died += OnHeroDied;

            // Subscribe before the first spawn so every defeated enemy reaches the reward service.
            _encounters.EnemyDefeated += OnEnemyDefeated;
            _encounters.EnemyRemoved += OnEnemyRemoved;
            _encounters.FloorCleared += OnFloorCleared;
            if (!IsChoosingMap)
                BeginRun();
            bool catchUp = StartFloor > 1;
            bool arrival = !catchUp && DevelopmentStart.GateArrival();
            _encounters.Initialize(Choices, Forge, () => HeroStats.Difficulty, Upgrades, Drops, arrival || IsChoosingMap || catchUp,
                Run.Seed, StartFloor, Tier);
            if (arrival) gameObject.AddComponent<GateArrivalView>().Initialize(this, movement);
            // docs/62 P0a (development/outworks.txt): an Outworks run's pads walk the hero, no blow lands mid-arc, each anvil
            // captured grants a level-up's offer as an altar does, and the phase is drawn. Nothing without the switch.
            if (_encounters.Leap != null)
            {
                if (movement != null)
                    movement.Leap = _encounters.Leap;
                _hero.Guard = new AirborneGuard(_encounters.Leap);
                // Logged before the grant, whose offer follows it in the run's log (as an altar's is).
                telemetry.WatchOutworks(_encounters);
                _encounters.AnvilCaptured += OnAnvilCaptured;
                gameObject.AddComponent<OutworksView>().Initialize(this, _hero.transform);
            }
            // After the encounter knows its floor.
            Altars.Initialize(this, _hero.transform);
            gameObject.AddComponent<AltarView>().Initialize(Altars, _text, _hero.transform);
            Pit = gameObject.AddComponent<PitEruptionService>();
            Pit.Initialize(this, _hero);
            gameObject.AddComponent<PitEruptionView>().Initialize(Pit);
            Warden = gameObject.AddComponent<WardenSlamService>();
            Warden.Initialize(this, _hero, _attack);
            Mirrors = gameObject.AddComponent<MirrorGuardService>();
            Mirrors.Initialize(this, _hero);
            Gloam = gameObject.AddComponent<GloamService>();
            Gloam.Initialize(this, _hero);
            // The foundry's elites as they stand, for every strike of the hero's (EliteRule: the Daggers' share on them).
            _attack.Elites = _encounters.Elites;
            // The hero strikes nothing while the boss's reveal holds the fight, as no enemy strikes it.
            _attack.FightHold = _encounters.FightHold;
            // Nor does its ability fire: the button stands greyed and a use is refused (AbilityController.FightHold).
            _ability.FightHold = _encounters.FightHold;
            gameObject.AddComponent<WardenSlamView>().Initialize(Warden);
            // The gate boss's body tells its blows, its phase two and its fall (docs/43 section 1): after the mark's view,
            // so the mark's windup is raised before the body's tell reads it. Presentation only.
            gameObject.AddComponent<BossFeelView>().Initialize(Warden);
            // ...and speaks its lines (docs/44 section 1.8, docs/64 stage 1): a short plate over it as it walks out, with the
            // first mark of each phase and as it falls, never over the mark. Presentation only.
            gameObject.AddComponent<BossBarkView>().Initialize(this, Warden);
            gameObject.AddComponent<MirrorGlintView>().Initialize(Mirrors, _encounters, _hero);
            gameObject.AddComponent<RunFeedbackView>().Initialize(Run, Upgrades, _hero, _runFeedbackArt);
            // The game-feel pass on the hero (docs/43 section 1, 2026-11-22): presentation only - it reads the attacker, the
            // hero's health, the run's level and the weapons picked up, and writes nothing to any of them.
            _hero.gameObject.AddComponent<HeroFeelView>().Initialize(_attack, _hero, Run, Upgrades);
            telemetry.WatchFoundry(Altars, Pit, () => HeroStats.Difficulty);
            if (movement != null && DevelopmentStart.Autopilot())
                gameObject.AddComponent<DevelopmentAutopilot>().Initialize(this, movement, _hero.transform);
            if (IsChoosingMap)
                gameObject.AddComponent<MapChoiceView>().Initialize(this, movement);
            // Last, with every view and service wired: a deeper start's level-ups open the level-up panel one after
            // another, and the first wave waits until the last is chosen (OnChoicesChanged).
            if (catchUp)
            {
                gameObject.AddComponent<CatchUpAutoPickView>().Initialize(this);
                CatchUpToStartFloor();
            }
        }

        // A deeper start's level-ups are still queued before its first wave: the level-up panel offers Auto-pick
        // (CatchUpAutoPickView). Never during a run's own level-ups.
        public bool IsCatchingUp => _firstWaveAfterChoices && Choices.IsOpen && Choices.Current.Kind == ChoiceKind.Upgrade &&
            Upgrades.CurrentOffer != null;

        // Takes every queued catch-up level-up as the careful autopilot takes its cards (AutopilotCohort.Careful.Pick, the
        // build's scoring; the first card on a legacy offer), from the run's own seeded offers, so the same seed and hero
        // pick the same build. Each through Choices like a tap, so telemetry logs it as a pick. Returns the cards' names in
        // the order taken; the last pick closes the panel and the first wave comes in (OnChoicesChanged).
        public List<string> AutoPickCatchUp()
        {
            var taken = new List<string>();
            for (int guard = 0; IsCatchingUp && guard < 1000; guard++)
            {
                UpgradeOffer offer = Upgrades.CurrentOffer;
                bool low = _hero.Current < _hero.Maximum * .5f;
                int slot = Upgrades.IsCategoryMode ? AutopilotCohort.Careful.Pick(offer, low, Upgrades) : 0;
                string name = offer.Choices[slot].DisplayName;
                if (!Choices.TrySelect(Choices.Current, slot))
                    break;
                taken.Add(name);
            }
            return taken;
        }

        // The run begins on a deeper map (DescentStart): the hero raised to the level it typically has arriving there,
        // every level-up of it left to choose before the first wave.
        private void CatchUpToStartFloor()
        {
            DescentStart.CatchUp(Run, StartFloor);
            if (Choices.IsOpen)
                _firstWaveAfterChoices = true;
            else if (!Run.HasEnded)
                _encounters.BeginFirstEncounter();
        }

        // The map the player tapped on the map choice (MapChoiceView), with the hero and the difficulty tier chosen beside
        // it: floor 1 with the scene's own hero and tier begins here and now, as every run began before the choice existed;
        // a deeper floor, another playable hero or another open tier reloads the scene with the choice (RunStartRequest),
        // and that load begins with that hero on that floor and tier (Awake) without the choice; so does a language other
        // than the one the scene loaded in (the map choice's toggle), so the run's words are all that language's. Once only,
        // and only while the choice is open. tier null keeps the scene's.
        public void StartOnMap(int floorNumber, HeroDefinition hero = null, DifficultyTier? tier = null)
        {
            bool deeper = floorNumber > DescentStart.FirstFloor && DescentStart.IsStart(floorNumber);
            DifficultyTier chosen = tier ?? Tier;
            // A tier not yet opened, or a deeper map not yet reached on the chosen tier (MapUnlock), starts nothing; the
            // choice stays open.
            if (!IsChoosingMap || _restarting || !IsTierOpen(chosen) || (deeper && !IsMapOpen(floorNumber, chosen)))
                return;
            IsChoosingMap = false;
            bool otherHero = hero != null && hero != Hero && IsHeroPlayable(hero);
            bool otherTier = chosen != Tier;
            if (deeper || otherHero || otherTier || LanguageChangedSinceLoad)
            {
                RunStartRequest.Set(deeper ? floorNumber : DescentStart.FirstFloor, otherHero ? hero.Id : null, chosen);
                RestartRun();
                return;
            }
            BeginRun();
            if (!Run.HasEnded)
                _encounters.BeginFirstEncounter();
        }

        // The run begins: its telemetry's run_start, and the result screen's video asked for ahead of need (docs/58 7.4).
        private void BeginRun()
        {
            _telemetry.Begin();
            MonetisationRuntime.BeginRun();
        }

        // The run's local event log. Attached before this root subscribes to a checkpoint choice, a kill or a floor clear,
        // so telemetry records each before the reaction to it (the run ending, gold paid). It only reads the run.
        private RunTelemetryBridge AttachTelemetry(RelicOption equippedRelic)
        {
            var context = new RunTelemetryContext
            {
                Session = TelemetryRuntime.SessionFor(TelemetryLocation.Resolve()),
                Run = Run,
                Pause = Pause,
                Choices = Choices,
                Upgrades = Upgrades,
                Forge = Forge,
                Checkpoint = Checkpoint,
                Relics = Relics,
                Loot = Loot,
                Anvil = Anvil,
                HeroId = Hero.Id,
                WeaponId = HeroWeapon.Id,
                RelicId = equippedRelic != null ? equippedRelic.Id : null,
                AbilityId = Hero.Ability.Id,
                // The skills the run carries in slots 2 and 3 (StartSkills ran first): run_start names them (docs/57 5.3).
                SkillTwoId = _ability.SkillAt(2) != null ? Hero.SkillAt(2).Id : null,
                SkillThreeId = _ability.SkillAt(3) != null ? Hero.SkillAt(3).Id : null,
                ForgeGold = Profile.Gold,
                DeepestFloorCleared = Profile.DeepestFloorCleared,
                Seed = Run.Seed,
                StartFloor = StartFloor,
                Gear = Gear,
                Tier = Tier,
                KillerId = () => KillerTelemetryId,
                HeroHealth = () => _hero.Current,
                HeroMaximumHealth = () => _hero.Maximum,
                Pulses = () => RunTelemetry.PulsesFired(Upgrades.Loadout, Weapon)
            };
            return RunTelemetryBridge.Attach(gameObject, context, _encounters, _ability);
        }

        // A reload rebuilds every runtime object from definitions and the saved profile, so nothing else carries over.
        public void RestartRun()
        {
            if (_restarting)
                return;

            _restarting = true;
            ResultSaved = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        private static bool AllPresent(UnityEngine.Object[] references)
        {
            if (references == null || references.Length == 0)
                return false;
            for (int i = 0; i < references.Length; i++)
            {
                if (references[i] == null)
                    return false;
            }
            return true;
        }

        private void ReportLoadProblems()
        {
            if (_profileStore.LastLoadWasUnsaved)
                Debug.LogWarning($"Profile: the profile this session could not save came over the reload; {_profileStore.Directory} " +
                    "still holds an older one.", this);
            if (_profileStore.LastLoadStatus != ProfileLoadStatus.Recovered && _profileStore.LastLoadStatus != ProfileLoadStatus.Reset &&
                _profileStore.LastLoadStatus != ProfileLoadStatus.NewerVersion)
                return;

            Debug.LogWarning($"Profile {_profileStore.LastLoadStatus} from {_profileStore.Directory}: " +
                string.Join("; ", _profileStore.LastLoadProblems), this);
        }

        // The player hears of the save's state (ProfileNoticeView): a newer build's profile on disk once a launch (this
        // session saves nothing); a profile that came over the reload unsaved is written again at once - room may have been
        // made since - and a write that fails again says so.
        private void TellProfileState()
        {
            if (_profileStore.IsReadOnly)
                ProfileNoticeView.TellNewerVersion(this, _profileStore.Directory);
            else if (_profileStore.LastLoadWasUnsaved)
                TrySaveProfile();
        }

        // Whether this run carries the hero's skill slot (docs/57 phase 4): skill 1 always; skills 2 and 3 once the Forge has
        // forged them for this hero (PlayerProfile.HasForgedSkill, after the first clears of floors 2 and 3), or where a
        // development file asks (DevelopmentStart.SkillSlots: "2" or "3", the Editor's measurement with the skills on and a
        // device session's; never in a release build).
        public bool CarriesSkill(int slot)
        {
            if (slot == 1)
                return true;
            if (Hero == null || !HeroSkillUnlock.IsForgeable(slot) || Hero.SkillAt(slot) == null)
                return false;
            return DevelopmentStart.SkillSlots() >= slot || Profile.HasForgedSkill(Hero.Id, slot);
        }

        // The hero the next run plays, as the forge's hero chooser has chosen it (the Forge's skills tab shows its skills).
        public HeroDefinition NextRunHero => ChooseHero();

        // A locked hero's unlock hint as the forge's chooser, the map choice and the arena show it: "Clear The Foundry" for
        // Ilya under the weapon roster's lever (docs/63 section 6: the clear alone unlocks her there, UnlockHeroesForTheFoundry),
        // the hero's own hint without it.
        public string UnlockHint(HeroDefinition hero) => HeroText.UnlockHint(hero, DevelopmentStart.WeaponRoster());

        // docs/57's skills 2 and 3, where the run carries them (CarriesSkill: forged at the Forge, or a development file).
        // Their cooldowns take the gear's line as the first's did (GearStats.Apply), and what they reach is handed over: the
        // guard's armour onto the hero's sheet, the first weapon the Rush's damage scales by, the stick and the dash, the
        // gate boss, the burns the glyph and the ward's strikers catch - as the simulation's run wires them.
        private void StartSkills(HeroMovementInput movement)
        {
            AbilityRuntime second = CarriesSkill(2) ? Hero.SkillAt(2).CreateRuntime() : null;
            AbilityRuntime third = CarriesSkill(3) ? Hero.SkillAt(3).CreateRuntime() : null;
            if (second == null && third == null)
                return;
            float cooldown = GearStats.Factor(Gear, GearStat.AbilityCooldown);
            if (cooldown != 1f)
            {
                second?.ShortenCooldown(cooldown);
                third?.ShortenCooldown(cooldown);
            }
            _ability.InitializeSkills(second, third);
            _ability.GuardArmor = armour => HeroStats.SetGuardArmor(armour);
            _ability.FirstWeapon = () => Weapon;
            _ability.IsGateBoss = enemy => enemy != null && ReferenceEquals(_encounters.GateBoss, enemy);
            if (movement != null)
            {
                _ability.Heading = () => movement.Steer;
                movement.Dash = _ability.Dash;
            }
            _burnNeighbourhood ??= new TargetingNeighbourhood(_hero.GetComponent<Targeting>());
            _ability.Ignite = (enemy, stacks) =>
                BurnStatus.On(enemy)?.Ignite(stacks, false, _hero, _burnNeighbourhood, _encounters.FightHold, _burnFactor);
            _hero.State.WardAbsorbed += (context, absorbed) =>
            {
                if (context.Source is Health striker && striker.IsAlive)
                    _ability.Ignite(striker, SkillRule.WardStacks);
            };
            // Said only when the development file forces the skills; a forged skill is the game working as it should.
            if (DevelopmentStart.SkillSlots() > 1)
                Debug.Log($"[Skills] the run carries skill 1{(second != null ? ", 2" : string.Empty)}{(third != null ? ", 3" : string.Empty)}" +
                    " (development/skills.txt)");
        }

        private void ApplyHeroStats()
        {
            using (PerformanceMarkers.PickHeroStats.Auto())
            {
                HeroMovementInput movement = _hero.GetComponent<HeroMovementInput>();
                HeroStatsBinding.Apply(HeroStats, _hero.State, movement != null ? movement.Motion : null,
                    movement != null ? movement.Stamina : null);
            }
        }

        private void SaveProfile() => TrySaveProfile();

        // The run ended: the profile is written once more, after the bank's deposit, and only a write that landed marks
        // the result saved.
        private void SaveResult() => ResultSaved = TrySaveProfile();

        // A failed write keeps the in-memory profile and the next change tries again; UnsavedProfile carries it over a scene
        // reload until a write lands (the store holds it), and the player is told (once a scene). While a newer build's
        // profile stands the store writes nothing (read only): false too, since the result is not on disk.
        private bool TrySaveProfile()
        {
            try
            {
                _profileStore.Save(Profile);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogError($"Saving the profile to {_profileStore.Directory} failed: {exception}", this);
                ProfileNoticeView.TellSaveFailed(this);
                return false;
            }
            return !_profileStore.IsReadOnly;
        }

        // A kill in the Descent drops its reward where the enemy fell; the finale pays it at once, as it was measured and
        // as the owner chose to keep it (docs/27, S6d).
        private void OnEnemyDefeated(Health enemy)
        {
            // The run's loot (docs/54): a gate boss's or an elite leader's drop, straight into the satchel - never on the
            // ground, so nothing the fight or the autopilot walks to changes.
            if (_encounters.IsGateBossOfWave(enemy))
                Loot.BossFelled(_encounters.FloorNumber);
            else if (_encounters.IsEliteLeaderOfWave(enemy))
                Loot.EliteFelled(_encounters.FloorNumber);
            // The enemy's own reward as the encounter spawned it: a horde enemy pays a share of its experience and no gold.
            int experience = _encounters.ExperienceRewardOf(enemy);
            int gold = _encounters.GoldRewardOf(enemy);
            if (_encounters.IsInSwarmRoom)
            {
                _rewards.TryAwardKill(enemy, experience, gold);
                return;
            }
            if (!_encounters.TryFloorPositionOf(enemy, out float x, out float y))
            {
                // Every enemy the Descent kills stands in its pack; one that does not is a bug, and paying it at once
                // keeps the run whole while the error says so.
                Debug.LogError($"{enemy.name} fell outside its pack; its reward was paid instead of dropped.", this);
                _rewards.TryAwardKill(enemy, experience, gold);
                return;
            }
            _rewards.TryDropKill(enemy, experience, gold, x, y, Drops);
        }

        // docs/62 P0a: a Tithe Anvil captured is a level-up's offer of cards, as an altar's is, without the altar's danger.
        private void OnAnvilCaptured(int anvil) => Run.GrantBonusUpgrade();

        // The encounter let go of the body and of its death: the kill paid, if it was one, cannot be reported again.
        private void OnEnemyRemoved(Health enemy) => _rewards.Forget(enemy);

        // Damaged fires before Died, so the killer is known when the defeat ends the run.
        private void OnHeroDamaged(DamageContext context)
        {
            if (Pit != null && Pit.IsBurningHero)
            {
                LastHurtByPit = true;
                return;
            }
            EnemyDefinition attacker = _encounters.DefinitionOf(context.Source);
            if (attacker != null)
            {
                LastAttacker = attacker;
                LastHurtByPit = false;
            }
        }

        private void OnHeroDied() => Run.End(RunOutcome.Defeat);

        // The hero the run plays (docs/45): the one chosen on the map choice for this load, then development/hero.txt, then
        // the profile's choice, each only when it may be played; otherwise the default. A choice the roster no longer has
        // falls back to the default.
        private HeroDefinition ChooseHero(string requested = null)
        {
            string wanted = requested;
            if (string.IsNullOrEmpty(wanted))
                wanted = DevelopmentStart.HeroId();
            if (string.IsNullOrEmpty(wanted))
                wanted = Profile.SelectedHeroId;
            if (!string.IsNullOrEmpty(wanted) && _heroes != null)
            {
                for (int i = 0; i < _heroes.Length; i++)
                {
                    HeroDefinition hero = _heroes[i];
                    if (hero != null && hero.Id == wanted && IsHeroPlayable(hero) && hero.Ability != null &&
                        hero.StartingWeapon != null && Array.IndexOf(_weapons, hero.StartingWeapon) >= 0)
                        return hero;
                }
            }
            return _heroDefinition;
        }

        // The catalogue's weapon development/start-weapon.txt names - the roster's shared weapons included, so a scene test
        // can start a run with one; null without the file, outside the Editor and a development build, or for an id the
        // catalogue lacks.
        private WeaponDefinition DevelopmentStartWeapon()
        {
            string id = DevelopmentStart.StartWeaponId();
            if (id == null)
                return null;
            WeaponDefinition[] all = AllWeapons();
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null && all[i].Id == id)
                    return all[i];
            return null;
        }

        // Every hero weapon the scene holds: _weapons, then the roster's shared weapons not already among them.
        private WeaponDefinition[] AllWeapons()
        {
            if (_rosterWeapons == null || _rosterWeapons.Length == 0)
                return _weapons;
            var all = new List<WeaponDefinition>(_weapons);
            for (int i = 0; i < _rosterWeapons.Length; i++)
                if (!all.Contains(_rosterWeapons[i]))
                    all.Add(_rosterWeapons[i]);
            return all.ToArray();
        }

        // The weapons this run's cards draw from (docs/63): _weapons as shipped; with development/weapon-roster.txt, every
        // hero weapon (AllWeapons) filtered by WeaponRoster - this run's own weapon, then every weapon no hero of the roster
        // starts with - the body the simulation's RosterCatalogue calls, over the same order.
        internal WeaponDefinition[] RunCatalogue()
        {
            if (!DevelopmentStart.WeaponRoster())
                return _weapons;
            WeaponDefinition[] all = AllWeapons();
            var ids = new string[all.Length];
            for (int i = 0; i < all.Length; i++)
                ids[i] = all[i].Id;
            List<int> kept = WeaponRoster.Catalogue(HeroWeapon.Id, ids, RosterSignatures());
            var catalogue = new WeaponDefinition[kept.Count];
            for (int i = 0; i < catalogue.Length; i++)
                catalogue[i] = all[kept[i]];
            return catalogue;
        }

        // The roster heroes' own weapons: the starting weapon of every hero the scene can play (Heroes), each once.
        internal string[] RosterSignatures()
        {
            var ids = new List<string>(Heroes.Count);
            for (int i = 0; i < Heroes.Count; i++)
            {
                WeaponDefinition own = Heroes[i] != null ? Heroes[i].StartingWeapon : null;
                if (own != null && !ids.Contains(own.Id))
                    ids.Add(own.Id);
            }
            return ids.ToArray();
        }

        // The weapon a hero would carry from this start (the map choice's hero cards, the forge's hero weapon, the Anvil's
        // weapon slot): this run's own for the scene's hero, its own starting weapon for another - every hero carries its
        // own (docs/63), whatever the profile recorded from the retired rack.
        public WeaponDefinition WeaponFor(HeroDefinition hero)
        {
            if (hero == null)
                return null;
            return hero == Hero ? HeroWeapon : hero.StartingWeapon;
        }

        // The weapon the next run's hero (NextRunHero, as the forge's hero chooser has chosen it) will carry: what the
        // forge shows and the Anvil's weapon slot works on between runs.
        public WeaponDefinition NextWeapon => WeaponFor(NextRunHero);

        // The Descent's items (EconomyConfig, docs/54 section 3.2) as the run's loot reads them.
        private ItemTemplate[] ItemTemplates()
        {
            var items = new List<ItemTemplate>(_economy.ItemCount);
            for (int i = 0; i < _economy.ItemCount; i++)
            {
                ItemDefinition item = _economy.ItemAt(i);
                if (item != null)
                    items.Add(item.CreateTemplate());
            }
            return items.ToArray();
        }

        // A piece's name for the Anvil's screens (docs/54 phase 4): a weapon's from the forge's rack, an item's from the
        // economy's list; the id itself for anything this build no longer has.
        public string ItemDisplayName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return string.Empty;
            for (int i = 0; _weapons != null && i < _weapons.Length; i++)
                if (_weapons[i] != null && _weapons[i].Id == itemId)
                    return _weapons[i].DisplayName;
            for (int i = 0; _economy != null && i < _economy.ItemCount; i++)
            {
                ItemDefinition item = _economy.ItemAt(i);
                if (item != null && item.Id == itemId)
                    return item.DisplayName;
            }
            return itemId;
        }

        // A dropped item's own painted icon (ItemDefinition.Icon) for the Anvil's screens; null for a weapon, for an id
        // this build no longer has and for an item without one (AnvilView then shows its slot's card icon).
        public Sprite ItemIcon(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return null;
            for (int i = 0; _economy != null && i < _economy.ItemCount; i++)
            {
                ItemDefinition item = _economy.ItemAt(i);
                if (item != null && item.Id == itemId)
                    return item.Icon;
            }
            return null;
        }

        // The game's text, for views built in code.
        internal PrototypeTextDefinition Text => _text;

        // Open from the start, unlocked by play, or any hero on a development build so the owner can try one early.
        public bool IsHeroPlayable(HeroDefinition hero) => HeroUnlock.IsPlayable(hero != null && hero.UnlockedFromStart,
            hero != null && Profile.HasUnlockedHero(hero.Id), DevelopmentStart.AllHeroesOpen());

        // Whether the map choice may start a run on this floor on this run's tier: the Foundry always, a deeper map once a
        // run on the tier has cleared the floor above it (MapUnlock, docs/53; each tier its own record, docs/54 section 21).
        // Unlike a hero, a development build gets no exception here: the development files (start-floor.txt) start deeper
        // without the panel.
        public bool IsMapOpen(int floorNumber) => IsMapOpen(floorNumber, Tier);

        public bool IsMapOpen(int floorNumber, DifficultyTier tier) => Profile.Tiers.IsMapOpen(floorNumber, tier);

        // Whether a run may be played on this tier: Forged always, another once the Descent is completed on the tier below
        // it. No development exception, as for the maps.
        public bool IsTierOpen(DifficultyTier tier) => Profile.Tiers.IsOpen(tier);

        // The gear power a hero would wear into a run from this start (GearLoadout.Power, hundredths of a percent): the
        // weapon it would carry (WeaponFor) with its own basma and lines, and the equipped armour, amulet and ring.
        public int GearPowerFor(HeroDefinition hero)
        {
            WeaponDefinition weapon = WeaponFor(hero);
            return weapon != null ? new GearLoadout(Profile.Gear.EquippedPieces(weapon.Id)).Power : 0;
        }

        // The Foundry cleared: every hero unlocked by carrying a weapon through it unlocks when this run carried it - or,
        // when this run's cards could never have handed it over (HeroUnlock.UnlocksOnFoundryClear: under the weapon roster
        // another hero's own weapon is never offered, docs/63), on the clear itself.
        internal void UnlockHeroesForTheFoundry()
        {
            if (_heroes == null)
                return;
            for (int i = 0; i < _heroes.Length; i++)
            {
                HeroDefinition hero = _heroes[i];
                if (hero == null || hero.UnlockedFromStart || hero.UnlockWeapon == null || Profile.HasUnlockedHero(hero.Id))
                    continue;
                string weaponId = hero.UnlockWeapon.Id;
                bool carried = HeroWeapon == hero.UnlockWeapon ||
                    (Upgrades.Loadout != null && Upgrades.Loadout.IndexOf(weaponId) >= 0);
                if (HeroUnlock.UnlocksOnFoundryClear(carried, CardsCanOffer(weaponId)))
                    Profile.UnlockHero(hero.Id);
            }
        }

        // Whether this run's cards could hand the hero this weapon: in category mode, whether the run's catalogue holds it;
        // in the legacy engine (the hall arena), which hands over no weapon, true, so a weapon there must be carried.
        private bool CardsCanOffer(string weaponId)
        {
            IReadOnlyList<WeaponCard> cards = Upgrades.WeaponCards;
            if (cards == null)
                return true;
            for (int i = 0; i < cards.Count; i++)
                if (cards[i].Id == weaponId)
                    return true;
            return false;
        }

        // Ember Memory: one direct Burn stack on the enemy each hit lands on; its fall then passes the burn on.
        private void IgniteStruck(Health target, WeaponRuntime weapon) =>
            BurnStatus.On(target)?.Ignite(1, false, _hero, _burnNeighbourhood, _encounters.FightHold, _burnFactor);

        // For tests: the neighbourhood a burn's fall asks.
        internal INeighbourhood BurnNeighbourhood => _burnNeighbourhood;

        // The final floor ends the Descent; every earlier floor offers Extract or Descend.
        private void OnFloorCleared()
        {
            // Whatever still lies on the floor flies to the hero before anything else happens, so the floor's gold is
            // whole when the checkpoint shows it and its level-ups are offered before the checkpoint opens.
            Drops.CollectAll();
            Profile.RecordFloorCleared(_encounters.FloorNumber, Tier);
            Loot.FloorCleared(_encounters.FloorNumber);
            if (_encounters.FloorNumber == 1)
                UnlockHeroesForTheFoundry();
            if (!_encounters.HasNextFloor)
            {
                Loot.WalkedOut(_encounters.FloorNumber);
                // The Descent walked out of on this tier - its finale, not a development or proof floor that ends a run
                // alone: the tier above it opens (ProfileTiers).
                if (_encounters.FloorNumber >= DescentStart.FinaleFloor)
                    Profile.RecordDescentCompleted(Tier);
                Run.End(RunOutcome.Victory);
                return;
            }

            FloorDefinition next = _encounters.Floor.NextFloor;
            string modifier = next.Modifier != null ? next.Modifier.Description : _text.NoFloorModifier;
            Checkpoint.Open(CheckpointOptions(_text.ExtractName, _text.ExtractDescriptionFormat, _text.DescendName,
                _text.DescendDescriptionFormat, Run.Gold, next.DisplayName, modifier));
        }

        // Extract and Descend with the run's gold and the next floor in their text. Their numbers are written in the
        // language the game speaks (Loc.Numbers, docs/60), never the phone's: a Turkish phone wrote "0,45" in English
        // (CultureTextTests).
        public static CheckpointOption[] CheckpointOptions(string extractName, string extractFormat, string descendName,
            string descendFormat, int gold, string nextFloorName, string modifier) =>
            new[]
            {
                new CheckpointOption(CheckpointKind.Extract, extractName,
                    string.Format(Loc.Numbers, extractFormat, gold)),
                new CheckpointOption(CheckpointKind.Descend, descendName,
                    string.Format(Loc.Numbers, descendFormat, gold, nextFloorName, modifier))
            };

        private void OnCheckpointChosen(CheckpointKind kind)
        {
            if (kind == CheckpointKind.Extract)
            {
                Run.End(RunOutcome.Extracted);
                return;
            }

            Run.SecureGold();
            Loot.Secure();
            _encounters.DescendToNextFloor();
        }

        private void OnChoicesChanged()
        {
            using (PerformanceMarkers.PickPause.Auto())
                Pause.SetChoiceOpen(Choices.IsOpen);
            // A deeper start's last level-up chosen: its first wave comes in (CatchUpToStartFloor).
            if (_firstWaveAfterChoices && !Choices.IsOpen)
            {
                _firstWaveAfterChoices = false;
                if (!Run.HasEnded)
                    _encounters.BeginFirstEncounter();
            }
        }

        // Leaving the app (a call, the home button) pauses the run so it waits for the player on return.
        private void OnApplicationPause(bool paused)
        {
            if (paused && Pause != null)
                Pause.TryPause();
        }

        // Scaled time freezes combat cadence while the run is frozen; uGUI input runs on unscaled time.
        private void ApplyPause()
        {
            bool frozen = Pause.IsFrozen;
            if (frozen == _timeFrozen)
                return;

            _timeFrozen = frozen;
            Time.timeScale = frozen ? 0f : 1f;
        }

        private void OnDestroy()
        {
            if (_hero != null)
            {
                _hero.Damaged -= OnHeroDamaged;
                _hero.Died -= OnHeroDied;
            }
            if (_encounters != null)
            {
                _encounters.EnemyDefeated -= OnEnemyDefeated;
                _encounters.EnemyRemoved -= OnEnemyRemoved;
                _encounters.FloorCleared -= OnFloorCleared;
            }
            if (Checkpoint != null)
                Checkpoint.Chosen -= OnCheckpointChosen;
            if (Choices != null)
                Choices.Changed -= OnChoicesChanged;
            if (Pause != null)
                Pause.Changed -= ApplyPause;
            if (Profile != null)
                Profile.Changed -= SaveProfile;
            if (_timeFrozen)
                Time.timeScale = 1f;
        }
    }
}
