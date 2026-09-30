# Foundry effect depth, forge-orange light, calmer bridge — 2026-09-30

Two helper agents (each owning its own files); a third wrote `../painted-surfaces-02/` (texture prompt pack v2).
Matched actual Unity captures: `../foundry-chest-light-01/after/` (before) and `after/`.

## What changed

- **Combat effects take depth** (`CombatEffectsView.cs`, `FoundryVisibilityView.cs`, `FoundryRegionTests`). Above-floor
  effects - hit sparks, slashes, crit stars, death remains/smoke/coins, chest heal spark and coins - store the floor y
  of what they belong to at spawn (the struck enemy's feet, the chest's floor spot) and are moved to that depth while a
  camera renders, like actors; so an enemy hit or killed behind a boiler no longer shows its effect drawn over the
  boiler. Floor effects (staff blast ring, ability pulse) are untouched. Damage/heal/gold numbers deliberately stay on
  top: they are readouts, and a structure edge cutting a number could make it read wrong (a hidden enemy is still
  marked by its silhouette). Enemy body offsets are flattened to z 0 when a wave is bound.
- **Forge-orange light** (`FoundryRegionFinish.cs`). The pools read sandy yellow because the soft ceiling acted per
  channel (red capped first, green kept climbing) and the blue/green absorption was linear. The ceiling now scales all
  channels by the brightest one (hue kept, strict bound 1.74 so the shader stays under +/-2), absorption bites harder in
  hot cores, fire tints are more amber, and pit/boiler pools are stronger. Measured with a Python mirror of the model:
  pit rim hue 30 -> 22, saturation .52 -> .62; open court unchanged (cool slate). Cores are ~6 % lower in luminance but
  more saturated.
- **Calmer bridge** (`FoundryRegionView.cs` `BuildBridgeDeck`). The ~210 small margin setts read as a wire net; the
  margins are now one course at twice the grid pitch with quieter joints and bevels. Centre flags, lane cuts under the
  brass, kerbs and walking quad unchanged. Deck 3,764 -> 1,556 vertices.

## Budget and gate

- Static budget: 17 renderers (unchanged, limit unchanged), 58,612 -> 56,404 vertices.
- Full gate: EditMode 405/405, PlayMode 130/130, Android build exit 0; integrity 520 / 1,099 / 568 passed.
- APK 39,339,430 bytes, SHA-256 `DEFA17127136C731A90059BCF016C33911AFA0E0C42B6CCEFC9C87F797AE43B5`, **installed** on the S23 after the owner chose to end an open run (Floor 3, level 13, 303 unbanked gold,
  `pre-install-owner-run.png`). Installed base.apk hash matches; profile (revision 221, 33,370 gold) and backup
  unchanged; backups in `TestResults/foundry-fx-install-20260930`. `device-start.png` and `device-pit-light.png`: the
  forge-orange light around the pit and the cool court on the phone. Logcat clean; app sent Home.

## Limits

- No capture shows an effect behind a structure; effect depth is covered by tests only.
- Pool cores may read slightly rusty/dark; orange Mites on the pit rim have a little less contrast.
- The bridge margins may now look plain.
