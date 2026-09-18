using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ModsPlus
{
    internal class Assets
    {
        public static GameObject BasePlayer = Resources.Load<GameObject>("Player");
        public static HealthBar BaseHealthBar = BasePlayer.GetComponentInChildren<HealthBar>();
    }
}
