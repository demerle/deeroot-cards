using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.Extensions;
using UnboundLib.GameModes;
using UnboundLib.Utils;
using UnityEngine;
using ModdingUtils.Extensions;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Amp Wall: pressing [X] deploys a short energy wall ~1 unit in front of
    /// the player, perpendicular to their aim direction at cast time (Baptiste
    /// window style). The wall is fixed once placed and lasts a few seconds.
    /// Only the caster's OWN bullets that cross the wall are amplified:
    /// damage ×2 and projectile speed ×2, once per crossing. Enemy bullets
    /// (and teammates' bullets) pass through untouched.
    ///
    /// Architecture (mirrors PortalCard, both verified in-game):
    /// - Placement: owner client detects the keypress, broadcasts an UnboundRPC
    ///   with (ownerPlayerID, center, direction); every client stores the wall
    ///   in the same static registry and draws its own local visual.
    /// - Amplification: a Harmony postfix on MoveTransform.Update fires on the
    ///   exact frame each projectile integrates its move (incl. its last frame
    ///   before a wall hit). The flight segment is reconstructed from the
    ///   current position (pos - velocity*dt*multiplier — the exact
    ///   MoveTransform integration), immune to pooled/reused bullets, and
    ///   tested against the wall segment. Ownership gate is the PUBLIC
    ///   ProjectileHit.ownPlayer field (Gun.ApplyPlayerStuff sets it on every
    ///   client) compared by playerID — nobody else's bullets are touched.
    /// - No custom networking beyond the placement RPC: the registry is
    ///   replicated via the RPC, and every client amplifies the same bullets
    ///   on its own local sim (same reasoning as the Portal bullet teleport).
    /// </summary>
    public class AmpWallCard : CustomCard
    {
        public const string CardName = "Amp Wall";

        // ---- tuning constants (edit here to retune) ----
        internal const float BaseCooldown = 10f;   // personal ability cooldown (s)
        internal const float WallDuration = 6f;    // wall lifetime (s)
        internal const float WallLength = 4.5f;    // wall span (world units)
        internal const float WallOffset = 1.5f;    // distance in front of the caster
        internal const float DamageMultiplier = 2f;
        internal const float SpeedMultiplier = 2f;

        // applies the MoveTransform.Update postfix once, at card registration
        private static bool harmonyApplied;

        public static void Init()
        {
            if (harmonyApplied)
            {
                return;
            }
            harmonyApplied = true;
            new Harmony("com.deeroot.cards.ampwall").PatchAll(typeof(AmpWallEffect.AmpWallBulletPatch));
        }

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // per-player unique: won't be re-offered once this player holds it
            cardInfo.allowMultiple = false;
            // No stat downside by design — the cooldown is the cost.
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.gameObject.GetOrAddComponent<AmpWallEffect>();
            effect.SetPlayer(player);
            UnityEngine.Debug.Log($"[DEER] AmpWall added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.GetComponent<AmpWallEffect>();
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
            return "Ability Card: Deploy an amplification wall — bullets you fire through it deal double damage and fly twice as fast.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Deploy wall",
                    amount = "X",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = true,
                    stat = "Your bullets through wall",
                    amount = "×2 dmg, ×2 speed",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Ability Cooldown",
                    amount = "10 seconds",
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
            return CardThemeColor.CardThemeColorType.FirepowerYellow;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }

    /// <summary>
    /// Attached to a player with the card. Owner client handles the keybind
    /// and broadcasts placement; every client (owner included) prunes expired
    /// walls and runs the bullet amplification against the shared registry.
    /// </summary>
    public class AmpWallEffect : MonoBehaviour
    {
        // ---- shared replicated state (identical on every client via RPC) ----
        private class WallData
        {
            public Vector3 center;   // wall midpoint, z = 0
            public Vector2 dir;      // unit vector ALONG the wall (perp of aim at cast)
            public float expire;     // Time.time at which the wall despawns
        }

        private static readonly Dictionary<int, WallData> walls = new Dictionary<int, WallData>();
        private static readonly Dictionary<int, GameObject> visuals = new Dictionary<int, GameObject>();

        // bullets already amplified by a wall, keyed with a memory window so a
        // reflected/bounced bullet can be amplified again on a LATER pass
        private static readonly Dictionary<MoveTransform, float> boosted = new Dictionary<MoveTransform, float>();
        private const float boostMemorySeconds = 3f;

        // tolerance: how far off the exact wall segment a crossing still counts
        private const float wallHalfThickness = 0.15f;
        // point-blank fallback: a bullet that SPAWNS overlapping the wall gets
        // amplified without needing a frame where it visibly crosses
        private const float pointBlankRadius = 0.3f;

        private static Sprite barSprite;

        private Player player;
        private KeyCode triggerKey = KeyCode.X;

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
            RegisterRoundResetHooks();
            UnityEngine.Debug.Log($"[DEER] AmpWallEffect started on player {player.data.name}");
        }

        private void OnDestroy()
        {
            AbilityHUD.Unregister(this);
        }

        // Key table is per-CLIENT (same recipe as PortalEffect/ShamblesEffect):
        // online every local player uses X; only on true local split-screen
        // (2+ IsMine players on one machine) does the second local player get B.
        private void ResolveKeys()
        {
            triggerKey = KeyCode.X;
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
                triggerKey = KeyCode.B;
            }
            UnityEngine.Debug.Log($"[DEER] AmpWall key resolved: {triggerKey} (local player index {localIndex}/{localCount})");
        }

        private void Update()
        {
            if (player == null || player.data == null)
            {
                return;
            }

            // every client prunes expired walls + visuals (idempotent, cheap)
            PruneExpiredWalls();

            if (player.data.view.IsMine)
            {
                SimulateOwner();
            }
        }

        private void SimulateOwner()
        {
            bool canTrigger = player.data.isPlaying
                && !player.data.dead
                && GameManager.instance.battleOngoing
                && TimeHandler.timeScale > 0f;

            cooldownLeft = Mathf.Max(0f, cooldownLeft - TimeHandler.deltaTime);

            if (!canTrigger || cooldownLeft > 0f || !Input.GetKeyDown(triggerKey))
            {
                return;
            }

            // aim direction at cast time (GeneralInput keeps it normalized)
            Vector3 aim = player.data.input.aimDirection;
            if (aim.sqrMagnitude < 0.0001f)
            {
                aim = Vector3.right;
            }
            aim.z = 0f;
            aim.Normalize();

            // wall sits ~1 unit in front of the player, perpendicular to aim
            Vector3 center = player.transform.position + aim * AmpWallCard.WallOffset;
            center.z = 0f;
            Vector2 along = new Vector2(-aim.y, aim.x); // perpendicular of the aim

            // cooldown only burns on a cast; Quick Attack scales it
            cooldownLeft = AbilityCooldowns.Apply(player, AmpWallCard.BaseCooldown);

            UnityEngine.Debug.Log($"[DEER] AmpWall trigger: player {player.playerID} wall at {center} along {along} (cooldown {AmpWallCard.BaseCooldown}s)");

            NetworkingManager.RPC(
                typeof(AmpWallEffect),
                nameof(RPC_PlaceAmpWall),
                player.playerID,
                center,
                along
            );
        }

        [UnboundLib.Networking.UnboundRPC]
        private static void RPC_PlaceAmpWall(int ownerPlayerID, Vector3 center, Vector2 along)
        {
            UnityEngine.Debug.Log($"[DEER] RPC_PlaceAmpWall owner {ownerPlayerID} at {center} along {along}");
            var wall = new WallData
            {
                center = center,
                dir = along.sqrMagnitude > 0.0001f ? along.normalized : Vector2.up,
                expire = Time.time + AmpWallCard.WallDuration
            };
            walls[ownerPlayerID] = wall;
            EnsureVisual(ownerPlayerID, wall);
        }

        // ---------------------------------------------------------------
        // Bullet amplification — Harmony postfix on MoveTransform.Update.
        // Fires on the exact frame each projectile integrates its move
        // (including its last frame before a wall hit), for every projectile
        // on every client (Portal-verified pattern).
        // ---------------------------------------------------------------
        [HarmonyPatch(typeof(MoveTransform), "Update")]
        internal static class AmpWallBulletPatch
        {
            [HarmonyPostfix]
            private static void AfterBulletMove(MoveTransform __instance)
            {
                CheckBullet(__instance);
            }
        }

        // called from the MoveTransform.Update postfix, once per bullet per frame
        private static void CheckBullet(MoveTransform mt)
        {
            if (walls.Count == 0)
            {
                return;
            }
            if (!battleOngoing())
            {
                return;
            }
            // only true projectiles: a bullet carries a ProjectileHit; generic
            // MoveTransform users (UI movers etc.) are skipped (portal recipe).
            var ph = mt.GetComponent<ProjectileHit>();
            if (ph == null)
            {
                return;
            }
            // OWNERSHIP GATE: only bullets fired by the wall's owner get
            // amplified. ownPlayer is public and set by Gun.ApplyPlayerStuff on
            // every client. Nobody else's bullets are touched.
            if (ph.ownPlayer == null)
            {
                return;
            }
            if (!walls.TryGetValue(ph.ownPlayer.playerID, out WallData wall))
            {
                return;
            }
            if (Time.time >= wall.expire)
            {
                return;
            }
            if (IsBoosted(mt))
            {
                return;
            }

            // self-contained flight segment: MoveTransform integrates
            // `velocity * deltaTime * multiplier` each frame, so working
            // backwards from the current position reconstructs this frame's
            // flight path without cross-frame bookkeeping (portal recipe).
            float dt = TimeHandler.deltaTime * mt.multiplier;
            Vector3 prev = mt.transform.position - mt.velocity * dt;
            Vector3 cur = mt.transform.position;

            if (SegmentCrossesWall(prev, cur, wall) || PointNearWall(cur, wall, pointBlankRadius))
            {
                Boost(mt, ph);
            }
        }

        private static void Boost(MoveTransform mt, ProjectileHit ph)
        {
            // Bullet visuals + detection radius are spawn-time caches:
            // RayCastTrail.Start caches size from damage, SetScaleFromSizeAndExtraSize.Start
            // scales the sprite from that size once. Damage changes after spawn are
            // invisible until we rescale them here (Full Counter recipe, which
            // mirrors vanilla RayHitReflect's rescale idiom).
            float oldTrailSize = 0f;
            RayCastTrail trail = mt.GetComponent<RayCastTrail>();
            if (trail == null)
            {
                trail = mt.GetComponentInParent<RayCastTrail>();
            }
            if (trail != null)
            {
                oldTrailSize = TrailSizeFromDamage(ph.damage, trail.extraSize);
            }

            float before = ph.damage;
            ph.damage *= AmpWallCard.DamageMultiplier;
            ph.shake *= AmpWallCard.DamageMultiplier;
            mt.velocity *= AmpWallCard.SpeedMultiplier;
            boosted[mt] = Time.time + boostMemorySeconds;

            float newTrailSize = 0f;
            if (trail != null)
            {
                // Visible sprite: ratio-rescale by the same factor spawn scaling
                // used (the transform is only ever scaled once, at Start).
                SetScaleFromSizeAndExtraSize spriteScaler =
                    mt.GetComponentInChildren<SetScaleFromSizeAndExtraSize>(true);
                if (spriteScaler != null && oldTrailSize > 0.0001f)
                {
                    newTrailSize = TrailSizeFromDamage(ph.damage, trail.extraSize);
                    float ratio = newTrailSize / oldTrailSize;
                    spriteScaler.transform.localScale *= ratio;
                }

                // Detection radius: exact same formula as RayCastTrail.Start.
                newTrailSize = TrailSizeFromDamage(ph.damage, trail.extraSize);
                trail.size = newTrailSize;

                // Trail width/thickness: vanilla's own rescale idiom.
                GetComponentInDisabled<ScaleTrailFromDamage>(mt.gameObject)?.Rescale();
            }

            UnityEngine.Debug.Log($"[DEER] AmpWall: bullet of player {ph.ownPlayer.playerID} amplified at {mt.transform.position} — damage {before:F0} -> {ph.damage:F0}, speed {mt.velocity.magnitude:F1}{(trail != null ? $", size {oldTrailSize:F2} -> {newTrailSize:F2}" : "")}");
        }

        // Exact formula from RayCastTrail.Start (verified against decompile).
        private static float TrailSizeFromDamage(float damage, float extraSize)
        {
            return Mathf.Clamp(Mathf.Pow(damage, 0.85f) / 400f, 0f, 100f) + 0.3f + extraSize;
        }

        // GetComponentInChildren(false) skips inactive children, which pooled
        // recycled bullets can be; this resolves it regardless. (Full Counter.)
        private static T GetComponentInDisabled<T>(GameObject root) where T : Component
        {
            T direct = root.GetComponent<T>();
            if (direct != null)
            {
                return direct;
            }
            T[] all = root.GetComponentsInChildren<T>(true);
            if (all == null || all.Length == 0)
            {
                return null;
            }
            // Mirror ScaleTrailFromDamage's own lookup: most-relevant descendant.
            Transform rootT = root.transform;
            T best = null;
            int bestDepth = int.MaxValue;
            foreach (T comp in all)
            {
                int depth = 0;
                Transform t = comp.transform.parent;
                while (t != null && t != rootT)
                {
                    depth++;
                    t = t.parent;
                }
                if (comp.transform.parent == null || bestDepth > depth)
                {
                    best = comp;
                    bestDepth = depth;
                }
            }
            return best;
        }

        private static bool IsBoosted(MoveTransform mt)
        {
            // prune stale entries (dead bullets, expired memory) once per frame
            if (Time.frameCount != lastBoostPruneFrame)
            {
                lastBoostPruneFrame = Time.frameCount;
                var stale = new List<MoveTransform>();
                foreach (var kv in boosted)
                {
                    if (kv.Key == null || Time.time >= kv.Value)
                    {
                        stale.Add(kv.Key);
                    }
                }
                foreach (var s in stale)
                {
                    boosted.Remove(s);
                }
            }
            if (boosted.TryGetValue(mt, out float until))
            {
                return Time.time < until;
            }
            return false;
        }

        private static int lastBoostPruneFrame = -1;

        // parametric segment-vs-segment intersection with a small margin so
        // edge grazes count; parallel shots (flying along the wall) never cross
        private static bool SegmentCrossesWall(Vector3 prev, Vector3 cur, WallData wall)
        {
            Vector2 p0 = prev;
            Vector2 p1 = cur;
            Vector2 q0 = (Vector2)wall.center - wall.dir * (AmpWallCard.WallLength * 0.5f);
            Vector2 q1 = (Vector2)wall.center + wall.dir * (AmpWallCard.WallLength * 0.5f);

            Vector2 d = p1 - p0;
            Vector2 e = q1 - q0;
            float denom = d.x * e.y - d.y * e.x;
            if (Mathf.Abs(denom) < 0.0001f)
            {
                return false; // parallel — flying along the wall
            }
            Vector2 diff = q0 - p0;
            float t = (diff.x * e.y - diff.y * e.x) / denom;   // along the bullet segment
            float u = (diff.x * d.y - diff.y * d.x) / denom;   // along the wall segment
            const float uMargin = 0.05f; // graze tolerance at the wall tips
            return t >= 0f && t <= 1f && u >= -uMargin && u <= 1f + uMargin;
        }

        // distance from a point to the wall segment (point-blank fallback)
        private static bool PointNearWall(Vector3 point, WallData wall, float radius)
        {
            Vector2 p = point;
            Vector2 q0 = (Vector2)wall.center - wall.dir * (AmpWallCard.WallLength * 0.5f);
            Vector2 q1 = (Vector2)wall.center + wall.dir * (AmpWallCard.WallLength * 0.5f);
            Vector2 seg = q1 - q0;
            float lenSqr = seg.sqrMagnitude;
            if (lenSqr < 0.000001f)
            {
                return (p - q0).sqrMagnitude <= radius * radius;
            }
            float t = Mathf.Clamp01(Vector2.Dot(p - q0, seg) / lenSqr);
            Vector2 closest = q0 + seg * t;
            return (p - closest).sqrMagnitude <= radius * radius;
        }

        private static bool battleOngoing()
        {
            return GameManager.instance.battleOngoing && TimeHandler.timeScale > 0f;
        }

        // ---- wall lifetime ----

        private static void PruneExpiredWalls()
        {
            List<int> expired = null;
            foreach (var kv in walls)
            {
                if (Time.time >= kv.Value.expire)
                {
                    if (expired == null)
                    {
                        expired = new List<int>();
                    }
                    expired.Add(kv.Key);
                }
            }
            if (expired == null)
            {
                return;
            }
            foreach (int id in expired)
            {
                walls.Remove(id);
                RemoveVisual(id);
            }
        }

        // ---- visuals ----

        private static void EnsureVisual(int ownerPlayerID, WallData wall)
        {
            GameObject go;
            if (!visuals.TryGetValue(ownerPlayerID, out go) || go == null)
            {
                go = new GameObject($"DEER_AmpWall_{ownerPlayerID}");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetBarSprite();
                sr.sortingOrder = 1;
                visuals[ownerPlayerID] = go;
            }

            go.transform.position = wall.center;
            float angle = Mathf.Atan2(wall.dir.y, wall.dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // color = the owner's current skin color (RWF extra-skins aware —
            // same resolution the Portal card uses)
            Color main = Color.white;
            Player owner = GetPlayerByID(ownerPlayerID);
            if (owner != null)
            {
                PlayerSkin skin = GetSkin(owner);
                if (skin != null)
                {
                    main = skin.color;
                }
            }
            main.a = 0.85f;
            foreach (var r in go.GetComponentsInChildren<SpriteRenderer>())
            {
                r.color = main;
            }
        }

        private static void RemoveVisual(int ownerPlayerID)
        {
            if (visuals.TryGetValue(ownerPlayerID, out GameObject go) && go != null)
            {
                Destroy(go);
            }
            visuals.Remove(ownerPlayerID);
        }

        // horizontal capsule bar texture; the sprite's pixelsPerUnit is chosen
        // so the rendered world length equals AmpWallCard.WallLength exactly
        private static Sprite GetBarSprite()
        {
            if (barSprite != null)
            {
                return barSprite;
            }
            int width = 256;
            int height = 24;
            float radius = height * 0.5f - 1f;
            float capCenter = width * 0.5f - radius;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float px = x + 0.5f - width * 0.5f;
                    float py = y + 0.5f - height * 0.5f;
                    float clampedX = Mathf.Clamp(px, -capCenter, capCenter);
                    float dist = Mathf.Sqrt((px - clampedX) * (px - clampedX) + py * py);
                    float alpha;
                    if (dist <= radius - 2f)
                    {
                        alpha = 1f;
                    }
                    else if (dist >= radius)
                    {
                        alpha = 0f;
                    }
                    else
                    {
                        alpha = Mathf.Clamp01((radius - dist) / 2f);
                    }
                    pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            barSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width / AmpWallCard.WallLength);
            barSprite.name = "DEERAmpWallSprite";
            return barSprite;
        }

        // Current color of a live player: UnboundLib colorID -> extra skins
        // (RoundsWithFriends uses these), falling back to the vanilla bank for
        // plain sandbox players. (Portal-card recipe.)
        private static PlayerSkin GetSkin(Player player)
        {
            PlayerSkin skin = null;
            try
            {
                int colorID = player.colorID();
                skin = ExtraPlayerSkins.GetPlayerSkinColors(colorID);
            }
            catch (System.Exception)
            {
                // colorID not assigned yet (offline sandbox before UnboundLib wiring)
            }
            if (skin == null)
            {
                skin = PlayerSkinBank.GetPlayerSkinColors(player.playerID);
            }
            return skin;
        }

        private static Player GetPlayerByID(int playerID)
        {
            MethodInfo method = typeof(PlayerManager).GetMethod(
                "GetPlayerWithID",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            return method?.Invoke(PlayerManager.instance, new object[] { playerID }) as Player;
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

        private static System.Collections.IEnumerator ResetAll()
        {
            UnityEngine.Debug.Log("[DEER] Round boundary — resetting all amp walls");
            var ownerIDs = new List<int>(walls.Keys);
            foreach (int id in ownerIDs)
            {
                RemoveVisual(id);
            }
            walls.Clear();
            visuals.Clear();
            boosted.Clear();
            yield break;
        }

        // ---------------------------------------------------------------
        // Client-side HUD (bottom-left icon), Portal/Shambles recipe:
        // register in Awake, unregister in OnDestroy, draw ready/cooldown
        // state. Only the owning client renders it (HudVisible gates on IsMine).
        // ---------------------------------------------------------------
        private static Texture2D hudIconTex;
        private float cooldownLeft;

        private void Awake()
        {
            if (player == null)
            {
                player = GetComponent<Player>();
            }
            EnsureHudTexture();
            AbilityHUD.Register(this, HudVisible, HudDraw);
        }

        private static void EnsureHudTexture()
        {
            if (hudIconTex == null)
            {
                // filled circle — shared icon presentation with the other abilities
                hudIconTex = AbilityHUD.MakeCircleTexture(128, 0, 56);
            }
        }

        private bool HudVisible()
        {
            return player != null && player.data != null && player.data.view.IsMine && player.data.isPlaying;
        }

        private void HudDraw(Rect area)
        {
            bool ready = cooldownLeft <= 0f;
            string caption = ready ? triggerKey.ToString() : cooldownLeft.ToString("F1");
            AbilityHUD.DrawCircle(
                area,
                hudIconTex,
                new Color(0.98f, 0.85f, 0.10f), // ready = firepower yellow
                new Color(0.30f, 0.30f, 0.34f), // on cooldown = dark gray
                ready,
                caption
            );
        }
    }
}
