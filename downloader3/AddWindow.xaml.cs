using Microsoft.Win32;
using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.IO;
using System.Collections.Generic;

namespace downloader3
{
    /// <summary>
    /// Interakční logika pro LinksWindow.xaml
    /// </summary>
    public partial class AddWindow : Window
    {
        public AddWindow()
        {
            InitializeComponent();
        }

        public List<string> urlList;
        private List<string> badLines;        

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (Util.IsValidPath(pathTextBox.Text))
            {                
                urlList = new List<string>();
                badLines = new List<string>();

                for (int i = 0; i < linksTextBox.LineCount; i++)
                {
                    string url = linksTextBox.GetLineText(i);

                    if (Util.IsValidURL(url))
                    {
                        url = Regex.Replace(url, @"\r\n?|\n", "");
                        urlList.Add(url);
                    }
                    else if (!String.IsNullOrWhiteSpace(url)) badLines.Add(url);                    
                }

                if (badLines.Count > 0)
                {
                    string s = "";
                    foreach (string url in badLines) s = s + url;
                    if (MessageBox.Show(Lang.Translate("lang_invalid_links") + "\n\n" + s, Lang.Translate("lang_error"), MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes)
                    {
                        foreach (string url in badLines)
                        {
                            linksTextBox.Text = linksTextBox.Text.Replace(url, "");
                        }
                    }
                }
                else if (urlList.Count < 1)
                {
                    MessageBox.Show(Lang.Translate("lang_no_valid_links"), Lang.Translate("lang_error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else DialogResult = true;
            }    
            else
            {
                MessageBox.Show(Lang.Translate("lang_invalid_path"), Lang.Translate("lang_error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }        

        private void buttonBrowse_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = pathTextBox.Text;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                pathTextBox.Text = dialog.FileName;
            }
        }

        private void linksTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!linksTextBox.Text.EndsWith("\n"))
            {
                int index = linksTextBox.CaretIndex;
                linksTextBox.Text = linksTextBox.Text + "\n";
                linksTextBox.CaretIndex = index;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            pathTextBox.Text = KnownFolders.Downloads.Path;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddHelpWindow window = new AddHelpWindow();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }
    }
}