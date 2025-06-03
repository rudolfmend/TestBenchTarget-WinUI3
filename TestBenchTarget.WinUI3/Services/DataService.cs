using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TestBenchTarget.WinUI3.Models;
using Windows.Storage;
using System.Diagnostics;

namespace TestBenchTarget.WinUI3.Services
{
    public class DataService
    {
        private readonly CustomObservableCollection<DataItem> _dataList = [];
        private const string _fileName = "TestBenchTarget.json";
        private const string _appFolderName = "TestBenchTarget";

        // Verejné vlastnosti
        public CustomObservableCollection<DataItem> DataList => _dataList;

        // Udalosti
        public event EventHandler? DataLoaded;
        public event EventHandler? DataSaved;
        public event EventHandler<DataErrorEventArgs>? DataError;

        // NOVÉ: Fallback storage path pre Store verziu
        private string? _fallbackStoragePath = null;

        // Konštruktor
        public DataService()
        {
            // Inicializácia fallback path
            InitializeFallbackStorage();
        }

        /// <summary>
        /// Inicializácia náhradného úložiska pre Store verziu
        /// </summary>
        private void InitializeFallbackStorage()
        {
            try
            {
                // Pokus o získanie štandardnej LocalFolder cesty
                var localPath = ApplicationData.Current?.LocalFolder?.Path;
                if (!string.IsNullOrEmpty(localPath))
                {
                    _fallbackStoragePath = localPath;
                    Debug.WriteLine($"Using ApplicationData.Current.LocalFolder: {_fallbackStoragePath}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ApplicationData.Current.LocalFolder failed: {ex.Message}");
            }

            // Fallback 1: Temp folder
            try
            {
                _fallbackStoragePath = Path.Combine(Path.GetTempPath(), _appFolderName);
                Directory.CreateDirectory(_fallbackStoragePath);
                Debug.WriteLine($"Using temp folder fallback: {_fallbackStoragePath}");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Temp folder fallback failed: {ex.Message}");
            }

            // Fallback 2: User profile
            try
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                _fallbackStoragePath = Path.Combine(userProfile, _appFolderName);
                Directory.CreateDirectory(_fallbackStoragePath);
                Debug.WriteLine($"Using user profile fallback: {_fallbackStoragePath}");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"User profile fallback failed: {ex.Message}");
            }

            // Posledný fallback: AppData Local
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _fallbackStoragePath = Path.Combine(localAppData, _appFolderName);
                Directory.CreateDirectory(_fallbackStoragePath);
                Debug.WriteLine($"Using LocalApplicationData fallback: {_fallbackStoragePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"All storage fallbacks failed: {ex.Message}");
                _fallbackStoragePath = Path.GetTempPath(); // Posledná šanca
            }
        }

        /// <summary>
        /// Bezpečné získanie storage path
        /// </summary>
        private string GetSafeStoragePath()
        {
            return _fallbackStoragePath ?? Path.GetTempPath();
        }

        /// <summary>
        /// HLAVNÁ OPRAVA: Store-safe SaveDataAsync
        /// </summary>
        public async Task<bool> SaveDataAsync()
        {
            try
            {
                // Pokus o štandardný WinUI prístup
                if (await TrySaveDataWinUIAsync())
                {
                    Debug.WriteLine("Data saved using WinUI method");
                    DataSaved?.Invoke(this, EventArgs.Empty);
                    return true;
                }

                // Fallback na .NET File API
                Debug.WriteLine("WinUI save failed, using .NET File API fallback");
                return await SaveDataFallbackAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveDataAsync: {ex.Message}");
                OnDataError("Error saving data", ex);
                return false;
            }
        }

        /// <summary>
        /// Pokus o štandardné WinUI uloženie
        /// </summary>
        private async Task<bool> TrySaveDataWinUIAsync()
        {
            try
            {
                if (ApplicationData.Current?.LocalFolder != null)
                {
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    StorageFile file = await localFolder.CreateFileAsync(_fileName,
                                        CreationCollisionOption.ReplaceExisting);
                    string jsonData = JsonConvert.SerializeObject(_dataList, Formatting.Indented);
                    await FileIO.WriteTextAsync(file, jsonData);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WinUI save method failed: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Fallback uloženie pomocou .NET File API
        /// </summary>
        private async Task<bool> SaveDataFallbackAsync()
        {
            try
            {
                string filePath = Path.Combine(GetSafeStoragePath(), _fileName);
                Debug.WriteLine($"Saving to fallback path: {filePath}");

                string jsonData = JsonConvert.SerializeObject(_dataList, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, jsonData);

                DataSaved?.Invoke(this, EventArgs.Empty);
                Debug.WriteLine("Fallback save successful");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fallback save failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// HLAVNÁ OPRAVA: Store-safe LoadDataAsync
        /// </summary>
        public async Task<bool> LoadDataAsync()
        {
            try
            {
                // Pokus o štandardný WinUI prístup
                if (await TryLoadDataWinUIAsync())
                {
                    Debug.WriteLine("Data loaded using WinUI method");
                    OnDataLoaded();
                    return true;
                }

                // Fallback na .NET File API
                Debug.WriteLine("WinUI load failed, using .NET File API fallback");
                return await LoadDataFallbackAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadDataAsync: {ex.Message}");
                OnDataError("Error loading data", ex);
                return false;
            }
        }

        /// <summary>
        /// Pokus o štandardné WinUI načítanie
        /// </summary>
        private async Task<bool> TryLoadDataWinUIAsync()
        {
            try
            {
                if (ApplicationData.Current?.LocalFolder != null)
                {
                    StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                    try
                    {
                        StorageFile file = await localFolder.GetFileAsync(_fileName);
                        string jsonData = await FileIO.ReadTextAsync(file);
                        await ProcessLoadedJsonData(jsonData);
                        return true;
                    }
                    catch (FileNotFoundException)
                    {
                        // Súbor neexistuje - normálne pri prvom spustení
                        _dataList.Clear();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WinUI load method failed: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Fallback načítanie pomocou .NET File API
        /// </summary>
        private async Task<bool> LoadDataFallbackAsync()
        {
            try
            {
                string filePath = Path.Combine(GetSafeStoragePath(), _fileName);
                Debug.WriteLine($"Loading from fallback path: {filePath}");

                if (!File.Exists(filePath))
                {
                    Debug.WriteLine("Fallback file doesn't exist - first run");
                    _dataList.Clear();
                    OnDataLoaded();
                    return true;
                }

                string jsonData = await File.ReadAllTextAsync(filePath);
                await ProcessLoadedJsonData(jsonData);

                OnDataLoaded();
                Debug.WriteLine("Fallback load successful");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fallback load failed: {ex.Message}");
                _dataList.Clear(); // Zabezpečenie prázdnej kolekcie pri chybe
                OnDataLoaded();
                return true; // Vrátime true aby aplikácia fungovala aj pri chybe
            }
        }

        /// <summary>
        /// Spracovanie načítaných JSON dát - OPRAVA: UI thread safe
        /// </summary>
        private async Task ProcessLoadedJsonData(string jsonData)
        {
            // Deserializácia na background thread
            List<DataItem>? loadedData = null;
            if (!string.IsNullOrEmpty(jsonData))
            {
                loadedData = await Task.Run(() =>
                    JsonConvert.DeserializeObject<List<DataItem>>(jsonData));
            }

            // KRITICKÉ: Modifikácia kolekcie na UI thread
            try
            {
                _dataList.Clear();

                if (loadedData != null)
                {
                    foreach (var item in loadedData)
                    {
                        _dataList.Add(item);
                    }
                }

                Debug.WriteLine($"ProcessLoadedJsonData completed: {_dataList.Count} items loaded");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating collection on UI thread: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Bezpečné získanie lokálneho priečinka
        /// </summary>
        public static async Task<StorageFolder> GetLocalFolderAsync()
        {
            try
            {
                return ApplicationData.Current.LocalFolder;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetLocalFolderAsync failed: {ex.Message}");

                // Fallback: vytvoriť dočasný priečinok
                var tempPath = Path.Combine(Path.GetTempPath(), _appFolderName);
                Directory.CreateDirectory(tempPath);

                // Vrátime StorageFolder z temp path
                return await StorageFolder.GetFolderFromPathAsync(tempPath);
            }
        }

        // ZVYŠOK METÓD OSTÁVA ROVNAKÝ...

        public async Task<bool> SaveDataAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return await SaveDataAsync();
            }

            try
            {
                string jsonData = JsonConvert.SerializeObject(_dataList, Formatting.Indented);
                await File.WriteAllTextAsync(filePath, jsonData);

                DataSaved?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveDataAsync(filePath): {ex.Message}");
                OnDataError("Error saving data", ex);
                return false;
            }
        }

        public DataItem AddItem(DataItem item)
        {
            _dataList.Add(item);
            return item;
        }

        public bool UpdateItem(DataItem oldItem, DataItem newItem)
        {
            int index = _dataList.IndexOf(oldItem);
            if (index >= 0)
            {
                _dataList[index] = newItem;
                return true;
            }
            return false;
        }

        public bool RemoveItem(DataItem item)
        {
            return _dataList.Remove(item);
        }

        public void ClearAllData()
        {
            System.Diagnostics.Debug.WriteLine($"DataService.ClearAllData called, clearing {_dataList.Count} items");
            _dataList.Clear();
            System.Diagnostics.Debug.WriteLine("DataService.ClearAllData completed");
        }

        protected virtual void OnDataLoaded()
        {
            DataLoaded?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDataError(string message, Exception ex)
        {
            DataError?.Invoke(this, new DataErrorEventArgs(message, ex));
        }
    }

    public class DataErrorEventArgs(string message, Exception exception) : EventArgs
    {
        public string Message { get; } = message;
        public Exception Exception { get; } = exception;
    }
}
