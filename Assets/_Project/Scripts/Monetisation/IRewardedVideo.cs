using System;

namespace Cryptforge.Monetisation
{
    // How a shown rewarded video ended. Completed is the network's "reward earned"; Skipped is a video closed before it;
    // Failed is a video that could not be shown (none ready, expired, or the network's error).
    public enum RewardedVideoResult
    {
        Completed,
        Skipped,
        Failed
    }

    // The one rewarded video placement of the first release, the result screen's (docs/58 sections 1 and 3.5). The SDK's
    // adapter implements it at integration (AdMob's RewardedAd, docs/59 decision 5); FakeRewardedVideo stands in for the
    // Editor, the tests and the capture tools. The adapter calls back on the main thread (the SDK's own callbacks may not),
    // once per call; RewardOfferCoordinator still ignores a second report.
    public interface IRewardedVideo
    {
        // A loaded video can be shown now. It can turn false again on its own (a network's video expires).
        bool IsReady { get; }

        // Requests one video. onLoaded(true) once it was matched and is ready, onLoaded(false) for no fill or an error.
        // The coordinator calls it only when IAdConsent.CanRequestAds and never for an owner of the skip (docs/58 5a).
        void Load(Action<bool> onLoaded);

        // Shows the ready video; onResult once, when it closes or fails to show. The run's result is saved before any call
        // (docs/58 3.1): the coordinator checks it first.
        void Show(Action<RewardedVideoResult> onResult);
    }
}
