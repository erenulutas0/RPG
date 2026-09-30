# Workshop and salvage dressing

## Integration

Call `BuildWorkshop(arena)` from `FoundryRegionView.Build` before the batch emission loop. The new partial is `FoundryRegionWorkshop.cs`; it uses only existing geometry helpers and borrowed materials. No runtime update, asset creation, collision, or scene changes.

## Content

- West salvage shelf: five beveled ingots in two staggered courses; one low strapped metal parts bin with an inset front panel and dull latch.
- North service shelf: three stored open-ended pipe lengths on two saddles; two retaining straps; a low rack with two tongs.
- Two masonry service shelves support the props rather than leaving stock floating in space. Low shelves are visually distinct from playable paving.

## Exact design-space footprint bounds

| Group | u range | v range | Clearance from walking union |
| --- | --- | --- | --- |
| West shelf and all stock | -20.7 to -18.45 | 5 to 8.4 | >= 0.45 from court west edge u=-18 |
| North shelf and all stock | 2 to 7 | 18.5 to 20.4 | >= 0.50 from court north edge v=18 |

All components, including rod thickness and crate panel offsets, fit within those shelf bounds. Heights: shelves 0.18 above floor, ingots <=0.63, crate <=0.935, pipe bands <=0.728, tool rack <=1.345. Shelf walls descend 2.8 below floor. Crane foundation remains u[-21,-18],v[11,15]; west shelf has a 2.6-unit gap to it. Neither group intersects the shaft or bridge. There is no implied traversable shelf or collectible content.

The five rectangles and bridge inequalities were inspected directly in FoundryMap. A separate 0.05-unit grid check over both complete bounds produced 7,113 samples and zero walking-union overlaps. This is a placement sanity check, not a Unity compilation or device test; integration owner runs those.

## Materials, rank and cost

- Existing FoundryWall -16/-14 and Floor -13 via Block for support shelves.
- All stock, straps, saddles, pipe mouths and tools use existing FoundryIron rank -11, already occupied by architectural ironwork.
- No new material, texture, renderer group or bitmap source is required.
- Estimated under 1,000 extra static vertices; integration's mesh census supplies the exact final combined cost.
- Parent must check actual crane-side and north-side Unity captures for composition and occlusion before APK delivery.

No Unity, dotnet or adb was run by this parallel agent. Only this report and FoundryRegionWorkshop.cs plus its meta were created.
