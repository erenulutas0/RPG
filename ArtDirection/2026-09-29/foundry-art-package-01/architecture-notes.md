# Gate and boiler detail pass

Changed only `Assets/_Project/Scripts/UI/Arena/FoundryRegionArchitecture.cs`.

The gate towers now have six dark inset panels with catching edges and sparse bronze fixing plates, three retaining collars per tower, and shallow hipped iron terminals. A small bronze forge diamond marks the raised central cornice block without covering the larger painted door seal. Boiler flues now have two retaining bands on each visible face, bronze fixings and soot-dark inspection slots. Existing fire-window artwork and all four open stack mouths remain intact.

The choice was restrained manufactured hardware against the existing stone rather than more stone courses or luminous ornament: the previous close-up already had strong masonry and painted brass doors, but the supporting towers and exposed flues lacked material contrast.

All additions use the existing shared FoundryIron material at order -10.5, one explicit additional batch above the stone architecture at -11. No new material, bitmap, frame update, collider, navigation change, camera edit or smoke-anchor edit. Tower tops rise from 7.25 to 7.36 within the original footprints; boiler mouth centres and 4.66 height are unchanged. Added geometry is 532 vertices (266 triangles): 276 gate vertices and 64 per boiler. Imported facade and existing masonry helpers are retained.

Code and whitespace diff inspected. Compilation, actual Unity gate/boiler captures, full combined integration tests, static budget and device verification are owned by the parent task and were not run by this agent. Inspect panel/collar contrast at portrait scale and equal-order painter composition during combined rendering; final acceptance must use the actual captures.

## Actual capture correction

The parent-generated gate close-up showed equal-order masonry covering the inset panels and hiding the lower faces of the iron terminals, leaving a pale chevron. Both new iron builders now use explicit order -10.5. Terminal feet overlap the existing stone top by 0.01, match its inset bounds, rise only 0.12 and use darker iron tint. Geometry count is unchanged. This revision was code/diff reviewed; a refreshed parent Unity capture is required for visual acceptance.

