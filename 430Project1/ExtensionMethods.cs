using System;
using System.Windows.Media;
using System.Windows.Threading;

namespace CellularAutomataClient
{
    /// <summary>
    /// Extension Methods
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Invokes a <c>Func</c> using the dispatcher, if using the dispatcher is required, otherwise
        /// invokes the <c>Func</c>on the calling thread.
        /// </summary>
        /// <typeparam name="T1">The type of the parameter of the function.</typeparam>
        /// <typeparam name="T2">The return type of the function.</typeparam>
        /// <param name="dispatcher">The dispatcher to invoke on.</param>
        /// <param name="action">The action to invoke.</param>
        /// <param name="param">The parameter of the function being invoked.</param>
        /// <returns>The result of the invoked <c>Func</c>.</returns>
        public static T2 InvokeIfRequired<T1, T2>(this Dispatcher dispatcher, Func<T1, T2> action, T1 param)
        {
            if (dispatcher.CheckAccess())
            {
                return action(param);
            }
            else
            {
                return (T2)dispatcher.Invoke(action, param);
            }
        }

        /// <summary>
        /// Invokes an <c>Action</c> using the dispatcher, if using the dispatcher is required, otherwise
        /// invokes the <c>Action</c> on the calling thread.
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="action">The action.</param>
        public static void InvokeIfRequired(this Dispatcher dispatcher, Action action)
        {
            if (dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                dispatcher.Invoke(action);
            }
        }

        /// <summary>
        /// Generates a random color using the given random.
        /// </summary>
        /// <param name="rand">The random used to generate the color.</param>
        /// <returns>A random Color.</returns>
        public static Color NextColor(this Random rand)
        {
            return Color.FromRgb((byte)rand.Next(256), (byte)rand.Next(256), (byte)rand.Next(256));
        }
    }
}
