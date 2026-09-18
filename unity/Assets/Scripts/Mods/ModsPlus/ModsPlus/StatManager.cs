using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ModdingUtils.MonoBehaviours;

namespace ModsPlus
{
    public class StatManager
    {
        public static StatChangeTracker Apply(Player player, StatChanges stats)
        {
            var effect = player.gameObject.AddComponent<TemporaryEffect>();
            return effect.Initialize(stats);
        }

        public static void Remove(StatChangeTracker status)
        {
            if (!status.active) return;
            UnityEngine.Object.Destroy(status.effect);
        }
    }

    public class StatChangeTracker
    {
        public bool active;
        internal TemporaryEffect effect;

        internal StatChangeTracker(TemporaryEffect effect)
        {
            this.effect = effect;
        }
    }

    [Serializable]
    public class StatChanges
    {
        /// <summary>
        /// Additive
        /// </summary>
        public int
            Bullets = 0,
            Jumps = 0,
            MaxAmmo = 0;

        /// <summary>
        /// Multiplicative
        /// </summary>
        public float
            AttackSpeed = 1,
            PlayerGravity = 1,
            MovementSpeed = 1,
            ProjectileGravity = 1,
            Damage = 1,
            PlayerSize = 1,
            MaxHealth = 1,
            BulletSpread = 1,
            BulletSpeed = 1,
            JumpHeight = 1;
    }

    internal class TemporaryEffect : ReversibleEffect
    {
        private StatChanges statChanges;
        private StatChangeTracker status;

        public StatChangeTracker Initialize(StatChanges stats)
        {
            this.statChanges = stats;
            this.status = new StatChangeTracker(this);
            return status;
        }

        public override void OnStart()
        {
            characterStatModifiersModifier.sizeMultiplier_mult = statChanges.PlayerSize;
            characterStatModifiersModifier.movementSpeed_mult = statChanges.MovementSpeed;
            characterStatModifiersModifier.jump_mult = statChanges.JumpHeight;

            characterDataModifier.numberOfJumps_add = statChanges.Jumps;
            characterDataModifier.maxHealth_mult = statChanges.MaxHealth;

            gravityModifier.gravityForce_mult = statChanges.PlayerGravity;

            gunStatModifier.numberOfProjectiles_add = statChanges.Bullets;
            gunStatModifier.spread_mult = statChanges.BulletSpread;
            gunStatModifier.attackSpeed_mult = statChanges.AttackSpeed;
            gunStatModifier.gravity_mult = statChanges.ProjectileGravity;
            gunStatModifier.damage_mult = statChanges.Damage;
            gunStatModifier.projectileSpeed_mult = statChanges.BulletSpeed;

            gunAmmoStatModifier.maxAmmo_add = statChanges.MaxAmmo;

            status.active = true;
        }

        public override void OnOnDestroy()
        {
            status.active = false;
        }
    }
}
