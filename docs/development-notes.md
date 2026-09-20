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

## Card removal → full deck rebuild (verified: Delete card, both v1 bugs reproduced + fixed)

- UnboundLib `Cards.instance.RemoveCardFromPlayer(player, idx, editCardBar)` rebuilds the **whole deck** from scratch: every remaining card's `OnAddCard` re-fires during the vanilla pick pipeline. DON'T assume removal is surgical.
- **DO** open `RebuildGuard` (static 2s unscaled-time quiet window, `RebuildGuard.cs`) on any removal path and have our cards' `OnAddCard` bail while `IsQuiet` — kills re-fire cascades in both Delete and Double.

## Pick-phase hold (verified: Delete card waits the game, sandbox-tested mechanics)

- `CardChoice.RPCA_DonePicking()` (private) is just `IsPicking = false`; `GM_ArmsRace.DoPick` waits `while (IsPicking)`. A Harmony **prefix returning false** holds the entire pick/round-start flow; on release set `CardChoice.instance.IsPicking = false` yourself — no reflection original-call needed.
- Patch registration: `new Harmony("id").PatchAll(typeof(PatchClass))`. Do NOT use `Harmony.CreateClassProcessor(...).Patch()` as a static call — doesn't compile (CS0120).
- Flow replication idiom: one broadcast RPC both bumps shared pending state and shows the client-local OnGUI overlay; clicks gated to picker via `player.data.view.IsMine`.
- Stall protection: per-client timeout fallback releases the hold; also release if the pending card itself is removed (`OnRemoveCard`).
- While held, `IsPicking == true` + empty `spawnedCards` makes `DoPlayerSelect` a no-op — picker can't sneak another card pick.
- Caveat: cross-PhotonView RPC ordering isn't guaranteed online (pending broadcast vs DonePicking) — unverified online; sandbox has no `DoPick` loop so blocking only exercises in real match modes.
- Arm armed-card logic directly inside `OnAddCard` — a card's own self-add is invisible to Update-polling.

## Player display names (verified in decompile)

- Display name comes from **`PhotonView.Owner.NickName`** (`PlayerName.cs`, `DisplayMatchPlayerNames.cs`); GameObject name is always `Player(Clone)`.
- `GetDisplayName(Player)` in `DeleteCard.cs`: online → `view.Owner.NickName`; offline → `Player {playerID + 1}`.

## Health/damage funnel (verified in decompile — Heart card Milestone 2)

- **`HealthHandler.DoDamage` is the single damage funnel**: `TakeDamage → DoDamage`, `RPCA_SendTakeDamage → TakeDamage → DoDamage`, `DamageOverTime.cs:34 → health.DoDamage (per DoT interval)`. One Harmony prefix on `DoDamage` gates everything (bullets, explosions, DoT). Wall/pit/spike deaths are not damage — unaffected.
- Regen actually heals every frame via **`HealthHandler.regeneration`** (public float) in `HealthHandler.Update`; `CharacterStatModifiers.regen` only feeds it at stat-apply time (`ApplyCardStats.cs:136`). Zero `HealthHandler.regeneration` to disable ALL healing. Lifesteal is consumed in `CharacterStatModifiers.DealtDamage` off `stats.lifeSteal`.
- `TimeHandler.deltaTime` (static) = `Time.deltaTime × timeScale`, already scaled — use it directly for timers instead of recomputing.
- `CharacterStatModifiers.ResetStats()` resets movementSpeed/regen/lifeSteal wholesale on round boundaries — temporary stat modify/restore around an ability is safe if restored in the same ability cycle.

## Heart card specifics (HeartCard.cs, decompile-verified; runtime details pending playtest where noted)

- Death cleanup MUST hook the death RPCs, not Update: `HealthHandler.RPCA_Die` (:320–341, sets `data.dead` then `SetActive(false)` at :328) and `RPCA_Die_Phoenix` (:344) run locally on every client — postfixes there (`DeathHeartPatch`) re-arm/re-clone deterministically. A dead-check in `HeartEffect.Update` on the player GameObject stops running at death because the GameObject is deactivated.
- Configurable-ability lifecycle: throw → save `movementSpeed`/`regeneration`/`lifeSteal` originals, zero/add modifiers; restore in `RemoveHeart` (the single choke point every heart-removal path funnels into: heart shot, wall hit, owner death, round reset, card removal).
- Heart-out damage gate: Harmony prefix on `HealthHandler.DoDamage` returning `false` while the owner's heart is OnGround — owner fully invincible (incl. own bullets). Own lethal finishers pass via a static `applyingHeartDrain` bypass flag; the heart-shot kill flips state off OnGround before calling DoDamage, so the gate naturally lets it through.
- Heart drain formula (constant per-tick damage): lifetime `L(h) = 20h/(h+100)` s from `maxHealth` sampled at throw; constant `dps = (h+100)/20` (100 hp → 10 hp/s, dead at exactly 10 s; lifetime asymptotically capped at 20 s). Ticks: fixed 0.25 s accumulator with `TimeHandler.deltaTime`, debit `data.health` directly on every client (deterministic replication), lethal `DoDamage` finisher at hp ≤ 0.
- Heart object: single root entity, ZERO colliders, never on the "Player" layer — players can never physically interact with it at all. Motion is custom kinematic: `velocity` integrated against the owner's `PlayerCollision.mask` via substep `Physics2D.CircleCastAll` (≤0.3 units/step) and a linear gravity ramp `velocity += down × gravityForce × sinceAir × dt` mimicking `Gravity.cs:19–34` (which itself applies force as `down × timeScale × pow(sinceGrounded, exponent) × gravityForce × rig.mass` — the heart integration deliberately drops the `mass`/`timeScale`-power terms and integrates velocity directly). `Physics2D.gravity` is ≈0 in ROUNDS; Rigidbody/gravityScale does nothing.
- Heart-as-Damagable: the heart GO root inherits `Damagable` with `TakeDamage`/`CallTakeDamage` overridden (routing to the `RPC_HeartShot` kill path), so explosion/AoE damage reaches the kill path; direct bullet detection uses a Harmony postfix on `MoveTransform.Update` (fires on the bullet's exact move-integration frame, incl. its last) with a swept `SegmentTouchesDisc` test against the disc list — same memory-less idiom as the bullet-portal pass.

## DIVE card (DiveCard.cs, compiled clean; runtime playtest pending)

- **Vanilla Shield Charge reference data lives in extracted prefabs, not the decompiled C#** (serialized fields are prefab-set): `unity/Assets/GameObject/A_ShieldCharge.prefab` → `force: 1700000`, `drag: 300000`, `time: 0.2`, 4-key `forceCurve` (peak 0.218 @ t≈0.22); `unity/Assets/Resources/0 cards/Shield Charge.prefab` → `block.cdAdd: 0.25`, `soundDisableBlockBasic: 1`, rarity 1 (Uncommon), colorTheme 2 (DefensiveBlue).
- **Charge velocity math** (verified against `PlayerVelocity.AddForce` + `HealthHandler.TakeForce`): per FixedUpdate `Δv = curve × force × 0.02 × (mass/100) / mass` and drag `Δv = -v × drag × dt × 0.02 / mass`. With the vanilla numbers: terminal speed ≈ `340 × curve / 1.2` units/FixedUpdate (≈62 u/s at curve peak). Distance scales **linearly with both `force` and `time`** — doubling either is exactly 2× distance. DIVE doubles `time` (0.2→0.4s): same speed, twice as long.
- **Block-event sync needs no mod RPC for player-sim effects**: `Block.RPCA_DoBlock` is a Photon `RpcTarget.All` RPC → `SuperFirstBlockAction` fires on every client → each client runs the same force loop on its local sim of that player (this is exactly how vanilla ShieldCharge replicates). Only owner-local data (e.g. Blink's cursor) needs an UnboundRPC.
- Block-triggered effects that must not re-trigger off vanilla ShieldCharge's free re-block should guard `triggerType != BlockTrigger.BlockTriggerType.ShieldCharge` (vanilla `ShieldCharge.DoBlock` does this; makes coexistence with the vanilla card safe).
- Custom-effect stacking without AttackLevel: merge in `Awake` — `GetComponents<T>()`, bump the existing sibling's `stacks`, `Destroy(this)`. Works even when both copies are added in the same frame (sibling not yet Started).
- `PlayerVelocity.velocity` read via cached static `FieldInfo` reflection (internal field; same as the Blink write pattern but cheaper per-frame).

## Bouncy Ball card (BouncyBallCard.cs; runtime verified, per-source ×4/×2/×3 playtest pending)

- **Knockback-taken chokepoint: `HealthHandler.CallTakeForce` is the networked wrapper for ALL external knockback** — the funnel prefix scales its `force` arg, and the scaled value rides the vanilla `RPCA_SendTakeForce` Photon RPC (`RpcTarget.All`) to every client, so it syncs with zero custom networking. Callers: `ProjectileHit` (bullets), `Explosion` (incl. own rockets), `DamageBox` hazards, `NetworkPhysicsObject.OnPlayerCollision` (**boxes/props hitting players**), `LineRangeEffect`, `OutOfBoundsHandler` (pit bounce).
- **Per-source multipliers via source stamps** (two-layer Harmony pattern): patch classes set/reset a static `SourceMultiplier`/`SourceTag` around their window — `ProjectileHit.RPCA_DoHit` prefix/postfix (bullets ×4; it's itself an `RpcTarget.All` RPC, so it runs on every client) and `OutOfBoundsHandler.LateUpdate` ("LateUpdate" string patch, ×2, both 400×-mass-blocking and 200×-mass branches). The `CallTakeForce` funnel applies `SourceMultiplier > 0 ? SourceMultiplier : Default` (×3). Adding a source = one tiny stamp class, funnel untouched. Caveat: Harmony skips a postfix if the original throws → one stale-multiplier event until next stamp (≈0 risk).
- **Direct `TakeForce` callers bypass the patch on purpose** (holder mobility stays vanilla): jump (`Movement.cs:127`), block self-push (`Block.cs:206`), Shield Charge + DIVE charge loops, Thrusters, Saws, BeamAttack. Blocking still negates knockback — `CallTakeForce`'s `IsBlocking()` gate is untouched by the prefix.
- `HealthHandler.data` is **private**; resolve the CharacterData in a patch via `__instance.GetComponent<CharacterData>()` (same GameObject — that's exactly how vanilla `HealthHandler.Awake` gets it).
- **PITFALL (cost us a debug round): `CustomCard.SetupCard` re-runs on every spawned pick-card clone** — UnboundLib's `CustomCard.Awake` calls it, and `Instantiate` copies the component onto each clone. NEVER cache `CardInfo` by instance identity from `SetupCard`: `data.currentCards` stores `CardInfo.sourceCard` (`ApplyCardStats.cs:110`) = the **registered** prefab instance (`CardChoice.cs:333`), not a clone. SetupCard-set **stats still work** because `ApplyCardStats` copies stats off the pick-card clone's own components.
- Card-ownership check in stat-ish patches: shared `HasCard(data)` helper — `data.currentCards.Any(c => c != null && c.cardName == CardName)` — `cardName` is identical on registered instances and clones (copied field), so it's immune to the identity problem; vanilla-authoritative state, no effect component, no stack/removal bookkeeping (stack copies don't multiply — per-source multipliers are flat).
- Speed stat: `CharacterStatModifiers.movementSpeed` is a plain multiplier copied off cards (default 1) — `1.5f` = +50%. (Unlike `attackSpeedMultiplier`, which is NOT copied off cards — see DON'T above.)
- Theme enum (`CardThemeColor.CardThemeColorType`) has **no** `NatureGreen` — actual values: `DestructiveRed, FirepowerYellow, DefensiveBlue, TechWhite, EvilPurple, PoisonGreen, NatureBrown, ColdBlue, MagicPink`. Green = `PoisonGreen`.

## Tooling

- Test logs readable directly at `~/.config/r2modmanPlus-local/ROUNDS/profiles/dev/BepInEx/LogOutput.log` (r2modman dev profile, no need for the user to paste).

## Conventions

- New stat cards: set values in `SetupCard` on components whose stat fields the doc says are copied off cards (Gun/Block/CharacterStatModifiers + GunAmmo).
- Debug-log `[DEER]`-prefixed lines in `Awake`/`SetupCard`/`OnAddCard` when a new mechanic is untested. Remove once proven.
