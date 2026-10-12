# Independent court source review

Read-only review of the frozen court assembly and its tests. No source changes, compile, Unity or device operations by this reviewer.

Decision: no blocking source issue found. Actual capture acceptance remains required.

- The medallion translation is local: `CourtSealCentre = (-3,-5)` and `CourtSealPoint` affect its field, ring and emblem while global `SealPolar` keeps vector-only semantics for gate and yard consumers. The retired origin's special paving zone and old angular brass joints are removed, reducing the risk of a duplicate architectural centre.
- The 5.05-radius stone extent stays below the fixed pit: its northernmost design coordinate is 0.05, compared with the pit boundary at 2. The expanded setting remains on ordinary court floor and away from the work islands. The larger area is stone; compass arms remain at their previous lengths and the metal setting stays thin.
- Entrance paired settings terminate within the shared bridge union. Their lateral offset does not reach the bridge side rails, and the new tests sample triangle edges through the concave union rather than checking endpoints alone. Their interior travel strip remains plain floor.
- Gate-directed settings remain far below the actual elevation ramp start at u=25. Their furthest vertex is below u=17, so no unsplit elevation break is introduced. The gate-side junction with the existing processional field should still be judged in actual images for a natural fitted transition.
- CourtDressedStone uses projected floor vertices, bounded inset geometry, world-continuous mineral UVs, the shared half-unit lighting lattice and explicit alpha 1. The same floor batch owns the new setting; there is no new raised blocker, depth footprint or material. Seal metal retains the existing bounded floor-decal lighting path and weathering.
- The two added test cases inspect actual emitted coverage, old-origin absence, pit clearance, grain and alpha, full paired-setting beds, a clear central travel field and bridge-transition edge coverage. Existing fitted-field and compass cases were updated for the moved assembly; the generic packer keeps its synthetic coverage tests and explicitly guards retirement of the forced joints.

Visual acceptance should establish that the enlarged stone medallion anchors the new forecourt without looking like a combat telegraph, that the paired stone settings do not become bright rails, and that their gate junction reads as fitted stone. Static source review alone cannot establish these qualities or phone framing.
