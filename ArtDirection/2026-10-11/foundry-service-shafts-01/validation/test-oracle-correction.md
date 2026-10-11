# Intermediate alpha test correction

The first complete EditMode run reached 497 cases and reported five failures in the new service-shaft tests. They all failed the alpha assertion before their remaining geometry checks: raw head/hollow helpers contained alpha 1.07 and the raw arch in the combined test contained alpha 1.08. The original failed XML is retained by the root agent.

This was a test precondition error. Unity's `Color * scalar` multiplies alpha along with RGB. These private geometry helpers intentionally supply intermediate colors; `DistantTower` finishes every newly emitted stone and fire color through `FadeDistantFoot`, which **replaces** alpha with `Mathf.Clamp01(...)`. It does not multiply the incoming alpha. `BuildDistantWorks` separately sets the arch's alpha to 1 before adding the five towers. No source path after those finalization steps changes opacity, and the two complete-tower tests passed their strict alpha checks in the failed run.

The corrected oracle always requires finite RGBA. Only the raw head and hollow-helper checks opt out of the final `[0,1]` alpha requirement. Completed tower and combined-batch checks retain that requirement. The combined test now reproduces the production arch alpha reset before adding towers. Geometry, mouth connections, fire pane boundaries, projected bounds and vertex budget assertions are unchanged. No runtime source was changed for this correction.

The test agent did not run Unity or ADB. Root will run the focused nine cases and the complete EditMode suite against the corrected test file; this note does not claim those reruns have passed.

Root verification follow-up: the focused nine cases passed, followed by all 497 EditMode cases with no failures or skips. The 159 PlayMode cases also passed against the same runtime source. See `editmode-focused.xml`, `editmode.xml`, `playmode.xml` and `verification.md`. The Android build succeeded after the final suite results were checked; no device install was performed.
