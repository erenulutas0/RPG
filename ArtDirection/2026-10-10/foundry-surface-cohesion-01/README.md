# Court, brass and furnace interior

Baseline:98313ba. Three coordinated art changes within the existing map layout, camera, movement, gameplay and saves:

- Stone: favor compact oblong flags, soften nonceremonial joints/bevels and neutralize broad mineral tones. Half-unit packing, exact coverage, forced brass boundaries, repair patches and ceremonial unit flags remain.
- Brass: use neutral warm vertex tints over the already golden coping texture; sample its clean middle and widen the metal within unchanged outer grooves. Shared existing lighting and its1.9 color ceiling remain. No emission, texture or new mesh batch.
- Pit: three contained lining flues below the deck, plus faceted blue-black vessel crowns within their original radius1.1 and height1.26. Pool, collar, atmosphere, flue mouths and vessel centres remain.

Actual matched Unity images are retained in before/ and after/. The proof tool uses production geometry and camera width for portrait captures, with hidden HUD and staged actors. Overview and full-map are separate wider review cameras. These are not phone screenshots. The output phase does not recreate historical source: baseline reproduction requires98313ba.

## Visual acceptance and limits

Brass improves most clearly: the seal and processional frame now read as warm worn metal; actor/range-ring separation remains. Fine repeated seams remain visible on round bands at close inspection. Floor joints are quieter and proportions slightly more varied, but the rectangular paving structure still differs from the reference's irregular stonework. Repeated palette/weight tweaks alone will not resolve that remaining gap.

Pit crowns are more consistent with the neighbouring boiler metal. First image review identified an overly black, ladder-like central flue; the final capture softens its backing and ties and confines the narrower heat aperture deeper in the shaft. The root accepted the corrected view. The central flue remains simple at close range; no floor/rail spill or floating vessel was observed.

Final runtime budget:29 renderers unchanged;116,178 to117,045 vertices (+867, approximately0.75%). No new textures/materials/per-frame components. Static mesh counts do not establish improved FPS. Current actual textures/resources are unchanged from the previous package.

## Verification and device status

409 pure .NET tests passed; compile check passed with0warnings/0errors. Baseline graphics1passed; two combined graphics passes succeeded, including the final flue adjustment. Final graphical review accepted with the limits above. Full Unity tests passed:432 EditMode and151 PlayMode, no failures/skips. Distinct-suite total992. Android build exit0; no C# warning/error matches. Static integrity with Unity closed:577 asset/folder GUIDs,1099 package GUIDs,568 scene objects/components; valid references/JSON. APK: 36165000bytes, SHA256 199F9356AE439D119C5F5EDCFD0A95810D4C46841D1A74897710E65864C1CE54. NOT INSTALLED. Final full-suite XML is retained in validation/.

No ADB action or fresh phone observation this turn. Prior owner-run restart/install question remains pending; the last recorded phone package021b95f and Room2/74HP/8unbankedgold are historical observations. A fresh preflight/backup is required before approved installation. Preserve current saves and original development flags, retain telemetry and remove only newly introduced diagnostic flags; never restore stale saves.

Full reference fidelity, purposeful working-yard composition, physical-device/crowd/midrange acceptance and the earlier first-ramp hitch remain open. Gameplay/encounter progression remains deferred by the owner's art-first direction.

## Reproduction

Run `pwsh -NoProfile -File Tools/ArtReview/Run-FoundrySurfaceCohesionProof.ps1 -Phase after` with Unity closed. Implementation: FoundryRegionFinish.cs, FoundryRegionPavingWear.cs, FoundryRegionHeraldry.cs, FoundryRegionView.cs, the pit block of FoundryRegionMassing.cs and FoundryRegionPitHoods.cs.
