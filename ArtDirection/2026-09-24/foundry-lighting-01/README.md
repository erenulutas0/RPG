# Foundry static lighting and surface pass

Owner requested continued visual development toward the approved foundry concept, after discussing the current quality gap and potential Blender production. This slice improves actual existing Unity scenery. Blender was not found on PATH or under the standard Program Files/Blender Foundation location and was not used. No new raster source was generated or edited.

FoundryRegionFinish adds build-time vertex-colour illumination to the existing projected floor: cool ambient tone, local warm pools at furnaces and beacons, boundary darkening and deterministic modest stone variation. The bridge is subdivided for local falloff. Block caps have narrow inset highlight bands; visible side faces have directional tone separation. These are stylized static shading cues, not physical lighting, cast shadows, normal maps or true bevelled 3D models. They use existing materials/batches and do no per-frame work. Heat does not flicker or affect characters.

Movement, combat, camera, map footprint, imported materials, and scene data were not modified by this slice. Prior uncommitted changes, including deferred movement energy, remain present.

The graphics fixture walked five points on the actual foundry scene and exported matching 3200x2400 views without HUD/actors. before.png was copied before editing; after.png is the resulting Unity capture. gate-camera.png and shaft-camera.png are actual Unity portrait renderings, not device screenshots. index.html compares before/after. The static budget is recorded in static-budget.txt. Full verification and device status follow below.

The next substantial architecture step needs modeled silhouettes and consistent surface detail, particularly gate buttresses, furnace roofs and the crane. This pass does not claim to attain the reference illustration's final quality or establish performance on midrange phones.

## Verification and installed device

Unity graphics walk/capture1 passed. Full EditMode405/405 and PlayMode130/130 passed; static integrity507 unique project GUIDs/568 scene objects. Android build exit0. APK33,774,777 bytes, SHA256 259EA288CC447CE38B8371DA694B7D43B57777A80AB8A6B6B2C5A63C57B394F1. Includes previous energy deferral. No pure combat changes in this slice; earlier pure390 result is historical, not rerun here.

Owner explicitly authorized restarting/installing/testing. S23 was awake/unlocked at launcher. Fresh backup TestResults/foundry-lighting-20260924/before; install-r Success, installed APK hash matched, all10 stored files immediately byte-identical. Stored profile revision199/28507gold/defaultSword/SecondWind/deepest3 retained. Two guarded12-second northeast drags with one Tempered Edge choice reached the gate/furnaces. device-gate.png is an unchanged actual phone screenshot. Final state is Forge room4/6 choice,14HP,level1,XP20/22,5runGold; pause tap occurred after Forge opened and did not dismiss its choice. No Forge choice selected. Profile SHA256 FBCE902BD0202A5F83C74D0BE4194F0D37EDBCD329D10120F48F6F6C0165E90D unchanged after review; no restore or save modification, telemetry grew normally.

Current process log has0 FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException matches. Normal sampled windows approximately59.9fps/16.7ms; first startup window max2211.5ms remains unresolved. Brief device art/movement smoke, not sustained density or midrange validation. Energy HUD absent; twelve-second movement reached gate under energy-free rules. No commit/push.
