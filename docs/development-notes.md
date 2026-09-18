# Development Notes

Verified working patterns. Keep entries short; add only what has been built and confirmed working.

## Project Layout

- Unity project: `unity/` (Unity 2018.4.34f1, created/updated via command line; Unity Hub is bypassed)
- Mod source: `unity/Assets/_DeerootCards/Code/` (asmdef: `DeerootCards`, references UnboundLib, ModdingUtils, plus game dlls)
- Build output: `build/DeerootCards.dll` — copy from `unity/Library/ScriptAssemblies/DeerootCards.dll` after each Unity compile
- Plugin: `com.deeroot.cards`, initials `DEER`, mod name text prefix for debug logs: `[DEER]`

## Build

```
~/Unity/Hub/Editor/2018.4.34f1/Editor/Unity -quit -batchmode -nographics \
  -projectPath ~/projects/deeroot-cards/unity -logFile -
cp ~/projects/deeroot-cards/unity/Library/ScriptAssemblies/DeerootCards.dll ~/projects/deeroot-cards/build/
```

Unity batch mode is the source of truth for compilation; grep the log for `error CS` / `Aborting`.

## DO / DON'T (verified)

- **DON'T** set `statModifiers.attackSpeedMultiplier` in a CustomCard — it is *not copied off cards* (`CharacterStatModifiers.Attack Speed Multiplier`); it had zero effect.
- **DO** set `gun.attackSpeed = 0.667f` (divided by the desired rate-boost factor). Gun.attackSpeed is the attack cooldown; card stats act as **multipliers** on the player's accumulated gun state, so 2/3 ≈ +50% attack rate. Verified working in-game.

## Conventions

- New stat cards: set values in `SetupCard` on components whose stat fields the doc says are copied off cards (Gun/Block/CharacterStatModifiers + GunAmmo).
- Debug-log `[DEER]`-prefixed lines in `Awake`/`SetupCard`/`OnAddCard` when a new mechanic is untested. Remove once proven.
