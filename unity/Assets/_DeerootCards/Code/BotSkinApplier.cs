using HarmonyLib;
using UnboundLib.Extensions;
using UnityEngine;

namespace DeerootCards.Cards
{
    /// <summary>
    /// Shared skin fix for Sovereign/Simulacrum bots: makes the bot LOOK like its
    /// master (same skin-bank color), fixing the "wrong color in DM/TDM" bug.
    ///
    /// Root cause (decompile-verified): a real player's color is assigned via
    /// `PlayerAssigner.CreatePlayer → AssignPlayerID → SetColors()` in the SAME
    /// frame as instantiation — before the prefab's `Start()`. One frame later,
    /// `PlayerSkinHandler.Init()` reads `data.player.playerID` and instantiates
    /// that player's skin-particle children from
    /// `PlayerSkinBank.GetPlayerSkinColors(playerID)`; `SetPlayerSpriteLayer.Start`
    /// wires sprite masks onto sorting layer `Player{playerID+1}`.
    ///
    /// Our bots bypass PlayerAssigner and set playerID via a deferred RPC — after
    /// Start already ran. On the owner's client vanilla never assigns an ID either
    /// (Player.Start's IsMine branch skips ReadPlayerID), so the skin particles
    /// were built from the prefab's default playerID (0). The later vanilla
    /// `SetColors()` can't repair that: it only recolors `SetTeamColor` components
    /// (outline sprites), while the body color lives in the cached
    /// `PlayerSkinParticle` children created by Init. And `ToggleSimpleSkin(true)`
    /// merely flips two bool fields — it does NOT re-run the Start-time setup.
    ///
    /// This helper rebuilds/recolors the bot's skin to its MASTER'S DISPLAY
    /// COLOR via UnboundLib's own per-player skin layer:
    ///   `colorID()` (UnboundLib) = PlayerAdditionalData.colorID, or teamID when
    ///   never assigned. PITFALL CAUGHT 2026-10: with RWF (Round With
    ///   Friends) the master's visible color is NOT skins[playerID] — UnboundLib
    ///   transpiles `Player.SetColors`, `PlayerSkinHandler.Init` and
    ///   `PlayerSkinBank.GetPlayerSkinColors` to swap `playerID` → `colorID()`,
    ///   and assigns the real player's colorID in the private room (e.g. cyan =
    ///   colorID 31 "Dark Cyan" with playerID 0). A bot with only playerID and
    ///   teamID copied falls back to skins[master.teamID] — a DIFFERENT color
    ///   (the observed orange bot next to a cyan master).
    ///   Fix: `bot.player.AssignColorID(master.colorID())` — UnboundLib's
    ///   official API — which stores the colorID on the bot AND applies the
    ///   full recolor (SetTeamColor surfaces + existing skin particles'
    ///   startColor fields). Then the particle rebuild (destroy stale → reset
    ///   `inited` → public `InitSpriteMask("Player{playerID+1}")` re-entry)
    ///   instantiates fresh particles from the colorID()'s skin prefab, which
    ///   already carries the master's colors.
    /// </summary>
    internal static class BotSkinApplier
    {
        public static void Apply(Player master, CharacterData bot)
        {
            if (master == null || bot == null || bot.player == null)
            {
                return;
            }

            // Claim the master's DISPLAY color, not just its IDs: under
            // UnboundLib/RWF every color lookup routes through colorID()
            // (transpiles into SetColors/Init/GetPlayerSkinColors), whose
            // fallback is teamID — which diverges from the master's actual
            // chosen color in DM/TDM. AssignColorID sets the bot's colorID to
            // the master's and applies the full official recolor.
            bot.player.AssignColorID(master.colorID());

            var skin = bot.GetComponentInChildren<PlayerSkinHandler>(true);
            if (skin == null)
            {
                UnityEngine.Debug.LogWarning($"[DEER] BotSkin: player {master.playerID}'s bot has no PlayerSkinHandler — SetColors only");
                return;
            }

            // We want the full player skin (not the lossy simple-skin shortcut
            // the previous implementation used, which never rebuilt anything).
            skin.simpleSkin = false;

            // Tear down whatever the wrong-ID Init built. If it never ran, this
            // is a no-op — the rebuild below idempotently produces the same
            // state Init would have left on a properly-ID'd player.
            var staleParticles = skin.GetComponentsInChildren<PlayerSkinParticle>(true);
            int staleCount = staleParticles.Length;
            foreach (var particle in staleParticles)
            {
                Object.Destroy(particle.gameObject);
            }

            // Reset Init's one-shot cache so the re-entry below actually runs.
            var initedField = AccessTools.Field(typeof(PlayerSkinHandler), "inited");
            var skinsField = AccessTools.Field(typeof(PlayerSkinHandler), "skins");
            if (initedField == null || skinsField == null)
            {
                // Loud, guaranteed-visible marker if the game update renames the
                // private fields — degrade to SetColors-only rather than throw.
                UnityEngine.Debug.LogError($"[DEER] BotSkin: PlayerSkinHandler private fields not found — bot {bot.player.playerID} not re-skinned");
                return;
            }
            initedField.SetValue(skin, false);
            skinsField.SetValue(skin, null);

            // Official re-entry (public): Init() re-instantiates the skin
            // prefab for the CURRENT colorID() (UnboundLib transpile replaces
            // the vanilla playerID lookup), then the masks are wired to the
            // bot's sorting layer — byte-for-byte what a real player gets,
            // now with the master's chosen color baked into the particles.

            int sortLayerID = UnityEngine.SortingLayer.NameToID($"Player{bot.player.playerID + 1}");
            skin.InitSpriteMask(sortLayerID);

            UnityEngine.Debug.Log(
                $"[DEER] BotSkin: bot of player {master.playerID} skinned (colorID {master.colorID()}, wrong-ID particles toasted: {staleCount}, sorting layer Player{bot.player.playerID + 1})"
            );
        }
    }
}
