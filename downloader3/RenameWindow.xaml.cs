using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace downloader3
{
    /// <summary>
    /// Interaction logic for RenameWindow.xaml
    /// </summary>
    public partial class RenameWindow : Window
    {    
        public LvData item;

        public RenameWindow()
        {
            InitializeComponent();
        }

        private void Rename()
        {
            string filename = textBox.Text;
            string path = Path.Combine(item.Directory, textBox.Text);

            if (Util.IsValidPath(path))
            {
                if (!File.Exists(path))
                {
                    item.Client.Rename(textBox.Text);

                    DialogResult = true;
                }
                else MessageBox.Show("Soubor už existuje", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else MessageBox.Show("Neplatný název souboru", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            Rename();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textBox.Text = item.Client.FileName;

            textBox.Focus();
            textBox.SelectAll();
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Rename();
            }
        }
    }
}