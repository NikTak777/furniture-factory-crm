using System;
using System.ComponentModel;
using System.Windows.Input;
using FurnitureCRMClient.Models;
using System.Collections.Generic;
using System.Linq;

namespace FurnitureCRMClient.ViewModels
{
    public class AddStaffViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private Staff _staff;
        public Staff Staff
        {
            get { return _staff; }
            set
            {
                _staff = value;
                OnPropertyChanged(nameof(Staff));
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Action? CloseAction { get; set; }

        public Dictionary<string, string> Errors { get; } = new Dictionary<string, string>();

        public List<string> AllowedPositions { get; } = new List<string>
        {
            "Менеджер", "Кладовщик", "Директор"
        };

        public AddStaffViewModel()
        {
            Staff = new Staff(); // Initialize with a new Staff object for adding
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Save(object obj)
        {
            CloseAction?.Invoke();
        }

        private bool CanSave(object obj)
        {
            ValidateAllStaffProperties();
            return !HasErrors;
        }

        private void Cancel(object obj)
        {
            Staff = null;
            CloseAction?.Invoke();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #region IDataErrorInfo Implementation

        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                string error = null;

                if (Staff == null) return null;

                switch (columnName)
                {
                    case nameof(Staff.FullName):
                        if (string.IsNullOrWhiteSpace(Staff.FullName))
                            error = "ФИО не может быть пустым.";
                        else if (Staff.FullName.Length > 100)
                            error = "ФИО не может превышать 100 символов.";
                        break;
                    case nameof(Staff.Position):
                        if (string.IsNullOrWhiteSpace(Staff.Position))
                            error = "Должность не может быть пустой.";
                        else if (Staff.Position.Length > 50)
                            error = "Должность не может превышать 50 символов.";
                        else if (!AllowedPositions.Contains(Staff.Position))
                            error = "Недопустимая должность. Выберите из списка: Менеджер, Кладовщик.";
                        break;
                }

                bool hadError = Errors.ContainsKey(columnName);

                if (error == null)
                {
                    if (hadError)
                    {
                        Errors.Remove(columnName);
                        OnPropertyChanged(nameof(Errors));
                        OnPropertyChanged(nameof(HasErrors));
                    }
                }
                else
                {
                    if (hadError)
                    {
                        if (Errors[columnName] != error)
                        {
                            Errors[columnName] = error;
                            OnPropertyChanged(nameof(Errors));
                        }
                    }
                    else
                    {
                        Errors.Add(columnName, error);
                        OnPropertyChanged(nameof(Errors));
                        OnPropertyChanged(nameof(HasErrors));
                    }
                }
                
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                return error;
            }
        }

        public bool HasErrors => Errors.Any();

        #endregion

        private void ValidateAllStaffProperties()
        {
            if (Staff == null) return;

            _ = this[nameof(Staff.FullName)];
            _ = this[nameof(Staff.Position)];

            OnPropertyChanged(nameof(HasErrors));
        }
    }
}
