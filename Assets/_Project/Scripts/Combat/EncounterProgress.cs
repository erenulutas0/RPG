using System;

namespace Cryptforge.Combat
{
    // Rules for one encounter at a time: per-fight hits and scaled clear time, and when the next fight may start.
    public sealed class EncounterProgress
    {
        private readonly float _advanceDelay;
        private float _waited;

        public int EncounterNumber { get; private set; }
        public int HitsTaken { get; private set; }
        public float Elapsed { get; private set; }
        public bool IsCleared { get; private set; }
        public int EncountersCleared => IsCleared ? EncounterNumber : Math.Max(0, EncounterNumber - 1);

        public EncounterProgress(float advanceDelay)
        {
            if (float.IsNaN(advanceDelay) || float.IsInfinity(advanceDelay) || advanceDelay < 0f)
                throw new ArgumentOutOfRangeException(nameof(advanceDelay));

            _advanceDelay = advanceDelay;
        }

        public void Begin()
        {
            EncounterNumber++;
            HitsTaken = 0;
            Elapsed = 0f;
            _waited = 0f;
            IsCleared = false;
        }

        public void RecordHit()
        {
            if (EncounterNumber > 0 && !IsCleared)
                HitsTaken++;
        }

        public bool Clear()
        {
            if (EncounterNumber == 0 || IsCleared)
                return false;

            IsCleared = true;
            return true;
        }

        // Elapsed stops at the clear, so it doubles as the clear time. The advance delay only counts
        // while the caller allows progression, for example when no upgrade choice is open.
        public bool Tick(float deltaTime, bool canAdvance)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (EncounterNumber == 0)
                return false;

            if (!IsCleared)
            {
                Elapsed += deltaTime;
                return false;
            }

            if (!canAdvance)
                return false;

            _waited += deltaTime;
            return _waited >= _advanceDelay;
        }
    }
}
