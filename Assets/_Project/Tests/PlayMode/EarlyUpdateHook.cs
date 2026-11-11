using System;
using UnityEngine;

namespace Cryptforge.Tests
{
    // For a scene test that must act inside a frame's Updates before the scene's own scripts do - the hero's walk (-60),
    // the encounter (-50), the Warden's slams (-40) and every AttackController (0): the action handed to Next runs once, at
    // the start of the next frame's Updates. A test coroutine resumes only after every Update, too late to land a blow
    // the rest of the frame then answers.
    [AddComponentMenu("")]
    [DefaultExecutionOrder(-1000)]
    internal sealed class EarlyUpdateHook : MonoBehaviour
    {
        public Action Next;

        private void Update()
        {
            Action next = Next;
            Next = null;
            next?.Invoke();
        }
    }
}
