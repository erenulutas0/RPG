using Cryptforge.Localisation;
using Cryptforge.Monetisation;

namespace Cryptforge.UI
{
    // The words of the result screen's rewarded offer and of the "Skip reward videos" product (docs/58 sections 1, 5a and
    // 7.4), in the game's language (docs/60). Plain words only: what the button gives, that no video is ready, and how a
    // video or a purchase ended - never a nag.
    public static class RewardOfferText
    {
        // The offer's button for a state it shows (RewardOfferRule.IsShown); empty for any other.
        public static string Button(RewardOfferState state, int bonus)
        {
            switch (state)
            {
                case RewardOfferState.Video:
                    return Loc.Format("result.offer_video", "Watch a video: +{bonus} gold", ("bonus", bonus));
                case RewardOfferState.Skip:
                    return Loc.Format("result.offer_skip", "Skip owned: +{bonus} gold", ("bonus", bonus));
                case RewardOfferState.NoAd:
                    return Loc.Text("result.offer_no_ad", "No video right now");
                default:
                    return string.Empty;
            }
        }

        // How this run's video or offer ended, in plain words; still says whether the screen offers it again (a first
        // failure's retry). Empty for an outcome that needs no words: the player declined, or left asking.
        public static string Outcome(RewardResultKind kind, bool credited, bool offeredAgain, int bonus)
        {
            if (credited)
                return Loc.Format("result.offer_credited", "+{bonus} gold added", ("bonus", bonus));
            switch (kind)
            {
                case RewardResultKind.Skipped:
                    return Loc.Text("result.offer_skipped", "The video was closed early: no bonus");
                case RewardResultKind.Failed:
                    return offeredAgain
                        ? Loc.Text("result.offer_failed_retry", "The video could not play: try once more")
                        : Loc.Text("result.offer_failed", "The video could not play: no bonus");
                default:
                    return string.Empty;
            }
        }

        // The skip product's link and settings entry: its name, "owned" once bought, and how a purchase that did not grant it
        // ended (pending, or the store not answering; a cancelled one says nothing).
        public static string Skip(bool owned, bool purchasing, StorePurchaseResult? last)
        {
            if (owned)
                return Loc.Text("store.skip_videos_owned", "Skip reward videos: owned");
            if (purchasing)
                return Loc.Text("store.purchasing", "Opening the store...");
            if (last == StorePurchaseResult.Pending)
                return Loc.Text("store.purchase_pending", "Purchase pending");
            if (last == StorePurchaseResult.Failed)
                return Loc.Text("store.purchase_failed", "The store did not answer: try later");
            return Loc.Text("store.skip_videos", "Skip reward videos");
        }
    }
}
