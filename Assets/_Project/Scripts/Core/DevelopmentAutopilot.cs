using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Progression;
using Cryptforge.UI;
using UnityEngine;

namespace Cryptforge.Core
{
    // A development build's hands-off player for device checks of the foundry (DevelopmentStart.Autopilot): it walks the
    // hero to each marked hall, takes both altars between fights, and in a fight draws the pack to the pit's rim and steps
    // out of the band while the rim glows, so the run's log measures what the pit does to a hero who uses it. It answers
    // every offer with its first card and every checkpoint with Descend, and walks onto an open exit (a minute into the
    // finale's swarm). It steers through HeroMovementInput.Hold, so a finger on the screen still takes over. Never part of
    // a player's build.
    public sealed class DevelopmentAutopilot : MonoBehaviour
    {
        private const float ChoiceDelay = .8f;
        // Design units beyond the pit's hole: where the hero lures (inside the band) and where it waits out a spill.
        private const float LureDepth = 1.3f;
        private const float ShelterDepth = PitEruptionRule.Reach + 1.4f;
        private const float Arrived = .35f;
        private const float BackOff = 1.1f;
        private const float SwarmStay = 60f;
        private CombatSetup _setup;
        private HeroMovementInput _movement;
        private Transform _hero;
        private readonly FoundryPursuit _route = new FoundryPursuit();
        private float _choiceWait;

        internal void Initialize(CombatSetup setup, HeroMovementInput movement, Transform hero)
        {
            _setup = setup;
            _movement = movement;
            _hero = hero;
            Debug.Log("[Autopilot] driving the hero (development/autopilot.txt)");
        }

        private void Update()
        {
            if (_movement == null || _setup.Run == null || _setup.Run.HasEnded)
            {
                _movement?.Release();
                return;
            }
            if (_setup.Choices.IsOpen)
            {
                _movement.Release();
                _choiceWait += Time.unscaledDeltaTime;
                if (_choiceWait >= ChoiceDelay)
                {
                    _choiceWait = 0f;
                    _setup.Choices.TrySelect(_setup.Choices.Current, ChoiceSlot());
                }
                return;
            }
            _choiceWait = 0f;
            float x = _hero.position.x, y = ArenaFloor.FloorY(_hero.position.y);
            if (Goal(x, y, out float gx, out float gy, out float stop))
                Steer(x, y, gx, gy, stop);
            else
                _movement.Release();
        }

        private int ChoiceSlot()
        {
            CheckpointOffer offer = _setup.Checkpoint.CurrentOffer;
            if (_setup.Choices.Current.Kind == ChoiceKind.Checkpoint && offer != null)
                for (int i = 0; i < offer.Choices.Count; i++)
                    if (offer.Choices[i].Kind == CheckpointKind.Descend)
                        return i;
            return 0;
        }

        private bool Goal(float x, float y, out float gx, out float gy, out float stop)
        {
            EncounterController encounter = _setup.Encounters;
            stop = Arrived;
            gx = x; gy = y;
            // The finale's exit opens at once; the autopilot fights the swarm a while first, as a player after depth does.
            if (encounter.IsExitOpen && (!encounter.IsInSwarmRoom || encounter.SwarmSeconds >= SwarmStay))
            {
                gx = encounter.ExitFloorX; gy = encounter.ExitFloorY;
                return true;
            }
            if (encounter.IsRoomSiteOpen)
            {
                gx = encounter.RoomSiteFloorX; gy = encounter.RoomSiteFloorY;
                stop = FoundryMap.RoomSiteRadius * .5f;
                return true;
            }
            bool fighting = encounter.AliveEnemyCount > 0;
            if (fighting && _setup.Pit != null && _setup.Pit.IsActive)
            {
                RimSpot(x, y, _setup.Pit.Clock.IsWarning ? ShelterDepth : LureDepth, out gx, out gy);
                // At the lure spot, keep a step ahead of the nearest enemy along the rim instead of trading blows.
                if (!_setup.Pit.Clock.IsWarning && Nearest(x, y, out float ex, out float ey) &&
                    ArenaFloor.FloorDistanceSquared(x, y, gx, gy) < 1f && ArenaFloor.FloorDistanceSquared(x, y, ex, ey) < BackOff * BackOff)
                {
                    gx = x + (x - ex); gy = y + (y - ey);
                    stop = 0f;
                }
                return true;
            }
            if (fighting)
            {
                if (!Nearest(x, y, out float ex, out float ey) || ArenaFloor.FloorDistanceSquared(x, y, ex, ey) >= BackOff * BackOff)
                    return false;
                gx = x + (x - ex); gy = y + (y - ey);
                stop = 0f;
                return true;
            }
            AltarField altars = _setup.Altars != null && _setup.Altars.IsActive ? _setup.Altars.Field : null;
            if (altars == null)
                return false;
            float best = float.PositiveInfinity;
            for (int i = 0; i < altars.Count; i++)
            {
                if (altars.IsUsed(i))
                    continue;
                float d = ArenaFloor.FloorDistanceSquared(x, y, altars.XOf(i), altars.YOf(i));
                if (d >= best)
                    continue;
                best = d; gx = altars.XOf(i); gy = altars.YOf(i);
            }
            stop = .1f;
            return !float.IsPositiveInfinity(best);
        }

        // The point Depth design units out from the pit's hole on the side the hero stands.
        private static void RimSpot(float x, float y, float depth, out float gx, out float gy)
        {
            FoundryMap.ToDesign(x, y, out float u, out float v);
            float cu = Mathf.Clamp(u, FoundryMap.PitWest, FoundryMap.PitEast);
            float cv = Mathf.Clamp(v, FoundryMap.PitSouth, FoundryMap.PitNorth);
            Vector2 away = new Vector2(u - cu, v - cv);
            if (away.sqrMagnitude < 1e-4f)
                away = new Vector2(0f, -1f);
            away.Normalize();
            FoundryMap.ToFloor(cu + away.x * depth, cv + away.y * depth, out gx, out gy);
        }

        private bool Nearest(float x, float y, out float ex, out float ey)
        {
            EncounterController encounter = _setup.Encounters;
            float best = float.PositiveInfinity;
            ex = x; ey = y;
            for (int i = 0; i < encounter.WaveEnemyCount; i++)
            {
                Health enemy = encounter.WaveEnemyAt(i);
                if (enemy == null || !enemy.IsAlive || !encounter.TryFloorPositionOf(enemy, out float px, out float py))
                    continue;
                float d = ArenaFloor.FloorDistanceSquared(x, y, px, py);
                if (d >= best)
                    continue;
                best = d; ex = px; ey = py;
            }
            return !float.IsPositiveInfinity(best);
        }

        private void Steer(float x, float y, float gx, float gy, float stop)
        {
            if (ArenaFloor.FloorDistanceSquared(x, y, gx, gy) <= stop * stop)
            {
                _movement.Release();
                return;
            }
            float tx = gx, ty = gy;
            if (!FoundryMap.Segment(x, y, gx, gy, .6f))
                _route.Target(x, y, gx, gy, ref tx, ref ty);
            var steer = new Vector2(tx - x, ty - y);
            if (steer.sqrMagnitude < 1e-6f)
            {
                _movement.Release();
                return;
            }
            _movement.Hold(steer.normalized);
        }
    }
}
