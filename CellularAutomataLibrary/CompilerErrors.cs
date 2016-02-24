using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CellularAutomataLibrary
{
    /// <summary>
    /// Indicates the severity of a compiler message.
    /// </summary>
    public enum Severity
    {
        /// <summary>
        /// A warning message.
        /// </summary>
        Warning,

        /// <summary>
        /// An error message.
        /// </summary>
        Error
    }

    /// <summary>
    /// A collection that contains all of the errors that occurred during compilation,
    /// or a message that indicates compilation was successful.
    /// </summary>
    [DataContract]
    public class CompilerErrors
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerErrors"/> class.
        /// </summary>
        public CompilerErrors()
        {
            Messages = new List<CompilerMessage>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is success.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is success; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets or sets the compilation messages.
        /// </summary>
        /// <value>
        /// The compilation messages.
        /// </value>
        [DataMember]
        public List<CompilerMessage> Messages { get; set; }
    }
}
