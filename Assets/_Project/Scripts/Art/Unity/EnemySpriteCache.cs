using System;
using UnityEngine;

// The PlayMode suite checks native ownership and subsystem cleanup without exposing reset to game callers.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Project.PlayModeTests")]

namespace Cryptforge.Art
{
    // Session-owned, immutable artwork, bounded by EnemyLook (six looks / 38 sprites including the shared bars).
    // Unlike per-enemy state this deliberately survives scene reloads. Only this owner destroys the textures.
    // The subsystem reset also runs with domain reload disabled, so a new Play session never inherits stale sprites.
    internal static class EnemySpriteCache
    {
        private static readonly EnemySprites[] Looks = new EnemySprites[Enum.GetValues(typeof(EnemyLook)).Length];
        internal static Sprite BarBack { get; private set; }
        internal static Sprite BarFill { get; private set; }

        internal static EnemySprites Get(EnemyLook look)
        {
            int index = (int)look;
            if (index < 0 || index >= Looks.Length)
                throw new ArgumentOutOfRangeException(nameof(look));
            if (Looks[index] != null)
                return Looks[index];

            EnsureBars();
            Looks[index] = new EnemySprites(look);
            return Looks[index];
        }

        // Imported enemies still borrow the bars, even if no procedural enemy has spawned yet.
        internal static void EnsureBars()
        {
            if (BarBack == null)
                BarBack = PixelSpriteFactory.CreateSprite(EnemyArt.DrawHealthBarBack(), "Health Bar Back", PixelSpriteFactory.Centre);
            if (BarFill == null)
                BarFill = PixelSpriteFactory.CreateSprite(EnemyArt.DrawHealthBarFill(), "Health Bar Fill", new Vector2(0f, 0.5f));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetSession()
        {
            Release();
            Application.quitting -= Release;
            Application.quitting += Release;
        }

        private static void Release()
        {
            for (int i = 0; i < Looks.Length; i++)
            {
                Looks[i]?.Dispose();
                Looks[i] = null;
            }
            PixelSpriteFactory.Destroy(BarBack);
            PixelSpriteFactory.Destroy(BarFill);
            BarBack = null;
            BarFill = null;
        }
    }

    // Arrays stay private: borrowers can assign frames to renderers but cannot replace the cached sequence.
    internal sealed class EnemySprites : IDisposable
    {
        private const int PoseCount = 3;
        private readonly Sprite[] _frames = new Sprite[PoseCount];
        private readonly Sprite[] _silhouettes = new Sprite[PoseCount];

        internal EnemySprites(EnemyLook look)
        {
            try
            {
                for (int i = 0; i < PoseCount; i++)
                {
                    var pose = (EnemyPose)i;
                    PixelCanvas canvas = EnemyArt.Draw(look, pose);
                    string name = look + " " + pose;
                    _frames[i] = PixelSpriteFactory.CreateSprite(canvas, name, PixelSpriteFactory.BottomCentre);
                    _silhouettes[i] = PixelSpriteFactory.CreateSprite(canvas.Silhouette(Rgba.White), name + " Flash",
                        PixelSpriteFactory.BottomCentre);
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal Sprite Frame(EnemyPose pose) => _frames[(int)pose];

        internal Sprite SilhouetteOf(Sprite frame)
        {
            if (frame == null)
                return null;
            for (int i = 0; i < PoseCount; i++)
                if (_frames[i] == frame)
                    return _silhouettes[i];
            return null;
        }

        public void Dispose()
        {
            for (int i = 0; i < PoseCount; i++)
            {
                PixelSpriteFactory.Destroy(_frames[i]);
                PixelSpriteFactory.Destroy(_silhouettes[i]);
                _frames[i] = null;
                _silhouettes[i] = null;
            }
        }
    }
}
