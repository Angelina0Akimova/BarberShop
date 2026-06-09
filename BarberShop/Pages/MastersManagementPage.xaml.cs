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
        public class MasterAppointmentDisplay
        {
            public string ClientName { get; set; }
            public string ServiceName { get; set; }
            public string AppointmentDate { get; set; }
            public string StartTime { get; set; }
        }

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
                if (AppConnect.currentUser == null)
                {
                    ShowErrorAndGoBack("Пожалуйста, авторизуйтесь");
                    return;
                }

                if (AppConnect.currentUser.RoleID != 1)
                {
                    ShowErrorAndGoBack("У вас нет прав доступа к этой странице");
                    return;
                }

                AdminInfoText.Text = $"Администратор: {AppConnect.currentUser.LastName} {AppConnect.currentUser.FirstName}";
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
            MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            AppFrame.frame.Navigate(new AdminPage());
        }

        private void LoadMasters()
        {
            try
            {
                // Загружаем мастеров без Format в LINQ
                var mastersQuery = from emp in AppConnect.modelBd.Employees
                                   join user in AppConnect.modelBd.Users on emp.UserID equals user.UserID
                                   where user.RoleID == 2
                                   orderby user.IsActive descending, user.LastName
                                   select new { Employee = emp, User = user };

                var mastersList = mastersQuery.ToList();
                allMasters.Clear();

                foreach (var item in mastersList)
                {
                    DateTime today = DateTime.Today;
                    int plannedStatusId = 1;

                    // Получаем активные записи
                    var activeAppointments = AppConnect.modelBd.Appointments
                        .Where(a => a.EmployeeID == item.Employee.EmployeeID &&
                                    a.AppointmentDate >= today &&
                                    a.StatusID == plannedStatusId)
                        .Join(AppConnect.modelBd.Clients, a => a.ClientID, c => c.ClientID, (a, c) => new { a, c })
                        .Join(AppConnect.modelBd.Users, ac => ac.c.UserID, u => u.UserID, (ac, u) => new { ac.a, ac.c, ClientUser = u })
                        .Join(AppConnect.modelBd.Services, a => a.a.ServiceID, s => s.ServiceID, (a, s) => new { a.a, a.ClientUser, Service = s })
                        .OrderBy(x => x.a.AppointmentDate).ThenBy(x => x.a.StartTime)
                        .ToList();

                    var activeList = activeAppointments.Select(a => new MasterAppointmentDisplay
                    {
                        ClientName = $"{a.ClientUser.LastName} {a.ClientUser.FirstName}",
                        ServiceName = a.Service.ServiceName,
                        AppointmentDate = a.a.AppointmentDate.ToString("dd.MM.yyyy"),
                        StartTime = a.a.StartTime.ToString(@"hh\:mm")
                    }).ToList();

                    string statusText = item.User.IsActive ? "Активен" : "Неактивен";
                    var statusColor = item.User.IsActive ?
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")) :
                        new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4444"));

                    string toggleStatusTooltip = item.User.IsActive ? "Деактивировать мастера" : "Активировать мастера";
                    string toggleStatusIcon = item.User.IsActive ? "🔴" : "🟢";

                    string position = "Мастер";
                    if (!string.IsNullOrEmpty(item.Employee.Bio))
                    {
                        position = item.Employee.Bio.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Мастер";
                        if (position.Length > 30) position = position.Substring(0, 30) + "...";
                    }

                    string hireDateText = $"В штате с {item.Employee.HireDate:dd.MM.yyyy}";

                    allMasters.Add(new MasterDisplay
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
                    });
                }

                UpdateMastersCount();
                SetFilterToAllMasters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки мастеров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetFilterToAllMasters()
        {
            try
            {
                if (FilterAllRadio != null) FilterAllRadio.IsChecked = true;
                filteredMasters = new List<MasterDisplay>(allMasters);
                MastersList.ItemsSource = filteredMasters;
                if (NoMastersText != null) NoMastersText.Visibility = filteredMasters.Any() ? Visibility.Collapsed : Visibility.Visible;
                UpdateFilteredMastersCount();
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}"); }
        }

        private void ApplyFilter()
        {
            try
            {
                if (!isPageLoaded || SearchTextBox == null) return;

                string searchText = SearchTextBox.Text?.ToLower() ?? "";
                var searchFiltered = string.IsNullOrWhiteSpace(searchText) ? allMasters : allMasters.Where(m => m.FullName.ToLower().Contains(searchText)).ToList();

                if (FilterActiveRadio != null && FilterActiveRadio.IsChecked == true)
                    filteredMasters = searchFiltered.Where(m => m.IsActive).ToList();
                else if (FilterInactiveRadio != null && FilterInactiveRadio.IsChecked == true)
                    filteredMasters = searchFiltered.Where(m => !m.IsActive).ToList();
                else
                    filteredMasters = searchFiltered;

                MastersList.ItemsSource = filteredMasters;
                if (NoMastersText != null) NoMastersText.Visibility = filteredMasters.Any() ? Visibility.Collapsed : Visibility.Visible;
                UpdateFilteredMastersCount();
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка фильтрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void UpdateMastersCount()
        {
            try
            {
                if (FilterActiveRadio == null || FilterInactiveRadio == null || FilterAllRadio == null) return;
                int total = allMasters.Count, active = allMasters.Count(m => m.IsActive), inactive = allMasters.Count(m => !m.IsActive);
                FilterActiveRadio.Content = $"Активные ({active})";
                FilterInactiveRadio.Content = $"Неактивные ({inactive})";
                FilterAllRadio.Content = $"Все ({total})";
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}"); }
        }

        private void UpdateFilteredMastersCount()
        {
            try { if (MastersCountText != null) MastersCountText.Text = $"Показано: {filteredMasters.Count} из {allMasters.Count}"; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Ошибка: {ex.Message}"); }
        }

        private void FilterRadio_Checked(object sender, RoutedEventArgs e) { if (isPageLoaded) ApplyFilter(); }
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) { if (isPageLoaded) ApplyFilter(); }
        private void BackButton_Click(object sender, RoutedEventArgs e) { AppFrame.frame.Navigate(new AdminPage()); }

        private void AddMasterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddMasterWindow();
                addWindow.Owner = Window.GetWindow(this);
                if (addWindow.ShowDialog() == true) { LoadMasters(); MessageBox.Show("Мастер успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); }
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag != null)
                {
                    var changePasswordWindow = new ChangeMasterPasswordWindow((int)button.Tag);
                    changePasswordWindow.Owner = Window.GetWindow(this);
                    if (changePasswordWindow.ShowDialog() == true)
                        MessageBox.Show("Пароль успешно изменен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async void ToggleStatusButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag != null)
                {
                    int userId = (int)button.Tag;
                    var master = allMasters.FirstOrDefault(m => m.UserId == userId);
                    if (master == null) return;

                    string action = master.IsActive ? "деактивировать" : "активировать";
                    if (MessageBox.Show($"Вы действительно хотите {action} мастера {master.FullName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        var user = AppConnect.modelBd.Users.FirstOrDefault(u => u.UserID == userId);
                        if (user != null)
                        {
                            user.IsActive = !user.IsActive;
                            await AppConnect.modelBd.SaveChangesAsync();

                            master.IsActive = user.IsActive;
                            master.StatusText = user.IsActive ? "Активен" : "Неактивен";
                            master.StatusColor = user.IsActive ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4444"));
                            master.ToggleStatusTooltip = user.IsActive ? "Деактивировать мастера" : "Активировать мастера";
                            master.ToggleStatusIcon = user.IsActive ? "🔴" : "🟢";

                            MastersList.Items.Refresh();
                            UpdateMastersCount();
                            ApplyFilter();
                            MessageBox.Show($"Мастер успешно {(user.IsActive ? "активирован" : "деактивирован")}!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}