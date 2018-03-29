using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Představuje okno nápovědy pro <see cref="AddWindow"/>
    /// </summary>
    public partial class AddHelpWindow : Window
    {
        /// <summary>
        /// Vytvoří novou instanci třídy <see cref="AddHelpWindow"/>
        /// </summary>
        public AddHelpWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
