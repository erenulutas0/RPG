# Vanguard first runtime integration — 2026-09-17

The approved body/equipment direction now runs on the actual hero in Gameplay. This is the first integrated animation prototype, not a finished directional character sheet. The gallery uses real Unity renders (`VanguardRuntimeCapture`), with combat targets/enemies isolated for inspection. Browser frame playback is a review aid, not a device recording.

## Assets and registration

- Eight body/white-flash pairs: idle, contact A, passing A, contact B, passing B, sword windup, strike, recovery. New unchanged masters and exact prompts are in this folder and `PROMPTS.md`; the body/contact/windup/equipment masters remain in the two 2026-09-16 proof folders.
- Common source crop: x=240, top=89, width=816, height=1124. The crop is shared, never fitted separately to an alpha bound. It includes the outstretched strike hand. Runtime canvas 128x176, 128 PPU, body pivot `(354/816, 0)`; world canvas stays 1x1.375. Hand anchors are per-frame source coordinates retained in `BuildVanguardAssets.cs`.
- TGA RGBA32 intermediates are produced by Unity GPU minification from the retained masters. Bilinear filtering, no mipmaps, clamp, no read/write, full-rect meshes, Android ASTC 4x4. This is a scoped Vanguard exception to the 32 PPU point-filtered procedural art, not a global setting change. No Packages or ProjectSettings edits.
- Sword and buckler are separate sprites anchored to hands. Staff/orb and Daggers remain the procedural equipment. Grip layering is sufficient for prototype review; detailed painted finger occlusion is not complete.

## Behaviour and ownership

`VanguardArtSet` owns imported references; views borrow them and never destroy them. The Gameplay scene adds one reference on `HeroLookView`. Its procedural fallback and original equipment generation remain, including their existing cleanup. This slice does not claim to eliminate hero-startup allocation.

`VanguardAnimator` reads actual floor displacement for the four walking frames, stops at idle when displacement stops, and freezes with combat time. A cycle covers 1.2 floor units. Sword anticipation observes a read-only weapon cooldown and an in-range target. The actual attack event immediately selects the strike after damage; recovery follows. The first ready attack can skip anticipation. No view moves an actor, ticks a weapon, changes targeting order, or applies damage. Staff/Daggers preserve their orb/thrust feedback and use the new hand positions.

`CombatantView` retains attack nudge, per-frame hit silhouettes and death hiding. The existing contact shadow stays on the floor. Note that its nudge/flash settle on unscaled time while the new body animation freezes on combat pause.

## Reproduce

`pwsh -File Tools/ArtReview/Run-VanguardProof.ps1` stages a temporary graphics-enabled fixture, checks pure tests/compilation, loads the actual runtime scene using isolated test profiles, captures observed walk/attack frames for all three loadouts, converts raw BMP evidence losslessly to PNG, and removes the temporary source/meta. It does not build/install an APK. Close Unity first.

Optional `-Reimport` rebuilds the asset set from retained masters before capture, using `Convert-CharacterProof.ps1 -Mode Source -Keyposes -Integration` and a temporary Editor builder. Existing metas are preserved. The initial import exposed that opening a scene could unload an unreferenced newly-created set; the builder now reloads and validates it after opening. The committed scene reference was assigned explicitly and validated by runtime tests.

Runtime gates and physical-device evidence are recorded in Docs/21–23. `VanguardAnimationTests` covers actual movement/pause/release, damage-aligned strikes and recovery/anticipation, flash/death/reload lifetime, and owned Staff/Daggers attack/walk behaviour. The full scene/simulation parity suite must remain green.

## Remaining visual work

Only the rear northeast-facing body exists. Moving/attacking in another direction does not yet rotate its appearance. Passing poses improve alternating support legs but still need planted-foot cleanup; the arm/cape continuity is not production-quality. Sword/hand occlusion needs polish. Staff/Daggers, enemies, effects and floor retain earlier procedural art, so the scene deliberately shows mixed fidelity. The Mite concept is not integrated by this slice. Next bounded art work is opposing facing plus gait/grip polish, then matching Staff/Daggers and one enemy.

## Verified build and device review

Full gate: .NET 272/272, EditMode 284/284, PlayMode 87/87, clean compile, Android exit 0 and integrity 317 GUIDs / 555 scene objects/components. The graphics-enabled capture passed 1/1, including a second run through the committed wrapper. Installed S23 APK SHA-256: `971FEF6BCEBF5EB8EAE06DEA51ECF08780833485D8FE80E1142511D016A90ADE`, matched on-device. `device-sword-walk.png` is an unchanged physical phone capture; other rendered comparison images come from Unity Editor. Staff and Sword received physical smoke checks; Daggers and death/flash/lifetime checks are Editor coverage in this slice. See Docs/23 for complete evidence, frame-time limitations and preserved profile/phone state.
