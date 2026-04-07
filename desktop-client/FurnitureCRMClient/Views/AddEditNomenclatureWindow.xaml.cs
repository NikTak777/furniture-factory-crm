using System.Windows;
using System.Windows.Controls;
using FurnitureCRMClient.ViewModels;

namespace FurnitureCRMClient.Views
{
    public partial class AddEditNomenclatureWindow : Window
    {
        public AddEditNomenclatureWindow(NomenclatureViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
            viewModel.CloseAction = () =>
            {
                DialogResult = true;
                Close();
            };

            Closing += (sender, e) =>
            {
                if (DialogResult != true)
                {
                    viewModel.Nomenclature = null; // Сигнализируем об отмене
                }
            };
        }

        private void SelectedMaterialsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not NomenclatureViewModel viewModel)
            {
                return;
            }

            if (sender is not ListBox listBox)
            {
                return;
            }

            if (listBox.SelectedItem is SelectableMaterialViewModel selectedMaterial &&
                selectedMaterial.Material is not null)
            {
                // Подставляем название выбранного материала в строку поиска слева
                viewModel.MaterialSearchText = selectedMaterial.Material.Наименование_материала;
            }
        }
    }
}
