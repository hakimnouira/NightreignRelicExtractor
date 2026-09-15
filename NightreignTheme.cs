using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NightreignRelicExtractor
{
    public static class NightreignTheme
    {
        // 1. Color Palette: Dark Fantasy Gamer HUD
        public static readonly Color BgVoid = Color.FromArgb(11, 13, 18);        // #0B0D12
        public static readonly Color BgDark = Color.FromArgb(18, 21, 29);        // #12151D
        public static readonly Color BgCard = Color.FromArgb(24, 27, 36);        // #181B24
        public static readonly Color BgCardHover = Color.FromArgb(32, 37, 50);
        public static readonly Color BgCardSelected = Color.FromArgb(36, 42, 58);
        public static readonly Color BgHeader = Color.FromArgb(14, 16, 23);
        public static readonly Color BgInput = Color.FromArgb(20, 23, 32);

        // Aliases
        public static readonly Color CardBg = BgCard;
        public static readonly Color CardSlotBg = BgInput;

        // Subtle Borders & Dividers
        public static readonly Color BorderSubtle = Color.FromArgb(42, 47, 61);   // #2A2F3D
        public static readonly Color BorderLight = Color.FromArgb(58, 65, 84);
        public static readonly Color BorderGlow = Color.FromArgb(236, 200, 100);  // #ECC864
        public static readonly Color BorderColor = BorderSubtle;
        public static readonly Color BorderHover = BorderLight;

        // Rune Gold & Accents
        public static readonly Color GoldRune = Color.FromArgb(236, 200, 100);   // #ECC864
        public static readonly Color GoldHover = Color.FromArgb(255, 216, 117);  // #FFD875
        public static readonly Color GoldDim = Color.FromArgb(185, 150, 65);
        public static readonly Color GoldDarkBg = Color.FromArgb(45, 38, 22);

        // Text Hierarchy
        public static readonly Color TextPrimary = Color.FromArgb(240, 242, 246); // #F0F2F6
        public static readonly Color TextMuted = Color.FromArgb(143, 151, 168);   // #8F97A8
        public static readonly Color TextSubtle = Color.FromArgb(100, 107, 122);
        public static readonly Color TextHighlight = Color.FromArgb(90, 220, 160);// #5ADCA0
        public static readonly Color TextEffects = Color.FromArgb(186, 230, 253);  // #BAE6FD
        public static readonly Color TextDanger = Color.FromArgb(240, 110, 110);
        public static readonly Color TextSecondary = TextMuted;

        // Relic Affinity Colors
        public static readonly Color AffinityRed = Color.FromArgb(224, 82, 82);     // #E05252
        public static readonly Color AffinityBlue = Color.FromArgb(74, 144, 226);   // #4A90E2
        public static readonly Color AffinityYellow = Color.FromArgb(245, 197, 66); // #F5C542
        public static readonly Color AffinityGreen = Color.FromArgb(60, 179, 113);  // #3CB371
        public static readonly Color AffinityAny = Color.FromArgb(155, 114, 207);   // #9B72CF
        public static readonly Color AffinityNone = Color.FromArgb(143, 151, 168);
        public static readonly Color AccentGreen = AffinityGreen;
        public static readonly Color AccentCyan = TextEffects;

        // Fonts
        public static readonly Font FontAppTitle = new Font("Segoe UI", 13.5f, FontStyle.Bold);
        public static readonly Font FontCardTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        public static readonly Font FontSection = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        public static readonly Font FontBody = new Font("Segoe UI", 9f, FontStyle.Regular);
        public static readonly Font FontSmall = new Font("Segoe UI", 8.25f, FontStyle.Regular);
        public static readonly Font FontSmallBold = new Font("Segoe UI", 8.25f, FontStyle.Bold);
        public static readonly Font FontCode = new Font("Consolas", 9.5f, FontStyle.Regular);

        public static Color GetAffinityColor(string colorName)
        {
            if (string.IsNullOrEmpty(colorName)) return AffinityNone;
            if (string.Equals(colorName, "Red", StringComparison.OrdinalIgnoreCase)) return AffinityRed;
            if (string.Equals(colorName, "Blue", StringComparison.OrdinalIgnoreCase)) return AffinityBlue;
            if (string.Equals(colorName, "Yellow", StringComparison.OrdinalIgnoreCase)) return AffinityYellow;
            if (string.Equals(colorName, "Green", StringComparison.OrdinalIgnoreCase)) return AffinityGreen;
            if (string.Equals(colorName, "Any", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(colorName, "White", StringComparison.OrdinalIgnoreCase)) return AffinityAny;
            return AffinityNone;
        }

        public static Color GetAffinityBadgeBg(string colorName)
        {
            Color baseColor = GetAffinityColor(colorName);
            return Color.FromArgb(38, baseColor.R, baseColor.G, baseColor.B);
        }

        public static string GetAffinityIcon(string colorName)
        {
            if (string.IsNullOrEmpty(colorName)) return "⚪";
            if (string.Equals(colorName, "Red", StringComparison.OrdinalIgnoreCase)) return "🔴";
            if (string.Equals(colorName, "Blue", StringComparison.OrdinalIgnoreCase)) return "🔵";
            if (string.Equals(colorName, "Yellow", StringComparison.OrdinalIgnoreCase)) return "🟡";
            if (string.Equals(colorName, "Green", StringComparison.OrdinalIgnoreCase)) return "🟢";
            return "🟣";
        }

        public static void StyleButton(Button btn, Color normalBg, Color hoverBg, Color foreColor, Color borderColor)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = normalBg;
            btn.ForeColor = foreColor;
            btn.FlatAppearance.BorderColor = borderColor;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = hoverBg;
            btn.Cursor = Cursors.Hand;
            btn.UseMnemonic = false;
        }

        public static void StyleGoldButton(Button btn)
        {
            StyleButton(btn,
                Color.FromArgb(215, 175, 55),
                Color.FromArgb(240, 195, 75),
                Color.FromArgb(15, 16, 20),
                Color.FromArgb(255, 216, 117));
            btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        }

        public static void StyleSecondaryButton(Button btn)
        {
            StyleButton(btn,
                Color.FromArgb(32, 37, 50),
                Color.FromArgb(44, 51, 68),
                Color.FromArgb(220, 226, 240),
                BorderSubtle);
        }

        public static void StylePrimaryBlueButton(Button btn)
        {
            StyleButton(btn,
                Color.FromArgb(34, 54, 82),
                Color.FromArgb(46, 74, 112),
                Color.FromArgb(195, 225, 255),
                Color.FromArgb(64, 102, 155));
        }

        public static void StyleDangerButton(Button btn)
        {
            StyleButton(btn,
                Color.FromArgb(48, 28, 34),
                Color.FromArgb(68, 36, 44),
                Color.FromArgb(250, 140, 140),
                Color.FromArgb(110, 48, 58));
        }

        public static void StyleSuccessButton(Button btn)
        {
            StyleButton(btn,
                Color.FromArgb(30, 52, 40),
                Color.FromArgb(42, 74, 56),
                Color.FromArgb(140, 240, 180),
                Color.FromArgb(50, 110, 75));
        }

        public static void DrawCardBorder(Graphics g, Rectangle rect, Color borderColor)
        {
            using (Pen pen = new Pen(borderColor, 1))
            {
                g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
            }
        }

        public static void DrawGlowingAffinityRing(Graphics g, int centerX, int centerY, int radius, Color color, bool filled)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Outer soft glow
            using (Pen outerGlow = new Pen(Color.FromArgb(40, color.R, color.G, color.B), 4f))
            {
                g.DrawEllipse(outerGlow, centerX - radius - 2, centerY - radius - 2, (radius + 2) * 2, (radius + 2) * 2);
            }

            // Core circle
            if (filled)
            {
                using (Brush brush = new SolidBrush(Color.FromArgb(180, color.R, color.G, color.B)))
                {
                    g.FillEllipse(brush, centerX - radius, centerY - radius, radius * 2, radius * 2);
                }
            }

            // Bright rim
            using (Pen corePen = new Pen(color, 2f))
            {
                g.DrawEllipse(corePen, centerX - radius, centerY - radius, radius * 2, radius * 2);
            }
        }
    }

    public class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            this.UpdateStyles();
        }
    }

    public class DoubleBufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public DoubleBufferedFlowLayoutPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            this.UpdateStyles();
        }
    }
}
