using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FurnitureCRMClient.Models;
using FurnitureCRMClient.ViewModels;
using System.Text;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Data;

namespace FurnitureCRMClient.Views
{
    public partial class ClientManagementUserControl : UserControl, INotifyPropertyChanged
    {
        private readonly AuthenticatedUser _currentUser;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/";

        public ObservableCollection<Client> Clients { get; set; } = new();
        public ICollectionView ClientsView { get; private set; }

        private string _searchFullName;
        public string SearchFullName
        {
            get => _searchFullName;
            set
            {
                if (_searchFullName != value)
                {
                    _searchFullName = value;
                    OnPropertyChanged(nameof(SearchFullName));
                    ClientsView?.Refresh();
                }
            }
        }

        private string _searchPhone;
        public string SearchPhone
        {
            get => _searchPhone;
            set
            {
                if (_searchPhone != value)
                {
                    _searchPhone = value;
                    OnPropertyChanged(nameof(SearchPhone));
                    ClientsView?.Refresh();
                }
            }
        }

        private string _searchEmail;
        public string SearchEmail
        {
            get => _searchEmail;
            set
            {
                if (_searchEmail != value)
                {
                    _searchEmail = value;
                    OnPropertyChanged(nameof(SearchEmail));
                    ClientsView?.Refresh();
                }
            }
        }

        private string _searchAddress;
        public string SearchAddress
        {
            get => _searchAddress;
            set
            {
                if (_searchAddress != value)
                {
                    _searchAddress = value;
                    OnPropertyChanged(nameof(SearchAddress));
                    ClientsView?.Refresh();
                }
            }
        }

        private DateTime? _registrationDateFrom;
        public DateTime? RegistrationDateFrom
        {
            get => _registrationDateFrom;
            set
            {
                if (_registrationDateFrom != value)
                {
                    _registrationDateFrom = value;
                    OnPropertyChanged(nameof(RegistrationDateFrom));
                    ClientsView?.Refresh();
                }
            }
        }

        private DateTime? _registrationDateTo;
        public DateTime? RegistrationDateTo
        {
            get => _registrationDateTo;
            set
            {
                if (_registrationDateTo != value)
                {
                    _registrationDateTo = value;
                    OnPropertyChanged(nameof(RegistrationDateTo));
                    ClientsView?.Refresh();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ClientManagementUserControl(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;

            ClientsView = CollectionViewSource.GetDefaultView(Clients);
            ClientsView.Filter = ClientFilter;

            this.DataContext = this;
            LoadClients();
        }

        public async void LoadClients()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}clients");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var clientList = JsonSerializer.Deserialize<List<Client>>(jsonString, options);

                Clients.Clear();
                if (clientList != null)
                {
                    foreach (var client in clientList)
                    {
                        Clients.Add(client);
                    }
                }

                ClientsView?.Refresh();
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка загрузки клиентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошибка обработки данных клиентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла непредвиденная ошибка при загрузке клиентов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ClientFilter(object item)
        {
            if (item is not Client c)
                return false;

            bool matchesName = string.IsNullOrWhiteSpace(SearchFullName)
                               || (!string.IsNullOrWhiteSpace(c.FullName)
                                   && c.FullName.IndexOf(SearchFullName, StringComparison.OrdinalIgnoreCase) >= 0);

            string phoneString = c.Phone.ToString();
            bool matchesPhone = string.IsNullOrWhiteSpace(SearchPhone)
                                || (!string.IsNullOrWhiteSpace(phoneString)
                                    && phoneString.Contains(SearchPhone));

            bool matchesEmail = string.IsNullOrWhiteSpace(SearchEmail)
                                || (!string.IsNullOrWhiteSpace(c.Email)
                                    && c.Email.IndexOf(SearchEmail, StringComparison.OrdinalIgnoreCase) >= 0);

            bool matchesAddress = string.IsNullOrWhiteSpace(SearchAddress)
                                  || (!string.IsNullOrWhiteSpace(c.Address)
                                      && c.Address.IndexOf(SearchAddress, StringComparison.OrdinalIgnoreCase) >= 0);

            bool matchesRegFrom = !RegistrationDateFrom.HasValue
                                  || c.RegistrationDate.Date >= RegistrationDateFrom.Value.Date;

            bool matchesRegTo = !RegistrationDateTo.HasValue
                                || c.RegistrationDate.Date <= RegistrationDateTo.Value.Date;

            return matchesName
                   && matchesPhone
                   && matchesEmail
                   && matchesAddress
                   && matchesRegFrom
                   && matchesRegTo;
        }

        private async void AddClient_Click(object sender, RoutedEventArgs e)
        {
            var newClient = new Client { RegistrationDate = DateTime.Now }; // ID will be generated by API
            // Формируем списки существующих телефонов и email для проверки уникальности
            var existingPhones = Clients.Select(c => c.Phone);
            var existingEmails = Clients
                .Where(c => !string.IsNullOrWhiteSpace(c.Email))
                .Select(c => c.Email);

            var viewModel = new ClientViewModel(newClient, existingPhones, existingEmails);
            var addEditWindow = new AddEditClientWindow(viewModel) { Owner = Window.GetWindow(this) };
            
            if (addEditWindow.ShowDialog() == true && viewModel.Client != null)
            {
                try
                {
                    var jsonContent = new StringContent(
                        JsonSerializer.Serialize(viewModel.Client),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var response = await _httpClient.PostAsync($"{ApiBaseUrl}clients", jsonContent);
                    response.EnsureSuccessStatusCode();

                    LoadClients(); // Reload to get the new client with its generated ID
                    MessageBox.Show("Клиент успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка добавления клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Ошибка обработки данных клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла непредвиденная ошибка при добавлении клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EditClient_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is not Client selectedClient)
            {
                MessageBox.Show("Пожалуйста, выберите клиента для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create a copy to avoid modifying the original in the DataGrid directly
            var clientCopy = new Client
            {
                Id = selectedClient.Id,
                FullName = selectedClient.FullName,
                Phone = selectedClient.Phone,
                Email = selectedClient.Email,
                Address = selectedClient.Address,
                RegistrationDate = selectedClient.RegistrationDate
            };

            // Для проверки уникальности исключаем редактируемого клиента
            var existingPhones = Clients
                .Where(c => c.Id != selectedClient.Id)
                .Select(c => c.Phone);
            var existingEmails = Clients
                .Where(c => c.Id != selectedClient.Id && !string.IsNullOrWhiteSpace(c.Email))
                .Select(c => c.Email);

            var viewModel = new ClientViewModel(clientCopy, existingPhones, existingEmails);
            var addEditWindow = new AddEditClientWindow(viewModel) { Owner = Window.GetWindow(this) };

            if (addEditWindow.ShowDialog() == true && viewModel.Client != null)
            {
                try
                {
                    var jsonContent = new StringContent(
                        JsonSerializer.Serialize(viewModel.Client),
                        Encoding.UTF8,
                        "application/json"
                    );

                    var response = await _httpClient.PutAsync($"{ApiBaseUrl}clients/{viewModel.Client.Id}", jsonContent);
                    response.EnsureSuccessStatusCode();

                    // Update the ObservableCollection to reflect changes in UI
                    var index = Clients.IndexOf(selectedClient);
                    if (index != -1)
                    {
                        Clients[index] = viewModel.Client;
                    }
                    
                    MessageBox.Show("Клиент успешно обновлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка обновления клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Ошибка обработки данных клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла непредвиденная ошибка при обновлении клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsDataGrid.SelectedItem is not Client selectedClient)
            {
                MessageBox.Show("Пожалуйста, выберите клиента для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Вы уверены, что хотите удалить клиента '{selectedClient.FullName}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _httpClient.DeleteAsync($"{ApiBaseUrl}clients/{selectedClient.Id}");
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        MessageBox.Show(errorMessage, "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    response.EnsureSuccessStatusCode();

                    Clients.Remove(selectedClient);
                    MessageBox.Show("Клиент успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка удаления клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Ошибка обработки данных клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла непредвиденная ошибка при удалении клиента: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ClearClientFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchFullName = string.Empty;
            SearchPhone = string.Empty;
            SearchEmail = string.Empty;
            SearchAddress = string.Empty;
            RegistrationDateFrom = null;
            RegistrationDateTo = null;

            ClientsView?.Refresh();
        }
    }
}
