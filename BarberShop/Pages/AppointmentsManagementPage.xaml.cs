using BarberShop.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BarberShop.Pages
{
    public partial class AppointmentsManagementPage : Page
    {
        // Класс для отображения записи
        public class AppointmentDisplay
        {
            public int AppointmentID { get; set; }
            public string ClientName { get; set; }
            public string ClientPhone { get; set; }
            public string ServiceName { get; set; }
            public decimal ServicePrice { get; set; }
            public string EmployeeName { get; set; }
            public string Date { get; set; }
            public string Time { get; set; }
            public string Price { get; set; }
            public string StatusText { get; set; }
            public SolidColorBrush StatusColor { get; set; }
            public DateTime AppointmentDateTime { get; set; }
            public int StatusId { get; set; } // Добавляем ID статуса для проверки
        }

        public AppointmentsManagementPage()
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

                // Проверяем роль администратора
                if (AppConnect.currentUser.RoleID != 1)
                {
                    ShowErrorAndGoBack("У вас нет прав доступа к этой странице");
                    return;
                }

                // Отображаем информацию об администраторе
                AdminInfoText.Text = $"Администратор: {AppConnect.currentUser.LastName} {AppConnect.currentUser.FirstName}";

                // Обновляем статусы прошедших записей
                UpdatePastAppointmentsStatus();

                // Загружаем записи
                LoadAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы: {ex.Message}", "Ошибка",
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
                var currentDateTime = DateTime.Now;
                var scheduledStatusId = 1; // ID статуса "Запланировано"
                var completedStatusId = 2; // ID статуса "Выполнено"

                // Находим все запланированные записи
                var pastScheduledAppointments = AppConnect.modelBd.Appointments
                    .Where(a => a.StatusID == scheduledStatusId)
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

                    System.Diagnostics.Debug.WriteLine($"Обновлено статусов: {pastScheduledAppointments.Count}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при обновлении статусов: {ex.Message}");
                // Не показываем сообщение пользователю, чтобы не прерывать загрузку страницы
            }
        }

        private void ShowErrorAndGoBack(string message)
        {
            MessageBox.Show(message, "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            AppFrame.frame.Navigate(new AdminPage());
        }

        private void LoadAppointments()
        {
            try
            {
                // Получаем все записи с объединением необходимых таблиц
                var appointments = from a in AppConnect.modelBd.Appointments
                                   join c in AppConnect.modelBd.Clients on a.ClientID equals c.ClientID
                                   join cu in AppConnect.modelBd.Users on c.UserID equals cu.UserID
                                   join e in AppConnect.modelBd.Employees on a.EmployeeID equals e.EmployeeID
                                   join eu in AppConnect.modelBd.Users on e.UserID equals eu.UserID
                                   join s in AppConnect.modelBd.Services on a.ServiceID equals s.ServiceID
                                   join st in AppConnect.modelBd.AppointmentStatuses on a.StatusID equals st.StatusID
                                   select new
                                   {
                                       Appointment = a,
                                       ClientUser = cu,
                                       EmployeeUser = eu,
                                       Service = s,
                                       Status = st
                                   };

                var appointmentsList = appointments.ToList();

                var today = DateTime.Today;
                var todayAppointments = new List<AppointmentDisplay>();
                var upcomingAppointments = new List<AppointmentDisplay>();
                var pastAppointments = new List<AppointmentDisplay>();

                foreach (var item in appointmentsList)
                {
                    // Определяем цвет статуса
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
                        EmployeeName = $"{item.EmployeeUser.LastName} {item.EmployeeUser.FirstName}",
                        Date = item.Appointment.AppointmentDate.ToString("dd.MM.yyyy"),
                        Time = $"{item.Appointment.StartTime.ToString(@"hh\:mm")} - {item.Appointment.EndTime.ToString(@"hh\:mm")}",
                        Price = $"{item.Service.Price:0.00} ₽",
                        StatusText = item.Status.StatusName,
                        StatusColor = statusColor,
                        AppointmentDateTime = appointmentEndDateTime,
                        StatusId = item.Status.StatusID
                    };

                    // Разделяем по категориям
                    if (appointmentEndDateTime.Date == today)
                    {
                        todayAppointments.Add(displayItem);
                    }
                    else if (appointmentEndDateTime.Date > today)
                    {
                        upcomingAppointments.Add(displayItem);
                    }
                    else
                    {
                        pastAppointments.Add(displayItem);
                    }
                }

                // Сортируем
                todayAppointments = todayAppointments.OrderBy(a => a.AppointmentDateTime).ToList();
                upcomingAppointments = upcomingAppointments.OrderBy(a => a.AppointmentDateTime).ToList();
                pastAppointments = pastAppointments.OrderByDescending(a => a.AppointmentDateTime).ToList();

                // Отображаем
                TodayAppointmentsList.ItemsSource = todayAppointments;
                UpcomingAppointmentsList.ItemsSource = upcomingAppointments;
                PastAppointmentsList.ItemsSource = pastAppointments;

                // Показываем/скрываем сообщения
                NoTodayAppointmentsText.Visibility = todayAppointments.Any() ? Visibility.Collapsed : Visibility.Visible;
                NoUpcomingAppointmentsText.Visibility = upcomingAppointments.Any() ? Visibility.Collapsed : Visibility.Visible;
                NoPastAppointmentsText.Visibility = pastAppointments.Any() ? Visibility.Collapsed : Visibility.Visible;

                // Отладка
                System.Diagnostics.Debug.WriteLine($"Загружено записей: всего {appointmentsList.Count}, сегодня {todayAppointments.Count}, предстоит {upcomingAppointments.Count}, прошло {pastAppointments.Count}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}", "Ошибка",
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
                case 1: // Запланировано
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
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

        private void AddAppointmentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddAppointmentWindow();
                addWindow.Owner = Window.GetWindow(this);

                if (addWindow.ShowDialog() == true)
                {
                    LoadAppointments();
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

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag != null)
                {
                    int appointmentId = (int)button.Tag;
                    System.Diagnostics.Debug.WriteLine($"Редактирование записи с ID: {appointmentId}");

                    var editWindow = new EditAppointmentWindow(appointmentId);
                    editWindow.Owner = Window.GetWindow(this);

                    if (editWindow.ShowDialog() == true)
                    {
                        // После редактирования снова проверяем статусы
                        UpdatePastAppointmentsStatus();
                        LoadAppointments();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Tag кнопки редактирования равен null");
                    MessageBox.Show("Не удалось определить ID записи", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button?.Tag != null)
                {
                    int appointmentId = (int)button.Tag;
                    System.Diagnostics.Debug.WriteLine($"Удаление записи с ID: {appointmentId}");

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
                                var paymentResult = MessageBox.Show(
                                    "У этой записи есть связанные платежи. При удалении записи платежи также будут удалены. Продолжить?",
                                    "Подтверждение удаления",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning);

                                if (paymentResult == MessageBoxResult.Yes)
                                {
                                    AppConnect.modelBd.Payments.RemoveRange(relatedPayments);
                                }
                                else
                                {
                                    return;
                                }
                            }

                            AppConnect.modelBd.Appointments.Remove(appointment);
                            AppConnect.modelBd.SaveChanges();

                            // Перезагружаем список записей
                            LoadAppointments();

                            MessageBox.Show("Запись успешно удалена!", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Запись не найдена в базе данных", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Tag кнопки удаления равен null");
                    MessageBox.Show("Не удалось определить ID записи", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            AppFrame.frame.Navigate(new AdminPage());
        }
    }
}