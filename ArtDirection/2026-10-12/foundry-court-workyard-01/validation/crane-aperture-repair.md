# Crane aperture — failed geometry gate and bounded repair

The first actual Unity geometry preflight passed the existing-footing containment case but failed the new aperture case: covered338 samples, visible31 (9.2%), requirement greater than35%. Root retained the XML as `initial-geometry-failed.xml`. No claim of visual acceptance or successful complete suite is made from that run.

A read-only diagnostic rebuilt the same projected primitive positions and triangle coverage outside Unity. It reproduced the exact338/31 counts and classified each final occluder: near cheek67, near gusset54, flat header117, far cheek66, far gusset3, drum31. Thus the failure reflected actual projected overpaint, not an unsuitable oracle. For example the header's front plane covered a drum sample at projected `(-31.24658,-5.39613)`; the far cheek's long side also covered the central-right barrel. These diagnostic counts supplement, not replace, root's actual Unity result.

Two production corrections are confined to `FoundryRegionCraneHousing.cs`:

1. Emit the higher-design-u (far) cheek and gusset before the drum, then the near bearing. The far face can no longer paint through the nearer barrel.
2. Replace the full-depth rectangular header with a continuous cast arch whose central soffit rises to2.40 and top remains2.56. Its broad flat end bearings remain at2.17, over the real bevelled cheek cap, with .19 of design-u bearing width. Sloped inner shoulders join those seats to the raised centre; the rear remains joined to the same casing and hood. This creates actual aperture volume rather than making a roof transparent or moving it off its supports.

The original casing footprint, sole contact, maximum2.60 height, depth owner, baked resource/anchors, material order and renderer contract remain. The new profile adds44 vertices over the first proposal (estimated760 total added vertices). The1100 ceiling and35% visibility guard are unchanged. The same projection diagnostic predicts146/338 visible (43.2%) for the corrected profile, but this is not a Unity test result. Actual root preflight, complete suites, captures and baked-card/lantern image review remain required.
