using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // Presentation only: each unused altar as a glowing sigil on the floor - an ember ring, an inner ring and a flame
    // glyph - with a hold arc while the hero stays on it and the bargain spelled out when the hero is near.
    // A taken altar goes dark. AltarService owns the hold and the grant.
    [DefaultExecutionOrder(100)]
    public sealed class AltarView : MonoBehaviour
    {
        private const int Segments = 48;
        private const float LabelReach = 3f;
        private AltarService _service;
        private PrototypeTextDefinition _text;
        private ArenaView _arena;
        private Transform _hero;
        private Material _material;
        private GameObject[] _sigils;
        private LineRenderer _progress;
        private TextMesh _label;
        private readonly Vector3[] _arc = new Vector3[Segments + 1];
        public bool IsShown(int altar) => _sigils != null && _sigils[altar].activeSelf;
        public float ShownProgress { get; private set; }

        internal void Initialize(AltarService service, PrototypeTextDefinition text, Transform hero)
        {
            _service = service; _text = text; _hero = hero;
            if (!service.IsActive) return;
            _arena = FindFirstObjectByType<ArenaView>();
            _material = new Material(Shader.Find("Sprites/Default"));
            var ember = new Color(1f, .55f, .22f, 1); var violet = new Color(.72f, .5f, 1f, 1);
            _sigils = new GameObject[service.Field.Count];
            for (int i = 0; i < _sigils.Length; i++)
            {
                var sigil = new GameObject("Altar Sigil " + i); sigil.transform.SetParent(transform, false);
                Ring(sigil.transform, ember, .05f, AltarRule.Radius);
                Ring(sigil.transform, violet, .03f, AltarRule.Radius * .62f);
                var glyph = Line("Altar Flame", sigil.transform, ember, .04f);
                glyph.positionCount = 5;
                glyph.SetPositions(new[]{Project(new Vector2(-.16f,-.12f)),Project(new Vector2(-.06f,.12f)),Project(new Vector2(0,-.02f)),
                    Project(new Vector2(.06f,.16f)),Project(new Vector2(.16f,-.12f))});
                Vector3 p = new Vector3(service.Field.XOf(i), ArenaFloor.WorldY(service.Field.YOf(i)), 0);
                if (_arena != null && _arena.IsFoundryRegion) p.y += FoundrySurface.Lift(p, _arena.transform.position);
                sigil.transform.position = p;
                _sigils[i] = sigil;
            }
            _progress = Line("Altar Hold", transform, new Color(1f, .82f, .5f), .055f); _progress.useWorldSpace = true;
            var labelObject = new GameObject("Altar Instruction"); labelObject.transform.SetParent(transform, false);
            _label = labelObject.AddComponent<TextMesh>(); _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.GetComponent<MeshRenderer>().sharedMaterial = _label.font.material;
            _label.GetComponent<MeshRenderer>().sortingOrder = 100;
            _label.anchor = TextAnchor.MiddleCenter; _label.alignment = TextAlignment.Center; _label.fontSize = 48; _label.characterSize = .04f;
            _label.color = new Color(1f, .86f, .62f, 1);
            Refresh();
        }

        private void Ring(Transform parent, Color colour, float width, float radius)
        {
            var ring = Line("Altar Ring", parent, colour, width);
            ring.positionCount = Segments + 1;
            for (int i = 0; i <= Segments; i++)
            {
                float a = i * Mathf.PI * 2 / Segments;
                ring.SetPosition(i, Project(new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius)));
            }
        }

        private LineRenderer Line(string name, Transform parent, Color colour, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>(); line.transform.SetParent(parent, false);
            line.sharedMaterial = _material; line.useWorldSpace = false; line.startColor = line.endColor = colour;
            line.startWidth = line.endWidth = width; line.sortingOrder = 0; line.numCapVertices = 2; return line;
        }

        private static Vector3 Project(Vector2 floor) => new Vector3(floor.x, ArenaFloor.WorldY(floor.y), 0);
        private void LateUpdate() => Refresh();

        private void Refresh()
        {
            if (_sigils == null) return;
            var field = _service.Field;
            float heroX = _hero.position.x, heroY = ArenaFloor.FloorY(_hero.position.y);
            int nearest = -1; float best = LabelReach * LabelReach;
            for (int i = 0; i < _sigils.Length; i++)
            {
                bool shown = enabled && !field.IsUsed(i);
                _sigils[i].SetActive(shown);
                if (!shown) continue;
                float dx = heroX - field.XOf(i), dy = heroY - field.YOf(i), d = dx * dx + dy * dy;
                if (d < best) { best = d; nearest = i; }
            }
            int standing = field.Standing;
            ShownProgress = standing >= 0 ? Mathf.Clamp01(field.Held / AltarRule.DwellSeconds) : 0;
            _progress.enabled = ShownProgress > 0;
            if (_progress.enabled)
            {
                Vector3 centre = _sigils[standing].transform.position;
                for (int i = 0; i <= Segments; i++)
                {
                    float a = Mathf.PI / 2 - i * (Mathf.PI * 2 / Segments) * ShownProgress;
                    _arc[i] = centre + Project(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * AltarRule.Radius * 1.12f);
                }
                _progress.positionCount = _arc.Length; _progress.SetPositions(_arc);
            }
            _label.gameObject.SetActive(nearest >= 0);
            if (nearest < 0) return;
            _label.transform.position = _sigils[nearest].transform.position + Vector3.down * .62f;
            if (_label.text != _text.AltarInstruction) _label.text = _text.AltarInstruction;
        }

        private void OnDestroy() { if (_material != null) Destroy(_material); }
    }
}
