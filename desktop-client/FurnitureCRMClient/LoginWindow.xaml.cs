using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using FurnitureCRMClient.Models;
using FurnitureCRMClient.Views;

namespace FurnitureCRMClient
{
    public partial class LoginWindow : Window
    {
        private readonly HttpClient _httpClient;
        private AuthenticatedUser _currentUser;
        private const string ApiBaseUrl = "http://localhost:5028/api/";

        public LoginWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
        }

        private void ShowError(string message)
        {
            ErrorToast.Show(message);
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string login = LoginTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Пожалуйста, введите логин и пароль.");
                return;
            }

            // Блокируем кнопку и поля во время входа
            LoginButton.IsEnabled = false;
            LoginTextBox.IsEnabled = false;
            PasswordBox.IsEnabled = false;

            try
            {
            // --- Блок исключения для быстрого входа "admin admin" (ТОЛЬКО ДЛЯ РАЗРАБОТКИ!) ---
            if (login == "admin" && password == "admin")
            {
                var adminUser = new AuthenticatedUser
                {
                        ID_сотрудника = 0,
                    ФИО = "Тестовый Директор",
                        Должность = "Директор"
                };
                    await StartLoginTransition(adminUser);
                    return;
            }
            // --- Конец блока исключения ---

                var loginRequest = new { Login = login, Password = password };
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(loginRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{ApiBaseUrl}auth/login", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var authenticatedUser = JsonSerializer.Deserialize<AuthenticatedUser>(responseContent, options);

                    if (authenticatedUser != null)
                    {
                        await StartLoginTransition(authenticatedUser);
                    }
                    else
                    {
                        ShowError("Ошибка при получении данных пользователя. Возможно, неверный формат ответа API.");
                        LoginButton.IsEnabled = true;
                        LoginTextBox.IsEnabled = true;
                        PasswordBox.IsEnabled = true;
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();

                    // Специальная обработка случая, когда сотрудник уволен / учетная запись неактивна
                    if ((int)response.StatusCode == 403)
                    {
                        // Для пользователя показываем только понятное сообщение с сервера,
                        // без технического статуса.
                        var message = string.IsNullOrWhiteSpace(errorResponse)
                            ? "Вход невозможен: ваша учетная запись деактивирована. Обратитесь к руководству."
                            : errorResponse;

                        ShowError(message);
                    }
                    else
                    {
                        ShowError($"Ошибка входа: {response.StatusCode} - {errorResponse}");
                    }

                    LoginButton.IsEnabled = true;
                    LoginTextBox.IsEnabled = true;
                    PasswordBox.IsEnabled = true;
                }
            }
            catch (HttpRequestException ex)
            {
                ShowError($"Ошибка соединения с сервером. Проверьте URL API и его доступность: {ex.Message}");
                LoginButton.IsEnabled = true;
                LoginTextBox.IsEnabled = true;
                PasswordBox.IsEnabled = true;
            }
            catch (JsonException ex)
            {
                ShowError($"Ошибка обработки данных от сервера: {ex.Message}");
                LoginButton.IsEnabled = true;
                LoginTextBox.IsEnabled = true;
                PasswordBox.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ShowError($"Произошла непредвиденная ошибка: {ex.Message}");
                LoginButton.IsEnabled = true;
                LoginTextBox.IsEnabled = true;
                PasswordBox.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task StartLoginTransition(AuthenticatedUser user)
        {
            _currentUser = user;

            // Шаг 1: Скрываем форму логина
            await HideLoginFormAsync();

            // Шаг 2: Сразу расширяем окно на весь экран (без анимации)
            ExpandWindowToFullScreen();

            // Шаг 3: Показываем "Вход успешно выполнен"
            await ShowSuccessMessageAsync();

            // Шаг 4: Показываем приветствие
            await ShowWelcomeMessageAsync(user);

            // Шаг 5: Скрываем приветствие и показываем панель управления
            await ShowMainPanelAsync(user);
        }

        private async System.Threading.Tasks.Task HideLoginFormAsync()
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeOut, LoginFormGrid);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeOut);

            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            storyboard.Completed += (s, e) =>
            {
                LoginFormGrid.Visibility = Visibility.Collapsed;
                tcs.SetResult(true);
            };

            storyboard.Begin();
            await tcs.Task;
        }

        private async System.Threading.Tasks.Task ShowSuccessMessageAsync()
        {
            WelcomeTextBlock.Text = "Вход успешно выполнен";

            WelcomeGrid.Visibility = Visibility.Visible;
            WelcomeGrid.Opacity = 0;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500)
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeIn, WelcomeGrid);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            storyboard.Completed += (s, e) => tcs.SetResult(true);

            storyboard.Begin();
            await tcs.Task;

            // Ждем 300 мс перед следующим шагом
            await System.Threading.Tasks.Task.Delay(300);

            // Скрываем сообщение об успехе
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            var storyboardOut = new Storyboard();
            Storyboard.SetTarget(fadeOut, WelcomeGrid);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(OpacityProperty));
            storyboardOut.Children.Add(fadeOut);

            var tcsOut = new System.Threading.Tasks.TaskCompletionSource<bool>();
            storyboardOut.Completed += (s, e) => tcsOut.SetResult(true);

            storyboardOut.Begin();
            await tcsOut.Task;
        }

        private async System.Threading.Tasks.Task ShowWelcomeMessageAsync(AuthenticatedUser user)
        {
            WelcomeTextBlock.Text = $"Добро пожаловать, {user.Должность.Trim()} {user.ФИО}!";

            WelcomeGrid.Opacity = 0;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500)
            };

            var storyboard = new Storyboard();
            Storyboard.SetTarget(fadeIn, WelcomeGrid);
            Storyboard.SetTargetProperty(fadeIn, new PropertyPath(OpacityProperty));
            storyboard.Children.Add(fadeIn);

            var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
            storyboard.Completed += (s, e) => tcs.SetResult(true);

            storyboard.Begin();
            await tcs.Task;

            // Ждем 1 секунду перед следующим шагом
            await System.Threading.Tasks.Task.Delay(1000);
        }

        private void ExpandWindowToFullScreen()
        {
            this.WindowState = WindowState.Normal;
            this.SizeToContent = SizeToContent.Manual;
            this.WindowState = WindowState.Maximized;
        }

        private async System.Threading.Tasks.Task ShowMainPanelAsync(AuthenticatedUser user)
        {
            // Скрываем приветствие
            var fadeOutWelcome = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            var storyboard1 = new Storyboard();
            Storyboard.SetTarget(fadeOutWelcome, WelcomeGrid);
            Storyboard.SetTargetProperty(fadeOutWelcome, new PropertyPath(OpacityProperty));
            storyboard1.Children.Add(fadeOutWelcome);

            var tcs1 = new System.Threading.Tasks.TaskCompletionSource<bool>();
            storyboard1.Completed += (s, e) =>
            {
                WelcomeGrid.Visibility = Visibility.Collapsed;
                tcs1.SetResult(true);
            };

            storyboard1.Begin();
            await tcs1.Task;

            // Загружаем панель управления
            LoadMainPanel(user);

            // Показываем панель управления
            MainPanelGrid.Visibility = Visibility.Visible;
            MainPanelGrid.Opacity = 0;

            var fadeInPanel = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(500)
            };

            var storyboard2 = new Storyboard();
            Storyboard.SetTarget(fadeInPanel, MainPanelGrid);
            Storyboard.SetTargetProperty(fadeInPanel, new PropertyPath(OpacityProperty));
            storyboard2.Children.Add(fadeInPanel);

            var tcs2 = new System.Threading.Tasks.TaskCompletionSource<bool>();
            storyboard2.Completed += (s, e) => tcs2.SetResult(true);

            storyboard2.Begin();
            await tcs2.Task;

            // Обновляем заголовок окна
            UpdateWindowTitle(user);
        }

        private void LoadMainPanel(AuthenticatedUser user)
        {
            switch (user.Должность.Trim())
            {
                case "Директор":
                    var directorPanel = new Grid();
                    directorPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    directorPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var directorTabControl = new TabControl { Margin = new Thickness(10) };
                    directorTabControl.Items.Add(new TabItem
                    {
                        Header = "Учёт сотрудников",
                        Content = new EmployeeManagementUserControl(user)
                    });
                    directorTabControl.Items.Add(new TabItem
                    {
                        Header = "Учёт номенклатуры",
                        Content = new NomenclatureManagementUserControl(user)
                    });
                    directorTabControl.Items.Add(new TabItem
                    {
                        Header = "Анализ отчётов о производстве",
                        Content = new DirectorProductionAnalysisTab(user)
                    });
                    directorTabControl.Items.Add(new TabItem
                    {
                        Header = "Анализ продаж персонала",
                        Content = new DirectorStaffSalesAnalysisTab(user)
                    });

                    Grid.SetRow(directorTabControl, 0);
                    directorPanel.Children.Add(directorTabControl);

                    MainPanelContent.Content = directorPanel;
                    break;

                case "Менеджер по производству":
                case "Менеджер":
                    var managerPanel = new Grid();
                    managerPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    managerPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var managerTabControl = new TabControl { Margin = new Thickness(10) };
                    managerTabControl.Items.Add(new TabItem
                    {
                        Header = "Просмотр номенклатуры",
                        Content = new ManagerNomenclatureTab(user)
                    });
                    managerTabControl.Items.Add(new TabItem
                    {
                        Header = "Учёт заказов",
                        Content = new ManagerOrdersTab(user)
                    });
                    managerTabControl.Items.Add(new TabItem
                    {
                        Header = "Учёт клиентов",
                        Content = new ClientManagementUserControl(user)
                    });
                    managerTabControl.Items.Add(new TabItem
                    {
                        Header = "Отчёты о производстве",
                        Content = new ManagerReportsTab(user)
                    });

                    Grid.SetRow(managerTabControl, 0);
                    managerPanel.Children.Add(managerTabControl);

                    MainPanelContent.Content = managerPanel;
                    break;

                case "Кладовщик":
                    var warehousePanel = new Grid();
                    warehousePanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    warehousePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var warehouseTabControl = new TabControl { Margin = new Thickness(10) };
                    var stockUC = new MaterialStockManagementUserControl(user);
                    var purchaseOrderUC = new MaterialPurchaseOrderUserControl(user);

                    warehouseTabControl.Items.Add(new TabItem
                    {
                        Header = "Учёт сырья на складе",
                        Content = stockUC
                    });
                    warehouseTabControl.Items.Add(new TabItem
                    {
                        Header = "Оформление закупки сырья",
                        Content = purchaseOrderUC
                    });
                    warehouseTabControl.Items.Add(new TabItem
                    {
                        Header = "Анализ потребности в материалах",
                        Content = new MaterialNeedsAnalysisUserControl(user)
                    });

                    // Подписки для кладовщика
                    purchaseOrderUC.MaterialOrderChanged += stockUC.ReloadMaterials;
                    stockUC.MaterialsListChanged += purchaseOrderUC.ReloadMaterialsListData;

                    Grid.SetRow(warehouseTabControl, 0);
                    warehousePanel.Children.Add(warehouseTabControl);

                    MainPanelContent.Content = warehousePanel;
                    break;

                default:
                    ShowError($"Ваша роль не определена ({user.Должность}) или не поддерживается. Обратитесь к администратору.");
                    // Возвращаемся к форме логина
                    LoginFormGrid.Visibility = Visibility.Visible;
                    LoginFormGrid.Opacity = 1;
                    LoginButton.IsEnabled = true;
                    LoginTextBox.IsEnabled = true;
                    PasswordBox.IsEnabled = true;
                    return;
            }
        }

        private void UpdateWindowTitle(AuthenticatedUser user)
        {
            this.Title = $"Панель {user.Должность.Trim()} - {user.ФИО}";
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            // Возвращаемся к форме логина
            ResetToLoginForm();
        }

        private void ResetToLoginForm()
        {
            // Останавливаем все анимации
            LoginFormGrid.BeginAnimation(OpacityProperty, null);
            WelcomeGrid.BeginAnimation(OpacityProperty, null);
            MainPanelGrid.BeginAnimation(OpacityProperty, null);

            // Скрываем панель управления
            MainPanelGrid.Visibility = Visibility.Collapsed;
            MainPanelGrid.Opacity = 0;
            MainPanelContent.Content = null;

            // Скрываем приветствие
            WelcomeGrid.Visibility = Visibility.Collapsed;
            WelcomeGrid.Opacity = 0;

            // Убеждаемся, что MainGrid видим
            MainGrid.Visibility = Visibility.Visible;
            MainGrid.Opacity = 1;

            // Возвращаем размер окна
            this.WindowState = WindowState.Normal;
            this.SizeToContent = SizeToContent.Manual;
            this.Width = 400;
            this.MinHeight = 350;
            this.Height = 350;
            this.SizeToContent = SizeToContent.Height;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // Центрируем окно
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;

            // Показываем форму логина
            LoginFormGrid.Visibility = Visibility.Visible;
            LoginFormGrid.Opacity = 1;

            // Убеждаемся, что все элементы формы логина видимы
            if (LoginInputsPanel != null)
            {
                LoginInputsPanel.Visibility = Visibility.Visible;
            }

            // Очищаем поля
            LoginTextBox.Text = "";
            PasswordBox.Password = "";
            LoginButton.IsEnabled = true;
            LoginTextBox.IsEnabled = true;
            PasswordBox.IsEnabled = true;

            this.Title = "Вход в систему";
            _currentUser = null;

            // Принудительно обновляем layout
            this.UpdateLayout();
        }

    }
}
