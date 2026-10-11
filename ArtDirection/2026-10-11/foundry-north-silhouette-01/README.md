# Foundry north silhouette — actual Unity proof

Baseline: `bd5b162`. The seven selected before views and accompanying diagnostic/XML files are copied unchanged from the accepted `foundry-distant-material-01/after` evidence. Each copy was SHA-256 checked against its original; see `validation/provenance-sha256.txt`. These are reused historical captures, not a new before execution.

Approved target: `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`. This proof evaluates structural silhouette and connection changes to the native northern distant works and broken span. Actual implementation, review limits and verification are recorded below and in `validation/`.

## Capture and evidence contract

`Tools/ArtReview/FoundryNorthSilhouetteCapture.cs` retains all standard matched camera views and diagnostics from the previous proof. The seven versioned gallery images are `overview`, `full-map`, `skyline-detail`, `expanded-30-5`, `expanded--26-15`, `bridge-foot` and `casting-portrait`. Other standard after views remain local review output. Before copies include only these selected images plus metadata/budgets and historical graphics XML.

The runner explicitly selects Android. `cavern-texture.txt` records the actual loaded backdrop texture, Editor target/importer settings and memory observation. `distant-material.txt` separately records native owned material atmosphere, per-batch rank/material/shader/vertex count, finite and nonfinite RGBA/UV entries and component ranges. Region and distant budget reports are retained. These resource observations are not physical-device frame-time or memory measurements.

All comparison images are actual Unity graphics captures with staged actors and hidden HUD. Portrait views retain the production camera width. They are not phone screenshots, and the approved concept is an art target rather than completed runtime evidence.

## Reproduction

With Unity closed, run `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryNorthSilhouetteProof.ps1 -Phase after`. Phase selects the destination only; it does not revert source. The runner installs a temporary capture source/meta and removes them in `finally`. Root alone operates Unity and ADB.

One actual Android-target after graphics capture passed. The before images remain reused historical evidence. Browser view selection and before/after/reference switching were checked; all visible comparison images loaded. Root alone ran Unity; three helpers split geometry implementation, proof/tooling and connection review, and independent art QA. Final suite/build results and APK identity are recorded separately in `validation/verification.md`.

## Implementation and budgets

Only runtime partial `FoundryRegionDistantWorks.cs` changes. The two dominant northern shafts gain different receding crowns: a narrow dark exhaust opening on the taller tower, and a broad hipped hood with a seated offset chimney on the lower tower. Two substantial corner buttresses on each shaft lean inward into the shoulders. Initial code review found a gap at their heads; this was corrected before the first actual capture. Independent calculation confirms continuous positive overlap through the sloping contact.

The severed span ends now use different closed, two-band fracture profiles with exposed thickness. Terminal posts are removed and the parapet ends are capped. The bridge anchors, curved underside, bearings and central gap remain. Fully buried foundation caps are omitted to preserve the existing geometry ceiling.

Actual distant batches: 2,352 masonry plus 144 furnace vertices = 2,496, up 108 from 2,388 and still below the unchanged 2,500 ceiling. Region: 32 renderers, 118,817 vertices, up 108 (about 0.09%). No new renderer, texture, material instance or per-frame component. These are Editor resource counts, not device frame-time measurements. Existing borrowed textures, owned material atmosphere .44, UV scale, lower fade and ranks -20/-19 are unchanged. Cavern inspection still reports Android-target ASTC6x6, one mip, non-readable.

Camera, walking/ramp geometry, gameplay, scene and saves are unchanged. Existing finite-geometry/UV, budget, ordering and resource-ownership tests remain intact.

## Visual acceptance and limits

Root reviewed skyline detail and full map. The taller crown reads more distinctly and the lower hood/chimney breaks the repeated block outline. Buttress heads meet the shafts without visible detached tips. Broken-end changes are mainly a close-view gain; this is not a claim that the full reference architecture is reproduced. New geometry stays behind the court and does not imply a usable crossing.

Large regular masonry masses and sparse northwest workyard distribution remain the main reference gaps. Next substantial art work should address coherent workyard composition with explicit circulation and blocked-footprint contracts, rather than repeating isolated tint or trim changes. Physical-device, crowd, midrange, first-ramp and motion acceptance remain open.

No phone action occurred in this slice. The earlier concrete owner-run restart/install decision remains pending; inspect fresh state and back up current original files before any later authorized install. Do not use historical run records or restore stale saves.

Full reference fidelity and broader workyard composition remain incomplete. This proof does not authorize or claim gameplay, progression, camera or walking-layout changes.
