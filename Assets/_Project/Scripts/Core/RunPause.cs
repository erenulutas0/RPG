using System;

namespace Cryptforge.Core
{
    // The single owner of "is the run frozen?". An open choice and the player's pause each hold the run, and it runs
    // again only when neither does. The player cannot pause on top of a choice, which already waits, or after the end.
    public sealed class RunPause
    {
        private readonly RunState _run;

        public bool IsChoiceOpen { get; private set; }
        public bool IsPlayerPaused { get; private set; }
        public bool IsFrozen => IsChoiceOpen || IsPlayerPaused;
        public bool CanPause => !_run.HasEnded && !IsFrozen;
        public event Action Changed;

        public RunPause(RunState run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
            _run.Ended += OnRunEnded;
        }

        public void SetChoiceOpen(bool open)
        {
            if (open == IsChoiceOpen)
                return;

            IsChoiceOpen = open;
            Changed?.Invoke();
        }

        public bool TryPause()
        {
            if (!CanPause)
                return false;

            IsPlayerPaused = true;
            Changed?.Invoke();
            return true;
        }

        public bool TryResume()
        {
            if (!IsPlayerPaused)
                return false;

            IsPlayerPaused = false;
            Changed?.Invoke();
            return true;
        }

        private void OnRunEnded()
        {
            if (!IsPlayerPaused)
                return;

            IsPlayerPaused = false;
            Changed?.Invoke();
        }
    }
}
