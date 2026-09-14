using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // The dashed ring on the floor around the hero that shows the ability's reach: bright while the burst is ready, faint
    // while it cools down. Built in Start, after CombatSetup has created the ability.
    public sealed class AbilityRingView : MonoBehaviour
    {
        private const string RingName = "Ability Ring";

        [SerializeField] private CombatSetup _setup;
        [SerializeField] private AbilityController _controller;
        [SerializeField, Range(0f, 1f)] private float _readyAlpha = 0.75f;
        [SerializeField, Range(0f, 1f)] private float _coolingAlpha = 0.22f;
        // Sorting order between the platform and the combatants.
        [SerializeField] private int _sortingOrder = 0;
        private Sprite _sprite;
        private SpriteRenderer _renderer;

        public SpriteRenderer Ring => _renderer;

        private void Start()
        {
            if (_setup == null || _controller == null || _controller.Ability == null)
            {
                Debug.LogError("AbilityRingView needs the combat setup and an initialised ability controller.", this);
                enabled = false;
                return;
            }

            int radiusTexels = Mathf.RoundToInt(_controller.Ability.Radius * PixelSpriteFactory.PixelsPerUnit);
            _sprite = PixelSpriteFactory.CreateSprite(AbilityArt.DrawRangeRing(radiusTexels), RingName, PixelSpriteFactory.Centre);
            var ring = new GameObject(RingName);
            ring.transform.SetParent(transform, false);
            _renderer = ring.AddComponent<SpriteRenderer>();
            _renderer.sprite = _sprite;
            _renderer.sortingOrder = _sortingOrder;
            Refresh();
        }

        private void Update()
        {
            if (_renderer != null)
                Refresh();
        }

        private void Refresh()
        {
            bool ready = _controller.Ability.IsReady && !_setup.Run.HasEnded;
            // A slow breath while ready draws the eye to the ring without competing with the fight.
            float alpha = ready ? _readyAlpha - 0.1f * (0.5f + 0.5f * Mathf.Sin(Time.time * 3f)) : _coolingAlpha;
            _renderer.color = new Color(1f, 1f, 1f, alpha);
        }

        private void OnDestroy() => PixelSpriteFactory.Destroy(_sprite);
    }
}
