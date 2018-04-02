using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace downloader3
{
    /// <summary>
    /// Představuje okno pro potvrzení odebrání položek
    /// </summary>
    public partial class RemoveWindow : Window
    {
        /// <summary>
        /// Udává jestli se mají soubory smazat také z disku
        /// </summary>
        public bool DeleteFiles { get; private set; } = false;

        /// <summary>
        /// Vytvoří novou instanci třídy <see cref="RemoveWindow"/>
        /// </summary>
        public RemoveWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            image.Source = Imaging.CreateBitmapSourceFromHIcon(
                System.Drawing.SystemIcons.Warning.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());            
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            //normálně by stačilo do DeleteFiles uložit hodnotu IsChecked,
            //jenže IsChecked je typu "bool?", takže může být i null
            if (checkbox.IsChecked == true) DeleteFiles = true;
            DialogResult = true;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
