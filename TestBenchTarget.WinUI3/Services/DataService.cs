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
        private readonly CustomObservableCollection<DataItem> _dataList = new CustomObservableCollection<DataItem>();
        private const string _fileName = "TestBenchTarget.json";
        private const string _appFolderName = "TestBenchTarget";

        // Verejné vlastnosti
        public CustomObservableCollection<DataItem> DataList => _dataList;

        // Udalosti
        public event EventHandler? DataLoaded;
        public event EventHandler? DataSaved;
        public event EventHandler<DataErrorEventArgs>? DataError;

        // Konštruktor
        public DataService()
        {
            // Môžete tu pridať inicializačnú logiku, ak je potrebná
        }

        /// <summary>
        /// Uloženie dát do lokálneho súboru aplikácie
        /// </summary>
        public async Task<bool> SaveDataAsync()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = await localFolder.CreateFileAsync(_fileName,
                                    CreationCollisionOption.ReplaceExisting);
                string jsonData = JsonConvert.SerializeObject(_dataList, Formatting.Indented);
                await FileIO.WriteTextAsync(file, jsonData);

                DataSaved?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveDataAsync: {ex.Message}");
                OnDataError("Error saving data", ex);
                return false;
            }
        }

        /// <summary>
        /// Načítanie dát z lokálneho súboru aplikácie
        /// </summary>
        public async Task<bool> LoadDataAsync()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                try
                {
                    StorageFile file = await localFolder.GetFileAsync(_fileName);
                    string jsonData = await FileIO.ReadTextAsync(file);

                    if (string.IsNullOrEmpty(jsonData))
                    {
                        // Prázdny súbor
                        _dataList.Clear();
                        OnDataLoaded();
                        return true;
                    }

                    var loadedData = JsonConvert.DeserializeObject<List<DataItem>>(jsonData);
                    _dataList.Clear();

                    if (loadedData != null)
                    {
                        foreach (var item in loadedData)
                        {
                            _dataList.Add(item);
                        }
                    }

                    OnDataLoaded();
                    return true;
                }
                catch (FileNotFoundException)
                {
                    // Súbor ešte neexistuje, čo je v poriadku pri prvom spustení
                    _dataList.Clear();
                    OnDataLoaded();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadDataAsync: {ex.Message}");
                OnDataError("Error loading data", ex);
                return false;
            }
        }

        /// <summary>
        /// Získanie lokálneho priečinka aplikácie
        /// </summary>
        public Task<StorageFolder> GetLocalFolderAsync()
        {
            return Task.FromResult(ApplicationData.Current.LocalFolder);
        }

        /// <summary>
        /// Uloženie dát do externého súboru
        /// </summary>
        public async Task<bool> SaveDataAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return await SaveDataAsync_Alternative();
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

        /// <summary>
        /// Alternatívna implementácia SaveDataAsync pomocou .NET File APIs
        /// </summary>
        public async Task<bool> SaveDataAsync_Alternative(string filePath = null!)
        {
            try
            {
                string path;
                if (string.IsNullOrEmpty(filePath))
                {
                    path = Path.Combine(ApplicationData.Current.LocalFolder.Path, _fileName);
                }
                else
                {
                    path = filePath;
                }

                // Zabezpečenie existencie priečinka
                //Directory.CreateDirectory(Path.GetDirectoryName(path)); // possible null reference for parameter path 
                var directoryPath = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string jsonData = JsonConvert.SerializeObject(_dataList, Formatting.Indented);
                await File.WriteAllTextAsync(path, jsonData);

                DataSaved?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SaveDataAsync_Alternative: {ex.Message}");
                OnDataError("Error saving data", ex);
                return false;
            }
        }

        /// <summary>
        /// Alternatívna implementácia LoadDataAsync pomocou .NET File APIs
        /// </summary>
        public async Task<bool> LoadDataAsync_Alternative(string filePath = null!)
        {
            try
            {
                string path;
                if (string.IsNullOrEmpty(filePath))
                {
                    path = Path.Combine(ApplicationData.Current.LocalFolder.Path, _fileName);
                }
                else
                {
                    path = filePath;
                }

                if (!File.Exists(path))
                {
                    // Súbor ešte neexistuje, čo je v poriadku pri prvom spustení
                    _dataList.Clear();
                    OnDataLoaded();
                    return true;
                }

                string jsonData = await File.ReadAllTextAsync(path);

                if (string.IsNullOrEmpty(jsonData))
                {
                    // Prázdny súbor
                    _dataList.Clear();
                    OnDataLoaded();
                    return true;
                }

                var loadedData = JsonConvert.DeserializeObject<List<DataItem>>(jsonData);
                _dataList.Clear();

                if (loadedData != null)
                {
                    foreach (var item in loadedData)
                    {
                        _dataList.Add(item);
                    }
                }

                OnDataLoaded();
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadDataAsync_Alternative: {ex.Message}");
                OnDataError("Error loading data", ex);
                return false;
            }
        }

        /// <summary>
        /// Alternatívna implementácia GetLocalFolderAsync - vracia cestu k priečinku
        /// </summary>
        public string GetLocalFolderPath()
        {
            return ApplicationData.Current.LocalFolder.Path;
        }

        /// <summary>
        /// Získanie priečinka v Documents
        /// </summary>
        public async Task<string> GetDocumentsFolderPathAsync()
        {
            try
            {
                // Skúsime získať prístup k priečinku Documents
                var documentsFolder = await Windows.Storage.KnownFolders.DocumentsLibrary.CreateFolderAsync(
                    _appFolderName, CreationCollisionOption.OpenIfExists);

                return documentsFolder.Path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error accessing Documents folder: {ex.Message}");
                // Núdzový návrat k LocalFolder
                return ApplicationData.Current.LocalFolder.Path;
            }
        }

        /// <summary>
        /// Pridanie novej položky do kolekcie
        /// </summary>
        public DataItem AddItem(DataItem item)
        {
            _dataList.Add(item);
            return item;
        }

        /// <summary>
        /// Aktualizácia existujúcej položky
        /// </summary>
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

        /// <summary>
        /// Odstránenie položky z kolekcie
        /// </summary>
        public bool RemoveItem(DataItem item)
        {
            return _dataList.Remove(item);
        }

        /// <summary>
        /// Vymazanie všetkých dát - už existuje v DataService.cs
        /// </summary>
        public void ClearAllData()
        {
            System.Diagnostics.Debug.WriteLine($"DataService.ClearAllData called, clearing {_dataList.Count} items");
            _dataList.Clear();
            System.Diagnostics.Debug.WriteLine("DataService.ClearAllData completed");
        }

        /// <summary>
        /// Exportovanie dát do CSV formátu
        /// </summary>
        public async Task<bool> ExportToCsvAsync(string filePath)
        {
            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    // Hlavička CSV
                    await writer.WriteLineAsync("Date,Procedure,Points,Delegate");

                    // Dáta
                    foreach (var item in _dataList)
                    {
                        await writer.WriteLineAsync(
                            $"{item.DateColumnValue:yyyy-MM-dd},{EscapeCsvField(item.ProcedureColumnValue)}," +
                            $"{item.PointsColumnValue},{EscapeCsvField(item.DelegateColumnValue)}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error exporting to CSV: {ex.Message}");
                OnDataError("Error exporting to CSV", ex);
                return false;
            }
        }

        /// <summary>
        /// Escapovanie poľa pre CSV formát
        /// </summary>
        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return string.Empty;

            // Ak pole obsahuje čiarku, úvodzovku alebo nový riadok, obaľujeme ho úvodzovkami
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                // Zdvojnásobíme všetky úvodzovky a obalíme celý reťazec úvodzovkami
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }

        /// <summary>
        /// Vyvolanie udalosti DataLoaded
        /// </summary>
        protected virtual void OnDataLoaded()
        {
            DataLoaded?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Vyvolanie udalosti DataError
        /// </summary>
        protected virtual void OnDataError(string message, Exception ex)
        {
            DataError?.Invoke(this, new DataErrorEventArgs(message, ex));
        }
    }

    /// <summary>
    /// Trieda pre dátové chyby
    /// </summary>
    public class DataErrorEventArgs : EventArgs
    {
        public string Message { get; }
        public Exception Exception { get; }

        public DataErrorEventArgs(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }
    }
}
