using BepInEx;
using UnboundLib;
using UnboundLib.Cards;
using DeerootCards.Cards;

namespace DeerootCards
{
    [BepInDependency("com.willis.rounds.unbound")]
    [BepInDependency("pykess.rounds.plugins.moddingutils")]
    [BepInDependency("pykess.rounds.plugins.cardchoicespawnuniquecardpatch")]
    [BepInPlugin("com.deeroot.cards", "DeerootCards", "0.0.1")]
    [BepInProcess("Rounds.exe")]
    public class DeerootCards : BaseUnityPlugin
    {
        internal static string modInitials = "DEER";

        void Awake()
        {
            UnityEngine.Debug.Log("[DEER] DeerootCards mod loading, building cards...");
            Cards.DeleteCard.Init(); // applies the pick-phase hold patch
            Cards.PortalCard.Init(); // applies the bullet-portal MoveTransform patch
            CustomCard.BuildCard<OverdriveCard>();
            CustomCard.BuildCard<BlinkCard>();
            CustomCard.BuildCard<PortalCard>();
            CustomCard.BuildCard<DoubleCard>();
            CustomCard.BuildCard<DeleteCard>();
        }

        void Start()
        {
        }
    }
}
