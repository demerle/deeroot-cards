using UnityEngine;
using UnboundLib.Cards;
using DeerootCards.Cards;

/// <summary>
/// KillStreak counter HUD: 5 small circles stacked vertically on the left
/// edge (vertically centered — clear of the bottom-left AbilityHUD row that
/// hosts ability icons). Filled circles track the local KillStreak holder's
/// current displayed streak (player skin color), gray circles are the rest.
///
/// Deliberately NOT an AbilityHUD registration: AbilityHUD's driver shares
/// one bottom-left horizontal row with all ability icons, and the counter
/// wants its own fixed left-edge column region instead.
/// </summary>
public static class KillStreakHud
{
    private const float EdgeMargin = 14f;
    private const float GapFraction = 0.18f;

    private static AbilityHudMono driver;

    public static void EnsureDriver()
    {
        if (driver != null)
        {
            return;
        }
        var go = new GameObject("KillStreakHudDriver");
        UnityEngine.Object.DontDestroyOnLoad(go);
        driver = go.AddComponent<AbilityHudMono>();
        UnityEngine.Debug.Log("[DEER] KillStreakHud driver created");
    }

    private class AbilityHudMono : MonoBehaviour
    {
        private Texture2D discTexture;

        private void OnGUI()
        {
            if (!KillStreakTracker.LocalHoldsKillStreak())
            {
                return;
            }

            int streak = KillStreakTracker.LocalDisplayedStreak();
            int circleCount = KillStreakTracker.LoopLength;

            if (discTexture == null)
            {
                discTexture = AbilityHUD.MakeCircleTexture(64, 28f, 31f);
            }

            float size = Mathf.Clamp(Screen.height / 26f, 18f, 26f);
            float gap = size * GapFraction;
            float total = circleCount * size + (circleCount - 1) * gap;

            // Bottom-up fill: the lowest circle is kill #1, gained kills stack
            // upward through the column.
            float y = (Screen.height - total) / 2f + total - size;
            float x = EdgeMargin;

            Color filled = GetLocalPlayerColor();
            for (int i = 0; i < circleCount; i++)
            {
                int killNumber = i + 1; // bottom-up: bottom circle = streak 1
                AbilityHUD.DrawCircle(
                    new Rect(x, y, size, size),
                    discTexture,
                    filled,
                    new Color(0.28f, 0.28f, 0.28f),
                    killNumber <= streak,
                    ""
                );
                y -= size + gap;
            }
        }

        private static Color GetLocalPlayerColor()
        {
            foreach (var p in PlayerManager.instance.players)
            {
                if (p == null || p.data == null || p.data.view == null || !p.data.view.IsMine)
                {
                    continue;
                }
                Color skin = PlayerSkinBank.GetPlayerSkinColors(p.playerID).color;
                return skin;
            }
            return Color.cyan;
        }
    }
}
