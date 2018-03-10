using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace uitest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            System.Media.SystemSounds.Asterisk.Play();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Media.SystemSounds.Beep.Play();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            System.Media.SystemSounds.Exclamation.Play();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            System.Media.SystemSounds.Hand.Play();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            System.Media.SystemSounds.Question.Play();
        }
    }
}
