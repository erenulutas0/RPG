using Cryptforge.Combat;
using Cryptforge.Content;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private Health _hero;
        [SerializeField] private Health _enemy;
        [SerializeField] private AttackController _attack;
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private EnemyDefinition _enemyDefinition;
        [SerializeField] private PrototypeTextDefinition _text;
        private string _heroLabel;
        private string _enemyLabel;
        private string _weaponLabel;
        private string _attackLabel;
        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _statusStyle;
        private bool _subscribed;

        private void Start()
        {
            if (_hero == null || _enemy == null || _attack == null || _heroDefinition == null ||
                _enemyDefinition == null || _text == null || _heroDefinition.StartingWeapon == null)
            {
                Debug.LogError("PrototypeHud is missing a scene or content reference.", this);
                enabled = false;
                return;
            }
            Subscribe();
        }

        private void OnEnable()
        {
            if (_weaponLabel != null)
                Subscribe();
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;
            _hero.Changed += RefreshLabels;
            _enemy.Changed += RefreshLabels;
            _attack.Attacked += RefreshLabels;
            _subscribed = true;
            WeaponDefinition weapon = _heroDefinition.StartingWeapon;
            _weaponLabel = string.Format(_text.WeaponFormat, weapon.DisplayName, weapon.Damage, weapon.Interval);
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            _heroLabel = string.Format(_text.HealthFormat, _heroDefinition.DisplayName, _hero.Current, _hero.Maximum);
            _enemyLabel = string.Format(_text.HealthFormat, _enemyDefinition.DisplayName, _enemy.Current, _enemy.Maximum);
            _attackLabel = string.Format(_text.AttackCountFormat, _attack.AttackCount);
        }

        private void OnGUI()
        {
            if (!_subscribed)
                return;

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, wordWrap = true };
                _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true };
                _statusStyle = new GUIStyle(_labelStyle) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            }

            Rect safe = Screen.safeArea;
            float scale = Mathf.Min(safe.width / 420f, safe.height / 740f);
            if (scale <= 0f)
                return;
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(safe.x, Screen.height - safe.yMax, 0f), Quaternion.identity, Vector3.one * scale);
            float width = safe.width / scale;
            float height = safe.height / scale;
            GUI.Label(new Rect(24f, 18f, width - 48f, 45f), _text.Title, _titleStyle);
            GUI.Label(new Rect(24f, 64f, width - 48f, 54f), _text.Subtitle, _labelStyle);
            GUI.Label(new Rect(24f, 122f, width - 48f, 55f), _weaponLabel, _labelStyle);
            GUI.Label(new Rect(24f, 185f, width - 48f, 35f), _enemyLabel, _labelStyle);
            DrawHealthBar(new Rect(24f, 226f, width - 48f, 10f), _enemy, new Color(1f, 0.55f, 0.35f));
            GUI.Label(new Rect(24f, height - 210f, width - 48f, 35f), _heroLabel, _labelStyle);
            DrawHealthBar(new Rect(24f, height - 170f, width - 48f, 10f), _hero, new Color(0.3f, 0.8f, 1f));
            GUI.Label(new Rect(24f, height - 142f, width - 48f, 70f), _enemy.IsAlive ? _text.Fighting : _text.Victory, _statusStyle);
            GUI.Label(new Rect(24f, height - 65f, width - 48f, 35f), _attackLabel, _labelStyle);
            GUI.matrix = previous;
        }

        private static void DrawHealthBar(Rect rect, Health health, Color color)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.18f, 0.22f, 0.29f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            rect.width *= health.Maximum > 0f ? health.Current / health.Maximum : 0f;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void OnDisable()
        {
            if (!_subscribed)
                return;
            _hero.Changed -= RefreshLabels;
            _enemy.Changed -= RefreshLabels;
            _attack.Attacked -= RefreshLabels;
            _subscribed = false;
        }
    }
}
