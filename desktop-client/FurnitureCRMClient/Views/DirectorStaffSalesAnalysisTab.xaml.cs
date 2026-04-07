using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FurnitureCRMClient.Models;

namespace FurnitureCRMClient.Views
{
    public partial class DirectorStaffSalesAnalysisTab : UserControl
    {
        private readonly AuthenticatedUser _currentUser;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/";

        public ObservableCollection<ManagerProductionSummaryItem> ManagerSummaries { get; set; } = new();
        public ObservableCollection<ProductProductionSummaryItem> ProductSummaries { get; set; } = new();

        public DirectorStaffSalesAnalysisTab(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            this.DataContext = this;
        }

        private string BuildUrl(string endpoint, DateTime? from, DateTime? to, int? staffId = null)
        {
            string url = $"{ApiBaseUrl}{endpoint}";
            var queryParts = new System.Collections.Generic.List<string>();
            if (from.HasValue)
                queryParts.Add("from=" + from.Value.ToString("yyyy-MM-dd"));
            if (to.HasValue)
                queryParts.Add("to=" + to.Value.ToString("yyyy-MM-dd"));
            if (staffId.HasValue)
                queryParts.Add("staffId=" + staffId.Value);
            if (queryParts.Count > 0)
                url += "?" + string.Join("&", queryParts);
            return url;
        }

        private async System.Threading.Tasks.Task LoadProductSummariesAsync(DateTime? from, DateTime? to, int? staffId)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var prodResponse = await _httpClient.GetAsync(BuildUrl("reports/production/by-product", from, to, staffId));
            if (!prodResponse.IsSuccessStatusCode)
            {
                var errorText = await prodResponse.Content.ReadAsStringAsync();
                if (!string.IsNullOrWhiteSpace(errorText))
                    MessageBox.Show(errorText, "Ошибка получения анализа по номенклатуре", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                    MessageBox.Show($"Ошибка получения анализа по номенклатуре: {prodResponse.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                var prodJson = await prodResponse.Content.ReadAsStringAsync();
                var prodItems = JsonSerializer.Deserialize<System.Collections.Generic.List<ProductProductionSummaryItem>>(prodJson, options) ?? new();
                ProductSummaries.Clear();
                foreach (var item in prodItems)
                    ProductSummaries.Add(item);
            }
        }

        private async void RefreshAnalysis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var from = FromDatePicker.SelectedDate;
                var to = ToDatePicker.SelectedDate;

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // Отчёт по менеджерам
                var mgrResponse = await _httpClient.GetAsync(BuildUrl("reports/production/by-manager", from, to));
                if (!mgrResponse.IsSuccessStatusCode)
                {
                    var errorText = await mgrResponse.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(errorText))
                        MessageBox.Show(errorText, "Ошибка получения анализа по менеджерам", MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        MessageBox.Show($"Ошибка получения анализа по менеджерам: {mgrResponse.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    var mgrJson = await mgrResponse.Content.ReadAsStringAsync();
                    var mgrItems = JsonSerializer.Deserialize<System.Collections.Generic.List<ManagerProductionSummaryItem>>(mgrJson, options) ?? new();
                    ManagerSummaries.Clear();
                    foreach (var item in mgrItems)
                        ManagerSummaries.Add(item);
                }

                // Отчёт по номенклатуре (общий, без фильтра по менеджеру)
                await LoadProductSummariesAsync(from, to, null);
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка связи с сервером при получении анализа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошибка обработки данных анализа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Непредвиденная ошибка при получении анализа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ManagersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ManagersDataGrid.SelectedItem is ManagerProductionSummaryItem manager)
            {
                var from = FromDatePicker.SelectedDate;
                var to = ToDatePicker.SelectedDate;
                await LoadProductSummariesAsync(from, to, manager.StaffId);
            }
        }

        private async void ClearManagerSelection_Click(object sender, RoutedEventArgs e)
        {
            ManagersDataGrid.SelectedItem = null;
            var from = FromDatePicker.SelectedDate;
            var to = ToDatePicker.SelectedDate;
            await LoadProductSummariesAsync(from, to, null);
        }
    }

    public class ManagerProductionSummaryItem
    {
        public int StaffId { get; set; }
        public string StaffFullName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int TotalRevenue { get; set; }
    }

    public class ProductProductionSummaryItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int TotalQuantity { get; set; }
        public int CompletedOrders { get; set; }
        public int TotalRevenue { get; set; }
    }
}

