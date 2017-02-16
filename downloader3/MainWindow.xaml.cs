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

namespace downloader3
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {              
        double speed;
        int index;

        Stopwatch sw = new Stopwatch();

        public MainWindow()
        {
            InitializeComponent();

            index = 0;
        }        

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            Window1 linksWindows = new Window1();
            linksWindows.Owner = this;
            if (linksWindows.ShowDialog() == true) {
                WebClientEx client = new WebClientEx();
                client.Proxy = null; //no lag
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                client.DownloadFileAsync(new Uri(linksWindows.url), linksWindows.filename);
                client.DownloadProgressChanged += wc_DownloadProgressChanged;
                client.Index = index;
                client.Filename = linksWindows.filename;
                sw.Start();

                DataView.Items.Add(new MyData() { Name = linksWindows.filename, Progress = 0, Client = client });

                index++;                
            }
        }

        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            WebClientEx c = sender as WebClientEx;

            speed = e.BytesReceived / 1024 / sw.Elapsed.TotalSeconds;
            MyData item = DataView.Items[c.Index] as MyData;
            item.Name = c.Filename;
            item.Size = string.Format("{0} / {1}", ConvertFileSize(e.BytesReceived), ConvertFileSize(e.TotalBytesToReceive));
            item.Progress = e.ProgressPercentage;
            item.Percent = e.ProgressPercentage;
            item.Speed = string.Format("{0} kB/s", speed.ToString("0.0"));            
            item.Remaining = ConvertTime((e.TotalBytesToReceive - e.BytesReceived) * sw.Elapsed.TotalSeconds / e.BytesReceived);            

            DataView.Items.Refresh();
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            WebClientEx c = sender as WebClientEx;
            sw.Reset();
            if (e.Cancelled)
            {
                File.Delete(c.Filename);
                c.Dispose();                
                return;
            }
        }

        private string ConvertFileSize(long bytes)
        {
            double MB = Math.Pow(1024, 2);
            double GB = Math.Pow(1024, 3);
            double TB = Math.Pow(1024, 4);

            if (bytes >= TB) return string.Format("{0:0.00} TB", (bytes / TB));
            else if (bytes >= GB) return string.Format("{0:0.00} GB", (bytes / GB));
            else if (bytes >= 1) return string.Format("{0:0.00} MB", (bytes / MB));

            else return "error";
        }        

        private string ConvertTime(double dSeconds)
        {   
            string[] words = { "sekund", "minut", "hodin" };                        
            string str = " ";
            int[] time = new int[3];
            
            time[0] = Convert.ToInt32(dSeconds);
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
            Process.Start(item.Name);         
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (DataView.SelectedIndex == -1) return;

            MyData item = DataView.SelectedItem as MyData;           

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = "explorer.exe";
            startInfo.Arguments = "/select, " + item.Name;
            process.StartInfo = startInfo;
            process.Start();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (DataView.SelectedIndex == -1) return;

            if (MessageBox.Show("Opravdu chcete smazat tento soubor?", "Zrušit", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MyData item = DataView.SelectedItem as MyData;
                item.Client.CancelAsync();
                item.Size = "";
                item.Progress = 0;
                item.Percent = 0;
                item.Speed = "";
                item.Remaining = "Zrušeno";
                DataView.Items.Refresh();
            }
        }
    }

    internal class MyData
    {
        public string Name { get; set; }
        public string Size { get; set; }
        public int Progress { get; set; }
        public int Percent { get; set; }
        public string Speed { get; set; }
        public string Remaining { get; set; }
        public WebClientEx Client;
    }  
    
    public class WebClientEx : WebClient
    {
        public int Index { get; set; }
        public string Filename { get; set; }
    }  
}
