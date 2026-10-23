# Foundry gate apron in cut stone

Verified 2026-10-23. Baseline `4fa884fe540e1e86c15415eee8b30beb29735046`.

Side by side with the concept after the bridge package, the gate apron (paving zone 2, between the court and the gate)
was the last broad field of smooth fitted V8 slabs; on the concept it is laid in the same cut stone as the court.

- `CourtFlagstone` now lays zone 2 in the flagstone batch too (V10, the court's world field, light lattice only). The
  gate seal inlay, the gate's dressed threshold and border, the seal's ring and the repaired work aprons keep their own
  fitted geometry. On the gate ramp the laid stone follows the ramp's projection, so its stones read taller on screen,
  as stones on a slope do.
- `FoundryCourtFlagstoneTests` no longer require fitted stone on the gate apron (work-apron repairs still must).

No new batch or texture. Region 38 renderers / 153,306 vertices (baseline 156,729).

Images: `before/`, `after/` (38 matched views; new: `gate-apron`), `compare-concept.png`.

Verification: 457 .NET / 737 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,454,649 bytes, SHA256
BC8F90144B9D802DD5E5B378B6B1639D040201961B889EA0B7F84B37179505B0, NOT INSTALLED (the owner is using the phone). Source
freeze `8f62d2ac38cde0b9f2ac41a146bfe1937c2e949ddd29b39f24d52db49e0c2755` (1,269 files) taken after the final After capture,
matched after the full suites. Gameplay: none.
