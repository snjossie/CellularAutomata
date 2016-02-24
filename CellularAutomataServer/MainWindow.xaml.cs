using System;
using System.Reflection;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CellularAutomataLibrary;

namespace CellularAutomataServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The service host.
        /// </summary>
        private ServiceHost host;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            stopButton.IsEnabled = false;
            this.Title += " " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Initialize();
        }

        /// <summary>
        /// Initializes the service host.
        /// </summary>
        private void Initialize()
        {
            if (host == null || host.State == CommunicationState.Closed)
            {
                host = null;

                host = new ServiceHost(typeof(RemoteCompilation));
                host.Opened += new EventHandler(Host_Opened);
                host.Closed += new EventHandler(Host_Closed);
            }
        }

        /// <summary>
        /// Handles the Closed event of the host control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Host_Closed(object sender, EventArgs e)
        {
            stopButton.IsEnabled = false;
            startButton.IsEnabled = true;
            taskbarItemInfo.Overlay = new BitmapImage(new Uri("pack://application:,,,/CellularAutomataServer;component/Images/StatusAnnotation_Stop.png"));
        }

        /// <summary>
        /// Handles the Opened event of the host control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Host_Opened(object sender, EventArgs e)
        {
            stopButton.IsEnabled = true;
            startButton.IsEnabled = false;
            taskbarItemInfo.Overlay = new BitmapImage(new Uri("pack://application:,,,/CellularAutomataServer;component/Images/StatusAnnotation_Run.png"));
        }

        /// <summary>
        /// Handles the Click event of the Start Button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Start_Button_Click(object sender, RoutedEventArgs e)
        {
            Log("Starting server...");

            try
            {
                Initialize();
                host.Open();
                Log(String.Format("{0} started with the following endpoints: ", host.Description.ConfigurationName));

                foreach (var ep in host.Description.Endpoints)
                {
                    Log(String.Format("\t{0}  --  {1}", ep.Name, ep.Address.ToString()));
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                host.Abort();
            }
        }

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        private void Log(String message)
        {
            var item = new ListBoxItem() { Content = message };
            logWindow.Items.Add(item);
            logWindow.ScrollIntoView(item);
        }

        /// <summary>
        /// Handles the Closing event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Log("Closing host...");
            host.Close();
            Log("Host closed");
        }

        /// <summary>
        /// Handles the Click event of the Stop Button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            Log("Closing host...");
            host.Close();
            Log("Host closed");
        }

        /// <summary>
        /// Handles the Click event of the Clear Button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Clear_Button_Click(object sender, RoutedEventArgs e)
        {
            logWindow.Items.Clear();
        }

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Start_Button_Click(this, new RoutedEventArgs());
        }
    }
}
