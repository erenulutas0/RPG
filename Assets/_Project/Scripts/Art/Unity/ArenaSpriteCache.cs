using System;
using UnityEngine;

namespace Cryptforge.Art
{
    // Bounded session ownership: one fixed backdrop and one platform geometry. Renderers/animation remain per view.
    // A different platform geometry gets view-owned art instead of reusing wrong dimensions or growing a dictionary.
    internal static class ArenaSpriteCache
    {
        private static PlatformSprites _platform;
        private static VoidSprites _void;
        private static Sprite _contactShadow;

        internal static PlatformSprites Platform(ArenaGeometry geometry, out bool shared)
        {
            if (_platform == null)
                _platform = new PlatformSprites(geometry);
            shared = _platform.Matches(geometry);
            return shared ? _platform : new PlatformSprites(geometry);
        }

        internal static VoidSprites Backdrop => _void ?? (_void = new VoidSprites());
        internal static Sprite ContactShadow => _contactShadow != null ? _contactShadow :
            (_contactShadow = PixelSpriteFactory.CreateSprite(ContactShadowArt.Draw(), "Contact Shadow", PixelSpriteFactory.Centre));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetSession()
        {
            Release();
            Application.quitting -= Release;
            Application.quitting += Release;
        }

        private static void Release()
        {
            _platform?.Dispose();
            _platform = null;
            _void?.Dispose();
            _void = null;
            PixelSpriteFactory.Destroy(_contactShadow);
            _contactShadow = null;
        }
    }

    internal sealed class PlatformSprites : IDisposable
    {
        private readonly ArenaGeometry _geometry;
        private readonly Sprite[] _lights = new Sprite[PlatformArt.FrameCount];
        internal Sprite Surface { get; private set; }

        internal PlatformSprites(ArenaGeometry geometry)
        {
            _geometry = geometry;
            var layout = new PlatformLayout(geometry);
            try
            {
                Surface = PixelSpriteFactory.CreateSprite(PlatformArt.DrawPlatform(layout), "Forge Platform", PixelSpriteFactory.BottomCentre);
                for (int i = 0; i < _lights.Length; i++)
                    _lights[i] = PixelSpriteFactory.CreateSprite(PlatformArt.DrawLights(layout, i), "Forge Platform Lights " + i,
                        PixelSpriteFactory.BottomCentre);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal Sprite Light(int frame) => _lights[frame];

        internal bool Matches(ArenaGeometry geometry) =>
            _geometry.NearCorner == geometry.NearCorner && _geometry.FarCorner == geometry.FarCorner &&
            _geometry.HalfWidth == geometry.HalfWidth;

        public void Dispose()
        {
            PixelSpriteFactory.Destroy(Surface);
            Surface = null;
            for (int i = 0; i < _lights.Length; i++)
            {
                PixelSpriteFactory.Destroy(_lights[i]);
                _lights[i] = null;
            }
        }
    }
}
