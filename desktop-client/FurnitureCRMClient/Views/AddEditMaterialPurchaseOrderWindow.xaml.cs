using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using FurnitureCRMClient.ViewModels;

namespace FurnitureCRMClient.Views
{
    public partial class AddEditMaterialPurchaseOrderWindow : Window
    {
        public AddEditMaterialPurchaseOrderWindow(MaterialPurchaseOrderViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
            this.Title = viewModel.IsEditMode ? "Редактировать заказ сырья" : "Оформить заказ сырья";
            viewModel.CloseAction = () =>
            {
                DialogResult = true;
                Close();
            };
            Closing += (sender, e) => {
                if (DialogResult != true)
                {
                    viewModel.Order = null;
                }
            };
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is Visibility v && v == Visibility.Visible);
        }
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
