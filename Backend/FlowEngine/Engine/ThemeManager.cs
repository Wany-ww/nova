using System;
using System.Windows.Media;

namespace FlowEngine.Engine
{
        /// <summary>
    /// Manages active color schemas and brushes across the application UI,
    /// triggering events to notify panels and windows when themes change.
    /// </summary>
    public static class ThemeManager
    {
                /// <summary>
        /// Gets the hex color string for title bar background.
        /// </summary>
        public static string TitleBarBg { get; private set; } = "#1e1e2e";
                /// <summary>
        /// Gets the hex color string for title bar foreground text.
        /// </summary>
        public static string TitleBarFg { get; private set; } = "#cdd6f4";
                /// <summary>
        /// Gets the hex color string for panel boundaries.
        /// </summary>
        public static string BorderColor { get; private set; } = "#313244";
                /// <summary>
        /// Gets the hex color string for panel body background.
        /// </summary>
        public static string PanelBg { get; private set; } = "#11111b";
                /// <summary>
        /// Gets the hex color string for docked window tab headers.
        /// </summary>
        public static string DialogHeaderBg { get; private set; } = "#252538";
                /// <summary>
        /// Gets the hex color string for active tab header text.
        /// </summary>
        public static string DialogHeaderFg { get; private set; } = "#f5e0dc";
                /// <summary>
        /// Gets the hex color string for inactive tab text.
        /// </summary>
        public static string TextMuted { get; private set; } = "#a6adc8";

        private static Brush CreateFrozenBrush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        public static Brush TitleBarBgBrush { get; private set; } = CreateFrozenBrush("#1e1e2e");
        public static Brush TitleBarFgBrush { get; private set; } = CreateFrozenBrush("#cdd6f4");
        public static Brush BorderBrush { get; private set; } = CreateFrozenBrush("#313244");
        public static Brush PanelBgBrush { get; private set; } = CreateFrozenBrush("#11111b");
        public static Brush DialogHeaderBgBrush { get; private set; } = CreateFrozenBrush("#252538");
        public static Brush DialogHeaderFgBrush { get; private set; } = CreateFrozenBrush("#f5e0dc");
        public static Brush TextMutedBrush { get; private set; } = CreateFrozenBrush("#a6adc8");
        public static Brush AccentBrush { get; private set; } = CreateFrozenBrush("#89b4fa");

        /// <summary>
        /// Occurs when active theme colors or brushes are changed.
        /// </summary>
        public static event Action? ThemeChanged;

        /// <summary>
        /// Parses new hex color schemes and updates TitleBar and Panel Brushes, triggering ThemeChanged.
        /// </summary>
        /// <param name="bgHex">New background hex color</param>
        /// <param name="fgHex">New foreground hex color</param>
        /// <param name="borderHex">New border boundary hex color</param>
        /// <param name="panelBgHex">Optional panel background hex color</param>
        /// <param name="dialogBgHex">Optional dialog header background hex color</param>
        /// <param name="dialogFgHex">Optional dialog header foreground hex color</param>
        /// <param name="textMutedHex">Optional inactive text hex color</param>
        public static void ApplyTheme(string bgHex, string fgHex, string borderHex, string? panelBgHex = null, string? dialogBgHex = null, string? dialogFgHex = null, string? textMutedHex = null)
        {
            try
            {
                panelBgHex ??= "#11111b";
                dialogBgHex ??= bgHex;
                dialogFgHex ??= fgHex;
                textMutedHex ??= "#a6adc8";

                var converter = new BrushConverter();
                var bg = (Brush)converter.ConvertFromString(bgHex)!;
                bg.Freeze();
                var fg = (Brush)converter.ConvertFromString(fgHex)!;
                fg.Freeze();
                var border = (Brush)converter.ConvertFromString(borderHex)!;
                border.Freeze();
                var pbg = (Brush)converter.ConvertFromString(panelBgHex)!;
                pbg.Freeze();
                var dbg = (Brush)converter.ConvertFromString(dialogBgHex)!;
                dbg.Freeze();
                var dfg = (Brush)converter.ConvertFromString(dialogFgHex)!;
                dfg.Freeze();
                var tm = (Brush)converter.ConvertFromString(textMutedHex)!;
                tm.Freeze();

                TitleBarBg = bgHex;
                TitleBarFg = fgHex;
                BorderColor = borderHex;
                PanelBg = panelBgHex;
                DialogHeaderBg = dialogBgHex;
                DialogHeaderFg = dialogFgHex;
                TextMuted = textMutedHex;

                TitleBarBgBrush = bg;
                TitleBarFgBrush = fg;
                BorderBrush = border;
                PanelBgBrush = pbg;
                DialogHeaderBgBrush = dbg;
                DialogHeaderFgBrush = dfg;
                TextMutedBrush = tm;

                ThemeChanged?.Invoke();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error applying theme: {ex.Message}");
                // Suppress: throw/logCallback/MessageBox not needed for theme applying failure
            }
        }
    }
}
