# Painted surfaces 02 (2026-09-30, evening)

**Supersedes** `../painted-surfaces-01/PROMPTS.md`. Pack 01 predates today's rim, seal, bridge, chain, cylinder,
depth/massing, NPOT and UI-panel changes; parts of it target dead code and its memory table is wrong. Don't generate
from 01 (what changed: `PROMPTS.md` §4).

`PROMPTS.md` contains: the status of every surface at the portrait camera (§1); five ranked items with paste-ready prompts
(§2): P1 court flagstones, P2 balustrade trim, P3 foundation masonry, P4 pit lining, P5 boiler drum wrap (optional, after
P4); and the renderer-budget choice (§3).

**Owner steps**
1. Pick §3 option A, B or C. It is needed before P2 is integrated, not before generating.
2. In one ChatGPT image chat, attach `foundry-v1.png` (plus `FurnaceFacade.png` for P4/P5) with the §2 reference sentence,
   then paste P1, P2a, P2b, P3, P4, P5, one per message.
3. Save each untouched output with its prompt under `ArtDirection/<generation date>/painted-surfaces-02-masters/`
   (`P1-raw.png` + `P1-prompt.txt`, …). Don't resize or edit; the session crops, heals seams and scales to power of two.
4. Tell the session which masters to integrate. It imports (never during a Unity test run), captures the review points
   and asks before any phone install.

**Pending:** all generation; the §3 decision; the P1–P5 integration code (`FoundryRegionFinish/View/Massing`,
`PlatformMaterialSet`); later items from §1 (one foundry floor-decal atlas, iron recolour, brass tint, pit-collar trim,
lantern-pane colour).
