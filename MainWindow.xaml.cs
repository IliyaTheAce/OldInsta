using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Timers;

namespace Insta_DM_Bot_server_wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow 
    {
        public MainWindow()
        {
            InitializeComponent();
            var connectionTimer = new System.Timers.Timer();
            connectionTimer.Elapsed += CheckConnectionTick;
            connectionTimer.Interval = 30000;
            connectionTimer.Start();
            Toggle2.IsEnabled = false;
            Manager.DriverCount = 2;
        }
        private void CheckConnectionTick(object? source, ElapsedEventArgs? e)
        {
            Task.Run(() =>
            {
                Manager.ConnectionToNet = Manager.IsConnectedToInternet();

                Manager.ConnectionToServer = Manager.IsConnectedToServer();
            });
            Thread.Sleep(2000);
            if (Manager.ConnectionToNet)
            {
                NetConnectionStatus.Dispatcher.Invoke(() => {
                    NetConnectionStatus.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 154, 59));
                });
            }
            else {
                NetConnectionStatus.Dispatcher.Invoke(() => {

                    NetConnectionStatus.Fill = new SolidColorBrush(Color.FromArgb(100, 139, 0, 0));
                });
            }

            if (Manager.ConnectionToServer)
            {
                ServerConnectionStatus.Dispatcher.Invoke(() =>
                {
                    ServerConnectionStatus.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 154, 59));
                });
            }
            else
            {
                ServerConnectionStatus.Dispatcher.Invoke(() =>
                {
                    ServerConnectionStatus.Fill = new SolidColorBrush(Color.FromArgb(100, 139, 0, 0));
                });
            }
        }

        private void ExitApp(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MinApp(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var dialog = MessageBox.Show("Start Sending?", "Did you check everything?", MessageBoxButton.YesNo , MessageBoxImage.Question);
            if (dialog == MessageBoxResult.No) return;
            for (int i = 0; i < Manager.DriverCount; i++)
            {
                Manager.GetUserFromServer();
            }
            Thread.Sleep(5000);
            Manager.StartSending();
        }

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (((bool)Pause.IsChecked)!)
            {
                Manager.IsPaused = true;
            }
            else
            {
                Manager.IsPaused = false;
            }
        }

        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Toggle1DriverCount(object sender, RoutedEventArgs e)
        {
                    Toggle1.IsEnabled = false;
                    Toggle2.IsEnabled = true;
                    Toggle3.IsEnabled = true;
                    Toggle4.IsEnabled = true;
            Manager.DriverCount = 1;
        }
        private void Toggle2DriverCount(object sender, RoutedEventArgs e)
        {
            Toggle1.IsEnabled = true;
            Toggle2.IsEnabled = false;
            Toggle3.IsEnabled = true;
            Toggle4.IsEnabled = true;
            Manager.DriverCount = 2;
        }
        private void Toggle3DriverCount(object sender, RoutedEventArgs e)
        {
            Toggle1.IsEnabled = true;
            Toggle2.IsEnabled = true;
            Toggle3.IsEnabled = false;
            Toggle4.IsEnabled = true;
            Manager.DriverCount = 3;
        }
        private void Toggle4DriverCount(object sender, RoutedEventArgs e)
        {
            Toggle1.IsEnabled = true;
            Toggle2.IsEnabled = true;
            Toggle3.IsEnabled = true;
            Toggle4.IsEnabled = false;
            Manager.DriverCount = 4;
        }

        private void Pause_Checked(object sender, RoutedEventArgs e)
        {
            Manager.IsPaused = (bool)Pause.IsChecked;
        }
    }
}
