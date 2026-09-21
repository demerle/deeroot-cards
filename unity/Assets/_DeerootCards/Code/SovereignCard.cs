using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    /// fights for you. Fixed default kit, 1 HP, unlimited hires.
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

        // Downside: block recharges this much slower (Block: (cooldown + cdAdd) * cdMultiplier).
        private const float BlockCooldownAdd = 2f;

        private static bool harmonyApplied;

        public static void Init()
        {
            if (harmonyApplied) return;
            harmonyApplied = true;
            var harmony = new Harmony("com.deeroot.cards.sovereign");
            harmony.PatchAll(typeof(SovereignFriendlyFirePatch));
            SovereignBot.RegisterRoundResetHooks();
        }

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // The holder pays a slower block recharge for an infinite bot army.
            block.cdAdd = BlockCooldownAdd;
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

        private class BotEntry
        {
            public CharacterData Bot;
            public int MasterPlayerID;
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
            bot.maxHealth = BotMaxHealth;
            bot.health = BotMaxHealth;

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
                bots.Add(new BotEntry { Bot = bot, MasterPlayerID = masterPlayerID });
            }
            if (view.IsMine)
            {
                // The brain runs ONLY on the owning client: every client writing
                // the same inputs would fire the bot's gun N times over Photon.
                var brain = bot.gameObject.AddComponent<SovereignBotBrain>();
                brain.Init(master, masterPlayerID);
            }

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
}
