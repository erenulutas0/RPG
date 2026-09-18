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

                float distanceSquared = FloorDistanceSquared(candidate.transform.position, transform.position);
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
            Vector3 origin = center.transform.position;
            for (int i = 0; i < _candidates.Length; i++)
            {
                Health candidate = _candidates[i];
                if (candidate == center || !IsValid(candidate))
                    continue;

                float distanceSquared = FloorDistanceSquared(candidate.transform.position, origin);
                if (distanceSquared > radiusSquared)
                    continue;

                int insertAt = results.Count;
                while (insertAt > 0 && FloorDistanceSquared(((Health)results[insertAt - 1]).transform.position, origin) > distanceSquared)
                    insertAt--;
                results.Insert(insertAt, candidate);
            }
        }

        private bool IsValid(Health candidate) =>
            candidate != null && candidate.transform != transform && candidate.isActiveAndEnabled && candidate.IsAlive;

        // Reach and splash are measured on the arena floor, not on the screen, and through the one function the Descent
        // simulation measures with, so a near-tie between two candidates resolves the same way in both.
        private static float FloorDistanceSquared(Vector3 position, Vector3 origin) =>
            ArenaFloor.FloorDistanceSquared(position.x, ArenaFloor.FloorY(position.y), origin.x, ArenaFloor.FloorY(origin.y));
    }
}
