using System.Windows;
using FurnitureCRMClient.Models; // Добавляем using для доступа к AuthenticatedUser
using FurnitureCRMClient;

namespace FurnitureCRMClient.Views
{
    public partial class DirectorWindow : Window
    {
        private readonly AuthenticatedUser _currentUser;

        public DirectorWindow(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            this.Title = $"Панель Директора - {_currentUser.ФИО}";

            // Инициализируем UserControl'ы и присваиваем их ContentControl'ам
            EmployeeManagementContent.Content = new EmployeeManagementUserControl(_currentUser);
            NomenclatureManagementContent.Content = new NomenclatureManagementUserControl(_currentUser);
            ProductionAnalysisContent.Content = new DirectorProductionAnalysisTab(_currentUser);
            StaffSalesAnalysisContent.Content = new DirectorStaffSalesAnalysisTab(_currentUser);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            this.Close();
        }
    }
}