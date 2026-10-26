using System;
using System.Collections.Generic;

namespace Cryptforge.Combat
{
    // Decides, frame by frame, what a continuous swarm spawns: which of the pack's ten places fills, with which enemy of
    // the ladder, at what difficulty. Pure and shared, so the scene and the Descent simulation cannot drift apart on
    // when an enemy walks in - the one thing a continuous room does that a wave never did (docs/27, S6c).
    //
    // The ladder is read by the clock (docs/27 decision 8). The room is on one wave of it at a time - the next every
    // SwarmRule.LadderSeconds, the last for as long as the room lasts - and each of the pack's places has a role in every
    // wave: place i is that wave's i-th enemy. A place that opens or fills again takes its role in the wave the clock has
    // reached, and as many places are open as that wave has enemies. So what stands on the floor at any moment is the
    // wave its minute calls for, however fast the hero has been killing. It was a queue first, read one spawn at a time,
    // and a hero who killed fast met the heavies in half a minute; the owner asked for time instead (docs/35, docs/36).
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
        // Enemies spawned so far, the opening included.
        public int Spawned { get; private set; }
        // Places in use, never more than the arena holds.
        public int Slots { get; private set; }
        // Whether the last Tick carried the clock past a level-up sitting (SwarmRule.OfferEverySeconds).
        public bool OfferDue { get; private set; }
        // The wave of the ladder the room's clock is on.
        public int Wave => SwarmRule.LadderWaveAt(Seconds, _sizes.Length);

        public SwarmDirector(IReadOnlyList<int> waveSizes)
        {
            if (waveSizes == null || waveSizes.Count == 0)
                throw new ArgumentException("A swarm needs at least one authored wave.", nameof(waveSizes));

            _sizes = new int[waveSizes.Count];
            for (int i = 0; i < _sizes.Length; i++)
            {
                if (waveSizes[i] < 1 || waveSizes[i] > PackLayout.MaxFormationSize)
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
            float before = Seconds;
            Seconds += deltaTime;
            OfferDue = SwarmRule.OfferFallsDue(before, Seconds);
            int cap = CapAt(Seconds);
            // A new place opens only when the clock's wave has more enemies than the pack has places - never because an
            // enemy fell. A fallen enemy's place is still a place: it waits out its breath and refills below. Counting the
            // living here instead would open a fresh place the instant anything died, and the breath would mean nothing.
            while (Slots < PackLayout.MaxPackSize && Slots < cap)
                spawns.Add(Take(Slots++));
            for (int slot = 0; slot < Slots; slot++)
            {
                if (_fellAt[slot] < 0f || Seconds - _fellAt[slot] < SwarmRule.RespawnSeconds)
                    continue;
                if (alive + spawns.Count >= cap)
                    break;
                _fellAt[slot] = -1f;
                spawns.Add(Take(slot));
            }
        }

        // How many may stand at once at this moment of the room: the size of the wave its clock has reached.
        public int CapAt(float seconds) => _sizes[SwarmRule.LadderWaveAt(seconds, _sizes.Length)];

        // The place's role in the wave the clock is on. A wave smaller than the pack - a ladder that shrinks - wraps
        // round its own list rather than reading past it.
        private Spawn Take(int slot)
        {
            int wave = SwarmRule.LadderWaveAt(Seconds, _sizes.Length);
            Spawned++;
            return new Spawn(slot, wave, slot % _sizes[wave], SwarmRule.DifficultyAtSeconds(Seconds));
        }
    }
}
