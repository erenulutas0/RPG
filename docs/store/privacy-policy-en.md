<!--
DRAFT for the owner to publish (docs/59 section 3 item 6). Not legal advice.
Before publishing: fill every {{PLACEHOLDER}}, settle every [[CHECK: ...]] and [[IF ...]] (then delete the marker),
and publish as an HTML page on {{POLICY_URL}} (Google Play wants an active, public, non-geofenced URL, no PDF),
linked from the Play listing and from inside the game. Nothing containing "{{" or "[[" may go live.
The Turkish text (privacy-policy-tr.md) is the same policy; keep the two in step.
-->

# Cryptforge Privacy Policy

**Effective date:** {{EFFECTIVE_DATE}}

This privacy policy explains what happens to information about you when you play **Cryptforge**, a game for Android
made by {{DEVELOPER_NAME}} ("we", "us"). It covers the game itself and the services built into it.

## In short

- **No account.** The game never asks for your name, email, phone number or password, and has no login.
- **No server of our own.** Your progress (your gold, your items, your heroes) is saved **on your phone** and does not
  reach us.
- **No chat and nothing you create is shared.** The arena in this version is played on your phone against rivals the
  game makes up; there is no online play.
- **Three outside services** handle the data that does leave your phone:
  - **Google AdMob** shows the optional reward video;
  - **Google's User Messaging Platform (UMP)** asks for your ad choices where the law requires it;
  - **GameAnalytics** counts how the game is played, so we can improve it.
- **Purchases** go through **Google Play**. We never see your card details.
- **The game works offline.** Without internet you can play everything; only the reward video needs a connection.

## 1. Who is responsible

The data controller is {{DEVELOPER_NAME}}, {{POSTAL_ADDRESS}}. For any privacy question or request, write to
**{{CONTACT_EMAIL}}**.

## 2. What stays on your phone

The game keeps these files on your phone, in the app's own storage. They are not sent to us or to anyone else by the
game:

- **Your save:** your gold, materials, items with their + levels and efsun lines, your unlocked heroes, weapons and
  relics, and your progress through the floors.
- **The arena's save:** your arena rivals, results and ladder position.
- **Your settings:** the language you chose.
- **Your purchase, cached:** a copy of whether you own "Skip reward videos", so the game knows even offline. The real
  record of the purchase is kept by Google Play (section 3.4).
- **A small play log:** a record of your recent runs, used to find bugs. It stays on the phone; the oldest part is
  deleted as it grows (about 768 KB at most). [[CHECK: keep this sentence only if the release build keeps the local
  log (docs/59 section 3.3).]]

**To delete it all**, uninstall the game or clear its storage in your phone's settings (Settings > Apps > Cryptforge >
Storage). We cannot restore a deleted save, because we never had a copy.

**Android's own backup.** If you have turned on backup in your phone's settings, Android may copy the game's files to
your Google account as part of its device backup. That backup is Google's service, under your Google account's
settings; we cannot see it. [[CHECK: keep this paragraph only if the release build allows backup. The prototype's
manifest sets no `android:allowBackup`, and Android's default is to allow it.]]

## 3. The services built into the game

### 3.1 Google AdMob (Google Mobile Ads SDK) - the reward video

**What it is for.** After a run, from your second run on, the result screen may offer a short video: watch it and the
run's gold is multiplied by 1.5. It is optional. Saying "No" changes nothing else. There are no other ads in the game:
no banners and no ads between screens.

**What it processes.** Google's own disclosure for the SDK lists, collected and shared automatically for advertising,
analytics and fraud prevention:

- your **IP address**, which may be used to estimate your general location;
- **interactions with the app**, such as app launches, taps and video views;
- **diagnostic information** about the app and the SDK, such as launch time, hang rate and energy use;
- **device identifiers**: the Android advertising ID, the app set ID and, if applicable, other identifiers tied to
  accounts signed in on the device.

**Personalised or not.** Where the law requires your consent (for example in the European Economic Area, the United
Kingdom and Switzerland), Google's consent message asks you first (section 3.2), and ads are only requested once that
step allows it. [[CHECK: what players in Turkey get - see the README's open question on ad consent in Turkey. One of:
the consent message shown everywhere, non-personalised ads for players outside the regions above, or personalised ads
on another legal basis. Rewrite this paragraph to match the choice.]]

**Who decides.** Google uses this data as an independent controller under its own terms. Read how Google uses
information from apps that use its services: <https://policies.google.com/technologies/partner-sites>, and Google's
privacy policy: <https://policies.google.com/privacy>.

**Your controls.** You can reset or delete your advertising ID in your phone's settings (Settings > Privacy > Ads, or
Settings > Google > Ads, depending on the phone). You can change your ad choices in the game at any time:
[[CHECK: the in-game path to the privacy options, e.g. Pause > Privacy options]].

[[CHECK: whether the ad SDK starts when the game opens or only when a video is about to be offered, and whether it
starts at all for owners of "Skip reward videos". If it starts at launch, add: "The SDK may start when the game opens,
even if you never watch a video."]]

### 3.2 Google User Messaging Platform (UMP) - your ad choices

UMP is Google's tool for asking consent for ads. When the game starts, it contacts Google to learn whether a consent
message applies where you are and, if so, shows it. Your answer is stored on your phone and passed to the ad SDK.
UMP is part of Google's services, under the same Google policies as above.

### 3.3 GameAnalytics - how the game is played

**What it is for.** To see how players use the game - for example how many come back the next day, where runs end,
how long sessions last, whether the reward offer is taken - so we can fix what does not work and balance the game.

**What it processes.**

- a **random identifier** created for your installation, which lets us count players without knowing who they are
  [[CHECK: and any device identifier (advertising ID, app set ID) the integrated SDK version collects]];
- **technical information about your device**: model, operating system version, the game's version, language;
- your **IP address**, used by GameAnalytics to work out your **approximate location** (country, region, city), then
  kept only in a shortened form, for up to thirty days, for security;
- **events from the game**: sessions; runs started and ended, how far they got and how they ended; the first death and
  the first Anvil strike; the reward offer and your answer; purchases (product, not card details)
  [[CHECK: the final event list sent to GameAnalytics]];
- **error reports** when the game hits an error [[CHECK: only if the SDK's error reporting is switched on]].

**Who decides.** GameAnalytics processes this data **on our behalf** (as a processor); we decide what is collected.
GameAnalytics says it does not use the data to identify you, contact you or advertise to you, and does not sell it.
We see it in GameAnalytics' dashboard, without your name. GameAnalytics ApS is in Denmark; it hosts data with Amazon
Web Services and Google Cloud in the United States. Its privacy notice: <https://www.gameanalytics.com/trust/privacy-notice>.

[[CHECK: whether analytics runs only after consent (GameAnalytics leaves it to the developer to ask "where required")
- see the README.]]

### 3.4 Google Play and Unity IAP - the one purchase

The game sells one thing: **"Skip reward videos"**, a one-time purchase that gives the same x1.5 gold bonus without
the video. Nothing that makes you stronger is sold.

- **Google Play** handles the payment under your Google account and Google Play's terms. We never see your card or
  bank details. The game receives a confirmation of the purchase (the product, an order reference and the time) to
  unlock it, and asks Google Play at start-up which products you own, so the purchase comes back on a new phone with
  the same Google account. (Your **save** does not come back that way; see section 2.)
- **Unity IAP** (Unity Technologies) is the code inside the game that talks to Google Play. Unity's own disclosure for
  it lists, sent to Unity and not shared: approximate location (processed briefly), identifiers, purchase history,
  crash logs and performance diagnostics, and device identifiers, for the purchase to work and for analytics. Unity's
  privacy policy: <https://unity.com/legal/privacy-policy>. [[CHECK: against the Unity IAP version integrated; the
  disclosure above is for Unity IAP 5.4 and later.]]

## 4. Why we use this data, and on what basis

| Purpose | Data | Basis |
|---|---|---|
| Showing the reward video you asked for, and crediting the bonus once | ad SDK data (3.1) | [[CHECK: legal basis, with a lawyer]] |
| Personalised ads, where you have agreed | ad SDK data (3.1) | your consent, which you can withdraw in the game |
| Preventing ad fraud | ad SDK data (3.1) | [[CHECK: legal basis, with a lawyer]] |
| Understanding and improving the game | GameAnalytics data (3.3) | [[CHECK: legal basis - legitimate interest or consent - with a lawyer]] |
| Selling and restoring "Skip reward videos" | purchase data (3.4) | providing what you bought [[CHECK: with a lawyer]] |

Players in Turkey: the **KVKK disclosure** (aydınlatma metni) sets out the same in the form Turkish law asks for:
[[CHECK: link to the published KVKK page]].

## 5. Where the data goes

Google, GameAnalytics and Unity process data outside Turkey, including in the United States and the European Union.
[[CHECK: the transfer safeguard relied on for each, under KVKK article 9 (and the GDPR for EEA players) - with a
lawyer; see the README.]]

## 6. How long it is kept

- **On your phone:** until you delete the game or its storage.
- **GameAnalytics:** event-level data up to twelve months; shortened IP addresses up to thirty days; aggregated
  player-level and daily data up to one year; GameAnalytics may delete sooner. (From its privacy notice, version 4.3 of
  9 November 2026.)
- **Google and Unity:** as their own policies say (links above).
- **Your emails to us:** as long as needed to answer, then deleted [[CHECK: a period, e.g. one year]].

## 7. Security

The game sends nothing to a server of ours, because there is none. The services above encrypt the data they send in
transit (TLS/HTTPS), as their own documentation states. Your save on the phone is protected by Android's app storage,
like any app's files.

## 8. Your rights

You can ask us whether we process data about you; what it is; why, and whether it is used for that purpose; who it
was passed to, in Turkey or abroad; ask for it to be corrected or deleted, and for those it was passed to to be told;
object to a result against you reached only by automated processing; and claim compensation for damage from unlawful
processing. Players in Turkey have these rights under article 11 of the Personal
Data Protection Law No. 6698 (KVKK); the KVKK disclosure explains how to use them. Players in the European Economic
Area, the United Kingdom or Switzerland also have the rights of the GDPR (access, rectification, erasure, restriction,
objection, portability, withdrawal of consent) and may complain to their data protection authority.
[[CHECK: keep the GDPR sentence only if the game is distributed there.]]

**How to ask:** write to {{CONTACT_EMAIL}}. Because we hold no account and no name, we may need your help to find your
data: [[CHECK: how a player identifies their analytics data - e.g. a player ID shown in the game's settings - see the
README]]. We answer within thirty days, free of charge.

**Ads:** you can reset or delete your advertising ID in the phone's settings, and change your ad choices in the game.

## 9. Children

Cryptforge is not directed at children under 13, and we do not knowingly collect data from them.
[[CHECK: match the target audience chosen in Play Console - see the README.]]

## 10. Changes

If this policy changes, the new version is published at {{POLICY_URL}} with its new effective date, and the game links
to it. For a significant change we will also say so in the game.

## 11. Contact

{{DEVELOPER_NAME}}
{{POSTAL_ADDRESS}}
{{CONTACT_EMAIL}}
