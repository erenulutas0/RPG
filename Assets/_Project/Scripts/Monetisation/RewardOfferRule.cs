using System;

namespace Cryptforge.Monetisation
{
    // Where the result screen's one offer stands (docs/58 sections 1, 3 and 5a). Every state before NoAd hides the offer;
    // NoAd shows it as unavailable, plainly, with the run's normal reward standing; Video and Skip offer it.
    public enum RewardOfferState
    {
        // No result screen is open.
        None,
        // Fewer than two completed runs: no video before the player understands the game (3.4).
        TooEarly,
        // The run's result is not saved yet: no video before it is (3.1).
        NotSaved,
        // The run banked less than the minimum (MinimumRunGold): a +1 offer is not worth a video.
        NoGold,
        // This run's one offer is over: its bonus credited, its video skipped or failed twice, or the player declined.
        Resolved,
        // The video is on screen.
        Showing,
        // No video is ready and the skip is not owned: the button says so plainly (5a).
        NoAd,
        // A video is ready.
        Video,
        // The player owns "skip reward videos": the same bonus, as often, without the video (section 1).
        Skip
    }

    // The offer's rule as pure arithmetic and a decision table, with nothing to load and nothing to save.
    public static class RewardOfferRule
    {
        // docs/58 section 1: the run's gold x1.5, as a percentage so the bonus stays in whole gold. A starting value that
        // gear_economy.py measures before it ships (section 4).
        public const int MultiplierPercent = 150;
        // The least banked gold a run needs for an offer (the lead, 2026-11-19): below it the bonus is a few gold, never
        // worth a video.
        public const int MinimumRunGold = 10;
        // The first completed run whose result screen may offer (section 3.4): the second.
        public const int FirstOfferRun = 2;

        // The gold a credited bonus adds to a run that banked runGold: the run's gold times (multiplierPercent - 100) %,
        // rounded UP to whole gold, so a run's total after the bonus is never below what "x1.5" promises (15 banked gives
        // +8, 23 in all, where 22.5 would be exact; 1 banked gives +1). Integer arithmetic only: no float, no culture.
        public static int BonusGold(int runGold, int multiplierPercent = MultiplierPercent)
        {
            if (runGold < 0)
                throw new ArgumentOutOfRangeException(nameof(runGold));
            if (multiplierPercent < 100)
                throw new ArgumentOutOfRangeException(nameof(multiplierPercent), "The bonus never takes gold away.");
            long extra = ((long)runGold * (multiplierPercent - 100) + 99) / 100;
            return extra > int.MaxValue ? int.MaxValue : (int)extra;
        }

        // completedRuns counts the run whose result is on screen. resolved: this run's offer is over (credited, its video
        // skipped or failed twice, or declined). An owner of the skip is offered the skip even with a video ready: they paid
        // not to watch.
        public static RewardOfferState Evaluate(int completedRuns, bool resultSaved, int runGold, bool resolved,
            bool ownsSkip, bool videoReady, int multiplierPercent = MultiplierPercent, int minimumRunGold = MinimumRunGold)
        {
            if (completedRuns < FirstOfferRun)
                return RewardOfferState.TooEarly;
            if (!resultSaved)
                return RewardOfferState.NotSaved;
            if (runGold < minimumRunGold || BonusGold(runGold, multiplierPercent) <= 0)
                return RewardOfferState.NoGold;
            if (resolved)
                return RewardOfferState.Resolved;
            if (ownsSkip)
                return RewardOfferState.Skip;
            return videoReady ? RewardOfferState.Video : RewardOfferState.NoAd;
        }

        // The offer can be taken: a ready video, or the skip.
        public static bool IsOffered(RewardOfferState state) =>
            state == RewardOfferState.Video || state == RewardOfferState.Skip;

        // The result screen shows the offer's button, taken or plainly unavailable.
        public static bool IsShown(RewardOfferState state) => IsOffered(state) || state == RewardOfferState.NoAd;
    }
}
