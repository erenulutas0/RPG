using UnityEngine;
using UnityEngine.Rendering;

namespace Cryptforge.UI
{
    // Sort whole actors by their floor root, not the changing bounds of body/weapon sprites.
    // Independent groups keep health bars and contact shadows in their existing global layers.
    internal static class GroundedSorting
    {
        internal static SortingGroup Attach(Transform root, int layer, int order)
        {
            var group = root.GetComponent<SortingGroup>();
            if (group == null) group = root.gameObject.AddComponent<SortingGroup>();
            group.sortingLayerID = layer;
            group.sortingOrder = order;
            group.sortAtRoot = true;
            return group;
        }
    }
}
