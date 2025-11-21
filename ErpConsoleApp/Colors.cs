using Terminal.Gui;

namespace ErpConsoleApp
{
    /// <summary>
    /// Utility class for defining all color schemes for the application.
    /// </summary>
    public static class Colors
    {
        // Defines a set of color attributes
        private static Terminal.Gui.Attribute Make(Color fore, Color back) => new Terminal.Gui.Attribute(fore, back);

        // This is our base scheme (e.g., for the app background)
        public static ColorScheme BaseScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.White, Color.Black),
            Focus = Make(Color.Black, Color.White),
            HotNormal = Make(Color.White, Color.Black),
            HotFocus = Make(Color.Black, Color.White),
            Disabled = Make(Color.Gray, Color.Black)
        };

        // Scheme for top-level menus
        public static ColorScheme MenuScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.White, Color.Black),
            Focus = Make(Color.Black, Color.White),
            HotNormal = Make(Color.White, Color.Black),
            HotFocus = Make(Color.Black, Color.White),
            Disabled = Make(Color.Gray, Color.Black)
        };

        // Scheme for windows and frames
        public static ColorScheme WindowScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.White, Color.Black),
            Focus = Make(Color.Black, Color.White),
            HotNormal = Make(Color.White, Color.Black),
            HotFocus = Make(Color.Black, Color.White),
            Disabled = Make(Color.Gray, Color.Black)
        };

        // Scheme for dialog boxes
        public static ColorScheme DialogScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.White, Color.Black),
            Focus = Make(Color.Black, Color.White),
            HotNormal = Make(Color.White, Color.Black),
            HotFocus = Make(Color.Black, Color.White),
            Disabled = Make(Color.Gray, Color.Black)
        };

        public static ColorScheme ErrorScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.Black, Color.White),
            Focus = Make(Color.White, Color.Black),
            HotNormal = Make(Color.Black, Color.White),
            HotFocus = Make(Color.White, Color.Black),
            Disabled = Make(Color.Gray, Color.White)
        };

        public static ColorScheme ButtonScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.Black, Color.White),
            Focus = Make(Color.White, Color.Black),
            HotNormal = Make(Color.Black, Color.White),
            HotFocus = Make(Color.White, Color.Black),
            Disabled = Make(Color.Gray, Color.Black)
        };

        public static ColorScheme TextScheme { get; } = new ColorScheme()
        {
            Normal = Make(Color.White, Color.Black),
            Focus = Make(Color.Black, Color.White),
            HotNormal = Make(Color.White, Color.Black),
            HotFocus = Make(Color.Black, Color.White),
            Disabled = Make(Color.Gray, Color.Black)
        };

        // --- NEW SCHEME FOR RESULTS / READ-ONLY FIELDS ---
        public static ColorScheme ResultScheme { get; } = new ColorScheme()
        {
            // Bright Yellow text on Black background
            Normal = Make(Color.BrightYellow, Color.Black),
            Focus = Make(Color.Black, Color.BrightYellow),
            HotNormal = Make(Color.BrightYellow, Color.Black),
            HotFocus = Make(Color.Black, Color.BrightYellow),
            Disabled = Make(Color.Red, Color.Black)
        };
    }
}