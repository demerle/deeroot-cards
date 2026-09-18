using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnboundLib.GameModes;

namespace ModsPlus
{
    public class PlayerHook : CardEffect
    {
        protected virtual void Awake()
        {
            var player = GetComponentInParent<Player>();
            var data = player.data;
            var gun = data.weaponHandler.gun;
            var gunAmmo = data.weaponHandler.gun.GetComponentInChildren<GunAmmo>();
            var healthHandler = data.healthHandler;
            var gravity = player.GetComponentInChildren<Gravity>();
            var block = data.block;
            var stats = data.stats;

            Initialize(player, gun, gunAmmo, data, healthHandler, gravity, block, stats);
        }
    }
}
