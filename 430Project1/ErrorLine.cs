using System;

namespace CellularAutomataClient
{
    /// <summary>
    /// A representation of a single error message to be displayed to the user.
    /// </summary>
    public struct ErrorLine
    {
        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        /// <value>
        /// The severity.
        /// </value>
        public String Severity { get; set; }

        /// <summary>
        /// Gets or sets the error text.
        /// </summary>
        /// <value>
        /// The error text.
        /// </value>
        public String ErrorText { get; set; }

        /// <summary>
        /// Gets or sets the line number.
        /// </summary>
        /// <value>
        /// The line number.
        /// </value>
        public int LineNumber { get; set; }
    }
}
