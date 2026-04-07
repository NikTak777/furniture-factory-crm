using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FurnitureCRMClient.Models;
using System.Text;
using System.Linq;
using FurnitureCRMClient.ViewModels;
using FurnitureCRMClient.Views; // Добавляем using для доступа к AddEditNomenclatureWindow
using System.ComponentModel;
using System.Windows.Data;

namespace FurnitureCRMClient.Views
{
    public partial class NomenclatureManagementUserControl : UserControl, INotifyPropertyChanged
    {
        private readonly AuthenticatedUser _currentUser;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/"; // URL твоего API

        public ObservableCollection<Nomenclature> Nomenclatures { get; set; }

        public ICollectionView NomenclaturesView { get; private set; }

        public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string>();

        // Статусы производства: в производстве / снятые с производства
        public ObservableCollection<string> ProductionStatuses { get; set; } = new ObservableCollection<string>
        {
            "Производимые товары",
            "Снятые с производства"
        };

        private string _searchName;
        public string SearchName
        {
            get => _searchName;
            set
            {
                if (_searchName != value)
                {
                    _searchName = value;
                    OnPropertyChanged(nameof(SearchName));
                    NomenclaturesView?.Refresh();
                }
            }
        }

        private string _selectedCategory;
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory != value)
                {
                    _selectedCategory = value;
                    OnPropertyChanged(nameof(SelectedCategory));
                    NomenclaturesView?.Refresh();
                }
            }
        }

        private string _minPriceText;
        public string MinPriceText
        {
            get => _minPriceText;
            set
            {
                if (_minPriceText != value)
                {
                    _minPriceText = value;
                    OnPropertyChanged(nameof(MinPriceText));
                    NomenclaturesView?.Refresh();
                }
            }
        }

        private string _maxPriceText;
        public string MaxPriceText
        {
            get => _maxPriceText;
            set
            {
                if (_maxPriceText != value)
                {
                    _maxPriceText = value;
                    OnPropertyChanged(nameof(MaxPriceText));
                    NomenclaturesView?.Refresh();
                }
            }
        }

        private string _selectedProductionStatus;
        public string SelectedProductionStatus
        {
            get => _selectedProductionStatus;
            set
            {
                if (_selectedProductionStatus != value)
                {
                    _selectedProductionStatus = value;
                    OnPropertyChanged(nameof(SelectedProductionStatus));

                    // Перезагружаем номенклатуру в соответствии с выбранным статусом
                    LoadNomenclatures();

                    // Обновляем видимость кнопок
                    UpdateButtonsForProductionStatus();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Конструктор по умолчанию для XAML-парсера
        public NomenclatureManagementUserControl()
        {
            InitializeComponent();
            Nomenclatures = new ObservableCollection<Nomenclature>();
            InitializeFiltering();
        }

        public NomenclatureManagementUserControl(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            Nomenclatures = new ObservableCollection<Nomenclature>();
            InitializeFiltering();
        }

        private void InitializeFiltering()
        {
            NomenclaturesView = CollectionViewSource.GetDefaultView(Nomenclatures);
            NomenclaturesView.Filter = NomenclatureFilter;

            this.DataContext = this;

            // По умолчанию показываем производимые товары
            SelectedProductionStatus = ProductionStatuses.FirstOrDefault() ?? "Производимые товары";
            UpdateButtonsForProductionStatus();

            LoadNomenclatures();
        }

        private async void LoadNomenclatures()
        {
            try
            {
                // Выбираем эндпоинт в зависимости от статуса производства
                var endpoint = SelectedProductionStatus == "Снятые с производства"
                    ? "nomenclature/notproduced"
                    : "nomenclature";

                var response = await _httpClient.GetAsync($"{ApiBaseUrl}{endpoint}");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var nomenclatureList = JsonSerializer.Deserialize<List<Nomenclature>>(jsonString, options);

                Nomenclatures.Clear();
                if (nomenclatureList != null)
                {
                    foreach (var item in nomenclatureList)
                    {
                        Nomenclatures.Add(item);
                    }
                }

                UpdateCategories();
                NomenclaturesView?.Refresh();
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка загрузки номенклатуры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошибка обработки данных номенклатуры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool NomenclatureFilter(object item)
        {
            if (item is not Nomenclature n)
                return false;

            // Здесь не фильтруем по полю Производится, так как это делает сервер (через разные эндпоинты)

            bool matchesName = string.IsNullOrWhiteSpace(SearchName)
                               || (!string.IsNullOrWhiteSpace(n.Наименование)
                                   && n.Наименование.IndexOf(SearchName, StringComparison.OrdinalIgnoreCase) >= 0);

            bool matchesCategory = string.IsNullOrWhiteSpace(SelectedCategory)
                                   || SelectedCategory == "Все категории"
                                   || (!string.IsNullOrWhiteSpace(n.Категория)
                                       && n.Категория.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));

            int? minPrice = TryParsePrice(MinPriceText);
            int? maxPrice = TryParsePrice(MaxPriceText);

            bool matchesMinPrice = !minPrice.HasValue || n.Стоимость >= minPrice.Value;
            bool matchesMaxPrice = !maxPrice.HasValue || n.Стоимость <= maxPrice.Value;

            return matchesName && matchesCategory && matchesMinPrice && matchesMaxPrice;
        }

        private int? TryParsePrice(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (int.TryParse(text, out var value) && value >= 0)
                return value;

            return null;
        }

        private void UpdateCategories()
        {
            var distinctCategories = Nomenclatures
                .Where(n => !string.IsNullOrWhiteSpace(n.Категория))
                .Select(n => n.Категория)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            Categories.Clear();
            Categories.Add("Все категории");

            foreach (var category in distinctCategories)
            {
                Categories.Add(category);
            }

            if (string.IsNullOrWhiteSpace(SelectedCategory))
            {
                SelectedCategory = Categories.FirstOrDefault();
            }
        }

        private async void AddNomenclature_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = new NomenclatureViewModel(new Nomenclature());
            var addEditWindow = new AddEditNomenclatureWindow(viewModel);
            
            addEditWindow.Owner = Window.GetWindow(this);
            addEditWindow.ShowDialog();

            if (viewModel.Nomenclature != null)
            {
                try
                {
                    var jsonContent = new StringContent(
                        JsonSerializer.Serialize(viewModel.Nomenclature, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        }),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var response = await _httpClient.PostAsync($"{ApiBaseUrl}nomenclature", jsonContent);
                    response.EnsureSuccessStatusCode();

                    // Десериализуем ответ, чтобы получить номенклатуру с актуальным Артикул_товара
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var addedNomenclature = JsonSerializer.Deserialize<Nomenclature>(responseJson, options);

                    if (addedNomenclature != null)
                    {
                        Nomenclatures.Add(addedNomenclature); // Добавляем новый элемент в коллекцию
                        UpdateCategories();
                        NomenclaturesView?.Refresh();
                    }

                    MessageBox.Show("Номенклатура успешно добавлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка добавления номенклатуры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Ошибка обработки данных номенклатуры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EditNomenclature_Click(object sender, RoutedEventArgs e)
        {
            if (NomenclatureDataGrid.SelectedItem is Nomenclature selectedNomenclature)
            {
                // Создаем глубокую копию для редактирования
                var jsonString = JsonSerializer.Serialize(selectedNomenclature);
                var nomenclatureCopy = JsonSerializer.Deserialize<Nomenclature>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var viewModel = new NomenclatureViewModel(nomenclatureCopy);
                var addEditWindow = new AddEditNomenclatureWindow(viewModel);
                
                addEditWindow.Owner = Window.GetWindow(this);
                addEditWindow.ShowDialog();

                if (viewModel.Nomenclature != null && !viewModel.Nomenclature.Equals(selectedNomenclature))
                {
                    try
                    {
                        var jsonContent = new StringContent(
                            JsonSerializer.Serialize(viewModel.Nomenclature),
                            Encoding.UTF8,
                            new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
                        );

                        // Предполагаем, что API ожидает PUT запрос для обновления
                        var response = await _httpClient.PutAsync($"{ApiBaseUrl}nomenclature/{viewModel.Nomenclature.Артикул_товара}", jsonContent);
                        response.EnsureSuccessStatusCode();

                        // Обновляем элемент в ObservableCollection
                        var index = Nomenclatures.IndexOf(selectedNomenclature);
                        if (index != -1)
                        {
                            Nomenclatures[index] = viewModel.Nomenclature;
                            UpdateCategories();
                            NomenclaturesView?.Refresh();
                        }

                        MessageBox.Show("Номенклатура успешно обновлена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (HttpRequestException ex)
                    {
                        MessageBox.Show($"Ошибка обновления номенклатуры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"Ошибка обработки данных номенклатуры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите элемент номенклатуры для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void DeleteNomenclature_Click(object sender, RoutedEventArgs e)
        {
            if (NomenclatureDataGrid.SelectedItem is Nomenclature selectedNomenclature)
            {
                MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить номенклатуру '{selectedNomenclature.Наименование}'?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _httpClient.DeleteAsync($"{ApiBaseUrl}nomenclature/{selectedNomenclature.Артикул_товара}");
                        response.EnsureSuccessStatusCode();

                        Nomenclatures.Remove(selectedNomenclature);
                        UpdateCategories();
                        NomenclaturesView?.Refresh();
                        MessageBox.Show("Номенклатура успешно удалена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (HttpRequestException ex)
                    {
                        MessageBox.Show($"Ошибка удаления номенклатуры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"Ошибка обработки данных номенклатуры: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите элемент номенклатуры для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearNomenclatureFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchName = string.Empty;
            MinPriceText = string.Empty;
            MaxPriceText = string.Empty;

            if (Categories.Any())
            {
                SelectedCategory = Categories.FirstOrDefault();
            }
            else
            {
                SelectedCategory = null;
            }

            NomenclaturesView?.Refresh();
        }

        /// <summary>
        /// Обновляет видимость нижних кнопок в зависимости от выбранного статуса производства.
        /// </summary>
        private void UpdateButtonsForProductionStatus()
        {
            if (AddNomenclatureButton == null || EditNomenclatureButton == null || DeleteNomenclatureButton == null || ReinstateNomenclatureButton == null)
                return;

            bool showingProduced = SelectedProductionStatus != "Снятые с производства";

            AddNomenclatureButton.Visibility = showingProduced ? Visibility.Visible : Visibility.Collapsed;
            EditNomenclatureButton.Visibility = showingProduced ? Visibility.Visible : Visibility.Collapsed;
            DeleteNomenclatureButton.Visibility = showingProduced ? Visibility.Visible : Visibility.Collapsed;

            ReinstateNomenclatureButton.Visibility = showingProduced ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void ReinstateNomenclature_Click(object sender, RoutedEventArgs e)
        {
            if (NomenclatureDataGrid.SelectedItem is not Nomenclature selectedNomenclature)
            {
                MessageBox.Show("Пожалуйста, выберите номенклатуру для возврата в производство.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите вернуть номенклатуру \"{selectedNomenclature.Наименование}\" в производство?",
                "Подтверждение возврата в производство",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var response = await _httpClient.PutAsync($"{ApiBaseUrl}nomenclature/{selectedNomenclature.Артикул_товара}/reinstate", null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(errorText))
                    {
                        MessageBox.Show(errorText, "Ошибка возврата номенклатуры в производство", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка возврата номенклатуры в производство: {response.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                // После успешного возврата обновляем список снятой номенклатуры
                LoadNomenclatures();
                MessageBox.Show("Номенклатура успешно возвращена в производство. Чтобы увидеть её среди производимых товаров, переключите фильтр статуса.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка соединения с сервером при возврате номенклатуры в производство: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при возврате номенклатуры в производство: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}