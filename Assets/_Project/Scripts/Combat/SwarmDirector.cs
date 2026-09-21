using System;
using System.Collections.Generic;

namespace Cryptforge.Combat
{
    // Decides, frame by frame, what a continuous swarm spawns: which of the pack's ten places fills, with which enemy of
    // the ladder, at what difficulty. Pure and shared, so the scene and the Descent simulation cannot drift apart on
    // when an enemy walks in - the one thing a continuous room does that a wave never did (docs/27, S6c).
    //
    // The ladder is read as a queue. The opening places the first wave whole; after that every spawn takes the next
    // enemy in line - the second wave's after the first's, through the last, whose list then repeats for as long as the
    // room lasts. How many may stand at once is the size of the wave the queue has reached, so the crowd grows exactly as
    // the ladder always meant it to, and it grows by being killed: a hero who kills nothing meets nothing new.
    public sealed class SwarmDirector
    {
        public readonly struct Spawn
        {
            public int Slot { get; }
            // Which wave of the ladder, and which enemy of it, walks into this place.
            public int Wave { get; }
            public int Index { get; }
            public float Difficulty { get; }

            public Spawn(int slot, int wave, int index, float difficulty)
            {
                Slot = slot;
                Wave = wave;
                Index = index;
                Difficulty = difficulty;
            }
        }

        private readonly int[] _sizes;
        // When each place's enemy fell, on the room's clock; negative for a place that holds a living enemy or none yet.
        private readonly float[] _fellAt = new float[PackLayout.MaxPackSize];

        // Seconds the hero has stood in the room.
        public float Seconds { get; private set; }
        // Enemies spawned so far, the opening included: the queue's position.
        public int Spawned { get; private set; }
        // Places in use, never more than the arena holds.
        public int Slots { get; private set; }

        public SwarmDirector(IReadOnlyList<int> waveSizes)
        {
            if (waveSizes == null || waveSizes.Count == 0)
                throw new ArgumentException("A swarm needs at least one authored wave.", nameof(waveSizes));

            _sizes = new int[waveSizes.Count];
            for (int i = 0; i < _sizes.Length; i++)
            {
                if (waveSizes[i] < 1 || waveSizes[i] > PackLayout.MaxPackSize)
                    throw new ArgumentOutOfRangeException(nameof(waveSizes));
                _sizes[i] = waveSizes[i];
            }
            for (int i = 0; i < _fellAt.Length; i++)
                _fellAt[i] = -1f;
        }

        // The opening: the ladder's first wave, whole, into the first places, at the room's first difficulty.
        public void Open(List<Spawn> spawns)
        {
            if (spawns == null)
                throw new ArgumentNullException(nameof(spawns));
            if (Spawned > 0)
                throw new InvalidOperationException("A swarm opens once.");

            spawns.Clear();
            for (int i = 0; i < _sizes[0]; i++)
                spawns.Add(Take(Slots++));
        }

        // The enemy in this place has fallen; its place fills again once RespawnSeconds have passed.
        public void Fell(int slot)
        {
            if (slot < 0 || slot >= Slots)
                throw new ArgumentOutOfRangeException(nameof(slot));
            _fellAt[slot] = Seconds;
        }

        // One frame of the room: the clock moves on, and whatever is due walks in - new places first while the crowd is
        // allowed to grow, then fallen places whose wait is over. alive is how many enemies stand now.
        public void Tick(float deltaTime, int alive, List<Spawn> spawns)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (spawns == null)
                throw new ArgumentNullException(nameof(spawns));

            spawns.Clear();
            Seconds += deltaTime;
            // A new place opens only when the ladder allows more places than the pack has - never because an enemy fell.
            // A fallen enemy's place is still a place: it waits out its breath and refills below. Counting the living
            // here instead would open a fresh place the instant anything died, and the breath would mean nothing.
            while (Slots < PackLayout.MaxPackSize && Slots < CapAt(Spawned))
                spawns.Add(Take(Slots++));
            for (int slot = 0; slot < Slots; slot++)
            {
                if (_fellAt[slot] < 0f || Seconds - _fellAt[slot] < SwarmRule.RespawnSeconds)
                    continue;
                if (alive + spawns.Count >= CapAt(Spawned))
                    break;
                _fellAt[slot] = -1f;
                spawns.Add(Take(slot));
            }
        }

        // How many may stand at once when the queue stands at this position: the size of the wave it has reached.
        public int CapAt(int position)
        {
            Locate(position, out int wave, out _);
            return _sizes[wave];
        }

        // Where in the ladder the queue's position falls. Past the last wave, the last wave's list repeats.
        public void Locate(int position, out int wave, out int index)
        {
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));

            int remaining = position;
            for (int w = 0; w < _sizes.Length - 1; w++)
            {
                if (remaining < _sizes[w])
                {
                    wave = w;
                    index = remaining;
                    return;
                }
                remaining -= _sizes[w];
            }
            wave = _sizes.Length - 1;
            index = remaining % _sizes[wave];
        }

        private Spawn Take(int slot)
        {
            Locate(Spawned, out int wave, out int index);
            Spawned++;
            return new Spawn(slot, wave, index, SwarmRule.DifficultyAtSeconds(Seconds));
        }
    }
}
