using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FurnitureCRMClient.Models;
using Microsoft.Win32;
using System.Text;

namespace FurnitureCRMClient.Views
{
    public partial class ManagerReportsTab : UserControl
    {
        private readonly AuthenticatedUser _currentUser;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/";

        public ObservableCollection<ProductionReportOrderItem> Orders { get; set; } = new();

        public ManagerReportsTab(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            this.DataContext = this;
        }

        private async void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = $"{ApiBaseUrl}reports/production";

                var from = FromDatePicker.SelectedDate;
                var to = ToDatePicker.SelectedDate;

                // Формируем строку запроса вручную, чтобы не тянуть System.Web
                var queryParts = new System.Collections.Generic.List<string>();
                if (from.HasValue)
                    queryParts.Add("from=" + from.Value.ToString("yyyy-MM-dd"));
                if (to.HasValue)
                    queryParts.Add("to=" + to.Value.ToString("yyyy-MM-dd"));

                if (queryParts.Count > 0)
                {
                    url += "?" + string.Join("&", queryParts);
                }

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(errorText))
                    {
                        MessageBox.Show(errorText, "Ошибка получения отчёта", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка получения отчёта: {response.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var report = JsonSerializer.Deserialize<ProductionReportResult>(json, options);
                if (report == null)
                {
                    MessageBox.Show("Не удалось разобрать данные отчёта.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Orders.Clear();
                foreach (var o in report.Orders)
                {
                    Orders.Add(o);
                }

                TotalOrdersText.Text = $"Всего заказов: {report.TotalOrders}";
                InProcessingText.Text = $"В обработке: {report.InProcessingCount}";
                InProductionText.Text = $"В производстве: {report.InProductionCount}";
                CompletedText.Text = $"Выполнено: {report.CompletedCount}";
                CancelledText.Text = $"Отменено: {report.CancelledCount}";
                TotalRevenueText.Text = $"Выручка (выполненные): {report.TotalRevenue} руб.";
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка получения отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошибка обработки данных отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка при получении отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveReport_Click(object sender, RoutedEventArgs e)
        {
            if (Orders.Count == 0)
            {
                MessageBox.Show("Нет данных для сохранения. Сначала сформируйте отчёт.", "Сохранение отчёта", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Формируем название файла с диапазоном дат
            // Если даты не заданы, используем дату первого заказа и сегодняшнюю дату
            DateTime? fromDate = FromDatePicker.SelectedDate;
            DateTime? toDate = ToDatePicker.SelectedDate;
            
            if (!fromDate.HasValue && Orders.Count > 0)
            {
                // Находим дату первого (самого раннего) заказа
                fromDate = Orders.Min(o => o.OrderDate).Date;
            }
            
            if (!toDate.HasValue)
            {
                // Используем сегодняшнюю дату
                toDate = DateTime.Now.Date;
            }

            var fromDateStr = fromDate?.ToString("dd.MM.yy") ?? "01.01.00";
            var toDateStr = toDate?.ToString("dd.MM.yy") ?? "01.01.00";
            var defaultFileName = $"Отчёт о производстве {fromDateStr}-{toDateStr}.csv";

            var dialog = new SaveFileDialog
            {
                Title = "Сохранить отчёт о производстве",
                Filter = "CSV файл (*.csv)|*.csv|Текстовый файл (*.txt)|*.txt",
                FileName = defaultFileName
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var sb = new StringBuilder();

                // Заголовок отчёта (используем те же даты, что и для имени файла)
                var from = fromDate?.ToString("dd.MM.yyyy") ?? "-";
                var to = toDate?.ToString("dd.MM.yyyy") ?? "-";
                sb.AppendLine($"Отчёт о производстве;Период:;{from};по;{to}");
                sb.AppendLine();

                // Сводка (из текстовых блоков)
                var totalOrders = TotalOrdersText.Text.Replace("Всего заказов: ", string.Empty);
                var inProcessing = InProcessingText.Text.Replace("В обработке: ", string.Empty);
                var inProduction = InProductionText.Text.Replace("В производстве: ", string.Empty);
                var completed = CompletedText.Text.Replace("Выполнено: ", string.Empty);
                var cancelled = CancelledText.Text.Replace("Отменено: ", string.Empty);
                var revenue = TotalRevenueText.Text.Replace("Выручка (выполненные): ", string.Empty);

                sb.AppendLine("Всего заказов;" + totalOrders);
                sb.AppendLine("В обработке;" + inProcessing);
                sb.AppendLine("В производстве;" + inProduction);
                sb.AppendLine("Выполнено;" + completed);
                sb.AppendLine("Отменено;" + cancelled);
                sb.AppendLine("Выручка (выполненные);" + revenue);
                sb.AppendLine();

                // Заголовки таблицы
                sb.AppendLine("Номер заказа;Дата оформления;Наименование товара;Количество;Статус;Итоговая стоимость");

                // Строки таблицы
                foreach (var o in Orders)
                {
                    var date = o.OrderDate.ToString("dd.MM.yyyy");
                    sb.AppendLine($"{o.OrderId};{date};{o.ProductName};{o.Quantity};{o.Status};{o.TotalPrice}");
                }

                System.IO.File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Отчёт успешно сохранён.", "Сохранение отчёта", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении отчёта: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class ProductionReportOrderItem
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalPrice { get; set; }
    }

    public class ProductionReportResult
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int TotalOrders { get; set; }
        public int InProcessingCount { get; set; }
        public int InProductionCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public int TotalRevenue { get; set; }
        public System.Collections.Generic.List<ProductionReportOrderItem> Orders { get; set; } = new();
    }
}


