# Independent V8 bitmap candidate review

Decision: **suitable for a calibrated actual-scene proof; final art acceptance pending**. This is read-only review of the generated source, not acceptance of a production import, a runtime capture or a phone result. No bitmap was edited or reconstructed. Comparable analysis is preserved in `paving-v7-v8-value-comparison.json`.

## Inputs and visual judgment

- V7: `Assets/_Project/Art/Props/Foundry/FoundryPavingV7.png`, 1254 x 1254 RGB, SHA256 `57E938EB1426675C002374C0E3E769349D7BCDF94E36102432F5226F704A29A5`.
- V8: `source/paving-v8-candidate.png`, 1254 x 1254 RGB, SHA256 `F4F24DAF3578CDAF753BDB4CC9ACC1917E57948AF03FD021200202365BB88BCF`.
- Generation brief: `source/paving-prompt.txt`, built-in image generation edit using V7 as the reference. The reviewer inspected both source bitmaps and read the prompt.

V8 visibly removes the long connected mineral waves that dominate V7. The replacement has short, irregular inclusions and pores with a substantially less consistent direction. It does not add painted flag joints or a new slab pattern. This is the right kind of material change for the existing laid geometry.

The qualification is its finer grain: at raw size the new stone is more granular and contrasty, with small light-rimmed inclusions and dark pores. It is not uniformly calm at full resolution. Diagnostic reduction shows that most of this added contrast is fine-scale rather than broad cloudy structure, so the candidate deserves an actual imported-scene comparison. Do not accept a gravelly/speckled runtime floor merely because long streaks are reduced. Conversely, do not reject raw-source pores without seeing the actual filtered scale.

## Comparable value and structure measurements

All encoded luma values use `0.2126R + 0.7152G + 0.0722B` on PNG values divided by 255. They are not linear luminance or rendered scene measurements. Downsizes use Pillow Lanczos as a diagnostic only, not as a simulation of Unity import, mip generation or ASTC compression.

| Source measure | V7 | V8 |
| --- | --- | --- |
| Mean encoded RGB | 0.477054 / 0.467065 / 0.448469 | 0.500068 / 0.478653 / 0.453054 |
| Encoded luma mean | 0.467846 | 0.481358 |
| Encoded luma standard deviation | 0.054426 | 0.069045 |
| Encoded luma p5 / p50 / p95 | 0.383444 / 0.465235 / 0.561838 | 0.377835 / 0.476074 / 0.603854 |
| Mean linear luminance after sRGB decode | 0.188936 | 0.202906 |
| 1024 diagnostic luma standard deviation | 0.052742 | 0.067389 |
| 256 diagnostic luma standard deviation | 0.033085 | 0.036396 |
| 64 diagnostic luma standard deviation | 0.020323 | 0.016129 |
| 16 diagnostic luma standard deviation | 0.010714 | 0.004710 |

At 1024, standard deviation rises about 27.8%; at 64 it falls about 20.6%, and at 16 about 56.0%. That supports the visual interpretation of more fine inclusions with less broad variation. It does not prove that phone motion is free of shimmer.

At 256, a simple central-difference gradient tensor has major/minor eigenvalue ratio 1.767 for V7 and 1.208 for V8; normalized anisotropy falls from 0.277 to 0.094. Wrapped X autocorrelation at a four-pixel lag falls from 0.258 to 0.057. These are diagnostic measures of reduced common direction, not aesthetic pass/fail thresholds. A small local direction remains, as expected of natural material.

Repeat-edge luma mean absolute error divided by ordinary adjacent-pixel error:

| Diagnostic size | V7 X / Y | V8 X / Y |
| --- | --- | --- |
| 1024 | 1.378 / 0.951 | 1.181 / 1.065 |
| 256 | 1.364 / 1.069 | 0.894 / 1.099 |
| 64 | 1.262 / 0.938 | 1.101 / 0.925 |

V8 has no numerical repeat-edge discontinuity that warrants blocking the scene proof. It is not certified seamless by these averages: real repeated floor areas and lower mips must still be inspected for visible boundaries or repeated distinctive clusters.

## Recommended initial material calibration

V8's encoded mean is **2.888% brighter**. Retaining the current neutral `_Color = 0.604573943` would raise the mean pre-vertex product from 0.282847 to 0.291016. Recommend a neutral scalar of **0.587603232** for the initial proof, calculated as `0.604573943 * V7Mean / V8Mean`, preserving the current mean to first order in the project's Gamma workflow. This keeps brightness gain from masquerading as material improvement.

That scalar does not neutralize hue. After mean matching, average RGB changes versus V7 are approximately +1.38 / -0.28 / -1.25 channel levels out of 255. The new source is modestly warmer. This is small enough to assess in the existing cool court first; there is no source-only justification for a separate coloured correction yet. Inspect the quiet combat centre, violet golem, brass and gate approach, and adjust only if actual matched output demonstrates unwanted warmth or a lost value hierarchy. Do not change FloorLight, regional tint or UV span to compensate pre-emptively.

Final acceptance needs the real Android-target 1024 / ASTC_6x6 / 11-mip import with unchanged UV geometry and matched native full-map, court, approach, bridge and workyard captures. Retain the existing resource ceilings, map/navigation/depth contracts and pending phone/crowd limitations. No phone or performance acceptance is made here.
