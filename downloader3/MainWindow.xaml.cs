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
    /// Představuje hlavní okno programu
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Představuje instanci <see cref="SettingsStorage"/> se současným nastavením programu
        /// </summary>
        public SettingsStorage settings = new SettingsStorage();
        private DispatcherTimer timer = new DispatcherTimer();
        private Downloader lastItem;

        System.Windows.Forms.NotifyIcon trayIcon;

        /// <summary>
        /// Vytvoří novou instanci třídy <see cref="MainWindow"/>
        /// </summary>
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
                Downloader item = new Downloader(link.Url, link.Directory, link.TotalBytes, link.FileName);
                item.OnDownloadInit += Client_OnDownloadInit;
                item.OnDownloadCompleted += Client_OnDownloadCompleted;
                item.OnDownloadError += Client_OnDownloadError;
                item.OnDownloadStateChanged += Client_OnDownloadStateChanged;
                item.SpeedLimit = link.SpeedLimit;
                item.Refresh();
                item.LoadIcon();
                listView.Items.Add(item);
            }
        }

        private void TrayIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            Activate();
        }

        private void TrayIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
        }

        private void Client_OnDownloadStateChanged(Downloader item, States oldState, States newState)
        {
            if (newState != States.Error)
            {
                item.Refresh();
                listView.Items.Refresh();
                if (oldState == States.Error) item.ErrorMsg = null;
            }
        }

        private void Client_OnDownloadInit(Downloader item)
        {
            item.LoadIcon();
        }

        private void Client_OnDownloadError(Downloader item, string message)
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

        private void Client_OnDownloadCompleted(Downloader item)
        {
            item.LoadIcon();

            CheckQueue();

            if (settings.ShowNotification)
            {
                trayIcon.Visible = true;
                trayIcon.ShowBalloonTip(10, Lang.Translate("lang_download_completed"), item.FileName, System.Windows.Forms.ToolTipIcon.Info);
            }
            if (settings.PlaySound) System.Media.SystemSounds.Asterisk.Play();
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            AddWindow addWindow = new AddWindow();
            addWindow.Owner = this;
            if (addWindow.ShowDialog() == true)
            {
                string dir = addWindow.pathTextBox.Text;
                foreach (string url in addWindow.UrlList)
                {
                    Downloader item = new Downloader(url, dir);
                    item.SpeedLimit = settings.SpeedLimit;
                    item.OnDownloadInit += Client_OnDownloadInit;
                    item.OnDownloadCompleted += Client_OnDownloadCompleted;
                    item.OnDownloadError += Client_OnDownloadError;
                    item.OnDownloadStateChanged += Client_OnDownloadStateChanged;

                    if (Downloader.ActiveCount < settings.MaxDownloads) item.Start();
                    else item.Queue();
                    item.Refresh();
                    item.LoadIcon();
                    listView.Items.Add(item);
                }

                //přidáno zpoždění protože z nějakého důvodu ScrollIntoView nefunguje hned po přidání položky
                Task.Delay(20).ContinueWith(t =>
                { //počká 20ms bez blokování současného vlákna
                    Dispatcher.Invoke(() =>
                    {
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
            List<Downloader> selectedList = new List<Downloader>();
            foreach (Downloader item in listView.Items) if (listView.SelectedItems.Contains(item)) selectedList.Add(item);

            foreach (Downloader item in selectedList)
            {
                if (item.State == States.Paused ||
                    item.State == States.Error)
                {
                    if (Downloader.ActiveCount < settings.MaxDownloads) item.Start();
                    else item.Queue();
                }
            }

            CheckQueue();
        }

        private void buttonPause_Click(object sender, RoutedEventArgs e)
        {
            foreach (Downloader item in listView.SelectedItems)
            {
                if (item.State == States.Downloading ||
                    item.State == States.Starting ||
                    item.State == States.Queue)
                {
                    item.Pause();
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
                List<Downloader> selectedList = new List<Downloader>();
                foreach (Downloader item in listView.SelectedItems) selectedList.Add(item);

                foreach (Downloader item in selectedList)
                {
                    if (removeWindow.DeleteFiles) item.Cancel();
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
                foreach (Downloader item in listView.Items) item.Refresh();
                listView.Items.Refresh();
            }
        }

        /// <summary>
        /// Zkontroluje stav položek a zahájí stahovaní nebo je přidá do fronty.
        /// </summary>
        private void CheckQueue()
        {
            int count = 0;

            foreach (Downloader item in listView.Items)
            {
                if (count < settings.MaxDownloads)
                {
                    if (item.State == States.Downloading || item.State == States.Starting)
                        count++;

                    else if (item.State == States.Queue)
                    {
                        item.Start();
                        count++;
                    }
                }
                else if (item.State == States.Downloading || item.State == States.Starting)
                    item.Queue();
            }
        }

        private void Timer_Tick(object sender, EventArgs e) //1 sekunda
        {
            long totalSpeed = 0;

            if (Downloader.ActiveCount > 0)
            {
                foreach (Downloader item in listView.Items)
                {
                    item.Refresh();
                    totalSpeed += item.BytesPerSecond;
                }
                listView.Items.Refresh();
            }

            Title = App.appName + " - " +
                Downloader.ActiveCount + "/" +
                listView.Items.Count + " - " +
                Util.ConvertBytes(totalSpeed) + "/s";
        }

        private void Item_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Downloader item = (sender as ListViewItem).DataContext as Downloader;
            try
            {
                if (File.Exists(item.FullPath)) Process.Start(item.FullPath);
            }
            catch (Win32Exception) { } //uživatel stisknul "ne" při pokusu získat admin oprávnění
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e) //Otevřít
        {
            Downloader item = lastItem;
            try
            {
                if (File.Exists(item.FullPath)) Process.Start(item.FullPath);
            }
            catch (Win32Exception) { }
        }

        private void MenuItemFolder_Click(object sender, RoutedEventArgs e) //Otevřít ve složce
        {
            Downloader item = lastItem;

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "explorer.exe";
            string path = item.FullPath;
            if (File.Exists(path)) startInfo.Arguments = "/select, " + path;
            else startInfo.Arguments = item.Directory;
            process.StartInfo = startInfo;
            process.Start();
        }

        private void MenuItemRename_Click(object sender, RoutedEventArgs e) //Přejmenovat
        {
            Downloader item = lastItem;

            if (item.FileName == "") return;

            RenameWindow renameWindow = new RenameWindow(item);
            renameWindow.Owner = this;

            if (renameWindow.ShowDialog() == true)
            {
                item.Refresh();
                listView.Items.Refresh();
            }
        }

        private void MenuItemLimit_Click(object sender, RoutedEventArgs e) //Omezit rychlost
        {
            Downloader item = lastItem;

            BandwidthWindow bandwidthWindow = new BandwidthWindow();
            bandwidthWindow.textBox.Text = (item.SpeedLimit / 1024).ToString();
            bandwidthWindow.Owner = this;
            if (bandwidthWindow.ShowDialog() == true)
            {
                item.SpeedLimit = Int64.Parse(bandwidthWindow.textBox.Text) * 1024;
                item.Refresh();
                listView.Items.Refresh();
            }
        }

        private void MenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            Downloader item = lastItem;

            Clipboard.SetText(item.Url);
        }

        private void ListViewItem_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            ListViewItem cItem = sender as ListViewItem;
            Downloader item = cItem.DataContext as Downloader;
            lastItem = item;

            if (cItem != null && item != null)
            {
                //zakáže nebo povolí položku "Otevřít" podle toho, jestli soubor existuje
                MenuItem menuItem = cItem.ContextMenu.Items[0] as MenuItem;
                if (File.Exists(item.FullPath)) menuItem.IsEnabled = true;
                else menuItem.IsEnabled = false;

                //zakáže nebo povolí položku "Otevřít ve složce" podle toho, jestli složka existuje
                menuItem = cItem.ContextMenu.Items[1] as MenuItem;
                if (Directory.Exists(item.Directory)) menuItem.IsEnabled = true;
                else menuItem.IsEnabled = false;

                //zakáže položku "Přejmenovat", pokud finální jméno souboru ještě nebylo získáno
                menuItem = cItem.ContextMenu.Items[2] as MenuItem;
                if (item.FileName == "") menuItem.IsEnabled = false;
                else menuItem.IsEnabled = true;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (Downloader.ActiveCount > 0)
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

            foreach (Downloader item in listView.Items)
            {
                Link link = new Link();
                link.Directory = item.Directory;
                link.FileName = item.FileName;
                link.Url = item.Url;
                link.TotalBytes = item.TotalBytes;
                link.SpeedLimit = item.SpeedLimit;
                links.List.Add(link);
            }
            links.Save();
            trayIcon.Dispose();
            System.Environment.Exit(1);
        }
    }
}