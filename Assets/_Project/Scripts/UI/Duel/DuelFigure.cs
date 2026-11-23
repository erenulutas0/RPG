using Cryptforge.Arena;
using Cryptforge.Art;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // One hero in the arena's lane, drawn with the run's own art as UI images (docs/55 section 11). A hero whose
    // HeroDefinition carries a pre-rendered set (RenderedCharacterArtSet) is drawn from it, as the run draws it: Ilya, and
    // the Vanguard since 2026-11-22 (VanguardRenderedArtSet, the Blender Vanguard of docs/08, its sword and buckler drawn
    // whatever the duel's weapon); the frames are chosen by the run's pure RenderedCharacterPlayer - walk by distance,
    // strike, cast, the Shield Rush's dash, flinch, death - and a blow taken shows the frame's silhouette in white over it,
    // RenderedCharacterAnimator's flash (a tint would darken the navy plate to a purple the player cannot read). Only a
    // hero without a rendered set falls back to the painted Vanguard and its painted weapons (VanguardArtSet, each weapon
    // in its hand as VanguardAnimator places it); without art a plain figure stands in.
    //
    // Presentation only: DuelPlaybackView tells it where the feet are, which way it faces, how far it walked this frame
    // and what the log said (a strike, a skill, a Shield Rush, a blow taken, the round lost); the playback's own clock
    // drives every motion, so x2 plays them twice as fast and a skip never plays them at all.
    internal sealed class DuelFigure
    {
        public const string FlashSuffix = " Flash";
        // Without the flash shader the frame is tinted this way instead (multiplying can only darken).
        private static readonly Color FallbackTint = new Color(1f, .5f, .45f, 1f);

        // UI units a world unit of the hero's art: the Vanguard's 1.375-unit figure stands about 260 units tall in the
        // lane, larger against the lane than in the run (the lane's unit is 80) so the two heroes read on a phone; at the
        // Sword's reach they meet, a little overlapped, as melee does.
        public const float Scale = 190f;
        // The painted Vanguard's walk: four poses a stride of this many lane units (VanguardAnimator's Stride).
        private const float PaintedStride = .8f;
        private const float PaintedStrikeSeconds = .2f;

        private readonly RectTransform _root;
        private readonly RectTransform _pose;
        private readonly Image _body;
        private readonly Image _flashImage;
        private readonly Image _right;
        private readonly Image _left;
        private readonly VanguardArtSet _painted;
        private readonly RenderedCharacterArtSet _rendered;
        private RenderedCharacterPlayer _player;
        private readonly string _weaponId;
        private readonly float _strikeSeconds;
        private float _distance;
        private bool _moving;
        private float _strikeLeft;
        private bool _flash;
        private bool _fallen;
        private float _fall;
        private int _facing = 1;

        public RectTransform Root => _root;
        public bool Fallen => _fallen;
        public Sprite BodySprite => _body.sprite;
        public Color BodyColour => _body.color;
        // The set it is drawn from: the rendered one, else the painted Vanguard (null when it has the other, or no art).
        public RenderedCharacterArtSet RenderedArt => _rendered;
        public VanguardArtSet PaintedArt => _painted;
        // What the rendered player shows now (Idle for a painted or plain figure).
        public RenderedMotion Motion => _player != null ? _player.Motion : RenderedMotion.Idle;
        public RenderedFacing Facing => _player != null ? _player.Facing : RenderedFacing.East;
        // The white silhouette over a rendered frame while a blow lands.
        public bool FlashShown => _flashImage != null && _flashImage.enabled;
        // The figure's height in UI units, for what stands over its head.
        public float Height { get; }

        public DuelFigure(ArenaUi ui, RectTransform parent, string name, string weaponId, VanguardArtSet painted,
            RenderedCharacterArtSet rendered)
        {
            _weaponId = weaponId;
            _painted = painted != null && painted.IsValid ? painted : null;
            _rendered = rendered != null && rendered.IsValid ? rendered : null;
            _root = ArenaUi.Node(parent, name);
            _root.anchorMin = _root.anchorMax = new Vector2(0f, 1f);
            _root.pivot = new Vector2(.5f, 0f);
            _root.sizeDelta = Vector2.zero;
            _pose = ArenaUi.Node(_root, name + " Pose");
            _pose.anchorMin = _pose.anchorMax = new Vector2(.5f, 0f);
            _pose.pivot = new Vector2(.5f, 0f);
            _pose.sizeDelta = Vector2.zero;
            // Side-on, the run draws the held weapons behind the body (VanguardAnimator's sorting): they come first.
            _right = ui.Part(_pose, name + " Right Hand", Color.white);
            _left = ui.Part(_pose, name + " Left Hand", Color.white);
            _body = ui.Part(_pose, name + " Body", Color.white);
            ArenaWeapon weapon = ArenaRules.Current.Weapon(weaponId);
            _strikeSeconds = RenderedCharacterFrames.StrikeSeconds(weapon != null ? weapon.IntervalTicks / (float)DuelRules.TicksPerSecond : 1f);
            if (_rendered != null)
            {
                _player = new RenderedCharacterPlayer(_rendered.Timings(), _rendered.StrideLength, _rendered.DrawsWest);
                _right.enabled = _left.enabled = false;
                Height = Measure(_rendered.Frame(RenderedMotion.Idle, RenderedFacing.East, 0));
                // The flash: the body's own rect, so it follows each frame's size and pivot, drawn over it.
                Material flash = ui.FlashMaterial;
                if (flash != null)
                {
                    _flashImage = ui.Part(_body.rectTransform, name + FlashSuffix, new Color(1f, 1f, 1f, RenderedCharacterAnimator.FlashAlpha));
                    _flashImage.material = flash;
                    ArenaUi.Stretch(_flashImage.rectTransform, Vector2.zero, Vector2.zero);
                    _flashImage.enabled = false;
                }
            }
            else if (_painted != null)
            {
                Height = Measure(_painted.GetFrame(0).Body);
                if (weaponId == ArenaRules.Staff)
                {
                    Set(_right, _painted.Staff);
                    _left.enabled = false;
                }
                else if (weaponId == ArenaRules.Daggers)
                {
                    Set(_right, _painted.Dagger);
                    Set(_left, _painted.Dagger);
                }
                else
                {
                    Set(_right, _painted.Sword);
                    Set(_left, _painted.Shield);
                }
            }
            else
            {
                // No art: a plain standing figure with a bar for the weapon.
                Height = 1.3f * Scale;
                _body.sprite = ui.White;
                _body.color = new Color(.78f, .76f, .72f, 1f);
                _body.rectTransform.pivot = new Vector2(.5f, 0f);
                _body.rectTransform.sizeDelta = new Vector2(.5f * Scale, Height);
                _right.sprite = ui.White;
                _right.color = new Color(.65f, .68f, .74f, 1f);
                _right.rectTransform.pivot = new Vector2(.5f, .1f);
                _right.rectTransform.sizeDelta = new Vector2(.08f * Scale, .9f * Scale);
                _right.rectTransform.anchoredPosition = new Vector2(.3f * Scale, .55f * Scale);
                _left.enabled = false;
            }
            Show();
        }

        private static float Measure(Sprite sprite) =>
            sprite != null ? sprite.rect.height / sprite.pixelsPerUnit * Scale : 1.3f * Scale;

        // A sprite at its own size and pivot, in the figure's scale.
        private static void Set(Image image, Sprite sprite)
        {
            image.enabled = sprite != null;
            if (sprite == null || image.sprite == sprite)
                return;
            image.sprite = sprite;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
            rect.sizeDelta = sprite.rect.size / sprite.pixelsPerUnit * Scale;
        }

        // Where the feet stand (the lane's UI units), which way it faces (1 right, -1 left) and how high a leap lifts it.
        public void Place(Vector2 feet, int facing, float lift)
        {
            _root.anchoredPosition = feet + new Vector2(0f, lift);
            if (!_moving && !_fallen && facing != 0)
                _facing = facing;
        }

        // The frame's walk: dt playback seconds, dx lane units walked.
        public void Step(float dt, float dx)
        {
            if (!(dt > 0f))
                return;
            _moving = Mathf.Abs(dx) > .0005f;
            if (_moving && !_fallen)
                _facing = dx > 0f ? 1 : -1;
            if (_player != null)
                _player.Step(dt, dx, 0f);
            if (_moving)
                _distance = (_distance + Mathf.Abs(dx)) % PaintedStride;
            if (_strikeLeft > 0f)
                _strikeLeft = Mathf.Max(0f, _strikeLeft - dt);
            if (_fallen)
                _fall = Mathf.Min(1f, _fall + dt * 4f);
            Show();
        }

        public void Strike()
        {
            if (_fallen)
                return;
            _strikeLeft = _player != null ? _strikeSeconds : PaintedStrikeSeconds;
            _player?.Strike(_strikeSeconds);
            Show();
        }

        public void Cast()
        {
            if (_fallen)
                return;
            if (_player != null)
                _player.Cast();
            else
                _strikeLeft = PaintedStrikeSeconds;
            Show();
        }

        // docs/57's Shield Rush: a set that draws the dash plays it once on the playback's clock, over the leap the log
        // draws (the Vanguard's, as RenderedCharacterAnimator plays it in the run); any other figure plays its cast.
        public void Rush()
        {
            if (_fallen)
                return;
            if (_player != null && _player.Dash())
            {
                Show();
                return;
            }
            Cast();
        }

        public void Hit() => _player?.Hit();

        public void Flash(bool on)
        {
            if (_flash == on)
                return;
            _flash = on;
            Show();
        }

        // The round lost: Ilya plays its death, the painted Vanguard sinks to the floor.
        public void Fall()
        {
            if (_fallen)
                return;
            _fallen = true;
            _fall = 0f;
            _player?.Die();
            Show();
        }

        // A new round: standing, idle, facing the foe.
        public void Begin(int facing)
        {
            _fallen = false;
            _fall = 0f;
            _flash = false;
            _strikeLeft = 0f;
            _distance = 0f;
            _moving = false;
            _facing = facing;
            // The rendered player holds a death; a fresh one stands again.
            if (_rendered != null)
                _player = new RenderedCharacterPlayer(_rendered.Timings(), _rendered.StrideLength, _rendered.DrawsWest);
            Show();
        }

        private void Show()
        {
            _pose.localScale = new Vector3(_facing, 1f, 1f);
            _pose.localRotation = Quaternion.identity;
            if (_rendered != null)
                ShowRendered();
            else if (_painted != null)
                ShowPainted();
            else
                _body.color = _flash ? Color.white : new Color(.78f, .76f, .72f, _fallen ? .4f : 1f);
            if (_rendered == null && _fallen)
            {
                // The painted figure has no fall: it tips over backwards about its feet and greys.
                _pose.localRotation = Quaternion.Euler(0f, 0f, 80f * _facing * _fall);
                _body.color = Color.Lerp(Color.white, new Color(.45f, .45f, .5f, 1f), _fall);
            }
        }

        private void ShowRendered()
        {
            RenderedCharacterPlayer player = _player;
            if (!player.IsDying && !player.IsMoving)
                player.FaceToward(_facing, 0f);
            Sprite frame = _rendered.Frame(player.Motion, player.Facing, player.Frame);
            Set(_body, frame);
            // The rendered set mirrors the east for the west unless it draws the west itself: the pose's scale already
            // faces the figure, so the facing the player chose is drawn unflipped.
            _pose.localScale = new Vector3(player.Mirrored ? -1f : 1f, 1f, 1f);
            if (_flashImage == null)
            {
                _body.color = _flash ? FallbackTint : Color.white;
                return;
            }
            _body.color = Color.white;
            bool on = _flash && !player.IsDying;
            if (on && _flashImage.sprite != frame)
                _flashImage.sprite = frame;
            if (_flashImage.enabled != on)
                _flashImage.enabled = on;
        }

        private void ShowPainted()
        {
            int index;
            float t = _strikeLeft > 0f ? 1f - _strikeLeft / PaintedStrikeSeconds : 0f;
            bool sword = _weaponId != ArenaRules.Staff && _weaponId != ArenaRules.Daggers;
            if (_strikeLeft > 0f && !_moving && sword)
                index = t < .42f ? 6 : 7;
            else if (_moving && !_fallen)
                index = 1 + Mathf.Min(3, (int)(_distance / PaintedStride * 4f));
            else
                index = 0;
            VanguardArtSet.Frame frame = _painted.GetFrame(index);
            Set(_body, _flash && frame.Flash != null ? frame.Flash : frame.Body);
            if (!_fallen)
                _body.color = Color.white;
            float pulse = _strikeLeft > 0f ? Mathf.Sin(t * Mathf.PI) : 0f;
            _right.rectTransform.anchoredPosition = frame.RightHand * Scale;
            _left.rectTransform.anchoredPosition = frame.LeftHand * Scale;
            if (_weaponId == ArenaRules.Staff)
            {
                _right.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -14f * pulse);
            }
            else if (_weaponId == ArenaRules.Daggers)
            {
                float angle = 35f + 12f * pulse;
                _left.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
                _right.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -angle);
            }
            else
            {
                _right.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _strikeLeft > 0f ? Mathf.Lerp(-50f, 0f, t) : 0f);
                _left.rectTransform.localRotation = Quaternion.identity;
            }
        }
    }
}
