# Foundry menus — result and Relic Forge

The owner approved continuing at medium reasoning effort to extend the established foundry UI. This slice reuses the existing forged frame and relic icons; no new generated artwork, gameplay system or purchase rule is introduced.

## Layout

The result screen groups outcome, cause, progress and banked/lost gold in the upper half of one opaque panel. Build details and the next-unlock hint follow; Try again and Relic Forge are separate full-width framed actions below. Existing text comes from `PrototypeText` and existing run state, including defeat and extraction. No text is baked into images.

The Forge uses its safe area's height to place a header, three weapon rows, two relic rows and a bottom Start Run control. The rows and text rectangles scale vertically to fit short and tall portrait screens. Names, effects and action/state labels have distinct text sizes. Weapons stay text-led; the existing attack-upgrade sword icon is deliberately not reused as an equipped Sword weapon illustration.

Relic art is resolved from the current relic effect, not assumed card order. Written states remain explicit. A slim accent and matching state text use green for equipped, brass for affordable, blue for owned and muted grey for too expensive. The source shop decides affordability/equipping; the existing input delay and disabled-card behaviour remain authoritative.

## Implementation and verification

Authored scene: `Assets/_Project/Scenes/Gameplay/Gameplay.unity`. Runtime change: optional icon/state accent fields in `RelicForgeView`; existing result logic is unchanged. A temporary Editor migration applied the layout and was removed after saving; its local record is `TestResults/ApplyFoundryMenus.cs`.

Existing flow tests still cover costs, save/reload, double-tap protection and equipped state. The relic forge flow now also checks the two icons and the visual transition from affordable to equipped. `ArtHudFlowTests` adds text-fit checks at 1080x1760 and 1080x2232 safe-area sizes. The Forge bounds guard checks all ten functional rows at safe heights 1760, 1920 and 2232, including horizontal containment and vertical non-overlap; the decorative background intentionally spans behind them. This replaces the old guard's assumption that every child uses bottom-fixed anchors. These are layout checks, not a claim of a second physical-device run or completed localization.

Final suite counts, APK hash, device evidence and limitations are recorded in docs/21–23. Pause-menu styling, per-badge detail interaction, character facings/walk cycles, audio and additional device testing remain separate work.
