using System;
using System.Collections.Generic;

namespace Cryptforge.Monetisation
{
    // The offline stand-in for the rewarded video (docs/58 3.5): no network and no SDK, for the Editor, the tests and the
    // capture tools. Every outcome is scriptable: each load fills or not (ScriptLoads, else FillByDefault), each show ends
    // as scripted (ScriptShows, else ResultByDefault). Immediate by default, the callbacks run inside Load and Show; Deferred
    // holds them until CompleteLoad and CompleteShow, so a test can keep a video on screen, or never finish it (the app
    // killed during the video). A shown video is used up, as a network's is; a show with none ready fails at once.
    public sealed class FakeRewardedVideo : IRewardedVideo
    {
        private readonly Queue<bool> _fills = new Queue<bool>();
        private readonly Queue<RewardedVideoResult> _results = new Queue<RewardedVideoResult>();
        private Action<bool> _pendingLoad;
        private Action<RewardedVideoResult> _pendingShow;

        // What a load does when no scripted fill waits.
        public bool FillByDefault { get; set; } = true;
        // How a show ends when no scripted result waits.
        public RewardedVideoResult ResultByDefault { get; set; } = RewardedVideoResult.Completed;
        // Callbacks wait for CompleteLoad / CompleteShow instead of running inside Load / Show.
        public bool Deferred { get; set; }
        // Each show reports its result twice, as a misbehaving SDK might.
        public bool ReportTwice { get; set; }

        public bool IsReady { get; private set; }
        public int LoadCalls { get; private set; }
        public int ShowCalls { get; private set; }
        public bool LoadPending => _pendingLoad != null;
        // A video is on screen: shown and not yet completed.
        public bool ShowPending => _pendingShow != null;

        public FakeRewardedVideo ScriptLoads(params bool[] fills)
        {
            if (fills != null)
            {
                foreach (bool fill in fills)
                    _fills.Enqueue(fill);
            }
            return this;
        }

        public FakeRewardedVideo ScriptShows(params RewardedVideoResult[] results)
        {
            if (results != null)
            {
                foreach (RewardedVideoResult result in results)
                    _results.Enqueue(result);
            }
            return this;
        }

        public void Load(Action<bool> onLoaded)
        {
            if (onLoaded == null)
                throw new ArgumentNullException(nameof(onLoaded));
            if (_pendingLoad != null)
                throw new InvalidOperationException("The fake holds one load at a time; complete the pending one first.");
            LoadCalls++;
            _pendingLoad = onLoaded;
            if (!Deferred)
                CompleteLoad();
        }

        // Ends the pending load with the next scripted fill.
        public void CompleteLoad()
        {
            Action<bool> callback = _pendingLoad;
            if (callback == null)
                throw new InvalidOperationException("No load is pending.");
            _pendingLoad = null;
            bool fill = _fills.Count > 0 ? _fills.Dequeue() : FillByDefault;
            if (fill)
                IsReady = true;
            callback(fill);
        }

        public void Show(Action<RewardedVideoResult> onResult)
        {
            if (onResult == null)
                throw new ArgumentNullException(nameof(onResult));
            ShowCalls++;
            if (!IsReady || _pendingShow != null)
            {
                onResult(RewardedVideoResult.Failed);
                return;
            }
            IsReady = false;
            _pendingShow = onResult;
            if (!Deferred)
                CompleteShow();
        }

        // Ends the video on screen with the next scripted result.
        public void CompleteShow()
        {
            Action<RewardedVideoResult> callback = _pendingShow;
            if (callback == null)
                throw new InvalidOperationException("No video is on screen.");
            _pendingShow = null;
            RewardedVideoResult result = _results.Count > 0 ? _results.Dequeue() : ResultByDefault;
            callback(result);
            if (ReportTwice)
                callback(result);
        }

        // The ready video expires unused, as a network's does after a while.
        public void Expire()
        {
            IsReady = false;
        }
    }
}
