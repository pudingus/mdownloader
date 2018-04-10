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
        private Downloader _item;

        /// <summary>
        /// Vytvoří novou instanci třídy <see cref="RenameWindow"/>
        /// </summary>
        /// <param name="item">Reference na vybranou položku</param>
        public RenameWindow(Downloader item)
        {
            InitializeComponent();
            _item = item;
        }

        private void Rename()
        {
            string filename = textBox.Text;
            if (filename == _item.Name)
            {
                Close();
            }
            else
            {
                string path = Path.Combine(_item.Directory, textBox.Text);

                if (Util.IsValidPath(path))
                {
                    if (!File.Exists(path))
                    {
                        _item.Rename(textBox.Text);

                        DialogResult = true;
                    }
                    else MessageBox.Show(Lang.Translate("lang_file_exists"), Lang.Translate("lang_error"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else MessageBox.Show(Lang.Translate("lang_invalid_path"), Lang.Translate("lang_error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }            
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
            textBox.Text = _item.FileName;

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