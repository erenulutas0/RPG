namespace Cryptforge.Combat
{
    public interface IHealable
    {
        float Maximum { get; }
        bool IsAlive { get; }
        void Heal(float amount);
    }
}
