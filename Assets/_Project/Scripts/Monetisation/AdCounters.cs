namespace Cryptforge.Monetisation
{
    // docs/58 5a's counters, kept apart (the owner's second report, section 3), for one launch. The fill side: eligible
    // requests, matches (match rate = matches / requests), shows (show rate = shows / matches), shows that failed, and
    // rewards. The player side: voluntary watch attempts, and those made with a video ready (availability = ready attempts
    // / attempts); a "No" is not an attempt, so a player who declines with a video ready is not missing demand. Confirmed
    // revenue is AdMob's own report, not counted here.
    //
    // Owners of the skip never reach these: RewardOfferCoordinator requests no video for an owner and an owner's tap takes
    // the skip, so an owner adds no request, match, show or attempt. A request is eligible by construction: none is made
    // without IAdConsent.CanRequestAds.
    public sealed class AdCounters
    {
        public int Requests { get; private set; }
        public int Matches { get; private set; }
        public int Shows { get; private set; }
        public int ShowFailures { get; private set; }
        public int Rewards { get; private set; }
        public int Attempts { get; private set; }
        public int AttemptsReady { get; private set; }

        // NaN while the denominator is 0; the telemetry writes NaN as null.
        public double MatchRate => Requests == 0 ? double.NaN : Matches / (double)Requests;
        public double ShowRate => Matches == 0 ? double.NaN : Shows / (double)Matches;
        public double Availability => Attempts == 0 ? double.NaN : AttemptsReady / (double)Attempts;

        public void RecordRequest() => Requests++;

        public void RecordMatch() => Matches++;

        // A completed or skipped video was shown; a failed one was not. Only a completed one rewards.
        public void RecordShow(RewardedVideoResult result)
        {
            if (result == RewardedVideoResult.Failed)
            {
                ShowFailures++;
                return;
            }
            Shows++;
            if (result == RewardedVideoResult.Completed)
                Rewards++;
        }

        public void RecordAttempt(bool videoReady)
        {
            Attempts++;
            if (videoReady)
                AttemptsReady++;
        }
    }
}
