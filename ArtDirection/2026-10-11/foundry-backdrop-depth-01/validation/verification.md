# Final verification — backdrop depth

Baseline49cf944e9653beda95111bc8c1709480f6bfaa7a. Before images are unchanged accepted baseline captures; hashes in provenance-sha256.txt. Root ran Unity exclusively, with no concurrent Assets mutation.

- Pure .NET:409passed,0failed/skipped.
- Final compile check:0warnings/errors.
- Final EditMode:446passed,0failed/skipped,exit0.
- Final PlayMode:156passed,0failed/skipped,exit0. Two new camera/fallback tests; existing ownership case strengthened with borrowed V2 lifetime and actual Android compression/mip checks.
- Total1011distinct cases, all passed. Suites/build repeated after the NPOT import correction; earlier successful results are not added to this distinct total.
- Graphics:3after passes (initial, explicitAndroid diagnosis, finalcompressed). Root/independent finalvisual acceptance. Before XML is historical evidence, not an additional execution.
- Region32renderers/118709vertices unchanged. Backdrop retains one sprite renderer and existing atmosphere shader/ranks/tint/cover/parallax. No FPS/allocation claim.
- Actual Android-target Editor import:1024x1536ASTC6x6,one mip,non-readable; direct asset and sprite texture agree. InitialNPOT+mips loadedRGBA32, despiteASTCoverride. Turningmips off restoredcompression. Editor texturememory16778104->1401728bytes; not a phone measurement. Androidbuildreport asset684.6KB, comparedwith8MBinitial uncompressed resource.
- Static586asset/folderGUIDs,1099packageGUIDs,568sceneobjects/components; references/JSON valid.
- Final graphics/EditMode/PlayMode/Android logs: no C# warning/error or Shader error matches.
- Final Androidexit0. Actual packaged APK31,210,301bytes, timestamp2026-10-11 13:48:53 Europe/Istanbul. SHA25644F3650572568D69C3536F517270182372A9E896A2FF5318E1E754B79269D24F. Initial uncompressed APK is superseded.
- Browser gallery view selector/Before/After/reference controls verified; displayed images loaded.

Source generated with built-in image_gen, imported unchanged; full prompt in source/prompt.txt. Source/importSHA2569FDEE9AE5B054A3368B3B1D17553FCD2F697D00A6330347CDAC101AC38B9AC0B. Original generic cavern untouched. No camera/walking/ramp/scene/gameplay/save change. Newassetmeta whitespace normalized after verification; no importer values changed.

NOT INSTALLED. No ADB or fresh phone observation. Earlier concrete owner-run restart/install decision remains pending; historical021b95f and Room2/74HP/8unbankedgold are not current state. After authorization inspect fresh, back up current original files, install/hash-check latestAPK and preserve current original saves/telemetry. Never restore stale saves. Device/crowd/midrange/first-ramp and motion-shimmer acceptance remain open. Regional encounters/boss progression remain deferred.

Reproduce sequentially with Unity closed: dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release; dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo; pwsh -NoProfile -File Tools/ArtReview/Run-FoundryBackdropDepthProof.ps1 -Phase after; pwsh -NoProfile -File Tools/Run-UnityTests.ps1; pwsh -NoProfile -File Tools/Verify-Project.ps1.