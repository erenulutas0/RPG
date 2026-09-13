using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Cryptforge.Core;

namespace Cryptforge.Progression
{
    // The Relic Forge's rules over the profile: which relics exist, what each card offers, and what a tap does.
    // A tap forges an affordable relic (which also equips it) or equips an owned one; one slot is equipped at a time.
    public sealed class RelicShop
    {
        private readonly RelicOption[] _relics;

        public PlayerProfile Profile { get; }
        public IReadOnlyList<RelicOption> Relics { get; }

        public RelicShop(PlayerProfile profile, IReadOnlyList<RelicOption> relics)
        {
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            if (relics == null)
                throw new ArgumentNullException(nameof(relics));

            _relics = new RelicOption[relics.Count];
            var ids = new HashSet<string>();
            for (int i = 0; i < relics.Count; i++)
            {
                if (relics[i] == null || !ids.Add(relics[i].Id))
                    throw new ArgumentException("Relics must be non-null with unique ids.", nameof(relics));
                _relics[i] = relics[i];
            }

            Relics = new ReadOnlyCollection<RelicOption>(_relics);
        }

        // Null when nothing is equipped or the saved id no longer matches a relic in this build.
        public RelicOption Equipped => Find(Profile.EquippedRelicId);

        // The cheapest relic not yet owned (first in catalog order on ties); null once every relic is forged.
        public RelicOption NextUnlock
        {
            get
            {
                RelicOption next = null;
                for (int i = 0; i < _relics.Length; i++)
                {
                    if (!Profile.Owns(_relics[i].Id) && (next == null || _relics[i].Price < next.Price))
                        next = _relics[i];
                }
                return next;
            }
        }

        public UnlockStatus StatusOf(RelicOption relic)
        {
            RequireListed(relic);
            if (Profile.EquippedRelicId == relic.Id)
                return UnlockStatus.Equipped;
            if (Profile.Owns(relic.Id))
                return UnlockStatus.Owned;
            return Profile.Gold >= relic.Price ? UnlockStatus.Affordable : UnlockStatus.TooExpensive;
        }

        public int GoldNeededFor(RelicOption relic)
        {
            RequireListed(relic);
            return Profile.Owns(relic.Id) ? 0 : Math.Max(0, relic.Price - Profile.Gold);
        }

        public bool TrySelect(RelicOption relic)
        {
            switch (StatusOf(relic))
            {
                case UnlockStatus.Affordable:
                    return Profile.TryForgeRelic(relic.Id, relic.Price);
                case UnlockStatus.Owned:
                    return Profile.Equip(relic.Id);
                default:
                    return false;
            }
        }

        private RelicOption Find(string id)
        {
            for (int i = 0; i < _relics.Length; i++)
            {
                if (_relics[i].Id == id)
                    return _relics[i];
            }
            return null;
        }

        private void RequireListed(RelicOption relic)
        {
            if (relic == null)
                throw new ArgumentNullException(nameof(relic));
            if (Find(relic.Id) != relic)
                throw new ArgumentException("The relic is not sold by this shop.", nameof(relic));
        }
    }
}
