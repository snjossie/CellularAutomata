using System;
using System.Runtime.Serialization;

namespace CellularAutomataLibrary
{
    /// <summary>
    /// Indicates that an invalid operation was performed on the server.
    /// </summary>
    [DataContract]
    public class InvalidOperationFault
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidOperationFault"/> class.
        /// </summary>
        /// <param name="reason">The reason that the operation is invalid.</param>
        public InvalidOperationFault(String reason)
        {
            Reason = reason;
        }

        /// <summary>
        /// Gets the reason.
        /// </summary>
        [DataMember]
        public String Reason { get; private set; }
    }
}
