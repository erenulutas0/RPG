using System;
using System.Collections.Generic;

namespace Cryptforge.Combat
{
    // Walks a floor room by room and wave by wave. Rooms with zero waves are non-combat rooms (the forge).
    public sealed class FloorProgress
    {
        private readonly int[] _waves;

        public int RoomCount => _waves.Length;
        public int RoomIndex { get; private set; } = -1;
        public int WaveIndex { get; private set; } = -1;
        public int RoomsCleared { get; private set; }
        public bool IsComplete { get; private set; }

        public bool IsFinalWave =>
            !IsComplete && RoomIndex == _waves.Length - 1 && _waves[RoomIndex] > 0 && WaveIndex == _waves[RoomIndex] - 1;

        public FloorProgress(IReadOnlyList<int> wavesPerRoom)
        {
            if (wavesPerRoom == null || wavesPerRoom.Count == 0)
                throw new ArgumentException("A floor needs at least one room.", nameof(wavesPerRoom));

            _waves = new int[wavesPerRoom.Count];
            for (int i = 0; i < _waves.Length; i++)
            {
                if (wavesPerRoom[i] < 0)
                    throw new ArgumentOutOfRangeException(nameof(wavesPerRoom));
                _waves[i] = wavesPerRoom[i];
            }
        }

        // Call once to start, then again after each wave is cleared or each non-combat room is resolved.
        public FloorStep Advance()
        {
            if (IsComplete)
                return new FloorStep(FloorStepKind.Cleared, RoomIndex, -1);

            if (RoomIndex >= 0 && WaveIndex + 1 < _waves[RoomIndex])
            {
                WaveIndex++;
                return new FloorStep(FloorStepKind.Wave, RoomIndex, WaveIndex);
            }

            if (RoomIndex >= 0)
                RoomsCleared++;

            if (RoomIndex + 1 >= _waves.Length)
            {
                // The last room stays current so progress still reads "room N of N".
                IsComplete = true;
                WaveIndex = -1;
                return new FloorStep(FloorStepKind.Cleared, RoomIndex, -1);
            }

            RoomIndex++;
            if (_waves[RoomIndex] == 0)
            {
                WaveIndex = -1;
                return new FloorStep(FloorStepKind.NonCombatRoom, RoomIndex, -1);
            }

            WaveIndex = 0;
            return new FloorStep(FloorStepKind.Wave, RoomIndex, 0);
        }
    }
}
