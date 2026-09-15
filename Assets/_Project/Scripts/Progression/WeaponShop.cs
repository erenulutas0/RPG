using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cryptforge.Core;

namespace Cryptforge.Progression
{
    // The Relic Forge's weapon rack over the profile. The hero's starting weapon is always owned and is carried whenever no
    // other owned weapon is equipped. A tap forges an affordable weapon (which also equips it) or equips an owned one.
    public sealed class WeaponShop
    {
        private readonly WeaponOption[] _weapons;

        public PlayerProfile Profile { get; }
        public IReadOnlyList<WeaponOption> Weapons { get; }
        public WeaponOption StartingWeapon { get; }

        // Raised once when a tap spends gold on a weapon, after the profile accepted the purchase, so observers such as
        // telemetry can count forge gold spent; profile.Changed cannot tell a purchase from an equip. Not raised for
        // equipping an owned weapon (the starting weapon included) or for a tap that spends nothing.
        public event Action<WeaponOption> Forged;

        public WeaponShop(PlayerProfile profile, IReadOnlyList<WeaponOption> weapons, WeaponOption startingWeapon)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            if (weapons == null)
                throw new ArgumentNullException(nameof(weapons));

            _weapons = new WeaponOption[weapons.Count];
            var ids = new HashSet<string>();
            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i] == null || !ids.Add(weapons[i].Id))
                    throw new ArgumentException("Weapons must be non-null with unique ids.", nameof(weapons));
                _weapons[i] = weapons[i];
            }

            Weapons = new ReadOnlyCollection<WeaponOption>(_weapons);
            if (startingWeapon == null || Find(startingWeapon.Id) != startingWeapon)
                throw new ArgumentException("The starting weapon must be one of the listed weapons.", nameof(startingWeapon));
            StartingWeapon = startingWeapon;
        }

        // The saved weapon while this build still sells it and the profile owns it; otherwise the starting weapon.
        public WeaponOption Equipped
        {
            get
            {
                WeaponOption saved = Find(Profile.EquippedWeaponId);
                return saved != null && IsOwned(saved) ? saved : StartingWeapon;
            }
        }

        // The cheapest weapon not yet owned (first in catalog order on ties); null once every weapon is forged.
        public WeaponOption NextUnlock
        {
            get
            {
                WeaponOption next = null;
                for (int i = 0; i < _weapons.Length; i++)
                {
                    if (!IsOwned(_weapons[i]) && (next == null || _weapons[i].Price < next.Price))
                        next = _weapons[i];
                }
                return next;
            }
        }

        public UnlockStatus StatusOf(WeaponOption weapon)
        {
            RequireListed(weapon);
            if (weapon == Equipped)
                return UnlockStatus.Equipped;
            if (IsOwned(weapon))
                return UnlockStatus.Owned;
            return Profile.Gold >= weapon.Price ? UnlockStatus.Affordable : UnlockStatus.TooExpensive;
        }

        public int GoldNeededFor(WeaponOption weapon)
        {
            RequireListed(weapon);
            return IsOwned(weapon) ? 0 : Math.Max(0, weapon.Price - Profile.Gold);
        }

        public bool TrySelect(WeaponOption weapon)
        {
            switch (StatusOf(weapon))
            {
                case UnlockStatus.Affordable:
                    if (!Profile.TryForgeWeapon(weapon.Id, weapon.Price))
                        return false;
                    Forged?.Invoke(weapon);
                    return true;
                case UnlockStatus.Owned:
                    return weapon == StartingWeapon ? Profile.UnequipWeapon() : Profile.EquipWeapon(weapon.Id);
                default:
                    return false;
            }
        }

        private bool IsOwned(WeaponOption weapon) => weapon == StartingWeapon || Profile.OwnsWeapon(weapon.Id);

        private WeaponOption Find(string id)
        {
            for (int i = 0; i < _weapons.Length; i++)
            {
                if (_weapons[i].Id == id)
                    return _weapons[i];
            }
            return null;
        }

        private void RequireListed(WeaponOption weapon)
        {
            if (weapon == null)
                throw new ArgumentNullException(nameof(weapon));
            if (Find(weapon.Id) != weapon)
                throw new ArgumentException("The weapon is not sold by this shop.", nameof(weapon));
        }
    }
}
