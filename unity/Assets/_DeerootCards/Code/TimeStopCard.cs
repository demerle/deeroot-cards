using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Photon.Pun;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.GameModes;
using UnityEngine;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Time Stop: ONE TIME USE ability card (OneShotAbility, Meteor recipe).
    /// Pressing the key stops time for a fixed real-time window:
    ///
    /// - The world is frozen by pinning the game's own time system: a Harmony
    ///   prefix on TimeHandler.Update writes `TimeHandler.timeScale`,
    ///   `static deltaTime` and `static fixedDeltaTime` to 0 while the stop is
    ///   active. Everything the game drives on scaled time freezes: player
    ///   sims (PlayerVelocity/PlayerMovement/Gravity all scale by timeScale),
    ///   bullets (MoveTransform uses TimeHandler.deltaTime), DoT, regen,
    ///   cooldowns, timers.
    /// - The caster (and any player who "enters" during the stop) keeps acting:
    ///   each of their personal ticking methods (PlayerVelocity.FixedUpdate,
    ///   PlayerMovement.FixedUpdate, Gravity.FixedUpdate, CharacterData
    ///   Update/FixedUpdate, PlayerJump.Update, Gun.Update, GunAmmo.Update) is
    ///   hijacked on the owning client with the stash-restore idiom: pin the
    ///   static to real dt during the call, restore the 0 right after — vanilla
    ///   bodies run unmodified at agent speed.
    /// - Frozen players become statues: inputs gated via the vanilla
    ///   `stunnedInput` recipe (GeneralInput.cs:98), the trigger methods hard-
    ///   gated (Gun.Attack / Block.RPCA_DoBlock — vanilla PlayerAI writes
    ///   data.input directly and would otherwise bypass the gate), and their
    ///   integration skipped. Forces landing on a statue (shockwaves,
    ///   knockback) QUEUE in its stored velocity and launch it the moment
    ///   time resumes; cooldowns (reload, block, our ability cards) freeze
    ///   with everything else because they all tick on the pinned clock.
    /// - EVERY bullet freezes no matter whose it is (MoveTransform skipped):
    ///   shots fired during the stop spawn at the muzzle and hang there with
    ///   full stored velocity; the caught volley launches on resume.
    /// - Colors go black and white and inverted: every active camera gets a
    ///   tiny OnRenderImage post pass with a bundled negative-monochrome
    ///   shader (raw bytes embedded in the DLL by TimeStopBundleBuilder).
    ///
    /// Any frozen player holding at least one Deeroot ability card may press
    /// the key during an active stop to "enter" it (start acting); anyone
    /// inside — including the caster — may press again to "fall out" while
    /// time is still stopped; the stop outlives them until its timer. Time
    /// resumes automatically; round hooks clear leftovers.
    /// </summary>
    public class TimeStopCard : CustomCard
    {
        public const string CardName = "Time Stop";

        // Tunables (real-time seconds; the stop runs on the un-frozen clock).
        internal const float StopDuration = 4f;
        internal const float MaxStopDuration = 10f;

        // Per-CLIENT key table (Meteor recipe): default U; only true
        // split-screen (2+ IsMine players) local index 1 uses I. U/I are unused
        // by our other cards (C/V F/G T/Y X/B H E/Q RightShift/RightControl are).
        internal const KeyCode KeyP1 = KeyCode.U;
        internal const KeyCode KeyP2 = KeyCode.I;

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // per-player unique: won't be re-offered once this player holds it
            cardInfo.allowMultiple = false;
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // Vanilla pick pipeline re-runs during UnboundLib's removal rebuild;
            // re-adding is fine and re-arms the HUD icon (Meteor recipe).
            var effect = player.gameObject.GetOrAddComponent<TimeStopEffect>();
            effect.SetPlayer(player);
            UnityEngine.Debug.Log($"[DEER] TimeStopCard added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.GetComponent<TimeStopEffect>();
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        protected override string GetTitle()
        {
            return CardName;
        }

        protected override string GetDescription()
        {
            return "Stop time. You keep acting while everyone else stands frozen; bullets hang mid-air until time resumes, and they hang where fired, too. Another ability holder may press their key to 'enter' the stopped world; anyone inside may press again to 'fall out' while time is still stopped. One time use.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Time",
                    amount = "Stops",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Other players",
                    amount = "Frozen",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Uses",
                    amount = "1",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                }
            };
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Rare;
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.TechWhite;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }

        public static void Init()
        {
            TimeStopState.EnsureDriver();
            TimeStopVisual.LoadAssets(); // embedded bundle → shader/material; loud log on failure
            var harmony = new Harmony("com.deeroot.cards.timestop");
            harmony.PatchAll(typeof(TimeStopPatches));
            // Round boundaries: everything frozen must release cleanly.
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, gm => TimeStopState.EndAllForRound());
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, gm => TimeStopState.EndAllForRound());
            GameModeManager.AddHook(GameModeHooks.HookGameStart, gm => TimeStopState.EndAllForRound());
        }
    }

    // ---------------------------------------------------------------
    // Authoritative per-client state, kept identical via broadcast RPCs
    // (Meteor/Shambles flow: the origin client decides, every client runs
    // the same state). Each client runs its own unscaled timeout — same
    // duration, same wall-clock start ⇒ consistent stop worldwide.
    // ---------------------------------------------------------------
    internal static class TimeStopState
    {
        private static bool active;
        private static readonly HashSet<int> agents = new HashSet<int>();

        /// <summary>
        /// Time.unscaledTime when the window closes. The game clock is pinned
        /// to 0 the whole window, so the stop must run on the real-time clock.
        /// </summary>
        internal static float EndTime { get; private set; }

        internal static bool IsActive
        {
            get { return active; }
        }

        internal static bool IsAgent(int playerID)
        {
            return active && agents.Contains(playerID);
        }

        internal static void EnsureDriver()
        {
            if (Object.FindObjectOfType<TimeStopDriver>() != null)
            {
                return;
            }
            var go = new GameObject("TimeStopDriver");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<TimeStopDriver>();
        }

        internal static void Begin(int casterPlayerID, float duration)
        {
            active = true;
            agents.Clear();
            agents.Add(casterPlayerID);
            EndTime = Time.unscaledTime + Mathf.Clamp(duration, 0.5f, TimeStopCard.MaxStopDuration);
            TimeStopVisual.Apply();
            UnityEngine.Debug.Log($"[DEER] TimeStop START caster {casterPlayerID} for {duration:F1}s ({agents.Count} inside)");
        }

        internal static void End()
        {
            if (!active)
            {
                return;
            }
            active = false;
            agents.Clear();
            TimeStopVisual.Restore();
            foreach (var data in UnityEngine.Object.FindObjectsOfType<CharacterData>())
            {
                // Leave vanilla stuns alone: StunHandler owns stunnedInput in
                // that case and un-sets it on its own schedule (Update-driven).
                if (data != null && data.input != null && !data.isStunned)
                {
                    data.input.stunnedInput = false;
                }
            }
            UnityEngine.Debug.Log("[DEER] TimeStop END — time resumes");
        }

        internal static void ToggleAgent(int playerID, bool join)
        {
            if (join)
            {
                agents.Add(playerID);
            }
            else
            {
                agents.Remove(playerID);
            }
            UnityEngine.Debug.Log($"[DEER] TimeStop agent toggle player {playerID} join={join} ({agents.Count} inside)");
        }

        internal static System.Collections.IEnumerator EndAllForRound()
        {
            End(); // idempotent; also covers stray scene switches
            yield break;
        }

        // -----------------------------------------------------------
        // Networking — broadcast RPCs (Unbound recipe; statics + primitives
        // only, verified Dev-notes pattern with [UnboundRPC]).
        // -----------------------------------------------------------
        internal static void BroadcastStart(int casterPlayerID, float duration)
        {
            NetworkingManager.RPC(typeof(TimeStopState), nameof(RPCA_TimeStopStart), casterPlayerID, duration);
        }

        internal static void BroadcastToggle(int playerID, bool join)
        {
            NetworkingManager.RPC(typeof(TimeStopState), nameof(RPCA_TimeStopToggleAgent), playerID, join);
        }

        [UnboundLib.Networking.UnboundRPC]
        public static void RPCA_TimeStopStart(int casterPlayerID, float duration)
        {
            Begin(casterPlayerID, duration);
        }

        [UnboundLib.Networking.UnboundRPC]
        public static void RPCA_TimeStopToggleAgent(int playerID, bool join)
        {
            ToggleAgent(playerID, join);
        }

        /// <summary>Cast eligibility — the user holds the exact card.</summary>
        internal static bool HoldsTimeStop(Player player)
        {
            return player != null && player.data != null && player.data.currentCards != null &&
                   player.data.currentCards.Any(c => c != null && c.cardName == TimeStopCard.CardName);
        }

        /// <summary>
        /// "Another player has ability" — holds at least one Deeroot ability
        /// card (keep in sync with our ability cards; Time Stop counts itself).
        /// Note: Portal and Blink have no CardName consts — literal titles.
        /// </summary>
        internal static readonly HashSet<string> AbilityCardNames = new HashSet<string>
        {
            TimeStopCard.CardName,
            MeteorCard.CardName,
            "Portals",
            "Blink",
            "DIVE",
            "Heart",
            SovereignCard.CardName,
            SimulacrumCard.CardName,
            AmpWallCard.CardName,
            FullCounterCard.CardName,
            DynamicFieldCard.CardName,
            "Shambles",
            "Invisibility",
            "Quick Attack",
            DoubleCard.CardName,
            DeleteCard.CardName
        };

        internal static bool HasAbility(Player player)
        {
            return player != null && player.data != null && player.data.currentCards != null &&
                   player.data.currentCards.Any(c => c != null && c.cardName != null && AbilityCardNames.Contains(c.cardName));
        }
    }

    // ---------------------------------------------------------------
    // Black-and-white inverted visual during the stop: a compiled
    // negative-monochrome image-effect shader ships inside the DLL as raw
    // bundle bytes (emitted by TimeStopBundleBuilder). Apply attaches a tiny
    // OnRenderImage post pass to every active camera — nothing on the game's
    // own camera components is mutated — and Restore removes the passes.
    // (The earlier AmplifyColor-LUT experiment is gone: playtest proved the
    // game camera carries no live AmplifyColorEffect to swap.)
    // ---------------------------------------------------------------
    internal static class TimeStopVisual
    {
        internal static Material Material;

        internal static void LoadAssets()
        {
            if (Material != null)
            {
                return;
            }
            var bundle = AssetBundle.LoadFromMemory(GeneratedTimeStopAssets.BundleBytes);
            if (bundle == null)
            {
                UnityEngine.Debug.LogError("[DEER] TimeStop visual: asset bundle failed to load from embedded bytes");
                return;
            }
            var shaders = bundle.LoadAllAssets<Shader>();
            if (shaders.Length == 0)
            {
                UnityEngine.Debug.LogError("[DEER] TimeStop visual: no shader inside the embedded bundle");
                return;
            }
            Material = new Material(shaders[0]);
            UnityEngine.Debug.Log($"[DEER] TimeStop visual: loaded inverted-monochrome shader '{shaders[0].name}' from embedded bundle ({GeneratedTimeStopAssets.BundleBytes.Length} bytes)");
        }

        internal static void Apply()
        {
            if (Material == null)
            {
                UnityEngine.Debug.LogWarning("[DEER] TimeStop visual: material missing — world will freeze WITHOUT inversion (check earlier [DEER] TimeStop visual logs)");
                return;
            }
            int newlyAttached = 0;
            foreach (var cam in Camera.allCameras)
            {
                if (cam.GetComponent<TimeStopCameraFX>() == null)
                {
                    cam.gameObject.AddComponent<TimeStopCameraFX>();
                    newlyAttached++;
                }
            }
            UnityEngine.Debug.Log($"[DEER] TimeStop visual: inverted-monochrome post pass on {Camera.allCameras.Length} camera(s) ({newlyAttached} newly attached)");
        }

        internal static void Restore()
        {
            foreach (var fx in UnityEngine.Object.FindObjectsOfType<TimeStopCameraFX>())
            {
                UnityEngine.Object.Destroy(fx);
            }
        }
    }

    // The per-camera post pass. Attached only while the stop is active.
    internal class TimeStopCameraFX : MonoBehaviour
    {
        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            var mat = TimeStopVisual.Material;
            if (mat == null)
            {
                Graphics.Blit(source, destination);
                return;
            }
            Graphics.Blit(source, destination, mat);
        }
    }

    // ---------------------------------------------------------------
    // Global driver (DontDestroyOnLoad): unscaled timeout + the SINGLE
    // keypress resolver for cast / enter / fall-out for every local player
    // (split-screen safe, Meteor recipe) + per-player statue enforcement.
    // ---------------------------------------------------------------
    internal class TimeStopDriver : MonoBehaviour
    {
        private void Update()
        {
            // Casting happens when inactive; enter/fall-out happen while the
            // stop runs — one key resolves per current state (TryHandle).
            PollKeys();

            if (!TimeStopState.IsActive)
            {
                return;
            }

            // Auto-end: local unscaled window (identical duration ⇒ consistent).
            // Offline escape menu must not consume the frozen time (vanilla
            // pauses the world there too via timeScale).
            if (!(PhotonNetwork.OfflineMode && EscapeMenuHandler.isEscMenu) && Time.unscaledTime >= TimeStopState.EndTime)
            {
                TimeStopState.End();
                return;
            }

            EnforceStatues();
        }

        private void LateUpdate()
        {
            // Hard failsafe: never keep the world frozen outside a battle.
            if (TimeStopState.IsActive && GameManager.instance != null && !GameManager.instance.battleOngoing)
            {
                TimeStopState.End();
            }
        }

        private void PollKeys()
        {
            if (PlayerManager.instance == null || GameManager.instance == null)
            {
                return; // menus / scene transitions — no players to act for
            }
            var local = GatherLocalPlayers();
            int count = local.Count;
            for (int idx = 0; idx < count; idx++)
            {
                KeyCode key;
                if (count == 1)
                {
                    key = TimeStopCard.KeyP1;
                }
                else
                {
                    key = idx == 0 ? TimeStopCard.KeyP1 : TimeStopCard.KeyP2;
                }
                if (!Input.GetKeyDown(key))
                {
                    continue;
                }
                TryHandle(local[idx]);
            }
        }

        private void TryHandle(Player p)
        {
            if (p == null || p.data == null || p.data.dead || !p.data.isPlaying || !GameManager.instance.battleOngoing)
            {
                return;
            }

            if (!TimeStopState.IsActive)
            {
                if (!TimeStopState.HoldsTimeStop(p))
                {
                    return;
                }
                UnityEngine.Debug.Log($"[DEER] TimeStop cast by {p.data.name}");
                TimeStopState.BroadcastStart(p.playerID, TimeStopCard.StopDuration);
                OneShotAbility.Consume(p, TimeStopCard.CardName);
                return;
            }

            if (TimeStopState.IsAgent(p.playerID))
            {
                UnityEngine.Debug.Log($"[DEER] TimeStop fall-out {p.data.name}");
                TimeStopState.BroadcastToggle(p.playerID, false);
            }
            else if (TimeStopState.HasAbility(p))
            {
                UnityEngine.Debug.Log($"[DEER] TimeStop enter {p.data.name}");
                TimeStopState.BroadcastToggle(p.playerID, true);
            }
        }

        private void EnforceStatues()
        {
            foreach (var data in UnityEngine.Object.FindObjectsOfType<CharacterData>())
            {
                if (data == null || data.player == null || data.dead)
                {
                    continue;
                }
                if (TimeStopState.IsAgent(data.player.playerID))
                {
                    continue;
                }
                if (data.input != null)
                {
                    data.input.stunnedInput = true;
                }
            }
        }

        private static List<Player> GatherLocalPlayers()
        {
            var list = new List<Player>();
            foreach (var p in PlayerManager.instance.players)
            {
                if (p != null && p.data != null && p.data.view != null && p.data.view.IsMine)
                {
                    list.Add(p);
                }
            }
            return list;
        }
    }

    // ---------------------------------------------------------------
    // Per-holder component: the AbilityHUD icon (Meteor recipe). All key
    // logic lives in the single global driver; this only draws the icon.
    // ---------------------------------------------------------------
    public class TimeStopEffect : MonoBehaviour
    {
        private Player player;

        private KeyCode triggerKey = TimeStopCard.KeyP1;

        private static Texture2D hudIconTex;

        public void SetPlayer(Player player)
        {
            this.player = player;
        }

        private void Awake()
        {
            if (player == null)
            {
                player = GetComponent<Player>();
            }
            EnsureHudTexture();
            AbilityHUD.Register(this, HudVisible, HudDraw);
        }

        private void OnDestroy()
        {
            AbilityHUD.Unregister(this);
        }

        private void Start()
        {
            if (player == null)
            {
                player = GetComponent<Player>();
            }
            ResolveKeys();
            UnityEngine.Debug.Log($"[DEER] TimeStopEffect started on player {(player != null && player.data != null ? player.data.name : "?")}");
        }

        // Meteor recipe: sole local or local index 0 → U; true split-screen
        // (2+ IsMine players) local index 1 → I.
        private void ResolveKeys()
        {
            triggerKey = TimeStopCard.KeyP1;
            if (player == null || player.data == null)
            {
                return;
            }
            int localIndex = 0;
            int localCount = 0;
            foreach (var p in PlayerManager.instance.players)
            {
                if (p == null || p.data == null)
                {
                    continue;
                }
                if (p.data.view.IsMine)
                {
                    if (p == player)
                    {
                        localIndex = localCount;
                    }
                    localCount++;
                }
            }
            if (localCount > 1 && localIndex == 1)
            {
                triggerKey = TimeStopCard.KeyP2;
            }
        }

        private static void EnsureHudTexture()
        {
            if (hudIconTex == null)
            {
                // filled disc with a contrasting inner notch — same shape as
                // every other ability icon, all drawn from AbilityHUD.MakeCircleTexture
                hudIconTex = AbilityHUD.MakeCircleTexture(128, 0, 56);
            }
        }

        private bool HudVisible()
        {
            return player != null && player.data != null && player.data.view != null &&
                   player.data.view.IsMine && player.data.isPlaying;
        }

        private void HudDraw(Rect area)
        {
            Color ready;
            if (TimeStopState.IsActive)
            {
                ready = TimeStopState.IsAgent(player.playerID)
                    ? new Color(0.78f, 0.95f, 1f)          // inside the stopped world
                    : new Color(0.55f, 0.75f, 0.85f);      // fell out; can re-enter
            }
            else
            {
                ready = new Color(0.92f, 0.97f, 1f);        // ready to cast
            }
            AbilityHUD.DrawCircle(
                area,
                hudIconTex,
                ready,
                new Color(0.30f, 0.30f, 0.34f),             // spent: dark gray
                true,
                triggerKey.ToString()
            );
        }
    }

    // ---------------------------------------------------------------
    // Harmony patches. Hijack idiom: stash the pinned game static, set the
    // per-call value the vanilla body should read, restore right after —
    // original bodies run UNMODIFIED. Targets never nest (all are top-level
    // Update/FixedUpdate calls), so one stash slot per target is exact.
    // ---------------------------------------------------------------
    [HarmonyPatch]
    internal static class TimeStopPatches
    {
        // --- 1. The global pin: everything on the game clock stops ---
        [HarmonyPatch(typeof(TimeHandler), "Update")]
        [HarmonyPrefix]
        private static bool TimeHandlerUpdate(TimeHandler __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            TimeHandler.timeScale = 0f;
            TimeHandler.deltaTime = 0f;
            TimeHandler.fixedDeltaTime = 0f;
            return false;
        }

        // --- 2. Player sim: statues parked, agents run at agent time ---
        [HarmonyPatch(typeof(PlayerVelocity), "FixedUpdate")]
        [HarmonyPrefix]
        private static bool PlayerVelocityFixedUpdate(PlayerVelocity __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            var data = __instance.GetComponent<CharacterData>();
            if (data == null || data.player == null)
            {
                return true;
            }
            if (TimeStopState.IsAgent(data.player.playerID) && data.view.IsMine)
            {
                // Agent owner: the vanilla body runs at agent time, so jumps,
                // knockback and walking keep integrating inside the stop.
                pendingRestore = TimeHandler.timeScale;
                TimeHandler.timeScale = 1f;
                return true;
            }
            // Statue (every client): skip the integration entirely, but DON'T
            // zero the stored velocity — forces landing on a frozen statue
            // (shockwaves, knockback) QUEUE in it; nothing integrates and
            // nothing decays during the stop (every decay path scales by the
            // pinned timeScale), so on resume the vanilla body integrates the
            // whole queue at once and the statue launches like causality
            // catching up. Only the rigidbody channel is parked here, since
            // Physics2D itself keeps running in real time.
            var rig = __instance.GetComponent<Rigidbody2D>();
            if (rig != null)
            {
                rig.velocity = Vector2.zero;
            }
            return false;
        }

        private static float pendingRestore = float.NaN;
        // separate stash slots — the three FixedUpdate hijacks run
        // sequentially within one physics frame
        private static float playerMovementSaved = float.NaN;
        private static float gravitySaved = float.NaN;

        [HarmonyPatch(typeof(PlayerVelocity), "FixedUpdate")]
        [HarmonyPostfix]
        private static void PlayerVelocityFixedUpdatePostfix(PlayerVelocity __instance)
        {
            if (!float.IsNaN(pendingRestore))
            {
                TimeHandler.timeScale = pendingRestore;
                pendingRestore = float.NaN;
            }
        }

        // --- 3. Walk force: PlayerMovement scales by timeScale too ---
        [HarmonyPatch(typeof(PlayerMovement), "FixedUpdate")]
        [HarmonyPrefix]
        private static bool PlayerMovementFixedUpdate(PlayerMovement __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            var data = __instance.GetComponent<CharacterData>();
            if (data == null || data.player == null)
            {
                return true;
            }
            if (TimeStopState.IsAgent(data.player.playerID) && data.view.IsMine)
            {
                playerMovementSaved = TimeHandler.timeScale;
                TimeHandler.timeScale = 1f;
            }
            return true;
        }

        [HarmonyPatch(typeof(PlayerMovement), "FixedUpdate")]
        [HarmonyPostfix]
        private static void PlayerMovementFixedUpdatePostfix(PlayerMovement __instance)
        {
            if (!float.IsNaN(playerMovementSaved))
            {
                TimeHandler.timeScale = playerMovementSaved;
                playerMovementSaved = float.NaN;
            }
        }

        // --- 4. Gravity-stat ramp for agents at agent time ---
        [HarmonyPatch(typeof(Gravity), "FixedUpdate")]
        [HarmonyPrefix]
        private static bool GravityFixedUpdate(Gravity __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            var data = __instance.GetComponent<CharacterData>();
            if (data == null || data.player == null)
            {
                return true;
            }
            if (TimeStopState.IsAgent(data.player.playerID) && data.view.IsMine)
            {
                gravitySaved = TimeHandler.timeScale;
                TimeHandler.timeScale = 1f;
            }
            return true;
        }

        [HarmonyPatch(typeof(Gravity), "FixedUpdate")]
        [HarmonyPostfix]
        private static void GravityFixedUpdatePostfix(Gravity __instance)
        {
            if (!float.IsNaN(gravitySaved))
            {
                TimeHandler.timeScale = gravitySaved;
                gravitySaved = float.NaN;
            }
        }

        // --- 5. Jump timers + hold-jump feather for agents ---
        [HarmonyPatch(typeof(CharacterData), "Update")]
        [HarmonyPrefix]
        private static bool CharacterDataUpdate(CharacterData __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            if (TimeStopState.IsAgent(__instance.player.playerID) && __instance.view.IsMine)
            {
                characterDataSavedDt = TimeHandler.deltaTime;
                TimeHandler.deltaTime = Time.deltaTime;
            }
            return true;
        }

        private static float characterDataSavedDt = float.NaN;

        [HarmonyPatch(typeof(CharacterData), "Update")]
        [HarmonyPostfix]
        private static void CharacterDataUpdatePostfix(CharacterData __instance)
        {
            if (!float.IsNaN(characterDataSavedDt))
            {
                TimeHandler.deltaTime = characterDataSavedDt;
                characterDataSavedDt = float.NaN;
            }
        }

        [HarmonyPatch(typeof(CharacterData), "FixedUpdate")]
        [HarmonyPrefix]
        private static bool CharacterDataFixedUpdate(CharacterData __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            if (TimeStopState.IsAgent(__instance.player.playerID) && __instance.view.IsMine)
            {
                characterDataSavedFdt = TimeHandler.fixedDeltaTime;
                TimeHandler.fixedDeltaTime = Time.fixedDeltaTime;
            }
            return true;
        }

        private static float characterDataSavedFdt = float.NaN;

        [HarmonyPatch(typeof(CharacterData), "FixedUpdate")]
        [HarmonyPostfix]
        private static void CharacterDataFixedUpdatePostfix(CharacterData __instance)
        {
            if (!float.IsNaN(characterDataSavedFdt))
            {
                TimeHandler.fixedDeltaTime = characterDataSavedFdt;
                characterDataSavedFdt = float.NaN;
            }
        }

        [HarmonyPatch(typeof(PlayerJump), "Update")]
        [HarmonyPrefix]
        private static bool PlayerJumpUpdate(PlayerJump __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            var data = __instance.GetComponent<CharacterData>();
            if (data == null || data.player == null || !TimeStopState.IsAgent(data.player.playerID) || !data.view.IsMine)
            {
                return true;
            }
            playerJumpSavedDt = TimeHandler.deltaTime;
            TimeHandler.deltaTime = Time.deltaTime;
            return true;
        }

        private static float playerJumpSavedDt = float.NaN;

        [HarmonyPatch(typeof(PlayerJump), "Update")]
        [HarmonyPostfix]
        private static void PlayerJumpUpdatePostfix(PlayerJump __instance)
        {
            if (!float.IsNaN(playerJumpSavedDt))
            {
                TimeHandler.deltaTime = playerJumpSavedDt;
                playerJumpSavedDt = float.NaN;
            }
        }

        // --- 6. Gun cooldown + reload at agent time ---
        [HarmonyPatch(typeof(Gun), "Update")]
        [HarmonyPrefix]
        private static bool GunUpdate(Gun __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            var player = __instance.player;
            if (player == null || !TimeStopState.IsAgent(player.playerID))
            {
                return true;
            }
            if (!player.data.view.IsMine)
            {
                return true;
            }
            gunSavedDt = TimeHandler.deltaTime;
            TimeHandler.deltaTime = Time.deltaTime;
            return true;
        }

        private static float gunSavedDt = float.NaN;

        [HarmonyPatch(typeof(Gun), "Update")]
        [HarmonyPostfix]
        private static void GunUpdatePostfix(Gun __instance)
        {
            if (!float.IsNaN(gunSavedDt))
            {
                TimeHandler.deltaTime = gunSavedDt;
                gunSavedDt = float.NaN;
            }
        }

        [HarmonyPatch(typeof(GunAmmo), "Update")]
        [HarmonyPrefix]
        private static bool GunAmmoUpdate(GunAmmo __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            var gun = __instance.GetComponentInParent<Gun>();
            var player = gun != null ? gun.player : null;
            if (player == null || !TimeStopState.IsAgent(player.playerID) || !player.data.view.IsMine)
            {
                return true;
            }
            gunAmmoSavedDt = TimeHandler.deltaTime;
            TimeHandler.deltaTime = Time.deltaTime;
            return true;
        }

        private static float gunAmmoSavedDt = float.NaN;

        [HarmonyPatch(typeof(GunAmmo), "Update")]
        [HarmonyPostfix]
        private static void GunAmmoUpdatePostfix(GunAmmo __instance)
        {
            if (!float.IsNaN(gunAmmoSavedDt))
            {
                TimeHandler.deltaTime = gunAmmoSavedDt;
                gunAmmoSavedDt = float.NaN;
            }
        }

        // --- 7. Bullets: EVERY bullet hangs, no matter whose it is ---
        [HarmonyPatch(typeof(MoveTransform), "Update")]
        [HarmonyPrefix]
        private static bool MoveTransformUpdate(MoveTransform __instance)
        {
            // While the stop is active every projectile sits exactly where it
            // is — including shots fired during the stop (they spawn at the
            // muzzle with full stored velocity and hang there). Nothing moves,
            // so no flight-segment raycasts / wall hits / player hits either.
            // The caught volley all launches on resume via normal integration.
            return !TimeStopState.IsActive;
        }

        // --- 8. Hard gates: frozen players cannot shoot or block, whatever
        //        writes their inputs (vanilla PlayerAI writes data.input
        //        directly and bypasses the stunnedInput gate; bundled bot
        //        brains do the same, by design) ---
        [HarmonyPatch(typeof(Gun), nameof(Gun.Attack))]
        [HarmonyPrefix]
        private static bool GunAttackGate(Gun __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            var player = __instance.player;
            if (player == null || player.data == null)
            {
                return true;
            }
            return TimeStopState.IsAgent(player.playerID); // frozen = block fire
        }

        [HarmonyPatch(typeof(Block), "RPCA_DoBlock")]
        [HarmonyPrefix]
        private static bool BlockGate(Block __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            var data = __instance.GetComponent<CharacterData>();
            if (data == null || data.player == null)
            {
                return true;
            }
            return TimeStopState.IsAgent(data.player.playerID); // frozen = block
        }

        // --- 9. Parked bullets are HARMLESS: nothing can be hit while the
        //        stop runs. RayCastTrail.Update is the ONLY projectile impact
        //        path (it owns every rayHit.Hit call — walls and players
        //        alike). With a bullet parked, its cast segment has zero
        //        length, but a zero-length CircleCastAll STILL returns
        //        anything overlapping the bullet's circle — so the parked
        //        muzzle bullet hits whoever it sits on (the root-skip guard
        //        only skips the projectile's own child colliders, never the
        //        shooter). Skip the whole update while frozen; since the
        //        transform never moves, lastPos == position still holds and
        //        the first vanilla update after resume sweeps the full frozen
        //        flight segment normally — vanilla hits, zero tunneling, no
        //        park-time damage.
        [HarmonyPatch(typeof(RayCastTrail), "Update")]
        [HarmonyPrefix]
        private static bool RayCastTrailUpdateGate(RayCastTrail __instance)
        {
            // Owner-agnostic: a parked bullet can damage NO ONE — not its
            // shooter, not a statue, not a wall — until time resumes.
            return !TimeStopState.IsActive;
        }

        // --- 10. The MOVING agent obeys vanilla out-of-bounds while stopped:
        //         fall off the map edge / bounce / die just like vanilla
        //         (playtest bug: the caster fell through the bottom forever).
        //         OutOfBoundsHandler.LateUpdate drives the whole edge system
        //         (bounce force, shield wall, the fall-out 51-damage launch)
        //         and its punish accumulator (`counter += TimeHandler.
        //         deltaTime`) is frozen by the pin, so the agent crossing the
        //         boundary never got punished and fell through. Hijack ONLY
        //         the local agent's instance: tick it in real time. Statues
        //         and remote copies keep the frozen vanilla body (their
        //         counter stays 0 — a statue at the edge just hovers).
        //         NOTE: OutOfBoundsHandler.Start does SetParent(null), so it
        //         is NOT a child of the player — GetComponent<CharacterData>
        //         would fail; instead Harmony injects the private `data`
        //         field via the matching parameter name.
        [HarmonyPatch(typeof(OutOfBoundsHandler), "LateUpdate")]
        [HarmonyPrefix]
        private static bool OutOfBoundsLateUpdate(OutOfBoundsHandler __instance, CharacterData ___data)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            var player = ___data != null ? ___data.player : null;
            if (player == null || !TimeStopState.IsAgent(player.playerID) || !___data.view.IsMine)
            {
                return true;
            }
            oobSavedDt = TimeHandler.deltaTime;
            TimeHandler.deltaTime = Time.deltaTime; // punish accumulator ticks in real time
            return true;
        }

        private static float oobSavedDt = float.NaN;

        [HarmonyPatch(typeof(OutOfBoundsHandler), "LateUpdate")]
        [HarmonyPostfix]
        private static void OutOfBoundsLateUpdatePostfix(OutOfBoundsHandler __instance)
        {
            if (!float.IsNaN(oobSavedDt))
            {
                TimeHandler.deltaTime = oobSavedDt;
                oobSavedDt = float.NaN;
            }
        }
    }
}
