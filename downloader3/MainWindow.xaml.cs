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

                DataView.Items.Add(new MyData() { Name = linksWindows.filename, Progress = 0 });

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
            sw.Reset();            
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

        /*private string ConvertTime(double dSeconds)
        {
            string hoursWord;
            string minutesWord;
            string secondsWord;

            int seconds = Convert.ToInt32(dSeconds);
            int minutes = seconds / 60;
            int hours = minutes / 60;

            if (hours == 1) hoursWord = "hodina";
            else if (hours >= 2 && hours <= 4) hoursWord = "hodiny";
            else hoursWord = "hodin";

            if (minutes == 1) minutesWord = "minuta";
            else if (minutes >= 2 && minutes <= 4) minutesWord = "minuty";
            else minutesWord = "minut";

            if (seconds == 1) secondsWord = "sekunda";
            else if (seconds >= 2 && seconds <= 4) secondsWord = "sekundy";
            else secondsWord = "sekund";

            if (seconds > 3600)
            {
                minutes = minutes - (hours * 60);
                return string.Format("{0} {1} a {2} {3}", hours, hoursWord, minutes, minutesWord);
            }
            if (seconds >= 60) return string.Format("{0} {1}", minutes, minutesWord);
            if (seconds < 60) return string.Format("{0} {1}", seconds, secondsWord);

            return "error";
        }*/

        private string ConvertTime(double dSeconds)
        {   
            string[] words = { "sekund", "minut", "hodin" };
            char letter;
            int seconds, minutes, hours, unit;
            string str;
            
            seconds = Convert.ToInt32(dSeconds);
            minutes = 0;
            hours = 0;            

            /*if (unit == 1) letter = 'a';
            else if (unit >= 2 && unit <= 4) letter = 'y';
            else letter = '\0';*/

            str = string.Format("{0} {1}", seconds, words[0]);

            if (seconds >= 60) //minuty
            {
                minutes = seconds / 60;
                seconds = seconds % minutes;
                str = string.Format("{0} {1}", minutes, words[1]);
            }
            else if (minutes >= 60) //hodiny
            {
                hours = minutes / 60;
                minutes = minutes % hours;
                str = string.Format("{0} {1} a {2} {3}", hours, words[2], minutes, words[1]);
            }            

            return str;           
        }
                

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {            
            if (DataView.SelectedIndex == -1)
            {
                return;
            }
            MyData item = DataView.SelectedItem as MyData;
            Process.Start(item.Name);           

        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (DataView.SelectedIndex == -1)
            {
                return;
            }
            MyData item = DataView.SelectedItem as MyData;
            Process.Start(System.IO.Path.GetDirectoryName(item.Name));
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(ConvertTime(Convert.ToDouble(textBox.Text)));
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
    }  
    
    public class WebClientEx : WebClient
    {
        public int Index { get; set; }
        public string Filename { get; set; }
    }  
}
