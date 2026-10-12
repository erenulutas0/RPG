# Remaining reference gaps — priority after ceremonial architecture

Read-only prioritization from the approved `foundry-layout-01/foundry-v1.png` and this package's actual `after/full-map.png`, with bridge/forecourt details used to distinguish real material differences from camera scale. This does not claim reference parity. No runtime or test execution was performed for this note.

## 1. The dominant floor still reads as one regular tile field

The reference's courtyard is organized by broad radial stonework, differently sized processional paving and worn joints. The current seal is better positioned, but its thin inlay and the quiet processional courses sit on a largely uniform large-square grid. This is the highest-area visible difference; more perimeter ornaments will not fix it.

**Source ownership:** `Assets/_Project/Scripts/UI/Arena/FoundryRegionCourtSurface.cs`, `FoundryRegionFinish.cs` (actual ashlar packing and medallion flags), `FoundryRegionSite.cs`, `FoundryRegionPavingWear.cs`, `FoundryRegionFlagContours.cs`. Existing material asset: `Assets/_Project/Art/Props/Foundry/MAT_FoundryPaving.mat`; any atlas change belongs to the material owner and must first resolve the actual material texture binding rather than assuming the highest numbered PNG is active.

**Bounded next slice:** one continuous entrance-to-seal paving composition: a substantial annular setting with actual radial block divisions, then larger processional flags meeting it with readable joints. Keep the new seal centre, pit clearance, map boundary and gameplay unchanged. Judge the block topology at full-map size and phone scale before spreading the treatment across the entire courtyard. This should change the stone arrangement, not just lighten it or add more gold.

## 2. Gate and lower perimeter still feel assembled from separate regular boxes

The reference uses deep, joined stepped masonry and supports of noticeably different mass. The current primary/secondary support pass improves hierarchy, but regular parapet courses, narrow vertical strips and large flat-capped gate towers remain prominent. The gate is recognizably the right landmark; its stone body still lacks the reference's continuous sculpted transition from terrace to portal and side towers.

**Source ownership:** gate `FoundryRegionGateEnclosure.cs`, `FoundryRegionGateHeader.cs`, `FoundryRegionGateRecesses.cs`; lower body `FoundryRegionUndercroft.cs`, `FoundryRegionBridgeSupports.cs`, `FoundryRegionFortressSurface.cs`, all under `Assets/_Project/Scripts/UI/Arena/`. Stone material: `Assets/_Project/Art/Props/Foundry/MAT_FoundryMasonry.mat`. Do not change the shared `GateStonePatch` mapping globally as a shortcut.

**Bounded subsequent slice:** prove one coherent gate-side assembly with a readable broad base, stepped shoulder and deeply framed upper face inside the approved footprint, then mirror only the successful structural language. Keep actual chain sockets, deck contacts and passage/depth ownership. Avoid another blanket colour pass or adding a small trim to every face.

## 3. The western workyard lacks the reference's strong industrial landmark grouping

In the reference the crane, loading machinery and dense working edge visibly balance the gate and boilers. In the actual full map the western machinery is much more delicate and widely separated, while the surrounding ground occupies more attention. The correct features exist, but their silhouette and grouping are less convincing. Simply populating the open combat area with small props would create noise.

**Source ownership:** `FoundryRegionBakedCrane.cs`, `FoundryRegionCraneHousing.cs`, `FoundryRegionProductionYard.cs`, `FoundryRegionBakedStockHoist.cs`, `FoundryRegionBakedStockStation.cs` under the same arena directory. The crane art is loaded by the `FoundryCrane` Resources key; its authoring/texture owner must be coordinated before any raster change. The existing baked boilers are owned by `FoundryRegionBakedBoiler.cs` and should remain a stable comparison in this slice.

**Bounded slice:** a single grounded west-yard group using the existing crane and approved work islands: visually substantial hoist housing/load seats, clear load-bearing contacts, and a strong readable cast-iron mass. Keep circulation clear and the existing crane/depth envelope; do not enlarge a sprite without rechecking physical alignment.

## Suggested next combined batch

Start with the **court paving topology** and **one west-yard landmark group** as separate owners. Treat each as one complete local composition and compare them together against the same full-map camera. Keep the gate assembly as the following structural slice rather than editing all three major systems at once. Freeze geometry/depth contracts and capture Before first. Success is a visible improvement at full-map and device scale, not a count of new trim pieces or a claim that the reference has been reached.
