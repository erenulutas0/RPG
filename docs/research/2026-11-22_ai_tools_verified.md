# AI tools for art, animation, effects and voice: the verified list (2026-11-22)

Research only. Nothing was bought and no account was created. No code, asset, scene or setting changed.

**What this is.** The owner ran a deep-research pass with the brief in `2026-11-22_ai_tools_brief.md`. The result is
`2026-11-22_ai_tools_owner_report.md` (Turkish). That report has no links, and several of its rows are wrong or
invented. This file checks the leads that matter for our pipeline against official pages: pricing, licences, terms and
docs. Section 1 lists the report's claims with a verdict. Section 2 has one table per category. Section 3 recommends
what to adopt, trial and skip. Section 4 is a first-month budget that adds up.

**How it was checked.** Web search and page fetches on 2026-11-22. Every claim carries a source key such as [M1]. The
keys resolve in section 5, with the URL, the page's own date where it shows one, and how the page was read:
- **page**: the page itself was fetched and read;
- **excerpt**: only a search engine's excerpt was available, usually because the site refused the fetch (HTTP 403).
  These are weaker, and say so.

Prices exclude VAT. Some vendor pages showed Turkish lira when fetched from this machine (Meshy, Cascadeur); those
figures are what the owner will see, and are marked "TRY as shown". Dollar to lira uses **50.33**, the TCMB buying rate
of 18 November 2026 [FX1]; 22 November is a Sunday and has no TCMB rate.

**Verdicts.** **Confirmed**: an official or primary source says so. **Wrong**: a source says otherwise. **Partly**: the
core is right, but a detail that matters is wrong or missing. **Unverifiable**: nothing found either way.

### What our pipeline needs (why the tables are cut the way they are)

- **Characters and bosses** are modelled, rigged and animated by Python scripts in Blender 4.5 and rendered as
  toon-shaded sprite frames in eight facings. One model renders every frame, so details cannot drift between frames
  (`Tools/ArtReview/BuildBlenderArcanist.py`). The toon look comes from our own node group, so an AI tool's textures
  matter little. What matters is the **silhouette, topology and licence of a starting mesh**, and **motion** we can
  retarget onto our script-built rigs.
- **The environment** is code-built meshes projected flat at 2:1 under an orthographic camera. A 3D asset pack does not
  drop in: it has to be re-rendered in Blender or converted by script (docs/62 section 6).
- **Engine:** Unity 6000.0, Built-in Render Pipeline, Android.
- **The agent** can drive anything with an API, a CLI or a local model. A GUI-only tool needs the owner's hands.

---

## 1. Corrections to the owner's report

Section letters (A-I) are the report's own.

| # | The report's claim | Verdict | What the sources say | Source |
|---|---|---|---|---|
| 1 | "All information was verified from current sources" (intro); "Sources: official docs..." (end) | **Wrong** | The report has no links. Of the 36 claims checked below (rows 2-37), only 5 are confirmed (rows 8, 10, 20, 26, 34); the other 31 are wrong, partly wrong or unverifiable. | this file |
| 2 | G: an "Elon Musk Starbucks" row in the cost table | **Wrong** | It is not a tool and has nothing to do with game art. Delete the row. | - |
| 3 | D: "Unity ArtEngine 2026", about $40/month, a texture and material tool | **Wrong** | The ArtEngine desktop app was reported cancelled in August 2022. The old product URL now serves Unity's Asset Transformer (the Pixyz successor) page. There is no 2026 ArtEngine. | [UA3], [UA4] |
| 4 | D: "Unity Muse": a 2026 beta for story and quest text | **Wrong** | Unity's own docs: "Unity Muse will be retired on October 1, 2025. It is being replaced by Unity AI." | [UA2] |
| 5 | D: "Unity AI (Sensation/Sentinel)": animation, auto-rig, faces | **Unverifiable** (likely invented) | No such product. Unity's AI page (beta) lists an in-editor assistant with Generators, an AI gateway, an MCP server, a plugin and a CLI. They need Unity 6.0 or later. Personal pays $10/month after a trial; Pro and Enterprise include the assistant. Sentis stays active for running models at runtime. | [UA1] |
| 6 | C: Unity VFX Graph is "the classic way" for our effects | **Wrong for us** | Unity 6000.0's feature table: GPU particles (VFX Graph) are **"No"** on the Built-in RP. URP needs compute-capable hardware, and OpenGL ES is not supported. Shuriken (CPU particles) is "Yes" on all three pipelines. | [UV1], [UV2] |
| 7 | C: on mobile, convert flipbooks into a GPU particle system | **Wrong for us** | That route is VFX Graph, which the Built-in RP cannot run. Shuriken with texture-sheet animation is the supported path. | [UV1], [UV3] |
| 8 | C: EmberGen indie about $299, perpetual | **Confirmed** | $300 for the first year (a permanent licence plus 12 months of updates), then $180/year maintenance (optional). Indie means revenue or funding under $1M. | [E1] |
| 9 | C: EmberGen used by "Witcher 3, Rainbow Six" | **Unverifiable** | No source names these titles. JangaFX says only that 200+ studios use it. | [E2] |
| 10 | B: Mixamo is free, commercial use allowed | **Confirmed, with caveats** | Adobe's FAQ: free; characters and animations royalty-free for commercial projects, games included. You must not redistribute the raw character or animation files, and must not train ML models on them. Not available to Enterprise or Federated IDs. The 2022 licensing FAQ calls it a "limited duration technology preview". No 2026 shutdown notice was found; there were outages in June and September 2025. | [X1] (excerpt), [X2], [X3] |
| 11 | B, G: Cascadeur free (limited); Pro about $100/year | **Partly wrong** | Free is **non-commercial only** and exports only `.casc` (no FBX). Indie: $19/month or $96/year, under $100k revenue or funding. Pro: $49/month or $396/year. | [C1] |
| 12 | B: DeepMotion: free and limited; output fine for commercial use | **Partly wrong** | The free plan is "for personal, non-commercial use"; "licensed animation" needs a premium plan. Exports: FBX, BVH, MP4, GLB. No prices on the official pages read. API by contact; the text-to-motion (SayMotion) API is "limited to verified partners". | [D1], [D2], [D3] |
| 13 | B: Rokoko Studio about $100/month (Pro); free mobile app | **Wrong** | Pro is $50/month billed yearly ($70 monthly). Enterprise starts at $100/month. The free Starter plan includes FBX export and 30 s/month of Vision video capture. | [K1] |
| 14 | B: "Tools that make a 3D model from a photo are not yet widespread" | **Wrong** | Meshy, Tripo, Rodin (Hyper3D), Hunyuan3D and TRELLIS.2 all ship image-to-3D in 2026, and three of them sell commercial plans with APIs (table 2.1). | [M1], [T2], [R1], [H1], [TR1] |
| 15 | B: Tripo: limited free plan; API details unclear | **Partly** | Tripo's help says free users' outputs stay Tripo's under its Terms (section 5.2.1): upgrade for commercial rights. Free models are public under CC BY 4.0. Pro is $20/month with 3,000 credits. An OpenAPI platform exists. | [T1] (excerpt), [T2], [T5] |
| 16 | A, G: GodMode AI: $29/month unlimited; also a local Windows app | **Partly wrong** | $29/month buys "up to 200 public generations". Credit packs: $32 for 30, $100 for 125. Web service; no local app is mentioned. A REST API and an MCP page are listed. "Commercial licensing is included with your subscription." | [G1] |
| 17 | A, G: PixelLab is sold in credit packs (1K = $4.90); no monthly plan | **Wrong** | PixelLab sells subscriptions (its FAQ explains why), and some API features need "Tier 1 or higher". API calls are priced per call. Commercial use is allowed, but not training models on the output. It is pixel art only. | [P1], [P2], [P3] |
| 18 | A: Ludo.ai has no API | **Wrong** | Indie ($15/month billed yearly) and Pro ($35/month billed yearly) include API, MCP and CLI access. | [L1] |
| 19 | A: Sprixen: $70/month or $0.5/image, Python SDK | **Unverifiable** | Sprixen exists (a pixel-art sprite tool launched 30 March 2026, 20 free credits). No pricing or SDK found. | [SX1] (excerpt) |
| 20 | C: Runway about $12/month | **Confirmed** | Standard: $12/month billed yearly, $15 monthly; 625 credits. | [RW1] (excerpt) |
| 21 | E: ElevenLabs "Professional" plan about $10/month (80K words) | **Wrong** | Starter $6 (30k credits, about 30 minutes; $1 first month), Creator $22, Pro $99 (600k credits). Free has no commercial licence. | [EL1], [EL3] |
| 22 | E: for Turkish, look at AmiVoice, ZIRA, Play.ht | **Wrong** (Play.ht) / not checked (others) | Play.ht is gone: Meta acquired PlayAI in July 2025, and the service shut down at the turn of 2026. AmiVoice and ZIRA were not checked. | [PH1], [PH2] (excerpts) |
| 23 | E: Amper Music as a current AI music platform | **Wrong** | Amper's public composer closed in October 2018. Shutterstock bought its assets in 2020. | [AM1] (excerpt) |
| 24 | F: Play Console needs a declaration when static in-app graphics are AI-made | **Wrong** | Google Play's AI-Generated Content policy covers apps that **generate** content from user prompts; such apps need in-app reporting. Apps that only contain or host AI-made content are out of its scope. No disclosure or label rule for static assets was found in the policy or in the August 2026 update. | [GP1], [GP2], [GP3] |
| 25 | F: App Store submissions must state that screenshots or descriptions are AI-made | **Wrong** | No such rule found. Apple's November 2025 change (guideline 5.1.2(i)) is about personal data: disclose and get permission before sharing it with third-party AI. The report's next sentence on that is right. | [AP1] (excerpt) |
| 26 | G, I: Dungeon Architect $300 | **Confirmed** (now $330) | $330. Compatible with Built-in, URP and HDRP on 6000.0.27f1, 6000.3.19f1 and 6000.5.3f1. Version 1.23.0 of 10 July 2026. Some reviews report script fixes on newer Unity 6 releases. | [DA1], [DA2] (excerpt) |
| 27 | G, I: "WFC Dungeon Generator", about EUR 60 | **Partly** | "Dungeon Generator - WFC Level Builder" is $65.99. It is **URP only (Built-in not compatible)**, version 1.0 of 6 October 2026, labelled "Created with AI". | [WF1] |
| 28 | G, I: "Stylized Garden Asset Pack" $30, gloomy trees and broken statues | **Partly** | The price is right, but the pack is **URP only (Built-in not compatible)**, and its tags read "cozy", "flowers". | [AS10] |
| 29 | G, I: "Stylized Ice" pack $10.99 | **Unverifiable** | Not found. Real ice packs are in table 2.6. | - |
| 30 | I: "Low Poly Industrial Environment Pack" $19; "TMW Stylized Industrial" | **Partly / unverifiable** | Low Poly Industrial Environment is real: $19.99, last updated May 2021, render pipeline not stated. "TMW Stylized Industrial" was not found. | [AS4] |
| 31 | I: Megabonk has "**exactly 15** shrines in every stage"; so we place 15 altars | **Partly right, misleading** | 15 is the generator's baseline for **Charge Shrines** only. Beacon (+2 each) and Wrench change it, and players report 12 on some runs. The same map also holds 8 Greed Shrines and one each of Challenge, Boss Curse, Succ, Moai and Shady Guy. Graveyard doubles shrine density. docs/62 plans 3-8 shrines for our smaller view. | [MB2], [MB3] |
| 32 | I: after the boss dies, a short final swarm follows; the boss door opens after the swarm | **Wrong order** | A boss spawner sits at a random spot and can be used at any time. The Final Swarm starts when the 10:00 timer hits zero, whether or not the boss is dead. Killing the boss cuts the timer to about 10 s and opens the next-stage portal if the tier has another stage. | [MB4], [MB5] |
| 33 | I: Megabonk mechanics "verified from the Shapes Inc wiki" | **Unverifiable** | No such wiki found. The fan wikis are megabonk.wiki (offline; Wayback copies) and Fextralife. | [MB2], [MB10] |
| 34 | I: DeepMind's "Genie 4" is a simulation model, not an asset generator | **Confirmed** | Announced 27 October 2026 as a research preview for trusted testers. | [GN1] (excerpt) |
| 35 | G: 1 USD = about 50.6 TRY on 22 November 2026 (TCMB) | **Partly** | The TCMB buying rate was 50.3292 on 18 November 2026. 22 November is a Sunday, with no TCMB rate. | [FX1] (excerpt) |
| 36 | G: budget scenarios $500 / $1,200 / $2,500+ | **Wrong arithmetic** | Low: $29 + $5 + $0 + ~$40 = **~$74**, not $500. Mid: $74 + $50 + $50 + $10 + $100 = **~$284**, not $1,200. Heavy lists no items. Section 4 has a budget that adds up. | the report itself |
| 37 | I: S23 portrait resolution "1440 x 3120" | **Wrong** | The Galaxy S23 is 2340 x 1080. 3088 x 1440 is the S23 Ultra. | [S23] (excerpt) |

**Not checked in this pass** (low impact on our decisions): Boomy and Soundraw prices, AIVA and Mubert terms, "Ada"
generative VFX, the "500-2000 draw calls" mobile target, the named-studio usage claims for Mixamo and DeepMotion
(Ubisoft, Genshin Impact).

---

## 2. Verified tables by category

### 2.1 Image or text to 3D (a starting mesh to clean up in Blender)

| Tool | What it does | Topology and quality for stylised characters | Output | Free plan licence | Paid licence | API | Price (2026-11) |
|---|---|---|---|---|---|---|---|
| **Meshy** | Image/text to 3D, remesh, retexture, auto-rig, animation library, text-to-motion | Remesh offers quad-dominant or triangle output; default target 30,000 faces [M5]. One blind test rated Meshy weakest on quad flow for complex shapes [Q1] (excerpt, single test). | GLB, FBX, OBJ, USDZ, STL, BLEND, 3MF [M5] | **CC BY 4.0**: commercial use allowed **with attribution**. 100 credits/month; 10 downloads (Meshy 6 Lite only) [M1], [M2] | "Private & customer owned"; "If you are on a premium plan, you own all assets you create" [M2] | Yes, from Pro. Costs per call: image-to-3D 20-35 credits, remesh 5, rigging 5, animation 3 per action, text-to-motion 10 (prime, FBX) or 3 (swift, BVH) [M3], [M4] | Pro **$20/month**, 1,000 credits [T3]. TRY as shown: Starter ₺440 (250 credits, no API), Pro ₺879, Premium ₺1,758 (3,000 credits) [M2] |
| **Tripo** | Image/text to 3D, auto-rig, smart low-poly (vendor claims) | Fast and detailed; topology varies between runs [Q1] (excerpt). Vendor claims of "clean low-poly" are untested here. | USD, FBX, OBJ, STL, GLB, 3MF [T4] (excerpt) | **Public models, non-commercial**: Tripo keeps the rights to free users' outputs (ToS 5.2.1) [T1] (excerpt). Tripo's own blog says otherwise; trust the terms. | Private models, commercial use [T2] | OpenAPI platform, usage-based; extra credits $0.01 each (2024 announcement) [T5] (excerpt) | Pro **$20/month**, 3,000 credits; Max $90 [T2]. One source says Pro is billed yearly ($240 up front) [T3]: check before paying. |
| **Rodin (Hyper3D)** | Image/text to 3D at higher quality; quad options | Ranked best overall in the same blind test [Q1] (excerpt). "Smart Low-Poly" from Creator; "High-Poly Quads" only on Business [R1]. | Not listed on the pricing page | Not stated. Free gives previews and 10 private assets [R1] | Creator: "Unlimited export and any use"; Business: "Commercial license included" [R1] | **Business only** ($120/month) [R1] | Creator **$30/month** ($24 billed yearly); Business $120 [R1] |
| **Hunyuan3D 2.x** (Tencent, open weights) | Local image-to-3D | Not evaluated here | GLB/OBJ (open-source tooling) | Tencent Community Licence: "Tencent claims no rights in Outputs". The licensed territory **excludes the EU, the UK and South Korea**: "use outside the Territory is unlicensed". Above 1M monthly users, a separate licence. No using outputs to improve other models. [H1], [H2] | same | Local (and resellers) | Free; needs a capable local GPU (the brief's GPU line is still blank) |
| **TRELLIS.2** (Microsoft) | Local image-to-3D, 4B parameters, PBR textures | Not evaluated here | GLB | **MIT** (some dependencies carry their own licences) [TR1] (excerpt) | same | Local | Free; third-party guides say 24 GB VRAM and Linux [TR2] (excerpt) |

**For us:** our toon node group replaces any generated texture, so the deciding factors are silhouette, clean topology
after remesh, and the licence. Free tiers are a trap for unreleased characters: Tripo's free models are **public** and
non-commercial, and Meshy's free models need attribution. Uploading a hero's concept on a free tier also publishes it.

### 2.2 Motion: auto-rig, libraries, video and text to motion

| Tool | What it is | Commercial terms | Export / Blender | API | Price |
|---|---|---|---|---|---|
| **Mixamo** (Adobe) | Auto-rig for humanoid meshes and a library of humanoid clips | Free, royalty-free in commercial games. No redistributing raw FBX files (asset packs, templates); no ML training. No Enterprise or Federated IDs. Called a "technology preview". [X1] (excerpt), [X2] | FBX, which imports into Blender. Free GPL add-on "Mixamo Rig" (v1.2.3, 2026-10-29) builds an IK rig and bakes in and out; Blender 4.2 LTS to 5.x [X4] | None | Free. A community expert said on 8 November 2026 that "no rules are changed till now" (not official) [X2]. Outages in 2025 [X3]. |
| **Cascadeur** | Physics-assisted keyframing, AutoPosing (interactive GUI) | **Free: non-commercial only.** Indie: commercial under $100k revenue or funding. Pro: no limits. [C1] | Free: `.casc` only. Indie and Pro: FBX, DAE, USD, GLTF. Free limits: 300 frames, 120 joints. [C1] | None found | Indie $19/month or $96/year; Pro $49/month or $396/year; 14-day Pro trial [C1] |
| **DeepMotion Animate 3D** | Video to 3D motion; custom humanoid characters | **Freemium: "personal, non-commercial use"**; a premium plan is needed for licensed animation [D2] | FBX, BVH, MP4, GLB (GLB only for custom characters) [D1] | "Contact us" for API plans [D1] | Freemium 60 s/month; paid prices not published on the pages read [D1] |
| **DeepMotion SayMotion** | Text to motion | Not stated on the API page | Not stated | "Limited to verified partners" [D3] | Separate pricing page, not readable [D3] |
| **Rokoko Vision / Studio** | Video to motion (Vision), text to motion, suit mocap | Terms v2.0.1 (16 July 2026): "You retain all rights to your User Content", animations included. Rokoko gets a broad licence to use it. Library assets are licensed only while you hold an account; no ML training. [K2] | FBX on every plan; BVH on paid plans; CSV on Pro and Enterprise [K1] | No public API found | Starter free (30 s/month of Vision), Basic $10/month yearly, Plus $20, Pro $50 [K1] |
| **Move One** (Move AI) | Single-phone video to motion | Move's pricing page shows "Non-commercial use" for a Move One tier (excerpt; which tier is unclear). The Move One EULA was not readable (404). Treat the free tier as non-commercial. | FBX, USD, MP4 listed (tier not confirmed) | Usage-based developer API | Free 30 one-time credits (30 s videos); Starter $18, Standard $48 [V1] |
| **Uthana** | Text and video to motion, retargeting, auto-rig | "Paid usage includes commercial use" [U1] | Not listed on the pricing page | GraphQL API, pay as you go | Text-to-motion **$0.02-0.10 per generated second**; auto-rig $0.15; no new subscriptions since August 2026 [U1] |
| **Meshy text-to-motion** | A standalone motion clip from a prompt (up to 400 characters), not tied to a model | As Meshy's plan (private on paid) | FBX (prime) or BVH (swift) [M4] | Yes [M4] | 10 or 3 credits per clip, inside a Meshy plan [M3] |
| **Hunyuan Motion** (Tencent, via fal.ai) | Text to motion | Tencent community licence family: check the territory clause, as for Hunyuan3D | FBX default [F1] (excerpt) | fal.ai API | $0.08 per generation [F1] (excerpt) |

**For us:** our rigs are script-built, so any library or AI clip arrives as **reference motion** retargeted by a bone
map in our own Blender script. The project already computes every pose in code, so this is a small addition. Mixamo
covers humanoid heroes for free. Bosses (Rootmother Myrr's roots, the Starved Astronomer) are not humanoid, and no
humanoid library or video tool helps there. Text-to-motion APIs are cheap enough to test on enemy walks and hit
reactions.

### 2.3 2D sprite and animation tools (for comparison)

| Tool | What it really does | Pixel only? | Licence | API | Price |
|---|---|---|---|---|---|
| **PixelLab** | Pixel-art characters with 4 or 8 directions, animate by text or skeleton, rotations, tilesets, VFX frames; Aseprite extension; MCP server [P1], [P2] | **Yes**: every tool is described as pixel art; max sizes 64-512 px depending on the endpoint [P1], [P2] | Commercial use yes; "do not train new models with the images" [P3] | Yes, per call: 8-direction character $0.013-0.017 (48-64 px); text animation $0.016-0.042; Pro endpoints $0.08-0.15 [P1] | Subscription tiers (Tier 1+) [P1]. Prices only from G2: $12 / $24 / $50 per month [P4] (excerpt, unconfirmed) |
| **GodMode AI** | Upload a character image; get sprite sheets or MP4 for side-scrolling, isometric (8 directions) or top-down; also Spine, FBX/GLB, VFX, UI [G1] | Its pixel tool is "trained for low-res sprites"; painted input is not described [G1] | "Commercial licensing is included with your subscription" [G1] | REST API and MCP pages listed [G1] | $29/month for up to 200 **public** generations; $32 for 30 credits, $100 for 125 [G1] |
| **Ludo.ai** | Sprite generator with animation presets, video-to-motion transfer, 3D tools [L1], [L2] | No (illustrated styles too) | Ludo says users own their outputs (its own comparison pages; terms not read) [L2] (excerpt) | API, MCP and CLI on paid plans [L1] | Indie $15/month (billed yearly, 3,000 credits/year); Pro $35/month (billed yearly) [L1] |
| **Sprixen** | Pixel-art sprites with a style lock and a pixel editor [SX1] | Yes | Not found | Not found | Not found; 20 free credits [SX1] (excerpt) |

**For us:** these tools make each frame or direction in a separate generation. Our method renders every frame from one
model, which is exactly how we beat the consistency problem the brief names as the biggest. PixelLab and Sprixen are
pixel-art only, which is not our look. None of them replaces the Blender pipeline. At most, Ludo or GodMode could make
quick concept flipbooks.

### 2.4 Effects on the Built-in RP, mobile

| Option | Works on Built-in RP? | Licence / price | Notes |
|---|---|---|---|
| **VFX Graph** | **No** (Unity 6000.0 table: "No"; URP needs compute, no OpenGL ES) [UV1], [UV2] | Free | Rule it out. |
| **Shuriken** (CPU particle system) | **Yes**, on all pipelines [UV1] | Free | Texture Sheet Animation plays a flipbook grid. All sprites on one system share a texture, and atlasing helps batching [UV3] (excerpt). What we use today. |
| **Shader Graph** | Yes on all three [UV1] | Free | For dissolve, scrolling UV and glow materials on our particles and LineRenderers. |
| **Blender-rendered flipbooks** | Yes (textures) | Free; "What you create with Blender is your sole property" [B1] (excerpt) | The project already renders sprites from Blender scripts headless, so fire, smoke, slash and seal flipbooks can be built and re-rendered by the agent in toon style. |
| **EmberGen** (JangaFX) | Yes (it exports flipbooks) | Indie: **$300** first year (permanent licence and 12 months of updates), $180/year maintenance; revenue or funding under $1M. Suite $525. Free trial. [E1] | Realistic volumetrics; a stylised look needs work. Interactive GUI. Buy only if Blender flipbooks fall short. |
| **Unity AI Generators** | Editor tools; Unity 6.0+ [UA1] | Personal $10/month after trial; credits [UA1] | Unity's page lists no specific sprite or VFX generator. Not needed now. |

### 2.5 Voice

| Service | Commercial terms | Turkish | Price |
|---|---|---|---|
| **ElevenLabs** | Free: "does not include a commercial license and cannot be used for any commercial purpose", and requires attribution. All paid plans include a commercial licence (not on Beta services). "Content generated during a paid subscription can be used commercially, and indefinitely." [EL3] | Turkish is supported, with Istanbul-accent voices listed [EL4] (excerpt). No independent Turkish benchmark was found; reviews are anecdotal and mostly affiliate pages [EL5] (excerpt). | Starter $6/month (30k credits, about 30 min; $1 first month), Creator $22 (121k), Pro $99 (600k) [EL1]. API: $0.08 per 1k characters for v2/v3, $0.04 for Flash [EL2]. |
| **Play.ht** | - | - | **Closed**: Meta bought PlayAI in July 2025; the service shut down at the turn of 2026 [PH1], [PH2] (excerpts). |
| Azure, Google Gemini TTS | Not checked in depth | No Turkish benchmark found [EL5] (excerpt) | Not checked |

**For us:** Turkish quality cannot be settled from the web. The fair test is a blind listen to five boss lines, read
in Turkish and English by two or three services. ElevenLabs Starter is the cheapest way to own the commercial rights to
lines made in that month.

### 2.6 Modular environment packs on the Unity Asset Store

Read on 2026-11-22. "RP" is the store's compatibility table (Built-in / URP / HDRP). Many listings show no
description or polygon counts to a plain fetch; those are marked "n/s" (not stated).

| Biome | Pack (publisher) | Price | RP table | Last update | Fit for us |
|---|---|---|---|---|---|
| Foundry / dark fantasy | [POLYGON - Dark Fantasy](https://assetstore.unity.com/packages/3d/environments/fantasy/polygon-dark-fantasy-low-poly-3d-art-by-synty-264560) (Synty) [AS1] | $99.99 (sale; list $199.99) | Built-in yes, URP yes, HDRP no (2022.3) | 2026-08-13 | The best-maintained dark-fantasy kit. Flat-shaded, not painted; a **Restricted** asset, so read Synty's own terms too. 244 MB. |
| Foundry | [Stylized forge](https://assetstore.unity.com/packages/3d/environments/stylized-forge-297147) (ROMPAS) [AS2] | $9.50 | Built-in **only** | 2024-10-21 | Cheap test piece for the Blender re-render or convert step. |
| Foundry | [Stylized Factory](https://assetstore.unity.com/packages/slug/257448) (Ata Khani) [AS3] | $19.99 | Built-in, URP, HDRP | 2023-07-24 | Industrial, but 1.3 GB with 4K textures: heavy for us. |
| Foundry | [Low Poly Industrial Environment](https://marketplace.unity.com/packages/3d/environments/industrial/low-poly-industrial-environment-90043) (Immortal Factory) [AS4] | $19.99 | n/s | 2021-05-04 | Old; RP not stated. |
| Ice / mirror vault | [Ice Caves & Frost Dominion (Low Poly)](https://assetstore.unity.com/packages/3d/environments/ice-caves-frost-dominion-low-poly-205056) (Immortal Factory) [AS5] | $18 | n/s | 2021-12-04 | Ice caves, spikes, a shrine set; 5.4 MB. Old. |
| Ice | [Low-Poly Ice World](https://assetstore.unity.com/packages/3d/environments/fantasy/low-poly-ice-world-83909) (False Wisp) [AS6] | $9 | n/s | 2017-03-20 | Unity 5.5 era. Avoid. |
| Ice | [Stylized Ice Formations](https://assetstore.unity.com/packages/3d/environments/landscapes/stylized-ice-formations-163302) (Invasion Studio) [AS7] | $19.99 | n/s | 2020-03-24 | 515 MB; old. |
| Ash garden | [Ultimate Low Poly Nature Pack](https://assetstore.unity.com/packages/3d/environments/landscapes/ultimate-low-poly-nature-pack-200773) (Pure Poly) [AS8] | $49.97 (sale; list $99.95) | Built-in, URP, HDRP (incl. 6000.2) | 2025-09-02 | Tags include cemetery, graveyard, lava. Bright low-poly; needs a palette pass. |
| Ash garden | [Forgotten Ruins](https://assetstore.unity.com/packages/slug/314220) (Nimble Fox) [AS9] | $22.90 | **URP only** | 2025-03-26 | Not Built-in. Avoid. |
| Garden | [Stylized Garden Asset Pack](https://assetstore.unity.com/packages/3d/environments/stylized-garden-asset-pack-369634) (AssetQuest) [AS10] | $30 | **URP only** | 2026-05-11 | The report's pick. Not Built-in, and "cozy". Avoid. |
| Dwarven forge | [Dwarven Expedition Pack](https://marketplace.unity.com/packages/3d/environments/dungeons/dwarven-expedition-pack-stylized-assets-155149) (Tobyfredson) [AS11] | Free | n/s | 2019-10-23 | Free; 732 MB; old. Only for a pipeline test. |

**The Asset Store EULA for a commercial game** (last updated 4 December 2024) [AS0]:
- **Licence:** "non-exclusive, non-transferable, worldwide, and perpetual". You may modify assets and monetise them
  inside a game, including with in-app purchases. A Single Entity licence covers all your own computers.
- **Condition:** the asset goes into a product "together with substantial, original content", and must not be a
  substantial portion of it.
- **No redistribution** of the assets themselves. Contractors need their own seat, unless you buy Multi Entity.
- **Extension assets** (tools such as Dungeon Architect) are per seat, up to 2 computers each.
- **No revenue cap** on your game.
- **AI clause (2.2.1.1(g) and 3.8(v)):** you may not use the Store or Assets "for purposes such as training an
  artificial intelligence or machine learning model" without consent. That includes using them as inputs to AI or ML.
  **So never feed an Asset Store model or its renders into Meshy, Tripo or an image model.**
- **Re-rendering into sprites** is modification and incorporation, which the licence allows. Restricted assets (Synty)
  add their own terms; read them before buying.

**For us:** docs/62 section 6 applies. Every pack must be re-rendered at our 2:1 angle or converted by script, and most
of these packs are flat-shaded, not painted. P0a is code, so **no pack is needed this month**. Re-test the shortlist
(Synty Dark Fantasy, Stylized forge, Ultimate Low Poly Nature) when P1 starts.

### 2.7 Procedural level tools for Unity

| Tool | Price | Built-in RP | Unity 6 | Notes |
|---|---|---|---|---|
| **Dungeon Architect** (Code Respawn) [DA1] | $330 (Extension Asset, per seat) | Yes | Yes: 6000.0.27f1, 6000.3.19f1, 6000.5.3f1 listed | v1.23.0 of 10 July 2026. Some reviews report script fixes on newer Unity 6 [DA2] (excerpt). Places 3D prefabs in the Editor. |
| **Tessera Pro** (Boris The Brave) [TE1] | $49.99 per seat | Yes (table lists 2019.4) | Not listed, though v6.4.1 shipped 2 March 2026 | WFC for 3D tiles. |
| **Dungeon Generator - WFC Level Builder** (Szekeres) [WF1] | $65.99 | **No (URP only)** | 6000.0.62 | v1.0 of 6 October 2026, "Created with AI". |
| **DeBroglie** [DB1] | Free, **MIT** | n/a (a C# library) | n/a | WFC with backtracking and non-local constraints; 2D, hex and 3D voxels. |
| **Our own placement** | Agent time | n/a | n/a | docs/62 3.1: skeleton, chunk templates and a validator. Our rules are small, and the generator must be **one pure body shared with the simulation** (the stamina parity lesson). |

**Is writing our own simpler? Yes.** These tools place 3D prefabs in a perspective scene; our world is projected
meshes that the simulation must reproduce from the same seed. A second generator the simulation cannot call would
break scene-to-simulation parity. If WFC is ever needed, DeBroglie (MIT) can sit inside our own pure generator.

### 2.8 Megabonk's maps

docs/62 section 9 (written the same day from Wayback copies of the wiki and Steam patch notes) is the fuller account;
the rows below re-check its key facts.

| Question | Finding | Source |
|---|---|---|
| Generation | "randomly generated maps" (store page). No developer statement on the algorithm was found. | [MB1], [MB10] |
| Maps | Forest, Desert, Graveyard. Graveyard multiplies shrines by 2.0 and chests, pots and props by 1.5. | [MB2], [MB10] |
| Stage clock | A 10-minute window. Fan-reported events: miniboss 3:00, swarms 4:00 and 7:00, miniboss 8:00, Final Swarm at timer end. | [MB4] (fan site) |
| Shrine types | Charge (stand in the zone several seconds; leaving resets; pick 1 of 3 stats; rare golden one gives Legendary only), Greed (+5 % difficulty for better rewards), Challenge (elite pack for a chest), Boss Curse (an extra boss), Shrine of Succ (pulls all XP), Moai (free item pick), Shady Guy (vendor), Microwave (duplicates an item). Map-specific: Bandit Statue (Desert), Shrine of Balance (Graveyard). | [MB2], [MB7] |
| Shrine counts (generator baseline) | Charge 15 (Beacon +2 each on later stages; Wrench also changes it); Greed 8; Challenge, Boss Curse, Succ, Moai, Shady Guy at density 1.0 (2.0 on Graveyard); pots 40 small and 15 big. | [MB2], [MB3] |
| Map size | **No published numbers.** Players describe a large square; a lap of the edge plus a pass through the middle reveals it. | [MB10] (player reports) |
| Verticality | Jump on every character (airtime about 1 s; Spaceman 2.15 s); extra jumps from upgrades (an `ExtraJumps` stat); slide (downhill slides reach about 50 speed); bunny-hop keeps speed; hugging a wall before landing or sliding on a slope negates fall damage. Code notes list Sliding and Wallrunning states. Forest has slopes and cliffs with ramps; Desert has dunes and tornadoes that launch you. **No jump pads** found. | [MB6], [MB8], [MB10] |
| Boss portal | A boss spawner at a random spot, usable early. It summons the stage boss; killing the boss opens the next-stage portal when the tier has another stage. A red edge arrow appears once it is in minimap range. | [MB4], [MB10] |
| Final phase | The Final Swarm starts at 0:00 on the timer, "does not care about bosses". Killing the boss cuts the timer to about 10 s, so players delay the boss. Ghost cap 400 alive, 300 after 2 minutes (community code analysis). A patch reworked it to end 5-hour runs. | [MB5] (players), [MB9], [MB10] |

### 2.9 Store and legal rules on AI-made content

| Rule | What it says | Applies to us? | Source |
|---|---|---|---|
| **Google Play: AI-Generated Content policy** | Covers AI content "created by generative AI models based on user prompts": chatbots, text/voice/image-to-image apps, apps that make recordings of real people. Such apps "must contain in-app user reporting or flagging features" and must prevent restricted content. **Out of scope:** apps that "merely host AI-generated content and are unable to create content using AI", summarisers, and productivity apps that add AI to an existing feature. | **No**, while the game only ships static assets made offline. It would apply if we added a runtime generative feature (for example, AI-written boss taunts from a player prompt). | [GP1], [GP2] |
| **Google Play: disclosure of static AI art** | No disclosure, label or Play Console declaration for static AI-made assets appears in the policy pages or the 25 August 2026 update. That update tightens rules for generative apps: full reviewer access to AI features, layered moderation, testing against non-consensual edits. | No requirement found | [GP1], [GP2], [GP3] |
| **Apple 5.1.2(i)** (November 2025) | Apps must disclose, and get permission, before sharing **personal data** with third-party AI. | Only if we ever send player data to an AI service. Not relevant to Android today. | [AP1] (excerpt) |
| **EU AI Act, Article 50** (in force 2 August 2026) | Deployers must disclose deepfakes. For evidently artistic or fictional works the duty shrinks to a light notice, and clearly fantastical content falls outside the deepfake definition. | Our stylised fantasy art is outside it. A cloned **real person's voice** would need care. | [EU1] (excerpt) |

### 2.10 Unity's own AI, for completeness

- **Muse** was retired on 1 October 2025 [UA2].
- **Unity's AI tools** (beta): an assistant with Generators, an AI gateway, an MCP server, a plugin and a CLI. They need
  Unity 6.0 or later. The CLI, MCP server and plugin are free; the assistant costs $10/month on Personal [UA1]. The page
  lists no sprite, animation or texture generator by name, and names "URP, Shader Graph" among the plugin's skills.
- **Sentis** (runtime model inference) remains active [UA1].
- **ArtEngine** is gone [UA3], [UA4].

---

## 3. Recommendations for our pipeline

### Adopt now (free)

1. **Shuriken plus Blender-rendered toon flipbooks for effects; drop VFX Graph.** VFX Graph cannot run on our pipeline
   [UV1]. The agent already renders headless from Blender, so slash arcs, staff rings, burn, ice, poison, shields,
   seals and level-up bursts become scripted renders. Re-rendering and palette changes cost nothing, and the output is
   ours [B1]. Use Shader Graph for dissolve and scrolling glows.
2. **Mixamo as free reference motion for humanoid heroes, retargeted by our own Blender script.** Import FBX, then
   bake onto our script-built rig through a bone map. Rendered sprite frames in the game are fine. Raw Mixamo FBX files
   must never ship in the build, and must stay out of git if the repository is ever public (no raw redistribution).
   Never use them for ML training [X1], [X2]. Treat it as a preview service that could change:
   bake what we use into our own `.blend` files.
3. **Rokoko Starter (free) for short video-to-motion references** filmed by the owner: 30 s a month, FBX, and "You
   retain all rights to your User Content" [K1], [K2].
4. **Our own world placement** (docs/62 3.1), with DeBroglie (MIT) on the shelf [DB1]. Skip Dungeon Architect and the
   WFC assets.
5. **A licence ledger.** For every generated or bought source, keep a line in the art folder's README: tool, plan,
   date, licence and attribution. The project already records prompts and hashes for generated images. Free-tier
   outputs never ship without their attribution.

### Trial (one month, paid, the agent can drive it)

1. **Meshy Pro, one month ($20; ₺879 as shown)** for a 3D bake-off. It has an API, quad remesh, rigging,
   text-to-motion, a private licence and monthly billing [M2], [M3], [M4]. Test on **one hero** (Riftblade / Nix Vane)
   and **one boss** (Rootmother Myrr) from their concept art. Steps: generate, export GLB, remesh to quads, then run
   our loft, auto-weight and toon render script. Pass if the eight-facing silhouettes read at sprite size, the remeshed
   body deforms cleanly under our computed weights in the walk and strike clips, and cleanup takes under half a day of
   agent time. If Meshy fails, try **Rodin
   Creator** ($30) next. Rodin has no API below Business ($120) [R1], so it is a manual trial.
2. **Text-to-motion for non-hero motion:** Meshy's endpoint (inside Pro) and **Uthana** pay-as-you-go ($0.02-0.10 per
   second; commercial use on paid usage) [U1]. Use them for enemy walks, hit reactions and boss telegraph poses,
   always retargeted in Blender.
3. **ElevenLabs Starter, one month ($6)** for a blind Turkish/English test of five boss lines. Lines made in a paid
   month stay commercially usable forever [EL3]; keep the files.
4. **Free trials, no purchase:** EmberGen's trial (only if Blender flipbooks miss the look) and Cascadeur's 14-day Pro
   trial (only if the owner wants to hand-polish a boss's two phases; its free tier cannot ship anything) [E1], [C1].

### Skip

- **VFX Graph, ArtEngine, Muse**: unsupported or gone.
- **Free tiers of Tripo, GodMode and similar for real characters.** Outputs are public, non-commercial or need
  attribution, and uploading a concept publishes it [T1], [T2], [G1].
- **PixelLab, Sprixen, GodMode and Ludo for production sprites.** They are pixel art or per-frame generators; our
  one-model rendering already solves frame consistency.
- **DeepMotion and Move One free tiers**: non-commercial [D2], [V1]. Revisit their paid tiers only if Mixamo and
  Rokoko clips fall short.
- **Dungeon Architect ($330), Tessera, and the WFC Level Builder** (URP only): they do not fit our projected world or
  the shared simulation.
- **URP-only packs**: Stylized Garden, Forgotten Ruins, the WFC Level Builder. **Pre-2022 packs** with no render
  pipeline listed.
- **No asset pack this month.** P0a is code; packs wait for P1 (after release, by the owner's answer in docs/62
  section 11).
- **Local Hunyuan3D for shipped assets**, unless the owner accepts the risk. Its licence does not apply in the EU, the
  UK or South Korea [H1], and it is unclear whether shipping its outputs to EU players is covered. **TRELLIS.2** (MIT) is the clean local option, if the PC has a 24 GB GPU and
  Linux. The brief's GPU line is still blank.

---

## 4. First-month budget (adds up)

USD list prices without VAT, converted at 50.33 TRY [FX1].

| Line | Purpose | USD | TRY |
|---|---|---:|---:|
| Meshy Pro, 1 month | 3D bake-off (hero + boss), text-to-motion, API | 20.00 | 1,006.60 |
| ElevenLabs Starter, 1 month | Blind Turkish/English voice test; commercial rights for that month's lines | 6.00 | 301.98 |
| Blender, Mixamo, Rokoko Starter, Shuriken, Shader Graph, DeBroglie | Free | 0.00 | 0.00 |
| EmberGen trial, Cascadeur 14-day trial | Free trials, no purchase | 0.00 | 0.00 |
| **Core total** | | **26.00** | **1,308.58** |
| *Optional:* Rodin Creator, 1 month | Only if Meshy fails the bake-off | 30.00 | 1,509.90 |
| *Optional:* Uthana pay-as-you-go credit | Text-to-motion for enemies and boss telegraphs | 10.00 | 503.30 |
| *Optional:* Stylized forge pack | Test of the re-render or convert step | 9.50 | 478.14 |
| **Optional total** | | **49.50** | **2,491.34** |
| **Ceiling: core + all optional** | | **75.50** | **3,799.92** |

Checks:
- 20.00 + 6.00 = 26.00.
- 30.00 + 10.00 + 9.50 = 49.50.
- 26.00 + 49.50 = 75.50.
- The TRY column is each USD line times 50.33, rounded half up to the kuruş (9.50 x 50.33 = 478.135, so 478.14). The
  TRY totals equal the sums of their lines: 1,006.60 + 301.98 = 1,308.58; 1,509.90 + 503.30 + 478.14 = 2,491.34;
  1,308.58 + 2,491.34 = 3,799.92.

Notes:
- With ElevenLabs' $1 first-month offer [EL1], the core is $21.00 (₺1,056.93).
- Meshy's lira price as shown (₺879) is about $17.46, so the core may be cheaper in practice.
- **Do not let a "monthly" plan turn into a yearly charge.** One source says Tripo's Pro is billed yearly ($240 up front)
  [T3], which is why Tripo is not in this budget.
- Later purchases, not in month 1: EmberGen $300 (if the trial wins), Cascadeur Indie $96/year (if hand animation is
  wanted), Synty Dark Fantasy $99.99 (P1, after a re-render test).

---

## 5. Sources

Accessed 2026-11-22 unless noted. "page" = fetched and read; "excerpt" = search-engine excerpt only.

**3D generation**
- [M1] Meshy docs, Pricing and credits: https://docs.meshy.ai/en/webapp/pricing (page)
- [M2] Meshy pricing page and FAQ (prices shown in TRY): https://www.meshy.ai/pricing (page)
- [M3] Meshy API pricing: https://docs.meshy.ai/en/api/pricing (page)
- [M4] Meshy Text to Motion API: https://docs.meshy.ai/en/api/text-to-motion (page)
- [M5] Meshy export formats and remesh API: https://docs.meshy.ai/en/webapp/guides/platform/export-formats ,
  https://docs.meshy.ai/en/api/remesh (excerpt)
- [T1] Tripo help, "Can I use the model for commercial purposes?":
  https://www.tripo3d.ai/help/privacy-policy/can-i-use-models-commercially (excerpt; direct fetch 403)
- [T2] CostBench, Tripo AI pricing (Pro/Max/Team checked 2026-11-21):
  https://www.costbench.com/software/ai-3d-generation/tripo-ai/ (page; third party)
- [T3] 3D AI Studio, AI 3D generator pricing compared (prices checked 2026-11-01; a reseller of these tools):
  https://www.3daistudio.com/3d-generator-ai-comparison-alternatives-guide/ai-3d-generator-pricing-compared (page)
- [T4] Tripo export formats tutorial (March 2026): https://www.tripo3d.ai/tutorials/tripo-ai-export-formats (excerpt)
- [T5] Tripo OpenAPI platform announcement (2024):
  https://www.tripo3d.ai/blog/announcement-tripo-openapi-platform-payment-system (excerpt)
- [R1] Hyper3D (Rodin) pricing: https://hyper3d.ai/pricing?lang=en (page)
- [H1] Tencent Hunyuan 3D 2.0 Community License (release 2025-01-21):
  https://huggingface.co/tencent/Hunyuan3D-2/blob/main/LICENSE (page)
- [H2] Tencent Hunyuan 3D 2.1 Community License (2025-06-13), mirror:
  https://gitserver.onethingai.com/ai-models/Hunyuan3D-2.1/raw/branch/main/LICENSE (excerpt)
- [TR1] Microsoft TRELLIS.2-4B model card: https://huggingface.co/microsoft/TRELLIS.2-4B (excerpt)
- [TR2] Third-party VRAM guides for TRELLIS.2: https://webkul.com/blog/trellis-2/ ,
  https://www.codesota.com/news/trellis-2-3d-generation (excerpt)
- [Q1] Hackernoon, blind stress test of Meshy 6, Tripo v3.1, Rodin Gen-2.5:
  https://hackernoon.com/lang/so/how-i-stress-tested-3-ai-3d-generators-on-the-same-inputs-what-the-numbers-actually-show
  (excerpt; one tester)

**Motion**
- [X1] Adobe, Mixamo FAQ: https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html (excerpt; direct fetch 403)
- [X2] Adobe Community, Mixamo FAQ: licensing, royalties, ownership (2022-09-29, with 2026 comments to 2026-11-08):
  https://community.adobe.com/questions-696/mixamo-faq-licensing-royalties-ownership-eula-and-tos-589400 (page)
- [X3] Adobe Community, "Mixamo down" threads (2025-06, 2025-09-24):
  https://community.adobe.com/questions-696/mixamo-down-589830 (excerpt)
- [X4] Blender Extensions, Mixamo Rig (v1.2.3, 2026-10-29): https://extensions.blender.org/add-ons/mixamo-rig/ (page)
- [C1] Cascadeur plans and pricing (prices also shown in TRY): https://cascadeur.com/plans (page)
- [D1] DeepMotion Animate 3D pricing: https://www.deepmotion.com/pricing-animate3d (page)
- [D2] DeepMotion FAQ: https://www.deepmotion.com/faq (page)
- [D3] DeepMotion SayMotion API: https://www.deepmotion.com/saymotion-api (page)
- [K1] Rokoko pricing: https://rokoko.com/pricing/ (page)
- [K2] Rokoko Studio and Vision Standard Terms of Use (effective 2026-07-16, v2.0.1):
  https://www.rokoko.com/studio-term-of-use (page)
- [V1] Move AI, Move One pricing: https://docs.move.ai/knowledge/move-one-pricing (page); Move's non-commercial free
  tier: https://www.move.ai/pricing (excerpt)
- [U1] Uthana pricing: https://uthana.com/pricing (page)
- [F1] fal.ai, Hunyuan Motion: https://fal.ai/models/fal-ai/hunyuan-motion/llms.txt (excerpt)

**2D sprite tools**
- [P1] PixelLab API: https://www.pixellab.ai/pixellab-api (page)
- [P2] PixelLab home: https://www.pixellab.ai/ (page)
- [P3] PixelLab FAQ: https://www.pixellab.ai/docs/faq (page)
- [P4] G2 marketplace, PixelLab: https://ai.g2.com/marketplace/tools/pixellab (excerpt)
- [G1] GodMode AI: https://www.godmodeai.co/ (page)
- [L1] Ludo.ai pricing: https://ludo.ai/pricing (page)
- [L2] Ludo.ai, API and MCP announcement and comparison pages:
  https://ludo.ai/blog/introducing-ludo-ai-api-mcp-integration , https://ludo.ai/compare/ludo-vs-pixellab (excerpt)
- [SX1] Sprixen on Product Hunt (launch 2026-03-30): https://www.producthunt.com/products/sprixen (excerpt)

**Effects**
- [UV1] Unity 6000.0 manual, render pipeline feature comparison:
  https://docs.unity.com/en-us/engine/6000.0/manual/render-pipelines/choose-a-render-pipeline/feature-comparison (page)
- [UV2] Visual Effect Graph 17.0, system requirements:
  https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.0/manual/System-Requirements.html (page)
- [UV3] Unity manual, particle animation (texture sheet): https://docs.unity3d.com/Manual/particle-animation.html
  (excerpt)
- [E1] JangaFX pricing: https://jangafx.com/software/pricing (page)
- [E2] JangaFX / EmberGen studio claims: https://jangafx.com/software/embergen ,
  https://odin-lang.org/showcase/embergen/ (excerpt)
- [B1] Blender licence ("What you create with Blender is your sole property"): https://www.blender.org/about/license/
  (excerpt; direct fetch 403)

**Voice and music**
- [EL1] ElevenLabs pricing: https://elevenlabs.io/pricing (page)
- [EL2] ElevenLabs API pricing: https://elevenlabs.io/pricing/api (page)
- [EL3] ElevenLabs help, "Can I publish the content I generate on the platform?":
  https://elevenlabs.io/docs/help-center/legal/can-i-publish-the-content-i-generate-on-the-platform (page)
- [EL4] ElevenLabs, Turkish text to speech: https://elevenlabs.io/text-to-speech/turkish (excerpt)
- [EL5] Turkish-quality reviews (affiliate-style): https://tad.ai/tr/hub/elevenlabs-review ,
  https://pollo.ai/tr/hub/elevenlabs-review ; general 2026 benchmarks:
  https://www.marktechpost.com/2026/05/30/best-text-to-speech-tts-models-in-2026-a-benchmark-based-comparison/ (excerpt)
- [PH1] Bloomberg, "Meta acquires voice AI startup PlayAI" (2025-07-11):
  https://www.bloomberg.com/news/articles/2025-07-11/meta-acquires-voice-ai-startup-playai-continuing-to-add-talent
  (excerpt)
- [PH2] Stork.ai, "PlayHT alternatives (2026): PlayHT ... have shut down": https://www.stork.ai/blog/playht-alternatives-2026
  (excerpt)
- [AM1] Broadcast, Amper closes its AI composer beta (2018):
  https://www.broadcastnow.co.uk/tech/amper-closes-down-ai-music-composition-tool/5133534.article (excerpt)
- [RW1] Runway pricing: https://runwayml.com/pricing (excerpt)

**Asset Store**
- [AS0] Unity Asset Store Terms of Service and EULA (last updated 2024-12-04): https://unity.com/legal/as-terms (page)
- [AS1]-[AS11]: the store pages linked in table 2.6 (page, each)

**Procedural tools**
- [DA1] Dungeon Architect: https://assetstore.unity.com/packages/tools/utilities/dungeon-architect-53895 (page)
- [DA2] Dungeon Architect reviews: https://assetstore.unity.com/packages/tools/utilities/dungeon-architect-53895/reviews
  (excerpt)
- [TE1] Tessera Pro: https://assetstore-fallback.unity.com/packages/tools/level-design/tessera-pro-161077 (page)
- [WF1] Dungeon Generator - WFC Level Builder, Live Painting & NavMesh: https://assetstore.unity.com/packages/package/403394
  (page)
- [DB1] DeBroglie: https://github.com/BorisTheBrave/DeBroglie (page)

**Megabonk**
- [MB1] Steam store page (released 2025-09-18): https://store.steampowered.com/app/3405340/Megabonk/ (page)
- [MB2] Fextralife, Megabonk Shrines and Interactables:
  https://megabonk.wiki.fextralife.com/Megabonk_Shrines_and_Interactables (page)
- [MB3] Gamepressure, Chaos Tome and charging all shrines (2025-10-15):
  https://www.gamepressure.com/newsroom/tips-on-how-to-get-chaos-tome-in-megabonk-charging-all-the-shrine/zd8795 (page)
- [MB4] megabonk.org, timer guide (fan site, undated): https://megabonk.org/guides/mechanics/timer/ (page)
- [MB5] Steam discussion, "Final swarm before Final boss is dead?" (2025-12-26, players):
  https://steamcommunity.com/app/3405340/discussions/0/687493125920415796/ (page)
- [MB6] Gamer Guides, Megabonk movement guide (updated 2026-10-26): https://gamer-guides.com/megabonk/movement-guide/
  (page)
- [MB7] Selphie1999 Gaming, Megabonk shrines (2025-10-13):
  https://selphie1999gaming.com/game-guides/megabonk/megabonk-shrines-what-they-are-and-how-they-work/ (page)
- [MB8] lukeod/megabonk_research, mechanics.md (community reverse-engineering):
  https://raw.githubusercontent.com/lukeod/megabonk_research/main/mechanics.md (page)
- [MB9] megabonkinfo, Difficulty (community analysis): https://www.megabonkinfo.org/player-stats/difficulty (page)
- [MB10] docs/62_OPEN_WORLD_FLOOR_PROPOSAL.md section 9 (same day; Wayback copies of megabonk.wiki, Steam patch notes,
  NamuWiki, player threads), in this repository

**Policy and law**
- [GP1] Google Play, "Understanding Google Play's AI-Generated Content policy":
  https://support.google.com/googleplay/android-developer/answer/14094294 (page)
- [GP2] Google Play, AI-Generated Content policy: https://support.google.com/googleplay/android-developer/answer/13985936?hl=en
  (page)
- [GP3] Android Developers Blog, generative AI safety requirements (2026-08-25):
  https://android-developers.googleblog.com/2026/08/ensuring-safety-genai-preventing-non-consensual-intimate-content.html
  (page)
- [AP1] TechCrunch, Apple's new App Review Guidelines on third-party AI (2025-11-13):
  https://techcrunch.com/2025/11/13/apples-new-app-review-guidelines-clamp-down-on-apps-sharing-personal-data-with-third-party-ai
  (excerpt)
- [EU1] Morgan Lewis, "EU AI Act's transparency rules: what went into effect on 2 August" (2026-08):
  https://www.morganlewis.com/blogs/sourcingatmorganlewis/2026/08/eu-ai-acts-transparency-rules-what-went-into-effect-on-2-august
  (excerpt)

**Unity**
- [UA1] Unity, AI tools page: https://unity.com/features/ai (page)
- [UA2] Muse Texture manual, retirement notice:
  https://docs.unity3d.com/Packages/com.unity.muse.texture@1.1/manual/generate.html (page)
- [UA3] Unity Discussions, "Unity ArtEngine is now discontinued" (2022-08-11; user posts, not a Unity notice):
  https://discussions.unity.com/t/unity-artengine-is-now-discontinued/890847 (page)
- [UA4] unity.com/products/unity-artengine, which now serves the Asset Transformer page:
  https://unity.com/products/unity-artengine (page)

**Other**
- [FX1] Endeks24, USD/TRY from TCMB EVDS (50.3292 on 2026-11-18): https://www.endeks24.com/gostergeler/dolar-kuru
  (excerpt)
- [S23] Samsung, Galaxy S23 specifications: https://www.samsung.com/global/galaxy/galaxy-s23/specs/ (excerpt)
- [GN1] TechCrunch, Google's Genie 4 world model (2026-10-27):
  https://techcrunch.com/2026/10/27/googles-genie-4-world-model-adds-audio-more-realistic-worlds-and-real-time-steering/
  (excerpt)
