using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace TestBenchTarget.WinUI3.Models
{
    /// <summary>
    /// Reprezentuje dátovú položku v TestBench aplikácii
    /// </summary>
    public class DataItem : INotifyPropertyChanged, IEquatable<DataItem>
    {
        private DateTime _dateColumnValue = DateTime.Now.Date;
        private string _procedureColumnValue = string.Empty;
        private int _pointsColumnValue;
        private string _delegateColumnValue = string.Empty;
        private Guid _id = Guid.NewGuid(); // Jedinečný identifikátor položky

        /// <summary>
        /// Udalosť volaná pri zmene hodnoty vlastnosti
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Jedinečný identifikátor položky
        /// </summary>
        [JsonPropertyName("id")]
        public Guid Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Dátum vykonania procedúry
        /// </summary>
        [JsonPropertyName("date")]
        public DateTime DateColumnValue
        {
            get => _dateColumnValue;
            set
            {
                if (_dateColumnValue != value.Date)
                {
                    _dateColumnValue = value.Date;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Názov alebo popis procedúry
        /// </summary>
        [JsonPropertyName("procedure")]
        public string ProcedureColumnValue
        {
            get => _procedureColumnValue;
            set
            {
                if (_procedureColumnValue != value)
                {
                    _procedureColumnValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Počet bodov priradených k procedúre
        /// </summary>
        [JsonPropertyName("points")]
        public int PointsColumnValue
        {
            get => _pointsColumnValue;
            set
            {
                if (_pointsColumnValue != value)
                {
                    _pointsColumnValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Meno alebo identifikátor delegovanej osoby
        /// </summary>
        [JsonPropertyName("delegate")]
        public string DelegateColumnValue
        {
            get => _delegateColumnValue;
            set
            {
                if (_delegateColumnValue != value)
                {
                    _delegateColumnValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Celkový popis položky vo formáte vhodnom na zobrazenie
        /// </summary>
        [JsonIgnore]
        public string DisplaySummary => $"{DateColumnValue:dd.MM.yyyy} - {ProcedureColumnValue} ({PointsColumnValue} bodov)";

        /// <summary>
        /// Dátumy vykonania procedúry vo formáte vhodnom pre zobrazenie
        /// </summary>
        [JsonIgnore]
        public string FormattedDate => DateColumnValue.ToString("dd.MM.yyyy");

        /// <summary>
        /// Metóda volaná pri zmene hodnoty vlastnosti
        /// </summary>
        /// <param name="propertyName">Názov zmenenej vlastnosti</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Aktualizuje hodnoty tejto položky z inej položky
        /// </summary>
        /// <param name="other">Položka, z ktorej sa majú skopírovať hodnoty</param>
        public void UpdateFrom(DataItem other)
        {
            if (other == null) return;

            DateColumnValue = other.DateColumnValue;
            ProcedureColumnValue = other.ProcedureColumnValue;
            PointsColumnValue = other.PointsColumnValue;
            DelegateColumnValue = other.DelegateColumnValue;
        }

        /// <summary>
        /// Vytvorí hlbokú kópiu objektu
        /// </summary>
        /// <returns>Nová inštancia DataItem s rovnakými hodnotami</returns>
        public DataItem Clone()
        {
            return new DataItem
            {
                Id = this.Id, // Copy the same ID for tracking
                DateColumnValue = this.DateColumnValue,
                ProcedureColumnValue = this.ProcedureColumnValue,
                PointsColumnValue = this.PointsColumnValue,
                DelegateColumnValue = this.DelegateColumnValue
            };
        }

        /// <summary>
        /// Porovnáva túto položku s inou položkou na základe jej hodnôt
        /// </summary>
        /// <param name="other">Položka na porovnanie</param>
        /// <returns>true ak sú položky rovnaké; inak false</returns>
        public bool Equals(DataItem? other)
        {
            if (other == null) return false;

            return DateColumnValue.Equals(other.DateColumnValue) &&
                   string.Equals(ProcedureColumnValue, other.ProcedureColumnValue) &&
                   PointsColumnValue == other.PointsColumnValue &&
                   string.Equals(DelegateColumnValue, other.DelegateColumnValue);
        }

        /// <summary>
        /// Porovnáva túto položku s ľubovoľným objektom
        /// </summary>
        /// <param name="obj">Objekt na porovnanie</param>
        /// <returns>true ak je objekt DataItem a má rovnaké hodnoty; inak false</returns>
        public override bool Equals(object? obj)
        {
            return obj is DataItem item && Equals(item);
        }

        /// <summary>
        /// Získava hash kód pre túto položku
        /// </summary>
        /// <returns>Hash kód</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(DateColumnValue, ProcedureColumnValue, PointsColumnValue, DelegateColumnValue);
        }

        /// <summary>
        /// Vytvára textovú reprezentáciu tejto položky
        /// </summary>
        /// <returns>Textová reprezentácia</returns>
        public override string ToString()
        {
            return DisplaySummary;
        }
    }
}
