using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Cryptforge.UI
{
    // What skips a presentation the run waits on (BossRevealView: the boss walking out of the gate): a tap or a click that
    // begins this frame, or Space. Not a finger that was already on the screen - a player walking the hero as the boss
    // walks out - nor one held on through it. Any touch on the screen used to count, so on the phone a player who was
    // moving skipped the reveal on its first frame (the run flow's review, 2026-11-11). Nor a tap on the UI - the pause
    // button - which is the button's, and nothing while the game stands paused: the reveal's Update runs at a time scale
    // of 0, so a tap on the paused screen latched the skip for the moment play resumed (the same review). A button the
    // presentation holds is no one's to tap: the ability button stands greyed while the reveal holds the fight
    // (AbilityController.IsHeld), so a tap on it skips as one on the floor does - counted as a tap on the UI, it did
    // nothing at all (the same review). Skips is pure, so the rule is tested without a device; ThisFrame reads the legacy
    // Input.
    public static class RevealSkip
    {
        private static readonly Func<int, TouchPhase> InputTouchPhase = index => Input.GetTouch(index).phase;
        private static readonly Func<int, bool> InputTouchOverUi = index => OverUi(Input.GetTouch(index).fingerId);
        private static readonly Func<int, bool> InputTouchOnHeld = index => IsOn(_held, Input.GetTouch(index).position);
        // The held button of the ThisFrame call under way, for InputTouchOnHeld: a field, so reading a frame allocates
        // nothing.
        private static RectTransform _held;

        // This frame's input, as the legacy Input reads it; paused whether the game stands paused; heldButton the button the
        // presentation holds greyed (BossRevealView.HeldButton: the ability button), or null. Read it once the EventSystem
        // has taken this frame's input (BossRevealView reads it in LateUpdate): before that, a touch that began this frame
        // is not yet known to be over a button.
        public static bool ThisFrame(bool paused, RectTransform heldButton = null)
        {
            if (paused)
                return false;
            bool mouse = Input.GetMouseButtonDown(0);
            int touches = Input.touchCount;
            bool mouseAlone = mouse && touches == 0;
            bool held = heldButton != null;
            _held = heldButton;
            bool skips = Skips(touches, InputTouchPhase, InputTouchOverUi, held ? InputTouchOnHeld : null, mouse,
                mouseAlone && OverUi(-1), mouseAlone && held && IsOn(heldButton, Input.mousePosition),
                Input.GetKeyDown(KeyCode.Space), false);
            _held = null;
            return skips;
        }

        // touchCount touches on the screen, phaseOf each one's phase this frame; mousePressed and spacePressed whether the
        // button or the key went down this frame. Nothing is over the UI and the game is not paused.
        public static bool Skips(int touchCount, Func<int, TouchPhase> phaseOf, bool mousePressed, bool spacePressed) =>
            Skips(touchCount, phaseOf, null, mousePressed, false, spacePressed, false);

        // As above; touchOverUi(index) whether that touch is over the UI (null: none is), mouseOverUi whether the mouse
        // pointer is, paused whether the game stands paused (then nothing skips).
        public static bool Skips(int touchCount, Func<int, TouchPhase> phaseOf, Func<int, bool> touchOverUi, bool mousePressed,
            bool mouseOverUi, bool spacePressed, bool paused) =>
            Skips(touchCount, phaseOf, touchOverUi, null, mousePressed, mouseOverUi, false, spacePressed, paused);

        // As above; touchOnHeldButton(index) whether that touch is on a button the presentation holds (null: none is), and
        // mouseOnHeldButton whether the mouse pointer is. Such a button stands greyed and takes no tap - the ability button
        // while the reveal holds the fight - so a tap on it skips as one on the floor does, though it is over the UI; a tap
        // on any other UI stays that UI's.
        public static bool Skips(int touchCount, Func<int, TouchPhase> phaseOf, Func<int, bool> touchOverUi,
            Func<int, bool> touchOnHeldButton, bool mousePressed, bool mouseOverUi, bool mouseOnHeldButton, bool spacePressed,
            bool paused)
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
                return mousePressed && (!mouseOverUi || mouseOnHeldButton);
            for (int i = 0; i < touchCount; i++)
            {
                if (phaseOf(i) == TouchPhase.Began && (touchOverUi == null || !touchOverUi(i) ||
                                                      (touchOnHeldButton != null && touchOnHeldButton(i))))
                    return true;
            }
            return false;
        }

        // Whether a screen point lies on this UI element, where the EventSystem finds it: on an overlay canvas, as the HUD
        // is, screen and canvas are one; any other canvas is seen through its camera. Null is on nothing.
        internal static bool IsOn(RectTransform element, Vector2 screenPoint)
        {
            if (element == null)
                return false;
            Canvas canvas = element.GetComponentInParent<Canvas>();
            Canvas root = canvas != null ? canvas.rootCanvas : null;
            Camera camera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(element, screenPoint, camera);
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
