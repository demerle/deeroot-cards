using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Sonigon;
using UnboundLib;
using UnboundLib.Cards;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;

namespace DeerootCards.Cards
{
    /// <summary>
    /// "Bopl Time Stop": a plain (non-one-shot) stat card with the timing
    /// mechanic of vanilla Abyssal Countdown and the prize of our Time Stop.
    /// -15% movement speed as the downside.
    ///
    /// EXACT REUSE, not a re-implementation: the vanilla Abyssal Countdown
    /// card ships an `A_AbyssalCountdown` carrier (CharacterStatModifiers.
    /// AddObjectToPlayer) that the vanilla pick pipeline instantiates as a
    /// child of the holder — the whole ring/rotator/still UI, the charge-loop
    /// SoundEvent and the serialized fill/drain/duration numbers ride along.
    /// Our Harmony postfix on `ApplyCardStats.ApplyStats` swaps the carrier's
    /// vanilla `AbyssalCountdown` script (which would make the holder a 2x-HP
    /// abyssal giant) for our own `BoplTimeStopCarrier` — the same
    /// component, same references, but the completion is a Time Stop card
    /// cast (`TimeStopState.BroadcastStart(... StopDuration)`): identical
    /// mechanic, identical readout, different prize.
    ///
    /// - Stand still (or press pure down, Abyssal's own rule) for the full
    ///   ring charge; ANY other input drains the ring.
    /// - Completing fires the full Time Stop: everyone freezes, the holder
    ///   keeps acting (the agent), bullets hang, inverted monochrome, the
    ///   stop sting — the entire Time Stop state machine, zero new state.
    /// - The ring re-arms at 0 after every stop; during any active stop the
    ///   countdown is frozen (TimeHandler.deltaTime = 0), so it never
    ///   self-triggers inside a stop.
    /// </summary>
    public class BoplTimeStopCard : CustomCard
    {
        public const string CardName = "Bopl Time Stop";

        /// <summary>The vanilla card whose injector prefab we extract (applyCardStats-verified).</summary>
        internal const string VanillaCardName = "ABYSSAL COUNTDOWN";

        // Vanilla carrier prefab cache (extracted once per session from the
        // registered vanilla card asset; an asset reference stays valid).
        private static GameObject carrierPrefab;

        /// <summary>The child-RPC key OUR carrier registers on the player's ChildRPC — vanilla's "Abyssal" key is never touched, so Abyssal Countdown holders and Bopl holders coexist.</summary>
        internal const string ChildRpcKey = "BoplTimeStop";

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // one ring per player (Time Stop family style)
            cardInfo.allowMultiple = false;
            // -15% movement speed — plain multiplier copied off the card's
            // CharacterStatModifiers (ApplyCardStats.cs:132 `*= myPlayerStats.movementSpeed`)
            statModifiers.movementSpeed = 0.85f;
            // The ring carrier rides the vanilla AddObjectToPlayer pipeline:
            // the GAME instantiates it as a child of the holder on every pick
            // (and on every deck rebuild), and ResetStats destroys it cleanly.
            statModifiers.AddObjectToPlayer = VanillaCarrierPrefab();
            UnityEngine.Debug.Log($"[DEER] BoplTimeStopCard SetupCard: movementSpeed={statModifiers.movementSpeed}, carrier={(statModifiers.AddObjectToPlayer != null ? statModifiers.AddObjectToPlayer.name : "null (ring-less fallback)")}");
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // Safety net: if the vanilla-carrier extraction came back null at
            // SetupCard time (CardChoice still empty), the card silently loses
            // its entire reason to exist. OnAddCard order vs ApplyStats is not
            // guaranteed, so re-check two frames AFTER the pick pipeline has
            // settled (Unbound recipe) and only spawn the fallback carrier if
            // nothing is attached — never on the do-both path.
            player.ExecuteAfterFrames(3, () =>
            {
                if (player == null || player.data == null)
                {
                    return; // gone between capture and check — nothing to arm
                }
                var stats = player.GetComponent<CharacterStatModifiers>();
                var hasCarrier = stats != null && stats.objectsAddedToPlayer != null &&
                                 stats.objectsAddedToPlayer.Any(o => o != null && o.GetComponent<BoplTimeStopCarrier>() != null);
                if (hasCarrier)
                {
                    return;
                }
                UnityEngine.Debug.LogWarning("[DEER] BoplTimeStop: holder carries no carrier after the pick pipeline — spawning the ring-less fallback carrier");
                BoplTimeStopCarrier.SpawnFallbackOn(player);
            });
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // Nothing: the destroyed-carrier path is vanilla-owned
            // (CharacterStatModifiers.ResetStats destroys objectsAddedToPlayer),
            // and the carrier's OnDestroy unregisters our ChildRPC key quietly.
        }

        protected override string GetTitle()
        {
            return CardName;
        }

        protected override string GetDescription()
        {
            return "Stand perfectly still and the soul ring fills; when it completes, time stops and only you may act. Any movement drains the ring and it re-charges from empty after every stop.";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Time stop",
                    amount = "4s",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = true,
                    stat = "Charges",
                    amount = "While standing still",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Movement speed",
                    amount = "-15%",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                }
            };
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Rare;
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.TechWhite;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }

        public static void Init()
        {
            var harmony = new Harmony("com.deeroot.cards.bopltimestop");
            harmony.PatchAll(typeof(BoplTimeStopPatches));
        }

        // -----------------------------------------------------------
        // Vanilla-carrier extraction (dev-notes Dynamic Field recipe):
        // find the vanilla "ABYSSAL COUNTDOWN" card in CardChoice.cards and
        // read its CharacterStatModifiers.AddObjectToPlayer — the carrier
        // prefab the vanilla pipeline itself instantiates. CardChoice is
        // populated by battle time (SetupCard re-runs on every pick-card
        // clone), so at worst the first SetupCard passes see null and the
        // card degrades gracefully to the code-built ring-less carrier via
        // BoplTimeStopPatches.ApplyStatsPostfix's fallback.
        // -----------------------------------------------------------
        internal static GameObject VanillaCarrierPrefab()
        {
            if (carrierPrefab != null)
            {
                return carrierPrefab;
            }
            var choice = CardChoice.instance;
            if (choice == null || choice.cards == null)
            {
                UnityEngine.Debug.LogWarning("[DEER] BoplTimeStop: CardChoice not populated yet — vanilla carrier not extracted this pass");
                return null;
            }
            foreach (var card in choice.cards)
            {
                if (card == null || card.cardName == null ||
                    string.Compare(card.cardName, VanillaCardName, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    continue;
                }
                var stats = card.GetComponent<CharacterStatModifiers>();
                if (stats == null || stats.AddObjectToPlayer == null)
                {
                    continue;
                }
                carrierPrefab = stats.AddObjectToPlayer;
                UnityEngine.Debug.Log($"[DEER] BoplTimeStop: extracted vanilla '{VanillaCardName}' carrier '{carrierPrefab.name}'");
                return carrierPrefab;
            }
            // Loud failure (dev-notes pitfall) so a spelling/registration
            // mismatch is immediately visible in the log.
            UnityEngine.Debug.LogError($"[DEER] BoplTimeStop: vanilla '{VanillaCardName}' not among {choice.cards.Length} registered card(s): {string.Join(" / ", choice.cards.Where(c => c != null).Select(c => c.cardName).ToArray())}");
            return null;
        }
    }

    // ---------------------------------------------------------------
    // The charge carrier: vanilla AbyssalCountdown verbatim in its ring
    // mechanics (the decompiled Update, copied line for line — fill rule
    // `direction == zero || direction == down`, drain `timeToEmpty`,
    // the 1s `startCounter` spawn grace, ring/rotator/still wiring, the
    // SoundEvent charge loop), with the abyssal-form payload REPLACED by
    // the full Time Stop card cast. `duration` and the HP-giant branches
    // are deleted outright — the completed ring is worth a stopped world.
    // ---------------------------------------------------------------
    public class BoplTimeStopCarrier : MonoBehaviour
    {
        public SoundEvent soundChargeLoop;

        private bool soundChargeIsPlaying;

        private float soundCounterLast;

        private SoundParameterIntensity soundParameterIntensity = new SoundParameterIntensity(0f, UpdateMode.Continuous);

        [Range(0f, 1f)]
        public float counter;

        public float timeToFill = 7f;

        public float timeToEmpty = 10f;

        // Copied from the vanilla component for shape parity; never activated
        // (the abyssal dark-form toggle is the ONE piece we do not carry over).
        public GameObject[] abyssalObjects;

        public ProceduralImage outerRing;

        public ProceduralImage fill;

        public Transform rotator;

        public Transform still;

        private CharacterData data;

        private float startCounter;

        // PlayerVelocity.simulated is internal (vanilla AbyssalCountdown reads
        // it from inside Assembly-CSharp) — reflection per the verified
        // PlayerVelocity.velocity pattern.
        private static readonly FieldInfo PlayerSimulatedField = AccessTools.Field(typeof(PlayerVelocity), "simulated");

        /// <summary>
        /// Degraded path when the vanilla carrier lookup failed this pass
        /// (extraction timing / cards missing): a minimal invisible carrier
        /// still gives the holder the ability — stand still, stop time —
        /// without the ring visuals or the charge-loop sound.
        /// </summary>
        internal static void SpawnFallbackOn(Player player)
        {
            var go = new GameObject("BoplTimeStopCarrier");
            go.transform.SetParent(player.transform, false);
            go.AddComponent<BoplTimeStopCarrier>();
        }

        private void Start()
        {
            soundCounterLast = counter;
            data = GetComponentInParent<CharacterData>();
            // vanilla revive lifecycle; ResetStuff is idempotent
            HealthHandler healthHandler = data.healthHandler;
            healthHandler.reviveAction = (Action)Delegate.Combine(healthHandler.reviveAction, new Action(ResetStuff));
            var childRPC = GetComponentInParent<ChildRPC>();
            if (childRPC != null && !childRPC.childRPCs.ContainsKey(BoplTimeStopCard.ChildRpcKey))
            {
                childRPC.childRPCs.Add(BoplTimeStopCard.ChildRpcKey, RPCA_Activate);
            }
            // sanity — the carrier's shipped values; never allow a div-by-zero loop
            if (timeToFill < 1f)
            {
                timeToFill = 7f;
            }
            if (timeToEmpty < 1f)
            {
                timeToEmpty = 10f;
            }
            UnityEngine.Debug.Log($"[DEER] BoplTimeStop carrier started on {(data != null && data.player != null ? data.player.data.name : "?")} (fill {timeToFill:F1}s / drain {timeToEmpty:F1}s)");
        }

        private void OnDestroy()
        {
            if (data != null && data.healthHandler != null)
            {
                data.healthHandler.reviveAction = (Action)Delegate.Remove(data.healthHandler.reviveAction, new Action(ResetStuff));
            }
            var childRPC = GetComponentInParent<ChildRPC>();
            if (childRPC != null)
            {
                childRPC.childRPCs.Remove(BoplTimeStopCard.ChildRpcKey);
            }
            SoundStop();
        }

        private void OnDisable()
        {
            SoundStop();
        }

        private void SoundPlay()
        {
            if (soundChargeLoop == null)
            {
                return; // silent fallback carrier — no SoundEvent to play
            }
            if (!soundChargeIsPlaying)
            {
                soundChargeIsPlaying = true;
                SoundManager.Instance.Play(soundChargeLoop, base.transform, soundParameterIntensity);
            }
        }

        private void SoundStop()
        {
            if (soundChargeIsPlaying)
            {
                soundChargeIsPlaying = false;
                SoundManager.Instance.Stop(soundChargeLoop, base.transform);
            }
        }

        private void ResetStuff()
        {
            SoundStop();
            counter = 0f;
        }

        private bool Simulated
        {
            get
            {
                if (data == null || data.playerVel == null)
                {
                    return true;
                }
                if (PlayerSimulatedField == null)
                {
                    return true; // unresolved reflection — vanilla default is `simulated = true`
                }
                return (bool)PlayerSimulatedField.GetValue(data.playerVel);
            }
        }

        private void Update()
        {
            if (data == null)
            {
                return;
            }
            if (soundCounterLast < counter)
            {
                SoundPlay();
            }
            else
            {
                SoundStop();
            }
            soundCounterLast = counter;
            soundParameterIntensity.intensity = counter;
            if (outerRing != null)
            {
                outerRing.fillAmount = counter;
            }
            if (fill != null)
            {
                fill.fillAmount = counter;
            }
            if (rotator != null)
            {
                rotator.localEulerAngles = new Vector3(0f, 0f, 0f - Mathf.Lerp(0f, 360f, counter));
            }
            if (!Simulated)
            {
                startCounter = 1f;
                return;
            }
            startCounter -= TimeHandler.deltaTime;
            if (startCounter > 0f)
            {
                return;
            }
            // THE Abyssal Countdown stillness rule, verbatim: stand still —
            // or hold pure down — and the ring fills; any other input drains it.
            if (data.input.direction == Vector3.zero || data.input.direction == Vector3.down)
            {
                counter += TimeHandler.deltaTime / timeToFill;
            }
            else
            {
                counter -= TimeHandler.deltaTime / timeToEmpty;
            }
            counter = Mathf.Clamp(counter, -0.1f / timeToFill, 1f);
            if (counter >= 1f && data.view.IsMine)
            {
                // The one behavioural change to Abyssal Countdown: the ChildRPC
                // completion fires the Time Stop cast (RpcTarget.All — the
                // counter reset inside lands on every client and the rings
                // stay in sync, exactly the vanilla Abyssal activation flow).
                GetComponentInParent<ChildRPC>().CallFunction(BoplTimeStopCard.ChildRpcKey);
            }
            if (counter <= 0f)
            {
                if (rotator != null)
                {
                    rotator.gameObject.SetActive(false);
                }
                if (still != null)
                {
                    still.gameObject.SetActive(false);
                }
            }
            else
            {
                if (rotator != null)
                {
                    rotator.gameObject.SetActive(true);
                }
                if (still != null)
                {
                    still.gameObject.SetActive(true);
                }
            }
        }

        // Completion, replicated to every client via the player's ChildRPC.
        // Only the OWNER's client re-broadcasts the stop (data.view.IsMine —
        // the same gate shape vanilla AbyssalCountdown uses), so the payload
        // fires exactly once worldwide; the counter re-arm lands on every
        // client synchronously because this handler IS the replicator.
        private void RPCA_Activate()
        {
            counter = 0f;
            SoundStop();
            if (data == null || data.player == null)
            {
                return;
            }
            if (TimeStopState.IsActive)
            {
                UnityEngine.Debug.Log("[DEER] BoplTimeStop: completion while a stop is already running — skipped");
                return; // never re-pin the clock mid-stop
            }
            UnityEngine.Debug.Log($"[DEER] BoplTimeStop: countdown complete for {data.player.data.name} — time stops for {TimeStopCard.StopDuration:F1}s");
            TimeStopState.BroadcastStart(data.player.playerID, TimeStopCard.StopDuration);
        }
    }

    // ---------------------------------------------------------------
    // Harmony: swap the vanilla AbyssalCountdown script for our carrier the
    // moment the vanilla pick pipeline instantiates the injector for a Bopl
    // Time Stop pick. Vanilla Abyssal Countdown picks are untouched (the
    // gate is on the pick-card's cardName), and the vanilla pitfall of a
    // second AbyssalCountdown instance's `Start` re-registering content is
    // sidestepped because our swap happens BEFORE the component ever ticks.
    // ---------------------------------------------------------------
    [HarmonyPatch]
    internal static class BoplTimeStopPatches
    {
        [HarmonyPatch(typeof(ApplyCardStats), "ApplyStats")]
        [HarmonyPostfix]
        private static void ApplyStatsPostfix(ApplyCardStats __instance, Player ___playerToUpgrade, CharacterStatModifiers ___myPlayerStats)
        {
            // Only deals with THIS card's own payload — every other card's
            // AddObjectToPlayer chain (vanilla Abyssal included) is untouched.
            if (___myPlayerStats == null || ___myPlayerStats.AddObjectToPlayer == null)
            {
                return; // this card injects no carrier
            }
            var info = __instance.GetComponent<CardInfo>();
            if (info == null || !string.Equals(info.cardName, BoplTimeStopCard.CardName, StringComparison.Ordinal))
            {
                return; // not our pick-card
            }
            if (___playerToUpgrade == null)
            {
                return;
            }
            var playerStats = ___playerToUpgrade.GetComponent<CharacterStatModifiers>();
            if (playerStats == null || playerStats.objectsAddedToPlayer == null || playerStats.objectsAddedToPlayer.Count == 0)
            {
                return;
            }
            // Exactly one carrier per ApplyStats call — lands appended last.
            var fresh = playerStats.objectsAddedToPlayer[playerStats.objectsAddedToPlayer.Count - 1];
            if (fresh == null)
            {
                return;
            }
            var vanilla = fresh.GetComponent<AbyssalCountdown>();
            if (vanilla == null)
            {
                // somehow not the vanilla shape — give the holder the silent carrier
                UnityEngine.Debug.LogWarning("[DEER] BoplTimeStop: injected carrier has no vanilla AbyssalCountdown — building a ring-less carrier instead");
                BoplTimeStopCarrier.SpawnFallbackOn(___playerToUpgrade);
                return;
            }
            var carrier = fresh.AddComponent<BoplTimeStopCarrier>();
            // All AbyssalCountdown fields are public — copy the serialized
            // readout and rules verbatim, zero reflection.
            carrier.soundChargeLoop = vanilla.soundAbyssalChargeLoop;
            carrier.counter = 0f;
            carrier.timeToFill = vanilla.timeToFill;
            carrier.timeToEmpty = vanilla.timeToEmpty;
            carrier.abyssalObjects = vanilla.abyssalObjects;
            carrier.outerRing = vanilla.outerRing;
            carrier.fill = vanilla.fill;
            carrier.rotator = vanilla.rotator;
            carrier.still = vanilla.still;
            // Disable FIRST so no lifecycle code of the vanilla script can
            // ever tick, then destroy it outright (its Start has not run yet
            // for a same-frame instantiation — nothing to unwind).
            vanilla.enabled = false;
            UnityEngine.Object.Destroy(vanilla);
            UnityEngine.Debug.Log($"[DEER] BoplTimeStop: AbyssalCountdown swapped for BoplTimeStopCarrier on {___playerToUpgrade.data.name} (fill {carrier.timeToFill:F1}s / drain {carrier.timeToEmpty:F1}s)");
        }
    }
}
