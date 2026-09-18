using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cryptforge.UI
{
    // Walks the hero by drag: a finger (or the mouse in the Editor) pressed anywhere that is not a button becomes a stick
    // whose displacement from where it landed steers the hero; lifting it stops the hero. Screen up is floor depth, so a
    // diagonal drag walks the hero along the same diagonal on screen. It runs before the encounter, so the packs chase the
    // hero's position of this frame, in the order the Descent simulation uses.
    [DefaultExecutionOrder(-60)]
    public sealed class HeroMovementInput : MonoBehaviour
    {
        [SerializeField] private CombatSetup _setup;
        [SerializeField] private Health _hero;
        [SerializeField] private ArenaView _arena;
        // Floor units per second at full steer.
        [SerializeField, Min(0.1f)] private float _speed = 2.5f;
        // Pixels of drag before the hero starts walking, and the drag that steers at full speed.
        [SerializeField, Min(1f)] private float _deadZonePixels = 24f;
        [SerializeField, Min(1f)] private float _fullSteerPixels = 110f;
        // The movement budget (26 Part 5). Seconds of walking a full bar buys, bar-seconds restored per second of
        // standing, and the share of its speed the hero keeps once the bar is empty.
        [SerializeField, Min(0.1f)] private float _staminaBar = HeroStamina.DefaultBar;
        [SerializeField, Min(0.1f)] private float _staminaRefill = HeroStamina.DefaultRefill;
        [SerializeField, Range(0f, 1f)] private float _staminaEmptySpeed = HeroStamina.DefaultEmptySpeed;
        private HeroMotion _motion;
        private Vector2 _dragStart;
        private bool _dragging;
        private Vector2 _heldSteer;
        private Vector2 _steer;

        public HeroMotion Motion => _motion;
        // What the hero has left to run on, for a view to show and for the Descent simulation to mirror.
        public HeroStamina Stamina { get; private set; }
        // The steer in floor units, length up to one.
        public Vector2 Steer => _steer;
        public bool IsMoving { get; private set; }
        public bool IsDragging => _dragging;

        // Holds a steer without any touch, until cleared; tests and later an auto-battle option drive the hero this way.
        public void Hold(Vector2 floorSteer) => _heldSteer = floorSteer;

        public void Release() => _heldSteer = Vector2.zero;

        private void Awake()
        {
            if (_setup == null || _hero == null || _arena == null)
            {
                Debug.LogError("HeroMovementInput needs the combat setup, the hero and the arena.", this);
                enabled = false;
                return;
            }

            _motion = new HeroMotion(_speed);
            Stamina = new HeroStamina(_staminaBar, _staminaRefill, _staminaEmptySpeed);
            _motion.Place(transform.position.x, ArenaFloor.FloorY(transform.position.y), _arena.Geometry);
            // Every wave starts with a full bar: the pause while the next pack forms up is the hero's breather, and it
            // keeps the budget in step with the Descent simulation, which fills it at the same moment.
            if (_setup.Encounters != null)
                _setup.Encounters.EncounterStarted += OnWaveStarted;
        }

        private void OnDestroy()
        {
            if (_setup != null && _setup.Encounters != null)
                _setup.Encounters.EncounterStarted -= OnWaveStarted;
        }

        private void OnWaveStarted() => Stamina.Fill();

        private void Update()
        {
            ReadDrag();
            _steer = _dragging ? DragSteer() : _heldSteer;
            IsMoving = false;
            if (_setup.Run == null || _setup.Run.HasEnded || !_hero.IsAlive || Time.deltaTime <= 0f)
                return;

            // Read the budget before walking and spend it after, on the displacement that really happened: a steer the
            // rim refuses costs nothing. The Descent simulation does the same in the same order.
            //
            // Only a live wave spends it. Walking between waves, to a chest or across an empty room, is free, and the
            // bar fills again for the next pack. That is also what keeps the scene and the simulation on one bar: the
            // simulation only steps frames while a wave is alive, and a frame that spends nothing still refills, so a
            // scene frame the simulation does not have would otherwise hand the hero a little more bar than it earned.
            bool fighting = _setup.Encounters != null && _setup.Encounters.AliveEnemyCount > 0;
            IsMoving = _motion.Move(_steer.x, _steer.y, Time.deltaTime * Stamina.SpeedFactor, _arena.Geometry);
            Stamina.Step(Time.deltaTime, fighting && IsMoving);
            if (IsMoving)
                transform.position = new Vector3(_motion.X, ArenaFloor.WorldY(_motion.Y), transform.position.z);
        }

        private void ReadDrag()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began && !OverUi(touch.fingerId))
                {
                    _dragging = true;
                    _dragStart = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    _dragging = false;
                }
                if (_dragging)
                    _dragCurrent = touch.position;
                return;
            }

            if (Input.GetMouseButtonDown(0) && !OverUi(-1))
            {
                _dragging = true;
                _dragStart = Input.mousePosition;
            }
            if (!Input.GetMouseButton(0))
                _dragging = false;
            if (_dragging)
                _dragCurrent = Input.mousePosition;
        }

        private Vector2 _dragCurrent;

        // Screen pixels to a floor steer: past the dead zone the drag grows to full speed; up on the screen is depth.
        private Vector2 DragSteer()
        {
            Vector2 drag = _dragCurrent - _dragStart;
            float pixels = drag.magnitude;
            if (pixels < _deadZonePixels)
                return Vector2.zero;
            float strength = Mathf.Clamp01((pixels - _deadZonePixels) / (_fullSteerPixels - _deadZonePixels));
            var floor = new Vector2(drag.x, drag.y / ArenaFloor.DepthScale);
            return floor.normalized * strength;
        }

        private static bool OverUi(int pointerId)
        {
            EventSystem events = EventSystem.current;
            if (events == null)
                return false;
            return pointerId < 0 ? events.IsPointerOverGameObject() : events.IsPointerOverGameObject(pointerId);
        }
    }
}
