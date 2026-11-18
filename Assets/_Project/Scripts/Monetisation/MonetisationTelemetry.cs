using System;
using Cryptforge.Analytics;

namespace Cryptforge.Monetisation
{
    // How the result screen's offer ended for a run.
    public enum RewardResultKind
    {
        // The video completed: the bonus is credited.
        Watched,
        // The video was closed before its reward: nothing credited.
        Skipped,
        // The video could not be shown: nothing credited.
        Failed,
        // The skip's owner took the bonus without a video.
        SkipOwned,
        // The player left the result screen with the offer open.
        Declined,
        // The player asked for a video while none was ready, and left without one.
        NoAd
    }

    // The monetisation's telemetry as data (docs/58 3.6 and 5a): each builder returns the fields of one event, in a fixed
    // order, and writes nothing. The caller emits them through IAnalytics; the offer's two events go under the run's id
    // (EmitForRun), whose envelope then carries "run", since the run's own events are over by the result screen.
    public static class MonetisationTelemetry
    {
        public const string RewardOfferEvent = "reward_offer";
        public const string RewardResultEvent = "reward_result";
        public const string StorePurchaseEvent = "store_purchase";
        public const string AdCountersEvent = "ad_counters";

        // reward_offer, once per run, the first time its offer can be taken: offer "video" or "skip", the run's banked
        // gold, the bonus a credit would add, the multiplier as a percentage, and the completed runs counted so far.
        public static TelemetryFields RewardOffer(RewardOfferState state, int runGold, int bonusGold, int multiplierPercent,
            int completedRuns)
        {
            if (!RewardOfferRule.IsOffered(state))
                throw new ArgumentException("reward_offer is written for an offer that can be taken.", nameof(state));
            return new TelemetryFields()
                .Add("offer", state == RewardOfferState.Skip ? "skip" : "video")
                .Add("run_gold", runGold)
                .Add("bonus_gold", bonusGold)
                .Add("multiplier_pct", multiplierPercent)
                .Add("completed_runs", completedRuns);
        }

        // reward_result, once per run whose offer was offered or asked for: how it ended, whether the bonus was credited,
        // and the gold credited (0 when not).
        public static TelemetryFields RewardResult(RewardResultKind result, bool credited, int creditedGold)
        {
            if (creditedGold < 0 || (!credited && creditedGold != 0))
                throw new ArgumentOutOfRangeException(nameof(creditedGold));
            return new TelemetryFields()
                .Add("result", Code(result))
                .Add("credited", credited)
                .Add("bonus_gold", creditedGold);
        }

        // store_purchase: the product and how its purchase ended.
        public static TelemetryFields StorePurchase(string productId, StorePurchaseResult result)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("A product id is required.", nameof(productId));
            return new TelemetryFields()
                .Add("product", productId)
                .Add("result", Code(result));
        }

        // ad_counters: the launch's counters so far (AdCounters), cumulative, so the last line of a session holds its
        // totals and a lost line costs nothing but its moment. Counts only; the rates are the reader's.
        public static TelemetryFields AdCounters(AdCounters counters)
        {
            if (counters == null)
                throw new ArgumentNullException(nameof(counters));
            return new TelemetryFields()
                .Add("requests", counters.Requests)
                .Add("matches", counters.Matches)
                .Add("shows", counters.Shows)
                .Add("show_failures", counters.ShowFailures)
                .Add("rewards", counters.Rewards)
                .Add("attempts", counters.Attempts)
                .Add("attempts_ready", counters.AttemptsReady);
        }

        public static string Code(RewardResultKind result)
        {
            switch (result)
            {
                case RewardResultKind.Watched:
                    return "watched";
                case RewardResultKind.Skipped:
                    return "skipped";
                case RewardResultKind.Failed:
                    return "failed";
                case RewardResultKind.SkipOwned:
                    return "skip_owned";
                case RewardResultKind.Declined:
                    return "declined";
                case RewardResultKind.NoAd:
                    return "no_ad";
                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
        }

        public static string Code(StorePurchaseResult result)
        {
            switch (result)
            {
                case StorePurchaseResult.Purchased:
                    return "purchased";
                case StorePurchaseResult.Pending:
                    return "pending";
                case StorePurchaseResult.Cancelled:
                    return "cancelled";
                case StorePurchaseResult.Failed:
                    return "failed";
                case StorePurchaseResult.AlreadyOwned:
                    return "already_owned";
                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }
        }
    }
}
