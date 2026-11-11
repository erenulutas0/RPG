using System;
using UnityEngine;

namespace Cryptforge.UI
{
    // What skips a presentation the run waits on (BossRevealView: the boss walking out of the gate): a tap or a click that
    // begins this frame, or Space. Not a finger that was already on the screen - a player walking the hero as the boss
    // walks out - nor one held on through it. Any touch on the screen used to count, so on the phone a player who was
    // moving skipped the reveal on its first frame (the run flow's review, 2026-11-11). Skips is pure, so the rule is
    // tested without a device; ThisFrame reads the legacy Input.
    public static class RevealSkip
    {
        private static readonly Func<int, TouchPhase> InputTouchPhase = index => Input.GetTouch(index).phase;

        // This frame's input, as the legacy Input reads it.
        public static bool ThisFrame() =>
            Skips(Input.touchCount, InputTouchPhase, Input.GetMouseButtonDown(0), Input.GetKeyDown(KeyCode.Space));

        // touchCount touches on the screen, phaseOf each one's phase this frame; mousePressed and spacePressed whether the
        // button or the key went down this frame.
        public static bool Skips(int touchCount, Func<int, TouchPhase> phaseOf, bool mousePressed, bool spacePressed)
        {
            if (phaseOf == null)
                throw new ArgumentNullException(nameof(phaseOf));
            if (mousePressed || spacePressed)
                return true;
            for (int i = 0; i < touchCount; i++)
            {
                if (phaseOf(i) == TouchPhase.Began)
                    return true;
            }
            return false;
        }
    }
}
