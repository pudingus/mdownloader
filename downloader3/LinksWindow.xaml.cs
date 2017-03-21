using Microsoft.Win32;
using System;
using System.Windows;

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

        public string fileName;
        public string url;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            Uri uri = new Uri(comboBox.Text);
            saveDialog.FileName = System.IO.Path.GetFileName(uri.LocalPath);
            if (saveDialog.ShowDialog() == true)
            {
                fileName = saveDialog.FileName;
                url = comboBox.Text;
                DialogResult = true;
                Close();
            }
        }
    }
}