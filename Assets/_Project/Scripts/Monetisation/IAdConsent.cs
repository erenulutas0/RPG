using System;

namespace Cryptforge.Monetisation
{
    // The consent gate in front of every ad request (docs/58 5a, docs/59 decision 5): Google's UMP at integration
    // (ConsentInformation.Update, then ConsentForm.LoadAndShowConsentFormIfRequired). No ad is requested unless
    // CanRequestAds; the run completes and pays its normal reward without ads, and offline. FakeAdConsent stands in until
    // the SDK comes in.
    public interface IAdConsent
    {
        // UMP's CanRequestAds(): true once consent was gathered, or is not required where the player is. It can be true on
        // a later launch before Gather runs (UMP keeps the last answer).
        bool CanRequestAds { get; }

        // Updates the consent information and shows the form when it is required; onCompleted once, after which
        // CanRequestAds holds the answer. A failure completes too, with CanRequestAds as it was.
        void Gather(Action onCompleted);
    }
}
