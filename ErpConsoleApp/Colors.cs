using Terminal.Gui;

namespace ErpConsoleApp
{
    /// <summary>
    /// Utility class for defining vibrant color schemes for the application.
    /// </summary>
    public static class Colors
    {
        // Helper to create attributes with specific foreground and background colors
        private static Terminal.Gui.Attribute Make(Color fore, Color back) => new Terminal.Gui.Attribute(fore, back);

        // --- GLOBAL BASE SCHEME ---
        // Used for the background of the entire app (Application.Top)
        public static ColorScheme BaseScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.BrightCyan, Color.Blue),
            Focus = Make(Color.Blue, Color.BrightCyan),
            HotNormal = Make(Color.BrightYellow, Color.Blue),
            HotFocus = Make(Color.BrightYellow, Color.BrightCyan),
            Disabled = Make(Color.DarkGray, Color.Blue)
        };

        // --- MENU BAR SCHEME ---
        // Professional gray/white look for the top bar
        public static ColorScheme MenuScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.White, Color.Black),
            Focus = Make(Color.Black, Color.Gray),
            HotNormal = Make(Color.BrightRed, Color.Black),
            HotFocus = Make(Color.BrightRed, Color.Gray),
            Disabled = Make(Color.DarkGray, Color.Black)
        };

        // --- WINDOW & FRAME SCHEME ---
        // Used for most module windows (Inventory, Salary, etc.)
        public static ColorScheme WindowScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.White, Color.Blue),
            Focus = Make(Color.BrightCyan, Color.Black),
            HotNormal = Make(Color.BrightYellow, Color.Blue),
            HotFocus = Make(Color.BrightYellow, Color.Black),
            Disabled = Make(Color.Gray, Color.Blue)
        };

        // --- DIALOG SCHEME ---
        // Vibrant Magenta theme for popups and queries
        public static ColorScheme DialogScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.White, Color.Magenta),
            Focus = Make(Color.Magenta, Color.White),
            HotNormal = Make(Color.BrightYellow, Color.Magenta),
            HotFocus = Make(Color.BrightYellow, Color.White),
            Disabled = Make(Color.Gray, Color.Magenta)
        };

        // --- ERROR SCHEME ---
        // High-visibility Red theme for errors
        public static ColorScheme ErrorScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.White, Color.Red),
            Focus = Make(Color.Red, Color.White),
            HotNormal = Make(Color.BrightYellow, Color.Red),
            HotFocus = Make(Color.Black, Color.White),
            Disabled = Make(Color.DarkGray, Color.Red)
        };

        // --- BUTTON SCHEME ---
        // Energetic Green theme for action buttons
        public static ColorScheme ButtonScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.White, Color.Green),
            Focus = Make(Color.Black, Color.BrightGreen),
            HotNormal = Make(Color.BrightYellow, Color.Green),
            HotFocus = Make(Color.BrightYellow, Color.BrightGreen),
            Disabled = Make(Color.Gray, Color.DarkGray)
        };

        // --- TEXT INPUT SCHEME ---
        // High contrast Black on White for easy reading during data entry
        public static ColorScheme TextScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.Black, Color.White),
            Focus = Make(Color.White, Color.DarkGray),
            HotNormal = Make(Color.Blue, Color.White),
            HotFocus = Make(Color.BrightCyan, Color.DarkGray),
            Disabled = Make(Color.Gray, Color.White)
        };

        // --- RESULTS & READ-ONLY SCHEME ---
        // Glowing Yellow on Blue for calculated fields and results
        public static ColorScheme ResultScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.BrightYellow, Color.Blue),
            Focus = Make(Color.Blue, Color.BrightYellow),
            HotNormal = Make(Color.White, Color.Blue),
            HotFocus = Make(Color.Black, Color.BrightYellow),
            Disabled = Make(Color.DarkGray, Color.Blue)
        };
    }
}