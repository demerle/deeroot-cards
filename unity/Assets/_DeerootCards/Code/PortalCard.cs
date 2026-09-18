using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnboundLib;
using UnboundLib.Cards;
using ModdingUtils.Extensions;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Portals: the player's bullets place/move two linked teleporters.
    /// Bullet 1 spawns portal A, bullet 2 spawns portal B, bullet 3 moves A,
    /// bullet 4 moves B, and so on. Any player touching a portal is teleported
    /// to its sibling (goes through walls — vanilla teleport recipe).
    /// </summary>
    public class PortalCard : CustomCard
    {
        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.gameObject.GetOrAddComponent<PortalEffect>();
            effect.SetPlayer(player);
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
            return "Every time you block, two linked teleporters appear on and around you.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "On block",
                    amount = "Place/re-arm two linked teleporters at your position",
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
    }

    /// <summary>
    /// Attached to a player. On each block, alternately spawns/moves that
    /// player's two portals (A then B) at the player's current position, and
    /// teleports the local player when they touch one of them.
    /// </summary>
    public class PortalEffect : MonoBehaviour
    {
        private Player player;
        private Block block;

        // ---- shared state registry (replicated identically on every client) ----
        // ownerPlayerID -> [portalA, portalB] positions
        private static Dictionary<int, Vector3[]> portalPositions = new Dictionary<int, Vector3[]>();
        private static Dictionary<int, bool[]> portalActive = new Dictionary<int, bool[]>();

        // ---- runtime visuals keyed the same way ----
        private static Dictionary<long, GameObject> portalVisuals = new Dictionary<long, GameObject>();
        private static Sprite ringSprite;
        private static Sprite discSprite;

        // ---- per-player (per-effect-instance) state ----
        private int blockCounter;
        private float cooldownUntil;
        private bool armed = true;
        private const float touchRadius = 0.75f;

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
            block = player.GetComponent<Block>();
            block.BlockAction += OnBlock;
            UnityEngine.Debug.Log($"[DEER] PortalEffect started on player {player.data.name}");
        }

        private void OnDestroy()
        {
            if (block != null)
            {
                block.BlockAction -= OnBlock;
            }
            RemovePortals(player != null ? player.playerID : -1);
        }

        private void OnBlock(BlockTrigger.BlockTriggerType triggerType)
        {
            // Only the owning client decides portal placement; it broadcasts.
            if (!player.data.view.IsMine)
            {
                return;
            }

            int slot = blockCounter % 2;
            blockCounter++;

            Vector3 pos = player.transform.position;
            pos.z = 0f;
            UnityEngine.Debug.Log($"[DEER] Portal block #{blockCounter} -> slot {slot} at {pos}");

            UnboundLib.NetworkingManager.RPC(
                typeof(PortalEffect),
                nameof(RPC_PlacePortal),
                player.playerID,
                slot,
                pos
            );
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
            EnsureVisual(ownerPlayerID, slot, pos, active[1 - slot]);
            EnsureVisual(ownerPlayerID, 1 - slot, slots[1 - slot], active[slot]);
        }

        private void Update()
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

                var skin = PlayerSkinBank.GetPlayerSkinColors(ownerPlayerID);
                Color main = skin != null ? skin.color : Color.white;
                Color back = skin != null ? skin.backgroundColor : new Color(1f, 1f, 1f, 0.25f);
                sr.color = main;
                dsr.color = new Color(back.r, back.g, back.b, 0.35f);

                portalVisuals[key] = go;
            }
            else
            {
                go.transform.position = pos;
            }

            // dim until its sibling exists; full brightness once linked
            float alpha = pairComplete ? 1f : 0.4f;
            foreach (var r in go.GetComponentsInChildren<SpriteRenderer>())
            {
                Color rc = r.color;
                rc.a = alpha;
                r.color = rc;
            }
        }

        private static bool portalActiveState(int ownerPlayerID, int slot)
        {
            return portalActive.TryGetValue(ownerPlayerID, out bool[] a) && a[slot];
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
                ringSprite = MakeCircleSprite(128, 44, 62, 64f);
            }
            return ringSprite;
        }

        private static Sprite GetDiscSprite()
        {
            if (discSprite == null)
            {
                discSprite = MakeCircleSprite(128, 0, 40, 64f);
            }
            return discSprite;
        }

        // Soft-edged anti-aliased circle/ring generated at runtime.
        // innerR/outerR are in pixels; ppm converts pixels to world units
        // (144 ppu ≈ game scale, matching the portal's ~1.9 world-unit diameter).
        private static Sprite MakeCircleSprite(int size, float innerR, float outerR, float spritesPerUnit)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            float center = size / 2f;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha;
                    if (r > outerR)
                    {
                        alpha = 0f;
                    }
                    else if (r >= innerR)
                    {
                        float soft = Mathf.Min((r - innerR) / 3f, (outerR - r) / 3f);
                        alpha = Mathf.Clamp01(soft);
                    }
                    else
                    {
                        alpha = 1f;
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), spritesPerUnit);
            sprite.name = "DEERPortalRing";
            return sprite;
        }
    }
}
