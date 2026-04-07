using System.Windows;
using FurnitureCRMClient.ViewModels;
using System.ComponentModel;

namespace FurnitureCRMClient.Views
{
    /// <summary>
    /// Interaction logic for AddEditMaterialWindow.xaml
    /// </summary>
    public partial class AddEditMaterialWindow : Window
    {
        public AddEditMaterialWindow(MaterialStockViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
            this.Title = viewModel.IsEditMode ? "Редактировать Сырье" : "Добавить Сырье";
            viewModel.CloseAction = () =>
            {
                DialogResult = true;
                Close();
            };

            // Обработчик события закрытия окна
            Closing += (sender, e) =>
            {
                if (DialogResult != true)
                {
                    viewModel.Material = null; // Сигнализируем об отмене
                }
            };
        }
    }
}
