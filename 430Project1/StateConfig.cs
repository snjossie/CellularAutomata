using System;
using System.ComponentModel;
using System.Windows.Media;

namespace CellularAutomataClient
{
    /// <summary>
    /// Represents a possible state configuration in a cellular automaton.
    /// </summary>
    [INotify]
    public class StateConfiguration : INotifyPropertyChanged
    {
        /// <summary>
        /// A random number generator for generating random colors.
        /// </summary>
        private static Random rand = new Random();

        /// <summary>
        /// The brush that will be used to paint with.
        /// </summary>
        private SolidColorBrush brush;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateConfiguration"/> class with a 
        /// default ID (of 0) and a random color.
        /// </summary>
        public StateConfiguration()
            : this(0, rand.NextColor())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateConfiguration"/> class with a random color.
        /// </summary>
        /// <param name="state">The state.</param>
        public StateConfiguration(byte state)
            : this(state, rand.NextColor())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateConfiguration"/> class.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="color">The color of this state.</param>
        public StateConfiguration(byte state, Color color)
        {
            State = state;
            Color = new SolidColorBrush(color);
            Color.Freeze();
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public byte State { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>
        /// The color as a SolidColorBrush.
        /// </value>
        public SolidColorBrush Color
        {
            get
            {
                return brush;
            }

            set
            {
                brush = value;
                brush.Freeze();
            }
        }

        /// <summary>
        /// Called when a property changed.  Invokes the PropertyChanged event.
        /// </summary>
        /// <param name="property">The name of the property that changed.</param>
        public void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null || obj == this)
            {
                return false;
            }

            if (obj is StateConfiguration)
            {
                var conf = obj as StateConfiguration;
                return conf.Color == this.Color && conf.State == this.State;
            }

            return base.Equals(obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.State.GetHashCode() ^ Color.GetHashCode() * 7;
        }

        public override string ToString()
        {
            return this.State.ToString();
        }
    }
}
