using RingOut.Core;
using UnityEngine;

namespace RingOut.Game
{
    /// <summary>The dusk-sky colours shared by the arena, the balls, the effects and the HUD.</summary>
    public static class Palette
    {
        public static readonly Color SkyTop = Hex("#1b1442");
        public static readonly Color SkyHorizon = Hex("#e8735a");
        public static readonly Color Fog = Hex("#5a3d7c");
        public static readonly Color Floor = Hex("#3c3f6b");
        public static readonly Color FloorLine = Hex("#5a5f99");
        public static readonly Color Rock = Hex("#2a2340");
        public static readonly Color Trim = Hex("#ffb547");
        public static readonly Color Player = Hex("#f2f5ff");
        public static readonly Color PlayerStripe = Hex("#33d6ff");
        public static readonly Color Text = Hex("#f4f1ff");
        public static readonly Color Gold = Hex("#ffd84a");
        public static readonly Color Danger = Hex("#ff3b4f");
        public static readonly Color Knockback = Hex("#ff8a2b");
        public static readonly Color Smash = Hex("#ff4fd8");
        public static readonly Color Rockets = Hex("#4fe8ff");
        public static readonly Color Dust = Hex("#c9b8ff");

        public static Color Of(EnemyKind kind) => kind switch
        {
            EnemyKind.Grunt => Hex("#ff5a5f"),
            EnemyKind.Fast => Hex("#ffd23f"),
            EnemyKind.Heavy => Hex("#5b6b8c"),
            EnemyKind.Splitter => Hex("#3ddc84"),
            EnemyKind.Mini => Hex("#9df5bf"),
            EnemyKind.Boss => Hex("#9b4dff"),
            _ => Color.white,
        };

        public static Color Of(PowerUpKind kind) => kind switch
        {
            PowerUpKind.Knockback => Knockback,
            PowerUpKind.Smash => Smash,
            _ => Rockets,
        };

        public static Color WithAlpha(this Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.magenta;
    }
}
