using System;
using System.Collections.Generic;
using System.Linq;
using ModdingUtils.Extensions;
using UnityEngine;
using ModdingUtils.Utils;

namespace ModdingUtils.MonoBehaviours
{
    public class InConeEffect : MonoBehaviour
    {
        // internal variables
        private Player player;
        private Gun gun;
        private CharacterData data;
        private HealthHandler health;
        private Gravity gravity;
        private Block block;
        private GunAmmo gunAmmo;
        private CharacterStatModifiers statModifiers;
        private List<ColorEffect> colorEffects = new List<ColorEffect>();
        private List<MonoBehaviour> effects = new List<MonoBehaviour>();
        private float timeOfLastApply; // last time the effect was applied
        private bool effectApplied;


        // variables set by card

        private Vector2 centerRay = Vector2.zero; // center ray of the cone
        private float range = float.MaxValue; // maximum range from the player for the effect to work
        private float angle = 361f; // arc angle (in degrees) of the cone
        private float period; // how often to reapply the effect while conditions are met, 0f for once only. will be unapplied when conditions are no longer met and reapplied when they are

        // the function that adds. applies, sets, and returns a list of monobehaivors to apply to the player
        private Func<Player, Gun, GunAmmo, CharacterData, HealthHandler, Gravity, Block, CharacterStatModifiers, List<MonoBehaviour>> effectFunc;
        // the function that adds. applies, sets, and returns a list of monobehaivors to apply to the other player(s)
        private Func<Player, Gun, GunAmmo, CharacterData, HealthHandler, Gravity, Block, CharacterStatModifiers, List<MonoBehaviour>> otherPlayerEffectFunc;

        private Color colorMaxWhileActive = Color.clear; // colorMax of the player while conditions are met, clear for none
        private Color colorMinWhileActive = Color.clear; // colorMin of the player while conditions are met, clear for none
        private Color otherPlayerColorMaxWhileActive = Color.clear; // colorMax of the other players while conditions are met, clear for none
        private Color otherPlayerColorMinWhileActive = Color.clear; // colorMin of the other players while conditions are met, clear for none
        private bool needsLineOfSight; // does the effect need line of sight?
        private bool checkEnemiesOnly = true; // should only enemies be able to activate the effect?
        private bool applyToOthers; // should the effect apply to other players in the cone (true)?
        private bool applyToSelf = true; //  should the effect apply to the player with this component (true)? 


        void Awake()
        {
            player = gameObject.GetComponent<Player>();
            gun = player.GetComponent<Holding>().holdable.GetComponent<Gun>();
            data = player.GetComponent<CharacterData>();
            health = player.GetComponent<HealthHandler>();
            gravity = player.GetComponent<Gravity>();
            block = player.GetComponent<Block>();
            gunAmmo = gun.GetComponentInChildren<GunAmmo>();
            statModifiers = player.GetComponent<CharacterStatModifiers>();
        }

        void Start()
        {
            // if the centerRay has magnitude 0 (i.e. has not been set or hasn't been set properly), just destroy this component
            if (centerRay.magnitude == 0f)
            {
                Destroy();
            }
            else
            {
                // otherwise, normalize it
                centerRay = centerRay.normalized;
            }
            // if the effectFunc has not been set, just destroy this component
            if (applyToSelf && effectFunc == null)
            {
                Destroy();
            }
            if (applyToOthers && otherPlayerEffectFunc == null)
            {
                Destroy();
            }

            ResetTimer();

        }

        void Update()
        {
            // if the player is alive, simulated, and the conditions are met
            if (Utils.PlayerStatus.PlayerAliveAndSimulated(player) && ConditionsMet())
            {
                if (period == 0f && !effectApplied)
                {
                    ApplyEffectToAllPrescribedPlayers();
                    ResetTimer();
                    effectApplied = true;
                }
                // if the period is 0, then it only needed to be applied once, so just return
                else if (period == 0f && effectApplied)
                {
                    return;
                }
                // otherwise, it may need to be applied multiple times
                else if (Time.time >= timeOfLastApply + period)
                {
                    ApplyEffectToAllPrescribedPlayers();
                    ResetTimer();
                }
            }
            // if the conditions are not met, then remove all effects that may be present
            else
            {
                RemoveAllEffects();
                effectApplied = false;
            }

        }
        public bool ConditionsMet()
        {
            return (GetValidPlayersInCone().Count > 0);
        }
        public void ApplyEffectToAllPrescribedPlayers()
        {
            List<Player> validPlayers = GetValidPlayersInCone();
            if (applyToOthers)
            {
                ApplyEffectToOthers(validPlayers);
            }
            if (applyToSelf)
            {
                ApplyEffectToSelf();
            }
        }
        public List<Player> GetValidPlayersInCone()
        {
            if (checkEnemiesOnly)
            {
                return GetAllEnemyPlayers().Where(player => OtherPlayerInCone(player)).ToList();
            }
            else
            {
                return GetAllOtherPlayers().Where(player => OtherPlayerInCone(player)).ToList();
            }
        }
        public List<Player> GetAllEnemyPlayers()
        {
            return PlayerManager.instance.players.Where(player => player.teamID != this.player.teamID).ToList();
        }
        public List<Player> GetAllOtherPlayers()
        {
            return PlayerManager.instance.players.Where(player => player.playerID != this.player.playerID).ToList();
        }
        public bool OtherPlayerInCone(Player otherPlayer)
        {
            // is the player:
            /* In range,
             * In the cone angle,
             * Alive,
             * Simulated,
             * and in line-of-sight of this player, if required
             */

            bool lineOfSight = true;
            // if the effect needs line-of-sight, then check for it
            if (needsLineOfSight)
            {
                CanSeeInfo canSeeInfo = PlayerManager.instance.CanSeePlayer(player.transform.position, otherPlayer);
                lineOfSight = canSeeInfo.canSee;
            }

            Vector2 displacement = otherPlayer.transform.position - player.transform.position;
            return (Utils.PlayerStatus.PlayerAliveAndSimulated(otherPlayer) && lineOfSight && displacement.magnitude <= range && Vector2.Angle(centerRay, displacement) <= Math.Abs(angle / 2));
        }
        public void OnDisable()
        {
            RemoveAllEffects();
        }
        public void OnDestroy()
        {
            RemoveAllEffects();
        }
        public void ApplyEffectToSelf()
        {
            List<MonoBehaviour> newEffects = effectFunc(player, gun, gunAmmo, data, health, gravity, block, statModifiers);
            foreach (MonoBehaviour newEffect in newEffects)
            {
                effects.Add(newEffect);
            }
            ApplyColorEffectToSelf();
        }
        public void ApplyEffectToOthers(List<Player> otherPlayers)
        {
            Gun otherGun;
            CharacterData otherData;
            HealthHandler otherHealth;
            Gravity otherGravity;
            Block otherBlock;
            GunAmmo otherGunAmmo;
            CharacterStatModifiers otherStatModifiers;

            foreach (Player otherPlayer in otherPlayers)
            {
                otherGun = otherPlayer.GetComponent<Holding>().holdable.GetComponent<Gun>();
                otherData = otherPlayer.GetComponent<CharacterData>();
                otherHealth = otherPlayer.GetComponent<HealthHandler>();
                otherGravity = otherPlayer.GetComponent<Gravity>();
                otherBlock = otherPlayer.GetComponent<Block>();
                otherGunAmmo = otherGun.GetComponentInChildren<GunAmmo>();
                otherStatModifiers = otherPlayer.GetComponent<CharacterStatModifiers>();

                List<MonoBehaviour> newEffects = otherPlayerEffectFunc(otherPlayer, otherGun, otherGunAmmo, otherData, otherHealth, otherGravity, otherBlock, otherStatModifiers);
                foreach (MonoBehaviour newEffect in newEffects)
                {
                    effects.Add(newEffect);
                }
                ApplyColorEffectToOtherPlayer(otherPlayer);
            }
        }
        public void ApplyColorEffectToSelf()
        {
            ColorEffect newColorEffect = player.gameObject.AddComponent<ColorEffect>();
            newColorEffect.SetColorMax(colorMaxWhileActive);
            newColorEffect.SetColorMin(colorMinWhileActive);

            colorEffects.Add(newColorEffect);
        }
        public void ApplyColorEffectToOtherPlayer(Player otherPlayer)
        {
            ColorEffect newColorEffect = otherPlayer.gameObject.AddComponent<ColorEffect>();
            newColorEffect.SetColorMax(otherPlayerColorMaxWhileActive);
            newColorEffect.SetColorMin(otherPlayerColorMinWhileActive);

            colorEffects.Add(newColorEffect);
        }
        public void RemoveEffects()
        {
            foreach (MonoBehaviour effect in effects)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }
            effects = new List<MonoBehaviour>();
        }
        public void RemoveColorEffects()
        {
            foreach (ColorEffect colorEffect in colorEffects)
            {
                if (colorEffect != null)
                {
                    Destroy(colorEffect);
                }
            }
            colorEffects = new List<ColorEffect>();
        }
        public void RemoveAllEffects()
        {
            RemoveEffects();
            RemoveColorEffects();
        }
        public void ResetTimer()
        {
            timeOfLastApply = Time.time;
        }
        public void Destroy()
        {
            UnityEngine.Object.Destroy(this);
        }

        public void SetCenterRay(Vector2 ray)
        {
            centerRay = ray;
        }
        public void SetRange(float range)
        {
            this.range = range;
        }
        public void SetAngle(float angle)
        {
            this.angle = angle;
        }
        public void SetPeriod(float period)
        {
            this.period = period;
        }
        public void SetEffectFunc(Func<Player, Gun, GunAmmo, CharacterData, HealthHandler, Gravity, Block, CharacterStatModifiers, List<MonoBehaviour>> effectFunc)
        {
            this.effectFunc = effectFunc;
        }
        public void SetOtherEffectFunc(Func<Player, Gun, GunAmmo, CharacterData, HealthHandler, Gravity, Block, CharacterStatModifiers, List<MonoBehaviour>> effectFunc)
        {
            otherPlayerEffectFunc = effectFunc;
        }
        public void SetColor(Color color)
        {
            colorMaxWhileActive = color;
            colorMinWhileActive = color;
        }
        public void SetColorMin(Color color)
        {
            colorMinWhileActive = color;
        }
        public void SetColorMax(Color color)
        {
            colorMaxWhileActive = color;
        }
        public void SetOtherColor(Color color)
        {
            otherPlayerColorMaxWhileActive = color;
            otherPlayerColorMinWhileActive = color;
        }
        public void SetOtherColorMin(Color color)
        {
            otherPlayerColorMinWhileActive = color;
        }
        public void SetOtherColorMax(Color color)
        {
            otherPlayerColorMaxWhileActive = color;
        }
        public void SetNeedsLineOfSight(bool needsLineOfSight)
        {
            this.needsLineOfSight = needsLineOfSight;
        }
        public void SetCheckEnemiesOnly(bool checkEnemiesOnly)
        {
            this.checkEnemiesOnly = checkEnemiesOnly;
        }
        public void SetApplyToOthers(bool applyToOthers)
        {
            this.applyToOthers = applyToOthers;
        }
        public void SetApplyToSelf(bool applyToSelf)
        {
            this.applyToSelf = applyToSelf;
        }
    }
}
