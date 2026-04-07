using System.Windows;
using FurnitureCRMClient.ViewModels;

namespace FurnitureCRMClient.Views
{
    public partial class AddEditClientWindow : Window
    {
        public AddEditClientWindow(ClientViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.CloseAction = () =>
            {
                DialogResult = viewModel.Client != null;
                Close();
            };
            
            // Вызываем валидацию при загрузке окна, чтобы ошибки отображались сразу
            Loaded += (s, e) =>
            {
                viewModel.ValidateAllProperties();
            };
        }
    }
}
