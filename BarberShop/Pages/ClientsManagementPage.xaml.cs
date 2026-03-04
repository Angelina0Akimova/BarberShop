using BarberShop.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BarberShop.Pages
{
    public partial class ClientsManagementPage : Page
    {
        // Класс для отображения клиента
        public class ClientDisplay
        {
            public int UserId { get; set; }
            public string FullName { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Login { get; set; } // Будем использовать Email как логин
            public string RegistrationDate { get; set; }
            public string LastVisitDate { get; set; }
            public string StatusText { get; set; }
            public SolidColorBrush StatusColor { get; set; }
        }

        private List<ClientDisplay> allClients = new List<ClientDisplay>();
        private string currentSortOrder = "desc"; // По умолчанию сначала новые

        public ClientsManagementPage()
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
                    ShowErrorAndGoBack("Пожалуйста, авторизуйтесь");
                    return;
                }

                // Проверяем роль администратора (RoleID = 1)
                if (AppConnect.currentUser.RoleID != 1)
                {
                    ShowErrorAndGoBack("У вас нет прав доступа к этой странице");
                    return;
                }

                // Отображаем информацию об администраторе
                AdminInfoText.Text = $"Администратор: {AppConnect.currentUser.FirstName} {AppConnect.currentUser.LastName}";

                // Загружаем клиентов
                LoadClients();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowErrorAndGoBack(string message)
        {
            MessageBox.Show(message, "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            AppFrame.frame.Navigate(new AdminPage());
        }

        private void LoadClients()
        {
            try
            {
                // Получаем всех клиентов (RoleID = 3)
                var users = AppConnect.modelBd.Users
                    .Where(u => u.RoleID == 3)
                    .ToList();

                allClients.Clear();

                foreach (var user in users)
                {
                    // Находим клиента в таблице Clients по UserID
                    var client = AppConnect.modelBd.Clients
                        .FirstOrDefault(c => c.UserID == user.UserID);

                    // Находим последнюю запись клиента в Appointments через ClientID
                    DateTime? lastAppointmentDate = null;

                    if (client != null)
                    {
                        // Ищем последнюю запись в Appointments по ClientID
                        var lastAppointment = AppConnect.modelBd.Appointments
                            .Where(a => a.ClientID == client.ClientID)  // Важно: ClientID, а не ClientId
                            .OrderByDescending(a => a.AppointmentDate)
                            .FirstOrDefault();

                        if (lastAppointment != null)
                        {
                            lastAppointmentDate = lastAppointment.AppointmentDate;
                        }
                    }

                    // Определяем статус активности
                    string statusText = user.IsActive ? "Активен" : "Заблокирован";
                    var statusColor = user.IsActive ?
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")) :
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4444"));

                    // Используем Email как логин
                    string login = user.Email ?? "Не указан";

                    // Формируем отображаемые данные
                    var clientDisplay = new ClientDisplay
                    {
                        UserId = user.UserID,
                        FullName = $"{user.LastName} {user.FirstName}",
                        Phone = user.Phone ?? "Не указан",
                        Email = string.IsNullOrEmpty(user.Email) ? "Не указан" : user.Email,
                        Login = login,
                        RegistrationDate = user.RegistrationDate.ToString("dd.MM.yyyy"),
                        LastVisitDate = lastAppointmentDate.HasValue ?
                            lastAppointmentDate.Value.ToString("dd.MM.yyyy HH:mm") :
                            "Нет посещений",
                        StatusText = statusText,
                        StatusColor = statusColor
                    };

                    allClients.Add(clientDisplay);
                }

                // Применяем сортировку
                ApplySorting();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplySorting()
        {
            try
            {
                // Сначала фильтруем по поиску
                var filteredClients = FilterClients();

                // Затем сортируем по дате последнего посещения
                if (currentSortOrder == "desc")
                {
                    filteredClients = filteredClients
                        .OrderByDescending(c =>
                        {
                            if (c.LastVisitDate == "Нет посещений")
                                return DateTime.MinValue;

                            try
                            {
                                return DateTime.ParseExact(c.LastVisitDate.Split(' ')[0], "dd.MM.yyyy", null);
                            }
                            catch
                            {
                                return DateTime.MinValue;
                            }
                        })
                        .ToList();
                }
                else
                {
                    filteredClients = filteredClients
                        .OrderBy(c =>
                        {
                            if (c.LastVisitDate == "Нет посещений")
                                return DateTime.MaxValue;

                            try
                            {
                                return DateTime.ParseExact(c.LastVisitDate.Split(' ')[0], "dd.MM.yyyy", null);
                            }
                            catch
                            {
                                return DateTime.MaxValue;
                            }
                        })
                        .ToList();
                }

                // Обновляем отображение
                ClientsList.ItemsSource = filteredClients;
                NoClientsText.Visibility = filteredClients.Any() ?
                    Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сортировки: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<ClientDisplay> FilterClients()
        {
            string searchText = SearchTextBox.Text?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                return allClients.ToList();
            }

            return allClients
                .Where(c => c.FullName.ToLower().Contains(searchText))
                .ToList();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySorting();
        }

        private void SortDescButton_Click(object sender, RoutedEventArgs e)
        {
            currentSortOrder = "desc";
            ApplySorting();
        }

        private void SortAscButton_Click(object sender, RoutedEventArgs e)
        {
            currentSortOrder = "asc";
            ApplySorting();
        }

        private void AddClientButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddClientWindow();
                addWindow.Owner = Window.GetWindow(this);

                if (addWindow.ShowDialog() == true)
                {
                    // Перезагружаем список клиентов
                    LoadClients();
                    MessageBox.Show("Клиент успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении клиента: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag != null)
                {
                    int userId = (int)button.Tag;

                    var changePasswordWindow = new ChangeClientPasswordWindow(userId);
                    changePasswordWindow.Owner = Window.GetWindow(this);

                    if (changePasswordWindow.ShowDialog() == true)
                    {
                        MessageBox.Show("Пароль успешно изменен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении пароля: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Возвращаемся на страницу администратора
                AppFrame.frame.Navigate(new AdminPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}