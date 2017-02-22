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

        public MainWindow()
        {
            InitializeComponent();

            index = 0;
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
                    item.Remaining = "Zrušeno";                    
                }
                else
                {
                    item.Progress = 100;
                    item.Remaining = "Dokončeno";
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
            string[] words = { "sekund", "minut", "hodin" };                        
            string str = " ";
            int[] time = new int[3];            
            
            time[0] = Convert.ToInt32(seconds);
            time[1] = time[0] / 60;
            time[0] = time[0] - (time[1] * 60);
            time[2] = time[1] / 60;
            time[1] = time[1] - (time[2] * 60);
            
            for (int i = 0; i < 3; i++) 
            {
                if (time[i] == 1) words[i] += 'a';
                else if (time[i] >= 2 && time[i] <= 4) words[i] += 'y';                
            }

            if (time[2] == 0 && time[1] == 0) str = string.Format("{0} {1}", time[0], words[0]);             
            else if (time[1] >= 1 && time[2] == 0) str = string.Format("{0} {1}", time[1], words[1]);            
            else if (time[2] >= 1 && time[1] == 0) str = string.Format("{0} {1}", time[2], words[2]);            
            else str = string.Format("{0} {1} a {2} {3}", time[2], words[2], time[1], words[1]);          

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

            if (MessageBox.Show("Opravdu chcete smazat tento soubor?", "Zrušit", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MyData item = DataView.SelectedItem as MyData;
                item.Client.Cancel();                
            }
        }

        private void buttonPause_Click(object sender, RoutedEventArgs e)
        {
            MyData item = DataView.SelectedItem as MyData;
            item.Client.Pause();
            item.Remaining = "Pozastaveno";
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
            settingsWindow.ShowDialog();
        }

        private void buttonLimit_Click(object sender, RoutedEventArgs e)
        {

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
