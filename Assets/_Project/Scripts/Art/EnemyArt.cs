using System;
using System.Collections.Generic;

namespace Cryptforge.Art
{
    // Which placeholder look an enemy prefab wears; one per prefab in Prefabs/Enemies.
    public enum EnemyLook
    {
        Grunt,
        Runner,
        Tank,
        Captain,
        Warden,
        Mite
    }

    // Two idle/walk frames and the attack frame. IdleB bobs the figure one texel up (the mite's flame flickers
    // instead) and the attack frame changes the silhouette: fists thrown up, legs splayed, flame flared.
    public enum EnemyPose
    {
        IdleA,
        IdleB,
        Attack
    }

    // The enemies of the astral foundry as placeholder pixel art, seen from the arena camera's 3/4 top-down angle and
    // facing down toward the hero so their faces show: molten slag brutes with lava cracks and ember eyes, a stone-backed
    // hulk, a brass-crested captain, the quicksilver Forge Warden with a single violet visor, and the cinder mite with its
    // flame crown. Every figure is assembled from rounded masses that keep a one-texel seam where they overlap, so limbs
    // read as separate boulders, then outlined and set on a flat ground shadow. Pure C#: EnemyLookView turns the
    // canvases into sprites. Row 0 of a canvas is its bottom; the pivot is the bottom centre.
    public static class EnemyArt
    {
        // Canvas sizes: odd widths so each figure is symmetric about the bottom-centre pivot, one texel of headroom for
        // the idle bob and the outline, and a spare row or two on the taller looks.
        public const int GruntWidth = 31;
        public const int GruntHeight = 30;
        public const int RunnerWidth = 21;
        public const int RunnerHeight = 32;
        public const int TankWidth = 45;
        public const int TankHeight = 36;
        public const int CaptainWidth = 35;
        public const int CaptainHeight = 40;
        public const int WardenWidth = 49;
        public const int WardenHeight = 56;
        public const int MiteWidth = 15;
        public const int MiteHeight = 16;
        // The bar above an enemy's head: a dark backing with a one-texel outline and the red fill inside it.
        public const int HealthBarWidth = 26;
        public const int HealthBarHeight = 5;
        public const int HealthFillWidth = 24;
        public const int HealthFillHeight = 3;

        // Tones the palette lacks: a lit facet for slag, the captain's darker red slag, the fill's edge tones and the
        // flat translucent ground shadow.
        public static readonly Rgba SlagLight = Rgba.FromHex("#5E3442");
        public static readonly Rgba RedSlagLight = Rgba.FromHex("#B2321C");
        public static readonly Rgba RedSlag = PixelPalette.Lava;
        public static readonly Rgba RedSlagDark = Rgba.FromHex("#450A0A");
        public static readonly Rgba HealthLight = Rgba.FromHex("#F4746A");
        public static readonly Rgba HealthShade = Rgba.FromHex("#9E2A22");
        public static readonly Rgba GroundShadow = PixelPalette.Outline.WithAlpha(110);

        // A material: its lit, body and shadow tones and the seam drawn where two masses of it meet.
        private readonly struct Tones
        {
            public readonly Rgba Light;
            public readonly Rgba Mid;
            public readonly Rgba Dark;
            public readonly Rgba Seam;

            public Tones(Rgba light, Rgba mid, Rgba dark, Rgba seam)
            {
                Light = light;
                Mid = mid;
                Dark = dark;
                Seam = seam;
            }

            public bool Owns(Rgba color) => color == Light || color == Mid || color == Dark;
        }

        private static readonly Tones SlagTones = new Tones(SlagLight, PixelPalette.Slag, PixelPalette.SlagDark, PixelPalette.Outline);
        private static readonly Tones RedSlagTones = new Tones(RedSlagLight, RedSlag, RedSlagDark, PixelPalette.Outline);
        private static readonly Tones StoneTones = new Tones(PixelPalette.StoneLight, PixelPalette.Stone, PixelPalette.StoneDark, PixelPalette.Grout);
        private static readonly Tones SilverTones = new Tones(PixelPalette.SilverLight, PixelPalette.Silver, PixelPalette.SilverDark, PixelPalette.SteelShadow);
        private static readonly Tones BrassTones = new Tones(PixelPalette.BrassLight, PixelPalette.Brass, PixelPalette.BrassDark, PixelPalette.BrassDark);

        // The mite's flame crown, 7 texels wide: two flickers and the flare it makes when it bites.
        private static readonly string[] FlameA =
        {
            "..L....",
            "..LL...",
            ".LLL.L.",
            ".ELLLL.",
            ".ELHLE.",
            "EELHHLE",
            ".ELHHLE",
        };

        private static readonly string[] FlameB =
        {
            "....L..",
            "...LL..",
            ".L.LLL.",
            ".LLLLE.",
            ".ELHLE.",
            "ELHHLEE",
            "ELHHLE.",
        };

        private static readonly string[] FlameFlare =
        {
            ".L...L.",
            ".L.L.L.",
            ".LLLLL.",
            "ELLHLLE",
            "ELHHHLE",
            "ELHHHLE",
            ".LHHHL.",
        };

        // The captain's brass crest: a comb rising from the brow.
        private static readonly string[] Crest =
        {
            "...B...",
            "..BBb..",
            ".BBbbk.",
            "BBbbbkk",
        };

        public static int WidthOf(EnemyLook look)
        {
            switch (look)
            {
                case EnemyLook.Grunt: return GruntWidth;
                case EnemyLook.Runner: return RunnerWidth;
                case EnemyLook.Tank: return TankWidth;
                case EnemyLook.Captain: return CaptainWidth;
                case EnemyLook.Warden: return WardenWidth;
                case EnemyLook.Mite: return MiteWidth;
                default: throw new ArgumentOutOfRangeException(nameof(look));
            }
        }

        public static int HeightOf(EnemyLook look)
        {
            switch (look)
            {
                case EnemyLook.Grunt: return GruntHeight;
                case EnemyLook.Runner: return RunnerHeight;
                case EnemyLook.Tank: return TankHeight;
                case EnemyLook.Captain: return CaptainHeight;
                case EnemyLook.Warden: return WardenHeight;
                case EnemyLook.Mite: return MiteHeight;
                default: throw new ArgumentOutOfRangeException(nameof(look));
            }
        }

        // One frame of one look, outlined, feet on row 1 over the ground shadow.
        public static PixelCanvas Draw(EnemyLook look, EnemyPose pose)
        {
            switch (look)
            {
                case EnemyLook.Grunt: return DrawGrunt(pose);
                case EnemyLook.Runner: return DrawRunner(pose);
                case EnemyLook.Tank: return DrawTank(pose);
                case EnemyLook.Captain: return DrawCaptain(pose);
                case EnemyLook.Warden: return DrawWarden(pose);
                case EnemyLook.Mite: return DrawMite(pose);
                default: throw new ArgumentOutOfRangeException(nameof(look));
            }
        }

        // The bar's dark backing with its outline; the fill sits inside the one-texel frame.
        public static PixelCanvas DrawHealthBarBack()
        {
            var back = new PixelCanvas(HealthBarWidth, HealthBarHeight);
            back.FillRect(0, 0, HealthBarWidth, HealthBarHeight, PixelPalette.Outline);
            back.FillRect(1, 1, HealthFillWidth, HealthFillHeight, PixelPalette.BarBack);
            return back;
        }

        // The full red fill: a lit top row, the red body and a shaded bottom row. The view scales it by the health
        // fraction from its left edge.
        public static PixelCanvas DrawHealthBarFill()
        {
            var fill = new PixelCanvas(HealthFillWidth, HealthFillHeight);
            fill.FillRect(0, 2, HealthFillWidth, 1, HealthLight);
            fill.FillRect(0, 1, HealthFillWidth, 1, PixelPalette.HealthRed);
            fill.FillRect(0, 0, HealthFillWidth, 1, HealthShade);
            return fill;
        }

        // The grunt: a hunched slag brute, its head sunk between two huge arms whose fists rest on the floor. The attack
        // throws both fists up beside the head while the body crouches.
        private static PixelCanvas DrawGrunt(EnemyPose pose)
        {
            const int cx = 15;
            var figure = new PixelCanvas(GruntWidth, GruntHeight);
            int lift = Lift(pose);
            bool attack = pose == EnemyPose.Attack;
            int ty = lift - (attack ? 1 : 0);
            int armY = (attack ? 15 : 12) + lift;
            int fistY = (attack ? 21 : 4) + lift;
            Tones slag = SlagTones;
            CrackTones(pose, out Rgba crack, out Rgba core);

            Place(figure, Boulder(3, 4, slag, false), cx - 4, 5 + lift, slag.Seam);
            Place(figure, Boulder(3, 4, slag, false), cx + 4, 5 + lift, slag.Seam);
            Place(figure, Boulder(9, 8, slag, false), cx, 13 + ty, slag.Seam);
            Place(figure, Boulder(4, 7, slag, false), cx - 9, armY, slag.Seam);
            Place(figure, Boulder(4, 7, slag, false), cx + 9, armY, slag.Seam);
            Place(figure, Boulder(3, 3, slag, false), cx - 10, fistY, slag.Seam);
            Place(figure, Boulder(3, 3, slag, false), cx + 10, fistY, slag.Seam);
            Place(figure, Boulder(5, 4, slag, false), cx, 22 + ty, slag.Seam);
            Grain(figure, slag, 7);

            // Plate seams across the chest, then the lava: a branching crack down the chest, one up each arm and leg.
            InkLine(figure, 8, 15 + ty, 11, 17 + ty, slag.Dark);
            InkLine(figure, 19, 17 + ty, 22, 14 + ty, slag.Dark);
            InkLine(figure, 9, 9 + ty, 13, 7 + ty, slag.Dark);
            InkLine(figure, 17, 7 + ty, 21, 10 + ty, slag.Dark);
            Crack(figure, crack, core, cx, 18 + ty, cx - 1, 14 + ty, cx + 1, 10 + ty);
            Crack(figure, crack, core, cx - 1, 14 + ty, cx - 4, 12 + ty);
            Crack(figure, crack, core, cx - 10, armY + 5, cx - 8, armY, cx - 9, armY - 4);
            Crack(figure, crack, core, cx + 10, armY + 5, cx + 8, armY, cx + 9, armY - 4);
            Crack(figure, crack, core, cx - 5, 7 + lift, cx - 3, 4 + lift);
            Crack(figure, crack, core, cx + 5, 7 + lift, cx + 3, 4 + lift);
            Ink(figure, cx - 11, fistY + 1, core);
            Ink(figure, cx + 11, fistY + 1, core);
            if (attack)
                Crack(figure, core, core, cx + 1, 10 + ty, cx + 4, 8 + ty);
            Eyes(figure, cx, 22 + ty, 2);
            Grin(figure, cx, 19 + ty, 2);
            return Finish(figure, cx, 11);
        }

        // The runner: a thin slag sprinter hunched forward, head thrust below its shoulders, arms trailing behind. The
        // idle frames swap the striding legs; the lunge drops the body and punches both fists down past the hips.
        private static PixelCanvas DrawRunner(EnemyPose pose)
        {
            const int cx = 10;
            var figure = new PixelCanvas(RunnerWidth, RunnerHeight);
            int lift = Lift(pose);
            bool attack = pose == EnemyPose.Attack;
            int ty = lift - (attack ? 2 : 0);
            Tones slag = SlagTones;
            CrackTones(pose, out Rgba crack, out Rgba core);

            if (attack)
            {
                Limb(figure, cx - 2, 11, cx - 5, 3, slag);
                Limb(figure, cx + 2, 11, cx + 5, 3, slag);
                Place(figure, Boulder(2, 1, slag, false), cx - 5, 2, slag.Seam);
                Place(figure, Boulder(2, 1, slag, false), cx + 5, 2, slag.Seam);
            }
            else
            {
                // The forward leg reaches down toward the hero; the trailing leg is bent up behind.
                int forward = pose == EnemyPose.IdleA ? -1 : 1;
                Limb(figure, cx + 2 * forward, 12 + lift, cx + 3 * forward, 3 + lift, slag);
                Limb(figure, cx - 2 * forward, 12 + lift, cx - 3 * forward, 7 + lift, slag);
                Place(figure, Boulder(2, 1, slag, false), cx + 3 * forward, 2 + lift, slag.Seam);
                Place(figure, Boulder(2, 1, slag, false), cx - 3 * forward, 6 + lift, slag.Seam);
            }

            Place(figure, Boulder(4, 6, slag, false), cx, 17 + ty, slag.Seam);
            Place(figure, Boulder(6, 3, slag, false), cx, 22 + ty, slag.Seam);
            if (attack)
            {
                Limb(figure, cx - 5, 20 + ty, cx - 6, 13 + ty, slag);
                Limb(figure, cx + 5, 20 + ty, cx + 6, 13 + ty, slag);
                Place(figure, Boulder(1, 1, slag, false), cx - 6, 12 + ty, slag.Seam);
                Place(figure, Boulder(1, 1, slag, false), cx + 6, 12 + ty, slag.Seam);
            }
            else
            {
                Limb(figure, cx - 5, 22 + ty, cx - 7, 26 + ty, slag);
                Limb(figure, cx + 5, 22 + ty, cx + 7, 26 + ty, slag);
                Place(figure, Boulder(1, 1, slag, false), cx - 7, 27 + ty, slag.Seam);
                Place(figure, Boulder(1, 1, slag, false), cx + 7, 27 + ty, slag.Seam);
            }
            Place(figure, Boulder(3, 3, slag, false), cx, 20 + ty, slag.Seam);
            Grain(figure, slag, 3);

            Crack(figure, crack, core, cx + 1, 16 + ty, cx - 1, 13 + ty, cx, 11 + ty);
            Crack(figure, crack, core, cx - 4, 24 + ty, cx - 3, 22 + ty);
            Crack(figure, crack, core, cx + 4, 24 + ty, cx + 3, 22 + ty);
            SmallEyes(figure, cx, 20 + ty, 1);
            Ink(figure, cx - 1, 18 + ty, PixelPalette.EmberLight);
            Ink(figure, cx, 18 + ty, PixelPalette.EmberHot);
            Ink(figure, cx + 1, 18 + ty, PixelPalette.EmberLight);
            return Finish(figure, cx, 7);
        }

        // The tank: a wide squat hulk whose face sits low in its chest under a shell of stone plates on its back; the
        // arms are as thick as the body. The attack raises both fists to shoulder height.
        private static PixelCanvas DrawTank(EnemyPose pose)
        {
            const int cx = 22;
            var figure = new PixelCanvas(TankWidth, TankHeight);
            int lift = Lift(pose);
            bool attack = pose == EnemyPose.Attack;
            int ty = lift - (attack ? 1 : 0);
            int armY = (attack ? 15 : 12) + lift;
            int fistY = (attack ? 23 : 4) + lift;
            Tones slag = SlagTones;
            Tones stone = StoneTones;
            CrackTones(pose, out Rgba crack, out Rgba core);

            Place(figure, Boulder(4, 4, slag, false), cx - 6, 5 + lift, slag.Seam);
            Place(figure, Boulder(4, 4, slag, false), cx + 6, 5 + lift, slag.Seam);
            Place(figure, Boulder(13, 7, stone, false), cx, 22 + ty, stone.Seam);
            Place(figure, Boulder(14, 9, slag, false), cx, 14 + ty, slag.Seam);
            Place(figure, Boulder(5, 8, slag, false), cx - 15, armY, slag.Seam);
            Place(figure, Boulder(5, 8, slag, false), cx + 15, armY, slag.Seam);
            Place(figure, Boulder(4, 3, slag, false), cx - 16, fistY, slag.Seam);
            Place(figure, Boulder(4, 3, slag, false), cx + 16, fistY, slag.Seam);
            Place(figure, Boulder(6, 4, slag, false), cx, 20 + ty, slag.Seam);
            Grain(figure, slag, 5);

            // The shell splits into plates along grout seams; each plate catches light at its top left.
            foreach (int x in new[] { cx - 7, cx, cx + 7 })
                InkLineOn(figure, x, 24 + ty, x, 29 + ty, stone.Seam, stone);
            InkLineOn(figure, cx - 13, 26 + ty, cx + 13, 26 + ty, stone.Seam, stone);
            foreach (int x in new[] { cx - 11, cx - 5, cx + 2, cx + 8 })
            {
                InkOn(figure, x, 28 + ty, stone.Light, stone);
                InkOn(figure, x + 1, 28 + ty, stone.Light, stone);
                InkOn(figure, x, 25 + ty, stone.Light, stone);
            }

            InkLine(figure, cx - 12, 16 + ty, cx - 8, 18 + ty, slag.Dark);
            InkLine(figure, cx + 8, 18 + ty, cx + 12, 15 + ty, slag.Dark);
            InkLine(figure, cx - 10, 9 + ty, cx - 5, 7 + ty, slag.Dark);
            InkLine(figure, cx + 5, 7 + ty, cx + 10, 10 + ty, slag.Dark);
            Crack(figure, crack, core, cx, 15 + ty, cx - 1, 11 + ty, cx + 1, 7 + ty);
            Crack(figure, crack, core, cx - 1, 11 + ty, cx - 6, 9 + ty);
            Crack(figure, crack, core, cx + 1, 7 + ty, cx + 5, 9 + ty);
            Crack(figure, crack, core, cx - 16, armY + 5, cx - 14, armY, cx - 15, armY - 4);
            Crack(figure, crack, core, cx + 16, armY + 5, cx + 14, armY, cx + 15, armY - 4);
            Crack(figure, crack, core, cx - 7, 7 + lift, cx - 5, 3 + lift);
            Crack(figure, crack, core, cx + 7, 7 + lift, cx + 5, 3 + lift);
            Ink(figure, cx - 17, fistY + 1, core);
            Ink(figure, cx + 17, fistY + 1, core);
            if (attack)
                Crack(figure, core, core, cx + 1, 7 + ty, cx + 3, 4 + ty);
            Eyes(figure, cx, 20 + ty, 2);
            Grin(figure, cx, 17 + ty, 2);
            return Finish(figure, cx, 17);
        }

        // The captain: a bigger grunt in darker red slag, a brass pauldron capping one shoulder and a brass crest on its
        // brow. Same wind-up attack as the grunt.
        private static PixelCanvas DrawCaptain(EnemyPose pose)
        {
            const int cx = 17;
            var figure = new PixelCanvas(CaptainWidth, CaptainHeight);
            int lift = Lift(pose);
            bool attack = pose == EnemyPose.Attack;
            int ty = lift - (attack ? 1 : 0);
            int armY = (attack ? 18 : 15) + lift;
            int fistY = (attack ? 25 : 5) + lift;
            Tones slag = RedSlagTones;
            Tones brass = BrassTones;
            CrackTones(pose, out Rgba crack, out Rgba core);

            Place(figure, Boulder(3, 5, slag, false), cx - 5, 6 + lift, slag.Seam);
            Place(figure, Boulder(3, 5, slag, false), cx + 5, 6 + lift, slag.Seam);
            Place(figure, Boulder(10, 10, slag, false), cx, 16 + ty, slag.Seam);
            Place(figure, Boulder(5, 9, slag, false), cx - 10, armY, slag.Seam);
            Place(figure, Boulder(5, 9, slag, false), cx + 10, armY, slag.Seam);
            // The pauldron: a half dome over the top of the left arm with two dark rivets.
            Attach(figure, HalfDome(5, 3, brass), cx - 15, armY + 5, brass.Seam);
            Ink(figure, cx - 12, armY + 9, brass.Dark);
            Ink(figure, cx - 8, armY + 9, brass.Dark);
            Place(figure, Boulder(4, 3, slag, false), cx - 11, fistY, slag.Seam);
            Place(figure, Boulder(4, 3, slag, false), cx + 11, fistY, slag.Seam);
            Place(figure, Boulder(6, 5, slag, false), cx, 28 + ty, slag.Seam);
            figure.BlitMap(Crest, BrassInk(), cx - 3, 34 + ty);
            Grain(figure, slag, 11);

            InkLine(figure, cx - 8, 18 + ty, cx - 4, 20 + ty, slag.Dark);
            InkLine(figure, cx + 4, 20 + ty, cx + 8, 17 + ty, slag.Dark);
            InkLine(figure, cx - 7, 10 + ty, cx - 3, 8 + ty, slag.Dark);
            InkLine(figure, cx + 3, 8 + ty, cx + 7, 11 + ty, slag.Dark);
            Crack(figure, crack, core, cx, 23 + ty, cx - 1, 19 + ty, cx + 1, 14 + ty, cx, 10 + ty);
            Crack(figure, crack, core, cx - 1, 19 + ty, cx - 5, 17 + ty);
            Crack(figure, crack, core, cx + 1, 14 + ty, cx + 5, 13 + ty);
            Crack(figure, crack, core, cx - 11, armY + 4, cx - 9, armY - 1, cx - 10, armY - 5);
            Crack(figure, crack, core, cx + 11, armY + 6, cx + 9, armY, cx + 10, armY - 5);
            Crack(figure, crack, core, cx - 6, 8 + lift, cx - 4, 4 + lift);
            Crack(figure, crack, core, cx + 6, 8 + lift, cx + 4, 4 + lift);
            Ink(figure, cx - 12, fistY + 1, core);
            Ink(figure, cx + 12, fistY + 1, core);
            if (attack)
                Crack(figure, core, core, cx, 10 + ty, cx + 3, 7 + ty);
            Eyes(figure, cx, 28 + ty, 2);
            Grin(figure, cx, 25 + ty, 2);
            return Finish(figure, cx, 12);
        }

        // The Forge Warden: a quicksilver golem of polished spheres, its dome head carrying an angular faceplate with a
        // single violet visor slit and no eyes. Its fists rest on the floor; the attack hauls both up to head height.
        private static PixelCanvas DrawWarden(EnemyPose pose)
        {
            const int cx = 24;
            var figure = new PixelCanvas(WardenWidth, WardenHeight);
            int lift = Lift(pose);
            bool attack = pose == EnemyPose.Attack;
            int ty = lift - (attack ? 1 : 0);
            int armY = (attack ? 33 : 20) + lift;
            int fistY = (attack ? 44 : 8) + lift;
            Tones silver = SilverTones;

            Place(figure, Boulder(5, 2, silver, true), cx - 10, 3 + lift, silver.Seam);
            Place(figure, Boulder(5, 2, silver, true), cx + 10, 3 + lift, silver.Seam);
            Place(figure, Boulder(4, 6, silver, true), cx - 10, 10 + lift, silver.Seam);
            Place(figure, Boulder(4, 6, silver, true), cx + 10, 10 + lift, silver.Seam);
            Place(figure, Boulder(12, 11, silver, true), cx, 23 + ty, silver.Seam);
            // Hanging arms sit behind the shoulder spheres; raised arms swing up in front of them.
            if (!attack)
            {
                Place(figure, Boulder(5, 7, silver, true), cx - 17, armY, silver.Seam);
                Place(figure, Boulder(5, 7, silver, true), cx + 17, armY, silver.Seam);
            }
            Place(figure, Boulder(7, 6, silver, true), cx - 15, 31 + lift, silver.Seam);
            Place(figure, Boulder(7, 6, silver, true), cx + 15, 31 + lift, silver.Seam);
            if (attack)
            {
                Place(figure, Boulder(5, 7, silver, true), cx - 17, armY, silver.Seam);
                Place(figure, Boulder(5, 7, silver, true), cx + 17, armY, silver.Seam);
            }
            Place(figure, Boulder(5, 4, silver, true), cx - 17, fistY, silver.Seam);
            Place(figure, Boulder(5, 4, silver, true), cx + 17, fistY, silver.Seam);
            Place(figure, Boulder(8, 8, silver, true), cx, 42 + ty, silver.Seam);
            Attach(figure, Plate(13, 8, silver), cx - 6, 36 + ty, silver.Seam);

            // The visor: one lit row over its shadow, the only violet on the boss.
            figure.FillRect(cx - 5, 43 + ty, 11, 1, PixelPalette.Visor);
            figure.FillRect(cx - 4, 42 + ty, 9, 1, PixelPalette.NebulaGlow);

            // Armour seams: the chest line and belt, pectoral plates, a knee line, shoulder arcs and knuckles.
            InkLineOn(figure, cx, 30 + ty, cx, 17 + ty, silver.Seam, silver);
            SeamPath(figure, silver.Seam, silver, cx - 11, 18 + ty, cx - 7, 16 + ty, cx, 15 + ty, cx + 7, 16 + ty, cx + 11, 18 + ty);
            SeamPath(figure, silver.Seam, silver, cx - 1, 28 + ty, cx - 5, 26 + ty, cx - 9, 28 + ty);
            SeamPath(figure, silver.Seam, silver, cx + 1, 28 + ty, cx + 5, 26 + ty, cx + 9, 28 + ty);
            InkLineOn(figure, cx - 13, 10 + lift, cx - 7, 10 + lift, silver.Seam, silver);
            InkLineOn(figure, cx + 7, 10 + lift, cx + 13, 10 + lift, silver.Seam, silver);
            SeamPath(figure, silver.Seam, silver, cx - 19, 32 + lift, cx - 15, 35 + lift, cx - 11, 32 + lift);
            SeamPath(figure, silver.Seam, silver, cx + 11, 32 + lift, cx + 15, 35 + lift, cx + 19, 32 + lift);
            foreach (int dx in new[] { -19, -17, -15, 15, 17, 19 })
                InkOn(figure, cx + dx, fistY + 1, silver.Seam, silver);
            return Finish(figure, cx, 19);
        }

        // The cinder mite: a dark ember ball on four stubs with two bright eyes, a lava grin and a flame crown that
        // flickers between the idle frames and flares when it bites, the body squashing wider as it lunges.
        private static PixelCanvas DrawMite(EnemyPose pose)
        {
            const int cx = 7;
            var figure = new PixelCanvas(MiteWidth, MiteHeight);
            bool attack = pose == EnemyPose.Attack;
            Tones slag = SlagTones;

            figure.FillRect(cx - 2, 1, 2, 1, slag.Dark);
            figure.FillRect(cx + 1, 1, 2, 1, slag.Dark);
            int armSpread = attack ? 5 : 4;
            int armY = attack ? 3 : 4;
            figure.FillRect(cx - armSpread, armY, 1, 2, slag.Mid);
            figure.FillRect(cx + armSpread, armY, 1, 2, slag.Mid);
            if (attack)
                Place(figure, Boulder(4, 3, slag, false), cx, 4, slag.Seam);
            else
                Place(figure, Boulder(3, 3, slag, false), cx, 5, slag.Seam);

            int faceY = attack ? 4 : 5;
            SmallEyes(figure, cx, faceY, 1);
            Ink(figure, cx - 1, faceY - 2, PixelPalette.EmberLight);
            Ink(figure, cx, faceY - 2, PixelPalette.EmberHot);
            Ink(figure, cx + 1, faceY - 2, PixelPalette.EmberLight);

            string[] flame = attack ? FlameFlare : pose == EnemyPose.IdleB ? FlameB : FlameA;
            figure.BlitMap(flame, FlameInk(), cx - 3, attack ? 7 : 8);
            return Finish(figure, cx, 5);
        }

        private static int Lift(EnemyPose pose) => pose == EnemyPose.IdleB ? 1 : 0;

        // Lava pulses between the idle frames: bright orange with a hot core, then a dimmer red.
        private static void CrackTones(EnemyPose pose, out Rgba crack, out Rgba core)
        {
            if (pose == EnemyPose.IdleB)
            {
                crack = PixelPalette.Ember;
                core = PixelPalette.SlagCrack;
                return;
            }
            crack = PixelPalette.SlagCrack;
            core = PixelPalette.EmberHot;
        }

        // A rounded mass on its own canvas, lit from the upper left: a one-texel shadow rim along the bottom and right,
        // and either two lit facet texels (rock) or a broad highlight with a white glint (polished metal).
        private static PixelCanvas Boulder(int rx, int ry, Tones tones, bool polished)
        {
            var part = new PixelCanvas(rx * 2 + 1, ry * 2 + 1);
            float ax = rx + 0.5f;
            float ay = ry + 0.5f;
            for (int y = 0; y <= ry * 2; y++)
            {
                for (int x = 0; x <= rx * 2; x++)
                {
                    float nx = (x - rx) / ax;
                    float ny = (y - ry) / ay;
                    if (nx * nx + ny * ny > 1f)
                        continue;
                    // Outside the same ellipse shifted one texel up and left lies the shadow rim.
                    float sx = (x - rx + 1) / ax;
                    float sy = (y - ry - 1) / ay;
                    Rgba tone = sx * sx + sy * sy > 1f ? tones.Dark : tones.Mid;
                    if (polished)
                    {
                        float hx = (x - rx + rx * 0.3f) / (ax * 0.6f);
                        float hy = (y - ry - ry * 0.3f) / (ay * 0.6f);
                        if (hx * hx + hy * hy <= 1f)
                            tone = tones.Light;
                    }
                    part.Set(x, y, tone);
                }
            }

            int fx = rx - (rx + 1) / 2;
            int fy = ry + (ry + 1) / 2;
            if (polished)
            {
                part.Set(fx, fy, PixelPalette.StarWhite);
                if (rx >= 4)
                    part.Set(fx + 1, fy, PixelPalette.StarWhite);
            }
            else
            {
                part.Set(fx, fy, tones.Light);
                part.Set(fx + 1, fy, tones.Light);
                if (rx >= 5)
                    part.Set(fx + 1, fy + 1, tones.Light);
            }
            return part;
        }

        // The upper half of a boulder with a flat underside: a pauldron.
        private static PixelCanvas HalfDome(int rx, int ry, Tones tones)
        {
            PixelCanvas dome = Boulder(rx, ry, tones, false);
            dome.FillRect(0, 0, dome.Width, ry - 1, Rgba.Transparent);
            return dome;
        }

        // A flat armour plate with a shadowed bottom and right edge, a lit top-left corner and a chamfered jaw.
        private static PixelCanvas Plate(int width, int height, Tones tones)
        {
            var part = new PixelCanvas(width, height);
            part.FillRect(0, 0, width, height, tones.Mid);
            part.FillRect(0, 0, width, 1, tones.Dark);
            part.FillRect(width - 1, 0, 1, height, tones.Dark);
            part.FillRect(1, height - 2, 3, 1, tones.Light);
            part.Set(0, 0, Rgba.Transparent);
            part.Set(width - 1, 0, Rgba.Transparent);
            return part;
        }

        // A limb three texels thick along a line, shaded on its right edge.
        private static void Limb(PixelCanvas figure, int x0, int y0, int x1, int y1, Tones tones)
        {
            var part = new PixelCanvas(figure.Width, figure.Height);
            var strip = new PixelCanvas(figure.Width, figure.Height);
            strip.Line(x0, y0, x1, y1, tones.Mid);
            for (int y = 0; y < strip.Height; y++)
            {
                for (int x = 0; x < strip.Width; x++)
                {
                    if (strip.Get(x, y).A == 0)
                        continue;
                    part.Set(x - 1, y, tones.Mid);
                    part.Set(x, y, tones.Mid);
                    part.Set(x + 1, y, tones.Dark);
                }
            }
            Attach(figure, part, 0, 0, tones.Seam);
        }

        // Centres a part on (cx, cy) and attaches it.
        private static void Place(PixelCanvas figure, PixelCanvas part, int cx, int cy, Rgba seam) =>
            Attach(figure, part, cx - part.Width / 2, cy - part.Height / 2, seam);

        // Lays a part over the figure with a one-texel seam wherever its edge crosses what is already drawn, so masses
        // read as separate; the seam never reaches the figure's exterior, which gets the outline last.
        private static void Attach(PixelCanvas figure, PixelCanvas part, int x, int y, Rgba seam)
        {
            for (int py = -1; py <= part.Height; py++)
            {
                for (int px = -1; px <= part.Width; px++)
                {
                    if (part.Get(px, py).A > 0)
                        continue;
                    bool edge = part.Get(px - 1, py).A > 0 || part.Get(px + 1, py).A > 0 ||
                                part.Get(px, py - 1).A > 0 || part.Get(px, py + 1).A > 0;
                    if (edge && figure.Get(x + px, y + py).A > 0)
                        figure.Set(x + px, y + py, seam);
                }
            }
            figure.Blit(part, x, y);
        }

        // Deterministic rock grain: a few body texels turn lit or shadowed.
        private static void Grain(PixelCanvas figure, Tones tones, int seed)
        {
            for (int y = 0; y < figure.Height; y++)
            {
                for (int x = 0; x < figure.Width; x++)
                {
                    if (figure.Get(x, y) != tones.Mid)
                        continue;
                    if (PixelNoise.Chance(x, y, seed, 0.06f))
                        figure.Set(x, y, tones.Light);
                    else if (PixelNoise.Chance(x, y, seed + 1, 0.04f))
                        figure.Set(x, y, tones.Dark);
                }
            }
        }

        // Two angry ember eyes either side of cx: each a V whose outer corner sits a row higher than its hot inner corner.
        private static void Eyes(PixelCanvas figure, int cx, int y, int spread)
        {
            figure.Set(cx - spread - 1, y + 1, PixelPalette.EmberLight);
            figure.Set(cx - spread, y + 1, PixelPalette.EmberHot);
            figure.Set(cx - spread, y, PixelPalette.EmberHot);
            figure.Set(cx - spread + 1, y, PixelPalette.EmberLight);
            figure.Set(cx + spread + 1, y + 1, PixelPalette.EmberLight);
            figure.Set(cx + spread, y + 1, PixelPalette.EmberHot);
            figure.Set(cx + spread, y, PixelPalette.EmberHot);
            figure.Set(cx + spread - 1, y, PixelPalette.EmberLight);
        }

        // Two-texel slanted eyes for the small heads.
        private static void SmallEyes(PixelCanvas figure, int cx, int y, int spread)
        {
            figure.Set(cx - spread - 1, y + 1, PixelPalette.EmberLight);
            figure.Set(cx - spread, y, PixelPalette.EmberHot);
            figure.Set(cx + spread + 1, y + 1, PixelPalette.EmberLight);
            figure.Set(cx + spread, y, PixelPalette.EmberHot);
        }

        // A jagged lava grin: alternate texels a row lower, the hottest at the centre.
        private static void Grin(PixelCanvas figure, int cx, int y, int half)
        {
            for (int x = cx - half; x <= cx + half; x++)
            {
                int row = ((x - cx) & 1) == 0 ? y : y - 1;
                Ink(figure, x, row, x == cx ? PixelPalette.EmberHot : PixelPalette.EmberLight);
            }
        }

        // A lava crack along a polyline over visible texels only, with the hot core at every inner joint.
        private static void Crack(PixelCanvas figure, Rgba crack, Rgba core, params int[] points)
        {
            for (int i = 0; i + 3 < points.Length; i += 2)
                InkLine(figure, points[i], points[i + 1], points[i + 2], points[i + 3], crack);
            for (int i = 2; i + 3 < points.Length; i += 2)
                Ink(figure, points[i], points[i + 1], core);
        }

        // A seam along a polyline drawn only over one material's texels.
        private static void SeamPath(PixelCanvas figure, Rgba color, Tones on, params int[] points)
        {
            for (int i = 0; i + 3 < points.Length; i += 2)
                InkLineOn(figure, points[i], points[i + 1], points[i + 2], points[i + 3], color, on);
        }

        private static void Ink(PixelCanvas figure, int x, int y, Rgba color)
        {
            if (figure.Get(x, y).A > 0)
                figure.Set(x, y, color);
        }

        private static void InkOn(PixelCanvas figure, int x, int y, Rgba color, Tones on)
        {
            if (on.Owns(figure.Get(x, y)))
                figure.Set(x, y, color);
        }

        private static void InkLine(PixelCanvas figure, int x0, int y0, int x1, int y1, Rgba color)
        {
            var strip = new PixelCanvas(figure.Width, figure.Height);
            strip.Line(x0, y0, x1, y1, color);
            for (int y = 0; y < strip.Height; y++)
            {
                for (int x = 0; x < strip.Width; x++)
                {
                    if (strip.Get(x, y).A > 0)
                        Ink(figure, x, y, color);
                }
            }
        }

        private static void InkLineOn(PixelCanvas figure, int x0, int y0, int x1, int y1, Rgba color, Tones on)
        {
            var strip = new PixelCanvas(figure.Width, figure.Height);
            strip.Line(x0, y0, x1, y1, color);
            for (int y = 0; y < strip.Height; y++)
            {
                for (int x = 0; x < strip.Width; x++)
                {
                    if (strip.Get(x, y).A > 0)
                        InkOn(figure, x, y, color, on);
                }
            }
        }

        // Outlines the figure and sets it on the flat ground shadow, which stays unoutlined under the feet.
        private static PixelCanvas Finish(PixelCanvas figure, int cx, int shadowRadius)
        {
            figure.Outline(PixelPalette.Outline);
            var sprite = new PixelCanvas(figure.Width, figure.Height);
            sprite.FillEllipse(cx, 0, shadowRadius, 1, GroundShadow);
            sprite.Blit(figure, 0, 0);
            return sprite;
        }

        private static Dictionary<char, Rgba> FlameInk() => new Dictionary<char, Rgba>
        {
            ['E'] = PixelPalette.Ember,
            ['L'] = PixelPalette.EmberLight,
            ['H'] = PixelPalette.EmberHot,
        };

        private static Dictionary<char, Rgba> BrassInk() => new Dictionary<char, Rgba>
        {
            ['B'] = PixelPalette.BrassLight,
            ['b'] = PixelPalette.Brass,
            ['k'] = PixelPalette.BrassDark,
        };
    }
}
