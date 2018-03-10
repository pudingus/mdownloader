using System;
using System.Windows;

namespace downloader3
{
    /// <summary>
    /// Interaction logic for RenameWindow.xaml
    /// </summary>
    public partial class RenameWindow : Window
    {    
        public MyData item;

        public RenameWindow()
        {
            InitializeComponent();
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {            
            try
            {
                item.Client.Rename(textBox.Text);
                DialogResult = true;
            }
            catch (ArgumentException ex)
            {                    
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textBox.Text = item.Name;

            textBox.Focus();
            textBox.SelectAll();
        }
    }
}