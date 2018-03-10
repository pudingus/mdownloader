using Microsoft.Win32;
using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.IO;

namespace downloader3
{
    /// <summary>
    /// Interakční logika pro LinksWindow.xaml
    /// </summary>
    public partial class LinksWindow : Window
    {
        public LinksWindow()
        {
            InitializeComponent();
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (Path.IsPathRooted(pathTextBox.Text))
            {
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Neplatná cesta", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            //MessageBox.Show("aaa");
            /*if (!linksTextBox.Text.EndsWith("\n"))
            {
                int index = linksTextBox.CaretIndex;
                linksTextBox.Text = linksTextBox.Text + "\n";
                linksTextBox.CaretIndex = index;
            }*/
        }
    }
}