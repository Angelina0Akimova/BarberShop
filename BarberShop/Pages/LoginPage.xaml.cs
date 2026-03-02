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
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        // Обработчик кнопки входа
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Скрываем предыдущие сообщения об ошибках
            ErrorMessageText.Visibility = Visibility.Collapsed;

            // Проверка на пустые поля
            if (string.IsNullOrWhiteSpace(LoginTextBox.Text) ||
                string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                ShowError("Пожалуйста, заполните все поля");
                return;
            }

            try
            {
                // Поиск пользователя по телефону или email
                var user = AppConnect.modelBd.Users
                    .FirstOrDefault(u => (u.Phone == LoginTextBox.Text ||
                                         u.Email == LoginTextBox.Text) &&
                                         u.IsActive == true);

                if (user == null)
                {
                    ShowError("Пользователь не найден");
                    return;
                }

                // Проверка пароля (в реальном проекте нужно хеширование!)
                if (user.PasswordHash != PasswordBox.Password)
                {
                    ShowError("Неверный пароль");
                    return;
                }

                // Сохраняем текущего пользователя
                AppConnect.currentUser = user;

                // Перенаправление на соответствующую страницу в зависимости от роли
                switch (user.RoleID)
                {
                    case 1: // Администратор
                        AppFrame.frame.Navigate(new AdminPage());
                        break;
                    case 2: // мастер
                            // AppFrame.frame.Navigate(new MasterPage());
                        break;
                    case 3: // клиент
                            
                        AppFrame.frame.Navigate(new ClientPage());
                        break;
                    default:
                        ShowError("Неизвестная роль пользователя");
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка подключения к базе данных: {ex.Message}");
            }
        }

        // Обработчик для кнопки "Войти как гость"
        private void GuestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Сбрасываем текущего пользователя (гость - не авторизован)
                AppConnect.currentUser = null;

                // Переходим на информационную страницу
                AppFrame.frame.Navigate(new GuestInfoPage());
            }
            catch (Exception ex)
            {
                ShowError($"Ошибка: {ex.Message}");
            }
        }

        // Обработчик для текста "Забыли пароль"
        private void ForgotPasswordText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Показываем/скрываем подсказку
            ToolTipBorder.Visibility = ToolTipBorder.Visibility == Visibility.Visible ?
                Visibility.Collapsed : Visibility.Visible;
        }

        // Метод для отображения ошибок
        private void ShowError(string message)
        {
            ErrorMessageText.Text = message;
            ErrorMessageText.Visibility = Visibility.Visible;
        }
    }
}
