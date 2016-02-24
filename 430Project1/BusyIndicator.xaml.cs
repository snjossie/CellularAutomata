using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace CellularAutomataClient
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [INotify]
    public partial class BusyIndicator : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// The DependencyProperty associated with the Busy value.
        /// </summary>
        public static readonly DependencyProperty BusyProperty;

        /// <summary>
        /// Initializes static members of the <see cref="BusyIndicator"/> class.
        /// </summary>
        static BusyIndicator()
        {
            var metadata = new FrameworkPropertyMetadata() { AffectsRender = true };
            DependencyProperty.Register("Busy", typeof(bool), typeof(BusyIndicator), metadata);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusyIndicator"/> class.
        /// </summary>
        public BusyIndicator()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BusyIndicator"/> is busy.
        /// </summary>
        /// <value>
        ///   <c>true</c> if busy; otherwise, <c>false</c>.
        /// </value>
        public bool Busy
        {
            get
            {
                return (bool)GetValue(BusyProperty);
            }

            set
            {
                SetValue(BusyProperty, value);
            }
        }

        /// <summary>
        /// Called when a property changed.  Invokes the PropertyChanged event.
        /// </summary>
        /// <param name="property">The name of the property that changed.</param>
        public void OnPropertyChanged(String property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }
}
