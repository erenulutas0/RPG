# Machinery material study integration

The scope and resource contract below remain final. The source/exposure sections retain the initial V1 experiment for provenance; **Final V2 correction** supersedes that source, texture GUID and exposure. No V1 texture is bound in the final runtime.

## Scope

The two existing formed pit hoods and the complete island-0 trolley now borrow one dedicated cast-iron material. The global framed/riveted iron atlas remains bound to all other machinery, including the mould bench. The existing hood silhouettes, openings, collars, straps, native geometry and colours are unchanged. The trolley retains its skid, wheels, stock, bands, cast yokes, support contacts, vertex colours and triangle order. No prop is replaced by a flat image card.

The hood retains its existing affine local grain field. The trolley's final construction step maps its complete builder, including the pre-existing skid, into one affine local field at 0.19 UV per projected world unit. This matches the hood density, removes per-piece atlas restarts and avoids clamping long faces to an atlas edge. Equal projected positions receive equal UVs. This is a fixed-view projected material field, not a new physically unfolded three-dimensional UV model.

## Source and import

- Source: `sources/cast-iron-v1-source.png`, copied byte-for-byte to `Assets/_Project/Art/Props/Foundry/FoundryCastIronV1.png`.
- Both files SHA-256: `EFE2D2B9D359663815F6BFDC2C9E3F8A5AB4FEF17B0D9EC9AB0042D079D39E11`.
- Source dimensions/mode: 1254 x 1254, RGB. No bitmap adjustment, resizing, recolouring or compositing was performed.
- Import contract: Default texture, opaque, sRGB, non-readable, mipmaps, Trilinear, anisotropy 2, Clamp, maximum 1024 and existing nearest-POT import policy. Android explicitly ASTC 6x6. Actual imported format/dimensions must be verified by root in Unity.
- Texture GUID: `a4e992bcef404e4790549e599284746f`.
- Material GUID: `857719c302f84784be4344b0a245dc7c`.
- `PlatformMaterialSet.FoundryCastIron` is an optional borrowed asset slot, falling back to existing `FoundryIron` when absent. The production asset assigns it to `MAT_FoundryCastIron`.

## Controlled initial luminance

The project uses Gamma colour space and the existing unlit shader multiplies texture, material and vertex colour. For this initial source A/B, the new material uses a uniform RGB scalar of **0.7747285**, alpha 1. It preserves the candidate's channel ratios and does not alter any geometry vertex colour or existing forge-light calculation.

Read-only source statistics, normalized 0-1 code values:

| Measurement | Existing iron plain field, UV 0.17-0.83 | New full jointless source |
| --- | ---: | ---: |
| Mean R | 0.2320419 | 0.3218632 |
| Mean G | 0.2504778 | 0.3204612 |
| Mean B | 0.2775945 | 0.3207261 |
| Rec.709-weighted code-value luminance | 0.2485162 | 0.3207784 |

The uniform scalar is the ratio of those two luminances. It is a starting exposure control, not a guarantee that every sampled local patch is equally bright. The candidate is more neutral than the old blue iron and has stronger authored material variation. Real matched portrait captures must establish whether this improves cast-metal identity without weakening actor contrast or reading as concrete.

## Resource tradeoff

The material is intentionally isolated instead of replacing the global iron atlas. This adds one imported 1024-limit texture and one borrowed material. The existing scene cutaway system creates one additional shared material variant. Hood order -10.5 and trolley order -11 require two additional static batches, so the approved region renderer allowance is **32 to 34**. Their actual scene sorting ranks remain -7 and -8 respectively. There are no new vertices, triangles, depth owners, shaders, update components or runtime texture generation.

The extra cutaway variant does incur **one additional existing focus-vector update per frame** and joins the existing reset/disposal loops. This is an explicit bounded cost, not a claim of zero extra per-frame work or device-performance acceptance. The existing 174,000 vertex cap is retained. Root owns the exact renderer-budget update and PlayMode resource/lifecycle verification.

## Source changes and verification

- `PlatformMaterialSet.cs`: optional cast-iron material slot/property.
- `PlatformMaterials.asset`: borrowed asset binding.
- `FoundryRegionMassing.cs`: only the hood material selection changes.
- `FoundryRegionProductionYard.cs`: island-0 material selection and final trolley UV field.
- New `FoundryCastIronV1.png`/meta and `MAT_FoundryCastIron.mat`/meta.
- `FoundryYardEquipmentTests.cs`: two additional cases check actual complete trolley/skid shared-position UVs, noncollapsed rendered triangles, consistent grain density, and the final remap's exact preservation of V/T/C.

Existing hood containment, open mouth, adjoining surface light, UV and vertex-budget tests are retained without edits. Root owns the actual imported resource checks, targeted/full Unity verification, matched image review and any eventual device work. No Unity, ADB, Git or phone operation was run by the machinery helper.


## Final V2 correction

The V1 candidate above was rejected in actual gameplay portraits: the source's dense aggregate read as concrete and obscured the existing form lighting. Its PNG and initial scene evidence remain under sources/ and validation/initial-v1/. V2 was edited by built-in image_gen to reduce pitting and replace the dense aggregate with quiet worked metal. Final runtime source is FoundryCastIronV2.png, copied unchanged; the uncommitted V1 runtime asset was removed, and V1 is retained as study evidence only.

Final per-channel material gains match the prior plain-iron mean RGB, preserving the established cold-iron hue as well as luminance. This supersedes the V1 neutral scalar above; geometry, authored vertex colours and forge-light equations are unchanged. Numerical source provenance is in iron-v2-stats.json.

Final texture GUID: 8d6a4a1b535744f8bab4894000b6cd5e; source SHA256: ab0a94dab1882366021cd7a8f0c01a53978803b00f2aa3fecfb2465a33f925f0.
