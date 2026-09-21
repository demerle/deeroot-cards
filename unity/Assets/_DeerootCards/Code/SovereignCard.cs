using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnityEngine;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Sovereign: every time you block, a loyal bot spawns in front of you and
    /// fights for you. Fixed default kit, 1 HP, unlimited hires. The crown is
    /// heavy: -20% max health, +0.25s gun reload, +2s block cooldown.
    ///
    /// Spawning follows the vanilla SpawnMinion recipe: a real player-clone
    /// (instantiated with PhotonNetwork.Instantiate so the game's own networking
    /// syncs position/bullets) with SetAI + its own AI brain — but deliberately
    /// NOT registered with PlayerAssigner/PlayerManager on the owner's client,
    /// so it never enters card-pick pipelines, round-end alive checks, or RWF's
    /// team/deathmatch player lists. On remote clients Photon re-registers the
    /// clone from the prefab's own code (CharacterData.Start, !IsMine branch) —
    /// that registration is stripped on the spot in RPC_SpawnBot, so a bot is
    /// invisible to game logic on every client while still being a fully
    /// simulated, shootable target.
    ///
    /// Team inheritance for free (verified in decompile): remote clones read
    /// the owning client's Photon custom properties (Player.Start ->
    /// ReadPlayerID/ReadTeamID), so bot.teamID == master.teamID everywhere —
    /// correct under ROUNDSWithFriends' N-team games without any hardcoded 0/1.
    /// </summary>
    public class SovereignCard : CustomCard
    {
        public const string CardName = "Sovereign";

        // Downsides, all applied through the vanilla card applier
        // (ApplyCardStats.ApplyStats): block.cdAdd adds to block recharge
        // (Block: (cooldown + cdAdd) * cdMultiplier), statModifiers.health
        // multiplies CharacterData.maxHealth, gun.reloadTimeAdd adds flat
        // seconds to every reload (GunAmmo.GetReloadTime:
        // (reloadTime + reloadTimeAdd) * reloadTimeMultiplier).
        private const float BlockCooldownAdd = 2f;
        private const float HealthMultiplier = 0.8f;   // -20% max health
        private const float ReloadTimeAdd = 0.25f;     // +0.25s per reload

        private static bool harmonyApplied;

        public static void Init()
        {
            if (harmonyApplied) return;
            harmonyApplied = true;
            var harmony = new Harmony("com.deeroot.cards.sovereign");
            harmony.PatchAll(typeof(SovereignFriendlyFirePatch));
            harmony.PatchAll(typeof(SovereignBotBulletPatch));
            SovereignBot.RegisterRoundResetHooks();
        }

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // The holder pays for an infinite bot army: slower block recharge,
            // frailty, and sluggish reloads. These are TEMPLATE stats — the
            // vanilla applier translates them on pick (see the constants above).
            block.cdAdd = BlockCooldownAdd;
            statModifiers.health = HealthMultiplier;
            gun.reloadTimeAdd = ReloadTimeAdd;
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            player.gameObject.GetOrAddComponent<SovereignEffect>();
            UnityEngine.Debug.Log($"[DEER] SovereignCard added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.GetComponent<SovereignEffect>();
            if (effect != null)
            {
                Destroy(effect);
            }
            // A keeperless army has no reason to stand around.
            SovereignBot.DespawnOwned(player.playerID);
        }

        protected override string GetTitle()
        {
            return CardName;
        }

        protected override string GetDescription()
        {
            return "Summon subjects to do your bidding";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "On block",
                    amount = "Summon bot",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Block cooldown",
                    amount = "+2s",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Health",
                    amount = "-20%",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Reload time",
                    amount = "+0.25s",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                }
            };
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Uncommon;
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.EvilPurple;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }

    /// <summary>
    /// Per-player hook component: wings the spawn onto the vanilla block event
    /// (Block.BlockAction) — the event fires on every client because
    /// RPCA_DoBlock is RpcTarget.All, so the spawn is gated to the master's own
    /// client (IsMine) before the broadcast RPC is even sent.
    /// </summary>
    public class SovereignEffect : MonoBehaviour
    {
        private Player player;
        private Block block;

        private void Awake()
        {
            player = GetComponent<Player>();
        }

        private void Start()
        {
            if (player == null)
            {
                player = GetComponent<Player>();
            }
            var blockComp = GetComponent<Block>();
            if (blockComp != null)
            {
                block = blockComp;
                block.BlockAction += OnBlockAction;
            }
            UnityEngine.Debug.Log($"[DEER] SovereignEffect started on player {player.data.name}");
        }

        private void OnDestroy()
        {
            if (block != null)
            {
                block.BlockAction -= OnBlockAction;
            }
        }

        private void OnBlockAction(BlockTrigger.BlockTriggerType triggerType)
        {
            // Vanilla ShieldCharge re-triggers blocks for free — never translate
            // those into an infinite bot print.
            if (triggerType == BlockTrigger.BlockTriggerType.ShieldCharge)
            {
                return;
            }
            if (player == null || player.data == null || !player.data.view.IsMine)
            {
                return; // only the master's client initiates the spawn
            }
            if (!player.data.isPlaying)
            {
                return; // never during pick phase / game over
            }

            // Spawn right in front of the player, along their shield/aim direction.
            Vector2 aim = player.data.aimDirection;
            if (aim.sqrMagnitude < 0.01f)
            {
                aim = Vector2.up;
            }
            Vector3 pos = player.transform.position + (Vector3)(aim.normalized * 1.5f) + Vector3.up * 0.3f;
            pos.z = 0f;
            SovereignBot.SpawnBot(player, pos);
        }
    }

    /// <summary>
    /// Bot factory + per-client registry + desync-safe config. All per-client
    /// work goes through one broadcast RPC that includes the bot's photon
    /// viewID, so every client configures its own clone (idempotent).
    /// </summary>
    public static class SovereignBot
    {
        // 1 HP = max health 1 (any nonzero damage kills).
        internal const float BotMaxHealth = 1f;

        // Brain tuning (verified vanilla PlayerAI/PlayerAIMinion behaviours).
        internal const float ShootRange = 14f;   // prefer shooting from mid range
        internal const float KeepRange = 7f;     // stand still once this close
        internal const float TooCloseRange = 3f; // back off below this
        internal const float FireCooldownMin = 0.25f;
        internal const float FireCooldownMax = 0.6f;
        internal const float AimCompensation = 0.02f; // up-bias per distance unit

        // Default bot kit tuning — these are applied AFTER resetting the clone
        // to vanilla defaults, so the bot is completely independent of the
        // master's cards. Change these constants to tweak the bot's kit.
        // 0.5× vanilla → 27.5 dmg/shot vs base 55 → exactly 4 shots to kill a
        // 100 HP base player (Gun.ApplyProjectileStats: damage = 55 * gun.damage).
        internal const float BotDamage = 0.5f;
        internal const float BotProjectileSpeed = 1f;
        internal const float BotProjectileSimulationSpeed = 1f;
        internal const float BotAttackSpeed = 0.3f;
        internal const float BotReloadTime = 1f;
        internal const float BotKnockback = 1f;
        internal const int BotMaxAmmo = 3;
        internal const int BotNumberOfProjectiles = 1;
        internal const int BotBursts = 0;
        internal const int BotReflects = 0;

        internal const float BotMovementSpeed = 1f;
        internal const float BotJump = 1f;
        internal const float BotGravity = 1f;
        internal const float BotSizeMultiplier = 1f;
        internal const float BotHealthMultiplier = 1f;

        private class BotEntry
        {
            public CharacterData Bot;
            public int MasterPlayerID;
            // For the bullet-shooter redirect (SovereignBotBulletPatch): the
            // bot's own view id (sentinel decode key) and the master's Photon
            // actor number (fallback when a bot dies with bullets in flight).
            public int BotViewID;
            public int MasterActorNr;
        }

        private static readonly List<BotEntry> bots = new List<BotEntry>();

        private static bool hooksRegistered;

        /// <summary>Owner-side entry: instantiate the clone, then broadcast config.</summary>
        public static void SpawnBot(Player master, Vector3 pos)
        {
            var prefab = PlayerAssigner.instance != null ? PlayerAssigner.instance.playerPrefab : null;
            if (prefab == null)
            {
                UnityEngine.Debug.LogError("[DEER] Sovereign: no PlayerAssigner.instance.playerPrefab to clone!");
                return;
            }

            // Same primitive the vanilla sandbox bot path uses (PlayerAssigner.
            // CreatePlayer): PhotonNetwork.Instantiate gives us free position sync
            // and valid PhotonView IDs for the bot's own bullets/death RPCs.
            var go = PhotonNetwork.Instantiate(prefab.name, pos, Quaternion.identity);
            var bot = go.GetComponent<CharacterData>();
            if (bot == null)
            {
                UnityEngine.Debug.LogError("[DEER] Sovereign: player prefab has no CharacterData!");
                return;
            }

            var botView = go.GetComponent<PhotonView>();
            UnityEngine.Debug.Log($"[DEER] Sovereign: master {master.playerID} blocked at {pos}, bot viewID {botView.ViewID}");

            // One broadcast configures every client's clone (owner included).
            NetworkingManager.RPC(typeof(SovereignBot), nameof(RPC_SpawnBot), master.playerID, botView.ViewID, pos.x, pos.y);
        }

        /// <summary>
        /// Per-client clone configuration. The clone auto-inherits master's
        /// playerID/teamID on remote clients (vanilla Player.Start reads the
        /// owner's Photon props); we reinforce it locally and finish the
        /// SpawnMinion recipe.
        /// </summary>
        [UnboundRPC]
        public static void RPC_SpawnBot(int masterPlayerID, int botViewID, float spawnPosX, float spawnPosY)
        {
            StartCoroutineDeferred(botViewID, masterPlayerID, new Vector3(spawnPosX, spawnPosY, 0f));
        }

        /// <summary>
        /// Resets the bot's gun, character stats, and block to vanilla defaults,
        /// then applies the tunable bot-kit constants. This makes the bot's
        /// stats completely independent of whatever cards the master has picked.
        /// </summary>
        private static void ResetBotToDefaultKit(CharacterData bot)
        {
            // Pre-flight: Gun.ResetStats() dereferences the private `gunAmmo`
            // field, which Gun only assigns in its own Start() — our config can
            // run before that, so the reset would NRE (verified in playtest:
            // the NRE aborted ConfigureBot and left brainless, passive bots).
            // Patch it exactly like Gun.Start() does, before FullReset().
            if (bot.weaponHandler != null && bot.weaponHandler.gun != null)
            {
                var gun = bot.weaponHandler.gun;
                var gunAmmoField = AccessTools.Field(typeof(Gun), "gunAmmo");
                if (gunAmmoField != null && gunAmmoField.GetValue(gun) == null)
                {
                    gunAmmoField.SetValue(gun, gun.GetComponentInChildren<GunAmmo>());
                }
            }
            // Same class of problem: CharacterStatModifiers.ResetStats iterates
            // objectsAddedToPlayer (public field, initialized on first use).
            if (bot.stats != null && bot.stats.objectsAddedToPlayer == null)
            {
                bot.stats.objectsAddedToPlayer = new List<GameObject>();
            }

            // Player.FullReset() is internal; use reflection (same idiom as the
            // rest of the mod). It resets gun, CharacterStatModifiers, and block.
            // Wrapped so a vanilla reset hiccup can never again abort bot
            // configuration — the kit constants below still produce a working
            // default bot on their own.
            try
            {
                var fullReset = typeof(Player).GetMethod(
                    "FullReset",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (fullReset != null)
                {
                    fullReset.Invoke(bot.player, null);
                }
                else
                {
                    UnityEngine.Debug.LogError("[DEER] Sovereign: could not reflect Player.FullReset; bot may inherit master stats.");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[DEER] Sovereign: FullReset failed, continuing with kit constants: {e.Message}");
            }

            // Apply the tunable bot kit on top of vanilla defaults.
            if (bot.weaponHandler != null && bot.weaponHandler.gun != null)
            {
                var gun = bot.weaponHandler.gun;
                gun.damage = BotDamage;
                gun.projectileSpeed = BotProjectileSpeed;
                gun.projectielSimulatonSpeed = BotProjectileSimulationSpeed;
                gun.attackSpeed = BotAttackSpeed;
                gun.reloadTime = BotReloadTime;
                gun.knockback = BotKnockback;
                gun.numberOfProjectiles = BotNumberOfProjectiles;
                gun.bursts = BotBursts;
                gun.reflects = BotReflects;

                var gunAmmo = gun.GetComponentInChildren<GunAmmo>();
                if (gunAmmo != null)
                {
                    gunAmmo.maxAmmo = BotMaxAmmo;
                    gunAmmo.ReDrawTotalBullets();
                }
            }

            if (bot.stats != null)
            {
                bot.stats.movementSpeed = BotMovementSpeed;
                bot.stats.jump = BotJump;
                bot.stats.gravity = BotGravity;
                bot.stats.sizeMultiplier = BotSizeMultiplier;
                bot.stats.health = BotHealthMultiplier;
            }

            // FullReset() sets health/maxHealth to 100; re-apply the bot's 1 HP.
            bot.maxHealth = BotMaxHealth;
            bot.health = BotMaxHealth;

            // Re-apply size/mass now that we've overridden the stat multipliers.
            bot.stats?.WasUpdated();

            UnityEngine.Debug.Log($"[DEER] Sovereign: bot kit reset to defaults (HP {bot.maxHealth}, damage {bot.weaponHandler?.gun?.damage}, projectileSpeed {bot.weaponHandler?.gun?.projectileSpeed})");
        }

        private static void ConfigureBot(int masterPlayerID, int botViewID, Vector3 spawnPos)
        {
            var view = PhotonNetwork.GetPhotonView(botViewID);
            if (view == null || view.gameObject == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] Sovereign: bot view {botViewID} vanished before config");
                return;
            }
            var bot = view.GetComponent<CharacterData>();
            if (bot == null)
            {
                return;
            }
            Player master = PlayerManager.instance != null
                ? PlayerManager.instance.players.FirstOrDefault(p => p != null && p.playerID == masterPlayerID)
                : null;

            if (master == null)
            {
                // Master is already gone (left room) — don't leave an orphan army.
                if (view.IsMine)
                {
                    PhotonNetwork.Destroy(view.gameObject);
                }
                return;
            }

            bot.player.playerID = master.playerID;
            bot.player.teamID = master.teamID;
            bot.player.SetColors();
            bot.SetAI(master);
            bot.isPlaying = true;
            bot.healthHandler.DestroyOnDeath = true;

            var skin = bot.GetComponentInChildren<PlayerSkinHandler>(true);
            if (skin != null)
            {
                skin.ToggleSimpleSkin(true);
            }

            if (!view.IsMine)
            {
                // The prefab's own code (CharacterData.Start, !IsMine branch)
                // registers remote clones into PlayerManager — undo that; we
                // never want a bot in card-pick / alive-count logic.
                var pm = PlayerManager.instance;
                if (pm != null && pm.players.Contains(bot.player))
                {
                    pm.players.Remove(bot.player);
                    UnityEngine.Debug.Log("[DEER] Sovereign: stripped remote clone from PlayerManager registry");
                }
            }

            if (bots.All(e => e.Bot != bot))
            {
                bots.Add(new BotEntry
                {
                    Bot = bot,
                    MasterPlayerID = masterPlayerID,
                    BotViewID = botViewID,
                    MasterActorNr = master.data.view.OwnerActorNr,
                });
            }
            if (view.IsMine)
            {
                // The brain runs ONLY on the owning client: every client writing
                // the same inputs would fire the bot's gun N times over Photon.
                var brain = bot.gameObject.AddComponent<SovereignBotBrain>();
                brain.Init(master, masterPlayerID);
            }

            // Kit reset LAST: even if the vanilla reset throws (it's wrapped,
            // but belt-and-braces), the bot above is already fully alive.
            ResetBotToDefaultKit(bot);

            // Position fix: every fresh player-prefab instantiate fires
            // PlayerManager.PlayerJoined (from Player.Start); the sandbox
            // gamemode (GM_Test.PlayerWasAdded) teleports new players to
            // MapManager.GetRandomSpawnPos() — that's why bots landed randomly.
            // Re-assert the intended spawn spot AFTER config, and keep
            // re-asserting for a few frames since our config RPC can run either
            // before or after that Start-frame teleport (ordering
            // non-deterministic). Same enforcement covers any RWF gamemode hook
            // that does the equivalent on remote clients.
            var host = SovereignRunner.Get();
            host.StartCoroutine(EnforcePosition(bot, spawnPos));
            UnityEngine.Debug.Log($"[DEER] Sovereign: bot configured (owner client: {view.IsMine}, master {masterPlayerID}, HP {bot.maxHealth})");
        }

        private static IEnumerator EnforcePosition(CharacterData bot, Vector3 spawnPos)
        {
            if (bot == null)
            {
                yield break;
            }
            var playerVel = bot.GetComponent<PlayerVelocity>();
            if (playerVel == null)
            {
                yield break;
            }
            for (int frame = 0; frame < 3; frame++)
            {
                playerVel.position = spawnPos;
                ZeroVelocity(playerVel);
                // ignore walls for the relocation (same pattern as portal
                // teleports) so the position apply doesn't clip into terrain
                var collision = bot.GetComponent<PlayerCollision>();
                if (collision != null)
                {
                    collision.IgnoreWallForFrames(2);
                }
                UnityEngine.Debug.Log($"[DEER] Sovereign: pos frame {frame}: want {spawnPos} got {bot.transform.position}");
                yield return null;
            }
        }

        private static void ZeroVelocity(PlayerVelocity playerVel)
        {
            if (velocityField == null)
            {
                velocityField = AccessTools.Field(typeof(PlayerVelocity), "velocity");
            }
            velocityField?.SetValue(playerVel, Vector2.zero);
        }

        private static System.Reflection.FieldInfo velocityField;

        private static void StartCoroutineDeferred(int botViewID, int masterPlayerID, Vector3 spawnPos)
        {
            // The networked clone may arrive after the RPC on a given client
            // (Photon doesn't order these globally) — retry for up to a second.
            var host = SovereignRunner.Get();
            host.StartCoroutine(DeferredConfig(botViewID, masterPlayerID, spawnPos));
        }

        private static IEnumerator DeferredConfig(int botViewID, int masterPlayerID, Vector3 spawnPos)
        {
            for (float waited = 0f; waited < 1f; waited += Time.unscaledDeltaTime)
            {
                if (PhotonNetwork.GetPhotonView(botViewID) != null)
                {
                    break;
                }
                yield return null;
            }
            // Let the clone's first Awake/Start cycle finish (Gun.Start assigns
            // its private gunAmmo field, etc.) before we reset its kit.
            yield return null;
            ConfigureBot(masterPlayerID, botViewID, spawnPos);
        }

        /// <summary>Destroys every bot whose master has this playerID (all clients).</summary>
        public static void DespawnOwned(int masterPlayerID)
        {
            foreach (var entry in bots.Where(e => e.MasterPlayerID == masterPlayerID).ToList())
            {
                DestroyBot(entry.Bot);
            }
            bots.RemoveAll(e => e.MasterPlayerID == masterPlayerID);
        }

        /// <summary>Master died / card removed — collect a player's minions.</summary>
        public static void DespawnBot(CharacterData bot)
        {
            DestroyBot(bot);
            bots.RemoveAll(e => e.Bot == bot || ReferenceEquals(e.Bot, null));
        }

        public static void RegisterRoundResetHooks()
        {
            if (hooksRegistered)
            {
                return;
            }
            hooksRegistered = true;
            // Round boundaries in vanilla AND RWF games both drive these hooks.
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, gm => ResetAllForRound());
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, gm => ResetAllForRound());
            GameModeManager.AddHook(GameModeHooks.HookGameStart, gm => ResetAllForRound());
        }

        private static IEnumerator ResetAllForRound()
        {
            foreach (var entry in bots.ToList())
            {
                DestroyBot(entry.Bot);
            }
            bots.RemoveAll(e => ReferenceEquals(e.Bot, null) || e.Bot == null);
            yield break;
        }

        private static void DestroyBot(CharacterData bot)
        {
            if (bot == null)
            {
                return;
            }
            var view = bot.view;
            if (view != null && view.IsMine)
            {
                PhotonNetwork.Destroy(view.gameObject); // replicates to all clients
            }
            else
            {
                // Not our view (remote clone on this client, or offline) — local
                // destroy only; the owning client replicates its own teardown.
                UnityEngine.Object.Destroy(bot.gameObject);
            }
        }

        internal static bool IsBot(Player p)
        {
            return p != null && p.data != null && bots.Any(e => e.Bot != null && e.Bot.player == p);
        }

        internal static bool IsBotData(CharacterData data)
        {
            return data != null && bots.Any(e => e.Bot == data);
        }

        /// <summary>
        /// Sentinel decode: viewID → the bot's CharacterData, but ONLY while its
        /// registry entry still exists (a despawned bot's clone may linger a
        /// frame — its half-destroyed gun must not receive BulletInit).
        /// </summary>
        internal static CharacterData ResolveBotByViewID(int viewID)
        {
            var view = PhotonNetwork.GetPhotonView(viewID);
            if (view == null)
            {
                return null;
            }
            var data = view.GetComponent<CharacterData>();
            return IsBotData(data) ? data : null;
        }

        /// <summary>Master's Photon actor number for a bot view, or -1 if the bot is gone.</summary>
        internal static int GetMasterActorNrForBotView(int botViewID)
        {
            var entry = bots.FirstOrDefault(e => e.BotViewID == botViewID);
            return entry?.MasterActorNr ?? -1;
        }
    }

    /// <summary>
    /// The bot's brain — runs ONLY on the owning client (PhotonView IsMine), so
    /// the gun fires exactly once across the network and other clients just
    /// receive the synced position/bullets. A team-aware rewrite of the vanilla
    /// sandbox brain (PlayerAI): same fake-input channel (GeneralInput through
    /// data.input), but targeting = nearest ALIVE enemy outside the master's
    /// team — teamID equality works identically for vanilla (0 vs 1) and RWF's
    /// N-team games, and never uses the hardcoded 0/1 helper GetOtherTeam.
    /// No target of the right team ⇒ stand still, never shoot anyone.
    /// </summary>
    public class SovereignBotBrain : MonoBehaviour
    {
        private CharacterData data;
        private PlayerAPI api;
        private Player master;
        private int masterPlayerID;

        private Player target;
        private bool canSeeTarget;
        private float distanceToTarget;
        private float untilNextDataUpdate;
        private float untilNextShot;
        private Vector3 aimDir;
        private Vector3 moveDir;
        private Vector3 targetPos;

        public void Init(Player masterPlayer, int masterID)
        {
            master = masterPlayer;
            data = GetComponent<CharacterData>();
            api = GetComponent<PlayerAPI>();
        }

        private void Update()
        {
            float dt = TimeHandler.deltaTime;

            // Master gone (died / left): collect yourself immediately.
            if (master == null || master.data == null || data == null)
            {
                DespawnSelf();
                return;
            }
            if (master.data.dead)
            {
                UnityEngine.Debug.Log($"[DEER] Sovereign: master died — despawning bot");
                DespawnSelf();
                return;
            }

            untilNextDataUpdate -= dt;
            untilNextShot -= dt;
            AcquireTarget();
            if (target == null)
            {
                // No enemy outside the master's team — idle (this is the RWF
                // degenerate case: never shoot a teammate as a fallback).
                data.input.ResetInput();
                return;
            }
            canSeeTarget = PlayerManager.instance != null &&
                           PlayerManager.instance.CanSeePlayer(transform.position, target).canSee;

            data.input.ResetInput();
            if (untilNextDataUpdate <= 0f)
            {
                untilNextDataUpdate = Random.Range(0f, 0.25f);
                UpdateData();
            }
            if (data.isWallGrab || !data.isGrounded)
            {
                api.Jump();
            }
            data.input.aimDirection = aimDir;
            data.input.direction = moveDir;

            if (canSeeTarget && distanceToTarget < SovereignBot.ShootRange && untilNextShot <= 0f && api.CanShoot())
            {
                api.Attack();
                untilNextShot = Random.Range(SovereignBot.FireCooldownMin, SovereignBot.FireCooldownMax);
            }
        }

        private void UpdateData()
        {
            targetPos = canSeeTarget ? target.transform.position : FindSearchPos();
            distanceToTarget = Vector3.Distance(transform.position, targetPos);
            aimDir = (targetPos - transform.position).normalized;
            // light gravity compensation so long shots bend down correctly
            aimDir += Vector3.up * (distanceToTarget * SovereignBot.AimCompensation);
            aimDir.Normalize();

            moveDir = new Vector3(Mathf.Sign(aimDir.x) * (Mathf.Abs(aimDir.x) > 0.2f ? 1f : 0f), 0f, 0f);
            if (canSeeTarget)
            {
                if (distanceToTarget < SovereignBot.TooCloseRange)
                {
                    // back off
                    moveDir = new Vector3(-moveDir.x, 0f, 0f);
                }
                else if (distanceToTarget <= SovereignBot.KeepRange && data.ThereIsGroundBelow(transform.position, 10f))
                {
                    moveDir = Vector3.zero; // hold position and shoot
                }
            }
        }

        private void AcquireTarget()
        {
            Player best = null;
            float bestDist = float.MaxValue;
            var pm = PlayerManager.instance;
            if (pm == null)
            {
                return;
            }
            int myTeam = master.teamID;
            foreach (var p in pm.players)
            {
                if (p == null || p.data == null || p.data.dead || !p.data.isPlaying)
                {
                    continue;
                }
                if (ReferenceEquals(p, master) || p.teamID == myTeam)
                {
                    continue; // never target the master or their team — ever
                }
                float d = Vector3.Distance(transform.position, p.transform.position);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = p;
                }
            }
            target = best;
        }

        private Vector3 FindSearchPos()
        {
            // vanilla PlayerAI-style random roam anchored on reachable ground
            Vector3 v = Vector3.zero;
            int tries = 10;
            while (v == Vector3.zero && tries-- > 0)
            {
                Vector3 candidate = transform.position + Vector3.up * 5f + (Vector3)Random.insideUnitCircle * 15f;
                if (data.ThereIsGroundBelow(candidate, 8f))
                {
                    v = candidate;
                }
            }
            return v == Vector3.zero ? transform.position : v;
        }

        private void DespawnSelf()
        {
            SovereignBot.DespawnBot(data);
        }
    }

    /// <summary>
    /// Minimal coroutine host for the deferred config polling (DontDestroyOnLoad,
    /// DeleteOverlayUI/AbilityHUD driver style).
    /// </summary>
    public class SovereignRunner : MonoBehaviour
    {
        private static SovereignRunner runner;

        internal static SovereignRunner Get()
        {
            if (runner == null)
            {
                var go = new GameObject("DeerootSovereignRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<SovereignRunner>();
            }
            return runner;
        }

        private void OnDestroy()
        {
            if (runner == this)
            {
                runner = null;
            }
        }
    }

    /// <summary>
    /// Friendly-fire gates around our bots, via a prefix on
    /// ProjectileHit.RPCA_DoHit (the single bullet-impact chokepoint — verified
    /// split file):
    ///   - enemy bullets hitting our bots: pass through untouched (bots are
    ///     valid targets — that's the point),
    ///   - ALly bullets (ownPlayer on the master's side) hitting a Sovereign bot
    ///     are swallowed, so teammates can't wipe your army by accident,
    ///   - Sovereign-bot bullets never damage their master's team (targeting
    ///     already avoids it; this covers stray shots).
    /// Explosion/AoE damage vs bots still lands (known stage-1 limitation).
    /// </summary>
    [HarmonyPatch]
    public class SovereignFriendlyFirePatch
    {
        [HarmonyPatch(typeof(ProjectileHit), nameof(ProjectileHit.RPCA_DoHit))]
        [HarmonyPrefix]
        static bool Prefix(ProjectileHit __instance, int viewID, bool wasBlocked)
        {
            if (viewID == -1)
            {
                return true; // map/hazard collision — nothing to gate
            }
            var shooter = __instance.ownPlayer;
            if (shooter == null)
            {
                return true;
            }

            var victimView = PhotonNetwork.GetPhotonView(viewID);
            if (victimView == null)
            {
                return true;
            }
            // Victim data: players share the GameObject (vanilla pattern).
            var victimData = victimView.GetComponent<CharacterData>();
            if (victimData == null || victimData.player == null)
            {
                return true;
            }

            // Case 1: one of our bots is the victim.
            if (SovereignBot.IsBot(victimData.player))
            {
                if (victimData.player.teamID == shooter.teamID)
                {
                    UnityEngine.Debug.Log("[DEER] Sovereign: swallowed friendly hit on bot");
                    return false;
                }
                return true; // enemy bullet on bot — it lands (bot has 1 HP anyway)
            }

            // Case 2: our bot's bullet hits the master's team.
            if (SovereignBot.IsBot(shooter) && victimData.player.teamID == shooter.teamID)
            {
                UnityEngine.Debug.Log("[DEER] Sovereign: swallowed bot hit on own team");
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Bullet-shooter redirect — closes the LAST master→bot leak.
    ///
    /// ROUNDS initializes every spawned bullet from the gun of whichever
    /// REGISTERED player owns the shooter's actor number: Gun.FireBurst raises
    /// RPCA_Init with holdable.holder.view.OwnerActorNr (Gun.cs:495) and
    /// ProjectileInit.RPCA_Init resolves GetPlayerWithActorID(senderID) and
    /// calls THAT player's gun.BulletInit (ProjectileInit.cs:11). Our bot's
    /// view is owned by the master's client, so bot bullets were initialized
    /// from the MASTER's gun: his damage, his projectile speed, and his
    /// gun.objectsToSpawn — which is how the poison card's RayHitPoison object
    /// got stapled onto bot bullets (BulletInit → ApplyProjectileStats,
    /// Gun.cs:617-628 instantiates objectsToSpawn as bullet children).
    ///
    /// Fix: tag the bot's bullet-init RPCs with a sentinel shooter id and
    /// decode them back to the bot on every client:
    ///   1. Gun.Attack prefix — bot guns set pendingBotShooter. Safe because
    ///      the whole spawn path runs synchronously inside Attack (FireBurst
    ///      yields only AFTER the spawn loops, Gun.cs:529, and the bot kit is
    ///      bursts=0/uncharged → exactly one synchronous burst).
    ///   2. PhotonView.RPC prefix — while the flag is up, rewrite senderID
    ///      (parameters[0]) of RPCA_Init* to -botViewID. Photon actor numbers
    ///      are always positive, so the sentinel is unambiguous, and it rides
    ///      the EXISTING init RPC to every client — no extra traffic, no
    ///      arrival-order race.
    ///   3. ProjectileInit.RPCA_Init prefix — negative id → resolve the bot's
    ///      view → BulletInit on the BOT's gun (public method), skip vanilla.
    ///      Stale sentinel (bot died mid-flight) → fall back to the master's
    ///      actor number, i.e. pre-patch behavior for orphan bullets.
    ///   4. OFFLINE_Init prefix — offline mode raises no RPC; the direct call
    ///      still happens inside the Attack window, so the flag works there.
    ///
    /// CONSTRAINT: BotBursts must stay 0 — bursts > 1 yield between shots,
    /// which would clear the flag before later burst bullets spawn.
    /// </summary>
    [HarmonyPatch]
    internal static class SovereignBotBulletPatch
    {
        private const string InitRpcName = "RPCA_Init";
        private const string InitNoAmmoRpcName = "RPCA_Init_noAmmoUse";

        /// <summary>The bot firing right now — valid only inside its Gun.Attack window.</summary>
        private static CharacterData pendingBotShooter;

        // --- 1. mark the shooter while a bot's Attack is on the stack -------

        [HarmonyPatch(typeof(Gun), nameof(Gun.Attack))]
        [HarmonyPrefix]
        static void MarkBotShooter(Gun __instance)
        {
            var holder = __instance.holdable != null ? __instance.holdable.holder : null;
            if (holder != null && SovereignBot.IsBotData(holder))
            {
                pendingBotShooter = holder;
            }
        }

        [HarmonyPatch(typeof(Gun), nameof(Gun.Attack))]
        [HarmonyFinalizer]
        static System.Exception UnmarkBotShooter()
        {
            pendingBotShooter = null;
            return null; // never swallow exceptions — just clear the flag
        }

        // --- 2. sentinel-encode the init RPC at raise time -------------------

        [HarmonyPatch(typeof(PhotonView), nameof(PhotonView.RPC),
            new[] { typeof(string), typeof(RpcTarget), typeof(object[]) })]
        [HarmonyPrefix]
        static void EncodeSentinel(PhotonView __instance, string methodName, object[] parameters)
        {
            if (pendingBotShooter == null || parameters == null || parameters.Length == 0)
            {
                return;
            }
            if (methodName != InitRpcName && methodName != InitNoAmmoRpcName)
            {
                return;
            }
            if (__instance == pendingBotShooter.view)
            {
                return; // paranoia: never touch RPCs raised on the bot's own view
            }
            parameters[0] = -pendingBotShooter.view.ViewID;
        }

        // --- 3. decode on every client ---------------------------------------

        [HarmonyPatch(typeof(ProjectileInit), InitRpcName)]
        [HarmonyPrefix]
        static bool DecodeInit(ProjectileInit __instance, ref int senderID, int nrOfProj, float dmgM, float randomSeed)
        {
            return DecodeBotBullet(__instance, ref senderID, nrOfProj, dmgM, randomSeed, useAmmo: true);
        }

        [HarmonyPatch(typeof(ProjectileInit), InitNoAmmoRpcName)]
        [HarmonyPrefix]
        static bool DecodeInitNoAmmo(ProjectileInit __instance, ref int senderID, int nrOfProj, float dmgM, float randomSeed)
        {
            return DecodeBotBullet(__instance, ref senderID, nrOfProj, dmgM, randomSeed, useAmmo: false);
        }

        // --- 4. offline path (no RPC — direct call inside the Attack window) --

        [HarmonyPatch(typeof(ProjectileInit), "OFFLINE_Init")]
        [HarmonyPrefix]
        static bool DecodeOfflineInit(ProjectileInit __instance, int nrOfProj, float dmgM, float randomSeed)
        {
            return InitBotBullet(__instance, nrOfProj, dmgM, randomSeed, useAmmo: true);
        }

        [HarmonyPatch(typeof(ProjectileInit), "OFFLINE_Init_noAmmoUse")]
        [HarmonyPrefix]
        static bool DecodeOfflineInitNoAmmo(ProjectileInit __instance, int nrOfProj, float dmgM, float randomSeed)
        {
            return InitBotBullet(__instance, nrOfProj, dmgM, randomSeed, useAmmo: false);
        }

        private static bool DecodeBotBullet(ProjectileInit __instance, ref int senderID, int nrOfProj, float dmgM, float randomSeed, bool useAmmo)
        {
            if (senderID >= 0)
            {
                return true; // a normal player's bullet — vanilla path
            }

            var bot = SovereignBot.ResolveBotByViewID(-senderID);
            if (bot != null && bot.weaponHandler != null && bot.weaponHandler.gun != null)
            {
                bot.weaponHandler.gun.BulletInit(__instance.gameObject, nrOfProj, dmgM, randomSeed, useAmmo);
                UnityEngine.Debug.Log($"[DEER] Sovereign: bot bullet init from BOT gun (damage {bot.weaponHandler.gun.damage}, objectsToSpawn {bot.weaponHandler.gun.objectsToSpawn.Length})");
                return false;
            }

            // Stale sentinel: the bot died while its bullet was in flight.
            // Fall back to the master's actor — pre-patch behavior for orphans.
            var masterActor = SovereignBot.GetMasterActorNrForBotView(-senderID);
            if (masterActor > 0)
            {
                senderID = masterActor;
                return true;
            }
            return false; // nothing left to resolve against — leave the bullet vanilla-default
        }

        private static bool InitBotBullet(ProjectileInit __instance, int nrOfProj, float dmgM, float randomSeed, bool useAmmo)
        {
            var bot = pendingBotShooter;
            if (bot == null || bot.weaponHandler == null || bot.weaponHandler.gun == null)
            {
                return true;
            }
            bot.weaponHandler.gun.BulletInit(__instance.gameObject, nrOfProj, dmgM, randomSeed, useAmmo);
            return false;
        }
    }
}
