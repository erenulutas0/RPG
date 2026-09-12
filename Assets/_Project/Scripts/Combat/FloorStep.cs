namespace Cryptforge.Combat
{
    public readonly struct FloorStep
    {
        public FloorStepKind Kind { get; }
        public int RoomIndex { get; }
        // -1 outside combat rooms.
        public int WaveIndex { get; }

        public FloorStep(FloorStepKind kind, int roomIndex, int waveIndex)
        {
            Kind = kind;
            RoomIndex = roomIndex;
            WaveIndex = waveIndex;
        }
    }
}
