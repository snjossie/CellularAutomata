using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using JetBrains.Annotations;

namespace CellularAutomataClient
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
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
        /// Gets or sets a value indicating whether this <see cref="BusyIndicator"/> is busy.
        /// </summary>
        /// <value>
        ///   <c>true</c> if busy; otherwise, <c>false</c>.
        /// </value>
        public bool Busy
        {
            get => (bool)GetValue(BusyProperty);

            set
            {
                SetValue(BusyProperty, value);
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
