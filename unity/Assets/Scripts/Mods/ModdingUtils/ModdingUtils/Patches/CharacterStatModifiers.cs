using System;
using System.Reflection.Emit;
using HarmonyLib;
using UnboundLib;
using System.Collections.Generic;
using UnityEngine;

namespace ModdingUtils.Patches
{
    [HarmonyPatch(typeof(CharacterStatModifiers), "ResetStats")]
    internal class CharacterStatModifiers_Patch_ResetStats
    {
        // check if the objects are null before destroying them to prevent errors
        static void TryDestroy(UnityEngine.Object obj)
        {
            if (obj != null) { UnityEngine.Object.Destroy(obj); }
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m_destroy = ExtensionMethods.GetMethodInfo(typeof(GameObject), nameof(GameObject.Destroy), new System.Type[] { typeof(UnityEngine.Object) });
            var m_tryDestroy = ExtensionMethods.GetMethodInfo(typeof(CharacterStatModifiers_Patch_ResetStats), nameof(TryDestroy));

            foreach (var code in instructions)
            {
                if (code.Calls(m_destroy))
                {
                    yield return new CodeInstruction(OpCodes.Call, m_tryDestroy);
                }
                else
                {
                    yield return code;
                }
            }

        }
    }
}
