using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnboundLib.GameModes;

namespace ClassesManagerReborn.Patchs
{
    internal class AjustRarities
    {
        internal static IEnumerator gameStart(IGameModeHandler gm)
        {
            ClassesRegistry.GetClassInfos(~CardType.Entry).ForEach(CI => {
                RarityLib.Utils.RarityUtils.SetCardRarityModifier(CI, RarityLib.Utils.RarityUtils.GetCardRarityModifier(CI) * ClassesManager.Class_Odds.Value);
            });
            yield break;
        }
    }
}
