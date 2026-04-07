using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FurnitureCRMClient.Models;
using System.Linq;
using System.ComponentModel;
using System.Windows.Data;

namespace FurnitureCRMClient.Views
{
    public partial class ManagerNomenclatureTab : UserControl, INotifyPropertyChanged
    {
        private readonly AuthenticatedUser _currentUser;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/";

        public ObservableCollection<Nomenclature> Nomenclatures { get; set; }
        public ICollectionView NomenclaturesView { get; private set; }
        public ObservableCollection<string> Categories { get; set; } = new ObservableCollection<string>();

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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ManagerNomenclatureTab(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            Nomenclatures = new ObservableCollection<Nomenclature>();

            NomenclaturesView = CollectionViewSource.GetDefaultView(Nomenclatures);
            NomenclaturesView.Filter = NomenclatureFilter;

            this.DataContext = this;
            LoadNomenclatures();
        }

        private async void LoadNomenclatures()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}nomenclature");
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

            // Показываем только номенклатуру, которая сейчас производится
            if (!n.Производится)
                return false;

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
    }
}
