using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TestBenchTarget.WinUI3.Models;
using TestBenchTarget.WinUI3.Services;
using TestBenchTarget.WinUI3.ViewModels;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace TestBenchTarget.WinUI3
{
    public sealed partial class MainWindow : Window
    {
        private DateTime _currentDate = DateTime.Now.Date;
        private DispatcherQueueTimer? _timer;
        private readonly MainViewModel _viewModel;
        public MainViewModel ViewModel => _viewModel;

        // Potrebné pre FilePicker vo WinUI 3
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = true, SetLastError = false)]
        public static extern IntPtr GetActiveWindow();

        public MainWindow()
        {
            this.InitializeComponent();

            // Nastavenie ve¾kosti a titulku okna
            SetWindowProperties();

            _viewModel = new MainViewModel(new DataService());

            // JEDNODUCHÉ RIEŠENIE: Nastavenie XamlRoot vždy cez delay
            // Táto metóda zabezpeèí, že sa XamlRoot nastaví po úplnom naèítaní okna
            _ = SetXamlRootDelayed();

            // Nastavenie DataContext pre koreòový Grid
            if (this.Content is FrameworkElement rootContent)
            {
                rootContent.DataContext = _viewModel;
                System.Diagnostics.Debug.WriteLine("DataContext set to MainViewModel for root element");
            }

            // Pridanie KeyDown na root element
            if (this.Content is UIElement rootUIElement)
            {
                rootUIElement.KeyDown += MainWindow_KeyDown;
            }

            System.Diagnostics.Debug.WriteLine("ViewModel initialized");

            // Registrácia event handlerrov
            _viewModel.ExportDataRequested += async (sender, args) => {
                await ExportDataAsync();
            };
            _viewModel.DataSavedSuccessfully += (sender, args) => {
                ShowInfoBar(" ", "Data saved successfully", InfoBarSeverity.Success);
            };

            DateFormatSelector.SelectionChanged += DateFormatSelector_SelectionChanged;

            // Inicializácia _currentDate aktuálnym dátumom
            _currentDate = DateTime.Now.Date;

            // Ak je SelectedDateString nastavený, skúste ho parsova
            if (!string.IsNullOrEmpty(_viewModel.SelectedDateString))
            {
                var converter = new DynamicDateFormatConverter();
                var result = converter.ConvertBack(_viewModel.SelectedDateString, typeof(DateTime), null, null);

                if (result is DateTime dateTime)
                {
                    _currentDate = dateTime;
                }
            }

            this.Closed += MainWindow_Closed;

            _viewModel.DataSavedSuccessfully += ViewModel_DataSavedSuccessfully;
            _viewModel.ListClearedSuccessfully += ViewModel_ListClearedSuccessfully;

            InitializeDateFormatSelector();
        }

        private async Task SetXamlRootDelayed()
        {
            // Poèkaj krátko, aby sa okno úplne naèítalo
            await Task.Delay(200); // Zvýšili sme delay

            try
            {
                System.Diagnostics.Debug.WriteLine("Attempting to set XamlRoot...");

                if (this.Content?.XamlRoot != null)
                {
                    _viewModel.SetXamlRoot(this.Content.XamlRoot);
                    System.Diagnostics.Debug.WriteLine("XamlRoot set successfully via delayed method");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("XamlRoot still null after delay - trying alternative");

                    // Alternatívny pokus po ïalšom èakaní
                    await Task.Delay(300);
                    if (this.Content?.XamlRoot != null)
                    {
                        _viewModel.SetXamlRoot(this.Content.XamlRoot);
                        System.Diagnostics.Debug.WriteLine("XamlRoot set successfully via alternative method");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("XamlRoot definitively unavailable");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting XamlRoot: {ex.Message}");
            }
        }

        private void BackToStartWindow_Click(object sender, RoutedEventArgs e)
        {
            // Vytvorenie a zobrazenie poèiatoèného okna
            StartWindow startWindow = new StartWindow();
            startWindow.Activate();
            // Zatvorenie aktuálneho okna
            this.Close();
        }



        private void InitializeDateFormatSelector()
        {
            // Vybratie správnej položky pod¾a aktuálneho formátu dátumu
            foreach (ComboBoxItem item in DateFormatSelector.Items)
            {
                if (item.Tag is string formatString && formatString == _viewModel.DateFormat)
                {
                    System.Diagnostics.Debug.WriteLine($"Selected date format: {formatString}");
                    DateFormatSelector.SelectedItem = item;
                    break;
                }
            }
        }

        // Handler pre KeyDown event
        private void MainWindow_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Enter:
                    if (_viewModel is MainViewModel mainViewModel && mainViewModel.AddCommand.CanExecute(null))
                    {
                        mainViewModel.AddCommand.Execute(null);
                        ProcedureInput.Focus(FocusState.Programmatic);
                    }
                    e.Handled = true;
                    break;

                case Windows.System.VirtualKey.Left:
                    ChangeDateByDays(-1);
                    e.Handled = true;
                    break;

                case Windows.System.VirtualKey.Right:
                    ChangeDateByDays(1);
                    e.Handled = true;
                    break;

                case Windows.System.VirtualKey.Up:
                    ChangeDateByDays(1);
                    e.Handled = true;
                    break;

                case Windows.System.VirtualKey.Down:
                    ChangeDateByDays(-1);
                    e.Handled = true;
                    break;

                case Windows.System.VirtualKey.PageUp:
                    ChangeDateByDays(30);
                    e.Handled = true;
                    break;

                case Windows.System.VirtualKey.PageDown:
                    ChangeDateByDays(-30);
                    e.Handled = true;
                    break;
            }
        }

        public void ShowInfoBar(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
        {
            // Implementácia zobrazenia InfoBar
            InfoBar infoBar = new InfoBar
            {
                Title = title,
                Message = message,
                Severity = severity,
                IsOpen = true,
                XamlRoot = this.Content.XamlRoot
            };
            NotificationContainer.Children.Add(infoBar);

            // Nastavenie èasovaèa pre automatické zatvorenie po 5 sekundách
            var timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, e) =>
            {
                infoBar.IsOpen = false;
                timer.Stop();
                // Odstránenie z XAML po zatvorení animácie
                DispatcherQueue.GetForCurrentThread().TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    if (NotificationContainer.Children.Contains(infoBar))
                    {
                        NotificationContainer.Children.Remove(infoBar);
                    }
                });
            };
            timer.Start();
        }

        private void SetWindowProperties()
        {
            // Nastavenie titulku okna
            Title = "TestBench Target - Main window";

            // Nastavenie ve¾kosti okna pre WinUI 3
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                // Požadovaná ve¾kos pre MainWindow
                var requestedSize = new Windows.Graphics.SizeInt32 { Width = 1024, Height = 868 };

                // Získanie optimálnej ve¾kosti (maximálne 85% obrazovky)
                var optimalSize = WindowHelper.GetOptimalWindowSize(appWindow, requestedSize, 0.85);

                // Inteligentné umiestnenie okna
                WindowHelper.EnsureWindowVisibility(appWindow, optimalSize);
            }
        }

        // Handler pre zmenu formátu dátumu
        private void DateFormatSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ComboBoxItem selectedItem && selectedItem.Tag is string formatString)
            {
                // Aktualizácia formátu dátumu vo ViewModeli
                _viewModel.DateFormat = formatString;

                // Aktualizácia zobrazenia dátumu
                if (DateTime.TryParse(_viewModel.SelectedDateString, out DateTime date))
                {
                    _currentDate = date;
                    _viewModel.SelectedDateString = _currentDate.ToString(formatString);
                }
            }
        }

        private void IncrementDate_Click(object sender, RoutedEventArgs e)
        {
            ChangeDateByDays(1);
        }

        private void DecrementDate_Click(object sender, RoutedEventArgs e)
        {
            ChangeDateByDays(-1);
        }

        private void DateDisplay_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            int delta = e.GetCurrentPoint(null).Properties.MouseWheelDelta;
            ChangeDateByDays(delta > 0 ? 1 : -1);
            e.Handled = true;
        }

        private void ChangeDateByDays(int days)
        {
            _currentDate = _currentDate.AddDays(days);
            _viewModel.SelectedDateString = _currentDate.ToString(_viewModel.DateFormat);
        }

        private void ShowNotification(string title, string content, InfoBarSeverity severity = InfoBarSeverity.Informational)
        {
            ShowInfoBar(title, content, severity);
        }

        private void ViewModel_ListClearedSuccessfully(object? sender, EventArgs e)
        {
            //ShowNotification("Úspech", "Zoznam bol úspešne vyèistený");
            ShowNotification(" ", "List cleared successfully", InfoBarSeverity.Success);
        }

        private void MainWindow_Closed(object sender, WindowEventArgs e)
        {
            // Zastavenie a vyèistenie èasovaèa
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null!;
            }
        }

        /// <summary>
        /// Získanie predvolenej cesty k JSON súboru 
        /// </summary>
        private async Task<string> GetDefaultJsonFilePath()
        {
            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await localFolder.CreateFileAsync("TestBenchTarget.json", CreationCollisionOption.OpenIfExists);
            return file.Path;
        }

        public async Task ExportDataAsync()
        {
            var savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.FileTypeChoices.Add("JSON File", new List<string>() { ".json" });
            savePicker.SuggestedFileName = "TestBenchTarget";

            // WinUI 3 vyžaduje nastavenie hwnd pre picker
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await _viewModel.SaveDataAsync(file.Path);
            }
        }

        private void ViewModel_DataSavedSuccessfully(object? sender, EventArgs e)
        {
            Debug.WriteLine("Data saved successfully - private void ViewModel_DataSavedSuccessfully");
        }

        private void PointsInput_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
            if (textBox == null) return;

            // Ak je hodnota "0", vymaž ju a nastav kurzor na zaèiatok
            if (textBox.Text == "0")
            {
                textBox.Text = string.Empty;
                textBox.SelectionStart = 0;
                textBox.SelectionLength = 0;
            }
            else
            {
                // Inak vyber celý text pre jednoduché prepísanie
                textBox.SelectAll();
            }

            System.Diagnostics.Debug.WriteLine($"PointsInput got focus, text: '{textBox.Text}'");
        }

        private void PointsInput_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
            if (textBox == null) return;

            // Ak je pole prázdne alebo obsahuje len whitespace, nastav na "0"
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "0";
                System.Diagnostics.Debug.WriteLine("PointsInput lost focus - set to '0' (was empty)");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"PointsInput lost focus, text: '{textBox.Text}'");
            }
        }

        private void PointsInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
            if (textBox == null) return;

            // Zachovanie pozície kurzora
            int cursorPosition = textBox.SelectionStart;

            // Odstránenie všetkých znakov okrem èísel
            StringBuilder cleanedText = new StringBuilder();
            foreach (char c in textBox.Text)
            {
                if (char.IsDigit(c))
                {
                    cleanedText.Append(c);
                }
            }

            string cleanedString = cleanedText.ToString();

            // ZMENA: Ak je text prázdny, nenastav "0" automaticky (to sa stane pri LostFocus)
            if (string.IsNullOrEmpty(cleanedString))
            {
                if (textBox.Text != string.Empty)
                {
                    textBox.Text = string.Empty;
                    textBox.SelectionStart = 0;
                }
            }
            // Ak sa text zmenil, aktualizuj ho
            else if (cleanedString != textBox.Text)
            {
                textBox.Text = cleanedString;

                // Zachovanie pozície kurzora
                if (cursorPosition <= textBox.Text.Length)
                {
                    textBox.SelectionStart = cursorPosition;
                }
                else
                {
                    textBox.SelectionStart = textBox.Text.Length;
                }
            }

            System.Diagnostics.Debug.WriteLine($"PointsInput text changed to: '{textBox.Text}'");
        }

        private void MainListView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Delete)
            {
                if (_viewModel.DeleteCommand.CanExecute(null))
                {
                    _viewModel.DeleteCommand.Execute(null);
                }
                e.Handled = true;
            }
        }

        private void MainListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = MainListView.SelectedItem as DataItem;
            System.Diagnostics.Debug.WriteLine($"XAML Selection changed: {item != null}");

            // Manuálna synchronizácia, ak by binding nefungoval
            if (item != null && _viewModel.SelectedItem != item)
            {
                _viewModel.SelectedItem = item;
                System.Diagnostics.Debug.WriteLine($"ViewModel SelectedItem manually updated");
            }
        }

        private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true; // Zabráni TextBoxu spracova Enter

                // Ak je tlaèidlo AddCommand povolené, vykona ho
                if (_viewModel.AddCommand.CanExecute(null))
                {
                    _viewModel.AddCommand.Execute(null);
                }
            }
        }
    }
}
