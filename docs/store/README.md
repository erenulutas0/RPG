# docs/store - the store and policy texts (drafts, 2026-11-19)

Drafts of every text the owner publishes for the Turkish release (docs/59 section 3 item 6): the privacy policy, the
KVKK disclosure, the Play forms' answers, the store listings and the reviewers' notes. They follow docs/58 (model B),
docs/59 (the release path), docs/60 (the Turkish style and glossary), docs/55 section 9 (the local arena), docs/57
section 12 (two-handed play) and the owner's two reports in `docs/research/`.

**None of this is legal advice.** Where the law decides, the texts say so with a `[[CHECK: ...]]` (English) or
`[[KONTROL: ...]]` (Turkish) marker and name the source; those points go to a lawyer or an SMMM before publishing.
Nothing here invents a legal fact, an email, an address or a name.

## The files

| File | What it is | Where it goes |
|---|---|---|
| `privacy-policy-tr.md` | The privacy policy in Turkish: AdMob, UMP, GameAnalytics, Google Play and Unity IAP, what each processes; the local save; retention; rights and contact | An HTML page at {{POLICY_URL}} (Turkish version); linked from Play Console and from inside the game |
| `privacy-policy-en.md` | The same policy in English | The same page's English version |
| `kvkk-aydinlatma-tr.md` | The KVKK disclosure (aydınlatma metni, KVKK m.10), **separate from any consent**: no "I accept" under it; consent for personalised ads, if needed, is asked on its own screen | Its own page, linked from the policy and the game |
| `data-safety-answers.md` | The Play Data safety form answer by answer, from each SDK's published disclosure, with the other App content declarations that must agree (Ads, Advertising ID, App access, target audience) | Play Console > App content |
| `iarc-answers.md` | The content rating questionnaire answered from the game's actual content, with the expected rating (PEGI 7 in Turkey, most likely) and why | Play Console > App content > Content rating |
| `store-listing-tr.md` | The Turkish listing: name, short and full description with counts, the in-app product's text, the screenshot plan, the feature graphic brief, keywords, category, contact details | Play Console > Store listing (default language, Turkish) |
| `store-listing-en.md` | The English listing | Store listing > translations > English |
| `reviewer-notes.md` | How a Play or AdMob reviewer reaches the reward offer (from the second completed run), the purchase, the offline game, the ad placement | Play Console > App access; AdMob's review |

## The placeholders the owner must fill

These appear as `{{NAME}}` in the files. Fill them in every file at once; never publish a text that still contains
`{{` or `[[` (`grep -rn "{{\|\[\[" docs/store` lists what is left; this README is not published).

| Placeholder | What | Notes |
|---|---|---|
| `{{DEVELOPER_NAME}}` | The developer's name | In the policy and the KVKK text this is the **data controller**, so the owner's legal name (a real person, no company). The store's developer name may be different; if so, write e.g. "Legal Name (Store Name)" in the policy. [[CHECK with a lawyer]] |
| `{{CONTACT_EMAIL}}` | A real, monitored email address | Play shows it publicly and requires it; the policy and the KVKK text name it as the channel for requests. Never invented here |
| `{{POSTAL_ADDRESS}}` | The owner's address | A personal Play account that earns money shows its full address on the store page (docs/59 decision 3). The KVKK text names the controller's address. Whether a different correspondence address is allowed: [[CHECK with a lawyer]] |
| `{{POLICY_URL}}` | The public URL of the privacy policy page | HTML, public, not geofenced, not a PDF (Play's User Data policy). The site itself is decided later (docs/59 decision 4) |
| `{{WEBSITE_URL}}` | The developer website's root (the domain of the policy page) | Play's "Website" field; AdMob reads `{{WEBSITE_URL}}/app-ads.txt` |
| `{{EFFECTIVE_DATE}}` | The date the policy goes live | Also the KVKK text's "last updated" |

## Open questions for the owner

1. **Ad consent for players in Turkey.** Google's consent tool (UMP) has messages for the EEA, the UK and Switzerland
   and for US states, and nothing for Turkey ([AdMob message types](https://support.google.com/admob/answer/10114020)).
   So by default a Turkish player would see no consent question and get personalised ads. The owner's report says
   personalisation should rest on consent the player can refuse and withdraw. The choices: **(a)** show the European
   consent message to everyone (AdMob allows "Everywhere" targeting for it), **(b)** request non-personalised ads for
   players outside those regions, or **(c)** personalised ads on another legal basis. (a) or (b) look safer; a lawyer
   decides. The policy's section 3.1 and the KVKK text's section 4 are written to change with the choice.
2. **Analytics consent.** Does GameAnalytics run from the first launch (legitimate interest) or only after the player
   agrees? GameAnalytics leaves it to the developer to ask "where required". Asking costs data: players who decline
   are missing from the D1/D7/D30 numbers docs/59 relies on. A lawyer decides.
3. **Sending data abroad (KVKK article 9).** Google, GameAnalytics (servers in the US) and Unity process data outside
   Turkey. Since 1 June 2024 that needs an adequacy decision or a safeguard such as the Board's standard contract
   (signed, then notified to the KVKK Board within five working days); KVKK's own guidance says the "occasional
   transfer" exceptions, consent included, do not cover regular transfers like an SDK's
   ([KVKK, transfers abroad](https://www.kvkk.gov.tr/Icerik/2053/Yurtdisina-Aktarim)). Ask the lawyer which safeguard
   applies to each provider, and ask GameAnalytics whether it signs the KVKK standard contract.
4. **VERBİS.** Whether the owner must register in the Data Controllers' Registry or is exempt: ask the SMMM or the
   lawyer (KVKK article 16 lets the Board grant exemptions; the current criteria are not checked here).
5. **Deletion requests.** If the game shows a player ID in the settings, a player can ask us to delete their analytics
   data and we can pass it to GameAnalytics; then the Data safety form can answer "Yes, users can request deletion".
   Without it, the answer is "No". Worth the small feature?
6. **Target audience.** The report suggests 13-15, 16-17 and 18+. Choosing any group under 13 brings in Google's
   Families policy and child-safe ad settings. The policy's "Children" section follows this choice. (The content
   rating, likely PEGI 7, is a separate thing.)
7. **The names on the page:** the store developer name, the contact email, the address and the website.
8. **The tone of the legal texts.** They use "sen", as docs/60 asks for the game. A lawyer may prefer the formal
   "siz" for the policy and the KVKK text; that is a find-and-replace pass, done for the whole text at once.
9. **The listing's name and category:** "Cryptforge" alone (recommended) or "Cryptforge: Sürü ve Örs"; category
   Action (recommended) or Role Playing.
10. **Glossary words the listing depends on** (docs/60 section 9, still open): "sefer" for a run, and "Cinder
   Arcanist" kept as a name. The listing uses both; change them there if the owner's answer differs.

## What the lead needs to settle in the build (the texts depend on it)

- **Inside the game:** a "Privacy options" entry (UMP requires one where its message applies), a link to the privacy
  policy (Play's User Data policy wants it "within the app") and to the KVKK text, e.g. on the pause overlay beside the
  language toggle.
- **When the ad SDK starts:** at launch, or only when an offer is about to show; and whether it starts at all for owners
  of "Skip reward videos". The policy's section 3.1 says which.
- **The offer's details** for reviewer-notes.md: what counts as a "completed run" (a fall, an extract and a full clear?),
  the button's labels, where the store entry sits, and the measured time of a quick run.
- **The release manifest:** `android:allowBackup` (unset in today's prototype APK, so Android backs the app's files up
  by default if the player has backup on; that would also restore a save on a new phone, which docs/58's "restoring a
  purchase is separate from restoring a save" did not expect), and the `AD_ID` permission the ad SDK adds.
- **Unity's hardware statistics:** `submitAnalytics: 1` in `ProjectSettings.asset`; switch it off for the release if
  the editor allows, and confirm with a network capture that nothing goes to Unity except Unity IAP.
- **GameAnalytics:** which identifiers the integrated SDK reads, whether its error reporting is on, the final event
  list, and whether the local telemetry log stays in release builds (the policy mentions it).
- **SDK versions:** re-read Google's and Unity's data disclosures for the versions actually integrated
  (data-safety-answers.md section 5).

## The lead's answers (2026-11-19)

The owner delegated gameplay and engineering calls to the lead. The legal ones stay with the owner and their lawyer or
SMMM.

- **Target audience: 13 and over.** The game is not directed at children, so Google's Families policy does not apply.
- **Title and category:** "Cryptforge" alone, in Action. The owner may change either.
- **Tone:** the texts keep "sen", like the game. A lawyer may ask for "siz" in the legal texts.
- **When the ad SDK starts:**
  - not at launch: after the first completed run, because the offer appears from the second;
  - consent (UMP) is gathered at that point, before any ad request;
  - never for owners of "Skip reward videos".
- **Android backup stays on** for the save. We have no cloud save, so a new phone keeps the player's progress.
  - The entitlement cache is excluded from the backup; Play is queried again.
  - The reward ledger travels with the save, so a restored save cannot be credited twice.
  - Built with backup rules in the release pipeline; the privacy policy already names Android's backup.
- **A player ID in the settings:** yes. A random install ID, shown in the settings, lets a player ask for deletion, so
  the Play form can answer "Yes" to deletion requests. Built in the settings pass.
- **Unity's hardware statistics** are turned off in the release build if the licence allows it. If not, they are
  declared honestly in the Data safety form. The release pipeline checks this.
- **Until a lawyer answers the consent questions**, the engineering default is the conservative one, behind a switch
  that is easy to flip:
  - non-personalised ads in Turkey;
  - GameAnalytics starts after a first-launch notice that offers an opt-out.
  The owner's questions 1-4 (consent in Turkey, analytics consent, transfers abroad, VERBİS) stay open for the lawyer
  or SMMM.

## Before publishing (checklist)

1. Fill the six placeholders everywhere.
2. Resolve every `[[CHECK]]` / `[[KONTROL]]` (the lawyer's and the lead's answers above) and delete the markers.
3. Keep `privacy-policy-tr.md` and `privacy-policy-en.md` saying the same thing.
4. Publish the policy and the KVKK text as HTML pages; put `app-ads.txt` at the site's root.
5. Enter the policy URL in Play Console and link both pages from inside the game.
6. Fill the Data safety form and the other App content declarations from `data-safety-answers.md`, then the rating
   questionnaire from `iarc-answers.md`.
7. Take the screenshots from a release build (store-listing-tr.md section 5).
8. Re-check `iarc-answers.md` section 4 and the listings' optional paragraphs (skills, arena) against what actually
   ships.

## The owner's reports: what is out of date, and what is new since

- **"Tek elle"** in the release report's draft store line: the game is two-handed (docs/57 section 12; docs/59
  already noted it). Corrected in the listing.
- **"Koşu" for a run** in both reports and in the product promise ("Aynı koşu sonu bonusunu video izlemeden al"):
  docs/60's glossary uses "sefer" ("koşu reads as jogging"), so the texts say "sefer sonu bonusu".
- **The Google Mobile Ads SDK version.** The report measured `play-services-ads` 24.7.0's size; Google's data
  disclosure now describes **25.5.0** as the latest. Both the size check (docs/59 section 3 item 5) and the disclosure
  should be redone on the version actually integrated.
- **New since the report: the KVKK Board's principle decision 2026/347** (18 February 2026, Official Gazette of
  24 March 2026, no. 33203): the disclosure and the consent text must be separate, not nested, not accepted with one
  tick; no "I read and accept" under a disclosure; and no consent asked when another legal basis applies. It agrees
  with the report's "aydınlatma ile açık rızayı ayır" and makes it stricter. (Read through a secondary source,
  alomaliye.com; the lawyer checks the Gazette text.)
- **New since the report: PEGI's interactive-risk criteria** (announced 12 March 2026, for games submitted from June
  2026): paid random items default to PEGI 16, time- or quantity-limited offers to PEGI 12, unrestricted chat to
  PEGI 18. Model B triggers none of them, but they bound what can be sold later (iarc-answers.md section 4).
- **Unity IAP's own disclosure** (IAP 5.4 and later) lists more than the report's general "Unity services": approximate
  location, identifiers, purchase history, crash logs and diagnostics, sent to Unity. The texts declare them.
- **GameAnalytics' privacy notice** (version 4.3, 9 November 2026), not cited in the reports: GameAnalytics is a
  processor, keeps event data up to twelve months and shortened IPs up to thirty days, and hosts in the US (Amazon Web
  Services, Google Cloud), which makes analytics a transfer abroad (question 3).
- Checked and still current: the report's KVKK transfer page (updated for the 2024 amendment in force since
  1 June 2024), Google's CMP requirement for the EEA, the UK and Switzerland, and Play's Data safety rules. Play lists
  Turkey under PEGI, which the report's "the rating is produced per region" fits.

## Sources (read 2026-11-19)

- Google Mobile Ads SDK, Play data disclosure: <https://developers.google.com/admob/android/privacy/play-data-disclosure>
- Unity IAP, Google Play data safety: <https://docs.unity.com/en-us/iap/privacy-and-consent/google-play-data-safety>
- GameAnalytics privacy notice: <https://www.gameanalytics.com/trust/privacy-notice>
- AdMob privacy message types: <https://support.google.com/admob/answer/10114020>
- Google Play: Data safety <https://support.google.com/googleplay/android-developer/answer/10787469>; User Data policy
  <https://support.google.com/googleplay/android-developer/answer/10144311>; account deletion
  <https://support.google.com/googleplay/android-developer/answer/13327111>; content ratings
  <https://support.google.com/googleplay/android-developer/answer/9898843> and
  <https://support.google.com/googleplay/android-developer/answer/188189>; store listing
  <https://support.google.com/googleplay/android-developer/answer/9859152>; preview assets
  <https://support.google.com/googleplay/android-developer/answer/9866151>
- PEGI: <https://pegi.info/what-do-the-labels-mean>; <https://pegi.info/news/pegi-expands-age-rating-criteria-interactive-risk-categories>
- Law No. 6698 (KVKK), current text: <https://www.mevzuat.gov.tr/mevzuatmetin/1.5.6698.pdf>
- KVKK, transfers abroad: <https://www.kvkk.gov.tr/Icerik/2053/Yurtdisina-Aktarim>
- KVKK Board decision 2026/347 (secondary source): <https://www.alomaliye.com/2026/03/24/kvkkdan-yeni-ilke-karari-acik-riza-metni-ile-aydinlatma-metni-artik-ayri-duzenlenmeli/>
- The owner's reports: `docs/research/2026-11-18_release_path_owner_report.md`,
  `docs/research/2026-11-18_monetisation_and_arena_owner_report.md`
