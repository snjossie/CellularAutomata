using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using JetBrains.Annotations;

namespace CellularAutomataClient
{
    /// <summary>
    /// Represents a possible state configuration in a cellular automaton.
    /// </summary>
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

        private byte state;

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
        /// Gets or sets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        public byte State
        {
            get => state;
            set
            {
                if (value == state) return;
                state = value;
                OnPropertyChanged();
            }
        }

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
                OnPropertyChanged();
            }
        }

        protected bool Equals(StateConfiguration other)
        {
            return Equals(brush, other.brush) && state == other.state;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == this.GetType() && Equals((StateConfiguration) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((brush != null ? brush.GetHashCode() : 0) * 397) ^ state.GetHashCode();
            }
        }

        public override string ToString()
        {
            return this.State.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
