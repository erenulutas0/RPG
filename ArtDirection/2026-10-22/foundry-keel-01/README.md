# Foundry platform: hanging keels below the deck

Verified 2026-10-22. Baseline `84cbd2112e4058869965a7bdbda777fd83f390ac`.

On the approved concept the court does not end in a level skirt: clusters of stepped, inverted masonry masses hang on
below its edges into the violet haze, so the platform reads as a floating fortress. The runtime underside ended 7.25
below the deck in the foundation curtain's contracted skirt, with arcades, buttresses and a few bastions above it and
nothing beneath.

- `FoundryRegionKeels.cs` (`BuildHangingKeels`): under every camera-facing foundation span (the curtain's own, now
  shared through `FoundationSpans()`), keels 1.4-3.2 wide hang from just inside the skirt's foot (7.1 down) in two or
  three steps 1.1-2.6 deep, each narrower than the one above and mostly set back under it; about one step in three
  stands proud of the one above and shows a lit ledge. About half the keels butt their neighbour, the rest leave a gap
  of haze. Faces are the bastions' weathered masonry (`WeatheredBoxFace`, world-continuous courses), lit toward -v and
  shaded toward -u; their tone falls into shadow below the deck and then into the cavern haze by 16 down, so the
  deepest keels (~15) dissolve into the void.
- They open the below-deck masonry batch (FoundryWall, -16): emitted before "Layout sidewalls", so the curtain, arches,
  buttresses, bastions, bridge bents and shelf foundations - all in front of them - paint over them. None under the gate
  causeway, where the boilers' foundation drums and the gate's foundations stand behind the edge; none on a chamfer
  seen edge-on. Deterministic: keel layout is hashed from each span's coordinates.

No new batch or texture. Region 38 renderers / 159,005 vertices (+1,064).

Images: `before/`, `after/` (35 matched views; new: `underside-south`, `underside-west`), `compare-concept.png`
(map-scale before / after / concept). Candidates were iterated on the same views and not kept: the first keels used the
curtain's coursed quarry faces with a strong haze and read as flat dark cards; the bastions' masonry, a lighter depth
fade and the occasional proud ledge replaced them, and the steps were deepened from one-to-three to two-to-three.

Tests: new `FoundryKeelTests` - bounded valid mesh within the fixed4 colour range, no actor depth; more than 40 faces,
every face hanging from at least 7.1 below the deck and no deeper than 16, the deepest below 12; never under the gate
causeway; every face under the walking surface (a little inward of it).

Verification: 457 .NET / 737 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,458,123 bytes, SHA256
39A753D5C24FB46DC08314A1C05AAA24B92A5CE02417E05E9244B7FF3FBB324B, NOT INSTALLED (the owner is using the phone). Source
freeze `d66e5e020112efd7144e18dc7e5ff97b07f5f98330dc4f8874799e8a76f557f9` (1,269 files) taken after the final After capture,
matched after the full suites. Gameplay: none.
