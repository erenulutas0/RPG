# Northwest service skyline — source intent and handoff

2026-10-12. Baseline `2c2fa7c`. Scope is the two non-walkable northwest service shafts and their broken connecting arch. The approved whole-map reference has a clearer hierarchy between its broad court, western loading works and gate; the baseline's tall plain shafts compete with that hierarchy.

## Changed architecture

- The higher shaft's exhaust crown drops from -2 to -5.4 authored elevation. The lower crown drops from -4.4 to -6.4. World anchors, plan dimensions and original foundation bottoms -26/-25 remain fixed.
- Furnace sills retain their original -18.2/-20.2 elevations. The bay heads compress to -9.1/-10.6. Six narrow furnace panes per shaft keep genuine intervening gaps but are deliberately repositioned inside the shorter bays.
- Joined shoulder haunches rise 1.1/1.3 rather than 1.85/2.10 above the new lower shoulder. Their leaning buttress heads meet the same neck datum. Lower, less pyramidal profiles continue into the recessed necks and real hollow exhaust crowns.
- The broken span's endpoint elevations descend with the corresponding upper shafts to -7.4/-6.8. Its unchanged bearing shoes enter the retained furnace cores. Its severed center and different broken ends remain visible construction, not a new route.
- Material colors, atmospheric material value, renderer sorting/ownership, lower fade, other three distant tower variants and all playable geometry remain unchanged. This is a silhouette change, not a darkening pass.

Runtime file: `Assets/_Project/Scripts/UI/Arena/FoundryRegionDistantWorks.cs`.

## Geometry and tests

All existing primitives and tessellation counts remain; no extra resource or per-frame work is introduced. The combined distant architecture is expected to remain 3,572 vertices against the existing 3,600 cap. Root must measure this in the actual capture.

Updated `FoundryServiceShaftTests.cs` to exercise the production service-tower/arch helpers. Independent authored skyline envelopes verify the new crown heights and original foundation feet. The furnace test verifies the retained sills, exact six-pane geometry in the shorter bays, and meaningful open gaps. Head connectivity, hollow-mouth wall incidence, finite data and combined budget guards remain. One new case verifies both arch endpoints and springings are covered by their actual supporting tower meshes, while the broken center has no face.

The older exact fire-elevation assertions represented the former taller bays. Their intentional new authored geometry is documented above; counts, UV patch, gap constraints and containment are retained rather than loosened.

## Handoff and acceptance

Source is frozen for root compilation, Unity capture and the required complete gate. No Unity, ADB, isolated compile or Git commands were run by the author.

Inspect the full map and western service portraits: the two works should remain recognizable and uneven, but should leave more open space above the crane/loading yard and defer to the main gate. Check that the lowered arch remains attached, does not cut across the island, and retains its broken middle. Inspect hollow mouths and shoulder/neck contacts for gaps after compression. This source note does not establish actual-image, device or performance acceptance.
