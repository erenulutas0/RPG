# Review: rendering and HUD work, 84d1cdb..4cda1cc (2026-11-04)

Cloud review, no Unity editor. Scope: `git diff 84d1cdb..4cda1cc` over `Assets/_Project/Scripts/UI/Arena/`,
`Assets/_Project/Scripts/UI/` (HudArtView, RunHud, FloorClockView, SharedSpriteMaterial, RunFeedbackView, RunResultView,
WardenSlamView, PitEruptionView, EnemyLookView, ExitSealView, AltarView), `Assets/_Project/Scripts/Art/FoundryMap.cs`, the
PlayMode tests named in the brief, and `Tools/ArtReview/RoomPipsRuntimeCapture.cs`. I read every hunk alongside its whole
file and its callers.

## Outcome

There are no confirmed correctness bugs in the scope, so no scene code changes. I added one new test file,
`Assets/_Project/Tests/PlayMode/FoundryDepthCoverTests.cs` (unverified, needs Unity). It is a plain `[Test]` fixture with no
scene that checks the silhouette gate's geometry directly. Nothing tested that geometry on its own before; only whole-scene
tests covered it:

- `ATriangleCoversEveryPointInsideItAtItsNearestDepth`: for 1,500 seeded random triangles, large and small, with 60 sampled
  points inside each, the cover reads at most the triangle's nearest depth, and a body just behind it is `Covered`, never
  `Open`.
- `TheCoverAnswersUnknownForWhatItCannotVouchFor`: an unfinished cover, a box beyond the cells, and NaN in a corner or in
  the depth all answer `Unknown`. `AddBox` respects `CoverMargin` on both sides.
- `EveryPoseOfAGateLeafStaysInsideItsSweep`: leaf vertices are laid out as `BuildGateDoorway` lays them, posed at 41
  openings through `FoundryGateDoorMotion.Pose`, and every pose stays inside `Sweep`'s box unioned with its rest. The
  swing never changes depth.

Before writing the test I ran the same checks outside Unity:

- I compiled `FoundryDepthCover.cs` unchanged against a stub `UnityEngine` (net10) and fuzzed it: 1.6 M sampled points,
  0 misses.
- I re-implemented `Sweep` and `Pose` in Python: the worst excursion outside the box over 400 openings per vertex was 0.

`dotnet test Tools/CombatChecks -c Release`: 682 passed, 0 failed. That is above the 658 in the brief, because main has
gained tests since. It ran on SDK 10.0.114 with `DOTNET_ROLL_FORWARD=Major`, because the .NET 9 download was refused by
the proxy (403).

## Findings

All three findings are low severity and latent. None was fixed, since none has a failing scenario with the shipped data.

1. **The gate countdown beats once when it hands over to the floor clock inside the last minute.**
   `UI/FloorClockView.cs:127-131`, low, not fixed.
   - Scenario: a floor whose `FoundryGateNotBeforeSeconds` is above 240 s. When the hold ends, the label switches from
     the gate's count to the floor clock. `_shownSecond` is still the gate's last second (>= 0) and the floor is in its
     last minute, so the switch itself starts a beat, as if a new second of the warning had ticked.
   - Why it is not a bug today: Ember Halls holds to 210 s, which is 90 s before the overrun, so `lastMinute` is false
     there. The GateHoldProof floor holds to 12 s.
   - Possible fix, if a floor ever holds past 240 s: set `_shownSecond = -1` when `_shownGate` was true.
2. **Room pips grow without a cap.** `UI/RunHud.cs:219, 274`, low, not fixed.
   - `GrowRoomPips` copies the last pip once per extra room. Its own comment says the row has room for nine before the
     pause button.
   - Scenario: a floor of 10 or more rooms would put the tenth pip under the pause button (x >= 936 on the 1080 canvas).
   - Why it is not a bug today: the most any floor plays is seven (Ember Halls' six plus one `FoundryExtraElites`), and
     the scaler matches width (1080 reference, match 0). So the row's geometry is the same at 1080x2400, 1080x1920 and
     on tablets.
3. **The gate hold flag goes stale while an offer is open.** `Combat/EncounterController.cs:614`, informational.
   - `_holdingForGate` is re-evaluated only on frames where `_fight.Tick` returns true, and it returns false while a
     choice is open.
   - Why the countdown still reads correctly: `CombatSetup` freezes `Time.timeScale` while a choice is open, so
     `FloorClockSeconds` does not move. The label therefore cannot run past 0:00 against a stale flag, and the panel covers
     it anyway.
   - The flag also clears in the cases that matter: a fallen hero (`OnHeroDied`, line 507), every `AdvanceFloor` and
     `EnterFloor`, and a restart (the scene reloads).

## Checked and found sound

### Silhouette gate (FoundryVisibilityView, FoundryDepthCover, FoundryRegionDepth)

- **Depth direction.** The x-ray second pass is `ZTest Greater`, and only the cutaway's second pass writes depth (no other
  shader in Assets has `ZWrite On`). So "covered" means a structure z is less than the body's farthest z, which is what
  `Ask` tests with `limit = far + CoverMargin`.
- **Spaces.** The cover is in region-local x and y, and z is mesh vertex z after `ApplyStructureDepth`. Meshes are
  identity children of the region (`SetParent(transform,false)`). A body's footprint is world minus `region` on all three
  axes. `Plain()` refuses a rotated or scaled region.
- **Footprint.** It is the sprite's local bounds, flipped about the pivot (matching `UnityFlipSprite`), then put through
  `localToWorldMatrix` as centre plus |M|·half. That is exact for an affine transform. It is read after `MoveToDepth`, so
  it uses the body's lifted and depth-placed pose, and the drop and chest roots are moved before `ShowLootWhenHidden`.
  Anything other than `Simple` draw mode, or a missing sprite, is Unmeasured and keeps the silhouette.
- **Coverage of depth writers.** Every structure-rank mesh goes through `Emit`, which goes through
  `CoverStructureDepth`. The only direct `Emit` calls (`FoundryRegionDistantWorks`, -19 and -18) are not structure ranks.
  A structure emitted with no cover abandons the cover, and the whole gate then refuses. Triangles are marked if any
  vertex has `flags.x > 0`, and the shader's interpolated `clip(flags.x - .5)` writes depth only inside such triangles.
  A faded fragment writes less, never more.
- **Moving vertices.** `FoundryFlameMotion.Reach` and `FoundryBannerMotion.Reach` bound the sine sums exactly. The door
  `Sweep` holds because `Project` is monotone in u and v on both screen axes and the pose's u and v are each monotone in
  the angle. Motion never changes z: the door copies the rest z, and flames and banners move x and y only.
  `DepthCoverRoom` of 4 exceeds the largest swing (under 2).
- **Cover edge cases.** The triangle and cell test is a correct separating-axis test: the triangle's three edge normals,
  with the box axes covered by restricting to the triangle's bounding cells. Degenerate edges separate nothing. The
  small-triangle shortcut only adds cells. `Ask` answers Unknown for an incomplete cover, out-of-range boxes and NaN.
  `MaxCoverArea` bounds memory (the real map is about 80k cells).
- **Camera gate.**
  - `CameraRefusal` refuses perspective cameras, cameras that clear nothing, targets without depth, and turned cameras.
  - Each camera re-places itself, because post-render restores and clears `_applied`.
  - A gated placement is never reused for a camera that is not flat.
  - A camera-less placement (`ApplyActorDepth()`, tests and probe only) is ungated, so every body keeps its silhouette.
  - The Scene view camera falls into a refusal.
- **Materials.**
  - `Register` and `GiveBack` swap only between a body's own material and the scene-owned silhouette materials.
  - No other code assigns an enemy, drop or chest body's `sharedMaterial`.
  - Pooled drops stay registered while parked, and `RestoreXrayed` on `OnDisable` gives everything back.
  - `OnDestroy` destroys the variant, x-ray and ground materials.
  - No `MaterialPropertyBlock` or `.material` instancing was added.
- **Allocations.** The gated path (`Footprint`, `Ask`, `GiveBack`, `Register` once the lists have grown, `CameraRefusal`,
  `CoverRefusal`) allocates nothing. Refusal strings are static readonly fields. `DescribeGate` allocates, but only the
  probe and tests call it.

### HUD

- **SharedSpriteMaterial.** The arena's `_material` in Gameplay.unity is the built-in Sprites-Default (fileID 10754).
  None of the five views writes a property on it: they set `sharedMaterial` only, and colour and width live on the
  renderers. `FoundryGroundProjection` restores whatever it swapped. Removing each view's `Destroy(_material)` is correct
  now that the material is shared. The session fallback survives scene loads, and its `_session == null` check catches a
  destroyed one.
- **RunHud and HudArtView pips.**
  - Clones are copies of an already painted pip. HudArtView's `LateUpdate` also paints any index past `_hudPipsPainted`
    before the frame draws, and `HudArtView` disables itself without valid art, so `PaintPip` never sees a null `_art`.
  - Pips beyond the room count are hidden, so floor 2 hides the seventh.
  - A restart is `SceneManager.LoadScene`, so pips, `_hudPipsPainted` and the clock's state start fresh with each run.
  - `RoomPipAt` is guarded by `RoomPipCount`.
- **Glint.** It is anchored to the fill from foot to top with `sizeDelta.y = 6`, so it stays 3 past each edge through
  any relayout. There is no division.
- **FloorClockView.**
  - The gate count is `CeilToInt(GateOpensIn)`, where `GateOpensIn` is at least 0, so it reads 3:30 down to 0:01 and
    cannot read negative.
  - An overrun takes precedence over the gate count.
  - Pause and offers freeze it, because the clock does not tick at `timeScale` 0.
  - With a fallen hero the run has ended, so the label is hidden, and `OnHeroDied` clears the hold anyway.
  - A Second Wind revive re-evaluates the hold on the next cleared tick.
  - Hide and reshow reset `_shownSecond`. The gate branch ends any beat and resets the scale.
- **EnemyLookView.Slam.** The new `!IsWindingUp` guard matches `WindUp` refusing sets without a windup pose.
  `WardenSlamView` drops a destroyed look through Unity's null check, and `CancelWindup` covers a withdrawn mark.
- **RunResultView.** `WeaponShop.OnlyStartingWeapon` exists. The new format string has a field initializer, so assets
  serialized before it still read the default.
- **PitEruptionView, AltarView, ExitSealView, WardenSlamView, RunFeedbackView.** These are material-only changes, and
  `_arena` is found before it is used.

### Tests and tools

- The tests use none of Unity's missing NUnit features: no `Has.Exactly`, and no target-typed `?:`.
- FoundryXrayGateRenderTests and FoundryXrayGateTests release and destroy their render textures in teardown.
- PerfProbe's new `RestorePhase` restores the canvases, the camera target and the frame cap, and is safe to call twice.
- `RoomPipsRuntimeCapture` resets `captureDeltaTime` and `timeScale` and unloads its scene. The development files it
  writes live in `TestProfile`'s temporary folder, which `TestProfile.End` deletes.
- `FoundryMap.cs` changed only in a comment.
