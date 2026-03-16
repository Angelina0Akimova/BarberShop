using BarberShop.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BarberShop.Pages
{
    public partial class EditAdminDataWindow : Window
    {
        private Users _currentAdmin;

        public EditAdminDataWindow()
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
                    LastNameTextBox.Text = _currentAdmin.LastName;
                    FirstNameTextBox.Text = _currentAdmin.FirstName;
                    PhoneTextBox.Text = _currentAdmin.Phone;
                    EmailTextBox.Text = _currentAdmin.Email;
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
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Введите фамилию", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("Введите имя", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                MessageBox.Show("Введите телефон", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Введите email", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Проверка уникальности email (если он изменился)
            if (_currentAdmin.Email != EmailTextBox.Text.Trim())
            {
                var existingUser = AppConnect.modelBd.Users
                    .FirstOrDefault(u => u.Email == EmailTextBox.Text.Trim() &&
                                        u.UserID != _currentAdmin.UserID);

                if (existingUser != null)
                {
                    MessageBox.Show("Пользователь с таким email уже существует", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            // Проверка уникальности телефона (если он изменился)
            if (_currentAdmin.Phone != PhoneTextBox.Text.Trim())
            {
                var existingUser = AppConnect.modelBd.Users
                    .FirstOrDefault(u => u.Phone == PhoneTextBox.Text.Trim() &&
                                        u.UserID != _currentAdmin.UserID);

                if (existingUser != null)
                {
                    MessageBox.Show("Пользователь с таким телефоном уже существует", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
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

                // Обновляем данные администратора
                _currentAdmin.LastName = LastNameTextBox.Text.Trim();
                _currentAdmin.FirstName = FirstNameTextBox.Text.Trim();
                _currentAdmin.Phone = PhoneTextBox.Text.Trim();
                _currentAdmin.Email = EmailTextBox.Text.Trim();

                AppConnect.modelBd.SaveChanges();

                MessageBox.Show("Данные успешно обновлены!", "Успех",
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