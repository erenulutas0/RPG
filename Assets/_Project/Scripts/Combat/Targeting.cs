using UnityEngine;

namespace Cryptforge.Combat
{
    public sealed class Targeting : MonoBehaviour
    {
        [SerializeField] private Health[] _candidates;

        public Health Acquire(float range)
        {
            Health nearest = null;
            float nearestDistanceSquared = range * range;
            if (_candidates == null)
                return null;

            for (int i = 0; i < _candidates.Length; i++)
            {
                Health candidate = _candidates[i];
                if (candidate == null || candidate.transform == transform ||
                    !candidate.isActiveAndEnabled || !candidate.IsAlive)
                    continue;

                float distanceSquared = ((Vector2)(candidate.transform.position - transform.position)).sqrMagnitude;
                if (distanceSquared <= nearestDistanceSquared)
                {
                    nearest = candidate;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            return nearest;
        }
    }
}
