using System.Linq;
using UnboundLib;
using UnboundLib.Cards;
using UnityEngine;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Dynamic Field: looks like vanilla Static Field, but the field FOLLOWS the
    /// caster (always centered on them). In exchange it is half the size and
    /// only lasts 3 seconds; damage per pulse stays at vanilla parity. Same
    /// +0.25s block cooldown as the vanilla card.
    ///
    /// Stacking: each extra copy doubles the field's size AND its pulse damage
    /// (the vanilla prefab ties both to localScale.x via Explosion
    /// scaleRadius/scaleDmg), so 2 copies = full vanilla size, 2x vanilla
    /// per-pulse damage. The size downgrade itself is damage-neutral: we
    /// divide Explosion.damage by the size factor on the spawned clone
    /// ((damage/0.5) × (0.5×stacks) = stacks → vanilla parity per copy).
    /// The +0.25s block-cooldown downside stacks by itself (vanilla additive
    /// stat application). The count is read at spawn time from
    /// CharacterData.currentCards by cardName, so removals downsize correctly.
    ///
    /// Fidelity by reuse (same philosophy as Sovereign's player-clone): at
    /// first block we extract the REAL vanilla field prefab from the vanilla
    /// "Static Field" card (CardChoice -> SpawnObjects.objectToSpawn) and spawn
    /// that, so visuals, sounds, slow and damage are exactly vanilla — down to
    /// the damage-attribution and victim-side gating the vanilla prefab does.
    ///
    /// Netcode: no RPC of our own is needed at all. Verified in the decompile,
    /// vanilla SpawnObjects.Spawn() is a plain local Object.Instantiate, and
    /// Block.BlockAction fires on every client (RpcTarget.All RPCA_DoBlock) —
    /// so every client spawns its own local copy of the field, just like
    /// vanilla. The field follows its master's transform, which is already
    /// network-synced, so the per-client copies stay aligned.
    /// </summary>
    public class DynamicFieldCard : CustomCard
    {
        public const string CardName = "Dynamic Field";

        public static void Init()
        {
            // No harmony patches — the field is plain per-client spawning on the
            // vanilla block event.
        }

        // Vanilla-card cost for the block-cooldown downside (Block:
        // (cooldown + cdAdd) * cdMultiplier).
        private const float BlockCooldownAdd = 0.25f;

        public override void SetupCard(CardInfo cardInfo, Gun gun, ApplyCardStats cardStats, CharacterStatModifiers statModifiers, Block block)
        {
            // Vanilla-Static-Field-style downside, PLUS half size and short
            // duration (those are baked into the spawned field instead).
            block.cdAdd = BlockCooldownAdd;
        }

        public override void OnAddCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            player.gameObject.GetOrAddComponent<DynamicFieldEffect>();
            UnityEngine.Debug.Log($"[DEER] DynamicField added to player {player.data.name}");
        }

        public override void OnRemoveCard(Player player, Gun gun, GunAmmo gunAmmo, CharacterData data, HealthHandler health, Gravity gravity, Block block, CharacterStatModifiers characterStats)
        {
            // Stacking: never destroy the effect component here — removing one
            // copy while another remains would silently kill the field. At zero
            // stacks the component is a harmless no-op (SpawnField reads the
            // live deck on every block and returns early).
        }

        protected override string GetTitle()
        {
            return CardName;
        }

        protected override string GetDescription()
        {
            return "Half-sized Static field that follows you — same damage, but doesnt last as long. Each extra copy doubles its size";
        }

        protected override CardInfoStat[] GetStats()
        {
            return new CardInfoStat[]
            {
                new CardInfoStat
                {
                    positive = true,
                    stat = "Field size",
                    amount = "x2 per extra card",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = true,
                    stat = "Field duration",
                    amount = "+3.0s",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                },
                new CardInfoStat
                {
                    positive = false,
                    stat = "Block cooldown",
                    amount = "+0.25s",
                    simepleAmount = CardInfoStat.SimpleAmount.Some
                }
            };
        }

        protected override CardInfo.Rarity GetRarity()
        {
            return CardInfo.Rarity.Uncommon;
        }

        protected override GameObject GetCardArt()
        {
            return null;
        }

        protected override CardThemeColor.CardThemeColorType GetTheme()
        {
            return CardThemeColor.CardThemeColorType.DefensiveBlue;
        }

        public override string GetModName()
        {
            return DeerootCards.modInitials;
        }
    }

    /// <summary>
    /// Per-player hook: spawns the (halved-per-stack, 3-second, following)
    /// field on the vanilla block event. Deliberately does NOT gate on IsMine
    /// — the block event is RpcTarget.All, and vanilla SpawnObjects semantics
    /// are "every client spawns its own local copy".
    /// </summary>
    public class DynamicFieldEffect : MonoBehaviour
    {
        private const string VanillaStaticFieldName = "static field";

        // Downgrades vs the vanilla field (size + duration only; damage per
        // pulse is kept at vanilla parity by decoupling it from the size
        // scale — see SpawnField).
        private const float SizeMultiplier = 0.5f; // half the vanilla radius
        private const float Duration = 3f;         // seconds, then despawn

        private static GameObject vanillaFieldPrefab;

        private Player player;
        private Block block;

        private void Awake()
        {
            player = GetComponent<Player>();
        }

        private void Start()
        {
            if (block == null)
            {
                block = GetComponent<Block>();
            }
            if (block != null)
            {
                block.BlockAction += OnBlockAction;
            }
            UnityEngine.Debug.Log($"[DEER] DynamicFieldEffect started on player {(player != null ? player.data.name : "?")}");
        }

        private void OnDestroy()
        {
            if (block != null)
            {
                block.BlockAction -= OnBlockAction;
            }
        }

        private void OnBlockAction(BlockTrigger.BlockTriggerType triggerType)
        {
            // Same ShieldCharge guard as Sovereign: vanilla ShieldCharge blocks
            // re-trigger for free — don't print a storm of fields off them.
            if (triggerType == BlockTrigger.BlockTriggerType.ShieldCharge)
            {
                return;
            }
            if (player == null || player.data == null || player.data.dead)
            {
                return;
            }
            if (!player.data.isPlaying)
            {
                return; // never during pick phase / game over
            }

            if (TimeStopState.IsActive)
            {
                // No field mid-stop: the vanilla field prefab's damage/slow
                // pulses run on DelayEvent coroutines (UNITY clock — the pin
                // does not freeze those), so a field spawned now would keep
                // dealing damage through the whole stop. Queue the spawn; on
                // resume it places itself at the caster and runs vanilla.
                TimeStopState.QueueOnResume("dynamic field spawn", player, () => SpawnField(player));
                UnityEngine.Debug.Log("[DEER] DynamicField: block during time stop — spawn queued for resume");
                return;
            }

            SpawnField(player);
        }

        private static void SpawnField(Player master)
        {
            // Stacks first: each extra copy of the card doubles the field's
            // size and pulse damage (both ride localScale.x in the vanilla
            // prefab — Explosion.scaleRadius/scaleDmg are set). Read at spawn
            // time so the time-stop resume path picks up the live deck; zero
            // stacks (every copy removed) means no field at all.
            int stacks = CountStacks(master);
            if (stacks <= 0)
            {
                return;
            }
            float sizeMultiplier = SizeMultiplier * stacks;

            var prefab = GetVanillaFieldPrefab();
            if (prefab == null)
            {
                return; // extraction already logged the failure
            }

            // Mirror vanilla SpawnObjects.Spawn()/ConfigureObject: plain local
            // instantiate at the (holder's) position, then attach SpawnedAttack
            // with spawner attribution so damage/kills credit the holder.
            var go = Object.Instantiate(prefab, master.transform.position, Quaternion.identity);

            var spawned = go.GetComponent<SpawnedAttack>();
            if (spawned == null)
            {
                spawned = go.AddComponent<SpawnedAttack>();
            }
            spawned.spawner = master;

            // --- Size: half vanilla per stack. PlayerInRangeTrigger reads
            // `range * root.localScale.x` when scaleWithRange is set, so scaling
            // the root scales everything only if that flag is true; patch the
            // trigger manually when it isn't to avoid a silently full-size field.
            var triggers = go.GetComponentsInChildren<PlayerInRangeTrigger>(true);
            foreach (var trigger in triggers)
            {
                if (!trigger.scaleWithRange)
                {
                    trigger.range *= sizeMultiplier;
                }
            }
            go.transform.localScale *= sizeMultiplier;

            // --- Damage: the prefab ties pulse damage to scale
            // (Explosion.scaleDmg: 1 — DoExplosionEffects multiplies damage by
            // localScale.x), so the size downgrade silently halved it too and
            // the field barely scratched anyone (playtest numbers: vanilla
            // Static Field ~100 total damage over 5s, ours ~20 over 2s).
            // Divide damage by the size factor on the spawned CLONE (the
            // prefab asset is never touched) to restore vanilla per-pulse
            // damage: (damage/0.5) × (0.5×stacks) = stacks per pulse.
            foreach (var explosion in go.GetComponentsInChildren<Explosion>(true))
            {
                explosion.damage /= SizeMultiplier;
            }

            // --- Duration: dies after 3 seconds regardless of stacks (on
            // every client, since each client owns its local copy).
            Object.Destroy(go, Duration);

            go.AddComponent<DynamicFieldFollower>().Init(master);

            UnityEngine.Debug.Log($"[DEER] DynamicField spawned for master {master.playerID}, prefab {prefab.name}, triggers {triggers.Length}, stacks {stacks} (size x{sizeMultiplier})");
        }

        // Stack count = how many copies of this card the holder's deck carries.
        // Match by cardName, never instance identity — the pick-clone is NOT the
        // instance stored in currentCards (that is sourceCard, the registered
        // prefab; see BouncyBallCard's SetupCard note). Same OrdinalIgnoreCase
        // scan as AbilityCooldowns.GetModifier.
        private static int CountStacks(Player player)
        {
            if (player == null || player.data == null || player.data.currentCards == null)
            {
                return 0;
            }
            int count = 0;
            foreach (CardInfo card in player.data.currentCards)
            {
                if (card != null && card.cardName != null &&
                    string.Equals(card.cardName, DynamicFieldCard.CardName, System.StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
            return count;
        }

        private static GameObject GetVanillaFieldPrefab()
        {
            if (vanillaFieldPrefab != null)
            {
                return vanillaFieldPrefab;
            }
            if (CardChoice.instance == null || CardChoice.instance.cards == null)
            {
                UnityEngine.Debug.LogWarning("[DEER] DynamicField: CardChoice not ready — no field this block");
                return null;
            }

            // Case-insensitive: the in-game cardName turned out to be spelled
            // "STATIC FIELD" (read straight off a Delete-card log). Multiple
            // cards may share the name (vanilla + another pack) — iterate ALL
            // matches and take the first one that actually yields a field
            // object, instead of trusting the first name hit.
            var cardMatches = CardChoice.instance.cards.Where(c => c != null && c.cardName != null && c.cardName.ToLower() == VanillaStaticFieldName).ToList();
            if (cardMatches.Count == 0)
            {
                // One-time diagnostic dump — if the case-insensitive match ever
                // misses, this shows the exact spelling / which pack owns it.
                UnityEngine.Debug.LogError("[DEER] DynamicField: no 'STATIC FIELD' card in CardChoice.cards! Card dump:");
                foreach (var name in CardChoice.instance.cards.Where(c => c != null).Select(c => c.cardName).Distinct())
                {
                    UnityEngine.Debug.Log($"[DEER]   card: {name}");
                }
                return null;
            }
            UnityEngine.Debug.Log($"[DEER] DynamicField: {cardMatches.Count} card(s) named '{VanillaStaticFieldName}' — searching each for the field object");

            foreach (var card in cardMatches)
            {
                // --- Candidate a: Block.objectsToSpawn[0] (vanilla
                // Block.Spawn() instantiates these on every block).
                var blockComp = card.GetComponentInChildren<Block>(true);
                var fieldList = blockComp != null ? blockComp.objectsToSpawn : null;
                if (fieldList != null && fieldList.Count > 0 && fieldList[0] != null)
                {
                    return ExtractFieldPrefab(fieldList[0], $"Block.objectsToSpawn[0]");
                }

                // --- Candidate b: CharacterStatModifiers.AddObjectToPlayer —
                // the vanilla recipe for "attach an effect carrier to the
                // player on card pick" (ApplyCardStats.cs:137-139). If the
                // carrier itself spawns objects via SpawnObjects, prefer that
                // spawned effect; otherwise use the carrier as the effect
                // object.
                var csm = card.GetComponentInChildren<CharacterStatModifiers>(true);
                var carrier = csm != null ? csm.AddObjectToPlayer : null;
                if (carrier != null)
                {
                    var carrierSpawner = carrier.GetComponentInChildren<SpawnObjects>(true);
                    if (carrierSpawner != null && carrierSpawner.objectToSpawn != null && carrierSpawner.objectToSpawn.Length > 0 && carrierSpawner.objectToSpawn[0] != null)
                    {
                        return ExtractFieldPrefab(carrierSpawner.objectToSpawn[0], $"CSM.AddObjectToPlayer '{carrier.name}' -> SpawnObjects.objectToSpawn[0]");
                    }
                    return ExtractFieldPrefab(carrier, $"CSM.AddObjectToPlayer '{carrier.name}' carrier");
                }

                // --- Candidate c: Gun.objectsToSpawn[].effect (on-hit objects,
                // long shot for a block card but free to check).
                var gun = card.GetComponentInChildren<Gun>(true);
                if (gun != null && gun.objectsToSpawn != null)
                {
                    foreach (var ots in gun.objectsToSpawn)
                    {
                        if (ots != null && ots.effect != null)
                        {
                            return ExtractFieldPrefab(ots.effect, $"Gun.objectsToSpawn.effect '{ots.effect.name}'");
                        }
                    }
                }

                // No candidate on this card — if it's the ONLY match, dump what
                // it carries so the next fix is a copy-paste.
                if (cardMatches.Count == 1)
                {
                    UnityEngine.Debug.LogError($"[DEER] DynamicField: matched STATIC FIELD card has no usable field prefab. Serialized-field dump:");
                    UnityEngine.Debug.Log($"[DEER]   Block.objectsToSpawn.Count: {(blockComp != null ? blockComp.objectsToSpawn.Count.ToString() : "no Block")}");
                    UnityEngine.Debug.Log($"[DEER]   CSM.AddObjectToPlayer: {(csm != null ? (csm.AddObjectToPlayer != null ? csm.AddObjectToPlayer.name : "set but null") : "no CSM")}");
                    UnityEngine.Debug.Log($"[DEER]   Gun.objectsToSpawn.Length: {(gun != null ? gun.objectsToSpawn.Length.ToString() : "no Gun")}");
                    foreach (var mono in card.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        UnityEngine.Debug.Log($"[DEER]   component: {mono.GetType().Name} on {mono.gameObject.name}");
                    }
                }
            }

            UnityEngine.Debug.LogError("[DEER] DynamicField: no candidate field prefab found on any matching card");
            return null;
        }

        private static GameObject ExtractFieldPrefab(GameObject prefab, string source)
        {
            vanillaFieldPrefab = prefab;
            var vanillaTrigger = prefab.GetComponentInChildren<PlayerInRangeTrigger>(true);
            UnityEngine.Debug.Log($"[DEER] DynamicField: extracted vanilla field prefab '{prefab.name}' via {source} (vanilla trigger range {(vanillaTrigger != null ? vanillaTrigger.range.ToString() : "n/a")}, scaleWithRange {(vanillaTrigger != null ? vanillaTrigger.scaleWithRange.ToString() : "n/a")})");
            return vanillaFieldPrefab;
        }
    }

    /// <summary>
    /// The namesake behaviour: keeps the field centered on its caster every
    /// frame (LateUpdate so movement that frame is done). Master gone/dead ⇒
    /// the field dies with them, like a Sovereign master's army does.
    /// </summary>
    public class DynamicFieldFollower : MonoBehaviour
    {
        private Player master;

        public void Init(Player masterPlayer)
        {
            master = masterPlayer;
        }

        private void LateUpdate()
        {
            if (master == null || master.data == null)
            {
                Destroy(gameObject);
                return;
            }
            if (master.data.dead)
            {
                UnityEngine.Debug.Log("[DEER] DynamicField: master died — despawning field");
                Destroy(gameObject);
                return;
            }
            var t = transform;
            var pos = master.transform.position;
            t.position = pos;
            t.rotation = Quaternion.identity;
        }
    }
}
