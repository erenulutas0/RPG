namespace Cryptforge.Combat
{
    public interface IHealable
    {
        float Current { get; }
        float Maximum { get; }
        bool IsAlive { get; }
        void Heal(float amount);
    }
}
