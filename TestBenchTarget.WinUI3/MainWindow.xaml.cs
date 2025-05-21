using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
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
        private MainViewModel _viewModel;
        public MainViewModel ViewModel => _viewModel;
        private CustomObservableCollection<DataItem> _dataItems = new CustomObservableCollection<DataItem>();
        private int _dateOffset = 0; // Offset from today's date for the DateNumberBox selection

        // Potrebn� pre FilePicker vo WinUI 3
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = true, SetLastError = false)]
        public static extern IntPtr GetActiveWindow();

        public MainWindow()
        {
            this.InitializeComponent();

            // Nastavenie ve�kosti a titulku okna
            SetWindowProperties();

            _viewModel = new MainViewModel(new DataService());
            _viewModel.SetXamlRoot(this.Content.XamlRoot);

            // Nastavenie DataContext pre kore�ov� Grid
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

            // Registr�cia event handlerrov
            _viewModel.ExportDataRequested += async (sender, args) => {
                await ExportDataAsync();
            };
            _viewModel.DataSavedSuccessfully += (sender, args) => {
                ShowInfoBar("Success", "Data saved successfully", InfoBarSeverity.Success);
            };
            _viewModel.ListClearedSuccessfully += (sender, args) => {
                ShowInfoBar("Success", "List cleared successfully", InfoBarSeverity.Success);
            };

            System.Diagnostics.Debug.WriteLine("ViewModel initialized");
            //System.Diagnostics.Debug.WriteLine($"DataContext set: {this.DataContext != null}");



            // Pridanie KeyDown na root element
            if (this.Content is UIElement rootElement)
            {
                rootElement.KeyDown += MainWindow_KeyDown;
            }

            // Zvy�ok kon�truktora bez KeyboardAccelerators
            DateFormatSelector.SelectionChanged += DateFormatSelector_SelectionChanged;

            // Inicializ�cia _currentDate aktu�lnym d�tumom
            _currentDate = DateTime.Now.Date;

            // Ak je SelectedDateString nastaven�, sk�ste ho parsova�
            if (!string.IsNullOrEmpty(_viewModel.SelectedDateString))
            {
                var converter = new DateFormatConverter();
                var result = converter.ConvertBack(_viewModel.SelectedDateString, typeof(DateTime), null, null);

                if (result is DateTime dateTime)
                {
                    _currentDate = dateTime;
                }
            }

            this.Closed += MainWindow_Closed;

            _viewModel.DataSavedSuccessfully += ViewModel_DataSavedSuccessfully!;
            _viewModel.ListClearedSuccessfully += ViewModel_ListClearedSuccessfully!;

            InitializeDateFormatSelector();
        }

        private void InitializeDateFormatSelector()
        {
            // Vybratie spr�vnej polo�ky pod�a aktu�lneho form�tu d�tumu
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
            // Implement�cia zobrazenia InfoBar

            InfoBar infoBar = new InfoBar
            {
                Title = title,
                Message = message,
                Severity = severity,
                IsOpen = true,
                XamlRoot = this.Content.XamlRoot
            };
            NotificationContainer.Children.Add(infoBar);
            // Nastavenie �asova�a pre automatick� zatvorenie po 5 sekund�ch
            var timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, e) =>
            {
                infoBar.IsOpen = false;
                timer.Stop();
                // Odstr�nenie z XAML po zatvoren� anim�cie
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
            Title = "TestBench Target";

            // Nastavenie ve�kosti okna pre WinUI 3
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(1024, 768));
        }

        // Handler pre zmenu form�tu d�tumu
        private void DateFormatSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is ComboBoxItem selectedItem && selectedItem.Tag is string formatString)
            {
                // Aktualiz�cia form�tu d�tumu vo ViewModeli
                _viewModel.DateFormat = formatString;

                // Aktualiz�cia zobrazenia d�tumu
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

        private void DateDisplay_PointerWheelChanged(object sender, PointerRoutedEventArgs e)        // handler for PointerWheelChanged in the TextBlock
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

        // When the user presses Enter to add an item to the table, the focus is set to the first field of the form.
        private void EnterAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            // Ensure _viewModel is of the correct type  
            if (_viewModel is MainViewModel mainViewModel)
            {
                // Execute the command when Enter is pressed  
                if (mainViewModel.AddCommand.CanExecute(null))
                {
                    mainViewModel.AddCommand.Execute(null);
                    ProcedureInput.Focus(FocusState.Programmatic); // Set focus to the first field of the form ("ProcedureInput" TextBox)
                }
            }
            args.Handled = true; // Prevent further processing of the Enter key  
        }

        private void ShowNotification(string title, string content, InfoBarSeverity severity = InfoBarSeverity.Informational)
        {
            // Vytvorenie InfoBar s automatick�m zmiznut�m po 5 sekund�ch
            InfoBar infoBar = new InfoBar
            {
                Title = title,
                Message = content,
                IsOpen = true,
                Severity = severity,
                XamlRoot = this.Content.XamlRoot
            };

            // Pridanie InfoBar do XAML rozlo�enia
            NotificationContainer.Children.Add(infoBar);

            // Nastavenie �asova�a pre automatick� zatvorenie po 5 sekund�ch
            var timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (s, e) =>
            {
                infoBar.IsOpen = false;
                timer.Stop();
                // Odstr�nenie z XAML po zatvoren� anim�cie
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

        private void ViewModel_ListClearedSuccessfully(object sender, EventArgs e)
        {
            ShowNotification("�spech", "Zoznam bol �spe�ne vy�isten�");
        }

        private void MainWindow_Closed(object sender, WindowEventArgs e)
        {
            // Zastavenie a vy�istenie �asova�a // Stop and clean the timer
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null!;
            }
        }

        /// <summary>
        /// EN - Get the default path to the JSON file
        /// SK - Z�skanie predvolenej cesty k JSON s�boru 
        /// </summary>
        /// <returns></returns>
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

            // WinUI 3 vy�aduje nastavenie hwnd pre picker
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                await _viewModel.SaveDataAsync(file.Path);
            }
        }

        private void ViewModel_DataSavedSuccessfully(object sender, EventArgs e)
        {
            ShowNotification("�spech", "D�ta boli �spe�ne ulo�en�");
        }

        private void PointsInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
            if (textBox == null) return;

            // Cursor position preservation // Zachovanie poz�cie kurzora
            int cursorPosition = textBox.SelectionStart;

            // Remove all characters except digits  // Odstr�nenie v�etk�ch znakov okrem ��sel
            StringBuilder cleanedText = new StringBuilder();
            foreach (char c in textBox.Text)
            {
                if (char.IsDigit(c))
                {
                    cleanedText.Append(c);
                }
            }

            // If is text empty, set 0 // Ak je text pr�zdny, nastav�me 0
            if (string.IsNullOrEmpty(cleanedText.ToString()))
            {
                textBox.Text = "0";
                cursorPosition = 1; // cursor after the number
            }
            // If text has changed, update it // Ak sa text zmenil, aktualizujeme ho
            else if (cleanedText.ToString() != textBox.Text)
            {
                textBox.Text = cleanedText.ToString();

                // Preserve cursor position // Obnovenie - Zachovanie poz�cie kurzora
                if (cursorPosition <= textBox.Text.Length)
                {
                    textBox.SelectionStart = cursorPosition;
                }
                else
                {
                    textBox.SelectionStart = textBox.Text.Length;
                }
            }
        }

        private void PointsInput_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox? textBox = sender as TextBox;
            if (textBox == null) return;

            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = "0";
            }
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

            // Manual synchronization if binding does not work // Manu�lna synchroniz�cia, ak by binding nefungoval
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
                e.Handled = true; // Zabr�ni� TextBoxu spracova� Enter

                // Ak je tla�idlo AddCommand povolen�, vykona� ho
                if (_viewModel.AddCommand.CanExecute(null))
                {
                    _viewModel.AddCommand.Execute(null);
                }
            }
        }
    }

    // Converter for date formatting // Konverter pre form�tovanie d�tumu
    public sealed class DateFormatConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                if (value is DateTime dateTime)
                {
                    // WinUI 3 nem� Window.Current, mus�me pou�i� in� pr�stup
                    var app = Application.Current;
                    if (app is App currentApp && currentApp.m_window is MainWindow mainWindow)
                    {
                        if (mainWindow.ViewModel != null)
                        {
                            return dateTime.ToString(mainWindow.ViewModel.DateFormat);
                        }
                    }

                    // Alternat�vne pou�i� parameter, ak bol poskytnut�
                    if (parameter is string format)
                    {
                        return dateTime.ToString(format);
                    }

                    // Z�lo�n� form�t
                    return dateTime.ToString("dd.MM.yyyy");
                }
                return value != null ? value.ToString() : string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in date conversion: {ex.Message}");
                return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object? parameter, string? language)
        {
            try
            {
                if (value is string dateString)
                {
                    // Sk�si� parseova� pod�a r�znych form�tov
                    foreach (var format in new[] { "dd.MM.yyyy", "MM/dd/yyyy", "yyyy-MM-dd" })
                    {
                        if (DateTime.TryParseExact(dateString, format, null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                        {
                            return parsedDate;
                        }
                    }

                    // Sk�si� oby�ajn� parsing ako posledn� mo�nos�
                    if (DateTime.TryParse(dateString, out DateTime result))
                    {
                        return result;
                    }
                }
                return DateTime.Now;
            }
            catch
            {
                return DateTime.Now;
            }
        }
    }
}
