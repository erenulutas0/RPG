# Verification

438 .NET / 710 EditMode / 160 PlayMode, zero failures/skips. One new test (declared posts, camera-facing flag, placement proven by the floor pool, outside the walking surface, >3.5 units from any other flame pool). Isolated compile 0/0. Fresh Before (25 views, baseline 2c3c408, clean Assets) and final After Android-target captures passed. PASS: 647 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components. Android build exit 0. APK 36,392,292 bytes; SHA256 C71EE7B839309CD352A2659E0C92055D50B7129A7602A443F0B3133C9D3663B6; NOT INSTALLED.

Region 37 renderers / 154820 vertices versus 154393: +427 (three posts). No new batch, material or texture. Distant 3572 unchanged. Ceilings 174000 / 3600; headroom 19180 / 28. Static counts do not establish device performance.

Source freeze 847684efae5d0360d82180c4b81e565665db45f09990ac54732d4b5b789e49fa matched before the final After capture, after it, and after the full suites/build.
