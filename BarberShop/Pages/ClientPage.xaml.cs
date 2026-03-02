using BarberShop.AppData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BarberShop.Pages
{
    public partial class ClientPage : Page
    {
        // Класс для отображения записи
        public class AppointmentDisplay
        {
            public string ServiceName { get; set; }
            public string EmployeeName { get; set; }
            public string AppointmentDate { get; set; }
            public string StartTime { get; set; }
            public string Status { get; set; }
        }

        // Класс для отображения товара
        public class ProductDisplay
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public string CategoryName { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
        }

        private Clients currentClient;

        public ClientPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем, авторизован ли пользователь
                if (AppConnect.currentUser == null)
                {
                    MessageBox.Show("Пожалуйста, авторизуйтесь", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    AppFrame.frame.Navigate(new LoginPage());
                    return;
                }

                // Загружаем данные клиента
                LoadClientData();

                // Загружаем записи клиента
                LoadAppointments();

                // Загружаем товары
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadClientData()
        {
            try
            {
                // Находим клиента по UserID
                currentClient = AppConnect.modelBd.Clients
                    .FirstOrDefault(c => c.UserID == AppConnect.currentUser.UserID);

                if (currentClient != null)
                {
                    // Заполняем данные пользователя
                    ClientNameText.Text = $"{AppConnect.currentUser.FirstName} {AppConnect.currentUser.LastName}";
                    ClientPhoneText.Text = AppConnect.currentUser.Phone;
                    ClientEmailText.Text = string.IsNullOrEmpty(AppConnect.currentUser.Email) ?
                        "Email не указан" : AppConnect.currentUser.Email;

                    // Форматируем дату рождения, если она есть
                    if (currentClient.BirthDate.HasValue)
                    {
                        ClientBirthDateText.Text = $"Дата рождения: {currentClient.BirthDate.Value:dd.MM.yyyy}";
                    }
                    else
                    {
                        ClientBirthDateText.Text = "Дата рождения не указана";
                    }

                    // Обновляем приветствие
                    WelcomeTextBlock.Text = $"Добро пожаловать, {AppConnect.currentUser.FirstName}!";
                }
                else
                {
                    // Если клиент не найден в таблице Clients (такое может быть)
                    ClientNameText.Text = $"{AppConnect.currentUser.FirstName} {AppConnect.currentUser.LastName}";
                    ClientPhoneText.Text = AppConnect.currentUser.Phone;
                    ClientEmailText.Text = AppConnect.currentUser.Email ?? "Email не указан";
                    ClientBirthDateText.Text = "Дата рождения не указана";
                    WelcomeTextBlock.Text = $"Добро пожаловать, {AppConnect.currentUser.FirstName}!";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных клиента: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAppointments()
        {
            try
            {
                if (currentClient == null) return;

                // Получаем сегодняшнюю дату для сравнения
                DateTime today = DateTime.Today;

                // Получаем все записи клиента
                var allAppointments = AppConnect.modelBd.Appointments
                    .Where(a => a.ClientID == currentClient.ClientID)
                    .Join(AppConnect.modelBd.Services,
                        a => a.ServiceID,
                        s => s.ServiceID,
                        (a, s) => new { Appointment = a, Service = s })
                    .Join(AppConnect.modelBd.Employees,
                        a => a.Appointment.EmployeeID,
                        e => e.EmployeeID,
                        (a, e) => new { a.Appointment, a.Service, Employee = e })
                    .Join(AppConnect.modelBd.Users,
                        a => a.Employee.UserID,
                        u => u.UserID,
                        (a, u) => new { a.Appointment, a.Service, a.Employee, User = u })
                    .Join(AppConnect.modelBd.AppointmentStatuses,
                        a => a.Appointment.StatusID,
                        st => st.StatusID,
                        (a, st) => new { a.Appointment, a.Service, a.User, Status = st })
                    .ToList();

                // Разделяем на предстоящие и прошедшие
                var upcoming = new List<AppointmentDisplay>();
                var past = new List<AppointmentDisplay>();

                foreach (var item in allAppointments)
                {
                    var displayItem = new AppointmentDisplay
                    {
                        ServiceName = item.Service.ServiceName,
                        EmployeeName = $"{item.User.FirstName} {item.User.LastName}",
                        AppointmentDate = item.Appointment.AppointmentDate.ToString("dd.MM.yyyy"),
                        StartTime = item.Appointment.StartTime.ToString(@"hh\:mm"),
                        Status = item.Status.StatusName
                    };

                    // Сравниваем дату записи с сегодняшней
                    if (item.Appointment.AppointmentDate >= today)
                    {
                        upcoming.Add(displayItem);
                    }
                    else
                    {
                        past.Add(displayItem);
                    }
                }

                // Отображаем записи
                UpcomingAppointmentsList.ItemsSource = upcoming;
                PastAppointmentsList.ItemsSource = past;

                // Показываем/скрываем сообщение об отсутствии записей
                NoUpcomingAppointmentsText.Visibility = upcoming.Any() ?
                    Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки записей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProducts()
        {
            try
            {
                // Получаем все товары, которые есть в наличии
                var products = AppConnect.modelBd.Products
                    .Where(p => p.Quantity > 0)
                    .Join(AppConnect.modelBd.ProductCategories,
                        p => p.CategoryID,
                        c => c.CategoryID,
                        (p, c) => new ProductDisplay
                        {
                            ProductID = p.ProductID,
                            ProductName = p.ProductName,
                            CategoryName = c.CategoryName,
                            Price = p.Price,
                            Quantity = p.Quantity
                        })
                    .Take(10) // Ограничиваем количество товаров для отображения
                    .ToList();

                // Очищаем панель
                ProductsPanel.Children.Clear();

                if (products.Any())
                {
                    foreach (var product in products)
                    {
                        // Создаем карточку товара
                        var card = CreateProductCard(product);
                        ProductsPanel.Children.Add(card);
                    }

                    NoProductsText.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NoProductsText.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                NoProductsText.Visibility = Visibility.Visible;
            }
        }

        private Border CreateProductCard(ProductDisplay product)
        {
            var border = new Border
            {
                Style = (Style)FindResource("ProductCardStyle"),
                Tag = product
            };

            var stackPanel = new StackPanel();

            // Название товара
            stackPanel.Children.Add(new TextBlock
            {
                Text = product.ProductName,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            });

            // Категория
            stackPanel.Children.Add(new TextBlock
            {
                Text = product.CategoryName,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC")),
                Margin = new Thickness(0, 0, 0, 5)
            });

            // Цена
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"{product.Price:0.00} ₽",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CE7802")),
                Margin = new Thickness(0, 10, 0, 10)
            });

            // Наличие
            stackPanel.Children.Add(new TextBlock
            {
                Text = $"В наличии: {product.Quantity} шт.",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                Margin = new Thickness(0, 0, 0, 15)
            });

            // Кнопка "Купить"
            var buyButton = new Button
            {
                Content = "Купить",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CE7802")),
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Height = 40,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            buyButton.Click += BuyButton_Click;

            stackPanel.Children.Add(buyButton);
            border.Child = stackPanel;

            return border;
        }

        private void BuyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var border = button?.Parent as StackPanel;
                var card = border?.Parent as Border;
                var product = card?.Tag as ProductDisplay;

                if (product != null)
                {
                    MessageBox.Show($"Товар '{product.ProductName}' добавлен в корзину!\n\n" +
                                   $"Цена: {product.Price:0.00} ₽\n" +
                                   "Для оформления заказа обратитесь к администратору.",
                                   "Информация",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Спрашиваем подтверждение
                var result = MessageBox.Show("Вы действительно хотите выйти?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Очищаем данные текущего пользователя
                    AppConnect.currentUser = null;

                    // Переходим на страницу входа
                    AppFrame.frame.Navigate(new LoginPage());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}