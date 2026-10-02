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

        /// <summary>
        /// Bots (Sovereign/Simulacrum) share their master's playerID, so a
        /// plain IsAgent(playerID) check misclassifies the bot as its master.
        /// Every per-instance simulation patch here consults this instead:
        /// a bot is NEVER an agent — it is a frozen statue too, even when its
        /// master walks around inside the stopped world. Registry-based, not
        /// team-based, so this freezes all bots, friend or foe.
        /// </summary>
        internal static bool IsSimAgent(CharacterData data)
        {
            return data != null && !IsBotData(data) && data.player != null && IsAgent(data.player.playerID);
        }

        internal static bool IsBotData(CharacterData data)
        {
            return SovereignBot.IsBotData(data) || SimulacrumBot.IsBotData(data);
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

        internal static void Begin(int casterPlayerID, float duration, int soundIndex = -1)
        {
            active = true;
            agents.Clear();
            agents.Add(casterPlayerID);
            EndTime = Time.unscaledTime + Mathf.Clamp(duration, 0.5f, TimeStopCard.MaxStopDuration);
            TimeStopVisual.Apply();
            TimeStopSounds.PlayStop(soundIndex);
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
            // Cut the frozen-world sting — time restarting silences it.
            TimeStopSounds.OnTimeResume();
            // Forces a frozen bot failed to shed (its statue branch parks the
            // Rigidbody2D channel, not PlayerVelocity.velocity) would otherwise
            // integrate as one lump on resume and punt the bot. Clear them.
            SovereignBot.SettleBotVelocities();
            SimulacrumBot.SettleBotVelocities();
            DrainResumeQueue();
        }

        // -----------------------------------------------------------
        // Resume queue — effects whose whole machinery would run in
        // real-time Unity clock (coroutines/FixedUpdate are NOT frozen by
        // the pin) register here instead of firing mid-stop. Drained by
        // End() once time has resumed: `active` is already false, so every
        // gate reads IsActive = false and lets the vanilla bodies run.
        // -----------------------------------------------------------
        internal static void QueueOnResume(string tag, UnityEngine.Object owner, System.Action run)
        {
            if (run != null)
            {
                resumeQueue.Add(new QueuedEffect { tag = tag, owner = owner, run = run });
            }
        }

        private sealed class QueuedEffect
        {
            internal string tag;
            internal UnityEngine.Object owner; // gone subjects are skipped (Unity fake-null)
            internal System.Action run;
        }

        private static readonly List<QueuedEffect> resumeQueue = new List<QueuedEffect>();

        private static void DrainResumeQueue()
        {
            if (resumeQueue.Count == 0)
            {
                return;
            }
            if (GameManager.instance == null || !GameManager.instance.battleOngoing)
            {
                UnityEngine.Debug.Log($"[DEER] TimeStop: discarding {resumeQueue.Count} queued effect(s), round over: [{string.Join(", ", resumeQueue.Select(q => q.tag).ToArray())}]");
                resumeQueue.Clear();
                return;
            }
            UnityEngine.Debug.Log($"[DEER] TimeStop: releasing deferred effect(s) on resume: [{string.Join(", ", resumeQueue.Select(q => q.tag).ToArray())}]");
            for (int i = 0; i < resumeQueue.Count; i++)
            {
                var queued = resumeQueue[i];
                if (queued.owner != null)
                {
                    try
                    {
                        queued.run();
                    }
                    catch (System.Exception ex)
                    {
                        // one bad entry must never eat the rest (RunInit doctrine)
                        UnityEngine.Debug.LogError($"[DEER] TimeStop: queued '{queued.tag}' FAILED — {ex}");
                    }
                }
                else
                {
                    UnityEngine.Debug.Log($"[DEER] TimeStop: queued '{queued.tag}' skipped, its object is gone");
                }
            }
            resumeQueue.Clear();
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
            // The CASTER picks the sting so every client hears the same one —
            // each client's UnityEngine.Random would diverge.
            int soundIndex = UnityEngine.Random.Range(0, TimeStopSounds.StopCount);
            NetworkingManager.RPC(typeof(TimeStopState), nameof(RPCA_TimeStopStart), casterPlayerID, duration, soundIndex);
        }

        internal static void BroadcastToggle(int playerID, bool join)
        {
            NetworkingManager.RPC(typeof(TimeStopState), nameof(RPCA_TimeStopToggleAgent), playerID, join);
        }

        [UnboundLib.Networking.UnboundRPC]
        public static void RPCA_TimeStopStart(int casterPlayerID, float duration, int soundIndex)
        {
            Begin(casterPlayerID, duration, soundIndex);
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
            BoplTimeStopCard.CardName,
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
    // Embedded-asset access: the 'deerootcards' bundle (shader + the four
    // stop/resume stings) ships base64 inside the DLL. One shared load —
    // a bundle reconstructed twice would waste a second copy of the bytes.
    // ---------------------------------------------------------------
    internal static class TimeStopAssets
    {
        private static AssetBundle bundle;
        private static bool failed;

        internal static AssetBundle GetBundle()
        {
            if (bundle != null)
            {
                return bundle;
            }
            if (failed)
            {
                return null;
            }
            bundle = AssetBundle.LoadFromMemory(GeneratedTimeStopAssets.BundleBytes);
            if (bundle == null)
            {
                failed = true;
                UnityEngine.Debug.LogError("[DEER] TimeStop assets: asset bundle failed to load from embedded bytes");
            }
            return bundle;
        }
    }

    // ---------------------------------------------------------------
    // Stop/resume stings (OGG clips inside the embedded bundle). Stop:
    // caster picks one of three locally and ships the index through the
    // start RPC so every client hears the SAME sting. Resume: End() runs
    // on every client already — local playback, no extra RPC.
    //
    // Audio plays through a plain AudioSource at pitch 1 — Unity does NOT
    // pitch/silence audio by timeScale, which is exactly what we want: the
    // jingle rings normally over the frozen world. A still-playing stop
    // sting is CUT when time resumes (End -> OnTimeResume) or when a new
    // stop chains before the old one ends.
    // ---------------------------------------------------------------
    internal static class TimeStopSounds
    {
        internal const int StopCount = 3;

        private static readonly AudioClip[] stopClips = new AudioClip[StopCount];
        private static AudioClip resumeClip;
        private static bool loaded;
        private static AudioSource stopSource;
        private static AudioSource resumeSource;

        internal static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }
            loaded = true;
            var bundle = TimeStopAssets.GetBundle();
            if (bundle == null)
            {
                return;
            }
            var clips = bundle.LoadAllAssets<AudioClip>();
            foreach (var clip in clips)
            {
                switch (clip.name)
                {
                    case "timestop1":
                        stopClips[0] = clip;
                        break;
                    case "timestop2":
                        stopClips[1] = clip;
                        break;
                    case "timestop3":
                        stopClips[2] = clip;
                        break;
                    case "time_resume":
                        resumeClip = clip;
                        break;
                    default:
                        UnityEngine.Debug.LogWarning($"[DEER] TimeStop sounds: unexpected clip '{clip.name}' in bundle");
                        break;
                }
            }
            for (int i = 0; i < StopCount; i++)
            {
                if (stopClips[i] == null)
                {
                    UnityEngine.Debug.LogWarning($"[DEER] TimeStop sounds: timestop{i + 1} missing from embedded bundle — casting will be silent");
                }
            }
            if (resumeClip == null)
            {
                UnityEngine.Debug.LogWarning("[DEER] TimeStop sounds: time_resume missing from embedded bundle — resumes will be silent");
            }
            UnityEngine.Debug.Log($"[DEER] TimeStop sounds: loaded {clips.Length} clip(s) from embedded bundle");
        }

        /// <summary>2D one-shot: survives scene loads until the clip is done.</summary>
        private static AudioSource Play2D(AudioClip clip)
        {
            var go = new GameObject("DeerootTimeStopSound");
            Object.DontDestroyOnLoad(go);
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = 0f; // 2D, no listener-side falloff/pan
            src.playOnAwake = false;
            src.pitch = 1f; // Unity won't pitch audio by timeScale, but be explicit
            src.clip = clip;
            src.Play();
            UnityEngine.Object.Destroy(go, clip.length + 0.1f);
            return src;
        }

        internal static void PlayStop(int soundIndex)
        {
            EnsureLoaded();
            if (stopClips == null)
            {
                return;
            }
            if (soundIndex < 0 || soundIndex >= StopCount || stopClips[soundIndex] == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] TimeStop sounds: bad/missing stop sting index {soundIndex} — silent cast");
                return;
            }
            // Recast before the old sting finished: cut it so the new cast rings clean.
            if (stopSource != null)
            {
                UnityEngine.Object.Destroy(stopSource.gameObject);
                stopSource = null;
            }
            stopSource = Play2D(stopClips[soundIndex]);
        }

        /// <summary>End() hit: cut the frozen-world sting, ring the resume sting.</summary>
        internal static void OnTimeResume()
        {
            if (stopSource != null)
            {
                UnityEngine.Object.Destroy(stopSource.gameObject);
                stopSource = null;
            }
            EnsureLoaded();
            if (resumeClip == null)
            {
                return;
            }
            // End() is idempotent-guarded upstream, but re-triggering from a
            // rapid re-cast must still cut any lingering resume sting's source
            // without overlapping: recreate fresh each time.
            if (resumeSource != null)
            {
                UnityEngine.Object.Destroy(resumeSource.gameObject);
                resumeSource = null;
            }
            resumeSource = Play2D(resumeClip);
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
            var bundle = TimeStopAssets.GetBundle();
            if (bundle == null)
            {
                return; // GetBundle already logged the failure
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
            if (TimeStopState.IsSimAgent(data) && data.view.IsMine)
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
            if (TimeStopState.IsSimAgent(data) && data.view.IsMine)
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
            if (TimeStopState.IsSimAgent(data) && data.view.IsMine)
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
            if (TimeStopState.IsSimAgent(__instance) && __instance.view.IsMine)
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
            if (TimeStopState.IsSimAgent(__instance) && __instance.view.IsMine)
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
            if (data == null || data.player == null || TimeStopState.IsSimAgent(data) == false || !data.view.IsMine)
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
            if (player == null || TimeStopState.IsSimAgent(player.data) == false || !player.data.view.IsMine)
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
            if (player == null || TimeStopState.IsSimAgent(player.data) == false || !player.data.view.IsMine)
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
            // Bots share their master's playerID — a bot is never an agent,
            // even when its master walks the stopped world.
            return TimeStopState.IsSimAgent(player.data); // frozen = block fire
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
            return TimeStopState.IsSimAgent(data); // frozen = block
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
            if (player == null || TimeStopState.IsSimAgent(___data) == false || !___data.view.IsMine)
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

        // --- 11. Block-spawned effect objects hold until time resumes.
        //        Vanilla recipe: the card injects an A_* carrier as a CHILD of
        //        the player (CharacterStatModifiers.AddObjectToPlayer); on
        //        block its SpawnObjects.Spawn() instantiates the E_* effect
        //        object at the carrier position. WITHOUT this gate the effect
        //        objects spawned mid-stop run their machinery immediately,
        //        because their drivers (DelayEvent = WaitForSeconds
        //        coroutines, FixedUpdate) run on the UNITY clock, which our
        //        TimeHandler pin does NOT freeze (TimeHandler.Update normally
        //        re-writes Unity Time.timeScale = 1 every frame) — Static
        //        Field pulsed and dealt damage through the whole stop and
        //        Supernova's stage chain fired mid-stop (playtest bugs).
        //        Policy: default-DEFER every carrier spawn until resume (the
        //        effect then plays out in one vanilla pass), EXCEPT the
        //        implode ball — the ball IS the epicenter the pull targets
        //        (spec: "player position at the time of blocking"), so it
        //        must exist at the block spot, parked, until resume.
        //        GOTCHA: the carrier is a child of the player and keeps
        //        FOLLOWING them during the stop, so queueing the bare Spawn()
        //        delegate materialized the field at the caster's RESUME
        //        position (playtest bug: Static Field on the player). The
        //        gate therefore captures the carrier's position/rotation at
        //        block time — that IS "where I blocked" — and releases via
        //        SpawnDeferredAt below, a faithful mirror of vanilla
        //        Spawn()/ConfigureObject().
        [HarmonyPatch(typeof(SpawnObjects), nameof(SpawnObjects.Spawn))]
        [HarmonyPrefix]
        private static bool SpawnObjectsSpawnGate(SpawnObjects __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            var objects = __instance.objectToSpawn;
            string prefabName = objects != null && objects.Length > 0 && objects[0] != null ? objects[0].name : null;
            if (prefabName != null && prefabName.StartsWith("E_Implode", System.StringComparison.OrdinalIgnoreCase))
            {
                // Parked visuals at the block-time epicenter; its detonation
                // is gated separately in section 12.
                return true;
            }
            Vector3 blockedPos = __instance.transform.position;
            Quaternion blockedRot = __instance.spawnRot == SpawnObjects.SpawnRot.TransformRotation
                ? __instance.transform.rotation
                : Quaternion.identity;
            TimeStopState.QueueOnResume(
                "spawn " + (prefabName ?? __instance.name) + " at blocked spot",
                __instance,
                () => SpawnDeferredAt(__instance, blockedPos, blockedRot));
            UnityEngine.Debug.Log($"[DEER] TimeStop: deferred block-spawn '{prefabName ?? __instance.name}' until time resumes (anchored at blocked spot {blockedPos})");
            return false;
        }

        // Mirror of vanilla SpawnObjects.Spawn() + ConfigureObject(), pinned
        // to the block-time position instead of the carrier's live transform.
        // State read at drain time is deliberately limited to what cannot
        // change during a stop (root player, AttackLevel, scales, flags) —
        // only the carrier's transform moves while deferred.
        private static void SpawnDeferredAt(SpawnObjects carrier, Vector3 blockedPos, Quaternion blockedRot)
        {
            var objects = carrier.objectToSpawn;
            if (objects != null)
            {
                for (int i = 0; i < objects.Length; i++)
                {
                    if (objects[i] == null)
                    {
                        continue;
                    }
                    GameObject go = Object.Instantiate(objects[i], blockedPos, blockedRot);
                    ConfigureDeferredObject(carrier, go);
                    carrier.mostRecentlySpawnedObject = go;
                }
            }
            if (carrier.destroyObject)
            {
                Object.Destroy(carrier.gameObject);
            }
            if (carrier.destroyRoot)
            {
                Object.Destroy(carrier.transform.root.gameObject);
            }
        }

        private static void ConfigureDeferredObject(SpawnObjects carrier, GameObject go)
        {
            var spawned = go.GetComponent<SpawnedAttack>();
            if (spawned == null)
            {
                spawned = go.AddComponent<SpawnedAttack>();
            }
            spawned.spawner = carrier.transform.root.GetComponent<Player>();
            if (spawned.spawner == null)
            {
                var parentSpawned = carrier.GetComponentInParent<SpawnedAttack>();
                if (parentSpawned != null)
                {
                    parentSpawned.CopySpawnedAttackTo(go);
                }
            }
            var level = carrier.GetComponentInParent<AttackLevel>();
            if (level != null)
            {
                spawned.attackLevel = level.attackLevel;
            }
            if (carrier.inheritScale)
            {
                go.transform.localScale *= carrier.transform.localScale.x;
            }
            // SpawnedAction subscribers (e.g. SetSpawnedParticleColor) provide
            // the skin tinting — skipping this would silently lose team colors.
            if (carrier.SpawnedAction != null)
            {
                carrier.SpawnedAction(go);
            }
        }

        // --- 12. The implode ball's DETONATION holds until resume. Its 0.1s
        //        DelayEvent fires mid-stop (Unity clock!), and vanilla would
        //        then hammer HealthHandler.TakeForce → PlayerVelocity.AddForce
        //        onto FROZEN players every physics frame: their FixedUpdate
        //        is skipped, so PlayerVelocity.velocity accumulates without
        //        limit, AND the sustained pull's loop (`for i < time; i +=
        //        TimeHandler.fixedDeltaTime`) never advances against the
        //        pinned 0 fixed step — at resume the first integration step
        //        launched victims toward/past the epicenter, the longer the
        //        stop the farther (the "sends opponents REALLY far" bug).
        //        With the detonation deferred, NO force exists during the
        //        stop; on release the queued Explode() runs vanilla in real
        //        time (impulse + drag-braked pull) and lands victims AT the
        //        parked ball. Scope: instances carrying Implosion — the
        //        implode family only; every other Explosion object is
        //        simply absent mid-stop now thanks to section 11.
        [HarmonyPatch(typeof(Explosion), nameof(Explosion.Explode))]
        [HarmonyPrefix]
        private static bool ExplosionExplodeGate(Explosion __instance)
        {
            if (!TimeStopState.IsActive)
            {
                return true;
            }
            if (__instance.GetComponent<Implosion>() == null)
            {
                return true;
            }
            TimeStopState.QueueOnResume("implode detonation", __instance, __instance.Explode);
            UnityEngine.Debug.Log("[DEER] TimeStop: implode ball parked — detonation deferred to resume");
            return false;
        }
    }
}
