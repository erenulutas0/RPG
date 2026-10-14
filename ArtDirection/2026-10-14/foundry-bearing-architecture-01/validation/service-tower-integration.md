# Native service-tower bearings

Source ownership is limited to `FoundryRegionDistantWorks.cs` and six new cases in the existing `FoundryServiceShaftTests.cs`. The two service towers retain their stations, original top/foot datums, shoulder contacts, furnace bays, six separated fire panes, arch springing contacts, open exhaust mouths and scenery sort/atmosphere ownership.

The two visible outer faces of each exhaust crown now form a shallow recessed fascia between sloping lower and upper bearings. All bands use shared ring stations, so the near corner joins continuously. The exact outer top and bottom rings, existing annular rim, inner walls and recessed throat remain. The fascia moves inward by 0.075 design units, leaving at least 0.145 units of stone before the unchanged throat. This replaces the former dressed flat faces; it does not add a second wall or cap over the opening.

The complete head now uses one bounded physical mineral field, reusing the existing broad joint-free stone patch. The same scale and coordinates cross its shoulders, inset returns and crown; each narrow face no longer restarts/stretches the whole patch. No source texture or material is added or changed.

Only service variants 1 and 2 receive a broad neutral bearing response in `FinishDistantStone`. Exposed haunches regain more quarry value than the neck/crown; existing directional face tones, atmosphere, damp-foot fade and localized furnace warmth remain. The three other distant works retain their previous finish. This is static vertex-color generation, not another light or per-frame component.

Each visible flat crown face formerly used 20 vertices through its dressed partition. Its three replacement bands use 12. Across two visible faces on each of two towers, the expected distant delta is **-32 vertices**, from 3,572 to **3,540**. Crown-local index count falls from 210 to 186 per head; no complete-scene index delta is claimed until measured. The existing 3,600 distant ceiling is unchanged. No renderer, resource owner, material, texture or per-frame work is added. Root's actual scene capture must confirm the full resource count; static counts are not device performance measurements.

New tests inspect both actual crown sizes for shared recessed corners, unchanged bearing/mouth rings and bounded counts; both complete head variants for one physical, noncollapsed UV field on referenced surfaces; and both heads' emitted material response after removing the unchanged atmospheric/fire field. Existing connected assembly, sky-line envelope, six-pane, actual mouth and broken-span attachment tests remain intact.

At source freeze this helper has not run Unity, dotnet, ADB, Git or any shared compilation. Root owns the compile, matched After capture and full regression gate. The larger composition/reference gap and phone motion/performance acceptance remain open.
