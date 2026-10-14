# V7 paving integration candidate

Status: implementation candidate; Unity visual and runtime acceptance remains with the root agent. Baseline: `66b83f5`, twenty sealed views. No Unity, ADB, build, Git, or gameplay operations were run by this helper.

## Scope

- Added `Assets/_Project/Art/Props/Foundry/FoundryPavingV7.png` as an exact byte copy of `sources/slate-v7-source.png`; no image processing, resizing, colour conversion, or recompression.
- Added its `.meta` by copying the V6 importer and replacing only the GUID with `77de59edd81d40bea9124bf37f4ba5aa`.
- Changed only the `_MainTex` GUID and neutral `_Color` RGB scalar in `MAT_FoundryPaving.mat`. Alpha remains 1. Shader, UV scale/offset, all other material settings, camera, geometry, light/contact fields and all source code remain untouched.
- V6 PNG and meta remain unchanged. Root owns `FoundryRegionTests.cs`; this helper added no tests that merely mirror the new asset name or GUID.

## Provenance and mean matching

V7 source/runtime PNG SHA-256: `57e938eb1426675c002374c0e3e769349d7bcdf94e36102432f5226f704a29a5`.
V6 PNG SHA-256: `2bdf23894cef4feaf1c8a1c5de9c1e0afd091aee717818a5b6bf4d3228a7ba89`.
V6 meta SHA-256: `bd8f44911a0c13d7947724ebc87c28146b8c8e24e637c530df880696cfb33f22`.

Both sources are RGB 1254 x 1254. Read-only Pillow/NumPy measurements use encoded RGB in [0,1] and weighted luminance `0.2126 R + 0.7152 G + 0.0722 B`. Project colour space is Gamma (`m_ActiveColorSpace: 0`), and the existing sprite shader multiplies material and vertex colours by the source.

| Source | Mean luminance | Luminance standard deviation | Neutral material scalar | Source-average output |
| --- | ---: | ---: | ---: | ---: |
| V6 baseline | 0.353559328 | 0.022585794 | 0.8 | 0.282847462 |
| V7 candidate | 0.467845936 | 0.054425715 | 0.604573943 | 0.282847462 |

The initial neutral scalar is `0.8 * mean(V6) / mean(V7) = 0.604573942762`. This controls generic brightness change without altering the bitmap or independently retinting channels. It matches the source-average weighted output, not an exact rendered frame mean: UV occupancy, vertex light channels, filtering, compression and mips can change that. Pixel-level statistics are recorded in `paving-v7-source-statistics.json`.

## Preserved import and sampling contract

The V7 importer is identical to V6 except GUID: Default 2D texture, non-readable, sRGB, alpha ignored, Repeat on all axes, trilinear filter, anisotropy 2, mipmaps enabled, ToNearest NPOT scaling, compression quality 50. Android override remains max 1024, format 50 (ASTC 6x6); expected loaded Android dimensions are 1024 x 1024, subject to actual runtime verification by root. Standalone override remains disabled, max 2048 in its existing entry.

Paving world sampling remains `uv = 1 + 2 * FoundryMap.Axis * designPosition / 5.7`. One texture repeat spans about 4.03 design units. Geometry still owns stone joints, so the new source supplies only jointless mineral grain. The shared binding affects layout paving, bridge, gate threshold and dressed border: all must be judged, including ordinary portrait views.

## Acceptance concerns

After mean matching, relative source luminance contrast is 1.821 times V6. Check that mineral layers improve stone at normal gameplay size without distracting pepper grain, repeated strata marks, visible wrap seams or increased shimmer. The source is not claimed mathematically seamless merely from its prompt. No global scene light lift was added. Final acceptance must use fresh matched-camera Unity views and loaded texture checks.

## File-level verification

Byte comparisons confirm the runtime V7 PNG is identical to its generated source, V6 PNG/meta hashes are unchanged, the new importer differs only by GUID, and the material differs only by texture GUID plus equal RGB scalar. Production C# and all tests were untouched by this helper.
