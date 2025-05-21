using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Dispatching;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TestBenchTarget.WinUI3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartWindow : Window
    {
        private DispatcherQueueTimer _timer;

        public StartWindow()
        {
            this.InitializeComponent();

            // Nastav veækosù a pozÌciu okna
            SetWindowSize();

            TimeDisplay.Text = DateTime.Now.ToString("HH:mm:ss");
            DateDisplay.Text = DateTime.Now.ToString("dd.MM.yyyy");

            // Pouûitie DispatcherQueueTimer namiesto DispatcherTimer vo WinUI 3
            _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                TimeDisplay.Text = DateTime.Now.ToString("HH:mm:ss");
            };
            _timer.Start();

            // Event registration for resource cleanup // Registrovaù udalosù pre vyËistenie zdrojov
            this.Closed += StartWindow_Closed;

            // Add KeyboardAccelerator for Enter // Pridanie KeyboardAccelerator pre Enter
            KeyboardAccelerator enterAccelerator = new KeyboardAccelerator
            {
                Key = Windows.System.VirtualKey.Enter
            };
            enterAccelerator.Invoked += EnterAccelerator_Invoked;
            //this.KeyboardAccelerators.Add(enterAccelerator);
        }

        private void SetWindowSize()
        {
            // Nastavenie veækosti a n·zvu okna
            Title = "Test Bench Target";

            // ZÌskanie handle okna a nastavenie veækosti
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            // Nastavenie veækosti okna
            appWindow.Resize(new Windows.Graphics.SizeInt32(600, 400));
        }

        private void StartWindow_Closed(object sender, WindowEventArgs e)
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
            // Show the about dialog // Zobraziù dialÛgovÈ okno O aplik·cii
            ContentDialog aboutDialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot, // PotrebnÈ pre WinUI 3
                CloseButtonText = "Close",
                PrimaryButtonText = "Close"
            };
            StackPanel contentPanel = new StackPanel { Margin = new Thickness(10) };

            // ZÌskanie verzie aplik·cie
            var packageVersion = GetAppVersion();

            // Verzia
            TextBlock versionBlock = new TextBlock
            {
                Text = $"Version: {packageVersion}",
                Margin = new Thickness(0, 10, 0, 20)
            };

            // Popis
            TextBlock descriptionBlock = new TextBlock
            {
                Text = "A sample application designed to serve as a testing subject for developers creating monitoring, " +
                       "accessibility, or UI automation tools. This app provides predictable user interface elements and " +
                       "behaviors that developers can use to test their monitoring solutions. For Windows 10 and newer.\n\n" +
                       "Main features:\n" +
                       "  - Small and fast application\n" +
                       "  - Tests opening a Windows directory\n" +
                       "  - Simulates adding defined items to a table\n" +
                       "  - Simple chronological display of data in a table format\n" +
                       "  - Provides a target app for trying out monitoring and testing tools\n\n" +
                       "Ideal for developers and testers who need a reliable target application when developing tools " +
                       "to monitor and test UI interactions.",
                TextWrapping = TextWrapping.Wrap
            };

            // Copyright
            TextBlock copyrightBlock = new TextBlock
            {
                Text = "Copyright © 2025 Rudolf Mendzezof",
                Margin = new Thickness(0, 20, 0, 0)
            };

            // Pridanie vöetk˝ch prvkov do panelu
            contentPanel.Children.Add(versionBlock);
            contentPanel.Children.Add(descriptionBlock);
            contentPanel.Children.Add(copyrightBlock);

            // Nastavenie obsahu dialÛgu
            aboutDialog.Content = contentPanel;

            // Zobrazenie dialÛgu
            await aboutDialog.ShowAsync();
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
            // Handle the Enter key press // Spracovaù stlaËenie kl·vesu Enter
            // Navigate to StartWindow when Enter is pressed
            NavigateToMainWindow();
            args.Handled = true;
        }

        private void NavigateToMainWindow()
        {
            // Open the application // Otvoriù aplik·ciu
            MainWindow mainWindow = new MainWindow();
            mainWindow.Activate();
            this.Close();
        }
    }
}
