# Forecourt boundary architecture integration

Source implementation for the West -36 to -32 / South -14 to -18 layout package. This note is source evidence; actual After-image and device acceptance remain separate.

## Changes

- `FoundryRegionMassing.cs`: three south finials follow the current south edge; their above/below-deck facing classification uses current bounds. Both south bastions and their connecting chain sockets move with the perimeter. The long south abyss chain now requires an actual emitted major-pier socket.
- `FoundryRegionFinish.cs` and `FoundryRegionSite.cs`: visible court lanterns and their static baked-light sources share one placement list. Bridge lantern rows are spaced between the relocated keeper head and fixed bridge mouth, preserving two rows/four cages. Existing depth landmarks already consume these same roots.
- `FoundryRegionDressing.cs`: the small hanging south chain moves with the deck edge.
- `FoundryRegionView.cs` and `FoundryRegionFinish.cs`: the southern brass guide and the packer's forced joints both use `ForecourtGuideV = South + 4`. The pitward vertical return extends to the new guide. The long bridge guides and gate guides remain fixed. Independent review found and corrected the initial mismatch where only the visible brass had moved.
- `FoundryRegionBridgeSupports.cs`: the three existing below-deck bents span the shortened exposed bridge. Their first row follows the keeper head; the last remains at the fixed bridge mouth. Their returns and outer shoulders remain south of the court plane.
- `FoundryRegionUndercroft.cs`: choose the long south-chain eye from the actual emitted major-pier list, restricted to the current south edge and unscreened iron plates. Equal-distance candidates choose greater u deterministically. No eligible socket means no floating fallback chain. The eye is 3.9 below the local deck, inside the existing plate's 3.05–4.25 drop.

The western crane/shelves, production islands, rail light sources and relevant depth footprints already use `WestAssemblyOffset`/`WorkyardShift`, so they need no second offset. Corner clipping, court sidewalls, bridge floor clipping and parapet seam geometry already derive the changed bounds.

## Validation and cost

Three additional cases in the existing `FoundryBridgeArchitectureTests.cs` guard actual finial masonry and registered depth ownership, visible lantern/baked-light coherence, bastion exclusion from the walking union, bridge fixture spacing and support extent, and rejection of absent/minor/stale/screened chain sockets with order-independent ties. Existing assertions are retained. Root owns actual scene/mesh coverage and actual-chain/socket PlayMode validation.

Isolated compile: `dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --no-restore --verbosity quiet` passed, zero warnings/errors. No Unity, ADB, Git or device operations were run by this helper.

No new texture, material, renderer family, animation or per-frame work. The existing posts, lanterns, bastions, chains and bridge support counts are retained; the chain selection is one small build-time pass. Total mesh cost can change because boundary-derived floor packing, rail segmentation and undercroft spans are rebuilt. Root must measure the combined actual result rather than infer a vertex delta from this source note.

## Review limits

Confirm the wider forecourt composition, new south edge, keeper/lantern separation, exact bridge contacts and the long chain's borne socket in actual matching captures. Source/compile success does not establish image quality, physical-device movement, crowd overlap, shimmer or performance.
