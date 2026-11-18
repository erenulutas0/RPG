using System;
using System.Collections.Generic;

namespace Cryptforge.Monetisation
{
    // The offline stand-in for the consent gate (docs/58 5a). CanRequestAds starts as given (false, as on a first launch
    // before UMP has answered); gathering sets it to GatherAnswer, or leaves it as it was when GatherAnswer is null (an
    // update that failed). Immediate by default; Deferred holds the callbacks until CompletePending.
    public sealed class FakeAdConsent : IAdConsent
    {
        private readonly Queue<Action> _pending = new Queue<Action>();

        public FakeAdConsent(bool canRequestAds = false)
        {
            CanRequestAds = canRequestAds;
        }

        public bool CanRequestAds { get; set; }
        public bool? GatherAnswer { get; set; } = true;
        public bool Deferred { get; set; }
        public int GatherCalls { get; private set; }
        public int Pending => _pending.Count;

        public void Gather(Action onCompleted)
        {
            if (onCompleted == null)
                throw new ArgumentNullException(nameof(onCompleted));
            GatherCalls++;
            Action complete = () =>
            {
                if (GatherAnswer.HasValue)
                    CanRequestAds = GatherAnswer.Value;
                onCompleted();
            };
            if (Deferred)
                _pending.Enqueue(complete);
            else
                complete();
        }

        // Runs every held callback in order; how many ran.
        public int CompletePending()
        {
            int ran = 0;
            while (_pending.Count > 0)
            {
                _pending.Dequeue()();
                ran++;
            }
            return ran;
        }
    }
}
