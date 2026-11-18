using System;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Analytics;
using Cryptforge.Arena;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // P0's arena on the phone (docs/55 sections 6, 10.9-10.10 and 11): one overlay canvas of four pages, built in code like
    // the Anvil (AnvilView), opened with ArenaScreen.Open(setup) and closed with Close().
    //
    //   - Build: the hero (Ilya only once the run would let the player take it: CombatSetup.IsHeroPlayable), the weapon
    //     type (one the hero carries and the profile owns), three distinct efsun line types of the hero's ranked pool
    //     (ArenaRules.RankedPool, each at the ranked value both sides fight at), and the autopilot, careful or bold. The
    //     builds are ArenaProfileBuild's, read from the profile and never written to it.
    //   - Ladder: the training ladder - the player's rung and training rating, three training rivals (or past heroes) near
    //     it, a Fight button - or the open arena's three offers at 2 % below, level and 2 % above the player's gear power,
    //     each card with both powers and the expected chance (docs/55 10.10).
    //   - Playback: the duel drawn from its log (DuelPlaybackView), x1 or x2, Skip.
    //   - Result: the winner, the rounds, the rating's change and the summary's two or three effects (DuelSummary through
    //     ArenaText.Why, by DuelSummary.Id's effect), Watch again and Next fight.
    //
    // The save is the arena's own (ArenaSaveStore beside the profile: Load, then ArenaLadder's Opponents, Fight with a
    // DuelLog, KeepSnapshot when the build changes, Save after each change); each fight writes arena_fight on the launch's
    // telemetry session (ArenaTelemetryWriter). The run underneath is not touched: the map choice already holds it, and the
    // canvas, above the map choice's, takes every touch. Its words are the language's (ArenaText) and follow a change.
    public sealed class ArenaScreen : MonoBehaviour
    {
        public enum Page
        {
            Build = 0,
            Ladder = 1,
            Playback = 2,
            Result = 3
        }

        public const string CanvasName = "Arena Screen";
        // Above the map choice it is opened from (MapChoiceView.SortingOrder, 100).
        public const int SortingOrder = 110;
        public const float InputDelay = .2f;
        public const float SurfaceWidth = 1000f;
        public const float SurfaceHeight = 1800f;
        private const float FitMargin = 12f;
        private const int Cards = ArenaLadder.Offered;
        private const int MaxWeapons = 3;
        private const int MaxLines = 6;

        private static readonly ArenaStat[] DefaultLines = { ArenaStat.Damage, ArenaStat.MaxHealth, ArenaStat.Armour };

        private CombatSetup _setup;
        private Action _closed;
        private ArenaUi _ui;
        private RectTransform _safe;
        private RectTransform _surface;
        private readonly GameObject[] _pages = new GameObject[4];
        private float _inputEnabledAt;
        private bool _subscribed;
        private bool _closing;

        // The save.
        private ArenaSaveStore _store;
        private ArenaSaveData _save;

        // The build.
        private string _heroId;
        private string _weaponId;
        private ArenaPolicy _policy;
        private readonly List<ArenaStat> _lines = new List<ArenaStat>(3);
        private readonly List<string> _heroIds = new List<string>();
        private readonly List<string> _weaponIds = new List<string>(MaxWeapons);
        private readonly List<ArenaStat> _pool = new List<ArenaStat>(MaxLines);
        private readonly Choice[] _heroCards = new Choice[2];
        private readonly Choice[] _weaponChips = new Choice[MaxWeapons];
        private readonly Choice[] _lineChips = new Choice[MaxLines];
        private readonly Choice[] _policyChips = new Choice[2];
        private Text _linesHeading;
        private Button _continue;

        // The ladder.
        private ArenaMode _mode;
        private readonly Button[] _tabs = new Button[2];
        private Text _status;
        private Text _record;
        private Text _buildSummary;
        private Text _ladderNote;
        private readonly Choice[] _rivalCards = new Choice[Cards];
        private Button _fight;
        private List<int> _opponents = new List<int>();
        private List<ArenaOpenOffer> _offers = new List<ArenaOpenOffer>();
        private int _selected = 1;

        // The playback and the result.
        private DuelPlaybackView _playback;
        private ArenaFightOutcome _outcome;
        private DuelLog _log;
        private string _rivalName;
        private Text _resultTitle;
        private Text _resultAgainst;
        private Text _resultRounds;
        private readonly Image[] _roundPips = new Image[DuelRules.MaxRounds];
        private Text _resultRating;
        private Text _whyHeading;
        private readonly Image[] _whyCards = new Image[DuelSummary.MaxLines];
        private readonly Text[] _whyLines = new Text[DuelSummary.MaxLines];
        private Button _again;
        private Button _next;

        private sealed class Choice
        {
            public Button Button;
            public Text Label;
            public Image Picture;
        }

        public static ArenaScreen Current { get; private set; }

        public Page Shown { get; private set; }
        public ArenaMode Mode => _mode;
        public bool IsOpen => this != null && gameObject.activeSelf && !_closing;
        public GameObject Panel => _surface != null ? _surface.gameObject : null;
        public ArenaSaveData SaveData => _save;
        public string HeroId => _heroId;
        public string WeaponId => _weaponId;
        public ArenaPolicy Policy => _policy;
        public IReadOnlyList<ArenaStat> Lines => _lines;
        public IReadOnlyList<string> Heroes => _heroIds;
        public bool CanContinue => _continue != null && _continue.interactable;
        public IReadOnlyList<int> Opponents => _opponents;
        public IReadOnlyList<ArenaOpenOffer> Offers => _offers;
        public int Selected => _selected;
        public ArenaFightOutcome LastOutcome => _outcome;
        public DuelLog LastLog => _log;
        internal DuelPlaybackView Playback => _playback;
        public Button ContinueButton => _continue;
        public Button FightButton => _fight;
        public Button AgainButton => _again;
        public Button NextButton => _next;
        public Button TabButton(ArenaMode mode) => _tabs[(int)mode];
        public Button HeroButton(int index) => _heroCards[index].Button;
        public Button LineButton(int index) => _lineChips[index].Button;
        public Button CardButton(int index) => _rivalCards[index].Button;
        public string CardText(int index) => _rivalCards[index].Label.text;
        public string StatusText => _status.text + "\n" + _record.text;
        public string ResultTitle => _resultTitle.text;
        public string RatingText => _resultRating.text;
        public string WhyText
        {
            get
            {
                var lines = new List<string>(DuelSummary.MaxLines);
                foreach (Text line in _whyLines)
                {
                    if (line.gameObject.activeSelf && line.text.Length > 0)
                        lines.Add(line.text);
                }
                return string.Join("\n", lines);
            }
        }

        // Opens the arena over the scene of the setup (the map choice's button, docs/55 section 11; the development entry,
        // ArenaDevelopmentEntry). closed runs once the player closes it. One screen at a time: an open one closes first.
        public static ArenaScreen Open(CombatSetup setup, Action closed = null, ArenaMode mode = ArenaMode.Ranked)
        {
            if (setup == null || setup.Profile == null)
                return null;
            if (Current != null)
                Current.Close();
            var host = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            host.layer = LayerMask.NameToLayer("UI") >= 0 ? LayerMask.NameToLayer("UI") : 0;
            if (setup.gameObject.scene.IsValid() && host.scene != setup.gameObject.scene)
                SceneManager.MoveGameObjectToScene(host, setup.gameObject.scene);
            var screen = host.AddComponent<ArenaScreen>();
            Current = screen;
            screen.Initialize(setup, closed, mode);
            return screen;
        }

        public void Close()
        {
            if (_closing)
                return;
            _closing = true;
            Action closed = _closed;
            _closed = null;
            Destroy(gameObject);
            closed?.Invoke();
        }

        private void Initialize(CombatSetup setup, Action closed, ArenaMode mode)
        {
            _setup = setup;
            _closed = closed;
            _mode = mode;
            var screens = FindFirstObjectByType<ScreenArtView>(FindObjectsInactive.Include);
            _ui = new ArenaUi(null, screens != null ? screens.Art : null);
            BuildCanvas();
            LoadSave();
            RestoreBuild();
            Loc.Changed += OnLanguageChanged;
            _subscribed = true;
            Show(_save.rankedBuild == null ? Page.Build : Page.Ladder);
        }

        // ---- The save ----

        private void LoadSave()
        {
            _store = new ArenaSaveStore(ProfileLocation.Resolve());
            _save = _store.Load(NewSeed());
            foreach (string problem in _store.LastLoadProblems)
                Debug.LogWarning("[Arena] " + problem);
        }

        // A new arena's seed from the clock, never 0.
        private static int NewSeed()
        {
            long ticks = DateTime.UtcNow.Ticks;
            int seed = (int)(ticks ^ (ticks >> 32)) & int.MaxValue;
            return seed != 0 ? seed : 1;
        }

        private void SaveNow()
        {
            try
            {
                _store.Save(_save);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning("[Arena] the arena save could not be written: " + exception.Message);
            }
        }

        // ---- The build ----

        private void RestoreBuild()
        {
            _heroIds.Clear();
            foreach (ArenaHero hero in ArenaRules.Current.Heroes)
                _heroIds.Add(hero.Id);
            ArenaSnapshotData stored = _save.rankedBuild;
            string hero0 = stored != null && Playable(stored.heroId) ? stored.heroId : null;
            if (hero0 == null)
                hero0 = Playable(_setup.Profile.SelectedHeroId) ? _setup.Profile.SelectedHeroId : ArenaRules.Vanguard;
            _heroId = hero0;
            _weaponId = ArenaProfileBuild.WeaponOf(_setup.Profile, _heroId, stored != null ? stored.weaponId : null);
            _policy = stored != null && stored.policy == "bold" ? ArenaPolicy.Bold : ArenaPolicy.Careful;
            _lines.Clear();
            List<ArenaStat> pool = ArenaRules.Current.RankedPool(ArenaRules.Current.Hero(_heroId));
            if (stored != null && stored.lineIds != null)
            {
                foreach (string id in stored.lineIds)
                {
                    if (ArenaGearTable.TryParse(id, out ArenaStat stat) && pool.Contains(stat) && !_lines.Contains(stat))
                        _lines.Add(stat);
                }
            }
            if (_lines.Count != ArenaRules.Current.RankedLines)
            {
                _lines.Clear();
                foreach (ArenaStat stat in DefaultLines)
                {
                    if (pool.Contains(stat))
                        _lines.Add(stat);
                }
            }
        }

        private HeroDefinition Definition(string heroId)
        {
            IReadOnlyList<HeroDefinition> heroes = _setup.Heroes;
            for (int i = 0; heroes != null && i < heroes.Count; i++)
            {
                if (heroes[i] != null && heroes[i].Id == heroId)
                    return heroes[i];
            }
            return null;
        }

        // A hero the arena knows that the run would let the player take (the same check as the run's hero choice).
        private bool Playable(string heroId)
        {
            if (string.IsNullOrEmpty(heroId) || ArenaRules.Current.Hero(heroId) == null)
                return false;
            HeroDefinition hero = Definition(heroId);
            return hero != null ? _setup.IsHeroPlayable(hero) : heroId == ArenaRules.Vanguard;
        }

        private bool Owns(string weaponId)
        {
            ArenaHero hero = ArenaRules.Current.Hero(_heroId);
            return hero != null && hero.Carries(weaponId) &&
                   (weaponId == hero.StartingWeaponId || _setup.Profile.OwnsWeapon(weaponId));
        }

        public ArenaBuild RankedBuild() => ArenaProfileBuild.Ranked(_setup.Profile, _heroId, _lines, _policy, _weaponId);

        public ArenaBuild OpenBuild() => ArenaProfileBuild.Open(_setup.Profile, _heroId, _policy, _weaponId);

        public void PickHero(string heroId)
        {
            if (!Playable(heroId))
                return;
            _heroId = heroId;
            _weaponId = ArenaProfileBuild.WeaponOf(_setup.Profile, heroId, _weaponId);
            List<ArenaStat> pool = ArenaRules.Current.RankedPool(ArenaRules.Current.Hero(heroId));
            _lines.RemoveAll(stat => !pool.Contains(stat));
            Refresh();
        }

        public void PickWeapon(string weaponId)
        {
            if (!Owns(weaponId))
                return;
            _weaponId = weaponId;
            Refresh();
        }

        // A line type picked or put back; a fourth pick replaces the earliest of the three.
        public void ToggleLine(ArenaStat stat)
        {
            if (_lines.Remove(stat))
            {
                Refresh();
                return;
            }
            if (!ArenaRules.Current.InRankedPool(stat, ArenaRules.Current.Hero(_heroId)))
                return;
            if (_lines.Count >= ArenaRules.Current.RankedLines)
                _lines.RemoveAt(0);
            _lines.Add(stat);
            Refresh();
        }

        public void PickPolicy(ArenaPolicy policy)
        {
            _policy = policy;
            Refresh();
        }

        // The build chosen: kept in the save for both arenas. A build that changed after it fought in its arena is kept as a
        // past hero first, at the rating it reached there (ArenaLadder.KeepSnapshot), so the player meets it on that
        // ladder later; one that never fought is simply replaced.
        public void ConfirmBuild()
        {
            if (!BuildReady())
                return;
            Keep(RankedBuild(), ref _save.rankedBuild);
            Keep(OpenBuild(), ref _save.openBuild);
            SaveNow();
            Show(Page.Ladder);
        }

        private void Keep(ArenaBuild build, ref ArenaSnapshotData stored)
        {
            string hash = ArenaHash.Hex(build.Hash);
            if (stored != null && stored.hash == hash)
                return;
            int since = stored != null ? stored.fight : 0;
            if (stored != null && _save.Ladder(build.Mode).history.Exists(match => match.fight >= since))
            {
                ArenaBuild previous = ArenaSnapshots.ToBuild(stored);
                if (previous != null)
                    ArenaLadder.KeepSnapshot(_save, previous);
            }
            stored = ArenaSnapshots.ToData(build, _save.Ladder(build.Mode).rating, _save.fights);
        }

        private bool BuildReady() =>
            Playable(_heroId) && Owns(_weaponId) && _lines.Count == ArenaRules.Current.RankedLines;

        // ---- The ladder ----

        public void ShowMode(ArenaMode mode)
        {
            _mode = mode;
            _selected = 1;
            Show(Page.Ladder);
        }

        public void Select(int card)
        {
            int count = _mode == ArenaMode.Ranked ? _opponents.Count : _offers.Count;
            if (card < 0 || card >= count)
                return;
            _selected = card;
            Refresh();
        }

        // The chosen rival fought now (a best of three in a fraction of a millisecond), the save written and the event sent,
        // then the duel played from its log.
        public void FightSelected()
        {
            var log = new DuelLog();
            ArenaFightOutcome outcome;
            if (_mode == ArenaMode.Ranked)
            {
                if (_selected < 0 || _selected >= _opponents.Count)
                    return;
                outcome = ArenaLadder.Fight(_save, ArenaMode.Ranked, _opponents[_selected], RankedBuild(), null, log);
                _rivalName = ArenaText.RivalName(outcome.Rival);
            }
            else
            {
                if (_selected < 0 || _selected >= _offers.Count)
                    return;
                outcome = ArenaLadder.FightOffer(_save, _offers[_selected], OpenBuild(), null, log);
                _rivalName = outcome.Offer.Name;
            }
            SaveNow();
            ArenaTelemetryWriter.Record(outcome);
            _outcome = outcome;
            _log = log;
            StartPlayback();
        }

        // ---- The playback and the result ----

        private void StartPlayback()
        {
            string[] names = { ArenaText.You, _rivalName };
            ArenaFighter[] fighters = { _outcome.Player, _outcome.Foe };
            var identities = new string[2];
            var weapons = new string[2];
            var art = new DuelSideArt[2];
            for (int side = 0; side < 2; side++)
            {
                ArenaFighter f = fighters[side];
                HeroDefinition hero = Definition(f.Hero.Id);
                identities[side] = ArenaText.HeroName(f.Hero.Id, hero != null ? hero.DisplayName : null) + "  " + ArenaText.WeaponName(f.Weapon.Id);
                weapons[side] = f.Weapon.Id;
                art[side] = ArtFor(hero);
            }
            Show(Page.Playback);
            _playback.Play(_log, names, identities, weapons, art);
        }

        // The run's own art for a hero: its rendered set (Ilya), else the painted Vanguard the scene's hero carries.
        private static DuelSideArt ArtFor(HeroDefinition hero)
        {
            if (hero != null && hero.RenderedArt != null && hero.RenderedArt.IsValid)
                return new DuelSideArt(null, hero.RenderedArt);
            var look = FindFirstObjectByType<HeroLookView>(FindObjectsInactive.Include);
            return new DuelSideArt(look != null ? look.PaintedArt : null, null);
        }

        public void SkipPlayback()
        {
            if (Shown != Page.Playback)
                return;
            _playback.Skip();
            Show(Page.Result);
        }

        public void WatchAgain()
        {
            if (_log == null)
                return;
            Show(Page.Playback);
            _playback.Restart();
        }

        private void Update()
        {
            FitToSafeArea();
            if (Shown != Page.Playback || _playback == null)
                return;
            _playback.Tick(Time.unscaledDeltaTime);
            if (_playback.Finished)
                Show(Page.Result);
        }

        // ---- Pages ----

        public void Show(Page page)
        {
            Shown = page;
            for (int i = 0; i < _pages.Length; i++)
                _pages[i].SetActive(i == (int)page);
            _inputEnabledAt = Time.unscaledTime + InputDelay;
            Refresh();
        }

        private bool Ready() => IsOpen && Time.unscaledTime >= _inputEnabledAt;

        private void OnLanguageChanged()
        {
            Refresh();
            _playback?.Relabel();
        }

        private void Refresh()
        {
            switch (Shown)
            {
                case Page.Build:
                    RefreshBuild();
                    break;
                case Page.Ladder:
                    RefreshLadder();
                    break;
                case Page.Result:
                    RefreshResult();
                    break;
            }
        }

        private void RefreshBuild()
        {
            ArenaRules rules = ArenaRules.Current;
            for (int i = 0; i < _heroCards.Length; i++)
            {
                Choice card = _heroCards[i];
                bool shown = i < _heroIds.Count;
                card.Button.gameObject.SetActive(shown);
                if (!shown)
                    continue;
                string id = _heroIds[i];
                HeroDefinition hero = Definition(id);
                bool playable = Playable(id);
                string name = ArenaText.HeroName(id, hero != null ? hero.DisplayName : null);
                string second = playable ? (hero != null ? HeroText.Subtitle(hero) : string.Empty)
                    : ArenaText.Locked + (hero != null && hero.UnlockHint.Length > 0 ? ": " + HeroText.UnlockHint(hero) : string.Empty);
                card.Label.text = name + (second.Length > 0 ? "\n<size=26>" + second + "</size>" : string.Empty);
                card.Picture.sprite = hero != null ? hero.Portrait : null;
                card.Picture.enabled = card.Picture.sprite != null;
                card.Picture.color = playable ? Color.white : new Color(.35f, .35f, .4f, 1f);
                card.Button.interactable = playable;
                _ui.Paint(card.Button, id == _heroId);
            }

            ArenaHero chosen = rules.Hero(_heroId);
            _weaponIds.Clear();
            if (chosen != null)
                _weaponIds.AddRange(chosen.WeaponIds);
            for (int i = 0; i < _weaponChips.Length; i++)
            {
                Choice chip = _weaponChips[i];
                bool shown = i < _weaponIds.Count;
                chip.Button.gameObject.SetActive(shown);
                if (!shown)
                    continue;
                string id = _weaponIds[i];
                bool owned = Owns(id);
                chip.Label.text = ArenaText.WeaponName(id) + (owned ? string.Empty : "\n<size=24>" + ArenaText.NotOwned + "</size>");
                chip.Button.interactable = owned;
                _ui.Paint(chip.Button, id == _weaponId);
            }

            _pool.Clear();
            _pool.AddRange(rules.RankedPool(chosen));
            _linesHeading.text = ArenaText.LinesHeading(_lines.Count, rules.RankedLines);
            for (int i = 0; i < _lineChips.Length; i++)
            {
                Choice chip = _lineChips[i];
                bool shown = i < _pool.Count;
                chip.Button.gameObject.SetActive(shown);
                if (!shown)
                    continue;
                ArenaStat stat = _pool[i];
                chip.Label.text = ArenaText.Line(stat, rules.RankedLineValue(stat));
                _ui.Paint(chip.Button, _lines.Contains(stat));
            }

            for (int i = 0; i < _policyChips.Length; i++)
            {
                var policy = (ArenaPolicy)i;
                _policyChips[i].Label.text = ArenaText.Policy(policy) + "\n<size=26>" + ArenaText.PolicyNote(policy) + "</size>";
                _ui.Paint(_policyChips[i].Button, policy == _policy);
            }
            _continue.interactable = BuildReady();
            _ui.Paint(_continue, true);
        }

        private void RefreshLadder()
        {
            ArenaRules rules = ArenaRules.Current;
            for (int i = 0; i < _tabs.Length; i++)
                _ui.Paint(_tabs[i], (int)_mode == i);
            ArenaLadderData ladder = _save.Ladder(_mode);
            ArenaBuild ranked = RankedBuild();
            if (_mode == ArenaMode.Ranked)
            {
                _status.text = ArenaText.Rung(ArenaText.RungOf(ladder.rating), ArenaLadder.Size) + "     " + ArenaText.Rating(ArenaMode.Ranked, ladder.rating);
                _ladderNote.text = ArenaText.TrainingNote;
                _opponents = ArenaLadder.Opponents(ladder);
            }
            else
            {
                _offers = ArenaLadder.OpenOffers(_save, OpenBuild());
                int power = _offers.Count > 0 ? _offers[0].PlayerPower : 0;
                _status.text = ArenaText.Rating(ArenaMode.Open, ladder.rating) + "     " + ArenaText.Gear(power);
                _ladderNote.text = ArenaText.OpenNote;
            }
            _record.text = ArenaText.Record(ladder.wins, ladder.losses, ladder.draws);

            var lines = new List<string>(ranked.LineTypes.Count);
            foreach (ArenaStat stat in ranked.LineTypes)
                lines.Add(ArenaText.StatName(stat));
            HeroDefinition hero = Definition(_heroId);
            _buildSummary.text = ArenaText.HeroName(_heroId, hero != null ? hero.DisplayName : null) + "  " + ArenaText.WeaponName(_weaponId) +
                                 "  " + ArenaText.Policy(_policy) + "\n<size=26>" +
                                 (_mode == ArenaMode.Ranked ? string.Join(", ", lines) : ArenaText.GearCounts) + "</size>";

            int count = _mode == ArenaMode.Ranked ? _opponents.Count : _offers.Count;
            if (_selected >= count)
                _selected = count - 1;
            for (int i = 0; i < _rivalCards.Length; i++)
            {
                Choice card = _rivalCards[i];
                bool shown = i < count;
                card.Button.gameObject.SetActive(shown);
                if (!shown)
                    continue;
                if (_mode == ArenaMode.Ranked)
                {
                    ArenaRivalData rival = ladder.rivals[_opponents[i]];
                    string tag = rival.pastSelf ? ArenaText.PastHero : ArenaLadder.IsTrainingRival(ladder, rival) ? ArenaText.TrainingRival : string.Empty;
                    card.Label.text = ArenaUi.Colour(ArenaText.RivalName(rival), ArenaUi.Bright) + "   " + ArenaUi.Colour(tag, ArenaUi.Gold) +
                                      "\n<size=30>" + Identity(ArenaSnapshots.ToBuild(rival.build)) + "</size>" +
                                      "\n<size=30>" + ArenaText.RivalRating(rival.rating) + "</size>";
                }
                else
                {
                    ArenaOpenOffer offer = _offers[i];
                    card.Label.text = ArenaUi.Colour(offer.Name, ArenaUi.Bright) + "   " +
                                      ArenaUi.Colour(ArenaText.Offer(offer.Kind, ArenaLadder.OfferGap), ArenaUi.Gold) +
                                      "\n<size=30>" + Identity(offer.Build) + "</size>" +
                                      "\n<size=30>" + ArenaText.Powers(offer.PlayerPower, offer.RivalPower) + "    " +
                                      ArenaText.Chance(offer.ExpectedPermille) + "</size>";
                }
                _ui.Paint(card.Button, i == _selected);
            }
            _fight.interactable = count > 0 && _selected >= 0;
            _ui.Paint(_fight, true);
        }

        // A rival's hero, weapon and autopilot: "Cinder Arcanist  Staff  Bold".
        private string Identity(ArenaBuild build)
        {
            if (build == null)
                return string.Empty;
            HeroDefinition hero = Definition(build.HeroId);
            return ArenaText.HeroName(build.HeroId, hero != null ? hero.DisplayName : null) + "  " + ArenaText.WeaponName(build.WeaponId) +
                   "  " + ArenaText.Policy(build.Policy);
        }

        private void RefreshResult()
        {
            if (_outcome == null)
                return;
            DuelMatch match = _outcome.Match;
            _resultTitle.text = ArenaText.Outcome(match.Winner);
            _resultTitle.color = match.Winner == 0 ? ArenaUi.Gold : match.Winner == 1 ? ArenaUi.Foe : ArenaUi.Bright;
            _resultAgainst.text = ArenaText.Against(_rivalName);
            _resultRounds.text = ArenaText.Rounds(match.WinsA, match.WinsB);
            for (int i = 0; i < _roundPips.Length; i++)
            {
                bool shown = i < match.Rounds.Count;
                _roundPips[i].gameObject.SetActive(shown);
                if (!shown)
                    continue;
                int winner = match.Rounds[i].Winner;
                _roundPips[i].color = winner == 0 ? ArenaUi.Ally : winner == 1 ? ArenaUi.Foe : ArenaUi.Muted;
                float x = SurfaceWidth / 2f + (i - (match.Rounds.Count - 1) / 2f) * 70f;
                ArenaUi.Centre(_roundPips[i].rectTransform, x, 368f, 44f, 44f);
            }
            _resultRating.text = ArenaText.RatingChange(_outcome.Mode, _outcome.RatingBefore, _outcome.RatingAfter);
            DuelWhy[] why = match.Why;
            ArenaFighter winnerFighter = match.Winner == 0 ? _outcome.Player : match.Winner == 1 ? _outcome.Foe : null;
            _whyHeading.text = match.Winner == 0 ? ArenaText.WhyYouWon : match.Winner == 1 ? ArenaText.WhyHeading(_rivalName) : ArenaText.NoWhy;
            for (int i = 0; i < _whyLines.Length; i++)
            {
                bool shown = i < why.Length && winnerFighter != null;
                _whyCards[i].gameObject.SetActive(shown);
                _whyLines[i].gameObject.SetActive(shown);
                if (shown)
                    _whyLines[i].text = ArenaText.Why(why[i], winnerFighter.Weapon.Id, winnerFighter.Mode == ArenaMode.Open);
            }
        }

        // ---- Building ----

        private void BuildCanvas()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = SortingOrder;
            var scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0f;

            Image backdrop = _ui.Part(transform, "Arena Backdrop", new Color(.02f, .02f, .04f, .94f), _ui.White);
            ArenaUi.Stretch(backdrop.rectTransform, Vector2.zero, Vector2.zero);
            backdrop.raycastTarget = true;

            _safe = ArenaUi.Node(transform, "Arena Safe Area");
            ArenaUi.Stretch(_safe, Vector2.zero, Vector2.zero);
            _safe.gameObject.AddComponent<SafeAreaFitter>();

            Image surface = _ui.Part(_safe, "Arena Surface", ArenaUi.Surface, _ui.White);
            _surface = surface.rectTransform;
            _surface.anchorMin = _surface.anchorMax = new Vector2(.5f, .5f);
            _surface.pivot = new Vector2(.5f, .5f);
            _surface.sizeDelta = new Vector2(SurfaceWidth, SurfaceHeight);
            surface.raycastTarget = true;

            for (int i = 0; i < _pages.Length; i++)
            {
                RectTransform page = ArenaUi.Node(_surface, "Arena Page " + (Page)i);
                ArenaUi.Stretch(page, Vector2.zero, Vector2.zero);
                _pages[i] = page.gameObject;
            }
            BuildBuildPage((RectTransform)_pages[(int)Page.Build].transform);
            BuildLadderPage((RectTransform)_pages[(int)Page.Ladder].transform);
            // The playback's speed and skip act at once (its own buttons); the other pages' wait out the input delay.
            _playback = new DuelPlaybackView(_ui, (RectTransform)_pages[(int)Page.Playback].transform);
            BuildResultPage((RectTransform)_pages[(int)Page.Result].transform);
        }

        // A page's button acts once the input delay after the page appeared has passed.
        private void WireButton(Button button, Action click) => button.onClick.AddListener(() => { if (Ready()) click(); });

        private void Title(RectTransform page, string name)
        {
            Text title = _ui.Label(page, name, 72, TextAnchor.MiddleCenter, ArenaUi.Gold, FontStyle.Bold);
            ArenaUi.Place(title.rectTransform, 140f, 14f, 720f, 92f);
            LocalisedLabel.Bind(title, () => ArenaText.Title);
            Button close = _ui.Button(page, name + " Close", 34, out Text label, null);
            ArenaUi.Place((RectTransform)close.transform, 20f, 24f, 150f, 76f);
            LocalisedLabel.Bind(label, () => ArenaText.Close);
            WireButton(close, Close);
        }

        private Text Heading(RectTransform page, string name, float y, Func<string> words)
        {
            Text heading = _ui.Label(page, name, 40, TextAnchor.MiddleLeft, ArenaUi.Gold, FontStyle.Bold);
            ArenaUi.Place(heading.rectTransform, 24f, y, 952f, 50f);
            if (words != null)
                LocalisedLabel.Bind(heading, words);
            return heading;
        }

        private Choice MakeChoice(RectTransform page, string name, float x, float y, float width, float height, int size, Action click,
            bool picture = false)
        {
            Button button = _ui.Button(page, name, size, out Text label, null);
            ArenaUi.Place((RectTransform)button.transform, x, y, width, height);
            WireButton(button, click);
            var choice = new Choice { Button = button, Label = label };
            if (picture)
            {
                choice.Picture = _ui.Part(button.transform, name + " Portrait", Color.white);
                RectTransform rect = choice.Picture.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(.36f, 1f);
                rect.offsetMin = new Vector2(10f, 8f);
                rect.offsetMax = new Vector2(0f, -8f);
                choice.Picture.preserveAspect = true;
                label.rectTransform.anchorMin = new Vector2(.36f, 0f);
            }
            return choice;
        }

        private void BuildBuildPage(RectTransform page)
        {
            Title(page, "Arena Build Title");
            Text subtitle = _ui.Label(page, "Arena Build Subtitle", 40, TextAnchor.MiddleCenter, ArenaUi.Bright);
            ArenaUi.Place(subtitle.rectTransform, 100f, 108f, 800f, 52f);
            LocalisedLabel.Bind(subtitle, () => ArenaText.BuildTitle);

            Heading(page, "Arena Hero Heading", 176f, () => ArenaText.HeroHeading);
            for (int i = 0; i < _heroCards.Length; i++)
            {
                int index = i;
                _heroCards[i] = MakeChoice(page, "Arena Hero " + i, 20f + i * 490f, 230f, 470f, 230f, 40,
                    () => { if (index < _heroIds.Count) PickHero(_heroIds[index]); }, true);
            }

            Heading(page, "Arena Weapon Heading", 484f, () => ArenaText.WeaponHeading);
            for (int i = 0; i < _weaponChips.Length; i++)
            {
                int index = i;
                _weaponChips[i] = MakeChoice(page, "Arena Weapon " + i, 20f + i * 325f, 538f, 310f, 116f, 38,
                    () => { if (index < _weaponIds.Count) PickWeapon(_weaponIds[index]); });
            }

            _linesHeading = Heading(page, "Arena Lines Heading", 680f, null);
            Text note = _ui.Label(page, "Arena Lines Note", 28, TextAnchor.MiddleLeft, ArenaUi.Muted);
            ArenaUi.Place(note.rectTransform, 24f, 730f, 952f, 66f);
            LocalisedLabel.Bind(note, () => ArenaText.LinesNote);
            for (int i = 0; i < _lineChips.Length; i++)
            {
                int index = i;
                _lineChips[i] = MakeChoice(page, "Arena Line " + i, 20f + (i % 2) * 490f, 806f + (i / 2) * 120f, 470f, 106f, 36,
                    () => { if (index < _pool.Count) ToggleLine(_pool[index]); });
            }

            Heading(page, "Arena Policy Heading", 1180f, () => ArenaText.PolicyHeading);
            for (int i = 0; i < _policyChips.Length; i++)
            {
                var policy = (ArenaPolicy)i;
                _policyChips[i] = MakeChoice(page, "Arena Policy " + policy, 20f + i * 490f, 1234f, 470f, 150f, 40, () => PickPolicy(policy));
            }

            _continue = _ui.Button(page, "Arena Continue Button", 48, out Text label, null);
            ArenaUi.Place((RectTransform)_continue.transform, 180f, 1650f, 640f, 128f);
            LocalisedLabel.Bind(label, () => ArenaText.Continue);
            WireButton(_continue, ConfirmBuild);
        }

        private void BuildLadderPage(RectTransform page)
        {
            Title(page, "Arena Ladder Title");
            for (int i = 0; i < _tabs.Length; i++)
            {
                var mode = (ArenaMode)i;
                _tabs[i] = _ui.Button(page, "Arena Tab " + (mode == ArenaMode.Ranked ? "Training" : "Open"), 40, out Text label, null);
                ArenaUi.Place((RectTransform)_tabs[i].transform, 20f + i * 490f, 120f, 470f, 104f);
                LocalisedLabel.Bind(label, mode == ArenaMode.Ranked ? (Func<string>)(() => ArenaText.TrainingTab) : () => ArenaText.OpenTab);
                WireButton(_tabs[i], () => ShowMode(mode));
            }
            _status = _ui.Label(page, "Arena Status", 38, TextAnchor.MiddleCenter, ArenaUi.Bright, FontStyle.Bold);
            ArenaUi.Place(_status.rectTransform, 20f, 240f, 960f, 56f);
            _record = _ui.Label(page, "Arena Record", 32, TextAnchor.MiddleCenter, ArenaUi.Muted);
            ArenaUi.Place(_record.rectTransform, 20f, 298f, 960f, 46f);

            Image card = _ui.Part(page, "Arena Build Card", ArenaUi.Card, _ui.White);
            ArenaUi.Place(card.rectTransform, 20f, 360f, 960f, 116f);
            _buildSummary = _ui.Label(card.transform, "Arena Build Summary", 34, TextAnchor.MiddleLeft, ArenaUi.Bright);
            ArenaUi.Place(_buildSummary.rectTransform, 20f, 8f, 640f, 100f);
            Button edit = _ui.Button(card.transform, "Arena Edit Build Button", 32, out Text editLabel, null);
            ArenaUi.Place((RectTransform)edit.transform, 680f, 14f, 266f, 88f);
            LocalisedLabel.Bind(editLabel, () => ArenaText.EditBuild);
            WireButton(edit, () => Show(Page.Build));

            _ladderNote = _ui.Label(page, "Arena Ladder Note", 30, TextAnchor.MiddleCenter, ArenaUi.Muted);
            ArenaUi.Place(_ladderNote.rectTransform, 20f, 490f, 960f, 60f);
            for (int i = 0; i < _rivalCards.Length; i++)
            {
                int index = i;
                _rivalCards[i] = MakeChoice(page, "Arena Card " + i, 20f, 566f + i * 330f, 960f, 306f, 42, () => Select(index));
                _rivalCards[i].Label.alignment = TextAnchor.MiddleLeft;
                _rivalCards[i].Label.rectTransform.offsetMin = new Vector2(36f, 12f);
                _rivalCards[i].Label.rectTransform.offsetMax = new Vector2(-36f, -12f);
            }
            _fight = _ui.Button(page, "Arena Fight Button", 56, out Text fightLabel, null);
            ArenaUi.Place((RectTransform)_fight.transform, 180f, 1650f, 640f, 128f);
            LocalisedLabel.Bind(fightLabel, () => ArenaText.Fight);
            WireButton(_fight, FightSelected);
        }

        private void BuildResultPage(RectTransform page)
        {
            _resultTitle = _ui.Label(page, "Arena Result Title", 120, TextAnchor.MiddleCenter, ArenaUi.Gold, FontStyle.Bold);
            ArenaUi.Place(_resultTitle.rectTransform, 40f, 50f, 920f, 150f);
            _resultTitle.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, .8f);
            _resultAgainst = _ui.Label(page, "Arena Result Against", 40, TextAnchor.MiddleCenter, ArenaUi.Bright);
            ArenaUi.Place(_resultAgainst.rectTransform, 40f, 204f, 920f, 56f);
            _resultRounds = _ui.Label(page, "Arena Result Rounds", 50, TextAnchor.MiddleCenter, ArenaUi.Bright, FontStyle.Bold);
            ArenaUi.Place(_resultRounds.rectTransform, 40f, 270f, 920f, 66f);
            for (int i = 0; i < _roundPips.Length; i++)
                _roundPips[i] = _ui.Part(page, "Arena Result Round " + i, Color.white, _ui.Disc);
            _resultRating = _ui.Label(page, "Arena Result Rating", 40, TextAnchor.MiddleCenter, ArenaUi.Bright);
            ArenaUi.Place(_resultRating.rectTransform, 40f, 416f, 920f, 60f);
            _whyHeading = _ui.Label(page, "Arena Why Heading", 46, TextAnchor.MiddleCenter, ArenaUi.Gold, FontStyle.Bold);
            ArenaUi.Place(_whyHeading.rectTransform, 40f, 520f, 920f, 64f);
            for (int i = 0; i < _whyLines.Length; i++)
            {
                _whyCards[i] = _ui.Part(page, "Arena Why Card " + i, ArenaUi.Card, _ui.White);
                ArenaUi.Place(_whyCards[i].rectTransform, 60f, 600f + i * 124f, 880f, 108f);
                _whyLines[i] = _ui.Label(page, "Arena Why " + i, 40, TextAnchor.MiddleCenter, ArenaUi.Bright);
                ArenaUi.Place(_whyLines[i].rectTransform, 80f, 604f + i * 124f, 840f, 100f);
            }
            _again = _ui.Button(page, "Arena Again Button", 44, out Text againLabel, null);
            ArenaUi.Place((RectTransform)_again.transform, 20f, 1650f, 470f, 128f);
            LocalisedLabel.Bind(againLabel, () => ArenaText.WatchAgain);
            WireButton(_again, WatchAgain);
            _next = _ui.Button(page, "Arena Next Button", 44, out Text nextLabel, null);
            ArenaUi.Place((RectTransform)_next.transform, 510f, 1650f, 470f, 128f);
            LocalisedLabel.Bind(nextLabel, () => ArenaText.NextFight);
            WireButton(_next, () => Show(Page.Ladder));
        }

        // ---- Fitting ----

        // The 1000 x 1800 surface scaled down to the safe area the canvas measures now (a short phone), never up; the
        // capture tool calls it after it redraws the canvases at a phone's size.
        public void FitNow() => FitToSafeArea();

        private void FitToSafeArea()
        {
            if (_safe == null || _surface == null)
                return;
            Rect area = _safe.rect;
            float scale = Mathf.Min(1f, Mathf.Min((area.height - 2f * FitMargin) / SurfaceHeight, area.width / SurfaceWidth));
            if (scale > 0f && !Mathf.Approximately(_surface.localScale.x, scale))
                _surface.localScale = new Vector3(scale, scale, 1f);
        }

        private void OnDestroy()
        {
            if (_subscribed)
                Loc.Changed -= OnLanguageChanged;
            _subscribed = false;
            _ui?.Dispose();
            if (Current == this)
                Current = null;
        }
    }
}
