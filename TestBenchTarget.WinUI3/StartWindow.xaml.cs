using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using Windows.System;

namespace TestBenchTarget.WinUI3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartWindow : Window
    {
        private Microsoft.UI.Dispatching.DispatcherQueueTimer? _timer;
        private Windows.Graphics.SizeInt32 _originalSize = new Windows.Graphics.SizeInt32(800, 550);
        private Windows.Graphics.PointInt32 _originalPosition;
        private Microsoft.UI.Windowing.AppWindow? _appWindow;

        public StartWindow()
        {
            this.InitializeComponent();

            // Nastav veækosù a pozÌciu okna
            SetWindowSize();

            TimeDisplay.Text = DateTime.Now.ToString("HH:mm:ss");
            DateDisplay.Text = DateTime.Now.ToString("dd.MM.yyyy");

            // Pouûitie DispatcherQueueTimer namiesto DispatcherTimer vo WinUI 3
            _timer = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                TimeDisplay.Text = DateTime.Now.ToString("HH:mm:ss");
            };
            _timer.Start();

            // Event registration for resource cleanup
            this.Closed += StartWindow_Closed;

            // Add KeyboardAccelerator for Enter
            KeyboardAccelerator enterAccelerator = new KeyboardAccelerator
            {
                Key = Windows.System.VirtualKey.Enter
            };
            enterAccelerator.Invoked += EnterAccelerator_Invoked;

            // Add the KeyboardAccelerator to the Content's KeyboardAccelerators collection
            if (this.Content is UIElement contentElement)
            {
                contentElement.KeyboardAccelerators.Add(enterAccelerator);
            }
        }

        private void SetWindowSize()
        {
            // Nastavenie n·zvu okna
            Title = "TestBench Target - Start window";

            // ZÌskanie handle okna a nastavenie veækosti
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (_appWindow == null)
            {
                throw new InvalidOperationException("Unable to get AppWindow from the current window handle.");
            }

            // Nastavenie poûadovanej veækosti okna
            var requestedSize = new Windows.Graphics.SizeInt32(900, 550);

            // ZÌskanie optim·lnej veækosti na z·klade dostupnÈho priestoru
            var optimalSize = WindowHelper.GetOptimalWindowSize(_appWindow, requestedSize, 0.8);

            // InteligentnÈ umiestnenie okna (centrovanÈ)
            WindowHelper.EnsureWindowVisibility(_appWindow, optimalSize);

            // Uloûenie aktu·lnej pozÌcie a veækosti
            _originalSize = _appWindow.Size;
            _originalPosition = _appWindow.Position;
        }

        private void ResizeWindowForDialog(bool forDialog)
        {
            if (_appWindow == null) return;

            if (forDialog)
            {
                // Zv‰Ëöenie okna pre About dialÛg s inteligentnou kontrolou
                var requestedDialogSize = new Windows.Graphics.SizeInt32(1000, 700);
                var optimalDialogSize = WindowHelper.GetOptimalWindowSize(_appWindow, requestedDialogSize, 0.9);

                // ZabezpeËenie viditeænosti aj pri v‰Ëöom okne
                WindowHelper.EnsureWindowVisibility(_appWindow, optimalDialogSize);
            }
            else
            {
                // N·vrat na pÙvodn˙ veækosù s kontrolou viditeænosti
                WindowHelper.EnsureWindowVisibility(_appWindow, _originalSize, _originalPosition);
            }
        }

        private void StartWindow_Closed(object? sender, WindowEventArgs e)
        {
            // VyËistenie zdrojov pri zatvorenÌ okna
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null!;
            }
        }

        private void OpenApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Activate();
            this.Close();
        }

        private async void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Zv‰Ëöenie okna pre lepöie zobrazenie About dialÛgu
            ResizeWindowForDialog(true);

            // Show the about dialog
            ContentDialog aboutDialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "About TestBench Target",
                CloseButtonText = "Close",
                DefaultButton = ContentDialogButton.Close,
                MaxWidth = 800
            };

            StackPanel contentPanel = new StackPanel { Margin = new Thickness(30) };

            // ZÌskanie verzie aplik·cie
            var packageVersion = GetAppVersion();

            // Logo/Icon placeholder
            TextBlock titleBlock = new TextBlock
            {
                Text = "TestBench Target",
                FontSize = 28,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Verzia
            TextBlock versionBlock = new TextBlock
            {
                Text = $"Version: {packageVersion}",
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 25),
                HorizontalAlignment = HorizontalAlignment.Center,
                Opacity = 0.8
            };

            // Popis
            TextBlock descriptionBlock = new TextBlock
            {
                Text = "A sample application designed to serve as a testing subject for developers creating monitoring, " +
                       "accessibility, or UI automation tools. This app provides predictable user interface elements and " +
                       "behaviors that developers can use to test their monitoring solutions.\n\n" +
                       "Main features:\n" +
                       "  ï Small and fast application\n" +
                       "  ï Tests opening a Windows directory\n" +
                       "  ï Simulates adding defined items to a table\n" +
                       "  ï Simple chronological display of data in a table format\n" +
                       "  ï Provides a target app for trying out monitoring and testing tools\n\n" +
                       "Ideal for developers and testers who need a reliable target application when developing tools " +
                       "to monitor and test UI interactions.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 25),
                LineHeight = 22,
                FontSize = 14
            };

            // Copyright
            TextBlock copyrightBlock = new TextBlock
            {
                Text = "Copyright © 2025 Rudolf Mendzezof",
                FontSize = 12,
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Opacity = 0.6
            };

            // Pridanie vöetk˝ch prvkov do panelu
            contentPanel.Children.Add(titleBlock);
            contentPanel.Children.Add(versionBlock);
            contentPanel.Children.Add(descriptionBlock);
            contentPanel.Children.Add(copyrightBlock);

            // Nastavenie obsahu dialÛgu
            aboutDialog.Content = contentPanel;

            // Zobrazenie dialÛgu
            await aboutDialog.ShowAsync();

            // N·vrat na pÙvodn˙ veækosù okna po zatvorenÌ dialÛgu
            ResizeWindowForDialog(false);
        }

        private async void PersonalDataProtectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Zv‰Ëöenie okna pre lepöie zobrazenie dialÛgu
            ResizeWindowForDialog(true);

            // Show the personal data protection dialog
            ContentDialog personalDataDialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "Personal Data Protection",
                PrimaryButtonText = "Learn More",
                CloseButtonText = "Close",
                MaxWidth = 800
            };

            StackPanel contentPanel = new StackPanel { Margin = new Thickness(30) };

            // Hlavn˝ text
            TextBlock mainTextBlock = new TextBlock
            {
                Text = "Privacy and Data Protection",
                FontSize = 20,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Obsah
            TextBlock contentTextBlock = new TextBlock
            {
                Text = "This application is designed with privacy in mind:\n\n" +
                       "ï No personal data collection: This application does not collect, store, or transmit any personal information.\n\n" +
                       "ï Local data storage: All application data is stored locally on your device and never shared with third parties.\n\n" +
                       "ï No network communication: The application operates entirely offline and does not connect to external servers.\n\n" +
                       "ï Transparent operation: As a testing tool, all operations are visible and predictable.\n\n" +
                       "For detailed information about data protection practices and your rights, please visit our privacy policy page.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 25),
                FontSize = 14,
                LineHeight = 22
            };

            // Link info
            TextBlock linkInfoBlock = new TextBlock
            {
                Text = "Click 'Learn More' to visit our detailed privacy policy and data protection information.",
                FontSize = 12,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            contentPanel.Children.Add(mainTextBlock);
            contentPanel.Children.Add(contentTextBlock);
            contentPanel.Children.Add(linkInfoBlock);

            personalDataDialog.Content = contentPanel;

            // Handle button clicks
            personalDataDialog.PrimaryButtonClick += PersonalDataDialog_PrimaryButtonClick;

            await personalDataDialog.ShowAsync();

            // N·vrat na pÙvodn˙ veækosù okna po zatvorenÌ dialÛgu
            ResizeWindowForDialog(false);
        }

        private async void PersonalDataDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Navigate to GitHub Pages privacy policy
            string privacyPolicyUrl = "https://rudolfmendzezof.github.io/testbench-target/privacy-policy";

            try
            {
                await Launcher.LaunchUriAsync(new Uri(privacyPolicyUrl));
            }
            catch (Exception ex)
            {
                // Fallback ak sa nepodarÌ otvoriù link
                System.Diagnostics.Debug.WriteLine($"Error opening privacy policy URL: {ex.Message}");

                // Zobraziù fallback dialÛg s URL
                ContentDialog fallbackDialog = new ContentDialog
                {
                    XamlRoot = this.Content.XamlRoot,
                    Title = "Privacy Policy",
                    Content = $"Please visit the following URL for our privacy policy:\n\n{privacyPolicyUrl}",
                    CloseButtonText = "Close"
                };
                await fallbackDialog.ShowAsync();
            }
        }

        private string GetAppVersion()
        {
            try
            {
                var package = Windows.ApplicationModel.Package.Current;
                return $"{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}.{package.Id.Version.Revision}";
            }
            catch
            {
                // Fallback pre nepakovanÈ aplik·cie
                return "1.0.0.0";
            }
        }

        private void EnterAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            // Handle the Enter key press
            NavigateToMainWindow();
            args.Handled = true;
        }

        private void NavigateToMainWindow()
        {
            // Open the application
            MainWindow mainWindow = new MainWindow();
            mainWindow.Activate();
            this.Close();
        }
    }
}
