# First-suite niche boundary failure

The rejected first full EditMode suite passed 610 of 612 cases. Two existing production niche tests detected frame triangles overpainting the unchanged projected backing by approximately 2.41e-7 and 1.86e-7 square world units. The strict limit of less than 1e-8 was retained.

Actual failed-source geometry identified newly interpolated course points on the frame's throat contour as the cause: float interpolation could round them into the opening. The repair changes only these intermediate inner-edge coordinates, preserves original end stations and shared stone/mortar points, and moves at most one adjacent float per coordinate from the nearest double-interpolated position. Existing production cases now assert that bound on emitted geometry. Test case count is unchanged by the repair.

The preserved 48-station diagnostic replay found 24 old inward points and zero after the correction; maximum change from the failed mesh is 2.132480599880018e-6 world units. Detailed per-triangle evidence, source copies, exact failure values and diagnostic limits are recorded in `niche-boundary-repair.md`, `first-suite/niche-diagnostic/` and `niche-boundary-repair/`.

The repaired source passed the actual targeted Unity EditMode run: **24 passed, zero failed, zero skipped**, including both previously failing full-area oracles. Evidence: `recess-repair-targeted.xml`. The initial PlayMode result was 159 passed; it predates the correction. These results do not claim that the new complete verification, final capture, APK or phone acceptance has passed.
