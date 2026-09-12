# Project Cryptforge — first combat slice

Original portrait mobile auto-battler prototype. The authoritative project brief is in [docs](docs/00_README.md).

Open this folder as a Unity project using **6000.0.65f1**. Open `Assets/_Project/Scenes/Gameplay/Gameplay.unity`, choose a **9:16** Game view, and press **Play**. The Vanguard automatically lands five Sword hits and defeats the Grunt; 10 XP reaches level 1, combat pauses, and two touch cards offer +5 damage or +25% attack speed. The chosen upgrade is applied once to the runtime weapon, then a fresh Grunt spawns and the HUD shows the faster kill (for example 4 hits in 2.4 s instead of 5 in 3.2 s). Grunts strike back for 6 damage per second, so the hero eventually falls; the result screen explains where and with which build, and **Try again** starts a fresh run in one tap.

- [Day 1–Day 3 plan and exact file inventory](docs/21_DAY_1_3_IMPLEMENTATION_PLAN.md)
- [Setup, acceptance checks, test commands, and validation limits](docs/22_FIRST_COMBAT_VERIFICATION.md)
- [Successful Android build and Samsung device test](docs/23_ANDROID_DEVICE_VALIDATION.md)

Implemented: automatic combat, kill → XP, upgrade choice, consecutive encounters in a Grunt, Runner, Grunt, Tank order (Runner: fast chip attacks; Tank: 120 HP with a slow, delayed slam), enemy attacks with hero death, and the result screen with one-tap restart. Enemy scaling, healing, floor progression with Extract (see the Descent decision), and additional weapons/enemies are subsequent work.

Local checks from the repository root:

```powershell
pwsh -File Tools/Verify-Project.ps1
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
```

The .NET check compiles and tests the actual engine-independent combat source. It does not validate Unity components, asset import, rendering, or Android builds.
