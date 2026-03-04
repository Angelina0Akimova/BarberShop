using BarberShop.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BarberShop.Pages
{
    public partial class AddClientWindow : Window
    {
        public AddClientWindow()
        {
            InitializeComponent();
            BirthDatePicker.SelectedDate = DateTime.Today.AddYears(-18);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
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
                if (string.IsNullOrWhiteSpace(LastNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(FirstNameTextBox.Text) ||
                    string.IsNullOrWhiteSpace(PhoneTextBox.Text) ||
                    string.IsNullOrWhiteSpace(EmailTextBox.Text) || // Используем Email как логин
                    string.IsNullOrWhiteSpace(PasswordBox.Password))
                {
                    MessageBox.Show("Пожалуйста, заполните все обязательные поля (отмеченные *)",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("Пароли не совпадают",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (PasswordBox.Password.Length < 6)
                {
                    MessageBox.Show("Пароль должен содержать не менее 6 символов",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем уникальность email (используется как логин)
                if (AppConnect.modelBd.Users.Any(u => u.Email == EmailTextBox.Text))
                {
                    MessageBox.Show("Пользователь с таким email уже существует",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверяем уникальность телефона
                if (AppConnect.modelBd.Users.Any(u => u.Phone == PhoneTextBox.Text))
                {
                    MessageBox.Show("Пользователь с таким телефоном уже существует",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Создаем нового пользователя (без MiddleName и Login)
                var newUser = new Users
                {
                    FirstName = FirstNameTextBox.Text.Trim(),
                    LastName = LastNameTextBox.Text.Trim(),
                    // MiddleName нет в таблице
                    Phone = PhoneTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim(), // Email используется как логин
                    // Login отсутствует, используем Email
                    PasswordHash = PasswordBox.Password, // В реальном проекте нужно хешировать!
                    RoleID = 3, // Роль "Клиент"
                    IsActive = true,
                    RegistrationDate = DateTime.Now
                };

                AppConnect.modelBd.Users.Add(newUser);
                AppConnect.modelBd.SaveChanges();

                // Создаем запись в таблице Clients (если такая таблица существует)
                // Проверяем, есть ли таблица Clients в вашей модели
                try
                {
                    var clientType = AppConnect.modelBd.GetType().GetProperty("Clients");
                    if (clientType != null)
                    {
                        var newClient = new Clients
                        {
                            UserID = newUser.UserID,
                            BirthDate = BirthDatePicker.SelectedDate
                        };

                        // Используем динамический вызов, если Clients существует
                        var clientsSet = AppConnect.modelBd.GetType().GetProperty("Clients").GetValue(AppConnect.modelBd);
                        var addMethod = clientsSet.GetType().GetMethod("Add");
                        addMethod.Invoke(clientsSet, new object[] { newClient });

                        AppConnect.modelBd.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    // Если таблицы Clients нет, просто логируем ошибку
                    System.Diagnostics.Debug.WriteLine($"Таблица Clients не найдена: {ex.Message}");
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}