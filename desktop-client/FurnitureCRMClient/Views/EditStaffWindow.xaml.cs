using System.Windows;
using FurnitureCRMClient.ViewModels;
using System;

namespace FurnitureCRMClient.Views
{
    public partial class EditStaffWindow : Window
    {
        public EditStaffWindow(EditStaffViewModel viewModel)
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
                    viewModel.Staff = null;
                }
            };
        }
    }
} 