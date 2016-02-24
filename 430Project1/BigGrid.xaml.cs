using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CellularAutomataClient
{
    /// <summary>
    /// Interaction logic for BigGrid.xaml
    /// </summary>
    public partial class BigGrid : UserControl
    {
        /// <summary>
        /// The DependencyProperty associated with the ShouldUpdate value.
        /// </summary>
        public static readonly DependencyProperty ShouldUpdateProperty;

        /// <summary>
        /// The DependencyProperty associated with the Running value.
        /// </summary>
        public static readonly DependencyProperty RunningProperty;

        /// <summary>
        /// The number of vertical pixels.
        /// </summary>
        private const int PixelRows = 3201; // pixels

        /// <summary>
        /// The number of horizontal pixels.
        /// </summary>
        private const int PixelCols = 3201; // pixels

        /// <summary>
        /// The pixel size of a sell (inside the borders).
        /// </summary>
        private const int CellSize = 3; // pixels

        /// <summary>
        /// The pixel size of a border.
        /// </summary>
        private const int BorderSize = 1; // pixels

        /// <summary>
        /// The number of actual Rows.
        /// </summary>
        private const int Rows = PixelRows / (CellSize + 1); // cells

        /// <summary>
        /// The number of actual columns.
        /// </summary>
        private const int Cols = PixelCols / (CellSize + 1); // cells

        /// <summary>
        /// A random used to generate random colors.
        /// </summary>
        private static readonly Random rand = new Random();

        /// <summary>
        /// The back buffer stride of the <c>WriteableBitmap</c>.
        /// </summary>
        private static int stride;

        /// <summary>
        /// A collection of StateConfigurations that will be used to choose the colors in the bitmap.
        /// </summary>
        private static PropertyChangedObservableCollection<StateConfiguration> stateConfigs = new PropertyChangedObservableCollection<StateConfiguration>();

        /// <summary>
        /// The bitmap to draw into.
        /// </summary>
        private WriteableBitmap bmp;

        /// <summary>
        /// The state with the highest ID.
        /// </summary>
        private byte maxState;

        /// <summary>
        /// The state with the lowest ID.
        /// </summary>
        private byte minState;

        /// <summary>
        /// The grid of state IDs.
        /// </summary>
        private byte[][] grid;

        /// <summary>
        /// A dictionary used to memoize color values.
        /// </summary>
        private ConcurrentDictionary<byte, int> colorDict;

        /// <summary>
        /// Initializes static members of the <see cref="BigGrid"/> class.
        /// </summary>
        static BigGrid()
        {
            var meta = new FrameworkPropertyMetadata(false);
            ShouldUpdateProperty = DependencyProperty.Register("ShouldUpdate", typeof(bool), typeof(BigGrid), meta);
            meta = new FrameworkPropertyMetadata(false);
            RunningProperty = DependencyProperty.Register("Running", typeof(bool), typeof(BigGrid), meta);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BigGrid"/> class.
        /// </summary>
        public BigGrid()
        {
            stateConfigs.CollectionChanged += new NotifyCollectionChangedEventHandler(StateConfigs_CollectionChanged);
            colorDict = new ConcurrentDictionary<byte, int>();
            InitializeComponent();

            bmp = new WriteableBitmap(PixelRows, PixelCols, 96, 96, PixelFormats.Bgr32, null);
            stride = bmp.BackBufferStride;
            img.Source = bmp;
            img.MouseDown += new MouseButtonEventHandler(Img_MouseDown);

            stateConfigs.Add(new StateConfiguration { State = 0, Color = Brushes.White });
            stateConfigs.Add(new StateConfiguration { State = 1, Color = Brushes.Black });

            DrawBoard();
        }

        /// <summary>
        /// Occurs when the grid changes.
        /// </summary>
        public event EventHandler GridUpdated;

        /// <summary>
        /// Gets or sets a value indicating whether the board should be redrawn.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the board needs redrawing; otherwise, <c>false</c>.
        /// </value>
        public bool ShouldUpdate
        {
            get
            {
                return (bool)GetValue(ShouldUpdateProperty);
            }

            set
            {
                SetValue(ShouldUpdateProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="BigGrid"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        public bool Running
        {
            get
            {
                return (bool)GetValue(RunningProperty);
            }

            set
            {
                SetValue(RunningProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the grid.
        /// </summary>
        /// <value>
        /// The grid.
        /// </value>
        public byte[][] Grid
        {
            get
            {
                return grid;
            }

            set
            {
                grid = value;
            }
        }

        /// <summary>
        /// Gets or sets the state configurations.
        /// </summary>
        /// <value>
        /// The state configurations.
        /// </value>
        public PropertyChangedObservableCollection<StateConfiguration> StateConfigurations
        {
            get
            {
                return stateConfigs;
            }

            set
            {
                stateConfigs = value;
            }
        }

        /// <summary>
        /// Called when the visual updates.
        /// </summary>
        public void VisualUpdated()
        {
            DrawBoard();
        }

        /// <summary>
        /// Fills a specified rectangle.
        /// </summary>
        /// <param name="backBufferBase">The back buffer base.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="color">The color.</param>
        private static unsafe void FillRectangle(int backBufferBase, int x, int y, int color)
        {
            const int PixelCellSize = CellSize * 4;
            backBufferBase += 4 + stride;
            int buffer = backBufferBase + ((x * 4 * stride) + (y * 16));
            for (int i = 0; i < CellSize + 1; i++)
            {
                for (int j = 0; j < PixelCellSize; j += 4)
                {
                    *((int*)(buffer + j)) = color;
                }

                buffer = backBufferBase + ((((x * 4) + i) * stride) + (y * 16));
            }
        }

        /// <summary>
        /// Handles the CollectionChanged event of the stateConfigs control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void StateConfigs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (stateConfigs.Count == 0)
            {
                return;
            }

            maxState = stateConfigs.Max(m => m.State);
            minState = stateConfigs.Min(m => m.State);
            colorDict.Clear();
        }

        /// <summary>
        /// Handles the MouseDown event of the img control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void Img_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Running)
            {
                return;
            }

            Point pos = e.GetPosition(img);
            int size = CellSize + BorderSize;

            int x = (int)(pos.X / size);
            int y = (int)(pos.Y / size);

            Console.WriteLine("Coordinates ({0}, {1}), State {2}", y, x, grid[y][x]);

            if (grid[y][x] == maxState)
            {
                grid[y][x] = minState;
            }
            else
            {
                byte temp = stateConfigs.First(k => k.State > grid[y][x]).State;
                grid[y][x] = temp;
            }

            OnGridUpdated();
            VisualUpdated();
        }

        /// <summary>
        /// Called when the grid is updated.
        /// </summary>
        private void OnGridUpdated()
        {
            if (GridUpdated != null)
            {
                GridUpdated(this, new EventArgs());
            }
        }

        /// <summary>
        /// Draws the board.
        /// </summary>
        private void DrawBoard()
        {
            if (grid == null || !ShouldUpdate)
            {
                return;
            }

            bmp.Lock();
            unsafe
            {
                int buffer = (int)bmp.BackBuffer;

                Parallel.For(0, grid.Length, i =>
                {
                    Parallel.For(0, grid.Length, j =>
                    {
                        int fillColor = ColorFromState(grid[i][j]);
                        FillRectangle(buffer, i, j, fillColor);
                    });
                });
            }

            // possible optimization?  only indicate that a portion of the 
            // image updated (i.e. only the area that changed)
            bmp.AddDirtyRect(new Int32Rect(0, 0, PixelRows, PixelCols));
            bmp.Unlock();
        }

        /// <summary>
        /// Gets a color for a given state.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>A color as a 32-bit integer.</returns>
        private int ColorFromState(byte state)
        {
            int color = 0;

            unchecked
            {
                if (colorDict.TryGetValue(state, out color))
                {
                    return color;
                }
                else
                {
                    for (int i = 0; i < stateConfigs.Count; i++)
                    {
                        if (state == stateConfigs[i].State)
                        {
                            var brushColor = stateConfigs[i].Color.Color;
                            color |= brushColor.A << 24
                                  | brushColor.R << 16
                                  | brushColor.G << 8
                                  | brushColor.B;

                            colorDict.TryAdd(state, color);

                            return color;
                        }
                    }

                    color |= rand.Next(256) << 24
                          | rand.Next(256) << 16
                          | rand.Next(256) << 8
                          | rand.Next(256);
                    colorDict.TryAdd(state, color);
                    stateConfigs.Add(new StateConfiguration()
                                    {
                                        State = state,
                                        Color = new SolidColorBrush(
                                                                    Color.FromArgb(
                                                                                    (byte)(color >> 24),
                                                                                    (byte)(color >> 16),
                                                                                    (byte)(color >> 8),
                                                                                    (byte)color))
                                    });

                    return color;
                }
            }
        }
    }
}
