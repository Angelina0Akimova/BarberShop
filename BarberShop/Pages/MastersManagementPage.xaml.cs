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
            public bool IsActive { get; set; }
            public string StatusText { get; set; }
            public SolidColorBrush StatusColor { get; set; }
            public string ToggleStatusTooltip { get; set; }
            public string ToggleStatusIcon { get; set; }
            public string HireDateText { get; set; }
            public List<MasterAppointmentDisplay> ActiveAppointments { get; set; }
            public Visibility HasNoAppointments { get; set; }
        }

        private List<MasterDisplay> allMasters = new List<MasterDisplay>();
        private List<MasterDisplay> filteredMasters = new List<MasterDisplay>();
        private bool isPageLoaded = false;

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

                isPageLoaded = true;
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
                // Получаем всех мастеров (RoleID = 2)
                var mastersQuery = from emp in AppConnect.modelBd.Employees
                                   join user in AppConnect.modelBd.Users on emp.UserID equals user.UserID
                                   where user.RoleID == 2 // Мастер
                                   orderby user.IsActive descending, user.LastName
                                   select new
                                   {
                                       Employee = emp,
                                       User = user
                                   };

                // Выполняем запрос к БД и получаем данные в память
                var mastersList = mastersQuery.ToList();

                allMasters.Clear();

                // Теперь работаем с данными в памяти
                foreach (var item in mastersList)
                {
                    // Получаем действующие записи мастера (статус "Запланировано" и дата >= сегодня)
                    DateTime today = DateTime.Today;

                    // Получаем ID статуса "Запланировано" (обычно 1)
                    int plannedStatusId = 1;

                    // Загружаем записи для конкретного мастера
                    var activeAppointmentsQuery = from a in AppConnect.modelBd.Appointments
                                                  join c in AppConnect.modelBd.Clients on a.ClientID equals c.ClientID
                                                  join cu in AppConnect.modelBd.Users on c.UserID equals cu.UserID
                                                  join s in AppConnect.modelBd.Services on a.ServiceID equals s.ServiceID
                                                  where a.EmployeeID == item.Employee.EmployeeID &&
                                                        a.AppointmentDate >= today &&
                                                        a.StatusID == plannedStatusId
                                                  orderby a.AppointmentDate, a.StartTime
                                                  select new
                                                  {
                                                      ClientLastName = cu.LastName,
                                                      ClientFirstName = cu.FirstName,
                                                      ServiceName = s.ServiceName,
                                                      AppointmentDate = a.AppointmentDate,
                                                      StartTime = a.StartTime
                                                  };

                    // Выполняем запрос и формируем отображаемые данные
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

                    // Определяем текст и иконку для кнопки изменения статуса
                    string toggleStatusTooltip = item.User.IsActive ? "Деактивировать мастера" : "Активировать мастера";
                    string toggleStatusIcon = item.User.IsActive ? "🔴" : "🟢";

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

                    // Форматируем дату найма
                    string hireDateText = $"В штате с {item.Employee.HireDate:dd.MM.yyyy}";

                    var masterDisplay = new MasterDisplay
                    {
                        UserId = item.User.UserID,
                        EmployeeId = item.Employee.EmployeeID,
                        FullName = $"{item.User.LastName} {item.User.FirstName}",
                        Phone = item.User.Phone ?? "Не указан",
                        PositionName = position,
                        Login = item.User.Email ?? item.User.Phone ?? "Нет логина",
                        IsActive = item.User.IsActive,
                        StatusText = statusText,
                        StatusColor = statusColor,
                        ToggleStatusTooltip = toggleStatusTooltip,
                        ToggleStatusIcon = toggleStatusIcon,
                        HireDateText = hireDateText,
                        ActiveAppointments = activeList,
                        HasNoAppointments = activeList.Any() ? Visibility.Collapsed : Visibility.Visible
                    };

                    allMasters.Add(masterDisplay);
                }

                // Обновляем счетчик
                UpdateMastersCount();

                // Явно устанавливаем фильтр "Все мастера" при загрузке
                SetFilterToAllMasters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мастеров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Явно устанавливает фильтр для отображения всех мастеров
        /// </summary>
        private void SetFilterToAllMasters()
        {
            try
            {
                // Убеждаемся, что выбран RadioButton "Все"
                if (FilterAllRadio != null)
                {
                    FilterAllRadio.IsChecked = true;
                }

                // Отображаем всех мастеров
                filteredMasters = new List<MasterDisplay>(allMasters);

                // Обновляем отображение
                MastersList.ItemsSource = filteredMasters;

                if (NoMastersText != null)
                {
                    NoMastersText.Visibility = filteredMasters.Any() ?
                        Visibility.Collapsed : Visibility.Visible;
                }

                // Обновляем счетчик
                UpdateFilteredMastersCount();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка SetFilterToAllMasters: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            try
            {
                // Проверяем, что страница загружена и элементы управления инициализированы
                if (!isPageLoaded || SearchTextBox == null)
                {
                    return;
                }

                string searchText = SearchTextBox.Text?.ToLower() ?? "";

                // Сначала фильтруем по поиску
                var searchFiltered = allMasters;

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    searchFiltered = allMasters
                        .Where(m => m.FullName.ToLower().Contains(searchText))
                        .ToList();
                }

                // Затем по статусу (проверяем, что RadioButton не null)
                if (FilterActiveRadio != null && FilterActiveRadio.IsChecked == true)
                {
                    filteredMasters = searchFiltered.Where(m => m.IsActive).ToList();
                }
                else if (FilterInactiveRadio != null && FilterInactiveRadio.IsChecked == true)
                {
                    filteredMasters = searchFiltered.Where(m => !m.IsActive).ToList();
                }
                else // Все мастера (по умолчанию)
                {
                    filteredMasters = searchFiltered;
                }

                // Обновляем отображение
                MastersList.ItemsSource = filteredMasters;

                if (NoMastersText != null)
                {
                    NoMastersText.Visibility = filteredMasters.Any() ?
                        Visibility.Collapsed : Visibility.Visible;
                }

                // Обновляем счетчик с учетом фильтра
                UpdateFilteredMastersCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateMastersCount()
        {
            try
            {
                if (FilterActiveRadio == null || FilterInactiveRadio == null || FilterAllRadio == null)
                    return;

                int total = allMasters.Count;
                int active = allMasters.Count(m => m.IsActive);
                int inactive = allMasters.Count(m => !m.IsActive);

                FilterActiveRadio.Content = $"Активные ({active})";
                FilterInactiveRadio.Content = $"Неактивные ({inactive})";
                FilterAllRadio.Content = $"Все ({total})";
            }
            catch (Exception ex)
            {
                // Просто логируем ошибку, но не показываем пользователю
                System.Diagnostics.Debug.WriteLine($"Ошибка UpdateMastersCount: {ex.Message}");
            }
        }

        private void UpdateFilteredMastersCount()
        {
            try
            {
                if (MastersCountText != null)
                {
                    MastersCountText.Text = $"Показано: {filteredMasters.Count} из {allMasters.Count}";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка UpdateFilteredMastersCount: {ex.Message}");
            }
        }

        private void FilterRadio_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем, что страница загружена
                if (!isPageLoaded)
                    return;

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при применении фильтра: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                // Проверяем, что страница загружена
                if (!isPageLoaded)
                    return;

                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private async void ToggleStatusButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag != null)
                {
                    int userId = (int)button.Tag;

                    // Находим мастера в списке
                    var master = allMasters.FirstOrDefault(m => m.UserId == userId);
                    if (master == null) return;

                    string newStatus = master.IsActive ? "деактивировать" : "активировать";
                    string action = master.IsActive ? "деактивации" : "активации";

                    // Запрашиваем подтверждение
                    var result = MessageBox.Show(
                        $"Вы действительно хотите {newStatus} мастера {master.FullName}?",
                        "Подтверждение действия",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Находим пользователя в базе данных
                        var user = AppConnect.modelBd.Users.FirstOrDefault(u => u.UserID == userId);
                        if (user != null)
                        {
                            // Меняем статус
                            user.IsActive = !user.IsActive;

                            // Сохраняем изменения в базе данных
                            await AppConnect.modelBd.SaveChangesAsync();

                            // Обновляем отображение мастера в списке
                            master.IsActive = user.IsActive;
                            master.StatusText = user.IsActive ? "Активен" : "Неактивен";
                            master.StatusColor = user.IsActive ?
                                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")) :
                                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4444"));
                            master.ToggleStatusTooltip = user.IsActive ? "Деактивировать мастера" : "Активировать мастера";
                            master.ToggleStatusIcon = user.IsActive ? "🔴" : "🟢";

                            // Обновляем отображение списка
                            MastersList.Items.Refresh();

                            // Обновляем счетчики статусов
                            UpdateMastersCount();

                            // Применяем фильтр заново (если текущий фильтр исключает мастера)
                            ApplyFilter();

                            MessageBox.Show(
                                $"Мастер успешно {(user.IsActive ? "активирован" : "деактивирован")}!",
                                "Успех",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении статуса: {ex.Message}", "Ошибка",
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