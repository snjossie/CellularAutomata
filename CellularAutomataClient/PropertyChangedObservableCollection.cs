using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace CellularAutomataClient
{
    /// <summary>
    /// An ObservableCollection that reports when an element's property has changed.
    /// </summary>
    /// <typeparam name="T">The type of elements to be stored.</typeparam>
    public class PropertyChangedObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        /// <summary>
        /// Inserts the item at the specified index.
        /// </summary>
        /// <param name="index">The index at which to insert.</param>
        /// <param name="item">The item to insert.</param>
        protected override void InsertItem(int index, T item)
        {
            base.InsertItem(index, item);
            item.PropertyChanged += new PropertyChangedEventHandler(Item_PropertyChanged);
        }

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        /// <param name="index">The index at which an item should be removed.</param>
        protected override void RemoveItem(int index)
        {
            this[index].PropertyChanged -= new PropertyChangedEventHandler(Item_PropertyChanged);
            base.RemoveItem(index);
        }

        /// <summary>
        /// Handles the PropertyChanged event of an item in the collection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
