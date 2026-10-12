# Court source integration review

Root reviewed the actual Finish diff, the new CourtSurface helper and its two geometry cases before the Unity capture.

The shared light function and world-space paving sampler are unchanged. Only the kept ceremonial stones join the existing broader quarry-family treatment; the other zone branches retain their previous tone endpoints. The medallion's original outer polygons remain the surface boundary. Its shoulders and inset face partition those polygons, using the existing inset helper rather than adding a raised outline or another overlapping full face.

The new regression cases exercise a clockwise radial ring stone and a clipped curved end with opposite winding. They check projected area, containment, noncollapsed triangles, continuous world grain and opaque bounded colours. These are relevant to the new inset partition; existing medallion footprint and field-coverage guards remain in place.

No source blocker identified. Actual appearance, the original field vertex cap and the new geometry cases still require Unity verification. Check the final central floor for overly bright shoulders, a separate pale medallion disc or new dark outlines. This source review is not device acceptance.
