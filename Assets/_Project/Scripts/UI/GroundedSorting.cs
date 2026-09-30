using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Cryptforge.UI
{
    // Sort whole actors by their floor root, not the changing bounds of body/weapon sprites.
    // Independent groups keep health bars and contact shadows in their existing global layers.
    internal static class GroundedSorting
    {
        // The combatant rank: hero, enemies and ground drops attach their roots at this order.
        internal const int ActorOrder = 1;
        private static readonly List<Transform> ActorRoots = new List<Transform>();

        // Every actor root attached so far, for presentation that depth-tests actors against scenery
        // (FoundryVisibilityView). Destroyed roots stay as null entries until a reader or the next attach prunes them.
        internal static List<Transform> Actors => ActorRoots;

        internal static SortingGroup Attach(Transform root, int layer, int order)
        {
            var group = root.GetComponent<SortingGroup>();
            if (group == null) group = root.gameObject.AddComponent<SortingGroup>();
            group.sortingLayerID = layer;
            group.sortingOrder = order;
            group.sortAtRoot = true;
            if (order == ActorOrder && !ActorRoots.Contains(root))
            {
                ActorRoots.RemoveAll(actor => actor == null);
                ActorRoots.Add(root);
            }
            return group;
        }
    }
}
