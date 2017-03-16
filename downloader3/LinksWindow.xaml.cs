using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

        public string filename;
        public string url;
        private void button_Click(object sender, RoutedEventArgs e)
        {            
            SaveFileDialog saveDialog = new SaveFileDialog();
            Uri uri = new Uri(comboBox.Text);            //
            saveDialog.FileName = System.IO.Path.GetFileName(uri.LocalPath);
            if (saveDialog.ShowDialog() == true)
            {
                filename = saveDialog.FileName;
                url = comboBox.Text;
                DialogResult = true;
                Close();
            }
        }        
    }
}
