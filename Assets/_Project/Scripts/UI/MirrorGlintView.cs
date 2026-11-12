using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only: the Mirror Sentry's mirror (MirrorGuardRule; docs/50 section 5, "Readability"). The Sentry's art
    // faces four ways, which cannot show a turn of 45 degrees a second, so each standing mirror wears a short violet arc on
    // the ground at its feet, turned to the guard's exact facing (MirrorGuardState.Facing) and as wide as its front arc:
    // where the hero stands inside it, a landing is blunted. It is violet, never red-orange, because standing
    // there does not hurt on a timer (docs/50 section 2 rule 3: red-orange means "this floor will hurt"; violet never does).
    // A landing the mirror blunted (MirrorGuardService.Blunted, at most one every BluntGlintSeconds) draws a three-frame
    // violet glint from the mirror toward the hero - a look only, it deals nothing; a reflection paid, where a research
    // tuning reflects (MirrorGuardService.Reflected), draws the same, and the hero flashes as for any hit (CombatantView).
    // Pooled: an arc a standing mirror, grown as mirrors stand up, and a few glints reused in turn - nothing is made or
    // destroyed in a fight. Only where the service steps mirrors (the foundry).
    [DefaultExecutionOrder(100)]
    public sealed class MirrorGlintView : MonoBehaviour
    {
        // The arc: its radius round the Sentry's feet on the floor, its points and its line.
        public const float ArcRadius = .95f;
        private const int ArcSegments = 13;
        private const float ArcWidth = .08f;
        // The glint: from the mirror - this far toward the facing and this high above the feet - to the hero's chest.
        private const float MirrorReach = .3f;
        private const float MirrorHeight = .6f;
        private const float HeroChest = .45f;
        public const int GlintFrames = 3;
        public const float GlintFrameSeconds = .06f;
        private const int GlintPool = 4;
        private const float GlintWidth = .07f;
        // #8B6CE8, the Wisp's and the frosted palette's violet (docs/49), and a paler flash of it for the glint.
        public static readonly Color Violet = new Color(139f / 255f, 108f / 255f, 232f / 255f, .75f);
        private static readonly Color Flash = new Color(.86f, .8f, 1f, 1f);

        private MirrorGuardService _service;
        private EncounterController _encounter;
        private Health _hero;
        private ArenaView _arena;
        private Material _material;
        private int _groundOrder;
        private readonly List<LineRenderer> _arcs = new List<LineRenderer>(2);
        private readonly Vector3[] _arcPoints = new Vector3[ArcSegments];
        private readonly LineRenderer[] _glints = new LineRenderer[GlintPool];
        private readonly int[] _glintFrame = new int[GlintPool];
        private readonly float[] _glintClock = new float[GlintPool];
        private int _nextGlint;

        // What the view shows now, for the tests: the arcs drawn, the facing the first was drawn at (radians, NaN for
        // none), and the glints begun since the run began.
        public int ShownArcs { get; private set; }
        public float FirstArcFacing { get; private set; } = float.NaN;
        public int GlintsBegun { get; private set; }

        internal void Initialize(MirrorGuardService service, EncounterController encounter, Health hero)
        {
            _service = service;
            _encounter = encounter;
            _hero = hero;
            if (_service == null || !_service.IsActive)
                return;
            _arena = FindFirstObjectByType<ArenaView>();
            _material = SharedSpriteMaterial.For(_arena);
            _groundOrder = _arena != null && _arena.IsFoundryRegion ? _arena.GroundEffectSortingOrder : 0;
            for (int i = 0; i < _glints.Length; i++)
            {
                _glints[i] = Line("Mirror Glint " + (i + 1), 2, GlintWidth, 6);
                _glints[i].gameObject.SetActive(false);
            }
            _service.Reflected += OnReflected;
            _service.Blunted += Glint;
        }

        private LineRenderer Line(string name, int points, float width, int order)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.sharedMaterial = _material;
            line.useWorldSpace = false;
            line.loop = false;
            line.numCapVertices = 2;
            line.sortingOrder = order;
            line.positionCount = points;
            line.startWidth = line.endWidth = width;
            return line;
        }

        // Where a body stands on the floor, in world space, lifted onto the foundry's ramp as every ground contact is.
        private Vector3 Feet(float floorX, float floorY)
        {
            var p = new Vector3(floorX, ArenaFloor.WorldY(floorY), 0f);
            if (_arena != null && _arena.IsFoundryRegion)
                p.y += FoundrySurface.Lift(p, _arena.transform.position);
            return p;
        }

        private void OnReflected(MirrorGuard from, float amount) => Glint(from);

        // The glint from the mirror of from toward the hero, its first frame drawn this frame.
        private void Glint(MirrorGuard from)
        {
            if (from == null || _hero == null || !_encounter.TryFloorPositionOf(from.Body, out float x, out float y))
                return;
            float facing = from.State.Facing;
            float reach = float.IsNaN(facing) ? 0f : MirrorReach;
            Vector3 mirror = Feet(x + reach * Mathf.Cos(facing), y + reach * Mathf.Sin(facing)) + Vector3.up * MirrorHeight;
            Vector3 heroAt = _hero.transform.position + Vector3.up * HeroChest;
            LineRenderer glint = _glints[_nextGlint];
            _glintFrame[_nextGlint] = 0;
            _glintClock[_nextGlint] = 0f;
            _nextGlint = (_nextGlint + 1) % _glints.Length;
            glint.transform.position = mirror;
            glint.SetPosition(0, Vector3.zero);
            glint.SetPosition(1, heroAt - mirror);
            glint.gameObject.SetActive(true);
            GlintsBegun++;
        }

        private void LateUpdate()
        {
            if (_service == null || !_service.IsActive)
                return;
            DrawArcs();
            DrawGlints();
        }

        private void DrawArcs()
        {
            int shown = 0;
            FirstArcFacing = float.NaN;
            for (int i = 0; i < MirrorGuard.StandingCount; i++)
            {
                MirrorGuard guard = MirrorGuard.StandingAt(i);
                Health body = guard.Body;
                if (!guard.State.IsGuarding || body == null || !body.IsAlive ||
                    !_encounter.TryFloorPositionOf(body, out float x, out float y))
                    continue;
                if (shown == _arcs.Count)
                    _arcs.Add(Line("Mirror Arc " + (shown + 1), ArcSegments, ArcWidth, _groundOrder));
                LineRenderer arc = _arcs[shown];
                if (!arc.gameObject.activeSelf)
                    arc.gameObject.SetActive(true);
                float facing = guard.State.Facing;
                if (shown == 0)
                    FirstArcFacing = facing;
                float half = guard.State.ArcDegrees * .5f * Mathf.Deg2Rad;
                for (int k = 0; k < ArcSegments; k++)
                {
                    float a = facing - half + 2f * half * k / (ArcSegments - 1);
                    _arcPoints[k] = new Vector3(ArcRadius * Mathf.Cos(a), ArenaFloor.WorldY(ArcRadius * Mathf.Sin(a)), 0f);
                }
                arc.transform.position = Feet(x, y);
                arc.SetPositions(_arcPoints);
                // A slow shimmer along the mirror's edge: a look, never a countdown.
                Color edge = Violet;
                edge.a = Violet.a * (.8f + .2f * Mathf.Sin(Time.time * 3f));
                arc.startColor = arc.endColor = edge;
                shown++;
            }
            for (int i = shown; i < _arcs.Count; i++)
                if (_arcs[i].gameObject.activeSelf)
                    _arcs[i].gameObject.SetActive(false);
            ShownArcs = shown;
        }

        // Each glint shows GlintFrames frames of GlintFrameSeconds - full, fading, faint - then goes.
        private void DrawGlints()
        {
            for (int i = 0; i < _glints.Length; i++)
            {
                LineRenderer glint = _glints[i];
                if (!glint.gameObject.activeSelf)
                    continue;
                _glintClock[i] += Time.deltaTime;
                while (_glintClock[i] >= GlintFrameSeconds && _glintFrame[i] < GlintFrames)
                {
                    _glintClock[i] -= GlintFrameSeconds;
                    _glintFrame[i]++;
                }
                if (_glintFrame[i] >= GlintFrames)
                {
                    glint.gameObject.SetActive(false);
                    continue;
                }
                float fade = 1f - _glintFrame[i] / (float)GlintFrames;
                Color color = Color.Lerp(Violet, Flash, fade);
                color.a = fade;
                glint.startColor = color;
                glint.endColor = new Color(color.r, color.g, color.b, color.a * .5f);
                glint.startWidth = GlintWidth * (.6f + .4f * fade);
                glint.endWidth = glint.startWidth * .5f;
            }
        }

        private void OnDestroy()
        {
            if (_service != null)
                _service.Reflected -= OnReflected;
                _service.Blunted -= Glint;
            // The material is shared (SharedSpriteMaterial), not this view's to destroy.
        }
    }
}
