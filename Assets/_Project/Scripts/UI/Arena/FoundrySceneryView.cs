using UnityEngine;

namespace Cryptforge.UI
{
    // Owns one foundation mesh; borrows the imported sprite and material. No gameplay or per-frame work.
    public sealed class FoundrySceneryView : MonoBehaviour
    {
        public void Build(Cryptforge.Art.ArenaGeometry geometry, Sprite sprite, Material floor, int order)
        {
            var prop = new GameObject("Perimeter furnace");
            prop.transform.SetParent(transform, false);
            prop.transform.localPosition = new Vector3(1.6f, geometry.WorldTop + .45f, 0);
            prop.transform.localScale = Vector3.one * .72f;
            var renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order + 1;
            // A visual foundation outside the playable apex. No collision or walkable extension is implied.
            var plinth = new GameObject("Furnace foundation");
            plinth.transform.SetParent(transform, false);
            _plinthMesh = new Mesh();
            _plinthMesh.vertices = new[] { new Vector3(.4f,13.1f),new Vector3(2.4f,14f),
                new Vector3(1.6f,14.8f),new Vector3(0,13.8f),
                new Vector3(.4f,12.85f),new Vector3(2.4f,13.75f),new Vector3(0,13.55f) };
            var vertices = _plinthMesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i].y += geometry.WorldTop - 13.5f;
            _plinthMesh.vertices = vertices;
            _plinthMesh.triangles = new[] {0,1,2,0,2,3,4,5,1,4,1,0,6,4,0,6,0,3};
            _plinthMesh.uv = new[] {Vector2.zero,new Vector2(.35f,0),new Vector2(.35f,.35f),
                new Vector2(0,.35f),new Vector2(0,-.12f),new Vector2(.35f,-.12f),new Vector2(-.12f,.35f)};
            _plinthMesh.colors = new[] {Color.white,Color.white,Color.white,Color.white,
                new Color(.45f,.45f,.5f),new Color(.45f,.45f,.5f),new Color(.45f,.45f,.5f)};
            _plinthMesh.RecalculateBounds();
            plinth.AddComponent<MeshFilter>().sharedMesh = _plinthMesh;
            var foundation = plinth.AddComponent<MeshRenderer>();
            foundation.sharedMaterial = floor;
            foundation.sortingOrder = order;

        }

        private Mesh _plinthMesh;
        private void OnDestroy() { if (_plinthMesh != null) Destroy(_plinthMesh); }
    }
}


