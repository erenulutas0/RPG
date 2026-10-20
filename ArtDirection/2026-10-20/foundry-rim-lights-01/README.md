# Foundry rim lanterns

Verified 2026-10-20. Baseline `2c3c4082181d5ac301698e0c8501bc8c98c99e80` (court inlay package). Target: the approved concept
`ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

Rim lantern density toward the concept, which lines its rim with fire every few units. Three balustrade lantern posts fill the longest bare straight rail runs: south rail at u -7.5 and 6.5 (camera-facing, with pilasters) and north rail at u 1.5. They reuse RailLanterns, so BuildBalustrade's placement tests, the post masonry, the lantern ironwork/panes and a matching ForgeLamps floor pool all follow from one declaration. Requested west (beside the workyard) and causeway (between the boilers) posts were rejected by the existing placement tests at two positions each; a north-west post placed but stood in front of the baked stock station and was removed.

Region 37 renderers / 154820 vertices versus 154393: +427 (three posts). No new batch, material or texture. Distant 3572 unchanged. Ceilings 174000 / 3600; headroom 19180 / 28. Static counts do not establish device performance.

## Verification

438 .NET / 710 EditMode / 160 PlayMode, zero failures/skips. One new test (declared posts, camera-facing flag, placement proven by the floor pool, outside the walking surface, >3.5 units from any other flame pool). Isolated compile 0/0. Fresh Before (25 views, baseline 2c3c408, clean Assets) and final After Android-target captures passed. PASS: 647 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components. Android build exit 0. APK 36,392,292 bytes; SHA256 C71EE7B839309CD352A2659E0C92055D50B7129A7602A443F0B3133C9D3663B6; NOT INSTALLED.

All-Assets source freeze `847684efae5d0360d82180c4b81e565665db45f09990ac54732d4b5b789e49fa` (1241 files including tests/metas) was taken before the final After capture and
matched after it and after the complete suites and APK build.

## Candidates (actual Unity captures)

- `candidates/01-six-requested`: six declared posts; three placed (south x2, north u 1.5).
- `candidates/02-relocated`: west and causeway moved, north-west added; only the north-west post placed.
- `candidates/03-with-northwest`: the north-west post overlapped the baked stock station in the map view; removed.
- Final After: the three accepted posts.

## Evidence boundary

A modest density step; the concept's rim still carries more fire. The west rail and the causeway have no free post
position under the existing placement rules. Lanterns, not new brazier types. No gameplay, navigation or save change.
No phone operation; APK not installed.

## Reproduction

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryRimLightsProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Runtime: `RailLanterns` in `Assets/_Project/Scripts/UI/Arena/FoundryRegionMassing.cs`. Test:
`Assets/_Project/Tests/EditMode/FoundryRimLightTests.cs`.
