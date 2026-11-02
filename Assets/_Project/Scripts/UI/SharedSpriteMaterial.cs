using UnityEngine;

namespace Cryptforge.UI
{
    // The one Sprites/Default material the views that draw plain lines and sprites share - the run's feedback, the
    // Warden's mark, the altars, the exit seal and the pit's band - where each used to make a copy of its own: the arena's
    // (ArenaView.Material, the built-in Sprites-Default that every new SpriteRenderer and every combat effect already draws
    // with), or, in a scene without an arena, one material made once for the session. A copy of Sprites/Default with its
    // defaults draws exactly as the built-in one does, and no view changes the material - colour, width and sprite live on
    // the renderers - so sharing it changes nothing on screen; it only spares a material per view and the switch between
    // materials wherever these draws meet the combat effects'. Being shared, it is never a view's to destroy.
    internal static class SharedSpriteMaterial
    {
        private const string ShaderName = "Sprites/Default";
        private static Material _session;

        internal static Material For(ArenaView arena)
        {
            Material own = arena != null ? arena.Material : null;
            if (own != null && own.shader != null && own.shader.name == ShaderName)
                return own;
            if (_session == null)
                _session = new Material(Shader.Find(ShaderName)) { name = "Shared Sprites/Default (session)" };
            return _session;
        }
    }
}
