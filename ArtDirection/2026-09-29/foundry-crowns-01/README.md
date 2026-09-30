# Gate finials and flue braces - 2026-09-29

## Scope

`FoundryRegionArchitecture.cs` adds supported cast finials to both gate tower terminals: a metal socket, restrained bronze collar, and tapered upper shaft. Each of the four boiler stacks gains two visible triangular shoulder braces with small cast feet and fixing marks. Existing chimney mouths, smoke origins, painted facades, and scenery footprints remain intact.

All additions use the existing iron material/batch. No new texture, runtime update, light, gameplay, collision, scene asset, or playable camera change. Static budget is 17 renderers / 52,556 vertices (previous 51,780; +776); atmosphere remains separate at 1 renderer / 780 vertices.

## Review

The gameplay portrait at the gate (`point-4.png`) shows the flue braces and clear space around the hero. Gate finials are above the top of this close-follow frame; their strongest effect is in wider compositions. No promise that this detail is continuously visible during phone play is implied.

The capture-only gate camera was expanded from orthographic size 8 to 8.8 and centered at (19,16.4), to retain the taller finials in frame. A fresh baseline was captured with this framing before runtime geometry edits. Before/after gate images therefore share the same framing. Furnace and overview captures use their unchanged review cameras.

Actual five-waypoint Unity graphics traversal passed. Root inspected the gate and furnace details; no rim obstruction or visible unsupported brace found. `walk.csv` and `static-budget.txt` preserve the capture evidence. Compile: 0 warnings/errors. Static integrity: 514 project GUIDs / 1,099 package GUIDs / 568 scene objects/components. Full test/build outcome follows below.

The previous arch and paving slices are included in the working tree. No phone operations in this slice; last verified installed APK remains `18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535`. No helper agents, commit, or push. This refines the landmark silhouette, not a claim of final reference-image parity.

## 2026-09-29 - Gate finials and boiler shoulder braces

Added two supported gate finials with cast sockets, bronze collars, and tapered tips, plus two visible roof-mounted shoulder braces on each of four boiler chimneys. Existing mouth/smoke anchors, painted facade, footprints, and static iron batches retained. Capture-only gate framing expanded; baseline recaptured with the same new review framing before runtime edits. Gameplay camera unchanged. Root reviewed matched gate/furnace details, overview, and gate portrait. Finials sit above the close-follow gate portrait; their main impact is the wide silhouette. Proof: ArtDirection/2026-09-29/foundry-crowns-01.

Static 17 renderers / 52,556 vertices (previous 51,780); atmosphere separately 1 / 780. Graphics traversal 1/1 passed, compile 0 warnings/errors, integrity 514/1099/568. Full gate EditMode 405/405, PlayMode 130/130, Android build exit 0. APK actual size 39,340,606 bytes, local build 2026-09-29 16:57:43, SHA256 6DBD8583D538C3C77458F1DBFA8823D20146FEDF3D5E0C284E241BE373158EC8. READY, NOT INSTALLED. Includes the prior uninstalled arch and paving slices. Last verified installed remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone actions, save changes, helper agents, commit, or push. Reference-quality art remains in progress.