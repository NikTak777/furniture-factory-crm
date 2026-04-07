using System.Windows;
using FurnitureCRMClient.ViewModels;
using System;

namespace FurnitureCRMClient.Views
{
    public partial class AddEditStaffWindow : Window
    {
        public AddEditStaffWindow(AddStaffViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            // �������� ��� ������ "���������" � ������� ���� � ������������� �����������
            viewModel.CloseAction = () =>
            {
                DialogResult = true;
                Close();
            };

            // ��������� �������� ���� ��������� ��� �������
            Closing += (sender, e) =>
            {
                // ���� ���� ����������� �� ����� "���������"
                if (DialogResult != true)
                {
                    viewModel.Staff = null; // ������������� �� ������
                }
            };
        }
    }
}
