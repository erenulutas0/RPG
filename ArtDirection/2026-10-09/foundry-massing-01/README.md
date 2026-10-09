# Foundry footprint and foundation — 2026-10-09

## Direction

The owner asked for the actual map's shape, size and major features to approach the approved `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png` reference. This pass changes spatial massing rather than adding another small ornament layer. The reference is a concept image; the before/after images are actual Unity renders of the production geometry.

## Delivered composition

- West court boundary moves from design u=-24 to u=-30. The pit, north edge, gate, bridge and gate elevation remain anchored. The widened side provides a broader workshop yard beside the pit.
- The complete west service assembly moves six design units with the edge, including its crane, salvage and associated architectural anchors. The shared FoundryMap continues to own rendering, movement, spawn validity and pursuit visibility.
- Exposed south/west foundation edges and front chamfers acquire one connected recessed masonry body. It reaches from 2.78 to 5.95 units below the deck, then tapers inward to a 7.25-unit foot. Existing arch faces sit in front as recessed architectural bays. No fill is added to the smelting pit or the bridge connection.
- Secondary undercroft responds are broader and shorter. The six main bastion feet shorten from roughly 17–21 units below the deck to 8.6–10.2 units, retaining their shoulders and chain anchors. This replaces the previous long isolated leg silhouette with a compact suspended fortress mass.

The combined capture also contains the companion pit/platform changes implemented in this working session. Read the final integration handoff for the complete file list and verification; this document records the visible massing comparison.

## Evidence

`before/overview.png` and `after/overview.png` use the same review camera and orthographic size 32. Detail views and four staged hero approaches are retained in each folder. They are actual generated-scene renders, with HUD hidden for architecture review; they are not phone screenshots or a substitute for gameplay-camera validation.

The unchanged concept reference is linked from the preview, not painted over or presented as a runtime result. The overview makes the wider west yard and shorter, connected underside directly comparable. The after workshop detail preserves a broad clear walking lane beside the fixed-function scenery.

Captured static region budget:

| Capture | Renderers | Vertices |
| --- | ---: | ---: |
| Before | 25 | 100,968 |
| After | 25 | 107,652 |

These are renderer/mesh counts from the graphics fixture, not frame-time or allocation measurements. Raw counts are retained in each folder's `runtime-budget.txt`.

## Reproduction

`Tools/ArtReview/Run-FoundryMassingProof.ps1 -Phase after` stages `FoundryMassingCapture.cs`, captures the current production geometry and cleans up its temporary Unity test file. The before folder was captured before the source edits and should remain historical evidence, rather than being regenerated from the new layout.

## Review and limits

The before/after overview and after workshop detail were inspected. The connected front/west wall, shorter supports, open bridge approach and unfilled pit are visible; no new clipping or unsupported dead-end face was identified in those views. Geometry/source review is not a full player traversal. The massing is closer to the reference's broad working court and substantial base; the environment still has simplified forms and is not claimed to reproduce the concept's final material richness.

Full tests and Android build passed; installation and phone review are pending permission to close the owner's current run. See validation/verification.md.


## 2026-10-09 — Reference-led footprint, fortress body and deeper shaft

Owner explicitly prioritized the approved foundry-v1 concept's actual map shape, size and major features over further small material polish or actor work. Three helper agents implemented/reviewed one coordinated geometry package. West changes-24→-30: lane beside fixed pit8→14 (+75%), +192design-square units of floor. Original pit/north/south/east/gate/bridge/ramp bounds and production camera remain. The complete western crane/salvage/stock/clutter/support/light/depth assembly translates-6 using a shared WestAssemblyOffset, so scenery stays outside the newly walkable area. Baked salvage source/UVs unchanged; runtime origin and original footprint move together. Pursuit cache gains3western nodes. Full spawn sweep now derives room bounds; ten added tests cover new clearance and bidirectional routes. Root corrected a new test point(-27,16) that lacked full.6corner clearance to(-26,16), without weakening the shared margin.

Below-deck front/west edges now carry continuous recessed masonry with a tapered skirt to-7.25. Main bastion feet shorten from~17..21 to8.6..10.2belowdeck, keeping shoulders/chain anchors. The pit remains12x12, but molten plane moves3.6→5.2belowdeck and coalbed5→6.6. Darker taller internal faces, iron vessel ribs/belts and contained base heat replace the shallow glowing-bowl reading. Atmosphere consumes the shared PitMolten depth. Actual matched overview/detail review accepted these shifts without visible floor spill, disconnected props or a filled bridge throat.

Verified .NET405, EditMode425, PlayMode151 (981total), three successful graphical captures (before, firstafter, finalafter including2newfloorportraits), Android exit0 and no C#warning/error matches. Static integrity565asset/folderGUIDs /1099packageGUIDs /568sceneobjects/components; references/JSON valid. Region25renderers unchanged; vertices100,968→107,652 (+6,684). No new textures/materials/per-frame components. No performance improvement or sustained FPS acceptance claimed.

APK ready at Builds/Android/Cryptforge-Prototype.apk:36,138,182bytes; SHA256 482CBAE8AEDFA73B7C8D04065805724F54F2DCBFAD6C31CB557CD04D7E225920. Device installation/review PENDING: preflight found a new owner run paused in Room2 with74HP and8unbankedgold. Asked whether that run may be closed; no answer yet at this record. No force-stop/install/device flags or save writes were performed for this slice. Phone still has prior021b95fworkshop package. Do not claim this new layout was traversed on S23.

Proof ArtDirection/2026-10-09/foundry-massing-01 compares actualsamecamera32before/after with the unchanged approvedconcept. Four matchedwalkableportraits plus after-only(-27,6)/(-26,15)showexpandedfloor at unchanged gameplaycamera width, hiddenHUD/stagedhero. TestXML and verification retained. Historicalbefore requires oldsource; phaseflag only selectsoutput.

Remaining reference gaps are the long narrow gate approach/enclosure, entrance-bridge proportions and sparse working-yard features. Keep those priorities ahead of unrelated actor polish per latestownerrequest. The chosen6unitexpansion is a design decision, not a dimension measured exactly from the perspectiveconcept. Fullreferencefidelity, currentphoneacceptance, cold-first-ramp diagnosis and sustained/midrange-device acceptance remain open. If installation is approved later, take a fresh backup then guarded west-newfloor/pit/normalrun, cleanup/hashchecks; scripts TestResults/foundry-massing-device-*.ps1 are prepared but NOTexecuted.
