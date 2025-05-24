using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TestBenchTarget.WinUI3.Models;
using TestBenchTarget.WinUI3.Services;
using Windows.System;

namespace TestBenchTarget.WinUI3.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DataService _dataService;
        private XamlRoot? _xamlRoot;
        private string _dateFormat = "yyyy-MM-dd";

        public event EventHandler<EventArgs>? ExportDataRequested;

        // Properties
        private CustomObservableCollection<DataItem> _dataItems = new CustomObservableCollection<DataItem>();
        public CustomObservableCollection<DataItem> DataItems
        {
            get => _dataItems;
            set => SetProperty(ref _dataItems, value);
        }

        private DataItem? _selectedItem = null;
        public DataItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                System.Diagnostics.Debug.WriteLine($"SelectedItem setter called, new value is null: {value == null}");
                if (SetProperty(ref _selectedItem, value))
                {
                    // Notifikácia príkazu o možnej zmene stavu
                    DeleteCommand.NotifyCanExecuteChanged();

                    // Ak je vybraná položka, aktualizujte údaje vo formulári
                    if (value != null)
                    {
                        // Aktualizácia polí podľa vybranej položky
                        SelectedDateString = value.DateColumnValue.ToString(DateFormat);
                        ProcedureText = value.ProcedureColumnValue;
                        PointsText = value.PointsColumnValue.ToString();
                        DelegateText = value.DelegateColumnValue;
                    }

                    System.Diagnostics.Debug.WriteLine("NotifyCanExecuteChanged called on DeleteCommand");
                }
            }
        }

        private string _selectedDateString = DateTime.Now.ToString("yyyy-MM-dd"); 
        public string SelectedDateString
        {
            get => _selectedDateString;
            set => SetProperty(ref _selectedDateString, value);
        }

        private string _procedureText = string.Empty;
        public string ProcedureText
        {
            get => _procedureText;
            set
            {
                if (SetProperty(ref _procedureText, value))
                {
                    // KRITICKÉ: Notifikovať AddCommand o zmene
                    AddCommand.NotifyCanExecuteChanged();

                    System.Diagnostics.Debug.WriteLine($"ProcedureText changed to: '{value}'");
                }
            }
        }

        private string _pointsText = "0";
        public string PointsText
        {
            get => _pointsText;
            set => SetProperty(ref _pointsText, value);
        }

        private string _delegateText = string.Empty;
        public string DelegateText
        {
            get => _delegateText;
            set => SetProperty(ref _delegateText, value);
        }

        // Events
        public event EventHandler? ListClearedSuccessfully;
        public event EventHandler? DataSavedSuccessfully;

        // Commands
        // Pozor - tu som zmenil na konkrétne RelayCommand namiesto IRelayCommand pre jednoduchšie volanie NotifyCanExecuteChanged
        public RelayCommand AddCommand { get; }
        public RelayCommand LoadCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand OpenFolderCommand { get; }
        public RelayCommand ClearFormCommand { get; }
        public RelayCommand ClearListCommand { get; }
        public RelayCommand ExportDataCommand { get; }

        public MainViewModel(DataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));

            // Načítanie dát pri inicializácii 
            DataItems = _dataService.DataList;

            // Inicializácia príkazov
            AddCommand = new RelayCommand(AddData);
            LoadCommand = new RelayCommand(LoadData);
            SaveCommand = new RelayCommand(() => SaveData(showNotification: true)); // explicitne zobrazovať notifikáciu
            DeleteCommand = new RelayCommand(Delete, CanDelete);
            OpenFolderCommand = new RelayCommand(OpenFolder);
            ClearFormCommand = new RelayCommand(ClearForm);
            ClearListCommand = new RelayCommand(ClearList);
            ExportDataCommand = new RelayCommand(ExportData);

            // Inicializácia načítania dát
            _ = InitializeAsync();
        }

        public void SetXamlRoot(XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot;
        }

        private async Task InitializeAsync()
        {
            try
            {
                await _dataService.LoadDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing data: {ex.Message}");
                await ShowErrorInfoBar("Error initializing", ex.Message);
            }
        }

        private void AddData()
        {
            try
            {
                // Kontrola, či je procedúra zadaná
                if (string.IsNullOrWhiteSpace(ProcedureText))
                {
                    ShowInfoBar("Warning", "Please enter a procedure name.");
                    return;
                }

                DateTime selectedDate;

                // Skúsime konvertovať dátum podľa aktuálneho formátu
                if (!DateTime.TryParseExact(_selectedDateString, _dateFormat, null,
                    System.Globalization.DateTimeStyles.None, out selectedDate))
                {
                    selectedDate = DateTime.Now.Date;
                }

                // Vytvorenie nového DataItem z vstupných hodnôt
                var newItem = new DataItem
                {
                    DateColumnValue = selectedDate,
                    ProcedureColumnValue = ProcedureText,
                    // Parse PointsText to integer with fallback
                    PointsColumnValue = string.IsNullOrEmpty(PointsText) || !int.TryParse(PointsText, out int points) ? 0 : points,
                    DelegateColumnValue = DelegateText
                };

                // DÔLEŽITÉ: Nastaviť FormattedDate podľa aktuálneho formátu
                newItem.UpdateFormattedDate(_dateFormat);

                // Ak je vybraná existujúca položka, aktualizujeme ju namiesto pridania novej
                if (SelectedItem != null && DataItems.Contains(SelectedItem))
                {
                    int index = DataItems.IndexOf(SelectedItem);
                    DataItems[index] = newItem;
                    ShowInfoBar(" ", "Item updated.");
                }
                else
                {
                    // Pridanie položky do kolekcie
                    DataItems.Add(newItem);
                    ShowInfoBar(" ", "Item added.");
                }

                // Po pridaní automaticky uložíme dáta
                SaveData(showNotification: false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding item: {ex.Message}");
                ShowInfoBar("Error", $"An error occurred while adding the item: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private async void LoadData()
        {
            try
            {
                await _dataService.LoadDataAsync();
                ShowInfoBar("", $"Loaded {DataItems.Count} položiek.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading data: {ex.Message}");
                await ShowErrorInfoBar("Error loading data", ex.Message);
            }
        }

        private void Delete()
        {
            try
            {
                Debug.WriteLine($"Delete called, SelectedItem: {SelectedItem != null}");

                if (SelectedItem != null)
                {
                    DataItems.Remove(SelectedItem);
                    Debug.WriteLine("Item removed from DataItems");
                    ShowInfoBar(" ", "Item deleted.");

                    // Vynulovanie vstupných polí po odstránení
                    ClearForm();

                    // Uloženie dát po zmazaní BEZ notifikácie
                    SaveData(showNotification: false);

                    Debug.WriteLine("Item deleted successfully");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting item: {ex.Message}");
                ShowInfoBar("Error", $"An error occurred while deleting the item: {ex.Message}", InfoBarSeverity.Error);
            }
        }

        private bool CanDelete()
        {
            return SelectedItem != null;
        }

        private async void OpenFolder()
        {
            try
            {
                var folder = await _dataService.GetLocalFolderAsync();
                await Launcher.LaunchFolderAsync(folder);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening folder: {ex.Message}");
                await ShowErrorInfoBar("Error opening folder", ex.Message);
            }
        }

        private static void ShowInfoBar(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational)
        {
            // InfoBar sa zobrazí prostredníctvom funkcie v MainWindow
            // Kontrola či App.Current je typu App a bezpečný prístup k m_window
            if (Application.Current is App app && app.m_window is MainWindow mainWindow)
            {
                mainWindow.ShowInfoBar(title, message, severity);
            }
        }

        private async Task ShowErrorInfoBar(string title, string message)
        {
            // InfoBar sa zobrazí prostredníctvom eventu v MainWindow
            Debug.WriteLine($"{title}: {message}");

            ShowInfoBar(title, message, InfoBarSeverity.Error);

            // Ak je ContentDialog nevyhnutný, použijeme XamlRoot
            if (_xamlRoot != null)
            {
                ContentDialog errorDialog = new ContentDialog
                {
                    XamlRoot = _xamlRoot,
                    Title = title,
                    Content = message,
                    PrimaryButtonText = "OK"
                };

                await errorDialog.ShowAsync();
            }
        }

        // SaveDataAsync - export do externého súboru
        public async Task<bool> SaveDataAsync(string filePath)
        {
            try
            {
                // V WinUI 3 použiť priamo súborový systém
                using (var fileStream = System.IO.File.Create(filePath))
                {
                    using (var writer = new System.IO.StreamWriter(fileStream))
                    {
                        string jsonData = JsonConvert.SerializeObject(DataItems, Newtonsoft.Json.Formatting.Indented);
                        await writer.WriteAsync(jsonData);
                    }
                }

                //ShowInfoBar("Úspech", $"Dáta boli exportované do: {System.IO.Path.GetFileName(filePath)}");
                ShowInfoBar("Succes", filePath, InfoBarSeverity.Success);
                Debug.WriteLine($"Data saved to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                await ShowErrorInfoBar("Error while saving data", ex.Message).ConfigureAwait(false);
                Debug.WriteLine($"Error in SaveDataAsync: {ex.Message}");
                return false;
            }
        }

        private void ClearForm()
        {
            // Vyčistiť výber
            SelectedItem = null;
            Debug.WriteLine("SelectedItem cleared");

            // Vyčistiť textové polia
            ProcedureText = string.Empty;
            Debug.WriteLine("ProcedureText cleared");
            PointsText = "0";
            Debug.WriteLine("PointsText cleared");
            DelegateText = string.Empty;
            Debug.WriteLine("DelegateText cleared");

            // Resetovať dátum na dnešný
            SelectedDateString = DateTime.Now.ToString(_dateFormat);
            Console.WriteLine("ClearForm called, notifying DeleteCommand");

            // Aktualizovať stav DeleteCommand
            DeleteCommand.NotifyCanExecuteChanged();
            Console.WriteLine("DeleteCommand notified");
            AddCommand.NotifyCanExecuteChanged();
            Console.WriteLine("AddCommand notified");
        }


        private async void SaveData(bool showNotification = true)
        {
            try
            {
                bool success = await _dataService.SaveDataAsync();
                if (success)
                {
                    Debug.WriteLine("Data saved successfully.");
                    if (showNotification)
                    {
                        DataSavedSuccessfully?.Invoke(this, EventArgs.Empty);
                    }
                }
                else
                {
                    await ShowErrorInfoBar("Error saving data", "Failed to save data.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving data: {ex.Message}");
                await ShowErrorInfoBar("Error while saving data", ex.Message);
            }
        }

        private async void ClearList()
        {
            // Získať XamlRoot priamo z MainWindow - najspoľahlivejšie riešenie
            XamlRoot? xamlRoot = null;

            if (Application.Current is App app && app.m_window is MainWindow mainWindow)
            {
                xamlRoot = mainWindow.Content?.XamlRoot;
                System.Diagnostics.Debug.WriteLine($"Getting XamlRoot from MainWindow: {xamlRoot != null}");
            }

            if (xamlRoot != null)
            {
                System.Diagnostics.Debug.WriteLine("Showing confirmation dialog");

                ContentDialog confirmDialog = new ContentDialog
                {
                    XamlRoot = xamlRoot,
                    Title = "Confirm deletion",
                    Content = "Are you sure you want to clear the entire list? This action cannot be undone if the data is not stored in a JSON file.",
                    PrimaryButtonText = "Yes, clear the list",
                    CloseButtonText = "Cancel"
                };

                ContentDialogResult result = await confirmDialog.ShowAsync();
                System.Diagnostics.Debug.WriteLine($"Dialog result: {result}");

                if (result == ContentDialogResult.Primary)
                {
                    DataItems.Clear();
                    ClearForm();
                    SaveData(showNotification: false);
                    Debug.WriteLine("List cleared after confirmation");
                    ListClearedSuccessfully?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    Debug.WriteLine("List clearing cancelled by user");
                }
            }
            else
            {
                // Backup - ak nemôžeme získať XamlRoot, zobrazíme aspoň InfoBar
                System.Diagnostics.Debug.WriteLine("ERROR: Cannot get XamlRoot for confirmation dialog");
                ShowInfoBar("Warning", "Cannot show confirmation dialog. Please try again.", InfoBarSeverity.Warning);
            }
        }

        private async void ExportData()
        {
            try
            {
                // Vyvolanie udalosti pre export dát
                ExportDataRequested?.Invoke(this, EventArgs.Empty);
                Debug.WriteLine("ExportDataRequested event fired");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error exporting data: {ex.Message}");
                await ShowErrorInfoBar("Data export error", $"An error occurred while exporting data: {ex.Message}");
            }
        }

        // Metódy pre prácu s dátumom
        public void IncrementDate()
        {
            try
            {
                if (DateTime.TryParse(SelectedDateString, out DateTime date))
                {
                    System.Diagnostics.Debug.WriteLine($"IncrementDate called, current date: {date.ToString(_dateFormat)}");
                    date = date.AddDays(1);
                    SelectedDateString = date.ToString(_dateFormat);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error incrementing date: {ex.Message}");
            }
        }

        public void DecrementDate()
        {
            try
            {
                if (DateTime.TryParse(SelectedDateString, out DateTime date))
                {
                    date = date.AddDays(-1);
                    SelectedDateString = date.ToString(_dateFormat);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error decrementing date: {ex.Message}");
            }
        }

        // V MainViewModel.cs pridajte túto metódu a upravte DateFormat setter:

        public string DateFormat
        {
            get => _dateFormat;
            set
            {
                if (SetProperty(ref _dateFormat, value))
                {
                    // Ak sa zmenil formát, preparsujeme existujúci dátum a naformátujeme ho na nový formát
                    if (DateTime.TryParseExact(_selectedDateString, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date) ||
                        DateTime.TryParseExact(_selectedDateString, "MM/dd/yyyy", null, System.Globalization.DateTimeStyles.None, out date) ||
                        DateTime.TryParseExact(_selectedDateString, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out date))
                    {
                        SelectedDateString = date.ToString(value);
                    }

                    // NOVÉ RIEŠENIE: Vytvoriť nové objekty s aktualizovaným FormattedDate
                    RefreshDataItemsFormatting();

                    // Debug výpis
                    System.Diagnostics.Debug.WriteLine($"DateFormat changed to: {value}");
                }
            }
        }

        /// <summary>
        /// Refresh all items to update their formatted date display
        /// </summary>
        private void RefreshDataItemsFormatting()
        {
            // Aktualizuje FormattedDate pre všetky existujúce položky
            foreach (var item in DataItems)
            {
                item.UpdateFormattedDate(_dateFormat);
            }

            System.Diagnostics.Debug.WriteLine($"Refreshed {DataItems.Count} items with new date format: {_dateFormat}");
        }
    }
}
