using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnboundLib.Cards;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Full Counter: blocking a bullet reflects it at double damage.
    ///
    /// Applies to EVERY bullet the block actually reflects — opponents, teammates,
    /// and the holder's own gun. No team or owner filter.
    ///
    /// Do NOT hook Block.BlockProjectileAction for this. Vanilla Block.blocked
    /// reverses velocity for every blocked projectile, then skips
    /// BlockProjectileAction when SpawnedAttack.spawner is the blocker
    /// (Block.cs). That skip is why a BlockProjectileAction damage hook doubles
    /// enemy bullets and silently does nothing to a bullet you shot into the air
    /// and blocked on the way down. The reflect itself already happened; only
    /// the callback was withheld. Damage has to be applied on blocked() itself.
    /// </summary>
    public class FullCounterCard : CustomCard
    {
        public const string CardName = "Full Counter";

        internal const float DamageMultiplierPerStack = 2f;

        internal static int Stacks(CharacterData data)
        {
            if (data == null || data.currentCards == null)
            {
                return 0;
            }
            return data.currentCards.Count(c => c != null && c.cardName == CardName);
        }

        private static bool harmonyApplied;

        public static void Init()
        {
            if (harmonyApplied)
            {
                return;
            }
            harmonyApplied = true;
            var harmony = new Harmony("com.deeroot.cards.fullcounter");
            harmony.PatchAll(typeof(FullCounterReflectPatch));
        }

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            UnityEngine.Debug.Log("[DEER] FullCounterCard SetupCard");
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            UnityEngine.Debug.Log($"[DEER] FullCounterCard added to player {player.data.name} stacks={Stacks(data)}");
        }

        protected override string GetTitle()
        {
            return CardName;
        }

        protected override string GetDescription()
        {
            return "Blocking a bullet reflects it at double damage.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Blocked bullet damage",
                    amount = "x2",
                    simepleAmount = CardInfoStat.SimpleAmount.aLotOf
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
            return CardThemeColor.CardThemeColorType.DefensiveBlue;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }

    /// <summary>
    /// Doubles ProjectileHit.damage on the reflect itself.
    ///
    /// Block.blocked runs on every client: ProjectileHit.RPCA_DoHit is
    /// RpcTarget.All and calls Block.DoBlock, which calls blocked(). Damage is
    /// a local field (not photon-synced; SyncProjectile only sends pos/vel),
    /// so multiplying it here keeps every client's copy in step with no RPC.
    /// The bullet owner (hasControl) is the client that later applies the hit,
    /// and that client also ran this postfix.
    ///
    /// Own bullets are included on purpose. The vanilla spawner==blocker check
    /// only gates BlockProjectileAction; it does not skip the velocity reverse
    /// or this postfix.
    /// </summary>
    [HarmonyPatch(typeof(Block), nameof(Block.blocked))]
    public class FullCounterReflectPatch
    {
        static void Postfix(Block __instance, GameObject projectile)
        {
            if (projectile == null)
            {
                return;
            }

            CharacterData data = __instance.GetComponent<CharacterData>();
            int stacks = FullCounterCard.Stacks(data);
            if (stacks <= 0)
            {
                return;
            }

            ProjectileHit hit = projectile.GetComponent<ProjectileHit>();
            if (hit == null)
            {
                hit = projectile.GetComponentInParent<ProjectileHit>();
            }
            if (hit == null)
            {
                return;
            }

            // Bullet visuals + detection radius are spawn-time caches:
            // RayCastTrail.Start caches size from damage, SetScaleFromSizeAndExtraSize.Start
            // scales the sprite from that size once. Damage changes after spawn are
            // invisible until we rescale them here (mirrors vanilla RayHitReflect,
            // which calls ScaleTrailFromDamage.Rescale() after its dmgM bump).
            float oldTrailSize = 0f;
            RayCastTrail trail = projectile.GetComponent<RayCastTrail>();
            if (trail == null)
            {
                trail = projectile.GetComponentInParent<RayCastTrail>();
            }
            if (trail != null)
            {
                oldTrailSize = TrailSizeFromDamage(hit.damage, trail.extraSize);
            }

            float multiplier = Mathf.Pow(FullCounterCard.DamageMultiplierPerStack, stacks);
            float before = hit.damage;
            hit.damage *= multiplier;
            hit.shake *= multiplier;

            float newTrailSize = 0f;
            if (trail != null)
            {
                // Visible sprite: ratio-rescale by the same factor spawn scaling
                // used (the transform is only ever scaled once, at Start).
                SetScaleFromSizeAndExtraSize spriteScaler =
                    projectile.GetComponentInChildren<SetScaleFromSizeAndExtraSize>(true);
                if (spriteScaler != null && oldTrailSize > 0.0001f)
                {
                    newTrailSize = TrailSizeFromDamage(hit.damage, trail.extraSize);
                    float ratio = newTrailSize / oldTrailSize;
                    spriteScaler.transform.localScale *= ratio;
                }

                // Detection radius: exact same formula as RayCastTrail.Start.
                newTrailSize = TrailSizeFromDamage(hit.damage, trail.extraSize);
                trail.size = newTrailSize;

                // Trail width/thickness: vanilla's own rescale idiom.
                GetComponentInDisabled<ScaleTrailFromDamage>(projectile)?.Rescale();
            }

            // Same comparison vanilla uses to SKIP BlockProjectileAction.
            // Logged so a playtest can prove the own-bullet case actually doubled.
            SpawnedAttack spawned = projectile.GetComponentInParent<SpawnedAttack>();
            bool ownBullet = spawned != null
                && spawned.spawner != null
                && spawned.spawner.gameObject == __instance.transform.root.gameObject;
            string who = ownBullet ? "OWN" : "foreign";
            string blocker = data != null && data.player != null ? data.player.name : __instance.name;
            string sizeLog = trail != null ? $" size {oldTrailSize:F2} -> {newTrailSize:F2}" : "";
            UnityEngine.Debug.Log($"[DEER] FullCounter: reflected {who} bullet damage {before:F1} -> {hit.damage:F1} (x{multiplier:F0}, stacks={stacks}){sizeLog} blocker={blocker}");
        }

        // Exact formula from RayCastTrail.Start (verified against decompile).
        private static float TrailSizeFromDamage(float damage, float extraSize)
        {
            return Mathf.Clamp(Mathf.Pow(damage, 0.85f) / 400f, 0f, 100f) + 0.3f + extraSize;
        }

        // GetComponentInChildren(false) skips inactive children, which pooled
        // recycled bullets can be; this resolves it regardless.
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
    }
}
