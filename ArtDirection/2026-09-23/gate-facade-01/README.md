# Gate facade 01 â€” first detailed architectural surface

Built-in image_gen produced an opaque orthographic front-elevation texture from the approved foundry photograph as style/architecture reference. Exact prompt: PROMPT.md. Source gate-facade-v1.png is unchanged1536x1024 RGB, SHA256 F4B36B1BDA91DDDA076B5062E5BC39FA468C2F4299355AE0A66FCD20B21455B7. No manual image cleanup, alpha extraction or raster edits. This deliberately avoids the haze in rejected boss-gate-01 V1â€“V3 cutouts.

Assets/_Project/Art/Props/Foundry/GateFacade.png is a byte-identical source copy. BuildGateFacadeAsset imports it as a non-readable, sRGB, clamped, mipmapped Default texture (max1024, Android ASTC6). MAT_GateFacade borrows the existing platform wall shader. PlatformMaterialSet owns the optional material reference; the scene does not need a new direct reference and ordinary hall validity remains unchanged.

FoundryRegionView maps full0..1 UVs onto the physical vertical plane u31.99,v11..-1,height6.5, in front of the stone body beyond the u32 walking end. The source has no baked isometric tilt; Unity projection supplies perspective. Flanking stone buttresses are still provisional. One static material layer, no extra runtime texture allocation. Shared facade assets survive reload; generated meshes are owned and disposed by the view. Gate remains closed scenery: no boss transport, unlock rule or walking elevation added.

unity-close.png and unity-overview.png are real Unity captures. Current whole-map graphics fixture walked all five locations successfully. Compile check0 warnings/errors; integrity496 GUIDs/568 scene objects. FoundryRegionTests now checks the imported material is used, clamped/non-readable and survives scene reload. Full gate and Android/device status follow below. Source plus consuming asset are both in the repository; generated original remains in Codex generated_images.

Remaining: compatible sidewall/cornice treatment, sculpted outer columns and lamps, furnace silhouettes, perimeter architecture and final lighting. The whole map is not finished artwork. No commit/push.


## Final build status — 2026-09-23

Full gate403 EditMode /131 PlayMode passed; Android exit0. APK48AB4DFA3F0AC71D8B9B6AF56A25F95B8E57B5D1FDADB897805FBFCD49050CA5,29,253,015bytes, built NOT installed. Device read-only check found NotificationShade/keyguard showing; guarded screenshot refused as designed. Asked owner to unlock and leave game closed or continue only on computer. No answer yet; no taps, installation, force-stop or profile operation this slice. Installed phone remains A8ED8E1C8F0DEFCF341CA998237E32A19E354794A9873D9B130EDC4B1C9C64A3. Last recorded profile197/28234 is historical until freshly read again.


## 2026-09-23 — Installed; USB lost before gate-position review

Owner unlocked and requested continuation. Fresh inspection launcher/unlocked. Backup TestResults/gate-install-20260923-211101/before: profile197/28234gold/defaultSword/SecondWind/deepest3. Installed48AB4DFA3F0AC71D8B9B6AF56A25F95B8E57B5D1FDADB897805FBFCD49050CA5, adb Success and base.apk hash matched. All10 files byte-identical immediately after installation. Foundry flag retained. No code/build changes this turn.

Launched and paused100HP. Guarded resume and28s northeast drag ended at level1 upgrade choice,86/100HP,XP11/22,5 run gold. Gate not yet visible. Next guarded input sequence was rejected before dispatch because device RFCW20W2WFX disappeared; adb devices confirmed empty. No further taps/swipes succeeded. Final profile/log pull could not be performed, so only prelaunch installation preservation is verified. No restore/save edit. Last screenshot gate-walk.png is the last observed state, not a guarantee of current phone state. Installation is complete; gate-specific physical-device review remains incomplete. Actual gate render remains verified in Unity. Inspect fresh on reconnection; do not reinstall blindly.
