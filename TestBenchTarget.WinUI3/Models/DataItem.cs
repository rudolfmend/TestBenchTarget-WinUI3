using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text.Json.Serialization;

namespace TestBenchTarget.WinUI3.Models
{
    /// <summary>
    /// Reprezentuje dátovú položku v TestBench aplikácii
    /// </summary>
    public partial class DataItem : ObservableObject, IEquatable<DataItem>
    {
        private Guid _id = Guid.NewGuid(); // Jedinečný identifikátor položky
        private DateTime _dateColumnValue = DateTime.Now.Date;
        private string _procedureColumnValue = string.Empty;
        private int _pointsColumnValue = 0;
        private string _delegateColumnValue = string.Empty;
        private string _formattedDate = string.Empty;

        /// <summary>
        /// Jedinečný identifikátor položky
        /// </summary>
        [JsonPropertyName("id")]
        public Guid Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// En: Date of the procedure.
        /// Sk: Dátum vykonania procedúry.
        /// </summary>
        [JsonPropertyName("date")]
        public DateTime DateColumnValue
        {
            get => _dateColumnValue;
            set
            {
                if (SetProperty(ref _dateColumnValue, value.Date))
                {
                    // Aktualizovať FormattedDate keď sa zmení dátum - ak nie je explicitne nastavený
                    if (string.IsNullOrEmpty(_formattedDate))
                    {
                        FormattedDate = value.ToString("yyyy-MM-dd"); // default format
                    }
                }
            }
        }

        /// <summary>
        /// En: Name or description of the procedure.
        /// Sk: Názov alebo popis procedúry.
        /// </summary>
        [JsonPropertyName("procedure")]
        public string ProcedureColumnValue
        {
            get => _procedureColumnValue;
            set
            {
                if (SetProperty(ref _procedureColumnValue, value))
                {
                }
            }
        }

        /// <summary>
        /// En: Number of points assigned to the procedure.
        /// Sk: Počet bodov priradených k procedúre.
        /// </summary>
        [JsonPropertyName("points")]
        public int PointsColumnValue
        {
            get => _pointsColumnValue;
            set
            {
                if (SetProperty(ref _pointsColumnValue, value))
                {
                }
            }
        }

        /// <summary>
        /// En: Name or identifier of the delegate.
        /// Sk: Meno alebo identifikátor delegovanej osoby.
        /// </summary>
        [JsonPropertyName("delegate")]
        public string DelegateColumnValue
        {
            get => _delegateColumnValue;
            set => SetProperty(ref _delegateColumnValue, value);
        }

        /// <summary>
        /// Formátovaný dátum pre zobrazenie v UI
        /// </summary>
        [JsonIgnore]
        public string FormattedDate
        {
            get => _formattedDate;
            set => SetProperty(ref _formattedDate, value);
        }

        /// <summary>
        /// Metóda na aktualizáciu FormattedDate podľa špecifikovaného formátu
        /// </summary>
        public void UpdateFormattedDate(string dateFormat)
        {
            FormattedDate = DateColumnValue.ToString(dateFormat);
        }

        /// <summary>
        /// En: Updates the values of this item from another item.
        /// Sk: Aktualizuje hodnoty tejto položky z inej položky.
        /// </summary>
        /// <param name="other">Položka, z ktorej sa majú skopírovať hodnoty</param>
        public void UpdateFrom(DataItem other)
        {
            if (other == null) return;

            DateColumnValue = other.DateColumnValue;
            ProcedureColumnValue = other.ProcedureColumnValue;
            PointsColumnValue = other.PointsColumnValue;
            DelegateColumnValue = other.DelegateColumnValue;
            FormattedDate = other.FormattedDate;
        }

        /// <summary>
        /// En: Creates a deep copy of the DataItem object.
        /// Sk: Vytvorí hlbokú kópiu objektu.
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
                DelegateColumnValue = this.DelegateColumnValue,
                FormattedDate = this.FormattedDate
            };
        }

        /// <summary>
        /// En: Compares this item with another item based on its values.
        /// Sk: Porovnáva túto položku s inou položkou na základe jej hodnôt.
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
        /// En: Compares this item with any object.
        /// Sk: Porovnáva túto položku s ľubovoľným objektom.
        /// </summary>
        /// <param name="obj">Objekt na porovnanie</param>
        /// <returns>true ak je objekt DataItem a má rovnaké hodnoty; inak false</returns>
        public override bool Equals(object? obj)
        {
            return obj is DataItem item && Equals(item);
        }

        /// <summary>
        /// En: Gets the hash code for this item.
        /// Sk: Získava hash kód pre túto položku.
        /// </summary>
        /// <returns>Hash kód</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(DateColumnValue, ProcedureColumnValue, PointsColumnValue, DelegateColumnValue);
        }
    }
}
