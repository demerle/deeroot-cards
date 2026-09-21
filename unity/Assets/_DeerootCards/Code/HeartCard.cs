using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.GameModes;
using UnboundLib.Networking;
using UnboundLib.Extensions;
using ModdingUtils.Extensions;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Heart: pressing [H] throws the player's heart out — a small physical
    /// object that falls and lands like a player. One throw per round: once
    /// thrown it stays on the ground until the round ends (you cannot pick it
    /// back up). It is fragile (1 HP): being shot, OR hitting a wall/ceiling
    /// (the floor is safe), kills the OWNER instantly via the vanilla death
    /// path (correct death effect, Phoenix handling, kill credit).
    /// Later milestones: invincibility while away + health ticking.
    /// </summary>
    public class HeartCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            UnityEngine.Debug.Log("[DEER] HeartCard SetupCard");
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.gameObject.GetOrAddComponent<HeartEffect>();
            effect.SetPlayer(player);
            HeartEffect.RegisterRoundResetHooks();
            UnityEngine.Debug.Log($"[DEER] HeartCard added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.GetComponent<HeartEffect>();
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        protected override string GetTitle()
        {
            return "Heart";
        }

        protected override string GetDescription()
        {
            return "Throw your heart on the ground. It is fragile. Do not let anything happen to it. You cannot pick it back up.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Throw Heart",
                    amount = "H (once per round)",
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
            return CardThemeColor.CardThemeColorType.EvilPurple;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }

        // applies the MoveTransform.Update postfix once, at card registration
        private static bool harmonyApplied;
        public static void Init()
        {
            if (harmonyApplied) return;
            harmonyApplied = true;
            var harmony = new Harmony("com.deeroot.cards.heart");
            harmony.PatchAll(typeof(HeartEffect.BulletHeartPatch));
            harmony.PatchAll(typeof(HeartEffect.DeathHeartPatch));
            harmony.PatchAll(typeof(HeartEffect.IncomingDamageGate));
        }
    }

    /// <summary>
    /// Attached to the player. [H] throws the heart (owner client only).
    /// State lives in static registries so it is identical on every client
    /// after the RPCs settle — the same replication philosophy as Portals.
    /// </summary>
    public class HeartEffect : MonoBehaviour
    {
        private Player player;

        private enum HeartState { Carried, OnGround, Dead }

        // ---- shared state registry (replicated identically on every client) ----
        private static Dictionary<int, HeartState> states = new Dictionary<int, HeartState>();
        private static Dictionary<int, Vector3> heartPositions = new Dictionary<int, Vector3>();
        private static Dictionary<int, GameObject> heartObjects = new Dictionary<int, GameObject>();

        // ---- Milestone 2 registries (keyed by ownerPlayerID) ----
        // constant drain: every tick removes the SAME amount of hp, so the
        // owner dies exactly lifetime seconds after the throw (sampled once
        // per throw from maxHealth).
        private static Dictionary<int, float> drainPerTick = new Dictionary<int, float>();
        private static Dictionary<int, float> drainTimer = new Dictionary<int, float>();
        // originals, saved at throw and restored the moment the heart resolves
        private static Dictionary<int, float> savedMoveSpeed = new Dictionary<int, float>();
        private static Dictionary<int, float> savedRegen = new Dictionary<int, float>();
        private static Dictionary<int, float> savedStatsRegen = new Dictionary<int, float>();
        private static Dictionary<int, float> savedLifeSteal = new Dictionary<int, float>();
        private static Dictionary<int, bool> loggedGateBlock = new Dictionary<int, bool>();
        // true only while the drain itself applies its lethal finisher, so the
        // damage gate knows our own DoDamage call is allowed through
        internal static bool applyingHeartDrain;

        private const float drainTickInterval = 0.25f;
        private const float heartSlowMultiplier = 0.75f;

        private static Sprite heartSprite;

        // ---- per-effect-instance state ----
        private KeyCode keyThrow = KeyCode.H;

        // key table is per-CLIENT (same rule as portals): every local player uses
        // H, except a second local player on true split-screen gets a spare key.
        private void ResolveKeys()
        {
            keyThrow = KeyCode.H;
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
                keyThrow = KeyCode.G; // P2 on one-machine split-screen
            }
        }

        public void SetPlayer(Player player)
        {
            this.player = player;
        }

        private void Start()
        {
            if (player == null)
            {
                player = GetComponent<Player>();
            }
            ResolveKeys();
            UnityEngine.Debug.Log($"[DEER] HeartEffect started on player {player.data.name}");
        }

        public void OnDestroy()
        {
            AbilityHUD.Unregister(this);
            if (player != null)
            {
                RemoveHeart(player.playerID);
            }
        }

        private void Update()
        {
            if (player == null || player.data == null)
            {
                return;
            }

            // ability reset on owner death — runs on EVERY client (data.dead is
            // synced): any owner death (heart kill, other kill, point end,
            // Phoenix) clears the heart everywhere deterministically and returns
            // the throw to Carried, so sandbox testing and Phoenix just work.
            if (player.data.dead && GetState(player.playerID) != HeartState.Carried)
            {
                DebugHeartDeathReset(player);
                RemoveHeart(player.playerID);
                states[player.playerID] = HeartState.Carried;
            }

            // Milestone 2: the heart drain ticks on EVERY client (from the
            // replicated state registries), debiting hp identically everywhere
            // — same deterministic-replication philosophy as the rest of the
            // card. Only the heart-card owner's HeartEffect drives it, which
            // is enough: only heart-card owners can ever be OnGround.
            TickHeartDrain();

            if (player.data.view.IsMine)
            {
                SimulateOwner();
            }
        }

        // ---------------------------------------------------------------
        // Milestone 2 — heart drain
        //
        // MODEL (user spec): constant damage EVERY tick. Lifetime is a
        // function of maxHealth sampled at throw:
        //     L(h) = 20h / (h + 100)          (seconds)
        //     dps = hp / L(h) = (h + 100)/20  (constant every tick)
        // Default 100 hp -> exactly 10 hp/s, dead 10 s after the throw.
        // Huge health pools approach but NEVER exceed a 20 s lifetime —
        // stacking health cards can't make the heart last forever.
        // The tick amount is the same every tick (fixed 0.25 s ticks).
        // ---------------------------------------------------------------
        private static void TickHeartDrain()
        {
            if (!GameManager.instance.battleOngoing || TimeHandler.timeScale <= 0f)
            {
                return;
            }
            foreach (var kv in states)
            {
                if (kv.Value != HeartState.OnGround)
                {
                    continue;
                }
                int id = kv.Key;
                float tickDmg;
                if (!drainPerTick.TryGetValue(id, out tickDmg) || tickDmg <= 0f)
                {
                    continue;
                }
                Player owner = GetPlayerByID(id);
                if (owner == null || owner.data == null || owner.data.dead)
                {
                    continue;
                }
                float t;
                if (!drainTimer.TryGetValue(id, out t))
                {
                    t = 0f;
                }
                t += TimeHandler.deltaTime; // already timescale-scaled (TimeHandler.cs:52)
                while (t >= drainTickInterval)
                {
                    t -= drainTickInterval;
                    ApplyDrainTick(owner, tickDmg);
                }
                drainTimer[id] = t;
            }
        }

        private static void ApplyDrainTick(Player owner, float tickDmg)
        {
            if (owner.data.health <= 0f)
            {
                return;
            }
            owner.data.health -= tickDmg; // constant tick — hp is spent linearly
            if (owner.data.health <= 0f)
            {
                // hp fully drained: finish with the vanilla lethal death path
                // (correct death effect, Phoenix handling, kill-free death).
                // The bypass flag lets our own DoDamage past the damage gate.
                UnityEngine.Debug.Log($"[DEER] Heart drain finished — killing {owner.data.name}");
                applyingHeartDrain = true;
                owner.data.healthHandler.DoDamage(
                    new Vector2(owner.data.maxHealth * 10f, 0f),
                    owner.transform.position,
                    Color.black,
                    null,
                    null,
                    false,
                    true,
                    false
                );
                applyingHeartDrain = false;
            }
        }

        // called from RPC_ThrowHeart on every client: samples the drain and
        // applies the heart-out stat modifications
        private static void BeginThrowBuffs(int ownerPlayerID)
        {
            Player owner = GetPlayerByID(ownerPlayerID);
            if (owner == null || owner.data == null)
            {
                return;
            }
            float maxHp = owner.data.maxHealth;
            float lifetime = 20f * maxHp / (maxHp + 100f);
            float dps = (maxHp + 100f) / 20f; // = hpAtThrow / lifetime, constant
            drainPerTick[ownerPlayerID] = dps * drainTickInterval; // 40 ticks * dmg = maxHp when h=100
            drainTimer[ownerPlayerID] = 0f;
            loggedGateBlock[ownerPlayerID] = false;
            UnityEngine.Debug.Log($"[DEER] Heart drain sampled: maxHealth {maxHp:F0} -> lifetime {lifetime:F2}s, drain {dps:F2} hp/s ({dps * drainTickInterval:F2} hp per 0.25s tick)");

            var stats = owner.data.stats;
            if (stats != null)
            {
                savedMoveSpeed[ownerPlayerID] = stats.movementSpeed;
                stats.movementSpeed = stats.movementSpeed * heartSlowMultiplier;
            }
            // zero ALL healing so the drain always wins in the end
            var health = owner.data.healthHandler;
            if (health != null)
            {
                savedRegen[ownerPlayerID] = health.regeneration;
                health.regeneration = 0f;
            }
            if (stats != null)
            {
                savedStatsRegen[ownerPlayerID] = stats.regen;
                savedLifeSteal[ownerPlayerID] = stats.lifeSteal;
                stats.regen = 0f;
                stats.lifeSteal = 0f;
            }
        }

        // called from RemoveHeart — the single choke point every heart-removal
        // path (heart shot, wall hit, owner death, round reset) funnels into
        private static void RestoreThrowBuffs(int ownerPlayerID)
        {
            drainPerTick.Remove(ownerPlayerID);
            drainTimer.Remove(ownerPlayerID);
            loggedGateBlock.Remove(ownerPlayerID);
            Player owner = GetPlayerByID(ownerPlayerID);
            if (owner == null || owner.data == null)
            {
                savedMoveSpeed.Remove(ownerPlayerID);
                savedRegen.Remove(ownerPlayerID);
                savedStatsRegen.Remove(ownerPlayerID);
                savedLifeSteal.Remove(ownerPlayerID);
                return;
            }
            var stats = owner.data.stats;
            float v;
            if (stats != null && savedMoveSpeed.TryGetValue(ownerPlayerID, out v))
            {
                stats.movementSpeed = v;
            }
            var health = owner.data.healthHandler;
            if (health != null && savedRegen.TryGetValue(ownerPlayerID, out v))
            {
                health.regeneration = v;
            }
            if (stats != null)
            {
                if (savedStatsRegen.TryGetValue(ownerPlayerID, out v))
                {
                    stats.regen = v;
                }
                if (savedLifeSteal.TryGetValue(ownerPlayerID, out v))
                {
                    stats.lifeSteal = v;
                }
            }
            savedMoveSpeed.Remove(ownerPlayerID);
            savedRegen.Remove(ownerPlayerID);
            savedStatsRegen.Remove(ownerPlayerID);
            savedLifeSteal.Remove(ownerPlayerID);
            UnityEngine.Debug.Log($"[DEER] Heart resolved — stats restored for {owner.data.name}");
        }

        private static void DebugHeartDeathReset(Player player)
        {
            UnityEngine.Debug.Log($"[DEER] Owner died — heart removed and throw re-armed for {player.data.name}");
        }

        // ---- local (owning client) control — one throw per round ----
        private void SimulateOwner()
        {
            bool canThrow = player.data.isPlaying
                && !player.data.dead
                && GameManager.instance.battleOngoing
                && TimeHandler.timeScale > 0f
                && GetState(player.playerID) == HeartState.Carried;

            if (!canThrow)
            {
                return;
            }

            if (Input.GetKeyDown(keyThrow))
            {
                Vector3 aim = player.data.aimDirection;
                Vector3 pos = player.transform.position + aim * 0.55f; // drop it out in front, not inside yourself
                // never buries it in the floor/ceiling when aiming straight down or up
                pos.y = Mathf.Clamp(pos.y, player.transform.position.y - 0.1f, player.transform.position.y + 0.4f);
                pos.z = 0f;

                // extremely short toss: mostly an arc forward and up, so it reads
                // like dropping the heart in front of you (~3 ft / ~1 world unit).
                Vector2 vel = (Vector2)aim * 2.0f + Vector2.up * 2.5f;
                if (vel.sqrMagnitude < 0.01f)
                {
                    vel = Vector2.up * 2.5f;
                }

                UnityEngine.Debug.Log($"[DEER] Heart key pressed -> throw at {pos}, vel {vel}");
                NetworkingManager.RPC(
                    typeof(HeartEffect),
                    nameof(RPC_ThrowHeart),
                    player.playerID,
                    pos,
                    vel
                );
            }
        }

        // ---- shared-state helpers ----
        private static HeartState GetState(int ownerPlayerID)
        {
            HeartState s;
            if (states.TryGetValue(ownerPlayerID, out s))
            {
                return s;
            }
            return HeartState.Carried;
        }

        // live registry accessors used by HeartObject (one GO per owner)
        public static bool HasHeart(int ownerPlayerID)
        {
            GameObject go;
            return heartObjects.TryGetValue(ownerPlayerID, out go) && go != null;
        }

        public static void SetHeartPosition(int ownerPlayerID, Vector3 pos)
        {
            heartPositions[ownerPlayerID] = pos;
        }

        private static Player GetPlayerByID(int playerID)
        {
            var method = typeof(PlayerManager).GetMethod(
                "GetPlayerWithID",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            return method?.Invoke(PlayerManager.instance, new object[] { playerID }) as Player;
        }

        // ---------------------------------------------------------------
        // RPCs (called on every client)
        // ---------------------------------------------------------------
        [UnboundRPC]
        private static void RPC_ThrowHeart(int ownerPlayerID, Vector3 pos, Vector2 vel)
        {
            UnityEngine.Debug.Log($"[DEER] RPC_ThrowHeart owner {ownerPlayerID} at {pos}, vel {vel}");
            RemoveHeart(ownerPlayerID); // re-throw safety
            BeginThrowBuffs(ownerPlayerID);
            states[ownerPlayerID] = HeartState.OnGround;
            EnsureHeartObject(ownerPlayerID, pos, vel);
        }

        [UnboundRPC]
        public static void RPC_HeartWallHit(int ownerPlayerID, Vector3 hitPos, int killerPlayerID)
        {
            UnityEngine.Debug.Log($"[DEER] RPC_HeartWallHit owner {ownerPlayerID} at {hitPos}");
            HeartDied(ownerPlayerID, hitPos, killerPlayerID);
        }

        [UnboundRPC]
        public static void RPC_HeartShot(int ownerPlayerID, Vector2 damage, Vector3 damagePos, int killerPlayerID)
        {
            UnityEngine.Debug.Log($"[DEER] RPC_HeartShot owner {ownerPlayerID} dmg {damage.magnitude:F1} from killer {killerPlayerID}");
            if (GetState(ownerPlayerID) == HeartState.OnGround)
            {
                // vanilla replication recipe: resolve the killer locally and take
                // the full damage on this client (mirrors HealthHandler.RPCA_SendTakeDamage)
                HeartDied(ownerPlayerID, damagePos, killerPlayerID);
            }
        }

        // kills the owner through the vanilla damage path (correct death effect,
        // Phoenix handling, kill credit)
        private static void HeartDied(int ownerPlayerID, Vector3 pos, int killerPlayerID)
        {
            RemoveHeart(ownerPlayerID);
            states[ownerPlayerID] = HeartState.Dead;
            Player owner = GetPlayerByID(ownerPlayerID);
            if (owner == null || owner.data == null || owner.data.dead)
            {
                return;
            }
            Player killer = killerPlayerID >= 0 ? GetPlayerByID(killerPlayerID) : null;
            UnityEngine.Debug.Log($"[DEER] Heart died — killing owner {owner.data.name} (killer: {(killer != null ? killer.data.name : "nobody")})");
            owner.data.healthHandler.DoDamage(
                new Vector2(owner.data.maxHealth * 10f, 0f),
                pos,
                Color.black,
                null,
                killer,
                false,
                true,
                false
            );
        }

        // ---- heart object ----
        private static void EnsureHeartObject(int ownerPlayerID, Vector3 pos, Vector2 vel)
        {
            if (heartObjects.TryGetValue(ownerPlayerID, out GameObject existing) && existing != null)
            {
                return;
            }
            Player owner = GetPlayerByID(ownerPlayerID);
            float scale = owner != null ? owner.transform.localScale.x : 1f;

            var go = new GameObject($"DEER_Heart_{ownerPlayerID}");
            go.transform.position = pos;

            // skin-colored heart visual
            var visual = new GameObject("visual");
            visual.transform.SetParent(go.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = GetHeartSprite();
            sr.sortingOrder = 2;

            // ONE entity, zero colliders: players never physically collide with
            // the map via rigidbodies — PlayerVelocity/PlayerCollision are custom
            // raycast integration against PlayerCollision.mask (verified). A
            // heart with NO colliders physically touches nothing: player physics
            // can never interact with it (no layer-matrix games at all). All
            // motion is custom kinematic integration in HeartObject using the
            // owner's own PlayerCollision.mask + Gravity.gravityForce. Bullet
            // hits arrive via the BulletHeartPatch sweep.
            var heart = go.AddComponent<HeartObject>();
            heart.ownerPlayerID = ownerPlayerID;
            heart.SetScale(scale);
            heart.velocity = vel;

            // player's own map mask + gravity — same recipe the player uses
            float gravityForce = 100f; // sandbox fallback
            LayerMask mask = 0;
            if (owner != null)
            {
                var pc = owner.GetComponent<PlayerCollision>();
                if (pc != null)
                {
                    mask = pc.mask;
                }
                var g = owner.GetComponent<Gravity>();
                if (g != null && g.gravityForce != 0f)
                {
                    gravityForce = g.gravityForce;
                }
            }
            heart.ownerGravityForce = gravityForce;
            heart.heartMask = mask;
            UnityEngine.Debug.Log($"[DEER] Heart spawned: gravityForce {gravityForce}, mask {mask.value}");

            Color main = Color.white;
            if (owner != null)
            {
                PlayerSkin skin = GetSkin(owner);
                if (skin != null)
                {
                    main = skin.color;
                }
            }
            sr.color = main;

            heartPositions[ownerPlayerID] = pos;
            heartObjects[ownerPlayerID] = go;
        }

        // current color of a live player (same fallback ladder as portals)
        private static void RemoveHeart(int ownerPlayerID)
        {
            RestoreThrowBuffs(ownerPlayerID);
            heartObjects.Remove(ownerPlayerID);
            heartPositions.Remove(ownerPlayerID);
            var go = GameObject.Find($"DEER_Heart_{ownerPlayerID}");
            if (go != null)
            {
                Destroy(go);
            }
        }

        // current color of a live player (same fallback ladder as portals)
        private static PlayerSkin GetSkin(Player player)
        {
            PlayerSkin skin = null;
            try
            {
                int colorID = player.colorID();
                skin = UnboundLib.Utils.ExtraPlayerSkins.GetPlayerSkinColors(colorID);
            }
            catch (Exception)
            {
                // colorID not assigned yet
            }
            if (skin == null)
            {
                skin = PlayerSkinBank.GetPlayerSkinColors(player.playerID);
            }
            return skin;
        }

        private static Sprite GetHeartSprite()
        {
            if (heartSprite != null)
            {
                return heartSprite;
            }
            // procedural heart: classic heart implicit curve,
            // hard-thresholded and softened by bilinear filtering
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float fx = (x + 0.5f - size / 2f) / 42f;
                    float fy = -(y + 0.5f - size / 2f) / 42f + 0.4f;
                    float v = Mathf.Pow(fx * fx + fy * fy - 0.5f, 3f) - fx * fx * Mathf.Pow(fy, 3f);
                    float alpha = v <= 0f ? 1f : 0f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            heartSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 128f / 1.1f);
            heartSprite.name = "DEERHeart";
            return heartSprite;
        }

        // ---- round reset (same idiom as Portals) ----
        private static bool hooksRegistered;

        public static void RegisterRoundResetHooks()
        {
            if (hooksRegistered)
            {
                return;
            }
            hooksRegistered = true;
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, gm => ResetAll());
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, gm => ResetAll());
            GameModeManager.AddHook(GameModeHooks.HookGameStart, gm => ResetAll());
        }

        private static IEnumerator ResetAll()
        {
            UnityEngine.Debug.Log("[DEER] Round boundary — resetting all hearts");
            var ownerIDs = states.Keys.ToList();
            foreach (int id in ownerIDs)
            {
                RemoveHeart(id);
            }
            states.Clear();
            heartObjects.Clear();
            heartPositions.Clear();
            yield break;
        }

        // ---------------------------------------------------------------
        // Bullet hits: the same memory-less swept-segment idiom as the
        // bullet-portal portalpass — a Harmony postfix on MoveTransform.Update
        // fires for every projectile on the exact frame it integrates its move
        // (including the last frame before death, which per-frame polling
        // misses). Each owner's heart is a disc; a bullet touching the disc
        // kills that heart's owner (1 HP — anyone's bullet, including their own).
        // ---------------------------------------------------------------
        [HarmonyPatch(typeof(MoveTransform), "Update")]
        internal static class BulletHeartPatch
        {
            [HarmonyPostfix]
            private static void AfterBulletMove(MoveTransform __instance)
            {
                HeartEffect.CheckBullet(__instance);
            }
        }

        // Death cleanup hook. Every death routes through HealthHandler.RPCA_Die
        // (or RPCA_Die_Phoenix for Phoenix deaths) — the same RPC that sets
        // data.dead = true and DEACTIVATES the player GameObject. That
        // deactivation is why the old HeartEffect.Update dead-check never ran:
        // a MonoBehaviour on a deactivated GameObject gets no Update ticks.
        // These PunRPCs execute locally on EVERY client, so a postfix here
        // re-arms the throw and removes the heart everywhere deterministically
        // (no network message needed, no reliance on Update running).
        [HarmonyPatch]
        internal static class DeathHeartPatch
        {
            private static void ResetOnDeath(HealthHandler health)
            {
                if (health == null)
                {
                    return;
                }
                var player = health.GetComponent<Player>();
                if (player == null)
                {
                    return;
                }
                if (GetState(player.playerID) != HeartState.Carried)
                {
                    RemoveHeart(player.playerID);
                    states[player.playerID] = HeartState.Carried;
                    UnityEngine.Debug.Log($"[DEER] Owner died — heart removed and throw re-armed for {player.data.name}");
                }
            }

            [HarmonyPatch(typeof(HealthHandler), "RPCA_Die")]
            [HarmonyPostfix]
            private static void AfterDie(HealthHandler __instance)
            {
                ResetOnDeath(__instance);
            }

            [HarmonyPatch(typeof(HealthHandler), "RPCA_Die_Phoenix")]
            [HarmonyPostfix]
            private static void AfterDiePhoenix(HealthHandler __instance)
            {
                ResetOnDeath(__instance);
            }
        }

        // Milestone 2 — heart-out invincibility. HealthHandler.DoDamage is the
        // single funnel for ALL conventional damage (decompiled, verified):
        //   TakeDamage -> DoDamage, RPCA_SendTakeDamage -> TakeDamage -> DoDamage,
        //   DamageOverTime.cs:34 -> health.DoDamage per DoT interval.
        // While the owner's heart is OnGround, a prefix here cancels every
        // incoming hit on EVERY client — including the owner's own bullets.
        // Map deaths (pits, spike settle) are not damage and still apply,
        // which is by design. Our drain finisher passes through via the
        // applyingHeartDrain bypass flag; the heart-shot kill is naturally
        // unaffected because the state is flipped off OnGround before it fires.
        [HarmonyPatch]
        internal static class IncomingDamageGate
        {
            [HarmonyPatch(typeof(HealthHandler), "DoDamage")]
            [HarmonyPrefix]
            private static bool Gate(HealthHandler __instance)
            {
                if (applyingHeartDrain)
                {
                    return true; // our own lethal drain finisher must land
                }
                var p = __instance.GetComponent<Player>();
                if (p == null)
                {
                    return true;
                }
                if (GetState(p.playerID) == HeartState.OnGround)
                {
                    if (!loggedGateBlock.TryGetValue(p.playerID, out bool logged) || !logged)
                    {
                        loggedGateBlock[p.playerID] = true;
                        UnityEngine.Debug.Log($"[DEER] Damage blocked — {p.data.name} is heart-out and invincible");
                    }
                    return false;
                }
                return true;
            }
        }

        // lazily built/cached heart-disc list — valid for the current frame
        private static List<(int owner, Vector3 pos, float radius)> heartDiscsCache;
        private static int heartDiscsFrame = -1;

        private static List<(int owner, Vector3 pos, float radius)> GetHeartDiscs()
        {
            if (heartDiscsFrame == Time.frameCount && heartDiscsCache != null)
            {
                return heartDiscsCache;
            }
            var discs = new List<(int, Vector3, float)>();
            foreach (var kv in heartPositions)
            {
                if (!(heartObjects.TryGetValue(kv.Key, out GameObject go) && go != null))
                {
                    continue;
                }
                float radius = 0.35f;
                var heartComp = go.GetComponent<HeartObject>();
                if (heartComp != null)
                {
                    radius = heartComp.PhysicsRadius();
                }
                discs.Add((kv.Key, kv.Value, radius));
            }
            heartDiscsCache = discs;
            heartDiscsFrame = Time.frameCount;
            return discs;
        }

        // called from the MoveTransform.Update postfix, once per bullet per frame
        private static void CheckBullet(MoveTransform mt)
        {
            if (!GameManager.instance.battleOngoing || TimeHandler.timeScale <= 0f)
            {
                return;
            }
            // only true projectiles (same filter as the portal patch)
            var ph = mt.GetComponent<ProjectileHit>();
            if (ph == null)
            {
                return;
            }
            var discs = GetHeartDiscs();
            if (discs.Count == 0)
            {
                return;
            }

            float dt = TimeHandler.deltaTime * mt.multiplier; // exact MoveTransform step length
            Vector3 pos = mt.transform.position;
            Vector3 prev = pos - mt.velocity * dt; // velocity-derived flight segment

            foreach (var disc in discs)
            {
                float r2 = disc.radius * disc.radius;
                bool touch =
                    SegmentTouchesDisc(prev, pos, disc.pos, disc.radius) ||
                    (pos - disc.pos).sqrMagnitude <= r2; // spawned-inside fallback
                if (!touch)
                {
                    continue;
                }

                int shooterID = ph.ownPlayer != null ? ph.ownPlayer.playerID : -1;
                UnityEngine.Debug.Log($"[DEER] Bullet hit heart of owner {disc.owner} at {pos} (shooter {shooterID}, dmg {ph.damage})");
                NetworkingManager.RPC(
                    typeof(HeartEffect),
                    nameof(RPC_HeartShot),
                    disc.owner,
                    new Vector2(ph.damage, 0f),
                    (Vector3)pos,
                    shooterID
                );
                return; // one hit is enough — the heart dies, disc list is next-frame stale
            }

            // prune stale discs — once per frame
            if (Time.frameCount != lastPruneFrame)
            {
                lastPruneFrame = Time.frameCount;
                var dead = heartPositions.Keys.Where(id => !HasHeart(id)).ToList();
                foreach (int id in dead)
                {
                    heartPositions.Remove(id);
                }
            }
        }

        private static int lastPruneFrame = -1;

        // exact same test as the portal card's SegmentTouchesDisc
        private static bool SegmentTouchesDisc(Vector3 prev, Vector3 cur, Vector3 center, float radius)
        {
            Vector3 seg = cur - prev;
            float lenSqr = seg.sqrMagnitude;
            if (lenSqr < 0.000001f)
            {
                return false; // no movement — positional fallback handles it
            }
            float t = Vector3.Dot(center - prev, seg) / lenSqr;
            t = Mathf.Clamp01(t); // clamp keeps discs behind the flight path dormant
            Vector3 closest = prev + seg * t;
            return (closest - center).sqrMagnitude <= radius * radius;
        }

        // ---------------------------------------------------------------
        // Client-side HUD (bottom-left): heart ability icon, same shared
        // presentation as the portal card via the AbilityHUD row
        // (see AbilityHud.cs). Red "H" = ready; amber, empty = the heart is
        // currently on the ground.
        // ---------------------------------------------------------------
        private static Texture2D hudIconTex;

        private void Awake()
        {
            EnsureHudTextures();
            AbilityHUD.Register(this, HudVisible, HudDraw);
        }

        private static void EnsureHudTextures()
        {
            if (hudIconTex == null)
            {
                // filled disc, sharp edge — deliberately NOT the portal ring look
                hudIconTex = MakeDiscTexture();
            }
        }

        private bool HudVisible()
        {
            return player != null && player.data != null && player.data.view.IsMine && player.data.isPlaying;
        }

        private void HudDraw(Rect area)
        {
            bool ready = GetState(player.playerID) == HeartState.Carried;
            string caption = ready ? keyThrow.ToString() : string.Empty;
            AbilityHUD.DrawCircle(
                area,
                hudIconTex,
                new Color(0.90f, 0.25f, 0.35f), // ready = red (it IS a heart)
                new Color(0.95f, 0.55f, 0.15f), // on the ground = amber
                ready,
                caption
            );
        }

        private static Texture2D MakeDiscTexture()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size / 2f;
                    float dy = y + 0.5f - size / 2f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01((size / 2f - r) / 2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.name = "DEERHeartHud";
            return tex;
        }
    }

    /// <summary>
    /// Damage surface + custom kinematic physics for the heart's root
    /// GameObject. Zero colliders: it can never physically interact with
    /// players. Motion = kinematic integration copying the player's own
    /// recipe (Gravity.gravityForce for acceleration, PlayerCollision.mask
    /// for map sweeps). Bullet kills arrive via the BulletHeartPatch sweep;
    /// wall/ceiling contact kills the owner, floor landings are safe.
    /// </summary>
    public class HeartObject : Damagable
    {
        public int ownerPlayerID = -1;
        public float ownerGravityForce = 100f;
        public LayerMask heartMask;
        public Vector2 velocity;

        private bool dying;
        private bool grounded;
        private bool loggedSettle;
        private float sinceSpawn;

        private const float radius = 0.525f; // 50% larger (0.35 * 1.5)
        private const float visualScaleBonus = 1.5f; // sprite grows with the disc
        private const float settleSpeed = 0.5f;
        private const float maxSubstep = 0.3f; // never move more than this per cast step
        private const float deathFallY = -200f; // fell out of the world entirely

        // player-recipe gravity ramp: airborne, force rises linearly with time
        // since grounded (Gravity.cs: pow(sinceGrounded, exponent)); exponent = 1
        private float sinceAir;

        public float PhysicsRadius()
        {
            return radius * transform.localScale.x;
        }

        public void SetScale(float scale)
        {
            // ×1.5: the heart is a 50% larger physical disc
            transform.localScale = Vector3.one * Mathf.Clamp(scale * 0.8f * visualScaleBonus, 0.6f, 2.1f);
        }

        private void Update()
        {
            if (dying)
            {
                return;
            }
            // keep the shared registry position in sync for bullet detection
            if (HeartEffect.HasHeart(ownerPlayerID))
            {
                HeartEffect.SetHeartPosition(ownerPlayerID, transform.position);
            }
        }

        // ---- kinematic integration (player recipe) ----
        private void FixedUpdate()
        {
            if (dying || heartMask.value == 0f)
            {
                return;
            }
            sinceSpawn += Time.fixedDeltaTime;

            float timeScale = TimeHandler.timeScale;
            if (timeScale <= 0f)
            {
                return;
            }
            // gravity — player recipe (Gravity.cs): the force scales with time
            // spent airborne (sinceGrounded ramp), starting at ZERO the frame
            // the leap/throw leaves the floor and rising linearly; grounded
            // (num <= 0) applies no gravity pull at all.
            if (!grounded)
            {
                sinceAir += Time.fixedDeltaTime * timeScale;
                velocity += Vector2.down * (ownerGravityForce * sinceAir) * Time.fixedDeltaTime * timeScale;
            }
            else
            {
                sinceAir = 0f; // grounded: no pull, exactly like the player
            }
            if (grounded && velocity.magnitude < settleSpeed)
            {
                velocity = Vector2.zero;
            }

            // substep sweep: no substep moves further than maxSubstep so the
            // heart can never tunnel through map geometry at any speed
            Vector2 delta = velocity * Time.fixedDeltaTime * timeScale;
            float dist = delta.magnitude;
            if (dist < 0.0001f)
            {
                return;
            }
            int steps = Mathf.CeilToInt(dist / maxSubstep);
            Vector2 stepVec = delta / steps;
            for (int i = 0; i < steps; i++)
            {
                float stepDist = stepVec.magnitude;
                // exact same cast family as PlayerCollision: swept circle
                // against the map (owner's own mask), ignoring player hits
                RaycastHit2D[] hits = Physics2D.CircleCastAll(
                    transform.position,
                    radius * transform.localScale.x,
                    stepVec.normalized,
                    stepDist,
                    heartMask
                );
                RaycastHit2D nearest = new RaycastHit2D { distance = float.PositiveInfinity };
                foreach (var h in hits)
                {
                    if (!h || h.collider == null || h.transform.GetComponentInParent<Player>() != null)
                    {
                        continue; // map geometry only — never players
                    }
                    if (h.distance < nearest.distance)
                    {
                        nearest = h;
                    }
                }
                if (!nearest.collider)
                {
                    transform.position += (Vector3)(stepVec * (stepDist > 0f ? 1f : 0f));
                    continue;
                }
                // snap to the contact surface, resolve velocity against it
                Vector2 n = nearest.normal.normalized;
                transform.position = (Vector3)nearest.point + (Vector3)(n * (radius * transform.localScale.x));
                float intoNormal = Vector2.Dot(velocity, n);
                float upDot = Vector2.Dot(n, Vector2.up);
                if (upDot < 0.5f)
                {
                    // wall/ceiling — the fragile heart (and its owner) dies, but
                    // only on a REAL impact: a barely-moving graze along a
                    // curved/sloped surface is a spared touch, not a hit.
                    if (intoNormal < -0.4f)
                    {
                        HeartWalled(transform.position);
                        return;
                    }
                }
                // floor (or grazing slope) contact: STOP dead — no slide, no
                // bounce. The heart lands exactly where it touches down.
                velocity = Vector2.zero;
                grounded = true;
                sinceAir = 0f;
                if (!loggedSettle)
                {
                    loggedSettle = true;
                    UnityEngine.Debug.Log($"[DEER] Heart settled (normal {n}) at {transform.position}");
                }
                break; // resting — no further movement this tick
            }
            if (transform.position.y < deathFallY)
            {
                HeartWalled(transform.position);
            }
        }

        // wall/ceiling hit or lost below the world: kill via the network path
        private void HeartWalled(Vector3 pos)
        {
            if (dying)
            {
                return;
            }
            dying = true;
            UnboundLib.NetworkingManager.RPC(
                typeof(HeartEffect),
                nameof(HeartEffect.RPC_HeartWallHit),
                ownerPlayerID,
                pos,
                -1
            );
        }

        // ---- bullet / explosion damage entry point ----
        public override void CallTakeDamage(Vector2 damage, Vector2 damagePosition, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true)
        {
            if (dying)
            {
                return;
            }
            int killerID = (damagingPlayer != null) ? damagingPlayer.playerID : -1;
            UnboundLib.NetworkingManager.RPC(
                typeof(HeartEffect),
                nameof(HeartEffect.RPC_HeartShot),
                ownerPlayerID,
                damage,
                (Vector3)damagePosition,
                killerID
            );
        }

        public override void TakeDamage(Vector2 damage, Vector2 damagePosition, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true, bool ignoreBlock = false)
        {
            CallTakeDamage(damage, damagePosition, damagingWeapon, damagingPlayer, lethal);
        }

        public override void TakeDamage(Vector2 damage, Vector2 damagePosition, Color dmgColor, GameObject damagingWeapon = null, Player damagingPlayer = null, bool lethal = true, bool ignoreBlock = false)
        {
            CallTakeDamage(damage, damagePosition, damagingWeapon, damagingPlayer, lethal);
        }
    }

}
