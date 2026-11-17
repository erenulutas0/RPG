using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // What a run wears (docs/54 phase 5): the carried weapon's own gear and the equipped armour, amulet and ring, read once
    // as the run begins (ProfileGear.EquippedPieces for the run's weapon) and fixed for the run - the Anvil works between
    // runs. Its totals are taken at construction, per stat and clamped at the caps (GearStats.Totals), so a piece changed
    // later cannot move a run already started. None is the ungeared run: every shipped balance test, the batch and the
    // gate play it.
    public sealed class GearLoadout
    {
        public static readonly GearLoadout None = new GearLoadout(null);

        private readonly ItemInstance[] _pieces;
        private readonly int[] _totals;

        public GearLoadout(IEnumerable<ItemInstance> pieces)
        {
            var kept = new List<ItemInstance>(4);
            if (pieces != null)
            {
                foreach (ItemInstance piece in pieces)
                {
                    if (piece != null)
                        kept.Add(piece);
                }
            }
            _pieces = kept.ToArray();
            _totals = GearStats.Totals(_pieces);
            for (int i = 0; i < _totals.Length; i++)
            {
                if (_totals[i] != 0)
                    IsEmpty = false;
            }
        }

        // The pieces as the run began with them, weapon first where the profile listed it so.
        public IReadOnlyList<ItemInstance> Pieces => _pieces;
        // No stat with a value: a run wearing this is the ungeared run, and GearStats.Apply touches nothing.
        public bool IsEmpty { get; } = true;

        // The clamped total of one stat, in hundredths (GearStats' units).
        public int Total(GearStat stat) => _totals[(int)stat];

        // Every stat's clamped total, a copy (GearStat's order).
        public int[] Totals() => (int[])_totals.Clone();

        // The power index, DPS x EHP - 1, in hundredths of a percent (4200 is +42 %).
        public int Power => GearStats.Power(_totals);
    }
}
