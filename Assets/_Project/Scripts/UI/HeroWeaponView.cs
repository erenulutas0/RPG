using System;
using Cryptforge.Content;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // Shows the placeholder loadout of the weapon the hero carries this run and hides the others.
    public sealed class HeroWeaponView : MonoBehaviour
    {
        [Serializable]
        private struct Loadout
        {
            public WeaponDefinition Weapon;
            public GameObject View;
        }

        [SerializeField] private CombatSetup _setup;
        [SerializeField] private Loadout[] _loadouts;

        public GameObject ShownLoadout { get; private set; }

        // CombatSetup chooses the weapon in Awake, so Start can show it.
        private void Start()
        {
            if (_setup == null || _setup.HeroWeapon == null || _loadouts == null)
            {
                Debug.LogError("HeroWeaponView is missing its setup or loadouts.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < _loadouts.Length; i++)
            {
                if (_loadouts[i].Weapon == null || _loadouts[i].View == null)
                {
                    Debug.LogError($"HeroWeaponView loadout {i} is missing its weapon or view.", this);
                    continue;
                }

                bool carried = _loadouts[i].Weapon == _setup.HeroWeapon;
                _loadouts[i].View.SetActive(carried);
                if (carried)
                    ShownLoadout = _loadouts[i].View;
            }

            if (ShownLoadout == null)
                Debug.LogError($"HeroWeaponView has no loadout for {_setup.HeroWeapon.DisplayName}.", this);
        }
    }
}
