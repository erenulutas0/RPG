# Conforming flue hardware

The actual furnace close-up showed retaining-band ends extending past the flue's cut corners. `BuildFlueIronwork` previously reused a fixed untapered plane at both band heights, while the stone body narrows and has octagonal corners.

Changed only `FoundryRegionArchitecture.cs`. Hardware now interpolates between the same lower/upper octagons used by the flue tier (bottom 3.62, top 4.47, bounds 0.86–2.14 to 0.93–2.07, corner 0.18 to 0.135). Each strip samples its lower and upper elevations independently, with small endpoint margins on the straight faces. Inspection slots and their catching edges follow the same surface. Bolt centres follow the corrected band.

The count remains eight quads per visible face, two faces per boiler: no vertex/index-count or batch-rank change. Existing material, chimney mouth at 4.66, smoke positions, stack geometry and gameplay remain unchanged. Compile check passed with zero warnings/errors. Root handles actual render comparison and combined verification. No Unity, adb, commit or push performed here.
