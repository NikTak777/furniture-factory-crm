using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FurnitureCRMClient.Models;
using FurnitureCRMClient.ViewModels;
using System.Text;
using System.Linq;
using System.ComponentModel;
using System.Windows.Data;

namespace FurnitureCRMClient.Views
{
    /// <summary>
    /// Interaction logic for MaterialPurchaseOrderUserControl.xaml
    /// </summary>
    public partial class MaterialPurchaseOrderUserControl : UserControl, INotifyPropertyChanged
    {
        private readonly AuthenticatedUser _currentUser;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/";
        public ObservableCollection<MaterialOrder> MaterialOrders { get; set; } = new();
        public ICollectionView MaterialOrdersView { get; private set; }
        public List<Material> MaterialsList { get; set; } = new();
        public List<Staff> StaffList { get; set; } = new();
        public event Action MaterialOrderChanged;

        public ObservableCollection<string> StaffFilters { get; set; } = new();
        public ObservableCollection<string> Statuses { get; set; } = new();

        private string _searchSupplier;
        public string SearchSupplier
        {
            get => _searchSupplier;
            set
            {
                if (_searchSupplier != value)
                {
                    _searchSupplier = value;
                    OnPropertyChanged(nameof(SearchSupplier));
                    MaterialOrdersView?.Refresh();
                }
            }
        }

        private string _searchMaterialName;
        public string SearchMaterialName
        {
            get => _searchMaterialName;
            set
            {
                if (_searchMaterialName != value)
                {
                    _searchMaterialName = value;
                    OnPropertyChanged(nameof(SearchMaterialName));
                    MaterialOrdersView?.Refresh();
                }
            }
        }

        private string _minQuantityText;
        public string MinQuantityText
        {
            get => _minQuantityText;
            set
            {
                if (_minQuantityText != value)
                {
                    _minQuantityText = value;
                    OnPropertyChanged(nameof(MinQuantityText));
                    MaterialOrdersView?.Refresh();
                }
            }
        }

        private string _maxQuantityText;
        public string MaxQuantityText
        {
            get => _maxQuantityText;
            set
            {
                if (_maxQuantityText != value)
                {
                    _maxQuantityText = value;
                    OnPropertyChanged(nameof(MaxQuantityText));
                    MaterialOrdersView?.Refresh();
                }
            }
        }

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
                    MaterialOrdersView?.Refresh();
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
                    MaterialOrdersView?.Refresh();
                }
            }
        }

        private string _selectedStaffFilter;
        public string SelectedStaffFilter
        {
            get => _selectedStaffFilter;
            set
            {
                if (_selectedStaffFilter != value)
                {
                    _selectedStaffFilter = value;
                    OnPropertyChanged(nameof(SelectedStaffFilter));
                    MaterialOrdersView?.Refresh();
                }
            }
        }

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
                    MaterialOrdersView?.Refresh();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Новый метод для обновления списка материалов
        public async void ReloadMaterialsListData()
        {
            try
            {
                var matResponse = await _httpClient.GetAsync($"{ApiBaseUrl}materials");
                matResponse.EnsureSuccessStatusCode();
                var matString = await matResponse.Content.ReadAsStringAsync();
                var matOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                MaterialsList = JsonSerializer.Deserialize<List<Material>>(matString, matOptions) ?? new();
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка загрузки списка материалов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошибка обработки данных списка материалов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public MaterialPurchaseOrderUserControl(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            this.DataContext = this;

            MaterialOrdersView = CollectionViewSource.GetDefaultView(MaterialOrders);
            MaterialOrdersView.Filter = MaterialOrderFilter;

            LoadMaterialsAndOrders();
        }

        private async void LoadMaterialsAndOrders()
        {
            try
            {
                // Материалы
                var matResponse = await _httpClient.GetAsync($"{ApiBaseUrl}materials");
                matResponse.EnsureSuccessStatusCode();
                var matString = await matResponse.Content.ReadAsStringAsync();
                var matOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                MaterialsList = JsonSerializer.Deserialize<List<Material>>(matString, matOptions) ?? new();

                // Сотрудники (для отображения ФИО оформителя)
                var staffResponse = await _httpClient.GetAsync($"{ApiBaseUrl}staff");
                staffResponse.EnsureSuccessStatusCode();
                var staffString = await staffResponse.Content.ReadAsStringAsync();
                var staffOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                StaffList = JsonSerializer.Deserialize<List<Staff>>(staffString, staffOptions) ?? new();

                // Заказы
                var ordResponse = await _httpClient.GetAsync($"{ApiBaseUrl}materialorders");
                ordResponse.EnsureSuccessStatusCode();
                var ordString = await ordResponse.Content.ReadAsStringAsync();
                var ordOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var orders = JsonSerializer.Deserialize<List<MaterialOrder>>(ordString, ordOptions) ?? new();
                MaterialOrders.Clear();
                foreach (var o in orders)
                {
                    // Расширяем для отображения наименования сырья и ФИО оформителя
                    o.Наименование_сырья = MaterialsList.FirstOrDefault(m => m.Артикул_сырья == o.Артикул_сырья)?.Наименование_материала ?? $"Сырьё {o.Артикул_сырья}";

                    // Ищем сотрудника по ID и подставляем его ФИО
                    var staff = StaffList.FirstOrDefault(s => s.StaffId == o.ID_оформляющего_сотрудника);
                    if (staff == null && o.ID_оформляющего_сотрудника != 0)
                    {
                        // Возможно, сотрудник сейчас неактивен и не попал в общий список /staff.
                        // Пробуем загрузить его по id (GET /staff/{id} возвращает и неактивных).
                        var staffResp = await _httpClient.GetAsync($"{ApiBaseUrl}staff/{o.ID_оформляющего_сотрудника}");
                        if (staffResp.IsSuccessStatusCode)
                        {
                            var staffJson = await staffResp.Content.ReadAsStringAsync();
                            var staffFromApi = JsonSerializer.Deserialize<Staff>(staffJson, staffOptions);
                            if (staffFromApi != null)
                            {
                                StaffList.Add(staffFromApi);
                                staff = staffFromApi;
                            }
                        }
                    }

                    o.ФИО_оформителя = staff?.FullName ?? o.ID_оформляющего_сотрудника.ToString();
                    MaterialOrders.Add(o);
                }

                UpdateStaffFilters();
                UpdateStatuses();
                MaterialOrdersView?.Refresh();
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошибка обработки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool MaterialOrderFilter(object item)
        {
            if (item is not MaterialOrder o)
                return false;

            bool matchesSupplier = string.IsNullOrWhiteSpace(SearchSupplier)
                                   || (!string.IsNullOrWhiteSpace(o.Поставщик)
                                       && o.Поставщик.IndexOf(SearchSupplier, StringComparison.OrdinalIgnoreCase) >= 0);

            bool matchesMaterial = string.IsNullOrWhiteSpace(SearchMaterialName)
                                   || (!string.IsNullOrWhiteSpace(o.Наименование_сырья)
                                       && o.Наименование_сырья.IndexOf(SearchMaterialName, StringComparison.OrdinalIgnoreCase) >= 0);

            int? minQty = TryParseQuantity(MinQuantityText);
            int? maxQty = TryParseQuantity(MaxQuantityText);
            bool matchesMinQty = !minQty.HasValue || o.Количество >= minQty.Value;
            bool matchesMaxQty = !maxQty.HasValue || o.Количество <= maxQty.Value;

            bool matchesOrderFrom = !OrderDateFrom.HasValue
                                    || o.Дата_заказа.Date >= OrderDateFrom.Value.Date;
            bool matchesOrderTo = !OrderDateTo.HasValue
                                  || o.Дата_заказа.Date <= OrderDateTo.Value.Date;

            bool matchesStaff = string.IsNullOrWhiteSpace(SelectedStaffFilter)
                                || SelectedStaffFilter == "Все оформители"
                                || (!string.IsNullOrWhiteSpace(o.ФИО_оформителя)
                                    && o.ФИО_оформителя.Equals(SelectedStaffFilter, StringComparison.OrdinalIgnoreCase));

            bool matchesStatus = string.IsNullOrWhiteSpace(SelectedStatusFilter)
                                 || SelectedStatusFilter == "Все статусы"
                                 || (!string.IsNullOrWhiteSpace(o.Статус)
                                     && o.Статус.Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));

            return matchesSupplier
                   && matchesMaterial
                   && matchesMinQty
                   && matchesMaxQty
                   && matchesOrderFrom
                   && matchesOrderTo
                   && matchesStaff
                   && matchesStatus;
        }

        private int? TryParseQuantity(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (int.TryParse(text, out var value) && value >= 0)
                return value;

            return null;
        }

        private void UpdateStaffFilters()
        {
            var distinctStaff = MaterialOrders
                .Where(o => !string.IsNullOrWhiteSpace(o.ФИО_оформителя))
                .Select(o => o.ФИО_оформителя)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            StaffFilters.Clear();
            StaffFilters.Add("Все оформители");

            foreach (var name in distinctStaff)
            {
                StaffFilters.Add(name);
            }

            if (string.IsNullOrWhiteSpace(SelectedStaffFilter))
            {
                SelectedStaffFilter = StaffFilters.FirstOrDefault();
            }
        }

        private void UpdateStatuses()
        {
            var distinctStatuses = MaterialOrders
                .Where(o => !string.IsNullOrWhiteSpace(o.Статус))
                .Select(o => o.Статус)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            Statuses.Clear();
            Statuses.Add("Все статусы");

            foreach (var status in distinctStatuses)
            {
                Statuses.Add(status);
            }

            if (string.IsNullOrWhiteSpace(SelectedStatusFilter))
            {
                SelectedStatusFilter = Statuses.FirstOrDefault();
            }
        }

        private async void AddOrder_Click(object sender, RoutedEventArgs e)
        {
            var newOrder = new MaterialOrder {
                ID_оформляющего_сотрудника = _currentUser.ID_сотрудника,
                Статус = "Ожидает поставки",
                Количество = 1
            };
            var existingSuppliers = MaterialOrders.Select(o => o.Поставщик);
            var vm = new MaterialPurchaseOrderViewModel(newOrder, MaterialsList, existingSuppliers);
            var addWindow = new AddEditMaterialPurchaseOrderWindow(vm) { Owner = Window.GetWindow(this) };
            if (addWindow.ShowDialog() == true && vm.Order != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(vm.Order);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var resp = await _httpClient.PostAsync($"{ApiBaseUrl}materialorders", content);
                    if (!resp.IsSuccessStatusCode)
                    {
                        var errorText = await resp.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(errorText))
                        {
                            MessageBox.Show(errorText, "Ошибка добавления заказа", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else
                        {
                            MessageBox.Show($"Ошибка добавления заказа: {resp.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }
                    LoadMaterialsAndOrders();
                    MaterialOrderChanged?.Invoke();
                    MessageBox.Show("Заказ успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка добавления заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EditOrder_Click(object sender, RoutedEventArgs e)
        {
            if (PurchaseOrdersDataGrid.SelectedItem is not MaterialOrder selectedOrder)
            {
                MessageBox.Show("Пожалуйста, выберите заказ для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            if (selectedOrder.Статус == "Доставлен" || selectedOrder.Статус == "Отменен")
            {
                MessageBox.Show("Редактирование невозможно. Заказ уже доставлен или отменен.", "Нельзя редактировать", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            // Создаем копию, чтобы не трогать оригинал
            var orderCopy = new MaterialOrder
            {
                Номер_заказа = selectedOrder.Номер_заказа,
                Поставщик = selectedOrder.Поставщик,
                Артикул_сырья = selectedOrder.Артикул_сырья,
                Количество = selectedOrder.Количество,
                ID_оформляющего_сотрудника = selectedOrder.ID_оформляющего_сотрудника,
                Статус = selectedOrder.Статус
            };
            var existingSuppliers = MaterialOrders.Select(o => o.Поставщик);
            var vm = new MaterialPurchaseOrderViewModel(orderCopy, MaterialsList, existingSuppliers);
            var editWindow = new AddEditMaterialPurchaseOrderWindow(vm) { Owner = Window.GetWindow(this) };
            if (editWindow.ShowDialog() == true && vm.Order != null)
            {
                try
                {
                    var json = JsonSerializer.Serialize(vm.Order);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var resp = await _httpClient.PutAsync($"{ApiBaseUrl}materialorders/{orderCopy.Номер_заказа}", content);

                    if (!resp.IsSuccessStatusCode)
                    {
                        var errorText = await resp.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(errorText))
                        {
                            MessageBox.Show(errorText, "Ошибка обновления заказа", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else
                        {
                            MessageBox.Show($"Ошибка обновления заказа: {resp.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }

                    LoadMaterialsAndOrders();
                    MaterialOrderChanged?.Invoke();
                    MessageBox.Show("Заказ успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка обновления заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearMaterialOrderFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchSupplier = string.Empty;
            SearchMaterialName = string.Empty;
            MinQuantityText = string.Empty;
            MaxQuantityText = string.Empty;
            OrderDateFrom = null;
            OrderDateTo = null;

            if (StaffFilters.Any())
            {
                SelectedStaffFilter = StaffFilters.FirstOrDefault();
            }
            else
            {
                SelectedStaffFilter = null;
            }

            if (Statuses.Any())
            {
                SelectedStatusFilter = Statuses.FirstOrDefault();
            }
            else
            {
                SelectedStatusFilter = null;
            }

            MaterialOrdersView?.Refresh();
        }
    }
}
