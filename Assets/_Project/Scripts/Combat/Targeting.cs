using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.Combat
{
    public sealed class Targeting : MonoBehaviour
    {
        [SerializeField] private Health[] _candidates;

        // The encounter owner supplies candidates explicitly; there are still no scene searches or registries.
        // On equal distance the earlier candidate wins, so a pack is fought in its spawn order.
        public void SetCandidates(Health[] candidates) => _candidates = candidates;

        public Health Acquire(float range)
        {
            Health nearest = null;
            float nearestDistanceSquared = range * range;
            if (_candidates == null)
                return null;

            for (int i = 0; i < _candidates.Length; i++)
            {
                Health candidate = _candidates[i];
                if (!IsValid(candidate))
                    continue;

                float distanceSquared = ((Vector2)(candidate.transform.position - transform.position)).sqrMagnitude;
                if (nearest == null ? distanceSquared <= nearestDistanceSquared : distanceSquared < nearestDistanceSquared)
                {
                    nearest = candidate;
                    nearestDistanceSquared = distanceSquared;
                }
            }

            return nearest;
        }

        // Fills results with the other living candidates within radius of center, nearest first (earlier on ties).
        public void CollectNear(Health center, float radius, List<IDamageable> results)
        {
            results.Clear();
            if (_candidates == null || center == null)
                return;

            float radiusSquared = radius * radius;
            Vector2 origin = center.transform.position;
            for (int i = 0; i < _candidates.Length; i++)
            {
                Health candidate = _candidates[i];
                if (candidate == center || !IsValid(candidate))
                    continue;

                float distanceSquared = ((Vector2)candidate.transform.position - origin).sqrMagnitude;
                if (distanceSquared > radiusSquared)
                    continue;

                int insertAt = results.Count;
                while (insertAt > 0 && DistanceSquared((Health)results[insertAt - 1], origin) > distanceSquared)
                    insertAt--;
                results.Insert(insertAt, candidate);
            }
        }

        private bool IsValid(Health candidate) =>
            candidate != null && candidate.transform != transform && candidate.isActiveAndEnabled && candidate.IsAlive;

        private static float DistanceSquared(Health health, Vector2 origin) =>
            ((Vector2)health.transform.position - origin).sqrMagnitude;
    }
}
