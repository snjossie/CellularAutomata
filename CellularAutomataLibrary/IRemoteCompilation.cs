using System;
using System.Collections.Concurrent;
using System.ServiceModel;

namespace CellularAutomataLibrary
{
    /// <summary>
    /// Defines a service contract that compiles and runs cellular automata code.
    /// </summary>
    [ServiceContract]
    public interface IRemoteCompilation
    {
        /// <summary>
        /// Compiles the specified code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>A CompilerErrors indicating whether the compilation succeeded or the errors if it failed.</returns>
        [OperationContract]
        CompilerErrors Compile(String code);

        /// <summary>
        /// Initializes the specified initial board.
        /// </summary>
        /// <param name="initialBoard">The initial board from the client.</param>
        [OperationContract]
        void Initialize(byte[][] initialBoard);

        /// <summary>
        /// Does one step of the cellular automaton.
        /// </summary>
        /// <returns>A dictionary of all of the changed states.</returns>
        /// <exception cref="FaultException{InvalidOperationFault}">
        /// Thrown to indicate that the step operation cannot be executed.
        /// </exception>
        [OperationContract]
        [FaultContract(typeof(InvalidOperationFault))]
        ConcurrentDictionary<Tuple<int, int>, byte> Step();
    }
}
