using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace TestBenchTarget.WinUI3.Models
{
    /// <summary>
    /// Rozšírená implementácia ObservableCollection s dodatočnými funkciami
    /// pre efektívnejšiu prácu s kolekciami.
    /// </summary>
    /// <typeparam name="T">Typ položiek v kolekcii.</typeparam>
    public class CustomObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification = false;

        /// <summary>
        /// Inicializuje novú inštanciu prázdnej kolekcie.
        /// </summary>
        public CustomObservableCollection() : base() { }

        /// <summary>
        /// Inicializuje novú inštanciu kolekcie s položkami skopírovanými zo zadanej kolekcie.
        /// </summary>
        /// <param name="collection">Kolekcia, z ktorej sa kopírujú položky.</param>
        /// <exception cref="ArgumentNullException">Ak je collection null.</exception>
        public CustomObservableCollection(IEnumerable<T> collection) : base(collection) { }

        /// <summary>
        /// Prepíše štandardné správanie InsertItem tak, aby položka bola vždy vložená na začiatok.
        /// </summary>
        /// <param name="index">Ignorovaný index.</param>
        /// <param name="item">Položka na vloženie.</param>
        protected override void InsertItem(int index, T item)
        {
            // Vždy vloží položku na začiatok kolekcie bez ohľadu na zadaný index
            base.InsertItem(0, item);
        }

        /// <summary>
        /// Explicitná implementácia Remove s vylepšeným debugovaním.
        /// </summary>
        /// <param name="item">Položka na odstránenie.</param>
        /// <returns>true ak bola položka odstránená; inak false.</returns>
        public new bool Remove(T item)
        {
            int index = IndexOf(item);
            System.Diagnostics.Debug.WriteLine($"Remove called, Item found at index: {index}");

            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Pridá viacero položiek naraz do kolekcie a vyvolá notifikačné udalosti len raz.
        /// </summary>
        /// <param name="items">Kolekcia položiek na pridanie.</param>
        /// <exception cref="ArgumentNullException">Ak je items null.</exception>
        public void AddRange(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            // Uloženie všetkých nových položiek do zoznamu pre neskoršie notifikácie
            List<T> newItems = new List<T>(items);
            if (newItems.Count == 0) return;

            CheckReentrancy();

            // Pridanie položiek na začiatok kolekcie v opačnom poradí
            for (int i = newItems.Count - 1; i >= 0; i--)
            {
                Items.Insert(0, newItems[i]);
            }

            // Vyvolanie udalostí len raz
            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add, newItems, 0));
        }

        /// <summary>
        /// Vyčistí celú kolekciu s vylepšeným debugovaním.
        /// </summary>
        protected override void ClearItems()
        {
            System.Diagnostics.Debug.WriteLine("ClearItems called, removing all items");
            base.ClearItems();
        }

        /// <summary>
        /// Zoradí položky podľa zadaného kľúča.
        /// </summary>
        /// <typeparam name="TKey">Typ kľúča.</typeparam>
        /// <param name="keySelector">Funkcia, ktorá extrahuje kľúč z položky.</param>
        /// <param name="ascending">true pre vzostupné zoradenie; false pre zostupné zoradenie.</param>
        public void SortBy<TKey>(Func<T, TKey> keySelector, bool ascending = true)
        {
            List<T> sortedItems;
            if (ascending)
                sortedItems = Items.OrderBy(keySelector).ToList();
            else
                sortedItems = Items.OrderByDescending(keySelector).ToList();

            CheckReentrancy();

            // Začiatok dočasného potlačenia notifikácií
            using (SuppressNotifications())
            {
                Items.Clear();
                foreach (var item in sortedItems)
                {
                    Items.Add(item);
                }
            }
            // Koniec potlačenia notifikácií

            // Vyvolanie udalosti Reset po zoradení
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Filtruje položky podľa zadaného predikátu.
        /// </summary>
        /// <param name="predicate">Funkcia, ktorá určuje, či položka spĺňa podmienku.</param>
        /// <returns>Nová inštancia CustomObservableCollection obsahujúca len položky spĺňajúce podmienku.</returns>
        public CustomObservableCollection<T> Filter(Func<T, bool> predicate)
        {
            return new CustomObservableCollection<T>(Items.Where(predicate));
        }

        /// <summary>
        /// Nahradí položku na zadanom indexe novou položkou.
        /// </summary>
        /// <param name="index">Index položky na nahradenie.</param>
        /// <param name="item">Nová položka.</param>
        /// <exception cref="ArgumentOutOfRangeException">Ak index nie je v rozsahu kolekcie.</exception>
        public void Replace(int index, T item)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            CheckReentrancy();
            T oldItem = this[index];
            Items[index] = item;

            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Replace, item, oldItem, index));
        }

        /// <summary>
        /// Nahrádza prvý výskyt zadanej položky novou položkou.
        /// </summary>
        /// <param name="oldItem">Položka na nahradenie.</param>
        /// <param name="newItem">Nová položka.</param>
        /// <returns>true ak bola položka nahradená; inak false.</returns>
        public bool Replace(T oldItem, T newItem)
        {
            int index = IndexOf(oldItem);
            if (index < 0) return false;

            Replace(index, newItem);
            return true;
        }

        /// <summary>
        /// Potlačí notifikácie o zmenách kým je aktívny.
        /// </summary>
        /// <returns>IDisposable objekt, ktorý po uvoľnení obnoví notifikácie.</returns>
        public IDisposable SuppressNotifications()
        {
            return new NotificationSuppressor(this);
        }

        /// <summary>
        /// Aktualizuje celú kolekciu naraz s jednou notifikáciou.
        /// </summary>
        /// <param name="items">Nové položky.</param>
        public void Reset(IEnumerable<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            CheckReentrancy();

            using (SuppressNotifications())
            {
                Items.Clear();
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Vykoná akciu pre každú položku kolekcie.
        /// </summary>
        /// <param name="action">Akcia na vykonanie.</param>
        public void ForEach(Action<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            // Vytvorenie kópie, aby sa predišlo problémom počas iterácie, ak by sa kolekcia zmenila
            foreach (var item in Items.ToList())
            {
                action(item);
            }
        }

        /// <summary>
        /// Vracia prvú položku spĺňajúcu podmienku alebo default(T), ak taká položka neexistuje.
        /// </summary>
        /// <param name="predicate">Funkcia, ktorá určuje, či položka spĺňa podmienku.</param>
        /// <returns>Prvá položka spĺňajúca podmienku alebo default(T), ak taká položka neexistuje.</returns>
        public T? FindFirst(Func<T, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            // Corrected the nullability issue by marking the return type as nullable
            return Items.FirstOrDefault(predicate);
        }

        /// <summary>
        /// Tento override zabezpečuje, že notifikácie sú vyvolané len ak nie sú dočasne potlačené.
        /// </summary>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
            {
                base.OnCollectionChanged(e);
            }
        }

        /// <summary>
        /// Tento override zabezpečuje, že notifikácie sú vyvolané len ak nie sú dočasne potlačené.
        /// </summary>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!_suppressNotification)
            {
                base.OnPropertyChanged(e);
            }
        }

        /// <summary>
        /// Trieda pre dočasné potlačenie notifikácií.
        /// </summary>
        private class NotificationSuppressor : IDisposable
        {
            private readonly CustomObservableCollection<T> _collection;
            private readonly bool _oldValue;

            public NotificationSuppressor(CustomObservableCollection<T> collection)
            {
                _collection = collection;
                _oldValue = collection._suppressNotification;
                collection._suppressNotification = true;
            }

            public void Dispose()
            {
                _collection._suppressNotification = _oldValue;
            }
        }
    }
}
