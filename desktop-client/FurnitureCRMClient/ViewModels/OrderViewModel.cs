using CommunityToolkit.Mvvm.ComponentModel;
using FurnitureCRMClient.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel; // Добавляем
using System.Collections; // Добавляем
using System.Collections.Generic; // Добавляем
using CommunityToolkit.Mvvm.Input; // Добавляем

namespace FurnitureCRMClient.ViewModels
{
    public partial class OrderViewModel : ObservableObject, INotifyDataErrorInfo // Добавляем INotifyDataErrorInfo
    {
        [ObservableProperty]
        private Order _order;

        [ObservableProperty]
        private ObservableCollection<Nomenclature> _availableProducts;

        [ObservableProperty]
        private Nomenclature? _selectedProduct;

        private List<Nomenclature> _allProductsList; // Полный список продуктов
        private ObservableCollection<Nomenclature> _filteredProducts; // Отфильтрованный список для ComboBox
        public ObservableCollection<Nomenclature> FilteredProducts
        {
            get => _filteredProducts;
            set
            {
                _filteredProducts = value;
                OnPropertyChanged(nameof(FilteredProducts));
            }
        }

        private string _searchProductText = string.Empty; // Текст для поиска продуктов
        public string SearchProductText
        {
            get => _searchProductText;
            set
            {
                if (_searchProductText != value)
                {
                    _searchProductText = value;
                    OnPropertyChanged(nameof(SearchProductText));
                    FilterProducts(); // Вызываем фильтрацию при изменении текста поиска
                }
            }
        }

        [ObservableProperty]
        private ObservableCollection<Client> _availableClients;

        [ObservableProperty]
        private Client? _selectedClient;

        private List<Client> _allClientsList; // Полный список клиентов
        private ObservableCollection<Client> _filteredClients; // Отфильтрованный список для ComboBox
        public ObservableCollection<Client> FilteredClients
        {
            get => _filteredClients;
            set
            {
                _filteredClients = value;
                OnPropertyChanged(nameof(FilteredClients));
            }
        }

        private string _searchClientText = string.Empty; // Текст для поиска клиентов
        public string SearchClientText
        {
            get => _searchClientText;
            set
            {
                if (_searchClientText != value)
                {
                    _searchClientText = value;
                    OnPropertyChanged(nameof(SearchClientText));
                    FilterClients(); // Вызываем фильтрацию при изменении текста поиска
                }
            }
        }

        // СТАТУСЫ заказа
        public List<string> Statuses { get; } = new() { "В обработке", "В производстве", "Выполнен", "Отменен" };

        public ICollection<string> AvailableStatuses {
            get {
                if (Order.OrderId == 0)
                    return new List<string> { "В обработке", "Отменен" };
                if (Order.Status == "В обработке")
                    return new List<string> { "В обработке", "В производстве", "Отменен" };
                if (Order.Status == "В производстве")
                    return new List<string> { "В производстве", "Выполнен", "Отменен" };
                return new List<string> { Order.Status };
            }
        }
        public bool IsStatusEditable => Order.Status != "Отменен" && Order.Status != "Выполнен";

        // Количество можно редактировать только пока заказ в статусе "В обработке"
        // (и для новых заказов, у которых OrderId == 0 и статус по умолчанию "В обработке")
        public bool IsQuantityEditable => Order.OrderId == 0 || Order.Status == "В обработке";
        
        // Товар (номенклатура) можно редактировать только пока заказ в статусе "В обработке"
        // (и для новых заказов, у которых OrderId == 0 и статус по умолчанию "В обработке")
        public bool IsProductEditable => Order.OrderId == 0 || Order.Status == "В обработке";

        // Клиента можно менять только пока заказ в статусе "В обработке"
        // (и для новых заказов, у которых OrderId == 0)
        public bool IsClientEditable => Order.OrderId == 0 || Order.Status == "В обработке";

        // 1. SelectedStatus — только VM-хранимое свойство, без логики переходов/async
        private string _selectedStatus;
        public string SelectedStatus
        {
            get => _selectedStatus;
            set
            {
                if (_selectedStatus != value)
                {
                    _selectedStatus = value;
                    OnPropertyChanged(nameof(SelectedStatus));
                }
            }
        }

        // 2. Асинхронная команда для смены статуса
        [RelayCommand]
        private async Task ChangeStatusAsync(string? newStatus)
        {
            if (string.IsNullOrWhiteSpace(newStatus))
                return;
            // Проверяем разрешён ли переход для текущего Order.Status
            var allowed = AvailableStatuses;
            if (!allowed.Contains(newStatus))
            {
                System.Windows.MessageBox.Show("Переход в этот статус запрещён!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                SelectedStatus = Order.Status;
                return;
            }
            // Переход в конечные статусы — ставим дату если нужно
            Order.Status = newStatus;
            if (Order.Status == "Выполнен")
                Order.CompletionDate = DateTime.Now;
            else if (Order.Status == "В производстве" || Order.Status == "В обработке")
                Order.CompletionDate = null;
            // Обновить всё
            SelectedStatus = Order.Status;
            OnPropertyChanged(nameof(Order));
            OnPropertyChanged(nameof(AvailableStatuses));
            OnPropertyChanged(nameof(IsStatusEditable));
            OnPropertyChanged(nameof(IsQuantityEditable));
            OnPropertyChanged(nameof(IsProductEditable));
            OnPropertyChanged(nameof(IsClientEditable));
        }

        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/";

        // Текстовое представление количества для ввода пользователем
        private string _quantityText = string.Empty;
        public string QuantityText
        {
            get => _quantityText;
            set
            {
                if (_quantityText != value)
                {
                    _quantityText = value;
                    OnPropertyChanged(nameof(QuantityText));

                    // Пытаемся распарсить и обновить Order.Quantity
                    if (int.TryParse(value, out int parsedQuantity) && parsedQuantity > 0)
                    {
                        Order.Quantity = parsedQuantity;
                    }
                    else
                    {
                        Order.Quantity = 0; // Некорректное значение — считаем как ошибку
                    }

                    // Валидируем количество и пересчитываем цену
                    ValidateProperty(nameof(Order.Quantity));
                    CalculateTotalPrice();
                }
            }
        }

        public OrderViewModel(Order order, List<Nomenclature> allProducts, List<Client> allClients)
        {
            Order = order;
            _allProductsList = allProducts;
            FilteredProducts = new ObservableCollection<Nomenclature>(_allProductsList);
            _searchProductText = SelectedProduct?.Наименование ?? string.Empty;

            _allClientsList = allClients;
            FilteredClients = new ObservableCollection<Client>(_allClientsList);
            _searchClientText = SelectedClient?.FullName ?? string.Empty;

            _selectedStatus = Statuses.Contains(order.Status) ? order.Status : Statuses.First();
            
            // Загрузка справочных данных теперь не нужна здесь, так как они передаются через конструктор
            // LoadReferenceData();

            // Если редактируем существующий заказ, выбираем текущие значения
            if (Order.OrderId != 0)
            {
                SelectedProduct = _allProductsList.FirstOrDefault(p => p.Артикул_товара == Order.ProductId);
                SelectedClient = _allClientsList.FirstOrDefault(c => c.Id == Order.ClientId);
                // Установка SearchText для ComboBox'ов при инициализации
                SearchProductText = SelectedProduct?.Наименование ?? string.Empty;
                SearchClientText = SelectedClient?.FullName ?? string.Empty;
                CalculateTotalPrice();
            }

            // Инициализируем текст количества из текущего значения заказа
            _quantityText = order.Quantity > 0 ? order.Quantity.ToString() : string.Empty;
            OnPropertyChanged(nameof(QuantityText));
            Validate(); // Изначальная валидация при создании ViewModel

            // Подписываемся на изменение выбранного продукта и количества для пересчета цены
            PropertyChanged += OrderViewModel_PropertyChanged;
            Order.PropertyChanged += Order_PropertyChanged; // Подписываемся на изменение свойств Order

            OnPropertyChanged(nameof(IsQuantityEditable));
            OnPropertyChanged(nameof(IsProductEditable));
            OnPropertyChanged(nameof(IsClientEditable));
        }

        private async void LoadReferenceData()
        {
            await LoadProducts();
            await LoadClients();

            // Если редактируем существующий заказ, выбираем текущие значения
            if (Order.OrderId != 0)
            {
                SelectedProduct = AvailableProducts.FirstOrDefault(p => p.Артикул_товара == Order.ProductId);
                SelectedClient = AvailableClients.FirstOrDefault(c => c.Id == Order.ClientId);
                CalculateTotalPrice();
            }
            Validate(); // Валидация после загрузки данных
        }

        private async Task LoadProducts()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}nomenclature");
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var products = JsonSerializer.Deserialize<List<Nomenclature>>(jsonString, options) ?? new List<Nomenclature>();
                _allProductsList = products; // Сохраняем полный список
                FilteredProducts.Clear();
                foreach (var p in products)
                {
                    FilteredProducts.Add(p);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadClients()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}clients");
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var clients = JsonSerializer.Deserialize<List<Client>>(jsonString, options) ?? new List<Client>();
                _allClientsList = clients; // Сохраняем полный список
                FilteredClients.Clear();
                foreach (var c in clients)
                {
                    FilteredClients.Add(c);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OrderViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedProduct))
            {
                if (SelectedProduct != null)
                {
                    if (Order != null)
                    {
                        Order.ProductId = SelectedProduct.Артикул_товара; // Обновляем Order.ProductId
                    }
                    if (_searchProductText != SelectedProduct.Наименование)
                    {
                        _searchProductText = SelectedProduct.Наименование;
                        OnPropertyChanged(nameof(SearchProductText));
                    }
                }
                else
                {
                    if (Order != null)
                    {
                        Order.ProductId = 0; 
                    }
                    if (!string.IsNullOrEmpty(_searchProductText))
                    {
                        _searchProductText = string.Empty;
                        OnPropertyChanged(nameof(SearchProductText));
                    }
                }
                ValidateProperty(nameof(SelectedProduct));
                CalculateTotalPrice();
            }
            else if (e.PropertyName == nameof(SelectedClient))
            {
                if (SelectedClient != null)
                {
                    if (Order != null)
                    {
                        Order.ClientId = SelectedClient.Id; // Обновляем Order.ClientId
                    }
                    if (_searchClientText != SelectedClient.FullName)
                    {
                        _searchClientText = SelectedClient.FullName;
                        OnPropertyChanged(nameof(SearchClientText));
                    }
                }
                else
                {
                    if (Order != null)
                    {
                        Order.ClientId = 0; 
                    }
                    if (!string.IsNullOrEmpty(_searchClientText))
                    {
                        _searchClientText = string.Empty;
                        OnPropertyChanged(nameof(SearchClientText));
                    }
                }
                ValidateProperty(nameof(SelectedClient));
            }
            // Валидация для Order.Quantity будет в Order_PropertyChanged
        }

        private void Order_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Order.Quantity))
            {
                ValidateProperty(nameof(Order.Quantity));
                CalculateTotalPrice();
            }
        }

        private void CalculateTotalPrice()
        {
            if (SelectedProduct != null && Order.Quantity > 0)
            {
                Order.TotalPrice = SelectedProduct.Стоимость * Order.Quantity;
            }
            else
            {
                Order.TotalPrice = 0;
            }
            // OnPropertyChanged(nameof(Order.TotalPrice)); // ObservableProperty уже вызывает PropertyChanged
        }

        private void FilterProducts()
        {
            FilteredProducts.Clear();
            if (string.IsNullOrWhiteSpace(SearchProductText))
            {
                foreach (var product in _allProductsList)
                {
                    FilteredProducts.Add(product);
                }
            }
            else
            {
                var lowerSearchText = SearchProductText.ToLower();
                foreach (var product in _allProductsList.Where(p => p.Наименование.ToLower().Contains(lowerSearchText)))
                {
                    FilteredProducts.Add(product);
                }
            }
        }

        private void FilterClients()
        {
            FilteredClients.Clear();
            if (string.IsNullOrWhiteSpace(SearchClientText))
            {
                foreach (var client in _allClientsList)
                {
                    FilteredClients.Add(client);
                }
            }
            else
            {
                var lowerSearchText = SearchClientText.ToLower();
                foreach (var client in _allClientsList.Where(c => c.FullName.ToLower().Contains(lowerSearchText)))
                {
                    FilteredClients.Add(client);
                }
            }
        }

        #region INotifyDataErrorInfo Implementation

        private readonly Dictionary<string, List<string>> _errors = new();

        // Упрощенный словарь для биндинга ошибок в XAML: первое сообщение на каждое свойство
        public Dictionary<string, string> Errors { get; } = new();

        public bool HasErrors => _errors.Any(kv => kv.Value is { Count: > 0 });

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (propertyName != null && _errors.TryGetValue(propertyName, out var propertyErrors))
            {
                return propertyErrors;
            }
            return Enumerable.Empty<string>();
        }

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors)); // Обновляем HasErrors
            OnPropertyChanged(nameof(CanSave)); // Обновляем CanSave
        }

        private void ValidateProperty(string propertyName)
        {
            _errors.Remove(propertyName);
            Errors.Remove(propertyName);
            List<string> propertySpecificErrors = new();

            switch (propertyName)
            {
                case nameof(SelectedProduct):
                    if (SelectedProduct == null)
                    {
                        propertySpecificErrors.Add("Номенклатура не может быть пустой.");
                    }
                    else if (!SelectedProduct.Наименование.Equals(SearchProductText, StringComparison.OrdinalIgnoreCase))
                    {
                        propertySpecificErrors.Add("Название номенклатуры не соответствует выбранному элементу.");
                    }
                    break;
                case nameof(SelectedClient):
                    if (SelectedClient == null)
                    {
                        propertySpecificErrors.Add("Клиент не может быть пустым.");
                    }
                    else if (!SelectedClient.FullName.Equals(SearchClientText, StringComparison.OrdinalIgnoreCase))
                    {
                        propertySpecificErrors.Add("Имя клиента не соответствует выбранному элементу.");
                    }
                    break;
                case nameof(Order.Quantity):
                    if (string.IsNullOrWhiteSpace(QuantityText))
                    {
                        propertySpecificErrors.Add("Количество не может быть пустым.");
                    }
                    else if (!int.TryParse(QuantityText, out var q))
                    {
                        propertySpecificErrors.Add("Количество должно быть числом.");
                    }
                    else if (q <= 0)
                    {
                        propertySpecificErrors.Add("Количество должно быть больше 0.");
                    }
                    break;
                case nameof(SelectedStatus):
                    if (string.IsNullOrWhiteSpace(SelectedStatus) || !Statuses.Contains(SelectedStatus))
                    {
                        propertySpecificErrors.Add("Выберите статус заказа.");
                    }
                    break;
            }

            if (propertySpecificErrors.Any())
            {
                _errors.Add(propertyName, propertySpecificErrors);
                // Для отображения под полем используем первое сообщение
                Errors[propertyName] = propertySpecificErrors[0];
            }
            else
            {
                Errors.Remove(propertyName);
            }
            OnPropertyChanged(nameof(Errors));
            OnErrorsChanged(propertyName);
        }

        private void Validate()
        {
            ValidateProperty(nameof(SelectedProduct));
            ValidateProperty(nameof(SelectedClient));
            ValidateProperty(nameof(Order.Quantity));
            ValidateProperty(nameof(SelectedStatus));
        }

        public bool CanSave => !HasErrors;

        #endregion
    }
}
