using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cryptforge.UI
{
    // What skips a presentation the run waits on (BossRevealView: the boss walking out of the gate): a tap or a click that
    // begins this frame, or Space. Not a finger that was already on the screen - a player walking the hero as the boss
    // walks out - nor one held on through it. Any touch on the screen used to count, so on the phone a player who was
    // moving skipped the reveal on its first frame (the run flow's review, 2026-11-11). Nor a tap on the UI - the pause
    // button, the ability button - which is the button's, and nothing while the game stands paused: the reveal's Update
    // runs at a time scale of 0, so a tap on the paused screen latched the skip for the moment play resumed (the same
    // review). Skips is pure, so the rule is tested without a device; ThisFrame reads the legacy Input.
    public static class RevealSkip
    {
        private static readonly Func<int, TouchPhase> InputTouchPhase = index => Input.GetTouch(index).phase;
        private static readonly Func<int, bool> InputTouchOverUi = index => OverUi(Input.GetTouch(index).fingerId);

        // This frame's input, as the legacy Input reads it; paused whether the game stands paused. Read it once the
        // EventSystem has taken this frame's input (BossRevealView reads it in LateUpdate): before that, a touch that began
        // this frame is not yet known to be over a button.
        public static bool ThisFrame(bool paused)
        {
            if (paused)
                return false;
            bool mouse = Input.GetMouseButtonDown(0);
            int touches = Input.touchCount;
            return Skips(touches, InputTouchPhase, InputTouchOverUi, mouse, mouse && touches == 0 && OverUi(-1),
                Input.GetKeyDown(KeyCode.Space), false);
        }

        // touchCount touches on the screen, phaseOf each one's phase this frame; mousePressed and spacePressed whether the
        // button or the key went down this frame. Nothing is over the UI and the game is not paused.
        public static bool Skips(int touchCount, Func<int, TouchPhase> phaseOf, bool mousePressed, bool spacePressed) =>
            Skips(touchCount, phaseOf, null, mousePressed, false, spacePressed, false);

        // As above; touchOverUi(index) whether that touch is over the UI (null: none is), mouseOverUi whether the mouse
        // pointer is, paused whether the game stands paused (then nothing skips).
        public static bool Skips(int touchCount, Func<int, TouchPhase> phaseOf, Func<int, bool> touchOverUi, bool mousePressed,
            bool mouseOverUi, bool spacePressed, bool paused)
        {
            if (phaseOf == null)
                throw new ArgumentNullException(nameof(phaseOf));
            if (paused)
                return false;
            if (spacePressed)
                return true;
            // With a finger on the screen the mouse button is its echo (Input.simulateMouseWithTouches), and the
            // EventSystem knows no mouse pointer over the UI then: the touches decide, as HeroMovementInput reads them.
            if (touchCount <= 0)
                return mousePressed && !mouseOverUi;
            for (int i = 0; i < touchCount; i++)
            {
                if (phaseOf(i) == TouchPhase.Began && (touchOverUi == null || !touchOverUi(i)))
                    return true;
            }
            return false;
        }

        private static bool OverUi(int pointerId)
        {
            EventSystem events = EventSystem.current;
            if (events == null)
                return false;
            return pointerId < 0 ? events.IsPointerOverGameObject() : events.IsPointerOverGameObject(pointerId);
        }
    }
}
