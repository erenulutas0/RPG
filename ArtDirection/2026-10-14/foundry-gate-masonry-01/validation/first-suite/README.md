# Initial full-suite rejection

The first complete suite attempt passed 603/605 EditMode cases and 159/159 PlayMode cases. The launcher correctly skipped the APK because two newly added geometry assertions failed. This directory preserves both XML reports, the exact source freeze and the two failed test sources before repair. The graphics XML belongs to the initially accepted After capture, not to a phone test.

See ../header-failure-analysis.md and ../coping-failure-analysis.md for the measured causes and correction rationale. The intended repairs are test-only: a valid three-quad corner has 12 referenced indices, and the envelope oracle must distinguish representable float boundary rounding from an actual expansion. Runtime geometry and material sources remain unchanged by these repairs.
