using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // Grounded under the actor root, so the body can nudge/flash without dragging the contact patch along with it.
    // All views borrow one session-owned texture; movement changes only a transform, with no drawing/allocation.
    public sealed class ContactShadowView : MonoBehaviour
    {
        private Health _health;
        private ArenaView _arena;
        private float _width;
        public SpriteRenderer Renderer { get; private set; }

        internal static ContactShadowView Attach(Transform actor, SpriteRenderer body, float width, Health health)
        {
            // A live actor clone already contains the generated child, but not its non-serialized view state.
            // Rebind that child to the new owner instead of leaving an orphan view and adding another shadow.
            Transform existing = actor.Find("Contact Shadow");
            GameObject child = existing != null ? existing.gameObject : new GameObject("Contact Shadow");
            if (existing == null)
                child.transform.SetParent(actor, false);
            var view = child.GetComponent<ContactShadowView>();
            if (view == null)
                view = child.AddComponent<ContactShadowView>();
            view._width = width;
            view._health = health;
            view.Renderer = child.GetComponent<SpriteRenderer>();
            if (view.Renderer == null)
                view.Renderer = child.AddComponent<SpriteRenderer>();
            view.Renderer.sprite = ArenaSpriteCache.ContactShadow;
            view.Renderer.sortingLayerID = body.sortingLayerID;
            view.Renderer.sortingOrder = -10;
            GroundedSorting.Attach(child.transform, body.sortingLayerID, -10);
            child.transform.localScale = new Vector3(width, width, 1f);
            return view;
        }

        private void Start()
        {
            _arena = FindFirstObjectByType<ArenaView>();
            if (_arena != null)
                Renderer.sortingOrder = _arena.GroundEffectSortingOrder;
            GroundedSorting.Attach(transform, Renderer.sortingLayerID, Renderer.sortingOrder);
            Refresh();
        }

        private void LateUpdate() => Refresh();

        internal void Refresh()
        {
            if (Renderer == null)
                return;
            Renderer.enabled = _health == null || _health.IsAlive;
            if (!Renderer.enabled)
                return;
            float fit = 1f;
            if (_arena != null)
            {
                Vector3 position = transform.position - _arena.transform.position;
                fit = ContactShadowArt.Fit(_arena.Geometry, position.x, position.y, _width);
            }
            transform.localScale = new Vector3(_width * fit, _width * fit, 1f);
        }
    }
}
