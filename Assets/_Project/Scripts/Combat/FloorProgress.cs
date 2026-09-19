using System;
using System.Collections.Generic;

namespace Cryptforge.Combat
{
    // Walks a floor room by room and wave by wave. Rooms with zero waves are non-combat rooms (the forge). A room may
    // also be endless - a swarm - in which case its waves never run out: it keeps handing out waves with a rising
    // ordinal until LeaveEndlessRoom is called, and its authored waves are a ladder SwarmRule reads that ordinal
    // against rather than a list that finishes.
    public sealed class FloorProgress
    {
        private readonly int[] _waves;
        private readonly bool[] _endless;
        private bool _leftEndlessRoom;

        public int RoomCount => _waves.Length;
        public int RoomIndex { get; private set; } = -1;
        public int WaveIndex { get; private set; } = -1;
        public int RoomsCleared { get; private set; }
        public bool IsComplete { get; private set; }

        // True while the current room is one whose waves do not run out and the player has not left it.
        public bool IsInEndlessRoom => !IsComplete && RoomIndex >= 0 && _endless[RoomIndex] && !_leftEndlessRoom;

        // An endless room never has a final wave, even after it is left: its ordinal runs past its ladder, so matching
        // the ladder's last index would mean nothing. Leaving it advances the floor through the ordinary path instead.
        public bool IsFinalWave =>
            !IsComplete && RoomIndex == _waves.Length - 1 && _waves[RoomIndex] > 0 && !_endless[RoomIndex] &&
            WaveIndex == _waves[RoomIndex] - 1;

        public FloorProgress(IReadOnlyList<int> wavesPerRoom, IReadOnlyList<bool> endlessRooms = null)
        {
            if (wavesPerRoom == null || wavesPerRoom.Count == 0)
                throw new ArgumentException("A floor needs at least one room.", nameof(wavesPerRoom));
            if (endlessRooms != null && endlessRooms.Count != wavesPerRoom.Count)
                throw new ArgumentException("One endless flag per room, or none at all.", nameof(endlessRooms));

            _waves = new int[wavesPerRoom.Count];
            _endless = new bool[wavesPerRoom.Count];
            for (int i = 0; i < _waves.Length; i++)
            {
                if (wavesPerRoom[i] < 0)
                    throw new ArgumentOutOfRangeException(nameof(wavesPerRoom));
                _waves[i] = wavesPerRoom[i];
                _endless[i] = endlessRooms != null && endlessRooms[i];
                if (_endless[i] && _waves[i] == 0)
                    throw new ArgumentException("An endless room needs at least one authored wave to repeat.", nameof(endlessRooms));
            }
        }

        // Stop asking this endless room for waves: the next Advance leaves it for the room after it. Does nothing
        // anywhere else, so a caller may say it without checking where the floor stands.
        public void LeaveEndlessRoom()
        {
            if (IsInEndlessRoom)
                _leftEndlessRoom = true;
        }

        // Call once to start, then again after each wave is cleared or each non-combat room is resolved.
        public FloorStep Advance()
        {
            if (IsComplete)
                return new FloorStep(FloorStepKind.Cleared, RoomIndex, -1);

            // An endless room's ordinal keeps climbing past its ladder; SwarmRule turns it into a wave and a difficulty.
            if (IsInEndlessRoom)
            {
                WaveIndex++;
                return new FloorStep(FloorStepKind.Wave, RoomIndex, WaveIndex);
            }

            // An endless room that has been left is finished whatever its ordinal reads, so it never falls back into
            // its ladder here; only the branch above ever hands out one of its waves.
            if (RoomIndex >= 0 && !_endless[RoomIndex] && WaveIndex + 1 < _waves[RoomIndex])
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
            _leftEndlessRoom = false;
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
