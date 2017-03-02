using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Threading;
using System.Drawing;

namespace downloader3
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        double speed;
        int index;
        DownloadClient client;

        long speedLimit = 1000;     //kB/s
        int langIndex = 0;          //čeština
        

        public MainWindow()
        {
            InitializeComponent();
            index = 0;
            
            speedLimit = Properties.Settings.Default.speedlimit;
            App.SelectCulture(Properties.Settings.Default.language);

        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            LinksWindow linksWindows = new LinksWindow();
            linksWindows.Owner = this;
            if (linksWindows.ShowDialog() == true)
            {
                client = new DownloadClient(linksWindows.url, linksWindows.filename);
                client.OnDownloadProgressChanged += Client_OnDownloadProgressChanged;
                client.OnDownloadProgressCompleted += Client_OnDownloadProgressCompleted;
                client.Index = index;
                client.SpeedLimit = speedLimit;
                client.Start();
                MyData item = new MyData();
                item.Name = System.IO.Path.GetFileName(linksWindows.filename);
                item.Filename = linksWindows.filename;
                item.Progress = 0;
                item.Client = client;
                DataView.Items.Add(item);
                index++;
            }
        }

        private void Client_OnDownloadProgressChanged(object sender)
        {
            DownloadClient c = sender as DownloadClient;

            MyData item = DataView.Items[c.Index] as MyData;
            item.Size = string.Format("{0} / {1}", ConvertFileSize(c.BytesDownloaded), ConvertFileSize(c.FileSize));
            item.Progress = c.Percentage;
            speed = c.BytesPerSecond / 1024;
            item.Speed = string.Format("{0} kB/s", speed.ToString("0.0"));
            item.Remaining = ConvertTime(c.SecondsRemaining);
            var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(item.Filename);
            var bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            sysicon.Dispose();
            item.Icon = bmpSrc;

            DataView.Items.Refresh();
        }

        private void Client_OnDownloadProgressCompleted(object sender, bool cancel)
        {
            Dispatcher.Invoke(delegate
            {
                DownloadClient c = sender as DownloadClient;
                MyData item = DataView.Items[c.Index] as MyData;
                if (cancel)
                {
                    item.Size = "";
                    item.Progress = 0;
                    item.Speed = "";
                    item.Remaining = Translate("lang_canceled");
                }
                else
                {
                    item.Progress = 100;
                    item.Remaining = Translate("lang_completed");
                }

                DataView.Items.Refresh();
            });
        }

        private string ConvertFileSize(long bytes)
        {
            double MB = Math.Pow(1024, 2);
            double GB = Math.Pow(1024, 3);
            double TB = Math.Pow(1024, 4);

            if (bytes >= TB) return string.Format("{0:0.0} TB", (bytes / TB));
            else if (bytes >= GB) return string.Format("{0:0.0} GB", (bytes / GB));
            else if (bytes >= 1) return string.Format("{0:0.0} MB", (bytes / MB));

            else return "error";
        }

        private string ConvertTime(double seconds)
        {
            string str = " ";
            int sec, min, hour;

            sec = Convert.ToInt32(seconds);
            min = sec / 60;
            sec = sec - (min * 60);
            hour = min / 60;
            min = min - (hour * 60);

            if (hour == 0 && min == 0) str = string.Format("{0}s", sec);
            else if (min >= 1 && hour == 0) str = string.Format("{0}m {1}s", min, sec);
            else if (hour >= 1 && min == 0) str = string.Format("{0}h", hour);
            else str = string.Format("{0}h a {1}m", hour, min);

            return str;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataView.SelectedIndex == -1) return;

            MyData item = DataView.SelectedItem as MyData;
            Process.Start(item.Filename);
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (DataView.SelectedIndex == -1) return;

            MyData item = DataView.SelectedItem as MyData;

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "explorer.exe";
            startInfo.Arguments = "/select, " + item.Filename;
            process.StartInfo = startInfo;
            process.Start();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (DataView.SelectedIndex == -1) return;

            if (MessageBox.Show(Translate("lang_confirm_delete"), Translate("lang_cancel"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MyData item = DataView.SelectedItem as MyData;
                item.Client.Cancel();
            }
        }

        private void buttonPause_Click(object sender, RoutedEventArgs e)
        {
            MyData item = DataView.SelectedItem as MyData;
            item.Client.Pause();
            item.Remaining = Translate("lang_paused");
            item.Speed = "";
            DataView.Items.Refresh();
        }

        private void buttonResume_Click(object sender, RoutedEventArgs e)
        {
            MyData item = DataView.SelectedItem as MyData;
            item.Client.Resume();
        }

        private void buttonSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.speedLimit.Text = speedLimit.ToString();
            if (Properties.Settings.Default.language == "cs-CZ") settingsWindow.langSelection.SelectedIndex = 0;
            if (Properties.Settings.Default.language == "en-US") settingsWindow.langSelection.SelectedIndex = 1;

            if (settingsWindow.ShowDialog() == true) //save settings
            {
                speedLimit = Int64.Parse(settingsWindow.speedLimit.Text);
                langIndex = settingsWindow.langSelection.SelectedIndex;

                Properties.Settings.Default.speedlimit = speedLimit;
                Properties.Settings.Default.language = settingsWindow.language;
                App.SelectCulture(Properties.Settings.Default.language);
                Properties.Settings.Default.Save();

                RefreshLanguage();
            }
        }

        public void RefreshLanguage()
        {
            foreach (MyData item in DataView.Items)
            {
                if (item.Client.Paused) item.Remaining = Translate("lang_paused");
                if (item.Client.Canceled) item.Remaining = Translate("lang_canceled");
                if (item.Client.Completed) item.Remaining = Translate("lang_completed");
            }
            DataView.Items.Refresh();
        }

        public string Translate(string resource)
        {
            string result = (string)TryFindResource(resource);
            if (result == null)
            {
                MessageBox.Show("Language resource \"" + resource + "\" is invalid", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return resource;
            }
            return result;
        }
    }

    internal class MyData
    {
        public ImageSource Icon { get; set; }
        public string Name { get; set; }
        public string Filename { get; set; }
        public string Size { get; set; }
        public float Progress { get; set; }
        public string Speed { get; set; }
        public string Remaining { get; set; }
        public DownloadClient Client;
    }
}
