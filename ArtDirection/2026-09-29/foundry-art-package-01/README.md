# Foundry combined art package — 2026-09-29

## Scope

This pass develops the existing playable foundry toward the approved architectural reference. It combines the following presentation changes:

- Quieter courtyard paving: narrower joints, softer bevel contrast, restrained deterministic stone variation and broad surface patina. Furnace floor bounce is broader and less concentrated.
- Gate and boiler hardware: inset iron panels, retaining collars, bronze fixings, tower terminals and soot-dark inspection slots. Existing door and fire-window artwork is retained.
- Workshop dressing: a west service shelf with ingots and a strapped parts bin; a north shelf with pipe stock, saddles, straps and tongs. These are environmental props outside the walking union.
- A translucent engraved court seal replaces the thin ring sketch; iron brazier cups support brighter orange flame cores. Both were attenuated/refined after actual portrait review.

The prior crane, shaft, bridge, undercroft and atmosphere remain part of the scene reviewed here. They are context for the combined composition, not all newly authored in this pass. This package does not claim that the reference's final visual quality has been reached.

Detailed contribution records:

- [Architecture](architecture-notes.md)
- [Floor finish](floor-notes.md)
- [Workshop](workshop-notes.md)

## Review page and evidence

Open `index.html` through the existing local art review server. The Turkish review page compares the map, shows architectural close-ups and includes the actual portrait view. It uses local files only.

Expected captures, supplied by the integration owner:

| File | Evidence |
| --- | --- |
| `before.png` | September 24 actual Unity map, matching comparison framing; September 25 atmosphere is pre-existing |
| `after.png` | Combined actual Unity map |
| `gate-detail.png` | Gate and boiler composition |
| `crane-detail.png` | Crane and adjacent service area |
| `shaft-detail.png` | Shaft and surrounding paving |
| `workshop-detail.png` | Workshop stock and supports |
| `point-2.png` | Actual Unity portrait gameplay camera |
| `device.png` | Optional actual Android screenshot after installation |

The phone section appears only after `device.png` loads successfully. A Unity screenshot is not device evidence. File names in this inventory describe intended evidence; their listing alone does not establish that capture or verification has completed. Screenshots must be copied unchanged from their real capture outputs, with no generated replacements or retouching.

## Verification status

Completed:

- Compile check: 0 warnings/errors. Static integrity: 514 project GUIDs, 1099 package GUIDs, 568 scene objects/components.
- Unity EditMode 405/405; PlayMode 130/130; Android build exit 0. Actual graphics capture and independent visual review passed.
- Static region: 17 renderers / 46,476 vertices, versus 16 / 44,716 before. Existing atmosphere remains a separate 1 renderer / 780 vertices.
- APK BF87942E8C9EAE76AA5DDCB9F575493F1B3C9C260E5EA41E82F4FFD69FC196CC (39,340,606 bytes) installed on S23; installed hash matched.
- All 7 critical save files byte-identical across installation. Profile revision 215, 32,776 gold, Staff/Second Wind, deepest floor 3 unchanged after review.
- Guarded physical movement captured the north storage shelf, paving and brazier during combat. Device left PAUSED in room 2/6, wave 2/3, 67/100 HP, level 0, XP 1/10, 0 run gold.
- Current process log: no fatal/null-reference/missing-reference/invalid-operation exception matches. Active windows 2-3: 59.9 fps, max 16.9 ms. Startup still spikes to 2165.4 ms. This is a short S23 smoke, not a sustained or midrange performance acceptance.

Evidence is in TestResults/foundry-art-package-20260929; docs/22 and docs/23 record details. No commit/push. No profile restoration/edit. The comparison baseline uses the September 24 capture because the later whole-map export had a capture-only backdrop-framing omission, now corrected in the capture helper. The game backdrop was not newly replaced here.

## Acceptance points

- The quieter paving must preserve actor separation at portrait phone scale.
- Iron collars and panel details should read as material variation without overpowering the painted door seal.
- Workshop stock must sit on its supporting shelves and remain visibly outside the playable floor.
- Central details and braziers must preserve a clear combat surface.
- Existing smoke, fire openings and their anchors must continue to align.

This review page adds no image assets or game behavior.


