# Foundry static Unity layout — 2026-09-23

Owner approved the foundry-layout-01 direction and requested matching art. Opus remains on hold.

Tools/ArtReview/FoundryLayoutCapture.cs builds the exact seven-rectangle union from the approved diagram in an isolated Gameplay test. 952 connected unit cells; optional side recess remains absent. Existing floor, wall and coping materials are borrowed. Three generated meshes are disposed with the temporary scene. No authored scene, gameplay geometry, navigation or collider change.

overview.png is a 1600x1100 whole-layout render. zone-0..4.png are actual 1080x2340 Unity renders at fixed 9-unit camera width. The hero is repositioned for each static view; HUD, encounters and movement input are disabled only in the fixture. These captures are NOT traversability or device evidence. Empty exterior and absent environment kit are intentional unfinished art.

An initial pair of furnaces lacked supporting foundations; removed before final capture. Props will require dedicated supports outside movement clearance. The generated gate remains source-only and is not present in these Unity captures.

Verification: Run-FoundryLayoutProof.ps1 final graphics test 1/1 passed, including connected-cell and open-recess assertions. No full game suite/APK/device operation for this isolated art-tool slice. Prior installed C8 build and uninstalled furnace-clearance correction remain unchanged.

Next: clean gate alpha/perspective and registered Unity sample; supported perimeter modules; then coordinate concave traversability separately. Do not ship this surface over the current convex collision.
