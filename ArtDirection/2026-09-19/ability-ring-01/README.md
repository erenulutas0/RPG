# Ability range ring — isolated visual proof, 2026-09-19

This is a comparison in the real Gameplay scene, not an integrated runtime change. No authored Assets, scene, gameplay data or device files are modified. Existing movement HUD commit cd10f03 remains the runtime baseline.

## Direction

Compare the existing short point-filtered dashes with a thin continuous contour and an eight-arc seal. Prefer the strengthened eight-arc seal: fewer breaks, a clear interior and four small inward lozenges tie it to the foundry motif without filling the fighting area. The first thin seal was too faint at phone scale; its unchanged capture is retained as seal-thin-rejected.png. Final acceptance still needs movement, cooldown and physical-device review.

This extends the existing code-native geometric indicator. No image model, downloaded texture or new external source was used. Tools/ArtReview/AbilityRingCapture.cs is the source of the deterministic proof geometry. This is not a generated spell illustration or a new attack/telegraph mechanic.

## Geometry and rendering

- 512×256 RGBA32, bilinear, clamp, no mipmaps. Each proof texture is 524,288 bytes of raw GPU pixel payload, excluding engine overhead. Two temporary alternatives are created once per capture test and destroyed on teardown; this is not a shipping allocation/lifetime decision.
- Ellipse centre at the canvas centre, ideal horizontal radius 240 texels and vertical radius 120 texels. Pixels-per-unit = 240 / the actual AbilityRuntime radius. The visible contour's centreline therefore preserves the exact world-space reach; its antialiased stroke has finite thickness around that boundary. Transparent padding does not represent range.
- The centre stays clear. Long arcs use eight small angular breaks, four inward lozenges and a narrow dark supporting edge. Seal core coverage is clamp01(2.1 - distance), with colour (0.63, 0.84, 1); continuous core is clamp01(1.35 - distance), colour (0.43, 0.76, 1). Distance is locally gradient-corrected after the 2:1 projection.
- Initial rejected seal used the continuous candidate's thinner core and darker colour. The retained rejected PNG records that iteration; reapply those constants to reproduce it.
- All alternatives borrow the existing ring renderer, hero root and independent ground sorting. Baseline colour is untouched; candidate ready alpha is held at 0.65 and cooling at 0.22 for comparison. These are frozen visual samples, not cooldown timing verification. No damage is fired to create the cooling image.
- At the rim the ring can extend past the platform, just like the existing marker. The proof does not add masking or change target eligibility. The rim capture moves only the hero in a frozen scene; it is not a movement or spacing simulation.

## Evidence and reproduction

Eight PNGs: current baseline, continuous ready, final seal ready/cooling, both candidates at 1080×1920, seal rim at 1080×2340, and retained rejected thin seal. Each final comparison comes from Camera.Render in an isolated profile with the ten-enemy development room frozen, then lossless BMP-to-PNG conversion. Gallery shows the actual stills. Short/aspect and rim views have different framing; they are not pixel-aligned A/B comparisons to the portrait baseline.

```powershell
pwsh -File Tools/ArtReview/Run-AbilityRingProof.ps1
pwsh -File Tools/Verify-Project.ps1
```

The runner temporarily stages the capture test and its unique meta under PlayMode, runs pure tests and compile checks, performs the graphics-enabled test, then removes only its own staged files. Gameplay scene and assets are never saved. PNG hashes are in sha256.txt.

No new APK was built. Latest built movement-HUD APK remains SHA-256 97A5F7511F0968E43AAF785BAAC2F0B8D654F91905CE4C858DECBACA3175A7AA, not installed by Astra. ADB enumeration detected the attached S23 this turn, but the device-use coordination question remains unanswered: no input, screencap, install, app launch, profile or telemetry operation. Latest recorded installed build remains D77FFFA6D385BBB017F9EBCBA8C9D6A7271250F5318E4021A8FD9F7BFA6DEED2 (Docs/23); inspect fresh before future work.

## Next bounded slice

Integrate the preferred contour with bounded shared texture ownership, exact radius scaling, existing ready/cooling behaviour, reload/pause checks and no per-frame texture generation. Validate during movement and real ability use, then full gate and coordinated phone review. Do not import the proof fixture's per-test construction as a per-enemy or per-wave allocation path. Boss danger telegraphs and cast impact effects remain separate future work.

Verified: .NET 291/291, compile 0 warnings/errors, graphics PlayMode capture 1/1, integrity 439 GUIDs / 568 scene objects/components. No runtime Assets change; full EditMode/PlayMode and APK build were not repeated. The previous movement-HUD full gate (306/108) and APK remain valid as the unchanged runtime baseline, not a test of an integrated new ring.
