using System;
using System.Collections.Generic;

namespace Cryptforge.Art
{
    // The hero's pose: two idle frames that differ by a one-texel breath of the upper body, and the attack frame with the
    // weapon arm raised beside the helmet.
    public enum HeroPose
    {
        IdleA,
        IdleB,
        Attack
    }

    // The weapon sprites the loadout objects under the hero body show. Each is drawn and anchored on its own so
    // HeroWeaponView keeps swapping whole loadouts while the body animates underneath.
    public enum HeroWeaponPart
    {
        Sword,
        Shield,
        StaffShaft,
        StaffOrb,
        DaggerLeft,
        DaggerRight
    }

    // The Vanguard knight as placeholder pixel art: a blue-steel armoured back seen from the arena camera's 3/4 top-down
    // angle, facing away toward the enemies, with a cape blown out to the left and the carried weapon drawn separately.
    // Pure C# so the drawing is testable without Unity; HeroLookView turns the canvases into sprites.
    public static class HeroArt
    {
        // The body canvas: about 40 texels (1.25 units) tall with room above for the breathing lift; pivot at the feet,
        // the centre line between texel columns 15 and 16.
        public const int BodyWidth = 32;
        public const int BodyHeight = 44;
        public const int SwordSize = 24;
        public const int ShieldSize = 13;
        public const int StaffWidth = 7;
        public const int StaffHeight = 42;
        public const int OrbSize = 11;
        public const int DaggerSize = 12;
        // The staff row the hand closes on and the brass cap the orb's cradle sits on, in staff texels.
        public const int StaffGripRow = 13;
        public const int StaffCapRow = 40;

        // Cape tones: a brighter, more saturated blue than the plate armour so the two materials separate at phone size.
        public static readonly Rgba CapeLight = Rgba.FromHex("#5F93E8");
        public static readonly Rgba Cape = Rgba.FromHex("#3C69C9");
        public static readonly Rgba CapeDark = Rgba.FromHex("#24408F");
        public static readonly Rgba CapeShadow = Rgba.FromHex("#182B63");
        // The flat ground shadow under the feet: one translucent tone, no gradient.
        public static readonly Rgba GroundShadow = PixelPalette.Outline.WithAlpha(110);

        // Helmet: a rounded dome lit from the upper left with a brass crest down the back, the visor slit's end showing
        // at the right where the 3/4 turn reveals the side of the face, a brass rim and the neck guard below.
        private static readonly string[] Helmet =
        {
            "....lBss....",
            "..llsBssdd..",
            ".llssBssddd.",
            ".lsssBsddddS",
            "lssssBsddddS",
            "lsssdBdddddS",
            "lssddBdddSSS",
            "sdddddddSooS",
            "BBBBbbbbbbkk",
            ".SSSSSSSSSS.",
            "...SSSSSS...",
        };

        // Rounded pauldrons with brass rims at each side and the steel collar the cape hangs from between them.
        private static readonly string[] Shoulders =
        {
            "..BBBB...ssss...BBBB..",
            ".BlssdB.SsddsS.BsdddB.",
            "BlssddSSSddddSSSddddSS",
            ".sdddS..........SdddS.",
        };

        // The cuirass: mostly under the cape, its right flank and the brass belt show beside the cape's edge.
        private static readonly string[] Torso =
        {
            "sddddddddddS",
            "sddddddddddS",
            "sddddddddddS",
            "sddddddddddS",
            "sddddddddddS",
            "sddddddddddS",
            "BbbbbbbbbbkS",
            "sddddddddddS",
            "sddddddddddS",
            "sddddddddddS",
        };

        // Greaves with brass knee plates over silver boots; the right boot turns out to the right with the 3/4 stance
        // and shows a brass toe cap.
        private static readonly string[] Legs =
        {
            "..dddd..dddd...",
            "..sddd..sddd...",
            "..sddd..sdddS..",
            "..sddS..sdddS..",
            "..sddS..sdddS..",
            "..Bbbk..Bbbbk..",
            "..sddS..sdddS..",
            "..sddS..sdddS..",
            "..sddS..sdddS..",
            "..dddS..ddddS..",
            "..SSSS..SSSSS..",
            ".LLLLN..LLLLLN.",
            ".LNNNN..LNNNNNB",
            ".NNNNM..NNNNNNB",
            "..MMM....MMMMk.",
        };

        // The weapon arm at rest: down from the right pauldron, a brass couter at the elbow and a cuff at the wrist, the
        // gauntlet at the bottom right where the weapon grip lands.
        private static readonly string[] ArmRest =
        {
            ".sdd...",
            ".sddS..",
            ".sddS..",
            "..sddS.",
            "..sddS.",
            "..Bbbk.",
            "..sddS.",
            "...sddS",
            "...sddS",
            "...sddS",
            "...Bbbk",
            "..lssdd",
            "..lsddd",
            "..ssddd",
            "...ddS.",
        };

        // The weapon arm raised: the elbow out from under the pauldron, the forearm straight up beside the helmet and
        // the gauntlet above shoulder height, where a sword held in it points up and out.
        private static readonly string[] ArmRaised =
        {
            "...lssd.",
            "...lsddS",
            "...ssddS",
            "...Bbbk.",
            "...sdS..",
            "...sdS..",
            "...sdS..",
            "..Bbbk..",
            ".sddd...",
            ".sdd....",
        };

        // The cape's left and right texel per row from the collar (row 28) down to the hem (row 5): it hangs from the
        // upper back, billows out to the left and its hem rises toward the right, as a cape caught by a forward step.
        private const int CapeTopRow = 28;
        private const int CapeHemRow = 5;
        private static readonly int[] CapeLeft = { 12, 12, 12, 10, 9, 8, 7, 6, 5, 5, 4, 4, 3, 3, 2, 2, 2, 2, 2, 2, 2, 2, 3, 4 };
        private static readonly int[] CapeRight = { 19, 19, 19, 19, 19, 19, 19, 19, 19, 19, 19, 18, 18, 18, 17, 17, 16, 15, 14, 13, 12, 11, 10, 9 };

        // The idle body, the breathing frame and the attack frame, all with the feet on the bottom row.
        public static PixelCanvas DrawBody(HeroPose pose)
        {
            Dictionary<char, Rgba> ink = Ink();
            var figure = new PixelCanvas(BodyWidth, BodyHeight);
            int lift = pose == HeroPose.IdleB ? 1 : 0;
            // The head turns a texel toward the swing.
            int turn = pose == HeroPose.Attack ? 1 : 0;

            figure.BlitMap(Legs, ink, 9, 2);
            figure.BlitMap(Torso, ink, 11, 17 + lift);
            DrawCape(figure, lift);
            // The arm goes under the pauldron, which is drawn after it so the shoulder plate caps the arm.
            if (pose == HeroPose.Attack)
                figure.BlitMap(ArmRaised, ink, 22, 27);
            else
                figure.BlitMap(ArmRest, ink, 22, 12 + lift);
            figure.BlitMap(Shoulders, ink, 5, 26 + lift);
            figure.BlitMap(Helmet, ink, 11 + turn, 30 + lift);
            figure.Outline(PixelPalette.Outline);

            // The ground shadow goes under the outlined figure so it never gets an outline of its own.
            var body = new PixelCanvas(BodyWidth, BodyHeight);
            body.FillEllipse(16, 0, 10, 1, GroundShadow);
            body.Blit(figure, 0, 0);
            return body;
        }

        // One weapon sprite, outlined, at the size given by the constants above.
        public static PixelCanvas DrawWeapon(HeroWeaponPart part)
        {
            switch (part)
            {
                case HeroWeaponPart.Sword:
                    return DrawSword();
                case HeroWeaponPart.Shield:
                    return DrawShield();
                case HeroWeaponPart.StaffShaft:
                    return DrawStaffShaft();
                case HeroWeaponPart.StaffOrb:
                    return DrawStaffOrb(false);
                case HeroWeaponPart.DaggerLeft:
                    return DrawDagger().FlipHorizontal();
                case HeroWeaponPart.DaggerRight:
                    return DrawDagger();
                default:
                    throw new ArgumentOutOfRangeException(nameof(part));
            }
        }

        // The staff's orb in its brass cradle, dim at rest and lit while the staff casts.
        public static PixelCanvas DrawStaffOrb(bool lit)
        {
            var orb = new PixelCanvas(OrbSize, OrbSize);
            Rgba rim = lit ? PixelPalette.Visor : PixelPalette.NebulaGlow;
            Rgba core = lit ? PixelPalette.StarLilac : PixelPalette.Visor;
            Rgba glint = lit ? PixelPalette.StarWhite : PixelPalette.StarLilac;
            // The cradle: a brass cup with two prongs reaching up beside the orb.
            orb.FillRect(4, 1, 3, 1, PixelPalette.Brass);
            orb.Set(3, 2, PixelPalette.BrassLight);
            orb.FillRect(4, 2, 3, 1, PixelPalette.Brass);
            orb.Set(7, 2, PixelPalette.BrassDark);
            orb.FillRect(2, 3, 1, 2, PixelPalette.BrassLight);
            orb.FillRect(8, 3, 1, 2, PixelPalette.BrassDark);
            orb.FillEllipse(5, 6, 3, 3, rim);
            orb.FillEllipse(4, 7, 2, 2, core);
            orb.Set(5, 6, core);
            orb.Set(3, 7, glint);
            orb.Set(4, 8, glint);
            orb.Outline(PixelPalette.Outline);
            return orb;
        }

        // Where the part draws relative to the body renderer's sorting order: the left-hand items hide behind the cape,
        // the right-hand weapons sit over the arm and the orb over the staff's cap.
        public static int SortingOffset(HeroWeaponPart part)
        {
            switch (part)
            {
                case HeroWeaponPart.Shield:
                case HeroWeaponPart.DaggerLeft:
                    return -1;
                case HeroWeaponPart.StaffOrb:
                    return 2;
                case HeroWeaponPart.Sword:
                case HeroWeaponPart.StaffShaft:
                case HeroWeaponPart.DaggerRight:
                    return 1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(part));
            }
        }

        // The texel of the part's canvas that sits on the hand (the grip, the shield's boss, the orb's centre): the
        // sprite pivot, so that the weapon's texel grid lines up with the body's.
        public static void Pivot(HeroWeaponPart part, out int x, out int y)
        {
            switch (part)
            {
                case HeroWeaponPart.Sword:
                    x = 3;
                    y = 3;
                    break;
                case HeroWeaponPart.Shield:
                    x = 6;
                    y = 6;
                    break;
                case HeroWeaponPart.StaffShaft:
                    x = 3;
                    y = StaffGripRow;
                    break;
                case HeroWeaponPart.StaffOrb:
                    x = 5;
                    y = 6;
                    break;
                case HeroWeaponPart.DaggerLeft:
                    x = DaggerSize - 4;
                    y = 3;
                    break;
                case HeroWeaponPart.DaggerRight:
                    x = 3;
                    y = 3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(part));
            }
        }

        // The texel of the body canvas where the part's pivot lands in the pose: the weapon hand for the right-hand
        // parts, the hidden left arm behind the cape for the shield and the left dagger.
        public static void Anchor(HeroWeaponPart part, HeroPose pose, out int x, out int y)
        {
            switch (part)
            {
                case HeroWeaponPart.Sword:
                case HeroWeaponPart.DaggerRight:
                case HeroWeaponPart.StaffShaft:
                    RightHand(pose, out x, out y);
                    break;
                case HeroWeaponPart.StaffOrb:
                    RightHand(pose, out x, out y);
                    // The orb's centre sits three texels above the cap its cradle rests on.
                    y += StaffCapRow - StaffGripRow + 4;
                    break;
                case HeroWeaponPart.Shield:
                    LeftHand(pose, out x, out y);
                    y += 1;
                    break;
                case HeroWeaponPart.DaggerLeft:
                    LeftHand(pose, out x, out y);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(part));
            }
        }

        // The centre of the weapon gauntlet.
        public static void RightHand(HeroPose pose, out int x, out int y)
        {
            if (pose == HeroPose.Attack)
            {
                x = 27;
                y = 35;
                return;
            }
            x = 26;
            y = pose == HeroPose.IdleB ? 14 : 13;
        }

        // The left hand hangs hidden behind the cape's left edge; what it holds peeks out beside the cape.
        public static void LeftHand(HeroPose pose, out int x, out int y)
        {
            x = 3;
            y = pose == HeroPose.IdleB ? 16 : 15;
        }

        private static void DrawCape(PixelCanvas canvas, int lift)
        {
            for (int row = CapeHemRow; row <= CapeTopRow; row++)
            {
                int index = CapeTopRow - row;
                int left = CapeLeft[index];
                int right = CapeRight[index];
                // Two folds run down the cape, drifting left as it flows out.
                int foldA = 8 + (row - CapeHemRow) / 7;
                int foldB = 13 + (row - CapeHemRow) / 5;
                for (int x = left; x <= right; x++)
                {
                    // A notch in the hem so it scallops instead of ending in a flat cut.
                    if (row == CapeHemRow && x == 7)
                        continue;
                    bool bottomEdge = row == CapeHemRow || x < CapeLeft[index + 1] || x > CapeRight[index + 1];

                    Rgba tone;
                    if (bottomEdge)
                        tone = CapeDark;
                    else if (x >= right - 1)
                        tone = CapeShadow;
                    else if (x <= left + 1)
                        tone = CapeLight;
                    else if (x == foldA || x == foldB)
                        tone = CapeDark;
                    else if (x - left <= 3 && row >= 8 && row <= 18 && ((x + row) & 1) == 0)
                        tone = CapeLight;
                    else
                        tone = Cape;
                    canvas.Set(x, row + lift, tone);
                    // The hem stays on the ground while the shoulders lift, so a breath stretches the cape a texel
                    // instead of hovering the whole thing and shifting the outline under the feet.
                    if (row == CapeHemRow && lift > 0)
                        canvas.Set(x, row, tone);
                }
            }
        }

        // A broad blade drawn along the 45° diagonal so it stays on the pixel grid at rest: a bright upper edge, a silver
        // body and a shaded lower edge, tapering to the point; brass crossguard, leather grip, brass pommel.
        private static PixelCanvas DrawSword()
        {
            var sword = new PixelCanvas(SwordSize, SwordSize);
            sword.Set(1, 1, PixelPalette.BrassLight);
            sword.Set(1, 2, PixelPalette.Brass);
            sword.Set(2, 1, PixelPalette.Brass);
            sword.FillRect(2, 2, 2, 2, PixelPalette.Face);
            sword.Set(2, 3, PixelPalette.FaceLight);
            sword.Line(3, 7, 7, 3, PixelPalette.BrassLight);
            sword.Line(4, 7, 7, 4, PixelPalette.Brass);
            sword.Set(5, 5, PixelPalette.BrassLight);
            for (int i = 6; i <= 21; i++)
                sword.Set(i, i, PixelPalette.SilverLight);
            for (int i = 6; i <= 19; i++)
            {
                sword.Set(i, i + 1, PixelPalette.SteelLight);
                sword.Set(i + 1, i, PixelPalette.Silver);
            }
            sword.Outline(PixelPalette.Outline);
            return sword;
        }

        // A small round shield: brass rim, blue-steel face lit from the upper left, brass boss at the centre.
        private static PixelCanvas DrawShield()
        {
            var shield = new PixelCanvas(ShieldSize, ShieldSize);
            shield.FillEllipse(6, 6, 5, 5, PixelPalette.Brass);
            shield.FillEllipse(6, 6, 4, 4, PixelPalette.SteelDark);
            // A lit blob carved back to a crescent by the darker lower-right.
            shield.FillEllipse(5, 7, 2, 2, PixelPalette.Steel);
            shield.FillEllipse(7, 5, 3, 3, PixelPalette.SteelDark);
            shield.Set(3, 8, PixelPalette.SteelLight);
            shield.Set(4, 9, PixelPalette.SteelLight);
            shield.FillEllipse(6, 6, 1, 1, PixelPalette.Brass);
            shield.Set(6, 6, PixelPalette.BrassLight);
            shield.Set(6, 11, PixelPalette.BrassLight);
            shield.Set(1, 6, PixelPalette.BrassLight);
            shield.Set(2, 9, PixelPalette.BrassLight);
            shield.Set(11, 6, PixelPalette.BrassDark);
            shield.Set(6, 1, PixelPalette.BrassDark);
            shield.Set(10, 3, PixelPalette.BrassDark);
            shield.Outline(PixelPalette.Outline);
            return shield;
        }

        // A tall wooden staff, three texels wide with a lit edge, a brass ferrule, rings and cap, and a leather wrap
        // around the grip.
        private static PixelCanvas DrawStaffShaft()
        {
            var staff = new PixelCanvas(StaffWidth, StaffHeight);
            staff.FillRect(2, 1, 1, StaffCapRow, PixelPalette.FaceLight);
            staff.FillRect(3, 1, 1, StaffCapRow, PixelPalette.Face);
            staff.FillRect(4, 1, 1, StaffCapRow, PixelPalette.FaceDark);
            for (int row = StaffGripRow - 2; row <= StaffGripRow + 2; row++)
            {
                staff.Set(2, row, PixelPalette.Face);
                staff.Set(3, row, PixelPalette.FaceDark);
                staff.Set(4, row, PixelPalette.FaceDark);
            }
            foreach (int row in new[] { 1, 2, 9, 25, StaffCapRow - 1, StaffCapRow })
            {
                staff.Set(2, row, PixelPalette.BrassLight);
                staff.Set(3, row, PixelPalette.Brass);
                staff.Set(4, row, PixelPalette.BrassDark);
            }
            staff.Set(1, StaffCapRow, PixelPalette.Brass);
            staff.Set(5, StaffCapRow, PixelPalette.BrassDark);
            staff.Outline(PixelPalette.Outline);
            return staff;
        }

        // A short diagonal blade for the right hand; the left dagger is its mirror.
        private static PixelCanvas DrawDagger()
        {
            var dagger = new PixelCanvas(DaggerSize, DaggerSize);
            dagger.Set(1, 1, PixelPalette.BrassLight);
            dagger.FillRect(2, 2, 2, 2, PixelPalette.Face);
            dagger.Set(2, 3, PixelPalette.FaceLight);
            dagger.Set(3, 2, PixelPalette.FaceDark);
            dagger.Line(3, 5, 5, 3, PixelPalette.BrassLight);
            dagger.Set(4, 4, PixelPalette.Brass);
            for (int i = 5; i <= 10; i++)
                dagger.Set(i, i, PixelPalette.SilverLight);
            for (int i = 5; i <= 8; i++)
            {
                dagger.Set(i, i + 1, PixelPalette.SteelLight);
                dagger.Set(i + 1, i, PixelPalette.Silver);
            }
            dagger.Outline(PixelPalette.Outline);
            return dagger;
        }

        private static Dictionary<char, Rgba> Ink() => new Dictionary<char, Rgba>
        {
            ['o'] = PixelPalette.Outline,
            ['S'] = PixelPalette.SteelShadow,
            ['d'] = PixelPalette.SteelDark,
            ['s'] = PixelPalette.Steel,
            ['l'] = PixelPalette.SteelLight,
            ['B'] = PixelPalette.BrassLight,
            ['b'] = PixelPalette.Brass,
            ['k'] = PixelPalette.BrassDark,
            ['L'] = PixelPalette.SilverLight,
            ['N'] = PixelPalette.Silver,
            ['M'] = PixelPalette.SilverDark,
        };
    }
}
