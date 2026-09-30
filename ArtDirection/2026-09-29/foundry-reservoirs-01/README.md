# Foundry pressure reservoirs - 2026-09-29

Two exterior pressure tanks connect the existing boiler-pair service runs. Centers (22.3,-5.8) and (22.3,15.8); 2x2 masonry feet remain outside the shared walkable apron. Each has a supported metal shoe, 12-sided cylinder, retaining belts, tapered shoulder, relief stem/wheel and short branch into the existing main pipe.

Runtime changes only in FoundryRegionDressing.cs. Existing masonry, iron and batches reused. No new bitmap/material, scene, gameplay, collision, camera or per-frame behavior. No helper agents used.

## Visual corrections

Initial whole-body-then-belt construction painted belt caps over the upper vessel. Final geometry is authored bottom-to-top with cylindrical sections between belts. Rear tank emits before the main pipes; near tank after them. Branches start at the tank wall, not its center. Actual final Unity furnace closeup reviewed after correction.

## Verification

Graphics-enabled five-position capture passed 1/1. Static17 renderers /59,584vertices (+2,024 from57,560). Atmosphere remains separately1/780. Compile check before painter-order correction0warnings/errors; integrity515/1099/568 passed. Full gate follows below.

Before images preserve the installed lantern package. After images are actual Unity captures. New tank package is not installed or device-tested in this session. This is scenery; tanks have no gameplay interaction. Reference-image fidelity remains in progress.

## 2026-09-29 - Pressure reservoirs integrated and verified

Two exterior tanks at (22.3,-5.8)/(22.3,15.8), each on supported2x2 masonry feet, now connect existing boiler service mains. Added faceted metal hulls, retaining belts, tapered shoulders, relief stems/wheels and short wall-to-main branches. All feet outside walking union. FoundryRegionDressing only; existing materials/static batches. No gameplay, collision, scene, camera, per-frame work or new raster assets. No agents used.

First visual review caught belt caps painting over the upper hull; corrected bottom-to-top course order. Near tank now emits after pipes, far tank before; branches begin on tank wall. Final actual five-point Unity capture1/1 passed; furnace closeup/overview reviewed. Static17renderers/59,584vertices (+2,024), atmosphere1/780 unchanged. Full gate EditMode405/405,PlayMode130/130,Android exit0. Compile/integrity earlier passed0warnings/errors and515/1099/568. Preview HTTP200,7/7 local links. Proof ArtDirection/2026-09-29/foundry-reservoirs-01.

APK actual39,340,606bytes, localtime2026-09-29 20:15:24, SHA25679B4C59EE5E4D7AA44BA53663228BA9D198BD7920DCCD1F004B7F0CDB7E62DC1. READY NOT INSTALLED. Last verified installed phone package1C4A1D885F27C7A5B2DFDA74FF16B990D24BC0193887DD27229C34C3EECA6688 remains; prior paused phone snapshot is historical until reinspected. No device operations/save edits/commit/push. Reference-image fidelity remains unfinished.
## 2026-09-29 - Reservoir package installed on S23

Owner explicitly authorized restarting the advanced Forge-choice run (40HP,5unbanked gold). Fresh backups under TestResults/foundry-reservoirs-install-20260929/before. adb install-r Success; installed base.apk SHA25679B4C59EE5E4D7AA44BA53663228BA9D198BD7920DCCD1F004B7F0CDB7E62DC1 matches local. All7 saved files byte-identical immediately after install. Supersedes pending installation above.

Startup run ended in Slag Gate defeat with0banked gold. Restarted through Try again; guarded northeast drags reached the boiler/tank group. Actual08-tank-detail.png confirms near reservoir hull, base, pipe connections and surrounding furnace art on S23. Movement/camera follow worked. A visual limitation is especially apparent here: foreground actor/ring rendering can make the hero look on top of projected boiler/pipe silhouettes while the walkable bounds remain unchanged. Record this as the next depth/readability issue; it was not fixed or claimed accepted in this device-only turn.

Left PAUSED in Cinder Walk2/6,wave1/3,77HP,level0,XP3/10,0runGold,Staff/SecondWind. Profile revision215/32776gold/deepest3 unchanged; profile,backup,two development flags,rotated telemetry byte-identical after review. Active telemetry retains exact prior prefix. Current-process log0matches for FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException. No save restore/edit/equipment purchase. Short art/movement smoke, not sustained performance or full-map acceptance. All input/capture gated by focus/awake/no-keyguard; post-capture focus rechecked. Evidence copied unchanged to ArtDirection/2026-09-29/foundry-reservoirs-01/device-tank.png. No code/build change,commit,push. Reinspect before future phone actions.