using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only: the Warden's mark on the floor - a ring the hammer's reach wide where the hero stood, and an inner
    // ring growing out to it through the telegraph, so the moment it fills is the moment the blow lands - then a flash
    // where it lands. The Warden's body tells it too: a painted Warden raises its hammer while the mark is down and
    // brings it down as the blow lands (EnemyLookView.WindUp and Slam). WardenSlamService owns the clock, the mark and
    // the blow.
    [DefaultExecutionOrder(100)]
    public sealed class WardenSlamView : MonoBehaviour
    {
        private const int Segments = 40;
        private const float FlashSeconds = .35f;
        private static readonly Color Ember = new Color(1f, .32f, .12f, 1f);
        private static readonly Color Hot = new Color(1f, .82f, .45f, 1f);
        private WardenSlamService _service;
        private ArenaView _arena;
        private Material _material;
        private Transform _mark;
        private LineRenderer _edge;
        private LineRenderer _fill;
        private float _flash;
        // The boss look holding its hammer up for the mark that is down.
        private EnemyLookView _windingUp;
        public bool IsMarkShown => _mark != null && _mark.gameObject.activeSelf;
        public float ShownFill { get; private set; }

        internal void Initialize(WardenSlamService service)
        {
            _service = service;
            _arena = FindFirstObjectByType<ArenaView>();
            _material = new Material(Shader.Find("Sprites/Default"));
            _mark = new GameObject("Warden Mark").transform;
            _mark.SetParent(transform, false);
            _edge = Ring("Warden Mark Edge", .07f);
            _fill = Ring("Warden Mark Fill", .05f);
            _mark.gameObject.SetActive(false);
            service.Marked += OnMarked;
            service.Slammed += _ => _flash = FlashSeconds;
            service.Slammed += OnSlammed;
        }

        // Where the mark is down, on the floor, in world space.
        private Vector3 MarkPoint => new Vector3(_service.MarkX, ArenaFloor.WorldY(_service.MarkY), 0f);

        private LineRenderer Ring(string name, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(_mark, false);
            line.sharedMaterial = _material; line.useWorldSpace = false; line.loop = true; line.numCapVertices = 2;
            line.sortingOrder = 0; line.positionCount = Segments; line.startWidth = line.endWidth = width;
            for (int k = 0; k < Segments; k++)
            {
                float a = k * Mathf.PI * 2 / Segments;
                line.SetPosition(k, new Vector3(Mathf.Cos(a) * WardenRule.SlamRadius, ArenaFloor.WorldY(Mathf.Sin(a) * WardenRule.SlamRadius), 0));
            }
            return line;
        }

        private void OnMarked()
        {
            var p = new Vector3(_service.MarkX, ArenaFloor.WorldY(_service.MarkY), 0);
            if (_arena != null && _arena.IsFoundryRegion) p.y += FoundrySurface.Lift(p, _arena.transform.position);
            _mark.position = p;
            _flash = 0f;
            Health boss = enabled ? _service.Boss : null;
            _windingUp = boss != null ? boss.GetComponent<EnemyLookView>() : null;
            if (_windingUp != null)
                _windingUp.WindUp(MarkPoint);
        }

        private void OnSlammed(bool hit)
        {
            if (_windingUp != null)
                _windingUp.Slam(MarkPoint);
            _windingUp = null;
        }

        private void LateUpdate()
        {
            if (_mark == null)
                return;
            if (_flash > 0f)
                _flash -= Time.deltaTime;
            bool marked = enabled && _service.IsFighting && _service.Clock.IsMarked;
            // A mark withdrawn without a blow (the boss fell or changed, the fight stopped): the hammer comes down.
            if (!marked && _windingUp != null)
            {
                _windingUp.CancelWindup();
                _windingUp = null;
            }
            float flash = Mathf.Clamp01(_flash / FlashSeconds);
            bool shown = marked || flash > 0f;
            if (_mark.gameObject.activeSelf != shown)
                _mark.gameObject.SetActive(shown);
            if (!shown)
            {
                ShownFill = 0f;
                return;
            }
            ShownFill = marked ? _service.Clock.TelegraphProgress : 1f;
            // The edge pulses faster as the blow nears; the fill grows out to it; the blow flashes the whole mark hot.
            float pulse = .6f + .4f * Mathf.Sin(Time.time * Mathf.Lerp(9f, 24f, ShownFill));
            Color edge = flash > 0f ? Color.Lerp(Ember, Hot, flash) : Ember;
            edge.a = flash > 0f ? flash : pulse;
            _edge.startColor = _edge.endColor = edge;
            Color fill = Hot;
            fill.a = flash > 0f ? flash : .4f + .6f * ShownFill;
            _fill.startColor = _fill.endColor = fill;
            float scale = Mathf.Max(.05f, ShownFill);
            _fill.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.Marked -= OnMarked;
                _service.Slammed -= OnSlammed;
            }
            if (_material != null)
                Destroy(_material);
        }
    }
}
