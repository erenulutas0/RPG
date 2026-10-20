# Foundry gate door opening

Verified 2026-10-20. Baseline `66a99ba8128cf2a5c64766fb35e84d4bff0b7376` (climb scaling).

When the floor's boss falls (the gate exit opens, `EncounterController.IsGateExitOpen`), the painted gate's two leaves
swing outward onto the landing over a doorway lit from the forge hall beyond, framing the gate exit. Until then nothing
changes: the leaves re-lay the painted door exactly (texture x .3385-.5 / .5-.6641, y .0303-.8828; design v 6.94-5 /
5-3.03, height .20-5.74 on the facade plane u 27.99), over a doorway quad tinted from a low forge glow to darkness. A new
floor shuts the gate at once (it only ever swings open, over `FoundryGateDoorMotion.SwingSeconds` = 1.4 s, to 72 degrees).

- `Scripts/UI/Arena/FoundryRegionGateDoor.cs`: doorway and leaves, built inside the gate's depth structure with the
  facade material.
- `Scripts/UI/Arena/FoundryGateDoorMotion.cs`: borrows the batch mesh; only the leaves' free-edge vertices move; structure
  depth stays authored; shut is exactly the built painting.
- The leaves own a separate batch (order -11.75, the facade's sorting order -10, bounds reaching below the map so it
  draws after the painting). Sharing the facade batch failed: that batch also carries animated flames, and the two
  motions would have rewritten each other's vertices (leaves snapping shut, flames freezing). The scene test caught it.
- Region 38 renderers / 154,832 vertices (+1 renderer, +12 vertices).

## Candidates (actual Unity captures, `gate-open-detail` = same camera as `gate-detail` with the leaves posed open)

- `candidates/01-inward`: leaves swung inward drew over the wall beside the doorway (they belong behind it); doorway
  nearly black.
- `candidates/02-outward-shared-batch`: outward, brighter doorway; still in the flame batch (rejected for the reason above).
- `candidates/03-separate-batch`: own batch; identical runtime to the final After apart from the later test changes.
- Final `after/gate-detail.png` equals Before within 5 pixels (closed gate unchanged); `after/gate-open-detail.png` shows
  the open gate. `validation/comparison.png` puts closed and open side by side.

## Verification

443 .NET / 718 EditMode / 163 PlayMode, zero failures; Verify-Project PASS (656 asset/folder GUIDs, 1099 package GUIDs,
568 scene objects); APK 36,409,937 bytes, SHA256 D156A70A7B6A90CBA1844F18E7085ADDC6C3532D9BAFA4663BEE8D6FFBD02B7D,
NOT INSTALLED. Source freeze `37f5ce97c46bb1aee1eebcdbacbd9c47bfd0230920e7474eaae2b3671983de5b` (1,259 files) matched before
and after the final capture and the full suites. New/changed tests: `FoundryGateDoorTests` (shut leaves equal the
painting, open leaves move only free edges outward and stay on the landing), `GateBossFlowTests` (leaves shut while the
boss stands, fully open after it falls; no door on the hall), `FoundryRegionTests` (one leaf batch; renderer cap 38).

## Not done

No boss intro camera yet; the doorway is a flat glow, not a modelled hall; no sound.
