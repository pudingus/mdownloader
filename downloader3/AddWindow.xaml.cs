﻿using Microsoft.Win32;
using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.IO;
using System.Collections.Generic;
using System.Windows.Media;

namespace downloader3
{
    /// <summary>
    /// Představuje okno pro přidání odkazů
    /// </summary>
    public partial class AddWindow : Window
    {
        /// <summary>
        /// Vytvoří novou instanci třídy <see cref="AddWindow"/>
        /// </summary>
        public AddWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Představuje seznam zadaných odkazů bez nadbytečných mezer nebo prázdných řádků
        /// </summary>
        public List<string> UrlList { get; private set; }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (Util.IsPathValid(pathTextBox.Text))
            {
                UrlList = new List<string>();
                List<string> badLines = new List<string>();

                for (int i = 0; i < linksTextBox.LineCount; i++)
                {
                    string url = linksTextBox.GetLineText(i);

                    if (Util.IsUrlValid(url))
                    {
                        url = Regex.Replace(url, @"\r\n?|\n", "");
                        UrlList.Add(url);
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
                else if (UrlList.Count < 1)
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