using BarberShop.AppData;
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

namespace BarberShop.Pages
{
    public partial class GuestInfoPage : Page
    {
        public GuestInfoPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик кнопки "Назад" - возвращает на страницу входа
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Навигация на страницу входа
            AppFrame.frame.Navigate(new LoginPage());
        }
    }
}