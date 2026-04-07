using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using FurnitureCRMClient.Models; // Используем новую модель Staff
using System.Text; // Добавляем для Encoding
using System.Linq; // Добавляем для Any() и Max()
using FurnitureCRMClient.ViewModels; // Добавляем using для доступа к StaffViewModel
using System.ComponentModel;
using System.Windows.Data;
using System.Net;

namespace FurnitureCRMClient.Views
{
    public partial class EmployeeManagementUserControl : UserControl, INotifyPropertyChanged
    {
        private readonly AuthenticatedUser _currentUser;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "http://localhost:5028/api/";

        public ObservableCollection<Staff> Employees { get; set; }

        public ICollectionView EmployeesView { get; private set; }

        public ObservableCollection<string> Positions { get; set; } = new ObservableCollection<string>();

        // Список статусов занятости: работники в штате или уволенные
        public ObservableCollection<string> EmploymentStatuses { get; set; } = new ObservableCollection<string>
        {
            "Работающие сотрудники",
            "Уволенные сотрудники"
        };

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    EmployeesView?.Refresh();
                }
            }
        }

        private string _selectedPosition;
        public string SelectedPosition
        {
            get => _selectedPosition;
            set
            {
                if (_selectedPosition != value)
                {
                    _selectedPosition = value;
                    OnPropertyChanged(nameof(SelectedPosition));
                    EmployeesView?.Refresh();
                }
            }
        }

        private string _selectedEmploymentStatus;
        public string SelectedEmploymentStatus
        {
            get => _selectedEmploymentStatus;
            set
            {
                if (_selectedEmploymentStatus != value)
                {
                    _selectedEmploymentStatus = value;
                    OnPropertyChanged(nameof(SelectedEmploymentStatus));

                    // Перезагружаем список сотрудников под выбранный статус
                    LoadEmployees();

                    // Обновляем видимость кнопок
                    UpdateButtonsForEmploymentStatus();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public EmployeeManagementUserControl(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            Employees = new ObservableCollection<Staff>();

            EmployeesView = CollectionViewSource.GetDefaultView(Employees);
            EmployeesView.Filter = EmployeeFilter;

            this.DataContext = this;

            // По умолчанию показываем работающих сотрудников
            SelectedEmploymentStatus = EmploymentStatuses.FirstOrDefault() ?? "Работающие сотрудники";
            UpdateButtonsForEmploymentStatus();
        }

        private async void LoadEmployees()
        {
            // Запоминаем текущий выбранный фильтр по должности,
            // чтобы не сбрасывать его после перезагрузки данных
            var previousSelectedPosition = SelectedPosition;

            try
            {
                // Выбираем нужный эндпоинт в зависимости от статуса сотрудников
                var endpoint = SelectedEmploymentStatus == "Уволенные сотрудники"
                    ? "staff/fired"
                    : "staff";

                var response = await _httpClient.GetAsync($"{ApiBaseUrl}{endpoint}");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var employeesList = JsonSerializer.Deserialize<List<Staff>>(jsonString, options); // Десериализуем в List<Staff>

                Employees.Clear();
                if (employeesList != null)
                {
                    foreach (var emp in employeesList)
                    {
                        Employees.Add(emp);
                    }
                }

                UpdatePositions(previousSelectedPosition);
                EmployeesView?.Refresh();
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка загрузки сотрудников: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Ошибка обработки данных сотрудников: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool EmployeeFilter(object item)
        {
            if (item is not Staff staff)
                return false;

            bool matchesName = string.IsNullOrWhiteSpace(SearchText)
                               || (!string.IsNullOrWhiteSpace(staff.FullName)
                                   && staff.FullName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);

            bool matchesPosition = string.IsNullOrWhiteSpace(SelectedPosition)
                                   || SelectedPosition == "Все должности"
                                   || (staff.Position != null
                                       && staff.Position.Equals(SelectedPosition, StringComparison.OrdinalIgnoreCase));

            return matchesName && matchesPosition;
        }

        private void UpdatePositions(string? previousSelectedPosition = null)
        {
            // Сохраняем "желаемое" значение фильтра:
            // либо переданное явно, либо текущее выбранное
            var targetSelectedPosition = previousSelectedPosition ?? SelectedPosition;

            var distinctPositions = Employees
                .Where(e => !string.IsNullOrWhiteSpace(e.Position))
                .Select(e => e.Position)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            Positions.Clear();
            Positions.Add("Все должности");

            foreach (var pos in distinctPositions)
            {
                Positions.Add(pos);
            }

            // Если ранее выбранная должность всё ещё существует — восстанавливаем её
            if (!string.IsNullOrWhiteSpace(targetSelectedPosition) &&
                Positions.Contains(targetSelectedPosition))
            {
                SelectedPosition = targetSelectedPosition;
            }
            // Иначе, если фильтр ещё вообще не выбран — ставим "Все должности"
            else if (string.IsNullOrWhiteSpace(SelectedPosition))
            {
                SelectedPosition = Positions.FirstOrDefault();
            }
        }

        private async void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            var newStaff = new Staff { StaffId = GetNextStaffId() }; // Генерация временного ID
            var viewModel = new AddStaffViewModel();
            var addEditWindow = new AddEditStaffWindow(viewModel);
            
            addEditWindow.Owner = Window.GetWindow(this);
            addEditWindow.ShowDialog();

            if (viewModel.Staff != null)
            {
                try
                {
                    var jsonContent = new StringContent(
                        JsonSerializer.Serialize(viewModel.Staff),
                        Encoding.UTF8,
                        new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
                    );

                    var response = await _httpClient.PostAsync($"{ApiBaseUrl}staff", jsonContent);
                    response.EnsureSuccessStatusCode();

                    LoadEmployees(); // Перезагружаем список после добавления
                    MessageBox.Show("Сотрудник успешно добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"Ошибка добавления сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"Ошибка обработки данных сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private int GetNextStaffId()
        {
            if (Employees.Any())
            {
                return Employees.Max(s => s.StaffId) + 1;
            }
            return 1;
        }

        private async void EditEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesDataGrid.SelectedItem is Staff selectedStaff)
            {
                var jsonString = JsonSerializer.Serialize(selectedStaff);
                var staffCopy = JsonSerializer.Deserialize<Staff>(jsonString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (staffCopy.UserAccount == null)
                {
                    staffCopy.UserAccount = new UserAccount();
                    staffCopy.UserAccount.StaffId = staffCopy.StaffId;
                }

                // Собираем логины всех остальных сотрудников, чтобы проверять уникальность логина на клиенте
                var existingUsernames = Employees
                    .Where(emp => emp.UserAccount != null
                                  && emp.StaffId != staffCopy.StaffId
                                  && !string.IsNullOrWhiteSpace(emp.UserAccount.Username))
                    .Select(emp => emp.UserAccount.Username)
                    .ToList();

                var viewModel = new EditStaffViewModel(staffCopy, existingUsernames);
                var editStaffWindow = new EditStaffWindow(viewModel);
                
                editStaffWindow.Owner = Window.GetWindow(this);
                editStaffWindow.ShowDialog();

                if (viewModel.Staff != null && !viewModel.Staff.Equals(selectedStaff))
                {
                    try
                    {
                        var jsonContent = new StringContent(
                            JsonSerializer.Serialize(viewModel.Staff),
                            Encoding.UTF8,
                            new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")
                        );

                        var response = await _httpClient.PutAsync($"{ApiBaseUrl}staff/{viewModel.Staff.StaffId}", jsonContent);

                        if (!response.IsSuccessStatusCode)
                        {
                            var errorText = await response.Content.ReadAsStringAsync();

                            if (response.StatusCode == HttpStatusCode.Conflict)
                            {
                                // Конфликт логина — показываем понятное сообщение
                                if (!string.IsNullOrWhiteSpace(errorText))
                                {
                                    MessageBox.Show(errorText, "Ошибка обновления сотрудника", MessageBoxButton.OK, MessageBoxImage.Warning);
                                }
                                else
                                {
                                    MessageBox.Show("Невозможно сохранить изменения: пользователь с таким логином уже существует.", "Ошибка обновления сотрудника", MessageBoxButton.OK, MessageBoxImage.Warning);
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrWhiteSpace(errorText))
                                {
                                    MessageBox.Show(errorText, "Ошибка обновления сотрудника", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                else
                                {
                                    MessageBox.Show($"Ошибка обновления сотрудника: {response.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }

                            return;
                        }

                        var index = Employees.IndexOf(selectedStaff);
                        if (index != -1)
                        {
                            Employees[index] = viewModel.Staff;
                        }

                        MessageBox.Show("Сотрудник успешно обновлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (HttpRequestException ex)
                    {
                        MessageBox.Show($"Ошибка обновления сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"Ошибка обработки данных сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите сотрудника для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void DeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesDataGrid.SelectedItem is Staff selectedStaff)
            {
                MessageBoxResult result = MessageBox.Show($"Вы уверены, что хотите удалить сотрудника '{selectedStaff.FullName}'?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Предварительная проверка на активные заказы на стороне приложения
                        bool hasActiveClientOrders = await HasActiveCustomerOrdersAsync(selectedStaff.StaffId);
                        bool hasActiveMaterialOrders = await HasActiveMaterialOrdersAsync(selectedStaff.StaffId);
                        if (hasActiveClientOrders || hasActiveMaterialOrders)
                        {
                            string msg = "Нельзя удалить (уволить) сотрудника, так как у него есть незавершённые ";
                            if (hasActiveClientOrders && hasActiveMaterialOrders)
                                msg += "заказы клиентов и заказы на закупку сырья.";
                            else if (hasActiveClientOrders)
                                msg += "заказы клиентов.";
                            else
                                msg += "заказы на закупку сырья.";

                            MessageBox.Show(msg, "Удаление запрещено", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var response = await _httpClient.DeleteAsync($"{ApiBaseUrl}staff/{selectedStaff.StaffId}");
                        if (!response.IsSuccessStatusCode)
                        {
                            var errorText = await response.Content.ReadAsStringAsync();
                            if (!string.IsNullOrWhiteSpace(errorText))
                            {
                                MessageBox.Show(errorText, "Ошибка удаления сотрудника", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                            else
                            {
                                MessageBox.Show($"Ошибка удаления сотрудника: {response.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            return;
                        }

                        Employees.Remove(selectedStaff);
                        MessageBox.Show("Сотрудник успешно удален (отмечен как уволенный).", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (HttpRequestException ex)
                    {
                        MessageBox.Show($"Ошибка удаления сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"Ошибка обработки данных сотрудника: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите сотрудника для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Проверка: есть ли у сотрудника активные заказы клиентов (статус не "Выполнен" и не "Отменен").
        /// </summary>
        private async Task<bool> HasActiveCustomerOrdersAsync(int staffId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}orders");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var orders = JsonSerializer.Deserialize<List<Order>>(json, options) ?? new List<Order>();

                return orders.Any(o => o.StaffId == staffId &&
                                       o.Status != "Выполнен" &&
                                       o.Status != "Отменен");
            }
            catch (Exception)
            {
                // Если не удалось проверить на клиенте, дадим решить это серверу/триггеру
                return false;
            }
        }

        /// <summary>
        /// Проверка: есть ли у сотрудника активные заказы на закупку сырья (статус не "Доставлен" и не "Отменен").
        /// </summary>
        private async Task<bool> HasActiveMaterialOrdersAsync(int staffId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBaseUrl}materialorders");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var orders = JsonSerializer.Deserialize<List<MaterialOrder>>(json, options) ?? new List<MaterialOrder>();

                return orders.Any(o => o.ID_оформляющего_сотрудника == staffId &&
                                       o.Статус != "Доставлен" &&
                                       o.Статус != "Отменен");
            }
            catch (Exception)
            {
                // Если проверка не удалась, пусть окончательно решит сервер/триггер
                return false;
            }
        }

        private void CopyLogin_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Staff staff)
            {
                if (staff.UserAccount != null)
                {
                    Clipboard.SetText(staff.UserAccount.Username);
                    MessageBox.Show("Логин скопирован в буфер обмена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Учетная запись не найдена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void CopyPassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is Staff staff)
            {
                if (staff.UserAccount != null)
                {
                    Clipboard.SetText(staff.UserAccount.Password);
                    MessageBox.Show("Пароль скопирован в буфер обмена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Учетная запись не найдена.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ClearEmployeeFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchText = string.Empty;

            if (Positions.Any())
            {
                SelectedPosition = Positions.FirstOrDefault();
            }
            else
            {
                SelectedPosition = null;
            }

            // Статус сотрудников не меняем, чтобы можно было чистить фильтры внутри выбранной вкладки

            EmployeesView?.Refresh();
        }

        /// <summary>
        /// Обновляет видимость кнопок внизу в зависимости от выбранного статуса сотрудников.
        /// </summary>
        private void UpdateButtonsForEmploymentStatus()
        {
            if (AddEmployeeButton == null || EditEmployeeButton == null || DeleteEmployeeButton == null || RehireEmployeeButton == null)
                return;

            bool showingActive = SelectedEmploymentStatus != "Уволенные сотрудники";

            AddEmployeeButton.Visibility = showingActive ? Visibility.Visible : Visibility.Collapsed;
            EditEmployeeButton.Visibility = showingActive ? Visibility.Visible : Visibility.Collapsed;
            DeleteEmployeeButton.Visibility = showingActive ? Visibility.Visible : Visibility.Collapsed;

            RehireEmployeeButton.Visibility = showingActive ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void RehireEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (EmployeesDataGrid.SelectedItem is not Staff selectedStaff)
            {
                MessageBox.Show("Пожалуйста, выберите уволенного сотрудника для возврата в штат.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Вы уверены, что хотите вернуть сотрудника \"{selectedStaff.FullName}\" в штат?",
                "Подтверждение возврата в штат",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                var response = await _httpClient.PutAsync($"{ApiBaseUrl}staff/{selectedStaff.StaffId}/reinstate", null);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(errorText))
                    {
                        MessageBox.Show(errorText, "Ошибка возврата сотрудника в штат", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        MessageBox.Show($"Ошибка возврата сотрудника в штат: {response.StatusCode}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                // После успешного возврата обновляем список уволенных
                LoadEmployees();
                MessageBox.Show("Сотрудник успешно возвращён в штат. Чтобы увидеть его в списке работающих, переключите фильтр статуса.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show($"Ошибка соединения с сервером при возврате сотрудника в штат: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Произошла ошибка при возврате сотрудника в штат: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
