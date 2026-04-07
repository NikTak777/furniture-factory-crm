using System.Windows;
using FurnitureCRMClient.Models;

namespace FurnitureCRMClient.Views
{
    public partial class ManagerWindow : Window
    {
        private readonly AuthenticatedUser _currentUser;
        public ManagerWindow(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            this.Title = $"Панель Менеджера — {_currentUser.ФИО}";
            NomenclatureTabContent.Content = new ManagerNomenclatureTab(_currentUser);
            OrdersTabContent.Content = new ManagerOrdersTab(_currentUser);
            ClientsTabContent.Content = new ClientManagementUserControl(_currentUser);
            ReportsTabContent.Content = new ManagerReportsTab(_currentUser);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            this.Close();
        }
    }
}