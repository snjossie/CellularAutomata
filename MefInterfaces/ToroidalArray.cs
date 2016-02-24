namespace CellularAutomataLibrary
{
    /// <summary>
    /// An Array that allows wrap around indexing.
    /// </summary>
    public class ToroidalArray
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToroidalArray"/> class.
        /// </summary>
        /// <param name="size">The size.</param>
        public ToroidalArray(int size)
        {
            Grid = new byte[size][];
            for (int i = 0; i < size; i++)
            {
                Grid[i] = new byte[size];
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToroidalArray"/> class.
        /// </summary>
        /// <param name="grid">The grid.</param>
        public ToroidalArray(byte[][] grid)
        {
            Grid = grid;
        }

        /// <summary>
        /// Gets the grid.
        /// </summary>
        public byte[][] Grid { get; private set; }

        /// <summary>
        /// Gets the <see cref="System.Byte"/> at the specified coordinates.
        /// </summary>
        /// <param name="x">The X-coordinate.</param>
        /// <param name="y">The Y-coordinate.</param>
        /// <returns>The byte at the specified coordinates.</returns>
        public byte this[int x, int y]
        {
            get
            {
                unchecked
                {
                    int actualX;
                    int actualY;

                    if (x < 0)
                    {
                        actualX = Grid.Length + x;
                    }
                    else if (x >= Grid.Length)
                    {
                        actualX = x - Grid.Length;
                    }
                    else
                    {
                        actualX = x;
                    }

                    if (y < 0)
                    {
                        actualY = Grid.Length + y;
                    }
                    else if (y >= Grid.Length)
                    {
                        actualY = y - Grid.Length;
                    }
                    else
                    {
                        actualY = y;
                    }

                    return Grid[actualX][actualY];
                }
            }
        }
    }
}
