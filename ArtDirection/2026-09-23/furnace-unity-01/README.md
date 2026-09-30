# Furnace Unity sampling and perimeter placement

The original built-in image_gen master is preserved in ../furnace-prop-01 with exact prompt and source SHA256. `Tools/ArtReview/BuildFurnaceAsset.cs` samples the whole unchanged canvas once through Unity into RGBA384x328. The aspect approximation is below0.1%; no independent crop or auto-fit. Imported asset: `Assets/_Project/Art/Props/Foundry/Furnace.tga`, GUID8297f21650a2ab44c903af9c32161ee0,128PPU, bilinear, clamp, no mipmaps, non-readable, Android ASTC4/max512. Custom pivot726/1358,114/1159 (bottom-origin). The asset is shared, not regenerated per prop.

`Import-Furnace.ps1` stages and removes its own Editor importer and does not save Gameplay or change build settings. `Run-FurnaceProof.ps1` stages a temporary enlarged Gameplay copy and FurnaceCapture, then removes its owned scene/script/metas. TestProfile is isolated. The capture borrows the imported sprite/material; a temporary foundation mesh is disposed at teardown.

Placement proof: scale0.72 (approximately1.5world units visible height), north perimeter at floor(0,27.1). Small visual foundation connects with the existing coping; it does not extend ArenaGeometry. Hero walks through real input to(0,24), reaching y23.81 after20.78game seconds with unchanged stamina. Encounters/attacks are disabled for this isolated art review. No reward, forge interaction, collision or local-encounter functionality added.

First render exposed an oversized flat-grey foundation and loose base contact. Follow-up narrows it, borrows the floor material and lowers the furnace foot. The foundation is still a placement mockup; it needs proper side material and brass trim before production acceptance. The fixture uses ScreenSpaceCamera HUD rendering, so phone occlusion/safe area are not verified here.

No production scene reference or automatic spawning: the imported asset exists in Assets, but the phone game does not use it yet. No APK build/install, profile access or physical-device test. Next: turn the accepted placement into a bounded region scenery layer alongside an explicit playable-region entry, then full gate/device review. Do not replace the normal boss hall globally with this test geometry.

Verified: final graphics1/1; compile clean; static487/1099/568; browser390px no overflow. No APK/device.
