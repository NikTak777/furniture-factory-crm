using System;
using System.ComponentModel;
using System.Windows.Input;
using FurnitureCRMClient.Models;
using System.Collections.Generic;
using System.Linq;
using FurnitureCRMClient.Utils; // Добавляем для AuthHelper

namespace FurnitureCRMClient.ViewModels
{
    public class EditStaffViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly HashSet<string> _existingUsernames;

        private Staff _staff;
        public Staff Staff
        {
            get { return _staff; }
            set
            {
                _staff = value;
                OnPropertyChanged(nameof(Staff));
                OnPropertyChanged(nameof(Username));
                OnPropertyChanged(nameof(Password));
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public string Username
        {
            get => Staff?.UserAccount?.Username ?? string.Empty;
            set
            {
                if (Staff != null)
                {
                    if (Staff.UserAccount == null)
                    {
                        Staff.UserAccount = new UserAccount();
                        Staff.UserAccount.StaffId = Staff.StaffId;
                    }
                    Staff.UserAccount.Username = value;
                    OnPropertyChanged(nameof(Username));
                    _ = this[nameof(Username)]; // Вызываем валидацию
                }
            }
        }

        public string Password
        {
            get => Staff?.UserAccount?.Password ?? string.Empty;
            set
            {
                if (Staff != null)
                {
                    if (Staff.UserAccount == null)
                    {
                        Staff.UserAccount = new UserAccount();
                        Staff.UserAccount.StaffId = Staff.StaffId;
                    }
                    Staff.UserAccount.Password = value;
                    OnPropertyChanged(nameof(Password));
                    _ = this[nameof(Password)]; // Вызываем валидацию
                }
            }
        }

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand RegenerateUsernameCommand { get; private set; }
        public ICommand RegeneratePasswordCommand { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public Action? CloseAction { get; set; }

        public Dictionary<string, string> Errors { get; } = new Dictionary<string, string>();

        public List<string> AllowedPositions { get; } = new List<string>
        {
            "Менеджер", "Кладовщик", "Директор"
        };

        public EditStaffViewModel(Staff staff)
            : this(staff, Enumerable.Empty<string>())
        {
        }

        public EditStaffViewModel(Staff staff, IEnumerable<string> existingUsernames)
        {
            _staff = staff;
            _existingUsernames = new HashSet<string>(
                existingUsernames ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            RegenerateUsernameCommand = new RelayCommand(RegenerateUsername);
            RegeneratePasswordCommand = new RelayCommand(RegeneratePassword);

            // Уведомляем UI о начальных значениях
            OnPropertyChanged(nameof(Staff));
            OnPropertyChanged(nameof(Username));
            OnPropertyChanged(nameof(Password));
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

        private void RegenerateUsername(object obj)
        {
            if (Staff.UserAccount == null)
            {
                Staff.UserAccount = new UserAccount();
                Staff.UserAccount.StaffId = Staff.StaffId;
                OnPropertyChanged(nameof(Staff.UserAccount)); // Notify UI that UserAccount object has changed
            }
            Staff.UserAccount.Username = AuthHelper.GenerateRandomUsername(Staff.FullName);
            OnPropertyChanged(nameof(Username));
            _ = this[nameof(Username)]; // Вызываем валидацию после изменения
        }

        private void RegeneratePassword(object obj)
        {
            if (Staff.UserAccount == null)
            {
                Staff.UserAccount = new UserAccount();
                Staff.UserAccount.StaffId = Staff.StaffId;
                OnPropertyChanged(nameof(Staff.UserAccount)); // Notify UI that UserAccount object has changed
            }
            Staff.UserAccount.Password = AuthHelper.GenerateRandomPassword();
            OnPropertyChanged(nameof(Password));
            _ = this[nameof(Password)]; // Вызываем валидацию после изменения
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
                    case "FullName":
                        if (string.IsNullOrWhiteSpace(Staff.FullName))
                            error = "ФИО не может быть пустым.";
                        break;
                    case "Position":
                        if (string.IsNullOrWhiteSpace(Staff.Position))
                            error = "Должность не может быть пустой.";
                        else if (!AllowedPositions.Contains(Staff.Position))
                            error = "Указана недопустимая должность.";
                        break;
                    case "Username":
                        if (Staff?.UserAccount == null || string.IsNullOrWhiteSpace(Username))
                            error = "Логин не может быть пустым.";
                        else if (Username.Length < 8)
                            error = "Логин должен содержать минимум 8 символов.";
                        else if (_existingUsernames.Contains(Username))
                            error = "Пользователь с таким логином уже существует.";
                        break;
                    case "Password":
                        if (Staff?.UserAccount == null || string.IsNullOrWhiteSpace(Password))
                            error = "Пароль не может быть пустым.";
                        else if (Password.Length < 8)
                            error = "Пароль должен содержать минимум 8 символов.";
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
            _ = this[nameof(Username)];
            _ = this[nameof(Password)];

            OnPropertyChanged(nameof(HasErrors));
        }
    }
} 