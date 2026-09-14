using System;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using UnityEngine;

namespace Cryptforge.Combat
{
    // Puts one chest on the floor for every combat room, on the spot ChestRule picks, and opens it when the hero walks
    // onto it: even rooms mend a quarter of the hero's health, odd rooms pay gold at the floor's rate. A chest stays open
    // where it was until the next room's chest replaces it.
    public sealed class ChestSpawner : MonoBehaviour
    {
        [SerializeField] private EncounterController _encounters;
        [SerializeField] private Health _hero;
        [SerializeField] private CombatSetup _setup;
        [SerializeField] private ArenaView _arena;
        // Sorted with the combatants, so the hero walks in front of or behind the chest by depth.
        [SerializeField] private int _sortingOrder = 1;
        private Sprite _closed;
        private Sprite _open;
        private SpriteRenderer _renderer;
        private int _floorNumber;
        private int _roomNumber;
        private float _floorX;
        private float _floorY;
        private ChestReward _reward;
        private bool _subscribed;

        public bool HasChest => _renderer != null && _renderer.enabled;
        public bool IsOpen { get; private set; }
        public int ChestsOpened { get; private set; }
        public float ChestFloorX => _floorX;
        public float ChestFloorY => _floorY;
        public event Action<ChestReward, Vector3> Opened;

        private void Awake()
        {
            if (_encounters == null || _hero == null || _setup == null || _arena == null)
            {
                Debug.LogError("ChestSpawner needs the encounter controller, the hero, the combat setup and the arena.", this);
                enabled = false;
                return;
            }

            _closed = PixelSpriteFactory.CreateSprite(ChestArt.Draw(false), "Chest Closed", PixelSpriteFactory.BottomCentre);
            _open = PixelSpriteFactory.CreateSprite(ChestArt.Draw(true), "Chest Open", PixelSpriteFactory.BottomCentre);
            var chest = new GameObject("Chest");
            chest.transform.SetParent(transform, false);
            _renderer = chest.AddComponent<SpriteRenderer>();
            _renderer.sprite = _closed;
            _renderer.sortingOrder = _sortingOrder;
            _renderer.enabled = false;
        }

        private void OnEnable()
        {
            if (_renderer == null)
                return;
            _encounters.EncounterStarted += OnEncounterStarted;
            _subscribed = true;
            // The first wave may have spawned before this spawner could listen.
            if (_encounters.WaveEnemyCount > 0)
                OnEncounterStarted();
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                _encounters.EncounterStarted -= OnEncounterStarted;
                _subscribed = false;
            }
        }

        private void Update()
        {
            if (!HasChest || IsOpen || !_hero.IsAlive || _setup.Run == null || _setup.Run.HasEnded || Time.deltaTime <= 0f)
                return;
            if (ChestRule.IsWithinReach(_encounters.HeroFloorX, _encounters.HeroFloorY, _floorX, _floorY))
                Open();
        }

        // A new room's first wave brings a new chest; later waves of the same room leave it be.
        private void OnEncounterStarted()
        {
            if (_encounters.FloorNumber == _floorNumber && _encounters.RoomNumber == _roomNumber)
                return;

            _floorNumber = _encounters.FloorNumber;
            _roomNumber = _encounters.RoomNumber;
            int roomIndex = _roomNumber - 1;
            ChestRule.SpotFor(roomIndex, _arena.Geometry, out _floorX, out _floorY);
            _reward = ChestRule.RewardFor(roomIndex);
            IsOpen = false;
            _renderer.sprite = _closed;
            _renderer.transform.position = new Vector3(_floorX, ArenaFloor.WorldY(_floorY), 0f);
            _renderer.enabled = true;
        }

        private void Open()
        {
            IsOpen = true;
            ChestsOpened++;
            _renderer.sprite = _open;
            if (_reward.Kind == ChestRewardKind.Heal)
            {
                _hero.Heal(_hero.Maximum * _reward.HealFraction);
            }
            else
            {
                FloorModifierDefinition modifier = _encounters.Floor != null ? _encounters.Floor.Modifier : null;
                _setup.Run.AddGold(FloorScaling.Gold(_reward.Gold, modifier != null ? modifier.GoldPercent : 0f));
            }
            Opened?.Invoke(_reward, _renderer.transform.position);
        }

        private void OnDestroy()
        {
            PixelSpriteFactory.Destroy(_closed);
            PixelSpriteFactory.Destroy(_open);
        }
    }
}
