using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for BandwidthWindow.xaml
    /// </summary>
    public partial class BandwidthWindow : Window
    {
        private int speedLimit;
        public int SpeedLimit
        {
            get { return speedLimit; }
            set { textBox.Text = value.ToString(); speedLimit = value; }
        }

        public BandwidthWindow()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+"); //pouze čísla
            e.Handled = regex.IsMatch(e.Text);
        }

        private void buttonLimit_Click(object sender, RoutedEventArgs e)
        {
            if (speedLimit.ToString() != textBox.Text)
            {
                speedLimit = Int32.Parse(textBox.Text);
                DialogResult = true;
            }
            Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
