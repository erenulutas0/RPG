using System;
using UnityEngine;

namespace Cryptforge.Art
{
    // Bounded session ownership: one fixed backdrop and one platform geometry. Renderers/animation remain per view.
    // A different platform geometry gets view-owned art instead of reusing wrong dimensions or growing a dictionary.
    internal static class ArenaSpriteCache
    {
        private static PlatformSprites _platform;
        private static PlatformSprites _foundation;
        private static PlatformMaterialMeshes _paintedMeshes;
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

        internal static PlatformSprites Foundation(ArenaGeometry geometry, out bool shared)
        {
            if (_foundation == null) _foundation = new PlatformSprites(geometry, true);
            shared = _foundation.Matches(geometry);
            return shared ? _foundation : new PlatformSprites(geometry, true);
        }

        internal static PlatformMaterialMeshes PaintedMeshes(ArenaGeometry geometry, out bool shared)
        {
            if (_paintedMeshes == null) _paintedMeshes = new PlatformMaterialMeshes(geometry);
            shared = _paintedMeshes.Matches(geometry);
            return shared ? _paintedMeshes : new PlatformMaterialMeshes(geometry);
        }
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
            _foundation?.Dispose();
            _foundation = null;
            _paintedMeshes?.Dispose();
            _paintedMeshes = null;
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

        internal PlatformSprites(ArenaGeometry geometry, bool foundationOnly = false)
        {
            _geometry = geometry;
            var layout = new PlatformLayout(geometry);
            try
            {
                Surface = PixelSpriteFactory.CreateSprite(foundationOnly ? PlatformArt.DrawFoundation(layout) : PlatformArt.DrawPlatform(layout), "Forge Platform", PixelSpriteFactory.BottomCentre);
                for (int i = 0; i < _lights.Length; i++)
                    _lights[i] = PixelSpriteFactory.CreateSprite(foundationOnly ? PlatformArt.DrawFoundationLights(layout, i) : PlatformArt.DrawLights(layout, i), "Forge Platform Lights " + i,
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
