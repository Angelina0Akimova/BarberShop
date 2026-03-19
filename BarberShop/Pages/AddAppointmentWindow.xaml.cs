using BarberShop.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BarberShop.Pages
{
    public partial class AddAppointmentWindow : Window
    {
        // Константа для длительности услуги (1 час = 60 минут)
        private const int DEFAULT_DURATION_MINUTES = 60;

        public AddAppointmentWindow()
        {
            InitializeComponent();
            LoadData();
            AppointmentDatePicker.SelectedDate = DateTime.Today;

            // Устанавливаем время по умолчанию
            TimeTextBox.Text = DateTime.Now.ToString("HH:mm");
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void LoadData()
        {
            try
            {
                // Загружаем клиентов (объединяем Users и Clients)
                var clientsQuery = from client in AppConnect.modelBd.Clients
                                   join user in AppConnect.modelBd.Users on client.UserID equals user.UserID
                                   select new
                                   {
                                       client.ClientID,
                                       user.LastName,
                                       user.FirstName,
                                       user.Phone
                                   };

                // Выполняем запрос к БД, затем формируем FullName в памяти
                var clients = clientsQuery.ToList()
                    .Select(x => new
                    {
                        ClientID = x.ClientID,
                        FullName = $"{x.LastName} {x.FirstName}",
                        Phone = x.Phone
                    })
                    .ToList();

                ClientComboBox.ItemsSource = clients;
                ClientComboBox.DisplayMemberPath = "FullName";
                ClientComboBox.SelectedValuePath = "ClientID";

                // Загружаем мастеров (объединяем Employees и Users)
                var employeesQuery = from emp in AppConnect.modelBd.Employees
                                     join user in AppConnect.modelBd.Users on emp.UserID equals user.UserID
                                     where user.RoleID == 2 // Мастер
                                     select new
                                     {
                                         emp.EmployeeID,
                                         user.LastName,
                                         user.FirstName
                                     };

                // Выполняем запрос к БД, затем формируем FullName в памяти
                var employees = employeesQuery.ToList()
                    .Select(x => new
                    {
                        EmployeeID = x.EmployeeID,
                        FullName = $"{x.LastName} {x.FirstName}"
                    })
                    .ToList();

                EmployeeComboBox.ItemsSource = employees;
                EmployeeComboBox.DisplayMemberPath = "FullName";
                EmployeeComboBox.SelectedValuePath = "EmployeeID";

                // Загружаем услуги
                var services = from service in AppConnect.modelBd.Services
                               where service.IsActive == true
                               select new
                               {
                                   ServiceID = service.ServiceID,
                                   ServiceName = service.ServiceName,
                                   Price = service.Price,
                                   DurationMinutes = service.DurationMinutes
                               };
                ServiceComboBox.ItemsSource = services.ToList();
                ServiceComboBox.DisplayMemberPath = "ServiceName";
                ServiceComboBox.SelectedValuePath = "ServiceID";

                // Загружаем статусы
                StatusComboBox.ItemsSource = AppConnect.modelBd.AppointmentStatuses.ToList();
                StatusComboBox.DisplayMemberPath = "StatusName";
                StatusComboBox.SelectedValuePath = "StatusID";
                StatusComboBox.SelectedIndex = 0; // По умолчанию первый статус
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ServiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ServiceComboBox.SelectedItem != null)
                {
                    // Безопасное получение цены
                    var selectedItem = ServiceComboBox.SelectedItem;
                    var priceProperty = selectedItem.GetType().GetProperty("Price");
                    if (priceProperty != null)
                    {
                        decimal price = (decimal)priceProperty.GetValue(selectedItem);
                        PriceTextBox.Text = $"{price} ₽";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Вычисляет время окончания записи на основе времени начала и длительности
        /// </summary>
        private TimeSpan CalculateEndTime(TimeSpan startTime)
        {
            // Добавляем 1 час (60 минут) к времени начала
            return startTime.Add(TimeSpan.FromMinutes(DEFAULT_DURATION_MINUTES));
        }

        /// <summary>
        /// Проверяет, не пересекается ли новая запись с существующими
        /// </summary>
        private bool IsTimeSlotAvailable(int employeeId, DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            var existingAppointments = AppConnect.modelBd.Appointments
                .Where(a => a.EmployeeID == employeeId
                    && a.AppointmentDate == date
                    && a.StatusID != 3) // Исключаем отмененные записи (StatusID = 3)
                .ToList();

            foreach (var appointment in existingAppointments)
            {
                TimeSpan existingStart = appointment.StartTime;
                TimeSpan existingEnd = appointment.EndTime;

                // Проверяем пересечение временных интервалов
                if ((startTime < existingEnd) && (endTime > existingStart))
                {
                    return false; // Время занято
                }
            }

            return true; // Время свободно
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация
                if (ClientComboBox.SelectedItem == null ||
                    EmployeeComboBox.SelectedItem == null ||
                    ServiceComboBox.SelectedItem == null ||
                    AppointmentDatePicker.SelectedDate == null ||
                    string.IsNullOrWhiteSpace(TimeTextBox.Text) ||
                    StatusComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Парсим время начала
                if (!TimeSpan.TryParse(TimeTextBox.Text, out TimeSpan startTime))
                {
                    MessageBox.Show("Неверный формат времени. Используйте ЧЧ:ММ", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Вычисляем время окончания (начало + 1 час)
                TimeSpan endTime = CalculateEndTime(startTime);

                // Получаем выбранные значения
                int clientId = (int)ClientComboBox.SelectedValue;
                int employeeId = (int)EmployeeComboBox.SelectedValue;
                int serviceId = (int)ServiceComboBox.SelectedValue;
                int statusId = (int)StatusComboBox.SelectedValue;
                DateTime appointmentDate = AppointmentDatePicker.SelectedDate.Value;

                // Проверяем, не занято ли это время у мастера
                if (!IsTimeSlotAvailable(employeeId, appointmentDate, startTime, endTime))
                {
                    MessageBox.Show($"Это время уже занято у выбранного мастера. Выберите другое время.",
                        "Время занято", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем новую запись
                var appointment = new Appointments
                {
                    ClientID = clientId,
                    EmployeeID = employeeId,
                    ServiceID = serviceId,
                    StatusID = statusId,
                    AppointmentDate = appointmentDate,
                    StartTime = startTime,
                    EndTime = endTime, // Устанавливаем время окончания (+1 час)
                    CreatedAt = DateTime.Now,
                    Comment = null // Можно добавить поле для комментария в интерфейсе при необходимости
                };

                AppConnect.modelBd.Appointments.Add(appointment);
                AppConnect.modelBd.SaveChanges();

                MessageBox.Show("Запись успешно добавлена!\n" +
                    $"Время: {startTime.ToString(@"hh\:mm")} - {endTime.ToString(@"hh\:mm")} (1 час)",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}