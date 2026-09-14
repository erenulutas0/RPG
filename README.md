# Project Cryptforge — first combat slice

Original portrait mobile auto-battler prototype. The authoritative project brief is in [docs](docs/00_README.md).

Open this folder as a Unity project using **6000.0.65f1**. Open `Assets/_Project/Scenes/Gameplay/Gameplay.unity`, choose a **9:16** Game view, and press **Play**. The fight plays on a floating stone-and-brass platform in a starry void, framed between the HUD on any portrait screen; the platform, the backdrop, the blue-steel knight, the molten enemies with their health bars and the hit effects are all pixel art drawn from code at startup (placeholders, no image assets). A round button at the bottom right fires the **Forge Burst**: 20 damage to every enemy within the dashed ring around the hero, then an 8-second cooldown. Drag anywhere on the arena to walk the Vanguard; the camera follows. A Grunt and a Cinder Mite walk in from two corners of the platform, and the Vanguard automatically strikes whichever is nearest in reach; each Sword swing cleaves a second enemy for 60% damage. Clearing the pack reaches level 1, combat pauses, and two touch cards offer +50% damage or +50% attack speed. The chosen upgrade is applied once to the runtime weapon and the run continues through **Ember Halls**, the first Descent floor: three combat rooms of waves where packs of up to four enemies (Grunt, Runner, Tank and small Cinder Mites) walk in from every corner toward the hero, a chest per room that mends or pays when the hero walks onto it, a forge offering Mend (heal 40%) or Temper (extra upgrade), the elite Grunt Captain with its escort, and the Forge Warden boss that enrages at half health. Kills pay gold. After the Warden a checkpoint asks **Extract** (bank the gold and end the run) or **Descend**: floor 2, **Quicksilver Vaults**, keeps your health, scales enemies up and adds the **Cursed Gold** modifier (+20% enemy damage, +50% gold). Dying loses half of the gold earned since the checkpoint. Clearing both floors shows **Descent complete**; dying shows **Defeated** with the room and enemy. **Try again** starts a fresh run in one tap. Banked gold is saved and spent in the **Relic Forge**, opened from the result screen. It sells weapons: the **Staff** (area blasts that hit a whole pack) and the **Daggers** (fast strikes, every third one a double-damage crit), with the Sword always free. It also sells relics: **Second Wind** (once per run, heal 25% at 25% health) or **Counterweight** (strike back for 60% of weapon damage when hit). One weapon and one relic are carried per run. The **II** button in the top-right corner pauses a run, and leaving the app pauses it too.

- [Day 1–Day 3 plan and exact file inventory](docs/21_DAY_1_3_IMPLEMENTATION_PLAN.md)
- [Setup, acceptance checks, test commands, and validation limits](docs/22_FIRST_COMBAT_VERIFICATION.md)
- [Successful Android build and Samsung device test](docs/23_ANDROID_DEVICE_VALIDATION.md)

Implemented: automatic combat, kill → XP, upgrade choice, Grunt/Runner/Tank archetypes, enemy attacks with hero death, Descent floor 1 (rooms and waves, forge, elite, enraging boss), gold with an Extract/Descend checkpoint, floor 2 with a scaling tier and modifier, victory/extracted/defeat results and one-tap restart, the Forge meta layer with a versioned local save and two relics, a pause button, packs of up to seven enemies that walk in across a floating placeholder arena, and three weapon behaviors (Sword cleave, Staff area damage, Daggers crits) with Forge weapon unlocks and save version 2. Analytics and a progress reset are subsequent work.

Local checks from the repository root:

```powershell
pwsh -File Tools/Verify-Project.ps1
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
```

The .NET check compiles and tests the actual engine-independent combat source. It does not validate Unity components, asset import, rendering, or Android builds.
