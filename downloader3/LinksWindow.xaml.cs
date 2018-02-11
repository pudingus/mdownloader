using Microsoft.Win32;
using System;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Net;
using System.Text.RegularExpressions;
using System.Collections.Specialized;

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

        //public string filePath;
        //public string url;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            /*SaveFileDialog saveDialog = new SaveFileDialog();
            try
            {
                //Uri uri = new Uri(comboBox.Text);

                //saveDialog.FileName = System.IO.Path.GetFileName(uri.LocalPath);
                if (saveDialog.ShowDialog() == true)
                {
                    filePath = saveDialog.FileName;
                    //url = comboBox.Text;
                    DialogResult = true;
                    Close();
                }                
            }
            catch (System.UriFormatException)
            {
                System.Windows.MessageBox.Show("Neplatná adresa", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }       */     
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;


            /*WebRequest webRequest = WebRequest.Create(linksTextBox.Text);
            WebResponse webResponse = webRequest.GetResponse();

            string filename;

            //attachment; filename*=UTF-8''vs_Community.exe
            string header = webResponse.Headers["Content-Disposition"];
            if (header != null)
            {
                filename = header.Replace("attachment; ", "").Replace("attachment;", "").Replace("filename=", "").Replace("filename*=UTF-8''", "").Replace("\"", "");
            }
            else
            {
                Uri uri = new Uri(linksTextBox.Text);
                filename = System.IO.Path.GetFileName(uri.LocalPath);
            }

            MessageBox.Show(filename);*/
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
    }
}