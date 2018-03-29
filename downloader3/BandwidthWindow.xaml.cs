using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace downloader3
{
    /// <summary>
    /// Představuje okno pro nastavení rychlostního limitu
    /// </summary>
    public partial class BandwidthWindow : Window
    {            
        /// <summary>
        /// Vytvoří novou instanci třídy <see cref="BandwidthWindow"/>
        /// </summary>
        public BandwidthWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {            
            textBox.Focus();
            textBox.SelectAll();
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {            
            DialogResult = true;            
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

        private void textBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+"); //pouze čísla
            e.Handled = regex.IsMatch(e.Text);
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {                
                DialogResult = true;
            }
        }
    }
}