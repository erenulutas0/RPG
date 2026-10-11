# Geometry connection review

Scope: corrected FoundryRegionDistantWorks.cs, before root's actual capture. Independent read-only review by perimeter_rhythm; root reviewed the diff and final images.

The first buttress taper left its upper head beside the receding shoulder. The correction shifts each head inward in design-plane u (.55, opposite signs on the two corners) and v (+.55), while preserving the original base and taper height.

The left head is u0+.55 to u0+.85, v0+.51 to v0+.83. At the corresponding variant-1 height the shaft starts about .6263 inward: minimum contact overlap is about .2037 in v and .2237 in u. The right corner is symmetric. Overlap is also positive at the shoulder start and .52 join; extents interpolate linearly within each segment, so the contact holds between these boundaries. Variant 2 has more overlap. Head dimensions remain .30 by .32 and taper height 2.75; projected surfaces retain nonzero area.

Fracture front/back faces share the same low/notch/high endpoints, and their exposed core planes close the severed tips. Roof cap geometry seats the offset chimney. No additional material, render order, alpha lifecycle, physics or update component was introduced. Existing structural/budget tests remain the automated guard.

Actual capture acceptance is separate in independent-art-review.md and README.md. Resource counts do not establish physical-device performance.
