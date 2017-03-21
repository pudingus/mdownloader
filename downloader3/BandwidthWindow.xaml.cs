using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

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