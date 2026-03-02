using BarberShop.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BarberShop.Pages
{
    public partial class AdminPage : Page
    {
        private Users currentAdmin;

        public AdminPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем авторизацию
                if (AppConnect.currentUser == null)
                {
                    ShowErrorAndNavigate("Пожалуйста, авторизуйтесь");
                    return;
                }

                // Проверяем роль администратора (RoleID = 1)
                if (AppConnect.currentUser.RoleID != 1)
                {
                    ShowErrorAndNavigate("У вас нет прав доступа к этой странице");
                    return;
                }

                currentAdmin = AppConnect.currentUser;

                // Загружаем данные администратора
                LoadAdminData();

                // Загружаем статистику
                LoadStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowErrorAndNavigate(string message)
        {
            MessageBox.Show(message, "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            AppFrame.frame.Navigate(new LoginPage());
        }

        private void LoadAdminData()
        {
            try
            {
                // Заполняем имя в верхней панели
                AdminNameText.Text = $"{currentAdmin.FirstName} {currentAdmin.LastName}";

                // Заполняем имя в боковой панели
                SidebarAdminName.Text = $"{currentAdmin.FirstName} {currentAdmin.LastName}";

                // Заполняем основную информацию
                FirstNameText.Text = currentAdmin.FirstName;
                LastNameText.Text = currentAdmin.LastName;
                PhoneText.Text = currentAdmin.Phone;
                LoginText.Text = string.IsNullOrEmpty(currentAdmin.Email) ?
                    "Email не указан" : currentAdmin.Email;

                // Дата регистрации - RegistrationDate не nullable в вашей БД
                // Форматируем дату напрямую, без проверки HasValue
                RegistrationDateText.Text = currentAdmin.RegistrationDate.ToString("dd.MM.yyyy");

                // Статус (всегда активен для текущего пользователя)
                StatusText.Text = "Активен";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных администратора: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStatistics()
        {
            try
            {
                // Общее количество клиентов (RoleID = 3)
                int totalClients = AppConnect.modelBd.Users
                    .Count(u => u.RoleID == 3 && u.IsActive == true);
                TotalClientsText.Text = totalClients.ToString();

                // Количество записей на сегодня
                DateTime today = DateTime.Today;
                int todayAppointments = AppConnect.modelBd.Appointments
                    .Count(a => a.AppointmentDate == today);
                TodayAppointmentsText.Text = todayAppointments.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик для кнопок навигации
        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = sender as Button;
                string pageTag = button.Tag.ToString();

                string message = "";

                if (pageTag == "Appointments")
                {
                    message = "Переход на страницу записей клиентов";
                }
                else if (pageTag == "Clients")
                {
                    message = "Переход на страницу клиентов";
                }
                else if (pageTag == "Masters")
                {
                    message = "Переход на страницу мастеров";
                }
                else if (pageTag == "Reports")
                {
                    message = "Переход на страницу отчетов";
                }
                else
                {
                    message = "Переход на страницу";
                }

                MessageBox.Show($"{message} (в разработке)", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик кнопки "Изменить данные"
        private void EditDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Функция редактирования данных будет доступна в следующей версии",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик кнопки "Изменить пароль"
        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("Функция смены пароля будет доступна в следующей версии",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик кнопки выхода
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Вы действительно хотите выйти?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    AppConnect.currentUser = null;
                    AppFrame.frame.Navigate(new LoginPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}