using System;

namespace Cryptforge.Combat
{
    // The foundry's altars (docs/27 S6d's shrines, agreed 2026-10-23): a place on the map that grants a power in exchange
    // for danger, the bargain an evolution makes, chosen by walking there rather than on a card. Staying on one for
    // DwellSeconds - walking is fine; the first version asked the hero to stand still for a second, and on the phone that
    // read as nothing happening (2026-10-24) - grants a bonus upgrade, an offer of cards as a level-up opens, and raises
    // the run's difficulty by DifficultyCost, half an evolution's. Each altar answers once per floor. Pure, so it can be
    // tested without a scene.
    public static class AltarRule
    {
        public const float Radius = 1.2f;
        public const float DwellSeconds = 0.4f;
        public const float DifficultyCost = EvolutionCostShare * 0.2f;
        // How much of an evolution's difficulty cost (EvolutionRule.DifficultyPerStack, 0.2) an altar asks.
        private const float EvolutionCostShare = 0.5f;
    }

    // A floor's altars: which have answered, and how long the hero has stayed on the one it stands on.
    public sealed class AltarField
    {
        private readonly float[] _x;
        private readonly float[] _y;
        private readonly bool[] _used;
        private int _standing = -1;

        public AltarField(float[] x, float[] y)
        {
            if (x == null || y == null || x.Length != y.Length)
                throw new ArgumentException("Altar coordinates must pair up.");
            _x = (float[])x.Clone();
            _y = (float[])y.Clone();
            _used = new bool[x.Length];
        }

        public int Count => _x.Length;
        public float XOf(int altar) => _x[altar];
        public float YOf(int altar) => _y[altar];
        public bool IsUsed(int altar) => _used[altar];
        // The altar the hero is standing on, or -1, and how long it has stayed there.
        public int Standing => _standing;
        public float Held { get; private set; }

        // A new floor: every altar answers again.
        public void Reset()
        {
            for (int i = 0; i < _used.Length; i++)
                _used[i] = false;
            _standing = -1;
            Held = 0f;
        }

        // One frame: the altar granted this frame, or -1. Stepping off starts the hold again.
        public int Step(float heroX, float heroY, float deltaTime)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            int on = -1;
            for (int i = 0; i < _x.Length; i++)
                if (!_used[i] && ExitRule.IsWithinReach(heroX, heroY, _x[i], _y[i], AltarRule.Radius))
                {
                    on = i;
                    break;
                }
            if (on != _standing)
                Held = 0f;
            _standing = on;
            if (on < 0)
                return -1;
            Held += deltaTime;
            if (Held < AltarRule.DwellSeconds)
                return -1;
            _used[on] = true;
            _standing = -1;
            Held = 0f;
            return on;
        }
    }
}
