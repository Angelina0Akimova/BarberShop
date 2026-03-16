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
        private int _appointmentId;
        private Appointments _currentAppointment;

        public EditAppointmentWindow(int appointmentId)
        {
            InitializeComponent();
            _appointmentId = appointmentId;
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
                var clientsQuery = from client in AppConnect.modelBd.Clients
                                   join user in AppConnect.modelBd.Users on client.UserID equals user.UserID
                                   select new
                                   {
                                       client.ClientID,
                                       user.LastName,
                                       user.FirstName
                                   };

                var clients = clientsQuery.ToList()
                    .Select(x => new
                    {
                        ClientID = x.ClientID,
                        FullName = $"{x.LastName} {x.FirstName}"
                    })
                    .ToList();

                ClientComboBox.ItemsSource = clients;
                ClientComboBox.DisplayMemberPath = "FullName";
                ClientComboBox.SelectedValuePath = "ClientID";

                // Загружаем мастеров
                var employeesQuery = from emp in AppConnect.modelBd.Employees
                                     join user in AppConnect.modelBd.Users on emp.UserID equals user.UserID
                                     where user.RoleID == 2
                                     select new
                                     {
                                         emp.EmployeeID,
                                         user.LastName,
                                         user.FirstName
                                     };

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
                _currentAppointment = AppConnect.modelBd.Appointments
                    .FirstOrDefault(a => a.AppointmentID == _appointmentId);

                if (_currentAppointment != null)
                {
                    ClientComboBox.SelectedValue = _currentAppointment.ClientID;
                    EmployeeComboBox.SelectedValue = _currentAppointment.EmployeeID;
                    ServiceComboBox.SelectedValue = _currentAppointment.ServiceID;
                    AppointmentDatePicker.SelectedDate = _currentAppointment.AppointmentDate;

                    // ИСПРАВЛЕНИЕ: StartTime - это TimeSpan, не nullable
                    // Просто форматируем TimeSpan в строку
                    TimeTextBox.Text = _currentAppointment.StartTime.ToString(@"hh\:mm");

                    StatusComboBox.SelectedValue = _currentAppointment.StatusID;

                    // Загружаем цену услуги
                    var service = AppConnect.modelBd.Services
                        .FirstOrDefault(s => s.ServiceID == _currentAppointment.ServiceID);
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
                    // Получаем цену выбранной услуги
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

                if (!TimeSpan.TryParse(TimeTextBox.Text, out TimeSpan time))
                {
                    MessageBox.Show("Неверный формат времени. Используйте ЧЧ:ММ", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_currentAppointment != null)
                {
                    _currentAppointment.ClientID = (int)ClientComboBox.SelectedValue;
                    _currentAppointment.EmployeeID = (int)EmployeeComboBox.SelectedValue;
                    _currentAppointment.ServiceID = (int)ServiceComboBox.SelectedValue;
                    _currentAppointment.AppointmentDate = AppointmentDatePicker.SelectedDate.Value;

                    // ИСПРАВЛЕНИЕ: StartTime - это TimeSpan, присваиваем напрямую
                    _currentAppointment.StartTime = time;

                    _currentAppointment.StatusID = (int)StatusComboBox.SelectedValue;

                    AppConnect.modelBd.SaveChanges();
                }

                MessageBox.Show("Запись успешно обновлена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

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