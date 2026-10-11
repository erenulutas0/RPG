# Active casting station source

2026-10-11, authored against runtime baseline `3044b83`. New editable native Blender source and transparent render; earlier workyard-clusters-01 evidence is untouched.

The active flask now contains a real annular bearing cavity around its sand core, connected to a pouring well by a recessed runner. Dark heat-facing sand and non-emissive cooling bronze distinguish it from the separate angled ingot mould and closed queued flask. A hollow ceramic pouring vessel is seated in a three-foot cast stand beside three uneven cooled billets. The press, supported oak bench and existing flask fittings remain.

Run `Tools/ArtReview/BuildBlenderCastingBench.py` with portable Blender 4.5.9 in background mode. `casting-bench.blend` is the editable model; `casting-bench.png` is its 1024-square RGBA render. `projection.json` records evaluated geometry bounds and the unchanged frame/depth contract. The tighter established source bounds remain asserted. `source-checks.json` verifies matching baseline framing, transparent corners and output hashes. Native mesh counts are not runtime mesh counts.

Both before and final source renders were visually inspected. The circular bearing and runner are visibly distinct from the ingot impression; the pot is visibly hollow, and the supported press/bench arrangement remains. The rear stand is partly hidden in the fixed projection, so source geometry also establishes the vessel support. No baked emission, floor or shadow catcher is added. Root owns Unity import, whole-map/actor-height review and device verification; no runtime or save file was changed here.
