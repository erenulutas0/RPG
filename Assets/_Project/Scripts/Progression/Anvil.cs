using System;
using System.Collections.Generic;
using Cryptforge.Core;

namespace Cryptforge.Progression
{
    // Why the Anvil refuses an operation, for the screen to say and the tests to read.
    public enum AnvilRefusal
    {
        None = 0,
        NotOwned,
        MaxLevel,
        Gold,
        Material,
        Seal,
        Hammer,
        // A reroll is shown and not yet kept or discarded: it is resolved first.
        RerollPending,
        Scroll,
        Rune,
        FullLines,
        Equipped,
        Slot,
        Level,
        // A bag item is required (a weapon's gear cannot be melted, inherited from or equipped here).
        Weapon
    }

    // What a strike would cost and the chance it would be made at, before it is made.
    public readonly struct AnvilQuote
    {
        public readonly AnvilRefusal Refusal;
        public readonly int ChancePercent;
        public readonly int Gold;
        public readonly bool NeedsMaterial;
        public readonly MaterialKind Material;
        public readonly int MaterialAmount;

        public AnvilQuote(AnvilRefusal refusal, int chancePercent, int gold, bool needsMaterial, MaterialKind material, int materialAmount)
        {
            Refusal = refusal;
            ChancePercent = chancePercent;
            Gold = gold;
            NeedsMaterial = needsMaterial;
            Material = material;
            MaterialAmount = materialAmount;
        }

        public bool CanStrike => Refusal == AnvilRefusal.None;
    }

    // One strike as telemetry reads it (docs/54 section 12, anvil_attempt).
    public sealed class AnvilAttempt
    {
        public ItemInstance Piece;
        public int FromLevel;
        public int BaseChancePercent;
        public int PityBefore;
        public bool Seal;
        public bool Hammer;
        public AnvilStrike Strike;
        public int Gold;
        public bool SpentMaterial;
        public MaterialKind Material;
        public int MaterialAmount;
    }

    // One reroll as telemetry reads it (docs/54 section 12, efsun_reroll): what was paid and the two sets of lines.
    public sealed class EfsunAttempt
    {
        public ItemInstance Piece;
        public bool PaidWithScroll;
        public int Gold;
        public EfsunLine[] Before;
        public EfsunLine[] After;
    }

    // The Anvil's rules over the profile (docs/54 sections 3-6, the lead's decisions of 2026-11-16), as RelicShop is the
    // Forge's: a strike (basma), an efsun reroll shown and then kept or discarded, a Binding Rune's line, a melt, an
    // inheritance and equipping. Each operation either changes nothing and returns false with the reason, or spends,
    // draws, changes and raises the profile's Changed exactly once - the save trigger - and then its own event, for
    // telemetry. Every draw comes from the profile's anvil stream (RunRandom.Anvil), one index per operation.
    public sealed class Anvil
    {
        private readonly Func<int> _freshSeed;

        public PlayerProfile Profile { get; }
        public ProfileGear Gear => Profile.Gear;
        public AnvilRefusal LastRefusal { get; private set; }

        public event Action<AnvilAttempt> Struck;
        public event Action<EfsunAttempt> Rerolled;
        // The reroll's piece and whether the new lines were kept.
        public event Action<ItemInstance, bool> RerollResolved;
        public event Action<ItemInstance, EfsunLine> LineAdded;
        // The melted item, the gold and the Ember Shards it gave.
        public event Action<ItemInstance, int, int> Melted;
        // The source (consumed), the target and the gold paid.
        public event Action<ItemInstance, ItemInstance, int> Inherited;
        public event Action<ItemInstance> Equipped;

        // freshSeed gives the anvil's seed the first time it draws (RunSeeds.Fresh by default; tests pass their own).
        public Anvil(PlayerProfile profile, Func<int> freshSeed = null)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _freshSeed = freshSeed ?? RunSeeds.Fresh;
        }

        // A weapon's upgrades, made the first time they are asked for: +0 with a forged weapon's two lines, drawn from the
        // anvil stream (one change, saved).
        public ItemInstance GearForWeapon(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId))
                throw new ArgumentException("A weapon id is required.", nameof(weaponId));
            ItemInstance existing = Gear.WeaponGear(weaponId);
            if (existing != null)
                return existing;
            RunRandom random = Draw();
            ItemInstance gear = Gear.AddWeaponGear(weaponId, EfsunRule.Roll(GearStats.MainStatOf(GearSlot.Weapon), EfsunRule.WeaponLines,
                random));
            Profile.NotifyChanged();
            return gear;
        }

        public AnvilQuote Quote(ItemInstance piece, bool useSeal = false, bool useHammer = false)
        {
            if (piece == null || !Gear.Owns(piece))
                return Refused(AnvilRefusal.NotOwned);
            if (Gear.PendingRerollPiece != null)
                return Refused(AnvilRefusal.RerollPending);
            if (piece.Level >= AnvilTable.MaxLevel)
                return Refused(AnvilRefusal.MaxLevel);
            int target = piece.Level + 1;
            int gold = AnvilTable.GoldCost(target);
            bool needs = AnvilTable.TryMaterialCost(target, out MaterialKind material, out int amount);
            if (useHammer && !AnvilRule.HammerAllowed(piece.Level))
                return new AnvilQuote(AnvilRefusal.Hammer, 0, gold, needs, material, amount);
            if (useHammer && Gear.Booster(BoosterKind.MastersHammer) < 1)
                return new AnvilQuote(AnvilRefusal.Hammer, 0, gold, needs, material, amount);
            int seals = useSeal && !useHammer ? 1 : 0;
            if (seals > 0 && Gear.Booster(BoosterKind.ForgeSeal) < 1)
                return new AnvilQuote(AnvilRefusal.Seal, 0, gold, needs, material, amount);
            int chance = AnvilRule.ChancePercent(piece.Level, piece.Pity, seals, useHammer);
            AnvilRefusal refusal = Profile.Gold < gold ? AnvilRefusal.Gold
                : needs && Gear.Material(material) < amount ? AnvilRefusal.Material
                : AnvilRefusal.None;
            return new AnvilQuote(refusal, chance, gold, needs, material, amount);
        }

        // A strike: spends the gold, the material and the chosen booster, then holds or fails. A failure keeps the item and
        // its level and adds to its pity. A booster on a strike that is already certain (the ceiling, or +1..+3) is not
        // spent.
        public bool TryStrike(ItemInstance piece, bool useSeal, bool useHammer, out AnvilStrike strike)
        {
            strike = default;
            AnvilQuote quote = Quote(piece, useSeal, useHammer);
            if (!Accept(quote.Refusal))
                return false;
            int fromLevel = piece.Level;
            int pityBefore = piece.Pity;
            bool certain = AnvilRule.ChancePercent(piece.Level, piece.Pity, 0, false) >= 100;
            bool seal = useSeal && !useHammer && !certain;
            bool hammer = useHammer && !certain;
            RunRandom random = Draw();
            if (!Profile.TrySpendGold(quote.Gold))
                throw new InvalidOperationException("The quote's gold was checked.");
            if (quote.NeedsMaterial)
                Gear.SpendMaterial(quote.Material, quote.MaterialAmount);
            if (seal)
                Gear.SpendBooster(BoosterKind.ForgeSeal, 1);
            if (hammer)
                Gear.SpendBooster(BoosterKind.MastersHammer, 1);
            strike = AnvilRule.Strike(piece.Level, piece.Pity, seal ? 1 : 0, hammer, random.NextBelow(100));
            piece.Level = strike.LevelAfter;
            piece.Pity = strike.PityAfter;
            Profile.NotifyChanged();
            Struck?.Invoke(new AnvilAttempt
            {
                Piece = piece,
                FromLevel = fromLevel,
                BaseChancePercent = AnvilTable.BaseChancePercent(fromLevel + 1),
                PityBefore = pityBefore,
                Seal = seal,
                Hammer = hammer,
                Strike = strike,
                Gold = quote.Gold,
                SpentMaterial = quote.NeedsMaterial,
                Material = quote.Material,
                MaterialAmount = quote.MaterialAmount
            });
            return true;
        }

        // Rerolls every line of the piece for its rarity's gold (a weapon's as a Rare's) or one Efsun Scroll; the new lines
        // wait beside the old until ResolveReroll keeps one set (docs/54 section 6.2: keep old or new).
        public bool TryReroll(ItemInstance piece, bool useScroll)
        {
            if (piece == null || !Gear.Owns(piece))
                return Refuse(AnvilRefusal.NotOwned);
            if (Gear.PendingRerollPiece != null)
                return Refuse(AnvilRefusal.RerollPending);
            int gold = useScroll ? 0 : EfsunRule.RerollGold(piece.Rarity);
            if (useScroll && Gear.Booster(BoosterKind.EfsunScroll) < 1)
                return Refuse(AnvilRefusal.Scroll);
            if (Profile.Gold < gold)
                return Refuse(AnvilRefusal.Gold);
            LastRefusal = AnvilRefusal.None;
            RunRandom random = Draw();
            if (useScroll)
                Gear.SpendBooster(BoosterKind.EfsunScroll, 1);
            else if (!Profile.TrySpendGold(gold))
                throw new InvalidOperationException("The gold was checked.");
            EfsunLine[] before = Copy(piece.Lines);
            EfsunLine[] after = EfsunRule.Roll(piece.MainStat, piece.Lines.Count, random);
            Gear.SetPending(piece, after);
            Profile.NotifyChanged();
            Rerolled?.Invoke(new EfsunAttempt { Piece = piece, PaidWithScroll = useScroll, Gold = gold, Before = before, After = after });
            return true;
        }

        // Keeps the shown reroll's new lines, or the old ones; false when no reroll is shown.
        public bool ResolveReroll(bool keepNew)
        {
            ItemInstance piece = Gear.PendingRerollPiece;
            if (piece == null)
                return Refuse(AnvilRefusal.NotOwned);
            LastRefusal = AnvilRefusal.None;
            if (keepNew)
                piece.SetLines(Gear.PendingRerollLines);
            Gear.ClearPending();
            Profile.NotifyChanged();
            RerollResolved?.Invoke(piece, keepNew);
            return true;
        }

        // A Binding Rune: one more line, a stat the piece does not carry, up to five.
        public bool TryAddLine(ItemInstance piece)
        {
            if (piece == null || !Gear.Owns(piece))
                return Refuse(AnvilRefusal.NotOwned);
            if (Gear.PendingRerollPiece != null)
                return Refuse(AnvilRefusal.RerollPending);
            if (piece.Lines.Count >= EfsunRule.MaxLines)
                return Refuse(AnvilRefusal.FullLines);
            if (Gear.Booster(BoosterKind.BindingRune) < 1)
                return Refuse(AnvilRefusal.Rune);
            LastRefusal = AnvilRefusal.None;
            RunRandom random = Draw();
            Gear.SpendBooster(BoosterKind.BindingRune, 1);
            EfsunLine line = EfsunRule.AddLine(piece.Lines, piece.MainStat, random);
            piece.AddLine(line);
            Profile.NotifyChanged();
            LineAdded?.Invoke(piece, line);
            return true;
        }

        // Melts a bag item that is not equipped for its rarity's gold and Ember Shards (docs/54 section 3.3).
        public bool TryMelt(ItemInstance item)
        {
            AnvilRefusal refusal = BagItemRefusal(item);
            if (refusal == AnvilRefusal.None && Gear.IsEquipped(item))
                refusal = AnvilRefusal.Equipped;
            if (!Accept(refusal))
                return false;
            int gold = AnvilTable.MeltGold(item.Rarity);
            int ember = AnvilTable.MeltEmber(item.Rarity);
            Gear.RemoveItem(item);
            Profile.AddGoldQuietly(gold);
            Gear.AddMaterial(MaterialKind.EmberShard, ember);
            Profile.NotifyChanged();
            Melted?.Invoke(item, gold, ember);
            return true;
        }

        // Moves the source's basma level onto a lower target of the same slot for a tenth of the level's expected gold
        // (docs/54 section 3.4); the source is consumed, the target's pity resets, its lines stay its own, and it takes the
        // source's place if the source was equipped.
        public bool TryInherit(ItemInstance source, ItemInstance target)
        {
            AnvilRefusal refusal = BagItemRefusal(source);
            if (refusal == AnvilRefusal.None)
                refusal = BagItemRefusal(target);
            if (refusal == AnvilRefusal.None && (source == target || source.Slot != target.Slot))
                refusal = AnvilRefusal.Slot;
            if (refusal == AnvilRefusal.None && source.Level <= target.Level)
                refusal = AnvilRefusal.Level;
            int gold = refusal == AnvilRefusal.None ? AnvilRule.InheritGold(source.Level) : 0;
            if (refusal == AnvilRefusal.None && Profile.Gold < gold)
                refusal = AnvilRefusal.Gold;
            if (!Accept(refusal))
                return false;
            bool wasEquipped = Gear.IsEquipped(source);
            if (!Profile.TrySpendGold(gold))
                throw new InvalidOperationException("The gold was checked.");
            target.Level = source.Level;
            target.Pity = 0;
            Gear.RemoveItem(source);
            if (wasEquipped)
                Gear.Equip(target);
            Profile.NotifyChanged();
            Inherited?.Invoke(source, target, gold);
            return true;
        }

        // Equips a bag item in its slot; false when it already is.
        public bool TryEquip(ItemInstance item)
        {
            AnvilRefusal refusal = BagItemRefusal(item);
            if (refusal == AnvilRefusal.None && Gear.IsEquipped(item))
                refusal = AnvilRefusal.Equipped;
            if (!Accept(refusal))
                return false;
            Gear.Equip(item);
            Profile.NotifyChanged();
            Equipped?.Invoke(item);
            return true;
        }

        private AnvilRefusal BagItemRefusal(ItemInstance item)
        {
            if (item == null || !Gear.Owns(item))
                return AnvilRefusal.NotOwned;
            if (item.IsWeapon)
                return AnvilRefusal.Weapon;
            return Gear.PendingRerollPiece != null ? AnvilRefusal.RerollPending : AnvilRefusal.None;
        }

        // The stream for this operation: seeded the first time, then one index each.
        private RunRandom Draw()
        {
            if (Gear.AnvilSeed == 0)
            {
                int seed = _freshSeed();
                Gear.SeedAnvil(seed != 0 ? seed : 1);
            }
            return RunRandom.Stream(Gear.AnvilSeed, RunRandom.Anvil, Gear.TakeDraw());
        }

        private AnvilQuote Refused(AnvilRefusal refusal) => new AnvilQuote(refusal, 0, 0, false, MaterialKind.EmberShard, 0);

        private bool Accept(AnvilRefusal refusal)
        {
            LastRefusal = refusal;
            return refusal == AnvilRefusal.None;
        }

        private bool Refuse(AnvilRefusal refusal)
        {
            LastRefusal = refusal;
            return false;
        }

        private static EfsunLine[] Copy(IReadOnlyList<EfsunLine> lines)
        {
            var copy = new EfsunLine[lines.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = lines[i];
            return copy;
        }
    }
}
