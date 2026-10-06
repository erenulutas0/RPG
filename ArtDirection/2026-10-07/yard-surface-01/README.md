# Yard surface hierarchy — 2026-10-07

## Scope

A bounded material-value pass in FoundryRegionFinish.FloorLight. Extend stock-area soot from a 2-unit to a 3.4-unit apron, use a broader deposit field and a maximum 30% attenuation. Preserve the original pit deposit independently. Add a smooth 24%-maximum service deposit around the existing boiler plinths. Strengthen the existing gate-approach wear lift from 4.5% to 11%, confined to its authored lane and fading at both ends. All changes apply at mesh build time through existing vertex colours; no new textures, materials, colliders or update work.

This is a restrained surface change, not a map redesign or achievement of the concept target. The first weaker candidate was discarded after portrait comparison. Central court stays substantially unchanged; the strongest visible differences are on boiler-side flags and the gate approach. Fire hue, contact shading and brass lines remain.

## Evidence

Before captures are the previous accepted FoundryRuntimeCapture output; after captures use the same test, route, camera and HUD. Both use isolated TestProfile data. The comparison page provides the workshop/pit, central court, boiler lane, gate front and overview. comparison.json contains whole-frame absolute RGB differences (not a perceptual quality score); whole-frame means are small because only localized floor surfaces changed.

Final graphics capture passed (1). Static renderers remain 17; adaptive light sampling changes vertex count from 80,316 to 80,344 (+28). Initial candidate had 80,566, but was superseded. No screenshot retouching. Full test/build/device closure is recorded below.


## Validation closure

Final graphics 1 passed; EditMode 410/410, PlayMode 143/143, Android exit 0. Integrity 536 asset/folder GUIDs, 1099 package GUIDs, 568 scene objects/components. Installed APK SHA256 CAC7489FB29F0AF91781D85731B3BC154B31236154B2DF3D01E8B8C85E2B3170, 35,934,327 bytes; installed base.apk matched.

Guarded S23 development depth-proof review: start, boiler lane and gate lane captured after two owner-authorized drags. Surface transitions and actor contrast reviewed; this is a staged movement/appearance check, not a new combat or FPS benchmark. No managed exception/crash pattern in captured current-process log. Profile and backup plus all three original development files (including the prior gate-arrival opt-in) SHA256-identical. Temporary depth-proof flag removed, app stopped; ordinary next launch retains the previous skippable entry presentation. Telemetry test records retained; no profile restore. Device backup/evidence: TestResults/yard-device-20261007. No push.
