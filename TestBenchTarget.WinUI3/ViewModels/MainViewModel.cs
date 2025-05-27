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
        private bool _clearListRequestPending = false; // Flag pre dvojité kliknutie

        public event EventHandler<EventArgs>? ExportDataRequested;

        // Properties
        private CustomObservableCollection<DataItem> _dataItems = [];
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
        public RelayCommand ClearFormCommand { get; } = null!;
        public RelayCommand ClearListCommand { get; } = null!;
        public RelayCommand ExportDataCommand { get; }

        public MainViewModel(DataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));

            // Načítanie dát pri inicializácii 
            DataItems = _dataService.DataList;

            // Inicializácia príkazov
            ClearFormCommand = new RelayCommand(ClearForm);
            ClearListCommand = new RelayCommand(ClearList);
            AddCommand = new RelayCommand(AddData);
            LoadCommand = new RelayCommand(LoadData);
            SaveCommand = new RelayCommand(() => SaveData(showNotification: true));
            DeleteCommand = new RelayCommand(Delete, CanDelete);
            OpenFolderCommand = new RelayCommand(OpenFolder);
            ExportDataCommand = new RelayCommand(ExportData);

            // DEBUG: Overenie inicializácie príkazov
            System.Diagnostics.Debug.WriteLine($"ClearFormCommand initialized: {ClearFormCommand != null}");
            System.Diagnostics.Debug.WriteLine($"ClearListCommand initialized: {ClearListCommand != null}");

            // Test či sa dajú vykonať
            System.Diagnostics.Debug.WriteLine($"ClearFormCommand CanExecute: {ClearFormCommand?.CanExecute(null)}");
            System.Diagnostics.Debug.WriteLine($"ClearListCommand CanExecute: {ClearListCommand?.CanExecute(null)}");

            // Inicializácia načítania dát
            _ = InitializeAsync();
        }

        /// <summary>
        /// Bezpečné vymazanie kolekcie s UI thread safety
        /// </summary>
        private async Task SafeClearDataItems()
        {
            System.Diagnostics.Debug.WriteLine("SafeClearDataItems called");

            try
            {
                // Metóda 1: Pokus o štandardné Clear()
                if (DataItems.Count > 0)
                {
                    int originalCount = DataItems.Count;
                    System.Diagnostics.Debug.WriteLine($"Attempting to clear {originalCount} items");

                    try
                    {
                        DataItems.Clear();
                        System.Diagnostics.Debug.WriteLine("Standard Clear() succeeded");
                        return;
                    }
                    catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x8001010E))
                    {
                        System.Diagnostics.Debug.WriteLine("Standard Clear() failed with COM exception, trying alternative");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Standard Clear() failed: {ex.Message}");
                    }

                    // Metóda 2: Postupné odoberanie položiek
                    System.Diagnostics.Debug.WriteLine("Trying progressive removal");
                    try
                    {
                        while (DataItems.Count > 0)
                        {
                            DataItems.RemoveAt(DataItems.Count - 1);

                            // Malé oneskorenie pre UI thread
                            if (DataItems.Count % 10 == 0)
                            {
                                await Task.Delay(1);
                            }
                        }
                        System.Diagnostics.Debug.WriteLine("Progressive removal succeeded");
                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Progressive removal failed: {ex.Message}");
                    }

                    // Metóda 3: Vytvorenie novej kolekcie
                    System.Diagnostics.Debug.WriteLine("Trying collection replacement");
                    try
                    {
                        var newCollection = new CustomObservableCollection<DataItem>();
                        DataItems = newCollection;

                        // Manuálna synchronizácia s DataService
                        _dataService.ClearAllData();

                        System.Diagnostics.Debug.WriteLine("Collection replacement succeeded");
                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Collection replacement failed: {ex.Message}");
                        throw;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Collection is already empty");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"All clear methods failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Aktualizovaná ExecuteClearList metóda s bezpečným clearovaním
        /// </summary>
        private async Task ExecuteClearList()
        {
            System.Diagnostics.Debug.WriteLine("ExecuteClearList - safe version");

            try
            {
                int itemCount = DataItems.Count;

                if (itemCount == 0)
                {
                    ShowInfoBar("Info", "List is already empty.", InfoBarSeverity.Informational);
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Clearing {itemCount} items safely");

                // Použitie bezpečnej clear metódy
                await SafeClearDataItems();

                System.Diagnostics.Debug.WriteLine($"DataItems cleared successfully, final count: {DataItems.Count}");

                // Vyčistenie formulára
                ClearForm();
                System.Diagnostics.Debug.WriteLine("Form cleared");

                // Uloženie prázdneho zoznamu do súboru
                try
                {
                    SaveData(showNotification: false);
                    System.Diagnostics.Debug.WriteLine("Empty data saved to file");
                }
                catch (Exception saveEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving empty data: {saveEx.Message}");
                    ShowInfoBar("Warning", "List cleared but could not save to file.", InfoBarSeverity.Warning);
                }

                // Vyvolanie eventu
                ListClearedSuccessfully?.Invoke(this, EventArgs.Empty);
                System.Diagnostics.Debug.WriteLine("ListClearedSuccessfully event fired");

                // Zobrazenie úspešnej notifikácie
                ShowInfoBar("✅ Success", $"All {itemCount} items have been cleared from the list.", InfoBarSeverity.Success);
                System.Diagnostics.Debug.WriteLine("Success notification shown");

                Debug.WriteLine("List cleared successfully - COMPLETE");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during list clearing: {ex.Message}");
                ShowInfoBar("Error", $"Failed to clear list: {ex.Message}", InfoBarSeverity.Error);

                // Log celý stack trace pre debugging
                System.Diagnostics.Debug.WriteLine($"Full exception: {ex}");
            }
        }


        public async void ClearList()
        {
            System.Diagnostics.Debug.WriteLine("=== ClearList START ===");

            // Najprv overíme, či máme vôbec nejaké dáta na vymazanie
            if (DataItems.Count == 0)
            {
                ShowInfoBar("Info", "The list is already empty.", InfoBarSeverity.Informational);
                System.Diagnostics.Debug.WriteLine("List is already empty, nothing to clear");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Items to clear: {DataItems.Count}");

            // NOVÉ RIEŠENIE: Získanie XamlRoot cez Application.Current a type casting
            XamlRoot? xamlRoot = GetXamlRootSafely();

            if (xamlRoot != null)
            {
                System.Diagnostics.Debug.WriteLine("XamlRoot successfully obtained, showing confirmation dialog");

                try
                {
                    ContentDialog confirmDialog = new()
                    {
                        XamlRoot = xamlRoot,
                        Title = "⚠️ Confirm List Deletion",
                        Content = new TextBlock
                        {
                            Text = $"Are you sure you want to delete all {DataItems.Count} items from the list?\n\nThis action cannot be undone unless the data is saved in a JSON file.",
                            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                            FontSize = 14
                        },
                        PrimaryButtonText = "🗑️ Yes, Delete All",
                        SecondaryButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Secondary
                    };

                    System.Diagnostics.Debug.WriteLine("ContentDialog created, showing...");
                    ContentDialogResult result = await confirmDialog.ShowAsync();
                    System.Diagnostics.Debug.WriteLine($"Dialog result: {result}");

                    if (result == ContentDialogResult.Primary)
                    {
                        await ExecuteClearList();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("List clearing cancelled by user");
                        ShowInfoBar("Info", "List clearing cancelled.", InfoBarSeverity.Informational);
                    }
                }
                catch (Exception dialogEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error showing dialog: {dialogEx.Message}");
                    // Fallback na bezpečné riešenie s dvojitým kliknutím
                    await HandleClearListFallback();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Cannot get XamlRoot - using safe fallback method");
                await HandleClearListFallback();
            }

            System.Diagnostics.Debug.WriteLine("=== ClearList END ===");
        }

        /// <summary>
        /// Bezpečné získanie XamlRoot z rôznych zdrojov
        /// </summary>
        private XamlRoot? GetXamlRootSafely()
        {
            // Pokus 1: Cez uložený _xamlRoot
            if (_xamlRoot != null)
            {
                System.Diagnostics.Debug.WriteLine("XamlRoot obtained from stored _xamlRoot");
                return _xamlRoot;
            }

            // Pokus 2: Cez Application.Current
            try
            {
                if (Application.Current is App app)
                {
                    System.Diagnostics.Debug.WriteLine($"App found: {app != null}");

                    // Skúsime direct cast
                    if (app?.m_window != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"m_window type: {app.m_window.GetType().Name}");

                        // Pokús o priamy prístup k Content
                        if (app.m_window is Window window && window.Content is FrameworkElement content)
                        {
                            System.Diagnostics.Debug.WriteLine("XamlRoot obtained via Window.Content");
                            return content.XamlRoot;
                        }

                        // Alternatívny pokus - reflection
                        var contentProperty = app.m_window.GetType().GetProperty("Content");
                        if (contentProperty != null)
                        {
                            var contentValue = contentProperty.GetValue(app.m_window) as FrameworkElement;
                            if (contentValue?.XamlRoot != null)
                            {
                                System.Diagnostics.Debug.WriteLine("XamlRoot obtained via reflection");
                                return contentValue.XamlRoot;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting XamlRoot: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Fallback riešenie s bezpečným dvojitým potvrdením
        /// </summary>
        private async Task HandleClearListFallback()
        {
            if (!_clearListRequestPending)
            {
                // Prvé kliknutie - zobrazenie varovania
                _clearListRequestPending = true;
                ShowInfoBar("⚠️ Confirmation Required",
                    $"Click 'Clear list' again within 10 seconds to delete all {DataItems.Count} items. This action cannot be undone!",
                    InfoBarSeverity.Warning);

                System.Diagnostics.Debug.WriteLine("First clear request - waiting for confirmation");

                // Automatické resetovanie po 10 sekundách
                _ = Task.Run(async () =>
                {
                    await Task.Delay(10000);
                    _clearListRequestPending = false;
                    System.Diagnostics.Debug.WriteLine("Clear list request expired");
                });
            }
            else
            {
                // Druhé kliknutie - vykonanie vymazania
                _clearListRequestPending = false;
                System.Diagnostics.Debug.WriteLine("Second clear request - proceeding with deletion");
                await ExecuteClearList();
            }
        }


        public void SetXamlRoot(XamlRoot xamlRoot)
        {
            _xamlRoot = xamlRoot;
            System.Diagnostics.Debug.WriteLine($"XamlRoot set: {xamlRoot != null}");

            // Test dialógu po nastavení XamlRoot
            if (xamlRoot != null)
            {
                System.Diagnostics.Debug.WriteLine("XamlRoot is now available for dialogs");
            }
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


                // Skúsime konvertovať dátum podľa aktuálneho formátu
                if (!DateTime.TryParseExact(_selectedDateString, _dateFormat, null,
                    System.Globalization.DateTimeStyles.None, out DateTime selectedDate))
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
                var folder = await DataService.GetLocalFolderAsync();
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
                ContentDialog errorDialog = new()
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
                    using var writer = new System.IO.StreamWriter(fileStream);
                    string jsonData = JsonConvert.SerializeObject(DataItems, Newtonsoft.Json.Formatting.Indented);
                    await writer.WriteAsync(jsonData);
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
            System.Diagnostics.Debug.WriteLine("=== ClearForm START ===");
    
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
    
            ShowInfoBar(" ", "Form cleared successfully", InfoBarSeverity.Informational);
            System.Diagnostics.Debug.WriteLine("=== ClearForm END ===");
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
