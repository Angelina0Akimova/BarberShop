using BarberShop.AppData;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BarberShop.Pages
{
    public partial class ReportsPage : Page
    {
        // Класс для хранения данных по услугам
        public class ServiceRevenue
        {
            public string ServiceName { get; set; }
            public int AppointmentCount { get; set; }
            public decimal TotalRevenue { get; set; }
        }

        // Класс для хранения статистики по статусам
        public class AppointmentStats
        {
            public int Total { get; set; }
            public int Completed { get; set; }
            public int Scheduled { get; set; }
            public int Cancelled { get; set; }
        }

        private List<ServiceRevenue> servicesRevenue = new List<ServiceRevenue>();

        public ReportsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем авторизацию
                if (AppConnect.currentUser == null)
                {
                    ShowErrorAndGoBack("Пожалуйста, авторизуйтесь");
                    return;
                }

                // Проверяем роль администратора (ID роли администратора = 1)
                if (AppConnect.currentUser.RoleID != 1)
                {
                    ShowErrorAndGoBack("У вас нет прав доступа к этой странице");
                    return;
                }

                // Отображаем информацию об администраторе
                AdminInfoText.Text = $"Администратор: {AppConnect.currentUser.LastName} {AppConnect.currentUser.FirstName}";

                // Устанавливаем даты по умолчанию (текущий месяц)
                var today = DateTime.Today;
                StartDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
                EndDatePicker.SelectedDate = today;

                // Загружаем отчеты
                LoadReports();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки страницы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowErrorAndGoBack(string message)
        {
            MessageBox.Show(message, "Ошибка",
                MessageBoxButton.OK, MessageBoxImage.Warning);

            // Возвращаемся на страницу администратора, так как пользователь уже авторизован
            AppFrame.frame.Navigate(new AdminPage());
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadReports();
        }

        private void LoadReports()
        {
            try
            {
                if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                    return;

                DateTime startDate = StartDatePicker.SelectedDate.Value.Date;
                DateTime endDate = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1); // Конец дня

                // Получаем все завершенные записи (статус "Выполнено" - ID = 2)
                var completedAppointments = AppConnect.modelBd.Appointments
                    .Where(a => a.AppointmentDate >= startDate &&
                               a.AppointmentDate <= endDate &&
                               a.StatusID == 2) // 2 = "Выполнено"
                    .Join(AppConnect.modelBd.Services,
                        a => a.ServiceID,
                        s => s.ServiceID,
                        (a, s) => new { Appointment = a, Service = s })
                    .ToList();

                // Общая выручка
                decimal totalRevenue = completedAppointments.Sum(x => x.Service.Price);
                TotalRevenueText.Text = $"{totalRevenue:N0} ₽";
                CompletedAppointmentsCountText.Text = completedAppointments.Count.ToString();

                // Выручка по услугам
                servicesRevenue = completedAppointments
                    .GroupBy(x => x.Service.ServiceName)
                    .Select(g => new ServiceRevenue
                    {
                        ServiceName = g.Key,
                        AppointmentCount = g.Count(),
                        TotalRevenue = g.Sum(x => x.Service.Price)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .ToList();

                // Привязываем данные к ItemsControl
                ServicesRevenueList.ItemsSource = servicesRevenue;

                // Итого по услугам
                TotalServicesCountText.Text = servicesRevenue.Sum(x => x.AppointmentCount).ToString();
                TotalServicesRevenueText.Text = $"{servicesRevenue.Sum(x => x.TotalRevenue):N0} ₽";

                // Статистика по статусам (все записи за период)
                var allAppointments = AppConnect.modelBd.Appointments
                    .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
                    .ToList();

                int total = allAppointments.Count;
                int completed = allAppointments.Count(a => a.StatusID == 2);
                int scheduled = allAppointments.Count(a => a.StatusID == 1);
                int cancelled = allAppointments.Count(a => a.StatusID == 3 || a.StatusID == 4);

                TotalAppointmentsText.Text = total.ToString();
                CompletedAppointmentsText.Text = completed.ToString();
                ScheduledAppointmentsText.Text = scheduled.ToString();
                CancelledAppointmentsText.Text = cancelled.ToString();

                // Дополнительная статистика
                // Средний чек
                if (completedAppointments.Any())
                {
                    decimal averageBill = totalRevenue / completedAppointments.Count;
                    AverageBillText.Text = $"{averageBill:N0} ₽";
                }
                else
                {
                    AverageBillText.Text = "0 ₽";
                }

                // Самый популярный мастер
                var topMaster = completedAppointments
                    .GroupBy(x => x.Appointment.EmployeeID)
                    .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefault();

                if (topMaster != null)
                {
                    var employee = AppConnect.modelBd.Employees.FirstOrDefault(e => e.EmployeeID == topMaster.EmployeeId);
                    if (employee != null)
                    {
                        var user = AppConnect.modelBd.Users.FirstOrDefault(u => u.UserID == employee.UserID);
                        if (user != null)
                        {
                            TopMasterText.Text = $"{user.LastName} {user.FirstName}";
                            TopMasterCountText.Text = $"{topMaster.Count} {(GetCorrectWord(topMaster.Count, "запись", "записи", "записей"))}";
                        }
                    }
                }
                else
                {
                    TopMasterText.Text = "-";
                    TopMasterCountText.Text = "0 записей";
                }

                // Самая популярная услуга
                var topService = servicesRevenue.FirstOrDefault();
                if (topService != null)
                {
                    TopServiceText.Text = topService.ServiceName;
                    TopServiceCountText.Text = $"{topService.AppointmentCount} {(GetCorrectWord(topService.AppointmentCount, "запись", "записи", "записей"))}";
                }
                else
                {
                    TopServiceText.Text = "-";
                    TopServiceCountText.Text = "0 записей";
                }

                // Самый активный клиент
                var topClient = completedAppointments
                    .GroupBy(x => x.Appointment.ClientID)
                    .Select(g => new { ClientId = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .FirstOrDefault();

                if (topClient != null)
                {
                    var client = AppConnect.modelBd.Clients.FirstOrDefault(c => c.ClientID == topClient.ClientId);
                    if (client != null)
                    {
                        var user = AppConnect.modelBd.Users.FirstOrDefault(u => u.UserID == client.UserID);
                        if (user != null)
                        {
                            TopClientText.Text = $"{user.LastName} {user.FirstName}";
                            TopClientCountText.Text = $"{topClient.Count} {(GetCorrectWord(topClient.Count, "запись", "записи", "записей"))}";
                        }
                    }
                }
                else
                {
                    TopClientText.Text = "-";
                    TopClientCountText.Text = "0 записей";
                }
                System.Diagnostics.Debug.WriteLine("=== LoadReports завершен ===");
                System.Diagnostics.Debug.WriteLine($"servicesRevenue.Count: {servicesRevenue.Count}");
                foreach (var s in servicesRevenue)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {s.ServiceName}: {s.AppointmentCount} шт, {s.TotalRevenue} руб");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчетов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        // Вспомогательный метод для правильного склонения слова "запись"
        private string GetCorrectWord(int number, string form1, string form2, string form5)
        {
            number = Math.Abs(number) % 100;
            int num = number % 10;

            if (number > 10 && number < 20) return form5;
            if (num > 1 && num < 5) return form2;
            if (num == 1) return form1;
            return form5;
        }

        #region Экспорт в PDF

        private void ExportRevenueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF файлы (*.pdf)|*.pdf",
                    FileName = $"Выручка_за_период_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    GenerateRevenueReport(saveFileDialog.FileName);
                    MessageBox.Show("Отчет успешно сохранен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportServicesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF файлы (*.pdf)|*.pdf",
                    FileName = $"Выручка_по_услугам_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    GenerateServicesReport(saveFileDialog.FileName);
                    MessageBox.Show("Отчет успешно сохранен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportAppointmentsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "PDF файлы (*.pdf)|*.pdf",
                    FileName = $"Статистика_записей_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    GenerateAppointmentsReport(saveFileDialog.FileName);
                    MessageBox.Show("Отчет успешно сохранен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateRevenueReport(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(document, fs);
                    document.Open();

                    // Заголовок
                    Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                    Paragraph title = new Paragraph("Отчет о выручке за период", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 20f;
                    document.Add(title);

                    // Период
                    Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                    Paragraph period = new Paragraph(
                        $"Период: {StartDatePicker.SelectedDate.Value:dd.MM.yyyy} - {EndDatePicker.SelectedDate.Value:dd.MM.yyyy}",
                        normalFont);
                    period.SpacingAfter = 20f;
                    document.Add(period);

                    // Таблица с данными
                    PdfPTable table = new PdfPTable(2);
                    table.WidthPercentage = 80;
                    table.SetWidths(new float[] { 60f, 40f });

                    // Заголовки таблицы
                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                    BaseColor headerColor = new BaseColor(206, 120, 2);

                    PdfPCell cell1 = new PdfPCell(new Phrase("Показатель", headerFont));
                    PdfPCell cell2 = new PdfPCell(new Phrase("Значение", headerFont));
                    cell1.BackgroundColor = headerColor;
                    cell2.BackgroundColor = headerColor;
                    cell1.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell2.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell1);
                    table.AddCell(cell2);

                    // Данные
                    table.AddCell(new Phrase("Общая выручка", normalFont));
                    table.AddCell(new Phrase(TotalRevenueText.Text, normalFont));

                    table.AddCell(new Phrase("Количество выполненных записей", normalFont));
                    table.AddCell(new Phrase(CompletedAppointmentsCountText.Text, normalFont));

                    table.AddCell(new Phrase("Средний чек", normalFont));
                    table.AddCell(new Phrase(AverageBillText.Text, normalFont));

                    document.Add(table);

                    // Дата создания
                    Paragraph footer = new Paragraph(
                        $"\n\nОтчет сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY));
                    footer.Alignment = Element.ALIGN_RIGHT;
                    document.Add(footer);

                    document.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании PDF: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateServicesReport(string filePath)
        {
            try
            {
                // ВАЖНО: Получаем актуальные данные перед генерацией PDF
                if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите период для отчета", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = StartDatePicker.SelectedDate.Value.Date;
                DateTime endDate = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);

                // Получаем свежие данные напрямую из БД
                var completedAppointments = AppConnect.modelBd.Appointments
                    .Where(a => a.AppointmentDate >= startDate &&
                               a.AppointmentDate <= endDate &&
                               a.StatusID == 2) // Статус "Выполнено"
                    .Join(AppConnect.modelBd.Services,
                        a => a.ServiceID,
                        s => s.ServiceID,
                        (a, s) => new { Appointment = a, Service = s })
                    .ToList();

                // Группируем по услугам
                var servicesData = completedAppointments
                    .GroupBy(x => x.Service.ServiceName)
                    .Select(g => new ServiceRevenue
                    {
                        ServiceName = g.Key,
                        AppointmentCount = g.Count(),
                        TotalRevenue = g.Sum(x => x.Service.Price)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"=== ГЕНЕРАЦИЯ PDF ОТЧЕТА ===");
                System.Diagnostics.Debug.WriteLine($"Количество услуг в servicesData: {servicesData?.Count ?? 0}");

                if (servicesData != null)
                {
                    foreach (var s in servicesData)
                    {
                        System.Diagnostics.Debug.WriteLine($"Услуга: '{s.ServiceName}', Количество: {s.AppointmentCount}, Сумма: {s.TotalRevenue}");
                    }
                }

                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(document, fs);
                    document.Open();

                    // Заголовок
                    Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                    Paragraph title = new Paragraph("Отчет о выручке по услугам", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 20f;
                    document.Add(title);

                    // Период
                    Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                    Paragraph period = new Paragraph(
                        $"Период: {StartDatePicker.SelectedDate.Value:dd.MM.yyyy} - {EndDatePicker.SelectedDate.Value:dd.MM.yyyy}",
                        normalFont);
                    period.SpacingAfter = 20f;
                    document.Add(period);

                    // Таблица с данными
                    PdfPTable table = new PdfPTable(3);
                    table.WidthPercentage = 100;
                    table.SetWidths(new float[] { 50f, 25f, 25f });

                    // Заголовки таблицы
                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                    BaseColor headerColor = new BaseColor(206, 120, 2);

                    PdfPCell cell1 = new PdfPCell(new Phrase("Услуга", headerFont));
                    PdfPCell cell2 = new PdfPCell(new Phrase("Количество", headerFont));
                    PdfPCell cell3 = new PdfPCell(new Phrase("Выручка", headerFont));

                    cell1.BackgroundColor = headerColor;
                    cell2.BackgroundColor = headerColor;
                    cell3.BackgroundColor = headerColor;
                    cell1.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell2.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell3.HorizontalAlignment = Element.ALIGN_CENTER;

                    table.AddCell(cell1);
                    table.AddCell(cell2);
                    table.AddCell(cell3);

                    // Данные по услугам
                    if (servicesData != null && servicesData.Any())
                    {
                        foreach (var service in servicesData)
                        {
                            // Название услуги
                            string serviceName = string.IsNullOrEmpty(service.ServiceName) ? "Без названия" : service.ServiceName;
                            System.Diagnostics.Debug.WriteLine($"Добавляем в PDF: {serviceName}");

                            PdfPCell nameCell = new PdfPCell(new Phrase(serviceName, normalFont));
                            nameCell.HorizontalAlignment = Element.ALIGN_LEFT;
                            nameCell.Padding = 5;
                            table.AddCell(nameCell);

                            // Количество
                            PdfPCell countCell = new PdfPCell(new Phrase(service.AppointmentCount.ToString(), normalFont));
                            countCell.HorizontalAlignment = Element.ALIGN_CENTER;
                            countCell.Padding = 5;
                            table.AddCell(countCell);

                            // Выручка
                            PdfPCell revenueCell = new PdfPCell(new Phrase($"{service.TotalRevenue:N0} ₽", normalFont));
                            revenueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                            revenueCell.Padding = 5;
                            table.AddCell(revenueCell);
                        }

                        // Итоговая строка
                        Font boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);

                        PdfPCell totalLabelCell = new PdfPCell(new Phrase("ИТОГО:", boldFont));
                        totalLabelCell.HorizontalAlignment = Element.ALIGN_LEFT;
                        totalLabelCell.BackgroundColor = new BaseColor(240, 240, 240);
                        totalLabelCell.Padding = 5;
                        table.AddCell(totalLabelCell);

                        PdfPCell totalCountCell = new PdfPCell(new Phrase(servicesData.Sum(x => x.AppointmentCount).ToString(), boldFont));
                        totalCountCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        totalCountCell.BackgroundColor = new BaseColor(240, 240, 240);
                        totalCountCell.Padding = 5;
                        table.AddCell(totalCountCell);

                        PdfPCell totalRevenueCell = new PdfPCell(new Phrase($"{servicesData.Sum(x => x.TotalRevenue):N0} ₽", boldFont));
                        totalRevenueCell.HorizontalAlignment = Element.ALIGN_RIGHT;
                        totalRevenueCell.BackgroundColor = new BaseColor(240, 240, 240);
                        totalRevenueCell.Padding = 5;
                        table.AddCell(totalRevenueCell);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("НЕТ ДАННЫХ для отображения в PDF!");

                        PdfPCell emptyCell = new PdfPCell(new Phrase("Нет данных за выбранный период", normalFont));
                        emptyCell.Colspan = 3;
                        emptyCell.HorizontalAlignment = Element.ALIGN_CENTER;
                        emptyCell.Padding = 20;
                        table.AddCell(emptyCell);
                    }

                    document.Add(table);

                    // Дата создания
                    Paragraph footer = new Paragraph(
                        $"{DateTime.Now:dd.MM.yyyy HH:mm:ss}",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY));
                    footer.Alignment = Element.ALIGN_RIGHT;
                    footer.SpacingBefore = 20f;
                    document.Add(footer);

                    document.Close();

                    System.Diagnostics.Debug.WriteLine($"PDF успешно создан: {filePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА при создании PDF: {ex.Message}");
                MessageBox.Show($"Ошибка при создании PDF: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateAppointmentsReport(string filePath)
        {
            try
            {
                // Получаем актуальные данные перед генерацией PDF
                if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите период для отчета", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = StartDatePicker.SelectedDate.Value.Date;
                DateTime endDate = EndDatePicker.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);

                var allAppointments = AppConnect.modelBd.Appointments
                    .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
                    .ToList();

                int total = allAppointments.Count;
                int completed = allAppointments.Count(a => a.StatusID == 2);
                int scheduled = allAppointments.Count(a => a.StatusID == 1);
                int cancelled = allAppointments.Count(a => a.StatusID == 3 || a.StatusID == 4);

                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    Document document = new Document(PageSize.A4, 25, 25, 30, 30);
                    PdfWriter writer = PdfWriter.GetInstance(document, fs);
                    document.Open();

                    // Заголовок
                    Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18);
                    Paragraph title = new Paragraph("Статистика записей за период", titleFont);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 20f;
                    document.Add(title);

                    // Период
                    Font normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                    Paragraph period = new Paragraph(
                        $"Период: {StartDatePicker.SelectedDate.Value:dd.MM.yyyy} - {EndDatePicker.SelectedDate.Value:dd.MM.yyyy}",
                        normalFont);
                    period.SpacingAfter = 20f;
                    document.Add(period);

                    // Таблица со статистикой
                    PdfPTable table = new PdfPTable(2);
                    table.WidthPercentage = 70;
                    table.SetWidths(new float[] { 50f, 20f });

                    // Заголовки таблицы
                    Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                    BaseColor headerColor = new BaseColor(206, 120, 2);

                    PdfPCell cell1 = new PdfPCell(new Phrase("Статус", headerFont));
                    PdfPCell cell2 = new PdfPCell(new Phrase("Количество", headerFont));
                    cell1.BackgroundColor = headerColor;
                    cell2.BackgroundColor = headerColor;
                    cell1.HorizontalAlignment = Element.ALIGN_CENTER;
                    cell2.HorizontalAlignment = Element.ALIGN_CENTER;
                    table.AddCell(cell1);
                    table.AddCell(cell2);

                    // Данные
                    table.AddCell(new Phrase("Всего записей", normalFont));
                    table.AddCell(new Phrase(total.ToString(), normalFont));

                    table.AddCell(new Phrase("Выполнено", normalFont));
                    table.AddCell(new Phrase(completed.ToString(), normalFont));

                    table.AddCell(new Phrase("Запланировано", normalFont));
                    table.AddCell(new Phrase(scheduled.ToString(), normalFont));

                    table.AddCell(new Phrase("Отменено", normalFont));
                    table.AddCell(new Phrase(cancelled.ToString(), normalFont));

                    document.Add(table);

                    // Проценты
                    if (total > 0)
                    {
                        Font boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                        Paragraph stats = new Paragraph();
                        stats.SpacingBefore = 20f;
                        stats.Add(new Chunk("\nПроцент выполнения: ", normalFont));
                        stats.Add(new Chunk($"{(completed * 100.0 / total):F1}%", boldFont));
                        stats.Add(new Chunk("\nПроцент отмен: ", normalFont));
                        stats.Add(new Chunk($"{(cancelled * 100.0 / total):F1}%", boldFont));
                        document.Add(stats);
                    }

                    // Дата создания
                    Paragraph footer = new Paragraph(
                        $"\n\nОтчет сгенерирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}",
                        FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.GRAY));
                    footer.Alignment = Element.ALIGN_RIGHT;
                    document.Add(footer);

                    document.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании PDF: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Вы действительно хотите выйти из раздела отчетов?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Возвращаемся на страницу администратора
                    AppFrame.frame.Navigate(new AdminPage());
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