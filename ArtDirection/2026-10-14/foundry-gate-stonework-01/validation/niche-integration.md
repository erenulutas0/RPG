# Coursed niche shaft integration

## Source scope

Only `FoundryRegionGateRecesses.cs` changes runtime presentation. The tall niche body and its splayed frame now use three broad stones per jamb. The body sill and upper shoulder bearings remain single stones; two fitted segments form each sloping side of the frame's pointed head. The existing outer contour, frame/throat/backing stations, first forty backing/return vertices, and final shaft corner/cap are retained.

Each original face is partitioned into stone and narrow mortar owners. A common normalized half-joint width of 0.0045 makes the jamb cuts meet exactly across the body/frame contact, including tapered production faces. No retained flat bed, extra decal, new texture or micro-brick grid is introduced. Three restrained stone gains (0.985/1.015/0.995) remain neutral; joint gain 0.72 describes the construction. The existing directional splay gain is still applied to the real rendered frame.

A niche-only indexed stone emitter uses one four-corner outer ring and one inner ring. Interpolated corner light describes its small dressed arris. This allows twenty fitted stones and ten mortar strips per complete recessed face while retaining the old 240 vertices: 20×8 +10×4 +40 unchanged backing/return vertices. A shaft retains 516 vertices including its old closed corner/cap. `GateRecessDressedFace`, `GateRecessGrain` and `GateStoneContact` are unchanged for their other callers. One grain pass spans each complete original body/frame face, crossing joints and arrises without a restart.

## Geometry cost

Vertex count is unchanged, but triangulation is not free: each recessed face now submits 240 triangles instead of 120. Four existing two-faced shafts imply 960 additional triangles / 2,880 additional indices for the complete region, subject to root's actual capture measurement. No new renderer, material, texture, shader, component, per-frame updater or resource ownership type is added. The existing 174,000-vertex region budget and focused 240 / 550 / 2,400 limits remain unchanged.

## Tests

`FoundryGateRecessTests.cs` retains existing original-envelope, cap/corner, backing/throat visibility, full-frame area, one-owner, splay-colour and noncollapsed-grain assertions with their existing tolerances. It derives the actual frame span from semantic frame parts instead of assuming a 140-vertex body prefix and 20 vertices per frame. The production shade comparison also now checks unchanged UVs.

Two added production-profile cases cover both visible sides of the inner and outer shafts. They require the actual body/frame stone and mortar owners to share each joint boundary; sample visible mortar against neighbouring broad stones on both jambs and both pointed-head sides; and verify an affine texture field across those joints. No test accesses internal runtime types directly across the EditMode assembly boundary.

Root owns compilation, Unity capture/suites, Android build and acceptance. No helper Unity, ADB, device or Git operation was performed for this source slice. Actual imagery must confirm readable assembled stone at portrait size without a bright checkerboard; reference parity is not claimed.

## Independent source review

The independent reviewer found no source blocker: matching parametric body/frame cuts, unchanged backing/return prefix and cap, and existing coverage/shading oracles were confirmed. Indexed corner interpolation is an intentional local change. Final matched images must still reject excessive horizontal striping or checkerboard contrast.

## First-suite correction

The first full EditMode run found two strict projected-backing overlap failures from float rounding of new split throat-edge points. The runtime now conservatively rounds only those intermediate frame-inner stations toward the stone, retaining exact original endpoints and common mortar/stone coordinates. No geometry-area tolerance was relaxed, and assertions in the existing two production-course cases now bound the emitted correction to one adjacent float per coordinate. See `niche-boundary-repair.md` and its preserved numerical evidence. Actual targeted Unity EditMode rerun passed all 24 niche cases, including both previously failing area cases (`recess-repair-targeted.xml`). Full package and device acceptance are not inferred from that targeted result.
