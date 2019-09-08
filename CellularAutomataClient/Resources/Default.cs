using System;
using System.Linq;
using CellularAutomataLibrary;

namespace Transition
{
    /// <summary>
    /// Defines a transition function for a cellular automaton.
    /// </summary>
    public class TransitionFunc : ITransition
    {
        /// <summary>
        /// The transition function. This method will be called once per step on each cell
        /// on the board.
        /// </summary>
        /// <param name="board">The read-only board representing the current state of the automaton.</param>
        /// <param name="currentX">The X-coordinate of the current cell.</param>
        /// <param name="currentY">The Y-coordinate of the current cell.</param>
        /// <returns>The next state for this cell.</returns>
        /// <remarks>
        ///     ToroidalArray is a 2-D array that wraps in both the x and y directions.
        ///     The only property of the ToroidalArray that is of any significance 
        ///     is the 2-D indexer.  Example usage is shown below...
        /// <para />  
        ///     if(board[0, 1] == 1) { }
        /// </remarks>
        public byte TransitionFunction(ToroidalArray board, int currentX, int currentY)
        {
            // TODO: Complete this method
            return 0;
        }
    }
}