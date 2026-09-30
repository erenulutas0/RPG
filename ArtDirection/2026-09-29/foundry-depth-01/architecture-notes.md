# Local reflected furnace light on stone

Changed only `FoundryRegionArchitecture.cs`. New `LitFoundryTier` wraps the unchanged `ArchitecturalTier` exclusively in `BuildGateArchitecture` and `BuildBoilerArchitecture`. Iron caps and other callers in Site/bridge/shaft keep their original colours.

The wrapper adjusts existing vertex RGB after construction. Strength follows the tier centre's squared design-plane distance to the four existing furnace-window positions, plus a weaker contribution from the two gate braziers. Reflected warmth fades with estimated elevation and reaches zero at 3.5 units, so plinths and corner shoes receive most warmth and upper crowns retain their cool ambient. Original directional shading and alpha are preserved. This is restrained vertex-colour art, not physical lighting or a new dynamic light system.

No positions, triangles, UVs, materials, shaders, textures, batch ranks, camera, gameplay or per-frame work changed. Geometry and renderer counts must remain unchanged. Compile check passed with 0 warnings/errors. Root will inspect the actual before/after furnace and gate captures, run combined verification, and handle device work. No Unity or adb executed by this contributor; no commit or push.
