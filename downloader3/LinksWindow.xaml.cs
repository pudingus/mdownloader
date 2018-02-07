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

        public string filePath;
        public string url;

        private void button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            try
            {
                Uri uri = new Uri(comboBox.Text);

                saveDialog.FileName = System.IO.Path.GetFileName(uri.LocalPath);
                if (saveDialog.ShowDialog() == true)
                {
                    filePath = saveDialog.FileName;
                    url = comboBox.Text;
                    DialogResult = true;
                    Close();
                }                
            }
            catch (System.UriFormatException)
            {               
                MessageBox.Show("Neplatná adresa", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }
    }
}