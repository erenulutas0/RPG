namespace Cryptforge.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(DamageContext context);
    }
}
