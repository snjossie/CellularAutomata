using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AvalonEdit.Sample;
using CellularAutomataClient.RemoteServices;
using ExceptionDialog.Wpf;
using ICSharpCode.AvalonEdit.Folding;
using Ionic.Zip;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace CellularAutomataClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// The height and width of the (square) board.
        /// </summary>
        private const int BoardSize = 800;

        /// <summary>
        /// The file filter for transition functions.
        /// </summary>
        private const string TransitionFunctionFileFilter = "Cellular Automata Transition Function|*.ca|C# Code File|*.cs|All Files|*.*";

        /// <summary>
        /// The file filter for grid definition files.
        /// </summary>
        private const string GridFileFilter = "Cellular Automata Grid|*.cag|All Files|*.*";

        /// <summary>
        /// The file filter for the combined file.
        /// </summary>
        private const string AutomataFileFilter = "Cellular Automata Definition|*.def|All Files|*.*";

        /// <summary>
        /// The file filter for state configuration files.
        /// </summary>
        private const string StateConfigFilter = "Cellular Automata State Configuration|*.cas|All Files|*.*";

        /// <summary>
        /// The file filter for all files.
        /// </summary>
        private const string AllFileFilter = "All Files|*.*";

        /// <summary>
        /// The board.
        /// </summary>
        private byte[][] board;

        /// <summary>
        /// The remote compilation client proxy.
        /// </summary>
        private RemoteCompilationClient client = new RemoteCompilationClient("Compile_TCP");

        /// <summary>
        /// The strategy to use to add folding to the code.
        /// </summary>
        private AbstractFoldingStrategy foldingStrategy;

        /// <summary>
        /// The object that manages code folding for the code editor.
        /// </summary>
        private FoldingManager foldingManager;

        /// <summary>
        /// A background worker for concurrent processing.
        /// </summary>
        private BackgroundWorker worker;

        /// <summary>
        /// Indicates whether the board has been uploaded or needs to be uploaded again.
        /// </summary>
        private bool boardUploaded = false;

        private int? runUntil;
        private bool simulationRunning;
        private int currentStep;
        private double scale;
        private StateConfiguration clearToState;
        private bool isExpanded;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            ClearToBox.SelectedIndex = 0;

            bigGrid.StateConfigurations.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(StateConfigurations_CollectionChanged);
            StateConfigurations_CollectionChanged(this, new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));

            // Initialize the data structures and assign the data context (must be in that order)
            CompilationErrors = new ObservableCollection<ErrorLine>();

            this.DataContext = this;

            // Create the background worker that will communicate with the server
            // and update the board
            worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_RunWorkerCompleted);
            worker.WorkerSupportsCancellation = true;

            Scale = 1;

            // Set up the board and initial code
            InitalizeBoard();
            LoadDefaultCode();
            bigGrid.Grid = board;
            ClearToState = bigGrid.StateConfigurations.First();

            board[5][5] = 1;
            board[5][6] = 1;
            board[5][7] = 1;
            board[6][5] = 1;
            board[7][6] = 1;

            UploadInitialGrid();

            bigGrid.GridUpdated += new EventHandler(BigGrid_GridUpdated);

            // Setup the folding manager and strategy for the code editor
            foldingManager = FoldingManager.Install(CodeEditor.TextArea);
            foldingStrategy = new BraceFoldingStrategy();
            foldingStrategy.UpdateFoldings(foldingManager, CodeEditor.Document);

            // Start a timer to update the code editor folding
            DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
            foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
            foldingUpdateTimer.Tick += new EventHandler(FoldingUpdateTimer_Tick);
            foldingUpdateTimer.Start();
        }

        /// <summary>
        /// Gets or sets the value to run until.
        /// </summary>
        /// <value>
        /// The number of steps to run until. Can be <c>null</c>.
        /// </value>
        public int? RunUntil
        {
            get => runUntil;
            set
            {
                if (value == runUntil) return;
                runUntil = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the simulation is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the simulation is running; otherwise, <c>false</c>.
        /// </value>
        public bool SimulationRunning
        {
            get => simulationRunning;
            set
            {
                if (value == simulationRunning) return;
                simulationRunning = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the current step.
        /// </summary>
        /// <value>
        /// The current step.
        /// </value>
        public int CurrentStep
        {
            get => currentStep;
            set
            {
                if (value == currentStep) return;
                currentStep = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the scale factor for zoom.
        /// </summary>
        /// <value>
        /// The scale factor.
        /// </value>
        public double Scale
        {
            get => scale;
            set
            {
                if (value.Equals(scale)) return;
                scale = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the clear-to-state.
        /// </summary>
        /// <value>       
        /// The clear-to-state.
        /// </value>
        public StateConfiguration ClearToState
        {
            get => clearToState;
            set
            {
                if (Equals(value, clearToState)) return;
                clearToState = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the compiler messages window is expanded.
        /// </summary>
        /// <value>
        ///     <c>true</c> if compiler messages window is expanded; otherwise, <c>false</c>.
        /// </value>
        public bool IsExpanded
        {
            get => isExpanded;
            set
            {
                if (value == isExpanded) return;
                isExpanded = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the compilation errors.
        /// </summary>
        public ObservableCollection<ErrorLine> CompilationErrors { get; private set; }

        /// <summary>
        /// Loads the cellular automata definition.
        /// </summary>
        public void LoadCellularAutomataDefinition()
        {
            String fileName = ShowOpenDialog(AutomataFileFilter);
            if (fileName == null)
            {
                return;
            }

            String dir = Path.GetTempPath();
            File.Delete(dir + "Code.ca");
            File.Delete(dir + "Grid.cag");
            File.Delete(dir + "States.cas");
            if (!ZipFile.IsZipFile(fileName))
            {
                MessageBox.Show(this, "The file " + fileName + " is not the correct format.", "Incorrect Format", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            bool hasCas = false;
            bool hasCag = false;
            bool hasCa = false;

            using (ZipFile file = new ZipFile(fileName))
            {
                foreach (var entry in file.Entries)
                {
                    switch (Path.GetExtension(entry.FileName).ToLower())
                    {
                        case ".cas":
                            entry.Extract(dir);
                            hasCas = true;
                            break;
                        case ".cag":
                            entry.Extract(dir);
                            hasCag = true;
                            break;
                        case ".ca":
                            entry.Extract(dir);
                            hasCa = true;
                            break;
                    }
                }
            }

            if (hasCas && hasCag && hasCa)
            {
                ReadCodeFromFile(dir + "Code.ca");
                ReadGridFromFile(dir + "Grid.cag");
                ReadStatesFromFile(dir + "States.cas");
            }
            else
            {
                MessageBox.Show(this, "The file " + fileName + " is not the correct format.", "Incorrect Format", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Saves the cellular automata definition.
        /// </summary>
        public void SaveCellularAutomataDefinition()
        {
            String fileName = ShowSaveDialog(AutomataFileFilter);
            if (fileName == null)
            {
                return;
            }

            String code = Path.GetTempFileName();
            String states = Path.GetTempFileName();
            String gridState = Path.GetTempFileName();

            WriteCodeToFile(code);
            WriteStatesToFile(states);
            WriteGridToFile(gridState);

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using (ZipFile file = new ZipFile(fileName))
            {
                file.AddFile(code, "/"); // add code file
                file.AddFile(states, "/"); // add state config
                file.AddFile(gridState, "/"); // add grid state

                // update the names to reflect the correct data
                file[Path.GetFileName(code)].FileName = "Code.ca";
                file[Path.GetFileName(states)].FileName = "States.cas";
                file[Path.GetFileName(gridState)].FileName = "Grid.cag";

                file.Save();
            }

            // Let's be nice and clean up the temp files we created :)
            File.Delete(code);
            File.Delete(states);
            File.Delete(gridState);
        }

        /// <summary>
        /// Handles the CollectionChanged event of the StateConfigurations control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void StateConfigurations_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Dispatcher.InvokeIfRequired(() =>
            {
                ClearToBox.Items.Clear();
                foreach (var item in bigGrid.StateConfigurations)
                {
                    ComboBoxItem comboItem = new ComboBoxItem();
                    comboItem.Background = item.Color;
                    comboItem.Content = item.State;
                    ClearToBox.Items.Add(comboItem);
                }
            });
        }

        /// <summary>
        /// Handles the RunWorkerCompleted event of the worker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            taskbarItemInfo.Overlay = null;
        }

        /// <summary>
        /// Handles the GridUpdated event of the bigGrid control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void BigGrid_GridUpdated(object sender, EventArgs e)
        {
            boardUploaded = false;
        }

        /// <summary>
        /// Handles the DoWork event of the worker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.DoWorkEventArgs"/> instance containing the event data.</param>
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.InvokeIfRequired(() => taskbarItemInfo.Overlay = new BitmapImage(new Uri("pack://application:,,,/CellularAutomataClient;component/Images/StatusAnnotation_Run.png")));
            int until = RunUntil ?? Int32.MaxValue;
            while (!worker.CancellationPending && until > CurrentStep)
            {
                Step();
            }

            SimulationRunning = false;
        }

        /// <summary>
        /// Handles the Tick event of the foldingUpdateTimer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void FoldingUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (foldingStrategy != null)
            {
                foldingStrategy.UpdateFoldings(foldingManager, CodeEditor.Document);
            }
        }

        /// <summary>
        /// Handles the CanExecute event of the New command. The new command resets the code window to default.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.CanExecuteRoutedEventArgs"/> instance containing the event data.</param>
        private void New_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        /// <summary>
        /// Handles the Executed event of the New command. The new command resets the code window to default.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        private void New_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoadDefaultCode();
        }

        /// <summary>
        /// Loads the default code.
        /// </summary>
        private void LoadDefaultCode()
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CellularAutomataClient.Resources.Default.cs"))
                using (StreamReader reader = new StreamReader(stream))
                {
                    CodeEditor.Text = reader.ReadToEnd();
                }
            }
            catch (IOException ex)
            {
                ExceptionMessageBox.Show("An error occurred loading the default code from file.", ex);
            }
        }

        /// <summary>
        /// Handles the CanExecute event of the Save control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.CanExecuteRoutedEventArgs"/> instance containing the event data.</param>
        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (CodeEditor == null)
            {
                e.CanExecute = false;
                return;
            }

            e.CanExecute = CodeEditor.Text.Length > 0;
        }

        /// <summary>
        /// Handles the Executed event of the Save control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog sdf = new SaveFileDialog();
            sdf.CheckFileExists = false;
            sdf.OverwritePrompt = true;
            sdf.Filter = TransitionFunctionFileFilter;
            if (sdf.ShowDialog() == true)
            {
                try
                {
                    WriteCodeToFile(sdf.FileName);
                }
                catch (IOException ex)
                {
                    ExceptionMessageBox.Show("Error saving code file.", ex, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error, true, this);
                }
            }
        }

        /// <summary>
        /// Writes the code to file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        private void WriteCodeToFile(String fileName)
        {
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.Write(CodeEditor.Text);
            }
        }

        /// <summary>
        /// Handles the CanExecute event of the Open control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.CanExecuteRoutedEventArgs"/> instance containing the event data.</param>
        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        /// <summary>
        /// Handles the Executed event of the Open control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = TransitionFunctionFileFilter;

            if (ofd.ShowDialog() == true)
            {
                ReadCodeFromFile(ofd.FileName);
            }
        }

        /// <summary>
        /// Reads the code from file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        private void ReadCodeFromFile(String fileName)
        {
            try
            {
                using (StreamReader reader = new StreamReader(fileName))
                {
                    CodeEditor.Text = reader.ReadToEnd();
                }
            }
            catch (IOException ex)
            {
                ExceptionMessageBox.Show("Error loading code file.", ex, "Load Error", MessageBoxButton.OK, MessageBoxImage.Error, true, this);
            }
        }

        /// <summary>
        /// Writes the states to file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        private void WriteStatesToFile(String fileName)
        {
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                foreach (var config in bigGrid.StateConfigurations)
                {
                    writer.Write(config.State);
                    writer.Write(" ");
                    writer.WriteLine(config.Color);
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the Compile button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Compile_Click(object sender, RoutedEventArgs e)
        {
            if (worker.IsBusy)
            {
                worker.CancelAsync();
            }

            var errors = client.Compile(CodeEditor.Text);
            if (errors.IsSuccess)
            {
                MessageBox.Show(this, "Compilation Successful.", "Remote Compilation", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            CurrentStep = 0;
            boardUploaded = false;
            CompilationErrors.Clear();

            if (errors.Messages.Any(error => error.Severity == Severity.Error))
            {
                IsExpanded = true;
            }

            for (int i = 0; i < errors.Messages.Length; i++)
            {
                ErrorLine err = new ErrorLine();
                CompilerMessage msg = errors.Messages[i];
                err.ErrorText = msg.ErrorMessage;
                err.LineNumber = msg.LineNumber;
                err.Severity = msg.Severity.ToString();
                CompilationErrors.Add(err);
            }
        }

        /// <summary>
        /// Initializes the board.
        /// </summary>
        /// <param name="initial">The optional state ID with which to fill the array.</param>
        private void InitalizeBoard(byte initial = 0)
        {
            board = new byte[BoardSize][];
            for (int i = 0; i < board.Length; i++)
            {
                board[i] = new byte[BoardSize];
            }

            if (initial != 0)
            {
                for (int i = 0; i < board.Length; i++)
                {
                    for (int j = 0; j < board.Length; j++)
                    {
                        board[i][j] = initial;
                    }
                }
            }

            boardUploaded = false;
        }

        /// <summary>
        /// Handles the Click event of the StepForward button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void StepForward_Click(object sender, RoutedEventArgs e)
        {
            if (worker.IsBusy)
            {
                worker.CancelAsync();
            }

            Step();
        }

        /// <summary>
        /// Executes one step of the cellular automata.
        /// </summary>
        private void Step()
        {
            if (!boardUploaded)
            {
                boardUploaded = true;
                UploadInitialGrid();
            }

            try
            {
                var newBoard = client.Step();

                Parallel.ForEach(newBoard.AsParallel(), (kv) =>
                    {
                        int x = kv.Key.Item1;
                        int y = kv.Key.Item2;
                        bigGrid.Grid[x][y] = kv.Value;
                    });

                Dispatcher.InvokeIfRequired(() => bigGrid.VisualUpdated());
                CurrentStep++;
            }
            catch (FaultException<InvalidOperationFault> ex)
            {
                if (Dispatcher.CheckAccess())
                {
                    MessageBox.Show(this, ex.Detail.Reason, "Invalid Operation", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    Dispatcher.Invoke(new Action(() => MessageBox.Show(this, ex.Detail.Reason, "Invalid Operation", MessageBoxButton.OK, MessageBoxImage.Error)));
                    if (worker.IsBusy)
                    {
                        worker.CancelAsync();
                    }
                }
            }
            catch (FaultException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Uploads the initial grid to the server.
        /// </summary>
        private void UploadInitialGrid()
        {
            try
            {
                client.Initialize(bigGrid.Grid);
            }
            catch (CommunicationException ex)
            {
                ExceptionMessageBox.Show("An error occurred while communicating with the server.", ex);
            }

            CurrentStep = 0;
        }

        /// <summary>
        /// Runs several steps of the cellular automata.
        /// </summary>
        private void Run()
        {
            if (SimulationRunning)
            {
                worker.CancelAsync();
                SimulationRunning = false;
            }
            else
            {
                worker.RunWorkerAsync();
                SimulationRunning = true;
            }
        }

        /// <summary>
        /// Handles the Click event of the Run button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Run_Click(object sender, RoutedEventArgs e)
        {
            Run();
        }

        /// <summary>
        /// Handles the Click event of the Add StateConfig button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Add_StateConfig_Click(object sender, RoutedEventArgs e)
        {
            bigGrid.StateConfigurations.Add(new StateConfiguration((byte)(bigGrid.StateConfigurations.Max(x => x.State) + 1)));
            bigGrid.VisualUpdated();
        }

        /// <summary>
        /// Handles the Click event of the Remove StateConfig button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Remove_StateConfig_Click(object sender, RoutedEventArgs e)
        {
            if (stateList.SelectedItem != null)
            {
                bigGrid.StateConfigurations.Remove((StateConfiguration)stateList.SelectedItem);
                bigGrid.VisualUpdated();
            }
        }

        /// <summary>
        /// Writes the grid to file.
        /// </summary>
        /// <param name="file">The file.</param>
        private void WriteGridToFile(String file)
        {
            using (StreamWriter writer = new StreamWriter(file))
            {
                for (int i = 0; i < bigGrid.Grid.Length; i++)
                {
                    for (int j = 0; j < bigGrid.Grid[i].Length; j++)
                    {
                        writer.WriteLine(bigGrid.Grid[i][j]);
                    }
                }
            }
        }

        /// <summary>
        /// Reads the grid from file.
        /// </summary>
        /// <param name="file">The file.</param>
        private void ReadGridFromFile(String file)
        {
            using (StreamReader reader = new StreamReader(file))
            {
                for (int i = 0; i < bigGrid.Grid.Length; i++)
                {
                    for (int j = 0; j < bigGrid.Grid.Length; j++)
                    {
                        String str = reader.ReadLine();
                        if (str == null)
                        {
                            MessageBox.Show(this, "File ended unexpectedly--cannot finish loading Grid.", "File Termination Unexpected", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        bigGrid.Grid[i][j] = Byte.Parse(str);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the SaveGrid button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SaveGrid_Click(object sender, RoutedEventArgs e)
        {
            if (worker.IsBusy)
            {
                worker.CancelAsync();
            }

            String file = ShowSaveDialog(GridFileFilter);
            if (file != null)
            {
                WriteGridToFile(file);
            }
        }

        /// <summary>
        /// Handles the Click event of the LoadGrid button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void LoadGrid_Click(object sender, RoutedEventArgs e)
        {
            boardUploaded = false;
            String file = ShowOpenDialog(GridFileFilter);
            if (file == null)
            {
                return;
            }

            ReadGridFromFile(file);
            bigGrid.VisualUpdated();
        }

        /// <summary>
        /// Shows the save dialog.
        /// </summary>
        /// <param name="fileFilter">The file filter.</param>
        /// <returns>The name of the file selected, or <c>null</c> if no file was selected.</returns>
        private String ShowSaveDialog(String fileFilter)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.AddExtension = true;
            sfd.OverwritePrompt = true;
            sfd.Filter = fileFilter ?? AllFileFilter;

            // silly bool? type...
            if (sfd.ShowDialog(this) == true)
            {
                return sfd.FileName;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Shows the open dialog.
        /// </summary>
        /// <param name="fileFilter">The file filter.</param>
        /// <returns>The name of the file selected, or <c>null</c> if no file was selected.</returns>
        private String ShowOpenDialog(String fileFilter)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.CheckFileExists = true;
            ofd.AddExtension = true;
            ofd.Filter = fileFilter ?? AllFileFilter;

            // silly bool? type...
            if (ofd.ShowDialog(this) == true)
            {
                return ofd.FileName;
            }

            return null;
        }

        /// <summary>
        /// Handles the Click event of the SaveStates button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SaveStates_Click(object sender, RoutedEventArgs e)
        {
            String fileName = ShowSaveDialog(StateConfigFilter);
            if (fileName == null)
            {
                return;
            }

            WriteStatesToFile(fileName);
        }

        /// <summary>
        /// Handles the Click event of the LoadStates button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void LoadStates_Click(object sender, RoutedEventArgs e)
        {
            String fileName = ShowOpenDialog(StateConfigFilter);
            if (fileName == null)
            {
                return;
            }

            ReadStatesFromFile(fileName);
        }

        /// <summary>
        /// Reads the states from file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        private void ReadStatesFromFile(string fileName)
        {
            bigGrid.StateConfigurations.Clear();
            using (StreamReader reader = new StreamReader(fileName))
            {
                while (!reader.EndOfStream)
                {
                    String line = reader.ReadLine();
                    var split = line.Split(' ');
                    if (split.Length != 2)
                    {
                        MessageBox.Show(this, "Unable to state or color data from state configuration file. Aborting.", "State Data Unreadable", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    byte state;
                    if (Byte.TryParse(split[0], out state))
                    {
                        try
                        {
                            bigGrid.StateConfigurations.Add(new StateConfiguration(state, ParseColor(split[1])));
                        }
                        catch
                        {
                            MessageBox.Show(this, "Unable to state or color data from state configuration file. Aborting.", "State Data Unreadable", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show(this, "Unable to state or color data from state configuration file. Aborting.", "State Data Unreadable", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Parses the color.
        /// </summary>
        /// <param name="str">The string from which to parse the color.</param>
        /// <returns>The color parsed from the string.</returns>
        private Color ParseColor(String str)
        {
            return (Color)ColorConverter.ConvertFromString(str);
        }

        /// <summary>
        /// Handles the Click event of the SaveDefinition control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SaveDefinition_Click(object sender, RoutedEventArgs e)
        {
            if (worker.IsBusy)
            {
                worker.CancelAsync();
            }

            SaveCellularAutomataDefinition();
        }

        /// <summary>
        /// Handles the Click event of the OpenDefinition control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void OpenDefinition_Click(object sender, RoutedEventArgs e)
        {
            LoadCellularAutomataDefinition();
            bigGrid.VisualUpdated();
        }

        /// <summary>
        /// Handles the Click event of the Clear button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (ClearToState != null)
            {
                InitalizeBoard(ClearToState.State);
            }
            else
            {
                InitalizeBoard();
            }

            bigGrid.Grid = board;
            bigGrid.VisualUpdated();
        }

        /// <summary>
        /// Handles the Closing event of the Window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                if (client != null && client.State == CommunicationState.Opened)
                {
                    client.Close();
                }
            }
            catch (Exception)
            {
                client.Abort();
            }
        }

        /// <summary>
        /// Handles the GotFocus event of the TabItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void TabItem_GotFocus(object sender, RoutedEventArgs e)
        {
            bigGrid.VisualUpdated();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the ClearToBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ClearToBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearToState = bigGrid.StateConfigurations.SingleOrDefault(x =>
            {
                var content = ((ComboBoxItem)ClearToBox.SelectedItem)?.Content;
                return content != null && x.State == (byte) content;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
