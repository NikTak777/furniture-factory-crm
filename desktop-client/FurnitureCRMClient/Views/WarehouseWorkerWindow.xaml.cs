using System.Windows;
using FurnitureCRMClient.Models; // Добавляем using для доступа к AuthenticatedUser
using FurnitureCRMClient;

namespace FurnitureCRMClient.Views
{
    public partial class WarehouseWorkerWindow : Window
    {
        private readonly AuthenticatedUser _currentUser;

        public WarehouseWorkerWindow(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
            this.Title = $"Панель Кладовщика - {_currentUser.ФИО}";

            // Инициализируем UserControl'ы и присваиваем их ContentControl'ам
            var stockUC = new MaterialStockManagementUserControl(_currentUser);
            var purchaseOrderUC = new MaterialPurchaseOrderUserControl(_currentUser);
            MaterialStockManagementContent.Content = stockUC;
            MaterialPurchaseOrderContent.Content = purchaseOrderUC;
            MaterialNeedsAnalysisContent.Content = new MaterialNeedsAnalysisUserControl(_currentUser);

            // Подписка: если заказ сырья изменился — обновить сырьё на складе
            purchaseOrderUC.MaterialOrderChanged += stockUC.ReloadMaterials;

            // Подписка: если список сырья изменился (добавили/удалили) — обновить список в 'Оформление закупки сырья'
            stockUC.MaterialsListChanged += purchaseOrderUC.ReloadMaterialsListData;
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            new LoginWindow().Show();
            this.Close();
        }
    }
}
