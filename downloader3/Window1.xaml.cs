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
    /// Interakční logika pro Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();
        }

        public string filename;
        public string url;
        private void button_Click(object sender, RoutedEventArgs e)
        {            
            SaveFileDialog s = new SaveFileDialog();
            Uri uri = new Uri(textBox.Text);            
            s.FileName = System.IO.Path.GetFileName(uri.LocalPath);
            if (s.ShowDialog() == true)
            {
                filename = s.FileName;
                url = textBox.Text;
                DialogResult = true;
                Close();
            }
        }
    }
}
