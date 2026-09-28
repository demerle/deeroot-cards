using System;
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
            // Every Init() applies patches — if ONE of them throws, the Awake
            // aborts and every CustomCard.BuildCard below it is skipped, which
            // made ALL mod cards vanish from the pool (HarmonyX field-injection
            // spelling bug, 2026-09-28). Box each Init so a broken patch can
            // never again swallow card registration; the failure is loud.
            RunInit("DeleteCard", () => { Cards.DeleteCard.Init(); }); // applies the pick-phase hold patch
            RunInit("PortalCard", () => { Cards.PortalCard.Init(); }); // applies the bullet-portal MoveTransform patch
            RunInit("HeartCard", () => { Cards.HeartCard.Init(); }); // applies the bullet-heart MoveTransform patch
            RunInit("BouncyBallCard", () => { Cards.BouncyBallCard.Init(); }); // applies the knockback-taken CallTakeForce patch
            RunInit("SovereignCard", () => { Cards.SovereignCard.Init(); }); // applies the Sovereign friendly-fire gates
            RunInit("SimulacrumCard", () => { Cards.SimulacrumCard.Init(); }); // applies the Simulacrum friendly-fire gates
            RunInit("DynamicFieldCard", () => { Cards.DynamicFieldCard.Init(); }); // no patches — vanilla block event drives the field
            RunInit("FullCounterCard", () => { Cards.FullCounterCard.Init(); }); // doubles damage inside Block.blocked, including own bullets
            RunInit("AmpWallCard", () => { Cards.AmpWallCard.Init(); }); // applies the amp-wall MoveTransform bullet patch
            RunInit("TimeStopCard", () => { Cards.TimeStopCard.Init(); }); // pins TimeHandler statics to 0 + agent-hijack patches while a stop runs
            CustomCard.BuildCard<OverdriveCard>();
            CustomCard.BuildCard<BlinkCard>();
            CustomCard.BuildCard<DiveCard>();
            CustomCard.BuildCard<BouncyBallCard>();
            CustomCard.BuildCard<PortalCard>();
            CustomCard.BuildCard<DoubleCard>();
            CustomCard.BuildCard<DeleteCard>();
            CustomCard.BuildCard<HeartCard>();
            CustomCard.BuildCard<SovereignCard>();
            CustomCard.BuildCard<SimulacrumCard>();
            CustomCard.BuildCard<DynamicFieldCard>();
            CustomCard.BuildCard<InvisibilityCard>();
            CustomCard.BuildCard<FullCounterCard>();
            CustomCard.BuildCard<AmpWallCard>();
            CustomCard.BuildCard<ShamblesCard>();
            CustomCard.BuildCard<AbilityUpCard>();
            CustomCard.BuildCard<PowerUpCard>();
            CustomCard.BuildCard<SlowAndSteadyCard>();
            CustomCard.BuildCard<MeteorCard>();
            CustomCard.BuildCard<TimeStopCard>();
        }

        private static void RunInit(string name, Action init)
        {
            try
            {
                init();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[DEER] {name}.Init() FAILED — its patches are OFF this session, cards still registered:\n{ex}");
            }
        }

        void Start()
        {
        }
    }
}
