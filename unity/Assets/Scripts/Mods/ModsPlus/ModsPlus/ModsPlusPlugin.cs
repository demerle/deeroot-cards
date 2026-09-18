using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModsPlus
{
    [BepInDependency("com.willis.rounds.unbound", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("pykess.rounds.plugins.moddingutils", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin(ModId, ModName, "1.5.4")]
    [BepInProcess("Rounds.exe")]
    public class ModsPlusPlugin : BaseUnityPlugin
    {
        public static ManualLogSource LOGGER { get => Instance.Logger; }
        private static ModsPlusPlugin Instance { get; set; }

        private const string ModId = "com.willis.rounds.modsplus";
        private const string ModName = "Mods Plus";

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            var harmony = new Harmony(ModId);
            harmony.PatchAll();
        }
    }
}
