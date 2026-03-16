using BarberShop.AppData;
using System;
using System.Windows;
using System.Windows.Input;

namespace BarberShop.Pages
{
    public partial class ChangeAdminPasswordWindow : Window
    {
        private Users _currentAdmin;

        public ChangeAdminPasswordWindow()
        {
            InitializeComponent();
            LoadAdminData();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void LoadAdminData()
        {
            try
            {
                _currentAdmin = AppConnect.currentUser;

                if (_currentAdmin != null)
                {
                    AdminInfoText.Text = $"Администратор: {_currentAdmin.LastName} {_currentAdmin.FirstName} ({_currentAdmin.Email})";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInput()
        {
            // Проверка старого пароля
            if (string.IsNullOrWhiteSpace(OldPasswordBox.Password))
            {
                MessageBox.Show("Введите старый пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка нового пароля
            if (string.IsNullOrWhiteSpace(NewPasswordBox.Password))
            {
                MessageBox.Show("Введите новый пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка подтверждения
            if (string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
            {
                MessageBox.Show("Подтвердите новый пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка совпадения старого пароля
            if (_currentAdmin.PasswordHash != OldPasswordBox.Password)
            {
                MessageBox.Show("Неверный старый пароль", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка совпадения нового пароля и подтверждения
            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Новый пароль и подтверждение не совпадают", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка длины пароля
            if (NewPasswordBox.Password.Length < 6)
            {
                MessageBox.Show("Пароль должен содержать не менее 6 символов", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка, что новый пароль отличается от старого
            if (NewPasswordBox.Password == OldPasswordBox.Password)
            {
                MessageBox.Show("Новый пароль должен отличаться от старого", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
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
                if (!ValidateInput())
                    return;

                // Сохраняем новый пароль
                _currentAdmin.PasswordHash = NewPasswordBox.Password;
                AppConnect.modelBd.SaveChanges();

                MessageBox.Show("Пароль успешно изменен!", "Успех",
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