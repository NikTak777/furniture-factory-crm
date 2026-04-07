using System.Windows;
using FurnitureCRMClient.ViewModels;
using FurnitureCRMClient.Models;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FurnitureCRMClient.Views
{
    public partial class AddEditOrderWindow : Window
    {
        public OrderViewModel ViewModel { get; set; }
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/";

        public AddEditOrderWindow(Order orderToEdit)
        {
            InitializeComponent();
            LoadAndSetViewModel(orderToEdit);
        }

        private async void LoadAndSetViewModel(Order orderToEdit)
        {
            List<Nomenclature> allProducts = new();
            List<Client> allClients = new();

            try
            {
                // Загрузка номенклатуры
                var productsResponse = await _httpClient.GetAsync($"{ApiBaseUrl}nomenclature");
                productsResponse.EnsureSuccessStatusCode();
                var productsJson = await productsResponse.Content.ReadAsStringAsync();
                var productOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                allProducts = JsonSerializer.Deserialize<List<Nomenclature>>(productsJson, productOptions) ?? new List<Nomenclature>();

                // Загрузка клиентов
                var clientsResponse = await _httpClient.GetAsync($"{ApiBaseUrl}clients");
                clientsResponse.EnsureSuccessStatusCode();
                var clientsJson = await clientsResponse.Content.ReadAsStringAsync();
                var clientOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                allClients = JsonSerializer.Deserialize<List<Client>>(clientsJson, clientOptions) ?? new List<Client>();
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка загрузки справочных данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
                return;
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошибка обработки справочных данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
                return;
            }
            
            ViewModel = new OrderViewModel(orderToEdit, allProducts, allClients);
            DataContext = ViewModel;

            if (ViewModel.Order.OrderId == 0)
            {
                Title = "Добавить Заказ";
            }
            else
            {
                Title = "Редактировать Заказ";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Эти строки теперь не нужны, так как ViewModel.SelectedProduct и ViewModel.SelectedClient
            // автоматически обновляют Order.ProductId и Order.ClientId через свои сеттеры
            // ViewModel.Order.ProductId = ViewModel.SelectedProduct?.Артикул_товара ?? 0;
            // ViewModel.Order.ClientId = ViewModel.SelectedClient?.Id ?? 0;
            
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ComboBoxStatus_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && ViewModel != null)
            {
                string? newStatus = e.AddedItems[0] as string;
                if (!string.IsNullOrWhiteSpace(newStatus) && ViewModel.ChangeStatusCommand.CanExecute(newStatus))
                {
                    ViewModel.ChangeStatusCommand.Execute(newStatus);
                    // Обновить ComboBox, если VM откатила выбор
                    (sender as System.Windows.Controls.ComboBox)!.SelectedItem = ViewModel.SelectedStatus;
                }
            }
        }
    }
}
