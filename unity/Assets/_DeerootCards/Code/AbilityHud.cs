using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared registration point for ability-card HUD icons (the bottom-left
/// "cooldown circles"): Portal, Heart, and every future ability card.
///
/// Nothing positions itself. Each ability effect registers once
/// ("here is how you decide I'm visible, here is how you draw me") and a
/// single driver stacks every currently-visible icon left-to-right, so they
/// line up in a row instead of overlapping — CoD-zombies perk style: a new
/// icon appends at the right end and existing ones shift right; a removed one
/// recompacts the row with no holes.
///
/// Recipe for a new ability card (two lines in the effect class):
///   Awake:   AbilityHUD.Register(this, HudVisible, HudDraw);
///   OnDestroy: AbilityHUD.Unregister(this);
/// where HudVisible returns true only for the owning local player while in
/// active play, and HudDraw calls AbilityHUD.DrawCircle with the card's
/// texture/colors/caption. Keying on the effect instance means card stacks
/// never produce duplicate icons (effects are GetOrAddComponent singletons).
/// </summary>
public static class AbilityHUD
{
    // Row layout constants, shared by every icon so they line up.
    private const float Margin = 14f;
    private const float GapFraction = 0.15f;

    private class Entry
    {
        public object Owner;
        public Func<bool> Visible;
        public Action<Rect> Draw;
    }

    private static readonly List<Entry> entries = new List<Entry>();

    /// <summary>
    /// Registers one HUD icon for an ability effect. Re-registering the same
    /// owner is a no-op, so re-picking a stacked card is harmless.
    /// </summary>
    public static void Register(object owner, Func<bool> visible, Action<Rect> draw)
    {
        if (owner == null || visible == null || draw == null)
        {
            return;
        }
        if (entries.Exists(e => ReferenceEquals(e.Owner, owner)))
        {
            return; // one icon per effect instance
        }
        entries.Add(new Entry { Owner = owner, Visible = visible, Draw = draw });
        UnityEngine.Debug.Log($"[DEER] AbilityHUD: registered icon {entries.Count} ({owner.GetType().Name})");
        EnsureDriver();
    }

    /// <summary>Removes the icon of a destroyed effect. Idempotent.</summary>
    public static void Unregister(object owner)
    {
        if (owner == null)
        {
            return;
        }
        int removed = entries.RemoveAll(e => ReferenceEquals(e.Owner, owner));
        if (removed > 0)
        {
            UnityEngine.Debug.Log($"[DEER] AbilityHUD: unregistered {owner.GetType().Name}");
        }
    }

    private static void EnsureDriver()
    {
        if (driver != null)
        {
            return;
        }
        var go = new GameObject("DeerootAbilityHUD");
        UnityEngine.Object.DontDestroyOnLoad(go);
        driver = go.AddComponent<AbilityHudDriver>();
        UnityEngine.Debug.Log("[DEER] AbilityHUD: driver created");
    }

    private static AbilityHudDriver driver;

    /// <summary>
    /// Shared icon presentation: dark outline disc + inset colored disc +
    /// centered bold caption. Identical look for every ability card.
    /// </summary>
    internal static void DrawCircle(
        Rect area,
        Texture2D discTexture,
        Color readyColor,
        Color spentColor,
        bool ready,
        string caption
    )
    {
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(area, discTexture, ScaleMode.ScaleToFit);

        float border = area.width * 0.08f;
        Rect inner = new Rect(area.x + border, area.y + border, area.width - 2f * border, area.height - 2f * border);
        GUI.color = ready ? readyColor : spentColor;
        GUI.DrawTexture(inner, discTexture, ScaleMode.ScaleToFit);

        var style = GetLabelStyle();
        style.fontSize = Mathf.RoundToInt(area.width * (ready ? 0.34f : 0.38f));
        GUI.color = Color.white;
        GUI.Label(area, caption, style);
        GUI.color = Color.white;
    }

    private static GUIStyle labelStyle;

    private static GUIStyle GetLabelStyle()
    {
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
        }
        return labelStyle;
    }

    /// <summary>
    /// The only place that computes layout: stacks all registered,
    /// currently-visible icons bottom-left, left to right. Draw order (and
    /// thus slot order) is registration order = card pick order, so new cards
    /// land on the right and shift the existing row.
    /// </summary>
    private class AbilityHudDriver : MonoBehaviour
    {
        private void OnGUI()
        {
            if (entries.Count == 0)
            {
                return;
            }
            float size = Mathf.Clamp(Screen.height / 14f, 42f, 60f);
            float y = Screen.height - size - Margin;
            float x = Margin;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Visible())
                {
                    entry.Draw(new Rect(x, y, size, size));
                    x += size + size * GapFraction;
                }
            }
        }
    }
}
