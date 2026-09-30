# Foundry reference pass — 2026-09-30 (late)

Three helper agents (each owning its own files). Matched actual Unity captures: `../foundry-rail-01/after/` (before)
and `after/`; `index.html` also sets the overview beside the approved reference.

## What changed

- **Floor light calibrated to the reference** (`FoundryRegionFinish.cs`). Measured on `foundry-v1.png` vs our
  overview: reference open court (81,76,88) violet-grey, only ~15 % of the court warm and ~1 % hot, brazier pools
  rose-tinted with a ~2.3-2.5 unit half radius; ours was ~34 % warm and ~24 % hot, brown overall. Now the open court
  is violet-grey (hue ~263-275), pools are tighter (pit 3.8, boilers 4.2-4.6, lanterns 3.6), lantern light is rose,
  and the pit rim / boiler aprons stay orange at the gameplay camera. Modelled warm share ~21 %. Shader worst case 1.86.
- **Floor decals take the light** (`FoundryRegionView.cs`, `FoundryRegionHeraldry.cs`). Every flush decal (brass lanes,
  compass, seal, kerbs, coping trim, bridge kerbs, gate threshold) is lit by `FloorLight` at emit; brass keeps a little
  more light in the cool court and glints more in pools. Long strips are split into 1-unit pieces so the light varies
  along them (+980 vertices). The compass's four cardinal lozenges now map onto brass (they sampled a dark corner).
- **Chains into the abyss, bridge head, finial order** (`FoundryRegionMassing.cs`, `FoundryRegionDressing.cs`,
  `FoundryRegionSite.cs`). Six new heavy sagging chains from the rim, bastions, bridge gateposts, an undercroft pier and
  the gate foundation (nine in all), darkening with depth; the court-side bridge gateposts carry fire braziers and
  hang blue banners cut from the gate facade's painted banner (same batch as the gate, no new renderer); the court
  finials are now painted in far-to-near order with the balustrade (fixes the rail painting over their lower front,
  0 overlap violations in a triangle test), and each lantern post takes its own depth.

## Budget and gate

- Static budget: 17 renderers (limit unchanged), 59,008 -> 63,644 vertices.
- Full gate: EditMode 410/410, PlayMode 134/134, Android build exit 0; integrity 524 / 1,099 / 568 passed.
- APK 37,022,536 bytes, SHA-256 `8F4C2C76A123C4E22293B4DF610BA0B224B3F78367F526423197CA1B90451F42`, **installed** on the
  S23 (owner said the phone was free; Chrome in front, game not running; hash matches; profile revision 221 unchanged;
  backups in `TestResults/foundry-refpass-install-20260930`). `device-start.png`, `device-pit-corner.png`: the violet-grey
  court with lit brass compass and lozenges, the pit corner, and the bridge-head brazier at the screen's lower edge.
  White figures in them are the existing hit flash. Logcat clean; app sent Home.

## Limits

- A thin bright orange line appears along the kerb by the squat boiler (`furnace-detail.png`): brass kerb glint in the
  boiler pool; lower `BrassGain` if it reads as an ember seam.
- Golems (violet-grey) now share the floor's hue; their lit faces, lava cracks and outline still separate them.
- Banners hang low beside the gateposts rather than on tall poles as in the reference (poles would need a new renderer).
- Pool edges still step slightly flag by flag (light is sampled at flag corners).
