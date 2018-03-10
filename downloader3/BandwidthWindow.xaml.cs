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

        private void buttonOK_Click(object sender, RoutedEventArgs e)
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



        private void textBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy ||
                e.Command == ApplicationCommands.Cut ||
                e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void labelUp_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int number = Int32.Parse(textBox.Text);
            number = number + 100;
            textBox.Text = number.ToString();
        }

        private void labelDown_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int number = Int32.Parse(textBox.Text);
            number = number - 100;
            textBox.Text = number.ToString();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }
}