# Foundry polish: dark iron pipework and a wider gate hold

Verified 2026-10-21. Baseline `fd06b2f4a0917007957626fccbbc5be994213f92`.

- Pipework (`FinishedPipeTone`): the rolled service mains and boiler feeds move from the former cool blue stock
  (value x .86/.98/1.16), which read pale teal beside the re-rendered blackened towers, to dark neutral-warm iron
  (x .66/.64/.63); the elevated mains' intermediate collars become brass bands (x .96/.70/.36), like the towers'. End
  flanges stay iron. MetalTone still owns reflected forge light. Texture-free, geometry unchanged.
- Gate hold (`ExitRule.GateRadius` 1.3, finale keeps 0.9): on the phone the 0.9 hold took several nudges to land on by
  touch at the gate's camera distance. `ExitSealView` scales the seal plate to the open exit's radius
  (`EncounterController.ExitRadius`).

Images: `before/` (baseline) and `after/` (final), 26 matched views; `candidates/01-dark-pipes` is identical runtime
captured before the freeze. Region 38 renderers / 154,832 vertices (unchanged).

Verification: 444 .NET / 719 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,410,838 bytes, SHA256
DBBB5ADFB09BDA3774590EC991B96A40EF782D07CCDD7A5367A8D252EA5385A2, NOT INSTALLED. Source freeze
`1b5112b5687938d70258844e885a30a5ff4082d7c5c7457589c3372b1c0357f9` (1,261 files) matched after the final capture and
the full suites. GateBossFlowTests now holds the gate 1.1 units off its centre.
