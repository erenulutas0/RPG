using System;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.Art
{
    // The skills' painted effects (docs/45), drawn to match the painted actors: a burning enemy's flame at each Burn stack
    // count, Ember Memory's passed ember, the Crucible Vent's eruption, embers, cast ring and eruption ring, and the Forge
    // Burst's shockwave, sparks and flash; and docs/57's four new skills (Iron Resolve, Shield Rush, Flame Sigil, Cinder
    // Ward), which Tools/ArtReview/build_hero_skill_vfx.py paints onto sheets of their own and writes into this asset
    // beside the rest. Tools/ArtReview/build_skill_vfx.py paints the first sheets and writes this asset. Each
    // effect family is one texture, so a crowd of burning enemies or a Vent across a crowd batches (the optional eruption
    // ring rides one of its own, drawn on the floor apart from the column and embers); each strip is a run of frames on
    // its family's sheet, which the views cut into sprites at startup. Views borrow the sheets and own those sprites.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Skill Effect Set")]
    public sealed class SkillEffectArtSet : ScriptableObject
    {
        // A frame's corner and size on its sheet, in texels of the sheet as painted, row 0 at the bottom.
        [Serializable]
        public struct Cell
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }

        [Serializable]
        public sealed class Strip
        {
            public Texture2D Sheet;
            // The sheet's size as painted: an importer that shrinks it scales every cell with it, so the frames still cut
            // true and keep their size in the world.
            public int SheetWidth;
            public int SheetHeight;
            // Texels of the painted sheet to a world unit at scale 1.
            public float TexelsPerUnit;
            // Where every frame of the strip pins, in its frame's own normalised size; it may lie outside the frame, as a
            // flame pins at its enemy's feet below its lowest tongue.
            public Vector2 Pivot;
            public Cell[] Frames;

            public int Length => Frames != null ? Frames.Length : 0;

            public bool IsValid
            {
                get
                {
                    if (Sheet == null || SheetWidth < 1 || SheetHeight < 1 || !(TexelsPerUnit > 0f) || Length == 0)
                        return false;
                    foreach (Cell cell in Frames)
                    {
                        if (cell.Width < 1 || cell.Height < 1 || cell.X < 0 || cell.Y < 0 ||
                            cell.X + cell.Width > SheetWidth || cell.Y + cell.Height > SheetHeight)
                            return false;
                    }
                    return true;
                }
            }

            // Cuts the strip's frames from its sheet as sprites named the prefix and the frame's index. The caller owns
            // the sprites, never the sheet.
            public Sprite[] Cut(string prefix)
            {
                float scaleX = Sheet.width / (float)SheetWidth;
                float scaleY = Sheet.height / (float)SheetHeight;
                var sprites = new Sprite[Frames.Length];
                for (int i = 0; i < sprites.Length; i++)
                {
                    Cell cell = Frames[i];
                    var rect = new Rect(cell.X * scaleX, cell.Y * scaleY, cell.Width * scaleX, cell.Height * scaleY);
                    sprites[i] = Sprite.Create(Sheet, rect, Pivot, TexelsPerUnit * scaleX, 0, SpriteMeshType.FullRect);
                    sprites[i].name = prefix + i;
                }
                return sprites;
            }
        }

        // [stacks - 1]: one looping flicker for each Burn stack count, every one as many frames long.
        [SerializeField] private Strip[] _flames;
        [SerializeField] private Strip _emberArc;
        [SerializeField] private Strip _ventColumn;
        [SerializeField] private Strip _ventEmbers;
        [SerializeField] private Strip _ventRing;
        // Optional, on a sheet of its own: a set painted before it is still whole without it (IsValid never asks).
        [SerializeField] private Strip _ventSplash;
        [SerializeField] private Strip _shockwave;
        [SerializeField] private Strip _forgeSpark;
        [SerializeField] private Strip _forgeFlash;
        // docs/57's four new skills (Tools/ArtReview/build_hero_skill_vfx.py), each a family on a sheet of its own and all
        // optional, as the eruption ring is: a set painted before them is still whole, and a skill whose strips are
        // missing draws nothing of its own, as before they were painted.
        // Iron Resolve: the iron bell closing over the hero, holding as a sheen (a loop) and falling away, all pinned at the
        // hero's feet; and the unit ring of rivets it stamps on the floor.
        [SerializeField] private Strip _resolveClose;
        [SerializeField] private Strip _resolveHold;
        [SerializeField] private Strip _resolveEnd;
        [SerializeField] private Strip _resolveRivets;
        // Shield Rush: the wake behind the hero and the impact where the dash stops, both facing +x with their heads at
        // the pivot, which the view turns along the dash.
        [SerializeField] private Strip _rushStreak;
        [SerializeField] private Strip _rushImpact;
        // Flame Sigil: the glyph's ignition and its loop, a unit floor ring the view grows to the glyph's radius.
        [SerializeField] private Strip _sigilIgnite;
        [SerializeField] private Strip _sigilGlyph;
        // Cinder Ward: the shell rising round Ilya, whirling (a loop) and blowing apart, pinned at her feet; and the
        // flare at a striker whose blow it absorbed, pinned at its middle.
        [SerializeField] private Strip _wardRaise;
        [SerializeField] private Strip _wardShell;
        [SerializeField] private Strip _wardBreak;
        [SerializeField] private Strip _wardFlare;

        public Strip Flames(int stacks) => _flames[stacks - 1];
        public int FlameStackCount => _flames != null ? _flames.Length : 0;
        public Strip EmberArc => _emberArc;
        public Strip VentColumn => _ventColumn;
        public Strip VentEmbers => _ventEmbers;
        // A unit-radius ring: the view scales it to the radius it marks.
        public Strip VentRing => _ventRing;
        // The eruption ring each Vent eruption draws round its enemy: a unit-radius floor ring like the cast's, which the
        // view grows to BurnRule.VentSplashRadius. Without it the eruptions draw the procedural ember ring.
        public Strip VentSplash => _ventSplash;
        public bool HasVentSplash => Valid(_ventSplash);
        // A unit-radius floor ring, like the Vent's.
        public Strip Shockwave => _shockwave;
        public Strip ForgeSpark => _forgeSpark;
        public Strip ForgeFlash => _forgeFlash;
        public Strip ResolveClose => _resolveClose;
        public Strip ResolveHold => _resolveHold;
        public Strip ResolveEnd => _resolveEnd;
        // A unit-radius floor ring, as the Vent's.
        public Strip ResolveRivets => _resolveRivets;
        public Strip RushStreak => _rushStreak;
        public Strip RushImpact => _rushImpact;
        // Unit-radius floor rings: the view grows them to the glyph's radius.
        public Strip SigilIgnite => _sigilIgnite;
        public Strip SigilGlyph => _sigilGlyph;
        public Strip WardRaise => _wardRaise;
        public Strip WardShell => _wardShell;
        public Strip WardBreak => _wardBreak;
        public Strip WardFlare => _wardFlare;
        // Each family whole, or the skill draws nothing of its own (IsValid never asks).
        public bool HasIronResolve =>
            Valid(_resolveClose) && Valid(_resolveHold) && Valid(_resolveEnd) && Valid(_resolveRivets);
        public bool HasShieldRush => Valid(_rushStreak) && Valid(_rushImpact);
        public bool HasFlameSigil => Valid(_sigilIgnite) && Valid(_sigilGlyph);
        public bool HasCinderWard =>
            Valid(_wardRaise) && Valid(_wardShell) && Valid(_wardBreak) && Valid(_wardFlare);

        public bool IsValid
        {
            get
            {
                if (_flames == null || _flames.Length != BurnRule.MaxStacks)
                    return false;
                int frames = _flames[0] != null ? _flames[0].Length : 0;
                foreach (Strip flame in _flames)
                {
                    if (flame == null || !flame.IsValid || flame.Length != frames || flame.Sheet != _flames[0].Sheet)
                        return false;
                }
                return Valid(_emberArc) && Valid(_ventColumn) && Valid(_ventEmbers) && Valid(_ventRing) &&
                    Valid(_shockwave) && Valid(_forgeSpark) && Valid(_forgeFlash);
            }
        }

        private static bool Valid(Strip strip) => strip != null && strip.IsValid;
    }
}
