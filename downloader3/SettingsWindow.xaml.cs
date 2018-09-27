using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace downloader3
{
    /// <summary>
    /// Představuje okno s nastavením programu
    /// </summary>
    public partial class SettingsWindow : Window
    {
        /// <summary>
        /// Získá nebo nastaví jazyk a vybere příslušnou položku v ComboBoxu
        /// </summary>
        public string Lang {
            get {
                if (langSelection.SelectedIndex == 0) _lang = "cs-CZ";
                else if (langSelection.SelectedIndex == 1) _lang = "en-US";
                return _lang;
            }
            set {
                _lang = value;
                if (_lang == "cs-CZ") langSelection.SelectedIndex = 0;
                else if (_lang == "en-US") langSelection.SelectedIndex = 1;
            }
        }
        private string _lang;

        /// <summary>
        /// Vytvoří novou instanci třídy <see cref="SettingsWindow"/>
        /// </summary>
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void textBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+"); //pouze čísla
            e.Handled = regex.IsMatch(e.Text);
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
    }
}