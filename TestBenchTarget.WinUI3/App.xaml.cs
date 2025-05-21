using Microsoft.UI.Xaml;

namespace TestBenchTarget.WinUI3
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initialized singleton application object. This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine($"Unhandled exception: {e.Exception}");
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new StartWindow();

            // Používajte aktuálne odpovádaúci prístup k nastaveniu témy v WinUI 3
            if (m_window.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = ElementTheme.Default; // alebo .Light alebo .Dark
            }

            m_window.Activate();
        }

        // Verejný prístup k oknu aplikácie
        public Window? m_window { get; private set; }
    }
}
