using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;

namespace downloader3
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private double speed;
        private int index;
        private DownloadClient client;
        private const string linksFile = "links.xml";

        public MainWindow()
        {
            InitializeComponent();
            index = 0;

            if (File.Exists(linksFile))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(linksFile);

                XmlNodeList elemList = doc.GetElementsByTagName("link");
                for (int i = 0; i < elemList.Count; i++)
                {
                    MyData item = new MyData();
                    item.FilePath = elemList[i].Attributes["filepath"].Value;
                    item.Name = System.IO.Path.GetFileName(item.FilePath);
                    item.Url = elemList[i].Attributes["url"].Value;
                    item.Progress = XmlConvert.ToDouble(elemList[i].Attributes["progress"].Value);
                    item.Size = elemList[i].Attributes["size"].Value; //vyjímka

                    if (item.Progress >= 100) item.Completed = true;
                    else
                    {
                        if (File.Exists(item.FilePath))
                        {
                            item.Completed = false;

                            item.Client = new DownloadClient(item.Url, item.FilePath);

                            FileInfo f = new FileInfo(item.FilePath);
                            long s1 = f.Length;
                            item.Client.AddBytes(s1);

                            item.Client.OnDownloadProgressChanged += Client_OnDownloadProgressChanged;
                            item.Client.OnDownloadFinished += Client_OnDownloadFinished;
                            item.Client.OnDownloadFileInit += Client_OnDownloadFileInit;
                            item.Client.Index = index;
                            item.Client.SpeedLimit = Properties.Settings.Default.speedLimit;
                            item.Client.Start();
                        }
                    }

                    if (File.Exists(item.FilePath))
                    {
                        var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(item.FilePath);
                        var bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                        sysicon.Dispose();
                        item.Icon = bmpSrc;
                    }

                    DataView.Items.Add(item);
                    index++;
                }
            }
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            LinksWindow linksWindows = new LinksWindow();
            linksWindows.Owner = this;
            if (linksWindows.ShowDialog() == true)
            {
                client = new DownloadClient(linksWindows.url, linksWindows.filePath);
                client.OnDownloadProgressChanged += Client_OnDownloadProgressChanged;
                client.OnDownloadFinished += Client_OnDownloadFinished;
                client.OnDownloadFileInit += Client_OnDownloadFileInit;
                client.Index = index;
                client.SpeedLimit = Properties.Settings.Default.speedLimit;
                client.Start();
                MyData item = new MyData();
                item.Name = System.IO.Path.GetFileName(linksWindows.filePath);
                item.Progress = 0;
                item.Client = client;
                item.FilePath = linksWindows.filePath;
                item.Url = linksWindows.url;

                DataView.Items.Add(item);
                index++;
            }
        }

        private void buttonPause_Click(object sender, RoutedEventArgs e)
        {
            MyData item = DataView.SelectedItem as MyData;
            item.Client.Pause();
            item.Remaining = Translate("lang_paused");
            item.Speed = "0 KB/s";
            DataView.Items.Refresh();
        }

        private void buttonResume_Click(object sender, RoutedEventArgs e)
        {
            MyData item = DataView.SelectedItem as MyData;
            item.Client.Resume();
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

        private void buttonUp_Click(object sender, RoutedEventArgs e)
        {
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
            settingsWindow.speedLimit.Text = Properties.Settings.Default.speedLimit.ToString();
            settingsWindow.maxDownloads.Text = Properties.Settings.Default.maxDownloads.ToString();
            if (Properties.Settings.Default.language == "cs-CZ") settingsWindow.langSelection.SelectedIndex = 0;
            if (Properties.Settings.Default.language == "en-US") settingsWindow.langSelection.SelectedIndex = 1;
            if (settingsWindow.ShowDialog() == true) //save settings
            {
                Properties.Settings.Default.speedLimit = Int32.Parse(settingsWindow.speedLimit.Text);
                Properties.Settings.Default.language = settingsWindow.language;
                Properties.Settings.Default.maxDownloads = Int32.Parse(settingsWindow.maxDownloads.Text);
                App.SelectCulture(Properties.Settings.Default.language);
                Properties.Settings.Default.Save();

                RefreshLanguage();
            }
        }

        private void Client_OnDownloadProgressChanged(object sender)
        {
            DownloadClient c = sender as DownloadClient;

            MyData item = DataView.Items[c.Index] as MyData;
            item.Size = string.Format("{0}/{1}", ConvertFileSize(c.BytesDownloaded), ConvertFileSize(c.FileSize));
            item.Progress = c.Percentage;
            speed = c.BytesPerSecond / 1024;
            item.Speed = string.Format("{0} ({1}) KB/s ", speed.ToString("0.0"), item.Client.SpeedLimit);
            item.Remaining = ConvertTime(c.SecondsRemaining);

            DataView.Items.Refresh();
        }

        private void Client_OnDownloadFinished(object sender, bool canceled)
        {
            Dispatcher.Invoke(delegate
            {
                DownloadClient c = sender as DownloadClient;
                MyData item = DataView.Items[c.Index] as MyData;
                if (canceled)
                {
                    item.Size = "";
                    item.Progress = 0;
                    item.Speed = "";
                    item.Remaining = Translate("lang_canceled");
                    item.Completed = false;
                }
                else
                {
                    item.Progress = 100;
                    item.Remaining = Translate("lang_completed");
                    item.Completed = true;
                }

                DataView.Items.Refresh();
            });
        }

        private void Client_OnDownloadFileInit(object sender)
        {
            Dispatcher.Invoke(delegate
            {
                DownloadClient c = sender as DownloadClient;

                MyData item = DataView.Items[c.Index] as MyData;

                var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(item.FilePath);
                var bmpSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                sysicon.Dispose();
                item.Icon = bmpSrc;

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
            else str = string.Format("{0}h {1}m", hour, min);

            return str;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataView.SelectedIndex == -1) return;

            MyData item = DataView.SelectedItem as MyData;
            Process.Start(item.FilePath);
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (DataView.SelectedIndex == -1) return;

            MyData item = DataView.SelectedItem as MyData;

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "explorer.exe";
            startInfo.Arguments = "/select, " + item.FilePath;
            process.StartInfo = startInfo;
            process.Start();
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e) //Přejmenovat
        {
            if (DataView.SelectedIndex == -1) return;

            MyData item = DataView.SelectedItem as MyData;

            RenameWindow renameWindow = new RenameWindow();
            renameWindow.FileName = item.Name;
            if (renameWindow.ShowDialog() == true)
            {
                item.Client.Rename(renameWindow.FileName);
                item.Name = renameWindow.FileName;
                item.FilePath = item.Client.FilePath;
            }
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e) //Omezit rychlost
        {
            if (DataView.SelectedIndex == -1) return;

            MyData item = DataView.SelectedItem as MyData;

            BandwidthWindow bandwidthWindow = new BandwidthWindow();
            bandwidthWindow.SpeedLimit = item.Client.SpeedLimit; //vyjímka
            if (bandwidthWindow.ShowDialog() == true)
            {
                item.Client.SpeedLimit = bandwidthWindow.SpeedLimit;
            }
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e) //Info
        {
            if (DataView.SelectedIndex == -1) return;

            MyData item = DataView.SelectedItem as MyData;

            MessageBox.Show(item.Name + "\r" + item.Completed.ToString() + "\r" + item.Url);
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;
            XmlWriter xmlWriter = XmlWriter.Create(linksFile, xmlWriterSettings);

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("links");

            foreach (MyData item in DataView.Items)
            {
                xmlWriter.WriteStartElement("link");
                xmlWriter.WriteAttributeString("filepath", item.FilePath);
                xmlWriter.WriteAttributeString("url", item.Url);
                xmlWriter.WriteAttributeString("progress", XmlConvert.ToString(item.Progress));
                xmlWriter.WriteAttributeString("size", item.Size);
                xmlWriter.WriteEndElement();
            }

            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        private void DataView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            MyData item = DataView.SelectedItem as MyData;

            if (item.Completed)
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

    internal class MyData
    {
        public ImageSource Icon { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public double Progress { get; set; }
        public string Speed { get; set; }
        public string Remaining { get; set; }
        public DownloadClient Client { get; set; }
        public string FilePath { get; set; }
        public string Url { get; set; }
        public bool Completed { get; set; }
        public int Priority { get; set; }
    }
}