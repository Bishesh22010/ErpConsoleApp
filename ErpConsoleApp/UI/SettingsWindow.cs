using Terminal.Gui;

namespace ErpConsoleApp.UI
{
    /// <summary>
    /// Wrapper window for Settings to maintain compatibility with AppMenuBar.
    /// </summary>
    public class SettingsWindow : Window
    {
        public SettingsWindow() : base("System Settings")
        {
            ColorScheme = Colors.WindowScheme;
            X = 0; Y = 0; Width = Dim.Fill(); Height = Dim.Fill(); Modal = true;

            // Wrap the SettingsView
            Add(new SettingsView());

            // Add a Close button since this is a modal window
            var btnClose = new Button("_Close")
            {
                X = Pos.Center(),
                Y = Pos.AnchorEnd(1),
                IsDefault = true,
                ColorScheme = Colors.ButtonScheme
            };
            btnClose.Clicked += () => Application.RequestStop();
            Add(btnClose);
        }
    }
}