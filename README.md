# Project Cryptforge — first combat slice

Original portrait mobile auto-battler prototype. The authoritative project brief is in [docs](docs/00_README.md).

Open this folder as a Unity project using **6000.0.65f1**. Open `Assets/_Project/Scenes/Gameplay/Gameplay.unity`, choose a **9:16** Game view, and press **Play**. The Vanguard automatically lands five Sword hits and defeats the Grunt; 10 XP reaches level 1, combat pauses, and two touch cards offer +5 damage or +50% attack speed. The chosen upgrade is applied once to the runtime weapon and the run continues through **Ember Halls**, the first Descent floor: three combat rooms of waves (Grunt, Runner, Tank), a forge offering Mend (heal 40%) or Temper (extra upgrade), the elite Grunt Captain, and the Forge Warden boss that enrages at half health. Kills pay gold. After the Warden a checkpoint asks **Extract** (bank the gold and end the run) or **Descend**: floor 2, **Quicksilver Vaults**, keeps your health, scales enemies up and adds the **Cursed Gold** modifier (+20% enemy damage, +50% gold). Dying loses half of the gold earned since the checkpoint. Clearing both floors shows **Descent complete**; dying shows **Defeated** with the room and enemy. **Try again** starts a fresh run in one tap.

- [Day 1–Day 3 plan and exact file inventory](docs/21_DAY_1_3_IMPLEMENTATION_PLAN.md)
- [Setup, acceptance checks, test commands, and validation limits](docs/22_FIRST_COMBAT_VERIFICATION.md)
- [Successful Android build and Samsung device test](docs/23_ANDROID_DEVICE_VALIDATION.md)

Implemented: automatic combat, kill → XP, upgrade choice, Grunt/Runner/Tank archetypes, enemy attacks with hero death, Descent floor 1 (rooms and waves, forge, elite, enraging boss), gold with an Extract/Descend checkpoint, floor 2 with a scaling tier and modifier, victory/extracted/defeat results and one-tap restart. The Forge meta layer, relics, local save and more weapon behaviors are subsequent work.

Local checks from the repository root:

```powershell
pwsh -File Tools/Verify-Project.ps1
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
```

The .NET check compiles and tests the actual engine-independent combat source. It does not validate Unity components, asset import, rendering, or Android builds.
