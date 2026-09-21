using System.Collections;
using System.Reflection;
using UnityEngine;
using UnboundLib;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// DIVE: on block, launch the player forward — Shield Charge's movement, doubled
    /// distance (0.4s vs vanilla 0.2s), with no damage, no knockback and no second block.
    /// </summary>
    public class DiveCard : CustomCard
    {
        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // Same downside as vanilla Shield Charge (extracted prefab: cdAdd 0.25).
            block.cdAdd = 0.25f;
            UnityEngine.Debug.Log($"[DEER] DiveCard SetupCard: setting block.cdAdd to {block.cdAdd}");
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.gameObject.GetOrAddComponent<DiveEffect>();
            effect.SetPlayer(player);
            UnityEngine.Debug.Log($"[DEER] DiveCard added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            var effect = player.GetComponent<DiveEffect>();
            if (effect != null)
            {
                Destroy(effect);
            }
        }

        protected override string GetTitle()
        {
            return "DIVE";
        }

        protected override string GetDescription()
        {
            return "Launch yourself at the enemy on block (double the shield charge distance).";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = false,
                    stat = "Block cooldown",
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
            return CardThemeColor.CardThemeColorType.DefensiveBlue;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }

    /// <summary>
    /// Attached to a player. Movement-only clone of vanilla ShieldCharge's DoCharge:
    /// per-FixedUpdate TakeForce with the same force curve + drag, sinceGrounded held
    /// at 0 (anti-gravity). Vanilla's second block (RPCA_DoBlock at charge end) and the
    /// player-collision damage path (collideWithPlayerAction / ChildRPC) are deliberately
    /// absent — this card is pure movement.
    ///
    /// Sync model = vanilla ShieldCharge: Block.RPCA_DoBlock is a Photon RpcTarget.All
    /// RPC, so SuperFirstBlockAction fires on every client and each client applies the
    /// same force loop to its local sim of that player. No mod RPC needed.
    /// </summary>
    public class DiveEffect : MonoBehaviour
    {
        // Vanilla A_ShieldCharge values (unity/Assets/GameObject/A_ShieldCharge.prefab):
        // force 1700000, drag 300000, time 0.2. DIVE = same speed, 2× duration → 2× distance.
        private const float BaseForce = 1700000f;
        private const float Drag = 300000f;
        private const float BaseDuration = 0.4f; // vanilla 0.2 × 2
        private const float ForcePerExtraStack = 0.1f;

        // Vanilla forceCurve (4 keys, from the extracted prefab).
        private static readonly AnimationCurve ForceCurve = new AnimationCurve(
            new Keyframe(0f, 0.0053100586f, -16.68966f, -16.68966f),
            new Keyframe(0.21951094f, 0.21818449f, 14.148807f, 14.148807f),
            new Keyframe(0.8699484f, 0.049984276f, -9.317379f, -9.317379f),
            new Keyframe(1f, 0.0053100586f, -0.12159106f, -0.12159106f)
        );

        // PlayerVelocity.velocity is internal — cache the reflection FieldInfo once.
        private static readonly FieldInfo VelocityField = typeof(PlayerVelocity).GetField(
            "velocity",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );

        private CharacterData data;
        private Block block;

        private int stacks = 1;
        private bool charging;
        private bool subscribed;

        public void SetPlayer(Player player)
        {
            // Kept for parity with BlinkEffect; Start resolves everything from the hierarchy.
        }

        private void Awake()
        {
            // Stack merge (replaces vanilla's AttackLevel duplicate-merge): a second DIVE
            // copy bumps the existing effect's stacks and destroys itself. Works whether
            // the sibling already started or was added in the same frame.
            var effects = GetComponents<DiveEffect>();
            foreach (var other in effects)
            {
                if (other != this && other != null)
                {
                    other.AddStack();
                    UnityEngine.Debug.Log($"[DEER] DiveEffect merged into existing effect, stacks now {other.stacks}");
                    Destroy(this);
                    return;
                }
            }
        }

        private void Start()
        {
            data = GetComponentInParent<CharacterData>();
            block = GetComponentInParent<Block>();
            if (data == null || block == null)
            {
                UnityEngine.Debug.LogWarning("[DEER] DiveEffect missing CharacterData/Block in parents — disabling");
                enabled = false;
                return;
            }
            // Guard matches vanilla DoBlock: ignore the ShieldCharge trigger type so the
            // effect never re-triggers off a ShieldCharge-type block (keeps it compatible
            // alongside vanilla Shield Charge too).
            block.SuperFirstBlockAction += OnSuperFirstBlock;
            subscribed = true;
            UnityEngine.Debug.Log($"[DEER] DiveEffect started on {data.gameObject.name} (stacks {stacks})");
        }

        private void OnDestroy()
        {
            if (subscribed && block != null)
            {
                block.SuperFirstBlockAction -= OnSuperFirstBlock;
            }
        }

        public void AddStack()
        {
            stacks++;
        }

        private float Force()
        {
            return BaseForce * (1f + ForcePerExtraStack * (stacks - 1));
        }

        private void OnSuperFirstBlock(BlockTrigger.BlockTriggerType triggerType)
        {
            if (triggerType == BlockTrigger.BlockTriggerType.ShieldCharge)
            {
                return;
            }
            if (charging)
            {
                return;
            }
            StartCoroutine(DoCharge());
        }

        private IEnumerator DoCharge()
        {
            charging = true;
            UnityEngine.Debug.Log($"[DEER] DiveEffect charging, dir {data.aimDirection}, stacks {stacks}, force {Force()}");
            // Vanilla reads aimDirection at charge start (the Empower dir quirk is a
            // ShieldCharge-specific nuance we skip — DIVE always dives toward aim).
            Vector3 dir = ((Vector2)data.aimDirection).normalized;
            float usedTime = BaseDuration;
            float c = 0f;
            while (c < 1f)
            {
                c += Time.fixedDeltaTime / usedTime;
                // Same force loop as ShieldCharge.DoCharge: curve-shaped forward force
                // (mass-ignored, applied through block) + velocity drag each fixed step.
                data.healthHandler.TakeForce(dir * ForceCurve.Evaluate(c) * Force(), ForceMode2D.Force, true, true);
                Vector2 velocity = velocityOf(data.playerVel);
                data.healthHandler.TakeForce(-velocity * Drag * Time.fixedDeltaTime, ForceMode2D.Force, true, true);
                data.sinceGrounded = 0f;
                yield return new WaitForFixedUpdate();
            }
            // NOTE: vanilla ShieldCharge ends with block.RPCA_DoBlock(...) — the free
            // second block. DIVE intentionally does nothing here.
            charging = false;
        }

        private static Vector2 velocityOf(PlayerVelocity playerVel)
        {
            if (playerVel == null || VelocityField == null)
            {
                return Vector2.zero;
            }
            object value = VelocityField.GetValue(playerVel);
            return value is Vector2 v ? v : Vector2.zero;
        }
    }
}
