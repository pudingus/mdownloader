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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Threading.Tasks;

namespace downloader3
{
    /// <summary>
    /// Interakční logika pro MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        public SettingsStorage settings = new SettingsStorage();
        private DispatcherTimer timer = new DispatcherTimer();
        private LvData lastItem;
        private string baseTitle = "";

        System.Windows.Forms.NotifyIcon trayIcon;

        public MainWindow()
        {
            InitializeComponent();
            baseTitle = Title;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var trayMenu = new System.Windows.Forms.ContextMenu();

            var itemOpen = new System.Windows.Forms.MenuItem("Open", OnTrayOpen);
            itemOpen.DefaultItem = true;

            trayMenu.MenuItems.Add(itemOpen);
            trayMenu.MenuItems.Add("Exit", OnTrayExit);

            trayIcon = new System.Windows.Forms.NotifyIcon();
            trayIcon.Text = "downloader3";
            trayIcon.Icon = Properties.Resources.favicon;
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            timer.Tick += new EventHandler(Timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 1); //1 sekunda
            timer.Start();

            settings = settings.Load();
            settings.Save();

            Lang.SetLanguage(settings.Language);

            LinksStorage links = new LinksStorage();
            links = links.Load();

            foreach (Link link in links.List)
            {
                string path = Path.Combine(link.Directory, link.FileName);
                LvData item = new LvData();

                item.Client = new DownloadClient(link.Url, link.Directory, item, link.TotalBytes, link.FileName);
                item.Client.OnDownloadInit += Client_OnDownloadInit;
                item.Client.OnDownloadCompleted += Client_OnDownloadCompleted;
                item.Client.OnDownloadError += Client_OnDownloadError;
                item.Client.OnDownloadStateChanged += Client_OnDownloadStateChanged;
                item.Client.SpeedLimit = link.SpeedLimit;
                Refresh(item);

                Icon sysicon;

                if (File.Exists(path)) sysicon = ShellIcon.GetSmallIcon(path);                
                else
                {
                    string ext = Path.GetExtension(item.Name);
                    if (ext == "") ext = item.Name;
                    sysicon = ShellIcon.GetSmallIconFromExtension(ext);
                }

                var bmpSrc = Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                sysicon.Dispose();
                item.Icon = bmpSrc;

                listView.Items.Add(item);
            }
        }

        private void Client_OnDownloadStateChanged(DownloadClient client, LvData item, States oldState, States newState)
        {
            if (newState != States.Error)
            {
                Refresh(item);
                listView.Items.Refresh();
                if (oldState == States.Error) item.ErrorMsg = null;
            }            
        }

        private void Client_OnDownloadInit(DownloadClient client, LvData item)
        {
            Icon sysicon = ShellIcon.GetSmallIcon(client.FullPath);
            var bmpSrc = Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            sysicon.Dispose();
            item.Icon = bmpSrc;            
        }

        private void Client_OnDownloadError(DownloadClient client, LvData item, string message)
        {            
            item.ErrorMsg = message;
            Refresh(item);
            listView.Items.Refresh();

            bool found = false;
            int j = 0;
            while (!found && j < listView.Items.Count)
            {
                LvData it = listView.Items[j] as LvData;
                if (it.Client.State == States.Queue)
                {
                    it.Client.Start();
                    found = true;
                }
                j++;
            }

            if (settings.ShowNotification) trayIcon.ShowBalloonTip(10, Lang.Translate("lang_error"), message, System.Windows.Forms.ToolTipIcon.Error);
            if (settings.PlaySound) System.Media.SystemSounds.Hand.Play();
        }

        private void Client_OnDownloadCompleted(DownloadClient client, LvData item)
        {
            Icon sysicon = ShellIcon.GetSmallIcon(client.FullPath);
            var bmpSrc = Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            sysicon.Dispose();
            item.Icon = bmpSrc;                       

            bool found = false;
            int j = 0;
            while (!found && j < listView.Items.Count)
            {
                LvData it = listView.Items[j] as LvData;
                if (it.Client.State == States.Queue)
                {
                    it.Client.Start();
                    found = true;
                }
                j++;
            }

            if (settings.ShowNotification) trayIcon.ShowBalloonTip(10, Lang.Translate("lang_download_completed"), client.FileName, System.Windows.Forms.ToolTipIcon.Info);
            if (settings.PlaySound) System.Media.SystemSounds.Asterisk.Play();
        }


        private void OnTrayOpen(object sender, EventArgs e)
        {
            this.Focus();
        }

        private void OnTrayExit(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            AddWindow linksWindows = new AddWindow();
            linksWindows.Owner = this;
            
            if (linksWindows.ShowDialog() == true)
            {
                string dir = linksWindows.pathTextBox.Text;
                foreach (string url in linksWindows.urlList)
                {
                    LvData item = new LvData();
                    item.Client = new DownloadClient(url, dir, item);
                    item.Client.OnDownloadInit += Client_OnDownloadInit;
                    item.Client.OnDownloadCompleted += Client_OnDownloadCompleted;
                    item.Client.OnDownloadError += Client_OnDownloadError;
                    item.Client.OnDownloadStateChanged += Client_OnDownloadStateChanged;
                    item.Client.SpeedLimit = settings.SpeedLimit;

                    if (DownloadClient.ActiveCount < settings.MaxDownloads) item.Client.Start();
                    else item.Client.Queue();

                    Refresh(item);

                    string ext = Path.GetExtension(item.Name);
                    if (ext == "") ext = item.Name;
                    var sysicon = ShellIcon.GetSmallIconFromExtension(ext);


                    var bmpSrc = Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    sysicon.Dispose();
                    item.Icon = bmpSrc;

                    listView.Items.Add(item);
                }

                //přidáno zpoždění protože z nějakého důvodu ScrollIntoView nefunguje hned po přidání položky
                Task.Delay(20).ContinueWith(t => {
                    Dispatcher.Invoke(() => {
                        //tento kód se za 20ms spustí asynchronně na jiném vlákně
                        listView.Focus();
                        listView.SelectedIndex = listView.Items.Count - 1;
                        listView.ScrollIntoView(listView.SelectedItem);                        
                    }); 
                });
            }              
        }

        private void buttonResume_Click(object sender, RoutedEventArgs e)
        {
            List<LvData> selectedList = new List<LvData>();
            foreach (LvData item in listView.Items) if (listView.SelectedItems.Contains(item)) selectedList.Add(item);

            foreach (LvData item in selectedList)
            {
                if (item.Client.State == States.Paused || 
                    item.Client.State == States.Error)
                {
                    if (DownloadClient.ActiveCount < settings.MaxDownloads) item.Client.Start();                    
                    else item.Client.Queue();
                }
            }
        }

        private void buttonPause_Click(object sender, RoutedEventArgs e)
        {  
            foreach (LvData item in listView.SelectedItems)
            {
                if (item.Client.State == States.Downloading || 
                    item.Client.State == States.Starting ||
                    item.Client.State == States.Queue)
                {
                    item.Client.Pause();                    
                }
            }
        }        

        private void buttonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;
            
            RemoveWindow removeWindow = new RemoveWindow();
            removeWindow.Owner = this;
            if (removeWindow.ShowDialog() == true)
            {
                List<LvData> selectedList = new List<LvData>();
                foreach (LvData item in listView.SelectedItems) selectedList.Add(item);

                foreach (LvData item in selectedList)
                {
                    if (removeWindow.deleteFile) item.Client.Cancel();
                    listView.Items.Remove(item);
                }
            }
        }

        private void buttonUp_Click(object sender, RoutedEventArgs e)
        {         
            int index = listView.SelectedIndex;

            if (index > 0)
            {
                object item = listView.SelectedItem;
                listView.Items.RemoveAt(index);
                listView.Items.Insert(index - 1, item);
                listView.SelectedItems.Add(item);
            }
        }

        private void buttonDown_Click(object sender, RoutedEventArgs e)
        {
            int index = listView.SelectedIndex;

            if (index < listView.Items.Count - 1 && index != -1)
            {
                object item = listView.SelectedItem;
                listView.Items.RemoveAt(index);
                listView.Items.Insert(index + 1, item);
                listView.SelectedItems.Add(item);
            }
        }

        private void buttonSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.speedLimit.Text = (settings.SpeedLimit / 1024).ToString();
            settingsWindow.maxDownloads.Text = settings.MaxDownloads.ToString();
            settingsWindow.checkboxNotify.IsChecked = settings.ShowNotification;
            settingsWindow.checkboxSound.IsChecked = settings.PlaySound;
            settingsWindow.Lang = settings.Language;
            if (settingsWindow.ShowDialog() == true) //uložit nastavení
            {
                settings.SpeedLimit = Int64.Parse(settingsWindow.speedLimit.Text) * 1024;
                settings.Language = settingsWindow.Lang;
                settings.MaxDownloads = Int32.Parse(settingsWindow.maxDownloads.Text);
                settings.ShowNotification = settingsWindow.checkboxNotify.IsChecked.GetValueOrDefault();
                settings.PlaySound = settingsWindow.checkboxSound.IsChecked.GetValueOrDefault();
                settings.Save();
                CheckQueue();
                Lang.SetLanguage(settings.Language);
                foreach (LvData item in listView.Items) Refresh(item);
                listView.Items.Refresh();
            }
        }

        private void CheckQueue()
        {
            int i = listView.Items.Count - 1;
            while (DownloadClient.ActiveCount > settings.MaxDownloads && i >= 0)
            {
                LvData it = listView.Items[i] as LvData;
                if (it.Client.State == States.Downloading || it.Client.State == States.Starting)
                    it.Client.Queue();
                i--;
            }

            i = 0;
            while (DownloadClient.ActiveCount < settings.MaxDownloads && i < listView.Items.Count)
            {
                LvData it = listView.Items[i] as LvData;
                if (it.Client.State == States.Queue)
                    it.Client.Start();
                i++;
            }
        }

        private void Timer_Tick(object sender, EventArgs e) //1 sekunda
        {  
            long totalSpeed = 0;

            if (DownloadClient.ActiveCount > 0)
            {
                foreach (LvData item in listView.Items)
                {
                    Refresh(item);
                    totalSpeed += item.Client.BytesPerSecond;
                }
                listView.Items.Refresh();
            }

            Title = baseTitle + " - " + DownloadClient.ActiveCount + "/" + listView.Items.Count + " - " + Util.ConvertBytes(totalSpeed) + "/s";

        }

        public void Refresh(LvData item)
        {           
            if (item.Client.FileName == "") item.Name = item.Client.Url;            
            else item.Name = item.Client.FileName;
            if (item.Client.TotalBytes > 0) item.Progress = (double)item.Client.BytesDownloaded / item.Client.TotalBytes * 100;
            item.Size = string.Format("{0}/{1}", Util.ConvertBytes(item.Client.BytesDownloaded), Util.ConvertBytes(item.Client.TotalBytes));
            if (item.Client.State == States.Paused) item.Remaining = Lang.Translate("lang_paused");            
            else if (item.Client.State == States.Queue) item.Remaining = Lang.Translate("lang_inqueue");
            else if (item.Client.State == States.Canceled) item.Remaining = Lang.Translate("lang_canceled");
            else if (item.Client.State == States.Completed) item.Remaining = Lang.Translate("lang_completed");
            else if (item.Client.State == States.Downloading)
            {
                long sec = 0;
                if (item.Client.BytesDownloaded > 0 && item.Client.BytesPerSecond > 0) sec = (item.Client.TotalBytes - item.Client.BytesDownloaded) * 1 / item.Client.BytesPerSecond;
                item.Remaining = Util.ConvertTime(sec);
            }
            else if (item.Client.State == States.Error) item.Remaining = Lang.Translate("lang_error") + ": " + item.ErrorMsg;
            else if (item.Client.State == States.Starting) item.Remaining = Lang.Translate("lang_starting");            

            if(item.Client.SpeedLimit > 0)            
                item.Speed = string.Format("{0}/s [{1}/s]", Util.ConvertBytes(item.Client.BytesPerSecond), Util.ConvertBytes(item.Client.SpeedLimit));
            else item.Speed = string.Format("{0}/s", Util.ConvertBytes(item.Client.BytesPerSecond));

            item.Directory = item.Client.Directory;
        }        


        private void Item_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            LvData item = (sender as ListViewItem).DataContext as LvData;
            try
            {
                if (File.Exists(item.Client.FullPath)) Process.Start(item.Client.FullPath);
            }
            catch (Win32Exception) { } //uživatel stisknul "ne" při pokusu získat admin oprávnění
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e) //Otevřít
        {
            LvData item = lastItem;
            try
            {
                if (File.Exists(item.Client.FullPath)) Process.Start(item.Client.FullPath);
            }
            catch (Win32Exception) { }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e) //Otevřít ve složce
        {
            LvData item = lastItem;
            
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
            LvData item = lastItem;

            if (item.Client.FileName == "") return;

            RenameWindow renameWindow = new RenameWindow();
            renameWindow.Owner = this;
            renameWindow.item = item;

            if (renameWindow.ShowDialog() == true)
            {
                Refresh(item);
                listView.Items.Refresh();
            }
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e) //Omezit rychlost
        {
            LvData item = lastItem;

            BandwidthWindow bandwidthWindow = new BandwidthWindow();
            bandwidthWindow.textBox.Text = (item.Client.SpeedLimit / 1024).ToString();
            bandwidthWindow.Owner = this;
            if (bandwidthWindow.ShowDialog() == true)
            {
                item.Client.SpeedLimit = Int64.Parse(bandwidthWindow.textBox.Text) * 1024;
                Refresh(item);
                listView.Items.Refresh();
            }
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e) //Info
        {
            LvData item = lastItem;

            MessageBox.Show(item.Name + "\r" + 
                            item.Client.State.ToString() + "\r" +
                            item.Client.Url);
        }        

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            LinksStorage links = new LinksStorage();

            foreach (LvData item in listView.Items)
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
            trayIcon.Dispose();            
            System.Environment.Exit(1);
        }       

        private void ListViewItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ListViewItem cItem = sender as ListViewItem;
            LvData item = cItem.DataContext as LvData;
            lastItem = item;

            if (cItem != null && item != null)
            {
                //zakáže nebo povolí položku "Otevřít" podle toho, jestli soubor existuje
                MenuItem menuItem = cItem.ContextMenu.Items[0] as MenuItem;
                if (File.Exists(item.Client.FullPath)) menuItem.IsEnabled = true;
                else menuItem.IsEnabled = false;

                //zakáže položku "Přejmenovat", pokud finální jméno souboru ještě nebylo získáno
                menuItem = cItem.ContextMenu.Items[2] as MenuItem;
                if (item.Client.FileName == "") menuItem.IsEnabled = false;
                else menuItem.IsEnabled = true;
            }
        }
    }

    public class LvData
    {
        public ImageSource Icon { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public double Progress { get; set; }
        public string Speed { get; set; }
        public string Remaining { get; set; }
        public string Directory { get; set; }
        public DownloadClient Client { get; set; }
        public string ErrorMsg { get; set; }
    }
}