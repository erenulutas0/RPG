using System.Collections;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // When the foundry's boss walks out of the gate, the camera turns to it for a moment and comes back to the hero. The
    // encounter holds still meanwhile (EncounterController.HoldForReveal) and the hero's movement waits, so nothing is
    // fought off screen. A tap, the boss falling or the run ending turns the camera back at once. Time follows the game
    // clock, so a pause freezes the reveal too.
    public sealed class BossRevealView : MonoBehaviour
    {
        public const float TurnSeconds = 0.7f, HoldSeconds = 1.2f;
        // Above the boss's feet, so the opened gate frames it rather than the landing.
        private const float GateRise = 1.6f;
        private CombatSetup _setup;
        private HeroMovementInput _movement;
        private bool _movementEnabled, _ownsMovement;
        private Camera _camera;
        private ArenaCameraFollow _follow;
        private bool _followEnabled;
        private Vector3 _home;
        private bool _skip;
        private AbilityButtonView _abilityButton;
        public bool IsRevealing { get; private set; }
        // How far the camera has turned toward the boss (0 at the hero, 1 at the gate), for tests and the HUD.
        public float Turn { get; private set; }
        // The ability button while the reveal holds the fight (AbilityButtonView.IsHeld): greyed, it takes no tap, so a tap on
        // it skips the reveal as one on the floor does (RevealSkip). Null without one, or once it answers its own taps again.
        internal RectTransform HeldButton =>
            _abilityButton != null && _abilityButton.IsHeld ? _abilityButton.ButtonRect : null;

        public void Initialize(CombatSetup setup, HeroMovementInput movement)
        {
            _setup = setup; _movement = movement;
            _setup.Encounters.GateBossArrived += OnGateBossArrived;
        }

        private void OnDestroy()
        {
            if (_setup != null && _setup.Encounters != null) _setup.Encounters.GateBossArrived -= OnGateBossArrived;
        }

        private void OnGateBossArrived()
        {
            if (IsRevealing || !isActiveAndEnabled) return;
            _camera = Camera.main;
            _follow = _camera != null ? _camera.GetComponent<ArenaCameraFollow>() : null;
            // The gate's boss, not the wave's nearest enemy: floor 2's Warden walks out between two Mites.
            var boss = _setup.Encounters.GateBoss;
            if (boss == null) boss = _setup.Encounters.CurrentEnemy;
            if (_follow == null || boss == null) return;
            // The button the reveal greys, found once a reveal: a tap on it is the reveal's (HeldButton).
            _abilityButton = FindFirstObjectByType<AbilityButtonView>();
            StartCoroutine(Reveal(boss));
        }

        private IEnumerator Reveal(Combat.Health boss)
        {
            IsRevealing = true; _skip = false;
            _setup.Encounters.HoldForReveal(true);
            if (_movement != null) { _movementEnabled = _movement.enabled; _ownsMovement = true; _movement.Release(); _movement.enabled = false; }
            _followEnabled = _follow.enabled; _follow.enabled = false;
            _home = _camera.transform.position;
            var arena = FindFirstObjectByType<ArenaView>();
            Vector3 feet = boss.transform.position;
            if (arena != null && arena.IsFoundryRegion) feet.y += FoundrySurface.Lift(feet, arena.transform.position);
            Vector3 gate = new Vector3(feet.x, feet.y + GateRise, _home.z);
            float elapsed = 0f, returnFrom = -1f, reached = 0f;
            while (true)
            {
                elapsed += Time.deltaTime;
                bool over = _skip || boss == null || !boss.IsAlive || _setup.Run.HasEnded;
                if (returnFrom < 0f && (over || elapsed >= TurnSeconds + HoldSeconds)) { returnFrom = elapsed; reached = Turn; }
                if (returnFrom < 0f) Turn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / TurnSeconds));
                else Turn = reached * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - returnFrom) / TurnSeconds)));
                _camera.transform.position = Vector3.Lerp(_home, gate, Turn);
                if (returnFrom >= 0f && elapsed - returnFrom >= TurnSeconds) break;
                yield return null;
            }
            Finish();
        }

        // After every Update, so the EventSystem has taken this frame's touches and a tap on a button (the pause button) is
        // the button's, not a skip, while a tap on the greyed ability button, which takes none, skips (RevealSkip.ThisFrame,
        // HeldButton); and never while the game stands paused, as this runs at a time scale of 0 too and a tap on the
        // paused screen latched the skip for the moment play resumed.
        private void LateUpdate()
        {
            if (IsRevealing && !_skip && RevealSkip.ThisFrame(IsPaused, HeldButton)) _skip = true;
        }

        private bool IsPaused => Time.timeScale <= 0f || (_setup != null && _setup.Pause != null && _setup.Pause.IsFrozen);

        private void Finish()
        {
            if (!IsRevealing) return;
            IsRevealing = false; Turn = 0f;
            if (_camera != null) _camera.transform.position = _home;
            if (_follow != null) _follow.enabled = _followEnabled;
            if (_ownsMovement && _movement != null) { _movement.Release(); _movement.enabled = _movementEnabled; }
            _ownsMovement = false;
            if (_setup != null && _setup.Encounters != null) _setup.Encounters.HoldForReveal(false);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            Finish();
        }
    }
}
