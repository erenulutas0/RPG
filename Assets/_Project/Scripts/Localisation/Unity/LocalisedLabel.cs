using System;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.Localisation
{
    // A label whose words follow the language: Bind gives it the function that writes them, applied at once, again when it
    // is enabled and whenever the language changes (Loc.Changed). For the static words of the views built in code - a
    // title, a button's name. A view's live text (a count, a price) is written by the view itself, which refreshes on
    // Loc.Changed where it shows any.
    [AddComponentMenu("")]
    public sealed class LocalisedLabel : MonoBehaviour
    {
        private Text _text;
        private TextMesh _mesh;
        private Func<string> _source;
        private bool _subscribed;

        public static LocalisedLabel Bind(Text text, Func<string> source)
        {
            if (text == null || source == null)
                return null;
            LocalisedLabel label = Host(text.gameObject);
            label._text = text;
            label._source = source;
            label.Apply();
            return label;
        }

        public static LocalisedLabel Bind(TextMesh mesh, Func<string> source)
        {
            if (mesh == null || source == null)
                return null;
            LocalisedLabel label = Host(mesh.gameObject);
            label._mesh = mesh;
            label._source = source;
            label.Apply();
            return label;
        }

        private static LocalisedLabel Host(GameObject host) =>
            host.TryGetComponent(out LocalisedLabel label) ? label : host.AddComponent<LocalisedLabel>();

        public void Apply()
        {
            if (_source == null)
                return;
            string words = _source() ?? string.Empty;
            if (_text != null && _text.text != words)
                _text.text = words;
            if (_mesh != null && _mesh.text != words)
                _mesh.text = words;
        }

        private void OnEnable()
        {
            if (!_subscribed)
            {
                Loc.Changed += Apply;
                _subscribed = true;
            }
            Apply();
        }

        private void OnDisable()
        {
            if (!_subscribed)
                return;
            Loc.Changed -= Apply;
            _subscribed = false;
        }
    }
}
