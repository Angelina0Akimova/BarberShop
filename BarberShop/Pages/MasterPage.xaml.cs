using BarberShop.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BarberShop.Pages
{
    public partial class MasterPage : Page
    {
        // Класс для отображения записи (общий для актуальных и прошедших)
        public class AppointmentDisplay
        {
            public int AppointmentID { get; set; }
            public string ClientName { get; set; }
            public string ClientPhone { get; set; }
            public string ServiceName { get; set; }
            public decimal ServicePrice { get; set; }
            public string Date { get; set; }
            public string Time { get; set; }
            public string Price { get; set; }
            public string StatusText { get; set; }
            public SolidColorBrush StatusColor { get; set; }
            public DateTime AppointmentDateTime { get; set; }
            public int StatusId { get; set; }
        }

        private Employees currentEmployee;
        private Users currentUser;

        public MasterPage()
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

                // Проверяем роль мастера (RoleID = 2)
                if (AppConnect.currentUser.RoleID != 2)
                {
                    ShowErrorAndNavigate("У вас нет прав доступа к этой странице");
                    return;
                }

                currentUser = AppConnect.currentUser;

                // Загружаем данные мастера из таблицы Employees
                LoadMasterData();

                // Обновляем статусы прошедших записей
                UpdatePastAppointmentsStatus();

                // Загружаем записи мастера
                LoadAppointments();

                // Загружаем статистику за текущий период (по умолчанию неделя)
                LoadStatistics(DateTime.Now.AddDays(-7), DateTime.Now);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowErrorAndNavigate(string message)
        {
            MessageBox.Show(message, "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            AppFrame.frame.Navigate(new LoginPage());
        }

        private void LoadMasterData()
        {
            try
            {
                // Находим запись мастера в таблице Employees по UserID
                currentEmployee = AppConnect.modelBd.Employees
                    .FirstOrDefault(e => e.UserID == currentUser.UserID);

                // Заполняем данные пользователя
                MasterNameText.Text = $"{currentUser.FirstName} {currentUser.LastName}";
                MasterPhoneText.Text = currentUser.Phone;
                MasterEmailText.Text = string.IsNullOrEmpty(currentUser.Email) ? "Email не указан" : currentUser.Email;

                // Заполняем данные из Employees
                if (currentEmployee != null)
                {
                    MasterHireDateText.Text = currentEmployee.HireDate.ToString("dd.MM.yyyy");
                    MasterBioText.Text = string.IsNullOrEmpty(currentEmployee.Bio) ? "Нет информации" : currentEmployee.Bio;
                }
                else
                {
                    // Если по какой-то причине записи в Employees нет
                    MasterHireDateText.Text = "Дата не указана";
                    MasterBioText.Text = "Нет информации";
                }

                // Обновляем приветствие
                WelcomeTextBlock.Text = $"Добро пожаловать, {currentUser.FirstName}!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных мастера: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обновляет статусы прошедших записей с "Запланировано" на "Выполнено"
        /// </summary>
        private void UpdatePastAppointmentsStatus()
        {
            try
            {
                if (currentEmployee == null) return;

                var currentDateTime = DateTime.Now;
                var scheduledStatusId = 1; // ID статуса "Запланировано"
                var completedStatusId = 2; // ID статуса "Выполнено"

                // Находим все запланированные записи текущего мастера
                var pastScheduledAppointments = AppConnect.modelBd.Appointments
                    .Where(a => a.EmployeeID == currentEmployee.EmployeeID && a.StatusID == scheduledStatusId)
                    .ToList() // Загружаем в память для дальнейших вычислений
                    .Where(a => {
                        // Создаем DateTime окончания записи: дата + время окончания
                        DateTime endDateTime = a.AppointmentDate.Date.Add(a.EndTime);
                        // Сравниваем с текущим временем
                        return endDateTime < currentDateTime;
                    })
                    .ToList();

                if (pastScheduledAppointments.Any())
                {
                    foreach (var appointment in pastScheduledAppointments)
                    {
                        appointment.StatusID = completedStatusId;
                    }

                    AppConnect.modelBd.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении статусов: {ex.Message}");
            }
        }

        private void LoadAppointments()
        {
            try
            {
                if (currentEmployee == null) return;

                // Получаем все записи мастера с объединением необходимых таблиц
                var appointments = from a in AppConnect.modelBd.Appointments
                                   join c in AppConnect.modelBd.Clients on a.ClientID equals c.ClientID
                                   join cu in AppConnect.modelBd.Users on c.UserID equals cu.UserID
                                   join s in AppConnect.modelBd.Services on a.ServiceID equals s.ServiceID
                                   join st in AppConnect.modelBd.AppointmentStatuses on a.StatusID equals st.StatusID
                                   where a.EmployeeID == currentEmployee.EmployeeID
                                   select new
                                   {
                                       Appointment = a,
                                       ClientUser = cu,
                                       Service = s,
                                       Status = st
                                   };

                var appointmentsList = appointments.ToList();

                var today = DateTime.Today;
                var upcomingAppointments = new List<AppointmentDisplay>();
                var pastAppointments = new List<AppointmentDisplay>();

                foreach (var item in appointmentsList)
                {
                    // Определяем цвет статуса (для прошедших)
                    SolidColorBrush statusColor = GetStatusColor(item.Status.StatusID);

                    // Формируем дату и время окончания записи
                    DateTime appointmentEndDateTime = item.Appointment.AppointmentDate.Date.Add(item.Appointment.EndTime);

                    var displayItem = new AppointmentDisplay
                    {
                        AppointmentID = item.Appointment.AppointmentID,
                        ClientName = $"{item.ClientUser.LastName} {item.ClientUser.FirstName}",
                        ClientPhone = item.ClientUser.Phone ?? "Не указан",
                        ServiceName = item.Service.ServiceName,
                        ServicePrice = item.Service.Price,
                        Date = item.Appointment.AppointmentDate.ToString("dd.MM.yyyy"),
                        Time = $"{item.Appointment.StartTime.ToString(@"hh\:mm")} - {item.Appointment.EndTime.ToString(@"hh\:mm")}",
                        Price = $"{item.Service.Price:0.00} ₽",
                        StatusText = item.Status.StatusName,
                        StatusColor = statusColor,
                        AppointmentDateTime = appointmentEndDateTime,
                        StatusId = item.Status.StatusID
                    };

                    // Разделяем на актуальные (статус "Запланировано") и прошедшие (все остальные)
                    if (item.Status.StatusID == 1) // Запланировано
                    {
                        upcomingAppointments.Add(displayItem);
                    }
                    else
                    {
                        pastAppointments.Add(displayItem);
                    }
                }

                // Сортируем
                upcomingAppointments = upcomingAppointments.OrderBy(a => a.AppointmentDateTime).ToList();
                pastAppointments = pastAppointments.OrderByDescending(a => a.AppointmentDateTime).ToList();

                // Отображаем
                UpcomingAppointmentsList.ItemsSource = upcomingAppointments;
                PastAppointmentsList.ItemsSource = pastAppointments;

                // Показываем/скрываем сообщения
                NoUpcomingAppointmentsText.Visibility = upcomingAppointments.Any() ? Visibility.Collapsed : Visibility.Visible;
                NoPastAppointmentsText.Visibility = pastAppointments.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Загружает статистику за указанный период
        /// </summary>
        private void LoadStatistics(DateTime startDate, DateTime endDate)
        {
            try
            {
                if (currentEmployee == null) return;

                // Устанавливаем конец периода на конец дня
                DateTime periodEnd = endDate.Date.AddDays(1).AddSeconds(-1);

                // Получаем все выполненные записи мастера за период
                var completedAppointments = AppConnect.modelBd.Appointments
                    .Where(a => a.EmployeeID == currentEmployee.EmployeeID &&
                                a.StatusID == 2 && // Статус "Выполнено"
                                a.AppointmentDate >= startDate.Date &&
                                a.AppointmentDate <= endDate.Date)
                    .ToList();

                // Количество уникальных клиентов
                int uniqueClientsCount = completedAppointments
                    .Select(a => a.ClientID)
                    .Distinct()
                    .Count();

                // Сумма выручки (суммируем цену услуг из связанной таблицы Services)
                decimal totalRevenue = 0;
                foreach (var appointment in completedAppointments)
                {
                    var service = AppConnect.modelBd.Services.FirstOrDefault(s => s.ServiceID == appointment.ServiceID);
                    if (service != null)
                    {
                        totalRevenue += service.Price;
                    }
                }

                // Обновляем UI
                StatsClientsCountText.Text = uniqueClientsCount.ToString();
                StatsRevenueText.Text = $"{totalRevenue:0.00} ₽";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Возвращает цвет для статуса записи
        /// </summary>
        private SolidColorBrush GetStatusColor(int statusId)
        {
            switch (statusId)
            {
                case 2: // Выполнено
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA500"));
                case 3: // Отменено
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4444"));
                case 4: // Не пришел
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
                default:
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
            }
        }

        /// <summary>
        /// Обработчик добавления новой записи
        /// </summary>
        private void AddAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddAppointmentWindow();
                addWindow.Owner = Window.GetWindow(this);

                if (addWindow.ShowDialog() == true)
                {
                    // После добавления перезагружаем записи
                    UpdatePastAppointmentsStatus();
                    LoadAppointments();
                    // Также обновляем статистику
                    PeriodRadio_Checked(null, null);

                    MessageBox.Show("Запись успешно добавлена!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении записи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PeriodRadio_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime endDate = DateTime.Now;
                DateTime startDate;

                if (PeriodWeekRadio.IsChecked == true)
                {
                    // Последние 7 дней
                    startDate = endDate.AddDays(-7);
                }
                else if (PeriodMonthRadio.IsChecked == true)
                {
                    // Последние 30 дней
                    startDate = endDate.AddDays(-30);
                }
                else
                {
                    return;
                }

                LoadStatistics(startDate, endDate);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при смене периода: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag != null)
                {
                    int appointmentId = (int)button.Tag;

                    var editWindow = new EditAppointmentWindow(appointmentId);
                    editWindow.Owner = Window.GetWindow(this);

                    if (editWindow.ShowDialog() == true)
                    {
                        // После редактирования снова проверяем статусы и перезагружаем записи
                        UpdatePastAppointmentsStatus();
                        LoadAppointments();
                        // Также обновляем статистику, так как мог измениться статус записи
                        PeriodRadio_Checked(null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag != null)
                {
                    int appointmentId = (int)button.Tag;

                    var result = MessageBox.Show("Вы уверены, что хотите удалить эту запись?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var appointment = AppConnect.modelBd.Appointments
                            .FirstOrDefault(a => a.AppointmentID == appointmentId);

                        if (appointment != null)
                        {
                            // Проверяем, есть ли связанные платежи
                            var relatedPayments = AppConnect.modelBd.Payments
                                .Where(p => p.AppointmentID == appointmentId).ToList();

                            if (relatedPayments.Any())
                            {
                                AppConnect.modelBd.Payments.RemoveRange(relatedPayments);
                            }

                            AppConnect.modelBd.Appointments.Remove(appointment);
                            AppConnect.modelBd.SaveChanges();

                            // Перезагружаем списки
                            LoadAppointments();
                            // Обновляем статистику
                            PeriodRadio_Checked(null, null);

                            MessageBox.Show("Запись успешно удалена!", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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