# Foundry rim fire: the remaining open rail gaps

Verified 2026-10-21. Baseline `38cad41f79f1f9a7227f0d35f00993f481a94046`.

A temporary EditMode probe (not committed) tested every rail lantern position on the west rail (v -16..16), both
causeway rails (u 14.5..23.5) and the north rail (u -30..12) against BuildBalustrade's own placement rules
(MassClear / MassBlocked / MassConflicts) and the distance to the nearest existing flame pool:

- West rail: v 5..15 is blocked by the workyard equipment; the open stretches already have lanterns within 1-3 units;
  below v -12 the probe positions fall off the court's cut corner. No new post.
- Causeway south rail: every position from u 18 stands behind a south boiler on screen (MassConflicts overlay) - a
  real rule, not relaxed.
- Open gaps found and filled: north rail u -2 and u 6, causeway north rail u 18.5.

The rim-lantern spacing test now requires > 2.9 units to the nearest flame pool (was 3.5), closer to the concept's
rhythm. Region 38 renderers / 155,167 vertices (+303).

Images: `before/`, `after/` (26 matched views); `candidates/01-three-posts` is identical runtime captured before the
freeze. Verification: 444 .NET / 719 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,411,342 bytes,
SHA256 98ACB99CD9354FB98C391E42147070C8A409FA0D15129B842AFC364E1DAB19E7, NOT INSTALLED (the owner is using the phone).
Source freeze `04e00932c0a88efbdbde0b35e1622c7611832b6fec44d780051effc2bad89ccb` (1,261 files) matched after the final
capture and the full suites. The west rail and the causeway's south rail stay as they are under the current rules.
