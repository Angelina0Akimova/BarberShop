using BarberShop.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BarberShop.Pages
{
    public partial class EditAppointmentWindow : Window
    {
        private int appointmentId;
        private Appointments currentAppointment;

        public EditAppointmentWindow(int appointmentId)
        {
            InitializeComponent();
            this.appointmentId = appointmentId;
            LoadData();
            LoadAppointment();
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
                // Загружаем клиентов
                var clients = from client in AppConnect.modelBd.Clients
                              join user in AppConnect.modelBd.Users on client.UserID equals user.UserID
                              select new
                              {
                                  ClientID = client.ClientID,
                                  FullName = $"{user.LastName} {user.FirstName}"
                              };
                ClientComboBox.ItemsSource = clients.ToList();
                ClientComboBox.DisplayMemberPath = "FullName";
                ClientComboBox.SelectedValuePath = "ClientID";

                // Загружаем мастеров
                var employees = from emp in AppConnect.modelBd.Employees
                                join user in AppConnect.modelBd.Users on emp.UserID equals user.UserID
                                where user.RoleID == 2
                                select new
                                {
                                    EmployeeID = emp.EmployeeID,
                                    FullName = $"{user.LastName} {user.FirstName}"
                                };
                EmployeeComboBox.ItemsSource = employees.ToList();
                EmployeeComboBox.DisplayMemberPath = "FullName";
                EmployeeComboBox.SelectedValuePath = "EmployeeID";

                // Загружаем услуги
                var services = from service in AppConnect.modelBd.Services
                               where service.IsActive == true
                               select new
                               {
                                   ServiceID = service.ServiceID,
                                   ServiceName = service.ServiceName,
                                   Price = service.Price
                               };
                ServiceComboBox.ItemsSource = services.ToList();
                ServiceComboBox.DisplayMemberPath = "ServiceName";
                ServiceComboBox.SelectedValuePath = "ServiceID";

                // Загружаем статусы
                StatusComboBox.ItemsSource = AppConnect.modelBd.AppointmentStatuses.ToList();
                StatusComboBox.DisplayMemberPath = "StatusName";
                StatusComboBox.SelectedValuePath = "StatusID";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAppointment()
        {
            try
            {
                currentAppointment = AppConnect.modelBd.Appointments
                    .FirstOrDefault(a => a.AppointmentID == appointmentId);

                if (currentAppointment != null)
                {
                    ClientComboBox.SelectedValue = currentAppointment.ClientID;
                    EmployeeComboBox.SelectedValue = currentAppointment.EmployeeID;
                    ServiceComboBox.SelectedValue = currentAppointment.ServiceID;
                    AppointmentDatePicker.SelectedDate = currentAppointment.AppointmentDate;

                    // Безопасное получение времени (исправление CS0183)
                    string timeText = "10:00";
                    try
                    {
                        var timeProperty = currentAppointment.GetType().GetProperty("StartTime");
                        if (timeProperty != null)
                        {
                            var timeValue = timeProperty.GetValue(currentAppointment);

                            // Используем pattern matching (не вызывает предупреждение CS0183)
                            if (timeValue is TimeSpan timeSpan)
                                timeText = timeSpan.ToString(@"hh\:mm");
                            else if (timeValue is TimeSpan?)
                            {
                                var nullableTime = (TimeSpan?)timeValue;
                                if (nullableTime.HasValue)
                                    timeText = nullableTime.Value.ToString(@"hh\:mm");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка получения времени: {ex.Message}");
                    }

                    TimeTextBox.Text = timeText;
                    StatusComboBox.SelectedValue = currentAppointment.StatusID;

                    // Загружаем цену услуги
                    var service = AppConnect.modelBd.Services
                        .FirstOrDefault(s => s.ServiceID == currentAppointment.ServiceID);
                    if (service != null)
                    {
                        PriceTextBox.Text = $"{service.Price:0.00} ₽";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ServiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ServiceComboBox.SelectedItem != null)
                {
                    dynamic selectedService = ServiceComboBox.SelectedItem;
                    PriceTextBox.Text = $"{selectedService.Price} ₽";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                if (ClientComboBox.SelectedItem == null || EmployeeComboBox.SelectedItem == null ||
                    ServiceComboBox.SelectedItem == null || AppointmentDatePicker.SelectedDate == null ||
                    string.IsNullOrWhiteSpace(TimeTextBox.Text) || StatusComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, заполните все поля", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!TimeSpan.TryParse(TimeTextBox.Text, out TimeSpan time))
                {
                    MessageBox.Show("Неверный формат времени. Используйте ЧЧ:ММ", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (currentAppointment != null)
                {
                    currentAppointment.ClientID = (int)ClientComboBox.SelectedValue;
                    currentAppointment.EmployeeID = (int)EmployeeComboBox.SelectedValue;
                    currentAppointment.ServiceID = (int)ServiceComboBox.SelectedValue;
                    currentAppointment.AppointmentDate = AppointmentDatePicker.SelectedDate.Value;
                    currentAppointment.StartTime = time;
                    currentAppointment.StatusID = (int)StatusComboBox.SelectedValue;

                    AppConnect.modelBd.SaveChanges();
                }

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