# Foundry art packages on the S23 — 2026-09-30

Installed build: `Builds/Android/Cryptforge-Prototype.apk`, 39,339,430 bytes, SHA-256
`CB66934314CD7CC7079E857236F752525A2CC28BE2F9D72ABA0118C357E339CE` (installed base.apk hash matches). It contains every
package of the day since the silhouette build: rim trim, parallel pass (seal, bridge, chains, cylinders, piers, loot
silhouettes, gate texture), chest depth and forge light, and the forged UI panel import.

## Installation

- Owner said the phone was free. Fresh check: awake, no keyguard, game in the foreground. Backups of profile, backup,
  development flags and telemetry in `TestResults/foundry-final-install-20260930` (profile revision 218, 33,082 gold).
- `pre-install-owner-run.png`: the screenshot taken just before installing showed an open run (Floor 3, level 12,
  303 unbanked gold, level-up choice open). The installation ended that run; its unbanked gold was lost. The saved
  profile was not affected: profile and backup hashes identical before and after, and after the checks below.

## What was seen on the phone

- `device-start.png`: the court start — the brass star seal and ring pair, the compass, warm light towards the furnace
  pit on the left and a cooler open court, clean HUD panels.
- `device-panel-defeat.png`: the defeat modal with the forged panel at 1024 ASTC 4x4; zoomed, the gold frame lines are
  continuous with no visible block artefacts.
- `device-boiler-corner.png`: the south-west boiler and balustrade corner; Mites and the hero draw correctly around it.
- Two short runs (Try again once), both lost to a Tank in Slag Gate (room 3) within about 30 s, banking 0 gold.
- Logcat: 0 matches for FATAL EXCEPTION / NullReferenceException / MissingReferenceException /
  InvalidOperationException / shader error. App sent Home afterwards.

## Not seen on the phone

- No enemy, drop or chest passed behind a structure in the captures, so the x-ray silhouettes are still verified
  only in Unity. On the phone, open-court enemies show no false silhouette.
- No performance measurement.
