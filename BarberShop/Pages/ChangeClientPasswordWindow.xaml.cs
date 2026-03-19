using BarberShop.AppData;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace BarberShop.Pages
{
    public partial class ChangeClientPasswordWindow : Window
    {
        private int _userId;
        private Users _user;

        /// <summary>
        /// Конструктор окна смены пароля.
        /// </summary>
        /// <param name="userId">ID пользователя, чей пароль меняем.</param>
        /// <param name="isClient">Флаг, указывающий, что это клиент (для текста приветствия).</param>
        public ChangeClientPasswordWindow(int userId, bool isClient = true)
        {
            InitializeComponent();
            _userId = userId;
            LoadUserData(isClient);
        }

        private void LoadUserData(bool isClient)
        {
            try
            {
                _user = AppConnect.modelBd.Users.FirstOrDefault(u => u.UserID == _userId);
                if (_user != null)
                {
                    // Меняем текст в зависимости от того, кто меняет пароль (клиент или админ)
                    string roleText = isClient ? "Клиент" : "Пользователь";
                    ClientInfoText.Text = $"{roleText}: {_user.LastName} {_user.FirstName}";
                    TitleText.Text = $"СМЕНА ПАРОЛЯ: {_user.Email ?? _user.Phone}";
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
                // 1. Валидация: все поля заполнены?
                if (string.IsNullOrWhiteSpace(OldPasswordBox.Password) ||
                    string.IsNullOrWhiteSpace(NewPasswordBox.Password) ||
                    string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
                {
                    MessageBox.Show("Пожалуйста, заполните все поля",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. Проверка старого пароля
                if (_user.PasswordHash != OldPasswordBox.Password)
                {
                    MessageBox.Show("Неверный старый пароль",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 3. Проверка совпадения нового пароля и подтверждения
                if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    MessageBox.Show("Новый пароль и подтверждение не совпадают",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 4. Проверка длины пароля
                if (NewPasswordBox.Password.Length < 6)
                {
                    MessageBox.Show("Пароль должен содержать не менее 6 символов",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 5. Проверка, что новый пароль отличается от старого
                if (NewPasswordBox.Password == OldPasswordBox.Password)
                {
                    MessageBox.Show("Новый пароль должен отличаться от старого",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Сохраняем новый пароль
                if (_user != null)
                {
                    _user.PasswordHash = NewPasswordBox.Password; // В реальном проекте нужно хешировать!
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