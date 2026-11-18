using System;
using System.Runtime.CompilerServices;
using Cryptforge.Art;

namespace Cryptforge.Combat
{
    // A hero's dash: Length floor units in TravelSeconds along a heading, walls sliding or stopping it as they do the walk
    // (what a wall takes is lost). docs/57's Shield Rush (4 units in 0.3 s) is the first; docs/46's Phase Dash (3 in 0.2 s,
    // its guard apart) is meant to be the same body when the Riftblade is built (lead decision 5, 2026-11-17), so the scene's
    // walk step and the simulation's move a dashing hero through the one arithmetic (StepOf, Move): the stamina lesson
    // (docs/21) - two copies of a distance product part in the last bit. Plain state, stepped at the walk step
    // (HeroMovementInput's order in the scene, the simulation's frame start); nothing here draws.
    public sealed class HeroDash
    {
        private const float Slack = 1e-4f;

        public float Length { get; }
        public float TravelSeconds { get; }
        public float TravelLeft { get; private set; }
        public float HeadingX { get; private set; }
        public float HeadingY { get; private set; }
        public int Dashes { get; private set; }
        public bool IsDashing => TravelLeft > Slack;

        public HeroDash(float length, float travelSeconds)
        {
            if (!(length > 0f) || float.IsInfinity(length))
                throw new ArgumentOutOfRangeException(nameof(length));
            if (!(travelSeconds > 0f) || float.IsInfinity(travelSeconds))
                throw new ArgumentOutOfRangeException(nameof(travelSeconds));
            Length = length;
            TravelSeconds = travelSeconds;
        }

        // The press, along a heading (normalised here; straight down the floor for none).
        public void Begin(float headingX, float headingY) => Begin(headingX, headingY, Length);

        // A press that travels only units of the dash's length (a Rush stopped on impact, SkillRule.ImpactTravel), at the
        // dash's own speed.
        public void Begin(float headingX, float headingY, float units)
        {
            if (float.IsNaN(units) || units < 0f)
                throw new ArgumentOutOfRangeException(nameof(units));
            float length = (float)Math.Sqrt(headingX * headingX + headingY * headingY);
            if (float.IsNaN(length) || float.IsInfinity(length))
                throw new ArgumentOutOfRangeException(nameof(headingX));
            if (length > 1e-5f)
            {
                HeadingX = headingX / length;
                HeadingY = headingY / length;
            }
            else
            {
                HeadingX = 0f;
                HeadingY = -1f;
            }
            TravelLeft = units >= Length ? TravelSeconds : TravelFor(units, Length, TravelSeconds);
            Dashes++;
        }

        // The seconds units take at length over travelSeconds. One body, never inlined.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float TravelFor(float units, float length, float travelSeconds) => travelSeconds * (units / length);

        // The dash's units on a step of deltaTime with travelLeft seconds of its travel to go: its length over its travel a
        // second, for the part of the step it still travels. One body, never inlined.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float StepOf(float length, float travelSeconds, float deltaTime, float travelLeft) =>
            length / travelSeconds * Math.Min(deltaTime, travelLeft);

        // One walk step of deltaTime: the units the dash moves the hero on it (0 once its travel is spent).
        public float Step(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            float units = IsDashing ? StepOf(Length, TravelSeconds, deltaTime, TravelLeft) : 0f;
            TravelLeft = Math.Max(0f, TravelLeft - deltaTime);
            return units;
        }

        // The walk step of a dashing hero: the step's units along the heading through HeroMotion (the walls as the walk
        // meets them), the stick ignored. False once the travel is spent - the walk is the stick's again - or when the dash
        // moved nothing this step. One body, never inlined, for the scene's HeroMovementInput and the simulation.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Move(HeroMotion motion, ArenaGeometry geometry, float deltaTime)
        {
            if (motion == null)
                throw new ArgumentNullException(nameof(motion));
            float units = Step(deltaTime);
            return units > 0f && motion.Move(HeadingX, HeadingY, units / motion.Speed, geometry);
        }

        // A dash stopped where it is (the hero fell, the run ended).
        public void Stop() => TravelLeft = 0f;
    }
}
