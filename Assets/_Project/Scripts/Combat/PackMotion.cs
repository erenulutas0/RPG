using System;
using Cryptforge.Art;

namespace Cryptforge.Combat
{
    // Walks one wave's enemies across the arena floor to the hero. Each enemy enters at one of the four corners and heads
    // for its own point just inside its reach, on an arc round the hero spread by where it entered, and stops once it is
    // that close; when the hero moves, the point moves with the hero and the enemy walks again. On a platform the point
    // never hangs over the void: when the hero stands at the rim, the enemy turns round the hero to the nearest point that
    // lies on the platform. It waits whenever its next step would bring it nearer than the spacing to an enemy that is
    // closer to the hero. Enemies step nearest first, so the result never depends on update order, and the scene and the
    // Descent simulation, which both use this class, move every enemy identically.
    public sealed class PackMotion
    {
        // How far inside its reach an enemy stops, so rounding can never leave it exactly on the edge.
        public const float ReachMargin = 0.1f;
        // An enemy entering at the pack's widest slot approaches from this many degrees off its corner's centre line.
        public const float MaxApproachDegrees = 60f;
        // Turning round the hero to find a stopping point on the platform goes in steps this wide, up to half a turn each way.
        public const float TurnStepDegrees = 5f;

        private const int TurnSteps = 36;
        private static readonly float[] TurnCos = TurnTable(Math.Cos);
        private static readonly float[] TurnSin = TurnTable(Math.Sin);

        private readonly float _spacingSquared;
        private readonly float _spreadWidth;
        private readonly bool _bounded;
        private readonly ArenaGeometry _platform;
        private readonly IDamageable[] _bodies = new IDamageable[PackLayout.MaxPackSize];
        private readonly float[] _x = new float[PackLayout.MaxPackSize];
        private readonly float[] _y = new float[PackLayout.MaxPackSize];
        private readonly float[] _outwardX = new float[PackLayout.MaxPackSize];
        private readonly float[] _outwardY = new float[PackLayout.MaxPackSize];
        private readonly float[] _approachSin = new float[PackLayout.MaxPackSize];
        private readonly float[] _approachCos = new float[PackLayout.MaxPackSize];
        private readonly float[] _stop = new float[PackLayout.MaxPackSize];
        private readonly float[] _stopSquared = new float[PackLayout.MaxPackSize];
        private readonly float[] _speed = new float[PackLayout.MaxPackSize];
        private readonly int[] _order = new int[PackLayout.MaxPackSize];

        public int Count { get; private set; }
        // Where the hero stood at the last step; where the pack was told it stands until then.
        public float HeroX { get; private set; }
        public float HeroY { get; private set; }

        // spacing is the closest two enemies may come; spreadWidth is the entry offset that maps to the widest approach angle.
        // The hero stands at the floor origin and nothing bounds the floor.
        public PackMotion(float spacing, float spreadWidth)
        {
            if (float.IsNaN(spacing) || float.IsInfinity(spacing) || spacing < 0f)
                throw new ArgumentOutOfRangeException(nameof(spacing));
            if (!(spreadWidth > 0f) || float.IsInfinity(spreadWidth))
                throw new ArgumentOutOfRangeException(nameof(spreadWidth));

            _spacingSquared = spacing * spacing;
            _spreadWidth = spreadWidth;
        }

        // A pack on a platform, with the hero standing at (heroX, heroY) when it enters: every enemy stops inside the same
        // margin from the rim that the hero keeps, so wherever the hero stands some point at its reach is on the platform.
        public PackMotion(float spacing, float spreadWidth, ArenaGeometry platform, float heroX, float heroY)
            : this(spacing, spreadWidth)
        {
            if (float.IsNaN(heroX) || float.IsInfinity(heroX) || float.IsNaN(heroY) || float.IsInfinity(heroY))
                throw new ArgumentOutOfRangeException(nameof(heroX));

            _bounded = true;
            _platform = platform;
            HeroX = heroX;
            HeroY = heroY;
        }

        // An enemy entering straight ahead of the hero; lateral is its formation offset across the corner's centre line and
        // depth its distance from the hero along it.
        public int Add(IDamageable body, float lateral, float depth, float speed, float reach) =>
            Add(body, lateral, depth, speed, reach, EntrySide.Far);

        public int Add(IDamageable body, float lateral, float depth, float speed, float reach, EntrySide side)
        {
            EntrySides.Outward(side, out float outwardX, out float outwardY);
            EntrySides.Lateral(side, out float lateralX, out float lateralY);
            return Add(body, new EntryPlacement(side, lateral, HeroX + outwardX * depth + lateralX * lateral,
                HeroY + outwardY * depth + lateralY * lateral), speed, reach);
        }

        // An enemy entering where EntrySides placed it.
        public int Add(IDamageable body, EntryPlacement placement, float speed, float reach)
        {
            if (body == null)
                throw new ArgumentNullException(nameof(body));
            if (Count == PackLayout.MaxPackSize)
                throw new InvalidOperationException($"A pack holds at most {PackLayout.MaxPackSize} enemies.");
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0f)
                throw new ArgumentOutOfRangeException(nameof(speed));
            if (!(reach > 0f) || float.IsInfinity(reach))
                throw new ArgumentOutOfRangeException(nameof(reach));
            if (float.IsNaN(placement.X) || float.IsInfinity(placement.X) || float.IsNaN(placement.Y) || float.IsInfinity(placement.Y) ||
                float.IsNaN(placement.Lateral) || float.IsInfinity(placement.Lateral))
                throw new ArgumentOutOfRangeException(nameof(placement));

            EntrySides.Outward(placement.Side, out float outwardX, out float outwardY);
            int index = Count++;
            float stop = Math.Max(0f, reach - ReachMargin);
            double angle = Math.Max(-1f, Math.Min(1f, placement.Lateral / _spreadWidth)) * MaxApproachDegrees * Math.PI / 180.0;
            _bodies[index] = body;
            _x[index] = placement.X;
            _y[index] = placement.Y;
            _outwardX[index] = outwardX;
            _outwardY[index] = outwardY;
            _approachSin[index] = (float)Math.Sin(angle);
            _approachCos[index] = (float)Math.Cos(angle);
            _stop[index] = stop;
            _stopSquared[index] = stop * stop;
            _speed[index] = speed;
            return index;
        }

        public float XOf(int index) => _x[index];

        public float YOf(int index) => _y[index];

        // True once the enemy stands close enough to the hero to stop walking.
        public bool HasArrived(int index) => DistanceToHeroSquared(index) <= _stopSquared[index];

        // Walks the pack toward the hero at the last known position.
        public void Step(float deltaTime) => Step(deltaTime, HeroX, HeroY);

        public void Step(float deltaTime, float heroX, float heroY)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (float.IsNaN(heroX) || float.IsInfinity(heroX) || float.IsNaN(heroY) || float.IsInfinity(heroY))
                throw new ArgumentOutOfRangeException(nameof(heroX));

            HeroX = heroX;
            HeroY = heroY;

            // Living enemies, nearest the hero first; an earlier slot goes first on equal distance.
            int living = 0;
            for (int i = 0; i < Count; i++)
            {
                if (!_bodies[i].IsAlive)
                    continue;
                float distance = DistanceToHeroSquared(i);
                int insertAt = living;
                while (insertAt > 0 && DistanceToHeroSquared(_order[insertAt - 1]) > distance)
                {
                    _order[insertAt] = _order[insertAt - 1];
                    insertAt--;
                }
                _order[insertAt] = i;
                living++;
            }

            for (int k = 0; k < living; k++)
            {
                int i = _order[k];
                if (HasArrived(i))
                    continue;

                // The enemy's own point on the arc round the hero: its corner's direction turned by its approach angle.
                float directionX = _outwardX[i] * _approachCos[i] + _outwardY[i] * _approachSin[i];
                float directionY = -_outwardX[i] * _approachSin[i] + _outwardY[i] * _approachCos[i];
                float targetX = heroX + directionX * _stop[i];
                float targetY = heroY + directionY * _stop[i];
                if (_bounded && !_platform.IsOnPlatform(targetX, targetY, HeroMotion.EdgeMargin))
                    TurnOntoPlatform(i, heroX, heroY, ref targetX, ref targetY);
                float dx = targetX - _x[i];
                float dy = targetY - _y[i];
                float remaining = (float)Math.Sqrt(dx * dx + dy * dy);
                if (remaining <= 0f)
                    continue;
                float stride = _speed[i] * deltaTime;
                float nextX = stride >= remaining ? targetX : _x[i] + dx / remaining * stride;
                float nextY = stride >= remaining ? targetY : _y[i] + dy / remaining * stride;
                if (!IsBlocked(nextX, nextY, k))
                {
                    _x[i] = nextX;
                    _y[i] = nextY;
                }
            }
        }

        // Turns the target round the hero, step by step both ways, to the first point that lies on the platform. When both
        // ways reach the platform at the same step, the enemy takes the one nearer to where it stands, so it never crosses
        // in front of the hero to the far side. A platform too small for the reach leaves the target as it was.
        private void TurnOntoPlatform(int i, float heroX, float heroY, ref float targetX, ref float targetY)
        {
            float offsetX = targetX - heroX;
            float offsetY = targetY - heroY;
            for (int step = 1; step <= TurnSteps; step++)
            {
                float cos = TurnCos[step];
                float sin = TurnSin[step];
                float anticlockwiseX = heroX + offsetX * cos - offsetY * sin;
                float anticlockwiseY = heroY + offsetX * sin + offsetY * cos;
                float clockwiseX = heroX + offsetX * cos + offsetY * sin;
                float clockwiseY = heroY - offsetX * sin + offsetY * cos;
                bool anticlockwise = _platform.IsOnPlatform(anticlockwiseX, anticlockwiseY, HeroMotion.EdgeMargin);
                bool clockwise = _platform.IsOnPlatform(clockwiseX, clockwiseY, HeroMotion.EdgeMargin);
                if (!anticlockwise && !clockwise)
                    continue;

                if (anticlockwise && clockwise)
                {
                    float toAnticlockwise = Squared(anticlockwiseX - _x[i]) + Squared(anticlockwiseY - _y[i]);
                    float toClockwise = Squared(clockwiseX - _x[i]) + Squared(clockwiseY - _y[i]);
                    anticlockwise = toAnticlockwise <= toClockwise;
                }
                targetX = anticlockwise ? anticlockwiseX : clockwiseX;
                targetY = anticlockwise ? anticlockwiseY : clockwiseY;
                return;
            }
        }

        // Only enemies already stepped this frame, which are closer to the hero, can block, so no two enemies wait on each other.
        private bool IsBlocked(float x, float y, int stepped)
        {
            for (int k = 0; k < stepped; k++)
            {
                int other = _order[k];
                float dx = _x[other] - x;
                float dy = _y[other] - y;
                if (dx * dx + dy * dy < _spacingSquared)
                    return true;
            }
            return false;
        }

        private float DistanceToHeroSquared(int index)
        {
            float dx = _x[index] - HeroX;
            float dy = _y[index] - HeroY;
            return dx * dx + dy * dy;
        }

        private static float Squared(float value) => value * value;

        private static float[] TurnTable(Func<double, double> function)
        {
            var table = new float[TurnSteps + 1];
            for (int step = 0; step <= TurnSteps; step++)
                table[step] = (float)function(step * TurnStepDegrees * Math.PI / 180.0);
            return table;
        }
    }
}
