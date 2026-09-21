using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.Extensions;
using UnboundLib.GameModes;
using UnboundLib.Utils;
using ModdingUtils.Extensions;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Portals: pressing [E] plants/moves Portal A and [Q] plants/moves Portal B
    /// at the player's position (shared personal cooldown).
    /// Any player touching a portal is teleported to its
    /// sibling (goes through walls — vanilla teleport recipe). Personal cooldown,
    /// independent of gun and block.
    /// </summary>
    public class PortalCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // downside: -25% movement speed (multiplier field; 0.75 = 75% of normal)
            statModifiers.movementSpeed = 0.75f;
            UnityEngine.Debug.Log($"[DEER] PortalCard SetupCard: setting movementSpeed to {statModifiers.movementSpeed}");
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.gameObject.GetOrAddComponent<PortalEffect>();
            effect.SetPlayer(player);
            PortalEffect.RegisterRoundResetHooks();
            UnityEngine.Debug.Log($"[DEER] PortalCard added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.GetComponent<PortalEffect>();
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        protected override string GetTitle()
        {
            return "Portals";
        }

        protected override string GetDescription()
        {
            return "Control two connected portals like an absolute gamer.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Place Portal",
                    amount = "E (1st) / Q (2nd)",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Movement speed",
                    amount = "-25%",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Ability Cooldown",
                    amount = "2.5 seconds",
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

        // applies the MoveTransform.Update postfix once, at card registration
        private static bool harmonyApplied;
        public static void Init()
        {
            if (harmonyApplied) return;
            harmonyApplied = true;
            new Harmony("com.deeroot.cards.portals").PatchAll(typeof(PortalEffect.BulletPortalPatch));
        }
    }

    /// <summary>
    /// Attached to a player. Pressing the player's portal keys (owner client,
    /// E = portal A, Q = portal B) spawns/moves that specific portal at the
    /// player's current position on a shared personal cooldown, and teleports
    /// the local player when they touch one of them.
    /// </summary>
    public class PortalEffect : MonoBehaviour
    {
        private Player player;

        // ---- shared state registry (replicated identically on every client) ----
        // ownerPlayerID -> [portalA, portalB] positions
        private static Dictionary<int, Vector3[]> portalPositions = new Dictionary<int, Vector3[]>();
        private static Dictionary<int, bool[]> portalActive = new Dictionary<int, bool[]>();

        // ---- runtime visuals keyed the same way ----
        private static Dictionary<long, GameObject> portalVisuals = new Dictionary<long, GameObject>();
        private static Sprite ringSprite;
        private static Sprite discSprite;

        // ---- per-player (per-effect-instance) state ----
        private KeyCode keyPortalA = KeyCode.E;
        private KeyCode keyPortalB = KeyCode.Q;
        private float placeCooldownLeft;
        private float cooldownUntil;
        private bool armed = true;
        private const float touchRadius = 0.75f;
        private const float placeCooldown = 2.5f; // personal portal keybind cooldown

        // Key table is per-CLIENT, not per-playerID: online each client has its
        // own keyboard, so every (solo/online) local player uses E/Q regardless
        // of playerID. Only on true local split-screen (2+ IsMine players on one
        // machine) does player index determine the key pair (P1 = E/Q, P2 = RShift/RCtrl).
        private void ResolveKeys()
        {
            if (player == null || player.data == null)
            {
                keyPortalA = KeyCode.E;
                keyPortalB = KeyCode.Q;
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
            if (localCount > 1)
            {
                if (localIndex == 1)
                {
                    keyPortalA = KeyCode.RightShift;
                    keyPortalB = KeyCode.RightControl;
                }
                else
                {
                    keyPortalA = KeyCode.E;
                    keyPortalB = KeyCode.Q;
                }
            }
            else
            {
                keyPortalA = KeyCode.E;
                keyPortalB = KeyCode.Q;
            }
            UnityEngine.Debug.Log($"[DEER] Portal keys resolved: A={keyPortalA} B={keyPortalB} (local player index {localIndex}/{localCount})");
        }

        private const float teleportCooldown = 0.35f;

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
            UnityEngine.Debug.Log($"[DEER] PortalEffect started on player {player.data.name}");
        }

        private void OnDestroy()
        {
            AbilityHUD.Unregister(this);
            RemovePortals(player != null ? player.playerID : -1);
        }

        private void Update()
        {
            if (player == null || player.data == null)
            {
                return;
            }

            if (player.data.view.IsMine)
            {
                SimulateOwner();
                TouchCheck();
                // bullet handling runs in the MoveTransform patch, on each
                // bullet's own update tick — see BulletPortalPatch
            }
        }

        // ---- bullet portal pass (bullet's own clock) ----
        private const float bulletExitOffset = 0.4f;
        // bullet -> (armed, expire). a teleported bullet stays FULLY dormant (no
        // segment test, no positional test) until a frame passes where its flight
        // segment touches no portal disc and its position is outside every
        // radius — then it re-arms. This makes a bounce-back geometrically
        // impossible: the loop trigger (a still-contacting bullet being tested
        // again) can't happen, because disarmed bullets are never tested.
        // Detection itself is memory-less: the flight segment is derived from
        // the bullet each frame (position - velocity*deltaTime*multiplier, the
        // exact MoveTransform integration), immune to pooled/reused bullets.
        private static readonly Dictionary<MoveTransform, (bool armed, float expire)> bulletArmed = new Dictionary<MoveTransform, (bool, float)>();

        // detection runs in a Harmony postfix on MoveTransform.Update: it fires
        // for every projectile on the exact frame it integrates its move —
        // including the final frame of a hyper-speed bullet, BEFORE the wall
        // raycast kills it. Per-frame FindObjectsOfType polling misses those
        // last-frame crossings entirely (the bullet is dead before our Update
        // ever samples it), which is why ultra-fast shots sometimes teleported.
        [HarmonyPatch(typeof(MoveTransform), "Update")]
        internal static class BulletPortalPatch
        {
            [HarmonyPostfix]
            private static void AfterBulletMove(MoveTransform __instance)
            {
                PortalEffect.CheckBullet(__instance);
            }
        }

        // lazily built/cached active portal pairs — valid for the current frame
        private static List<(Vector3 a, Vector3 b)> portalPairsCache;
        private static int portalPairsFrame = -1;

        private static List<(Vector3 a, Vector3 b)> GetPortalPairs()
        {
            if (portalPairsFrame == Time.frameCount && portalPairsCache != null)
            {
                return portalPairsCache;
            }
            var pairs = new List<(Vector3 a, Vector3 b)>();
            foreach (var kv in portalPositions)
            {
                int owner = kv.Key;
                if (!portalActive.TryGetValue(owner, out bool[] active) || !(active[0] && active[1]))
                {
                    continue;
                }
                pairs.Add((kv.Value[0], kv.Value[1]));
            }
            portalPairsCache = pairs;
            portalPairsFrame = Time.frameCount;
            return pairs;
        }

        // called from the MoveTransform.Update postfix, once per bullet per frame
        private static void CheckBullet(MoveTransform mt)
        {
            if (!battleOngoing())
            {
                return;
            }
            // only true projectiles: a bullet/core carries a ProjectileHit;
            // generic MoveTransform users (UI movers etc.) are skipped.
            var ph = mt.GetComponent<ProjectileHit>();
            if (ph == null)
            {
                return;
            }
            if (portalPairsFrame != Time.frameCount && !HasActivePairs())
            {
                return;
            }
            var pairs = GetPortalPairs();
            if (pairs.Count == 0)
            {
                return;
            }

            float now = Time.time;
            float dt = TimeHandler.deltaTime * mt.multiplier; // exact MoveTransform step length

            if (bulletArmed.TryGetValue(mt, out var entry) && Time.time < entry.expire && !entry.armed)
            {
                // FULLY dormant after a teleport: no segment test, no positional
                // test — anything else lets a slow bullet re-cross the exit disc
                // the moment it re-arms and ping-pong between portals forever.
                // It re-arms only in a frame where its flight segment and
                // position touch NO portal at all; the first clear frame is not
                // re-tested, so portals behind the bullet can't grab it back.
                Vector3 dormantPrev = mt.transform.position - mt.velocity * dt;
                bool contact = false;
                foreach (var pair in pairs)
                {
                    if (SegmentTouchesDisc(dormantPrev, mt.transform.position, pair.a) ||
                        SegmentTouchesDisc(dormantPrev, mt.transform.position, pair.b) ||
                        (mt.transform.position - pair.a).sqrMagnitude <= touchRadius * touchRadius ||
                        (mt.transform.position - pair.b).sqrMagnitude <= touchRadius * touchRadius)
                    {
                        contact = true;
                        break;
                    }
                }
                if (contact)
                {
                    bulletArmed[mt] = (false, now + 1f);
                    UnityEngine.Debug.Log($"[DEER] Bullet dormant (still touching a portal disc) at {mt.transform.position}");
                    return;
                }
                bulletArmed[mt] = (true, now + 1f); // re-armed; skip this frame entirely
                return;
            }

            // self-contained flight segment: MoveTransform integrates
            // `velocity * deltaTime * multiplier` each frame (deltaTime =
            // TimeHandler.deltaTime * simulationSpeed, which is ~1 here), so
            // working backwards from the current position reconstructs this
            // frame's flight path without any cross-frame bookkeeping.
            Vector3 prev = mt.transform.position - mt.velocity * dt;

            foreach (var pair in pairs)
            {
                // swept test: teleport if the flight segment crossed the disc
                // this frame; position test retained as fallback for bullets
                // that spawn inside a portal at point-blank range
                if (SegmentTouchesDisc(prev, mt.transform.position, pair.a) ||
                    (mt.transform.position - pair.a).sqrMagnitude <= touchRadius * touchRadius)
                {
                    TeleportBullet(mt, pair.b);
                    return;
                }
                if (SegmentTouchesDisc(prev, mt.transform.position, pair.b) ||
                    (mt.transform.position - pair.b).sqrMagnitude <= touchRadius * touchRadius)
                {
                    TeleportBullet(mt, pair.a);
                    return;
                }
            }
            bulletArmed[mt] = (true, now + 1f);

            // prune stale entries (dead bullets, cleared portals) — once per frame
            if (Time.frameCount != lastPruneFrame)
            {
                lastPruneFrame = Time.frameCount;
                var stale = new List<MoveTransform>();
                foreach (var kv in bulletArmed)
                {
                    if (kv.Key == null || Time.time >= kv.Value.expire)
                    {
                        stale.Add(kv.Key);
                    }
                }
                foreach (var s in stale)
                {
                    bulletArmed.Remove(s);
                }
            }
        }

        private static int lastPruneFrame = -1;

        private static bool HasActivePairs()
        {
            foreach (var kv in portalPositions)
            {
                if (portalActive.TryGetValue(kv.Key, out bool[] active) && active[0] && active[1])
                {
                    return true;
                }
            }
            return false;
        }

        // closest-point-on-segment distance test: does the flight segment
        // prev->cur pass within touchRadius of the portal center? Equivalent
        // to Physics2D.RaycastAll-style swept collision (RayCastTrail's own
        // anti-tunneling recipe), just refactored to a disc.
        private static bool SegmentTouchesDisc(Vector3 prev, Vector3 cur, Vector3 center)
        {
            Vector3 seg = cur - prev;
            float lenSqr = seg.sqrMagnitude;
            if (lenSqr < 0.000001f)
            {
                return false; // no movement — positional fallback handles it
            }
            float t = Vector3.Dot(center - prev, seg) / lenSqr;
            t = Mathf.Clamp01(t); // clamp keeps portals behind the flight path dormant
            Vector3 closest = prev + seg * t;
            return (closest - center).sqrMagnitude <= touchRadius * touchRadius;
        }

        private static void TeleportBullet(MoveTransform mt, Vector3 dest)
        {
            // keep momentum: leave MoveTransform.velocity alone, offset exit forward
            // so the bullet doesn't overlap geometry right behind the ring.
            Vector3 exit = dest + mt.velocity.normalized * bulletExitOffset;
            exit.z = 0f;
            mt.transform.root.position = exit;
            // Critical: RayCastTrail raycasts lastPos -> current each frame; without
            // snapping lastPos the bullet "travels" the whole gap along the span ray
            // and explodes on any wall in between. MoveRay() is the public vanilla hook.
            var trail = mt.GetComponentInParent<RayCastTrail>();
            if (trail != null)
            {
                trail.MoveRay();
            }
            bulletArmed[mt] = (false, Time.time + 1f);
            UnityEngine.Debug.Log($"[DEER] Bullet teleported to {exit}, velocity {mt.velocity} (speed {mt.velocity.magnitude:F1})");
        }

        private static bool battleOngoing()
        {
            return GameManager.instance.battleOngoing && TimeHandler.timeScale > 0f;
        }



        private void SimulateOwner()
        {
            bool canPlace = player.data.isPlaying
                && !player.data.dead
                && GameManager.instance.battleOngoing
                && TimeHandler.timeScale > 0f;

            placeCooldownLeft = Mathf.Max(0f, placeCooldownLeft - TimeHandler.deltaTime);

            if (!canPlace)
            {
                return;
            }

            // edge-triggered presses; both keys share one personal cooldown.
            // Each key targets a FIXED slot: A/B are anchor-or-move, alternation is gone.
            if (placeCooldownLeft <= 0f)
            {
                if (Input.GetKeyDown(keyPortalA) || Input.GetKeyDown(keyPortalB))
                {
                    int slot = Input.GetKeyDown(keyPortalA) ? 0 : 1;
                    // Global ability-cooldown modifier (e.g. Quick Attack +20%):
                    // 2.5s base -> 3.0s with one Quick Attack.
                    placeCooldownLeft = AbilityCooldowns.Apply(player, placeCooldown);

                    Vector3 pos = player.transform.position;
                    pos.z = 0f;
                    UnityEngine.Debug.Log($"[DEER] Portal key {(slot == 0 ? keyPortalA.ToString() : keyPortalB.ToString())} -> slot {slot} at {pos} (cooldown {placeCooldown}s)");

                    UnboundLib.NetworkingManager.RPC(
                        typeof(PortalEffect),
                        nameof(RPC_PlacePortal),
                        player.playerID,
                        slot,
                        pos
                    );
                }
            }
        }

        [UnboundLib.Networking.UnboundRPC]
        private static void RPC_PlacePortal(int ownerPlayerID, int slot, Vector3 pos)
        {
            UnityEngine.Debug.Log($"[DEER] RPC_PlacePortal owner {ownerPlayerID} slot {slot} at {pos}");
            if (!portalPositions.TryGetValue(ownerPlayerID, out Vector3[] slots))
            {
                slots = new Vector3[2];
                portalPositions[ownerPlayerID] = slots;
            }
            if (!portalActive.TryGetValue(ownerPlayerID, out bool[] active))
            {
                active = new bool[2];
                portalActive[ownerPlayerID] = active;
            }
            slots[slot] = pos;
            active[slot] = true;
            bool pairComplete = active[0] && active[1];
            // Only render slots that were actually placed — creating the not-yet-placed
            // sibling produced a full-brightness ghost at Vector3.zero (screen middle).
            if (active[slot])
            {
                EnsureVisual(ownerPlayerID, slot, pos, pairComplete);
            }
            if (active[1 - slot])
            {
                EnsureVisual(ownerPlayerID, 1 - slot, slots[1 - slot], pairComplete);
            }
        }

        private void TouchCheck()
        {
            Player local = player;
            if (local == null || local.data == null || !local.data.view.IsMine)
            {
                return;
            }
            if (!portalPositions.TryGetValue(local.playerID, out Vector3[] slots) ||
                !portalActive.TryGetValue(local.playerID, out bool[] active))
            {
                return;
            }
            if (!(active[0] && active[1]))
            {
                return; // both needed to teleport
            }

            Vector3 myPos = local.transform.position;
            bool touching = false;
            for (int i = 0; i < 2; i++)
            {
                if ((myPos - slots[i]).sqrMagnitude <= touchRadius * touchRadius)
                {
                    touching = true;
                    if (armed && Time.time >= cooldownUntil)
                    {
                        Vector3 dest = slots[1 - i];
                        armed = false;
                        cooldownUntil = Time.time + teleportCooldown;
                        UnboundLib.NetworkingManager.RPC(
                            typeof(PortalEffect),
                            nameof(RPC_TouchTeleport),
                            local.playerID,
                            dest
                        );
                        UnityEngine.Debug.Log($"[DEER] Portal touch: player {local.data.name} teleported portal {i} -> sibling at {dest}");
                    }
                }
            }
            if (!touching)
            {
                armed = true; // re-arm only after physically leaving the portal
            }
        }

        [UnboundLib.Networking.UnboundRPC]
        private static void RPC_TouchTeleport(int playerID, Vector3 dest)
        {
            Player target = GetPlayerByID(playerID);
            if (target == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] RPC_TouchTeleport could not resolve player {playerID}");
                return;
            }

            // Vanilla teleport recipe (Teleport.cs, decompiled): raw position write,
            // kill velocity, brief wall collision ignore.
            target.transform.root.position = dest;
            target.GetComponentInParent<PlayerCollision>()?.IgnoreWallForFrames(2);
            target.data.sinceGrounded = 0f;

            // PlayerVelocity.velocity is an internal field — clear it via reflection.
            var playerVel = target.data.playerVel;
            if (playerVel != null)
            {
                FieldInfo velField = typeof(PlayerVelocity).GetField(
                    "velocity",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public
                );
                velField?.SetValue(playerVel, Vector2.zero);
            }
            UnityEngine.Debug.Log($"[DEER] RPC_TouchTeleport moved player {playerID} to {dest}");
        }

        private static Player GetPlayerByID(int playerID)
        {
            MethodInfo method = typeof(PlayerManager).GetMethod(
                "GetPlayerWithID",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );
            return method?.Invoke(PlayerManager.instance, new object[] { playerID }) as Player;
        }

        // ---------------------------------------------------------------
        // Client-side HUD (bottom-left): portal ability icon + cooldown.
        // Registered with the shared AbilityHUD row (see AbilityHud.cs) so it
        // lines up next to other ability icons instead of stacking on them.
        // Only the owning client renders it — the visible delegate gates on
        // IsMine, so other players never draw this icon.
        // ---------------------------------------------------------------
        private static Texture2D hudIconTex;

        private void Awake()
        {
            if (player == null)
            {
                player = GetComponent<Player>();
            }
            EnsureHudTextures();
            AbilityHUD.Register(this, HudVisible, HudDraw);
        }

        private static void EnsureHudTextures()
        {
            if (hudIconTex == null)
            {
                // filled circle, sharp edge — deliberately NOT the portal ring look
                hudIconTex = AbilityHUD.MakeCircleTexture(128, 0, 56);
            }
        }

        private bool HudVisible()
        {
            return player != null && player.data != null && player.data.view.IsMine && player.data.isPlaying;
        }

        private void HudDraw(Rect area)
        {
            bool ready = placeCooldownLeft <= 0f;
            string caption = ready ? $"{keyPortalA} / {keyPortalB}" : placeCooldownLeft.ToString("F1");
            AbilityHUD.DrawCircle(
                area,
                hudIconTex,
                new Color(0.20f, 0.85f, 0.35f), // ready = green
                new Color(0.90f, 0.25f, 0.20f), // on cooldown = red
                ready,
                caption
            );
        }

        // ---- visuals ----

        private static void EnsureVisual(int ownerPlayerID, int slot, Vector3 pos, bool pairComplete)
        {
            long key = ((long)ownerPlayerID << 1) | (long)slot;
            GameObject go;
            if (!portalVisuals.TryGetValue(key, out go) || go == null)
            {
                go = new GameObject($"DEER_Portal_{ownerPlayerID}_{slot}");
                go.transform.position = pos;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetRingSprite();
                sr.sortingOrder = 1;

                var disc = new GameObject("disc");
                disc.transform.SetParent(go.transform, false);
                var dsr = disc.AddComponent<SpriteRenderer>();
                dsr.sprite = GetDiscSprite();
                dsr.sortingOrder = 0;

                portalVisuals[key] = go;
            }

            // Re-resolve the skin on every call: under RoundsWithFriends the chosen
            // color lives in UnboundLib's extra-skins spectrum, not the vanilla bank —
            // reading GetPlayerSkinColors(playerID) always returned bank slot 0 (orange).
            Player owner = GetPlayerByID(ownerPlayerID);
            Color main = Color.white;
            Color back = new Color(1f, 1f, 1f, 0.25f);
            if (owner != null)
            {
                PlayerSkin skin = GetSkin(owner);
                if (skin != null)
                {
                    main = skin.color;
                    back = skin.backgroundColor;
                }
            }

            go.transform.position = pos;
            var rends = go.GetComponentsInChildren<SpriteRenderer>();
            var ringRend = rends.FirstOrDefault(r => r.sortingOrder == 1);
            var discRend = rends.FirstOrDefault(r => r.sortingOrder == 0);
            if (ringRend != null)
            {
                ringRend.color = main;
            }
            if (discRend != null)
            {
                discRend.color = new Color(back.r, back.g, back.b, 0.35f);
            }

            // dim until its sibling exists; full brightness once linked
            float alpha = pairComplete ? 1f : 0.4f;
            foreach (var r in rends)
            {
                Color rc = r.color;
                rc.a = alpha;
                r.color = rc;
            }
        }

        // Current color of a live player: UnboundLib colorID -> extra skins
        // (RoundsWithFriends uses these), falling back to the vanilla bank for
        // plain sandbox players.
        private static PlayerSkin GetSkin(Player player)
        {
            PlayerSkin skin = null;
            try
            {
                int colorID = player.colorID();
                skin = ExtraPlayerSkins.GetPlayerSkinColors(colorID);
            }
            catch (Exception)
            {
                // colorID not assigned yet (offline sandbox before UnboundLib wiring)
            }
            if (skin == null)
            {
                skin = PlayerSkinBank.GetPlayerSkinColors(player.playerID);
            }
            return skin;
        }

        private static bool portalActiveState(int ownerPlayerID, int slot)
        {
            return portalActive.TryGetValue(ownerPlayerID, out bool[] a) && a[slot];
        }

        // ---- round reset (works in vanilla via UnboundLib patches and in
        // RoundsWithFriends via its own game loop — both fire the same hooks) ----
        private static bool hooksRegistered;

        public static void RegisterRoundResetHooks()
        {
            if (hooksRegistered)
            {
                return;
            }
            hooksRegistered = true;
            // every small round (point / deathmatch round), every big round, and
            // every new game — the last also wipes visuals left over from a
            // finished match.
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, gm => ResetAll());
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, gm => ResetAll());
            GameModeManager.AddHook(GameModeHooks.HookGameStart, gm => ResetAll());
        }

        private static IEnumerator ResetAll()
        {
            UnityEngine.Debug.Log("[DEER] Round boundary — resetting all portals");
            // clear placement/teleport state for every player
            var ownerIDs = portalPositions.Keys.ToList();
            foreach (int id in ownerIDs)
            {
                RemovePortals(id);
            }
            // also drop any orphaned visuals whose owner already left
            foreach (var kv in portalVisuals)
            {
                if (kv.Value != null)
                {
                    Destroy(kv.Value);
                }
            }
            portalVisuals.Clear();
            portalPositions.Clear();
            portalActive.Clear();
            yield break;
        }

        private static void RemovePortals(int ownerPlayerID)
        {
            if (ownerPlayerID == -1)
            {
                foreach (var kv in portalVisuals)
                {
                    if (kv.Value != null)
                    {
                        Destroy(kv.Value);
                    }
                }
                portalVisuals.Clear();
                portalPositions.Clear();
                portalActive.Clear();
                return;
            }
            portalPositions.Remove(ownerPlayerID);
            portalActive.Remove(ownerPlayerID);
            RemoveVisual(ownerPlayerID, 0);
            RemoveVisual(ownerPlayerID, 1);
        }

        private static void RemoveVisual(int ownerPlayerID, int slot)
        {
            long key = ((long)ownerPlayerID << 1) | (long)slot;
            if (portalVisuals.TryGetValue(key, out GameObject go) && go != null)
            {
                Destroy(go);
            }
            portalVisuals.Remove(key);
        }

        private static Sprite GetRingSprite()
        {
            if (ringSprite == null)
            {
                ringSprite = PortalSprite(AbilityHUD.MakeCircleTexture(128, 44, 62));
            }
            return ringSprite;
        }

        private static Sprite GetDiscSprite()
        {
            if (discSprite == null)
            {
                discSprite = PortalSprite(AbilityHUD.MakeCircleTexture(128, 0, 40));
            }
            return discSprite;
        }

        // Sprite.Create wrapper: 64 ppu converts pixels to world units ≈ game
        // scale (matches the portal's ~1.9 world-unit diameter).
        private static Sprite PortalSprite(Texture2D tex)
        {
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 64f);
            sprite.name = "DEERPortalSprite";
            return sprite;
        }
    }
}
