using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only: the pit's danger band - the line PitEruptionRule.Reach beyond the hole's edge, a rounded square
    // on the floor - glows in as the warning runs, brightening and thickening to the spill, then flashes and fades.
    // Through the warning every enemy the slag holds in the band gets a molten grip at its feet: a ring pulsing with the
    // band and a jagged crust closing in on the enemy to the spill (a held enemy otherwise looked like one merely
    // standing, so a player could not tell the pit had it). PitEruptionService owns the clock, the hold and the burn.
    [DefaultExecutionOrder(100)]
    public sealed class PitEruptionView : MonoBehaviour
    {
        private const int CornerSteps = 8;
        private const float FlashSeconds = .45f;
        private const int GripSegments = 24;
        private const float GripRadius = .5f;
        private PitEruptionService _service;
        private EncounterController _encounters;
        private ArenaView _arena;
        private Material _material;
        private LineRenderer _band;
        // One grip per pack slot, made up front so the warning only moves and recolours them.
        private Transform[] _grips;
        private LineRenderer[] _rings;
        private LineRenderer[] _crusts;
        private float _flash;
        public float ShownAlpha { get; private set; }
        // How many held enemies show a grip this frame.
        public int ShownHeldCount { get; private set; }

        internal void Initialize(PitEruptionService service)
        {
            _service = service;
            if (!service.IsActive) return;
            _material = new Material(Shader.Find("Sprites/Default"));
            _band = new GameObject("Pit Eruption Band").AddComponent<LineRenderer>();
            _band.transform.SetParent(transform, false);
            _band.sharedMaterial = _material; _band.useWorldSpace = true; _band.loop = true; _band.numCapVertices = 2;
            _band.sortingOrder = 0;
            _encounters = service.Encounters;
            var arena = _arena = FindFirstObjectByType<ArenaView>();
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
            _grips = new Transform[PackLayout.MaxPackSize];
            _rings = new LineRenderer[_grips.Length]; _crusts = new LineRenderer[_grips.Length];
            for (int i = 0; i < _grips.Length; i++)
            {
                var grip = new GameObject("Pit Grip " + i).transform; grip.SetParent(transform, false);
                _rings[i] = Loop("Pit Grip Ring", grip, false);
                _crusts[i] = Loop("Pit Grip Crust", grip, true);
                grip.gameObject.SetActive(false);
                _grips[i] = grip;
            }
            _service.Erupted += () => _flash = FlashSeconds;
            Refresh();
        }

        // A closed floor ellipse of GripRadius around the grip's centre; the crust alternates its points inward into teeth.
        private LineRenderer Loop(string name, Transform parent, bool jagged)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(parent, false);
            line.sharedMaterial = _material; line.useWorldSpace = false; line.loop = true; line.numCapVertices = 2;
            line.sortingOrder = 0; line.positionCount = GripSegments;
            for (int k = 0; k < GripSegments; k++)
            {
                float a = k * Mathf.PI * 2 / GripSegments, r = GripRadius * (jagged && k % 2 == 1 ? .62f : 1f);
                line.SetPosition(k, new Vector3(Mathf.Cos(a) * r, ArenaFloor.WorldY(Mathf.Sin(a) * r), 0));
            }
            return line;
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
            if (_band.enabled)
            {
                var hot = new Color(1f, Mathf.Lerp(.45f, .85f, flash), Mathf.Lerp(.12f, .5f, flash), ShownAlpha);
                _band.startColor = _band.endColor = hot;
                _band.startWidth = _band.endWidth = Mathf.Lerp(.04f, .12f, Mathf.Max(warning, flash));
            }
            ShowGrips(warning, pulse);
        }

        // The same test PitEruptionService gives the pack (PackHeld): a living wave enemy whose floor point lies in the band
        // while a fight's warning runs.
        private void ShowGrips(float warning, float pulse)
        {
            int shown = 0;
            if (enabled && _encounters != null && _service.IsRunning && _service.Clock.IsWarning)
                for (int i = 0; i < _encounters.WaveEnemyCount && shown < _grips.Length; i++)
                {
                    Health enemy = _encounters.WaveEnemyAt(i);
                    if (enemy == null || !enemy.IsAlive || !_encounters.TryFloorPositionOf(enemy, out float x, out float y)
                        || !PitEruptionRule.InBand(x, y)) continue;
                    var p = new Vector3(x, ArenaFloor.WorldY(y), 0);
                    if (_arena != null) p.y += FoundrySurface.Lift(p, _arena.transform.position);
                    Transform grip = _grips[shown];
                    grip.position = p;
                    if (!grip.gameObject.activeSelf) grip.gameObject.SetActive(true);
                    float alpha = .5f + .5f * pulse;
                    LineRenderer ring = _rings[shown], crust = _crusts[shown];
                    ring.startColor = ring.endColor = new Color(1f, .5f, .14f, alpha);
                    ring.startWidth = ring.endWidth = Mathf.Lerp(.035f, .07f, warning);
                    // The crust starts on the ring and tightens toward the feet as the spill nears.
                    crust.startColor = crust.endColor = new Color(1f, .8f, .4f, alpha * Mathf.Lerp(.55f, 1f, warning));
                    crust.startWidth = crust.endWidth = Mathf.Lerp(.025f, .045f, warning);
                    float closing = Mathf.Lerp(1f, .55f, warning);
                    crust.transform.localScale = new Vector3(closing, closing, 1f);
                    shown++;
                }
            for (int i = shown; i < _grips.Length; i++)
                if (_grips[i].gameObject.activeSelf) _grips[i].gameObject.SetActive(false);
            ShownHeldCount = shown;
        }

        private void OnDestroy() { if (_material != null) Destroy(_material); }
    }
}
