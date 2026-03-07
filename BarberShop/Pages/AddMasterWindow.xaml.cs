using BarberShop.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BarberShop.Pages
{
    public partial class AddMasterWindow : Window
    {
        public AddMasterWindow()
        {
            InitializeComponent();
            // Удаляем ссылку на PositionComboBox, которого больше нет
            // Просто оставляем видимым информационный текст
            PositionTextBlock.Visibility = Visibility.Visible;
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
                    string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
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

                // Проверяем уникальность email
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

                // Создаем нового пользователя
                var newUser = new Users
                {
                    FirstName = FirstNameTextBox.Text.Trim(),
                    LastName = LastNameTextBox.Text.Trim(),
                    Phone = PhoneTextBox.Text.Trim(),
                    Email = EmailTextBox.Text.Trim(),
                    PasswordHash = PasswordBox.Password, // В реальном проекте нужно хешировать!
                    RoleID = 2, // Роль "Мастер"
                    IsActive = true,
                    RegistrationDate = DateTime.Now
                };

                AppConnect.modelBd.Users.Add(newUser);
                AppConnect.modelBd.SaveChanges();

                // Создаем запись в таблице Employees
                var newEmployee = new Employees
                {
                    UserID = newUser.UserID,
                    HireDate = DateTime.Now, // Текущая дата как дата найма
                    Bio = $"Мастер {FirstNameTextBox.Text.Trim()}", // Краткое описание по умолчанию
                    PhotoPath = null // Пока без фото
                };

                AppConnect.modelBd.Employees.Add(newEmployee);
                AppConnect.modelBd.SaveChanges();

                MessageBox.Show("Мастер успешно добавлен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

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