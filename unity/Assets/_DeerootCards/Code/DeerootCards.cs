using BepInEx;
using UnboundLib;
using UnboundLib.Cards;
using DeerootCards.Cards;

namespace DeerootCards
{
    [BepInDependency("com.willis.rounds.unbound")]
    [BepInDependency("pykess.rounds.plugins.moddingutils")]
    [BepInDependency("pykess.rounds.plugins.pickncards")]
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
            Cards.HeartCard.Init(); // applies the bullet-heart MoveTransform patch
            Cards.BouncyBallCard.Init(); // applies the knockback-taken CallTakeForce patch
            Cards.SovereignCard.Init(); // applies the Sovereign friendly-fire gates
            Cards.DynamicFieldCard.Init(); // no patches — vanilla block event drives the field
            CustomCard.BuildCard<OverdriveCard>();
            CustomCard.BuildCard<BlinkCard>();
            CustomCard.BuildCard<DiveCard>();
            CustomCard.BuildCard<BouncyBallCard>();
            CustomCard.BuildCard<PortalCard>();
            CustomCard.BuildCard<DoubleCard>();
            CustomCard.BuildCard<DeleteCard>();
            CustomCard.BuildCard<HeartCard>();
            CustomCard.BuildCard<SovereignCard>();
            CustomCard.BuildCard<DynamicFieldCard>();
            CustomCard.BuildCard<InvisibilityCard>();
        }

        void Start()
        {
        }
    }
}
