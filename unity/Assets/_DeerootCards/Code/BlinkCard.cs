using System.Reflection;
using UnityEngine;
using UnboundLib;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Blink: on block, teleport the player to their cursor. Goes through walls.
    /// </summary>
    public class BlinkCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // per-player unique: won't be re-offered once this player holds it
            cardInfo.allowMultiple = false;
            // downside: flat 3s added to block cooldown — decompiled Block:
            // effective cooldown = (cooldown + cdAdd) * cdMultiplier, base 4s → 7s
            block.cdAdd = 3f;
            UnityEngine.Debug.Log($"[DEER] BlinkCard SetupCard: setting block.cdAdd to {block.cdAdd}");
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.gameObject.GetOrAddComponent<BlinkEffect>();
            effect.SetPlayer(player);
            UnityEngine.Debug.Log($"[DEER] BlinkCard added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.GetComponent<BlinkEffect>();
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        protected override string GetTitle()
        {
            return "Blink";
        }

        protected override string GetDescription()
        {
            return "Be where the danger isn't.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "On block",
                    amount = "Teleport to cursor",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Block cooldown",
                    amount = "+3s",
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
    /// Attached to a player. On block, teleports that player to the owner's cursor
    /// position, synced to all clients via an UnboundLib NetworkingManager RPC.
    /// </summary>
    public class BlinkEffect : MonoBehaviour
    {
        private Player player;
        private Block block;

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
            UnityEngine.Debug.Log($"[DEER] BlinkEffect started on player {player.data.name}");
        }

        private void OnDestroy()
        {
            if (block != null)
            {
                block.BlockAction -= OnBlock;
            }
        }

        private void OnBlock(BlockTrigger.BlockTriggerType triggerType)
        {
            // Only the owning client knows the local cursor position; it computes and broadcasts.
            if (!player.data.view.IsMine)
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
            {
                cam = UnityEngine.Object.FindObjectOfType<Camera>();
            }
            if (cam == null)
            {
                return;
            }

            // ScreenToWorldPoint: z=distance from camera; use camera distance to the z=0 aim plane.
            Vector3 screenPos = Input.mousePosition;
            Vector3 settle = new Vector3(screenPos.x, screenPos.y, 0f - cam.transform.position.z);
            Vector3 target = cam.ScreenToWorldPoint(settle);
            target = new Vector3(target.x, target.y, 0f);

            UnityEngine.Debug.Log($"[DEER] Blink block triggered, teleporting player {player.data.name} to {target}");

            // ReceiverGroup.All so the teleport runs on every client (including the
            // sender in online play), and locally in offline mode via NetworkingManager.
            // NOTE: target method MUST be decorated with [UnboundRPC].
            UnboundLib.NetworkingManager.RPC(
                typeof(BlinkEffect),
                nameof(RPC_Teleport),
                player.playerID,
                target
            );
        }

        [UnboundLib.Networking.UnboundRPC]
        private static void RPC_Teleport(int playerID, Vector3 pos)
        {
            UnityEngine.Debug.Log($"[DEER] RPC_Teleport received for playerID {playerID} at {pos}");
            Player target = GetPlayerByID(playerID);
            if (target == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] RPC_Teleport could not resolve player {playerID}");
                return;
            }

            // Same teleport mechanics as the vanilla Teleport card: raw root position
            // write + kill velocity + brief wall collision ignore.
            target.transform.root.position = pos;
            target.GetComponentInParent<PlayerCollision>()?.IgnoreWallForFrames(2);
            target.data.sinceGrounded = 0f;

            // Kill any leftover momentum so the player doesn't fly off after blinking.
            // PlayerVelocity.velocity is an internal field — set it via reflection.
            var playerVel = target.data.playerVel;
            if (playerVel != null)
            {
                FieldInfo velField = typeof(PlayerVelocity).GetField(
                    "velocity",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );
                velField?.SetValue(playerVel, Vector2.zero);
            }
        }

        private static Player GetPlayerByID(int playerID)
        {
            // PlayerManager.GetPlayerWithID is private — reflect it, same as other mods do.
            MethodInfo method = typeof(PlayerManager).GetMethod(
                "GetPlayerWithID",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            return method?.Invoke(PlayerManager.instance, new object[] { playerID }) as Player;
        }
    }
}
