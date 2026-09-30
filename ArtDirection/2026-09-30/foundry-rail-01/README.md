# Foundry stone balustrade, review fixes, depth proof with effects — 2026-09-30

Three helper agents (a read-only code review, the balustrade, the depth-proof extension) plus fixes by the main
session. Matched actual Unity captures: `../foundry-fx-light-01/after/` (before) and `after/`.

## What changed

- **Balustrade** (`FoundryRegionMassing.cs`). The rim and pit-collar rail read as a castle wall with arrow slits at the
  gameplay camera (item P2 in `../painted-surfaces-02/PROMPTS.md`). It is now a stone balustrade: a 0.15 plinth with a
  lit ledge, turned vase balusters 0.15-0.57 (about 0.2 wide at the belly, 0.14-0.22 clear gaps, 4-7 per span between
  posts, 255 in all), and a 0.57-0.72 handrail with a lit top edge; posts and lanterns unchanged. Gaps have no backing,
  so the depth pass writes nothing there and actors behind show between balusters; the pit glow shows through the
  collar. Same batches/orders (17 renderers); 56,404 -> 59,008 vertices. This may make the painted P2 trim - and its
  extra renderer - unnecessary; the owner decides after seeing it on the phone.
- **Review fixes** (`FoundryVisibilityView.cs`), from a read-only review that found no high-severity bug:
  a camera rendered by hand in a later frame than the last placement re-applies it (frame stamp) instead of reusing
  a stale one; both restores run newest-first, so a transform/body registered twice returns to its true original;
  `ForStructure(null)` returns null (pink, as before) instead of aborting the region build; the header now states that
  the moved z persists through frames in which no camera renders (batch `-nographics` tests), which nothing in
  gameplay reads.
- **Depth proof shows effects** (`DepthProofArrangement.cs`, development flag only): every 0.3 s the hidden Grunt and a
  Mite placed in the open court in front take 0.001 damage and are healed straight back, through the real hit path, so
  a hidden and a visible hit spark appear side by side. Nothing dies, nothing is banked or saved.

## Budget and gate

- Static budget: 17 renderers (limit unchanged), 59,008 vertices.
- Full gate: EditMode 410/410, PlayMode 134/134, Android build exit 0; integrity 524 project GUIDs (+4 metas of the
  development tools and their tests) / 1,099 / 568 passed.
- APK 37,011,610 bytes, SHA-256 `14612D369B9334EB6F479D54F916A546343697A1A006A21BAB07A0F172462A29`, **installed** on the
  S23 (hash matches; the game was not running - the owner was in another app, which stayed in front; profile revision
  221 unchanged; backups in `TestResults/foundry-rail-install-20260930`). Viewed afterwards with the depth-proof flag (`device-proof-sparks.png`, `device-proof.png`): the balustrade reads with its balusters on the phone; hit flashes and sparks show on the Grunt's uncovered part and on the Mite in the open court, while nothing is drawn over the boiler where it covers the Grunt - only its silhouette. Flag removed and the app stopped afterwards; profile unchanged; logcat clean.

## Limits

- Known from the review, not changed: a hidden enemy's silhouette can draw over a nearer actor just in front of a long
  sloped wall (the actors' own screen-y order); the court finials are emitted before the rail in the same batch, so the
  first span after a finial paints over the finial's lower front (pre-existing, now more visible).
- The balustrade is thinner (0.24 deep instead of ~0.42); the one open run end at the bridge stub shows it.
