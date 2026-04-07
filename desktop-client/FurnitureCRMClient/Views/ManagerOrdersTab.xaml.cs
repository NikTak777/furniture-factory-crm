using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FurnitureCRMClient.Models;
using System.Linq;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using FurnitureCRMClient.Views;
using FurnitureCRMClient.ViewModels; // Добавляем этот using
using System.ComponentModel;
using System.Windows.Data;

namespace FurnitureCRMClient.Views
{
    public partial class ManagerOrdersTab : UserControl, INotifyPropertyChanged
    {
        private readonly AuthenticatedUser _currentUser;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/";

        public ObservableCollection<Order> Orders { get; set; }

        public ICollectionView OrdersView { get; private set; }

        // Статусы для фильтра
        public ObservableCollection<string> Statuses { get; set; } = new ObservableCollection<string>();

        private string _selectedStatusFilter;
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                if (_selectedStatusFilter != value)
                {
                    _selectedStatusFilter = value;
                    OnPropertyChanged(nameof(SelectedStatusFilter));
                    OrdersView?.Refresh();
                }
            }
        }

        // Фильтры по строкам
        private string _searchProductName;
        public string SearchProductName
        {
            get => _searchProductName;
            set
            {
                if (_searchProductName != value)
                {
                    _searchProductName = value;
                    OnPropertyChanged(nameof(SearchProductName));
                    OrdersView?.Refresh();
                }
            }
        }

        private string _quantityText;
        public string QuantityText
        {
            get => _quantityText;
            set
            {
                if (_quantityText != value)
                {
                    _quantityText = value;
                    OnPropertyChanged(nameof(QuantityText));
                    OrdersView?.Refresh();
                }
            }
        }

        private string _searchStaffName;
        public string SearchStaffName
        {
            get => _searchStaffName;
            set
            {
                if (_searchStaffName != value)
                {
                    _searchStaffName = value;
                    OnPropertyChanged(nameof(SearchStaffName));
                    OrdersView?.Refresh();
                }
            }
        }

        private string _searchClientName;
        public string SearchClientName
        {
            get => _searchClientName;
            set
            {
                if (_searchClientName != value)
                {
                    _searchClientName = value;
                    OnPropertyChanged(nameof(SearchClientName));
                    OrdersView?.Refresh();
                }
            }
        }

        // Диапазоны по сумме
        private string _minTotalPriceText;
        public string MinTotalPriceText
        {
            get => _minTotalPriceText;
            set
            {
                if (_minTotalPriceText != value)
                {
                    _minTotalPriceText = value;
                    OnPropertyChanged(nameof(MinTotalPriceText));
                    OrdersView?.Refresh();
                }
            }
        }

        private string _maxTotalPriceText;
        public string MaxTotalPriceText
        {
            get => _maxTotalPriceText;
            set
            {
                if (_maxTotalPriceText != value)
                {
                    _maxTotalPriceText = value;
                    OnPropertyChanged(nameof(MaxTotalPriceText));
                    OrdersView?.Refresh();
                }
            }
        }

        // Диапазоны дат
        private DateTime? _orderDateFrom;
        public DateTime? OrderDateFrom
        {
            get => _orderDateFrom;
            set
            {
                if (_orderDateFrom != value)
                {
                    _orderDateFrom = value;
                    OnPropertyChanged(nameof(OrderDateFrom));
                    OrdersView?.Refresh();
                }
            }
        }

        private DateTime? _orderDateTo;
        public DateTime? OrderDateTo
        {
            get => _orderDateTo;
            set
            {
                if (_orderDateTo != value)
                {
                    _orderDateTo = value;
                    OnPropertyChanged(nameof(OrderDateTo));
                    OrdersView?.Refresh();
                }
            }
        }

        private DateTime? _completionDateFrom;
        public DateTime? CompletionDateFrom
        {
            get => _completionDateFrom;
            set
            {
                if (_completionDateFrom != value)
                {
                    _completionDateFrom = value;
                    OnPropertyChanged(nameof(CompletionDateFrom));
                    OrdersView?.Refresh();
                }
            }
        }

        private DateTime? _completionDateTo;
        public DateTime? CompletionDateTo
        {
            get => _completionDateTo;
            set
            {
                if (_completionDateTo != value)
                {
                    _completionDateTo = value;
                    OnPropertyChanged(nameof(CompletionDateTo));
                    OrdersView?.Refresh();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ManagerOrdersTab(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            Orders = new ObservableCollection<Order>();

            OrdersView = CollectionViewSource.GetDefaultView(Orders);
            OrdersView.Filter = OrderFilter;

            this.DataContext = this;
            LoadOrders();
        }

        private async void LoadOrders()
        {
            // Сохраняем текущий выбранный статус фильтра,
            // чтобы не сбрасывать его после перезагрузки данных
            var previousStatusFilter = SelectedStatusFilter;

            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}orders");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var ordersList = JsonSerializer.Deserialize<List<Order>>(jsonString, options);

                // Загружаем дополнительные данные для отображения в UI (Product Name, Staff Name, Client Name)
                // Это может быть не самым эффективным способом, но для начала подойдет.
                // В идеале, API должен возвращать эти данные сразу же.
                var products = await GetProducts();
                var staff = await GetStaff();
                var clients = await GetClients();

                Orders.Clear();
                if (ordersList != null)
                {
                    foreach (var order in ordersList)
                    {
                        order.ProductName = products.FirstOrDefault(p => p.Артикул_товара == order.ProductId)?.Наименование ?? "Неизвестно";
                        order.StaffFullName = staff.FirstOrDefault(s => s.StaffId == order.StaffId)?.FullName ?? "Неизвестно";
                        order.ClientFullName = clients.FirstOrDefault(c => c.Id == order.ClientId)?.FullName ?? "Неизвестно";
                        Orders.Add(order);
                    }
                }

                UpdateStatuses(previousStatusFilter);
                OrdersView?.Refresh();
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошибка обработки данных заказов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool OrderFilter(object item)
        {
            if (item is not Order o)
                return false;

            // Наименование товара
            bool matchesProduct = string.IsNullOrWhiteSpace(SearchProductName)
                                  || (!string.IsNullOrWhiteSpace(o.ProductName)
                                      && o.ProductName.IndexOf(SearchProductName, StringComparison.OrdinalIgnoreCase) >= 0);

            // Количество
            bool matchesQuantity = true;
            if (!string.IsNullOrWhiteSpace(QuantityText) && int.TryParse(QuantityText, out var qty))
            {
                matchesQuantity = o.Quantity == qty;
            }

            // ФИО сотрудника
            bool matchesStaff = string.IsNullOrWhiteSpace(SearchStaffName)
                                || (!string.IsNullOrWhiteSpace(o.StaffFullName)
                                    && o.StaffFullName.IndexOf(SearchStaffName, StringComparison.OrdinalIgnoreCase) >= 0);

            // ФИО клиента
            bool matchesClient = string.IsNullOrWhiteSpace(SearchClientName)
                                 || (!string.IsNullOrWhiteSpace(o.ClientFullName)
                                     && o.ClientFullName.IndexOf(SearchClientName, StringComparison.OrdinalIgnoreCase) >= 0);

            // Статус
            bool matchesStatus = string.IsNullOrWhiteSpace(SelectedStatusFilter)
                                 || SelectedStatusFilter == "Все статусы"
                                 || string.Equals(o.Status, SelectedStatusFilter, StringComparison.OrdinalIgnoreCase);

            // Итоговая стоимость
            int? minTotal = TryParseInt(MinTotalPriceText);
            int? maxTotal = TryParseInt(MaxTotalPriceText);
            bool matchesMinTotal = !minTotal.HasValue || o.TotalPrice >= minTotal.Value;
            bool matchesMaxTotal = !maxTotal.HasValue || o.TotalPrice <= maxTotal.Value;

            // Дата оформления
            bool matchesOrderFrom = !OrderDateFrom.HasValue
                                    || (o.OrderDate.HasValue && o.OrderDate.Value.Date >= OrderDateFrom.Value.Date);
            bool matchesOrderTo = !OrderDateTo.HasValue
                                  || (o.OrderDate.HasValue && o.OrderDate.Value.Date <= OrderDateTo.Value.Date);

            // Дата выполнения
            bool matchesCompletionFrom = !CompletionDateFrom.HasValue
                                         || (o.CompletionDate.HasValue && o.CompletionDate.Value.Date >= CompletionDateFrom.Value.Date);
            bool matchesCompletionTo = !CompletionDateTo.HasValue
                                       || (o.CompletionDate.HasValue && o.CompletionDate.Value.Date <= CompletionDateTo.Value.Date);

            return matchesProduct
                   && matchesQuantity
                   && matchesStaff
                   && matchesClient
                   && matchesStatus
                   && matchesMinTotal
                   && matchesMaxTotal
                   && matchesOrderFrom
                   && matchesOrderTo
                   && matchesCompletionFrom
                   && matchesCompletionTo;
        }

        private int? TryParseInt(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            return int.TryParse(text, out var value) && value >= 0 ? value : null;
        }

        private void UpdateStatuses(string? previousSelectedStatus = null)
        {
            // Желаемый статус фильтра: либо переданный явно, либо текущий выбранный
            var targetStatus = previousSelectedStatus ?? SelectedStatusFilter;

            var distinctStatuses = Orders
                .Where(o => !string.IsNullOrWhiteSpace(o.Status))
                .Select(o => o.Status)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            Statuses.Clear();
            Statuses.Add("Все статусы");

            foreach (var status in distinctStatuses)
            {
                Statuses.Add(status);
            }

            // Если ранее выбранный статус всё ещё существует — восстанавливаем его
            if (!string.IsNullOrWhiteSpace(targetStatus) &&
                Statuses.Contains(targetStatus))
            {
                SelectedStatusFilter = targetStatus;
            }
            // Иначе, если фильтр ещё не выбран — ставим "Все статусы"
            else if (string.IsNullOrWhiteSpace(SelectedStatusFilter))
            {
                SelectedStatusFilter = Statuses.FirstOrDefault();
            }
        }

        private void ClearOrderFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchProductName = string.Empty;
            QuantityText = string.Empty;
            SearchStaffName = string.Empty;
            SearchClientName = string.Empty;
            MinTotalPriceText = string.Empty;
            MaxTotalPriceText = string.Empty;
            OrderDateFrom = null;
            OrderDateTo = null;
            CompletionDateFrom = null;
            CompletionDateTo = null;

            if (Statuses.Any())
            {
                SelectedStatusFilter = Statuses.FirstOrDefault();
            }
            else
            {
                SelectedStatusFilter = null;
            }

            OrdersView?.Refresh();
        }

        private async Task<List<Nomenclature>> GetProducts()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}nomenclature");
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<Nomenclature>>(jsonString, options) ?? new List<Nomenclature>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<Nomenclature>();
            }
        }

        private async Task<List<Staff>> GetStaff()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}staff");
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<Staff>>(jsonString, options) ?? new List<Staff>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<Staff>();
            }
        }

        private async Task<List<Client>> GetClients()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}clients");
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<Client>>(jsonString, options) ?? new List<Client>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<Client>();
            }
        }

        private async void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            var newOrder = new Order
            {
                // Дату оформления не задаём - её поставит БД по умолчанию
                StaffId = _currentUser.ID_сотрудника, // ID текущего сотрудника
                Status = "В обработке" // Статус по умолчанию
            };

            // Загрузка справочных данных
            var allProducts = await GetProducts();
            var allClients = await GetClients();

            var addEditWindow = new AddEditOrderWindow(newOrder);
            
            addEditWindow.Owner = Window.GetWindow(this);
            bool? result = addEditWindow.ShowDialog();

            if (result == true)
            {
                // Отправляем новый заказ на API
                try
                {
                    var viewModel = addEditWindow.DataContext as OrderViewModel;
                    if (viewModel?.Order == null)
                    {
                        MessageBox.Show("Ошибка: Данные заказа отсутствуют.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var orderToSerialize = viewModel.Order;

                    var jsonContent = new StringContent(
                        JsonSerializer.Serialize(orderToSerialize), // Используем локальную переменную
                        System.Text.Encoding.UTF8,
                        new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
                    );

                    string json = JsonSerializer.Serialize(orderToSerialize, new JsonSerializerOptions { WriteIndented = true });
                    MessageBox.Show(json);

                    var response = await _httpClient.PostAsync($"{ApiBaseUrl}orders", jsonContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorText = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(errorText))
                        {
                            MessageBox.Show(errorText, "Ошибка добавления заказа", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else
                        {
                            MessageBox.Show($"Ошибка добавления заказа: {response.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }

                    MessageBox.Show("Заказ успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadOrders(); // Перезагружаем список заказов
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка добавления заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Ошибка обработки данных заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла непредвиденная ошибка при добавлении заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem is not Order selectedOrder) {
                MessageBox.Show("Выберите заказ для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (selectedOrder.Status == "Выполнен" || selectedOrder.Status == "Отменен")
            {
                MessageBox.Show("Редактирование невозможно. Заказ уже завершён или отменен.", "Нельзя редактировать", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            // Клонируем заказ (чтобы изменения не отразились в списке до подтверждения)
            var orderToEdit = new Order {
                OrderId = selectedOrder.OrderId,
                ProductId = selectedOrder.ProductId,
                Quantity = selectedOrder.Quantity,
                OrderDate = selectedOrder.OrderDate,
                StaffId = selectedOrder.StaffId,
                CompletionDate = selectedOrder.CompletionDate,
                ClientId = selectedOrder.ClientId,
                TotalPrice = selectedOrder.TotalPrice,
                Status = selectedOrder.Status
            };
            
            // Загрузка справочных данных
            var allProducts = await GetProducts();
            var allClients = await GetClients();

            var addEditWindow = new AddEditOrderWindow(orderToEdit);
            addEditWindow.Owner = Window.GetWindow(this);
            bool? result = addEditWindow.ShowDialog();
            if (result == true)
            {
                // Отправить PATCH или PUT-запрос на API для обновления заказа
                try
                {
                    var viewModel = addEditWindow.DataContext as OrderViewModel;
                    if (viewModel?.Order == null)
                    {
                        MessageBox.Show("Ошибка: Данные заказа отсутствуют.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    var orderToSerialize = viewModel.Order;

                    var jsonContent = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize<Order>(orderToSerialize),
                        System.Text.Encoding.UTF8,
                        new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
                    );
                    var response = await _httpClient.PutAsync($"{ApiBaseUrl}orders/{orderToSerialize.OrderId}", jsonContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        var errorText = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(errorText))
                        {
                            MessageBox.Show(errorText, "Ошибка обновления заказа", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else
                        {
                            MessageBox.Show($"Ошибка обновления заказа: {response.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }

                    MessageBox.Show("Заказ успешно обновлён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadOrders();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Удалить заказ - заглушка");
        }
    }
}
