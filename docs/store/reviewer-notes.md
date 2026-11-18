# Notes for the Play and AdMob reviewers (draft, 2026-11-19)

Where these go:

- **Play Console > Policy > App content > App access**: choose "All functionality in my app is available without any
  access restrictions", and paste the short version (section 1) where the form offers instructions. Play reviews the
  release; the production-access request after the closed test is a separate step (docs/59 section 4).
- **AdMob's app review** (after the Play listing is linked): AdMob reviews the app's ad placement and its
  `app-ads.txt` on its own; section 2 is what to have ready if AdMob asks, and what the game must do to pass.

Written in English, which both reviews read. Before sending: fill {{CONTACT_EMAIL}} and {{WEBSITE_URL}}, settle each
[[CHECK]] against the release build (the offer and the store entry are built by the monetisation scaffolding, docs/58
section 6, which is not built yet), then delete the markers.

## 1. Short version (for the App access field)

```text
No login, account or code is needed; every feature is open. The game is fully playable offline and without ads. The only ad is an optional rewarded video, offered on the result screen from the player's second completed run: tap Start run, play until the run ends, tap Try again, end a second run; its result screen offers a video for 1.5x that run's gold. No ad ever plays during play. One in-app product, Skip reward videos, gives the same bonus without the video. Contact: {{CONTACT_EMAIL}}
```

(492 characters with the placeholder, about 500 once a typical email is in; if the field is shorter, cut the last two
sentences.)

## 2. Long version

### 2.1 Access and first launch

- No account, login, code or server. Install and play.
- The game opens in the phone's language: Turkish when the system language is Turkish, English otherwise. To switch,
  pause (the "II" button) and tap "Language: English" / "Dil: Türkçe".
- In the European Economic Area, the United Kingdom and Switzerland, Google's consent message (UMP) may appear at
  launch. [[CHECK: and what players in Turkey see - README question 1.]]

### 2.2 Reaching the reward offer

The offer appears **only on the result screen, and only from the second completed run** (docs/58: "no video before the
player understands the game").

1. On the first screen (the map and hero choice), tap **Start run** ("Sefere çık").
2. Play. The hero attacks automatically; drag on the screen to move; the round button at the bottom right fires the
   hero's skill.
3. Let the run end. Any ending counts [[CHECK: that a fall, an extract and a full clear all count as "completed"]]:
   - **fastest:** stand among the enemies until the hero falls ("Defeated") - about [[CHECK: measured time]] on the
     first floor;
   - or clear the first floor and choose **Extract** ("Çık") at the checkpoint - about [[CHECK: measured time]].
4. On the result screen, tap **Try again** ("Tekrar dene") and end a second run the same way.
5. The second run's result screen shows the offer: [[CHECK: the button's exact English and Turkish labels]].
   - **Watch:** a rewarded video plays. When Google reports it completed, the run's gold is multiplied by 1.5 and
     credited once.
   - **No / skip / the video fails:** nothing extra; the run's normal reward stands. Nothing asks again on that
     screen.
   - **No ad ready** (no connection, no fill): the button says so plainly and the normal reward stands.
   - The run's result and rewards are saved **before** the video opens, so closing the app during the video costs at
     most the bonus, never the run (docs/58 section 3).

### 2.3 The in-app product

- **Skip reward videos** ("Ödül videolarını atla"), one-time, non-consumable. Where it is sold: [[CHECK: the store
  entry's place, e.g. beside the offer on the result screen or in the pause menu]].
- After purchase, the result screen's bonus is granted **without** a video, as often as the free player gets it with
  one. It gives nothing else: no gold, materials, boosters, efsun or items are sold.
- Reinstall with the same Google account: the game asks Google Play which products the account owns and unlocks it
  again (the save itself is local and does not come back; the policy says so).

### 2.4 Offline

Turn on airplane mode: everything works - the runs, the Anvil (basma), efsun, the Bag, the Relic Forge and the arena
(local: rivals made on the phone, no online play). The result screen's offer says no video is available; the run's
reward is unchanged. With the connection back, the offer returns on later runs.

### 2.5 Ads, for the AdMob review

- **One ad unit, rewarded.** No interstitials, no banners, no app-open ads, no native ads, no revive video
  (docs/58 section 1).
- **Never during play** and never forced: only on the result screen, after the run is saved, only when the player taps
  the offer.
- Ads are requested only when Google's UMP says ads may be requested (`CanRequestAds()`), docs/58 section 5a.
- Development and test builds use Google's test ad units; the release build uses the live unit. Nobody on the team taps
  live ads.
- `app-ads.txt` is published at **{{WEBSITE_URL}}/app-ads.txt**, the root of the developer website on the Play listing.
- The app is linked to its Play listing in AdMob before the review is requested.

### 2.6 Privacy options

To change ad choices at any time: [[CHECK: the path, e.g. pause > Privacy options]]. Google's UMP requires this entry
point where its consent message applies; the privacy policy is linked from the same place (Play's User Data policy wants
it inside the app too).

### 2.7 Content

A single-player fantasy action game: a human hero (a knight or a mage) fights non-human creatures (stone, lava and bark
golems, a mite, a mirror sentry, a wraith, a glass regent). No blood. No chat, no user-generated content, no accounts,
no online play in this version.

### 2.8 Contact

{{CONTACT_EMAIL}}
