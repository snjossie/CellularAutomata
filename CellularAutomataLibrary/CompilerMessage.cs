using System;
using System.Runtime.Serialization;

namespace CellularAutomataLibrary
{
    /// <summary>
    /// An individual message from the compiler.
    /// </summary>
    [DataContract]
    public class CompilerMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerMessage"/> class.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="severity">The severity.</param>
        /// <param name="message">The message.</param>
        public CompilerMessage(int line, Severity severity, String message)
        {
            LineNumber = line;
            Severity = severity;
            ErrorMessage = message;
        }

        /// <summary>
        /// Gets the line number that the error or warning occurred on.
        /// </summary>
        [DataMember]
        public int LineNumber { get; private set; }

        /// <summary>
        /// Gets the severity of message.
        /// </summary>
        [DataMember]
        public Severity Severity { get; private set; }

        /// <summary>
        /// Gets the error message.
        /// </summary>
        [DataMember]
        public String ErrorMessage { get; private set; }
    }
}
