﻿using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Timers;
using System.Windows.Documents;
using System;

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
            connectionTimer.Elapsed += RefreshLog;
            connectionTimer.Interval = 30000;
            connectionTimer.Start();
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
                Manager.GetUserFromServer(true);
            }
            Thread.Sleep(5000);
            Manager.StartSending(Manager.DriverCount);
        }
        
        private void PauseButtonClick(object sender, RoutedEventArgs e)
        {

            if (Pause.IsChecked.Value)
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
        private void RefreshLog(object sender , ElapsedEventArgs? e)
        {
            LogTextBox.Dispatcher.Invoke(() =>
            {
                LogTextBox.Document.Blocks.Clear();
                var flow = new FlowDocument();
                if (Manager.worker1 is not null)
                {
                    var para = new Paragraph();
                    var Worker1Header = new Run("W1_Online: " + Manager.worker1.Username + System.Environment.NewLine);
                    var Worker1Line1 = new Run(Manager.worker1.User1 + System.Environment.NewLine + Manager.worker1.User2);

                    Worker1Header.Foreground = new SolidColorBrush(Color.FromArgb(100, 96, 156, 224));
                    Worker1Line1.Foreground = new SolidColorBrush(Color.FromArgb(100, 65, 115, 171));
                    para.Inlines.Add(Worker1Header);
                    para.Inlines.Add(Worker1Line1);
                    flow.Blocks.Add(para);
                }
                if (Manager.worker2 is not null)
                {
                    var para = new Paragraph();
                    var Worker2Header = new Run("W2_Online: " + Manager.worker2.Username + System.Environment.NewLine);
                    var Worker2Line1 = new Run(Manager.worker2.User1 + ": " + System.Environment.NewLine + Manager.worker2.User2);
                    Worker2Header.Foreground = new SolidColorBrush(Color.FromArgb(100, 96, 156, 224));
                    Worker2Line1.Foreground = new SolidColorBrush(Color.FromArgb(100, 65, 115, 171));

                    para.Inlines.Add(Worker2Header);
                    para.Inlines.Add(Worker2Line1);
                    flow.Blocks.Add(para);
                }
                if (Manager.worker3 is not null)
                {
                    var para = new Paragraph();
                    var Worker3Header = new Run("W3_Online: " + Manager.worker3.Username + ": " + System.Environment.NewLine);
                    var Worker3Line1 = new Run(Manager.worker3.User1 + ": " + System.Environment.NewLine + Manager.worker3.User2);

                    Worker3Header.Foreground = new SolidColorBrush(Color.FromArgb(100, 96, 156, 224));
                    Worker3Line1.Foreground = new SolidColorBrush(Color.FromArgb(100, 65, 115, 171));

                    para.Inlines.Add(Worker3Header);
                    para.Inlines.Add(Worker3Line1);
                    flow.Blocks.Add(para);
                }
                if (Manager.worker4 is not null)
                {
                    var para = new Paragraph();
                    var Worker4Header = new Run("W4_Online: " + Manager.worker4.Username + ": " + System.Environment.NewLine);
                    var Worker4Line1 = new Run(Manager.worker4.User1 + ": " + System.Environment.NewLine+ Manager.worker4.User2);

                    Worker4Header.Foreground = new SolidColorBrush(Color.FromArgb(100, 96, 156, 224));
                    Worker4Line1.Foreground = new SolidColorBrush(Color.FromArgb(100, 65, 115, 171));

                    para.Inlines.Add(Worker4Header);
                    para.Inlines.Add(Worker4Line1);
                    flow.Blocks.Add(para);
                }


                LogTextBox.Document = flow;

            });
        }
    }
}