using BarberShop.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BarberShop.Pages
{
    public partial class ChangeClientPasswordWindow : Window
    {
        private int userId;
        private Users user;

        public ChangeClientPasswordWindow(int userId)
        {
            InitializeComponent();
            this.userId = userId;
            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                user = AppConnect.modelBd.Users.FirstOrDefault(u => u.UserID == userId);
                if (user != null)
                {
                    // Убираем MiddleName, так как его нет в таблице
                    ClientInfoText.Text = $"Клиент: {user.LastName} {user.FirstName}";
                    TitleText.Text = $"СМЕНА ПАРОЛЯ: {user.Email}"; // Используем Email как логин
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                if (string.IsNullOrWhiteSpace(NewPasswordBox.Password) ||
                    string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("Пароли не совпадают",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (NewPasswordBox.Password.Length < 6)
                {
                    MessageBox.Show("Пароль должен содержать не менее 6 символов",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Сохраняем новый пароль
                if (user != null)
                {
                    user.PasswordHash = NewPasswordBox.Password; // В реальном проекте нужно хешировать!
                    AppConnect.modelBd.SaveChanges();

                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}