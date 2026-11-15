using System;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // The Cinder Bloom's open state and its bark (docs/52 section 5's readability; the art of 2026-11-14, ArtDirection
    // cinder-bloom-01/sprites.json). The closed Bloom is its painted set's (CinderBloomArtSet on EnemyLookView: idle, walk,
    // the maul's windup and strike); while its bloom is open this view lays the open-state frames over the body after the
    // look has drawn the frame (LateUpdate): unfold-0 then unfold-1 as the window begins, open for the window, unfold-1 then
    // unfold-0 in its last CloseLead seconds, TransitionSeconds a frame - facing and mirrored as the look faces it. The open
    // frames' flashes are handed to the look (EnemyLookView.AddFlash), so a hit on the open bloom flashes its silhouette.
    // The open Bloom is rooted, so its walk no longer turns it: while open it faces the hero (CinderBloom.Hero) itself -
    // mirrored when the hero stands to its right, from behind only when the hero stands clearly farther from the camera,
    // else its open front (the bloom is the read), with the look's thresholds - so a hero who circles a rooted Bloom sees
    // it turn to where the hero stands.
    // A landing the closed bark blunted tints the hit flash toward bark and plays the bark chip at the chest
    // (CombatantView.FlashTint). The bloom is orchid, never red-orange: it does not hurt.
    [RequireComponent(typeof(CinderBloom))]
    public sealed class CinderBloomView : MonoBehaviour
    {
        [Serializable]
        public struct Frame
        {
            public Sprite Body;
            public Sprite Flash;
        }

        public const float TransitionSeconds = .08f;
        public const float CloseLead = .16f;
        public const float ChipFrameSeconds = .08f;
        // How far the struck closed Bloom's flash goes toward bark (#65574C).
        public const float BarkTintShare = .65f;
        public static readonly Color Bark = new Color(0x65 / 255f, 0x57 / 255f, 0x4C / 255f, 1f);

        // unfold-0, unfold-1, open, as seen from the front and from behind (both look left, as the painted set's).
        [SerializeField] private Frame[] _front = new Frame[3];
        [SerializeField] private Frame[] _rear = new Frame[3];
        [SerializeField] private Sprite[] _barkChip = new Sprite[3];
        // Where the chip plays, above the feet, in world units (the furnace's height on the closed body).
        [SerializeField] private float _chipHeight = .5f;

        private CinderBloom _bloom;
        private EnemyLookView _look;
        private SpriteRenderer _body;
        private CombatantView _combatant;
        private SpriteRenderer _chip;
        private float _chipAge = -1f;

        // The open-state frame the body shows now (0 unfold-0, 1 unfold-1, 2 open), -1 while the closed look shows.
        public int OpenFrame { get; private set; } = -1;
        // How the open Bloom faces: from behind, and mirrored (looking right); the look's own while closed.
        public bool FacesRear { get; private set; }
        public bool FacesMirrored { get; private set; }
        // A turn needs the direction to the hero (on the floor, unit length) this far off an axis, as EnemyLookView's walk does.
        public const float TurnThreshold = .15f;
        // Bark chips played since the Bloom stood up.
        public int Chips { get; private set; }
        public bool IsChipping => _chipAge >= 0f;

        private void Awake()
        {
            _bloom = GetComponent<CinderBloom>();
            _look = GetComponent<EnemyLookView>();
            _combatant = GetComponent<CombatantView>();
            _body = _look != null ? _look.Body : null;
            if (_look != null)
                foreach (Frame[] side in new[] { _front, _rear })
                    for (int i = 0; side != null && i < side.Length; i++)
                        _look.AddFlash(side[i].Body, side[i].Flash);
        }

        private void OnEnable()
        {
            _bloom.BarkStruck += OnBarkStruck;
            if (_combatant != null)
                _combatant.FlashTint = TintOf;
            OpenFrame = -1;
            Chips = 0;
            StopChip();
        }

        private void OnDisable()
        {
            _bloom.BarkStruck -= OnBarkStruck;
            if (_combatant != null && _combatant.FlashTint == (Func<Color, Color>)TintOf)
                _combatant.FlashTint = null;
            StopChip();
        }

        // The frame of the open window seconds into it, of a window lasting window: 0 and 1 unfolding, 2 open, 1 and 0
        // closing in its last CloseLead. -1 while closed.
        public static int FrameAt(float openFor, float window)
        {
            if (openFor < 0f)
                return -1;
            float left = window - openFor;
            if (openFor < TransitionSeconds || left <= TransitionSeconds)
                return 0;
            if (openFor < 2f * TransitionSeconds || left <= CloseLead)
                return 1;
            return 2;
        }

        private void LateUpdate()
        {
            if (_chipAge >= 0f)
                TickChip();
            OpenFrame = -1;
            if (_body == null || !_bloom.IsCycling || !_bloom.Body.IsAlive)
                return;
            int frame = FrameAt(_bloom.Clock.OpenFor, _bloom.Clock.OpenSeconds);
            if (frame < 0)
            {
                FacesRear = _look.RearFacing;
                FacesMirrored = _look.Mirrored;
                return;
            }
            FaceTheHero();
            Frame[] side = FacesRear ? _rear : _front;
            if (side == null || frame >= side.Length || side[frame].Body == null)
                return;
            _body.sprite = side[frame].Body;
            Vector3 scale = _body.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (FacesMirrored ? -1f : 1f);
            _body.transform.localScale = scale;
            OpenFrame = frame;
        }

        // The hero's side of the rooted Bloom on the floor, turned only past TurnThreshold on each axis.
        private void FaceTheHero()
        {
            Health hero = _bloom.Hero;
            if (hero == null)
                return;
            Vector3 at = transform.position, to = hero.transform.position;
            var direction = new Vector2(to.x - at.x, ArenaFloor.FloorY(to.y) - ArenaFloor.FloorY(at.y));
            if (direction.sqrMagnitude < 1e-6f)
                return;
            direction.Normalize();
            if (Mathf.Abs(direction.x) > TurnThreshold)
                FacesMirrored = direction.x > 0f;
            // The open bloom is the read: it shows its front unless the hero stands clearly farther from the camera.
            FacesRear = direction.y > TurnThreshold;
        }

        // A hit on the closed Bloom flashes toward bark; on the open bloom, as any hit.
        private Color TintOf(Color color) =>
            _bloom.IsCycling && !_bloom.Clock.IsOpen ? Color.Lerp(color, Bark, BarkTintShare) : color;

        private void OnBarkStruck(float amount, float taken)
        {
            if (_barkChip == null || _barkChip.Length == 0 || _barkChip[0] == null || _body == null)
                return;
            if (_chip == null)
            {
                var chip = new GameObject("BarkChip");
                chip.transform.SetParent(_body.transform, false);
                _chip = chip.AddComponent<SpriteRenderer>();
                _chip.sortingLayerID = _body.sortingLayerID;
                _chip.sortingOrder = _body.sortingOrder + 2;
            }
            _chip.transform.localPosition = new Vector3(0f, _chipHeight, 0f);
            _chip.sprite = _barkChip[0];
            _chip.enabled = true;
            _chipAge = 0f;
            Chips++;
        }

        private void TickChip()
        {
            // Unscaled, as the hit flash: a chip still settles under an upgrade pause.
            _chipAge += Time.unscaledDeltaTime;
            int frame = (int)(_chipAge / ChipFrameSeconds);
            if (frame >= _barkChip.Length || _barkChip[frame] == null)
            {
                StopChip();
                return;
            }
            _chip.sprite = _barkChip[frame];
        }

        private void StopChip()
        {
            _chipAge = -1f;
            if (_chip != null)
                _chip.enabled = false;
        }
    }
}
