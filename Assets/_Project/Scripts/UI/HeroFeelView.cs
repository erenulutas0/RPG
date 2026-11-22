using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only - the game-feel pass on the hero (docs/43 section 1, 2026-11-22). The body swings with the first
    // weapon alone (AttackController.Attacked; docs/42, risks), so a weapon fired from slot 1 or 2 shows a small cue of its
    // own at the moment it strikes (AttackController.StruckWith): the Sword an off-hand flourish - a ghost of the blade
    // sweeping from behind the off shoulder over the top toward the struck enemy, a steel arc trailing it; the Staff a
    // raised glow - a ghost staff lifted at the off side, a glow swelling at its tip and a thin ring spreading from it; the
    // Daggers a quick double flick - two ghost blades flicking out at the waist one after the other, each with its streak.
    // Which cue a weapon plays is GameFeel.CueFor its strike behaviour, as CombatEffectsView picks its strike effect; its
    // length GameFeel.CueSeconds, inside the weapon's interval.
    //
    // A cue never hides the hero or an enemy: its ghosts draw behind the whole figure, inside the hero's sorting group, so
    // only what reaches past the silhouette shows; its trails and glows draw over it but thin and translucent, at the
    // off shoulder, the staff's tip or the waist - never the face - and every cue is gone within a fifth of a second or
    // so. The ghosts are the weapons' existing sprites (the puppet's painted ones, else the painted frames', else the
    // loadout's own), no new frame; the glow is FeelArt's code-drawn disc.
    //
    // Everything is made once, under one child of the hero (RootName), one rig a cue, and only moved, recoloured and shown
    // after that: no allocation per strike or per frame. Being a child of the hero it moves with it, sorts inside its
    // group and stands at its depth on the foundry (FoundryVisibilityView moves an actor's children). Scaled time: a pause
    // holds a cue as it holds the strike. It reads the attacker and writes nothing to it.
    [DisallowMultipleComponent]
    public sealed class HeroFeelView : MonoBehaviour
    {
        public const string RootName = "Hero Feel";
        // Inside the hero's sorting group: ghosts behind every part the puppet paints (its parts sort -20 to +11 about the
        // body, its held items a little past), trails and glows over every part.
        internal const int BehindFigure = 40;
        internal const int OverFigure = 40;
        private const int TrailPoints = 9;
        private const int StreakPoints = 5;
        private const int RingPoints = 18;

        // The flourish: its pivot behind the off shoulder (hero-local, facing right), the trail's share of the sweep behind
        // the blade, the ghost's and the trail's strength at full.
        private const float FlourishPivotX = -.1f;
        private const float FlourishPivotY = .8f;
        private const float FlourishTrailLag = .35f;
        private const float FlourishGhostAlpha = .8f;
        private const float FlourishTrailAlpha = .6f;
        private const float FlourishTrailWidth = .05f;
        // The pulse: the staff stands at the off side, raised this far and tilted out; its tip's glow swells from and to
        // these radii, its ring spreads from and to these.
        private const float PulseX = -.3f;
        private const float PulseY = .22f;
        private const float PulseRaise = .14f;
        private const float PulseTilt = 12f;
        private const float PulseGhostAlpha = .75f;
        private const float PulseGlowFrom = .07f;
        private const float PulseGlowTo = .27f;
        private const float PulseGlowAlpha = .9f;
        private const float PulseRingFrom = .05f;
        private const float PulseRingTo = .34f;
        private const float PulseRingAlpha = .7f;
        private const float PulseRingWidth = .03f;
        // The flick: two blades at the waist on the struck side, the second a little higher and StaggerShare of the cue
        // later, each flicking over FlickShare of it.
        private const float FlickX = .17f;
        private const float FlickY = .42f;
        private const float FlickRise = .12f;
        private const float FlickStagger = .38f;
        private const float FlickShare = .62f;
        private const float FlickGhostAlpha = .85f;
        private const float FlickStreakAlpha = .6f;
        private const float FlickStreakWidth = .04f;
        // A ghost's reach from its grip, as its sprite is drawn, held to a sensible length.
        private const float MinimumReach = .25f;
        private const float MaximumReach = .8f;

        // The Staff's own violet (StaffSplashArt's arcs), the Sword's and the Daggers' steel.
        private static readonly Color Steel = new Color(.9f, .95f, 1f, 1f);
        private static readonly Color StaffGlow = new Color(.91f, .79f, 1f, 1f);
        private static readonly Color StaffRing = new Color(.62f, .45f, .98f, 1f);

        private sealed class Cue
        {
            public SlotCue Kind;
            public SpriteRenderer[] Sprites;
            public LineRenderer[] Lines;
            public Vector3[][] Points;
            public bool Active;
            public float Elapsed;
            public float Seconds;
            public float Side;
            public float Reach;
            public int Shown;
        }

        private AttackController _attack;
        private HeroLookView _look;
        private Transform _root;
        private Material _material;
        private Cue[] _cues;
        private float _side = 1f;
        private bool _subscribed;
        private System.Action<Health, WeaponRuntime> _onStruckWith;

        // The child every cue lives under, for tests and the perf census.
        internal Transform Root => _root;
        public bool IsCueShowing(SlotCue cue) => _cues != null && _cues[(int)cue].Active;
        // How many times each cue has played this scene.
        public int CuesShown(SlotCue cue) => _cues != null ? _cues[(int)cue].Shown : 0;
        // A cue's renderers, for tests: its ghosts and glows, then its trails.
        internal SpriteRenderer[] CueSprites(SlotCue cue) => _cues[(int)cue].Sprites;
        internal LineRenderer[] CueLines(SlotCue cue) => _cues[(int)cue].Lines;

        internal void Initialize(AttackController attack)
        {
            if (_root != null)
                return;
            _attack = attack;
            _look = GetComponent<HeroLookView>();
            _material = SharedSpriteMaterial.For(FindFirstObjectByType<ArenaView>());
            _root = new GameObject(RootName).transform;
            _root.SetParent(transform, false);
            SpriteRenderer body = _look != null ? _look.Body : null;
            int layer = body != null ? body.sortingLayerID : 0;
            int order = body != null ? body.sortingOrder : 0;
            _cues = new Cue[3];
            _cues[(int)SlotCue.Flourish] = Build(SlotCue.Flourish, 1, 1, TrailPoints, layer, order);
            _cues[(int)SlotCue.Pulse] = Build(SlotCue.Pulse, 2, 1, RingPoints, layer, order);
            _cues[(int)SlotCue.Flick] = Build(SlotCue.Flick, 2, 2, StreakPoints, layer, order);
            Line(SlotCue.Pulse, 0).loop = true;
            // The glow is the disc, over the figure.
            SpriteRenderer glow = _cues[(int)SlotCue.Pulse].Sprites[1];
            glow.sprite = FeelArt.Disc();
            glow.sortingOrder = order + OverFigure;
            Subscribe();
        }

        private LineRenderer Line(SlotCue cue, int index) => _cues[(int)cue].Lines[index];

        private Cue Build(SlotCue kind, int sprites, int lines, int points, int layer, int order)
        {
            var cue = new Cue
            {
                Kind = kind,
                Sprites = new SpriteRenderer[sprites],
                Lines = new LineRenderer[lines],
                Points = new Vector3[lines][]
            };
            for (int i = 0; i < sprites; i++)
            {
                var sprite = new GameObject(kind + " Ghost " + i).AddComponent<SpriteRenderer>();
                sprite.transform.SetParent(_root, false);
                sprite.sharedMaterial = _material;
                sprite.sortingLayerID = layer;
                sprite.sortingOrder = order - BehindFigure;
                sprite.enabled = false;
                cue.Sprites[i] = sprite;
            }
            for (int i = 0; i < lines; i++)
            {
                var line = new GameObject(kind + " Trail " + i).AddComponent<LineRenderer>();
                line.transform.SetParent(_root, false);
                line.sharedMaterial = _material;
                line.useWorldSpace = false;
                line.numCapVertices = 2;
                line.positionCount = points;
                line.sortingLayerID = layer;
                line.sortingOrder = order + OverFigure;
                line.enabled = false;
                cue.Lines[i] = line;
                cue.Points[i] = new Vector3[points];
            }
            return cue;
        }

        private void OnEnable() => Subscribe();

        private void OnDisable()
        {
            Unsubscribe();
            if (_cues == null)
                return;
            for (int i = 0; i < _cues.Length; i++)
                End(_cues[i]);
        }

        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (_subscribed || _attack == null)
                return;
            _attack.StruckWith += _onStruckWith ??= OnStruckWith;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;
            if (_attack != null)
                _attack.StruckWith -= _onStruckWith;
            _subscribed = false;
        }

        // The first weapon's strike is the body's swing: only the weapons after it cue.
        private void OnStruckWith(Health target, WeaponRuntime weapon)
        {
            if (weapon == null || ReferenceEquals(weapon, _attack.Weapon) || !isActiveAndEnabled)
                return;
            Play(GameFeel.CueFor(weapon.Pattern.Behavior), target, weapon.Interval);
        }

        // Starts (or restarts) a cue toward the struck target. Internal for the capture tool, which plays each cue on a
        // standing hero.
        internal void Play(SlotCue kind, Health target, float interval)
        {
            if (_cues == null)
                return;
            if (target != null)
            {
                float dx = target.transform.position.x - transform.position.x;
                if (Mathf.Abs(dx) > .05f)
                    _side = dx > 0f ? 1f : -1f;
            }
            Cue cue = _cues[(int)kind];
            cue.Active = true;
            cue.Elapsed = 0f;
            cue.Seconds = GameFeel.CueSeconds(kind, interval);
            cue.Side = _side;
            cue.Shown++;
            Sprite ghost = GhostOf(kind);
            cue.Reach = ghost != null ? Mathf.Clamp(ghost.bounds.max.y, MinimumReach, MaximumReach) : .45f;
            int ghosts = kind == SlotCue.Pulse ? 1 : cue.Sprites.Length;
            for (int i = 0; i < ghosts; i++)
            {
                cue.Sprites[i].sprite = ghost;
                cue.Sprites[i].flipX = _side < 0f;
            }
            Draw(cue);
        }

        private Sprite GhostOf(SlotCue kind)
        {
            if (_look == null)
                return null;
            HeroWeaponPart part = kind == SlotCue.Flourish ? HeroWeaponPart.Sword
                : kind == SlotCue.Pulse ? HeroWeaponPart.StaffShaft
                : HeroWeaponPart.DaggerRight;
            return _look.WeaponSprite(part);
        }

        private void LateUpdate()
        {
            if (_cues == null)
                return;
            using (PerformanceMarkers.LateHeroFeel.Auto())
                Tick(Time.deltaTime);
        }

        private void Tick(float dt)
        {
            if (dt <= 0f)
                return;
            for (int i = 0; i < _cues.Length; i++)
            {
                Cue cue = _cues[i];
                if (!cue.Active)
                    continue;
                cue.Elapsed += dt;
                if (cue.Elapsed >= cue.Seconds)
                {
                    End(cue);
                    continue;
                }
                Draw(cue);
            }
        }

        private void Draw(Cue cue)
        {
            float t = cue.Seconds > 0f ? Mathf.Clamp01(cue.Elapsed / cue.Seconds) : 1f;
            switch (cue.Kind)
            {
                case SlotCue.Flourish:
                    DrawFlourish(cue, t);
                    break;
                case SlotCue.Pulse:
                    DrawPulse(cue, t);
                    break;
                default:
                    DrawFlick(cue, t);
                    break;
            }
        }

        // A blade direction for the hero facing right, turned to the cue's side.
        private static float Toward(float degrees, float side) => side > 0f ? degrees : 180f - degrees;

        private static Vector3 Along(Vector3 from, float degrees, float reach)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector3(from.x + Mathf.Cos(radians) * reach, from.y + Mathf.Sin(radians) * reach, 0f);
        }

        private void DrawFlourish(Cue cue, float t)
        {
            float strength = GameFeel.CueEnvelope(t);
            var pivot = new Vector3(cue.Side * FlourishPivotX, FlourishPivotY, 0f);
            float angle = Toward(GameFeel.FlourishAngle(t), cue.Side);
            ShowGhost(cue.Sprites[0], pivot, angle, Steel, FlourishGhostAlpha * strength);
            // The arc behind the blade's tip, from where it was a share of the sweep ago to where it is.
            float tail = Mathf.Max(0f, t - FlourishTrailLag);
            Vector3[] points = cue.Points[0];
            float reach = cue.Reach * .9f;
            for (int i = 0; i < points.Length; i++)
            {
                float u = Mathf.Lerp(tail, t, i / (points.Length - 1f));
                points[i] = Along(pivot, Toward(GameFeel.FlourishAngle(u), cue.Side), reach);
            }
            ShowLine(cue.Lines[0], points, Steel, 0f, FlourishTrailAlpha * strength, .005f, FlourishTrailWidth * strength);
        }

        private void DrawPulse(Cue cue, float t)
        {
            float strength = GameFeel.CueEnvelope(t);
            float lift = PulseRaise * EaseOut(Mathf.Clamp01(t / .35f));
            var grip = new Vector3(cue.Side * PulseX, PulseY + lift, 0f);
            // Tilted out, away from the body: the off side is -Side, so its top leans further that way.
            float tilt = cue.Side * PulseTilt;
            ShowGhost(cue.Sprites[0], grip, 90f + tilt, Color.white, PulseGhostAlpha * strength);
            Vector3 tip = Along(grip, 90f + tilt, cue.Reach);
            SpriteRenderer glow = cue.Sprites[1];
            float radius = Mathf.Lerp(PulseGlowFrom, PulseGlowTo, EaseOut(t));
            glow.transform.localPosition = tip;
            glow.transform.localRotation = Quaternion.identity;
            glow.transform.localScale = new Vector3(radius, radius, 1f);
            Color colour = StaffGlow;
            colour.a = PulseGlowAlpha * strength * (1f - .5f * t);
            glow.color = colour;
            glow.enabled = true;
            Vector3[] ring = cue.Points[0];
            float spread = Mathf.Lerp(PulseRingFrom, PulseRingTo, EaseOut(t));
            for (int i = 0; i < ring.Length; i++)
            {
                float a = i * Mathf.PI * 2f / ring.Length;
                ring[i] = new Vector3(tip.x + Mathf.Cos(a) * spread, tip.y + Mathf.Sin(a) * spread, 0f);
            }
            float fade = PulseRingAlpha * (1f - t);
            ShowLine(cue.Lines[0], ring, StaffRing, fade, fade, PulseRingWidth, PulseRingWidth);
        }

        private void DrawFlick(Cue cue, float t)
        {
            for (int k = 0; k < 2; k++)
            {
                float own = (t - k * FlickStagger) / FlickShare;
                SpriteRenderer blade = cue.Sprites[k];
                LineRenderer streak = cue.Lines[k];
                if (own < 0f || own >= 1f)
                {
                    blade.enabled = false;
                    streak.enabled = false;
                    continue;
                }
                float strength = GameFeel.CueEnvelope(own);
                var grip = new Vector3(cue.Side * FlickX, FlickY + k * FlickRise, 0f);
                // The first flicks from up and back down to forward, the second from level to forward and down.
                float from = k == 0 ? 120f : 75f;
                float to = k == 0 ? 15f : -30f;
                float angle = Toward(Mathf.Lerp(from, to, EaseOut(own)), cue.Side);
                ShowGhost(blade, grip, angle, Steel, FlickGhostAlpha * strength);
                Vector3[] points = cue.Points[k];
                float tail = Mathf.Max(0f, own - .5f);
                for (int i = 0; i < points.Length; i++)
                {
                    float u = Mathf.Lerp(tail, own, i / (points.Length - 1f));
                    points[i] = Along(grip, Toward(Mathf.Lerp(from, to, EaseOut(u)), cue.Side), cue.Reach * .85f);
                }
                ShowLine(streak, points, Steel, 0f, FlickStreakAlpha * strength, .004f, FlickStreakWidth * strength);
            }
        }

        // A ghost on its grip, its blade along the direction (degrees; the sprites are drawn blade up). A cue whose weapon
        // has no sprite shows its trails alone.
        private static void ShowGhost(SpriteRenderer ghost, Vector3 grip, float degrees, Color colour, float alpha)
        {
            if (ghost.sprite == null)
            {
                ghost.enabled = false;
                return;
            }
            Transform at = ghost.transform;
            at.localPosition = grip;
            at.localRotation = Quaternion.Euler(0f, 0f, degrees - 90f);
            at.localScale = Vector3.one;
            colour.a = alpha;
            ghost.color = colour;
            ghost.enabled = true;
        }

        private static void ShowLine(LineRenderer line, Vector3[] points, Color colour, float tailAlpha, float headAlpha,
            float tailWidth, float headWidth)
        {
            line.SetPositions(points);
            Color tail = colour;
            tail.a = tailAlpha;
            Color head = colour;
            head.a = headAlpha;
            line.startColor = tail;
            line.endColor = head;
            line.startWidth = tailWidth;
            line.endWidth = headWidth;
            line.enabled = true;
        }

        private static void End(Cue cue)
        {
            cue.Active = false;
            for (int i = 0; i < cue.Sprites.Length; i++)
                if (cue.Sprites[i] != null)
                    cue.Sprites[i].enabled = false;
            for (int i = 0; i < cue.Lines.Length; i++)
                if (cue.Lines[i] != null)
                    cue.Lines[i].enabled = false;
        }

        private static float EaseOut(float t)
        {
            float inverse = 1f - Mathf.Clamp01(t);
            return 1f - inverse * inverse;
        }
    }
}
