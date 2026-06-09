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
        private const int DEFAULT_DURATION_MINUTES = 60;

        public class ClientItem
        {
            public int ClientID { get; set; }
            public string FullName { get; set; }
        }

        public class EmployeeItem
        {
            public int EmployeeID { get; set; }
            public string FullName { get; set; }
        }

        public class ServiceItem
        {
            public int ServiceID { get; set; }
            public string ServiceName { get; set; }
            public decimal Price { get; set; }
            public int DurationMinutes { get; set; }
        }

        public class StatusItem
        {
            public int StatusID { get; set; }
            public string StatusName { get; set; }
        }

        public AddAppointmentWindow()
        {
            InitializeComponent();
            LoadData();
            AppointmentDatePicker.SelectedDate = DateTime.Today;
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
                // Загружаем клиентов - сначала получаем данные из БД, потом форматируем в памяти
                var clientsFromDb = (from client in AppConnect.modelBd.Clients
                                     join user in AppConnect.modelBd.Users on client.UserID equals user.UserID
                                     select new
                                     {
                                         client.ClientID,
                                         user.LastName,
                                         user.FirstName
                                     }).ToList(); // Выполняем запрос здесь!

                var clients = clientsFromDb.Select(x => new ClientItem
                {
                    ClientID = x.ClientID,
                    FullName = x.LastName + " " + x.FirstName // Форматируем в памяти
                }).ToList();

                ClientComboBox.ItemsSource = clients;

                // Загружаем мастеров - сначала получаем данные из БД
                var employeesFromDb = (from emp in AppConnect.modelBd.Employees
                                       join user in AppConnect.modelBd.Users on emp.UserID equals user.UserID
                                       where user.RoleID == 2 && user.IsActive == true
                                       select new
                                       {
                                           emp.EmployeeID,
                                           user.LastName,
                                           user.FirstName
                                       }).ToList(); // Выполняем запрос здесь!

                var employees = employeesFromDb.Select(x => new EmployeeItem
                {
                    EmployeeID = x.EmployeeID,
                    FullName = x.LastName + " " + x.FirstName // Форматируем в памяти
                }).ToList();

                EmployeeComboBox.ItemsSource = employees;

                // Загружаем услуги - напрямую, без форматирования строк
                var services = AppConnect.modelBd.Services
                    .Where(s => s.IsActive == true)
                    .Select(s => new ServiceItem
                    {
                        ServiceID = s.ServiceID,
                        ServiceName = s.ServiceName,
                        Price = s.Price,
                        DurationMinutes = s.DurationMinutes
                    }).ToList();

                ServiceComboBox.ItemsSource = services;

                // Загружаем статусы
                var statuses = AppConnect.modelBd.AppointmentStatuses
                    .Select(s => new StatusItem
                    {
                        StatusID = s.StatusID,
                        StatusName = s.StatusName
                    }).ToList();

                StatusComboBox.ItemsSource = statuses;

                // Устанавливаем статус "Запланировано" (StatusID = 1)
                var plannedStatus = statuses.FirstOrDefault(s => s.StatusID == 1);
                if (plannedStatus != null)
                {
                    StatusComboBox.SelectedItem = plannedStatus;
                }
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
                if (ServiceComboBox.SelectedItem is ServiceItem selectedService)
                {
                    PriceTextBox.Text = $"{selectedService.Price:N0} ₽";
                }
                else
                {
                    PriceTextBox.Text = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TimeSpan CalculateEndTime(TimeSpan startTime)
        {
            int durationMinutes = DEFAULT_DURATION_MINUTES;

            if (ServiceComboBox.SelectedItem is ServiceItem selectedService && selectedService.DurationMinutes > 0)
            {
                durationMinutes = selectedService.DurationMinutes;
            }

            return startTime.Add(TimeSpan.FromMinutes(durationMinutes));
        }

        private bool IsTimeSlotAvailable(int employeeId, DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            var existingAppointments = AppConnect.modelBd.Appointments
                .Where(a => a.EmployeeID == employeeId
                    && a.AppointmentDate == date
                    && a.StatusID != 3)
                .ToList();

            foreach (var appointment in existingAppointments)
            {
                TimeSpan existingStart = appointment.StartTime;
                TimeSpan existingEnd = appointment.EndTime;

                if ((startTime < existingEnd) && (endTime > existingStart))
                {
                    return false;
                }
            }

            return true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка выбора клиента
                if (!(ClientComboBox.SelectedItem is ClientItem selectedClient))
                {
                    MessageBox.Show("Выберите клиента", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка выбора мастера
                if (!(EmployeeComboBox.SelectedItem is EmployeeItem selectedEmployee))
                {
                    MessageBox.Show("Выберите мастера", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка выбора услуги
                if (!(ServiceComboBox.SelectedItem is ServiceItem selectedService))
                {
                    MessageBox.Show("Выберите услугу", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка даты
                if (AppointmentDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Выберите дату", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка времени
                if (string.IsNullOrWhiteSpace(TimeTextBox.Text))
                {
                    MessageBox.Show("Введите время", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка статуса
                if (!(StatusComboBox.SelectedItem is StatusItem selectedStatus))
                {
                    MessageBox.Show("Выберите статус", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Парсим время
                if (!TimeSpan.TryParse(TimeTextBox.Text, out TimeSpan startTime))
                {
                    MessageBox.Show("Неверный формат времени. Используйте ЧЧ:ММ", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                TimeSpan endTime = CalculateEndTime(startTime);
                DateTime appointmentDate = AppointmentDatePicker.SelectedDate.Value;

                // Проверка занятости
                if (!IsTimeSlotAvailable(selectedEmployee.EmployeeID, appointmentDate, startTime, endTime))
                {
                    MessageBox.Show("Это время уже занято у выбранного мастера. Выберите другое время.",
                        "Время занято", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем запись
                var appointment = new Appointments
                {
                    ClientID = selectedClient.ClientID,
                    EmployeeID = selectedEmployee.EmployeeID,
                    ServiceID = selectedService.ServiceID,
                    StatusID = selectedStatus.StatusID,
                    AppointmentDate = appointmentDate,
                    StartTime = startTime,
                    EndTime = endTime,
                    CreatedAt = DateTime.Now,
                    Comment = null
                };

                AppConnect.modelBd.Appointments.Add(appointment);
                AppConnect.modelBd.SaveChanges();

                MessageBox.Show($"Запись успешно добавлена!\n\n" +
                    $"Клиент: {selectedClient.FullName}\n" +
                    $"Мастер: {selectedEmployee.FullName}\n" +
                    $"Услуга: {selectedService.ServiceName}\n" +
                    $"Статус: {selectedStatus.StatusName}\n" +
                    $"Дата: {appointmentDate:dd.MM.yyyy}\n" +
                    $"Время: {startTime:hh\\:mm} - {endTime:hh\\:mm}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}