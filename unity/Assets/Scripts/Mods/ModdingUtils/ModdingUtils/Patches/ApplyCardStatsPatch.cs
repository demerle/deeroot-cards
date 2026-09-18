using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ModdingUtils.Patches
{
    [HarmonyPatch(typeof(ApplyCardStats))]
    class CheckBulletsAfterGettingCards
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(ApplyCardStats.CopyGunStats))]
        private static void StopStackingObjectsToSpawn(ApplyCardStats __instance, Gun copyFromGun, Gun copyToGun)
        {
            List<ObjectsToSpawn> list = new List<ObjectsToSpawn>();

            foreach (var objectToSpawn in copyToGun.objectsToSpawn)
            {
                if (copyFromGun.objectsToSpawn.Contains(objectToSpawn))
                {
                    ObjectsToSpawn newObj = new ObjectsToSpawn();
                    int stacks = 0;
                    if (objectToSpawn.stacks > 0)
                    {
                        stacks = objectToSpawn.stacks;
                        objectToSpawn.stacks = 0;
                    }
                    CopyObjectsToSpawn(newObj, objectToSpawn);
                    newObj.stacks = stacks;
                    list.Add(newObj);
                }
                else
                {
                    list.Add(objectToSpawn);
                }
            }

            copyToGun.objectsToSpawn = list.ToArray();
        }

        private static void CopyObjectsToSpawn(ObjectsToSpawn to, ObjectsToSpawn from)
        {
            to.effect = from.effect;
            to.direction = from.direction;
            to.spawnOn = from.spawnOn;
            to.spawnAsChild = from.spawnAsChild;
            to.numberOfSpawns = from.numberOfSpawns;
            to.normalOffset = from.normalOffset;
            to.stickToBigTargets = from.stickToBigTargets;
            to.stickToAllTargets = from.stickToAllTargets;
            to.zeroZ = from.zeroZ;
            to.AddToProjectile = from.AddToProjectile;
            to.removeScriptsFromProjectileObject = from.removeScriptsFromProjectileObject;
            to.scaleStacks = from.scaleStacks;
            to.scaleStackM = from.scaleStackM;
            to.scaleFromDamage = from.scaleFromDamage;
            to.stacks = from.stacks;
        }
    }
}
