using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib;
using UnboundLib.Cards;
using UnboundLib.GameModes;
using UnityEngine;

namespace ModsPlus
{
    public abstract class CustomEffectCard<T> : CustomCard where T : CardEffect
    {
        public abstract CardDetails Details { get; }

        public class CardDetails
        {
            public string Title { get; set; } = typeof(T).Name;
            public string Description { get; set; } = $"{typeof(T).Name} description";
            public string ModName { get; set; } = "Modded";
            public bool OwnerOnly { get; set; } = false;
            public CardInfo.Rarity Rarity { get; set; } = CardInfo.Rarity.Common;
            public CardThemeColor.CardThemeColorType Theme { get; set; } = CardThemeColor.CardThemeColorType.TechWhite;
            public CardInfoStat[] Stats { get; set; }
            public GameObject Art { get; set; }
        }

        public sealed override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            Added(player, gun, gunAmmo, data, health, gravity, block, characterStats);

            if (Details.OwnerOnly && !player.data.view.IsMine) return;

            var effect = player.gameObject.GetComponentInChildren<T>();

            if (effect != null)
            {
                effect.OnUpgradeCardInternal();
            }
            else
            {
                effect = new GameObject($"{Details.Title} effect").AddComponent<T>();
                effect.transform.SetParent(player.transform);
                effect.transform.localPosition = Vector3.zero;
                effect.Initialize(player, gun, gunAmmo, data, health, gravity, block, characterStats);
                player.data.stats.objectsAddedToPlayer.Add(effect.gameObject);
            }
        }

        public sealed override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            if (Details.OwnerOnly && !player.data.view.IsMine) return;

            Removed(player, gun, gunAmmo, data, health, gravity, block, statModifiers);
        }

        public sealed override void OnRemoveCard()
        {
            base.OnRemoveCard();
        }

        protected sealed override string GetTitle() => Details.Title;
        protected sealed override string GetDescription() => Details.Description;
        public sealed override string GetModName() => Details.ModName;
        protected sealed override CardInfo.Rarity GetRarity() => Details.Rarity;
        protected sealed override CardThemeColor.CardThemeColorType GetTheme() => Details.Theme;
        protected sealed override CardInfoStat[] GetStats() => Details.Stats;
        protected sealed override GameObject GetCardArt() => Details.Art;

        protected virtual void Added(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats) { }
        protected virtual void Removed(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats) { }
    }

    public abstract class CardEffect : MonoBehaviour
    {
        protected Player player { get; private set; }
        protected Gun gun { get; private set; }
        protected GunAmmo gunAmmo { get; private set; }
        protected CharacterData data { get; private set; }
        protected HealthHandler health { get; private set; }
        protected Gravity gravity { get; private set; }
        protected Block block { get; private set; }
        protected CharacterStatModifiers characterStats { get; private set; }

        public virtual void Initialize(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            this.player = player;
            this.gun = gun;
            this.gunAmmo = gunAmmo;
            this.data = data;
            this.health = health;
            this.gravity = gravity;
            this.block = block;
            this.characterStats = characterStats;
        }

        protected virtual void Start()
        {
            gun.ShootPojectileAction += OnShootInternal;
            data.TouchGroundAction += OnTouchGroundInternal;
            data.TouchWallAction += OnTouchWallInternal;
            health.reviveAction += OnReviveInternal;
            health.delayedReviveAction += OnDelayedReviveInternal;
            block.BlockAction += OnBlockInternal;
            block.BlockActionEarly += OnBlockEarlyInternal;
            block.BlockProjectileAction += OnBlockProjectileInternal;
            block.BlockRechargeAction += OnBlockRechargeInternal;
            characterStats.DealtDamageAction += OnDealtDamageInternal;
            characterStats.WasDealtDamageAction += OnTakeDamageInternal;
            characterStats.OnReloadDoneAction += OnReloadDoneInternal;
            characterStats.OutOfAmmpAction += OnOutOfAmmoInternal;
            data.jump.JumpAction += OnJumpInternal;

            GameModeManager.AddHook(GameModeHooks.HookGameEnd, OnGameEndInternal);
            GameModeManager.AddHook(GameModeHooks.HookGameStart, OnGameStartInternal);
            GameModeManager.AddHook(GameModeHooks.HookBattleStart, OnBattleStartInternal);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickStart, OnStartPickInternal);
            GameModeManager.AddHook(GameModeHooks.HookPlayerPickEnd, OnEndPickInternal);
            GameModeManager.AddHook(GameModeHooks.HookPointStart, OnPointStartInternal);
            GameModeManager.AddHook(GameModeHooks.HookPointEnd, OnPointEndInternal);
            GameModeManager.AddHook(GameModeHooks.HookPickStart, OnPickPhaseStartInternal);
            GameModeManager.AddHook(GameModeHooks.HookPickEnd, OnPickPhaseEndInternal);
            GameModeManager.AddHook(GameModeHooks.HookRoundStart, OnRoundStartInternal);
            GameModeManager.AddHook(GameModeHooks.HookRoundEnd, OnRoundEndInternal);
        }

        protected virtual void OnDestroy()
        {
            gun.ShootPojectileAction -= OnShootInternal;
            data.TouchGroundAction -= OnTouchGroundInternal;
            data.TouchWallAction -= OnTouchWallInternal;
            health.reviveAction -= OnReviveInternal;
            health.delayedReviveAction -= OnDelayedReviveInternal;
            block.BlockAction -= OnBlockInternal;
            block.BlockActionEarly -= OnBlockEarlyInternal;
            block.BlockProjectileAction -= OnBlockProjectileInternal;
            block.BlockRechargeAction -= OnBlockRechargeInternal;
            characterStats.DealtDamageAction -= OnDealtDamageInternal;
            characterStats.WasDealtDamageAction -= OnTakeDamageInternal;
            characterStats.OnReloadDoneAction -= OnReloadDoneInternal;
            characterStats.OutOfAmmpAction -= OnOutOfAmmoInternal;
            data.jump.JumpAction -= OnJumpInternal;

            GameModeManager.RemoveHook(GameModeHooks.HookGameEnd, OnGameEndInternal);
            GameModeManager.RemoveHook(GameModeHooks.HookGameStart, OnGameStartInternal);
            GameModeManager.RemoveHook(GameModeHooks.HookBattleStart, OnBattleStartInternal);
            GameModeManager.RemoveHook(GameModeHooks.HookPlayerPickStart, OnStartPickInternal);
            GameModeManager.RemoveHook(GameModeHooks.HookPlayerPickEnd, OnEndPickInternal);
            GameModeManager.RemoveHook(GameModeHooks.HookPointStart, OnPointStartInternal);
            GameModeManager.RemoveHook(GameModeHooks.HookPointEnd, OnPointEndInternal);
            GameModeManager.RemoveHook(GameModeHooks.HookPickStart, OnPickPhaseStartInternal);
            GameModeManager.RemoveHook(GameModeHooks.HookPickEnd, OnPickPhaseEndInternal);
            GameModeManager.RemoveHook(GameModeHooks.HookRoundStart, OnRoundStartInternal);
            GameModeManager.RemoveHook(GameModeHooks.HookRoundEnd, OnRoundEndInternal);

            StopAllCoroutines();
        }

        internal IEnumerator OnGameEndInternal(IGameModeHandler gameModeHandler)
        {
            yield return OnGameEnd(gameModeHandler);
        }
        internal IEnumerator OnGameStartInternal(IGameModeHandler gameModeHandler)
        {
            yield return OnGameStart(gameModeHandler);
        }
        internal IEnumerator OnBattleStartInternal(IGameModeHandler gameModeHandler)
        {
            yield return OnBattleStart(gameModeHandler);
        }
        internal IEnumerator OnStartPickInternal(IGameModeHandler gameModeHandler)
        {
            yield return OnStartPick(gameModeHandler);
        }
        internal IEnumerator OnEndPickInternal(IGameModeHandler gameModeHandler)
        {
            yield return OnEndPick(gameModeHandler);
        }
        internal IEnumerator OnPointStartInternal(IGameModeHandler gameModeHandler)
        {
            yield return OnPointStart(gameModeHandler);
        }
        internal IEnumerator OnPointEndInternal(IGameModeHandler gameModeHandler)
        {
            yield return OnPointEnd(gameModeHandler);
        }
        internal IEnumerator OnPickPhaseStartInternal(IGameModeHandler gameModeHandler)
        {
            yield return OnPickPhaseStart(gameModeHandler);
        }
        internal IEnumerator OnPickPhaseEndInternal(IGameModeHandler gameModeHandler)
        {
            yield return OnPickPhaseEnd(gameModeHandler);
        }
        internal IEnumerator OnRoundStartInternal(IGameModeHandler gameModeHandler)
        {
            yield return OnRoundStart(gameModeHandler);
        }
        internal IEnumerator OnRoundEndInternal(IGameModeHandler gameModeHandler)
        {
            yield return OnRoundEnd(gameModeHandler);
        }
        public virtual IEnumerator OnGameEnd(IGameModeHandler gameModeHandler) { yield break; }
        public virtual IEnumerator OnGameStart(IGameModeHandler gameModeHandler) { yield break; }
        public virtual IEnumerator OnBattleStart(IGameModeHandler gameModeHandler) { yield break; }
        public virtual IEnumerator OnStartPick(IGameModeHandler gameModeHandler) { yield break; }
        public virtual IEnumerator OnEndPick(IGameModeHandler gameModeHandler) { yield break; }
        public virtual IEnumerator OnPointStart(IGameModeHandler gameModeHandler) { yield break; }
        public virtual IEnumerator OnPointEnd(IGameModeHandler gameModeHandler) { yield break; }
        public virtual IEnumerator OnPickPhaseStart(IGameModeHandler gameModeHandler) { yield break; }
        public virtual IEnumerator OnPickPhaseEnd(IGameModeHandler gameModeHandler) { yield break; }
        public virtual IEnumerator OnRoundStart(IGameModeHandler gameModeHandler) { yield break; }
        public virtual IEnumerator OnRoundEnd(IGameModeHandler gameModeHandler) { yield break; }


        internal void OnUpgradeCardInternal()
        {
            OnUpgradeCard();
            SafeStartCoroutine(OnUpgradeCardCoroutine());
        }
        internal HasToReturn OnBulletHitInternal(GameObject projectile, HitInfo hit)
        {
            OnBulletHit(projectile, hit);
            SafeStartCoroutine(OnBulletHitCoroutine(projectile, hit));
            return HasToReturn.canContinue;
        }
        internal void OnShootInternal(GameObject projectile)
        {
            projectile.AddComponent<BulletHitEvent>().OnHit += OnBulletHitInternal;
            OnShoot(projectile);
            SafeStartCoroutine(OnShootCoroutine(projectile));
        }
        internal void OnTouchGroundInternal(float timeSinceGrounded, Vector3 position, Vector3 groundNormal, Transform groundTransform)
        {
            OnTouchGround(timeSinceGrounded, position, groundNormal, groundTransform);
            SafeStartCoroutine(OnTouchGroundCoroutine(timeSinceGrounded, position, groundNormal, groundTransform));
        }
        internal void OnTouchWallInternal(float timeSinceLastGrab, Vector3 position, Vector3 wallNormal)
        {
            OnTouchWall(timeSinceLastGrab, position, wallNormal);
            SafeStartCoroutine(OnTouchWallCoroutine(timeSinceLastGrab, position, wallNormal));
        }
        internal void OnReviveInternal()
        {
            SafeExecuteAction(OnRevive);
            SafeStartCoroutine(OnReviveCoroutine());
        }
        internal void OnDelayedReviveInternal()
        {
            SafeExecuteAction(OnDelayedRevive);
            SafeStartCoroutine(OnDelayedReviveCoroutine());
        }
        internal void OnBlockInternal(BlockTrigger.BlockTriggerType blockTriggerType)
        {
            OnBlock(blockTriggerType);
            SafeStartCoroutine(OnBlockCoroutine(blockTriggerType));
        }
        internal void OnBlockEarlyInternal(BlockTrigger.BlockTriggerType blockTriggerType)
        {
            OnBlockEarly(blockTriggerType);
            SafeStartCoroutine(OnBlockEarlyCoroutine(blockTriggerType));
        }
        internal void OnBlockProjectileInternal(GameObject projectile, Vector3 forward, Vector3 hitPosition)
        {
            OnBlockProjectile(projectile, forward, hitPosition);
            SafeStartCoroutine(OnBlockProjectileCoroutine(projectile, forward, hitPosition));
        }
        internal void OnBlockRechargeInternal()
        {
            OnBlockRecharge();
            SafeStartCoroutine(OnBlockRechargeCoroutine());
        }
        internal void OnDealtDamageInternal(Vector2 damage, bool selfDamage)
        {
            OnDealtDamage(damage, selfDamage);
            SafeStartCoroutine(OnDealtDamageCoroutine(damage, selfDamage));
        }
        internal void OnTakeDamageInternal(Vector2 damage, bool selfDamage)
        {
            OnTakeDamage(damage, selfDamage);
            SafeStartCoroutine(OnTakeDamageCoroutine(damage, selfDamage));
        }
        internal void OnReloadDoneInternal(int bulletsReloaded)
        {
            OnReloadDone(bulletsReloaded);
            SafeStartCoroutine(OnReloadDoneCoroutine(bulletsReloaded));
        }
        internal void OnOutOfAmmoInternal(int bulletsReloaded)
        {
            OnOutOfAmmo(bulletsReloaded);
            SafeStartCoroutine(OnOutOfAmmoCoroutine(bulletsReloaded));
        }
        internal void OnJumpInternal()
        {
            OnJump();
            SafeStartCoroutine(OnJumpCoroutine());
        }

        private void SafeStartCoroutine(IEnumerator coroutine)
        {
            Unbound.Instance.StartCoroutine(ExecuteWhenPlayerActive(() => player.StartCoroutine(coroutine)));
        }

        private void SafeExecuteAction(Action action)
        {
            Unbound.Instance.StartCoroutine(ExecuteWhenPlayerActive(action));
        }

        private IEnumerator ExecuteWhenPlayerActive(Action action)
        {
            yield return new WaitUntil(() => player.gameObject.activeInHierarchy);
            action?.Invoke();
        }

        public virtual void OnUpgradeCard() { }
        public virtual void OnBulletHit(GameObject projectile, HitInfo hit) { }
        public virtual void OnShoot(GameObject projectile) { }
        public virtual void OnTouchGround(float timeSinceGrounded, Vector3 position, Vector3 groundNormal, Transform groundTransform) { }
        public virtual void OnTouchWall(float timeSinceLastGrab, Vector3 position, Vector3 wallNormal) { }
        public virtual void OnRevive() { }
        public virtual void OnDelayedRevive() { }
        public virtual void OnBlock(BlockTrigger.BlockTriggerType blockTriggerType) { }
        public virtual void OnBlockEarly(BlockTrigger.BlockTriggerType blockTriggerType) { }
        public virtual void OnBlockProjectile(GameObject projectile, Vector3 forward, Vector3 hitPosition) { }
        public virtual void OnBlockRecharge() { }
        public virtual void OnDealtDamage(Vector2 damage, bool selfDamage) { }
        public virtual void OnTakeDamage(Vector2 damage, bool selfDamage) { }
        public virtual void OnReloadDone(int bulletsReloaded) { }
        public virtual void OnOutOfAmmo(int bulletsReloaded) { }
        public virtual void OnJump() { }

        public virtual IEnumerator OnUpgradeCardCoroutine() { yield return null; }
        public virtual IEnumerator OnBulletHitCoroutine(GameObject projectile, HitInfo hit) { yield return null; }
        public virtual IEnumerator OnShootCoroutine(GameObject projectile) { yield return null; }
        public virtual IEnumerator OnTouchGroundCoroutine(float timeSinceGrounded, Vector3 position, Vector3 groundNormal, Transform groundTransform) { yield return null; }
        public virtual IEnumerator OnTouchWallCoroutine(float timeSinceLastGrab, Vector3 position, Vector3 wallNormal) { yield return null; }
        public virtual IEnumerator OnReviveCoroutine() { yield return null; }
        public virtual IEnumerator OnDelayedReviveCoroutine() { yield return null; }
        public virtual IEnumerator OnBlockCoroutine(BlockTrigger.BlockTriggerType blockTriggerType) { yield return null; }
        public virtual IEnumerator OnBlockEarlyCoroutine(BlockTrigger.BlockTriggerType blockTriggerType) { yield return null; }
        public virtual IEnumerator OnBlockProjectileCoroutine(GameObject projectile, Vector3 forward, Vector3 hitPosition) { yield return null; }
        public virtual IEnumerator OnBlockRechargeCoroutine() { yield return null; }
        public virtual IEnumerator OnDealtDamageCoroutine(Vector3 damage, bool selfDamage) { yield return null; }
        public virtual IEnumerator OnTakeDamageCoroutine(Vector3 damage, bool selfDamage) { yield return null; }
        public virtual IEnumerator OnReloadDoneCoroutine(int bulletsReloaded) { yield return null; }
        public virtual IEnumerator OnOutOfAmmoCoroutine(int bulletsReloaded) { yield return null; }
        public virtual IEnumerator OnJumpCoroutine() { yield return null; }
    }

    internal class BulletHitEvent : RayHitEffect
    {
        public delegate HasToReturn HitEvent(GameObject projectile, HitInfo hit);
        public event HitEvent OnHit;

        public override HasToReturn DoHitEffect(HitInfo hit)
        {
            return OnHit.Invoke(gameObject, hit);
        }
    }
}
