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

        System.Windows.Forms.NotifyIcon trayIcon;

        public MainWindow()
        {
            InitializeComponent();
            Title = App.appName;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        { 
            trayIcon = new System.Windows.Forms.NotifyIcon();
            trayIcon.Text = App.appName;
            trayIcon.Icon = Properties.Resources.favicon;
            trayIcon.BalloonTipClosed += TrayIcon_BalloonTipClosed;
            trayIcon.BalloonTipClicked += TrayIcon_BalloonTipClicked;

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
                item.Refresh();
                item.LoadIcon();
                listView.Items.Add(item);
            }
        }

        private void TrayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            this.Focus();
            trayIcon.Visible = false;
        }

        private void TrayIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
        }

        private void Client_OnDownloadStateChanged(DownloadClient client, LvData item, States oldState, States newState)
        {
            if (newState != States.Error)
            {
                item.Refresh();
                listView.Items.Refresh();
                if (oldState == States.Error) item.ErrorMsg = null;
            }            
        }

        private void Client_OnDownloadInit(DownloadClient client, LvData item)
        {
            item.LoadIcon();       
        }

        private void Client_OnDownloadError(DownloadClient client, LvData item, string message)
        {            
            item.ErrorMsg = message;
            item.Refresh();
            listView.Items.Refresh();

            CheckQueue();
            

            if (settings.ShowNotification)
            {
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(10, Lang.Translate("lang_error"), message, System.Windows.Forms.ToolTipIcon.Error);
            }                
            if (settings.PlaySound) System.Media.SystemSounds.Hand.Play();
        }

        private void Client_OnDownloadCompleted(DownloadClient client, LvData item)
        {
            item.LoadIcon();

            CheckQueue();

            if (settings.ShowNotification)
            {
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(10, Lang.Translate("lang_download_completed"), client.FileName, System.Windows.Forms.ToolTipIcon.Info);
            }
            if (settings.PlaySound) System.Media.SystemSounds.Asterisk.Play();
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
                    item.Client.SpeedLimit = settings.SpeedLimit;
                    item.Client.OnDownloadInit += Client_OnDownloadInit;
                    item.Client.OnDownloadCompleted += Client_OnDownloadCompleted;
                    item.Client.OnDownloadError += Client_OnDownloadError;
                    item.Client.OnDownloadStateChanged += Client_OnDownloadStateChanged;

                    if (DownloadClient.ActiveCount < settings.MaxDownloads) item.Client.Start();
                    else item.Client.Queue();
                    item.Refresh();
                    item.LoadIcon();
                    listView.Items.Add(item);
                }

                //přidáno zpoždění protože z nějakého důvodu ScrollIntoView nefunguje hned po přidání položky
                Task.Delay(20).ContinueWith(t => { //počká 20ms bez blokování současného vlákna
                    Dispatcher.Invoke(() => { 
                        //po 20ms se kód spustí zpět na vlákně rozhraní
                        listView.Focus();
                        listView.SelectedIndex = listView.Items.Count - 1;
                        listView.ScrollIntoView(listView.SelectedItem);                        
                    }); 
                });
            }              
        }

        private void buttonResume_Click(object sender, RoutedEventArgs e)
        {
            //seřadí položky podle pořadí v listView, protože jsou defaultně v pořadí, v jakém byly vybrány
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

            CheckQueue();
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

            CheckQueue();
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
                CheckQueue();
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

            CheckQueue();
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

            CheckQueue();
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
                foreach (LvData item in listView.Items) item.Refresh();
                listView.Items.Refresh();
            }
        }

        /// <summary>
        /// Zkontroluje stav položek a zahájí stahovaní nebo je přidá do fronty.
        /// </summary>
        private void CheckQueue()
        {  
            int count = 0;

            foreach (LvData item in listView.Items)
            {
                if (count < settings.MaxDownloads)
                {
                    if (item.Client.State == States.Downloading || item.Client.State == States.Starting)                    
                        count++;

                    else if (item.Client.State == States.Queue)
                    {
                        item.Client.Start();
                        count++;
                    }
                }
                else if (item.Client.State == States.Downloading || item.Client.State == States.Starting)
                    item.Client.Queue();
            }
        }

        private void Timer_Tick(object sender, EventArgs e) //1 sekunda
        {  
            long totalSpeed = 0;

            if (DownloadClient.ActiveCount > 0)
            {
                foreach (LvData item in listView.Items)
                {
                    item.Refresh();
                    totalSpeed += item.Client.BytesPerSecond;
                }
                listView.Items.Refresh();
            }

            Title = App.appName + " - " + 
                DownloadClient.ActiveCount + "/" + 
                listView.Items.Count + " - " + 
                Util.ConvertBytes(totalSpeed) + "/s";
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

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e) //Otevřít
        {
            LvData item = lastItem;
            try
            {
                if (File.Exists(item.Client.FullPath)) Process.Start(item.Client.FullPath);
            }
            catch (Win32Exception) { }
        }

        private void MenuItemFolder_Click(object sender, RoutedEventArgs e) //Otevřít ve složce
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

        private void MenuItemRename_Click(object sender, RoutedEventArgs e) //Přejmenovat
        {
            LvData item = lastItem;

            if (item.Client.FileName == "") return;

            RenameWindow renameWindow = new RenameWindow();
            renameWindow.Owner = this;
            renameWindow.item = item;

            if (renameWindow.ShowDialog() == true)
            {
                item.Refresh();
                listView.Items.Refresh();
            }
        }

        private void MenuItemLimit_Click(object sender, RoutedEventArgs e) //Omezit rychlost
        {
            LvData item = lastItem;

            BandwidthWindow bandwidthWindow = new BandwidthWindow();
            bandwidthWindow.textBox.Text = (item.Client.SpeedLimit / 1024).ToString();
            bandwidthWindow.Owner = this;
            if (bandwidthWindow.ShowDialog() == true)
            {
                item.Client.SpeedLimit = Int64.Parse(bandwidthWindow.textBox.Text) * 1024;
                item.Refresh();
                listView.Items.Refresh();
            }
        }

        private void MenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            LvData item = lastItem;

            Clipboard.SetText(item.Client.Url);
        }     

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DownloadClient.ActiveCount > 0)
            {
                if (MessageBox.Show(
                    Lang.Translate("lang_active_download"),
                    Lang.Translate("lang_confirm"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }                
            }

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
}