using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace downloader3
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public string language;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+"); //pouze čísla
            e.Handled = regex.IsMatch(e.Text);
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            if (langSelection.SelectedIndex == 0) language = "cs-CZ";
            else if (langSelection.SelectedIndex == 1) language = "en-US";
            DialogResult = true;
            Close();
        }
    }
}