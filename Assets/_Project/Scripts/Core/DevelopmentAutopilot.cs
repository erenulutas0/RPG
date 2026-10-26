using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Economy;
using Cryptforge.Progression;
using Cryptforge.UI;
using UnityEngine;

namespace Cryptforge.Core
{
    // A development build's hands-off player for device checks of the foundry (DevelopmentStart.Autopilot): it walks the
    // hero to each marked hall, picks up the drops beside it and takes both altars between fights, kites any enemy that
    // comes close, draws a node fight to the pit's rim and steps out of the band while the rim glows, so the run's log
    // measures what the floor does to a hero who plays it. It answers every offer with its first card and every
    // checkpoint with Descend, and walks onto an open exit (a minute into the finale's swarm). It steers through
    // HeroMovementInput.Hold, so a finger on the screen still takes over. Never part of a player's build.
    public sealed class DevelopmentAutopilot : MonoBehaviour
    {
        private const float ChoiceDelay = .8f;
        // Design units beyond the pit's hole: where the hero lures (inside the band) and where it waits out a spill.
        private const float LureDepth = 1.3f;
        private const float ShelterDepth = PitEruptionRule.Reach + 1.4f;
        private const float Arrived = .35f;
        private const float SwarmStay = 60f;
        // How far between fights the autopilot steps aside for a drop. It was 10, and with a horde dropping experience all
        // round the hero it chased drops for minutes instead of walking to the next hall; the floor's end sweeps the rest.
        private const float DropDetour = 4f;
        // Kiting, as a player in a crowd does (docs/41): every enemy within KiteReach pushes the hero away, the nearer the
        // harder, and the hero walks away from the push turned KiteTurn round it, so it circles the crowd instead of backing
        // into a wall. Just beyond a melee enemy's 1.5: at 2.2 it kited all the time and never stopped for a drop.
        private const float KiteReach = 1.6f;
        private const float KiteTurn = 35f * Mathf.Deg2Rad;
        private const float KiteStride = 2f;
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
            bool fighting = encounter.AliveEnemyCount > 0;
            bool pit = _setup.Pit != null && _setup.Pit.IsActive;
            // The Warden's mark under the hero: out of it first, as a player who reads it does.
            WardenSlamService warden = _setup.Warden;
            if (warden != null && warden.IsFighting && warden.Clock.IsMarked)
            {
                float mx = x - warden.MarkX, my = y - warden.MarkY;
                float reach = WardenRule.SlamRadius + .6f;
                if (mx * mx + my * my < reach * reach && EscapeMark(x, y, warden.MarkX, warden.MarkY, reach + .4f, out gx, out gy))
                {
                    stop = 0f;
                    return true;
                }
            }
            // A spill coming and the hero on the rim: shelter first, whatever is chasing.
            if (pit && _setup.Pit.IsRunning && _setup.Pit.Clock.IsWarning && PitEruptionRule.InBand(x, y))
            {
                RimSpot(x, y, ShelterDepth, out gx, out gy);
                return true;
            }
            if (Threat(x, y, out float tx, out float ty))
            {
                float cos = Mathf.Cos(KiteTurn), sin = Mathf.Sin(KiteTurn);
                gx = x + (tx * cos - ty * sin) * KiteStride;
                gy = y + (tx * sin + ty * cos) * KiteStride;
                stop = 0f;
                return true;
            }
            // A node fight is drawn to the pit's rim; the horde alone is fought wherever the hero is going.
            if (fighting && pit)
            {
                RimSpot(x, y, LureDepth, out gx, out gy);
                return true;
            }
            if (fighting)
                return false;
            // Between fights, as a player would: the drops in sight, then the floor's altars, then the next hall.
            if (Nearest(_setup.Drops, x, y, out gx, out gy))
            {
                stop = DropRule.PickupRadius * .5f;
                return true;
            }
            if (NearestAltar(x, y, out gx, out gy))
            {
                stop = .1f;
                return true;
            }
            if (encounter.IsRoomSiteOpen)
            {
                gx = encounter.RoomSiteFloorX; gy = encounter.RoomSiteFloorY;
                stop = FoundryMap.RoomSiteRadius * .5f;
                return true;
            }
            return false;
        }

        private static bool Nearest(GroundDrops drops, float x, float y, out float gx, out float gy)
        {
            gx = x; gy = y;
            float best = DropDetour * DropDetour;
            bool found = false;
            for (int i = 0; drops != null && i < drops.Count; i++)
            {
                GroundDrops.Drop drop = drops.At(i);
                float d = ArenaFloor.FloorDistanceSquared(x, y, drop.X, drop.Y);
                if (d >= best)
                    continue;
                best = d; gx = drop.X; gy = drop.Y; found = true;
            }
            return found;
        }

        private bool NearestAltar(float x, float y, out float gx, out float gy)
        {
            gx = x; gy = y;
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

        // The nearest of eight points round the mark, just past its reach, that the hero can walk straight to: the straight
        // line away from the centre ran into the gate's walls and the hero stood still on the mark.
        private static bool EscapeMark(float x, float y, float markX, float markY, float radius, out float gx, out float gy)
        {
            gx = x; gy = y;
            float best = float.PositiveInfinity;
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI / 4f;
                float px = markX + Mathf.Cos(a) * radius, py = markY + Mathf.Sin(a) * radius;
                if (!FoundryMap.Contains(px, py, HeroMotion.EdgeMargin) || !FoundryMap.Segment(x, y, px, py, HeroMotion.EdgeMargin))
                    continue;
                float d = ArenaFloor.FloorDistanceSquared(x, y, px, py);
                if (d >= best)
                    continue;
                best = d; gx = px; gy = py;
            }
            return !float.IsPositiveInfinity(best);
        }

        // The push of every living enemy within KiteReach, as a unit direction away from them; false when none is that close.
        private bool Threat(float x, float y, out float tx, out float ty)
        {
            EncounterController encounter = _setup.Encounters;
            tx = ty = 0f;
            for (int i = 0; i < encounter.EnemyCount; i++)
            {
                Health enemy = encounter.EnemyAt(i);
                if (enemy == null || !enemy.IsAlive || !encounter.TryFloorPositionOf(enemy, out float px, out float py))
                    continue;
                float d = ArenaFloor.FloorDistanceSquared(x, y, px, py);
                if (d >= KiteReach * KiteReach || d < 1e-6f)
                    continue;
                tx += (x - px) / d;
                ty += (y - py) / d;
            }
            float length = Mathf.Sqrt(tx * tx + ty * ty);
            if (length < 1e-4f)
                return false;
            tx /= length;
            ty /= length;
            return true;
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
