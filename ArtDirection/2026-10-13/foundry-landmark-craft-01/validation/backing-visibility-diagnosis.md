# Gate backing visibility diagnosis

The first full gate verification reported one failure out of 593 EditMode cases. The two production splay cases exercised four visible shaft faces; only the outer profile's narrow side failed the new rear-centre visibility assertion. PlayMode had passed 159 cases; the root workflow skipped the APK after the EditMode failure.

## Cause

The failing profile is `(27.53, -6.14, 30.42, -4.61, 2.85, 6.55, 0.065, 0.36)`, side 1, corresponding to octagon edge 6. Its backing is translated laterally in projection by the existing 0.36 reveal depth. The physical centre of that translated backing is behind a jamb, rather than in the portion of backing visible through the throat.

Independent reconstruction of the existing octagonal shell, bilinear face, original flat frame, new splayed frame and emitted triangle order gives the following classification. `160` is the first interior triangle of frame edge 1; indices below 20 belong to the backing.

| Profile | Side | Rear-centre rises | Old flat frame: last covering vertex index | New splay: last covering vertex index |
| --- | --- | --- | --- | --- |
| Inner | 0 | 0.31 / 0.47 / 0.61 | 0 / 12 / 12 | 0 / 12 / 12 |
| Inner | 1 | 0.31 / 0.47 / 0.61 | 160 / 160 / 160 | 0 / 12 / 12 |
| Outer | 0 | 0.31 / 0.47 / 0.61 | 0 / 12 / 12 | 0 / 12 / 12 |
| Outer | 1 | 0.31 / 0.47 / 0.61 | 160 / 160 / 160 | 160 / 160 / 160 |

The failed assertion therefore had an invalid visibility precondition that also fails against the old flat geometry. The splay did not introduce this occlusion.

## Independent projected-area check

The legitimate visible backing is the convex intersection of the rear pentagon and the front throat pentagon. Double-precision clipping of reconstructed geometry produced these projected world areas; these diagnostic values are not Unity test results.

| Profile / side | Full rear area | Old visible area | New visible area | Old / new fraction of rear |
| --- | ---: | ---: | ---: | --- |
| Inner / 0 | 1.4860455 | 0.9535075 | 1.0809683 | 64.164% / 72.741% |
| Inner / 1 | 0.8145850 | 0.3278013 | 0.4407886 | 40.242% / 54.112% |
| Outer / 0 | 1.7416035 | 1.0542803 | 1.2179275 | 60.535% / 69.931% |
| Outer / 1 | 0.7985053 | 0.1982961 | 0.3339289 | 24.833% / 41.819% |

Every old visible polygon is contained in its new visible polygon. The sum of the actual backing-triangle intersections equals each new polygon's entire area. No later return, body or frame triangle has positive-area intersection beyond numerical noise (below 9e-14 in the independent diagnostic). A second reconstruction using float-rounded vector/projection operations gave the same classification and area conclusions.

## Test-only correction

The existing two production test cases now calculate both visible polygons independently from the original shell and authored opening stations. They require a positive baseline area, a strictly larger new area, complete containment of the old visible domain, complete actual backing-triangle coverage and no later triangle overpaint. Polygon clipping uses double precision and a strict 1e-8 area tolerance. Existing geometry, UV, ownership, material and other test tolerances remain unchanged. No probe was moved, no runtime geometry was changed, and no additional test case was added.

The repair is restricted to `FoundryGateRecessTests.cs`. Root owns the targeted Unity preflight and subsequent full verification. This report does not assert their outcome.
