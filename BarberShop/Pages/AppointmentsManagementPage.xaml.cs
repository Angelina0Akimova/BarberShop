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

                // Загружаем записи
                LoadAppointments();
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

                var today = DateTime.Today;
                var todayAppointments = new List<AppointmentDisplay>();
                var upcomingAppointments = new List<AppointmentDisplay>();
                var pastAppointments = new List<AppointmentDisplay>();

                foreach (var item in appointments)
                {
                    // Определяем цвет статуса
                    SolidColorBrush statusColor;
                    switch (item.Status.StatusID)
                    {
                        case 1: // Запланировано
                            statusColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                            break;
                        case 2: // Выполнено
                            statusColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFA500"));
                            break;
                        case 3: // Отменено
                            statusColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF4444"));
                            break;
                        case 4: // Не пришел
                            statusColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
                            break;
                        default:
                            statusColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
                            break;
                    }

                    // Проверяем через рефлексию, является ли свойство nullable
                    DateTime appointmentDateTime;
                    var startTimeProperty = item.Appointment.GetType().GetProperty("StartTime");
                    if (startTimeProperty != null && Nullable.GetUnderlyingType(startTimeProperty.PropertyType) != null)
                    {
                        // Свойство nullable
                        var startTimeValue = startTimeProperty.GetValue(item.Appointment) as TimeSpan?;
                        appointmentDateTime = item.Appointment.AppointmentDate.Date
                            .Add(startTimeValue ?? TimeSpan.Zero);
                    }
                    else if (startTimeProperty != null)
                    {
                        // Свойство не nullable
                        var startTimeValue = (TimeSpan)startTimeProperty.GetValue(item.Appointment);
                        appointmentDateTime = item.Appointment.AppointmentDate.Date.Add(startTimeValue);
                    }
                    else
                    {
                        appointmentDateTime = item.Appointment.AppointmentDate.Date;
                    }

                    var displayItem = new AppointmentDisplay
                    {
                        AppointmentID = item.Appointment.AppointmentID,
                        ClientName = $"{item.ClientUser.LastName} {item.ClientUser.FirstName}",
                        ClientPhone = item.ClientUser.Phone ?? "Не указан",
                        ServiceName = item.Service.ServiceName,
                        ServicePrice = item.Service.Price,
                        EmployeeName = $"{item.EmployeeUser.LastName} {item.EmployeeUser.FirstName}",
                        Date = item.Appointment.AppointmentDate.ToString("dd.MM.yyyy"),
                        Time = (item.Appointment.StartTime as TimeSpan?)?.ToString(@"hh\:mm") ?? "Не указано",
                        Price = $"{item.Service.Price:0.00} ₽",
                        StatusText = item.Status.StatusName,
                        StatusColor = statusColor,
                        AppointmentDateTime = appointmentDateTime
                    };

                    // Разделяем по категориям
                    if (appointmentDateTime.Date == today)
                    {
                        todayAppointments.Add(displayItem);
                    }
                    else if (appointmentDateTime.Date > today)
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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

                    var editWindow = new EditAppointmentWindow(appointmentId);
                    editWindow.Owner = Window.GetWindow(this);

                    if (editWindow.ShowDialog() == true)
                    {
                        LoadAppointments();
                        MessageBox.Show("Запись успешно обновлена!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
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

                    var result = MessageBox.Show("Вы уверены, что хотите удалить эту запись?",
                        "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var appointment = AppConnect.modelBd.Appointments
                            .FirstOrDefault(a => a.AppointmentID == appointmentId);

                        if (appointment != null)
                        {
                            AppConnect.modelBd.Appointments.Remove(appointment);
                            AppConnect.modelBd.SaveChanges();

                            LoadAppointments();
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

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            AppFrame.frame.Navigate(new AdminPage());
        }
    }
}