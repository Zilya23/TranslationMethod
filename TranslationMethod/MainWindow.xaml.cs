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
using TranslationMethod.Pages;

namespace TranslationMethod
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainPage.NavigationService.Navigate(new Lab1Page());

            var allLabs = new List<string>() { "Лаб1", "Лаб2", "Лаб3" };
            cb_LabNumber.ItemsSource = allLabs;
        }

        private void cb_LabNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cb_LabNumber.SelectedIndex != -1)
            {
                if (cb_LabNumber.SelectedIndex == 0)
                    MainPage.Navigate(new Lab1Page());
                else if (cb_LabNumber.SelectedIndex == 1)
                    MainPage.Navigate(new Lab1Page());
                else
                    MainPage.Navigate(new Lab1Page());
            }
        }
    }
}
