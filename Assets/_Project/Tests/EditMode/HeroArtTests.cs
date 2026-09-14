using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Vanguard's generated pixel art: canvas budget, outline, palette, the frames and the weapon parts that hang on it.
    public sealed class HeroArtTests
    {
        private static readonly HeroPose[] Poses = { HeroPose.IdleA, HeroPose.IdleB, HeroPose.Attack };

        private static readonly HeroWeaponPart[] Parts =
        {
            HeroWeaponPart.Sword, HeroWeaponPart.Shield, HeroWeaponPart.StaffShaft, HeroWeaponPart.StaffOrb,
            HeroWeaponPart.DaggerLeft, HeroWeaponPart.DaggerRight
        };

        [Test]
        public void TheBodyFitsItsBudgetWithFeetOnTheBottomRowAndAirAboveTheHelmet()
        {
            foreach (HeroPose pose in Poses)
            {
                PixelCanvas body = HeroArt.DrawBody(pose);
                Assert.That(body.Width, Is.LessThanOrEqualTo(32), pose.ToString());
                Assert.That(body.Height, Is.LessThanOrEqualTo(44), pose.ToString());
                Assert.That(body.Get(16, 0).A, Is.GreaterThan(0).And.LessThan(255), $"{pose}: a translucent ground shadow sits on the pivot row.");
                Assert.That(body.Get(12, 3), Is.EqualTo(PixelPalette.Silver), $"{pose}: silver boots stand just above the shadow.");
                Assert.That(body.Get(23, 3), Is.EqualTo(PixelPalette.BrassLight), $"{pose}: the right boot's brass toe cap.");
                for (int x = 0; x < body.Width; x++)
                    Assert.That(body.Get(x, body.Height - 1), Is.EqualTo(Rgba.Transparent), $"{pose}: top row, column {x}");
                Assert.That(body.Get(0, 0), Is.EqualTo(Rgba.Transparent), pose.ToString());
                Assert.That(body.Get(body.Width - 1, 0), Is.EqualTo(Rgba.Transparent), pose.ToString());
                Assert.That(HighestVisibleRow(body), Is.InRange(40, 42), $"{pose}: the helmet reaches about 1.3 units.");
                Assert.That(body.CountVisible(), Is.GreaterThan(500), pose.ToString());
            }
        }

        [Test]
        public void TheFigureIsOutlinedAndDrawnInBlueSteelBrassAndCapeTones()
        {
            PixelCanvas body = HeroArt.DrawBody(HeroPose.IdleA);
            Assert.That(Count(body, PixelPalette.Outline), Is.GreaterThan(80), "A one-texel outline wraps the whole figure.");
            for (int y = 0; y < body.Height; y++)
            {
                for (int x = 0; x < body.Width; x++)
                {
                    Rgba texel = body.Get(x, y);
                    if (!texel.IsOpaque || texel == PixelPalette.Outline)
                        continue;
                    // Every opaque texel of the plate, cape and boots is enclosed: its neighbours are never transparent.
                    Assert.That(body.Get(x - 1, y).A, Is.GreaterThan(0), $"({x}, {y}) left");
                    Assert.That(body.Get(x + 1, y).A, Is.GreaterThan(0), $"({x}, {y}) right");
                    Assert.That(body.Get(x, y - 1).A, Is.GreaterThan(0), $"({x}, {y}) below");
                    Assert.That(body.Get(x, y + 1).A, Is.GreaterThan(0), $"({x}, {y}) above");
                }
            }

            Assert.That(Count(body, PixelPalette.SteelDark), Is.GreaterThan(60), "The plate is mostly blue steel.");
            Assert.That(Count(body, PixelPalette.Steel), Is.GreaterThan(15));
            Assert.That(Count(body, PixelPalette.SteelLight), Is.GreaterThan(5), "Highlights on the lit side.");
            Assert.That(Count(body, PixelPalette.SteelShadow), Is.GreaterThan(15));
            Assert.That(Count(body, PixelPalette.BrassLight), Is.GreaterThan(8), "Brass trim on the helmet, pauldrons and belt.");
            Assert.That(Count(body, PixelPalette.SilverLight) + Count(body, PixelPalette.Silver), Is.GreaterThan(10), "Silver boots.");
            int cape = Count(body, HeroArt.CapeLight) + Count(body, HeroArt.Cape) + Count(body, HeroArt.CapeDark) + Count(body, HeroArt.CapeShadow);
            Assert.That(cape, Is.GreaterThan(150), "The cape covers most of the back.");
            Assert.That(Count(body, HeroArt.CapeLight), Is.GreaterThan(20), "Its outer edge is lit.");
            Assert.That(Count(body, PixelPalette.Visor), Is.Zero, "The knight's visor is a dark slit; only the boss glows.");
            Assert.That(body.Get(16, 38), Is.EqualTo(PixelPalette.BrassLight), "A brass crest runs down the back of the helmet.");
            Assert.That(body.Get(20, 33), Is.EqualTo(PixelPalette.Outline), "The visor slit's end shows on the turned right side.");
        }

        [Test]
        public void IdleFramesBreatheByOneTexelAndTheAttackFrameRaisesTheWeaponArm()
        {
            PixelCanvas idleA = HeroArt.DrawBody(HeroPose.IdleA);
            PixelCanvas idleB = HeroArt.DrawBody(HeroPose.IdleB);
            PixelCanvas attack = HeroArt.DrawBody(HeroPose.Attack);

            Assert.That(HighestVisibleRow(idleB), Is.EqualTo(HighestVisibleRow(idleA) + 1), "The breath lifts the upper body one texel.");
            int breath = Differences(idleA, idleB);
            Assert.That(breath, Is.GreaterThan(0).And.LessThan(idleA.CountVisible() / 2), "A breath is a small change.");
            for (int y = 0; y <= 4; y++)
            {
                for (int x = 0; x < idleA.Width; x++)
                    Assert.That(idleB.Get(x, y), Is.EqualTo(idleA.Get(x, y)), $"The feet stay planted at ({x}, {y}).");
            }

            Assert.That(Differences(idleA, attack), Is.GreaterThan(40), "The attack frame swings the arm.");
            HeroArt.RightHand(HeroPose.IdleA, out int restX, out int restY);
            HeroArt.RightHand(HeroPose.Attack, out int swungX, out int swungY);
            Assert.That(swungY, Is.GreaterThan(restY + 10), "The gauntlet rises above the shoulder.");
            Assert.That(swungX, Is.GreaterThanOrEqualTo(restX));
            Assert.That(idleA.Get(swungX, swungY), Is.EqualTo(Rgba.Transparent), "At rest nothing is drawn where the raised gauntlet goes.");
            Assert.That(attack.Get(swungX, swungY), Is.EqualTo(PixelPalette.SteelDark), "The raised gauntlet is steel.");
            Assert.That(attack.Get(restX, restY), Is.Not.EqualTo(idleA.Get(restX, restY)), "The gauntlet leaves its resting place.");
        }

        [Test]
        public void EachWeaponPartHasItsSizeTonesAndOutline()
        {
            foreach (HeroWeaponPart part in Parts)
            {
                PixelCanvas canvas = HeroArt.DrawWeapon(part);
                Assert.That(Count(canvas, PixelPalette.Outline), Is.GreaterThan(6), $"{part} is outlined.");
                Assert.That(canvas.Get(0, 0), Is.EqualTo(Rgba.Transparent), $"{part}: bottom-left corner");
                Assert.That(canvas.Get(canvas.Width - 1, canvas.Height - 1), Is.EqualTo(Rgba.Transparent), $"{part}: top-right corner");
            }

            PixelCanvas sword = HeroArt.DrawWeapon(HeroWeaponPart.Sword);
            Assert.That((sword.Width, sword.Height), Is.EqualTo((HeroArt.SwordSize, HeroArt.SwordSize)));
            Assert.That(Count(sword, PixelPalette.SilverLight), Is.GreaterThan(10), "A bright blade.");
            Assert.That(Count(sword, PixelPalette.SteelLight), Is.GreaterThan(8), "Blue-steel edge highlight.");
            Assert.That(Count(sword, PixelPalette.BrassLight), Is.GreaterThan(2), "Brass crossguard and pommel.");

            PixelCanvas shield = HeroArt.DrawWeapon(HeroWeaponPart.Shield);
            Assert.That((shield.Width, shield.Height), Is.EqualTo((HeroArt.ShieldSize, HeroArt.ShieldSize)));
            Assert.That(Count(shield, PixelPalette.Brass), Is.GreaterThan(10), "Brass rim.");
            Assert.That(Count(shield, PixelPalette.SteelDark), Is.GreaterThan(10), "Blue-steel face.");
            Assert.That(shield.Get(6, 6), Is.EqualTo(PixelPalette.BrassLight), "Brass boss at the centre.");

            PixelCanvas staff = HeroArt.DrawWeapon(HeroWeaponPart.StaffShaft);
            Assert.That((staff.Width, staff.Height), Is.EqualTo((HeroArt.StaffWidth, HeroArt.StaffHeight)));
            Assert.That(Count(staff, PixelPalette.Face), Is.GreaterThan(20), "A wooden shaft.");
            Assert.That(Count(staff, PixelPalette.Brass), Is.GreaterThanOrEqualTo(5), "Ferrule, rings and cap.");
            Assert.That(staff.Get(3, HeroArt.StaffCapRow), Is.EqualTo(PixelPalette.Brass));

            PixelCanvas orb = HeroArt.DrawWeapon(HeroWeaponPart.StaffOrb);
            PixelCanvas lit = HeroArt.DrawStaffOrb(true);
            Assert.That((orb.Width, orb.Height), Is.EqualTo((HeroArt.OrbSize, HeroArt.OrbSize)));
            Assert.That(Count(orb, PixelPalette.Visor), Is.GreaterThan(3), "The orb is violet.");
            Assert.That(Count(orb, PixelPalette.NebulaGlow), Is.GreaterThan(3));
            Assert.That(Count(orb, PixelPalette.Brass), Is.GreaterThan(2), "A brass cradle.");
            Assert.That(Count(lit, PixelPalette.StarWhite), Is.GreaterThan(0), "The lit orb has a white glint.");
            Assert.That(Differences(orb, lit), Is.GreaterThan(10));

            PixelCanvas right = HeroArt.DrawWeapon(HeroWeaponPart.DaggerRight);
            PixelCanvas left = HeroArt.DrawWeapon(HeroWeaponPart.DaggerLeft);
            Assert.That((right.Width, right.Height), Is.EqualTo((HeroArt.DaggerSize, HeroArt.DaggerSize)));
            Assert.That(Count(right, PixelPalette.SilverLight), Is.GreaterThan(4));
            Assert.That(left.Pixels, Is.EqualTo(right.FlipHorizontal().Pixels), "The left dagger mirrors the right one.");
        }

        [Test]
        public void PivotsSitOnTheGripAndAnchorsLandOnTheHandsOfEveryPose()
        {
            foreach (HeroWeaponPart part in Parts)
            {
                PixelCanvas canvas = HeroArt.DrawWeapon(part);
                HeroArt.Pivot(part, out int pivotX, out int pivotY);
                Assert.That(canvas.Contains(pivotX, pivotY), Is.True, part.ToString());
                Rgba grip = canvas.Get(pivotX, pivotY);
                Assert.That(grip.IsOpaque, Is.True, $"{part}: the pivot is on the drawn grip.");
                Assert.That(grip, Is.Not.EqualTo(PixelPalette.Outline), $"{part}: the pivot is inside the outline.");
            }

            foreach (HeroPose pose in Poses)
            {
                PixelCanvas body = HeroArt.DrawBody(pose);
                HeroArt.RightHand(pose, out int handX, out int handY);
                Assert.That(body.Get(handX, handY), Is.EqualTo(PixelPalette.SteelDark), $"{pose}: the weapon gauntlet is steel.");
                foreach (HeroWeaponPart part in new[] { HeroWeaponPart.Sword, HeroWeaponPart.DaggerRight, HeroWeaponPart.StaffShaft })
                {
                    HeroArt.Anchor(part, pose, out int x, out int y);
                    Assert.That((x, y), Is.EqualTo((handX, handY)), $"{pose}: {part} sits in the right hand.");
                }
                HeroArt.Anchor(HeroWeaponPart.StaffOrb, pose, out int orbX, out int orbY);
                Assert.That(orbX, Is.EqualTo(handX));
                Assert.That(orbY, Is.EqualTo(handY + HeroArt.StaffCapRow - HeroArt.StaffGripRow + 4), "The orb rides the cap.");

                HeroArt.LeftHand(pose, out int leftX, out int leftY);
                Assert.That(IsCape(body.Get(leftX, leftY)), Is.True, $"{pose}: the left hand hides behind the cape.");
                HeroArt.Anchor(HeroWeaponPart.Shield, pose, out int shieldX, out int shieldY);
                Assert.That(IsCape(body.Get(shieldX, shieldY)), Is.True, $"{pose}: the shield hangs at the cape's edge.");
                Assert.That(HeroArt.SortingOffset(HeroWeaponPart.Shield), Is.LessThan(0), "The shield draws behind the body.");
                Assert.That(HeroArt.SortingOffset(HeroWeaponPart.Sword), Is.GreaterThan(0), "The sword draws over the arm.");
                Assert.That(HeroArt.SortingOffset(HeroWeaponPart.StaffOrb), Is.GreaterThan(HeroArt.SortingOffset(HeroWeaponPart.StaffShaft)));
            }
        }

        [Test]
        public void DrawingIsDeterministic()
        {
            Assert.That(HeroArt.DrawBody(HeroPose.Attack).Pixels, Is.EqualTo(HeroArt.DrawBody(HeroPose.Attack).Pixels));
            Assert.That(HeroArt.DrawBody(HeroPose.IdleB).Pixels, Is.EqualTo(HeroArt.DrawBody(HeroPose.IdleB).Pixels));
            Assert.That(HeroArt.DrawWeapon(HeroWeaponPart.Shield).Pixels, Is.EqualTo(HeroArt.DrawWeapon(HeroWeaponPart.Shield).Pixels));
            Assert.That(HeroArt.DrawStaffOrb(true).Pixels, Is.EqualTo(HeroArt.DrawStaffOrb(true).Pixels));
        }

        private static bool IsCape(Rgba texel) =>
            texel == HeroArt.CapeLight || texel == HeroArt.Cape || texel == HeroArt.CapeDark || texel == HeroArt.CapeShadow;

        private static int Count(PixelCanvas canvas, Rgba color)
        {
            int count = 0;
            foreach (Rgba texel in canvas.Pixels)
            {
                if (texel == color)
                    count++;
            }
            return count;
        }

        private static int Differences(PixelCanvas a, PixelCanvas b)
        {
            int count = 0;
            for (int y = 0; y < a.Height; y++)
            {
                for (int x = 0; x < a.Width; x++)
                {
                    if (a.Get(x, y) != b.Get(x, y))
                        count++;
                }
            }
            return count;
        }

        private static int HighestVisibleRow(PixelCanvas canvas)
        {
            for (int y = canvas.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    if (canvas.Get(x, y).A > 0)
                        return y;
                }
            }
            return -1;
        }
    }
}
