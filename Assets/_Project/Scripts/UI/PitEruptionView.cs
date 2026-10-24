using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only: the pit's danger band - the line PitEruptionRule.Reach beyond the hole's edge, a rounded square
    // on the floor - glows in as the warning runs, brightening and thickening to the spill, then flashes and fades.
    // PitEruptionService owns the clock and the burn.
    [DefaultExecutionOrder(100)]
    public sealed class PitEruptionView : MonoBehaviour
    {
        private const int CornerSteps = 8;
        private const float FlashSeconds = .45f;
        private PitEruptionService _service;
        private Material _material;
        private LineRenderer _band;
        private float _flash;
        public float ShownAlpha { get; private set; }

        internal void Initialize(PitEruptionService service)
        {
            _service = service;
            if (!service.IsActive) return;
            _material = new Material(Shader.Find("Sprites/Default"));
            _band = new GameObject("Pit Eruption Band").AddComponent<LineRenderer>();
            _band.transform.SetParent(transform, false);
            _band.sharedMaterial = _material; _band.useWorldSpace = true; _band.loop = true; _band.numCapVertices = 2;
            _band.sortingOrder = 0;
            var arena = FindFirstObjectByType<ArenaView>();
            var points = new Vector3[CornerSteps * 4 + 4];
            float[] cu = { FoundryMap.PitEast, FoundryMap.PitWest, FoundryMap.PitWest, FoundryMap.PitEast };
            float[] cv = { FoundryMap.PitNorth, FoundryMap.PitNorth, FoundryMap.PitSouth, FoundryMap.PitSouth };
            int n = 0;
            for (int corner = 0; corner < 4; corner++)
                for (int k = 0; k <= CornerSteps; k++)
                {
                    float a = (corner + k / (float)CornerSteps) * Mathf.PI * .5f;
                    FoundryMap.ToFloor(cu[corner] + Mathf.Cos(a) * PitEruptionRule.Reach, cv[corner] + Mathf.Sin(a) * PitEruptionRule.Reach,
                        out float x, out float y);
                    var p = new Vector3(x, ArenaFloor.WorldY(y), 0);
                    if (arena != null) p.y += FoundrySurface.Lift(p, arena.transform.position);
                    points[n++] = p;
                }
            _band.positionCount = n; _band.SetPositions(points);
            _service.Erupted += () => _flash = FlashSeconds;
            Refresh();
        }

        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (_band == null) return;
            if (_flash > 0f) _flash -= Time.deltaTime;
            float warning = _service.IsRunning ? _service.Clock.WarningProgress : 0f;
            float flash = Mathf.Clamp01(_flash / FlashSeconds);
            // A pulse that quickens through the warning, under a rising floor of light.
            float pulse = warning > 0f ? .55f + .45f * Mathf.Sin(Time.time * Mathf.Lerp(8f, 22f, warning)) : 0f;
            ShownAlpha = Mathf.Max(warning * (.35f + .65f * pulse), flash);
            _band.enabled = enabled && ShownAlpha > .01f;
            if (!_band.enabled) return;
            var hot = new Color(1f, Mathf.Lerp(.45f, .85f, flash), Mathf.Lerp(.12f, .5f, flash), ShownAlpha);
            _band.startColor = _band.endColor = hot;
            _band.startWidth = _band.endWidth = Mathf.Lerp(.04f, .12f, Mathf.Max(warning, flash));
        }

        private void OnDestroy() { if (_material != null) Destroy(_material); }
    }
}
