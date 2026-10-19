# Foundry gate crown stone study

Verified 2026-10-19. Baseline `3ce262627236c71165377404f674bd5cdaa2b933` (trolley package).

Gate crown/header stone study. Diagnosis from actual captures and the source atlas: the mottled native crown was dominated by atlas sampling, not face lighting or subdivision. The shared gate patch (U .04-.21) brightens toward its arris (column means range 9.3 levels); mirrored over the long crown that ramp printed as cloudy bands, and the native stone sat more violet than the baked slate bays. The shared GateHouseGrain field (header, crown, lower cornice, inner returns, cartouche, curtain wings, landing shoulders) now samples the level interior right of the authored drip (U .272-.43, same V, density and fold construction; column range 4.9, 16-texel block spread 3.58 vs 4.64), and one constant albedo balance (r x1.015, g x1.035, b x.95; channel-mean value within 0.5 %) moves it to the bays' slate hue while keeping every relative forge-light value. The inner-pier .11 field and its source light are untouched; painter order, depth owners and batches are unchanged. Measured on clouded crown samples: broad cloud -13 to -31 % and fine grain -10 to -27 % (one already calm shoulder sample stays within noise, 3.0 -> 3.4 / 2.4 -> 2.6); tops read 6-13 % darker, never brighter; blue-minus-red 17-18 -> 11-13 levels against the bays' 12-15. Bounded: the atlas's fine cellular grain remains (still finer than the baked bays), faint mirror blotches survive on the cartouche, and this is not reference parity.

Region 36 renderers / 172160 vertices versus 171940: +220 from the narrower patch's extra mirror folds. Distant 3572 included and unchanged. Ceilings 174000 / 3600; headroom 1840 / 28. No new bitmap, material, shader, batch or updater. Static counts do not establish device performance.

## Verification

438 .NET / 704 EditMode / 160 PlayMode, zero failures/skips. Focused Foundry EditMode 302/302 (three new crown-finish tests; the surface and inner-pier field oracles now read the shared patch). Isolated compile 0 warnings/0 errors. Fresh Before (24 views, baseline 3ce2626, clean Assets) and final After Android-target graphics captures passed. PASS: 640 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components. Android build exit 0. APK 36,387,952 bytes; SHA256 405D15BE7118022258D85EDDD419848681BEB0350029D29B124E765F9597DF39; NOT INSTALLED.

All-Assets source freeze `ad9e61f274f228c5c16ac0b1d4d14de9044ae3c537482180898a3cbf3e45038a` (1227 files including tests/metas) was taken before the final After capture and
matched after it and after the complete suites and APK build.

## Candidates

- `candidates/01-level-patch`: patch change only. Calmer and darker, but the crown kept its violet cast against the
  baked bays. Retained as an intermediate, not accepted alone.
- `candidates/02-slate-balance`: patch plus albedo balance; identical runtime to the final After (captured before the
  tests were written and the freeze taken; at most 136 animation pixels differ).

## Images

`index.html` compares Before / intermediate / After / reference across 24 matched actual Unity views (identical cameras
and actor staging, paused combat, hidden HUD). `gate-header-close` is new for this package. Changed pixels are confined
to the gate in every gate, crown, full-map and overview view; other views differ only by actor/spark animation
(`validation/actual-image-review.md`).

## Evidence boundary

Bounded native gate-stone material consistency. The fine atlas grain is intrinsic at this texel density; removing it
would need a new or re-baked stone source, which this study did not create. Inner piers keep their accepted separate
field. Map, camera, navigation, collision, gameplay, encounters, energy deferral and saves unchanged. No phone
operation, install, save or telemetry change; the restart/install decision remains pending. Crowd congestion remains
open.

## Reproduction

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/Run-UnityTests.ps1 -Platform EditMode -Filter Foundry -SkipApk
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryGateCrownProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Runtime source: `Assets/_Project/Scripts/UI/Arena/FoundryRegionGateSurface.cs`. Tests:
`Assets/_Project/Tests/EditMode/FoundryGateCrownFinishTests.cs`, `FoundryGateSurfaceTests.cs`,
`FoundryGateInnerPierTests.cs`. Capture: `Tools/ArtReview/FoundryGateCrownCapture.cs`, `Run-FoundryGateCrownProof.ps1`.
