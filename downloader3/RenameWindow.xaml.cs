using System.Windows;

namespace downloader3
{
    /// <summary>
    /// Interaction logic for RenameWindow.xaml
    /// </summary>
    public partial class RenameWindow : Window
    {
        private string fileName;

        public string FileName
        {
            get { return fileName; }
            set { textBox.Text = value; fileName = value; }
        }

        public RenameWindow()
        {
            InitializeComponent();
        }

        private void buttonRename_Click(object sender, RoutedEventArgs e)
        {
            if (fileName != textBox.Text)
            {
                fileName = textBox.Text;
                DialogResult = true;
            }
            Close();
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textBox.Focus();
            textBox.SelectAll();
        }
    }
}