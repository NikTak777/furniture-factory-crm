using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using FurnitureCRMClient.Models;

namespace FurnitureCRMClient.ViewModels
{
    public class ClientViewModel : INotifyPropertyChanged, IDataErrorInfo
    {
        private readonly HashSet<long> _existingPhones;
        private readonly HashSet<string> _existingEmails;
        private Client _client;
        public Client Client
        {
            get => _client;
            set
            {
                _client = value;
                OnPropertyChanged(nameof(Client));
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        public Action? CloseAction { get; set; }

        public Dictionary<string, string> Errors { get; } = new();

        public bool IsEditMode => Client?.Id != 0;
        public bool HasErrors => Errors.Any();

        // Текстовое представление телефона для ввода пользователем
        private string _phoneText = string.Empty;
        public string PhoneText
        {
            get => _phoneText;
            set
            {
                if (_phoneText != value)
                {
                    _phoneText = value;
                    OnPropertyChanged(nameof(PhoneText));

                    // Пытаемся распарсить и обновить Client.Phone
                    if (long.TryParse(value, out long parsedPhone) && parsedPhone > 0)
                    {
                        if (Client != null) Client.Phone = parsedPhone;
                    }
                    else
                    {
                        if (Client != null) Client.Phone = 0; // Некорректное значение — считаем как ошибку
                    }

                    // Валидируем телефон
                    _ = this[nameof(Client.Phone)];
                }
            }
        }

        public ClientViewModel(Client client)
            : this(client, Enumerable.Empty<long>(), Enumerable.Empty<string>())
        {
        }

        public ClientViewModel(Client client, IEnumerable<long> existingPhones, IEnumerable<string> existingEmails)
        {
            Client = client;
            // Инициализируем PhoneText из текущего значения телефона (если не 0)
            _phoneText = client.Phone > 0 ? client.Phone.ToString() : string.Empty;

            _existingPhones = new HashSet<long>(existingPhones ?? Enumerable.Empty<long>());
            _existingEmails = new HashSet<string>(
                existingEmails?.Where(e => !string.IsNullOrWhiteSpace(e)) ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void Save(object obj)
        {
            ValidateAllProperties();
            if (HasErrors) return;
            CloseAction?.Invoke();
        }

        private bool CanSave(object obj)
        {
            ValidateAllProperties();
            return !HasErrors;
        }

        private void Cancel(object obj)
        {
            Client = null; // Important for cancellation
            CloseAction?.Invoke();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        // IDataErrorInfo implementation
        public string Error => null;

        public string this[string columnName]
        {
            get
            {
                string? error = null;
                if (Client == null) return null;

                // Обрабатываем как "Client.FullName", так и "FullName" для совместимости
                string normalizedName = columnName;
                if (columnName.StartsWith("Client."))
                    normalizedName = columnName.Substring(7); // Убираем "Client."

                switch (normalizedName)
                {
                    case "FullName":
                        if (string.IsNullOrWhiteSpace(Client.FullName))
                            error = "ФИО не может быть пустым.";
                        break;
                    case "Phone":
                    case "PhoneText":
                        // Проверяем, что телефон указан
                        if (string.IsNullOrWhiteSpace(PhoneText))
                        {
                            error = "Номер телефона должен быть указан.";
                        }
                        // Проверяем, что введены только цифры
                        else if (!System.Text.RegularExpressions.Regex.IsMatch(PhoneText, @"^\d+$"))
                        {
                            error = "Номер телефона должен содержать только цифры.";
                        }
                        // Проверяем, что телефон больше 0 (после парсинга)
                        else if (Client.Phone <= 0)
                        {
                            error = "Номер телефона должен быть указан.";
                        }
                        // Уникальность номера телефона
                        else if (_existingPhones.Contains(Client.Phone))
                        {
                            error = "Клиент с таким номером телефона уже существует.";
                        }
                        break;
                    case "Email":
                        if (string.IsNullOrWhiteSpace(Client.Email))
                        {
                            error = "Email не может быть пустым.";
                        }
                        else if (!IsValidEmail(Client.Email))
                        {
                            error = "Некорректный формат Email.";
                        }
                        // Уникальность Email
                        else if (_existingEmails.Contains(Client.Email))
                        {
                            error = "Клиент с таким Email уже существует.";
                        }
                        break;
                    case "Address":
                        if (string.IsNullOrWhiteSpace(Client.Address))
                            error = "Адрес не может быть пустым.";
                        break;
                }
                
                // Нормализуем ключ: для PhoneText используем "PhoneText", для остальных "Client.PropertyName"
                string errorKey;
                if (normalizedName == "PhoneText" || normalizedName == nameof(PhoneText))
                {
                    errorKey = "PhoneText";
                }
                else
                {
                    errorKey = columnName.StartsWith("Client.") ? columnName : $"Client.{normalizedName}";
                }

                bool hadError = Errors.ContainsKey(errorKey);
                if (error == null)
                {
                    if (hadError)
                    {
                        Errors.Remove(errorKey);
                        OnPropertyChanged(nameof(Errors));
                        OnPropertyChanged(nameof(HasErrors));
                    }
                }
                else
                {
                    if (hadError)
                    {
                        if (Errors[errorKey] != error)
                        {
                            Errors[errorKey] = error;
                            OnPropertyChanged(nameof(Errors));
                        }
                    }
                    else
                    {
                        Errors.Add(errorKey, error);
                        OnPropertyChanged(nameof(Errors));
                        OnPropertyChanged(nameof(HasErrors));
                    }
                }
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
                return error;
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        public void ValidateAllProperties()
        {
            if (Client == null) return;
            _ = this[nameof(Client.FullName)];
            _ = this[nameof(PhoneText)]; // Валидируем через PhoneText
            _ = this[nameof(Client.Email)];
            _ = this[nameof(Client.Address)];
            OnPropertyChanged(nameof(HasErrors));
        }
    }
}
