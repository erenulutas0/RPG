namespace Cryptforge.Combat
{
    // Something ahead of a body's health that judges each landing before it lands (Health.Guard): the Mirror Sentry's
    // mirror (MirrorGuard) blunts a landing of the hero's made from its front. Returns the landing as the body takes it -
    // the context itself when the guard lets it through.
    public interface IDamageGuard
    {
        DamageContext Guard(DamageContext context);
    }
}
