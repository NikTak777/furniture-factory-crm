using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FurnitureCRMClient.Models;
using System.Text;
using System.Linq;
using FurnitureCRMClient.ViewModels;
using FurnitureCRMClient.Views;
using System; // Добавляем using для Action
using System.ComponentModel;
using System.Windows.Data;

namespace FurnitureCRMClient.Views
{
    /// <summary>
    /// Interaction logic for MaterialStockManagementUserControl.xaml
    /// </summary>
    public partial class MaterialStockManagementUserControl : UserControl, INotifyPropertyChanged
    {
        public event Action MaterialsListChanged;
        private readonly AuthenticatedUser _currentUser;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/"; // URL твоего API

        public ObservableCollection<Material> Materials { get; set; }

        public ICollectionView MaterialsView { get; private set; }

        public ObservableCollection<string> Units { get; set; } = new ObservableCollection<string>();

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
                    MaterialsView?.Refresh();
                }
            }
        }

        private string _selectedUnit;
        public string SelectedUnit
        {
            get => _selectedUnit;
            set
            {
                if (_selectedUnit != value)
                {
                    _selectedUnit = value;
                    OnPropertyChanged(nameof(SelectedUnit));
                    MaterialsView?.Refresh();
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
                    MaterialsView?.Refresh();
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
                    MaterialsView?.Refresh();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MaterialStockManagementUserControl(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            Materials = new ObservableCollection<Material>();

            MaterialsView = CollectionViewSource.GetDefaultView(Materials);
            MaterialsView.Filter = MaterialFilter;

            this.DataContext = this;

            LoadMaterials();
        }

        private async void LoadMaterials()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}materials");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var materialList = JsonSerializer.Deserialize<List<Material>>(jsonString, options);

                Materials.Clear();
                if (materialList != null)
                {
                    foreach (var item in materialList)
                    {
                        Materials.Add(item);
                    }
                }

                UpdateUnits();
                MaterialsView?.Refresh();
                MaterialsListChanged?.Invoke(); // Вызываем событие после загрузки материалов
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка загрузки сырья: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошибка обработки данных сырья: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool MaterialFilter(object item)
        {
            if (item is not Material m)
                return false;

            bool matchesName = string.IsNullOrWhiteSpace(SearchName)
                               || (!string.IsNullOrWhiteSpace(m.Наименование_материала)
                                   && m.Наименование_материала.IndexOf(SearchName, StringComparison.OrdinalIgnoreCase) >= 0);

            bool matchesUnit = string.IsNullOrWhiteSpace(SelectedUnit)
                               || SelectedUnit == "Все единицы"
                               || (!string.IsNullOrWhiteSpace(m.Единица_измерения)
                                   && m.Единица_измерения.Equals(SelectedUnit, StringComparison.OrdinalIgnoreCase));

            int? minQty = TryParseQuantity(MinQuantityText);
            int? maxQty = TryParseQuantity(MaxQuantityText);

            bool matchesMinQty = !minQty.HasValue || m.Количество_в_наличии >= minQty.Value;
            bool matchesMaxQty = !maxQty.HasValue || m.Количество_в_наличии <= maxQty.Value;

            return matchesName && matchesUnit && matchesMinQty && matchesMaxQty;
        }

        private int? TryParseQuantity(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (int.TryParse(text, out var value) && value >= 0)
                return value;

            return null;
        }

        private void UpdateUnits()
        {
            var distinctUnits = Materials
                .Where(m => !string.IsNullOrWhiteSpace(m.Единица_измерения))
                .Select(m => m.Единица_измерения)
                .Distinct()
                .OrderBy(u => u)
                .ToList();

            Units.Clear();
            Units.Add("Все единицы");

            foreach (var unit in distinctUnits)
            {
                Units.Add(unit);
            }

            if (string.IsNullOrWhiteSpace(SelectedUnit))
            {
                SelectedUnit = Units.FirstOrDefault();
            }
        }

        private async void AddMaterial_Click(object sender, RoutedEventArgs e)
        {
            var newMaterial = new Material { Артикул_сырья = 0 }; // Устанавливаем 0 или другое значение по умолчанию, которое API проигнорирует
            var viewModel = new MaterialStockViewModel(newMaterial);
            var addEditWindow = new AddEditMaterialWindow(viewModel);
            
            addEditWindow.Owner = Window.GetWindow(this);
            addEditWindow.ShowDialog();

            if (viewModel.Material != null)
            {
                try
                {
                    var jsonContent = new StringContent(
                        JsonSerializer.Serialize(viewModel.Material),
                        Encoding.UTF8,
                        new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
                    );

                    var response = await _httpClient.PostAsync($"{ApiBaseUrl}materials", jsonContent);
                    response.EnsureSuccessStatusCode();

                    LoadMaterials(); // Это уже вызывает MaterialsListChanged
                    MessageBox.Show("Сырье успешно добавлено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка добавления сырья: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Ошибка обработки данных сырья: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EditMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialDataGrid.SelectedItem is Material selectedMaterial)
            {
                var jsonString = JsonSerializer.Serialize(selectedMaterial);
                var materialCopy = JsonSerializer.Deserialize<Material>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var viewModel = new MaterialStockViewModel(materialCopy);
                var addEditWindow = new AddEditMaterialWindow(viewModel);
                
                addEditWindow.Owner = Window.GetWindow(this);
                addEditWindow.ShowDialog();

                if (viewModel.Material != null && !viewModel.Material.Equals(selectedMaterial))
                {
                    try
                    {
                        var jsonContent = new StringContent(
                            JsonSerializer.Serialize(viewModel.Material),
                            Encoding.UTF8,
                            new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
                        );

                        var response = await _httpClient.PutAsync($"{ApiBaseUrl}materials/{viewModel.Material.Артикул_сырья}", jsonContent);
                        response.EnsureSuccessStatusCode();

                        var index = Materials.IndexOf(selectedMaterial);
                        if (index != -1)
                        {
                            Materials[index] = viewModel.Material;
                        }
                        MaterialsListChanged?.Invoke(); // Вызываем событие после редактирования материала

                        MessageBox.Show("Сырье успешно обновлено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (HttpRequestException ex)
                    {
                        MessageBox.Show($"Ошибка обновления сырья: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"Ошибка обработки данных сырья: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите элемент сырья для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void DeleteMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (MaterialDataGrid.SelectedItem is Material selectedMaterial)
            {
                MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить сырье '{selectedMaterial.Наименование_материала}'?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _httpClient.DeleteAsync($"{ApiBaseUrl}materials/{selectedMaterial.Артикул_сырья}");
                        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                        {
                            string errorMessage = await response.Content.ReadAsStringAsync();
                            MessageBox.Show(errorMessage, "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        response.EnsureSuccessStatusCode();

                        Materials.Remove(selectedMaterial);
                        MaterialsListChanged?.Invoke(); // Вызываем событие после удаления материала
                        MessageBox.Show("Сырье успешно удалено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (HttpRequestException ex)
                    {
                        MessageBox.Show($"Ошибка удаления сырья: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"Ошибка обработки данных сырья: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите элемент сырья для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void ReloadMaterials()
        {
            LoadMaterials();
        }

        private void ClearMaterialFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchName = string.Empty;
            MinQuantityText = string.Empty;
            MaxQuantityText = string.Empty;

            if (Units.Any())
            {
                SelectedUnit = Units.FirstOrDefault();
            }
            else
            {
                SelectedUnit = null;
            }

            MaterialsView?.Refresh();
        }
    }
}
