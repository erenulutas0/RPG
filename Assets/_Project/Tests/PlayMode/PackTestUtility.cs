using System.Text;
using Cryptforge.Combat;

namespace Cryptforge.Tests
{
    // Scene tests drive packs wave by wave; the hero's own cleaves can finish enemies in any order, so tests describe a
    // wave by what spawned rather than by who fell first.
    internal static class PackTestUtility
    {
        public const float Lethal = 100000f;

        // "room.wave:Room name:Enemy+Enemy" for the current wave.
        public static string Describe(EncounterController encounters)
        {
            var names = new StringBuilder();
            for (int i = 0; i < encounters.WaveEnemyCount; i++)
            {
                if (i > 0)
                    names.Append('+');
                names.Append(encounters.DefinitionOf(encounters.WaveEnemyAt(i)).DisplayName);
            }
            return $"{encounters.RoomNumber}.{encounters.WaveNumber}:{encounters.CurrentRoom.DisplayName}:{names}";
        }

        public static bool AnyAlive(EncounterController encounters)
        {
            for (int i = 0; i < encounters.WaveEnemyCount; i++)
            {
                Health enemy = encounters.WaveEnemyAt(i);
                if (enemy != null && enemy.IsAlive)
                    return true;
            }
            return false;
        }

        public static void KillWave(EncounterController encounters)
        {
            for (int i = 0; i < encounters.WaveEnemyCount; i++)
            {
                Health enemy = encounters.WaveEnemyAt(i);
                if (enemy != null && enemy.IsAlive)
                    enemy.ApplyDamage(new DamageContext(Lethal));
            }
        }
    }
}
