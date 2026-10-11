# Final verification — industrial furnace complex

2026-10-11. Baseline 2c3c83651faa1303162e39d1cd5b5ad8c436422d. Root alone ran Unity. Five helpers completed source authoring, runtime integration and independent reviews; maximum three helpers concurrent.

- Pure .NET: 425/425 passed (pure.trx).
- Unity EditMode: 462/462 passed (editmode.xml).
- Unity PlayMode: 159/159 passed (playmode.xml).
- Total: 1,046 distinct suite cases, zero failures or skips.
- Compile check: zero warnings/errors.
- Static integrity: 586 unique asset/folder GUIDs, 1,099 package GUIDs, 568 scene objects/components; references and JSON valid.
- Actual Android-target Unity graphics capture: 1/1 passed, retained separately in after/graphics.xml; not counted as a suite case.
- Android build exit 0; no C# compiler or shader errors in build log.
- APK: 36,220,337 bytes; SHA256 344C19DFC44D4C84D7A0248BD7B30D47F650436343C25AD440DB4576CE20B0D9. The Unity build report also reports an aggregate size of 227,531,174 bytes; that is not the on-disk APK size. NOT INSTALLED.

Root and independent actual-image review accepted the four industrial variants and visible socket/flue contacts. Initial solid-capped tall flues and receiver gap were corrected before integration. Import assertions verify both atlases actually load as 2048x1024 ASTC6x6, 12 mips, clamp and non-readable. Original texture GUIDs remain. Exact pixels of the four native PNGs are retained in their atlas tiles. Existing two source materials and four band batches remain; borrowed texture and owned material/mesh lifetime are covered.

Region remains 32 renderers; vertices 125,406 -> 125,234 after removal of the superseded hatch overlay. Distant geometry remains 2,496 vertices. Theoretical ASTC block payload rises 1,248,832 bytes across the two atlases, excluding engine overhead. Editor texture-memory readings are 2,502,304 bytes per atlas. These are not device measurements or frame-time claims.

Twelve before images are SHA256-checked copies from the compact-gate proof. The capture launcher is after-only; two extra close views have no before counterpart. Browser view selection and before/after controls were verified, and all displayed images loaded. Graphics are captured in Unity with hidden HUD and staged heroes, not on the phone.

No ADB command, installation or fresh device observation. Earlier concrete restart/install decision remains pending. Future deployment must inspect fresh state and preserve current original saves/telemetry; never restore historical snapshots. Phone circulation, crowd, ramp and motion/shimmer validation and midrange performance remain open. Full reference fidelity remains unfinished, with gate housing and supporting stone architecture the next substantial visual gap. Region gameplay/boss progression remains deferred.
