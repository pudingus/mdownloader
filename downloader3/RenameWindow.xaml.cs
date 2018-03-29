using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace downloader3
{
    /// <summary>
    /// Představuje okno pro přejmenování položky a souboru
    /// </summary>
    public partial class RenameWindow : Window
    {    
        private LvData _item;

        /// <summary>
        /// Vytvoří novou instanci třídy <see cref="RenameWindow"/>
        /// </summary>
        /// <param name="item">Reference na vybranou položku</param>
        public RenameWindow(LvData item)
        {
            InitializeComponent();
            _item = item;
        }

        private void Rename()
        {
            string filename = textBox.Text;
            string path = Path.Combine(_item.Directory, textBox.Text);

            if (Util.IsValidPath(path))
            {
                if (!File.Exists(path))
                {
                    _item.Client.Rename(textBox.Text);

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
            textBox.Text = _item.Client.FileName;

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