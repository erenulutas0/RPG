using System;

namespace Cryptforge.Combat
{
    // Walks one wave's enemies across the arena floor to the hero, who stands at the origin. Each enemy heads for its own
    // point just inside its reach, on an arc spread by where it entered, and stops once it is that close to the hero. It
    // waits whenever its next step would bring it nearer than the spacing to an enemy that is closer to the hero. Enemies
    // step nearest first, so the result never depends on update order, and the scene and the Descent simulation, which
    // both use this class, move every enemy identically.
    public sealed class PackMotion
    {
        // How far inside its reach an enemy stops, so rounding can never leave it exactly on the edge.
        public const float ReachMargin = 0.1f;
        // An enemy entering at the pack's widest slot approaches from this many degrees off the centre line.
        public const float MaxApproachDegrees = 60f;

        private readonly float _spacingSquared;
        private readonly float _spreadWidth;
        private readonly IDamageable[] _bodies = new IDamageable[PackLayout.MaxPackSize];
        private readonly float[] _x = new float[PackLayout.MaxPackSize];
        private readonly float[] _y = new float[PackLayout.MaxPackSize];
        private readonly float[] _targetX = new float[PackLayout.MaxPackSize];
        private readonly float[] _targetY = new float[PackLayout.MaxPackSize];
        private readonly float[] _stopSquared = new float[PackLayout.MaxPackSize];
        private readonly float[] _speed = new float[PackLayout.MaxPackSize];
        private readonly int[] _order = new int[PackLayout.MaxPackSize];

        public int Count { get; private set; }

        // spacing is the closest two enemies may come; spreadWidth is the entry offset that maps to the widest approach angle.
        public PackMotion(float spacing, float spreadWidth)
        {
            if (float.IsNaN(spacing) || float.IsInfinity(spacing) || spacing < 0f)
                throw new ArgumentOutOfRangeException(nameof(spacing));
            if (!(spreadWidth > 0f) || float.IsInfinity(spreadWidth))
                throw new ArgumentOutOfRangeException(nameof(spreadWidth));

            _spacingSquared = spacing * spacing;
            _spreadWidth = spreadWidth;
        }

        public int Add(IDamageable body, float x, float y, float speed, float reach)
        {
            if (body == null)
                throw new ArgumentNullException(nameof(body));
            if (Count == PackLayout.MaxPackSize)
                throw new InvalidOperationException($"A pack holds at most {PackLayout.MaxPackSize} enemies.");
            if (float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0f)
                throw new ArgumentOutOfRangeException(nameof(speed));
            if (!(reach > 0f) || float.IsInfinity(reach))
                throw new ArgumentOutOfRangeException(nameof(reach));

            int index = Count++;
            float stop = Math.Max(0f, reach - ReachMargin);
            double angle = Math.Max(-1f, Math.Min(1f, x / _spreadWidth)) * MaxApproachDegrees * Math.PI / 180.0;
            _bodies[index] = body;
            _x[index] = x;
            _y[index] = y;
            _targetX[index] = (float)(Math.Sin(angle) * stop);
            _targetY[index] = (float)(Math.Cos(angle) * stop);
            _stopSquared[index] = stop * stop;
            _speed[index] = speed;
            return index;
        }

        public float XOf(int index) => _x[index];

        public float YOf(int index) => _y[index];

        // True once the enemy stands close enough to the hero to stop walking.
        public bool HasArrived(int index) => DistanceToHeroSquared(index) <= _stopSquared[index];

        public void Step(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

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

                float dx = _targetX[i] - _x[i];
                float dy = _targetY[i] - _y[i];
                float remaining = (float)Math.Sqrt(dx * dx + dy * dy);
                float stride = _speed[i] * deltaTime;
                float nextX = stride >= remaining ? _targetX[i] : _x[i] + dx / remaining * stride;
                float nextY = stride >= remaining ? _targetY[i] : _y[i] + dy / remaining * stride;
                if (!IsBlocked(nextX, nextY, k))
                {
                    _x[i] = nextX;
                    _y[i] = nextY;
                }
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

        private float DistanceToHeroSquared(int index) => _x[index] * _x[index] + _y[index] * _y[index];
    }
}
