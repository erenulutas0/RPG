# Verification

438 .NET / 707 EditMode / 160 PlayMode, zero failures/skips. Three new flagstone tests (continuous field and exact coverage; seamless calibrated raster and import; one cool stone family across both materials); the compass ownership test now builds both base layers. Isolated compile 0/0. Fresh Before (25 views, baseline aa1f390, clean Assets) and final After Android-target captures passed. PASS: 644 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components. Android build exit 0. APK 31,210,485 bytes; SHA256 D0441EF3D7721C1A61D986D31BEB91A507D488160213425FA8E2626125A2A857; NOT INSTALLED. V9 ships as 611.7 KB ASTC_6x6 (build report); the APK is ~5 MB smaller than the previous 36,387,952-byte build for reasons this package does not establish.

Region 37 renderers / 152889 vertices versus 36 / 172160: one new material batch, -19271 vertices (the laid ashlar's joint beds, bevel strips and contours). Distant 3572 unchanged. Ceilings 174000 / 3600; headroom now 21111 / 28. The PlayMode renderer guard moves 36 -> 37 for this batch. Static counts do not establish device performance.

Source freeze 248b716102385ddbf52ab890bda1ff113f3d00f688759b7fc17639ef70a09109 matched before the final After capture, after it, and after the full suites/build.
