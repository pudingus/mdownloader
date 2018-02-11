using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Interop;
using System.Drawing;

namespace downloader3
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        private DispatcherTimer timer = new DispatcherTimer();
        public SettingsStorage Settings = new SettingsStorage();        

        public MainWindow()
        {
            InitializeComponent();            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            buttonPause.IsEnabled = false;
            buttonResume.IsEnabled = false;
            buttonCancel.IsEnabled = false;

            timer.Tick += new EventHandler(Timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 1); //1 sekunda
            timer.Start();

            Settings = Settings.Load();
            Settings.Save();

            SelectCulture(Settings.Language);

            LinksStorage links = new LinksStorage();
            links = links.Load();

            foreach (Link link in links.List)
            {
                string path = Path.Combine(link.Directory, link.FileName);
                MyData item = new MyData();
                FileInfo file = new FileInfo(path);

                item.Name = link.FileName;
                item.Client = new DownloadClient(link.Url, link.Directory, item, link.TotalBytes, link.FileName);
                item.Client.OnDownloadInit += Client_OnDownloadInit;
                item.Client.OnDownloadCompleted += Client_OnDownloadCompleted;
                item.Client.SpeedLimit = link.SpeedLimit;                

                Refresh(item);
                                
                //jestli soubor na seznamu existuje, použít tu ikonu, jinak vytvořit dočasný soubor
                if (File.Exists(path))
                { 
                    var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                    var bmpSrc = Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    sysicon.Dispose();
                    item.Icon = bmpSrc;
                }
                else
                {
                    string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + Path.GetExtension(link.FileName));
                    FileStream fs = File.Create(tempPath);

                    var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(tempPath);
                    var bmpSrc = Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    sysicon.Dispose();
                    item.Icon = bmpSrc;

                    fs.Close();
                    File.Delete(tempPath);
                }
                DataView.Items.Add(item);
            }
        }

        private void Client_OnDownloadInit(DownloadClient client, MyData item)
        {
            string path = Path.Combine(client.Directory, client.FileName);
            var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            var bmpSrc = Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            sysicon.Dispose();
            item.Icon = bmpSrc;
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            LinksWindow linksWindows = new LinksWindow();
            linksWindows.Owner = this;
            if (linksWindows.ShowDialog() == true)
            {   
                for (int i = 0; i < linksWindows.linksTextBox.LineCount; i++)
                {
                    string url = linksWindows.linksTextBox.GetLineText(i);

                    string dir = linksWindows.pathTextBox.Text;
                    
                    MyData item = new MyData();                    
                    item.Client = new DownloadClient(url, dir, item);
                    item.Name = item.Client.FileName;
                    item.Client.OnDownloadInit += Client_OnDownloadInit;
                    item.Client.OnDownloadCompleted += Client_OnDownloadCompleted;
                    item.Client.SpeedLimit = Settings.SpeedLimit;

                    int downloadingCount = 0;
                    foreach (MyData it in DataView.Items)
                        if (it.Client.State == DCStates.Downloading) downloadingCount++;

                    if (downloadingCount < Settings.MaxDownloads) item.Client.Start();
                    else item.Client.Queue();                    

                    Refresh(item);
                    DataView.Items.Add(item);                    
                }                
            }
        }

        private void buttonPause_Click(object sender, RoutedEventArgs e)
        {
            MyData item = DataView.SelectedItem as MyData;
            item.Client.Pause();
            Refresh(item);
            DataView.Items.Refresh();            
        }

        private void buttonResume_Click(object sender, RoutedEventArgs e)
        {
            MyData item = DataView.SelectedItem as MyData;        

            item.Client.Start();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (DataView.SelectedIndex == -1) return;
            if (MessageBox.Show(Translate("lang_confirm_remove"), Translate("lang_cancel"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MyData item = DataView.SelectedItem as MyData;
                item.Client.Cancel();                
            }
        }

        private void buttonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (DataView.SelectedIndex == -1) return;
            if (MessageBox.Show(Translate("lang_confirm_remove") + "\r" + Translate("lang_confirm_remove2"), Translate("lang_cancel"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                MyData item = DataView.SelectedItem as MyData;
                item.Client.Cancel();

                int fak = DataView.SelectedIndex;
                if (DataView.Items.Count == fak + 1) fak--;  //pokud je vybraná položka poslední, vybraný index se posune o jedno zpět, jinak zůstane stejný
                DataView.Items.Remove(item);
                DataView.SelectedIndex = fak; //ikdyž zůstane stejný, je potřeba ho znovu vybrat
            }
        }

        private void buttonUp_Click(object sender, RoutedEventArgs e)
        {
            if (DataView.SelectedIndex == -1) return;
            int index = DataView.SelectedIndex;

            if (index > 0)
            {
                object item = DataView.SelectedItem;
                DataView.Items.RemoveAt(index);
                DataView.Items.Insert(index - 1, item);
                DataView.SelectedItems.Add(item);
            }
        }

        private void buttonDown_Click(object sender, RoutedEventArgs e)
        {
            if (DataView.SelectedIndex == -1) return;
            int index = DataView.SelectedIndex;

            if (index < DataView.Items.Count - 1)
            {
                object item = DataView.SelectedItem;
                DataView.Items.RemoveAt(index);
                DataView.Items.Insert(index + 1, item);
                DataView.SelectedItems.Add(item);
            }
        }

        private void buttonSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.speedLimit.Text = Settings.SpeedLimit.ToString();
            settingsWindow.maxDownloads.Text = Settings.MaxDownloads.ToString();
            if (Settings.Language == "cs-CZ") settingsWindow.langSelection.SelectedIndex = 0;
            if (Settings.Language == "en-US") settingsWindow.langSelection.SelectedIndex = 1;
            if (settingsWindow.ShowDialog() == true) //uložit nastavení
            {
                Settings.SpeedLimit = Int32.Parse(settingsWindow.speedLimit.Text);
                Settings.Language = settingsWindow.language;
                Settings.MaxDownloads = Int32.Parse(settingsWindow.maxDownloads.Text);                
                Settings.Save();

                SelectCulture(Settings.Language);
                foreach (MyData item in DataView.Items) Refresh(item);
                DataView.Items.Refresh();
            }
        }        

        private void Timer_Tick(object sender, EventArgs e) //1 sekunda
        {
            int downloadingCount = 0;
            foreach (MyData item in DataView.Items)
            {                
                if (item.Client.State == DCStates.Downloading)
                {
                    Refresh(item);
                    downloadingCount++;
                }            
            }
            if (downloadingCount > 0) DataView.Items.Refresh();
        }
        
        public void Refresh(MyData item)
        {
            item.Name = item.Client.FileName;
            item.Progress = (double)item.Client.BytesDownloaded / item.Client.TotalBytes * 100;
            item.Size = string.Format("{0}/{1}", ConvertFileSize(item.Client.BytesDownloaded), ConvertFileSize(item.Client.TotalBytes));
            double speed = item.Client.BytesPerSecond / 1024;
            item.Speed = string.Format("{0} ({1}) KB/s ", speed.ToString("0.0"), item.Client.SpeedLimit);

            if (item.Client.State == DCStates.Paused) item.Remaining = Translate("lang_paused");
            else if (item.Client.State == DCStates.Queue) item.Remaining = Translate("lang_inqueue");
            else if (item.Client.State == DCStates.Canceled) item.Remaining = Translate("lang_canceled");
            else if (item.Client.State == DCStates.Completed) item.Remaining = Translate("lang_completed");
            else if (item.Client.State == DCStates.Downloading)
            {
                long sec = 0;
                if (item.Client.BytesDownloaded > 0) sec = (item.Client.TotalBytes - item.Client.BytesDownloaded) * (long)item.Client.Elapsed / item.Client.BytesDownloaded;
                item.Remaining = ConvertTime(sec);
            }                         
        }

        private void Client_OnDownloadCompleted(DownloadClient client, MyData item)
        {
            string path = Path.Combine(item.Client.Directory, item.Client.FileName);
            var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            var bmpSrc = Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            sysicon.Dispose();
            item.Icon = bmpSrc;
            Refresh(item);
            DataView.Items.Refresh();

            int downloadingCount = 0;
            int initCount = 0;
            foreach (MyData it in DataView.Items)
            {
                if (it.Client.State == DCStates.Downloading) downloadingCount++;
                else if (it.Client.State == DCStates.Queue) initCount++;
            }

            if (downloadingCount < Settings.MaxDownloads && initCount > 0)
            {
                bool found = false;
                int j = 0;
                while (!found && j < DataView.Items.Count)
                {
                    MyData it = DataView.Items[j] as MyData;
                    if (it.Client.State == DCStates.Queue)
                    {
                        it.Client.Start();
                        found = true;
                    }
                    j++;
                }
            }
        }

        private string ConvertFileSize(long bytes)
        {
            double KB = Math.Pow(1024, 1);
            double MB = Math.Pow(1024, 2);
            double GB = Math.Pow(1024, 3);
            double TB = Math.Pow(1024, 4);

            if (bytes >= TB) return string.Format("{0:0.0} TB", (bytes / TB));
            else if (bytes >= GB) return string.Format("{0:0.0} GB", (bytes / GB));
            else if (bytes >= MB) return string.Format("{0:0.0} MB", (bytes / MB));
            else if (bytes >= 0) return string.Format("{0:0.0} kB", (bytes / KB));
            else return "error";
        }

        private string ConvertTime(long sec)
        {
            string str = " ";
            long min, hour;
            
            min = sec / 60;
            sec = sec - (min * 60);
            hour = min / 60;
            min = min - (hour * 60);

            if (hour == 0 && min == 0) str = string.Format("{0}s", sec);
            else if (min >= 1 && hour == 0) str = string.Format("{0}m {1}s", min, sec);
            else if (hour >= 1 && min == 0) str = string.Format("{0}h", hour);
            else str = string.Format("{0}h {1}m", hour, min);

            return str;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e) //Otevřít
        {
            if (DataView.SelectedIndex == -1) return;
            MyData item = DataView.SelectedItem as MyData;
            string path = Path.Combine(item.Client.Directory, item.Client.FileName);
            if (File.Exists(path)) Process.Start(path);            
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e) //Otevřít ve složce
        {
            if (DataView.SelectedIndex == -1) return;
            MyData item = DataView.SelectedItem as MyData;

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "explorer.exe";
            string path = Path.Combine(item.Client.Directory, item.Client.FileName);
            if (File.Exists(path)) startInfo.Arguments = "/select, " + path;
            else startInfo.Arguments = item.Client.Directory;
            process.StartInfo = startInfo;
            process.Start();
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e) //Přejmenovat
        {
            if (DataView.SelectedIndex == -1) return;

            MyData item = DataView.SelectedItem as MyData;

            RenameWindow renameWindow = new RenameWindow();
            renameWindow.FileName = item.Name;
            renameWindow.Owner = this;
            if (renameWindow.ShowDialog() == true)
            {
                item.Client.Rename(renameWindow.FileName);
                item.Name = renameWindow.FileName;
                DataView.Items.Refresh();
            }
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e) //Omezit rychlost
        {
            if (DataView.SelectedIndex == -1) return;

            MyData item = DataView.SelectedItem as MyData;

            BandwidthWindow bandwidthWindow = new BandwidthWindow();
            bandwidthWindow.SpeedLimit = item.Client.SpeedLimit; //vyjímka
            bandwidthWindow.Owner = this;
            if (bandwidthWindow.ShowDialog() == true)
            {
                item.Client.SpeedLimit = bandwidthWindow.SpeedLimit;
                item.Speed = string.Format("{0} ({1}) KB/s ", 0, item.Client.SpeedLimit);
                DataView.Items.Refresh();
            }
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e) //Info
        {
            if (DataView.SelectedIndex == -1) return;
            MyData item = DataView.SelectedItem as MyData;
            MessageBox.Show(item.Name + "\r" + 
                            item.Client.State.ToString() + "\r" +
                            item.Client.Url);
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            LinksStorage links = new LinksStorage();

            foreach (MyData item in DataView.Items)
            {
                Link link = new Link();
                link.Directory = item.Client.Directory;
                link.FileName = item.Client.FileName;
                link.Url = item.Client.Url;
                link.TotalBytes = item.Client.TotalBytes;
                link.SpeedLimit = item.Client.SpeedLimit;
                links.List.Add(link);
            }
            links.Save();
            
            System.Environment.Exit(1);
        }

        private void DataView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            MyData item = DataView.SelectedItem as MyData;

            if (item != null)
            {
                if (item.Client.State == DCStates.Completed)
                {
                    buttonPause.IsEnabled = false;
                    buttonResume.IsEnabled = false;
                    buttonCancel.IsEnabled = false;
                }
                else
                {
                    buttonPause.IsEnabled = true;
                    buttonResume.IsEnabled = true;
                    buttonCancel.IsEnabled = true;
                }
            }           
        }

        protected void HandleDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //var track = ((ListViewItem)sender).Content as Track; //Casting back to the binded Track
            MessageBox.Show("kek");
        }

        public static void SelectCulture(string culture)
        {
            if (String.IsNullOrEmpty(culture)) return;

            var dictionaryList = Application.Current.Resources.MergedDictionaries.ToList();

            string requestedCulture = string.Format("StringResources.{0}.xaml", culture);
            var resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == requestedCulture);

            if (resourceDictionary == null)
            {
                requestedCulture = "StringResources.en-US.xaml";
                resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString == requestedCulture);
            }
            else
            {
                Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }

            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
        }

    }

    public class MyData
    {
        public ImageSource Icon { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public double Progress { get; set; }
        public string Speed { get; set; }
        public string Remaining { get; set; }
        public DownloadClient Client { get; set; }
    }

}