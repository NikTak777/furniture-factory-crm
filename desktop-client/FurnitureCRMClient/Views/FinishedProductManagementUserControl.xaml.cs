using System.Windows.Controls;
using FurnitureCRMClient.Models;

namespace FurnitureCRMClient.Views
{
    /// <summary>
    /// Interaction logic for FinishedProductManagementUserControl.xaml
    /// </summary>
    public partial class FinishedProductManagementUserControl : UserControl
    {
        private readonly AuthenticatedUser _currentUser;

        public FinishedProductManagementUserControl(AuthenticatedUser user)
        {
            InitializeComponent();
            _currentUser = user;
        }
    }
}
