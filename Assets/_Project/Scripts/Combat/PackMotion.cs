using System;
using Cryptforge.Art;

namespace Cryptforge.Combat
{
    // Walks one wave's enemies across the arena floor to the hero. Each enemy enters at one of the four corners and heads
    // for its own point just inside its reach, on an arc round the hero spread by where it entered, and stops once it is
    // that close; when the hero moves, the point moves with the hero and the enemy walks again. On a platform the point
    // never hangs over the void: when the hero stands at the rim, the enemy turns round the hero to the nearest point that
    // lies on the platform.
    //
    // No step ever brings an enemy nearer than the spacing to another living enemy, whichever of the two stands nearer the
    // hero, so enemies that start a body apart stay a body apart for as long as they live; a step may always widen a gap
    // that is already too small. A step that would close on a neighbour slides instead, keeping only the part of the step
    // that runs along the spacing of every neighbour the step could touch, so an enemy squeezing between two walks down
    // the gap rather than glancing off each in turn; the slide is taken only if it still brings the enemy nearer its point
    // and keeps it on the platform, and otherwise the enemy waits where it is and tries again next step. Every step brings
    // an enemy nearer its point, so while the hero stands still no enemy walks away from its point and back, and the pack
    // settles and comes to rest - though an enemy squeezing past a slower one may be nudged one way and then the other on
    // the way, and one heading exactly through a neighbour standing on its point waits behind it. With nothing in its way
    // an enemy walks straight at its point exactly as a lone enemy does. Enemies step nearest the hero first, ties by
    // slot, so the result never depends on update order, and the scene and the Descent simulation, which both use this
    // class, move every enemy identically. A long frame is walked in equal steps.
    public sealed class PackMotion
    {
        // How far inside its reach an enemy stops, so rounding can never leave it exactly on the edge.
        public const float ReachMargin = 0.1f;
        // An enemy entering at the pack's widest slot approaches from this many degrees off its corner's centre line.
        public const float MaxApproachDegrees = 60f;
        // Turning round the hero to find a stopping point on the platform goes in steps this wide, up to half a turn each way.
        public const float TurnStepDegrees = 5f;
        // The longest step an enemy takes at once: a frame that would carry the fastest enemy further, after a hitch, is
        // walked in equal steps no longer than this, up to MaxSubSteps, so a walk after a hitch follows the path the frames
        // it stands for would have. At the game's frame rates every frame is a single step, and a frame of a third of a
        // second, the longest Unity hands a script, stays within the cap for every authored enemy. The spacing never depends
        // on it: every step's end is checked.
        public const float MaxStride = 0.25f;
        // A frame is walked in at most this many steps; past that each step is simply longer.
        public const int MaxSubSteps = 8;
        // A slide shorter than this share of the stride means the enemy heads straight into its neighbours, so it waits.
        private const float ShortestSlide = 1e-3f;
        private const int NoEnemy = -1;

        private const int TurnSteps = 36;
        private static readonly float[] TurnCos = TurnTable(Math.Cos);
        private static readonly float[] TurnSin = TurnTable(Math.Sin);

        private readonly float _spacing;
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
        private readonly bool[] _stalled = new bool[PackLayout.MaxPackSize];

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

            _spacing = spacing;
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

        // True when the last step left the enemy where it stood although it had not arrived: its straight step would have
        // closed on a neighbour, and sliding along its neighbours brought it no nearer its point.
        public bool IsStalled(int index) => _stalled[index];

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
            Array.Clear(_stalled, 0, Count);
            int living = OrderLiving();
            if (living == 0 || deltaTime <= 0f)
                return;

            float fastest = 0f;
            for (int k = 0; k < living; k++)
                fastest = Math.Max(fastest, _speed[_order[k]]);
            int steps = 1;
            if (fastest * deltaTime > MaxStride)
                steps = (int)Math.Min(MaxSubSteps, Math.Ceiling(fastest * deltaTime / MaxStride));
            float stepTime = deltaTime / steps;

            for (int step = 0; step < steps; step++)
            {
                if (step > 0)
                    OrderLiving();
                for (int k = 0; k < living; k++)
                {
                    int i = _order[k];
                    Walk walk = WalkOne(i, living, stepTime, heroX, heroY);
                    // An enemy that walks on any step of a long frame did not wait through it.
                    _stalled[i] = walk == Walk.Waited && (step == 0 || _stalled[i]);
                }
            }
        }

        private enum Walk
        {
            Stood,
            Walked,
            Waited
        }

        // Puts the living enemies into _order, nearest the hero first, an earlier slot first on equal distance, and counts them.
        private int OrderLiving()
        {
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
            return living;
        }

        // One step of enemy i toward its point: straight at it when that keeps the spacing, else sliding round the enemy in
        // the way, else none.
        private Walk WalkOne(int i, int living, float deltaTime, float heroX, float heroY)
        {
            if (HasArrived(i))
                return Walk.Stood;

            // The enemy's own point on the arc round the hero: its corner's direction turned by its approach angle.
            float directionX = _outwardX[i] * _approachCos[i] + _outwardY[i] * _approachSin[i];
            float directionY = -_outwardX[i] * _approachSin[i] + _outwardY[i] * _approachCos[i];
            float targetX = heroX + directionX * _stop[i];
            float targetY = heroY + directionY * _stop[i];
            if (_bounded && !_platform.IsOnPlatform(targetX, targetY, HeroMotion.EdgeMargin))
                TurnOntoPlatform(i, heroX, heroY, ref targetX, ref targetY);
            float dx = targetX - _x[i];
            float dy = targetY - _y[i];
            float remainingSquared = dx * dx + dy * dy;
            float remaining = (float)Math.Sqrt(remainingSquared);
            float stride = _speed[i] * deltaTime;
            if (remaining <= 0f || stride <= 0f)
                return Walk.Stood;

            float nextX = stride >= remaining ? targetX : _x[i] + dx / remaining * stride;
            float nextY = stride >= remaining ? targetY : _y[i] + dy / remaining * stride;
            int blocker = Blocker(i, living, nextX, nextY);
            if (blocker == NoEnemy)
            {
                _x[i] = nextX;
                _y[i] = nextY;
                return Walk.Walked;
            }

            // Slide: drop the part of the step that heads into any neighbour this step could touch, the one in the way among
            // them, and keep the part that runs along their spacing, as far as that part reaches. Two passes, since dropping
            // the part that heads into one can leave a little heading back into another.
            float normalX = _x[i] - _x[blocker];
            float normalY = _y[i] - _y[blocker];
            if (!(normalX * normalX + normalY * normalY > 0f))
                return Walk.Waited;
            float slideX = dx / remaining;
            float slideY = dy / remaining;
            float touchSquared = (_spacing + stride) * (_spacing + stride);
            for (int pass = 0; pass < 2; pass++)
            {
                for (int k = 0; k < living; k++)
                {
                    int other = _order[k];
                    if (other == i)
                        continue;
                    float awayX = _x[i] - _x[other];
                    float awayY = _y[i] - _y[other];
                    float awaySquared = awayX * awayX + awayY * awayY;
                    if (awaySquared >= touchSquared || !(awaySquared > 0f))
                        continue;
                    float away = (float)Math.Sqrt(awaySquared);
                    awayX /= away;
                    awayY /= away;
                    float inward = slideX * awayX + slideY * awayY;
                    if (inward >= 0f)
                        continue;
                    slideX -= inward * awayX;
                    slideY -= inward * awayY;
                }
            }
            slideX *= stride;
            slideY *= stride;
            float slideSquared = slideX * slideX + slideY * slideY;
            if (slideSquared < ShortestSlide * ShortestSlide * stride * stride)
                return Walk.Waited;

            float slidX = _x[i] + slideX;
            float slidY = _y[i] + slideY;
            float leftX = targetX - slidX;
            float leftY = targetY - slidY;
            if (leftX * leftX + leftY * leftY >= remainingSquared)
                return Walk.Waited;
            if (_bounded && !_platform.IsOnPlatform(slidX, slidY, HeroMotion.EdgeMargin))
                return Walk.Waited;
            if (Blocker(i, living, slidX, slidY) != NoEnemy)
                return Walk.Waited;

            _x[i] = slidX;
            _y[i] = slidY;
            return Walk.Walked;
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

        // The living enemy that keeps enemy i from standing at (x, y): one the spot lies nearer than the spacing to, and
        // nearer than i stands to it now, so a step never closes on a neighbour inside the spacing but may always widen a
        // gap that is already too small. Of several, the one the spot comes nearest, the earlier in the order on a tie; -1
        // when none does.
        private int Blocker(int i, int living, float x, float y)
        {
            int blocker = NoEnemy;
            float nearest = float.MaxValue;
            for (int k = 0; k < living; k++)
            {
                int other = _order[k];
                if (other == i)
                    continue;
                float dx = x - _x[other];
                float dy = y - _y[other];
                float apart = dx * dx + dy * dy;
                if (apart >= nearest || !Blocks(i, other, x, y))
                    continue;
                blocker = other;
                nearest = apart;
            }
            return blocker;
        }

        // Whether living enemy other keeps enemy i from standing at (x, y): the spot lies nearer than the spacing to it and
        // nearer than i stands to it now.
        private bool Blocks(int i, int other, float x, float y)
        {
            if (!_bodies[other].IsAlive)
                return false;
            float dx = x - _x[other];
            float dy = y - _y[other];
            float apart = dx * dx + dy * dy;
            if (apart >= _spacingSquared)
                return false;
            float nowX = _x[i] - _x[other];
            float nowY = _y[i] - _y[other];
            return apart < nowX * nowX + nowY * nowY;
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
