using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed class CombatantView : MonoBehaviour
    {
        [SerializeField] private Health _health;
        [SerializeField] private AttackController _attack;
        [SerializeField] private SpriteRenderer _body;
        [SerializeField, Min(0.01f)] private float _feedbackDuration = 0.14f;
        [SerializeField] private float _attackNudge = 0.2f;
        private Color _baseColor;
        private Vector3 _basePosition;
        private float _feedbackRemaining;
        private bool _subscribed;

        private void OnEnable()
        {
            if (_health == null || _body == null)
            {
                Debug.LogError("CombatantView needs health and a body renderer.", this);
                enabled = false;
                return;
            }

            _baseColor = _body.color;
            _basePosition = _body.transform.localPosition;
            _health.Changed += OnDamaged;
            _health.Died += OnDied;
            if (_attack != null)
                _attack.Attacked += OnAttacked;
            _subscribed = true;
        }

        private void Start() => _body.enabled = _health.IsAlive;

        private void OnDamaged()
        {
            _body.color = Color.white;
            _feedbackRemaining = _feedbackDuration;
        }

        private void OnAttacked()
        {
            _body.transform.localPosition = _basePosition + Vector3.up * _attackNudge;
            _feedbackRemaining = _feedbackDuration;
        }

        private void OnDied()
        {
            _body.enabled = false;
            // Hide child equipment as well as the body when its owner dies.
            _body.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_feedbackRemaining <= 0f)
                return;

            _feedbackRemaining -= Time.deltaTime;
            if (_feedbackRemaining <= 0f)
                ResetFeedback();
        }

        private void ResetFeedback()
        {
            if (_body == null)
                return;
            _body.color = _baseColor;
            _body.transform.localPosition = _basePosition;
            _feedbackRemaining = 0f;
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                _health.Changed -= OnDamaged;
                _health.Died -= OnDied;
                if (_attack != null)
                    _attack.Attacked -= OnAttacked;
                _subscribed = false;
            }
            ResetFeedback();
        }
    }
}
