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
- **DO** set `cardInfo.allowMultiple = false` in `SetupCard` for per-player unique cards (verified in decompile): vanilla `CardChoice.SpawnUniqueCard` re-rolls a card the picker already holds; CardChoiceSpawnUniqueCardPatch (hard dependency) fixes vanilla's buggy flag logic and enforces the check correctly. Note scope: per-player pick-phase only — other players can still take the card. Applied to Portal, Heart, Invisibility, Shambles, Blink, Sovereign.

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

## Ability-card HUD icons — the standard (AbilityHud.cs; Portal + Heart migrated)

- **`AbilityHUD` is the single shared registration point for bottom-left ability icons** (portal, heart, future ability cards). Nothing positions itself: an effect registers `AbilityHUD.Register(this, HudVisible, HudDraw)` in `Awake` and calls `AbilityHUD.Unregister(this)` in its existing `OnDestroy`. A `DontDestroyOnLoad` `AbilityHudDriver` (created on demand, `DeleteOverlayUI`-style guard) stacks every currently-visible icon bottom-left in one row — CoD-zombies perk style: new icons append right and shift existing ones; removals recompact with no holes.
- Card recipe: two lines — register in Awake, unregister in OnDestroy; draw via `AbilityHUD.DrawCircle(rect, tex, readyColor, spentColor, ready, caption)` (shared dark-outline + inset-disc + centered bold-label presentation, so all icons look identical). Slot order = registration order = card pick order.
- DO NOT write a per-card `OnGUI` with its own fixed Rect — two cards doing that overlap on the pixel. This replaces the per-effect OnGUI blocks that used to live in PortalEffect/HeartEffect.
- Stack safety: registration is keyed on the effect owner (`ReferenceEquals`) and effects are `GetOrAddComponent` singletons, so re-picking a stacked card never duplicates an icon. Remote players' effects also register but their `HudVisible` is `IsMine`-gated → zero footprint.
- Draw delegates run inside the driver's `OnGUI`, so `GUI.skin`/label styles are valid in them (the shared `labelStyle` lives in `AbilityHUD`).

## Sovereign card (SovereignCard.cs; compiled clean, runtime playtest pending)

- **Bot = real player-clone spawned via `PhotonNetwork.Instantiate(PlayerAssigner.instance.playerPrefab.name, ...)`** — the exact primitive vanilla sandbox bots use (`PlayerAssigner.CreatePlayer`), minus everything we DON'T want: no `CreatePlayer`, no `PlayerAssigner.players` add, no `PlayerManager.RegisterPlayer` on the owner client. So the bot never enters card-pick pipelines, round-end alive checks (e.g. `TeamsAlive`/`GetLastTeamAlive`), or RWF player lists.
- **Bot kit is fully independent of the master**: `SovereignBot.ResetBotToDefaultKit` reflects `Player.FullReset()` (internal) to wipe gun, `CharacterStatModifiers`, and `Block` back to vanilla defaults, then re-applies explicit bot-kit constants (`BotDamage`, `BotProjectileSpeed`, `BotMovementSpeed`, etc.). The bot is intentionally **not** a copy of the master's current cards. Edit the constants at the top of `SovereignBot` to tweak the default kit.
- **Remote clients auto-REGISTER the clone** because the prefab's own `CharacterData.Start` runs `RegisterPlayer` when `!view.IsMine` — our `RPC_SpawnBot` config call strips that registration (`PlayerManager.instance.players.Remove(bot.player)`). If bots are missing/whatever on remote clients, check whether moving the strip TOO EARLY broke position sync (playtest gate).
- **teamID/playerID inheritance is free under RWF**: remote clones read the OWNING client's Photon custom properties in `Player.Start` (`ReadPlayerID`/`ReadTeamID`), so `bot.teamID == master.teamID` everywhere. NEVER use `PlayerManager.GetOtherTeam` (hardcoded 0/1) — team-aware targeting compares `teamID` equality against the master.
- **Brain (`SovereignBotBrain`) runs ONLY on the owning client** (`view.IsMine`) — every client writing the same fake inputs would fire the bot's gun N times over Photon (bullets spawn via `PhotonNetwork.Instantiate`). All AI works through vanilla channels: `GeneralInput` writes (`data.input.aimDirection/direction/shootIsPressed`) + `PlayerAPI` (`Jump`/`Attack`/`CanShoot`), same as vanilla `PlayerAI`.
- **Block hook, no Harmony needed**: `Block.BlockAction` fires on every client (`RPCA_DoBlock` is RpcTarget.All — same as `SuperFirstBlockAction` per dev-notes). Guard `triggerType != ShieldCharge` so vanilla Shield Charge's free re-blocks don't print infinite bots; spawn only if `data.view.IsMine`.
- **FF gates via `ProjectileHit.RPCA_DoHit` prefix** (single bullet-impact chokepoint): swallow friendly bullet→our-bot hits, and our-bot bullet→master's-team hits, keyed on `ownPlayer.teamID`/victim `teamID` (NOT `ProjectileHit.team` — that's a skin, per decompile). Expansions/AoE vs bots still land (stage-1 known limitation).
- **Networking details verified in decompile**: bot bullets use `PhotonNetwork.Instantiate` + RPCs on the BOT's valid photon view (created because our clone went through PhotonNetwork.Instantiate, not local Instantiate). 1-HP death via `healthHandler.DestroyOnDeath = true` + `isPlaying = true` (needed for DoDamage to even trigger). Master death → brain despawns via `PhotonNetwork.Destroy` (owner-auth allowed/photonReplicates).
- **Pitfall (verified, fixed): every fresh player-prefab instantiate fires `PlayerManager.PlayerJoined` (from vanilla `Player.Start`), and the sandbox gamemode `GM_Test.PlayerWasAdded` teleports all such clones to `MapManager.GetRandomSpawnPos()`** — Sovereign bots first spawned "randomly on the map". Fix: `ConfigureBot` re-asserts the intended spawn pos via the PUBLIC `PlayerVelocity.position` setter + zeroed internal `velocity` field (reflection) + `PlayerCollision.IgnoreWallForFrames(2)`, re-asserted for ~3 frames after config because the config RPC can run before or after the Start-frame teleport (non-deterministic). Any mod spawning player-clone prefabs must do the same; this also predates any RWF gamemode spawn-point hook for remote clients.
- Caveats (verify in playtest): RPC vs Photon-clone arrival ordering handled by a 1s delayed config retry; bots remain unregistered so knockback sources fully apply to them (no stacking fully-blocked knockback); `wasBlocked` path skipped in our patch for now.

## Bouncy Ball card (BouncyBallCard.cs; runtime verified, ×2 knockback + ×2-ob tuning playtest pending)

- **Knockback-taken chokepoint: `HealthHandler.CallTakeForce` is the networked wrapper for ALL external knockback** — the funnel prefix scales its `force` arg, and the scaled value rides the vanilla `RPCA_SendTakeForce` Photon RPC (`RpcTarget.All`) to every client, so it syncs with zero custom networking. Callers: `ProjectileHit` (bullets), `Explosion` (incl. own rockets), `DamageBox` hazards, `NetworkPhysicsObject.OnPlayerCollision` (**boxes/props hitting players**), `LineRangeEffect`, `OutOfBoundsHandler` (pit bounce).
- **Per-source multipliers via source stamps** (two-layer Harmony pattern): patch classes set/reset a static `SourceMultiplier`/`SourceTag` around their window —   `ProjectileHit.RPCA_DoHit` prefix/postfix (bullets — stamp matches Default ×2, kept for easy retuning; it's itself an `RpcTarget.All` RPC, so it runs on every client) and `OutOfBoundsHandler.LateUpdate` ("LateUpdate" string patch, ×2, both 400×-mass-blocking and 200×-mass branches). The `CallTakeForce` funnel applies `SourceMultiplier > 0 ? SourceMultiplier : Default` (×3). Adding a source = one tiny stamp class, funnel untouched. Caveat: Harmony skips a postfix if the original throws → one stale-multiplier event until next stamp (≈0 risk).
- **Direct `TakeForce` callers bypass the patch on purpose** (holder mobility stays vanilla): jump (`Movement.cs:127`), block self-push (`Block.cs:206`), Shield Charge + DIVE charge loops, Thrusters, Saws, BeamAttack. Blocking still negates knockback — `CallTakeForce`'s `IsBlocking()` gate is untouched by the prefix.
- `HealthHandler.data` is **private**; resolve the CharacterData in a patch via `__instance.GetComponent<CharacterData>()` (same GameObject — that's exactly how vanilla `HealthHandler.Awake` gets it).
- **PITFALL (cost us a debug round): `CustomCard.SetupCard` re-runs on every spawned pick-card clone** — UnboundLib's `CustomCard.Awake` calls it, and `Instantiate` copies the component onto each clone. NEVER cache `CardInfo` by instance identity from `SetupCard`: `data.currentCards` stores `CardInfo.sourceCard` (`ApplyCardStats.cs:110`) = the **registered** prefab instance (`CardChoice.cs:333`), not a clone. SetupCard-set **stats still work** because `ApplyCardStats` copies stats off the pick-card clone's own components.
- Card-ownership check in stat-ish patches: shared `HasCard(data)` helper — `data.currentCards.Any(c => c != null && c.cardName == CardName)` — `cardName` is identical on registered instances and clones (copied field), so it's immune to the identity problem; vanilla-authoritative state, no effect component, no stack/removal bookkeeping (stack copies don't multiply — per-source multipliers are flat).
- Speed stat: `CharacterStatModifiers.movementSpeed` is a plain multiplier copied off cards (default 1) — `1.5f` = +50%. (Unlike `attackSpeedMultiplier`, which is NOT copied off cards — see DON'T above.)
- Theme enum (`CardThemeColor.CardThemeColorType`) has **no** `NatureGreen` — actual values: `DestructiveRed, FirepowerYellow, DefensiveBlue, TechWhite, EvilPurple, PoisonGreen, NatureBrown, ColdBlue, MagicPink`. Green = `PoisonGreen`.

## Dynamic Field card (DynamicFieldCard.cs; compiled clean, runtime playtest pending)

- **Extraction is an ordered fallback chain, not a single slot** (log-verified on a dead round): the vanilla card's `Block.objectsToSpawn` was EMPTY at runtime — the field object is referenced by a serialized field pointing OUTSIDE the card tree. Chain: a) `Block.objectsToSpawn[0]`, b) `CSM.AddObjectToPlayer` (vanilla "attach carrier to player on pick" recipe, ApplyCardStats.cs:137-139) — preferring its `SpawnObjects.objectToSpawn[0]` over the carrier itself, c) `Gun.objectsToSpawn[].effect`. Multiple cards can share the name "STATIC FIELD" (case-insensitive match) — iterate ALL matches and take the first that yields an object, never trust `FirstOrDefault`.
- **Vanilla `SpawnObjects.Spawn()` is a plain local `Object.Instantiate`** (decompile-verified) — no PhotonInstantiate, no PrefabPool requirement for spawned objects. Vanilla "X creates Y on block" effects work by **every client spawning its own local copy**, because `Block.BlockAction` fires on all clients (`RPCA_DoBlock` is RpcTarget.All). So Dynamic Field needs ZERO custom networking: no RPC, no IsMine gate on the spawn — the per-client local copies stay aligned because the field follows the caster's (network-synced) transform. Contrast with Sovereign bots, which need PhotonNetwork.Instantiate + broadcast RPC because they have PhotonViews/guns.
- **Attribution**: mirror vanilla `SpawnObjects.ConfigureObject` — add/assign `SpawnedAttack.spawner = <casting player>` immediately after instantiate (same synchronous frame, so field scripts resolving `transform.root.GetComponent<Player>() ?? GetComponentInParent<SpawnedAttack>().spawner` in `Start` find it; `PlayerInRangeTrigger.Start` depends on it).
- **`PlayerInRangeTrigger` scaling pitfall**: range is `range * transform.root.localScale.x` ONLY when `scaleWithRange` is true; when it's false the root scale halves visuals but NOT the slow/damage radius. We patch `trigger.range *= 0.5` only for triggers with the flag false (avoid double-halving), then `localScale *= 0.5`.
- Field lifetime is just `Object.Destroy(field, 2f)` per client (mirrored timing; nothing to sync). Follower (`DynamicFieldFollower`, per-client local) pins `field.position = master.position` in LateUpdate and destroys the field early when the master dies (Sovereign-consistent).
- **Pitfall (verified in a debug round): the in-game cardName is the literal "STATIC FIELD"** — check via the Delete card's confirm log, which prints `cardName` raw. Match with a case-insensitive comparison; a failed lookup now dumps every card name in CardChoice.cards so the exact spelling/owner pack is visible.
- Keeps vanilla Static Field's `block.cdAdd = 0.25` downside; the "half size + 3s" downgrades live in the spawned field, not the stats.
- Caveats (verify in playtest): extraction failure (CardChoice missing / no SpawnObjects on the vanilla card) logs loudly and results in a no-op block — check `[DEER] DynamicField:` logs first; vanilla stack semantics = multiple independent follow-fields, untested; RWF damage attribution inherits whatever vanilla TeamColor/SpawnedAttack wiring does (TDM watch-item).
- `[DEER]` logs present: strip together with the Sovereign/BouncyBall/AbilityHud/Dive batch after playtest.

## Full Counter card (FullCounterCard.cs; compiled clean, runtime playtest pending)

- **Bullet visuals + detection radius are spawn-time caches**: `RayCastTrail.Start` caches `size = Pow(damage,0.85)/400 + 0.3 + extraSize` (hit disc via CircleCastAll) and `SetScaleFromSizeAndExtraSize.Start` scales the sprite from that size once. Changing `ProjectileHit.damage` after spawn is invisible until you rescale: ratio-scale the `SetScaleFromSizeAndExtraSize.transform` (spawn scale happens exactly once, so ratio is exact), rewrite `RayCastTrail.size` from the same Start formula, and call vanilla `ScaleTrailFromDamage.Rescale()` (what `RayHitReflect.DoHitEffect` does after its dmgM bump; idempotent, rebuilds from cached startWidth). `GetComponentInChildren<T>(true)` needed — pooled bullets can have inactive children.
- **DON'T hook `Block.BlockProjectileAction` (or a `BlockEffect.DoBlockedProjectile`) to modify a blocked bullet.** Vanilla `Block.blocked` reverses velocity for every blocked projectile, THEN skips `BlockProjectileAction` when `SpawnedAttack.spawner.gameObject == transform.root` (`Block.cs`). That is exactly "enemy bullets double, my own bullet shot into the air and blocked does not." The reflect already happened; only the callback was withheld.
- **DO postfix `Block.blocked` and multiply `ProjectileHit.damage` (and `shake`) there.** `blocked` is the reflect itself and does not discriminate. It runs on every client because `ProjectileHit.RPCA_DoHit` (`RpcTarget.All`, `wasBlocked`) calls `Block.DoBlock` → `blocked`. Damage is a local field (`SyncProjectile` syncs pos/vel only), so the multiply needs no RPC and stays aligned, including on the bullet owner who has `hasControl` and actually applies the hit.
- Own-bullet block is otherwise vanilla: `RayCastTrail` ignores same `teamID` for 0.2s after spawn, `ProjectileHit.Start` holds the owner in `playersHit` for `holdPlayerFor` (0.5s). After that a bullet you fired can be blocked, and `blocked` already reverses it. `WasBlocked()` sets `timeAtSpawn = 0` so the post-reflect bullet can hit anyone, including you, once the 0.5s hold expires.
- Stacks multiply (`2^stacks`). Template `allowMultiple` is 0, so a normal pick is exactly ×2. Log line tags `OWN` vs `foreign` using the same spawner comparison vanilla uses to skip the callback — the OWN line is the playtest gate.
- Do not also scale `transform.localScale` on the bullet: `ProjectileHit.Start` does `damage *= localScale.x`, so a scale bump before Start double-applies.

## Global ability cooldowns (AbilityCooldowns.cs; compiled clean, runtime playtest pending)

- **Pattern: cooldown modifiers are a pure function of the deck, applied at trigger time.** `AbilityCooldowns.Apply(player, BaseCooldown)` scans `player.data.currentCards` by `cardName` (case-insensitive, same identity-immune trick as `HasCard` in BouncyBallCard) against the static `Sources` table (`cardName -> additive modifier per copy`), and returns `Base × (1 + sum)`. Quick Attack is the first source: `("Quick Attack", 0.25f)` = +25% (its only downside; it no longer has a movement debuff).
- **Why no effect component / no add-remove bookkeeping:** card removal rebuilds the whole deck and re-fires every `OnAddCard` (see Card-removal section) — an accumulate/subtract registry would drift. Stacks and removal are handled for free because they're just changes to `currentCards`.
- **Recipe for a new ability card with a cooldown:** define `internal const float BaseCooldown = ...f` on the effect, and at trigger time (owner client only — ability input is IsMine-gated, so no RPC) set `cooldownLeft = AbilityCooldowns.Apply(player, BaseCooldown);`. HUD countdown picks up the scaled value automatically.
- **Recipe for a new cooldown-MODIFYING card:** add one line to `AbilityCooldowns.Sources` and a matching `CardInfoStat` on the card. No `OnAddCard` logic.
- Migrated: InvisibilityEffect (base 10s) and PortalEffect placement (base 2.5s). Heart card NOT migrated (out of scope for now). Block cooldown (`cdAdd`/`cdMultiplier`) untouched.
- Known playtest gates: Invisibility + Quick Attack → 12.5s; Portals + Quick Attack → 3.125s; Delete Quick Attack mid-match → back to base on the next trigger.

## Shambles card (ShamblesCard.cs; compiled clean, runtime playtest pending)

- F-key (P2: G) cursor-targeted swap with the nearest "item": any player (any team, alive) or any in-flight `MoveTransform`+`ProjectileHit` bullet. Nearest candidate center to cursor world pos wins; no candidates → no-op with NO cooldown burn. Zero Harmony patches — pure input + one broadcast `UnboundRPC` (`RPC_ShamblesSwap(casterID, otherPlayerID, bulletViewID, targetPos, casterOldPos)`).
- **Bullet targeting addresses the PhotonView**: bullets are `PhotonNetwork.Instantiate`d so they carry a valid `PhotonView.ViewID` — cast-time snapshot passes `ph.view.ViewID` (fallback `GetComponent<PhotonView>`), RPC resolves `PhotonNetwork.GetPhotonView(viewID)` (same API Sovereign uses). Bullet dies before RPC arrives → `GetPhotonView` null → caster teleports anyway, log sonde.
- **Bullet momentum preserved by construction**: TeleportBullet touches ONLY the transform (+ critical `RayCastTrail.MoveRay()` snap per the portal pitfall) and never reads/writes `MoveTransform.velocity`. No `bulletArmed`-style disarm state needed — Shambles only moves a bullet on an explicit keypress, so the portal ping-pong hazard doesn't apply.
- Player-side swap = vanilla Teleport recipe × 2 (caster to target spot, target/other to caster's OLD spot as seen by the caster; both velocities zeroed). Positions travel IN the RPC so all clients write identical values — no divergence from network lag.
- Bullet post-swap continuation: an enemy bullet swapped to your old spot keeps flying along its original direction past it; `holdPlayerFor`/`playersHit` self-hit interactions with own bullets are a playtest gate.
- HUD: AbilityHUD recipe, orange ready / dark gray cooldown, key caption + 6s countdown (`BaseCooldown` consumed via `AbilityCooldowns.Apply`).

## Tooling

- Test logs readable directly at `~/.config/r2modmanPlus-local/ROUNDS/profiles/dev/BepInEx/LogOutput.log` (r2modman dev profile, no need for the user to paste).

## Conventions

- New stat cards: set values in `SetupCard` on components whose stat fields the doc says are copied off cards (Gun/Block/CharacterStatModifiers + GunAmmo).
- Debug-log `[DEER]`-prefixed lines in `Awake`/`SetupCard`/`OnAddCard` when a new mechanic is untested. Remove once proven.
