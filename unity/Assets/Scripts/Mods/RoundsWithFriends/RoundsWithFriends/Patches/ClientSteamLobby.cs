using HarmonyLib;
using Landfall.Network;

namespace RWF.Patches
{
    [HarmonyPatch(typeof(ClientSteamLobby), "ShowInviteScreenWhenConnected")]
    class ClientSteamLobby_Patch_ShowInviteScreenWhenConnected
    {
        static bool Prefix(ClientSteamLobby __instance) {
            // Allow inviting multiple times in the same room
            return true;
        }
    }
}
