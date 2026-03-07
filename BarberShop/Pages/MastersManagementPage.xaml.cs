using BarberShop.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BarberShop.Pages
{
    public partial class MastersManagementPage : Page
    {
        // Класс для отображения записи мастера
        public class MasterAppointmentDisplay
        {
            public string ClientName { get; set; }
            public string ServiceName { get; set; }
            public string AppointmentDate { get; set; }
            public string StartTime { get; set; }
        }

        // Класс для отображения мастера
        public class MasterDisplay
        {
            public int UserId { get; set; }
            public int EmployeeId { get; set; }
            public string FullName { get; set; }
            public string Phone { get; set; }
            public string PositionName { get; set; }
            public string Login { get; set; }
            public string StatusText { get; set; }
            public SolidColorBrush StatusColor { get; set; }
            public List<MasterAppointmentDisplay> ActiveAppointments { get; set; }
            public Visibility HasNoAppointments { get; set; }
        }

        private List<MasterDisplay> allMasters = new List<MasterDisplay>();

        public MastersManagementPage()
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
                AdminInfoText.Text = $"Администратор: {AppConnect.currentUser.LastName} {AppConnect.currentUser.FirstName}";

                // Загружаем мастеров
                LoadMasters();
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

        private void LoadMasters()
        {
            try
            {
                // Получаем всех мастеров (RoleID = 2) - сначала выполняем запрос к БД
                var mastersQuery = from emp in AppConnect.modelBd.Employees
                                   join user in AppConnect.modelBd.Users on emp.UserID equals user.UserID
                                   where user.RoleID == 2 // Мастер
                                   select new
                                   {
                                       Employee = emp,
                                       User = user
                                   };

                // Выполняем запрос к БД и получаем данные в память
                var mastersList = mastersQuery.ToList();

                allMasters.Clear();

                // Теперь работаем с данными в памяти, здесь можно использовать C# методы
                foreach (var item in mastersList)
                {
                    // Получаем действующие записи мастера (статус "Запланировано" и дата >= сегодня)
                    DateTime today = DateTime.Today;

                    // Получаем ID статуса "Запланировано" (обычно 1)
                    int plannedStatusId = 1;

                    // Загружаем записи для конкретного мастера - отдельный запрос к БД
                    var activeAppointmentsQuery = from a in AppConnect.modelBd.Appointments
                                                  join c in AppConnect.modelBd.Clients on a.ClientID equals c.ClientID
                                                  join cu in AppConnect.modelBd.Users on c.UserID equals cu.UserID
                                                  join s in AppConnect.modelBd.Services on a.ServiceID equals s.ServiceID
                                                  where a.EmployeeID == item.Employee.EmployeeID &&
                                                        a.AppointmentDate >= today &&
                                                        a.StatusID == plannedStatusId
                                                  select new
                                                  {
                                                      ClientLastName = cu.LastName,
                                                      ClientFirstName = cu.FirstName,
                                                      ServiceName = s.ServiceName,
                                                      AppointmentDate = a.AppointmentDate,
                                                      StartTime = a.StartTime
                                                  };

                    // Выполняем запрос и формируем отображаемые данные уже в памяти
                    var activeList = activeAppointmentsQuery.ToList()
                        .Select(a => new MasterAppointmentDisplay
                        {
                            ClientName = $"{a.ClientLastName} {a.ClientFirstName}",
                            ServiceName = a.ServiceName,
                            AppointmentDate = a.AppointmentDate.ToString("dd.MM.yyyy"),
                            StartTime = a.StartTime.ToString(@"hh\:mm")
                        })
                        .ToList();

                    // Определяем статус активности мастера
                    string statusText = item.User.IsActive ? "Активен" : "Неактивен";
                    var statusColor = item.User.IsActive ?
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")) :
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4444"));

                    // Определяем должность (из Bio или просто "Мастер")
                    string position = "Мастер";
                    if (!string.IsNullOrEmpty(item.Employee.Bio))
                    {
                        // Берем первую строку из Bio как должность
                        position = item.Employee.Bio.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .FirstOrDefault() ?? "Мастер";
                        if (position.Length > 30)
                            position = position.Substring(0, 30) + "...";
                    }

                    var masterDisplay = new MasterDisplay
                    {
                        UserId = item.User.UserID,
                        EmployeeId = item.Employee.EmployeeID,
                        FullName = $"{item.User.LastName} {item.User.FirstName}",
                        Phone = item.User.Phone ?? "Не указан",
                        PositionName = position,
                        Login = item.User.Email ?? item.User.Phone ?? "Нет логина",
                        StatusText = statusText,
                        StatusColor = statusColor,
                        ActiveAppointments = activeList,
                        HasNoAppointments = activeList.Any() ? Visibility.Collapsed : Visibility.Visible
                    };

                    allMasters.Add(masterDisplay);
                }

                // Применяем фильтрацию
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мастеров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            try
            {
                string searchText = SearchTextBox.Text?.ToLower() ?? "";

                var filteredMasters = allMasters;

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    filteredMasters = allMasters
                        .Where(m => m.FullName.ToLower().Contains(searchText))
                        .ToList();
                }

                // Обновляем отображение
                MastersList.ItemsSource = filteredMasters;
                NoMastersText.Visibility = filteredMasters.Any() ?
                    Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void AddMasterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddMasterWindow();
                addWindow.Owner = Window.GetWindow(this);

                if (addWindow.ShowDialog() == true)
                {
                    // Перезагружаем список мастеров
                    LoadMasters();
                    MessageBox.Show("Мастер успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении мастера: {ex.Message}", "Ошибка",
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

                    var changePasswordWindow = new ChangeMasterPasswordWindow(userId);
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