using FurnitureCRMClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;

namespace FurnitureCRMClient
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient = new();

        public MainWindow()
        {
            InitializeComponent();
            _httpClient.BaseAddress = new Uri("http://192.168.0.1/"); // URL твоего API
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clients = await GetClientsAsync();
                ClientsGrid.ItemsSource = clients;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private async Task<List<Client>> GetClientsAsync()
        {
            var response = await _httpClient.GetAsync("api/clients"); // путь к твоему контроллеру
            response.EnsureSuccessStatusCode();
            var clients = await response.Content.ReadFromJsonAsync<List<Client>>();
            return clients ?? new List<Client>();
        }
    }
}