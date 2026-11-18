# Play Console: the Data safety form, answer by answer (draft, 2026-11-19)

For Play Console > Policy > App content > Data safety. The form is required on the closed test, the open test and
production; only an app that never leaves internal testing is exempt. The answers come from the SDK list of docs/58
and docs/59 (Google Mobile Ads with UMP, Unity IAP over Google Play Billing, GameAnalytics), each SDK's own published
disclosure as read on 2026-11-19, and the game's own behaviour. **Google holds the developer responsible for the
answers**, so the ones marked "verify" are checked against the release build once the SDKs are in (section 5).

Play's definitions that decide the answers
([Data safety help](https://support.google.com/googleplay/android-developer/answer/10787469)):

- **Collected** = sent off the device by the app, **including by its SDKs**. Data that never leaves the phone (the save,
  the arena save, the language, the cached purchase, the local play log) is **not** collected and is not declared.
- **Shared** = passed to a third party. **Not** sharing: a service provider processing on our behalf (GameAnalytics,
  a processor; Unity for IAP, which declares "not shared"), a legal obligation, a user-initiated transfer, fully
  anonymous data. Google's ad SDK declares its data "collected and shared" (Google uses it as an independent
  controller), so those types are shared.
- **Optional** only if every user can avoid it. Nothing here can be avoided by every user once the SDKs run, so every
  type is **required**.
- An email a player sends us from their own mail app is not collected by the app and is not declared.

## 1. Data collection and security

| Question | Answer | Why |
|---|---|---|
| Does your app collect or share any of the required user data types? | **Yes** | The three SDKs send data off the device (section 2) |
| Is all of the user data collected by your app encrypted in transit? | **Yes** (verify) | Google: "All user data collected by the SDK is encrypted in transit using TLS". Unity IAP: encrypted in transit, Yes. GameAnalytics: its collection API accepts HTTPS only ([setup](https://docs.gameanalytics.com/event-tracking-and-integrations/sdks-and-collection-api/api/setup)); confirm the SDK's endpoint in a network capture of the release build |
| Account creation | **My app does not allow users to create an account** | No account, login or sign-in of any kind (docs/55 section 9, answer 7). The account-deletion web link is required only of apps that allow account creation ([account deletion](https://support.google.com/googleplay/android-developer/answer/13327111)) |
| Do you provide a way for users to request that their data is deleted? | **Owner's decision** (README, question 5) | "Yes" only if a request can be honoured: the player must be able to tell us their analytics ID (e.g. a player ID shown in the settings), which we pass to GameAnalytics (its notice: players go to the developer, "who can ask us for assistance"). Google's ad data is not deletable by us; the player resets or deletes the advertising ID in Android's settings. Unity IAP answers "No" for its own SDK. Without the player ID, answer **No** and say in the policy how the rights are used |
| Independent security review (optional badge) | **No** | Not done |
| Families policy badge | **Not applicable** | The game is not designed for children (README, question 6) |

## 2. Data types

"Collected" and "Shared" are for the app as a whole; the SDK column says where each comes from. Every collected type:
**not processed ephemerally** (answer "No"; Unity marks its approximate location ephemeral, but GameAnalytics keeps a
shortened IP for up to 30 days and the location fields), and **required**.

| Category | Data type | Collected | Shared | Purposes of collection | Purposes of sharing | From |
|---|---|---|---|---|---|---|
| Location | **Approximate location** | Yes | Yes | Advertising or marketing; Analytics; Fraud prevention, security, and compliance; App functionality | Advertising or marketing; Analytics; Fraud prevention, security, and compliance | Google Mobile Ads (IP address, "may be used to estimate the general location"); GameAnalytics (IP to country, region, city); Unity IAP |
| Location | Precise location | No | No | | | No location permission; nothing reads GPS |
| Personal info | **User IDs** | Yes | No | App functionality; Analytics | | Unity IAP declares "Personal identifiers: collected" for itself (verify what it means at integration; drop the row if the integrated version does not) |
| Personal info | Name, Email address, Address, Phone number, Race and ethnicity, Political or religious beliefs, Sexual orientation, Other info | No | No | | | No account, no form, no free text |
| Financial info | **Purchase history** | Yes | No | App functionality; Analytics | | Unity IAP (purchase history, collected, not shared); GameAnalytics if the purchase is sent as an event (docs/58 `store_purchase`) |
| Financial info | User payment info, Credit score, Other financial info | No | No | | | Google Play takes the payment; the app never sees card or bank details (Play: a payment processor's collection need not be declared when the app never accesses it) |
| Health and fitness | Health info, Fitness info | No | No | | | |
| Messages | Emails, SMS or MMS, Other in-app messages | No | No | | | No chat |
| Photos and videos | Photos, Videos | No | No | | | |
| Audio files | Voice or sound recordings, Music files, Other audio files | No | No | | | No microphone |
| Files and docs | Files and docs | No | No | | | The save is the app's own file, never sent |
| Calendar | Calendar events | No | No | | | |
| Contacts | Contacts | No | No | | | |
| App activity | **App interactions** | Yes | Yes | Analytics; Advertising or marketing; Fraud prevention, security, and compliance | Advertising or marketing; Analytics; Fraud prevention, security, and compliance | Google Mobile Ads ("user product interactions, including app launch, taps, and video views", collected and shared); GameAnalytics (sessions) |
| App activity | In-app search history | No | No | | | No search |
| App activity | Installed apps | No | No | | | Not in any of the three disclosures |
| App activity | Other user-generated content | No | No | | | No UGC; the arena's rivals are generated on the phone |
| App activity | **Other actions** | Yes | No | Analytics | | GameAnalytics: gameplay events (runs started and ended, the first death, the first basma, the reward offer and the answer) |
| Web browsing | Web browsing history | No | No | | | |
| App info and performance | **Crash logs** | Yes | No | Analytics | | Unity IAP (crash logs, collected); GameAnalytics error events if its error reporting is on |
| App info and performance | **Diagnostics** | Yes | Yes | Analytics; App functionality; Advertising or marketing; Fraud prevention, security, and compliance | Advertising or marketing; Analytics; Fraud prevention, security, and compliance | Google Mobile Ads ("app and SDK performance, such as app launch time, hang rate, and energy usage", collected and shared); Unity IAP (app diagnostics) |
| App info and performance | **Other app performance data** | Yes | No | App functionality; Analytics | | Unity IAP |
| Device or other IDs | **Device or other IDs** | Yes | Yes | Advertising or marketing; Analytics; Fraud prevention, security, and compliance; App functionality | Advertising or marketing; Analytics; Fraud prevention, security, and compliance | Google Mobile Ads (Android advertising ID, app set ID, "other identifiers related to signed-in accounts on the device", collected and shared); GameAnalytics (its generated user ID, plus any device ID the integrated version reads); Unity IAP (device identifiers) |

**The UMP SDK.** No separate Play data disclosure for Google's User Messaging Platform was found (searched
2026-11-19). It asks Google's servers whether a consent message applies and stores the answer on the phone; its
request carries the IP address like any other, which the Location row already declares. Re-check the
[Play SDK Index](https://play.google.com/sdks) entry for UMP at integration.

## 3. What the listing will then say (Play generates it)

- "This app may share these data types with third parties: Location, App activity, App info and performance,
  Device or other IDs."
- "This app may collect these data types: Location, Personal info, Financial info, App activity, App info and
  performance, Device or other IDs."
- "Data is encrypted in transit." And the deletion line per section 1's decision.

## 4. The other App content declarations that must agree with this form

| Declaration | Answer | Note |
|---|---|---|
| **Ads** | **Yes, my app contains ads** | Rewarded only, but "rewarded only" or "a product turns it off" does not change the answer (the owner's report, section 1, "Reklam ve erişim") |
| **Advertising ID** | **Yes**; purposes: Advertising or marketing, Analytics, Fraud prevention, security, and compliance | The Google Mobile Ads SDK reads it. Its library merges the `com.google.android.gms.permission.AD_ID` permission into the manifest (verify in the release AAB's merged manifest; the answer and the manifest must agree) |
| **App access** | **All functionality is available without special access** | No login; give the reviewer's path anyway (reviewer-notes.md) |
| **Target audience and content** | The owner's choice; recommended 13-15, 16-17, 18 and over | README question 6. Choosing any group under 13 brings in the Families policy and certified ad SDK settings |
| **Content rating** | iarc-answers.md | |
| **Financial features** | My app doesn't provide any financial features | A game's own in-app purchase is not a financial feature |
| **Government app / Health / News** | No | |
| **Privacy policy** | {{POLICY_URL}} | Must also be linked inside the game (Play's User Data policy: "linked in the Play Console and within the app") |

## 5. Verify at integration (before the closed test)

1. **SDK versions.** Google's disclosure read today is for Google Mobile Ads **25.5.0** ("the latest version");
   Unity's is for **Unity IAP 5.4 and later** (a [legacy page](https://docs.unity.com/en-us/iap/privacy-and-consent/legacy-google-play-data-safety)
   covers older). Re-read both pages for the versions actually integrated and update this file.
2. **GameAnalytics' identifiers.** Its privacy notice speaks of "a randomly generated identifier" and "persistent
   identifiers such as device identifiers" without naming them. Check what the integrated Unity SDK reads (advertising
   ID, app set ID) and whether it can be told not to; the Device IDs row is declared either way.
3. **GameAnalytics' error reporting** (Crash logs row) and the final list of events (Other actions row).
4. **Unity's own player settings.** `ProjectSettings.asset` has `submitAnalytics: 1` (Unity's hardware statistics);
   Unity Analytics, Crash Reporting, Performance Reporting, Purchasing and Ads services are all off in
   `UnityConnectSettings.asset`. Turn hardware statistics off in the release build if the editor allows it, then
   confirm with a network capture of the release build (e.g. a proxy on a test phone) that nothing goes to Unity
   beyond Unity IAP.
5. **The merged manifest** of the release AAB (`aapt2 dump xmltree`): the permissions (today's prototype APK asks only
   `INTERNET`), `AD_ID`, and `android:allowBackup` (unset today, so Android's default, backup allowed - the privacy
   policy's backup paragraph depends on it).
6. **Consent order.** If analytics or the ad SDK must wait for consent (README questions 1-2), confirm that no request
   leaves the phone before the answer, again with a network capture.

## Sources (read 2026-11-19)

- Google Mobile Ads SDK, Play data disclosure (Android, SDK 25.5.0): <https://developers.google.com/admob/android/privacy/play-data-disclosure>;
  the Unity plugin's page says the same: <https://developers.google.com/admob/unity/privacy/play-data-disclosure>
- Unity IAP, Google Play data safety (IAP 5.4+): <https://docs.unity.com/en-us/iap/privacy-and-consent/google-play-data-safety>
- GameAnalytics privacy notice, version 4.3 of 9 November 2026 (processor role, data, retention, hosting): <https://www.gameanalytics.com/trust/privacy-notice>.
  GameAnalytics publishes no Play data safety guide of its own (searched); its rows above are derived from the notice.
- Google Play, Data safety section: <https://support.google.com/googleplay/android-developer/answer/10787469>
- Google Play, account deletion requirement: <https://support.google.com/googleplay/android-developer/answer/13327111>
- Google Play, User Data policy (privacy policy contents and placement): <https://support.google.com/googleplay/android-developer/answer/10144311>
- The owner's release report, section 1 ("Veri Güvenliği ve gizlilik", "Reklam ve erişim"): `docs/research/2026-11-18_release_path_owner_report.md`
