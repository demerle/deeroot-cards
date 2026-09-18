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

## Event-driven cards (verified in-game — Blink card worked end-to-end)

- Block-side hook: `block.BlockAction += (BlockTrigger.BlockTriggerType triggerType) => ...` (public event on `Block`; vanilla cards use `SuperFirstBlockAction`, which fires earlier via delegate combine).
- `NetworkingManager.RPC(typeof(Type), nameof(StaticMethod), args...)` invokes **static** methods **that must be marked `[UnboundLib.Networking.UnboundRPC]`** — without the attribute it throws "no method found" and silently fails (cost us a debug round). Requires `Photon3Unity3D.dll` in asmdef `precompiledReferences` because the overloads expose `SendOptions`/`RaiseEventOptions`.
- `PlayerVelocity.velocity` is an **internal** field (not private) — read/write via reflection.
- **Verified in-game:** the missing `UnboundRPC` attribute was the exact cause of the first Blink build doing nothing on block (fixed and confirmed working).
- `PlayerManager.GetPlayerWithID` is `internal` and matches on `Player.playerID` (public field, synced via Photon CustomProperties). Pass `player.playerID` in RPCs, not `view.ControllerActorNr`.
- Cursor→world: `Camera.main.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -cam.transform.position.z))`.

## Teleport mechanics (matches vanilla `Teleport.cs` card, decompiled & verified)

Vanilla block-teleport does exactly:
```
transform.root.position = target;            // raw write works — sim moves from there
GetComponentInParent<PlayerCollision>()?.IgnoreWallForFrames(2); // go through walls
data.sinceGrounded = 0f;
data.playerVel.velocity *= 0f;               // internal field; use reflection from mods
```
The player sim (`PlayerVelocity.FixedUpdate`) does `transform.position += velocity * dt`, so a raw transform write is NOT stomped — vanilla teleports work this way.

## Decompiled game code

The game code is already decompiled on the linux drive:

- Decompiled sources: `decompiled/Assembly-CSharp-split/` (798 per-class .cs files) and `decompiled/Assembly-CSharp-firstpass-split/`. **Read these to verify any game API before guessing** — e.g. vanilla cards like `Teleport.cs` are ready-made references.
- Game install (Windows Steam library): `/mnt/f/SteamLibrary/steamapps/common/ROUNDS/` — re-decompile only if the game updates: `ilspycmd -p --nested-directories -o <out> <...>/Managed/Assembly-CSharp.dll` (need `ilspycmd` — pin **9.1.0.7988**; 11.x targets net9 which isn't installed).

## Conventions

- New stat cards: set values in `SetupCard` on components whose stat fields the doc says are copied off cards (Gun/Block/CharacterStatModifiers + GunAmmo).
- Debug-log `[DEER]`-prefixed lines in `Awake`/`SetupCard`/`OnAddCard` when a new mechanic is untested. Remove once proven.
