# Facade support refinement — 2026-09-29

Scope: `FoundryRegionUndercroft.cs` only. The old single flat ribbon below each arch foot is replaced with a tapered three-face masonry pier, two shallow joining courses and a deterministic major/minor rhythm. Every third pier is deeper and broader; a terminal arch gets a closing support instead of an unsupported downward point. Major piers have small recessed-looking iron anchor plates with four restrained bronze bolt heads. All new vertices remain below the deck.

Existing FoundryWall / logical -16 and FoundryIron / logical -15.4 groups are reused. The iron group already belongs to furnace discharge casings; its explicit painter rank exposes plates above stone while remaining behind walking floor. No new material, texture, renderer group, animation, collision, walking union, gameplay change or scene edit. Shaft exclusion is retained.

I read the latest docs 08, 22 and 23 and inspected the previous full-map actual Unity capture. This is a source implementation pending root-run compile, real Unity render inspection and final suite/device gate. Look particularly at terminal piers, arch-foot intersections, silhouette length and iron visibility in the new whole-map, bridge and shaft closeups. Do not claim rendered approval from this source review alone.
